using FIOSharp.Data;
using System.Collections.Generic;

namespace FIOSharp
{
	/// <summary>
	/// Supplies fixed data such as materials or buildings, but cannot supply variable data such as exchange information
	/// </summary>
	public interface IFixedDataSource
	{
		public List<Material> GetMaterials();
		/// <summary>
		/// Try to get a list of all buildings. This can result in cascading data access.
		/// To prevent cascading, pass a list of all materials.
		/// </summary>
		public List<Building> GetBuildings(List<Material> allMaterials = null);
		/// <summary>
		/// Try to get a list of all recipes. This can result in cascading data access.
		/// To prevent cascading, pass a list of all materials and a list of all buildings
		/// </summary>
		public List<Recipe> GetRecipes(List<Material> allMaterials = null, List<Building> buildings = null);
		
		/// <summary>
		/// Try to get the consumable requirements for each population type. This can result in cascading data access.
		/// To prevent cascading, pass a list of all materials
		/// </summary>
		public List<WorkforceRequirement> GetWorkforceRequirements(List<Material> allMaterials = null);
	}
}
