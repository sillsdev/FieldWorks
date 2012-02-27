using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Testing the methods of a SandboxBase.
	/// </summary>
	[TestFixture]
	public class SandboxBaseTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Test the indicated method.
		/// </summary>
		[Test]
		public void HandleTab()
		{
			using (var sandbox = SetupNihimbilira())
			{
				//Initialize the selection to the first editable field.
				sandbox.RootBox.MakeSimpleSel(true, true, false, true);
				// Test that we start with a default selection in the first place editing is possible, in the text of the first morpheme.
				VerifySelection(sandbox, false, SandboxBase.ktagSbNamedObjName, SandboxBase.ktagSbMorphForm, 0);
				// One tab moves to the text of the second morpheme, then the third and fourth
				sandbox.HandleTab(false);
				VerifySelection(sandbox, false, SandboxBase.ktagSbNamedObjName, SandboxBase.ktagSbMorphForm, 1);
				sandbox.HandleTab(false);
				VerifySelection(sandbox, false, SandboxBase.ktagSbNamedObjName, SandboxBase.ktagSbMorphForm, 2);
				// -ra is a suffix, so tabbing to the start of us puts us in the prefix.
				sandbox.HandleTab(false);
				VerifySelection(sandbox, false, SandboxBase.ktagSbMorphPrefix, 0, 3);
				// The next tab takes us to the pull-down icon on the lex entries line. Then the other three of them.
				sandbox.HandleTab(false);
				VerifySelection(sandbox, true, SandboxBase.ktagSbNamedObjName, SandboxBase.ktagSbMorphEntry, 0);
				sandbox.HandleTab(false);
				VerifySelection(sandbox, true, SandboxBase.ktagSbNamedObjName, SandboxBase.ktagSbMorphEntry, 1);
				sandbox.HandleTab(false);
				VerifySelection(sandbox, true, SandboxBase.ktagSbNamedObjName, SandboxBase.ktagSbMorphEntry, 2);
				sandbox.HandleTab(false);
				VerifySelection(sandbox, true, SandboxBase.ktagSbNamedObjName, SandboxBase.ktagSbMorphEntry, 3);
				// Next the icon on the word gloss line.
				sandbox.HandleTab(false);
				VerifySelection(sandbox, true, SandboxBase.ktagWordGlossIcon, 0, -1);
				// then into the word gloss itself.
				sandbox.HandleTab(false);
				VerifySelection(sandbox, false, SandboxBase.ktagSbWordGloss, 0, -1);
				// Next the icon on the word cat line.
				sandbox.HandleTab(false);
				VerifySelection(sandbox, true, SandboxBase.ktagWordPosIcon, 0, -1);
				// Then we wrap around to the start icon on the word line.
				sandbox.HandleTab(false);
				VerifySelection(sandbox, true, SandboxBase.ktagAnalysisIcon, 0, -1);
				// And the one on the morphemes line.
				sandbox.HandleTab(false);
				VerifySelection(sandbox, true, SandboxBase.ktagMorphFormIcon, 0, -1);
				// And back to where we started.
				sandbox.HandleTab(false);
				VerifySelection(sandbox, false, SandboxBase.ktagSbNamedObjName, SandboxBase.ktagSbMorphForm, 0);

			// Now we reverse the sequence using shift-tab.
				sandbox.HandleTab(true);
				VerifySelection(sandbox, true, SandboxBase.ktagMorphFormIcon, 0, -1);
				sandbox.HandleTab(true);
				VerifySelection(sandbox, true, SandboxBase.ktagAnalysisIcon, 0, -1);
				sandbox.HandleTab(true);
				VerifySelection(sandbox, true, SandboxBase.ktagWordPosIcon, 0, -1);
				sandbox.HandleTab(true);
				VerifySelection(sandbox, false, SandboxBase.ktagSbWordGloss, 0, -1);
				sandbox.HandleTab(true);
				VerifySelection(sandbox, true, SandboxBase.ktagWordGlossIcon, 0, -1);
				sandbox.HandleTab(true);
				VerifySelection(sandbox, true, SandboxBase.ktagSbNamedObjName, SandboxBase.ktagSbMorphEntry, 3);
				sandbox.HandleTab(true);
				VerifySelection(sandbox, true, SandboxBase.ktagSbNamedObjName, SandboxBase.ktagSbMorphEntry, 2);
				sandbox.HandleTab(true);
				VerifySelection(sandbox, true, SandboxBase.ktagSbNamedObjName, SandboxBase.ktagSbMorphEntry, 1);
				sandbox.HandleTab(true);
				VerifySelection(sandbox, true, SandboxBase.ktagSbNamedObjName, SandboxBase.ktagSbMorphEntry, 0);
				sandbox.HandleTab(true);
				VerifySelection(sandbox, false, SandboxBase.ktagSbMorphPrefix, 0, 3);
				sandbox.HandleTab(true);
				VerifySelection(sandbox, false, SandboxBase.ktagSbNamedObjName, SandboxBase.ktagSbMorphForm, 2);
				sandbox.HandleTab(true);
				VerifySelection(sandbox, false, SandboxBase.ktagSbNamedObjName, SandboxBase.ktagSbMorphForm, 1);
				sandbox.HandleTab(true);
				VerifySelection(sandbox, false, SandboxBase.ktagSbNamedObjName, SandboxBase.ktagSbMorphForm, 0);
				// If you're changing this, remember that teardown does some funny stuff if the selection is on an icon.
				// Best to leave it in a text box.
			}
		}

		/// <summary>
		/// Test various options of GetRealAnalysisMethod.GetBestGloss
		/// </summary>
		[Test]
		public void GetBestGloss()
		{
			var wf = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			var wa = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			wf.AnalysesOC.Add(wa);
			var sda = VwCacheDaClass.Create();
			var wsIds = new List<int>();
			wsIds.Add(Cache.DefaultAnalWs);
			int hvoAbc = 123456;

			// Basic check: no glosses, we don't find any.
			Assert.That(SandboxBase.GetRealAnalysisMethod.GetBestGloss(wa, wsIds, sda, hvoAbc), Is.Null);

			// Only possibility, everything blank, we want it.
			var wgAbc = Cache.ServiceLocator.GetInstance<IWfiGlossFactory>().Create();
			wa.MeaningsOC.Add(wgAbc);
			Assert.That(SandboxBase.GetRealAnalysisMethod.GetBestGloss(wa, wsIds, sda, hvoAbc), Is.EqualTo(wgAbc));

			// Looking for a particular string and not finding one that has it, we fail
			sda.CacheStringAlt(hvoAbc, SandboxBase.ktagSbWordGloss, Cache.DefaultAnalWs, MakeAnalysisString("abc"));
			Assert.That(SandboxBase.GetRealAnalysisMethod.GetBestGloss(wa, wsIds, sda, hvoAbc), Is.Null);

			// Likewise, we won't return a gloss that has relevant alternatives, even if all the desired strings are empty.
			// (An easy way to have all the SDA strings be empty is to use a different HVO.)
			sda.CacheStringAlt(hvoAbc, SandboxBase.ktagSbWordGloss, Cache.DefaultAnalWs, MakeAnalysisString("abc"));
			wgAbc.Form.AnalysisDefaultWritingSystem = MakeAnalysisString("abc");
			Assert.That(SandboxBase.GetRealAnalysisMethod.GetBestGloss(wa, wsIds, sda, 27), Is.Null);

			// Simple success: the one and only WS matches.
			Assert.That(SandboxBase.GetRealAnalysisMethod.GetBestGloss(wa, wsIds, sda, hvoAbc), Is.EqualTo(wgAbc));

			// It matches even if there is another, empty one.
			var wgDef = Cache.ServiceLocator.GetInstance<IWfiGlossFactory>().Create();
			wa.MeaningsOC.Add(wgDef);
			Assert.That(SandboxBase.GetRealAnalysisMethod.GetBestGloss(wa, wsIds, sda, hvoAbc), Is.EqualTo(wgAbc));

			// Also if there is another one with a different value for the form
			wgDef.Form.AnalysisDefaultWritingSystem = MakeAnalysisString("def");
			Assert.That(SandboxBase.GetRealAnalysisMethod.GetBestGloss(wa, wsIds, sda, hvoAbc), Is.EqualTo(wgAbc));

			// We can also find the def one.
			int hvoDef = 123457;
			sda.CacheStringAlt(hvoDef, SandboxBase.ktagSbWordGloss, Cache.DefaultAnalWs, MakeAnalysisString("def"));
			Assert.That(SandboxBase.GetRealAnalysisMethod.GetBestGloss(wa, wsIds, sda, hvoDef), Is.EqualTo(wgDef));

			// Now trying to match two writing systems. One with both correct should beat one with only one correct.
			var wgAbc3 = Cache.ServiceLocator.GetInstance<IWfiGlossFactory>().Create();
			wa.MeaningsOC.Add(wgAbc3);
			wgAbc3.Form.AnalysisDefaultWritingSystem = MakeAnalysisString("abc");
			var wsSpn = Cache.WritingSystemFactory.get_Engine("es").Handle;
			var wsFrn = Cache.WritingSystemFactory.get_Engine("fr").Handle;
			wgAbc3.Form.set_String(wsSpn, Cache.TsStrFactory.MakeString("abcS", wsSpn));
			wgAbc3.Form.set_String(wsFrn, Cache.TsStrFactory.MakeString("abcF", wsFrn));
			wsIds.Add(wsSpn);
			sda.CacheStringAlt(hvoAbc, SandboxBase.ktagSbWordGloss, wsSpn, Cache.TsStrFactory.MakeString("abcS", wsSpn));
			Assert.That(SandboxBase.GetRealAnalysisMethod.GetBestGloss(wa, wsIds, sda, hvoAbc), Is.EqualTo(wgAbc3));

			// Of two partial matches, prefer the one where other alternatives are empty.
			// wgAbc2, wgAbc3 both match on English and Spanish. Neither matches on French, but wgAbc2 has no French
			// at all and is thus a better match.
			var wgAbc2 = Cache.ServiceLocator.GetInstance<IWfiGlossFactory>().Create();
			wa.MeaningsOC.Add(wgAbc2);
			wgAbc2.Form.AnalysisDefaultWritingSystem = MakeAnalysisString("abc");
			wgAbc2.Form.set_String(wsSpn, Cache.TsStrFactory.MakeString("abcS", wsSpn));
			wsIds.Add(wsFrn);
			sda.CacheStringAlt(hvoAbc, SandboxBase.ktagSbWordGloss, wsFrn, Cache.TsStrFactory.MakeString("abcOther", wsFrn));
			Assert.That(SandboxBase.GetRealAnalysisMethod.GetBestGloss(wa, wsIds, sda, hvoAbc), Is.EqualTo(wgAbc2));

			// Of two perfect matches, we prefer the one that has no other information.
			wsIds.Remove(wsFrn);
			Assert.That(SandboxBase.GetRealAnalysisMethod.GetBestGloss(wa, wsIds, sda, hvoAbc), Is.EqualTo(wgAbc2));

			// We will not return one where the WfiGloss has a relevant non-empty alternative, even if the corresponding target is empty.
			sda.CacheStringAlt(hvoAbc, SandboxBase.ktagSbWordGloss, wsSpn, Cache.TsStrFactory.MakeString("", wsSpn));
			wa.MeaningsOC.Remove(wgAbc);
			Assert.That(SandboxBase.GetRealAnalysisMethod.GetBestGloss(wa, wsIds, sda, hvoAbc), Is.Null);
		}

		/// <summary>
		/// Make all the stuff we need to display Nihimbilira in the standard way.
		/// </summary>
		private SandboxBase SetupNihimbilira()
		{
			var occurrence = MakeDataForNihimbilira();
			var lineChoices = InterlinLineChoices.DefaultChoices(Cache.LangProject, Cache.DefaultVernWs, Cache.DefaultAnalWs);
			var sandbox = new SandboxBase(Cache, null, null, lineChoices, occurrence.Analysis.Hvo);
			sandbox.MakeRoot();
			return sandbox;
		}

		void VerifySelection(SandboxBase sandbox, bool fPicture, int tagText, int tagObj, int morphIndex)
		{
			Assert.That(sandbox.RootBox.Selection, Is.Not.Null);
			Assert.That(sandbox.MorphIndex, Is.EqualTo(morphIndex));
			int ihvoRoot;
			int tagTextProp;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ws;
			bool fAssocPrev;
			int ihvoEnd;
			ITsTextProps ttpBogus;
			// Main array of information retrived from sel that made combo.
			SelLevInfo[] rgvsli;
			bool fIsPictureSel; // icon selected.

			IVwSelection sel = sandbox.RootBox.Selection;
			fIsPictureSel = sel.SelType == VwSelType.kstPicture;
			int cvsli = sel.CLevels(false) - 1;
			rgvsli = SelLevInfo.AllTextSelInfo(sel, cvsli,
				out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
				out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);
			Assert.That(fIsPictureSel, Is.EqualTo(fPicture));
			Assert.That(tagTextProp, Is.EqualTo(tagText));
			if (tagTextProp == SandboxBase.ktagSbNamedObjName)
			{
				int tagObjProp = rgvsli[0].tag;
				Assert.That(tagObjProp, Is.EqualTo(tagObj));
			}
			//sandbox.InterlinLineChoices.
		}

		private AnalysisOccurrence MakeDataForNihimbilira()
		{
			var greenMat = MakeText("nihimbilira");
			var wf = MakeWordform("nihimbilira");
			var wa = MakeAnalysis(wf);
			var wg = MakeGloss(wa, "I see");
			var ni = MakeBundle(wa, "ni-", "1SgSubj", "V:(Subject)");
			var him = MakeBundle(wa, "him-", "3SgObj", "V:Object");
			var bili = MakeBundle(wa, "bili", "to see", "trans (1)");
			var ra = MakeBundle(wa, "-ra", "Pres", "sta:Tense");
			var para = (IStTxtPara) greenMat.ContentsOA.ParagraphsOS[0];
			var seg = para.SegmentsOS[0];
			seg.AnalysesRS.Add(wg);
			return new AnalysisOccurrence(seg, 0);
		}

		private FDO.IText MakeText(string contents)
		{
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			Cache.LangProject.TextsOC.Add(text);
			var stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = stText;
			var para = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			stText.ParagraphsOS.Add(para);
			para.Contents = Cache.TsStrFactory.MakeString(contents, Cache.DefaultVernWs);
			var seg = Cache.ServiceLocator.GetInstance<ISegmentFactory>().Create();
			para.SegmentsOS.Add(seg);
			return text;
		}

		private ITsString MakeVernString(string content)
		{
			return Cache.TsStrFactory.MakeString(content, Cache.DefaultVernWs);
		}
		private ITsString MakeAnalysisString(string content)
		{
			return Cache.TsStrFactory.MakeString(content, Cache.DefaultAnalWs);
		}

		private IWfiWordform MakeWordform(string form)
		{
			var result = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			result.Form.VernacularDefaultWritingSystem = MakeVernString(form);
			return result;
		}

		private IWfiAnalysis MakeAnalysis(IWfiWordform wf)
		{
			var wa = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			wf.AnalysesOC.Add(wa);
			return wa;
		}

		private IWfiGloss MakeGloss(IWfiAnalysis wa, string gloss)
		{
			var wg = Cache.ServiceLocator.GetInstance<IWfiGlossFactory>().Create();
			wa.MeaningsOC.Add(wg);
			wg.Form.AnalysisDefaultWritingSystem = MakeAnalysisString("gloss");
			return wg;
		}

		private IWfiMorphBundle MakeBundle(IWfiAnalysis wa, string form, string gloss, string pos)
		{
			var bundle = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
			wa.MorphBundlesOS.Add(bundle);
			var slotType = MoMorphTypeTags.kguidMorphStem;
			if (form.StartsWith("-"))
				slotType = MoMorphTypeTags.kguidMorphSuffix;
			else if (form.EndsWith("-"))
				slotType = MoMorphTypeTags.kguidMorphPrefix;
			var entry = MakeEntry(form.Replace("-", ""), gloss, pos, slotType);
			bundle.SenseRA = entry.SensesOS[0];
			bundle.MorphRA = entry.LexemeFormOA;
			bundle.MsaRA = bundle.SenseRA.MorphoSyntaxAnalysisRA;
			return bundle;
		}

		private IPartOfSpeech MakePartOfSpeech(string name)
		{
			var partOfSpeech = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(partOfSpeech);
			partOfSpeech.Name.AnalysisDefaultWritingSystem = MakeAnalysisString(name);
			partOfSpeech.Abbreviation.AnalysisDefaultWritingSystem = partOfSpeech.Name.AnalysisDefaultWritingSystem;
			return partOfSpeech;
		}

		/// <summary>
		/// Make an entry with the specified lexeme form of the specified slot type, a sense with the specified gloss,
		/// an MSA with the specified part of speech, and generally hook things up as expected.
		/// Assumes all of the required objects need to be created; in general this might not be true, but it works
		/// for the test data here.
		/// </summary>
		private ILexEntry MakeEntry(string lf, string gloss, string pos, Guid slotType)
		{
			// The entry itself.
			ILexEntry entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			// Bare bones of Sense
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem = MakeAnalysisString(gloss);

			// Lexeme Form and MSA.
			IMoForm form;
			IMoMorphSynAnalysis msa;
			if (slotType == MoMorphTypeTags.kguidMorphStem)
			{
				form = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				var stemMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
				msa = stemMsa;
				entry.MorphoSyntaxAnalysesOC.Add(msa);
				stemMsa.PartOfSpeechRA = MakePartOfSpeech(pos);
			}
			else
			{
				form = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
				var affixMsa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
				msa = affixMsa;
				entry.MorphoSyntaxAnalysesOC.Add(msa);
				affixMsa.PartOfSpeechRA = MakePartOfSpeech(pos);
			}
			sense.MorphoSyntaxAnalysisRA = msa;
			entry.LexemeFormOA = form;
			form.Form.VernacularDefaultWritingSystem =MakeVernString(lf);
			form.MorphTypeRA = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(slotType);
			return entry;
		}
	}
}
