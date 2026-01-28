using System;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.XmlContract;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.Compilation;

public readonly record struct PresentationCompilationCacheKey(
	string ProjectId,
	LayoutId RootLayout,
	string ConfigurationFingerprint)
{
	private string ShortFingerprint
		=> string.IsNullOrEmpty(ConfigurationFingerprint)
			? ""
			: (ConfigurationFingerprint.Length <= 12 ? ConfigurationFingerprint : ConfigurationFingerprint[..12]);

	public override string ToString()
		=> $"project='{ProjectId}' root='{RootLayout}' fingerprint='{ShortFingerprint}'";

	public static PresentationCompilationCacheKey ForPreview(ResolvedContract contract)
		=> new("preview", contract.RootLayout, contract.ConfigurationFingerprint);
}
