// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CreateModifyTimeTests.cs
// Responsibility: FW Team

using System;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Test the CreateModifyTimeManager
	/// </summary>
	///
	[TestFixture]
	public class CreateModifyTimeTests : MemoryOnlyBackendProviderTestBase
	{
		/// <summary>
		/// Test we can modify an object and its (own) modify time gets set.
		/// </summary>
		[Test]
		public void ModifyMajorObject()
		{
			ILexEntry le = null;
			DateTime current = DateTime.Now.Subtract(TimeSpan.FromMinutes(5.0));
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
				{
					le = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
					le.DateModified = current;
				});
			Assert.AreEqual(current, le.DateModified, "setting DateModified explicitly failed");

			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
				{
					le.CitationForm.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("abc", Cache.DefaultVernWs);
				});
			Assert.IsTrue(DateTime.Compare(le.DateModified, current) > 0,
				"modify time increased setting string prop");
		}
		/// <summary>
		/// Test we can modify a child object and its (indirect owner) modify time gets set.
		/// </summary>
		[Test]
		public void ModifyMinorObject()
		{
			DateTime current = DateTime.Now.Subtract(TimeSpan.FromMinutes(5.0));
			ILexEntry le = null;
			ILexSense ls2 = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
				{
					le = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
					ILexSense ls = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
					le.SensesOS.Add(ls);
					ls2 = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
					ls.SensesOS.Add(ls2);
					le.DateModified = current;
				});
			Assert.AreEqual(current, le.DateModified, "setting DateModified explicitly failed");

			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
				{
					ls2.Gloss.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("abc", Cache.DefaultVernWs);
				});
			Assert.IsTrue(DateTime.Compare(le.DateModified, current) > 0,
				"modify time increased setting string prop of 2-level child");

			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
				{
					le.DateModified = current;
				});
			Assert.AreEqual(current, le.DateModified, "setting DateModified explicitly failed");

			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
				{
					ILexSense ls3 = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
					ls2.SensesOS.Add(ls3);
				});
			Assert.IsTrue(DateTime.Compare(le.DateModified, current) > 0,
				"modify time increased setting object prop of 2-level child");
		}
	}
}
