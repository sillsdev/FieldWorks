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
			Assert.That(morphs.Count, Is.EqualTo(1), String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord1_morphs1, baseWord1));
			Assert.That(morphs[0], Is.EqualTo("xxxpus"));

			string baseWord1_morphs2 = "xxxpu -s";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord1_morphs2, baseWord1);
			Assert.That(morphs.Count, Is.EqualTo(2), String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord1_morphs2, baseWord1));
			Assert.That(morphs[0], Is.EqualTo("xxxpu"));
			Assert.That(morphs[1], Is.EqualTo("-s"));

			string baseWord1_morphs3 = "xxx pu -s";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord1_morphs3, baseWord1);
			Assert.That(morphs.Count, Is.EqualTo(3), String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord1_morphs3, baseWord1));
			Assert.That(morphs[0], Is.EqualTo("xxx"));
			Assert.That(morphs[1], Is.EqualTo("pu"));
			Assert.That(morphs[2], Is.EqualTo("-s"));

			// Test word breaks on a phrase wordform.
			string baseWord2 = "xxxpus xxxyalola";
			string baseWord2_morphs1 = "pus xxxyalola";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord2_morphs1, baseWord2);
			Assert.That(morphs.Count, Is.EqualTo(1), String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord2_morphs1, baseWord2));
			Assert.That(morphs[0], Is.EqualTo("pus xxxyalola"));

			string baseWord2_morphs2 = "xxxpus xxxyalo  -la";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord2_morphs2, baseWord2);
			Assert.That(morphs.Count, Is.EqualTo(2), String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord2_morphs2, baseWord2));
			Assert.That(morphs[0], Is.EqualTo("xxxpus xxxyalo"));
			Assert.That(morphs[1], Is.EqualTo("-la"));

			string baseWord2_morphs3 = "xxxpus  xxxyalo  -la";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord2_morphs3, baseWord2);
			Assert.That(morphs.Count, Is.EqualTo(3), String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord2_morphs3, baseWord2));
			Assert.That(morphs[0], Is.EqualTo("xxxpus"));
			Assert.That(morphs[1], Is.EqualTo("xxxyalo"));
			Assert.That(morphs[2], Is.EqualTo("-la"));

			string baseWord3 = "xxxnihimbilira xxxpus xxxyalola";
			string baseWord3_morphs1 = "xxxnihimbilira xxxpus xxxyalola";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord3_morphs1, baseWord3);
			Assert.That(morphs.Count, Is.EqualTo(1), String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord3_morphs1, baseWord3));
			Assert.That(morphs[0], Is.EqualTo("xxxnihimbilira xxxpus xxxyalola"));

			string baseWord3_morphs2 = "xxxnihimbili  -ra  xxxpus xxxyalola";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord3_morphs2, baseWord3);
			Assert.That(morphs.Count, Is.EqualTo(3), String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord3_morphs2, baseWord3));
			Assert.That(morphs[0], Is.EqualTo("xxxnihimbili"));
			Assert.That(morphs[1], Is.EqualTo("-ra"));
			Assert.That(morphs[2], Is.EqualTo("xxxpus xxxyalola"));

			string baseWord4 = "xxxpus xxxyalola xxxnihimbilira";
			string baseWord4_morphs1 = "xxxpus xxxyalola xxxnihimbilira";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord4_morphs1, baseWord4);
			Assert.That(morphs.Count, Is.EqualTo(1), String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord4_morphs1, baseWord4));
			Assert.That(morphs[0], Is.EqualTo("xxxpus xxxyalola xxxnihimbilira"));

			string baseWord4_morphs2 = "xxxpus  xxxyalola xxxnihimbilira";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord4_morphs2, baseWord4);
			Assert.That(morphs.Count, Is.EqualTo(2), String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord4_morphs2, baseWord4));
			Assert.That(morphs[0], Is.EqualTo("xxxpus"));
			Assert.That(morphs[1], Is.EqualTo("xxxyalola xxxnihimbilira"));

			string baseWord5 = "kicked the bucket";
			string baseWord5_morphs2 = "kick the bucket  -ed";
			morphs = SandboxBase.MorphemeBreaker.BreakIntoMorphs(baseWord5_morphs2, baseWord5);
			Assert.That(morphs.Count, Is.EqualTo(2), String.Format("Unexpected number of morphs in string '{0}' compared to baseWord '{1}'.", baseWord5_morphs2, baseWord5));
			Assert.That(morphs[0], Is.EqualTo("kick the bucket"));
			Assert.That(morphs[1], Is.EqualTo("-ed"));
		}

		[Test]
		public void EstablishDefaultEntry_Empty_Basic()
		{
			using (var mediator = new Mediator())
			using (var propertyTable = new PropertyTable(mediator))
			using (var testSandbox = new AddWordsToLexiconTests.SandboxForTests(Cache, mediator, propertyTable,
					InterlinLineChoices.DefaultChoices(Cache.LangProject, Cache.DefaultVernWs, Cache.DefaultAnalWs)))
			{
				var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
				entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, "here");
				morph.MorphTypeRA = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphSuffix);

				testSandbox.RawWordform = TsStringUtils.MakeString("here", Cache.DefaultVernWs);
				Assert.DoesNotThrow(() => testSandbox.EstablishDefaultEntry(morph.Hvo, "here", morph.MorphTypeRA, false));
				Assert.DoesNotThrow(() => testSandbox.EstablishDefaultEntry(morph.Hvo, "notHere", morph.MorphTypeRA, false));
			}
		}
	}
}
