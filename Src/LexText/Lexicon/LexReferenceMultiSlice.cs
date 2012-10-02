using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FdoUi;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.LexText.Controls;
using XCore;

namespace SIL.FieldWorks.XWorks.LexEd
{
	public interface ILexReferenceSlice
	{
		Slice MasterSlice { get; set; }
		void HandleDeleteCommand(Command cmd);
		void HandleLaunchChooser();
		void HandleEditCommand();
	}

	/// <summary>
	/// Summary description for LexReferenceMultiSlice.
	/// </summary>
	public class LexReferenceMultiSlice : Slice
	{
		// Array of ints.
		private List<int> m_refs;
		// Array of ints.
		private List<int> m_refTypesAvailable = new List<int>();
		private List<bool> m_rgfReversedRefType = new List<bool>();

		// virtual handler for LexReferences
		private BaseLexReferencesVirtualHandler m_vh;

		public LexReferenceMultiSlice() : base()
		{
		}

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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
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

			string className = m_cache.MetaDataCacheAccessor.GetClassName((uint)m_obj.ClassID);
			string fieldName = XmlUtils.GetManditoryAttributeValue(m_configurationNode, "field");
			IVwCacheDa cda = m_cache.VwCacheDaAccessor;
			m_vh = cda.GetVirtualHandlerName(className, fieldName) as BaseLexReferencesVirtualHandler;
			Debug.Assert(m_vh != null, "We should have a virtual handler installed for " +
				className + "." + fieldName);

			base.FinishInit();
		}

