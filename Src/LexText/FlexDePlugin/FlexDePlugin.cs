// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright file="FlexDePlugin.cs" from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
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
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Reflection;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.XWorks;
using XCore;

namespace SIL.PublishingSolution
{
	#region Class FlexDePlugin

	/// <summary>
	/// Implements Fieldworks Utility Interface for DictionaryExpress
	/// </summary>
	public class FlexDePlugin : IUtility
	{
		#region IUtility implementation

		#region Private Variable
		/// <summary>
		/// provides access to the dialog box, mediator and cache
		/// </summary>
		UtilityDlg exportDialog;

		/// <summary>
		/// Label used for listbox of utilities and some registry variables
		/// </summary>
		static string utilityLabel;

		/// <summary>
		/// Default css for the Flex
		/// </summary>
		const string ExpCss = "main.css";

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
			exportDialog.WhatDescription = "Enable output via XHTML to Open Office, Prince PDF, XeTeX, and other formats.";
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
		public void Process()
		{
			const string MainXhtml = "main.xhtml";
			const string RevXhtml = "FlexRev.xhtml";
			const string SketchXml = "sketch.xml";

			IExportContents sf = DynamicLoader.CreateObject("CssDialog.dll", "SIL.PublishingSolution.Contents") as IExportContents;
			Debug.Assert(sf != null);
			FdoCache cache = (FdoCache)exportDialog.Mediator.PropertyTable.GetValue("cache");
			sf.DatabaseName = cache.DatabaseName;
			sf.ExportReversal = sf.ReversalExists = ContentsExists("lexicon", "reversalToolEditComplete", "ReversalIndexXHTML");
			sf.GrammarExists = false;

			DialogResult result = sf.ShowDialog();
			if (result == DialogResult.Cancel)
				return;

			string outPath = Path.Combine(sf.OutputLocationPath, sf.DictionaryName);

			if(sf.ExistingDirectoryInput)
			{
				string inputPath = sf.ExistingDirectoryLocationPath;
				if (inputPath != outPath)
				{
					string dirFilter = string.Empty;
					if(sf.OutputLocationPath == inputPath)
					{
						dirFilter = sf.DictionaryName;
					}
					try
					{
						if (!MyFolders.Copy(inputPath, outPath, dirFilter)) return;
					}
					catch (Exception ex)
					{

						MessageBox.Show(ex.Message);
						return;
					}
				}
			}

			if (!MyFolders.CreateDirectory(outPath)) return;

			string mainFullName = Path.Combine(outPath, MainXhtml);
			string revFullXhtml = Path.Combine(outPath, RevXhtml);
			string gramFullName = Path.Combine(outPath, SketchXml);
			if (!sf.ExportMain)
				mainFullName = "";
			if (!sf.ExportReversal)
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

			IExporter exporter = DynamicLoader.CreateObject("PsExport.dll", "SIL.PublishingSolution.PsExport") as IExporter;
			Debug.Assert(exporter != null);
			exporter.DataType = "Dictionary";
			exporter.ProgressBar = exportDialog.ProgressBar;
			exporter.Export(mainFullName != "" ? mainFullName : revFullXhtml);

			Reporting();
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
			xDoc.XmlResolver = null;
			xDoc.Load(xml);
		}
		#endregion Process
		#endregion IUtility implementation

