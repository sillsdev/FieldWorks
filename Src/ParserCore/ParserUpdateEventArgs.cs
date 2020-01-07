// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// The event args for the ParserUpdate events.
	/// </summary>
	public class ParserUpdateEventArgs : EventArgs
	{
		public ParserUpdateEventArgs(TaskReport task)
		{
			Task = task;
		}

		public TaskReport Task
		{
			get;
		}
	}
}