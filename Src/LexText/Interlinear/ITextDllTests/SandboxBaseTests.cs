using System;
using System.Collections.Generic;
using System.Linq;
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
		/// This test is for LT-14662, a problem that was happening in a bizarre situation where a morph bundle
		/// has a sense and MSA, but no morph. We need to verify that we can actually make the combo without crashing
		/// in that situation.
		/// </summary>
		[Test]
		public void LexEntriesComboHandlerForBundleWithSenseAndMsaButNoMorph()
		{
			using (var sandbox = SetupSandbox(() =>
				{
					const string ali = "ali";
					var mockText = MakeText(ali);
					var wf = MakeWordform(ali);
					var wa = MakeAnalysis(wf);
					var bundle = MakeBundle(wa, ali, "something", "N");
					bundle.MorphRA = null;
					var para = (IStTxtPara)mockText.ContentsOA.ParagraphsOS[0];
					var seg = para.SegmentsOS[0];
					seg.AnalysesRS.Add(wa);
					return new AnalysisOccurrence(seg, 0);
				}))
			{
				using (var handler = GetComboHandler(sandbox, InterlinLineChoices.kflidLexEntries, 0) as SandboxBase.IhMissingEntry)
				{
					var imorphItemCurrentSandboxState = handler.IndexOfCurrentItem;
					Assert.That(imorphItemCurrentSandboxState, Is.EqualTo(-1)); // no menu item corresponds to no allomorph.
				}
			}
		}

		[Test]
		public void LexEntriesComboHandler_IndexOfCurrentLexSenseAndMsa()
		{
			using (var sandbox = SetupSandbox(() =>
			{
				const string wff = "monomorphemicstem";
				var mockText = MakeText(wff);
				var wf = MakeWordform(wff);
				var wa = MakeAnalysis(wf);
				var options = new MakeBundleOptions();
				options.LexEntryForm = wff;
				options.MakeMorph = (mff) =>
				{
					Guid slotType = GetSlotType(mff);
					IMoMorphSynAnalysis msa;
					var entry = MakeEntry(mff.Replace("-", ""), "V:(Imperative)", slotType, out msa);
					var sense2 = MakeSense(entry, "gloss1", msa);
					return entry.LexemeFormOA;
				};
				options.MakeSense = (entry) =>
										{
											var msa = entry.MorphoSyntaxAnalysesOC.ToList()[0];
											var sense2 = MakeSense(entry, "gloss2", msa);
											return sense2;
										};
				options.MakeMsa = (sense) => { return sense.MorphoSyntaxAnalysisRA; };
				var wmb = MakeBundle(wa, options);
				var para = (IStTxtPara)mockText.ContentsOA.ParagraphsOS[0];
				var seg = para.SegmentsOS[0];
				seg.AnalysesRS.Add(wa);
				return new AnalysisOccurrence(seg, 0);
			}))
			{
				var initialAnalysisStack = sandbox.CurrentAnalysisTree;
				using (var handler = GetComboHandler(sandbox, InterlinLineChoices.kflidLexEntries, 0) as SandboxBase.IhMissingEntry)
				{
					var imorphItemCurrentSandboxState = handler.IndexOfCurrentItem;
					Assert.That(imorphItemCurrentSandboxState, Is.GreaterThan(-1));
					var items = handler.MorphItems;
					var miCurrentSandboxState = items[imorphItemCurrentSandboxState];
					Assert.That(miCurrentSandboxState.m_hvoMorph,
								Is.EqualTo(initialAnalysisStack.WfiAnalysis.MorphBundlesOS[0].MorphRA.Hvo));
					Assert.That(miCurrentSandboxState.m_hvoSense,
								Is.EqualTo(initialAnalysisStack.WfiAnalysis.MorphBundlesOS[0].SenseRA.Hvo));
					Assert.That(miCurrentSandboxState.m_hvoMsa,
								Is.EqualTo(initialAnalysisStack.WfiAnalysis.MorphBundlesOS[0].MsaRA.Hvo));
					Assert.That(miCurrentSandboxState.m_hvoMainEntryOfVariant, Is.EqualTo(0));

				}
			}
		}

		[Test]
		public void LexEntriesComboHandler_IndexOfCurrentInflVariant_NoInflTypeChosen()
		{
			using (var sandbox = SetupSandbox(() =>
			{
				const string wff = "variantEntry";
				var mockText = MakeText(wff);
				var wf = MakeWordform(wff);
				var wa = MakeAnalysis(wf);
				var options = new MakeBundleOptions();
				options.LexEntryForm = wff;
				options.MakeMorph = (mff) =>
				{
					Guid slotType = GetSlotType(mff);
					IMoMorphSynAnalysis msa1;
					const string mainEntryForm = "mainEntry";
					var mainEntry = MakeEntry(mainEntryForm.Replace("-", ""), "V:(Imperative)", slotType, out msa1);
					var mainSense = MakeSense(mainEntry, "mainGloss", msa1);

					IMoMorphSynAnalysis msa2;
					var variantEntry = MakeEntry(mff.Replace("-", ""), "V:(Imperative)", slotType, out msa2);
					var letPlural =
						Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(
							LexEntryTypeTags.kguidLexTypPluralVar);
					letPlural.GlossAppend.set_String(Cache.DefaultAnalWs, "pl");
					variantEntry.MakeVariantOf(mainSense, letPlural);
					return variantEntry.LexemeFormOA;
				};
				options.MakeSense = (entry) =>
										{
											var entryRef = entry.EntryRefsOS.First();
											return entryRef.ComponentLexemesRS.First() as ILexSense;
										};
				options.MakeMsa = (sense) => { return sense.MorphoSyntaxAnalysisRA; };
				var wmb = MakeBundle(wa, options);
				var para = (IStTxtPara)mockText.ContentsOA.ParagraphsOS[0];
				var seg = para.SegmentsOS[0];
				seg.AnalysesRS.Add(wa);
				return new AnalysisOccurrence(seg, 0);
			}))
			{
				var initialAnalysisStack = sandbox.CurrentAnalysisTree;
				using (var handler = GetComboHandler(sandbox, InterlinLineChoices.kflidLexEntries, 0) as SandboxBase.IhMissingEntry)
				{
					var imorphItemCurrentSandboxState = handler.IndexOfCurrentItem;

					Assert.That(imorphItemCurrentSandboxState, Is.EqualTo(-1));

					/*
					var items = handler.MorphItems;
					var miCurrentSandboxState = items[imorphItemCurrentSandboxState];
					// WfiMorphBundle.InflType hasn't been set yet, so we shouldn't return a value;
					Assert.That(miCurrentSandboxState.m_inflType, Is.Null);
					Assert.That(miCurrentSandboxState.m_hvoMorph,
								Is.EqualTo(initialAnalysisStack.WfiAnalysis.MorphBundlesOS[0].MorphRA.Hvo));
					Assert.That(miCurrentSandboxState.m_hvoSense,
								Is.EqualTo(initialAnalysisStack.WfiAnalysis.MorphBundlesOS[0].SenseRA.Hvo));
					Assert.That(miCurrentSandboxState.m_hvoMsa,
								Is.EqualTo(initialAnalysisStack.WfiAnalysis.MorphBundlesOS[0].MsaRA.Hvo));


					Assert.That(miCurrentSandboxState.m_hvoMainEntryOfVariant, Is.EqualTo(initialAnalysisStack.WfiAnalysis.MorphBundlesOS[0].SenseRA.Entry.Hvo));
					var variantEntry = initialAnalysisStack.WfiAnalysis.MorphBundlesOS[0].MorphRA.Owner as ILexEntry;
					var variantType = variantEntry.VariantEntryRefs.First().VariantEntryTypesRS.First();
					 */
				}
			}
		}

		[Test]
		public void LexEntriesComboHandler_ItemsInComboForInflVariant()
		{
			using (var sandbox = SetupSandbox(() =>
			{
				const string wff = "blonde";
				var mockText = MakeText(wff);
				var wf = MakeWordform(wff);
				var wa = MakeAnalysis(wf);
				var options = new MakeBundleOptions();
				options.LexEntryForm = wff;
				options.MakeMorph = (mff) =>
				{
					Guid slotType = GetSlotType(mff);
					IMoMorphSynAnalysis msa1;
					var mainEntry = MakeEntry("blondEntry", "", slotType, out msa1);
					var mainSense = MakeSense(mainEntry, "fair haired", msa1);

					IMoMorphSynAnalysis msa2;
					var variantEntry = MakeEntry("blondeEntry", "", slotType, out msa2);
					ILexEntryType letDialectalVariantType = Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypDialectalVar);
					letDialectalVariantType.Abbreviation.set_String(Cache.DefaultAnalWs, "dial.var. of");
					letDialectalVariantType.Name.set_String(Cache.DefaultAnalWs, "Dialectal Variant");
					letDialectalVariantType.ReverseAbbr.set_String(Cache.DefaultAnalWs, "dial.var.");

					variantEntry.MakeVariantOf(mainSense, letDialectalVariantType);
					return variantEntry.LexemeFormOA;
				};
				options.MakeSense = (entry) =>
				{
					var entryRef = entry.EntryRefsOS.First();
					return entryRef.ComponentLexemesRS.First() as ILexSense;
				};
				options.MakeMsa = (sense) => { return sense.MorphoSyntaxAnalysisRA; };
				var wmb = MakeBundle(wa, options);
				var para = (IStTxtPara)mockText.ContentsOA.ParagraphsOS[0];
				var seg = para.SegmentsOS[0];
				seg.AnalysesRS.Add(wa);
				return new AnalysisOccurrence(seg, 0);
			}))
			{
				var initialAnalysisStack = sandbox.CurrentAnalysisTree;
				using (var handler = GetComboHandler(sandbox, InterlinLineChoices.kflidLexEntries, 0) as SandboxBase.IhMissingEntry)
				{
					var imorphItemCurrentSandboxState = handler.IndexOfCurrentItem;

					Assert.That(imorphItemCurrentSandboxState, Is.EqualTo(1));

					var handlerList =  handler.ComboList.Items;

					Assert.That(handlerList[0].ToString(), Is.EqualTo("Add New Sense for blondeEntry ..."));
					Assert.That(handlerList[1].ToString(), Is.EqualTo("  fair haired, ??? , blondEntry+dial.var."));
					Assert.That(handlerList[2].ToString(), Is.EqualTo("    Add New Sense..."));
				}
			}
		}

		[Test]
		public void LexEntriesComboHandler_IndexOfCurrentInflVariant_InflTypeChosen()
		{
			using (var sandbox = SetupSandbox(() =>
			{
				const string wff = "variantEntry";
				var mockText = MakeText(wff);
				var wf = MakeWordform(wff);
				var wa = MakeAnalysis(wf);
				var options = new MakeBundleOptions();
				options.LexEntryForm = wff;
				options.MakeMorph = (mff) =>
				{
					Guid slotType = GetSlotType(mff);
					IMoMorphSynAnalysis msa1;
					const string mainEntryForm = "mainEntry";
					var mainEntry = MakeEntry(mainEntryForm.Replace("-", ""), "V:(Imperative)", slotType, out msa1);
					var mainSense = MakeSense(mainEntry, "mainGloss", msa1);

					IMoMorphSynAnalysis msa2;
					var variantEntry = MakeEntry(mff.Replace("-", ""), "V:(Imperative)", slotType, out msa2);
					var letPlural =
						Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(
							LexEntryTypeTags.kguidLexTypPluralVar);
					letPlural.GlossAppend.set_String(Cache.DefaultAnalWs, "pl");
					variantEntry.MakeVariantOf(mainSense, letPlural);
					return variantEntry.LexemeFormOA;
				};
				options.MakeSense = (entry) =>
				{
					var entryRef = entry.EntryRefsOS.First();
					return entryRef.ComponentLexemesRS.First() as ILexSense;
				};
				options.MakeMsa = (sense) => { return sense.MorphoSyntaxAnalysisRA; };
				options.MakeInflType = (mf) =>
										   {
											   var entry = mf.Owner as ILexEntry;
											   var entryRef = entry.EntryRefsOS.First();
											   var vet = entryRef.VariantEntryTypesRS.First() as ILexEntryInflType;
											   return vet;
										   };
				var wmb = MakeBundle(wa, options);

				var para = (IStTxtPara)mockText.ContentsOA.ParagraphsOS[0];
				var seg = para.SegmentsOS[0];
				seg.AnalysesRS.Add(wa);
				return new AnalysisOccurrence(seg, 0);
			}))
			{
				var initialAnalysisStack = sandbox.CurrentAnalysisTree;
				using (var handler = GetComboHandler(sandbox, InterlinLineChoices.kflidLexEntries, 0) as SandboxBase.IhMissingEntry)
				{
					var imorphItemCurrentSandboxState = handler.IndexOfCurrentItem;
					var items = handler.MorphItems;
					var miCurrentSandboxState = items[imorphItemCurrentSandboxState];
					Assert.That(miCurrentSandboxState.m_hvoMorph,
								Is.EqualTo(initialAnalysisStack.WfiAnalysis.MorphBundlesOS[0].MorphRA.Hvo));
					Assert.That(miCurrentSandboxState.m_hvoSense,
								Is.EqualTo(initialAnalysisStack.WfiAnalysis.MorphBundlesOS[0].SenseRA.Hvo));
					Assert.That(miCurrentSandboxState.m_hvoMsa,
								Is.EqualTo(initialAnalysisStack.WfiAnalysis.MorphBundlesOS[0].MsaRA.Hvo));

					var variantEntry = initialAnalysisStack.WfiAnalysis.MorphBundlesOS[0].MorphRA.Owner as ILexEntry;
					var variantType = variantEntry.VariantEntryRefs.First().VariantEntryTypesRS.First();
					Assert.That(miCurrentSandboxState.m_inflType, Is.EqualTo(variantType));
					Assert.That(miCurrentSandboxState.m_hvoMainEntryOfVariant, Is.EqualTo(initialAnalysisStack.WfiAnalysis.MorphBundlesOS[0].SenseRA.Entry.Hvo));
				}
			}
		}

		[Test]
		public void LexEntriesComboHandler_InflTypeOptions_GlossAppendSenseNames_Ordered_PL_PST()
		{
			using (var sandbox = SetupSandbox(() =>
			{
				const string wff = "variantEntry";
				var mockText = MakeText(wff);
				var wf = MakeWordform(wff);
				var wa = MakeAnalysis(wf);
				var options = new MakeBundleOptions();
				options.LexEntryForm = wff;
				options.MakeMorph = (mff) =>
				{
					Guid slotType = GetSlotType(mff);
					IMoMorphSynAnalysis msa1;
					const string mainEntryForm = "mainEntry";
					var mainEntry = MakeEntry(mainEntryForm.Replace("-", ""), "V:(Imperative)", slotType, out msa1);
					var mainSense = MakeSense(mainEntry, "mainGloss", msa1);

					IMoMorphSynAnalysis msa2;
					var variantEntry = MakeEntry(mff.Replace("-", ""), "V:(Imperative)", slotType, out msa2);
					var letPlural =
						Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(
							LexEntryTypeTags.kguidLexTypPluralVar);
					// add types to entryRef in the order of "pl" followed by "pst"
					letPlural.GlossAppend.set_String(Cache.DefaultAnalWs, "pl");
					var ler = variantEntry.MakeVariantOf(mainSense, letPlural);
					var letPst =
						Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(
							LexEntryTypeTags.kguidLexTypPastVar);
					letPst.GlossAppend.set_String(Cache.DefaultAnalWs, "pst");
					ler.VariantEntryTypesRS.Add(letPst);
					return variantEntry.LexemeFormOA;
				};
				options.MakeSense = (entry) =>
				{
					var entryRef = entry.EntryRefsOS.First();
					return entryRef.ComponentLexemesRS.First() as ILexSense;
				};
				options.MakeMsa = (sense) => { return sense.MorphoSyntaxAnalysisRA; };
				options.MakeInflType = (mf) =>
				{
					var entry = mf.Owner as ILexEntry;
					var entryRef = entry.EntryRefsOS.First();
					var vet = entryRef.VariantEntryTypesRS.First() as ILexEntryInflType;
					return vet;
				};
				var wmb = MakeBundle(wa, options);

				var para = (IStTxtPara)mockText.ContentsOA.ParagraphsOS[0];
				var seg = para.SegmentsOS[0];
				seg.AnalysesRS.Add(wa);
				return new AnalysisOccurrence(seg, 0);
			}))
			{

				var letPlural =
					Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(
						LexEntryTypeTags.kguidLexTypPluralVar);
				var letPst =
					Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(
						LexEntryTypeTags.kguidLexTypPastVar);

				using (var handler = GetComboHandler(sandbox, InterlinLineChoices.kflidLexEntries, 0) as SandboxBase.IhMissingEntry)
				{
					var sortedMorphItems = handler.MorphItems;
					sortedMorphItems.Sort();
					// menu items should be ordered according to entryRef.VariantEntryTypesRS
					{
						var mi1 = sortedMorphItems[0];
						Assert.That(mi1.m_inflType, Is.EqualTo(letPlural));
						Assert.That(mi1.m_nameSense, Is.EqualTo("mainGloss.pl"));
					}
					{
						var mi2 = sortedMorphItems[1];
						Assert.That(mi2.m_inflType, Is.EqualTo(letPst));
						Assert.That(mi2.m_nameSense, Is.EqualTo("mainGloss.pst"));
					}
				}
			}
		}

		[Test]
		public void LexEntriesComboHandler_InflTypeOptions_GlossAppendSenseNames_Ordered_PST_PL()
		{
			using (var sandbox = SetupSandbox(() =>
			{
				const string wff = "variantEntry";
				var mockText = MakeText(wff);
				var wf = MakeWordform(wff);
				var wa = MakeAnalysis(wf);
				var options = new MakeBundleOptions();
				options.LexEntryForm = wff;
				options.MakeMorph = (mff) =>
				{
					Guid slotType = GetSlotType(mff);
					IMoMorphSynAnalysis msa1;
					const string mainEntryForm = "mainEntry";
					var mainEntry = MakeEntry(mainEntryForm.Replace("-", ""), "V:(Imperative)", slotType, out msa1);
					var mainSense = MakeSense(mainEntry, "mainGloss", msa1);

					IMoMorphSynAnalysis msa2;
					var variantEntry = MakeEntry(mff.Replace("-", ""), "V:(Imperative)", slotType, out msa2);
					var letPlural =
						Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(
							LexEntryTypeTags.kguidLexTypPluralVar);
					letPlural.GlossAppend.set_String(Cache.DefaultAnalWs, "pl");
					var letPst =
						Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(
							LexEntryTypeTags.kguidLexTypPastVar);
					letPst.GlossAppend.set_String(Cache.DefaultAnalWs, "pst");
					// add types to entryRef in the order of "pst" followed by "pl"
					var ler = variantEntry.MakeVariantOf(mainSense, letPst);
					ler.VariantEntryTypesRS.Add(letPlural);
					return variantEntry.LexemeFormOA;
				};
				options.MakeSense = (entry) =>
				{
					var entryRef = entry.EntryRefsOS.First();
					return entryRef.ComponentLexemesRS.First() as ILexSense;
				};
				options.MakeMsa = (sense) => { return sense.MorphoSyntaxAnalysisRA; };
				options.MakeInflType = (mf) =>
				{
					var entry = mf.Owner as ILexEntry;
					var entryRef = entry.EntryRefsOS.First();
					var vet = entryRef.VariantEntryTypesRS.First() as ILexEntryInflType;
					return vet;
				};
				var wmb = MakeBundle(wa, options);

				var para = (IStTxtPara)mockText.ContentsOA.ParagraphsOS[0];
				var seg = para.SegmentsOS[0];
				seg.AnalysesRS.Add(wa);
				return new AnalysisOccurrence(seg, 0);
			}))
			{

				var letPlural =
					Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(
						LexEntryTypeTags.kguidLexTypPluralVar);
				var letPst =
					Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(
						LexEntryTypeTags.kguidLexTypPastVar);

				using (var handler = GetComboHandler(sandbox, InterlinLineChoices.kflidLexEntries, 0) as SandboxBase.IhMissingEntry)
				{
					var sortedMorphItems = handler.MorphItems;
					sortedMorphItems.Sort();
					// menu items should be ordered according to entryRef.VariantEntryTypesRS
					{
						var mi0 = sortedMorphItems[0];
						Assert.That(mi0.m_inflType, Is.EqualTo(letPst));
						Assert.That(mi0.m_nameSense, Is.EqualTo("mainGloss.pst"));
					}
					{
						var mi1 = sortedMorphItems[1];
						Assert.That(mi1.m_inflType, Is.EqualTo(letPlural));
						Assert.That(mi1.m_nameSense, Is.EqualTo("mainGloss.pl"));
					}
				}
			}
		}


		private SandboxBase.InterlinComboHandler GetComboHandler(SandboxBase sandbox, int flid, int morphIndex)
		{
			// first select the proper pull down icon.
			int tagIcon = 0;
			switch (flid)
			{
				default:
					break;
				case InterlinLineChoices.kflidLexEntries:
					tagIcon = SandboxBase.ktagMorphEntryIcon;
					break;
				case InterlinLineChoices.kflidWordGloss:
					tagIcon = SandboxBase.ktagWordGlossIcon;
					break;
				case InterlinLineChoices.kflidWordPos:
					tagIcon = SandboxBase.ktagWordPosIcon;
					break;
			}
			return SandboxBase.InterlinComboHandler.MakeCombo(null, tagIcon, sandbox, morphIndex) as SandboxBase.InterlinComboHandler;
		}

		/// <summary>
		/// Make all the stuff we need to display Nihimbilira in the standard way.
		/// </summary>
		private SandboxBase SetupNihimbilira()
		{
			return SetupSandbox(MakeDataForNihimbilira);
		}

		private SandboxBase SetupSandbox(Func<AnalysisOccurrence> createDataForSandbox)
		{
			var occurrence = createDataForSandbox();
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
			//Cache.LangProject.TextsOC.Add(text);
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
			return MakeBundleDefault(wa, form, gloss, pos);
		}

		private IWfiMorphBundle MakeBundleDefault(IWfiAnalysis wa, string form, string gloss, string pos)
		{
			var options = new MakeBundleOptions();
			options.LexEntryForm = form;
			options.MakeMorph = (mff) =>
									{
										Guid slotType = GetSlotType(mff);
										IMoMorphSynAnalysis msa;
										var entry = MakeEntry(mff.Replace("-", ""), pos, slotType, out msa);
										return entry.LexemeFormOA;
									};
			options.MakeSense = (entry) =>
									{
										var msa = entry.MorphoSyntaxAnalysesOC.ToList()[0];
										var sense = MakeSense(entry, gloss, msa);
										return sense;
									};
			options.MakeMsa = (sense) => { return sense.MorphoSyntaxAnalysisRA; };
			return MakeBundle(wa, options);
		}

		private Guid GetSlotType(string form)
		{
			var slotType = MoMorphTypeTags.kguidMorphStem;
			if (form.StartsWith("-"))
				slotType = MoMorphTypeTags.kguidMorphSuffix;
			else if (form.EndsWith("-"))
				slotType = MoMorphTypeTags.kguidMorphPrefix;
			return slotType;
		}


		class MakeBundleOptions
		{
			internal string LexEntryForm;
			internal Func<string, IMoForm> MakeMorph;
			internal Func<ILexEntry, ILexSense> MakeSense;
			internal Func<ILexSense, IMoMorphSynAnalysis> MakeMsa;
			internal Func<IMoForm, ILexEntryInflType> MakeInflType;
		}

		private IWfiMorphBundle MakeBundle(IWfiAnalysis wa, MakeBundleOptions mbOptions)
		{
			var bundle = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
			wa.MorphBundlesOS.Add(bundle);
			var morph = mbOptions.MakeMorph(mbOptions.LexEntryForm);
			bundle.MorphRA = morph;
			var sense = mbOptions.MakeSense(morph.Owner as ILexEntry);
			bundle.SenseRA = sense;
			var msa = mbOptions.MakeMsa(sense);
			bundle.MsaRA = msa;
			if (mbOptions.MakeInflType != null)
			{
				var inflType = mbOptions.MakeInflType(morph);
				bundle.InflTypeRA = inflType;
			}

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
		private ILexEntry MakeEntry(string lf, string pos, Guid slotType, out IMoMorphSynAnalysis msa)
		{
			// The entry itself.
			ILexEntry entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			// Lexeme Form and MSA.
			IMoForm form;
			msa = GetMsaAndMoForm(entry, slotType, pos, out form);
			entry.LexemeFormOA = form;
			form.Form.VernacularDefaultWritingSystem = MakeVernString(lf);
			form.MorphTypeRA = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(slotType);
			return entry;
		}

		private ILexSense MakeSense(ILexEntry entry, string gloss, IMoMorphSynAnalysis msa)
		{
			// Bare bones of Sense
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem = MakeAnalysisString(gloss);
			sense.MorphoSyntaxAnalysisRA = msa;
			return sense;
		}

		private IMoMorphSynAnalysis GetMsaAndMoForm(ILexEntry entry, Guid slotType, string pos, out IMoForm form)
		{
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
			return msa;
		}
	}
}
