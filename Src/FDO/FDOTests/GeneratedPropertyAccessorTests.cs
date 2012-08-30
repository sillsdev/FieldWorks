// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: GeneratedPropertyAccessorTests.cs
// Responsibility: Randy regnier
// Last reviewed:
//
// <remarks>
// Tests the model property accessors for the typical generated FDO properties.
// It does not test each one, but each flid type,
// since the generator works the same for each flid type.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO.CoreTests.GeneratedModelTests
{
	/// <summary>
	/// Class that tests the most basic generated properties.
	/// We test these flidtypes here:
	/// CellarPropertyType.Boolean:
	/// CellarPropertyType.Integer:
	/// CellarPropertyType.Time:
	/// CellarPropertyType.Guid:
	/// CellarPropertyType.GenDate:
	/// CellarPropertyType.Binary:
	/// CellarPropertyType.Unicode: (string)
	/// CellarPropertyType.String: (ITsString)
	/// CellarPropertyType.Numeric: (Not used in model.)
	/// CellarPropertyType.Float: (Not used in model.)
	/// CellarPropertyType.Image: (Not used in model.)
	/// </summary>
	[TestFixture]
	public class BasicPropertyAccessorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ICmPossibilityListFactory m_possListFactory;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			var servLoc = Cache.ServiceLocator;
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, ()=>
			{
				Cache.LanguageProject.PeopleOA = servLoc.GetInstance<ICmPossibilityListFactory>().Create();
				Cache.LanguageProject.PeopleOA.PossibilitiesOS.Add(servLoc.GetInstance<ICmPersonFactory>().Create());
			});
			m_possListFactory = servLoc.GetInstance<ICmPossibilityListFactory>();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureTeardown()
		{
			m_possListFactory = null;

			base.FixtureTeardown();
		}

		/// <summary>
		/// Test the get/set accessors for boolean properties.
		/// </summary>
		[Test]
		public void kcptBooleanTests()
		{
			var startVal = Cache.LanguageProject.PeopleOA.IsVernacular;
			Cache.LanguageProject.PeopleOA.IsVernacular = !startVal;
			Assert.AreEqual(!startVal, Cache.LanguageProject.PeopleOA.IsVernacular);
			// Reset it to original value..
			Cache.LanguageProject.PeopleOA.IsVernacular = startVal;
			Assert.AreEqual(startVal, Cache.LanguageProject.PeopleOA.IsVernacular);
		}

		/// <summary>
		/// Test the get/set accessors for integer properties.
		/// </summary>
		[Test]
		public void kcptIntegerTests()
		{
			var startValue = Cache.LanguageProject.PeopleOA.Depth;
			Cache.LanguageProject.PeopleOA.Depth = startValue + 1;
			Assert.AreEqual(startValue, Cache.LanguageProject.PeopleOA.Depth - 1);
			// Reset it to original value.
			Cache.LanguageProject.PeopleOA.Depth = startValue;
			Assert.AreEqual(startValue, Cache.LanguageProject.PeopleOA.Depth);
		}

		/// <summary>
		/// Test the get/set accessors for DateTime properties.
		/// </summary>
		[Test]
		public void kcptTimeTests()
		{
			var startValue = Cache.LanguageProject.DateCreated;
			var now = DateTime.Now;
			Cache.LanguageProject.DateCreated = now;
			Assert.AreEqual(now, Cache.LanguageProject.DateCreated);
			// Reset it to original value..
			Cache.LanguageProject.DateCreated = startValue;
			Assert.AreEqual(startValue, Cache.LanguageProject.DateCreated);
		}

		/// <summary>
		/// Test the get/set accessors for Guid properties.
		/// </summary>
		[Test]
		public void kcptGuidTests()
		{
			var poss = m_possListFactory.Create();
			Cache.LanguageProject.TimeOfDayOA = poss;

			poss.ListVersion = Guid.NewGuid();
			var startingValue = poss.ListVersion;
			Assert.AreEqual(startingValue, poss.ListVersion);
			var newAppGuid = Guid.NewGuid();
			poss.ListVersion = newAppGuid;
			Assert.AreEqual(newAppGuid, poss.ListVersion);
			// Reset it to original value.
			poss.ListVersion = startingValue;
			Assert.AreEqual(startingValue, poss.ListVersion);
		}

		/// <summary>
		/// Test the get/set accessors for general date properties.
		/// </summary>
		[Test]
		public void kcptGenDateTests()
		{
			var firstPerson = (ICmPerson)Cache.LanguageProject.PeopleOA.PossibilitiesOS[0];
			var startValue = firstPerson.DateOfBirth;
			var newValue = new GenDate(GenDate.PrecisionType.Before, 1, 1, 3000, true);
			firstPerson.DateOfBirth = newValue;
			Assert.AreEqual(newValue, firstPerson.DateOfBirth);
			// Reset it to original value.
			firstPerson.DateOfBirth = startValue;
			Assert.AreEqual(startValue, firstPerson.DateOfBirth);
		}

		/// <summary>
		/// Test the get/set accessors for binary data properties.
		/// </summary>
		[Test]
		public void kcptBinaryTests()
		{
			IUserConfigAcct acct = Cache.ServiceLocator.GetInstance<IUserConfigAcctFactory>().Create();
			Cache.LanguageProject.UserAccountsOC.Add(acct);
			acct.Sid = new byte[] { 4, 5 };
			var startingValue = acct.Sid;
			var newValue = new byte[] { 1, 2, 3 };
			acct.Sid = newValue;
			Assert.AreEqual(newValue, acct.Sid);
			// Reset it to original value.
			acct.Sid = startingValue;
			Assert.AreEqual(startingValue, acct.Sid);
		}

		/// <summary>
		/// Test the get/set accessors for normal unicode properties (kcptUnicode).
		/// </summary>
		[Test]
		public void kcptUnicodeTests()
		{
			var startValue = Cache.LanguageProject.EthnologueCode;
			const string newValue = "NEWCode";
			Cache.LanguageProject.EthnologueCode = newValue;
			Assert.AreEqual(newValue, Cache.LanguageProject.EthnologueCode);
			// Reset it to original value.
			Cache.LanguageProject.EthnologueCode = startValue;
			Assert.AreEqual(startValue, Cache.LanguageProject.EthnologueCode);
		}

		/// <summary>
		/// Test the get/set accessors for normal ITsString properties (kcptString).
		/// </summary>
		[Test]
		public void kcptStringTests()
		{
			var le = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var defValue = le.ImportResidue;
			Assert.IsNull(defValue.Text, "Default for null property should have null for the Text of the returned ITsString.");

			var irOriginalValue = Cache.TsStrFactory.MakeString("import residue",
				Cache.WritingSystemFactory.UserWs);
			le.ImportResidue = irOriginalValue;
			Assert.AreEqual(irOriginalValue, le.ImportResidue);

			// Set to new value.
			var irNewValue = Cache.TsStrFactory.MakeString("new import residue",
				Cache.WritingSystemFactory.UserWs);
			le.ImportResidue = irNewValue;
			Assert.AreEqual(irNewValue, le.ImportResidue);

			// Reset it to original value.
			le.ImportResidue = irOriginalValue;
			Assert.AreEqual(irOriginalValue, le.ImportResidue);
		}
	}

	/// <summary>
	/// Test the owning and reference atomic property accessors.
	/// </summary>
	[TestFixture]
	public class AtomicPropertyAccessorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Check the get/set generated atomic reference property.
		/// </summary>
		[Test]
		public void AtomicReferencePropertyAccessorTest()
		{
			var servLoc = Cache.ServiceLocator;
			var ann = servLoc.GetInstance<ICmBaseAnnotationFactory>().Create();
			Cache.LanguageProject.AnnotationsOC.Add(ann);
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoObjectDeleted, ann.Hvo);
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoUninitializedObject, ann.Hvo);
			Assert.IsNull(ann.SourceRA, "Not null original value.");

			var agentFactory = servLoc.GetInstance<ICmAgentFactory>();
			var agent1 = agentFactory.Create();
			Cache.LanguageProject.AnalyzingAgentsOC.Add(agent1);
			var agent2 = agentFactory.Create();
			Cache.LanguageProject.AnalyzingAgentsOC.Add(agent2);
			try
			{
				ann.SourceRA = agent1;
				Assert.AreEqual(agent1, ann.SourceRA);

				ann.SourceRA = agent2;
				Assert.AreEqual(agent2, ann.SourceRA);
				ann.SourceRA = null;
				Assert.IsNull(ann.SourceRA, "Not null original value.");

			}
			finally
			{
				// Remove the annotation.
				Cache.LanguageProject.AnnotationsOC.Remove(ann);
				Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, ann.Hvo);
				// Remove the two agents.
				Cache.LanguageProject.AnalyzingAgentsOC.Clear();
				Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, agent1.Hvo);
				Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, agent2.Hvo);
			}
		}

		/// <summary>
		/// Tests various things related to cleaning up incoming references.
		/// </summary>
		[Test]
		public void IncomingReferenceTest()
		{
			var servLoc = Cache.ServiceLocator;

			var leFact = servLoc.GetInstance<ILexEntryFactory>();
			var le1 = leFact.Create();

			var lsFact = servLoc.GetInstance<ILexSenseFactory>();
			var ls1 = lsFact.Create();
			le1.SensesOS.Add(ls1);
			var ls2 = lsFact.Create();
			le1.SensesOS.Add(ls2);

			if (Cache.LangProject.StatusOA == null)
			{
				Cache.LangProject.StatusOA = servLoc.GetInstance<ICmPossibilityListFactory>().Create();
			}
			var possFact = servLoc.GetInstance<ICmPossibilityFactory>();
			var status1 = possFact.Create();
			Cache.LangProject.StatusOA.PossibilitiesOS.Add(status1);
			var status2 = possFact.Create();
			Cache.LangProject.StatusOA.PossibilitiesOS.Add(status2);

			ls1.StatusRA = status1;
			ls1.SenseTypeRA = status1; // pathological, but we want two atomic refs from same source to same dest.

			Assert.AreEqual(1, status1.ReferringObjects.Count);
			Assert.AreEqual(ls1, status1.ReferringObjects.First());

			((IReferenceSource) ls1).RemoveAReference(status1);
			Assert.IsTrue(ls1.StatusRA == null || ls1.SenseTypeRA == null);
			Assert.IsFalse(ls1.StatusRA == null && ls1.SenseTypeRA == null);
			Assert.AreEqual(1, status1.ReferringObjects.Count, "removing one reference from a source that has two should keep it in the set");
			Assert.AreEqual(ls1, status1.ReferringObjects.First());

			((IReferenceSource)ls1).RemoveAReference(status1);
			Assert.IsNull(ls1.StatusRA);
			Assert.IsNull(ls1.SenseTypeRA);
			Assert.AreEqual(0, status1.ReferringObjects.Count);

			le1.MainEntriesOrSensesRS.Add(ls1);
			le1.MainEntriesOrSensesRS.Add(ls2);
			le1.MainEntriesOrSensesRS.Add(ls1);
			Assert.AreEqual(1, ls1.ReferringObjects.Count);
			Assert.AreEqual(le1, ls1.ReferringObjects.First());

			((IReferenceSource)le1.MainEntriesOrSensesRS).RemoveAReference(ls1);
			Assert.AreEqual(2, le1.MainEntriesOrSensesRS.Count);
			Assert.AreEqual(ls2, le1.MainEntriesOrSensesRS[0]);
			Assert.AreEqual(ls1, le1.MainEntriesOrSensesRS[1]);
			Assert.AreEqual(1, ls1.ReferringObjects.Count);
			Assert.AreEqual(le1, ls1.ReferringObjects.First());

			((IReferenceSource)le1.MainEntriesOrSensesRS).RemoveAReference(ls1);
			Assert.AreEqual(1, le1.MainEntriesOrSensesRS.Count);
			Assert.AreEqual(ls2, le1.MainEntriesOrSensesRS[0]);
			Assert.AreEqual(0, ls1.ReferringObjects.Count);

			// Similarly ref collection (again abusing the expected destination somewhat).
			ls1.DomainTypesRC.Add(status1);
			Assert.AreEqual(1, status1.ReferringObjects.Count);
			Assert.AreEqual(ls1, status1.ReferringObjects.First());

			ls1.DomainTypesRC.Add(status2);
			ls1.ThesaurusItemsRC.Add(status1);
			Assert.AreEqual(1, status1.ReferringObjects.Count);
			Assert.AreEqual(ls1, status1.ReferringObjects.First());

			((IReferenceSource)ls1.DomainTypesRC).RemoveAReference(status1);
			Assert.AreEqual(1,ls1.DomainTypesRC.Count);
			Assert.AreEqual(status2, ls1.DomainTypesRC.ToArray()[0]);
			Assert.AreEqual(1, ls1.ThesaurusItemsRC.Count);
			Assert.AreEqual(1, status1.ReferringObjects.Count);
			Assert.AreEqual(ls1, status1.ReferringObjects.First());

			((IReferenceSource)ls1.ThesaurusItemsRC).RemoveAReference(status1);
			Assert.AreEqual(0, status1.ReferringObjects.Count);
		}

		/// <summary>
		/// Check that Undo and Redo properly adjust incoming refs.
		/// </summary>
		[Test]
		public void UndoRedoIncomingRefs()
		{
			var servLoc = Cache.ServiceLocator;

			var leFact = servLoc.GetInstance<ILexEntryFactory>();
			var le1 = leFact.Create();

			var lsFact = servLoc.GetInstance<ILexSenseFactory>();
			var ls1 = lsFact.Create();
			le1.SensesOS.Add(ls1);

			if (Cache.LangProject.StatusOA == null)
			{
				Cache.LangProject.StatusOA = servLoc.GetInstance<ICmPossibilityListFactory>().Create();
			}
			var possFact = servLoc.GetInstance<ICmPossibilityFactory>();
			var status1 = possFact.Create();
			Cache.LangProject.StatusOA.PossibilitiesOS.Add(status1);

			Cache.ActionHandlerAccessor.EndUndoTask(); // so we can have our own units of work to test Undo

			// Atomic
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () => ls1.StatusRA = status1);
			Assert.AreEqual(1, status1.ReferringObjects.Count);
			m_actionHandler.Undo();
			Assert.AreEqual(0, status1.ReferringObjects.Count);
			m_actionHandler.Redo();
			Assert.AreEqual(1, status1.ReferringObjects.Count);
			m_actionHandler.Undo(); // leave it Undone so it doesn't affect the ref collection test below.
			Assert.AreEqual(0, status1.ReferringObjects.Count);

			// Ref sequence
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () => le1.MainEntriesOrSensesRS.Add(ls1));
			Assert.AreEqual(1, ls1.ReferringObjects.Count);
			m_actionHandler.Undo();
			Assert.AreEqual(0, ls1.ReferringObjects.Count);
			m_actionHandler.Redo();
			Assert.AreEqual(1, ls1.ReferringObjects.Count);
			m_actionHandler.Undo(); // cleanup
			Assert.AreEqual(0, ls1.ReferringObjects.Count);

			// Ref collection
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () => ls1.DomainTypesRC.Add(status1));
			Assert.AreEqual(1, status1.ReferringObjects.Count);
			m_actionHandler.Undo();
			Assert.AreEqual(0, status1.ReferringObjects.Count);
			m_actionHandler.Redo();
			Assert.AreEqual(1, status1.ReferringObjects.Count);
			m_actionHandler.Undo(); // cleanup
			Assert.AreEqual(0, status1.ReferringObjects.Count);

			ILexSense ls2 = null;
			// Now see if it happens properly when we Undo and Redo object creation and deletion.
			// Atomic
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler,
				() =>
				{
					ls2 = lsFact.Create();
					le1.SensesOS.Add(ls2);
					ls2.StatusRA = status1;
				});
			Assert.AreEqual(1, status1.ReferringObjects.Count);
			m_actionHandler.Undo();
			Assert.AreEqual(0, status1.ReferringObjects.Count);
			m_actionHandler.Redo();
			Assert.AreEqual(1, status1.ReferringObjects.Count);
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler,
				() => le1.SensesOS.Remove(ls2));
			Assert.AreEqual(0, status1.ReferringObjects.Count);
			m_actionHandler.Undo();
			Assert.AreEqual(1, status1.ReferringObjects.Count, "incoming ref should come back on undoing delete");
			m_actionHandler.Redo();
			Assert.AreEqual(0, status1.ReferringObjects.Count, "incoming ref should go away on redoing delete");

			// collection
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler,
				() =>
					{
						ls2 = lsFact.Create();
						le1.SensesOS.Add(ls2);
						ls2.DomainTypesRC.Add(status1);
					});
			Assert.AreEqual(1, status1.ReferringObjects.Count);
			m_actionHandler.Undo();
			Assert.AreEqual(0, status1.ReferringObjects.Count);
			m_actionHandler.Redo();
			Assert.AreEqual(1, status1.ReferringObjects.Count);
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler,
				() => le1.SensesOS.Remove(ls2));
			Assert.AreEqual(0, status1.ReferringObjects.Count);
			m_actionHandler.Undo();
			Assert.AreEqual(1, status1.ReferringObjects.Count, "incoming ref should come back on undoing delete");
			m_actionHandler.Redo();
			Assert.AreEqual(0, status1.ReferringObjects.Count, "incoming ref should go away on redoing delete");

			// sequence
			ILexEntry le2 = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler,
				() =>
				{
					le2 = leFact.Create();
					le2.MainEntriesOrSensesRS.Add(ls1);
				});
			Assert.AreEqual(1, ls1.ReferringObjects.Count);
			m_actionHandler.Undo();
			Assert.AreEqual(0, ls1.ReferringObjects.Count);
			m_actionHandler.Redo();
			Assert.AreEqual(1, ls1.ReferringObjects.Count);
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler,
				() => le2.Delete());
			Assert.AreEqual(0, ls1.ReferringObjects.Count);
			m_actionHandler.Undo();
			Assert.AreEqual(1, ls1.ReferringObjects.Count, "incoming ref should come back on undoing delete");
			m_actionHandler.Redo();
			Assert.AreEqual(0, ls1.ReferringObjects.Count, "incoming ref should go away on redoing delete");

			// The base class for this group of tests expects to end an Undo task after the test is over.
			Cache.ActionHandlerAccessor.BeginUndoTask("undo something", "redo something");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check the get/set generated atomic owning property when setting to an initial value,
		/// changing it to another value, and setting it to null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AtomicOwningPropertyAccessorTest_SetAndChange()
		{
			// GJM -- 22 June 2010:
			// In order to implement FWR-133 Allow Custom Lists, ICmPossibilityListFactory needs
			// to be able to create unowned lists. Hence the 2 lines commented out below.
			ICmPossibilityListFactory possListFactory = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>();
			ICmPossibilityList possList1 = possListFactory.Create();
			//Assert.AreEqual((int)SpecialHVOValues.kHvoUninitializedObject, possList1.Hvo);

			Cache.LanguageProject.LocationsOA = possList1;
			Assert.AreEqual(possList1, Cache.LanguageProject.LocationsOA);
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoObjectDeleted, possList1.Hvo);
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoUninitializedObject, possList1.Hvo);
			Assert.AreEqual(Cache.LanguageProject, possList1.Owner);
			Assert.AreEqual(LangProjectTags.kflidLocations, possList1.OwningFlid);

			ICmPossibilityList possList2 = possListFactory.Create();
			//Assert.AreEqual((int)SpecialHVOValues.kHvoUninitializedObject, possList2.Hvo);
			Cache.LanguageProject.LocationsOA = possList2;
			Assert.AreEqual(possList2, Cache.LanguageProject.LocationsOA);
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoObjectDeleted, possList2.Hvo);
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoUninitializedObject, possList2.Hvo);
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, possList1.Hvo);

			Cache.LanguageProject.LocationsOA = null;
			Assert.IsNull(Cache.LanguageProject.LocationsOA);
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, possList2.Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check the get/set generated atomic owning property when an object's owner is changed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AtomicOwningPropertyAccessor_ChangeOwner()
		{
			ICmPossibilityList posList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			Cache.LanguageProject.LocationsOA = posList;
			Assert.AreEqual(posList, Cache.LanguageProject.LocationsOA);
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoObjectDeleted, posList.Hvo);
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoUninitializedObject, posList.Hvo);
			Assert.AreEqual(Cache.LanguageProject, posList.Owner);
			Assert.AreEqual(LangProjectTags.kflidLocations, posList.OwningFlid);

			// Now change the list to be owned by a different object
			var lexDb = Cache.LanguageProject.LexDbOA;
			lexDb.ComplexEntryTypesOA = posList;
			Assert.AreEqual(posList, lexDb.ComplexEntryTypesOA);
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoObjectDeleted, posList.Hvo);
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoUninitializedObject, posList.Hvo);
			Assert.AreEqual(lexDb, posList.Owner);
			Assert.AreEqual(LexDbTags.kflidComplexEntryTypes, posList.OwningFlid);
			Assert.IsNull(Cache.LanguageProject.PeopleOA);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check the get/set generated atomic owning property when an object's ownership is
		/// changed from one atomic property to another, but with the same owner.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AtomicOwningPropertyAccessor_ChangeOwningFlid()
		{
			ICmPossibilityList posList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			Cache.LanguageProject.LocationsOA = posList;
			Assert.AreEqual(posList, Cache.LanguageProject.LocationsOA);
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoObjectDeleted, posList.Hvo);
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoUninitializedObject, posList.Hvo);
			Assert.AreEqual(Cache.LanguageProject, posList.Owner);
			Assert.AreEqual(LangProjectTags.kflidLocations, posList.OwningFlid);

			// Now change the list to be owned in a different field of the same object
			Cache.LanguageProject.PeopleOA = posList;
			Assert.AreEqual(posList, Cache.LanguageProject.PeopleOA);
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoObjectDeleted, posList.Hvo);
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoUninitializedObject, posList.Hvo);
			Assert.AreEqual(Cache.LanguageProject, posList.Owner);
			Assert.AreEqual(LangProjectTags.kflidPeople, posList.OwningFlid);
			Assert.IsNull(Cache.LanguageProject.LocationsOA);
		}
	}
}