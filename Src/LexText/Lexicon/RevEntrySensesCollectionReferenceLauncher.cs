using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls;

namespace SIL.FieldWorks.XWorks.LexEd
{
	public class RevEntrySensesCollectionReferenceLauncher : VectorReferenceLauncher
	{
		private System.ComponentModel.IContainer components = null;

		public RevEntrySensesCollectionReferenceLauncher()
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

		protected override VectorReferenceView CreateVectorReverenceView()
		{
			return new RevEntrySensesCollectionReferenceView();
		}

		/// <summary>
		/// Override method to handle launching of a chooser for selecting lexical entries or senses.
		/// </summary>
		protected override void HandleChooser()
		{
			using (LinkEntryOrSenseDlg dlg = new LinkEntryOrSenseDlg())
			{
				WindowParams wp = new WindowParams();
				wp.m_title = LexEdStrings.ksIdentifySense;
				wp.m_label = LexEdStrings.ksFind_;
				wp.m_btnText = LexEdStrings.ksSetReversal;
				dlg.SetDlgInfo(m_cache, wp, m_mediator);
				dlg.SelectSensesOnly = true;
				if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
					AddItem(dlg.SelectedID);
			}
		}

		public override void AddItem(int hvoNew)
		{
			CheckDisposed();

			ILexSense selectedSense = LexSense.CreateFromDBObject(m_cache, hvoNew);
			FdoReferenceCollection<IReversalIndexEntry> col = selectedSense.ReversalEntriesRC;
			int hvoCurrentObj = m_obj.Hvo;
			int h1 = m_vectorRefView.RootBox.Height;
			if (!col.Contains(hvoCurrentObj))
			{
				int oldCount = col.Count;
				m_cache.BeginUndoTask(LexEdStrings.ksUndoAddRevToSense,
					LexEdStrings.ksRedoAddRevToSense);
				// Does a PropChanged on the sense's ReversalEntries property.
				col.Add(hvoCurrentObj);
				// Update the ReferringSenses property, and do PropChanged on it.
				ReversalIndexEntry.ResetReferringSenses(m_cache, m_obj.Hvo);
				m_cache.EndUndoTask();
				int h2 = m_vectorRefView.RootBox.Height;
				CheckViewSizeChanged(h1, h2);
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
