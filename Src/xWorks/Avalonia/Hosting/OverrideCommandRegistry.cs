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
	/// entries keyed by command HelpId. <see cref="TryBuild"/> is the interceptor shape
	/// <see cref="XCoreMenuBridge"/> consumes: the first matching entry builds the replacement
	/// item; null leaves the command on its normal dispatch.
	/// </summary>
	internal sealed class OverrideCommandRegistry
	{
		private readonly List<KeyValuePair<string, Func<ChoiceBase, DetailMenuItem>>> _entries
			= new List<KeyValuePair<string, Func<ChoiceBase, DetailMenuItem>>>();

		public void Add(string helpId, Func<ChoiceBase, DetailMenuItem> build)
			=> _entries.Add(new KeyValuePair<string, Func<ChoiceBase, DetailMenuItem>>(helpId, build));

		public DetailMenuItem TryBuild(ChoiceBase choice)
		{
			foreach (var entry in _entries)
			{
				if (string.Equals(choice.HelpId, entry.Key, StringComparison.Ordinal))
					return entry.Value(choice);
			}

			return null;
		}
	}
}
