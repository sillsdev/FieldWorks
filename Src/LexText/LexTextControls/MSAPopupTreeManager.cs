using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.LexText.Controls;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Handles a TreeCombo control (Widgets assembly) for use with MorphoSyntaxAnalysis objects.
	/// </summary>
	public class MSAPopupTreeManager : PopupTreeManager
	{
		private const int kEmpty = 0;
		private const int kLine = -1;
		private const int kMore = -2;
		private const int kCreate = -3;
		private const int kModify = -4;

		#region Data members

		private IPersistenceProvider m_persistProvider;
		private Mediator m_mediator;
		private ILexSense m_sense = null;
		private string m_fieldName = "";
		// The following strings are loaded from the string table if possible.
		private string m_sUnknown = null;
		private string m_sSpecifyGramFunc = null;
		private string m_sModifyGramFunc = null;
		private string m_sSpecifyDifferent = null;
		private string m_sCreateGramFunc = null;
		private string m_sEditGramFunc = null;
		#endregion Data members

		#region Events

		#endregion Events

		/// <summary>
		/// Constructor.
		/// </summary>
		public MSAPopupTreeManager(TreeCombo treeCombo, FdoCache cache, ICmPossibilityList list,
			int ws, bool useAbbr, Mediator mediator, Form parent)
			: base(treeCombo, cache, list, ws, useAbbr, parent)
		{
			m_mediator = mediator;
			LoadStrings();
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public MSAPopupTreeManager(PopupTree popupTree, FdoCache cache, ICmPossibilityList list,
			int ws, bool useAbbr, Mediator mediator, Form parent)
			: base(popupTree,  cache, list, ws, useAbbr, parent)
		{
			m_mediator = mediator;
			LoadStrings();
		}

		public ILexSense Sense
		{
			get
			{
				CheckDisposed();
				return m_sense;
			}
			set
			{
				CheckDisposed();
				m_sense = value;
			}
		}

		public string FieldName
		{
			get
			{
				CheckDisposed();
				return m_fieldName;
			}
			set
			{
				CheckDisposed();
				m_fieldName = value;
			}
		}

		public IPersistenceProvider PersistenceProvider
		{
			get
			{
				CheckDisposed();
				return m_persistProvider;
			}
			set
			{
				CheckDisposed();
				m_persistProvider = value;
			}
		}

		private void LoadStrings()
		{
			// Load the special strings from the string table if possible.  If not, use the
			// default (English) values.
			if (m_mediator.StringTbl != null)
			{
				m_sUnknown = m_mediator.StringTbl.GetString("NullItemLabel",
					"DetailControls/MSAReferenceComboBox");
				m_sSpecifyGramFunc = m_mediator.StringTbl.GetString("AddNewGramFunc",
					"DetailControls/MSAReferenceComboBox");
				m_sModifyGramFunc = m_mediator.StringTbl.GetString("ModifyGramFunc",
					"DetailControls/MSAReferenceComboBox");
				m_sSpecifyDifferent = m_mediator.StringTbl.GetString("SpecifyDifferentGramFunc",
					"DetailControls/MSAReferenceComboBox");
				m_sCreateGramFunc = m_mediator.StringTbl.GetString("CreateGramFunc",
					"DetailControls/MSAReferenceComboBox");
				m_sEditGramFunc = m_mediator.StringTbl.GetString("EditGramFunc",
					"DetailControls/MSAReferenceComboBox");
			}
			if (m_sUnknown == null || m_sUnknown.Length == 0 ||
				m_sUnknown == "*NullItemLabel*")
			{
				m_sUnknown = LexTextControls.ks_NotSure_;
			}
			if (m_sSpecifyGramFunc == null || m_sSpecifyGramFunc.Length == 0 ||
				m_sSpecifyGramFunc == "*AddNewGramFunc*")
			{
				m_sSpecifyGramFunc = LexTextControls.ksSpecifyGrammaticalInfo_;
			}
			if (m_sModifyGramFunc == null || m_sModifyGramFunc.Length == 0 ||
				m_sModifyGramFunc == "*ModifyGramFunc*")
			{
				m_sModifyGramFunc = LexTextControls.ksModifyThisGrammaticalInfo_;
			}
			if (m_sSpecifyDifferent == null || m_sSpecifyDifferent.Length == 0 ||
				m_sSpecifyDifferent == "*SpecifyDifferentGramFunc*")
			{
				m_sSpecifyDifferent = LexTextControls.ksSpecifyDifferentGrammaticalInfo_;
			}
			if (m_sCreateGramFunc == null || m_sCreateGramFunc.Length == 0 ||
				m_sCreateGramFunc == "*CreateGramFuncGramFunc*")
			{
				m_sCreateGramFunc = LexTextControls.ksCreateNewGrammaticalInfo;
			}
			if (m_sEditGramFunc == null || m_sEditGramFunc.Length == 0 ||
				m_sEditGramFunc == "*EditGramFuncGramFunc*")
			{
				m_sEditGramFunc = LexTextControls.ksEditGrammaticalInfo;
			}
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

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
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				// Do NOT dispose of the mediator, which does not 'belong' to us!
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mediator = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// Populate the tree with just ONE menu item, the one that we want to select, and select it.
		/// </summary>
		/// <param name="popupTree"></param>
		public TreeNode MakeTargetMenuItem()
		{
			CheckDisposed();

			PopupTree popupTree = GetPopupTree();
			popupTree.Nodes.Clear();
			IMoMorphSynAnalysis msa = m_sense.MorphoSyntaxAnalysisRA;
			if (msa == null)
			{
				// CreateEmptyMSA during an Undo can crash out,
				// or cause the Undo (e.g. merge senses) not to complete. (cf. LT-7776)
				//CreateEmptyMsa();
				//msa = m_sense.MorphoSyntaxAnalysisRA;
				//if (msa == null)
					return null;
			}
			int hvoTarget = msa.Hvo;
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			TreeNode match = AddTreeNodeForMsa(popupTree, tsf, msa);
			SelectChosenItem(match, popupTree);
			return match;
		}

		/// <summary>
		/// NOTE that this implementation IGNORES hvoTarget and selects the MSA indicated by the sense.
		/// </summary>
		/// <param name="popupTree"></param>
		/// <param name="hvoTarget"></param>
		/// <returns></returns>
		protected override TreeNode MakeMenuItems(PopupTree popupTree, int hvoTarget)
		{
			Debug.Assert(m_sense != null);
			hvoTarget = m_sense.MorphoSyntaxAnalysisRAHvo;
			TreeNode match = null;
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			bool fStem = m_sense.GetDesiredMsaType() == MsaType.kStem;
			if (fStem /*m_sense.Entry.MorphoSyntaxAnalysesOC.Count != 0*/)
			{
				// We want the order to be:
				// 1. current msa items
				// 2. possible Parts of Speech
				// 3. "not sure" items
				// We also want the Parts of Speech to be sorted, but not the whole tree.
				// First add the part of speech items (which may be a tree...).
				int tagName = (int)CmPossibility.CmPossibilityTags.kflidName;
				// make sure they are sorted
				popupTree.Sorted = true;
				AddNodes(popupTree.Nodes, List.Hvo,
					(int)CmPossibilityList.CmPossibilityListTags.kflidPossibilities, 0, tagName);
				// reset the sorted flag - we only want the parts of speech to be sorted.
				popupTree.Sorted = false;
				// Remember the (sorted) nodes in an array (so we can use the AddRange() method).
				TreeNode[] posArray = new TreeNode[popupTree.Nodes.Count];
				popupTree.Nodes.CopyTo(posArray, 0);
				// now clear out the nodes so we can get the order we want
				popupTree.Nodes.Clear();

				// Add the existing MSA items for the sense's owning entry.
				foreach (IMoMorphSynAnalysis msa in m_sense.Entry.MorphoSyntaxAnalysesOC)
				{
					HvoTreeNode node = AddTreeNodeForMsa(popupTree, tsf, msa);
					if (msa.Hvo == hvoTarget)
						match = node;
				}
				AddTimberLine(popupTree);

				// now add the sorted parts of speech
				popupTree.Nodes.AddRange(posArray);

				AddTimberLine(popupTree);

				//	1. "<Not Sure>" to produce a negligible Msa reference.
				//	2. "More..." command to launch category chooser dialog.
				TreeNode empty = AddNotSureItem(popupTree, hvoTarget);
				if (match == null)
					match = empty;
				AddMoreItem(popupTree);
			}
			else
			{
				int cMsa = m_sense.Entry.MorphoSyntaxAnalysesOC.Count;
				if (cMsa == 0)
				{
					//	1. "<Not Sure>" to produce a negligible Msa reference.
					//	2. "Specify..." command.
					//Debug.Assert(hvoTarget == 0);
					match = AddNotSureItem(popupTree, hvoTarget);
					popupTree.Nodes.Add(new HvoTreeNode(Cache.MakeUserTss(m_sSpecifyGramFunc), kCreate));
				}
				else
				{
					// 1. Show the current Msa at the top.
					// 2. "Modify ..." command.
					// 3. Show other existing Msas next (if any).
					// 4. <Not Sure> to produce a negligible Msa reference.
					// 5. "Specify different..." command.
					hvoTarget = m_sense.MorphoSyntaxAnalysisRAHvo;
					if (hvoTarget != 0)
					{
						ITsString tssLabel = m_sense.MorphoSyntaxAnalysisRA.InterlinearNameTSS;
						HvoTreeNode node = new HvoTreeNode(tssLabel, hvoTarget);
						popupTree.Nodes.Add(node);
						match = node;
						popupTree.Nodes.Add(new HvoTreeNode(Cache.MakeUserTss(m_sModifyGramFunc), kModify));
						AddTimberLine(popupTree);
					}
					int cMsaExtra = 0;
					foreach (IMoMorphSynAnalysis msa in m_sense.Entry.MorphoSyntaxAnalysesOC)
					{
						if (msa.Hvo == hvoTarget)
							continue;
						ITsString tssLabel = msa.InterlinearNameTSS;
						HvoTreeNode node = new HvoTreeNode(tssLabel, msa.Hvo);
						popupTree.Nodes.Add(node);
						++cMsaExtra;
					}
					if (cMsaExtra > 0)
						AddTimberLine(popupTree);
					// Per final decision on LT-5084, don't want <not sure> for affixes.
					//TreeNode empty = AddNotSureItem(popupTree, hvoTarget);
					//if (match == null)
					//    match = empty;
					popupTree.Nodes.Add(new HvoTreeNode(Cache.MakeUserTss(m_sSpecifyDifferent), kCreate));
				}
			}
			return match;
		}

		private HvoTreeNode AddTreeNodeForMsa(PopupTree popupTree, ITsStrFactory tsf, IMoMorphSynAnalysis msa)
		{
			// JohnT: as described in LT-4633, a stem can be given an allomorph that
			// is an affix. So we need some sort of way to handle this.
			//Debug.Assert(msa is MoStemMsa);
			ITsString tssLabel = msa.InterlinearNameTSS;
			if (msa is IMoStemMsa && (msa as IMoStemMsa).PartOfSpeechRAHvo == 0)
				tssLabel = tsf.MakeString(m_sUnknown, Cache.DefaultUserWs);
			HvoTreeNode node = new HvoTreeNode(tssLabel, msa.Hvo);
			popupTree.Nodes.Add(node);
			return node;
		}

		protected override void m_treeCombo_AfterSelect(object sender, TreeViewEventArgs e)
		{
			HvoTreeNode selectedNode = e.Node as HvoTreeNode;

			// Launch dialog only by a mouse click (or simulated mouse click).
			if (selectedNode != null && selectedNode.Hvo == kMore && e.Action == TreeViewAction.ByMouse)
			{
				if (ChooseFromMasterCategoryList())
					return;
			}
			else if (selectedNode != null && selectedNode.Hvo == kCreate && e.Action == TreeViewAction.ByMouse)
			{
				if (AddNewMsa())
					return;
			}
			else if (selectedNode != null && selectedNode.Hvo == kModify && e.Action == TreeViewAction.ByMouse)
			{
				if (EditExistingMsa())
					return;
			}
			else if (selectedNode != null && selectedNode.Hvo == kEmpty && e.Action == TreeViewAction.ByMouse)
			{
				SwitchToEmptyMsa();
				return;
			}
			base.m_treeCombo_AfterSelect(sender, e);
		}

		private void SwitchToEmptyMsa()
		{
			CreateEmptyMsa();
			LoadPopupTree(m_sense.MorphoSyntaxAnalysisRAHvo);
		}

		private void CreateEmptyMsa()
		{
			DummyGenericMSA dummyMsa = new DummyGenericMSA();
			dummyMsa.MsaType = m_sense.GetDesiredMsaType();
			// To make it fully 'not sure' we must discard knowledge of affix type.
			if (dummyMsa.MsaType == MsaType.kInfl || dummyMsa.MsaType == MsaType.kDeriv)
				dummyMsa.MsaType = MsaType.kUnclassified;
			Cache.BeginUndoTask(String.Format(LexTextControls.ksUndoSetX, FieldName),
				String.Format(LexTextControls.ksRedoSetX, FieldName));
			(m_sense as LexSense).DummyMSA = dummyMsa;
			Cache.EndUndoTask();
		}

		private bool ChooseFromMasterCategoryList()
		{
			PopupTree pt = GetPopupTree();
			// Force the PopupTree to Hide() to trigger popupTree_PopupTreeClosed().
			// This will effectively revert the list selection to a previous confirmed state.
			// Whatever happens below, we don't want to actually leave the "More..." node selected!
			// This is at least required if the user selects "Cancel" from the dialog below.
			pt.Hide();
			using (MasterCategoryListDlg dlg = new MasterCategoryListDlg())
			{
				dlg.SetDlginfo(List, m_mediator, false, null);
				switch (dlg.ShowDialog(ParentForm))
				{
				case DialogResult.OK:
					DummyGenericMSA dummyMSA = new DummyGenericMSA();
					dummyMSA.MainPOS = dlg.SelectedPOS.Hvo;
					dummyMSA.MsaType = m_sense.GetDesiredMsaType();
					Cache.BeginUndoTask(String.Format(LexTextControls.ksUndoSetX, FieldName),
						String.Format(LexTextControls.ksRedoSetX, FieldName));
					(m_sense as LexSense).DummyMSA = dummyMSA;
					Cache.EndUndoTask();
					LoadPopupTree(m_sense.MorphoSyntaxAnalysisRAHvo);
					// everything should be setup with new node selected, so return.
					return true;
				case DialogResult.Yes:
					// Post a message so that we jump to Grammar(area)/Categories tool.
					// Do this before we close any parent dialog in case
					// the parent wants to check to see if such a Jump is pending.
					// NOTE: We use PostMessage here, rather than SendMessage which
					// disposes of the PopupTree before we and/or our parents might
					// be finished using it (cf. LT-2563).
					m_mediator.PostMessage("FollowLink",
						SIL.FieldWorks.FdoUi.FwLink.Create("posEdit", Cache.GetGuidFromId(dlg.SelectedPOS.Hvo),
						Cache.ServerName,
						Cache.DatabaseName));
					if (ParentForm != null && ParentForm.Modal)
					{
						// Close the dlg that opened the master POS dlg,
						// since its hotlink was used to close it,
						// and a new POS has been created.
						ParentForm.DialogResult = DialogResult.Cancel;
						ParentForm.Close();
					}
					return false;
				default:
					// NOTE: If the user has selected "Cancel", then don't change
					// our m_lastConfirmedNode to the "More..." node. Keep it
					// the value set by popupTree_PopupTreeClosed() when we
					// called pt.Hide() above. (cf. comments in LT-2522)
					return false;
				}
			}
		}

		private bool AddNewMsa()
		{
			PopupTree pt = GetPopupTree();
			// Force the PopupTree to Hide() to trigger popupTree_PopupTreeClosed().
			// This will effectively revert the list selection to a previous confirmed state.
			// Whatever happens below, we don't want to actually leave the "Specify ..." node selected!
			// This is at least required if the user selects "Cancel" from the dialog below.
			pt.Hide();
			using (MsaCreatorDlg dlg = new MsaCreatorDlg())
			{
				DummyGenericMSA dummyMsa = new DummyGenericMSA();
				dummyMsa.MsaType = m_sense.GetDesiredMsaType();
				dlg.SetDlgInfo(Cache, m_persistProvider, m_mediator, m_sense.Entry, dummyMsa, 0, false, null);
				if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
				{
					Cache.BeginUndoTask(String.Format(LexTextControls.ksUndoSetX, FieldName),
						String.Format(LexTextControls.ksRedoSetX, FieldName));
					(m_sense as LexSense).DummyMSA = dlg.DummyMSA;
					Cache.EndUndoTask();
					LoadPopupTree(m_sense.MorphoSyntaxAnalysisRAHvo);
					return true;
				}
			}
			return false;
		}

		private bool EditExistingMsa()
		{
			PopupTree pt = GetPopupTree();
			// Force the PopupTree to Hide() to trigger popupTree_PopupTreeClosed().
			// This will effectively revert the list selection to a previous confirmed state.
			// Whatever happens below, we don't want to actually leave the "Modify ..." node selected!
			// This is at least required if the user selects "Cancel" from the dialog below.
			pt.Hide();
			DummyGenericMSA dummyMsa = DummyGenericMSA.Create(m_sense.MorphoSyntaxAnalysisRA);
			using (MsaCreatorDlg dlg = new MsaCreatorDlg())
			{
				dlg.SetDlgInfo(Cache, m_persistProvider, m_mediator, m_sense.Entry, dummyMsa,
					m_sense.MorphoSyntaxAnalysisRAHvo, true, m_sEditGramFunc);
				if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
				{
					Cache.BeginUndoTask(String.Format(LexTextControls.ksUndoSetX, FieldName),
						String.Format(LexTextControls.ksRedoSetX, FieldName));
					(m_sense as LexSense).DummyMSA = dlg.DummyMSA;
					Cache.EndUndoTask();
					LoadPopupTree(m_sense.MorphoSyntaxAnalysisRAHvo);
					return true;
				}
			}
			return false;
		}
	}
}
