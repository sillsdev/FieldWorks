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
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Widgets;
// needed for Marshal

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.LexText.Controls;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public class MSAReferenceComboBoxSlice : FieldSlice, IVwNotifyChange
	{
		private const int kAdd = -3;

		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		private IPersistenceProvider m_persistProvider;
		private Mediator m_mediator;
		private MSAPopupTreeManager m_MSAPopupTreeManager;
		private TreeCombo m_tree;
		int m_treeBaseWidth = 0;

		//private bool m_processSelectionEvent = true;
		private bool m_handlingMessage = false;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache">FDO cache.</param>
		/// <param name="obj">CmObject that is being displayed.</param>
		/// <param name="flid">The field identifier for the attribute we are displaying.</param>
		public MSAReferenceComboBoxSlice(FdoCache cache, ICmObject obj, int flid,
			IPersistenceProvider persistenceProvider, Mediator mediator)
			: base(new UserControl(), cache, obj, flid)
		{
			m_mediator = mediator;
			m_persistProvider = persistenceProvider;
			m_tree = new TreeCombo();
			m_tree.WritingSystemFactory = cache.LanguageWritingSystemFactoryAccessor;
			m_tree.Font = new System.Drawing.Font(cache.LangProject.DefaultAnalysisWritingSystemFont, 10);
			if (!Application.RenderWithVisualStyles)
				m_tree.HasBorder = false;

			//Set the stylesheet and writing system information so that the font size for the
			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromMediator(mediator);
			m_tree.WritingSystemCode = cache.LangProject.DefaultAnalysisWritingSystem;
			m_tree.StyleSheet = stylesheet;

			// We embed the tree combo in a layer of UserControl, so it can have a fixed width
			// while the parent window control is, as usual, docked 'fill' to work with the splitter.
			m_tree.Dock = DockStyle.Left;
			m_tree.Width = 240;
			m_tree.DropDown += new EventHandler(m_tree_DropDown);

			Control.Controls.Add(m_tree);
			m_tree.SizeChanged += new EventHandler(m_tree_SizeChanged);
			if (m_MSAPopupTreeManager == null)
			{
				ICmPossibilityList list = m_cache.LangProject.PartsOfSpeechOA;
				int ws = m_cache.LangProject.DefaultAnalysisWritingSystem;
				m_tree.WritingSystemCode = ws;
				m_MSAPopupTreeManager = new MSAPopupTreeManager(m_tree, m_cache, list, ws, true,
					mediator, (Form)mediator.PropertyTable.GetValue("window"));
				m_MSAPopupTreeManager.AfterSelect += new TreeViewEventHandler(m_MSAPopupTreeManager_AfterSelect);
				m_MSAPopupTreeManager.Sense = m_obj as ILexSense;
				m_MSAPopupTreeManager.PersistenceProvider = m_persistProvider;
			}
			try
			{
				m_handlingMessage = true;
				m_MSAPopupTreeManager.MakeTargetMenuItem();
				//m_MSAPopupTreeManager.LoadPopupTree(0);
			}
			finally
			{
				m_handlingMessage = false;
			}

			if (m_cache != null)
			{
				m_sda = m_cache.MainCacheAccessor;
				m_sda.AddNotification(this);
			}
			m_treeBaseWidth = m_tree.Width;

			Control.Height = m_tree.PreferredHeight;
						// m_tree has sensible PreferredHeight once the text is set, UserControl does not.
						//we need to set the Height after m_tree.Text has a value set to it.
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
				if (m_sda != null)
					m_sda.RemoveNotification(this);

				if (m_tree != null && m_tree.Parent == null)
					m_tree.Dispose();

				if (m_MSAPopupTreeManager != null)
				{
					m_MSAPopupTreeManager.AfterSelect -= new TreeViewEventHandler(m_MSAPopupTreeManager_AfterSelect);
					m_MSAPopupTreeManager.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_sda = null;
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
					LexSense sense = m_obj as LexSense;
					m_MSAPopupTreeManager.LoadPopupTree(sense.MorphoSyntaxAnalysisRAHvo);
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
			if (!m_cache.VerifyValidObject(m_obj))
				return;

			int hvoSel = htn.Hvo;
			// if hvoSel is negative, then MSAPopupTreeManager's AfterSelect has handled it,
			// except possibly for refresh.
			if (hvoSel < 0)
			{
				ContainingDataTree.RefreshList(false);
				return;
			}
			LexSense sense = m_obj as LexSense;
			// Setting sense.DummyMSA can cause the DataTree to want to refresh.  Don't
			// let this happen until after we're through!  See LT-9713 and LT-9714.
			bool fOldDoNotRefresh = ContainingDataTree.DoNotRefresh;
			try
			{
				m_handlingMessage = true;
				int clidSel = 0;
				if (hvoSel > 0)
					clidSel = m_cache.GetClassOfObject(hvoSel);
				bool didChange = false;
				if (clidSel == PartOfSpeech.kclsidPartOfSpeech)
				{
					ContainingDataTree.DoNotRefresh = true;
					m_cache.BeginUndoTask(
						String.Format(DetailControlsStrings.ksUndoSet, m_fieldName),
						String.Format(DetailControlsStrings.ksRedoSet, m_fieldName));
					DummyGenericMSA dummyMSA = new DummyGenericMSA();
					dummyMSA.MsaType = sense.GetDesiredMsaType();
					dummyMSA.MainPOS = hvoSel;
					MoStemMsa stemMsa = sense.MorphoSyntaxAnalysisRA as MoStemMsa;
					if (stemMsa != null)
						dummyMSA.FromPartsOfSpeech = stemMsa.FromPartsOfSpeechRC;
					sense.DummyMSA = dummyMSA;
					didChange = true;
				}
				else if (sense.MorphoSyntaxAnalysisRAHvo != hvoSel)
				{
					ContainingDataTree.DoNotRefresh = true;
					m_cache.BeginUndoTask(
						String.Format(DetailControlsStrings.ksUndoSet, m_fieldName),
						String.Format(DetailControlsStrings.ksRedoSet, m_fieldName));
					sense.MorphoSyntaxAnalysisRAHvo = hvoSel;
					didChange = true;
				}
				if (didChange)
					m_cache.EndUndoTask();
				if (!ContainingDataTree.RefreshListNeeded)
				{
					m_MSAPopupTreeManager.LoadPopupTree(sense.MorphoSyntaxAnalysisRAHvo);
					// Don't refresh the datatree unless the popup has actually been loaded.
					// It could be setting the selection in the process of loading!  See LT-9191.
					if (m_MSAPopupTreeManager.IsTreeLoaded)
						ContainingDataTree.RefreshList(false);
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

			// LT-3119: If an MSA has changed in our LexEntry, then update our combo box. This may be overkill, since
			// not every combo box for every LexSense will need to be updated. Furthermore, sometimes more than
			// one property change will be issued causing us to reload the list when it is not necessary. Perhaps
			// UpdateDisplayFromDatabase() can be further enhanced to check when it is necessary to reload?
			// For now, performance should not be a problem, so let's cover the general cases.
			if (m_cache == null)
				return;

			int ownClassId = m_cache.GetOwnClsId(tag);
			bool fUpdatedMorphoSyntaxAnalyses = (tag == (int)LexEntry.LexEntryTags.kflidMorphoSyntaxAnalyses);
			if (fUpdatedMorphoSyntaxAnalyses)
			{
				// Update if LexEntry for our Sense is equal to the LexEntry owning the changed property.
				LexSense sense = (LexSense)m_obj;
				if (sense.OwnerHVO == hvo)
					this.UpdateDisplayFromDatabase();
				return;
			}
			// Make sure the property belongs to a class we might actually we care about.
			if (ownClassId != (int)MoStemMsa.kClassId &&
				ownClassId != (int)MoInflAffMsa.kClassId &&
				ownClassId != (int)MoDerivAffMsa.kClassId &&
				ownClassId != (int)MoDerivStepMsa.kClassId &&
				ownClassId != (int)MoUnclassifiedAffixMsa.kClassId)
				return;

			if (hvo <= 0)
				return;
		}

		#endregion IVwNotifyChange implementation
	}
}
