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
// File: GeneratedHtmlViewer.cs
// Responsibility: AndyBlack
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.Resources;
using System.Text;
using Microsoft.Win32;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.XWorks
{
	public interface IDataRetriever
	{
		void Retrieve(string outputPath, ILangProject langProject);
	}

	/// <summary>
	/// Summary description for GeneratedHtmlViewer.
	/// </summary>
	/// <remarks>
	/// IxCoreColleague is included in the IxCoreContentControl definition.
	/// </remarks>
	public class GeneratedHtmlViewer : UserControl, IxCoreContentControl, IFWDisposable
	{
		#region Data Members
		/// <summary>
		/// The control that shows the HTML data.
		/// </summary>
		protected HtmlControl m_htmlControl;
		/// <summary>
		/// Mediator that passes off messages.
		/// </summary>
		protected Mediator m_mediator;

		protected string m_outputDirectory;

		private IDataRetriever m_retriever;

		/// <summary>
		/// special nodes in config file
		/// </summary>
		XmlNode m_retrieverNode;
		XmlNode m_transformsNode;
		XmlNode m_AlsoSaveTransformNode;
		/// <summary>
		/// counts for progress bar
		/// </summary>
		int m_cPrompts;
		int m_cTransforms;
		ResourceManager m_stringResMan;

		private string m_sHtmlFileName;
		private string m_sAlsoSaveFileName;
		private string m_sReplaceDoctype;
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

		private Button m_GenerateBtn;
		private Panel m_panelTop;
		private Panel m_panelBottom;
		private Button m_BackBtn;
		private Button m_ForwardBtn;
		private ToolTip toolTip1;
		private System.ComponentModel.IContainer components;

		private const string m_skExtensionUri = "urn:xsltExtension-DateTime";

		/// <summary>
		/// since we are going to hide the tree bar, remember the state it was in so
		/// that when we go way we can restore it to the state it was in.
		/// </summary>
		private bool m_previousShowTreeBarValue;

		/// <summary>
		/// Back/Forward counter to keep track of state and control enable/disable of back adn forward buttons
		/// </summary>
		protected int m_iURLCounter;
		private ImageList imageList1;
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
		private static string TransformPath
		{
			get { return Path.Combine(DirectoryFinder.FlexFolder, "Transforms"); }
		}
		/// <summary>
		/// Path to utility html files
		/// </summary>
		private static string UtilityHtmlPath
		{
			get { return Path.Combine(DirectoryFinder.FlexFolder, "GeneratedHtmlViewer"); }
		}

		/// <summary>
		/// Name of htm file to display if in the process of generating
		/// </summary>
		private static string GeneratingDocument
		{
			get { return Path.Combine(UtilityHtmlPath, "GeneratingDocumentPleaseWait.htm"); }
		}

		/// <summary>
		/// Name of htm file to display the first time
		/// </summary>
		private static string InitialDocument
		{
			get { return Path.Combine(UtilityHtmlPath, "InitialDocument.htm"); }
		}

		private RegistryKey RegistryKey
		{
			get
			{
				Debug.Assert(Cache != null);
				Debug.Assert(m_sRegKeyName != null);
				return FwRegistryHelper.FieldWorksRegistryKey.CreateSubKey("GeneratedHtmlViewer\\" +
					Cache.ProjectId.Name + "\\" + m_sRegKeyName);
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

		public GeneratedHtmlViewer()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			InitHtmlControl();
			m_panelBottom.Controls.Add(m_htmlControl);
			m_stringResMan = new ResourceManager("SIL.FieldWorks.XWorks.xWorksStrings", Assembly.GetExecutingAssembly());
			m_outputDirectory = Path.GetTempPath();
		}

		private void InitHtmlControl()
		{
			m_htmlControl = new HtmlControl {Dock = DockStyle.Fill};
			m_htmlControl.HCBeforeNavigate += OnBeforeNavigate;

			ResetURLCount();
		}

		private void ReadParameters()
		{
			m_sRegKeyName = XmlUtils.GetManditoryAttributeValue(m_configurationParameters, "regKeyName");
			m_sProgressDialogTitle = XmlUtils.GetManditoryAttributeValue(m_configurationParameters, "dialogTitle");
			m_sFileNameKey = XmlUtils.GetManditoryAttributeValue(m_configurationParameters, "fileNameKey");
			m_sStringsPath = XmlUtils.GetManditoryAttributeValue(m_configurationParameters, "stringsPath");

			foreach (XmlNode rNode in m_configurationParameters.ChildNodes)
			{
				if (rNode.Name == "retriever")
				{
					m_retrieverNode = rNode;
					m_retriever = (IDataRetriever) DynamicLoader.CreateObject(m_retrieverNode.SelectSingleNode("dynamicloaderinfo"));
				}
				else if (rNode.Name == "transforms")
				{
					m_transformsNode = rNode;
				}
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
			RegistryKey regkey = RegistryKey;
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
					components.Dispose();
				if (m_mediator != null)
					m_mediator.RemoveColleague(this);
				if (m_stringResMan != null)
					m_stringResMan.ReleaseAllResources();
			}
			m_mediator = null;
			m_retrieverNode = null;
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
			ResetURLCount();
			WriteRegistry();
		}

		private void OnBackButtonClick(object sender, EventArgs e)
		{
			m_iURLCounter -= 2; // need to decrement two because OnBeforeNavigate will increment it one
			m_htmlControl.Back();
		}

		private void ResetURLCount()
		{
			m_iURLCounter = 0;
			m_iMaxURLCount = 0;
		}
		private void OnForwardButtonClick(object sender, EventArgs e)
		{
			m_htmlControl.Forward();
			// N.B. no need to increment m_iURLCounter because OnBeforeNavigate does it
		}
		public bool OnSaveAsWebpage(object parameterObj)
		{
			var param = parameterObj as Tuple<string, string>;
			if (param == null)
				return false; // we sure can't handle it; should we throw?
			string whatToSave = param.Item1;
			string outPath = param.Item2;
			string directory = Path.GetDirectoryName(outPath);
			if (!Directory.Exists(directory))
			{
				// can't copy to a directory that doesn't exist
				return false;
			}
				switch (whatToSave)
				{
					case "GrammarSketchXLingPaper":
							if (File.Exists(m_sAlsoSaveFileName))
							{
								CopyFile(m_sAlsoSaveFileName, outPath);
								return true;
							}
						break;
					default:
						if (File.Exists(m_sHtmlFileName))
						{
							CopyFile(m_sHtmlFileName, outPath);
							return true;
						}
						break;
			}
			return false;
		}

		private void CopyFile(string sFileName, string outPath)
		{
			// For those poor souls who have run into LT-6264,
			// we need to be nice and remove the read-only attr.
			// Besides, the reporter may not believe the bug is dead,
			// as it still won't be in a copy state. :-)
			RemoveWriteProtection(outPath);
			File.Copy(sFileName, outPath, true);
			// If m_sHtmlFileName is the initial doc, then it will be read-only.
			// Setting the attr to normal fixes LT-6264.
			// I (RandyR) don't know why the save button is enabled,
			// when the sketch has not been generated, but this ought to be the 'fix/hack'.
			RemoveWriteProtection(outPath);
		}

		private void OnSaveAsHtmlButtonClick(object sender, EventArgs e)
		{
			if (File.Exists(m_sHtmlFileName))
			{
				using (var dlg = new SaveFileDialog())
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
				string sContents;
				using (var sr = new StreamReader(m_sAlsoSaveFileName, System.Text.Encoding.UTF8))
				{
					sContents = sr.ReadToEnd();
					sr.Close();
				}
				int i = sContents.IndexOf("<!DOCTYPE ");
				if (i > -1)
				{
					var sb = new StringBuilder();
					sb.Append(sContents.Substring(0, i + 10));
					sb.Append(m_sReplaceDoctype);
					i = sContents.IndexOf(">", i);
					sb.Append(sContents.Substring(i));
					using (var sw = new StreamWriter(sAlsoSaveFile, false))
					{
						sw.Write(sb.ToString());
						sw.Close();
					}
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
			m_ForwardBtn.Enabled = m_iURLCounter < m_iMaxURLCount;
		}

		private void SetBackButtonEnabledState()
		{
			m_BackBtn.Enabled = m_iURLCounter > 1;
		}

		#endregion // Message Handlers

		#region Other Methods

		private void WriteRegistry()
		{
			using (RegistryKey regkey = RegistryKey)
			{
				regkey.SetValue(m_ksHtmlFilePath, m_sHtmlFileName);
				regkey.SetValue(m_ksAlsoSaveFilePath, m_sAlsoSaveFileName);
				regkey.Close();
			}
		}

		private void ProduceSketch()
		{
			string sFxtOutputPath;
			using (ProgressDialogWorkingOn dlg = InitProgressDialog())
			{
				ShowGeneratingPage();
				PerformRetrieval(out sFxtOutputPath, dlg);
				PerformTransformations(sFxtOutputPath, dlg);
				UpdateProgress(m_mediator.StringTbl.GetString("Complete", "DocumentGeneration"), dlg);//m_stringResMan.GetString("stidCompleted"), dlg);
				dlg.Close();
			}
		}

		private ProgressDialogWorkingOn InitProgressDialog()
		{
			Form owner = FindForm();
			Icon icon = null;
			if (owner != null)
				icon = owner.Icon;
			var dlg = new ProgressDialogWorkingOn
						{
							Owner = owner,
							Icon = icon,
							Minimum = 0,
							Maximum = m_cPrompts + m_cTransforms,
							Text = m_sProgressDialogTitle
						};
			dlg.Show();
			return dlg;
		}
		private void ShowGeneratingPage()
		{
			// this doesn't work for some reason...
			m_sHtmlFileName = Path.Combine(DirectoryFinder.FWCodeDirectory, GeneratingDocument);
			ShowSketch();
		}

		public void PerformRetrieval(out string outputPath, ProgressDialogWorkingOn dlg)
		{
			CheckDisposed();

			string sPrompt = XmlUtils.GetOptionalAttributeValue(m_retrieverNode, "progressPrompt");
			if (sPrompt != null)
				UpdateProgress(sPrompt, dlg);

			outputPath = Path.Combine(m_outputDirectory, Cache.ProjectId.Name + "RetrieverResult.xml");
			m_retriever.Retrieve(outputPath, Cache.LanguageProject);
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
			string sOutputFile = Path.Combine(m_outputDirectory, Cache.ProjectId.Name + sXslt + "Result." + GetExtensionFromNode(node));

			XmlUtils.XSLParameter[] parameterList = CreateParameterList(node);
			IWritingSystemContainer wsContainer = Cache.ServiceLocator.WritingSystems;
			if (parameterList != null)
			{
				foreach (XmlUtils.XSLParameter param in parameterList)
				{
					if (param.Name == "prmVernacularFontSize")
					{
						param.Value = GetNormalStyleFontSize(wsContainer.DefaultVernacularWritingSystem.Handle);
					}
					if (param.Name == "prmGlossFontSize")
					{
						param.Value = GetNormalStyleFontSize(wsContainer.DefaultAnalysisWritingSystem.Handle);
					}
				}
			}
			XmlUtils.TransformFileToFile(Path.Combine(TransformPath, sXslt), parameterList, sInputFile, sOutputFile);
			return sOutputFile;
		}

		private string GetNormalStyleFontSize(int ws)
		{
			ILgWritingSystemFactory wsf = Cache.WritingSystemFactory;
			using (Font myFont = FontHeightAdjuster.GetFontForNormalStyle(ws, m_mediator, wsf))
				return myFont.Size + "pt";
		}

		private static XmlUtils.XSLParameter[] CreateParameterList(XmlNode node)
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

		private static XmlUtils.XSLParameter[] GetParameters(int cParams, XmlNode rNode)
		{
			var parameterList = new XmlUtils.XSLParameter[cParams];
			int i = 0;
			foreach (XmlNode rParamNode in rNode.ChildNodes)
			{
				if (rParamNode.Name == "param")
				{
					string sName = XmlUtils.GetManditoryAttributeValue(rParamNode, "name");
					string sValue = XmlUtils.GetManditoryAttributeValue(rParamNode, "value");
					if (sValue == "TransformDirectory")
					{
						sValue = TransformPath.Replace("\\", "/");
					}
					parameterList[i] = new XmlUtils.XSLParameter(sName, sValue);
					i++;
				}
			}
			return parameterList;
		}

		private static int CountParams(XmlNode node)
		{
			return node.ChildNodes.Cast<XmlNode>().Count(rNode => rNode.Name == "param");
		}

		private static string GetExtensionFromNode(XmlNode node)
		{
			return XmlUtils.GetManditoryAttributeValue(node, "ext");
		}

		private static void UpdateProgress(string sMessage, ProgressDialogWorkingOn dlg)
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
			m_mediator.SendMessage("AddContextToHistory", new FwLinkArgs(toolName, Guid.Empty), false);
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

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		public int Priority
		{
			get { return (int)ColleaguePriority.Medium; }
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
			return new XCoreAccessibleObject(this);
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

				return "GeneratedHtmlViewer";
			}
		}

		#endregion IXCoreUserControl implementation

		#region IxCoreCtrlTabProvider implementation

		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			if (targetCandidates == null)
				throw new ArgumentNullException("targetCandidates");

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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GeneratedHtmlViewer));
			this.m_GenerateBtn = new System.Windows.Forms.Button();
			this.m_panelTop = new System.Windows.Forms.Panel();
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
			resources.ApplyResources(this.m_GenerateBtn, "m_GenerateBtn");
			this.m_GenerateBtn.Name = "m_GenerateBtn";
			this.toolTip1.SetToolTip(this.m_GenerateBtn, resources.GetString("m_GenerateBtn.ToolTip"));
			this.m_GenerateBtn.Click += new System.EventHandler(this.OnGenerateButtonClick);
			//
			// m_panelTop
			//
			resources.ApplyResources(this.m_panelTop, "m_panelTop");
			this.m_panelTop.Controls.Add(this.m_ForwardBtn);
			this.m_panelTop.Controls.Add(this.m_BackBtn);
			this.m_panelTop.Controls.Add(this.m_GenerateBtn);
			this.m_panelTop.Name = "m_panelTop";
			//
			// m_ForwardBtn
			//
			resources.ApplyResources(this.m_ForwardBtn, "m_ForwardBtn");
			this.m_ForwardBtn.ImageList = this.imageList1;
			this.m_ForwardBtn.Name = "m_ForwardBtn";
			this.toolTip1.SetToolTip(this.m_ForwardBtn, resources.GetString("m_ForwardBtn.ToolTip"));
			this.m_ForwardBtn.Click += new System.EventHandler(this.OnForwardButtonClick);
			//
			// imageList1
			//
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Fuchsia;
			this.imageList1.Images.SetKeyName(0, "");
			this.imageList1.Images.SetKeyName(1, "");
			//
			// m_BackBtn
			//
			resources.ApplyResources(this.m_BackBtn, "m_BackBtn");
			this.m_BackBtn.ImageList = this.imageList1;
			this.m_BackBtn.Name = "m_BackBtn";
			this.toolTip1.SetToolTip(this.m_BackBtn, resources.GetString("m_BackBtn.ToolTip"));
			this.m_BackBtn.Click += new System.EventHandler(this.OnBackButtonClick);
			//
			// m_panelBottom
			//
			resources.ApplyResources(this.m_panelBottom, "m_panelBottom");
			this.m_panelBottom.Name = "m_panelBottom";
			//
			// GeneratedHtmlViewer
			//
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.m_panelBottom);
			this.Controls.Add(this.m_panelTop);
			this.Name = "GeneratedHtmlViewer";
			this.m_panelTop.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// see if it makes sense to display a menu controlling the "ShowRecordList" property
		/// </summary>
		/// <param name="commandObject">The command object.</param>
		/// <param name="display">The display.</param>
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
		public bool OnDisplayExport(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = true;
			return true;
		}
		public bool OnExport(object argument)
		{
			CheckDisposed();

				using (var dlg = new ExportDialog(m_mediator))
				{
					dlg.ShowDialog();
				}
			return true;	// handled
		}
	}

	public class GrammarSketchDataRetriever : IDataRetriever
	{
		public void Retrieve(string outputPath, ILangProject langProject)
		{
			M3ModelExportServices.ExportGrammarSketch(outputPath, langProject);
		}
	}
}
