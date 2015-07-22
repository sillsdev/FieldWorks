using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.DomainServices;
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
		private ILexSense m_sense;
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
			int ws, bool useAbbr, Mediator mediator, PropertyTable propertyTable, Form parent)
			: base(treeCombo, cache, mediator, propertyTable, list, ws, useAbbr, parent)
		{
			LoadStrings();
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public MSAPopupTreeManager(PopupTree popupTree, FdoCache cache, ICmPossibilityList list,
			int ws, bool useAbbr, Mediator mediator, PropertyTable propertyTable, Form parent)
			: base(popupTree, cache, mediator, propertyTable, list, ws, useAbbr, parent)
		{
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
			// Load the special strings from the string table.
			m_sUnknown = StringTable.Table.GetString("NullItemLabel",
				"DetailControls/MSAReferenceComboBox");
			m_sSpecifyGramFunc = StringTable.Table.GetString("AddNewGramFunc",
				"DetailControls/MSAReferenceComboBox");
			m_sModifyGramFunc = StringTable.Table.GetString("ModifyGramFunc",
				"DetailControls/MSAReferenceComboBox");
			m_sSpecifyDifferent = StringTable.Table.GetString("SpecifyDifferentGramFunc",
				"DetailControls/MSAReferenceComboBox");
			m_sCreateGramFunc = StringTable.Table.GetString("CreateGramFunc",
				"DetailControls/MSAReferenceComboBox");
			m_sEditGramFunc = StringTable.Table.GetString("EditGramFunc",
				"DetailControls/MSAReferenceComboBox");
			if (string.IsNullOrEmpty(m_sUnknown) ||
				m_sUnknown == "*NullItemLabel*")
			{
				m_sUnknown = LexTextControls.ks_NotSure_;
			}
			if (string.IsNullOrEmpty(m_sSpecifyGramFunc) ||
				m_sSpecifyGramFunc == "*AddNewGramFunc*")
			{
				m_sSpecifyGramFunc = LexTextControls.ksSpecifyGrammaticalInfo_;
			}
			if (string.IsNullOrEmpty(m_sModifyGramFunc) ||
				m_sModifyGramFunc == "*ModifyGramFunc*")
			{
				m_sModifyGramFunc = LexTextControls.ksModifyThisGrammaticalInfo_;
			}
			if (string.IsNullOrEmpty(m_sSpecifyDifferent) ||
				m_sSpecifyDifferent == "*SpecifyDifferentGramFunc*")
			{
				m_sSpecifyDifferent = LexTextControls.ksSpecifyDifferentGrammaticalInfo_;
			}
			if (string.IsNullOrEmpty(m_sCreateGramFunc) ||
				m_sCreateGramFunc == "*CreateGramFuncGramFunc*")
			{
				m_sCreateGramFunc = LexTextControls.ksCreateNewGrammaticalInfo;
			}
			if (string.IsNullOrEmpty(m_sEditGramFunc) ||
				m_sEditGramFunc == "*EditGramFuncGramFunc*")
			{
				m_sEditGramFunc = LexTextControls.ksEditGrammaticalInfo;
			}
		}

		/// <summary>
		/// Populate the tree with just ONE menu item, the one that we want to select.
		/// </summary>
		public TreeNode MakeTargetMenuItem()
		{
			CheckDisposed();

			PopupTree popupTree = GetPopupTree();
			popupTree.Nodes.Clear();
			var msa = m_sense.MorphoSyntaxAnalysisRA;
			TreeNode match = null;
			if (msa == null)
				match = AddNotSureItem(popupTree);
			else
				match = AddTreeNodeForMsa(popupTree, m_sense.Cache.TsStrFactory, msa);
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
			hvoTarget = m_sense.MorphoSyntaxAnalysisRA == null ? 0 : m_sense.MorphoSyntaxAnalysisRA.Hvo;
			TreeNode match = null;
			ITsStrFactory tsf = Cache.TsStrFactory;
			bool fStem = m_sense.GetDesiredMsaType() == MsaType.kStem;
			if (fStem /*m_sense.Entry.MorphoSyntaxAnalysesOC.Count != 0*/)
			{
				// We want the order to be:
				// 1. current msa items
				// 2. possible Parts of Speech
				// 3. "not sure" items
				// We also want the Parts of Speech to be sorted, but not the whole tree.
				// First add the part of speech items (which may be a tree...).
				int tagName = CmPossibilityTags.kflidName;
				// make sure they are sorted
				popupTree.Sorted = true;
				AddNodes(popupTree.Nodes, List.Hvo,
					CmPossibilityListTags.kflidPossibilities, 0, tagName);
				// reset the sorted flag - we only want the parts of speech to be sorted.
				popupTree.Sorted = false;
				// Remember the (sorted) nodes in an array (so we can use the AddRange() method).
				TreeNode[] posArray = new TreeNode[popupTree.Nodes.Count];
				popupTree.Nodes.CopyTo(posArray, 0);
				// now clear out the nodes so we can get the order we want
				popupTree.Nodes.Clear();

				// Add the existing MSA items for the sense's owning entry.
				foreach (var msa in m_sense.Entry.MorphoSyntaxAnalysesOC)
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
				TreeNode empty = AddNotSureItem(popupTree);
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
					match = AddNotSureItem(popupTree);
					popupTree.Nodes.Add(new HvoTreeNode(Cache.TsStrFactory.MakeString(m_sSpecifyGramFunc, Cache.WritingSystemFactory.UserWs), kCreate));
				}
				else
				{
					// 1. Show the current Msa at the top.
					// 2. "Modify ..." command.
					// 3. Show other existing Msas next (if any).
					// 4. <Not Sure> to produce a negligible Msa reference.
					// 5. "Specify different..." command.
					hvoTarget = 0;
					// We should always have an MSA assigned to every sense, but sometimes this
					// hasn't happened.  Don't crash if the data isn't quite correct.  See FWR-3090.
					if (m_sense.MorphoSyntaxAnalysisRA != null)
						hvoTarget = m_sense.MorphoSyntaxAnalysisRA.Hvo;
					if (hvoTarget != 0)
					{
						ITsString tssLabel = m_sense.MorphoSyntaxAnalysisRA.InterlinearNameTSS;
						HvoTreeNode node = new HvoTreeNode(tssLabel, hvoTarget);
						popupTree.Nodes.Add(node);
						match = node;
						popupTree.Nodes.Add(new HvoTreeNode(Cache.TsStrFactory.MakeString(m_sModifyGramFunc, Cache.WritingSystemFactory.UserWs), kModify));
						AddTimberLine(popupTree);
					}
					int cMsaExtra = 0;
					foreach (var msa in m_sense.Entry.MorphoSyntaxAnalysesOC)
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
					popupTree.Nodes.Add(new HvoTreeNode(Cache.TsStrFactory.MakeString(m_sSpecifyDifferent, Cache.WritingSystemFactory.UserWs), kCreate));
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
			if (msa is IMoStemMsa && (msa as IMoStemMsa).PartOfSpeechRA == null)
				tssLabel = tsf.MakeString(
					m_sUnknown,
					Cache.ServiceLocator.WritingSystemManager.UserWs);
			var node = new HvoTreeNode(tssLabel, msa.Hvo);
			popupTree.Nodes.Add(node);
			return node;
		}

		protected override void m_treeCombo_AfterSelect(object sender, TreeViewEventArgs e)
		{
			HvoTreeNode selectedNode = e.Node as HvoTreeNode;

			// Launch dialog only by a mouse click (or simulated mouse click).
			if (selectedNode != null && selectedNode.Hvo == kMore && e.Action == TreeViewAction.ByMouse)
			{
				ChooseFromMasterCategoryList();
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
			LoadPopupTree(m_sense.MorphoSyntaxAnalysisRA.Hvo);
		}

		private void CreateEmptyMsa()
		{
			SandboxGenericMSA dummyMsa = new SandboxGenericMSA();
			dummyMsa.MsaType = m_sense.GetDesiredMsaType();
			// To make it fully 'not sure' we must discard knowledge of affix type.
			if (dummyMsa.MsaType == MsaType.kInfl || dummyMsa.MsaType == MsaType.kDeriv)
				dummyMsa.MsaType = MsaType.kUnclassified;
			UndoableUnitOfWorkHelper.Do(string.Format(LexTextControls.ksUndoSetX, FieldName),
				string.Format(LexTextControls.ksRedoSetX, FieldName), m_sense, () =>
			{
				m_sense.SandboxMSA = dummyMsa;
			});
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="GetPopupTree() returns a reference")]
		private void ChooseFromMasterCategoryList()
		{
			PopupTree pt = GetPopupTree();
			// Force the PopupTree to Hide() to trigger popupTree_PopupTreeClosed().
			// This will effectively revert the list selection to a previous confirmed state.
			// Whatever happens below, we don't want to actually leave the "More..." node selected!
			// This is at least required if the user selects "Cancel" from the dialog below.
			if (m_sense.MorphoSyntaxAnalysisRA != null)
				pt.SelectObjWithoutTriggeringBeforeAfterSelects(m_sense.MorphoSyntaxAnalysisRA.Hvo);
			// FWR-3542 -- Need this in .Net too, or it eats the first mouse click intended
			// for the dialog we're about to show below.
			pt.HideForm();

			new MasterCategoryListChooserLauncher(ParentForm, m_mediator, m_propertyTable, List, FieldName, m_sense);
		}

		private bool AddNewMsa()
		{
			PopupTree pt = GetPopupTree();
			// Force the PopupTree to Hide() to trigger popupTree_PopupTreeClosed().
			// This will effectively revert the list selection to a previous confirmed state.
			// Whatever happens below, we don't want to actually leave the "Specify ..." node selected!
			// This is at least required if the user selects "Cancel" from the dialog below.
			if (m_sense.MorphoSyntaxAnalysisRA != null)
				pt.SelectObj(m_sense.MorphoSyntaxAnalysisRA.Hvo);
#if __MonoCS__
			// If Popup tree is shown whilest the dialog is shown, the first click on the dialog is consumed by the
			// Popup tree, (and closes it down). On .NET the PopupTree appears to be automatically closed.
			pt.HideForm();
#endif
			using (MsaCreatorDlg dlg = new MsaCreatorDlg())
			{
				SandboxGenericMSA dummyMsa = new SandboxGenericMSA();
				dummyMsa.MsaType = m_sense.GetDesiredMsaType();
				dlg.SetDlgInfo(Cache, m_persistProvider, m_mediator, m_propertyTable, m_sense.Entry, dummyMsa, 0, false, null);
				if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
				{
					Cache.DomainDataByFlid.BeginUndoTask(String.Format(LexTextControls.ksUndoSetX, FieldName),
						String.Format(LexTextControls.ksRedoSetX, FieldName));
					m_sense.SandboxMSA = dlg.SandboxMSA;
					Cache.DomainDataByFlid.EndUndoTask();
					LoadPopupTree(m_sense.MorphoSyntaxAnalysisRA.Hvo);
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
			if (m_sense.MorphoSyntaxAnalysisRA != null)
				pt.SelectObj(m_sense.MorphoSyntaxAnalysisRA.Hvo);
#if __MonoCS__
			// If Popup tree is shown whilest the dialog is shown, the first click on the dialog is consumed by the
			// Popup tree, (and closes it down). On .NET the PopupTree appears to be automatically closed.
			pt.HideForm();
#endif
			SandboxGenericMSA dummyMsa = SandboxGenericMSA.Create(m_sense.MorphoSyntaxAnalysisRA);
			using (MsaCreatorDlg dlg = new MsaCreatorDlg())
			{
				dlg.SetDlgInfo(Cache, m_persistProvider, m_mediator, m_propertyTable, m_sense.Entry, dummyMsa,
					m_sense.MorphoSyntaxAnalysisRA.Hvo, true, m_sEditGramFunc);
				if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
				{
					Cache.DomainDataByFlid.BeginUndoTask(String.Format(LexTextControls.ksUndoSetX, FieldName),
						String.Format(LexTextControls.ksRedoSetX, FieldName));
					m_sense.SandboxMSA = dlg.SandboxMSA;
					Cache.DomainDataByFlid.EndUndoTask();
					LoadPopupTree(m_sense.MorphoSyntaxAnalysisRA.Hvo);
					return true;
				}
			}
			return false;
		}
	}

	/// <summary>
	/// This is an attempt to avoid LT-11548 where the MSAPopupTreeManager was being disposed
	/// under certain circumstances while it was still processing AfterSelect messages.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_parentOfPopupMgr, m_mediator, and Cache are references")]
	public class MasterCategoryListChooserLauncher
	{
		private readonly ILexSense m_sense;
		private readonly Form m_parentOfPopupMgr;
		private readonly Mediator m_mediator;
		private readonly PropertyTable m_propertyTable;
		private readonly string m_field;

		public MasterCategoryListChooserLauncher(Form popupMgrParent, Mediator mediator, PropertyTable propertyTable,
			ICmPossibilityList possibilityList, string fieldName, ILexSense sense)
		{
			m_parentOfPopupMgr = popupMgrParent;
			m_mediator = mediator;
			m_propertyTable = propertyTable;
			CategoryList = possibilityList;
			m_sense = sense;
			FieldName = fieldName;
			Cache = m_sense.Cache;

			Application.Idle += new EventHandler(LaunchChooseFromMasterCategoryListOnIdle);
		}

		public ICmPossibilityList CategoryList { get; private set; }
		public string FieldName { get; private set; }
		public FdoCache Cache { get; private set; }

		void LaunchChooseFromMasterCategoryListOnIdle(object sender, EventArgs e)
		{
			Application.Idle -= LaunchChooseFromMasterCategoryListOnIdle; // now being handled

			// now launch the dialog
			using (MasterCategoryListDlg dlg = new MasterCategoryListDlg())
			{
				dlg.SetDlginfo(CategoryList, m_mediator, m_propertyTable, false, null);
				switch (dlg.ShowDialog(m_parentOfPopupMgr))
				{
					case DialogResult.OK:
						var sandboxMsa = new SandboxGenericMSA();
						sandboxMsa.MainPOS = dlg.SelectedPOS;
						sandboxMsa.MsaType = m_sense.GetDesiredMsaType();
						UndoableUnitOfWorkHelper.Do(String.Format(LexTextControls.ksUndoSetX, FieldName),
							String.Format(LexTextControls.ksRedoSetX, FieldName), m_sense, () =>
							{
								m_sense.SandboxMSA = sandboxMsa;
							});
						// Under certain circumstances (LT-11548) 'this' was disposed during the EndUndotask above!
						// That's why we're now launching this on idle.
						// Here's hoping we can get away without doing this! (It doesn't seem to make a difference.)
						//LoadPopupTree(m_sense.MorphoSyntaxAnalysisRA.Hvo);
						// everything should be setup with new node selected, so return.
						break;
					case DialogResult.Yes:
						// represents a click on the link to create a new Grammar Category.
						// Post a message so that we jump to Grammar(area)/Categories tool.
						// Do this before we close any parent dialog in case
						// the parent wants to check to see if such a Jump is pending.
						// NOTE: We use PostMessage here, rather than SendMessage which
						// disposes of the PopupTree before we and/or our parents might
						// be finished using it (cf. LT-2563).
						m_mediator.PostMessage("FollowLink", new FwLinkArgs("posEdit", dlg.SelectedPOS.Guid));
						if (m_parentOfPopupMgr != null && m_parentOfPopupMgr.Modal)
						{
							// Close the dlg that opened the master POS dlg,
							// since its hotlink was used to close it,
							// and a new POS has been created.
							m_parentOfPopupMgr.DialogResult = DialogResult.Cancel;
							m_parentOfPopupMgr.Close();
						}
						break;
					default:
						// NOTE: If the user has selected "Cancel", then don't change
						// our m_lastConfirmedNode to the "More..." node. Keep it
						// the value set by popupTree_PopupTreeClosed() when we
						// called pt.Hide() above. (cf. comments in LT-2522)
						break;
				}
			}
		}
	}
}
