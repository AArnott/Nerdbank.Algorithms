using System;

namespace NerdBank.Tools
{
	/// <summary>
	/// A custom method attribute used to authorize any method
	/// to be called from the web directly through an Embed class.
	/// A WebDirect method:
	///  x Must be declared public static
	///  x May return a Stream object with data for the browser
	///  x May have an "out string contentType" argument to set the type of streaming data
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class WebDirectAttribute : Attribute
	{
		/// <summary>
		/// Constructs the WebDirect attribute object.
		/// </summary>
		public WebDirectAttribute()
		{
		}
	}
}
