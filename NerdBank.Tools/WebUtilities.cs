using System;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Web.Caching;

namespace NerdBank.Tools
{
	/// <summary>
	/// Summary description for Web.
	/// </summary>
	public class WebUtilities
	{
		private WebUtilities()
		{
		}

		/// <summary>
		/// Sends the contents of a stream to the HttpResponse object and ends the connection.
		/// </summary>
		/// <param name="stream">
		/// The stream to send to the browser.
		/// </param>
		/// <param name="contentType">
		/// The MIME type that a browser would recognize, so it can properly deal
		/// with the incoming stream.
		/// </param>
		/// <param name="filename">
		/// If the file should be downloaded and "open or save as" should pop up 
		/// on the web client computer, specify a filename to suggest to the user.
		/// A full path should NOT be specified here -- just a filename.  Optional.
		/// </param>
		/// <remarks>
		/// The stream is closed after being read to the end.
		/// </remarks>
		public static void StreamToBrowser(Stream stream, string contentType, string filename)
		{
			HttpContext context = HttpContext.Current;
			if( context == null ) throw new InvalidOperationException("No web context detected.  A web request must be in progress.");
			HttpResponse Response = context.Response;
			Response.Clear();
			Response.ContentType = contentType;
			if( filename != null && filename.Length > 0 )
				Response.AppendHeader("Content-Disposition", "attachment; filename=\"" + filename + "\"");
			int b;
			while( (b = stream.ReadByte()) >= 0 )
				Response.OutputStream.WriteByte((byte)b);
			stream.Close();
			Response.End();
		}

		#region Caching functions
		/// <summary>
		/// Loads an object with a given key from the web cache, if available.
		/// </summary>
		/// <param name="key">
		/// The key that the cached item would have been stored under.
		/// </param>
		/// <returns>
		/// The value for that key, if one was found.
		/// </returns>
		public static object LoadFromCache(string key)
		{
			if( HttpContext.Current != null && HttpContext.Current.Cache[key] != null )
				return HttpContext.Current.Cache[key];
			else
				return null;
		}
		/// <summary>
		/// Saves an object under a given key in the web cache.
		/// </summary>
		public static void SaveInCache(string key, object value)
		{
			SaveInCache(key, value, null);
		}
		/// <summary>
		/// Saves an object under a given key in the web cache.
		/// </summary>
		/// <remarks>
		/// If no web context is available, the object is NOT saved in the cache,
		/// and no error is returned.
		/// </remarks>
		public static void SaveInCache(string key, object value, string fileDependency)
		{
			if( key == null || key.Length == 0 ) throw new ArgumentNullException("key");
			if( value == null ) throw new ArgumentNullException("value");

			if( HttpContext.Current == null ) return; // no web context's cache to save into

			CacheDependency dep = null;
			if( fileDependency != null )  
				dep = new CacheDependency(fileDependency);
			HttpContext.Current.Cache.Add(key, value,
				dep, Cache.NoAbsoluteExpiration,Cache.NoSlidingExpiration,
				System.Web.Caching.CacheItemPriority.Default,
				null);
		}		
		#endregion

		/// <summary>
		/// Gets the URL to the site currently within a web request.
		/// Guaranteed to end with a slash.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// Thrown when called not within a web context.
		/// </exception>
		public static Uri CurrentSiteUrl
		{
			get
			{
				HttpContext context = HttpContext.Current;
				if (context == null) throw new InvalidOperationException("No web context.");
				string appPath = context.Request.ApplicationPath;
				if (!appPath.EndsWith("/")) appPath += "/"; // ensure trailing slash
				return new Uri(context.Request.Url, appPath);
			}
		}
	}
}
