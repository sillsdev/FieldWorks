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
		// Task 4.9: the attribute vocabulary the importer actually consumes, per element role. Anything
		// outside these sets is reported with an `unhandled-attribute` diagnostic instead of being
		// silently dropped, so importer coverage is measurable (see LayoutImportCoverage).
		public static readonly HashSet<string> HandledLayoutAttributes =
			new HashSet<string>(System.StringComparer.Ordinal) { "class", "type", "name", "version" };

		public static readonly HashSet<string> HandledCallerPartAttributes =
			new HashSet<string>(System.StringComparer.Ordinal)
			{
				"ref", "label", "abbr", "visibility", "expansion", "param", "customFields",
				"localizationKey", "labelId", "automationId", "surface"
			};

		public static readonly HashSet<string> HandledSliceAttributes =
			new HashSet<string>(System.StringComparer.Ordinal)
			{
				"label", "abbr", "field", "ws", "editor", "visibility", "expansion",
				"localizationKey", "labelId", "automationId", "surface"
			};

		public static readonly HashSet<string> HandledObjSeqAttributes =
			new HashSet<string>(System.StringComparer.Ordinal)
			{
				"field", "layout", "label", "abbr", "ws", "visibility", "expansion",
				"localizationKey", "labelId", "automationId", "surface"
			};

		// Dropped attributes that change behavior (menus, ghost lines) get Warning severity; purely
		// presentational ones (styles, separators, numbering) get Info.
		private static readonly HashSet<string> FunctionalDroppedAttributes =
			new HashSet<string>(System.StringComparer.Ordinal)
			{
				"menu", "hotlinks", "ghost", "ghostWs", "ghostLabel", "ghostAbbr", "ghostClass",
				"ghostField", "ghostInitMethod", "editor"
			};

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
					// Task 4.9: named drop codes for the real-layout constructs the importer does not
					// expand yet, so coverage reports can count them instead of lumping them as unknown.
					case "generate":
						diagnostics.Add(new ViewDiagnostic(
							ViewDiagnosticSeverity.Warning,
							"generated-content-dropped",
							$"<generate class='{(string)el.Attribute("class")}' fieldType='{(string)el.Attribute("fieldType")}'> drives schema/custom-field UI generation and is not imported.",
							$"{parentPath}/#{output.Count}"));
						break;
					case "if":
					case "ifnot":
						diagnostics.Add(new ViewDiagnostic(
							ViewDiagnosticSeverity.Warning,
							"conditional-dropped",
							$"Conditional <{el.Name.LocalName} target='{(string)el.Attribute("target")}'> is not evaluated; its content is not imported.",
							$"{parentPath}/#{output.Count}"));
						break;
					case "sublayout":
						diagnostics.Add(new ViewDiagnostic(
							ViewDiagnosticSeverity.Info,
							"sublayout-dropped",
							$"<sublayout name='{(string)el.Attribute("name")}'> is a publishing construct and is not imported for detail views.",
							$"{parentPath}/#{output.Count}"));
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

			// Task 4.9: report attributes the importer drops instead of dropping them silently. The caller
			// element is reported only when distinct from the content element (inline children pass the
			// same element for both).
			if (!ReferenceEquals(callerEl, contentEl))
			{
				ReportUnhandledAttributes(callerEl, HandledCallerPartAttributes, "part ref", stableId, diagnostics);
				ReportSubstitutionValues(callerEl, HandledCallerPartAttributes, stableId, diagnostics);
			}

			// Task 4.7 metadata. Legacy XML Parts/Layout does not carry these, so they stay null/Inherit for
			// imported layouts (preserving semantic baselines); authored or region-spec sources may set them.
			var localizationKey = Attr(callerEl, "localizationKey") ?? Attr(contentEl, "localizationKey")
				?? Attr(callerEl, "labelId") ?? Attr(contentEl, "labelId");
			var automationId = Attr(callerEl, "automationId") ?? Attr(contentEl, "automationId");
			var routing = ParseRouting(Attr(callerEl, "surface") ?? Attr(contentEl, "surface"));

			switch (contentEl.Name.LocalName)
			{
				case "slice":
				{
					var editor = Attr(contentEl, "editor");
					var classification = EditorKindMap.Classify(editor);
					RaiseEditorDiagnostics(editor, classification, stableId, diagnostics);
					ReportUnhandledAttributes(contentEl, HandledSliceAttributes, "slice", stableId, diagnostics);
					ReportSubstitutionValues(contentEl, HandledSliceAttributes, stableId, diagnostics);

					var childElements = new List<XElement>();
					foreach (var child in contentEl.Elements())
					{
						if (child.Name.LocalName == "slice" || child.Name.LocalName == "seq" || child.Name.LocalName == "obj")
						{
							childElements.Add(child);
						}
						else
						{
							diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Info, "slice-content-dropped",
								$"Slice content child <{child.Name.LocalName}> is not imported.", stableId));
						}
					}

					var children = new List<ViewNode>();
					BuildInlineChildren(childElements, parts, className, layoutType, stableId, children, diagnostics);

					// Caller children under a slice-content part (<indent>/<part> wrappers on a section
					// part, e.g. AsLexemeForm's MorphTypeBasic) become child nodes, mirroring how
					// DataTree.ProcessPartRefNode realizes them as indented child slices. Other caller
					// child kinds are reported, not silently dropped (task 4.9).
					if (!ReferenceEquals(callerEl, contentEl))
					{
						var structuralCallerChildren = new List<XElement>();
						foreach (var callerChild in callerEl.Elements())
						{
							if (callerChild.Name.LocalName == "indent" || callerChild.Name.LocalName == "part")
							{
								structuralCallerChildren.Add(callerChild);
							}
							else
							{
								diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Warning, "caller-children-dropped",
									$"Caller child <{callerChild.Name.LocalName}> under part ref with slice content is not imported.",
									stableId));
							}
						}

						ProcessContainer(structuralCallerChildren, parts, className, layoutType, stableId, false,
							children, diagnostics);
					}

					if (classification == EditorClassification.GroupingNone && children.Count > 0)
					{
						return new ViewNode(stableId, ViewNodeKind.Group, label, abbreviation, field, editor,
							classification, ws, visibility, expansion, indented, null, children,
							localizationKey, automationId, routing);
					}

					return new ViewNode(stableId, ViewNodeKind.Field, label, abbreviation, field, editor,
						classification, ws, visibility, expansion, indented, null, children,
						localizationKey, automationId, routing);
				}
				case "obj":
				case "seq":
				{
					var kind = contentEl.Name.LocalName == "obj" ? ViewNodeKind.ObjectAtom : ViewNodeKind.Sequence;
					var targetLayout = Attr(callerEl, "param") ?? Attr(contentEl, "layout");
					ReportUnhandledAttributes(contentEl, HandledObjSeqAttributes, contentEl.Name.LocalName, stableId, diagnostics);
					ReportSubstitutionValues(contentEl, HandledObjSeqAttributes, stableId, diagnostics);
					var children = new List<ViewNode>();
					BuildInjectedChildren(callerEl, parts, layoutType, stableId, children, diagnostics);
					return new ViewNode(stableId, kind, label, abbreviation, field, null,
						EditorClassification.GroupingNone, ws, visibility, expansion, indented, targetLayout, children,
						localizationKey, automationId, routing);
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
			foreach (var child in callerEl.Elements())
			{
				if (child.Name.LocalName != "part")
				{
					// E.g. an <indent> wrapper under an obj/seq caller; its nested parts are not expanded
					// here. Report rather than silently drop (task 4.9).
					diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Warning, "injected-child-dropped",
						$"Caller child <{child.Name.LocalName}> under an object/sequence part is not imported.",
						$"{parentPath}/#{output.Count}"));
					continue;
				}

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
			bool indented, string targetLayout,
			string localizationKey = null, string automationId = null, SurfaceRouting routing = SurfaceRouting.Inherit)
			=> new ViewNode(stableId, kind, label, abbreviation, field, editor, classification, ws, visibility,
				expansion, indented, targetLayout, System.Array.Empty<ViewNode>(), localizationKey, automationId, routing);

		private static string Attr(XElement el, string name) => (string)el.Attribute(name);

		// Task 4.9: one diagnostic per attribute the importer does not consume. Functional drops
		// (menus, ghost lines) are warnings; presentational drops (style, separators, numbering) are info.
		private static void ReportUnhandledAttributes(
			XElement el, HashSet<string> handled, string role, string stableId,
			List<ViewDiagnostic> diagnostics)
		{
			foreach (var attr in el.Attributes())
			{
				var name = attr.Name.LocalName;
				if (handled.Contains(name))
				{
					continue;
				}

				var severity = FunctionalDroppedAttributes.Contains(name)
					? ViewDiagnosticSeverity.Warning
					: ViewDiagnosticSeverity.Info;
				diagnostics.Add(new ViewDiagnostic(severity, "unhandled-attribute",
					$"Attribute '{name}'='{attr.Value}' on <{el.Name.LocalName}> ({role}) is not imported.",
					stableId));
			}
		}

		// Task 4.9: handled attributes whose values use runtime substitution ($param, {0}) are consumed
		// literally by the importer; flag them so substitution semantics are not silently lost.
		private static void ReportSubstitutionValues(
			XElement el, HashSet<string> handled, string stableId, List<ViewDiagnostic> diagnostics)
		{
			foreach (var attr in el.Attributes())
			{
				var name = attr.Name.LocalName;
				if (!handled.Contains(name))
				{
					continue;
				}

				if (attr.Value.IndexOf('$') < 0 && !attr.Value.Contains("{0}"))
				{
					continue;
				}

				diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Info, "param-substitution",
					$"Attribute '{name}'='{attr.Value}' on <{el.Name.LocalName}> uses runtime substitution the importer does not expand.",
					stableId));
			}
		}

		private static SurfaceRouting ParseRouting(string value)
		{
			switch (value)
			{
				case "product": return SurfaceRouting.Product;
				case "preview": return SurfaceRouting.Preview;
				case "unsupported": return SurfaceRouting.Unsupported;
				default: return SurfaceRouting.Inherit;
			}
		}

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
