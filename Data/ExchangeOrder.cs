using Newtonsoft.Json;

namespace FIOSharp.Data
{
	public class ExchangeOrder
	{
		[JsonProperty("CompanyCode", Required = Required.AllowNull)]
		public readonly string CompanyTicker;
		[JsonProperty("ItemCount", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		public readonly int Count = -1;
		[JsonProperty("ItemCost", Required = Required.Always)]
		public readonly double Price;
	}
}