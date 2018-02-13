// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using LanguageExplorer.Controls.DetailControls;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary />
	internal sealed class LexReferenceTreeBranchesSlice : CustomReferenceVectorSlice, ILexReferenceSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LexReferenceTreeBranchesSlice"/> class.
		/// </summary>
		public LexReferenceTreeBranchesSlice()
			: base(new LexReferenceTreeBranchesLauncher())
		{
		}

		#region ILexReferenceSlice Members

#if RANDYTODO
		public override bool HandleDeleteCommand(Command cmd)
		{
			((LexReferenceMultiSlice)m_parentSlice).DeleteReference(GetObjectForMenusToOperateOn() as ILexReference);
			return true; // delete was done
		}
#endif

		/// <summary />
		public override void HandleLaunchChooser()
		{
			((LexReferenceTreeBranchesLauncher)Control).LaunchChooser();
		}

		/// <summary />
		public override void HandleEditCommand()
		{
			((LexReferenceMultiSlice)ParentSlice).EditReferenceDetails(GetObjectForMenusToOperateOn() as ILexReference);
		}
		#endregion
	}
}
