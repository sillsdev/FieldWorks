// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.XWorks.LexEd;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace LexEdDllTests
{
	/// <summary>
	/// Unit tests for <see cref="DummyReversalIndexEntrySlice"/> which uses <see cref="DummyReversalIndexEntrySliceView"/>
	/// </summary>
	[TestFixture]
	class ReversalEntryViewTests : MemoryOnlyBackendProviderTestBase
	{
		protected ILexEntry m_lexEntry;
		protected DummyReversalIndexEntrySlice m_reversalSlice;
		protected IReversalIndexRepository m_revIndexRepo;
		protected IReversalIndexEntryRepository m_revIndexEntryRepo;

		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_revIndexRepo = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			m_revIndexEntryRepo = Cache.ServiceLocator.GetInstance<IReversalIndexEntryRepository>();
		}

		[TearDown]
		public override void TestTearDown()
		{
			base.TestTearDown();
			m_reversalSlice.DummyControl.Dispose();
			m_reversalSlice.Dispose();
			m_reversalSlice = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => m_lexEntry?.Delete());
			m_lexEntry = null;
		}

		/// <summary>
		/// Tests that a reversal index entry in the dummy cache gets converted into a real one when the focus on the view control is lost.
		/// </summary>
		[Test]
		public void DummyReversalCreatedOnFocusLost()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_lexEntry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				ILexSense sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				m_lexEntry.SensesOS.Add(sense);
			});
			int engWsId = Cache.DefaultAnalWs;
			m_reversalSlice = new DummyReversalIndexEntrySlice(m_lexEntry.SensesOS[0]);
			m_reversalSlice.Cache = Cache;

			// Creating the reversal index before the slice is fully initialized ensures the reversal index gets loaded into the dummy cache
			IReversalIndex ri = m_revIndexRepo.FindOrCreateIndexForWs(engWsId);
			m_reversalSlice.FinishInit();
			DummyReversalIndexEntrySliceView reversalView = m_reversalSlice.DummyControl;
			int indexFirstEntry = 0;
			reversalView.CacheNewDummyEntry(ri.Hvo, engWsId); // Create an additional dummy reversal entry before we edit one
			reversalView.EditRevIndexEntryInCache(ri.Hvo, indexFirstEntry, engWsId, TsStringUtils.MakeString("first", engWsId));

			// The dummy cache will have two dummy reversal index entries, but non exists in the real data yet.
			// The reversal index entry control must maintain a dummy entry at the end to allow a place to click to add new entries.
			Assert.AreEqual(0, m_revIndexEntryRepo.Count);
			Assert.AreEqual(2, reversalView.GetIndexSize(ri.Hvo)); // The second dummy entry will remain a dummy
			reversalView.KillFocus(new Control());
			Assert.AreEqual(1, m_revIndexEntryRepo.Count);
			Assert.AreEqual(2, reversalView.GetIndexSize(ri.Hvo));
			IReversalIndexEntry rie = m_revIndexEntryRepo.AllInstances().First();
			Assert.AreEqual("first", rie.ShortName);
			Assert.AreEqual(1, m_lexEntry.SensesOS[0].ReferringReversalIndexEntries.Count());
			Assert.True(m_lexEntry.SensesOS[0].ReferringReversalIndexEntries.Contains(rie));
		}
	}
}
