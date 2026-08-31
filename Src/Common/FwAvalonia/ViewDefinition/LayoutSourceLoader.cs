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
	/// The ONE loader for the shipped layout/parts directories: merges every
	/// <c>*Parts.xml</c> into a single <c>&lt;PartInventory&gt;</c> and loads the <c>*.fwlayout</c>
	/// files, both in ordinal filename order so the merge is deterministic. Shared by
	/// <c>LexiconFirstSlice</c> (FwAvalonia) and <c>DetailComposer</c> (xWorks) so the
	/// two compile paths cannot drift apart. Directory resolution (FwDirectoryFinder vs an explicit
	/// test path) stays with the caller; this class only reads the directories it is given.
	/// </summary>
	public static class LayoutSourceLoader
	{
		private sealed class LayoutKeyComparer : IEqualityComparer<(string ClassName, string Type, string Name)>
		{
			private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

			public bool Equals((string ClassName, string Type, string Name) left,
				(string ClassName, string Type, string Name) right)
				=> Comparer.Equals(left.ClassName, right.ClassName)
					&& Comparer.Equals(left.Type, right.Type)
					&& Comparer.Equals(left.Name, right.Name);

			public int GetHashCode((string ClassName, string Type, string Name) key)
			{
				unchecked
				{
					var hash = Comparer.GetHashCode(key.ClassName ?? string.Empty);
					hash = (hash * 397) ^ Comparer.GetHashCode(key.Type ?? string.Empty);
					return (hash * 397) ^ Comparer.GetHashCode(key.Name ?? string.Empty);
				}
			}
		}

		private static readonly IEqualityComparer<(string ClassName, string Type, string Name)> LayoutKeys
			= new LayoutKeyComparer();

		/// <summary>
		/// Merges every <c>*Parts.xml</c> in <paramref name="partsDirectory"/> (ordinal filename
		/// order) into one <c>&lt;PartInventory&gt;</c> source string. Returns null when the
		/// directory is missing or holds no parts files.
		/// </summary>
		public static string LoadMergedPartsXml(string partsDirectory)
			=> LoadMergedPartsXml(new[] { partsDirectory });

		/// <summary>
		/// Merges every <c>*Parts.xml</c> across <paramref name="partsDirectories"/> into one
		/// <c>&lt;PartInventory&gt;</c> source string. Directories are taken in precedence
		/// order: <see cref="DictionaryPartResolver"/> keeps the first part it sees for an id,
		/// so a part in an earlier directory shadows the same id in a later one. Files within a
		/// directory merge in ordinal filename order. Returns null when no directory holds a
		/// parts file.
		/// </summary>
		public static string LoadMergedPartsXml(IEnumerable<string> partsDirectories)
		{
			var partsFiles = FilesIn(partsDirectories, "*Parts.xml");
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
			=> LoadLayoutFiles(new[] { partsDirectory });

		/// <summary>
		/// Loads every <c>*.fwlayout</c> file across <paramref name="partsDirectories"/> in
		/// precedence order: <see cref="IndexLayoutsByChoice"/> and <see cref="FindLayout"/>
		/// both return the first match, so a layout in an earlier directory shadows the same
		/// class/type/name/choiceGuid in a later one. Files within a directory load in ordinal
		/// filename order. Missing directories contribute nothing.
		/// </summary>
		public static IReadOnlyList<XElement> LoadLayoutFiles(IEnumerable<string> partsDirectories)
			=> FilesIn(partsDirectories, "*.fwlayout").Select(XElement.Load).ToList();

		private static List<string> FilesIn(IEnumerable<string> directories, string pattern)
		{
			var files = new List<string>();
			if (directories == null)
			{
				return files;
			}

			foreach (var directory in directories)
			{
				if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
				{
					continue;
				}

				files.AddRange(Directory.GetFiles(directory, pattern)
					.OrderBy(f => f, StringComparer.Ordinal));
			}

			return files;
		}

		/// <summary>
		/// Finds the first <c>&lt;layout class=... type=... name=...&gt;</c> match across the given
		/// files, in file order then document order -- the legacy first-wins merge.
		/// </summary>
		public static XElement FindLayout(IEnumerable<XElement> layoutFiles, string className,
			string layoutName, string layoutType = "detail")
		{
			foreach (var file in layoutFiles)
			{
				var match = file.Descendants("layout").FirstOrDefault(l =>
					StringComparer.OrdinalIgnoreCase.Equals((string)l.Attribute("class"), className)
					&& StringComparer.OrdinalIgnoreCase.Equals((string)l.Attribute("type"), layoutType)
					&& StringComparer.OrdinalIgnoreCase.Equals((string)l.Attribute("name"), layoutName));
				if (match != null)
				{
					return match;
				}
			}

			return null;
		}

		/// <summary>
		/// Indexes layouts by (class, type, name) with first-wins semantics matching
		/// <see cref="FindLayout"/>, for callers that look layouts up repeatedly.
		/// </summary>
		public static Dictionary<(string ClassName, string Type, string Name), XElement> IndexLayouts(
			IEnumerable<XElement> layoutFiles)
		{
			var index = new Dictionary<(string, string, string), XElement>(LayoutKeys);
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
		/// Indexes layouts by (class, type, name) -> ALL matching variants in file-then-document
		/// order. Unlike <see cref="IndexLayouts"/> (3-key first-wins), this keeps every <c>choiceGuid</c>
		/// variant so a caller with a record's layout-choice GUID can pick the right one. Legacy DataTree
		/// distinguishes e.g. the 11 <c>RnGenericRec/detail/Normal</c> layouts only by <c>choiceGuid</c>; a
		/// first-wins 3-key lookup would collapse them all to the document-first (Analysis) layout.
		/// </summary>
		public static Dictionary<(string ClassName, string Type, string Name), List<XElement>> IndexLayoutsByChoice(
			IEnumerable<XElement> layoutFiles)
		{
			var index = new Dictionary<(string, string, string), List<XElement>>(LayoutKeys);
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
		/// From a (class,type,name) variant list (see <see cref="IndexLayoutsByChoice"/>),
		/// pick only the layout with the same <c>choiceGuid</c>, case-insensitively. A null
		/// choice selects only a layout with no <c>choiceGuid</c> attribute; null and an
		/// explicitly empty attribute are distinct inventory keys.
		/// </summary>
		public static XElement SelectLayoutForChoice(IReadOnlyList<XElement> variants, string choiceGuid)
		{
			if (variants == null || variants.Count == 0)
			{
				return null;
			}

			foreach (var layout in variants)
			{
				var attribute = layout.Attribute("choiceGuid");
				if (choiceGuid == null ? attribute == null : attribute != null
					&& string.Equals(attribute.Value, choiceGuid, StringComparison.OrdinalIgnoreCase))
					return layout;
			}

			return null;
		}
	}
}
