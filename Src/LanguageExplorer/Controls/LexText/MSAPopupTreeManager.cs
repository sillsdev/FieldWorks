// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Controls.LexText
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
		// The following strings are loaded from the string table if possible.
		private string m_sUnknown;
		private string m_sSpecifyGramFunc;
		private string m_sModifyGramFunc;
		private string m_sSpecifyDifferent;
		private string m_sCreateGramFunc;
		private string m_sEditGramFunc;
		#endregion Data members

		/// <summary>
		/// Constructor.
		/// </summary>
		public MSAPopupTreeManager(TreeCombo treeCombo, LcmCache cache, ICmPossibilityList list, int ws, bool useAbbr, IPropertyTable propertyTable, IPublisher publisher, Form parent)
			: base(treeCombo, cache, propertyTable, publisher, list, ws, useAbbr, parent)
		{
			LoadStrings();
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public MSAPopupTreeManager(PopupTree popupTree, LcmCache cache, ICmPossibilityList list, int ws, bool useAbbr, IPropertyTable propertyTable, IPublisher publisher, Form parent)
			: base(popupTree, cache, propertyTable, publisher, list, ws, useAbbr, parent)
		{
			LoadStrings();
		}

		public ILexSense Sense { get; set; }

		public string FieldName { get; set; } = string.Empty;

		public IPersistenceProvider PersistenceProvider { get; set; }

		private void LoadStrings()
		{
			// Load the special strings from the string table.
			m_sUnknown = StringTable.Table.GetString("NullItemLabel", "DetailControls/MSAReferenceComboBox");
			m_sSpecifyGramFunc = StringTable.Table.GetString("AddNewGramFunc", "DetailControls/MSAReferenceComboBox");
			m_sModifyGramFunc = StringTable.Table.GetString("ModifyGramFunc", "DetailControls/MSAReferenceComboBox");
			m_sSpecifyDifferent = StringTable.Table.GetString("SpecifyDifferentGramFunc", "DetailControls/MSAReferenceComboBox");
			m_sCreateGramFunc = StringTable.Table.GetString("CreateGramFunc", "DetailControls/MSAReferenceComboBox");
			m_sEditGramFunc = StringTable.Table.GetString("EditGramFunc", "DetailControls/MSAReferenceComboBox");
			if (string.IsNullOrEmpty(m_sUnknown) || m_sUnknown == "*NullItemLabel*")
			{
				m_sUnknown = LexTextControls.ks_NotSure_;
			}
			if (string.IsNullOrEmpty(m_sSpecifyGramFunc) || m_sSpecifyGramFunc == "*AddNewGramFunc*")
			{
				m_sSpecifyGramFunc = LexTextControls.ksSpecifyGrammaticalInfo_;
			}
			if (string.IsNullOrEmpty(m_sModifyGramFunc) || m_sModifyGramFunc == "*ModifyGramFunc*")
			{
				m_sModifyGramFunc = LexTextControls.ksModifyThisGrammaticalInfo_;
			}
			if (string.IsNullOrEmpty(m_sSpecifyDifferent) || m_sSpecifyDifferent == "*SpecifyDifferentGramFunc*")
			{
				m_sSpecifyDifferent = LexTextControls.ksSpecifyDifferentGrammaticalInfo_;
			}
			if (string.IsNullOrEmpty(m_sCreateGramFunc) || m_sCreateGramFunc == "*CreateGramFuncGramFunc*")
			{
				m_sCreateGramFunc = LexTextControls.ksCreateNewGrammaticalInfo;
			}
			if (string.IsNullOrEmpty(m_sEditGramFunc) || m_sEditGramFunc == "*EditGramFuncGramFunc*")
			{
				m_sEditGramFunc = LexTextControls.ksEditGrammaticalInfo;
			}
		}

		/// <summary>
		/// Populate the tree with just ONE menu item, the one that we want to select.
		/// </summary>
		public TreeNode MakeTargetMenuItem()
		{
			var popupTree = GetPopupTree();
			popupTree.Nodes.Clear();
			var msa = Sense.MorphoSyntaxAnalysisRA;
			return msa == null ? AddNotSureItem(popupTree) : AddTreeNodeForMsa(popupTree, msa);
		}

		/// <summary>
		/// NOTE that this implementation IGNORES hvoTarget and selects the MSA indicated by the sense.
		/// </summary>
		protected override TreeNode MakeMenuItems(PopupTree popupTree, int hvoTarget)
		{
			Debug.Assert(Sense != null);
			hvoTarget = Sense.MorphoSyntaxAnalysisRA?.Hvo ?? 0;
			TreeNode match = null;
			var fStem = Sense.GetDesiredMsaType() == MsaType.kStem;
			if (fStem /*m_sense.Entry.MorphoSyntaxAnalysesOC.Count != 0*/)
			{
				// We want the order to be:
				// 1. current msa items
				// 2. possible Parts of Speech
				// 3. "not sure" items
				// We also want the Parts of Speech to be sorted, but not the whole tree.
				// First add the part of speech items (which may be a tree...).
				const int tagName = CmPossibilityTags.kflidName;
				// make sure they are sorted
				popupTree.Sorted = true;
				AddNodes(popupTree.Nodes, List.Hvo, CmPossibilityListTags.kflidPossibilities, 0, tagName);
				// reset the sorted flag - we only want the parts of speech to be sorted.
				popupTree.Sorted = false;
				// Remember the (sorted) nodes in an array (so we can use the AddRange() method).
				var posArray = new TreeNode[popupTree.Nodes.Count];
				popupTree.Nodes.CopyTo(posArray, 0);
				// now clear out the nodes so we can get the order we want
				popupTree.Nodes.Clear();

				// Add the existing MSA items for the sense's owning entry.
				foreach (var msa in Sense.Entry.MorphoSyntaxAnalysesOC)
				{
					var node = AddTreeNodeForMsa(popupTree, msa);
					if (msa.Hvo == hvoTarget)
					{
						match = node;
					}
				}
				AddTimberLine(popupTree);

				// now add the sorted parts of speech
				popupTree.Nodes.AddRange(posArray);

				AddTimberLine(popupTree);

				//	1. "<Not Sure>" to produce a negligible Msa reference.
				//	2. "More..." command to launch category chooser dialog.
				var empty = AddNotSureItem(popupTree);
				if (match == null)
				{
					match = empty;
				}
				AddMoreItem(popupTree);
			}
			else
			{
				var cMsa = Sense.Entry.MorphoSyntaxAnalysesOC.Count;
				if (cMsa == 0)
				{
					//	1. "<Not Sure>" to produce a negligible Msa reference.
					//	2. "Specify..." command.
					//Debug.Assert(hvoTarget == 0);
					match = AddNotSureItem(popupTree);
					popupTree.Nodes.Add(new HvoTreeNode(TsStringUtils.MakeString(m_sSpecifyGramFunc, Cache.WritingSystemFactory.UserWs), kCreate));
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
					if (Sense.MorphoSyntaxAnalysisRA != null)
					{
						hvoTarget = Sense.MorphoSyntaxAnalysisRA.Hvo;
					}
					if (hvoTarget != 0)
					{
						var tssLabel = Sense.MorphoSyntaxAnalysisRA.InterlinearNameTSS;
						var node = new HvoTreeNode(tssLabel, hvoTarget);
						popupTree.Nodes.Add(node);
						match = node;
						popupTree.Nodes.Add(new HvoTreeNode(TsStringUtils.MakeString(m_sModifyGramFunc, Cache.WritingSystemFactory.UserWs), kModify));
						AddTimberLine(popupTree);
					}
					var cMsaExtra = 0;
					foreach (var msa in Sense.Entry.MorphoSyntaxAnalysesOC)
					{
						if (msa.Hvo == hvoTarget)
						{
							continue;
						}
						var tssLabel = msa.InterlinearNameTSS;
						var node = new HvoTreeNode(tssLabel, msa.Hvo);
						popupTree.Nodes.Add(node);
						++cMsaExtra;
					}

					if (cMsaExtra > 0)
					{
						AddTimberLine(popupTree);
					}
					popupTree.Nodes.Add(new HvoTreeNode(TsStringUtils.MakeString(m_sSpecifyDifferent, Cache.WritingSystemFactory.UserWs), kCreate));
				}
			}
			return match;
		}

		private HvoTreeNode AddTreeNodeForMsa(PopupTree popupTree, IMoMorphSynAnalysis msa)
		{
			// JohnT: as described in LT-4633, a stem can be given an allomorph that
			// is an affix. So we need some sort of way to handle this.
			var tssLabel = msa.InterlinearNameTSS;
			var stemMsa = msa as IMoStemMsa;
			if (stemMsa != null && stemMsa.PartOfSpeechRA == null)
			{
				tssLabel = TsStringUtils.MakeString(m_sUnknown, Cache.ServiceLocator.WritingSystemManager.UserWs);
			}
			var node = new HvoTreeNode(tssLabel, msa.Hvo);
			popupTree.Nodes.Add(node);
			return node;
		}

		protected override void m_treeCombo_AfterSelect(object sender, TreeViewEventArgs e)
		{
			var selectedNode = e.Node as HvoTreeNode;
			// Launch dialog only by a mouse click (or simulated mouse click).
			if (selectedNode != null && selectedNode.Hvo == kMore && e.Action == TreeViewAction.ByMouse)
			{
				ChooseFromMasterCategoryList();
			}
			else if (selectedNode != null && selectedNode.Hvo == kCreate && e.Action == TreeViewAction.ByMouse)
			{
				if (AddNewMsa())
				{
					return;
				}
			}
			else if (selectedNode != null && selectedNode.Hvo == kModify && e.Action == TreeViewAction.ByMouse)
			{
				if (EditExistingMsa())
				{
					return;
				}
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
			LoadPopupTree(Sense.MorphoSyntaxAnalysisRA.Hvo);
		}

		private void CreateEmptyMsa()
		{
			var dummyMsa = new SandboxGenericMSA { MsaType = Sense.GetDesiredMsaType() };
			// To make it fully 'not sure' we must discard knowledge of affix type.
			if (dummyMsa.MsaType == MsaType.kInfl || dummyMsa.MsaType == MsaType.kDeriv)
			{
				dummyMsa.MsaType = MsaType.kUnclassified;
			}
			UndoableUnitOfWorkHelper.Do(string.Format(LexTextControls.ksUndoSetX, FieldName), string.Format(LexTextControls.ksRedoSetX, FieldName), Sense, () =>
			{
				Sense.SandboxMSA = dummyMsa;
			});
		}

		private void ChooseFromMasterCategoryList()
		{
			var pt = GetPopupTree();
			// Force the PopupTree to Hide() to trigger popupTree_PopupTreeClosed().
			// This will effectively revert the list selection to a previous confirmed state.
			// Whatever happens below, we don't want to actually leave the "More..." node selected!
			// This is at least required if the user selects "Cancel" from the dialog below.
			if (Sense.MorphoSyntaxAnalysisRA != null)
			{
				pt.SelectObjWithoutTriggeringBeforeAfterSelects(Sense.MorphoSyntaxAnalysisRA.Hvo);
			}
			// FWR-3542 -- Need this in .Net too, or it eats the first mouse click intended
			// for the dialog we're about to show below.
			pt.HideForm();

			// The constructor adds an Application.Idle handler, which when run, removes the handler
			new MasterCategoryListChooserLauncher(ParentForm, m_propertyTable, m_publisher, List, FieldName, Sense);
		}

		private bool AddNewMsa()
		{
			var pt = GetPopupTree();
			// Force the PopupTree to Hide() to trigger popupTree_PopupTreeClosed().
			// This will effectively revert the list selection to a previous confirmed state.
			// Whatever happens below, we don't want to actually leave the "Specify ..." node selected!
			// This is at least required if the user selects "Cancel" from the dialog below.
			if (Sense.MorphoSyntaxAnalysisRA != null)
			{
				pt.SelectObj(Sense.MorphoSyntaxAnalysisRA.Hvo);
			}
#if __MonoCS__
			// If Popup tree is shown whilest the dialog is shown, the first click on the dialog is consumed by the
			// Popup tree, (and closes it down). On .NET the PopupTree appears to be automatically closed.
			pt.HideForm();
#endif
			using (var dlg = new MsaCreatorDlg())
			{
				var dummyMsa = new SandboxGenericMSA { MsaType = Sense.GetDesiredMsaType() };
				dlg.SetDlgInfo(Cache, PersistenceProvider, m_propertyTable, m_publisher, Sense.Entry, dummyMsa, 0, false, null);
				if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
				{
					Cache.DomainDataByFlid.BeginUndoTask(string.Format(LexTextControls.ksUndoSetX, FieldName), string.Format(LexTextControls.ksRedoSetX, FieldName));
					Sense.SandboxMSA = dlg.SandboxMSA;
					Cache.DomainDataByFlid.EndUndoTask();
					LoadPopupTree(Sense.MorphoSyntaxAnalysisRA.Hvo);
					return true;
				}
			}
			return false;
		}

		private bool EditExistingMsa()
		{
			var pt = GetPopupTree();
			// Force the PopupTree to Hide() to trigger popupTree_PopupTreeClosed().
			// This will effectively revert the list selection to a previous confirmed state.
			// Whatever happens below, we don't want to actually leave the "Modify ..." node selected!
			// This is at least required if the user selects "Cancel" from the dialog below.
			if (Sense.MorphoSyntaxAnalysisRA != null)
			{
				pt.SelectObj(Sense.MorphoSyntaxAnalysisRA.Hvo);
			}
#if __MonoCS__
			// If Popup tree is shown whilest the dialog is shown, the first click on the dialog is consumed by the
			// Popup tree, (and closes it down). On .NET the PopupTree appears to be automatically closed.
			pt.HideForm();
#endif
			var dummyMsa = SandboxGenericMSA.Create(Sense.MorphoSyntaxAnalysisRA);
			using (var dlg = new MsaCreatorDlg())
			{
				dlg.SetDlgInfo(Cache, PersistenceProvider, m_propertyTable, m_publisher, Sense.Entry, dummyMsa, Sense.MorphoSyntaxAnalysisRA.Hvo, true, m_sEditGramFunc);
				if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
				{
					Cache.DomainDataByFlid.BeginUndoTask(string.Format(LexTextControls.ksUndoSetX, FieldName), string.Format(LexTextControls.ksRedoSetX, FieldName));
					Sense.SandboxMSA = dlg.SandboxMSA;
					Cache.DomainDataByFlid.EndUndoTask();
					LoadPopupTree(Sense.MorphoSyntaxAnalysisRA.Hvo);
					return true;
				}
			}
			return false;
		}
	}
}
