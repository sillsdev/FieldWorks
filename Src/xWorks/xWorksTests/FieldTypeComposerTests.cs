// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// §19e (T1 composer + T3 edge + T4 workflow, real LCModel) — the remaining detail-editor field types
	/// classify into the right <see cref="RegionFieldKind"/> and their setters round-trip through the
	/// composed edit context: a closed enum combo (the integer-backed enum), an Integer numeric editor, a
	/// generic-date (GenDate) qualifier editor, and the per-field writing-system visibility override. The
	/// composer is the real product path; custom fields give us integer/GenDate slices on a plain entry the
	/// way the legacy generated layout does.
	/// </summary>
	[TestFixture]
	public class FieldTypeComposerTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;
		private int m_flidNumber;
		private int m_flidGenDate;
		private GenDate m_genDate;
		private bool m_fieldsCreated;

		public override void TestSetup()
		{
			base.TestSetup();
			// The fixture cache is shared across tests (MemoryOnlyBackendProviderTestBase): create the
			// custom fields ONCE (re-running UpdateCustomField per test mints duplicate fields), then read
			// their flids each time and build a fresh entry to keep each test independent.
			if (!m_fieldsCreated)
			{
				m_fieldsCreated = true;
				MakeCustomField("Frequency Count", LexEntryTags.kClassId, CellarPropertyType.Integer,
					WritingSystemServices.kwsAnal);
				MakeCustomField("Date Collected", LexEntryTags.kClassId, CellarPropertyType.GenDate, 0);
			}
			m_flidNumber = Cache.MetaDataCacheAccessor.GetFieldId("LexEntry", "Frequency Count", false);
			m_flidGenDate = Cache.MetaDataCacheAccessor.GetFieldId("LexEntry", "Date Collected", false);
			m_genDate = new GenDate(GenDate.PrecisionType.Approximate, GenDate.UnknownMonth, GenDate.UnknownDay,
				1985, true);

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
				Cache.DomainDataByFlid.SetInt(m_entry.Hvo, m_flidNumber, 42);
				((ISilDataAccessManaged)Cache.DomainDataByFlid).SetGenDate(m_entry.Hvo, m_flidGenDate, m_genDate);
			});
		}

		private int MakeCustomField(string userLabel, int classId, CellarPropertyType type, int wsSelector)
		{
			var fd = new FieldDescription(Cache)
			{
				Userlabel = userLabel, HelpString = string.Empty, Class = classId, Type = type,
				WsSelector = wsSelector
			};
			fd.UpdateCustomField();
			return fd.Id;
		}

		private IReadOnlyList<LexicalEditRegionField> Fields()
			=> FullEntryRegionComposer.Compose(m_entry, Cache).Model.Fields;

		private ComposedEntryRegion Compose() => FullEntryRegionComposer.Compose(m_entry, Cache);

		// ---- Integer (E2/C3) ----

		[Test]
		public void Integer_ComposesIntegerKind_NotPlainText()
		{
			var number = Fields().First(f => f.Label == "Frequency Count");
			Assert.That(number.Kind, Is.EqualTo(RegionFieldKind.Integer),
				"an integer field composes the dedicated numeric editor, not the plain text editor");
			Assert.That(number.Values.Single().Value, Is.EqualTo("42"));
		}

		[Test]
		public void Integer_SetterAcceptsValidInt_AndNegative_RejectsNonNumericEmptyOverflow()
		{
			var composed = Compose();
			var number = composed.Model.Fields.First(f => f.Label == "Frequency Count");
			var sda = Cache.DomainDataByFlid;

			Assert.That(composed.EditContext.TrySetText(number, "", "abc"), Is.False, "non-numeric rejected");
			Assert.That(composed.EditContext.TrySetText(number, "", ""), Is.False, "empty rejected");
			Assert.That(composed.EditContext.TrySetText(number, "", "99999999999999999"), Is.False,
				"overflow rejected (no corruption)");
			Assert.That(composed.EditContext.TrySetText(number, "", "-7"), Is.True, "negative accepted");
			composed.EditContext.Commit();
			Assert.That(sda.get_IntProp(m_entry.Hvo, m_flidNumber), Is.EqualTo(-7));
		}

		// ---- GenDate qualifiers (E3/C4/C5) ----

		[Test]
		public void GenDate_ComposesDateKind_WithGenDateFlavor_AndLongStringValue()
		{
			var date = Fields().First(f => f.Label == "Date Collected");
			Assert.That(date.Kind, Is.EqualTo(RegionFieldKind.Date));
			Assert.That(date.DateKind, Is.EqualTo(RegionDateKind.GenDate),
				"a GenDate field carries the GenDate flavor so the structured qualifier editor is built");
			Assert.That(date.Values.Single().Value, Is.EqualTo(m_genDate.ToLongString()));
		}

		[Test]
		public void GenDate_ComposedStringFromTheEditor_RoundTripsThroughTheSetter()
		{
			// The structured editor composes these canonical long-strings; assert each parses + stores
			// the intended GenDate, so the qualifier editor and the GenDate setter agree.
			AssertGenDateRoundTrip(FwGenDateField.Compose(1985, GenDatePrecision.Approximate, true),
				GenDate.PrecisionType.Approximate, 1985, true);
			AssertGenDateRoundTrip(FwGenDateField.Compose(1200, GenDatePrecision.Before, true),
				GenDate.PrecisionType.Before, 1200, true);
			AssertGenDateRoundTrip(FwGenDateField.Compose(500, GenDatePrecision.After, false),
				GenDate.PrecisionType.After, 500, false);
			AssertGenDateRoundTrip(FwGenDateField.Compose(300, GenDatePrecision.Approximate, false),
				GenDate.PrecisionType.Approximate, 300, false);
			AssertGenDateRoundTrip(FwGenDateField.Compose(1990, GenDatePrecision.Exact, true),
				GenDate.PrecisionType.Exact, 1990, true);
		}

		private void AssertGenDateRoundTrip(string composed, GenDate.PrecisionType precision, int year, bool ad)
		{
			var ctx = Compose();
			var date = ctx.Model.Fields.First(f => f.Label == "Date Collected");
			Assert.That(ctx.EditContext.TrySetOption(date, composed), Is.True,
				$"the composed string '{composed}' must parse + stage");
			ctx.EditContext.Commit();
			var stored = ((ISilDataAccessManaged)Cache.DomainDataByFlid).get_GenDateProp(m_entry.Hvo, m_flidGenDate);
			Assert.That(stored.Precision, Is.EqualTo(precision), $"precision of '{composed}'");
			Assert.That(stored.Year, Is.EqualTo(year), $"year of '{composed}'");
			Assert.That(stored.IsAD, Is.EqualTo(ad), $"era of '{composed}'");
		}

		[Test]
		public void GenDate_SetterRejectsGarbage_AndEmptyClears()
		{
			var composed = Compose();
			var date = composed.Model.Fields.First(f => f.Label == "Date Collected");
			Assert.That(composed.EditContext.TrySetOption(date, "not a date at all"), Is.False,
				"unparseable GenDate text is rejected, never stored");

			Assert.That(composed.EditContext.TrySetOption(date, ""), Is.True, "empty clears the GenDate");
			composed.EditContext.Commit();
			var stored = ((ISilDataAccessManaged)Cache.DomainDataByFlid).get_GenDateProp(m_entry.Hvo, m_flidGenDate);
			Assert.That(stored.IsEmpty, Is.True);
		}

		// ---- Field-visibility toggle (E9) — pin the showHidden toggle over the never-fields ----

		// ---- Per-field writing-system visibility override (E10/C9) ----

		[Test]
		public void PerFieldWs_LimitsDisplayedWritingSystems_OneVsMany()
		{
			var all = FullEntryRegionComposer.ResolveWritingSystems(Cache, "all analysis");
			Assume.That(all.Count, Is.GreaterThan(0), "the project must expose at least one analysis ws");
			var firstTag = all[0].Id;

			// A single-ws override yields exactly that ws, in order.
			var one = FullEntryRegionComposer.ApplyVisibleWritingSystems(all, new[] { firstTag });
			Assert.That(one.Select(w => w.Id), Is.EqualTo(new[] { firstTag }),
				"a one-ws override shows exactly that writing system");

			// No override keeps the full set; an all-unknown override degrades to the full set (never blank).
			Assert.That(FullEntryRegionComposer.ApplyVisibleWritingSystems(all, null), Is.SameAs(all));
			Assert.That(FullEntryRegionComposer.ApplyVisibleWritingSystems(all, new[] { "zz-not-a-ws" }),
				Is.SameAs(all), "a stale override that matches nothing keeps the full set, never blanks the field");
		}

		[Test]
		public void ShowHidden_RevealsNeverFields_HideOmitsThem()
		{
			var hidden = FullEntryRegionComposer.Compose(m_entry, Cache, showHiddenFields: false)
				.Model.Fields;
			var shown = FullEntryRegionComposer.Compose(m_entry, Cache, showHiddenFields: true)
				.Model.Fields;

			// DateCreated is a visibility=never field on the shipped LexEntry layout.
			Assert.That(hidden.Any(f => f.Field == "DateCreated" && f.ObjectHvo == m_entry.Hvo), Is.False,
				"a never-visibility field is hidden when show-hidden is off");
			Assert.That(shown.Any(f => f.Field == "DateCreated" && f.ObjectHvo == m_entry.Hvo), Is.True,
				"the never-visibility field reappears when show-hidden is on");
		}

		// ---- T4 workflow (real cache): edit integer + GenDate(+qualifier) + add a semantic-domain item,
		// commit, reopen, verify all round-tripped; and the cancel journey leaves everything unchanged. ----

		[Test]
		public void Workflow_EditIntegerAndGenDateAndAddSemanticDomain_RoundTrips()
		{
			var sense = AddSenseWithSemanticDomainList(out var subDomain);

			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var number = composed.Model.Fields.First(f => f.Label == "Frequency Count");
			var date = composed.Model.Fields.First(f => f.Label == "Date Collected");
			var domains = composed.Model.Fields.First(f => f.Field == "SemanticDomains"
				&& f.ObjectHvo == sense.Hvo && f.Kind == RegionFieldKind.ReferenceVector);

			// The semantic-domain options carry the 2-level hierarchy (depth 0 parent, depth 1 child).
			Assert.That(domains.Options.Any(o => o.Depth == 1), Is.True,
				"the semantic-domain chooser carries the 2-level tree as option depth");

			Assert.That(composed.EditContext.TrySetText(number, "", "7"), Is.True);
			Assert.That(composed.EditContext.TrySetOption(date,
				FwGenDateField.Compose(1850, GenDatePrecision.Before, true)), Is.True);
			Assert.That(composed.EditContext.TryAddReferenceItem(domains, subDomain.Guid.ToString()), Is.True);
			composed.EditContext.Commit();

			// Reopen (recompose from domain truth) and verify every field round-tripped.
			var reopened = FullEntryRegionComposer.Compose(m_entry, Cache);
			Assert.That(reopened.Model.Fields.First(f => f.Label == "Frequency Count").Values.Single().Value,
				Is.EqualTo("7"), "the integer round-tripped");
			var stored = ((ISilDataAccessManaged)Cache.DomainDataByFlid)
				.get_GenDateProp(m_entry.Hvo, m_flidGenDate);
			Assert.That(stored.Precision, Is.EqualTo(GenDate.PrecisionType.Before));
			Assert.That(stored.Year, Is.EqualTo(1850), "the GenDate qualifier round-tripped");
			Assert.That(sense.SemanticDomainsRC.Contains(subDomain), Is.True,
				"the added semantic-domain item round-tripped");
		}

		[Test]
		public void Workflow_CancelLeavesEverythingUnchanged()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			var number = composed.Model.Fields.First(f => f.Label == "Frequency Count");
			composed.EditContext.TrySetText(number, "", "999");
			composed.EditContext.Cancel();

			var reopened = FullEntryRegionComposer.Compose(m_entry, Cache);
			Assert.That(reopened.Model.Fields.First(f => f.Label == "Frequency Count").Values.Single().Value,
				Is.EqualTo("42"), "cancel leaves the integer at its original value");
		}

		// Builds a sense on the entry and a semantic-domain list with a parent + child (2-level tree) so
		// the reference-vector chooser composes the hierarchy. Returns the sense and the child sub-domain.
		private ILexSense AddSenseWithSemanticDomainList(out ICmSemanticDomain subDomain)
		{
			ICmSemanticDomain child = null;
			ILexSense sense = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				if (Cache.LangProject.SemanticDomainListOA == null)
					Cache.LangProject.SemanticDomainListOA =
						Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
				var list = Cache.LangProject.SemanticDomainListOA;
				var sdFactory = Cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>();
				var parent = sdFactory.Create();
				list.PossibilitiesOS.Add(parent);
				parent.Name.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("Universe, creation", Cache.DefaultAnalWs));
				child = sdFactory.Create();
				parent.SubPossibilitiesOS.Add(child);
				child.Name.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("Sky", Cache.DefaultAnalWs));

				sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				m_entry.SensesOS.Add(sense);
				sense.Gloss.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("house", Cache.DefaultAnalWs));
			});
			subDomain = child;
			return sense;
		}
	}
}
