using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.XmlContract;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Presentation;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.Compilation;

public sealed class PresentationCompiler
{
	private readonly HashSet<LayoutId> _activeLayouts = new();
	private readonly List<LayoutId> _layoutStack = new();

	public PresentationLayout Compile(ResolvedContract contract, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		_activeLayouts.Clear();
		_layoutStack.Clear();
		return CompileLayout(contract, contract.RootLayout, cancellationToken);
	}

	private PresentationLayout CompileLayout(ResolvedContract contract, LayoutId layoutId, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (!_activeLayouts.Add(layoutId))
		{
			var chain = string.Join(" -> ", _layoutStack.Append(layoutId).Select(l => l.ToString()));
			throw new InvalidOperationException($"Recursive layout reference detected while compiling '{layoutId}': {chain}");
		}

		_layoutStack.Add(layoutId);
		try
		{
			var layout = layoutId.Equals(contract.RootLayout)
				? contract.LayoutElement
				: contract.GetLayoutOrThrow(layoutId);

			var children = new List<PresentationNode>();
			foreach (var partRef in layout.Elements().Where(e => e.Name.LocalName == "part"))
			{
				cancellationToken.ThrowIfCancellationRequested();
				children.Add(CompileLayoutPart(layoutId, partRef, contract, cancellationToken));
			}

			return new PresentationLayout(new PresentationNodeId($"layout:{layoutId}"))
			{
				RootClass = layoutId.Class,
				RootType = layoutId.Type,
				RootName = layoutId.Name,
				Children = children,
				Label = (string?)layout.Attribute("name"),
			};
		}
		finally
		{
			_layoutStack.RemoveAt(_layoutStack.Count - 1);
			_activeLayouts.Remove(layoutId);
		}
	}

	private PresentationNode CompileLayoutPart(
		LayoutId layoutId,
		XElement layoutPartElement,
		ResolvedContract contract,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var partRef = (string?)layoutPartElement.Attribute("ref") ?? "";
		var label = (string?)layoutPartElement.Attribute("label");
		var visibility = ParseVisibility((string?)layoutPartElement.Attribute("visibility"));
		var param = (string?)layoutPartElement.Attribute("param");
		var indentedChildren = CompileIndentedChildren(layoutId, layoutPartElement, contract, cancellationToken);

		var partIdCandidates = GetPartIdCandidates(layoutId, partRef);
		var partElement = FindFirstPart(contract.PartsById, partIdCandidates);
		if (partElement is null)
		{
			// Some layouts inline their children under an <indent> block even when there is no corresponding
			// part in *Parts.xml (e.g., HeavySummary wrappers). In that case, keep the children.
			if (indentedChildren.Count > 0)
			{
				return new PresentationSection(new PresentationNodeId($"{layoutId}:indent:{partRef}"))
				{
					Label = label,
					Children = indentedChildren,
					Visibility = visibility,
				};
			}

			return new PresentationUnknown(new PresentationNodeId($"{layoutId}:part:{partRef}"))
			{
				Source = $"Missing part ref='{partRef}'",
				Label = label ?? partRef,
				Visibility = visibility,
			};
		}

		var compiled = CompilePartElement(layoutId, partRef, partElement, param, contract, cancellationToken);
		var adjusted = compiled with
		{
			Label = label ?? compiled.Label ?? partRef,
			Visibility = visibility.Kind == PresentationVisibility.Always ? compiled.Visibility : visibility,
		};

		if (indentedChildren.Count == 0)
			return adjusted;

		return new PresentationSection(new PresentationNodeId($"{layoutId}:indent:{partRef}"))
		{
			// Do not force a category unless the layout explicitly labeled this wrapper.
			Label = label,
			Children = new[] { adjusted }.Concat(indentedChildren).ToArray(),
			Visibility = visibility,
		};
	}

	private IReadOnlyList<PresentationNode> CompileIndentedChildren(
		LayoutId layoutId,
		XElement layoutPartElement,
		ResolvedContract contract,
		CancellationToken cancellationToken)
	{
		var indent = layoutPartElement.Elements().FirstOrDefault(e => e.Name.LocalName == "indent");
		if (indent is null)
			return Array.Empty<PresentationNode>();

		var children = new List<PresentationNode>();
		foreach (var childPart in indent.Elements().Where(e => e.Name.LocalName == "part"))
		{
			cancellationToken.ThrowIfCancellationRequested();
			children.Add(CompileLayoutPart(layoutId, childPart, contract, cancellationToken));
		}

		return children;
	}

