// Copyright (c) 2014-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using XCore;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.XWorks
{
	public partial class ConfiguredXHTMLGeneratorTests
	{
		private static void DeleteTempXhtmlAndCssFiles(string xhtmlPath)
		{
			if (string.IsNullOrEmpty(xhtmlPath))
				return;
			File.Delete(xhtmlPath);
			File.Delete(Path.ChangeExtension(xhtmlPath, "css"));
			var xhtmlDir = Path.GetDirectoryName(xhtmlPath);
			if (string.IsNullOrEmpty(xhtmlDir))
				return;
			File.Delete(Path.Combine(xhtmlDir, "ProjectDictionaryOverrides.css"));
			File.Delete(Path.Combine(xhtmlDir, "ProjectReversalOverrides.css"));
		}

		/// <summary>
		/// Creates a DictionaryConfigurationModel with one Main and one of each neeeded Minor Entry nodes, all with enabled HeadWord children
		/// </summary>
		internal static DictionaryConfigurationModel CreateInterestingConfigurationModel(LcmCache cache, PropertyTable propertyTable = null,
			DictionaryConfigurationModel.ConfigType configType = DictionaryConfigurationModel.ConfigType.Root)
		{
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				CSSClassNameOverride = "entry",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				Before = "MainEntry: ",
			};
			var subEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode },
				FieldDescription = "Subentries"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode },
				FieldDescription = "LexEntry"
			};
			if (configType == DictionaryConfigurationModel.ConfigType.Hybrid || configType == DictionaryConfigurationModel.ConfigType.Root)
				mainEntryNode.Children.Add(subEntryNode);
			if (configType == DictionaryConfigurationModel.ConfigType.Hybrid || configType == DictionaryConfigurationModel.ConfigType.Lexeme)
				mainEntryNode.DictionaryNodeOptions = GetFullyEnabledListOptions(cache, DictionaryNodeListOptions.ListIds.Complex);

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var minorEntryNode = mainEntryNode.DeepCloneUnderSameParent();
			minorEntryNode.CSSClassNameOverride = "minorentry";
			minorEntryNode.Before = "MinorEntry: ";
			minorEntryNode.DictionaryNodeOptions = GetFullyEnabledListOptions(cache, DictionaryNodeListOptions.ListIds.Complex);

			var minorSecondNode = minorEntryNode.DeepCloneUnderSameParent();
			minorSecondNode.Before = "HalfStep: ";
			minorSecondNode.DictionaryNodeOptions = GetFullyEnabledListOptions(cache, DictionaryNodeListOptions.ListIds.Variant);

			var model = new DictionaryConfigurationModel
			{
				AllPublications = true,
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode, minorEntryNode, minorSecondNode },
				FilePath = propertyTable == null ? null : Path.Combine(DictionaryConfigurationListener.GetProjectConfigurationDirectory(propertyTable),
																	"filename" + DictionaryConfigurationModel.FileExtension),
				IsRootBased = configType == DictionaryConfigurationModel.ConfigType.Root
			};

			if (configType != DictionaryConfigurationModel.ConfigType.Root)
				model.Parts.Remove(minorEntryNode);

			return model;
		}

		internal static ConfigurableDictionaryNode CreatePictureModel()
		{
			var thumbNailNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "PictureFileRA",
				CSSClassNameOverride = "picture"
			};
			var pictureNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "PicturesOfSenses",
				CSSClassNameOverride = "Pictures",
				Children = new List<ConfigurableDictionaryNode> { thumbNailNode }
			};
			var sensesNode = new ConfigurableDictionaryNode { FieldDescription = "Senses" };
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode, pictureNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			return mainEntryNode;
		}

		/// <summary>
		/// Creates an ILexEntry object, optionally with specified headword and gloss
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="headword">Optional: defaults to 'Citation'</param>
		/// <param name="gloss">Optional: defaults to 'gloss'</param>
		/// <param name="definition">Optional: default is to omit</param>
		/// <returns></returns>
		internal static ILexEntry CreateInterestingLexEntry(LcmCache cache, string headword = "Citation", string gloss = "gloss", string definition = null)
		{
			var entryFactory = cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var entry = entryFactory.Create();
			var wsEn = EnsureWritingSystemSetup(cache, "en", false);
			var wsFr = EnsureWritingSystemSetup(cache, "fr", true);
			AddHeadwordToEntry(entry, headword, wsFr);
			entry.Comment.set_String(wsEn, TsStringUtils.MakeString("Comment", wsEn));
			AddSenseToEntry(entry, gloss, wsEn, cache, definition);
			return entry;
		}

		internal static int EnsureWritingSystemSetup(LcmCache cache, string wsStr, bool isVernacular)
		{
			var wsFact = cache.WritingSystemFactory;
			var result = wsFact.GetWsFromStr(wsStr);
			if (result < 1)
			{
				if (isVernacular)
				{
					cache.LangProject.AddToCurrentVernacularWritingSystems(cache.WritingSystemFactory.get_Engine(wsStr) as CoreWritingSystemDefinition);
				}
				else
				{
					cache.LangProject.AddToCurrentAnalysisWritingSystems(cache.WritingSystemFactory.get_Engine(wsStr) as CoreWritingSystemDefinition);
				}
			}
			return wsFact.GetWsFromStr(wsStr);
		}

		/// <summary>
		/// Creates an ILexEntry object, optionally with specified headword and gloss
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="headword">Optional: defaults to 'Citation'</param>
		/// <param name="gloss">Optional: defaults to 'gloss'</param>
		/// <returns></returns>
		internal static ILexEntry CreateInterestingSuffix(LcmCache cache, string headword = "ba", string gloss = "gloss")
		{
			var entry = CreateInterestingLexEntry(cache, headword, gloss);
			var wsEn = cache.WritingSystemFactory.GetWsFromStr("en");
			var suffixType = cache.LangProject.LexDbOA.MorphTypesOA.FindOrCreatePossibility("suffix", wsEn);
			entry.LexemeFormOA = cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			entry.LexemeFormOA.MorphTypeRA = suffixType as IMoMorphType;
			return entry;
		}

		internal sealed class TempGuidOn<T> : IDisposable where T : ICmObject
		{
			public T Item { get; private set; }
			private readonly Guid m_OriginalGuid;

			public TempGuidOn(T item, Guid tempGuid)
			{
				Item = item;
				m_OriginalGuid = item.Guid;
				SetGuidOn(item, tempGuid);
			}

			~TempGuidOn()
			{
				Dispose(false);
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");

				if (disposing)
					SetGuidOn(Item, m_OriginalGuid);
			}

			private static void SetGuidOn(ICmObject item, Guid newGuid)
			{
				var refGuidField = ReflectionHelper.GetField(item, "m_guid");
				ReflectionHelper.SetField(refGuidField, "m_guid", newGuid);
			}
		}

		/// <summary>
		/// Use reflection to set the guid on a variant form. May not work for all kinds of tests or appropriately be editing the database.
		/// Because changing the Guid causes teardown problem, it must be reset prior to teardown (hence the Disposable <returns/>)
		/// </summary>
		internal static TempGuidOn<ILexEntryRef> CreateVariantForm(LcmCache cache, IVariantComponentLexeme main, ILexEntry variantForm, Guid guid,
			string type = TestVariantName)
		{
			return new TempGuidOn<ILexEntryRef>(CreateVariantForm(cache, main, variantForm, type), guid);
		}

		/// <summary>
		/// 'internal static' so Reversal tests can use it
		/// </summary>
		internal static ILexEntryRef CreateVariantForm(LcmCache cache, IVariantComponentLexeme main, ILexEntry variantForm, string type = TestVariantName)
		{
			var owningList = cache.LangProject.LexDbOA.VariantEntryTypesOA;
			Assert.That(owningList, Is.Not.Null, "No VariantEntryTypes property on Lexicon object.");
			var varType = owningList.ReallyReallyAllPossibilities.LastOrDefault(poss => poss.Name.AnalysisDefaultWritingSystem.Text == type) as ILexEntryType;
			if (varType == null && type != null) // if this type doesn't exist, create it
			{
				varType = cache.ServiceLocator.GetInstance<ILexEntryTypeFactory>().Create();
				owningList.PossibilitiesOS.Add(varType);
				varType.Name.set_String(cache.DefaultAnalWs, type);
			}
			var refOut = variantForm.MakeVariantOf(main, varType);
			// ILexEntry.MakeVariantOf sets a Type even if null is specified. But we want to test typeless variants, so clear them if null is specified.
			if (type == null)
				refOut.VariantEntryTypesRS.Clear();
			return refOut;
		}

		/// <summary>
		/// Use reflection to set the guid on a complex form. May not work for all kinds of tests or appropriately be editing the database.
		/// Because changing the Guid causes teardown problem, it must be reset prior to teardown (hence the Disposable <returns/>)
		/// </summary>
		internal static TempGuidOn<ILexEntryRef> CreateComplexForm(LcmCache cache, IVariantComponentLexeme main, ILexEntry complexForm, Guid guid,
			bool subentry)
		{
			return new TempGuidOn<ILexEntryRef>(CreateComplexForm(cache, main, complexForm, subentry), guid);
		}

		internal static ILexEntryRef CreateComplexForm(LcmCache cache, ICmObject main, ILexEntry complexForm, bool subentry, byte complexFormTypeIndex = 1)
		{
			return CreateComplexForm(cache, main, complexForm, subentry,
				(ILexEntryType)cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS[complexFormTypeIndex]);
		}

		private static ILexEntryRef CreateComplexForm(LcmCache cache, ICmObject main, ILexEntry complexForm, bool subentry, Guid typeGuid)
		{
			return CreateComplexForm(cache, main, complexForm, subentry,
				(ILexEntryType)cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.First(x => x.Guid == typeGuid));
		}

		private static ILexEntryRef CreateComplexForm(LcmCache cache, ICmObject main, ILexEntry complexForm, bool subentry, ILexEntryType complexEntryType)
		{
			var complexEntryRef = cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			complexForm.EntryRefsOS.Add(complexEntryRef);
			var complexEntryTypeAbbrText = complexEntryType.Abbreviation.BestAnalysisAlternative.Text;
			var complexEntryTypeRevAbbr = complexEntryType.ReverseAbbr;
			// If there is no reverseAbbr, generate one from the forward abbr (e.g. "comp. of") by trimming the trailing " of"
			if (complexEntryTypeRevAbbr.BestAnalysisAlternative.Equals(complexEntryTypeRevAbbr.NotFoundTss))
				complexEntryTypeRevAbbr.SetAnalysisDefaultWritingSystem(complexEntryTypeAbbrText.Substring(0, complexEntryTypeAbbrText.Length - 3));
			complexEntryRef.ComplexEntryTypesRS.Add(complexEntryType);
			complexEntryRef.RefType = LexEntryRefTags.krtComplexForm;
			complexEntryRef.ComponentLexemesRS.Add(main);
			if (subentry)
				complexEntryRef.PrimaryLexemesRS.Add(main);
			else
				complexEntryRef.ShowComplexFormsInRS.Add(main);
			return complexEntryRef;
		}

		/// <summary>
		/// Generates a Lexical Reference.
		/// If refTypeReverseName is specified, generates a Ref of an Asymmetric Type (EntryOrSenseTree) with the specified reverse name;
		/// otherwise, generates a Ref of a Symmetric Type (EntryOrSenseSequence).
		/// </summary>
		internal static void CreateLexicalReference(LcmCache cache, ICmObject mainEntry, ICmObject referencedForm, string refTypeName, string refTypeReverseName = null)
		{
			CreateLexicalReference(cache, mainEntry, referencedForm, null, refTypeName, refTypeReverseName);
		}

		private static void CreateLexicalReference(LcmCache cache, ICmObject firstEntry, ICmObject secondEntry, ICmObject thirdEntry, string refTypeName, string refTypeReverseName = null)
		{
			var lrt = CreateLexRefType(cache, LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence, refTypeName, "", refTypeReverseName, "");
			if (!string.IsNullOrEmpty(refTypeReverseName))
			{
				lrt.ReverseName.set_String(cache.DefaultAnalWs, refTypeReverseName);
				lrt.MappingType = (int)MappingTypes.kmtEntryOrSenseTree;
			}
			var lexRef = cache.ServiceLocator.GetInstance<ILexReferenceFactory>().Create();
			lrt.MembersOC.Add(lexRef);
			lexRef.TargetsRS.Add(firstEntry);
			lexRef.TargetsRS.Add(secondEntry);
			if (thirdEntry != null)
				lexRef.TargetsRS.Add(thirdEntry);
		}

		private static ILexRefType CreateLexRefType(LcmCache cache, LexRefTypeTags.MappingTypes type, string name, string abbr, string revName, string revAbbr)
		{
			if (cache.LangProject.LexDbOA.ReferencesOA == null)
			{
				cache.LangProject.LexDbOA.ReferencesOA = cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			}
			var referencePossibilities = cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS;
			if (referencePossibilities.Any(r => r.Name.BestAnalysisAlternative.Text == name))
			{
				return referencePossibilities.First(r => r.Name.BestAnalysisAlternative.Text == name) as ILexRefType;
			}
			var lrt = cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create();
			referencePossibilities.Add(lrt);
			lrt.MappingType = (int)type;
			lrt.Name.set_String(cache.DefaultAnalWs, name);
			lrt.Abbreviation.set_String(cache.DefaultAnalWs, abbr);
			if (!string.IsNullOrEmpty(revName))
				lrt.ReverseName.set_String(cache.DefaultAnalWs, revName);
			if (!string.IsNullOrEmpty(revAbbr))
				lrt.ReverseAbbreviation.set_String(cache.DefaultAnalWs, revAbbr);
			return lrt;
		}

		private void CreateLexReference(ILexRefType lrt, IEnumerable<ICmObject> sensesAndEntries)
		{
			CreateLexReference(lrt, sensesAndEntries, Guid.Empty);
		}

		private void CreateLexReference(ILexRefType lrt, IEnumerable<ICmObject> sensesAndEntries, Guid lexRefGuid)
		{
			var lexRef = Cache.ServiceLocator.GetInstance<ILexReferenceFactory>().Create(lexRefGuid, lrt);
			foreach (var senseOrEntry in sensesAndEntries)
				lexRef.TargetsRS.Add(senseOrEntry);
		}

		internal static ICmPossibility CreatePublicationType(string name, LcmCache cache)
		{
			return DictionaryConfigurationImportController.AddPublicationType(name, cache);
		}

		internal static void AddHeadwordToEntry(ILexEntry entry, string headword, int wsId)
		{
			// The headword field is special: it uses Citation if available, or LexemeForm if Citation isn't filled in
			entry.CitationForm.set_String(wsId, TsStringUtils.MakeString(headword, wsId));
		}

		internal static void AddLexemeFormToEntry(ILexEntry entry, string lexemeForm, LcmCache cache)
		{
			entry.LexemeFormOA = cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			entry.LexemeFormOA.Form.SetVernacularDefaultWritingSystem(lexemeForm);
		}

		internal static ILexPronunciation AddPronunciationToEntry(ILexEntry entry, string content, int wsId, LcmCache cache)
		{
			var pronunciation = cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
			entry.PronunciationsOS.Add(pronunciation);
			pronunciation.Form.set_String(wsId, TsStringUtils.MakeString(content, wsId));
			return pronunciation;
		}

		internal static void AddSenseToEntry(ILexEntry entry, string gloss, int wsId, LcmCache cache, string definition = null)
		{
			var senseFactory = cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			var sense = senseFactory.Create();
			entry.SensesOS.Add(sense);
			if (!string.IsNullOrEmpty(gloss))
			{
				sense.Gloss.set_String(wsId, TsStringUtils.MakeString(gloss, wsId));
			}

			if (!string.IsNullOrEmpty(definition))
			{
				sense.Definition.set_String(wsId, TsStringUtils.MakeString(definition, wsId));
			}
		}

		private void AddSenseAndTwoSubsensesToEntry(ICmObject entryOrSense, string gloss)
		{
			AddSenseAndTwoSubsensesToEntry(entryOrSense, gloss, Cache, m_wsEn);
		}

		internal static void AddSenseAndTwoSubsensesToEntry(ICmObject entryOrSense, string gloss, LcmCache cache, int ws)
		{
			var senseFactory = cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			var sense = senseFactory.Create();
			var entry = entryOrSense as ILexEntry;
			if (entry != null)
				entry.SensesOS.Add(sense);
			else
				((ILexSense)entryOrSense).SensesOS.Add(sense);
			sense.Gloss.set_String(ws, TsStringUtils.MakeString(gloss, ws));
			var subSensesOne = senseFactory.Create();
			sense.SensesOS.Add(subSensesOne);
			subSensesOne.Gloss.set_String(ws, TsStringUtils.MakeString(gloss + "2.1", ws));
			var subSensesTwo = senseFactory.Create();
			sense.SensesOS.Add(subSensesTwo);
			subSensesTwo.Gloss.set_String(ws, TsStringUtils.MakeString(gloss + "2.2", ws));
		}

		private void AddSingleSubSenseToSense(string gloss, ILexSense sense)
		{
			sense.Gloss.set_String(m_wsEn, TsStringUtils.MakeString(gloss, m_wsEn));
			AddSubSenseToSense(gloss + "1.1", sense);
		}

		private void AddSubSenseToSense(string gloss, ILexSense sense)
		{
			var subSensesOne = sense.Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			sense.SensesOS.Add(subSensesOne);
			subSensesOne.Gloss.set_String(m_wsEn, TsStringUtils.MakeString(gloss, m_wsEn));
		}

		internal static ILexExampleSentence AddExampleToSense(ILexSense sense, string content, LcmCache cache, int vern, int analy, string translation = null)
		{
			var exampleFact = cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>();
			var example = exampleFact.Create(new Guid(), sense);
			example.Example.set_String(vern, TsStringUtils.MakeString(content, vern));
			if (translation != null)
			{
				var type = cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranFreeTranslation);
				var cmTranslation = cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(example, type);
				cmTranslation.Translation.set_String(analy, TsStringUtils.MakeString(translation, analy));
				example.TranslationsOC.Add(cmTranslation);
			}
			return example;
		}

		private IMoForm AddAllomorphToEntry(ILexEntry entry)
		{
			var morphFact = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			var morph = morphFact.Create();
			entry.AlternateFormsOS.Add(morph);
			morph.Form.set_String(m_wsFr, TsStringUtils.MakeString("Allomorph", m_wsFr));

			// add environment to the allomorph
			const int stringRepresentationFlid = 5097008;
			var env = Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create();
			Cache.LangProject.PhonologicalDataOA.EnvironmentsOS.Add(env);
			morph.PhoneEnvRC.Add(env);
			Cache.MainCacheAccessor.SetString(env.Hvo, stringRepresentationFlid, TsStringUtils.MakeString("phoneyEnv", m_wsEn));

			return morph;
		}

		internal static ICmPicture CreatePicture(LcmCache cache, bool exists = true, string caption = "caption", string ws = "en")
		{
			var pic = cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			if (caption != null)
			{
				var wsHandle = cache.WritingSystemFactory.GetWsFromStr(ws);
				pic.Caption.set_String(wsHandle, TsStringUtils.MakeString(caption, wsHandle));
			}
			var file = cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			if (cache.LangProject.MediaOC.Any())
			{
				cache.LangProject.MediaOC.First().FilesOC.Add(file);
			}
			else
			{
				var folder = cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
				cache.LangProject.MediaOC.Add(folder);
				folder.FilesOC.Add(file);
			}
			file.InternalPath = exists
				? Path.Combine(FwDirectoryFinder.SourceDirectory, "xWorks/xWorksTests/TestData/ImageFiles/test_auth_copy_license.jpg")
				: "does/not/exist.jpg";
			pic.PictureFileRA = file;
			return pic;
		}

		internal static IStText CreateMultiParaText(string content, LcmCache cache)
		{
			var text = cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			//cache.LangProject.
			var stText = cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			cache.LangProject.InterlinearTexts.Add(stText);
			text.ContentsOA = stText;
			var para = cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			stText.ParagraphsOS.Add(para);
			para.Contents = MakeVernTss("First para " + content, cache);
			var para1 = cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			stText.ParagraphsOS.Add(para1);
			para1.Contents = MakeVernTss("Second para " + content, cache);
			return text.ContentsOA;
		}

		private static ITsString MakeVernTss(string content, LcmCache cache)
		{
			return TsStringUtils.MakeString(content, cache.DefaultVernWs);
		}

		private ITsString MakeMulitlingualTss(IEnumerable<string> content)
		{
			// automatically alternates runs between 'en' and 'fr'
			var tsFact = TsStringUtils.TsStrFactory;
			var lastWs = m_wsFr;
			var builder = tsFact.GetIncBldr();
			foreach (var runContent in content)
			{
				lastWs = lastWs == m_wsEn ? m_wsFr : m_wsEn; // switch ws for each run
				builder.AppendTsString(TsStringUtils.MakeString(runContent, lastWs));
			}
			return builder.GetString();
		}

		internal static ITsString MakeBidirectionalTss(IEnumerable<string> content, LcmCache cache)
		{
			var wsHe = EnsureHebrewExists(cache);
			var wsEn = cache.ServiceLocator.WritingSystems.AllWritingSystems
				.First(ws => ws.Id == "en").Handle;
			// automatically alternates runs between 'en' and 'he' (Hebrew)
			var tsFact = TsStringUtils.TsStrFactory;
			var lastWs = wsEn;
			var builder = tsFact.GetIncBldr();
			foreach (var runContent in content)
			{
				lastWs = lastWs == wsEn ? wsHe : wsEn; // switch ws for each run
				builder.AppendTsString(tsFact.MakeString(runContent, lastWs));
			}
			return builder.GetString();
		}

		private static int EnsureHebrewExists(LcmCache cache)
		{
			var heWs =
				cache.ServiceLocator.WritingSystems.AllWritingSystems.FirstOrDefault(ws =>
					ws.Id == "he");
			if (heWs != null)
				return heWs.Handle;
			var wsManager = cache.ServiceLocator.WritingSystemManager;
			CoreWritingSystemDefinition hebrew;
			wsManager.GetOrSet("he", out hebrew);
			hebrew.RightToLeftScript = true;
			return hebrew.Handle;
		}

		private void SetDictionaryNormalDirection(InheritableStyleProp<TriStateBool> rightToLeft)
		{
			ReflectionHelper.SetField(DictionaryNormalStyle, "m_rtl", rightToLeft);
		}

		internal static void SetPublishAsMinorEntry(ILexEntry entry, bool publish)
		{
			foreach (var ler in entry.EntryRefsOS)
				ler.HideMinorEntry = publish ? 0 : 1;
		}

		public static DictionaryNodeOptions GetSenseNodeOptions()
		{
			return new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = false };
		}

		public static DictionaryNodeOptions GetWsOptionsForLanguages(string[] languages)
		{
			return new DictionaryNodeWritingSystemOptions { Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(languages) };
		}

		public static DictionaryNodeOptions GetWsOptionsForLanguages(string[] languages, DictionaryNodeWritingSystemOptions.WritingSystemType type)
		{
			return new DictionaryNodeWritingSystemOptions
			{
				Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(languages),
				WsType = type
			};
		}

		public static DictionaryNodeOptions GetWsOptionsForLanguageswithDisplayWsAbbrev(string[] languages,
			DictionaryNodeWritingSystemOptions.WritingSystemType type = 0)
		{
			return new DictionaryNodeWritingSystemOptions
			{
				Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(languages),
				DisplayWritingSystemAbbreviations = true,
				WsType = type
			};
		}

		public static DictionaryNodeOptions GetListOptionsForItems(DictionaryNodeListOptions.ListIds listName, ICmPossibility[] checkedItems)
		{
			var listOptions = new DictionaryNodeListOptions
			{
				ListId = listName,
				Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(checkedItems.Select(id => id.Guid.ToString()).ToList())
			};
			return listOptions;
		}

		public static DictionaryNodeOptions GetListOptionsForStrings(DictionaryNodeListOptions.ListIds listName, IEnumerable<string> checkedItems)
		{
			var listOptions = new DictionaryNodeListOptions
			{
				ListId = listName,
				Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(checkedItems)
			};
			return listOptions;
		}

		public static DictionaryNodeOptions GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds listName, LcmCache cache)
		{
			return GetFullyEnabledListOptions(cache, listName);
		}

		public static DictionaryNodeOptions GetFullyEnabledListOptions(LcmCache cache, DictionaryNodeListOptions.ListIds listName)
		{
			List<DictionaryNodeListOptions.DictionaryNodeOption> dnoList;
			var useParaOptions = false;
			switch (listName)
			{
				case DictionaryNodeListOptions.ListIds.Minor:
					dnoList = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(
						new[] { XmlViewsUtils.GetGuidForUnspecifiedVariantType(), XmlViewsUtils.GetGuidForUnspecifiedComplexFormType() }
							.Select(guid => guid.ToString())
						.Union(cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS
						.Union(cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS).Select(item => item.Guid.ToString())));
					break;
				case DictionaryNodeListOptions.ListIds.Variant:
					dnoList = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(
						new[] { XmlViewsUtils.GetGuidForUnspecifiedVariantType().ToString() }
						.Union(cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.Select(item => item.Guid.ToString())));
					break;
				case DictionaryNodeListOptions.ListIds.Complex:
					useParaOptions = true;
					dnoList = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(
						new[] { XmlViewsUtils.GetGuidForUnspecifiedComplexFormType().ToString() }
						.Union(cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.Select(item => item.Guid.ToString())));
					break;
				case DictionaryNodeListOptions.ListIds.Note:
					useParaOptions = true;
					dnoList = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(
						new[] { XmlViewsUtils.GetGuidForUnspecifiedExtendedNoteType().ToString() }
						.Union(cache.LangProject.LexDbOA.ExtendedNoteTypesOA.PossibilitiesOS.Select(item => item.Guid.ToString())));
					break;
				default:
					throw new NotImplementedException(string.Format("Unknown list id {0}", listName));
			}

			DictionaryNodeListOptions listOptions = useParaOptions ? new DictionaryNodeListAndParaOptions() : new DictionaryNodeListOptions();

			listOptions.ListId = listName;
			listOptions.Options = dnoList;
			return listOptions;
		}

		/// <summary>
		/// Search haystack with regexQuery, and assert that requiredNumberOfMatches matches are found.
		/// Can be used in place of AssertThatXmlIn.String().HasSpecifiedNumberOfMatchesForXpath(),
		/// when slashes are needed in an argument to xpath starts-with.
		/// </summary>
		private static void AssertRegex(string haystack, string regexQuery, int requiredNumberOfMatches)
		{
			var regex = new Regex(regexQuery);
			var matches = regex.Matches(haystack);
			Assert.That(matches.Count, Is.EqualTo(requiredNumberOfMatches), "Unexpected number of matches");
		}

		public IPartOfSpeech CreatePartOfSpeech(string name, string abbr)
		{
			var posSeq = Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS;
			var pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos);
			pos.Name.set_String(m_wsEn, name);
			pos.Abbreviation.set_String(m_wsEn, abbr);
			return pos;
		}

		// ReSharper disable once InconsistentNaming
		public IMoMorphSynAnalysis CreateMSA(ILexEntry entry, IPartOfSpeech pos)
		{
			var msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			msa.PartOfSpeechRA = pos;
			return msa;
		}
	}

	#region Test classes and interfaces for testing the reflection code in GetPropertyTypeForConfigurationNode
	internal class TestRootClass
	{
		public ITestInterface RootMember { get; set; }
		public TestNonInterface ConcreteMember { get; set; }
	}

	internal interface ITestInterface : ITestBaseOne, ITestBaseTwo
	{
		string TestString { get; }
	}

	internal interface ITestBaseOne
	{
		IMoForm TestMoForm { get; }
	}

	internal interface ITestBaseTwo : ITestGrandParent
	{
		ICmObject TestIcmObject { get; }
	}

	internal class TestNonInterface
	{
		// ReSharper disable UnusedMember.Local // Justification: called by reflection
		private string TestNonInterfaceString { get; set; }
		// ReSharper restore UnusedMember.Local
	}

	internal interface ITestGrandParent
	{
		Stack<TestRootClass> TestCollection { get; }
	}

	internal class TestPictureClass
	{
		public ILcmList<ICmPicture> Pictures { get; set; }
	}

	internal static class TestExtensionMethod
	{
		static string Creator(this TestPictureClass extend)
		{
			return "bob";
		}
	}
	#endregion
}
