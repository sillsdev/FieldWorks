// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FlexDePluginTest.cs
// Responsibility: Greg Trihus
// Last reviewed:
//
// <remarks>
//		Unit tests for FlexDePlugin
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using NUnit.Framework;
using NMock;
using SIL.PublishingSolution;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Utils;

namespace FlexDePluginTests
{
	/// <summary>
	///This is a test class for FlexDePluginTest and is intended
	///to contain all FlexDePluginTest Unit Tests
	///</summary>
	[TestFixture]
	public class FlexDePluginTest : FlexDePlugin
	{
		/// <summary>Mock help provider</summary>
		private IMock helpProvider;

		/// <summary>Location of test files</summary>
		protected string _TestPath = Path.Combine(DirectoryFinder.FwSourceDirectory, @"LexText\FlexDePlugin\FlexDePluginTests\Input");

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
			helpProvider = new DynamicMock(typeof (SIL.FieldWorks.Common.COMInterfaces.IHelpTopicProvider));
			UtilityDlg expected = new UtilityDlg((SIL.FieldWorks.Common.COMInterfaces.IHelpTopicProvider)helpProvider.MockInstance);
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
		///A test for UsageReport
		/// NOTE: Test depends on user having an email client that puts the subject in the Window title.
		///</summary>
		[Test]
		[Ignore("Build machine will probably fail here.")]
		public void UsageReportTest()
		{
			FlexDePlugin target = new FlexDePlugin();
			string label = target.Label;
			ClearLaunchCount();
			string emailAddress = string.Empty;
			string topMessage = string.Empty;
			int noLaunches = 0;
			FlexDePlugin.UsageReport(emailAddress, topMessage, noLaunches);
			while (!MyProcess.KillProcess("Report 0 Launches"))
			{
			}
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
		///A test for Reporting
		/// NOTE: Test depends on user having an email client that puts the subject in the Window title.
		///</summary>
		[Test]
		[Ignore("Build machine will probably fail here.")]
		public void ReportingTest()
		{
			FlexDePlugin target = new FlexDePlugin();
			string label = target.Label;
			ClearLaunchCount();
			Reporting();
			Reporting();
			while (!MyProcess.KillProcess("Report 1 Launches"))
			{
			}
		}

		/// <summary>
		///A test for Process
		///</summary>
		//[Test]
		//public void ProcessTest()
		//{
		//    FlexDePlugin target = new FlexDePlugin(); // TODO: Initialize to an appropriate value
		//    target.Process();
		//    // TODO: A method that does not return a value cannot be verified.");
		//}

		/// <summary>
		///A test for OnSelection
		///</summary>
		[Test]
		public void OnSelectionTest()
		{
			FlexDePlugin target = new FlexDePlugin();
			helpProvider = new DynamicMock(typeof(SIL.FieldWorks.Common.COMInterfaces.IHelpTopicProvider));
			UtilityDlg exportDialog = new UtilityDlg((SIL.FieldWorks.Common.COMInterfaces.IHelpTopicProvider)helpProvider.MockInstance);
			target.Dialog = exportDialog;
			target.OnSelection();
			// NOTE: The only test is really that it doesn't crash. The variables set have not getters.
		}

		/// <summary>
		///A test for LoadUtilities
		///</summary>
		[Test]
		public void LoadUtilitiesTest()
		{
			FlexDePlugin target = new FlexDePlugin();
			helpProvider = new DynamicMock(typeof(SIL.FieldWorks.Common.COMInterfaces.IHelpTopicProvider));
			UtilityDlg exportDialog = new UtilityDlg((SIL.FieldWorks.Common.COMInterfaces.IHelpTopicProvider)helpProvider.MockInstance);
			target.Dialog = exportDialog;
			target.LoadUtilities();
			// NOTE: The only test is really that it doesn't crash. The variables set have not getters.
		}

		/// <summary>
		///A test for IncrementLaunchCount
		///</summary>
		[Test]
		public void IncrementLaunchCountTest()
		{
			FlexDePlugin target = new FlexDePlugin();
			string label = target.Label;
			RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", false).OpenSubKey("SIL", false).OpenSubKey("Fieldworks", false).OpenSubKey(Application.ProductName, false);
			int expected = int.Parse((string)key.GetValue(label, "0")) + 1;
			IncrementLaunchCount();
			key = Registry.CurrentUser.OpenSubKey("Software", false).OpenSubKey("SIL", false).OpenSubKey("Fieldworks", false).OpenSubKey(Application.ProductName, false);
			int actual = int.Parse((string)key.GetValue(label, "0"));
			Assert.AreEqual(expected,actual);
		}

		/// <summary>
		///A test for ExportTool
		///</summary>
		[Test]
		[ExpectedException("System.NullReferenceException")]
		public void ExportToolTest()
		{
			FlexDePlugin target = new FlexDePlugin();
			helpProvider = new DynamicMock(typeof(SIL.FieldWorks.Common.COMInterfaces.IHelpTopicProvider));
			UtilityDlg exportDialog = new UtilityDlg((SIL.FieldWorks.Common.COMInterfaces.IHelpTopicProvider)helpProvider.MockInstance);
			target.Dialog = exportDialog;
			string areaChoice = "lexicon";
			string toolChoice = "lexiconDictionary";
			string exportFormat = "ConfiguredXHTML";
			string filePath = Path.Combine(_TestPath, "main.xhtml");
			ExportTool(areaChoice, toolChoice, exportFormat, filePath);
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
		///A test for ClearLaunchCount
		///</summary>
		[Test]
		public void ClearLaunchCountTest()
		{
			FlexDePlugin target = new FlexDePlugin();
			string label = target.Label;
			ClearLaunchCount();
			RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", false).OpenSubKey("SIL", false).OpenSubKey("Fieldworks", false).OpenSubKey(Application.ProductName, false);
			int actual = int.Parse((string)key.GetValue(label, "0"));
			int expected = 0;
			Assert.AreEqual(expected,actual);
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
	}
}
