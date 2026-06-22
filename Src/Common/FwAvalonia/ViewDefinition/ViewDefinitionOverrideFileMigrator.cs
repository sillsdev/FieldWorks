// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>
	/// File-level driver for the legacy-override → sparse-patch migration (task 9.2 step 3, project-file
	/// side). Reads a project's whole-copy <c>.fwlayout</c> override from disk, diffs it against the shipped
	/// layout via <see cref="ViewDefinitionOverrideMigrator"/>, and writes the canonical JSON patch file.
	///
	/// The only piece left to the XCore caller is providing the <em>shipped</em> layout element (resolved
	/// from <c>Inventory</c>) and the part resolver — those are passed in, so this whole orchestration is
	/// unit-testable with temp files and inline XML, with no XCore/Inventory dependency.
	/// </summary>
	public static class ViewDefinitionOverrideFileMigrator
	{
		/// <summary>
		/// Migrates the override file at <paramref name="overrideFilePath"/> against
		/// <paramref name="shippedLayout"/>. If <paramref name="outputPatchPath"/> is non-empty, the canonical
		/// JSON patch is written there. Returns the patch (also when no file is written).
		/// </summary>
		public static ViewDefinitionOverride MigrateOverrideFile(
			XElement shippedLayout,
			string overrideFilePath,
			IPartResolver parts,
			string outputPatchPath = null,
			IViewDefinitionImporter importer = null)
		{
			if (shippedLayout == null) throw new ArgumentNullException(nameof(shippedLayout));
			if (string.IsNullOrEmpty(overrideFilePath)) throw new ArgumentNullException(nameof(overrideFilePath));
			if (parts == null) throw new ArgumentNullException(nameof(parts));
			if (!File.Exists(overrideFilePath))
				throw new FileNotFoundException("Override layout file not found.", overrideFilePath);

			var overriddenLayout = LoadLayout(overrideFilePath);
			var patch = ViewDefinitionOverrideMigrator.MigrateLayout(shippedLayout, overriddenLayout, parts, importer);

			if (!string.IsNullOrEmpty(outputPatchPath))
			{
				var dir = Path.GetDirectoryName(outputPatchPath);
				if (!string.IsNullOrEmpty(dir))
					Directory.CreateDirectory(dir);
				File.WriteAllText(outputPatchPath, ViewDefinitionOverrideJsonSerializer.Serialize(patch));
			}

			return patch;
		}

		// The legacy override file (Inventory.PersistOverrideElement) is a copy of the customized <layout>
		// element. Accept either a file whose root is <layout> or one that wraps it.
		private static XElement LoadLayout(string path)
		{
			var root = XElement.Load(path);
			if (root.Name.LocalName == "layout")
				return root;
			var layout = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "layout");
			if (layout == null)
				throw new InvalidDataException($"No <layout> element found in override file '{path}'.");
			return layout;
		}
	}
}
