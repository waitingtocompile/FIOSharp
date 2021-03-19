

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FIOSharp.Data
{
	public class ExchangeOrder
	{
		[JsonProperty("CompanyCode")]
		[JsonRequired]
		public readonly string CompanyTicker;
		[JsonProperty("ItemCount", NullValueHandling = NullValueHandling.Ignore)]
		[JsonRequired]
		public readonly int Count = -1;
		[JsonProperty("ItemCost")]
		[JsonRequired]
		public readonly double Price;
	}
}