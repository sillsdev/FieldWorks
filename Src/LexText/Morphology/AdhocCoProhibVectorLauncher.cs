using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;

using SIL.FieldWorks.FDO;
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
			BaseGoDlg dlg = null;
			try
			{
				if (frm != null)
					frm.Cursor = Cursors.WaitCursor;
				if (m_obj is IMoAlloAdhocProhib)
					dlg = new LinkAllomorphDlg();
				else
				{
					Debug.Assert(m_obj is IMoMorphAdhocProhib);
					dlg = new LinkMSADlg();
				}
				Debug.Assert(dlg != null);
				dlg.SetDlgInfo(m_cache, null, m_mediator);
				if (dlg.ShowDialog(frm) == DialogResult.OK)
					AddItem(dlg.SelectedObject);
			}
			finally
			{
				if (frm != null)
					frm.Cursor = Cursors.Default;
				if (dlg != null)
					dlg.Dispose();
			}
		}

		public override void AddItem(ICmObject obj)
		{
			CheckDisposed();

			List<ICmObject> results = null;
			if (m_obj is IMoAlloAdhocProhib)
			{
				var acop = m_obj as IMoAlloAdhocProhib;
				results = new List<ICmObject>(acop.RestOfAllosRS.Cast<ICmObject>());
			}
			else
			{
				Debug.Assert(m_obj is IMoMorphAdhocProhib);
				var mcop = m_obj as IMoMorphAdhocProhib;
				results = new List<ICmObject>(mcop.RestOfMorphsRS.Cast<ICmObject>());
			}
			results.Add(obj);
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
