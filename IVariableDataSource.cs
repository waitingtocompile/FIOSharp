using FIOSharp.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FIOSharp
{
	/// <summary>
	/// A data source that can provide variable data such as exchange information, in addition to fixed data.
	/// </summary>
	public interface IVariableDataSource : IFixedDataSource
	{
		/// <summary>
		/// Get a List of all Commodity exchanges
		/// </summary>
		public List<ExchangeData> GetExchanges();

		/// <summary>
		/// Get all commodity entries for a given commodity exchange. This can result in cascading data access. To prevent cascading, pass a list of all materials
		/// If you need data for multiple exchanges it will almost always be quicker to use GetEntriesForExchanges
		/// </summary>
		/// <param name="exchange">the exchange to look up data for</param>
		/// <param name="applyToExchange">if the found entries should be used to update our exchange</param>
		public List<ExchangeEntry> GetEntriesForExchange(ExchangeData exchange, List<Material> allMaterials = null, bool applyToExchange = true);
		/// <summary>
		/// Get all commodities for several commodity exchanges. This can result in cascading data access. To prevent cascading, pass a list of all materials
		/// </summary>
		/// <param name="exchanges">the exchanges to look up data for</param>
		/// <param name="applyToExchanges">if the found entries should be used to update our exchanges</param>
		public List<ExchangeEntry> GetEntriesForExchanges(List<ExchangeData> exchanges, List<Material> allMaterials = null, bool applyToExchanges = true);
		/// <summary>
		/// Look up the commodity entry of a given material for a given exchange.
		/// If you need to look up many materials, you should instead be using GetEntriesForExchange where possible
		/// </summary>
		/// <param name="exchange">the exchange to look up data for</param>
		/// <param name="material">the material we are getting data on</param>
		/// <param name="applyToExchange">if the found entry should be used to update our exchange</param>
		public ExchangeEntry GetEntryForExchange(ExchangeData exchange, Material material, bool applyToExchange = true);

		/// <summary>
		/// Get a List of all Commodity exchanges
		/// </summary>
		public Task<List<ExchangeData>> GetExchangesAsync();

		/// <summary>
		/// Get all commodity entries for a given commodity exchange. This can result in cascading data access. To prevent cascading, pass a list of all materials
		/// If you need data for multiple exchanges it will almost always be quicker to use GetEntriesForExchanges
		/// </summary>
		/// <param name="exchange">the exchange to look up data for</param>
		/// <param name="applyToExchange">if the found entries should be used to update our exchange</param>
		public Task<List<ExchangeEntry>> GetEntriesForExchangeAsync(ExchangeData exchange, List<Material> allMaterials = null, bool applyToExchange = true);
		/// <summary>
		/// Get all commodities for several commodity exchanges. This can result in cascading data access. To prevent cascading, pass a list of all materials
		/// </summary>
		/// <param name="exchanges">the exchanges to look up data for</param>
		/// <param name="applyToExchanges">if the found entries should be used to update our exchanges</param>
		public Task<List<ExchangeEntry>> GetEntriesForExchangesAsync(List<ExchangeData> exchanges, List<Material> allMaterials = null, bool applyToExchanges = true);
		/// <summary>
		/// Look up the commodity entry of a given material for a given exchange.
		/// If you need to look up many materials, you should instead be using GetEntriesForExchange where possible
		/// </summary>
		/// <param name="exchange">the exchange to look up data for</param>
		/// <param name="material">the material we are getting data on</param>
		/// <param name="applyToExchange">if the found entry should be used to update our exchange</param>
		public Task<ExchangeEntry> GetEntryForExchangeAsync(ExchangeData exchange, Material material, bool applyToExchange = true);
	}
}
