using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;

namespace FIOSharp.Data
{
	public struct Material : IEquatable<Material>
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

		public bool Equals([AllowNull] Material other)
		{
			return other.Ticker.Equals(Ticker, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(object o)
		{
			if (o is Material material) return Equals(material);
			return false;
		}

		public override int GetHashCode()
		{
			return Ticker.GetHashCode(StringComparison.OrdinalIgnoreCase);
		}

		public static bool operator ==(Material mat1, Material mat2)
		{
			return mat1.Equals(mat2);
		}

		public static bool operator !=(Material mat1, Material mat2)
		{
			return !mat1.Equals(mat2);
		}
	}
}
