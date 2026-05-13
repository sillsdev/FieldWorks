using System;
using System.ComponentModel;
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
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Services;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.ViewModels;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia;

internal sealed class AdvancedEntryPreviewDataProvider : IFwPreviewDataProvider
{
	public object? CreateDataContext(string dataMode)
	{
		var vm = new MainWindowViewModel();
		vm.StartLoading(ct => Task.Run(() => BuildEntry(dataMode, ct), ct));
		return vm;
	}

	private static (object? Entry, IDisposable? Lifetime) BuildEntry(string dataMode, CancellationToken cancellationToken)
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

		var cache = CreateMemoryOnlyCache();
		var session = new AdvancedEntryEditSession(
			cache,
			"AdvancedEntry: Edit",
			"AdvancedEntry: Undo"
		);

		try
		{
			var entry = cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			if (string.Equals(dataMode, "sample", StringComparison.OrdinalIgnoreCase))
			{
				ApplySampleData(cache, entry);
			}

			return (
				Entry: new LcmObjectView("LexEntry", cache, entry, ir!.Children),
				Lifetime: new PreviewLifetime(session, cache)
			);
		}
		catch
		{
			session.Dispose();
			cache.Dispose();
			throw;
		}
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

	private static void ApplySampleData(LcmCache cache, ILexEntry entry)
	{
		var wsVern = cache.DefaultVernWs;
		var wsAnal = cache.DefaultAnalWs > 0 ? cache.DefaultAnalWs : cache.DefaultVernWs;

		// LexEntry fields
		entry.CitationForm.set_String(wsVern, TsStringUtils.MakeString("Sample", wsVern));

		// LexemeForm (ghost: MoStemAllomorph)
		entry.LexemeFormOA = cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
		entry.LexemeFormOA.Form.set_String(wsVern, TsStringUtils.MakeString("sample", wsVern));

		// Senses -> Examples
		var senseFactory = cache.ServiceLocator.GetInstance<ILexSenseFactory>();
		var exampleFactory = cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>();

		var sense1 = senseFactory.Create();
		entry.SensesOS.Add(sense1);
		sense1.Gloss.set_String(wsAnal, TsStringUtils.MakeString("sample (gloss 1)", wsAnal));
		var ex1 = exampleFactory.Create();
		sense1.ExamplesOS.Add(ex1);
		ex1.Example.set_String(wsVern, TsStringUtils.MakeString("This is a sample example.", wsVern));

		var sense2 = senseFactory.Create();
		entry.SensesOS.Add(sense2);
		sense2.Gloss.set_String(wsAnal, TsStringUtils.MakeString("sample (gloss 2)", wsAnal));
		var ex2 = exampleFactory.Create();
		sense2.ExamplesOS.Add(ex2);
		ex2.Example.set_String(wsVern, TsStringUtils.MakeString("Another example.", wsVern));
	}

	private static LcmCache CreateMemoryOnlyCache()
	{
		var projectId = new MemoryOnlyProjectId(BackendProviderType.kMemoryOnly, null);
		var ui = new HeadlessLcmUI();
		var directories = new NullLcmDirectories();
		return LcmCache.CreateCacheWithNewBlankLangProj(
			projectId,
			"en",
			"en",
			"en",
			ui,
			directories,
			new LcmSettings()
		);
	}

	private sealed class PreviewLifetime : IDisposable, ISavableWork
	{
		private readonly AdvancedEntryEditSession _session;
		private readonly LcmCache _cache;
		private int _disposed;

		public PreviewLifetime(AdvancedEntryEditSession session, LcmCache cache)
		{
			_session = session;
			_cache = cache;
		}

		public void Save() => _session.Save();

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) != 0)
				return;

			_session.Dispose();
			_cache.Dispose();
		}
	}

	private sealed class MemoryOnlyProjectId : IProjectIdentifier
	{
		public MemoryOnlyProjectId(BackendProviderType type, string? path)
		{
			Type = type;
			Path = path ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FwAdvancedEntry", "MemoryOnly");
		}

		public bool IsLocal => true;
		public string Path { get; set; }
		public string ProjectFolder => System.IO.Path.GetDirectoryName(Path) ?? System.IO.Path.GetTempPath();
		public string SharedProjectFolder => ProjectFolder;
		public string ServerName => string.Empty;
		public string Handle => Name;
		public string PipeHandle => Handle;
		public string Name => string.IsNullOrEmpty(Path)
			? "MemoryOnly"
			: (System.IO.Path.GetFileNameWithoutExtension(Path) ?? "MemoryOnly");
		public BackendProviderType Type { get; }
		public string UiName => Name;
	}

	private sealed class NullLcmDirectories : ILcmDirectories
	{
		public string ProjectsDirectory => System.IO.Path.GetTempPath();
		public string TemplateDirectory => System.IO.Path.GetTempPath();
	}

	private sealed class HeadlessLcmUI : ILcmUI
	{
		private readonly ISynchronizeInvoke _sync = new InlineSynchronizeInvoke();

		public ISynchronizeInvoke SynchronizeInvoke => _sync;

		public bool ConflictingSave() => true;
		public FileSelection ChooseFilesToUse() => FileSelection.Cancel;
		public bool RestoreLinkedFilesInProjectFolder() => false;
		public YesNoCancel CannotRestoreLinkedFilesToOriginalLocation() => YesNoCancel.Cancel;

		public void DisplayMessage(MessageType type, string message, string caption, string helpTopic) { }
		public void DisplayCircularRefBreakerReport(string report, string caption) { }
		public void ReportException(Exception error, bool isLethal) { }
		public DateTime LastActivityTime => DateTime.UtcNow;
		public void ReportDuplicateGuids(string errorText) { }
		public bool OfferToRestore(string projectPath, string backupPath) => false;
		public bool Retry(string msg, string caption) => false;
	}

	private sealed class InlineSynchronizeInvoke : ISynchronizeInvoke
	{
		public bool InvokeRequired => false;

		public IAsyncResult BeginInvoke(Delegate method, object?[]? args)
		{
			var result = Invoke(method, args);
			return new CompletedAsyncResult(result);
		}

		public object? EndInvoke(IAsyncResult result) =>
			result is CompletedAsyncResult completed
				? completed.Result
				: null;

		public object? Invoke(Delegate method, object?[]? args) => method.DynamicInvoke(args);

		private sealed class CompletedAsyncResult : IAsyncResult
		{
			private readonly System.Threading.ManualResetEvent _waitHandle = new(true);

			public CompletedAsyncResult(object? result)
			{
				Result = result;
			}

			public object? Result { get; }
			public object? AsyncState => Result;
			public WaitHandle AsyncWaitHandle => _waitHandle;
			public bool CompletedSynchronously => true;
			public bool IsCompleted => true;
		}
	}
}