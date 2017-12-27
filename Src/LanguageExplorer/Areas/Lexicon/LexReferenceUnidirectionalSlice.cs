// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Areas.Lexicon.Tools.Edit;

namespace LanguageExplorer.Areas.Lexicon
{
	/// <summary>
	/// </summary>
	internal class LexReferenceUnidirectionalSlice : CustomReferenceVectorSlice, ILexReferenceSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LexReferenceUnidirectionalSlice"/> class.
		/// </summary>
		internal LexReferenceUnidirectionalSlice()
			: base(new LexReferenceUnidirectionalLauncher())
		{
		}

		#region ILexReferenceSlice Members

#if RANDYTODO
		public override bool HandleDeleteCommand(Command cmd)
		{
			CheckDisposed();
			((LexReferenceMultiSlice)m_parentSlice).DeleteReference(GetObjectForMenusToOperateOn() as ILexReference);
			return true; // delete was done
		}
#endif

		public override void HandleLaunchChooser()
		{
			CheckDisposed();
			((LexReferenceUnidirectionalLauncher)Control).LaunchChooser();
		}

		public override void HandleEditCommand()
		{
			CheckDisposed();
			((LexReferenceMultiSlice)m_parentSlice).EditReferenceDetails(GetObjectForMenusToOperateOn() as ILexReference);
		}
#endregion
	}
}
