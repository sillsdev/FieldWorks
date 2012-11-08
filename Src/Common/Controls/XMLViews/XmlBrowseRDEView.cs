// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlBrowseRDEView.cs
// Responsibility: WordWorks
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class XmlBrowseRDEView : XmlBrowseViewBase
	{
		#region Data members

		private System.ComponentModel.IContainer components = null;

		private bool fInDoMerges = false; // used to ignore recursive calls to DoMerges.

		#endregion Data members

		#region Construction, initialization, and disposal.

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:XmlBrowseRDEView"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlBrowseRDEView()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose( disposing );

			if( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the specified node spec.
		/// </summary>
		/// <param name="nodeSpec">The node spec.</param>
		/// <param name="hvoRoot">The hvo root.</param>
		/// <param name="fakeFlid">The fake flid.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="mediator">The mediator.</param>
		/// <param name="bv">The bv.</param>
		/// ------------------------------------------------------------------------------------
		public override void Init(XmlNode nodeSpec, int hvoRoot, int fakeFlid,
			FdoCache cache, Mediator mediator, BrowseViewer bv)
		{
			CheckDisposed();

			// Use the ones in fakeFlid, and any we create.
			base.Init(nodeSpec, hvoRoot, fakeFlid, cache, mediator, bv);
		}

		#endregion Construction, initialization, and disposal.

		#region Properties

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the VC. It has some important functions related to interpreting fragment IDs
		/// that the filter bar needs.
		/// </summary>
		/// <value>The vc.</value>
		/// ------------------------------------------------------------------------------------
		public override XmlBrowseViewBaseVc Vc
		{
			get
			{
				CheckDisposed();

				if (m_xbvvc == null)
				{
					m_xbvvc = new XmlRDEBrowseViewVc(m_nodeSpec, m_fakeFlid, m_stringTable, this);
				}
				return base.Vc;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the RDE vc.
		/// </summary>
		/// <value>The RDE vc.</value>
		/// ------------------------------------------------------------------------------------
		protected XmlRDEBrowseViewVc RDEVc
		{
			get { return m_xbvvc as XmlRDEBrowseViewVc; }
		}

		/// <summary>
		/// True if we are running the read-only version of the view that is primarily used for
		/// selecting.
		/// </summary>
		protected override bool ReadOnlySelect
		{
			get { return false; }
		}

		/// <summary>
		/// Overrides the selected row highlighting method so the highlighting will always be none instead of being reliant on
		/// ReadOnlySelect
		/// </summary>
		public override void SetSelectedRowHighlighting()
		{
			CheckDisposed();

			SelectedRowHighlighting = SelectionHighlighting.none;
		}

		#endregion Properties

		#region XCore message handlers

		/// <summary>
		/// This name is magic for an xCoreColleague that is active at the time when an xWindow is being closed.
		/// If some active colleague implements this method, it gets a chance to do something special as the
		/// xWindow closes (and can veto the close, though we aren't really using that here).
		/// </summary>
		/// <returns></returns>
		public bool OnConsideringClosing(object sender, CancelEventArgs arg)
		{
			CheckDisposed();

			arg.Cancel = CleanupPendingEdits();
			return arg.Cancel; // if we want to cancel, others don't need to be asked.
		}

		/// <summary>
		/// This is invoked by the PropertyTable (because XmlBrowseView is a mediator).
		/// </summary>
		/// <param name="propName"></param>
		public override void OnPropertyChanged(string propName)
		{
			CheckDisposed();

			/*
			 * Starting up with Domains being startup tool.
			 *	currentContentControlObject
			 *	StatusPanelProgress
			 *
			 * Switching away from Domains to another tool in same area.
			 *	currentContentControlParameters
			 *
			 * Switching to Domains, when another tool in same area was first to start
			 *	ToolForAreaNamed_lexicon
			 *	currentContentControlObject
			 *
			 * Switch to another area from domains
			 *	areaChoiceParameters
			 *	InitialArea
			 *	currentContentControlParameters
			 *
			 * Switch to Domains from another area (other area was first to start)
			 *	currentContentControlObject
			 *
			 * Switch to another domain
			 *	StatusPanelRecordNumber
			 *	StatusPanelMessage
			 *	StatusPanelRecordNumber
			 *	StatusPanelMessage
			 *	-selected
			 *	SemanticDomainList-selected
			 *	ActiveClerkSelectedObject
			 *	SelectedTreeBarNode
			 */

			// "currentContentControlObject" occurs in two incoming contexts,
			// and one switching away context.
			// We only need to handle the switching away context, but how to tell them apart?
			// JohnT: don't handle switching to another domain by catching StatusPanelRecordNumber;
			// too unreliable. Catch that by override of RootObjectHvo below.
			if (propName == "areaChoiceParameters" // Switching to another area
				|| propName == "currentContentControlParameters") // Switching to another tool in same area
			{
				CleanupPendingEdits();
			}

			base.OnPropertyChanged(propName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// the object that has properties that are shown by this view.
		/// </summary>
		/// <value></value>
		/// <remarks> this will be changed often in the case where this view is dependent on another one;
		/// that is, or some other browse view has a list and each time to selected item changes, our
		/// root object changes.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public override int RootObjectHvo
		{
			set
			{
				CheckDisposed();

				CleanupPendingEdits();
				base.RootObjectHvo = value;
			}
		}

		/// <summary>
		/// In general, we just want to make sure to scroll to the current selection,
		/// or if none exists, make one in the new row and scroll there.
		/// </summary>
		/// <param name="hvo">ignore</param>
		/// <param name="index">ignore</param>
		protected override void DoSelectAndScroll(int hvo, int index)
		{
			if (m_rootb == null)
				MakeRoot();

			// if we haven't already made a selection, make one in the new row.
			if (m_rootb.Selection == null)
			{
				SetSelectionToFirstColumnInNewRow();
			}
			else
			{
				ScrollToCurrentSelection();
			}
		}
		#endregion XCore message handlers

		#region Other methods

		/// <summary>
		/// Cleanup any pending edits.
		/// </summary>
		/// <returns>True to cancel the window closing, otherwise false.</returns>
		private bool CleanupPendingEdits()
		{
			bool cancelClose = false;
			ITsString[] rgtss;
			if (CanGotoNextRow(out rgtss))
			{
				// JohnT: if we pass false here, then the following sequence fails (See LT-7140)
				// Use Rapid Words to add a sense where the form (but not definition) matches an existing entry.
				// Without hitting enter, click on another view (e.g., main data entry view).
				// Then return to the Rapid Words view. The last item added does not appear.
				// However, if we pass true, we get another LT-7140 problem: if we add a row but don't press enter,
				// then switch to another Category, the new item shows up there! That is because a call-back
				// to the record list happens during adding the item, but the record list has already changed its
				// current HVO.
				// The solution is to pass false here, but set a property <Clerkid>_AlwaysReloadVirtualProperty
				// on the tool so we always reload when switching.
				CreateObjectFromEntryRow(rgtss, false);
			}
			//ProcessRDEKeyPress('\r');
			DoMerges();

			return cancelClose;
		}
		/// <summary>
		/// Check whether we have enough data entered in this row to go on to create another row.
		/// </summary>
		/// <returns>true if we can proceed to the next row, false otherwise</returns>
		private bool CanGotoNextRow(out ITsString[] rgtss)
		{
			bool fCanGotoNextRow = false;
			List<XmlNode> columns = m_xbvvc.ColumnSpecs;
			rgtss = GetColumnStringsFromNewRow();
			for (int i = 1; i <= columns.Count; ++i)
			{
				// We must have a citation form, but definitions are optional. (!?)
				// Review: Currently we key off the column labels to determine which columns
				// correspond to CitationForm and which correspond to Definition.
				// Ideally we'd like to get at the flids used to build the column display strings.
				// Instead of passing in only ITsStrings, we could pass in a structure containing
				// an index of strings with any corresponding flids. Here's we'd expect strings
				// based upon either LexemeForm.Form or LexSense.Definition. We could probably
				// do this as part of the solution to handling duplicate columns in LT-3763.
				XmlNode column = columns[i - 1];
				string columnLabel = XmlUtils.GetManditoryAttributeValue(column, "label");
				string[] columnLabelComponents = columnLabel.Split(new char[] { ' ', ':' });
				// get column label without writing system or extraneous information.
				string columnBasicLabel = LocalizeIfPossible(columnLabelComponents[0]);
				// true if we find any Word column entry of nonzero length.
				if (!fCanGotoNextRow &&
					columnBasicLabel == LocalizeIfPossible("Word") &&
					rgtss[i - 1].Length > 0)
				{
					string s = rgtss[i - 1].Text;
					if (s.Trim().Length > 0)
						fCanGotoNextRow = true;
				}
			}
			return fCanGotoNextRow;
		}

		private ITsString[] GetColumnStringsFromNewRow()
		{
			ITsString[] rgtss;
			ISilDataAccess sda = m_bv.SpecialCache;
			List<XmlNode> columns = m_xbvvc.ColumnSpecs;
			// Conceptual model class.
			rgtss = new ITsString[columns.Count];
			for (int i = 1; i <= columns.Count; ++i)
			{
				int kflid = XMLViewsDataCache.ktagEditColumnBase + i;
				int wsCol = WritingSystemServices.GetWritingSystem(m_fdoCache, columns[i - 1], null,
					m_fdoCache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle).Handle;
				// Get the string for each column.
				rgtss[i - 1] = sda.get_MultiStringAlt(XmlRDEBrowseViewVc.khvoNewItem,
					kflid, wsCol);
			}
			return rgtss;
		}

		/// <summary>
		/// Clears the strings from the new row and puts cursor back in first column.
		/// </summary>
		private void ClearColumnStringsFromNewRow()
		{
			XMLViewsDataCache sda = m_bv.SpecialCache;
			List<XmlNode> columns = m_xbvvc.ColumnSpecs;
			// Conceptual model class.
			string sCmClass = ((XmlRDEBrowseViewVc) m_xbvvc).EditRowModelClass;
			// Reset the new item row so that the cells are all empty.
			for (int i = 1; i <= columns.Count; ++i)
			{
				int kflid = XMLViewsDataCache.ktagEditColumnBase + i;
				int wsCol = WritingSystemServices.GetWritingSystem(m_fdoCache, columns[i - 1], null,
					m_fdoCache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle).Handle;
				sda.SetMultiStringAlt(XmlRDEBrowseViewVc.khvoNewItem, kflid, wsCol, Cache.TsStrFactory.MakeString("", wsCol));
			}
			// Set the selection to the first column.
			SetSelectionToFirstColumnInNewRow();

		}

		private string LocalizeIfPossible(string sValue)
		{
			if (m_stringTable == null)
				return sValue;
			else
				return m_stringTable.LocalizeAttributeValue(sValue);
		}

		/// <summary>
		/// check whether we need to handle the key press, and do so if so.  The keys we handle
		/// are TAB ('\t') and ENTER/RETURN ('\r').
		/// </summary>
		/// <param name="keyPressed">key press data</param>
		/// <returns>true if the key press has been handled, otherwise false.</returns>
		private bool ProcessRDEKeyPress(char keyPressed)
		{
			IVwSelection vwsel = m_rootb.Selection;
			if (vwsel == null)
				return false;

			int cvsli = vwsel.CLevels(false) - 1;
			int ihvoRoot;
			int tagTextProp;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ws;
			bool fAssocPrev;
			int ihvoEnd;
			ITsTextProps ttp;
			SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(vwsel, cvsli,
				out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
				out ws, out fAssocPrev, out ihvoEnd, out ttp);

			bool fNewOrEditable = rgvsli[0].hvo == XmlRDEBrowseViewVc.khvoNewItem;
			if (!fNewOrEditable)
			{
				for (int i = 0; i < cvsli; ++i)
				{
					if (RDEVc.EditableObjectsContains(rgvsli[i].hvo))
					{
						fNewOrEditable = true;
						break;
					}
				}
				if (!fNewOrEditable)
					return false;
			}

			List<XmlNode> columns = m_xbvvc.ColumnSpecs;
			if (columns == null || columns.Count == 0)
				return false;		// Something is broken!

			ITsString[] rgtss = null;
			bool retval = true;
			switch (keyPressed)
			{
				default:
					retval = false;
					break;
				case '\t':
					ScrollToCurrentSelection();
					break;
				case '\r':
					if (!CanGotoNextRow(out rgtss))
						return true;
					HandleEnterKey(rgtss);
					break;
			}

			return retval;
		}

		/// <summary>
		/// Handle a tab key.
		/// </summary>
		/// <param name="vwsel"></param>
		/// <param name="rgtss"></param>
		/// <returns></returns>
		private bool CanAdvanceToNewRow(IVwSelection vwsel, out ITsString[] rgtss)
		{
			rgtss = null;
			int iLevel;
			int iBox;
			int iTableBox;
			int cTableBoxes;
			int iTableLevel;
			int iCellBox;
			int cCellBoxes;
			int iCellLevel;
			GetCurrentTableCellInfo(vwsel, out iLevel, out iBox, out iTableBox, out cTableBoxes,
				out iTableLevel, out iCellBox, out cCellBoxes, out iCellLevel);
			bool fBackTab = (ModifierKeys & Keys.Shift) == Keys.Shift;
			//bool fPrevRow = false;
			//bool fNextRowRTL = false;
			iLevel = iCellLevel;
			if (m_xbvvc.ShowColumnsRTL)
			{
				if (fBackTab)
				{
					// BackTab: move Left-to-Right, Bottom-to-Top
					iBox = iCellBox + 1;
					if (iBox >= cCellBoxes)
					{
						//fPrevRow = true;
						if (iTableBox > 0)
						{
							iLevel = iTableLevel;
							iBox = iTableBox - 1;
						}
						else
						{
							iBox = 0;
						}
					}
				}
				else
				{
					// Tab: move Right-to-Left, Top-to-Bottom
					iBox = iCellBox - 1;
					if (iBox < 0)
					{
						++iTableBox;
						if (iTableBox < cTableBoxes)
						{
							iLevel = iTableLevel;
							iBox = iTableBox;
							//fNextRowRTL = true;
						}
						else
						{
							if (CanGotoNextRow(out rgtss))
							{
								// Treat the same as Enter in the last column.
								return true;	// needed for HandleEnterKey.
							}
							else
							{
								// We need this information -- make it easy to enter it.
								iBox = 0;
							}
						}
					}
				}
			}
			else
			{
				// BackTab: move Right-to-Left, Bottom-to-Top
				if (fBackTab)
				{
					iBox = iCellBox - 1;
					if (iBox < 0)
					{
						//fPrevRow = true;
						if (iTableBox > 0)
						{
							iLevel = iTableLevel;
							iBox = iTableBox - 1;
						}
						else
						{
							iBox = 0;
						}
					}
				}
				else
				{
					// Tab: move Left-to-Right, Top-to-Bottom
					iBox = iCellBox + 1;
					if (iBox >= cCellBoxes)
					{
						++iTableBox;
						if (iTableBox < cTableBoxes)
						{
							iLevel = iTableLevel;
							iBox = iTableBox;
						}
						else
						{
							if (CanGotoNextRow(out rgtss))
							{
								// Treat the same as Enter in the last column.
								return true;	// needed for HandleEnterKey.
							}
							else
							{
								// We need this information -- make it easy to enter it.
								iBox = 0;
							}
						}
					}
				}
			}
			return false;	// signal we're done.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process an Enter key by creating a new edit row and moving to it.
		/// </summary>
		/// <param name="rgtss">The RGTSS.</param>
		/// ------------------------------------------------------------------------------------
		private void HandleEnterKey(ITsString[] rgtss)
		{
			// Use reflection to invoke a static method on an assembly/class.
			CreateObjectFromEntryRow(rgtss, true);
		}

		/// <summary>
		/// Set selection (and index) to first column in new row, and scroll into view.
		/// </summary>
		private void SetSelectionToFirstColumnInNewRow()
		{
			List<XmlNode> columns = m_xbvvc.ColumnSpecs;
			int flidNew = XMLViewsDataCache.ktagEditColumnBase + 1;
			int wsNew = WritingSystemServices.GetWritingSystem(m_fdoCache, columns[0], null,
				m_fdoCache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle).Handle;
			try
			{
				SelLevInfo[] rgvsli = new SelLevInfo[1];
				rgvsli[0].hvo = XmlRDEBrowseViewVc.khvoNewItem;
				rgvsli[0].tag = -1;
				IVwSelection vwsel = m_rootb.MakeTextSelection(0, rgvsli.Length, rgvsli, flidNew, 0,
					0, 0, wsNew, false, -1, null, false);
				if (m_xbvvc.ShowColumnsRTL)
				{
					int iLevel;
					int iBox;
					int iTableBox;
					int cTableBoxes;
					int iTableLevel;
					int iCellBox;
					int cCellBoxes;
					int iCellLevel;
					GetCurrentTableCellInfo(vwsel, out iLevel, out iBox, out iTableBox,
						out cTableBoxes, out iTableLevel, out iCellBox, out cCellBoxes,
						out iCellLevel);
					m_rootb.MakeSelInBox(vwsel, true, iCellLevel, cCellBoxes - 1,
							true, false, true);
				}
				else
				{
					vwsel.Install();
				}
				ScrollToCurrentSelection();
			}
			catch
			{
				// Ignore a failure here.
				Debug.WriteLine("MakeTextSelection failed in ProcessRDEKeyPress()");
			}
		}

		void ScrollToCurrentSelection()
		{
			if (m_rootb == null)
				MakeRoot();

			if (m_rootb.Selection == null)
				return;
			this.ScrollSelectionIntoView(m_rootb.Selection, VwScrollSelOpts.kssoDefault);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disables/enables the Edit/Undo menu item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnDisplayUndo(object commandObject, ref UIItemDisplayProperties display)
		{
			if (HasNonEmptyNewRow)
			{
				display.Enabled = true;
				display.Text = XMLViewsStrings.ksUndoNewRowData;
				return true;
			}
			return false; // we don't want to handle the command.
		}

		/// <summary>
		/// Return true if we have setup a new row that has data which has not yet been
		/// committed to the database.
		/// </summary>
		private bool HasNonEmptyNewRow
		{
			get
			{
				ITsString[] rgtss = GetColumnStringsFromNewRow();
				foreach (ITsString tss in rgtss)
				{
					if (tss != null && tss.Length > 0)
						return true;
				}
				return false;
			}
		}

		/// <summary>
		/// We need to override Undo so that we can clear data in the new row
		/// since it has not been committed to the database. otherwise, Undo
		/// will get rid of the current row, and the previously committed row.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		internal bool OnUndo(object args)
		{
			if (HasNonEmptyNewRow)
			{
				// simply reset the current row.
				ClearColumnStringsFromNewRow();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Create an undo/redo action for updating the display after the user commits changes in a new row.
		/// </summary>
		class CreateObjectFromEntryRowUndoAction : UndoActionBase
		{
			XmlBrowseRDEView m_xbrdev = null;
			int m_newObjHvo = 0;
			bool m_fAddItems;

			internal CreateObjectFromEntryRowUndoAction(XmlBrowseRDEView browseView, int newObjHvo, bool fAddItems)
			{
				m_xbrdev = browseView;
				m_newObjHvo = newObjHvo;
				m_fAddItems = fAddItems;
			}

			public override bool Redo()
			{
				DoIt();
				return true;
			}

			public override bool Undo()
			{
				m_xbrdev.RDEVc.EditableObjectsRemove(m_newObjHvo);
				if (m_fAddItems)
				{
					// remove sort item
					m_xbrdev.Vc.SortItemProvider.RemoveItemsFor(m_newObjHvo);
				}
				m_xbrdev.DoNotify();
				return true;
			}

			public void DoIt()
			{
				// Add this to the list of objects we're allowed to edit.
				m_xbrdev.RDEVc.EditableObjectsAdd(m_newObjHvo);
				// AFTER that (so the VC knows it is editable), add it to the fake
				// property and update the display to show it.
				if (m_fAddItems)
				{
					m_xbrdev.Vc.SortItemProvider.AppendItemsFor(m_newObjHvo);
				}
				m_xbrdev.DoNotify();
			}
		}

		/// <summary>
		/// Create an object from the current data entry row.
		/// </summary>
		/// <param name="rgtss"></param>
		/// <param name="fAddItems"></param>
		/// <returns></returns>
		private int CreateObjectFromEntryRow(ITsString[] rgtss, bool fAddItems)
		{
			int newObjHvo = 0;
			// Conceptual model class.
			string sUndo = String.Format(XMLViewsStrings.ksUndoNewEntryX, rgtss[0].Text);
			string sRedo = String.Format(XMLViewsStrings.ksRedoNewEntryX, rgtss[0].Text);
			try
			{
				UndoableUnitOfWorkHelper.Do(sUndo, sRedo, Cache.ActionHandlerAccessor, () =>
				{
					newObjHvo = CreateObjectFromEntryRow(rgtss);
					CreateObjectFromEntryRowUndoAction undoRedoAction = new CreateObjectFromEntryRowUndoAction(this, newObjHvo, fAddItems);
					Cache.ActionHandlerAccessor.AddAction(undoRedoAction);
					undoRedoAction.DoIt();
				});
			}
			catch (Exception error)
			{
				throw new RuntimeConfigurationException(String.Format(
					"XmlBrowseRDEView.ProcessRDEKeyPress() could not invoke the static {0} method of the class {1}",
					RDEVc.EditRowSaveMethod, RDEVc.EditRowClass), error);
			}
			return newObjHvo;
		}

		private int CreateObjectFromEntryRow(ITsString[] rgtss)
		{
			List<XmlNode> columns = m_xbvvc.ColumnSpecs;

			// Enhance JohnT: here we assume the class that creates instances is the class created plus "Factory".
			// We may want to make it a separate attribute of the configuration XML at some point.
			string factoryClassName = RDEVc.EditRowClass + "Factory";
			Type factoryType = ReflectionHelper.GetType(RDEVc.EditRowAssembly, factoryClassName);
			object factory = Cache.ServiceLocator.GetService(factoryType);
			System.Reflection.MethodInfo mi = factoryType.GetMethod(RDEVc.EditRowSaveMethod);
			object[] parameters = new object[5];
			parameters[0] = (object)m_hvoRoot;
			parameters[1] = (object)columns;
			parameters[2] = (object)rgtss;
			parameters[3] = (object)m_fdoCache;
			parameters[4] = (m_mediator != null && m_mediator.HasStringTable) ? (object)m_mediator.StringTbl : null;
			int newObjHvo = (int)mi.Invoke(factory, parameters);
			return newObjHvo;
		}

		private void DoNotify()
		{
			// Reset the new item row so that the cells are all empty.
			// Needs doing even when leaving the view altogether, because a new view on the same
			// category may remember it and re-create it on startup as its root object changes if we aren't careful.
			ClearColumnStringsFromNewRow();
		}

		/// <summary>
		/// Invoke the editRowSaveMethod for each new sense to merge any that match a pre-existing
		/// sense (or each other).
		/// The prototypical method we call is this one (from LexSense)
		/// public static void RDEMergeSense(int hvoDomain, int tagList,
		///		generic List(XmlNode) columns, FdoCache cache, int hvoSense, Dictionary(int, bool) newHvos)
		/// </summary>
		public void DoMerges()
		{
			CheckDisposed();

			// Check for recursive calls, which can happen when deleting duplicate entries
			// generates a PropChanged which CleanupPendingEdits handles by calling us again.
			if (fInDoMerges)
				return;

			try
			{
				try
				{
					RDEVc.EditableObjectsRemoveInvalidObjects();

					Set<int> idsClone = RDEVc.EditableObjectsClone();
					fInDoMerges = true;
					Type targetType = ReflectionHelper.GetType(RDEVc.EditRowAssembly, RDEVc.EditRowClass);
					System.Reflection.MethodInfo mi = targetType.GetMethod(RDEVc.EditRowMergeMethod);
					object[] parameters = new object[4];
					parameters[0] = (object)m_hvoRoot;
					parameters[1] = (object)m_xbvvc.ColumnSpecs;	// this is already a List<XmlNode>
					parameters[2] = (object)m_fdoCache;
					parameters[3] = (object)idsClone; // This is a Set<int>.

					// Make a copy. I (JohnT) don't see how this collection can get modified
					// during the loop, but we've had exceptions (e.g., LT-1355) claiming that
					// it has been.
					foreach (int hvoSense in idsClone)
					{
						ICmObject target = Cache.ServiceLocator.GetObject(hvoSense);
						try
						{
							if ((bool)mi.Invoke(target, parameters))
							{
								// The sense was deleted as a duplicate; get rid of it from our fakeflid, too.
								ISilDataAccessManaged sda = m_bv.SpecialCache;
								int[] oldList = sda.VecProp(m_hvoRoot, m_fakeFlid);
								for (int i = 0; i < oldList.Length; i++)
								{
									if (oldList[i] == hvoSense)
									{
										m_bv.SpecialCache.Replace(m_hvoRoot, m_fakeFlid, i, i+1, new int[0], 0);
									}
								}
							}
						}
						catch (Exception e)
						{
							// Ignore a failure here - LT-3091 (after adding a word and def in cat entry,
							// selecting undo and then closing the application causes the following error:
							//     Msg: Tried to create an FDO object based on db object(hvo=39434class=0),
							//     but that class is not fit in this signature (LexSense)
							Debug.WriteLine("mi.Invoke failed in XmlBrowseRDEView: " + e.InnerException.Message);
							throw e;
						}
					}
				}
				catch (Exception error)
				{
					throw new RuntimeConfigurationException(String.Format(
						"XmlBrowseRDEView.DoMerges() could not invoke the static {0} method of the class {1}",
						RDEVc.EditRowMergeMethod, RDEVc.EditRowClass), error);
				}
				RDEVc.EditableObjectsClear();
				int cobj = m_bv.SpecialCache.get_VecSize(m_hvoRoot, m_fakeFlid);
				m_bv.BrowseView.RootBox.PropChanged(m_hvoRoot, m_fakeFlid, 0, cobj, cobj);
			}
			finally
			{
				fInDoMerges = false;
			}
		}

		#endregion Other methods

		#region Other message handlers

		/// <summary>
		/// Overridden to implement the Show Entry in Lexicon right-click menu.
		/// We would prefer to implement this using the methods of CmObjectUi, but the assembly
		/// dependencies go the wrong way.
		/// </summary>
		/// <param name="pt"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="Context menu - can't dispose right away")]
		protected override bool OnRightMouseUp(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			var sel = MakeSelectionAt(new MouseEventArgs(MouseButtons.Right, 1, pt.X, pt.Y, 0));
			if (sel == null)
				return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
			var index = GetRowIndexFromSelection(sel, false);
			if (index < 0)
				 return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
			int hvo = m_sda.get_VecItem(m_hvoRoot, m_fakeFlid, index);
			// It would sometimes work to jump to one of the editable objects.
			// But sometimes one of them will get destroyed when we switch away from this view
			// (e.g., because there is already a similar sense to which we can just add this semantic domain).
			// Then we would crash trying to jump to it.
			if (RDEVc.EditableObjectsContains(hvo))
				return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
			ICmObject target;
			if (!Cache.ServiceLocator.ObjectRepository.TryGetObject(hvo, out target) || !(target is ILexSense))
				return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
			// We have a valid (real, not temporary fake) LexEntry and will put up the context menu
			var menu = new ContextMenuStrip();
			var item = new ToolStripMenuItem(XMLViewsStrings.ksShowEntryInLexicon);
			menu.Items.Add(item);
			item.Click += (sender, args) => JumpToToolFor(((ILexSense)target).Entry);
			menu.Show(this, pt);
			return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
		}

		private void JumpToToolFor(ICmObject target)
		{
			m_mediator.PostMessage("FollowLink", new FwLinkArgs("lexiconEdit", target.Guid));
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles OnKeyDown.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnKeyDown(KeyEventArgs e)
		{
			// try to calculate whether we will be advancing to the next row
			// BEFORE we call OnKeyDown, because it will change the selection.
			IVwSelection vwsel = RootBox.Selection;
			bool fHandleEnterKey = false;
			ITsString[] rgtss = null;
			if (e.KeyCode == Keys.Tab && vwsel != null)
			{
				fHandleEnterKey = CanAdvanceToNewRow(vwsel, out rgtss);
			}
			base.OnKeyDown(e);
			if (fHandleEnterKey)
				HandleEnterKey(rgtss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles OnKeyPress.  Passes most things to EditingHelper.OnKeyPress
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (!ProcessRDEKeyPress(e.KeyChar))
				base.OnKeyPress(e);
		}

		// JohnT: Someone found a better way to catch things changing; see callers of
		//		CleanupPendingEdits. We must not split this up, however; we had a problem when
		//		CleanupPendingEdits handled simulating typing Enter, but OnLostFocus did the DoMerges.
		// It's important for DoMerges to come after the simulated Enter.
		//		protected override void OnGotFocus(EventArgs e)
		//		{
		//			base.OnGotFocus(e);
		////-			if (m_hmark == 0)
		////-			{
		////-				IActionHandler ah = m_fdoCache.ActionHandlerAccessor;
		////-				if (ah != null)
		////-					m_hmark = ah.Mark();
		////-			}
		//		}


		//		protected override void OnLostFocus(EventArgs e)
		//		{
		//			this.DoMerges();
		////-			IActionHandler ah = m_fdoCache.ActionHandlerAccessor;
		////-			if (ah != null && m_hmark != 0)
		////-			{
		////-				if (ah.get_TasksSinceMark(true))
		////-					ah.CollapseToMark(m_hmark, "Undo Entry", "Redo Entry");
		////-				else
		////-					ah.DiscardToMark(m_hmark);
		////-				m_hmark = 0;
		////-			}
		//			base.OnLostFocus(e);
		//		}


		/// <summary>
		///	see if it makes sense to provide the "delete record" command now
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public override bool OnDisplayDeleteRecord(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			bool fCanDelete = false;
			// This crashed once on exiting program, so m_rootb may not be set at that point.
			IVwSelection vwsel = m_rootb != null ? m_rootb.Selection: null;
			if (vwsel != null)
			{
				int ihvoRoot;
				int tag;
				int cpropPrevious;
				int ichAnchor;
				int ichEnd;
				int ws;
				bool fAssocPrev;
				int ihvoEnd;
				ITsTextProps ttp;
				int cvsli = vwsel.CLevels(false) - 1;
				SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(vwsel, cvsli,
					out ihvoRoot, out tag, out cpropPrevious, out ichAnchor, out ichEnd,
					out ws, out fAssocPrev, out ihvoEnd, out ttp);

				if (rgvsli[0].hvo == XmlRDEBrowseViewVc.khvoNewItem)
				{
					ITsString[] rgtss;
					fCanDelete = CanGotoNextRow(out rgtss);
				}
				else
				{
					for (int i = 0; i < cvsli; ++i)
					{
						if (RDEVc.EditableObjectsContains(rgvsli[i].hvo))
						{
							fCanDelete = true;
							break;
						}
					}
				}
			}
			display.Enabled = fCanDelete;
			display.Text = String.Format(display.Text, XMLViewsStrings.ksRow);
			return true;
		}

		/// <summary>
		/// Figure a tooltop for the DeleteRecord command. Must be passed a ToolTipHolder, but we have to follow a pattern.
		/// Called by mediator using reflection.
		/// </summary>
		public bool OnDeleteRecordToolTip(object holder)
		{
			var realHolder = (ToolTipHolder)holder;
			realHolder.ToolTip = String.Format(realHolder.ToolTip, XMLViewsStrings.ksRow);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when [delete record].
		/// </summary>
		/// <param name="commandObject">The command object.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool OnDeleteRecord(object commandObject)
		{
			CheckDisposed();

			if (m_rootb == null)
				MakeRoot();

			IVwSelection vwsel = m_rootb.Selection;
			if (vwsel == null)
				return false;
			ISilDataAccess sda = m_bv.SpecialCache;
			List<XmlNode> columns = m_xbvvc.ColumnSpecs;
			if (columns == null || columns.Count == 0)
				return false;		// Something is broken!

			TextSelInfo tsi = new TextSelInfo(m_rootb.Selection);
			if (tsi.ContainingObject(0) == XmlRDEBrowseViewVc.khvoNewItem)
			{
				ClearColumnStringsFromNewRow();
			}
			else
			{
				// 1. Remove the domain from the sense shown in the second column.
				// 2. Delete the sense iff it is now empty except for the definition shown.
				// 3. Delete the entry iff the entry now has no senses.
#if false // JohnT: don't understand the following code at all. In particular it makes no sense
				// to use ihvoRoot to index rgvsli; ihvoRoot is always zero in this view, it has only one root.
				// Possibly this was an unsuccessful attempt to adapt some generic code I wrote to this
				// particular application involving senses and entries?
				// I'm leaving it in existence for now in case the original author turns up and
				// can explain what he was getting at.
				int cLevels = vwsel.get_BoxDepth(true);
				int iLevel;
				int cBoxes = -1;
				int iBox = -1;
				VwBoxType[] rgvbt = new VwBoxType[cLevels];
				VwBoxType vbt = VwBoxType.kvbtUnknown;
				for (iLevel = 0; iLevel < cLevels; ++iLevel)
				{
					vbt = vwsel.get_BoxType(false, iLevel);
					if (vbt == VwBoxType.kvbtTableCell)
					{
						cBoxes = vwsel.get_BoxCount(true, iLevel);
						iBox = vwsel.get_BoxIndex(true, iLevel);
						break;
					}
				}
				Debug.Assert(cBoxes == 2);
				Debug.Assert(iBox != -1);
				int hvoEntry;
				int hvoSense;
				if (iBox == 0)
				{
					hvoEntry = rgvsli[ihvoRoot].hvo;
					IVwSelection vwsel2 = m_rootb.MakeSelInBox(vwsel, false, iLevel, 1,
						true, false, false);
					SelLevInfo[] rgvsli2 = SelLevInfo.AllTextSelInfo(vwsel, vwsel2.CLevels(false) - 1,
						out ihvoRoot, out tag, out cpropPrevious, out ichAnchor, out ichEnd,
						out ws, out fAssocPrev, out ihvoEnd, out ttp);
					hvoSense = rgvsli2[ihvoRoot].hvo;
				}
				else
				{
					hvoSense = rgvsli[ihvoRoot].hvo;
					IVwSelection vwsel2 = m_rootb.MakeSelInBox(vwsel, false, iLevel, 0,
						true, false, false);
					SelLevInfo[] rgvsli2 = SelLevInfo.AllTextSelInfo(vwsel, vwsel2.CLevels(false) - 1,
						out ihvoRoot, out tag, out cpropPrevious, out ichAnchor, out ichEnd,
						out ws, out fAssocPrev, out ihvoEnd, out ttp);
					hvoEntry = rgvsli2[ihvoRoot].hvo;
				}
#else
				int cvsli = tsi.Levels(false) - 1;
				int tag = tsi.ContainingObjectTag(cvsli - 1);
				Debug.Assert(cvsli >= 1); // there should be at least one level (each row is a sense)
				// The outermost thing in the VC is a display of all the senses of the root domain.
				// Therefore the last thing in rgvsli is always the information identifying the sense we
				// want to process.
				int hvoSense = tsi.ContainingObject(cvsli - 1);
				int hvoEntry = m_fdoCache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoSense).Owner.Hvo;
#endif
				// If this was an editable object, it no longer is, because it's about to no longer exist.
				RDEVc.EditableObjectsRemove(hvoSense);

				var le = m_fdoCache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(hvoEntry);
				var ls = m_fdoCache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(hvoSense);
				string sUndo = XMLViewsStrings.ksUndoDeleteRecord;
				string sRedo = XMLViewsStrings.ksRedoDeleteRecord;
				UndoableUnitOfWorkHelper.Do(sUndo, sRedo, Cache.ActionHandlerAccessor, () =>
				{
					ls.SemanticDomainsRC.Remove(Cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>().GetObject(m_hvoRoot));
					if (ls.SemanticDomainsRC.Count == 0 &&
					ls.AnthroCodesRC.Count == 0 &&
					ls.AppendixesRC.Count == 0 &&
					ls.DomainTypesRC.Count == 0 &&
					ls.ThesaurusItemsRC.Count == 0 &&
					ls.UsageTypesRC.Count == 0)
					{
						bool fKeep = false;
						ITsString tss = ls.Gloss.AnalysisDefaultWritingSystem;
						if (tss != null && tss.Length > 0)
							fKeep = true;
						if (!fKeep)
						{
							tss = ls.Gloss.UserDefaultWritingSystem;
							if (tss != null && tss.Length > 0)
								fKeep = true;
						}
						if (!fKeep)
						{
							tss = ls.Gloss.VernacularDefaultWritingSystem;
							if (tss != null && tss.Length > 0)
								fKeep = true;
						}
						if (!fKeep)
						{
							tss = ls.Definition.VernacularDefaultWritingSystem;
							if (tss != null && tss.Length > 0)
								fKeep = true;
						}
						if (!fKeep)
						{
							tss = ls.DiscourseNote.AnalysisDefaultWritingSystem;
							if (tss != null && tss.Length > 0)
								fKeep = true;
						}
						if (!fKeep)
						{
							tss = ls.DiscourseNote.UserDefaultWritingSystem;
							if (tss != null && tss.Length > 0)
								fKeep = true;
						}
						if (!fKeep)
						{
							tss = ls.DiscourseNote.VernacularDefaultWritingSystem;
							if (tss != null && tss.Length > 0)
								fKeep = true;
						}
						if (!fKeep)
						{
							tss = ls.ScientificName;
							if (tss != null && tss.Length > 0)
								fKeep = true;
						}
						if (!fKeep)
						{
							tss = ls.Source;
							if (tss != null && tss.Length > 0)
								fKeep = true;
						}
						if (!fKeep)
						{
							le.SensesOS.Remove(ls);
							ls = null;
						}
					}
					if (ls == null && le.SensesOS.Count == 0)
					{
						le.Delete();
						le = null;
					}
				});
			}
			return true;
		}

		#endregion Other message handlers

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion
	}
}
