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
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Diagnostics;
using System.Configuration;
using System.Xml;
using System.Runtime.InteropServices; // needed for Marshal

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks;
using SIL.FieldWorks.XWorks;
using XCore;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.WordWorks.Parser;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// this class just gets all the parser calling and event and receiving
	/// out of the form code. It is scheduled for refactoring
	/// </summary>
	[XCore.MediatorDispose]
	public class ParserListener : IxCoreColleague, IFWDisposable, IVwNotifyChange
	{
		protected XCore.Mediator m_mediator;
		private int m_maxID;
		private int m_maxIDAnal;
		private bool m_busy = false;
		protected FdoCache m_cache; //a pointer to the one owned by from the form
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		System.Windows.Forms.Timer m_updateTimer;		// timer needed to have the UI thread process the event
		/// <summary>
		/// Control how much output we send to the application's listeners (e.g. visual studio output window)
		/// </summary>
		protected TraceSwitch m_traceSwitch = new TraceSwitch("ParserListener", "");

		/// <summary>
		/// constructor
		/// </summary>
		/// <remarks> this must be followed by a call to ConnectToParser ()</remarks>
		public ParserListener()
		{
			m_updateTimer = new System.Windows.Forms.Timer();
			m_updateTimer.Interval = 10000;
			m_updateTimer.Tick += new EventHandler(m_updateTimer_Elapsed);
		}

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
				CmObject obj = (CmObject)m_mediator.PropertyTable.GetValue(propertyName);
				if (obj is WfiWordform)
					UpdateWordformAsap(obj as WfiWordform);
			}
		}

		#region IVwNotifyChange Members

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			// no need to parse if we don't have a connection.
			if (this.Connection == null)
				return;
			// If someone updated the wordform inventory with a real wordform, schedule it to be parsed.
			if (tag == (int)WfiWordform.WfiWordformTags.kflidForm)
			{
				// the form of this WfiWordform was changed, so update its parse info.
				UpdateWordformAsap(WfiWordform.CreateFromDBObject(m_cache, hvo) as WfiWordform);
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

		public void ConnectToParser(bool fParseAllWordforms)
		{
			CheckDisposed();

			if(Connection == null)
			{
				Connection = new ParserConnection(m_cache.ServerName, m_cache.DatabaseName,
					m_cache.LangProject.Name.AnalysisDefaultWritingSystem, fParseAllWordforms);
				m_mediator.PropertyTable.SetProperty("ParserConnection", Connection);
			}
		}

		public void DisconnectFromParser()
		{
			CheckDisposed();

			if (Connection!=null)
			{
				Connection.Dispose();
			}
			Connection = null;
		}

		/// <summary>
		/// Put the wordform in the highest priority queue of the Parser
		/// </summary>
		/// <param name="wf"></param>
		public void UpdateWordformAsap(WfiWordform wf)
		{
			CheckDisposed();

			ParserConnection con = Connection;
			if (con != null && con.Parser != null &&
				wf.Form.VernacularDefaultWritingSystem != null)
			{
				con.Parser.ScheduleOneWordformForUpdate(wf.Hvo, WordWorks.Parser.ParserScheduler.Priority.ASAP);
			}
		}

		/// <summary>
		/// Put all (unique) wordforms of the text in the medium priority queue of the Parser
		/// </summary>
		/// <param name="text"></param>
		public void UpdateWordformsInText(StText text)
		{
			CheckDisposed();

			int[] aiWordformHvos = text.UniqueWordforms();

			ParserConnection con = Connection;
			if (con != null)
			{
				ParserScheduler parser = con.Parser;
				if (parser != null)
				{
					parser.LoadGrammarAndLexiconIfNeeded();
					parser.ScheduleWordformsForUpdate(aiWordformHvos, WordWorks.Parser.ParserScheduler.Priority.soon);
				}
			}
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="sWordForm"></param>
		/// <param name="fDoTrace"></param>
		public void TryAWordAsynchronously(string sWordForm, bool fDoTrace)
		{
			CheckDisposed();

			this.Connection.TryAWordAsynchronously(sWordForm,fDoTrace);
		}

		/// <summary>
		/// put the wordform in the medium priority queue of the Parser
		/// </summary>
		/// <param name="hvo"></param>
		public void UpdateWordformSoon(int hvo)
		{
			CheckDisposed();

			this.Connection.Parser.ScheduleOneWordformForUpdate(hvo, WordWorks.Parser.ParserScheduler.Priority.soon);
		}

		private string FormOfWord(int hvo)
		{
			try
			{
				WfiWordform word = new WfiWordform (m_cache, hvo);
				return word.Form.VernacularDefaultWritingSystem;
			}
			catch (Exception)
			{
				Debug.Assert(false, "The word returned in msg from parser apparently doesn't exist in db");
				return ParserUIStrings.ksUnknown;
			}
		}

		protected void LogError(string s)
		{
			throw new ApplicationException("s");//todo
		}

		public void Reload()
		{
			CheckDisposed();

			Debug.Fail("Not implemented. Hit 'ignore' now.");
			//GetParserConnection().Prepare(m_iParserClientNum, 0, 2/*reload flag */);
		}

		private List<int> SimpleEdits
		{
			get
			{
				int currentWordformHvo = CurrentWordformHvo;
				if (currentWordformHvo == 0)
					return null;

				int flid = (int)WfiWordform.WfiWordformTags.kflidAnalyses;
				string sqlQuery = String.Format("SELECT ID "
					+ "FROM Sync$ "
					+ "WHERE LpInfoID = '{0}' AND ID > {1} AND ObjFlid = {2} AND ObjId = {3} AND Msg = {4} "
					+ "ORDER BY ID",
					ParserScheduler.AppGuid, m_maxID, flid.ToString(), currentWordformHvo.ToString(), (int)SyncMsg.ksyncSimpleEdit);
				return DbOps.ReadIntsFromCommand(m_cache, sqlQuery, null);
			}
		}

		private List<int[]> FullRefreshes
		{
			get
			{
				int currentWordformHvo = CurrentWordformHvo;
				if (currentWordformHvo == 0)
					return null;

				int flid = (int)WfiWordform.WfiWordformTags.kflidAnalyses;
				string sqlQuery = String.Format("SELECT ID, ObjID "
					+ "FROM Sync$ "
					+ "WHERE LpInfoID = '{0}' AND ID > {1} AND ObjFlid = {2} AND Msg = {3} "
					+ "ORDER BY ID",
					ParserScheduler.AppGuid, m_maxIDAnal, flid, (int)SyncMsg.ksyncFullRefresh);
				return DbOps.ReadIntArray(m_cache, sqlQuery, null, 2);
			}
		}

		private void m_updateTimer_Elapsed(object sender, EventArgs myEventArgs)
		{
			if (!InWordsAnalyses)
			{
				TraceVerboseLine("ParserListener:Timer not in Analyses tool - don't process timer message.");
				return;
			}
			TraceVerbose(" <<updateTimer,");
			TraceVerbose(" TID="+System.Threading.Thread.CurrentThread.GetHashCode()+" ");
			if (m_busy)
			{
				TraceVerbose(" THE BUSY MEMBER IS ALREADY SET.  STILL PROCESSING LAST TIMER MESSAGE.");
				TraceVerboseLine(" Done>>");
				return;
			}

			int currentWordformHvo = CurrentWordformHvo;
			if (!m_busy && currentWordformHvo > 0)
			{
				m_busy = true;
				try
				{
					// SyncMsg.ksyncSimpleEdit is added to Sync$ table in ParseFiler for every wordform,
					// whether it was changed, or not.
					using (WfiWordformUi wfui = new WfiWordformUi(CurrentWordform))
					{
						foreach (int id in SimpleEdits)
						{
							m_maxID = id;
							wfui.UpdateWordsToolDisplay(currentWordformHvo, false, false, true, true);
						}
					}

					// SyncMsg.ksyncFullRefresh is added to Sync$ table in ParseFiler for every wordform,
					// but only when its count of analyses was changed.
					foreach (int[] ints in FullRefreshes)
					{
						m_maxIDAnal = ints[0];
						int wfID = ints[1];
						// it is possible for this word form to no longer be valid. The parser thread could have
						// added this sync record before the main thread removed it. See LT-9618
						if (m_cache.IsValidObject(wfID))
						{
							using (WfiWordformUi wfui = new WfiWordformUi(WfiWordform.CreateFromDBObject(m_cache, wfID)))
							{
								wfui.UpdateWordsToolDisplay(wfui.Object.Hvo, false, false, true, (currentWordformHvo == wfID));
							}
						}
					}
				}
				finally
				{
					m_busy = false;
				}
			}
		}

		public bool OnIdle(object argument)
		{
			CheckDisposed();

			m_mediator.PropertyTable.SetProperty("StatusPanelProgress", GetParserQueueString() + " " + GetParserActivityString());
			m_mediator.PropertyTable.SetPropertyPersistence("StatusPanelProgress", false);

			ParserConnection con = Connection;
			if (con != null)
			{
				string notification = con.GetAndClearNotification();
				if (notification != null)
					m_mediator.SendMessage("ShowNotification", notification);

				// It is possible that the Activity will be Idle (at least not 'Update',
				// but there are still items in the Sync$ table to process.
				// We have to check here for that case, or some items won't be processed,
				// which will result in the PropChanges not being done, and thus,
				// the display not being updated.
				int countSimpleEdits = 0;
				int countFullRefreshes = 0;
				if (CurrentWordformHvo > 0)
				{
					countSimpleEdits = SimpleEdits.Count;
					countFullRefreshes = FullRefreshes.Count;
				}
				if (con.Activity.IndexOf(ParserUIStrings.ksUpdate) >= 0 || countSimpleEdits > 0 || countFullRefreshes > 0)
					m_updateTimer.Start();
				else
					m_updateTimer.Stop();
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

			if(this.Connection ==null)
				return ParserUIStrings.ksNoParserLoaded;
			else
				return this.Connection.Activity;
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
			if(this.Connection != null)
			{
				low = this.Connection.Parser.GetQueueSize(ParserScheduler.Priority.eventually).ToString();
				med = this.Connection.Parser.GetQueueSize(ParserScheduler.Priority.soon).ToString();
				high = this.Connection.Parser.GetQueueSize(ParserScheduler.Priority.ASAP).ToString();
			}

			return String.Format(ParserUIStrings.ksQueueXYZ, low, med, high);
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
		private bool m_isDisposed = false;

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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_updateTimer != null)
				{
					m_updateTimer.Stop();
					m_updateTimer.Tick -= new EventHandler(m_updateTimer_Elapsed);
					m_updateTimer.Dispose();
				}

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
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_sda = null;
			m_mediator = null;
			m_cache = null;
			m_updateTimer = null;
			m_traceSwitch = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// enable/disable and check items in the Parser menu
		/// </summary>
		internal void menuParser_Popup(object sender, System.EventArgs e)
		{
			CheckDisposed();

			const int kIndexOfStartItem = 6;		//the only one that is enabled if we are disconnected
			const int kIndexOfPauseItem = 8;	//need a check
			bool connected = (null != this.Connection);

			//first, set them all to match whether we are enabled or not
			foreach(MenuItem item in ((MenuItem)sender).MenuItems)
			{
				item.Enabled = connected;
			}
			//next, switch the "start" item to be the opposite
			((MenuItem)sender).MenuItems[kIndexOfStartItem].Enabled = !connected;

			//finally, set the check mark on the pause item.
			if(this.Connection != null)
				((MenuItem)sender).MenuItems[kIndexOfPauseItem].Checked = this.Connection.IsPaused;
		}

		private StText CurrentText
		{
			get
			{
				Object obj = m_mediator.PropertyTable.GetValue("ActiveClerkSelectedObject");
				if (obj != null)
					return obj as StText;
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
		private WfiWordform CurrentWordform
		{
			get
			{
				Object obj = m_mediator.PropertyTable.GetValue("ActiveClerkSelectedObject");
				if (obj != null)
					return obj as WfiWordform;
				else
					return null;
			}
		}

		public int CurrentWordformHvo
		{
			get
			{
				CheckDisposed();

				WfiWordform wf = CurrentWordform;
				if (wf == null)
					return -1;
				else
					return wf.Hvo;
			}
		}

		#region ClearSelectedWordParserAnalyses handlers

		public bool OnDisplayClearSelectedWordParserAnalyses(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Visible = (InWordsAnalyses && (CurrentWordformHvo > 0));
			display.Enabled = (InWordsAnalyses && (CurrentWordformHvo > 0));
			return true;	//we handled this.
		}

		public bool OnClearSelectedWordParserAnalyses(object argument)
		{
			WfiWordform wf = CurrentWordform;
			if (wf == null)
			{
				MessageBox.Show(ParserUIStrings.ksSelectWordFirst);
			}
			else
			{
				if (CurrentWordformHvo > 0)
				{
					using (WfiWordformUi wfui = new WfiWordformUi(WfiWordform.CreateFromDBObject(m_cache, CurrentWordformHvo)))
					{
						if (m_cache.DatabaseAccessor.IsTransactionOpen())
							m_cache.DatabaseAccessor.CommitTrans();
						m_cache.DatabaseAccessor.BeginTrans();
						DbOps.ExecuteStoredProc(
							m_cache,
							string.Format("EXEC RemoveParserApprovedAnalyses$ {0}", CurrentWordformHvo),
							null);
						m_cache.DatabaseAccessor.CommitTrans();
						wfui.UpdateWordsToolDisplay(CurrentWordformHvo, false, false, true, true);
					}
				}
			}

			return true;	//we handled this.
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

			WfiWordform wf = CurrentWordform;
			if (wf == null)
			{
				MessageBox.Show(ParserUIStrings.ksSelectWordFirst);
			}
			else
			{
				if (Connection == null)
					ConnectToParser(false);
				else
				{
					ParserScheduler parser = Connection.Parser;
					if (parser != null)
						parser.LoadGrammarAndLexiconIfNeeded();
				}
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

			StText text = CurrentText;
			if (text != null)
			{
				if (Connection == null)
					ConnectToParser(false);
				UpdateWordformsInText(text);
			}
			return true;	//we handled this.
		}
		public bool OnParseAllWords(object argument)
		{
			CheckDisposed();

			if (Connection == null)
				ConnectToParser(true);

			return true;	//we handled this.
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
			ParserScheduler parser = null;
			if (con != null)
				parser = con.Parser;
			// must wait for the queue to empty before we can fill it up again or else we run the risk of breaking the parser thread
			display.Enabled = ((con != null) && (parser != null) &&
							   parser.GetQueueSize(ParserScheduler.Priority.eventually) == 0);

			return true;	//we handled this.
		}

		public bool OnStopParser(object argument)
		{
			CheckDisposed();

			if(Connection != null)
				DisconnectFromParser();
			return true;	//we handled this.
		}

		// used by Try a Word to get the parser running
		public bool OnReInitParser(object argument)
		{
			CheckDisposed();

			if (Connection == null)
				ConnectToParser(false);
			Connection.Parser.ParseAllWordforms = false;
			Connection.Parser.ReloadGrammarAndLexicon();
			return true; //we handled this.
		}

		public bool OnReparseAllWords(object argument)
		{
			CheckDisposed();

			ParserConnection con = Connection;
			if(con != null)
			{
				ParserScheduler parser = con.Parser;
				if (parser != null)
				{
					parser.ParseAllWordforms = true;
					parser.LoadGrammarAndLexiconIfNeeded();
					parser.InvalidateAllWordforms();
				}
			}
			return true;	//we handled this.
		}

		public virtual bool OnDisplayChooseParser(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			Command cmd = commandObject as Command;

			FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			if (cache == null)
				throw new ArgumentException("no cache!");

			display.Checked = cache.LangProject.MorphologicalDataOA.ActiveParser == cmd.GetParameter("parser");
			return true; //we've handled this
		}

		public bool OnChooseParser(object argument)
		{
			CheckDisposed();
			Command cmd = argument as Command;

			FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			if (cache == null)
				throw new ArgumentException("no cache!");

			string newParser = cmd.GetParameter("parser");
			if (cache.LangProject.MorphologicalDataOA.ActiveParser != newParser)
			{
				if (Connection != null)
					DisconnectFromParser();
				cache.LangProject.MorphologicalDataOA.ActiveParser = newParser;
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
