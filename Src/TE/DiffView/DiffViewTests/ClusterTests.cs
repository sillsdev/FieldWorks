// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ClusterTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the <see cref="Cluster"/>,  <see cref="ClusterListHelper"/>,
	/// <see cref="ClusterType"/>, and <see cref="OverlapInfo"/> classes.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ClusterTests : ScrInMemoryFdoTestBase
	{
		private enum ClusterKind
		{
			ScrSection,
			ScrVerse
		}

		// member variables for testing
		private IScrBook m_genesis;
		private IScrBook m_genesisRevision;

		private ITsString m_tssVerse; // text to include in a verse

		#region Setup

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_genesis = null;
			m_genesisRevision = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to only create a book with no content, heading, title, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_genesis = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			m_genesisRevision = m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis");

			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "verse text", null);
			m_tssVerse = strBldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_genesis = null;
			m_genesisRevision = null;

			base.Exit();
		}
		#endregion

		#region Section Overlap Cluster tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineSectionOverlapClusters"/> method for sections
		/// that match refs exactly.
		/// </summary>
		/// <remarks> Here is a diagram of which sections will match. The data pattern is
		/// deliberately inverted from SectionOverlap_CloseRefs(), so that we better verify
		/// the way DetermineSectionOverlapClusters() finds matching sections.
		///  Current  Revision
		///		0 -----	0 Gen 1:1-3
		///		1 --\	1
		///		2	 \-	2 Gen 1:9-15
		///		3 -----	3 Gen 2:1-3
		///		4 --\	4
		///			 \	5
		///			  \-6 Gen 3:9-15
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionOverlap_ExactRefs()
		{
			CheckDisposed();

			// Create sections for the current version of Genesis
			IScrSection section0Cur = CreateSection(m_genesis, "Gen 1:1-3", 01001001, 01001003);
			IScrSection section1Cur = CreateSection(m_genesis, "Gen 1:9-15", 01001009, 01001015);
			IScrSection section2Cur = CreateSection(m_genesis, "Gen 1:40-50", 01001040, 01001050);
			IScrSection section3Cur = CreateSection(m_genesis, "Gen 2:1-3", 01002001, 01002003);
			IScrSection section4Cur = CreateSection(m_genesis, "Gen 3:9-15", 01003009, 01003015);

			// Create sections for the revised version of Genesis
			IScrSection section0Rev = CreateSection(m_genesisRevision, "Gen 1:1-3", 01001001, 01001003);
			IScrSection section1Rev = CreateSection(m_genesisRevision, "Gen 1:4-8", 01001004, 01001008);
			IScrSection section2Rev = CreateSection(m_genesisRevision, "Gen 1:9-15", 01001009, 01001015);
			IScrSection section3Rev = CreateSection(m_genesisRevision, "Gen 2:1-3", 01002001, 01002003);
			IScrSection section4Rev = CreateSection(m_genesisRevision, "Gen 2:4-8", 01002004, 01002008);
			IScrSection section5Rev = CreateSection(m_genesisRevision, "Gen 2:9-15", 01002009, 01002015);
			IScrSection section6Rev = CreateSection(m_genesisRevision, "Gen 3:9-15", 01003009, 01003015);

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineSectionOverlapClusters(m_genesis, m_genesisRevision,
				m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(8, clusterList.Count);

			// Verify cluster 0: Current section 0 matches Revision section 0
			VerifySectionCluster(clusterList[0],
				01001001, 01001003, ClusterType.MatchedItems, section0Cur, section0Rev);

			// Verify cluster 1: Current section missing, Revision section 1 is added
			VerifySectionCluster(clusterList[1],
				01001004, 01001008, ClusterType.MissingInCurrent, null, section1Rev, 1);

			// Verify cluster 2: Current section 1 matches Revision section 2
			VerifySectionCluster(clusterList[2],
				01001009, 01001015, ClusterType.MatchedItems, section1Cur, section2Rev);

			// Verify cluster 3: Current section 2 is added, Revision section missing
			VerifySectionCluster(clusterList[3],
				01001040, 01001050, ClusterType.AddedToCurrent, section2Cur, null, 3);

			// Verify cluster 4: Current section 3 matches Revision section 3
			VerifySectionCluster(clusterList[4],
				01002001, 01002003, ClusterType.MatchedItems, section3Cur, section3Rev);

			// Verify cluster 5: Current section missing, Revision section 4 is added
			VerifySectionCluster(clusterList[5],
				01002004, 01002008, ClusterType.MissingInCurrent, null, section4Rev, 4);

			// Verify cluster 6: Current section missing, Revision section 5 is added
			VerifySectionCluster(clusterList[6],
				01002009, 01002015, ClusterType.MissingInCurrent, null, section5Rev, 4);

			// Verify cluster 7: Current section 4 matches Revision section 6
			VerifySectionCluster(clusterList[7],
				01003009, 01003015, ClusterType.MatchedItems, section4Cur, section6Rev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineSectionOverlapClusters"/> method for sections
		/// that match refs closely, not exactly.
		/// </summary>
		/// <remarks> Here is a diagram of which sections will match. The data pattern is
		/// deliberately inverted from SectionOverlap_ExactRefs(), so that we better verify
		/// the way DetermineSectionOverlapClusters() finds matching sections.
		///  Revision  Current
		///		0 -----	0 Gen 1:1-3
		///		1 --\	1
		///		2	 \-	2 Gen 1:9-15
		///		3 -----	3 Gen 2:1-3
		///		4 --\	4
		///			 \	5
		///			  \-6 Gen 3:9-15
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionOverlap_CloseRefs()
		{
			CheckDisposed();

			// Create sections for the revised version of Genesis
			IScrSection section0Rev = CreateSection(m_genesisRevision, "Gen 1:2-3", 01001002, 01001003);
			IScrSection section1Rev = CreateSection(m_genesisRevision, "Gen 1:9-19", 01001009, 01001019);
			IScrSection section2Rev = CreateSection(m_genesisRevision, "Gen 1:40-50", 01001040, 01001050);
			IScrSection section3Rev = CreateSection(m_genesisRevision, "Gen 2:2", 01002002, 01002002);
			IScrSection section4Rev = CreateSection(m_genesisRevision, "Gen 3:5-19", 01003005, 01003019);

			// Create sections for the current version of Genesis
			IScrSection section0Cur = CreateSection(m_genesis, "Gen 1:1-3", 01001001, 01001003);
			IScrSection section1Cur = CreateSection(m_genesis, "Gen 1:5-8", 01001005, 01001008);
			IScrSection section2Cur = CreateSection(m_genesis, "Gen 1:9-15", 01001009, 01001015);
			IScrSection section3Cur = CreateSection(m_genesis, "Gen 2:1-3", 01002001, 01002003);
			IScrSection section4Cur = CreateSection(m_genesis, "Gen 2:4-8", 01002004, 01002008);
			IScrSection section5Cur = CreateSection(m_genesis, "Gen 2:9-15", 01002009, 01002015);
			IScrSection section6Cur = CreateSection(m_genesis, "Gen 3:9-15", 01003009, 01003015);

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineSectionOverlapClusters(m_genesis, m_genesisRevision,
				m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(8, clusterList.Count);

			// Verify cluster 0: Current section 0 matches Revision section 0
			VerifySectionCluster(clusterList[0],
				01001001, 01001003, ClusterType.MatchedItems, section0Cur, section0Rev);

			// Verify cluster 1: Current section 1 is added, Revision section missing
			VerifySectionCluster(clusterList[1],
				01001005, 01001008, ClusterType.AddedToCurrent, section1Cur, null, 1);

			// Verify cluster 2: Current section 2 matches Revision section 1
			VerifySectionCluster(clusterList[2],
				01001009, 01001019, ClusterType.MatchedItems, section2Cur, section1Rev);

			// Verify cluster 3: Current section missing, Revision section 2 is added
			VerifySectionCluster(clusterList[3],
				01001040, 01001050, ClusterType.MissingInCurrent, null, section2Rev, 3);

			// Verify cluster 4: Current section 3 matches Revision section 3
			VerifySectionCluster(clusterList[4],
				01002001, 01002003, ClusterType.MatchedItems, section3Cur, section3Rev);

			// Verify cluster 5: Current section 4 is added, Revision section missing
			VerifySectionCluster(clusterList[5],
				01002004, 01002008, ClusterType.AddedToCurrent, section4Cur, null, 4);

			// Verify cluster 6: Current section 5 is added, Revision section missing
			VerifySectionCluster(clusterList[6],
				01002009, 01002015, ClusterType.AddedToCurrent, section5Cur, null, 4);

			// Verify cluster 7: Current section 6 matches Revision section 4
			VerifySectionCluster(clusterList[7],
				01003005, 01003019, ClusterType.MatchedItems, section6Cur, section4Rev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineSectionOverlapClusters"/> method for sections
		/// that have only minimal overlap.
		/// </summary>
		/// <remarks> Here is a diagram of which sections will match.
		///  Revision         Current
		///	 Gen 1:1-40 ----- Gen 1:20
		///	 Gen 2:1-30 ----- Gen 2:30-50
		///	 Gen 3:30-49 ---- Gen 3:9-31
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionOverlap_MinimalOverlap()
		{
			CheckDisposed();

			// Create sections for the revised version of Genesis
			IScrSection section0Rev = CreateSection(m_genesisRevision, "Gen 1:1-40", 01001001, 01001040);
			IScrSection section1Rev = CreateSection(m_genesisRevision, "Gen 2:1-30", 01002001, 01002030);
			IScrSection section2Rev = CreateSection(m_genesisRevision, "Gen 3:30-49", 01003030, 01003049);

			// Create sections for the current version of Genesis
			IScrSection section0Cur = CreateSection(m_genesis, "Gen 1:20", 01001020, 01001020);
			IScrSection section1Cur = CreateSection(m_genesis, "Gen 2:30-50", 01002030, 01002050);
			IScrSection section2Cur = CreateSection(m_genesis, "Gen 3:9-31", 01003009, 01003031);

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineSectionOverlapClusters(m_genesis, m_genesisRevision,
				m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(3, clusterList.Count);

			// Verify cluster 0: Current section 0 matches Revision section 0
			VerifySectionCluster(clusterList[0],
				01001001, 01001040, ClusterType.MatchedItems, section0Cur, section0Rev);

			// Verify cluster 1: Current section 1 matches Revision section 1
			VerifySectionCluster(clusterList[1],
				01002001, 01002050, ClusterType.MatchedItems, section1Cur, section1Rev);

			// Verify cluster 2: Current section 2 matches Revision section 2
			VerifySectionCluster(clusterList[2],
				01003009, 01003049, ClusterType.MatchedItems, section2Cur, section2Rev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineSectionOverlapClusters"/> method for sections
		/// that are added at the beginning and end of the Current, and have adjacent refs.
		/// </summary>
		/// <remarks> Here is a diagram of which sections will match.
		///  Revision      Current
		///                 0 Gen 1:1
		///                 1 Gen 1:2
		///	0 Gen 1:3 ----- 2 Gen 1:3
		///		            3 Gen 1:4
		///		            4 Gen 1:5
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionOverlap_AddedSectionsInCurrent()
		{
			CheckDisposed();

			// Create sections for the revised version of Genesis
			IScrSection section0Rev = CreateSection(m_genesisRevision, "Gen 1:3", 01001003, 01001003);

			// Create sections for the current version of Genesis
			IScrSection section0Cur = CreateSection(m_genesis, "Gen 1:1", 01001001, 01001001);
			IScrSection section1Cur = CreateSection(m_genesis, "Gen 1:2", 01001002, 01001002);
			IScrSection section2Cur = CreateSection(m_genesis, "Gen 1:3", 01001003, 01001003);
			IScrSection section3Cur = CreateSection(m_genesis, "Gen 1:4", 01001004, 01001004);
			IScrSection section4Cur = CreateSection(m_genesis, "Gen 1:5", 01001005, 01001005);

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineSectionOverlapClusters(m_genesis, m_genesisRevision,
				m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(5, clusterList.Count);

			// Verify cluster 0: Current section 0 is added, Revision section missing
			VerifySectionCluster(clusterList[0],
				01001001, 01001001, ClusterType.AddedToCurrent, section0Cur, null, 0);

			// Verify cluster 1: Current section 1 is added, Revision section missing
			VerifySectionCluster(clusterList[1],
				01001002, 01001002, ClusterType.AddedToCurrent, section1Cur, null, 0);

			// Verify cluster 2: Current section 2 matches Revision section 0
			VerifySectionCluster(clusterList[2],
				01001003, 01001003, ClusterType.MatchedItems, section2Cur, section0Rev);

			// Verify cluster 3: Current section 3 is added, Revision section missing
			VerifySectionCluster(clusterList[3],
				01001004, 01001004, ClusterType.AddedToCurrent, section3Cur, null, 1);

			// Verify cluster 4: Current section 4 is added, Revision section missing
			VerifySectionCluster(clusterList[4],
				01001005, 01001005, ClusterType.AddedToCurrent, section4Cur, null, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineSectionOverlapClusters"/> method for sections
		/// that are added at the beginning and end of the Revision, and have adjacent refs.
		/// </summary>
		/// <remarks> Here is a diagram of which sections will match.
		///    Revision          Current
		///     0 Gen 1:1
		///     1 Gen 1:2
		///		2 Gen 1:3 ------- 0 Gen 1:3
		///		3 Gen 1:4
		///		4 Gen 1:5
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionOverlap_AddedSectionsInRevision()
		{
			CheckDisposed();

			// Create sections for the revised version of Genesis
			IScrSection section0Rev = CreateSection(m_genesisRevision, "Gen 1:1", 01001001, 01001001);
			IScrSection section1Rev = CreateSection(m_genesisRevision, "Gen 1:2", 01001002, 01001002);
			IScrSection section2Rev = CreateSection(m_genesisRevision, "Gen 1:3", 01001003, 01001003);
			IScrSection section3Rev = CreateSection(m_genesisRevision, "Gen 1:4", 01001004, 01001004);
			IScrSection section4Rev = CreateSection(m_genesisRevision, "Gen 1:5", 01001005, 01001005);

			// Create sections for the current version of Genesis
			IScrSection section0Cur = CreateSection(m_genesis, "Gen 1:3", 01001003, 01001003);

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineSectionOverlapClusters(m_genesis, m_genesisRevision,
				m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(5, clusterList.Count);

			// Verify cluster 1: Current section missing, Revision section 0 is added
			VerifySectionCluster(clusterList[0],
				01001001, 01001001, ClusterType.MissingInCurrent, null, section0Rev, 0);

			// Verify cluster 1: Current section missing, Revision section 1 is added
			VerifySectionCluster(clusterList[1],
				01001002, 01001002, ClusterType.MissingInCurrent, null, section1Rev, 0);

			// Verify cluster 2: Current section 0 matches Revision section 2
			VerifySectionCluster(clusterList[2],
				01001003, 01001003, ClusterType.MatchedItems, section0Cur, section2Rev);

			// Verify cluster 3: Current section missing, Revision section 3 is added
			VerifySectionCluster(clusterList[3],
				01001004, 01001004, ClusterType.MissingInCurrent, null, section3Rev, 1);

			// Verify cluster 4: Current section missing, Revision section 4 is added
			VerifySectionCluster(clusterList[4],
				01001005, 01001005, ClusterType.MissingInCurrent, null, section4Rev, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineSectionOverlapClusters"/> method for proper
		/// detection and formation of a split cluster
		/// </summary>
		/// <remarks> Here is a diagram of which sections will match.
		///  Revision      Current
		/// 0 Gen 1:1-4     0 Gen 1:1-3
		///                 1 Gen 1:4-5
		/// 1 Gen 1:6-8     2 Gen 1:6-10
		/// 2 Gen 1:9-10
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionOverlap_SectionSplitOrMerged()
		{
			CheckDisposed();

			// Create sections for the revised version of Genesis
			IScrSection section0Rev = CreateSection(m_genesisRevision, "Gen 1:1-4", 01001001, 01001004);
			IScrSection section1Rev = CreateSection(m_genesisRevision, "Gen 1:6-8", 01001006, 01001008);
			IScrSection section2Rev = CreateSection(m_genesisRevision, "Gen 1:9-10", 01001009, 01001010);

			// Create sections for the current version of Genesis
			IScrSection section0Cur = CreateSection(m_genesis, "Gen 1:1-3", 01001001, 01001003);
			IScrSection section1Cur = CreateSection(m_genesis, "Gen 1:4-5", 01001004, 01001005);
			IScrSection section2Cur = CreateSection(m_genesis, "Gen 1:6-10", 01001006, 01001010);

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineSectionOverlapClusters(m_genesis, m_genesisRevision,
				m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(2, clusterList.Count);

			// Verify cluster 0: Revision section 0 has been split into Current sections 0 & 1
			List<IScrSection> expectedItemsCur =
				new List<IScrSection>(new IScrSection[] { section0Cur, section1Cur });
			List<IScrSection> expectedItemsRev =
				new List<IScrSection>(new IScrSection[] { section0Rev });
			VerifySectionCluster(clusterList[0],
				01001001, 01001005, ClusterType.SplitInCurrent, expectedItemsCur, expectedItemsRev);

			// Verify cluster 1: Revision sections 1 & 2 have been merged to form Current section 2
			expectedItemsCur = new List<IScrSection>(new IScrSection[] { section2Cur });
			expectedItemsRev = new List<IScrSection>(new IScrSection[] { section1Rev, section2Rev });
			VerifySectionCluster(clusterList[1],
				01001006, 01001010, ClusterType.MergedInCurrent, expectedItemsCur, expectedItemsRev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineSectionOverlapClusters"/> method for sections
		/// that have multiple overlaps in both directions.
		/// </summary>
		/// <remarks> Here is a diagram of the sections. (-) items overlap with two in the other
		/// book, (*) items overlap with three in the other book. All connect to make one big
		/// rat's nest of a cluster. Oh my.
		///  Revision         Current
		///	 Gen 1:1-6	 -	  Gen 1:2-4
		///					- Gen 1:5-10
		///	 Gen 1:9-2:10*	  Gen 2:1
		///	 Gen 2:14		* Gen 2:8-20
		///  Gen 2:16-19
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionOverlap_RatsNest()
		{
			CheckDisposed();

			// Create sections for the revised version of Genesis
			IScrSection section0Rev = CreateSection(m_genesisRevision, "Gen 1:1-6", 01001001, 01001006);
			IScrSection section1Rev = CreateSection(m_genesisRevision, "Gen 1:9-2:10", 01001009, 01002010);
			IScrSection section2Rev = CreateSection(m_genesisRevision, "Gen 2:14", 01002014, 01002014);
			IScrSection section3Rev = CreateSection(m_genesisRevision, "Gen 2:16-19", 01002016, 01002019);

			// Create sections for the current version of Genesis
			IScrSection section0Cur = CreateSection(m_genesis, "Gen 1:2-4", 01001002, 01001004);
			IScrSection section1Cur = CreateSection(m_genesis, "Gen 1:5-10", 01001005, 01001010);
			IScrSection section2Cur = CreateSection(m_genesis, "Gen 2:1", 01002001, 01002001);
			IScrSection section3Cur = CreateSection(m_genesis, "Gen 2:8-20", 01002008, 01002020);

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineSectionOverlapClusters(m_genesis, m_genesisRevision,
				m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(1, clusterList.Count);

			// Verify cluster 0: all Current sections overlap all Revision sections
			List<IScrSection> expectedItemsCur =
				new List<IScrSection>(new IScrSection[] { section0Cur, section1Cur, section2Cur, section3Cur });
			List<IScrSection> expectedItemsRev =
				new List<IScrSection>(new IScrSection[] { section0Rev, section1Rev, section2Rev, section3Rev });
			VerifySectionCluster((Cluster)clusterList[0],
				01001001, 01002020, ClusterType.MultipleInBoth, expectedItemsCur, expectedItemsRev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineSectionOverlapClusters"/> method for
		/// sections that are out-of-order.
		/// </summary>
		/// <remarks> Here is a diagram of which sections will match. The Current section with
		/// chapter 3 is placed out of order.
		///  Revision         Current
		///	 Gen 1:1-40 ----- Gen 1:20
		///	 Gen 2:1-30 --  * Gen 3:9-31
		///               \
		///	               -- Gen 2:30-50
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionOverlap_OutOfOrder()
		{
			CheckDisposed();

			// Create sections for the revised version of Genesis
			IScrSection section0Rev = CreateSection(m_genesisRevision, "Gen 1:1-40", 01001001, 01001040);
			IScrSection section1Rev = CreateSection(m_genesisRevision, "Gen 2:1-30", 01002001, 01002030);

			// Create sections for the current version of Genesis
			IScrSection section0Cur = CreateSection(m_genesis, "Gen 1:20", 01001020, 01001020);
			IScrSection section1Cur = CreateSection(m_genesis, "Gen 3:9-31", 01003009, 01003031);
			IScrSection section2Cur = CreateSection(m_genesis, "Gen 2:30-50", 01002030, 01002050);

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineSectionOverlapClusters(m_genesis, m_genesisRevision,
				m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(3, clusterList.Count);

			// Verify cluster 0: Current section 0 matches Revision section 0
			VerifySectionCluster(clusterList[0],
				01001001, 01001040, ClusterType.MatchedItems, section0Cur, section0Rev);

			// Verify cluster 1: Current section 2 matches Revision section 1
			VerifySectionCluster(clusterList[1],
				01002001, 01002050, ClusterType.MatchedItems, section2Cur, section1Rev);

			// Verify cluster 2: Current section 1 is added, albeit out-of-order
			VerifySectionCluster(clusterList[2],
				01003009, 01003031, ClusterType.AddedToCurrent, section1Cur, null, 2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineSectionOverlapClusters"/> method for
		/// duplicated verse ranges in different sections of a book.
		/// </summary>
		/// <remarks> Here is a diagram of which sections will match. Verses 3 and 4 occur twice
		/// in the Revision.
		///  Revision         Current
		///	 Gen 1:1 ----- Gen 1:1-3
		///	 Gen 1:2-4 --- Gen 1:4
		///  Gen 1:3-4 -/
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionOverlap_DuplicateSectionReferences()
		{
			CheckDisposed();

			// Create sections for the revised version of Genesis
			IScrSection section0Rev = CreateSection(m_genesisRevision, "Gen 1:1", 01001001, 01001001);
			IScrSection section1Rev = CreateSection(m_genesisRevision, "Gen 1:2-4", 01001002, 01001004);
			IScrSection section2Rev = CreateSection(m_genesisRevision, "Gen 1:3-4", 01001003, 01001004);

			// Create sections for the current version of Genesis
			IScrSection section0Cur = CreateSection(m_genesis, "Gen 1:1-3", 01001001, 01001003);
			IScrSection section1Cur = CreateSection(m_genesis, "Gen 1:4", 01001004, 01001004);

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineSectionOverlapClusters(m_genesis, m_genesisRevision,
				m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(1, clusterList.Count);

			// Verify cluster 0: all Current sections overlap all Revision sections
			List<IScrSection> expectedItemsCur =
				new List<IScrSection>(new IScrSection[] { section0Cur, section1Cur });
			List<IScrSection> expectedItemsRev =
				new List<IScrSection>(new IScrSection[] { section0Rev, section1Rev, section2Rev });
			VerifySectionCluster((Cluster)clusterList[0],
				01001001, 01001004, ClusterType.MultipleInBoth, expectedItemsCur, expectedItemsRev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineSectionOverlapClusters"/> method for sections
		/// that are added and the references of the revision completely encompass the
		/// references of the current.
		/// </summary>
		/// <remarks>
		/// revision section has chapters 1-8 and the current has chapters 3-6
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionOverlap_EnclosedRefs1()
		{
			CheckDisposed();

			// Build the "current" paras: 3,6
			IScrSection section1Curr = CreateSection(m_genesis, "My Section");
			StTxtPara para1Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(section1Curr.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1Curr, "3", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1Curr, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1Curr, "This is the first paragraph", Cache.DefaultVernWs);

			StTxtPara para2Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(section1Curr.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2Curr, "6", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2Curr, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2Curr, "This is the second paragraph", Cache.DefaultVernWs);
			section1Curr.AdjustReferences();

			// Build the "revision" paras: 1,2,4,5,7,8
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My Section");
			StTxtPara para1Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(section1Rev.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1Rev, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1Rev, "This is the first paragraph", Cache.DefaultVernWs);

			StTxtPara para2Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(section1Rev.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2Rev, "2", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2Rev, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2Rev, "This is the second paragraph", Cache.DefaultVernWs);

			StTxtPara para3Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(section1Rev.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para3Rev, "4", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para3Rev, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para3Rev, "This is the third paragraph", Cache.DefaultVernWs);

			StTxtPara para4Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(section1Rev.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para4Rev, "5", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para4Rev, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para4Rev, "This is the fourth paragraph", Cache.DefaultVernWs);

			StTxtPara para5Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(section1Rev.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para5Rev, "7", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para5Rev, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para5Rev, "This is the fifth paragraph", Cache.DefaultVernWs);

			StTxtPara para6Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(section1Rev.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para6Rev, "8", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para6Rev, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para6Rev, "This is the sixth paragraph", Cache.DefaultVernWs);
			section1Rev.AdjustReferences();

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineSectionOverlapClusters(m_genesis, m_genesisRevision,
				m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(1, clusterList.Count);

			// Verify cluster
			VerifySectionCluster(clusterList[0],
				01001001, 01008001, ClusterType.MatchedItems, section1Curr, section1Rev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineSectionOverlapClusters"/> method for sections
		/// that are added and the references of the revision completely encompass the
		/// references of the current.
		/// </summary>
		/// <remarks>
		/// revision section has chapters 3-6 and the current has chapters 1-8
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionOverlap_EnclosedRefs2()
		{
			CheckDisposed();

			// Build the "revision" paras: 3-6
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My Section");
			StTxtPara para1Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(section1Rev.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1Rev, "3", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1Rev, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1Rev, "This is the first paragraph", Cache.DefaultVernWs);

			StTxtPara para2Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(section1Rev.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2Rev, "6", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2Rev, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2Rev, "This is the second paragraph", Cache.DefaultVernWs);
			section1Rev.AdjustReferences();

			// Build the "current" paras: 1,2,4,5,7,8
			IScrSection section1Curr = CreateSection(m_genesis, "My Section");
			StTxtPara para1Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(section1Curr.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1Curr, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1Curr, "This is the first paragraph", Cache.DefaultVernWs);

			StTxtPara para2Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(section1Curr.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2Curr, "2", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2Curr, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2Curr, "This is the second paragraph", Cache.DefaultVernWs);

			StTxtPara para3Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(section1Curr.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para3Curr, "4", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para3Curr, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para3Curr, "This is the third paragraph", Cache.DefaultVernWs);

			StTxtPara para4Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(section1Curr.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para4Curr, "5", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para4Curr, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para4Curr, "This is the fourth paragraph", Cache.DefaultVernWs);

			StTxtPara para5Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(section1Curr.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para5Curr, "7", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para5Curr, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para5Curr, "This is the fifth paragraph", Cache.DefaultVernWs);

			StTxtPara para6Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(section1Curr.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para6Curr, "8", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para6Curr, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para6Curr, "This is the sixth paragraph", Cache.DefaultVernWs);
			section1Curr.AdjustReferences();

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineSectionOverlapClusters(m_genesis, m_genesisRevision,
				m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(1, clusterList.Count);

			// Verify cluster
			VerifySectionCluster(clusterList[0],
				01001001, 01008001, ClusterType.MatchedItems, section1Curr, section1Rev);
		}
		#endregion

		#region Section Head Correlation Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SectionHeadCorrelationHelper.DetermineSectionHeadCorrelationClusters"/>
		/// method to verify accurate section head correlation amongst matching pairs.
		/// </summary>
		/// <remarks> Here is a diagram of which section heads will correlate.
		///  Revision         Current
		///	 Gen 1:1-5 ----- Gen 1:2-7
		///	 Gen 1:7-8 ----- Gen 1:8-12
		///  Gen 1:9-20 ---- Gen 1:11
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionHeadCorrelation_Pairs()
		{
			CheckDisposed();

			// Create sections for the revised version of Genesis
			IScrSection section0Rev = CreateSection(m_genesisRevision, "Gen 1:1-5", 01001001, 01001005);
			IScrSection section1Rev = CreateSection(m_genesisRevision, "Gen 1:7-8", 01001007, 01001008);
			IScrSection section2Rev = CreateSection(m_genesisRevision, "Gen 1:9-20", 01001009, 01001020);

			// Create sections for the current version of Genesis
			IScrSection section0Cur = CreateSection(m_genesis, "Gen 1:2-7", 01001002, 01001007);
			IScrSection section1Cur = CreateSection(m_genesis, "Gen 1:8-12", 01001008, 01001012);
			IScrSection section2Cur = CreateSection(m_genesis, "Gen 1:11", 01001011, 01001011);

			// Make  the multiple-overlap cluster
			List<Cluster> clusterOverlapList = ClusterListHelper.DetermineSectionOverlapClusters(m_genesis, m_genesisRevision,
				m_inMemoryCache.Cache);
			Assert.AreEqual(1, clusterOverlapList.Count);
			// check the details before we proceed
			List<IScrSection> expectedItemsCur =
				new List<IScrSection>(new IScrSection[] { section0Cur, section1Cur, section2Cur });
			List<IScrSection> expectedItemsRev =
				new List<IScrSection>(new IScrSection[] { section0Rev, section1Rev, section2Rev });
			VerifySectionCluster((Cluster)clusterOverlapList[0],
				01001001, 01001020, ClusterType.MultipleInBoth, expectedItemsCur, expectedItemsRev);

			// Call the method under test
			List<Cluster> correlationList =
				SectionHeadCorrelationHelper.DetermineSectionHeadCorrelationClusters(clusterOverlapList[0]);

			// Verify the section head correlations
			Assert.AreEqual(3, correlationList.Count);
			// we expect three pairs, even though section1Curr has two possible correlations
			VerifySectionCluster(correlationList[0],
				01001001, 01001007, ClusterType.MatchedItems, section0Cur, section0Rev);
			VerifySectionCluster(correlationList[1],
				01001007, 01001012, ClusterType.MatchedItems, section1Cur, section1Rev);
			VerifySectionCluster(correlationList[2],
				01001009, 01001020, ClusterType.MatchedItems, section2Cur, section2Rev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SectionHeadCorrelationHelper.DetermineSectionHeadCorrelationClusters"/>
		/// method to verify accurate section head correlation with added/deleted section heads.
		/// </summary>
		/// <remarks> Here is a diagram of which section heads will correlate.
		///  Revision         Current
		///	 Gen 1:1-10    /- Gen 1:10-12
		///	 Gen 1:12-15 -/   Gen 1:15
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionHeadCorrelation_Added()
		{
			CheckDisposed();

			// Create sections for the revised version of Genesis
			IScrSection section0Rev = CreateSection(m_genesisRevision, "Gen 1:1-10", 01001001, 01001010);
			IScrSection section1Rev = CreateSection(m_genesisRevision, "Gen 1:12-15", 01001012, 01001015);

			// Create sections for the current version of Genesis
			IScrSection section0Cur = CreateSection(m_genesis, "Gen 1:10-12", 01001010, 01001012);
			IScrSection section1Cur = CreateSection(m_genesis, "Gen 1:15", 01001015, 01001015);

			// Make  the multiple-overlap cluster
			List<Cluster> clusterOverlapList = ClusterListHelper.DetermineSectionOverlapClusters(m_genesis, m_genesisRevision,
				m_inMemoryCache.Cache);
			Assert.AreEqual(1, clusterOverlapList.Count);
			// check the details before we proceed
			List<IScrSection> expectedItemsCur =
				new List<IScrSection>(new IScrSection[] { section0Cur, section1Cur });
			List<IScrSection> expectedItemsRev =
				new List<IScrSection>(new IScrSection[] { section0Rev, section1Rev });
			VerifySectionCluster((Cluster)clusterOverlapList[0],
				01001001, 01001015, ClusterType.MultipleInBoth, expectedItemsCur, expectedItemsRev);

			// Call the method under test
			List<Cluster> correlationList =
				SectionHeadCorrelationHelper.DetermineSectionHeadCorrelationClusters(clusterOverlapList[0]);

			// Verify the section head correlations
			Assert.AreEqual(3, correlationList.Count);
			// we expect three pairs, even though section1Curr has two possible correlations
			VerifySectionCluster(correlationList[0],
				01001001, 01001010, ClusterType.MissingInCurrent, null, section0Rev, -1);
			VerifySectionCluster(correlationList[1],
				01001010, 01001015, ClusterType.MatchedItems, section0Cur, section1Rev);
			VerifySectionCluster(correlationList[2],
				01001015, 01001015, ClusterType.AddedToCurrent, section1Cur, null, -1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SectionHeadCorrelationHelper.DetermineSectionHeadCorrelationClusters"/>
		/// method to verify accurate section head correlation when the intro and/or ending sections
		/// are correlated by default because nothing else potentially correlates with them.
		/// </summary>
		/// <remarks> Here is a diagram of which section heads will correlate.
		///  Revision         Current
		///	 Gen 1:1-15 ----- Gen 1:15-20
		///	 Gen 1:20-21 ---- Gen 1:21-22
		///  Gen 1:22-40 ---- Gen 1:40
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionHeadCorrelation_BegEnd()
		{
			CheckDisposed();

			// Create sections for the revised version of Genesis
			IScrSection section0Rev = CreateSection(m_genesisRevision, "Gen 1:1-15", 01001001, 01001015);
			IScrSection section1Rev = CreateSection(m_genesisRevision, "Gen 1:20-21", 01001020, 01001021);
			IScrSection section2Rev = CreateSection(m_genesisRevision, "Gen 1:22-40", 01001022, 01001040);

			// Create sections for the current version of Genesis
			IScrSection section0Cur = CreateSection(m_genesis, "Gen 1:15-20", 01001015, 01001020);
			IScrSection section1Cur = CreateSection(m_genesis, "Gen 1:21-22", 01001021, 01001022);
			IScrSection section2Cur = CreateSection(m_genesis, "Gen 1:40", 01001040, 01001040);

			// Make  the multiple-overlap cluster
			List<Cluster> clusterOverlapList =
				ClusterListHelper.DetermineSectionOverlapClusters(m_genesis, m_genesisRevision,
					m_inMemoryCache.Cache);
			Assert.AreEqual(1, clusterOverlapList.Count);
			// check the details before we proceed
			List<IScrSection> expectedItemsCur =
				new List<IScrSection>(new IScrSection[] { section0Cur, section1Cur, section2Cur });
			List<IScrSection> expectedItemsRev =
				new List<IScrSection>(new IScrSection[] { section0Rev, section1Rev, section2Rev });
			VerifySectionCluster(clusterOverlapList[0],
				01001001, 01001040, ClusterType.MultipleInBoth, expectedItemsCur, expectedItemsRev);

			// Call the method under test
			List<Cluster> correlationList =
				SectionHeadCorrelationHelper.DetermineSectionHeadCorrelationClusters(clusterOverlapList[0]);

			// Verify the section head correlations
			Assert.AreEqual(3, correlationList.Count);
			// we expect three pairs, even though section1Curr has two possible correlations
			VerifySectionCluster(correlationList[0],
				01001001, 01001020, ClusterType.MatchedItems, section0Cur, section0Rev);
			VerifySectionCluster(correlationList[1],
				01001020, 01001022, ClusterType.MatchedItems, section1Cur, section1Rev);
			VerifySectionCluster(correlationList[2],
				01001022, 01001040, ClusterType.MatchedItems, section2Cur, section2Rev);
		}
		#endregion

		#region ScrVerse OverLap Cluster tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineScrVerseOverlapClusters"/> method for
		/// ScrVerses that are added at the beginning and end of the Current, and have adjacent refs.
		/// </summary>
		/// <remarks> Here is a diagram of which ScrVerses will match.
		///  Revision      Current
		///                 0 Gen 1:1
		///                 1 Gen 1:2
		///	0 Gen 1:3 ----- 2 Gen 1:3
		///		            3 Gen 1:4
		///		            4 Gen 1:5
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScrVerseOverlap_AddedScrVersesInCurrent()
		{
			CheckDisposed();

			// Create ScrVerses for the revised version of Genesis
			List<ScrVerse> scrVersesRev = new List<ScrVerse>(1);
			scrVersesRev.Add(new ScrVerse(01001003, 01001003, null, null, 0, 0, 0, 0, false, false));

			// Create ScrVerses for the current version of Genesis
			List<ScrVerse> scrVersesCurr = new List<ScrVerse>(5);
			scrVersesCurr.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, 0, 0, false, false));
			scrVersesCurr.Add(new ScrVerse(01001002, 01001002, null, null, 0, 0, 0, 0, false, false));
			scrVersesCurr.Add(new ScrVerse(01001003, 01001003, null, null, 0, 0, 0, 0, false, false));
			scrVersesCurr.Add(new ScrVerse(01001004, 01001004, null, null, 0, 0, 0, 0, false, false));
			scrVersesCurr.Add(new ScrVerse(01001005, 01001005, null, null, 0, 0, 0, 0, false, false));

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineScrVerseOverlapClusters(
				scrVersesCurr, scrVersesRev, m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(5, clusterList.Count);

			// Verify cluster 0: Current ScrVerse 0 is added, Revision ScrVerse missing
			VerifyScrVerseCluster(clusterList[0],
				01001001, 01001001, ClusterType.AddedToCurrent, scrVersesCurr[0], null, 0);

			// Verify cluster 1: Current ScrVerse 1 is added, Revision ScrVerse missing
			VerifyScrVerseCluster(clusterList[1],
				01001002, 01001002, ClusterType.AddedToCurrent, scrVersesCurr[1], null, 0);

			// Verify cluster 2: Current ScrVerse 2 matches Revision ScrVerse 0
			VerifyScrVerseCluster(clusterList[2],
				01001003, 01001003, ClusterType.MatchedItems, scrVersesCurr[2], scrVersesRev[0]);

			// Verify cluster 3: Current ScrVerse 3 is added, Revision ScrVerse missing
			VerifyScrVerseCluster(clusterList[3],
				01001004, 01001004, ClusterType.AddedToCurrent, scrVersesCurr[3], null, 1);

			// Verify cluster 4: Current ScrVerse 4 is added, Revision ScrVerse missing
			VerifyScrVerseCluster(clusterList[4],
				01001005, 01001005, ClusterType.AddedToCurrent, scrVersesCurr[4], null, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineScrVerseOverlapClusters"/> method for
		/// ScrVerses that are added at the beginning and end of the Current, and have adjacent refs.
		/// </summary>
		/// <remarks> Here is a diagram of which ScrVerses will match.
		///  Revision      Current
		///  0 Gen 1:1
		///  1 Gen 1:2
		///	 2 Gen 1:3 --- 0 Gen 1:3
		///	 3 Gen 1:4
		///	 4 Gen 1:5
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScrVerseOverlap_MissingScrVersesInCurrent()
		{
			CheckDisposed();

			// Create ScrVerses for the revision version of Genesis
			List<ScrVerse> scrVersesRev = new List<ScrVerse>(5);
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, 0, 0, false, false));
			scrVersesRev.Add(new ScrVerse(01001002, 01001002, null, null, 0, 0, 0, 0, false, false));
			scrVersesRev.Add(new ScrVerse(01001003, 01001003, null, null, 0, 0, 0, 0, false, false));
			scrVersesRev.Add(new ScrVerse(01001004, 01001004, null, null, 0, 0, 0, 0, false, false));
			scrVersesRev.Add(new ScrVerse(01001005, 01001005, null, null, 0, 0, 0, 0, false, false));

			// Create ScrVerses for the current version of Genesis
			List<ScrVerse> scrVersesCur = new List<ScrVerse>(1);
			scrVersesCur.Add(new ScrVerse(01001003, 01001003, null, null, 0, 0, 0, 0, false, false));

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineScrVerseOverlapClusters(
				scrVersesCur, scrVersesRev, m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(5, clusterList.Count);

			// Verify cluster 0: Current ScrVerse missing, Revision ScrVerse 0 is added
			VerifyScrVerseCluster(clusterList[0],
				01001001, 01001001, ClusterType.MissingInCurrent, null, scrVersesRev[0], 0);

			// Verify cluster 1: Current ScrVerse 1 missing, Revision ScrVerse 1 is added
			VerifyScrVerseCluster(clusterList[1],
				01001002, 01001002, ClusterType.MissingInCurrent, null, scrVersesRev[1], 0);

			// Verify cluster 2: Current ScrVerse 2 matches Revision ScrVerse 0
			VerifyScrVerseCluster(clusterList[2],
				01001003, 01001003, ClusterType.MatchedItems, scrVersesCur[0], scrVersesRev[2]);

			// Verify cluster 3: Current ScrVerse missing, Revision ScrVerse 3 is added
			VerifyScrVerseCluster(clusterList[3],
				01001004, 01001004, ClusterType.MissingInCurrent, null, scrVersesRev[3], 1);

			// Verify cluster 4: Current ScrVerse missing, Revision ScrVerse 4 is added
			VerifyScrVerseCluster(clusterList[4],
				01001005, 01001005, ClusterType.MissingInCurrent, null, scrVersesRev[4], 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineScrVerseOverlapClusters"/> method for
		/// ScrVerses when the current repeats verse 1 two times out of order.
		/// </summary>
		/// <remarks> Here is a diagram of which ScrVerses will match.
		///  Revision      Current
		///  0 Gen 1:1 --- 0 Gen 1:1
		///  1 Gen 1:2 --- 1 Gen 1:2
		///	 2 Gen 1:3 -\  2 Gen 1:1
		///	 3 Gen 1:4   \-3 Gen 1:3
		///	 4 Gen 1:5     4 Gen 1:1
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScrVerseOverlap_RepeatedFirstVerseCurr()
		{
			CheckDisposed();

			// Create ScrVerses for the revision version of Genesis
			List<ScrVerse> scrVersesRev = new List<ScrVerse>(5);
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesRev.Add(new ScrVerse(01001002, 01001002, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesRev.Add(new ScrVerse(01001003, 01001003, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesRev.Add(new ScrVerse(01001004, 01001004, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesRev.Add(new ScrVerse(01001005, 01001005, m_tssVerse, null, 0, 0, 0, 0, false, false));

			// Create ScrVerses for the current version of Genesis
			List<ScrVerse> scrVersesCur = new List<ScrVerse>(5);
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesCur.Add(new ScrVerse(01001002, 01001002, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesCur.Add(new ScrVerse(01001003, 01001003, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false));

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineScrVerseOverlapClusters(
				scrVersesCur, scrVersesRev, m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(7, clusterList.Count);

			// Verify cluster 0: Current ScrVerse 0 matches Revision ScrVerse 0
			VerifyScrVerseCluster(clusterList[0],
				01001001, 01001001, ClusterType.MatchedItems, scrVersesCur[0], scrVersesRev[0]);

			// Verify cluster 1: Current ScrVerse 1 matches Revision ScrVerse 1
			VerifyScrVerseCluster(clusterList[1],
				01001002, 01001002, ClusterType.MatchedItems, scrVersesCur[1], scrVersesRev[1]);

			// Verify cluster 2: Current ScrVerse 2 missing in Revision
			VerifyScrVerseCluster(clusterList[2],
				01001001, 01001001, ClusterType.AddedToCurrent, scrVersesCur[2], null, 2);

			// Verify cluster 3: Current ScrVerse 3 matches Revision ScrVerse 2
			VerifyScrVerseCluster(clusterList[3],
				01001003, 01001003, ClusterType.MatchedItems, scrVersesCur[3], scrVersesRev[2]);

			// Verify cluster 4: Current ScrVerse 4 missing in Revision
			VerifyScrVerseCluster(clusterList[4],
				01001001, 01001001, ClusterType.AddedToCurrent, scrVersesCur[4], null, 3);

			// Verify cluster 5: Current ScrVerse missing, Revision ScrVerse 3 added
			VerifyScrVerseCluster(clusterList[5],
				01001004, 01001004, ClusterType.MissingInCurrent, null, scrVersesRev[3], 5);

			// Verify cluster 6: Current ScrVerse missing, Revision ScrVerse 4 added
			VerifyScrVerseCluster(clusterList[6],
				01001005, 01001005, ClusterType.MissingInCurrent, null, scrVersesRev[4], 5);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineScrVerseOverlapClusters"/> method for
		/// ScrVerses when the revision repeats verse 1 two times out of order.
		/// </summary>
		/// <remarks> Here is a diagram of which ScrVerses will match.
		///  Revision      Current
		///  0 Gen 1:1 --- 0 Gen 1:1
		///  1 Gen 1:2 --- 1 Gen 1:2
		///	 2 Gen 1:1   /-2 Gen 1:3
		///	 3 Gen 1:3 -/  3 Gen 1:4
		///	 4 Gen 1:1     4 Gen 1:5
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		//[Ignore("Fix needs to be implemented")]
		public void ScrVerseOverlap_RepeatedFirstVerseRev()
		{
			CheckDisposed();

			// Create ScrVerses for the revision version of Genesis
			List<ScrVerse> scrVersesRev = new List<ScrVerse>(5);
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesRev.Add(new ScrVerse(01001002, 01001002, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesRev.Add(new ScrVerse(01001003, 01001003, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false));

			// Create ScrVerses for the current version of Genesis
			List<ScrVerse> scrVersesCur = new List<ScrVerse>(5);
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesCur.Add(new ScrVerse(01001002, 01001002, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesCur.Add(new ScrVerse(01001003, 01001003, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesCur.Add(new ScrVerse(01001004, 01001004, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesCur.Add(new ScrVerse(01001005, 01001005, m_tssVerse, null, 0, 0, 0, 0, false, false));

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineScrVerseOverlapClusters(
				scrVersesCur, scrVersesRev, m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(7, clusterList.Count);

			// Verify cluster 0: Current ScrVerse 0 matches Revision ScrVerse 0
			VerifyScrVerseCluster(clusterList[0],
				01001001, 01001001, ClusterType.MatchedItems, scrVersesCur[0], scrVersesRev[0]);

			// Verify cluster 1: Current ScrVerse 1 matches Revision ScrVerse 1
			VerifyScrVerseCluster(clusterList[1],
				01001002, 01001002, ClusterType.MatchedItems, scrVersesCur[1], scrVersesRev[1]);

			// Verify cluster 2: Current ScrVerse missing, Revision ScrVerse 2 added
			VerifyScrVerseCluster(clusterList[2],
				01001001, 01001001, ClusterType.MissingInCurrent, null, scrVersesRev[2], 2);

			// Verify cluster 3: Current ScrVerse 2 matches Revision ScrVerse 3
			VerifyScrVerseCluster(clusterList[3],
				01001003, 01001003, ClusterType.MatchedItems, scrVersesCur[2], scrVersesRev[3]);

			// Verify cluster 4: Current ScrVerse missing, Revision ScrVerse 4 added
			VerifyScrVerseCluster(clusterList[4],
				01001001, 01001001, ClusterType.MissingInCurrent, null, scrVersesRev[4], 3);

			// Verify cluster 5: Current ScrVerse 3 missing in Revision
			VerifyScrVerseCluster(clusterList[5],
				01001004, 01001004, ClusterType.AddedToCurrent, scrVersesCur[3], null, 5);

			// Verify cluster 6: Current ScrVerse 4 missing in Revision
			VerifyScrVerseCluster(clusterList[6],
				01001005, 01001005, ClusterType.AddedToCurrent, scrVersesCur[4], null, 5);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineScrVerseOverlapClusters"/> method for
		/// ScrVerses when the current repeats verse 4 two times out of order.
		/// </summary>
		/// <remarks> Here is a diagram of which ScrVerses will match.
		///  Revision      Current
		///  0 Gen 1:1  /--0 Gen 1:4
		///  1 Gen 1:2  |  1 Gen 1:1
		///  2 Gen 1:3  |  2 Gen 1:4
		///	 3 Gen 1:4-/   3 Gen 1:2
		///	               4 Gen 1:4
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScrVerseOverlap_RepeatedLastVerseCurr()
		{
			CheckDisposed();

			// Create ScrVerses for the revision version of Genesis
			List<ScrVerse> scrVersesRev = new List<ScrVerse>(5);
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesRev.Add(new ScrVerse(01001002, 01001002, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesRev.Add(new ScrVerse(01001003, 01001003, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesRev.Add(new ScrVerse(01001004, 01001004, m_tssVerse, null, 0, 0, 0, 0, false, false));

			// Create ScrVerses for the current version of Genesis
			List<ScrVerse> scrVersesCur = new List<ScrVerse>(5);
			scrVersesCur.Add(new ScrVerse(01001004, 01001004, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesCur.Add(new ScrVerse(01001004, 01001004, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesCur.Add(new ScrVerse(01001002, 01001002, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesCur.Add(new ScrVerse(01001004, 01001004, m_tssVerse, null, 0, 0, 0, 0, false, false));

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineScrVerseOverlapClusters(
				scrVersesCur, scrVersesRev, m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(8, clusterList.Count);

			// Verify cluster 0: Current ScrVerse missing, Revision ScrVerse 0 added
			VerifyScrVerseCluster(clusterList[0],
				01001001, 01001001, ClusterType.MissingInCurrent, null, scrVersesRev[0], 0);

			// Verify cluster 1: Current ScrVerse missing, Revision ScrVerse 1 added
			VerifyScrVerseCluster(clusterList[1],
				01001002, 01001002, ClusterType.MissingInCurrent, null, scrVersesRev[1], 0);

			// Verify cluster 1: Current ScrVerse missing, Revision ScrVerse 2 added
			VerifyScrVerseCluster(clusterList[2],
				01001003, 01001003, ClusterType.MissingInCurrent, null, scrVersesRev[2], 0);

			// Verify cluster 0: Current ScrVerse 0 matches Revision ScrVerse 3
			VerifyScrVerseCluster(clusterList[3],
				01001004, 01001004, ClusterType.MatchedItems, scrVersesCur[0], scrVersesRev[3]);

			// Verify cluster 1: Current ScrVerse 1 missing in Revision
			VerifyScrVerseCluster(clusterList[4],
				01001001, 01001001, ClusterType.AddedToCurrent, scrVersesCur[1], null, 4);

			// Verify cluster 2: Current ScrVerse 2 missing in Revision
			VerifyScrVerseCluster(clusterList[5],
				01001004, 01001004, ClusterType.AddedToCurrent, scrVersesCur[2], null, 4);

			// Verify cluster 2: Current ScrVerse 3 missing in Revision
			VerifyScrVerseCluster(clusterList[6],
				01001002, 01001002, ClusterType.AddedToCurrent, scrVersesCur[3], null, 4);

			// Verify cluster : Current ScrVerse 4 missing in Revision
			VerifyScrVerseCluster(clusterList[7],
				01001004, 01001004, ClusterType.AddedToCurrent, scrVersesCur[4], null, 4);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineScrVerseOverlapClusters"/> method for
		/// ScrVerses when the revision repeats verse 1 two times out of order.
		/// </summary>
		/// <remarks> Here is a diagram of which ScrVerses will match.
		///  Revision      Current
		///  0 Gen 1:4-\   0 Gen 1:1
		///  1 Gen 1:1  |  1 Gen 1:2
		///	 2 Gen 1:4  |  2 Gen 1:3
		///	 3 Gen 1:2   \-3 Gen 1:4
		///	 4 Gen 1:4
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScrVerseOverlap_RepeatedLastVerseRev()
		{
			CheckDisposed();

			// Create ScrVerses for the revision version of Genesis
			List<ScrVerse> scrVersesRev = new List<ScrVerse>(5);
			scrVersesRev.Add(new ScrVerse(01001004, 01001004, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesRev.Add(new ScrVerse(01001004, 01001004, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesRev.Add(new ScrVerse(01001002, 01001002, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesRev.Add(new ScrVerse(01001004, 01001004, m_tssVerse, null, 0, 0, 0, 0, false, false));

			// Create ScrVerses for the current version of Genesis
			List<ScrVerse> scrVersesCur = new List<ScrVerse>(5);
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesCur.Add(new ScrVerse(01001002, 01001002, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesCur.Add(new ScrVerse(01001003, 01001003, m_tssVerse, null, 0, 0, 0, 0, false, false));
			scrVersesCur.Add(new ScrVerse(01001004, 01001004, m_tssVerse, null, 0, 0, 0, 0, false, false));

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineScrVerseOverlapClusters(
				scrVersesCur, scrVersesRev, m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(8, clusterList.Count);

			// Verify cluster 0: Current ScrVerse 0 added, Revision missing
			VerifyScrVerseCluster(clusterList[0],
				01001001, 01001001, ClusterType.AddedToCurrent, scrVersesCur[0], null, 0);

			// Verify cluster 1: Current ScrVerse 1 added, Revision missing
			VerifyScrVerseCluster(clusterList[1],
				01001002, 01001002, ClusterType.AddedToCurrent, scrVersesCur[1], null, 0);

			// Verify cluster 1: Current ScrVerse 2 added, Revision missing
			VerifyScrVerseCluster(clusterList[2],
				01001003, 01001003, ClusterType.AddedToCurrent, scrVersesCur[2], null, 0);

			// Verify cluster 0: Current ScrVerse 0 matches Revision ScrVerse 3
			VerifyScrVerseCluster(clusterList[3],
				01001004, 01001004, ClusterType.MatchedItems, scrVersesCur[3], scrVersesRev[0]);

			// Verify cluster 1: Current ScrVerse missing, Revision ScrVerse 1 added
			VerifyScrVerseCluster(clusterList[4],
				01001001, 01001001, ClusterType.MissingInCurrent, null, scrVersesRev[1], 4);

			// Verify cluster 2: Current ScrVerse missing, Revision ScrVerse 2 added
			VerifyScrVerseCluster(clusterList[5],
				01001004, 01001004, ClusterType.MissingInCurrent, null, scrVersesRev[2], 4);

			// Verify cluster 2: Current ScrVerse missing, Revision ScrVerse 3 added
			VerifyScrVerseCluster(clusterList[6],
				01001002, 01001002, ClusterType.MissingInCurrent, null, scrVersesRev[3], 4);

			// Verify cluster : Current ScrVerse missing, Revision ScrVerse 4 added
			VerifyScrVerseCluster(clusterList[7],
				01001004, 01001004, ClusterType.MissingInCurrent, null, scrVersesRev[4], 4);
		}

		#region Stanza Break tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineScrVerseOverlapClusters"/> method for
		/// ScrVerses when the revision contains a Stanza Break and the current has two
		/// verses.
		/// </summary>
		/// <remarks> Here is a diagram of which ScrVerses will match.
		///  Revision      Current
		///	 [Stanza Break]
		///					1 Gen 1:1
		///					2 Gen 1:2
		///					3 Gen 1:3
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScrVerseOverlap_StanzaOnlyInRevision()
		{
			CheckDisposed();

			// Create ScrVerses for the revision version of Genesis
			List<ScrVerse> scrVersesRev = new List<ScrVerse>(1);
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));

			// Create ScrVerses for the current version of Genesis
			List<ScrVerse> scrVersesCur = new List<ScrVerse>(3);
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));
			scrVersesCur.Add(new ScrVerse(01001002, 01001002, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));
			scrVersesCur.Add(new ScrVerse(01001003, 01001003, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineScrVerseOverlapClusters(
				scrVersesCur, scrVersesRev, m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(4, clusterList.Count);

			// Verify cluster 0: Current ScrVerse 0 added
			VerifyScrVerseCluster(clusterList[0],
				01001001, 01001001, ClusterType.AddedToCurrent, scrVersesCur[0], null, 0);

			// Verify cluster 1: Revision ScrVerse 0 (stanza break) added
			VerifyScrVerseCluster(clusterList[1],
				01001001, 01001001, ClusterType.MissingInCurrent, null, scrVersesRev[0], 0);

			// Verify cluster 2: Current ScrVerse 0 added
			VerifyScrVerseCluster(clusterList[2],
				01001002, 01001002, ClusterType.AddedToCurrent, scrVersesCur[1], null, 1);

			// Verify cluster 3: Current ScrVerse 1 added
			VerifyScrVerseCluster(clusterList[3],
				01001003, 01001003, ClusterType.AddedToCurrent, scrVersesCur[2], null, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineScrVerseOverlapClusters"/> method for
		/// ScrVerses when the current begins with a Stanza Break.
		/// </summary>
		/// <remarks> Here is a diagram of which ScrVerses will match.
		///  Revision      Current
		///					[Stanza Break]
		///  0 Chap 1	--	0 Chap 1
		///  1 Gen 1:1  --	1 Gen 1:1
		///	 2 Gen 1:2
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScrVerseOverlap_AddedStanzaBeforeFirstScrVerse()
		{
			CheckDisposed();

			// Create ScrVerses for the revision version of Genesis
			List<ScrVerse> scrVersesRev = new List<ScrVerse>(3);
			scrVersesRev.Add(new ScrVerse(01001000, 01001000, m_tssVerse, null, 0, 0, 0, 0, true, false, false, false));
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));
			scrVersesRev.Add(new ScrVerse(01001002, 01001002, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));

			// Create ScrVerses for the current version of Genesis
			List<ScrVerse> scrVersesCur = new List<ScrVerse>(3);
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));
			scrVersesCur.Add(new ScrVerse(01001000, 01001000, m_tssVerse, null, 0, 0, 0, 0, true, false, false, false));
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineScrVerseOverlapClusters(
				scrVersesCur, scrVersesRev, m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(4, clusterList.Count);

			// Verify cluster 0: Current ScrVerse 0 added, Revision missing
			VerifyScrVerseCluster(clusterList[0],
				01001001, 01001001, ClusterType.AddedToCurrent, scrVersesCur[0], null, 0);

			// Verify cluster 1: Current ScrVerse 1 matches Revision ScrVerse 0
			VerifyScrVerseCluster(clusterList[1],
				01001000, 01001000, ClusterType.MatchedItems, scrVersesCur[1], scrVersesRev[0]);

			// Verify cluster 2: Current ScrVerse 2 matches Revision ScrVerse 1
			VerifyScrVerseCluster(clusterList[2],
				01001001, 01001001, ClusterType.MatchedItems, scrVersesCur[2], scrVersesRev[1]);

			// Verify cluster 3: Missing in Current, Revision ScrVerse 3
			VerifyScrVerseCluster(clusterList[3],
				01001002, 01001002, ClusterType.MissingInCurrent, null, scrVersesRev[2], 3);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineScrVerseOverlapClusters"/> method for
		/// ScrVerses when the current and revision begin with multiple (but unequal) stanza
		/// breaks.
		/// </summary>
		/// <remarks> Here is a diagram of which ScrVerses will match.
		///  Revision			Current
		///	 [Stanza Break] --	[Stanza Break]
		///  [Stanza Break] --	[Stanza Break]
		///	 					[Stanza Break]
		///  1 Gen 1:1		--	1 Gen 1:1
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScrVerseOverlap_MultipleStanzaLeadingParasCurr()
		{
			CheckDisposed();

			// Create ScrVerses for the revision version of Genesis
			List<ScrVerse> scrVersesRev = new List<ScrVerse>(3);
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));

			// Create ScrVerses for the current version of Genesis
			List<ScrVerse> scrVersesCur = new List<ScrVerse>(4);
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineScrVerseOverlapClusters(
				scrVersesCur, scrVersesRev, m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(4, clusterList.Count);

			// Verify cluster 0: Current ScrVerse 0 matches Revision ScrVerse 0
			VerifyScrVerseCluster(clusterList[0],
				01001001, 01001001, ClusterType.MatchedItems, scrVersesCur[0], scrVersesRev[0]);

			// Verify cluster 1: Current ScrVerse 1 matches Revision ScrVerse 1
			VerifyScrVerseCluster(clusterList[1],
							01001001, 01001001, ClusterType.MatchedItems, scrVersesCur[1], scrVersesRev[1]);

			//Verify Cluster 2 is Added to current
			VerifyScrVerseCluster(clusterList[2],
				01001001, 01001001, ClusterType.AddedToCurrent, scrVersesCur[2], null, 2);

			// Verify cluster 3: Current ScrVerse 3 matches Revision ScrVerse 2
			VerifyScrVerseCluster(clusterList[3],
				01001001, 01001001, ClusterType.MatchedItems, scrVersesCur[3], scrVersesRev[2]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineScrVerseOverlapClusters"/> method for
		/// ScrVerses when the revision begins with one more stanza than the current.
		/// </summary>
		/// <remarks> Here is a diagram of which ScrVerses will match.
		///  Revision      Current
		///	 [Stanza Break] --	[Stanza Break]
		///  [Stanza Break] --	[Stanza Break]
		///	 [Stanza Break]
		///  1 Gen 1:1		--	1 Gen 1:1
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScrVerseOverlap_MultipleStanzaLeadingParasRev()
		{
			CheckDisposed();

			// Create ScrVerses for the revision version of Genesis
			List<ScrVerse> scrVersesRev = new List<ScrVerse>(3);
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));

			// Create ScrVerses for the current version of Genesis
			List<ScrVerse> scrVersesCur = new List<ScrVerse>(4);
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineScrVerseOverlapClusters(
				scrVersesCur, scrVersesRev, m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(4, clusterList.Count);

			// Verify cluster 0: Current ScrVerse 0 matches Revision ScrVerse 0
			VerifyScrVerseCluster(clusterList[0],
				01001001, 01001001, ClusterType.MatchedItems, scrVersesCur[0], scrVersesRev[0]);

			// Verify cluster 1: Current ScrVerse 1 matches Revision ScrVerse 1
			VerifyScrVerseCluster(clusterList[1],
							01001001, 01001001, ClusterType.MatchedItems, scrVersesCur[1], scrVersesRev[1]);

			//Verify Cluster 2 is Missing in current
			VerifyScrVerseCluster(clusterList[2],
				01001001, 01001001, ClusterType.MissingInCurrent, null, scrVersesRev[2], 2);

			// Verify cluster 3: Current ScrVerse 2 matches Revision ScrVerse 3
			VerifyScrVerseCluster(clusterList[3],
				01001001, 01001001, ClusterType.MatchedItems, scrVersesCur[2], scrVersesRev[3]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineScrVerseOverlapClusters"/> method for
		/// ScrVerses when a verse in the current ends with one more stanza break than the
		/// revision.
		/// </summary>
		/// <remarks> Here is a diagram of which ScrVerses will match.
		///  Revision			Current
		///  0 Chap 1		 --	0 Chap 1
		///  1 Gen 1:1		 --	1 Gen 1:1
		///	 2 [Stanza Break]--	2[Stanza Break]
		///  					3[Stanza Break]
		///  3	Gen 1:2		 --	4 Gen 1:2
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScrVerseOverlap_AddedStanzaAfterMidScrVerse()
		{
			CheckDisposed();

			// Create ScrVerses for the revision version of Genesis
			List<ScrVerse> scrVersesRev = new List<ScrVerse>(4);
			scrVersesRev.Add(new ScrVerse(01001000, 01001000, m_tssVerse, null, 0, 0, 0, 0, true, false, false, false));
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 1, 0, 0, 0, false, false, false, false));
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));
			scrVersesRev.Add(new ScrVerse(01001002, 01001002, m_tssVerse, null, 3, 0, 0, 0, false, false, true, false));

			// Create ScrVerses for the current version of Genesis
			List<ScrVerse> scrVersesCur = new List<ScrVerse>(5);
			scrVersesCur.Add(new ScrVerse(01001000, 01001000, m_tssVerse, null, 0, 0, 0, 0, true, false, false, false));
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 1, 0, 0, 0, false, false, false, false));
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));
			scrVersesCur.Add(new ScrVerse(01001002, 01001002, m_tssVerse, null, 4, 0, 0, 0, false, false, true, false));

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineScrVerseOverlapClusters(
				scrVersesCur, scrVersesRev, m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(5, clusterList.Count);

			// Verify cluster 0: Current ScrVerse 0 matches Revision ScrVerse 0
			VerifyScrVerseCluster(clusterList[0],
				01001000, 01001000, ClusterType.MatchedItems, scrVersesCur[0], scrVersesRev[0]);

			// Verify cluster 1: Current ScrVerse 1 matches Revision ScrVerse 1
			VerifyScrVerseCluster(clusterList[1],
				01001001, 01001001, ClusterType.MatchedItems, scrVersesCur[1], scrVersesRev[1]);

			// Verify cluster 2: Added to Current, Revision ScrVerse 3
			VerifyScrVerseCluster(clusterList[2],
				01001001, 01001001, ClusterType.MatchedItems, scrVersesCur[2], scrVersesRev[2]);

			// Verify cluster 3: Current ScrVerse 2 added to current
			VerifyScrVerseCluster(clusterList[3],
				01001001, 01001001, ClusterType.AddedToCurrent, scrVersesCur[3], null, 3);

			// Verify cluster 4: Current ScrVerse 4 matches Revision ScrVerse 3
			VerifyScrVerseCluster(clusterList[4],
				01001002, 01001002, ClusterType.MatchedItems, scrVersesCur[4], scrVersesRev[3]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineScrVerseOverlapClusters"/> method for
		/// ScrVerses when the current has a stanza break in the middle of a verse (cluster).
		/// </summary>
		/// <remarks> Here is a diagram of which ScrVerses will match.
		///  Revision			Current
		///  0 Chap 1		--	0 Chap 1
		///					  /	1 Gen 1:1
		///	 1 Gen 1:1    ---|	2[Stanza Break]
		///  				  \	3 "Gen1:1 continued"
		///  **2	Gen 1:2		--	4 Gen 1:2
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScrVerseOverlap_AddedStanzaInMiddleOfScrVerse()
		{
			CheckDisposed();

			// Create ScrVerses for the current version of Genesis
			List<ScrVerse> scrVersesCur = new List<ScrVerse>(5);
			scrVersesCur.Add(new ScrVerse(01001000, 01001000, m_tssVerse, null, 0, 0, 0, 0, true, false, false, false));
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 2, 0, 0, 0, false, false, true, false));
			scrVersesCur.Add(new ScrVerse(01001002, 01001002, m_tssVerse, null, 2, 0, 0, 0, false, false, true, false));

			// Create ScrVerses for the revision version of Genesis
			List<ScrVerse> scrVersesRev = new List<ScrVerse>(3);
			scrVersesRev.Add(new ScrVerse(01001000, 01001000, m_tssVerse, null, 0, 0, 0, 0, true, false, false, false));
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));
			scrVersesRev.Add(new ScrVerse(01001002, 01001002, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineScrVerseOverlapClusters(
				scrVersesCur, scrVersesRev, m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(5, clusterList.Count);

			// Verify cluster 0: Current ScrVerse 0 matches Revision ScrVerse 0
			VerifyScrVerseCluster(clusterList[0],
				01001000, 01001000, ClusterType.MatchedItems, scrVersesCur[0], scrVersesRev[0]);

			// Verify cluster 1: Current ScrVerse 1 matches Revision ScrVerse 1
			VerifyScrVerseCluster(clusterList[1],
				01001001, 01001001, ClusterType.MatchedItems, scrVersesCur[1], scrVersesRev[1]);

			// Verify cluster 2: Current ScrVerse 2 added (empty paragraph)
			VerifyScrVerseCluster(clusterList[2],
				01001001, 01001001, ClusterType.AddedToCurrent, scrVersesCur[2], null, 2);

			// Verify cluster 3: Current ScrVerse 3 added (verse 1 continued)
			List<ScrVerse> expectedCurVerses = new List<ScrVerse>(1);
			expectedCurVerses.Add(scrVersesCur[3]);
			VerifyScrVerseCluster(clusterList[3],
				01001001, 01001001, ClusterType.OrphansInCurrent, expectedCurVerses, null, 2);

			// Verify cluster 4: Current ScrVerse 4 matches Revision ScrVerse 2
			VerifyScrVerseCluster(clusterList[4],
				01001002, 01001002, ClusterType.MatchedItems, scrVersesCur[4], scrVersesRev[2]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineScrVerseOverlapClusters"/> method for
		/// ScrVerses when the revision has stanza breaks before and after a verse segment.
		/// </summary>
		/// <remarks> Here is a diagram of which ScrVerses will match.
		///  Revision			Current
		///	 0 [Stanza Break]
		///	 1 [Stanza Break]
		///  2 "Gen1:1 continued"
		///	 3 [Stanza Break]
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScrVerseOverlap_AddedStanzaBeforeAndAfter()
		{
			CheckDisposed();

			// Create ScrVerses for the current version of Genesis
			List<ScrVerse> scrVersesRev = new List<ScrVerse>(5);
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));

			List<ScrVerse> scrVersesCur = new List<ScrVerse>();
			// No ScrVerses in the current side.

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineScrVerseOverlapClusters(
				scrVersesCur, scrVersesRev, m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(4, clusterList.Count);

			// Verify cluster 0: Revision ScrVerse 0 missing in Current
			VerifyScrVerseCluster(clusterList[0],
				01001001, 01001001, ClusterType.MissingInCurrent, null, scrVersesRev[0], 0);

			// Verify cluster 1: Revision ScrVerse 1 missing in Current
			VerifyScrVerseCluster(clusterList[1],
				01001001, 01001001, ClusterType.MissingInCurrent, null, scrVersesRev[1], 0);

			// Verify cluster 2: Revision ScrVerse 2 missing in Current
			List<ScrVerse> expectedItemsRev =
				new List<ScrVerse>(new ScrVerse[] { scrVersesRev[2] });
			VerifyScrVerseCluster(clusterList[2],
				01001001, 01001001, ClusterType.OrphansInRevision, null, expectedItemsRev, 0);

			// Verify cluster 3: Revision ScrVerse 3 missing in Current
			VerifyScrVerseCluster(clusterList[3],
				01001001, 01001001, ClusterType.MissingInCurrent, null, scrVersesRev[3], 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineScrVerseOverlapClusters"/> method for
		/// ScrVerses when the current ends with a stanza break.
		/// </summary>
		/// <remarks> Here is a diagram of which ScrVerses will match.
		///  Revision      Current
		///	 0 Gen 1:1	--  0 Gen 1:1
		///	 1 Gen 1:2
		///  2 Gen 1:3  --  1 Gen 1:3
		///                 [Stanza Break]
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScrVerseOverlap_AddedStanzaAfterEndScrVerse()
		{
			CheckDisposed();

			// Create ScrVerses for the revision version of Genesis
			List<ScrVerse> scrVersesRev = new List<ScrVerse>(3);
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));
			scrVersesRev.Add(new ScrVerse(01001002, 01001002, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));
			scrVersesRev.Add(new ScrVerse(01001003, 01001003, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));

			// Create ScrVerses for the current version of Genesis
			List<ScrVerse> scrVersesCur = new List<ScrVerse>(3);
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));
			scrVersesCur.Add(new ScrVerse(01001003, 01001003, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));
			scrVersesCur.Add(new ScrVerse(01001003, 01001003, null, null, 0, 0, true)); // stanza break

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineScrVerseOverlapClusters(
				scrVersesCur, scrVersesRev, m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(4, clusterList.Count);

			// Verify cluster 0: Current ScrVerse 0 matches Revision ScrVerse 0
			VerifyScrVerseCluster(clusterList[0],
				01001001, 01001001, ClusterType.MatchedItems, scrVersesCur[0], scrVersesRev[0]);

			// Verify cluster 1: Revision ScrVerse 1 missing
			VerifyScrVerseCluster(clusterList[1],
				01001002, 01001002, ClusterType.MissingInCurrent, null, scrVersesRev[1], 1);

			// Verify cluster 2: Current ScrVerse 1 matches Revision ScrVerse 2
			VerifyScrVerseCluster(clusterList[2],
				01001003, 01001003, ClusterType.MatchedItems, scrVersesCur[1], scrVersesRev[2]);

			// Verify cluster 3: Current ScrVerse 3 added
			VerifyScrVerseCluster(clusterList[3],
				01001003, 01001003, ClusterType.AddedToCurrent, scrVersesCur[2], null, 2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ClusterListHelper.DetermineScrVerseOverlapClusters"/> method for
		/// ScrVerses when the current ends with an added stanza break and is missing a verse.
		/// </summary>
		/// <remarks> Here is a diagram of which ScrVerses will match.
		///  Revision      Current
		///	 0 Gen 1:1	--  0 Gen 1:1
		///	 1 Gen 1:2		[Stanza Break]
		///	</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("REVIEW: The empty para is associated with verse 1 so the clustering (putting verse two in a different paragraph is not unreasonable).")]
		public void ScrVerseOverlap_AddedEmptyAtEndAndMissingScrVerse()
		{
			CheckDisposed();

			// Create ScrVerses for the revision version of Genesis
			List<ScrVerse> scrVersesRev = new List<ScrVerse>(3);
			scrVersesRev.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));
			scrVersesRev.Add(new ScrVerse(01001002, 01001002, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));

			// Create ScrVerses for the current version of Genesis
			List<ScrVerse> scrVersesCur = new List<ScrVerse>(3);
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, m_tssVerse, null, 0, 0, 0, 0, false, false, false, false));
			scrVersesCur.Add(new ScrVerse(01001001, 01001001, null, null, 0, 0, true));

			// Run the method under test
			List<Cluster> clusterList = ClusterListHelper.DetermineScrVerseOverlapClusters(
				scrVersesCur, scrVersesRev, m_inMemoryCache.Cache);

			// Verify the list size
			Assert.AreEqual(3, clusterList.Count);

			// Verify cluster 0: Current ScrVerse 0 matches Revision ScrVerse 0
			VerifyScrVerseCluster(clusterList[0],
				01001001, 01001001, ClusterType.MatchedItems, scrVersesCur[0], scrVersesRev[0]);

			// Verify cluster 1: Current ScrVerse 1 added
			VerifyScrVerseCluster(clusterList[1],
				01001001, 01001001, ClusterType.AddedToCurrent, scrVersesCur[1], null, 2);

			// Verify cluster 2: Revision ScrVerse 2 missing
			VerifyScrVerseCluster(clusterList[2],
				01001002, 01001002, ClusterType.MissingInCurrent, null, scrVersesRev[1], 1);
		}
		#endregion

		#endregion

		#region Helper Methods - Verify Cluster
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies expected characteristics of any kind of cluster.
		/// </summary>
		/// <param name="cluster">The cluster.</param>
		/// <param name="refMin">The reference min.</param>
		/// <param name="refMax">The reference max.</param>
		/// <param name="type">The cluster type.</param>
		/// <param name="expectedItemsCurr">The expected items in the Current.</param>
		/// <param name="expectedItemsRev">The expected items in the Revision.</param>
		/// <param name="indexToInsertAtInOther">The index to insert at in other.</param>
		/// <param name="kindOfCluster">The kind of cluster.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyCluster(Cluster cluster, int refMin, int refMax, ClusterType type,
			object expectedItemsCurr, object expectedItemsRev, int indexToInsertAtInOther,
			ClusterKind kindOfCluster)
		{
			// verify the basics
			Assert.AreEqual(refMin, cluster.verseRefMin);
			Assert.AreEqual(refMax, cluster.verseRefMax);
			Assert.AreEqual(type, cluster.clusterType);

			// verify the indexToInsertAtInOther
			Assert.AreEqual(indexToInsertAtInOther, cluster.indexToInsertAtInOther);

			// now verify the cluster's items
			switch (kindOfCluster)
			{
				case ClusterKind.ScrSection:
					VerifySectionClusterItems(expectedItemsCurr, cluster.itemsCurr, kindOfCluster);
					VerifySectionClusterItems(expectedItemsRev, cluster.itemsRev, kindOfCluster);
					break;
				case ClusterKind.ScrVerse:
					VerifyScrVerseClusterItems(expectedItemsCurr, cluster.itemsCurr, kindOfCluster);
					VerifyScrVerseClusterItems(expectedItemsRev, cluster.itemsRev, kindOfCluster);
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for ScrVerse cluster tests-
		/// Verifies the contents of the given Cluster.
		/// </summary>
		/// <param name="cluster">The given cluster.</param>
		/// <param name="refMin">The expected verse ref min.</param>
		/// <param name="refMax">The expected verse ref max.</param>
		/// <param name="type">The the expected cluster type.</param>
		/// <param name="expectedItemsCurr">The expected items for the Current
		/// (see VerifyClusterItems() for details).</param>
		/// <param name="expectedItemsRev">The expected items for the Revision
		/// (see VerifyClusterItems() for details)</param>
		/// <param name="indexToInsertAtInOther">The expected index</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyScrVerseCluster(Cluster cluster, int refMin, int refMax, ClusterType type,
			object expectedItemsCurr, object expectedItemsRev, int indexToInsertAtInOther)
		{
			//here we check the calling test code:
			// expected items should be consistent with the expected cluster type
			switch (type)
			{
				case ClusterType.MatchedItems:
					Assert.IsTrue(expectedItemsCurr is ScrVerse);
					Assert.IsTrue(expectedItemsRev is ScrVerse);
					break;
				case ClusterType.MissingInCurrent:
					Assert.IsNull(expectedItemsCurr);
					Assert.IsTrue(expectedItemsRev is ScrVerse);
					break;
				case ClusterType.OrphansInRevision:
					Assert.IsNull(expectedItemsCurr);
					Assert.IsTrue(expectedItemsRev is List<ScrVerse>);
					break;
				case ClusterType.AddedToCurrent:
					Assert.IsTrue(expectedItemsCurr is ScrVerse);
					Assert.IsNull(expectedItemsRev);
					break;
				case ClusterType.OrphansInCurrent:
					Assert.IsTrue(expectedItemsCurr is List<ScrVerse>);
					Assert.IsNull(expectedItemsRev);
					break;
				case ClusterType.MultipleInBoth:
					Assert.IsTrue(expectedItemsCurr is List<ScrVerse>);
					Assert.IsTrue(expectedItemsRev is List<ScrVerse>);
					break;
				case ClusterType.SplitInCurrent:
					Assert.IsTrue(expectedItemsCurr is List<ScrVerse>);
					Assert.IsTrue(expectedItemsRev is List<ScrVerse>);
					break;
				case ClusterType.MergedInCurrent:
					Assert.IsTrue(expectedItemsCurr is List<ScrVerse>);
					Assert.IsTrue(expectedItemsRev is List<ScrVerse>);
					break;
				default:
					Assert.Fail("invalid type expected");
					break;
			}

			VerifyCluster(cluster, refMin, refMax, type, expectedItemsCurr, expectedItemsRev,
				indexToInsertAtInOther, ClusterKind.ScrVerse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for ScrVerse cluster tests-
		/// Verifies the contents of the given Cluster. This overload lets the caller ignore
		/// the indexToInsertAtInOther, which is only needed for Missing/Added clusters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void VerifyScrVerseCluster(Cluster cluster, int refMin, int refMax, ClusterType type,
			object expectedItemsCurr, object expectedItemsRev)
		{
			Assert.IsTrue(cluster.clusterType != ClusterType.MissingInCurrent &&
				cluster.clusterType != ClusterType.AddedToCurrent,
				"Missing/Added clusters must be verified by passing in the indexToInsertAtInOther parameter.");

			// verify the details
			VerifyScrVerseCluster(cluster, refMin, refMax, type, expectedItemsCurr, expectedItemsRev, -1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for section cluster tests-
		/// Verifies the contents of the given Cluster.
		/// </summary>
		/// <param name="cluster">The given cluster.</param>
		/// <param name="refMin">The expected verse ref min.</param>
		/// <param name="refMax">The expected verse ref max.</param>
		/// <param name="type">The the expected cluster type.</param>
		/// <param name="expectedItemsCurr">The expected items for the Current
		/// (see VerifyClusterItems() for details).</param>
		/// <param name="expectedItemsRev">The expected items for the Revision
		/// (see VerifyClusterItems() for details)</param>
		/// <param name="indexToInsertAtInOther">The expected index</param>
		/// ------------------------------------------------------------------------------------
		private void VerifySectionCluster(Cluster cluster, int refMin, int refMax, ClusterType type,
			object expectedItemsCurr, object expectedItemsRev, int indexToInsertAtInOther)
		{
			//here we check the calling test code:
			// expected items should be consistent with the expected cluster type
			switch (type)
			{
				case ClusterType.MatchedItems:
					Assert.IsTrue(expectedItemsCurr is IScrSection || expectedItemsCurr is StTxtPara);
					Assert.IsTrue(expectedItemsRev is IScrSection || expectedItemsRev is StTxtPara);
					break;
				case ClusterType.MissingInCurrent:
					Assert.IsNull(expectedItemsCurr);
					Assert.IsTrue(expectedItemsRev is IScrSection || expectedItemsRev is StTxtPara);
					break;
				case ClusterType.AddedToCurrent:
					Assert.IsTrue(expectedItemsCurr is IScrSection || expectedItemsCurr is StTxtPara);
					Assert.IsNull(expectedItemsRev);
					break;
				case ClusterType.MultipleInBoth:
					Assert.IsTrue(expectedItemsCurr is List<IScrSection>);
					Assert.IsTrue(expectedItemsRev is List<IScrSection>);
					break;
				case ClusterType.SplitInCurrent:
					Assert.IsTrue(expectedItemsCurr is List<IScrSection>);
					Assert.IsTrue(expectedItemsRev is List<IScrSection>);
					break;
				case ClusterType.MergedInCurrent:
					Assert.IsTrue(expectedItemsCurr is List<IScrSection>);
					Assert.IsTrue(expectedItemsRev is List<IScrSection>);
					break;
				default:
					Assert.Fail("invalid type expected");
					break;
			}

			VerifyCluster(cluster, refMin, refMax, type, expectedItemsCurr, expectedItemsRev,
				indexToInsertAtInOther, ClusterKind.ScrSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for section cluster tests-
		/// Verifies the contents of the given Cluster. This overload lets the caller ignore
		/// the indexToInsertAtInOther, which is only needed for Missing/Added clusters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void VerifySectionCluster(Cluster cluster, int refMin, int refMax, ClusterType type,
			object expectedItemsCurr, object expectedItemsRev)
		{
			Assert.IsTrue(cluster.clusterType != ClusterType.MissingInCurrent &&
				cluster.clusterType != ClusterType.AddedToCurrent,
				"Missing/Added clusters must be verified by passing in the indexToInsertAtInOther parameter.");

			// verify the details
			VerifySectionCluster(cluster, refMin, refMax, type, expectedItemsCurr, expectedItemsRev, -1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for section cluster tests-
		/// Verifies the given cluster items. (The items could be from Current or Revsion.)
		/// </summary>
		/// <param name="expectedItems">a CmObject (IScrSection or StTxtPara), or a List of
		/// such items, which we expect the given cluster items to represent.
		/// A single object indicates we expect one item in clusterItems,
		/// a List indicates we expect multiple items,
		/// a null indicates that we expect zero items.</param>
		/// <param name="clusterItems">The List of OverlapInfo cluster items</param>
		/// <param name="kindOfCluster">The kind of cluster.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifySectionClusterItems(object expectedItems, List<OverlapInfo> clusterItems,
			ClusterKind kindOfCluster)
		{
			if (expectedItems == null)
			{
				Assert.AreEqual(0, clusterItems.Count);
			}
			else if (expectedItems is List<IScrSection>)
			{
				List<IScrSection> expectedList = (List<IScrSection>)expectedItems; //make local var with type info, to reduce code clutter
				Assert.AreEqual(expectedList.Count, clusterItems.Count);
				for (int i = 0; i < expectedList.Count; i++)
				{
					VerifyClusterItem((CmObject)expectedList[i], clusterItems[i]);
				}
			}
			else if (expectedItems is List<StTxtPara>)
			{
				List<StTxtPara> expectedList = (List<StTxtPara>)expectedItems; //make local var with type info, to reduce code clutter
				Assert.AreEqual(expectedList.Count, clusterItems.Count);
				for (int i = 0; i < expectedList.Count; i++)
				{
					VerifyClusterItem((CmObject)expectedList[i], clusterItems[i]);
				}
			}
			else
			{	// single object is expected
				Assert.AreEqual(1, clusterItems.Count);
				switch (kindOfCluster)
				{
					case ClusterKind.ScrSection:
						Assert.IsTrue(expectedItems is IScrSection || expectedItems is StTxtPara,
							"expected item should be of type IScrSection or StTxtPara");
						break;
					case ClusterKind.ScrVerse:
						Assert.IsTrue(expectedItems is ScrVerse,
							"expected item should be of type ScrVerse");
						break;
				}
				VerifyClusterItem((CmObject)expectedItems, clusterItems[0]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for ScrVerse cluster tests-
		/// Verifies the given cluster items. (The items could be from Current or Revsion.)
		/// </summary>
		/// <param name="expectedItems">a ScrVerse, or a List of ScrVerses, which we expect the
		/// given cluster items to represent.
		/// A single object indicates we expect one item in clusterItems,
		/// a List indicates we expect multiple items,
		/// a null indicates that we expect zero items.</param>
		/// <param name="clusterItems">The List of OverlapInfo cluster items</param>
		/// <param name="kindOfCluster">The kind of cluster.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyScrVerseClusterItems(object expectedItems, List<OverlapInfo> clusterItems,
			ClusterKind kindOfCluster)
		{
			if (expectedItems == null)
			{
				Assert.AreEqual(0, clusterItems.Count);
			}
			else if (expectedItems is List<ScrVerse>)
			{
				List<ScrVerse> expectedList = (List<ScrVerse>)expectedItems; //make local var with type info, to reduce code clutter
				Assert.AreEqual(expectedList.Count, clusterItems.Count);
				for (int i = 0; i < expectedList.Count; i++)
				{
					VerifyClusterItem(expectedList[i], clusterItems[i]);
				}
			}
			else
			{	// single object is expected
				Assert.AreEqual(1, clusterItems.Count);
				Assert.IsTrue(expectedItems is ScrVerse, "expected item should be of type ScrVerse");
				VerifyClusterItem(expectedItems, clusterItems[0]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A helper method for section cluster tests-
		/// Verifies a given OverlapInfo item in a Cluster.
		/// </summary>
		/// <param name="objExpected">a CmObject (IScrSection or StTxtPara) or a ScrVerse which
		/// we expect the given OverlapInfo to represent</param>
		/// <param name="oiActual">the given OverlapInfo cluster item to be verified</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyClusterItem(object objExpected, OverlapInfo oiActual)
		{
			// We don't need to check everything in the OverlapInfo,
			//  just the things that are essential to processing the Cluster that owns it

			if (objExpected is CmObject)
			{
				CmObject cmObjExpected = (CmObject)objExpected;

				// check the index
				Assert.AreEqual(cmObjExpected.IndexInOwner, oiActual.indexInOwner);
				// check hvo too
				Assert.AreEqual(cmObjExpected.Hvo, oiActual.myHvo);

				// for good measure, if a section, check section refs too
				if (cmObjExpected is IScrSection)
				{
					Assert.AreEqual(((IScrSection)cmObjExpected).VerseRefMin, oiActual.verseRefMin);
					Assert.AreEqual(((IScrSection)cmObjExpected).VerseRefMax, oiActual.verseRefMax);
				}
			}
			else if (objExpected is ScrVerse)
			{
				Assert.AreEqual(((ScrVerse)objExpected).StartRef, oiActual.verseRefMin);
				Assert.AreEqual(((ScrVerse)objExpected).EndRef, oiActual.verseRefMin);
			}
			else
				Assert.Fail("Unhandled expected type.");
		}
		#endregion
	}
}
