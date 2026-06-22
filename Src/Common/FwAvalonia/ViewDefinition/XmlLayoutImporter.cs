// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
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
				"localizationKey", "labelId", "automationId", "surface", "menu", "hotlinks",
				"visibleWritingSystems"
			};

		public static readonly HashSet<string> HandledSliceAttributes =
			new HashSet<string>(System.StringComparer.Ordinal)
			{
				"label", "abbr", "field", "ws", "editor", "visibility", "expansion",
				"localizationKey", "labelId", "automationId", "surface", "menu", "contextMenu", "hotlinks",
				"forVariant", "visibleWritingSystems"
			};

		public static readonly HashSet<string> HandledObjSeqAttributes =
			new HashSet<string>(System.StringComparer.Ordinal)
			{
				"field", "layout", "label", "abbr", "ws", "visibility", "expansion",
				"localizationKey", "labelId", "automationId", "surface", "menu", "hotlinks",
				"ghost", "ghostWs", "ghostClass", "ghostLabel", "ghostInitMethod"
			};

		// B7: the chooserLink attribute vocabulary the importer consumes (the legacy reader's exact
		// set, ReallySimpleListChooser.cs:887-926). The shipped files carry type/label/tool on all 94
		// links and target on 2 (grammar-area slot links).
		public static readonly HashSet<string> HandledChooserLinkAttributes =
			new HashSet<string>(System.StringComparer.Ordinal) { "type", "label", "tool", "target" };

		// B3: the condition vocabulary the importer parses into ViewCondition — exactly the forms the
		// shipped DETAIL layouts use (audited 2026-06-11 over DistFiles .../Parts: boolequals 44,
		// intequals 9, lengthatleast/-most 8, intmemberof 2, intlessthan 5, guidequals 2, is/target on
		// where clauses). Publishing-lane-only forms (stringequals, stringaltequals, hvoequals,
		// flidequals, bidi, atleastoneis, func, index, ws, class, flid) are NOT parsed; a condition
		// carrying one keeps the conditional-dropped lane so it is never evaluated wrongly.
		public static readonly HashSet<string> HandledConditionAttributes =
			new HashSet<string>(System.StringComparer.Ordinal)
			{
				"target", "is", "excludesubclasses", "field", "boolequals", "intequals",
				"intlessthan", "intgreaterthan", "intmemberof", "lengthatleast", "lengthatmost",
				"guidequals"
			};

		// Dropped attributes that change behavior (menus, ghost lines) get Warning severity; purely
		// presentational ones (styles, separators, numbering) get Info.
		private static readonly HashSet<string> FunctionalDroppedAttributes =
			new HashSet<string>(System.StringComparer.Ordinal)
			{
				"ghostAbbr", "ghostField", "ghostInitMethod", "editor"
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
					// B3: conditionals import as typed Conditional/ChoiceGroup nodes (evaluated per
					// object at compose time); unsupported condition forms still drop with a diagnostic.
					case "if":
					case "ifnot":
					case "choice":
					{
						var conditional = BuildNode(el, el, parts, className, layoutType,
							$"{parentPath}/#{output.Count}", indented, diagnostics);
						if (conditional != null)
							output.Add(conditional);
						break;
					}
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

				// Recover the caller's structural children so an unresolved *section* part (e.g.
				// LexSense/Normal's HeavySummary, which has no shipped part definition) does not drop
				// its real fields: <part ref='HeavySummary'><indent><part ref='GlossAllA'/>…</indent></part>.
				// Legacy DataTree omits the whole subtree here; recovering the children is strictly
				// more faithful to what users see and keeps the diagnostic for the audit trail.
				var recoverable = new List<XElement>();
				foreach (var child in callerEl.Elements())
				{
					if (child.Name.LocalName == "indent" || child.Name.LocalName == "part")
						recoverable.Add(child);
				}

				if (recoverable.Count > 0)
				{
					// 15.3: the recovered children ride a group node that keeps the CALLER's bindings —
					// HeavySummary's menu="mnuDataTree-Sense"/hotlinks survive here so the composed
					// per-sense headers can offer the legacy sense menu (Insert Sense etc.).
					var recoveredChildren = new List<ViewNode>();
					ProcessContainer(recoverable, parts, className, layoutType, stableId, indented,
						recoveredChildren, diagnostics);
					if (recoveredChildren.Count > 0)
					{
						output.Add(new ViewNode(stableId, ViewNodeKind.Group,
							Attr(callerEl, "label"), Attr(callerEl, "abbr"), null, null,
							EditorClassification.GroupingNone, null,
							ParseVisibility(Attr(callerEl, "visibility")),
							ParseExpansion(Attr(callerEl, "expansion")), indented, null,
							recoveredChildren,
							menuId: Attr(callerEl, "menu"),
							hotlinksId: Attr(callerEl, "hotlinks")));
					}
				}
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

			// 13.1: legacy menu bindings — slice menu from the caller (layout part) first, like
			// DTMenuHandler.ShowContextMenu2Id; in-string contextMenu lives on the slice content.
			var menuId = Attr(callerEl, "menu") ?? Attr(contentEl, "menu");
			var contextMenuId = Attr(contentEl, "contextMenu");
			var hotlinksId = Attr(callerEl, "hotlinks") ?? Attr(contentEl, "hotlinks");
			var automationId = Attr(callerEl, "automationId") ?? Attr(contentEl, "automationId");
			var routing = ParseRouting(Attr(callerEl, "surface") ?? Attr(contentEl, "surface"));

			switch (contentEl.Name.LocalName)
			{
				case "slice":
				{
					var editor = Attr(contentEl, "editor");
					var classification = EditorKindMap.Classify(editor);

					// Viewing parity (11.15): capture the visual-emphasis <properties> legacy slices
					// honor (bold + percentage fontsize, e.g. the lexeme form's bold/120%).
					var boldEmphasis = false;
					var fontScalePercent = 0;
					var properties = contentEl.Element("properties");
					if (properties != null)
					{
						boldEmphasis = (string)properties.Element("bold")?.Attribute("value") == "on";
						var fontsize = (string)properties.Element("fontsize")?.Attribute("value");
						if (fontsize != null && fontsize.EndsWith("%", System.StringComparison.Ordinal)
							&& int.TryParse(fontsize.TrimEnd('%'), out var percent))
						{
							fontScalePercent = percent;
						}
					}
					RaiseEditorDiagnostics(editor, classification, stableId, diagnostics);
					ReportUnhandledAttributes(contentEl, HandledSliceAttributes, "slice", stableId, diagnostics);
					ReportSubstitutionValues(contentEl, HandledSliceAttributes, stableId, diagnostics);

					var chooserLinks = new List<ViewChooserLink>();
					var childElements = new List<XElement>();
					ViewStringList enumStringList = null;
					foreach (var child in contentEl.Elements())
					{
						if (child.Name.LocalName == "slice" || child.Name.LocalName == "seq" || child.Name.LocalName == "obj")
						{
							childElements.Add(child);
						}
						else if (child.Name.LocalName == "chooserInfo")
						{
							// B7: the chooser jump links import as typed metadata (the legacy
							// "Edit the … list" links, ReallySimpleListChooser.InitializeExtras);
							// chooserInfo's other facets (title/text/guicontrol/textparam) are still
							// reported, not silently dropped.
							ImportChooserInfo(child, stableId, chooserLinks, diagnostics);
						}
						else if (child.Name.LocalName == "deParams")
						{
							// Review task 2: an enumComboBox slice's options live in
							// <deParams><stringList ids=.. group=..> (EnumComboSlice.PopulateCombo).
							// Carry that onto the node so the row can render a CLOSED option chooser
							// instead of degrading to a free-form int editor that could persist an
							// invalid enum value. The labels resolve through the StringTable lane at
							// compose time, so only the ids/group ride the IR.
							enumStringList = ImportStringList(child, stableId, diagnostics);
						}
						else if (child.Name.LocalName != "properties")
						{
							// <properties> is consumed above (11.15 emphasis); the rest is reported.
							diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Info, "slice-content-dropped",
								$"Slice content child <{child.Name.LocalName}> is not imported.", stableId));
						}
					}

					var children = new List<ViewNode>();
					BuildInlineChildren(childElements, parts, className, layoutType, stableId, children, diagnostics);

					// §19e: a jtview slice (editor="jtview") names the nested layout to compose for this
					// object in its caller's param (legacy SliceFactory jtview: param ?? node layout attr).
					// Carry it as the node's TargetLayout so the composer's WalkEmbeddedView can recurse the
					// nested layout's fields, exactly as an obj/seq descent does. Only jtview slices read it;
					// every other editor leaves TargetLayout null.
					string sliceTargetLayout = null;
					if (string.Equals(editor, EditorKindMap.JtViewEditor, System.StringComparison.OrdinalIgnoreCase))
						sliceTargetLayout = Attr(callerEl, "param") ?? Attr(contentEl, "layout");

					// §19e: a per-field writing-system visibility override (legacy visibleWritingSystems on a
					// multistring slice or its persisted partRef property — a space/comma list of ws specs).
					// Carry the ordered specs onto the node; the composer intersects them with the resolved
					// ws= set so the field shows exactly that subset. Caller (partRef) wins over content,
					// matching where the legacy editor persists the user's choice.
					var visibleWss = ParseWsList(Attr(callerEl, "visibleWritingSystems")
						?? Attr(contentEl, "visibleWritingSystems"));

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
							classification, ws, visibility, expansion, indented, sliceTargetLayout, children,
							localizationKey, automationId, routing, boldEmphasis, fontScalePercent,
							menuId, contextMenuId, hotlinksId,
							chooserLinks: chooserLinks.Count > 0 ? chooserLinks : null,
							visibleWritingSystems: visibleWss);
					}

					// Dynamic custom slices keep their legacy class/assembly identity so the host can
					// promote designated WinForms-only editors (e.g. the Chorus Messages notes bar) to
					// the hybrid companion lane instead of an unsupported row. The attributes stay in
					// the unhandled-attribute report (no Avalonia editor consumes them).
					return new ViewNode(stableId, ViewNodeKind.Field, label, abbreviation, field, editor,
						classification, ws, visibility, expansion, indented, sliceTargetLayout, children,
						localizationKey, automationId, routing, boldEmphasis, fontScalePercent,
						menuId, contextMenuId, hotlinksId,
						forVariant: ParseOptionalBool(Attr(contentEl, "forVariant")) ?? false,
						customEditorClass: Attr(contentEl, "class"),
						customEditorAssembly: Attr(contentEl, "assemblyPath"),
						chooserLinks: chooserLinks.Count > 0 ? chooserLinks : null,
						enumStringList: enumStringList,
						visibleWritingSystems: visibleWss,
						// §20.1.4 (F-7): legacy toggleValue= on a boolean slice (the displayed checkbox is the
						// logical inverse of the stored property); carried so the composer inverts read+write.
						toggleValue: ParseOptionalBool(Attr(contentEl, "toggleValue")) ?? false);
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
					// 14.1: the legacy ghost bindings ride the typed node so empty fields can offer
					// the create-on-edit add-prompt line (DataTree ghost slices).
					return new ViewNode(stableId, kind, label, abbreviation, field, null,
						EditorClassification.GroupingNone, ws, visibility, expansion, indented, targetLayout, children,
						localizationKey, automationId, routing, menuId: menuId, contextMenuId: contextMenuId,
						hotlinksId: hotlinksId,
						ghostField: Attr(contentEl, "ghost") ?? Attr(callerEl, "ghost"),
						ghostWs: Attr(contentEl, "ghostWs") ?? Attr(callerEl, "ghostWs"),
						ghostClass: Attr(contentEl, "ghostClass") ?? Attr(callerEl, "ghostClass"),
						ghostLabel: Attr(contentEl, "ghostLabel") ?? Attr(callerEl, "ghostLabel"),
						// B2: the layout's post-create hook rides the node so the composer's ghost
						// setter can invoke it the way GhostStringSliceView.MakeRealObject does.
						ghostInitMethod: Attr(contentEl, "ghostInitMethod") ?? Attr(callerEl, "ghostInitMethod"));
				}
				// B3: conditional display. <if>/<ifnot> wrap content shown only when the condition
				// passes (fails, for ifnot) — DataTree.ProcessSubpartNode cases "if"/"ifnot" over
				// XmlVc.ConditionPasses. The condition is preserved as structured metadata; the
				// composer evaluates it per object.
				case "if":
				case "ifnot":
				{
					var condition = TryParseCondition(contentEl, contentEl.Name.LocalName == "ifnot",
						stableId, diagnostics);
					if (condition == null)
						return null; // unsupported condition form; diagnostic already raised

					var children = new List<ViewNode>();
					BuildConditionalChildren(contentEl, parts, className, layoutType, stableId, indented,
						children, diagnostics);
					return new ViewNode(stableId, ViewNodeKind.Conditional, label, abbreviation,
						Attr(contentEl, "field"), null, EditorClassification.GroupingNone, null,
						visibility, expansion, indented, null, children, localizationKey, automationId,
						routing, menuId: menuId, contextMenuId: contextMenuId, hotlinksId: hotlinksId,
						condition: condition);
				}

				// B3: <choice> holds <where> branches (first passing one renders) and an optional
				// trailing <otherwise> — DataTree.ProcessSubpartNode case "choice".
				case "choice":
				{
					var branches = new List<ViewNode>();
					foreach (var clause in contentEl.Elements())
					{
						var branchId = $"{stableId}/#{branches.Count}";
						ViewCondition branchCondition = null;
						if (clause.Name.LocalName == "where")
						{
							branchCondition = TryParseCondition(clause, false, branchId, diagnostics);
							// One unevaluable where would mis-select a later branch/otherwise; drop the
							// whole choice (the diagnostic from TryParseCondition records why).
							if (branchCondition == null)
								return null;
						}
						else if (clause.Name.LocalName != "otherwise")
						{
							// Legacy throws "elements in choice must be <where...> or <otherwise>".
							diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Warning,
								"unknown-part-content",
								$"<choice> child <{clause.Name.LocalName}> is not where/otherwise and is not imported.",
								branchId));
							continue;
						}

						var branchChildren = new List<ViewNode>();
						BuildConditionalChildren(clause, parts, className, layoutType, branchId, indented,
							branchChildren, diagnostics);
						branches.Add(new ViewNode(branchId, ViewNodeKind.Conditional, null, null,
							Attr(clause, "field"), null, EditorClassification.GroupingNone, null,
							ViewVisibility.Always, ViewExpansion.NotApplicable, indented, null,
							branchChildren, condition: branchCondition));
					}

					return new ViewNode(stableId, ViewNodeKind.ChoiceGroup, label, abbreviation, null,
						null, EditorClassification.GroupingNone, null, visibility, expansion, indented,
						null, branches, localizationKey, automationId, routing, menuId: menuId,
						contextMenuId: contextMenuId, hotlinksId: hotlinksId);
				}

				default:
					diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Warning, "unknown-part-content",
						$"Unsupported part content element '{contentEl.Name.LocalName}'.", stableId));
					return null;
			}
		}

		// B7: import a slice's <chooserInfo> — the chooserLink jump links become typed metadata in
		// document order, mirroring the legacy reader's attribute set exactly
		// (ReallySimpleListChooser.cs:887-926: type defaults to "goto", label/tool/target verbatim).
		// chooserInfo's OTHER facets (title/text/textparam/flidTextParam/guicontrol/helpBrowser) are
		// not imported yet; they keep the slice-content-dropped report so the B7 remainder stays
		// measured rather than silently lost.
		private static void ImportChooserInfo(
			XElement chooserInfoEl, string stableId, List<ViewChooserLink> chooserLinks,
			List<ViewDiagnostic> diagnostics)
		{
			foreach (var linkEl in chooserInfoEl.Elements("chooserLink"))
			{
				chooserLinks.Add(new ViewChooserLink(
					Attr(linkEl, "type"),
					Attr(linkEl, "label"),
					Attr(linkEl, "tool"),
					Attr(linkEl, "target")));
				ReportUnhandledAttributes(linkEl, HandledChooserLinkAttributes, "chooserLink", stableId, diagnostics);
			}

			var droppedFacets = chooserInfoEl.Attributes()
				.Select(a => a.Name.LocalName)
				.Concat(chooserInfoEl.Elements()
					.Where(e => e.Name.LocalName != "chooserLink")
					.Select(e => "<" + e.Name.LocalName + ">"))
				.ToList();
			if (droppedFacets.Count > 0)
			{
				diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Info, "slice-content-dropped",
					$"Slice content child <chooserInfo> facets ({string.Join(", ", droppedFacets)}) are not imported; only chooserLink is.",
					stableId));
			}
		}

		// Review task 2: import an enumComboBox slice's <deParams><stringList ids=.. group=..> into the
		// typed ViewStringList. The labels themselves stay out of the IR (they resolve through the
		// StringTable lane at compose time); only the ids and the optional group path ride. A deParams
		// without a stringList, or a stringList without ids, is reported rather than silently dropped.
		private static ViewStringList ImportStringList(
			XElement deParamsEl, string stableId, List<ViewDiagnostic> diagnostics)
		{
			var stringListEl = deParamsEl.Element("stringList");
			if (stringListEl == null)
			{
				diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Info, "slice-content-dropped",
					"Slice content child <deParams> has no <stringList>; it is not imported.", stableId));
				return null;
			}

			var ids = Attr(stringListEl, "ids");
			if (string.IsNullOrEmpty(ids))
			{
				diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Warning, "enum-stringlist-dropped",
					"<stringList> has no 'ids'; the enum option list could not be imported.", stableId));
				return null;
			}

			var idList = ids.Split(',')
				.Select(s => s.Trim())
				.Where(s => s.Length > 0)
				.ToList();
			if (idList.Count == 0)
			{
				diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Warning, "enum-stringlist-dropped",
					$"<stringList ids='{ids}'> yielded no option ids.", stableId));
				return null;
			}

			return new ViewStringList(idList, Attr(stringListEl, "group"));
		}

		// B3: a conditional wrapper's children are part content (<slice>/<seq>/<obj>, possibly nested
		// conditionals) inside part definitions, or <part>/<indent> refs at layout level — exactly the
		// child kinds DataTree.ProcessPartChildren dispatches.
		private void BuildConditionalChildren(
			XElement container,
			IPartResolver parts,
			string className,
			string layoutType,
			string parentPath,
			bool indented,
			List<ViewNode> output,
			List<ViewDiagnostic> diagnostics)
		{
			foreach (var child in container.Elements())
			{
				switch (child.Name.LocalName)
				{
					case "part":
					case "indent":
						ProcessContainer(new[] { child }, parts, className, layoutType, parentPath,
							indented, output, diagnostics);
						break;
					default:
					{
						var node = BuildNode(child, child, parts, className, layoutType,
							$"{parentPath}/#{output.Count}", indented, diagnostics);
						if (node != null)
							output.Add(node);
						break;
					}
				}
			}
		}

		// B3: parse an <if>/<ifnot>/<where> element's condition attributes into the typed
		// ViewCondition, or report conditional-dropped and return null when the element uses a
		// condition form outside the supported (detail-lane) vocabulary — never half-evaluate.
		private static ViewCondition TryParseCondition(
			XElement el, bool negated, string stableId, List<ViewDiagnostic> diagnostics)
		{
			foreach (var attr in el.Attributes())
			{
				var name = attr.Name.LocalName;
				if (!HandledConditionAttributes.Contains(name))
				{
					diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Warning, "conditional-dropped",
						$"Conditional <{el.Name.LocalName}> uses unsupported condition attribute '{name}'; its content is not imported.",
						stableId));
					return null;
				}

				// $-substituted values (e.g. target='$fieldName' inside <generate>) and slash field
				// paths need runtime substitution/path hops the composer does not perform (B9).
				if (attr.Value.IndexOf('$') >= 0 || (name == "field" && attr.Value.IndexOf('/') >= 0))
				{
					diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Warning, "conditional-dropped",
						$"Conditional <{el.Name.LocalName}> condition attribute '{name}'='{attr.Value}' needs runtime substitution; its content is not imported.",
						stableId));
					return null;
				}
			}

			return new ViewCondition(
				negated,
				Attr(el, "target"),
				Attr(el, "is"),
				Attr(el, "excludesubclasses") == "true" || Attr(el, "excludesubclasses") == "yes",
				Attr(el, "field"),
				boolEquals: Attr(el, "boolequals") == null ? (bool?)null : Attr(el, "boolequals") == "true",
				intEquals: ParseNullableInt(Attr(el, "intequals")),
				intLessThan: ParseNullableInt(Attr(el, "intlessthan")),
				intGreaterThan: ParseNullableInt(Attr(el, "intgreaterthan")),
				intMemberOf: Attr(el, "intmemberof"),
				lengthAtLeast: ParseNullableInt(Attr(el, "lengthatleast")),
				lengthAtMost: ParseNullableInt(Attr(el, "lengthatmost")),
				guidEquals: Attr(el, "guidequals"));
		}

		private static int? ParseNullableInt(string value)
			=> value != null && int.TryParse(value, System.Globalization.NumberStyles.Integer,
				System.Globalization.CultureInfo.InvariantCulture, out var parsed)
				? parsed
				: (int?)null;

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

		// §19e: split a legacy visibleWritingSystems value (the legacy slice persists a comma-delimited ICU
		// locale list; layout authors also write space-separated). Null/blank => no override (full set).
		private static IReadOnlyList<string> ParseWsList(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return null;
			var parts = value.Split(new[] { ',', ' ', ';' }, System.StringSplitOptions.RemoveEmptyEntries);
			return parts.Length == 0 ? null : parts;
		}

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

		private static bool? ParseOptionalBool(string value)
		{
			if (string.IsNullOrEmpty(value))
				return null;
			if (bool.TryParse(value, out var parsed))
				return parsed;
			return null;
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
