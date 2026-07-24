// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>
	/// The ONE loader for the shipped layout/parts directory (review finding D): merges every
	/// <c>*Parts.xml</c> into a single <c>&lt;PartInventory&gt;</c> and loads the <c>*.fwlayout</c>
	/// files, both in ordinal filename order so the merge is deterministic. Shared by
	/// <c>LexicalEditFirstSlice</c> (FwAvalonia) and <c>FullEntryRegionComposer</c> (xWorks) so the
	/// two compile paths cannot drift apart. Directory resolution (FwDirectoryFinder vs an explicit
	/// test path) stays with the caller; this class only reads a directory it is given.
	/// </summary>
	public static class LayoutSourceLoader
	{
		/// <summary>
		/// Merges every <c>*Parts.xml</c> in <paramref name="partsDirectory"/> (ordinal filename
		/// order) into one <c>&lt;PartInventory&gt;</c> source string. Returns null when the
		/// directory is missing or holds no parts files.
		/// </summary>
		public static string LoadMergedPartsXml(string partsDirectory)
		{
			if (string.IsNullOrEmpty(partsDirectory) || !Directory.Exists(partsDirectory))
			{
				return null;
			}

			var partsFiles = Directory.GetFiles(partsDirectory, "*Parts.xml")
				.OrderBy(f => f, StringComparer.Ordinal)
				.ToList();
			if (partsFiles.Count == 0)
			{
				return null;
			}

			return new XElement("PartInventory", partsFiles.Select(XElement.Load)).ToString();
		}

		/// <summary>
		/// Loads every <c>*.fwlayout</c> file in <paramref name="partsDirectory"/> in ordinal
		/// filename order. Returns an empty list when the directory is missing.
		/// </summary>
		public static IReadOnlyList<XElement> LoadLayoutFiles(string partsDirectory)
		{
			if (string.IsNullOrEmpty(partsDirectory) || !Directory.Exists(partsDirectory))
			{
				return new List<XElement>();
			}

			return Directory.GetFiles(partsDirectory, "*.fwlayout")
				.OrderBy(f => f, StringComparer.Ordinal)
				.Select(XElement.Load)
				.ToList();
		}

		/// <summary>
		/// Finds the first <c>&lt;layout class=... type=... name=...&gt;</c> match across the given
		/// files, in file order then document order — the legacy first-wins merge.
		/// </summary>
		public static XElement FindLayout(IEnumerable<XElement> layoutFiles, string className,
			string layoutName, string layoutType = "detail")
		{
			foreach (var file in layoutFiles)
			{
				var match = file.Descendants("layout").FirstOrDefault(l =>
					(string)l.Attribute("class") == className
					&& (string)l.Attribute("type") == layoutType
					&& (string)l.Attribute("name") == layoutName);
				if (match != null)
				{
					return match;
				}
			}

			return null;
		}

		/// <summary>
		/// Indexes layouts by (class, type, name) with first-wins semantics matching
		/// <see cref="FindLayout"/>, for callers that look layouts up repeatedly (review finding A).
		/// </summary>
		public static Dictionary<(string ClassName, string Type, string Name), XElement> IndexLayouts(
			IEnumerable<XElement> layoutFiles)
		{
			var index = new Dictionary<(string, string, string), XElement>();
			foreach (var file in layoutFiles)
			{
				foreach (var layout in file.Descendants("layout"))
				{
					var key = ((string)layout.Attribute("class"), (string)layout.Attribute("type"),
						(string)layout.Attribute("name"));
					if (key.Item1 == null || key.Item2 == null || key.Item3 == null)
					{
						continue;
					}

					if (!index.ContainsKey(key))
					{
						index[key] = layout;
					}
				}
			}

			return index;
		}

		/// <summary>
		/// §20.1.4 (F-1): indexes layouts by (class, type, name) → ALL matching variants in file-then-document
		/// order. Unlike <see cref="IndexLayouts"/> (3-key first-wins), this keeps every <c>choiceGuid</c>
		/// variant so a caller with a record's layout-choice GUID can pick the right one. Legacy DataTree
		/// distinguishes e.g. the 11 <c>RnGenericRec/detail/Normal</c> layouts only by <c>choiceGuid</c>; a
		/// first-wins 3-key lookup would collapse them all to the document-first (Analysis) layout.
		/// </summary>
		public static Dictionary<(string ClassName, string Type, string Name), List<XElement>> IndexLayoutsByChoice(
			IEnumerable<XElement> layoutFiles)
		{
			var index = new Dictionary<(string, string, string), List<XElement>>();
			foreach (var file in layoutFiles)
			{
				foreach (var layout in file.Descendants("layout"))
				{
					var className = (string)layout.Attribute("class");
					var type = (string)layout.Attribute("type");
					var name = (string)layout.Attribute("name");
					if (className == null || type == null || name == null)
					{
						continue;
					}

					var key = (className, type, name);
					if (!index.TryGetValue(key, out var variants))
					{
						variants = new List<XElement>();
						index[key] = variants;
					}
					variants.Add(layout);
				}
			}

			return index;
		}

		/// <summary>
		/// §20.1.4 (F-1): from a (class,type,name) variant list (see <see cref="IndexLayoutsByChoice"/>),
		/// pick the layout matching <paramref name="choiceGuid"/> (case-insensitive, mirroring legacy
		/// <c>DataTree.GetTemplateForObjLayout</c>): an exact <c>choiceGuid</c> match wins; otherwise the
		/// variant with NO <c>choiceGuid</c> attribute is the fallback; otherwise the first variant. A blank
		/// <paramref name="choiceGuid"/> selects the choiceGuid-less fallback (or the first), preserving the
		/// old first-wins behavior for layouts that do not use a layoutChoiceField.
		/// </summary>
		public static XElement SelectLayoutForChoice(IReadOnlyList<XElement> variants, string choiceGuid)
		{
			if (variants == null || variants.Count == 0)
			{
				return null;
			}

			XElement choicelessFallback = null;
			foreach (var layout in variants)
			{
				var lcg = (string)layout.Attribute("choiceGuid");
				if (!string.IsNullOrEmpty(choiceGuid)
					&& string.Equals(lcg, choiceGuid, StringComparison.OrdinalIgnoreCase))
				{
					return layout; // exact choiceGuid match wins.
				}
				if (string.IsNullOrEmpty(lcg) && choicelessFallback == null)
				{
					choicelessFallback = layout; // first choiceGuid-less variant is the fallback.
				}
			}

			return choicelessFallback ?? variants[0];
		}
	}
}
