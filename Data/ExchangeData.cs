using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FIOSharp.Data
{
	public class ExchangeData
	{
		[JsonProperty("ExchangeName")]
		[JsonRequired]
		private readonly string name;
		[JsonProperty("ExchangeCode")]
		[JsonRequired]
		private readonly string ticker;
		[JsonProperty("CurrencyCode")]
		[JsonRequired]
		private readonly string currency;
		[JsonProperty("CurrencyName")]
		[JsonRequired]
		private readonly string currencyName;
		[JsonProperty("LocationName")]
		[JsonRequired]
		private readonly string locationName;
		[JsonRequired]
		[JsonProperty("LocationNaturalId")]
		private readonly string locationID;

		[JsonIgnore]
		public IReadOnlyDictionary<Material, ExchangeEntry> ExchangeEntries => _exchangeEntries;

		[JsonIgnore]
		public string Name => name;
		[JsonIgnore]
		public string Ticker => ticker;
		[JsonIgnore]
		public string Currency => currency;
		[JsonIgnore]
		public string CurrencyName => currencyName;
		[JsonIgnore]
		public string LocationName => locationName;
		[JsonIgnore]
		public string LocationID => locationID;

		//we're using a ConcurrentDictionary since it might be edited by asynchronous methods. There are perfomrance issues, but it'll have to do
		[JsonIgnore]
		private ConcurrentDictionary<Material, ExchangeEntry> _exchangeEntries = new ConcurrentDictionary<Material, ExchangeEntry>();

		public string GetComexMaterialCode(string materialTicker)
		{
			return materialTicker + "." + Ticker;
		}

		/// <summary>
		/// update our exchange data with the entry for a given commodity in a thread safe manner
		/// </summary>
		/// <param name="entry">the entry to be applied</param>
		internal void UpdateWithCommodityEntry(ExchangeEntry entry)
		{
			if (entry.Exchange != this) throw new ArgumentException(nameof(entry), "Entry passed was for a different exchange");
			_exchangeEntries.AddOrUpdate(entry.Material, entry, (key, value) => entry);
		}
	}
}
