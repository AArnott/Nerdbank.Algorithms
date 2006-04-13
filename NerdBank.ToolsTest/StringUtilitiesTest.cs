using System;
using System.Collections.Specialized;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NerdBank.Tools;

namespace NerdBank.ToolsTest
{
	/// <summary>
	///This is a test class for StringUtilities and is intended
	///to contain all StringUtilities Unit Tests
	///</summary>
	[TestClass()]
	public class StringUtilitiesTest
	{
		const string input = "Dear $ParentName,\r\n\r\nYour child, $StudentName, is inviting you to participate in a school research \r\nproject.  Professors are offering students extra credit in their college course \r\nif they and their parents participate in this study.  $StudentName has already \r\ncompleted his or her survey and now needs your help to complete the \r\nassignment.  It will take you 20 to 30 minutes to fill out the survey.  \r\nClick below to learn more about this study:  \r\n\r\nhttp://www.projectready.edu?StudentID=$StudentID&ParentEmail=$ParentEmail\r\n\r\nYour help with this project is greatly appreciated. ";
		const string param1 = "$ParentName";
		const string param2 = "$StudentName";
		const string subst1 = "Ron";
		const string subst2 = "Andrew";

		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
		{
			get
			{
				return testContextInstance;
			}
			set
			{
				testContextInstance = value;
			}
		}

		/// <summary>
		///Initialize() is called once during test execution before
		///test methods in this test class are executed.
		///</summary>
		[TestInitialize()]
		public void Initialize()
		{
			//  TODO: Add test initialization code
		}

		/// <summary>
		///Cleanup() is called once during test execution after
		///test methods in this class have executed unless
		///this test class' Initialize() method throws an exception.
		///</summary>
		[TestCleanup()]
		public void Cleanup()
		{
			//  TODO: Add test cleanup code
		}


		/// <summary>
		///A test case for SwapVarsForValues (string, System.Collections.Specialized.StringDictionary)
		///</summary>
		[TestMethod()]
		public void SwapVarsForValuesDictionaryTest()
		{
			StringDictionary varsAndValues = new StringDictionary();
			varsAndValues.Add(param1, subst1);
			varsAndValues.Add(param2, subst2);

			string expected = input.Replace(param1, subst1).Replace(param2, subst2);
			string actual;

			actual = StringUtilities.SwapVarsForValues(input, varsAndValues);

			Assert.AreEqual(expected, actual, "StringUtilities.SwapVarsForValues did not return the expected value.");
		}

		/// <summary>
		///A test case for SwapVarsForValues (string, string, string)
		///</summary>
		[TestMethod()]
		public void SwapVarsForValuesStringsTest()
		{
			string sInput = input;
			string sMatch = param1;
			string sValue = subst1;

			string expected = input.Replace(param1, subst1);
			string actual;

			actual = StringUtilities.SwapVarsForValues(sInput, sMatch, sValue);

			Assert.AreEqual(expected, actual, "StringUtilities.SwapVarsForValues did not return the expected value.");
		}


		/// <summary>
		///A test case for QuoteXPathString (string)
		///</summary>
		[TestMethod()]
		public void QuoteXPathStringTest()
		{
			string str = "test";

			string expected = "'test'";
			string actual;

			actual = StringUtilities.QuoteXPathString(str);

			Assert.AreEqual(expected, actual, "StringUtilities.QuoteXPathString did not return the expected value.");
		}

		/// <summary>
		///A test case for QuoteXPathString (string)
		///</summary>
		[TestMethod()]
		public void QuoteXPathStringWithSinglesTest()
		{
			string str = "te'st";

			string expected = "\"te'st\"";
			string actual;

			actual = StringUtilities.QuoteXPathString(str);

			Assert.AreEqual(expected, actual, "StringUtilities.QuoteXPathString did not return the expected value.");
		}

		/// <summary>
		///A test case for QuoteXPathString (string)
		///</summary>
		[TestMethod()]
		public void QuoteXPathStringWithDoublesTest()
		{
			string str = "te\"st";

			string expected = "'te\"st'";
			string actual;

			actual = StringUtilities.QuoteXPathString(str);

			Assert.AreEqual(expected, actual, "StringUtilities.QuoteXPathString did not return the expected value.");
		}

		/// <summary>
		///A test case for QuoteXPathString (string)
		///</summary>
		[TestMethod()]
		[ExpectedException(typeof(ArgumentException))]
		public void QuoteXPathStringWithBothTest()
		{
			string str = "te'\"st";
			StringUtilities.QuoteXPathString(str);
		}

		/// <summary>
		///A test case for Tokenize (string)
		///</summary>
		[TestMethod()]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TokenizeNullTest()
		{
			string str = null; // TODO: Initialize to an appropriate value
			StringUtilities.Tokenize(str);
		}

		[TestMethod()]
		public void OccurrencesOf1()
		{
			Assert.AreEqual(3, StringUtilities.OccurrencesOf("a", "abcdefg abc a"));
		}
		[TestMethod()]
		public void OccurrencesOf2()
		{
			Assert.AreEqual(2, StringUtilities.OccurrencesOf("abc", "abcdefg abc a"));
		}
		[TestMethod()]
		public void OccurrencesOf3()
		{
			Assert.AreEqual(2, StringUtilities.OccurrencesOf("c", "abcdefg abc a"));
		}
		[TestMethod()]
		public void OccurrencesOf4()
		{
			Assert.AreEqual(3, StringUtilities.OccurrencesOf("c", "ccc"));
		}
		[TestMethod()]
		public void OccurrencesOf5()
		{
			Assert.AreEqual(1, StringUtilities.OccurrencesOf("chc", "chchc"));
		}
		[TestMethod()]
		public void OccurrencesOf6()
		{
			Assert.AreEqual(0, StringUtilities.OccurrencesOf("abc", ""));
		}
		[TestMethod()]
		[ExpectedException(typeof(ArgumentNullException))]
		public void OccurrencesOfNull1()
		{
			StringUtilities.OccurrencesOf("abc", null);
		}
		[TestMethod()]
		[ExpectedException(typeof(ArgumentNullException))]
		public void OccurrencesOfNull2()
		{
			StringUtilities.OccurrencesOf(null, "");
		}
		[TestMethod()]
		[ExpectedException(typeof(ArgumentNullException))]
		public void OccurrencesOfNull3()
		{
			StringUtilities.OccurrencesOf("", "abc");
		}

	}


}
