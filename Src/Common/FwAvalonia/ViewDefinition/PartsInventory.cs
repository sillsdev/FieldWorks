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
	/// The one home for the shipped parts/layout search path: which directories hold view
	/// definitions, and which one wins when both define the same part id or layout key.
	/// </summary>
	/// <remarks>
	/// Legacy <c>Inventory</c> searches two directories -- the build-generated
	/// <c>Parts</c> defaults (<c>GeneratedParts.xml</c>, one part per model field) added first by
	/// the constructor, then the hand-authored <c>Language Explorer\Configuration\Parts</c>
	/// customizations. Its merge is last-loaded-wins (<c>Inventory.AddNode</c> replaces the table
	/// entry for a repeated key), so hand-authored definitions override generated ones. 231 part
	/// ids collide across the two shipped directories;
	/// <c>PartsDirectoryPrecedenceCharacterizationTests</c> pins the direction against the real
	/// files through the real legacy <c>Inventory</c>.
	///
	/// <see cref="LayoutSourceLoader"/>'s matchers are first-wins, so this class reproduces
	/// legacy precedence by ordering the search path the opposite way: hand-authored first.
	///
	/// Resolving a subdirectory name to an absolute path is injected rather than owned here,
	/// because FwAvalonia deliberately does not reference FwUtils. Production passes
	/// <c>FwDirectoryFinder.GetCodeSubDirectory</c>; tests resolve against the repo's DistFiles.
	/// </remarks>
	public static class PartsInventory
	{
		/// <summary>
		/// The shipped search path in first-wins order: hand-authored customizations, then the
		/// build-generated defaults. This ordering is the decision the class exists to own -- see
		/// the class remarks for why it is the reverse of legacy's load order.
		/// </summary>
		public static readonly IReadOnlyList<string> ShippedSubdirectories = new[]
		{
			@"Language Explorer\Configuration\Parts",
			@"Parts"
		};

		/// <summary>
		/// Resolves <see cref="ShippedSubdirectories"/> through <paramref
		/// name="resolveSubdirectory"/>,
		/// preserving order. Callers hold path resolution; this class holds which directories and
		/// in what precedence.
		/// </summary>
		public static IReadOnlyList<string> SearchPath(Func<string, string> resolveSubdirectory)
		{
			if (resolveSubdirectory == null)
			{
				throw new ArgumentNullException(nameof(resolveSubdirectory));
			}

			return ShippedSubdirectories.Select(resolveSubdirectory).ToList();
		}

		/// <summary>
		/// Merges every <c>*Parts.xml</c> across the search path into one
		/// <c>&lt;PartInventory&gt;</c> source string, search-path order preserved so a colliding
		/// part id resolves to the earlier directory. Returns null when no directory has parts.
		/// </summary>
		public static string LoadMergedPartsXml(IReadOnlyList<string> searchPath)
		{
			var partsFiles = (searchPath ?? new string[0])
				.SelectMany(LayoutSourceLoader.LoadPartsFiles)
				.ToList();
			return partsFiles.Count == 0 ? null : new XElement("PartInventory", partsFiles).ToString();
		}

		/// <summary>
		/// Loads every <c>*.fwlayout</c> across the search path, in order, so the first-wins
		/// matchers in <see cref="LayoutSourceLoader"/> prefer a hand-authored layout over a
		/// generated one with the same (class, type, name).
		/// </summary>
		public static IReadOnlyList<XElement> LoadLayoutFiles(IReadOnlyList<string> searchPath)
		{
			return (searchPath ?? new string[0])
				.SelectMany(LayoutSourceLoader.LoadLayoutFiles)
				.ToList();
		}

		/// <summary>
		/// The first directory on the search path holding <paramref name="fileName"/>, or null.
		/// Lets a caller that names a layout file directly obey the same precedence as the
		/// glob-based loads above instead of assuming one directory.
		/// </summary>
		public static string FindFile(IReadOnlyList<string> searchPath, string fileName)
		{
			if (searchPath == null || string.IsNullOrEmpty(fileName))
			{
				return null;
			}

			return searchPath
				.Where(d => !string.IsNullOrEmpty(d))
				.Select(d => Path.Combine(d, fileName))
				.FirstOrDefault(File.Exists);
		}
	}
}
