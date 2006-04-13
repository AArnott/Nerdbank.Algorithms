// 
// InProjectResolver.cs
//
// Begin:  June 1, 2004
// Author: Andrew Arnott <andrewarnott@gmail.com>
//
// Copyright (C) 2004 Brigham Young University <http://www.byu.edu>
// All rights reserved.
//

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Reflection;

namespace NerdBank.Tools
{
	/// <summary>
	/// A special XML resolver so that XSLTs that reference other XSLT files 
	/// that are actually embedded in the assembly can retrieve those files.
	/// </summary>
	public class ProjectXmlResolver : XmlResolver
	{
		protected XmlUrlResolver fallback = new XmlUrlResolver();
		protected Assembly srcAssembly;
		protected string resourceNamespace;
		public ProjectXmlResolver(string resourceNamespace) : this(resourceNamespace, System.Reflection.Assembly.GetCallingAssembly()) {}
		public ProjectXmlResolver(string resourceNamespace, Assembly sourceAssembly) 
		{
			if( sourceAssembly == null ) throw new ArgumentNullException("sourceAssembly");
			if( resourceNamespace == null || resourceNamespace.Length == 0 ) throw new ArgumentNullException("resourceNamespace");
			this.resourceNamespace = resourceNamespace;
			this.srcAssembly = sourceAssembly;
		}
		public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
		{
			if( absoluteUri == null ) throw new ArgumentNullException("absoluteUri");
			if( absoluteUri.AbsolutePath == null ) throw new ArgumentNullException("absoluteUri.AbsolutePath");

			switch( absoluteUri.Scheme )
			{
				case "project":
					// Get embedded file name
					string embeddedFileName = absoluteUri.AbsolutePath;
					// This is the special kind of URI that we handle
					return EmbeddedResources.GetFileStreamFromAssembly(embeddedFileName, resourceNamespace, srcAssembly);
				default:
					// This is some other URI.  Call another resolver to do this.
					return fallback.GetEntity(absoluteUri, role, ofObjectToReturn);
			}
		}

		public override System.Net.ICredentials Credentials
		{
			set
			{
				fallback.Credentials = value;
			}
		}

	}
}
