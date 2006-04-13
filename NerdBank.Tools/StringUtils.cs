using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;

namespace NerdBank.Tools
{
	/// <summary>
	/// Summary description for StringUtils.
	/// </summary>
	public class StringUtilities
	{
		// Simple private constructor to prevent default constructor from being added by the compiler
		private StringUtilities() {}

		const string ChooseFromAll = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
		// NOTE: 1's, I's, L's, 0's and O's are missing intentionally
		// because they look too much alike when printed.		
		const string ChooseFromNoLookAlikes = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
		static int iteration = 0; // global to guarantee unique random characters
		/// <summary>
		/// Generates random character combinations that include letters and numbers
		/// to form random strings.
		/// </summary>
		/// <param name="Count">
		/// The number of strings to generate.
		/// </param>
		/// <param name="UniqueFrom">
		/// Optional. An array of existing strings from which the newly generated strings will
		/// be guaranteed to be unique.
		/// </param>
		/// <param name="StrLength">
		/// The length of each random string.
		/// </param>
		/// <param name="IncludeLookAlikes">
		/// Whether to include characters/numbers in the random generated strings
		/// that look similar to the human eye using the common fonts.  If false, 
		/// then characters like 1 (one) and I (eye) are never used.
		/// </param>
		/// <returns>
		/// An array of randomly generated strings of the specified length.
		/// </returns>
		public static string[] GenerateUniqueStrings(int Count, string[] UniqueFrom, int StrLength, bool IncludeLookAlikes) 
		{
			if( Count < 1 ) throw new ArgumentOutOfRangeException("Count", Count, "Must be a positive integer.");
			if( StrLength < 1 ) throw new ArgumentOutOfRangeException("StrLength", StrLength, "Must be a positive integer.");
			string ChooseFrom = IncludeLookAlikes ? ChooseFromAll : ChooseFromNoLookAlikes;

			// Generate random strings, that are guaranteed to be unique from anything in the passed
			// list.  Return the result as an array of strings.
			
			// We start a random number generator here.  But because this can get called
			// very fast, the system time may not change between iterations, and it is SO
			// important that these strings be unique, that we add a little something to the clock.
			Random random = new Random(unchecked((int)DateTime.Now.Ticks+iteration++));
			iteration %= int.MaxValue; // reset when it gets too high

			// Validate the passed arguments
			if( StrLength < 0) throw new ArgumentOutOfRangeException("StrLength must be equal to or greater than zero.");
			if( StrLength == 0 ) 
			{
				// We must generate this argument ourselves
				if( UniqueFrom.Length == 0 ) throw new ArgumentException("StrLength must be greater than zero when UniqueFrom is an empty array.");
				StrLength = UniqueFrom[0].Length;
			}

			// Prepare a hash table master list to compare each new token to
			int capacity = Count;
			if( UniqueFrom != null ) capacity += UniqueFrom.Length;
			Hashtable masterList = new Hashtable(capacity);
			if( UniqueFrom != null) 
				for( int i = 0; i < UniqueFrom.Length; i++ ) 
				{
					// Make sure all these unique strings are the same length
					if( UniqueFrom[i].Length != StrLength) throw new ArgumentException("The UniqueFrom string array contains a string whose length does not equal StrLength.");
					masterList.Add(UniqueFrom[i], null);
				}
			// Prepare another list to store the generated numbers
			string[] newList = new string[Count];
			System.Text.StringBuilder uniqStr = new System.Text.StringBuilder(StrLength);
			for( int i = 0; i < newList.Length; i++ ) 
			{
				do 
				{
					// Generate a string of desired length
					uniqStr.Length = 0; // clear what we've got
					for( int j = 0; j < StrLength; j++ )
						uniqStr.Append(ChooseFrom[random.Next(ChooseFrom.Length)]);
				} while( masterList.ContainsKey(uniqStr.ToString()) );
				masterList.Add(uniqStr.ToString(), null);
				newList[i] = uniqStr.ToString();
			}
		
			return newList;
		}
		/// <summary>
		/// Generates random character combinations that include letters and numbers
		/// to form random strings.
		/// </summary>
		/// <param name="Count">
		/// The number of strings to generate.
		/// </param>
		/// <param name="UniqueFrom">
		/// Optional. An array of existing strings from which the newly generated strings will
		/// be guaranteed to be unique.
		/// </param>
		/// <param name="StrLength">
		/// The length of each random string.
		/// </param>
		/// <returns>
		/// An array of randomly generated strings of the specified length.
		/// </returns>
		public static string[] GenerateUniqueStrings(int Count, string[] UniqueFrom, int StrLength) 
		{
			// Assume no look-alikes
			return GenerateUniqueStrings(Count, UniqueFrom, StrLength, false);
		}

