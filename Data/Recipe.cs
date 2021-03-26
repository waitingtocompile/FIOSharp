using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FIOSharp.Data
{
	public class Recipe
	{
		//unique identifier for that particular recipe variant
		public readonly string Name;
		//base duration in seconds
		public readonly int Duration;
		public readonly Building Building;
		
		
		public readonly IReadOnlyDictionary<Material, int> Inputs;
		public readonly IReadOnlyDictionary<Material, int> Outputs;

		public Recipe(string key, Building building, int duration, IReadOnlyDictionary<Material, int> inputs, IReadOnlyDictionary<Material, int> outputs)
		{
			Name = key;
			Building = building;
			Duration = duration;
			Inputs = inputs;
			Outputs = outputs;
		}

		
		public static Recipe FromJson(JObject jObject, List<Material> allMaterials, List<Building> allBuildings)
		{
			string buildingTicker;
			
			try
			{
				buildingTicker = jObject.GetValue("BuildingTicker").ToObject<string>();
				
			}
			catch(Exception ex) when (ex is NullReferenceException || ex is ArgumentException || ex is FormatException)
			{
				throw new JsonSchemaException("Expected entry BuildingName not found or improperly formatted");
			}

			Building building;
			try
			{
				building = allBuildings.Where(building => building.Ticker.Equals(buildingTicker)).First();
			}
			catch(InvalidOperationException)
			{
				throw new ArgumentException($"Recived incomplete building list, missing {buildingTicker}");
			}


			string name;
			try
			{
				name = jObject.GetValue("RecipeName").ToObject<string>();
			}
			catch (Exception ex) when (ex is NullReferenceException || ex is ArgumentException || ex is FormatException)
			{
				throw new JsonSchemaException("Expected entry RecipeName not found or improperly formatted");
			}

			int duration;
			try
			{
				if (jObject.ContainsKey("TimeMs"))
				{
					//the format from oracle is in milliseconds.
					duration = jObject.GetValue("TimeMs").ToObject<int>() / 1000;
				}
				else if (jObject.ContainsKey("Duration"))
				{
					duration = jObject.GetValue("Duration").ToObject<int>();
				}
				else throw new JsonSchemaException("Expected entry Duration or TimeMs not found");
			}
			catch (Exception ex) when (ex is NullReferenceException || ex is ArgumentException || ex is FormatException)
			{
				//the durations weren't numbers
				throw new JsonSchemaException("Duration information was not formatted as a number");
			}

			Dictionary<Material, int> inputs;
			try
			{
				JArray inputArray = (JArray)jObject.GetValue("Input");
				inputs = inputArray==null?new Dictionary<Material, int>(): ReadInputOutputJson(inputArray, allMaterials);
			}
			catch(InvalidCastException)
			{
				throw new JsonSchemaException("Input in invalid format, expected an array");
			}

			Dictionary<Material, int> outputs;
			try
			{
				JArray outputArray = (JArray)jObject.GetValue("Outputs");
				outputs = outputArray== null ? new Dictionary<Material, int>() : ReadInputOutputJson(outputArray, allMaterials);
			}
			catch (InvalidCastException)
			{
				throw new JsonSchemaException("Output in invalid format, expected an array");
			}

			return new Recipe(name, building, duration, inputs, outputs);
		}


		private static Dictionary<Material, int> ReadInputOutputJson(JArray jArray, List<Material> allMaterials)
		{
			try
			{
				return jArray.Select(token => (JObject)token).ToDictionary(jObject =>
				{
					string ticker = jObject.GetValue("Ticker").ToObject<string>();
					try
					{
						return allMaterials.Where(mat => mat.Ticker.Equals(ticker)).First();
					}
					catch(InvalidOperationException)
					{
						throw new ArgumentException($"Incomplete material list, missing{ticker}");
					}

				}, jObject=> jObject.GetValue("Amount").ToObject<int>());
			}
			catch (InvalidCastException)
			{
				throw new JsonSchemaException("input or output entry in invalid format, expected an object");
			}
			catch (Exception ex) when (ex is FormatException || ex is ArgumentException || ex is NullReferenceException)
			{
				throw new JsonSchemaException("input or output amount was not a number");
			}
		}

		public class Builder{
			public string Key;
			public Building Building;
			public int Duration;
			private Dictionary<Material, int> Inputs = new Dictionary<Material, int>();
			private Dictionary<Material, int> Outputs = new Dictionary<Material, int>();

			public Builder(string key, Building building, int duration)
			{
				Key = key;
				Building = building;
				Duration = duration;
			}

			public Builder SetInput(Material material, int amount)
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

			public Builder SetOutput(Material material, int amount)
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

			public Recipe Build()
			{
				return new Recipe(Key, Building, Duration, Inputs, Outputs);
			}
		}
	}
}
