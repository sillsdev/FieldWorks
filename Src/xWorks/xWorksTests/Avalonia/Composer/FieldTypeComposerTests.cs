// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Composer path (real LCModel): the per-field writing-system visibility override restricts the
	/// displayed writing systems, and the show-hidden toggle reveals/omits visibility=never fields. The
	/// composer is the real product path; custom fields give us a plain entry the way the legacy generated
	/// layout does.
	/// </summary>
	[TestFixture]
	public class FieldTypeComposerTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;
		private int m_flidNumber;
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
			}
			m_flidNumber = Cache.MetaDataCacheAccessor.GetFieldId("LexEntry", "Frequency Count", false);

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
				Cache.DomainDataByFlid.SetInt(m_entry.Hvo, m_flidNumber, 42);
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

		// ---- Per-field writing-system visibility override ----

		[Test]
		public void PerFieldWs_LimitsDisplayedWritingSystems_OneVsMany()
		{
			var all = DetailComposer.ResolveWritingSystems(Cache, "all analysis");
			Assume.That(all.Count, Is.GreaterThan(0), "the project must expose at least one analysis ws");
			var firstTag = all[0].Id;

			// A single-ws override yields exactly that ws, in order.
			var one = DetailComposer.ApplyVisibleWritingSystems(all, new[] { firstTag });
			Assert.That(one.Select(w => w.Id), Is.EqualTo(new[] { firstTag }),
				"a one-ws override shows exactly that writing system");

			// No override keeps the full set; an all-unknown override degrades to the full set (never blank).
			Assert.That(DetailComposer.ApplyVisibleWritingSystems(all, null), Is.SameAs(all));
			Assert.That(DetailComposer.ApplyVisibleWritingSystems(all, new[] { "zz-not-a-ws" }),
				Is.SameAs(all), "a stale override that matches nothing keeps the full set, never blanks the field");
		}

		// ---- Transient "Show all right now" reveal ----

		// The reveal bypasses the ws restriction for EXACTLY the revealed part: Compose
		// consults the template StableId set the host holds until record navigation.
		[Test]
		public void ShowAllReveal_BypassesTheWsRestriction_ForTheRevealedRowOnly()
		{
			CoreWritingSystemDefinition second = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out second);
				Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(second);
			});
			try
			{
				var form = ComposedFormRow(null, null);
				var fullSet = form.Values.Select(v => v.WsTag).ToList();
				Assume.That(fullSet.Count, Is.GreaterThanOrEqualTo(2),
					"the Form row must offer at least two writing systems for a visible reveal");

				var templateId = ViewDefinitionOverrideEditor.StripRuntimeSuffix(form.StableId);
				var restriction = new ViewDefinitionOverride(form.ClassName, form.LayoutName, "detail",
					new[]
					{
						new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibleWritingSystems,
							templateId, writingSystems: new[] { fullSet[0] })
					}, null);
				ViewDefinitionOverrideResolver resolver = (cls, layout) =>
					cls == form.ClassName && layout == form.LayoutName ? restriction : null;

				var restricted = ComposedFormRow(resolver, null);
				Assert.That(restricted.Values.Select(v => v.WsTag), Is.EqualTo(new[] { fullSet[0] }),
					"precondition: the override restricts the row to one writing system");

				var revealedElsewhere = ComposedFormRow(resolver,
					new HashSet<string> { "not-a-part" });
				Assert.That(revealedElsewhere.Values.Select(v => v.WsTag),
					Is.EqualTo(new[] { fullSet[0] }),
					"a reveal keyed to another part leaves this row restricted");

				var revealed = ComposedFormRow(resolver, new HashSet<string> { templateId });
				Assert.That(revealed.Values.Select(v => v.WsTag), Is.EqualTo(fullSet),
					"the revealed row composes with its full writing-system set, in layout order");
			}
			finally
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Remove(second));
			}
		}

		// One reveal covers the whole part: every row sharing the template (each sense's
		// Gloss) composes with the full set.
		[Test]
		public void ShowAllReveal_CoversEveryRowOfThePart()
		{
			CoreWritingSystemDefinition second = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out second);
				Cache.ServiceLocator.WritingSystems.AddToCurrentAnalysisWritingSystems(second);
				var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
				foreach (var gloss in new[] { "first", "second" })
				{
					var sense = senseFactory.Create();
					m_entry.SensesOS.Add(sense);
					sense.Gloss.set_String(Cache.DefaultAnalWs,
						TsStringUtils.MakeString(gloss, Cache.DefaultAnalWs));
				}
			});
			try
			{
				var glossRows = GlossRows(null, null);
				Assume.That(glossRows.Count, Is.EqualTo(2), "one Gloss row per sense");
				var fullCount = glossRows[0].Values.Count;
				Assume.That(fullCount, Is.GreaterThanOrEqualTo(2),
					"the Gloss row must offer at least two writing systems for a visible reveal");
				var templateId = ViewDefinitionOverrideEditor.StripRuntimeSuffix(glossRows[0].StableId);
				Assume.That(ViewDefinitionOverrideEditor.StripRuntimeSuffix(glossRows[1].StableId),
					Is.EqualTo(templateId), "sibling sense rows share one template");

				var restriction = new ViewDefinitionOverride(glossRows[0].ClassName,
					glossRows[0].LayoutName, "detail", new[]
					{
						new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibleWritingSystems,
							templateId, writingSystems: new[] { glossRows[0].Values[0].WsTag })
					}, null);
				ViewDefinitionOverrideResolver resolver = (cls, layout) =>
					cls == glossRows[0].ClassName && layout == glossRows[0].LayoutName
						? restriction
						: null;

				Assert.That(GlossRows(resolver, null).Select(r => r.Values.Count),
					Is.All.EqualTo(1), "precondition: the restriction reaches both sense rows");

				Assert.That(GlossRows(resolver, new HashSet<string> { templateId })
						.Select(r => r.Values.Count), Is.All.EqualTo(fullCount),
					"one revealed template covers every sense's row");
			}
			finally
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Remove(second));
			}
		}

		// The composed Lexeme Form row (the MoForm's own Form field), under the given override
		// resolver and transient reveal set.
		private DetailField ComposedFormRow(ViewDefinitionOverrideResolver overrides,
			ISet<string> showAllWritingSystemsFields)
			=> DetailComposer.Compose(m_entry, Cache, overrides: overrides,
					showAllWritingSystemsFields: showAllWritingSystemsFields)
				.Model.Fields.Single(f => f.Field == "Form" && f.Kind == DetailFieldKind.Text
					&& f.ObjectHvo == m_entry.LexemeFormOA.Hvo);

		// Every composed Gloss text row (one per sense), under the given override resolver and
		// reveal set.
		private List<DetailField> GlossRows(ViewDefinitionOverrideResolver overrides,
			ISet<string> showAllWritingSystemsFields)
			=> DetailComposer.Compose(m_entry, Cache, overrides: overrides,
					showAllWritingSystemsFields: showAllWritingSystemsFields)
				.Model.Fields.Where(f => f.Field == "Gloss" && f.Kind == DetailFieldKind.Text)
				.ToList();

		[Test]
		public void ShowHidden_RevealsNeverFields_HideOmitsThem()
		{
			var hidden = DetailComposer.Compose(m_entry, Cache, showHiddenFields: false)
				.Model.Fields;
			var shown = DetailComposer.Compose(m_entry, Cache, showHiddenFields: true)
				.Model.Fields;

			// DateCreated is a visibility=never field on the shipped LexEntry layout.
			Assert.That(hidden.Any(f => f.Field == "DateCreated" && f.ObjectHvo == m_entry.Hvo), Is.False,
				"a never-visibility field is hidden when show-hidden is off");
			Assert.That(shown.Any(f => f.Field == "DateCreated" && f.ObjectHvo == m_entry.Hvo), Is.True,
				"the never-visibility field reappears when show-hidden is on");
		}
	}
}
