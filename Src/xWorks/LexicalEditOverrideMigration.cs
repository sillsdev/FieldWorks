// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// xWorks adapter that migrates a project's legacy whole-copy <c>.fwlayout</c> override into a sparse
	/// canonical JSON patch (task 9.2, project-file side). It bridges the live <see cref="Inventory"/> to
	/// the framework-neutral, fully-tested migration core
	/// (<see cref="ViewDefinitionOverrideFileMigrator"/> + <see cref="DictionaryPartResolver"/>).
	///
	/// The caller supplies the <em>pristine shipped</em> layout (resolved from the appropriate
	/// non-overridden source); this adapter does not decide the baseline — that choice (e.g. a base
	/// inventory vs. the project inventory whose overrides are already merged) belongs to the caller and
	/// is the one piece needing a real-project smoke test before production use.
	/// </summary>
	public static class LexicalEditOverrideMigration
	{
		/// <summary>
		/// Framework-neutral core: shipped layout + parts inventory as XElements. Unit-testable with inline
		/// XML — it composes the tested <see cref="DictionaryPartResolver"/> and
		/// <see cref="ViewDefinitionOverrideFileMigrator"/>.
		/// </summary>
		public static ViewDefinitionOverride MigrateProjectOverride(
			XElement shippedLayout,
			XElement partsInventory,
			string overrideFilePath,
			string outputPatchPath = null)
		{
			if (shippedLayout == null) throw new ArgumentNullException(nameof(shippedLayout));
			if (partsInventory == null) throw new ArgumentNullException(nameof(partsInventory));

			var parts = new DictionaryPartResolver(partsInventory);
			return ViewDefinitionOverrideFileMigrator.MigrateOverrideFile(
				shippedLayout, overrideFilePath, parts, outputPatchPath);
		}

		/// <summary>
		/// Live-<see cref="Inventory"/> bridge: adapts the shipped layout node and the parts inventory root
		/// (System.Xml) to the XElement core. The <paramref name="shippedLayout"/> must be the pristine
		/// shipped layout (see the type remarks on baseline selection).
		/// </summary>
		public static ViewDefinitionOverride MigrateProjectOverride(
			XmlNode shippedLayout,
			Inventory partsInventory,
			string overrideFilePath,
			string outputPatchPath = null)
		{
			if (shippedLayout == null) throw new ArgumentNullException(nameof(shippedLayout));
			if (partsInventory == null) throw new ArgumentNullException(nameof(partsInventory));

			return MigrateProjectOverride(
				XElement.Parse(shippedLayout.OuterXml),
				XElement.Parse(partsInventory.Root.OuterXml),
				overrideFilePath,
				outputPatchPath);
		}
	}
}
