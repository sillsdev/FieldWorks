using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using SIL.CoreImpl;
using SIL.CoreImpl.MessageBoxEx;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.FdoUi.Dialogs;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.LexText.Controls;

namespace SIL.FieldWorks.XWorks.LexEd
{
	public interface ILexReferenceSlice
	{
		Slice ParentSlice { get; set; }
#if RANDYTODO
		bool HandleDeleteCommand(Command cmd);
#endif
		void HandleLaunchChooser();
		void HandleEditCommand();
	}

	/// <summary>
	/// Summary description for LexReferenceMultiSlice.
	/// </summary>
	public class LexReferenceMultiSlice : Slice
	{
		private List<ILexReference> m_refs;
		private List<ILexRefType> m_refTypesAvailable = new List<ILexRefType>();
		private List<bool> m_rgfReversedRefType = new List<bool>();

		#region IDisposable override

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
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_refs != null)
					m_refs.Clear();
				if (m_refTypesAvailable != null)
					m_refTypesAvailable.Clear();
				if (m_rgfReversedRefType != null)
					m_rgfReversedRefType.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_refs = null;
			m_refTypesAvailable = null;
			m_rgfReversedRefType = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// <summary>
		/// Override method to add suitable control.
		/// </summary>
		public override void FinishInit()
		{
			CheckDisposed();
			Debug.Assert(m_cache != null);
			Debug.Assert(m_configurationNode != null);

			base.FinishInit();
		}

		void SetRefs()
		{
			var fieldName = XmlUtils.GetManditoryAttributeValue(m_configurationNode, "field");
			var refs = ReflectionHelper.GetProperty(m_obj, fieldName);
			var refsInts = refs as IEnumerable<int>;
			if (refsInts != null)
			{
				m_refs = (from hvo in refsInts
						  select m_cache.ServiceLocator.GetInstance<ILexReferenceRepository>().GetObject(hvo)).ToList();
			}
			else
			{
				m_refs = new List<ILexReference>();
				IEnumerable refsObjs = refs as IEnumerable;
				if (refsObjs != null)
					m_refs.AddRange(refsObjs.Cast<ILexReference>());
				else
					Debug.Fail("LexReferenceSlice could not interpret results from " + fieldName);
			}
			var flid = Cache.MetaDataCacheAccessor.GetFieldId2(m_obj.ClassID, fieldName, true);
			ContainingDataTree.MonitorProp(m_obj.Hvo, flid);
		}

		public override void GenerateChildren(XmlNode node, XmlNode caller, ICmObject obj, int indent,
			ref int insPos, ArrayList path, ObjSeqHashMap reuseMap, bool fUsePersistentExpansion)
		{
			CheckDisposed();
			// If node has children, figure what to do with them...

			// It's important to initialize m_refs here rather than in FinishInit, because we need it
			// to be updated when the slice is reused in a regenerate.
			// Refactor JohnT: better still, make it a virtual attribute, and Refresh will automatically
			// clear it from the cache.

			SetRefs();

			if (m_refs.Count == 0)
			{
				// It could have children but currently can't: we always show this as collapsedEmpty.
				Expansion = DataTree.TreeItemState.ktisCollapsedEmpty;
				return;
			}

			for (int i = 0; i < m_refs.Count; i++)
			{
				GenerateChildNode(i, node, caller, indent, ref insPos, path, reuseMap);
			}

			Expansion = DataTree.TreeItemState.ktisExpanded;
		}

