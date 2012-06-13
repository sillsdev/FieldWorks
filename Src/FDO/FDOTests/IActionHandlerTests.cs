// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IActionHandlerTests.cs
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure.Impl;
using SIL.Utils;
using System.Reflection;
using Rhino.Mocks;
using System.Collections;

namespace SIL.FieldWorks.FDO.CoreTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the FdoCache IActionHanlder implementation.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class UnitOfWorkTests : MemoryOnlyBackendProviderTestBase
	{
		#region MockUndoAction class
		internal class MockUndoAction : IFdoPropertyChanged
		{
			private const int kflidBogusProp = 432543;

			internal List<MockUndoAction> AllActions { get; set; }
			internal bool HasBeenUndone { get; set; }
			internal bool PropChangeWasIssued { get; set; }
			internal string Name { get; set; }

			internal MockUndoAction(bool isDataChange, string name)
			{
				IsDataChange = isDataChange;
				Name = name;
			}

			internal bool HasBeenRedone
			{
				get { return !HasBeenUndone; }
				set { HasBeenUndone = !value; }
			}

			#region IUndoAction Members

			public void Commit()
			{
			}

			public bool IsDataChange { get; private set; }

			public bool IsRedoable
			{
				get { return true; }
			}

			public bool Redo()
			{
				bool fFoundMyself = false;
				foreach (MockUndoAction action in AllActions)
				{
					if (action == this)
						fFoundMyself = true;
					else if (fFoundMyself)
					{
						if (!this.IsDataChange && action.IsDataChange)
							Assert.IsTrue(action.HasBeenRedone, "Data change action " + action.Name + " should have been redone before " + Name + " and all other non-data changes were redone.");
						else
							Assert.IsFalse(action.HasBeenRedone, "Action " + action.Name + " should not have been redone before action " + Name);
					}
					else
					{
						if (this.IsDataChange && !action.IsDataChange)
							Assert.IsFalse(action.HasBeenRedone, "Non-data change action " + action.Name + " should not have been redone until " + Name + " and all other data changes were redone.");
						else
							Assert.IsTrue(action.HasBeenRedone, "Action " + action.Name + " should have been redone before action " + Name);
					}
				}
				Assert.AreEqual(!IsDataChange, PropChangeWasIssued, "At the time redo is called " +
					(IsDataChange ? "for data change " + Name + ", PropChanged should not yet have been called." :
					"for non-data change " + Name + ", PropChanged should already have been called."));
				HasBeenRedone = true;
				return true;
			}

			public bool SuppressNotification
			{
				set { }
			}

			public bool Undo()
			{
				bool fFoundMyself = false;
				foreach (MockUndoAction action in AllActions)
				{
					if (action == this)
						fFoundMyself = true;
					else if (fFoundMyself)
					{
						if (this.IsDataChange && !action.IsDataChange)
							Assert.IsFalse(action.HasBeenUndone, "Non-data change action " + action.Name + " should not have been undone until " + Name + " and all other data changes were undone.");
						else
							Assert.IsTrue(action.HasBeenUndone, "Action " + action.Name + " should have been undone before action " + Name);
					}
					else
					{
						if (!this.IsDataChange && action.IsDataChange)
							Assert.IsTrue(action.HasBeenUndone, "Data change action " + action.Name + " should have been undone before " + Name + " and all other non-data changes were undone.");
						else
							Assert.IsFalse(action.HasBeenUndone, "Action " + action.Name + " should not have been undone before action " + Name);
					}
				}
				Assert.AreEqual(!IsDataChange, PropChangeWasIssued, "At the time undo is called " +
					(IsDataChange ? "for data change " + Name + ", PropChanged should not yet have been called." :
					"for non-data change " + Name + ", PropChanged should already have been called."));
				HasBeenUndone = true;
				return true;
			}
			#endregion

			#region IFdoPropertyChanged Members

			public ChangeInformation GetChangeInfo(bool fForUndo)
			{
				if (fForUndo)
					Assert.IsTrue(HasBeenUndone || !IsDataChange);
				else
					Assert.IsTrue(HasBeenRedone || !IsDataChange);

				// This doesn't really *issue* the PropChanged, but we assume that if this
				// method gets called, the caller is using the value to call PropChanged.
				PropChangeWasIssued = true;
				return new ChangeInformation(MockRepository.GenerateStub<ICmObject>(), kflidBogusProp, 0, 0, 0);
			}

			/// <summary>
			/// Gets a value indicating whether the changed object is (now) deleted or uninitialized.
			/// In tests, we always consider the value to be valid.
			/// </summary>
			public bool ObjectIsInvalid
			{
				get { return false; }
			}

			#endregion
		}
		#endregion

		private IWfiWordformFactory m_wordFormFactory;
		private IUnitOfWorkService m_uowService;

		/// <summary>
		///
		/// </summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_wordFormFactory = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>();

			var servLoc = Cache.ServiceLocator;
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
				{
					Cache.LanguageProject.PeopleOA = servLoc.GetInstance<ICmPossibilityListFactory>().Create();
					Cache.LanguageProject.PeopleOA.PossibilitiesOS.Add(servLoc.GetInstance<ICmPersonFactory>().Create());
					// force Scripture reference system to be created - along with TranslatedScriptureOA
					Cache.LangProject.TranslatedScriptureOA = servLoc.GetInstance<IScriptureFactory>().Create();
				});
			m_uowService = Cache.ServiceLocator.GetInstance<IUnitOfWorkService>();
		}

		/// <summary>
		/// Set variable to null;
		/// </summary>
		public override void FixtureTeardown()
		{
			m_wordFormFactory = null;

			base.FixtureTeardown();
		}

		/// <summary>
		/// Undo remaining stuff.
		/// </summary>
		public override void TestTearDown()
		{
			while (m_actionHandler.CanUndo())
				Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			((IActionHandlerExtensions)m_actionHandler).ClearAllMarks();
			m_actionHandler.Commit();
			m_actionHandler.CreateMarkIfNeeded(false);

			base.TestTearDown();
		}

		/// <summary>
		/// End an undoable task without starting it first.
		/// </summary>
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void EndUndoableTaskWithNoBeginTest()
		{
			m_actionHandler.EndUndoTask();
		}

		/// <summary>
		/// End a non-undoable teask without starting it first.
		/// </summary>
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void EndNonUndoableTaskWithNoBeginTest()
		{
			m_actionHandler.EndNonUndoableTask();
		}

		/// <summary>
		/// Make sure we can undo/redo a good string.
		/// </summary>
		[Test]
		public void MultiStringTest()
		{
			// Start with expected information.
			var english = Cache.LanguageProject.CurrentAnalysisWritingSystems.First();
			Assert.AreEqual(0, Cache.LanguageProject.MainCountry.StringCount, "Wrong number of alternatives for Cache.LanguageProject.MainCountryAccessor");

			// Create a good string.
			var firstNewValue = Cache.TsStrFactory.MakeString("Mexico", english.Handle);
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add MainCountry alt"))
			{
				Cache.LanguageProject.MainCountry.set_String(english.Handle, firstNewValue);
				undoHelper.RollBack = false;
			}

			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(0, Cache.LanguageProject.MainCountry.StringCount, "Wrong number of alternatives for Cache.LanguageProject.MainCountryAccessor");
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.AreEqual(1, Cache.LanguageProject.MainCountry.StringCount, "Wrong number of alternatives for Cache.LanguageProject.MainCountryAccessor");
			Assert.AreSame(firstNewValue, Cache.LanguageProject.MainCountry.get_String(english.Handle));

			var secondNewValue = Cache.TsStrFactory.MakeString("USA", english.Handle);
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add MainCountry alt"))
			{
				Cache.LanguageProject.MainCountry.set_String(english.Handle, secondNewValue);
				undoHelper.RollBack = false;
			}

			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(1, Cache.LanguageProject.MainCountry.StringCount, "Wrong number of alternatives for Cache.LanguageProject.MainCountryAccessor");
			Assert.AreSame(firstNewValue, Cache.LanguageProject.MainCountry.get_String(english.Handle));
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.AreEqual(1, Cache.LanguageProject.MainCountry.StringCount, "Wrong number of alternatives for Cache.LanguageProject.MainCountryAccessor");
			Assert.AreSame(secondNewValue, Cache.LanguageProject.MainCountry.get_String(english.Handle));
		}

		/// <summary>
		/// Test undo/redo for normal unicode properties
		/// (kcptUnicode or kcptBigUnicode).
		/// </summary>
		[Test]
		public void kcptUnicodeTests()
		{
			var startValue = Cache.LanguageProject.EthnologueCode;
			const string newValue = "NEWCode";

			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "set EthnologueCode"))
			{
				Cache.LanguageProject.EthnologueCode = newValue;
				undoHelper.RollBack = false;
			}

			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreSame(startValue, Cache.LanguageProject.EthnologueCode, "Wrong undone EthnologueCode.");
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.AreSame(newValue, Cache.LanguageProject.EthnologueCode, "Wrong redone EthnologueCode.");
		}

		/// <summary>
		/// Test undo/redo for binary data properties.
		/// </summary>
		[Test]
		public void kcptBinaryTests()
		{
			IUserConfigAcctFactory acctFactory = Cache.ServiceLocator.GetInstance<IUserConfigAcctFactory>();
			IUserConfigAcct acct;

			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add UserAccount"))
			{
				acct = acctFactory.Create();
				Cache.LanguageProject.UserAccountsOC.Add(acct);
				undoHelper.RollBack = false;
			}
			m_actionHandler.Commit();

			var startValue = acct.Sid;

			var firstChangedValue = new byte[] { 4, 5 };
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Details"))
			{
				acct.Sid = firstChangedValue;
				undoHelper.RollBack = false;
			}
			var secondChangedValue = new byte[] { 1, 2, 3 };
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Details"))
			{
				acct.Sid = secondChangedValue;
				undoHelper.RollBack = false;
			}

			// Should be two undos.
			Assert.AreEqual(2, m_uowService.UnsavedUnitsOfWork.Count, "Wrong unsaved UOW count.");
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(firstChangedValue, acct.Sid, "Wrong first details after undo.");
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(startValue, acct.Sid, "Wrong original details after undo.");
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.AreEqual(firstChangedValue, acct.Sid, "Wrong second details after redo.");
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.AreEqual(secondChangedValue, acct.Sid, "Wrong second details after redo.");
		}

		/// <summary>
		/// Automatic updating of DateModified properties.
		/// </summary>
		[Test]
		public void DateModifiedUpdate()
		{
			var leFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			ILexEntry le = null;
			UndoableUnitOfWorkHelper.Do("undo make entry", "redo", Cache.ActionHandlerAccessor,
				()=>
					{
						le = leFactory.Create();
					});
			var mod1 = le.DateModified;
			var wsAnalysis = Cache.DefaultAnalWs;

			// Change one of its own properties.
			WaitForTimeToChange(mod1);
			UndoableUnitOfWorkHelper.Do("undo set meaning", "redo", Cache.ActionHandlerAccessor,
				() =>
				{
					le.LiteralMeaning.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString("meaning", wsAnalysis);
				});
			var mod2 = le.DateModified;
			Assert.That(mod2, Is.GreaterThan(mod1));

			// Let it own something.
			WaitForTimeToChange(mod2);
			ILexSense ls1 = null;
			UndoableUnitOfWorkHelper.Do("undo create sense", "redo", Cache.ActionHandlerAccessor,
				() =>
				{
					ls1 = senseFactory.Create();
					le.SensesOS.Add(ls1);
				});
			var mod3 = le.DateModified;
			Assert.That(mod3, Is.GreaterThan(mod2));

			// Now modify the sense
			WaitForTimeToChange(mod3);
			UndoableUnitOfWorkHelper.Do("undo set residue", "redo", Cache.ActionHandlerAccessor,
				() =>
				{
					ls1.ImportResidue = Cache.TsStrFactory.MakeString("residue", wsAnalysis);
				});
			var mod4 = le.DateModified;
			Assert.That(mod4, Is.GreaterThan(mod3));

			// More levels
			WaitForTimeToChange(mod4);
			ILexSense ls2 = null;
			UndoableUnitOfWorkHelper.Do("undo create subsense", "redo", Cache.ActionHandlerAccessor,
				() =>
				{
					ls2 = senseFactory.Create();
					ls1.SensesOS.Add(ls2);
				});
			var mod5 = le.DateModified;
			Assert.That(mod5, Is.GreaterThan(mod4));

			// Make a status: should NOT modify the le's modify time
			WaitForTimeToChange(mod5);
			ICmPossibility status = null;
			UndoableUnitOfWorkHelper.Do("undo create status", "redo", Cache.ActionHandlerAccessor,
				() =>
				{
					if (Cache.LangProject.StatusOA == null)
						Cache.LangProject.StatusOA =
							Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
					status = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
					Cache.LangProject.StatusOA.PossibilitiesOS.Add(status);
				});
			var mod6 = le.DateModified;
			Assert.That(mod6, Is.EqualTo(mod5));

			// Modify the subsense
			WaitForTimeToChange(mod6);
			UndoableUnitOfWorkHelper.Do("undo set status", "redo", Cache.ActionHandlerAccessor,
				() =>
				{
					ls2.StatusRA = status;
				});
			var mod7 = le.DateModified;
			Assert.That(mod7, Is.GreaterThan(mod6));

			// Undo should restore them
			// (Enhance JohnT: there may be reasons we don't want this, e.g., if we committed to revision control
			// while the change was done, we MIGHT want the next commit after the Undo to show a later modification.
			// But for now this is the specified behavior.)
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.That(le.DateModified, Is.EqualTo(mod6));
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.That(le.DateModified, Is.EqualTo(mod5));
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.That(le.DateModified, Is.EqualTo(mod4));
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.That(le.DateModified, Is.EqualTo(mod3));
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.That(le.DateModified, Is.EqualTo(mod2));
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.That(le.DateModified, Is.EqualTo(mod1));
		}

		/// <summary>
		/// Busy-wait until we can be sure Now will be a later time than the input.
		/// </summary>
		/// <param name="old"></param>
		private void WaitForTimeToChange(DateTime old)
		{
			while (DateTime.Now == old)
			{}
		}

		/// <summary>
		/// Get and set "Unknown".
		/// </summary>
		[Test]
		public void UnknownTests()
		{
			var userWs = Cache.WritingSystemFactory.UserWs;
			var bldr = TsPropsBldrClass.Create();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Arial");
			bldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
			var tppOriginal = bldr.GetTextProps();
			bldr = TsPropsBldrClass.Create();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Times new Roman");
			bldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, userWs);
			var tppNew = bldr.GetTextProps();
			IStStyle style;
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add style"))
			{
				style = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
				Cache.LanguageProject.StylesOC.Add(style);
				undoHelper.RollBack = false;
			}
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Rule"))
			{
				style.Rules = tppOriginal;
				undoHelper.RollBack = false;
			}
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add new Rule"))
			{
				style.Rules = tppNew;
				undoHelper.RollBack = false;
			}

			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreSame(tppOriginal, style.Rules, "Not the right text props after Undo.");
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.AreSame(tppNew, style.Rules, "Not the right text props after Redo.");
		}

		/// <summary>
		/// Test undo/redo for boolean properties.
		/// </summary>
		[Test]
		public void kcptBooleanTests()
		{
			var startVal = Cache.LanguageProject.PeopleOA.IsVernacular;
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "set IsVernacular"))
			{
				Cache.LanguageProject.PeopleOA.IsVernacular = !startVal;
				undoHelper.RollBack = false;
			}

			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(startVal, Cache.LanguageProject.PeopleOA.IsVernacular, "Wrong undone boolean property");
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.AreEqual(!startVal, Cache.LanguageProject.PeopleOA.IsVernacular, "Wrong redone boolean property");
		}

		/// <summary>
		/// Test undo/redo for general date properties.
		/// </summary>
		[Test]
		public void kcptGenDateTests()
		{
			var firstPerson = (ICmPerson)Cache.LanguageProject.PeopleOA.PossibilitiesOS[0];
			var startValue = firstPerson.DateOfBirth;
			var newValue = new GenDate(GenDate.PrecisionType.Before, 1, 1, 3000, true);
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "set DOB"))
			{
				firstPerson.DateOfBirth = newValue;
				undoHelper.RollBack = false;
			}

			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(startValue, firstPerson.DateOfBirth, "Wrong starting DOB");
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.AreEqual(newValue, firstPerson.DateOfBirth, "Wrong undone DOB");
		}

		/// <summary>
		/// Test undo/redo for Guid properties.
		/// </summary>
		[Test]
		public void kcptGuidTests()
		{
			ICmPossibilityListFactory possFactory = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>();
			ICmPossibilityList possList;

			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add TimeOfDay"))
			{
				possList = possFactory.Create();
				Cache.LanguageProject.TimeOfDayOA = possList;
				undoHelper.RollBack = false;
			}
			m_actionHandler.Commit();

			var startingValue = possList.ListVersion;
			var newValue = Guid.NewGuid();
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "set new App guid"))
			{
				possList.ListVersion = newValue;
				undoHelper.RollBack = false;
			}

			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(startingValue, possList.ListVersion, "Wrong undone App guid.");
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.AreEqual(newValue, possList.ListVersion, "Wrong redone App guid.");
		}

		/// <summary>
		/// Test undo/redo for integer properties.
		/// </summary>
		[Test]
		public void kcptIntegerTests()
		{
			var startValue = Cache.LanguageProject.PeopleOA.Depth;
			var newValue = startValue + 1;
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "set Depth property"))
			{
				Cache.LanguageProject.PeopleOA.Depth = newValue;
				undoHelper.RollBack = false;
			}

			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(startValue, Cache.LanguageProject.PeopleOA.Depth, "Wrong undone Depth.");
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.AreEqual(newValue, Cache.LanguageProject.PeopleOA.Depth, "Wrong redone Depth.");
		}

		/// <summary>
		/// Test undo/redo for DateTime properties.
		/// </summary>
		[Test]
		public void kcptTimeTests()
		{
			var startValue = Cache.LanguageProject.DateCreated;
			var newValue = DateTime.Now;
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "set DateCreated property."))
			{
				Cache.LanguageProject.DateCreated = newValue;
				undoHelper.RollBack = false;
			}

			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(startValue, Cache.LanguageProject.DateCreated, "Wrong undone DateCreated");
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.AreEqual(newValue, Cache.LanguageProject.DateCreated, "Wrong redone DateCreated");
		}

		/// <summary>
		/// Test undo/redo for normal ITsString properties
		/// (kcptString or kcptBigString).
		/// </summary>
		[Test]
		public void kcptStringTests()
		{
			Assert.AreEqual("(Nothing to Undo.)", m_actionHandler.GetUndoText(), "Wrong undo text.");
			// JohnT: can't be sure of this now, may depend on previous test.
			//Assert.AreEqual("(Nothing to Redo.)", m_actionHandler.GetRedoText(), "Wrong redo text.");

			ILexEntry le;
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "Add entry"))
			{
				le = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				undoHelper.RollBack = false;
			}
			Assert.AreEqual("Undo Add entry", m_actionHandler.GetUndoText(), "Wrong undo text.");
			m_actionHandler.Commit();

			var startValue = le.ImportResidue;
			var secondValue = Cache.TsStrFactory.MakeString("import residue",
															Cache.WritingSystemFactory.UserWs);
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "Add import residue"))
			{
				le.ImportResidue = secondValue;
				undoHelper.RollBack = false;
			}
			Assert.AreEqual("Undo Add import residue", m_actionHandler.GetUndoText(), "Wrong undo text.");
			Assert.AreEqual("(Nothing to Redo.)", m_actionHandler.GetRedoText(), "Wrong redo text.");

			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual("Redo Add import residue", m_actionHandler.GetRedoText(), "Wrong redo text.");
			Assert.AreEqual("(Nothing to Undo.)", m_actionHandler.GetUndoText(), "Wrong undo text.");
			Assert.AreSame(startValue, le.ImportResidue, "Wrong undo value.");
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.AreSame(secondValue, le.ImportResidue, "Wrong redo value.");
			Assert.AreEqual("Undo Add import residue", m_actionHandler.GetUndoText(), "Wrong undo text.");
			Assert.AreEqual("(Nothing to Redo.)", m_actionHandler.GetRedoText(), "Wrong redo text.");

			// Set to new value.
			var thirdValue = Cache.TsStrFactory.MakeString("new import residue",
														   Cache.WritingSystemFactory.UserWs);
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "Change import residue"))
			{
				le.ImportResidue = thirdValue;
				undoHelper.RollBack = false;
			}

			Assert.AreEqual("Undo Change import residue", m_actionHandler.GetUndoText(), "Wrong undo text.");
			Assert.AreEqual("(Nothing to Redo.)", m_actionHandler.GetRedoText(), "Wrong redo text.");
			Assert.AreSame(thirdValue, le.ImportResidue, "Wrong value.");
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual("Undo Add import residue", m_actionHandler.GetUndoText(), "Wrong undo text.");
			//Assert.AreEqual("Redo Add import residue", m_actionHandler.GetRedoText(), "Wrong redo text.");
			Assert.AreEqual("Redo Change import residue", m_actionHandler.GetRedoText(), "Wrong redo text.");
			Assert.AreSame(secondValue, le.ImportResidue, "Wrong undo value.");
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual("(Nothing to Undo.)", m_actionHandler.GetUndoText(), "Wrong undo text.");
			Assert.AreEqual("Redo Add import residue", m_actionHandler.GetRedoText(), "Wrong redo text.");
			Assert.AreSame(startValue, le.ImportResidue, "Wrong undo value.");
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			// Cleanup: get rid of lex entry we committed earlier.
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler,
				() =>
					{
						le.Delete();
					});
		}

		/// <summary>
		/// Make sure the UndoableUnitOfWorkHelper rolls back on a failure of some kind.
		/// (This also tests the basic Rollback capability.)
		/// </summary>
		[Test]
		public void RollbackUndoableUnitOfWorkHelperFailureTest()
		{
			var lexDb = Cache.LanguageProject.LexDbOA;
			var startingEntryCount = lexDb.Entries.Count();
			using (new UndoableUnitOfWorkHelper(m_actionHandler, "Crash and burn"))
			{
				Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				Assert.AreEqual(startingEntryCount + 1, lexDb.Entries.Count(), "Entry not added.");
				// Simulate an error by not setting the RollBack to 'false'.
			}
			Assert.AreEqual(startingEntryCount, lexDb.Entries.Count(), "Entry add not rolled back.");
		}

		/// <summary>
		/// Make sure the NonUndoableUnitOfWorkHelper rolls back on a failure of some kind.
		/// (This also tests the basic Rollback capability.)
		/// </summary>
		[Test]
		public void RollbackNonUndoableUnitOfWorkHelperFailureTest()
		{
			var lexDb = Cache.LanguageProject.LexDbOA;
			var startingEntryCount = lexDb.Entries.Count();
			using (new NonUndoableUnitOfWorkHelper(m_actionHandler))
			{
				Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				Assert.AreEqual(startingEntryCount + 1, lexDb.Entries.Count(), "Entry not added.");
				// Simulate an error by not setting the RollBack to 'false'.
			}
			Assert.AreEqual(startingEntryCount, lexDb.Entries.Count(), "Entry add not rolled back.");
		}

		/// <summary>
		/// Make sure ContinueUndoTask is not yet supported at all.
		/// </summary>
		[Test]
		[ExpectedException(typeof(NotSupportedException))]
		public void ContinueUndoTaskTest()
		{
			m_actionHandler.ContinueUndoTask();
		}

		/// <summary>
		/// Make sure EndOuterUndo is not yet supported at all.
		/// </summary>
		[Test]
		[ExpectedException(typeof(NotSupportedException))]
		public void EndOuterUndoTest()
		{
			m_actionHandler.EndOuterUndoTask();
		}

		/// <summary>
		/// Make sure StartSeq is not yet supported at all.
		/// </summary>
		[Test]
		[ExpectedException(typeof(NotSupportedException))]
		public void StartSeqTest()
		{
			m_actionHandler.StartSeq("Undo Add Stuff", "Redo Add Stuff", null);
		}

		/// <summary>
		/// Make sure GetRedoTextN works
		/// </summary>
		[Test]
		public void GetRedoTextNTest()
		{
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add WordForm"))
			{
				m_wordFormFactory.Create();
				undoHelper.RollBack = false;
			}

			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Monkeys"))
			{
				m_wordFormFactory.Create();
				undoHelper.RollBack = false;
			}
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual("Redo add WordForm", m_actionHandler.GetRedoTextN(0));
			Assert.AreEqual("Redo add Monkeys", m_actionHandler.GetRedoTextN(1));
		}

		/// <summary>
		/// Make sure creating a mark works correctly
		/// </summary>
		[Test]
		public void MarkTest()
		{
			int hMark = m_actionHandler.Mark();
			Assert.IsTrue(hMark > 0);
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Object"))
			{
				m_wordFormFactory.Create();
				undoHelper.RollBack = false;
			}
			while (m_actionHandler.CanUndo())
				Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());

			Assert.AreEqual(hMark, m_actionHandler.TopMarkHandle);
		}

		/// <summary>
		/// Make sure accessing a now invalid mark throws an exception
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void MarkTest_InvalidMark()
		{
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Object"))
			{
				m_wordFormFactory.Create();
				undoHelper.RollBack = false;
			}
			int hMark = m_actionHandler.Mark();
			Assert.IsTrue(hMark > 0);
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Swine"))
			{
				m_wordFormFactory.Create();
				undoHelper.RollBack = false;
			}
			while (m_actionHandler.CanUndo())
				Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(0, m_actionHandler.TopMarkHandle);
			m_actionHandler.DiscardToMark(hMark);
		}

		/// <summary>
		/// Make sure committing the actions will clear the marks
		/// </summary>
		[Test]
		public void MarkTest_CommitClears()
		{
			int hMark = m_actionHandler.Mark();
			Assert.IsTrue(hMark > 0);
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Object"))
			{
				m_wordFormFactory.Create();
				undoHelper.RollBack = false;
			}
			Assert.AreEqual(hMark, m_actionHandler.TopMarkHandle);
			m_actionHandler.Commit();
			Assert.AreEqual(0, m_actionHandler.TopMarkHandle);
		}

		/// <summary>
		/// Make sure closing the action handler will clear the marks
		/// </summary>
		[Test]
		public void MarkTest_CloseClears()
		{
			int hMark = m_actionHandler.Mark();
			Assert.IsTrue(hMark > 0);
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Object"))
			{
				m_wordFormFactory.Create();
				undoHelper.RollBack = false;
			}
			Assert.AreEqual(hMark, m_actionHandler.TopMarkHandle);
			m_actionHandler.Close();
			Assert.AreEqual(0, m_actionHandler.TopMarkHandle);
		}

		/// <summary>
		/// Make sure CollapseToMark works correctly.
		/// </summary>
		[Test]
		public void CollapseToMarkTest()
		{
			Assert.AreEqual(0, m_actionHandler.TopMarkHandle);
			int hMark = m_actionHandler.Mark();
			IWfiWordform wordForm;
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Object"))
			{
				wordForm = m_wordFormFactory.Create();
				undoHelper.RollBack = false;
			}
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Monkeys"))
			{
				IWfiAnalysis wfiA = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
				wordForm.AnalysesOC.Add(wfiA);
				undoHelper.RollBack = false;
			}
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Pigs"))
			{
				m_wordFormFactory.Create();
				undoHelper.RollBack = false;
			}
			Assert.AreEqual(4, m_actionHandler.UndoableActionCount);
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(2, m_actionHandler.UndoableSequenceCount);
			Assert.AreEqual(1, m_actionHandler.RedoableSequenceCount);

			m_actionHandler.CollapseToMark(hMark, "Undo....", "Redo....");

			Assert.AreEqual(1, m_actionHandler.UndoableSequenceCount);
			Assert.AreEqual(0, m_actionHandler.RedoableSequenceCount);
			Assert.AreEqual(0, m_actionHandler.TopMarkHandle);
			Assert.AreEqual(3, m_actionHandler.UndoableActionCount);

			Stack<FdoUnitOfWork> undoBundle = (Stack<FdoUnitOfWork>)ReflectionHelper.GetField(m_actionHandler, "m_undoBundles");
			List<FdoUnitOfWork> uowList = undoBundle.ToList();
			Assert.AreEqual(1, uowList.Count);
			List<IUndoAction> actionList = (List<IUndoAction>)ReflectionHelper.GetField(uowList[0], "m_changes");
			Assert.AreEqual(3, actionList.Count);

			// Make sure the collapsed sequences can be undone
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(0, m_actionHandler.UndoableSequenceCount);
			Assert.AreEqual(1, m_actionHandler.RedoableSequenceCount);
		}

		/// <summary>
		/// Make sure CollapseToMark works correctly when two items are added to the same vector in two
		/// different undo actions.
		/// </summary>
		[Test]
		public void CollapseToMarkTest_TwoItemsAddedToVector()
		{
			Assert.AreEqual(0, m_actionHandler.TopMarkHandle);
			IWfiWordform wordForm;
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Object"))
			{
				wordForm = m_wordFormFactory.Create();
				undoHelper.RollBack = false;
			}
			int hMark = m_actionHandler.Mark();
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Monkeys"))
			{
				IWfiAnalysis wfiA = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
				wordForm.AnalysesOC.Add(wfiA);
				undoHelper.RollBack = false;
			}
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Pigs"))
			{
				IWfiAnalysis wfiA = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
				wordForm.AnalysesOC.Add(wfiA);
				undoHelper.RollBack = false;
			}
			Assert.AreEqual(5, m_actionHandler.UndoableActionCount);
			Assert.AreEqual(3, m_actionHandler.UndoableSequenceCount);
			Assert.AreEqual(0, m_actionHandler.RedoableSequenceCount);

			m_actionHandler.CollapseToMark(hMark, "Undo....", "Redo....");

			Assert.AreEqual(2, m_actionHandler.UndoableSequenceCount);
			Assert.AreEqual(0, m_actionHandler.RedoableSequenceCount);
			Assert.AreEqual(0, m_actionHandler.TopMarkHandle);
			Assert.AreEqual(5, m_actionHandler.UndoableActionCount, "Should still have 5 undoable actions after CollapseToMark");

			Stack<FdoUnitOfWork> undoBundle = (Stack<FdoUnitOfWork>)ReflectionHelper.GetField(m_actionHandler, "m_undoBundles");
			List<FdoUnitOfWork> uowList = undoBundle.ToList();
			Assert.AreEqual(2, uowList.Count);
			List<IUndoAction> actionList = (List<IUndoAction>)ReflectionHelper.GetField(uowList[0], "m_changes");
			Assert.AreEqual(4, actionList.Count);

			// Make sure the collapsed sequences can be undone
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(1, m_actionHandler.UndoableSequenceCount);
			Assert.AreEqual(1, m_actionHandler.RedoableSequenceCount);
		}

		/// <summary>
		/// Make sure CollapseToMark works correctly when an item is added and then deleted in two
		/// different undo actions.
		/// </summary>
		[Test]
		public void CollapseToMarkTest_ItemAddedThenDeleted()
		{
			Assert.AreEqual(0, m_actionHandler.TopMarkHandle);
			IWfiWordform wordForm;
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Object"))
			{
				wordForm = m_wordFormFactory.Create();
				undoHelper.RollBack = false;
			}
			int hMark = m_actionHandler.Mark();
			IWfiAnalysis wfiA;
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Monkeys"))
			{
				wfiA = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
				wordForm.AnalysesOC.Add(wfiA);
				undoHelper.RollBack = false;
			}
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Pigs"))
			{
				wordForm.AnalysesOC.Remove(wfiA);
				undoHelper.RollBack = false;
			}
			Assert.AreEqual(5, m_actionHandler.UndoableActionCount);
			Assert.AreEqual(3, m_actionHandler.UndoableSequenceCount);
			Assert.AreEqual(0, m_actionHandler.RedoableSequenceCount);

			m_actionHandler.CollapseToMark(hMark, "Undo....", "Redo....");

			Assert.AreEqual(2, m_actionHandler.UndoableSequenceCount);
			Assert.AreEqual(0, m_actionHandler.RedoableSequenceCount);
			Assert.AreEqual(0, m_actionHandler.TopMarkHandle);
			Assert.AreEqual(5, m_actionHandler.UndoableActionCount, "Should still have 5 undoable actions");

			Stack<FdoUnitOfWork> undoBundle = (Stack<FdoUnitOfWork>)ReflectionHelper.GetField(m_actionHandler, "m_undoBundles");
			List<FdoUnitOfWork> uowList = undoBundle.ToList();
			Assert.AreEqual(2, uowList.Count);
			List<IUndoAction> actionList = (List<IUndoAction>)ReflectionHelper.GetField(uowList[0], "m_changes");
			Assert.AreEqual(4, actionList.Count);

			// Make sure the collapsed sequences can be undone
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(1, m_actionHandler.UndoableSequenceCount);
			Assert.AreEqual(1, m_actionHandler.RedoableSequenceCount);
		}

		/// <summary>
		/// Make sure Rollback does not work with no current UOW.
		/// </summary>
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void RollbackCrashesTest()
		{
			m_actionHandler.Rollback(0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test DiscardToMark
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DiscardToMarkTest()
		{
			Assert.AreEqual(0, m_actionHandler.TopMarkHandle);

			int hMark = m_actionHandler.Mark();
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Object"))
			{
				IUndoAction action = MockRepository.GenerateMock<IUndoAction>();
				m_actionHandler.AddAction(action);
				undoHelper.RollBack = false;
			}
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Monkeys"))
			{
				IUndoAction action = MockRepository.GenerateMock<IUndoAction>();
				m_actionHandler.AddAction(action);
				undoHelper.RollBack = false;
			}
			m_actionHandler.DiscardToMark(hMark);
			Assert.AreEqual(0, m_actionHandler.UndoableSequenceCount);
			Assert.AreEqual(0, m_actionHandler.RedoableSequenceCount);
			Assert.AreEqual(0, m_actionHandler.TopMarkHandle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test get_TasksSinceMark
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void get_TasksSinceMarkTest()
		{
			Assert.AreEqual(0, m_actionHandler.TopMarkHandle);

			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Flu"))
			{
				m_wordFormFactory.Create();
				undoHelper.RollBack = false;
			}
			int hMark = m_actionHandler.Mark();
			Assert.IsFalse(m_actionHandler.get_TasksSinceMark(true));
			Assert.IsFalse(m_actionHandler.get_TasksSinceMark(false));
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Object"))
			{
				m_wordFormFactory.Create();
				undoHelper.RollBack = false;
			}
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Monkeys"))
			{
				m_wordFormFactory.Create();
				undoHelper.RollBack = false;
			}
			Assert.IsTrue(m_actionHandler.get_TasksSinceMark(true));
			Assert.IsFalse(m_actionHandler.get_TasksSinceMark(false));
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.IsTrue(m_actionHandler.get_TasksSinceMark(true));
			Assert.IsTrue(m_actionHandler.get_TasksSinceMark(false));

			while (m_actionHandler.CanUndo())
				Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			// Should have deleted the mark
			Assert.IsFalse(m_actionHandler.get_TasksSinceMark(true));
			Assert.IsFalse(m_actionHandler.get_TasksSinceMark(false));
		}

		/// <summary>
		/// Make sure UndoGrouper 'getter' is not yet supported at all.
		/// </summary>
		[Test]
		[ExpectedException(typeof(NotSupportedException))]
		public void get_UndoGrouperTest()
		{
			var grouper = m_actionHandler.UndoGrouper;
			Assert.IsNull(grouper, "Can't get here.");
		}

		/// <summary>
		/// Make sure UndoGrouper 'setter' is not yet supported at all.
		/// </summary>
		[Test]
		[ExpectedException(typeof(NotSupportedException))]
		public void set_UndoGrouperTest()
		{
			m_actionHandler.UndoGrouper = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test TopMarkHandle
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TopMarkHandleTest()
		{
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Object"))
			{
				IUndoAction action = MockRepository.GenerateMock<IUndoAction>();
				m_actionHandler.AddAction(action);
				undoHelper.RollBack = false;
			}
			int hMark1 = m_actionHandler.Mark();
			Assert.AreEqual(hMark1, m_actionHandler.TopMarkHandle);
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Monkeys"))
			{
				IUndoAction action = MockRepository.GenerateMock<IUndoAction>();
				m_actionHandler.AddAction(action);
				undoHelper.RollBack = false;
			}
			int hMark2 = m_actionHandler.Mark();
			Assert.AreEqual(hMark2, m_actionHandler.TopMarkHandle);
			m_actionHandler.DiscardToMark(hMark1);
			Assert.AreEqual(0, m_actionHandler.TopMarkHandle);
		}

		/// <summary>
		/// Make sure CreateMarkIfNeeded is not yet supported at all.
		/// </summary>
		[Test]
		public void CreateMarkIfNeededTest()
		{
			// Test that one is not created
			Assert.AreEqual(0, m_actionHandler.TopMarkHandle);
			m_actionHandler.CreateMarkIfNeeded(false);
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Object"))
			{
				m_wordFormFactory.Create();
				undoHelper.RollBack = false;
			}
			Assert.AreEqual(0, m_actionHandler.TopMarkHandle);

			// Test that one is created
			m_actionHandler.CreateMarkIfNeeded(true);
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Monkeys"))
			{
				m_wordFormFactory.Create();
				undoHelper.RollBack = false;
			}
			Assert.IsTrue(m_actionHandler.TopMarkHandle > 0);
		}

		/// <summary>
		/// Make sure GetUndoTextN works
		/// </summary>
		[Test]
		public void GetUndoTextNTest()
		{
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Object"))
			{
				m_wordFormFactory.Create();
				undoHelper.RollBack = false;
			}

			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add Monkeys"))
			{
				m_wordFormFactory.Create();
				undoHelper.RollBack = false;
			}
			Assert.AreEqual("Undo add Object", m_actionHandler.GetUndoTextN(0));
			Assert.AreEqual("Undo add Monkeys", m_actionHandler.GetUndoTextN(1));
		}

		/// <summary>
		/// Object creation Undo/Redo.
		/// </summary>
		[Test]
		public void UndoRedoObjectCreationDeletionTest()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			ICmPossibilityListFactory listFactory = servLoc.GetInstance<ICmPossibilityListFactory>();
			ICmPossibilityListRepository listRepo = servLoc.GetInstance<ICmPossibilityListRepository>();
			ILangProject lp = Cache.LanguageProject;
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add original anthro list"))
			{
				lp.AnthroListOA = listFactory.Create();
				undoHelper.RollBack = false;
			}
			ICmPossibilityList originalAnthroList = lp.AnthroListOA;
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add new anthro list"))
			{
				lp.AnthroListOA = listFactory.Create();
				undoHelper.RollBack = false;
			}
			ICmPossibilityList newAnthroList = lp.AnthroListOA;

			Assert.AreSame(newAnthroList, lp.AnthroListOA, "Wrong anthro list.");
			Assert.IsTrue(listRepo.AllInstances().Contains(newAnthroList));

			// This checks both Undo of the creation of 'newAnthroList' and the deletion of 'originalAnthroList'.
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreSame(originalAnthroList, lp.AnthroListOA, "Wrong undone anthro list.");
			Assert.IsFalse(listRepo.AllInstances().Contains(newAnthroList));

			// This checks both Redo of the creation of 'newAnthroList' and the deletion of 'originalAnthroList'.
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.AreSame(newAnthroList, lp.AnthroListOA, "Wrong redone anthro list.");
			Assert.IsTrue(listRepo.AllInstances().Contains(newAnthroList));
		}

		/// <summary>
		/// Object creation/deletion in one UOW
		/// </summary>
		[Test]
		public void UndoRedoObjectCreationDeletionSameUowTest()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			ICmPossibilityListFactory listFactory = servLoc.GetInstance<ICmPossibilityListFactory>();
			ICmPossibilityListRepository listRepo = servLoc.GetInstance<ICmPossibilityListRepository>();
			int origCount = listRepo.AllInstances().Count();
			ILangProject lp = Cache.LanguageProject;
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add original anthro list"))
			{
				lp.AnthroListOA = listFactory.Create();
				lp.AnthroListOA = listFactory.Create();
				undoHelper.RollBack = false;
			}
			ICmPossibilityList newAnthroList = lp.AnthroListOA;

			Assert.AreSame(newAnthroList, lp.AnthroListOA, "Wrong anthro list.");
			Assert.IsTrue(listRepo.AllInstances().Contains(newAnthroList));

			// This checks both Undo of the creation of 'newAnthroList' and the deletion of 'originalAnthroList'.
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(origCount, listRepo.AllInstances().Count());

			// This checks both Redo of the creation of 'newAnthroList' and the deletion of 'originalAnthroList'.
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.AreSame(newAnthroList, lp.AnthroListOA, "Wrong redone anthro list.");
			Assert.IsTrue(listRepo.AllInstances().Contains(newAnthroList));
		}

		/// <summary>
		/// Test the special behaviors that occur when we have multiple Undo stacks.
		/// </summary>
		[Test]
		public void MultipleStacks()
		{
			var manager = Cache.ServiceLocator.GetInstance<IUndoStackManager>();
			var stack2 = manager.CreateUndoStack();
			var oldStack = Cache.ActionHandlerAccessor;
			Assert.Throws(typeof (ArgumentException), () => manager.DisposeStack(oldStack));

			var leFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var lsFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			ILexEntry le1 = null;
			ILexSense ls1 = null;
			UndoableUnitOfWorkHelper.Do("Undo create entry on old stack", "Redo create entry on old stack",
				m_actionHandler,
				() =>
					{
						le1 = leFactory.Create();
					});
			UndoableUnitOfWorkHelper.Do("Undo create sense on old stack", "Redo create sense on old stack", m_actionHandler,
				() =>
				{
					ls1 = lsFactory.Create();
						le1.SensesOS.Add(ls1);
					});

			manager.SetCurrentStack(stack2);
			Assert.Throws(typeof(ArgumentException), () => manager.DisposeStack(stack2));

			ILexEntry le2 = null;
			ILexSense ls2 = null;
			UndoableUnitOfWorkHelper.Do("Undo create entry on new stack", "Redo create entry on new stack", stack2,
				() =>
				{
					le2 = leFactory.Create();
					});
			UndoableUnitOfWorkHelper.Do("Undo create sense on new stack", "Redo create sense on new stack", stack2,
				() =>
				{
					ls2 = lsFactory.Create();
					le2.SensesOS.Add(ls2);
				});
			// Now, we should be able to undo creating the first sense, even though not in exact reverse order.
			Assert.That(oldStack.CanUndo(), Is.True);

			// Now we are creating a sense on the new stack, but it affects the entry that owns ls1.
			// We can no longer undo creating ls1 before we undo creating ls1_2.
			ILexSense ls1_2 = null;
			UndoableUnitOfWorkHelper.Do("Undo create sense on old entry new stack", "Redo create sense on new stack",
				stack2,
				() =>
					{
						ls1_2 = lsFactory.Create();
						le1.SensesOS.Add(ls1_2);
					});
			Assert.That(oldStack.CanUndo(), Is.False);
			Assert.That(stack2.CanUndo(), Is.True);

			// But, we should be able to Undo it once we Undo the stack2 change.
			stack2.Undo();
			Assert.That(oldStack.CanUndo(), Is.True);

			// At this point, we can of course redo on stack 2.
			Assert.That(stack2.CanRedo(), Is.True);

			// But, if we actually do Undo the change on stack 1, the potential Redo task on stack 2 is now a conflict.
			oldStack.Undo();
			Assert.That(stack2.CanRedo(), Is.False);

			oldStack.Redo(); // give me back ls1
			Assert.That(stack2.CanRedo(), Is.True); // so now we could redo again.
			Assert.That(oldStack.CanUndo(), Is.True); // and can still Undo making ls1.

			// Check that a modification on the other stack is a conflict.
			UndoableUnitOfWorkHelper.Do("Undo modify sense new stack", "Redo modify sense on new stack",
				stack2,
				() =>
				{
					ls1.Gloss.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString("rubbish", Cache.DefaultAnalWs);
				});
			Assert.That(oldStack.CanUndo(), Is.False);
			Assert.That(stack2.CanUndo(), Is.True);

			// Deleting it on stack 2 is also a blocker.
			stack2.Undo(); // get back to where we can Undo creating it.
			Assert.That(oldStack.CanUndo(), Is.True);
			manager.SetCurrentStack(oldStack);
			UndoableUnitOfWorkHelper.Do("Undo create second sense on old stack", "Redo",
				oldStack,
				() =>
				{
					ls1_2 = lsFactory.Create();
					le1.SensesOS.Add(ls1_2);
				});
			manager.SetCurrentStack(stack2);
			UndoableUnitOfWorkHelper.Do("Undo delete sense new stack", "Redo",
				stack2,
				() =>
				{
					le1.SensesOS.RemoveAt(1);
				});
			Assert.That(oldStack.CanUndo(), Is.False); // can't undo creating something another window has deleted.
			Assert.That(stack2.CanUndo(), Is.True);
			stack2.Undo(); // get back to where we can Undo creating it.
			Assert.That(oldStack.CanUndo(), Is.True);

			// --- Undo must not delete an object which a later change creates a reference to.
			// Nor may Redo re-create the reference before the object is re-created.

			// Furthermore, if stack 2 created something that references ls1_2, we can't undo creating it.
			CreateAtomicRefToSense(stack2, ls1_2, true);
			VerifyOrderRequired(oldStack, stack2);
			// It's also a problem if stack 2 added a reference, in a different UOW from creating the referring object.
			CreateAtomicRefToSense(stack2, ls1_2, false);
			VerifyOrderRequired(oldStack, stack2);
			stack2.Undo(); // undo creation of referring objects, too.

			// It's also a problem if stack 2 added a vector reference, in a different UOW from creating the referring object.
			// We could also check this passing true, but the algorithm for finding refs in newly created objects is not
			// different for atomic and sequence props.
			CreateSeqRefToSense(stack2, ls1_2, false);
			VerifyOrderRequired(oldStack, stack2);
			stack2.Undo(); // undo creation of referring objects, too.

			// --- Undo must not re-create a reference to an object which a later change deletes.
			// Nor may Redo re-delete the object before the deletion of the reference is redone.

			// In the group of tests above, we created an object, then created a reference, then tried to undo creating the object.
			// This time, we create the object and reference in advance, then the first change deletes the reference,
			// while the second deletes the object. The change deleting the reference must not be Undone first.
			var mb = CreateAtomicRefToSense(stack2, ls1_2, false);
			manager.SetCurrentStack(oldStack);
			UndoableUnitOfWorkHelper.Do("Undo removing ref on old stack", "Redo",
				oldStack,
				() =>
					{
						mb.SenseRA = null; // remove the reference.
					});
			manager.SetCurrentStack(stack2);
			UndoableUnitOfWorkHelper.Do("Undo deleting sense on new stack", "Redo",
				stack2,
				() =>
				{
					le1.SensesOS.Remove(ls1_2);
				});
			VerifyOrderRequired(oldStack, stack2);
			oldStack.Undo(); // get the reference back
			stack2.Undo(); // get rid of all the referring stuff, ready for another test.

			// Simlarly with a ref sequence.
			var lr = CreateSeqRefToSense(stack2, ls1_2, true);
			manager.SetCurrentStack(oldStack);
			UndoableUnitOfWorkHelper.Do("Undo removing ref on old stack", "Redo",
				oldStack,
				() =>
				{
					lr.TargetsRS.RemoveAt(0); // remove the reference.
				});
			manager.SetCurrentStack(stack2);
			UndoableUnitOfWorkHelper.Do("Undo deleting sense on new stack", "Redo",
				stack2,
				() =>
				{
					le1.SensesOS.Remove(ls1_2);
				});
			VerifyOrderRequired(oldStack, stack2);
			oldStack.Undo(); // get the reference back (but keep lr intact for now)
			// Similarly when the referring object is destroyed.
			manager.SetCurrentStack(oldStack);
			UndoableUnitOfWorkHelper.Do("Undo removing referring object on old stack", "Redo",
				oldStack,
				() =>
				{
					((ILexRefType) lr.Owner).MembersOC.Remove(lr);
				});
			manager.SetCurrentStack(stack2);
			UndoableUnitOfWorkHelper.Do("Undo deleting sense on new stack", "Redo",
				stack2,
				() =>
				{
					le1.SensesOS.Remove(ls1_2);
				});
			VerifyOrderRequired(oldStack, stack2);
			oldStack.Undo(); // get referring object back for now
			stack2.Undo(); // Get rid of all the referring stuff (so oldStack is in a condition to Undo all the way)

			// Restore things.
			manager.SetCurrentStack(oldStack);
			manager.DisposeStack(stack2);
		}

		/// <summary>
		/// Two changes have been made, first on firstStack, then on secondStack. Verify that they must be Undone
		/// in the reverse order, and redone in the same order.
		/// </summary>
		/// <param name="firstStack"></param>
		/// <param name="secondStack"></param>
		private void VerifyOrderRequired(IActionHandler firstStack, IActionHandler secondStack)
		{
			Assert.That(firstStack.CanUndo(), Is.False); // can't undo the first change before the other.
			Assert.That(secondStack.CanUndo(), Is.True);
			secondStack.Undo(); // get back to where we can Undo creating it.
			Assert.That(firstStack.CanUndo(), Is.True); // now the second one is undone, we can
			firstStack.Undo(); // now both are undone.
			Assert.That(secondStack.CanRedo(), Is.False); // can't redo the second change first
			Assert.That(firstStack.CanRedo(), Is.True); // but of course we can redo the first one first
			firstStack.Redo();
			Assert.That(secondStack.CanRedo(), Is.True); // now we can redo the second change, since we already redid the first.

		}

		private IWfiMorphBundle CreateAtomicRefToSense(IActionHandler stack, ILexSense ls, bool createReferringObjInSameUow)
		{
			IWfiMorphBundle mb = null;
			UndoableUnitOfWorkHelper.Do("Undo create referring item on new stack", "Redo",
				stack,
				() =>
					{
						var wf = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
						var wa = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
						wf.AnalysesOC.Add(wa);
						mb = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
						wa.MorphBundlesOS.Add(mb);
						if (!createReferringObjInSameUow)
							stack.BreakUndoTask("undo setting ref", "redo setting ref");
						mb.SenseRA = ls;
					});
			return mb;
		}
		private ILexReference CreateSeqRefToSense(IActionHandler stack, ILexSense ls, bool createReferringObjInSameUow)
		{
			ILexReference lr = null;
			UndoableUnitOfWorkHelper.Do("Undo create referring item on new stack", "Redo",
				stack,
				() =>
				{
					var list = Cache.LangProject.LexDbOA.ReferencesOA;
					if (list == null)
					{
						list = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
						Cache.LangProject.LexDbOA.ReferencesOA = list;
					}
					var lrt = Cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create();
					list.PossibilitiesOS.Add(lrt);
					lr = Cache.ServiceLocator.GetInstance<ILexReferenceFactory>().Create();
					lrt.MembersOC.Add(lr);
					if (!createReferringObjInSameUow)
						stack.BreakUndoTask("undo setting ref", "redo setting ref");
					lr.TargetsRS.Add(ls);
				});
			return lr;
		}

		/// <summary>
		/// Test that if we change undo stacks while a UOW is in progress, the UOW completes in its own stack.
		/// </summary>
		[Test]
		public void DelegateToActiveUow()
		{
			var manager = Cache.ServiceLocator.GetInstance<IUndoStackManager>();
			var stack2 = manager.CreateUndoStack();
			var oldStack = Cache.ActionHandlerAccessor;

			var leFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var lsFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			oldStack.BeginUndoTask("do something", "undo it");
			ILexEntry le1 = leFactory.Create();
			manager.SetCurrentStack(stack2);

			ILexSense ls1 = lsFactory.Create();
			le1.SensesOS.Add(ls1);
			stack2.EndUndoTask();
			Assert.That(stack2.CanUndo(), Is.False, "UOW should belong to original stack");
			Assert.That(oldStack.CanUndo(), Is.True, "UOW should belong to original stack");
			stack2.BeginUndoTask("do more", "undo more");
			ILexEntry le2 = leFactory.Create();
			stack2.EndUndoTask();
			stack2.Undo();
			Assert.That(le2.IsValidObject, Is.False);
			manager.SetCurrentStack(oldStack);
			oldStack.Undo();
			Assert.That(le1.IsValidObject, Is.False);
			manager.DisposeStack(stack2);
		}

		/// <summary>
		/// This tests stuff related to Save and undo stacks.
		/// </summary>
		[Test]
		public void SaveMultipleStacks()
		{
			var manager = Cache.ServiceLocator.GetInstance<IUndoStackManager>();
			((IUndoStackManager)m_uowService).Save(); // Anything from setup doesn't count as unsaved.
			Assert.That(m_uowService.UnsavedUnitsOfWork.Count, Is.EqualTo(0));
			var stack2 = manager.CreateUndoStack();
			var oldStack = Cache.ActionHandlerAccessor;
			Assert.Throws(typeof(ArgumentException), () => manager.DisposeStack(oldStack));

			var leFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var lsFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			var lexDb = Cache.LangProject.LexDbOA;
			ILexEntry le1 = null;
			ILexSense ls1 = null;
			UndoableUnitOfWorkHelper.Do("Undo create entry on old stack", "Redo create entry on old stack",
				m_actionHandler,
				() =>
				{
					le1 = leFactory.Create();
				});
			Assert.That(m_uowService.UnsavedUnitsOfWork.Count, Is.EqualTo(1));
			VerifyChanges(new ICmObject[] {le1}, new ICmObject[] {}, new ICmObjectId[0], m_uowService);

			// This verifies both that we see changes from both stacks, and that something modified in one UOW
			// but created in the other only shows up once as created.
			manager.SetCurrentStack(stack2);
			UndoableUnitOfWorkHelper.Do("Undo create sense on new stack", "Redo", stack2,
				() =>
				{
					ls1 = lsFactory.Create();
					le1.SensesOS.Add(ls1);
				});

			Assert.That(m_uowService.UnsavedUnitsOfWork.Count, Is.EqualTo(2));
			VerifyChanges(new ICmObject[] { le1, ls1 }, new ICmObject[] {}, new ICmObjectId[0], m_uowService);
			((IUndoStackManager)m_uowService).Save();
			Assert.That(m_uowService.UnsavedUnitsOfWork.Count, Is.EqualTo(0));
			var ls1Id = ls1.Id; // get this before we destroy it.
			stack2.Undo(); // undo creating the sense: modifies the entry, deletes the sense.
			VerifyChanges(new ICmObject[0], new ICmObject[] { le1 }, new [] { ls1Id }, m_uowService);
			manager.SetCurrentStack(oldStack);
			var le1Id = le1.Id;
			oldStack.Undo(); // undo creating the entry, too.
			VerifyChanges(new ICmObject[0], new ICmObject[] {}, new ICmObjectId[] {ls1Id, le1Id}, m_uowService);

			// Make sure changes resulting from Undoing something saved are not lost, even if we do something
			// else on the same stack.
			ILexEntry le2 = null;
			UndoableUnitOfWorkHelper.Do("Undo create 2nd entry on old stack", "Redo ",
				m_actionHandler,
				() =>
					{
						le2 = leFactory.Create();
					});
			ILexSense ls2 = null;
			manager.SetCurrentStack(stack2);
			UndoableUnitOfWorkHelper.Do("Undo create sense on new stack", "Redo", stack2,
				() =>
				{
					ls2 = lsFactory.Create();
					le2.SensesOS.Add(ls2);
				});
			VerifyChanges(new ICmObject[] {le2, ls2}, new ICmObject[] {}, new [] { ls1Id, le1Id }, m_uowService);
			((IUndoStackManager)m_uowService).Save();
			stack2.Undo();
			manager.SetCurrentStack(oldStack);
			oldStack.Undo(); // undo creating the entry, too.
			var ls2Id = ls2.Id;
			var le2Id = le2.Id;
			VerifyChanges(new ICmObject[0], new ICmObject[] {}, new [] { ls2Id, le2Id }, m_uowService);
			((IUndoStackManager)m_uowService).Save();
			// Check that Redo changes get saved.
			oldStack.Redo(); // re-creates le2
			VerifyChanges(new ICmObject[] {le2}, new ICmObject[] {}, new ICmObjectId[0], m_uowService);
			// Now, see what happens if we create an object, delete it, then Save and undo both changes.
			UndoableUnitOfWorkHelper.Do("Undo delete 2nd entry on old stack", "Redo ",
				m_actionHandler,
				() =>
				{
					le2.Delete();
				});
			// I don't think we can reasonably hope to detect that lexDb hasn't really changed.
			VerifyChanges(new ICmObject[0], new ICmObject[] {}, new ICmObjectId[0], m_uowService);
			((IUndoStackManager)m_uowService).Save();
			oldStack.Undo();
			oldStack.Undo();
			// Now we have on the redo stack changes which undelete it and uncreate it.
			VerifyChanges(new ICmObject[0], new ICmObject[] {}, new ICmObjectId[0], m_uowService);
		}

		void VerifyChanges(ICmObject[] expectedNewbies, ICmObject[] expectedDirtballs,
			ICmObjectId[] expectedGoners, IUnitOfWorkService m_uowService)
		{
			var newbies = new HashSet<ICmObjectId>();
			var dirtballs = new HashSet<ICmObjectOrSurrogate>(new ObjectSurrogateEquater());
			var goners = new HashSet<ICmObjectId>();
			m_uowService.GatherChanges(newbies, dirtballs, goners);
			var setNewbies = new HashSet<ICmObjectId>(from obj in expectedNewbies select obj.Id);
			Assert.That(newbies.Except(setNewbies), Is.Empty, "some unexpected newbies were found");
			Assert.That(setNewbies.Except(newbies), Is.Empty, "some expected newbies were not found");
			var setDirtballs = new HashSet<ICmObjectOrSurrogate>(expectedDirtballs.Cast<ICmObjectOrSurrogate>());
			Assert.That(dirtballs.Except(setDirtballs), Is.Empty, "some unexpected dirtballs were found");
			Assert.That(setDirtballs.Except(dirtballs), Is.Empty, "some expected dirtballs were not found");
			var setGoners = new HashSet<ICmObjectId>(expectedGoners);
			Assert.That(goners.Except(setGoners), Is.Empty, "some unexpected goners were found");
			Assert.That(setGoners.Except(goners), Is.Empty, "some expected goners were not found");
		}

		/// <summary>
		/// Test the proper behavior of Undo/Redo when creating an object and things it owns in the same UOW
		/// </summary>
		[Test]
		public void UndoRedoCreateObjectWithChildObjects()
		{
			var lp = Cache.LanguageProject;
			IText text = null;
			IStText stText = null;
			IStTxtPara para = null;
			UndoableUnitOfWorkHelper.Do("Undo create text", "Redo create text", m_actionHandler, () =>
			{
				text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
				//lp.TextsOC.Add(text);
				stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
				text.ContentsOA = stText;
				para = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
				stText.ParagraphsOS.Add(para);
			});
			int hvoText = text.Hvo;
			int hvoStText = stText.Hvo;
			int hvoPara = para.Hvo;
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.IsFalse(text.IsValidObject);
			Assert.IsFalse(Cache.ServiceLocator.IsValidObjectId(hvoText));
			Assert.IsFalse(stText.IsValidObject);
			Assert.IsFalse(Cache.ServiceLocator.IsValidObjectId(hvoStText));
			Assert.IsFalse(para.IsValidObject);
			Assert.IsFalse(Cache.ServiceLocator.IsValidObjectId(hvoPara));
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.IsTrue(text.IsValidObject);
			Assert.IsTrue(Cache.ServiceLocator.IsValidObjectId(hvoText));
			Assert.IsTrue(stText.IsValidObject);
			Assert.IsTrue(Cache.ServiceLocator.IsValidObjectId(hvoStText));
			Assert.IsTrue(para.IsValidObject);
			Assert.IsTrue(Cache.ServiceLocator.IsValidObjectId(hvoPara));
			Assert.AreEqual(hvoText, text.Hvo);
			Assert.AreEqual(hvoStText, stText.Hvo);
			Assert.AreEqual(hvoPara, para.Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the proper behavior of Undo/Redo when making data change and non-data change
		/// actions in the same UOW
		/// </summary>
		/// <remarks>Note that most of the interesting logic (and the assertions) are actually
		/// in MockUndoAction.</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UndoRedoVerifyOrder()
		{
			var lp = Cache.LanguageProject;
			IActionHandler actionHandler = Cache.ServiceLocator.GetInstance<IUndoStackManager>().CreateUndoStack();
			try
			{
				Cache.ServiceLocator.GetInstance<IUndoStackManager>().SetCurrentStack(actionHandler);
				List<MockUndoAction> actionList = new List<MockUndoAction>();
				UndoableUnitOfWorkHelper.Do("Undo test order", "Redo test order", actionHandler, () =>
				{
					MockUndoAction dataChange1 = new MockUndoAction(true, "First DC");
					dataChange1.AllActions = actionList;
					actionList.Add(dataChange1);
					actionHandler.AddAction(dataChange1);
					MockUndoAction nonDataChange1 = new MockUndoAction(false, "First NDC");
					nonDataChange1.AllActions = actionList;
					actionList.Add(nonDataChange1);
					actionHandler.AddAction(nonDataChange1);
					MockUndoAction nonDataChange2 = new MockUndoAction(false, "Second NDC");
					nonDataChange2.AllActions = actionList;
					actionList.Add(nonDataChange2);
					actionHandler.AddAction(nonDataChange2);
					MockUndoAction dataChange2 = new MockUndoAction(true, "Second DC");
					dataChange2.AllActions = actionList;
					actionList.Add(dataChange2);
					actionHandler.AddAction(dataChange2);
				});

				foreach (MockUndoAction action in actionList)
					action.PropChangeWasIssued = false;
				Assert.AreEqual(UndoResult.kuresSuccess, actionHandler.Undo());

				foreach (MockUndoAction action in actionList)
					action.PropChangeWasIssued = false;
				Assert.AreEqual(UndoResult.kuresSuccess, actionHandler.Redo());
			}
			finally
			{
				Cache.ServiceLocator.GetInstance<IUndoStackManager>().SetCurrentStack(m_actionHandler);
			}
		}

		/// <summary>
		/// Test the proper behavior of Undo/Redo when creating a ScrBook and things it owns in the same UOW.
		/// </summary>
		[Test]
		public void UndoRedoRemoveScrBookWithChildObjects()
		{
			var lp = Cache.LanguageProject;
			IText text = null;
			IStText stText = null;
			IStTxtPara para = null;
			IScrBook book = null;
			UndoableUnitOfWorkHelper.Do("Undo create book", "Redo create book", m_actionHandler, () =>
			{
				book = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(1, out stText);
				para = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(stText, "Monkey");
				object owner = ReflectionHelper.CreateObject("FDO.dll", "SIL.FieldWorks.FDO.Infrastructure.Impl.CmObjectId", BindingFlags.NonPublic,
					new object[] {book.Guid});
				ReflectionHelper.SetField(stText, "m_owner", owner);
			});

			int hvoStText = stText.Hvo;
			int hvoPara = para.Hvo;
			int hvoBook = book.Hvo;

			UndoableUnitOfWorkHelper.Do("Undo delete book", "Redo delete book", m_actionHandler, () =>
			{
				lp.TranslatedScriptureOA.ScriptureBooksOS.Remove(book);
			});

			Assert.IsFalse(book.IsValidObject);
			Assert.IsFalse(Cache.ServiceLocator.IsValidObjectId(hvoBook));
			Assert.IsFalse(stText.IsValidObject);
			Assert.IsFalse(Cache.ServiceLocator.IsValidObjectId(hvoStText));
			Assert.IsFalse(para.IsValidObject);
			Assert.IsFalse(Cache.ServiceLocator.IsValidObjectId(hvoPara));

			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());

			Assert.IsTrue(book.IsValidObject);
			Assert.IsTrue(Cache.ServiceLocator.IsValidObjectId(hvoBook));
			Assert.IsTrue(stText.IsValidObject);
			Assert.IsTrue(Cache.ServiceLocator.IsValidObjectId(hvoStText));
			Assert.IsTrue(para.IsValidObject);
			Assert.IsTrue(Cache.ServiceLocator.IsValidObjectId(hvoPara));
			Assert.AreEqual(hvoBook, book.Hvo);
			Assert.AreEqual(hvoStText, stText.Hvo);
			Assert.AreEqual(hvoPara, para.Hvo);

			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());

			Assert.IsFalse(book.IsValidObject);
			Assert.IsFalse(Cache.ServiceLocator.IsValidObjectId(hvoBook));
			Assert.IsFalse(stText.IsValidObject);
			Assert.IsFalse(Cache.ServiceLocator.IsValidObjectId(hvoStText));
			Assert.IsFalse(para.IsValidObject);
			Assert.IsFalse(Cache.ServiceLocator.IsValidObjectId(hvoPara));
		}

		/// <summary>
		/// Make sure Undo/Redo works when object is created and deleted in the same UOW.
		/// </summary>
		[Test]
		public void UndoRedoCreateDeleteAtomicPropertyInSameUOW()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			ILangProject lp = Cache.LanguageProject;
			ICmPossibilityListFactory listFactory = servLoc.GetInstance<ICmPossibilityListFactory>();
			UndoableUnitOfWorkHelper.Do("Undo set original value", "Redo set original value", m_actionHandler, () =>
			{
				lp.AnthroListOA = listFactory.Create();
			});
			ICmPossibilityList originalAnthroList = lp.AnthroListOA;
			UndoableUnitOfWorkHelper.Do("Undo add-delete something", "Redo add-delete something", m_actionHandler, () =>
			{
				ICmPossibilityList newAnthroList = listFactory.Create();
				lp.AnthroListOA = newAnthroList;
				newAnthroList.PossibilitiesOS.Add(servLoc.GetInstance<ICmPossibilityFactory>().Create());
				lp.AnthroListOA = null;
			});
			Assert.IsNull(lp.AnthroListOA);
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreSame(originalAnthroList, lp.AnthroListOA, "Wrong redone anthro list.");
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.IsNull(lp.AnthroListOA, "Should be reset to null by Redo command");
		}

		/// <summary>
		/// Make sure Undo/Redo works when object is moved from one owner to another.
		/// </summary>
		[Test]
		public void UndoRedoMoveObjectTest()
		{
			var lp = Cache.LanguageProject;
			IStStyle style = null;
			IStText title = null;
			IScrSection section1 = null;
			IScrSection section2 = null;
			IStTxtPara para = null;
			IScrBook book = null;

			// Basic setup
			UndoableUnitOfWorkHelper.Do("Undo create book", "Redo create book", m_actionHandler, () =>
			{
				style = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
				lp.TranslatedScriptureOA.StylesOC.Add(style);
				style.Name = "Intro Section";
				style.Structure = StructureValues.Heading;
				book = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(1, out title);
				section1 = Cache.ServiceLocator.GetInstance<IScrSectionFactory>().CreateScrSection(book, 0, "testing",
					StyleUtils.ParaStyleTextProps("Intro Paragraph"), true);
				para = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(section1.ContentOA,
					1, "Intro Section");
				section2 = Cache.ServiceLocator.GetInstance<IScrSectionFactory>().CreateEmptySection(book, 1);
			});

			Assert.AreEqual(section1.ContentOA.ParagraphsOS.Count, 2);
			Assert.AreEqual(section2.HeadingOA.ParagraphsOS.Count, 0);

			// Move para from content to heading
			UndoableUnitOfWorkHelper.Do("Undo move para", "Redo move para", m_actionHandler, () =>
			{
				book.MoveContentParasToNextSectionHeading(0, 1, style);
			});

			Assert.AreEqual(section1.ContentOA.ParagraphsOS.Count, 1);
			Assert.AreEqual(section2.HeadingOA.ParagraphsOS.Count, 1);
			Assert.AreEqual(section2.HeadingOA.ParagraphsOS[0].Owner, section2.HeadingOA);

			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());

			Assert.AreEqual(section1.ContentOA.ParagraphsOS.Count, 2);
			Assert.AreEqual(section2.HeadingOA.ParagraphsOS.Count, 0);
			Assert.AreEqual(section1.ContentOA.ParagraphsOS[1].Owner, section1.ContentOA);

			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());

			Assert.AreEqual(section1.ContentOA.ParagraphsOS.Count, 1);
			Assert.AreEqual(section2.HeadingOA.ParagraphsOS.Count, 1);
			Assert.AreEqual(section2.HeadingOA.ParagraphsOS[0].Owner, section2.HeadingOA);
		}

		/// <summary>
		/// Test undo/redo for a Custom property.
		/// </summary>
		[Test]
		public void CustomPropertyTests()
		{
			var servLoc = Cache.ServiceLocator;
			var mdc = servLoc.GetInstance<IFwMetaDataCacheManaged>();
			var customCertifiedFlid = mdc.AddCustomField("WfiWordform", "Certified", CellarPropertyType.Boolean, 0);
			var customITsStringFlid = mdc.AddCustomField("WfiWordform", "NewTsStringProp", CellarPropertyType.String, 0);
			var customMultiUnicodeFlid = mdc.AddCustomField("WfiWordform", "MultiUnicodeProp", CellarPropertyType.MultiUnicode, 0);
			var customAtomicReferenceFlid = mdc.AddCustomField("WfiWordform", "NewAtomicRef", CellarPropertyType.ReferenceAtomic, CmPersonTags.kClassId);
			IWfiWordform wf;
			ICmPerson person;

			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add wordform"))
			{
				var lp = Cache.LanguageProject;
				wf = servLoc.GetInstance<IWfiWordformFactory>().Create();
				lp.PeopleOA = servLoc.GetInstance<ICmPossibilityListFactory>().Create();
				var personFactory = servLoc.GetInstance<ICmPersonFactory>();
				person = personFactory.Create();
				lp.PeopleOA.PossibilitiesOS.Add(person);
				undoHelper.RollBack = false;
			}
			m_actionHandler.Commit();

			var sda = Cache.DomainDataByFlid;
			var startingValue = sda.get_BooleanProp(wf.Hvo, customCertifiedFlid);
			var newValue = !startingValue;
			var tsf = Cache.TsStrFactory;
			var userWs = Cache.WritingSystemFactory.UserWs;
			var emptyStr = tsf.EmptyString(userWs);
			var newStringValue = tsf.MakeString("New ITsString", userWs);
			var newUnicodeTsStringValue = tsf.MakeString("New unicode ITsString", userWs);
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "set Certified"))
			{
				sda.SetBoolean(wf.Hvo, customCertifiedFlid, newValue);
				sda.SetString(wf.Hvo, customITsStringFlid, newStringValue);
				sda.SetMultiStringAlt(wf.Hvo, customMultiUnicodeFlid, userWs, newUnicodeTsStringValue);
				sda.SetObjProp(wf.Hvo, customAtomicReferenceFlid, person.Hvo);
				undoHelper.RollBack = false;
			}

			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(startingValue, sda.get_BooleanProp(wf.Hvo, customCertifiedFlid), "Wrong undone boolean.");
			Assert.AreSame(emptyStr, sda.get_StringProp(wf.Hvo, customITsStringFlid), "TsString custom property is not undone to tsf.EmptyString.");
			Assert.AreSame(emptyStr, sda.get_MultiStringAlt(wf.Hvo, customMultiUnicodeFlid, userWs), "MultiUnicode custom property is not undone to tsf.EmptyString.");
			Assert.AreEqual(0, sda.get_ObjectProp(wf.Hvo, customAtomicReferenceFlid), "Add atomic ref not undone in custom property.");
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.AreEqual(newValue, sda.get_BooleanProp(wf.Hvo, customCertifiedFlid), "Wrong redone boolean.");
			Assert.AreSame(newStringValue, sda.get_StringProp(wf.Hvo, customITsStringFlid), "Wrong redone TsString value in custom property.");
			Assert.AreSame(newUnicodeTsStringValue, sda.get_MultiStringAlt(wf.Hvo, customMultiUnicodeFlid, userWs), "Wrong redone MultiUnicode value in custom property.");
			Assert.AreEqual(person.Hvo, sda.get_ObjectProp(wf.Hvo, customAtomicReferenceFlid), "Add atomic ref not redone in custom property.");
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests undo/redo with a UOW that has the deletion of a multiple objects owned by other
		/// deleted objects. This makes sure that object creation during an undo happens in the
		/// correct order. Also tests that redo will re-delete the original objects correctly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandleDeletingOfObjHierarcy()
		{
			IScripture scr;
			IScrBook book;
			IScrSection section;
			IStText content;
			IScrTxtPara para;
			Guid bookGuid;
			Guid sectionGuid;
			Guid textGuid;
			Guid paraGuid;
			IFdoServiceLocator servloc = Cache.ServiceLocator;
			// Create the data used for testing
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "add para"))
			{
				scr = Cache.LangProject.TranslatedScriptureOA;
				book = servloc.GetInstance<IScrBookFactory>().Create(1);
				bookGuid = book.Guid;
				section = servloc.GetInstance<IScrSectionFactory>().Create();
				book.SectionsOS.Add(section);
				sectionGuid = section.Guid;
				content = servloc.GetInstance<IStTextFactory>().Create();
				section.ContentOA = content;
				textGuid = content.Guid;
				para = servloc.GetInstance<IScrTxtParaFactory>().CreateWithStyle(content, "Monkey");
				paraGuid = para.Guid;

				undoHelper.RollBack = false;
			}

			// Remove a book from scripture
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "Delete book"))
			{
				scr.ScriptureBooksOS.Remove(book);

				undoHelper.RollBack = false;
			}

			// Try undo and make sure it put everything back together
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			book = scr.ScriptureBooksOS[0];
			Assert.AreEqual(bookGuid, book.Guid);
			section = book.SectionsOS[0];
			Assert.AreEqual(sectionGuid, section.Guid);
			content = section.ContentOA;
			Assert.AreEqual(textGuid, content.Guid);
			para = (IScrTxtPara)content[0];
			Assert.AreEqual(paraGuid, para.Guid);

			// Undo again to make sure that scripture gets removed
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(scr.ScriptureBooksOS.Count, 0);

			// Redo to make sure that scripture, book, section, etc. are re-created
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			book = scr.ScriptureBooksOS[0];
			Assert.AreEqual(bookGuid, book.Guid);
			section = book.SectionsOS[0];
			Assert.AreEqual(sectionGuid, section.Guid);
			content = section.ContentOA;
			Assert.AreEqual(textGuid, content.Guid);
			para = (IScrTxtPara)content[0];
			Assert.AreEqual(paraGuid, para.Guid);

			// Redo again to make sure the book is deleted
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.AreEqual(0, scr.ScriptureBooksOS.Count);
		}

		/// <summary>
		/// Test that we can merge two units of work into one.
		/// </summary>
		[Test]
		public void MergeUnitsOfWork()
		{
			// The main current application of this is a complex delete followed by inserting a character, so try that.
			IText text = null;
			IStText stText = null;
			IStTxtPara para1 = null;
			IStTxtPara para2 = null;
			UndoableUnitOfWorkHelper.Do("setup", "redo", m_actionHandler,
				() =>
				{
					text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
					//Cache.LangProject.TextsOC.Add(text);
					 stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
					text.ContentsOA = stText;
				   var paraFactory = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>();
					para1 = paraFactory.Create();
					stText.ParagraphsOS.Add(para1);
					para2 = paraFactory.Create();
					stText.ParagraphsOS.Add(para2);
					para1.Contents = Cache.TsStrFactory.MakeString("First Para", Cache.DefaultVernWs);
					para2.Contents = Cache.TsStrFactory.MakeString("Second Thing", Cache.DefaultVernWs);
				});
			// First test change merges two paragraphs.
			UndoableUnitOfWorkHelper.Do("delete", "redo", m_actionHandler,
				() =>
				{
					// move "Thing" to the end of the first paragraph.
					Cache.DomainDataByFlid.MoveString(para2.Hvo, StTxtParaTags.kflidContents, 0, "Second ".Length, "Second Thing".Length,
						para1.Hvo, StTxtParaTags.kflidContents, 0, "First Para".Length, false);
					// Delete " Para".
					para1.Contents = Cache.TsStrFactory.MakeString("First Thing", Cache.DefaultVernWs);
					// Delete para 2 altogether.
					stText.ParagraphsOS.RemoveAt(1);
				});
			UndoableUnitOfWorkHelper.Do("insert", "redo", m_actionHandler,
				() =>
				{
					// Insert a character into para1
					para1.Contents = Cache.TsStrFactory.MakeString("First XThing", Cache.DefaultVernWs);
				});
			var extensions = m_actionHandler as IActionHandlerExtensions;
			extensions.MergeLastTwoUnitsOfWork();
			Assert.That(m_actionHandler.GetUndoText(), Is.EqualTo("delete"), "Should keep the label of the first UOW");
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.That(stText.ParagraphsOS, Has.Count.EqualTo(2), "Should undo both changes");
			Assert.That(para1.Contents.Text, Is.EqualTo("First Para"));
			Assert.That(para2.Contents.Text, Is.EqualTo("Second Thing"));
		}

		/// <summary>
		/// Test undo/redo with attached event handlers on events DoingUndoOrRedo and PropChangedCompleted
		/// </summary>
		[Test]
		public void UndoOrRedoEventsTests()
		{
			// Before we can do undo or redo we need to have actually done something
			// so here is some test data so we can do undos and redos.
			var english = Cache.LanguageProject.CurrentAnalysisWritingSystems.First();
			var firstNewValue = Cache.TsStrFactory.MakeString("Some Value", english.Handle);
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "Some Test Value"))
			{
				Cache.LanguageProject.MainCountry.set_String(english.Handle, firstNewValue);
				undoHelper.RollBack = false;
			}

			int doingUndoOrRedoCounter = 0;
			int didUndoOrRedoCounter = 0;

			// Create some Delegate instances that are going to help us test the IActionHandlerExtension events
			DoingUndoOrRedoDelegate doingCountingDelegate = (cancelArg) => { doingUndoOrRedoCounter++; };
			PropChangedCompletedDelegate didCountingDelegate = (sender, fromUndoRedo) => { if (fromUndoRedo) didUndoOrRedoCounter++; };
			DoingUndoOrRedoDelegate cancelDelegate = (cancelArg) => { cancelArg.Cancel = true; };

			IActionHandlerExtensions actionHandlerExtensions = (IActionHandlerExtensions)m_actionHandler;
			Assert.IsTrue(actionHandlerExtensions != null);

			actionHandlerExtensions.DoingUndoOrRedo += doingCountingDelegate;
			actionHandlerExtensions.PropChangedCompleted += didCountingDelegate;

			Assert.AreEqual(0, doingUndoOrRedoCounter);
			Assert.AreEqual(0, didUndoOrRedoCounter);
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(1, doingUndoOrRedoCounter);
			Assert.AreEqual(1, didUndoOrRedoCounter);

			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.AreEqual(2, doingUndoOrRedoCounter);
			Assert.AreEqual(2, didUndoOrRedoCounter);

			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(3, doingUndoOrRedoCounter);
			Assert.AreEqual(3, didUndoOrRedoCounter);

			// Do another Redo so we can Undo
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Redo());
			Assert.AreEqual(4, doingUndoOrRedoCounter);
			Assert.AreEqual(4, didUndoOrRedoCounter);

			actionHandlerExtensions.DoingUndoOrRedo -= doingCountingDelegate;

			// Now try a cancelled undo
			actionHandlerExtensions.DoingUndoOrRedo += cancelDelegate;
			Assert.AreEqual(UndoResult.kuresError, m_actionHandler.Undo());
			actionHandlerExtensions.DoingUndoOrRedo -= cancelDelegate;
			Assert.AreEqual(4, didUndoOrRedoCounter);

			// Go back to inital state.
			Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
			Assert.AreEqual(5, didUndoOrRedoCounter);

			// Now try a cancelled redo
			actionHandlerExtensions.DoingUndoOrRedo += cancelDelegate;
			Assert.AreEqual(UndoResult.kuresError, m_actionHandler.Redo());
			actionHandlerExtensions.DoingUndoOrRedo -= cancelDelegate;
			Assert.AreEqual(5, didUndoOrRedoCounter);

			actionHandlerExtensions.PropChangedCompleted -= didCountingDelegate;
		}
	}
}
