// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.XMLViews;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.FixData;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;
using SIL.Reporting;

namespace LanguageExplorer.UtilityTools
{
	/// <summary>
	/// This dialog presents the users with the list of utilities defined in 'Language Explorer\Configuration\UtilityCatalogInclude.xml'
	/// These utilities must implement the IUtility class and can set several labels in the dialog to explain the conditions where they
	/// are needed and their behavior.
	/// </summary>
	public class UtilityDlg : Form, IFlexComponent
	{
		private RichTextBox m_rtbDescription;
		private Label m_lSteps;
		private IContainer components = null;
		private const string s_helpTopic = "khtpProjectUtilities";
		private HelpProvider m_helpProvider;
		private Button m_btnRunUtils;
		private Label label1;
		private Label label2;
		private Button m_btnHelp;
		private Button m_btnClose;
		private IHelpTopicProvider m_helpTopicProvider;
		private MajorFlexComponentParameters _flexComponentParameters;

		/// <summary />
		public UtilityDlg(IHelpTopicProvider helpTopicProvider)
		{
			InitializeComponent();
			AccessibleName = GetType().Name;
			m_btnRunUtils.Enabled = false;
			m_helpTopicProvider = helpTopicProvider;
			m_helpProvider = new HelpProvider
			{
				HelpNamespace = FwDirectoryFinder.CodeDirectory + m_helpTopicProvider.GetHelpString(FwUtilsConstants.UserHelpFile)
			};
			m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
			m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			// The standard localization doesn't seem to be working, so do it explicitly here.
			label1.Text = LanguageExplorerResources.ksUtilities;
			label2.Text = LanguageExplorerResources.ksDescription;
			m_btnHelp.Text = LanguageExplorerResources.ksHelpForUtiltiesDlg;
			m_btnClose.Text = LanguageExplorerResources.ks_Close;
			m_btnRunUtils.Text = LanguageExplorerResources.ksRunUtilities;
		}

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

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
			_flexComponentParameters = PropertyTable.GetValue<MajorFlexComponentParameters>(LanguageExplorerConstants.MajorFlexComponentParameters);

			Text = StringTable.Table.LocalizeAttributeValue("FieldWorks Project Utilities");
			SuspendLayout();
			Utilities.Items.Clear();
			Utilities.Sorted = false;
			Utilities.Items.Add(new HomographResetter(_flexComponentParameters, ProgressBar));
			Utilities.Items.Add(new ParserAnalysisRemover(_flexComponentParameters, ProgressBar));
			Utilities.Items.Add(new ErrorFixer(this));
			Utilities.Items.Add(new WriteAllObjectsUtility(_flexComponentParameters.LcmCache));
			Utilities.Items.Add(new DuplicateWordformFixer(this));
			Utilities.Items.Add(new DuplicateAnalysisFixer(this));
			Utilities.Items.Add(new ParseIsCurrentFixer(_flexComponentParameters.LcmCache, ProgressBar));
			Utilities.Items.Add(new DeleteEntriesSensesWithoutInterlinearization(_flexComponentParameters.FlexComponentParameters, _flexComponentParameters.LcmCache, ProgressBar));
			Utilities.Items.Add(new LexEntryInflTypeConverter(this));
			Utilities.Items.Add(new LexEntryTypeConverter(this));
			Utilities.Items.Add(new GoldEticGuidFixer(this));
			Utilities.Items.Add(new SortReversalSubEntries(this));
			Utilities.Items.Add(new CircularRefBreaker(_flexComponentParameters.LcmCache));
			// Add user supplied utilities.
			//var userCreatedUtilities = new Dictionary<string, IUtility>();
			var interfaceType = typeof(IUtility);
			var myPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			foreach (var utilityElement in XDocument.Parse(File.ReadAllText(Path.Combine(FwDirectoryFinder.FlexFolder, "UserDefinedUtilities.xml"))).Root.Elements("utility"))
			{
				var currentAssembly = Assembly.ReflectionOnlyLoadFrom(Path.Combine(myPath, utilityElement.Attribute("assembly").Value));
				foreach (var type in currentAssembly.GetTypes().Where(t => interfaceType.IsAssignableFrom(t) && t.IsClass))
				{
					Utilities.Items.Add((IUtility)currentAssembly.CreateInstance(type.FullName, true, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { this }, null, null));
				}
			}
			ResumeLayout();
			Utilities.SelectedIndex = 0;
		}

		#endregion
		/// <summary>
		/// Get the Utilities list box.
		/// </summary>
		private CheckedListBox Utilities { get; set; }

