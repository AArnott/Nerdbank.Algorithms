using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NerdBank.Tools;

namespace NerdBank.ToolsTest
{
	/// <summary>
	///This is a test class for EmbeddedResources and is intended
	///to contain all EmbeddedResources Unit Tests
	///</summary>
	[TestClass()]
	public class EmbeddedResourcesTest
	{
		private const string embeddedFilePath = "/embed test.txt";
		private const string embeddedFilePathPt = "/embed test.pt.txt";
		private const string embeddedFilePathZz = "/embed test.zz.txt";
		private const string embeddedFilePathAu = "/embed test.en-AU.txt";
		private const string DefaultNamespace = "NerdBank.ToolsTest";
		private const string embeddedFileContents = "HELLO";
		private const string embeddedFileContentsPt = "HELLO PT";
		private const string embeddedFileContentsZz = "HELLO ZZ";
		private const string embeddedFileContentsAu = "HELLO AU";



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


		[TestMethod()]
		public void ManifestNameFromFileName()
		{
			string expName = DefaultNamespace + embeddedFilePath.Replace('/', '.');
			string calcName = EmbeddedResources.ManifestNameFromFileNameAndNamespace(embeddedFilePath, DefaultNamespace);
			Assert.AreEqual(expName, calcName);
		}

		[TestMethod()]
		public void ManifestNameFromFileNameWithSpaces()
		{
			string path = "/some dir/some file.txt";
			string expected = DefaultNamespace + ".some_dir.some file.txt";
			Assert.AreEqual(expected, EmbeddedResources.ManifestNameFromFileNameAndNamespace(path, DefaultNamespace));
		}

		[TestMethod()]
		public void LoadFileFromAssemblyDefaultCultureTest()
		{
			string contents = EmbeddedResources.LoadFileFromAssemblyWithNamespace(embeddedFilePath, DefaultNamespace);
			Assert.AreEqual(embeddedFileContents, contents);
		}

		[TestMethod()]
		[DeploymentItem(@"NerdBank.ToolsTest\bin\Debug\pt\NerdBank.ToolsTest.resources.dll", "pt")]
		public void LoadFileFromAssemblyCultureTest()
		{
			string contents = EmbeddedResources.LoadFileFromAssemblyWithNamespace(embeddedFilePathPt, DefaultNamespace);
			Assert.AreEqual(embeddedFileContentsPt, contents);
		}

		[TestMethod()]
		[DeploymentItem(@"NerdBank.ToolsTest\bin\Debug\en-AU\NerdBank.ToolsTest.resources.dll", "en-AU")]
		public void LoadFileFromAssemblySubcultureTest()
		{
			string contents = EmbeddedResources.LoadFileFromAssemblyWithNamespace(embeddedFilePathAu, DefaultNamespace);
			Assert.AreEqual(embeddedFileContentsAu, contents);
		}

		[TestMethod()]
		public void LoadFileFromAssemblyFakeCultureTest()
		{
			string contents = EmbeddedResources.LoadFileFromAssemblyWithNamespace(embeddedFilePathZz, DefaultNamespace);
			Assert.AreEqual(embeddedFileContentsZz, contents);
		}

		[TestMethod()]
		public void GetStreamFromAssembly()
		{
			using (System.IO.Stream s = EmbeddedResources.GetFileStreamFromAssembly(embeddedFilePath, DefaultNamespace))
			{
				Assert.IsNotNull(s);
				Assert.IsTrue(s.Length == embeddedFileContents.Length);
			}
		}

	}


}
