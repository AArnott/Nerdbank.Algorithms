using System;
using System.Xml;
using System.Xml.Schema;

namespace NerdBank.Tools
{
	/// <summary>
	/// A collection of utilities to help with XML handling.
	/// </summary>
	public class XmlUtilities
	{
		private XmlUtilities() {}

		public static XmlDocument LoadValidXml(string filename)
		{
			return LoadValidXml(filename, true);
		}

		public static XmlDocument LoadValidXml(string filename, XmlSchema xschema, bool useCache)
		{
			if( filename == null || filename.Length == 0 ) throw new ArgumentNullException("filename");
			XmlDocument xmlDoc;
			
			// If we have the file available in cache, and the client wants to use it,
			// return it quickly.
			if( useCache && (xmlDoc = WebUtilities.LoadFromCache(filename) as XmlDocument) != null )
				return xmlDoc;

			// Load the file from disk
			System.Xml.XmlValidatingReader xmlDocValidator = null;
			try 
			{
				xmlDocValidator = new System.Xml.XmlValidatingReader(new XmlTextReader(filename));
				if (xschema != null)
				{
					xmlDocValidator.Schemas.Add(xschema);
					xmlDocValidator.ValidationType = ValidationType.Schema;
				}

				xmlDocValidator.ValidationEventHandler += new System.Xml.Schema.ValidationEventHandler ((new ValidationCallbackClass(filename)).ValidationCallBack);
				
				xmlDoc = new XmlDocument();
				xmlDoc.Load(xmlDocValidator);

				// If the user wants to use cache, save the loaded document
				if( useCache )
					WebUtilities.SaveInCache(filename, xmlDoc, filename);
		
				// Return what we have
				return xmlDoc;
			}
			catch( Exception ex )
			{
				throw new ApplicationException("Error while loading " + filename, ex);
			}
			finally
			{
				if( xmlDocValidator != null ) 
					xmlDocValidator.Close(); // This keeps the file from getting locked
			}
		}

		public static XmlDocument LoadValidXml(string filename, bool cache)
		{
			return LoadValidXml(filename, null, cache);
		}

		
		class ValidationCallbackClass 
		{
			private string Filename;
			public ValidationCallbackClass(string filename) 
			{
				this.Filename = filename;
			}
			public void ValidationCallBack(object sender, System.Xml.Schema.ValidationEventArgs args) 
			{
				throw new XmlException("The " + Filename + " file cannot be validated. \n" +
					"Reason: " + args.Message);
			}
		}
	}
}