	private PresentationNode CompilePartElement(
		LayoutId layoutId,
		string partRef,
		XElement partElement,
		string? param,
		ResolvedContract contract,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var id = new PresentationNodeId($"{layoutId}:part:{partRef}");

		// Many parts are just wrappers around a single slice/seq/obj.
		var content = partElement.Elements().FirstOrDefault();
		if (content is null)
			return new PresentationUnknown(id) { Source = $"Empty part '{partRef}'" };
		var command = ParseDisplayCommand(layoutId, partRef, content);
		command = ApplyParam(command, param);
		return TranslateDisplayCommand(id, layoutId, contract, command, cancellationToken);
	}

	private static DisplayCommand ApplyParam(DisplayCommand command, string? param)
	{
		if (string.IsNullOrWhiteSpace(param))
			return command;

		switch (command)
		{
			case DisplayCommandSeq seq when string.IsNullOrWhiteSpace(seq.Layout):
				return seq with { Layout = param };
			case DisplayCommandObj obj when string.IsNullOrWhiteSpace(obj.Layout):
				return obj with { Layout = param };
			default:
				return command;
		}
	}

	private static DisplayCommand ParseDisplayCommand(LayoutId layoutId, string partRef, XElement element)
	{
		return element.Name.LocalName switch
		{
			"slice" => new DisplayCommandSlice(
				(string?)element.Attribute("field") ?? "",
				(string?)element.Attribute("label")),
			"seq" => new DisplayCommandSeq(
				(string?)element.Attribute("field") ?? "",
				(string?)element.Attribute("layout"),
				(string?)element.Attribute("label"),
				ParseGhost(element)),
			"obj" => new DisplayCommandObj(
				(string?)element.Attribute("field") ?? "",
				(string?)element.Attribute("layout"),
				(string?)element.Attribute("label"),
				ParseGhost(element)),
			"if" => ParseIf(layoutId, partRef, element),
			_ => new DisplayCommandUnknown($"Unsupported element '{element.Name.LocalName}' in part '{partRef}'"),
		};
	}

	private static DisplayCommand ParseIf(LayoutId layoutId, string partRef, XElement ifElement)
	{
		var condition = string.Join(" ",
			new[]
			{
				"if",
				$"field='{(string?)ifElement.Attribute("field")}'",
				$"lengthatmost='{(string?)ifElement.Attribute("lengthatmost")}'",
				$"lengthatleast='{(string?)ifElement.Attribute("lengthatleast")}'",
			}.Where(s => !s.EndsWith("''", StringComparison.Ordinal)));

		var inner = ifElement.Elements().FirstOrDefault();
		if (inner is null)
			return new DisplayCommandUnknown($"Empty if in part '{partRef}'");

		return new DisplayCommandIf(condition, ParseDisplayCommand(layoutId, partRef, inner));
	}

	private PresentationNode TranslateDisplayCommand(
		PresentationNodeId id,
		LayoutId ownerLayout,
		ResolvedContract contract,
		DisplayCommand command,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		switch (command)
		{
			case DisplayCommandSlice slice:
				return new PresentationField(id)
				{
					Field = slice.Field,
					Label = slice.Label,
					IsRequired = RequiredFieldPolicy.IsRequired(ownerLayout.Class, slice.Field),
				};
			case DisplayCommandSeq seq:
				{
					var itemClass = FieldClassMap.GetItemClass(ownerLayout.Class, seq.Field, seq.Ghost);
					var template = CompileNestedTemplate(contract, itemClass, seq.Layout, cancellationToken);
					return new PresentationSequence(id)
					{
						Field = seq.Field,
						Layout = seq.Layout,
						Label = seq.Label,
						Ghost = seq.Ghost,
						ItemTemplate = template,
					};
				}
			case DisplayCommandObj obj:
				{
					var itemClass = FieldClassMap.GetItemClass(ownerLayout.Class, obj.Field, obj.Ghost);
					var children = CompileNestedTemplate(contract, itemClass, obj.Layout, cancellationToken);
					return new PresentationObject(id)
					{
						Field = obj.Field,
						Layout = obj.Layout,
						Label = obj.Label,
						Ghost = obj.Ghost,
						Children = children,
					};
				}
			case DisplayCommandIf iff:
				{
					var inner = TranslateDisplayCommand(id, ownerLayout, contract, iff.Inner, cancellationToken);
					return inner with { Visibility = new VisibilitySpec(PresentationVisibility.Conditional, iff.Condition) };
				}
			case DisplayCommandUnknown unknown:
				return new PresentationUnknown(id) { Source = unknown.Source };
			default:
				return new PresentationUnknown(id) { Source = $"Unknown DisplayCommand type '{command.GetType().Name}'" };
		}
	}

