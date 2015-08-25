using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using System.Linq;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.LexText.Controls;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	///
	/// </summary>
	public class RecordReferenceVectorLauncher : VectorReferenceLauncher
	{
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FindForm() returns a reference")]
		protected override void HandleChooser()
		{
			using (var dlg = new RecordGoDlg())
			{
				var wp = new WindowParams { m_title = LexEdStrings.ksIdentifyRecord, m_btnText = LexEdStrings.ks_Add };
				dlg.SetDlgInfo(m_cache, wp, PropertyTable, Publisher);
				dlg.SetHelpTopic(Slice.GetChooserHelpTopicID());
				if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
					AddItem(dlg.SelectedObject);
			}
		}

		public override void AddItem(ICmObject obj)
		{
			if (!Targets.Contains(obj))
				AddItem(obj, LexEdStrings.ksUndoAddRef, LexEdStrings.ksRedoAddRef);
		}
	}
}
