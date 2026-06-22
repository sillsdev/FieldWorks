// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 3.1) — T4 commit round-trip for the Basic IPA Symbol editor: the
	/// sink writes the phoneme's BasicIPASymbol through the region's fenced session (one undo step) and the
	/// value round-trips from domain truth. // PARITY: derive-on-commit (Description/Features) deferred.
	/// </summary>
	[TestFixture]
	public class BasicIpaSymbolEditWorkflowTests : MemoryOnlyBackendProviderTestBase
	{
		private IPhPhoneme m_phoneme;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS.Add(
					Cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create());
				m_phoneme = Cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC.Add(m_phoneme);
				m_phoneme.Name.SetVernacularDefaultWritingSystem("p");
			});
		}

		[Test]
		public void Commit_WritesBasicIPASymbol_AndUndoRestores()
		{
			var host = new ComposedRegionEditContext(Cache, m_phoneme,
				new Dictionary<string, Func<string, string, bool>>(),
				new Dictionary<string, Func<string, bool>>());
			var sink = new BasicIpaSymbolEditSink(m_phoneme, Cache, host);

			Assert.That(sink.Commit("pʰ"), Is.True);
			Assert.That(m_phoneme.BasicIPASymbol.Text, Is.EqualTo("pʰ"),
				"the committed IPA symbol persisted to the phoneme");

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_phoneme.BasicIPASymbol?.Text ?? string.Empty, Is.Not.EqualTo("pʰ"),
				"one Undo reverts the IPA symbol edit");
		}

		[Test]
		public void Derive_FillsDescriptionForAKnownSymbol_AndClearsOnEmpty()
		{
			// "a" is a standard low vowel present in BasicIPAInfo.xml with an English description; the
			// test cache's default analysis WS is English. Derive-on-commit fills the empty Description.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_phoneme.BasicIPASymbol = SIL.LCModel.Core.Text.TsStringUtils.MakeString("a", Cache.DefaultVernWs);
				BasicIpaSymbolDeriver.Derive(m_phoneme, Cache);
			});
			var derived = m_phoneme.Description.AnalysisDefaultWritingSystem.Text;
			Assert.That(derived, Is.Not.Null.And.Not.Empty,
				"committing a known IPA symbol derives its description from BasicIPAInfo.xml");

			// Clearing the symbol clears the derived description.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_phoneme.BasicIPASymbol = SIL.LCModel.Core.Text.TsStringUtils.MakeString(string.Empty, Cache.DefaultVernWs);
				BasicIpaSymbolDeriver.Derive(m_phoneme, Cache);
			});
			Assert.That(m_phoneme.Description.AnalysisDefaultWritingSystem.Text ?? string.Empty, Is.Empty,
				"clearing the symbol clears the derived description");
		}
	}
}
