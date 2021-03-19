using System;
using System.Diagnostics.CodeAnalysis;

namespace FIOSharp.Data
{
	public class PopulationType : IEquatable<string>
	{
		public static readonly PopulationType PIONEER = new PopulationType("Pioneer");
		public static readonly PopulationType SETTLER = new PopulationType("Settler");
		public static readonly PopulationType TECHNITIAN = new PopulationType("Technitian");
		public static readonly PopulationType ENGINEER = new PopulationType("Engineer");
		public static readonly PopulationType SCIENTIST = new PopulationType("Scientist");
		public static PopulationType[] ALL { get
			{
				if(_all == null)
				{
					_all = new PopulationType[] { PIONEER, SETTLER, TECHNITIAN, ENGINEER, SCIENTIST };
				}
				return _all;
			} }
		private static PopulationType[] _all;

		//Pioneer, Settler, Technician, Engineer, Scientist
		public readonly string Name;

		private PopulationType(string name)
		{
			Name = name;
		}


		public static PopulationType Parse(string s)
		{
			foreach(PopulationType population in ALL)
			{
				if (population.Equals(s)) return population;
			}
			throw new ArgumentException($"Tried to parse impossible population type: {s}");
		}

		public bool Equals([AllowNull] string other)
		{
			return StringComparer.InvariantCultureIgnoreCase.Equals(other, Name);
		}
	}
}
