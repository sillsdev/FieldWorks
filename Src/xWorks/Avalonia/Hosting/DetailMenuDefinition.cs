// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The item tree of one context-menu id as its configuration defines it: separators,
	/// command leaves, submenus (a list-populated one carrying only its list id), and the
	/// items no authority can answer. Built by <see cref="XCoreMenuBridge.ResolveMenu"/>; an
	/// owned menu is rendered from it and an authority decides ownership from it.
	/// </summary>
	public sealed class DetailMenuDefinition
	{
		private readonly List<DetailMenuLeaf> _leaves = new List<DetailMenuLeaf>();
		private readonly List<string> _listIds = new List<string>();
		private readonly List<string> _unanswerable = new List<string>();

		public DetailMenuDefinition(string menuId, IReadOnlyList<DetailMenuEntry> entries)
		{
			MenuId = menuId ?? throw new ArgumentNullException(nameof(menuId));
			Entries = entries ?? throw new ArgumentNullException(nameof(entries));
			Collect(entries);
		}

		/// <summary>The menu id the definition was resolved from.</summary>
		public string MenuId { get; }

		/// <summary>The top-level entries in configuration order.</summary>
		public IReadOnlyList<DetailMenuEntry> Entries { get; }

		/// <summary>Every command leaf, submenus included, in configuration order.</summary>
		public IReadOnlyList<DetailMenuLeaf> Leaves => _leaves;

		/// <summary>The list ids of the list-populated submenus, submenus included.</summary>
		public IReadOnlyList<string> ListIds => _listIds;

		/// <summary>
		/// Why each item no authority can answer cannot be answered, submenus included: a
		/// property toggle belongs to the property table, and an undefined command has no
		/// message. A menu with any such item is never owned.
		/// </summary>
		public IReadOnlyList<string> Unanswerable => _unanswerable;

		private void Collect(IReadOnlyList<DetailMenuEntry> entries)
		{
			foreach (var entry in entries)
			{
				if (entry.Leaf != null)
					_leaves.Add(entry.Leaf);
				else if (entry.Unanswerable != null)
					_unanswerable.Add(entry.Unanswerable);
				else if (entry.ListId != null)
					_listIds.Add(entry.ListId);
				else if (entry.Children != null)
					Collect(entry.Children);
			}
		}
	}

	/// <summary>
	/// One entry of a <see cref="DetailMenuDefinition"/>: a separator, a command leaf, an
	/// item no authority can answer, or a submenu whose children are configured items or, for
	/// a list-populated one, come from the authority at build time.
	/// </summary>
	public sealed class DetailMenuEntry
	{
		private DetailMenuEntry(DetailMenuLeaf leaf, string label, string unanswerable, string listId,
			bool isInline, IReadOnlyList<DetailMenuEntry> children)
		{
			Leaf = leaf;
			Label = label ?? string.Empty;
			Unanswerable = unanswerable;
			ListId = listId;
			IsInline = isInline;
			Children = children;
		}

		public static DetailMenuEntry Separator() => new DetailMenuEntry(null, null, null, null, false, null);

		public static DetailMenuEntry ForLeaf(DetailMenuLeaf leaf)
			=> new DetailMenuEntry(leaf ?? throw new ArgumentNullException(nameof(leaf)), null, null, null, false, null);

		/// <param name="label">The item's localized label with its accelerator marker.</param>
		/// <param name="reason">Why no authority can answer the item.</param>
		public static DetailMenuEntry ForUnanswerable(string label, string reason)
			=> new DetailMenuEntry(null, label, reason ?? throw new ArgumentNullException(nameof(reason)),
				null, false, null);

		/// <param name="label">The submenu's localized label with its accelerator marker.</param>
		/// <param name="isInline">Whether its items splice into the parent instead of
		/// nesting.</param>
		public static DetailMenuEntry ForSubmenu(string label, bool isInline, IReadOnlyList<DetailMenuEntry> children)
			=> new DetailMenuEntry(null, label, null, null, isInline,
				children ?? throw new ArgumentNullException(nameof(children)));

		/// <param name="listId">The <c>list</c> attribute naming the items' source.</param>
		public static DetailMenuEntry ForList(string label, bool isInline, string listId)
			=> new DetailMenuEntry(null, label, null, listId ?? throw new ArgumentNullException(nameof(listId)),
				isInline, null);

		public bool IsSeparator => Leaf == null && Unanswerable == null && Children == null && ListId == null;

		/// <summary>The command leaf, or null for any other entry.</summary>
		public DetailMenuLeaf Leaf { get; }

		/// <summary>A submenu's or unanswerable item's localized label; empty
		/// otherwise.</summary>
		public string Label { get; }

		/// <summary>Why no authority can answer this item; null for any other entry.</summary>
		public string Unanswerable { get; }

		/// <summary>A list-populated submenu's list id; null otherwise.</summary>
		public string ListId { get; }

		/// <summary>Whether a submenu's items splice into the parent menu.</summary>
		public bool IsInline { get; }

		/// <summary>A configured submenu's entries; null for a leaf, separator or list.</summary>
		public IReadOnlyList<DetailMenuEntry> Children { get; }
	}
}
