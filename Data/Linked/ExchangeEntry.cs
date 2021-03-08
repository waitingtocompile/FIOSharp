using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FIOSharp.Data.Linked
{
	//Contains data on a given commodity for each currency type
	public class ExchangeEntry
	{
		public IReadOnlyList<ExchangeOrder> BuyOrders { get; private set; }
		public IReadOnlyList<ExchangeOrder> SellOrders { get; private set; }
		public Material Material { get; private set; }
		public ExchangeData Exchange { get; private set; }

		[JsonProperty("Timestamp")]
		public readonly DateTime Timestamp;

		[JsonProperty("Previous", NullValueHandling = NullValueHandling.Ignore)]
		public readonly double Previous = -1;
		[JsonProperty("Price", NullValueHandling = NullValueHandling.Ignore)]
		public readonly double Price = -1;
		[JsonProperty("High", NullValueHandling = NullValueHandling.Ignore)]
		public readonly double High = -1;
		[JsonProperty("Low", NullValueHandling = NullValueHandling.Ignore)]
		public readonly double Low = -1;
		[JsonProperty("Ask", NullValueHandling = NullValueHandling.Ignore)]
		public readonly double Ask = -1;
		[JsonProperty("AskCount", NullValueHandling = NullValueHandling.Ignore)]
		public readonly int AskCount = -1;
		[JsonProperty("Bid", NullValueHandling = NullValueHandling.Ignore)]
		public readonly double Bid = -1;
		[JsonProperty("BidCount", NullValueHandling = NullValueHandling.Ignore)]
		public readonly int BidCount = -1;
		[JsonProperty("Supply", NullValueHandling = NullValueHandling.Ignore)]
		public readonly int Supply = -1;
		[JsonProperty("Demand", NullValueHandling = NullValueHandling.Ignore)]
		public readonly int Demand = -1;
		[JsonProperty("Traded")]
		public readonly int Traded;
		[JsonProperty("Volume")]
		public readonly double TradeVolume;
		[JsonProperty("PriceAverage", NullValueHandling = NullValueHandling.Ignore)]
		public readonly double PriceAverage = -1;
		[JsonProperty("NarrowPriceBandHigh", NullValueHandling = NullValueHandling.Ignore)]
		public readonly double NarrowBandHigh = -1;
		[JsonProperty("NarrowPriceBandLow", NullValueHandling = NullValueHandling.Ignore)]
		public readonly double NarrowBandLow = -1;
		[JsonProperty("WidePriceBandHigh", NullValueHandling = NullValueHandling.Ignore)]
		public readonly double WideBandHigh = -1;
		[JsonProperty("WidePriceBandLow", NullValueHandling = NullValueHandling.Ignore)]
		public readonly double WideBandLow = -1;
		[JsonProperty("MMBuy", NullValueHandling = NullValueHandling.Ignore)]
		public readonly double MMBuy = -1;
		[JsonProperty("MMSell", NullValueHandling = NullValueHandling.Ignore)]
		public readonly double MMSell = -1;

		public static ExchangeEntry FromJson(JObject jObject, Material material, ExchangeData exchange)
		{
			ExchangeEntry entry = jObject.ToObject<ExchangeEntry>();
			entry.BuyOrders = ((JArray)jObject.GetValue("BuyingOrders")).Select(token => token.ToObject<ExchangeOrder>()).ToList();
			entry.SellOrders = ((JArray)jObject.GetValue("SellingOrders")).Select(token => token.ToObject<ExchangeOrder>()).ToList();
			entry.Exchange = exchange;
			entry.Material = material;
			return entry;
		}
	}
}
