using FIOSharp.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace FIOSharp
{
	public class FnarOracleDataSource : IVariableDataSource
	{
		/// <summary>
		/// The underlying RestSharp client that we're using
		/// </summary>
		protected readonly RestClient restClient;
		private string authKey = "";
		/// <summary>
		/// The username that we are currently logged in as, null if we aren't logged in, or an empty string if the username is unknown
		/// </summary>
		public string AuthoriedAs { get; protected set; } = null;
		private DateTime authKeyExpiry = DateTime.MinValue;
		public bool AuthKeyExpired => authKeyExpiry.CompareTo(DateTime.Now) < 0;
		public bool AlwaysRequireAuth;
		public string APIBaseUrl => restClient.BaseUrl.ToString();

		public FnarOracleDataSource(string APIBaseUrl = "https://rest.fnar.net", bool alwaysAuth = true)
		{
			restClient = new RestClient(APIBaseUrl);
			AlwaysRequireAuth = alwaysAuth;
		}

		/// <summary>
		/// Build a rest request for a given API endpoint. Remember, RestRequests are designed to be single use.
		/// If the authmode is Never, then the current AlwaysRequireAuth condition is ignored. You should not use Never except for login requests
		/// </summary>
		protected RestRequest BuildRequest(string path, AuthMode authMode = AuthMode.IfAvailible)
		{
			RestRequest request = new RestRequest(path, DataFormat.Json);
			if (authMode == AuthMode.Never) return request;
			if (!AuthKeyExpired)
			{
				//auth key is still valid
				request.AddHeader("Authorization", authKey);
			}
			else if (authMode == AuthMode.Require || AlwaysRequireAuth)
			{
				throw new InvalidOperationException("Authorization required, but no valid auth key stored, make sure you've logged in and are keeping your key fresh");
			}
			return request;
		}

		/// <summary>
		/// Log in to the FIO API with a given username and password
		/// </summary>
		/// <returns>The resulting status code. 200 indicates a succesful login, 401 indicates invalid credentials, any other code indicates a failure of some kind</returns>
		public System.Net.HttpStatusCode LoginAs(string username, string password)
		{
			RestRequest request = BuildRequest("auth/login", AuthMode.Never);
			JObject jObject = new JObject();
			jObject.Add("UserName", username);
			jObject.Add("Password", password);
			request.AddJsonBody(jObject.ToString());

			IRestResponse response = restClient.Post(request);
			if (response.StatusCode == System.Net.HttpStatusCode.OK)
			{
				try
				{
					JObject responseObject = JObject.Parse(response.Content);
					authKeyExpiry = responseObject.GetValue("Expiry").ToObject<DateTime>();
					authKey = responseObject.GetValue("AuthToken").ToObject<string>();
					AuthoriedAs = username;
				}
				catch (Exception ex) when (ex is NullReferenceException || ex is JsonReaderException || ex is FormatException || ex is ArgumentException)
				{
					ClearAuth();
					throw new OracleResponseException("auth/login", "Invalid json schema", ex);
				}
			}
			return response.StatusCode;
		}

		/// <summary>
		/// Clear the current stored authorization key.
		/// </summary>
		public void ClearAuth()
		{
			authKeyExpiry = DateTime.MinValue;
			authKey = "";
			AuthoriedAs = null;
		}

		/// <summary>
		/// Check if we are authoried. This will cause API calls to check that our key is valid, so don't over-use this.
		/// Rely on AuthKeyExpired to check that we have a key and it's not stale when you care about execution speed, since this will block threads while it waits for a response.
		/// </summary>
		/// <returns></returns>
		public bool IsAuth()
		{
			if (AuthKeyExpired) return false;
			RestRequest request = BuildRequest("auth", AuthMode.Require);
			IRestResponse response = restClient.Get(request);

			if (response.StatusCode == HttpStatusCode.OK) return true;
			if (response.StatusCode == HttpStatusCode.Unauthorized) return false;

			throw new HttpException(response.StatusCode, response.StatusDescription);
		}

		public virtual List<Material> GetMaterials()
		{
			return GetAndConvertArray<Material>("rain/materials");
		}

		public List<ExchangeData> GetExchanges()
		{
			return GetAndConvertArray<ExchangeData>("global/comexchanges");
		}

		public List<ExchangeEntry> GetEntriesForExchange(ExchangeData exchange, List<Material> allMaterials = null, bool applyToExchange = true)
		{
			return GetEntriesForExchanges(new List<ExchangeData>() { exchange }, allMaterials, applyToExchange);
		}
		
		public List<ExchangeEntry> GetEntriesForExchanges(List<ExchangeData> exchanges, List<Material> allMaterials = null, bool applyToExchanges = true)
		{
			if (allMaterials == null) allMaterials = GetMaterials();

			return GetAndConvertArray("exchange/full", token => {
				try
				{
					JObject jObject = (JObject)token;
					IEnumerable<ExchangeData> foundExchanges = exchanges.Where(exchange => exchange.Ticker.Equals(jObject.GetValue("ExchangeCode").ToObject<string>()));
					return ExchangeEntry.FromJson(jObject, allMaterials, foundExchanges.FirstOrDefault(), applyToExchanges);
				}
				catch(InvalidCastException)
				{
					throw new OracleResponseException("exchange/full", $"Invalid schema, expected json object and got {token.Type}");
				}
				catch(Exception ex) when (ex is JsonSerializationException || ex is NullReferenceException || ex is JsonSchemaException || ex is ArgumentException)
				{
					throw new OracleResponseException("exchange/full", ex);
				}
			}).Where(data => data.Exchange != null).ToList();
		}

		public ExchangeEntry GetEntryForExchange(ExchangeData exchange, Material material, bool applyToExchange = true)
		{
			if (material.Ticker.Equals("CMK")) throw new ArgumentException("Special non marketable material type CMK");

			RestRequest request = BuildRequest($"exchange/{exchange.GetComexMaterialCode(material.Ticker)}");
			IRestResponse response = restClient.Get(request);
			if(response.StatusCode == HttpStatusCode.OK)
			{
				try
				{
					return ExchangeEntry.FromJson(JObject.Parse(response.Content), material, exchange, applyToExchange);

				}
				catch(JsonSchemaException ex)
				{
					throw new OracleResponseException(request.Resource, ex);
				}
			}
			throw new HttpException(response.StatusCode, response.StatusDescription);
		}

		public List<Building> GetBuildings(List<Material> allMaterials = null)
		{
			if (allMaterials == null) allMaterials = GetMaterials();

			Dictionary<string, Building.Builder> builders = GetAndConvertArray<Building.Builder>("rain/buildings").ToDictionary(builder => builder.Ticker.ToUpper());

			foreach(var entry in GetConstructionCosts(allMaterials))
			{
				if (!builders.ContainsKey(entry.building))
				{
					//yes this is throwing for rain/buildings.
					//This is intentional, we're assuming that the building list is incomplete, rather than something extra sneaking into buildingcosts
					throw new OracleResponseException("rain/buildings", $"Missing building {entry.building} required by costs");
				}
				builders[entry.building].setConstructionMaterial(entry.material, entry.count);
			}

			foreach(var entry in GetBuildingPopulations())
			{
				if (!builders.ContainsKey(entry.building))
				{
					//see previous
					throw new OracleResponseException("rain/buildings", $"Missing building {entry.building} required by populations");
				}
				builders[entry.building].setPopulation(entry.populationType, entry.count);
			}

			return builders.Values.Select(builder => builder.Build()).ToList();
		}

		public List<(string building, Material material, int count)> GetConstructionCosts(List<Material> allMaterials = null)
		{
			IRestResponse response = restClient.Get(BuildRequest("rain/buildingcosts"));
			if (response.StatusCode != HttpStatusCode.OK) throw new HttpException(response.StatusCode, response.StatusDescription);

			JArray costsArray;
			try
			{
				costsArray = JArray.Parse(response.Content);
			}
			catch (JsonReaderException ex)
			{
				throw new OracleResponseException("rain/buildingcosts", ex);
			}
			
			return costsArray.Select(token =>
			{
				JObject jObject;
				try
				{
					jObject = (JObject)token;
				}
				catch (InvalidCastException)
				{
					throw new OracleResponseException("rain/buildingcosts", $"expected json object, found {token.Type}");
				}

				string buildingTicker;
				string materialTicker;
				int count;
				try
				{
					buildingTicker = jObject.GetValue("Building").ToObject<string>().ToUpper();
					materialTicker = jObject.GetValue("Material").ToObject<string>().ToUpper();
					count = jObject.GetValue("Amount").ToObject<int>();
				}
				catch (Exception ex) when (ex is NullReferenceException || ex is FormatException || ex is ArgumentException)
				{
					throw new OracleResponseException("rain/buildingcosts", "invalid schema for building costs recived");
				}

				Material material;
				try
				{
					material = allMaterials.Where(mat => mat.Ticker.ToUpper().Equals(materialTicker)).First();
				}
				catch (InvalidOperationException)
				{
					throw new ArgumentException($"Incomplete list of materials provided, missing {materialTicker}");
				}

				return (buildingTicker, material, count);
			}).ToList();
		}

		public List<(string building, PopulationType populationType, int count)> GetBuildingPopulations()
		{
			IRestResponse response = restClient.Get(BuildRequest("rain/buildingworkforces"));
			if (response.StatusCode != HttpStatusCode.OK) throw new HttpException(response.StatusCode, response.StatusDescription);
			JArray jArray;
			try
			{
				jArray = JArray.Parse(response.Content);
			}
			catch(JsonReaderException ex)
			{
				throw new OracleResponseException("rain/buildingworkforces", ex);
			}

			return jArray.Select(token =>
			{
				try
				{
					JObject jObject = (JObject)token;
					return (
					jObject.GetValue("Building").ToObject<string>(),
					PopulationType.Parse(jObject.GetValue("Level").ToObject<string>()),
					jObject.GetValue("Capacity").ToObject<int>());
				}
				catch (Exception ex) when (ex is InvalidCastException || ex is JsonSerializationException || ex is NullReferenceException || ex is ArgumentException || ex is FormatException)
				{
					throw new OracleResponseException("rain/buildingworkforces", ex);
				}
			}).ToList();

		}

		public List<Recipe> GetRecipes(List<Material> allMaterials = null, List<Building> allBuildings = null)
		{
			if (allMaterials == null) allMaterials = GetMaterials();
			if (allBuildings == null) allBuildings = GetBuildings(allMaterials);

			try
			{
				return GetAndConvertArray(BuildRequest("recipe/allrecipes", AuthMode.Require), token => Recipe.FromJson((JObject)token, allMaterials, allBuildings));
			}
			catch (InvalidCastException)
			{
				throw new OracleResponseException("recipe/allrecipes", "did not recive a list of objects");
			}
			catch (JsonSchemaException ex)
			{
				throw new OracleResponseException("recipe/allrecipes", ex);
			}
		}

		public List<WorkforceRequirement> GetWorkforceRequirements(List<Material> allMaterials = null)
		{
			if (allMaterials == null) allMaterials = GetMaterials();
			try
			{
				return GetAndConvertArray("global/workforceneeds", token => WorkforceRequirement.FromJson((JObject)token, allMaterials));
			} catch (JsonSchemaException ex)
			{
				throw new OracleResponseException("global/workforceneeds", ex);
			}
		}

		protected List<T> GetAndConvertArray<T>(string path)
		{
			return GetAndConvertArray(path, token =>
			{
				try { return token.ToObject<T>(); }
				catch (Exception ex) when (ex is JsonSerializationException || ex is FormatException || ex is ArgumentException)
				{
					throw new OracleResponseException(path, ex);
				}
			}
			);
		}
		protected List<T> GetAndConvertArray<T>(string path, Func<JToken, T> converter)
		{
			return GetAndConvertArray<T>(BuildRequest(path), converter);
		}

		protected List<T> GetAndConvertArray<T>(RestRequest request, Func<JToken, T> converter)
		{
			IRestResponse response = restClient.Get(request);
			if(response.StatusCode != HttpStatusCode.OK) throw new HttpException(response.StatusCode, response.StatusDescription);
			try
			{
				return JArray.Parse(response.Content).Select(converter).ToList();
			}
			catch(JsonReaderException ex)
			{
				throw new OracleResponseException(request.Resource, "Could not parse recived json", ex);
			}
		}
	}
}