		/// <summary>
		/// The hashing algorithm for the hash functions in this class to use
		/// </summary>
		public enum HashAlgorithm 
		{
			/// <summary>
			/// The MD5 hash algorithm
			/// </summary>
			MD5,
			/// <summary>
			/// The SHA-1 hash algorithm
			/// </summary>
			SHA1
		}
		/// <summary>
		/// Returns a hashed version of any unicode string.
		/// </summary>
		/// <param name="StringToHash">The string to hash.</param>
		/// <param name="algorithm">Which hash algorithm of MD5 or SHA1 to use, or SHA1 if not 
		/// specified.</param>
		/// <returns>A 40 character hash version of the string.  All characters are hexadecimal.</returns>
		public static string HashString(string StringToHash, HashAlgorithm algorithm) 
		{
			byte[] bytes = (new UnicodeEncoding()).GetBytes(StringToHash.ToCharArray());
			byte[] hash;
			switch( algorithm )
			{
				case HashAlgorithm.MD5:
					hash = (new System.Security.Cryptography.MD5CryptoServiceProvider()).ComputeHash( bytes );
					break;
				case HashAlgorithm.SHA1:
					hash = (new System.Security.Cryptography.SHA1CryptoServiceProvider()).ComputeHash( bytes );
					break;
				default:
					throw new ArgumentOutOfRangeException("algorithm", algorithm, "Must specify either MD5 or SHA1 hashing.");
			}
			return BitConverter.ToString( hash ).Replace("-", "");
		}
		/// <summary>
		/// Returns a hashed version of any unicode string.
		/// </summary>
		/// <param name="StringToHash">The string to hash.</param>
		/// <returns>A 40 character hash version of the string.  All characters are hexadecimal.</returns>
		public static string HashString(string StringToHash) 
		{
			// Default to SHA1 hashing
			return HashString(StringToHash, HashAlgorithm.SHA1);
		}

