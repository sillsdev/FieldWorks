// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Chorus.notes;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using SIL.Progress;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// chorus-notes-contract.md §8 — the five mandatory compatibility assertions for the Avalonia
	/// Chorus notes bar's UI-free core (<see cref="ChorusNotesStore"/>): the plugin reads and
	/// writes the SAME files with the SAME ref-URL/key shapes legacy `MessageSlice` wrote, so
	/// FLExBridge/S&amp;R and the legacy bar see one notes store. Everything here runs against a
	/// temp project folder and real LibChorus repositories — no LCModel needed (the ShortName /
	/// AllOwnedObjects key derivation rides the memory cache in
	/// <see cref="ChorusNotesEntryKeyTests"/> below).
	/// </summary>
	[TestFixture]
	public class ChorusNotesContractTests
	{
		private string m_folder;
		private ChorusNotesStore m_store;

		/// <summary>The entry guid the §4 canonical example annotates (lowercase, like FLEx supplies).</summary>
		private const string EntryGuid = "6b466f54-f88a-42f6-b770-aca8fee5734c";

		// Verbatim from chorus-notes-contract.md §4 (canonical example annotation XML, itself
		// verbatim from LexEdDllTests/FlexBridgeListenerTests.cs:22-31).
		private const string LegacyAnnotationXml = @"<annotation
    class=""question""
    ref=""silfw://localhost/link?app=flex&amp;database=current&amp;server=&amp;tool=default&amp;guid=6b466f54-f88a-42f6-b770-aca8fee5734c&amp;tag=&amp;id=6b466f54-f88a-42f6-b770-aca8fee5734c&amp;label=bother""
    guid=""10fa26c9-ce35-4341-8a30-c1aa1250d0e0"">
    <message
        author=""WhoAmI?""
        status=""""
        date=""2013-01-28T08:51:40Z""
        guid=""25f47900-f6f6-4288-89ac-44f738e63431"">Is this the strongest expression of annoyance?</message>
</annotation>";

		// A FLExBridge-style merge-conflict annotation as found in Linguistics/Lexicon/*.lexdb.ChorusNotes:
		// keyed by the `guid=` query parameter (no `id=`), class mergeConflict (not resolvable, §5.7).
		private const string LexdbConflictAnnotationXml = @"<annotation
    class=""mergeConflict""
    ref=""silfw://localhost/link?app=flex&amp;database=current&amp;server=&amp;tool=default&amp;guid=6b466f54-f88a-42f6-b770-aca8fee5734c&amp;tag=&amp;label=bother""
    guid=""3c44ff62-5781-4d2c-befb-a6233ee0c596"">
    <message
        author=""merger""
        status=""open""
        date=""2013-01-28T08:51:40Z""
        guid=""b3d3e2c1-8e0e-4c50-93c2-966e26de9bd6"">Both users edited the same field.</message>
</annotation>";

		[SetUp]
		public void Setup()
		{
			m_folder = Path.Combine(Path.GetTempPath(), "ChorusNotesContractTests_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(m_folder);
		}

		[TearDown]
		public void Teardown()
		{
			m_store?.Dispose();
			m_store = null;
			try
			{
				Directory.Delete(m_folder, true);
			}
			catch (IOException)
			{
				// A FileSystemWatcher handle can outlive Dispose by a beat on Windows; the temp
				// folder is uniquely named, so leaking it cannot affect another run.
			}
		}

		private string PrimaryNotesPath => Path.Combine(m_folder, ChorusNotesStore.PrimaryNotesFileName);

		private void WritePrimaryNotesFile(string annotationsXml)
		{
			File.WriteAllText(PrimaryNotesPath, "<notes version='0'>" + annotationsXml + "</notes>");
		}

		private string CreateLexdbNotesFile(string annotationsXml, string baseName = "Lexicon_04")
		{
			var lexiconFolder = Path.Combine(m_folder, "Linguistics", "Lexicon");
			Directory.CreateDirectory(lexiconFolder);
			var lexdbPath = Path.Combine(lexiconFolder, baseName + ".lexdb");
			File.WriteAllText(lexdbPath, ""); // the data file the notes are "about"
			var notesPath = lexdbPath + "." + ChorusNotesStore.ChorusNotesExtension;
			File.WriteAllText(notesPath, "<notes version='0'>" + annotationsXml + "</notes>");
			return notesPath;
		}

		/// <summary>§8.1 — the legacy-format annotation file round-trips through the plugin's core.</summary>
		[Test]
		public void RoundTrip_LegacyAnnotation_IsMatchedForTheEntryGuid_CaseSensitively()
		{
			WritePrimaryNotesFile(LegacyAnnotationXml);
			m_store = new ChorusNotesStore(m_folder);

			var matches = m_store.GetAnnotationsFor(EntryGuid);
			Assert.That(matches.Count, Is.EqualTo(1), "the §4 example must be matched off `id=` of the still-escaped ref");
			Assert.That(matches[0].ClassName, Is.EqualTo("question"));
			Assert.That(matches[0].Guid, Is.EqualTo("10fa26c9-ce35-4341-8a30-c1aa1250d0e0"));
			Assert.That(matches[0].Messages.Single().Text,
				Is.EqualTo("Is this the strongest expression of annoyance?"));

			Assert.That(m_store.GetAnnotationsFor(EntryGuid.ToUpperInvariant()), Is.Empty,
				"key matching is a case-sensitive raw string compare — FLEx supplies lowercase guids (§3.4)");
			Assert.That(m_store.HasPrimaryNotes(EntryGuid), Is.True,
				"ifdata visibility looks at the PRIMARY file only (§6)");
		}

		/// <summary>§8.2 — a note written by the Avalonia bar reads back through a fresh legacy repository.</summary>
		[Test]
		public void NewNote_ReadBackThroughAFreshRepository_HasTheLegacyShape()
		{
			m_store = new ChorusNotesStore(m_folder);
			Assert.That(m_store.AddNote(EntryGuid, "bother", "Is this annoying?"), Is.Not.Null);
			Assert.That(m_store.AddNote(EntryGuid, "bother", "   "), Is.Null,
				"a blank note is discarded, nothing written (§5.2)");
			m_store.Dispose();
			m_store = null;

			using (var fresh = AnnotationRepository.FromFile("id", PrimaryNotesPath, new NullProgress()))
			{
				var note = fresh.GetMatchesByPrimaryRefKey(EntryGuid).Single();
				Assert.That(note.ClassName, Is.EqualTo("question"), "class is always 'question' (§5.1)");
				Assert.That(note.RefUnEscaped, Is.EqualTo(
					"silfw://localhost/link?app=flex&database=current&server=&tool=default"
					+ $"&guid={EntryGuid}&tag=&id={EntryGuid}&label=bother"),
					"the silfw template with the same guid in guid= and id=, label last (§4)");

				var message = note.Messages.Single();
				Assert.That(message.Author, Is.EqualTo(Environment.UserName),
					"first message author = Environment.UserName (SendReceiveUser, §5.3)");
				Assert.That(message.Status, Is.Empty, "first message status is '' (§5.4)");
				var dateAttribute = message.Element.Attribute("date")?.Value;
				Assert.That(DateTime.TryParseExact(dateAttribute, Annotation.TimeFormatWithTimeZone,
						CultureInfo.InvariantCulture, DateTimeStyles.None, out _), Is.True,
					$"message date '{dateAttribute}' must use yyyy-MM-ddTHH:mm:ssK");
			}

			var doc = XDocument.Load(PrimaryNotesPath);
			Assert.That(doc.Root?.Name.LocalName, Is.EqualTo("notes"));
			Assert.That((string)doc.Root?.Attribute("version"), Is.EqualTo("0"));
		}

		/// <summary>§8.3 — stub-file creation: exact legacy content, and never rewritten when present.</summary>
		[Test]
		public void StubFile_IsCreatedWithTheExactLegacyContent_AndIsNeverRewritten()
		{
			var stubPath = Path.Combine(m_folder, ChorusNotesStore.StubFileName);

			m_store = new ChorusNotesStore(m_folder);
			Assert.That(File.Exists(stubPath), Is.True, "opening the store creates the missing stub (§1)");
			Assert.That(File.ReadAllText(stubPath), Is.EqualTo(
				"This is a stub file to provide an attachment point for Lexicon.fwstub.ChorusNotes"
				+ Environment.NewLine), "the single legacy line, exactly (MessageSlice.cs:86-98)");
			Assert.That(File.ReadAllBytes(stubPath).Take(3), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }),
				"UTF-8 with BOM, like the legacy StreamWriter");
			m_store.Dispose();
			m_store = null;

			// Idempotence: an existing stub — whatever its content — is left untouched.
			File.WriteAllText(stubPath, "sentinel");
			m_store = new ChorusNotesStore(m_folder);
			Assert.That(File.ReadAllText(stubPath), Is.EqualTo("sentinel"));
		}

		/// <summary>§8.4 — guid-keyed lexdb conflict notes appear; new notes only ever land in the primary file.</summary>
		[Test]
		public void LexdbNotes_KeyedByGuid_Appear_AndNewNotesNeverLandThere()
		{
			var lexdbNotesPath = CreateLexdbNotesFile(LexdbConflictAnnotationXml);
			m_store = new ChorusNotesStore(m_folder);

			Assert.That(m_store.GetAnnotationsFor(EntryGuid).Select(a => a.ClassName),
				Is.EquivalentTo(new[] { "mergeConflict" }),
				"notes in Linguistics/Lexicon/*.lexdb.ChorusNotes keyed by guid= show on the entry's bar");

			var lexdbBytesBefore = File.ReadAllBytes(lexdbNotesPath);
			Assert.That(m_store.AddNote(EntryGuid, "bother", "a fresh question"), Is.Not.Null);

			Assert.That(m_store.GetAnnotationsFor(EntryGuid).Select(a => a.ClassName),
				Is.EquivalentTo(new[] { "question", "mergeConflict" }));
			Assert.That(File.ReadAllBytes(lexdbNotesPath), Is.EqualTo(lexdbBytesBefore),
				"new notes always go to the primary repository, never a .lexdb file (§3.3)");

			m_store.Dispose();
			m_store = null;
			using (var primary = AnnotationRepository.FromFile("id", PrimaryNotesPath, new NullProgress()))
			{
				Assert.That(primary.GetMatchesByPrimaryRefKey(EntryGuid).Single().ClassName,
					Is.EqualTo("question"), "the new note landed in Lexicon.fwstub.ChorusNotes");
			}
		}

		/// <summary>§8.5 — resolve toggles append empty status messages; conflicts are not resolvable.</summary>
		[Test]
		public void ResolveToggles_AppendEmptyStatusMessages_AndConflictNotesAreNotResolvable()
		{
			CreateLexdbNotesFile(LexdbConflictAnnotationXml);
			m_store = new ChorusNotesStore(m_folder);
			var note = m_store.AddNote(EntryGuid, "bother", "Is this annoying?");

			Assert.That(note.CanResolve, Is.True);
			Assert.That(m_store.ToggleResolved(note), Is.True);
			Assert.That(note.IsClosed, Is.True, "the resolve toggle closes an open question");
			Assert.That(note.Messages.Count(), Is.EqualTo(2),
				"SetStatus appends an EMPTY message carrying the new status (§5.4)");
			Assert.That(note.Messages.Last().Text, Is.Empty);
			Assert.That(note.Messages.Last().Status, Is.EqualTo(Annotation.Closed));

			Assert.That(m_store.ToggleResolved(note), Is.True);
			Assert.That(note.IsClosed, Is.False, "toggling again reopens");
			Assert.That(note.Messages.Count(), Is.EqualTo(3));
			Assert.That(note.Messages.Last().Status, Is.EqualTo(Annotation.Open));

			// The toggles persisted through the canonical save lane (§4: never write the file directly).
			using (var fresh = AnnotationRepository.FromString("id",
				File.ReadAllText(PrimaryNotesPath)))
			{
				Assert.That(fresh.GetMatchesByPrimaryRefKey(EntryGuid).Single().Messages.Count(),
					Is.EqualTo(3));
			}

			// Resolvability pins the SHIPPED LibChorus (6.0.0-beta), not Chorus master docs: its
			// Annotation.CanResolve excludes only the lowercase classes "conflict" and "note", so
			// "mergeConflict"/"notification" ARE resolvable — the same behavior the legacy WinForms
			// bar (built on this very assembly) exhibited. Parity = defer to the library; if a
			// Chorus upgrade changes the exclusion list, this pin tells us.
			var conflict = m_store.GetAnnotationsFor(EntryGuid).Single(a => a.ClassName == "mergeConflict");
			Assert.That(conflict.CanResolve, Is.True,
				"shipped LibChorus: mergeConflict is resolvable (legacy bar parity)");
			Assert.That(new Annotation(System.Xml.Linq.XElement.Parse(
					"<annotation class='note' ref='silfw://localhost/link?id=x' guid='"
					+ System.Guid.NewGuid() + "'/>")).CanResolve,
				Is.False, "shipped LibChorus: plain 'note' annotations are not resolvable");
			Assert.That(new Annotation(System.Xml.Linq.XElement.Parse(
					"<annotation class='conflict' ref='silfw://localhost/link?id=x' guid='"
					+ System.Guid.NewGuid() + "'/>")).CanResolve,
				Is.False, "shipped LibChorus: legacy 'conflict' annotations are not resolvable");
		}

		/// <summary>§6 — external file change (e.g. after S/R) notifies the store's observers.</summary>
		[Test]
		public void ExternalChangeToThePrimaryFile_RaisesNotesChanged()
		{
			m_store = new ChorusNotesStore(m_folder);
			var notified = new System.Threading.ManualResetEventSlim(false);
			m_store.NotesChanged += (s, e) => notified.Set();

			WritePrimaryNotesFile(LegacyAnnotationXml); // an S/R pulling a note from a teammate

			Assert.That(notified.Wait(TimeSpan.FromSeconds(10)), Is.True,
				"the store's own debounced file watcher must surface an external write as NotesChanged");
		}
	}

	/// <summary>
	/// chorus-notes-contract.md §3/§4 over the memory cache: the bar's keys are LOWERCASE guids —
	/// the entry's own and every AllOwnedObjects guid (notes attached to senses/allomorphs show on
	/// the entry's bar) — and the new-note label is the entry's ShortName (headword).
	/// </summary>
	[TestFixture]
	public class ChorusNotesEntryKeyTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;
		private IMoStemAllomorph m_morph;
		private ILexSense m_sense;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				m_morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = m_morph;
				m_morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
				m_sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create(m_entry, null, "house");
			});
		}

		[Test]
		public void EntryKeys_AreLowercaseGuids_IncludingAllOwnedObjects_AndLabelIsShortName()
		{
			Assert.That(ChorusNotesEntryModel.GetIdForObject(m_entry),
				Is.EqualTo(m_entry.Guid.ToString().ToLowerInvariant()));

			var additional = ChorusNotesEntryModel.GetAdditionalIdsForObject(m_entry).ToList();
			Assert.That(additional, Has.Member(m_sense.Guid.ToString().ToLowerInvariant()),
				"notes attached to senses show on the entry's bar (§3.4)");
			Assert.That(additional, Has.Member(m_morph.Guid.ToString().ToLowerInvariant()),
				"notes attached to allomorphs show on the entry's bar (§3.4)");
			Assert.That(additional, Has.All.Matches<string>(id => id == id.ToLowerInvariant()),
				"every key is lowercased for the case-sensitive raw compare");

			Assert.That(ChorusNotesEntryModel.GetLabelForObject(m_entry), Is.EqualTo(m_entry.ShortName),
				"label = ICmObject.ShortName, the headword (§4)");
		}

		[Test]
		public void NewNoteUrl_FollowsTheSilfwTemplate_WithTheEntryGuidTwice()
		{
			var guid = m_entry.Guid.ToString().ToLowerInvariant();
			Assert.That(ChorusNotesStore.BuildRefUrl(guid, m_entry.ShortName), Is.EqualTo(
				"silfw://localhost/link?app=flex&database=current&server=&tool=default"
				+ $"&guid={guid}&tag=&id={guid}&label={m_entry.ShortName}"));
		}
	}
}
