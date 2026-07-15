# D2 Contract: Avalonia Chorus Notes Bar (replaces WinForms `NotesBarView`)

Verified against `Src/LexText/Lexicon/MessageSlice.cs`, `Src/LexText/Lexicon/FLExBridgeListener.cs`,
the `SIL.Chorus.LibChorus` 6.0.0-beta0063 netstandard2.0 assembly (reflection dump; repo pins
6.0.0-beta0065 in `Build/SilVersions.props:18`), and Chorus master sources on GitHub. This is the
compatibility contract `ChorusNotesPlugin` and its tests implement — see
`winforms-free-lexeme-editor.md` decision D2.

## 1. Files and paths (must match exactly)

| Item | Value | Source |
|---|---|---|
| Stub file ("file being annotated") | `<ProjectFolder>\Lexicon.fwstub` | `FLExBridgeListener.cs:1062` (`FakeLexiconFileName`) |
| Primary notes file | `<ProjectFolder>\Lexicon.fwstub.ChorusNotes` | `FLExBridgeListener.cs:1066` |
| Notes extension constant | `"ChorusNotes"` (no leading dot) | `FLExBridgeListener.cs:1190`; same as `AnnotationRepository.FileExtension` |
| Additional annotated files | every `<ProjectFolder>\Linguistics\Lexicon\*.lexdb` whose `<file>.ChorusNotes` already exists | `MessageSlice.cs:109-122` |
| LIFT notes file (FLExBridge-converted; not opened by the bar) | `<liftPath>.ChorusNotes` | `FLExBridgeListener.cs:1071-1074` |

**Stub creation (must replicate):** if `Lexicon.fwstub` is missing, create it UTF-8 with the single
line `"This is a stub file to provide an attachment point for Lexicon.fwstub.ChorusNotes"`
(`MessageSlice.cs:86-98`). The stub is deliberately NOT sent/received; only `.ChorusNotes` is.

**Notes file creation:** `AnnotationRepository.FromFile` creates a missing file containing exactly
`<notes version='0'/>`. Malformed XML throws `AnnotationFormatException` (legacy lets it propagate).

## 2. LibChorus API surface (namespace `Chorus.notes`, LibChorus 6.0.0-beta0063)

```csharp
// AnnotationRepository : IAnnotationRepository, IDisposable
static AnnotationRepository FromFile(string primaryRefParameter, string path, IProgress progress); // SIL.Progress.IProgress
static AnnotationRepository FromString(string primaryRefParameter, string contents);
static IEnumerable<AnnotationRepository> CreateRepositoriesFromFolder(string folderPath, IProgress progress);
static string FileExtension; // "ChorusNotes"
string AnnotationFilePath { get; }
IAnnotationMutex SavingMutex { get; set; }
void AddAnnotation(Annotation annotation);   // appends + notifies observers + sets dirty
void Remove(Annotation annotation);
bool ContainsAnnotation(Annotation annotation);
IEnumerable<Annotation> GetAllAnnotations();
IEnumerable<Annotation> GetByCurrentStatus(string status);
IEnumerable<Annotation> GetMatches(Func<Annotation,bool> predicate);
IEnumerable<Annotation> GetMatchesByPrimaryRefKey(string key);
void AddObserver(IAnnotationRepositoryObserver observer, IProgress progress);
void RemoveObserver(IAnnotationRepositoryObserver observer);
void Save(IProgress progress);               // canonical XML, under SavingMutex
void SaveNowIfNeeded(IProgress progress);    // saves only if dirty
void Dispose();                              // unhooks watcher, then SaveNowIfNeeded(new NullProgress())

// Annotation
Annotation(XElement element);
Annotation(string annotationClass, string refUrl, string path);            // new Guid
Annotation(string annotationClass, string refUrl, Guid guid, string path); // refUrl run through UrlHelper.GetEscapedUrl
string ClassName { get; }        // "question" | "note" | "mergeConflict" | "notification"
string Guid { get; }
string RefStillEscaped { get; }  // raw "ref" attr (indexing uses this)
string RefUnEscaped { get; }
IEnumerable<Message> Messages { get; }
Message AddMessage(string author, string status, string contents); // status==null => inherits current Status
string Status { get; }           // status attr of LAST message; "" if none
bool IsClosed { get; }           // Status.ToLower() == "closed"
void SetStatus(string author, string status);  // appends EMPTY message carrying the new status
void SetStatusToClosed(string userName);
bool CanResolve { get; }         // false for conflict/notification classes
string GetLabelFromRef(string defaultIfCannotGetIt); // "label" query param
string LabelOfThingAnnotated { get; }
DateTime Date { get; }           // date of last message
static readonly string Open = "open"; static readonly string Closed = "closed";
static string TimeFormatWithTimeZone = "yyyy-MM-ddTHH:mm:ssK";

// Message
Message(string author, string status, string contents); // date=now(TimeFormatWithTimeZone), guid=new
string Author { get; } string Status { get; } string Text { get; }
DateTime Date { get; } string Guid { get; } XElement Element { get; }

// Index / observer / multi-source
class IndexOfAllAnnotationsByKey : AnnotationIndex { IndexOfAllAnnotationsByKey(string nameOfParameterInRefToIndex); }
interface IAnnotationRepositoryObserver {
    void Initialize(Func<IEnumerable<Annotation>> allAnnotationsFunction, IProgress progress);
    void NotifyOfAddition(Annotation a); void NotifyOfModification(Annotation a);
    void NotifyOfDeletion(Annotation a); void NotifyOfStaleList();
}
class MultiSourceAnnotationRepository : IAnnotationRepository {
    MultiSourceAnnotationRepository(IAnnotationRepository primary, IEnumerable<IAnnotationRepository> others);
    // GetMatchesByPrimaryRefKey = primary ∪ others; AddAnnotation → primary only; Remove → whichever contains it
}
```

