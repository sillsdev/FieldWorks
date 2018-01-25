// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal class SandboxChangedEventArgs : EventArgs
	{
		internal SandboxChangedEventArgs(bool fHasBeenEdited)
			: base()
		{
			Edited = fHasBeenEdited;
		}

		internal bool Edited { get; }
	}

	internal delegate void SandboxChangedEventHandler(object sender, SandboxChangedEventArgs e);
}