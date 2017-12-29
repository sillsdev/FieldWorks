// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using LanguageExplorer;
using NUnit.Framework;
using SIL.LCModel.Core.Text;
using SIL.LCModel;
using SIL.FieldWorks.Common.Framework;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;

namespace LanguageExplorerTests.Works
{
	internal struct ControlAssemblyReplacement
	{
		public string m_toolName;
		public string m_controlName;
		public string m_targetAssembly;
		public string m_targetControlClass;
		public string m_newAssembly;
		public string m_newControlClass;
	}

	[TestFixture]
	public abstract class XWorksAppTestBase : MemoryOnlyBackendProviderTestBase
	{
		private ICmPossibilityFactory m_possFact;
		private ICmPossibilityRepository m_possRepo;
		private IPartOfSpeechFactory m_posFact;
		private IPartOfSpeechRepository m_posRepo;
		private ILexEntryFactory m_entryFact;
		private ILexSenseFactory m_senseFact;
		private IMoStemAllomorphFactory m_stemFact;
		private IMoAffixAllomorphFactory m_affixFact;

		//this needs to set the m_application and be called separately from the constructor because nunit runs the
		//default constructor on all of the fixtures before showing anything...
		//and since multiple fixtures will start Multiple FieldWorks applications,
		//this shows multiple splash screens before we have done anything, and
		//runs afoul of the code which enforces only one FieldWorks application defined in the process
		//at any one time.
		protected abstract void FixtureInit();

		/// <summary>
		/// Instantiate a XWorksAppTestBase object.
		/// </summary>
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			FixtureInit();

			// Setup for possibility loading [GetPossibilityOrCreateOne()]
			// and test data creation
			SetupFactoriesAndRepositories();
		}

		private void SetupFactoriesAndRepositories()
		{
			Assert.True(Cache != null, "No cache yet!?");
			var servLoc = Cache.ServiceLocator;
			m_possFact = servLoc.GetInstance<ICmPossibilityFactory>();
			m_possRepo = servLoc.GetInstance<ICmPossibilityRepository>();
			m_posFact = servLoc.GetInstance<IPartOfSpeechFactory>();
			m_posRepo = servLoc.GetInstance<IPartOfSpeechRepository>();
			m_entryFact = servLoc.GetInstance<ILexEntryFactory>();
			m_senseFact = servLoc.GetInstance<ILexSenseFactory>();
			m_stemFact = servLoc.GetInstance<IMoStemAllomorphFactory>();
			m_affixFact = servLoc.GetInstance<IMoAffixAllomorphFactory>();
		}

		#region Data Setup methods

		/// <summary>
		/// Will find a morph type (if one exists) with the given (analysis ws) name.
		/// If not found, will create the morph type in the Lexicon MorphTypes list.
		/// </summary>
		/// <param name="morphTypeName"></param>
		/// <returns></returns>
		protected IMoMorphType GetMorphTypeOrCreateOne(string morphTypeName)
		{
			Assert.IsNotNull(m_possFact, "Fixture Initialization is not complete.");
			var poss = m_possRepo.AllInstances().Where(someposs => someposs.Name.AnalysisDefaultWritingSystem.Text == morphTypeName).FirstOrDefault();
			if (poss != null)
				return poss as IMoMorphType;
			var owningList = Cache.LangProject.LexDbOA.MorphTypesOA;
			Assert.IsNotNull(owningList, "No MorphTypes property on Lexicon object.");
			var ws = Cache.DefaultAnalWs;
			poss = m_possFact.Create(new Guid(), owningList);
			poss.Name.set_String(ws, morphTypeName);
			return poss as IMoMorphType;
		}

		/// <summary>
		/// Will find a variant entry type (if one exists) with the given (analysis ws) name.
		/// If not found, will create the variant entry type in the Lexicon VariantEntryTypes list.
		/// </summary>
		/// <param name="variantTypeName"></param>
		/// <returns></returns>
		protected ILexEntryType GetVariantTypeOrCreateOne(string variantTypeName)
		{
			Assert.IsNotNull(m_possFact, "Fixture Initialization is not complete.");
			var poss = m_possRepo.AllInstances().Where(someposs => someposs.Name.AnalysisDefaultWritingSystem.Text == variantTypeName).FirstOrDefault();
			if (poss != null)
				return poss as ILexEntryType;
			// shouldn't get past here; they're already defined.
			var owningList = Cache.LangProject.LexDbOA.VariantEntryTypesOA;
			Assert.IsNotNull(owningList, "No VariantEntryTypes property on Lexicon object.");
			var ws = Cache.DefaultAnalWs;
			poss = m_possFact.Create(new Guid(), owningList);
			poss.Name.set_String(ws, variantTypeName);
			return poss as ILexEntryType;
		}

		/// <summary>
		/// Will find a grammatical category (if one exists) with the given (analysis ws) name.
		/// If not found, will create a category as a subpossibility of a grammatical category.
		/// </summary>
		/// <param name="catName"></param>
		/// <param name="owningCategory"></param>
		/// <returns></returns>
		protected IPartOfSpeech GetGrammaticalCategoryOrCreateOne(string catName, IPartOfSpeech owningCategory)
		{
			return GetGrammaticalCategoryOrCreateOne(catName, null, owningCategory);
		}

