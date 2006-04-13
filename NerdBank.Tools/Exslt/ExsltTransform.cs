using System;
using System.Xml.Xsl;
using System.Xml;
using System.Xml.XPath;
using System.IO; 

namespace NerdBank.Tools.Exslt
{


	/// <summary>
	/// Enumeration used to indicate an EXSLT function namespace. 
	/// </summary>
	[Flags]
	public enum ExsltFunctionNamespace{ 
		None  = 0,
		Common     = 1, 
		DatesAndTimes  = 2, 
		Math = 4, 
		RegularExpressions = 8, 
		Sets = 16, 
		Strings = 32, 
		All  = Common | DatesAndTimes | Math | RegularExpressions | Sets | Strings
	}

	/// <summary>
	/// Transforms XML data using an XSLT stylesheet. Supports a number of EXSLT as 
	/// defined at http://www.exslt.org
	/// </summary>
	/// <remarks>
	/// XslTransform supports the XSLT 1.0 syntax. The XSLT stylesheet must use the 
	/// namespace http://www.w3.org/1999/XSL/Transform. Additional arguments can also be 
	/// added to the stylesheet using the XsltArgumentList class. 
	/// This class contains input parameters for the stylesheet and extension objects which can be called from the stylesheet.
	/// This class also recognizes functions from the following namespaces 
	/// * http://exslt.org/common
	/// * http://exslt.org/dates-and-times
	/// * http://exslt.org/math
	/// * http://exslt.org/regular-expressions
	/// * http://exslt.org/sets
	/// * http://exslt.org/strings
	/// </remarks>
	public class ExsltTransform
	{

		#region Private Fields and Properties
	/// <summary>
	/// The XslTransform object wrapped by this class. 
	/// </summary>
		private XslTransform xslTransform; 
		
		/// <summary>
		/// Bitwise enumeration used to specify which EXSLT functions should be accessible to 
		/// the ExsltTransform object. The default value is ExsltFunctionNamespace.All 
		/// </summary>
		private ExsltFunctionNamespace _supportedFunctions = ExsltFunctionNamespace.All; 

		/// <summary>
		/// The XSLT argument list passed to the internal XslTransform object which contains
		/// the objects that implement the EXSLT functions. 
		/// </summary>
		private XsltArgumentList argList; 

		/// <summary>
		/// Extension object which implements the functions in the http://exslt.org/common namespace
		/// </summary>
		private ExsltCommon exsltCommon = new ExsltCommon(); 

		/// <summary>
		/// Extension object which implements the functions in the http://exslt.org/math namespace
		/// </summary>
		private ExsltMath exsltMath = new ExsltMath(); 

		
		/// <summary>
		/// Extension object which implements the functions in the http://exslt.org/dates-and-times namespace
		/// </summary>
		private ExsltDatesAndTimes exsltDatesAndTimes = new ExsltDatesAndTimes(); 

		/// <summary>
		/// Extension object which implements the functions in the http://exslt.org/regular-expressions namespace
		/// </summary>
		private ExsltRegularExpressions exsltRegularExpressions = new ExsltRegularExpressions(); 

		/// <summary>
		/// Extension object which implements the functions in the http://exslt.org/strings namespace
		/// </summary>
		private ExsltStrings exsltStrings = new ExsltStrings(); 

		/// <summary>
		/// Extension object which implements the functions in the http://exslt.org/sets namespace
		/// </summary>
		private ExsltSets exsltSets = new ExsltSets(); 


		#endregion

		#region Public Fields and Properties 
		

/// <summary>
/// Sets the XmlResolver used to resolve external resources when the 
/// Transform method is called.
/// </summary>
		public XmlResolver XmlResolver { set { this.xslTransform.XmlResolver = value; } }

/// <summary>
/// Bitwise enumeration used to specify which EXSLT functions should be accessible to 
/// the ExsltTransform object. The default value is ExsltFunctionNamespace.All 
/// </summary>
		public ExsltFunctionNamespace SupportedFunctions{		
			set { if (Enum.IsDefined(typeof(ExsltFunctionNamespace), value)) 
					  this._supportedFunctions = value; 
				} 
			get { return this._supportedFunctions; }
		}

		#endregion

		#region Constructors
/// <summary>
/// Constructor initializes class. 
/// </summary>
		public ExsltTransform(){
			this.xslTransform = new XslTransform(); 
			this.argList = new XsltArgumentList(); 
		}

		#endregion 

		#region Load() method Overloads  

		/// <summary> Loads the XSLT stylesheet contained in the IXPathNavigable</summary>
		public void Load(IXPathNavigable ixn){ this.xslTransform.Load(ixn); }

		/// <summary> Loads the XSLT stylesheet specified by a URL</summary>
		public void Load(string s){ this.xslTransform.Load(s); }

		/// <summary> Loads the XSLT stylesheet contained in the XmlReader</summary>
		public void Load(XmlReader reader){ this.xslTransform.Load(reader); }

		/// <summary> Loads the XSLT stylesheet contained in the XPathNavigator</summary>
		public void Load(XPathNavigator navigator){ this.xslTransform.Load(navigator); }

		/// <summary> Loads the XSLT stylesheet contained in the IXPathNavigable</summary>
		public void Load(IXPathNavigable ixn, XmlResolver resolver){ this.xslTransform.Load(ixn, resolver); }

		/// <summary> Loads the XSLT stylesheet specified by a URL</summary>
		public void Load(string s, XmlResolver resolver){ this.xslTransform.Load(s, resolver); }