		/// <summary>
		/// Detects whether a string is a hash.
		/// </summary>
		/// <param name="str">
		/// The string that may or may not be a hash.
		/// </param>
		/// <returns>
		/// True if the provided string is a hash.
		/// </returns>
		public static bool IsHashed(string str)
		{
			return IsHashed(str, HashAlgorithm.SHA1);
		}
		/// <summary>
		/// Detects whether a string is a hash.
		/// </summary>
		/// <param name="str">
		/// The string that may or may not be a hash.
		/// </param>
		/// <param name="algorithm">
		/// The hashing algorithm to check for.  Optional.  Default to SHA-1.
		/// </param>
		/// <returns>
		/// True if the provided string is a hash.
		/// </returns>
		public static bool IsHashed(string str, HashAlgorithm algorithm)
		{
			switch( algorithm )
			{
				case HashAlgorithm.SHA1:
					return str.Length == 40 && str == str.ToUpper();
				case HashAlgorithm.MD5:
				default:
					throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Replace all references to a keyword/variable in some string with the given value.
		/// </summary>
		/// <param name="sInput">
		/// The string to be changed.
		/// </param>
		/// <param name="sMatch">
		/// The name of the variable.  This can start and/or end with some non-alphanumeric character
		/// to prevent surrounding whitespace from being required.
		/// </param>
		/// <param name="sValue">
		/// The string to substitute in for each match.
		/// </param>
		/// <returns>
		/// The new string.
		/// </returns>
		public static string SwapVarsForValues(string sInput, string sMatch, string sValue) 
		{
			if( sInput == null ) throw new ArgumentNullException("sInput");
			// Use RegEx to make sure that a varname of "$myvar" doesn't also match "$myvariable".
			string pattern = Regex.Escape(sMatch);
			// Check to see if there appears to be a prefix on this variable name, such as 
			// a dollar sign. If not, then add a prefix to the pattern to ensure that "cat" 
			// doesn't match "tomcat"
			// The @"\b" ensure a word boundary (ms-help://MS.VSCC.2003/MS.MSDNQTR.2003FEB.1033/cpgenref/html/cpconAtomicZero-WidthAssertions.htm)
			if( Regex.IsMatch(sMatch, @"\A\w") ) // if no prefix
				pattern = @"\b" + pattern; // knock out "tomcat"
			// Now do the same thing for suffixes, so that a variable name of "cat" doesn't match
			// "cats"
			if( Regex.IsMatch(sMatch, @"\w\z") ) // no suffix
				pattern += @"\b"; // knock out "cats"

			return Regex.Replace( sInput, pattern, sValue, RegexOptions.IgnoreCase );
		}
	
		/// <summary>
		/// Replace all references to a keyword/variable in some string with the given value.
		/// </summary>
		/// <param name="input">
		/// The string to be changed.
		/// </param>
		/// <param name="varsAndValues">
		/// A map of string keys and string values that represent the variables and values respectively.
		/// </param>
		/// <returns>
		/// The new string.
		/// </returns>
		public static string SwapVarsForValues(string input, System.Collections.Specialized.StringDictionary varsAndValues)
		{
			if( input == null ) throw new ArgumentNullException("input");
			if( varsAndValues == null ) throw new ArgumentNullException("varsAndValues");

			foreach( DictionaryEntry de in varsAndValues )
				input = SwapVarsForValues( input, de.Key as string, de.Value as string );

			return input;
		}
		/// <summary>
		/// Tokenize a string into an array of strings.
		/// </summary>
		/// <param name="str">
		/// The string to split.
		/// </param>
		/// <param name="delimiters">
		/// The delimiters to split the string on.  Or null to tokenize on whitespace.
		/// </param>
		/// <returns>
		/// An array of tokenized strings.
		/// </returns>
		public static string[] Tokenize(string str, char[] delimiters)
		{
			if( str == null ) throw new ArgumentNullException("str");
			string[] tokens = str.Split(delimiters);
			// reproduce this array without blank entries
			ArrayList newList = new ArrayList(tokens.Length);
			foreach( string token in tokens )
				if(token.Length>0) newList.Add(token);
			return (string[]) newList.ToArray(typeof(string));
		}
		/// <summary>
		/// Tokenize a string into an array of strings, splitting at whitespace.
		/// </summary>
		/// <param name="str">
		/// The string to split.
		/// </param>
		/// <returns>
		/// An array of tokenized strings.
		/// </returns>
		public static string[] Tokenize(string str)
		{
			return Tokenize(str, null); // null = tokenize around whitespace
		}
		/// <summary>
		/// Quotes a string for passing into an XPath expression.
		/// </summary>
		/// <param name="str">
		/// The string to quote.
		/// </param>
		/// <returns>
		/// The string to quote, surrounded with either single or double quotes.
		/// </returns>
		/// <remarks>
		/// Single quotes are used to quote the supplied string if no single quotes are found
		/// in the supplied string.  Double quotes are used in single quotes exist but double quotes
		/// do not.
		/// </remarks>
		/// <exception cref="ArgumentException">
		/// Thrown when the supplied string contains both single and double quotes.
		/// </exception>
		public static string QuoteXPathString(string str)
		{
			const char apos = '\''; const char quot = '"';
			if( str.IndexOf(apos) < 0 ) // if no apostrophes are used in the string...
				return apos + str + apos; // quote with apostrophes
			else if( str.IndexOf(quot) < 0 ) // if no quotes are used in the string...
				return quot + str + quot; // quote with double quotes
			else // if both quotes and double-quotes are used in string
				throw new ArgumentException("XPath search string contains both single and double quotes.  No way to quote it.", "str");
		}
		/// <summary>
		/// Counts the number of times that one string occurs within another.
		/// </summary>
		/// <param name="str">
		/// The string to search for.
		/// </param>
		/// <param name="instr">
		/// The string to search within.
		/// </param>
		/// <returns>
		/// The number of times str occurs in instr.
		/// </returns>
		public static int OccurrencesOf(string str, string instr)
		{
			if( str == null || str.Length == 0 ) throw new ArgumentNullException("str");
			if( instr == null ) throw new ArgumentNullException("instr");

			int count = 0;
			for( int idxOfLastFound = instr.IndexOf(str); idxOfLastFound >= 0; idxOfLastFound = instr.IndexOf(str, idxOfLastFound+str.Length) )
				count++;

			return count;
		}
	}
}
