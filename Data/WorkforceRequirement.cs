﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FIOSharp.Data
{
	public struct WorkforceRequirement
	{
		public readonly PopulationType PopulationType;
		//count indicates the amount consumed per day by 100 workers of that type
		public readonly IReadOnlyDictionary<Material, (double count, bool isRequired)> Requirements;

		public WorkforceRequirement(PopulationType populationType, IReadOnlyDictionary<Material, (double count, bool isOptional)> requirements)
		{
			PopulationType = populationType;
			Requirements = requirements;
		}

		public static WorkforceRequirement FromJson(JObject jObject, List<Material> allMaterials)
		{
			PopulationType populationType;
			try
			{
				populationType = PopulationType.Parse(jObject.GetValue("WorkforceType").ToObject<string>());
			} catch (Exception ex) when (ex is NullReferenceException || ex is ArgumentException || ex is FormatException)
			{
				throw new JsonSchemaException("Missing or invalid parameter WorkforceType");
			}

			JArray needsArray;
			try
			{
				needsArray = (JArray)jObject.GetValue("Needs");
				if (needsArray == null) throw new JsonSchemaException("Missing Parameter Needs");
			}
			catch (InvalidCastException)
			{
				throw new JsonSchemaException("Invalid format for parameter Needs");
			}

			Dictionary<Material, (double count, bool isOptiona)> dict = new Dictionary<Material, (double count, bool isOptiona)>();

			foreach(JToken token in needsArray)
			{
				JObject needsObject;
				string materialTicker;
				double amount;
				try
				{
					needsObject = (JObject)token;
					materialTicker = needsObject.GetValue("MaterialTicker").ToObject<string>();
					amount = needsObject.GetValue("Amount").ToObject<double>();
				}
				catch (Exception ex) when (ex is NullReferenceException || ex is ArgumentException || ex is FormatException || ex is InvalidCastException)
				{
					throw new JsonSchemaException("Invalid format for needs element");
				}

				Material material;
				try
				{
					material = allMaterials.Where(mat => mat.Ticker.Equals(materialTicker)).First();
				} catch (InvalidOperationException)
				{
					throw new ArgumentException($"Incomplete material list provided, missing {materialTicker}");
				}

				bool required;
				if (needsObject.ContainsKey("Required"))
				{
					try
					{
						required = needsObject.GetValue("Required").ToObject<bool>();
					}
					catch (Exception ex) when (ex is ArgumentException || ex is FormatException)
					{
						throw new JsonSchemaException("Invalid format on \"Required\" parameter in needs object");
					}
				}
				else
				{
					//infer if it's a luxury
					required = !material.Category.Contains("luxury", StringComparison.InvariantCultureIgnoreCase);
				}

				dict[material] = (amount, required);
			}

			return new WorkforceRequirement(populationType, dict);
		}
	}
}
