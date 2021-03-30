using System;

namespace FIOSharp
{
	/// <summary>
	/// Indicates that a local data file was improperly formatted or missing, and no fallback was availible.
	/// </summary>
	public class LocalFileException : Exception
	{
		/// <summary>
		/// The absolute file path of the missing or invalid file.
		/// </summary>
		public readonly string FilePath;
		public LocalFileException(string filePath, string message = null, Exception innerException = null) : base(message, innerException)
		{
			FilePath = filePath;
		}

		public LocalFileException(string filePath, Exception innerException = null) : base(null, innerException)
		{
			FilePath = filePath;
		}
	}
}