GUI-only types to re-implement in Avalonia (they live in WinForms `Chorus.exe`):
`NotesBarView`, `NotesBarModel`, `NoteDetailDialog`. `NotesToRecordMapping` is also Chorus.exe —
copy its three-delegate shape (`FunctionToGetCurrentUrlForNewNotes(object, string escapedId)`,
`FunctionToGoFromObjectToItsId(object)`, `FunctionToGoFromObjectToAdditionalIds(object)`).

## 3. Repository wiring (what `ChorusSystem.WinForms.CreateNotesBar` did)

From `MessageSlice.FinishInit` (`MessageSlice.cs:52-77`):

1. Primary repo: `AnnotationRepository.FromFile("id", projectFolder + @"\Lexicon.fwstub.ChorusNotes", progress)` —
   **primary ref parameter is hard-coded `"id"`**.
2. Each additional repo: `AnnotationRepository.FromFile("guid", lexdbPath + ".ChorusNotes", progress)` —
   `idAttrForOtherFiles = "guid"`.
3. Wrap in `MultiSourceAnnotationRepository(primary, others)`. New notes always go to the primary.
4. Key matching is **case-sensitive raw string compare** on that query parameter in the
   still-escaped `ref` — which is why FLEx supplies **lowercase** guids:
   - target id: `((ICmObject)target).Guid.ToString().ToLowerInvariant()` (`MessageSlice.cs:132-135`)
   - additional ids: `m_obj.AllOwnedObjects` guids, lowercased (`MessageSlice.cs:137-140`) — notes
     attached to senses/allomorphs of the entry show on the entry's bar.
5. Annotations to show = for each target key, `GetMatchesByPrimaryRefKey(key)`, concatenated.
   Legacy shows open AND closed notes (no filtering).

## 4. Ref URL format (compatibility-critical)

New FLEx lexicon notes carry this `ref` (before XML escaping), built at `MessageSlice.cs:124-130`:

```
silfw://localhost/link?app=flex&database=current&server=&tool=default&guid={guid}&tag=&id={guid}&label={entry.ShortName}
```

- `{guid}` = entry guid, lowercase, appears twice (`guid=` drives FLEx jump-navigation; `id=` is
  what the primary index matches).
- `label` = `ICmObject.ShortName` (headword); last param; may contain spaces/quotes.
- The `Annotation` ctor applies `UrlHelper.GetEscapedUrl` to the whole URL (`&`→`&amp;`, `"`→`&quot;`,
  `'`→`&apos;`, `<`→`&lt;`, `>`→`&gt;`), and `RefStillEscaped` (used for indexing) keeps that form.
- LIFT-side refs (`lift://{file}?type=entry&label={label}&id={guid}`) are rewritten to the silfw
  form by `FLExBridgeListener.ConvertLiftNotesToFlex` (`FLExBridgeListener.cs:1283-1291`) before the
  bar sees them.

### Canonical example annotation XML (verbatim from `LexEdDllTests/FlexBridgeListenerTests.cs:22-31`)

```xml
<annotation
    class="question"
    ref="silfw://localhost/link?app=flex&amp;database=current&amp;server=&amp;tool=default&amp;guid=6b466f54-f88a-42f6-b770-aca8fee5734c&amp;tag=&amp;id=6b466f54-f88a-42f6-b770-aca8fee5734c&amp;label=bother"
    guid="10fa26c9-ce35-4341-8a30-c1aa1250d0e0">
    <message
        author="WhoAmI?"
        status=""
        date="2013-01-28T08:51:40Z"
        guid="25f47900-f6f6-4288-89ac-44f738e63431">Is this the strongest expression of annoyance?</message>
</annotation>
```

