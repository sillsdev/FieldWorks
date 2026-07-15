// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// The shared structural projection rules used by BOTH region projectors — the thin
	/// <see cref="LexicalEditRegionMapper"/> (view-definition → region model, LCModel-free) and the full
	/// xWorks <c>FullEntryRegionComposer</c> (LCModel-backed). Task 18.11: the section-header row
	/// construction and the child-indent rule live ONCE here so the two paths cannot drift, before a
	/// second region reuses the foundation. (The third structural rule — editor → renderable kind — is
	/// likewise shared, in <see cref="EditorKindMap.ClassifyRegionFieldKind"/>.)
	/// </summary>
	public static class RegionStructureProjector
	{
		/// <summary>
		/// The indent depth a grouping node gives its children: a labeled group indents one level; an
		/// unlabeled passthrough group keeps the parent's depth. Mirrors the legacy grouping-slice
		/// nesting and is identical in both projectors.
		/// </summary>
		public static int ChildIndent(string label, int depth)
			=> string.IsNullOrEmpty(label) ? depth : depth + 1;

		/// <summary>
		/// Builds the canonical section-header row — the single construction site for
		/// <see cref="RegionFieldKind.Header"/> rows across both projectors. The thin mapper passes the
		/// defaults (no collapse affordance, no menu/HVO); the composer passes its LCModel-enriched
		/// values (collapsible state from expansion, slice menu/hotlinks, owning object HVO).
		/// </summary>
		public static LexicalEditRegionField BuildHeaderField(
			string stableId,
			string label,
			string field,
			string writingSystem,
			EditorClassification editorClassification,
			string automationId,
			string localizationKey,
			SurfaceRouting routing,
			int depth,
			bool isCollapsible = false,
			bool isInitiallyExpanded = true,
			string menuId = null,
			string hotlinksId = null,
			int objectHvo = 0)
		{
			return new LexicalEditRegionField(
				stableId, label, field, writingSystem, RegionFieldKind.Header, editorClassification,
				automationId, localizationKey, routing, null, null, null,
				isEditable: false, indent: depth,
				isCollapsible: isCollapsible, isInitiallyExpanded: isInitiallyExpanded,
				menuId: menuId, hotlinksId: hotlinksId, objectHvo: objectHvo);
		}
	}
}
