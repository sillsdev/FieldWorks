// Copyright (c) 2009-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// <remarks>
//		Unit tests for FlexDePlugin
// </remarks>

using System;
using System.IO;
using System.Xml;
using NUnit.Framework;
using NMock;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.PublishingSolution;
using SIL.FieldWorks.Common.FwUtils;

namespace FlexDePluginTests
{
	/// <summary>
	///This is a test class for FlexDePluginTest and is intended
	///to contain all FlexDePluginTest Unit Tests
	///</summary>
	[TestFixture]
	public class FlexPathwayPluginTest : FlexPathwayPlugin
	{
		/// <summary>Mock help provider</summary>
		private IMock helpProvider;

		/// <summary>Location of test files</summary>
		protected string _TestPath;
		//protected string _TestPath = Path.Combine(@"..\..\src", @"LexText\FlexDePlugin\FlexDePluginTests\Input");
		//protected string _TestPath = Path.Combine(DirectoryFinder.SourceDirectory, @"LexText\FlexDePlugin\FlexDePluginTests\Input");

		/// <summary>
		/// Runs before all tests. CompanyName must be forced b/c Resharper sets it to itself
		/// </summary>
		[OneTimeSetUp]
		public void TestFixtureSetup()
		{
			var path = String.Format("LexText{0}FlexPathwayPlugin{0}FlexPathwayPluginTests{0}Input", Path.DirectorySeparatorChar);
			_TestPath = Path.Combine(FwDirectoryFinder.SourceDirectory, path);
		}

		/// <summary>
		///A test for Label
		///</summary>
		[Test]
		public void LabelTest()
		{
			FlexPathwayPlugin target = new FlexPathwayPlugin();
			string actual;
			actual = target.Label;
			Assert.AreEqual("Pathway", actual);
		}

		/// <summary>
		///A test for Dialog
		///</summary>
		[Test]
		public void DialogTest()
		{
			FlexPathwayPlugin target = new FlexPathwayPlugin();
			helpProvider = new DynamicMock(typeof (IHelpTopicProvider));
			using (UtilityDlg expected = new UtilityDlg((IHelpTopicProvider)helpProvider.MockInstance))
				target.Dialog = expected;
		}

		/// <summary>
		///A test for ValidXmlFile
		///</summary>
		[Test]
		public void ValidXmlFileNullTest()
		{
			string xml = string.Empty;
			Assert.That(() => ValidXmlFile(xml), Throws.TypeOf<FileNotFoundException>());
		}

		/// <summary>
		///A test for ValidXmlFile
		///</summary>
		[Test]
		public void ValidXmlFileSuccessTest()
		{
			string xml = Path.Combine(_TestPath, "T1.xhtml");
			ValidXmlFile(xml);
		}

		/// <summary>
		///A test for ValidXmlFile
		///</summary>
		[Test]
		public void ValidXmlFileFailedTest()
		{
			string xml = Path.Combine(_TestPath, "T1-bad.xhtml");
			Assert.That(() => ValidXmlFile(xml), Throws.TypeOf<XmlException>());
		}

		/// <summary>
		///A test for ToString
		///</summary>
		[Test]
		public void ToStringTest()
		{
			FlexPathwayPlugin target = new FlexPathwayPlugin();
			string expected = "Pathway";
			string actual;
			actual = target.ToString();
			Assert.AreEqual(expected, actual);
		}

		/// <summary>
		///A test for OnSelection
		///</summary>
		[Test]
		public void OnSelectionTest()
		{
			FlexPathwayPlugin target = new FlexPathwayPlugin();
			helpProvider = new DynamicMock(typeof(IHelpTopicProvider));
			using (UtilityDlg exportDialog = new UtilityDlg((IHelpTopicProvider)helpProvider.MockInstance))
			{
				target.Dialog = exportDialog;
				target.OnSelection();
				// NOTE: The only test is really that it doesn't crash. The variables set have not getters.
			}
		}

		/// <summary>
		///A test for LoadUtilities
		///</summary>
		[Test]
		public void LoadUtilitiesTest()
		{
			FlexPathwayPlugin target = new FlexPathwayPlugin();
			helpProvider = new DynamicMock(typeof(IHelpTopicProvider));
			using (UtilityDlg exportDialog = new UtilityDlg((IHelpTopicProvider)helpProvider.MockInstance))
			{
				target.Dialog = exportDialog;
				target.LoadUtilities();
				// NOTE: The only test is really that it doesn't crash. The variables set have not getters.
			}
		}

		/// <summary>
		///A test for ExportTool
		///</summary>
		[Test]
		public void ExportToolTest()
		{
			FlexPathwayPlugin target = new FlexPathwayPlugin();
			helpProvider = new DynamicMock(typeof(IHelpTopicProvider));
			using (UtilityDlg exportDialog = new UtilityDlg((IHelpTopicProvider)helpProvider.MockInstance))
			{
				target.Dialog = exportDialog;
				string areaChoice = "lexicon";
				string toolChoice = "lexiconDictionary";
				string exportFormat = "ConfiguredXHTML";
				string filePath = Path.Combine(_TestPath, "main.xhtml");
				Assert.That(() => ExportTool(areaChoice, toolChoice, exportFormat, filePath), Throws.TypeOf<NullReferenceException>());
			}
		}

		/// <summary>
		///A Null test for DeFlexExports
		///</summary>
		[Test]
		public void DeFlexExportsNullTest()
		{
			string expCss = "";
			string mainFullName = "";
			string revFullXhtml = "";
			string gramFullName = "";
			Assert.That(() => DeFlexExports(expCss, mainFullName, revFullXhtml, gramFullName), Throws.TypeOf<NullReferenceException>());
		}

		/// <summary>
		///A test for DeFlexExports
		///</summary>
		[Test]
		public void DeFlexExportsFailTest()
		{
			string expCss = Path.Combine(_TestPath, "T1.css");
			string mainFullName = Path.Combine(_TestPath, "T1.css");
			string revFullXhtml = "";
			string gramFullName = "";
			Assert.That(DeFlexExports(expCss, mainFullName, revFullXhtml, gramFullName), Is.False);
		}

		/// <summary>
		///A test for ChangeAreaTool
		///</summary>
		[Test]
		public void ChangeAreaToolNullTest()
		{
			string areaChoice = string.Empty;
			string toolChoice = string.Empty;
			Assert.That(() => ChangeAreaTool(areaChoice, toolChoice), Throws.TypeOf<NullReferenceException>());
		}

		/// <summary>
		///A test for ChangeAreaTool
		///</summary>
		[Test]
		public void ChangeAreaToolFailTest()
		{
			string areaChoice = "lexicon";
			string toolChoice = "lexiconDictionary";
			Assert.That(() =>  ChangeAreaTool(areaChoice, toolChoice), Throws.TypeOf<NullReferenceException>());
		}

		/// <summary>
		///A test for FlexDePlugin Constructor
		///</summary>
		[Test]
		public void FlexDePluginConstructorTest()
		{
			FlexPathwayPlugin target = new FlexPathwayPlugin();
			// TODO: TODO: Implement code to verify target");
		}
	}
}
