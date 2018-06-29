// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Areas;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel.Application;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary> XML Browse View for Rapid Data Entry (Collect Words) </summary>
	internal class XmlBrowseRDEView : XmlBrowseViewBase, IUndoRedoHandler
	{
		#region Data members

		private System.ComponentModel.IContainer components = null;

		private bool fInDoMerges; // used to ignore recursive calls to DoMerges.

		#endregion Data members

		#region Construction, initialization, and disposal.

		/// <summary>
		/// see XmlBrowseViewBase OnGotFocus
		/// </summary>
		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			SetSelection();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:XmlBrowseRDEView"/> class.
		/// </summary>
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
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				Subscriber.Unsubscribe("areaChoiceParameters", CleanupPendingEdits);
				Subscriber.Unsubscribe("currentContentControlParameters", CleanupPendingEdits);
			}

			base.Dispose( disposing );

			if( disposing )
			{
				components?.Dispose();
			}
		}

		#endregion Construction, initialization, and disposal.

		#region Properties

		/// <summary>
		/// Return the VC. It has some important functions related to interpreting fragment IDs
		/// that the filter bar needs.
		/// </summary>
		internal override XmlBrowseViewBaseVc Vc
		{
			get
			{
				if (m_xbvvc == null)
				{
					m_xbvvc = new XmlRDEBrowseViewVc(m_nodeSpec, MainTag, this);
				}
				return base.Vc;
			}
		}

		/// <summary>
		/// Gets the RDE vc.
		/// </summary>
		protected XmlRDEBrowseViewVc RDEVc => (XmlRDEBrowseViewVc)m_xbvvc;

		/// <summary>
		/// True if we are running the read-only version of the view that is primarily used for
		/// selecting.
		/// </summary>
		protected override bool ReadOnlySelect => false;

		/// <summary>
		/// Overrides the selected row highlighting method so the highlighting will always be none instead of being reliant on
		/// ReadOnlySelect
		/// </summary>
		public override void SetSelectedRowHighlighting()
		{
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
			arg.Cancel = CleanupPendingEdits();  // NB: "CleanupPendingEdits" always returns false.
			return arg.Cancel; // if we want to cancel, others don't need to be asked.
		}

		#region Overrides of XmlBrowseViewBase

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			Subscriber.Subscribe("areaChoiceParameters", CleanupPendingEdits);
			Subscriber.Subscribe("currentContentControlParameters", CleanupPendingEdits);
		}

		#endregion

		/// <summary>
		/// the object that has properties that are shown by this view.
		/// </summary>
		/// <remarks> this will be changed often in the case where this view is dependent on another one;
		/// that is, or some other browse view has a list and each time to selected item changes, our
		/// root object changes.
		/// </remarks>
		public override int RootObjectHvo
		{
			set
			{
				CleanupPendingEdits();
				base.RootObjectHvo = value;
			}
		}

		/// <summary>
		/// In general, we just want to make sure to scroll to the current selection,
		/// or if none exists, make one in the new row and scroll there.
		/// </summary>
		/// <remarks>
		/// Both parameters are ignored.
		/// </remarks>
		protected override void DoSelectAndScroll(int hvo, int index)
		{
			if (m_rootb == null)
				MakeRoot();

			SetSelection();
		}

		private void SetSelection()
		{
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

		private void CleanupPendingEdits(object newValue)
		{
			CleanupPendingEdits();
		}

		/// <summary>
		/// Cleanup any pending edits.
		/// </summary>
		/// <returns>Always returns false.</returns>
		private bool CleanupPendingEdits()
		{
			const bool cancelClose = false;
			ITsString[] rgtss;
			if (CanGotoNextRow(out rgtss))
			{
				// JohnT: if we pass false here, then the following sequence fails (See LT-7140)
				// Use Collect Words to add a sense where the form (but not definition) matches an existing entry.
				// Without hitting enter, click on another view (e.g., main data entry view).
				// Then return to the Collect Words view. The last item added does not appear.
				// However, if we pass true, we get another LT-7140 problem: if we add a row but don't press enter,
				// then switch to another Category, the new item shows up there! That is because a call-back
				// to the record list happens during adding the item, but the record list has already changed its
				// current HVO.
				// The solution is to pass false here, but set a property <RecordListid>_AlwaysReloadVirtualProperty
				// on the tool so we always reload when switching.
				CreateObjectFromEntryRow(rgtss, false);
			}
			DoMerges();

			return cancelClose;
		}

		/// <summary>
		/// Check whether we have enough data entered in this row to go on to create another row.
		/// </summary>
		/// <returns>true if we can proceed to the next row, false otherwise</returns>
		private bool CanGotoNextRow(out ITsString[] rgtss)
		{
			var fCanGotoNextRow = false;
			var columns = m_xbvvc.ColumnSpecs;
			rgtss = GetColumnStringsFromNewRow();
			for (var i = 1; i <= columns.Count; ++i)
			{
				// We must have a citation form, but definitions are optional. (!?)
				// Review: Currently we key off the column labels to determine which columns
				// correspond to CitationForm and which correspond to Definition.
				// Ideally we'd like to get at the flids used to build the column display strings.
				// Instead of passing in only ITsStrings, we could pass in a structure containing
				// an index of strings with any corresponding flids. Here's we'd expect strings
				// based upon either LexemeForm.Form or LexSense.Definition. We could probably
				// do this as part of the solution to handling duplicate columns in LT-3763.
				var column = columns[i - 1];
				var columnLabel = XmlUtils.GetMandatoryAttributeValue(column, "label");
				var columnLabelComponents = columnLabel.Split(new char[] { ' ', ':' });
				// get column label without writing system or extraneous information.
				var columnBasicLabel = LocalizeIfPossible(columnLabelComponents[0]);
				// true if we find any Word column entry of nonzero length.
				if (!fCanGotoNextRow && columnBasicLabel == LocalizeIfPossible("Word") && rgtss[i - 1].Length > 0)
				{
					var s = rgtss[i - 1].Text;
					if (s.Trim().Length > 0)
					{
						fCanGotoNextRow = true;
					}
				}
			}
			return fCanGotoNextRow;
		}

		private ITsString[] GetColumnStringsFromNewRow()
		{
			ISilDataAccess sda = m_bv.SpecialCache;
			var columns = m_xbvvc.ColumnSpecs;
			// Conceptual model class.
			var rgtss = new ITsString[columns.Count];
			for (var i = 1; i <= columns.Count; ++i)
			{
				var kflid = XMLViewsDataCache.ktagEditColumnBase + i;
				var wsCol = WritingSystemServices.GetWritingSystem(m_cache, FwUtils.ConvertElement(columns[i - 1]), null, m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle).Handle;
				// Get the string for each column.
				rgtss[i - 1] = sda.get_MultiStringAlt(XmlRDEBrowseViewVc.khvoNewItem, kflid, wsCol);
			}
			return rgtss;
		}

		/// <summary>
		/// Clears the strings from the new row and puts cursor back in first column.
		/// </summary>
		private void ClearColumnStringsFromNewRow()
		{
			var sda = m_bv.SpecialCache;
			var columns = m_xbvvc.ColumnSpecs;
			// Reset the new item row so that the cells are all empty.
			for (var i = 1; i <= columns.Count; ++i)
			{
				var kflid = XMLViewsDataCache.ktagEditColumnBase + i;
				var wsCol = WritingSystemServices.GetWritingSystem(m_cache, FwUtils.ConvertElement(columns[i - 1]), null, m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle).Handle;
				sda.SetMultiStringAlt(XmlRDEBrowseViewVc.khvoNewItem, kflid, wsCol, TsStringUtils.EmptyString(wsCol));
			}
			// Set the selection to the first column.
			SetSelectionToFirstColumnInNewRow();

		}

		private string LocalizeIfPossible(string sValue)
		{
			return StringTable.Table.LocalizeAttributeValue(sValue);
		}

		/// <summary>
		/// check whether we need to handle the key press, and do so if so.  The keys we handle
		/// are TAB ('\t') and ENTER/RETURN ('\r').
		/// </summary>
		/// <returns>true if the key press has been handled, otherwise false.</returns>
		private bool ProcessRDEKeyPress(char keyPressed)
		{
			var vwsel = m_rootb.Selection;
			if (vwsel == null)
			{
				return false;
			}

			var cvsli = vwsel.CLevels(false) - 1;
			int ihvoRoot;
			int tagTextProp;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ws;
			bool fAssocPrev;
			int ihvoEnd;
			ITsTextProps ttp;
			var rgvsli = SelLevInfo.AllTextSelInfo(vwsel, cvsli,
				out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
				out ws, out fAssocPrev, out ihvoEnd, out ttp);

			var fNewOrEditable = rgvsli[0].hvo == XmlRDEBrowseViewVc.khvoNewItem;
			if (!fNewOrEditable)
			{
				for (var i = 0; i < cvsli; ++i)
				{
					if (!RDEVc.EditableObjectsContains(rgvsli[i].hvo))
					{
						continue;
					}
					fNewOrEditable = true;
					break;
				}
				if (!fNewOrEditable)
				{
					return false;
				}
			}

			var columns = m_xbvvc.ColumnSpecs;
			if (columns == null || columns.Count == 0)
			{
				return false;		// Something is broken!
			}

			var retval = true;
			switch (keyPressed)
			{
				default:
					retval = false;
					break;
				case '\t':
					ScrollToCurrentSelection();
					break;
				case '\r':
					ITsString[] rgtss;
					if (!CanGotoNextRow(out rgtss))
					{
						return true;
					}
					HandleEnterKey(rgtss);
					break;
			}

			return retval;
		}

		/// <summary>
		/// Handle a tab key.
		/// </summary>
		private bool CanAdvanceToNewRow(IVwSelection vwsel, out ITsString[] rgtss)
		{
			rgtss = null;
			int iCellBox;
			int cCellBoxes;
			int iDummyBox;
			int dummyLevel;
			int iDummyTableBox;
			int cDummyTableBoxes;
			int iDummyTableLevel;
			int iDummyCellLevel;
			GetCurrentTableCellInfo(vwsel, out dummyLevel, out iDummyBox, out iDummyTableBox, out cDummyTableBoxes, out iDummyTableLevel, out iCellBox, out cCellBoxes, out iDummyCellLevel);
			if ((ModifierKeys & Keys.Shift) != Keys.Shift)
			{
				bool cellsAllowMove;
				if (m_xbvvc.ShowColumnsRTL)
				{
					// BackTab: move Left-to-Right, Bottom-to-Top
					// Tab: move Right-to-Left, Top-to-Bottom
					cellsAllowMove = (iCellBox - 1) < 0;
				}
				else
				{
					// BackTab: move Right-to-Left, Bottom-to-Top
					// Tab: move Left-to-Right, Top-to-Bottom
					cellsAllowMove = (iCellBox + 1) >= cCellBoxes;
				}
				if (cellsAllowMove && CanGotoNextRow(out rgtss))
				{
					// Treat the same as Enter in the last column.
					return true; // needed for HandleEnterKey.
				}
			}
			return false; // signal we're done.
		}

		/// <summary>
		/// Process an Enter key by creating a new edit row and moving to it.
		/// </summary>
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
			var columns = m_xbvvc.ColumnSpecs;
			const int flidNew = XMLViewsDataCache.ktagEditColumnBase + 1;
			var wsNew = WritingSystemServices.GetWritingSystem(m_cache, FwUtils.ConvertElement(columns[0]), null,
				m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle).Handle;
			try
			{
				var rgvsli = new SelLevInfo[1];
				rgvsli[0].hvo = XmlRDEBrowseViewVc.khvoNewItem;
				rgvsli[0].tag = -1;
				var vwsel = m_rootb.MakeTextSelection(0, rgvsli.Length, rgvsli, flidNew, 0, 0, 0, wsNew, false, -1, null, false);
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
					m_rootb.MakeSelInBox(vwsel, true, iCellLevel, cCellBoxes - 1, true, false, true);
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

		private void ScrollToCurrentSelection()
		{
			if (m_rootb == null)
			{
				MakeRoot();
			}

			if (m_rootb.Selection == null)
			{
				return;
			}
			ScrollSelectionIntoView(m_rootb.Selection, VwScrollSelOpts.kssoDefault);
		}

		#region Implementation of IUndoRedoHandler

		/// <summary>
		/// Get the text for the Undo menu.
		/// </summary>
		public string UndoText => HasNonEmptyNewRow ? XMLViewsStrings.ksUndoNewRowData : LanguageExplorerResources.Undo;

		/// <summary>
		/// Get the enabled condition for the Undo menu.
		/// </summary>
		public bool UndoEnabled(bool callerEnableOpinion)
		{
			return HasNonEmptyNewRow || callerEnableOpinion;
		}

		/// <summary>
		/// Handle Undo event
		/// </summary>
		/// <returns>'true' if the event was handled, ortherwise 'false' which has caller deal with it.</returns>
		public bool HandleUndo(object sender, EventArgs e)
		{
			if (!HasNonEmptyNewRow)
			{
				return false;
			}
			// simply reset the current row.
			ClearColumnStringsFromNewRow();
			return true;
		}

		/// <summary>
		/// Get the text for the Redo menu.
		/// </summary>
		public string RedoText => LanguageExplorerResources.Redo;

		/// <summary>
		/// Get the enabled condition for the Undo menu.
		/// </summary>
		public bool RedoEnabled(bool callerEnableOpinion)
		{
			return callerEnableOpinion; // We are apathetic.
		}

		/// <summary>
		/// Handle Redo event
		/// </summary>
		/// <returns>'true' if the event was handled, ortherwise 'false' which has caller deal with it.</returns>
		public bool HandleRedo(object sender, EventArgs e)
		{
			return false;
		}

		#endregion

		/// <summary>
		/// Return true if we have setup a new row that has data which has not yet been
		/// committed to the database.
		/// </summary>
		private bool HasNonEmptyNewRow
		{
			get
			{
				return GetColumnStringsFromNewRow().Any(tss => tss != null && tss.Length > 0);
			}
		}

		/// <summary>
		/// Create an undo/redo action for updating the display after the user commits changes in a new row.
		/// </summary>
		private sealed class CreateObjectFromEntryRowUndoAction : UndoActionBase
		{
			XmlBrowseRDEView m_xbrdev;
			int m_newObjHvo;
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
		private int CreateObjectFromEntryRow(ITsString[] rgtss, bool fAddItems)
		{
			var newObjHvo = 0;
			// Conceptual model class.
			var sUndo = string.Format(XMLViewsStrings.ksUndoNewEntryX, rgtss[0].Text);
			var sRedo = string.Format(XMLViewsStrings.ksRedoNewEntryX, rgtss[0].Text);
			try
			{
				UndoableUnitOfWorkHelper.Do(sUndo, sRedo, Cache.ActionHandlerAccessor, () =>
				{
					newObjHvo = CreateObjectFromEntryRow(rgtss);
					var undoRedoAction = new CreateObjectFromEntryRowUndoAction(this, newObjHvo, fAddItems);
					Cache.ActionHandlerAccessor.AddAction(undoRedoAction);
					undoRedoAction.DoIt();
				});
			}
			catch (Exception error)
			{
				throw new RuntimeConfigurationException($"XmlBrowseRDEView.ProcessRDEKeyPress() could not invoke the static {RDEVc.EditRowSaveMethod} method of the class {RDEVc.EditRowClass}", error);
			}
			return newObjHvo;
		}

		private int CreateObjectFromEntryRow(ITsString[] rgtss)
		{
			var columns = m_xbvvc.ColumnSpecs;

			// Enhance JohnT: here we assume the class that creates instances is the class created plus "Factory".
			// We may want to make it a separate attribute of the configuration XML at some point.
			var factoryClassName = RDEVc.EditRowClass + "Factory";
			var factoryType = ReflectionHelper.GetType(RDEVc.EditRowAssembly, factoryClassName);
			var factory = Cache.ServiceLocator.GetService(factoryType);
			var mi = factoryType.GetMethod(RDEVc.EditRowSaveMethod);
			var parameters = new object[3];
			parameters[0] = m_hvoRoot;
			parameters[1] = columns;
			parameters[2] = rgtss;
			var newObjHvo = (int)mi.Invoke(factory, parameters);
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
		/// public static void RDEMergeSense(int hvoDomain, Set(int) newHvos)
		/// </summary>
		public void DoMerges()
		{
			// Check for recursive calls, which can happen when deleting duplicate entries
			// generates a PropChanged which CleanupPendingEdits handles by calling us again.
			if (fInDoMerges)
			{
				return;
			}

			try
			{
				try
				{
					RDEVc.EditableObjectsRemoveInvalidObjects();
					var idsClone = RDEVc.EditableObjectsClone();
					fInDoMerges = true;
					var targetType = ReflectionHelper.GetType(RDEVc.EditRowAssembly, RDEVc.EditRowClass);
					var mi = targetType.GetMethod(RDEVc.EditRowMergeMethod);
					var parameters = new object[2];
					parameters[0] = m_hvoRoot;
					parameters[1] = idsClone; // This is a Set<int>.

					// Make a copy. I (JohnT) don't see how this collection can get modified
					// during the loop, but we've had exceptions (e.g., LT-1355) claiming that
					// it has been.
					foreach (var hvoSense in idsClone)
					{
						var target = Cache.ServiceLocator.GetObject(hvoSense);
						try
						{
							if ((bool)mi.Invoke(target, parameters))
							{
								// The sense was deleted as a duplicate; get rid of it from our madeUpFieldIdentifier, too.
								ISilDataAccessManaged sda = m_bv.SpecialCache;
								var oldList = sda.VecProp(m_hvoRoot, MainTag);
								for (var i = 0; i < oldList.Length; i++)
								{
									if (oldList[i] == hvoSense)
									{
										m_bv.SpecialCache.Replace(m_hvoRoot, MainTag, i, i+1, new int[0], 0);
									}
								}
							}
						}
						catch (Exception e)
						{
							// Ignore a failure here - LT-3091 (after adding a word and def in cat entry,
							// selecting undo and then closing the application causes the following error:
							//     Msg: Tried to create an LCM object based on db object(hvo=39434class=0),
							//     but that class is not fit in this signature (LexSense)
							Debug.WriteLine("mi.Invoke failed in XmlBrowseRDEView: " + e.InnerException.Message);
							throw;
						}
					}
				}
				catch (Exception error)
				{
					throw new RuntimeConfigurationException($"XmlBrowseRDEView.DoMerges() could not invoke the static {RDEVc.EditRowMergeMethod} method of the class {RDEVc.EditRowClass}", error);
				}
				RDEVc.EditableObjectsClear();
				var cobj = m_bv.SpecialCache.get_VecSize(m_hvoRoot, MainTag);
				m_bv.BrowseView.RootBox.PropChanged(m_hvoRoot, MainTag, 0, cobj, cobj);
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
		protected override bool OnRightMouseUp(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			var sel = MakeSelectionAt(new MouseEventArgs(MouseButtons.Right, 1, pt.X, pt.Y, 0));
			if (sel == null)
			{
				return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
			}
			var index = GetRowIndexFromSelection(sel, false);
			if (index < 0)
			{
				return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
			}
			var hvo = SpecialCache.get_VecItem(m_hvoRoot, MainTag, index);
			// It would sometimes work to jump to one of the editable objects.
			// But sometimes one of them will get destroyed when we switch away from this view
			// (e.g., because there is already a similar sense to which we can just add this semantic domain).
			// Then we would crash trying to jump to it.
			if (RDEVc.EditableObjectsContains(hvo))
			{
				return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
			}
			ICmObject target;
			if (!Cache.ServiceLocator.ObjectRepository.TryGetObject(hvo, out target) || !(target is ILexSense))
			{
				return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
			}
			// We have a valid (real, not temporary fake) LexEntry and will put up the context menu
			var menu = new ContextMenuStrip();
			var item = new ToolStripMenuItem(XMLViewsStrings.ksShowEntryInLexicon);
			menu.Items.Add(item);
			item.Click += (sender, args) => LinkHandler.PublishFollowLinkMessage(Publisher, new FwLinkArgs(AreaServices.LexiconEditMachineName, target.Guid));
			menu.Show(this, pt);
			return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
		}

		/// <summary>
		/// Handles OnKeyDown.
		/// </summary>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			// try to calculate whether we will be advancing to the next row
			// BEFORE we call OnKeyDown, because it will change the selection.
			var vwsel = RootBox.Selection;
			var fHandleEnterKey = false;
			ITsString[] rgtss = null;
			if (e.KeyCode == Keys.Tab && vwsel != null)
			{
				fHandleEnterKey = CanAdvanceToNewRow(vwsel, out rgtss);
			}
			base.OnKeyDown(e);
			if (fHandleEnterKey)
			{
				HandleEnterKey(rgtss);
			}
		}

		/// <summary>
		/// Handles OnKeyPress.  Passes most things to EditingHelper.OnKeyPress
		/// </summary>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (!ProcessRDEKeyPress(e.KeyChar))
			{
				base.OnKeyPress(e);
			}
		}
		/// <summary>
		///	Let caller know if the Delete menu can be enabled or not.
		/// And, come up with the new menu text that is displayed, whether enabled or not.
		/// </summary>
		public bool SetupDeleteMenu(string deleteTextBase, out string deleteText)
		{
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
			deleteText = string.Format(deleteTextBase, XMLViewsStrings.ksRow);
			return fCanDelete;
		}

		/// <summary>
		/// Called when [delete record].
		/// </summary>
		public void DeleteRecord()
		{
			if (m_rootb == null)
			{
				MakeRoot();
			}

			var vwsel = m_rootb.Selection;
			if (vwsel == null)
			{
				return;
			}
			var columns = m_xbvvc.ColumnSpecs;
			if (columns == null || columns.Count == 0)
			{
				return; // Something is broken!
			}

			var tsi = new TextSelInfo(m_rootb.Selection);
			if (tsi.ContainingObject(0) == XmlRDEBrowseViewVc.khvoNewItem)
			{
				ClearColumnStringsFromNewRow();
			}
			else
			{
				// 1. Remove the domain from the sense shown in the second column.
				// 2. Delete the sense iff it is now empty except for the definition shown.
				// 3. Delete the entry iff the entry now has no senses.
				var cvsli = tsi.Levels(false) - 1;
				Debug.Assert(cvsli >= 1); // there should be at least one level (each row is a sense)
				// The outermost thing in the VC is a display of all the senses of the root domain.
				// Therefore the last thing in rgvsli is always the information identifying the sense we
				// want to process.
				var hvoSense = tsi.ContainingObject(cvsli - 1);
				var hvoEntry = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoSense).Owner.Hvo;
				// If this was an editable object, it no longer is, because it's about to no longer exist.
				RDEVc.EditableObjectsRemove(hvoSense);

				var le = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(hvoEntry);
				var ls = m_cache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(hvoSense);
				var sUndo = XMLViewsStrings.ksUndoDeleteRecord;
				var sRedo = XMLViewsStrings.ksRedoDeleteRecord;
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
						var fKeep = false;
						var tss = ls.Gloss.AnalysisDefaultWritingSystem;
						if (tss != null && tss.Length > 0)
						{
							fKeep = true;
						}
						if (!fKeep)
						{
							tss = ls.Gloss.UserDefaultWritingSystem;
							if (tss != null && tss.Length > 0)
							{
								fKeep = true;
							}
						}
						if (!fKeep)
						{
							tss = ls.Gloss.VernacularDefaultWritingSystem;
							if (tss != null && tss.Length > 0)
							{
								fKeep = true;
							}
						}
						if (!fKeep)
						{
							tss = ls.Definition.VernacularDefaultWritingSystem;
							if (tss != null && tss.Length > 0)
							{
								fKeep = true;
							}
						}
						if (!fKeep)
						{
							tss = ls.DiscourseNote.AnalysisDefaultWritingSystem;
							if (tss != null && tss.Length > 0)
							{
								fKeep = true;
							}
						}
						if (!fKeep)
						{
							tss = ls.DiscourseNote.UserDefaultWritingSystem;
							if (tss != null && tss.Length > 0)
							{
								fKeep = true;
							}
						}
						if (!fKeep)
						{
							tss = ls.DiscourseNote.VernacularDefaultWritingSystem;
							if (tss != null && tss.Length > 0)
							{
								fKeep = true;
							}
						}
						if (!fKeep)
						{
							tss = ls.ScientificName;
							if (tss != null && tss.Length > 0)
							{
								fKeep = true;
							}
						}
						if (!fKeep)
						{
							tss = ls.Source;
							if (tss != null && tss.Length > 0)
							{
								fKeep = true;
							}
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