		/// <summary> Loads the XSLT stylesheet contained in the XmlReader</summary>
		public void Load(XmlReader reader, XmlResolver resolver){ this.xslTransform.Load(reader, resolver); }

		/// <summary> Loads the XSLT stylesheet contained in the XPathNavigator</summary>
		public void Load(XPathNavigator navigator, XmlResolver resolver) {this.xslTransform.Load(navigator, resolver); }

		#endregion 

		#region Transform() method Overloads

		/// <summary> Transforms the XML data in the IXPathNavigable using the specified args and outputs the result to an XmlReader</summary>
		public XmlReader Transform(IXPathNavigable ixn, XsltArgumentList arglist){  
			return this.xslTransform.Transform(ixn, this.AddExsltExtensionObjects(arglist));
		}

		/// <summary> Transforms the XML data in the input file and outputs the result to an output file</summary>
		public void Transform(string infile, string outfile){ 
			this.xslTransform.Transform(new XPathDocument(infile), this.AddExsltExtensionObjects(null),new StreamWriter(outfile)); 
		}

		/// <summary> Transforms the XML data in the XPathNavigator using the specified args and outputs the result to an XmlReader</summary>
		public XmlReader Transform(XPathNavigator navigator, XsltArgumentList arglist){return this.xslTransform.Transform(navigator, this.AddExsltExtensionObjects(arglist)); }

		/// <summary> Transforms the XML data in the IXPathNavigable using the specified args and outputs the result to a Stream</summary>
		public void Transform(IXPathNavigable ixn, XsltArgumentList arglist, Stream stream){ this.xslTransform.Transform(ixn, this.AddExsltExtensionObjects(arglist), stream); }

		/// <summary> Transforms the XML data in the IXPathNavigable using the specified args and outputs the result to a TextWriter</summary>
		public void Transform(IXPathNavigable ixn, XsltArgumentList arglist, TextWriter writer){ this.xslTransform.Transform(ixn, this.AddExsltExtensionObjects(arglist), writer); }

		/// <summary> Transforms the XML data in the IXPathNavigable using the specified args and outputs the result to an XmlWriter</summary>
		public void Transform(IXPathNavigable ixn, XsltArgumentList arglist, XmlWriter writer){ this.xslTransform.Transform(ixn, this.AddExsltExtensionObjects(arglist), writer); }

		/// <summary> Transforms the XML data in the XPathNavigator using the specified args and outputs the result to a Stream</summary>
		public void Transform(XPathNavigator navigator, XsltArgumentList arglist, Stream stream){ this.xslTransform.Transform(navigator, this.AddExsltExtensionObjects(arglist), stream); }

		/// <summary> Transforms the XML data in the XPathNavigator using the specified args and outputs the result to a TextWriter</summary>
		public void Transform(XPathNavigator navigator, XsltArgumentList arglist, TextWriter writer){ this.xslTransform.Transform(navigator, this.AddExsltExtensionObjects(arglist), writer);}

		/// <summary> Transforms the XML data in the XPathNavigator using the specified args and outputs the result to an XmlWriter</summary>
		public void Transform(XPathNavigator navigator, XsltArgumentList arglist, XmlWriter writer){ this.xslTransform.Transform(navigator, this.AddExsltExtensionObjects(arglist), writer); }

		#endregion 

		#region Public Methods 

		#endregion 

		#region Private Methods 

		/// <summary>
		/// Adds the objects that implement the EXSLT extensions to the provided argument 
		/// list. The extension objects added depend on the value of the SupportedFunctions
		/// property.
		/// </summary>
		/// <param name="list">The argument list</param>
		/// <returns>An XsltArgumentList containing the contents of the list passed in 
		/// and objects that implement the EXSLT. </returns>
		/// <remarks>If null is passed in then a new XsltArgumentList is constructed. </remarks>
		private XsltArgumentList AddExsltExtensionObjects(XsltArgumentList list){
			if(list == null){
				list = new XsltArgumentList();
			}
		
			//remove all our extension objects in case the XSLT argument list is being reused
          	list.RemoveExtensionObject("http://exslt.org/common"); 
			list.RemoveExtensionObject("http://exslt.org/math"); 
			list.RemoveExtensionObject("http://exslt.org/dates-and-times"); 
			list.RemoveExtensionObject("http://exslt.org/regular-expressions"); 
			list.RemoveExtensionObject("http://exslt.org/strings"); 

			//add extension objects as specified by SupportedFunctions
			if((this.SupportedFunctions & ExsltFunctionNamespace.Common) > 0){ 
				list.AddExtensionObject("http://exslt.org/common", this.exsltCommon); 
			}

			if((this.SupportedFunctions & ExsltFunctionNamespace.Math) > 0){ 
				list.AddExtensionObject("http://exslt.org/math", this.exsltMath); 
			}

			if((this.SupportedFunctions & ExsltFunctionNamespace.DatesAndTimes) > 0){ 
				list.AddExtensionObject("http://exslt.org/dates-and-times", this.exsltDatesAndTimes); 
			}

			if((this.SupportedFunctions & ExsltFunctionNamespace.RegularExpressions) > 0){ 
				list.AddExtensionObject("http://exslt.org/regular-expressions", this.exsltRegularExpressions); 
			}

			if((this.SupportedFunctions & ExsltFunctionNamespace.Strings) > 0){ 
				list.AddExtensionObject("http://exslt.org/strings", this.exsltStrings); 
			}

			if((this.SupportedFunctions & ExsltFunctionNamespace.Sets) > 0){ 
				list.AddExtensionObject("http://exslt.org/sets", this.exsltSets); 
			}

			return list; 
		}

		#endregion

	}
}
