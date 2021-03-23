using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FIOSharp.Data
{
	public class ExchangeData
	{
		private object guard = new object();

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
		//we're using a ConcurrentDictionary since it might be edited by asynchronous methods
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
