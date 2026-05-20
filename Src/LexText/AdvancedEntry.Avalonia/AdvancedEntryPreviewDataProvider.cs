using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SIL.FieldWorks.Common.Avalonia.Preview;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.Compilation;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.XmlContract;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.PropertyGrid;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Staging;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.ViewModels;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia;

internal sealed class AdvancedEntryPreviewDataProvider : IFwPreviewDataProvider
{
	public object? CreateDataContext(string dataMode)
	{
		var vm = new MainWindowViewModel();
		vm.StartLoading(ct => Task.Run(() => BuildEntry(dataMode, ct), ct));
		return vm;
	}

	private static object? BuildEntry(string dataMode, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var contract = LoadShippedContract();
		cancellationToken.ThrowIfCancellationRequested();

		var cacheKey = PresentationCompilationCacheKey.ForPreview(contract);
		var isCacheHit = PresentationCompilationCache.TryGet(cacheKey, out var cached);

		var ir = cached;
		if (!isCacheHit)
		{
			AdvancedEntryTrace.Info($"Presentation IR cache miss ({cacheKey})");
			var sw = Stopwatch.StartNew();
			ir = PresentationCompilationCache.GetOrAdd(cacheKey, () =>
			{
				var compiler = new PresentationCompiler();
				return compiler.Compile(contract, cancellationToken);
			});
			sw.Stop();
			AdvancedEntryTrace.Info($"Presentation IR compilation completed in {sw.ElapsedMilliseconds} ms ({cacheKey})");
		}
		else
		{
			AdvancedEntryTrace.Info($"Presentation IR cache hit ({cacheKey})");
		}

		cancellationToken.ThrowIfCancellationRequested();

		var state = new StagedEntryState(ir!.RootClass);
		if (string.Equals(dataMode, "sample", StringComparison.OrdinalIgnoreCase))
		{
			ApplySampleData(state);
		}

		return new StagedObjectView("LexEntry", state.RootClass, state.Root, ir.Children);
	}

	private static ResolvedContract LoadShippedContract()
	{
		var partsDir = FindShippedPartsDirectory();

		var loader = new PartsLayoutLoader();
		return loader.Load(new LayoutId("LexEntry", "detail", "Normal"), new[] { partsDir });
	}

	private static string FindShippedPartsDirectory()
	{
		var relativePartsPath = Path.Combine(
			"DistFiles",
			"Language Explorer",
			"Configuration",
			"Parts");
		var candidates = new[]
		{
			Environment.CurrentDirectory,
			AppContext.BaseDirectory,
			Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
		}
			.Where(p => !string.IsNullOrWhiteSpace(p))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		foreach (var start in candidates)
		{
			var dir = new DirectoryInfo(start);
			for (var i = 0; i < 8 && dir is not null; i++)
			{
				var partsDir = Path.Combine(dir.FullName, relativePartsPath);
				if (Directory.Exists(partsDir))
				{
					return partsDir;
				}

				dir = dir.Parent;
			}
		}

		throw new DirectoryNotFoundException(
			$"Parts directory not found. Expected '{relativePartsPath}' relative to one of: {string.Join("; ", candidates)}");
	}

	private static void ApplySampleData(StagedEntryState state)
	{
		state.Root.Fields["CitationForm"] = "Sample";

		var lexemeForm = state.Root.GetOrCreateObject("LexemeForm", "MoStemAllomorph");
		lexemeForm.Fields["Form"] = "sample";

		var senses = state.Root.GetOrCreateSequence("Senses", "LexSense");
		senses.SetCount(2);

		var sense1 = senses.EnsureItem(0);
		sense1.Fields["Gloss"] = "sample (gloss 1)";
		var examples1 = sense1.GetOrCreateSequence("Examples", "LexExampleSentence");
		examples1.SetCount(1);
		examples1.EnsureItem(0).Fields["Example"] = "This is a sample example.";

		var sense2 = senses.EnsureItem(1);
		sense2.Fields["Gloss"] = "sample (gloss 2)";
		var examples2 = sense2.GetOrCreateSequence("Examples", "LexExampleSentence");
		examples2.SetCount(1);
		examples2.EnsureItem(0).Fields["Example"] = "Another example.";
	}
}