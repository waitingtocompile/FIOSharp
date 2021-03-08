

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FIOSharp.Data.Linked
{
	public class ExchangeOrder
	{
		[JsonProperty("CompanyCode")]
		public readonly string CompanyTicker;
		[JsonProperty("ItemCount", NullValueHandling = NullValueHandling.Ignore)]
		public readonly int Count = -1;
		[JsonProperty("ItemCost")]
		public readonly double Price;

		/*
		public ExchangeOrder(string ticker, int count, double price)
		{
			this.CompanyTicker = ticker;
			this.Count = count;
			this.Price = price;
		}

		
		public static ExchangeOrder fromJson(JObject jObject)
		{
			string ticker = jObject.GetValue("CompanyCode").ToObject<string>();
			int count = jObject.GetValue("ItemCount").ToObject<int>();
			double price = jObject.GetValue("ItemCost").ToObject<double>();
			return new ExchangeOrder(ticker, count, price);
		}
		*/
	}
}