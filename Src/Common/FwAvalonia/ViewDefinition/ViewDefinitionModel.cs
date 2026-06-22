// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>
	/// Structural kind of a typed view-definition node. Mirrors the node types produced by the
	/// legacy XML Parts/Layout interpretation in <c>SliceFactory</c>/<c>DataTree</c>:
	/// a leaf field editor, a grouping header, an atomic object, a sequence, or the custom-field
	/// placeholder that the legacy code expands from the model.
	/// </summary>
	public enum ViewNodeKind
	{
		/// <summary>A leaf field bound to an editor (legacy <c>&lt;slice editor=.. field=..&gt;</c>).</summary>
		Field,

		/// <summary>A grouping header with child nodes and no editor (legacy <c>&lt;slice&gt;</c> with children).</summary>
		Group,

		/// <summary>An atomic owned/reference object (legacy <c>&lt;obj field=.. layout=..&gt;</c>).</summary>
		ObjectAtom,

		/// <summary>An owning/reference sequence (legacy <c>&lt;seq field=.. layout=..&gt;</c>).</summary>
		Sequence,

		/// <summary>The custom-field placeholder expanded from the model (legacy <c>customFields="here"</c>).</summary>
		CustomFieldPlaceholder
	}

	/// <summary>Field visibility, mirroring the legacy <c>visibility</c> attribute.</summary>
	public enum ViewVisibility
	{
		/// <summary>Always shown (legacy default / "always").</summary>
		Always,

		/// <summary>Shown only when the field has data (legacy "ifdata").</summary>
		IfData,

		/// <summary>Never shown unless the user enables hidden fields (legacy "never").</summary>
		Never
	}

	/// <summary>Expansion state for grouping/sequence nodes, mirroring the legacy <c>expansion</c> attribute.</summary>
	public enum ViewExpansion
	{
		/// <summary>Not an expandable node.</summary>
		NotApplicable,

		/// <summary>Expandable and currently collapsed.</summary>
		Collapsed,

		/// <summary>Expandable and currently expanded.</summary>
		Expanded
	}

	/// <summary>
	/// How an editor string is classified. The legacy <c>SliceFactory</c> switch resolves known
	/// editors directly, loads dynamic editors via <c>DynamicLoader</c> (<c>custom</c>,
	/// <c>customwithparams</c>, <c>autocustom</c>), throws on obsolete ones (<c>message</c>),
	/// treats a null editor as a grouping node, and falls back to a placeholder for the rest.
	/// The typed importer records the classification instead of constructing a control.
	/// </summary>
	public enum EditorClassification
	{
		/// <summary>A known, statically-resolved editor.</summary>
		Known,

		/// <summary>A grouping node with no editor.</summary>
		GroupingNone,

		/// <summary>A dynamically loaded editor (<c>custom</c>/<c>customwithparams</c>/<c>autocustom</c>).</summary>
		Dynamic,

		/// <summary>An obsolete editor that the legacy code rejects.</summary>
		Obsolete,

		/// <summary>An editor string not recognized by the legacy factory's known set.</summary>
		Unknown
	}

	/// <summary>Severity of a view-definition diagnostic.</summary>
	public enum ViewDiagnosticSeverity
	{
		Info,
		Warning,
		Error
	}

	/// <summary>
	/// Product-vs-preview routing for a node that can appear on a globally switchable surface (task 4.7).
	/// <c>Inherit</c> defers to the region/host decision; <c>Product</c> is wired through real
	/// LCModel-backed contracts; <c>Preview</c> is preview-host/sample only; <c>Unsupported</c> renders a
	/// resource-backed unsupported state instead of pretending to be a product editor.
	/// </summary>
	public enum SurfaceRouting
	{
		/// <summary>Defer to the enclosing region/host routing decision (default).</summary>
		Inherit,

		/// <summary>Product surface: must use real edit-session/domain contracts.</summary>
		Product,

		/// <summary>Preview/sample only: detached DTO models are allowed.</summary>
		Preview,

		/// <summary>Explicitly unsupported: render a resource-backed unsupported state.</summary>
		Unsupported
	}

	/// <summary>
	/// A diagnostic raised while importing/compiling a view definition. Carries the layout part and
	/// node path so unsupported constructs are reported, not silently dropped (task 4.4 / 3.8).
	/// </summary>
	public sealed class ViewDiagnostic
	{
		public ViewDiagnostic(ViewDiagnosticSeverity severity, string code, string message, string nodePath)
		{
			Severity = severity;
			Code = code;
			Message = message;
			NodePath = nodePath;
		}

		public ViewDiagnosticSeverity Severity { get; }

		/// <summary>Stable diagnostic code (e.g. "dynamic-editor", "unknown-editor", "unresolved-part").</summary>
		public string Code { get; }

		public string Message { get; }

		/// <summary>The stable node path the diagnostic applies to.</summary>
		public string NodePath { get; }

		public override string ToString()
			=> $"{Severity}: [{Code}] {Message} ({NodePath})";
	}

	/// <summary>
	/// An immutable typed view-definition node. This is the framework-neutral migration contract that
	/// both the legacy WinForms adapter and the future Avalonia adapter consume instead of raw XML.
	/// In the hybrid roadmap this is the typed node that the DataTree region's <c>SliceSpec</c> realizes.
	/// </summary>
	public sealed class ViewNode
	{
		public ViewNode(
			string stableId,
			ViewNodeKind kind,
			string label,
			string abbreviation,
			string field,
			string rawEditor,
			EditorClassification editorClassification,
			string writingSystem,
			ViewVisibility visibility,
			ViewExpansion expansion,
			bool indented,
			string targetLayout,
			IReadOnlyList<ViewNode> children,
			string localizationKey = null,
			string automationId = null,
			SurfaceRouting routing = SurfaceRouting.Inherit)
		{
			StableId = stableId;
			Kind = kind;
			Label = label;
			Abbreviation = abbreviation;
			Field = field;
			RawEditor = rawEditor;
			EditorClassification = editorClassification;
			WritingSystem = writingSystem;
			Visibility = visibility;
			Expansion = expansion;
			Indented = indented;
			TargetLayout = targetLayout;
			Children = children ?? (IReadOnlyList<ViewNode>)Array.Empty<ViewNode>();
			LocalizationKey = localizationKey;
			AutomationId = automationId;
			Routing = routing;
		}

		/// <summary>Deterministic identity derived from the node's path (stable across realizations).</summary>
		public string StableId { get; }

		public ViewNodeKind Kind { get; }

		public string Label { get; }

		public string Abbreviation { get; }

		public string Field { get; }

		/// <summary>The raw legacy editor string, preserved for audit/fallback.</summary>
		public string RawEditor { get; }

		public EditorClassification EditorClassification { get; }

		public string WritingSystem { get; }

		public ViewVisibility Visibility { get; }

		public ViewExpansion Expansion { get; }

		public bool Indented { get; }

		/// <summary>For object/sequence nodes, the destination layout name (deep expansion is deferred).</summary>
		public string TargetLayout { get; }

		public IReadOnlyList<ViewNode> Children { get; }

		/// <summary>
		/// Optional localization/resource key for this node's user-facing text (task 4.7). Null when the
		/// source carries no key; the label is then treated as a literal. Carried so a globally switchable
		/// surface can resolve localized strings without re-deriving them from incidental layout text.
		/// </summary>
		public string LocalizationKey { get; }

		/// <summary>
		/// Optional stable, nonlocalized accessibility identity for this node (task 4.7), stamped on the
		/// rendered control's <c>AutomationProperties.AutomationId</c>. Null when not authored.
		/// </summary>
		public string AutomationId { get; }

		/// <summary>Product-vs-preview routing for this node (task 4.7). Defaults to <see cref="SurfaceRouting.Inherit"/>.</summary>
		public SurfaceRouting Routing { get; }
	}

	/// <summary>
	/// An immutable compiled view definition: the typed node tree imported from XML Parts/Layout,
	/// plus any diagnostics raised during import. Produced by <c>IViewDefinitionImporter</c>.
	/// </summary>
	public sealed class ViewDefinitionModel
	{
		public ViewDefinitionModel(
			string className,
			string layoutName,
			string layoutType,
			IReadOnlyList<ViewNode> roots,
			IReadOnlyList<ViewDiagnostic> diagnostics)
		{
			ClassName = className;
			LayoutName = layoutName;
			LayoutType = layoutType;
			Roots = roots ?? (IReadOnlyList<ViewNode>)Array.Empty<ViewNode>();
			Diagnostics = diagnostics ?? (IReadOnlyList<ViewDiagnostic>)Array.Empty<ViewDiagnostic>();
		}

		public string ClassName { get; }

		public string LayoutName { get; }

		public string LayoutType { get; }

		public IReadOnlyList<ViewNode> Roots { get; }

		public IReadOnlyList<ViewDiagnostic> Diagnostics { get; }

		/// <summary>
		/// Produces a deterministic, normalized snapshot of the typed tree for parity/regression tests.
		/// One indented line per node keyed on stable identity, kind, binding, editor classification,
		/// writing system, visibility, and expansion — incidental layout noise is intentionally excluded.
		/// </summary>
		public string ToSnapshot()
		{
			var sb = new StringBuilder();
			sb.AppendLine($"layout class={ClassName} name={LayoutName} type={LayoutType}");
			foreach (var root in Roots)
			{
				AppendNode(sb, root, 0);
			}

			foreach (var diag in Diagnostics.OrderBy(d => d.NodePath, StringComparer.Ordinal)
				.ThenBy(d => d.Code, StringComparer.Ordinal))
			{
				sb.AppendLine($"diag {diag.Severity} [{diag.Code}] {diag.NodePath}");
			}

			return sb.ToString();
		}

		private static void AppendNode(StringBuilder sb, ViewNode node, int depth)
		{
			var indent = new string(' ', depth * 2);
			sb.AppendLine(
				$"{indent}{node.StableId} | {node.Kind} | label={node.Label} | field={node.Field} | " +
				$"editor={node.RawEditor}({node.EditorClassification}) | ws={node.WritingSystem} | " +
				$"vis={node.Visibility} | exp={node.Expansion} | indent={(node.Indented ? "1" : "0")} | " +
				$"target={node.TargetLayout}{AppendMetadata(node)}");
			foreach (var child in node.Children)
			{
				AppendNode(sb, child, depth + 1);
			}
		}

		// Task 4.7 metadata is appended only when present so existing semantic baselines (which carry no
		// localization/accessibility/routing metadata) keep their exact snapshot.
		private static string AppendMetadata(ViewNode node)
		{
			var sb = new StringBuilder();
			if (!string.IsNullOrEmpty(node.LocalizationKey))
				sb.Append($" | loc={node.LocalizationKey}");
			if (!string.IsNullOrEmpty(node.AutomationId))
				sb.Append($" | autoId={node.AutomationId}");
			if (node.Routing != SurfaceRouting.Inherit)
				sb.Append($" | routing={node.Routing}");
			return sb.ToString();
		}
	}
}
