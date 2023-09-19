// Copyright (c) 2020-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// This class contains the order events should execute when we are at the end of an action.
	/// </summary>
	internal static class EndOfActionOrder
	{
		private static readonly List<string> m_order = new List<string>
		{
			EventConstants.RecordNavigation,
			EventConstants.SelectionChanged
		};

		/// <summary>
		/// Returns a read only collection with the fixed order that events should
		/// execute at the end of an action.
		/// </summary>
		internal static ReadOnlyCollection<string> Order { get => m_order.AsReadOnly(); }
	}
}