// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls.Resources;
using LanguageExplorer.Controls.LexText;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class MSAReferenceComboBoxSlice : FieldSlice, IVwNotifyChange
	{
		private IPersistenceProvider m_persistProvider;
		private MSAPopupTreeManager m_MSAPopupTreeManager;
		private TreeCombo m_tree;
		int m_treeBaseWidth;
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

			m_MSAPopupTreeManager = new MSAPopupTreeManager(m_tree, Cache, list, m_tree.WritingSystemCode, true, PropertyTable, Publisher, PropertyTable.GetValue<Form>("window"));
			m_MSAPopupTreeManager.AfterSelect += m_MSAPopupTreeManager_AfterSelect;
			m_MSAPopupTreeManager.Sense = Object as ILexSense;
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

		void m_tree_DropDown(object sender, EventArgs e)
		{
			m_MSAPopupTreeManager.LoadPopupTree(0); // load the tree for real, with up-to-date list of available MSAs (see LT-5041).
		}

		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				if (SplitCont != null && !SplitCont.IsDisposed && SplitCont.Panel2 != null && !SplitCont.Panel2.IsDisposed)
				{
					SplitCont.Panel2.SizeChanged -= SplitContPanel2_SizeChanged;
				}
				// Dispose managed resources here.
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
				var sense = Object as ILexSense;
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
			if (!Object.IsValidObject)
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
			var sense = Object as ILexSense;
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
					UndoableUnitOfWorkHelper.Do(String.Format(DetailControlsStrings.ksUndoSet, m_fieldName),
						string.Format(DetailControlsStrings.ksRedoSet, m_fieldName), sense, () =>
						{
							sense.SandboxMSA = sandoxMSA;
						});
				}
				else if (sense.MorphoSyntaxAnalysisRA != obj)
				{
					ContainingDataTree.DoNotRefresh = true;
					UndoableUnitOfWorkHelper.Do(string.Format(DetailControlsStrings.ksUndoSet, m_fieldName),
						string.Format(DetailControlsStrings.ksRedoSet, m_fieldName), sense, () =>
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
			var sense = Object as ILexSense;
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
	}
}