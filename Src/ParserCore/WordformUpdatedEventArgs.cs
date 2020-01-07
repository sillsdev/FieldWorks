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
	public class WordformUpdatedEventArgs : EventArgs
	{
		public WordformUpdatedEventArgs(IWfiWordform wordform, ParserPriority priority)
		{
			Wordform = wordform;
			Priority = priority;
		}

		public IWfiWordform Wordform
		{
			get;
		}

		public ParserPriority Priority
		{
			get;
		}
	}
}