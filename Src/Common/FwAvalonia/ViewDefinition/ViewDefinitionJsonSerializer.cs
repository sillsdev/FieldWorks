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
	/// Canonical JSON serialization of the typed view definition: deterministic property order,
	/// defaults omitted, and a <c>formatVersion</c> header. This is an interchange format for the
	/// complete typed model; it is not the current layout-persistence store.
	/// </summary>
	public static class ViewDefinitionJsonSerializer
	{
		/// <summary>The canonical JSON format version.</summary>
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
				["requestedIdentity"] = WriteIdentity(model.RequestedIdentity),
				["resolvedIdentity"] = WriteIdentity(model.ResolvedIdentity),
				["nodes"] = new JArray(model.Roots.Select(WriteNode))
			};
			AddIfNotNull(root, "choiceGuid", model.ChoiceGuid);
			if (!string.Equals(model.RequestedLayoutName, model.ResolvedLayoutName,
				StringComparison.Ordinal))
				root["requestedName"] = model.RequestedLayoutName;
			AddIfNotNull(root, "requestedChoiceGuid", model.RequestedChoiceGuid);
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
			var resolvedName = (string)root["name"] ?? "";
			var resolvedChoice = (string)root["choiceGuid"];
			var resolvedIdentity = ReadIdentity((JObject)root["resolvedIdentity"])
				?? new ViewDefinitionIdentity((string)root["class"] ?? "", (string)root["type"] ?? "detail",
					resolvedName, resolvedChoice);
			var requestedIdentity = ReadIdentity((JObject)root["requestedIdentity"])
				?? new ViewDefinitionIdentity((string)root["class"] ?? "", (string)root["type"] ?? "detail",
					(string)root["requestedName"] ?? resolvedName, (string)root["requestedChoiceGuid"]);
			var model = new ViewDefinitionModel(
				resolvedIdentity.ClassName,
				resolvedIdentity.LayoutName,
				resolvedIdentity.LayoutType,
				nodes,
				Array.Empty<ViewDiagnostic>(),
				resolvedIdentity.ChoiceGuid);
			return model.WithLayoutIdentities(requestedIdentity, resolvedIdentity);
		}

		private static JObject WriteIdentity(ViewDefinitionIdentity identity)
		{
			var value = new JObject
			{
				["class"] = identity.ClassName,
				["type"] = identity.LayoutType,
				["name"] = identity.LayoutName
			};
			AddIfNotNull(value, "choiceGuid", identity.ChoiceGuid);
			return value;
		}

		private static ViewDefinitionIdentity? ReadIdentity(JObject value)
		{
			if (value == null)
				return null;
			return new ViewDefinitionIdentity((string)value["class"] ?? "",
				(string)value["type"] ?? "detail", (string)value["name"] ?? "",
				(string)value["choiceGuid"]);
		}

		private static JObject WriteNode(ViewNode node)
		{
			// Deterministic order; defaults omitted so committed definitions diff cleanly.
			var o = new JObject
			{
				["id"] = node.StableId,
				["kind"] = node.Kind.ToString()
			};
			AddIfNotNull(o, "sourceCallerPath", node.SourceCallerPath);
			AddIfNotNull(o, "sourceCallerXml", node.SourceCallerXml);
			AddIfPresent(o, "label", node.Label);
			AddIfPresent(o, "abbr", node.Abbreviation);
			AddIfPresent(o, "field", node.Field);
			AddIfPresent(o, "editor", node.RawEditor);
			if (node.EditorClassification != EditorClassification.GroupingNone)
				o["editorClass"] = node.EditorClassification.ToString();
			AddIfPresent(o, "ws", node.WritingSystem);
			AddIfNotNull(o, "optionalWs", node.OptionalWritingSystem);
			if (node.ForceIncludeEnglish)
				o["forceIncludeEnglish"] = true;
			if (node.Visibility != ViewVisibility.Always)
				o["visibility"] = node.Visibility.ToString();
			if (node.Expansion != ViewExpansion.NotApplicable)
				o["expansion"] = node.Expansion.ToString();
			if (node.Indented)
				o["indented"] = true;
			AddIfPresent(o, "targetLayout", node.TargetLayout);
			AddIfNotNull(o, "layoutChoiceField", node.LayoutChoiceField);
			if (node.VisibleWritingSystems != null)
				o["visibleWritingSystems"] = new JArray(node.VisibleWritingSystems);
			AddIfPresent(o, "localizationKey", node.LocalizationKey);
			AddIfPresent(o, "automationId", node.AutomationId);
			if (node.Routing != HostRouting.Inherit)
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
			AddIfNotNull(o, "customEditorClass", node.CustomEditorClass);
			AddIfNotNull(o, "customEditorAssembly", node.CustomEditorAssembly);
			AddIfPresent(o, "ghostInitMethod", node.GhostInitMethod);
			if (node.Condition != null)
				o["condition"] = WriteCondition(node.Condition);
			// The chooser jump-link block -- label/tool/type/target exactly as the legacy
			// chooserLink carries.
			if (node.ChooserLinks.Count > 0)
				o["chooserLinks"] = new JArray(node.ChooserLinks.Select(WriteChooserLink));
			if (node.EnumStringList != null)
			{
				var list = new JObject { ["ids"] = new JArray(node.EnumStringList.Ids) };
				AddIfNotNull(list, "group", node.EnumStringList.Group);
				o["enumStringList"] = list;
			}
			if (node.ToggleValue)
				o["toggleValue"] = true;
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

		// The structured conditional-display metadata (legacy <if>/<ifnot>/<where>).
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
				ParseEnum(o, "routing", HostRouting.Inherit),
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
				customEditorClass: (string)o["customEditorClass"],
				customEditorAssembly: (string)o["customEditorAssembly"],
				ghostInitMethod: (string)o["ghostInitMethod"],
				condition: ReadCondition((JObject)o["condition"]),
				chooserLinks: ((JArray)o["chooserLinks"])?.Select(ReadChooserLink).ToList(),
				enumStringList: ReadStringList((JObject)o["enumStringList"]),
				visibleWritingSystems: ((JArray)o["visibleWritingSystems"])?.Values<string>().ToList(),
				toggleValue: (bool?)o["toggleValue"] ?? false,
				sourceCallerPath: (string)o["sourceCallerPath"],
				layoutChoiceField: (string)o["layoutChoiceField"],
				sourceCallerXml: (string)o["sourceCallerXml"],
				optionalWritingSystem: (string)o["optionalWs"],
				forceIncludeEnglish: (bool?)o["forceIncludeEnglish"] ?? false);
		}

		private static ViewStringList ReadStringList(JObject o)
		{
			if (o == null)
				return null;
			return new ViewStringList(((JArray)o["ids"])?.Values<string>().ToList(),
				(string)o["group"]);
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

		private static void AddIfNotNull(JObject o, string name, string value)
		{
			if (value != null)
				o[name] = value;
		}
	}
}
