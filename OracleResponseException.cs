using System;

namespace FIOSharp
{
	/// <summary>
	/// indicates that the FNAR information oracle has returned data that does not match the format or content we expected.
	/// You should not throw this exception yourself unless you're implementing the wrapping for a new endpoint
	/// If the api call is responsing with an http error, you should be throwing HttpException instead
	/// </summary>
	public class OracleResponseException : Exception
	{
		/// <summary>
		/// The api endpoint that resulted in the invalid data
		/// </summary>
		public string EndpointPath { get; }

		public OracleResponseException(string endpointPath, Exception inner = null):this(endpointPath, null, inner)
		{
			
		}

		public OracleResponseException(string endpointPath, string message, Exception inner = null) : base(message, inner)
		{
			EndpointPath = endpointPath;
		}
	}
}
