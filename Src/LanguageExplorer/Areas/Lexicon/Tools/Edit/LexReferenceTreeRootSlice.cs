// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls.DetailControls;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// LexReferenceTreeRootSlice is used to support selecting
	/// of a Sense or Entry tree.
	/// </summary>
	internal sealed class LexReferenceTreeRootSlice : CustomAtomicReferenceSlice, ILexReferenceSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LexReferenceTreeRootSlice"/> class.
		/// </summary>
		public LexReferenceTreeRootSlice()
			: base(new LexReferenceTreeRootLauncher())
		{
		}

		#region ILexReferenceSlice Members

#if RANDYTODO
		public override bool HandleDeleteCommand(Command cmd)
		{
			CheckDisposed();
			((LexReferenceMultiSlice)m_parentSlice).DeleteFromReference(GetObjectForMenusToOperateOn() as ILexReference);
			return true; // delete was done
		}
#endif

		/// <summary />
		public override void HandleLaunchChooser()
		{
			CheckDisposed();
			((LexReferenceTreeRootLauncher)Control).LaunchChooser();
		}

		/// <summary />
		public override void HandleEditCommand()
		{
			CheckDisposed();
			((LexReferenceMultiSlice)ParentSlice).EditReferenceDetails(GetObjectForMenusToOperateOn() as ILexReference);
		}

		#endregion
	}
}
