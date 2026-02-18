using System;
using NUnit.Framework;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Services;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Tests;

[TestFixture]
public sealed class AdvancedEntryCommitFenceTests
{
	[Test]
	public void Commit_is_blocked_while_session_is_active()
	{
		using var cache = CreateMemoryOnlyCache();
		SkipIfActionHandlerCannotBeSwapped(cache);

		using var session = new AdvancedEntryEditSession(
			cache,
			"AdvancedEntry: Edit",
			"AdvancedEntry: Undo"
		);

		var handler = cache.DomainDataByFlid.GetActionHandler();
		var ex = Assert.Throws<InvalidOperationException>(() => handler.Commit());
		Assert.That(ex!.Message, Does.Contain("Commit is blocked"));

		// Save should be allowed (bypasses the fence).
		session.Save();
	}

	[Test]
	public void EndUndoTask_is_blocked_when_it_would_end_outer_task()
	{
		using var cache = CreateMemoryOnlyCache();
		SkipIfActionHandlerCannotBeSwapped(cache);

		using var session = new AdvancedEntryEditSession(
			cache,
			"AdvancedEntry: Edit",
			"AdvancedEntry: Undo"
		);

		var handler = cache.DomainDataByFlid.GetActionHandler();
		var ex = Assert.Throws<InvalidOperationException>(() => handler.EndUndoTask());
		Assert.That(ex!.Message, Does.Contain("EndUndoTask is blocked"));

		session.Save();
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

	private static void SkipIfActionHandlerCannotBeSwapped(LcmCache cache)
	{
		try
		{
			var handler = cache.DomainDataByFlid.GetActionHandler();
			cache.DomainDataByFlid.SetActionHandler(handler);
		}
		catch (NotSupportedException)
		{
			Assert.Ignore("This cache implementation does not support swapping the action handler; commit-fence cannot be tested here.");
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
		private readonly System.ComponentModel.ISynchronizeInvoke _sync = new InlineSynchronizeInvoke();

		public System.ComponentModel.ISynchronizeInvoke SynchronizeInvoke => _sync;

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

	private sealed class InlineSynchronizeInvoke : System.ComponentModel.ISynchronizeInvoke
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
