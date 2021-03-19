using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using FIOSharp.Data;

namespace FIOSharp
{

	// this is old legacy junk. It's only here for my personal reference
	/*
	public class FnarOracleClient
	{
		
		#region requests
		private RestRequest materialsRequest => BuildRequest("rain/materials");
		private RestRequest buildingRequest => BuildRequest("rain/buildings");
		private RestRequest buildingCostRequest => BuildRequest("rain/buildingcosts");
		private RestRequest buildingWorkForceRequest => BuildRequest("rain/buildingworkforces");
		private RestRequest recipeRequest => BuildRequest("rain/buildingrecipes");
		private RestRequest recipeInputRequest => BuildRequest("rain/recipeinputs");
		private RestRequest recipeOutputRequest => BuildRequest("rain/recipeoutputs");
		private RestRequest exchangesRequest => BuildRequest("global/comexexchanges");
		#endregion

		private readonly RestClient oracleClient;
		private string authKey = "";
		public string AuthoriedAs { get; private set; } = "";
		private DateTime authKeyExpiry = DateTime.MinValue;

		public bool AuthKeyExpired => authKeyExpiry.CompareTo(DateTime.Now) < 0;
		public bool AlwaysRequireAuth;
		public string APIBaseUrl => oracleClient.BaseUrl.ToString();

		public FnarOracleClient(string APIBaseUrl = "https://rest.fnar.net", bool alwaysAuth = true)
		{
			oracleClient = new RestClient(APIBaseUrl);
			AlwaysRequireAuth = alwaysAuth;
		}

		private RestRequest BuildRequest(string path, AuthMode authMode = AuthMode.IfAvailible)
		{
			RestRequest request = new RestRequest(path, DataFormat.Json);
			if (authMode == AuthMode.Never) return request;
			if(!AuthKeyExpired)
			{
				//auth key is still valid
				request.AddHeader("Authorization", authKey);
			}
			else if(authMode == AuthMode.Require || AlwaysRequireAuth)
			{
				throw new InvalidOperationException("Authorization required, but no valid auth key stored, make sure you've logged in and are keeping your key fresh");
			}
			return request;
		}

		public System.Net.HttpStatusCode LoginAs(string username, string password)
		{
			RestRequest request = new RestRequest("auth/login", DataFormat.Json);
			JObject jObject = new JObject();
			jObject.Add("UserName", username);
			jObject.Add("Password", password);
			request.AddJsonBody(jObject.ToString());

			IRestResponse response = oracleClient.Post(request);
			if(response.StatusCode == System.Net.HttpStatusCode.OK)
			{
				JObject responseObject = JObject.Parse(response.Content);
				authKeyExpiry = responseObject.Value<JToken>("Expiry").ToObject<DateTime>();
				authKey = responseObject.Value<JToken>("AuthToken").ToObject<string>();
				AuthoriedAs = username;
			}
			return response.StatusCode;
		}

		public void ClearAuth()
		{
			authKeyExpiry = DateTime.MinValue;
			authKey = "";
			AuthoriedAs = "";
		}

		public bool IsAuth()
		{
			if (AuthKeyExpired) return false;
			RestRequest request = BuildRequest("auth", AuthMode.Require);
			IRestResponse response = oracleClient.Get(request);

			if (response.StatusCode == System.Net.HttpStatusCode.OK) return true;
			if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) return false;

			throw new HttpException(response.StatusCode, response.StatusDescription);
		}

		public virtual List<Material> GetMaterials()
		{
			IRestResponse response = oracleClient.Get(materialsRequest);
			if (response.StatusCode == System.Net.HttpStatusCode.OK)
				return JArray.Parse(response.Content).Select(token => token.ToObject<Material>()).ToList();
			throw new HttpException(response.StatusCode, response.StatusDescription);
		}

		public virtual List<ExchangeData> GetExchanges()
		{
			IRestResponse response = oracleClient.Get(exchangesRequest);
			if(response.StatusCode == System.Net.HttpStatusCode.OK)
				return JArray.Parse(response.Content).Select(token => token.ToObject<ExchangeData>()).ToList();

			throw new HttpException(response.StatusCode, response.StatusDescription);
		}

		public List<ExchangeEntry> GetAllEntriesForExchange(ExchangeData exchange, List<Material> allMaterials, bool applyUpdate = true)
		{
			return GetAllEntriesForExchanges(new List<ExchangeData>() { exchange }, allMaterials, applyUpdate);
		}

		public List<ExchangeEntry> GetAllEntriesForExchanges(List<ExchangeData> exchanges, List<Material> allMaterials, bool applyUpdate = true)
		{
			RestRequest request = BuildRequest("exchange/full");
			IRestResponse response = oracleClient.Get(request);
			if (response.StatusCode == System.Net.HttpStatusCode.OK)
				return JArray.Parse(response.Content).Select(token =>
				{
					JObject jObject = (JObject)token;
					var foundExchanges = exchanges.Where(exchange => exchange.Ticker.Equals(jObject.GetValue("ExchangeCode").ToObject<string>()));
					return ExchangeEntry.FromJson(jObject, allMaterials, foundExchanges.FirstOrDefault(), applyUpdate);
				}).Where(entry => entry.Exchange != null).ToList();

			throw new HttpException(response.StatusCode, response.StatusDescription);
		}

		public ExchangeEntry GetEntryForExchange(ExchangeData exchange, Material material, bool applyUpdate = true)
		{
			if (material.Ticker.Equals("CMK"))
			{
				throw new ArgumentException("Special non marketable material type CMK");
			}

			RestRequest request = BuildRequest("exchange/" + exchange.GetComexMaterialCode(material.Ticker));
			IRestResponse response = oracleClient.Get(request);
			if(response.StatusCode == System.Net.HttpStatusCode.OK)
				return ExchangeEntry.FromJson(JObject.Parse(response.Content), material, exchange, applyUpdate);


			throw new HttpException(response.StatusCode, response.StatusDescription);
		}

		public virtual List<Building> GetBuildings(bool getConstructionCost = true, bool getPopulation = true)
		{
			List<Building.Builder> builders = TryGetAndConvertArray<Building.Builder>(buildingRequest);

			if (getConstructionCost)
			{
				//small helper method for parsing into a tuple
				static (string Building, string Material, int Amount) parse(JToken token)
				{
					JObject obj = (JObject)token;
					string building = obj.GetValue("Building").ToObject<string>();
					string capacity = obj.GetValue("Material").ToObject<string>();
					int amount = obj.GetValue("Amount").ToObject<int>();
					return (building, capacity, amount);
				}

				foreach((string Building, string Material, int Amount) in TryGetAndConvertArray(buildingCostRequest, parse))
				{
					foreach(Building.Builder builder in builders)
					{
						if (builder.Ticker.Equals(Building))
						{
							builder.setConstructionMaterial(Material, Amount);
						}
					}
					//builders.Where(builder => builder.Ticker.Equals(Building)).Select(builder => builder.setConstructionMaterial(Material, Amount));
				}
			}

			if (getPopulation)
			{
				//small helper method for parsing into a tuple
				static (string Building, PopulationType populationType, int Amount) parse(JToken token)
				{
					JObject obj = (JObject)token;
					string building = obj.GetValue("Building").ToObject<string>();
					int capacity = obj.GetValue("Capacity").ToObject<int>();
					string level = obj.GetValue("Level").ToObject<string>();
					PopulationType pop;
					switch (level)
					{
						case "PIONEER":
							pop = PopulationType.Pioneer;
							break;
						case "SETTLER":
							pop = PopulationType.Settler;
							break;
						case "TECHNICIAN":
							pop = PopulationType.Technician;
							break;
						case "ENGINEER":
							pop = PopulationType.Engineer;
							break;
						case "SCIENTIST":
							pop = PopulationType.Scientist;
							break;
						default:
							throw new InvalidOperationException("Recived impossible population type: " + level);
					}

					return (building, pop, capacity);
				}


				foreach ((string Building, PopulationType populationType, int Amount) in TryGetAndConvertArray(buildingWorkForceRequest, parse))
				{
					foreach (Building.Builder builder in builders)
					{
						if (builder.Ticker.Equals(Building))
						{
							builder.setPopulation(populationType, Amount);
						}
					}
					//linq doesn't seem to be playing nice for some reason
					//builders.Where(builder => builder.Ticker.Equals(Building)).Select(builder => builder.setPopulation(populationType, Amount));
				}
			}


			return builders.Select(builder => builder.Build()).ToList();
		}

		public List<Recipe> GetRecipes(bool includeInputs = true, bool includeOutputs = true)
		{
			List<Recipe.Builder> builders = TryGetAndConvertArray<Recipe.Builder>(recipeRequest);


			static (string Key, string Material, int Amount) parse(JToken token)
			{
				JObject obj = (JObject)token;
				string key = obj.GetValue("Key").ToObject<string>();
				string capacity = obj.GetValue("Material").ToObject<string>();
				int amount = obj.GetValue("Amount").ToObject<int>();
				return (key, capacity, amount);
			}

			if (includeInputs)
			{
				foreach((string Key, string Material, int Amount) in TryGetAndConvertArray(recipeInputRequest, parse))
				{
					foreach(Recipe.Builder builder in builders)
					{
						if (builder.Key.Equals(Key))
						{
							builder.SetInput(Material, Amount);
						}
					}
				}
			}

			if (includeOutputs)
			{
				foreach ((string Key, string Material, int Amount) in TryGetAndConvertArray(recipeOutputRequest, parse))
				{
					foreach (Recipe.Builder builder in builders)
					{
						if (builder.Key.Equals(Key))
						{
							builder.SetOutput(Material, Amount);
						}
					}
				}
			}


			return builders.Select(builder => builder.Build()).ToList();
		}

		private List<T> TryGetAndConvertArray<T>(RestRequest request)
		{
			return TryGetAndConvertArray(request, token => token.ToObject<T>());
		}

		private List<T> TryGetAndConvertArray<T>(RestRequest request, Func<JToken, T> converter)
		{
			return TryGetArray(request).Select(converter).ToList();
		}

		private JArray TryGetArray(RestRequest request){
			IRestResponse response = oracleClient.Execute(request);
			if (response.StatusCode == System.Net.HttpStatusCode.OK)
			{
				//parse the Json
				return JArray.Parse(response.Content);
			}
			else
			{
				throw new HttpException(response.StatusCode, response.StatusDescription);
			}
		}
	}
	*/
}
