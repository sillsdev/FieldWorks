// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>
	/// A dictionary-backed <see cref="IPartResolver"/> built from a parts inventory element
	/// (<c>&lt;PartInventory&gt;&lt;bin&gt;&lt;part id="Class-Type-Ref"&gt;...</c>). Resolution tries the
	/// exact <c>{class}-{type}-{ref}</c> key first, then falls back to <c>{class}-Detail-{ref}</c>,
	/// mirroring how the legacy inventory keys detail parts.
	/// </summary>
	public sealed class DictionaryPartResolver : IPartResolver
	{
		private readonly Dictionary<string, XElement> _partsById = new Dictionary<string, XElement>();

		/// <summary>Builds a resolver from a <c>&lt;PartInventory&gt;</c> (or its inner <c>&lt;bin&gt;</c>) element.</summary>
		public DictionaryPartResolver(XElement partInventory)
		{
			foreach (var part in partInventory.Descendants("part"))
			{
				var id = (string)part.Attribute("id");
				if (string.IsNullOrEmpty(id) || _partsById.ContainsKey(id))
				{
					continue;
				}

				_partsById[id] = part;
			}
		}

		/// <inheritdoc />
		public XElement ResolvePart(string className, string layoutType, string refName)
		{
			if (string.IsNullOrEmpty(refName))
			{
				return null;
			}

			var type = string.IsNullOrEmpty(layoutType) ? "Detail" : layoutType;
			if (TryGetContent($"{className}-{type}-{refName}", out var content)
				|| TryGetContent($"{className}-Detail-{refName}", out content))
			{
				return content;
			}

			return null;
		}

		/// <inheritdoc />
		public XElement ResolvePartByRef(string refName)
		{
			if (string.IsNullOrEmpty(refName))
			{
				return null;
			}

			var suffix = $"-{refName}";
			XElement match = null;
			foreach (var pair in _partsById)
			{
				if (!pair.Key.EndsWith(suffix, System.StringComparison.Ordinal))
				{
					continue;
				}

				foreach (var child in pair.Value.Elements())
				{
					if (match != null)
					{
						// Ambiguous across classes: refuse rather than guess.
						return null;
					}

					match = child;
					break;
				}
			}

			return match;
		}

		private bool TryGetContent(string id, out XElement content)
		{
			content = null;
			if (!_partsById.TryGetValue(id, out var part))
			{
				return false;
			}

			// The part's content is its first child element (slice/obj/seq).
			foreach (var child in part.Elements())
			{
				content = child;
				return true;
			}

			return false;
		}
	}
}
