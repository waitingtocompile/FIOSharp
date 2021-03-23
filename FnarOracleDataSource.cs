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
using System.Threading.Tasks;

namespace FIOSharp
{
	public class FnarOracleDataSource : IVariableDataSource
	{
		/// <summary>
		/// The underlying RestSharp client that we're using. You shouldn't be making calls to this directly for the most part, but instead using the rate-limited helper functions
		/// </summary>
		protected readonly RestClient restClient;
		
		/// <summary>
		/// The username that we are currently logged in as, null if we aren't logged in, or an empty string if the username is unknown
		/// </summary>
		public string AuthoriedAs { get; protected set; } = null;
		
		public bool AuthKeyExpired => authKeyExpiry.CompareTo(DateTime.Now) < 0;
		public bool AlwaysRequireAuth;
		public string APIBaseUrl => restClient.BaseUrl.ToString();

		/// <summary>
		/// The amount of time in ms to wait for the rate limiter when making a request.
		/// 0 inidcates to never wait for the rate limited, -1 indicates to wait forever
		/// </summary>
		public int RateLimitTimeout { get => _rateLimitTimeout; set
			{
				if (value < -1) throw new ArgumentOutOfRangeException("rate limt timeout must be a positive integer or -1");
				_rateLimitTimeout = value;
			}
		}
		private int _rateLimitTimeout = -1;

		/// <summary>
		/// The amount of time to wait for a response from the server in ms
		/// </summary>
		public int RestTimeout { get => restClient.Timeout; set => restClient.Timeout = value; }


		private string authKey = "";
		private DateTime authKeyExpiry = DateTime.MinValue;
		/// <summary>
		/// This is our rate limiter, all API calls should in some capacity go through this. There's a bunch of helper methods to make this fairly painless when you're adding new enpoints
		/// </summary>
		protected readonly RateLimiter rateLimiter;

		/// <param name="APIBaseUrl">The base URL of the API.</param>
		/// <param name="alwaysAuth">If true, will only send API requests if it is logged in, even if the endpoint doesn't strictly require authorisation</param>
		/// <param name="rateLimit">the maximum number of API calls to send per second. Setting below the default value is strongly discouraged since Saganaki doesn't want the server to get overwhlemed</param>
		public FnarOracleDataSource(string APIBaseUrl = "https://rest.fnar.net", bool alwaysAuth = true, int rateLimit = 3)
		{
			restClient = new RestClient(APIBaseUrl);
			AlwaysRequireAuth = alwaysAuth;
			rateLimiter = new RateLimiter(rateLimit, 1000);
		}

