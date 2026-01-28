using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Presentation;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.Compilation;

internal abstract record DisplayCommand;

internal sealed record DisplayCommandSlice(string Field, string? Label) : DisplayCommand;

internal sealed record DisplayCommandSeq(string Field, string? Layout, string? Label, GhostSpec? Ghost) : DisplayCommand;

internal sealed record DisplayCommandObj(string Field, string? Layout, string? Label, GhostSpec? Ghost) : DisplayCommand;

internal sealed record DisplayCommandIf(string Condition, DisplayCommand Inner) : DisplayCommand;

internal sealed record DisplayCommandUnknown(string Source) : DisplayCommand;