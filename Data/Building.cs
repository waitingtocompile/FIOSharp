using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace FIOSharp.Data
{
	public struct Building
	{
		public readonly string Ticker;
		public readonly string Name;
		public readonly int Area;
		public readonly string Expertise;
		
		public readonly IReadOnlyDictionary<PopulationType, int> Populations;
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