		/// <summary>
		/// Will find a grammatical category (if one exists) with the given (analysis ws) name.
		/// If not found, will create the grammatical category in the owning list.
		/// </summary>
		/// <param name="catName"></param>
		/// <param name="owningList"></param>
		/// <returns></returns>
		protected IPartOfSpeech GetGrammaticalCategoryOrCreateOne(string catName, ICmPossibilityList owningList)
		{
			return GetGrammaticalCategoryOrCreateOne(catName, owningList, null);
		}

		/// <summary>
		/// Will find a grammatical category (if one exists) with the given (analysis ws) name.
		/// If not found, will create a grammatical category either as a possibility of a list,
		/// or as a subpossibility of a category.
		/// </summary>
		/// <param name="catName"></param>
		/// <param name="owningList"></param>
		/// <param name="owningCategory"></param>
		/// <returns></returns>
		protected IPartOfSpeech GetGrammaticalCategoryOrCreateOne(string catName, ICmPossibilityList owningList,
			IPartOfSpeech owningCategory)
		{
			Assert.True(m_posFact != null, "Fixture Initialization is not complete.");
			var category = m_posRepo.AllInstances().Where(someposs => someposs.Name.AnalysisDefaultWritingSystem.Text == catName).FirstOrDefault();
			if (category != null)
				return category;
			var ws = Cache.DefaultAnalWs;
			if (owningList == null)
			{
				if (owningCategory == null)
					throw new ArgumentException(
						"Grammatical category not found and insufficient information given to create one.");
				category = m_posFact.Create(new Guid(), owningCategory);
			}
			else
				category = m_posFact.Create(new Guid(), owningList);
			category.Name.set_String(ws, catName);
			return category;
		}

		protected ILexEntry AddLexeme(IList<ICmObject> addList, string lexForm, string citationForm,
			IMoMorphType morphTypePoss, string gloss, IPartOfSpeech catPoss)
		{
			var ws = Cache.DefaultVernWs;
			var le = AddLexeme(addList, lexForm, morphTypePoss, gloss, catPoss);
			le.CitationForm.set_String(ws, citationForm);
			return le;
		}

		protected ILexEntry AddLexeme(IList<ICmObject> addList, string lexForm, IMoMorphType morphTypePoss,
			string gloss, IPartOfSpeech categoryPoss)
		{
			var msa = new SandboxGenericMSA { MainPOS = categoryPoss };
			var comp = new LexEntryComponents { MorphType = morphTypePoss, MSA = msa };
			comp.GlossAlternatives.Add(TsStringUtils.MakeString(gloss, Cache.DefaultAnalWs));
			comp.LexemeFormAlternatives.Add(TsStringUtils.MakeString(lexForm, Cache.DefaultVernWs));
			var entry = m_entryFact.Create(comp);
			addList.Add(entry);
			return entry;
		}

		protected ILexEntry AddVariantLexeme(IList<ICmObject> addList, IVariantComponentLexeme origLe,
			string lexForm, IMoMorphType morphTypePoss, string gloss, IPartOfSpeech categoryPoss,
			ILexEntryType varType)
		{
			Assert.IsNotNull(varType, "Need a variant entry type!");
			var msa = new SandboxGenericMSA { MainPOS = categoryPoss };
			var comp = new LexEntryComponents { MorphType = morphTypePoss, MSA = msa };
			comp.GlossAlternatives.Add(TsStringUtils.MakeString(gloss, Cache.DefaultAnalWs));
			comp.LexemeFormAlternatives.Add(TsStringUtils.MakeString(lexForm, Cache.DefaultVernWs));
			var entry = m_entryFact.Create(comp);
			var ler = entry.MakeVariantOf(origLe, varType);
			addList.Add(entry);
			addList.Add(ler);
			return entry;
		}

		protected ILexSense AddSenseToEntry(IList<ICmObject> addList, ILexEntry le, string gloss,
			IPartOfSpeech catPoss)
		{
			var msa = new SandboxGenericMSA();
			msa.MainPOS = catPoss;
			var sense = m_senseFact.Create(le, msa, gloss);
			addList.Add(sense);
			return sense;
		}

		protected ILexSense AddSubSenseToSense(IList<ICmObject> addList, ILexSense ls, string gloss,
			IPartOfSpeech catPoss)
		{
			var msa = new SandboxGenericMSA();
			msa.MainPOS = catPoss;
			var sense = m_senseFact.Create(new Guid(), ls);
			sense.SandboxMSA = msa;
			sense.Gloss.set_String(Cache.DefaultAnalWs, gloss);
			addList.Add(sense);
			return sense;
		}

		protected void AddStemAllomorphToEntry(IList<ICmObject> addList, ILexEntry le, string alloName,
			IPhEnvironment env)
		{
			var allomorph = m_stemFact.Create();
			le.AlternateFormsOS.Add(allomorph);
			if (env != null)
				allomorph.PhoneEnvRC.Add(env);
			allomorph.Form.set_String(Cache.DefaultVernWs, alloName);
			addList.Add(allomorph);
		}

		protected void AddAffixAllomorphToEntry(IList<ICmObject> addList, ILexEntry le, string alloName,
			IPhEnvironment env)
		{
			var allomorph = m_affixFact.Create();
			le.AlternateFormsOS.Add(allomorph);
			if (env != null)
				allomorph.PhoneEnvRC.Add(env);
			allomorph.Form.set_String(Cache.DefaultVernWs, alloName);
			addList.Add(allomorph);
		}

		#endregion
	}
}
