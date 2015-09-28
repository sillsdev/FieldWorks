// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using System.Linq;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.LexText.Controls;

namespace LanguageExplorer.Areas.Notebook.Tools.NotebookEdit
{
	/// <summary />
	internal sealed class RecordReferenceVectorLauncher : VectorReferenceLauncher
	{
		/// <summary />
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FindForm() returns a reference")]
		protected override void HandleChooser()
		{
			using (var dlg = new RecordGoDlg())
			{
				var wp = new WindowParams
				{
					m_title = LanguageExplorerResources.ksIdentifyRecord,
					m_btnText = LanguageExplorerResources.ks_Add
				};
				dlg.SetDlgInfo(m_cache, wp, PropertyTable, Publisher);
				dlg.SetHelpTopic(Slice.GetChooserHelpTopicID());
				if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
					AddItem(dlg.SelectedObject);
			}
		}

		/// <summary />
		public override void AddItem(ICmObject obj)
		{
			if (!Targets.Contains(obj))
				AddItem(obj, LanguageExplorerResources.ksUndoAddRef, LanguageExplorerResources.ksRedoAddRef);
		}
	}
}
