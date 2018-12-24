// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Utils;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// Interface to let LexImportTests work where LexImport is a private class of LexImportWizard.
	/// </summary>
	internal interface ILexImportOnlyForTesting
	{
		object Import(IThreadedProgress dlg, object[] parameters);

		string Phase1FileName { get; }
	}
}