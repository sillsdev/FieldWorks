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
	/// Canonical JSON wire format for a per-project <see cref="ViewDefinitionOverride"/> (task 9.2 step 3,
	/// per `canonical-view-definition-design.md` Layer 2): deterministic, sparse, keyed by StableId, with a
	/// `formatVersion` header that succeeds the legacy `LayoutVersionNumber`. Mirrors the conventions of
	/// <see cref="ViewDefinitionJsonSerializer"/> (Newtonsoft, ordered keys, defaults omitted) so the
	/// override store and the base store read alike and diff cleanly under review.
	/// </summary>
	public static class ViewDefinitionOverrideJsonSerializer
	{
		// Stable wire tokens for the operation kinds (camelCase, decoupled from the C# enum names).
		private static readonly Dictionary<ViewOverrideOperationKind, string> OpToWire =
			new Dictionary<ViewOverrideOperationKind, string>
			{
				{ ViewOverrideOperationKind.SetVisibility, "setVisibility" },
				{ ViewOverrideOperationKind.SetLabel, "setLabel" },
				{ ViewOverrideOperationKind.ReorderChildren, "reorderChildren" },
				{ ViewOverrideOperationKind.HideNode, "hideNode" },
				{ ViewOverrideOperationKind.AddNode, "addNode" },
				{ ViewOverrideOperationKind.DuplicateNode, "duplicateNode" }
			};

		private static readonly Dictionary<string, ViewOverrideOperationKind> WireToOp =
			OpToWire.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.Ordinal);

		public static string Serialize(ViewDefinitionOverride patch)
		{
			if (patch == null) throw new ArgumentNullException(nameof(patch));

			var root = new JObject
			{
				["formatVersion"] = patch.FormatVersion,
				["class"] = patch.ClassName,
				["name"] = patch.LayoutName,
				["type"] = patch.LayoutType,
				["operations"] = new JArray(patch.Operations.Select(WriteOperation))
			};

			// Diagnostics are the audit lane: present only when the override had non-representable parts.
			if (patch.Diagnostics.Count > 0)
				root["diagnostics"] = new JArray(patch.Diagnostics.Select(WriteDiagnostic));

			return root.ToString(Formatting.Indented);
		}

		public static ViewDefinitionOverride Deserialize(string json)
		{
			if (string.IsNullOrEmpty(json)) throw new ArgumentNullException(nameof(json));
			var root = JObject.Parse(json);

			var version = (int?)root["formatVersion"] ?? -1;
			if (version != ViewDefinitionOverride.CurrentFormatVersion)
				throw new InvalidDataException(
					$"Unsupported override formatVersion {version} (expected {ViewDefinitionOverride.CurrentFormatVersion}).");

			var operations = ((JArray)root["operations"] ?? new JArray()).Select(ReadOperation).ToList();
			var diagnostics = ((JArray)root["diagnostics"] ?? new JArray()).Select(ReadDiagnostic).ToList();

			return new ViewDefinitionOverride(
				(string)root["class"] ?? "",
				(string)root["name"] ?? "",
				(string)root["type"] ?? "detail",
				operations,
				diagnostics,
				version);
		}

		private static JObject WriteOperation(ViewOverrideOperation op)
		{
			var o = new JObject
			{
				["op"] = OpToWire[op.Kind],
				["id"] = op.StableId
			};
			switch (op.Kind)
			{
				case ViewOverrideOperationKind.SetVisibility:
					o["visibility"] = op.Visibility?.ToString();
					break;
				case ViewOverrideOperationKind.SetLabel:
					o["label"] = op.Label;
					break;
				case ViewOverrideOperationKind.ReorderChildren:
					o["childOrder"] = new JArray(op.ChildOrder);
					break;
				case ViewOverrideOperationKind.HideNode:
					break;
				case ViewOverrideOperationKind.AddNode:
					o["parent"] = op.ParentStableId;
					o["index"] = op.Index;
					o["nodeKind"] = op.NodeKind?.ToString();
					if (op.Label != null) o["label"] = op.Label;
					if (op.Field != null) o["field"] = op.Field;
					if (op.Editor != null) o["editor"] = op.Editor;
					if (op.WritingSystem != null) o["ws"] = op.WritingSystem;
					if (op.Visibility.HasValue) o["visibility"] = op.Visibility.Value.ToString();
					break;
				case ViewOverrideOperationKind.DuplicateNode:
					o["source"] = op.SourceStableId;
					o["parent"] = op.ParentStableId;
					o["index"] = op.Index;
					break;
			}
			return o;
		}

		private static ViewOverrideOperation ReadOperation(JToken token)
		{
			var o = (JObject)token;
			var wire = (string)o["op"];
			if (wire == null || !WireToOp.TryGetValue(wire, out var kind))
				throw new InvalidDataException($"Unknown override operation '{wire}'.");

			var stableId = (string)o["id"];
			switch (kind)
			{
				case ViewOverrideOperationKind.SetVisibility:
					var visText = (string)o["visibility"];
					var vis = ParseEnum<ViewVisibility>(visText, "visibility");
					return new ViewOverrideOperation(kind, stableId, visibility: vis);
				case ViewOverrideOperationKind.SetLabel:
					return new ViewOverrideOperation(kind, stableId, label: (string)o["label"]);
				case ViewOverrideOperationKind.ReorderChildren:
					var order = ((JArray)o["childOrder"] ?? new JArray()).Select(t => (string)t).ToList();
					return new ViewOverrideOperation(kind, stableId, childOrder: order);
				case ViewOverrideOperationKind.AddNode:
					var addKindText = (string)o["nodeKind"];
					var addKind = addKindText == null
						? (ViewNodeKind?)null
						: ParseEnum<ViewNodeKind>(addKindText, "nodeKind");
					var addVisText = (string)o["visibility"];
					var addVis = addVisText == null
						? (ViewVisibility?)null
						: ParseEnum<ViewVisibility>(addVisText, "visibility");
					return new ViewOverrideOperation(kind, stableId,
						visibility: addVis, label: (string)o["label"],
						parentStableId: (string)o["parent"], index: (int?)o["index"],
						nodeKind: addKind, field: (string)o["field"], editor: (string)o["editor"],
						writingSystem: (string)o["ws"]);
				case ViewOverrideOperationKind.DuplicateNode:
					return new ViewOverrideOperation(kind, stableId,
						parentStableId: (string)o["parent"], index: (int?)o["index"],
						sourceStableId: (string)o["source"]);
				default:
					return new ViewOverrideOperation(kind, stableId);
			}
		}

		private static JObject WriteDiagnostic(ViewDiagnostic diag)
			=> new JObject
			{
				["severity"] = diag.Severity.ToString(),
				["code"] = diag.Code,
				["path"] = diag.NodePath,
				["message"] = diag.Message
			};

		private static ViewDiagnostic ReadDiagnostic(JToken token)
		{
			var o = (JObject)token;
			var severity = ParseEnum<ViewDiagnosticSeverity>((string)o["severity"], "severity");
			return new ViewDiagnostic(severity, (string)o["code"], (string)o["message"], (string)o["path"]);
		}

		// Parses an enum value from committed JSON, turning a null/garbage token into a controlled
		// InvalidDataException (the load lane catches it) rather than a raw ArgumentException/NRE.
		private static TEnum ParseEnum<TEnum>(string text, string field) where TEnum : struct
		{
			if (string.IsNullOrEmpty(text) || !Enum.TryParse<TEnum>(text, ignoreCase: false, out var value)
				|| !Enum.IsDefined(typeof(TEnum), value))
				throw new InvalidDataException($"Invalid {field} value '{text}' in override patch.");
			return value;
		}
	}
}
