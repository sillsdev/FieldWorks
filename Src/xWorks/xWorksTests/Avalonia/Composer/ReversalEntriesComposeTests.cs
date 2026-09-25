// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The sense's Reversal Entries row (LT-22673) against real LCModel data: how the row
	/// composes, what each row commit does to the reversal entries, and where a row's jump
	/// lands.
	/// </summary>
	[TestFixture]
	public class ReversalEntriesComposeTests : MemoryOnlyBackendProviderTestBase
	{
		private const string ReversalField = "ReferringReversalIndexEntries";
		private ILexEntry m_entry;
		private ILexSense m_sense;
		private IReversalIndex m_enIndex;

		public override void TestSetup()
		{
			base.TestSetup();
			// Real controls need the headless platform and its theme resources.
			FwAvaloniaRuntime.EnsureInitialized();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				// The project outlives each test, so start every test from empty reversal
				// indexes.
				var indexes = Cache.LanguageProject.LexDbOA.ReversalIndexesOC;
				foreach (var index in indexes.ToList())
					indexes.Remove(index);
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
				m_sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				m_entry.SensesOS.Add(m_sense);
				m_enIndex = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>()
					.FindOrCreateIndexForWs(Cache.DefaultAnalWs);
			});
		}

		private int EnWs => Cache.DefaultAnalWs;

		private string EnTag => Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id;

		private int UndoCount => Cache.ActionHandlerAccessor.UndoableSequenceCount;

		private IReversalIndexEntry AddEntry(IReversalIndex index, string form, params ILexSense[] senses)
		{
			IReversalIndexEntry entry = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				entry = index.FindOrCreateReversalEntry(form);
				foreach (var sense in senses)
					entry.SensesRS.Add(sense);
			});
			return entry;
		}

		private IReversalIndexEntry AddSubentry(IReversalIndexEntry parent, string form, params ILexSense[] senses)
		{
			IReversalIndexEntry entry = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				entry = Cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>().Create();
				parent.SubentriesOS.Add(entry);
				entry.ReversalForm.set_String(EnWs, form);
				foreach (var sense in senses)
					entry.SensesRS.Add(sense);
			});
			return entry;
		}

		private ILexSense AddOtherSense()
		{
			ILexSense sense = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
			});
			return sense;
		}

		private CoreWritingSystemDefinition AddAnalysisWs(string tag, bool rightToLeft = false)
		{
			CoreWritingSystemDefinition ws = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				WritingSystemServices.FindOrCreateWritingSystem(Cache, null, tag, false, false, out ws);
				ws.RightToLeftScript = rightToLeft;
				Cache.LangProject.AddToCurrentAnalysisWritingSystems(ws);
			});
			return ws;
		}

		private IReversalIndex AddIndex(CoreWritingSystemDefinition ws)
		{
			IReversalIndex index = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				index = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(ws.Handle));
			return index;
		}

		// The host is the detail view's own fenced context, so commits reach the real undo stack.
		private (ReversalDetailEditContext Editing, IDetailEditContext Host) NewContext(ILexSense sense = null)
		{
			var host = DetailComposer.Compose(m_entry, Cache).EditContext;
			return (new ReversalDetailEditContext(Cache, host, sense ?? m_sense, "Reversal Entries"), host);
		}

		private static DetailReversalGroup Group(IReadOnlyList<DetailReversalGroup> groups, string wsTag)
			=> groups.Single(g => g.WsTag == wsTag);

		private static DetailReversalRow AddRow(DetailReversalGroup group) => group.Rows.Single(r => r.IsAddSlot);

		private static List<string> EntryTexts(DetailReversalGroup group)
			=> group.Rows.Where(r => !r.IsAddSlot).Select(r => r.Text).ToList();

		private DetailField ReversalRow(bool showHidden = false)
			=> DetailComposer.Compose(m_entry, Cache, showHidden).Model.Fields
				.SingleOrDefault(f => f.Kind == DetailFieldKind.Custom && f.Field == ReversalField);

		// ----- Compose -----

		[Test]
		public void SenseWithEntries_ComposesTheReversalField()
		{
			AddEntry(m_enIndex, "dwelling", m_sense);

			var row = ReversalRow();

			Assert.That(row, Is.Not.Null, "a sense with an entry composes the plugin row");
			Assert.That(row.ControlFactory(new SliceFactoryContext()), Is.InstanceOf<FwReversalEntriesField>(),
				"the row builds the reversal editor, not the Unsupported text");
		}

		[Test]
		public void EntriesInOneIndex_ShareAGroup_ThenOneAddRow()
		{
			AddEntry(m_enIndex, "dwelling", m_sense);
			AddEntry(m_enIndex, "abode", m_sense);

			var group = Group(NewContext().Editing.CreateGroups(null), EnTag);

			Assert.That(EntryTexts(group), Is.EquivalentTo(new[] { "dwelling", "abode" }));
			Assert.That(group.Rows.Count(r => r.IsAddSlot), Is.EqualTo(1));
			Assert.That(group.Rows.Last().IsAddSlot, Is.True, "the add row comes last");
			Assert.That(group.WsAbbrev, Is.EqualTo(Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Abbreviation));
		}

		[Test]
		public void EntriesInTwoIndexes_ComposeTwoGroups_InAnalysisOrder()
		{
			var es = AddAnalysisWs("es");
			AddEntry(m_enIndex, "dwelling", m_sense);
			AddEntry(AddIndex(es), "casa", m_sense);

			var groups = NewContext().Editing.CreateGroups(null);

			Assert.That(groups.Select(g => g.WsTag), Is.EqualTo(new[] { EnTag, es.Id }));
			Assert.That(EntryTexts(Group(groups, es.Id)), Is.EqualTo(new[] { "casa" }));
		}

		[Test]
		public void Subentry_ShowsItsAncestorChain()
		{
			var top = AddEntry(m_enIndex, "top");
			var middle = AddSubentry(top, "middle");
			AddSubentry(middle, "leaf", m_sense);

			var group = Group(NewContext().Editing.CreateGroups(null), EnTag);

			Assert.That(EntryTexts(group), Is.EqualTo(new[] { "top: middle: leaf" }));
		}

		[Test]
		public void FormsInOtherWritingSystems_RideTheRowAsAlternatives()
		{
			// A variant of the index's own language, which is what an entry's alternatives use.
			var enGb = AddAnalysisWs("en-GB");
			var entry = AddEntry(m_enIndex, "house", m_sense);
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => entry.ReversalForm.set_String(enGb.Handle, "houze"));

			var group = Group(NewContext().Editing.CreateGroups(null), EnTag);
			var row = group.Rows.Single(r => !r.IsAddSlot);

			Assert.That(row.OtherWsForms.Select(a => a.Text), Is.EqualTo(new[] { "houze" }));
			Assert.That(row.OtherWsForms.Single().WsAbbrev, Is.EqualTo(enGb.Abbreviation));
			Assert.That(AddRow(group).OtherWsForms, Is.Empty, "an add row has no alternatives");
		}

		[Test]
		public void AnalysisWsWithoutAnIndex_ComposesNoGroup_AndCreatesNoIndex()
		{
			var es = AddAnalysisWs("es");
			AddEntry(m_enIndex, "dwelling", m_sense);
			var indexCount = Cache.LanguageProject.LexDbOA.ReversalIndexesOC.Count;

			var groups = NewContext().Editing.CreateGroups(null);

			Assert.That(groups.Select(g => g.WsTag), Has.None.EqualTo(es.Id));
			Assert.That(Cache.LanguageProject.LexDbOA.ReversalIndexesOC.Count, Is.EqualTo(indexCount),
				"composing never creates a reversal index");
		}

		[Test]
		public void NoEntries_HidesTheRow()
		{
			Assert.That(ReversalRow(), Is.Null,
				"an ifdata row with no reversal entries composes nothing, not an Unsupported row");
		}

		[Test]
		public void NoEntries_ShowHiddenFields_ComposesOnlyAddRows()
		{
			var row = ReversalRow(showHidden: true);

			Assert.That(row, Is.Not.Null, "Show Hidden Fields reveals the empty row");
			var groups = NewContext().Editing.CreateGroups(null);
			Assert.That(groups.SelectMany(g => g.Rows).All(r => r.IsAddSlot), Is.True);
			Assert.That(Group(groups, EnTag).Rows, Has.Count.EqualTo(1));
		}

		[Test]
		public void EntriesOnlyInAHiddenWs_StillComposeTheRow()
		{
			var es = AddAnalysisWs("es");
			AddEntry(AddIndex(es), "casa", m_sense);

			Assert.That(ReversalRow(), Is.Not.Null, "an entry in any writing system is data");
			var groups = NewContext().Editing.CreateGroups(new[] { EnTag });
			Assert.That(groups.Select(g => g.WsTag), Is.EqualTo(new[] { EnTag }));
			Assert.That(AddRow(Group(groups, EnTag)), Is.Not.Null, "the visible index offers its add row");
		}

		[Test]
		public void EntriesInAHiddenWs_ProduceNoRow()
		{
			var es = AddAnalysisWs("es");
			AddEntry(m_enIndex, "dwelling", m_sense);
			AddEntry(AddIndex(es), "casa", m_sense);

			var groups = NewContext().Editing.CreateGroups(new[] { EnTag });

			Assert.That(groups.Select(g => g.WsTag), Is.EqualTo(new[] { EnTag }));
		}

		// ----- Edit -> one undo step -----

		[Test]
		public void TypingInTheAddRow_CreatesAndLinksAnEntry_AsOneUndoStep()
		{
			AddEntry(m_enIndex, "dwelling", m_sense);
			var (editing, host) = NewContext();
			var add = AddRow(Group(editing.CreateGroups(null), EnTag));
			var before = UndoCount;

			Assert.That(editing.TryCommitRow(add.RowKey, "home"), Is.True);
			host.Commit();

			Assert.That(UndoCount - before, Is.EqualTo(1));
			Assert.That(m_sense.ReferringReversalIndexEntries.Select(e => e.ReversalForm.get_String(EnWs).Text),
				Is.EquivalentTo(new[] { "dwelling", "home" }));
			var regrouped = Group(NewContext().Editing.CreateGroups(null), EnTag);
			Assert.That(regrouped.Rows.Last().IsAddSlot, Is.True, "the re-shown group ends in a fresh add row");
		}

		[Test]
		public void ColonChain_CreatesEntryAndSubentry_LinksTheDeepest()
		{
			var (editing, host) = NewContext();
			var add = AddRow(Group(editing.CreateGroups(null), EnTag));
			var before = UndoCount;

			editing.TryCommitRow(add.RowKey, "body: arm");
			host.Commit();

			Assert.That(UndoCount - before, Is.EqualTo(1));
			var linked = m_sense.ReferringReversalIndexEntries.Single();
			Assert.That(linked.ReversalForm.get_String(EnWs).Text, Is.EqualTo("arm"));
			Assert.That(linked.OwningEntry?.ReversalForm.get_String(EnWs).Text, Is.EqualTo("body"));
			Assert.That(linked.OwningEntry.SensesRS, Does.Not.Contain(m_sense), "only the deepest entry is linked");
			Assert.That(EntryTexts(Group(NewContext().Editing.CreateGroups(null), EnTag)),
				Is.EqualTo(new[] { "body: arm" }));
		}

		[Test]
		public void ColonChain_ReusesAnExistingParent()
		{
			var body = AddEntry(m_enIndex, "body");
			var (editing, host) = NewContext();
			var add = AddRow(Group(editing.CreateGroups(null), EnTag));

			editing.TryCommitRow(add.RowKey, "body: arm");
			host.Commit();

			Assert.That(m_enIndex.EntriesOC.Count(e => e.ReversalForm.get_String(EnWs).Text == "body"), Is.EqualTo(1),
				"the existing parent is reused, not duplicated");
			Assert.That(m_sense.ReferringReversalIndexEntries.Single().OwningEntry, Is.SameAs(body));
		}

		[Test]
		public void ColonChain_FindsTheChainUnderWhicheverHomographHasIt()
		{
			AddEntry(m_enIndex, "body");
			IReversalIndexEntry second = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				second = Cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>().Create();
				m_enIndex.EntriesOC.Add(second);
				second.ReversalForm.set_String(EnWs, "body");
			});
			var arm = AddSubentry(second, "arm");
			var (editing, host) = NewContext();

			editing.TryCommitRow(AddRow(Group(editing.CreateGroups(null), EnTag)).RowKey, "body: arm");
			host.Commit();

			Assert.That(m_sense.ReferringReversalIndexEntries.Single(), Is.SameAs(arm),
				"a full match wins over a partial one");
		}

		[Test]
		public void ClearingARow_UnlinksAndDeletesAnOrphan_AsOneUndoStep()
		{
			var entry = AddEntry(m_enIndex, "dwelling", m_sense);
			var (editing, host) = NewContext();
			var row = Group(editing.CreateGroups(null), EnTag).Rows.First(r => !r.IsAddSlot);
			var before = UndoCount;

			Assert.That(editing.TryCommitRow(row.RowKey, string.Empty), Is.True);
			host.Commit();

			Assert.That(UndoCount - before, Is.EqualTo(1));
			Assert.That(m_sense.ReferringReversalIndexEntries, Is.Empty);
			Assert.That(entry.IsValidObject, Is.False, "an entry with no senses and no subentries is deleted");
		}

		[Test]
		public void ClearingARow_KeepsAnEntryAnotherSenseUses()
		{
			var other = AddOtherSense();
			var entry = AddEntry(m_enIndex, "dwelling", m_sense, other);
			var (editing, host) = NewContext();
			var row = Group(editing.CreateGroups(null), EnTag).Rows.First(r => !r.IsAddSlot);

			editing.TryCommitRow(row.RowKey, string.Empty);
			host.Commit();

			Assert.That(entry.IsValidObject, Is.True);
			Assert.That(entry.SensesRS, Does.Contain(other));
			Assert.That(entry.SensesRS, Does.Not.Contain(m_sense));
		}

		[Test]
		public void ClearingARow_KeepsAnEntryWithASubentry()
		{
			var other = AddOtherSense();
			var entry = AddEntry(m_enIndex, "body", m_sense);
			var arm = AddSubentry(entry, "arm", other);
			var (editing, host) = NewContext();
			var row = Group(editing.CreateGroups(null), EnTag).Rows.First(r => !r.IsAddSlot);

			editing.TryCommitRow(row.RowKey, string.Empty);
			host.Commit();

			Assert.That(entry.IsValidObject, Is.True, "an entry that still has subentries stays");
			Assert.That(arm.SensesRS, Does.Contain(other));
		}

		[Test]
		public void ShorteningAChain_DeletesTheAncestorsItLeavesEmpty()
		{
			var (editing, host) = NewContext();
			var key = AddRow(Group(editing.CreateGroups(null), EnTag)).RowKey;
			editing.TryCommitRow(key, "arm: hand: finger");
			host.Commit();
			var finger = m_sense.ReferringReversalIndexEntries.Single();
			var hand = finger.OwningEntry;
			var arm = hand.OwningEntry;

			editing.TryCommitRow(key, "arm");
			host.Commit();

			Assert.That(m_sense.ReferringReversalIndexEntries.Single(), Is.SameAs(arm));
			Assert.That(finger.IsValidObject, Is.False);
			Assert.That(hand.IsValidObject, Is.False, "the parent left with no senses and no subentries goes too");
		}

		[Test]
		public void ClearingAChain_DeletesEveryLevelLeftEmpty()
		{
			var (editing, host) = NewContext();
			var key = AddRow(Group(editing.CreateGroups(null), EnTag)).RowKey;
			editing.TryCommitRow(key, "arm: hand: finger");
			host.Commit();
			var finger = m_sense.ReferringReversalIndexEntries.Single();
			var hand = finger.OwningEntry;
			var arm = hand.OwningEntry;

			editing.TryCommitRow(key, string.Empty);
			host.Commit();

			Assert.That(new[] { finger, hand, arm }.Any(e => e.IsValidObject), Is.False);
		}

		[Test]
		public void TheCascade_StopsAtAnAncestorStillInUse()
		{
			var other = AddOtherSense();
			var arm = AddEntry(m_enIndex, "arm", other);
			var hand = AddSubentry(arm, "hand");
			AddSubentry(hand, "palm", other);
			var finger = AddSubentry(hand, "finger", m_sense);
			var (editing, host) = NewContext();
			var row = Group(editing.CreateGroups(null), EnTag).Rows.First(r => !r.IsAddSlot);

			editing.TryCommitRow(row.RowKey, string.Empty);
			host.Commit();

			Assert.That(finger.IsValidObject, Is.False);
			Assert.That(hand.IsValidObject, Is.True, "hand still has the subentry palm");
			Assert.That(arm.IsValidObject, Is.True, "arm still has another sense");
		}

		[Test]
		public void EditingASharedEntrysRow_RelinksWithoutRenaming()
		{
			var other = AddOtherSense();
			var shared = AddEntry(m_enIndex, "dwelling", m_sense, other);
			var (editing, host) = NewContext();
			var row = Group(editing.CreateGroups(null), EnTag).Rows.First(r => !r.IsAddSlot);

			editing.TryCommitRow(row.RowKey, "abode");
			host.Commit();

			Assert.That(shared.ReversalForm.get_String(EnWs).Text, Is.EqualTo("dwelling"), "the shared entry keeps its form");
			Assert.That(shared.SensesRS, Does.Contain(other));
			Assert.That(m_sense.ReferringReversalIndexEntries.Single().ReversalForm.get_String(EnWs).Text,
				Is.EqualTo("abode"));
		}

		[Test]
		public void UnchangedText_StagesNothing()
		{
			AddEntry(m_enIndex, "dwelling", m_sense);
			var (editing, host) = NewContext();
			var row = Group(editing.CreateGroups(null), EnTag).Rows.First(r => !r.IsAddSlot);

			Assert.That(editing.TryCommitRow(row.RowKey, "dwelling"), Is.False);
			Assert.That(editing.TryCommitRow(AddRow(Group(editing.CreateGroups(null), EnTag)).RowKey, string.Empty),
				Is.False, "an empty add row is not a change");
			Assert.That(host.IsOpen, Is.False, "no session opens for a no-op commit");
		}

		[Test]
		public void ReeditingAJustAddedRow_EditsThatEntry()
		{
			var (editing, host) = NewContext();
			var add = AddRow(Group(editing.CreateGroups(null), EnTag));

			editing.TryCommitRow(add.RowKey, "one");
			host.Commit();
			var first = m_sense.ReferringReversalIndexEntries.Single();
			editing.TryCommitRow(add.RowKey, "two");
			host.Commit();

			Assert.That(m_sense.ReferringReversalIndexEntries.Select(e => e.ReversalForm.get_String(EnWs).Text),
				Is.EqualTo(new[] { "two" }), "the row's second commit replaced its own entry");
			Assert.That(first.IsValidObject, Is.False, "the replaced entry was orphaned, so it is gone");
		}

		[Test]
		public void EditsToSeveralSlots_InOneFieldVisit_AreOneUndoStep()
		{
			AddEntry(m_enIndex, "dwelling", m_sense);
			var (editing, host) = NewContext();
			var group = Group(editing.CreateGroups(null), EnTag);
			var before = UndoCount;

			editing.TryCommitRow(group.Rows.First(r => !r.IsAddSlot).RowKey, "abode");
			editing.TryCommitRow(AddRow(group).RowKey, "home");
			host.Commit();

			Assert.That(UndoCount - before, Is.EqualTo(1), "the field's edits share one undo step");
			Assert.That(m_sense.ReferringReversalIndexEntries.Select(e => e.ReversalForm.get_String(EnWs).Text),
				Is.EquivalentTo(new[] { "abode", "home" }));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_sense.ReferringReversalIndexEntries.Select(e => e.ReversalForm.get_String(EnWs).Text),
				Is.EquivalentTo(new[] { "dwelling" }), "one Ctrl+Z undoes the whole visit");
		}

		[Test]
		public void IssuedAddKeys_AddSeparateEntries_InOneUndoStep()
		{
			var (editing, host) = NewContext();
			var add = AddRow(Group(editing.CreateGroups(null), EnTag));
			var second = editing.IssueAddRowKey(add.RowKey);
			var before = UndoCount;

			Assert.That(second, Is.Not.Null.And.Not.EqualTo(add.RowKey));
			editing.TryCommitRow(add.RowKey, "one");
			editing.TryCommitRow(second, "two");
			host.Commit();

			Assert.That(UndoCount - before, Is.EqualTo(1));
			Assert.That(m_sense.ReferringReversalIndexEntries.Select(e => e.ReversalForm.get_String(EnWs).Text),
				Is.EquivalentTo(new[] { "one", "two" }), "the second slot adds, it does not replace the first");
			Assert.That(editing.IssueAddRowKey("no-such-key"), Is.Null);
		}

		[Test]
		public void AnAddedEntry_PersistsIntoTheNextCompose()
		{
			var (editing, host) = NewContext();
			editing.TryCommitRow(AddRow(Group(editing.CreateGroups(null), EnTag)).RowKey, "home");
			host.Commit();

			Assert.That(ReversalRow(), Is.Not.Null, "the row now has data, so it composes");
			Assert.That(EntryTexts(Group(NewContext().Editing.CreateGroups(null), EnTag)), Is.EqualTo(new[] { "home" }));
		}

		// ----- Validation -----

		[Test]
		public void ACommit_TripsNoValidationRule()
		{
			var (editing, host) = NewContext();
			editing.TryCommitRow(AddRow(Group(editing.CreateGroups(null), EnTag)).RowKey, "home");

			Assert.That(host.Validate(), Is.Empty);
			Assert.That(editing.Validate(), Is.Empty);
			host.Commit();
		}

		// ----- Re-show -----

		[Test]
		public void AnExternalLinkChange_ShowsOnTheNextCompose()
		{
			var (editing, _) = NewContext();
			editing.CreateGroups(null);
			AddEntry(m_enIndex, "dwelling", m_sense);

			Assert.That(EntryTexts(Group(NewContext().Editing.CreateGroups(null), EnTag)), Is.EqualTo(new[] { "dwelling" }));
		}

		[Test]
		public void UndoAndRedo_OfAnAdd_RoundTrip()
		{
			var (editing, host) = NewContext();
			editing.TryCommitRow(AddRow(Group(editing.CreateGroups(null), EnTag)).RowKey, "home");
			host.Commit();

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_sense.ReferringReversalIndexEntries, Is.Empty);
			Cache.ActionHandlerAccessor.Redo();
			Assert.That(m_sense.ReferringReversalIndexEntries.Single().ReversalForm.get_String(EnWs).Text,
				Is.EqualTo("home"));
		}

		// ----- Cluster / bidi -----

		[Test]
		public void RightToLeftIndex_RoundTripsItsForm()
		{
			var ar = AddAnalysisWs("ar", rightToLeft: true);
			AddIndex(ar);
			var (editing, host) = NewContext();
			var group = Group(editing.CreateGroups(null), ar.Id);
			const string form = "بَيْت";

			Assert.That(group.RightToLeft, Is.True);
			editing.TryCommitRow(AddRow(group).RowKey, form);
			host.Commit();

			Assert.That(m_sense.ReferringReversalIndexEntries.Single().ReversalForm.get_String(ar.Handle).Text,
				Is.EqualTo(form), "combining marks survive the colon split and the commit");
		}

		[TestCase(" body : arm ", new[] { "body", "arm" })]
		[TestCase("::", new string[0])]
		[TestCase("body::arm", new[] { "body", "arm" })]
		[TestCase(":body:", new[] { "body" })]
		[TestCase("", new string[0])]
		public void SplitForms_TrimsAndDropsEmptyParts(string text, string[] expected)
		{
			Assert.That(ReversalDetailEditContext.SplitForms(text), Is.EqualTo(expected));
		}

		// ----- Navigation -----

		[Test]
		public void ATopLevelEntrysRow_JumpsToThatEntry()
		{
			var entry = AddEntry(m_enIndex, "dwelling", m_sense);
			var (editing, _) = NewContext();
			var group = Group(editing.CreateGroups(null), EnTag);

			Assert.That(editing.TryResolveMainEntryGuid(group.Rows.First(r => !r.IsAddSlot).RowKey),
				Is.EqualTo(entry.Guid));
			Assert.That(editing.TryResolveMainEntryGuid(AddRow(group).RowKey), Is.Null, "an add row has no entry");
		}

		[Test]
		public void ASubentrysRow_JumpsToItsMainEntry()
		{
			var top = AddEntry(m_enIndex, "body");
			AddSubentry(top, "arm", m_sense);
			var (editing, _) = NewContext();
			var row = Group(editing.CreateGroups(null), EnTag).Rows.First(r => !r.IsAddSlot);

			Assert.That(editing.TryResolveMainEntryGuid(row.RowKey), Is.EqualTo(top.Guid));
		}

		[Test]
		public void TheJumpRequest_TargetsTheReversalIndexTool()
		{
			var entry = AddEntry(m_enIndex, "dwelling", m_sense);
			var requests = new List<DetailLinkRequest>();

			ReversalIndexEntryPlugin.RequestShowInReversalIndex(requests.Add, null, entry.Guid);

			Assert.That(requests.Single().Link.Tool, Is.EqualTo(ReversalIndexEntryPlugin.ReversalIndexTool));
			Assert.That(requests.Single().Link.TargetGuid, Is.EqualTo(entry.Guid.ToString()));
		}

		[Test]
		public void BuildControl_WithoutAHostEditContext_IsReadOnly()
		{
			AddEntry(m_enIndex, "dwelling", m_sense);
			var context = new SlicePluginBuildContext(m_sense, null, () => null, Cache);

			var field = new ReversalIndexEntryPlugin().BuildControl(context);

			Assert.That(field, Is.InstanceOf<FwReversalEntriesField>(),
				"with no edit session to stage on, the rows still show");
		}
	}
}