		/// <summary>
		/// Get the progress bar.
		/// </summary>
		private ProgressBar ProgressBar { get; set; }

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
				m_helpProvider.ResetShowHelp(this);
				m_helpProvider.Dispose();
			}

			PropertyTable = null;
			Publisher = null;
			Subscriber = null;
			m_helpProvider = null;
			m_helpTopicProvider = null;

			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UtilityDlg));
			this.Utilities = new System.Windows.Forms.CheckedListBox();
			this.m_rtbDescription = new System.Windows.Forms.RichTextBox();
			this.m_btnRunUtils = new System.Windows.Forms.Button();
			this.ProgressBar = new System.Windows.Forms.ProgressBar();
			this.m_lSteps = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_btnClose = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_clbUtilities
			//
			resources.ApplyResources(this.Utilities, "m_clbUtilities");
			this.Utilities.Name = "m_clbUtilities";
			this.Utilities.SelectedIndexChanged += new System.EventHandler(this.m_clbUtilities_SelectedIndexChanged);
			this.Utilities.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.m_clbUtilities_ItemCheck);
			//
			// m_rtbDescription
			//
			resources.ApplyResources(this.m_rtbDescription, "m_rtbDescription");
			this.m_rtbDescription.Name = "m_rtbDescription";
			this.m_rtbDescription.ReadOnly = true;
			//
			// m_btnRunUtils
			//
			resources.ApplyResources(this.m_btnRunUtils, "m_btnRunUtils");
			this.m_btnRunUtils.Name = "m_btnRunUtils";
			this.m_btnRunUtils.Click += new System.EventHandler(this.m_btnRunUtils_Click);
			//
			// m_progressBar
			//
			resources.ApplyResources(this.ProgressBar, "m_progressBar");
			this.ProgressBar.Name = "m_progressBar";
			//
			// m_lSteps
			//
			resources.ApplyResources(this.m_lSteps, "m_lSteps");
			this.m_lSteps.Name = "m_lSteps";
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_btnClose
			//
			this.m_btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.m_btnClose, "m_btnClose");
			this.m_btnClose.Name = "m_btnClose";
			this.m_btnClose.Click += new System.EventHandler(this.m_btnClose_Click);
			//
			// UtilityDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnClose;
			this.ControlBox = false;
			this.Controls.Add(this.m_lSteps);
			this.Controls.Add(this.ProgressBar);
			this.Controls.Add(this.m_btnClose);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnRunUtils);
			this.Controls.Add(this.m_rtbDescription);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.Utilities);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "UtilityDlg";
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);

		}
		#endregion

		#region Events

		private void m_clbUtilities_SelectedIndexChanged(object sender, EventArgs e)
		{
			var currentFont = m_rtbDescription.SelectionFont;
			Font boldFont = null;
			try
			{
				// Creating the bold equivalent of the current font doesn't seem to work in Mono,
				// as we crash shortly due to failures in GDIPlus.GdipMeasureString() using that
				// font.
				boldFont = Platform.IsMono ? currentFont : new Font(currentFont.FontFamily, currentFont.Size, FontStyle.Bold);
				var selectedUtility = (IUtility)Utilities.SelectedItem;
				m_rtbDescription.Clear();
				// What
				m_rtbDescription.SelectionFont = boldFont;
				m_rtbDescription.AppendText(LanguageExplorerResources.ksWhatItDoes);
				m_rtbDescription.AppendText(Environment.NewLine);
				m_rtbDescription.SelectionFont = currentFont;
				m_rtbDescription.AppendText(selectedUtility.WhatDescription);
				m_rtbDescription.AppendText(string.Format("{0}{0}", Environment.NewLine));
				// When
				m_rtbDescription.SelectionFont = boldFont;
				m_rtbDescription.AppendText(LanguageExplorerResources.ksWhenToUse);
				m_rtbDescription.AppendText(Environment.NewLine);
				m_rtbDescription.SelectionFont = currentFont;
				m_rtbDescription.AppendText(selectedUtility.WhenDescription);
				m_rtbDescription.AppendText(string.Format("{0}{0}", Environment.NewLine));
				// Cautions
				m_rtbDescription.SelectionFont = boldFont;
				m_rtbDescription.AppendText(LanguageExplorerResources.ksCautions);
				m_rtbDescription.AppendText(Environment.NewLine);
				m_rtbDescription.SelectionFont = currentFont;
				m_rtbDescription.AppendText(selectedUtility.RedoDescription);
				if (Platform.IsMono)
				{
					// If we don't have a selection explicitly set, we will crash deep in the Mono
					// code (RichTextBox.cs:618, property SelectionFont:get) shortly.
					m_rtbDescription.Focus();
					m_rtbDescription.SelectionStart = 0;
					m_rtbDescription.SelectionLength = 0;
					Utilities.Focus();
				}
			}
			finally
			{
				if (!Platform.IsMono)
				{
					boldFont?.Dispose();
				}
			}
		}

		private void m_btnRunUtils_Click(object sender, System.EventArgs e)
		{
			using (new WaitCursor(this))
			{
				// Note: Resetting the steps text doesn't work.
				//m_lSteps.Text = string.Empty;
				//int totalSteps = m_clbUtilities.CheckedItems.Count;
				//int currentStep = 0;
				var checkedItems = new HashSet<IUtility>();
				foreach (IUtility util in Utilities.CheckedItems)
				{
					util.Process();
					ProgressBar.Value = 0;
					checkedItems.Add(util);
				}
				// Uncheck each one that was done.
				foreach (var checkedUtil in checkedItems)
				{
					Utilities.SetItemChecked(Utilities.Items.IndexOf(checkedUtil), false);
				}
			}
		}

		private void m_btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void m_clbUtilities_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			var cnt = Utilities.CheckedItems.Count;
			cnt = (e.NewValue == CheckState.Checked) ? cnt + 1 : cnt - 1;
			m_btnRunUtils.Enabled = cnt > 0;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}

		#endregion Events

		#region IUtility implementations

		/// <summary>
		/// What: This utility cleans up the homographs numbers of lexical entries. It preserves the current relative order of homographs, so you won't lose any ordering you have done.
		/// When: Run this utility when the FieldWorks project has entries with duplicate or missing homograph numbers, or when there are gaps in the homograph number sequences.
		/// </summary>
		private sealed class HomographResetter : IUtility
		{
			private readonly LcmCache _cache;
			private readonly ProgressBar _progressBar;

			/// <summary />
			internal HomographResetter(MajorFlexComponentParameters flexComponentParameters, ProgressBar progressBar)
			{
				Guard.AgainstNull(flexComponentParameters, nameof(flexComponentParameters));
				Guard.AgainstNull(progressBar, nameof(progressBar));

				_cache = flexComponentParameters.LcmCache;
				_progressBar = progressBar;
			}

			#region IUtility implementation

			/// <summary>
			/// State what the utility does, or FwUtilsStrings.ksThreeQuestionMarks, if there is no what description.
			/// </summary>
			string IUtility.WhatDescription => LanguageExplorerResources.ksWhatIsReassignHomographs;

			/// <summary>
			/// State when the utility should be run, or FwUtilsStrings.ksThreeQuestionMarks, if there is no when description.
			/// </summary>
			string IUtility.WhenDescription => LanguageExplorerResources.ksWhenToReassignHomographs;

			/// <summary>
			/// State what the utility does for a redo, or FwUtilsStrings.ksThreeQuestionMarks, if there is no redo description.
			/// </summary>
			string IUtility.RedoDescription => LanguageExplorerResources.ksGenericUtilityCannotUndo;

			/// <summary>
			/// Have the utility do what it does.
			/// </summary>
			void IUtility.Process()
			{
				var homographWsId = _cache.LanguageProject.HomographWs;
				var homographWs = _cache.ServiceLocator.WritingSystems.AllWritingSystems.Where(ws => ws.Id == homographWsId);
				var homographWsLabel = homographWs.First().DisplayLabel;
				var defaultVernacularWs = _cache.LanguageProject.DefaultVernacularWritingSystem;
				var defaultVernacularWsId = defaultVernacularWs.Id;
				var changeWs = false;
				if (homographWsId != defaultVernacularWsId)
				{
					var caution = string.Format(LanguageExplorerResources.ksReassignHomographsCaution, homographWsLabel, defaultVernacularWs.DisplayLabel);
					if (MessageBox.Show(caution, LanguageExplorerResources.ksReassignHomographs, MessageBoxButtons.YesNo) == DialogResult.Yes)
					{
						changeWs = true;
					}
				}
				if (changeWs)
				{
					UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(LanguageExplorerResources.ksUndoHomographWs, LanguageExplorerResources.ksRedoHomographWs, _cache.ActionHandlerAccessor, () =>
					{
						_cache.LanguageProject.HomographWs = defaultVernacularWsId;
					});
				}
				_cache.LanguageProject.LexDbOA.ResetHomographNumbers(new ProgressBarWrapper(_progressBar));
			}

			#endregion IUtility implementation

			/// <summary>
			/// Override method to return the Label property.
			/// </summary>
			public override string ToString()
			{
				return LanguageExplorerResources.ksReassignHomographs;
			}
		}

		/// <summary>
		/// This class serves to remove all analyses that are only approved by the parser.
		/// Analyses that have a human evaluation (approved or disapproved) remain afterwards.
		/// </summary>
		private sealed class ParserAnalysisRemover : IUtility
		{
			private readonly LcmCache _cache;
			private readonly IPublisher _publisher;
			private readonly ProgressBar _progressBar;
			private const string kPath = "/group[@id='Linguistics']/group[@id='Morphology']/group[@id='RemoveParserAnalyses']/";

			/// <summary />
			internal ParserAnalysisRemover(MajorFlexComponentParameters flexComponentParameters, ProgressBar progressBar)
			{
				Guard.AgainstNull(flexComponentParameters, nameof(flexComponentParameters));
				Guard.AgainstNull(progressBar, nameof(progressBar));

				_cache = flexComponentParameters.LcmCache;
				_publisher = flexComponentParameters.FlexComponentParameters.Publisher;
				_progressBar = progressBar;
			}

			#region IUtility implementation

			/// <summary>
			/// State what the utility does, or FwUtilsStrings.ksThreeQuestionMarks, if there is no what description.
			/// </summary>
			string IUtility.WhatDescription => StringTable.Table.GetStringWithXPath("WhatDescription", kPath);

			/// <summary>
			/// State when the utility should be run, or FwUtilsStrings.ksThreeQuestionMarks, if there is no when description.
			/// </summary>
			string IUtility.WhenDescription => StringTable.Table.GetStringWithXPath("WhenDescription", kPath);

			/// <summary>
			/// State what the utility does for a redo, or FwUtilsStrings.ksThreeQuestionMarks, if there is no redo description.
			/// </summary>
			string IUtility.RedoDescription => StringTable.Table.GetStringWithXPath("RedoDescription", kPath);

			/// <summary>
			/// Have the utility do what it does.
			/// </summary>
			void IUtility.Process()
			{
				var analyses = _cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().AllInstances().ToArray();
				if (analyses.Length == 0)
				{
					return;
				}
				// Set up progress bar.
				_progressBar.Minimum = 0;
				_progressBar.Maximum = analyses.Length;
				_progressBar.Step = 1;
				// stop parser if it's running.
				_publisher.Publish(new PublisherParameterObject(LanguageExplorerConstants.StopParser));
				NonUndoableUnitOfWorkHelper.Do(_cache.ActionHandlerAccessor, () =>
				{
					foreach (var analysis in analyses)
					{
						var parserEvals = analysis.EvaluationsRC.Where(evaluation => !evaluation.Human).ToArray();
						foreach (var parserEval in parserEvals)
						{
							analysis.EvaluationsRC.Remove(parserEval);
						}
						var wordform = analysis.Wordform;
						if (analysis.EvaluationsRC.Count == 0)
						{
							wordform.AnalysesOC.Remove(analysis);
						}
						if (parserEvals.Length > 0)
						{
							wordform.Checksum = 0;
						}
						_progressBar.PerformStep();
					}
				});
			}

			#endregion IUtility implementation

			/// <summary>
			/// Override method to return the Label property.
			/// </summary>
			public override string ToString()
			{
				return StringTable.Table.GetStringWithXPath("Label", kPath);
			}
		}

		/// <summary>
		/// Connect the error fixing code to the FieldWorks UtilityDlg facility.
		/// </summary>
		private sealed class ErrorFixer : IUtility
		{
			private UtilityDlg _dlg;
			private readonly List<string> _errors = new List<string>();
			private int _errorsFixed;

			/// <summary />
			internal ErrorFixer(UtilityDlg utilityDlg)
			{
				Guard.AgainstNull(utilityDlg, nameof(utilityDlg));

				_dlg = utilityDlg;
			}

			#region IUtility Members

			/// <summary>
			/// State what the utility does, or FwUtilsStrings.ksThreeQuestionMarks, if there is no what description.
			/// </summary>
			string IUtility.WhatDescription => LanguageExplorerResources.ksErrorFixerUtilityAttemptsTo;

			/// <summary>
			/// State when the utility should be run, or FwUtilsStrings.ksThreeQuestionMarks, if there is no when description.
			/// </summary>
			string IUtility.WhenDescription => LanguageExplorerResources.ksUseErrorFixerWhen;

			/// <summary>
			/// State what the utility does for a redo, or FwUtilsStrings.ksThreeQuestionMarks, if there is no redo description.
			/// </summary>
			string IUtility.RedoDescription => LanguageExplorerResources.ksGenericUtilityCannotUndo;

			/// <summary>
			/// Run the utility on command from the main utility dialog.
			/// </summary>
			void IUtility.Process()
			{
				using (var dlg = new FixErrorsDlg())
				{
					try
					{
						if (dlg.ShowDialog(_dlg) != DialogResult.OK)
						{
							return;
						}
						var pathname = Path.Combine(Path.Combine(FwDirectoryFinder.ProjectsDirectory, dlg.SelectedProject), dlg.SelectedProject + LcmFileHelper.ksFwDataXmlFileExtension);
						if (!File.Exists(pathname))
						{
							return;
						}
						using (new WaitCursor(_dlg))
						{
							using (var progressDlg = new ProgressDialogWithTask(_dlg))
							{
								var fixes = (string)progressDlg.RunTask(true, FixDataFile, pathname);
								if (fixes.Length <= 0)
								{
									return;
								}
								MessageBox.Show(fixes, LanguageExplorerResources.ksErrorsFoundOrFixed);
								File.WriteAllText(pathname.Replace(LcmFileHelper.ksFwDataXmlFileExtension, "fixes"), fixes);
							}
						}
					}
					catch
					{
					}
				}
			}
			#endregion

			private object FixDataFile(IProgress progressDlg, params object[] parameters)
			{
				var pathname = parameters[0] as string;
				var bldr = new StringBuilder();
				var data = new FwDataFixer(pathname, progressDlg, LogErrors, ErrorCount);
				_errorsFixed = 0;
				_errors.Clear();
				data.FixErrorsAndSave();
				foreach (var err in _errors)
				{
					bldr.AppendLine(err);
				}
				return bldr.ToString();
			}

			private void LogErrors(string message, bool errorFixed)
			{
				_errors.Add(message);
				if (errorFixed)
				{
					++_errorsFixed;
				}
			}

			private int ErrorCount()
			{
				return _errorsFixed;
			}

			/// <summary>
			/// Override method to return the Label property.
			/// </summary>
			public override string ToString()
			{
				return LanguageExplorerResources.ksFindAndFixErrors;
			}
		}

		private sealed class WriteAllObjectsUtility : IUtility
		{
			private readonly LcmCache _cache;

			/// <summary />
			internal WriteAllObjectsUtility(LcmCache cache)
			{
				Guard.AgainstNull(cache, nameof(cache));

				_cache = cache;
			}

			#region IUtility implementation

			/// <summary>
			/// State what the utility does, or FwUtilsStrings.ksThreeQuestionMarks, if there is no what description.
			/// </summary>
			string IUtility.WhatDescription => LanguageExplorerResources.ksWhatIsWriteAllObjects;

			/// <summary>
			/// State when the utility should be run, or FwUtilsStrings.ksThreeQuestionMarks, if there is no when description.
			/// </summary>
			string IUtility.WhenDescription => LanguageExplorerResources.ksWhenToWriteAllObjects;

			/// <summary>
			/// State what the utility does for a redo, or FwUtilsStrings.ksThreeQuestionMarks, if there is no redo description.
			/// </summary>
			string IUtility.RedoDescription => LanguageExplorerResources.ksWriteAllObjectsUndo;

			/// <summary />
			void IUtility.Process()
			{
				_cache.ExportEverythingAsModified();
			}

			#endregion IUtility implementation

			/// <summary>
			/// Override method to return the Label property.
			/// </summary>
			public override string ToString()
			{
				return "Write Everything";
			}
		}

		/// <summary>
		/// What: This utility finds groups of Wordforms that have the same text form in all writing systems
		///		(though possibly some may be missing some alternatives). It merges such groups into a single wordform.
		///		It keeps all the analyses, which may result in some duplicate analyses to sort out using the
		///		Word Analyses tool. Spelling status will be set to Correct if any of the old wordforms is Correct,
		///		and to Incorrect if any old form is Incorrect; otherwise it stays Undecided.
		///
		/// When: This utility finds groups of Wordforms that have the same text form in all writing systems
		///		(though possibly some may be missing some alternatives). It merges such groups into a single wordform.
		///		It keeps all the analyses, which may result in some duplicate analyses to sort out using the Word Analyses tool.
		///		Spelling status will be set to Correct if any of the old wordforms is Correct, and to Incorrect if
		///		any old form is Incorrect; otherwise it stays Undecided.
		/// </summary>
		private sealed class DuplicateWordformFixer : IUtility
		{
			private readonly UtilityDlg _dlg;

			/// <summary />
			internal DuplicateWordformFixer(UtilityDlg utilityDlg)
			{
				_dlg = utilityDlg ?? throw new ArgumentNullException(nameof(utilityDlg));
			}

			#region IUtility implementation

			/// <summary>
			/// State what the utility does, or FwUtilsStrings.ksThreeQuestionMarks, if there is no what description.
			/// </summary>
			string IUtility.WhatDescription => LanguageExplorerResources.ksMergeWordformsAttemptsTo;

			/// <summary>
			/// State when the utility should be run, or FwUtilsStrings.ksThreeQuestionMarks, if there is no when description.
			/// </summary>
			string IUtility.WhenDescription => LanguageExplorerResources.ksUseMergeWordformsWhen;

			/// <summary>
			/// State what the utility does for a redo, or FwUtilsStrings.ksThreeQuestionMarks, if there is no redo description.
			/// </summary>
			string IUtility.RedoDescription => LanguageExplorerResources.ksMergeWordformsWarning;

			/// <summary>
			/// This actually makes the fix.
			/// </summary>
			void IUtility.Process()
			{
				var cache = _dlg.PropertyTable.GetValue<LcmCache>(FwUtilsConstants.cache);
				string failures = null;
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMergeWordforms, LanguageExplorerResources.ksRedoMergeWordforms, cache.ActionHandlerAccessor,
					() => failures = WfiWordformServices.FixDuplicates(cache, new ProgressBarWrapper(_dlg.ProgressBar)));
				if (!string.IsNullOrEmpty(failures))
				{
					MessageBox.Show(_dlg, string.Format(LanguageExplorerResources.ksWordformMergeFailures, failures), LanguageExplorerResources.ksWarning, MessageBoxButtons.OK);
				}
			}

			#endregion IUtility implementation

			/// <summary>
			/// This is what is actually shown in the dialog as the ID of the task.
			/// </summary>
			public override string ToString()
			{
				return LanguageExplorerResources.ksMergeDuplicateWordforms;
			}
		}

		/// <summary>
		/// What: This utility finds groups of analyses that have the same word category and morphological analysis.
		///		It merges such groups into a single analysis. It keeps all the glosses,
		///		except that if some glosses are duplicates (in all writing systems) such groups will also be merged.
		///		Analyzed texts which use any of the merged analyses will be made to use the merged one.
		///
		/// When: Use this when you discover (e.g., in Word Analyses) that you have more than one copy
		///		of the same analysis of the same wordform. It is especially helpful when you have
		///		many instances of this, for example, as a result of merging work done in multiple places.
		/// </summary>
		private sealed class DuplicateAnalysisFixer : IUtility
		{
			private readonly UtilityDlg _dlg;

			/// <summary />
			internal DuplicateAnalysisFixer(UtilityDlg utilityDlg)
			{
				_dlg = utilityDlg ?? throw new ArgumentNullException(nameof(utilityDlg));
			}

			#region IUtility implementation

			/// <summary>
			/// State what the utility does, or FwUtilsStrings.ksThreeQuestionMarks, if there is no what description.
			/// </summary>
			string IUtility.WhatDescription => LanguageExplorerResources.ksMergeAnalysesAttemptsTo;

			/// <summary>
			/// State when the utility should be run, or FwUtilsStrings.ksThreeQuestionMarks, if there is no when description.
			/// </summary>
			string IUtility.WhenDescription => LanguageExplorerResources.ksUseMergeAnalysesWhen;

			/// <summary>
			/// State what the utility does for a redo, or FwUtilsStrings.ksThreeQuestionMarks, if there is no redo description.
			/// </summary>
			string IUtility.RedoDescription => LanguageExplorerResources.ksMergeAnalysesWarning;

			/// <summary>
			/// This actually makes the fix.
			/// </summary>
			void IUtility.Process()
			{
				var cache = _dlg.PropertyTable.GetValue<LcmCache>(FwUtilsConstants.cache);
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMergeAnalyses, LanguageExplorerResources.ksRedoMergeAnalyses, cache.ActionHandlerAccessor,
					() => WfiWordformServices.MergeDuplicateAnalyses(cache, new ProgressBarWrapper(_dlg.ProgressBar)));
			}

			#endregion IUtility implementation

			/// <summary>
			/// This is what is actually shown in the dialog as the ID of the task.
			/// </summary>
			public override string ToString()
			{
				return LanguageExplorerResources.ksMergeDuplicateAnalyses;
			}
		}

		/// <summary />
		private sealed class ParseIsCurrentFixer : IUtility
		{
			private readonly LcmCache _cache;
			private readonly ProgressBar _progressBar;

			/// <summary />
			internal ParseIsCurrentFixer(LcmCache cache, ProgressBar progressBar)
			{
				Guard.AgainstNull(cache, nameof(cache));
				Guard.AgainstNull(progressBar, nameof(progressBar));

				_cache = cache;
				_progressBar = progressBar;
			}

			#region IUtility implementation

			/// <summary>
			/// State what the utility does, or FwUtilsStrings.ksThreeQuestionMarks, if there is no what description.
			/// </summary>
			string IUtility.WhatDescription => LanguageExplorerResources.ksClearParseIsCurrentDoes;

			/// <summary>
			/// State when the utility should be run, or FwUtilsStrings.ksThreeQuestionMarks, if there is no when description.
			/// </summary>
			string IUtility.WhenDescription => LanguageExplorerResources.ksUseClearParseIsCurrentWhen;

			/// <summary>
			/// State what the utility does for a redo, or FwUtilsStrings.ksThreeQuestionMarks, if there is no redo description.
			/// </summary>
			string IUtility.RedoDescription => LanguageExplorerResources.ksParseIsCurrentWarning;

			/// <summary>
			/// This actually makes the fix.
			/// </summary>
			void IUtility.Process()
			{
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMergeWordforms, LanguageExplorerResources.ksRedoMergeWordforms, _cache.ActionHandlerAccessor, () => ClearFlags());

			}

			#endregion IUtility implementation

			private void ClearFlags()
			{
				var paras = _cache.ServiceLocator.GetInstance<IStTxtParaRepository>().AllInstances().ToArray();
				_progressBar.Minimum = 0;
				_progressBar.Maximum = paras.Length;
				_progressBar.Step = 1;
				foreach (var para in paras)
				{
					_progressBar.PerformStep();
					para.ParseIsCurrent = false;
				}
			}

			/// <summary>
			/// This is what is actually shown in the dialog as the ID of the task.
			/// </summary>
			public override string ToString()
			{
				return LanguageExplorerResources.ksClearParseIsCurrent;
			}
		}

		/// <summary>
		/// This class is used in Tools...Utilities to delete all entries and senses that do not have
		/// analyzed occurrences in the interesting list of interlinear texts. It warns the user prior
		/// to actually deleting the entries and senses.
		/// </summary>
		private sealed class DeleteEntriesSensesWithoutInterlinearization : IUtility
		{
			private readonly FlexComponentParameters _flexComponentParameters;
			private readonly LcmCache _cache;
			private readonly ProgressBar _progressBar;


			/// <summary />
			internal DeleteEntriesSensesWithoutInterlinearization(FlexComponentParameters flexComponentParameters, LcmCache cache, ProgressBar progressBar)
			{
				Guard.AgainstNull(flexComponentParameters, nameof(flexComponentParameters));
				Guard.AgainstNull(cache, nameof(cache));
				Guard.AgainstNull(progressBar, nameof(progressBar));

				_flexComponentParameters = flexComponentParameters;
				_cache = cache;
				_progressBar = progressBar;
			}

			#region IUtility implementation

			/// <summary>
			/// State what the utility does, or FwUtilsStrings.ksThreeQuestionMarks, if there is no what description.
			/// </summary>
			string IUtility.WhatDescription => LanguageExplorerResources.ksDeleteEntriesSensesDoes;

			/// <summary>
			/// State when the utility should be run, or FwUtilsStrings.ksThreeQuestionMarks, if there is no when description.
			/// </summary>
			string IUtility.WhenDescription => LanguageExplorerResources.ksDeleteEntriesSensesWhen;

			/// <summary>
			/// State what the utility does for a redo, or FwUtilsStrings.ksThreeQuestionMarks, if there is no redo description.
			/// </summary>
			string IUtility.RedoDescription => LanguageExplorerResources.ksDeleteEntriesSensesWarning;

			/// <summary>
			/// Have the utility do what it does.
			/// </summary>
			void IUtility.Process()
			{
				NonUndoableUnitOfWorkHelper.Do(_cache.ActionHandlerAccessor, () =>
				{
					DeleteUnusedEntriesAndSenses();
				});
			}

			#endregion

			/// <summary>
			/// This is what is actually shown in the dialog as the ID of the task.
			/// </summary>
			public override string ToString()
			{
				return LanguageExplorerResources.ksDeleteEntriesSenses;
			}

			private void DeleteUnusedEntriesAndSenses()
			{
				var cd = new ConcDecorator(_cache.ServiceLocator);
				cd.InitializeFlexComponent(_flexComponentParameters);
				var entries = _cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances().ToArray();
				_progressBar.Minimum = 0;
				_progressBar.Maximum = entries.Length;
				_progressBar.Step = 1;
				var entriesToDel = new List<ILexEntry>();
				foreach (var entry in entries)
				{
					var count = 0;
					_progressBar.PerformStep();
					var forms = new List<IMoForm>();
					if (entry.LexemeFormOA != null)
					{
						forms.Add(entry.LexemeFormOA);
					}
					forms.AddRange(entry.AlternateFormsOS);
					foreach (var mfo in forms)
					{
						foreach (var cmo in mfo.ReferringObjects)
						{
							if (!(cmo is IWfiMorphBundle))
							{
								continue;
							}
							count += cd.get_VecSize(cmo.Owner.Hvo, ConcDecorator.kflidWaOccurrences);
							if (count > 0)
							{
								break;
							}
						}
						if (count > 0)
						{
							break;
						}
					}
					if (count == 0)
					{
						entriesToDel.Add(entry);
					}
				}
				// Warn if entries are to be deleted. We'll assume a specific warning for senses is not critical.
				if (entriesToDel.Count > 0)
				{
					var dlgTxt = string.Format(LanguageExplorerResources.ksDeleteEntrySenseConfirmText, entriesToDel.Count);
					if (MessageBox.Show(dlgTxt, LanguageExplorerResources.ksDeleteEntrySenseConfirmTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
					{
						return;
					}
				}
				_progressBar.Value = 1;
				_progressBar.Maximum = entriesToDel.Count;
				foreach (var entry in entriesToDel)
				{
					_progressBar.PerformStep();
					_cache.DomainDataByFlid.DeleteObj(entry.Hvo);
				}
				var senses = _cache.ServiceLocator.GetInstance<ILexSenseRepository>().AllInstances().ToArray();
				_progressBar.Value = 1;
				_progressBar.Maximum = senses.Length;
				foreach (var sense in senses)
				{
					_progressBar.PerformStep();
					var count = 0;
					foreach (var cmo in sense.ReferringObjects)
					{
						if (!(cmo is IWfiMorphBundle))
						{
							continue;
						}
						count += cd.get_VecSize(cmo.Owner.Hvo, ConcDecorator.kflidWaOccurrences);
						if (count > 0)
						{
							break;
						}
					}
					if (count == 0)
					{
						_cache.DomainDataByFlid.DeleteObj(sense.Hvo);
					}
				}
			}
		}

		/// <summary>
		/// Abstract base class for the two implementations: LexEntryTypeConverter & LexEntryInflTypeConverter.
		///
		/// LexEntryTypeConverter.
		/// What: This utility allows you to select which irregularly inflected form variant types should be converted
		///		to variant types (irregularly inflected form variant types are a special sub-kind of variant types).
		/// When: Run this utility when you need to convert one or more of your existing irregularly inflected form
		///		variant types to be variant types.  When a variant type is an irregularly inflected form variant type,
		///		it has extra fields such as 'Append to Gloss', 'Inflection Features', and 'Slots.'
		///
		/// LexEntryInflTypeConverter.
		/// What: This utility allows you to select which variant types should be converted
		///		to irregularly inflected form variant types, which are a special sub-kind of variant types.
		/// When: Run this utility when you need to convert one or more of your existing variant types to be irregularly inflected form variant types.
		///		When a variant type is an irregularly inflected form variant type, it has extra fields such as 'Append to Gloss', 'Inflection Features', and 'Slots.'
		/// </summary>
		private abstract class LexEntryTypeConverters
		{
			/// <summary />
			protected UtilityDlg _dlg;
			/// <summary />
			protected LcmCache _cache;
			/// <summary />
			private int m_flid;
			/// <summary />
			private ICmObject m_obj;
			/// <summary />
			private const string s_helpTopic = "khtpToolsConvertVariants";

			protected LexEntryTypeConverters(UtilityDlg utilityDlg)
			{
				_dlg = utilityDlg ?? throw new ArgumentNullException(nameof(utilityDlg));
				_cache = _dlg.PropertyTable.GetValue<LcmCache>(FwUtilsConstants.cache);
			}

			/// <summary />
			private static void DisableNodes(TreeNodeCollection nodes, int classId)
			{
				foreach (TreeNode tnode in nodes)
				{
					if (!(tnode is LabelNode node))
					{
						continue;
					}
					var label = node.Label;
					var obj = label.Object;
					if (obj.ClassID == classId)
					{
						node.Enabled = false;
					}
					DisableNodes(node.Nodes, classId);
				}
			}

			/// <summary />
			protected abstract void Convert(IEnumerable<ILexEntryType> itemsToChange);

			/// <summary>
			/// Overridden to provide a chooser with multiple selections (checkboxes and all).
			/// </summary>
			private SimpleListChooser GetChooser(IEnumerable<ObjectLabel> labels, int classId)
			{
				return new SimpleListChooser(_dlg.PropertyTable.GetValue<IPersistenceProvider>("persistProvider"), labels,
					StringTable.Table.GetString("VariantEntryTypes", StringTable.PossibilityListItemTypeNames), _cache,
					_cache.LangProject.LexDbOA.VariantEntryTypesOA.ReallyReallyAllPossibilities.Where(lexEntryType => lexEntryType.ClassID == classId),
					_dlg.PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider));
			}

			/// <summary />
			protected void ShowDialogAndConvert(int targetClassId)
			{
				// maybe there's a better way, but
				// this creates a temporary LexEntryRef in a temporary LexEntry
				var leFactory = _cache.ServiceLocator.GetInstance<ILexEntryFactory>();
				var entry = leFactory.Create();
				var lerFactory = _cache.ServiceLocator.GetInstance<ILexEntryRefFactory>();
				var ler = lerFactory.Create();
				entry.EntryRefsOS.Add(ler);
				m_flid = LexEntryRefTags.kflidVariantEntryTypes;
				m_obj = ler;
				var labels = ObjectLabel.CreateObjectLabels(_cache, m_obj.ReferenceTargetCandidates(m_flid), "LexEntryType", "best analysis");
				using (var chooser = GetChooser(labels, targetClassId))
				{
					chooser.Cache = _cache;
					chooser.SetObjectAndFlid(m_obj.Hvo, m_flid);
					chooser.SetHelpTopic(s_helpTopic);
					var tv = chooser.TreeView;
					DisableNodes(tv.Nodes, targetClassId);
					_dlg.Visible = false; // no reason to show the utility dialog, too
					var res = chooser.ShowDialog(_dlg.FindForm());
					if (res == DialogResult.OK && chooser.ChosenObjects.Any())
					{
						var itemsToChange = chooser.ChosenObjects.Where(lexEntryType => lexEntryType.ClassID != targetClassId).Cast<ILexEntryType>();
						Convert(itemsToChange);
					}
				}
				entry.Delete(); // remove the temporary LexEntry
				_dlg.Visible = true; // now we show the utility dialog again
			}
		}

		/// <summary>
		/// What: This utility allows you to select which variant types should be converted to irregularly inflected form variant types, which are a special sub-kind of variant types.
		/// When: Run this utility when you need to convert one or more of your existing variant types to be irregularly inflected form variant types.
		///		When a variant type is an irregularly inflected form variant type, it has extra fields such as 'Append to Gloss', 'Inflection Features', and 'Slots.'
		/// </summary>
		private sealed class LexEntryInflTypeConverter : LexEntryTypeConverters, IUtility
		{
			/// <summary />
			internal LexEntryInflTypeConverter(UtilityDlg utilityDlg)
				: base(utilityDlg)
			{
			}

			#region IUtility implementation

			/// <summary>
			/// State what the utility does, or FwUtilsStrings.ksThreeQuestionMarks, if there is no what description.
			/// </summary>
			string IUtility.WhatDescription => LanguageExplorerResources.ksWhatIsConvertIrregularlyInflectedFormVariants;

			/// <summary>
			/// State when the utility should be run, or FwUtilsStrings.ksThreeQuestionMarks, if there is no when description.
			/// </summary>
			string IUtility.WhenDescription => LanguageExplorerResources.ksWhenToConvertIrregularlyInflectedFormVariants;

			/// <summary>
			/// State what the utility does for a redo, or FwUtilsStrings.ksThreeQuestionMarks, if there is no redo description.
			/// </summary>
			string IUtility.RedoDescription => LanguageExplorerResources.ksCannotRedoConvertIrregularlyInflectedFormVariants;

			/// <summary>
			/// Have the utility do what it does.
			/// </summary>
			void IUtility.Process()
			{
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoConvertIrregularlyInflectedFormVariants, LanguageExplorerResources.ksRedoConvertIrregularlyInflectedFormVariants,
											_cache.ActionHandlerAccessor, () => ShowDialogAndConvert(LexEntryInflTypeTags.kClassId));
			}

			#endregion IUtility implementation

			/// <summary>
			/// Override method to return the Label property.
			/// </summary>
			/// <returns></returns>
			public override string ToString()
			{
				return LanguageExplorerResources.ksConvertIrregularlyInflectedFormVariants;
			}

			/// <summary />
			protected override void Convert(IEnumerable<ILexEntryType> itemsToChange)
			{
				_cache.LanguageProject.LexDbOA.ConvertLexEntryInflTypes(new ProgressBarWrapper(_dlg.ProgressBar), itemsToChange);
			}
		}

		/// <summary>
		/// What: This utility allows you to select which irregularly inflected form variant types should be converted
		///		to variant types (irregularly inflected form variant types are a special sub-kind of variant types).
		/// When: Run this utility when you need to convert one or more of your existing irregularly inflected form
		///		variant types to be variant types.  When a variant type is an irregularly inflected form variant type,
		///		it has extra fields such as 'Append to Gloss', 'Inflection Features', and 'Slots.'
		/// </summary>
		private sealed class LexEntryTypeConverter : LexEntryTypeConverters, IUtility
		{
			/// <summary />
			internal LexEntryTypeConverter(UtilityDlg utilityDlg)
				: base(utilityDlg)
			{
			}

			#region IUtility implementation

			/// <summary>
			/// State what the utility does, or FwUtilsStrings.ksThreeQuestionMarks, if there is no what description.
			/// </summary>
			string IUtility.WhatDescription => LanguageExplorerResources.ksWhatIsConvertVariants;

			/// <summary>
			/// State when the utility should be run, or FwUtilsStrings.ksThreeQuestionMarks, if there is no when description.
			/// </summary>
			string IUtility.WhenDescription => LanguageExplorerResources.ksWhenToConvertVariants;

			/// <summary>
			/// State what the utility does for a redo, or FwUtilsStrings.ksThreeQuestionMarks, if there is no redo description.
			/// </summary>
			string IUtility.RedoDescription => LanguageExplorerResources.ksCannotRedoConvertVariants;

			/// <summary>
			/// Have the utility do what it does.
			/// </summary>
			void IUtility.Process()
			{
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoConvertVariants, LanguageExplorerResources.ksRedoConvertVariants,
					_cache.ActionHandlerAccessor, () => ShowDialogAndConvert(LexEntryTypeTags.kClassId));

			}

			#endregion IUtility implementation

			/// <summary>
			/// Override method to return the Label property.
			/// </summary>
			/// <returns></returns>
			public override string ToString()
			{
				return LanguageExplorerResources.ksConvertVariants;
			}

			/// <summary />
			protected override void Convert(IEnumerable<ILexEntryType> itemsToChange)
			{
				_cache.LanguageProject.LexDbOA.ConvertLexEntryTypes(new ProgressBarWrapper(_dlg.ProgressBar), itemsToChange);
			}
		}

		/// <summary>
		/// This class implements a utility to allow users to fix any part of speech guids that do not match the GOLD etic file.
		/// This is needed to simplify cross language analysis. We need this because there was a defect in FLEx for a number of years
		/// which did not use the correct guid for the items inserted into a new project.
		/// </summary>
		private sealed class GoldEticGuidFixer : IUtility
		{
			private readonly UtilityDlg _dlg;
			private readonly LcmCache _cache;

			/// <summary />
			internal GoldEticGuidFixer(UtilityDlg utilityDlg)
			{
				Guard.AgainstNull(utilityDlg, nameof(utilityDlg));

				_dlg = utilityDlg;
				_cache = _dlg.PropertyTable.GetValue<LcmCache>(FwUtilsConstants.cache);
			}

			#region IUtility implementation

			/// <summary>
			/// State what the utility does, or FwUtilsStrings.ksThreeQuestionMarks, if there is no what description.
			/// </summary>
			string IUtility.WhatDescription => LanguageExplorerResources.ksWhatIsSetPartOfSpeechGUIDsToGold;

			/// <summary>
			/// State when the utility should be run, or FwUtilsStrings.ksThreeQuestionMarks, if there is no when description.
			/// </summary>
			string IUtility.WhenDescription => LanguageExplorerResources.ksWhenToSetPartOfSpeechGUIDsToGold;

			/// <summary>
			/// State what the utility does for a redo, or FwUtilsStrings.ksThreeQuestionMarks, if there is no redo description.
			/// </summary>
			string IUtility.RedoDescription => LanguageExplorerResources.ksGenericUtilityCannotUndo;

			/// <summary>
			/// Have the utility do what it does.
			/// </summary>
			void IUtility.Process()
			{
				NonUndoableUnitOfWorkHelper.DoSomehow(_cache.ActionHandlerAccessor, () =>
				{
					var fixedGuids = _cache.ReplacePOSGuidsWithGoldEticGuids();
					var caption = fixedGuids ? LanguageExplorerResources.GoldEticGuidFixer_Guids_changed_Title : LanguageExplorerResources.GoldEticGuidFixer_NoChangeTitle;
					var content = fixedGuids ? LanguageExplorerResources.GoldEticGuidFixer_GuidsChangedContent : LanguageExplorerResources.GoldEticGuidFixer_NoChangeContent;
					MessageBox.Show(_dlg, content, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
				});
			}

			#endregion

			/// <summary>
			/// Override to return the Label.
			/// </summary>
			/// <returns>The Label</returns>
			public override string ToString()
			{
				return LanguageExplorerResources.GoldEticGuidFixer_Label;
			}
		}

		private sealed class SortReversalSubEntries : IUtility
		{
			private readonly UtilityDlg _dlg;
			private readonly LcmCache _cache;

			/// <summary />
			internal SortReversalSubEntries(UtilityDlg utilityDlg)
			{
				Guard.AgainstNull(utilityDlg, nameof(utilityDlg));

				_dlg = utilityDlg;
				_cache = _dlg.PropertyTable.GetValue<LcmCache>(FwUtilsConstants.cache);
			}

			#region IUtility implementation

			/// <summary>
			/// State what the utility does, or FwUtilsStrings.ksThreeQuestionMarks, if there is no what description.
			/// </summary>
			string IUtility.WhatDescription => LanguageExplorerResources.ksWhatIsSortReversalSubentries;

			/// <summary>
			/// State when the utility should be run, or FwUtilsStrings.ksThreeQuestionMarks, if there is no when description.
			/// </summary>
			string IUtility.WhenDescription => LanguageExplorerResources.ksWhenToSortReversalSubentries;

			/// <summary>
			/// State what the utility does for a redo, or FwUtilsStrings.ksThreeQuestionMarks, if there is no redo description.
			/// </summary>
			string IUtility.RedoDescription => LanguageExplorerResources.ksWarningSortReversalSubentries;

			/// <summary />
			void IUtility.Process()
			{
				NonUndoableUnitOfWorkHelper.DoSomehow(_cache.ActionHandlerAccessor, () =>
				{
					_cache.SortReversalSubEntriesInPlace();
					MessageBox.Show(_dlg, LanguageExplorerResources.SortReversalSubEntries_CompletedContent, LanguageExplorerResources.SortReversalSubEntries_CompletedTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
				});
			}

			#endregion

			/// <summary>
			/// This is what is actually shown in the dialog as the ID of the task.
			/// </summary>
			public override string ToString()
			{
				return LanguageExplorerResources.SortReversalSubentries_Label;
			}
		}

		/// <summary>
		/// Go through all the PrimaryLexeme lists of complex form LexEntryRefs searching for and fixing any circular references.
		/// If a circular reference is found, the entry with the longer headword is removed as a component (and primary lexeme)
		/// of the other one.
		/// </summary>
		/// <remarks>
		/// This fixes https://jira.sil.org/browse/LT-16362.
		/// </remarks>
		private sealed class CircularRefBreaker : IUtility
		{
			private readonly LcmCache _cache;

			/// <summary />
			internal CircularRefBreaker(LcmCache cache)
			{
				Guard.AgainstNull(cache, nameof(cache));

				_cache = cache;
			}

			#region Implement IUtility

			/// <summary>
			/// State what the utility does, or FwUtilsStrings.ksThreeQuestionMarks, if there is no what description.
			/// </summary>
			string IUtility.WhatDescription => LanguageExplorerResources.ksWhatAreCircularRefs;

			/// <summary>
			/// State when the utility should be run, or FwUtilsStrings.ksThreeQuestionMarks, if there is no when description.
			/// </summary>
			string IUtility.WhenDescription => LanguageExplorerResources.ksTryIfProgramGoesPoof;

			/// <summary>
			/// State what the utility does for a redo, or FwUtilsStrings.ksThreeQuestionMarks, if there is no redo description.
			/// </summary>
			string IUtility.RedoDescription => LanguageExplorerResources.ksGenericUtilityCannotUndo;

			/// <summary />
			void IUtility.Process()
			{
				CircularRefBreakerService.ReferenceBreaker(_cache, out var count, out var circular, out var report);
				// Show the message returned from running the circular reference breaker service.
				MessageBox.Show(report, LanguageExplorerResources.ksCircularRefsFixed);
				Logger.WriteEvent(report);
			}

			#endregion

			/// <summary>
			/// Override method to return the Label property.  This is really needed.
			/// </summary>
			public override string ToString()
			{
				return LanguageExplorerResources.ksBreakCircularRefs;
			}
		}

		#endregion IUtility implementations

		/// <summary>
		/// Wrapper class to allow a ProgressBar to function as an IProgress
		/// </summary>
		private sealed class ProgressBarWrapper : IProgress
		{
			/// <summary>
			/// Gets the wrapped ProgressBar
			/// </summary>
			private ProgressBar ProgressBar { get; }

			/// <summary>
			/// Constructor which passes in the progressBar to wrap
			/// </summary>
			internal ProgressBarWrapper(ProgressBar progressBar)
			{
				ProgressBar = progressBar;
			}

			#region IProgress implementation
			/// <summary>
			/// Event handler for listening to whether or the cancel button is pressed.
			/// </summary>
			public event CancelEventHandler Canceling;

			/// <summary>
			/// Cause the progress indicator to advance by the specified amount.
			/// </summary>
			/// <param name="amount">Amount of progress.</param>
			public void Step(int amount)
			{
				var stepSizeHold = StepSize;
				StepSize = amount;
				ProgressBar.PerformStep();
				StepSize = stepSizeHold;
				if (Canceling != null)
				{
					// don't do anything -- this just shuts up the compiler about the
					// event handler never being used.
				}
			}

			/// <summary>
			/// The title of the progress display window.
			/// </summary>
			public string Title { get; set; }

			/// <summary>
			/// The message within the progress display window.
			/// </summary>
			public string Message { get; set; }

			/// <summary>
			/// The current position of the progress bar. This should be within the limits set by
			/// SetRange, or returned by GetRange.
			/// </summary>
			public int Position
			{
				get => ProgressBar.Value;
				set => ProgressBar.Value = value;
			}

			/// <summary>
			/// The size of the step increment used by Step.
			/// </summary>
			public int StepSize
			{
				get => ProgressBar.Step;
				set => ProgressBar.Step = value;
			}

			/// <summary>
			/// The minimum value of the progress bar.
			/// </summary>
			public int Minimum
			{
				get => ProgressBar.Minimum;
				set => ProgressBar.Minimum = value;
			}
			/// <summary>
			/// The maximum value of the progress bar.
			/// </summary>
			public int Maximum
			{
				get => ProgressBar.Maximum;
				set => ProgressBar.Maximum = value;
			}

			/// <summary>
			/// Gets an object to be used for ensuring that required tasks are invoked on the main UI thread.
			/// </summary>
			public ISynchronizeInvoke SynchronizeInvoke => ProgressBar;

			/// <summary>
			/// Gets or sets a value indicating whether this progress is indeterminate.
			/// </summary>
			public bool IsIndeterminate
			{
				get => ProgressBar.Style == ProgressBarStyle.Marquee;
				set => ProgressBar.Style = value ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
			}

			/// <summary>
			/// Gets or sets a value indicating whether the opertation executing on the separate thread
			/// can be cancelled by a different thread (typically the main UI thread).
			/// </summary>
			public bool AllowCancel
			{
				get => false;
				set { }
			}
			#endregion
		}
	}
}