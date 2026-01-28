namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.XmlContract;

public readonly record struct LayoutId(string Class, string Type, string Name)
{
	public override string ToString() => $"{Class}/{Type}/{Name}";
}