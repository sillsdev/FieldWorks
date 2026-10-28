// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.LCModel;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.LexText.Controls;
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

		protected override ICmObject Target
		{
			get
			{
				return (m_obj as ILexReference).TargetsRS[0];
			}
			set
			{
				Debug.Assert(value != null);

				LexicalRelationEditing.ReplaceTreeWhole((ILexReference)m_obj, GetChildObject(), value);
			}
		}

		/// <summary>
		/// Get the object for which we want to change the root. For example, the first object in
		/// the lex reference which is our m_obj is the one that is in some relation (typically 'whole')
		/// to all the others (typically 'parts'). We need to figure out which part we are trying
		/// to associate with a different whole.
		/// This control is always configured as the child of a LexReferenceTreeRootSlice, which always
		/// has a ParentSlice whose Object is the one we want.
		/// </summary>
		/// <remarks>internal and virtual to support testing...otherwise would be private</remarks>
		/// <returns></returns>
		internal virtual ICmObject GetChildObject()
		{
			LexReferenceTreeRootSlice owningSlice = null;
			for (var parent = Parent; parent != null && owningSlice == null; parent = parent.Parent)
			{
				owningSlice = parent as LexReferenceTreeRootSlice;
			}
			if (owningSlice == null)
				throw new ConfigurationException("LexReferenceTreeRootLauncher must be a child of a LexReferenceTreeRootSlice");
			return owningSlice.ParentSlice.Object;
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
			//This method is only called when we are Replacing the tree root of a Whole/Part lexical relation
			var chooser = LexicalRelationEditing.Chooser((ILexReference)m_obj, GetChildObject());

			// New-UI gate (mirrors the EntrySequence gate): the LinkEntryOrSense relations use the Avalonia
			// Choose-Lexical-Entry-or-Sense dialog in New mode; the entry-only kmtEntryTree keeps its EntryGoDlg.
			var uiMode = m_propertyTable.GetStringProperty("UIMode", null);
			if (chooser.UseLinkDialog && UIModeGates.ShouldUseAvaloniaUI(uiMode))
			{
				ShowAvaloniaLinkDialog(chooser.AllowSenses, chooser.SensesOnly, chooser.Title);
				return;
			}

			BaseGoDlg dlg = null;
			try
			{
				dlg = chooser.UseLinkDialog ? (BaseGoDlg)new LinkEntryOrSenseDlg() : new EntryGoDlg();
				if (chooser.UseLinkDialog)
					((LinkEntryOrSenseDlg)dlg).SelectSensesOnly = chooser.SensesOnly;
				Debug.Assert(dlg != null);
				var wp = new WindowParams { m_title = chooser.Title, m_btnText = chooser.ButtonText };
				dlg.SetDlgInfo(m_cache, wp, m_mediator, m_propertyTable);
				dlg.SetHelpTopic(LexicalRelationEditing.ChooserHelpTopic);
				if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
				{
					if (dlg.SelectedObject != null)
						AddItem(dlg.SelectedObject);
				}
			}
			finally
			{
				if (dlg != null)
					dlg.Dispose();
			}
		}

		public override void AddItem(ICmObject obj)
		{
			string undoStr, redoStr;
			if (Target == null)
			{
				undoStr = LexicalRelationEditing.UndoAddReference;
				redoStr = LexicalRelationEditing.RedoAddReference;
			}
			else
			{
				undoStr = LexicalRelationEditing.UndoReplaceReference;
				redoStr = LexicalRelationEditing.RedoReplaceReference;
			}
			AddItem(obj, undoStr, redoStr);
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

		// NoInlining keeps the Avalonia assembly load out of the gated caller's JIT (Legacy loader isolation).
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ShowAvaloniaLinkDialog(bool allowSenses, bool sensesOnly, string sTitle)
		{
			var helpProvider = m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider", null);
			var chosen = LcmLinkEntryOrSenseDialogLauncher.Show(m_cache, m_mediator, m_propertyTable, null,
				FindForm(), "khtpChooseLexicalRelationAdd", helpProvider, allowSenses: allowSenses,
				sensesOnly: sensesOnly, title: sTitle, okButtonText: LexicalRelationEditing.ReplaceButtonText);
			if (chosen != null)
				AddItem(chosen);
		}
	}
}
