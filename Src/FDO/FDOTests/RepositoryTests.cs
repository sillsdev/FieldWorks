// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2009' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RepositoryTests.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Infrastructure.Impl;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.CoreTests
{
	#region Misc Repository Tests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to test select Repository functionality.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class RepositoryTests : MemoryOnlyBackendProviderTestBase
	{
		/// <summary>
		/// Test the LangProj Repository.
		/// </summary>
		[Test]
		public void LangProjRepositoryTests()
		{
			var lpRepository = Cache.ServiceLocator.GetInstance<ILangProjectRepository>();
			Assert.IsNotNull(lpRepository, "LP repository is null.");
			Assert.AreEqual(1, lpRepository.Count, "Wrong LP count");
			Assert.AreEqual(lpRepository.Count, lpRepository.AllInstances().Count(), "Wrong LP count 'second test'");
			var lp = Cache.LanguageProject;
			Assert.AreSame(lp, lpRepository.GetObject(lp.Guid), "Wrong LP in Repository.");
			Assert.AreSame(lp, lpRepository.AllInstances().First(), "Wrong LP in Repo.");
		}

		/// <summary>
		/// Test several CmPossibility related repositories.
		/// </summary>
		[Test]
		public void CmPossibilityRelatedRepositoryTests()
		{
			var morphTypeRepository = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			var morphTypes = morphTypeRepository.AllInstances();
			var cmPosRepository = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>();
			var cmpos = cmPosRepository.AllInstances().ToArray();
			// MoMorphTypes should be in the CmPoss Repo, since MoMorphTypes are subclasses of CmPossibility.
			foreach (var mt in morphTypes)
				Assert.Contains(mt, cmpos, "MorphType not in cmPos Repo.");
			var lexEntryTypeRepo = Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>();
			var allLexEntryTypes = lexEntryTypeRepo.AllInstances();
			// LexEntryTypes should be in the CmPoss Repo, since LexEntryTypes are subclasses of CmPossibility.
			foreach (var let in allLexEntryTypes)
				Assert.Contains(let, cmpos, "LexEntryType not in cmPos Repo.");
		}

		/// <summary>
		/// Test methods that track new instances.
		/// </summary>
		[Test]
		public void NewInstancesThisSession()
		{
			var entryFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			ILexEntry entry = null;
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
					{
						entry = entryFactory.Create();
					});
			var objRepo = Cache.ServiceLocator.ObjectRepository;
			Assert.That(objRepo.WasCreatedThisSession(entry), Is.True);
			Assert.That(objRepo.InstancesCreatedThisSession(entry.ClassID), Is.True);
			Cache.ActionHandlerAccessor.Undo();
		}

		#region ICmObjectRepository tests
		/// <summary>
		/// Make sure it throws the exception on asking for a non-existing Guid.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void GetNonExistantObjectByGuidTest()
		{
			Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(Guid.NewGuid());
		}

		/// <summary>
		/// Make sure it throws the exception on asking for a non-existing Guid.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void GetNonExistantObjectByGuidTest2()
		{
			Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(Guid.Empty);
		}

		/// <summary>
		/// Make sure the GetObject method works.
		/// </summary>
		[Test]
		public void GetObjectByGuidTest()
		{
			var lp = Cache.LanguageProject;
			Assert.AreSame(lp, Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(lp.Guid), "Wrong object.");
		}

		/// <summary>
		/// Make sure it throws the exception on asking for a non-extisting hvo.
		/// </summary>
		[Test]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void GetNonExtistantObjectByHvoTest()
		{
			Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(Int32.MinValue);
		}

		/// <summary>
		/// Make sure the GetObject method works.
		/// </summary>
		[Test]
		public void GetObjectByHvoTest()
		{
			var lp = Cache.LanguageProject;
			Assert.AreSame(lp, Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(lp.Hvo), "Wrong object.");
		}
		#endregion

		/// <summary>
		/// Make sure the AllInstances methods work.
		/// </summary>
		[Test]
		public void GetAllInstancesTest()
		{
			var lp = Cache.LanguageProject;
			var allMorphTypes = lp.LexDbOA.MorphTypesOA.ReallyReallyAllPossibilities;
			var allInstancesByClsid = lp.Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().AllInstances();
			Assert.AreEqual(allMorphTypes.Count, allInstancesByClsid.Count(), "Wrong count.");
			foreach (var mt in allInstancesByClsid)
				Assert.IsTrue(allMorphTypes.Contains(mt), "Missing instance.");
		}
	}
	#endregion
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to test additions to MoStemAllomorphRepository functionality.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class MoStemAllomorphRepositoryTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Test the repository MonomorphemicMorphData cache
		/// </summary>
		[Test]
		public void MonomorphemicMorphData()
		{
			var kick = MakeEntry("kick", "strike with foot");
			var morphRepo = (MoStemAllomorphRepository) Cache.ServiceLocator.GetInstance<IMoStemAllomorphRepository>();
			var morphData = morphRepo.MonomorphemicMorphData();
			var kickKey = new Tuple<int, string>(Cache.DefaultVernWs, "kick");
			Assert.That(morphData.ContainsKey(kickKey), Is.False, "morph with no type is not included");

			var kickMorph = kick.LexemeFormOA;
			var morphTypeRepo = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			kickMorph.MorphTypeRA = morphTypeRepo.GetObject(MoMorphTypeTags.kguidMorphBoundRoot);
			morphData = morphRepo.MonomorphemicMorphData();
			Assert.That(morphData.ContainsKey(kickKey), Is.False, "bound root is not included");

			kickMorph.MorphTypeRA = morphTypeRepo.GetObject(MoMorphTypeTags.kguidMorphRoot);
			morphData = morphRepo.MonomorphemicMorphData();
			Assert.That(morphData[kickKey], Is.EqualTo(kickMorph), "root should be included");

			var bug = MakeEntry("bug", "detestable monster");
			var bugMorph = bug.LexemeFormOA;
			bugMorph.MorphTypeRA = morphTypeRepo.GetObject(MoMorphTypeTags.kguidMorphBoundStem);
			morphData = morphRepo.MonomorphemicMorphData();
			var bugKey = new Tuple<int, string>(Cache.DefaultVernWs, "bug");
			Assert.That(morphData[kickKey], Is.EqualTo(kickMorph), "root should be included");
			Assert.That(morphData.ContainsKey(bugKey), Is.False, "bound stem is not included");

			bugMorph.MorphTypeRA = morphTypeRepo.GetObject(MoMorphTypeTags.kguidMorphStem);
			morphData = morphRepo.MonomorphemicMorphData();
			Assert.That(morphData[bugKey], Is.EqualTo(bugMorph), "stem should be included");

			var bugAltA = MakeAllomorph(bug, "bugA", MoMorphTypeTags.kguidMorphPrefix);
			var bugAltAKey = new Tuple<int, string>(Cache.DefaultVernWs, "bugA");
			morphData = morphRepo.MonomorphemicMorphData();
			Assert.That(morphData.ContainsKey(bugAltAKey), Is.False, "prefix is not included");

			bugAltA.MorphTypeRA = morphTypeRepo.GetObject(MoMorphTypeTags.kguidMorphEnclitic);
			morphData = morphRepo.MonomorphemicMorphData();
			Assert.That(morphData[bugAltAKey], Is.EqualTo(bugAltA), "enclitic allomorph should be included");

			var bugAltB = MakeAllomorph(bug, "bugB", MoMorphTypeTags.kguidMorphSuffix);
			var bugAltBKey = new Tuple<int, string>(Cache.DefaultVernWs, "bugB");
			morphData = morphRepo.MonomorphemicMorphData();
			Assert.That(morphData.ContainsKey(bugAltBKey), Is.False, "suffix is not included");

			bugAltB.MorphTypeRA = morphTypeRepo.GetObject(MoMorphTypeTags.kguidMorphProclitic);
			morphData = morphRepo.MonomorphemicMorphData();
			Assert.That(morphData[bugAltBKey], Is.EqualTo(bugAltB), "proclitic allomorph should be included");

			kick.Delete();
			morphData = morphRepo.MonomorphemicMorphData();
			Assert.That(morphData.ContainsKey(kickKey), Is.False, "deleted morpheme is no longer included");

			bugAltA.MorphTypeRA = morphTypeRepo.GetObject(MoMorphTypeTags.kguidMorphPrefix);
			morphData = morphRepo.MonomorphemicMorphData();
			Assert.That(morphData.ContainsKey(bugAltAKey), Is.False, "changing back to prefix causes exclusion");

			m_actionHandler.EndUndoTask();
			UndoableUnitOfWorkHelper.Do("undo it", "redo it", m_actionHandler,
				() => bugAltB.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("bugBB", Cache.DefaultVernWs));
			morphData = morphRepo.MonomorphemicMorphData();
			Assert.That(morphData.ContainsKey(bugAltBKey), Is.False, "changing form causes exclusion");
			var bugAltBBKey = new Tuple<int, string>(Cache.DefaultVernWs, "bugBB");
			Assert.That(morphData[bugAltBBKey], Is.EqualTo(bugAltB), "changing form causes new key to be included");

			m_actionHandler.Undo();
			morphData = morphRepo.MonomorphemicMorphData();
			Assert.That(morphData[bugAltBKey], Is.EqualTo(bugAltB), "Undo should reinstate things");

			m_actionHandler.Redo();
			morphData = morphRepo.MonomorphemicMorphData();
			Assert.That(morphData.ContainsKey(bugAltBKey), Is.False, "Redo should re-change things");
		}

		/// <summary>
		/// Copied from StringServicesTests (plus UOW); possibly best for each test set to have own utility functions?
		/// </summary>
		private ILexEntry MakeEntry(string lf, string gloss)
		{
			ILexEntry entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var form = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = form;
			form.Form.VernacularDefaultWritingSystem =
				Cache.TsStrFactory.MakeString(lf, Cache.DefaultVernWs);
			AddSense(entry, gloss);
			return entry;
		}

		private IMoForm MakeAllomorph(ILexEntry entry, string form, Guid morphType)
		{
			var result = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.AlternateFormsOS.Add(result);
			result.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString(form, Cache.DefaultVernWs);
			result.MorphTypeRA = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(morphType);
			return result;
		}

		private ILexSense AddSense(ILexEntry entry, string gloss)
		{
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(gloss,
				Cache.DefaultAnalWs);
			return sense;
		}
	}


	#region PunctuationFormRepository Tests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to test select PunctuationFormRepository functionality.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class PunctuationFormRepositoryTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		IPunctuationFormRepository m_repo;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to clear hashtables to siumulate initial state.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			m_repo = Cache.ServiceLocator.GetInstance<IPunctuationFormRepository>();
			ReflectionHelper.SetField(m_repo, "m_punctFormFromForm", null);
			ReflectionHelper.SetField(m_repo, "m_orcFormFromForm", null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the PunctuationFormRepository.TryGetObject method when the form to search for
		/// is an ORC character. We expect it to return false for ORCs with different
		/// properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TryGetObject_ORCWithDifferentProps_WsOnly()
		{
			IPunctuationForm pf = Cache.ServiceLocator.GetInstance<IPunctuationFormFactory>().Create();
			pf.Form = Cache.TsStrFactory.MakeString(StringUtils.kszObject, Cache.DefaultAnalWs);
			IPunctuationForm pfDummy;
			ITsString tssOrcness = Cache.TsStrFactory.MakeString(StringUtils.kszObject, Cache.DefaultVernWs);
			Assert.IsFalse(m_repo.TryGetObject(tssOrcness, out pfDummy));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the PunctuationFormRepository.TryGetObject method when the form to search for
		/// is an ORC character after it has been called the first time (which initializes the
		/// hash table) and additional forms are added. We still expect it to return false for
		/// ORCs with different properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TryGetObject_ORC_Twice_WsOnly()
		{
			IPunctuationForm pfDummy;
			ITsString tssOrcness = Cache.TsStrFactory.MakeString(StringUtils.kszObject, Cache.DefaultVernWs);
			Assert.IsFalse(m_repo.TryGetObject(tssOrcness, out pfDummy));

			IPunctuationForm pf = Cache.ServiceLocator.GetInstance<IPunctuationFormFactory>().Create();
			pf.Form = Cache.TsStrFactory.MakeString(StringUtils.kszObject, Cache.DefaultVernWs);
			tssOrcness = Cache.TsStrFactory.MakeString(StringUtils.kszObject, Cache.DefaultUserWs);
			Assert.IsFalse(m_repo.TryGetObject(tssOrcness, out pfDummy));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the PunctuationFormRepository.TryGetObject method when the form to search for
		/// is an ORC character with the same properties as an existing one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TryGetObject_ORCWithSameProps_WsOnly()
		{
			IPunctuationForm pf = Cache.ServiceLocator.GetInstance<IPunctuationFormFactory>().Create();
			pf.Form = Cache.TsStrFactory.MakeString(StringUtils.kszObject, Cache.DefaultAnalWs);
			IPunctuationForm pfExisting;
			ITsString tssOrcness = Cache.TsStrFactory.MakeString(StringUtils.kszObject, Cache.DefaultAnalWs);
			Assert.IsTrue(m_repo.TryGetObject(tssOrcness, out pfExisting));
			Assert.AreEqual(pf, pfExisting);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the PunctuationFormRepository.TryGetObject method when the form to search for
		/// is an ORC character with the same properties as an existing one. This version
		/// actually has real object data set for the ORC props.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TryGetObject_ORCWithDifferentProps_ObjData()
		{
			IPunctuationForm pf = Cache.ServiceLocator.GetInstance<IPunctuationFormFactory>().Create();
			ITsStrBldr bldr = TsStrBldrClass.Create();
			StringUtils.InsertOrcIntoPara(Guid.NewGuid(), FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultVernWs);
			pf.Form = bldr.GetString();
			IPunctuationForm pfDummy;
			// Replace the ORC in the builder with a different ORC
			StringUtils.InsertOrcIntoPara(Guid.NewGuid(), FwObjDataTypes.kodtOwnNameGuidHot, bldr, 0, 1, Cache.DefaultVernWs);
			ITsString tssOrcness = bldr.GetString();
			Assert.IsFalse(m_repo.TryGetObject(tssOrcness, out pfDummy));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the PunctuationFormRepository.TryGetObject method when the form to search for
		/// is an ORC character with the same properties as an existing one. This version
		/// actually has real object data set for the ORC props.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TryGetObject_ORCWithSameProps_ObjData()
		{
			IPunctuationForm pf = Cache.ServiceLocator.GetInstance<IPunctuationFormFactory>().Create();
			ITsStrBldr bldr = TsStrBldrClass.Create();
			StringUtils.InsertOrcIntoPara(Guid.NewGuid(), FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultVernWs);
			pf.Form = bldr.GetString();
			IPunctuationForm pfExisting;
			ITsString tssOrcness = bldr.GetString();
			Assert.IsTrue(m_repo.TryGetObject(tssOrcness, out pfExisting));
			Assert.AreEqual(pf, pfExisting);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the PunctuationFormRepository.TryGetObject method when the form to search for
		/// is an ordinary punctuation character whose form is not already in the repo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TryGetObject_PlainPunctString_NotExists()
		{
			IPunctuationForm pfDummy;
			ITsString tssPunc = Cache.TsStrFactory.MakeString(".", Cache.DefaultVernWs);
			Assert.IsFalse(m_repo.TryGetObject(tssPunc, out pfDummy));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the PunctuationFormRepository.TryGetObject method when the form to search for
		/// is an ordinary punctuation character whose form is not already in the repo. This
		/// case checks the condition when there is also an entry in the hashtable of ORCs to
		/// compare against.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TryGetObject_PlainPunctString_NotExists_ORCTableNotEmpty()
		{
			IPunctuationForm pf = Cache.ServiceLocator.GetInstance<IPunctuationFormFactory>().Create();
			ITsStrBldr bldr = TsStrBldrClass.Create();
			StringUtils.InsertOrcIntoPara(Guid.NewGuid(), FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultVernWs);
			pf.Form = bldr.GetString();

			IPunctuationForm pfDummy;
			ITsString tssPunc = Cache.TsStrFactory.MakeString(".", Cache.DefaultVernWs);
			Assert.IsFalse(m_repo.TryGetObject(tssPunc, out pfDummy));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the PunctuationFormRepository.TryGetObject method when the form to search for
		/// is an ordinary punctuation character whose form is already in the repo, albeit with
		/// a different writing system (but we don't care about that).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TryGetObject_PlainPunctString_Exists()
		{
			IPunctuationForm pf = Cache.ServiceLocator.GetInstance<IPunctuationFormFactory>().Create();
			pf.Form = Cache.TsStrFactory.MakeString(".", Cache.DefaultAnalWs);
			IPunctuationForm pfExisting;
			ITsString tssPunc = Cache.TsStrFactory.MakeString(".", Cache.DefaultVernWs);
			Assert.IsTrue(m_repo.TryGetObject(tssPunc, out pfExisting));
			Assert.AreEqual(pf, pfExisting);
		}
	}
	#endregion
}
