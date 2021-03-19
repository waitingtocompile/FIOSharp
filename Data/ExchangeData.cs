using Newtonsoft.Json;
using System.Collections.Generic;

namespace FIOSharp.Data
{
	public class ExchangeData
	{
		[JsonProperty("ExchangeName")]
		[JsonRequired]
		public readonly string Name;
		[JsonProperty("ExchangeCode")]
		[JsonRequired]
		public readonly string Ticker;
		[JsonProperty("CurrencyCode")]
		[JsonRequired]
		public readonly string Currency;
		[JsonProperty("CurrencyName")]
		[JsonRequired]
		public readonly string CurrencyName;
		[JsonProperty("LocationName")]
		[JsonRequired]
		public readonly string LocationName;
		[JsonRequired]
		[JsonProperty("LocationNaturalId")]
		public readonly string LocationID;

		[JsonIgnore]
		public IReadOnlyDictionary<Material, ExchangeEntry> ExchangeEntries => _exchangeEntries;
		[JsonIgnore]
		private Dictionary<Material, ExchangeEntry> _exchangeEntries = new Dictionary<Material, ExchangeEntry>();

		public string GetComexMaterialCode(string materialTicker)
		{
			return materialTicker + "." + Ticker;
		}


		internal void UpdateWithCommodityEntry(ExchangeEntry entry)
		{
			_exchangeEntries[entry.Material] = entry;
		}
	}
}
