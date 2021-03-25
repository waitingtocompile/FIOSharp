using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
		public double Previous => previous;
		[JsonIgnore]
		public double Price => price;
		[JsonIgnore]
		public double High => high;
		[JsonIgnore]
		public double Low => low;
		[JsonIgnore]
		public double Ask => ask;
		[JsonIgnore]
		public int AskCount => askCount;
		[JsonIgnore]
		public double Bid => bid;
		[JsonIgnore]
		public int BidCount => bidCount;
		[JsonIgnore]
		public int Supply => supply;
		[JsonIgnore]
		public int Demand => demand;
		[JsonIgnore]
		public int Traded => traded;
		[JsonIgnore]
		public double TradeVolume => tradeVolume;
		[JsonIgnore]
		public double PriceAverage => priceAverage;
		[JsonIgnore]
		public double NarrowBandHigh => narrowBandHigh;
		[JsonIgnore]
		public double NarrowBandLow => narrowBandLow;
		[JsonIgnore]
		public double WideBandHigh => wideBandHigh;
		[JsonIgnore]
		public double WideBandLow => wideBandLow;
		[JsonIgnore]
		public double MMBuy => mMBuy;
		[JsonIgnore]
		public double MMSell => mMSell;
		#endregion

		#region general json properties
		[JsonProperty("Previous", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly double previous = -1;
		[JsonProperty("Price", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly double price = -1;
		[JsonProperty("High", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly double high = -1;
		[JsonProperty("Low", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly double low = -1;
		[JsonProperty("Ask", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly double ask = -1;
		[JsonProperty("AskCount", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly int askCount = -1;
		[JsonProperty("Bid", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly double bid = -1;
		[JsonProperty("BidCount", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]	
		private readonly int bidCount = -1;
		[JsonProperty("Supply", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]		
		private readonly int supply = -1;
		[JsonProperty("Demand", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly int demand = -1;
		[JsonProperty("Traded", Required = Required.Always)]
		private readonly int traded;
		[JsonProperty("VolumeAmount", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly double tradeVolume = 0;
		[JsonProperty("PriceAverage", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]		
		private readonly double priceAverage = -1;
		[JsonProperty("NarrowPriceBandHigh", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]		
		private readonly double narrowBandHigh = -1;
		[JsonProperty("NarrowPriceBandLow", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]		
		private readonly double narrowBandLow = -1;
		[JsonProperty("WidePriceBandHigh", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]		
		private readonly double wideBandHigh = -1;
		[JsonProperty("WidePriceBandLow", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly double wideBandLow = -1;
		[JsonProperty("MMBuy", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		private readonly double mMBuy = -1;
		[JsonProperty("MMSell", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		
		private readonly double mMSell = -1;
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
