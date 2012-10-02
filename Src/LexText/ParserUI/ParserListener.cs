// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ParserListener.cs
// Responsibility: John Hatton
// Last reviewed:
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
// --------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FdoUi;
using XCore;
using SIL.Utils;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// this class just gets all the parser calling and event and receiving
	/// out of the form code. It is scheduled for refactoring
	/// </summary>
	[MediatorDispose]
	public class ParserListener : IxCoreColleague, IFWDisposable, IVwNotifyChange
	{
		protected Mediator m_mediator;
		protected FdoCache m_cache; //a pointer to the one owned by from the form
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
		protected TraceSwitch m_traceSwitch = new TraceSwitch("ParserListener", "");

		/// <summary>
		/// Set when we are running the parser; must be freed when we no longer are.
		/// </summary>
		private IDisposable m_lock;

		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();

			m_mediator = mediator;
			m_cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			mediator.AddColleague(this);
			mediator.PropertyTable.SetProperty("ParserListener", this);
			mediator.PropertyTable.SetPropertyPersistence("ParserListener", false);

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


		private ParserConnection Connection
		{
			get
			{
				CheckDisposed();
				return (ParserConnection)m_mediator.PropertyTable.GetValue("ParserConnection");
			}
			set
			{
				CheckDisposed();
				m_mediator.PropertyTable.SetProperty("ParserConnection", value);
				m_mediator.PropertyTable.SetPropertyPersistence("ParserConnection", false);
			}
		}

		/// <summary>
		/// Send the newly selected wordform on to the parser.
		/// </summary>
		public void OnPropertyChanged(string propertyName)
		{
			CheckDisposed();

			if (propertyName == "ActiveClerkSelectedObject")
			{
				var wordform = m_mediator.PropertyTable.GetValue(propertyName) as IWfiWordform;
				if (wordform != null)
					UpdateWordformAsap(wordform);
			}
		}

		#region IVwNotifyChange Members

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			// no need to parse if we don't have a connection.
			if (Connection == null)
				return;
			// If someone updated the wordform inventory with a real wordform, schedule it to be parsed.
			if (tag == WfiWordformTags.kflidForm)
			{
				// the form of this WfiWordform was changed, so update its parse info.
				UpdateWordformAsap(m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().GetObject(hvo));
			}
			// Note: typically when adding a new WfiWordform to WordformInventory, it will not
			// have it's form set. So, there's no point in checking for that case.
			// It's sufficient to check for change in Form.
			//else if (tag == (int)WordformInventory.WordformInventoryTags.kflidWordforms &&
			//    cvIns == 1 && cvDel == 0)
			//{
			//    // find the item with the highest id.
			//    int[] hvosWf = m_cache.LangProject.WordformInventoryOA.WordformsOC.HvoArray;
			//    List<int> hvosSorted = new List<int>(hvosWf);
			//    hvosSorted.Sort();
			//    int hvoWfNew = hvosSorted[hvosSorted.Count - 1];
			//    // Schedule this one to be parsed.
			//    WfiWordform wf = WfiWordform.CreateFromDBObject(m_cache, hvoWfNew) as WfiWordform;
			//    // no need to update
			//    UpdateWordformAsap(wf);
			//}
		}

		#endregion

		public void ConnectToParser()
		{
			CheckDisposed();

			if (Connection == null)
			{
				Connection = new ParserConnection(m_cache, m_mediator.IdleQueue);
				m_mediator.PropertyTable.SetProperty("ParserConnection", Connection);
			}
		}

		public void DisconnectFromParser()
		{
			CheckDisposed();

			if (Connection != null)
			{
				Connection.Dispose();
			}
			Connection = null;
		}

		/// <summary>
		/// Put the wordform in the highest priority queue of the Parser
		/// </summary>
		/// <param name="wf"></param>
		public void UpdateWordformAsap(IWfiWordform wf)
		{
			CheckDisposed();

			ParserConnection con = Connection;
			if (con != null && wf.Form.VernacularDefaultWritingSystem != null)
				con.UpdateWordform(wf, ParserPriority.High);
		}

		/// <summary>
		/// Put all (unique) wordforms of the text in the medium priority queue of the Parser
		/// </summary>
		/// <param name="text"></param>
		public void UpdateWordformsInText(IStText text)
		{
			CheckDisposed();

			var wordforms = text.UniqueWordforms();

			ParserConnection con = Connection;
			if (con != null)
				con.UpdateWordforms(wordforms, ParserPriority.Medium);
		}

		/// <summary>
		/// put the wordform in the medium priority queue of the Parser
		/// </summary>
		/// <param name="wordform">The wordform.</param>
		public void UpdateWordformSoon(IWfiWordform wordform)
		{
			CheckDisposed();

			Connection.UpdateWordform(wordform, ParserPriority.Medium);
		}

		public void Reload()
		{
			CheckDisposed();

			Debug.Fail("Not implemented. Hit 'ignore' now.");
			//GetParserConnection().Prepare(m_iParserClientNum, 0, 2/*reload flag */);
		}

		public bool OnIdle(object argument)
		{
			CheckDisposed();

			m_mediator.PropertyTable.SetProperty("StatusPanelProgress", GetParserQueueString() + " " + GetParserActivityString());
			m_mediator.PropertyTable.SetPropertyPersistence("StatusPanelProgress", false);

			ParserConnection con = Connection;
			if (con != null)
			{
				Exception ex = con.UnhandledException;
				if (ex != null)
				{
					DisconnectFromParser();
					var app = (IApp) m_mediator.PropertyTable.GetValue("App");
					ErrorReporter.ReportException(ex, app.SettingsKey, app.SupportEmailAddress, app.ActiveMainWindow, false);
				}
				else
				{
					string notification = con.GetAndClearNotification();
					if (notification != null)
						m_mediator.SendMessage("ShowNotification", notification);
				}
			}

			return false; // Don't stop other people from getting the idle message
		}

		//note that the Parser also supports an event oriented system
		//so that we are notified for every single event that happens.
		//Here, we have instead chosen to use the polling ability.
		//We will thus missed some events but not get slowed down with too many.
		public string GetParserActivityString()
		{
			CheckDisposed();

			return Connection == null ? ParserUIStrings.ksNoParserLoaded : Connection.Activity;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public string GetParserQueueString()
		{
			CheckDisposed();

			string low = ParserUIStrings.ksDash;
			string med = ParserUIStrings.ksDash;
			string high = ParserUIStrings.ksDash;
			if (Connection != null)
			{
				low = Connection.GetQueueSize(ParserPriority.Low).ToString();
				med = Connection.GetQueueSize(ParserPriority.Medium).ToString();
				high = Connection.GetQueueSize(ParserPriority.High).ToString();
			}

			return string.Format(ParserUIStrings.ksQueueXYZ, low, med, high);
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

		private const string ksParserLockName = "parser";

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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				Unlock();
				// other clients may now parse
				// Dispose managed resources here.
				if (m_sda != null)
					m_sda.RemoveNotification(this);
				m_mediator.RemoveColleague(this);
				ParserConnection cnx = Connection;
				if (cnx != null)
				{
					// Remove ParserConnection from the PropertyTable.
					m_mediator.PropertyTable.SetProperty("ParserConnection", null, false);
					m_mediator.PropertyTable.SetPropertyPersistence("ParserConnection", false);
					m_mediator.PropertyTable.SetProperty("ParserListener", null, false);
					m_mediator.PropertyTable.SetPropertyPersistence("ParserListener", false);
					cnx.Dispose();
				}
				if (m_lock != null)
					m_lock.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_sda = null;
			m_mediator = null;
			m_cache = null;
			m_traceSwitch = null;
			m_lock = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// enable/disable and check items in the Parser menu
		/// </summary>
		internal void menuParser_Popup(object sender, EventArgs e)
		{
			CheckDisposed();

			const int kIndexOfStartItem = 6;		//the only one that is enabled if we are disconnected
			bool connected = (null != Connection);

			//first, set them all to match whether we are enabled or not
			foreach (MenuItem item in ((MenuItem)sender).MenuItems)
			{
				item.Enabled = connected;
			}
			//next, switch the "start" item to be the opposite
			((MenuItem)sender).MenuItems[kIndexOfStartItem].Enabled = !connected;
		}

		private IStText CurrentText
		{
			get
			{
				Object obj = m_mediator.PropertyTable.GetValue("ActiveClerkSelectedObject");
				if (obj != null)
					return obj as IStText;
#if TryInWordsConcordance
				// Andy Black: Not sure if this is always the case or if it is worth it...
				{
					StText text = obj as StText;
					if (text != null)
						return text;
					// Not in interlinear, so must be in words concordance;  try to get the text
					obj = m_mediator.PropertyTable.GetValue("OccurrencesOfSelectedWordform-selected");
					if (obj != null)
					{
						RecordNavigationInfo info = obj as RecordNavigationInfo;
						if (info != null)
						{
							CmBaseAnnotation anno = info.Clerk.CurrentObject as CmBaseAnnotation;
							if (anno != null)
							{
								StTxtPara para = anno.BeginObjectRA as StTxtPara;
								if (para != null)
								{
									text = para.Owner as StText;
									return text;
								}
							}
						}
					}
				}
#endif
				return null;
			}
		}
		private IWfiWordform CurrentWordform
		{
			get
			{
				object obj = m_mediator.PropertyTable.GetValue("ActiveClerkSelectedObject");
				return obj != null ? obj as IWfiWordform : null;
			}
		}

		#region ClearSelectedWordParserAnalyses handlers

		public bool OnDisplayClearSelectedWordParserAnalyses(object commandObject, ref UIItemDisplayProperties display)
		{
			bool enable = InWordsAnalyses && CurrentWordform != null;
			display.Visible = enable;
			display.Enabled = enable;
			return true;	//we handled this.
		}

		public bool OnClearSelectedWordParserAnalyses(object dummyObj)
		{
			var wf = CurrentWordform;
			if (wf == null)
			{
				MessageBox.Show(ParserUIStrings.ksSelectWordFirst);
				return true;
			}
			if (wf.Hvo > 0 && wf.HasWordform)
			{
				var analyses = wf.AnalysesOC.ToArray();
				var canalyses = analyses.Length;
				UndoableUnitOfWorkHelper.Do(ParserUIStrings.ksUndoClearParserAnalyses,
					ParserUIStrings.ksRedoClearParserAnalyses, m_cache.ActionHandlerAccessor, () =>
				{
					for (var i = 0; i < canalyses; i++)
					{
						var curAnalysis = analyses[i];
						if (DoesAnyParserHaveAnOpinionOnAnalysis(curAnalysis))
							curAnalysis.Delete();
					}
				});
			}
			return true;	//we handled this.
		}

		private static bool DoesAnyParserHaveAnOpinionOnAnalysis(IWfiAnalysis curAnalysis)
		{
			return curAnalysis.EvaluationsRC.Any(eval => eval.Approves && !eval.Human);
		}

		#endregion ClearSelectedWordParserAnalyses handlers

		public bool OnDisplayParseCurrentWord(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			// JohnT: changed this to check CurrentWordformHvo to fix LT-1184.
			// Seems in IText Document view, no wordform gets recorded in the property table,
			// even when the user has selected text that looks like a wordform.
			// Susanna thinks the command SHOULD be disabled in this view, so enhancing it to
			// note a wordform when the user clicks doesn't work.
			// This change of course defeats the attempt to have choosing the command tell the user
			// to select a word. But that message is merely confusing in this context.
			//display.Enabled = m_mediator.PropertyTable.GetValue("ParserConnection") != null
			//	&& CurrentWordformHvo > 0;

			// CurtisH: As per LT-3087 and Susanna's suggestion, for now we're just going to disable
			// the menu item outside of the Words area.  In the future, it would be nice to fix this
			// so it can parse whatever word is selected in any area as suggested in LT-1184.

			string sToolName = m_mediator.PropertyTable.GetStringProperty("currentContentControl", "");
			display.Enabled = (InTextsWordsArea &&
							   ((sToolName == "Analyses") ||
								(sToolName == "wordListConcordance") ||
								(sToolName == "toolBulkEditWordforms")));

			return true;	//we handled this.
		}

		public bool OnParseCurrentWord(object argument)
		{
			CheckDisposed();

			IWfiWordform wf = CurrentWordform;
			if (wf == null)
			{
				MessageBox.Show(ParserUIStrings.ksSelectWordFirst);
			}
			else
			{
				ConnectToParser();
				UpdateWordformAsap(wf);
			}

			return true;	//we handled this.
		}

		public bool OnDisplayParseWordsInCurrentText(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			string sToolName = m_mediator.PropertyTable.GetStringProperty("currentContentControl", "");
			string sTabName = m_mediator.PropertyTable.GetStringProperty("InterlinearTab", "");
			display.Enabled = (InTextsWordsArea &&
							   ((sToolName == "interlinearEdit") &&
							   ((sTabName == "Interlinearizer") || (sTabName == "RawText"))));

			return true;	//we handled this.
		}

		public bool OnParseWordsInCurrentText(object argument)
		{
			CheckDisposed();

			IStText text = CurrentText;
			if (text != null)
			{
				ConnectToParser();
				UpdateWordformsInText(text);
			}
			return true;	//we handled this.
		}
		public bool OnParseAllWords(object argument)
		{
			CheckDisposed();
			if (!GetLock())
				return true; // we still dealt with the command.

			ConnectToParser();
			Connection.UpdateWordforms(m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances(), ParserPriority.Low);

			return true;	//we handled this.
		}

		private bool GetLock()
		{
			if (m_lock != null)
				return true; // we already have it locked.
			m_lock = ClientServerServices.Current.GetExclusiveModeToken(m_cache, ksParserLockName);
			if (m_lock == null)
			{
				MessageBox.Show(Form.ActiveForm, ParserUIStrings.ksOtherClientIsParsing, ParserUIStrings.ksParsingElsewhere,
								MessageBoxButtons.OK);
				return false;
			}
			return true;
		}

		private void Unlock()
		{
			if (m_lock == null)
				return; // nothing to do.
			m_lock.Dispose();
			m_lock = null;
		}

		protected bool InFriendlyArea
		{
			get
			{
				string areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
				return (areaChoice == "textsWords");
			}
		}

		protected bool InTextsWordsArea
		{
			get
			{
				string areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", "");
				if (areaChoice.ToLower() == "textswords")
					return true;
				return false;
			}
		}
		protected bool InWordsAnalyses
		{
			get
			{
				if (InTextsWordsArea)
				{
					string toolName = m_mediator.PropertyTable.GetStringProperty("currentContentControl","");
					if (toolName.ToLower() == "analyses")
						return true;
				}
				return false;
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

			display.Enabled = (Connection == null);
			return true;	//we handled this.
		}
		public bool OnDisplayReInitParser(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = (Connection != null);
			return true;	//we handled this.
		}
		public bool OnDisplayStopParser(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = (Connection != null);
			return true;	//we handled this.
		}
		public bool OnDisplayReparseAllWords(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			ParserConnection con = Connection;
			// must wait for the queue to empty before we can fill it up again or else we run the risk of breaking the parser thread
			display.Enabled = con != null && con.GetQueueSize(ParserPriority.Low) == 0;

			return true;	//we handled this.
		}

		public bool OnStopParser(object argument)
		{
			CheckDisposed();

			if (Connection != null)
				DisconnectFromParser();
			Unlock();
			return true;	//we handled this.
		}

		// used by Try a Word to get the parser running
		public bool OnReInitParser(object argument)
		{
			CheckDisposed();

			if (Connection == null)
				ConnectToParser();
			else
				Connection.ReloadGrammarAndLexicon();
			return true; //we handled this.
		}

		public bool OnReparseAllWords(object argument)
		{
			CheckDisposed();
			if (!GetLock())
				return true; // we still dealt with the command.

			ConnectToParser();
			Connection.UpdateWordforms(
				m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances(),
				ParserPriority.Low);
			return true;	//we handled this.
		}

		public virtual bool OnDisplayChooseParser(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			var cmd = (Command) commandObject;

			display.Checked = m_cache.LangProject.MorphologicalDataOA.ActiveParser ==
				cmd.GetParameter("parser");
			return true; //we've handled this
		}

		public bool OnChooseParser(object argument)
		{
			CheckDisposed();
			var cmd = (Command) argument;

			string newParser = cmd.GetParameter("parser");
			if (m_cache.LangProject.MorphologicalDataOA.ActiveParser != newParser)
			{
				if (Connection != null)
					DisconnectFromParser();
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				{
					m_cache.LangProject.MorphologicalDataOA.ActiveParser = newParser;
				});
			}

			return true;
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
