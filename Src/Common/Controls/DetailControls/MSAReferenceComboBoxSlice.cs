// --------------------------------------------------------------------------------------------
#region // Copyright (c) MMVI, SIL International. All Rights Reserved.
// <copyright from='MMIII' to='MMVI' company='SIL International'>
//		Copyright (c) MMVI, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MSAReferenceComboBoxSlice.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// Implements the "MSAReferenceComboBox" XDE editor.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.Utils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.LexText.Controls;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public class MSAReferenceComboBoxSlice : FieldSlice, IVwNotifyChange
	{
		private const int kAdd = -3;

		private IPersistenceProvider m_persistProvider;
		private MSAPopupTreeManager m_MSAPopupTreeManager;
		private TreeCombo m_tree;
		int m_treeBaseWidth = 0;

		//private bool m_processSelectionEvent = true;
		private bool m_handlingMessage = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache">FDO cache.</param>
		/// <param name="obj">CmObject that is being displayed.</param>
		/// <param name="flid">The field identifier for the attribute we are displaying.</param>
		/// <param name="persistenceProvider">The persistence provider.</param>
		/// ------------------------------------------------------------------------------------
		public MSAReferenceComboBoxSlice(FdoCache cache, ICmObject obj, int flid,
			IPersistenceProvider persistenceProvider)
			: base(new UserControl(), cache, obj, flid)
		{
			IWritingSystem defAnalWs = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			m_persistProvider = persistenceProvider;
			m_tree = new TreeCombo();
			m_tree.WritingSystemFactory = cache.WritingSystemFactory;
			m_tree.Font = new System.Drawing.Font(defAnalWs.DefaultFontName, 10);
			if (!Application.RenderWithVisualStyles)
				m_tree.HasBorder = false;

			m_tree.WritingSystemCode = defAnalWs.Handle;

			// We embed the tree combo in a layer of UserControl, so it can have a fixed width
			// while the parent window control is, as usual, docked 'fill' to work with the splitter.
			m_tree.Dock = DockStyle.Left;
			m_tree.Width = 240;
			m_tree.DropDown += m_tree_DropDown;

			Control.Controls.Add(m_tree);
			m_tree.SizeChanged += m_tree_SizeChanged;

			if (m_cache != null)
				m_cache.DomainDataByFlid.AddNotification(this);
			m_treeBaseWidth = m_tree.Width;

			// m_tree has sensible PreferredHeight once the text is set, UserControl does not.
			//we need to set the Height after m_tree.Text has a value set to it.
			Control.Height = m_tree.PreferredHeight;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the mediator.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override Mediator Mediator
		{
			set
			{
				base.Mediator = value;

				//Set the stylesheet so that the font size for the...
				IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
				m_tree.StyleSheet = stylesheet;
				var list = m_cache.LanguageProject.PartsOfSpeechOA;

				m_MSAPopupTreeManager = new MSAPopupTreeManager(m_tree, m_cache, list,
					m_tree.WritingSystemCode, true, m_mediator,
					(Form)m_mediator.PropertyTable.GetValue("window"));
				m_MSAPopupTreeManager.AfterSelect += m_MSAPopupTreeManager_AfterSelect;
				m_MSAPopupTreeManager.Sense = m_obj as ILexSense;
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
		}

		/// <summary>
		/// Make the slice tall enough to hold the tree combo's internal textbox at a
		/// comfortable size.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void m_tree_SizeChanged(object sender, EventArgs e)
		{
			this.Height = Math.Max(this.Height, m_tree.PreferredHeight);
		}

		public override void Install(DataTree parent)
		{
			base.Install(parent);
			SplitCont.Panel2.SizeChanged += new EventHandler(SplitContPanel2_SizeChanged);
		}

		void SplitContPanel2_SizeChanged(object sender, EventArgs e)
		{
			int dxPanelWidth = SplitCont.Panel2.Width;

			if ((dxPanelWidth < m_tree.Width && dxPanelWidth >= 80) ||
				(dxPanelWidth > m_tree.Width && dxPanelWidth <= m_treeBaseWidth))
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
				return;

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				if (SplitCont != null && !SplitCont.IsDisposed &&
					SplitCont.Panel2 != null && !SplitCont.Panel2.IsDisposed)
				{
					SplitCont.Panel2.SizeChanged -= new EventHandler(SplitContPanel2_SizeChanged);
				}
				// Dispose managed resources here.
				if (m_cache != null)
					m_cache.DomainDataByFlid.RemoveNotification(this);

				if (m_tree != null && m_tree.Parent == null)
					m_tree.Dispose();

				if (m_MSAPopupTreeManager != null)
				{
					m_MSAPopupTreeManager.AfterSelect -= new TreeViewEventHandler(m_MSAPopupTreeManager_AfterSelect);
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
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		internal protected override bool UpdateDisplayIfNeeded(int hvo, int tag)
		{
			CheckDisposed();
			if (tag == Flid)
			{
				m_handlingMessage = true;
				try
				{
					var sense = m_obj as ILexSense;
					if (sense.MorphoSyntaxAnalysisRA != null)
						m_MSAPopupTreeManager.LoadPopupTree(sense.MorphoSyntaxAnalysisRA.Hvo);
					ContainingDataTree.RefreshListNeeded = true;
					return true;
				}
				finally
				{
					m_handlingMessage = false;
				}
			}
			return false;
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
				return;
			HvoTreeNode htn = e.Node as HvoTreeNode;
			if (htn == null)
				return;

			// Don't try changing values on a deleted object!  See LT-8656 and LT-9119.
			if (!m_obj.IsValidObject)
				return;

			int hvoSel = htn.Hvo;
			// if hvoSel is negative, then MSAPopupTreeManager's AfterSelect has handled it,
			// except possibly for refresh.
			if (hvoSel < 0)
			{
				ContainingDataTree.RefreshList(false);
				return;
			}
			var sense = m_obj as ILexSense;
			// Setting sense.DummyMSA can cause the DataTree to want to refresh.  Don't
			// let this happen until after we're through!  See LT-9713 and LT-9714.
			bool fOldDoNotRefresh = ContainingDataTree.DoNotRefresh;
			try
			{
				m_handlingMessage = true;
				if (hvoSel > 0)
				{
					ICmObject obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoSel);
					if (obj.ClassID == PartOfSpeechTags.kClassId)
					{
						ContainingDataTree.DoNotRefresh = true;
						var sandoxMSA = new SandboxGenericMSA();
						sandoxMSA.MsaType = sense.GetDesiredMsaType();
						sandoxMSA.MainPOS = obj as IPartOfSpeech;
						var stemMsa = sense.MorphoSyntaxAnalysisRA as IMoStemMsa;
						if (stemMsa != null)
							sandoxMSA.FromPartsOfSpeech = stemMsa.FromPartsOfSpeechRC;
						UndoableUnitOfWorkHelper.Do(String.Format(DetailControlsStrings.ksUndoSet, m_fieldName),
							String.Format(DetailControlsStrings.ksRedoSet, m_fieldName), sense, () =>
						{
							sense.SandboxMSA = sandoxMSA;
						});
					}
					else if (sense.MorphoSyntaxAnalysisRA != obj)
					{
						ContainingDataTree.DoNotRefresh = true;
						UndoableUnitOfWorkHelper.Do(String.Format(DetailControlsStrings.ksUndoSet, m_fieldName),
							String.Format(DetailControlsStrings.ksRedoSet, m_fieldName), sense, () =>
						{
							sense.MorphoSyntaxAnalysisRA = obj as IMoMorphSynAnalysis;
						});
					}
				}
			}
			finally
			{
				m_handlingMessage = false;
				// We still can't refresh the data at this point without causing a crash due to
				// a pending Windows message.  See LT-9713 and LT-9714.
				if (ContainingDataTree.DoNotRefresh != fOldDoNotRefresh)
					Mediator.BroadcastMessage("DelayedRefreshList", fOldDoNotRefresh);
			}
		}

		#region IVwNotifyChange implementation

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			var sense = m_obj as ILexSense;
			if (sense.MorphoSyntaxAnalysisRA != null)
			{
				if (sense.MorphoSyntaxAnalysisRA.Hvo == hvo)
					m_MSAPopupTreeManager.LoadPopupTree(sense.MorphoSyntaxAnalysisRA.Hvo);
				else if (sense.Hvo == hvo && tag == LexSenseTags.kflidMorphoSyntaxAnalysis)
					m_MSAPopupTreeManager.LoadPopupTree(sense.MorphoSyntaxAnalysisRA.Hvo);
			}
		}

		#endregion IVwNotifyChange implementation
	}
}
