// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Filters;

namespace LanguageExplorer
{
	internal sealed class RecordFilterParameterObject
	{
		internal RecordFilterParameterObject(bool allowDeletions = false, bool shouldHandleDeletion = false)
			: this(null, allowDeletions, shouldHandleDeletion)
		{
		}

		internal RecordFilterParameterObject(RecordFilter defaultFilter = null, bool allowDeletions = false, bool shouldHandleDeletion = false)
		{
			DefaultFilter = defaultFilter;
			AllowDeletions = allowDeletions;
			ShouldHandleDeletion = shouldHandleDeletion;
		}

		internal RecordFilter DefaultFilter { get; }

		internal bool AllowDeletions { get; }

		internal bool ShouldHandleDeletion { get; }
	}
}