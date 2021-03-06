using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FIOSharp.Data
{
	//Contains data on a given commodity at a given exchange
	public class ExchangeEntry
	{
		public IReadOnlyList<ExchangeOrder> BuyOrders { get; private set; }
		public IReadOnlyList<ExchangeOrder> SellOrders { get; private set; }
		public Material Material { get; private set; }
		public string Ticker => Exchange.GetComexMaterialCode(Material.Ticker);
		public ExchangeData Exchange { get; private set; }

		[JsonProperty("Timestamp", Required = Required.Always)]
		public readonly DateTime Timestamp;

		#region sneaky property wrappers
		//this is a miserable gross hack so that it'll play nice with both Json.Net (which doesn't like properties) and winforms (which only likes properties)
		[JsonIgnore]
		public decimal? Previous => previous;
		[JsonIgnore]
		public decimal? Price => price;
		[JsonIgnore]
		public decimal? High => high;
		[JsonIgnore]
		public decimal? Low => low;
		[JsonIgnore]
		public decimal? Ask => ask;
		[JsonIgnore]
		public int? AskCount => askCount;
		[JsonIgnore]
		public decimal? Bid => bid;
		[JsonIgnore]
		public int? BidCount => bidCount;
		[JsonIgnore]
		public int? Supply => supply;
		[JsonIgnore]
		public int? Demand => demand;
		[JsonIgnore]
		public int? Traded => traded;
		[JsonIgnore]
		public decimal? TradeVolume => tradeVolume;
		[JsonIgnore]
		public decimal? PriceAverage => priceAverage;
		[JsonIgnore]
		public decimal? NarrowBandHigh => narrowBandHigh;
		[JsonIgnore]
		public decimal? NarrowBandLow => narrowBandLow;
		[JsonIgnore]
		public decimal? WideBandHigh => wideBandHigh;
		[JsonIgnore]
		public decimal? WideBandLow => wideBandLow;
		[JsonIgnore]
		public decimal? MMBuy => mMBuy;
		[JsonIgnore]
		public decimal? MMSell => mMSell;
		#endregion

		#region general json properties
		[JsonProperty("Previous", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly decimal? previous = null;
		[JsonProperty("Price", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly decimal? price = null;
		[JsonProperty("High", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly decimal? high = null;
		[JsonProperty("Low", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly decimal? low = null;
		[JsonProperty("Ask", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly decimal? ask = null;
		[JsonProperty("AskCount", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly int? askCount = null;
		[JsonProperty("Bid", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly decimal? bid = null;
		[JsonProperty("BidCount", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]	
		private readonly int? bidCount = null;
		[JsonProperty("Supply", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]		
		private readonly int? supply = null;
		[JsonProperty("Demand", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly int? demand = null;
		[JsonProperty("Traded", Required = Required.Always)]
		private readonly int? traded;
		[JsonProperty("VolumeAmount", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly decimal? tradeVolume = 0;
		[JsonProperty("PriceAverage", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]		
		private readonly decimal? priceAverage = null;
		[JsonProperty("NarrowPriceBandHigh", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]		
		private readonly decimal? narrowBandHigh = null;
		[JsonProperty("NarrowPriceBandLow", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]		
		private readonly decimal? narrowBandLow = null;
		[JsonProperty("WidePriceBandHigh", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]		
		private readonly decimal? wideBandHigh = null;
		[JsonProperty("WidePriceBandLow", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly decimal? wideBandLow = null;
		[JsonProperty("MMBuy", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly decimal? mMBuy = null;
		[JsonProperty("MMSell", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly decimal? mMSell = null;
		#endregion

		public static ExchangeEntry FromJson(JObject jObject, List<Material> allMaterials, ExchangeData exchange, bool tryApplyToExchange = true)
		{
			Material material;
			try
			{
				material = allMaterials.Where(mat => mat.Ticker.Equals(jObject.GetValue("MaterialTicker").ToObject<string>())).First();
			}
			catch(InvalidOperationException)
			{
				throw new ArgumentException($"Incomplete material list provided, missing{jObject.GetValue("MaterialTicker")}");
			}
			catch (Exception ex) when (ex is NullReferenceException || ex is ArgumentException || ex is FormatException)
			{
				throw new JsonSchemaException("Material ticker information improperly formatted or missing", ex);
			}
			return FromJson(jObject, material, exchange, tryApplyToExchange);
		}

		public static ExchangeEntry FromJson(JObject jObject, Material material, ExchangeData exchange, bool tryApplyToExchange = true)
		{
			ExchangeEntry entry;
			try
			{
				entry = jObject.ToObject<ExchangeEntry>();
				entry.BuyOrders = ((JArray)jObject.GetValue("BuyingOrders")).Select(token => token.ToObject<ExchangeOrder>()).ToList();
				entry.SellOrders = ((JArray)jObject.GetValue("SellingOrders")).Select(token => token.ToObject<ExchangeOrder>()).ToList();
				entry.Exchange = exchange;
				entry.Material = material;
			}
			catch (Exception ex) when (ex is NullReferenceException || ex is ArgumentException || ex is FormatException || ex is JsonSerializationException)
			{
				//This is lacklustre and undescriptive. Maybe break up the try/catch into one for each thing?
				throw new JsonSchemaException("Exchange entry information is improperly formatted", ex);
			}

			if (tryApplyToExchange && exchange != null)
			{
				exchange.UpdateWithCommodityEntry(entry);
			}
			return entry;
		}
	}
}
