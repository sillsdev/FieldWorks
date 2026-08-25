// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The commands the Avalonia detail view retargets away from mediator dispatch, as ordered
	/// (matcher, builder) entries. <see cref="TryBuild"/> builds the replacement item from the
	/// first matching entry; null leaves the command on its normal dispatch.
	/// </summary>
	internal sealed class OverrideCommandRegistry
	{
		private readonly List<KeyValuePair<Func<ChoiceBase, bool>, Func<ChoiceBase, DetailMenuItem>>> _entries
			= new List<KeyValuePair<Func<ChoiceBase, bool>, Func<ChoiceBase, DetailMenuItem>>>();

		public void Add(string helpId, Func<ChoiceBase, DetailMenuItem> build)
			=> Add(c => string.Equals(c.HelpId, helpId, StringComparison.Ordinal), build);

		/// <summary>Registers by matcher, for items that carry no command id.</summary>
		public void Add(Func<ChoiceBase, bool> matches, Func<ChoiceBase, DetailMenuItem> build)
			=> _entries.Add(
				new KeyValuePair<Func<ChoiceBase, bool>, Func<ChoiceBase, DetailMenuItem>>(matches, build));

		public DetailMenuItem TryBuild(ChoiceBase choice)
		{
			foreach (var entry in _entries)
			{
				if (entry.Key(choice))
					return entry.Value(choice);
			}

			return null;
		}
	}
}
