using FIOSharp.Data;
using System.Collections.Generic;

namespace FIOSharp
{
	/// <summary>
	/// A data source that can provide variable data such as exchange information, in addition to fixed data.
	/// </summary>
	public interface IVariableDataSource : IFixedDataSource
	{
		public List<ExchangeData> GetExchanges();
		public List<ExchangeEntry> GetEntriesForExchange(ExchangeData exchange, List<Material> allMaterials = null, bool applyToExchange = true);
		public List<ExchangeEntry> GetEntriesForExchanges(List<ExchangeData> exchanges, List<Material> allMaterials = null, bool applyToExchanges = true);
		public ExchangeEntry GetEntryForExchange(ExchangeData exchange, Material material, bool applyToExchange = true);


	}
}
