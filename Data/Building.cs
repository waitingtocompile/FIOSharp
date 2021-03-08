using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace FIOSharp.Data
{
	public struct Building
	{
		public readonly string Ticker;
		public readonly string Name;
		public readonly int Area;
		public readonly string Expertise;
		
		public readonly IReadOnlyDictionary<PopulationType, int> Populations;
		public readonly IReadOnlyDictionary<string, int> BaseConstructionCost;

		public Building(string ticker, string name, int area, string expertise, IReadOnlyDictionary<PopulationType, int> populations, IReadOnlyDictionary<string, int> baseConstructionCost)
		{
			Ticker = ticker;
			Name = name;
			Area = area;
			Expertise = expertise;
			Populations = populations;
			BaseConstructionCost = baseConstructionCost;
		}

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
			private Dictionary<string, int> BaseConstructionCost { get; } = new Dictionary<string, int>();

			

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

			public Builder setConstructionMaterial(string materialTicker, int count)
			{
				if(count == 0)
				{
					BaseConstructionCost.Remove(materialTicker);
					return this;
				}
				if (BaseConstructionCost.ContainsKey(materialTicker))
				{
					BaseConstructionCost[materialTicker] = count;
				}
				else
				{
					BaseConstructionCost.Add(materialTicker, count);
				}
				return this;
			}

			public Builder setConstructionMaterials((string materialTicker, int count)[] mats)
			{
				foreach(var mat in mats)
				{
					setConstructionMaterial(mat.materialTicker, mat.count);
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
