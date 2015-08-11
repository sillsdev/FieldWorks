// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// <author>Greg Trihus</author>
// <email>greg_trihus@sil.org</email>
// Last reviewed:
//
// <remarks>
//		FlexDePlugin - Utility for running Pathway from Flex
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Reflection;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Microsoft.Win32;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.FieldWorks.XWorks;
using XCore;
using SIL.FieldWorks.FwCoreDlgs;
using System.Diagnostics.CodeAnalysis;
using SIL.CoreImpl;

namespace SIL.PublishingSolution
{
	#region Class FlexDePlugin

	/// <summary>
	/// Implements Fieldworks Utility Interface for DictionaryExpress
	/// </summary>
	public class FlexDePlugin : IUtility, IFeedbackInfoProvider
	{
		#region IUtility implementation

		#region Private Variable
		/// <summary>
		/// provides access to the dialog box, mediator and cache
		/// </summary>
		private UtilityDlg exportDialog;

		/// <summary>
		/// Label used for listbox of utilities and some registry variables
		/// </summary>
		private static string utilityLabel;

		/// <summary>
		/// Default css for the Flex
		/// </summary>
		private const string ExpCss = "main.css";

		#endregion Private Variable

		#region get.Label
		/// <summary>
		/// Gets the main label describing the utility.
		/// </summary>
		public string Label
		{
			get
			{
				//Debug.Assert(exportDialog != null);
				utilityLabel = "Pathway";
				return utilityLabel;
			}
		}

		/// <summary>
		/// The utility is represented by its label
		/// </summary>
		/// <returns>Label property</returns>
		public override string ToString()
		{
			return Label;
		}
		#endregion get.Label

		#region set.Dialog
		/// <summary>
		/// Sets the UtilityDlg.
		/// </summary>
		public UtilityDlg Dialog
		{
			set
			{
				Debug.Assert(value != null);
				Debug.Assert(exportDialog == null);
				exportDialog = value;
			}
		}
		#endregion set.Dialog

		#region OnSelection
		/// <summary>
		/// Notify the utility is has been selected in the dlg.
		/// </summary>
		public void OnSelection()
		{
			Debug.Assert(exportDialog != null);
			exportDialog.WhatDescription = "Enable output via XHTML to Open/Libre Office, Prince PDF, XeTeX, and other formats.";
			exportDialog.WhenDescription = "Preparing the configured dictionary or reversal for output.";
			exportDialog.RedoDescription = "You may want to sort, filter and configure you the data before you use this Pathway tool to process it. You may want to process the main section and reversal section separately even though the tool allows them to be processed all at once.";
		}
		#endregion OnSelection

		#region LoadUtilities
		/// <summary>
		/// Load 0 or more items in the list box.
		/// </summary>
		public void LoadUtilities()
		{
			Debug.Assert(exportDialog != null);
			exportDialog.Utilities.Items.Add(this);
		}
		#endregion LoadUtilities

