// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using LanguageExplorer.HelpTopics;
using LanguageExplorer.Impls;

namespace LanguageExplorer
{
	/// <summary>
	/// Get types in this assembly that are globally and windows level scoped.
	/// </summary>
	internal static class LanguageExplorerCompositionServices
	{
		/// <summary>
		/// Get globally scoped types in this assembly.
		/// </summary>
		internal static IList<Type> GetGloballyAvailableTypes()
		{
			return new List<Type>
			{
				typeof(FlexApp),
				typeof(FlexHelpTopicProvider)
			};
		}

		/// <summary>
		/// Get types in this assembly that are created once per window instance (including the window).
		/// </summary>
		internal static IList<Type> GetWindowScopedTypes()
		{
			return new List<Type>
			{
				typeof(FwMainWnd),
				typeof(Publisher),
				typeof(Subscriber),
				typeof(PropertyTable)
			};
		}
	}
}
