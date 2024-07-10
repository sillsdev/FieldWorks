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
using System.Diagnostics;
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
		private Dictionary<IWfiWordform, bool> m_wordformProcessed = null;

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
					m_parserConnection.UpdateWordform(wordform, ParserPriority.High);
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
				m_parserConnection.UpdateWordform(m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().GetObject(hvo), ParserPriority.High);
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

			return true;	//we handled this.
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
			return true;	//we handled this.
		}

		#endregion ClearSelectedWordParserAnalyses handlers

		public bool OnDisplayParseCurrentWord(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			bool enable = CurrentWordform != null;
			display.Visible = enable;
			display.Enabled = enable;

			return true;	//we handled this.
		}

		public bool OnParseCurrentWord(object argument)
		{
			CheckDisposed();

			if (ConnectToParser())
			{
				IWfiWordform wf = CurrentWordform;
				m_parserConnection.UpdateWordform(wf, ParserPriority.High);
			}

			return true;	//we handled this.
		}

		public bool OnDisplayParseWordsInCurrentText(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			bool enable = CurrentText != null;
			display.Visible = enable;
			display.Enabled = enable;

			return true;	//we handled this.
		}

		public bool OnParseWordsInCurrentText(object argument)
		{
			CheckDisposed();

			if (CurrentText != null && ConnectToParser())
			{
				IStText text = CurrentText;
				IEnumerable<IWfiWordform> wordforms = text.UniqueWordforms();
				m_parserConnection.UpdateWordforms(wordforms, ParserPriority.Medium);
			}

			return true;    //we handled this.
		}

		public bool OnCheckParserOnCurrentText(object argument)
		{
			CheckDisposed();

			if (CurrentText != null && ConnectToParser())
			{
				IStText text = CurrentText;
				IEnumerable<IWfiWordform> wordforms = text.UniqueWordforms();
				InitWordformProcessed(wordforms);
				m_parserConnection.UpdateWordforms(wordforms, ParserPriority.Medium);
			}

			return true;    //we handled this.
		}

		private void InitWordformProcessed(IEnumerable<IWfiWordform> wordforms)
		{
			if (wordforms == null)
			{
				m_wordformProcessed = null;
			}
			else
			{
				m_wordformProcessed = new Dictionary<IWfiWordform, bool>();
				foreach (var wordform in wordforms)
				{
					m_wordformProcessed[wordform] = false;
				}

			}
		}

		private void WordformUpdatedEventHandler(object sender, WordformUpdatedEventArgs e)
		{
			if (m_wordformProcessed != null && m_wordformProcessed.ContainsKey(e.Wordform))
			{
				m_wordformProcessed[e.Wordform] = true;
				// See whether all of the wordforms have been processed.
				foreach (var key in m_wordformProcessed.Keys)
				{
					if (!m_wordformProcessed[key])
						return;
				}
				// Show the conflicts.
				FwLinkArgs link = new FwAppArgs(m_cache.ProjectId.Handle, "Analyses", Guid.Empty);
				m_mediator.PostMessage("FollowLink", link);

			}
		}

		public bool OnParseAllWords(object argument)
		{
			CheckDisposed();
			if (ConnectToParser())
				m_parserConnection.UpdateWordforms(m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances(), ParserPriority.Low);

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
				m_parserConnection.UpdateWordforms(m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances(), ParserPriority.Low);
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
		/// Handles the xWorks message for Try A Word
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>false</returns>
		public bool OnTryAWord(object argument)
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
				m_dialog.SetDlgInfo(m_mediator, m_propertyTable, CurrentWordform, this);
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
