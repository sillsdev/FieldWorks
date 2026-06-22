// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>
	/// A dictionary-backed <see cref="IPartResolver"/> built from a parts inventory element
	/// (<c>&lt;PartInventory&gt;&lt;bin&gt;&lt;part id="Class-Type-Ref"&gt;...</c>). Resolution tries the
	/// exact <c>{class}-{type}-{ref}</c> key first, then falls back to <c>{class}-Detail-{ref}</c>,
	/// mirroring how the legacy inventory keys detail parts. Lookup is case-insensitive because the
	/// legacy <c>Inventory.GetElementKey</c> lowercases every key attribute value
	/// (<c>Src/XCore/Inventory.cs:1516</c>), so e.g. a layout ref <c>CVPattern</c> finds the shipped
	/// part id <c>PhWordSet-Detail-CVpattern</c>.
	/// </summary>
	public sealed class DictionaryPartResolver : IPartResolver
	{
		private readonly Dictionary<string, XElement> _partsById =
			new Dictionary<string, XElement>(System.StringComparer.OrdinalIgnoreCase);
		private readonly IReadOnlyDictionary<string, string> _baseClassMap;

		/// <summary>
		/// Builds a resolver from a <c>&lt;PartInventory&gt;</c> (or its inner <c>&lt;bin&gt;</c>) element.
		/// The optional <paramref name="baseClassMap"/> (subclass → base class) mirrors the legacy
		/// Inventory's class-hierarchy fallback: a ref unresolved on the subclass is retried on its base
		/// class chain (e.g. <c>MoStemAllomorph-Detail-AsLexemeForm</c> → <c>MoForm-Detail-AsLexemeForm</c>).
		/// </summary>
		public DictionaryPartResolver(XElement partInventory, IReadOnlyDictionary<string, string> baseClassMap = null)
		{
			_baseClassMap = baseClassMap;
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
			var contents = ResolvePartContents(className, layoutType, refName);
			return contents.Count > 0 ? contents[0] : null;
		}

		/// <inheritdoc />
		public IReadOnlyList<XElement> ResolvePartContents(string className, string layoutType, string refName)
		{
			if (string.IsNullOrEmpty(refName))
			{
				return System.Array.Empty<XElement>();
			}

			var type = string.IsNullOrEmpty(layoutType) ? "Detail" : layoutType;
			var currentClass = className;
			// Bounded walk so a cyclic base-class map cannot loop.
			for (var hop = 0; hop < 16 && !string.IsNullOrEmpty(currentClass); hop++)
			{
				if (_partsById.TryGetValue($"{currentClass}-{type}-{refName}", out var part)
					|| _partsById.TryGetValue($"{currentClass}-Detail-{refName}", out part))
				{
					// ALL child elements of the part — a part may hold several (e.g. the
					// <if Disabled=true>/<if Disabled=false> enable/disable pair the grammar detail
					// layouts use); legacy DataTree processes every child, so importing only the first
					// dropped the active-state variant (LT: compound-rule Name/Description vanished).
					return part.Elements().ToList();
				}

				if (_baseClassMap == null || !_baseClassMap.TryGetValue(currentClass, out currentClass))
				{
					break;
				}
			}

			return System.Array.Empty<XElement>();
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
				if (!pair.Key.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase))
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
	}
}