Root document: `<notes version="0">…</notes>`. Always write through
`AnnotationRepository.Save/SaveNowIfNeeded` (canonical XML under the saving mutex) — never write
the file directly.

## 5. "Add a new note" behavior

1. `new Annotation("question", url, "doesntmakesense")` — **class is always `"question"`**; the
   path placeholder is irrelevant (repository assigns it on add); url per §4 with the escaped id.
2. Note detail UI: only if the user confirms AND `annotation.Messages.Any()` →
   `repository.AddAnnotation(annotation)` then `SaveNowIfNeeded(new NullProgress())` (immediate
   flush). Cancel/empty ⇒ discarded, nothing written.
3. First message author = `Environment.UserName` (`FLExBridgeListener.SendReceiveUser`,
   `FLExBridgeListener.cs:866-869`; threaded via `MessageSlice.cs:47-55`).
4. First message status = `""` (AddMessage with `null` status inherits empty). Resolve toggles call
   `SetStatus(user, "closed"/"open")`, which appends an EMPTY message carrying the status;
   `IsClosed` derives from the last message.
5. **FLExBridge displays it with no extra registration** — it enumerates `*.ChorusNotes` in the
   repo; the silfw ref is what lets it jump FLEx back to the entry. The note syncs because
   `Lexicon.fwstub.ChorusNotes` is in the Hg repo (the `.fwstub` itself is excluded).
6. Existing-note affordances: icon by class + open/closed state; tooltip starts
   `ClassName + ": " + LabelOfThingAnnotated` then message texts; click reopens detail, then
   `SaveNowIfNeeded`.
7. Resolvability: defer to `Annotation.CanResolve`. **Shipped-library correction (verified
   empirically against the pinned 6.0.0-beta assembly, 2026-06-11):** `CanResolve` excludes
   only the lowercase classes `"conflict"` and `"note"` — so `mergeConflict`/`notification`
   ARE resolvable in this version, matching the legacy WinForms bar built on the same
   assembly. (Chorus master docs differ; the contract pins shipped behavior, and the
   compatibility test will flag any change on a Chorus upgrade.)

## 6. Refresh semantics

- Observe the repository (`IAnnotationRepositoryObserver`); `AnnotationRepository` owns a
  `FileSystemWatcher` on its file and raises `NotifyOfStaleList` on external change (e.g. after
  S/R) — external refresh is free if the bar observes. Legacy polls a reload flag on a 500 ms
  timer; the Avalonia bar can refresh on the notification directly (UI-thread dispatch).
- `ShowSliceForVisibleIfData` ("ifdata" visibility) = any annotation matches the entry guid in the
  PRIMARY file only (`MessageSlice.cs:155-172`).
- Dispose order: dispose repositories on teardown; `Dispose()` performs a final `SaveNowIfNeeded`.

## 7. Writing-system / font behaviors (FWNX-1239; `MessageSlice.cs:69-75`)

- Label WS = **default vernacular**: language name, WS id, `DefaultFontName`, size 12 — note
  label/headword rendering.
- Message WS = **default analysis**: same shape — message text entry/display.
- `Chorus.IWritingSystem` contract: `Name`, `Code` (WS id; used to activate the matching keyboard),
  `FontName`, `FontSize`, `ActivateKeyboard()`. The Avalonia bar must (a) render labels in the
  vernacular font, (b) render/edit messages in the analysis font, (c) switch the keyboard to the
  analysis WS when the message editor gains focus.

## 8. Compatibility tests must assert

1. Round-trip: the §4 example XML is readable and matched for entry guid
   `6b466f54-f88a-42f6-b770-aca8fee5734c` (case-sensitive lowercase key off `id=` of the
   still-escaped ref).
2. A new note written by the Avalonia bar, read back via a fresh
   `AnnotationRepository.FromFile("id", path, progress)`: class `question`, ref matches the silfw
   template (same guid in `guid=` and `id=`), label = ShortName, one message with author =
   `Environment.UserName`, `status=""`, date in `yyyy-MM-ddTHH:mm:ssK`, root `<notes version="0">`.
3. Stub-file creation idempotence and exact content (§1).
4. Notes in `Linguistics/Lexicon/*.lexdb.ChorusNotes` keyed by `guid=` appear; new notes never land
   there.
5. Resolve toggles append empty status messages; `IsClosed` flips; resolvability matches the
   shipped `Annotation.CanResolve` (see §5.7: `note`/`conflict` excluded, `mergeConflict`
   resolvable in 6.0.0-beta).
