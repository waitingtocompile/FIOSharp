using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FIOSharp.Data
{
	public struct Material
	{
		[JsonProperty("Name")]
		[JsonRequired]
		public readonly string Name;
		[JsonProperty("Ticker")]
		[JsonRequired]
		public readonly string Ticker;
		[JsonProperty("Category")]
		[JsonRequired]
		public readonly string Category;
		[JsonProperty("Weight")]
		[JsonRequired]
		public readonly decimal Mass;
		[JsonProperty("Volume")]
		[JsonRequired]
		public readonly decimal Volume;
				
		public static readonly Material NULL_MATERIAL = new Material( "", "", "", 0.0m, 0.0m );

		private Material(string name, string ticker, string category, decimal mass, decimal volume)
		{
			Name = name;
			Ticker = ticker;
			Category = category;
			Mass = mass;
			Volume = volume;
		}
	}
}
