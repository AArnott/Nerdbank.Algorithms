using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Text.RegularExpressions;
using System.Reflection;
using System.IO;
using System.Collections.Specialized;
using System.Threading;
using System.Globalization;

namespace NerdBank.Tools
{
	/// <summary>
	/// Summary description for WebDirect.
	/// </summary>
	/// <example>
	/// An example URL for this call
	/// Assembly: NAssess
	/// Fully-qualified class name: NAssess.Engines.Reporting.Graphs.GraphWindow
	/// Method name: Render
	/// Arguments: (string sFirstName, out outputType)
	/// http://uri.byu.edu/WebDirect.aspx/nassess/NAssess.Engines.Reporting.Graphs.GraphWindow/Render?sFirstName=Andrew
	/// </example>
	public class WebDirect : IHttpHandler
	{
		protected const string regexString = @"\A(?<assembly>[\w\s]+)/(?<className>[^/]+)/(?<methodName>\w+)(?:&(?<params>.+))?\z";
		protected Match regexMatch;

		#region IHttpHandler Members

		public bool IsReusable { get { return true; } }

		public void ProcessRequest(HttpContext context)
		{
			regexMatch = Regex.Match(HttpUtility.UrlDecode(context.Request.QueryString.ToString()), regexString);
			if (!regexMatch.Success)
				throw new ArgumentException("Bad path requested.");

			// Let the called method know what culture the request is coming from.
			if (context.Request.UserLanguages != null)
			{
				Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(context.Request.UserLanguages[0]);
				Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(context.Request.UserLanguages[0]);
			}

			Assembly assembly = GetAssemblyFromRequest();
			MethodInfo mi = GetMethodInAssembly(assembly);
			object[] paramValues = GetParameters(context, mi);

			object result = mi.Invoke(null, paramValues);
			CheckOutParameters(context, mi, paramValues);

			if (result != null && result.GetType().IsSubclassOf(typeof(Stream)))
			{
				// The method returned a stream.  Send it to the browser.
				Stream s = (Stream)result;
				int b;
				while ((b = s.ReadByte()) != -1)
					context.Response.OutputStream.WriteByte((byte)b);
				s.Close();
			}

			context.Response.End();
		}

		#endregion
		#region Helper methods

		protected virtual string GetAssemblyName()
		{
			return regexMatch.Groups["assembly"].Value;
		}

		protected virtual string GetClassName()
		{
			return regexMatch.Groups["className"].Value;
		}

		protected virtual string GetMethodName()
		{
			return regexMatch.Groups["methodName"].Value;
		}
		protected virtual Assembly GetAssemblyFromRequest()
		{
			return Assembly.Load(GetAssemblyName());
		}

		protected virtual MethodInfo GetMethodInAssembly(Assembly assembly)
		{
			string className = GetClassName();
			Type classType = assembly.GetType(className, true, false);
			string methodName = GetMethodName();
			MethodInfo mi = classType.GetMethod(methodName,
				BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod);
			if (mi == null)
				throw new ArgumentOutOfRangeException("Method called for does not exist.");
			if (mi.GetCustomAttributes(typeof(WebDirectAttribute), true).Length == 0)
				// a web-callable method must have this attribute
				throw new ArgumentOutOfRangeException("Method called for does not have appropriate web permissions.");
			return mi; // it passes.  Return so that it can be invoked.
		}

		protected virtual object[] GetParameters(HttpContext context, MethodInfo mi)
		{
			NameValueCollection queryString = context.Request.QueryString;
			// Prepare parameters
			ParameterInfo[] pi = mi.GetParameters();
			object[] paramValues = new object[pi.Length];
			foreach (ParameterInfo p in pi)
			{
				// p.IsIn isn't true when "out" is omitted in a declaration, so we test for both.
				if (!p.IsOut || p.IsIn)
				{
					if (p.ParameterType.IsEnum && queryString[p.Name] != null)
						paramValues[p.Position] = Enum.Parse(p.ParameterType, queryString[p.Name], true);
					else if (p.ParameterType.IsArray && queryString[p.Name] != null)
					{ // this param is an array.  collect all parameters with this name
						string[] values = queryString[p.Name].Split(',');
						Array pa = Array.CreateInstance(p.ParameterType.GetElementType(), values.Length);
						for (int i = 0; i < values.Length; i++)
							if (values[i].Length > 0)
								pa.SetValue(Convert.ChangeType(values[i], p.ParameterType.GetElementType()), i);
						paramValues[p.Position] = pa;
					}
					else if (queryString[p.Name] != null) // standard parameter
						paramValues[p.Position] = Convert.ChangeType(queryString[p.Name], p.ParameterType);
				}
			}
			return paramValues;
		}

		protected virtual void CheckOutParameters(HttpContext context, MethodInfo mi, object[] paramValues)
		{
			// Go through method's parameters, looking for "out" parameters,
			// then see if we know how to handle any of them.
			ParameterInfo[] pi = mi.GetParameters();
			foreach (ParameterInfo p in pi)
			{
				if (!p.IsOut) continue;
				switch (p.Name)
				{
					case "contentType":
						context.Response.ContentType = (string)paramValues[p.Position];
						break;
				}
			}
		}

		#endregion
	}
}