		private void GenerateChildNode(int iChild, XmlNode node, XmlNode caller, int indent,
			ref int insPos, ArrayList path, ObjSeqHashMap reuseMap)
		{
			var lr = m_refs[iChild];
			var lrt = lr.Owner as ILexRefType;
			string sLabel = lrt.ShortName;
			if (sLabel == null || sLabel == string.Empty)
				sLabel = lrt.Abbreviation.BestAnalysisAlternative.Text;
			bool fTreeRoot = true;
			ISilDataAccess sda = m_cache.DomainDataByFlid;
			int chvoTargets = sda.get_VecSize(lr.Hvo, LexReferenceTags.kflidTargets);
			// change the label for a Tree relationship.
			switch ((LexRefTypeTags.MappingTypes)lrt.MappingType)
			{
				case LexRefTypeTags.MappingTypes.kmtSenseTree:
				case LexRefTypeTags.MappingTypes.kmtEntryTree:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
				case LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair: // Sense Pair with different Forward/Reverse names
				case LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair: // Entry Pair with different Forward/Reverse names
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair: // Entry or sense Pair with different Forward/Reverse names
					//int chvo = sda.get_VecSize(lr.Hvo, LexReferenceTags.kflidTargets);
					if (chvoTargets > 0)
					{
						int hvoFirst = sda.get_VecItem(lr.Hvo, LexReferenceTags.kflidTargets, 0);
						if (hvoFirst != m_obj.Hvo)
						{
							sLabel = lrt.ReverseName.BestAnalysisAlternative.Text;
							if (sLabel == null || sLabel == string.Empty)
								sLabel = lrt.ReverseAbbreviation.BestAnalysisAlternative.Text;
							fTreeRoot = false;
						}
					}
					break;
			}

			if (sLabel == null || sLabel == string.Empty)
				sLabel = LexEdStrings.ksStars;
			string sXml = "<slice label=\"" + sLabel + "\" field=\"Targets\"" +
				" editor=\"Custom\" assemblyPath=\"LexEdDll.dll\"";
			//string sMenu = "mnuDataTree-DeleteFromLexSenseReference"; we used to have distinct strings in the menu
			string sMenu = "mnuDataTree-DeleteAddLexReference";

			// generate Xml for a specific slice matching this reference
			switch ((LexRefTypeTags.MappingTypes)lrt.MappingType)
			{
				case LexRefTypeTags.MappingTypes.kmtSenseCollection:
					sXml +=	" class=\"SIL.FieldWorks.XWorks.LexEd.LexReferenceCollectionSlice\"";
					break;
				case LexRefTypeTags.MappingTypes.kmtSensePair:
				case LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair: // Sense Pair with different Forward/Reverse names
				case LexRefTypeTags.MappingTypes.kmtEntryPair:
				case LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair: // Entry Pair with different Forward/Reverse names
				case LexRefTypeTags.MappingTypes.kmtEntryOrSensePair:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair: // Entry or sense Pair with different forward/Reverse names
					sXml +=	" class=\"SIL.FieldWorks.XWorks.LexEd.LexReferencePairSlice\"";
					sMenu = "mnuDataTree-DeleteReplaceLexReference";
					break;
				case LexRefTypeTags.MappingTypes.kmtSenseTree:
					if (fTreeRoot)
					{
						sXml +=	" class=\"SIL.FieldWorks.XWorks.LexEd.LexReferenceTreeBranchesSlice\"";
						sMenu = "mnuDataTree-DeleteAddLexReference";
					}
					else
					{
						sXml +=	" class=\"SIL.FieldWorks.XWorks.LexEd.LexReferenceTreeRootSlice\"";
						sMenu = "mnuDataTree-DeleteReplaceLexReference";
					}
					break;
				case LexRefTypeTags.MappingTypes.kmtSenseSequence:
				case LexRefTypeTags.MappingTypes.kmtEntrySequence:
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence:
					sXml +=	" class=\"SIL.FieldWorks.XWorks.LexEd.LexReferenceSequenceSlice\"";
					break;
				case LexRefTypeTags.MappingTypes.kmtEntryCollection:
					sXml +=	" class=\"SIL.FieldWorks.XWorks.LexEd.LexReferenceCollectionSlice\"";
					//sMenu = "mnuDataTree-DeleteFromLexEntryReference"; we used to have distinct strings in the menu
					sMenu = "mnuDataTree-DeleteAddLexReference";
					break;
				case LexRefTypeTags.MappingTypes.kmtEntryTree:
					//sMenu = "mnuDataTree-DeleteFromLexEntryReference"; we used to have distinct strings in the menu
					sMenu = "mnuDataTree-DeleteAddLexReference";
					if (fTreeRoot)
					{
						sXml +=	" class=\"SIL.FieldWorks.XWorks.LexEd.LexReferenceTreeBranchesSlice\"";
						sMenu = "mnuDataTree-DeleteAddLexReference";
					}
					else
					{
						sXml +=	" class=\"SIL.FieldWorks.XWorks.LexEd.LexReferenceTreeRootSlice\"";
						sMenu = "mnuDataTree-DeleteReplaceLexReference";
					}
					break;
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection:
					sXml +=	" class=\"SIL.FieldWorks.XWorks.LexEd.LexReferenceCollectionSlice\"";
					if (m_obj is ILexEntry)
						//sMenu = "mnuDataTree-DeleteFromLexEntryReference"; we used to have distinct strings in the menu
						sMenu = "mnuDataTree-DeleteAddLexReference";
					break;
				case LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
					if (m_obj is ILexEntry)
						//sMenu = "mnuDataTree-DeleteFromLexEntryReference"; we used to have distinct strings in the menu
						sMenu = "mnuDataTree-DeleteAddLexReference";
					if (fTreeRoot)
					{
						sXml +=	" class=\"SIL.FieldWorks.XWorks.LexEd.LexReferenceTreeBranchesSlice\"";
						sMenu = "mnuDataTree-DeleteAddLexReference";
					}
					else
					{
						sXml +=	" class=\"SIL.FieldWorks.XWorks.LexEd.LexReferenceTreeRootSlice\"";
					}
					break;

			}

			sXml += " mappingType=\"" + lrt.MappingType + "\" hvoDisplayParent=\"" + m_obj.Hvo + "\"" +
				" menu=\"" + sMenu + "\"><deParams displayProperty=\"HeadWord\"/></slice>";
			node.InnerXml = sXml;
			int firstNewSliceIndex = insPos;
			CreateIndentedNodes(caller, lr, indent, ref insPos, path, reuseMap, node);
			for (int islice = firstNewSliceIndex; islice < insPos; islice++)
			{
				Slice child = ContainingDataTree.Slices[islice] as Slice;
				if (child is ILexReferenceSlice)
				{
					(child as ILexReferenceSlice).ParentSlice = this;
				}
			}
			node.InnerXml = "";
		}

