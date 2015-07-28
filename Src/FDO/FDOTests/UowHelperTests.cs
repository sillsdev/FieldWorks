// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Tests (so far a rather incomplete set) for the UOW helper classes.
	/// </summary>
	[TestFixture]
	public class UowHelperTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// What it says
		/// </summary>
		[Test]
		public void DoSomehow_WorksDuringOtherUow()
		{
			// Baseclass arranges that a UOW is active at the start of all test methods.
			NonUndoableUnitOfWorkHelper.DoSomehow(m_actionHandler, () => MakeEntry("an entry"));
		}

		/// <summary>
		/// What it says
		/// </summary>
		[Test]
		public void DoSomehow_WorksWhenNoUowIsActive()
		{
			m_actionHandler.EndUndoTask();
			NonUndoableUnitOfWorkHelper.DoSomehow(m_actionHandler, () => MakeEntry("an entry"));
		}

		/// <summary>
		/// What it says
		/// </summary>
		[Test]
		public void DoSomehow_WorksDuringPropChanged()
		{
			m_actionHandler.EndUndoTask();
			var propChangeHandler = new PropChangedImplementor() {Parent = this};
			Cache.DomainDataByFlid.AddNotification(propChangeHandler);
			ILexEntry entry = null;
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () => { entry = MakeEntry("an entry"); }); // new object, no PropChanged sent
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler,
				() =>
					entry.LexemeFormOA.Form.VernacularDefaultWritingSystem =
						Cache.TsStrFactory.MakeString("changed", Cache.DefaultVernWs)); // triggers PropChanged, which triggers another UOW
			Assert.That(propChangeHandler.Entry, Is.Not.Null);
		}

		/// <summary>
		/// Copied from StringServicesTests (plus UOW); possibly best for each test set to have own utility functions?
		/// </summary>
		private ILexEntry MakeEntry(string lf)
		{
			ILexEntry entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var form = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = form;
			form.Form.VernacularDefaultWritingSystem =
				Cache.TsStrFactory.MakeString(lf, Cache.DefaultVernWs);
			return entry;
		}
		class PropChangedImplementor : IVwNotifyChange
		{
			public UowHelperTests Parent;
			public ILexEntry Entry;
			public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
			{
				if (Entry == null) // only do it once, or we get a stack overflow
					NonUndoableUnitOfWorkHelper.DoSomehow(Parent.m_actionHandler, () => { Entry = Parent.MakeEntry("another entry"); });
			}
		}
	}

}
