// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// The event args for the WordformUpdated event.
	/// </summary>
	internal sealed class WordformUpdatedEventArgs : EventArgs
	{
		internal WordformUpdatedEventArgs(IWfiWordform wordform, ParserPriority priority)
		{
			Wordform = wordform;
			Priority = priority;
		}

		internal IWfiWordform Wordform
		{
			get;
		}

		internal ParserPriority Priority
		{
			get;
		}
	}
}