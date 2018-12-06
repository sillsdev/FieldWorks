// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LCMBrowser
{
	/// <summary />
	public interface IInspectorList
	{
		/// <summary />
		event EventHandler BeginItemExpanding;
		/// <summary />
		event EventHandler EndItemExpanding;

		/// <summary />
		void Initialize(object obj);

		/// <summary />
		int Count { get; }

		/// <summary />
		object TopLevelObject { get; }

		/// <summary />
		IInspectorObject this[int index] { get; set; }

		/// <summary>
		/// Toggles the object expansion.
		/// </summary>
		bool ToggleObjectExpansion(int index);

		/// <summary>
		/// Determines whether the object at the specified index is expanded.
		/// </summary>
		bool IsExpanded(int index);

		/// <summary>
		/// Collapses the object at the specified index.
		/// </summary>
		bool CollapseObject(int index);

		/// <summary>
		/// Expands the specified object at the specified index.
		/// </summary>
		/// <param name="index">The index of the object to expand.</param>
		bool ExpandObject(int index);

		/// <summary>
		/// Determines whether the object at the specified index is a terminus.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns>
		/// 	<c>true</c> if the specified index is terminus; otherwise, <c>false</c>.
		/// </returns>
		bool IsTerminus(int index);

		/// <summary>
		/// Determines whether or not the item specified by index has any following uncles at
		/// the specified level.
		/// </summary>
		bool HasFollowingUncleAtLevel(int index, int level);

		/// <summary>
		/// Gets the first preceding item in the list before the one specified by index, that
		/// has a shallower level.
		/// </summary>
		IInspectorObject GetParent(int index);

		/// <summary>
		/// Gets the first preceding item in the list before the one specified by index, that
		/// has a shallower level.
		/// </summary>
		IInspectorObject GetParent(int index, out int indexParent);
	}
}
