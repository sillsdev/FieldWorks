using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.Compilation;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.XmlContract;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Presentation;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Tests;

[TestFixture]
public sealed class PresentationCompilerSnapshotTests
{
	[Test]
	public void LexEntryDetailNormal_TopLevelSubsetMatchesSnapshot()
	{
		var repoRoot = TestRepoRoot.Find();
		var partsDir = Path.Combine(repoRoot, "DistFiles", "Language Explorer", "Configuration", "Parts");
		var snapshotsDir = Path.Combine(repoRoot, "Src", "LexText", "AdvancedEntry.Avalonia", "AdvancedEntry.Avalonia.Tests", "Snapshots");
		var snapshotPath = Path.Combine(snapshotsDir, "LexEntry-detail-Normal.top.json");

		var loader = new PartsLayoutLoader();
		var contract = loader.Load(new LayoutId("LexEntry", "detail", "Normal"), new[] { partsDir });
		var compiler = new PresentationCompiler();
		var ir = compiler.Compile(contract);

		var subset = SelectTopLevelSubset(ir);
		var actual = NormalizeNewlines(Serialize(subset)).TrimEnd();
		var expected = NormalizeNewlines(File.ReadAllText(snapshotPath)).TrimEnd();

		AssertSnapshotMatches(actual, expected, "LexEntry-detail-Normal.top.actual.json");
	}

	[Test]
	public void CompilationIsDeterministic()
	{
		var repoRoot = TestRepoRoot.Find();
		var partsDir = Path.Combine(repoRoot, "DistFiles", "Language Explorer", "Configuration", "Parts");

		var loader = new PartsLayoutLoader();
		var contract = loader.Load(new LayoutId("LexEntry", "detail", "Normal"), new[] { partsDir });
		var compiler = new PresentationCompiler();

		var ir1 = compiler.Compile(contract);
		var ir2 = compiler.Compile(contract);

		Assert.That(Serialize(SelectTopLevelSubset(ir1)), Is.EqualTo(Serialize(SelectTopLevelSubset(ir2))));
	}

	[Test]
	public void LexEntryDetailNormal_TopLevelNodesPreserveLayoutFocusOrder()
	{
		var repoRoot = TestRepoRoot.Find();
		var partsDir = Path.Combine(repoRoot, "DistFiles", "Language Explorer", "Configuration", "Parts");

		var loader = new PartsLayoutLoader();
		var contract = loader.Load(new LayoutId("LexEntry", "detail", "Normal"), new[] { partsDir });
		var compiler = new PresentationCompiler();
		var ir = compiler.Compile(contract);

		var topLevelRefs = ir.Children
			.Select(n => ExtractRef(n.Id.Value))
			.Where(refId => refId is not null)
			.ToArray();

		AssertAppearsBefore(topLevelRefs, "ChangeHandler", "LexemeForm");
		AssertAppearsBefore(topLevelRefs, "LexemeForm", "CitationFormAllV");
		AssertAppearsBefore(topLevelRefs, "CitationFormAllV", "Pronunciations");
		AssertAppearsBefore(topLevelRefs, "Pronunciations", "Senses");
	}

	private static object SelectTopLevelSubset(PresentationLayout layout)
	{
		var wantedOrder = new[]
		{
			"ChangeHandler",
			"LexemeForm",
			"CitationFormAllV",
			"Pronunciations",
			"Senses",
		};
		var wantedRefs = new HashSet<string>(wantedOrder, StringComparer.Ordinal);

		var nodes = layout.Children
			.Select(n => new
			{
				Ref = ExtractRef(n.Id.Value),
				Kind = n.GetType().Name,
				n.Label,
				Field = (n as PresentationField)?.Field ?? (n as PresentationObject)?.Field ?? (n as PresentationSequence)?.Field,
				Layout = (n as PresentationObject)?.Layout ?? (n as PresentationSequence)?.Layout,
				GhostLabel = (n as PresentationObject)?.Ghost?.GhostLabel ?? (n as PresentationSequence)?.Ghost?.GhostLabel,
				Visibility = n.Visibility.Kind.ToString(),
			})
			.Where(x => x.Ref is not null && wantedRefs.Contains(x.Ref))
			.OrderBy(x => Array.IndexOf(wantedOrder, x.Ref!))
			.ToArray();

		return new { Layout = layout.Id.Value, Nodes = nodes };
	}

	private static void AssertAppearsBefore(string?[] refs, string before, string after)
	{
		var beforeIndex = Array.IndexOf(refs, before);
		var afterIndex = Array.IndexOf(refs, after);

		Assert.That(beforeIndex, Is.GreaterThanOrEqualTo(0), $"Expected to find {before} in the top-level layout.");
		Assert.That(afterIndex, Is.GreaterThanOrEqualTo(0), $"Expected to find {after} in the top-level layout.");
		Assert.That(beforeIndex, Is.LessThan(afterIndex), $"Expected {before} to precede {after} in focus order.");
	}

	private static void AssertSnapshotMatches(string actual, string expected, string artifactName)
	{
		if (string.Equals(actual, expected, StringComparison.Ordinal))
			return;

		var artifactDir = Path.Combine(
			TestContext.CurrentContext.WorkDirectory,
			"TestArtifacts",
			nameof(PresentationCompilerSnapshotTests)
		);
		Directory.CreateDirectory(artifactDir);
		var actualPath = Path.Combine(artifactDir, artifactName);
		File.WriteAllText(actualPath, actual);
		TestContext.AddTestAttachment(actualPath);

		Assert.Fail($"Snapshot mismatch. Actual snapshot written to {actualPath}");
	}

	private static string? ExtractRef(string id)
	{
		var marker = ":part:";
		var idx = id.LastIndexOf(marker, StringComparison.Ordinal);
		if (idx < 0)
			return null;
		return id[(idx + marker.Length)..];
	}

	private static string Serialize(object value)
	{
		return JsonSerializer.Serialize(value, new JsonSerializerOptions
		{
			WriteIndented = true,
		});
	}

	private static string NormalizeNewlines(string s) => s.Replace("\r\n", "\n");
}
