// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Areas;
using LanguageExplorer.Controls.DetailControls.Resources;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.PlatformUtilities;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class MSAReferenceComboBoxSlice : FieldSlice, IVwNotifyChange
	{
		private IPersistenceProvider m_persistProvider;
		private MSAPopupTreeManager m_MSAPopupTreeManager;
		private TreeCombo m_tree;
		private int m_treeBaseWidth;
		private bool m_handlingMessage;

		/// <summary>
		/// Constructor.
		/// </summary>
		public MSAReferenceComboBoxSlice(LcmCache cache, ICmObject obj, int flid, IPersistenceProvider persistenceProvider)
			: base(new UserControl(), cache, obj, flid)
		{
			var defAnalWs = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			m_persistProvider = persistenceProvider;
			m_tree = new TreeCombo
			{
				WritingSystemFactory = cache.WritingSystemFactory,
				Font = new System.Drawing.Font(defAnalWs.DefaultFontName, 10),
				WritingSystemCode = defAnalWs.Handle,
				// We embed the tree combo in a layer of UserControl, so it can have a fixed width
				// while the parent window control is, as usual, docked 'fill' to work with the splitter.
				Dock = DockStyle.Left,
				Width = 240,
			};
			if (!Application.RenderWithVisualStyles)
			{
				m_tree.HasBorder = false;
			}
			m_tree.DropDown += m_tree_DropDown;
			Control.Controls.Add(m_tree);
			m_tree.SizeChanged += m_tree_SizeChanged;
			Cache?.DomainDataByFlid.AddNotification(this);
			m_treeBaseWidth = m_tree.Width;
			// m_tree has sensible PreferredHeight once the text is set, UserControl does not.
			//we need to set the Height after m_tree.Text has a value set to it.
			Control.Height = m_tree.PreferredHeight;
		}

		#region Overrides of Slice

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			//Set the stylesheet so that the font size for the...
			IVwStylesheet stylesheet = FwUtils.StyleSheetFromPropertyTable(PropertyTable);
			m_tree.StyleSheet = stylesheet;
			var list = Cache.LanguageProject.PartsOfSpeechOA;
			m_MSAPopupTreeManager = new MSAPopupTreeManager(m_tree, Cache, list, m_tree.WritingSystemCode, true, flexComponentParameters, PropertyTable.GetValue<Form>(FwUtils.window));
			m_MSAPopupTreeManager.AfterSelect += m_MSAPopupTreeManager_AfterSelect;
			m_MSAPopupTreeManager.Sense = MyCmObject as ILexSense;
			m_MSAPopupTreeManager.PersistenceProvider = m_persistProvider;
			try
			{
				m_handlingMessage = true;
				m_tree.SelectedNode = m_MSAPopupTreeManager.MakeTargetMenuItem();
			}
			finally
			{
				m_handlingMessage = false;
			}
		}

		#endregion

		/// <summary>
		/// Make the slice tall enough to hold the tree combo's internal textbox at a
		/// comfortable size.
		/// </summary>
		void m_tree_SizeChanged(object sender, EventArgs e)
		{
			Height = Math.Max(Height, m_tree.PreferredHeight);
		}

		public override void Install(DataTree parentDataTree)
		{
			base.Install(parentDataTree);
			SplitCont.Panel2.SizeChanged += SplitContPanel2_SizeChanged;
		}

		private void SplitContPanel2_SizeChanged(object sender, EventArgs e)
		{
			var dxPanelWidth = SplitCont.Panel2.Width;
			if ((dxPanelWidth < m_tree.Width && dxPanelWidth >= 80) || (dxPanelWidth > m_tree.Width && dxPanelWidth <= m_treeBaseWidth))
			{
				m_tree.Width = dxPanelWidth;
			}
			else if (m_tree.Width != m_treeBaseWidth && dxPanelWidth >= 80)
			{
				m_tree.Width = Math.Min(m_treeBaseWidth, dxPanelWidth);
			}
		}

		private void m_tree_DropDown(object sender, EventArgs e)
		{
			m_MSAPopupTreeManager.LoadPopupTree(0); // load the tree for real, with up-to-date list of available MSAs (see LT-5041).
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				if (SplitCont != null && !SplitCont.IsDisposed && SplitCont.Panel2 != null && !SplitCont.Panel2.IsDisposed)
				{
					SplitCont.Panel2.SizeChanged -= SplitContPanel2_SizeChanged;
				}
				Cache?.DomainDataByFlid.RemoveNotification(this);
				if (m_tree != null && m_tree.Parent == null)
				{
					m_tree.Dispose();
				}
				if (m_MSAPopupTreeManager != null)
				{
					m_MSAPopupTreeManager.AfterSelect -= m_MSAPopupTreeManager_AfterSelect;
					m_MSAPopupTreeManager.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_tree = null;
			m_MSAPopupTreeManager = null;
			m_persistProvider = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Override FieldSlice method because UpdateDisplayFromDatabase has too many code paths with
		/// recursive side-effects.
		/// </summary>
		protected internal override bool UpdateDisplayIfNeeded(int hvo, int tag)
		{
			if (tag != Flid)
			{
				return false;
			}
			m_handlingMessage = true;
			try
			{
				var sense = MyCmObject as ILexSense;
				if (sense.MorphoSyntaxAnalysisRA != null)
				{
					m_MSAPopupTreeManager.LoadPopupTree(sense.MorphoSyntaxAnalysisRA.Hvo);
				}
				ContainingDataTree.RefreshListNeeded = true;
				return true;
			}
			finally
			{
				m_handlingMessage = false;
			}
		}

		protected override void UpdateDisplayFromDatabase()
		{
			// What do we need to do here, if anything?
		}

		private void m_MSAPopupTreeManager_AfterSelect(object sender, TreeViewEventArgs e)
		{
			// unless we get a mouse click or simulated mouse click (e.g. by ENTER or TAB),
			// do not treat as an actual selection.
			if (m_handlingMessage || e.Action != TreeViewAction.ByMouse)
			{
				return;
			}
			var htn = e.Node as HvoTreeNode;
			if (htn == null)
			{
				return;
			}
			// Don't try changing values on a deleted object!  See LT-8656 and LT-9119.
			if (!MyCmObject.IsValidObject)
			{
				return;
			}
			var hvoSel = htn.Hvo;
			// if hvoSel is negative, then MSAPopupTreeManager's AfterSelect has handled it,
			// except possibly for refresh.
			if (hvoSel < 0)
			{
				ContainingDataTree.RefreshList(false);
				return;
			}
			var sense = MyCmObject as ILexSense;
			// Setting sense.DummyMSA can cause the DataTree to want to refresh.  Don't
			// let this happen until after we're through!  See LT-9713 and LT-9714.
			var fOldDoNotRefresh = ContainingDataTree.DoNotRefresh;
			try
			{
				m_handlingMessage = true;
				if (hvoSel <= 0)
				{
					return;
				}
				var obj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoSel);
				if (obj.ClassID == PartOfSpeechTags.kClassId)
				{
					ContainingDataTree.DoNotRefresh = true;
					var sandoxMSA = new SandboxGenericMSA
					{
						MsaType = sense.GetDesiredMsaType(),
						MainPOS = obj as IPartOfSpeech
					};
					var stemMsa = sense.MorphoSyntaxAnalysisRA as IMoStemMsa;
					if (stemMsa != null)
					{
						sandoxMSA.FromPartsOfSpeech = stemMsa.FromPartsOfSpeechRC;
					}
					UndoableUnitOfWorkHelper.Do(string.Format(DetailControlsStrings.ksUndoSet, m_fieldName), string.Format(DetailControlsStrings.ksRedoSet, m_fieldName), sense, () =>
					{
						sense.SandboxMSA = sandoxMSA;
					});
				}
				else if (sense.MorphoSyntaxAnalysisRA != obj)
				{
					ContainingDataTree.DoNotRefresh = true;
					UndoableUnitOfWorkHelper.Do(string.Format(DetailControlsStrings.ksUndoSet, m_fieldName), string.Format(DetailControlsStrings.ksRedoSet, m_fieldName), sense, () =>
					{
						sense.MorphoSyntaxAnalysisRA = obj as IMoMorphSynAnalysis;
					});
				}
			}
			finally
			{
				m_handlingMessage = false;
				// We still can't refresh the data at this point without causing a crash due to
				// a pending Windows message.  See LT-9713 and LT-9714.
				if (ContainingDataTree.DoNotRefresh != fOldDoNotRefresh)
				{
					Publisher.Publish("DelayedRefreshList", fOldDoNotRefresh);
				}
			}
		}

		#region IVwNotifyChange implementation

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			var sense = MyCmObject as ILexSense;
			if (sense.MorphoSyntaxAnalysisRA != null)
			{
				if (sense.MorphoSyntaxAnalysisRA.Hvo == hvo)
				{
					m_MSAPopupTreeManager.LoadPopupTree(sense.MorphoSyntaxAnalysisRA.Hvo);
				}
				else if (sense.Hvo == hvo && tag == LexSenseTags.kflidMorphoSyntaxAnalysis)
				{
					m_MSAPopupTreeManager.LoadPopupTree(sense.MorphoSyntaxAnalysisRA.Hvo);
				}
			}
		}

		#endregion IVwNotifyChange implementation

		/// <summary>
		/// Handles a TreeCombo control for use with MorphoSyntaxAnalysis objects.
		/// </summary>
		private sealed class MSAPopupTreeManager : PopupTreeManager
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
			public MSAPopupTreeManager(TreeCombo treeCombo, LcmCache cache, ICmPossibilityList list, int ws, bool useAbbr, FlexComponentParameters flexComponentParameters, Form parent)
				: base(treeCombo, cache, flexComponentParameters, list, ws, useAbbr, parent)
			{
				LoadStrings();
			}

			/// <summary>
			/// Constructor.
			/// </summary>
			public MSAPopupTreeManager(PopupTree popupTree, LcmCache cache, ICmPossibilityList list, int ws, bool useAbbr, FlexComponentParameters flexComponentParameters, Form parent)
				: base(popupTree, cache, flexComponentParameters, list, ws, useAbbr, parent)
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
					m_sUnknown = LanguageExplorerControls.ks_NotSure_;
				}
				if (string.IsNullOrEmpty(m_sSpecifyGramFunc) || m_sSpecifyGramFunc == "*AddNewGramFunc*")
				{
					m_sSpecifyGramFunc = LanguageExplorerControls.ksSpecifyGrammaticalInfo_;
				}
				if (string.IsNullOrEmpty(m_sModifyGramFunc) || m_sModifyGramFunc == "*ModifyGramFunc*")
				{
					m_sModifyGramFunc = LanguageExplorerControls.ksModifyThisGrammaticalInfo_;
				}
				if (string.IsNullOrEmpty(m_sSpecifyDifferent) || m_sSpecifyDifferent == "*SpecifyDifferentGramFunc*")
				{
					m_sSpecifyDifferent = LanguageExplorerControls.ksSpecifyDifferentGrammaticalInfo_;
				}
				if (string.IsNullOrEmpty(m_sCreateGramFunc) || m_sCreateGramFunc == "*CreateGramFuncGramFunc*")
				{
					m_sCreateGramFunc = LanguageExplorerControls.ksCreateNewGrammaticalInfo;
				}
				if (string.IsNullOrEmpty(m_sEditGramFunc) || m_sEditGramFunc == "*EditGramFuncGramFunc*")
				{
					m_sEditGramFunc = LanguageExplorerControls.ksEditGrammaticalInfo;
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
				if (fStem)
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
				UndoableUnitOfWorkHelper.Do(string.Format(LanguageExplorerControls.ksUndoSetX, FieldName), string.Format(LanguageExplorerControls.ksRedoSetX, FieldName), Sense, () =>
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
				Application.Idle += LaunchChooseFromMasterCategoryListOnIdle;
			}

			private void LaunchChooseFromMasterCategoryListOnIdle(object sender, EventArgs e)
			{
				// now being handled
				Application.Idle -= LaunchChooseFromMasterCategoryListOnIdle;
				// now launch the dialog
				using (var dlg = new MasterCategoryListDlg())
				{
					dlg.SetDlginfo(List, _flexComponentParameters.PropertyTable, false, null);
					switch (dlg.ShowDialog(ParentForm))
					{
						case DialogResult.OK:
							var sandboxMsa = new SandboxGenericMSA();
							sandboxMsa.MainPOS = dlg.SelectedPOS;
							sandboxMsa.MsaType = Sense.GetDesiredMsaType();
							UndoableUnitOfWorkHelper.Do(string.Format(LanguageExplorerControls.ksUndoSetX, FieldName), string.Format(LanguageExplorerControls.ksRedoSetX, FieldName), Sense, () =>
							{
								Sense.SandboxMSA = sandboxMsa;
							});
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
							LinkHandler.PublishFollowLinkMessage(_flexComponentParameters.Publisher, new FwLinkArgs(AreaServices.PosEditMachineName, dlg.SelectedPOS.Guid));
							if (ParentForm != null && ParentForm.Modal)
							{
								// Close the dlg that opened the master POS dlg,
								// since its hotlink was used to close it,
								// and a new POS has been created.
								ParentForm.DialogResult = DialogResult.Cancel;
								ParentForm.Close();
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
				if (Platform.IsMono)
				{
					// If Popup tree is shown whilst the dialog is shown, the first click on the dialog is consumed by the
					// Popup tree, (and closes it down). On .NET the PopupTree appears to be automatically closed.
					pt.HideForm();
				}
				using (var dlg = new MsaCreatorDlg())
				{
					var dummyMsa = new SandboxGenericMSA { MsaType = Sense.GetDesiredMsaType() };
					dlg.SetDlgInfo(Cache, PersistenceProvider, _flexComponentParameters, Sense.Entry, dummyMsa, 0, false, null);
					if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
					{
						Cache.DomainDataByFlid.BeginUndoTask(string.Format(LanguageExplorerControls.ksUndoSetX, FieldName), string.Format(LanguageExplorerControls.ksRedoSetX, FieldName));
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
				if (Platform.IsMono)
				{
					// If Popup tree is shown whilst the dialog is shown, the first click on the dialog is consumed by the
					// Popup tree, (and closes it down). On .NET the PopupTree appears to be automatically closed.
					pt.HideForm();
				}
				var dummyMsa = SandboxGenericMSA.Create(Sense.MorphoSyntaxAnalysisRA);
				using (var dlg = new MsaCreatorDlg())
				{
					dlg.SetDlgInfo(Cache, PersistenceProvider, _flexComponentParameters, Sense.Entry, dummyMsa, Sense.MorphoSyntaxAnalysisRA.Hvo, true, m_sEditGramFunc);
					if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
					{
						Cache.DomainDataByFlid.BeginUndoTask(string.Format(LanguageExplorerControls.ksUndoSetX, FieldName), string.Format(LanguageExplorerControls.ksRedoSetX, FieldName));
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
}