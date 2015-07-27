using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.XWorks.LexEd;

namespace LexEdDllTests
{
	class SortReversalSubEntriesTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private IReversalIndexRepository m_revIndexRepo;
		private IReversalIndexEntryFactory m_revIndexEntryFactory;

		[SetUp]
		public void Setup()
		{
			m_revIndexRepo = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			m_revIndexEntryFactory = Cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>();
		}

		[Test]
		public void SortReversalSubEntries_NoReversalIndexesDoesNotThrow()
		{
			// verify test conditions
			Assert.AreEqual(m_revIndexRepo.Count, 0, "Test setup is broken, should be no RIs");
			Assert.DoesNotThrow(()=>SortReversalSubEntries.SortReversalSubEntriesInPlace(Cache));
		}

		[Test]
		public void SortReversalSubEntries_SortWorks()
		{
			var reversalMainEntry = CreateReversalIndexEntry("a");
			var subEntryZ = CreateReversalIndexSubEntry("z", reversalMainEntry);
			var subEntryB = CreateReversalIndexSubEntry("b", reversalMainEntry);
			var subEntryA = CreateReversalIndexSubEntry("a", reversalMainEntry);
			// Verify initial incorrect order
			CollectionAssert.AreEqual(reversalMainEntry.SubentriesOS, new [] { subEntryZ, subEntryB, subEntryA});
			// SUT
			SortReversalSubEntries.SortReversalSubEntriesInPlace(Cache);
			CollectionAssert.AreEqual(reversalMainEntry.SubentriesOS, new[] { subEntryA, subEntryB, subEntryZ });
		}

		protected IReversalIndexEntry CreateReversalIndexEntry(string riForm)
		{
			var revIndexEntry = m_revIndexEntryFactory.Create();
			var wsObj = Cache.LanguageProject.DefaultAnalysisWritingSystem;
			IReversalIndex revIndex = m_revIndexRepo.FindOrCreateIndexForWs(wsObj.Handle);
			//Add an entry to the Reveral index
			revIndex.EntriesOC.Add(revIndexEntry);
			revIndexEntry.ReversalForm.set_String(wsObj.Handle, Cache.TsStrFactory.MakeString(riForm, wsObj.Handle));
			return revIndexEntry;
		}

		protected IReversalIndexEntry CreateReversalIndexSubEntry(string subEntryForm, IReversalIndexEntry indexEntry)
		{
			var wsObj = Cache.LanguageProject.DefaultAnalysisWritingSystem;
			var revIndexEntry = m_revIndexEntryFactory.Create();
			indexEntry.SubentriesOS.Add(revIndexEntry);
			revIndexEntry.ReversalForm.set_String(wsObj.Handle, Cache.TsStrFactory.MakeString(subEntryForm, wsObj.Handle));
			return revIndexEntry;
		}
	}
}
