﻿

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FIOSharp.Data
{
	public class ExchangeOrder
	{
		[JsonProperty("CompanyCode", Required = Required.Always)]
		public readonly string CompanyTicker;
		[JsonProperty("ItemCount", NullValueHandling = NullValueHandling.Ignore, Required = Required.AllowNull)]
		public readonly int Count = -1;
		[JsonProperty("ItemCost", Required = Required.Always)]
		public readonly double Price;
	}
}