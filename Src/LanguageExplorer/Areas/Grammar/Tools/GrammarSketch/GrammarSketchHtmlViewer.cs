// Copyright (c) 2003-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: GrammarSketchHtmlViewer.cs
// Responsibility: AndyBlack

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using Microsoft.Win32;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

namespace LanguageExplorer.Areas.Grammar.Tools.GrammarSketch
{
	/// <summary>
	/// Summary description for GrammarSketchHtmlViewer.
	/// </summary>
	internal sealed class GrammarSketchHtmlViewer : UserControl, IMainContentControl, IFWDisposable
	{
		#region Data Members

		private FdoCache m_cache;
		/// <summary>
		/// The control that shows the HTML data.
		/// </summary>
		private readonly HtmlControl m_htmlControl;
		private readonly string m_outputDirectory;
		private readonly XElement m_step1MainTransformElement;
		private readonly XElement m_step2XLingPaperTransformElement;
		private readonly int m_promptsCount;
		private readonly int m_transformsCount;
		private string m_sHtmlFileName;
		private string m_step1MainOutputFileName;
		private readonly string m_sReplaceDoctype;
		/// <summary>
		/// Registry key name
		/// </summary>
		private readonly string m_sRegKeyName;
		/// Initial file name key in strings file to use when doing a save as webpage
		private readonly string m_sFileNameKey;
		/// strings path where one can find the file name key above
		private readonly string m_sStringsPath;
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
		private int m_iURLCounter;
		private ImageList imageList1;
		private int m_iMaxURLCount;

		/// <summary>
		/// Registry constants
		/// </summary>
		private const string m_ksHtmlFilePath = "HtmlFilePath";
		private const string m_ksAlsoSaveFilePath = "AlsoSaveFilePath";

		private readonly Dictionary<string, XslCompiledTransform> m_transforms = new Dictionary<string, XslCompiledTransform>();

		#endregion // Data Members

		#region Properties

		/// <summary>
		/// Path to transforms
		/// </summary>
		private static string TransformPath
		{
			get { return Path.Combine(FwDirectoryFinder.FlexFolder, "Transforms"); }
		}

		/// <summary>
		/// Path to Export Templates
		/// </summary>
		private static string ExportTemplatePath
		{
			get { return Path.Combine(FwDirectoryFinder.FlexFolder, "Export Templates"); }
		}

		/// <summary>
		/// Path to utility html files
		/// </summary>
		private static string UtilityHtmlPath
		{
			get { return Path.Combine(FwDirectoryFinder.FlexFolder, "GeneratedHtmlViewer"); }
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

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "We're returning an object")]
		private RegistryKey RegistryKey
		{
			get
			{
				using (var regKey = FwRegistryHelper.FieldWorksRegistryKey)
				{
					return regKey.CreateSubKey("GeneratedHtmlViewer\\" +
						Cache.ProjectId.Name + "\\" + m_sRegKeyName);
				}
			}
		}

		/// <summary>
		/// FDO cache.
		/// </summary>
		private FdoCache Cache
		{
			get
			{
				return m_cache ?? (m_cache = PropertyTable.GetValue<FdoCache>("cache"));
			}
		}

		#endregion // Properties

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			FlexComponentCheckingService.CheckInitializationValues(propertyTable, publisher, subscriber, PropertyTable, Publisher, Subscriber);

			PropertyTable = propertyTable;
			Publisher = publisher;
			Subscriber = subscriber;

			Subscriber.Subscribe("SaveAsWebpage", SaveAsWebpage);
			m_previousShowTreeBarValue = PropertyTable.GetValue("ShowRecordList", true);

			PropertyTable.SetProperty("ShowRecordList", false, true, true);

			PropertyTable.SetProperty("StatusPanelRecordNumber", "", false, true);

