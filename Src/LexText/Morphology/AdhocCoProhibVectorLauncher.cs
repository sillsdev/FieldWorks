using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.LexText.Controls;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// (JohnT, by inspection not as author:) this class seems to be responsible for the whole body,
	/// not just the launcher button, of an AdhocCoProhibVectorReferenceSlice, which is (for example)
	/// used in Grammar/Ad hoc Co-prohibitions, Other Morphemes slice. It is basically a
	/// VectorReferenceLauncher, with some special code mainly related to the chooser button.
	/// </summary>
	public class AdhocCoProhibVectorLauncher : VectorReferenceLauncher
	{
		private System.ComponentModel.IContainer components = null;

		public AdhocCoProhibVectorLauncher()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		/// <summary>
		/// Override method to handle launching of a chooser for selecting lexical entries or senses.
		/// </summary>
		protected override void HandleChooser()
		{
			Form frm = FindForm();
			try
			{
				if (frm != null)
					frm.Cursor = Cursors.WaitCursor;
				BaseGoDlg dlg = null;
				if (m_obj is MoAlloAdhocProhib)
					dlg = new LinkAllomorphDlg();
				else
				{
					Debug.Assert(m_obj is MoMorphAdhocProhib);
					dlg = new LinkMSADlg();
				}
				Debug.Assert(dlg != null);
				dlg.SetDlgInfo(m_cache, null, m_mediator);
				if (dlg.ShowDialog(frm) == DialogResult.OK)
					AddItem(dlg.SelectedID);
				dlg.Dispose();
			}
			finally
			{
				if (frm != null)
					frm.Cursor = Cursors.Default;
			}
		}

		public override void AddItem(int hvoNew)
		{
			CheckDisposed();

			List<int> results = null;
			if (m_obj is MoAlloAdhocProhib)
			{
				MoAlloAdhocProhib acop = m_obj as MoAlloAdhocProhib;
				results = new List<int>(acop.RestOfAllosRS.HvoArray);
			}
			else
			{
				Debug.Assert(m_obj is MoMorphAdhocProhib);
				MoMorphAdhocProhib mcop = m_obj as MoMorphAdhocProhib;
				results = new List<int>(mcop.RestOfMorphsRS.HvoArray);
			}
			results.Add(hvoNew);
			SetItems(results);
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion
	}
}