		~FnarOracleDataSource()
		{
			rateLimiter.Dispose();
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

		#region rate limited GET and POST helpers
		/// <summary>
		/// Perform a rate limited GET call on the api
		/// </summary>
		/// <param name="path">the path of the endpoint to use</param>
		/// <param name="authMode">the authentication mode to use</param>
		/// <returns>The RestResponse from the request</returns>
		protected IRestResponse RateLimitedGet(string path, AuthMode authMode = AuthMode.IfAvailible)
		{
			return RateLimitedGet(BuildRequest(path, authMode));
		}

		/// <summary>
		/// Perform a rate limited GET call on the api
		/// </summary>
		/// <param name="restRequest">the rest request to be executed</param>
		/// <returns>The RestResponse from the request</returns>
		protected IRestResponse RateLimitedGet(RestRequest restRequest)
		{
			return rateLimiter.Run(() => restClient.Get(restRequest), RateLimitTimeout);
		}

		/// <summary>
		/// Perform a rate limited asynchronous GET call on the api
		/// </summary>
		/// <param name="path">the path of the endpoint to use</param>
		/// <param name="authMode">the authentication mode to use</param>
		/// <returns>The RestResponse from the request</returns>
		protected async Task<IRestResponse> RateLimitedGetAsync(string path, AuthMode authMode = AuthMode.IfAvailible)
		{
			return await RateLimitedGetAsync(BuildRequest(path, authMode));
		}

		/// <summary>
		/// Perform a rate limited asynchronous GET call on the api
		/// </summary>
		/// <param name="restRequest">the rest request to be executed</param>
		/// <returns>The RestResponse from the request</returns>
		protected async Task<IRestResponse> RateLimitedGetAsync(RestRequest restRequest)
		{
			return await rateLimiter.RunAsync(async () => await restClient.ExecuteGetAsync(restRequest), RateLimitTimeout);
		}

		/// <summary>
		/// Perform a rate limited POST call on the api
		/// </summary>
		/// <param name="path">the endpoint to use</param>
		/// <param name="requestBody">the request body, or leave blank to POST with no body</param>
		/// <param name="authMode">the authentication mode to use</param>
		/// <returns>The RestResponse from the request<</returns>
		protected IRestResponse RateLimitedPost(string path, string requestJsonBody = "", AuthMode authMode = AuthMode.IfAvailible)
		{
			RestRequest request = BuildRequest(path, authMode);
			if(requestJsonBody.Length > 0)
			{
				request.AddJsonBody(requestJsonBody);
			}
			return RateLimitedPost(request);
		}

		/// <summary>
		/// Perform a rate limited asynchronous POST call on the api
		/// </summary>
		/// <param name="restRequest">the rest request to be executed</param>
		/// <returns>The RestResponse from the request<</returns>
		protected IRestResponse RateLimitedPost(RestRequest restRequest)
		{
			return rateLimiter.Run(() => restClient.Post(restRequest), RateLimitTimeout);
		}

		/// <summary>
		/// Perform a rate limited asynchronous POST call on the api
		/// </summary>
		/// <param name="path">the endpoint to use</param>
		/// <param name="requestBody">the request body, or leave blank to POST with no body</param>
		/// <param name="authMode">the authentication mode to use</param>
		/// <returns>The RestResponse from the request<</returns>
		protected async Task<IRestResponse> RateLimitedPostAsync(string path, string requestJsonBody = "", AuthMode authMode = AuthMode.IfAvailible)
		{
			RestRequest request = BuildRequest(path, authMode);
			if (requestJsonBody.Length > 0)
			{
				request.AddJsonBody(requestJsonBody);
			}
			return await RateLimitedPostAsync(request);
		}

		/// <summary>
		/// Perform a rate limited POST call on the api
		/// </summary>
		/// <param name="restRequest">the rest request to be executed</param>
		/// <returns>The RestResponse from the request<</returns>
		protected async Task<IRestResponse> RateLimitedPostAsync(RestRequest restRequest)
		{
			return await rateLimiter.RunAsync(async () => await restClient.ExecutePostAsync(restRequest), RateLimitTimeout);
		}
		#endregion


		/// <summary>
		/// Clear the current stored authorization key.
		/// </summary>
		public void ClearAuth()
		{
			authKeyExpiry = DateTime.MinValue;
			authKey = "";
			AuthoriedAs = null;
		}

		#region sync endpoints

		/// <summary>
		/// Check if we are authoried. This will cause API calls to check that our key is valid, so don't over-use this.
		/// Rely on AuthKeyExpired to check that we have a key and it's not stale when you care about execution speed, since this will block threads while it waits for a response.
		/// </summary>
		public bool IsAuth()
		{
			if (AuthKeyExpired) return false;
			IRestResponse response = RateLimitedGet("auth", AuthMode.Require);

			if (response.StatusCode == HttpStatusCode.OK) return true;
			if (response.StatusCode == HttpStatusCode.Unauthorized) return false;

			throw new HttpException(response.StatusCode, response.StatusDescription);
		}

		/// <summary>
		/// Log in to the FIO API with a given username and password
		/// </summary>
		/// <returns>The resulting status code. 200 indicates a succesful login, 401 indicates invalid credentials, any other code indicates a failure of some kind</returns>
		public HttpStatusCode LoginAs(string username, string password)
		{
			JObject jObject = new JObject();
			jObject.Add("UserName", username);
			jObject.Add("Password", password);

			IRestResponse response = RateLimitedPost("auth/login", jObject.ToString(), AuthMode.Never);
			if (response.StatusCode == HttpStatusCode.OK)
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


		public List<Material> GetMaterials()
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
			
			IRestResponse response = RateLimitedGet($"exchange/{exchange.GetComexMaterialCode(material.Ticker)}");
			if(response.StatusCode == HttpStatusCode.OK)
			{
				try
				{
					return ExchangeEntry.FromJson(JObject.Parse(response.Content), material, exchange, applyToExchange);

				}
				catch(JsonSchemaException ex)
				{
					throw new OracleResponseException(response.Request.Resource, ex);
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

		public List<(string building, Material material, int count)> GetConstructionCosts(List<Material> allMaterials)
		{
			IRestResponse response = RateLimitedGet("rain/buildingcosts");
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
			IRestResponse response = RateLimitedGet("rain/buildingworkforces");
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

		protected List<T> GetAndConvertArray<T>(string path, Func<JToken, T> converter = null)
		{
			

			return GetAndConvertArray<T>(BuildRequest(path), converter);
		}

		protected List<T> GetAndConvertArray<T>(RestRequest request, Func<JToken, T> converter = null)
		{
			if (converter == null) converter = token =>
			{
				try { return token.ToObject<T>(); }
				catch (Exception ex) when (ex is JsonSerializationException || ex is FormatException || ex is ArgumentException)
				{
					throw new OracleResponseException(request.Resource, ex);
				}
			};

			IRestResponse response = RateLimitedGet(request);
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
		#endregion

		#region Async endpoints

		/// <summary>
		/// Check if we are authoried. This will cause API calls to check that our key is valid, so don't over-use this.
		/// Rely on AuthKeyExpired to check that we have a key and it's not stale when you care about execution speed.
		/// </summary>
		public async Task<bool> IsAuthAsync()
		{
			if (AuthKeyExpired) return false;
			IRestResponse response = await RateLimitedGetAsync("auth", AuthMode.Require);

			if (response.StatusCode == HttpStatusCode.OK) return true;
			if (response.StatusCode == HttpStatusCode.Unauthorized) return false;

			throw new HttpException(response.StatusCode, response.StatusDescription);
		}

		/// <summary>
		/// Log in to the FIO API with a given username and password. On a success the resulting auth key will be stored for later use.
		/// </summary>
		/// <returns>The resulting status code. 200 indicates a succesful login, 401 indicates invalid credentials, any other code indicates a failure of some kind</returns>
		public async Task<HttpStatusCode> LoginAsAsync(string username, string password)
		{
			JObject jObject = new JObject();
			jObject.Add("UserName", username);
			jObject.Add("Password", password);

			IRestResponse response = await RateLimitedPostAsync("auth/login", jObject.ToString(), AuthMode.Never);
			if (response.StatusCode == HttpStatusCode.OK)
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

		public async Task<List<Material>> GetMaterialsAsync()
		{
			return (await GetAndConvertArrayAsync<Material>("rain/materials")).ToList();
		}

		public async Task<List<ExchangeData>> GetExchangesAsync()
		{
			return (await GetAndConvertArrayAsync<ExchangeData>("global/comexchanges")).ToList();
		}

		public async Task<List<ExchangeEntry>> GetEntriesForExchangeAsync(ExchangeData exchange, List<Material> allMaterials = null, bool applyToExchanges = true)
		{
			return await GetEntriesForExchangesAsync(new List<ExchangeData>() { exchange }, allMaterials, applyToExchanges);
		}

		public async Task<List<ExchangeEntry>> GetEntriesForExchangesAsync(List<ExchangeData> exchanges, List<Material> allMaterials = null, bool applyToExchanges = true)
		{
			//todo: do our cascading in a more async friendly manner
			Task<List<Material>> awaitingMaterials = (allMaterials == null) ? GetMaterialsAsync() : Task.FromResult(allMaterials);

			IEnumerable<JObject> jObjects = await GetAndConvertArrayAsync("exchange/full", token => {
				try { return (JObject)token; }
				catch (InvalidCastException) { throw new OracleResponseException("exchange/full", "Invalid schema, was expecting a json objects"); }
			});
			allMaterials = await awaitingMaterials;

			IEnumerable<ExchangeEntry> entries = await Task.WhenAll(jObjects.Select(async jObject => 
			{
				try
				{
					IEnumerable<ExchangeData> foundExchanges = exchanges.Where(exchange => exchange.Ticker.Equals(jObject.GetValue("ExchangeCode").ToObject<string>()));
					return ExchangeEntry.FromJson(jObject, await awaitingMaterials, foundExchanges.FirstOrDefault(), applyToExchanges);
				}
				catch (Exception ex) when (ex is JsonSerializationException || ex is NullReferenceException || ex is JsonSchemaException || ex is ArgumentException)
				{
					throw new OracleResponseException("exchange/full", ex);
				}
			})));

			return entries.Where(data => data.Exchange != null).ToList();
		}

		public async Task<ExchangeEntry> GetEntryForExchangeAsync(ExchangeData exchange, Material material, bool applyToExchange = true)
		{
			if (material.Ticker.Equals("CMK")) throw new ArgumentException("Special non marketable material type CMK");

			IRestResponse response = await RateLimitedGetAsync($"exchange/{exchange.GetComexMaterialCode(material.Ticker)}");
			if (response.StatusCode == HttpStatusCode.OK)
			{
				try
				{
					return ExchangeEntry.FromJson(JObject.Parse(response.Content), material, exchange, applyToExchange);

				}
				catch (JsonSchemaException ex)
				{
					throw new OracleResponseException(response.Request.Resource, ex);
				}
			}
			throw new HttpException(response.StatusCode, response.StatusDescription);
		}

		public async Task<List<Building>> GetBuildingsAsync(List<Material> allMaterials = null)
		{
			Task<List<Material>> materialsTask = (allMaterials == null) ? GetMaterialsAsync() : Task.FromResult(allMaterials);

			Task<IEnumerable<Building.Builder>> buildersTask = GetAndConvertArrayAsync<Building.Builder>("rain/buildings");
			var costsTask = GetConstructionCostsAsync(materialsTask);
			var populationsTask = GetBuildingPopulationsAsync();

			Dictionary<string, Building.Builder> builders = (await buildersTask).ToDictionary(builder => builder.Ticker, builder => builder);

			//we're doing this assignment process synchronously so that we don't have multiple threads trying to interact with our non thread safe collections.
			//yes we probably *could* make them all thread safe or all our operations atomic, I'm not convinced it's worth the effort, we're already threading our two longest processes:
			//Rest requests and deserialization
			foreach (var entry in await costsTask)
			{
				if (!builders.ContainsKey(entry.building))
				{
					//yes this is throwing for rain/buildings.
					//This is intentional, we're assuming that the building list is incomplete, rather than something extra sneaking into buildingcosts
					throw new OracleResponseException("rain/buildings", $"Missing building {entry.building} required by costs");
				}
				builders[entry.building].setConstructionMaterial(entry.material, entry.count);
			}

			foreach (var entry in await populationsTask)
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
		public async Task<List<(string building, Material material, int count)>> GetConstructionCostsAsync(List<Material> allMaterials)
		{
			return await GetConstructionCostsAsync(Task.FromResult(allMaterials));
		}

		protected async Task<List<(string building, Material material, int count)>> GetConstructionCostsAsync(Task<List<Material>> allMaterialsTask)
		{
			IRestResponse response = await RateLimitedGetAsync("rain/buildingcosts");
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

			List<Material> allMaterials = await allMaterialsTask;

			return (await Task.WhenAll(costsArray.Select(token => Task.Run(() =>
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
			})))).ToList();
		}

		public async Task<List<(string building, PopulationType populationType, int count)>> GetBuildingPopulationsAsync()
		{
			IRestResponse response = await RateLimitedGetAsync("rain/buildingworkforces");
			if (response.StatusCode != HttpStatusCode.OK) throw new HttpException(response.StatusCode, response.StatusDescription);
			JArray jArray;
			try
			{
				jArray = JArray.Parse(response.Content);
			}
			catch (JsonReaderException ex)
			{
				throw new OracleResponseException("rain/buildingworkforces", ex);
			}

			return (await Task.WhenAll(jArray.Select(token => Task.Run(() =>
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
			})))).ToList();
		}

		public async Task<List<Recipe>> GetRecipesAsync(List<Material> allMaterials = null, List<Building> allBuildings = null)
		{
			Task<List<Material>> materialsTask = (allMaterials == null) ? GetMaterialsAsync() : Task.FromResult(allMaterials);
			Task<List<Building>> buildingsTask = (allBuildings == null) ? GetBuildingsAsync(await materialsTask) : Task.FromResult(allBuildings);

			try
			{

				Task<Recipe[]> task = Task.WhenAll(await GetAndConvertArrayAsync(BuildRequest("recipe/allrecipes", AuthMode.Require), async token => Recipe.FromJson((JObject)token, await materialsTask, await buildingsTask)));
				return (await task).ToList();
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

		public async Task<List<WorkforceRequirement>> GetWorkforceRequirementsAsync(List<Material> allMaterials = null)
		{
			Task<List<Material>> materialsTask = (allMaterials == null) ? GetMaterialsAsync() : Task.FromResult(allMaterials);
			try
			{
				return (await GetAndConvertArrayAsync("global/workforceneeds", async token => WorkforceRequirement.FromJson((JObject)token, await materialsTask))).ToList();
			}
			catch (JsonSchemaException ex)
			{
				throw new OracleResponseException("global/workforceneeds", ex);
			}
		}

		protected async Task<IEnumerable<T>> GetAndConvertArrayAsync<T>(string path, Func<JToken, Task<T>> taskConverter = null)
		{
			if (taskConverter != null) taskConverter = token => Task.Run(() =>
			{
				try { return token.ToObject<T>(); }
				catch (Exception ex) when (ex is JsonSerializationException || ex is FormatException || ex is ArgumentException)
				{
					throw new OracleResponseException(path, ex);
				}
			});
			IEnumerable<Task<T>> tasks = await GetAndConvertArrayAsync<Task<T>>(path, taskConverter);
			return await Task.WhenAll(tasks.ToArray());
		}

		protected async Task<IEnumerable<T>> GetAndConvertArrayAsync<T>(string path, Func<JToken, T> converter)
		{
			return await GetAndConvertArrayAsync<T>(BuildRequest(path), converter);
		}

		protected async Task<IEnumerable<T>> GetAndConvertArrayAsync<T>(RestRequest request, Func<JToken, T> converter)
		{
			

			IRestResponse response = await RateLimitedGetAsync(request);
			if (response.StatusCode != HttpStatusCode.OK) throw new HttpException(response.StatusCode, response.StatusDescription);
			try
			{
				return JArray.Parse(response.Content).Select(converter);
			}
			catch (JsonReaderException ex)
			{
				throw new OracleResponseException(request.Resource, "Could not parse recived json", ex);
			}
		}

		#endregion
	}
}
