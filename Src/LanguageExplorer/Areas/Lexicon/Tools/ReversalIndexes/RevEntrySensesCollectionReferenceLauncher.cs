// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.LexText;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon.Tools.ReversalIndexes
{
	/// <summary />
	internal sealed class RevEntrySensesCollectionReferenceLauncher : VectorReferenceLauncher
	{
		private System.ComponentModel.IContainer components;

		/// <summary />
		public RevEntrySensesCollectionReferenceLauncher()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
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
				components?.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary />
		protected override VectorReferenceView CreateVectorReferenceView()
		{
			return new RevEntrySensesCollectionReferenceView();
		}

		/// <summary>
		/// Override method to handle launching of a chooser for selecting lexical entries or senses.
		/// </summary>
		protected override void HandleChooser()
		{
			using (var dlg = new LinkEntryOrSenseDlg())
			{
				dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				var wp = new WindowParams
				{
					m_title = LanguageExplorerResources.ksIdentifySense,
					m_btnText = LanguageExplorerResources.ksSetReversal
				};
				dlg.SetDlgInfo(m_cache, wp);
				dlg.SelectSensesOnly = true;
				if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.SelectedObject != null)
				{
					AddItem(dlg.SelectedObject);
				}
			}
		}

		/// <summary />
		public override void AddItem(ICmObject obj)
		{
			var selectedSense = (ILexSense)obj;
			var col = selectedSense.ReferringReversalIndexEntries;
			if (col.Contains(m_obj as IReversalIndexEntry))
			{
				return;
			}
			var h1 = m_vectorRefView.RootBox.Height;
			using (var helper = new UndoableUnitOfWorkHelper(m_cache.ActionHandlerAccessor, LanguageExplorerResources.ksUndoAddRevToSense, LanguageExplorerResources.ksRedoAddRevToSense))
			{
				((IReversalIndexEntry)m_obj).SensesRS.Add(selectedSense);
				helper.RollBack = false;
			}
			var h2 = m_vectorRefView.RootBox.Height;
			CheckViewSizeChanged(h1, h2);
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