	private IReadOnlyList<PresentationNode> CompileNestedTemplate(
		ResolvedContract contract,
		string? className,
		string? layoutName,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (string.IsNullOrWhiteSpace(className))
			return Array.Empty<PresentationNode>();

		var name = string.IsNullOrWhiteSpace(layoutName) ? "Normal" : layoutName;
		var nestedLayoutId = new LayoutId(className!, "detail", name);
		if (!contract.LayoutsById.ContainsKey(nestedLayoutId))
			return Array.Empty<PresentationNode>();

		// Shipped configs can be self-referential (e.g., LexSense.Detail.Senses uses layout Normal to show sub-senses).
		// In the classic app, nested layouts are instantiated at runtime per-object; they are not expanded recursively at compile time.
		// Our Presentation IR currently expands templates, so we must cut off recursion to avoid stack overflow.
		if (_activeLayouts.Contains(nestedLayoutId))
		{
			var chain = string.Join(" -> ", _layoutStack.Append(nestedLayoutId).Select(l => l.ToString()));
			Trace.TraceWarning($"Recursive nested layout reference detected; truncating template expansion: {chain}");
			return new PresentationNode[]
			{
				new PresentationUnknown(new PresentationNodeId($"layout:{nestedLayoutId}:recursive"))
				{
					Source = $"Recursive layout reference detected; template expansion truncated: {chain}",
					Label = "(recursive layout)",
				},
			};
		}

		var nestedLayout = CompileLayout(contract, nestedLayoutId, cancellationToken);
		return nestedLayout.Children;
	}

	private static GhostSpec? ParseGhost(XElement element)
	{
		var ghost = (string?)element.Attribute("ghost");
		var ghostWs = (string?)element.Attribute("ghostWs");
		var ghostLabel = (string?)element.Attribute("ghostLabel");
		var ghostClass = (string?)element.Attribute("ghostClass");
		var ghostInitMethod = (string?)element.Attribute("ghostInitMethod");

		if (ghost is null && ghostWs is null && ghostLabel is null && ghostClass is null && ghostInitMethod is null)
			return null;

		return new GhostSpec(ghost, ghostWs, ghostLabel, ghostClass, ghostInitMethod);
	}

	private static VisibilitySpec ParseVisibility(string? visibility)
	{
		return visibility?.ToLowerInvariant() switch
		{
			"always" => new VisibilitySpec(PresentationVisibility.Always),
			"ifdata" => new VisibilitySpec(PresentationVisibility.IfData),
			"never" => new VisibilitySpec(PresentationVisibility.Never),
			null or "" => new VisibilitySpec(PresentationVisibility.Always),
			_ => new VisibilitySpec(PresentationVisibility.Conditional, visibility),
		};
	}

	private static IReadOnlyList<string> GetPartIdCandidates(LayoutId layoutId, string partRef)
	{
		// FieldWorks convention in shipped files: "{Class}-{Detail|Jt|...}-{Ref}" (case varies).
		var typeFragment = layoutId.Type.Equals("detail", StringComparison.OrdinalIgnoreCase) ? "Detail" : layoutId.Type;
		return new[]
		{
			$"{layoutId.Class}-{typeFragment}-{partRef}",
			$"{layoutId.Class}-{layoutId.Type}-{partRef}",
			$"{layoutId.Class}-{layoutId.Type.ToLowerInvariant()}-{partRef}",
		};
	}

	private static XElement? FindFirstPart(IReadOnlyDictionary<string, XElement> partsById, IReadOnlyList<string> candidates)
	{
		foreach (var candidate in candidates)
		{
			if (partsById.TryGetValue(candidate, out var part))
				return part;
		}

		return null;
	}
}