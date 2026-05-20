using System;
using System.ComponentModel;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Presentation;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.PropertyGrid;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Services;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Tests;

[TestFixture]
public sealed class AdvancedEntryLcmPropertyObjectsTests
{
	[Test]
	public void Field_edit_mutates_LCModel_and_rollback_restores_previous_value()
	{
		using var cache = CreateMemoryOnlyCache();

		var wsVern = cache.DefaultVernWs;
		cache.ActionHandlerAccessor.BeginUndoTask("Setup", "Setup");
		var entry = cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
		entry.CitationForm.set_String(wsVern, TsStringUtils.MakeString("Before", wsVern));
		cache.ActionHandlerAccessor.EndUndoTask();
		cache.ActionHandlerAccessor.Commit();

		var schema = new PresentationNode[]
		{
			new PresentationField(new PresentationNodeId("CitationForm"))
			{
				Field = "CitationForm",
				Label = "CitationForm",
			},
		};

		using (var session = new AdvancedEntryEditSession(cache, "AdvancedEntry: Edit", "AdvancedEntry: Undo"))
		{
			var view = new LcmObjectView("LexEntry", cache, entry, schema);
			var pd = TypeDescriptor.GetProperties(view).Cast<PropertyDescriptor>().Single(p => p.DisplayName == "CitationForm");

			pd.SetValue(view, "After");
			Assert.That(entry.CitationForm.get_String(wsVern).Text, Is.EqualTo("After"));
			// No Save => rollback on dispose.
		}

		Assert.That(entry.CitationForm.get_String(wsVern).Text, Is.EqualTo("Before"));
	}

	[Test]
	public void Sequence_add_creates_LCModel_object_and_rollback_removes_it()
	{
		using var cache = CreateMemoryOnlyCache();

		cache.ActionHandlerAccessor.BeginUndoTask("Setup", "Setup");
		var entry = cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
		cache.ActionHandlerAccessor.EndUndoTask();
		cache.ActionHandlerAccessor.Commit();

		var schema = new PresentationNode[]
		{
			new PresentationSequence(new PresentationNodeId("Senses"))
			{
				Field = "Senses",
				Label = "Senses",
				ItemTemplate = new PresentationNode[]
				{
					new PresentationField(new PresentationNodeId("Gloss"))
					{
						Field = "Gloss",
						Label = "Gloss",
					},
				},
			},
		};

		using (var session = new AdvancedEntryEditSession(cache, "AdvancedEntry: Edit", "AdvancedEntry: Undo"))
		{
			var view = new LcmObjectView("LexEntry", cache, entry, schema);
			var pd = TypeDescriptor.GetProperties(view).Cast<PropertyDescriptor>().Single(p => p.DisplayName == "Senses");
			var list = (System.Collections.IList)pd.GetValue(view)!;

			Assert.That(entry.SensesOS.Count, Is.EqualTo(0));
			_ = list.Add(new LcmSequenceItem());
			Assert.That(entry.SensesOS.Count, Is.EqualTo(1));
			Assert.That(entry.SensesOS[0], Is.Not.Null);
			// No Save => rollback on dispose.
		}

		Assert.That(entry.SensesOS.Count, Is.EqualTo(0));
	}

	[Test]
	public void Sequence_remove_is_rollbackable()
	{
		using var cache = CreateMemoryOnlyCache();

		cache.ActionHandlerAccessor.BeginUndoTask("Setup", "Setup");
		var entry = cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
		entry.SensesOS.Add(cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create());
		cache.ActionHandlerAccessor.EndUndoTask();
		cache.ActionHandlerAccessor.Commit();

		var schema = new PresentationNode[]
		{
			new PresentationSequence(new PresentationNodeId("Senses"))
			{
				Field = "Senses",
				Label = "Senses",
				ItemTemplate = Array.Empty<PresentationNode>(),
			},
		};

		using (var session = new AdvancedEntryEditSession(cache, "AdvancedEntry: Edit", "AdvancedEntry: Undo"))
		{
			var view = new LcmObjectView("LexEntry", cache, entry, schema);
			var pd = TypeDescriptor.GetProperties(view).Cast<PropertyDescriptor>().Single(p => p.DisplayName == "Senses");
			var list = (System.Collections.IList)pd.GetValue(view)!;

			Assert.That(entry.SensesOS.Count, Is.EqualTo(1));
			list.RemoveAt(0);
			Assert.That(entry.SensesOS.Count, Is.EqualTo(0));
			// No Save => rollback on dispose.
		}

		Assert.That(entry.SensesOS.Count, Is.EqualTo(1));
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
			public System.Threading.WaitHandle AsyncWaitHandle => _waitHandle;
			public bool CompletedSynchronously => true;
			public bool IsCompleted => true;
		}
	}
}
