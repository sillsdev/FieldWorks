using System;
using System.ComponentModel;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Services;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Tests;

[TestFixture]
public sealed class AdvancedEntryEditSessionTests
{
	[Test]
	public void Dispose_without_Save_rolls_back_changes()
	{
		using var cache = CreateMemoryOnlyCache();
		Assert.That(CountLexEntries(cache), Is.EqualTo(0));

		using (var session = new AdvancedEntryEditSession(
			cache,
			"AdvancedEntry: Edit",
			"AdvancedEntry: Undo"
		))
		{
			_ = cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			Assert.That(CountLexEntries(cache), Is.EqualTo(1));
			// No Save => Dispose should roll back.
		}

		Assert.That(CountLexEntries(cache), Is.EqualTo(0));
	}

	[Test]
	public void Save_keeps_changes()
	{
		using var cache = CreateMemoryOnlyCache();
		Assert.That(CountLexEntries(cache), Is.EqualTo(0));

		using (var session = new AdvancedEntryEditSession(
			cache,
			"AdvancedEntry: Edit",
			"AdvancedEntry: Undo"
		))
		{
			_ = cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			Assert.That(CountLexEntries(cache), Is.EqualTo(1));

			session.Save();
		}

		Assert.That(CountLexEntries(cache), Is.EqualTo(1));
	}

	private static int CountLexEntries(LcmCache cache) => cache.ServiceLocator
		.GetInstance<ILexEntryRepository>()
		.AllInstances()
		.Count();

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

		public void DisplayMessage(
			MessageType type,
			string message,
			string caption,
			string helpTopic
		) { }
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
			public System.Threading.WaitHandle AsyncWaitHandle => _waitHandle;
			public bool CompletedSynchronously => true;
			public bool IsCompleted => true;
		}
	}
}