		private ContextMenuStrip m_contextMenuStrip;
		public override bool HandleMouseDown(Point p)
		{
			CheckDisposed();
			DisposeContextMenu(this, new EventArgs());
			m_contextMenuStrip = SetupContextMenuStrip();
			m_contextMenuStrip.Closed += contextMenuStrip_Closed; // dispose when no longer needed (but not sooner! needed after this returns)
			if (m_contextMenuStrip.Items.Count > 0)
				m_contextMenuStrip.Show(TreeNode, p);
			return true;
		}

		void contextMenuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			// It's apparently still needed by the menu handling code in .NET.
			// So we can't dispose it yet.
			// But we want to eventually (Eberhard says if it has a Dispose we MUST call it to make Mono happy)
			Application.Idle += DisposeContextMenu;
		}

		void DisposeContextMenu(object sender, EventArgs e)
		{
			Application.Idle -= DisposeContextMenu;
			if (m_contextMenuStrip != null && !m_contextMenuStrip.IsDisposed)
			{
				m_contextMenuStrip.Dispose();
				m_contextMenuStrip = null;
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "ToolStripMenuItems are added to menu and disposed there")]
		protected ContextMenuStrip SetupContextMenuStrip()
		{
			ContextMenuStrip contextMenuStrip = new System.Windows.Forms.ContextMenuStrip();

			m_refTypesAvailable.Clear();
			m_rgfReversedRefType.Clear();
			string formatName = StringTable.Table.GetString("InsertSymmetricReference", "LexiconTools");
			string formatNameWithReverse = StringTable.Table.GetString("InsertAsymmetricReference", "LexiconTools");
			foreach (var lrt in m_cache.LanguageProject.LexDbOA.ReferencesOA.PossibilitiesOS.Cast<ILexRefType>())
			{
				if (m_obj is ILexEntry)
				{
					switch ((LexRefTypeTags.MappingTypes)lrt.MappingType)
					{
						case LexRefTypeTags.MappingTypes.kmtSenseCollection:
						case LexRefTypeTags.MappingTypes.kmtSensePair:
						case LexRefTypeTags.MappingTypes.kmtSenseTree:
						case LexRefTypeTags.MappingTypes.kmtSenseSequence:
						case LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair:
							continue;
						default:
							break;
					}
				}
				else
				{
					switch ((LexRefTypeTags.MappingTypes)lrt.MappingType)
					{
						case LexRefTypeTags.MappingTypes.kmtEntryCollection:
						case LexRefTypeTags.MappingTypes.kmtEntryPair:
						case LexRefTypeTags.MappingTypes.kmtEntryTree:
						case LexRefTypeTags.MappingTypes.kmtEntrySequence:
						case LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair:
							continue;
						default:
							break;
					}
				}
				string label = "";
				string label2 = "";
				// was: string reverseName = ILexRefType.BestAnalysisOrVernReverseName(lrt.Cache, lrt.Hvo).Text; // replaces lrt.ReverseName.AnalysisDefaultWritingSystem;
				// ToDo JohnT: find a way to extend to include reversal writing systems.
				string reverseName = lrt.ReverseName.BestAnalysisVernacularAlternative.Text;
				if (reverseName == null || reverseName == string.Empty)
					reverseName = LexEdStrings.ksStars;
				string name = lrt.ShortName;
				if (name == null || name == string.Empty)
					name = LexEdStrings.ksStars;
				switch ((LexRefTypeTags.MappingTypes)lrt.MappingType)
				{
					case LexRefTypeTags.MappingTypes.kmtSenseCollection:
					case LexRefTypeTags.MappingTypes.kmtSensePair:
					case LexRefTypeTags.MappingTypes.kmtSenseSequence:
					case LexRefTypeTags.MappingTypes.kmtEntryCollection:
					case LexRefTypeTags.MappingTypes.kmtEntryPair:
					case LexRefTypeTags.MappingTypes.kmtEntrySequence:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSensePair:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence:
						label = string.Format(formatName, name);
						break;
					case LexRefTypeTags.MappingTypes.kmtSenseTree:
					case LexRefTypeTags.MappingTypes.kmtEntryTree:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
					case LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair:
					case LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair:
						label = string.Format(formatNameWithReverse, name, reverseName);
						label2 = string.Format(formatNameWithReverse, reverseName, name);
						break;
				}

				int iInsert = contextMenuStrip.Items.Count;
				//This block of code commented out was set up to sort the items alphabetically.
				// Find the index to insert the menu items in sorted order.  This is a simple
				// linear search of the existing items in the menu, which could be improved to
				// a binary search if this is a slowdown.  I don't expect that to happen for
				// expected number of different lexical relation types.
				//for (int idx = 0; idx < contextMenuStrip.Items.Count; ++idx)
				//{
				//    if (String.Compare(label, contextMenuStrip.Items[idx].Text) < 0)
				//    {
				//        iInsert = idx;
				//        break;
				//    }
				//}
				//   We could use the following method for inputing items if we want to go back to having them sorted.
				//   This would also require making sure MergeIndex has the correct value when referenced in
				//   HandleCreateMenuItem()
				//contextMenuStrip.Items.Insert(iInsert, new ToolStripMenuItem(label2, null, new EventHandler(this.HandleCreateMenuItem)));

				contextMenuStrip.Items.Add(new ToolStripMenuItem(label, null, new EventHandler(this.HandleCreateMenuItem)));
				m_refTypesAvailable.Insert(iInsert, lrt);
				m_rgfReversedRefType.Insert(iInsert, false);
				if (label2.Length > 0)
				{
					iInsert = contextMenuStrip.Items.Count;
					//This block of code commented out was set up to sort the items alphabetically
					//for (int idx = 0; idx < contextMenuStrip.Items.Count; ++idx)
					//{
					//    if (String.Compare(label2, contextMenuStrip.Items[idx].Text) < 0)
					//    {
					//        iInsert = idx;
					//        break;
					//    }
					//}
					//   We could use the following method for inputing items if we want to go back to having them sorted.
					//   This would also require making sure MergeIndex has the correct value when referenced in
					//   HandleCreateMenuItem()
					//contextMenuStrip.Items.Insert(iInsert, new ToolStripMenuItem(label2, null, new EventHandler(this.HandleCreateMenuItem)));
					contextMenuStrip.Items.Add(new ToolStripMenuItem(label2, null, new EventHandler(this.HandleCreateMenuItem)));
					m_refTypesAvailable.Insert(iInsert, lrt);
					m_rgfReversedRefType.Insert(iInsert, true);
				}
			}

			AddFinalContextMenuStripOptions(contextMenuStrip);
			return contextMenuStrip;
		}

		private void AddFinalContextMenuStripOptions(ContextMenuStrip contextMenuStrip)
		{
			contextMenuStrip.Items.Add(new ToolStripSeparator());
			contextMenuStrip.Items.Add(new ToolStripMenuItem(LexEdStrings.ksCreateLexRefType_, null, new EventHandler(this.HandleMoreMenuItem)));

			ToolStripDropDownMenu tsdropdown = new ToolStripDropDownMenu();
			ToolStripMenuItem itemAlways = new ToolStripMenuItem(LexEdStrings.ksAlwaysVisible, null, new EventHandler(this.OnShowFieldAlwaysVisible1));
			ToolStripMenuItem itemIfData = new ToolStripMenuItem(LexEdStrings.ksHiddenUnlessData, null, new EventHandler(this.OnShowFieldIfData1));
			ToolStripMenuItem itemHidden = new ToolStripMenuItem(LexEdStrings.ksNormallyHidden, null, new EventHandler(this.OnShowFieldNormallyHidden1));
			itemAlways.CheckOnClick = true;
			itemIfData.CheckOnClick = true;
			itemHidden.CheckOnClick = true;
			itemAlways.Checked = IsVisibilityItemChecked("always");
			itemIfData.Checked = IsVisibilityItemChecked("ifdata");
			itemHidden.Checked = IsVisibilityItemChecked("never");

			tsdropdown.Items.Add(itemAlways);
			tsdropdown.Items.Add(itemIfData);
			tsdropdown.Items.Add(itemHidden);
			ToolStripMenuItem fieldVis = new ToolStripMenuItem(LexEdStrings.ksFieldVisibility);
			fieldVis.DropDown = tsdropdown;

			contextMenuStrip.Items.Add(new ToolStripSeparator());
			contextMenuStrip.Items.Add(fieldVis);
#if RANDYTODO
			Image imgHelp = ContainingDataTree.SmallImages.GetImage("Help");
			contextMenuStrip.Items.Add(new ToolStripMenuItem(LexEdStrings.ksHelp, imgHelp, this.OnHelp));
#endif
		}

		public void OnShowFieldAlwaysVisible1(object sender, EventArgs args)
		{
			CheckDisposed();
			SetFieldVisibility("always");
		}
		public void OnShowFieldIfData1(object sender, EventArgs args)
		{
			CheckDisposed();
			SetFieldVisibility("ifdata");
		}
		public void OnShowFieldNormallyHidden1(object sender, EventArgs args)
		{
			CheckDisposed();
			SetFieldVisibility("never");
		}

		public void OnHelp(object sender, EventArgs args)
		{
			CheckDisposed();
			if(HelpId == "LexSenseReferences")
				ShowHelp.ShowHelpTopic(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), "khtpFieldLexSenseLexicalRelations");
			else if(HelpId == "LexEntryReferences")
				ShowHelp.ShowHelpTopic(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), "khtpFieldLexEntryCrossReference");
			else
				Debug.Assert(false, "Tried to show help for a LexReferenceMultiSlice that does not have an associated Help Topic ID");
		}

		/// <summary>
		/// Updates the display of a slice, if an hvo and tag it cares about has changed in some way.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns>true, if it the slice updated its display</returns>
		protected override bool UpdateDisplayIfNeeded(int hvo, int tag)
		{
			CheckDisposed();

			// Can't check hvo since it may have been deleted by an undo operation already.
			if (tag == LexRefTypeTags.kflidMembers)
			{
				// if this flickers too annoyingly, we can probably optimize by extracting relevant lines from Collapse.
				Collapse();
				Expand();
				return true;
			}
			else
			{
				return false;
			}
		}

		public void HandleCreateMenuItem(object sender, EventArgs ea)
		{
			CheckDisposed();
			var tsItem = sender as ToolStripItem;
			int itemIndex = (tsItem.Owner as ContextMenuStrip).Items.IndexOf(tsItem);
			var lrt = m_refTypesAvailable[itemIndex];
			bool fReverseRef = m_rgfReversedRefType[itemIndex];
			ILexReference newRef = null;
			ICmObject first = null;
			if (fReverseRef)
			{
				// When creating a tree Lexical Relation and the user is choosing
				// the root of the tree, first see if the user selects a lexical entry.
				// If they do not select anything (hvoFirst==0) return and do not create the slice.
				first = GetRootObject(lrt);
				if (first == null)
					return;		// the user cancelled out of the operation.
				if (lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtSenseTree ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryTree ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree)
				{
					// Use an existing ILexReference if one exists.
					foreach (var lr in lrt.MembersOC)
					{
						if (lr.TargetsRS.Count > 0 && lr.TargetsRS[0] == first)
						{
							newRef = lr;
							break;
						}
					}
				}
			}
			else
			{
				// Launch the dialog that allows the user to choose a lexical entry.
				// If they choose an entry, it is returned in hvoFirst so go ahead and
				// create the lexical relation and add this lexical entry to that relation.
				first = GetChildObject(lrt);
				if (first == null)
					return;		// the user cancelled out of the operation.
			}

			UndoableUnitOfWorkHelper.Do(string.Format(LexEdStrings.ksUndoInsertRelation, tsItem.Text),
				string.Format(LexEdStrings.ksRedoInsertRelation, tsItem.Text), m_obj, () =>
			{
				if (newRef != null)
				{
					newRef.TargetsRS.Add(m_obj);
				}
				else
				{
					newRef = m_cache.ServiceLocator.GetInstance<ILexReferenceFactory>().Create();
					lrt.MembersOC.Add(newRef);
					if (fReverseRef)
					{
						newRef.TargetsRS.Insert(0, first);
						newRef.TargetsRS.Insert(1, m_obj);
					}
					else
					{
						//When creating a lexical relation slice,
						//add the current lexical entry to the lexical relation as the first item
						newRef.TargetsRS.Insert(0, m_obj);
						//then also add the lexical entry that the user selected in the chooser dialog.
						newRef.TargetsRS.Insert(1, first);
					}
				}
				m_refs.Add(newRef);
			});
		}

		/// <summary>
		/// This method is called when we are creating a new lexical relation slice.
		/// If the user selects an item it's hvo is returned.
		/// Otherwise 0 is returned and the lexical relation should not be created.
		/// </summary>
		/// <param name="lrt"></param>
		/// <returns></returns>
		private ICmObject GetRootObject(ILexRefType lrt)
		{
			ICmObject first = null;
			EntryGoDlg dlg = null;
			try
			{
				switch ((LexRefTypeTags.MappingTypes)lrt.MappingType)
				{
					case LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair:
					case LexRefTypeTags.MappingTypes.kmtSenseTree:
						dlg = new LinkEntryOrSenseDlg();
						(dlg as LinkEntryOrSenseDlg).SelectSensesOnly = true;
						break;
					case LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair:
					case LexRefTypeTags.MappingTypes.kmtEntryTree:
						dlg = new EntryGoDlg();
						break;
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
						dlg = new LinkEntryOrSenseDlg();
						break;
					default:
						Debug.Assert(lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair || lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtSenseTree);
						return null;
				}
				Debug.Assert(dlg != null);
				var wp = new WindowParams { m_title = String.Format(LexEdStrings.ksIdentifyXEntry, lrt.ReverseName.BestAnalysisAlternative.Text), m_btnText = LexEdStrings.ks_Add };
				dlg.SetDlgInfo(m_cache, wp, PropertyTable, Publisher);
				dlg.SetHelpTopic("khtpChooseLexicalRelationAdd");
				if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
					first = dlg.SelectedObject;
				return first;
			}
			finally
			{
				if (dlg != null)
					dlg.Dispose();
			}
		}

		/// <summary>
		/// This method is called when we are creating a new lexical relation slice.
		/// If the user selects an item it's hvo is returned.
		/// Otherwise 0 is returned and the lexical relation should not be created.
		/// </summary>
		/// <param name="lrt"></param>
		/// <returns></returns>
		private ICmObject GetChildObject(ILexRefType lrt)
		{
			ICmObject first = null;
			EntryGoDlg dlg = null;
			string sTitle = string.Empty;
			try
			{
				switch ((LexRefTypeTags.MappingTypes)lrt.MappingType)
				{
					case LexRefTypeTags.MappingTypes.kmtEntryOrSensePair:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair:
						// Entry or sense pair with different Forward/Reverse
						dlg = new LinkEntryOrSenseDlg();
						(dlg as LinkEntryOrSenseDlg).SelectSensesOnly = false;
						sTitle = String.Format(LexEdStrings.ksIdentifyXLexEntryOrSense, lrt.Name.BestAnalysisAlternative.Text);
						break;

					case LexRefTypeTags.MappingTypes.kmtSenseCollection:
					case LexRefTypeTags.MappingTypes.kmtSensePair:
					case LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair:
					// Sense pair with different Forward/Reverse names
					case LexRefTypeTags.MappingTypes.kmtSenseSequence:
					case LexRefTypeTags.MappingTypes.kmtSenseTree:
						dlg = new LinkEntryOrSenseDlg();
						(dlg as LinkEntryOrSenseDlg).SelectSensesOnly = true;
						sTitle = String.Format(LexEdStrings.ksIdentifyXSense, lrt.Name.BestAnalysisAlternative.Text);
						break;

					case LexRefTypeTags.MappingTypes.kmtEntryCollection:
					case LexRefTypeTags.MappingTypes.kmtEntryPair:
					case LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair:
					// Entry pair with different Forward/Reverse names
					case LexRefTypeTags.MappingTypes.kmtEntrySequence:
					case LexRefTypeTags.MappingTypes.kmtEntryTree:
						dlg = new EntryGoDlg();
						sTitle = String.Format(LexEdStrings.ksIdentifyXLexEntry, lrt.Name.BestAnalysisAlternative.Text);
						break;

					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
						dlg = new LinkEntryOrSenseDlg();
						sTitle = String.Format(LexEdStrings.ksIdentifyXLexEntryOrSense, lrt.Name.BestAnalysisAlternative.Text);
						break;
					default:
						Debug.Assert(lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair || lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtSenseTree);
						return null;
				}
				Debug.Assert(dlg != null);
				var wp = new WindowParams { m_title = sTitle, m_btnText = LexEdStrings.ks_Add };

				// Don't display the current entry in the list of matching entries.  See LT-2611.
				ICmObject objEntry = this.Object;
				while (objEntry.ClassID == LexSenseTags.kClassId)
					objEntry = objEntry.Owner;
				Debug.Assert(objEntry.ClassID == LexEntryTags.kClassId);
				dlg.StartingEntry = objEntry as ILexEntry;

				dlg.SetDlgInfo(m_cache, wp, PropertyTable, Publisher);
				dlg.SetHelpTopic("khtpChooseLexicalRelationAdd");
				if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
					first = dlg.SelectedObject;
				return first;
			}
			finally
			{
				if (dlg != null)
					dlg.Dispose();
			}
		}

		public void HandleMoreMenuItem(object sender, EventArgs ea)
		{
			CheckDisposed();
			MessageBoxExManager.Trigger("CreateNewLexicalReferenceType");
			m_cache.DomainDataByFlid.BeginUndoTask(LexEdStrings.ksUndoInsertLexRefType,
				LexEdStrings.ksRedoInsertLexRefType);
			ICmPossibilityList list = m_cache.LanguageProject.LexDbOA.ReferencesOA;
			ILexRefType newKid = list.Services.GetInstance<ILexRefTypeFactory>().Create();
			list.PossibilitiesOS.Add(newKid);
			m_cache.DomainDataByFlid.EndUndoTask();
			var commands = new List<string>
			{
				"AboutToFollowLink",
				"FollowLink"
			};
			var parms = new List<object>
			{
				null,
				new FwLinkArgs("lexRefEdit", newKid.Guid)
			};
			ContainingDataTree.Publisher.Publish(commands, parms);
		}

		protected void ExpandNewNode()
		{
			try
			{
				ContainingDataTree.DeepSuspendLayout();
				XmlNode caller = null;
				if (Key.Length > 1)
					caller = Key[Key.Length - 2] as XmlNode;
				int insPos = this.IndexInContainer + m_refs.Count;
				GenerateChildNode(m_refs.Count-1, m_configurationNode, caller, Indent,
					ref insPos, new ArrayList(Key), new ObjSeqHashMap());
				Expansion = DataTree.TreeItemState.ktisExpanded;
			}
			finally
			{
				ContainingDataTree.DeepResumeLayout();
			}

		}

		/// <summary>
		/// Expand this node, which is at position iSlice in its parent.
		/// </summary>
		/// <remarks> I (JH) don't know why this was written to take the index of the slice.
		/// It's just as easy for this class to find its own index.</remarks>
		/// <param name="iSlice"></param>
		public override void Expand(int iSlice)
		{
			CheckDisposed();
			try
			{
				ContainingDataTree.DeepSuspendLayout();
				XmlNode caller = null;
				if (Key.Length > 1)
					caller = Key[Key.Length - 2] as XmlNode;
				int insPos = iSlice + 1;
				GenerateChildren(m_configurationNode, caller, m_obj, Indent, ref insPos, new ArrayList(Key), new ObjSeqHashMap(), false);
				Expansion = DataTree.TreeItemState.ktisExpanded;
			}
			finally
			{
				ContainingDataTree.DeepResumeLayout();
			}
		}


		/// <summary>
		/// This method is called when a user selects Delete Relation on a Lexical Relation slice.
		/// For: sequence relations (eg. Calendar)
		///     collection relations (eg. Synonym)
		///     tree relation (parts/whole when deleting a Whole slice)
		/// </summary>
		/// <param name="hvo"></param>
		public void DeleteFromReference(ILexReference lr)
		{
			CheckDisposed();
			if (lr == null)
			{
				throw new ConfigurationException("Slice:GetObjectHvoForMenusToOperateOn is either messed up or should not have been called, because it could not find the object to be deleted.", m_configurationNode);
			}
			else
			{
				var mainWindow = PropertyTable.GetValue<Form>("window");
				using (new WaitCursor(mainWindow))
				{
					using (var dlg = new ConfirmDeleteObjectDlg(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
					{

						var ui = CmObjectUi.MakeUi(m_cache, lr.Hvo);
						ui.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);

						//We need this to determine which kind of relation we are deleting
						var lrtOwner = (ILexRefType) lr.Owner;

						var analWs = lrtOwner.Services.WritingSystems.DefaultAnalysisWritingSystem.Handle;
						var userWs = m_cache.WritingSystemFactory.UserWs;
						var tisb = TsIncStrBldrClass.Create();
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);

						switch ((LexRefTypeTags.MappingTypes)lrtOwner.MappingType)
						{
							case LexRefTypeTags.MappingTypes.kmtSenseSequence:
							case LexRefTypeTags.MappingTypes.kmtEntrySequence:
							case LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence:
							case LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection:
							case LexRefTypeTags.MappingTypes.kmtEntryCollection:
							case LexRefTypeTags.MappingTypes.kmtSenseCollection:
								if (lr.TargetsRS.Count > 2)
								{
									tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
									tisb.Append(String.Format(LexEdStrings.ksDeleteSequenceCollectionA,
										StringUtils.kChHardLB.ToString()));
									tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, analWs);
									tisb.Append(lrtOwner.ShortName);
									tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
									tisb.Append(LexEdStrings.ksDeleteSequenceCollectionB);

									dlg.SetDlgInfo(ui, m_cache, PropertyTable, tisb.GetString());
								}
								else
								{
									dlg.SetDlgInfo(ui, m_cache, PropertyTable);
								}
								break;
							default:
								dlg.SetDlgInfo(ui, m_cache, PropertyTable);
								break;
						}

						if (DialogResult.Yes == dlg.ShowDialog(mainWindow))
						{
							UndoableUnitOfWorkHelper.Do(LexEdStrings.ksUndoDeleteRelation, LexEdStrings.ksRedoDeleteRelation, m_obj, () =>
							{
								//If the user selected Yes, then we need to delete 'this' sense or entry
								lr.TargetsRS.Remove(m_obj);
							});
							//Update the display because we have removed this slice from the Lexical entry.
							UpdateForDelete(lr);
						 }
					}
				}
			}
		}

		/// <summary>
		/// This method is called when a user selects Delete Relation on a Lexical Relation slice.
		/// For: Pair relation (eg. Antonym)
		///     tree relation (parts/whole when deleting a Parts slice)
		/// </summary>
		/// <param name="hvo"></param>
		public void DeleteReference(ILexReference lr)
		{
			CheckDisposed();
			if (lr == null)
			{
				throw new ConfigurationException("Slice:GetObjectHvoForMenusToOperateOn is either messed up or should not have been called, because it could not find the object to be deleted.", m_configurationNode);
			}
			else
			{
				var mainWindow = PropertyTable.GetValue<Form>("window");
				using (new WaitCursor(mainWindow))
				{
					using (var dlg = new ConfirmDeleteObjectDlg(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
					{
						var ui = CmObjectUi.MakeUi(m_cache, lr.Hvo);
						ui.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);

						//We need this to determine which kind of relation we are deleting
						var lrtOwner = lr.Owner as ILexRefType;

						var userWs = m_cache.WritingSystemFactory.UserWs;
						var tisb = TsIncStrBldrClass.Create();
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);

						switch ((LexRefTypeTags.MappingTypes)lrtOwner.MappingType)
						{
						case LexRefTypeTags.MappingTypes.kmtSenseTree:
						case LexRefTypeTags.MappingTypes.kmtEntryTree:
						case LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
							tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
							tisb.Append(String.Format(LexEdStrings.ksDeleteLexTree, StringUtils.kChHardLB));
							dlg.SetDlgInfo(ui, m_cache, PropertyTable, tisb.GetString());
							break;
						default:
							dlg.SetDlgInfo(ui, m_cache, PropertyTable);
							break;
						}

						if (DialogResult.Yes == dlg.ShowDialog(mainWindow))
						{
							UndoableUnitOfWorkHelper.Do(LexEdStrings.ksUndoDeleteRelation, LexEdStrings.ksRedoDeleteRelation, m_obj, () =>
							{
								m_cache.DomainDataByFlid.DeleteObj(lr.Hvo);
							});
							//Update the display because we have removed this slice from the Lexical entry.
							UpdateForDelete(lr);
						}
					}
				}
			}
		}

		private void UpdateForDelete(ILexReference lr)
		{
			// This slice might get disposed by one of the calling methods.  See FWR-3291.
			if (IsDisposed)
				return;
			m_refs.Remove(lr);
			// if this flickers too annoyingly, we can probably optimize by extracting relevant lines from Collapse.
			Collapse();
			Expand();
		}

		/// <summary>
		/// This method is called when a user selects "Edit Reference Set Details" for a Lexical Relation slice.
		/// </summary>
		/// <param name="hvo"></param>
		public void EditReferenceDetails(ILexReference lr)
		{
			CheckDisposed();
			if (lr == null)
			{
				throw new ConfigurationException("Slice:GetObjectHvoForMenusToOperateOn is either messed up or should not have been called, because it could not find the object to be deleted.", m_configurationNode);
			}
			else
			{
				using (var dlg = new LexReferenceDetailsDlg(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
				{
					dlg.ReferenceName = lr.Name.AnalysisDefaultWritingSystem.Text;
					dlg.ReferenceComment = lr.Comment.AnalysisDefaultWritingSystem.Text;
					if (dlg.ShowDialog() == DialogResult.OK)
					{
						using (UndoableUnitOfWorkHelper helper = new UndoableUnitOfWorkHelper(m_cache.ActionHandlerAccessor,
							LexEdStrings.ksUndoEditRefSetDetails, LexEdStrings.ksRedoEditRefSetDetails))
						{
							lr.Name.SetAnalysisDefaultWritingSystem(dlg.ReferenceName);
							lr.Comment.SetAnalysisDefaultWritingSystem(dlg.ReferenceComment);
							helper.RollBack = false;
						}
					}
				}
			}
		}

		public static SimpleListChooser MakeSenseChooser(FdoCache cache,
			IHelpTopicProvider helpTopicProvider)
		{
			var senses = cache.ServiceLocator.GetInstance<ILexSenseRepository>().AllInstances();
			var labels = ObjectLabel.CreateObjectLabels(cache, senses.Cast<ICmObject>(), "LongNameTSS");
			SimpleListChooser chooser = new SimpleListChooser(null, labels,
				LexEdStrings.ksSenses, helpTopicProvider);
			chooser.Cache = cache;
			return chooser;
		}
	}
}
