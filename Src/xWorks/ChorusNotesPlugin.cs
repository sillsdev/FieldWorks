// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Chorus.notes;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;
using SIL.Progress;
using SIL.Reporting;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// winforms-free-lexeme-editor.md D2 / chorus-notes-contract.md — the UI-free core of the
	/// Avalonia Chorus notes bar: owns the LibChorus repository wiring the legacy
	/// <c>ChorusSystem.WinForms.CreateNotesBar</c> did (contract §3), and every read/write goes
	/// through the same files, ref-URL/key shapes, and canonical save lane legacy `MessageSlice`
	/// used — FLExBridge/S&amp;R and the legacy bar see ONE notes store. No UI types here; the
	/// Avalonia pixels live in <see cref="ChorusNotesBarControl"/>, bridged by
	/// <see cref="ChorusNotesEntryModel"/>. Pinned by ChorusNotesContractTests (contract §8).
	/// </summary>
	public sealed class ChorusNotesStore : IDisposable
	{
		/// <summary>The dummy "file being annotated" (FLExBridgeListener.FakeLexiconFileName, §1).</summary>
		public const string StubFileName = "Lexicon.fwstub";

		/// <summary>AnnotationRepository.FileExtension / FLExBridgeListener.kChorusNotesExtension (§1).</summary>
		public const string ChorusNotesExtension = "ChorusNotes";

		/// <summary>The primary notes file new notes land in (§1).</summary>
		public const string PrimaryNotesFileName = StubFileName + "." + ChorusNotesExtension;

		/// <summary>New FLEx notes are always class "question" (§5.1).</summary>
		public const string NewNoteClass = "question";

		/// <summary>The exact single line legacy MessageSlice writes into a missing stub (§1).</summary>
		public const string StubFileContent =
			"This is a stub file to provide an attachment point for " + PrimaryNotesFileName;

		private readonly AnnotationRepository _primary;
		private readonly List<AnnotationRepository> _additional = new List<AnnotationRepository>();
		private readonly MultiSourceAnnotationRepository _all;
		private readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
		private readonly object _watchGate = new object();
		private System.Threading.Timer _notesChangedDebounce;
		private bool _disposed;

		// Coalesces FileSystemWatcher bursts (one save raises several events) and gives the
		// repository's OWN reload of the same file (same event, its watcher thread) time to land
		// before consumers re-query. The legacy bar polled at 500 ms; half that keeps us snappier.
		private const int NotesChangedDebounceMilliseconds = 250;

		/// <summary>
		/// Opens (creating as needed, §1) the project's lexicon notes store: the primary repository
		/// on <c>Lexicon.fwstub.ChorusNotes</c> keyed by the <c>id</c> ref parameter, plus one
		/// <c>guid</c>-keyed repository per <c>Linguistics/Lexicon/*.lexdb</c> whose
		/// <c>.ChorusNotes</c> already exists (FLExBridge conflict notes), wrapped multi-source so
		/// new notes always go to the primary (§3).
		/// </summary>
		public ChorusNotesStore(string projectFolder)
		{
			if (string.IsNullOrEmpty(projectFolder))
				throw new ArgumentNullException(nameof(projectFolder));

			var progress = new NullProgress();
			EnsureStubFile(projectFolder);
			PrimaryNotesFilePath = Path.Combine(projectFolder, PrimaryNotesFileName);
			// The primary ref parameter is hard-coded "id" (§3.1); FromFile creates a missing file
			// containing exactly <notes version='0'/> (§1).
			_primary = AnnotationRepository.FromFile("id", PrimaryNotesFilePath, progress);
			foreach (var lexdbPath in GetAdditionalLexiconFilePaths(projectFolder))
			{
				// .lexdb chorus notes files identify the FLEx object with a url attr of "guid" (§3.2).
				_additional.Add(AnnotationRepository.FromFile("guid",
					lexdbPath + "." + ChorusNotesExtension, progress));
			}
			_all = new MultiSourceAnnotationRepository(_primary, _additional.Cast<IAnnotationRepository>());

			// External refresh (§6): watch each notes file OURSELVES instead of registering an
			// IAnnotationRepositoryObserver. The shipped LibChorus enumerates its observer list on
			// its FileSystemWatcher thread with no lock (AnnotationRepository.UnderlyingFileChanged
			// → _observers.ForEach), so Add/RemoveObserver from any other thread can race that
			// enumeration — and the resulting InvalidOperationException on an IO-completion thread
			// terminates the PROCESS. Owning the watcher leaves the repository's observer list
			// untouched after construction (only its internal key index, registered in its ctor,
			// remains) and gives this store a teardown it fully controls. Local writes raise
			// NotesChanged directly from the mutating methods instead.
			WatchNotesFile(_primary.AnnotationFilePath);
			foreach (var repository in _additional)
				WatchNotesFile(repository.AnnotationFilePath);
		}

		private void WatchNotesFile(string path)
		{
			var watcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path))
			{
				NotifyFilter = NotifyFilters.LastWrite
			};
			watcher.Changed += OnNotesFileChanged;
			watcher.Created += OnNotesFileChanged;
			watcher.EnableRaisingEvents = true;
			_watchers.Add(watcher);
		}

		// Runs on the watcher's IO-completion thread: an escaping exception there terminates the
		// process, so everything is guarded; the debounce timer turns an event burst into one raise.
		private void OnNotesFileChanged(object sender, FileSystemEventArgs e)
		{
			try
			{
				lock (_watchGate)
				{
					if (_disposed)
						return;
					if (_notesChangedDebounce == null)
					{
						_notesChangedDebounce = new System.Threading.Timer(
							_ => RaiseNotesChanged(), null,
							NotesChangedDebounceMilliseconds, System.Threading.Timeout.Infinite);
					}
					else
					{
						_notesChangedDebounce.Change(
							NotesChangedDebounceMilliseconds, System.Threading.Timeout.Infinite);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.WriteError("ChorusNotesStore: notes file watcher failed.", ex);
			}
		}

		/// <summary>Full path of <c>Lexicon.fwstub.ChorusNotes</c>.</summary>
		public string PrimaryNotesFilePath { get; }

		/// <summary>
		/// The backing files changed (addition, modification, deletion, or a stale list after an
		/// external write such as Send/Receive). Raised on whatever thread the repository observer
		/// fires on — UI consumers marshal (§6).
		/// </summary>
		public event EventHandler NotesChanged;

		// Raised from the debounce timer's pool thread or a mutating method's calling thread; an
		// exception escaping a pool thread terminates the process, so consumer failures are logged.
		private void RaiseNotesChanged()
		{
			if (_disposed)
				return;
			try
			{
				NotesChanged?.Invoke(this, EventArgs.Empty);
			}
			catch (Exception ex)
			{
				Logger.WriteError("ChorusNotesStore: a NotesChanged consumer failed.", ex);
			}
		}

		/// <summary>
		/// The silfw link new FLEx lexicon notes carry (contract §4, verbatim template from
		/// MessageSlice.cs:124-130): the entry guid appears twice — <c>guid=</c> drives FLEx
		/// jump-navigation, <c>id=</c> is what the primary index matches — and the label
		/// (ShortName/headword) is the LAST parameter.
		/// </summary>
		public static string BuildRefUrl(string entryGuid, string label)
		{
			return string.Format(
				"silfw://localhost/link?app=flex&database=current&server=&tool=default&guid={0}&tag=&id={0}&label={1}",
				entryGuid, label);
		}

		/// <summary>
		/// The annotations to show for a target: for each key (the entry's lowercase guid plus its
		/// AllOwnedObjects guids — notes attached to senses/allomorphs show on the entry's bar),
		/// the matches across primary and lexdb repositories, concatenated. Open AND closed — the
		/// legacy bar never filtered (§3.4/§3.5).
		/// </summary>
		public IReadOnlyList<Annotation> GetAnnotationsFor(string targetId, IEnumerable<string> additionalIds = null)
		{
			var keys = new List<string>();
			if (!string.IsNullOrEmpty(targetId))
				keys.Add(targetId);
			if (additionalIds != null)
				keys.AddRange(additionalIds.Where(id => !string.IsNullOrEmpty(id)));
			return keys.SelectMany(key => _all.GetMatchesByPrimaryRefKey(key)).ToList();
		}

		/// <summary>"ifdata" visibility parity: any annotation for the key in the PRIMARY file only (§6).</summary>
		public bool HasPrimaryNotes(string targetId)
		{
			return !string.IsNullOrEmpty(targetId) && _primary.GetMatchesByPrimaryRefKey(targetId).Any();
		}

		/// <summary>
		/// Writes a new note (§5): class "question", the §4 silfw ref, first message authored by
		/// <see cref="Environment.UserName"/> (SendReceiveUser) with status "" — added to the
		/// PRIMARY repository and flushed immediately. A blank text is discarded: returns null,
		/// nothing written (the legacy dialog's cancel/empty path).
		/// </summary>
		public Annotation AddNote(string entryGuid, string label, string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return null;
			// The path placeholder is irrelevant — the repository assigns it on add (§5.1).
			var annotation = new Annotation(NewNoteClass, BuildRefUrl(entryGuid, label), "doesntmakesense");
			annotation.AddMessage(Environment.UserName, null, text); // null status inherits "" (§5.4)
			_all.AddAnnotation(annotation); // multi-source routes new notes to the primary (§3.3)
			_primary.SaveNowIfNeeded(new NullProgress()); // immediate flush (§5.2)
			RaiseNotesChanged(); // local writes notify directly (no repository observer; see ctor)
			return annotation;
		}

		/// <summary>Appends a message (current user, status inherited) and saves. False when blank.</summary>
		public bool AppendMessage(Annotation annotation, string text)
		{
			if (annotation == null || string.IsNullOrWhiteSpace(text))
				return false;
			annotation.AddMessage(Environment.UserName, null, text);
			SaveOwnerOf(annotation);
			RaiseNotesChanged();
			return true;
		}

		/// <summary>
		/// Toggles resolved (§5.4): SetStatus appends an EMPTY message carrying "closed"/"open";
		/// IsClosed derives from the last message. Refused (false, nothing changes) when
		/// <see cref="Annotation.CanResolve"/> is false — we defer to the SHIPPED library's
		/// exclusion list ("note"/"conflict" in 6.0.0-beta; see contract §5.7), exactly like the
		/// legacy bar built on the same assembly.
		/// </summary>
		public bool ToggleResolved(Annotation annotation)
		{
			if (annotation == null || !annotation.CanResolve)
				return false;
			annotation.SetStatus(Environment.UserName,
				annotation.IsClosed ? Annotation.Open : Annotation.Closed);
			SaveOwnerOf(annotation);
			RaiseNotesChanged();
			return true;
		}

		// Always write through the repository's canonical save lane (under the saving mutex), never
		// the file directly (§4). Save unconditionally: an in-place message append must reach disk
		// even if this LibChorus build's dirty flag misses it.
		private void SaveOwnerOf(Annotation annotation)
		{
			var progress = new NullProgress();
			if (_primary.ContainsAnnotation(annotation))
			{
				_primary.Save(progress);
				return;
			}
			var owner = _additional.FirstOrDefault(r => r.ContainsAnnotation(annotation));
			owner?.Save(progress);
		}

		// Stub creation, replicated from MessageSlice.cs:86-98 (§1): UTF-8 (BOM) with the single
		// content line; deliberately NOT sent/received — only the .ChorusNotes is. Never rewritten
		// when present.
		private static void EnsureStubFile(string projectFolder)
		{
			var stubPath = Path.Combine(projectFolder, StubFileName);
			if (File.Exists(stubPath))
				return;
			using (var writer = new StreamWriter(stubPath, false, Encoding.UTF8))
				writer.WriteLine(StubFileContent);
		}

		// Mirrors MessageSlice.GetAdditionalLexiconFilePaths (§1): every Linguistics/Lexicon/*.lexdb
		// whose .ChorusNotes already exists (the existence check doubles as the legacy perf guard).
		private static IEnumerable<string> GetAdditionalLexiconFilePaths(string projectFolder)
		{
			var lexiconFolder = Path.Combine(projectFolder, "Linguistics", "Lexicon");
			if (!Directory.Exists(lexiconFolder))
				yield break;
			foreach (var path in Directory.EnumerateFiles(lexiconFolder, "*.lexdb"))
			{
				if (File.Exists(path + "." + ChorusNotesExtension))
					yield return path;
			}
		}

		/// <summary>
		/// Dispose order per §6: stop OUR watchers and debounce first (under the gate, so an
		/// in-flight watcher callback observes _disposed), then dispose the repositories — each
		/// repository's Dispose performs the final SaveNowIfNeeded.
		/// </summary>
		public void Dispose()
		{
			lock (_watchGate)
			{
				if (_disposed)
					return;
				_disposed = true;
				foreach (var watcher in _watchers)
				{
					watcher.EnableRaisingEvents = false;
					watcher.Changed -= OnNotesFileChanged;
					watcher.Created -= OnNotesFileChanged;
					watcher.Dispose();
				}
				_watchers.Clear();
				_notesChangedDebounce?.Dispose();
				_notesChangedDebounce = null;
			}
			_primary.Dispose();
			foreach (var repository in _additional)
				repository.Dispose();
		}
	}

	/// <summary>
	/// Projects <see cref="ChorusNotesStore"/> for ONE entry as the UI-free model the Avalonia bar
	/// renders (<see cref="IChorusNotesBarModel"/>): keys are lowercase guids — the entry's own and
	/// every AllOwnedObjects guid (§3.4) — the new-note label is the entry's ShortName (§4), and
	/// the fonts/keyboard follow FWNX-1239 (§7: labels in the default vernacular font, messages in
	/// the default analysis font at size 12, analysis keyboard on message-editor focus).
	/// </summary>
	public sealed class ChorusNotesEntryModel : IChorusNotesBarModel
	{
		private readonly ChorusNotesStore _store;
		private readonly ICmObject _target;
		private readonly LcmCache _cache;
		private readonly string _analysisWsTag;

		public ChorusNotesEntryModel(ChorusNotesStore store, ICmObject target, LcmCache cache)
		{
			_store = store ?? throw new ArgumentNullException(nameof(store));
			_target = target ?? throw new ArgumentNullException(nameof(target));
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));

			var writingSystems = cache.ServiceLocator.WritingSystems;
			var vernacular = writingSystems.DefaultVernacularWritingSystem;
			var analysis = writingSystems.DefaultAnalysisWritingSystem;
			LabelFontFamily = vernacular?.DefaultFontName;
			MessageFontFamily = analysis?.DefaultFontName;
			_analysisWsTag = analysis?.Id;
		}

		/// <summary>Target id = lowercase entry guid (MessageSlice.GetIdForObject, §3.4).</summary>
		public static string GetIdForObject(ICmObject obj)
		{
			return obj.Guid.ToString().ToLowerInvariant();
		}

		/// <summary>
		/// AllOwnedObjects guids, lowercased (MessageSlice.GetAdditionalIdsForObject, §3.4) — notes
		/// attached to senses/allomorphs of the entry show on the entry's bar.
		/// </summary>
		public static IEnumerable<string> GetAdditionalIdsForObject(ICmObject obj)
		{
			return obj.AllOwnedObjects.Select(t => t.Guid.ToString().ToLowerInvariant());
		}

		/// <summary>New-note label = ShortName (the headword), the silfw ref's last parameter (§4).</summary>
		public static string GetLabelForObject(ICmObject obj)
		{
			return obj.ShortName;
		}

		public IReadOnlyList<IChorusNoteItem> GetNotes()
		{
			return _store.GetAnnotationsFor(GetIdForObject(_target), GetAdditionalIdsForObject(_target))
				.Select(annotation => (IChorusNoteItem)new NoteItem(_store, annotation))
				.ToList();
		}

		public bool AddNote(string text)
		{
			return _store.AddNote(GetIdForObject(_target), GetLabelForObject(_target), text) != null;
		}

		// §7 (FWNX-1239): label/headword rendering uses the default vernacular font, message
		// display/entry the default analysis font; legacy pinned both at size 12.
		public string LabelFontFamily { get; }

		public double LabelFontSize => 12;

		public string MessageFontFamily { get; }

		public double MessageFontSize => 12;

		public void MessageEditorFocused()
		{
			// §7c: switch the keyboard to the analysis WS — the same lane the region's own editors
			// use for per-WS keyboards.
			if (!string.IsNullOrEmpty(_analysisWsTag))
				LexicalEditRegionBuilder.ActivateKeyboardForWritingSystem(_cache, _analysisWsTag);
		}

		public event EventHandler NotesChanged
		{
			add => _store.NotesChanged += value;
			remove => _store.NotesChanged -= value;
		}

		private sealed class NoteItem : IChorusNoteItem
		{
			private readonly ChorusNotesStore _store;
			private readonly Annotation _annotation;

			public NoteItem(ChorusNotesStore store, Annotation annotation)
			{
				_store = store;
				_annotation = annotation;
			}

			public string ClassName => _annotation.ClassName;

			public string Label => _annotation.GetLabelFromRef(string.Empty);

			public bool IsClosed => _annotation.IsClosed;

			public bool CanResolve => _annotation.CanResolve;

			// §5.6: class + ": " + label of the thing annotated, then the message texts.
			public string Tooltip
			{
				get
				{
					var builder = new StringBuilder();
					builder.Append(_annotation.ClassName).Append(": ").Append(_annotation.LabelOfThingAnnotated);
					foreach (var message in _annotation.Messages)
					{
						if (!string.IsNullOrEmpty(message.Text))
							builder.AppendLine().Append(message.Text);
					}
					return builder.ToString();
				}
			}

			public IReadOnlyList<ChorusNoteMessage> Messages
			{
				get
				{
					return _annotation.Messages
						.Select(m => new ChorusNoteMessage(m.Author, m.Date, m.Status, m.Text))
						.ToList();
				}
			}

			public bool AppendMessage(string text) => _store.AppendMessage(_annotation, text);

			public bool ToggleResolved() => _store.ToggleResolved(_annotation);
		}
	}

	/// <summary>
	/// winforms-free-lexeme-editor.md D2 — the native Avalonia Messages row: claims the legacy
	/// <c>SIL.FieldWorks.XWorks.LexEd.MessageSlice</c> layout identity through the D1 plugin
	/// contract and renders <see cref="ChorusNotesBarControl"/> over LibChorus at the slice's real
	/// in-tree position, retiring the WinForms companion strip's only designated class. The fenced
	/// edit context is not used: Chorus notes live in .ChorusNotes files, not LCModel — there is
	/// nothing to stage or undo (legacy MessageSlice wrote through immediately too).
	/// </summary>
	public sealed class ChorusNotesPlugin : IRegionEditorPlugin
	{
		public string LegacyClassName => AvaloniaCompanionSlices.MessageSliceClassName;

		public Control BuildControl(RegionEditorBuildContext context)
		{
			var obj = context?.Target;
			var cache = context?.Cache;
			if (obj == null || cache == null)
				return null;
			ChorusNotesStore store = null;
			try
			{
				store = new ChorusNotesStore(cache.ProjectId.ProjectFolder);
				var model = new ChorusNotesEntryModel(store, obj, cache);
				// The control owns the store: disposing on detach performs the final save (§6).
				return new ChorusNotesBarControl(model, store);
			}
			catch (Exception e)
			{
				// Graceful degradation, same policy as the companion lane: without a usable notes
				// store the row degrades to the explicit unsupported row (the view's guard lane for
				// a null factory result) — never take the pane down.
				Logger.WriteEvent($"ChorusNotesPlugin: notes bar unavailable for '{obj.Guid}': {e}");
				store?.Dispose();
				return null;
			}
		}
	}
}
