// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FlexDePluginTest.cs
// Responsibility: Greg Trihus
// Last reviewed:
//
// <remarks>
//		Unit tests for FlexDePlugin
// </remarks>

using System;
using System.IO;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.PublishingSolution;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace FlexDePluginTests
{
	/// <summary>
	///This is a test class for FlexDePluginTest and is intended
	///to contain all FlexDePluginTest Unit Tests
	///</summary>
	[TestFixture]
	public class FlexDePluginTest : FlexDePlugin
	{
		private IHelpTopicProvider m_realFakeHelpProvider;

		/// <summary>Location of test files</summary>
		protected string _TestPath;
		//protected string _TestPath = Path.Combine(@"..\..\src", @"LexText\FlexDePlugin\FlexDePluginTests\Input");
		//protected string _TestPath = Path.Combine(DirectoryFinder.SourceDirectory, @"LexText\FlexDePlugin\FlexDePluginTests\Input");

		/// <summary>
		/// Runs before all tests. CompanyName must be forced b/c Resharper sets it to itself
		/// </summary>
		[TestFixtureSetUp]
		public void TestFixtureSetup()
		{
			// This needs to be set for ReSharper
			RegistryHelper.CompanyName = "SIL";
			RegistryHelper.ProductName = "FieldWorks";
			var path = String.Format("LexText{0}FlexDePlugin{0}FlexDePluginTests{0}Input", Path.DirectorySeparatorChar);
			_TestPath = Path.Combine(FwDirectoryFinder.SourceDirectory, path);
		}

		/// <summary>
		///A test for Label
		///</summary>
		[Test]
		public void LabelTest()
		{
			FlexDePlugin target = new FlexDePlugin();
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
			FlexDePlugin target = new FlexDePlugin();
			m_realFakeHelpProvider = new DummyHelpProvider();
			using (UtilityDlg expected = new UtilityDlg(m_realFakeHelpProvider))
				target.Dialog = expected;
		}

		/// <summary>
		///A test for ValidXmlFile
		///</summary>
		[Test]
		[ExpectedException("System.IO.FileNotFoundException")]
		public void ValidXmlFileNullTest()
		{
			string xml = string.Empty;
			ValidXmlFile(xml);
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
		[ExpectedException("System.Xml.XmlException")]
		public void ValidXmlFileFailedTest()
		{
			string xml = Path.Combine(_TestPath, "T1-bad.xhtml");
			ValidXmlFile(xml);
		}

		/// <summary>
		///A test for ToString
		///</summary>
		[Test]
		public void ToStringTest()
		{
			FlexDePlugin target = new FlexDePlugin();
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
			FlexDePlugin target = new FlexDePlugin();
			m_realFakeHelpProvider = new DummyHelpProvider();
			using (UtilityDlg exportDialog = new UtilityDlg(m_realFakeHelpProvider))
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
			FlexDePlugin target = new FlexDePlugin();
			m_realFakeHelpProvider = new DummyHelpProvider();
			using (UtilityDlg exportDialog = new UtilityDlg(m_realFakeHelpProvider))
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
		[ExpectedException("System.NullReferenceException")]
		public void ExportToolTest()
		{
			FlexDePlugin target = new FlexDePlugin();
			m_realFakeHelpProvider = new DummyHelpProvider();
			using (UtilityDlg exportDialog = new UtilityDlg(m_realFakeHelpProvider))
			{
				target.Dialog = exportDialog;
				string areaChoice = "lexicon";
				string toolChoice = "lexiconDictionary";
				string exportFormat = "ConfiguredXHTML";
				string filePath = Path.Combine(_TestPath, "main.xhtml");
				ExportTool(areaChoice, toolChoice, exportFormat, filePath);
			}
		}

		/// <summary>
		///A Null test for DeFlexExports
		///</summary>
		[Test]
		[ExpectedException("System.NullReferenceException")]
		public void DeFlexExportsNullTest()
		{
			string expCss = "";
			string mainFullName = "";
			string revFullXhtml = "";
			string gramFullName = "";
			bool expected = false;
			bool actual;
			actual = DeFlexExports(expCss, mainFullName, revFullXhtml, gramFullName);
			Assert.AreEqual(expected, actual);
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
			bool expected = false;
			bool actual;
			actual = DeFlexExports(expCss, mainFullName, revFullXhtml, gramFullName);
			Assert.AreEqual(expected, actual);
		}

		/// <summary>
		///A test for ChangeAreaTool
		///</summary>
		[Test]
		[ExpectedException("System.NullReferenceException")]
		public void ChangeAreaToolNullTest()
		{
			string areaChoice = string.Empty;
			string toolChoice = string.Empty;
			bool actual;
			actual = ChangeAreaTool(areaChoice, toolChoice);
		}

		/// <summary>
		///A test for ChangeAreaTool
		///</summary>
		[Test]
		[ExpectedException("System.NullReferenceException")]
		public void ChangeAreaToolFailTest()
		{
			string areaChoice = "lexicon";
			string toolChoice = "lexiconDictionary";
			bool actual;
			actual = ChangeAreaTool(areaChoice, toolChoice);
		}

		/// <summary>
		///A test for FlexDePlugin Constructor
		///</summary>
		[Test]
		public void FlexDePluginConstructorTest()
		{
			FlexDePlugin target = new FlexDePlugin();
			// TODO: TODO: Implement code to verify target");
		}

		private class DummyHelpProvider : IHelpTopicProvider
		{
			#region Implementation of IHelpTopicProvider

			/// ------------------------------------------------------------------------------------
			/// <summary>Gets a URL identifying a Help topic.</summary>
			/// <param name='ksPropName'>A constant identifier for the desired Help topic</param>
			/// ------------------------------------------------------------------------------------
			public string GetHelpString(string ksPropName)
			{
				return "Some help string";
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// The HTML Help file (.chm) for the app.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public string HelpFile { get; private set; }

			#endregion
		}
	}
}
