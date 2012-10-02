// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FxtViewer.cs
// Responsibility: AndyBlack
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.Resources;
using System.Text;
using Microsoft.Win32;

using XCore;
using SIL.FieldWorks;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.Common.FXT;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Widgets;
using MSXML2;
using MsHtmHstInterop;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Summary description for FxtViewer.
	/// </summary>
	/// <remarks>
	/// IxCoreColleague is included in the IxCoreContentControl definition.
	/// </remarks>
	public class FxtViewer : UserControl, IxCoreContentControl, IFWDisposable
	{
		#region Data Members
		/// <summary>
		/// The control that shows the HTML data.
		/// </summary>
		protected HtmlControl m_htmlControl;
		/// <summary>
		/// Mediator that passes off messages.
		/// </summary>
		protected XCore.Mediator m_mediator;
		/// <summary>
		/// FXT Dumper.
		/// </summary>
		protected XDumper m_fxtDumper;
		protected string m_outputDirectory;

		/// <summary>
		/// special nodes in config file
		/// </summary>
		XmlNode m_fxtNode;
		XmlNode m_transformsNode;
		XmlNode m_AlsoSaveTransformNode;
		/// <summary>
		/// counts for progress bar
		/// </summary>
		int m_cPrompts;
		int m_cTransforms;
		ResourceManager m_stringResMan;

		private string m_sHtmlFileName = null;
		private string m_sAlsoSaveFileName = null;
		private string m_sReplaceDoctype = null;
		private XmlNode m_configurationParameters;

		// These data members are set from the parameters.
		/// <summary>
		/// title for dialog box
		/// </summary>
		private string m_sProgressDialogTitle;
		private string m_sSaveAsWebpageDialogTitle;
		private string m_sAlsoSaveDialogTitle;
		/// <summary>
		/// Registry key name (from config file)
		/// </summary>
		private string m_sRegKeyName;
		/// Initial file name key in strings file to use when doing a save as webpage
		private string m_sFileNameKey;
		/// strings path where one can find the file name key above
		private string m_sStringsPath;

		private System.Windows.Forms.Button m_GenerateBtn;
		private System.Windows.Forms.Panel m_panelTop;
		private System.Windows.Forms.Panel m_panelBottom;
		private System.Windows.Forms.Button m_BackBtn;
		private System.Windows.Forms.Button m_ForwardBtn;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.ComponentModel.IContainer components;

		private const string m_skExtensionUri = "urn:xsltExtension-DateTime";
		private System.Windows.Forms.Button m_SaveAsHtmlBtn;

		/// <summary>
		/// since we are going to hide the tree bar, remember the state it was in so
		/// that when we go way we can restore it to the state it was in.
		/// </summary>
		private bool m_previousShowTreeBarValue;

		/// <summary>
		/// Back/Forward counter to keep track of state and control enable/disable of back adn forward buttons
		/// </summary>
		protected int m_iURLCounter;
		private System.Windows.Forms.ImageList imageList1;
		protected int m_iMaxURLCount;

		/// <summary>
		/// Registry constants
		/// </summary>
		private const string m_ksHtmlFilePath = "HtmlFilePath";
		private const string m_ksAlsoSaveFilePath = "AlsoSaveFilePath";

		#endregion // Data Members

		#region Properties

		/// <summary>
		/// Path to transforms
		/// </summary>
		private string TransformPath
		{
			get { return DirectoryFinder.GetFWCodeSubDirectory(@"Language Explorer\Transforms"); }
		}
		/// <summary>
		/// Path to utility html files
		/// </summary>
		private string UtilityHtmlPath
		{
			get { return DirectoryFinder.GetFWCodeSubDirectory(@"Language Explorer\FxtViewer"); }
		}

		/// <summary>
		/// Name of FXT Dump to XLingPap transform
		/// </summary>
		private string DumpToXLingPap
		{
			get { return "FxtM3MorphologySketch.xsl"; }
		}

		/// <summary>
		/// Name of XLingPap DTD
		/// </summary>
		private string XLingPapDTD
		{
			get { return "XLingPap.dtd"; }
		}

		/// <summary>
		/// Name of XLingPap transform
		/// </summary>
		private string XLingPapXSLT
		{
			get { return "XLingPap1.xsl"; }
		}

		/// <summary>
		/// Name of XLingPap Cascading Style Sheet
		/// </summary>
		private string XLingPapCSS
		{
			get { return "MorphSketch.css"; }
		}

		/// <summary>
		/// Name of htm file to display if in the process of generating
		/// </summary>
		private string GeneratingDocument
		{
			get { return Path.Combine(UtilityHtmlPath, "GeneratingDocumentPleaseWait.htm"); }
		}

		/// <summary>
		/// Name of htm file to display the first time
		/// </summary>
		private string InitialDocument
		{
			get { return Path.Combine(UtilityHtmlPath, "InitialDocument.htm"); }
		}

		private string RegistryKey
		{
			get
			{
				Debug.Assert(Cache != null);
				Debug.Assert(m_sRegKeyName != null);
				return Path.Combine(@"Software\SIL\Fieldworks\FxtViewer",
					Path.Combine(Cache.DatabaseName, m_sRegKeyName));
			}
		}


		/// <summary>
		/// FDO cache.
		/// </summary>
		protected FdoCache Cache
		{
			get
			{
				return (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			}
		}

		#endregion // Properties

		#region Construction, Initialization, and disposal

		public FxtViewer()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			InitHtmlControl();
			m_panelBottom.Controls.Add(m_htmlControl);
			m_stringResMan = new ResourceManager("SIL.FieldWorks.XWorks.xWorksStrings", Assembly.GetExecutingAssembly());
			m_outputDirectory = System.IO.Path.GetTempPath();
			//
			//			base.AccNameDefault = "FxtViewer";	// default accessibility name
			//			m_ForwardBtn.AccessibleName = "btnForward";
			//		private System.Windows.Forms.Button m_GenerateBtn;
			//		private System.Windows.Forms.Panel m_panelTop;
			//		private System.Windows.Forms.Panel m_panelBottom;
			//		private System.Windows.Forms.Button m_BackBtn;
			//		private System.Windows.Forms.Button m_ForwardBtn;
			//		private System.Windows.Forms.ToolTip toolTip1;
			//		private System.ComponentModel.IContainer components;
			//
		}

		private void InitHtmlControl()
		{
			m_htmlControl = new HtmlControl();
			m_htmlControl.Dock = System.Windows.Forms.DockStyle.Fill;
			m_htmlControl.HCBeforeNavigate += new HtmlControlEventHandler(OnBeforeNavigate);

			ResetURLCount();
		}

		private void ReadParameters()
		{
			m_sRegKeyName = XmlUtils.GetManditoryAttributeValue(m_configurationParameters, "regKeyName");
			m_sProgressDialogTitle = XmlUtils.GetManditoryAttributeValue(m_configurationParameters, "dialogTitle");
			m_sFileNameKey = XmlUtils.GetManditoryAttributeValue(m_configurationParameters, "fileNameKey");
			m_sStringsPath = XmlUtils.GetManditoryAttributeValue(m_configurationParameters, "stringsPath");
			string s = XmlUtils.GetManditoryAttributeValue(m_configurationParameters, "saveButtonToolTip");
			toolTip1.SetToolTip(m_SaveAsHtmlBtn, s);

			foreach (XmlNode rNode in m_configurationParameters.ChildNodes)
			{
				if (rNode.Name == "fxtRetriever")
					m_fxtNode = rNode;
				else if (rNode.Name == "transforms")
					m_transformsNode = rNode;
			}
		}
		private void DetermineNumberOfPrompts()
		{
			m_cPrompts = 1;
		}

		private void DetermineNumberOfTransforms()
		{
			m_cTransforms = m_transformsNode.ChildNodes.Count;  // doesn't take comments into account
		}

		private void ReadRegistry()
		{
			m_sHtmlFileName = null;
			m_sAlsoSaveFileName = "";
			RegistryKey regkey = Registry.CurrentUser.OpenSubKey(RegistryKey);
			if (regkey != null)
			{
				m_sHtmlFileName = (string)regkey.GetValue(m_ksHtmlFilePath, Path.Combine(DirectoryFinder.FWCodeDirectory, InitialDocument));
				m_sAlsoSaveFileName = (string)regkey.GetValue(m_ksAlsoSaveFilePath, "");
				regkey.Close();
			}
			if (!File.Exists(m_sHtmlFileName))
			{
				m_sHtmlFileName = Path.Combine(DirectoryFinder.FWCodeDirectory, InitialDocument);
				//DisableButtons();
			}
		}

		private void DisableButtons()
		{
			m_BackBtn.Enabled = false;
			m_ForwardBtn.Enabled = false;
			m_SaveAsHtmlBtn.Enabled = false;
		}
		private void EnableButtons()
		{
			m_BackBtn.Enabled = true;
			m_ForwardBtn.Enabled = true;
			m_SaveAsHtmlBtn.Enabled = true;
		}

		private void ShowSketch()
		{
			m_htmlControl.URL = m_sHtmlFileName;
			//m_htmlControl.Invalidate();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
					components.Dispose();
				if (m_mediator != null)
					m_mediator.RemoveColleague(this);
				if (m_fxtDumper != null)
					m_fxtDumper.Dispose();
				if (m_stringResMan != null)
					m_stringResMan.ReleaseAllResources();
			}
			m_mediator = null;
			m_fxtDumper = null;
			m_fxtNode = null;
			m_transformsNode = null;
			m_sHtmlFileName = null;
			m_configurationParameters = null;
			m_sProgressDialogTitle = null;
			m_sRegKeyName = null;
			m_sFileNameKey = null;
			m_stringResMan = null;
			m_AlsoSaveTransformNode = null;
			m_sAlsoSaveDialogTitle = null;
			m_sAlsoSaveFileName = null;
			m_sReplaceDoctype = null;
			m_sSaveAsWebpageDialogTitle = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		#endregion // Construction, Initialization, and disposal

		#region Message Handlers

		void OnGenerateButtonClick(object obj, EventArgs ea)
		{
			ProduceSketch();
			ShowSketch();
			m_SaveAsHtmlBtn.Enabled = true;
			ResetURLCount();
			WriteRegistry();
		}

		private void OnBackButtonClick(object sender, System.EventArgs e)
		{
			m_iURLCounter -= 2; // need to decrement two because OnBeforeNavigate will increment it one
			m_htmlControl.Back();
		}

		private void ResetURLCount()
		{
			m_iURLCounter = 0;
			m_iMaxURLCount = 0;
		}
		private void OnForwardButtonClick(object sender, System.EventArgs e)
		{
			m_htmlControl.Forward();
			// N.B. no need to increment m_iURLCounter because OnBeforeNavigate does it
		}
		private void OnSaveAsHtmlButtonClick(object sender, System.EventArgs e)
		{
			if (File.Exists(m_sHtmlFileName))
			{
				using (SaveFileDialog dlg = new SaveFileDialog())
				{
					InitSaveAsWebpageDialog(dlg);
					if (dlg.ShowDialog() == DialogResult.OK)
					{
						string dlgFileName = dlg.FileName;
						// For those poor souls who have run into LT-6264,
						// we need to be nice and remove the read-only attr.
						// Besides, the reporter may not believe the bug is dead,
						// as it still won't be in a copy state. :-)
						RemoveWriteProtection(dlgFileName);
						File.Copy(m_sHtmlFileName, dlg.FileName, true);
						// If m_sHtmlFileName is the initial doc, then it will be read-only.
						// Setting the attr to normal fixes LT-6264.
						// I (RandyR) don't know why the save button is enabled,
						// when the sketch has not been generated, but this ought to be the 'fix/hack'.
						RemoveWriteProtection(dlgFileName);
						if (File.Exists(m_sAlsoSaveFileName))
						{
							InitAlsoSaveDialog(dlg);
							if (dlg.ShowDialog() == DialogResult.OK)
							{
								DoAlsoSaveAs(dlg);
							}
						}
					}
				}
			}
		}

		private static void RemoveWriteProtection(string dlgFileName)
		{
			if (File.Exists(dlgFileName) && (File.GetAttributes(dlgFileName) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
				File.SetAttributes(dlgFileName, FileAttributes.Normal);
		}

		private void DoAlsoSaveAs(SaveFileDialog dlg)
		{
			string sAlsoSaveFile = Path.ChangeExtension(dlg.FileName, "xml");
			RemoveWriteProtection(sAlsoSaveFile);
			if (m_sReplaceDoctype == "")
			{
				File.Copy(m_sAlsoSaveFileName, sAlsoSaveFile, true);
			}
			else
			{
				StreamReader sr = new StreamReader(m_sAlsoSaveFileName, System.Text.Encoding.UTF8);
				string sContents = sr.ReadToEnd();
				sr.Close();
				int i = sContents.IndexOf("<!DOCTYPE ");
				if (i > -1)
				{
					StringBuilder sb = new StringBuilder();
					sb.Append(sContents.Substring(0, i+10));
					sb.Append(m_sReplaceDoctype);
					i = sContents.IndexOf(">", i);
					sb.Append(sContents.Substring(i));
					StreamWriter sw = new StreamWriter(sAlsoSaveFile, false);
					sw.Write(sb.ToString());
					sw.Close();
				}
				else
				{ // could not find DOCTYPE; nothing to replace
					File.Copy(m_sAlsoSaveFileName, sAlsoSaveFile, true);
				}
			}
		}

		private void InitAlsoSaveDialog(SaveFileDialog dlg)
		{
			dlg.InitialDirectory = Path.GetDirectoryName(dlg.FileName);
			dlg.Filter = ResourceHelper.FileFilter(FileFilterType.XML);
			dlg.Title = m_sAlsoSaveDialogTitle;
			dlg.FileName = Path.GetFileNameWithoutExtension(dlg.FileName);
		}

		private void InitSaveAsWebpageDialog(SaveFileDialog dlg)
		{
			dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			dlg.FileName =  m_mediator.StringTbl.GetString(m_sFileNameKey, m_sStringsPath);
			dlg.AddExtension = true;
			dlg.Filter = ResourceHelper.FileFilter(FileFilterType.HTM);
			dlg.Title = m_sSaveAsWebpageDialogTitle;
		}
		public void OnBeforeNavigate(object sender, HtmlControlEventArgs e)
		{
			CheckDisposed();

			m_iURLCounter++;
			m_iMaxURLCount = Math.Max(m_iMaxURLCount, m_iURLCounter);
			SetBackButtonEnabledState();
			SetForwardButtonEnabledState();
		}
		private void SetForwardButtonEnabledState()
		{
			if (m_iURLCounter < m_iMaxURLCount)
				m_ForwardBtn.Enabled = true;
			else
				m_ForwardBtn.Enabled = false;
		}

		private void SetBackButtonEnabledState()
		{
			if (m_iURLCounter > 1)
				m_BackBtn.Enabled = true;
			else
				m_BackBtn.Enabled = false;
		}

		#endregion // Message Handlers

		#region Other Methods

		private void WriteRegistry()
		{
			RegistryKey regkey = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
			if (regkey == null)
				regkey = Registry.CurrentUser.CreateSubKey(RegistryKey);
			regkey.SetValue(m_ksHtmlFilePath, m_sHtmlFileName);
			regkey.SetValue(m_ksAlsoSaveFilePath, m_sAlsoSaveFileName);
			regkey.Close();
		}

		private void ProduceSketch()
		{
			string sFxtOutputPath;
			ProgressDialogWorkingOn dlg = InitProgressDialog();
			ShowGeneratingPage();
			PerformRetrieval(out sFxtOutputPath, dlg);
			PerformTransformations(sFxtOutputPath, dlg);
			UpdateProgress(m_mediator.StringTbl.GetString("Complete", "DocumentGeneration"), dlg);//m_stringResMan.GetString("stidCompleted"), dlg);
			dlg.Close();
		}

		private ProgressDialogWorkingOn InitProgressDialog()
		{
			ProgressDialogWorkingOn dlg = new ProgressDialogWorkingOn();
			dlg.Owner = this.FindForm();
			dlg.Icon = dlg.Owner.Icon;
			dlg.Minimum = 0;
			dlg.Maximum = m_cPrompts + m_cTransforms;
			dlg.Text = m_sProgressDialogTitle;
			dlg.Show();
			return dlg;
		}
		private void ShowGeneratingPage()
		{
			// this doesn't work for some reason...
			m_sHtmlFileName = Path.Combine(DirectoryFinder.FWCodeDirectory, GeneratingDocument);
			ShowSketch();
		}

		public void PerformRetrieval(out string sFxtOutputPath, ProgressDialogWorkingOn dlg)
		{
			CheckDisposed();

			string sPrompt = XmlUtils.GetOptionalAttributeValue(m_fxtNode, "progressPrompt");
			if (sPrompt != null)
				UpdateProgress(sPrompt, dlg);
			string sFxt = XmlUtils.GetManditoryAttributeValue(m_fxtNode, "file");
			string sFxtPath = Path.Combine(DirectoryFinder.FWCodeDirectory, Path.Combine(@"Language Explorer\Configuration\Grammar\FXTs", sFxt));
			m_fxtDumper = new XDumper(Cache);
			sFxtOutputPath = Path.Combine(m_outputDirectory, Cache.DatabaseName + sFxt + "Result.xml");
			m_fxtDumper.Go(Cache.LangProject as CmObject, sFxtPath, File.CreateText(sFxtOutputPath));
		}

		private void PerformTransformations(string sLastFile, ProgressDialogWorkingOn dlg)
		{
			foreach (XmlNode rNode in m_transformsNode.ChildNodes)
			{
				sLastFile = ApplyTransform(sLastFile, rNode, dlg);
				if (m_AlsoSaveTransformNode == rNode)
					m_sAlsoSaveFileName = sLastFile;
			}
			m_sHtmlFileName = sLastFile;
		}

		private string ApplyTransform(string sInputFile, XmlNode node, ProgressDialogWorkingOn dlg)
		{
			string sProgressPrompt = XmlUtils.GetManditoryAttributeValue(node, "progressPrompt");
			UpdateProgress(sProgressPrompt, dlg);
			string sXslt = XmlUtils.GetManditoryAttributeValue(node, "file");
			string sOutputFile = Path.Combine(m_outputDirectory, Cache.DatabaseName + sXslt + "Result." + GetExtensionFromNode(node));

			XmlUtils.XSLParameter[] parameterList = CreateParameterList(node);
			if (parameterList != null)
			{
				foreach (XmlUtils.XSLParameter param in parameterList)
				{
					if (param.Name == "prmVernacularFontSize")
					{
						param.Value = GetNormalStyleFontSize(Cache.LangProject.DefaultVernacularWritingSystem);
					}
					if (param.Name == "prmGlossFontSize")
					{
						param.Value = GetNormalStyleFontSize(Cache.LangProject.DefaultAnalysisWritingSystem);
					}
				}
			}
			SIL.Utils.XmlUtils.TransformFileToFile(Path.Combine(TransformPath, sXslt), parameterList, sInputFile, sOutputFile);
			return sOutputFile;
		}

		private string GetNormalStyleFontSize(int ws)
		{
			ILgWritingSystemFactory wsf = Cache.LanguageWritingSystemFactoryAccessor;
			Font myFont = FontHeightAdjuster.GetFontForNormalStyle(ws, m_mediator, wsf);
			return myFont.Size.ToString() + "pt";
		}

		private XmlUtils.XSLParameter[] CreateParameterList(XmlNode node)
		{
			XmlUtils.XSLParameter[] parameterList = null;
			foreach (XmlNode rNode in node.ChildNodes)
			{
				if (rNode.Name == "xsltParameters")
				{
					int cParams = CountParams(rNode);
					if (cParams > 0)
					{
						parameterList = GetParameters(cParams, rNode);
					}
				}
			}
			return parameterList;
		}

		private XmlUtils.XSLParameter[] GetParameters(int cParams, XmlNode rNode)
		{
			XmlUtils.XSLParameter[] parameterList = new XmlUtils.XSLParameter[cParams];
			int i = 0;
			foreach (XmlNode rParamNode in rNode.ChildNodes)
			{
				if (rParamNode.Name == "param")
				{
					string sName = XmlUtils.GetManditoryAttributeValue(rParamNode, "name");
					string sValue = XmlUtils.GetManditoryAttributeValue(rParamNode, "value");
					if (sValue == "TransformDirectory")
					{
						sValue = TransformPath;
					}
					parameterList[i] = new XmlUtils.XSLParameter(sName, sValue);
					i++;
				}
			}
			return parameterList;
		}

		private int CountParams(XmlNode node)
		{
			int cParams = 0;
			foreach (XmlNode rNode in node.ChildNodes)
			{
				if (rNode.Name == "param")
					cParams++;
			}
			return cParams;
		}

		private string GetExtensionFromNode(XmlNode node)
		{
			return XmlUtils.GetManditoryAttributeValue(node, "ext");
		}

		private void UpdateProgress(string sMessage, ProgressDialogWorkingOn dlg)
		{
			dlg.WorkingOnText = sMessage;
			dlg.PerformStep();
			dlg.Refresh();
		}
		#endregion // Other Methods

		#region IxCoreColleague implementation

		/// <summary>
		/// Initialize.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="configurationParameters"></param>
		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();

			m_mediator = mediator;
			m_previousShowTreeBarValue = m_mediator.PropertyTable.GetBoolProperty("ShowRecordList", true);

			m_mediator.PropertyTable.SetProperty("ShowRecordList", false);

			m_configurationParameters = configurationParameters;
			mediator.AddColleague(this);

			m_mediator.PropertyTable.SetProperty("StatusPanelRecordNumber", "");
			m_mediator.PropertyTable.SetPropertyPersistence("StatusPanelRecordNumber", false);

#if notnow
			m_htmlControl.Browser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(Browser_DocumentCompleted);
#endif

			SetStrings();
			ReadParameters();
			DetermineNumberOfPrompts();
			DetermineNumberOfTransforms();
			SetAlsoSaveInfo();
			ReadRegistry();
			ShowSketch();

			//add our current state to the history system
			string toolName = m_mediator.PropertyTable.GetStringProperty("currentContentControl","");
			FdoCache cache = Cache;
			m_mediator.SendMessage("AddContextToHistory",
				FwLink.Create(toolName, Guid.Empty, cache.ServerName, cache.DatabaseName), false);
		}
#if notnow
		void Browser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			m_BackBtn.Enabled = m_htmlControl.Browser.CanGoBack;
			m_ForwardBtn.Enabled = m_htmlControl.Browser.CanGoForward;
		}
#endif
		private void SetStrings()
		{
			const string ksPath = "Linguistics/Morphology/MorphSketch";
			m_sSaveAsWebpageDialogTitle = m_mediator.StringTbl.GetString("SaveAsWebpageDialogTitle", ksPath);
			toolTip1.SetToolTip(m_BackBtn, m_mediator.StringTbl.GetString("BackButtonToolTip", ksPath));
			toolTip1.SetToolTip(m_ForwardBtn, m_mediator.StringTbl.GetString("ForwardButtonToolTip", ksPath));
		}

		private void SetAlsoSaveInfo()
		{
			if (m_cTransforms > 0)
			{
				foreach (XmlNode rNode in m_transformsNode.ChildNodes)
				{ // note that if more than one transform is set to save, only the last one will be effective
					bool fDoSave = XmlUtils.GetOptionalBooleanAttributeValue(rNode, "saveResult", false);
					if (!fDoSave)
						continue;
					string sSavePrompt = XmlUtils.GetOptionalAttributeValue(rNode, "saveResultPrompt", "");
					if (sSavePrompt!= "")
					{
						m_sAlsoSaveDialogTitle = sSavePrompt;
						m_AlsoSaveTransformNode = rNode;
						m_sReplaceDoctype = XmlUtils.GetOptionalAttributeValue(rNode, "replaceDOCTYPE", "");
					}
				}
			}
		}

		/// <summary>
		/// Return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// </summary>
		/// <returns>An array of IxCoreColleague objects. Here it is just 'this'.</returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			return new IxCoreColleague[]{this};
		}

		#endregion // IxCoreColleague implementation

		#region IxCoreContentControl implementation

		/// <summary>
		/// From IxCoreContentControl
		/// </summary>
		/// <returns>true if ok to go away</returns>
		public bool PrepareToGoAway()
		{
			CheckDisposed();

			m_mediator.PropertyTable.SetProperty("ShowRecordList", m_previousShowTreeBarValue);
			return true;
		}

		public string AreaName
		{
			get
			{
				CheckDisposed();

				return XmlUtils.GetOptionalAttributeValue( m_configurationParameters, "area", "unknown");
			}
		}

		#endregion // IxCoreContentControl implementation

		protected override AccessibleObject CreateAccessibilityInstance()
		{
			return new XCoreAccessibleObject(this as IXCoreUserControl);
		}

		#region IXCoreUserControl implementation

		/// <summary>
		/// This is the property that return the name to be used by the accessibility object.
		/// </summary>
		public string AccName
		{
			get
			{
				CheckDisposed();

				return "FxtViewer";
			}
		}

		#endregion IXCoreUserControl implementation

		#region IxCoreCtrlTabProvider implementation

		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			if (targetCandidates == null)
				throw new ArgumentNullException("'targetCandidates' is null.");

			targetCandidates.Add(this);

			return ContainsFocus ? this : null;
		}

		#endregion  IxCoreCtrlTabProvider implementation

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(FxtViewer));
			this.m_GenerateBtn = new System.Windows.Forms.Button();
			this.m_panelTop = new System.Windows.Forms.Panel();
			this.m_SaveAsHtmlBtn = new System.Windows.Forms.Button();
			this.m_ForwardBtn = new System.Windows.Forms.Button();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.m_BackBtn = new System.Windows.Forms.Button();
			this.m_panelBottom = new System.Windows.Forms.Panel();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.m_panelTop.SuspendLayout();
			this.SuspendLayout();
			//
			// m_GenerateBtn
			//
			this.m_GenerateBtn.AccessibleDescription = resources.GetString("m_GenerateBtn.AccessibleDescription");
			this.m_GenerateBtn.AccessibleName = resources.GetString("m_GenerateBtn.AccessibleName");
			this.m_GenerateBtn.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("m_GenerateBtn.Anchor")));
			this.m_GenerateBtn.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("m_GenerateBtn.BackgroundImage")));
			this.m_GenerateBtn.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("m_GenerateBtn.Dock")));
			this.m_GenerateBtn.Enabled = ((bool)(resources.GetObject("m_GenerateBtn.Enabled")));
			this.m_GenerateBtn.FlatStyle = ((System.Windows.Forms.FlatStyle)(resources.GetObject("m_GenerateBtn.FlatStyle")));
			this.m_GenerateBtn.Font = ((System.Drawing.Font)(resources.GetObject("m_GenerateBtn.Font")));
			this.m_GenerateBtn.Image = ((System.Drawing.Image)(resources.GetObject("m_GenerateBtn.Image")));
			this.m_GenerateBtn.ImageAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("m_GenerateBtn.ImageAlign")));
			this.m_GenerateBtn.ImageIndex = ((int)(resources.GetObject("m_GenerateBtn.ImageIndex")));
			this.m_GenerateBtn.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("m_GenerateBtn.ImeMode")));
			this.m_GenerateBtn.Location = ((System.Drawing.Point)(resources.GetObject("m_GenerateBtn.Location")));
			this.m_GenerateBtn.Name = "m_GenerateBtn";
			this.m_GenerateBtn.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("m_GenerateBtn.RightToLeft")));
			this.m_GenerateBtn.Size = ((System.Drawing.Size)(resources.GetObject("m_GenerateBtn.Size")));
			this.m_GenerateBtn.TabIndex = ((int)(resources.GetObject("m_GenerateBtn.TabIndex")));
			this.m_GenerateBtn.Text = resources.GetString("m_GenerateBtn.Text");
			this.m_GenerateBtn.TextAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("m_GenerateBtn.TextAlign")));
			this.toolTip1.SetToolTip(this.m_GenerateBtn, resources.GetString("m_GenerateBtn.ToolTip"));
			this.m_GenerateBtn.Visible = ((bool)(resources.GetObject("m_GenerateBtn.Visible")));
			this.m_GenerateBtn.Click += new System.EventHandler(this.OnGenerateButtonClick);
			//
			// m_panelTop
			//
			this.m_panelTop.AccessibleDescription = resources.GetString("m_panelTop.AccessibleDescription");
			this.m_panelTop.AccessibleName = resources.GetString("m_panelTop.AccessibleName");
			this.m_panelTop.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("m_panelTop.Anchor")));
			this.m_panelTop.AutoScroll = ((bool)(resources.GetObject("m_panelTop.AutoScroll")));
			this.m_panelTop.AutoScrollMargin = ((System.Drawing.Size)(resources.GetObject("m_panelTop.AutoScrollMargin")));
			this.m_panelTop.AutoScrollMinSize = ((System.Drawing.Size)(resources.GetObject("m_panelTop.AutoScrollMinSize")));
			this.m_panelTop.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("m_panelTop.BackgroundImage")));
			this.m_panelTop.Controls.Add(this.m_SaveAsHtmlBtn);
			this.m_panelTop.Controls.Add(this.m_ForwardBtn);
			this.m_panelTop.Controls.Add(this.m_BackBtn);
			this.m_panelTop.Controls.Add(this.m_GenerateBtn);
			this.m_panelTop.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("m_panelTop.Dock")));
			this.m_panelTop.Enabled = ((bool)(resources.GetObject("m_panelTop.Enabled")));
			this.m_panelTop.Font = ((System.Drawing.Font)(resources.GetObject("m_panelTop.Font")));
			this.m_panelTop.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("m_panelTop.ImeMode")));
			this.m_panelTop.Location = ((System.Drawing.Point)(resources.GetObject("m_panelTop.Location")));
			this.m_panelTop.Name = "m_panelTop";
			this.m_panelTop.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("m_panelTop.RightToLeft")));
			this.m_panelTop.Size = ((System.Drawing.Size)(resources.GetObject("m_panelTop.Size")));
			this.m_panelTop.TabIndex = ((int)(resources.GetObject("m_panelTop.TabIndex")));
			this.m_panelTop.Text = resources.GetString("m_panelTop.Text");
			this.toolTip1.SetToolTip(this.m_panelTop, resources.GetString("m_panelTop.ToolTip"));
			this.m_panelTop.Visible = ((bool)(resources.GetObject("m_panelTop.Visible")));
			//
			// m_SaveAsHtmlBtn
			//
			this.m_SaveAsHtmlBtn.AccessibleDescription = resources.GetString("m_SaveAsHtmlBtn.AccessibleDescription");
			this.m_SaveAsHtmlBtn.AccessibleName = resources.GetString("m_SaveAsHtmlBtn.AccessibleName");
			this.m_SaveAsHtmlBtn.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("m_SaveAsHtmlBtn.Anchor")));
			this.m_SaveAsHtmlBtn.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("m_SaveAsHtmlBtn.BackgroundImage")));
			this.m_SaveAsHtmlBtn.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("m_SaveAsHtmlBtn.Dock")));
			this.m_SaveAsHtmlBtn.Enabled = ((bool)(resources.GetObject("m_SaveAsHtmlBtn.Enabled")));
			this.m_SaveAsHtmlBtn.FlatStyle = ((System.Windows.Forms.FlatStyle)(resources.GetObject("m_SaveAsHtmlBtn.FlatStyle")));
			this.m_SaveAsHtmlBtn.Font = ((System.Drawing.Font)(resources.GetObject("m_SaveAsHtmlBtn.Font")));
			this.m_SaveAsHtmlBtn.Image = ((System.Drawing.Image)(resources.GetObject("m_SaveAsHtmlBtn.Image")));
			this.m_SaveAsHtmlBtn.ImageAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("m_SaveAsHtmlBtn.ImageAlign")));
			this.m_SaveAsHtmlBtn.ImageIndex = ((int)(resources.GetObject("m_SaveAsHtmlBtn.ImageIndex")));
			this.m_SaveAsHtmlBtn.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("m_SaveAsHtmlBtn.ImeMode")));
			this.m_SaveAsHtmlBtn.Location = ((System.Drawing.Point)(resources.GetObject("m_SaveAsHtmlBtn.Location")));
			this.m_SaveAsHtmlBtn.Name = "m_SaveAsHtmlBtn";
			this.m_SaveAsHtmlBtn.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("m_SaveAsHtmlBtn.RightToLeft")));
			this.m_SaveAsHtmlBtn.Size = ((System.Drawing.Size)(resources.GetObject("m_SaveAsHtmlBtn.Size")));
			this.m_SaveAsHtmlBtn.TabIndex = ((int)(resources.GetObject("m_SaveAsHtmlBtn.TabIndex")));
			this.m_SaveAsHtmlBtn.Text = resources.GetString("m_SaveAsHtmlBtn.Text");
			this.m_SaveAsHtmlBtn.TextAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("m_SaveAsHtmlBtn.TextAlign")));
			this.toolTip1.SetToolTip(this.m_SaveAsHtmlBtn, resources.GetString("m_SaveAsHtmlBtn.ToolTip"));
			this.m_SaveAsHtmlBtn.Visible = ((bool)(resources.GetObject("m_SaveAsHtmlBtn.Visible")));
			this.m_SaveAsHtmlBtn.Click += new System.EventHandler(this.OnSaveAsHtmlButtonClick);
			//
			// m_ForwardBtn
			//
			this.m_ForwardBtn.AccessibleDescription = resources.GetString("m_ForwardBtn.AccessibleDescription");
			this.m_ForwardBtn.AccessibleName = resources.GetString("m_ForwardBtn.AccessibleName");
			this.m_ForwardBtn.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("m_ForwardBtn.Anchor")));
			this.m_ForwardBtn.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("m_ForwardBtn.BackgroundImage")));
			this.m_ForwardBtn.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("m_ForwardBtn.Dock")));
			this.m_ForwardBtn.Enabled = ((bool)(resources.GetObject("m_ForwardBtn.Enabled")));
			this.m_ForwardBtn.FlatStyle = ((System.Windows.Forms.FlatStyle)(resources.GetObject("m_ForwardBtn.FlatStyle")));
			this.m_ForwardBtn.Font = ((System.Drawing.Font)(resources.GetObject("m_ForwardBtn.Font")));
			this.m_ForwardBtn.Image = ((System.Drawing.Image)(resources.GetObject("m_ForwardBtn.Image")));
			this.m_ForwardBtn.ImageAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("m_ForwardBtn.ImageAlign")));
			this.m_ForwardBtn.ImageIndex = ((int)(resources.GetObject("m_ForwardBtn.ImageIndex")));
			this.m_ForwardBtn.ImageList = this.imageList1;
			this.m_ForwardBtn.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("m_ForwardBtn.ImeMode")));
			this.m_ForwardBtn.Location = ((System.Drawing.Point)(resources.GetObject("m_ForwardBtn.Location")));
			this.m_ForwardBtn.Name = "m_ForwardBtn";
			this.m_ForwardBtn.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("m_ForwardBtn.RightToLeft")));
			this.m_ForwardBtn.Size = ((System.Drawing.Size)(resources.GetObject("m_ForwardBtn.Size")));
			this.m_ForwardBtn.TabIndex = ((int)(resources.GetObject("m_ForwardBtn.TabIndex")));
			this.m_ForwardBtn.Text = resources.GetString("m_ForwardBtn.Text");
			this.m_ForwardBtn.TextAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("m_ForwardBtn.TextAlign")));
			this.toolTip1.SetToolTip(this.m_ForwardBtn, resources.GetString("m_ForwardBtn.ToolTip"));
			this.m_ForwardBtn.Visible = ((bool)(resources.GetObject("m_ForwardBtn.Visible")));
			this.m_ForwardBtn.Click += new System.EventHandler(this.OnForwardButtonClick);
			//
			// imageList1
			//
			this.imageList1.ImageSize = ((System.Drawing.Size)(resources.GetObject("imageList1.ImageSize")));
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Fuchsia;
			//
			// m_BackBtn
			//
			this.m_BackBtn.AccessibleDescription = resources.GetString("m_BackBtn.AccessibleDescription");
			this.m_BackBtn.AccessibleName = resources.GetString("m_BackBtn.AccessibleName");
			this.m_BackBtn.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("m_BackBtn.Anchor")));
			this.m_BackBtn.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("m_BackBtn.BackgroundImage")));
			this.m_BackBtn.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("m_BackBtn.Dock")));
			this.m_BackBtn.Enabled = ((bool)(resources.GetObject("m_BackBtn.Enabled")));
			this.m_BackBtn.FlatStyle = ((System.Windows.Forms.FlatStyle)(resources.GetObject("m_BackBtn.FlatStyle")));
			this.m_BackBtn.Font = ((System.Drawing.Font)(resources.GetObject("m_BackBtn.Font")));
			this.m_BackBtn.Image = ((System.Drawing.Image)(resources.GetObject("m_BackBtn.Image")));
			this.m_BackBtn.ImageAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("m_BackBtn.ImageAlign")));
			this.m_BackBtn.ImageIndex = ((int)(resources.GetObject("m_BackBtn.ImageIndex")));
			this.m_BackBtn.ImageList = this.imageList1;
			this.m_BackBtn.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("m_BackBtn.ImeMode")));
			this.m_BackBtn.Location = ((System.Drawing.Point)(resources.GetObject("m_BackBtn.Location")));
			this.m_BackBtn.Name = "m_BackBtn";
			this.m_BackBtn.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("m_BackBtn.RightToLeft")));
			this.m_BackBtn.Size = ((System.Drawing.Size)(resources.GetObject("m_BackBtn.Size")));
			this.m_BackBtn.TabIndex = ((int)(resources.GetObject("m_BackBtn.TabIndex")));
			this.m_BackBtn.Text = resources.GetString("m_BackBtn.Text");
			this.m_BackBtn.TextAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("m_BackBtn.TextAlign")));
			this.toolTip1.SetToolTip(this.m_BackBtn, resources.GetString("m_BackBtn.ToolTip"));
			this.m_BackBtn.Visible = ((bool)(resources.GetObject("m_BackBtn.Visible")));
			this.m_BackBtn.Click += new System.EventHandler(this.OnBackButtonClick);
			//
			// m_panelBottom
			//
			this.m_panelBottom.AccessibleDescription = resources.GetString("m_panelBottom.AccessibleDescription");
			this.m_panelBottom.AccessibleName = resources.GetString("m_panelBottom.AccessibleName");
			this.m_panelBottom.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("m_panelBottom.Anchor")));
			this.m_panelBottom.AutoScroll = ((bool)(resources.GetObject("m_panelBottom.AutoScroll")));
			this.m_panelBottom.AutoScrollMargin = ((System.Drawing.Size)(resources.GetObject("m_panelBottom.AutoScrollMargin")));
			this.m_panelBottom.AutoScrollMinSize = ((System.Drawing.Size)(resources.GetObject("m_panelBottom.AutoScrollMinSize")));
			this.m_panelBottom.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("m_panelBottom.BackgroundImage")));
			this.m_panelBottom.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("m_panelBottom.Dock")));
			this.m_panelBottom.Enabled = ((bool)(resources.GetObject("m_panelBottom.Enabled")));
			this.m_panelBottom.Font = ((System.Drawing.Font)(resources.GetObject("m_panelBottom.Font")));
			this.m_panelBottom.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("m_panelBottom.ImeMode")));
			this.m_panelBottom.Location = ((System.Drawing.Point)(resources.GetObject("m_panelBottom.Location")));
			this.m_panelBottom.Name = "m_panelBottom";
			this.m_panelBottom.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("m_panelBottom.RightToLeft")));
			this.m_panelBottom.Size = ((System.Drawing.Size)(resources.GetObject("m_panelBottom.Size")));
			this.m_panelBottom.TabIndex = ((int)(resources.GetObject("m_panelBottom.TabIndex")));
			this.m_panelBottom.Text = resources.GetString("m_panelBottom.Text");
			this.toolTip1.SetToolTip(this.m_panelBottom, resources.GetString("m_panelBottom.ToolTip"));
			this.m_panelBottom.Visible = ((bool)(resources.GetObject("m_panelBottom.Visible")));
			//
			// FxtViewer
			//
			this.AccessibleDescription = resources.GetString("$this.AccessibleDescription");
			this.AccessibleName = resources.GetString("$this.AccessibleName");
			this.AutoScroll = ((bool)(resources.GetObject("$this.AutoScroll")));
			this.AutoScrollMargin = ((System.Drawing.Size)(resources.GetObject("$this.AutoScrollMargin")));
			this.AutoScrollMinSize = ((System.Drawing.Size)(resources.GetObject("$this.AutoScrollMinSize")));
			this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
			this.Controls.Add(this.m_panelBottom);
			this.Controls.Add(this.m_panelTop);
			this.Enabled = ((bool)(resources.GetObject("$this.Enabled")));
			this.Font = ((System.Drawing.Font)(resources.GetObject("$this.Font")));
			this.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("$this.ImeMode")));
			this.Location = ((System.Drawing.Point)(resources.GetObject("$this.Location")));
			this.Name = "FxtViewer";
			this.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("$this.RightToLeft")));
			this.Size = ((System.Drawing.Size)(resources.GetObject("$this.Size")));
			this.toolTip1.SetToolTip(this, resources.GetString("$this.ToolTip"));
			this.m_panelTop.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		///	see if it makes sense to display a menu controlling the "ShowRecordList" property
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayShowTreeBar(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = false;
			return true;//we handled this, no need to ask anyone else.
		}

		/// <summary>
		/// Refresh doesn't make any sense for us -- regenerating the HTML sketch via
		/// m_GenerateBtn makes more sense.  See LT-3961.
		/// Note that OnMasterRefresh is enabled by default if there is no OnDisplayMasterRefresh
		/// target method available.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayMasterRefresh(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = false;
			return true;	// we handled this, no need to ask anyone else.
		}
	}
}
