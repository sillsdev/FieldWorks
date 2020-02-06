// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;
using SIL.Utils;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary>
	/// This class just gets all the parser calling and event and receiving
	/// out of the form code. It is scheduled for refactoring
	/// </summary>
	internal sealed class ParserMenuManager : IFlexComponent, IDisposable, IVwNotifyChange
	{
		//a pointer to the one owned by from the form
		private LcmCache m_cache;
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanaged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the managed section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		/// <summary>
		/// Control how much output we send to the application's listeners (e.g. visual studio output window)
		/// </summary>
		private TraceSwitch m_traceSwitch = new TraceSwitch("ParserMenuManager", string.Empty);
		private TryAWordDlg m_dialog;
		private FormWindowState m_prevWindowState;
		private Timer m_timer;
		private ISharedEventHandlers _sharedEventHandlers;
		private StatusBarPanel _statusPanelProgress;
		private ToolStripMenuItem _parserMenu;
		private IReadOnlyDictionary<Command, ToolStripItem> _parserMenuDictionary;
		private GlobalUiWidgetParameterObject _globalUiWidgetParameterObject;
		private IStText _currentStText;
		private IWfiWordform _currentWordform;
		private IRecordList _recordList;

		/// <summary />
		internal ParserMenuManager(ISharedEventHandlers sharedEventHandlers, StatusBarPanel statusPanelProgress, ToolStripMenuItem parserMenu, IReadOnlyDictionary<Command, ToolStripItem> parserMenuDictionary, GlobalUiWidgetParameterObject globalUiWidgetParameterObject)
		{
			_sharedEventHandlers = sharedEventHandlers;
			_statusPanelProgress = statusPanelProgress;
			_parserMenu = parserMenu;
			_parserMenuDictionary = parserMenuDictionary;
			_globalUiWidgetParameterObject = globalUiWidgetParameterObject;
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

		#region Implentation of IFlexComponent

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

			m_cache = PropertyTable.GetValue<LcmCache>(FwUtils.cache);
			m_sda = m_cache.MainCacheAccessor;
			_parserMenu.DropDownOpening += ParserMenu_DropDownOpening;
			var parserMenuDictionary = _globalUiWidgetParameterObject.GlobalMenuItems[MainMenu.Parser];
			parserMenuDictionary.Add(Command.CmdParseAllWords, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ParseAllWords_Click, () => CanCmdParseAllWords));
			parserMenuDictionary.Add(Command.CmdReparseAllWords, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ReparseAllWords_Click, () => CanCmdReparseAllWords));
			parserMenuDictionary.Add(Command.CmdReInitializeParser, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ReloadGrammarLexicon_Click, () => CanCmdReInitializeParser));
			parserMenuDictionary.Add(Command.CmdStopParser, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(StopParser_Click, () => CanCmdStopParser));
			parserMenuDictionary.Add(Command.CmdTryAWord, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(TryAWord_Click, () => CanCmdTryAWord));
			parserMenuDictionary.Add(Command.CmdParseWordsInCurrentText, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ParseWordsInText_Click, () => CanCmdParseWordsInCurrentText));
			parserMenuDictionary.Add(Command.CmdParseCurrentWord, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ParseCurrentWord_Click, () => CanCmdParseCurrentWord));
			parserMenuDictionary.Add(Command.CmdClearSelectedWordParserAnalyses, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ClearCurrentParserAnalyses_Click, () => CanCmdClearSelectedWordParserAnalyses));
			parserMenuDictionary.Add(Command.CmdChooseXAmpleParser, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ChooseParser_Click, () => UiWidgetServices.CanSeeAndDo));
			parserMenuDictionary.Add(Command.CmdChooseHCParser, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ChooseParser_Click, () => UiWidgetServices.CanSeeAndDo));
			parserMenuDictionary.Add(Command.CmdEditParserParameters, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(EditParserParameters_Click, () => UiWidgetServices.CanSeeAndDo));

			Subscriber.Subscribe(TextAndWordsArea.TextSelectedWord, TextSelectedWord_Handler);
			Subscriber.Subscribe(LanguageExplorerConstants.StopParser, StopParser_Handler);

			UpdateStatusPanelProgress();
		}

		#endregion

		public IRecordList MyRecordList
		{
			set
			{
				if (ReferenceEquals(_recordList, value))
				{
					// Same one, so skip.
					return;
				}
				if (_recordList != null)
				{
					// Unwire from older record list
					_recordList.RecordChanged -= RecordListRecordChanged;
				}
				if (value == null)
				{
					_recordList = null;
					return;
				}
				_recordList = value;
				if (InInterlinearText && value.SubservientRecordList != null)
				{
					_recordList = value.SubservientRecordList;
				}
				// Wire up to new record list.
				_recordList.RecordChanged += RecordListRecordChanged;
			}
		}

		private void TextSelectedWord_Handler(object newValue)
		{
			// newValue will be an IWfiWordform or null;
			_currentWordform = (IWfiWordform)newValue;
		}

		private void StopParser_Handler(object newValue)
		{
			DisconnectFromParser();
		}

		private void RecordListRecordChanged(object sender, RecordNavigationEventArgs e)
		{
			var currentObject = e.RecordNavigationInfo.MyRecordList.CurrentObject;
			var asIStText = currentObject as IStText;
			if (asIStText != null)
			{
				if (!ReferenceEquals(_currentStText, asIStText))
				{
					_currentStText = asIStText;
				}
				return;
			}
			var asIWfiWordform = currentObject as IWfiWordform;
			if (asIWfiWordform != null)
			{
				_currentWordform = asIWfiWordform;
				Connection?.UpdateWordform(_currentWordform, ParserPriority.High);
			}
		}

		private Tuple<bool, bool> CanCmdParseAllWords => new Tuple<bool, bool>(true, Connection == null);

		private Tuple<bool, bool> CanCmdReparseAllWords => new Tuple<bool, bool>(true, Connection != null && Connection.GetQueueSize(ParserPriority.Low) == 0);

		private Tuple<bool, bool> CanCmdReInitializeParser => new Tuple<bool, bool>(true, Connection != null);

		private Tuple<bool, bool> CanCmdStopParser => new Tuple<bool, bool>(true, Connection != null);

		private Tuple<bool, bool> CanCmdTryAWord => new Tuple<bool, bool>(true, CurrentWordform != null);

		private Tuple<bool, bool> CanCmdParseWordsInCurrentText
		{
			get
			{
				var enable = InInterlinearText && _currentStText != null;
				return new Tuple<bool, bool>(enable, enable);
			}
		}

		private Tuple<bool, bool> CanCmdParseCurrentWord
		{
			get
			{
				var enable = CurrentWordform != null;
				return new Tuple<bool, bool>(enable, enable);
			}
		}

		private Tuple<bool, bool> CanCmdClearSelectedWordParserAnalyses
		{
			get
			{
				var enable = CurrentWordform != null;
				return new Tuple<bool, bool>(enable, enable);
			}
		}

		private void ParserMenu_DropDownOpening(object sender, EventArgs e)
		{
			var activeParser = m_cache.LangProject.MorphologicalDataOA.ActiveParser;
			((ToolStripMenuItem)_parserMenuDictionary[Command.CmdChooseXAmpleParser]).Checked = activeParser == "XAmple";
			((ToolStripMenuItem)_parserMenuDictionary[Command.CmdChooseHCParser]).Checked = activeParser == "HC";
		}

		internal ParserConnection Connection { get; set; }

		#region IVwNotifyChange Members

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			// If someone updated the wordform inventory with a real wordform, schedule it to be parsed.
			if (Connection != null && tag == WfiWordformTags.kflidForm)
			{
				// the form of this WfiWordform was changed, so update its parse info.
				Connection.UpdateWordform(m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().GetObject(hvo), ParserPriority.High);
			}
		}

		#endregion

		#region Timer Related

		private const int TIMER_INTERVAL = 250; // every 1/4 second

		private void StartProgressUpdateTimer()
		{
			if (m_timer == null)
			{
				m_timer = new Timer
				{
					Interval = TIMER_INTERVAL
				};
				m_timer.Tick += m_timer_Tick;
			}
			m_timer.Start();
		}

		private void StopUpdateProgressTimer()
		{
			m_timer?.Stop();
		}

		public void m_timer_Tick(object sender, EventArgs eventArgs)
		{
			UpdateStatusPanelProgress();
		}

		#endregion

		public bool ConnectToParser()
		{
			if (Connection == null)
			{
				// Don't bother if the lexicon is empty.  See FWNX-1019.
				if (m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().Count == 0)
				{
					return false;
				}
				var window = PropertyTable.GetValue<IIdleQueueProvider>(FwUtils.window);
				Connection = new ParserConnection(m_cache, window.IdleQueue);
			}
			m_sda?.AddNotification(this);
			StartProgressUpdateTimer();
			return true;
		}

		public void DisconnectFromParser()
		{
			StopUpdateProgressTimer();
			if (Connection != null)
			{
				m_sda?.RemoveNotification(this);
				Connection.Dispose();
				Connection = null;
			}
		}

		public bool OnIdle(object argument)
		{
			UpdateStatusPanelProgress();

			return false; // Don't stop other people from getting the idle message
		}

		// Now called by timer AND by OnIdle
		private void UpdateStatusPanelProgress()
		{
			_statusPanelProgress.Text = ParserQueueString + " " + ParserActivityString;

			if (Connection != null)
			{
				var ex = Connection.UnhandledException;
				if (ex != null)
				{
					DisconnectFromParser();
					var app = PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App);
					ErrorReporter.ReportException(ex, app.SettingsKey, app.SupportEmailAddress, app.ActiveMainWindow, false);
				}
				else
				{
					var notification = Connection.GetAndClearNotification();
					if (notification != null)
					{
						using (var nw = new NotifyWindow(notification))
						{
							nw.SetDimensions(150, 150);
							nw.WaitTime = 4000;
							nw.Notify();
						}
					}
				}
			}
			if (ParserActivityString == ParserUIStrings.ksIdle_ && m_timer.Enabled)
			{
				StopUpdateProgressTimer();
			}
		}

		//note that the Parser also supports an event oriented system
		//so that we are notified for every single event that happens.
		//Here, we have instead chosen to use the polling ability.
		//We will thus missed some events but not get slowed down with too many.
		public string ParserActivityString => Connection == null ? ParserUIStrings.ksNoParserLoaded : Connection.Activity;

		/// <summary />
		public string ParserQueueString
		{
			get
			{
				var low = ParserUIStrings.ksDash;
				var med = ParserUIStrings.ksDash;
				var high = ParserUIStrings.ksDash;
				if (Connection != null)
				{
					low = Connection.GetQueueSize(ParserPriority.Low).ToString();
					med = Connection.GetQueueSize(ParserPriority.Medium).ToString();
					high = Connection.GetQueueSize(ParserPriority.High).ToString();
				}
				return string.Format(ParserUIStrings.ksQueueXYZ, low, med, high);
			}
		}

		#region IDisposable & Co. implementation

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		private bool IsDisposed { get; set; }

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~ParserMenuManager()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SuppressFinalize to
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
		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				if (_parserMenu != null)
				{
					_parserMenu.DropDownOpening -= ParserMenu_DropDownOpening;
				}
				if (_recordList != null)
				{
					_recordList.RecordChanged -= RecordListRecordChanged;
				}
				Subscriber.Unsubscribe(TextAndWordsArea.TextSelectedWord, TextSelectedWord_Handler);
				Subscriber.Unsubscribe(LanguageExplorerConstants.StopParser, StopParser_Handler);

				if (m_timer != null)
				{
					m_timer.Stop();
					m_timer.Tick -= m_timer_Tick;
				}
				if (m_sda != null)
				{
					m_sda.RemoveNotification(this);
					m_sda = null;
				}
				Connection?.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_sda?.RemoveNotification(this); // See note about doing this in unmanaged section. It is now attempted in both sections to ensure it gets done.
			m_sda = null;
			m_timer = null;
			m_cache = null;
			m_traceSwitch = null;
			Connection = null;
			_sharedEventHandlers = null;
			_statusPanelProgress = null;
			_parserMenu = null;
			_parserMenuDictionary = null;
			_globalUiWidgetParameterObject = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;
			_recordList = null;

			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		private IWfiWordform CurrentWordform
		{
			get
			{
				if (!InInterlinearText && !InWordAnalyses)
				{
					_currentWordform = null;
				}
				return _currentWordform; // Will be null, if not in a friendly space.
			}
		}

		private void EditParserParameters_Click(object sender, EventArgs e)
		{
			using (var dlg = new ParserParametersDlg(PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider)))
			{
				var md = m_cache.LangProject.MorphologicalDataOA;
				dlg.SetDlgInfo(ParserUIStrings.ksParserParameters, md.ParserParameters);
				if (dlg.ShowDialog(PropertyTable.GetValue<Form>(FwUtils.window)) != DialogResult.OK)
				{
					return;
				}
				using (var helper = new UndoableUnitOfWorkHelper(m_cache.ActionHandlerAccessor, ParserUIStrings.ksUndoEditingParserParameters, ParserUIStrings.ksRedoEditingParserParameters))
				{
					md.ParserParameters = dlg.XmlRep;
					helper.RollBack = false;
				}
			}
		}

		private void ClearCurrentParserAnalyses_Click(object sender, EventArgs e)
		{
			var wf = CurrentWordform;
			if (wf == null)
			{
				return;
			}
			UndoableUnitOfWorkHelper.Do(ParserUIStrings.ksUndoClearParserAnalyses, ParserUIStrings.ksRedoClearParserAnalyses, m_cache.ActionHandlerAccessor, () =>
			{
				foreach (var analysis in wf.AnalysesOC.ToArray())
				{
					var parserEvals = analysis.EvaluationsRC.Where(evaluation => !evaluation.Human).ToArray();
					foreach (var parserEval in parserEvals)
					{
						analysis.EvaluationsRC.Remove(parserEval);
					}
					if (analysis.EvaluationsRC.Count == 0)
					{
						wf.AnalysesOC.Remove(analysis);
					}
					wf.Checksum = 0;
				}
			});
		}

		private void ParseCurrentWord_Click(object sender, EventArgs e)
		{
			if (!ConnectToParser())
			{
				return;
			}
			Connection.UpdateWordform(CurrentWordform, ParserPriority.High);
		}

		private void ParseWordsInText_Click(object sender, EventArgs e)
		{
			if (!ConnectToParser())
			{
				return;
			}
			Connection.UpdateWordforms(_currentStText.UniqueWordforms(), ParserPriority.Medium);
		}

		private void ParseAllWords_Click(object sender, EventArgs e)
		{
			if (ConnectToParser())
			{
				Connection.UpdateWordforms(m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances(), ParserPriority.Low);
			}
		}

		private bool InWordAnalyses
		{
			get
			{
				var acceptableToolNames = new HashSet<string> { AreaServices.AnalysesMachineName, AreaServices.WordListConcordanceMachineName, AreaServices.BulkEditWordformsMachineName };
				return acceptableToolNames.Contains(PropertyTable.GetValue<string>(AreaServices.ToolChoice, string.Empty));
			}
		}

		private bool InInterlinearText
		{
			get
			{
				var acceptableTabNames = new HashSet<string> { "RawText", "Interlinearizer", "Gloss" };
				return PropertyTable.GetValue<string>(AreaServices.ToolChoice) == AreaServices.InterlinearEditMachineName
					   && acceptableTabNames.Contains(PropertyTable.GetValue(TextAndWordsArea.InterlinearTab, string.Empty));
			}
		}

		private void StopParser_Click(object sender, EventArgs e)
		{
			DisconnectFromParser();
		}

		private void ReloadGrammarLexicon_Click(object sender, EventArgs e)
		{
			if (Connection == null)
			{
				ConnectToParser();
			}
			else
			{
				Connection.ReloadGrammarAndLexicon();
			}
		}

		private void ReparseAllWords_Click(object sender, EventArgs e)
		{
			if (ConnectToParser())
			{
				Connection.UpdateWordforms(m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances(), ParserPriority.Low);
			}
		}

		private void ChooseParser_Click(object sender, EventArgs e)
		{
			var chooserMenuItem = (ToolStripMenuItem)sender;
			var newParser = chooserMenuItem.Name.Contains("XAmple") ? "XAmple" : "HC";
			if (m_cache.LangProject.MorphologicalDataOA.ActiveParser == newParser)
			{
				return;
			}
			DisconnectFromParser();
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				m_cache.LangProject.MorphologicalDataOA.ActiveParser = newParser;
			});
		}

		private void TryAWord_Click(object sender, EventArgs e)
		{
			if (m_dialog == null || m_dialog.IsDisposed)
			{
				m_dialog = new TryAWordDlg();
				m_dialog.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				m_dialog.SizeChanged += (sender1, e1) =>
				{
					if (m_dialog.WindowState != FormWindowState.Minimized)
					{
						m_prevWindowState = m_dialog.WindowState;
					}
				};
				m_dialog.SetDlgInfo(_sharedEventHandlers, CurrentWordform, this);
				var form = PropertyTable.GetValue<Form>(FwUtils.window);
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
				{
					m_dialog.WindowState = m_prevWindowState;
				}
				else
				{
					m_dialog.Activate();
				}
			}
		}

		#region TraceSwitch methods

		private void TraceVerbose(string s)
		{
			if (m_traceSwitch.TraceVerbose)
			{
				Trace.Write(s);
			}
		}
		private void TraceVerboseLine(string s)
		{
			if (m_traceSwitch.TraceVerbose)
			{
				Trace.WriteLine("PLID=" + System.Threading.Thread.CurrentThread.GetHashCode() + ": " + s);
			}
		}
		private void TraceInfoLine(string s)
		{
			if (m_traceSwitch.TraceInfo || m_traceSwitch.TraceVerbose)
			{
				Trace.WriteLine("PLID=" + System.Threading.Thread.CurrentThread.GetHashCode() + ": " + s);
			}
		}

		#endregion TraceSwitch methods
	}
}