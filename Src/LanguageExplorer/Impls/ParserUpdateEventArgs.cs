// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.WordWorks.Parser;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// The event args for the ParserUpdate events.
	/// </summary>
	internal sealed class ParserUpdateEventArgs : EventArgs
	{
		internal ParserUpdateEventArgs(TaskReport task)
		{
			Task = task;
		}

		internal TaskReport Task
		{
			get;
		}
	}
}