		public override void GenerateChildren(XmlNode node, XmlNode caller, ICmObject obj, int indent,
			ref int insPos, ArrayList path, ObjSeqHashMap reuseMap)
		{
			CheckDisposed();
			// If node has children, figure what to do with them...
			XmlNodeList children = node.ChildNodes;

			// It's important to initialize m_refs here rather than in FinishInit, because we need it
			// to be updated when the slice is reused in a regenerate.
			// Refactor JohnT: better still, make it a virtual attribute, and Refresh will automatically
			// clear it from the cache.
			if (m_vh != null)
			{
				m_refs = m_vh.LexReferences(m_obj.Hvo);
			}
			else
			{
				// DEPRECATED.
				string className = m_cache.MetaDataCacheAccessor.GetClassName((uint)m_obj.ClassID);
//				m_flid = AutoDataTreeMenuHandler.ContextMenuHelper.GetFlid(m_cache.MetaDataCacheAccessor,
//					className, m_fieldName);
				string qry = string.Format("SELECT DISTINCT [Src] FROM LexReference_Targets WHERE [Dst]={0}",
					m_obj.Hvo);
				m_refs = DbOps.ReadIntsFromCommand(m_cache, qry, null);
			}

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
			ILexReference lr = LexReference.CreateFromDBObject(m_cache, (int)m_refs[iChild]);
			ILexRefType lrt = LexRefType.CreateFromDBObject(m_cache, lr.OwnerHVO);
			string sLabel = lrt.ShortName;
			if (sLabel == null || sLabel == string.Empty)
				sLabel = lrt.Abbreviation.BestAnalysisAlternative.Text;
			bool fTreeRoot = true;
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			int chvoTargets = sda.get_VecSize(lr.Hvo, (int)LexReference.LexReferenceTags.kflidTargets);
			// change the label for a Tree relationship.
			switch ((LexRefType.MappingTypes)lrt.MappingType)
			{
				case LexRefType.MappingTypes.kmtSenseTree:
				case LexRefType.MappingTypes.kmtEntryTree:
				case LexRefType.MappingTypes.kmtEntryOrSenseTree:
				case LexRefType.MappingTypes.kmtSenseAsymmetricPair: // Sense Pair with different Forward/Reverse names
				case LexRefType.MappingTypes.kmtEntryAsymmetricPair: // Entry Pair with different Forward/Reverse names
				case LexRefType.MappingTypes.kmtEntryOrSenseAsymmetricPair: // Entry or sense Pair with different Forward/Reverse names
					//int chvo = sda.get_VecSize(lr.Hvo, (int)LexReference.LexReferenceTags.kflidTargets);
					if (chvoTargets > 0)
					{
						int hvoFirst = sda.get_VecItem(lr.Hvo, (int)LexReference.LexReferenceTags.kflidTargets, 0);
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
			switch ((LexRefType.MappingTypes)lrt.MappingType)
			{
				case LexRefType.MappingTypes.kmtSenseCollection:
					sXml +=	" class=\"SIL.FieldWorks.XWorks.LexEd.LexReferenceCollectionSlice\"";
					break;
				case LexRefType.MappingTypes.kmtSensePair:
				case LexRefType.MappingTypes.kmtSenseAsymmetricPair: // Sense Pair with different Forward/Reverse names
				case LexRefType.MappingTypes.kmtEntryPair:
				case LexRefType.MappingTypes.kmtEntryAsymmetricPair: // Entry Pair with different Forward/Reverse names
				case LexRefType.MappingTypes.kmtEntryOrSensePair:
				case LexRefType.MappingTypes.kmtEntryOrSenseAsymmetricPair: // Entry or sense Pair with different forward/Reverse names
					sXml +=	" class=\"SIL.FieldWorks.XWorks.LexEd.LexReferencePairSlice\"";
					sMenu = "mnuDataTree-DeleteReplaceLexReference";
					break;
				case LexRefType.MappingTypes.kmtSenseTree:
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
				case LexRefType.MappingTypes.kmtSenseSequence:
				case LexRefType.MappingTypes.kmtEntrySequence:
				case LexRefType.MappingTypes.kmtEntryOrSenseSequence:
					sXml +=	" class=\"SIL.FieldWorks.XWorks.LexEd.LexReferenceSequenceSlice\"";
					break;
				case LexRefType.MappingTypes.kmtEntryCollection:
					sXml +=	" class=\"SIL.FieldWorks.XWorks.LexEd.LexReferenceCollectionSlice\"";
					//sMenu = "mnuDataTree-DeleteFromLexEntryReference"; we used to have distinct strings in the menu
					sMenu = "mnuDataTree-DeleteAddLexReference";
					break;
				case LexRefType.MappingTypes.kmtEntryTree:
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
				case LexRefType.MappingTypes.kmtEntryOrSenseCollection:
					sXml +=	" class=\"SIL.FieldWorks.XWorks.LexEd.LexReferenceCollectionSlice\"";
					if (m_obj is LexEntry)
						//sMenu = "mnuDataTree-DeleteFromLexEntryReference"; we used to have distinct strings in the menu
						sMenu = "mnuDataTree-DeleteAddLexReference";
					break;
				case LexRefType.MappingTypes.kmtEntryOrSenseTree:
					if (m_obj is LexEntry)
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
				Slice child = Parent.Controls[islice] as Slice;
				if (child is ILexReferenceSlice)
				{
					(child as ILexReferenceSlice).MasterSlice = this;
				}
			}
			node.InnerXml = "";
		}

		public override bool HandleMouseDown(Point p)
		{
			CheckDisposed();
			System.Windows.Forms.ContextMenuStrip contextMenuStrip = SetupContextMenuStrip();
			if (contextMenuStrip.Items.Count > 0)
				contextMenuStrip.Show(TreeNode, p);
			return true;
		}


		protected System.Windows.Forms.ContextMenuStrip SetupContextMenuStrip()
		{
			System.Windows.Forms.ContextMenuStrip contextMenuStrip = new System.Windows.Forms.ContextMenuStrip();

			int[] refTypes = m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.HvoArray;
			m_refTypesAvailable.Clear();
			m_rgfReversedRefType.Clear();
			string formatName = Mediator.StringTbl.GetString("InsertSymmetricReference", "LexiconTools");
			string formatNameWithReverse = Mediator.StringTbl.GetString("InsertAsymmetricReference", "LexiconTools");
			for (int i = 0; i < refTypes.Length; i++)
			{
				ILexRefType lrt = LexRefType.CreateFromDBObject(m_cache, refTypes[i]);
				if (m_obj is ILexEntry)
				{
					switch ((LexRefType.MappingTypes)lrt.MappingType)
					{
						case LexRefType.MappingTypes.kmtSenseCollection:
						case LexRefType.MappingTypes.kmtSensePair:
						case LexRefType.MappingTypes.kmtSenseTree:
						case LexRefType.MappingTypes.kmtSenseSequence:
						case LexRefType.MappingTypes.kmtSenseAsymmetricPair:
							continue;
						default:
							break;
					}
				}
				else
				{
					switch ((LexRefType.MappingTypes)lrt.MappingType)
					{
						case LexRefType.MappingTypes.kmtEntryCollection:
						case LexRefType.MappingTypes.kmtEntryPair:
						case LexRefType.MappingTypes.kmtEntryTree:
						case LexRefType.MappingTypes.kmtEntrySequence:
						case LexRefType.MappingTypes.kmtEntryAsymmetricPair:
							continue;
						default:
							break;
					}
				}
				string label = "";
				string label2 = "";
				string reverseName = LexRefType.BestAnalysisOrVernReverseName(lrt.Cache, lrt.Hvo).Text; // replaces lrt.ReverseName.AnalysisDefaultWritingSystem;
				if (reverseName == null || reverseName == string.Empty)
					reverseName = LexEdStrings.ksStars;
				string name = lrt.ShortName;
				if (name == null || name == string.Empty)
					name = LexEdStrings.ksStars;
				switch ((LexRefType.MappingTypes)lrt.MappingType)
				{
					case LexRefType.MappingTypes.kmtSenseCollection:
					case LexRefType.MappingTypes.kmtSensePair:
					case LexRefType.MappingTypes.kmtSenseSequence:
					case LexRefType.MappingTypes.kmtEntryCollection:
					case LexRefType.MappingTypes.kmtEntryPair:
					case LexRefType.MappingTypes.kmtEntrySequence:
					case LexRefType.MappingTypes.kmtEntryOrSenseCollection:
					case LexRefType.MappingTypes.kmtEntryOrSensePair:
					case LexRefType.MappingTypes.kmtEntryOrSenseSequence:
						label = string.Format(formatName, name);
						break;
					case LexRefType.MappingTypes.kmtSenseTree:
					case LexRefType.MappingTypes.kmtEntryTree:
					case LexRefType.MappingTypes.kmtEntryOrSenseTree:
					case LexRefType.MappingTypes.kmtSenseAsymmetricPair:
					case LexRefType.MappingTypes.kmtEntryAsymmetricPair:
					case LexRefType.MappingTypes.kmtEntryOrSenseAsymmetricPair:
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
				m_refTypesAvailable.Insert(iInsert, refTypes[i]);
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
					m_refTypesAvailable.Insert(iInsert, refTypes[i]);
					m_rgfReversedRefType.Insert(iInsert, true);
				}
			}

			AddFinalContextMenuStripOptions(contextMenuStrip);
			return contextMenuStrip;
		}

		private void AddFinalContextMenuStripOptions(System.Windows.Forms.ContextMenuStrip contextMenuStrip)
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
			Image imgHelp = ContainingDataTree.SmallImages.GetImage("Help");
			contextMenuStrip.Items.Add(new ToolStripMenuItem(LexEdStrings.ksHelp, imgHelp, new EventHandler(this.OnHelp)));
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
				ShowHelp.ShowHelpTopic(FwApp.App, "khtpFieldLexSenseLexicalRelations");
			else if(HelpId == "LexEntryReferences")
				ShowHelp.ShowHelpTopic(FwApp.App, "khtpFieldLexEntryCrossReference");
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
			if (tag == (int)LexRefType.LexRefTypeTags.kflidMembers)
			{
				// if this flickers too annoyingly, we can probably optimize by extracting relevant lines from Collapse.
				CollapseMaster();
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
			if (!m_cache.VerifyValidObject(m_obj))
				return;
			int itemIndex = (((ToolStripItem)(sender)).Owner
							as ContextMenuStrip).Items.IndexOf((ToolStripItem)sender);
			int hvoType = m_refTypesAvailable[itemIndex];
			bool fReverseRef = m_rgfReversedRefType[itemIndex];
			ILexRefType lrt = LexRefType.CreateFromDBObject(m_cache, hvoType);
			int hvoNew = 0;
			int hvoFirst = 0;
			if (fReverseRef)
			{
				// When creating a tree Lexical Relation and the user is choosing
				// the root of the tree, first see if the user selects a lexical entry.
				// If they do not select anything (hvoFirst==0) return and do not create the slice.
				hvoFirst = GetRootObjectHvo(lrt);
				if (hvoFirst == 0)
					return;		// the user cancelled out of the operation.
				if (lrt.MappingType == (int)LexRefType.MappingTypes.kmtSenseTree ||
					lrt.MappingType == (int)LexRefType.MappingTypes.kmtEntryTree ||
					lrt.MappingType == (int)LexRefType.MappingTypes.kmtEntryOrSenseTree)
				{
					// Use an existing LexReference if one exists.
					foreach (ILexReference lr in lrt.MembersOC)
					{
						if (lr.TargetsRS.Count > 0 && lr.TargetsRS.HvoArray[0] == hvoFirst)
						{
							lr.TargetsRS.Append(m_obj.Hvo);
							hvoNew = lr.Hvo;
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
				hvoFirst = GetChildObjectHvo(lrt);
				if (hvoFirst == 0)
					return;		// the user cancelled out of the operation.
			}
			if (hvoNew == 0)
			{
				hvoNew = m_cache.CreateObject((int)LexReference.kclsidLexReference, hvoType,
					(int)LexRefType.LexRefTypeTags.kflidMembers, 0);
				ILexReference lr = LexReference.CreateFromDBObject(m_cache, hvoNew);
				if (fReverseRef)
				{
					lr.TargetsRS.InsertAt(hvoFirst, 0);
					lr.TargetsRS.InsertAt(m_obj.Hvo, 1);
				}
				else
				{
					//When creating a lexical relation slice,
					//add the current lexical entry to the lexical relation as the first item
					lr.TargetsRS.InsertAt(m_obj.Hvo, 0);
					//then also add the lexical entry that the user selected in the chooser dialog.
					lr.TargetsRS.InsertAt(hvoFirst, 1);
				}
			}
			m_refs.Add(hvoNew);

			this.ExpandNewNode();

			// update the cache through our virtual handler.
			if (m_vh != null)
			{
				IVwCacheDa cda = m_cache.VwCacheDaAccessor;
				int flid = m_vh.Tag;
				m_vh.Load(m_obj.Hvo, flid, 0, cda);
				m_cache.MainCacheAccessor.PropChanged(null,
					(int)PropChangeType.kpctNotifyAll, m_obj.Hvo, flid, 0, 1, 0);
			}
			(m_obj as CmObject).UpdateTimestampForVirtualChange();
			if (hvoFirst != 0)
			{
				ICmObject cmoFirst = CmObject.CreateFromDBObject(m_cache, hvoFirst);
				(cmoFirst as CmObject).UpdateTimestampForVirtualChange();
			}
		}

		/// <summary>
		/// This method is called when we are creating a new lexical relation slice.
		/// If the user selects an item it's hvo is returned.
		/// Otherwise 0 is returned and the lexical relation should not be created.
		/// </summary>
		/// <param name="lrt"></param>
		/// <returns></returns>
		private int GetRootObjectHvo(ILexRefType lrt)
		{
			int hvoFirst = 0;
			BaseEntryGoDlg dlg = null;
			switch ((LexRefType.MappingTypes)lrt.MappingType)
			{
				case LexRefType.MappingTypes.kmtSenseAsymmetricPair:
				case LexRefType.MappingTypes.kmtSenseTree:
					dlg = new LinkEntryOrSenseDlg();
					(dlg as LinkEntryOrSenseDlg).SelectSensesOnly = true;
					break;
				case LexRefType.MappingTypes.kmtEntryAsymmetricPair:
				case LexRefType.MappingTypes.kmtEntryTree:
					dlg = new GoDlg();
					break;
				case LexRefType.MappingTypes.kmtEntryOrSenseAsymmetricPair:
				case LexRefType.MappingTypes.kmtEntryOrSenseTree:
					dlg = new LinkEntryOrSenseDlg();
					break;
				default:
					Debug.Assert(lrt.MappingType == (int)LexRefType.MappingTypes.kmtSenseAsymmetricPair ||
						lrt.MappingType == (int)LexRefType.MappingTypes.kmtSenseTree);
					return 0;
			}
			Debug.Assert(dlg != null);
			WindowParams wp = new WindowParams();
			//wp.m_title = String.Format(LexEdStrings.ksIdentifyXEntry,
			//   lrt.Name.AnalysisDefaultWritingSystem);
			wp.m_title = String.Format(LexEdStrings.ksIdentifyXEntry,
				lrt.ReverseName.BestAnalysisAlternative.Text);
			wp.m_label = LexEdStrings.ksFind_;
			//wp.m_btnText = LexEdStrings.ks_Link;
			wp.m_btnText = LexEdStrings.ks_Add;
			dlg.SetDlgInfo(m_cache, wp, Mediator);
			dlg.SetHelpTopic("khtpChooseLexicalRelationAdd");
			if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
				hvoFirst = dlg.SelectedID;
			dlg.Dispose();
			return hvoFirst;
		}

		/// <summary>
		/// This method is called when we are creating a new lexical relation slice.
		/// If the user selects an item it's hvo is returned.
		/// Otherwise 0 is returned and the lexical relation should not be created.
		/// </summary>
		/// <param name="lrt"></param>
		/// <returns></returns>
		private int GetChildObjectHvo(ILexRefType lrt)
		{
			int hvoFirst = 0;
			BaseEntryGoDlg dlg = null;
			string sTitle = "";
			switch ((LexRefType.MappingTypes)lrt.MappingType)
			{
				case LexRefType.MappingTypes.kmtEntryOrSensePair:
				case LexRefType.MappingTypes.kmtEntryOrSenseAsymmetricPair: // Entry or sense pair with different Forward/Reverse
					dlg = new LinkEntryOrSenseDlg();
					(dlg as LinkEntryOrSenseDlg).SelectSensesOnly = false;
					sTitle = String.Format(LexEdStrings.ksIdentifyXLexEntryOrSense,
						 lrt.Name.BestAnalysisAlternative.Text);
					break;

				case LexRefType.MappingTypes.kmtSenseCollection:
				case LexRefType.MappingTypes.kmtSensePair:
				case LexRefType.MappingTypes.kmtSenseAsymmetricPair: // Sense pair with different Forward/Reverse names
				case LexRefType.MappingTypes.kmtSenseSequence:
				case LexRefType.MappingTypes.kmtSenseTree:
					dlg = new LinkEntryOrSenseDlg();
					(dlg as LinkEntryOrSenseDlg).SelectSensesOnly = true;
					sTitle = String.Format(LexEdStrings.ksIdentifyXSense,
						lrt.Name.BestAnalysisAlternative.Text);
					break;

				case LexRefType.MappingTypes.kmtEntryCollection:
				case LexRefType.MappingTypes.kmtEntryPair:
				case LexRefType.MappingTypes.kmtEntryAsymmetricPair: // Entry pair with different Forward/Reverse names
				case LexRefType.MappingTypes.kmtEntrySequence:
				case LexRefType.MappingTypes.kmtEntryTree:
					dlg = new GoDlg();
					sTitle = String.Format(LexEdStrings.ksIdentifyXLexEntry,
						lrt.Name.BestAnalysisAlternative.Text);
					break;

				case LexRefType.MappingTypes.kmtEntryOrSenseCollection:
				case LexRefType.MappingTypes.kmtEntryOrSenseSequence:
				case LexRefType.MappingTypes.kmtEntryOrSenseTree:
					dlg = new LinkEntryOrSenseDlg();
					sTitle = String.Format(LexEdStrings.ksIdentifyXLexEntryOrSense,
						lrt.Name.BestAnalysisAlternative.Text);
					break;
				default:
					Debug.Assert(lrt.MappingType == (int)LexRefType.MappingTypes.kmtSenseAsymmetricPair ||
						lrt.MappingType == (int)LexRefType.MappingTypes.kmtSenseTree);
					return 0;
			}
			Debug.Assert(dlg != null);
			WindowParams wp = new WindowParams();
			wp.m_title = sTitle;
			wp.m_label = LexEdStrings.ksFind_;
			wp.m_btnText = LexEdStrings.ks_Add;

			// Don't display the current entry in the list of matching entries.  See LT-2611.
			ICmObject objEntry = this.Object;
			while (objEntry.ClassID == LexSense.kclsidLexSense)
				objEntry = CmObject.CreateFromDBObject(m_cache, objEntry.OwnerHVO);
			Debug.Assert(objEntry.ClassID == LexEntry.kclsidLexEntry);
			dlg.StartingEntry = objEntry as ILexEntry;

			dlg.SetDlgInfo(m_cache, wp, Mediator);
			dlg.SetHelpTopic("khtpChooseLexicalRelationAdd");
			if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
				hvoFirst = dlg.SelectedID;
			dlg.Dispose();
			return hvoFirst;
		}

		public void HandleMoreMenuItem(object sender, EventArgs ea)
		{
			CheckDisposed();
			XCore.XMessageBoxExManager.Trigger("CreateNewLexicalReferenceType");
			m_cache.BeginUndoTask(LexEdStrings.ksUndoInsertLexRefType,
				LexEdStrings.ksRedoInsertLexRefType);
			ICmPossibilityList list = m_cache.LangProject.LexDbOA.ReferencesOA;
			ILexRefType newKid = (ILexRefType)list.PossibilitiesOS.Append(new LexRefType());
			m_cache.EndUndoTask();
			m_cache.MainCacheAccessor.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
				list.Hvo, (int)CmPossibilityList.CmPossibilityListTags.kflidPossibilities,
				list.PossibilitiesOS.Count - 1,
				1, 0);
			ContainingDataTree.Mediator.SendMessage("FollowLink",
				SIL.FieldWorks.FdoUi.FwLink.Create("lexRefEdit",
				m_cache.GetGuidFromId(newKid.Hvo),
				m_cache.ServerName,
				m_cache.DatabaseName));
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
				// A crude way to force the +/- icon to be redrawn.
				// If this gets flashy, we could figure a smaller region to invalidate.
				ContainingDataTree.Invalidate(true);  // Invalidates both children.
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
				GenerateChildren(m_configurationNode, caller, m_obj, Indent, ref insPos, new ArrayList(Key), new ObjSeqHashMap());
				Expansion = DataTree.TreeItemState.ktisExpanded;
				// A crude way to force the +/- icon to be redrawn.
				// If this gets flashy, we could figure a smaller region to invalidate.
				ContainingDataTree.Invalidate(true);  // Invalidates both children.
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
		public void DeleteFromReference(int hvo)
		{
			CheckDisposed();
			if (hvo <= 0)
			{
				throw new ConfigurationException("Slice:GetObjectHvoForMenusToOperateOn is either messed up or should not have been called, because it could not find the object to be deleted.", m_configurationNode);
			}
			else
			{
					Form mainWindow = (Form)Mediator.PropertyTable.GetValue("window");
					mainWindow.Cursor = Cursors.WaitCursor;
					using (ConfirmDeleteObjectDlg dlg = new ConfirmDeleteObjectDlg())
					{

						CmObjectUi ui = CmObjectUi.MakeUi(m_cache, hvo);
						ILexReference lr = LexReference.CreateFromDBObject(m_cache, hvo);

						//We need this to determine which kind of relation we are deleting
						LexRefType lrtOwner =
						(LexRefType)CmObject.CreateFromDBObject(m_cache, lr.OwnerHVO);

						int analWs = m_cache.DefaultAnalWs;
						int userWs = m_cache.DefaultUserWs;
						ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);

						switch ((LexRefType.MappingTypes)lrtOwner.MappingType)
						{
							case LexRefType.MappingTypes.kmtSenseSequence:
							case LexRefType.MappingTypes.kmtEntrySequence:
							case LexRefType.MappingTypes.kmtEntryOrSenseSequence:
							case LexRefType.MappingTypes.kmtEntryOrSenseCollection:
							case LexRefType.MappingTypes.kmtEntryCollection:
							case LexRefType.MappingTypes.kmtSenseCollection:
								if (lr.TargetsRS.Count > 2)
								{
									tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
									tisb.Append(String.Format(LexEdStrings.ksDeleteSequenceCollectionA,
										"\x2028"));
									tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, analWs);
									tisb.Append(lrtOwner.ShortName);
									tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
									tisb.Append(LexEdStrings.ksDeleteSequenceCollectionB);

									dlg.SetDlgInfo(ui, m_cache, Mediator, tisb.GetString());
								}
								else
								{
									dlg.SetDlgInfo(ui, m_cache, Mediator);
								}
								break;
							default:
								dlg.SetDlgInfo(ui, m_cache, Mediator);
								break;
						}

						if (DialogResult.Yes == dlg.ShowDialog(mainWindow))
						{
							//If the user selected Yes, then we need to delete 'this' sense or entry
							int hvoDisplayParent = m_obj.Hvo;
							lr.TargetsRS.Remove(hvoDisplayParent);

							if (lr.TargetsRS.Count < 2)
								//in this situation there is only 1 or 0 items left in this lexical Relation so
								//we need to delete the relation in the other Lexicon entries.
							{
								lr.DeleteUnderlyingObject();
							}
							//Update the display because we have removed this slice from the Lexical entry.
							UpdateForDelete(hvo);

							mainWindow.Cursor = Cursors.Default;
						}
						else  //If the user selected Cancel in the delete dialog do nothing
						{
							mainWindow.Cursor = Cursors.Default;
							return;
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
		public void DeleteReference(int hvo)
		{
			CheckDisposed();
			if (hvo <= 0)
			{
				throw new ConfigurationException("Slice:GetObjectHvoForMenusToOperateOn is either messed up or should not have been called, because it could not find the object to be deleted.", m_configurationNode);
			}
			else
			{
				Form mainWindow = (Form)Mediator.PropertyTable.GetValue("window");
				mainWindow.Cursor = Cursors.WaitCursor;
				using (ConfirmDeleteObjectDlg dlg = new ConfirmDeleteObjectDlg())
				{
					CmObjectUi ui = CmObjectUi.MakeUi(m_cache, hvo);
					ILexReference lr = LexReference.CreateFromDBObject(m_cache, hvo);

					//We need this to determine which kind of relation we are deleting
					LexRefType lrtOwner =
					(LexRefType)CmObject.CreateFromDBObject(m_cache, lr.OwnerHVO);

					int analWs = m_cache.DefaultAnalWs;
					int userWs = m_cache.DefaultUserWs;
					ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);

					switch ((LexRefType.MappingTypes)lrtOwner.MappingType)
					{
					case LexRefType.MappingTypes.kmtSenseTree:
					case LexRefType.MappingTypes.kmtEntryTree:
					case LexRefType.MappingTypes.kmtEntryOrSenseTree:
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
						tisb.Append(String.Format(LexEdStrings.ksDeleteLexTree, "\x2028"));
						dlg.SetDlgInfo(ui, m_cache, Mediator, tisb.GetString() );
						break;
					default:
						dlg.SetDlgInfo(ui, m_cache, Mediator);
						break;
					}

					if (DialogResult.Yes == dlg.ShowDialog(mainWindow))
					{
						lr.DeleteUnderlyingObject();
						//Update the display because we have removed this slice from the Lexical entry.
						UpdateForDelete(hvo);

						mainWindow.Cursor = Cursors.Default;
					}
					else //If the user selected Cancel in the delete dialog do nothing
					{
						mainWindow.Cursor = Cursors.Default;
						return;
					}
				}

			}
		}




		public void UpdateForDelete(int hvoRef)
		{
			CheckDisposed();
			m_refs.Remove(hvoRef);
			// if this flickers too annoyingly, we can probably optimize by extracting relevant lines from Collapse.
			CollapseMaster();
			Expand();
		}

		/// <summary>
		/// This method is called when a user selects "Edit Reference Set Details" for a Lexical Relation slice.
		/// </summary>
		/// <param name="hvo"></param>
		public void EditReferenceDetails(int hvo)
		{
			CheckDisposed();
			if (hvo <= 0)
			{
				throw new ConfigurationException("Slice:GetObjectHvoForMenusToOperateOn is either messed up or should not have been called, because it could not find the object to be deleted.", m_configurationNode);
			}
			else
			{
				ILexReference lr = LexReference.CreateFromDBObject(m_cache, hvo);
				using (LexReferenceDetailsDlg dlg = new LexReferenceDetailsDlg())
				{
					dlg.ReferenceName = lr.Name.AnalysisDefaultWritingSystem;
					dlg.ReferenceComment = lr.Comment.AnalysisDefaultWritingSystem.Text;
					if (dlg.ShowDialog() == DialogResult.OK)
					{
						lr.Name.AnalysisDefaultWritingSystem = dlg.ReferenceName;
						lr.Comment.SetAnalysisDefaultWritingSystem(dlg.ReferenceComment);
					}
				}
			}
		}

		static public SimpleListChooser MakeSenseChooser(FdoCache cache)
		{
			List<int> lexSenses = new List<int>(DbOps.ReadIntArrayFromCommand(cache, "SELECT Dst FROM LexEntry_Senses", null));
			ObjectLabelCollection labels = new ObjectLabelCollection(cache, lexSenses, "LongNameTSS");
			SimpleListChooser chooser = new SimpleListChooser(null, labels, LexEdStrings.ksSenses);
			chooser.Cache = cache;
			return chooser;
		}
	}
}
