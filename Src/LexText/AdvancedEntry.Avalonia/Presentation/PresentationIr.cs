using System.Collections.Generic;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Presentation;

public readonly record struct PresentationNodeId(string Value)
{
	public override string ToString() => Value;
}

public enum PresentationVisibility
{
	Always,
	IfData,
	Never,
	Conditional,
}

public sealed record VisibilitySpec(PresentationVisibility Kind, string? Expression = null);

public sealed record GhostSpec(
	string? GhostField,
	string? GhostWritingSystem,
	string? GhostLabel,
	string? GhostClass,
	string? GhostInitMethod);

public abstract record PresentationNode(PresentationNodeId Id)
{
	public string? Label { get; init; }
	public VisibilitySpec Visibility { get; init; } = new(PresentationVisibility.Always);
}

public sealed record PresentationLayout(PresentationNodeId Id) : PresentationNode(Id)
{
	public required string RootClass { get; init; }
	public required string RootType { get; init; }
	public required string RootName { get; init; }
	public required IReadOnlyList<PresentationNode> Children { get; init; }
}

public sealed record PresentationSection(PresentationNodeId Id) : PresentationNode(Id)
{
	public required IReadOnlyList<PresentationNode> Children { get; init; }
}

public sealed record PresentationField(PresentationNodeId Id) : PresentationNode(Id)
{
	public required string Field { get; init; }
	public bool IsRequired { get; init; }
}

public sealed record PresentationObject(PresentationNodeId Id) : PresentationNode(Id)
{
	public required string Field { get; init; }
	public string? Layout { get; init; }
	public GhostSpec? Ghost { get; init; }
	public IReadOnlyList<PresentationNode> Children { get; init; } = new List<PresentationNode>();
}

public sealed record PresentationSequence(PresentationNodeId Id) : PresentationNode(Id)
{
	public required string Field { get; init; }
	public string? Layout { get; init; }
	public GhostSpec? Ghost { get; init; }
	public IReadOnlyList<PresentationNode> ItemTemplate { get; init; } = new List<PresentationNode>();
	public bool IsVirtualized { get; init; } = true;
}

public sealed record PresentationUnknown(PresentationNodeId Id) : PresentationNode(Id)
{
	public required string Source { get; init; }
}