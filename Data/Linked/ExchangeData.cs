using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace FIOSharp.Data.Linked
{
	public class ExchangeData
	{
		[JsonProperty("ExchangeName")]
		public readonly string Name;
		[JsonProperty("ExchangeCode")]
		public readonly string Ticker;
		[JsonProperty("CurrencyCode")]
		public readonly string Currency;
		[JsonProperty("CurrencyName")]
		public readonly string CurrencyName;
		[JsonProperty("LocationName")]
		public readonly string LocationName;
		[JsonProperty("LocationNaturalId")]
		public readonly string LocationID;

		public IReadOnlyDictionary<Material, ExchangeEntry> ExchangeEntries => _exchangeEntries;
		private Dictionary<Material, ExchangeEntry> _exchangeEntries = new Dictionary<Material, ExchangeEntry>();

		public string GetComexMaterialCode(string materialTicker)
		{
			return materialTicker + "." + Ticker;
		}

		public void UpdateCommodityOrders(List<Material> materialsToUpdate, FnarOracleClient oracleClient)
		{
			foreach((Material material, ExchangeEntry entry) in materialsToUpdate.Where(mat => !mat.Ticker.Equals("CMK")).Select(material => (material, oracleClient.GetEntryForExchange(this, material))))
			{
				if (_exchangeEntries.ContainsKey(material))
				{
					_exchangeEntries[material] = entry;
				} else
				{
					_exchangeEntries.Add(material, entry);
				}
			}
		}
		
	}
}
