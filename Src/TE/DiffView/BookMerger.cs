// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2004' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BookMerger.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Diagnostics;
using SIL.FieldWorks.IText;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.Framework;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	#region ParaCorrelationInfo
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// A data type that can instantiated for a pair of paragraphs that correlate.
	/// It holds information about correlated items and their degree of correlation.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class ParaCorrelationInfo
	{
		/// <summary>
		/// Index of the "current" paragraph in its section
		/// </summary>
		public int iParaCurr;

		/// <summary>
		/// Index of the "revision" paragraph in its section
		/// </summary>
		public int iParaRev;

		/// <summary>
		/// Calculated correlation factor between the paragraphs
		/// </summary>
		public double correlationFactor;

		/// <summary>
		/// The count of other correlations that this correlation crosses over.
		/// </summary>
		public int crossoverCount;

		/// <summary>
		/// constructor
		/// </summary>
		public ParaCorrelationInfo(int paraCurr, int paraRev, double factor)
		{
			iParaCurr = paraCurr;
			iParaRev = paraRev;
			correlationFactor = factor;
		}
	}

	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to compare ParaCorrelationInfo objects. They will be sorted in paragraph order.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	public class ParaCorrelationInfoSorter : IComparer<ParaCorrelationInfo>
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Comparison method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Compare(ParaCorrelationInfo obj1, ParaCorrelationInfo obj2)
		{
			return (obj1.iParaCurr - obj2.iParaCurr);
		}
	}
	#endregion

	#region ParaInfo - not used
	///// ------------------------------------------------------------------------------------
	///// <summary>
	///// Contains information about paragraphs in an StText.
	///// </summary>
	///// ------------------------------------------------------------------------------------
	//internal class ParaInfo
	//{
	//    private ScrReference m_startRef;
	//    private ScrReference m_endRef;


	//    /// --------------------------------------------------------------------------------
	//    /// <summary>
	//    /// Initializes a new instance of the <see cref="ParaInfo"/> class.
	//    /// </summary>
	//    /// --------------------------------------------------------------------------------
	//    ParaInfo()
	//    {
	//    }

	//    /// --------------------------------------------------------------------------------
	//    /// <summary>
	//    /// Gets or sets the starting Scripture reference in a paragraph.
	//    /// </summary>
	//    /// --------------------------------------------------------------------------------
	//    public ScrReference StartRef
	//    {
	//        get { return m_startRef; }
	//        set { m_startRef = value; }
	//    }

	//    /// --------------------------------------------------------------------------------
	//    /// <summary>
	//    /// Gets or sets the ending Scripture reference in a paragraph.
	//    /// </summary>
	//    /// --------------------------------------------------------------------------------
	//    public ScrReference EndRef
	//    {
	//        get { return m_endRef; }
	//        set { m_endRef = value; }
	//    }
	//}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for BookMerger.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class BookMerger : IObjectMetaInfoProvider, IFWDisposable
	{
		#region member variables
		private FdoCache m_cache;
		private Scripture m_scr;
		private IVwStylesheet m_stylesheet;
		private IAdvInd4 m_progressDlg;

		/// <summary>The current list of differences we will detect and then show to the user
		/// NOTE: The reference for this list can NOT be changed (enforced by readonly attribute)
		/// for another difference list because the DiffViewVc and the DiffDialog keep a reference
		/// to it internally.
		/// </summary>
		internal readonly DifferenceList m_differences = new DifferenceList();

		/// <summary>the master list of all differences.</summary>
		internal DifferenceList m_allDifferences = null;
		/// <summary>list of differences with added sections filtered to avoid data loss.</summary>
		private DifferenceList m_filteredDifferences = null;
		/// <summary>if true, don't include SectionAddedToCurrent differences.</summary>
		private bool m_fUsingFilteredDiffs = false;
		/// <summary>Flag indicating whether DetectDifferences should also attempt to merge if there
		/// are no conflicts</summary>
		protected bool m_fAttemptAutoMerge = false;
		/// <summary>Agent needed to support AutoMerge feature so that the version of a book before
		/// the automerge happens can be archived.</summary>
		private IBookVersionAgent m_bookVersionAgent = null;

		/// <summary>Flag indicating whether DetectDifferences was successful in merging the
		/// saved or imported version into the current version</summary>
		protected bool m_fAutoMergeSucceeded = false;
		private bool m_fCannotMergeBecauseOfTitleDifference = false;

		/// <summary>lookup table for all differences (will not have items removed).</summary>
		protected Dictionary<int, List<Difference>> m_diffHashTable;

		/// <summary>The number of differences found on the first call to DetectDifferences.</summary>
		private int m_origDiffCount;

		// the list of the differences that the user has marked as "Reviewed"
		private DifferenceList m_reviewedDiffs = new DifferenceList();

		// Member variables for DetectDifferences
		private IScrBook m_bookRev;
		private IScrBook m_bookCurr;

		/// <summary>the temporary list of differences we will detect for the current cluster</summary>
		private List<Difference> m_clusterDiffs;

		/// <summary>iterator for ScrVerses in the Current</summary>
		protected IVerseIterator m_currIterator;
		/// <summary>iterator for ScrVerses in the Revision</summary>
		protected IVerseIterator m_revIterator;
		// lists of ScrVerses for the (section overlap) cluster being processed
		private List<ScrVerse> m_scrVersesCurr;
		private List<ScrVerse> m_scrVersesRev;
		/// <summary>list of section overlap clusters for this book</summary>
		private List<Cluster> m_sectionOverlapClusterList;
		/// <summary>list of ScrVerses clusters found within the section overlap cluster being processed</summary>
		private List<Cluster> m_scrVerseClusters;

		// list of ScrVerse comparisons, only used for multiple-overlap clusters.
		private List<Comparison> m_verseComparisons;
		// list of section head correlations, only used for multiple-overlap clusters.
		private ReadOnlyCollection<Cluster> m_sectionHeadCorrelationList;

		/// <summary>difference between current and revision</summary>
		private Difference m_diff;

		/// <summary>ScrVerse in current book</summary>
		private ScrVerse m_verseCurr;
		/// <summary>ScrVerse in revision</summary>
		private ScrVerse m_verseRev;
		/// <summary>the position of the end of the previous ScrVerse, in current</summary>
		protected ScrVerse m_endOfPrevVerseCurr;
		/// <summary>the position of the end of the previous ScrVerse, in revision</summary>
		protected ScrVerse m_endOfPrevVerseRev;

		/// <summary> the id of the Current paragraph that we have most recently checked for a
		/// para style difference </summary>
		private int m_hvoPrevParaCurr;
		/// <summary> the id of the Revision paragraph that we have most recently checked for a
		/// para style difference </summary>
		private int m_hvoPrevParaRev;

		/// <summary>List of paragraphs whose segmented back translations have been changed
		/// and whose CmTranslations are still awaiting update.</summary>
		private List<StTxtPara> m_parasNeedingUpdateMainTrans;
		#endregion

		#region IncrementIndicies enum
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// type to return from methods that process ScrVerses to indicate which indices
		/// should be incremented.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected enum IncrementIndices
		{
			/// <summary>Do not increment either index</summary>
			None,
			/// <summary>Increment the current index</summary>
			Current,
			/// <summary>Increment the revision index</summary>
			Revision,
			/// <summary>Increment both indices</summary>
			Both
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="BookMerger"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <param name="bookRev">The book revision that is to be merged (with the current book
		/// having the same canonical number).</param>
		/// ------------------------------------------------------------------------------------
		public BookMerger(FdoCache cache, IVwStylesheet stylesheet, IScrBook bookRev)
		{
			m_cache = cache;
			m_stylesheet = stylesheet;
			m_bookRev = bookRev;

			// find the matching book in scripture
			m_scr = (Scripture)m_bookRev.Cache.LangProject.TranslatedScriptureOA;
			m_bookCurr = m_scr.FindBook(m_bookRev.CanonicalNum);
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~BookMerger()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing">if set to <c>true</c> [disposing].</param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				m_differences.Clear();
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_bookCurr = null;
			m_bookRev = null;
			m_currIterator = null;
			m_revIterator = null;
			m_diff = null;
			m_verseCurr = null;
			m_verseRev = null;
			m_endOfPrevVerseCurr = null;
			m_endOfPrevVerseRev = null;
			m_cache = null;
			m_stylesheet = null;
			m_progressDlg = null;
			m_allDifferences = null;
			m_filteredDifferences = null;
			m_diffHashTable = null;
			m_reviewedDiffs = null;
			ClusterDiffs = null;
			m_scrVersesCurr = null;
			m_scrVersesRev = null;
			m_verseComparisons = null;
			m_sectionHeadCorrelationList = null;

			m_isDisposed = true;
		}
		#endregion IDisposable & Co. implementation

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the cluster diffs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal List<Difference> ClusterDiffs
		{
			get { return m_clusterDiffs; }
			set { m_clusterDiffs = value; }
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the saved version of the book, shown in the left pane of the dialog
		/// </summary>
		/// <remarks>In the future if we support comparing two saved versions, this will be the
		/// older of the two. (Might want to rename this property then)</remarks>
		/// ------------------------------------------------------------------------------------
		public IScrBook BookRev
		{
			get { return m_bookRev; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current version of the book, shown in the right pane of the dialog.
		/// </summary>
		/// <remarks>In the future if we support comparing two saved versions, this will be the
		/// newer of the two. (Might want to rename this property then)</remarks>
		/// ------------------------------------------------------------------------------------
		public IScrBook BookCurr
		{
			get { return m_bookCurr; }
		}

		/// <summary>
		/// Gets the cache associated with the BookMerger.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get { return m_cache; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style sheet associated with the BookMerger.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwStylesheet StyleSheet
		{
			get { return m_stylesheet; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the list of differences that have been marked as "reviewed"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal DifferenceList ReviewedDiffs
		{
			get
			{
				CheckDisposed();
				return m_reviewedDiffs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the list of differences detected
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal DifferenceList Differences
		{
			get { return m_differences; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a lookup table of all (unfiltered) differences for searching.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected Dictionary<int, List<Difference>> DiffHashTable
		{
			get { return m_diffHashTable; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of differences.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int NumberOfDifferences
		{
			get { return m_differences.Count; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the original number of differences.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int OriginalNumberOfDifferences
		{
			get { return m_origDiffCount; }
		}
		#endregion

		#region Detect Differences
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compile a list of differences between the current version and the revision
		/// for the book.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">Not used.</param>
		/// <returns>Always <c>null</c>.</returns>
		/// <remarks>This version is called from the ProgressDialogWithTask class and runs
		/// on a background thread.</remarks>
		/// ------------------------------------------------------------------------------------
		public object DetectDifferences(IAdvInd4 progressDlg, object[] parameters)
		{
			DetectDifferences(progressDlg);
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compile a list of differences between the current version and the revision
		/// for the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DetectDifferences(IAdvInd4 progressDlg)
		{
			CheckDisposed();

			m_progressDlg = progressDlg;
			if (m_progressDlg != null)
			{
				m_progressDlg.Title = DlgResources.ResourceString("kstidCompareCaption");
				m_progressDlg.SetRange(0, m_bookRev.SectionsOS.Count);
				m_progressDlg.Position = 0;
			}

			DetectDifferencesInBook();

			// Sort the differences once they've all been found
			m_differences.Sort();
			// only save the difference count on the first time.
			if (m_origDiffCount == 0)
				m_origDiffCount = m_differences.Count;
			m_allDifferences = m_differences;

			// Build lookup table of all differences by reference. No differences will be
			// removed from this table because sometimes we need to know all the other differences
			// at a particular Scripture reference to determine whether a revert would cause data loss.
			m_diffHashTable = BuildDiffLookupTable();

			if (m_fUsingFilteredDiffs)
				RemoveSectionAddedDiffs();

			if (m_fAttemptAutoMerge)
				AutoMerge();

			m_progressDlg = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine all the differences between the book and book revision,
		/// and append the differences found to the master list of diffs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DetectDifferencesInBook()
		{
			int nSectionsCurr = m_bookCurr.SectionsOS.Count;
			int nSectionsRev = m_bookRev.SectionsOS.Count;
			Debug.Assert(nSectionsCurr > 0);
			Debug.Assert(nSectionsRev > 0);
			m_scrVerseClusters = null; //clean up from prior

			// Check the title first
			DetectDifferencesInTitle(m_bookCurr, m_bookRev);

			// init endOfPrevVerse for start of first heading in each book
			InitPrevVerseForBook(m_bookCurr, out m_endOfPrevVerseCurr);
			InitPrevVerseForBook(m_bookRev, out m_endOfPrevVerseRev);

			// Determine which clusters of sections have overlapping verse ref ranges.
			m_sectionOverlapClusterList =
				ClusterListHelper.DetermineSectionOverlapClusters(m_bookCurr, m_bookRev, m_cache);

			// Look through the overlap section cluster list, and detect differences
			for (int iCluster = 0; iCluster < m_sectionOverlapClusterList.Count; )
			{
				if (m_progressDlg != null)
					m_progressDlg.Step(0);

				// init for this cluster
				ClusterDiffs = new List<Difference>();
				Cluster cluster = m_sectionOverlapClusterList[iCluster];
				m_scrVerseClusters = null; // clear previously used

				// Intro sections
				if (cluster.verseRefMin.Chapter == 1 && cluster.verseRefMin.Verse == 0)
				{
					// all intro sections (having ref of chapter 1 verse 0) are
					//  normally in one cluster. Process them now.
					ProcessIntroSections(cluster);
					iCluster++;
				}

				// A pair of sections are a ref match (overlap)!
				else if (cluster.clusterType == ClusterType.MatchedItems)
				{
					ProcessMatchingSections(cluster);
					iCluster++;
				}

				// Added section in one book
				else if (cluster.clusterType == ClusterType.MissingInCurrent ||
						 cluster.clusterType == ClusterType.AddedToCurrent)
				{
					// this one could process multiple adjacent clusters
					ProcessMissingAddedSections(m_sectionOverlapClusterList, ref iCluster);
				}

				// Multiple sections in one or both books have ref overlap
				else if (cluster.clusterType == ClusterType.MultipleInBoth ||
						 cluster.clusterType == ClusterType.SplitInCurrent ||
						 cluster.clusterType == ClusterType.MergedInCurrent)
				{
					ProcessMultipleOverlappingSections(cluster);
					iCluster++;
				}
				else
					Debug.Assert(false, "Unexpected cluster type");

				// Append the list of cluster differences to the master list.
				m_differences.AddRange(ClusterDiffs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Detect differences in a pair matching sections.
		/// </summary>
		/// <param name="cluster">the given cluster- of a matched pair of sections</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessMatchingSections(Cluster cluster)
		{
			// Process the matched sections. They are always one-to-one.
			int iSectionCurr = ((OverlapInfo)cluster.itemsCurr[0]).indexInOwner;
			int iSectionRev = ((OverlapInfo)cluster.itemsRev[0]).indexInOwner;
			IScrSection sectionCurr = m_bookCurr.SectionsOS[iSectionCurr];
			IScrSection sectionRev = m_bookRev.SectionsOS[iSectionRev];
			// compare the section headings
			DetectDifferencesInStTexts_ByParagraph(sectionCurr.HeadingOA,
				sectionRev.HeadingOA, sectionCurr.VerseRefStart);
			// compare the section contents verse-by-verse
			DetectDifferencesInStText(sectionCurr.ContentOA, sectionRev.ContentOA);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a list of section Overlap Clusters, generate a difference for contiguous
		/// Added or Missing sections, beginning at the given index.
		/// (Each given Added/Missing overlap cluster identifies one section.)
		/// </summary>
		/// <param name="sectionClusterList">The given list of section overlap clusters.</param>
		/// <param name="iCluster">ref: the given cluster index to start at; updated to the
		/// cluster index beyond those we process</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessMissingAddedSections(List<Cluster> sectionClusterList, ref int iCluster)
		{
			// Find the range of clusters we will process in this one diff
			int iClusterLim = FindRangeOfSimilarClusters(iCluster, sectionClusterList);

			Cluster startCluster = sectionClusterList[iCluster];

			// Form a single processing cluster for the adjacent missing/added sections
			//  i.e. merge the range of clusters into one cluster
			Cluster processingCluster = startCluster.Clone();
			for (int i = iCluster + 1; i < iClusterLim; i++)
			{
				Cluster nextOverlapCluster = sectionClusterList[i];
				processingCluster.MergeSourceItems(nextOverlapCluster);
			}

			// Now create the difference using the info in our processing cluster
			ProcessMissingAddedSections(processingCluster);

			// update the cluster index for our caller
			iCluster = iClusterLim;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the range of clusters that are similar to the given cluster at startIndex,
		/// so they can be processed as a group.
		/// </summary>
		/// <param name="startIndex">starting index in the list to look at</param>
		/// <param name="list">list of Clusters to look in</param>
		/// <returns>the lim index just beyond the last similar cluster</returns>
		/// ------------------------------------------------------------------------------------
		private int FindRangeOfSimilarClusters(int startIndex, List<Cluster> list)
		{
			Cluster startCluster = list[startIndex];

			for (int i = startIndex + 1; i < list.Count; i++)
			{
				// if this cluster is not similar, we are done
				Cluster cluster = list[i];
				if (!cluster.IsSimilar(startCluster))
					return i;
			}

			// if we get here, all remaining clusters are similar
			return list.Count;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Produce a single difference for the set of Added or Missing sections in the given
		/// processing cluster.
		/// </summary>
		/// <param name="cluster">the given cluster- of a set of added or missing sections</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessMissingAddedSections(Cluster cluster)
		{
			// determine the owning sequences that cluster refers to
			FdoOwningSequence<IScrSection> owningSeqDest;
			if (cluster.clusterType == ClusterType.AddedToCurrent)
				owningSeqDest = m_bookRev.SectionsOS;
			else
			{
				Debug.Assert(cluster.clusterType == ClusterType.MissingInCurrent);
				owningSeqDest = m_bookCurr.SectionsOS; //the added items could be inserted in the Rev
			}

			// Get the paragraph hvo and ich in the Destination to associate with
			StTxtPara paraDest = null;
			int ichDest = 0;
			int index = cluster.indexToInsertAtInOther;
			if (index < owningSeqDest.Count)
			{
				// place the association point at the start of the matching section heading
				IScrSection sectionDest = owningSeqDest[index];
				paraDest = (StTxtPara)sectionDest.HeadingOA.ParagraphsOS[0];
				ichDest = 0;
			}
			else
			{
				// place the association point at the end of the last section of the book
				if (owningSeqDest.Count > 0) //paranoid?
				{
					IScrSection sectionDest = owningSeqDest[owningSeqDest.Count - 1];
					int iLastPara = sectionDest.ContentOA.ParagraphsOS.Count - 1;
					paraDest = (StTxtPara)sectionDest.ContentOA.ParagraphsOS[iLastPara];
					ichDest = paraDest.Contents.Length;
				}
			}

			// translate cluster type to diff type
			DifferenceType diffType;
			if (cluster.clusterType == ClusterType.AddedToCurrent)
				diffType = DifferenceType.SectionAddedToCurrent;
			else if (cluster.clusterType == ClusterType.MissingInCurrent)
				diffType = DifferenceType.SectionMissingInCurrent;
			else
				throw new Exception("BookMerger.ProcessMissingAddedSections - clusterType must be Missing or Added");

			// Create a difference for the first added/missing section
			Difference diff = new Difference(cluster.verseRefMin, cluster.verseRefMax, diffType,
				cluster.HvosItemsSource, paraDest, ichDest, m_cache);
			ClusterDiffs.Add(diff);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Detect differences in the given set of introduction sections.
		/// The given cluster may well have zero, one, or many intro sections in both
		/// the Current and the Rev.
		/// </summary>
		/// <param name="cluster">the given cluster- of a set of intro sections</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessIntroSections(Cluster cluster)
		{
			// Extract the two OverlapInfo lists from the cluster
			List<OverlapInfo> itemsCurr = cluster.itemsCurr;
			List<OverlapInfo> itemsRev = cluster.itemsRev;

			// TE-2900: determine the correlations of the introductory sections in each book revision.
			//Then detect differences appropriately within the correlated intro sections.
			//This will enable more meaningful comparisions of the intro sections, e.g. when an
			//intro section has been added or moved in one revision.
			//Intro sections will correlated by finding the best match using the paragraph correlation
			//algorithm.

			// but for now, just match up intro sections one-to-one and mark the remaining ones as "added"
			for (int i = 0; i < Math.Min(itemsCurr.Count, itemsRev.Count); i++)
			{
				// Get the current pair of intro sections
				OverlapInfo oi = itemsCurr[i];
				IScrSection sectionCurr = m_bookCurr.SectionsOS[oi.indexInOwner];
				oi = itemsRev[i];
				IScrSection sectionRev = m_bookRev.SectionsOS[oi.indexInOwner];
				Debug.Assert(sectionCurr.IsIntro && sectionRev.IsIntro);

				// compare the section headings as text paragraphs
				DetectDifferencesInStTexts_ByParagraph(sectionCurr.HeadingOA,
					sectionRev.HeadingOA, sectionCurr.VerseRefStart);
				// compare the section contents as text paragraphs
				DetectDifferencesInStTexts_ByParagraph(sectionCurr.ContentOA, sectionRev.ContentOA,
					sectionCurr.VerseRefStart);
			}

			// Continue on with whichever list (rev or curr) is longer
			if (itemsCurr.Count > itemsRev.Count)
			{
				int iNext = itemsRev.Count; // the lim of the shorter list, and the next item in the longer list
				Cluster processingCluster =
					new Cluster(ClusterType.AddedToCurrent, itemsCurr[iNext], iNext);
				for (int i = iNext + 1; i < itemsCurr.Count; i++)
				{
					processingCluster.AddSourceItem(itemsCurr[i]);
				}
				ProcessMissingAddedSections(processingCluster);
			}
			else if (itemsRev.Count > itemsCurr.Count)
			{
				int iNext = itemsCurr.Count; // the lim of the shorter list, and the next item in the longer list
				Cluster processingCluster =
					new Cluster(ClusterType.MissingInCurrent, itemsRev[iNext], iNext);
				for (int i = iNext + 1; i < itemsRev.Count; i++)
				{
					processingCluster.AddSourceItem(itemsRev[i]);
				}
				ProcessMissingAddedSections(processingCluster);
			}
		}
		#endregion

		#region DetectDifferences: ProcessMultipleOverlappingSections
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Detect differences in the set of overlapping sections (a cluster with multiple overlaps
		/// in either Revision and Current)
		/// </summary>
		/// <param name="overlapCluster">the given cluster- of a set of overlapping sections</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessMultipleOverlappingSections(Cluster overlapCluster)
		{
			// Prepare to compare the combined sections' contents of the overlap cluster.
			//  we'll make two lists of StTexts
			List<IStText> listCurrContent = new List<IStText>();
			List<IStText> listRevContent = new List<IStText>();
			// collect content of Current sections
			foreach (OverlapInfo oi in overlapCluster.itemsCurr)
			{
				Debug.Assert(oi.myBook == OverlapInfo.kCurrent);
				int i = oi.indexInOwner;
				listCurrContent.Add(m_bookCurr.SectionsOS[i].ContentOA);
			}
			// collect content of Revision sections
			foreach (OverlapInfo oi in overlapCluster.itemsRev)
			{
				Debug.Assert(oi.myBook == OverlapInfo.kRevision);
				int i = oi.indexInOwner;
				listRevContent.Add(m_bookRev.SectionsOS[i].ContentOA);
			}

			// Now detect differences in the Contents of the sections
			Debug.Assert(listCurrContent.Count != 0 && listRevContent.Count != 0);
			m_verseComparisons = new List<Comparison>(); // keep a list of comparisons for this cluster
			DetectDifferencesInListOfStTexts(listCurrContent, listRevContent);

			// Determine which sections have correlated headings
			// We'll get that info in a special list of clusters
			// TODO: review ReadOnly for following List
			List<Cluster> readable = new List<Cluster>(
				SectionHeadCorrelationHelper.DetermineSectionHeadCorrelationClusters(overlapCluster));
			m_sectionHeadCorrelationList = readable.AsReadOnly();

			// Look through the section head correlation list, and detect differences in the
			//  section heads
			for (int i = 0; i < m_sectionHeadCorrelationList.Count; i++)
			{
				Cluster correlation = m_sectionHeadCorrelationList[i];

				// A pair of sections are correlated
				// (i.e. they should be compared, not marked as one added, the other missing,
				// even though they are at slightly different verse references)
				if (correlation.clusterType == ClusterType.MatchedItems)
				{
					ProcessCorrelatedSectionHeads(correlation);
					continue;
				}

				// Added section head is first in cluster, with verses moved
				if (( //correlation.clusterType == ClusterType.MissingInCurrent ||
					correlation.clusterType == ClusterType.AddedToCurrent) &&
					i == 0)
				{
					ProcessMissingAddedFirstSectionHead(correlation, overlapCluster);
					continue;
				}

				// Added section head in one book
				if (correlation.clusterType == ClusterType.MissingInCurrent ||
					correlation.clusterType == ClusterType.AddedToCurrent)
				{
					ProcessMissingAddedSectionHead(correlation, overlapCluster);
					continue;
				}
				Debug.Assert(false, "Unexpected cluster type for section head correlation");
			}

			// clean up
			m_verseComparisons.Clear();
			m_verseComparisons = null;
			m_sectionHeadCorrelationList = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the correlated section heads.
		/// </summary>
		/// <param name="correlation">The correlation.</param>
		/// ------------------------------------------------------------------------------------
		public void ProcessCorrelatedSectionHeads(Cluster correlation)
		{
			CheckDisposed();

			// Process the correlated section heads. They are always one-to-one.
			ScrSection sectionCurr =
				new ScrSection(m_cache, ((OverlapInfo)correlation.itemsCurr[0]).myHvo);
			ScrSection sectionRev =
				new ScrSection(m_cache, ((OverlapInfo)correlation.itemsRev[0]).myHvo);

			// compare the text of the section headings
			DetectDifferencesInStTexts_ByParagraph(sectionCurr.HeadingOA,
				sectionRev.HeadingOA, correlation.verseRefMin);

// TODO: TE-7328, 4704 Determine Verse(s)Moved diffs here. Need to analyze the ScrVerses.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes a missing/added section head.
		/// </summary>
		/// <param name="correlation">The correlation cluster identifying the added section
		/// head.</param>
		/// <param name="overlap">The overlap cluster from which correlation clusters were
		/// derived.</param>
		/// ------------------------------------------------------------------------------------
		public void ProcessMissingAddedSectionHead(Cluster correlation, Cluster overlap)
		{
			CheckDisposed();

			// translate cluster type to diff type
			DifferenceType diffType;
			if (correlation.clusterType == ClusterType.AddedToCurrent)
				diffType = DifferenceType.SectionHeadAddedToCurrent;
			else if (correlation.clusterType == ClusterType.MissingInCurrent)
				diffType = DifferenceType.SectionHeadMissingInCurrent;
			else
				throw new Exception("BookMerger.ProcessMissingAddedSectionHead - clusterType must be Missing or Added");

			StTxtPara paraDest;
			int ichDest;
			GetDestinationIP(correlation, overlap, diffType, out paraDest, out ichDest);

			if (paraDest != null)
			{
				// Create a difference for the added/missing section head
				Difference diff = new Difference(correlation.verseRefMin, correlation.verseRefMin, diffType,
												 correlation.HvosItemsSource, paraDest, ichDest, m_cache);
				ClusterDiffs.Add(diff);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the destination IP for the given added section head by using an adjacent ScrVerse
		/// and seeing where it correlates on the other side.
		/// </summary>
		/// <param name="correlation">The correlation cluster for the added section head.</param>
		/// <param name="overlap">The section overlap cluster.</param>
		/// <param name="diffType">Type of the difference (SectionHeadAdded or SectionHeadMissing).</param>
		/// <param name="paraDest">out: The destination paragraph</param>
		/// <param name="ichDest">out: The destination char index.</param>
		/// ------------------------------------------------------------------------------------
		private void GetDestinationIP(Cluster correlation, Cluster overlap, DifferenceType diffType,
			out StTxtPara paraDest, out int ichDest)
		{
			// get the section index of the added section head;
			//  (for VerseMoved diff, the verses have been moved into this section)
			int iSectionAdded = correlation.SourceItems[0].indexInOwner;
			bool fCurrentIsSource = Difference.CurrentIsSource(diffType);
			List<ScrVerse> srcScrVerses = SourceVerses(diffType);

			// If we find the first ScrVerse for this added section head...
			int iScrVerseFirst = GetFirstScrVerseInSection(srcScrVerses, iSectionAdded);
			if (iScrVerseFirst > -1)
			{
				ScrVerse scrVerseFirst = srcScrVerses[iScrVerseFirst];
				// Get the destination IP based on the scrVerseCluster for this first ScrVerse
				Cluster scrVerseCluster = GetScrVerseCluster(iScrVerseFirst, fCurrentIsSource);
				GetDestinationIpFromCluster(scrVerseCluster, fCurrentIsSource, true,
					out paraDest, out ichDest);
				if (paraDest != null)
					return;
			}

			// We didn't find the first ScrVerse for the added section head because added section is empty,
			//  or the first ScrVerse didn't help us find a destination ScrVerse.
			// Next, try the ScrVerse just prior to the Section Head.
			int iScrVersePrior = GetScrVerseIndexJustBeforeSectionHead(srcScrVerses, iSectionAdded);
			if (iScrVersePrior != -1)
			{
				// Get the destination IP based on the scrVerseCluster for this prior ScrVerse
				Cluster scrVerseCluster = GetScrVerseCluster(iScrVersePrior, fCurrentIsSource);
				GetDestinationIpFromCluster(scrVerseCluster, fCurrentIsSource, false,
					out paraDest, out ichDest);
				if (paraDest != null)
					return;
			}

			// Handle case where there are no ScrVerses adjacent to the Added SectionHead, or
			// those ScrVerses didn't help us find a destination ScrVerse.
			// We'll have to determine the destination by looking at the just-prior section head.
			int iCorr = m_sectionHeadCorrelationList.IndexOf(correlation);

			if (iCorr > 0)
			{
				// Get the section head correlation just prior to this SectionHeadAdded.
				Cluster correlationPrior = m_sectionHeadCorrelationList[iCorr - 1];
				if (correlationPrior.clusterType == ClusterType.MatchedItems)
				{
					ScrSection correlatedSection = GetCorrelatedSection(
						correlationPrior, correlation.clusterType);
					int iLastPara = correlatedSection.ContentOA.ParagraphsOS.Count - 1;
					paraDest = (StTxtPara)correlatedSection.ContentOA.ParagraphsOS[iLastPara];
					ichDest = paraDest.Contents.Length;
					return;
				}
				else
				{
					// The previously correlated item does not have a match. However, it may be
					// because we are in a sequence of missing sections.
					// Search the diffs in this cluster. If we find the prior correlated item,
					// then we want to create a new added/missing section difference.
					Difference addedSectionDiff = FindMissingSectionClusterDiffs(correlationPrior,
						fCurrentIsSource);

					paraDest = null;
					ichDest = 0;
					Debug.Assert(addedSectionDiff != null, "Didn't find added/missing section diff for this cluster.");
					if (addedSectionDiff != null)
					{
						paraDest = new StTxtPara(m_cache, addedSectionDiff.GetHvo(fCurrentIsSource));
						ichDest = addedSectionDiff.GetIchLim(fCurrentIsSource);
					}
				}
			}
			else
			{
				//This is the first item in the correlation list.
				Debug.Assert(iCorr == 0);
				// The destination will be the very start of the the other side.
				ScrSection sectionDest = GetFirstDestSection(overlap, fCurrentIsSource);
				//int iLastPara = sectionDest.ContentOA.ParagraphsOS.Count - 1;
				//paraDest = (StTxtPara)sectionDest.ContentOA.ParagraphsOS[iLastPara];
				//ichDest = paraDest.Contents.Length;
				paraDest = (StTxtPara)sectionDest.ContentOA.ParagraphsOS[0];
				ichDest = 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Search the diffs in this cluster. If we find a section head added/missing diff
		/// referencing the prior correlated item, then we return this diff.
		/// </summary>
		/// <param name="correlationPrior">The correlation of the prior item.</param>
		/// <param name="fCurrentIsSource">if set to <c>true</c> the current is the source;
		/// if <c>false</c> the revision is the source.</param>
		/// <returns>
		/// 	the diff in the cluster that referenced the prior correlated item;
		///		otherwise <c>null</c>
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private Difference FindMissingSectionClusterDiffs(Cluster correlationPrior, bool fCurrentIsSource)
		{
			if (ClusterDiffs == null)
				return null;

			foreach (Difference diff in ClusterDiffs)
			{
				if ((diff.DiffType & DifferenceType.SectionHeadAddedToCurrent) != 0 ||
					(diff.DiffType & DifferenceType.SectionHeadMissingInCurrent) != 0)
				{
					List<OverlapInfo> addedItemsPrev = fCurrentIsSource	? correlationPrior.itemsCurr :
						correlationPrior.itemsRev;
					int[] addedSectionHvos = fCurrentIsSource ? diff.HvosSectionsCurr :
						diff.HvosSectionsRev;
					Debug.Assert(addedItemsPrev.Count == 1, "Unexpected number items in previous correlation.");
					foreach (int addedSectionHvo in addedSectionHvos)
					{
						if (addedItemsPrev[0].myHvo == addedSectionHvo)
							return diff;
					}
				}
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets index of the first ScrVerse in specified section.
		/// </summary>
		/// <param name="srcScrVerses">The list of ScrVerses in the cluster.</param>
		/// <param name="iSectionAdded">The index of the section.</param>
		/// <returns>index of the first ScrVerse in the section, or -1 if section is empty.</returns>
		/// ------------------------------------------------------------------------------------
		private int GetFirstScrVerseInSection(List<ScrVerse> srcScrVerses, int iSectionAdded)
		{
			int iVerse;
			for (iVerse = 0; iVerse < srcScrVerses.Count; iVerse++)
			{
				// if this verse is in the added section...
				if (srcScrVerses[iVerse].ParaNodeMap.SectionIndex == iSectionAdded)
				{
					// Got the position we need!
					Debug.Assert(srcScrVerses[iVerse].VerseStartIndex == 0, "Should be first ScrVerse in section!");
					return iVerse;
				}
				// if we passed the added section...
				else if (srcScrVerses[iVerse].ParaNodeMap.SectionIndex > iSectionAdded)
					break;
			}

			return -1; // no ScrVerses in the specified section
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the last ScrVerse in the section just before the added section head.
		/// </summary>
		/// <param name="srcScrVerses">The list of ScrVerses on the source book (either Current
		/// or Revision). The source book is the book with the added section head.</param>
		/// <param name="iSectionAdded">The index of section added in the book.</param>
		/// <returns>index of the ScrVerse just after the added section head</returns>
		/// ------------------------------------------------------------------------------------
		private int GetScrVerseIndexJustBeforeSectionHead(List<ScrVerse> srcScrVerses, int iSectionAdded)
		{
			// scan for ScrVerse just prior to this section head
			int iScrVersePrior = -2; //to be set to index of the ScrVerse just prior, or -1 if none
			int iVerse;
			for (iVerse = 0; iVerse < srcScrVerses.Count; iVerse++)
			{
				// if this verse is in (or beyond) the section where the section head was added...
				if (srcScrVerses[iVerse].ParaNodeMap.SectionIndex >= iSectionAdded)
				{
					// Got the position we need! Calculate the prior verse index.
					iScrVersePrior = iVerse - 1;
					ScrVerse scrVersePrior = srcScrVerses[iScrVersePrior];
					if (scrVersePrior.ParaNodeMap.SectionIndex == iSectionAdded - 1)
						return iScrVersePrior;
					else
						break; // no ScrVerses in the prior section
				}
				// if we passed the added section...
				else if (srcScrVerses[iVerse].ParaNodeMap.SectionIndex > iSectionAdded)
					break;
			}

			return -1; // no ScrVerses in the prior section
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ScrVerse cluster containing the specified ScrVerse (specified by its index).
		/// </summary>
		/// <param name="iScrVerse">The index of the first ScrVerse in the section.</param>
		/// <param name="fCurrentIsSource">if set to <c>true</c> current is source; otherwise
		/// revision is source.</param>
		/// <returns>ScrVerse cluster containing the specified index of the ScrVerse</returns>
		/// ------------------------------------------------------------------------------------
		private Cluster GetScrVerseCluster(int iScrVerse, bool fCurrentIsSource)
		{
			foreach (Cluster scrVerseCluster in m_scrVerseClusters)
			{
				List<OverlapInfo> items = fCurrentIsSource ? scrVerseCluster.itemsCurr :
					scrVerseCluster.itemsRev;

				foreach (OverlapInfo oi in items)
				{
					if (oi.indexInOwner == iScrVerse)
						return scrVerseCluster;
				}
			}

			throw new Exception("Invalid ScrVerse index specified.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the destination IP from cluster.
		/// </summary>
		/// <param name="scrVerseCluster">The ScrVerse cluster.</param>
		/// <param name="fCurrentIsSource">if set to <c>true</c> current is source;
		/// <c>false</c> if revision is source.</param>
		/// <param name="fNeedDestAtStartOfCluster">if set to <c>true</c> we need destination IP to be
		/// at start of cluster's destination ScrVerses; if <c>false</c> we need IP to be at end.
		/// </param>
		/// <param name="paraDest">out: The destination para.</param>
		/// <param name="ichDest">out: The character offset in the destination para.</param>
		/// ------------------------------------------------------------------------------------
		private void GetDestinationIpFromCluster(Cluster scrVerseCluster, bool fCurrentIsSource,
			bool fNeedDestAtStartOfCluster, out StTxtPara paraDest, out int ichDest)
		{
			List<OverlapInfo> itemsDest = fCurrentIsSource ? scrVerseCluster.itemsRev :
				scrVerseCluster.itemsCurr;

			// Determine the index of the destination ScrVerse
			int iScrVerseDest;
			if (itemsDest.Count == 0)
			{
				// Process an added or missing cluster type (one-sided)
				iScrVerseDest = scrVerseCluster.indexToInsertAtInOther;
			}
			else
			{
				// Process two-sided cluster
				if (fNeedDestAtStartOfCluster)
					iScrVerseDest = itemsDest[0].indexInOwner;
				else
					iScrVerseDest = itemsDest[itemsDest.Count - 1].indexInOwner;
			}

			List<ScrVerse> scrVersesDest = fCurrentIsSource ? m_scrVersesRev :
				m_scrVersesCurr;

			// if the destination is beyond the end of the destination side ScrVerses (i.e. insert at end)
			// bail out
			if (iScrVerseDest >= scrVersesDest.Count)
			{
				paraDest = null;
				ichDest = -1;
				return;
			}

			// Get the destination ScrVerse and its para.
			ScrVerse scrVerseDest = scrVersesDest[iScrVerseDest];
			int hvoParaDest = scrVerseDest.HvoPara;
			paraDest = new StTxtPara(m_cache, hvoParaDest);

			// Calculate the character offset into destination para
			if (itemsDest.Count == 0)
			{
				// One-sided cluster
				ichDest = scrVerseDest.VerseStartIndex;
			}
			else
			{
				// Two-sided cluster
				ichDest = fNeedDestAtStartOfCluster ? scrVerseDest.VerseStartIndex :
						scrVerseDest.VerseStartIndex + scrVerseDest.TextLength;
			}
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Gets the difference just before section head. We expect the difference to be an added
		///// verse/paragraph or missing verse/para in the immediately preceding section.
		///// </summary>
		///// <param name="addedVerse">The added ScrVerse.</param>
		///// <param name="fCurrentIsSource">if set to <c>true</c> Current is the source;
		///// otherwise the Revision is the source.</param>
		///// <param name="iSectionAdded">The index of added section.</param>
		///// <returns>Difference just before the section head.</returns>
		///// ------------------------------------------------------------------------------------
		//private Difference GetDiffJustBeforeSectionHead(ScrVerse addedVerse, bool fCurrentIsSource,
		//    int iSectionAdded)
		//{
		//    // Scan through differences for verse/para that is MissingInCurrent immediately prior
		//    // to this section head.
		//    int iDiff;
		//    for (iDiff = ClusterDiffs.Count - 1; iDiff > 0; iDiff--)
		//    {
		//        int paraHvo = fCurrentIsSource ? ClusterDiffs[iDiff].HvoCurr : ClusterDiffs[iDiff].HvoRev;
		//        int ichDiff = fCurrentIsSource ? ClusterDiffs[iDiff].IchLimCurr : ClusterDiffs[iDiff].IchLimRev;

		//        // if the difference references the same paragraph and character offset
		//        // as the added verse/paragraph...
		//        if (paraHvo == addedVerse.HvoPara &&
		//            ichDiff == addedVerse.VerseStartIndex + addedVerse.TextLength)
		//        {
		//            int iSectionDiff = fCurrentIsSource ? ClusterDiffs[iDiff].ParaNodeMapCurr.SectionIndex :
		//                ClusterDiffs[iDiff].ParaNodeMapRev.SectionIndex;
		//            // and if the diff is in the immediately preceding section
		//            if (iSectionDiff == iSectionAdded - 1)
		//                return ClusterDiffs[iDiff]; //got it!
		//            else
		//                return null; // not in the immediately preceding section
		//        }
		//    }

		//    Debug.Fail("Did not find an added/missing para/verse diff.");
		//    return null;
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the correlated section.
		/// </summary>
		/// <param name="sectionHeadCorrelation">The section head correlation.</param>
		/// <param name="addedMissingClusterType">Type of the added missing section head correlation.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private ScrSection GetCorrelatedSection(Cluster sectionHeadCorrelation,
			ClusterType addedMissingClusterType)
		{
			Debug.Assert(sectionHeadCorrelation.clusterType == ClusterType.MatchedItems,
				"Must have matched pair of section heads");

			int hvoSection;
			if (Cluster.CurrentIsSource(addedMissingClusterType))
				hvoSection = sectionHeadCorrelation.itemsRev[0].myHvo;
			else
				hvoSection = sectionHeadCorrelation.itemsCurr[0].myHvo;

			return new ScrSection(m_cache, hvoSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first destination section in two-sided overlap cluster.
		/// </summary>
		/// <param name="overlapCluster">The overlap cluster.</param>
		/// <param name="fCurrentIsSource">if set to <c>true</c> current is source;
		/// <c>false</c> if revision is source.</param>
		/// <returns>first destination section in the overlap cluster</returns>
		/// ------------------------------------------------------------------------------------
		private ScrSection GetFirstDestSection(Cluster overlapCluster, bool fCurrentIsSource)
		{
			Debug.Assert(overlapCluster.itemsRev.Count > 0 && overlapCluster.itemsCurr.Count > 0,
				"Must be a two-sided overlap cluster.");
			int hvoSection;
			if (fCurrentIsSource)
				hvoSection = overlapCluster.itemsRev[0].myHvo;
			else
				hvoSection = overlapCluster.itemsCurr[0].myHvo;

			return new ScrSection(m_cache, hvoSection);
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Gets the destination IP for the given added section head by searching for a
		///// corresponding verse number.
		///// </summary>
		///// <param name="correlation">The correlation cluster for the added section head.</param>
		///// <param name="overlap">The section overlap cluster.</param>
		///// <param name="paraDest">out: The destination paragraph</param>
		///// <param name="ichDest">out: The destination char index.</param>
		///// ------------------------------------------------------------------------------------
		//private void GetDestinationIP_Old(Cluster correlation, Cluster overlap, out StTxtPara paraDest,
		//    out int ichDest)
		//{
		//    // Determine which side of the overlap cluster (whether revision or current)
		//    // we should search for the matching section in
		//    List<OverlapInfo> toSearch = null;
		//    if (correlation.clusterType == ClusterType.AddedToCurrent)
		//    {
		//        toSearch = overlap.itemsCurr;
		//    }
		//    else if (correlation.clusterType == ClusterType.MissingInCurrent)
		//    {
		//        toSearch = overlap.itemsRev;
		//    }

		//    // Get the section of the Destination to associate with
		//    ScrSection sectionDest = null;
		//    // lookup the added section head's proxy in the overlap cluster list to find
		//    // its list of overlapping items.
		//    foreach (OverlapInfo oi in toSearch)
		//    {
		//        if (oi.myHvo == correlation.HvosItemsSource[0])
		//        {
		//            // pick the first overlapping item from the list, as it is certain to
		//            // contain the insertion point for this section head
		//            // REVIEW: This is not the correct section. We need to add a new section in the case of
		//            //   TE-7132, not associate it with an existing section.
		//            sectionDest = new ScrSection(m_cache, ((OverlapInfo)oi.overlappedItemsInOther[0]).myHvo);
		//            break;
		//        }
		//    }
		//    Debug.Assert(sectionDest != null);

		//    // Get the paragraph hvo and ich in the Destination to associate with
		//    //int hvoParaDest = 0;
		//    paraDest = null;
		//    ichDest = 0;
		//    ScrReference currentRefStart = new ScrReference(sectionDest.VerseRefStart, m_scr.Versification);
		//    ScrReference currentRefEnd = new ScrReference(sectionDest.VerseRefStart, m_scr.Versification);
		//    bool fFoundTheVerse = false;

		//    int iPara = 0;
		//    for (; iPara < sectionDest.ContentOA.ParagraphsOS.Count; iPara++)
		//    {
		//        StTxtPara para = (StTxtPara)sectionDest.ContentOA.ParagraphsOS[iPara];
		//        //REVIEW: this is a simplistic solution. it can't find the best destination IP
		//        // if the verses are out of order, or if the added section head splits a verse.
		//        ITsString paraContents = para.Contents.UnderlyingTsString;
		//        int iRunLim = paraContents.RunCount;
		//        int iRunNew = 0;
		//        for (int iRun = 0; iRun < iRunLim && !fFoundTheVerse; )
		//        {
		//            // check the next reference in this para
		//            RefRunType refType = Scripture.GetNextRef(iRun, iRunLim, paraContents, true,
		//                ref currentRefStart, ref currentRefEnd, out iRunNew);
		//            if (currentRefStart >= correlation.verseRefMin)
		//            {
		//                // found the destination IP!
		//                //hvoParaDest = para.Hvo;
		//                paraDest = para;
		//                TsRunInfo runInfo;
		//                bool putIpAtEnd = false;

		//                // Get character position of run that started the reference.
		//                int iRunToUse;
		//                switch (refType)
		//                {
		//                    case RefRunType.ChapterAndVerse:
		//                        iRunToUse = iRunNew - 2;
		//                        break;
		//                    case RefRunType.Chapter:
		//                    case RefRunType.Verse:
		//                        iRunToUse = iRunNew - 1;
		//                        break;
		//                    default:
		//                        iRunToUse = iRunLim - 1;
		//                        putIpAtEnd = true;
		//                        break;
		//                }

		//                paraContents.FetchRunInfo(iRunToUse, out runInfo);
		//                ichDest = (putIpAtEnd ? runInfo.ichLim : runInfo.ichMin);
		//                fFoundTheVerse = true;
		//            }
		//            iRun = iRunNew;
		//        }
		//        if (fFoundTheVerse)
		//            break;
		//    }

		//    // Review: what do we do when the paragraph count is zero? Will that ever be?
		//    if (paraDest == null && sectionDest.ContentOA.ParagraphsOS.Count > 0)
		//    {
		//        // For some reason, we don't have a destination paragrah. Therefore,
		//        // just use the last paragraph in the destination section.
		//        int iLastPara = sectionDest.ContentOA.ParagraphsOS.Count - 1;
		//        paraDest = sectionDest.ContentOA.ParagraphsOS[iLastPara] as StTxtPara;
		//        ITsString paraContents = paraDest.Contents.UnderlyingTsString;
		//        if (paraContents.RunCount > 0)
		//        {
		//            TsRunInfo runInfo;
		//            paraContents.FetchRunInfo(paraContents.RunCount - 1, out runInfo);
		//            ichDest = runInfo.ichMin;
		//        }
		//    }

		//    //else if (iPara == 0 && ichDest == 0)
		//    //{
		//    //    // The IP is at the start of a section's contents. This may happen when a verse
		//    //    // spans sections, for example.
		//    //    // Since this is illegal because we are trying to split a section here. If we do so,
		//    //    // we will create an empty section. We need to scan in this section until we find the end
		//    //    // of the verse. If we find the verse and the end of the paragraph, we will not
		//    //    // continue to scan through the following segments of the verse to determine the
		//    //    // insertion point.
		//    //    ITsString paraContents = paraDest.Contents.UnderlyingTsString;
		//    //    int iRunLim = paraContents.RunCount;
		//    //    int iRunNew = 0;
		//    //    currentRefStart = new ScrReference(sectionDest.VerseRefStart, m_scr.Versification);
		//    //    currentRefEnd = new ScrReference(sectionDest.VerseRefStart, m_scr.Versification);
		//    //    for (int iRun = 0; iRun < iRunLim; )
		//    //    {
		//    //        // check the next reference in this para
		//    //        RefRunType refType = Scripture.GetNextRef(iRun, iRunLim, paraContents, true,
		//    //            ref currentRefStart, ref currentRefEnd, out iRunNew);
		//    //        if (refType == RefRunType.None && currentRefStart >= correlation.verseRefMin)
		//    //        {
		//    //            ichDest = paraContents.get_LimOfRun(iRun);
		//    //            return;
		//    //        }
		//    //        iRun = iRunNew;
		//    //    }

		//    //    Debug.Assert(currentRefStart >= correlation.verseRefMin,
		//    //        "First paragraph does not contain target correlation reference.");
		//    //    // set the IP to the end of the first paragraph.
		//    //    ichDest = paraContents.Length;
		//    //}
		//}
		#endregion

		#region DetectDifferences: Missing/Added First Section Head
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes a missing/added section head when it is the first in the cluster.
		/// </summary>
		/// <param name="correlation">The correlation cluster identifying the added section
		/// head.</param>
		/// <param name="overlap">The overlap cluster from which correlation clusters were
		/// derived.</param>
		/// ------------------------------------------------------------------------------------
		public void ProcessMissingAddedFirstSectionHead(Cluster correlation, Cluster overlap)
		{
			CheckDisposed();

			// translate cluster type to diff type
			DifferenceType diffType;
			if (correlation.clusterType == ClusterType.AddedToCurrent)
				diffType = DifferenceType.SectionAddedToCurrent;
			else if (correlation.clusterType == ClusterType.MissingInCurrent)
				diffType = DifferenceType.SectionMissingInCurrent;
			else
				throw new Exception("BookMerger.ProcessMissingAddedFirstSectionHead - clusterType must be Missing or Added");

			StTxtPara paraDest;
			int ichDest;
			//TODO: Perhaps write a GetDestinationIPAtEnd to match the end of the added section to
			// the last overlapping section, to the matching ScrVerse
			GetDestinationIP(correlation, overlap, diffType, out paraDest, out ichDest);
			ichDest = 0; //patch for now

			// Create a root difference for the added/missing section
			Difference rootDiff = new Difference(correlation.verseRefMin, correlation.verseRefMax, diffType,
				correlation.HvosItemsSource, paraDest, ichDest, m_cache);
			ClusterDiffs.Add(rootDiff);

			// Make subdiffs for ScrVerses that were moved.
			rootDiff.SubDiffsForParas = DetermineVersesMovedDiffs(correlation, diffType);

			// Remove the Added Verse/Para Diffs
			for (int i = ClusterDiffs.Count - 1; i >= 0; i--)
			{
				Difference diff = ClusterDiffs[i];
				if ((diff.DiffType == DifferenceType.VerseAddedToCurrent ||
					diff.DiffType == DifferenceType.ParagraphAddedToCurrent ||
					diff.DiffType == DifferenceType.StanzaBreakAddedToCurrent) &&
					correlation.itemsCurr[0].indexInOwner == diff.ParaNodeMapCurr.SectionIndex)
				{
					ClusterDiffs.RemoveAt(i);
				}

				// If this difference is a one-sided multi-paragraph verse on the Current side...
				else if (diff.DiffType == DifferenceType.ParagraphStructureChange &&
					diff.SubDiffsForParas[0].IchMinRev == diff.SubDiffsForParas[0].IchLimRev &&
					correlation.itemsCurr[0].indexInOwner == diff.ParaNodeMapCurr.SectionIndex)
				{
					// also remove this difference since it's included in the added section.
					ClusterDiffs.RemoveAt(i);
				}

				// If this difference is a verse-boundary paragraph split on the Current side...
				else if (diff.DiffType == DifferenceType.ParagraphSplitInCurrent &&
					diff.SubDiffsForParas[0].IchMinRev == diff.SubDiffsForParas[0].IchLimRev &&
					correlation.itemsCurr[0].indexInOwner == diff.ParaNodeMapCurr.SectionIndex)
				{
					// also remove this difference since it's included in the added section.
					ClusterDiffs.RemoveAt(i);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines the verse moved diffs.
		/// </summary>
		/// <param name="correlation">The correlation cluster identifying the added section
		/// head.</param>
		/// <param name="diffType">Type of the diff.</param>
		/// <returns>list of ScrVerses which have been moved to another section</returns>
		/// ------------------------------------------------------------------------------------
		private List<Difference> DetermineVersesMovedDiffs(Cluster correlation, DifferenceType diffType)
		{
			//TODO: Some of the following code & methods need updating to make them general for SectionAdded
			// or SectionMissing diff types.

			// get the section index of the added section head;
			//  the verses have been moved into this section
			int iSectionMovedTo = correlation.SourceItems[0].indexInOwner;

			// collect the ScrVerses that were moved into the section
			List<ScrVerse> versesMovedToSection = new List<ScrVerse>();
			foreach (ScrVerse verse in SourceVerses(diffType))
			{
				// if this verse is in the section the verses were moved into...
				if (verse.ParaNodeMap.SectionIndex == iSectionMovedTo)
				{
					// and this verse is not part of an 'added' diff in our cluster...
					if (!ScrVerseIsAdded(verse))
						versesMovedToSection.Add(verse); // it has been moved; save it
				}
			}

			// generate the verse moved differences
			List<Difference> verseMovedDiffs = new List<Difference>();
			foreach (ScrVerse verse in versesMovedToSection)
			{
				// get the matched ScrVerse in the other book
				Comparison comparison = FindComparison(verse);
				Debug.Assert(comparison != null);
				// TE-7260: Is an edge case where we don't have a comparison.
				// What needs to be done here is use the ScrVerse clusters to find the corresponding
				// ScrVerse(s). One complication is that it could be a one-to-many, many-to-one or
				// multiple-to-multiple ScrVerse cluster.
				// So, as a patch for now, we will fail to detect the verse moved difference
				// if the comparison is null.
				if (comparison == null)
					continue;
				ScrVerse matchedVerse = comparison.verseRev;
				// get its section index
				int iSectionOtherBook = matchedVerse.ParaNodeMap.SectionIndex;

				// find the section our moved verse was moved from.
				// method: find matched verse's correlated section head
				int iSectionMovedFrom = -1;
				int hvoSectionMovedFrom = -1;
				foreach (Cluster corr in m_sectionHeadCorrelationList)
				{
					if (corr.itemsRev.Count > 0 &&
						corr.itemsRev[0].indexInOwner == iSectionOtherBook)
					{
						Debug.Assert(corr.itemsRev.Count == 1);
						iSectionMovedFrom = corr.itemsCurr[0].indexInOwner;
						hvoSectionMovedFrom = corr.itemsCurr[0].myHvo;
						break;
					}
				}
				Debug.Assert(iSectionMovedFrom >= 0);
				Debug.Assert(hvoSectionMovedFrom > 0);

				// Determine the MovedFromIP
				int hvoParaMovedFrom = -1;
				int ichMovedFrom = -1;
				if (iSectionMovedFrom > iSectionMovedTo)
				{
					// our MovedFromIP is at the start of this section's first para
					ScrSection section = new ScrSection(m_cache, hvoSectionMovedFrom);
					Debug.Assert(section.ContentOA.ParagraphsOS.Count > 0);
					hvoParaMovedFrom = section.ContentOA.ParagraphsOS[0].Hvo;
					ichMovedFrom = 0;
				}
				else
					Debug.Assert(false, "not implemented yet");

				// Create the VerseMoved difference
				Difference diff = new Difference(verse, matchedVerse, DifferenceType.VerseMoved);
				diff.SetMovedFromIP(hvoParaMovedFrom, ichMovedFrom);
				verseMovedDiffs.Add(diff);
			}

			return verseMovedDiffs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the comparison containing the given ScrVerse.
		/// </summary>
		/// <param name="verse">ScrVerse to find in comparison list</param>
		/// <returns>The matching verse or null if not found.</returns>
		/// ------------------------------------------------------------------------------------
		private Comparison FindComparison(ScrVerse verse)
		{
			Debug.Assert(m_verseComparisons != null);

			foreach (Comparison comparison in m_verseComparisons)
			{
				if (comparison.verseCurr == verse)
					return comparison;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the compared verse.
		/// </summary>
		/// <param name="sourceVerse">The source verse.</param>
		/// <param name="diffType">Type of the diff.</param>
		/// <returns>ScrVerse that is being compared to the source verse</returns>
		/// <exception cref="InvalidOperationException">thrown if diffType is not added to current
		/// or missing in current.</exception>
		/// ------------------------------------------------------------------------------------
		private ScrVerse GetComparedVerse(ScrVerse sourceVerse, DifferenceType diffType)
		{
			Debug.Assert(m_verseComparisons != null);
			bool fCurrentIsSource = Difference.CurrentIsSource(diffType);

			foreach (Comparison comparison in m_verseComparisons)
			{
				if (fCurrentIsSource &&	comparison.verseCurr == sourceVerse)
					return comparison.verseRev;
				else if (!fCurrentIsSource && comparison.verseRev == sourceVerse)
					return comparison.verseCurr;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines the ScrVerses in the source cluster.
		/// </summary>
		/// <param name="diffType">type of difference</param>
		/// <returns>list of ScrVerses</returns>
		/// ------------------------------------------------------------------------------------
		private List<ScrVerse> SourceVerses(DifferenceType diffType)
		{
			bool fCurrentIsSource = Difference.CurrentIsSource(diffType);

			if (fCurrentIsSource)
				return m_scrVersesCurr;
			else if (!fCurrentIsSource)
				return m_scrVersesRev;
			else
				throw new Exception("BookMerger.SourceVerses - unexpected type of difference");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the given ScrVerse is part of an 'added' difference in our cluster.
		/// </summary>
		/// <param name="verse">The given ScrVerse.</param>
		/// <returns>true if the given ScrVerse is part of an 'added' diff in our cluster
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool ScrVerseIsAdded(ScrVerse verse)
		{
			foreach (Difference diff in ClusterDiffs)
			{
				if (diff.DiffType == DifferenceType.VerseAddedToCurrent ||
					diff.DiffType == DifferenceType.ParagraphAddedToCurrent ||
					diff.DiffType == DifferenceType.StanzaBreakAddedToCurrent)
				{
					// if this difference refers to the given scrVerse...
					if (verse.StartRef >= diff.RefStart && verse.StartRef <= diff.RefEnd &&
						diff.HvoCurr == verse.HvoPara)
					{
						return true;
					}
				}

				// If this difference is a one-sided multi-paragraph verse on the Current side...
				if (diff.DiffType == DifferenceType.ParagraphStructureChange &&
					diff.SubDiffsForParas[0].IchMinRev == diff.SubDiffsForParas[0].IchLimRev &&
					verse.StartRef >= diff.RefStart && verse.StartRef <= diff.RefEnd)
				{
					foreach (Difference subDiff in diff.SubDiffsForParas)
					{
						// if this subdifference refers to the given scrVerse...
						if (subDiff.HvoCurr == verse.HvoPara)
							return true; // this ScrVerse is added in the Current.
					}
				}
			}

			return false;
		}
		#endregion

		#region Detect Differences: Title
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine the differences in the titles of the given books.
		/// Appends the differences found to the master list of diffs.
		/// </summary>
		/// <param name="bookCurr">the given current book</param>
		/// <param name="bookRev">the given book revision</param>
		/// ------------------------------------------------------------------------------------
		private void DetectDifferencesInTitle(IScrBook bookCurr, IScrBook bookRev)
		{
			IStText bookTitleCurr = bookCurr.TitleOA;
			IStText bookTitleRev = bookRev.TitleOA;
			BCVRef titleRef = new BCVRef(bookRev.CanonicalNum, 1, 0);
			// need to init the temporary list, since the title is not in a 'cluster'
			ClusterDiffs = new List<Difference>();

			// title StText may be null in some tests, but not real life
			if (bookTitleRev != null && bookTitleCurr != null)
				DetectDifferencesInStTexts_ByParagraph(bookTitleCurr, bookTitleRev, titleRef);

			if (m_fAttemptAutoMerge)
			{
				foreach (Difference diff in ClusterDiffs)
				{
					// If a title exists in only one of the versions or if an identical title
					// exists in both versions (i.e., no difference at all), then we can safely
					// automerge them. Otherwise, no can do.
					if (!IsTitleAddedToCurrent(diff) && !IsTitleMissingInCurrent(diff))
						m_fCannotMergeBecauseOfTitleDifference = true;
				}
			}

			m_differences.AddRange(ClusterDiffs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified diff represents a title that was added to current
		/// version (i.e., not in revision at all).
		/// </summary>
		/// <param name="diff">The difference.</param>
		/// <returns>
		/// 	<c>true</c> if the specified diff represents a title that was added to current
		/// 	version; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool IsTitleAddedToCurrent(Difference diff)
		{
			Debug.Assert(IsTitleParaDiff(diff),
				"IsTitleAddedToCurrent should only be called for a difference on a title.");
			if (diff.DiffType != DifferenceType.TextDifference)
				return false;
			return (diff.IchMinCurr == 0 && diff.IchLimCurr > 0 &&
				diff.IchMinRev == 0 && diff.IchLimRev == 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified diff represents a title that is missing in the
		/// current version (i.e., it was added to the revision).
		/// </summary>
		/// <param name="diff">The difference.</param>
		/// <returns>
		/// 	<c>true</c> if the specified diff represents a title that is missing in the
		///		current version; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool IsTitleMissingInCurrent(Difference diff)
		{
			if (diff.DiffType != DifferenceType.TextDifference)
				return false;
			return (diff.IchMinRev == 0 && diff.IchLimRev > 0 &&
				diff.IchMinCurr == 0 && diff.IchLimCurr == 0) && IsTitleParaDiff(diff);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified difference is for a title paragraph.
		/// </summary>
		/// <param name="diff">The difference</param>
		/// <returns>
		/// 	<c>true</c> if the specified difference is for a title paragraph;
		/// 	otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool IsTitleParaDiff(Difference diff)
		{
			Debug.Assert(diff.HvoCurr > 0);
			StTxtPara possibleTitlePara = new StTxtPara(Cache, diff.HvoCurr);
			return (possibleTitlePara.Owner.OwningFlid == (int)ScrBook.ScrBookTags.kflidTitle);
		}
		#endregion

		#region Detect Differences: By Verse (Scripture text)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Detect differences in the given pair of StTexts.
		/// </summary>
		/// <param name="textCurr"></param>
		/// <param name="textRev"></param>
		/// ------------------------------------------------------------------------------------
		private void DetectDifferencesInStText(IStText textCurr, IStText textRev)
		{
			// Set up the curr & rev verse iterators for the given pair of StTexts
			m_currIterator = new VerseIteratorForStText(textCurr);
			m_revIterator = new VerseIteratorForStText(textRev);

			GatherScrVerses();
			DetectDifferencesInScrVerses(m_scrVersesCurr, m_scrVersesRev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Detect differences in the given lists of StTexts.
		/// </summary>
		/// <param name="stTextsCurr"></param>
		/// <param name="stTextsRev"></param>
		/// ------------------------------------------------------------------------------------
		protected void DetectDifferencesInListOfStTexts(List<IStText> stTextsCurr, List<IStText> stTextsRev)
		{
			// Set up the curr and rev verse iterators for the given collections of StTexts
			m_currIterator = new VerseIteratorForSetOfStTexts(stTextsCurr);
			m_revIterator = new VerseIteratorForSetOfStTexts(stTextsRev);

			GatherScrVerses();

			DetectDifferencesInScrVerses(m_scrVersesCurr, m_scrVersesRev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gathers the ScrVerses using the provided iterators.
		/// The caller must initialize m_currIterator and m_revIterator with the given
		///  StTexts before calling this method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void GatherScrVerses()
		{
			// Gather all ScrVerses for the StTexts into two array lists
			m_scrVersesCurr = new List<ScrVerse>();
			for (ScrVerse verseCurr = FirstVerseForStText(m_currIterator, ref m_endOfPrevVerseCurr);
				verseCurr != null;
				verseCurr = m_currIterator.NextVerse())
			{
				m_scrVersesCurr.Add(verseCurr);
			}
			m_scrVersesRev = new List<ScrVerse>();
			for (ScrVerse verseRev = FirstVerseForStText(m_revIterator, ref m_endOfPrevVerseRev);
				verseRev != null;
				verseRev = m_revIterator.NextVerse())
			{
				m_scrVersesRev.Add(verseRev);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine all the differences in the given sets of ScrVerses.
		/// This is the workhorse function that processes through the ScrVerses and then
		/// appends the differences found to the master list of diffs.
		/// </summary>
		/// <param name="scrVersesCurr">list of ScrVerses for the Current</param>
		/// <param name="scrVersesRev">list of ScrVerses for the Revision</param>
		/// ------------------------------------------------------------------------------------
		private void DetectDifferencesInScrVerses(List<ScrVerse> scrVersesCurr, List<ScrVerse> scrVersesRev)
		{
			// Look for paragraph splits and merges
			DetectParaSplitMergeAtVerseBoundaries();

			// Make list of clusters of ScrVerse with overlapping reference ranges.
			m_scrVerseClusters = ClusterListHelper.DetermineScrVerseOverlapClusters(
				scrVersesCurr, scrVersesRev, m_cache);

			// m_diff will keep track of the current difference when out-of-synch
			// verse bridges need to accumulate as a single difference.
			m_diff = null;
			// clear paragraph hvos used for para style comparisons
			m_hvoPrevParaCurr = 0;
			m_hvoPrevParaRev = 0;

			// Process each cluster of ScrVerses
			foreach (Cluster cluster in m_scrVerseClusters )
			{
				if (cluster.clusterType == ClusterType.MatchedItems)
				{
					m_verseCurr = scrVersesCurr[cluster.itemsCurr[0].indexInOwner];
					m_verseRev = scrVersesRev[cluster.itemsRev[0].indexInOwner];

					ProcessFirstPairOfScrVerses();

					m_endOfPrevVerseCurr = MakeEndOfPreviousVerse(m_verseCurr);
					m_endOfPrevVerseRev = MakeEndOfPreviousVerse(m_verseRev);
				}

				else if (cluster.clusterType == ClusterType.MissingInCurrent && cluster.itemsRev.Count == 1)
				{
					m_verseCurr = null;
					foreach (OverlapInfo oi in cluster.itemsRev)
					{
						m_verseRev = scrVersesRev[oi.indexInOwner];
						ProcessSingleAddedMissingScrVerse();
					}
					m_endOfPrevVerseRev = MakeEndOfPreviousVerse(m_verseRev);
				}

				else if (cluster.clusterType == ClusterType.AddedToCurrent && cluster.itemsCurr.Count == 1)
				{
					m_verseRev = null;
					foreach (OverlapInfo oi in cluster.itemsCurr)
					{
						m_verseCurr = scrVersesCurr[cluster.itemsCurr[0].indexInOwner];
						ProcessSingleAddedMissingScrVerse();
					}
					m_endOfPrevVerseCurr = MakeEndOfPreviousVerse(m_verseCurr);
				}

				else if (cluster.clusterType == ClusterType.OrphansInCurrent ||
						cluster.clusterType == ClusterType.AddedToCurrent)
						//note: multiple AddedToCurrent ScrVerses are normally in different paras
				{
					ProcessParaStructureOneSidedCluster(cluster);
					m_endOfPrevVerseCurr = MakeEndOfPreviousVerse(m_scrVersesCurr[GetLastScrVerseIndexInCluster(cluster.itemsCurr)]);
				}
				else if	(cluster.clusterType == ClusterType.MissingInCurrent ||
						cluster.clusterType == ClusterType.OrphansInRevision)
						//note: multiple MissingInCurrent ScrVerses are normally in different paras
				{
					ProcessParaStructureOneSidedCluster(cluster);
					m_endOfPrevVerseRev = MakeEndOfPreviousVerse(m_scrVersesRev[GetLastScrVerseIndexInCluster(cluster.itemsRev)]);
				}

				else if (cluster.clusterType == ClusterType.MultipleInBoth ||
						cluster.clusterType == ClusterType.SplitInCurrent ||
						cluster.clusterType == ClusterType.MergedInCurrent)
				{
					// If there is no para break in this cluster..
					if (!cluster.SpansParaBreak)
					{
						// Our complex cluster items contain verse bridge overlaps within one paragraph.
						// process the first pair of ScrVerses as a new diff
						m_verseCurr = scrVersesCurr[cluster.itemsCurr[0].indexInOwner];
						m_verseRev = scrVersesRev[cluster.itemsRev[0].indexInOwner];
						ProcessFirstPairOfScrVerses();

						// accumulate stuff from the remaining ScrVerses
						int maxScrVerseCount = Math.Max(cluster.itemsCurr.Count, cluster.itemsRev.Count);
						for (int i = 1; i < maxScrVerseCount; i++)
						{
							if (i < cluster.itemsRev.Count)
								m_verseRev = scrVersesRev[cluster.itemsRev[i].indexInOwner];
							if (i < cluster.itemsCurr.Count)
								m_verseCurr = scrVersesCurr[cluster.itemsCurr[i].indexInOwner];
							ProcessMoreVerseBridgeItemsWithinPara();
						}

						m_endOfPrevVerseCurr = MakeEndOfPreviousVerse(m_verseCurr);
						m_endOfPrevVerseRev = MakeEndOfPreviousVerse(m_verseRev);
					}

					else
					{
						// Our complex cluster items contains verse segments in multiple paragraphs
						ProcessParaStructureCluster(cluster);
						m_endOfPrevVerseCurr = MakeEndOfPreviousVerse(
							m_scrVersesCurr[GetLastScrVerseIndexInCluster(cluster.itemsCurr)]);
						m_endOfPrevVerseRev = MakeEndOfPreviousVerse(
							m_scrVersesRev[GetLastScrVerseIndexInCluster(cluster.itemsRev)]);
					}
				}

				else
				{
					Debug.Fail("Unhandled cluster type");
				}
			}
			// finally, finalize the last accumulation, if any
			if (m_diff != null)
				ClusterDiffs.Add(m_diff);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last ScrVerse in cluster.
		/// </summary>
		/// <param name="items">The list of either Current or Revision ScrVerses in the cluster.
		/// </param>
		/// <returns>Index of the ScrVerse</returns>
		/// ------------------------------------------------------------------------------------
		private int GetLastScrVerseIndexInCluster(List<OverlapInfo> items)
		{
			return items[items.Count - 1].indexInOwner;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the verse bridge items within a paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessMoreVerseBridgeItemsWithinPara()
		{
			// accumulate a difference to cover the entire range.
			CreateOrAccumTextDifference();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Detect the differences for a multi-paragraph cluster that has added or missing
		/// paragraph structure on only one side - the Current or the Revision.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessParaStructureOneSidedCluster(Cluster cluster)
		{
			Debug.Assert(cluster.itemsCurr.Count == 0 || cluster.itemsRev.Count == 0);
			Debug.Assert(!ClusterSpansStTexts(cluster));
			int cItemsInCurr = cluster.itemsCurr.Count;
			int cItemsInRev = cluster.itemsRev.Count;

			// Establish the Diff type
			DifferenceType diffType = DifferenceType.ParagraphStructureChange;
			if (cluster.clusterType == ClusterType.OrphansInCurrent && cItemsInCurr == 1)
				diffType = DifferenceType.ParagraphSplitInCurrent;
			else if (cluster.clusterType == ClusterType.OrphansInRevision && cItemsInRev == 1)
				diffType = DifferenceType.ParagraphMergedInCurrent;
			// REVIEW: ??make new diff types if multiple orphans: ParasAddedWithinVerseInCurrent, ParasMissingWithinVerseInCurrent

			Difference baseDiff = new Difference(cluster.verseRefMin, cluster.verseRefMax,
				diffType, 0, 0, 0, 0, 0, 0,
				null, null);

			// Create Subdiffs
			CreateSubDiffsForOneSidedClusterParas(baseDiff, cluster);

			baseDiff.FixParaStructureRootInfo();
			ClusterDiffs.Add(baseDiff);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Detect the differences for a multi-paragraph cluster that has changes in
		/// the paragraph structure.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ProcessParaStructureCluster(Cluster cluster)
		{
			Debug.Assert(cluster.itemsCurr.Count > 0 && cluster.itemsRev.Count > 0);
			Debug.Assert(!ClusterSpansStTexts(cluster));

			// get the range of ScrVerses for this ScrVerse cluster
			int iMinScrVerseClstrCurr = cluster.itemsCurr[0].indexInOwner;
			int iLimScrVerseClstrCurr = cluster.itemsCurr[cluster.itemsCurr.Count - 1].indexInOwner + 1;
			int iMinScrVerseClstrRev = cluster.itemsRev[0].indexInOwner;
			int iLimScrVerseClstrRev = cluster.itemsRev[cluster.itemsRev.Count - 1].indexInOwner + 1;

			List<ScrVerse> scrVersesBlobCurr = new List<ScrVerse>(4); //capacity of 4 should be adequate
			List<ScrVerse> scrVersesBlobRev = new List<ScrVerse>(4);

			for (int i = iMinScrVerseClstrCurr; i < iLimScrVerseClstrCurr; i++)
				scrVersesBlobCurr.Add(m_scrVersesCurr[i].Clone()); // need clone rather than reference??
			for (int i = iMinScrVerseClstrRev; i < iLimScrVerseClstrRev; i++)
				scrVersesBlobRev.Add(m_scrVersesRev[i].Clone());

			// Establish the Diff type
			DifferenceType diffType = DifferenceType.ParagraphStructureChange;
			if (cluster.clusterType == ClusterType.SplitInCurrent)
				diffType = DifferenceType.ParagraphSplitInCurrent;
			else if (cluster.clusterType == ClusterType.MergedInCurrent)
				diffType = DifferenceType.ParagraphMergedInCurrent;

			Difference baseDiff = new Difference(cluster.verseRefMin, cluster.verseRefMax,
				diffType, 0, 0, 0, 0, 0, 0,
				scrVersesBlobCurr[0].ParaNodeMap, scrVersesBlobRev[0].ParaNodeMap);

			// Get the lengths of matching text in the start paras and in the end paras in the cluster
			int matchedTextLenInStartParas, matchedTextLenInEndParas;
			DifferenceType textCompareDiffType;
			GetMatchedTextLengths(scrVersesBlobCurr, scrVersesBlobRev,
				out matchedTextLenInStartParas, out matchedTextLenInEndParas, out textCompareDiffType,
				baseDiff);

			// Create Subdiffs
			CreateSubDiffsForClusterParas(baseDiff, scrVersesBlobCurr, scrVersesBlobRev,
				matchedTextLenInStartParas, matchedTextLenInEndParas, textCompareDiffType);

			//If the blob has different count of paragraphs in the Curr and Rev, and more than one in each,
			// slide the early ending para info down to the last subdiff (for convenience when reverting)
			FixMultiToMultiSubDiffs(baseDiff);

			baseDiff.FixParaStructureRootInfo();
			ClusterDiffs.Add(baseDiff);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the lengths of matching text in the start and ending paragraphs within the cluster
		/// We will compare the text of the Revision items as a whole and the Current items as a whole.
		/// </summary>
		/// <param name="scrVersesCur">list of ScrVerses on the Current side of the cluster</param>
		/// <param name="scrVersesRev">list of ScrVerses on the Revision side of the cluster</param>
		/// <param name="matchedTextLenInStartParas">out: The matched text length in the start paras.</param>
		/// <param name="matchedTextLenInEndParas">out: The matched text length in the end paras.</param>
		/// <param name="textCompareDiffType">out: The DifferenceType bits from comparing the
		/// concatenated Revision and Current text</param>
		/// <param name="baseDiff">The base difference where sub differences may be set fort
		/// ORCs, if any, in the range of different text.</param>
		/// ------------------------------------------------------------------------------------
		private void GetMatchedTextLengths(List<ScrVerse> scrVersesCur, List<ScrVerse> scrVersesRev,
			out int matchedTextLenInStartParas, out int matchedTextLenInEndParas,
			out DifferenceType textCompareDiffType, Difference baseDiff)
		{
			Debug.Assert(scrVersesCur.Count > 0 && scrVersesRev.Count > 0);

			// Concatenate all Curr and Rev text for the cluster
			ITsString tssConcatCurr, tssConcatRev;
			tssConcatCurr = ConcatenateScrVerses(scrVersesCur, 0, scrVersesCur.Count);
			tssConcatRev = ConcatenateScrVerses(scrVersesRev, 0, scrVersesRev.Count);

			// Compare the concatenated texts
			int ichMinConcat, ichLimConcatCurr, ichLimConcatRev; // range of text changes found
			string sCharStyleNameCurr = null, sCharStyleNameRev = null;
			string sWsNameCurr = null, sWsNameRev = null;
			if (baseDiff.SubDiffsForORCs == null)
				baseDiff.SubDiffsForORCs = new List<Difference>();
			textCompareDiffType = CompareVerseText(tssConcatCurr, tssConcatRev,
				out ichMinConcat, out ichLimConcatCurr, out ichLimConcatRev, baseDiff.SubDiffsForORCs,
				ref sCharStyleNameCurr, ref sCharStyleNameRev, ref sWsNameCurr, ref sWsNameRev);

			// For this base diff, we may need to extend the text difference range to include first and last
			// paragraph breaks in the cluster.
			// First, calculate the length of the shortest 'starting ScrVerse to the endOfStartPara'.
			// The shortest length indicates the first location of a paragraph break.
			StTxtPara startParaClstrCurr = new StTxtPara(m_cache, scrVersesCur[0].HvoPara);
			StTxtPara startParaClstrRev = new StTxtPara(m_cache, scrVersesRev[0].HvoPara);
			int minLenInStartParas = Math.Min(
				ParaLength(startParaClstrCurr) - scrVersesCur[0].VerseStartIndex,
				ParaLength(startParaClstrRev) - scrVersesRev[0].VerseStartIndex);
			// Second, calculate the length of the shortest startOfEndPara-thru-ending ScrVerse.
			// The shortest length indicates the last position of a paragraph break.
			ScrVerse endScrVerseCurr = scrVersesCur[scrVersesCur.Count - 1];
			ScrVerse endScrVerseRev = scrVersesRev[scrVersesRev.Count - 1];
			int minLenInEndParas = Math.Min(
				endScrVerseCurr.VerseStartIndex + endScrVerseCurr.TextLength,
				endScrVerseRev.VerseStartIndex + endScrVerseRev.TextLength);

			// Finally, calculate the Length outputs!
			if (textCompareDiffType != DifferenceType.NoDifference)
			{
				// Third, if there is a text difference, based on theses shortest lengths within the
				// StartParas and EndParas, we may 'extend' the concatenated text difference range to
				// include first and last paragraph breaks in the cluster by now calculating the length
				// of the MATCHED text to delimit the highlighted text in the start and end paragraphs.
				matchedTextLenInStartParas = Math.Min(ichMinConcat, minLenInStartParas);
				matchedTextLenInEndParas = Math.Min(tssConcatCurr.Length - ichLimConcatCurr, minLenInEndParas);
				Debug.Assert(tssConcatCurr.Length - ichLimConcatCurr == tssConcatRev.Length - ichLimConcatRev);
			}
			else
			{
				// text is identical, so we must set the text difference range to that which includes
				//  the paragraph breaks
				matchedTextLenInStartParas = minLenInStartParas;
				matchedTextLenInEndParas = minLenInEndParas;
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the length of the paragraph.
		/// </summary>
		/// <param name="para">The specified para.</param>
		/// <returns>number of characters in the paragraph</returns>
		/// ------------------------------------------------------------------------------------
		private int ParaLength(StTxtPara para)
		{
			if (para.Contents.Text == null)
				return 0;
			return para.Contents.Text.Length;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Concatenate the verse text for a specified range of ScrVerses.
		/// </summary>
		/// <param name="scrVerses">The current or revision verses from current section.</param>
		/// <param name="iMin">The first ScrVerse to concatenate.</param>
		/// <param name="iLim">The limit of the ScrVerse to concatenate.</param>
		/// <returns>The concatenated ITsString of the specified ScrVerses/// </returns>
		/// ------------------------------------------------------------------------------------
		private ITsString ConcatenateScrVerses(List<ScrVerse> scrVerses, int iMin, int iLim)
		{
			ITsStrBldr strBldr = TsStrBldrClass.Create();

			Debug.Assert(iMin >= 0 && iLim <= scrVerses.Count);
			for (int iVerse = iMin; iVerse < iLim; iVerse++)
			{
				ScrVerse verse = scrVerses[iVerse];
				strBldr.ReplaceTsString(strBldr.Length, strBldr.Length, verse.Text);
			}

			return strBldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the Subdiffs for this multi-para one-sided cluster of ScrVerses.
		/// The pieces of the subDiffs will represent one paragraph each.
		/// (The first subdiff piece may be the last portion of an earlier paragraph,
		/// the last subdiff piece may be the first portion of a later paragraph.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateSubDiffsForOneSidedClusterParas(Difference baseDiff, Cluster cluster)
		{
			int cItemsInCurr = cluster.itemsCurr.Count;
			int cItemsInRev = cluster.itemsRev.Count;

			// a properly formed one-sided cluster looks like this...
			Debug.Assert(cItemsInCurr == 0 || cItemsInRev == 0);
			Debug.Assert(cItemsInCurr > 0 || cItemsInRev > 0);
			Debug.Assert(cluster.indexToInsertAtInOther > -1);

			// By design, all one-sided ScrVerses are in different paragraphs. Assert that it is so...
			#if DEBUG
			if (cItemsInCurr > 1)
			{
				int hvoPara = cluster.itemsCurr[0].myHvo;
				for (int iCurr = 1; iCurr < cItemsInCurr; iCurr++)
				{
					if (cluster.itemsCurr[iCurr].myHvo == hvoPara)
						Debug.Fail("A ParaStructure cluster must have a different paragraph in each ScrVerse");
				}
			}

			if (cItemsInRev > 1)
			{
				int hvoPara = cluster.itemsRev[0].myHvo;
				for (int iRev = 1; iRev < cItemsInRev; iRev++)
				{
					if (cluster.itemsRev[iRev].myHvo == hvoPara)
						Debug.Fail("A ParaStructure cluster must have a different paragraph in each ScrVerse");
				}
			}
			#endif

			// Get the previous ScrVerses if available; also the first ScrVerse in some cases
			ScrVerse prevScrVerseCurr = null;
			ScrVerse prevScrVerseRev = null;
			ScrVerse firstScrVerseCurr = null;
			ScrVerse firstScrVerseRev = null;
			if (cItemsInCurr > 0)
			{
				firstScrVerseCurr = m_scrVersesCurr[cluster.itemsCurr[0].indexInOwner];
				// get the prevScrVerse only if it exists and is within the same StText
				// (note that the list of ScrVerses may be concatenated from more than one Section)
				if (cluster.itemsCurr[0].indexInOwner > 0 && !FirstInStText(firstScrVerseCurr))
					prevScrVerseCurr = m_scrVersesCurr[cluster.itemsCurr[0].indexInOwner - 1];
				if (cluster.indexToInsertAtInOther > 0)
					prevScrVerseRev = m_scrVersesRev[cluster.indexToInsertAtInOther - 1];
				else
					firstScrVerseRev = m_scrVersesRev[cluster.indexToInsertAtInOther];
			}
			else
			{
				Debug.Assert(cItemsInRev > 0);
				firstScrVerseRev = m_scrVersesRev[cluster.itemsRev[0].indexInOwner];
				// get the prevScrVerse only if it exists and is within the same StText
				if (cluster.itemsRev[0].indexInOwner > 0 && !FirstInStText(firstScrVerseRev))
					prevScrVerseRev = m_scrVersesRev[cluster.itemsRev[0].indexInOwner - 1];
				if (cluster.indexToInsertAtInOther > 0)
					prevScrVerseCurr = m_scrVersesCurr[cluster.indexToInsertAtInOther - 1];
				else
					firstScrVerseCurr = m_scrVersesCurr[cluster.indexToInsertAtInOther];
			}

			// Choose the ScrVerses and ich's for possible use as reference points in the first subdiff
			ScrVerse refPointScrVerseCurr, refPointScrVerseRev;
			int ichCurr, ichRev;
			if (prevScrVerseCurr != null)
			{
				// normally the first subdiff indicates the end positions of the previous ScrVerse
				refPointScrVerseCurr = prevScrVerseCurr;
				ichCurr = prevScrVerseCurr.VerseStartIndex + prevScrVerseCurr.TextLength;
			}
			else
			{
				// sometime the first subdiff indicates the start of the first ScrVerse
				refPointScrVerseCurr = firstScrVerseCurr;
				ichCurr = 0;
			}
			if (prevScrVerseRev != null)
			{
				// normally the first subdiff indicates the end positions of the previous ScrVerse
				refPointScrVerseRev = prevScrVerseRev;
				ichRev = prevScrVerseRev.VerseStartIndex + prevScrVerseRev.TextLength;
			}
			else
			{
				// sometime the first subdiff indicates the start of the first ScrVerse
				refPointScrVerseRev = firstScrVerseRev;
				ichRev = 0;
			}

			// Finally, create the sub-diffs
			baseDiff.SubDiffsForParas = new List<Difference>(6); //capacity of 6 should be plenty

			// Create the 'reference points' subdiff IF the first cluster item is at beginning of a paragraph
			if ((cItemsInCurr > 0 && firstScrVerseCurr.VerseStartIndex == 0) ||
				(cItemsInRev > 0 && firstScrVerseRev.VerseStartIndex == 0))
			{
				baseDiff.SubDiffsForParas.Add(new Difference(
					refPointScrVerseCurr.HvoPara, ichCurr, ichCurr,
					refPointScrVerseRev.HvoPara, ichRev, ichRev,
					DifferenceType.NoDifference, null, null, null, null));
				// also fix up the baseDiff with paraNodeMaps of these same positions
				baseDiff.ParaNodeMapCurr = refPointScrVerseCurr.ParaNodeMap;
				baseDiff.ParaNodeMapRev = refPointScrVerseRev.ParaNodeMap;
			}

			// Add a subdifference piece for each (orphan) paragraph in the blob.
			for (int index = 0; index < Math.Max(cItemsInCurr, cItemsInRev); index++)
			{
				if (cItemsInCurr > 0)
				{
					// Process this paragraph in the Current
					Debug.Assert(index < cItemsInCurr);
					ScrVerse verseCurr = m_scrVersesCurr[cluster.itemsCurr[index].indexInOwner];

					if (index == 0 && firstScrVerseCurr.VerseStartIndex > 0)
					{
						// Create first subdiff for this Current ScrVerse, with reference point on the Revision side
						baseDiff.SubDiffsForParas.Add(new Difference(
							verseCurr.HvoPara, verseCurr.VerseStartIndex, verseCurr.VerseStartIndex + verseCurr.TextLength,
							refPointScrVerseRev.HvoPara, ichRev, ichRev,
							DifferenceType.TextDifference,
							null, null, null, null));
						// also fix up the baseDiff with paraNodeMaps of these same positions
						baseDiff.ParaNodeMapCurr = verseCurr.ParaNodeMap;
						baseDiff.ParaNodeMapRev = refPointScrVerseRev.ParaNodeMap;
					}
					else
					{
						DifferenceType diffType;
						if (cItemsInCurr > 1)
						{
							diffType = (verseCurr.IsStanzaBreak) ? DifferenceType.StanzaBreakAddedToCurrent :
								DifferenceType.ParagraphAddedToCurrent;
						}
						else
							diffType = DifferenceType.TextDifference;

						// Create one-sided subdiff for this Current ScrVerse, which starts a new paragraph
						baseDiff.SubDiffsForParas.Add(new Difference(
							verseCurr.HvoPara, 0, verseCurr.TextLength,
							0, 0, 0, diffType, null, null, null, null));
						Debug.Assert(verseCurr.VerseStartIndex == 0);
					}
				}

				else
				{
					// Process this paragraph in the Revision
					Debug.Assert(index < cItemsInRev);
					ScrVerse verseRev = m_scrVersesRev[cluster.itemsRev[index].indexInOwner];
					if (index == 0 && firstScrVerseRev.VerseStartIndex > 0)
					{
						// Create subdiff for this Revision ScrVerse, with reference point on the Current side
						baseDiff.SubDiffsForParas.Add(new Difference(
							refPointScrVerseCurr.HvoPara, ichCurr, ichCurr,
							verseRev.HvoPara, verseRev.VerseStartIndex, verseRev.VerseStartIndex + verseRev.TextLength,
							DifferenceType.TextDifference,
							null, null, null, null));
						// also fix up the baseDiff with paraNodeMaps of these same positions
						baseDiff.ParaNodeMapCurr = refPointScrVerseCurr.ParaNodeMap;
						baseDiff.ParaNodeMapRev = verseRev.ParaNodeMap;
					}
					else
					{
						DifferenceType diffType;
						if (cItemsInRev > 1)
						{
							diffType = (verseRev.IsStanzaBreak) ? DifferenceType.StanzaBreakMissingInCurrent :
								DifferenceType.ParagraphMissingInCurrent;
						}
						else
							diffType = DifferenceType.TextDifference;

						// Create one-sided subdiff for this Revision ScrVerse, which starts a new paragraph
						baseDiff.SubDiffsForParas.Add(new Difference(
							0, 0, 0, verseRev.HvoPara, 0, verseRev.TextLength,
							diffType, null, null, null, null));
						Debug.Assert(verseRev.VerseStartIndex == 0);
					}
				}
			}
			// ????
			//if last orphan piece in the Rev is followed by a ScrVerse in the same paragraph,
			// and if that following ScrVerse has a matching ScrVerse in the Current (i.e. in the following ScrVerse cluster)
			// connect that orphan to an IP there in the Current so it can be reverted into that Current parapraph.
			// may NOT need same for a last orphan piece in Curr, since it is simply deleted.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the given ScrVerse is the first ScrVerse in an StText (e.g. first in
		/// the section Contents).
		/// </summary>
		/// <param name="scrVerse">The given ScrVerse</param>
		/// <returns>true if it is the first ScrVerse in an StText; otherwise false</returns>
		/// ------------------------------------------------------------------------------------
		// REVIEW: move to ScrVerse object as a property?
		private bool FirstInStText(ScrVerse scrVerse)
		{
			if (scrVerse.ParaNodeMap.ParaIndex == 0 && scrVerse.VerseStartIndex == 0)
				return true;
			else
				return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the Subdiffs for this multi-para cluster. The pieces of the subDiffs will
		/// represent one paragraph each.
		/// (The first subdiff piece may be the last portion of an earlier paragraph,
		/// the last subdiff piece may be the first portion of a later paragraph.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateSubDiffsForClusterParas(Difference baseDiff,
			List<ScrVerse> scrVersesCur, List<ScrVerse> scrVersesRev,
			int matchedTextLenInStartParas, int matchedTextLenInEndParas,
			DifferenceType textCompareDiffType)
		{
			Debug.Assert(scrVersesCur.Count > 0 && scrVersesRev.Count > 0);

			StTxtPara startParaClstrCurr = new StTxtPara(m_cache, scrVersesCur[0].HvoPara);
			StTxtPara startParaClstrRev = new StTxtPara(m_cache, scrVersesRev[0].HvoPara);
			ScrSection sectionCurr = (ScrSection)((StText)startParaClstrCurr.Owner).Owner;
			ScrSection sectionRev = (ScrSection)((StText)startParaClstrRev.Owner).Owner;

			// get the starting and ending indices of the paragraphs in the cluster.
			int iMinParaClstrCurr = ((StTxtPara)(new StTxtPara(m_cache,
				scrVersesCur[0].HvoPara))).IndexInOwner;
			int iLimParaClstrCurr = ((StTxtPara)(new StTxtPara(m_cache,
				scrVersesCur[scrVersesCur.Count - 1].HvoPara))).IndexInOwner + 1;
			int iMinParaClstrRev = ((StTxtPara)(new StTxtPara(m_cache,
				scrVersesRev[0].HvoPara))).IndexInOwner;
			int iLimParaClstrRev = ((StTxtPara)(new StTxtPara(m_cache,
				scrVersesRev[scrVersesRev.Count - 1].HvoPara))).IndexInOwner + 1;

			// Finally, create the sub-diffs
			baseDiff.SubDiffsForParas = new List<Difference>(4); //capacity of 4 should be adequate

			// Add a subdifference piece for each paragraph in the blob.
			int iParaCurr = iMinParaClstrCurr;
			int iParaRev = iMinParaClstrRev;
			StTxtPara paraCurr = null;
			StTxtPara paraRev = null;
			StTxtPara lastParaCurr = null;
			StTxtPara lastParaRev = null;
			while (iParaCurr < iLimParaClstrCurr || iParaRev < iLimParaClstrRev)
			{
				// get the next set of paragraphs
				if (iParaCurr < iLimParaClstrCurr)
					paraCurr = (StTxtPara)sectionCurr.ContentOA.ParagraphsOS[iParaCurr];
				else
				{
					if (lastParaCurr == null)
						lastParaCurr = paraCurr;
					paraCurr = null; // reached end of current paras
				}
				if (iParaRev < iLimParaClstrRev)
					paraRev = (StTxtPara)sectionRev.ContentOA.ParagraphsOS[iParaRev];
				else
				{
					if (lastParaRev == null)
						lastParaRev = paraRev;
					paraRev = null; // reached end of revision paras
				}

				// calculate the hvo and ich numbers for this set of paragraphs
				bool fAtStartParas = (iParaCurr == iMinParaClstrCurr);
				bool fAtEndParaClstrCurr = (iParaCurr == iLimParaClstrCurr - 1);
				bool fAtEndParaClstrRev = (iParaRev == iLimParaClstrRev - 1);
				bool fAtOnlyParaCurr = fAtStartParas && fAtEndParaClstrCurr;
				bool fAtOnlyParaRev = fAtStartParas && fAtEndParaClstrRev;

				int hvoCurr = 0;
				int ichMinCurr = 0;
				int ichLimCurr = 0;
				if (paraCurr != null)
				{
					hvoCurr = paraCurr.Hvo;
					if (fAtOnlyParaCurr)
					{
						// the range starts after the matchedTextLenInStartParas
						ichMinCurr = scrVersesCur[0].VerseStartIndex +
							matchedTextLenInStartParas;
						// the range ends before the matchedTextLenInEndParas
						ichLimCurr = scrVersesCur[scrVersesCur.Count - 1].VerseStartIndex +
							scrVersesCur[scrVersesCur.Count - 1].TextLength -
							matchedTextLenInEndParas;
					}
					else if (fAtStartParas)
					{
						Debug.Assert(scrVersesCur[0].HvoPara == paraCurr.Hvo);
						// the range starts after the matchedTextLenInStartParas
						ichMinCurr = scrVersesCur[0].VerseStartIndex +
							matchedTextLenInStartParas;
						ichLimCurr = paraCurr.Contents.Length;
					}
					else if (fAtEndParaClstrCurr)
					{
						Debug.Assert(scrVersesCur[scrVersesCur.Count - 1].HvoPara == paraCurr.Hvo);
						ichMinCurr = 0;
						// the range ends before the matchedTextLenInEndParas
						ichLimCurr = scrVersesCur[scrVersesCur.Count - 1].VerseStartIndex +
							scrVersesCur[scrVersesCur.Count - 1].TextLength -
							matchedTextLenInEndParas;
					}
					else
					{
						ichMinCurr = 0;
						ichLimCurr = paraCurr.Contents.Length;
					}
				}

				int hvoRev = 0;
				int ichMinRev = 0;
				int ichLimRev = 0;
				if (paraRev != null)
				{
					hvoRev = paraRev.Hvo;
					if (fAtOnlyParaRev)
					{
						// the range starts after the matchedTextLenInStartParas
						ichMinRev = scrVersesRev[0].VerseStartIndex +
							matchedTextLenInStartParas;
						// the range ends before the matchedTextLenInEndParas
						ichLimRev = scrVersesRev[scrVersesRev.Count - 1].VerseStartIndex +
							scrVersesRev[scrVersesRev.Count - 1].TextLength -
							matchedTextLenInEndParas;
					}
					else if (fAtStartParas)
					{
						Debug.Assert(scrVersesRev[0].HvoPara == paraRev.Hvo);
						// the range starts after the matchedTextLenInStartParas
						ichMinRev = scrVersesRev[0].VerseStartIndex + matchedTextLenInStartParas;
						ichLimRev = paraRev.Contents.Length;
					}
					else if (fAtEndParaClstrRev)
					{
						Debug.Assert(scrVersesRev[scrVersesRev.Count - 1].HvoPara == paraRev.Hvo);
						ichMinRev = 0;
						// the range ends before the matchedTextLenInEndParas
						ichLimRev = scrVersesRev[scrVersesRev.Count - 1].VerseStartIndex +
							scrVersesRev[scrVersesRev.Count - 1].TextLength -
							matchedTextLenInEndParas;
					}
					else
					{
						ichMinRev = 0;
						ichLimRev = paraRev.Contents.Length;
					}
				}

				// We may have found a text difference between the concatenated strings.
				// However, there might not be an actual text difference for this subdifference so...
				// we calculate the find the maximum number of characters different in the
				// Current and Revision. If this maximum number is 0, there were no differences.
				int maxCharsDifferent = Math.Max(ichLimCurr - ichMinCurr, ichLimRev - ichMinRev);

				// Add a subdifference for this pair of paragraphs in the verse.
				baseDiff.SubDiffsForParas.Add(new Difference(
					hvoCurr, ichMinCurr, ichLimCurr,
					hvoRev, ichMinRev, ichLimRev,
					textCompareDiffType != DifferenceType.NoDifference && maxCharsDifferent > 0 ?
					textCompareDiffType : DifferenceType.NoDifference,
					null, null, null, null));
				// TODO TE-7036: para & char style parameters may need to be set when creating subdiff.

				// Compare the para styles for both current and revision paragraphs.
				// If we don't have a paragraph on either side, use the last valid one.
				StTxtPara compareParaCurr = (paraCurr != null) ? paraCurr : lastParaCurr;
				StTxtPara compareParaRev = (paraRev != null) ? paraRev : lastParaRev;
				Debug.Assert(compareParaCurr != null && compareParaRev != null);

				iParaCurr++;
				iParaRev++;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixes the multi-to-multi sub diffs.
		/// If we have different counts of paragraphs in the Curr and Rev, and more than one para
		/// in each, slide the early ending para info down to the last subdiff. We do this so
		/// that the revert will be able to maintain the original ending paragraph, and thus
		/// reduces required maintenance on the diff list.
		/// </summary>
		/// <param name="baseDiff">The base paragraph structure diff.</param>
		/// ------------------------------------------------------------------------------------
		private void FixMultiToMultiSubDiffs(Difference baseDiff)
		{
			// count the number of paras still referenced by the subdiffs
			//  (this count may have been reduced from the original paras in the cluster)
			int numCurrParas = 0;
			int numRevParas = 0;
			foreach (Difference subDiff in baseDiff.SubDiffsForParas)
			{
				if (subDiff.HvoCurr != 0)
					numCurrParas++;
				if (subDiff.HvoRev != 0)
					numRevParas++;
			}

			// In case the diff had been reduced from a multi-to-multi, update the type of difference
			if (baseDiff.DiffType == DifferenceType.ParagraphStructureChange)
			{
				if (numCurrParas == 1 && numRevParas > 1)
					baseDiff.DiffType = DifferenceType.ParagraphMergedInCurrent;
				else if (numCurrParas > 1 && numRevParas == 1)
					baseDiff.DiffType = DifferenceType.ParagraphSplitInCurrent;
			}

			if (numCurrParas > 1 && numRevParas > 1)
			{
				// We have a multi-multi paragraph cluster.
				int iLastSubDiff = baseDiff.SubDiffsForParas.Count - 1;

				if (numCurrParas < numRevParas)
				{
					// Move the last Current paragraph in the diff to the last subdifference.
					baseDiff.SubDiffsForParas[iLastSubDiff].HvoCurr = baseDiff.SubDiffsForParas[numCurrParas - 1].HvoCurr;
					baseDiff.SubDiffsForParas[iLastSubDiff].IchMinCurr = baseDiff.SubDiffsForParas[numCurrParas - 1].IchMinCurr;
					baseDiff.SubDiffsForParas[iLastSubDiff].IchLimCurr = baseDiff.SubDiffsForParas[numCurrParas - 1].IchLimCurr;
					baseDiff.SubDiffsForParas[numCurrParas - 1].HvoCurr = 0;
					baseDiff.SubDiffsForParas[numCurrParas - 1].IchMinCurr = 0;
					baseDiff.SubDiffsForParas[numCurrParas - 1].IchLimCurr = 0;
				}
				else if (numRevParas < numCurrParas)
				{
					// Move the last Revision paragraph in the diff to the last subdifference.
					baseDiff.SubDiffsForParas[iLastSubDiff].HvoRev = baseDiff.SubDiffsForParas[numRevParas - 1].HvoRev;
					baseDiff.SubDiffsForParas[iLastSubDiff].IchMinRev = baseDiff.SubDiffsForParas[numRevParas - 1].IchMinRev;
					baseDiff.SubDiffsForParas[iLastSubDiff].IchLimRev = baseDiff.SubDiffsForParas[numRevParas - 1].IchLimRev;
					baseDiff.SubDiffsForParas[numRevParas - 1].HvoRev = 0;
					baseDiff.SubDiffsForParas[numRevParas - 1].IchMinRev = 0;
					baseDiff.SubDiffsForParas[numRevParas - 1].IchLimRev = 0;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process detect differences for the first pair of ScrVerses in a ScrVerse cluster.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IncrementIndices ProcessFirstPairOfScrVerses()
		{
			// finalize any previous accumulation
			if (m_diff != null)
			{
				ClusterDiffs.Add(m_diff);
				m_diff = null;
			}

			// Check the para styles.
			CheckForParaStyleDifference();

			// if the end refs match...
			if (m_verseCurr.EndRef == m_verseRev.EndRef)
			{
				// the two ScrVerses are completely in synch- compare them!
				CompareTheScrVerses();
				return IncrementIndices.Both;
			}

			else // Verse bridge(s) have end numbers out of sync.
			{
				return ProcessWhenVerseBridgeEndOutOfSync();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare the ScrVerses, and generate a Difference object if differences are found.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CompareTheScrVerses()
		{
			// Compare the text and char styles of the two ScrVerses.
			int ichMin; // start offset in verse of difference
			int ichLimCurr, ichLimRev; // ending of difference in verses
			List<Difference> subDiffs = new List<Difference>();
			string sCharStyleNameCurr = null, sCharStyleNameRev = null;
			string sWsNameCurr = null, sWsNameRev = null;

			DifferenceType diffType = CompareVerseText(m_verseCurr.Text,
				m_verseRev.Text, out ichMin, out ichLimCurr,
				out ichLimRev, subDiffs, ref sCharStyleNameCurr, ref sCharStyleNameRev,
				ref sWsNameCurr, ref sWsNameRev);
			if (diffType != DifferenceType.NoDifference)
			{
				// save this difference
				ClusterDiffs.Add(new Difference(m_verseCurr.StartRef, m_verseCurr.EndRef,
					m_verseCurr.HvoPara, m_verseCurr.VerseStartIndex + ichMin,
					m_verseCurr.VerseStartIndex + ichLimCurr,
					m_verseRev.HvoPara, m_verseRev.VerseStartIndex + ichMin, m_verseRev.VerseStartIndex + ichLimRev,
					diffType, subDiffs, null, sCharStyleNameCurr, sCharStyleNameRev,
					sWsNameCurr, sWsNameRev, m_verseCurr.ParaNodeMap, m_verseRev.ParaNodeMap));
			}

			// if appropriate, keep a record of the comparison (regardless of if a Diff was
			// found or not)
			if (m_verseComparisons != null)
				m_verseComparisons.Add(new Comparison(m_verseCurr, m_verseRev));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare the paragraph styles of the ScrVerses, if either paragraph is a new one,
		/// and create a ParagraphStyleDifference when appropriate.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CheckForParaStyleDifference()
		{
			// If either paragraph is a new one...
			if (m_verseCurr.HvoPara != m_hvoPrevParaCurr || m_verseRev.HvoPara != m_hvoPrevParaRev)
			{
				// Finalize any previous accumulation
				if (m_diff != null)
				{
					ClusterDiffs.Add(m_diff);
					m_diff = null;
				}

				// Compare the paragraph styles
				ScrTxtPara paraCurr = new ScrTxtPara(m_cache, m_verseCurr.HvoPara);
				ScrTxtPara paraRev = new ScrTxtPara(m_cache, m_verseRev.HvoPara);

				// Notes in case StyleRules has gotten corrupted:
				// If we have StyleRules for the Revision, we can go ahead and make a difference even
				// if the Current StyleRules is null. However, if the Revision StyleRules are null,
				// we should not create a difference because reverting it could corrupt the Current.
				// We only report a difference if both paragraphs have some contents.
				if (paraRev.Contents.Length > 0 && paraCurr.Contents.Length > 0 &&
					paraRev.StyleRules != null && !paraRev.StyleRules.Equals(paraCurr.StyleRules))
				{
					// save this paragraph style difference
					// note: it seems that ref info is not terribly important for para style diffs
					// so for now we will enter relevant info from the Current ScrVerse
					ClusterDiffs.Add(
						new Difference(m_verseCurr.StartRef, m_verseCurr.EndRef, DifferenceType.ParagraphStyleDifference,
						paraCurr.Hvo, 0, paraCurr.Contents.Length,
						paraRev.Hvo, 0, paraRev.Contents.Length,
						m_verseCurr.ParaNodeMap, m_verseRev.ParaNodeMap));
				}

				// remember which paragraphs we just checked
				m_hvoPrevParaCurr = m_verseCurr.HvoPara;
				m_hvoPrevParaRev = m_verseRev.HvoPara;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process detect differences in a ScrVerse cluster with a single ScrVerse added or missing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IncrementIndices ProcessSingleAddedMissingScrVerse()
		{
			if (m_verseRev == null)
			{
				// Else if no more revision verses, accumulate remaining curr verses as "missing in revision"
				CreateOrAccumMissingInRevision();
			}
			else if (m_verseCurr == null)
			{
				// Else if no more curr verses, accumulate remaining revision verses as "missing in current"
				CreateOrAccumMissingInCurrent();
			}
			else
				Debug.Assert(false); // we should be at the end of one book revision, but it appears not!

			// Advance the Current/Revsion ScrVerse that wasn't at the end
			if (m_verseRev == null)
				return IncrementIndices.Current;

			//m_verseCurr == null
			return IncrementIndices.Revision;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process detect differences in book when we find that the two ScrVerses have matching
		/// start refs, but they are verse bridge(s) which have end numbers out of sync.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IncrementIndices ProcessWhenVerseBridgeEndOutOfSync()
		{
			// Create a new text diff, for possible accumulation
			m_diff = new Difference(m_verseCurr, m_verseRev, DifferenceType.TextDifference);

			// Advance the verse iterator which has the smaller end ref
			//  so that we can perhaps accumulate until we get a matching end ref
			if (m_verseCurr.EndRef < m_verseRev.EndRef)
				return IncrementIndices.Current;
			return IncrementIndices.Revision;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function handles processing when there is no matching ScrVerse in the
		/// Current, so we need a "Missing In Current" diff.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateOrAccumMissingInCurrent()
		{
			// If the previous accumulation is a "Missing in Current" type, in the same para,
			//  then add to it
			if (m_diff != null && m_diff.HvoRev == m_verseRev.HvoPara &&
				(m_diff.DiffType & DifferenceType.VerseMissingInCurrent) != 0)
			{
				m_diff.Accumulate(null, m_verseRev);  // accumulate within this para

				// If the accumulation causes the entire paragraph to be included, then
				// change the type to ParagraphMissingInCurrent
				if (m_diff.IchMinRev == 0)
				{
					int paraLength =
						new StTxtPara(m_cache, m_verseRev.HvoPara).Contents.Length;
					if (m_diff.IchLimRev == paraLength)
						m_diff.DiffType = DifferenceType.ParagraphMissingInCurrent;
				}
			}
			else
			{
				// Finalize any previous accumulation
				if (m_diff != null)
					ClusterDiffs.Add(m_diff);

				// Determine the type of difference to add.
				DifferenceType diffType;
				if (!m_verseRev.IsCompleteParagraph && m_verseRev.Text != null)
					diffType = DifferenceType.VerseMissingInCurrent;
				else if (m_verseRev.IsStanzaBreak)
					diffType = DifferenceType.StanzaBreakMissingInCurrent;
				else
					diffType = DifferenceType.ParagraphMissingInCurrent;

				if (diffType == DifferenceType.VerseMissingInCurrent ||
					diffType == DifferenceType.StanzaBreakMissingInCurrent ||
					(m_verseRev.Text != null && !string.IsNullOrEmpty(m_verseRev.Text.Text)))
				{
					// Create a new diff, for possible accumulation
					m_diff = new Difference(m_endOfPrevVerseCurr, m_verseRev, diffType);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function handles processing when there is no matching ScrVerse in the
		/// Revision, so we need a "Missing In Revision" diff.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateOrAccumMissingInRevision()
		{
			// If the previous accumulation is a "Missing in Revision" type, in the same para,
			//  then add to it
			if (m_diff != null && m_diff.HvoCurr == m_verseCurr.HvoPara &&
				(m_diff.DiffType & DifferenceType.VerseAddedToCurrent) != 0)
			{
				m_diff.Accumulate(m_verseCurr, null); // accumulate within this para

				// If the accumulation causes the entire paragraph to be included, then
				// change the type to ParagraphAddedToCurrent
				if (m_diff.IchMinCurr == 0)
				{
					int paraLength =
						new StTxtPara(m_cache, m_verseCurr.HvoPara).Contents.Length;
					if (m_diff.IchLimCurr == paraLength)
						m_diff.DiffType = DifferenceType.ParagraphAddedToCurrent;
				}
			}
			else
			{
				// Finalize any previous accumulation
				if (m_diff != null)
					ClusterDiffs.Add(m_diff);

				// Determine the type of difference to add.
				DifferenceType diffType;
				if (!m_verseCurr.IsCompleteParagraph && m_verseCurr.Text != null)
					diffType = DifferenceType.VerseAddedToCurrent;
				else if (m_verseCurr.IsStanzaBreak)
					diffType = DifferenceType.StanzaBreakAddedToCurrent;
				else
					diffType = DifferenceType.ParagraphAddedToCurrent;

				if (diffType == DifferenceType.VerseAddedToCurrent ||
					diffType == DifferenceType.StanzaBreakAddedToCurrent ||
					(m_verseCurr.Text != null && !string.IsNullOrEmpty(m_verseCurr.Text.Text)))
				{
					// Always add the diff if it is a VerseAdded or StanzaBreakAdded.
					// Only add a ParagraphAdded for non-empty paragraphs.
					// Create a new diff, for possible accumulation
					m_diff = new Difference(m_verseCurr, m_endOfPrevVerseRev, diffType);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function handles processing when we have matching or partially matching
		/// ScrVerses in the Current and Revision, so we need a "Text Difference" diff.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateOrAccumTextDifference()
		{
			// If the previous accumulation is a "Text Difference" type, in the same paragraphs (hvo),
			//  then add to it.
			// (if either ScrVerse is at the end (==null), we still need to add the other ScrVerse
			// to the accumulation)
			if (m_diff != null && (m_diff.DiffType & DifferenceType.TextDifference) != 0 &&
				(m_verseCurr == null || (m_verseCurr != null && m_diff.HvoCurr == m_verseCurr.HvoPara)) &&
				(m_verseRev == null || (m_verseRev != null && m_diff.HvoRev == m_verseRev.HvoPara)))
			{
				m_diff.Accumulate(m_verseCurr, m_verseRev);
			}
			else
			{
				// Finalize any previous accumulation
				if (m_diff != null)
					ClusterDiffs.Add(m_diff);

				// Create a new diff, for possible accumulation
				m_diff = new Difference(m_verseCurr, m_verseRev, DifferenceType.TextDifference);
			}
		}
		#endregion

		#region Detect Differences: By Verse - Paragraph Start at Verse Boundary
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Detects paragraphs that are split or merged at a verse boundary.
		/// Adds these differences to the ClusterDiffs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DetectParaSplitMergeAtVerseBoundaries()
		{
			// Walk through the revision and current ScrVerses to compare paragraph structure.
			ScrVerse verseCurr;
			ScrVerse verseRev;
			int verseCurrIndex = 0;
			int verseRevIndex = 0;
			while (verseCurrIndex < m_scrVersesCurr.Count || verseRevIndex < m_scrVersesRev.Count)
			{
				// Get the ScrVerse at the current index locations.
				verseCurr = (verseCurrIndex < m_scrVersesCurr.Count) ?
					m_scrVersesCurr[verseCurrIndex] : null;
				verseRev = (verseRevIndex < m_scrVersesRev.Count) ?
					m_scrVersesRev[verseRevIndex] : null;
				// and the previous ScrVerses, too.
				ScrVerse prevVerseCurr = (verseCurrIndex > 0) ? m_scrVersesCurr[verseCurrIndex - 1] : null;
				ScrVerse prevVerseRev = (verseRevIndex > 0) ? m_scrVersesRev[verseRevIndex - 1] : null;

				if (verseCurr != null && verseRev != null &&
					prevVerseCurr != null && prevVerseRev != null)
				{
					// If we're comparing the same verse reference in the current and revision...
					if (verseCurr.StartRef == verseRev.StartRef)
					{
						ScrTxtPara prevParaCurr = FindParagraph(prevVerseCurr.HvoPara);
						ScrTxtPara prevParaRev = FindParagraph(prevVerseRev.HvoPara);

						Debug.Assert(prevParaCurr != null);
						Debug.Assert(prevParaRev != null);

						// If there is a verse boundary paragraph split in the current...
						if (FindVerseBoundaryParaBreak(m_scrVersesCurr, verseCurrIndex,
							m_scrVersesRev, verseRevIndex))
						{
							// Add a root diff
							Difference baseDiff = new Difference(prevVerseCurr.EndRef, prevVerseCurr.EndRef,
								DifferenceType.ParagraphSplitInCurrent,
								prevVerseCurr.HvoPara, prevParaCurr.Contents.Length, prevParaCurr.Contents.Length,
								verseRev.HvoPara, verseRev.VerseStartIndex, verseRev.VerseStartIndex,
								prevVerseCurr.ParaNodeMap, verseRev.ParaNodeMap);
							// and subdiffs.
							baseDiff.SubDiffsForParas = new List<Difference>(2);
							// Add a subdifference for the first Current para and the Revision para.
							baseDiff.SubDiffsForParas.Add(new Difference(
								baseDiff.HvoCurr, baseDiff.IchMinCurr, baseDiff.IchLimCurr,
								baseDiff.HvoRev, baseDiff.IchMinRev, baseDiff.IchLimRev,
								DifferenceType.NoDifference, null, null, null, null));
							// Add a subdifference for the Current para following the split and null for the Revision.
							baseDiff.SubDiffsForParas.Add(new Difference(
								verseCurr.HvoPara, 0, 0, 0, 0, 0,
								DifferenceType.NoDifference, null, null, null, null));
							ClusterDiffs.Add(baseDiff);
						}

						// If there is a verse boundary paragraph merge in the current...
						else if (FindVerseBoundaryParaBreak(m_scrVersesRev, verseRevIndex,
							m_scrVersesCurr, verseCurrIndex))
						{
							// Add a root diff
							Difference baseDiff = new Difference(prevVerseCurr.EndRef, prevVerseCurr.EndRef,
								DifferenceType.ParagraphMergedInCurrent,
								verseCurr.HvoPara, verseCurr.VerseStartIndex, verseCurr.VerseStartIndex,
								prevVerseRev.HvoPara, prevParaRev.Contents.Length, prevParaRev.Contents.Length,
								verseCurr.ParaNodeMap, prevVerseRev.ParaNodeMap);
							// and subdiffs.
							baseDiff.SubDiffsForParas = new List<Difference>(2);
							// Add a subdifference for the first Current para and the Revision para.
							baseDiff.SubDiffsForParas.Add(new Difference(
								baseDiff.HvoCurr, baseDiff.IchMinCurr, baseDiff.IchLimCurr,
								baseDiff.HvoRev, baseDiff.IchMinRev, baseDiff.IchLimRev,
								DifferenceType.NoDifference, null, null, null, null));
							// Add a subdifference for the Revision para following the split and null for the Current.
							baseDiff.SubDiffsForParas.Add(new Difference(
								0, 0, 0, verseRev.HvoPara, 0, 0,
								DifferenceType.NoDifference, null, null, null, null));
							ClusterDiffs.Add(baseDiff);
						}
					}
				}

				AdvanceScrVerseIndices(verseCurr, verseRev, ref verseCurrIndex, ref verseRevIndex);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds a para break at the verse boundary, if any.
		/// </summary>
		/// <param name="versesSplit">The list of verses that we are evaluating for a split.</param>
		/// <param name="verseSplitIndex">Index of the current verse in versesSplit.</param>
		/// <param name="versesMerged">The list of verses that we are evaluating for a merge.</param>
		/// <param name="verseMergedIndex">Index of the current verse in versesMerged.</param>
		/// <returns><c>true</c> if a paragraph split is found at the beginning of a verse
		/// boundary;<c>false</c> otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		private bool FindVerseBoundaryParaBreak(List<ScrVerse> versesSplit, int verseSplitIndex,
			List<ScrVerse> versesMerged, int verseMergedIndex)
		{
			if (versesSplit[verseSplitIndex].VerseStartIndex != 0)
				return false; // verse does not begin a para, so it is not the beginning of a para split
			if (versesMerged[verseMergedIndex].VerseStartIndex == 0)
				return false; // the other verse also begins a para, so it is not a verse boundary split
			if (versesSplit[verseSplitIndex].VerseStartIndex == versesSplit[verseSplitIndex].TextStartIndex)
				return false; // the verse does not start with a verse number, so the para does not begin on a verse boundary

			// Now scan backward until we find a ScrVerse (in the ScrVerses we are evaluating for a split)
			//  with the same reference as a ScrVerse in the ScrVerses that we are evaluating for a merge.
			int splitParaIndex = versesSplit[verseSplitIndex].ParaNodeMap.ParaIndex;
			int splitSectionIndex = versesSplit[verseSplitIndex].ParaNodeMap.SectionIndex;
			int mergedParaIndex = versesMerged[verseMergedIndex].ParaNodeMap.ParaIndex;

			for (int iVerseSplit = --verseSplitIndex, iVerseMerged = --verseMergedIndex;
				iVerseSplit >= 0 && iVerseMerged >= 0; )
			{
				// If we cross a paragraph boundary on the side we are evaluating for a merge...
				if (versesMerged[iVerseMerged].ParaNodeMap.ParaIndex != mergedParaIndex)
					return false; // we are finished searching for a matching prior verse.

				// NOTE: Our clusters do not cross section boundaries now, so we don't consider
				// paragraphs in prior sections.
				// If we cross a section boundary on the side we are evaluating for a split...
				if (versesSplit[iVerseSplit].ParaNodeMap.SectionIndex != splitSectionIndex)
					return false; // we are finished searching for a matching verse in prior para.

				// If there is a common reference in a PRIOR para on the side we are evaluating for a split
				// and in the SAME para on the side we are evaluating for a merge...
				if (IsRefOverlap(versesSplit[iVerseSplit], versesMerged[iVerseMerged]))
					return true; // we found a verse boundary paragraph split.

				// Attempt to keep the verse comparison in sync by reference
				if (versesSplit[iVerseSplit].StartRef > versesMerged[iVerseMerged].StartRef)
					iVerseSplit--;
				else if (versesSplit[iVerseSplit].StartRef < versesMerged[iVerseMerged].StartRef)
					iVerseMerged--;
				else
				{
					iVerseSplit--;
					iVerseMerged--;
				}
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Advances the indices in the list of ScrVerses for the current StText. The indices
		/// will be advanced, attempting to keep the current and revision StTexts in synch by
		/// verse number.
		/// </summary>
		/// <param name="verseCurr">The current verse in the current version.</param>
		/// <param name="verseRev">The current verse in the revision.</param>
		/// <param name="verseCurrIndex">Index of the verse curr.</param>
		/// <param name="verseRevIndex">Index of the verse rev.</param>
		/// ------------------------------------------------------------------------------------
		private void AdvanceScrVerseIndices(ScrVerse verseCurr, ScrVerse verseRev,
			ref int verseCurrIndex, ref int verseRevIndex)
		{
			IncrementIndices incrementType = IncrementIndices.None;
			// if either the current or revision has reached the end of the book,
			// determine how to increment the current and/or revision verse.
			if (verseCurr == null || verseRev == null)
			{
				// Advance the Current/Revsion ScrVerse that wasn't at the end
				if (verseRev == null && verseCurr != null)
					incrementType = IncrementIndices.Current;
				else if (verseRev != null)
					incrementType = IncrementIndices.Revision;
			}

			// else Process if the verse start refs match - a typical comparison
			else if (verseCurr.StartRef == verseRev.StartRef)
			{
				bool fCurrentHasFollowingSegment = (verseCurrIndex + 1 < m_scrVersesCurr.Count) ?
					(m_scrVersesCurr[verseCurrIndex + 1].StartRef == verseCurr.StartRef) : false;
				bool fRevisionHasFollowingSegment = (verseRevIndex + 1 < m_scrVersesRev.Count) ?
					(m_scrVersesRev[verseRevIndex + 1].StartRef == verseRev.StartRef) : false;

				if (fCurrentHasFollowingSegment && !fRevisionHasFollowingSegment)
					incrementType = IncrementIndices.Current;
				else if (!fCurrentHasFollowingSegment && fRevisionHasFollowingSegment)
					incrementType = IncrementIndices.Revision;
				else
					incrementType = IncrementIndices.Both;
			}
			else
			{
				// Verse start refs are out of sync.
				// Advance the ScrVerse that has the smallest end ref
				if (verseCurr.EndRef > verseRev.EndRef)
					incrementType = IncrementIndices.Revision;
				else if (verseCurr.EndRef < verseRev.EndRef)
					incrementType = IncrementIndices.Current;
				else
					incrementType = IncrementIndices.Both;
			}

			// Advance indices to the next set of verses
			if (incrementType == IncrementIndices.Current || incrementType == IncrementIndices.Both)
			{
				//m_endOfPrevVerseCurr = MakeEndOfPreviousVerse(verseCurr);
				verseCurrIndex++;
			}
			if (incrementType == IncrementIndices.Revision || incrementType == IncrementIndices.Both)
			{
				//m_endOfPrevVerseRev = MakeEndOfPreviousVerse(verseRev);
				verseRevIndex++;
			}
		}
		#endregion

		#region  Detect Differences: By Paragraph (Title, Section Head, Intro)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine all the differences in the given StTexts, by paragraph.
		/// This method is similar to the workhorse function DetectDifferencesInStTexts(), but
		/// this one is specialized for StTexts that don't have verse numbers
		/// (title, section head, or intro section contents).
		/// This method gathers paragraph text correlation info, iterates through the paragraphs
		/// accordingly, compares them, and then appends the differences found to the master
		/// list of diffs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DetectDifferencesInStTexts_ByParagraph(IStText textCurr, IStText textRev,
			BCVRef bcvRef)
		{
			// there should not be a case of an StText with no paragraphs. However, this
			// is just-in-case code to prevent failure.
			Debug.Assert(textCurr.ParagraphsOS.Count > 0);
			Debug.Assert(textRev.ParagraphsOS.Count > 0);
			if (textCurr.ParagraphsOS.Count == 0)
				textCurr.ParagraphsOS.Append(new StTxtPara());
			if (textRev.ParagraphsOS.Count == 0)
				textRev.ParagraphsOS.Append(new StTxtPara());

			// Get paragraph correlation information about the current and revision StTexts
			List<ParaCorrelationInfo> paraCorrelationList = DetermineAllParagraphCorrelations(textCurr, textRev);

			int iParaCurr = 0;
			int iParaRev = 0;
			int iCorrelation = 0;
			int cParasCurr = textCurr.ParagraphsOS.Count;
			int cParasRev = textRev.ParagraphsOS.Count;

			// Go through all of the paragraphs generating differences for missing paras and
			// text differences for correlated paragraphs.
			while (iParaCurr < cParasCurr || iParaRev < cParasRev)
			{
				// get the ParaCorrelationInfo at iCorrelation
				ParaCorrelationInfo pci = null; //if null, all remaining paras are unmatched
				if (iCorrelation < paraCorrelationList.Count)
					pci = paraCorrelationList[iCorrelation];

				// Generate AddedToCurrent diffs for all current paras before the correlation and
				// MissingInCurrent diffs for all rev paras before the correlation.
				int iLimCurr = (pci == null) ? cParasCurr : pci.iParaCurr;
				int iLimRev = (pci == null) ? cParasRev : pci.iParaRev;
				ProcessUnmatchedParas(bcvRef, textCurr, textRev, iParaCurr, iLimCurr, iParaRev, iLimRev);
				iParaCurr = iLimCurr;
				iParaRev = iLimRev;

				// Now compare the correlated paragraphs
				if (pci != null)
				{
					// At this point, we are ready to process two paragraphs with correlated text.
					StTxtPara paraCurr = (StTxtPara)textCurr.ParagraphsOS[iParaCurr];
					Debug.Assert( paraCurr != null, "See TE-3173");
					int nLengthCurr = paraCurr.Contents.Length;
					ParaNodeMap mapCurr = new ParaNodeMap(paraCurr);
					StTxtPara paraRev = (StTxtPara)textRev.ParagraphsOS[iParaRev];
					Debug.Assert( paraRev != null, "See TE-3173" );
					int nLengthRev = paraRev.Contents.Length;
					ParaNodeMap mapRev = new ParaNodeMap(paraRev);

					// Compare paragraph styles first
					Debug.Assert(paraCurr != null && paraRev != null, "See TE-3173: how did this happen?");
					Debug.Assert(paraCurr.StyleRules != null);
					Debug.Assert(paraRev.StyleRules != null);
					if (paraCurr.StyleRules == null || !paraCurr.StyleRules.Equals(paraRev.StyleRules))
					{
						// save this paragraph style difference
						ClusterDiffs.Add(
							new Difference(bcvRef, bcvRef, DifferenceType.ParagraphStyleDifference,
							paraCurr.Hvo, 0, nLengthCurr,
							paraRev.Hvo, 0, nLengthRev,
							mapCurr, mapRev));
					}

					// Compare paragraph text, and generate a Difference object if differences are found
					int ichMin; // start offset of difference in paragraphs
					int ichLimCurr, ichLimRev; // ending of difference in paragraphs
					List<Difference> subDiffs = new List<Difference>();
					string sCharStyleNameCurr = null, sCharStyleNameRev = null;
					string sWsNameCurr = null, sWsNameRev = null;
					DifferenceType diffType = CompareVerseText(paraCurr.Contents.UnderlyingTsString,
						paraRev.Contents.UnderlyingTsString, out ichMin, out ichLimCurr, out ichLimRev,
						subDiffs, ref sCharStyleNameCurr, ref sCharStyleNameRev,
						ref sWsNameCurr, ref sWsNameRev);
					if (diffType != DifferenceType.NoDifference)
					{
						Debug.Assert(paraCurr != null && paraRev != null, "See TE-3173: how did this happen?");
						// save this difference
						ClusterDiffs.Add(new Difference(bcvRef, bcvRef,
							paraCurr.Hvo, ichMin, ichLimCurr,
							paraRev.Hvo, ichMin, ichLimRev, diffType,
							subDiffs, null, sCharStyleNameCurr, sCharStyleNameRev,
							sWsNameCurr, sWsNameRev, mapCurr, mapRev));
					}

					// advance for the next correlation
					iParaCurr++;
					iParaRev++;
					iCorrelation++;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process the given range of paragraphs in the current and revision StTexts, to
		/// add "Paragraph Added" and "Paragraph Missing" diffs to the master list of diffs.
		/// </summary>
		/// <param name="bcvRef">Scripture reference for these paragraphs</param>
		/// <param name="textCurr"></param>
		/// <param name="textRev"></param>
		/// <param name="iMinCurr">index of first para to process in textCurr</param>
		/// <param name="iLimCurr">lim of para indexes to process in textCurr</param>
		/// <param name="iMinRev"></param>
		/// <param name="iLimRev"></param>
		/// ------------------------------------------------------------------------------------
		private void ProcessUnmatchedParas(BCVRef bcvRef, IStText textCurr, IStText textRev,
			int iMinCurr, int iLimCurr,
			int iMinRev, int iLimRev)
		{
			StTxtPara paraCurr = null;
			StTxtPara paraRev = null;

			// determine the paragraph and position in revision at which unmatched para diffs
			// should point
			int ichRev = 0;
			if (iMinCurr < iLimCurr)
			{
				int hvoRev;
				GetUnmatchedParaPos(textRev, iLimRev, out hvoRev, out ichRev);
				paraRev = new StTxtPara(m_cache, hvoRev);
			}

			// All of the unmatched paragraphs in current are AddedToCurrent diffs.
			for (int iCurr = iMinCurr; iCurr < iLimCurr; iCurr++)
			{
				paraCurr = (StTxtPara)textCurr.ParagraphsOS[iCurr];
				Debug.Assert(paraCurr != null, "See TE-3173");
				int nLengthCurr = paraCurr.Contents.Length;

				// Create a difference for an inserted "current" paragraph.
				ClusterDiffs.Add(
					new Difference(bcvRef, bcvRef, IsStanzaBreak(paraCurr) ?
						DifferenceType.StanzaBreakAddedToCurrent : DifferenceType.ParagraphAddedToCurrent,
						paraCurr.Hvo, 0, nLengthCurr,
						paraRev.Hvo, ichRev, ichRev,
						new ParaNodeMap(paraCurr), new ParaNodeMap(paraRev)));
			}

			// determine the paragraph and position in current at which unmatched para diffs
			// should point
			int ichCurr = 0;
			if (iMinRev < iLimRev)
			{
				int hvoCurr;
				GetUnmatchedParaPos(textCurr, iLimCurr, out hvoCurr, out ichCurr);
				paraCurr = new StTxtPara(m_cache, hvoCurr);
			}

			// All of the unmatched paragraphs in revision are MissingInCurrent diffs.
			for (int iRev = iMinRev; iRev < iLimRev; iRev++)
			{
				paraRev = (StTxtPara)textRev.ParagraphsOS[iRev];
				Debug.Assert(paraRev != null, "See TE-3173");
				int nLengthRev = paraRev.Contents.Length;

				ClusterDiffs.Add(
					new Difference(bcvRef, bcvRef,
					IsStanzaBreak(paraCurr) ? DifferenceType.StanzaBreakMissingInCurrent :
						DifferenceType.ParagraphMissingInCurrent,
					paraCurr.Hvo, ichCurr, ichCurr,
					paraRev.Hvo, 0, nLengthRev,
					new ParaNodeMap(paraCurr), new ParaNodeMap(paraRev)));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified para is an empty stanza break.
		/// </summary>
		/// <param name="para">The specified para.</param>
		/// <returns>
		/// 	<c>true</c> if the specified para is an empty stanza break; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool IsStanzaBreak(StTxtPara para)
		{
			return string.Equals(
				ScrStyleNames.StanzaBreak, ScrStyleNames.GetStyleName(para.Hvo, m_cache)) &&
				para.Contents.Length == 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine the ending paragraph HVO and position for the given StText and para index Lim,
		/// at which an unmatched (added/missing) paragraph difference could point.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="iParaLim"></param>
		/// <param name="matchingParaHvo"></param>
		/// <param name="matchingParaIch"></param>
		/// ------------------------------------------------------------------------------------
		private void GetUnmatchedParaPos(IStText text, int iParaLim, out int matchingParaHvo,
			out int matchingParaIch)
		{
			// If the lim is a valid paragraph, then match to the start of that paragraph
			if (iParaLim < text.ParagraphsOS.Count)
			{
				matchingParaHvo = text.ParagraphsOS.HvoArray[iParaLim];
				matchingParaIch = 0;
			}
			else
			{
				// otherwise, move back one paragraph and match to the end of it
				IStTxtPara para = (IStTxtPara)text.ParagraphsOS[iParaLim - 1];
				matchingParaHvo = para.Hvo;
				matchingParaIch = para.Contents.Length;
			}
		}
		#endregion

		#region Detect Differences: Determine Paragraph/String Correlations
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build a list of paragraph correlations that will be used to generate differences
		/// </summary>
		/// <param name="textCurr">the StText of the current book</param>
		/// <param name="textRev">the StText of the revision book</param>
		/// <returns>a list of ParaCorrelationInfo objects - one for each correlation</returns>
		/// ------------------------------------------------------------------------------------
		private List<ParaCorrelationInfo> DetermineAllParagraphCorrelations(IStText textCurr, IStText textRev)
		{
			// from the paragraphs of the two given StTexts, build two array lists of ITsStrings
			List<string> stringsCurr = new List<string>(textCurr.ParagraphsOS.Count);
			List<string> stringsRev = new List<string>(textRev.ParagraphsOS.Count);
			foreach (StTxtPara para in textCurr.ParagraphsOS)
				stringsCurr.Add(para.Contents.Text);
			foreach (StTxtPara para in textRev.ParagraphsOS)
				stringsRev.Add(para.Contents.Text);

			return DetermineAllStringCorrelations(stringsCurr, stringsRev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build a list of string correlations that will be used to generate differences
		/// </summary>
		/// <param name="stringsCurr">current set of strings</param>
		/// <param name="stringsRev">revision set of strings</param>
		/// <returns>a list of ParaCorrelationInfo objects - one for each correlation</returns>
		/// ------------------------------------------------------------------------------------
		private List<ParaCorrelationInfo> DetermineAllStringCorrelations(List<string> stringsCurr,
			List<string> stringsRev)
		{
			List<ParaCorrelationInfo> correlationList = new List<ParaCorrelationInfo>();

			// Build a map of correlation factors between all of the current and revision
			// paragraphs.
			double[,] factors = BuildCorrelationFactorMap(stringsCurr, stringsRev);

			// Look for the largest correlation factors in the map and create paragraph
			// correlations for them.
			ParaCorrelationInfo corrInfo;
			while (FindLargestRemainingFactor(factors, out corrInfo))
				correlationList.Add(corrInfo);

			// Sort the correlation list.
			correlationList.Sort(new ParaCorrelationInfoSorter());

			// remove cross-over correlations from the list
			while (CrossoversCount(correlationList) != 0)
				RemoveLargestCrossover(correlationList);

			return correlationList;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Look through a list of paragraph correlations and count the number of times each
		/// correlation crosses over any other correlation.
		/// </summary>
		/// <param name="correlationList"></param>
		/// <returns>the number of crossovers encountered</returns>
		/// ------------------------------------------------------------------------------------
		private int CrossoversCount(List<ParaCorrelationInfo> correlationList)
		{
			int count = 0;

			// zero out all of the crossover counts
			foreach (ParaCorrelationInfo info in correlationList)
				info.crossoverCount = 0;

			// look for crossovers and count them
			for (int i = 0; i < correlationList.Count - 1; i++)
			{
				ParaCorrelationInfo info_i = correlationList[i];
				for (int j = i + 1; j < correlationList.Count; j++)
				{
					ParaCorrelationInfo info_j = correlationList[j];
					if (info_j.iParaRev < info_i.iParaRev)
					{
						// we found a crossover, count it in both correlations and update
						// the total count
						info_j.crossoverCount++;
						info_i.crossoverCount++;
						count++;
					}
				}
			}

			return count;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove the correlation from the list that has the largest crossover count.
		/// </summary>
		/// <param name="correlationList"></param>
		/// ------------------------------------------------------------------------------------
		private void RemoveLargestCrossover(List<ParaCorrelationInfo> correlationList)
		{
			ParaCorrelationInfo largestInfo = null;
			int largestCount = -1;

			// Look for the info with the largest correlation count.
			foreach (ParaCorrelationInfo info in correlationList)
			{
				// If an item has an equal count to a previous one, then use the later one.
				// This will favor the first one which will be one in the "current".
				if (info.crossoverCount >= largestCount)
				{
					largestInfo = info;
					largestCount = info.crossoverCount;
				}
			}

			// remove the item with the largest count from the list
			Debug.Assert(largestInfo != null);
			if (largestInfo != null)
				correlationList.Remove(largestInfo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the largest remaining factor in the factor map. Once a factor is returned,
		/// all of the factors in the same row and column will be removed to prevent duplicate
		/// correlations. Once all of the factors are below a threshold value, no more factors
		/// are returned.
		/// </summary>
		/// <param name="factors">2-dimensional correlation factor map</param>
		/// <param name="corrInfo">paragraph correlation info object returned</param>
		/// <returns>true if a factor was found, else false.</returns>
		/// ------------------------------------------------------------------------------------
		private bool FindLargestRemainingFactor(double[,] factors, out ParaCorrelationInfo corrInfo)
		{
			int largestI = -1;
			int largestJ = -1;
			double largestFactor = .2;	// start at a threshold value

			for (int i = 0; i <= factors.GetUpperBound(0); i++)
			{
				for (int j = 0; j <= factors.GetUpperBound(1); j++)
				{
					double factor = factors[i, j];
					if (factor > largestFactor)
					{
						largestI = i;
						largestJ = j;
						largestFactor = factor;
					}
				}
			}

			// If there are no more large factors, then fail.
			if (largestI == -1)
			{
				corrInfo = null;
				return false;
			}

			// Build a correlation info object to return
			corrInfo = new ParaCorrelationInfo(largestI, largestJ, largestFactor);

			// Remove the row and column information for where we found the largest factor.
			for (int j = 0; j <= factors.GetUpperBound(1); j++)
				factors[largestI, j] = 0.0;
			for (int i = 0; i <= factors.GetUpperBound(0); i++)
				factors[i, largestJ] = 0.0;

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build a two-dimensional array of correlation factors between the current and
		/// revision paragraphs. The first dimension represents the current paragraphs and the
		/// second represents the revision paragraphs.
		/// </summary>
		/// <param name="stringsCurr">set of paragraph strings from current</param>
		/// <param name="stringsRev">set of paragraph strings from revision</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private double[,] BuildCorrelationFactorMap(List<string> stringsCurr, List<string> stringsRev)
		{
			double[,] factors = new double[stringsCurr.Count, stringsRev.Count];

			// If both lists have only one paragraph then this is a special case in which
			// we consider them to correlate.
			if (stringsCurr.Count == 1 && stringsRev.Count == 1)
			{
				factors[0, 0] =  1.0;
				return factors;
			}

			// Calculate correlation factors for all pairs of paragraphs.
			for (int i = 0; i < stringsCurr.Count; i++)
			{
				for (int j = 0; j < stringsRev.Count; j++)
				{
					ParagraphCorrelation pc = new ParagraphCorrelation(stringsCurr[i], stringsRev[j],
						m_cache.UnicodeCharProps);
					factors[i,j] = pc.CorrelationFactor;
				}
			}

			return factors;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build a list of correlations for verses that are split across paragraphs
		/// (i.e. for groups of ScrVerses that have the same BCV reference in different paragraphs)
		/// </summary>
		/// <param name="scrVersesCurr">List of ScrVerse objects in the current</param>
		/// <param name="currCorrelations">Array of correlations to fill in. Each entry
		/// corresponds to the same index in the ScrVersesCurr array and has the index
		/// of the correlating ScrVerse in the ScrVersesRev list, or -1 if there
		/// is no correlation</param>
		/// <param name="scrVersesRev">list of ScrVerse objects in the revision</param>
		/// <param name="revCorrelations">Array of correlations to fill in. Each entry
		/// corresponds to the same index in the ScrVersesRev array and has the index
		/// of the correlating ScrVerse in the ScrVersesCurr list, or -1 if there
		/// is no correlation</param>
		/// ------------------------------------------------------------------------------------
		private void BuildMultiParaVerseCorrelations(List<ScrVerse> scrVersesCurr, int[] currCorrelations,
			List<ScrVerse> scrVersesRev, int[] revCorrelations)
		{
			// Start by assumuning that nothing correlates and fill in the lists.
			for (int i = 0; i < currCorrelations.Length; i++)
				currCorrelations[i] = -1;
			for (int i = 0; i < revCorrelations.Length; i++)
				revCorrelations[i] = -1;

			int indexCurr = 0; // indexes to the lists of ScrVerses
			int indexRev = 0;
			// Look through the entire list of ScrVerses on both sides looking for reference correlations
			while (indexCurr < scrVersesCurr.Count && indexRev < scrVersesRev.Count)
			{
				ScrVerse scrVerseCurr = scrVersesCurr[indexCurr];
				ScrVerse scrVerseRev = scrVersesRev[indexRev];
				if (scrVerseCurr.StartRef > scrVerseRev.StartRef)
					indexRev++;
				else if (scrVerseCurr.StartRef < scrVerseRev.StartRef)
					indexCurr++;
				else
				{
					// At this point the start refs match. Are there any following ScrVerses
					// that have an overlapping reference range in different paragraphs?
					int iLimLastMatchCurr;
					int iLimLastMatchRev;
					bool gotSome = GetRangeOfMultiParaScrVerses(scrVersesCurr, scrVersesRev,
						indexCurr, indexRev, out iLimLastMatchCurr, out iLimLastMatchRev);

					// If we found a range of ScrVerses that have overlapping references in different paragraphs
					if (gotSome)
					{
						// Determine correlations between the text of ScrVerses in the range
						List<string> stringsCurr = new List<string>();
						for (int i = indexCurr; i < iLimLastMatchCurr; i++)
							stringsCurr.Add(scrVersesCurr[i].Text.Text);
						List<string> stringsRev = new List<string>();
						for (int i = indexRev; i < iLimLastMatchRev; i++)
							stringsRev.Add(scrVersesRev[i].Text.Text);
						List<ParaCorrelationInfo> corrInfoList = DetermineAllStringCorrelations(stringsCurr, stringsRev);
						foreach (ParaCorrelationInfo correlation in corrInfoList)
						{
							// enter the indexes of correlating verse paragraphs into our
							//  output arrays of ScrVerse correlations
							currCorrelations[correlation.iParaCurr + indexCurr] = correlation.iParaRev + indexRev;
							revCorrelations[correlation.iParaRev + indexRev] = correlation.iParaCurr + indexCurr;
						}
					}
					indexCurr = iLimLastMatchCurr;
					indexRev = iLimLastMatchRev;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if there any following ScrVerses in different paragraphs that have an
		/// overlapping reference range with the given starting ScrVerses.
		/// </summary>
		/// <param name="scrVersesCurr">list of ScrVerses in Current</param>
		/// <param name="scrVersesRev">list of ScrVerses in Revision</param>
		/// <param name="indexCurr">the starting index in Current</param>
		/// <param name="indexRev">the starting index in Revision</param>
		/// <param name="iLimLastMatchCurr">out: the Curr index Lim beyond the last ScrVerse that
		/// matches our criteria</param>
		/// <param name="iLimLastMatchRev">out: the Rev index Lim beyond the last ScrVerse that
		/// matches our criteria</param>
		/// <returns>true if we found any following ScrVerses that match our criteria; false
		/// if not</returns>
		/// ------------------------------------------------------------------------------------
		private bool GetRangeOfMultiParaScrVerses(
			List<ScrVerse> scrVersesCurr, List<ScrVerse> scrVersesRev,
			int indexCurr, int indexRev, out int iLimLastMatchCurr, out int iLimLastMatchRev)
		{
			// At this point the start refs match. Are there any following ScrVerses
			// that have an overlapping reference range in different paragraphs?
			iLimLastMatchCurr = indexCurr + 1;
			iLimLastMatchRev = indexRev + 1;

			bool gotOneMore = true; //enter the while loop first time
			while ((iLimLastMatchCurr < scrVersesCurr.Count || iLimLastMatchRev < scrVersesRev.Count)
				&& gotOneMore)
			{
				gotOneMore = false;

				// increment iLimLastMatchCurr if the following curr paragraph has a ref overlap with scrVerseRev
				ScrVerse scrVerseRev = scrVersesRev[iLimLastMatchRev - 1];
				int lastCurrHvo = scrVersesCurr[iLimLastMatchCurr - 1].HvoPara;

				ScrVerse scrVerse = null;
				int i = iLimLastMatchCurr;
				while (i < scrVersesCurr.Count)
				{
					scrVerse = scrVersesCurr[i];
					if (scrVerse.HvoPara != lastCurrHvo)
						break; //found a scrVerse in the next para
					i++;
				}
				if (i < scrVersesCurr.Count && IsRefOverlap(scrVerse, scrVerseRev))
				{
					// got one more in the Curr
					iLimLastMatchCurr = ++i;
					gotOneMore = true;
				}

				// increment iLimLastMatchRev if the following rev paragraph has a ref overlap with scrVerseCurr
				ScrVerse scrVerseCurr = scrVersesCurr[iLimLastMatchCurr - 1];
				int lastRevHvo = scrVersesRev[iLimLastMatchRev - 1].HvoPara;

				i = iLimLastMatchRev;
				while (i < scrVersesRev.Count)
				{
					scrVerse = scrVersesRev[i];
					if (scrVerse.HvoPara != lastRevHvo)
						break; //found a scrVerse in the next para
					i++;
				}
				if (i < scrVersesRev.Count && IsRefOverlap(scrVerse, scrVerseCurr))
				{
					// got one more in the Rev
					iLimLastMatchRev = ++i;
					gotOneMore = true;
				}
			}

			return (iLimLastMatchCurr > indexCurr + 1 || iLimLastMatchRev > indexRev + 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if two ScrVerses have any reference overlap
		/// </summary>
		/// <returns>true if there is any reference overlap</returns>
		/// ------------------------------------------------------------------------------------
		protected bool IsRefOverlap(ScrVerse v1, ScrVerse v2)
		{
			// is one of the ScrVerses empty? Don't consider it as an overlap.
			if (v1.TextLength == 0 || v2.TextLength == 0)
				return false;

			// is v1 completely before v2?
			if (v1.EndRef < v2.StartRef)
				return false;
			// is v2 completely before v1?
			if (v2.EndRef < v1.StartRef)
				return false;
			// there must be some overlap
			return true;

			// former algorithm, for short term reference just in case...
			//// is scrverse 2 start ref in between the range of scrverse 1?
			//if (v2.StartRef >= v1.StartRef && v2.StartRef <= v1.EndRef)
			//    return true;
			//// is scrverse 2 end ref in between the range of scrverse 1?
			//if (v2.EndRef >= v1.StartRef && v2.EndRef <= v1.EndRef)
			//    return true;
			//// does scrverse 2 completely encompass scrverse 1?
			//if (v2.StartRef <= v1.StartRef && v2.EndRef >= v1.EndRef)
			//    return true;
			//return false;
		}
		#endregion

		#region Detect Differences: CompareVerseText
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="tss"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private ITsString GetTrimmedString(ITsString tss)
		{
//			if (tss.Length == 0)
				return tss;
//			// determine how many trailing spaces need to be removed
//			string text = tss.Text;
//			int oldLen = text.Length;
//			int trimSize = oldLen - text.TrimEnd().Length;
//
//			// if there are no trailing spaces then the string will not change
//			if (trimSize == 0)
//				return tss;
//
//			// remove the trailing spaces and return the new string
//			ITsStrBldr bldr = tss.GetBldr();
//			bldr.Replace(oldLen - trimSize, oldLen, null, null);
//			return bldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare the text of two verses to determine if there are differences.
		/// </summary>
		/// <param name="verse1">first verse text to compare</param>
		/// <param name="verse2">second verse text to compare</param>
		/// <param name="ichMin">out: If a difference is detected, this is the offset of the
		/// start of the difference in the verses</param>
		/// <param name="ichLim1">out: If a difference is detected, this is the offset of the
		/// end of the difference within verse1</param>
		/// <param name="ichLim2">out: If a difference is detected, this is the offset of the
		/// end of the difference within verse2</param>
		/// <param name="subDiffs">Collection for sub-differences that should be part of the
		/// root difference that our caller is making, e.g. footnotes;
		/// or null if no subdiffs should be gathered
		/// </param>
		/// <param name="sCharStyleNameCurr">ref: The name of the character style in the current
		/// text, if this difference represents a single character style difference</param>
		/// <param name="sCharStyleNameRev">The name of the character style in the revision
		/// text, if this difference represents a single character style difference</param>
		/// <param name="sWsNameCurr">ref: The name of the writing system in the current
		/// text, if this difference represents a single writing system difference</param>
		/// <param name="sWsNameRev">The name of the character style in the revision
		/// text, if this difference represents a single writing system difference</param>
		/// <returns>NoDifference if no differences, or'd difference types if there are
		/// differences</returns>
		/// ------------------------------------------------------------------------------------
		private DifferenceType CompareVerseText(ITsString verse1, ITsString verse2,
			out int ichMin, out int ichLim1, out int ichLim2, List<Difference> subDiffs,
			ref string sCharStyleNameCurr, ref string sCharStyleNameRev,
			ref string sWsNameCurr, ref string sWsNameRev)
		{
			//REVIEW: consider returning a Difference instead of all the pieces; input param bool fIncludeSubDiffs
			sCharStyleNameCurr = sCharStyleNameRev = sWsNameCurr = sWsNameRev = null;

			// remove trailing spaces before comparing
			ITsString tssV1 = GetTrimmedString(verse1);
			ITsString tssV2 = GetTrimmedString(verse2);
			int length1 = (tssV1 != null) ? tssV1.Length : 0;
			int length2 = (tssV2 != null) ? tssV2.Length : 0;

			// Search the strings to find the extent of any textual difference
			DifferenceType foundDiffType = FindStringDifference(tssV1, tssV2, out ichMin,
				out ichLim1, out ichLim2);

			Difference firstSubDiff = null;
			Difference lastSubDiff = null;
			int iRunForwardScan = -1;
			DifferenceType diffType = DifferenceType.NoDifference;

			/* Start at the beginning of both strings. Compare sub-runs forward until we find the first difference (of any type).
			 * Mark that as the ichMin for both strings. Jump to the end of the strings and begin comparing sub-runs
			 * backwards. As soon as you find the first (i.e., last) difference, record the ichLim for each string.
			 * Then in each of the strings scan any runs between the min and the lim, looking only for objects.
			 * When we find any object, add as a sub-difference.*/

			// cycle through the sub-runs going forward
			int ichMinNextSubRun;
			for (int ichMinSubRun = 0;
				ichMinSubRun < Math.Min(length1, length2);
				ichMinSubRun = ichMinNextSubRun)
			{
				// If ichMinSubRun is after the beginning of the text difference, then break
				// because we have no way to know if the sub-runs correlate in a meaningful way for the user
				if (ichMin != -1 && ichMinSubRun >= ichMin)
				{
					iRunForwardScan = tssV1.get_RunAt(ichMin) - 1;
					break;
				}
				diffType = CompareSubRunPropsForward(tssV1, tssV2, ichMinSubRun,
					out firstSubDiff, ref sCharStyleNameCurr, ref sCharStyleNameRev,
					ref sWsNameCurr, ref sWsNameRev, out ichMinNextSubRun);

				if (diffType != DifferenceType.NoDifference) // there was a difference
				{
					iRunForwardScan = tssV1.get_RunAt(ichMinSubRun); // store which run the prop diff was in

					// If we already have found a (text) difference, then we have to take the min
					// of the position of that difference and this (style or object) difference.
					if (foundDiffType == DifferenceType.NoDifference)
						ichMin = ichMinSubRun;
					else
						ichMin = Math.Min(ichMinSubRun, ichMin);

					foundDiffType |= diffType;
					break;
				}
			} // end 'for': cycling through runs (forward)

			// If no difference was found and the strings are the same length, then the verses are identical
			if (foundDiffType == DifferenceType.NoDifference)
			{
				if (length1 == length2)
					return foundDiffType;
				// If the verses are not the same length, then an ORC must have been added to the end of one
				// of them. Set the min of the differences to the length of the shorter string.
				ichMin = Math.Min(length1, length2);
			}
			// If no difference was found in the properties looking forward, then we don't need to look in reverse
			// cycle through the runs (backward)
			int ichLimSubRun1 = length1;
			int ichLimSubRun2 = length2;
			int ichLimNextSubRun1, ichLimNextSubRun2;
			int iRun1 = -1;
			int iRun2 = -1;
			//REVIEW: should we quit when we reach what was searched on the forward loop?
			for (; Math.Min(ichLimSubRun1, ichLimSubRun2) > 0;
				ichLimSubRun1 = ichLimNextSubRun1, ichLimSubRun2 = ichLimNextSubRun2)
			{
				// If ichLims are before the end of the text difference, then break
				// because we have no way to know if the sub-runs correlate in a meaningful way for the user
				if (ichLim1 != -1 && (ichLimSubRun1 <= ichLim1 || ichLimSubRun2 <= ichLim2))
				{
					// Need to add 1 to each run index because these will be pre-decremented below
					// when searching for embedded objects, and we need to include in this scan any
					// run that has the final text difference
					// BenW and KimM added the following commented out lines in order to fix some problem they observed, but they
					// didn't write a test and it caused other tests to fail and they can't remember the problem, so we're commenting
					// it out until...
					//					if (diffType != DifferenceType.NoDifference)
					//					{
					iRun1 = tssV1.get_RunAt(ichLimSubRun1 - 1) + 1;
					iRun2 = tssV2.get_RunAt(ichLimSubRun2 - 1) + 1;
					//					}
					break;
				}
				Difference subDiff;
				diffType = CompareSubRunPropsBackward(tssV1, tssV2, ichLimSubRun1, ichLimSubRun2,
					out subDiff, ref sCharStyleNameCurr, ref sCharStyleNameRev,
					ref sWsNameCurr, ref sWsNameRev, out ichLimNextSubRun1, out ichLimNextSubRun2);

				// if we found a difference then 'or' it with the difference that was found
				// in any previous compare.
				if (diffType != DifferenceType.NoDifference)
				{
					// TODO: Deal with multiple Writing System differences

					// If one of the previous differences was a character style difference
					// and the run that we find with a character style difference is not the
					// same run we found while going forward then we must have a multiple
					// style difference.
					iRun1 = tssV1.get_RunAt(ichLimSubRun1 - 1);
					iRun2 = tssV2.get_RunAt(ichLimSubRun2 - 1);
					bool fTextDifference = ((foundDiffType & DifferenceType.TextDifference) != 0);
					if (((foundDiffType & diffType) != 0) &&
						(iRunForwardScan < (iRun1 - (fTextDifference ? 0 : 1))))
					{
						if ((diffType & DifferenceType.CharStyleDifference) != 0)
						{
							diffType |= DifferenceType.MultipleCharStyleDifferences;
							// we need to get rid of the character style difference type.
							diffType ^= DifferenceType.CharStyleDifference;
							foundDiffType ^= DifferenceType.CharStyleDifference;
							sCharStyleNameCurr = sCharStyleNameRev = null;
						}
						else if ((diffType & DifferenceType.WritingSystemDifference) != 0)
						{
							diffType |= DifferenceType.MultipleWritingSystemDifferences;
							// we need to get rid of the writing system difference type.
							diffType ^= DifferenceType.WritingSystemDifference;
							foundDiffType ^= DifferenceType.WritingSystemDifference;
							sWsNameCurr = sWsNameRev = null;
						}
						lastSubDiff = subDiff;
					}
					else if ((foundDiffType & diffType) == 0)
					{
						// We didn't find this kind of difference at all going forward, so if this
						// has a subdiff, we need to add it to our collection.
						lastSubDiff = subDiff;
					}

					ichLim1 = Math.Max(ichLimSubRun1, ichLim1);
					ichLim2 = Math.Max(ichLimSubRun2, ichLim2);
					foundDiffType |= diffType;
					break;
				}
			}  // end 'for': cycling through runs (backward)

			// If a difference was found and a lim was not set then it was an added run at the
			// start so we need to set the lims.
			if (foundDiffType != DifferenceType.NoDifference && ichLim1 == -1)
			{
				ichLim1 = ichLimSubRun1;
				ichLim2 = ichLimSubRun2;
			}

			// Collect all applicable subdiffs, in the order that the ORCs occur in the text.
			// First one first.
			if (firstSubDiff != null)
				subDiffs.Add(firstSubDiff);

			// TODO: Scan intervening runs
			// Now create subdifferences for any object runs in either string between the first
			// different run and the last different run.
			if (iRunForwardScan >= 0)
			{
				int cFootnotesAdded1 = 0;
				int cFootnotesAdded2 = 0;

				int i = iRunForwardScan;
				while (++i < iRun1)
				{
					// Check this run. If it's an object, consider it to be an object "added" to V1.
					if (CreateSubDiffForObject(tssV1, i, subDiffs, true))
						cFootnotesAdded1++;
				}
				int j = iRunForwardScan;
				while (++j < iRun2)
				{
					// Check this run. If it's an object, consider it to be an object "added" to V2.
					if (CreateSubDiffForObject(tssV2, j, subDiffs, false))
						cFootnotesAdded2++;
				}
				if (cFootnotesAdded1 > cFootnotesAdded2)
				{
					foundDiffType |= DifferenceType.FootnoteAddedToCurrent;
				}
				else if (cFootnotesAdded2 > cFootnotesAdded1)
				{
					foundDiffType |= DifferenceType.FootnoteMissingInCurrent;
				}
			}

			// Finally, collect the last subdiff too
			if (lastSubDiff != null)
				subDiffs.Add(lastSubDiff);

			// If either lim crossed over the min then adjust the lims forward by the same
			// amount so there is no crossing over. This is needed, eg, when a word is missing
			// in one string, and one space gets matched from both directions. Also needed if a
			// word is repeated in one string.
			if (((foundDiffType & DifferenceType.TextDifference) != 0 ||
				(foundDiffType & DifferenceType.FootnoteMissingInCurrent) != 0 ||
				(foundDiffType & DifferenceType.FootnoteAddedToCurrent) != 0) &&
				(ichLim1 < ichMin || ichLim2 < ichMin))
			{
				int adjust = ichMin - Math.Min(ichLim1, ichLim2);
				ichLim1 += adjust;
				ichLim2 += adjust;
			}
			else if((foundDiffType & DifferenceType.CharStyleDifference) != 0 ||
				(foundDiffType & DifferenceType.MultipleCharStyleDifferences) != 0)
			{
				int tempLim = Math.Min(tssV2.Length - ichLim2,
					tssV1.Length - ichLim1);
				ichLim1 = tssV1.Length - tempLim;
				ichLim2 = tssV2.Length - tempLim;
			}

			return foundDiffType;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Search the strings to find the extent of any textual difference
		/// </summary>
		/// <param name="tssV1">A string from Current</param>
		/// <param name="tssV2">A string from Revision</param>
		/// <param name="ichMin">If a difference is detected, this is the offset of the
		/// start of the difference. Except if the diff</param>
		/// <param name="ichLim1">If a difference is detected, this is the offset of the
		/// end of the difference</param>
		/// <param name="ichLim2">If a difference is detected, this is the offset of the
		/// end of the difference</param>
		/// <returns>A DifferenceType of either NoDifference or TextDifference</returns>
		/// ------------------------------------------------------------------------------------
		private DifferenceType FindStringDifference(ITsString tssV1, ITsString tssV2,
			out int ichMin, out int ichLim1, out int ichLim2)
		{
			string sV1 = (tssV1 == null || tssV1.Text == null) ? string.Empty : tssV1.Text;
			string sV2 = (tssV2 == null || tssV2.Text == null) ? string.Empty : tssV2.Text;

			if (StringUtils.FindStringDifference(sV1, sV2, out ichMin, out ichLim1, out ichLim2))
			{
				// Does the text difference we found consist only of inserted/deleted ORCs?
				bool fOnlyDifferencesAreORCS = true;

				// Look to see if there are any differences other than ORCs in the found ranges
				for (int i = ichMin; (i < ichLim1) && fOnlyDifferencesAreORCS; i++)
				{
					if (sV1[i] != StringUtils.kchObject)
						fOnlyDifferencesAreORCS = false;
				}
				for (int i = ichMin; (i < ichLim2) && fOnlyDifferencesAreORCS; i++)
				{
					if (sV2[i] != StringUtils.kchObject)
						fOnlyDifferencesAreORCS = false;
				}

				// If the only text difference is inserted/deleted ORCs, we
				// don't want to treat it as a text difference at all. Sorry!
				// The prop comparisons will handle the ORCs later.
				if (fOnlyDifferencesAreORCS)
				{
					ichMin = ichLim1 = ichLim2 = -1;
					return DifferenceType.NoDifference;
				}
				else
					return DifferenceType.TextDifference;
			}
			return DifferenceType.NoDifference;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a subdiff to the array if the given run of the ITsString is an embedded object
		/// that needs to be displayed with a highlight in a special way in the diff dialog.
		/// </summary>
		/// <param name="tss">TsString to check for an object</param>
		/// <param name="iRun">run number to check</param>
		/// <param name="subDiffs"></param>
		/// <param name="inCurr">True specifies we are dealing with current, false if revision</param>
		/// <returns><c>true</c> if a footnote subdiff was added</returns>
		/// ------------------------------------------------------------------------------------
		bool CreateSubDiffForObject(ITsString tss, int iRun, List<Difference> subDiffs, bool inCurr)
		{
			Debug.Assert(iRun < tss.RunCount);

			TsRunInfo tri;

			// Get the properties and text for each run.
			ITsTextProps ttp = tss.FetchRunInfo(iRun, out tri);

			string sRun = tss.GetChars(tri.ichMin, tri.ichLim);

			// Check for ORC in strings - strings will be 1 character long and both
			// contain the ORC character.
			if (sRun.Length == 1 && sRun[0] == StringUtils.kchObject)
			{
				ICmObject cmObject = GetObjectFromProps(ttp);
				if (cmObject is IStFootnote)
				{
					subDiffs.Add(CreateSubDiffForFootnote((IStFootnote)cmObject, inCurr));
					return true;
				}
				// REVIEW: Do we need to do anything special for pictures?
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a one-sided (with info only about the curr or rev) difference for the given
		/// footnote
		/// </summary>
		/// <param name="footnote">The footnote</param>
		/// <param name="inCurr">True specifies we are dealing with current, false if revision
		/// </param>
		/// <returns>A new Difference</returns>
		/// ------------------------------------------------------------------------------------
		private Difference CreateSubDiffForFootnote(IStFootnote footnote, bool inCurr)
		{
			// Review: Handle multi-para footnotes (someday)
			IStTxtPara para = (IStTxtPara)footnote.ParagraphsOS[0];
			int cchParaContents = para.Contents.Length;
			if (inCurr)
				return new Difference(para.Hvo, cchParaContents, 0, 0);
			else
				return new Difference(0, 0, para.Hvo, cchParaContents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a root difference, get the first sub-diff for a footnote (if any), for
		/// either the Current or the Revision.
		/// </summary>
		/// <remarks>We assume and require that the subdiffs are in the same order as the
		/// footnotes that they represent</remarks>
		/// <param name="rootDiff">The root difference.</param>
		/// <param name="fRev">True specifies we are dealing with Revision, false if Current.
		/// </param>
		/// <returns>the first sub-diff found for a footnote in the specified view,
		/// or zero if none</returns>
		/// ------------------------------------------------------------------------------------
		internal Difference GetFirstFootnoteDiff(Difference rootDiff, bool fRev)
		{
			CheckDisposed();

			if (rootDiff.SubDiffsForORCs == null || rootDiff.SubDiffsForORCs.Count == 0)
				return null;

			foreach (Difference subDiff in rootDiff.SubDiffsForORCs)
			{
				// get the hvo of the applicable sub-diff paragraph
				int hvoPara;
				if (fRev)
					hvoPara = subDiff.HvoRev;
				else
					hvoPara = subDiff.HvoCurr;

				if (hvoPara != 0)
				{
					// if this para belongs to a StFootnote, we have found the first footnote diff
					int hvoOwner = m_cache.GetOwnerOfObject(hvoPara);
					if (m_cache.GetClassOfObject(hvoOwner) == StFootnote.kClassId)
						return subDiff;
				}
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare the run properties of the given sub-runs in two TsStrings, to determine
		/// if there are property and/or object differences. This is implemented for stepping
		/// forwards through a verse, with the given ichMin indicating the start of the sub-runs.
		/// A sub-run is a portion of a run, with text of the same length and probably at least
		/// partially matching characters.
		/// </summary>
		/// <param name="tssV1">A string from Current</param>
		/// <param name="tssV2">A string from Revision</param>
		/// <param name="ichMin">the starting char position of the two sub-runs</param>
		/// <param name="subDiff">Sequence of differences in owned objects, such as footnotes.
		/// </param>
		/// <param name="sCharStyleName1">The name of the character style in sub-run 1,
		///  if we detect a character style difference</param>
		/// <param name="sCharStyleName2">The name of the character style in sub-run 2,
		///  if we detect a character style difference</param>
		/// <param name="sWsName1">The name of the writing system in sub-run 1,
		///  if we detect a writing system difference</param>
		/// <param name="sWsName2">The name of the writing system in sub-run 2,
		///  if we detect a writing system difference</param>
		///  <param name="ichMinNextSubRun">output: the starting char position of the sub-runs
		///  just after the given sub-runs; this will be just after the "shorter" run</param>
		/// <returns>type of difference found</returns>
		/// ------------------------------------------------------------------------------------
		private DifferenceType CompareSubRunPropsForward(ITsString tssV1, ITsString tssV2,
			int ichMin,  out Difference subDiff,
			ref string sCharStyleName1, ref string sCharStyleName2,
			ref string sWsName1, ref string sWsName2, out int ichMinNextSubRun)
		{
			TsRunInfo triV1, triV2;

			ITsTextProps ttpV1 = tssV1.FetchRunInfoAt(ichMin, out triV1);
			//REVIEW: if one para has a verse and the other is empty, could we possibly have an empty tss
			// which would need extra code here?  maybe our caller would not call us for an empty tss... write/check if a unit test proves it
			string sFirstChar1 = tssV1.GetChars(ichMin, ichMin + 1);

			ITsTextProps ttpV2 = tssV2.FetchRunInfoAt(ichMin, out triV2);
			string sFirstChar2 = tssV2.GetChars(ichMin, ichMin + 1);

			// the loop that calls us should advance just past the shorter string
			ichMinNextSubRun = Math.Min(triV1.ichLim, triV2.ichLim);

			// now compare the props and objects of the current sub-run
			return CompareRunProps(ttpV1, ttpV2, sFirstChar1[0], sFirstChar2[0], out subDiff,
				ref sCharStyleName1, ref sCharStyleName2, ref sWsName1, ref sWsName2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare the run properties of the given sub-runs in two TsStrings, to determine
		/// if there are property and/or object differences. This is implemented for stepping
		/// backwards through a verse, with the given ichLim's indicating the end of the sub-runs.
		/// A sub-run is a portion of a run, with text of the same length and probably at least
		/// partially matching characters.
		/// </summary>
		/// <param name="tssV1">A string from Current</param>
		/// <param name="tssV2">A string from Revision</param>
		/// <param name="ichLim1">the Lim char position of the sub-run 1</param>
		/// <param name="ichLim2">the Lim char position of the sub-run 2</param>
		/// <param name="subDiff">Sequence of differences in owned objects, such as footnotes.
		/// </param>
		/// <param name="sCharStyleName1">The name of the character style in sub-run 1,
		///  if we detect a character style difference</param>
		/// <param name="sCharStyleName2">The name of the character style in sub-run 2,
		///  if we detect a character style difference</param>
		/// <param name="sWsName1">The name of the writing system in sub-run 1,
		///  if we detect a writing system difference</param>
		/// <param name="sWsName2">The name of the writing system in sub-run 2,
		///  if we detect a writing system difference</param>
		///  <param name="ichLimNextSubRun1">output: the Lim char position of the sub-run
		///  just before the given sub-run 1; this will decrement to the start of the "shorter" run</param>
		///  <param name="ichLimNextSubRun2">output: the Lim char position of the sub-run
		///  just before the given sub-run 2; this will decrement to start of the "shorter" run</param>
		/// <returns>type of difference found</returns>
		/// ------------------------------------------------------------------------------------
		private DifferenceType CompareSubRunPropsBackward(ITsString tssV1, ITsString tssV2,
			int ichLim1, int ichLim2, out Difference subDiff,
			ref string sCharStyleName1, ref string sCharStyleName2,
			ref string sWsName1, ref string sWsName2,
			out int ichLimNextSubRun1, out int ichLimNextSubRun2)
		{
			TsRunInfo triV1, triV2;

			ITsTextProps ttpV1 = tssV1.FetchRunInfoAt(ichLim1 - 1, out triV1);
			string sLastChar1 = tssV1.GetChars(ichLim1 - 1, ichLim1);

			ITsTextProps ttpV2 = tssV2.FetchRunInfoAt(ichLim2 - 1, out triV2);
			string sLastChar2 = tssV2.GetChars(ichLim2 - 1, ichLim2);

			// the loop that calls us should decrement both Lim's to the start of the shorter string
			int length1 = ichLim1 - triV1.ichMin;
			int length2 = ichLim2 - triV2.ichMin;
			ichLimNextSubRun1 = ichLim1 - Math.Min(length1, length2);
			ichLimNextSubRun2 = ichLim2 - Math.Min(length1, length2);

			// now compare the props and objects of the current sub-run
			return CompareRunProps(ttpV1, ttpV2, sLastChar1[0], sLastChar2[0], out subDiff,
				ref sCharStyleName1, ref sCharStyleName2, ref sWsName1, ref sWsName2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a simple class with no state information whose only purpose is to allow us
		/// to create an object that represents a missing object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class MissingObject
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare the given run properties of runs in two TsStrings, to determine if there are
		/// property and/or object differences. Actually, these may be properties of sub-runs
		/// (a portion of a run, with text of the same length and at least partially matching
		/// characters).
		/// </summary>
		/// <param name="ttpV1">props of the sub-run from the Current</param>
		/// <param name="ttpV2">props of the sub-run from the Revision</param>
		/// <param name="char1">a character from the first sub-run, used to check if the sub-run
		/// content is an ORC (if present, an ORC is the entire run)</param>
		/// <param name="char2">a character from the second sub-run, used to check if the sub-run
		/// content is an ORC (if present, an ORC is the entire run)</param>
		/// <param name="subDiff"></param>
		/// <param name="sCharStyleName1">The name of the character style in sub-run 1,
		///  if we detect a character style difference</param>
		/// <param name="sCharStyleName2">The name of the character style in sub-run 2,
		///  if we detect a character style difference</param>
		/// <param name="sWsName1">The name of the writing system in sub-run 1,
		///  if we detect a writing system difference</param>
		/// <param name="sWsName2">The name of the writing system in sub-run 2,
		///  if we detect a writing system difference</param>
		/// <returns>type of difference found</returns>
		/// ------------------------------------------------------------------------------------
		private DifferenceType CompareRunProps(ITsTextProps ttpV1, ITsTextProps ttpV2,
			char char1, char char2, out Difference subDiff,
			ref string sCharStyleName1, ref string sCharStyleName2,
			ref string sWsName1, ref string sWsName2)
		{
			subDiff = null;
			DifferenceType diffType = DifferenceType.NoDifference;

			// Get the properties and text for run 1 (if we still have any runs left).
			bool fRun1IsObject = (char1 == StringUtils.kchObject);
			object obj1 = null;

			if (fRun1IsObject)
			{
				try
				{
					obj1 = GetObjectFromProps(ttpV1);
					if (obj1 == null)
						obj1 = new MissingObject();
				}
				catch (NullReferenceException)
				{
					obj1 = new MissingObject();
				}
			}

			// Get the properties and text for run 2 (if we still have any runs left).
			bool fRun2IsObject = (char2 == StringUtils.kchObject);
			object obj2 = null;

			if (fRun2IsObject)
			{
				try
				{
					obj2 = GetObjectFromProps(ttpV2);
					if (obj2 == null)
						obj2 = new MissingObject();
				}
				catch (NullReferenceException)
				{
					obj2 = new MissingObject();
				}
			}

			if (fRun1IsObject && fRun2IsObject)
			{
				// Are these two objects different??? How?
				if (obj1.GetType() != obj2.GetType())
				{
					// TODO: Report as object difference instead of text difference
					diffType = DifferenceType.TextDifference;
				}
					// They are same type, now need to do object specific comparisons
					// Don't necessarily want full equality checks - since some minor differences
					// may be ignored.
				else if (obj1 is StFootnote)
				{
					subDiff = CompareFootnotes((StFootnote)obj1, (StFootnote) obj2);
					if (subDiff != null)
					{
						diffType = DifferenceType.FootnoteDifference;
					}
				}
				// TODO: Handle pictures
				// Unknown object types will be treated as equal
			}
			else if (fRun1IsObject)
			{
				// An object has been added to the Current
				if (obj1 is StFootnote)
				{
					subDiff = CreateSubDiffForFootnote((StFootnote)obj1, true);
					diffType = DifferenceType.FootnoteAddedToCurrent;
				}
				else if (obj1 is CmPicture)
				{
					diffType = DifferenceType.PictureAddedToCurrent;
				}
				else if (obj1 is MissingObject)
				{
					diffType = DifferenceType.FootnoteAddedToCurrent;
				}
			}
			else if (fRun2IsObject)
			{
				// An object has been added to the Revision
				if (obj2 is StFootnote)
				{
					subDiff = CreateSubDiffForFootnote((StFootnote)obj2, false);
					diffType = DifferenceType.FootnoteMissingInCurrent;
				}
				else if (obj2 is CmPicture)
				{
					diffType = DifferenceType.PictureMissingInCurrent;
				}
				else if (obj2 is MissingObject)
				{
					diffType = DifferenceType.FootnoteMissingInCurrent;
				}
			}

			// There is no object difference. Check to see if the text props have a different
			// character style or writing system
			else if (!ttpV1.Equals(ttpV2))
			{
				sCharStyleName1 = ttpV1.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				sCharStyleName2 = ttpV2.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				if (sCharStyleName1 != sCharStyleName2)
				{
					diffType = DifferenceType.CharStyleDifference;
					if (sCharStyleName1 == null)
						sCharStyleName1 = FdoResources.DefaultParaCharsStyleName;
					if (sCharStyleName2 == null)
						sCharStyleName2 = FdoResources.DefaultParaCharsStyleName;
				}

				int var;
				int ws1 = ttpV1.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
				int ws2 = ttpV2.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
				if (ws1 != ws2)
				{
					diffType |= DifferenceType.WritingSystemDifference;
					sWsName1 = (new LgWritingSystem(m_cache, ws1)).Name.UserDefaultWritingSystem;
					sWsName2 = (new LgWritingSystem(m_cache, ws2)).Name.UserDefaultWritingSystem;
				}
			}
			return diffType;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create an FDO object from the GUID in the given props
		/// </summary>
		/// <param name="ttp">The props that we hope represent an object</param>
		/// <returns>The extracted object</returns>
		/// ------------------------------------------------------------------------------------
		private ICmObject GetObjectFromProps(ITsTextProps ttp)
		{
			string objData = ttp.GetStrPropValue((int)FwTextPropType.ktptObjData);

			if (objData != null && objData.Length != 0 &&
				(objData[0] == (char) FwObjDataTypes.kodtOwnNameGuidHot ||
				objData[0] == (char) FwObjDataTypes.kodtNameGuidHot ||
				objData[0] == (char) FwObjDataTypes.kodtGuidMoveableObjDisp))
			{
				Guid objGuid = MiscUtils.GetGuidFromObjData(objData.Substring(1));
				int hvoObj = m_cache.GetIdFromGuid(objGuid);

				if (hvoObj > 0)
				{
					return CmObject.CreateFromDBObject(m_cache, hvoObj);
				}
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two footnotes to see if they are equivalent for diff purposes. Footnote
		/// properties are ignored and only the text of footnotes is compared.
		/// </summary>
		/// <param name="footnoteCur"></param>
		/// <param name="footnoteRev"></param>
		/// <returns>a <see cref="Difference"/> if footnotes are not equivalent</returns>
		/// ------------------------------------------------------------------------------------
		private Difference CompareFootnotes(StFootnote footnoteCur, StFootnote footnoteRev)
		{
			if (footnoteCur.ParagraphsOS.Count != footnoteRev.ParagraphsOS.Count)
			{
				// Review: Report difference(s) more specifically (and accurately).
				return new Difference(0, 0, 0, 0, 0, 0,
					DifferenceType.ParagraphAddedToCurrent, null, null, null, null);
			}

			StTxtPara paraCur;
			StTxtPara paraRev;
			int ichMin, ichLimCur, ichLimRev;
			string sCharStyleNameCurr = null, sCharStyleNameRev = null;
			string sWsNameCurr = null, sWsNameRev = null;
			for (int i = 0; i < footnoteCur.ParagraphsOS.Count; i++)
			{
				paraCur = (StTxtPara) footnoteCur.ParagraphsOS[i];
				paraRev = (StTxtPara) footnoteRev.ParagraphsOS[i];

				Debug.Assert(paraCur.StyleRules != null);
				Debug.Assert(paraRev.StyleRules != null);
				if (paraCur.StyleRules == null || !paraCur.StyleRules.Equals(paraRev.StyleRules))
				{
					return new Difference(paraCur.Hvo, 0, paraCur.Contents.Length,
						paraRev.Hvo, 0, paraRev.Contents.Length,
						DifferenceType.ParagraphStyleDifference, null, null, null, null);
				}

				DifferenceType diffType = CompareVerseText(paraCur.Contents.UnderlyingTsString,
					paraRev.Contents.UnderlyingTsString, out ichMin, out ichLimCur, out ichLimRev,
					null, ref sCharStyleNameCurr, ref sCharStyleNameRev,
					ref sWsNameCurr, ref sWsNameRev);
				if (diffType != DifferenceType.NoDifference)
				{
					return new Difference(paraCur.Hvo, ichMin, ichLimCur,
						paraRev.Hvo, ichMin, ichLimRev, diffType,
						sCharStyleNameCurr, sCharStyleNameRev, sWsNameCurr, sWsNameRev);
				}
			}
			return null; // no difference
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns>requested paragraph or null if creation of paragraph fails</returns>
		/// ------------------------------------------------------------------------------------
		protected ScrTxtPara FindParagraph(int hvo)
		{
			try
			{
				// Check first if object really exists in database (this is done automatically
				// in debug builds, but not in release)
				if (m_cache.IsRealObject(hvo, ScrTxtPara.kClassId))
				{
					// Don't want to get assert on bad HVO value
					if (hvo > 0)
						return new ScrTxtPara(m_cache, hvo);
				}
			}
			catch
			{
			}
			return null;
		}
		#endregion

		#region Methods/Properties for Auto-Merge
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets whether DetectDifferences should also attempt to merge the saved or
		/// imported version into the current version if no overlapping sections (i.e., all
		/// differences are SectionMissingInCurrent or SectionAddedToCurrent and no (identical)
		/// sections in the current and revision overlap).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AttemptAutoMerge
		{
			get { return m_fAttemptAutoMerge; }
			set { m_fAttemptAutoMerge = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the book version agent.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IBookVersionAgent BookVersionAgent
		{
			set { m_bookVersionAgent = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether DetectDifferences was successful in merging the saved or imported
		/// version into the current version
		/// </summary>
		/// <value><c>true</c> if there were no overlapping sections.</value>
		/// ------------------------------------------------------------------------------------
		public bool AutoMerged
		{
			get { return m_fAutoMergeSucceeded; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to auto merge the saved or imported version into the current version if
		/// no overlapping sections (i.e., all differences are SectionMissingInCurrent or
		/// SectionAddedToCurrent and no (identical) sections in the current and revision
		/// overlap).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AutoMerge()
		{
			m_fAutoMergeSucceeded = false;
			if (!IsAutoMergePossible)
				return;
			Debug.Assert(Differences.Count > 0);

			// This undo task will eventually get rolled into the outer Import task, but we
			// need to do this so that the low-level back translation code won't think we're
			// outside any undo task (because it suppresses the undo actions in that case).
			using (new UndoRedoTaskHelper(Cache, "Undo AutoMerge", "Redo AutoMerge"))
			{
				if (m_progressDlg != null)
				{
					m_progressDlg.Message = string.Format(
							DlgResources.ResourceString("kstidAutoMerging"), BookCurr.BestUIName);
					m_progressDlg.SetRange(0, Differences.Count);
					m_progressDlg.Position = 0;
				}

				if (m_bookVersionAgent != null)
					m_bookVersionAgent.MakeBackupIfNeeded(this);
				MergeRevIntoCurr();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Auto-merges the revision title and/or new sections into the current version. This
		/// is used for auto-merge and partial book overwrite.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void MergeRevIntoCurr()
		{
			Difference diff = Differences.MoveFirst();
			while (diff != null)
			{
				switch (diff.DiffType)
				{
					case DifferenceType.SectionMissingInCurrent:
						ReplaceCurrentWithRevision(diff);
						diff = Differences.CurrentDifference;
						break;
					case DifferenceType.TextDifference:
						//IsTitleParaDiff
						if (IsTitleMissingInCurrent(diff))
						{
							ReplaceCurrentWithRevision(diff);
							diff = Differences.CurrentDifference;
						}
						else
							diff = Differences.MoveNext();
						break;
					default:
						diff = Differences.MoveNext();
						break;
				}
				if (m_progressDlg != null)
					m_progressDlg.Position++;
			}
			m_fAutoMergeSucceeded = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether auto merge is possible for the current differences.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsAutoMergePossible
		{
			get
			{
				if (m_fCannotMergeBecauseOfTitleDifference)
					return false;
				foreach (Cluster cluster in m_sectionOverlapClusterList)
				{
					if (cluster.clusterType != ClusterType.MissingInCurrent &&
						cluster.clusterType != ClusterType.AddedToCurrent)
					{
						return false;
					}
				}
				return true;
			}
		}
		#endregion

		#region Methods for support of partial overwrite
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs a partial overwrite of the book using the title and/or sections of the
		/// version in m_bookRev.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">A single parameter, which is a list of sections to remove.
		/// </param>
		/// <returns>Always <c>null</c>.</returns>
		/// <remarks>This version is called from the ProgressDialogWithTask class and runs
		/// on a background thread.</remarks>
		/// ------------------------------------------------------------------------------------
		public object DoPartialOverwrite(IAdvInd4 progressDlg, object[] parameters)
		{
			if (parameters.Length != 1 || !(parameters[0] is List<IScrSection>))
				throw new ArgumentException("parameters must contain a list of sections to remove.");
			m_progressDlg = progressDlg;
			DoPartialOverwrite((List<IScrSection>)parameters[0]);
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs a partial overwrite of the book using the title and/or sections of the
		/// version in m_bookRev.
		/// </summary>
		/// <param name="sectionsToRemove">The sections to remove as the first step (before
		/// calling the auto-merge code).</param>
		/// ------------------------------------------------------------------------------------
		public void DoPartialOverwrite(List<IScrSection> sectionsToRemove)
		{
			const int kStepsToCalcDiffs = 2;
			m_fCannotMergeBecauseOfTitleDifference = false;
			m_fAutoMergeSucceeded = false;

			// This undo task will eventually get rolled into an outer undo task, but we
			// need to do this so that the low-level back translation code won't think we're
			// outside any undo task (because it suppresses the undo actions in that case).
			using (new UndoRedoTaskHelper(Cache, "Undo Partial Overwrite", "Redo Partial Overwrite"))
			{
				if (m_progressDlg != null)
				{
					m_progressDlg.Message = string.Format(
							TeResourceHelper.GetResourceString("kstidOverwriting"), BookCurr.BestUIName);
					m_progressDlg.SetRange(0, Differences.Count + sectionsToRemove.Count + kStepsToCalcDiffs);
					m_progressDlg.Position = 0;
				}

				if (m_bookVersionAgent != null)
					m_bookVersionAgent.MakeBackupIfNeeded(this);

				if (m_bookCurr.TitleOA != null && m_bookRev.TitleOA != null &&
					m_bookRev.TitleOA.ParagraphsOS.Count > 0 &&
					((StTxtPara)m_bookRev.TitleOA.ParagraphsOS[0]).Contents.Length > 0)
				{
					OverwriteTitle();
				}
				((ScrBook)m_bookCurr).RemoveSections(sectionsToRemove, m_progressDlg);

				if (m_bookCurr.SectionsOS.Count > 0)
				{
					// Some of the current book remains. We need to perform a diff with the revision
					// to determine which sections to merge into the current.
					Differences.Clear();
					DetectDifferencesInBook();
					if (m_progressDlg != null)
						m_progressDlg.Position += kStepsToCalcDiffs;
					MergeRevIntoCurr();
				}
				else
				{
					// The current book had all sections removed, so all sections in the revision
					// need to be copied to the current.
					Debug.Assert(m_bookRev.SectionsOS.Count > 0);
					foreach (int hvo in m_bookRev.SectionsOS.HvoArray)
						InsertSection(hvo);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrites the title of the current with that of the revision version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OverwriteTitle()
		{
			while (m_bookCurr.TitleOA.ParagraphsOS.Count > m_bookRev.TitleOA.ParagraphsOS.Count)
				m_bookCurr.TitleOA.ParagraphsOS.RemoveAt(0);
			int iPara = 0;
			foreach (StTxtPara titleParaRev in m_bookRev.TitleOA.ParagraphsOS)
			{
				StTxtPara titleParaCur;
				if (iPara < m_bookCurr.TitleOA.ParagraphsOS.Count)
				{
					titleParaCur = new StTxtPara(Cache,
						m_bookCurr.TitleOA.ParagraphsOS.HvoArray[iPara]);
				}
				else
				{
					titleParaCur = new StTxtPara();
					m_bookCurr.TitleOA.ParagraphsOS.Append(titleParaCur);
				}
				titleParaCur.StyleRules = titleParaRev.StyleRules;
				titleParaCur.Contents.UnderlyingTsString =
					titleParaRev.Contents.UnderlyingTsString;
			}
		}
		#endregion

		#region Methods to help prevent data loss
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selects the difference list to use in the BookMerger which has the
		/// SectionAddedToCurrent diffs removed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UseFilteredDiffList()
		{
			if (!m_fUsingFilteredDiffs)
			{
				m_fUsingFilteredDiffs = true;
				RemoveSectionAddedDiffs();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove section added differences that would cause data loss, if reverted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RemoveSectionAddedDiffs()
		{
			if (m_allDifferences == null)
			{
				Debug.Assert(m_differences.Count == 0);
				return;
			}
			DifferenceList localDiffList = m_allDifferences.Clone();
			Difference diff = localDiffList.MoveFirst();
			do
			{
				diff = localDiffList.CurrentDifference;
				if (diff != null && (diff.DiffType & DifferenceType.SectionAddedToCurrent) != 0 &&
					!FindDifference(diff.SubDiffsForParas, DifferenceType.VerseMoved))
				{
					// We want to remove all SectionAddedToCurrent differences, except for VerseMoved
					// diffs (which have a root diff type of SectionAddedToCurrent).
					localDiffList.Remove(diff);
				}
				else
					diff = localDiffList.MoveNext();
			} while (diff != null);

			m_filteredDifferences = localDiffList;
			m_differences.Clear();
			m_differences.AddRange(localDiffList);
			m_origDiffCount = m_filteredDifferences.Count;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the given diff could potentially cause data loss, if reverted.
		/// </summary>
		/// <param name="diff">given difference</param>
		/// <returns><c>true</c> if no content is referenced in the revision; <c>false</c>otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		internal bool IsDataLossDifference(Difference diff)
		{
			// If content is added to the current, it is a data loss difference.
			// NOTE: Most of the time SectionAddedToCurrent and ParagraphAddedToCurrent differences
			//  are also data loss differences. However, there are some exceptions that are handled below.
			if ((diff.DiffType & DifferenceType.VerseAddedToCurrent) != 0 ||
				(diff.DiffType & DifferenceType.FootnoteAddedToCurrent) != 0 ||
				(diff.DiffType & DifferenceType.PictureAddedToCurrent) != 0)
			{
				return true;
			}

			// If content is missing in current, a revert would not cause data loss.
			if ((diff.DiffType & DifferenceType.SectionMissingInCurrent) != 0 ||
				(diff.DiffType & DifferenceType.SectionHeadMissingInCurrent) != 0 ||
				(diff.DiffType & DifferenceType.ParagraphMissingInCurrent) != 0 ||
				(diff.DiffType & DifferenceType.StanzaBreakMissingInCurrent) != 0 ||
				(diff.DiffType & DifferenceType.VerseMissingInCurrent) != 0 ||
				(diff.DiffType & DifferenceType.FootnoteMissingInCurrent) != 0 ||
				(diff.DiffType & DifferenceType.PictureMissingInCurrent) != 0)
			{
				return false;
			}

			// If content is referenced in both the revision and current, the user
			// would want to evaluate the difference.
			if ((diff.DiffType & DifferenceType.TextDifference) != 0 ||
				(diff.DiffType & DifferenceType.ParagraphMergedInCurrent) != 0 ||
				(diff.DiffType & DifferenceType.CharStyleDifference) != 0 ||
				(diff.DiffType & DifferenceType.MultipleCharStyleDifferences) != 0 ||
				(diff.DiffType & DifferenceType.ParagraphStyleDifference) != 0 ||
				(diff.DiffType & DifferenceType.FootnoteDifference) != 0 ||
				(diff.DiffType & DifferenceType.PictureDifference) != 0 ||
				(diff.DiffType & DifferenceType.WritingSystemDifference) != 0 ||
				(diff.DiffType & DifferenceType.MultipleWritingSystemDifferences) != 0 ||
				// The following diff types are added content, but the user may want to
				//  evaluate them.
				(diff.DiffType & DifferenceType.SectionHeadAddedToCurrent) != 0 ||
				(diff.DiffType & DifferenceType.StanzaBreakAddedToCurrent) != 0)
			{
				return false;
			}

			// ParagraphAddedToCurrent differences are not considered data loss differences
			//  if they occur with other differences at the same verse reference.
			if ((diff.DiffType & DifferenceType.ParagraphAddedToCurrent) != 0)
			{
				// if we don't find a SectionHeadAddedToCurrent, then it is  a data loss difference.
				return !FindDifference(DiffHashTable[diff.RefStart.BBCCCVVV],
					DifferenceType.SectionHeadAddedToCurrent);
			}

			// Determine if a ParagraphSplitInCurrent difference refers only to content in the Current.
			// If so, it's a data loss difference.
			// TODO: When TE-7334 is fixed, this ParagraphSplitInCurrent difference would become
			//     a ParagraphsAddedToCurrent difference.
			//  For a ParagraphsAddedToCurrent in a multiple-paragraph verse to be considered a
			//  data loss difference:
			//  * first determine if a SectionHeadAddedToCurrent difference occurs at this verse reference
			//    (as is done above)
			//  * then, we would need to check the index of the paragraph in its owner. For this to
			//     not be considered data loss, its owning section must be the Section(Head)AddedToCurrent.
			//     The added paragraph would have to be the first paragraph of this added section.
			//     If we don't check the index of the section and index in owner of the paragraph, we
			//     won't know if the paragraph added is really added content or created as a result of
			//     an inserted section head.
			if ((diff.DiffType & DifferenceType.ParagraphSplitInCurrent) != 0)
				return CurrentHasContent(diff) && !RevisionHasContent(diff);

			// Search subdifferences for a difference that contains references to multiple paragraphs.
			if (diff.SubDiffsForParas != null && diff.SubDiffsForParas.Count > 0)
			{
				Debug.Assert((diff.DiffType & DifferenceType.ParagraphStructureChange) != 0 ||
					(diff.DiffType & DifferenceType.SectionAddedToCurrent) != 0);

				foreach (Difference subDiff in diff.SubDiffsForParas)
				{
					// Verse moved diffs are contained in SectionAdded differences
					if ((subDiff.DiffType & DifferenceType.VerseMoved) != 0)
						return false; // The user would want to evaluate VerseMoved differences.

					// ParagraphAdded diffs may be contained in ParagraphStructureChange diffs
					if ((subDiff.DiffType & DifferenceType.ParagraphAddedToCurrent) != 0)
						return true; // ParagraphAddedToCurrent is a data loss difference
				}

				if ((diff.DiffType & DifferenceType.ParagraphStructureChange) != 0)
					return false; // no data loss subdiffs located.
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build a lookup table for searching differences.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected Dictionary<int, List<Difference>> BuildDiffLookupTable()
		{
			Dictionary<int, List<Difference>> diffHashTable = new Dictionary<int, List<Difference>>();

			Difference diff = m_allDifferences.MoveFirst();
			while (diff != null)
			{
				int refStart = diff.RefStart.BBCCCVVV;
				if (!diffHashTable.ContainsKey(refStart))
					diffHashTable.Add(refStart, new List<Difference>());
				diffHashTable[refStart].Add(diff.Clone());

				diff = m_allDifferences.MoveNext();
			}
			m_allDifferences.MoveFirst();

			return diffHashTable;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the given difference references content in the current.
		/// </summary>
		/// <param name="diff">given difference</param>
		/// <returns><c>true</c> if the difference references content in the current</returns>
		/// ------------------------------------------------------------------------------------
		protected bool CurrentHasContent(Difference diff)
		{
			if (diff.IchLimCurr - diff.IchMinCurr > 0)
				return true;

			// Search subdifferences for a difference that refers to a range of text.
			if (diff.SubDiffsForParas != null && diff.SubDiffsForParas.Count > 0)
			{
				foreach (Difference subDiff in diff.SubDiffsForParas)
				{
					if (subDiff.IchLimCurr - subDiff.IchMinCurr > 0)
						return true;
				}
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the given difference references content in the revision.
		/// </summary>
		/// <param name="diff">given difference</param>
		/// <returns><c>true</c> if the difference references content in the current</returns>
		/// ------------------------------------------------------------------------------------
		protected bool RevisionHasContent(Difference diff)
		{
			if (diff.IchLimRev - diff.IchMinRev > 0)
				return true;

			// Search subdifferences for a difference that refers to a range of text.
			if (diff.SubDiffsForParas != null && diff.SubDiffsForParas.Count > 0)
			{
				foreach (Difference subDiff in diff.SubDiffsForParas)
				{
					if (subDiff.IchLimRev - subDiff.IchMinRev > 0)
						return true;
				}
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a difference of the specified type(s) within the specified references.
		/// </summary>
		/// <param name="diffList">list of differences to search</param>
		/// <param name="startRef">starting reference for the diff</param>
		/// <param name="endRef">ending reference for the diff</param>
		/// <param name="diffType">difference type(s) to find</param>
		/// <returns><c>true</c> if a diff of the specified type(s) was (were) found;
		/// <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		private static bool FindDifference(DifferenceList diffList, BCVRef startRef, BCVRef endRef,
			DifferenceType diffType)
		{
			Difference diff = diffList.MoveFirst();
			while (diff != null)
			{
				if (diff.RefStart >= startRef && diff.RefEnd <= endRef &&
					(diff.DiffType & diffType) != 0)
				{
					return true;
				}

				diff = diffList.MoveNext();
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a difference of the specified type(s) for a list of differences beginning at
		/// the same reference.
		/// </summary>
		/// <param name="diffAtRef">difference(s) at the same reference.</param>
		/// <param name="diffType">type(s) of difference to find.</param>
		/// <returns><c>true</c> if difference  of specified diffType is found.</returns>
		/// ------------------------------------------------------------------------------------
		private static bool FindDifference(List<Difference> diffAtRef, DifferenceType diffType)
		{
			if (diffAtRef == null)
				return false;

			foreach (Difference diff in diffAtRef)
			{
				if ((diff.DiffType & diffType) != 0)
					return true;
			}

			return false;
		}
		#endregion

		#region NextVerse helper methods for DetectDifferences
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper method to initialize endOfPrevVerse to the start of the first heading in the
		/// given book.
		/// </summary>
		/// <param name="book">The Scripture book</param>
		/// <param name="endOfPrevVerse">the output</param>
		/// ------------------------------------------------------------------------------------
		protected void InitPrevVerseForBook(IScrBook book, out ScrVerse endOfPrevVerse)
		{
			Debug.Assert(book.SectionsOS.Count > 0);

			// init endOfPrevVerse to very beginning of first section heading
			IScrSection FirstSection = book.SectionsOS[0];
			Debug.Assert(FirstSection.HeadingOA.ParagraphsOS.Count > 0);
			if (FirstSection.HeadingOA.ParagraphsOS.Count == 0)
				FirstSection.HeadingOA.ParagraphsOS.Append(new StTxtPara());
			endOfPrevVerse = new ScrVerse(ScrReference.Empty, ScrReference.Empty, null,
				new ParaNodeMap((StTxtPara)FirstSection.HeadingOA.ParagraphsOS[0]),
				FirstSection.HeadingOA.ParagraphsOS[0].Hvo,
				FirstSection.HeadingOA.ParagraphsOS[0].OwnerHVO, 0, 0, false, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper method creates an endOfPrevVerse, based on the start of the first paragraph
		/// in the given iterator.
		/// Then returns the next verse from the given iterator.
		/// If iterator contains no StTexts, we handle it appropriately.
		/// </summary>
		/// <param name="iterator">given iterator</param>
		/// <param name="endOfPrevVerse">the output, if iterator is available</param>
		/// <returns>the next verse, or null if none available</returns>
		/// ------------------------------------------------------------------------------------
		protected ScrVerse FirstVerseForStText(IVerseIterator iterator, ref ScrVerse endOfPrevVerse)
		{
			if (iterator.ParagraphHvo == 0)
			{
				// No paragraph means there were no StTexts to iterate.
				// (I.e. the iterator was constructed with an empty list of StTexts.)
				// In this case, leave endOfPrevVerse as is from last use.
				return null; // no verse to return
			}
			else
			{
				endOfPrevVerse = new ScrVerse(ScrReference.Empty, ScrReference.Empty, null,
					new ParaNodeMap(iterator.Paragraph), iterator.ParagraphHvo,
					iterator.ParagraphOwnerHvo, 0, 0, false, false);
				ScrVerse nextVerse = iterator.NextVerse();
				if (nextVerse == null)
					return endOfPrevVerse;
				return nextVerse;
			}
		}

		//		/// ------------------------------------------------------------------------------------
		//		/// <summary>
		//		/// Helper method creates an endOfPrevVerse (based on the given previous verse and structure).
		//		/// Then returns the next verse from the given iterator.
		//		/// </summary>
		//		/// <param name="previousVerse">The previous ScrVerse</param>
		//		/// <param name="iterator">Iterator that is used to advance to next verse</param>
		//		/// <param name="endOfPrevVerse">output: ScrVerse set to the end position of the given
		//		/// previous verse </param>
		//		/// <returns>the Next verse</returns>
		//		/// ------------------------------------------------------------------------------------
		//		private ScrVerse NextVerse(ScrVerse previousVerse, IVerseIterator iterator,
		//			out ScrVerse endOfPrevVerse)
		//		{
		//			endOfPrevVerse = new ScrVerse(previousVerse.StartRef, previousVerse.EndRef, null,
		//				previousVerse.Hvo, previousVerse.VerseStartIndex + previousVerse.Text.Length, 0,
		//				previousVerse.VerseNumberRun);
		//			return iterator.NextVerse();


		//REVIEW: TE-2861 problem: If newVerse is null, it is possible that the iterator
		// passed over empty pararaphs just before it encountered the end of the book.
		// At times, especially when it passed over an empty section heading or contents,
		// it would be better to set the endOfPrevVerse to the empty para rather than to
		// the end of the previousVerse as we do now.

		//			ScrVerse newVerse = iterator.NextVerse();
		//
		//			// if the new verse exists and is the same in structure as the previous
		//			if (newVerse == null || newVerse.IsHeading == previousVerse.IsHeading)
		//			{
		//				// set up our endOfPrevVerse to be at the previous verse
		//				endOfPrevVerse = new ScrVerse(previousVerse.StartRef, previousVerse.EndRef, null,
		//					previousVerse.Hvo, previousVerse.VerseStartIndex + previousVerse.Text.Length,
		//					previousVerse.IsHeading);
		//			}
		//			else
		//			// the new verse is a change in structure from the previous
		//			{
		//				// set up our endOfPrevVerse to be at the begining of the new verse's paragraph
		//				endOfPrevVerse = new ScrVerse(previousVerse.StartRef, previousVerse.EndRef, null,
		//					newVerse.Hvo, 0, newVerse.IsHeading);
		//			}
		//			return newVerse;
		//
		//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a ScrVerse that holds information about the end of a source verse
		/// </summary>
		/// <param name="source">ScrVerse to use as a source</param>
		/// <returns>a ScrVerse that represents the end of the verse location</returns>
		/// ------------------------------------------------------------------------------------
		private ScrVerse MakeEndOfPreviousVerse(ScrVerse source)
		{
			return new ScrVerse(source.StartRef, source.EndRef, null, source.ParaNodeMap,
				source.HvoPara, source.HvoParaOwner, source.VerseStartIndex + source.TextLength, 0,
				source.ChapterNumberRun, source.VerseNumberRun);
		}
		#endregion

		#region Cluster helper methods for DetectDifferences
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if this cluster of ScrVerses spans more than one StText (i.e. a section
		/// break) in either the current or revision.
		/// </summary>
		/// <param name="cluster">The cluster of ScrVerses.</param>
		/// <returns><c>true</c>if it does; <c>false</c> if not</returns>
		/// ------------------------------------------------------------------------------------
		//REVIEW: simplify algorithm; make a property of Cluster??
		private bool ClusterSpansStTexts(Cluster cluster)
		{
			// We need to keep track of the paragraphs that go with each StText for the current
			// and the revision.
			Dictionary<int, List<int>> stTextsToParasCurr;
			Dictionary<int, List<int>> stTextsToParasRev;

			stTextsToParasCurr = GetStTextToParaMapping(cluster.itemsCurr);
			stTextsToParasRev = GetStTextToParaMapping(cluster.itemsRev);

			// If either the current or revision has more than one StText as a key...
			if (stTextsToParasCurr.Keys.Count > 1 || stTextsToParasRev.Keys.Count > 1)
				return true; // the cluster spans different StTexts in the current or the revision

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets mapping from StTexts to StTxtParas.
		/// </summary>
		/// <param name="infoList">The list of either current or revision paragraphs that are
		/// in the cluster.</param>
		/// <returns>a dictionary with each entry consisting of an StText and all of the
		/// StTxtParas that it owns in the current cluster.</returns>
		/// ------------------------------------------------------------------------------------
		private Dictionary<int, List<int>> GetStTextToParaMapping(List<OverlapInfo> infoList)
		{
			Dictionary<int, List<int>> stTextToParas = new Dictionary<int, List<int>>();
			int hvoPrevPara = 0;
			for (int iItem = 0; iItem < infoList.Count; iItem++)
			{
				// if we found a new paragraph...
				if (hvoPrevPara == 0 || hvoPrevPara != infoList[iItem].myHvo)
				{
					// add it to a dictionary that keeps track of which StText owns each
					// paragraph in the cluster.
					StTxtPara para = new StTxtPara(m_cache, infoList[iItem].myHvo);

					if (!stTextToParas.ContainsKey(para.OwnerHVO))
						stTextToParas.Add(para.OwnerHVO, new List<int>());
					stTextToParas[para.OwnerHVO].Add(para.Hvo);
				}
			}

			return stTextToParas;
		}
		#endregion

		#region VerseIterator for StText internal classes
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This interface is implemented by two different VerseIterator classes.
		/// The DetectDifferences code can utilize either iterator via this interface.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected interface IVerseIterator
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Get the next verse (or non-verse paragraph) in the iterator.
			/// </summary>
			/// <returns>the next ScrVerse, or null if we reached the end of the iterator's
			/// data</returns>
			/// --------------------------------------------------------------------------------
			ScrVerse NextVerse();

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the hvo of the paragraph of the iterator's current ScrVerse.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			int ParagraphHvo
			{ get; }

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the paragraph's owner's hvo.
			/// </summary>
			/// --------------------------------------------------------------------------------
			int ParagraphOwnerHvo
			{ get; }

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the paragraph of the iterator's current ScrVerse.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			StTxtPara Paragraph
			{ get; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This class will iterate through a set of StTexts verse by verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected class VerseIteratorForSetOfStTexts : IVerseIterator
		{
			// given list
			private List<IStText> m_stTextList;
			// index in list
			private int m_iStText;
			// iterator for a single StText
			private VerseIteratorForStText m_verseIteratorForStText = null;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Constructor for a VerseIterator for a given list of StTexts.
			/// </summary>
			/// <param name="stTextList">the given list of StText objects</param>
			/// --------------------------------------------------------------------------------
			public VerseIteratorForSetOfStTexts(List<IStText> stTextList)
			{
				m_stTextList = stTextList;
				m_iStText = 0;

				if (m_stTextList.Count > 0)
				{
					m_verseIteratorForStText =
						new VerseIteratorForStText(m_stTextList[m_iStText]);
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Get the next verse (or non-verse paragraph) in the StTexts.
			/// </summary>
			/// <returns>the next ScrVerse, or null if we reached the end of the StTexts</returns>
			/// --------------------------------------------------------------------------------
			public ScrVerse NextVerse()
			{
				if (m_verseIteratorForStText == null || m_iStText >= m_stTextList.Count)
					return null;

				// Another verse in the current stText?
				ScrVerse scrVerse = m_verseIteratorForStText.NextVerse();
				if (scrVerse != null)
					return scrVerse; // Return it!!!

				if (StTextContainsOnlyOneEmptyPara)
					return EmptyScrVerse;

				// There are no more verses in the current StText. Search through subsequent
				//  StTexts in our list for the next ScrVerse.
				m_iStText++;

				// Search for next ScrVerse in this StText's paragraphs, if there is one.
				while (m_iStText < m_stTextList.Count)
				{
					// Init the verse enumerator for this StText
					m_verseIteratorForStText =
						new VerseIteratorForStText(m_stTextList[m_iStText]);

					if (StTextContainsOnlyOneEmptyPara)
						return EmptyScrVerse;

					// Do we have a new ScrVerse in this StText?
					scrVerse = m_verseIteratorForStText.NextVerse();
					if (scrVerse != null)
						return scrVerse; // Return it!!!

					// No verses in this StText. Move to next StText in the List.
					m_iStText++;
				}
				return null; // no more verses--end of the set of StTexts
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the hvo of the current paragraph of the current StText.
			/// If the list of StTexts was empty, this gets 0.
			/// If the current paragraph has no text, this will still get the hvo, though the
			/// NextVerse() method may return a null.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public int ParagraphHvo
			{
				get
				{
					if (m_verseIteratorForStText == null)
						return 0; // no StText, so there's no paragraph
					else
						return m_verseIteratorForStText.ParagraphHvo;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the hvo of the owner of the current paragraph (i.e. the hvo of the current
			/// StText).
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public int ParagraphOwnerHvo
			{
				get
				{
					return m_verseIteratorForStText.ParagraphOwnerHvo;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the paragraph of the iterator's current ScrVerse.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public StTxtPara Paragraph
			{
				get
				{
					if (m_verseIteratorForStText == null)
						return null; // no StText, so there's no paragraph
					else
						return m_verseIteratorForStText.Paragraph;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the section of the iterator's current ScrVerse.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			private ScrSection Section
			{
				get
				{
					if (m_iStText >= m_stTextList.Count || m_stTextList[m_iStText] == null)
						return null;

					return (ScrSection)((StText)m_stTextList[m_iStText]).Owner;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets whether the current StText has only one empty paragraph.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			private bool StTextContainsOnlyOneEmptyPara
			{
				get
				{
					if (m_iStText >= m_stTextList.Count)
						return false;

					StText contents = ((StText)m_stTextList[m_iStText]);
					int firstParaLength = ((StTxtPara)contents.ParagraphsOS[0]).Contents.Length;

					return contents.ParagraphsOS.Count == 1 && firstParaLength == 0;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets an empty ScrVerse for the current (empty) StText.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			private ScrVerse EmptyScrVerse
			{
				get
				{
					Debug.Assert(StTextContainsOnlyOneEmptyPara,
						"Should not return an empty ScrVerse for a section with content.");
					// The current StText has an empty paragraph. Return it.
					try
					{
						// return empty ScrVerse and go on to next StText
						Debug.Assert(Section != null);
						Debug.Assert(Paragraph != null);
						string styleName = ScrStyleNames.GetStyleName(Paragraph.Hvo, Paragraph.Cache);
						ScrVerse emptyVerse = new ScrVerse(Section.VerseRefMin, Section.VerseRefMax,
							Paragraph.Contents.UnderlyingTsString, new ParaNodeMap(Paragraph),
							Paragraph.Hvo, Paragraph.OwnerHVO, string.Equals(ScrStyleNames.StanzaBreak, styleName));
						return emptyVerse;
					}
					finally
					{
						// This needs to be incremented to the next StText for the next verse.
						m_iStText++;

						if (m_iStText < m_stTextList.Count)
						{
							// Init the verse enumerator for this StText
							m_verseIteratorForStText =
								new VerseIteratorForStText(m_stTextList[m_iStText]);
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This class will iterate through a single scripture StText verse by verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected class VerseIteratorForStText : IVerseIterator
		{
			private IStText m_StText;

			// the current paragraph
			private int m_iPara;
			private ScrTxtPara m_scrPara;

			// m_verseSet is the enumerator that we utilize within the current paragraph
			private ScrVerseSet m_verseSet;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Constructor for a VerseIterator in the given StText.
			/// </summary>
			/// <param name="stText">given StText</param>
			/// --------------------------------------------------------------------------------
			public VerseIteratorForStText(IStText stText)
			{
				m_StText = stText;
				Debug.Assert(m_StText.ParagraphsOS.Count > 0);
				if (m_StText.ParagraphsOS.Count == 0)
					m_StText.ParagraphsOS.Append(new StTxtPara());

				m_iPara = 0;
				m_scrPara = new ScrTxtPara(m_StText.Cache, m_StText.ParagraphsOS[m_iPara].Hvo);
				m_verseSet = (ScrVerseSet)m_scrPara.GetEnumerator();
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Get the next verse (or non-verse paragraph) in the StText.
			/// </summary>
			/// <returns>the next ScrVerse, or null if we reached the end of the StText</returns>
			/// --------------------------------------------------------------------------------
			public ScrVerse NextVerse()
			{
				if (m_verseSet == null)
					return null;

				// Another verse in the current paragraph?
				if (m_verseSet.MoveNext())
					return (ScrVerse)m_verseSet.Current; // Return it!!!

					// There are no more verses in the current paragraph. Search through subsequent
					//  paragraphs in this section (and, if needed, subsequent sections in this
					//  book) for the next paragraph with a ScrVerse.
				else
				{
					m_iPara++;

					// Search for next ScrVerse in this StText's paragraphs, if there is one.
					while (m_iPara < m_StText.ParagraphsOS.Count)
					{
						m_scrPara = new ScrTxtPara(m_StText.Cache,
							m_StText.ParagraphsOS[m_iPara].Hvo);

						// Init the verse enumerator for this para
						m_verseSet = (ScrVerseSet)m_scrPara.GetEnumerator();

						// Do we have a new ScrVerse in this para?
						if (m_verseSet.MoveNext())
							return (ScrVerse)m_verseSet.Current; // Got it!!!

						// No verses in this para. Move to next paragraph in StText.
						m_iPara++;
					}
				}
				return null; // no more verses--end of StText
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the hvo of the current paragraph.
			/// If the current paragraph has no text, this will still get the hvo, though the
			/// NextVerse() method may return a null.
			/// If we've reached the end of the StText, this will return the hvo of the last paragraph.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public int ParagraphHvo
			{
				get { return m_scrPara.Hvo; }
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the hvo of the owner of the current paragraph (i.e. the StText).
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public int ParagraphOwnerHvo
			{
				get { return m_scrPara.OwnerHVO; }
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the paragraph of the iterator's current ScrVerse.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public StTxtPara Paragraph
			{
				get { return m_scrPara; }
			}
		}
		#endregion

		#region ReplaceCurrentWithRevision (for user actions in DiffDialog)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a difference, replace the current with the stuff in the revision.
		/// </summary>
		/// <param name="diff"></param>
		/// ------------------------------------------------------------------------------------
		internal void ReplaceCurrentWithRevision(Difference diff)
		{
			CheckDisposed();
			m_parasNeedingUpdateMainTrans = new List<StTxtPara>();

			// TODO (TE-7099, TE-7108): Now that reverting some section or para structure diffs
			// can split a Current para, we should check if a diff of type VerseAdded or
			// VerseMissing now encompasses an entire paragraph. Changing the diffType might
			// muck up the difference list, but we should process it here as a ParagraphAdded
			// or Missing. Conversely, reverting some section or para structure diffs can
			// combine Current paras, so we should check if a diff of type ParaAdded or
			// ParaMissing now encompasses less than an entire paragraph. If so, process it here
			// as VerseAdded or VerseMissing.
			switch(diff.DiffType)
			{
				case DifferenceType.SectionMissingInCurrent:
					ReplaceCurrentWithRevision_InsertSection(diff);
					break;
				case DifferenceType.SectionAddedToCurrent:
					if (diff.HasParaSubDiffs)
					{
						ReplaceCurrentWithRevision_MoveVersesBack(diff.SubDiffsForParas);
						FixCurrParaHvosForSectionDelete(diff);
					}
					ReplaceCurrentWithRevision_RemoveSection(diff);
					break;
				case DifferenceType.SectionHeadMissingInCurrent:
					ReplaceCurrentWithRevision_InsertSectionBreak(diff);
					break;
				case DifferenceType.SectionHeadAddedToCurrent:
					ReplaceCurrentWithRevision_RemoveSectionBreak(diff);
					break;
				case DifferenceType.ParagraphMissingInCurrent:
				case DifferenceType.StanzaBreakMissingInCurrent:
					ReplaceCurrentWithRevision_InsertPara(diff);
					break;
				case DifferenceType.ParagraphAddedToCurrent:
				case DifferenceType.StanzaBreakAddedToCurrent:
					ReplaceCurrentWithRevision_RemovePara(diff);
					break;
				//case DifferenceType.ParagraphSplitInCurrent:
				//    ReplaceCurrentWithRevision_RemoveParaBreak(diff);
				//    break;
				case DifferenceType.ParagraphStructureChange:
				case DifferenceType.ParagraphSplitInCurrent:
				case DifferenceType.ParagraphMergedInCurrent:
					ReplaceCurrentWithRevision_CopyParaStructure(diff);
					break;
				default:
					ReplaceCurrentWithRevision_WithinPara(diff);
					if ((diff.DiffType & DifferenceType.ParagraphStyleDifference) != 0)
					{
						// apply rev para style to the current paragraph
						// (should never get here if the paragraph was removed)
						StTxtPara paraCurr = new StTxtPara(m_cache, diff.HvoCurr);
						StTxtPara paraRev = new StTxtPara(m_cache, diff.HvoRev);
						paraCurr.StyleRules = paraRev.StyleRules;
					}
					break;
			}
			MarkDifferenceAsReviewed(diff);
			// try resorting - nice when verses were moved
			//m_differences.SortIfNeeded();
			foreach (StTxtPara para in m_parasNeedingUpdateMainTrans)
				FreeTransEditMonitor.UpdateMainTransFromSegmented(para, BtWss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Notes that the given paragraph's segmented back translation has changed, so it's
		/// CmTranslation also needs to be updated.
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// ------------------------------------------------------------------------------------
		private void NoteParaNeedsUpdateMainTrans(StTxtPara para)
		{
			// Don't use "Contains", equality doesn't work on CmObjects.
			foreach (StTxtPara p in m_parasNeedingUpdateMainTrans )
				if (p.Hvo == para.Hvo)
					return; // we already know about it.
			m_parasNeedingUpdateMainTrans.Add(para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixes all of the current paragraph hvos for the section that will be deleted.
		/// </summary>
		/// <param name="diff">The SectionAdded difference which must have VerseMoved
		/// subdifferences</param>
		/// ------------------------------------------------------------------------------------
		private void FixCurrParaHvosForSectionDelete(Difference diff)
		{
			Debug.Assert(diff.HvosSectionsCurr.Length == 1);
			ScrSection section = new ScrSection(m_cache, diff.HvosSectionsCurr[0]);

			// Get the new paragraphIP;
			//  this will be the same place that the moved verses are being moved back to
			Debug.Assert(diff.SubDiffsForParas.Count > 0);
			Difference subDiff = (Difference)diff.SubDiffsForParas[0];
			int newParaHvo = subDiff.HvoMovedFrom;
			Debug.Assert(newParaHvo > 0);
			int newIch = subDiff.IchMovedFrom;

			// fix all of the content paragraphs in the section
			foreach (StTxtPara para in section.ContentOA.ParagraphsOS)
				m_differences.FixCurrParaHvosAndSetIch(para.Hvo, newParaHvo, newIch, newIch);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a verse difference, replace the difference range of the current verse with
		/// the difference text of the revision verse fixing up any ORCs along the way.
		/// </summary>
		/// <param name="diff">The object that encapsulates the difference</param>
		/// ------------------------------------------------------------------------------------
		private void ReplaceCurrentWithRevision_WithinPara(Difference diff)
		{
			if (diff.DiffType == DifferenceType.ParagraphStyleDifference)
				return;

			StTxtPara paraCurr = DiffTextRangeReplace(diff);

			// Adjust offsets stored in the differences list
			int adjustment = (diff.IchLimRev - diff.IchMinRev) - (diff.IchLimCurr - diff.IchMinCurr);
			if (adjustment != 0)
				m_differences.AdjustFollowingOffsets(diff, adjustment);

			// adjust the references for the section that owns the changed paragraph
			ScrSection section = ScrSection.GetSectionFromParagraph(paraCurr);
			if (section != null)
				section.AdjustReferences();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replace current range of text with the revision range of text as specified in a
		/// difference. We take care of making copies of owned objects.
		/// </summary>
		/// <param name="diff">The specified difference.</param>
		/// <returns>the current paragraph with the text replacement</returns>
		/// ------------------------------------------------------------------------------------
		private StTxtPara DiffTextRangeReplace(Difference diff)
		{
			ITsStrBldr stringRev;
			StTxtPara paraRev = null;
			if (diff.HvoRev != 0)
			{
				// isolate the difference text of the Revision verse
				paraRev = new StTxtPara(m_cache, diff.HvoRev);
				PrepareParaForBtSegMerging(paraRev);
				stringRev = paraRev.Contents.UnderlyingTsString.GetBldr();
				stringRev.Replace(diff.IchLimRev, stringRev.Length, "", null);
				stringRev.Replace(0, diff.IchMinRev, "", null);
			}
			else // We don't have a corresponding text replacement in the revision.
				stringRev = TsStrBldrClass.Create();

			// replace the difference range of the Current verse
			Debug.Assert(diff.HvoCurr != 0);
			StTxtPara paraCurr = new StTxtPara(m_cache, diff.HvoCurr);

			// Get this info from the paragraph BEFORE we modify it.
			PrepareParaForBtSegMerging(paraCurr);
			int isegMinLowCurr, isegMinCurr, isegLimCurr;
			GetSegmentsOverlapping(paraCurr, diff.IchMinCurr, diff.IchLimCurr, out isegMinLowCurr, out isegMinCurr, out isegLimCurr);

			paraCurr.RemoveOwnedObjectsForString(diff.IchMinCurr, diff.IchLimCurr);
			ITsStrBldr strCurrBldr = paraCurr.Contents.UnderlyingTsString.GetBldr();
			strCurrBldr.ReplaceTsString(diff.IchMinCurr, diff.IchLimCurr, stringRev.GetString());
			//note: setting paraCurr.Contents fires a ChangeWatcher which should clear the status of all BTs
			paraCurr.Contents.UnderlyingTsString = strCurrBldr.GetString();
			paraCurr.CreateOwnedObjects(diff.IchMinCurr, diff.IchMinCurr + stringRev.Length, this);
			int isegMinLowRev, isegMinRev, isegLimRev;
			GetSegmentsOverlapping(paraRev, diff.IchMinRev, diff.IchLimRev, out isegMinLowRev, out isegMinRev, out isegLimRev);
			// Now we have to decide whether to include the previous verse when there is ambiguity.
			// If one is ambiguous and the other is not, we need to include the doubtful verse from the ambiguous one.
			if (isegMinCurr > isegMinLowCurr && isegMinRev == isegMinLowRev)
				isegMinCurr = isegMinLowCurr;
			else if (isegMinRev > isegMinLowRev && isegMinCurr == isegMinLowCurr)
				isegMinRev = isegMinLowRev;
			FixBtSegsForReplace(paraCurr, diff.IchMinCurr, diff.IchLimCurr, isegMinCurr, isegLimCurr,
								paraRev, diff.IchMinRev, diff.IchLimRev, isegMinRev, isegLimRev);
			return paraCurr;
		}


		/// <summary>
		/// Given (or pretending) that the range of characters from ichMinCurr to ichLimCurr in paraCurr has been replaced
		/// with the text from ichMinRev to ichLimRev in paraRev, replace the segments from iSegMinCurr to isegLimCurr in
		/// paraCurr with the ones from isegMinRev to isegLimRev in paraRev.
		/// </summary>
		private void FixBtSegsForReplace(StTxtPara paraCurr, int ichMinCurr, int ichLimCurr, int isegMinCurr, int isegLimCurr,
			StTxtPara paraRev, int ichMinRev, int ichLimRev, int isegMinRev, int isegLimRev)
		{
			// Adjust offsets of any later segments.
			int kflidSegments = StTxtPara.SegmentsFlid(m_cache);
			int kflidFT = StTxtPara.SegmentFreeTranslationFlid(m_cache);
			int cseg = m_cache.GetVectorSize(paraCurr.Hvo, kflidSegments);
			int adjustment = ichLimRev - ichMinRev - (ichLimCurr - ichMinCurr);
			for (int i = isegLimCurr; i < cseg; i++)
			{
				CmBaseAnnotation seg = new CmBaseAnnotation(m_cache, m_cache.GetVectorItem(paraCurr.Hvo, kflidSegments, i));
				seg.BeginOffset += adjustment;
				seg.EndOffset += adjustment;
			}
			// Replace segments from isegMinCurr to isegLimCurr with the ones from the revision.
			int csegReuse = Math.Min(isegLimRev - isegMinRev, isegLimCurr - isegMinCurr);
			int hvoSegAnnDefn = m_cache.GetIdFromGuid(LangProject.kguidAnnTextSegment);
			int ichEndPrevSeg = 0;
			if (isegMinCurr > 0)
				ichEndPrevSeg =
					new CmBaseAnnotation(m_cache, m_cache.GetVectorItem(paraCurr.Hvo, kflidSegments, isegMinCurr - 1)).EndOffset;
			// It's tempting to use paraCurr.Contents.Length as a limit; we'd like to check that.
			// However, in some cases, when concatenating two paragraphs into one, we copy a range of segments that
			// extends beyond the end of the paragraph, then copy the rest of the text later.
			// Enhance JohnT: if we make an invalid set of segments here, remember it somehow and check they get fixed.
			int ichLimNewSegs = int.MaxValue;
			if (isegLimCurr < cseg)
				ichLimNewSegs =
					new CmBaseAnnotation(m_cache, m_cache.GetVectorItem(paraCurr.Hvo, kflidSegments, isegLimCurr)).BeginOffset;
			ICmBaseAnnotation lastSegInserted = null;
			for (int i = isegMinCurr; i < isegMinCurr + csegReuse; i++)
			{
				CmBaseAnnotation segRev = new CmBaseAnnotation(m_cache, m_cache.GetVectorItem(paraRev.Hvo, kflidSegments,
					i + isegMinRev - isegMinCurr));
				CmBaseAnnotation segCurr = new CmBaseAnnotation(m_cache, m_cache.GetVectorItem(paraCurr.Hvo, kflidSegments, i));
				// In this range the text should be identical so we can just copy the segments.
				segCurr.BeginOffset = ichEndPrevSeg; // hopefully segRev.BeginOffset - ichMinRev + ichMinCurr;
				ichEndPrevSeg = Math.Max(segRev.EndOffset - ichMinRev + ichMinCurr, ichEndPrevSeg); // hopefully the first arg, but not out of order
				ichEndPrevSeg = Math.Min(ichEndPrevSeg, ichLimNewSegs); // hopefully no change, but ensure we keep in order.
				segCurr.EndOffset = ichEndPrevSeg;
				CopyBt(segCurr, segRev);
				lastSegInserted = segCurr;
			}
			// Copy any extra segments (will be 0 iterations if none).
			for (int i = 0; i < isegLimRev - isegMinRev - csegReuse; i++)
			{
				ICmBaseAnnotation segRev = new CmBaseAnnotation(m_cache, m_cache.GetVectorItem(paraRev.Hvo, kflidSegments,
					i + isegMinRev + csegReuse));
				ICmBaseAnnotation segCurr = CmBaseAnnotation.CreateUnownedCba(m_cache);
				segCurr.AnnotationTypeRAHvo = hvoSegAnnDefn;
				segCurr.BeginObjectRA = paraCurr;
				// In this range the text should be identical so we can just copy the segments.
				segCurr.BeginOffset = ichEndPrevSeg; // hopefully segRev.BeginOffset - ichMinRev + ichMinCurr;
				ichEndPrevSeg = Math.Max(segRev.EndOffset - ichMinRev + ichMinCurr, ichEndPrevSeg); // hopefully the first arg, but not out of order
				ichEndPrevSeg = Math.Min(ichEndPrevSeg, ichLimNewSegs); // hopefully no change, but ensure we keep in order.
				segCurr.EndOffset = ichEndPrevSeg;
				m_cache.VwCacheDaAccessor.CacheReplace(paraCurr.Hvo, kflidSegments, isegMinCurr + csegReuse + i,
					isegMinCurr + csegReuse + i, new int[] {segCurr.Hvo}, 1);
				CopyBt(segCurr, segRev);
				lastSegInserted = segCurr;
			}
			int csegDel = isegLimCurr - isegMinCurr - csegReuse;
			if(csegDel > 0)
			{
				// Delete any left-over segments.
				List<CmBaseAnnotation> badSegs = new List<CmBaseAnnotation>();
				for (int i = isegMinCurr + csegReuse; i < isegLimCurr; i++)
					badSegs.Add(new CmBaseAnnotation(m_cache, m_cache.GetVectorItem(paraCurr.Hvo, kflidSegments, i)));
				RemoveBtSegs(badSegs, paraCurr, isegMinCurr + csegReuse);
			}
			// Make sure things are contiguous: if we copied any segments and have a following segment, the last new one
			// should end where the first surviving one starts.
			if (lastSegInserted != null && ichLimNewSegs < int.MaxValue)
				lastSegInserted.EndOffset = ichLimNewSegs;
			NoteParaNeedsUpdateMainTrans(paraCurr);
		}

		private void CopyBt(ICmBaseAnnotation segDest, ICmBaseAnnotation segSrc)
		{
			if (HasChapVerseStyle(segSrc)) // nb don't check segDest, its offsets may not be consistent with para contents.
				return;
			int kflidFT = StTxtPara.SegmentFreeTranslationFlid(m_cache);
			int hvoFtSrc = m_cache.GetObjProperty(segSrc.Hvo, kflidFT);
			if (hvoFtSrc == 0)
				return;
			CmIndirectAnnotation ftSrc = new CmIndirectAnnotation(m_cache, hvoFtSrc);
			int hvoFtDest = m_cache.GetObjProperty(segDest.Hvo, kflidFT);
			ICmIndirectAnnotation ftDest;
			if (hvoFtDest == 0)
			{
				ftDest = CmIndirectAnnotation.CreateUnownedIndirectAnnotation(m_cache);
				ftDest.AppliesToRS.Append(segDest);
				ftDest.AnnotationTypeRAHvo = m_cache.GetIdFromGuid(LangProject.kguidAnnFreeTranslation);
				m_cache.VwCacheDaAccessor.CacheObjProp(segDest.Hvo, kflidFT, ftDest.Hvo);
			}
			else
			{
				ftDest = new CmIndirectAnnotation(m_cache, hvoFtDest);
			}
			// Enhance JohnT: copy other alternatives? Copy literal translation and notes?
			for (int i = 0; i < BtWss.Length; i++)
			{
				ITsString source = ftSrc.Comment.GetAlternativeTss(BtWss[i]);
				if (source.Length > 0)
					ftDest.Comment.SetAlternative(source, BtWss[i]);
			}
		}

		private int[] m_btWss;
		/// <summary>
		/// The writing systems that we should merge for segmented back translations.
		/// We may eventually be able to get a more accurate value.
		/// We may need to merge more than one.
		/// Use of this method should at least mark the code needing attention.
		/// </summary>
		int[] BtWss
		{
			get { if (m_btWss == null)
				m_btWss = m_cache.LangProject.CurAnalysisWssRS.HvoArray;
				return m_btWss;
			}
			set
			{
				Debug.Assert(value.Length > 0);
				m_btWss = value;
			}
		}
		/// <summary>
		/// Get the range of segments which are treated as a single unit for diffing based on a character range.
		/// The segments in question are from the end of a previous to the start of a following label segment,
		/// or to the start or end of the paragraph.
		/// A null paragraph is treated as having no BT segments (return isegMinLow = isegMinHigh = isegLim = 0).
		/// If the character range is exactly at the end of a verse, it is ambiguous whether to include that verse.
		/// Accordingly we return two isegMin values, a low one including the ambiguous verse, and a high one not
		/// including it. If ichMin is not ambiguous they are the same.
		/// </summary>
		internal void GetSegmentsOverlapping(StTxtPara para, int ichMin, int ichLim, out int isegMinLow, out int isegMinHigh, out int isegLim)
		{
			if (para == null)
			{
				isegMinLow = isegLim = isegMinHigh = 0;
				return;
			}
			int kflidSegments = StTxtPara.SegmentsFlid(m_cache);
			int cseg = m_cache.GetVectorSize(para.Hvo, kflidSegments);
			isegMinLow = isegMinHigh = 0; // by default we include all segments (e.g., for introduction or heading para).
			isegLim = cseg;

			for (int i = 0; i < cseg; i++)
			{
				int hvoSeg = m_cache.GetVectorItem(para.Hvo, kflidSegments, i);
				CmBaseAnnotation seg = new CmBaseAnnotation(m_cache, hvoSeg);
				if (HasChapVerseStyle(seg))
				{
					if (seg.EndOffset <= ichMin)
						isegMinLow = isegMinHigh = i + 1; // found a preceding label
					else if (ichMin >= seg.BeginOffset)
					{
						isegMinHigh = i; // make sure we don't include any segments from a verse entirely before the start of the change.
						// Except if we're exactly at the end of a verse we may still want to include them in the 'low' range.
						if (ichMin != seg.BeginOffset)
							isegMinLow = i;
					}
					if (seg.BeginOffset >= ichLim)
					{
						isegLim = i;
						break;
					}
				}
			}
			// End of paragraph is like finding a verse boundary: if ichMin is exactly there, we need
			// to make sure isegMinHigh is at the end of the last segment.
			if (ichMin == para.Contents.Length)
				isegMinHigh = cseg;
		}
		/// <summary>
		/// Test whether a run of text in a TsString contains 'label' text which should cause a segment break.
		/// (Current definition is that there are characters in the range with the Scripture styles VerseNumber
		/// or ChapterNumber, there is a single-character run which contains an ORC (0fffc), or the range being
		/// tested is a single character (possibly part of a longer run) which is a hard line break.
		/// </summary>
		public static bool HasChapVerseStyle(ITsString tss, int ichMin, int ichLim)
		{
			if (ichMin >= tss.Length)
				return false; // can happen when doing complex merges; if it's out of range it can't 'contain' verse styled text.
			// True if the run at ichMin has one of the interesting styles.
			int irun = tss.get_RunAt(ichMin);
			if (HasChapVerseStyle(tss, irun))
				return true;
			// See if any later run in the character range has the required style.
			int crun = tss.RunCount;
			for (irun++; irun < crun && tss.get_MinOfRun(irun) < ichLim; irun++)
			{
				if (HasChapVerseStyle(tss, irun))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Test whether a run in a TsString contains 'label' text which should cause a segment break.
		/// True if it has one of the interesting styles or the whole run is an ORC.
		/// Nb: this method won't detect hard line breaks.
		/// </summary>
		private static bool HasChapVerseStyle(ITsString tss, int irun)
		{
			ITsTextProps ttp = tss.get_Properties(irun);
			string styleName = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			return (styleName == ScrStyleNames.VerseNumber || styleName == ScrStyleNames.ChapterNumber);
		}

		private bool HasChapVerseStyle(ICmBaseAnnotation seg)
		{
			return HasChapVerseStyle((seg.BeginObjectRA as StTxtPara).Contents.UnderlyingTsString, seg.BeginOffset, seg.EndOffset);
		}

		private bool HasNoBtSegments(StTxtPara para)
		{

			StTxtPara.LoadSegmentFreeTranslations(new int[] {para.Hvo}, para.Cache, BtWss[0]);
			return para.HasNoSegmentBt(BtWss[0]);
		}

		private void PrepareParaForBtSegMerging(StTxtPara para)
		{
			StTxtPara.LoadSegmentFreeTranslations(new int[] { para.Hvo }, para.Cache, BtWss[0]);
			if (para.HasNoSegmentBt(BtWss[0]))
				new BtConverter(para).ConvertCmTransToInterlin(BtWss[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reviewed differences are just removed from the difference list.
		/// </summary>
		/// <param name="diff"></param>
		/// ------------------------------------------------------------------------------------
		internal void MarkDifferenceAsReviewed(Difference diff)
		{
			CheckDisposed();

			m_reviewedDiffs.Add(diff);
			Differences.Remove(diff);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a ParagraphMissingInCurrent difference, insert a copy of the revision
		/// paragraph into the current.
		/// </summary>
		/// <param name="diff">The object that encapsulates the difference</param>
		/// ------------------------------------------------------------------------------------
		private void ReplaceCurrentWithRevision_InsertPara(Difference diff)
		{
			StTxtPara paraRev = new StTxtPara(m_cache, diff.HvoRev);
			bool fRevIsStanzaBreak = IsStanzaBreak(paraRev);
			StTxtPara paraCurr = new StTxtPara(m_cache, diff.HvoCurr);

			StText textCurr = new StText(m_cache, paraCurr.OwnerHVO);
			StTxtPara insertedPara;
			int insertIndex = 0; // or -1 (value must be calculated and set before use)
			bool insertAfter = false;

			// If there is only one paragraph and it is empty, then reuse the existing paragraph
			bool fUsingExistingPara = (textCurr.ParagraphsOS.Count == 1 && paraCurr.Contents.Text == null);

			// Copy the Revision paragraph to the Current StText
			if (fUsingExistingPara)
			{
				//use the existing empty paragraph as the copy destination
				insertedPara = paraCurr;
				paraRev.CopyTo(insertedPara);
			}
			else
			{
				// Get the index of where to insert the new paragraph
				insertIndex = GetParaInsertIndex(diff, paraCurr, textCurr, fRevIsStanzaBreak, out insertAfter);

				// Create a new paragraph at the insert index as the copy destination
				//TODO TE-5053: These 3 lines could be replaced by calling an enhanced cache.CopyObject .
				insertedPara = new StTxtPara();
				textCurr.ParagraphsOS.InsertAt(insertedPara, insertIndex);
				paraRev.CopyTo(insertedPara);
			}

			// Copy the para's footnote & picture objects, and update the ORCs to point to the new copies
			int newLength = insertedPara.Contents.Length;
			insertedPara.CreateOwnedObjects(0, newLength, this);
			CopyBtSegments(insertedPara, paraRev);

	// REVIEW: would these ever call 'Fix' twice??
			if (fUsingExistingPara)
			{
				// Adjust the target location in Curr for all following diffs
				FixDestIpForInsert(diff, insertedPara, true);
			}

			// If this is a non-scripture paragraph, then fix the differences adjacent to
			// this one, so that adjacent inserted paragraphs will maintain the same order.
			if (!IsParaInScriptureContent(insertedPara))
				FixDestIpForInsert(diff, insertedPara, insertAfter);

			// If we inserted a new paragraph (as opposed to reusing an existing empty one) we can
			// now issue the prop-changed.
			if (!fUsingExistingPara)
			{
				m_cache.PropChanged(null, PropChangeType.kpctNotifyAll,
					insertedPara.OwnerHVO, insertedPara.OwningFlid, insertIndex, 1, 0);
			}

			// adjust the references for the section that owns the inserted paragraph
			ScrSection section = ScrSection.GetSectionFromParagraph(insertedPara);
			if (section != null)
				section.AdjustReferences();
		}

		/// <summary>
		/// Copy the free translations of the source paragraph to the destination.
		/// If destination already has some, reuse them.
		/// Also duplicates corresponding translation status information (since may have
		/// beein set to unfinished by rebuilding the CmTrans BT from the segments.
		/// </summary>
		private void CopyBtSegments(StTxtPara paraDest, StTxtPara paraSource)
		{
			if (HasNoBtSegments(paraSource))
				return;
			PrepareParaForBtSegMerging(paraDest);
			int kflidSegments = StTxtPara.SegmentsFlid(m_cache);
			int csegSource = m_cache.GetVectorSize(paraSource.Hvo, kflidSegments);
			int csegDest = m_cache.GetVectorSize(paraDest.Hvo, kflidSegments);

			// Save all destination status information.
			List<CmTranslation> translations = new List<CmTranslation>();
			List<int> wss = new List<int>();
			List<ITsString> statuses = new List<ITsString>();
			foreach (CmTranslation trans in paraDest.TranslationsOC)
			{
				foreach (int ws in BtWss)
				{
					statuses.Add(trans.Status.GetAlternativeTss(ws));
					wss.Add(ws);
					translations.Add(trans);
				}
			}
			FixBtSegsForReplace(paraDest, 0, 0, 0, csegDest, paraSource, 0, 0, 0, csegSource);

			// Restore all destination status information.
			for (int i = 0; i < translations.Count; i++)
				if (translations[i].Status.GetAlternativeTss(wss[i]) != statuses[i])
					translations[i].Status.SetAlternative(statuses[i], wss[i]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine where a paragraph should be inserted in the Current StText.
		/// In scripture, we must insert a paragraph in verse reference order.
		/// </summary>
		/// <param name="diff">the diff of type "para missing in current"</param>
		/// <param name="paraCurr">the Current paragraph refered to by the diff</param>
		/// <param name="textCurr">the Current StText we may want to insert the rev para into.</param>
		/// <param name="fRevParaIsStanza">Revision (inserted para) is a stanza break.</param>
		/// <param name="insertAfter">out: set to true if the returned insert index is after the paraCurr
		/// rather than before. This is useful only for maintaining the ordering of non-scripture
		/// paragraphs.</param>
		/// <returns>the index that the revision paragraph should be inserted at in
		/// the textCurr</returns>
		/// ------------------------------------------------------------------------------------
		private int GetParaInsertIndex(Difference diff, StTxtPara paraCurr, StText textCurr,
			bool fRevParaIsStanza, out bool insertAfter)
		{
			insertAfter = false;
			if (IsParaInScriptureContent(paraCurr) && !fRevParaIsStanza)
			{
				// Look thru paragraphs in textCurr to see where to insert the diff ref in proper order
				for (int iPara = 0; iPara < textCurr.ParagraphsOS.Count; iPara++)
				{
					ScrTxtPara thisPara = new ScrTxtPara(m_cache, textCurr.ParagraphsOS.HvoArray[iPara]);
					BCVRef paraRefStart, dummy;
					thisPara.GetBCVRefAtPosition(0, false, out paraRefStart, out dummy);

					// is the start ref of the diff before or the same as the start ref of thisPara?
					if (diff.RefStart <= paraRefStart)
					{
						// REVIEW: This reference condition has been here for a while, but it does not
						//  work well when the same verse spans multipe paras.
						return iPara;
					}
				}
				return textCurr.ParagraphsOS.Count;
			}
			else
			{
				// We are in an intro, section head, or title. Use the diff's Current info
				//  to determine the insert index.
				int insertIndex = m_cache.GetObjIndex(paraCurr.OwnerHVO, paraCurr.OwningFlid, paraCurr.Hvo);
				// If the min of the curr paragraph is 0, then insert the new paragraph before it,
				// otherwise insert it after.
				if (diff.IchMinCurr != 0)
				{
					insertAfter = true;
					++insertIndex;
				}
				return insertIndex;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if a paragraph belongs to the content of a Scripture section
		/// </summary>
		/// <param name="para">paragraph to check</param>
		/// <returns>true if the paragraph is in the content of a Scripture section</returns>
		/// ------------------------------------------------------------------------------------
		private bool IsParaInScriptureContent(StTxtPara para)
		{
			// Determine if there is no section or if it is an intro section
			ScrSection section = ScrSection.GetSectionFromParagraph(para);
			if (section == null || section.IsIntro)
				return false;

			// Check if the para is in the content of the section
			StText text = new StText(m_cache, para.OwnerHVO);
			return (text.OwningFlid == (int)ScrSection.ScrSectionTags.kflidContent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a paragraph is inserted into Current after the hvoCurr para, there may be diffs
		/// following this one that also insert at the same location. All of these need to be
		/// adjusted to insert after the newly inserted paragraph so they will all be inserted
		/// in the same order.
		/// Also, if the paragraph was inserted BEFORE the hvoCurr para then the PREVIOUS diffs
		/// will need to be adjusted.
		/// </summary>
		/// <param name="diff">the ParaMissingInCurr diff by which a paragraph
		/// has just been inserted in the Current</param>
		/// <param name="paraInsertedCurr">the paragraph just inserted in the Current</param>
		/// <param name="fixAfter">true to fix the diffs after the current diff, false to
		/// fix the diffs before the current diff</param>
		/// ------------------------------------------------------------------------------------
		private void FixDestIpForInsert(Difference diff, StTxtPara paraInsertedCurr,
			bool fixAfter)
		{
			int hvoNew = paraInsertedCurr.Hvo;
			int ichMinNew = 0;
			int ichLimNew = 0;

			if (fixAfter)
			{
				// For inserting after, we want to adjust certain following
				//  diffs to point to the end of the newly inserted paragraph
				ichMinNew = ichLimNew = paraInsertedCurr.Contents.Length;
			}
			else
			{
				// for inserting before, we want to adjust certain previous diffs
				//  to point to the beginning of the newly inserted paragraph
				ichMinNew = ichLimNew = 0;
			}

			// Let the difference list fix appropriate references to the hvoCurr
			m_differences.FixMissingCurrParaDestIP(diff, fixAfter, hvoNew, ichMinNew, ichLimNew);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a ParagraphAddedToCurrent difference, delete the "current" paragraph
		/// </summary>
		/// <param name="diff">The object that encapsulates the difference</param>
		/// ------------------------------------------------------------------------------------
		private void ReplaceCurrentWithRevision_RemovePara(Difference diff)
		{
			StTxtPara paraCurr = new StTxtPara(m_cache, diff.HvoCurr);

			// Get the section for the paragraph so we can adjust references after
			// the paragraph is deleted.
			ScrSection section = ScrSection.GetSectionFromParagraph(paraCurr);

			// If the section has only one paragraph, then just empty the contents rather
			// than deleting the paragraph.
			StText text = new StText(m_cache, paraCurr.OwnerHVO);
			if (text.ParagraphsOS.Count == 1)
			{
				EmptyParagraph(paraCurr);
				m_differences.FixCurrParaHvosAndSetIch(paraCurr.Hvo, paraCurr.Hvo, 0, 0); //REVIEW: this could be handled by a slightly smarter FixParaHvosForDelete(paraCurr), to simplify
			}
			else
			{	// fix references to the paragraph and then delete it.
				FixParaHvosForDelete(paraCurr);
				DeleteParagraph(paraCurr);
			}

			// adjust the references for the section that owned the deleted paragraph
			if (section != null)
				section.AdjustReferences();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a paragraph is deleted from current, there may be remaining diffs that refer
		/// to the deleted paragraph. This method will fix the Current para references that are
		/// about to become obsolete.
		/// This method must be called before the paragraph is deleted because it will use
		/// the existing paragraph to find a neighbor.
		/// </summary>
		/// <param name="paraCurr">the Current paragraph that is about to be deleted</param>
		/// ------------------------------------------------------------------------------------
		private void FixParaHvosForDelete(StTxtPara paraCurr)
		{
			StText textCurr = new StText(m_cache, paraCurr.OwnerHVO);
			int indexParaCurr = paraCurr.IndexInOwner;
			// the given para should never be the only para in its StText
			Debug.Assert(textCurr.ParagraphsOS.Count > 1);

			int hvoNew;
			int ichMinNew = 0;
			int ichLimNew = 0;

			// If the deleted paragraph is not at the start...
			if (indexParaCurr > 0)
			{
				// use the end of the previous paragraph
				StTxtPara paraNew = (StTxtPara)textCurr.ParagraphsOS[indexParaCurr - 1];
				hvoNew = paraNew.Hvo;
				ichMinNew = ichLimNew = paraNew.Contents.Length;
			}
			else
			{
				// otherwise use the start of the next paragraph
				hvoNew = textCurr.ParagraphsOS.HvoArray[indexParaCurr + 1];
				ichMinNew = ichLimNew = 0;
			}

			// Let the difference list fix all references to this para
			m_differences.FixCurrParaHvosAndSetIch(paraCurr.Hvo, hvoNew, ichMinNew, ichLimNew);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Transfer the changes in paragraph structure (along with any text changes in that
		/// range) from the revision to the current. In this process, we want to prevserve
		/// paragraphs, as much as possible, in the existing current draft to minimize processing
		/// to update other annotations, diffs, etc.
		/// </summary>
		/// <param name="baseDiff">The base paragraph structure difference.</param>
		/// ------------------------------------------------------------------------------------
		private void ReplaceCurrentWithRevision_CopyParaStructure(Difference baseDiff)
		{
			Debug.Assert(baseDiff.HasParaSubDiffs);

			// Get the section for the first paragraph referenced in the first subdiff.
			Debug.Assert(baseDiff.SubDiffsForParas[0].HvoCurr != 0, "No current para hvo in subdiffs");
			ScrSection sectionCurr = ScrSection.GetSectionFromParagraph(new StTxtPara(m_cache,
				baseDiff.SubDiffsForParas[0].HvoCurr));

#if DEBUG
			// The section owning the first paragraph in subdifferences must be the same as the last.
			// If there's only one paragraph for the Current in the cluster, we don't have to worry
			// that we might be spanning sections.
			if (baseDiff.SubDiffsForParas[baseDiff.SubDiffsForParas.Count - 1].HvoCurr != 0)
			{
				// We have a paragraph for the last subdifference. Get it.
				ScrSection sectionCurrLast = ScrSection.GetSectionFromParagraph(new StTxtPara(m_cache,
					baseDiff.SubDiffsForParas[baseDiff.SubDiffsForParas.Count - 1].HvoCurr));
				Debug.Assert(sectionCurr.Hvo == sectionCurrLast.Hvo, "Different sections spanned in ScrVerse cluster");
			}
#endif

			// Tss to hold the unchanged ending portion of a merged paragraph in the Current,
			//  for use in constructing the last reverted paragraph
			ITsString tssRemainderOfMergedPara = null;
			List<CmBaseAnnotation> segsRemainderOfMergedPara = null;
			int hvoForRemainderOfMergedPara = 0; //original id of the paragraph of the remainder
			int ichStartRemainder = 0; //original start ich of the remainder

			// items we need to issue PropChanges after we are done with the whole diff
			int hvoOwningStText = 0;
			int flidOwningStText = 0;
			List<int> indicesOfInsertedParas = new List<int>(6); // probably not more than 6 paras inserted for this cluster

			int hvoLastParaProcessedCurr = 0; //id of the last previous para we processed in this cluster in the Curr
			bool fInsertingParasAtStartOfStText = false;
			bool fDeletingWholeParas = false;

			// Iterate through subdifferences...
			for (int iSubDiff = 0; iSubDiff < baseDiff.SubDiffsForParas.Count; iSubDiff++)
			{
				Difference subdiff = baseDiff.SubDiffsForParas[iSubDiff];

				// If we have a Current and Revision paragraph...
				if (subdiff.HvoCurr != 0 && subdiff.HvoRev != 0)
				{
					CopyRevParaToCurPara(baseDiff, iSubDiff,
						ref tssRemainderOfMergedPara, ref segsRemainderOfMergedPara, //remainder outputs -first subdiff
						ref hvoForRemainderOfMergedPara, ref ichStartRemainder,     //remainder outputs -first subdiff
						ref hvoLastParaProcessedCurr,
						ref fInsertingParasAtStartOfStText, ref fDeletingWholeParas); //flag outputs -first subdiff
				}

				// If the Revision has no paragraph...
				else if (subdiff.HvoRev == 0)
				{
					RemovePartOrAllOfParaInCurr(baseDiff, iSubDiff, sectionCurr,
						ref hvoLastParaProcessedCurr, fDeletingWholeParas);
				}

				// If the Current has no paragraph...
				else if (subdiff.HvoCurr == 0)
				{
					CopyRevParaToNewParaInCur(baseDiff, iSubDiff, sectionCurr,
						tssRemainderOfMergedPara, segsRemainderOfMergedPara,        // remainder inputs -used with last subdiff
						hvoForRemainderOfMergedPara, ichStartRemainder,             // remainder inputs -used with last subdiff
						ref hvoOwningStText, ref flidOwningStText, indicesOfInsertedParas, // outputs for PropChanges
						ref hvoLastParaProcessedCurr, fInsertingParasAtStartOfStText);
				}
				else
					Debug.Fail("Subdifference is empty");
			}

			// Now issue PropChanges for all paragraphs that were inserted, if any.
			foreach (int paraIndex in indicesOfInsertedParas)
			{
				m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, hvoOwningStText,
					flidOwningStText, paraIndex, 1, 0);
			}

			// adjust the references for the section
			if (sectionCurr != null)
				sectionCurr.AdjustReferences();
		}

		/// <summary> This method is used by ReplaceCurrentWithRevision_CopyParaStructure()
		/// when the given subdiff has a Revision paragraph (or portion) that must overwrite
		/// the Current paragraph (or portion).
		/// </summary>
		/// <remarks>each of the parameters is named precisely and documented within
		/// ReplaceCurrentWithRevision_CopyParaStructure() above</remarks>
		private void CopyRevParaToCurPara(Difference baseDiff, int iSubDiff,
			ref ITsString tssRemainderOfMergedPara, ref List<CmBaseAnnotation> segsRemainderOfMergedPara, //remainder outputs -first subdiff
			ref int hvoForRemainderOfMergedPara, ref int ichStartRemainder,     //remainder outputs -first subdiff
			ref int hvoLastParaProcessedCurr,
			ref bool fInsertingParasAtStartOfStText, ref bool fDeletingWholeParas) //flag outputs -first subdiff
		{
			Difference subdiff = baseDiff.SubDiffsForParas[iSubDiff];

			// Special handling for the first subdiff of a ParaMerge or ParasMissing diff
			if (iSubDiff == 0)
			{
				if (baseDiff.DiffType == DifferenceType.ParagraphMergedInCurrent ||
					// this next possibility matches a diff that contains orphans from a simplified cluster
					(baseDiff.DiffType == DifferenceType.ParagraphStructureChange &&
					baseDiff.SubDiffsForParas[1].DiffType == DifferenceType.ParagraphMissingInCurrent &&
					subdiff.IchLimCurr > 0))
				{
					// For these types of diffs, the last subdiff has no Current info
					// (therefore the Revision side will provide the last para added)
					Debug.Assert(baseDiff.SubDiffsForParas[baseDiff.SubDiffsForParas.Count - 1].HvoCurr == 0);

					// remove the remainder of the Curr paragraph for use at the last added paragraph
					StTxtPara paraCurr = new StTxtPara(m_cache, subdiff.HvoCurr);
					Debug.Assert(subdiff.IchLimCurr <= paraCurr.Contents.Length,
						"The ichLimCurr was not less than the para length, but should have been");
					tssRemainderOfMergedPara = paraCurr.Contents.UnderlyingTsString.GetSubstring(
						subdiff.IchLimCurr, paraCurr.Contents.Length);
					hvoForRemainderOfMergedPara = paraCurr.Hvo;
					ichStartRemainder = subdiff.IchLimCurr;
					segsRemainderOfMergedPara = AdjustMovedSegments(paraCurr, ichStartRemainder);
					ITsStrBldr paraBldr = paraCurr.Contents.UnderlyingTsString.GetBldr();
					paraBldr.ReplaceRgch(subdiff.IchLimCurr, paraCurr.Contents.Length,
						string.Empty, 0, null);
					paraCurr.Contents.UnderlyingTsString = paraBldr.GetString();
				}
				else if (baseDiff.DiffType == DifferenceType.ParagraphStructureChange &&
					baseDiff.SubDiffsForParas[1].DiffType == DifferenceType.ParagraphMissingInCurrent &&
					subdiff.IchLimCurr == 0)
				{
					// This is a 'reference points' subdiff zero of a 'paras missing in Current' cluster
					//  with Rev paras that must be inserted starting at index zero in the Current.
					//  the subdiff.HvoCurr in this one case is the para to insert at - not the previous para
					//Debug.Assert(new StTxtPara(m_cache, subdiff.HvoCurr).IndexInOwner == 0); This is almost always true, but alas it's not if the Current StText begins with an empty para.
					fInsertingParasAtStartOfStText = true;
					return; // we're done with this subdiff for this special case
				}
				else if (baseDiff.DiffType == DifferenceType.ParagraphStructureChange &&
					baseDiff.SubDiffsForParas[1].DiffType == DifferenceType.ParagraphAddedToCurrent &&
					subdiff.IchLimCurr == 0)
				{
					// This is a 'reference points' subdiff zero of a 'paras added in Current' cluster
					//  with Current paras that must be deleted exacty as they are
					fDeletingWholeParas = true;
					return; // we're done with this subdiff for this special case
				}
			}

			// We replace the specified range, and remember the Current para just processed.
			DiffTextRangeReplace(subdiff);
			//TODO: Also copy the paragraph style from the revision.
			hvoLastParaProcessedCurr = subdiff.HvoCurr;

			if (iSubDiff == baseDiff.SubDiffsForParas.Count - 1)
			{
				// This is the last subdiff of a 'multiple in both' cluster.
				// Adjust ich's of following diffs appropriately.
				int adjustment = (subdiff.IchLimRev - subdiff.IchMinRev) -
					(subdiff.IchLimCurr - subdiff.IchMinCurr);
				if (adjustment != 0)
					m_differences.AdjustFollowingOffsets(baseDiff, subdiff.HvoCurr, adjustment);
			}
		}

		/// <summary> This method is used by ReplaceCurrentWithRevision_CopyParaStructure()
		/// when the given subdiff has a Current paragraph (or portion) that must be removed.
		/// </summary>
		/// <remarks>each of the parameters is named precisely and documented within
		/// ReplaceCurrentWithRevision_CopyParaStructure() above</remarks>
		private void RemovePartOrAllOfParaInCurr(Difference baseDiff, int iSubDiff, ScrSection sectionCurr,
			ref int hvoLastParaProcessedCurr, bool fDeletingWholeParas)
		{
			Difference subdiff = baseDiff.SubDiffsForParas[iSubDiff];

			if (iSubDiff == 0 && subdiff.IchMinCurr > 0)
			{
				// This is the first subdiff of 'paras added to Current' cluster
				//  AND there is text in the Current paragraph before the range of this subdiff.
				// Remove the text range of this subdiff
				DiffTextRangeReplace(subdiff);
				hvoLastParaProcessedCurr = subdiff.HvoCurr;
			}
			else if (iSubDiff == baseDiff.SubDiffsForParas.Count - 1 && !fDeletingWholeParas)
			{
				// This is the last subdiff of
				// 1. a 'para split in Current' cluster - we only have one para in the Rev, and more than one in the Current
				// or 2. a 'paras added to Current' cluster - nothing in the Rev, and one or more paras in the Current.
				// Also, if  fDeletingWholeParas == true the append should not be attempted, so we can't do this block of code

				// First, remove any diff range in the Current para
				DiffTextRangeReplace(subdiff);

				// Get the Current para and its previous paragraph so that we can join them
				StTxtPara paraCurr = new StTxtPara(m_cache, subdiff.HvoCurr);
				Debug.Assert(paraCurr.IndexInOwner - 1 >= 0,
					"The current paragraph is the first in the section but should not be.");
				StTxtPara paraPrevCurr =
					(StTxtPara)sectionCurr.ContentOA.ParagraphsOS[paraCurr.IndexInOwner - 1];

				// Append the Current paragraph's content to the previous Current paragraph.
				PrepareParaForBtSegMerging(paraPrevCurr); // before we modify it.
				ITsStrBldr strBldr = paraPrevCurr.Contents.UnderlyingTsString.GetBldr();
				int paraPrevCurrOrigLength = strBldr.Length;
				strBldr.ReplaceTsString(strBldr.Length, strBldr.Length, paraCurr.Contents.UnderlyingTsString);
				paraPrevCurr.Contents.UnderlyingTsString = strBldr.GetString();

				//Append BT too
				MoveParaBt(paraPrevCurr, paraPrevCurrOrigLength, paraCurr);

				// Adjust following diffs appropriately. We may have removed text with the diff and
				// we appended the contents of the current to the previous para.
				int adjustment = paraPrevCurrOrigLength - (subdiff.IchLimCurr - subdiff.IchMinCurr);
				m_differences.FixFollowingParaDiffs(baseDiff, subdiff.HvoCurr, paraPrevCurr.Hvo, adjustment);

				// Delete the paragraph (but not the owned objects because the ORCs are moved to the previous!)
				StText text = new StText(m_cache, paraCurr.OwnerHVO);
				text.ParagraphsOS.Remove(paraCurr);
			}
			else
			{
				// This is a subdiff of 'paras added to Current' cluster AND it covers the entire Current para
				// OR This is the 'mid' subdiff of a 'para split in Current' or 'para structure chg' cluster.
				// Remove the Current paragraph.
				Debug.Assert(subdiff.HvoCurr != 0, "Can't delete a para without valid hvo.");
				StTxtPara paraCurr = new StTxtPara(m_cache, subdiff.HvoCurr);
				Debug.Assert(subdiff.IchMinCurr == 0 && subdiff.IchLimCurr == paraCurr.Contents.Length);
				FixParaHvosForDelete(paraCurr);
				DeleteParagraph(paraCurr);
			}
		}

		/// <summary> This method is used by ReplaceCurrentWithRevision_CopyParaStructure()
		/// when the given subdiff needs the Revision paragraph to be copied to a NEW Paragraph
		/// in the Current.
		/// </summary>
		/// <remarks>each of the parameters is named precisely and documented within
		/// ReplaceCurrentWithRevision_CopyParaStructure() above</remarks>
		private void CopyRevParaToNewParaInCur(Difference baseDiff, int iSubDiff, ScrSection sectionCurr,
			ITsString tssRemainderOfMergedPara, List<CmBaseAnnotation> segsRemainderOfMergedPara, // remainder inputs -last subdiff
			int hvoForRemainderOfMergedPara, int ichStartRemainder,                             // remainder inputs -last subdiff
			ref int hvoOwningStText, ref int flidOwningStText, List<int> indicesOfInsertedParas, // outputs for PropChanges
			ref int hvoLastParaProcessedCurr, bool fInsertingParasAtStartOfStText)
		{
			Difference subdiff = baseDiff.SubDiffsForParas[iSubDiff];

			// Determine where we will insert a new para in the current
			int indexToInsertAt;
			// if there is a prev para processed within the cluster...
			if (hvoLastParaProcessedCurr > 0)
			{
				StTxtPara prevParaCurr = new StTxtPara(m_cache, hvoLastParaProcessedCurr);
				indexToInsertAt = prevParaCurr.IndexInOwner + 1;
			}
			else
			{
				// No previous para has been processed within the current
				// This subdiff[1] covers the first actual para of a 'paras missing in Current' cluster
				// and it must be inserted before all existing paras in the Current.
				Debug.Assert(iSubDiff == 1); // (subDiff zero had the reference point)
				indexToInsertAt = 0;
			}

			// Copy the revision paragraph to a new paragraph in the Current
			StTxtPara paraRev = new StTxtPara(m_cache, subdiff.HvoRev);
			StText stTextCurr = (StText)sectionCurr.ContentOA;
			//TODO TE-5053: These 3 lines could be replaced by calling an enhanced cache.CopyObject.
			StTxtPara insertedPara = new StTxtPara();
			stTextCurr.ParagraphsOS.InsertAt(insertedPara, indexToInsertAt);
			paraRev.CopyTo(insertedPara);
			CopyBtSegments(insertedPara, paraRev);

			if (iSubDiff == baseDiff.SubDiffsForParas.Count - 1 && !fInsertingParasAtStartOfStText)
			{
				// This is the last subdiff of
				// 1. a 'para merge in Current' cluster - we only have one para in the Current, and more than one in the Revision.
				// or 2. a 'paras missing in Current' cluster - nothing in the Current, and one or more paras in the Revision.
				// Also, if  fInsertingParasAtStartOfStText == true there is no remainder, so we can't do this block of code

				// remove the para contents beyond the range of this subdiff
				ITsStrBldr paraBldr = insertedPara.Contents.UnderlyingTsString.GetBldr();
				paraBldr.ReplaceRgch(subdiff.IchLimRev, paraRev.Contents.Length,
					string.Empty, 0, null);
				insertedPara.Contents.UnderlyingTsString = paraBldr.GetString();
				RemoveBtAfterEnd(insertedPara);

				// Copy the para's footnote & picture objects, and update the ORCs to point to the new copies
				int length = insertedPara.Contents.Length;
				insertedPara.CreateOwnedObjects(0, length, this);

				// Append the unchanged ending portion of the merged paragraph
				PrepareParaForBtSegMerging(insertedPara);
				Debug.Assert(tssRemainderOfMergedPara != null, "No remainder string when reverting merged or missing paras");
				paraBldr = insertedPara.Contents.UnderlyingTsString.GetBldr();
				paraBldr.ReplaceTsString(paraBldr.Length, paraBldr.Length, tssRemainderOfMergedPara);
				insertedPara.Contents.UnderlyingTsString = paraBldr.GetString();
				MoveParaBt(insertedPara, length, segsRemainderOfMergedPara);

				// We will adjust the character offsets of following diffs by the amount
				// that the paragraph was shortened by splitting the paragraph.
				int adjustment = subdiff.IchLimRev - ichStartRemainder;
				// We must fix any references in following diffs - i.e. for the remainder of the paragraph
				// that we just split.
				m_differences.FixFollowingParaDiffs(baseDiff, hvoForRemainderOfMergedPara,
					insertedPara.Hvo, adjustment);
			}
			else
			{
				// This is the 'mid' subdiff of a 'para merge in Current' or 'para structure chg' cluster.
				// Or possibly the last subdiff of a 'paras missing in Current' if we are fInsertingParasAtStartOfStText
				// Copy the para's footnote & picture objects, and update the ORCs to point to the new copies
				int len = insertedPara.Contents.Length;
				insertedPara.CreateOwnedObjects(0, len, this);
			}

			if (hvoOwningStText == 0)
			{
				hvoOwningStText = insertedPara.OwnerHVO;
				flidOwningStText = insertedPara.OwningFlid;
			}
			Debug.Assert(hvoOwningStText == insertedPara.OwnerHVO && flidOwningStText == insertedPara.OwningFlid,
				"Owning StText and/or flid should not have changed!");
			indicesOfInsertedParas.Add(insertedPara.IndexInOwner);

			hvoLastParaProcessedCurr = insertedPara.Hvo;
		}


		///// <summary>
		///// Determine if the specified diff would have text, if reverted.
		///// </summary>
		///// <param name="diff"></param>
		///// <returns><c>true</c> if the specified diff would change text; <c>false</c> otherwise</returns>
		//private bool NotChangingText(Difference diff)
		//{
		//    if ((diff.IchLimRev - diff.IchMinRev != 0) || (diff.IchLimCurr - diff.IchMinCurr != 0))
		//        return false;

		//    return true;
		//}

		/// <summary>
		/// A paragraph has been truncated and the truncated part is not to be moved.
		/// Delete any segments and their back translations.
		/// Caller is responsible to make the paragraph BT consistent.
		/// </summary>
		/// <param name="insertedPara"></param>
		private void RemoveBtAfterEnd(StTxtPara insertedPara)
		{
			List<CmBaseAnnotation> badSegs = AdjustMovedSegments(insertedPara, insertedPara.Contents.Length);
			RemoveBtSegs(badSegs);
		}

		/// <summary>
		/// Remove all the segments (and their free translations, if any) for the specified paragraph.
		/// This is currently used when deleting or emptying the paragraph. When deleting it isn't
		/// necessary to do anything about the paragraph BT. When emptying, the caller deletes the
		/// paragraph BT. Any additional callers should do something about keeping the paragraph BT consistent.
		/// </summary>
		private void RemoveBtSegs(StTxtPara para)
		{
			// This is slightly weaker than PrepareParaForBtSegMerging. Since the goal is to get rid
			// of all BT segments, we don't need to create them if they don't already exist!
			StTxtPara.LoadSegmentFreeTranslations(new int[] { para.Hvo }, para.Cache, BtWss[0]);
			int kflidSegments = StTxtPara.SegmentsFlid(m_cache);
			int cseg = m_cache.GetVectorSize(para.Hvo, kflidSegments);
			List<CmBaseAnnotation> segs = new List<CmBaseAnnotation>();
			for (int i = 0; i < cseg; i++)
				segs.Add(new CmBaseAnnotation(m_cache, m_cache.GetVectorItem(para.Hvo, kflidSegments, i)));
			RemoveBtSegs(segs);
		}

		private void RemoveBtSegs(List<CmBaseAnnotation> badSegs, StTxtPara para, int ihvoMin)
		{
			int kflidSegments = StTxtPara.SegmentsFlid(m_cache);
			int[] oldSegs = m_cache.GetVectorProperty(para.Hvo, kflidSegments, true);
			RemoveBtSegs(badSegs);
			int[] newSegs = new int[oldSegs.Length - badSegs.Count];
			Array.Copy(oldSegs, 0, newSegs, 0, ihvoMin);
			Array.Copy(oldSegs, ihvoMin + badSegs.Count, newSegs, ihvoMin, newSegs.Length - ihvoMin);
			m_cache.VwCacheDaAccessor.CacheVecProp(para.Hvo, kflidSegments, newSegs, newSegs.Length);
			// This version behaves differently in test code, since the test cache is not as smart about
			// cleaning stale refs out of miscellaneous properties. Better to actually set it to what we think
			// it should be.
			//m_cache.VwCacheDaAccessor.CacheReplace(para.Hvo, kflidSegments, ihvoMin,
			//    ihvoMin + badSegs.Count, new int[0], 0);
			NoteParaNeedsUpdateMainTrans(para);
		}

		// Remove the specified segments. Caller is responsible to update kflidSegments and to UpdateMainTransFromSegmented
		private void RemoveBtSegs(List<CmBaseAnnotation> badSegs)
		{
			Set<int> hvosToDel = new Set<int>(badSegs.Count*2);
			int kflidFT = StTxtPara.SegmentFreeTranslationFlid(m_cache);

			foreach (CmBaseAnnotation seg in badSegs)
			{
				hvosToDel.Add(seg.Hvo);
				int hvoFt = m_cache.GetObjProperty(seg.Hvo, kflidFT);
				if (hvoFt != 0)
					hvosToDel.Add(hvoFt); // Enhance JohnT: also delete Lit Trans and notes
			}
			CmObject.DeleteObjects(hvosToDel, m_cache);
		}

		/// <summary>
		/// Given that para is to be truncated at ichTruncate, if there is a segment that
		/// overlaps that position shorten it. If there are segments which begin after
		/// ichTruncate, subtract ichTruncate from their offsets and return them.
		/// Caller is responsible to delete them or move them to the appropriate new paragraph.
		/// This routine will set their Begin and EndObjects to null (so they won't be found
		/// as segments of para) and fix the kflidSegments virtual.
		/// </summary>
		private List<CmBaseAnnotation> AdjustMovedSegments(StTxtPara para, int ichTruncate)
		{
			PrepareParaForBtSegMerging(para);
			int kflidSegments = StTxtPara.SegmentsFlid(m_cache);
			int cseg = m_cache.GetVectorSize(para.Hvo, kflidSegments);
			List<CmBaseAnnotation> result = new List<CmBaseAnnotation>();
			int isegFirstMove = cseg; // by default none move
			for (int i = 0; i < cseg; i++)
			{
				CmBaseAnnotation seg = new CmBaseAnnotation(m_cache, m_cache.GetVectorItem(para.Hvo, kflidSegments, i));
				if (seg.EndOffset <= ichTruncate)
					continue;
				if (seg.BeginOffset < ichTruncate)
				{
					// overlaps break...truncate it.
					seg.EndOffset = ichTruncate;
				}
				else
				{
					if (result.Count == 0)
						isegFirstMove = i;
					// beyond break...detach from para and make offsets relative to cut of part.
					result.Add(seg);
					seg.BeginOffset -= ichTruncate;
					seg.EndOffset -= ichTruncate;
					seg.BeginObjectRA = null;
					seg.EndObjectRA = null;
				}
			}
			// fix the kflidSegments virtual property.
			if (isegFirstMove < cseg)
				m_cache.VwCacheDaAccessor.CacheReplace(para.Hvo, kflidSegments, isegFirstMove, cseg, new int[0], 0);
			NoteParaNeedsUpdateMainTrans(para);
			return result;
		}

		/// <summary>
		/// Move the free translation segments of paraSource to ParaDest. The text has
		/// already been moved. Before the move, paraDest had length paraDestOrigLength,
		/// so add this to the offsets.
		/// Caller should call PrepareParaForBtSegMerging(paraDest) BEFORE appending the text.
		/// Possibly the join results in some segments needing to merge. If so, and if the merge is simple enough, do it.
		/// Enhance JohnT: possibly copy the segmented BT to the old.
		/// </summary>
		private void MoveParaBt(StTxtPara paraDest, int paraDestOrigLength, StTxtPara paraSource)
		{
			PrepareParaForBtSegMerging(paraSource);
			int kflidSegments = StTxtPara.SegmentsFlid(m_cache);
			int csegSource = m_cache.GetVectorSize(paraSource.Hvo, kflidSegments);
			if (csegSource == 0)
				return; // no segs to move.
			List<CmBaseAnnotation> segsToMove = new List<CmBaseAnnotation>();
			for (int i = 0; i < csegSource; i++)
				segsToMove.Add(new CmBaseAnnotation(m_cache, m_cache.GetVectorItem(paraSource.Hvo, kflidSegments, i)));
			MoveParaBt(paraDest, paraDestOrigLength, segsToMove);
		}
		/// <summary>
		/// Move the free translation segments of paraSource to ParaDest. The text has
		/// already been moved. Before the move, paraDest had length paraDestOrigLength,
		/// so add this to the offsets.
		/// Caller should call PrepareParaForBtSegMerging(paraDest) BEFORE appending the text.
		/// Possibly the join results in some segments needing to merge. If so, and if the merge is simple enough, do it.
		/// Enhance JohnT: possibly copy the segmented BT to the old.
		/// </summary>
		private void MoveParaBt(StTxtPara paraDest, int paraDestOrigLength, List<CmBaseAnnotation> segsToMove)
		{
			int kflidSegments = StTxtPara.SegmentsFlid(m_cache);
			int[] segs = new int[segsToMove.Count];
			for (int i = 0; i < segs.Length; i++)
			{
				CmBaseAnnotation seg = segsToMove[i];
				segs[i] = seg.Hvo;
				seg.BeginObjectRA = paraDest;
				seg.EndObjectRA = paraDest;
				seg.BeginOffset += paraDestOrigLength;
				seg.EndOffset += paraDestOrigLength;
			}
			int csegDest = m_cache.GetVectorSize(paraDest.Hvo, kflidSegments);
			m_cache.VwCacheDaAccessor.CacheReplace(paraDest.Hvo, kflidSegments, csegDest, csegDest, segs, segsToMove.Count);

			// Many simple cases don't trigger this, because (before it fires) fixing up the text of the merged
			// paragraph sorts everything out. It takes quite a complex change
			// (e.g., ReplaceCurWithRev_MultiParasInVerse_OneToThreeParas_TextChanges).
			SegmentCollector collector = new SegmentCollector(paraDest.Contents.UnderlyingTsString,
				m_cache.LanguageWritingSystemFactoryAccessor);
			collector.Run();
			int cseg = csegDest + segsToMove.Count;
			if (collector.Segments.Count < cseg)
			{
				// See if we can find a single desired segment that combines two or more existing ones.
				// Eventually the paragraph will get reparsed into some reasonable state, but some BT might
				// get lost in the process. Our hope of doing better is that there might be a desirable
				// segment whose begin matches one of the current ones and whose end matches a (later)
				// current one.
				List<CmBaseAnnotation> delSegs = new List<CmBaseAnnotation>();
				int iMerge = 0; // index into paraDest.Segments, a segment we hope to merge with following one(s)
				int iBegin = 0; // index into collector.Segments, a desired segment we may be able to make by merging.
				// stop at cseg-1 because we are only interested if we have at least two segs to merge.
				for (; iMerge < cseg - 1 && iBegin < collector.Segments.Count; )
				{
					CmBaseAnnotation segMerge = new CmBaseAnnotation(m_cache,
						m_cache.GetVectorItem(paraDest.Hvo, kflidSegments, iMerge));
					TsStringSegment segWanted = collector.Segments[iBegin];
					if (segMerge.BeginOffset < segWanted.BeginOffset)
					{
						iMerge++;
						continue;
					}
					if (segMerge.BeginOffset > segWanted.BeginOffset)
					{
						iBegin++;
						continue;
					}
					// Found a begin segment!
					if (segMerge.EndOffset >= segWanted.EndOffset)
					{
						// typically no need to change, the segments match.
						// or, the desired segment is shorter, can't do any good by merging.
						iBegin++;
						iMerge++;
						continue;
					}
					CmBaseAnnotation segEnd;
					int iEnd = -1;
					for (int iTry = iBegin + 1; iTry < cseg; iTry++)
					{
						segEnd = new CmBaseAnnotation(m_cache,
													  m_cache.GetVectorItem(paraDest.Hvo, kflidSegments, iTry));
						if (segEnd.EndOffset > segWanted.EndOffset)
						{
							// Can't do a legitimate merge at this point, probably we've had it, but keep trying the outer loop.
							// Or we may be able to merge an earlier segment that worked.
							break;
						}
						if (segEnd.EndOffset == segWanted.EndOffset)
							iEnd = iTry; // Will merge with this one, unless we can also merge an empty one.
					}
					if (iEnd < 0)
					{
						// no following segment has matching end point.
						// Keep trying, it's remotely possible that some later sequence of segments can be merged.
						iBegin++;
						iMerge++;
						continue;
					}
					// OK, we are going to merge the segments from iMerge to iEnd, keeping but modifying iMerge.
					int kflidFT = StTxtPara.SegmentFreeTranslationFlid(m_cache);
					int hvoFtMerge = m_cache.GetObjProperty(segMerge.Hvo, kflidFT);
					CmIndirectAnnotation ftMerge = null;
					ITsStrBldr[] bldrs = null;
					if (hvoFtMerge != 0)
					{
						ftMerge = new CmIndirectAnnotation(m_cache, hvoFtMerge);
						bldrs = new ITsStrBldr[BtWss.Length];
						for (int i = 0; i < bldrs.Length; i++ )
							bldrs[i] = ftMerge.Comment.GetAlternativeTss(BtWss[i]).GetBldr();
					}
					CmBaseAnnotation segDel = null;
					for (int iDel = iBegin + 1; iDel <= iEnd; iDel++)
					{
						segDel = new CmBaseAnnotation(m_cache,
							m_cache.GetVectorItem(paraDest.Hvo, kflidSegments, iDel));
						delSegs.Add(segDel);
						int hvoFtDel = m_cache.GetObjProperty(segDel.Hvo, kflidFT);
						if (hvoFtDel != 0)
						{
							if (bldrs != null)
							{
								// Enhance JohnT: merge ALL ws alternatives.
								CmIndirectAnnotation ftDel = new CmIndirectAnnotation(m_cache, hvoFtDel);
								// Enhance JohnT: consider inserting space if neither already has one.
								for (int i = 0; i < bldrs.Length; i++ )
									bldrs[i].ReplaceTsString(bldrs[i].Length, bldrs[i].Length, ftDel.Comment.GetAlternativeTss(BtWss[i]));
							}
						}
					}
					if (bldrs != null)
						for (int i = 0; i < bldrs.Length; i++ )
							ftMerge.Comment.SetAlternative(bldrs[i].GetString(), BtWss[i]);
					segMerge.EndOffset = segDel.EndOffset;
					RemoveBtSegs(delSegs, paraDest, iBegin + 1);
					break;
				}
			}
			NoteParaNeedsUpdateMainTrans(paraDest);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete a paragraph from the database
		/// </summary>
		/// <param name="para">paragraph to delete</param>
		/// ------------------------------------------------------------------------------------
		private void DeleteParagraph(StTxtPara para)
		{
			// delete any linked objects (footnotes or pictures)
			para.RemoveOwnedObjectsForString(0, para.Contents.Length);
			RemoveBtSegs(para);

			// delete the paragraph
			StText text = new StText(m_cache, para.OwnerHVO);
			text.ParagraphsOS.Remove(para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Empty the contents of a paragraph
		/// </summary>
		/// <param name="para">paragraph to empty</param>
		/// ------------------------------------------------------------------------------------
		private void EmptyParagraph(StTxtPara para)
		{
			para.RemoveOwnedObjectsForString(0, para.Contents.Length);
			RemoveBtSegs(para);
			para.Contents.Text = string.Empty;
			para.TranslationsOC.RemoveAll();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a SectionMissingInCurrent difference, insert a copy of the revision
		/// section into the current.
		/// </summary>
		/// <param name="diff">The object that encapsulates the difference</param>
		/// ------------------------------------------------------------------------------------
		private void ReplaceCurrentWithRevision_InsertSection(Difference diff)
		{
			Debug.Assert(diff.HvosSectionsRev != null);
			foreach (int hvo in diff.HvosSectionsRev)
				InsertSection(hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a copy of the given Revision section into the Current.
		/// </summary>
		/// <param name="hvoSectionRev">hvo of the Revision section</param>
		/// ------------------------------------------------------------------------------------
		protected void InsertSection(int hvoSectionRev)
		{
			IScrSection sectionRev = new ScrSection(m_cache, hvoSectionRev);

			// Determine the index to insert at.
			// This is for now based on the proper place for the BCV refs in sectionRev
			int insertIndex = GetCurrSectionInsertIndex(sectionRev.VerseRefEnd);

			// Create/get the Current section we are now inserting
			// If there is only one section and it is empty, we want to replace the empty
			// section; otherwise we insert a whole new section.
			IScrSection sectionCurr;
			bool replaceEmptySection = false;
			if (m_bookCurr.SectionsOS.Count == 1 && IsEmptySection(m_bookCurr.SectionsOS[0]))
			{
				replaceEmptySection = true;
				sectionCurr = m_bookCurr.SectionsOS[0];
			}
			else
			{
				sectionCurr = new ScrSection();
				m_bookCurr.SectionsOS.InsertAt(sectionCurr, insertIndex);
				// note: if fields are ever added to the ScrSection object, we'd better be sure they get copied
				sectionCurr.HeadingOA = new StText();
				sectionCurr.ContentOA = new StText();
			}

			// Copy the heading paragraphs
			int headingCount = sectionRev.HeadingOA.ParagraphsOS.Count;
			for (int i = 0; i < headingCount; i++)
			{
				StTxtPara sourcePara = (StTxtPara)sectionRev.HeadingOA.ParagraphsOS[i];
				StTxtPara newPara;
				if (i == 0 && replaceEmptySection)
				{
					//use the existing empty first paragraph as the copy destination
					newPara = (StTxtPara)sectionCurr.HeadingOA.ParagraphsOS[0];
					sourcePara.CopyTo(newPara);
				}
				else
				{
					//append a new section heading paragaph as the copy destination
					int hvo = m_cache.CopyObject(sourcePara.Hvo,
						sectionCurr.HeadingOA.Hvo, (int)StText.StTextTags.kflidParagraphs);
					newPara = new StTxtPara(m_cache, hvo);
				}
			}

			// Copy the content paragraphs, preserving an existing empty paragraph
			// as the last paragraph.
			int contentCount = sectionRev.ContentOA.ParagraphsOS.Count;
			for (int i = 0; i < contentCount; i++)
			{
				StTxtPara sourcePara = (StTxtPara)sectionRev.ContentOA.ParagraphsOS[i];
				StTxtPara newPara;
				// if last para and we're replacing an empty para...
				if (i == contentCount - 1 && replaceEmptySection)
				{
					//use the existing empty paragraph as the last para, for the copy destination
					newPara = (StTxtPara)sectionCurr.ContentOA.ParagraphsOS[contentCount - 1];
					sourcePara.CopyTo(newPara);
				}
				else
				{
					//insert a new paragraph at the original index, as the copy destination
					//TODO TE-5053: These 3 lines could be replaced by calling an enhanced cache.CopyObject .
					newPara = new StTxtPara();
					sectionCurr.ContentOA.ParagraphsOS.InsertAt(newPara, i);
					sourcePara.CopyTo(newPara);
				}
			}

			// Finish updating the remaining fields of the new section
			sectionCurr.AdjustReferences();

			// We need to create the owned objects after the section references have been adjusted.
			// Otherwise CreateOwnedObjects will assume that the footnotes belong to a paragraph
			// with invalid references and clear the footnotes.
			// For heading and content paragraphs we need to copy the para's footnote & picture objects,
			// and update the ORCs to point to the new copies.
			for (int iPara = 0; iPara < headingCount; iPara++)
			{
				StTxtPara sourcePara = (StTxtPara)sectionRev.HeadingOA.ParagraphsOS[iPara];
				StTxtPara newPara = (StTxtPara)sectionCurr.HeadingOA.ParagraphsOS[iPara];

				newPara.CreateOwnedObjects(0, newPara.Contents.Length, this);
				CopyBtSegments(newPara, sourcePara);
			}
			for (int iPara = 0; iPara < contentCount; iPara++)
			{
				StTxtPara sourcePara = (StTxtPara)sectionRev.ContentOA.ParagraphsOS[iPara];
				StTxtPara newPara = (StTxtPara)sectionCurr.ContentOA.ParagraphsOS[iPara];

				newPara.CreateOwnedObjects(0, newPara.Contents.Length, this);
				CopyBtSegments(newPara, sourcePara);
			}

			// If the empty section was not reused, then a section was inserted. Issue a prop
			// change to indicate that a section was inserted.
			if (!replaceEmptySection)
			{
				m_cache.PropChanged(null, PropChangeType.kpctNotifyAll,
					sectionCurr.OwnerHVO, sectionCurr.OwningFlid, insertIndex, 1, 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the section index in the Current where the Revision section (referenced in the
		/// diff) should be inserted.
		/// </summary>
		/// <param name="endRefRev">the end ref of the Revision section that we want to insert
		/// into the Current</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int GetCurrSectionInsertIndex(BCVRef endRefRev)
		{
			CheckDisposed();

			// Look for a section with a reference beyond the given ref; we want to insert
			// at the start of that section.
			foreach (ScrSection section in m_bookCurr.SectionsOS)
				if (section.VerseRefStart > endRefRev)
					return section.IndexInBook;

			// If a section was not found, then insertion should be at the end of existing sections
			return m_bookCurr.SectionsOS.Count;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if a section is empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsEmptySection(IScrSection section)
		{
			if (section.HeadingOA.ParagraphsOS.Count > 1)
				return false;
			if (section.ContentOA.ParagraphsOS.Count > 1)
				return false;
			if (((StTxtPara)section.HeadingOA.ParagraphsOS[0]).Contents.Text != null)
				return false;
			if (((StTxtPara)section.ContentOA.ParagraphsOS[0]).Contents.Text != null)
				return false;

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a SectionAddedToCurrent difference, remove the section from the Current.
		/// </summary>
		/// <param name="diff"></param>
		/// ------------------------------------------------------------------------------------
		private void ReplaceCurrentWithRevision_RemoveSection(Difference diff)
		{
			Debug.Assert(diff.HvosSectionsCurr != null);
			foreach (int hvo in diff.HvosSectionsCurr)
				DeleteOrEmptyOutSection(hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the or empty out the given section.
		/// </summary>
		/// <param name="hvoSection">hvo of the section</param>
		/// ------------------------------------------------------------------------------------
		protected void DeleteOrEmptyOutSection(int hvoSection)
		{
			// Get the section
			ScrSection section = new ScrSection(m_cache, hvoSection);

			// If this is not the last section, then remove it, otherwise just empty it
			// so we don't end up with a book with no sections.
			if (section.OwningBook.SectionsOS.Count > 1)
				DeleteSection(section);
			else
				EmptyOutSection(section);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete a section from a book
		/// </summary>
		/// <param name="section">section to delete</param>
		/// ------------------------------------------------------------------------------------
		private void DeleteSection(ScrSection section)
		{
			// delete all of the paragraphs from the section
			while (section.HeadingOA.ParagraphsOS.Count > 0)
				DeleteParagraph((StTxtPara)section.HeadingOA.ParagraphsOS[0]);
			while (section.ContentOA.ParagraphsOS.Count > 0)
				DeleteParagraph((StTxtPara)section.ContentOA.ParagraphsOS[0]);

			// save the index of the section and delete the section
			int sectionIndex = section.IndexInOwner;
			ScrBook book = section.OwningBook;
			book.SectionsOS.Remove(section);

			// If there is a section that followed the deleted section, then
			// adjust its references.
			if (sectionIndex < book.SectionsOS.Count)
				book.SectionsOS[sectionIndex].AdjustReferences();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Empty out all of the contents of a section leaving only one empty heading paragraph
		/// and one empty content paragraph.
		/// </summary>
		/// <param name="section">section to empty</param>
		/// ------------------------------------------------------------------------------------
		private void EmptyOutSection(ScrSection section)
		{
			// Delete all but one paragraph in the heading.
			while (section.HeadingOA.ParagraphsOS.Count > 1)
				DeleteParagraph((StTxtPara)section.HeadingOA.ParagraphsOS[0]);

			// Delete all but one paragraph in the content.
			while (section.ContentOA.ParagraphsOS.Count > 1)
				DeleteParagraph((StTxtPara)section.ContentOA.ParagraphsOS[0]);

			// remove the contents of the heading and content paragraphs
			EmptyParagraph((StTxtPara)section.HeadingOA.ParagraphsOS[0]);
			EmptyParagraph((StTxtPara)section.ContentOA.ParagraphsOS[0]);

			section.AdjustReferences();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a SectionHeadMissingInCurrent difference, insert the section head in the
		/// Current.
		/// </summary>
		/// <param name="diff"></param>
		/// ------------------------------------------------------------------------------------
		private void ReplaceCurrentWithRevision_InsertSectionBreak(Difference diff)
		{
			// get the Revision section that owns the added section head
			Debug.Assert(diff.HvosSectionsRev.Length == 1);
			ScrSection sectionRev = new ScrSection(m_cache, diff.HvosSectionsRev[0]);

			// get the Current paragraph we will move or divide, and it's section
			StTxtPara paraCurr = new StTxtPara(m_cache, diff.HvoCurr);
			ScrSection sectionCurr = ScrSection.GetSectionFromParagraph(paraCurr);
			// the paraCurr must be in section content
			Debug.Assert((new StText(m_cache, paraCurr.OwnerHVO)).OwningFlid ==
				(int)ScrSection.ScrSectionTags.kflidContent);

			// Insert empty section after the Current section
			int insertIndex = sectionCurr.IndexInBook + 1;
			ScrSection newSection =
				(ScrSection)ScrSection.CreateEmptySection(sectionCurr.OwningBook, insertIndex);

			// Move Current section's Content after ichMinCurr to new section.
			// if we divide at a paragraph break in Current
			if (diff.IchMinCurr == 0)
			{
				// Move paraCurr and the following section content paras to the new section
				StText.MoveTextParagraphs(sectionCurr.ContentOA, newSection.ContentOA,
					paraCurr.IndexInOwner, false);

				// If, after moving the content to the new section, the previous owning section
				// is left without any paragraphs, then we need to make sure we add an empty
				// one back in. Otherwise, we'll be left with a corrupt database. (TE-7132).
				if (sectionCurr.ContentOA.ParagraphsOS.Count == 0)
				{
					sectionCurr.ContentOA.ParagraphsOS.Append(new StTxtPara());
					StTxtPara para = new StTxtPara(m_cache, sectionCurr.ContentOA.ParagraphsOS[0].Hvo);
					para.StyleRules = paraCurr.StyleRules;
					m_cache.PropChanged(null, PropChangeType.kpctNotifyAll,	para.OwnerHVO,
						(int)StText.StTextTags.kflidParagraphs, 0, 1, 0);
				}
			}
			else
			{
				// Divide the paraCurr at ichMinCurr, and move the latter part plus the
				//  following section content paras to the new section
				StText.MovePartialContents(sectionCurr.ContentOA, newSection.ContentOA,
					paraCurr.IndexInOwner, diff.IchMinCurr, false);

				// If, after moving the partial paragraph we have a new section in the current
				// that has no paragraph, we need to add one. Otherwise we'll leave the database
				// in a corrupt state. This will happen if the section being inserted has no
				// contents. (TE-7208 and TE-7132).
				if (newSection.ContentOA.ParagraphsOS.Count == 0)
					newSection.ContentOA.ParagraphsOS.Append(new StTxtPara());

				// Apply the paragraph style from the Revision Content's first para to the
				//  portion of ParaCurr that was moved into a new para
				//  (required to give the Current the same properties as the Revision)
				StTxtPara newContentPara = new StTxtPara(m_cache, newSection.ContentOA.ParagraphsOS[0].Hvo);
				StTxtPara revContentPara = new StTxtPara(m_cache, sectionRev.ContentOA.ParagraphsOS[0].Hvo);
				newContentPara.StyleRules = revContentPara.StyleRules;

				// Adjust diff hvos and ichs for the portion of paraCurr that was moved
				//  into a new para
				m_differences.FixFollowingParaDiffs(diff, diff.HvoCurr, newContentPara.Hvo,
					-diff.IchMinCurr);

				// For now, leave the entire BT of paraCurr with the original part of the divided paragraph.
				// but the BT status must be cleared
				paraCurr.MarkBackTranslationsAsUnfinished();
			}

			// Copy the Revision section heading to the new section
			//  note: first the section contents are moved, THEN we copy the heading, because
			//  CreateOwnedObjects() for the new heading must be able to scan what remains in
			//  the prior section to get an accurate NextFootnoteIndex
			for (int i = 0; i < sectionRev.HeadingOA.ParagraphsOS.Count; i++)
			{
				StTxtPara sourcePara = (StTxtPara)sectionRev.HeadingOA.ParagraphsOS[i];
				//append a new section heading paragaph as the copy destination
				int hvoNewPara = m_cache.CopyObject(sourcePara.Hvo,
					newSection.HeadingOA.Hvo, (int)StText.StTextTags.kflidParagraphs);
				// Copy the para's footnote & picture objects, and update the ORCs to point to the new copies
				StTxtPara newPara = new StTxtPara(m_cache, hvoNewPara);
				newPara.CreateOwnedObjects(0, newPara.Contents.Length, this);
			}

			// finish up
			sectionCurr.AdjustReferences();
			newSection.AdjustReferences();

			m_cache.PropChanged(null, PropChangeType.kpctNotifyAll,
				newSection.OwnerHVO, newSection.OwningFlid, insertIndex, 1, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a SectionHeadAddedToCurrent difference, remove the section head in the
		/// Current.
		/// </summary>
		/// <param name="diff"></param>
		/// ------------------------------------------------------------------------------------
		private void ReplaceCurrentWithRevision_RemoveSectionBreak(Difference diff)
		{
			// get the Current section
			Debug.Assert(diff.HvosSectionsCurr.Length == 1);
			ScrSection section = new ScrSection(m_cache, diff.HvosSectionsCurr[0]);
			// diff should never have a "sectionHeadAddedToCurrent" for the first section
			Debug.Assert(section.IndexInBook > 0);

			// and the previous section
			ScrSection prevSection = section.PreviousSection;
			int nParasInPrevSectionContent = prevSection.ContentOA.ParagraphsOS.Count;

			// Move the Content paragraphs, and then delete the section
			MergeSectionContentIntoPrevious(section);

			// if the DestIP in the Revision book is in mid-paragraph...
			StTxtPara destPara = new StTxtPara(m_cache, diff.HvoRev);
			if (diff.IchMinRev > 0 && diff.IchMinRev < destPara.Contents.Length)
			{
				// ... then the Rev paragarph had been divided by the the new Curr section break,
				//  and we need to 're-combine' the divided paragraph.
				// Combine the first para moved with the last original para just before it.
				StTxtPara firstMovedPara = new StTxtPara(m_cache,
					prevSection.ContentOA.ParagraphsOS[nParasInPrevSectionContent].Hvo);
				int firstMovedParaHvo = firstMovedPara.Hvo;
				StTxtPara prevPara = new StTxtPara(m_cache,
					prevSection.ContentOA.ParagraphsOS[nParasInPrevSectionContent - 1].Hvo);
				int prevParaOrigLen = prevPara.Contents.UnderlyingTsString.Length;

				MergeParagraphIntoPrevious(firstMovedPara);

				//adjust diff hvos and ichs for the para just merged
				m_differences.FixFollowingParaDiffs(diff, firstMovedParaHvo, prevPara.Hvo,
					prevParaOrigLen);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the content of the given section into the previous section's content.
		/// The section content is moved as whole paragraphs.
		/// Then deletes the given section. Heading text of the given section is discarded.
		/// </summary>
		/// <param name="section">the given section</param>
		/// ------------------------------------------------------------------------------------
		private void MergeSectionContentIntoPrevious(ScrSection section)
		{
			int nParasToMove = section.ContentOA.ParagraphsOS.Count;

			// move the Content into the previous section
			ScrSection prevSection = section.PreviousSection;
			int insertIndex = prevSection.ContentOA.ParagraphsOS.Count;
			StText.MoveTextContents(section.ContentOA, prevSection.ContentOA, true);
			// (FdoCache.MoveOwningSequence() does the PropChanged for us)

			prevSection.AdjustReferences();

			// delete the given section
			section.OwningBook.SectionsOS.Remove(section);
			// fdo Remove does propchanged

			// note: we should not need to adjust references in following section
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the given paragraph into the previous paragarph.
		/// </summary>
		/// <param name="para">The given paragraph.</param>
		/// ------------------------------------------------------------------------------------
		private void MergeParagraphIntoPrevious(StTxtPara para)
		{
			// Append the paragraph contents to the previous paragraph
			StTxtPara prevPara = para.PreviousParagraph;
			ITsString tss = prevPara.Contents.UnderlyingTsString;
			ITsStrBldr bldr = tss.GetBldr();
			bldr.ReplaceTsString(tss.Length, tss.Length, para.Contents.UnderlyingTsString);
			prevPara.Contents.UnderlyingTsString = bldr.GetString();
			// FDO takes care of doing the PropChanged when setting the TsString

			// Append the translations to the previous paragraph translations
			ICmTranslation prevBT = prevPara.GetOrCreateBT();
			prevBT.Translation.MergeAlternatives(para.GetOrCreateBT().Translation, true, "");
			// and the BT status must be cleared
			prevPara.MarkBackTranslationsAsUnfinished();
			//TODO: if we add other types of translations someday, those will need to be
			// coordinated and merged too.

			// Delete the given paragraph
			StText text = new StText(m_cache, para.OwnerHVO);
			text.ParagraphsOS.Remove(para);
			// fdo Remove does propchanged
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given list of subDiffs of types VerseMoved and VerseMissing,
		/// execute a ReplaceCurrentWithRevision for each one.
		/// </summary>
		/// <param name="subDiffs">The given list of subDiffs.</param>
		/// ------------------------------------------------------------------------------------
		private void ReplaceCurrentWithRevision_MoveVersesBack(List<Difference> subDiffs)
		{
// TODO: also handle VerseMissing diffs as subDiffs here. be sure all subDiffs are in the same order
//	as their position in the Current

			// All VerseMoved and VerseMissing subdiffs should get inserted at the same 'MovedFrom' para
			//  and at ich of zero. So do them in reverse order -
			//  that way the first subDiff winds up at the start of the para, and last subDiff last
			for (int i = subDiffs.Count - 1; i >= 0; i--)
			{
				Difference subDiff = subDiffs[i];
				ReplaceCurrentWithRevision_MoveVerseBack(subDiff);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a VerseMoved difference,
		/// move the Current verse back to the position it had in the Revision.
		/// </summary>
		/// <param name="diff">The diff.</param>
		/// ------------------------------------------------------------------------------------
		private void ReplaceCurrentWithRevision_MoveVerseBack(Difference diff)
		{
			Debug.Assert(diff.DiffType == DifferenceType.VerseMoved);

			// Background: Reverting a "VerseMoved" diff involves:
			//  * deleting the verse in it's original pargraph at the diff's "Current" range
			//  * inserting the verse back at the diff's "MovedFrom" paragraph and position

			// isolate the text of the Current verse
			StTxtPara paraCurr = new StTxtPara(m_cache, diff.HvoCurr);
			ITsStrBldr stringCurr = paraCurr.Contents.UnderlyingTsString.GetBldr();
			stringCurr.Replace(diff.IchLimCurr, stringCurr.Length, "", null);
			stringCurr.Replace(0, diff.IchMinCurr, "", null);

			// insert the verse text at the diff's MovedFromIP
			StTxtPara paraDest = new StTxtPara(m_cache, diff.HvoMovedFrom);
			ITsStrBldr strDestBldr = paraDest.Contents.UnderlyingTsString.GetBldr();
			strDestBldr.ReplaceTsString(diff.IchMovedFrom, diff.IchMovedFrom, stringCurr.GetString());
			paraDest.Contents.UnderlyingTsString = strDestBldr.GetString();

			//TODO: TE-5090 should add somethng like
			// paraCurr.MoveOwnedObjectsWithinSequence(ichMin, ichLim, paraDest, ichDest, objInfoProvider)
			// remove objects (all with ORCs in the range to be moved) from their owner's sequence
			// insert these objects in their owner's sequence at the proper index for the new destination
			// remove ref ORCS from paraOrig translations, unless someday we are able to move a portion of the translation

			// Remove the verse text at the diff's Current range
			stringCurr = paraCurr.Contents.UnderlyingTsString.GetBldr();
			stringCurr.Replace(diff.IchMinCurr, diff.IchLimCurr, "", null);
			paraCurr.Contents.UnderlyingTsString = stringCurr.GetString();

			//IMPROVEMENT: if the BT verses are marked in in paraCurr back translation, someday...
			//in the BT of paraCurr, remove the BT of the verses we remove from paraCurr, and
			//in the BT of paraDest, insert the BT of the verses we insert in paraDest

			// fix hvos and adjust offsets stored in the differences list
			m_differences.FixDiffsForVerseMovedBack(diff);

			// adjust the references for the sections that were changed
			ScrSection section = ScrSection.GetSectionFromParagraph(paraCurr);
			if (section != null)
				section.AdjustReferences();
			section = ScrSection.GetSectionFromParagraph(paraDest);
			if (section != null)
				section.AdjustReferences();
		}
		#endregion

		#region RecalculateDifferences
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to display the progress dialog.
		/// </summary>
		/// <value>Always <c>true</c> (overridden in tests).</value>
		/// ------------------------------------------------------------------------------------
		protected virtual bool DisplayUi
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recalculate the difference list after an edit has occurred.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RecalculateDifferences()
		{
			CheckDisposed();

			using (ProgressDialogWithTask progressDlg = new ProgressDialogWithTask(Form.ActiveForm))
			{
				progressDlg.Message = TeDiffViewResources.kstidRecalculateDiff;
				progressDlg.RunTask(DisplayUi, new BackgroundTaskInvoker(RecalculateDifferences));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recalculate the difference list after an edit has occurred.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">The parameters (ignored).</param>
		/// <returns>always null</returns>
		/// ------------------------------------------------------------------------------------
		private object RecalculateDifferences(IAdvInd4 progressDlg, params object[] parameters)
		{
			// Rebuild the difference list
			Differences.Clear();
			DetectDifferences(progressDlg);

			// Remove any differences from the new list that were already marked as reviewed.
			for (Difference reviewedDiff = m_reviewedDiffs.MoveFirst();
				reviewedDiff != null;
				reviewedDiff = m_reviewedDiffs.MoveNext())
			{
				Difference alreadyReviewed = null;
				for (Difference newDiff = Differences.MoveFirst();
					newDiff != null;
					newDiff = Differences.MoveNext())
				{
					if (newDiff.IsEquivalent(reviewedDiff))
					{
						alreadyReviewed = newDiff;
						break;
					}
				}
				if (alreadyReviewed != null)
					Differences.Remove(alreadyReviewed);
			}
			return null;
		}
		#endregion

		#region IObjectMetaInfoProvider implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the next 0-based footnote index to use for the first footnote being created
		/// in the given paragraph at or after the given char location.
		/// This should be called only for the first foonote; the caller is expected to properly
		/// increment the returned index if multiple contiguous footnotes are to be inserted.
		/// Typically this is called from StTxtPara.CreateOwnedObjects() when footnote material
		/// has been insterted into a paragraph.
		/// <param name="para">Paragraph to start looking in</param>
		/// <param name="ich">offset in paragraph to start looking before</param>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int NextFootnoteIndex(StTxtPara para, int ich)
		{
			CheckDisposed();

			int iFootnote = 0;
			ScrFootnote prevFootnote = ScrFootnote.FindPreviousFootnote(para, ich);

			if (prevFootnote != null)
				iFootnote = prevFootnote.IndexInOwner + 1;

			return iFootnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the picture folder to use for any copied pictures
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string PictureFolder
		{
			get
			{
				CheckDisposed();

				return StringUtils.LocalPictures;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns "Note Marker"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string FootnoteMarkerStyle
		{
			get
			{
				CheckDisposed();
				return ScrStyleNames.FootnoteMarker;
			}
		}
		#endregion
	}
}
