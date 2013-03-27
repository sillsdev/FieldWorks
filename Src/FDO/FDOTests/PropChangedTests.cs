// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PropChangedTests.cs
// Responsibility: Randy Regnier
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.FwUtils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.CoreTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the PropChanged call on IVwnotifyChange implementation.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class PropChangedTests : MemoryOnlyBackendProviderTestBase
	{
		private ISilDataAccess m_sda;
		private Notifiee m_notifiee;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_sda = Cache.DomainDataByFlid;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureTeardown()
		{
			m_sda = null;

			base.FixtureTeardown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			m_notifiee = new Notifiee();
			m_sda.AddNotification(m_notifiee);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			m_sda.RemoveNotification(m_notifiee);
			m_notifiee = null;

			while (m_actionHandler.CanUndo())
				m_actionHandler.Undo();
			m_actionHandler.Commit();

			base.TestTearDown();
		}

		private void CheckChanges(int expectedCount, int changeIndex, int hvo, int flid,
			int expectedIvMin, int expectedCvIns, int expectedCvDel)
		{
			Assert.AreEqual(expectedCount, m_notifiee.Changes.Count, "Wrong number of changes.");
			if (changeIndex >= 0)
			{
				var change = m_notifiee.Changes[changeIndex];
				Assert.AreEqual(hvo, change.Hvo, "Wrong hvo.");
				Assert.AreEqual(flid, change.Tag, "Wrong tag.");
				Assert.AreEqual(expectedIvMin, change.IvMin, "Wrong ivMin.");
				Assert.AreEqual(expectedCvIns, change.CvIns, "Wrong cvIns.");
				Assert.AreEqual(expectedCvDel, change.CvDel, "Wrong cvDel.");
			}
		}

		private void ClearChanges()
		{
			m_notifiee.ClearChanges();
		}

		/// <summary>
		/// This is a weaker CheckChanges, useful when there might be other changes we don't care about.
		/// </summary>
		private void CheckChanges(int hvo, int flid,
			int expectedIvMin, int expectedCvIns, int expectedCvDel)
		{
			var c = new ChangeInformationTest(hvo, flid, expectedIvMin, expectedCvIns, expectedCvDel);
			if (!m_notifiee.IsValidChange(c))
				Assert.Fail("Expected change not found: Hvo " + hvo + ", flid " + flid + ", ivMin " + expectedIvMin
					+ ", cvIns " + expectedCvIns +", cvDel " + expectedCvDel);
		}

		/// <summary>
		/// Test the various kinds of vector properties.
		/// </summary>
		[Test]
		public void VectorPropertyTests()
		{
			var servLoc = Cache.ServiceLocator;
			var entryFactory = servLoc.GetInstance<ILexEntryFactory>();
			var resourceFactory = servLoc.GetInstance<ICmResourceFactory>();
			var lexDb = Cache.LanguageProject.LexDbOA;
			ILexEntry le2 = null;
			// The following tests assume one resource originally exists. We also need le2, to be the owner
			// of the senses.
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler,
				() =>
				{
					lexDb.ResourcesOC.Add(resourceFactory.Create());
					le2 = entryFactory.Create();
				});
			// 1
			ClearChanges();
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () => lexDb.ResourcesOC.Clear());
			// Check the Resources vector prop.
			CheckChanges(1, -1, lexDb.Hvo, LexDbTags.kflidResources, 0, 1, 0);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(1, -1, lexDb.Hvo, LexDbTags.kflidResources, 0, 0, 0);
			m_actionHandler.Redo();
			ClearChanges();

			// 2
			// Test a collection property.
			ICmResource res1 = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Add one item.
				res1 = resourceFactory.Create();
				lexDb.ResourcesOC.Add(res1);
			});
			// Check the Entries vector prop.
			CheckChanges(1, 0, lexDb.Hvo, LexDbTags.kflidResources, 0, 1, 0);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(1, 0, lexDb.Hvo, LexDbTags.kflidResources, 0, 0, 1);
			m_actionHandler.Redo();
			ClearChanges();

			// 3
			ICmResource res2 = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Add another item.
				res2 = resourceFactory.Create();
				lexDb.ResourcesOC.Add(res2);
			});
			// Check the Resources vector prop.
			CheckChanges(1, 0, lexDb.Hvo, LexDbTags.kflidResources, 1, 1, 0);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(1, 0, lexDb.Hvo, LexDbTags.kflidResources, 1, 0, 1);
			m_actionHandler.Redo();
			SetLexeme(le2, "second");
			ClearChanges();

			// 4
			ICmResource res3 = null;
			ICmResource res4 = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Add two new items.
				res3 = resourceFactory.Create();
				lexDb.ResourcesOC.Add(res3);
				res4 = resourceFactory.Create();
				lexDb.ResourcesOC.Add(res4);
			});
			// Check the Entries vector prop.
			CheckChanges(1, 0, lexDb.Hvo, LexDbTags.kflidResources, 2, 2, 0);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(1, 0, lexDb.Hvo, LexDbTags.kflidResources, 2, 0, 2);
			m_actionHandler.Redo();
			ClearChanges();

			// 5
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Remove a couple of non-contiguous items.
				res1.Delete();
				res3.Delete();
			 });
			// Check the Entries vector prop.
			CheckChanges(1, 0, lexDb.Hvo, LexDbTags.kflidResources, 0, 1, 3);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(1, 0, lexDb.Hvo, LexDbTags.kflidResources, 0, 3, 1);
			m_actionHandler.Redo();
			ClearChanges();

			// 6
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () => res4.Delete());
			// Check the Entries vector prop.
			CheckChanges(1, 0, lexDb.Hvo, LexDbTags.kflidResources, 1, 0, 1);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(1, 0, lexDb.Hvo, LexDbTags.kflidResources, 1, 1, 0);
			m_actionHandler.Redo();
			ClearChanges();

			// 7
			// Test a sequence property.
			// By this point only le2 is still there,
			// which is fine, since we are moving to test senses,
			// and they can all go into le2.
			var senseFactory = servLoc.GetInstance<ILexSenseFactory>();
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Add one item.
				var se1 = senseFactory.Create();
				le2.SensesOS.Add(se1);
			});
			// Check the Senses vector prop.
			CheckChanges(le2.Hvo, LexEntryTags.kflidSenses, 0, 1, 0);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(le2.Hvo, LexEntryTags.kflidSenses, 0, 0, 1);
			m_actionHandler.Redo();
			ClearChanges();

			// 8
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Add another item, but before the other sense.
				var se2 = senseFactory.Create();
				le2.SensesOS.Insert(0, se2);
			});
			// Check the Senses vector prop.
			CheckChanges(le2.Hvo, LexEntryTags.kflidSenses, 0, 1, 0);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(le2.Hvo, LexEntryTags.kflidSenses, 0, 0, 1);
			m_actionHandler.Redo();
			ClearChanges();

			// 9
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Add another item, but between the other two senses.
				var se3 = senseFactory.Create();
				le2.SensesOS.Insert(1, se3);
			});
			// Check the Senses vector prop.
			CheckChanges(le2.Hvo, LexEntryTags.kflidSenses, 1, 1, 0);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(le2.Hvo, LexEntryTags.kflidSenses, 1, 0, 1);
			m_actionHandler.Redo();
			ClearChanges();

			// 10
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Add another item, but between two senses.
				var se4 = senseFactory.Create();
				le2.SensesOS.Insert(2, se4);
				// Add yet another one at the end.
				var se5 = senseFactory.Create();
				le2.SensesOS.Add(se5);
			});
			// Check the Senses vector prop.
			CheckChanges(le2.Hvo, LexEntryTags.kflidSenses, 2, 3, 1);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(le2.Hvo, LexEntryTags.kflidSenses, 2, 1, 3);
			m_actionHandler.Redo();
			ClearChanges();

			// 11
			// Clear Entries prop
			var resourcesCount = lexDb.ResourcesOC.Count();
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () => lexDb.ResourcesOC.Clear());
			// Check the Entries vector prop.
			CheckChanges(1, 0, lexDb.Hvo, LexDbTags.kflidResources, 0, 0, resourcesCount);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(lexDb.Hvo, LexDbTags.kflidResources, 0, resourcesCount, 0);
			m_actionHandler.Redo();
			ClearChanges();
		}

		private void SetLexeme(ILexEntry le, string form)
		{
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler,
				() =>
					{
						var lf = le.Services.GetInstance<IMoStemAllomorphFactory>().Create();
						le.LexemeFormOA = lf;
						lf.Form.VernacularDefaultWritingSystem = lf.Cache.TsStrFactory.MakeString(form,
							Cache.DefaultVernWs);
					});
		}

		/// <summary>
		/// Test the get/set accessors for normal ITsString properties (kcptString).
		/// </summary>
		[Test]
		public void kcptStringTests()
		{
			ILexEntry le;
			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				le = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				undoHelper.RollBack = false;
			}
			// Skip the main checking on the vector prop.
			CheckChanges(1, -1, 0, 0, 0, 0, 0);
			ClearChanges();

			var tsf = Cache.TsStrFactory;
			var userWs = Cache.WritingSystemFactory.UserWs;
			ITsString importResidue;
			int originalImportResidueLength = 0;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				importResidue = tsf.MakeString("import residue", userWs);
				originalImportResidueLength = importResidue.Length;
				le.ImportResidue = importResidue;
			});
			// Null to new value should have cvIns be the new length and cvDel be 0.
			CheckChanges(1, 0, le.Hvo, LexEntryTags.kflidImportResidue, 0, originalImportResidueLength, 0);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(1, 0, le.Hvo, LexEntryTags.kflidImportResidue, 0, 0, originalImportResidueLength);
			m_actionHandler.Redo();
			ClearChanges();

			int newImportResidueLength = 0;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				importResidue = tsf.MakeString("new import residue", userWs);
				newImportResidueLength = importResidue.Length;
				le.ImportResidue = importResidue;
			});
			// Old value to new value should have cvIns be the new length and cvDel be old value length.
			CheckChanges(1, 0, le.Hvo, LexEntryTags.kflidImportResidue, 0, newImportResidueLength, originalImportResidueLength);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(1, 0, le.Hvo, LexEntryTags.kflidImportResidue, 0, originalImportResidueLength, newImportResidueLength);
			m_actionHandler.Redo();
			ClearChanges();

			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () => le.ImportResidue = null);
			// Old value to null should have cvIns be 0 and cvDel be old value length.
			CheckChanges(1, 0, le.Hvo, LexEntryTags.kflidImportResidue, 0, 0, newImportResidueLength);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(1, 0, le.Hvo, LexEntryTags.kflidImportResidue, 0, newImportResidueLength, 0);
			m_actionHandler.Redo();
			ClearChanges();
		}

		/// <summary>
		/// Test the get/set accessors for normal unicode properties (kcptUnicode).
		/// </summary>
		[Test]
		public void kcptUnicodeTests()
		{
			var lp = Cache.LanguageProject;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () => lp.EthnologueCode = "NEWCode");
			// Null to new value should have cvIns be the new length and cvDel be 0.
			CheckChanges(1, 0, lp.Hvo, LangProjectTags.kflidEthnologueCode, 0, 7, 0);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(1, 0, lp.Hvo, LangProjectTags.kflidEthnologueCode, 0, 0, 7);
			m_actionHandler.Redo();
			ClearChanges();

			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () => lp.EthnologueCode = "NEWERCode");
			// Old value to new value should have cvIns be the new length and cvDel be old value length.
			CheckChanges(1, 0, lp.Hvo, LangProjectTags.kflidEthnologueCode, 0, 9, 7);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(1, 0, lp.Hvo, LangProjectTags.kflidEthnologueCode, 0, 7, 9);
			m_actionHandler.Redo();
			ClearChanges();

			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () => lp.EthnologueCode = null);
			// Old value to null should have cvIns be 0 and cvDel be old value length.
			CheckChanges(1, 0, lp.Hvo, LangProjectTags.kflidEthnologueCode, 0, 0, 9);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(1, 0, lp.Hvo, LangProjectTags.kflidEthnologueCode, 0, 9, 0);
			m_actionHandler.Redo();
			ClearChanges();
		}

		/// <summary>
		/// Test the MultiUnicode PropChanges.
		/// </summary>
		[Test]
		public void MultiUnicodeTests()
		{
			var tsf = Cache.TsStrFactory;
			var englishWsHvo = Cache.WritingSystemFactory.GetWsFromStr("en");
			var spanishWsHvo = Cache.WritingSystemFactory.GetWsFromStr("es");
			var lp = Cache.LanguageProject;

			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				// Description is MultiString
				// Set LP's Description.
				lp.Description.set_String(
					englishWsHvo,
					tsf.MakeString("Stateful FDO Test Language Project: Desc", englishWsHvo));
				lp.Description.set_String(
					spanishWsHvo,
					tsf.MakeString("Proyecto de prueba: FDO: desc", spanishWsHvo));

				undoHelper.RollBack = false;
			}
			// Should be two PCs on LP.
			CheckChanges(2, 0, lp.Hvo, CmProjectTags.kflidDescription, englishWsHvo, 0, 0);
			CheckChanges(2, 1, lp.Hvo, CmProjectTags.kflidDescription, spanishWsHvo, 0, 0);
			ClearChanges();
		}

		/// <summary>
		/// Test the get/set accessors for binary data properties.
		/// </summary>
		[Test]
		public void kcptBinaryTests()
		{
			IUserConfigAcctFactory acctFactory = Cache.ServiceLocator.GetInstance<IUserConfigAcctFactory>();
			IUserConfigAcct acct;
			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				acct = acctFactory.Create();
				Cache.LanguageProject.UserAccountsOC.Add(acct);
				undoHelper.RollBack = false;
			}
			CheckChanges(1, 0, Cache.LanguageProject.Hvo, LangProjectTags.kflidUserAccounts, 0, 1, 0);
			ClearChanges();

			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				acct.Sid = new byte[] { 1, 2, 3 };
				undoHelper.RollBack = false;
			}
			// Changing a prop on an unowned object does make one PC.
			CheckChanges(1, 0, acct.Hvo, UserConfigAcctTags.kflidSid, 0, 0, 0);
			ClearChanges();
		}

		/// <summary>
		/// Test the get/set accessors for ITsTextProps data properties.
		/// </summary>
		[Test]
		public void UnknownTests()
		{
			var lp = Cache.LanguageProject;
			var styleFactory = Cache.ServiceLocator.GetInstance<IStStyleFactory>();
			IStStyle style;
			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				style = styleFactory.Create();
				lp.StylesOC.Add(style);
				undoHelper.RollBack = false;
			}
			// Should be one PC.
			CheckChanges(1, 0, lp.Hvo, LangProjectTags.kflidStyles, 0, 1, 0);
			ClearChanges();

			var userWs = Cache.WritingSystemFactory.UserWs;
			var bldr = TsPropsBldrClass.Create();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Arial");
			bldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
			var tpp = bldr.GetTextProps();
			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				style.Rules = tpp;
				undoHelper.RollBack = false;
			}
			// Should be one PC.
			CheckChanges(1, 0, style.Hvo, StStyleTags.kflidRules, 0, 0, 0);
			ClearChanges();
		}

		/// <summary>
		/// Test the get/set accessors for boolean properties.
		/// </summary>
		[Test]
		public void kcptBooleanTests()
		{
			var servLoc = Cache.ServiceLocator;
			var lp = servLoc.GetInstance<ILangProjectRepository>().AllInstances().First();
			ICmPossibilityList peopleList;
			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				lp.PeopleOA = servLoc.GetInstance<ICmPossibilityListFactory>().Create();
				peopleList = lp.PeopleOA;
				undoHelper.RollBack = false;
			}
			CheckChanges(1, -1, 0, 0, 0, 0, 0);
			ClearChanges();

			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "set boolean"))
			{
				peopleList.IsVernacular = !peopleList.IsVernacular;
				undoHelper.RollBack = false;
			}
			CheckChanges(1, 0, peopleList.Hvo, CmPossibilityListTags.kflidIsVernacular, 0, 0, 0);
			ClearChanges();
			// Check Undo/Redo to ensure they fire PropChanges.
			m_actionHandler.Undo();
			CheckChanges(1, 0, peopleList.Hvo, CmPossibilityListTags.kflidIsVernacular, 0, 0, 0);
			ClearChanges();
			m_actionHandler.Redo();
			CheckChanges(1, 0, peopleList.Hvo, CmPossibilityListTags.kflidIsVernacular, 0, 0, 0);
			ClearChanges();

			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				// Get rid of the list.
				lp.PeopleOA = null;
				undoHelper.RollBack = false;
			}
			ClearChanges();
		}

		/// <summary>
		/// Test the get/set accessors for DateTime properties.
		/// </summary>
		[Test]
		public void kcptTimeTests()
		{
			var servLoc = Cache.ServiceLocator;
			var lp = servLoc.GetInstance<ILangProjectRepository>().AllInstances().First();
			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				lp.DateCreated = DateTime.Now;
				undoHelper.RollBack = false;
			}
			CheckChanges(1, 0, lp.Hvo, CmProjectTags.kflidDateCreated, 0, 0, 0);
			ClearChanges();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// int32 property item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void kcptIntegerTests()
		{
			var servLoc = Cache.ServiceLocator;
			var lp = servLoc.GetInstance<ILangProjectRepository>().AllInstances().First();
			ICmPossibilityList peopleList;
			ICmPerson firstPerson;
			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				lp.PeopleOA = servLoc.GetInstance<ICmPossibilityListFactory>().Create();
				peopleList = lp.PeopleOA;
				firstPerson = servLoc.GetInstance<ICmPersonFactory>().Create();
				peopleList.PossibilitiesOS.Add(firstPerson);
				undoHelper.RollBack = false;
			}
			// Two new objects (nested) should only make one PC on the top owner.
			CheckChanges(1, 0, lp.Hvo, LangProjectTags.kflidPeople, 0, 1, 0);
			ClearChanges();

			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				peopleList.Depth += 1;
				undoHelper.RollBack = false;
			}
			CheckChanges(1, 0, peopleList.Hvo, CmPossibilityListTags.kflidDepth, 0, 0, 0);
			ClearChanges();

			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				// Get rid of the list.
				lp.PeopleOA = null;
				undoHelper.RollBack = false;
			}
			ClearChanges();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// GenDate property item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void kcptGenDateTests()
		{
			var servLoc = Cache.ServiceLocator;
			var lp = servLoc.GetInstance<ILangProjectRepository>().AllInstances().First();
			ICmPossibilityList peopleList;
			ICmPerson firstPerson;
			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				lp.PeopleOA = servLoc.GetInstance<ICmPossibilityListFactory>().Create();
				peopleList = lp.PeopleOA;
				firstPerson = servLoc.GetInstance<ICmPersonFactory>().Create();
				peopleList.PossibilitiesOS.Add(firstPerson);
				undoHelper.RollBack = false;
			}
			ClearChanges();

			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				firstPerson.DateOfBirth = new GenDate(GenDate.PrecisionType.Before, 1, 1, 3000, true);
				undoHelper.RollBack = false;
			}
			CheckChanges(1, 0, firstPerson.Hvo, CmPersonTags.kflidDateOfBirth, 0, 0, 0);
			ClearChanges();

			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				// Get rid of the list.
				lp.PeopleOA = null;
				undoHelper.RollBack = false;
			}
			ClearChanges();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Guid property item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void kcptGuidTests()
		{
			ICmPossibilityListFactory possFactory = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>();
			ICmPossibilityList possList;
			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				possList = possFactory.Create();
				Cache.LanguageProject.TimeOfDayOA = possList;
				undoHelper.RollBack = false;
			}
			CheckChanges(1, 0, Cache.LanguageProject.Hvo, LangProjectTags.kflidTimeOfDay, 0, 1, 0);
			ClearChanges();

			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				possList.ListVersion = Guid.NewGuid();
				undoHelper.RollBack = false;
			}
			// Changing a prop on an unowned object does make one PC.
			CheckChanges(1, 0, possList.Hvo, CmPossibilityListTags.kflidListVersion, 0, 0, 0);
			ClearChanges();

			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				possList.ListVersion = Guid.NewGuid();
				possList.ListVersion = Guid.NewGuid();
				undoHelper.RollBack = false;
			}
			// Changing a prop twice in one UOW only makes one PC for the property.
			CheckChanges(1, 0, possList.Hvo, CmPossibilityListTags.kflidListVersion, 0, 0, 0);
			ClearChanges();

			// TODO: Delete unowned uv, as that should also not produce a PC.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add and remove atomic property item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AtomicItemTests()
		{
			var lp = Cache.ServiceLocator.GetInstance<ILangProjectRepository>().AllInstances().First();

			// Delete current value.
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () => lp.AnthroListOA = null);
			CheckChanges(1, 0, lp.Hvo, LangProjectTags.kflidAnthroList, 0, 0, 1);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(1, 0, lp.Hvo, LangProjectTags.kflidAnthroList, 0, 1, 0);
			m_actionHandler.Redo();
			ClearChanges();

			// Insert new value.
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				lp.AnthroListOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			});
			CheckChanges(1, 0, lp.Hvo, LangProjectTags.kflidAnthroList, 0, 1, 0);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(1, 0, lp.Hvo, LangProjectTags.kflidAnthroList, 0, 0, 1);
			m_actionHandler.Redo();
			ClearChanges();

			// Replace current non-null value.
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				lp.AnthroListOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			});
			CheckChanges(1, 0, lp.Hvo, LangProjectTags.kflidAnthroList, 0, 1, 1);
			ClearChanges();
			m_actionHandler.Undo();
			CheckChanges(1, 0, lp.Hvo, LangProjectTags.kflidAnthroList, 0, 1, 1);
			m_actionHandler.Redo();
			ClearChanges();
		}

		/// <summary>
		/// Test the a custom property.
		/// </summary>
		[Test]
		public void CustomPropertyTests()
		{
			var servLoc = Cache.ServiceLocator;
			var sda = Cache.DomainDataByFlid;
			var mdc = servLoc.GetInstance<IFwMetaDataCacheManaged>();
			var customCertifiedFlid = mdc.AddCustomField("WfiWordform", "Certified", CellarPropertyType.Boolean, 0);
			var customITsStringFlid = mdc.AddCustomField("WfiWordform", "NewTsStringProp", CellarPropertyType.String, 0);
			var customMultiUnicodeFlid = mdc.AddCustomField("WfiWordform", "MultiUnicodeProp", CellarPropertyType.MultiUnicode, 0);
			var customAtomicReferenceFlid = mdc.AddCustomField("WfiWordform", "NewAtomicRef", CellarPropertyType.ReferenceAtomic, CmPersonTags.kClassId);
			var lp = servLoc.GetInstance<ILangProjectRepository>().AllInstances().First();

			// Add wordform & person.
			IWfiWordform wf;
			ICmPerson person;
			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				// This does not fire a PC, since the wf is not owned.
				wf = servLoc.GetInstance<IWfiWordformFactory>().Create();

				lp.PeopleOA = servLoc.GetInstance<ICmPossibilityListFactory>().Create();
				var personFactory = servLoc.GetInstance<ICmPersonFactory>();
				person = personFactory.Create();
				lp.PeopleOA.PossibilitiesOS.Add(person);
				undoHelper.RollBack = false;
			}
			CheckChanges(1, -1, 0, 0, 0, 0, 0);
			ClearChanges();

			// Set custom value.
			var userWs = Cache.WritingSystemFactory.UserWs;
			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				sda.SetBoolean(wf.Hvo, customCertifiedFlid, false);
				var tsf = Cache.TsStrFactory;
				sda.SetString(wf.Hvo, customITsStringFlid,
					tsf.MakeString("New ITsString", userWs));
				sda.SetMultiStringAlt(wf.Hvo, customMultiUnicodeFlid,
					userWs, tsf.MakeString("New unicode ITsString", userWs));
				sda.SetObjProp(wf.Hvo, customAtomicReferenceFlid, person.Hvo);
				undoHelper.RollBack = false;
			}
			CheckChanges(4, 0, wf.Hvo, customCertifiedFlid, 0, 0, 0);
			CheckChanges(4, 1, wf.Hvo, customITsStringFlid, 0, 0, 0);
			CheckChanges(4, 2, wf.Hvo, customMultiUnicodeFlid, userWs, 0, 0);
			CheckChanges(4, 3, wf.Hvo, customAtomicReferenceFlid, 0, 0, 0);
			ClearChanges();

			using (var undoHelper = new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				// Get rid of the list.
				lp.PeopleOA = null;
				undoHelper.RollBack = false;
			}
			ClearChanges();
		}
	}

	/// <summary>
	/// Test case notification recipient.
	/// </summary>
	public class Notifiee : IVwNotifyChange
	{
		private readonly List<ChangeInformationTest> m_changes = new List<ChangeInformationTest>();

		/// <summary>
		/// Get the list of changes.
		/// </summary>
		public List<ChangeInformationTest> Changes
		{
			get { return m_changes; }
		}

		#region Implementation of IVwNotifyChange

		/// <summary>
		/// Informs the recipient that a property of an object has changed. In some cases, may
		/// provide useful information about how much of it changed. Some objects
		/// reporting changes may not have full information about the extent of the change, in
		/// which case, they should err on the side of exaggerating it, for example by
		/// pretending that all objects were deleted and a new group inserted.
		///</summary>
		/// <param name='hvo'>The object that changed </param>
		/// <param name='tag'>The property that changed </param>
		/// <param name='ivMin'>
		/// For vectors, the starting index where the change occurred.
		/// For MultiStrings, the writing system where the change occurred.
		/// </param>
		/// <param name='cvIns'>
		/// For vectors, the number of items inserted.
		/// For atomic objects, 1 if an item was added.
		/// Otherwise (including basic properties), 0.
		/// </param>
		/// <param name='cvDel'>
		/// For vectors, the number of items deleted.
		/// For atomic objects, 1 if an item was deleted.
		/// Otherwise (including basic properties), 0.
		/// </param>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			// Best to not collect this change. Most tests don't care about it, so it' at best one more
			// change to list. But worse, it can be unpredictable: if we do two units of work which
			// affect the same object, they may or may not occur within the same 100ns, and so the
			// date modified may or may not change.
			// May need to add a few other cases here if we use the relevant classes in tests where we count changes.
			if (tag == LexEntryTags.kflidDateModified || tag == CmProjectTags.kflidDateModified || tag == CmMajorObjectTags.kflidDateModified || tag == CmPossibilityTags.kflidDateModified)
				return;
			m_changes.Add(new ChangeInformationTest(hvo, tag, ivMin, cvIns, cvDel));
		}

		#endregion

		/// <summary>
		/// Check that the list of changes contains all the expected changes.
		/// </summary>
		public void CheckChanges(ChangeInformationTest[] rgExpectedChanges, string label)
		{
			Assert.AreEqual(rgExpectedChanges.Length, Changes.Count, label + " wrong number of changes");
			foreach (ChangeInformationTest c in rgExpectedChanges)
				CheckChange(c, label);
		}

		/// <summary>
		/// Check that the list of changes contains all the expected changes.
		/// In this case, we don't care if other changes exist too.
		/// </summary>
		public void CheckChangesWeaker(ChangeInformationTest[] rgExpectedChanges, string label)
		{
			foreach (ChangeInformationTest c in rgExpectedChanges)
				CheckChange(c, label);
		}

		/// <summary>
		/// Check that we have the specified change.
		/// </summary>
		public void CheckChange(ChangeInformationTest c, string label)
		{
			if (IsValidChange(c))
				return;
			Assert.Fail(label + ": Expected change not found " + c.Hvo + ", " + c.Tag + ", " + c.IvMin + ", " + c.CvIns + ", " + c.CvDel);
		}

		/// <summary>
		/// Verify that we have the specified change.
		/// </summary>
		internal bool IsValidChange(ChangeInformationTest c)
		{
			foreach (ChangeInformationTest c2 in Changes)
			{
				if (c.Hvo == c2.Hvo && c.Tag == c2.Tag && c.IvMin == c2.IvMin && c.CvIns == c2.CvIns && c.CvDel == c2.CvDel)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Clears out the list of changes.
		/// </summary>
		public void ClearChanges()
		{
			Changes.Clear();
		}
	}

	/// <summary>
	/// Class that registers changes that come to the Notifiee class.
	///
	/// Notifiee will hold one, or more, ChangeInformationTest instances.
	/// </summary>
	public class ChangeInformationTest
	{
		private readonly int m_hvo;
		private readonly int m_tag;
		private readonly int m_ivMin;
		private readonly int m_cvIns;
		private readonly int m_cvDel;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name='hvo'>The object that changed </param>
		/// <param name='tag'>The property that changed </param>
		/// <param name='ivMin'>
		/// For vectors, the starting index where the change occurred.
		/// For MultiStrings, the writing system where the change occurred.
		/// </param>
		/// <param name='cvIns'>
		/// For vectors, the number of items inserted.
		/// For atomic objects, 1 if an item was added.
		/// Otherwise (including basic properties), 0.
		/// </param>
		/// <param name='cvDel'>
		/// For vectors, the number of items deleted.
		/// For atomic objects, 1 if an item was deleted.
		/// Otherwise (including basic properties), 0.
		/// </param>
		public ChangeInformationTest(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			m_hvo = hvo;
			m_tag = tag;
			m_ivMin = ivMin;
			m_cvIns = cvIns;
			m_cvDel = cvDel;
		}

		/// <summary>
		/// object modified
		/// </summary>
		public int Hvo
		{
			get { return m_hvo; }
		}

		/// <summary>
		/// Property modified
		/// </summary>
		public int Tag
		{
			get { return m_tag; }
		}

		/// <summary>
		/// Index of first object deleted, or if cvDel is zero, object to insert before
		/// </summary>
		public int IvMin
		{
			get { return m_ivMin; }
		}

		/// <summary>
		/// Number of objects inserted (first at IvMin)
		/// </summary>
		public int CvIns
		{
			get { return m_cvIns; }
		}

		/// <summary>
		/// Number of objects to delete, starting from IvMin.
		/// </summary>
		public int CvDel
		{
			get { return m_cvDel; }
		}
	}
}
