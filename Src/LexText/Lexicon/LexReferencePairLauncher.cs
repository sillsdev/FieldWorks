using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Common.Utils;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for LexReferencePairLauncher.
	/// </summary>
	public class LexReferencePairLauncher : AtomicReferenceLauncher
	{
		protected int m_hvoDisplayParent = 0;

		public LexReferencePairLauncher()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		protected override void ReplaceObject(int hvo)
		{
			(m_obj as LexReference).ReplaceTarget(m_atomicRefView.ObjectHvo, hvo);
		}

		protected override int TargetHvo
		{
			get
			{
				ISilDataAccess sda = m_cache.MainCacheAccessor;
				int chvo = sda.get_VecSize(m_obj.Hvo, m_flid);
				if (chvo < 2)
					return 0;
				int hvoItem = sda.get_VecItem(m_obj.Hvo, m_flid, 0);
				if (hvoItem == m_hvoDisplayParent)
					hvoItem = sda.get_VecItem(m_obj.Hvo, m_flid, 1);
				return hvoItem;
			}
			set
			{
				if (value != 0)
				{
					int index = 0;
					ISilDataAccess sda = m_cache.MainCacheAccessor;
					int hvoItem = sda.get_VecItem(m_obj.Hvo, m_flid, 0);
					if (hvoItem == m_hvoDisplayParent)
						index = 1;
					ILexReference lr = (m_obj as ILexReference);
					ICmObject co = CmObject.CreateFromDBObject(m_cache, (int)value);
					(lr as LexReference).UpdateTargetTimestamps();
					(co as CmObject).UpdateTimestampForVirtualChange();
					if (index < lr.TargetsRS.Count)
						lr.TargetsRS.RemoveAt(index);
					lr.TargetsRS.InsertAt(co, index);
					m_atomicRefView.RootBox.Reconstruct(); // view is somehow too complex for auto-update.
				}
			}
		}

		/// <summary>
		/// Wrapper for HandleChooser() to make it available to the slice.
		/// </summary>
		internal void LaunchChooser()
		{
			CheckDisposed();

			HandleChooser();
		}

		/// <summary>
		/// Override method to handle launching of a chooser for selecting lexical entries.
		/// </summary>
		protected override void HandleChooser()
		{
			ILexRefType lrt = LexRefType.CreateFromDBObject(m_cache, m_obj.OwnerHVO);
			LexRefType.MappingTypes type = (LexRefType.MappingTypes)lrt.MappingType;
			BaseGoDlg dlg = null;
			switch (type)
			{
				case LexRefType.MappingTypes.kmtSensePair:
				case LexRefType.MappingTypes.kmtSenseAsymmetricPair: // Sense pair with different Forward/Reverse names
					dlg = new LinkEntryOrSenseDlg();
					(dlg as LinkEntryOrSenseDlg).SelectSensesOnly = true;
					break;
				case LexRefType.MappingTypes.kmtEntryPair:
				case LexRefType.MappingTypes.kmtEntryAsymmetricPair: // Entry pair with different Forward/Reverse names
					dlg = new GoDlg();
					break;
				case LexRefType.MappingTypes.kmtEntryOrSensePair:
				case LexRefType.MappingTypes.kmtEntryOrSenseAsymmetricPair: // Entry or sense pair with different Forward/Reverse
					dlg = new LinkEntryOrSenseDlg();
					(dlg as LinkEntryOrSenseDlg).SelectSensesOnly = false;
					break;
			}
			Debug.Assert(dlg != null);
			WindowParams wp = new WindowParams();
			//on creating Pair Lexical Relation have an Add button and Add in the title bar
			if ( TargetHvo == 0 )
			{
				wp.m_title = String.Format(LexEdStrings.ksIdentifyXEntry,
				lrt.Name.AnalysisDefaultWritingSystem);
				wp.m_btnText = LexEdStrings.ks_Add;
			}
			else //Otherwise we are Replacing the item
			{
				wp.m_title = String.Format(LexEdStrings.ksReplaceXEntry);
				wp.m_btnText = LexEdStrings.ks_Replace;
			}

			wp.m_label = LexEdStrings.ksFind_;

			dlg.SetDlgInfo(m_cache, wp, m_mediator);
			dlg.SetHelpTopic("khtpChooseLexicalRelationAdd");
			if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
				TargetHvo = dlg.SelectedID;
			dlg.Dispose();
		}

		#region Component Designer generated code
		/// <summary>
		/// Everything except the Name is taken care of by the Superclass.
		/// </summary>
		private void InitializeComponent()
		{
			this.Name = "LexReferencePairLauncher";
		}
		#endregion

		protected override AtomicReferenceView CreateAtomicReferenceView()
		{
			LexReferencePairView pv = new LexReferencePairView();
			if (m_hvoDisplayParent != 0)
				pv.DisplayParentHvo = m_hvoDisplayParent;
			return pv;
		}

		public int DisplayParentHvo
		{
			set
			{
				CheckDisposed();

				m_hvoDisplayParent = value;
				if (m_atomicRefView != null)
					(m_atomicRefView as LexReferencePairView).DisplayParentHvo = value;
			}
		}
	}
}
