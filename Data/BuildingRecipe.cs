using System;
using System.Collections.Generic;
using System.Text;

namespace FIOSharp.Data
{
	public struct BuildingRecipe
	{
		//unique identifier for that particular recipe variant
		public readonly string Key;
		public readonly string Building;
		//base duration is ??seconds??
		public readonly int Duration;
		public readonly IReadOnlyDictionary<string, int> Inputs;
		public readonly IReadOnlyDictionary<string, int> Outputs;

		public BuildingRecipe(string key, string building, int duration, IReadOnlyDictionary<string, int> inputs, IReadOnlyDictionary<string, int> outputs)
		{
			Key = key;
			Building = building;
			Duration = duration;
			Inputs = inputs;
			Outputs = outputs;
		}

		public class Builder{
			public string Key;
			public string Building;
			public int Duration;
			private Dictionary<string, int> Inputs = new Dictionary<string, int>();
			private Dictionary<string, int> Outputs = new Dictionary<string, int>();

			public Builder(string key, string building, int duration)
			{
				Key = key;
				Building = building;
				Duration = duration;
			}

			public Builder SetInput(string material, int amount)
			{
				if (amount == 0)
				{
					Inputs.Remove(material);
					return this;
				}
				if (Inputs.ContainsKey(material))
				{
					Inputs[material] = amount;
				}
				else
				{
					Inputs.Add(material, amount);
				}
				return this;
			}

			public Builder SetOutput(string material, int amount)
			{
				if (amount == 0)
				{
					Outputs.Remove(material);
					return this;
				}
				if (Outputs.ContainsKey(material))
				{
					Outputs[material] = amount;
				}
				else
				{
					Outputs.Add(material, amount);
				}
				return this;
			}

			public BuildingRecipe Build()
			{
				return new BuildingRecipe(Key, Building, Duration, Inputs, Outputs);
			}
		}
	}
}
