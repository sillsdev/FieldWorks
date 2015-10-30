// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Test the various functions that depend on the StringServices functions and settings related to
	/// positioning homograph number and sense number. We don't need to change the data, just the state of StringServices,
	/// so we don't need to restore for each test.
	/// </summary>
	[TestFixture]
	public class ConfigureHomographTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_kick; // no homographs
		private ILexSense m_kickS1;
		private ILexEntry m_rightCorrect; // homograph 1
		private ILexSense m_rightCorrectS2;
		private ILexEntry m_rightDirection; // homograph 2
		private ILexSense m_rightDirectionS1;
		private int m_wsVern;
		private int m_wsAnalysis;
		private HomographConfiguration m_hc;
		/// <summary>
		/// It's convenient to test pretty much all these functions with one set of data.
		/// </summary>
		///
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_wsVern = Cache.DefaultVernWs;
			m_wsAnalysis = Cache.DefaultAnalWs;
			m_hc = Cache.ServiceLocator.GetInstance<HomographConfiguration>();
			UndoableUnitOfWorkHelper.Do("undoit", "redoit", m_actionHandler,
				() =>
					{
						m_kick = MakeEntry("kick", "strike with foot");
						m_kickS1 = m_kick.SensesOS[0];
						m_rightCorrect = MakeEntry("right", "correct");
						m_rightCorrectS2 = MakeSense(m_rightCorrect, "morally perfect");
						m_rightDirection = MakeEntry("right", "turn right");
						m_rightDirectionS1 = m_rightDirection.SensesOS[0];
					});
		}

		void ResetConfiguration()
		{
			m_hc.HomographNumberBefore = false;
			m_hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.Main, true);
			m_hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.DictionaryCrossRef, true);
			m_hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.ReversalCrossRef, true);
			m_hc.ShowSenseNumberRef = true;
			m_hc.ShowSenseNumberReversal = true;
		}

		private ILexEntry MakeEntry(string sLexForm, string gloss)
		{
			var lme = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			lme.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			lme.LexemeFormOA.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString(sLexForm, Cache.DefaultVernWs);
			MakeSense(lme, gloss);
			return lme;
		}

		private ILexSense MakeSense(ILexEntry lme, string gloss)
		{
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			lme.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(gloss, Cache.DefaultAnalWs);
			return sense;
		}

		/// <summary>
		/// Check the routine that handles headwords as displayed at the start of entries and in the UI.
		/// </summary>
		[Test]
		public void MLHeadword()
		{
			ResetConfiguration();
			VerifyTss(m_kick.HeadWordForWs(m_wsVern), new [] {new Run("kick", m_wsVern, "")});
			VerifyTss(m_rightCorrect.HeadWordForWs(m_wsVern),
				new[] { new Run("right", m_wsVern, ""), new Run("1", m_wsVern, HomographConfiguration.ksHomographNumberStyle) });
			m_hc.HomographNumberBefore = true;
			VerifyTss(m_kick.HeadWordForWs(m_wsVern), new[] { new Run("kick", m_wsVern, "") });
			VerifyTss(m_rightCorrect.HeadWordForWs(m_wsVern),
				new[] { new Run("1", m_wsVern, HomographConfiguration.ksHomographNumberStyle), new Run("right", m_wsVern, "") });
			m_hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.DictionaryCrossRef, false); // no effect on this method
			VerifyTss(m_rightDirection.HeadWordForWs(m_wsVern),
				new[] { new Run("2", m_wsVern, HomographConfiguration.ksHomographNumberStyle), new Run("right", m_wsVern, "") });
			m_hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.Main, false);
			VerifyTss(m_kick.HeadWordForWs(m_wsVern), new[] { new Run("kick", m_wsVern, "") });
			VerifyTss(m_rightCorrect.HeadWordForWs(m_wsVern), new[] {new Run("right", m_wsVern, "") });
			m_hc.HomographNumberBefore = false; // make sure this doesn't affect it.
			VerifyTss(m_rightCorrect.HeadWordForWs(m_wsVern), new[] { new Run("right", m_wsVern, "") });
		}

		/// <summary>
		/// Check the routine that handles headwords as displayed in cross-refs in the dictionary
		/// </summary>
		[Test]
		public void HeadwordRef()
		{
			ResetConfiguration();
			VerifyTss(m_kick.HeadWordRefForWs(m_wsVern), new[] { new Run("kick", m_wsVern, "") });
			VerifyTss(m_rightCorrect.HeadWordRefForWs(m_wsVern),
				new[] { new Run("right", m_wsVern, ""), new Run("1", m_wsVern, HomographConfiguration.ksHomographNumberStyle) });
			m_hc.HomographNumberBefore = true;
			VerifyTss(m_kick.HeadWordRefForWs(m_wsVern), new[] { new Run("kick", m_wsVern, "") });
			VerifyTss(m_rightCorrect.HeadWordRefForWs(m_wsVern),
				new[] { new Run("1", m_wsVern, HomographConfiguration.ksHomographNumberStyle), new Run("right", m_wsVern, "") });
			m_hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.ReversalCrossRef, false);
			VerifyTss(m_rightCorrect.HeadWordRefForWs(m_wsVern),
				new[] { new Run("1", m_wsVern, HomographConfiguration.ksHomographNumberStyle), new Run("right", m_wsVern, "") });
			m_hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.DictionaryCrossRef, false);
			VerifyTss(m_kick.HeadWordRefForWs(m_wsVern), new[] { new Run("kick", m_wsVern, "") });
			VerifyTss(m_rightCorrect.HeadWordRefForWs(m_wsVern), new[] { new Run("right", m_wsVern, "") });
			m_hc.HomographNumberBefore = false; // make sure this doesn't affect it.
			m_hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.Main, false);
			VerifyTss(m_rightCorrect.HeadWordRefForWs(m_wsVern), new[] { new Run("right", m_wsVern, "") });
		}

		/// <summary>
		/// Check the routine that handles headwords as displayed in cross-refs in the reversal index
		/// Enhance JohnT: the three headword tests are very similar. Especially if we get more variants, it may be worth merging them.
		/// </summary>
		[Test]
		public void HeadwordReversal()
		{
			ResetConfiguration();
			VerifyTss(m_kick.HeadWordReversalForWs(m_wsVern), new[] { new Run("kick", m_wsVern, "") });
			VerifyTss(m_rightCorrect.HeadWordReversalForWs(m_wsVern),
				new[] { new Run("right", m_wsVern, ""), new Run("1", m_wsVern, HomographConfiguration.ksHomographNumberStyle) });
			m_hc.HomographNumberBefore = true;
			VerifyTss(m_kick.HeadWordReversalForWs(m_wsVern), new[] { new Run("kick", m_wsVern, "") });
			VerifyTss(m_rightCorrect.HeadWordReversalForWs(m_wsVern),
				new[] { new Run("1", m_wsVern, HomographConfiguration.ksHomographNumberStyle), new Run("right", m_wsVern, "") });
			m_hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.DictionaryCrossRef, false);
			VerifyTss(m_rightCorrect.HeadWordReversalForWs(m_wsVern),
				new[] { new Run("1", m_wsVern, HomographConfiguration.ksHomographNumberStyle), new Run("right", m_wsVern, "") });
			m_hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.ReversalCrossRef, false);
			VerifyTss(m_kick.HeadWordReversalForWs(m_wsVern), new[] { new Run("kick", m_wsVern, "") });
			VerifyTss(m_rightCorrect.HeadWordReversalForWs(m_wsVern), new[] { new Run("right", m_wsVern, "") });
			m_hc.HomographNumberBefore = false; // make sure this doesn't affect it.
			m_hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.Main, false);
			VerifyTss(m_rightCorrect.HeadWordReversalForWs(m_wsVern), new[] { new Run("right", m_wsVern, "") });
		}

		/// <summary>
		/// Check the routine that handles headwords (and possibly sense numbers) when referring to senses.
		/// </summary>
		[Test]
		public void OwnerOutlineName()
		{
			TrySenseOutlineName((sense, ws) => sense.OwnerOutlineNameForWs(ws), HomographConfiguration.HeadwordVariant.DictionaryCrossRef,
				() => m_hc.ShowSenseNumberRef = false);

		}

		/// <summary>
		/// Check the routine that handles headwords (and possibly sense numbers) when referring to senses in reversal entries.
		/// </summary>
		[Test]
		public void ReversalName()
		{
			TrySenseOutlineName((sense, ws) => sense.ReversalNameForWs(ws), HomographConfiguration.HeadwordVariant.ReversalCrossRef,
				() => m_hc.ShowSenseNumberReversal = false);

		}
		private void TrySenseOutlineName(Func<ILexSense, int, ITsString> reader, HomographConfiguration.HeadwordVariant hv,
			Action turnOffSenseNumber)
		{
			ResetConfiguration();
			VerifyTss(reader(m_kickS1, m_wsVern), new[] { new Run("kick", m_wsVern, "") });
			VerifyTss(reader(m_rightDirectionS1, m_wsVern),
				new[] { new Run("right", m_wsVern, ""), new Run("2", m_wsVern, HomographConfiguration.ksHomographNumberStyle) });
			var numAfterCorrect2Runs = new[] { new Run("right", m_wsVern, ""),
				new Run("1", m_wsVern, HomographConfiguration.ksHomographNumberStyle),
				new Run(" 2", m_wsAnalysis, HomographConfiguration.ksSenseReferenceNumberStyle)};
			VerifyTss(reader(m_rightCorrectS2, m_wsVern), numAfterCorrect2Runs);
			// Owner outline is affected by putting homograph number first.
			m_hc.HomographNumberBefore = true;
			VerifyTss(reader(m_kickS1, m_wsVern), new[] { new Run("kick", m_wsVern, "") });
			VerifyTss(reader(m_rightDirectionS1, m_wsVern),
				new[] {  new Run("2", m_wsVern, HomographConfiguration.ksHomographNumberStyle), new Run("right", m_wsVern, "") });
			var numBeforeCorrect2Runs = new[] { new Run("1", m_wsVern, HomographConfiguration.ksHomographNumberStyle),
				new Run("right", m_wsVern, ""),
				new Run(" 2", m_wsAnalysis, HomographConfiguration.ksSenseReferenceNumberStyle)};
			VerifyTss(reader(m_rightCorrectS2, m_wsVern), numBeforeCorrect2Runs);
			// Not by hiding HN in main or reversal cross-refs
			m_hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.ReversalCrossRef, false);
			m_hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.DictionaryCrossRef, false);
			m_hc.SetShowHomographNumber(hv, true);
			VerifyTss(reader(m_rightCorrectS2, m_wsVern), numBeforeCorrect2Runs);

			// Hiding HN in appropriate kind of cross-refs also hides sense number.
			m_hc.SetShowHomographNumber(hv, false);
			VerifyTss(reader(m_kickS1, m_wsVern), new[] { new Run("kick", m_wsVern, "") });
			VerifyTss(reader(m_rightDirectionS1, m_wsVern), new[] { new Run("right", m_wsVern, "") });
			VerifyTss(reader(m_rightCorrectS2, m_wsVern), new[] { new Run("right", m_wsVern, "")});

			// .. even if it's in the normal position
			m_hc.HomographNumberBefore = false;
			VerifyTss(reader(m_rightCorrectS2, m_wsVern), new[] { new Run("right", m_wsVern, "") });

			// but it can be turned on again...
			m_hc.SetShowHomographNumber(hv, true);
			VerifyTss(reader(m_rightCorrectS2, m_wsVern), numAfterCorrect2Runs);

			// We can also turn just the sense number off...
			turnOffSenseNumber();
			VerifyTss(reader(m_rightCorrectS2, m_wsVern),
				new[]
					{
						new Run("right", m_wsVern, ""),
						new Run("1", m_wsVern, HomographConfiguration.ksHomographNumberStyle)
					});
		}

		void VerifyTss(ITsString tss, Run[] runs)
		{
			Assert.That(tss.RunCount, Is.EqualTo(runs.Length));
			for (int i = 0; i < runs.Length; i++)
			{
				Assert.That(tss.get_RunText(i), Is.EqualTo(runs[i].Text));
				var props = tss.get_Properties(i);
				int nvar;
				Assert.That(props.GetIntPropValues((int)FwTextPropType.ktptWs, out nvar), Is.EqualTo(runs[i].Ws));
				var style = props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				if (string.IsNullOrEmpty(runs[i].Style))
					Assert.That(string.IsNullOrEmpty(style), Is.True);
				else
					Assert.That(style, Is.EqualTo(runs[i].Style));
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void PersistData()
		{
			var hc = new HomographConfiguration();
			Assert.That(hc.PersistData, Is.EqualTo(""));
			hc.HomographNumberBefore = true;
			hc.ShowSenseNumberRef = false;
			Assert.That(hc.PersistData, Is.EqualTo("before snRef "));
			var hc2 = new HomographConfiguration();
			hc2.PersistData = hc.PersistData;
			Assert.That(hc2.ShowSenseNumberRef, Is.False);
			Assert.That(hc2.HomographNumberBefore, Is.True);
			Assert.That(hc2.ShowSenseNumberReversal, Is.True);

			hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.Main, false);
			hc2.PersistData = hc.PersistData;
			Assert.That(hc2.ShowHomographNumber(HomographConfiguration.HeadwordVariant.Main), Is.False);
			Assert.That(hc2.HomographNumberBefore, Is.False);

			hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.Main, true);
			hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.DictionaryCrossRef, false);
			hc2.PersistData = hc.PersistData;
			Assert.That(hc2.ShowHomographNumber(HomographConfiguration.HeadwordVariant.Main), Is.True);
			Assert.That(hc2.ShowHomographNumber(HomographConfiguration.HeadwordVariant.DictionaryCrossRef), Is.False);
			Assert.That(hc2.ShowHomographNumber(HomographConfiguration.HeadwordVariant.ReversalCrossRef), Is.True);

			hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.ReversalCrossRef, false);
			hc2.PersistData = hc.PersistData;
			Assert.That(hc2.ShowHomographNumber(HomographConfiguration.HeadwordVariant.Main), Is.True);
			Assert.That(hc2.ShowHomographNumber(HomographConfiguration.HeadwordVariant.DictionaryCrossRef), Is.False);
			Assert.That(hc2.ShowHomographNumber(HomographConfiguration.HeadwordVariant.ReversalCrossRef), Is.False);

			hc.SetShowHomographNumber(HomographConfiguration.HeadwordVariant.DictionaryCrossRef, true);
			hc2.PersistData = hc.PersistData;
			Assert.That(hc2.ShowHomographNumber(HomographConfiguration.HeadwordVariant.Main), Is.True);
			Assert.That(hc2.ShowHomographNumber(HomographConfiguration.HeadwordVariant.DictionaryCrossRef), Is.True);
			Assert.That(hc2.ShowHomographNumber(HomographConfiguration.HeadwordVariant.ReversalCrossRef), Is.False);

			hc.ShowSenseNumberRef = true;
			hc.ShowSenseNumberReversal = false;
			hc2.PersistData = hc.PersistData;
			Assert.That(hc2.ShowSenseNumberRef, Is.True);
			Assert.That(hc2.ShowSenseNumberReversal, Is.False);
		}

		class Run
		{
			public string Text;
			public int Ws;
			public string Style;

			public Run(string text, int ws, string style)
			{
				Text = text;
				Ws = ws;
				Style = style;
			}
		}
	}
}