		#region Process
		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "applicationKey is a reference")]
		public void Process()
		{
			if (!PathwayUtils.IsPathwayInstalled)
			{
				MessageBox.Show(ResourceHelper.GetResourceString("kstidInvalidPathwayInstallation"),
					string.Empty, MessageBoxButtons.OK);
				return;
			}

			const string MainXhtml = "main.xhtml";
			const string RevXhtml = "FlexRev.xhtml";
			const string SketchXml = "sketch.xml";

			IApp app = exportDialog.PropTable.GetValue<IApp>("App");
			string cssDialog = Path.Combine(PathwayUtils.PathwayInstallDirectory, "CssDialog.dll");
			var sf = ReflectionHelper.CreateObject(cssDialog, "SIL.PublishingSolution.Contents", null);
			Debug.Assert(sf != null);
			FdoCache cache = exportDialog.PropTable.GetValue<FdoCache>("cache");
			ReflectionHelper.SetProperty(sf, "DatabaseName", cache.ProjectId.Name);
			bool fContentsExists = ContentsExists("lexicon", "reversalEditComplete", "ReversalIndexXHTML");
			ReflectionHelper.SetProperty(sf, "ExportReversal", fContentsExists);
			ReflectionHelper.SetProperty(sf, "ReversalExists", fContentsExists);
			ReflectionHelper.SetProperty(sf, "GrammarExists", false);

			DialogResult result = (DialogResult)ReflectionHelper.GetResult(sf, "ShowDialog");
			if (result == DialogResult.Cancel)
				return;

			string strOutputPath = (string)ReflectionHelper.GetProperty(sf, "OutputLocationPath");
			string strDictionaryName = (string)ReflectionHelper.GetProperty(sf, "DictionaryName");
			string outPath = Path.Combine(strOutputPath, strDictionaryName);

			bool fExistingDirectoryInput = (bool)ReflectionHelper.GetProperty(sf, "ExistingDirectoryInput");
			if(fExistingDirectoryInput)
			{
				string inputPath = (string)ReflectionHelper.GetProperty(sf, "ExistingDirectoryLocationPath");
				if (inputPath != outPath)
				{
					string dirFilter = string.Empty;
					if(strOutputPath == inputPath)
					{
						dirFilter = strDictionaryName;
					}
					try
					{
						if (!MyFolders.Copy(inputPath, outPath, dirFilter, app.ApplicationName)) return;
					}
					catch (Exception ex)
					{

						MessageBox.Show(ex.Message);
						return;
					}
				}
			}

			if (!MyFolders.CreateDirectory(outPath, app.ApplicationName)) return;

			string mainFullName = Path.Combine(outPath, MainXhtml);
			string revFullXhtml = Path.Combine(outPath, RevXhtml);
			string gramFullName = Path.Combine(outPath, SketchXml);
			if (!(bool)ReflectionHelper.GetProperty(sf, "ExportMain"))
				mainFullName = "";
			if (!fContentsExists)
			{
				revFullXhtml = "";
			}

			switch (result)
			{
				// No = Skip export of data from Flex but still prepare exported output (ODT, PDF or whatever)
				case DialogResult.No:
					break;

				case DialogResult.Yes:
					if (!DeFlexExports(ExpCss, mainFullName, revFullXhtml, gramFullName))
						return;
					break;
			}

			string psExport = Path.Combine(PathwayUtils.PathwayInstallDirectory, "PsExport.dll");
			var exporter = ReflectionHelper.CreateObject(psExport, "SIL.PublishingSolution.PsExport", null);
			Debug.Assert(exporter != null);
			ReflectionHelper.SetProperty(exporter, "DataType", "Dictionary");
			ReflectionHelper.SetProperty(exporter, "ProgressBar", exportDialog.ProgressBar);
			ReflectionHelper.CallMethod(exporter, "Export", mainFullName != "" ? mainFullName : revFullXhtml);

			RegistryKey applicationKey = app.SettingsKey;
			UsageEmailDialog.IncrementLaunchCount(applicationKey);
			Assembly assembly = exporter.GetType().Assembly;

			UsageEmailDialog.DoTrivialUsageReport(utilityLabel, applicationKey, FeedbackEmailAddress,
				string.Format("1. What do you hope {0} will do for you?%0A%0A2. What languages are you working on?", utilityLabel),
				false, 1, assembly);
			UsageEmailDialog.DoTrivialUsageReport(utilityLabel, applicationKey, FeedbackEmailAddress,
				string.Format("1. Do you have suggestions to improve the program?%0A%0A2. What are you happy with?"),
				false, 10, assembly);
			UsageEmailDialog.DoTrivialUsageReport(utilityLabel, applicationKey, FeedbackEmailAddress,
				string.Format("1. What would you like to say to others about {0}?%0A%0A2. What languages have you used with {0}", utilityLabel),
				false, 40, assembly);
		}

		/// <summary>
		/// Validating the xml file with xmldocument to avoid further processing.
		/// </summary>
		/// <param name="xml">Xml file Name for Validating</param>
		/// <exception cref="FileNotFoundException">if xml file missing</exception>
		/// <exception cref="XmlException">if xml file won't load</exception>
		protected static void ValidXmlFile(string xml)
		{
			if (!File.Exists(xml))
				throw new FileNotFoundException();
			XmlDocument xDoc = new XmlDocument();
			using (var stream = new FileStream(xml, FileMode.Open))
			{
				xDoc.XmlResolver = FileStreamXmlResolver.GetNullResolver(); // Null may not work on Mono; not trying to validate any URLs.
				xDoc.Load(stream);
			}
		}
		#endregion Process
		#endregion IUtility implementation

		#region DeFlexExports
		/// <summary>
		/// Export process from Fieldworks Language explorer
		/// </summary>
		/// <param name="expCss">Style sheet exported</param>
		/// <param name="mainFullName">Source of main dictionary</param>
		/// <param name="revFullXhtml">source of reversal Index if available in Xhtml format</param>
		/// <param name="gramFullName">Source of grammar</param>
		/// <returns>True if there is something to do</returns>
		protected bool DeFlexExports(string expCss, string mainFullName, string revFullXhtml, string gramFullName)
		{

			if (File.Exists(mainFullName))
				File.Delete(mainFullName);

			if (File.Exists(revFullXhtml))
			File.Delete(revFullXhtml);

			if (File.Exists(gramFullName))
			File.Delete(gramFullName);
			string currInput = string.Empty;
			try
			{
				if (mainFullName != "")
				{
					ExportTool("lexicon", "lexiconDictionary", "ConfiguredXHTML", mainFullName);
					currInput = "Main";
					ValidXmlFile(mainFullName);
				}
				if (revFullXhtml != "")
				{
					ExportTool("lexicon", "reversalEditComplete", "ReversalIndexXHTML", revFullXhtml);
					currInput = "Reversal";
					ValidXmlFile(revFullXhtml);

				}
			}
			catch (FileNotFoundException )
			{
				IApp app = exportDialog.PropTable.GetValue<IApp>("App");
				MessageBox.Show("The " + currInput + " Section may be Empty (or) Not exported", app.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;

			}
			catch (Exception)
			{
				return false;
			}

			//The grammar currently doesn't have an export function!
			//DoExport("grammar", "grammarSketch", "", sketchXml);
			//if (!ValidXmlFile(gramFullName))
			//    return;

			ChangeAreaTool("lexicon", "lexiconDictionary");
			return true;
		}

		/// <summary>
		/// Sets the Flex Area and Tool and executes the Run export
		/// </summary>
		/// <param name="areaChoice">Area to choose</param>
		/// <param name="toolChoice">Tool to choose</param>
		/// <param name="exportFormat">Part of path for format of file to export</param>
		/// <param name="filePath">path for file to export</param>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "mediator is a reference")]
		protected void ExportTool(string areaChoice, string toolChoice, string exportFormat, string filePath)
		{
			if (File.Exists(filePath))
				File.Delete(filePath);
			if (!ChangeAreaTool(areaChoice, toolChoice))
				return;
			exportDialog.PropTable.SetProperty("ExportDir", Path.GetDirectoryName(filePath), true, true);
			using (DeExportDialog ed = new DeExportDialog(exportDialog.Mediator, exportDialog.PropTable))
			{
				ed.Show();
				ed.Visible = false;
				if (ed.SelectItem(exportFormat))
					ed.DeDoExport(filePath);
			}
		}

		/// <summary>
		/// Sets the Flex Area and Tool and executes the Run export
		/// </summary>
		/// <param name="areaChoice">Area to choose</param>
		/// <param name="toolChoice">Tool to choose</param>
		/// <param name="exportFormat">Part of path for format of file to export</param>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "mediator is a reference")]
		protected bool ContentsExists(string areaChoice, string toolChoice, string exportFormat)
		{
			if (!ChangeAreaTool(areaChoice, toolChoice))
				return false;
			using (DeExportDialog ed = new DeExportDialog(exportDialog.Mediator, exportDialog.PropTable))
			{
				ed.Show();
				ed.Visible = false;
				return ed.SelectItem(exportFormat);
			}
		}

		/// <summary>
		/// Sets the Flex Area and Tool
		/// </summary>
		/// <param name="areaChoice">Area to choose</param>
		/// <param name="toolChoice">Tool to choose</param>
		/// <returns>True if possible</returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "mediator is a reference")]
		protected bool ChangeAreaTool(string areaChoice, string toolChoice)
		{
			var mediator = exportDialog.Mediator;
			var currentAreaControl = exportDialog.PropTable.GetValue<IxCoreContentControl>("currentContentControlObject");
			//MessageBox.Show("AreaName=" + ((IxCoreContentControl)current).AreaName);
			if (currentAreaControl.AreaName != areaChoice)
			{
				exportDialog.PropTable.SetProperty("areaChoice", areaChoice, false, true);
				while (mediator.JobItems > 0)
					mediator.ProcessItem();
			}
			string toolSelector = "ToolForAreaNamed_" + areaChoice;
			string toolName = exportDialog.PropTable.GetValue(toolSelector, string.Empty);
			//MessageBox.Show("toolName=" + toolName);
			if (toolName != toolChoice)
			{
				string xpath = string.Format("//item[@value='lexicon']/parameters/tools/tool[@value = '{0}']", toolChoice);
				XmlNode windowConfiguration = exportDialog.PropTable.GetValue<XmlNode>("WindowConfiguration");
				XmlNode node = windowConfiguration.SelectSingleNode(xpath);
				if (node == null)
					return false;
				exportDialog.PropTable.SetProperty("currentContentControlParameters", node.SelectSingleNode("control"), false, true);
				exportDialog.PropTable.SetProperty("currentContentControl", toolChoice, false, true);
				while (mediator.JobItems > 0)
					mediator.ProcessItem();
			}
			return true;
		}
		#endregion DeFlexExports

		#region IFeedbackInfoProvider Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the feedback e-mail address.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string FeedbackEmailAddress
		{
			get { return "pathway@sil.org"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the support e-mail address.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string SupportEmailAddress
		{
			get { return "pathway@sil.org"; }
		}

		#endregion
	}
	#endregion Class FlexDePlugin
}

namespace SIL.FieldWorks.XWorks
{
	#region DeExportDialog
	/// <summary>
	/// This creates a special version of ExportDialog so we can export without displaying dialogs
	/// </summary>
	public class DeExportDialog : ExportDialog
	{
		/// <summary>
		/// Initializes a new instance of the DeExportDialog class by calling the base class constuctor
		/// </summary>
		/// <param name="mediator">this is a pointer to the current state</param>
		/// <param name="propertyTable"></param>
		public DeExportDialog(Mediator mediator, IPropertyTable propertyTable)
			: base(mediator, propertyTable)
		{
		}

		/// <summary>
		/// Select the item containing exportFormat in the path name of the xml file name.
		/// </summary>
		/// <param name="exportFormat">The file name of the export xml file</param>
		/// <returns>True if the desired export format exists and is enabled.</returns>
		public bool SelectItem(string exportFormat)
		{
			foreach (ListViewItem lvi in m_exportList.Items)
			{
				if (lvi.Tag.ToString().Contains(exportFormat))
				{
					if (ItemDisabled(lvi.Tag.ToString()))
						return false;
					lvi.Selected = true;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Creates export file outPath.
		/// </summary>
		/// <param name="outPath">path of the file to output.</param>
		public void DeDoExport(string outPath)
		{
			DoExport(outPath);
		}
	}
	#endregion DeExportDialog
}
