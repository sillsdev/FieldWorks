// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DataUpdateMonitor.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	#region UpdateSemaphore class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class represents the data update status for a given database connection.
	/// Objects of this class are kept in DataUpdateMonitor's static hashtable.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UpdateSemaphore
	{
		/// <summary></summary>
		public bool fDataUpdateInProgress;
		/// <summary></summary>
		public string sDescr;
		/// <summary></summary>
		public Queue<VwChangeInfo> changeInfoQueue;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Contructor for UpdateSemaphore
		/// </summary>
		/// <param name="fUpdateInProgress">Probably always going to be true.</param>
		/// <param name="sDescription">description of the data update being done,
		/// for debugging.</param>
		/// --------------------------------------------------------------------------------
		public UpdateSemaphore(bool fUpdateInProgress, string sDescription)
		{
			fDataUpdateInProgress = fUpdateInProgress;
			sDescr = sDescription;
			changeInfoQueue = new Queue<VwChangeInfo>();
		}
	}
	#endregion

	#region DataUpdateMonitor class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Create an instance of this class for the duration of a possibly substantial data update.
	/// The DataUpdateMonitor will properly handle all requests to Commit for the duration of
	/// the data update.
	/// Event handlers that do various data updates will also want to test IsUpdateInProgress
	/// because we don't want to begin a second update for a particular DB connections before
	/// the first is done. For example, you don't want to begin an EditPaste or even process
	/// a keypress if another update has not finished processing.
	/// </summary>
	/// <remarks>The purpose of the DataUpdateMonitor is to fix real and potential crashes
	/// resulting from processing windows messages when a data operation is in progress.</remarks>
	/// ----------------------------------------------------------------------------------------
	public class DataUpdateMonitor : IFWDisposable
	{
		/// <summary>
		/// keep track of the sites we're monitoring so we don't try to monitor when DataUpdateMonitor is nested.
		/// </summary>
		private static Set<IVwRootSite> s_sitesMonitoring = new Set<IVwRootSite>();
		// Keys are ISilDataAccess objects, values are UpdateSemaphore objects
		private static Dictionary<ISilDataAccess, UpdateSemaphore> s_UpdateSemaphores = new Dictionary<ISilDataAccess, UpdateSemaphore>(1);
		private ISilDataAccess m_sda;
		private Cursor m_oldCursor;
		private Control m_Owner;
		private IVwRootSite m_rootSite;
		private EditingHelper m_editingHelper;
		/// <summary> save the state of the original selection </summary>
		TextSelInfo m_tsi;
		/// <summary> the tss that we will be inserting at the IP during paste operation</summary>
		private ITsString m_tssIns = null;
		/// <summary> whether or not to queue data change information for doing all the prop changes after
		/// the edits. if false, DataUpdateMonitor doesn't do anything.</summary>
		bool m_fTurnOnMonitor = true;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="owner">The object that creates this DataUpdateMonitor</param>
		/// <param name="sda">Data access object (corresponds to a DB connection)</param>
		/// <param name="site">Root site used to recover the selection after the update</param>
		/// <param name="updateDescription">description of the data update being done,
		/// for debugging.</param>
		/// ------------------------------------------------------------------------------------
		public DataUpdateMonitor(Control owner, ISilDataAccess sda, IVwRootSite site,
			string updateDescription)
			: this(owner, sda, site, updateDescription, true, false)
		{
		}
		/// <summary>
		/// Create one.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="sda"></param>
		/// <param name="site"></param>
		/// <param name="updateDescription"></param>
		/// <param name="fTurnOnMonitor">if true, start monitoring. if false, suspend/disable monitoring.</param>
		/// <param name="fSuppressRecordingPriorSelection">True for operations (currently only ReplaceAll)
		/// where we do not want to record the prior selection. This prevents calling editinghelper.OnAboutToEdit,
		/// which therefore doesn't save info about the current selection. The current actual effect
		/// is to suppress the work of the AnnotationAdjuster for the selected paragraphs.</param>
		public DataUpdateMonitor(Control owner, ISilDataAccess sda, IVwRootSite site,
			string updateDescription, bool fTurnOnMonitor, bool fSuppressRecordingPriorSelection)
		{
			// DataUpdateMonitor may be nested, so make sure we're not already monitoring the site.
			m_fTurnOnMonitor = fTurnOnMonitor && (site == null || !s_sitesMonitoring.Contains(site));
			if (!m_fTurnOnMonitor)
				return;
			// register this site as being monitored.
			if (site != null)
				s_sitesMonitoring.Add(site);
			Debug.Assert(sda != null);
			if (s_UpdateSemaphores.ContainsKey(sda))
			{
				UpdateSemaphore semaphore = s_UpdateSemaphores[sda];
				if (semaphore.fDataUpdateInProgress)
					throw new Exception("Concurrent access on Database detected");
				// Set ((static semaphore) members) for this data update
				semaphore.fDataUpdateInProgress = true;
				semaphore.sDescr = updateDescription;
			}
			else
			{
				s_UpdateSemaphores[sda] = new UpdateSemaphore(true, updateDescription);
			}
			m_Owner = owner;
			m_sda = sda;
			m_rootSite = site;
			if (m_rootSite != null)
				m_editingHelper = ((IRootSite)m_rootSite).EditingHelper;
			// store original selection info.
			// Note, some of its internals are computed from the actual selection
			// which can get changed during the life of DataUpdateMonitor.
			// But its properties and Hvo(fEndPoint) should remain the same.
			if (m_editingHelper != null && !fSuppressRecordingPriorSelection)
			{
				m_tsi = new TextSelInfo(m_editingHelper.EditedRootBox);
				m_editingHelper.OnAboutToEdit();
			}

			// Set wait cursor
			if (owner != null)
			{
				m_oldCursor = owner.Cursor;
				owner.Cursor = Cursors.WaitCursor;
			}
		}

		/// <summary>
		/// During "EditPaste", set this string so that we can accurately
		/// report the number of characters inserted for a PropChange.
		/// </summary>
		internal ITsString InsertedTss
		{
			get { return m_tssIns; }
			set { m_tssIns = value; }
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is disposed; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		~DataUpdateMonitor()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources.
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		/// ------------------------------------------------------------------------------------
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing">if set to <c>true</c> [disposing].</param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing && m_fTurnOnMonitor)
			{
				try
				{
					// Dispose managed resources here.
					if (m_sda != null)
					{
						UpdateSemaphore semaphore = s_UpdateSemaphores[m_sda];
						Debug.Assert(semaphore.fDataUpdateInProgress);
						// Remember selection so that we can try to reconstruct it after the PropChangeds
						SelectionHelper selection = SelectionHelper.Create(m_rootSite);

						TextSelInfo tsiAfterEdit = null;
						if (selection != null)
							tsiAfterEdit = new TextSelInfo(selection.Selection);

						bool fAdjustedChangeInfo = false;
						foreach (VwChangeInfo changeInfo in semaphore.changeInfoQueue)
						{
							int ivIns = changeInfo.ivIns;
							int cvDel = changeInfo.cvDel;
							int cvIns = changeInfo.cvIns;

							// Note: m_sda.MetaDataCache increments an internal com object
							// ref count that may not get cleared until you do
							// Marshal.FinalReleaseComObject on it. if it doesn't get cleared
							// it may hang tests.
							IFwMetaDataCache mdc = m_sda.MetaDataCache;
							if (!fAdjustedChangeInfo &&
								mdc != null &&
								tsiAfterEdit != null &&
								m_editingHelper != null &&
								m_editingHelper.MonitorTextEdits)
							{
								// if the selection-edit resulted in keeping the cursor in the same paragraph
								// we may need to do some more adjustments because views code
								// calculates VwChangeInfo based upon a string comparison, which does not
								// accurately tell us where the string was actually changed if the inserted
								// characters match those character positions in the original string.
								// For example changing "this is the old text" by inserting "this is the new text, but "
								// at the beginning results in the string "this is the old text, but this is the new text"
								// In that example the views code StringCompare would say ivIns started at "old text"
								// rather than the beginning of the string, since "this is the " matches the old string.
								// The first condition prevents our trying to make these adjustments when we have a multi-para
								// (end-before-anchor) selection.
								if (m_tsi.Hvo(true) == m_tsi.Hvo(false) &&
									m_tsi.HvoAnchor == tsiAfterEdit.HvoAnchor &&
									m_tsi.HvoAnchor == changeInfo.hvo && m_tsi.TagAnchor == changeInfo.tag)
								{
									// Our insertion point can be at the beginning or end of the range.
									int ichInsertionPointOrig = Math.Min(m_tsi.IchAnchor, m_tsi.IchEnd);
									// we may need to adjust ivIns, but not for MultiStrings, since
									// ivIns in that case is a ws, not an offset.
									int flidtype = mdc.GetFieldType((uint)changeInfo.tag);
									if (flidtype == (int)CellarModuleDefns.kcptBigString ||
										flidtype == (int)CellarModuleDefns.kcptString)
									{
										// if the anchor has moved back after a delete, use it as a starting point
										if (!m_tsi.IsRange && cvDel > 0 && ivIns < m_tsi.IchAnchor)
										{
											if (ivIns + cvDel == m_tsi.IchAnchor)
											{
												// user did backspace from insertion point, so effectively
												// move the IP back the number of characters deleted.
												ivIns = Math.Max(m_tsi.IchAnchor - cvDel, 0);
											}
											// ctrl-del can also cause this, but in that case, characters
											// after the IP may have been deleted, too. Seems best not to try to adjust.
										}
										else
										{
											// use the original IP, since changeInfo uses CompareStrings
											// to calculate it, and that can be wrong when pasted string has
											// characters that coincidentally match the original string.
											ivIns = ichInsertionPointOrig;
										}
									}

									// if the initial selection is a range selection in the same paragraph
									// set the number of deleted characters to be the difference between
									// the begin and end offsets.
									if (m_tsi.HvoAnchor == m_tsi.Hvo(true) && m_tsi.IsRange)
										cvDel = Math.Abs(m_tsi.IchEnd - m_tsi.IchAnchor);
									// Review: do we expect this string to be Normalized already?
									// Review: should we do nothing if the pasted string contains newline, or set cvIns to the
									// length of the text after the last newline, or what??
									if (InsertedTss != null && InsertedTss.Text != null && InsertedTss.Text.IndexOf(Environment.NewLine) == -1)
										cvIns = InsertedTss.Length;
									// indicate we've adjusted the changeInfo for the next PropChange.
									// this should be done only once per edit action.
									fAdjustedChangeInfo = true;
								}
							}
							m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
								changeInfo.hvo, changeInfo.tag, ivIns, cvIns, cvDel);
						}
						semaphore.fDataUpdateInProgress = false;
						semaphore.sDescr = string.Empty;
						semaphore.changeInfoQueue.Clear();

						// It is possible that the PropChanged caused a regenerate of the view. This
						// turned our selection invalid. Try to recover it.
						if (selection != null && !selection.IsValid)
							selection.SetSelection(false);

						// This needs to be called after setting the selection. It can cause
						// AnnotationAdjuster.EndKeyPressed() to be called which expects a
						// selection to be set.
						if (m_editingHelper != null)
							m_editingHelper.OnFinishedEdit();

						// It is possible that OnFinishedEdit() caused a regenerate of the view. This
						// turned our selection invalid. Try to recover it.
						if (selection != null && !selection.IsValid)
							selection.SetSelection(false);
					}
				}
				finally
				{
					// In case anything goes wrong, if we possibly can, do this anyway, other wise the pane
					// may be more-or-less permanently locked.
					if (m_rootSite != null)
						s_sitesMonitoring.Remove(m_rootSite);
					// end Wait Cursor
					// Since it needs m_Owner, this must be done when 'disposing' is true,
					// as that is a disposable object, which may be gone in
					// Finalizer mode.
					if (m_Owner != null)
						m_Owner.Cursor = m_oldCursor;
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_sda = null;
			m_Owner = null;
			m_rootSite = null;
			m_oldCursor = null;
			m_tsi = null;
			m_tssIns = null;

			m_isDisposed = true;
		}

		/// <summary>
		/// Throw if the IsDisposed property is true
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		#endregion IDisposable & Co. implementation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If true, a data update is in progress.
		/// </summary>
		/// <param name="sda">Data access object (corresponds to a DB connection)</param>
		/// <returns><c>true</c> if a data update is in progress, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public static bool IsUpdateInProgress(ISilDataAccess sda)
		{
			if (sda == null
				|| !s_UpdateSemaphores.ContainsKey(sda)
				|| s_UpdateSemaphores[sda] == null)
			{
				return false;
			}
			return s_UpdateSemaphores[sda].fDataUpdateInProgress;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a data update is actually in progress, we want to only complete edits and not
		/// notify the world of the prop changes yet (we'll store the info in a queue for
		/// broadcast later, in Dispose()).
		/// </summary>
		/// <param name="vwsel"></param>
		/// <param name="sda">Data access object (corresponds to a DB connection)</param>
		/// <returns>Return value from IVwSelection.Commit() or IVwSelection.CompleteEdits()
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static bool Commit(IVwSelection vwsel, ISilDataAccess sda)
		{
			if (vwsel == null)
				return false;

			UpdateSemaphore semaphore = null;
			if (s_UpdateSemaphores.ContainsKey(sda))
			{
				semaphore = s_UpdateSemaphores[sda];
			}
			if (semaphore == null || !semaphore.fDataUpdateInProgress)
				return vwsel.Commit();

			VwChangeInfo changeInfo;
			bool fRet = vwsel.CompleteEdits(out changeInfo);
			if (changeInfo.hvo != 0)
				semaphore.changeInfoQueue.Enqueue(changeInfo);
			return fRet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove knowledge of a particular SilDataAccess (DB connection). This is used both
		/// for testing and also whenever a database connection is being closed but the app is
		/// not being destroyed (e.g., during backup, or when the last window connected to a
		/// particular DB is closed).
		/// </summary>
		/// <param name="sda">Data access object (corresponds to a DB connection)</param>
		/// ------------------------------------------------------------------------------------
		public static void RemoveDataAccess(ISilDataAccess sda)
		{
			Debug.Assert(!IsUpdateInProgress(sda));
			if (sda != null)
				s_UpdateSemaphores.Remove(sda);
		}
	}
	#endregion
}
