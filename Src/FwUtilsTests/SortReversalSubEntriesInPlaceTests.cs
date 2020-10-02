// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary />
	public sealed class SortReversalSubEntriesInPlaceTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private IReversalIndexRepository m_revIndexRepo;
		private IReversalIndexEntryFactory m_revIndexEntryFactory;

		#region Overrides of MemoryOnlyBackendProviderRestoredForEachTestTestBase
		/// <inheritdoc />
		public override void TestSetup()
		{
			base.TestSetup();
			m_revIndexRepo = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			m_revIndexEntryFactory = Cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>();
		}
		#endregion

		/// <summary />
		[Test]
		public void SortReversalSubEntries_NoReversalIndexesDoesNotThrow()
		{
			// verify test conditions
			Assert.AreEqual(m_revIndexRepo.Count, 0, "Test setup is broken, should be no RIs");
			Assert.DoesNotThrow(() => Cache.SortReversalSubEntriesInPlace());
		}

		/// <summary />
		[Test]
		public void SortReversalSubEntries_SortWorks()
		{
			var reversalMainEntry = CreateReversalIndexEntry("a");
			var subEntryZ = CreateReversalIndexSubEntry("z", reversalMainEntry);
			var subEntryB = CreateReversalIndexSubEntry("b", reversalMainEntry);
			var subEntryA = CreateReversalIndexSubEntry("a", reversalMainEntry);
			// Verify initial incorrect order
			CollectionAssert.AreEqual(reversalMainEntry.SubentriesOS, new[] { subEntryZ, subEntryB, subEntryA });

			// SUT
			Cache.SortReversalSubEntriesInPlace();

			// Verify
			CollectionAssert.AreEqual(reversalMainEntry.SubentriesOS, new[] { subEntryA, subEntryB, subEntryZ });
		}

		/// <summary />
		private IReversalIndexEntry CreateReversalIndexEntry(string riForm)
		{
			var revIndexEntry = m_revIndexEntryFactory.Create();
			var wsObj = Cache.LanguageProject.DefaultAnalysisWritingSystem;
			var revIndex = m_revIndexRepo.FindOrCreateIndexForWs(wsObj.Handle);
			//Add an entry to the Reversal index
			revIndex.EntriesOC.Add(revIndexEntry);
			revIndexEntry.ReversalForm.set_String(wsObj.Handle, TsStringUtils.MakeString(riForm, wsObj.Handle));
			return revIndexEntry;
		}

		/// <summary />
		private IReversalIndexEntry CreateReversalIndexSubEntry(string subEntryForm, IReversalIndexEntry indexEntry)
		{
			var wsObj = Cache.LanguageProject.DefaultAnalysisWritingSystem;
			var revIndexEntry = m_revIndexEntryFactory.Create();
			indexEntry.SubentriesOS.Add(revIndexEntry);
			revIndexEntry.ReversalForm.set_String(wsObj.Handle, TsStringUtils.MakeString(subEntryForm, wsObj.Handle));
			return revIndexEntry;
		}
	}
}