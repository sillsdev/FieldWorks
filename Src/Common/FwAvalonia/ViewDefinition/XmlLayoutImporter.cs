// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>
	/// Imports legacy XML Parts/Layout into the typed <see cref="ViewDefinitionModel"/>. The importer
	/// mirrors the structural decisions of <c>DataTree</c>/<c>SliceFactory</c> (parts, grouping slices,
	/// objects, sequences, indent, custom-field placeholders, visibility/expansion/label overrides) but
	/// produces immutable typed nodes and diagnostics instead of WinForms controls. It never throws on
	/// unsupported constructs; it records a diagnostic and continues.
	/// </summary>
	public sealed class XmlLayoutImporter : IViewDefinitionImporter
	{
		/// <inheritdoc />
		public ViewDefinitionModel Import(XElement layoutElement, IPartResolver parts)
		{
			var className = (string)layoutElement.Attribute("class") ?? "";
			var layoutName = (string)layoutElement.Attribute("name") ?? "";
			var layoutType = (string)layoutElement.Attribute("type") ?? "detail";

			var diagnostics = new List<ViewDiagnostic>();
			var roots = new List<ViewNode>();
			var basePath = $"{className}/{layoutName}";

			ProcessContainer(layoutElement.Elements(), parts, className, layoutType, basePath, false, roots, diagnostics);

			return new ViewDefinitionModel(className, layoutName, layoutType, roots, diagnostics);
		}

		private void ProcessContainer(
			IEnumerable<XElement> elements,
			IPartResolver parts,
			string className,
			string layoutType,
			string parentPath,
			bool indented,
			List<ViewNode> output,
			List<ViewDiagnostic> diagnostics)
		{
			foreach (var el in elements)
			{
				switch (el.Name.LocalName)
				{
					case "part":
						ProcessPart(el, parts, className, layoutType, parentPath, indented, output, diagnostics);
						break;
					case "indent":
						var indentAttr = (string)el.Attribute("indent");
						var indentFlag = indentAttr == null || indentAttr != "false";
						ProcessContainer(el.Elements(), parts, className, layoutType, parentPath, indentFlag, output, diagnostics);
						break;
					default:
						diagnostics.Add(new ViewDiagnostic(
							ViewDiagnosticSeverity.Warning,
							"unknown-container-element",
							$"Unsupported layout container element '{el.Name.LocalName}'.",
							$"{parentPath}/#{output.Count}"));
						break;
				}
			}
		}

		private void ProcessPart(
			XElement callerEl,
			IPartResolver parts,
			string className,
			string layoutType,
			string parentPath,
			bool indented,
			List<ViewNode> output,
			List<ViewDiagnostic> diagnostics)
		{
			var stableId = $"{parentPath}/#{output.Count}";
			var refName = (string)callerEl.Attribute("ref");

			// Custom-field placeholder: <part customFields="here"/> or ref="_CustomFieldPlaceholder".
			if (callerEl.Attribute("customFields") != null || refName == "_CustomFieldPlaceholder")
			{
				output.Add(MakeLeaf(stableId, ViewNodeKind.CustomFieldPlaceholder, "(custom fields)", null,
					null, null, EditorClassification.GroupingNone, null, ViewVisibility.Always,
					ViewExpansion.NotApplicable, indented, null));
				return;
			}

			if (string.IsNullOrEmpty(refName))
			{
				diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Warning, "part-without-ref",
					"A <part> has neither a 'ref' nor 'customFields' attribute.", stableId));
				return;
			}

			var content = parts.ResolvePart(className, layoutType, refName);
			if (content == null)
			{
				diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Error, "unresolved-part",
					$"Could not resolve part ref '{refName}' for class '{className}'.", stableId));
				return;
			}

			var node = BuildNode(content, callerEl, parts, className, layoutType, stableId, indented, diagnostics);
			if (node != null)
			{
				output.Add(node);
			}
		}

		private ViewNode BuildNode(
			XElement contentEl,
			XElement callerEl,
			IPartResolver parts,
			string className,
			string layoutType,
			string stableId,
			bool indented,
			List<ViewDiagnostic> diagnostics)
		{
			var label = Attr(callerEl, "label") ?? Attr(contentEl, "label");
			var abbreviation = Attr(callerEl, "abbr") ?? Attr(contentEl, "abbr");
			var visibility = ParseVisibility(Attr(callerEl, "visibility") ?? Attr(contentEl, "visibility"));
			var expansion = ParseExpansion(Attr(callerEl, "expansion") ?? Attr(contentEl, "expansion"));
			var field = Attr(contentEl, "field");
			var ws = Attr(contentEl, "ws");

			switch (contentEl.Name.LocalName)
			{
				case "slice":
				{
					var editor = Attr(contentEl, "editor");
					var classification = EditorKindMap.Classify(editor);
					RaiseEditorDiagnostics(editor, classification, stableId, diagnostics);

					var childElements = new List<XElement>();
					foreach (var child in contentEl.Elements())
					{
						if (child.Name.LocalName == "slice" || child.Name.LocalName == "seq" || child.Name.LocalName == "obj")
						{
							childElements.Add(child);
						}
					}

					if (classification == EditorClassification.GroupingNone && childElements.Count > 0)
					{
						var children = new List<ViewNode>();
						BuildInlineChildren(childElements, parts, className, layoutType, stableId, children, diagnostics);
						return new ViewNode(stableId, ViewNodeKind.Group, label, abbreviation, field, editor,
							classification, ws, visibility, expansion, indented, null, children);
					}

					return MakeLeaf(stableId, ViewNodeKind.Field, label, abbreviation, field, editor,
						classification, ws, visibility, expansion, indented, null);
				}
				case "obj":
				case "seq":
				{
					var kind = contentEl.Name.LocalName == "obj" ? ViewNodeKind.ObjectAtom : ViewNodeKind.Sequence;
					var targetLayout = Attr(callerEl, "param") ?? Attr(contentEl, "layout");
					var children = new List<ViewNode>();
					BuildInjectedChildren(callerEl, parts, layoutType, stableId, children, diagnostics);
					return new ViewNode(stableId, kind, label, abbreviation, field, null,
						EditorClassification.GroupingNone, ws, visibility, expansion, indented, targetLayout, children);
				}
				default:
					diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Warning, "unknown-part-content",
						$"Unsupported part content element '{contentEl.Name.LocalName}'.", stableId));
					return null;
			}
		}

		// Inline children are concrete <slice>/<seq>/<obj> elements nested directly inside a grouping slice.
		private void BuildInlineChildren(
			IEnumerable<XElement> childElements,
			IPartResolver parts,
			string className,
			string layoutType,
			string parentPath,
			List<ViewNode> output,
			List<ViewDiagnostic> diagnostics)
		{
			foreach (var child in childElements)
			{
				var stableId = $"{parentPath}/#{output.Count}";
				var node = BuildNode(child, child, parts, className, layoutType, stableId, false, diagnostics);
				if (node != null)
				{
					output.Add(node);
				}
			}
		}

		// Caller-injected children are <part ref=..> elements nested under a layout's object/sequence part.
		// Their destination class is not known from XML alone, so they are resolved by ref name.
		private void BuildInjectedChildren(
			XElement callerEl,
			IPartResolver parts,
			string layoutType,
			string parentPath,
			List<ViewNode> output,
			List<ViewDiagnostic> diagnostics)
		{
			foreach (var child in callerEl.Elements("part"))
			{
				var stableId = $"{parentPath}/#{output.Count}";
				var refName = (string)child.Attribute("ref");
				if (string.IsNullOrEmpty(refName))
				{
					continue;
				}

				var content = parts.ResolvePartByRef(refName);
				if (content == null)
				{
					diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Info, "cross-object-deferred",
						$"Injected child '{refName}' could not be resolved by ref; deep cross-object expansion is deferred.",
						stableId));
					continue;
				}

				var node = BuildNode(content, child, parts, "", layoutType, stableId, false, diagnostics);
				if (node != null)
				{
					output.Add(node);
				}
			}
		}

		private static void RaiseEditorDiagnostics(
			string editor, EditorClassification classification, string stableId, List<ViewDiagnostic> diagnostics)
		{
			switch (classification)
			{
				case EditorClassification.Dynamic:
					diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Info, "dynamic-editor",
						$"Editor '{editor}' is dynamically loaded; it needs an Avalonia editor mapping.", stableId));
					break;
				case EditorClassification.Obsolete:
					diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Error, "obsolete-editor",
						$"Editor '{editor}' is obsolete and rejected by the legacy factory.", stableId));
					break;
				case EditorClassification.Unknown:
					diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Warning, "unknown-editor",
						$"Editor '{editor}' is not in the known editor set.", stableId));
					break;
			}
		}

		private static ViewNode MakeLeaf(
			string stableId, ViewNodeKind kind, string label, string abbreviation, string field, string editor,
			EditorClassification classification, string ws, ViewVisibility visibility, ViewExpansion expansion,
			bool indented, string targetLayout)
			=> new ViewNode(stableId, kind, label, abbreviation, field, editor, classification, ws, visibility,
				expansion, indented, targetLayout, System.Array.Empty<ViewNode>());

		private static string Attr(XElement el, string name) => (string)el.Attribute(name);

		private static ViewVisibility ParseVisibility(string value)
		{
			switch (value)
			{
				case "never": return ViewVisibility.Never;
				case "ifdata": return ViewVisibility.IfData;
				default: return ViewVisibility.Always;
			}
		}

		private static ViewExpansion ParseExpansion(string value)
		{
			switch (value)
			{
				case "expanded": return ViewExpansion.Expanded;
				case "collapsed": return ViewExpansion.Collapsed;
				default: return ViewExpansion.NotApplicable;
			}
		}
	}
}