			m_sHtmlFileName = null;
			m_step1MainOutputFileName = string.Empty;
			var regkey = RegistryKey;
			if (regkey != null)
			{
				m_sHtmlFileName = (string)regkey.GetValue(m_ksHtmlFilePath, Path.Combine(FwDirectoryFinder.CodeDirectory, InitialDocument));
				m_step1MainOutputFileName = (string)regkey.GetValue(m_ksAlsoSaveFilePath, "");
				regkey.Close();
			}
			if (!File.Exists(m_sHtmlFileName))
			{
				m_sHtmlFileName = Path.Combine(FwDirectoryFinder.CodeDirectory, InitialDocument);
				//DisableButtons();
			}

			ShowSketch();

			//add our current state to the history system
			var toolName = PropertyTable.GetValue("currentContentControl", "");
			Publisher.Publish("AddContextToHistory", new FwLinkArgs(toolName, Guid.Empty));
		}

		#endregion

		#region Construction, Initialization, and disposal

		internal GrammarSketchHtmlViewer()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			m_htmlControl = new HtmlControl
			{
				Dock = DockStyle.Fill
			};
			m_htmlControl.HCBeforeNavigate += OnBeforeNavigate;

			ResetURLCount();

			m_panelBottom.Controls.Add(m_htmlControl);
			m_outputDirectory = Path.GetTempPath();

			m_sRegKeyName = "MorphSketchGen";
			m_sFileNameKey = "MorphSketchFileName";
			m_sStringsPath = "Linguistics/Morphology/MorphSketch";

			// The param "sWordWorksTransformPath" value 'TransformDirectory' is a special key the FxtViewer.dll knows about
			// The param "prmIMaxMorphsInAppendices" value of '10' is the maximum number of morphemes to show in each subsection of the appendices.
			//		NB: The "prmSMaxMorphsInAppendices" parameter below should be kept in sync with the "prmIMaxMorphsInAppendices" parameter.
			//		If you want all the morphemes to appear, remove the Line: <param name='prmIMaxMorphsInAppendices' value='10'/>
			// The value of "prmSMaxMorphsInAppendices" is the analysis language's word(s) for the maximum number of morphemes to show in each subsection of the appendices.
			// The name of "prmSDateTime" is a special key the FxtViewer.dll knows about; it gets the current date and time and then passes it to the transform as a parameter.
			// The name of "prmVernacularFontSize" is a special key the FxtViewer.dll knows about; it gets the font size of the normal style of the vernacular font.
			// The name of "prmGlossFontSize" is a special key the FxtViewer.dll knows about; it gets the font size of the normal style of the analysis (gloss) font.
			m_step1MainTransformElement = XElement.Parse(@"<transform progressPrompt='Processing data, step 1 of 2' stylesheetName='FxtM3MorphologySketch' stylesheetAssembly='ApplicationTransforms' ext='xml' ><xsltParameters><param name='sWordWorksTransformPath' value='TransformDirectory'/><param name='prmIMaxMorphsInAppendices' value='10'/><param name='prmSMaxMorphsInAppendices' value='ten'/><param name='prmSDateTime' value='fake'/><param name='prmVernacularFontSize' value='fake'/><param name='prmGlossFontSize' value='fake'/></xsltParameters></transform>");
			m_step2XLingPaperTransformElement = XElement.Parse(@"<transform progressPrompt='Processing data, step 2 of 2' stylesheetName='XLingPap1' stylesheetAssembly='PresentationTransforms' ext='htm' />");
			m_transformsCount = 2;
			m_promptsCount = 1;

			const string ksPath = "Linguistics/Morphology/MorphSketch";
			toolTip1.SetToolTip(m_BackBtn, StringTable.Table.GetString("BackButtonToolTip", ksPath));
			toolTip1.SetToolTip(m_ForwardBtn, StringTable.Table.GetString("ForwardButtonToolTip", ksPath));
		}

		private void ShowSketch()
		{
			m_htmlControl.URL = m_sHtmlFileName;
			//m_htmlControl.Invalidate();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Design", "UseCorrectDisposeSignaturesRule",
			Justification = "The class derives from UserControl. Therefore Dispose(bool) can't be private")]
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (Subscriber != null)
				{
					Subscriber.Unsubscribe("SaveAsWebpage", SaveAsWebpage);
				}
				if (components != null)
				{
					components.Dispose();
				}
			}
			m_sHtmlFileName = null;
			m_step1MainOutputFileName = null;
			m_cache = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

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

		private void GenerateButton_Clicked(object obj, EventArgs ea)
		{
			using (var dlg = InitProgressDialog())
			{
				ShowGeneratingPage();
				string sFxtOutputPath;
				PerformRetrieval(out sFxtOutputPath, dlg);
				PerformTransformations(sFxtOutputPath, dlg);
				UpdateProgress(StringTable.Table.GetString("Complete", "DocumentGeneration"), dlg);
				dlg.Close();
			}
			ShowSketch();
			ResetURLCount();
			WriteRegistry();
		}

		private void BackButton_Clicked(object sender, EventArgs e)
		{
			m_iURLCounter -= 2; // need to decrement two because OnBeforeNavigate will increment it one
			m_htmlControl.Back();
		}

		private void ResetURLCount()
		{
			m_iURLCounter = 0;
			m_iMaxURLCount = 0;
		}
		private void ForwardButton_clickeded(object sender, EventArgs e)
		{
			m_htmlControl.Forward();
			// N.B. no need to increment m_iURLCounter because OnBeforeNavigate does it
		}

		/// <summary>
		/// Handles the message "SaveAsWebpage", which is currently sent by the "ExportDialog"
		/// </summary>
		/// <param name="parameterObj"></param>
		private void SaveAsWebpage(object parameterObj)
		{
			var param = parameterObj as Tuple<string, string, string>;
			if (param == null)
			{
				throw new ArgumentException("Unexpected data type for 'parameterObj'.");
			}

			var whatToSave = param.Item1;
			var outPath = param.Item2;
			var xsltFiles = param.Item3;
			var directory = Path.GetDirectoryName(outPath);
			if (string.IsNullOrWhiteSpace(directory))
			{
				throw new ArgumentException("'outPath' parameter cannot be null, an empoty string, or only whitespace.");
			}
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
			switch (whatToSave)
			{
				case "GrammarSketchXLingPaper":
					if (File.Exists(m_step1MainOutputFileName))
					{
						var inputFile = m_step1MainOutputFileName;
						if (!string.IsNullOrEmpty(xsltFiles))
						{
							var newFileName = Path.GetFileNameWithoutExtension(outPath);
							var tempFileName = Path.Combine(Path.GetTempPath(), newFileName);
							var outputFile = tempFileName;
							var rgsXslts = xsltFiles.Split(';');
							var cXslts = rgsXslts.GetLength(0);
							for (var i = 0; i < cXslts; ++i)
							{
								outputFile = outputFile + (i + 1);
								var transform = GetTransformFromFile(Path.Combine(ExportTemplatePath, rgsXslts[i]));
								var xmlReaderSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse };
								using (var writer = new StreamWriter(outputFile + ".xml"))
								using (var reader = XmlReader.Create(inputFile, xmlReaderSettings))
								{
									transform.Transform(reader, null, writer);
								}
								inputFile = outputFile + ".xml";
							}
						}
						CopyFile(inputFile, outPath);
					}
					break;
				default:
					if (File.Exists(m_sHtmlFileName))
					{
						CopyFile(m_sHtmlFileName, outPath);
						// This task is too fast on Linux/Mono (FWNX-1191).  Wait half a second...
						// (I would like a more principled fix, but have spent too much time on this issue already.)
						System.Threading.Thread.Sleep(500);
					}
					break;
			}
		}

		private static void CopyFile(string sFileName, string outPath)
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

		private static void RemoveWriteProtection(string dlgFileName)
		{
			if (File.Exists(dlgFileName) && (File.GetAttributes(dlgFileName) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
				File.SetAttributes(dlgFileName, FileAttributes.Normal);
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
				regkey.SetValue(m_ksAlsoSaveFilePath, m_step1MainOutputFileName);
				regkey.Close();
			}
		}

		private ProgressDialogWorkingOn InitProgressDialog()
		{
			var owner = FindForm();
			Icon icon = null;
			if (owner != null)
				icon = owner.Icon;
			var dlg = new ProgressDialogWorkingOn
						{
							Owner = owner,
							Icon = icon,
							Minimum = 0,
							Maximum = m_promptsCount + m_transformsCount,
							Text = @"Generate Morphological Sketch"
						};
			dlg.Show();
			return dlg;
		}
		private void ShowGeneratingPage()
		{
			// this doesn't work for some reason...
			m_sHtmlFileName = Path.Combine(FwDirectoryFinder.CodeDirectory, GeneratingDocument);
			ShowSketch();
		}

		public void PerformRetrieval(out string outputPath, ProgressDialogWorkingOn dlg)
		{
			CheckDisposed();

			UpdateProgress("Preparing data", dlg);

			outputPath = Path.Combine(m_outputDirectory, Cache.ProjectId.Name + "RetrieverResult.xml");
			M3ModelExportServices.ExportGrammarSketch(outputPath, Cache.LanguageProject);
		}

		private void PerformTransformations(string fxtOutputPath, ProgressDialogWorkingOn dlg)
		{
			m_step1MainOutputFileName = ApplyTransform(fxtOutputPath, m_step1MainTransformElement, dlg);
			m_sHtmlFileName = ApplyTransform(m_step1MainOutputFileName, m_step2XLingPaperTransformElement, dlg);
		}

		private string ApplyTransform(string inputFile, XElement transformElement, ProgressDialogWorkingOn dlg)
		{
			var progressPrompt = XmlUtils.GetManditoryAttributeValue(transformElement, "progressPrompt");
			UpdateProgress(progressPrompt, dlg);
			var stylesheetName = XmlUtils.GetManditoryAttributeValue(transformElement, "stylesheetName");
			var stylesheetAssembly = XmlUtils.GetManditoryAttributeValue(transformElement, "stylesheetAssembly");
			var outputFile = Path.Combine(m_outputDirectory, Cache.ProjectId.Name + stylesheetName + "Result." + GetExtensionFromNode(transformElement));

			var argumentList = CreateParameterList(transformElement);
			var wsContainer = Cache.ServiceLocator.WritingSystems;

			if (argumentList.GetParam("prmVernacularFontSize", string.Empty) != null)
			{
				argumentList.RemoveParam("prmVernacularFontSize", string.Empty);
				argumentList.AddParam("prmVernacularFontSize", string.Empty, GetNormalStyleFontSize(wsContainer.DefaultVernacularWritingSystem.Handle));
			}
			if (argumentList.GetParam("prmGlossFontSize", string.Empty) != null)
			{
				argumentList.RemoveParam("prmGlossFontSize", string.Empty);
				argumentList.AddParam("prmGlossFontSize", string.Empty, GetNormalStyleFontSize(wsContainer.DefaultAnalysisWritingSystem.Handle));
			}

			var xmlReaderSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse };
			using (var writer = new StreamWriter(outputFile))
			using (var reader = XmlReader.Create(inputFile, xmlReaderSettings))
			{
				GetTransform(stylesheetName, stylesheetAssembly).Transform(reader, argumentList, writer);
			}
			return outputFile;
		}

		private XslCompiledTransform GetTransform(string xslName, string xslAssembly)
		{
			XslCompiledTransform transform;
			if (!m_transforms.TryGetValue(xslName, out transform))
			{
				transform = XmlUtils.CreateTransform(xslName, xslAssembly);
				m_transforms[xslName] = transform;
			}

			return transform;
		}

		private XslCompiledTransform GetTransformFromFile(string xslPath)
		{
			lock (m_transforms)
			{
				XslCompiledTransform transform;
				m_transforms.TryGetValue(xslPath, out transform);
				if (transform != null)
					return transform;

				transform = new XslCompiledTransform();
				transform.Load(xslPath);
				m_transforms.Add(xslPath, transform);
				return transform;
			}
		}

		private string GetNormalStyleFontSize(int ws)
		{
			var wsf = Cache.WritingSystemFactory;
			using (var myFont = FontHeightAdjuster.GetFontForNormalStyle(ws, wsf, PropertyTable))
			{
				return myFont.Size + "pt";
			}
		}

		private static XsltArgumentList CreateParameterList(XElement element)
		{
			var parameterList = new XsltArgumentList();
			foreach (var xsltParameterElement in element.Elements("xsltParameters"))
			{
				int cParams = CountParams(xsltParameterElement);
				if (cParams > 0)
				{
					parameterList = GetParameters(xsltParameterElement);
					break;
				}
			}
			return parameterList;
		}

		private static XsltArgumentList GetParameters(XElement element)
		{
			var parameterList = new XsltArgumentList();
			foreach (var paramElement in element.Elements("param"))
			{
				var name = XmlUtils.GetManditoryAttributeValue(paramElement, "name");
				var value = XmlUtils.GetManditoryAttributeValue(paramElement, "value");
				if (value == "TransformDirectory")
				{
					value = TransformPath.Replace("\\", "/");
				}
				parameterList.AddParam(name, "", value);
			}
			return parameterList;
		}

		private static int CountParams(XContainer element)
		{
			return element.Elements("param").Count();
		}

		private static string GetExtensionFromNode(XElement element)
		{
			return XmlUtils.GetManditoryAttributeValue(element, "ext");
		}

		private static void UpdateProgress(string sMessage, ProgressDialogWorkingOn dlg)
		{
			dlg.WorkingOnText = sMessage;
			dlg.PerformStep();
			dlg.Refresh();
		}
		#endregion // Other Methods

		#region IMainContentControl implementation

		/// <summary>
		/// From IMainContentControl
		/// </summary>
		/// <returns>true if ok to go away</returns>
		public bool PrepareToGoAway()
		{
			CheckDisposed();

			PropertyTable.SetProperty("ShowRecordList", m_previousShowTreeBarValue, true, true);
			return true;
		}

		public string AreaName
		{
			get
			{
				CheckDisposed();

				return "grammar";
			}
		}

		#endregion // IMainContentControl implementation

		protected override AccessibleObject CreateAccessibilityInstance()
		{
			var ao = new ControlAccessibleObject(this)
			{
				Name = AccName
			};
			return ao;
		}

		#region IMainUserControl implementation

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
			set { ;}
		}

		/// <summary>
		/// Get/set string that will trigger a message box to show.
		/// </summary>
		/// <remarks>Set to null or string.Empty to not show the message box.</remarks>
		public string MessageBoxTrigger
		{
			get { return string.Empty; }
			set { ;}
		}

		#endregion IMainUserControl implementation

		#region ICtrlTabProvider implementation

		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			if (targetCandidates == null)
				throw new ArgumentNullException("targetCandidates");

			targetCandidates.Add(this);

			return ContainsFocus ? this : null;
		}

		#endregion  ICtrlTabProvider implementation

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GrammarSketchHtmlViewer));
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
			this.m_GenerateBtn.Click += new System.EventHandler(this.GenerateButton_Clicked);
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
			this.m_ForwardBtn.Click += new System.EventHandler(this.ForwardButton_clickeded);
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
			this.m_BackBtn.Click += new System.EventHandler(this.BackButton_Clicked);
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
	}
}
