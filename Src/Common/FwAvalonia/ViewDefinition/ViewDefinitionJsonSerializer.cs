// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>
	/// Canonical JSON serialization of the typed view definition (tasks 9.2/9.4, per
	/// `canonical-view-definition-design.md`): deterministic property order, defaults omitted, and a
	/// `formatVersion` header so per-project overrides can be validated. This is the migration
	/// tooling core — shipped XML compiles to the typed IR (existing importer), the IR serializes to
	/// canonical JSON, and a gated surface can load JSON with the XML importer retained as fallback.
	/// </summary>
	public static class ViewDefinitionJsonSerializer
	{
		/// <summary>Successor version line to the legacy XML `LayoutVersionNumber`.</summary>
		public const int FormatVersion = 1;

		public static string Serialize(ViewDefinitionModel model)
		{
			if (model == null) throw new ArgumentNullException(nameof(model));

			var root = new JObject
			{
				["formatVersion"] = FormatVersion,
				["class"] = model.ClassName,
				["name"] = model.LayoutName,
				["type"] = model.LayoutType,
				["nodes"] = new JArray(model.Roots.Select(WriteNode))
			};
			return root.ToString(Formatting.Indented);
		}

		public static ViewDefinitionModel Deserialize(string json)
		{
			if (string.IsNullOrEmpty(json)) throw new ArgumentNullException(nameof(json));
			var root = JObject.Parse(json);

			var version = (int?)root["formatVersion"] ?? -1;
			if (version != FormatVersion)
				throw new InvalidDataException($"Unsupported view-definition formatVersion {version} (expected {FormatVersion}).");

			var nodes = ((JArray)root["nodes"] ?? new JArray()).Select(ReadNode).ToList();
			return new ViewDefinitionModel(
				(string)root["class"] ?? "",
				(string)root["name"] ?? "",
				(string)root["type"] ?? "detail",
				nodes,
				Array.Empty<ViewDiagnostic>());
		}

		private static JObject WriteNode(ViewNode node)
		{
			// Deterministic order; defaults omitted so committed definitions diff cleanly.
			var o = new JObject
			{
				["id"] = node.StableId,
				["kind"] = node.Kind.ToString()
			};
			AddIfPresent(o, "label", node.Label);
			AddIfPresent(o, "abbr", node.Abbreviation);
			AddIfPresent(o, "field", node.Field);
			AddIfPresent(o, "editor", node.RawEditor);
			if (node.EditorClassification != EditorClassification.GroupingNone)
				o["editorClass"] = node.EditorClassification.ToString();
			AddIfPresent(o, "ws", node.WritingSystem);
			if (node.Visibility != ViewVisibility.Always)
				o["visibility"] = node.Visibility.ToString();
			if (node.Expansion != ViewExpansion.NotApplicable)
				o["expansion"] = node.Expansion.ToString();
			if (node.Indented)
				o["indented"] = true;
			AddIfPresent(o, "targetLayout", node.TargetLayout);
			AddIfPresent(o, "localizationKey", node.LocalizationKey);
			AddIfPresent(o, "automationId", node.AutomationId);
			if (node.Routing != SurfaceRouting.Inherit)
				o["routing"] = node.Routing.ToString();
			if (node.BoldEmphasis)
				o["bold"] = true;
			if (node.FontScalePercent != 0)
				o["fontScalePercent"] = node.FontScalePercent;
			AddIfPresent(o, "menu", node.MenuId);
			AddIfPresent(o, "contextMenu", node.ContextMenuId);
			AddIfPresent(o, "hotlinks", node.HotlinksId);
			AddIfPresent(o, "ghost", node.GhostField);
			AddIfPresent(o, "ghostWs", node.GhostWs);
			AddIfPresent(o, "ghostClass", node.GhostClass);
			AddIfPresent(o, "ghostLabel", node.GhostLabel);
			if (node.ForVariant)
				o["forVariant"] = true;
			AddIfPresent(o, "ghostInitMethod", node.GhostInitMethod);
			if (node.Condition != null)
				o["condition"] = WriteCondition(node.Condition);
			// B7: the chooser jump-link block, reserved in the canonical schema (xml-retirement-blockers
			// cross-cutting deadline) — label/tool/type/target exactly as the legacy chooserLink carries.
			if (node.ChooserLinks.Count > 0)
				o["chooserLinks"] = new JArray(node.ChooserLinks.Select(WriteChooserLink));
			if (node.Children.Count > 0)
				o["children"] = new JArray(node.Children.Select(WriteNode));
			return o;
		}

		private static JObject WriteChooserLink(ViewChooserLink link)
		{
			var o = new JObject();
			// "goto" is the legacy default; anything else must be explicit.
			if (!string.Equals(link.Type, "goto", StringComparison.Ordinal))
				o["type"] = link.Type;
			AddIfPresent(o, "label", link.Label);
			AddIfPresent(o, "tool", link.Tool);
			AddIfPresent(o, "target", link.Target);
			return o;
		}

		private static ViewChooserLink ReadChooserLink(JToken token)
		{
			var o = (JObject)token;
			return new ViewChooserLink(
				(string)o["type"],
				(string)o["label"],
				(string)o["tool"],
				(string)o["target"]);
		}

		// B3: the structured conditional-display metadata (legacy <if>/<ifnot>/<where>), reserved in
		// the canonical schema before Layer-1 freezes (xml-retirement-blockers, cross-cutting deadline).
		private static JObject WriteCondition(ViewCondition condition)
		{
			var o = new JObject();
			if (condition.Negated)
				o["negated"] = true;
			AddIfPresent(o, "target", condition.Target);
			AddIfPresent(o, "is", condition.IsClass);
			if (condition.ExcludeSubclasses)
				o["excludeSubclasses"] = true;
			AddIfPresent(o, "field", condition.Field);
			if (condition.BoolEquals.HasValue)
				o["boolEquals"] = condition.BoolEquals.Value;
			if (condition.IntEquals.HasValue)
				o["intEquals"] = condition.IntEquals.Value;
			if (condition.IntLessThan.HasValue)
				o["intLessThan"] = condition.IntLessThan.Value;
			if (condition.IntGreaterThan.HasValue)
				o["intGreaterThan"] = condition.IntGreaterThan.Value;
			AddIfPresent(o, "intMemberOf", condition.IntMemberOf);
			if (condition.LengthAtLeast.HasValue)
				o["lengthAtLeast"] = condition.LengthAtLeast.Value;
			if (condition.LengthAtMost.HasValue)
				o["lengthAtMost"] = condition.LengthAtMost.Value;
			AddIfPresent(o, "guidEquals", condition.GuidEquals);
			return o;
		}

		private static ViewCondition ReadCondition(JObject o)
		{
			if (o == null)
				return null;
			return new ViewCondition(
				(bool?)o["negated"] ?? false,
				(string)o["target"],
				(string)o["is"],
				(bool?)o["excludeSubclasses"] ?? false,
				(string)o["field"],
				(bool?)o["boolEquals"],
				(int?)o["intEquals"],
				(int?)o["intLessThan"],
				(int?)o["intGreaterThan"],
				(string)o["intMemberOf"],
				(int?)o["lengthAtLeast"],
				(int?)o["lengthAtMost"],
				(string)o["guidEquals"]);
		}

		private static ViewNode ReadNode(JToken token)
		{
			var o = (JObject)token;
			var children = ((JArray)o["children"])?.Select(ReadNode).ToList()
				?? (IReadOnlyList<ViewNode>)Array.Empty<ViewNode>();

			return new ViewNode(
				(string)o["id"],
				ParseEnum(o, "kind", ViewNodeKind.Field),
				(string)o["label"],
				(string)o["abbr"],
				(string)o["field"],
				(string)o["editor"],
				ParseEnum(o, "editorClass", EditorClassification.GroupingNone),
				(string)o["ws"],
				ParseEnum(o, "visibility", ViewVisibility.Always),
				ParseEnum(o, "expansion", ViewExpansion.NotApplicable),
				(bool?)o["indented"] ?? false,
				(string)o["targetLayout"],
				children,
				(string)o["localizationKey"],
				(string)o["automationId"],
				ParseEnum(o, "routing", SurfaceRouting.Inherit),
				(bool?)o["bold"] ?? false,
				(int?)o["fontScalePercent"] ?? 0,
				(string)o["menu"],
				(string)o["contextMenu"],
				(string)o["hotlinks"],
				(string)o["ghost"],
				(string)o["ghostWs"],
				(string)o["ghostClass"],
				(string)o["ghostLabel"],
				(bool?)o["forVariant"] ?? false,
				ghostInitMethod: (string)o["ghostInitMethod"],
				condition: ReadCondition((JObject)o["condition"]),
				chooserLinks: ((JArray)o["chooserLinks"])?.Select(ReadChooserLink).ToList());
		}

		private static T ParseEnum<T>(JObject o, string name, T fallback) where T : struct
		{
			var value = (string)o[name];
			return value != null && Enum.TryParse<T>(value, out var parsed) ? parsed : fallback;
		}

		private static void AddIfPresent(JObject o, string name, string value)
		{
			if (!string.IsNullOrEmpty(value))
				o[name] = value;
		}
	}
}
