using System;
using System.Collections.Generic;
using System.Text;

namespace FIOSharp
{
	///<summary>
	/// This is general purpose exception to be used when encountering json that not match with our expected schema.
	/// It's mostly for use by our custom serializers, and data sources should not generally return it directly
	/// </summary>
	public class JsonSchemaException : Exception
	{
		public JsonSchemaException(string message = null, Exception inner = null) : base(message, inner) { }
	}
}