		#region Protected Function
		#region DeFlexExports
		/// <summary>
		/// Export process from Fieldworks Language explorer
		/// </summary>
		/// <param name="expCss">Style sheet exported</param>
		/// <param name="mainFullName">Source of main dictionary</param>
		/// <param name="revFullXhtml">source of reversal Index if available in Xhtml format</param>
		/// <param name="revFullXhtml">source of reversal Index</param>
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
					ExportTool("lexicon", "reversalToolEditComplete", "ReversalIndexXHTML", revFullXhtml);
					currInput = "Reversal";
					ValidXmlFile(revFullXhtml);

				}
			}
			catch (FileNotFoundException )
			{
				MessageBox.Show("The " + currInput + " Section may be Empty (or) Not exported", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
		protected void ExportTool(string areaChoice, string toolChoice, string exportFormat, string filePath)
		{
			if (File.Exists(filePath))
				File.Delete(filePath);
			if (!ChangeAreaTool(areaChoice, toolChoice))
				return;
			Mediator mediator = exportDialog.Mediator;
			mediator.PropertyTable.SetProperty("ExportDir", Path.GetDirectoryName(filePath));
			mediator.PropertyTable.SetPropertyPersistence("ExportDir", true);
			DeExportDialog ed = new DeExportDialog(mediator);
			ed.Show();
			ed.Visible = false;
			if (ed.SelectItem(exportFormat))
			ed.DeDoExport(filePath);
		}

		/// <summary>
		/// Sets the Flex Area and Tool and executes the Run export
		/// </summary>
		/// <param name="areaChoice">Area to choose</param>
		/// <param name="toolChoice">Tool to choose</param>
		/// <param name="exportFormat">Part of path for format of file to export</param>
		protected bool ContentsExists(string areaChoice, string toolChoice, string exportFormat)
		{
			if (!ChangeAreaTool(areaChoice, toolChoice))
				return false;
			Mediator mediator = exportDialog.Mediator;
			DeExportDialog ed = new DeExportDialog(mediator);
			ed.Show();
			ed.Visible = false;
			return ed.SelectItem(exportFormat);
		}

		/// <summary>
		/// Sets the Flex Area and Tool
		/// </summary>
		/// <param name="areaChoice">Area to choose</param>
		/// <param name="toolChoice">Tool to choose</param>
		/// <returns>True if possible</returns>
		protected bool ChangeAreaTool(string areaChoice, string toolChoice)
		{
			Mediator mediator = exportDialog.Mediator;
			object current = mediator.PropertyTable.GetValue("currentContentControlObject");
			//MessageBox.Show("AreaName=" + ((IxCoreContentControl)current).AreaName);
			if (((IxCoreContentControl)current).AreaName != areaChoice)
			{
				mediator.PropertyTable.SetProperty("areaChoice", areaChoice);
				mediator.PropertyTable.SetPropertyPersistence("areaChoice", false);
				while (mediator.JobItems > 0)
					mediator.ProcessItem();
			}
			string toolSelector = "ToolForAreaNamed_" + areaChoice;
			string toolName = mediator.PropertyTable.GetStringProperty(toolSelector, "");
			//MessageBox.Show("toolName=" + toolName);
			if (toolName != toolChoice)
			{
				string xpath = string.Format("//item[@value='lexicon']/parameters/tools/tool[@value = '{0}']", toolChoice);
				XmlNode windowConfiguration = (XmlNode)mediator.PropertyTable.GetValue("WindowConfiguration");
				XmlNode node = windowConfiguration.SelectSingleNode(xpath);
				if (node == null)
					return false;
				mediator.PropertyTable.SetProperty("currentContentControlParameters", node.SelectSingleNode("control"));
				mediator.PropertyTable.SetPropertyPersistence("currentContentControlParameters", false);
				mediator.PropertyTable.SetProperty("currentContentControl", toolChoice);
				mediator.PropertyTable.SetPropertyPersistence("currentContentControl", false);
				while (mediator.JobItems > 0)
					mediator.ProcessItem();
			}
			return true;
		}
		#endregion DeFlexExports
		#endregion Protected Function

		#region Reporting
		/// <summary>
		/// This sends the email reports if appropriate.
		/// </summary>
		protected static void Reporting()
		{
			IncrementLaunchCount();
			UsageReport("pathway@sil.org", string.Format("1. What do you hope {0} will do for you?%0A%0A2. What language are you work on?", utilityLabel), 1);
			UsageReport("pathway@sil.org", string.Format("1. Do you have suggestions to improve the program?%0A%0A2. What are you happy with?"), 10);
			UsageReport("pathway@sil.org", string.Format("1. What would you like to say to others about {0}?%0A%0A2. What languages have you used with {0}", utilityLabel), 40);
		}

		/// <summary>
		/// call this each time the application is launched if you have launch count-based reporting
		/// </summary>
		public static void IncrementLaunchCount()
		{
			int launchCount = 1 + int.Parse(Utils.UsageEmailDialog.RegistryAccess.GetStringRegistryValue(utilityLabel, "0"));
			Utils.UsageEmailDialog.RegistryAccess.SetStringRegistryValue(utilityLabel, launchCount.ToString());
		}

		/// <summary>
		/// used for testing purposes
		/// </summary>
		public static void ClearLaunchCount()
		{
			Utils.UsageEmailDialog.RegistryAccess.SetStringRegistryValue(utilityLabel, "0");
		}

		/// <summary>
		/// Generates an email with the <paramref name="topMessage">topMessage</paramref> if the launch count = <paramref name="noLaunches">noLaunches</paramref>.
		/// </summary>
		/// <param name="emailAddress">email address to send report to</param>
		/// <param name="topMessage">Message for report</param>
		/// <param name="noLaunches">number of launches at which report should be generated.</param>
		public static void UsageReport(string emailAddress, string topMessage, int noLaunches)
		{
			int launchCount = int.Parse(Utils.UsageEmailDialog.RegistryAccess.GetStringRegistryValue(utilityLabel, "0"));

			if (launchCount == noLaunches)
			{
				// Set the Application label to the name of the app
				Assembly assembly = Assembly.GetExecutingAssembly();
				IExporter exporter = DynamicLoader.CreateObject("PsExport.dll", "SIL.PublishingSolution.PsExport") as IExporter;
				if (exporter != null)
					assembly = exporter.GetType().Assembly;
				string version = Application.ProductVersion;

				if (assembly != null)
				{
					object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
					version = (attributes != null && attributes.Length > 0) ?
						((AssemblyFileVersionAttribute)attributes[0]).Version : Application.ProductVersion;
				}

				string emailSubject = string.Format("{0} {1} Report {2} Launches", utilityLabel, version, launchCount);
				string emailBody = string.Format("<report app='{0}' version='{1}'><stat type='launches' value='{2}'/></report>", utilityLabel, version, launchCount);
				string body = emailBody.Replace(Environment.NewLine, "%0A").Replace("\"", "%22").Replace("&", "%26");

				Process p = new Process();
				p.StartInfo.FileName = String.Format("mailto:{0}?subject={1}&body={2}", emailAddress, emailSubject, body);
				p.Start();
			}
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
		public DeExportDialog(Mediator mediator) : base(mediator)
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
