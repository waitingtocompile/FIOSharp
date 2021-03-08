using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FIOSharp.Data
{
	public struct Material
	{
		[JsonProperty("Name")]
		public readonly string Name;
		[JsonProperty("Ticker")]
		public readonly string Ticker;
		[JsonProperty("Category")]
		public readonly string Category;
		[JsonProperty("Weight")]
		public readonly decimal Mass;
		[JsonProperty("Volume")]
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
