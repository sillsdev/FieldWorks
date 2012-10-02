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
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Common.Utils;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for LexReferenceTreeRootLauncher.
	/// </summary>
	public class LexReferenceTreeRootLauncher : AtomicReferenceLauncher
	{
		public LexReferenceTreeRootLauncher()
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
				return m_cache.GetVectorItem(m_obj.Hvo, m_flid, 0);
			}
			set
			{
				if (value != 0)
				{
					ILexReference lr = (m_obj as ILexReference);
					(lr as LexReference).UpdateTargetTimestamps();
					lr.TargetsRS.RemoveAt(0);
					ICmObject co = CmObject.CreateFromDBObject(m_cache, (int)value);
					(co as CmObject).UpdateTimestampForVirtualChange();
					lr.TargetsRS.InsertAt(co, 0);
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
			int type = lrt.MappingType;
			BaseGoDlg dlg = null;
			switch ((LexRefType.MappingTypes)type)
			{
				case LexRefType.MappingTypes.kmtSenseTree:
					dlg = new LinkEntryOrSenseDlg();
					(dlg as LinkEntryOrSenseDlg).SelectSensesOnly = true;
					break;
				case LexRefType.MappingTypes.kmtEntryTree:
					dlg = new GoDlg();
					break;
				case LexRefType.MappingTypes.kmtEntryOrSenseTree:
					dlg = new LinkEntryOrSenseDlg();
					break;
			}
			Debug.Assert(dlg != null);
			WindowParams wp = new WindowParams();

			//This method is only called when we are Replacing the
			//tree root of a Whole/Part lexical relation
			wp.m_title = String.Format(LexEdStrings.ksReplaceXEntry);
			wp.m_btnText = LexEdStrings.ks_Replace;
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
			this.Name = "LexReferenceTreeRootLauncher";
		}
		#endregion

		protected override AtomicReferenceView CreateAtomicReferenceView()
		{
			return new LexReferenceTreeRootView();
		}
	}
}
