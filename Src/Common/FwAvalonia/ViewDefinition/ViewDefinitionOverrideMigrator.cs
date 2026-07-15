// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>
	/// Migrates a legacy whole-copy <c>.fwlayout</c> override into a sparse, stable-id-keyed
	/// <see cref="ViewDefinitionOverride"/> (task 9.2 step 3). It imports both the shipped layout and the
	/// project's customized copy to the typed IR (reusing <see cref="XmlLayoutImporter"/>), then diffs them
	/// by <see cref="ViewNode.StableId"/>. Because a legacy override copies the shipped <c>&lt;layout&gt;</c>
	/// under the same name, the imported StableIds align by position, so the diff is exactly the customer's
	/// edits — replacing the lossy whole-tree <c>LayoutMerger</c> with per-node operations.
	///
	/// This is the framework-neutral migration core: it takes XML in and produces the patch. The thin
	/// remaining wrapper (read the shipped layout from <c>Inventory</c> and the override file from the
	/// project ConfigurationSettings folder, then write the patch file) is the XCore-coupled driver layer
	/// (still open), kept out of here so the migration logic stays unit-testable with inline XML.
	/// </summary>
	public static class ViewDefinitionOverrideMigrator
	{
		/// <summary>Migrates one shipped/overridden <c>&lt;layout&gt;</c> pair into a sparse override patch.</summary>
		public static ViewDefinitionOverride MigrateLayout(
			XElement shippedLayout,
			XElement overriddenLayout,
			IPartResolver parts,
			IViewDefinitionImporter importer = null)
		{
			if (shippedLayout == null) throw new ArgumentNullException(nameof(shippedLayout));
			if (overriddenLayout == null) throw new ArgumentNullException(nameof(overriddenLayout));
			if (parts == null) throw new ArgumentNullException(nameof(parts));

			importer = importer ?? new XmlLayoutImporter();
			var shippedModel = importer.Import(shippedLayout, parts);
			var overriddenModel = importer.Import(overriddenLayout, parts);
			return ViewDefinitionOverrideDiffer.Diff(shippedModel, overriddenModel);
		}

		/// <summary>String overload for callers/tests holding the layout XML as text.</summary>
		public static ViewDefinitionOverride MigrateLayout(
			string shippedLayoutXml,
			string overriddenLayoutXml,
			IPartResolver parts,
			IViewDefinitionImporter importer = null)
		{
			if (shippedLayoutXml == null) throw new ArgumentNullException(nameof(shippedLayoutXml));
			if (overriddenLayoutXml == null) throw new ArgumentNullException(nameof(overriddenLayoutXml));
			return MigrateLayout(XElement.Parse(shippedLayoutXml), XElement.Parse(overriddenLayoutXml), parts, importer);
		}

		/// <summary>Migrates and serializes the patch to canonical JSON in one step (the committed artifact).</summary>
		public static string MigrateLayoutToJson(
			XElement shippedLayout,
			XElement overriddenLayout,
			IPartResolver parts,
			IViewDefinitionImporter importer = null)
			=> ViewDefinitionOverrideJsonSerializer.Serialize(
				MigrateLayout(shippedLayout, overriddenLayout, parts, importer));
	}
}
