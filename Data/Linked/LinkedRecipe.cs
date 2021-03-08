using System.Collections.Generic;
using System.Linq;

namespace FIOSharp.Data.Linked
{
	public class LinkedRecipe
	{
		public BuildingRecipe UnderlyingRecipe { get; }
		public IReadOnlyDictionary<Material, int> Inputs { get; }
		public IReadOnlyDictionary<Material, int> Outputs { get; }
		public Building Building { get; }
		public string Key => UnderlyingRecipe.Key;
		public int Duration => UnderlyingRecipe.Duration;

		private LinkedRecipe(BuildingRecipe underlyingRecipe, IReadOnlyDictionary<Material, int> inputs, IReadOnlyDictionary<Material, int> outputs, Building building)
		{
			UnderlyingRecipe = underlyingRecipe;
			Inputs = inputs;
			Outputs = outputs;
			Building = building;
		}


		public static List<LinkedRecipe> CreateAll(List<BuildingRecipe> recipes, List<Material> materials, List<Building> buildings)
		{
			string[] errors;
			return CreateAll(recipes, materials, buildings, out errors);
		}

		public static List<LinkedRecipe> CreateAll(List<BuildingRecipe> recipes, List<Material> materials, List<Building> buildings, out string[] errors)
		{
			List<string> foundErrors = new List<string>();

			IEnumerable<LinkedRecipe> foundRecipes = recipes.Select(recipe =>
			{
				string err;
				LinkedRecipe linkedRecipe = Create(recipe, materials, buildings, out err);
				if (err.Length > 0) foundErrors.Add(err);
				return linkedRecipe;
			});

			errors = foundErrors.ToArray();
			return foundRecipes.ToList();
		}

		public static LinkedRecipe Create(BuildingRecipe recipe, List<Material> materials, List<Building> buildings)
		{
			//for when we don't care about the error messages
			string a;
			return Create(recipe, materials, buildings, out a);
		}

		//we're using a factory method so we can return null if things break
		public static LinkedRecipe Create(BuildingRecipe recipe, List<Material> materials, List<Building> buildings, out string error)
		{
			Dictionary<Material, int> inputs = new Dictionary<Material, int>();
			foreach(string inputName in recipe.Inputs.Keys)
			{
				Material foundMaterial = materials.Where(mat => mat.Ticker.Equals(inputName)).FirstOrDefault();
				if (!foundMaterial.Ticker.Equals(inputName))
				{
					//no material was found, error out
					error = "unknown material " + inputName;
					return null;
				}
				inputs.Add(foundMaterial, recipe.Inputs[inputName]);
			}
			Dictionary<Material, int> outputs = new Dictionary<Material, int>();
			foreach (string outputName in recipe.Outputs.Keys)
			{
				Material foundMaterial = materials.Where(mat => mat.Ticker.Equals(outputName)).FirstOrDefault();
				if (!foundMaterial.Ticker.Equals(outputName))
				{
					//no material was found, error out
					error = "unknown material " + outputName;
					return null;
				}
				outputs.Add(foundMaterial, recipe.Outputs[outputName]);
			}


			Building foundBuilding = buildings.Where(building => building.Ticker.Equals(recipe.Building)).FirstOrDefault();
			if(foundBuilding.Ticker != recipe.Building)
			{
				error = "unkown building " + recipe.Building;
				return null;
			}


			error = "";
			return new LinkedRecipe(recipe, inputs, outputs, foundBuilding);
		}
	}
}
