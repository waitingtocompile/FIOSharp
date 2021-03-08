using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using FIOSharp.Data;
using FIOSharp.Data.Linked;

namespace FIOSharp
{
	public class FnarOracleClient
	{
		private RestClient oracleClient;

		//region requests
		private static RestRequest materialsRequest => new RestRequest("rain/materials", DataFormat.Json);
		private static RestRequest buildingRequest => new RestRequest("rain/buildings", DataFormat.Json);
		private static RestRequest buildingCostRequest => new RestRequest("rain/buildingcosts", DataFormat.Json);
		private static RestRequest buildingWorkForceRequest => new RestRequest("rain/buildingworkforces", DataFormat.Json);
		private static RestRequest recipeRequest => new RestRequest("rain/buildingrecipes", DataFormat.Json);
		private static RestRequest recipeInputRequest => new RestRequest("rain/recipeinputs", DataFormat.Json);
		private static RestRequest recipeOutputRequest => new RestRequest("rain/recipeoutputs", DataFormat.Json);
		private static RestRequest exchangesRequest => new RestRequest("global/comexexchanges", DataFormat.Json);
		//endregion

		public FnarOracleClient(string APIBaseUrl = "https://rest.fnar.net")
		{
			oracleClient = new RestClient(APIBaseUrl);
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
				DateTime expiry = responseObject.Value<JToken>("Expiry").ToObject<DateTime>();
				string authKey = responseObject.Value<JToken>("AuthToken").ToObject<string>();
				//logged in, actually store the token etc
			}
			return response.StatusCode;
		}

		public List<Material> GetMaterials()
		{
			IRestResponse response = oracleClient.Get(materialsRequest);
			if (response.StatusCode == System.Net.HttpStatusCode.OK)
			{
				return JArray.Parse(response.Content).Select(token => token.ToObject<Material>()).ToList();
			}
			else
			{
				throw new HttpException(response.StatusCode, response.StatusDescription);
			}
		}

		public List<ExchangeData> GetExchanges()
		{
			IRestResponse response = oracleClient.Get(exchangesRequest);
			if(response.StatusCode == System.Net.HttpStatusCode.OK)
			{
				JArray responseArray = JArray.Parse(response.Content);
				return responseArray.Select(token => token.ToObject<ExchangeData>()).ToList();
			}
			else
			{
				throw new HttpException(response.StatusCode, response.StatusDescription);
			}
		}

		public ExchangeEntry GetEntryForExchange(ExchangeData exchange, Material material)
		{
			if (material.Ticker.Equals("CMK"))
			{
				throw new ArgumentException("Special non marketable material type CMK");
			}
			RestRequest request = new RestRequest("exchange/" + exchange.GetComexMaterialCode(material.Ticker), DataFormat.Json);
			IRestResponse response = oracleClient.Get(request);
			if(response.StatusCode == System.Net.HttpStatusCode.OK)
			{
				return ExchangeEntry.FromJson(JObject.Parse(response.Content), material, exchange);
			}
			else
			{
				throw new HttpException(response.StatusCode, response.StatusDescription);
			}
		}

		public List<Building> GetBuildings(bool getConstructionCost = true, bool getPopulation = true)
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

		public List<BuildingRecipe> GetRecipes(bool includeInputs = true, bool includeOutputs = true)
		{
			List<BuildingRecipe.Builder> builders = TryGetAndConvertArray<BuildingRecipe.Builder>(recipeRequest);


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
					foreach(BuildingRecipe.Builder builder in builders)
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
					foreach (BuildingRecipe.Builder builder in builders)
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
}
