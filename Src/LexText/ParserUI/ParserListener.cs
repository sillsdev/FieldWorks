// Copyright (c) 2002-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// <remarks>
// This is an XCore "Listener" which facilitates interaction with the Parser.
// </remarks>
// <example>
//	<code>
//		<listeners>
//			<listener assemblyPath="LexTextDll.dll" class="SIL.FieldWorks.LexText"/>
//		</listeners>
//	</code>
// </example>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.FieldWorks.XWorks;
using SIL.Utils;
using XCore;
using SIL.ObjectModel;
using SIL.LCModel.Core.Text;
using System.Runtime.Remoting.Contexts;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.Controls;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// this class just gets all the parser calling and event and receiving
	/// out of the form code. It is scheduled for refactoring
	/// </summary>
	[MediatorDispose]
	public class ParserListener : IxCoreColleague, IDisposable, IVwNotifyChange
	{
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private LcmCache m_cache; //a pointer to the one owned by from the form
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		/// <summary>
		/// Control how much output we send to the application's listeners (e.g. visual studio output window)
		/// </summary>
		private TraceSwitch m_traceSwitch = new TraceSwitch("ParserListener", "");
		private TryAWordDlg m_dialog;
		private FormWindowState m_prevWindowState;
		private ParserConnection m_parserConnection;
		private Timer m_timer;
		// Keep track of parse results as we parse wordforms.
		private Dictionary<IWfiWordform, ParseResult> m_checkParserResults = null;
		private int m_checkParserResultsCount = 0;
		private string m_sourceText = null;
		private ObservableCollection<ParserReportViewModel> m_parserReports = null;
		private ParserReportsDialog m_parserReportsDialog = null;
		private string m_defaultComment = null;

		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			CheckDisposed();

			m_mediator = mediator;
			m_propertyTable = propertyTable;
			m_cache = m_propertyTable.GetValue<LcmCache>("cache");
			mediator.AddColleague(this);

			m_sda = m_cache.MainCacheAccessor;
			m_sda.AddNotification(this);
		}

		/// <summary>
		/// return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// </summary>
		/// <returns></returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			return new IxCoreColleague[] { this };
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


		public ParserConnection Connection
		{
			get
			{
				CheckDisposed();
				return m_parserConnection;
			}
			set
			{
				CheckDisposed();
				m_parserConnection = value;
			}
		}

		/// <summary>
		/// Send the newly selected wordform on to the parser.
		/// </summary>
		public void OnPropertyChanged(string propertyName)
		{
			CheckDisposed();

			if (m_parserConnection != null && propertyName == "ActiveClerkSelectedObject")
			{
				var wordform = m_propertyTable.GetValue<ICmObject>(propertyName) as IWfiWordform;
				if (wordform != null)
				{
					UpdateWordform(wordform, ParserPriority.High);
				}
			}
		}

		#region IVwNotifyChange Members

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			// If someone updated the wordform inventory with a real wordform, schedule it to be parsed.
			if (m_parserConnection != null && tag == WfiWordformTags.kflidForm)
			{
				// the form of this WfiWordform was changed, so update its parse info.
				UpdateWordform(m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().GetObject(hvo), ParserPriority.High);
			}
		}

		#endregion

		#region Timer Related

		private const int TIMER_INTERVAL = 250; // every 1/4 second

		private void StartProgressUpdateTimer()
		{
			if (m_timer == null)
			{
				m_timer = new Timer();
				m_timer.Interval = TIMER_INTERVAL;
				m_timer.Tick += m_timer_Tick;
			}
			m_timer.Start();
		}

		private void StopUpdateProgressTimer()
		{
			if (m_timer != null)
				m_timer.Stop();
		}

		public void m_timer_Tick(object sender, EventArgs eventArgs)
		{
			UpdateStatusPanelProgress();
		}

		#endregion

		public bool ConnectToParser()
		{
			CheckDisposed();

			if (m_parserConnection == null)
			{
				// Don't bother if the lexicon is empty.  See FWNX-1019.
				if (m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().Count == 0)
					return false;
				m_parserConnection = new ParserConnection(m_cache, m_mediator.IdleQueue, WordformUpdatedEventHandler);
			}
			StartProgressUpdateTimer();
			return true;
		}

		public void DisconnectFromParser()
		{
			CheckDisposed();

			StopUpdateProgressTimer();
			if (m_parserConnection != null)
			{
				m_parserConnection.Dispose();
			}
			m_parserConnection = null;
		}

		public bool OnIdle(object argument)
		{
			CheckDisposed();

			UpdateStatusPanelProgress();

			return false; // Don't stop other people from getting the idle message
		}

		// Now called by timer AND by OnIdle
		private void UpdateStatusPanelProgress()
		{
			var statusMessage = ParserQueueString + " " + ParserActivityString;
			m_propertyTable.SetProperty("StatusPanelProgress", statusMessage, true);
			m_propertyTable.SetPropertyPersistence("StatusPanelProgress", false);

			if (m_parserConnection != null)
			{
				Exception ex = m_parserConnection.UnhandledException;
				if (ex != null)
				{
					DisconnectFromParser();
					var app = m_propertyTable.GetValue<IApp>("App");
					ErrorReporter.ReportException(ex, app.SettingsKey, app.SupportEmailAddress,
													app.ActiveMainWindow, false);
				}
				else
				{
					string notification = m_parserConnection.GetAndClearNotification();
					if (notification != null)
						m_mediator.SendMessage("ShowNotification", notification);
				}
			}
			if (ParserActivityString == ParserUIStrings.ksIdle_ && m_timer.Enabled)
				StopUpdateProgressTimer();
		}

		//note that the Parser also supports an event oriented system
		//so that we are notified for every single event that happens.
		//Here, we have instead chosen to use the polling ability.
		//We will thus missed some events but not get slowed down with too many.
		public string ParserActivityString
		{
			get
			{
				CheckDisposed();

				return m_parserConnection == null ? ParserUIStrings.ksNoParserLoaded : m_parserConnection.Activity;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public string ParserQueueString
		{
			get
			{
				CheckDisposed();

				string low = ParserUIStrings.ksDash;
				string med = ParserUIStrings.ksDash;
				string high = ParserUIStrings.ksDash;
				if (m_parserConnection != null)
				{
					low = m_parserConnection.GetQueueSize(ParserPriority.Low).ToString();
					med = m_parserConnection.GetQueueSize(ParserPriority.Medium).ToString();
					high = m_parserConnection.GetQueueSize(ParserPriority.High).ToString();
				}

				return string.Format(ParserUIStrings.ksQueueXYZ, low, med, high);
			}
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

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

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed;

		private const string ParserLockName = "parser";

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~ParserListener()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				// other clients may now parse
				// Dispose managed resources here.
				if (m_timer != null)
				{
					m_timer.Stop();
					m_timer.Tick -= m_timer_Tick;
				}
				if (m_sda != null)
					m_sda.RemoveNotification(this);
				m_mediator.RemoveColleague(this);
				if (m_parserConnection != null)
					m_parserConnection.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_timer = null;
			m_sda = null;
			m_mediator = null;
			m_cache = null;
			m_traceSwitch = null;
			m_parserConnection = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		private IStText CurrentText
		{
			get
			{
				return InInterlinearText ? m_propertyTable.GetValue<IStText>("ActiveClerkSelectedObject") : null;
			}
		}
		private IWfiWordform CurrentWordform
		{
			get
			{
				IWfiWordform wordform = null;
				if (InInterlinearText)
					wordform = m_propertyTable.GetValue<IWfiWordform>("TextSelectedWord");
				else if (InWordAnalyses)
					wordform = m_propertyTable.GetValue<ICmObject>("ActiveClerkSelectedObject") as IWfiWordform;
				return wordform;
			}
		}

		#region ClearSelectedWordParserAnalyses handlers

		public bool OnDisplayClearSelectedWordParserAnalyses(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			bool enable = CurrentWordform != null;
			display.Visible = enable;
			display.Enabled = enable;

			return true;    //we handled this.
		}

		public bool OnClearSelectedWordParserAnalyses(object dummyObj)
		{
			IWfiWordform wf = CurrentWordform;
			UndoableUnitOfWorkHelper.Do(ParserUIStrings.ksUndoClearParserAnalyses,
				ParserUIStrings.ksRedoClearParserAnalyses, m_cache.ActionHandlerAccessor, () =>
			{
				foreach (IWfiAnalysis analysis in wf.AnalysesOC.ToArray())
				{
					ICmAgentEvaluation[] parserEvals = analysis.EvaluationsRC.Where(evaluation => !evaluation.Human).ToArray();
					foreach (ICmAgentEvaluation parserEval in parserEvals)
						analysis.EvaluationsRC.Remove(parserEval);

					if (analysis.EvaluationsRC.Count == 0)
						wf.AnalysesOC.Remove(analysis);

					wf.Checksum = 0;
				}
			});
			return true;    //we handled this.
		}

		#endregion ClearSelectedWordParserAnalyses handlers

		public bool OnDisplayParseCurrentWord(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			bool enable = CurrentWordform != null;
			display.Visible = enable;
			display.Enabled = enable;

			return true;    //we handled this.
		}

		public bool OnParseCurrentWord(object argument)
		{
			CheckDisposed();

			if (ConnectToParser())
			{
				IWfiWordform wf = CurrentWordform;
				UpdateWordform(wf, ParserPriority.High);
			}

			return true;    //we handled this.
		}

		public bool OnDisplayParseWordsInCurrentText(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			bool enable = CurrentText != null;
			display.Visible = enable;
			display.Enabled = enable;

			return true;    //we handled this.
		}

		public bool OnParseWordsInCurrentText(object argument)
		{
			CheckDisposed();

			if (CurrentText != null && ConnectToParser())
			{
				IStText text = CurrentText;
				IEnumerable<IWfiWordform> wordforms = text.UniqueWordforms();
				UpdateWordforms(wordforms, ParserPriority.Medium);
			}

			return true;    //we handled this.
		}

		public bool OnCheckParserOnCurrentText(object argument)
		{
			CheckDisposed();

			if (CurrentText != null && ConnectToParser())
			{
				IEnumerable<IWfiWordform> wordforms = CurrentText.UniqueWordforms();
				UpdateWordforms(wordforms, ParserPriority.Medium, checkParser: true, CurrentText.ShortName);
			}

			return true;    //we handled this.
		}

		public bool OnCheckParserOnGenre(object argument)
		{
			CheckDisposed();

			if (ConnectToParser())
			{
				// Get the selected genre from the user.
				string displayWs = "analysis vernacular";
				var labels = ObjectLabel.CreateObjectLabels(m_cache, m_cache.LanguageProject.GenreListOA.PossibilitiesOS, "", displayWs);
				var chooser = new SimpleListChooser(null, labels, ParserUIStrings.ksGenre, m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"));
				// chooser.SetHelpTopic("FLExHelpFile");
				ExpandTreeViewNodes(chooser.TreeView.Nodes);
				chooser.ShowDialog();
				ICmPossibility selectedGenre = (ICmPossibility)chooser.SelectedObject;
				if (chooser.ChosenOne == null || selectedGenre == null)
					return false;

				// Get all of the wordforms in the genre's texts.
				IEnumerable<IWfiWordform> wordforms = new HashSet<IWfiWordform>();
				foreach (var text in m_cache.LanguageProject.InterlinearTexts.Where(t => t.GenreCategories.Any(genre => ContainsGenre(selectedGenre, genre))))
				{
					wordforms = wordforms.Union(text.UniqueWordforms());
				}
				// Check all of the wordforms.
				var genreName = String.Format(ParserUIStrings.ksXGenre, selectedGenre.Name.AnalysisDefaultWritingSystem.Text);
				UpdateWordforms(wordforms, ParserPriority.Medium, checkParser: true, genreName);
			}

			return true;    //we handled this.
		}

		private void ExpandTreeViewNodes(TreeNodeCollection nodes)
		{
			foreach (TreeNode node in nodes)
			{
				node.Expand();
				ExpandTreeViewNodes(node.Nodes);
			}
		}

		private bool ContainsGenre(ICmPossibility genre1, ICmPossibility genre2)
		{
			while (genre2 != null)
			{
				if (genre1 == genre2) return true;
				genre2 = genre2.Owner as ICmPossibility;
			}
			return false;
		}

		/// <summary>
		/// Check parser on all words in texts.
		/// </summary>
		public bool OnCheckParserOnAll(object argument)
		{
			CheckDisposed();

			if (ConnectToParser())
			{
				IEnumerable<IWfiWordform> wordforms = new HashSet<IWfiWordform>();
				foreach (var text in m_cache.LanguageProject.InterlinearTexts)
				{
					wordforms = wordforms.Union(text.UniqueWordforms());
				}
				UpdateWordforms(wordforms, ParserPriority.Low, checkParser: true, "All Texts");
			}

			return true;    //we handled this.
		}

		public bool OnShowParserReports(object argument)
		{
			CheckDisposed();

			ShowParserReports();

			return true;
		}

		/// <summary>
		/// Run the parser on the given wordforms and then update the wordforms
		/// unless checkParser is true, in which case create a test report instead.
		/// </summary>
		/// <param name="wordforms">The wordforms to parse</param>
		/// <param name="priority">The priority the parser is run at</param>
		/// <param name="checkParser">Whether to check the parser and not update the wordforms</param>
		/// <param name="sourceText">The source text for the word forms (used for the test report)</param>
		private void UpdateWordforms(IEnumerable<IWfiWordform> wordforms, ParserPriority priority, bool checkParser = false, string sourceText = null)
		{
			if (checkParser)
			{
				InitCheckParserResults(wordforms, sourceText);
				if (wordforms.Count() == 0)
				{
					ReadParserReports();
					// Write an empty parser report.
					var parserReport = CreateParserReport();
					ParserReportViewModel viewModel = AddParserReport(parserReport);
					ShowParserReport(viewModel, m_mediator, m_cache);
				}
			}
			m_parserConnection.UpdateWordforms(wordforms, priority, checkParser);
		}

		private void UpdateWordform(IWfiWordform wordform, ParserPriority priority)
		{
			m_parserConnection.UpdateWordform(wordform, priority);
		}

		private void InitCheckParserResults(IEnumerable<IWfiWordform> wordforms, string sourceText)
		{
			// Initialize m_parseResults with the given wordforms.
			if (wordforms == null)
			{
				m_checkParserResults = null;
				m_checkParserResultsCount = 0;
			}
			else
			{
				m_checkParserResults = new Dictionary<IWfiWordform, ParseResult>();
				m_checkParserResultsCount = 0;
				foreach (var wordform in wordforms)
				{
					m_checkParserResults[wordform] = null;
				}
			}
			m_sourceText = sourceText;
		}

		private void WordformUpdatedEventHandler(object sender, WordformUpdatedEventArgs e)
		{
			if (e.CheckParser && m_checkParserResults != null && m_checkParserResults.ContainsKey(e.Wordform))
			{
				// Record the parse result.
				m_checkParserResults[e.Wordform] = e.ParseResult;
				m_checkParserResultsCount++;
				// Verify that all of the wordforms have been processed.
				if (m_checkParserResultsCount < m_checkParserResults.Count())
					return;
				foreach (var key in m_checkParserResults.Keys)
				{
					if (m_checkParserResults[key] == null)
						return;
				}
				// Read parser reports before writing and adding a parser report to avoid duplicates.
				ReadParserReports();
				// Convert parse results into ParserReport.
				var parserReport = CreateParserReport();
				ParserReportViewModel viewModel = AddParserReport(parserReport);
				ShowParserReport(viewModel, m_mediator, m_cache);
			}
		}

		/// <summary>
		/// Create a parser report from the parse results.
		/// </summary>
		ParserReport CreateParserReport()
		{
			var parserReport = new ParserReport(m_cache)
			{
				SourceText = m_sourceText
			};
			if (m_checkParserResults != null)
			{
				foreach (var wordform in m_checkParserResults.Keys)
				{
					if (SuppressableParseResult(wordform))
						continue;
					var parseResult = m_checkParserResults[wordform];
					var parseReport = new ParseReport(wordform, parseResult);
					var form = wordform.Form.VernacularDefaultWritingSystem;
					parserReport.AddParseReport(form.Text, parseReport);
				}
			}
			// Clear the data we wrote.
			m_checkParserResults = null;
			return parserReport;
		}

		public static void SaveParserReport(ParserReportViewModel reportViewModel, LcmCache cache, string defaultComment)
		{
			Form inputBox = CreateInputBox(ParserUIStrings.ksEnterComment, ref defaultComment);
			DialogResult result = inputBox.ShowDialog();
			if (result == DialogResult.OK)
			{
				ParserReport report = reportViewModel.ParserReport;
				Control textBox = inputBox.Controls["input"];
				report.Comment = textBox.Text;
				report.DeleteJsonFile();
				report.WriteJsonFile(cache);
				reportViewModel.UpdateDisplayComment();
			}
		}

		private static Form CreateInputBox(string title, ref string input)
		{
			System.Drawing.Size size = new System.Drawing.Size(400, 70);
			Form inputBox = new Form();

			inputBox.FormBorderStyle = FormBorderStyle.FixedDialog;
			inputBox.ClientSize = size;
			inputBox.Text = title;
			inputBox.StartPosition = FormStartPosition.CenterScreen;

			TextBox textBox = new TextBox();
			textBox.Size = new System.Drawing.Size(size.Width - 10, 23);
			textBox.Location = new System.Drawing.Point(5, 5);
			textBox.Text = input;
			textBox.Name = "input";
			inputBox.Controls.Add(textBox);

			Button okButton = new Button();
			okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			okButton.Name = "okButton";
			okButton.Size = new System.Drawing.Size(75, 23);
			okButton.Text = "&OK";
			okButton.Location = new System.Drawing.Point(size.Width - 80 - 80, 39);
			inputBox.Controls.Add(okButton);

			Button cancelButton = new Button();
			cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			cancelButton.Name = "cancelButton";
			cancelButton.Size = new System.Drawing.Size(75, 23);
			cancelButton.Text = "&Cancel";
			cancelButton.Location = new System.Drawing.Point(size.Width - 80, 39);
			inputBox.Controls.Add(cancelButton);

			inputBox.AcceptButton = okButton;
			inputBox.CancelButton = cancelButton;

			return inputBox;
		}

		/// <summary>
		/// Suppress this parse result if it is an uppercase wordform whose analyses all came from its lowercase version.
		/// This only happens in projects that were parsed before we decided that the case of wordforms in analyses
		/// should be determined by the case of the word that was parsed rather than the case of the surface form.
		/// So, the wordform for "The" should be "the" rather than "The" because "The" is parsed as the determiner "the".
		/// </summary>
		/// <param name="wordform"></param>
		/// <returns></returns>
		private bool SuppressableParseResult(IWfiWordform wordform)
		{
			var result = m_checkParserResults[wordform];
			if (result.Analyses.Count > 0)
				return false;
			// See if there is a lowercase version of wordform in the parse results.
			ITsString itsString = wordform.Form.VernacularDefaultWritingSystem;
			if (itsString == null || itsString.Text == null)
				return true;
			var cf = new CaseFunctions(m_cache.ServiceLocator.WritingSystemManager.Get(itsString.get_WritingSystemAt(0)));
			string lcText = cf.ToLower(itsString.Text);
			if (lcText != itsString.Text)
			{
				var lcItsString = TsStringUtils.MakeString(lcText, itsString.get_WritingSystem(0));
				IWfiWordform lcWordform;
				if (m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().TryGetObject(lcItsString, out lcWordform))
				{
					if (m_checkParserResults.ContainsKey(lcWordform))
					{
						var lcResult = m_checkParserResults[lcWordform];
						// See if lcResult covers wordform's approved analyses.
						var userAgent = wordform.Cache.LanguageProject.DefaultUserAgent;
						foreach (IWfiAnalysis wfAnalysis in wordform.AnalysesOC)
						{
							var wfOpinion = wfAnalysis.GetAgentOpinion(userAgent);
							if (wfOpinion == Opinions.approves)
							{
								foreach (ParseAnalysis lcWfAnalysis in lcResult.Analyses)
								{
									if (!lcWfAnalysis.MatchesIWfiAnalysis(wfAnalysis))
									{
										return false;
									}
								}
							}
						}
						// All approved analyses are covered.
						// Suppress the parse results for wordform.
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Show the parser reports in the ParserReports window.
		/// </summary>
		public void ShowParserReports()
		{
			if (m_parserReportsDialog == null)
			{
				ReadParserReports();
				// Create parser reports window.
				m_parserReportsDialog = new ParserReportsDialog(m_parserReports, m_mediator, m_cache, m_defaultComment);
				m_parserReportsDialog.Closed += ParserReportsDialog_Closed;
			}
			m_parserReportsDialog.Show(); // Show the dialog but do not block other app access
			m_parserReportsDialog.BringIntoView();
		}

		private void ParserReportsDialog_Closed(object sender, EventArgs e)
		{
			ParserReportsDialog dialog = (ParserReportsDialog)sender;
			// Preserve the default comment for the next call to ShowParserReports.
			if (dialog != null)
				m_defaultComment = dialog.DefaultComment;
			m_parserReportsDialog = null;
		}

		/// <summary>
		/// Read the parser reports from the disk if they haven't been read.
		/// </summary>
		private void ReadParserReports()
		{
			if (m_parserReports == null)
			{
				m_parserReports = new ObservableCollection<ParserReportViewModel>();
				var reportDir = ParserReport.GetProjectReportsDirectory(m_cache);
				foreach (string filename in Directory.EnumerateFiles(reportDir, "*.json"))
				{
					var parserReport = ParserReport.ReadJsonFile(filename);
					m_parserReports.Add(new ParserReportViewModel { ParserReport = parserReport});
				}
			}
		}

		/// <summary>
		/// Add parserReport to the list of parser reports.
		/// </summary>
		private ParserReportViewModel AddParserReport(ParserReport parserReport)
		{
			ParserReportViewModel viewModel = new ParserReportViewModel { ParserReport = parserReport };
			m_parserReports.Insert(0, viewModel);
			if (m_parserReportsDialog != null)
				// Reset ParserReports so that the window gets notified when the new report is selected.
				((ParserReportsViewModel)m_parserReportsDialog.DataContext).ParserReports = m_parserReports;
			return viewModel;
		}

		/// <summary>
		/// Display a parser report window.
		/// </summary>
		/// <param name="parserReport"></param>
		/// <param name="mediator">the mediator is used to call TryAWord</param>
		public static void ShowParserReport(ParserReportViewModel parserReport, Mediator mediator, LcmCache cache)
		{
			ParserReportDialog dialog = new ParserReportDialog(parserReport, mediator, cache);
			dialog.Show();
		}

		public bool OnParseAllWords(object argument)
		{
			CheckDisposed();
			if (ConnectToParser())
			{
				IEnumerable<IWfiWordform> wordforms = m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances();
				UpdateWordforms(wordforms, ParserPriority.Low);
			}

			return true;	//we handled this.
		}

		private bool InTextsWordsArea
		{
			get
			{
				string areaChoice = m_propertyTable.GetStringProperty("areaChoice", "");
				return areaChoice == "textsWords";
			}
		}

		private bool InWordAnalyses
		{
			get
			{
				string toolName = m_propertyTable.GetStringProperty("currentContentControl", "");
				return InTextsWordsArea && (toolName == "Analyses" || toolName == "wordListConcordance" || toolName == "toolBulkEditWordforms");
			}
		}

		private bool InInterlinearText
		{
			get
			{
				string toolName = m_propertyTable.GetStringProperty("currentContentControl", "");
				string tabName = m_propertyTable.GetStringProperty("InterlinearTab", "");
				return InTextsWordsArea && toolName == "interlinearEdit" && (tabName == "RawText" || tabName == "Interlinearizer" || tabName == "Gloss");
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks> this is something of a hack until we come up with a generic solution to the problem
		/// on how to control we are CommandSet are handled by listeners are visible. It is difficult
		/// because some commands, like this one, may be appropriate from more than 1 area.</remarks>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayParseAllWords(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = m_parserConnection == null;
			return true;	//we handled this.
		}
		public bool OnDisplayReInitParser(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = m_parserConnection != null;
			return true;	//we handled this.
		}
		public bool OnDisplayStopParser(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = m_parserConnection != null;
			return true;	//we handled this.
		}
		public bool OnDisplayReparseAllWords(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			// must wait for the queue to empty before we can fill it up again or else we run the risk of breaking the parser thread
			display.Enabled = m_parserConnection != null && m_parserConnection.GetQueueSize(ParserPriority.Low) == 0;

			return true;	//we handled this.
		}

		public bool OnStopParser(object argument)
		{
			CheckDisposed();

			DisconnectFromParser();
			return true;	//we handled this.
		}

		// used by Try a Word to get the parser running
		public bool OnReInitParser(object argument)
		{
			CheckDisposed();

			if (m_parserConnection == null)
				ConnectToParser();
			else
				m_parserConnection.ReloadGrammarAndLexicon();
			return true; //we handled this.
		}

		public bool OnReparseAllWords(object argument)
		{
			CheckDisposed();
			if (ConnectToParser())
			{
				IEnumerable<IWfiWordform> wordforms = m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances();
				UpdateWordforms(wordforms, ParserPriority.Low);
			}
			return true;	//we handled this.
		}

		public virtual bool OnDisplayChooseParser(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			var cmd = (Command) commandObject;

			display.Checked = m_cache.LangProject.MorphologicalDataOA.ActiveParser == cmd.GetParameter("parser");
			return true; //we've handled this
		}

		public bool OnChooseParser(object argument)
		{
			CheckDisposed();
			var cmd = (Command) argument;

			string newParser = cmd.GetParameter("parser");
			if (m_cache.LangProject.MorphologicalDataOA.ActiveParser != newParser)
			{
				DisconnectFromParser();
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				{
					m_cache.LangProject.MorphologicalDataOA.ActiveParser = newParser;
				});
			}

			return true;
		}

		/// <summary>
		/// Handles the xWorks message for Try This Word
		/// </summary>
		/// <param name="argument">The word to try</param>
		/// <returns></returns>
		public bool OnTryThisWord(object commandObject)
		{
			CheckDisposed();

			var result = TryAWord(commandObject as string);
			// Invoke it immediately.
			m_dialog.TryIt();
			return result;
		}

		/// <summary>
		/// Handles the xWorks message for Try A Word
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>false</returns>
		public bool OnTryAWord(object argument)
		{
			string word = null;
			if (CurrentWordform != null)
				word = CurrentWordform.Form.VernacularDefaultWritingSystem.Text;

			return TryAWord(word);
		}

		public bool TryAWord(string initialWord)
		{
			CheckDisposed();

			if (m_dialog == null || m_dialog.IsDisposed)
			{
				m_dialog = new TryAWordDlg();
				m_dialog.SizeChanged += (sender, e) =>
				{
					if (m_dialog.WindowState != FormWindowState.Minimized)
						m_prevWindowState = m_dialog.WindowState;
				};
				m_dialog.SetDlgInfo(m_mediator, m_propertyTable, initialWord, this);
				var form = m_propertyTable.GetValue<FwXWindow>("window");
				m_dialog.Show(form);
				// This allows Keyman to work correctly on initial typing.
				// Marc Durdin suggested switching to a different window and back.
				// PostMessage gets into the queue after the dialog settles down, so it works.
				Win32.PostMessage(form.Handle, Win32.WinMsgs.WM_SETFOCUS, 0, 0);
				Win32.PostMessage(m_dialog.Handle, Win32.WinMsgs.WM_SETFOCUS, 0, 0);
			}
			else
			{
				if (initialWord != null)
					m_dialog.SetWordToUse(initialWord);
				if (m_dialog.WindowState == FormWindowState.Minimized)
					m_dialog.WindowState = m_prevWindowState;
				else
					m_dialog.Activate();
			}

			return true; // we handled this
		}

		#region TraceSwitch methods

		protected void TraceVerbose(string s)
		{
			if(m_traceSwitch.TraceVerbose)
				Trace.Write(s);
		}
		protected void TraceVerboseLine(string s)
		{
			if(m_traceSwitch.TraceVerbose)
				Trace.WriteLine("PLID="+System.Threading.Thread.CurrentThread.GetHashCode()+": "+s);
		}
		protected void TraceInfoLine(string s)
		{
			if(m_traceSwitch.TraceInfo || m_traceSwitch.TraceVerbose)
				Trace.WriteLine("PLID="+System.Threading.Thread.CurrentThread.GetHashCode()+": "+s);
		}

		#endregion TraceSwitch methods
	}
}
