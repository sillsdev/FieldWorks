// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using XCore;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Test functions of the Sandbox MorphemeBreaker class.
	/// </summary>
	[TestFixture]
	public class MorphemeBreakerTest : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		[Test]
		public void Phrase_BreakIntoMorphs()
		{
			// Test word breaks on a standard wordform.
			string baseWord1 = "xxxpus";
			string baseWord1_morphs1 = "xxxpus";
			List<string> morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord1_morphs1, baseWord1);
			Assert.AreEqual(1, morphs.Count,
							String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord1_morphs1, baseWord1));
			Assert.AreEqual("xxxpus", morphs[0]);

			string baseWord1_morphs2 = "xxxpu -s";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord1_morphs2, baseWord1);
			Assert.AreEqual(2, morphs.Count,
							String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord1_morphs2, baseWord1));
			Assert.AreEqual("xxxpu", morphs[0]);
			Assert.AreEqual("-s", morphs[1]);

			string baseWord1_morphs3 = "xxx pu -s";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord1_morphs3, baseWord1);
			Assert.AreEqual(3, morphs.Count,
							String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord1_morphs3, baseWord1));
			Assert.AreEqual("xxx", morphs[0]);
			Assert.AreEqual("pu", morphs[1]);
			Assert.AreEqual("-s", morphs[2]);

			// Test word breaks on a phrase wordform.
			string baseWord2 = "xxxpus xxxyalola";
			string baseWord2_morphs1 = "pus xxxyalola";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord2_morphs1, baseWord2);
			Assert.AreEqual(1, morphs.Count,
							String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord2_morphs1, baseWord2));
			Assert.AreEqual("pus xxxyalola", morphs[0]);

			string baseWord2_morphs2 = "xxxpus xxxyalo  -la";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord2_morphs2, baseWord2);
			Assert.AreEqual(2, morphs.Count,
							String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord2_morphs2, baseWord2));
			Assert.AreEqual("xxxpus xxxyalo", morphs[0]);
			Assert.AreEqual("-la", morphs[1]);

			string baseWord2_morphs3 = "xxxpus  xxxyalo  -la";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord2_morphs3, baseWord2);
			Assert.AreEqual(3, morphs.Count,
							String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord2_morphs3, baseWord2));
			Assert.AreEqual("xxxpus", morphs[0]);
			Assert.AreEqual("xxxyalo", morphs[1]);
			Assert.AreEqual("-la", morphs[2]);

			string baseWord3 = "xxxnihimbilira xxxpus xxxyalola";
			string baseWord3_morphs1 = "xxxnihimbilira xxxpus xxxyalola";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord3_morphs1, baseWord3);
			Assert.AreEqual(1, morphs.Count,
							String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord3_morphs1, baseWord3));
			Assert.AreEqual("xxxnihimbilira xxxpus xxxyalola", morphs[0]);

			string baseWord3_morphs2 = "xxxnihimbili  -ra  xxxpus xxxyalola";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord3_morphs2, baseWord3);
			Assert.AreEqual(3, morphs.Count,
							String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord3_morphs2, baseWord3));
			Assert.AreEqual("xxxnihimbili", morphs[0]);
			Assert.AreEqual("-ra", morphs[1]);
			Assert.AreEqual("xxxpus xxxyalola", morphs[2]);

			string baseWord4 = "xxxpus xxxyalola xxxnihimbilira";
			string baseWord4_morphs1 = "xxxpus xxxyalola xxxnihimbilira";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord4_morphs1, baseWord4);
			Assert.AreEqual(1, morphs.Count,
							String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord4_morphs1, baseWord4));
			Assert.AreEqual("xxxpus xxxyalola xxxnihimbilira", morphs[0]);

			string baseWord4_morphs2 = "xxxpus  xxxyalola xxxnihimbilira";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord4_morphs2, baseWord4);
			Assert.AreEqual(2, morphs.Count,
							String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord4_morphs2, baseWord4));
			Assert.AreEqual("xxxpus", morphs[0]);
			Assert.AreEqual("xxxyalola xxxnihimbilira", morphs[1]);

			string baseWord5 = "kicked the bucket";
			string baseWord5_morphs2 = "kick the bucket  -ed";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord5_morphs2, baseWord5);
			Assert.AreEqual(2, morphs.Count,
							String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord5_morphs2, baseWord5));
			Assert.AreEqual("kick the bucket", morphs[0]);
			Assert.AreEqual("-ed", morphs[1]);
		}

		[Test]
		public void EstablishDefaultEntry_Empty_Basic()
		{
			using (var mediator = new Mediator())
			{
				var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
				entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, "here");
				morph.MorphTypeRA = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphSuffix);
				var testSandbox = new AddWordsToLexiconTests.SandboxForTests(Cache, mediator, new PropertyTable(mediator),
					InterlinLineChoices.DefaultChoices(Cache.LangProject, Cache.DefaultVernWs, Cache.DefaultAnalWs));
				testSandbox.RawWordform = TsStringUtils.MakeString("here", Cache.DefaultVernWs);
				Assert.DoesNotThrow(() => testSandbox.EstablishDefaultEntry(morph.Hvo, "here", morph.MorphTypeRA, false));
				Assert.DoesNotThrow(() => testSandbox.EstablishDefaultEntry(morph.Hvo, "notHere", morph.MorphTypeRA, false));
			}
		}
	}
}
