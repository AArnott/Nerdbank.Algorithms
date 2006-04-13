using System;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Resources;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace NerdBank.Tools
{
	/// <summary>
	/// Summary description for CultureAssist.
	/// </summary>
	public class CultureAssist
	{
		ResourceManager RM;

		public CultureAssist(object typeObject) : this(typeObject.GetType()) { }

		public CultureAssist(Type type)
		{
			RM = new ResourceManager(type);
		}

		public string GetResourceString(string ResourceName)
		{
			return RM.GetString(ResourceName);
		}

		public string GetResourceString(string ResourceName, params object[] arguments)
		{
			string formatString = RM.GetString(ResourceName);
			if (formatString == null)
				throw new ArgumentOutOfRangeException("ResourceName", ResourceName, "Could not find a string resource by that name.");
			return string.Format(formatString, arguments);
		}

		#region XSLT supporting methods
		// The following redundant looking methods are to support XSLTs that call in 
		// via extension objects
		private System.Xml.XPath.XPathNodeIterator GetResourceStringXSLTCommon(string ResourceName, params object[] arguments)
		{
			string str = GetResourceString(ResourceName, arguments);
			System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
			doc.AppendChild(doc.CreateElement("root")).AppendChild(doc.CreateTextNode(str));
			return doc.DocumentElement.CreateNavigator().Select(".");
		}

		public System.Xml.XPath.XPathNodeIterator GetResourceStringXSLT(string ResourceName)
		{
			return GetResourceStringXSLTCommon(ResourceName);
		}

		public System.Xml.XPath.XPathNodeIterator GetResourceStringXSLT(string ResourceName, string arg1)
		{
			return GetResourceStringXSLTCommon(ResourceName, (object)arg1);
		}

		public System.Xml.XPath.XPathNodeIterator GetResourceStringXSLT(string ResourceName, string arg1, string arg2)
		{
			return GetResourceStringXSLTCommon(ResourceName, (object)arg1, (object)arg2);
		}
		#endregion

		#region Access culturally specific data in databases
		/// <summary>
		/// Assembles an array of ISO standard culture names
		/// that apply to the current thread.
		/// </summary>
		/// <returns>
		/// A string array of cultures to search for that are applicable.
		/// The strength of the applicability decreases as you search through the array.
		/// </returns>
		public static string[] ApplicableSpecificCultures
		{
			get
			{
				string culture = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
				string language = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

				return new string[] { culture, language };
			}
		}
		/// <summary>
		/// Assembles an array of ISO standard culture names
		/// that apply to the current thread, plus the english standard.
		/// </summary>
		/// <returns>
		/// A string array of cultures to search for that are applicable.
		/// The strength of the applicability decreases as you search through the array.
		/// </returns>
		/// <remarks>
		/// Used to help assemble a SQL query that will return the culturally
		/// applicable rows from somewhere.
		/// </remarks>
		public static string[] ApplicableCultures
		{
			get
			{
				string culture = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
				string language = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

				return new string[] { culture, language, "en-US", "en" };
			}
		}

		/// <returns>The object pulled from the database, or null if the culturized data could not be found.</returns>
		public static object GetCulturizedDataInDB(SqlConnection conn,
			string DataTableName, string CultureColumnName,
			string DataColumnName,
			string PKColumnName, int PKValue)
		{
			if (conn == null) throw new ArgumentNullException("conn");
			if (DataTableName == null || DataTableName.Length == 0) throw new ArgumentNullException("DataTableName");
			if (CultureColumnName == null || CultureColumnName.Length == 0) throw new ArgumentNullException("CultureColumnName");
			if (DataColumnName == null || DataColumnName.Length == 0) throw new ArgumentNullException("DataColumnName");
			if (PKColumnName == null || PKColumnName.Length == 0) throw new ArgumentNullException("PKColumnName");

			bool bOpenedDB = conn.State == ConnectionState.Closed;
			try
			{
				if (bOpenedDB) conn.Open();

				string sql = string.Format("SELECT {0} FROM {1} " +
					"WHERE {2} = {3} AND {4} = @culture",
					DataColumnName,
					DataTableName,
					PKColumnName,
					PKValue,
					CultureColumnName
					);
				SqlCommand cmd = new SqlCommand(sql, conn);
				cmd.Parameters.Add("@culture", SqlDbType.VarChar, 5);
				object result = null;
				foreach (string culture in ApplicableCultures)
				{
					cmd.Parameters["@culture"].Value = culture;
					result = cmd.ExecuteScalar();
					if (result != null) break; // we found a match!
				}

				// If the row was simply not found at all, a null will result.
				// If the row was found but the value was NULL, then DBNull
				// will result.  Either way, we will return null to simply things
				// for the caller.
				return result == DBNull.Value ? null : result;
			}
			finally
			{
				if (bOpenedDB) conn.Close();
			}
		}
		public static void SetCulturizedDataInDB(SqlConnection conn,
			string DataTableName, string CultureColumnName,
			string DataColumnName,
			string PKColumnName, int PKValue,
			object value)
		{
			if (conn == null) throw new ArgumentNullException("conn");
			if (DataTableName == null || DataTableName.Length == 0) throw new ArgumentNullException("DataTableName");
			if (CultureColumnName == null || CultureColumnName.Length == 0) throw new ArgumentNullException("CultureColumnName");
			if (DataColumnName == null || DataColumnName.Length == 0) throw new ArgumentNullException("DataColumnName");
			if (PKColumnName == null || PKColumnName.Length == 0) throw new ArgumentNullException("PKColumnName");

			bool bOpenedDB = conn.State == ConnectionState.Closed;
			try
			{
				if (bOpenedDB) conn.Open();

				string sql = string.Format("SELECT * FROM {0} " +
					"WHERE {1} = {2} AND {3} = @culture",
					DataTableName,
					PKColumnName,
					PKValue,
					CultureColumnName
					);
				SqlDataAdapter da = new SqlDataAdapter(sql, conn);
				da.SelectCommand.Parameters.AddWithValue("@culture", ApplicableCultures[0]);
				new SqlCommandBuilder(da); // build the Update command
				DataTable tbl = new DataTable();
				da.Fill(tbl);
				// There may not be a cultural row for this yet.  Add it if we need.
				if (tbl.Rows.Count == 0)
				{
					string sqlToAdd = string.Format("INSERT INTO {0} ({1}, {2}) VALUES (@ID, @culture)",
						DataTableName, PKColumnName, CultureColumnName);
					SqlCommand cmd = new SqlCommand(sqlToAdd, conn);
					cmd.Parameters.AddWithValue("@ID", PKValue);
					cmd.Parameters.AddWithValue("@culture", System.Threading.Thread.CurrentThread.CurrentUICulture.Name);
					cmd.ExecuteNonQuery();
					da.Fill(tbl);
					if (tbl.Rows.Count == 0) throw new ApplicationException("No row for culture found for " +
												 "setting value, and the row could not be added.");
				}
				// Set the data (set DBNull if null was passed in)
				tbl.Rows[0][DataColumnName] = value == null ? DBNull.Value : value;
				// Update the data source
				da.Update(tbl);
			}
			finally
			{
				if (bOpenedDB) conn.Close();
			}
		}
		#endregion

		#region Get most appropriate culture from a list
		/// <summary>
		/// Gets the list of cultures, in precedence order, that should be checked through
		/// when parsing the arrays of culture-specific attributes in this class.
		/// </summary>
		/// <param name="lastResort">The culture to use if none of the current thread cultures match.</param>
		public static CultureInfo[] GetRelevantCultures(CultureInfo lastResort)
		{
			List<CultureInfo> cultures = new List<CultureInfo>(3);
			// start with the thread's current culture
			cultures.Add(Thread.CurrentThread.CurrentUICulture);
			// if the current culture has a neutral culture above it, try that
			if (!cultures[0].IsNeutralCulture)
				cultures.Add(cultures[0].Parent);
			// finally, revert to the questionnaire's primary culture
			if (!cultures.Contains(lastResort))
				cultures.Add(lastResort);
			return cultures.ToArray();
		}
		/// <summary>
		/// Gets the most relevant localized resource from a dictionary of
		/// culture-specific resources.
		/// </summary>
		/// <typeparam name="T">The type of resource being sought.</typeparam>
		/// <param name="dictionary">The dictionary to search.</param>
		/// <param name="cultures">The list of cultures to check for resource information on.</param>
		/// <returns>The relevant resource, or default(T) if none found.</returns>
		public static T GetMostRelevantLocalResource<T>(IDictionary<CultureInfo, T> dictionary, CultureInfo[] cultures)
		{
			CultureInfo bestCulture = GetMostRelevantCulture(dictionary.Keys, cultures);
			return bestCulture != null ? dictionary[bestCulture] : default(T);
		}
		public static CultureInfo GetMostRelevantCulture(ICollection<CultureInfo> availableCultures, IEnumerable<CultureInfo> relevantCultures)
		{
			foreach (CultureInfo culture in relevantCultures)
				if (availableCultures.Contains(culture))
					return culture;
			return null;
		}
		#endregion
	}
}
