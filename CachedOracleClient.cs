using FIOSharp.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FIOSharp
{
	// this is old legacy junk. It's only here for my personal reference

	/*
	/// <summary>
	/// A special oracle client that can use json caches to reduce API calls for certain kinds of data
	/// </summary>
	public class CachedOracleClient : FnarOracleClient
	{
		public string DataDirectory { get; private set; }

		#region file paths
		private string MaterialsFile => DataDirectory + "/materials.json";
		private string ExchangesFile => DataDirectory + "/exchanges.json";
		private string BuildingsFile => DataDirectory + "/buildings.json";
		private string RecipesFile => DataDirectory + "/recipes.json";
		#endregion

		public CachedOracleClient(string dataDirectory, string APIBaseUrl = "https://rest.fnar.net", bool alwaysAuth = true)
			:base(APIBaseUrl, alwaysAuth)
		{
			DataDirectory = dataDirectory;
		}

		public override List<Material> GetMaterials()
		{
			return GetMaterials(false);
		}

		public List<Material> GetMaterials(bool forceAPI)
		{
			return ListGetter(MaterialsFile, base.GetMaterials, forceAPI);
		}
		public override List<ExchangeData> GetExchanges()
		{
			return GetExchanges(false);
		}

		public List<ExchangeData> GetExchanges(bool forceAPI)
		{
			return ListGetter(ExchangesFile, base.GetExchanges, forceAPI);
		}

		
		private JObject serializeBuilding(Building building)
		{
			JObject jObject = new JObject();
			jObject.Add("Ticker", new JValue(building.Ticker));
			jObject.Add("Name", new JValue(building.Name));
			jObject.Add("Area", new JValue(building.Area));
			jObject.Add("Expertise", new JValue(building.Expertise));

			JArray constructionArray = new JArray();
			foreach(var pair in building.BaseConstructionCost)
			{
				JObject costObject = new JObject();
				costObject.Add("Material", new JValue(pair.Key));
				costObject.Add("Amount", new JValue(pair.Value));
				constructionArray.Add(costObject);
			}
			jObject.Add("ConstructionCost", constructionArray);

			JArray populationsArray = new JArray();
			foreach(var pair in building.Populations)
			{
				JObject populationObject = new JObject();
				populationObject.Add("Population", new JValue(pair.Key.ToString()));
				populationObject.Add("Amount", new JValue(pair.Value));
				populationsArray.Add(populationObject);
			}
			jObject.Add("Populations", populationsArray);

			return jObject;
		}

		private Building deserializeBuilding(JObject jObject)
		{
			Building.Builder builder = jObject.ToObject<Building.Builder>();
			JArray constructionArray = (JArray)jObject.GetValue("ConstructionCost");
			foreach(JObject constructionObject in constructionArray.Select(token => (JObject)token))
			{
				builder.setConstructionMaterial(
					constructionObject.GetValue("Material").ToObject<string>(),
					constructionObject.GetValue("Amount").ToObject<int>());
			}

			JArray populationArray = (JArray)jObject.GetValue("Populations");
			foreach (JObject populationObject in populationArray.Select(token => (JObject)token))
			{
				builder.setPopulation(
					PopulationType.Parse(populationObject.GetValue("Population").ToObject<string>()),
					populationObject.GetValue("Amount").ToObject<int>());
			}

			return builder.Build();
		}

		public List<Building> GetBuildings(bool forceApi)
		{
			return GenericDataGetter(BuildingsFile,
				jArray => jArray.Select(token => deserializeBuilding((JObject) token)).ToList(),
				list =>
				{
					JArray array = new JArray();
					foreach(JObject jObject in list.Select(serializeBuilding))
					{
						array.Add(jObject);
					}
					return array;
				},
				() => base.GetBuildings(true, true),
				forceApi);
		}

		//we ignore the construction cost and population arguments, they do not matter to us, our local schema just doesn't care about them
		public override List<Building> GetBuildings(bool getConstructionCost = true, bool getPopulation = true)
		{
			return GetBuildings(false);
		}

		//todo: recipe serialization and deserialization

		public List<Recipe> GetRecipes(bool forceApi)
		{
			throw new NotImplementedException();
		}

		//like with buildings, we ignore the input and output arguments, we get and store everything
		public List<Recipe> GetRecipes(bool includeInputs = true, bool includeOutputs = true)
		{
			return GetRecipes(false);
		}

		private List<T> ListGetter<T>(string filePath, Func<List<T>> underlyingDataProvider, bool forceAPI)
		{
			return ListGetter<T>(filePath,
				token => token.ToObject<T>(),
				JArray.FromObject,
				underlyingDataProvider,
				forceAPI);
		}

		private List<T> ListGetter<T>(string filePath, Func<JToken, T> fromJson, Func<List<T>, JArray> toJson, Func<List<T>> underlyingDataProvider, bool forceAPI)
		{
			return GenericDataGetter<List<T>, JArray>(filePath,
				jArray => jArray.Select(fromJson).ToList(),
				toJson,
				underlyingDataProvider,
				forceAPI);
		}

		private T GenericDataGetter<T, J>(string filePath, Func<J, T> fromJson, Func<T, J> toJson, Func<T> underlyingDataProvider, bool forceAPI) where J : JToken
		{
			if(!forceAPI && File.Exists(filePath))
			{
				using (StreamReader file = File.OpenText(filePath))
				{
					using (JsonTextReader reader = new JsonTextReader(file))
					{
						J jToken = (J)JToken.ReadFrom(reader);
						return fromJson((J)JToken.ReadFrom(reader));
					}
				}
			}

			T found = underlyingDataProvider();
			using (StreamWriter file = File.CreateText(filePath))
			{
				using (JsonTextWriter writer = new JsonTextWriter(file))
				{
					toJson(found).WriteTo(writer);
				}
			}

			return found;
		}
	}
	*/
}
