using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.XmlContract;

public sealed record ResolvedContract(
	LayoutId RootLayout,
	XElement LayoutElement,
	IReadOnlyDictionary<LayoutId, XElement> LayoutsById,
	IReadOnlyDictionary<string, XElement> PartsById,
	IReadOnlyList<string> SearchRoots,
	string ConfigurationFingerprint)
{
	public XElement GetPartOrThrow(string partId)
	{
		if (!PartsById.TryGetValue(partId, out var part))
			throw new InvalidOperationException($"Part not found: {partId}");
		return part;
	}

	public XElement GetLayoutOrThrow(LayoutId layoutId)
	{
		if (!LayoutsById.TryGetValue(layoutId, out var layout))
			throw new InvalidOperationException($"Layout not found: {layoutId}");
		return layout;
	}
}