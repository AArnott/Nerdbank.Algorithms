using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NerdBank.Tools
{
	/// <summary>
	/// Summary description for EmbeddedFile.
	/// </summary>
	public class EmbeddedResources
	{
		#region Loads a file by name from the assembly as a string
		/// <summary>
		/// Use to conveniently load files that are built into the assembly.
		/// </summary>
		/// <param name="filename">
		/// The filename of the embedded file.  This filename should 
		/// include the path within the project of the file, and start with a forward slash.
		/// </param>
		/// <param name="namespacePrefix">
		/// The namespace of the file.  This is either the default namespace of the project,
		/// or the namespace set under that file's Properties under "Custom Tool Namespace".
		/// </param>
		/// <param name="assembly">
		/// The assembly in which to look for the embedded file.  
		/// If null, the calling assembly is assumed.
		/// </param>
		/// <returns>
		/// A string with the contents of the file.
		/// </returns>
		/// <example>
		/// <code>
		/// [C#]
		/// Project.LoadFileFromAssemblyWithNamespace("/mypath/myembeddedfile.xslt", "MyProject");
		/// [VB]
		/// Project.LoadFileFromAssemblyWithNamespace("/mypath/myembeddedfile.xslt", "")
		/// </code>
		/// </example>
		/// <remarks>
		/// Note that in VB.NET projects, embedded resources are stored only by their
		/// own file names.  In C# embedded resources are filed by a combination of 
		/// their path within the project and their file names.
		/// </remarks>
		public static string LoadFileFromAssemblyWithNamespace(string filename, string namespacePrefix, Assembly assembly)
		{
			if (filename == null) throw new ArgumentNullException("filename");
			if( assembly == null ) assembly = System.Reflection.Assembly.GetCallingAssembly();

			Stream assemblyFileStream = GetLocalizedManifestResourceStream(filename, namespacePrefix, assembly);

			StreamReader reader = new StreamReader(assemblyFileStream);
			try 
			{
				return reader.ReadToEnd();
			}
			finally
			{
				reader.Close();
			}	
		}

		/// <summary>
		/// Use to conveniently load files that are built into the assembly.
		/// </summary>
		/// <param name="filename">
		/// The filename of the embedded file.  This filename should 
		/// include the path within the project of the file, and start with a forward slash.
		/// </param>
		/// <param name="namespacePrefix">
		/// The namespace of the file.  This is either the default namespace of the project,
		/// or the namespace set under that file's Properties under "Custom Tool Namespace".
		/// </param>
		/// <returns>
		/// A string with the contents of the file.
		/// </returns>
		/// <example>
		/// [C#]
		/// Project.LoadFileFromAssemblyWithNamespace("/mypath/myembeddedfile.xslt", "MyProject");
		/// [VB]
		/// Project.LoadFileFromAssemblyWithNamespace("/mypath/myembeddedfile.xslt", "")
		/// </example>
		/// <remarks>
		/// Note that in VB.NET projects, embedded resources are stored only by their
		/// own file names.  In C# embedded resources are filed by a combination of 
		/// their path within the project and their file names.
		/// </remarks>
		public static string LoadFileFromAssemblyWithNamespace(string filename, string namespacePrefix)
		{
			return LoadFileFromAssemblyWithNamespace(filename, namespacePrefix, Assembly.GetCallingAssembly());
		}
	
		#region Obsolete
		/// <summary>
		/// Use to conveniently load files that are built into the assembly.
		/// In c#, the syntax for calling this function is:
		/// Project.LoadFileFromAssembly("/mypath/myembeddedfile.xslt", "", this.GetType().Assembly);
		/// In VB, the syntax is:
		/// Project.LoadFileFromAssembly("/myembeddedfile.xslt", "", Me.GetType.Assembly)
		/// </summary>
		/// <param name="filename">
		/// The filename of the embedded file.  This filename should 
		/// include the path within the project of the file, and start with a forward slash.
		/// </param>
		/// <param name="namespacePrefix">
		/// If your assembly has some special prefix that is attached even before the 
		/// assembly name in its embedded resources, specify it here.
		/// </param>
		/// <param name="assembly">
		/// The calling assembly.  The assembly in which to look for
		/// the embedded file.
		/// </param>
		/// <returns>
		/// A string with the contents of the file.
		/// </returns>
		/// <remarks>
		/// Note that in VB.NET projects, embedded resources are stored only by their
		/// own file names.  In C# embedded resources are filed by a combination of 
		/// their path within the project and their file names.
		/// </remarks>
		[Obsolete("Use LoadFileFromAssemblyWithNamespace instead.")]
		public static string LoadFileFromAssembly(string filename, string namespacePrefix, Assembly assembly) 
		{
			if( filename == null || filename == "" ) throw new ArgumentNullException("filename");
			if( namespacePrefix == null ) namespacePrefix = "";
			if( assembly == null ) assembly = System.Reflection.Assembly.GetCallingAssembly();

			// Make sure the file starts with a slash to signify the root of the Assembly's project
			if( filename.Substring(0,1) != "/" ) filename = "/" + filename;
			// Now exchange all slashes for periods, the Assembly convention
			string AssemblyFilename = filename.Replace("/", ".");
			// Prefix this with the name of the Assembly itself
			AssemblyFilename = namespacePrefix + assembly.GetName().Name + AssemblyFilename;

			Stream AssemblyFileStream = 
				assembly.GetManifestResourceStream( AssemblyFilename );
			if( AssemblyFileStream == null )
				throw new ArgumentException("File to load from Assembly, " + filename + ", does not " +
					"exist in the Assembly " + assembly.FullName + ".  Check to see that the file's Build Action " +
					"attribute in the project is set to \"Embedded Resource\".  " +
					"Exact manifest name tried: " + AssemblyFilename, "Filename");
			StreamReader reader = new StreamReader(AssemblyFileStream);
			try 
			{
				return reader.ReadToEnd();
			}
			finally
			{
				reader.Close();
			}	
		}

		/// <summary>
		/// Use to conveniently load files that are built into the assembly.
		/// In c#, the syntax for calling this function is:
		/// Project.LoadFileFromAssembly("/mypath/myembeddedfile.xslt", "", this.GetType().Assembly);
		/// In VB, the syntax is:
		/// Project.LoadFileFromAssembly("/myembeddedfile.xslt", "", Me.GetType.Assembly)
		/// </summary>
		/// <param name="filename">
		/// The filename of the embedded file.  This filename should 
		/// include the path within the project of the file, and start with a forward slash.</param>
		/// <param name="namespacePrefix">
		/// If your assembly has some special prefix that is attached even before the 
		/// assembly name in its embedded resources, specify it here.
		/// </param>
		/// <returns>
		/// A string with the contents of the file.
		/// </returns>
		/// <remarks>
		/// Note that in VB.NET projects, embedded resources are stored only by their
		/// own file names.  In C# embedded resources are filed by a combination of 
		/// their path within the project and their file names.
		/// </remarks>
		[Obsolete("Use LoadFileFromAssemblyWithNamespace instead.")]
		public static string LoadFileFromAssembly(string filename, string namespacePrefix) 
		{
			return LoadFileFromAssembly(filename, namespacePrefix, System.Reflection.Assembly.GetCallingAssembly());
		}

		/// <summary>
		/// Use to conveniently load files that are built into the assembly.
		/// In c#, the syntax for calling this function is:
		/// Project.LoadFileFromAssembly("/mypath/myembeddedfile.xslt", "", this.GetType().Assembly);
		/// In VB, the syntax is:
		/// Project.LoadFileFromAssembly("/myembeddedfile.xslt", "", Me.GetType.Assembly)
		/// </summary>
		/// <param name="filename">
		/// The filename of the embedded file.  This filename should 
		/// include the path within the project of the file, and start with a forward slash.
		/// </param>
		/// <returns>
		/// A string with the contents of the file.
		/// </returns>
		/// <remarks>
		/// Note that in VB.NET projects, embedded resources are stored only by their
		/// own file names.  In C# embedded resources are filed by a combination of 
		/// their path within the project and their file names.
		/// </remarks>
		[Obsolete("Use LoadFileFromAssemblyWithNamespace instead.")]
		public static string LoadFileFromAssembly(string filename) 
		{
			return LoadFileFromAssembly(filename, null, System.Reflection.Assembly.GetCallingAssembly());
		}

		#endregion
		#endregion

		#region Get localized resource stream
		public static Stream GetLocalizedManifestResourceStream(string manifestName, Assembly baseAssembly)
		{
			if (manifestName == null || manifestName.Length == 0)
				throw new ArgumentNullException("manifestName");
			if (baseAssembly == null) throw new ArgumentNullException("baseAssembly");

			Stream stream = null;
			CultureInfo culture = Thread.CurrentThread.CurrentUICulture;
			// first try the specific culture
			stream = GetLocalizedManifestResourceStreamIfExists(manifestName, baseAssembly, culture);
			// then try the neutral culture
			if (stream == null && !culture.IsNeutralCulture)
				stream = GetLocalizedManifestResourceStreamIfExists(manifestName, baseAssembly, culture.Parent);
			// lastly, try the default culture
			if (stream == null)
				stream = GetFileStreamFromAssembly(manifestName, baseAssembly);

			// if an exception need be thrown, our last call will do the job
			return stream;
		}

		public static Stream GetLocalizedManifestResourceStream(string manifestName, Assembly baseAssembly, CultureInfo culture)
		{
			if (manifestName == null || manifestName.Length == 0)
				throw new ArgumentNullException("manifestName");
			if (baseAssembly == null) throw new ArgumentNullException("baseAssembly");
			if (culture == null) throw new ArgumentNullException("culture");

			Stream stream = baseAssembly.GetSatelliteAssembly(culture).GetManifestResourceStream(manifestName);
			if( stream == null && manifestName.Contains("_") ) // test projects use spaces
				stream = baseAssembly.GetSatelliteAssembly(culture).GetManifestResourceStream(manifestName.Replace('_', ' '));
			return stream;
		}

		public static Stream GetLocalizedManifestResourceStreamIfExists(string manifestName, Assembly baseAssembly, CultureInfo culture)
		{
			if (manifestName == null || manifestName.Length == 0)
				throw new ArgumentNullException("manifestName");
			if (baseAssembly == null) throw new ArgumentNullException("baseAssembly");
			if (culture == null) throw new ArgumentNullException("culture");

			try
			{
				return baseAssembly.GetSatelliteAssembly(culture).GetManifestResourceStream(manifestName);
			}
			catch( FileNotFoundException) 
			{
				return null;
			}
		}

		public static Stream GetLocalizedManifestResourceStream(string fileName, string namespacePrefix)
		{
			return GetLocalizedManifestResourceStream(fileName, namespacePrefix, Assembly.GetCallingAssembly());
		}

		public static Stream GetLocalizedManifestResourceStream(string fileName, string namespacePrefix, Assembly baseAssembly)
		{
			string manifestName = ManifestNameFromFileNameAndNamespace(fileName, namespacePrefix);
			CultureInfo culture = GetCultureFromManifestName(manifestName, out manifestName);
			if( culture == null )
				return GetLocalizedManifestResourceStream(manifestName, baseAssembly);
			else
				return GetLocalizedManifestResourceStream(manifestName, baseAssembly, culture);
		}
		#endregion

		#region Get embedded file in stream from manifest name
		/// <summary>
		/// Conveniently loads files that are built into the assembly.
		/// </summary>
		/// <param name="manifestName">
		/// The filename of the embedded file.  This filename should 
		/// include the path within the project of the file, and start with a forward slash.
		/// </param>
		/// <param name="assembly">
		/// The calling assembly.  The assembly in which to look for
		/// the embedded file.
		/// </param>
		/// <returns>
		/// A stream with the contents of the file.
		/// </returns>
		/// <remarks>
		/// Note that in VB.NET projects, embedded resources are stored only by their
		/// own file names.  In C# embedded resources are filed by a combination of 
		/// their path within the project and their file names.
		/// </remarks>
		public static Stream GetFileStreamFromAssembly(string manifestName, Assembly assembly)
		{
			if( manifestName == null ) throw new ArgumentNullException("manifestName");
			if( assembly == null ) throw new ArgumentNullException("assembly");

			// Test to see if name includes a culturized resource 
			Stream fileStream = assembly.GetManifestResourceStream(manifestName);
			if( fileStream == null ) throw new ArgumentOutOfRangeException("manifestName", manifestName, 
										 "The embedded file could not be found in the assembly " +
										 assembly.FullName + ".  " +
										 "Check to see that the file's Build Action " +
										"attribute in the project is set to \"Embedded Resource\".");
			return fileStream;
		}

		/// <summary>
		/// Conveniently loads files that are built into the assembly.
		/// </summary>
		/// <param name="manifestName">
		/// The filename of the embedded file.  This filename should 
		/// include the path within the project of the file, and start with a forward slash.
		/// </param>
		/// <returns>
		/// A stream with the contents of the file.
		/// </returns>
		/// <remarks>
		/// Note that in VB.NET projects, embedded resources are stored only by their
		/// own file names.  In C# embedded resources are filed by a combination of 
		/// their path within the project and their file names.
		/// </remarks>
		public static Stream GetFileStreamFromAssembly(string manifestName)
		{
			return GetFileStreamFromAssembly(manifestName, System.Reflection.Assembly.GetCallingAssembly());
		}
	
		/// <summary>
		/// Conveniently loads files that are built into the assembly.
		/// </summary>
		/// <param name="filename">
		/// The filename of the embedded file.  This filename should 
		/// include the path within the project of the file, and start with a forward slash.
		/// </param>
		/// <param name="namespacePrefix">
		/// The namespace of the file.  This is either the default namespace of the project,
		/// or the namespace set under that file's Properties under "Custom Tool Namespace".
		/// </param>
		/// <param name="assembly">
		/// The assembly the load the embedded file from.
		/// </param>
		/// <returns>
		/// A stream with the contents of the file.
		/// </returns>
		public static Stream GetFileStreamFromAssembly(string filename, string namespacePrefix, Assembly assembly)
		{
			return GetFileStreamFromAssembly(ManifestNameFromFileNameAndNamespace(filename, namespacePrefix), assembly);
		}
		/// <summary>
		/// Conveniently loads files that are built into the assembly.
		/// </summary>
		/// <param name="filename">
		/// The filename of the embedded file.  This filename should 
		/// include the path within the project of the file, and start with a forward slash.
		/// </param>
		/// <param name="namespacePrefix">
		/// The namespace of the file.  This is either the default namespace of the project,
		/// or the namespace set under that file's Properties under "Custom Tool Namespace".
		/// </param>
		/// <returns>
		/// A stream with the contents of the file.
		/// </returns>
		public static Stream GetFileStreamFromAssembly(string filename, string namespacePrefix)
		{
			return GetFileStreamFromAssembly(filename, namespacePrefix, Assembly.GetCallingAssembly());
		}
		#endregion

		#region Convert filename to manifest name
		/// <summary>
		/// Converts the name of a file embedded within an assembly into its manifest name.  
		/// </summary>
		/// <param name="filename">
		/// The full path to the file as it appears in the project.  Should start with a forward slash
		/// signifying the root of the project.
		/// </param>
		/// <param name="namespacePrefix">
		/// The namespace of the file.  This is either the default namespace of the project,
		/// or the namespace set under that file's Properties under "Custom Tool Namespace".
		/// </param>
		/// <returns>
		/// The manifest filename that can be used to retrieve the resource from the assembly.
		/// </returns>
		public static string ManifestNameFromFileNameAndNamespace(string filename, string namespacePrefix)
		{
			if( filename == null || filename.Length == 0 ) throw new ArgumentNullException("filename");
			if( namespacePrefix == null ) namespacePrefix = "";
	
			// Make sure the file starts with a slash to signify the root of the Assembly's project
			if( filename.Substring(0,1) != "/" ) filename = "/" + filename;
			// Swap a few characters consistent with Visual Studio convention
			string manifestName = filename.Replace('/', '.');
			// Change spaces to underscores in directory names only.
			int pathEndsAt = filename.LastIndexOf('/');
			manifestName = manifestName.Substring(0, pathEndsAt).Replace(' ', '_') + manifestName.Substring(pathEndsAt);
			// Prefix this with the namespace within the Assembly itself
			manifestName = namespacePrefix + manifestName;

			return manifestName;
		}

		public static CultureInfo GetCultureFromManifestName(string manifestName, out string newManifestName)
		{
			newManifestName = manifestName;
			CultureInfo defaultCulture = null;
			Match m = Regex.Match(manifestName, @"\A(?<pre>.+)\.(?<culture>[a-z]{2}(?:-[A-Z]{2})?)(?<post>\.[^\.]+)\z");
			if( !m.Success ) return defaultCulture; // assume current culture
			try
			{
				CultureInfo culture = CultureInfo.GetCultureInfo(m.Groups["culture"].Value);
				newManifestName = m.Groups["pre"].Value + m.Groups["post"].Value;
				return culture;
			}
			catch (ArgumentException) // not a real culture
			{
				return defaultCulture;
			} 
		}
		#endregion
	}
}
