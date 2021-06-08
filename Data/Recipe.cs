using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace FIOSharp.Data
{
	public class Recipe : IEquatable<Recipe>
	{
		//unique identifier for that particular recipe variant
		public readonly string Name;
		//base duration in seconds
		public readonly int Duration;
		public readonly Building Building;
		
		
		public readonly IReadOnlyDictionary<Material, int> Inputs;
		public readonly IReadOnlyDictionary<Material, int> Outputs;

		private int? cachedHash;

		public Recipe(string key, Building building, int duration, IReadOnlyDictionary<Material, int> inputs, IReadOnlyDictionary<Material, int> outputs)
		{
			Name = key;
			Building = building;
			Duration = duration;
			Inputs = inputs;
			Outputs = outputs;
		}

		#region equality bits
		public bool Equals([AllowNull] Recipe other)
		{
			if (other is null) return false;
			if (Building != other.Building) return false;
			if (Inputs.Count != other.Inputs.Count || Outputs.Count != other.Outputs.Count) return false;
			if (!Inputs.All(pair => other.Inputs.ContainsKey(pair.Key) && other.Inputs[pair.Key] == pair.Value)) return false;
			if (!Outputs.All(pair => other.Outputs.ContainsKey(pair.Key) && other.Outputs[pair.Key] == pair.Value)) return false;

			return true;
		}

		public override bool Equals(object o)
		{
			if (o is Recipe recipe) return Equals(recipe);
			return false;
		}

		public override int GetHashCode()
		{
			if (cachedHash.HasValue) return cachedHash.Value;
			cachedHash = HashCode.Combine(Building, inputsHash(), outputsHash());

			return cachedHash.Value;
		}

		public int inputsHash()
		{
			KeyValuePair<Material, int>[] pairs = Inputs.ToArray();
			Array.Sort(pairs, Comparer<KeyValuePair<Material, int>>.Create((pair1, pair2) => pair1.Key.Name.CompareTo(pair2.Key.Name)));
			var hash = 17;
			for(int i = 0; i < pairs.Length; i++)
			{
				hash = hash * 31 + pairs[i].Key.GetHashCode();
				hash = hash * 31 + pairs[i].Value;
			}

			return hash;
		}

		public int outputsHash()
		{
			KeyValuePair<Material, int>[] pairs = Outputs.ToArray();
			Array.Sort(pairs, Comparer<KeyValuePair<Material, int>>.Create((pair1, pair2) => pair1.Key.Name.CompareTo(pair2.Key.Name)));
			var hash = 17;
			for (int i = 0; i < pairs.Length; i++)
			{
				hash = hash * 31 + pairs[i].Key.GetHashCode();
				hash = hash * 31 + pairs[i].Value;
			}

			return hash;
		}

		public static bool operator ==(Recipe recipe1, Recipe recipe2)
		{
			if (recipe1 is null) return recipe2 is null;
			return recipe1.Equals(recipe2);
		}

		public static bool operator !=(Recipe recipe1, Recipe recipe2)
		{
			if (recipe1 is null) return !(recipe2 is null);
			return !recipe1.Equals(recipe2);
		}
		#endregion


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
					//our format is in seconds
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
				JArray inputArray = (JArray)jObject.GetValue("Inputs");
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

		public JObject ToJson()
		{
			JObject jObject = new JObject();
			jObject.Add("RecipeName", Name);
			jObject.Add("Duration", Duration);
			jObject.Add("BuildingTicker", Building.Ticker);
			JArray inputsArray = new JArray();
			foreach(Material mat in Inputs.Keys)
			{
				JObject elem = new JObject();
				elem.Add("Ticker", mat.Ticker);
				elem.Add("Amount", Inputs[mat]);
				inputsArray.Add(elem);
			}
			jObject.Add("Inputs", inputsArray);
			JArray outputsArray = new JArray();
			foreach (Material mat in Outputs.Keys)
			{
				JObject elem = new JObject();
				elem.Add("Ticker", mat.Ticker);
				elem.Add("Amount", Outputs[mat]);
				outputsArray.Add(elem);
			}
			jObject.Add("Outputs", outputsArray);
			return jObject;
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
