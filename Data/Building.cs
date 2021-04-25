using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FIOSharp.Data
{
	public struct Building
	{
		#region expertise constants
		public const string EXPERTISE_AGRICULTURE = "AGRICULTURE";
		public const string EXPERTISE_CHEMISTRY = "CHEMISTRY";
		public const string EXPERTISE_CONSTRUCTION = "CONSTRUCTION";
		public const string EXPERTISE_ELECTRONICS = "ELECTRONICS";
		public const string EXPERTISE_FOOD_INDUSTRIES = "FOOD_INDUSTRIES";
		public const string EXPERTISE_FUEL_REFINING = "FUEL_REFINING";
		public const string EXPERTISE_MANUFACTURING = "MANUFACTURING";
		public const string EXPERTISE_METALLURGY = "METALLURGY";
		public const string EXPERTISE_RESOURCE_EXTRACTION = "RESOURCE_EXTRACTION";
		public static readonly string[] EXPERTISE_ALL = {EXPERTISE_AGRICULTURE, EXPERTISE_CHEMISTRY, EXPERTISE_CONSTRUCTION, EXPERTISE_ELECTRONICS, EXPERTISE_FOOD_INDUSTRIES, EXPERTISE_FUEL_REFINING, EXPERTISE_MANUFACTURING, EXPERTISE_METALLURGY, EXPERTISE_RESOURCE_EXTRACTION};
		#endregion expertise constants




		[JsonProperty("Ticker")]
		public readonly string Ticker;
		[JsonProperty("Name")]
		public readonly string Name;
		[JsonProperty("Area")]
		public readonly int Area;
		[JsonProperty("Expertise")]
		public readonly string Expertise;
		[JsonIgnore]
		public readonly IReadOnlyDictionary<PopulationType, int> Populations;
		[JsonIgnore]
		public readonly IReadOnlyDictionary<Material, int> BaseConstructionCost;

		public Building(string ticker, string name, int area, string expertise, IReadOnlyDictionary<PopulationType, int> populations, IReadOnlyDictionary<Material, int> baseConstructionCost)
		{
			Ticker = ticker;
			Name = name;
			Area = area;
			Expertise = expertise;
			Populations = populations;
			BaseConstructionCost = baseConstructionCost;
		}

		/// <summary>
		/// This is for deserializing our unified building json. If fetching from the REST API you should instead use the builder
		/// </summary>
		public static Building FromJson(JObject jObject, List<Material> allMaterials)
		{
			try
			{
				string ticker = jObject.GetValue("Ticker").ToObject<string>();
				string name = jObject.GetValue("Name").ToObject<string>();
				int area = jObject.GetValue("Area").ToObject<int>();
				string expertise = jObject.GetValue("Expertise").ToObject<string>();
				Dictionary<PopulationType, int> populations = ((JObject)jObject.GetValue("Populations")).Properties().ToDictionary(property => PopulationType.Parse(property.Name), property => property.Value.ToObject<int>());
				Dictionary<Material, int> constructionCost = ((JObject)jObject.GetValue("ConstructionCosts")).Properties().ToDictionary(property => allMaterials.Where(mat => mat.Ticker.Equals(property.Name, StringComparison.OrdinalIgnoreCase)).First(), property => property.Value.ToObject<int>());
				return new Building(ticker, name, area, expertise, populations, constructionCost);
			}
			catch (Exception ex) when (ex is NullReferenceException || ex is ArgumentException || ex is FormatException || ex is JsonSerializationException)
			{
				throw new JsonSchemaException(null, ex);
			}
		}

		/// <summary>
		/// This is for converting to our unified building json
		/// </summary>
		public JObject ToJson()
		{
			JObject jObject = JObject.FromObject(this);
			jObject.Add("Populations", JObject.FromObject(Populations.ToDictionary(pair => pair.Key.Name, pair => pair.Value)));
			jObject.Add("ConstructionCosts", JObject.FromObject(BaseConstructionCost.ToDictionary(pair => pair.Key.Ticker, pair => pair.Value)));
			return jObject;
		}

		//because our building data is fetched from three different endpoints it gets assembled slowly and out of order, hence a builder
		public class Builder
		{
			public string Ticker { get; set; }
			public string Name { get; set; }
			public int Area { get; set; }
			public string Expertise { get; set; }

			public Builder(string ticker, string name, int area, string expertise)
			{
				Ticker = ticker;
				Name = name;
				Area = area;
				Expertise = expertise;
			}

			private Dictionary<PopulationType, int> Populations { get; } = new Dictionary<PopulationType, int>();
			private Dictionary<Material, int> BaseConstructionCost { get; } = new Dictionary<Material, int>();

			

			public static Builder Create(string ticker, string name, int area, string expertise)
			{
				return new Builder(ticker, name, area, expertise);
			}

			public Builder setPopulation(PopulationType populationType, int count)
			{
				if(count == 0)
				{
					Populations.Remove(populationType);
					return this;
				}
				if (Populations.ContainsKey(populationType))
				{
					Populations[populationType] = count;
				}
				else
				{
					Populations.Add(populationType, count);
				}
				return this;
			}

			public Builder setPopulations((PopulationType populationType, int count)[] pops)
			{
				foreach(var pop in pops)
				{
					setPopulation(pop.populationType, pop.count);
				}
				return this;
			}

			public Builder setConstructionMaterial(Material material, int count)
			{
				if(count == 0)
				{
					BaseConstructionCost.Remove(material);
					return this;
				}
				if (BaseConstructionCost.ContainsKey(material))
				{
					BaseConstructionCost[material] = count;
				}
				else
				{
					BaseConstructionCost.Add(material, count);
				}
				return this;
			}

			public Builder setConstructionMaterials((Material material, int count)[] mats)
			{
				foreach(var mat in mats)
				{
					setConstructionMaterial(mat.material, mat.count);
				}
				return this;
			}

			public Building Build()
			{
				return new Building(Ticker, Name, Area, Expertise, Populations, BaseConstructionCost);
			}
		}
	}
}
