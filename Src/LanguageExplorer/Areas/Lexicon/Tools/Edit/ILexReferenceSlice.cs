// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls.DetailControls;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary />
	internal interface ILexReferenceSlice
	{
		Slice ParentSlice { get; set; }
#if RANDYTODO
		bool HandleDeleteCommand(Command cmd);
#endif
		void HandleLaunchChooser();
		void HandleEditCommand();
	}
}