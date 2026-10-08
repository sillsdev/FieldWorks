// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Detail;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The native authority for the Lexicon detail menus, answered by the message each
	/// command sends (Insert, Delete, Move, Merge and the rest) and parameterized by the
	/// command's configuration. It owns a menu id by predicate: when it has an answerer for
	/// the message of every leaf under the id and the id carries neither a list-populated
	/// submenu nor an item no authority can answer. Adding an answerer therefore switches
	/// over every id it completes, and the ownership test pins the resulting list.
	/// </summary>
	internal sealed class LexiconMenuAuthority : IDetailMenuAuthority
	{
		private readonly Func<string, DetailMenuDefinition> _resolve;
		private readonly Dictionary<string, DetailMenuDefinition> _resolved
			= new Dictionary<string, DetailMenuDefinition>(StringComparer.Ordinal);
		private readonly Dictionary<string, Func<DetailMenuLeaf, DetailMenuItem>> _answerers
			= new Dictionary<string, Func<DetailMenuLeaf, DetailMenuItem>>(StringComparer.Ordinal);

		/// <param name="resolve">Resolves a menu id to its configured item tree.</param>
		public LexiconMenuAuthority(Func<string, DetailMenuDefinition> resolve)
		{
			_resolve = resolve ?? throw new ArgumentNullException(nameof(resolve));
		}

		/// <summary>The messages this authority answers, for the ownership test.</summary>
		internal IEnumerable<string> AnsweredMessages => _answerers.Keys;

		/// <summary>
		/// Registers the answerer for every leaf whose command sends <paramref name="message"/>;
		/// the ids it completes become owned. One answerer per message.
		/// </summary>
		internal void Answer(string message, Func<DetailMenuLeaf, DetailMenuItem> answerer)
		{
			if (string.IsNullOrEmpty(message))
				throw new ArgumentException("A message is required.", nameof(message));
			_answerers.Add(message, answerer ?? throw new ArgumentNullException(nameof(answerer)));
		}

		public bool Owns(string menuId)
		{
			var menu = Resolve(menuId);
			if (menu.Leaves.Count == 0 || menu.ListIds.Count > 0 || menu.Unanswerable.Count > 0)
				return false;
			foreach (var leaf in menu.Leaves)
			{
				if (!_answerers.ContainsKey(leaf.Message))
					return false;
			}
			return true;
		}

		public DetailMenuItem Build(string menuId, DetailMenuLeaf leaf)
		{
			if (leaf == null)
				throw new ArgumentNullException(nameof(leaf));
			if (_answerers.TryGetValue(leaf.Message, out var answer))
				return answer(leaf);
			throw new InvalidOperationException(string.Format(
				"Menu '{0}' has a leaf '{1}' this authority does not answer.", menuId, leaf.CommandId));
		}

		// An id with a list-populated submenu is never owned.
		public IReadOnlyList<DetailMenuItem> BuildList(string menuId, string listId)
			=> throw new InvalidOperationException(string.Format(
				"Menu '{0}' has a list submenu '{1}' this authority does not answer.", menuId, listId));

		// Resolved once per id for the authority's lifetime, one menu request.
		private DetailMenuDefinition Resolve(string menuId)
		{
			if (!_resolved.TryGetValue(menuId, out var menu))
			{
				menu = _resolve(menuId) ?? throw new InvalidOperationException(string.Format(
					"Menu '{0}' did not resolve.", menuId));
				_resolved.Add(menuId, menu);
			}
			return menu;
		}
	}
}
