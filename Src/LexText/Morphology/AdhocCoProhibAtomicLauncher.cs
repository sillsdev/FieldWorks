using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.LexText.Controls;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	public class AdhocCoProhibAtomicLauncher : AtomicReferenceLauncher
	{
		private System.ComponentModel.IContainer components = null;

		public AdhocCoProhibAtomicLauncher()
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
		/// Override method to handle launching of a chooser for selecting lexical entries.
		/// </summary>
		protected override void HandleChooser()
		{
			Form frm = FindForm();
			WaitCursor wc = null;
			BaseGoDlg dlg = null;
			try
			{
				if (frm != null)
					wc = new WaitCursor(frm);
				if (m_obj is IMoAlloAdhocProhib)
					dlg = new LinkAllomorphDlg();
				else
				{
					Debug.Assert(m_obj is IMoMorphAdhocProhib);
					dlg = new LinkMSADlg();
				}
				Debug.Assert(dlg != null);
				dlg.SetDlgInfo(m_cache, null, PropertyTable, Publisher);
				if (dlg.ShowDialog(frm) == DialogResult.OK)
					AddItem(dlg.SelectedObject);
			}
			finally
			{
				if (wc != null)
					wc.Dispose();
				if (dlg != null)
					dlg.Dispose();
			}
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
