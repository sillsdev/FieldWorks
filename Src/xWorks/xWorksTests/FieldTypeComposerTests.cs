// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
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
			var all = RegionComposer.ResolveWritingSystems(Cache, "all analysis");
			Assume.That(all.Count, Is.GreaterThan(0), "the project must expose at least one analysis ws");
			var firstTag = all[0].Id;

			// A single-ws override yields exactly that ws, in order.
			var one = RegionComposer.ApplyVisibleWritingSystems(all, new[] { firstTag });
			Assert.That(one.Select(w => w.Id), Is.EqualTo(new[] { firstTag }),
				"a one-ws override shows exactly that writing system");

			// No override keeps the full set; an all-unknown override degrades to the full set (never blank).
			Assert.That(RegionComposer.ApplyVisibleWritingSystems(all, null), Is.SameAs(all));
			Assert.That(RegionComposer.ApplyVisibleWritingSystems(all, new[] { "zz-not-a-ws" }),
				Is.SameAs(all), "a stale override that matches nothing keeps the full set, never blanks the field");
		}

		[Test]
		public void ShowHidden_RevealsNeverFields_HideOmitsThem()
		{
			var hidden = RegionComposer.Compose(m_entry, Cache, showHiddenFields: false)
				.Model.Fields;
			var shown = RegionComposer.Compose(m_entry, Cache, showHiddenFields: true)
				.Model.Fields;

			// DateCreated is a visibility=never field on the shipped LexEntry layout.
			Assert.That(hidden.Any(f => f.Field == "DateCreated" && f.ObjectHvo == m_entry.Hvo), Is.False,
				"a never-visibility field is hidden when show-hidden is off");
			Assert.That(shown.Any(f => f.Field == "DateCreated" && f.ObjectHvo == m_entry.Hvo), Is.True,
				"the never-visibility field reappears when show-hidden is on");
		}
	}
}
