using System;
using System.Diagnostics;
using Microsoft.Practices.ServiceLocation;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// <summary>
	/// Bootstrap a new language project suitable for use by end users.
	///
	/// This will contain everything in that is included when a new FW 6.0 LP is created.
	/// </summary>
	internal static class BootstrapNewLanguageProject
	{
		/// <summary>
		/// Create a new Language Project.
		/// </summary>
		internal static void BootstrapNewSystem(IServiceLocator servLoc)
		{
			using (var nonUndoableUOW = new NonUndoableUnitOfWorkHelper(servLoc.GetInstance<IActionHandler>()))
			{
				var lp = servLoc.GetInstance<ILangProjectFactory>().Create();

				BootstrapWritingSystems(lp);

				// Add some required objects.
				SetupVariousPossibilityLists(lp);

				lp.MsFeatureSystemOA = servLoc.GetInstance<IFsFeatureSystemFactory>().Create();
				lp.PhFeatureSystemOA = servLoc.GetInstance<IFsFeatureSystemFactory>().Create();
				lp.PhonologicalDataOA = servLoc.GetInstance<IPhPhonDataFactory>().Create();
				lp.MorphologicalDataOA = servLoc.GetInstance<IMoMorphDataFactory>().Create();

				// This should possibly be in SetupVariousPossibilityLists, if we did that after making the owner.
				lp.MorphologicalDataOA.ProdRestrictOA = servLoc.GetInstance<ICmPossibilityListFactory>().Create();
				lp.MorphologicalDataOA.ProdRestrictOA.ItemClsid = CmPossibilityTags.kClassId;

				lp.PhonologicalDataOA.PhonRuleFeatsOA = servLoc.GetInstance<ICmPossibilityListFactory>().Create();
				lp.PhonologicalDataOA.PhonRuleFeatsOA.ItemClsid = PhPhonRuleFeatTags.kClassId;

				lp.ResearchNotebookOA = servLoc.GetInstance<IRnResearchNbkFactory>().Create();

				InitializeLexDb(lp);

				// Add all fixed Guid objects here.
				SetupAnnotationDefns(lp);
				SetupAgents(lp);

				// Translation Types Possibility List and KeyTerms List
				SetupTranslationTypesAndKeyTermsList(lp);

				// If there are any exceptions before this point,
				// then the whole mess gets rolled back.
				nonUndoableUOW.RollBack = false;
			}
		}

		/// <summary>
		/// Create various possibility lists and add them to the language project.
		/// </summary>
		/// <param name="lp"></param>
		private static void SetupVariousPossibilityLists(ILangProject lp)
		{
			var possListFactory = lp.Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>();

			lp.AnnotationDefsOA = possListFactory.Create();
			lp.AnnotationDefsOA.ItemClsid = CmAnnotationDefnTags.kClassId;
			lp.AnthroListOA = possListFactory.Create();
			lp.AnthroListOA.ItemClsid = CmAnthroItemTags.kClassId;
			lp.PartsOfSpeechOA = possListFactory.Create();
			lp.PartsOfSpeechOA.ItemClsid = PartOfSpeechTags.kClassId;
			lp.ConfidenceLevelsOA = possListFactory.Create();
			lp.ConfidenceLevelsOA.ItemClsid = CmPossibilityTags.kClassId;
			lp.LocationsOA = possListFactory.Create();
			lp.LocationsOA.ItemClsid = CmLocationTags.kClassId;
			lp.SemanticDomainListOA = possListFactory.Create();
			lp.SemanticDomainListOA.ItemClsid = CmSemanticDomainTags.kClassId;
		}

		/// <summary>
		/// Add the translation types with their guids to the proper list
		/// and create the KeyTerms list.
		/// </summary>
		/// <param name="lp"></param>
		private static void SetupTranslationTypesAndKeyTermsList(ILangProject lp)
		{
			var servLoc = lp.Cache.ServiceLocator;
			var dataReader = (IDataReader)servLoc.GetInstance<IDataSetup>();
			var mdcManaged = servLoc.GetInstance<IFwMetaDataCacheManaged>();
			var listFactory = servLoc.GetInstance<ICmPossibilityListFactory>() as ICmPossibilityListFactoryInternal;
			Debug.Assert(listFactory != null, "ServiceLocator has no instance of ICmPossibilityListFactory.");

			int flidTranslationTags = mdcManaged.GetFieldId("LangProject", "TranslationTags", false);

			var list = listFactory.Create(
				CmPossibilityListTags.kguidTranslationTypes,
				dataReader.GetNextRealHvo());
			lp.TranslationTagsOA = list;
			lp.TranslationTagsOA.ItemClsid = CmPossibilityTags.kClassId;

			var possFactory = servLoc.GetInstance<ICmPossibilityFactory>() as ICmPossibilityFactoryInternal;
			Debug.Assert(possFactory != null, "ServiceLocator has no instance of ICmPossibilityFactory.");

			// Back trans item.
			possFactory.Create(
				CmPossibilityTags.kguidTranBackTranslation,
				dataReader.GetNextRealHvo(),
				list,
				0);

			// Back trans item.
			possFactory.Create(
				CmPossibilityTags.kguidTranFreeTranslation,
				dataReader.GetNextRealHvo(),
				list,
				1);

			// Literal trans item.
			possFactory.Create(
				CmPossibilityTags.kguidTranLiteralTranslation,
				dataReader.GetNextRealHvo(),
				list,
				2);

			// Key terms list.
			int flidCheckLists = mdcManaged.GetFieldId("LangProject", "CheckLists", false);

			list = listFactory.Create(
				CmPossibilityListTags.kguidChkKeyTermsList,
				dataReader.GetNextRealHvo());
			list.ItemClsid = 0;		// it's not clear what this value should be -- it's 0 in Sena 3 for version 6.0 of FieldWorks
			lp.CheckListsOC.Add(list);
		}

		/// <summary>
		/// Add the agent types with their guids to the proper list.
		/// </summary>
		/// <param name="lp"></param>
		private static void SetupAgents(ILangProject lp)
		{
			var servLoc = lp.Cache.ServiceLocator;
			var dataReader = (IDataReader)servLoc.GetInstance<IDataSetup>();
			var agentFactory = servLoc.GetInstance<ICmAgentFactory>() as ICmAgentFactoryInternal;
			Debug.Assert(agentFactory != null, "ServiceLocator has no instance of ICmAgentFactory.");
			var agent = agentFactory.Create(
				CmAgentTags.kguidAgentDefUser,
				dataReader.GetNextRealHvo(),
				true,
				null);
			lp.AnalyzingAgentsOC.Add(agent);

			agent = agentFactory.Create(
				CmAgentTags.kguidAgentXAmpleParser,
				dataReader.GetNextRealHvo(),
				false,
				"Normal");
			lp.AnalyzingAgentsOC.Add(agent);

			agent = agentFactory.Create(
				CmAgentTags.kguidAgentComputer,
				dataReader.GetNextRealHvo(),
				false,
				null);
			lp.AnalyzingAgentsOC.Add(agent);
		}

		/// <summary>
		/// Add the annotation types with their guids to the propery possibility or list.
		/// TODO: these lists have other properties that need to be set at some point. Hopefully we'll
		/// just load that from NewLangProj or some other xml file.
		/// </summary>
		/// <param name="lp"></param>
		private static void SetupAnnotationDefns(ILangProject lp)
		{
			ICmPossibilityList posList = lp.AnnotationDefsOA;

			// Text Annotations
			ICmAnnotationDefn txtAnnDef = AddAnnotationDefn(posList, CmAnnotationDefnTags.kguidAnnText);

			// Notes
			ICmAnnotationDefn noteAnnDef = AddAnnotationDefn(posList, CmAnnotationDefnTags.kguidAnnNote);
			ICmAnnotationDefn noteSubDef = AddAnnotationDefn(noteAnnDef, CmAnnotationDefnTags.kguidAnnConsultantNote);
			noteSubDef.UserCanCreate = true;
			noteSubDef.Name.UserDefaultWritingSystem = TsStringUtils.MakeTss("Consultant", lp.Cache.DefaultUserWs);
			noteSubDef = AddAnnotationDefn(noteAnnDef, CmAnnotationDefnTags.kguidAnnTranslatorNote);
			noteSubDef.UserCanCreate = true;
			noteSubDef.Name.UserDefaultWritingSystem = TsStringUtils.MakeTss("Translator", lp.Cache.DefaultUserWs);

			// Others
			AddAnnotationDefn(posList, CmAnnotationDefnTags.kguidAnnComment);
			AddAnnotationDefn(posList, CmAnnotationDefnTags.kguidAnnCheckingError);
		}

		private static ICmAnnotationDefn AddAnnotationDefn(ICmPossibilityList posList, Guid guid)
		{
			var cache = posList.Cache;
			var servLoc = cache.ServiceLocator;
			var dataReader = (IDataReader)servLoc.GetInstance<IDataSetup>();
			var posFactory = servLoc.GetInstance<ICmAnnotationDefnFactory>() as ICmAnnotationDefnFactoryInternal;
			return posFactory.Create(
				guid,
				dataReader.GetNextRealHvo(),
				posList,
				posList.PossibilitiesOS.Count);
		}

		private static ICmAnnotationDefn AddAnnotationDefn(ICmPossibility owner, Guid guid)
		{
			var cache = owner.Cache;
			var servLoc = cache.ServiceLocator;
			var dataReader = (IDataReader)servLoc.GetInstance<IDataSetup>();
			var posFactory = servLoc.GetInstance<ICmAnnotationDefnFactory>() as ICmAnnotationDefnFactoryInternal;
			return posFactory.Create(
				guid,
				dataReader.GetNextRealHvo(),
				owner,
				owner.SubPossibilitiesOS.Count);
		}

		/// <summary>
		/// Create the most basic parts of the LgWritingSystem objects.
		/// This can be everything except ITsStrings,
		/// which may depend on non-existing WS definitions.
		/// A second pass will add the ITsStrings.
		/// </summary>
		private static void BootstrapWritingSystems(ILangProject lp)
		{
			WritingSystemManager wsManager = lp.Services.WritingSystemManager;
			// English WS.
			CoreWritingSystemDefinition ws;
			wsManager.GetOrSet("en", out ws);
			lp.AddToCurrentAnalysisWritingSystems(ws);

			// Spanish WS.
			wsManager.GetOrSet("es", out ws);
			// German WS.
			wsManager.GetOrSet("de", out ws);
			// French WS.
			wsManager.GetOrSet("fr", out ws);

			wsManager.Save();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize lexical database and morph types
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void InitializeLexDb(ILangProject lp)
		{
			var cache = lp.Cache;
			var servLoc = cache.ServiceLocator;
			var dataReader = (IDataReader)servLoc.GetInstance<IDataSetup>();

			var lexDb = servLoc.GetInstance<ILexDbFactory>().Create();
			lp.LexDbOA = lexDb;
			var listFactory = servLoc.GetInstance<ICmPossibilityListFactory>();
			lexDb.UsageTypesOA = listFactory.Create();
			lexDb.UsageTypesOA.ItemClsid = CmPossibilityTags.kClassId;
			lexDb.DomainTypesOA = listFactory.Create();
			lexDb.DomainTypesOA.ItemClsid = CmPossibilityTags.kClassId;
			//LangProject.StatusOA = listFactory.Create();
			//LangProject.StatusOA.ItemClsid = CmPossibilityTags.kClassId;
			lexDb.SenseTypesOA = listFactory.Create();
			lexDb.SenseTypesOA.ItemClsid = CmPossibilityTags.kClassId;

			AddMorphTypes(lexDb);

			var mdcManaged = servLoc.GetInstance<IFwMetaDataCacheManaged>();
			var listFactoryInternal = listFactory as ICmPossibilityListFactoryInternal;
			lexDb.ComplexEntryTypesOA = listFactoryInternal.Create(
				new Guid("1ee09905-63dd-4c7a-a9bd-1d496743ccd6"),
				dataReader.GetNextRealHvo());
			lexDb.ComplexEntryTypesOA.ItemClsid = LexEntryTypeTags.kClassId;
			lexDb.VariantEntryTypesOA = listFactoryInternal.Create(
				new Guid("bb372467-5230-43ef-9cc7-4d40b053fb94"),
				dataReader.GetNextRealHvo());
			lexDb.VariantEntryTypesOA.ItemClsid = LexEntryTypeTags.kClassId;
			AddEntryTypes(lexDb);

			// TODO: add lexDb.Introduction, lexDb.Domain/Subentry/Sense,
			// lexDb.AllomorphConditions, lexDb.Status
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="lexDb"></param>
		/// ------------------------------------------------------------------------------------
		private static void AddEntryTypes(ILexDb lexDb)
		{
			var cache = lexDb.Cache;
			var servLoc = cache.ServiceLocator;
			var dataReader = (IDataReader)servLoc.GetInstance<IDataSetup>();
			var tsf = cache.TsStrFactory;
			var eng = servLoc.WritingSystemManager.UserWritingSystem;

			var complexEntryTypesList = lexDb.ComplexEntryTypesOA;
			var lexEntryTypeFactory = servLoc.GetInstance<ILexEntryTypeFactory>() as ILexEntryTypeFactoryInternal;
			for (var i = 1; i <= 6; i++)
			{
				var guid = Guid.Empty;
				ITsString name = null;
				ITsString abbr = null;
				switch (i)
				{
					case 1:
						guid = new Guid("1f6ae209-141a-40db-983c-bee93af0ca3c");
						name = tsf.MakeString("Compound", eng.Handle);
						abbr = tsf.MakeString("comp. of", eng.Handle);
						break;
					case 2:
						guid = new Guid("73266a3a-48e8-4bd7-8c84-91c730340b7d");
						name = tsf.MakeString("Contraction", eng.Handle);
						abbr = tsf.MakeString("cont. of", eng.Handle);
						break;
					case 3:
						guid = new Guid("98c273c4-f723-4fb0-80df-eede2204dfca");
						name = tsf.MakeString("Derivation", eng.Handle);
						abbr = tsf.MakeString("der. of", eng.Handle);
						break;
					case 4:
						guid = new Guid("b2276dec-b1a6-4d82-b121-fd114c009c59");
						name = tsf.MakeString("Idiom", eng.Handle);
						abbr = tsf.MakeString("id. of", eng.Handle);
						break;
					case 5:
						guid = new Guid("35cee792-74c8-444e-a9b7-ed0461d4d3b7");
						name = tsf.MakeString("Phrasal Verb", eng.Handle);
						abbr = tsf.MakeString("p.v.", eng.Handle);
						break;
					case 6:
						guid = new Guid("9466d126-246e-400b-8bba-0703e09bc567");
						name = tsf.MakeString("Saying", eng.Handle);
						abbr = tsf.MakeString("say.", eng.Handle);
						break;
				}

				// Create the LexEntryType.
				lexEntryTypeFactory.Create(
					guid,
					dataReader.GetNextRealHvo(),
					complexEntryTypesList,
					i - 1, // Zero based ord.
					name, eng.Handle,
					abbr, eng.Handle);
			}

			var entryTypesList = lexDb.VariantEntryTypesOA;
			for (var i = 1; i <= 6; i++)
			{
				var guid = Guid.Empty;
				ITsString name = null;
				ITsString abbr = null;
				switch (i)
				{
					case 1:
						guid = new Guid("024b62c9-93b3-41a0-ab19-587a0030219a");
						name = tsf.MakeString("Dialectal Variant", eng.Handle);
						abbr = tsf.MakeString("dial. var. of", eng.Handle);
						break;
					case 2:
						guid = new Guid("4343b1ef-b54f-4fa4-9998-271319a6d74c");
						name = tsf.MakeString("Free Variant", eng.Handle);
						abbr = tsf.MakeString("fr. var. of", eng.Handle);
						break;
					case 3:
						guid = LexEntryTypeTags.kguidLexTypIrregInflectionVar;
						name = tsf.MakeString("Irregular Inflectional Variant", eng.Handle);
						abbr = tsf.MakeString("irr. inf. var. of", eng.Handle);
						break;
					case 4:
						guid = LexEntryTypeTags.kguidLexTypPluralVar;
						name = tsf.MakeString("Plural Variant", eng.Handle);
						abbr = tsf.MakeString("pl. var. of", eng.Handle);
						break;
					case 5:
						guid = LexEntryTypeTags.kguidLexTypPastVar;
						name = tsf.MakeString("Past Variant", eng.Handle);
						abbr = tsf.MakeString("pst. var. of", eng.Handle);
						break;
					case 6:
						guid = new Guid("0c4663b3-4d9a-47af-b9a1-c8565d8112ed");
						name = tsf.MakeString("Spelling Variant", eng.Handle);
						abbr = tsf.MakeString("sp. var. of", eng.Handle);
						break;
				}

				// for Irregularly Inflected Variant Types, use LexEntryInflType factory
				if (guid == LexEntryTypeTags.kguidLexTypIrregInflectionVar ||
					guid == LexEntryTypeTags.kguidLexTypPluralVar ||
					guid == LexEntryTypeTags.kguidLexTypPastVar)
				{
					entryTypesList.PossibilitiesOS.Insert(i - 1,
						new LexEntryInflType(cache, dataReader.GetNextRealHvo(), guid));
					var leit = entryTypesList.PossibilitiesOS[i - 1] as ILexEntryInflType;
					leit.Name.set_String(eng.Handle, name);
					leit.Abbreviation.set_String(eng.Handle, abbr);
					// todo: ReverseAbbr
				}
				else
				{
					// Create the LexEntryType.
					lexEntryTypeFactory.Create(
						guid,
						dataReader.GetNextRealHvo(),
						entryTypesList,
						i - 1, // Zero based ord.
						name, eng.Handle,
						abbr, eng.Handle);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="lexDb"></param>
		/// ------------------------------------------------------------------------------------
		private static void AddMorphTypes(ILexDb lexDb)
		{
			var cache = lexDb.Cache;
			var servLoc = cache.ServiceLocator;
			var dataReader = (IDataReader)servLoc.GetInstance<IDataSetup>();
			var lexEntryTypeFactory = servLoc.GetInstance<IMoMorphTypeFactory>() as IMoMorphTypeFactoryInternal;
			var tsf = cache.TsStrFactory;
			var eng = servLoc.WritingSystemManager.UserWritingSystem;

			var morphTypesList = servLoc.GetInstance<ICmPossibilityListFactory>().Create();
			lexDb.MorphTypesOA = morphTypesList;
			lexDb.MorphTypesOA.ItemClsid = MoMorphTypeTags.kClassId;

			for (var i = 1; i <= 19; i++)
			{
				var guid = Guid.Empty;
				string prefix = null;
				string postfix = null;
				var secondaryOrder = 0;
				ITsString name = null;
				ITsString abbr = null;
				switch (i)
				{
					case 1:
						guid = new Guid("d7f713e4-e8cf-11d3-9764-00c04f186933");
						prefix = "*";
						secondaryOrder = 2;
						name = tsf.MakeString("bound root", eng.Handle);
						abbr = tsf.MakeString("bd root", eng.Handle);
						break;
					case 2:
						guid = new Guid("d7f713e7-e8cf-11d3-9764-00c04f186933");
						prefix = "*";
						secondaryOrder = 2;
						name = tsf.MakeString("bound stem", eng.Handle);
						abbr = tsf.MakeString("bd stem", eng.Handle);
						break;
					case 3:
						guid = new Guid("d7f713df-e8cf-11d3-9764-00c04f186933");
						secondaryOrder = 1;
						name = tsf.MakeString("circumfix", eng.Handle);
						abbr = tsf.MakeString("cfx", eng.Handle);
						break;
					case 4:
						guid = new Guid("d7f713e1-e8cf-11d3-9764-00c04f186933");
						prefix = "=";
						secondaryOrder = 7;
						name = tsf.MakeString("enclitic", eng.Handle);
						abbr = tsf.MakeString("enclit", eng.Handle);
						break;
					case 5:
						guid = new Guid("d7f713da-e8cf-11d3-9764-00c04f186933");
						prefix = "-";
						postfix = "-";
						secondaryOrder = 5;
						name = tsf.MakeString("infix", eng.Handle);
						abbr = tsf.MakeString("ifx", eng.Handle);
						break;
					case 6:
						guid = new Guid("56db04bf-3d58-44cc-b292-4c8aa68538f4");
						secondaryOrder = 1;
						name = tsf.MakeString("particle", eng.Handle);
						abbr = tsf.MakeString("part", eng.Handle);
						break;
					case 7:
						guid = new Guid("d7f713db-e8cf-11d3-9764-00c04f186933");
						postfix = "-";
						secondaryOrder = 3;
						name = tsf.MakeString("prefix", eng.Handle);
						abbr = tsf.MakeString("pfx", eng.Handle);
						break;
					case 8:
						guid = new Guid("d7f713e2-e8cf-11d3-9764-00c04f186933");
						postfix = "=";
						secondaryOrder = 4;
						name = tsf.MakeString("proclitic", eng.Handle);
						abbr = tsf.MakeString("proclit", eng.Handle);
						break;
					case 9:
						guid = new Guid("d7f713e5-e8cf-11d3-9764-00c04f186933");
						secondaryOrder = 1;
						name = tsf.MakeString("root", eng.Handle);
						abbr = tsf.MakeString("ubd root", eng.Handle);
						break;
					case 10:
						guid = new Guid("d7f713dc-e8cf-11d3-9764-00c04f186933");
						prefix = "=";
						postfix = "=";
						secondaryOrder = 5;
						name = tsf.MakeString("simulfix", eng.Handle);
						abbr = tsf.MakeString("smfx", eng.Handle);
						break;
					case 11:
						guid = new Guid("d7f713e8-e8cf-11d3-9764-00c04f186933");
						secondaryOrder = 1;
						name = tsf.MakeString("stem", eng.Handle);
						abbr = tsf.MakeString("ubd stem", eng.Handle);
						break;
					case 12:
						guid = new Guid("d7f713dd-e8cf-11d3-9764-00c04f186933");
						prefix = "-";
						secondaryOrder = 6;
						name = tsf.MakeString("suffix", eng.Handle);
						abbr = tsf.MakeString("sfx", eng.Handle);
						break;
					case 13:
						guid = new Guid("d7f713de-e8cf-11d3-9764-00c04f186933");
						prefix = "~";
						postfix = "~";
						secondaryOrder = 5;
						name = tsf.MakeString("suprafix", eng.Handle);
						abbr = tsf.MakeString("spfx", eng.Handle);
						break;
					case 14:
						guid = new Guid("18d9b1c3-b5b6-4c07-b92c-2fe1d2281bd4");
						prefix = "-";
						postfix = "-";
						name = tsf.MakeString("infixing interfix", eng.Handle);
						abbr = tsf.MakeString("ifxnfx", eng.Handle);
						break;
					case 15:
						guid = new Guid("af6537b0-7175-4387-ba6a-36547d37fb13");
						postfix = "-";
						name = tsf.MakeString("prefixing interfix", eng.Handle);
						abbr = tsf.MakeString("pfxnfx", eng.Handle);
						break;
					case 16:
						guid = new Guid("3433683d-08a9-4bae-ae53-2a7798f64068");
						prefix = "-";
						name = tsf.MakeString("suffixing interfix", eng.Handle);
						abbr = tsf.MakeString("sfxnfx", eng.Handle);
						break;
					case 17:
						guid = new Guid("a23b6faa-1052-4f4d-984b-4b338bdaf95f");
						name = tsf.MakeString("phrase", eng.Handle);
						abbr = tsf.MakeString("phr", eng.Handle);
						break;
					case 18:
						guid = new Guid("0cc8c35a-cee9-434d-be58-5d29130fba5b");
						name = tsf.MakeString("discontiguous phrase", eng.Handle);
						abbr = tsf.MakeString("dis phr", eng.Handle);
						break;
					case 19:
						guid = new Guid("c2d140e5-7ca9-41f4-a69a-22fc7049dd2c");
						name = tsf.MakeString("clitic", eng.Handle);
						abbr = tsf.MakeString("clit", eng.Handle);
						break;
				}

				// Create the MoMorphType.
				lexEntryTypeFactory.Create(
					guid,
					dataReader.GetNextRealHvo(),
					morphTypesList,
					i - 1,
					name, eng.Handle,
					abbr, eng.Handle,
					prefix,
					postfix,
					secondaryOrder);
			}
		}
	}
}
