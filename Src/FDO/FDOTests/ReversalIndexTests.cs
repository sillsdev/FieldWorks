// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Test of added functions of ReversalIndex.
	/// </summary>
	[TestFixture]
	public class ReversalIndexTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Stop the Undo task the base class kicks off,
		/// since this test makes its own
		/// </summary>
		public override void TestSetup()
		{
			base.TestSetup();

			m_actionHandler.EndUndoTask();
		}

		/// <summary>
		/// Tests this function.
		/// </summary>
		[Test]
		public void FindOrCreateReversalEntry()
		{
			int wsEn = Cache.DefaultAnalWs;
			IReversalIndex revIndex = null;
			UndoableUnitOfWorkHelper.Do("undo make index", "redo make index", m_actionHandler,
				() =>
					{
						revIndex =
							Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(wsEn);
					});
			IReversalIndexEntry empty = null;
			UndoableUnitOfWorkHelper.Do("undo make rie", "redo make rie", m_actionHandler,
				() =>
				{
					empty = revIndex.FindOrCreateReversalEntry("");
				});
			IReversalIndexEntry bank = null;
			UndoableUnitOfWorkHelper.Do("undo make rie", "redo make rie", m_actionHandler,
				() =>
				{
					bank = revIndex.FindOrCreateReversalEntry("bank");
				});

			Assert.IsNotNull(bank);
			Assert.AreEqual("bank", bank.LongName);

			m_actionHandler.Undo(); // deletes bank

			IReversalIndexEntry bank2 = null;
			UndoableUnitOfWorkHelper.Do("undo make rie", "redo make rie", m_actionHandler,
				() =>
				{
					bank2 = revIndex.FindOrCreateReversalEntry("bank");
				});
			Assert.AreNotEqual(bank, bank2, "should make a new rie after Undo deletes old one");

			// Enhance JohnT: if we could look for one without creating it, we should test that Redo will
			// reinstate the old object. But we can't Redo that action, because we've made new actions since.

			bank = bank2; // treat that as the base from here on.
			UndoableUnitOfWorkHelper.Do("undo make rie", "redo make rie", m_actionHandler,
				() =>
				{
					bank2 = revIndex.FindOrCreateReversalEntry("bank");
				});
			Assert.AreEqual(bank, bank2, "should find the same RIE for the same name");

			IReversalIndexEntry moneybank = null;
			UndoableUnitOfWorkHelper.Do("undo set rie name", "redo set rie name", m_actionHandler,
				() =>
					{
						bank.ReversalForm.set_String(wsEn, bank.Cache.TsStrFactory.MakeString("moneybank", wsEn));
						moneybank = revIndex.FindOrCreateReversalEntry("moneybank");
					});
			Assert.AreEqual(bank, moneybank, "changing existing name should allow us to find old one under new name");

			m_actionHandler.Undo(); // name is back to 'bank'
			bank = revIndex.FindOrCreateReversalEntry("bank"); // should not need UOW
			Assert.AreEqual(bank, moneybank, "after Undo should find same item under original name");
			m_actionHandler.Redo();
			moneybank = revIndex.FindOrCreateReversalEntry("moneybank");
			Assert.AreEqual(bank, moneybank, "after Redodo should find same item under new name");

			UndoableUnitOfWorkHelper.Do("undo rest of stuff", "redo rest of stuff", m_actionHandler,
				() =>
					{
						bank = revIndex.FindOrCreateReversalEntry("bank");
						Assert.IsNotNull(bank);
						Assert.AreNotEqual(bank, moneybank,
							"after rename, should make new object when looking up old name");

						var riverbank = revIndex.FindOrCreateReversalEntry("bank:of river");
						Assert.AreEqual("of river", riverbank.ShortName);
						Assert.AreEqual("bank: of river", riverbank.LongName);
						Assert.AreEqual(bank, riverbank.Owner);
						var riverbank2 = revIndex.FindOrCreateReversalEntry("bank: of river");
						Assert.AreEqual(riverbank, riverbank2);

						moneybank = revIndex.FindOrCreateReversalEntry("bank: for money");
						Assert.IsNotNull(moneybank);
						Assert.AreNotEqual(riverbank, moneybank);
						Assert.AreEqual("bank: for money", moneybank.LongName);

						var planebank = revIndex.FindOrCreateReversalEntry("bank: tilt:plane");
						Assert.AreEqual("bank: tilt: plane", planebank.LongName);
						Assert.AreEqual(bank, planebank.Owner.Owner);
					});
		}

		/// <summary>
		/// Test setting the ReversalEntriesBulkText of a LexSense.
		/// </summary>
		[Test]
		public void ReversalEntriesBulkText()
		{
			m_actionHandler.BeginUndoTask("undo stuff", "redo stuff");
			int wsEn = Cache.DefaultAnalWs;
			var le = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var ls = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create() as LexSense;
			le.SensesOS.Add(ls);

			ls.ReversalEntriesBulkText.set_String(wsEn, Cache.TsStrFactory.MakeString("bank:of river;riverbank;shore", wsEn));

			Assert.AreEqual(3, ls.ReversalEntriesRC.Count);
			var revIndex = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(wsEn);
			Assert.IsTrue(ls.ReversalEntriesRC.Contains(revIndex.FindOrCreateReversalEntry("bank:of river")), "sense should have bank:of river RE");
			Assert.IsTrue(ls.ReversalEntriesRC.Contains(revIndex.FindOrCreateReversalEntry("riverbank")), "sense should have riverbank RE");
			Assert.IsTrue(ls.ReversalEntriesRC.Contains(revIndex.FindOrCreateReversalEntry("shore")), "sense should have shore RE");

			var ls2 = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create() as LexSense;
			le.SensesOS.Add(ls2);

			ls2.ReversalEntriesBulkText.set_String(wsEn, Cache.TsStrFactory.MakeString("moneylender;invest", wsEn));
			Assert.AreEqual(2, ls2.ReversalEntriesRC.Count);
			Assert.IsTrue(ls2.ReversalEntriesRC.Contains(revIndex.FindOrCreateReversalEntry("moneylender")), "sense should have moneylender RE");
			Assert.IsTrue(ls2.ReversalEntriesRC.Contains(revIndex.FindOrCreateReversalEntry("invest")), "sense should have invest RE");
			Assert.AreEqual(3, ls.ReversalEntriesRC.Count, "other sense should not be affected");

			var oldriverbank = revIndex.FindOrCreateReversalEntry("riverbank");
			ls.ReversalEntriesBulkText.set_String(wsEn, Cache.TsStrFactory.MakeString("bank:of river;shore;river-bank;side", wsEn));
			Assert.AreEqual(4, ls.ReversalEntriesRC.Count);
			Assert.IsTrue(ls.ReversalEntriesRC.Contains(revIndex.FindOrCreateReversalEntry("bank:of river")), "sense should have bank:of river RE");
			Assert.IsTrue(ls.ReversalEntriesRC.Contains(revIndex.FindOrCreateReversalEntry("river-bank")), "sense should have riverbank RE");
			Assert.IsTrue(ls.ReversalEntriesRC.Contains(revIndex.FindOrCreateReversalEntry("shore")), "sense should have shore RE");
			Assert.IsTrue(ls.ReversalEntriesRC.Contains(revIndex.FindOrCreateReversalEntry("side")), "sense should have side RE");
			Assert.IsFalse(oldriverbank.IsValidObject, "old reversal entry with no senses should be deleted");

			m_actionHandler.EndUndoTask();
		}
	}
}
