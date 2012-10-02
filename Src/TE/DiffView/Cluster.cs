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
// File: Cluster.cs
// Responsibility: TE Team
//
// <remarks> This file implements the following classes:
// Cluster, OverlapInfo, ClusterType, ClusterListHelper.
// Usage: Call a static method in the ClusterListHelper to generate a Cluster list.
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.FDO;
using System.Diagnostics;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.TE
{
	#region Cluster class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A Cluster holds details about a cluster of Current and Revision paragraphs or
	/// sections, which overlap with one another in their verse ref range.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class Cluster : IComparable
	{
		// These member variables should be set individually only by the ClusterListHelper class.
		/// <summary>The min verse ref (inclusive) of all items in this cluster</summary>
		internal BCVRef verseRefMin = new BCVRef();
		/// <summary>The max verse ref (inclusive) of all items in this cluster</summary>
		internal BCVRef verseRefMax = new BCVRef();

		/// <summary>list of Current OverlapInfo objects (representing sections or paras)
		/// in this cluster</summary>
		internal List<OverlapInfo> itemsCurr;
		/// <summary>list of Revision OverlapInfo objects (representing sections or paras)
		/// in this cluster</summary>
		internal List<OverlapInfo> itemsRev;

		/// <summary>the classification of the items in this cluster</summary>
		internal ClusterType clusterType = ClusterType.None;

		/// <summary>if one of the lists is empty (i.e. the clusterType is Added or Missing),
		/// this is the "index in owner" where the added items would go in the other book</summary>
		internal int indexToInsertAtInOther = -1;

		/// <summary>the sort key for the cluster, optional</summary>
		internal int[] sortKey = new int[2] { 0, 0 };

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Bare bones constructor.
		/// This constructor is not intended for use outside of the ClusterListHelper class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Cluster()
		{
			itemsCurr = new List<OverlapInfo>(6); //initial capacity of 6 seems reasonable
			itemsRev = new List<OverlapInfo>(6);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines the cluster type based upon how many items it has in rev and curr.
		/// If it does not have any items in rev and curr (ie-it's been constructed but not
		/// yet filled out) no type will be set, or Debug.Assert will fail.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void DetermineType()
		{
			// determine the cluster type
			if (this.itemsRev.Count == 1 && this.itemsCurr.Count == 1)
				this.clusterType = ClusterType.MatchedItems;
			else if (this.itemsRev.Count == 0 && this.itemsCurr.Count == 1)
				this.clusterType = ClusterType.AddedToCurrent;
			else if (this.itemsRev.Count == 1 && this.itemsCurr.Count == 0)
				this.clusterType = ClusterType.MissingInCurrent;
			else if (this.itemsRev.Count == 0 && this.itemsCurr.Count > 1)
				this.clusterType = ClusterType.AddedToCurrent;
			else if (this.itemsRev.Count > 1 && this.itemsCurr.Count == 0)
				this.clusterType = ClusterType.MissingInCurrent;
			else if (this.itemsRev.Count == 1 && this.itemsCurr.Count > 1)
				this.clusterType = ClusterType.SplitInCurrent;
			else if (this.itemsRev.Count > 1 && this.itemsCurr.Count == 1)
				this.clusterType = ClusterType.MergedInCurrent;
			else if (this.itemsRev.Count > 1 && this.itemsCurr.Count > 1)
				this.clusterType = ClusterType.MultipleInBoth;
			else Debug.Assert(false, "problem determinining cluster type");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the Current is the source.
		/// </summary>
		/// <param name="clusterType">Type of the cluster--should be a missing or added.</param>
		/// <returns><c>true</c> if the cluster item(s) was/were added to current;
		/// <c>false</c> if the cluster item(s) was/were missing in current</returns>
		/// <exception cref="InvalidOperationException">thrown if clusterType is not added to current
		/// or missing in current.</exception>
		/// ------------------------------------------------------------------------------------
		public static bool CurrentIsSource(ClusterType clusterType)
		{
			if (clusterType == ClusterType.AddedToCurrent || clusterType == ClusterType.OrphansInCurrent)
				return true;

			if (clusterType == ClusterType.MissingInCurrent || clusterType == ClusterType.OrphansInRevision)
				return false;

			throw new InvalidOperationException(string.Format(
				"Cluster type {0} incompatible. Cluster type must be added to or missing in current.",
				clusterType.ToString()));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Generates a sort key based upon items it has in rev and curr.
		/// Note: A one-sided cluster must already have a valid indexToInsertAtInOther.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void GenerateSortKey()
		{
			// First part of sort key reflects the position of the first item in Current
			if (this.itemsCurr != null && this.itemsCurr.Count > 0)
			{
				this.sortKey[0] = this.itemsCurr[0].indexInOwner;
			}
			else
				this.sortKey[0] = this.indexToInsertAtInOther;

			// Second part of sort key reflects the position of the first item in Revision
			if (this.itemsRev != null && this.itemsRev.Count > 0)
			{
				this.sortKey[1] = this.itemsRev[0].indexInOwner;
			}
			else
				this.sortKey[1] = this.indexToInsertAtInOther;

		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an array of the Revision items for this cluster.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ICmObject[] ItemsRev
		{
			get { return GetArrayOfItems(itemsRev); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an array of the Current items for this cluster.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ICmObject[] ItemsCurr
		{
			get { return GetArrayOfItems(itemsCurr); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an array of the Source items for this cluster, i.e. the list of
		/// of added items.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ICmObject[] ItemsSource
		{
			get { return GetArrayOfItems(SourceItems); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if this cluster of ScrVerses spans a paragraph break in either the current
		/// or revision.
		/// </summary>
		/// <returns><c>true</c> if either current or revision spans a para break; <c>false</c>
		/// otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool	SpansParaBreak
		{
			get
			{
				ICmObject objectFirst;
				if (this.itemsCurr.Count > 1)
				{
					objectFirst = this.itemsCurr[0].myObj;
					for (int iCurr = 1; iCurr < this.itemsCurr.Count; iCurr++)
					{
						if (this.itemsCurr[iCurr].myObj != objectFirst)
							return true;
					}
				}

				if (this.itemsRev.Count > 1)
				{
					objectFirst = this.itemsRev[0].myObj;
					for (int iRev = 1; iRev < this.itemsRev.Count; iRev++)
					{
						if (this.itemsRev[iRev].myObj != objectFirst)
							return true;
					}
				}

				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this ScrVerse overlap cluster contains a verse
		/// bridge difference, i.e. if this cluster has a difference between the current and
		/// revision in how verses are represented with bridges.
		/// </summary>
		/// <value><c>true</c> if current and revision contains a verse bridge difference;
		/// otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool ContainsVerseBridgeDifference
		{
			get
			{
				if (this.itemsCurr.Count > 0 && this.itemsRev.Count > 0)
				{
					BCVRef startRefCurr = this.itemsCurr[0].verseRefMin;
					BCVRef endRefCurr = this.itemsCurr[0].verseRefMax;

					for (int iCurr = 1; iCurr < this.itemsCurr.Count; iCurr++)
					{
						// if we find a different starting or end reference in the current...
						if (this.itemsCurr[iCurr].verseRefMin != startRefCurr ||
							this.itemsCurr[iCurr].verseRefMax != endRefCurr)
						{
							// we found that there is a difference in verse bridges (i.e.
							// we encountered another ScrVerse reference indicating that a
							// verse bridge range in the revision overlaps with
							// different verses in the current).
							return true;
						}
					}

					for (int iRev = 0; iRev < this.itemsRev.Count; iRev++)
					{
						// if we find a different starting or end reference in any Revision than
						// in the first Current item...
						if (this.itemsRev[iRev].verseRefMin != startRefCurr ||
							this.itemsRev[iRev].verseRefMax != endRefCurr)
						{
							// we found that there is a difference in verse bridges (i.e.
							// we encountered another ScrVerse reference indicating that a
							// verse bridge range in the current overlaps with
							// different verses in the revision).
							return true;
						}
					}
				}
				return false;
			}
		}
		#endregion

		// helper function creates an hvo array for the given item list
		private ICmObject[] GetArrayOfItems(List<OverlapInfo> itemList)
		{
			ICmObject[] itemArray = new ICmObject[itemList.Count];
			for (int i = 0; i < itemList.Count; i++)
				itemArray[i] = itemList[i].myObj;
			return itemArray;
		}

		#region For processing Missing/Added cluster types, with Source & Dest params
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor creates a new one-sided cluster with only one item.
		/// This is valid only for Missing/Added Cluster types.
		/// Useful for forming smaller clusters as we break down a complex cluster.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Cluster(ClusterType type, OverlapInfo sourceItem, int indexToInsertAtInDest)
		{
			Debug.Assert(type == ClusterType.AddedToCurrent || type == ClusterType.MissingInCurrent);

			clusterType = type;
			verseRefMin = sourceItem.verseRefMin;
			verseRefMax = sourceItem.verseRefMax;
			indexToInsertAtInOther = indexToInsertAtInDest;
			itemsCurr = new List<OverlapInfo>(6);
			itemsRev = new List<OverlapInfo>(6);
			SourceItems.Add(sourceItem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the source items list for this cluster, i.e. the list of added items.
		/// This is valid only for Missing/Added (i.e. one-sided) Cluster types.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<OverlapInfo> SourceItems
		{
			get
			{
				if (clusterType == ClusterType.AddedToCurrent)
					return itemsCurr;
				else if (clusterType == ClusterType.MissingInCurrent)
					return itemsRev;
				else
					throw new Exception("Cluster.SourceItems - clusterType must be Added or Missing");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the given source item to this one-sided cluster.
		/// This is valid only for Missing/Added Cluster types.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddSourceItem(OverlapInfo sourceItem)
		{
			// This cluster must already have the clusterType set and the source list started.
			Debug.Assert(clusterType == ClusterType.AddedToCurrent ||
				clusterType == ClusterType.MissingInCurrent);
			Debug.Assert(SourceItems.Count > 0);

			// Add the source item to the correct list
			SourceItems.Add(sourceItem);
			// Be sure to update the references as well
			verseRefMin = Math.Min(verseRefMin, sourceItem.verseRefMin);
			verseRefMax = Math.Max(verseRefMax, sourceItem.verseRefMax);
		}
		#endregion

		#region Methods for merging similar clusters when we want to process them as one
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a shallow copy of this cluster and returns it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Cluster Clone()
		{
			// Create the cloned cluster ...
			Cluster toReturn = new Cluster();
			// ... And dump our data into it, making shallow copies of the ArrayLists
			toReturn.clusterType = clusterType;
			toReturn.verseRefMax = verseRefMax;
			toReturn.verseRefMin = verseRefMin;
			toReturn.indexToInsertAtInOther = indexToInsertAtInOther;
			toReturn.itemsCurr = new List<OverlapInfo>(itemsCurr.ToArray());
			toReturn.itemsRev = new List<OverlapInfo>(itemsRev.ToArray());
			toReturn.sortKey = sortKey;

			// Finally, return the clone
			return toReturn;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the given cluster's source item into this cluster.
		/// This is valid only for similar Missing/Added Cluster types.
		/// </summary>
		/// <param name="cluster">the given cluster.</param>
		/// ------------------------------------------------------------------------------------
		public void MergeSourceItems(Cluster cluster)
		{
			// our given cluster must be 'similar'
			Debug.Assert(this.IsSimilar(cluster));

			// Do the merge
			// our given cluster normally has only one source item; that's all we'll accomodate for now
			Debug.Assert(cluster.SourceItems.Count == 1);
			AddSourceItem((OverlapInfo)cluster.SourceItems[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given cluster is similar to this cluster.
		/// Purpose: Consective "similar" Added/Missing clusters may be combined and form a
		/// single difference.
		/// </summary>
		/// <param name="cluster">the given cluster to compare with</param>
		/// <returns> <c>true</c> if the given cluster is similar; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool IsSimilar(Cluster cluster)
		{
			// if this cluster is not similar, we are done
			if (cluster.clusterType != this.clusterType)
				return false;
			if (cluster.indexToInsertAtInOther != this.indexToInsertAtInOther) //Dest index
				return false;

			return true;
		}
		#endregion

		#region Methods for determining empty paragraphs
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether a stanza break is at the specified index.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <param name="fIsCurrent">if set to <c>true</c> check a current item;
		/// <c>false</c> to check a revision item.</param>
		/// ------------------------------------------------------------------------------------
		public bool IsStanzaBreak(int index, bool fIsCurrent)
		{
			if (index >= Items(fIsCurrent) || Item(index, fIsCurrent) == null)
				return false;

			return Item(index, fIsCurrent).isStanzaBreak;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of items in the current or revision.
		/// </summary>
		/// <param name="fIsCurrent">if set to <c>true</c> get the current number of items;
		/// <c>false</c> to get the revision number of items.</param>
		/// ------------------------------------------------------------------------------------
		internal int Items(bool fIsCurrent)
		{
			return (fIsCurrent) ? itemsCurr.Count : itemsRev.Count;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the an item at a specified index in the current or revision.
		/// </summary>
		/// <param name="index">specified index</param>
		/// <param name="fIsCurrent">if set to <c>true</c> get the current item at index;
		/// <c>false</c> to get the revision item at index.</param>
		/// ------------------------------------------------------------------------------------
		internal OverlapInfo Item(int index, bool fIsCurrent)
		{
			Debug.Assert(index < ((fIsCurrent) ? itemsCurr.Count : itemsRev.Count),
				"Index out of range");
			return (fIsCurrent) ? itemsCurr[index] : itemsRev[index];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the item list in the current or revision.
		/// </summary>
		/// <param name="fIsCurrent">if set to <c>true</c> get the current item at index;
		/// <c>false</c> to get the revision item at index.</param>
		/// ------------------------------------------------------------------------------------
		internal List<OverlapInfo> ItemList(bool fIsCurrent)
		{
			return (fIsCurrent) ? itemsCurr : itemsRev;
		}
		#endregion

		#region IComparable Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the current instance with another object of the same type.
		/// </summary>
		/// <param name="obj">An object to compare with this instance.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the objects being compared.
		/// The return value has these meanings:
		/// * Less than zero This instance is less than obj.
		/// * Zero This instance is equal to obj.
		/// * Greater than zero This instance is greater than obj.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">obj is not the same type as this
		/// instance. </exception>
		/// ------------------------------------------------------------------------------------
		public int CompareTo(object obj)
		{

#if __MonoCS__
			// TODO-Linux: List.Sort seems to call CompareTo on the same item.
			// Maybe duplicates in the list? Investigate.
			if (obj == this)
				return 0;
#endif

			// Cast the given object and store it, so we don't have to keep casting it
			Cluster otherCluster = (Cluster)obj;

			// Walk down the list, starting at the first level
			for (int i = 0; i < this.sortKey.Length; i++)
			{
				// If at our current level of the depth, the object given to compare to
				// us is less than us, return 1 to show that we are bigger
				if (otherCluster.sortKey[i] < this.sortKey[i])
				{
					return 1;
				}
				// If it is greater than us, return -1
				else if (otherCluster.sortKey[i] > this.sortKey[i])
				{
					return -1;
				}
			}

			// The indices of the items are equal. Now compare on cluster type.
			int typeCompareVal = this.clusterType.CompareTo(otherCluster.clusterType);
			if (typeCompareVal != 0)
				return typeCompareVal;

			// If the whole loop ran through and never returned, return 0
			// to signify that the objects are equal
			Debug.Fail("We found two equal cluster sort keys.");
			return 0;

		}

		#endregion
	}
	#endregion

	#region OverlapInfo class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// OverlapInfo is a proxy structure that can instantiated for a given scripture section or
	/// paragraph. These items normally have a verse ref range.
	/// OverlapInfo holds information about overlapping or related section(s) or paragraph(s)
	/// that exist in another book revision.
	/// OverlapInfo objects are used to determine Clusters of overlapping or related scripture
	/// sections or paragraphs, and then to detect Differences within each Cluster.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class OverlapInfo
	{
		// public member variables
		/// <summary>The min verse ref (inclusive) of this scripture section or para</summary>
		public BCVRef verseRefMin;
		/// <summary>The max verse ref (inclusive) of this scripture section or para</summary>
		public BCVRef verseRefMax;

		/// <summary>
		/// bookIsFromRev signifies this OverlapInfo as being part of either the Revision book
		/// or the Current book (this is important to the algorithm that determines overlap clusters)
		/// </summary>
		public bool bookIsFromRev;

		/// <summary>index of the section or paragraph that this OverlapInfo represents;
		/// specifically, its index in its owner's sequence</summary>
		public int indexInOwner = -1;

		/// <summary>the section or paragraph that this OverlapInfo represents </summary>
		public ICmObject myObj;

		/// <summary>the StText that owns the paragraph (if this OverlapInfo is for
		/// a ScrVerse)</summary>
		public IStText myParaOwner;

		/// <summary>whether the ScrVerse has contents</summary>
		public bool isStanzaBreak = false;

		/// <summary>list of other OverlapInfo objects (representing sections or paras or ScrVerses)
		/// in the other book (Current or Revision)
		/// which have a relationship (ref overlap, section correlation, etc) with myself</summary>
		public List<OverlapInfo> overlappedItemsInOther;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// zero-arg constructor is private to restrict its use
		/// </summary>
		/// --------------------------------------------------------------------------------
		private OverlapInfo() { }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for an OverlapInfo proxy for a ScrSection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public OverlapInfo(IScrSection section, bool isRevisionBook)
		{
			// Set the max and the min to that supplied by our caller
			verseRefMax = section.VerseRefMax;
			verseRefMin = section.VerseRefMin;
			// Set the type
			bookIsFromRev = isRevisionBook;
			// set the index
			indexInOwner = section.IndexInOwner;
			// set the hvo
			myObj = section;

			// the overlap list should be created when needed
			overlappedItemsInOther = null;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for an OverlapInfo proxy for a ScrVerse.
		/// Initializes a new instance of the <see cref="OverlapInfo"/> class.
		/// </summary>
		/// <param name="verse">The ScrVerse.</param>
		/// <param name="isRevisionBook">Type of the book (current or revision).</param>
		/// <param name="indexInList">The index of the ScrVerse in its owning list.</param>
		/// --------------------------------------------------------------------------------
		public OverlapInfo(ScrVerse verse, bool isRevisionBook, int indexInList)
		{
			// Set the max and the min to that supplied by our caller
			verseRefMin = verse.StartRef;
			verseRefMax = verse.EndRef;

			// Set the type
			bookIsFromRev = isRevisionBook;
			// set the index
			indexInOwner = indexInList;
			// set the hvo
			myObj = verse.Para;
			// set the hvo of the owning StText
			myParaOwner = verse.ParaOwner;
			// set whether the ScrVerse is from an empty paragraph
			isStanzaBreak = verse.IsStanzaBreak;
			// the overlap list should be created when needed
			overlappedItemsInOther = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for an OverlapInfo proxy for a StTxtPara.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public OverlapInfo(IStTxtPara para, BCVRef refMin, BCVRef refMax, bool isRevisionBook)
		{
			// Set the max and the min to that supplied by our caller
			verseRefMax = new BCVRef(refMax);
			verseRefMin = new BCVRef(refMin);
			// Set the type
			bookIsFromRev = isRevisionBook;
			// set the index
			indexInOwner = para.IndexInOwner;
			// set the hvo
			myObj = para;

			// the overlap list should be created when needed
			overlappedItemsInOther = null;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Clones the text information of this OverlapInfo object, without copying it's
		/// references (overlappedItemsInOther will be set to null)
		/// </summary>
		/// <returns>The cloned OverlapInfo</returns>
		/// --------------------------------------------------------------------------------
		public OverlapInfo Clone()
		{
			OverlapInfo toReturn = new OverlapInfo();
			toReturn.verseRefMin = new BCVRef(this.verseRefMin);
			toReturn.verseRefMax = new BCVRef(this.verseRefMax);
			toReturn.bookIsFromRev = this.bookIsFromRev;
			toReturn.indexInOwner = this.indexInOwner;
			toReturn.myObj = this.myObj;
			// don't copy the related items list, since caller's don't need it,
			//  and it might create an infinite loop if you make a deep copy
			toReturn.overlappedItemsInOther = null;
			return toReturn;
		}
	}
	#endregion

	#region ClusterType enum
	/// --------------------------------------------------------------------------------
	/// <summary>
	/// ClusterType enum describes the relationship of the items within a Cluster,
	/// and also the type of processing needed for the Cluster.
	/// </summary>
	/// --------------------------------------------------------------------------------
	public enum ClusterType
	{
		/// <summary>type not determined</summary>
		None,
		/// <summary>One item each in Current and Revision, requiring a straightforward
		/// comparison of the data within</summary>
		MatchedItems,
		/// <summary> One item in Current, none in Revision</summary>
		AddedToCurrent,
		/// <summary> One item in Revision, none in Current</summary>
		MissingInCurrent,
		/// <summary>Orphan(s) in Current left over after extracting
		/// correlated pairs from a complex cluster</summary>
		OrphansInCurrent,
		/// <summary>Orphan(s) in Revision left over after extracting
		/// correlated pairs from a complex cluster</summary>
		OrphansInRevision,
		/// <summary> Multiple items in Current, one in Revision</summary>
		SplitInCurrent,
		/// <summary> Multiple items in Revision, one in Current</summary>
		MergedInCurrent,
		/// <summary>Multiple items in both Current and Revision, requiring further detailed
		/// analysis of which references were moved</summary>
		MultipleInBoth,
	}
	#endregion

	#region ClusterListHelper class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The ClusterListHelper is an implementation class that provides static methods to build
	/// a useful list of Clusters of sections (or paragraphs) that have overlapping verse ref
	/// ranges.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ClusterListHelper
	{
		// Each proxy in these lists represents a scripture section or paragraph with a
		//  BCV reference range.
		/// <summary>The given list of OverlapInfo proxies for the Current.</summary>
		private List<OverlapInfo> m_proxyListCurr;
		/// <summary>The given list of OverlapInfo proxies for the Revision.</summary>
		private List<OverlapInfo> m_proxyListRev;

		/// <summary>Database cache</summary>
		private FdoCache m_cache;

		/// <summary>The list of Clusters we will return</summary>
		private List<Cluster> m_clusterList;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// private constructor
		/// </summary>
		/// <param name="cache">The database cache.</param>
		/// ------------------------------------------------------------------------------------
		private ClusterListHelper(FdoCache cache)
		{
			m_cache = cache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This static method steps through all the sections of the given Current and Revision
		/// books and determines which clusters of sections have overlapping verse ref ranges.
		/// </summary>
		/// <param name="bookCurr">the given current book</param>
		/// <param name="bookRev">the given book revision</param>
		/// <param name="cache">The database cache.</param>
		/// <returns>
		/// list of Cluster objects for the sections of the books
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static List<Cluster> DetermineSectionOverlapClusters(IScrBook bookCurr,
			IScrBook bookRev, FdoCache cache)
		{
			return DetermineSectionOverlapClusters(bookCurr, bookRev, cache, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This static method steps through all the sections of the given Current and Revision
		/// books and determines which clusters of sections have overlapping verse ref ranges.
		/// </summary>
		/// <param name="bookCurr">the given current book</param>
		/// <param name="bookRev">the given book revision</param>
		/// <param name="cache">The database cache.</param>
		/// <param name="progressDlg">The progress dialog box.</param>
		/// <returns>
		/// list of Cluster objects for the sections of the books
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static List<Cluster> DetermineSectionOverlapClusters(IScrBook bookCurr,
			IScrBook bookRev, FdoCache cache, IProgress progressDlg)
		{
			int nSectionsCurr = bookCurr.SectionsOS.Count;
			int nSectionsRev = bookRev.SectionsOS.Count;

			// build the current and rev proxy lists, of OverlapInfo objects
			List<OverlapInfo> sectionProxyListCurr = new List<OverlapInfo>(nSectionsCurr);
			List<OverlapInfo> sectionProxyListRev = new List<OverlapInfo>(nSectionsRev);
			foreach (IScrSection section in bookRev.SectionsOS)
			{
				//REVIEW: should we skip intro sections in these lists??
				sectionProxyListRev.Add(new OverlapInfo(section, true));
				Debug.Assert(BCVRef.GetChapterFromBcv(section.VerseRefMin) > 0); // catch test that forgot to set section refs
				if (progressDlg != null)
					progressDlg.Step(1);
			}
			foreach (IScrSection section in bookCurr.SectionsOS)
			{
				sectionProxyListCurr.Add(new OverlapInfo(section, false));
				Debug.Assert(BCVRef.GetChapterFromBcv(section.VerseRefMin) > 0); // catch test that forgot to set section refs
				if (progressDlg != null)
					progressDlg.Step(1);
			}

			// Now build the list of section overlap clusters
			ClusterListHelper clh = new ClusterListHelper(cache);
			clh.DetermineOverlapClusters(sectionProxyListCurr, sectionProxyListRev);
			return clh.m_clusterList;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This static method steps through all the ScrVerses of the given Current and Revision
		/// lists and determines which clusters of ScrVerses have overlapping verse ref ranges.
		/// </summary>
		/// <param name="scrVersesCurr">list of ScrVerses for the Current</param>
		/// <param name="scrVersesRev">list of ScrVerses for the Revision</param>
		/// <param name="cache">The database cache.</param>
		/// <returns>
		/// list of Cluster objects for the ScrVerses
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static List<Cluster> DetermineScrVerseOverlapClusters(List<ScrVerse> scrVersesCurr,
			List<ScrVerse> scrVersesRev, FdoCache cache)
		{
			int nScrVersesCurr = scrVersesCurr.Count;
			int nScrVersesRev = scrVersesRev.Count;

			// build the current and rev proxy lists, of OverlapInfo objects
			List<OverlapInfo> scrVerseProxyListCurr = new List<OverlapInfo>(nScrVersesCurr);
			List<OverlapInfo> scrVerseProxyListRev = new List<OverlapInfo>(nScrVersesRev);
			for (int iVerseRev = 0; iVerseRev < scrVersesRev.Count; iVerseRev++)
			{
				scrVerseProxyListRev.Add(new OverlapInfo(scrVersesRev[iVerseRev], true, iVerseRev));
				// When there is no content at the start of Scripture, it won't have a reference greater than 0.
				//Debug.Assert(BCVRef.GetChapterFromBcv(scrVersesRev[iVerseRev].StartRef) > 0); // catch test that forgot to set refs
			}
			for (int iVerseCurr = 0; iVerseCurr < scrVersesCurr.Count; iVerseCurr++)
			{
				scrVerseProxyListCurr.Add(new OverlapInfo(scrVersesCurr[iVerseCurr], false, iVerseCurr));
				// When there is no content at the start of Scripture, it won't have a reference greater than 0.
				//Debug.Assert(BCVRef.GetChapterFromBcv(scrVersesCurr[iVerseCurr].StartRef) > 0); // catch test that forgot to set refs
			}

			// Now build the list of section overlap clusters
			ClusterListHelper clh = new ClusterListHelper(cache);
			clh.DetermineAdjacentOverlapClusters(scrVerseProxyListCurr, scrVerseProxyListRev);

			// Simplify the complex clusters that have some correlated pairs
			// If the ScrVerse has the same number of words, but a couple are different, each
			// different word will count two times in the current alogrithm. However, if the words
			// are simply deleted or added, these words will only be counted once.
			clh.SimplifyComplexScrVerseClusters(scrVersesCurr, scrVersesRev, 0.75);

			// Simplifying clusters may have created new clusters with leading or trailing empty paras.
			// If so, pull these empty leading and/or trailing paragraphs into other clusters.
			clh.SimplifyLeadingTrailingEmptyParas(scrVersesCurr);

			// simplifying may create/delete clusters so we need to sort them
			clh.GenerateSortKeys(scrVersesCurr);
			clh.m_clusterList.Sort();

			return clh.m_clusterList;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Generates the sort keys for the list of clusters.
		/// Note: All one-sided clusters must already have a valid indexToInsertAtInOther.
		/// </summary>
		/// <param name="scrVersesCurr">The ScrVerses curr.</param>
		/// ------------------------------------------------------------------------------------
		private void GenerateSortKeys(List<ScrVerse> scrVersesCurr)
		{
			foreach (Cluster cluster in m_clusterList)
			{
				cluster.GenerateSortKey();
			}
		}

		#region Simplify complex ScrVerse clusters
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simplifies the complex ScrVerse clusters that have some correlated pairs.
		/// </summary>
		/// <param name="scrVersesCurr">The list of Current ScrVerses.</param>
		/// <param name="scrVersesRev">The list of Revision ScrVerses.</param>
		/// <param name="correlationThreshold">The correlation threshold.</param>
		/// ------------------------------------------------------------------------------------
		private void SimplifyComplexScrVerseClusters(List<ScrVerse> scrVersesCurr,
			List<ScrVerse> scrVersesRev, double correlationThreshold)
		{
			// We need a copy of the original list to iterate through, because
			// the master list will likely need to have items added and removed
			Cluster[] clusterListCopy = new Cluster[m_clusterList.Count];
			m_clusterList.CopyTo(clusterListCopy);

			for (int iCluster = 0; iCluster < clusterListCopy.Length; iCluster++)
			{
				Cluster cluster = clusterListCopy[iCluster];
				if ((cluster.clusterType == ClusterType.MultipleInBoth ||
					cluster.clusterType == ClusterType.SplitInCurrent ||
					cluster.clusterType == ClusterType.MergedInCurrent) &&
					// we don't simplify a complex cluster caused by a network of verse bridge overlaps
					!cluster.ContainsVerseBridgeDifference && cluster.SpansParaBreak)
				{
					// In the master list, if possible, extract simpler clusters
					ExtractCorrelatedPairsFromScrVerseCluster(m_clusterList[iCluster], scrVersesCurr, scrVersesRev,
						correlationThreshold);
				}
			}

			CleanUpClusterListForRemovedItems();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Extracts correlated pairs of ScrVerses from the given complex ScrVerse cluster.
		/// </summary>
		/// <param name="cluster">The given ScrVerse complex cluster in the master list. We may
		/// reduce the overlapping items in this cluster, but we must not remove it while we
		/// iterate through the clusters, lest we mangle our indexing.</param>
		/// <param name="scrVersesCurr">The list of Current ScrVerses.</param>
		/// <param name="scrVersesRev">The list of Revision ScrVerses.</param>
		/// <param name="correlationThreshold">The correlation threshold.</param>
		/// ------------------------------------------------------------------------------------
		private void ExtractCorrelatedPairsFromScrVerseCluster(Cluster cluster, List<ScrVerse> scrVersesCurr,
			List<ScrVerse> scrVersesRev, double correlationThreshold)
		{
			// Attempt correlation of ScrVerse pairs from the start and end of the cluster.
			// If greater than our correlation threshold, add a matched items cluster and remove
			// those ScrVerses from our complex cluster.

			// Forward Scan
			// start comparing at the beginning of cluster until we find Current and Revision strings below threshold.
			double correlationFactor;
			int iCorrelatedFwd = -1; // the last index correlated on the forward scan
			ScrVerse verseCurr, verseRev;
			for (int iClstrItem = 0; iClstrItem < cluster.itemsCurr.Count; iClstrItem++)
			{
				if (iClstrItem >= cluster.itemsRev.Count)
					break; //no more Rev items to compare to

				verseCurr = scrVersesCurr[cluster.itemsCurr[iClstrItem].indexInOwner];
				verseRev = scrVersesRev[cluster.itemsRev[iClstrItem].indexInOwner];

				// The references must match before we simplify clusters
				if (verseCurr.StartRef == verseRev.StartRef && verseCurr.EndRef == verseRev.EndRef)
				{
					correlationFactor = ParagraphCorrelation.DetermineStringCorrelation(
						(verseCurr.Text != null) ? verseCurr.Text.Text : null,
						(verseRev.Text != null) ? verseRev.Text.Text : null,
						m_cache.ServiceLocator.UnicodeCharProps);
					if (correlationFactor >= correlationThreshold)
					{
						// There is enough correlation to create a more-simple cluster here.
						ExtractMatchedItemsCluster(cluster, iClstrItem, iClstrItem, true);
						iCorrelatedFwd = iClstrItem;
					}
					else
					{
						// this correlation attempt failed,
						// so we are finished with the forward scan looking for correlated strings
						break;
					}
				}
				else
					break; // this correlation attempt failed because references don't match
			}

			if (iCorrelatedFwd == cluster.itemsCurr.Count - 1 && iCorrelatedFwd == cluster.itemsRev.Count - 1)
			{
				// entire cluster was correlated on the forward scan; there is no remaining blob.
				return;
			}

			// Backward Scan
			// Begin comparing at the end of the cluster until we find Current and Revision strings below threshold.
			//    at the verse number (that may cause an extra or missing verse number during revert)
			int cCorrelatedBkwrd = 0;
			for (int iClstrItemCurr = cluster.itemsCurr.Count - 1, iClstrItemRev = cluster.itemsRev.Count - 1;
				iClstrItemCurr >= 0 && iClstrItemRev >= 0;
				iClstrItemCurr--, iClstrItemRev--)
			{
				if (iClstrItemCurr <= iCorrelatedFwd)
					break; //we've reached the last Curr ScrVerse that was correlated on the forward scan
				if (iClstrItemRev <= iCorrelatedFwd)
					break; //we've reached the last Rev ScrVerse that was correlated on the forward scan

				verseCurr = scrVersesCurr[cluster.itemsCurr[iClstrItemCurr].indexInOwner];
				verseRev = scrVersesRev[cluster.itemsRev[iClstrItemRev].indexInOwner];

				if ((iClstrItemCurr == 0 || iClstrItemRev == 0) &&
					verseCurr.HasVerseNumberRun != verseRev.HasVerseNumberRun)
				{
					// We will not process a pair at iCurr==0 nor iRev ==0 when one starts with a verse number
					// and the other does not, to avoid messy comparisons
					break;
				}

				// The references must match before we simplify clusters
				if (verseCurr.StartRef == verseRev.StartRef && verseCurr.EndRef == verseRev.EndRef)
				{
					correlationFactor = ParagraphCorrelation.DetermineStringCorrelation(verseCurr.Text.Text,
						verseRev.Text.Text, m_cache.ServiceLocator.UnicodeCharProps);

					if (correlationFactor >= correlationThreshold)
					{
						// There is enough correlation to create a more-simple cluster here.
						ExtractMatchedItemsCluster(cluster, iClstrItemCurr, iClstrItemRev, false);
						cCorrelatedBkwrd++;
					}
					else
						break; // finished backward scan looking for correlated strings
				}
				else
					break; // finished backward scan because references don't match
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Extracts the matched items from the given cluster.
		/// </summary>
		/// <param name="cluster">The given cluster.</param>
		/// <param name="iItemCurr">The index of the Current item in the cluster.</param>
		/// <param name="iItemRev">The index of the Revision item in the cluster.</param>
		/// <param name="fFwd"><c>true</c> if this is the forward scan; false if backward scan.</param>
		/// ------------------------------------------------------------------------------------
		private void ExtractMatchedItemsCluster(Cluster cluster, int iItemCurr, int iItemRev,
			bool fFwd)
		{
			// Make a new cluster for the pair of matched items
			Cluster newCluster = new Cluster();
			newCluster.clusterType = ClusterType.MatchedItems;
			newCluster.verseRefMin = cluster.verseRefMin;
			newCluster.verseRefMax = cluster.verseRefMax;
			newCluster.itemsCurr.Add(cluster.itemsCurr[iItemCurr]); //use reference, not clone; the reference in original cluster will soon be deleted
			newCluster.itemsRev.Add(cluster.itemsRev[iItemRev]);

			m_clusterList.Add(newCluster);

			// If we are about to null out the last item on one side of the original cluster
			// (thus leaving orphans on the other side), we must set the indexToInsertAtInOther
			int newIndexToInsertAtInOther;
			if (ExtractingTheLastItemOnOneSide(cluster, iItemCurr, iItemRev, fFwd, out newIndexToInsertAtInOther))
				cluster.indexToInsertAtInOther = newIndexToInsertAtInOther;

			// Mark the items in the original complex cluster for later deletion
			cluster.itemsCurr[iItemCurr] = null;
			cluster.itemsRev[iItemRev] = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if we are extracting the last item on one side of the given cluster
		/// (thus leaving orphans on the other side).
		///  If so, calculate the necessary indexToInsertAtInOther for the cluster.
		/// </summary>
		/// <param name="cluster">The given cluster.</param>
		/// <param name="iItemCurr">The index of the Current item in the cluster.</param>
		/// <param name="iItemRev">The index of the Revision item in the cluster.</param>
		/// <param name="fFwd"><c>true</c> if this is the forward scan; false if backward scan.</param>
		/// <param name="newIndexToInsertAtInOther">Out: The new index to insert at in other.</param>
		/// <returns>true if we are indeed extracting the last item on one side, and calculating the
		/// new index to insert at in other</returns>
		/// ------------------------------------------------------------------------------------
		private bool ExtractingTheLastItemOnOneSide(Cluster cluster, int iItemCurr, int iItemRev, bool fFwd,
			out int newIndexToInsertAtInOther)
		{
			newIndexToInsertAtInOther = -1;

			// if we have the same number of items on both sides, it's impossible to leave orphans on one side
			if (cluster.itemsCurr.Count == cluster.itemsRev.Count)
				return false;

			if (fFwd)
			{
				// on the forward scan
				// If we're at the last item on either side of the cluster,
				//  the insert index just beyond my index.
				if (iItemCurr == cluster.itemsCurr.Count - 1)
					newIndexToInsertAtInOther = cluster.itemsCurr[iItemCurr].indexInOwner + 1;
				else if (iItemRev == cluster.itemsRev.Count - 1)
					newIndexToInsertAtInOther = cluster.itemsRev[iItemRev].indexInOwner + 1;
			}
			else
			{
				// we're on the backward scan
				Debug.Assert(iItemCurr >= 0 && iItemRev >= 0);
				// If the item above me is null or I'm the first value, then I'm the last
				//  non-null item on this side, and the insert index is at my index.
				if (iItemCurr == 0 || cluster.itemsCurr[iItemCurr - 1] == null)
					newIndexToInsertAtInOther = cluster.itemsCurr[iItemCurr].indexInOwner;
				else if (iItemRev == 0 || cluster.itemsRev[iItemRev - 1] == null)
					newIndexToInsertAtInOther = cluster.itemsRev[iItemRev].indexInOwner;
			}

			// return true if we found the critter
			return (newIndexToInsertAtInOther > -1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleans the up cluster list by removing cluster items and clusters which were
		/// marked for deletion. Also re-evaluates the type of reduced-size clusters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CleanUpClusterListForRemovedItems()
		{
			// We must iterate in reverse order to safely delete as we go
			for (int iClstr = m_clusterList.Count - 1; iClstr >= 0; iClstr--)
			{
				Cluster cluster = m_clusterList[iClstr];
				bool fRemovedItems = false;
				// remove Current items marked for deletion
				for (int iItemCurr = cluster.itemsCurr.Count - 1; iItemCurr >= 0; iItemCurr--)
				{
					if (cluster.itemsCurr[iItemCurr] == null)
					{
						cluster.itemsCurr.RemoveAt(iItemCurr);
						fRemovedItems = true;
					}
				}

				// remove Revision items marked for deletion
				for (int iItemRev = cluster.itemsRev.Count - 1; iItemRev >= 0; iItemRev--)
				{
					if (cluster.itemsRev[iItemRev] == null)
					{
						cluster.itemsRev.RemoveAt(iItemRev);
						fRemovedItems = true;
					}
				}

				// if no items were removed, we're done with this cluster
				if (!fRemovedItems)
					continue;

				// if this cluster has no items left, remove it entirely
				if (cluster.itemsCurr.Count == 0 && cluster.itemsRev.Count == 0)
					m_clusterList.RemoveAt(iClstr);

				else
				{
					// re-evaluate ClusterType of this reduced-size cluster
					// we here may utilize the special case 'Orphan' cluster types
					if (cluster.itemsRev.Count == 0 && cluster.itemsCurr.Count > 0 && !cluster.itemsCurr[0].isStanzaBreak)
					{
						cluster.clusterType = ClusterType.OrphansInCurrent;
						if (cluster.indexToInsertAtInOther < 0)
							cluster.indexToInsertAtInOther = FindIndexToInsertAtInOther(cluster);
					}
					else if (cluster.itemsRev.Count > 0 && !cluster.itemsRev[0].isStanzaBreak && cluster.itemsCurr.Count == 0)
					{
						cluster.clusterType = ClusterType.OrphansInRevision;
						if (cluster.indexToInsertAtInOther < 0)
							cluster.indexToInsertAtInOther = FindIndexToInsertAtInOther(cluster);
					}
					else
						cluster.DetermineType();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the index to insert at in the other side (i.e. current or revision).
		/// </summary>
		/// <param name="cluster">The cluster that needs to know where it should be inserted.</param>
		/// <returns>index where this cluster should be inserted in the </returns>
		/// ------------------------------------------------------------------------------------
		private int FindIndexToInsertAtInOther(Cluster cluster)
		{
			Debug.Assert((cluster.itemsCurr.Count == 0 && cluster.itemsRev.Count > 0) ||
				(cluster.itemsRev.Count == 0 && cluster.itemsCurr.Count > 0),
				"This should be a one-sided cluster");
			// Set flag indicating which side (current or revision) has items.
			bool fCurrentHasItems = cluster.itemsCurr.Count > 0;

			foreach (Cluster clstr in m_clusterList)
			{
				int numItemsOtherSide = clstr.Items(!fCurrentHasItems);
				if (numItemsOtherSide > 0)
				{
					// Since the other side in this cluster has items, then it is a candidate.
					OverlapInfo lastItemOtherSide = clstr.Item(numItemsOtherSide - 1, !fCurrentHasItems);
					if (cluster.verseRefMin >= lastItemOtherSide.verseRefMin)
					{
						// Since we are beyond the reference on the other side, set the index after this cluster
						// on the other side.
						return lastItemOtherSide.indexInOwner + 1;
					}
				}
			}

			return 0;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the central non-static method that creates a list of overlap Clusters for
		/// the given proxies. Each proxy represents a scripture section or paragraph or ScrVerse
		/// with a BCV reference range.
		/// </summary>
		/// <param name="proxyListCurr">The given list of OverlapInfo proxies for the Current.</param>
		/// <param name="proxyListRev">The given list of OverlapInfo proxies for the Revision.</param>
		/// ------------------------------------------------------------------------------------
		private void DetermineOverlapClusters(List<OverlapInfo> proxyListCurr, List<OverlapInfo> proxyListRev)
		{
			m_proxyListCurr = proxyListCurr;
			m_proxyListRev = proxyListRev;

			// process the proxy lists to produce our desired list of section clusters
			CreateBasicOverlapClusters();
			DetermineOverlapClusterTypes();
			DetermineInsertIndices();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is another central non-static method that creates a list of overlap Clusters for
		/// the given proxies. In this version each cluster includes only adjacent items from the
		/// source lists.
		/// Each proxy represents a scripture section or paragraph or ScrVerse with a
		/// BCV reference range.
		/// </summary>
		/// <param name="proxyListCurr">The given list of OverlapInfo proxies for the Current.</param>
		/// <param name="proxyListRev">The given list of OverlapInfo proxies for the Revision.</param>
		/// ------------------------------------------------------------------------------------
		private void DetermineAdjacentOverlapClusters(List<OverlapInfo> proxyListCurr, List<OverlapInfo> proxyListRev)
		{
			m_proxyListCurr = proxyListCurr;
			m_proxyListRev = proxyListRev;

			// process the proxy lists to produce our desired list of section clusters
			CreateBasicAdjacentOverlapClusters();
			DetermineOverlapClusterTypes();
			DetermineInsertIndices();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simplifies the clusters with leading and/or trailing empty paras.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SimplifyLeadingTrailingEmptyParas(List<ScrVerse> scrVersesCurr)
		{
			// We need a copy of the original list to iterate through, because
			// the master list will likely need to have items added and removed
			Cluster[] clusterListCopy = new Cluster[m_clusterList.Count];
			m_clusterList.CopyTo(clusterListCopy);

			for (int iCluster = 0; iCluster < clusterListCopy.Length; iCluster++)
			{
				Cluster cluster = clusterListCopy[iCluster];
				if (cluster.clusterType == ClusterType.MultipleInBoth ||
					 cluster.clusterType == ClusterType.SplitInCurrent ||
					 cluster.clusterType == ClusterType.MergedInCurrent)
				{
					// In the master list, if possible, extract simpler clusters
					ExtractStanzaBreaksFromScrVerseCluster(clusterListCopy[iCluster]);
				}
				else if (cluster.clusterType == ClusterType.AddedToCurrent ||
					cluster.clusterType == ClusterType.OrphansInCurrent ||
					cluster.clusterType == ClusterType.MissingInCurrent ||
					cluster.clusterType == ClusterType.OrphansInRevision)
				{
					bool fIsCurrent = cluster.clusterType == ClusterType.AddedToCurrent;
					if (cluster.ItemList(fIsCurrent).Count > 1)
					{
						// Simplify added/missing clusters that have more than one item in the cluster.
						// We simplify them because items in them to separate empty paragraphs.
						ExtractMissingAddedEmptyParasFromScrVerseCluster(clusterListCopy[iCluster]);
					}
				}
				else if (cluster.clusterType == ClusterType.MatchedItems)
				{
					// If a match was made with a stanza break and an non-stanza break para, then
					// we need to break the cluster apart.
					ExtractMismatchedParas(clusterListCopy[iCluster]);
				}
			}

			CleanUpClusterListForRemovedItems();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Extracts the empty leading or trailing paragraphs from a ScrVerse cluster.
		/// </summary>
		/// <param name="cluster">The cluster.</param>
		/// ------------------------------------------------------------------------------------
		private void ExtractStanzaBreaksFromScrVerseCluster(Cluster cluster)
		{
			// Forward Scan
			// Start comparing at the beginning of cluster until we no longer find empty paragraphs
			// in both the current AND revision.
			int iMatchingEmptyParaFwd = -1; // the last index of matching empty paragraphs on the forward scan
			for (int iClstrItem = 0; iClstrItem < cluster.itemsCurr.Count; iClstrItem++)
			{
				if (iClstrItem >= cluster.itemsRev.Count)
					break; //no more Rev items to compare to

				if (cluster.itemsCurr[iClstrItem].isStanzaBreak && cluster.itemsRev[iClstrItem].isStanzaBreak)
				{
					ExtractMatchedItemsCluster(cluster, iClstrItem, iClstrItem, true);
					iMatchingEmptyParaFwd = iClstrItem;
				}
				else
					break; // finished with forward scan (found content paras)
			}

			// Need to determine if either side has non-matching stanza breaks before content paras.
			if (cluster.IsStanzaBreak(iMatchingEmptyParaFwd + 1, true))
			{
				// Added empty paragraphs before current side. Move into added cluster(s)
				int endIndex = cluster.Items(true);
				ExtractMissingAddedItems(cluster, iMatchingEmptyParaFwd + 1, endIndex, true, true);
			}
			else if (cluster.IsStanzaBreak(iMatchingEmptyParaFwd + 1, false))
			{
				// Added empty paragraphs before revision side. Move into missing cluster(s)
				int endIndex = cluster.Items(false);
				ExtractMissingAddedItems(cluster, iMatchingEmptyParaFwd + 1, endIndex, false, true);
			}

			// Backward Scan
			// Start comparing at the end of cluster until we no longer find empty paragraphs
			// in both the current AND revision.
			int cCorrelatedBkwrd = 0;
			for (int iClstrItemCurr = cluster.itemsCurr.Count - 1, iClstrItemRev = cluster.itemsRev.Count - 1;
				 iClstrItemCurr > 0 && iClstrItemRev > 0;
				 iClstrItemCurr--, iClstrItemRev--)
			{
				if (iClstrItemCurr <= iMatchingEmptyParaFwd)
					break; //we've reached the last Curr ScrVerse that was matched on the forward scan
				if (iClstrItemRev <= iMatchingEmptyParaFwd)
					break; //we've reached the last Rev ScrVerse that was correlated on the forward scan

				if (cluster.itemsCurr[iClstrItemCurr].isStanzaBreak && cluster.itemsRev[iClstrItemRev].isStanzaBreak)
				{
					ExtractMatchedItemsCluster(cluster, iClstrItemCurr, iClstrItemRev, false);
					cCorrelatedBkwrd++;
				}
			}

			// Need to determine if either side has non-matching empty paragraphs after content paras.
			int iStartScanCurr = cluster.Items(true) - cCorrelatedBkwrd - 1;
			int iStartScanRev = cluster.Items(false) - cCorrelatedBkwrd - 1;
			if (cluster.IsStanzaBreak(iStartScanCurr, true))
			{
				// Added non-matching stanza breaks after current side. Move into new added cluster(s)
				ExtractMissingAddedItems(cluster, iStartScanCurr, iMatchingEmptyParaFwd + 1, true, false);
			}
			else if (cluster.IsStanzaBreak(iStartScanRev, false))
			{
				// Added non-matching stanza breaks after revision side. Move into new missing cluster(s)
				ExtractMissingAddedItems(cluster, iStartScanRev, iMatchingEmptyParaFwd + 1, false, false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Extracts the missing or added empty paras from a ScrVerse cluster. This method does
		/// not attempt to find matching empty paragraphs.
		/// </summary>
		/// <param name="cluster">The cluster which should be an AddedToCurrent or
		/// MissingInCurrent cluster.</param>
		/// ------------------------------------------------------------------------------------
		private void ExtractMissingAddedEmptyParasFromScrVerseCluster(Cluster cluster)
		{
			Debug.Assert(cluster.clusterType == ClusterType.AddedToCurrent ||
				cluster.clusterType == ClusterType.OrphansInCurrent ||
				cluster.clusterType == ClusterType.MissingInCurrent ||
				cluster.clusterType == ClusterType.OrphansInRevision);

			bool fIsCurrent = Cluster.CurrentIsSource(cluster.clusterType);

			// Extract missing/added items from beginning of cluster.
			int removedFromStart =
				ExtractMissingAddedItems(cluster, 0, cluster.Items(fIsCurrent), fIsCurrent, true);

			// Extract missing/added items from end of cluster.
			ExtractMissingAddedItems(cluster, cluster.Items(fIsCurrent) - 1, removedFromStart,
				fIsCurrent, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Extracts paras that are mismatched in terms of being stanza breaks.
		/// </summary>
		/// <param name="cluster">The cluster.</param>
		/// ------------------------------------------------------------------------------------
		private void ExtractMismatchedParas(Cluster cluster)
		{
			Debug.Assert(cluster.clusterType == ClusterType.MatchedItems);
			if (cluster.itemsCurr[0].isStanzaBreak != cluster.itemsRev[0].isStanzaBreak)
			{
				// Create new cluster from current side.
				int newIndexToInsertAtInOther = cluster.itemsCurr[0].indexInOwner;
				AddMissingAddedCluster(newIndexToInsertAtInOther, 0, cluster, true);

				// Create new cluster from revision side.
				newIndexToInsertAtInOther = cluster.itemsRev[0].indexInOwner;
				AddMissingAddedCluster(newIndexToInsertAtInOther, 0, cluster, false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Extracts the missing or added items from a cluster.
		/// </summary>
		/// <param name="cluster">The cluster.</param>
		/// <param name="iFrom">Beginning limit for extracting items.</param>
		/// <param name="iTo">Ending limit for extracting items.</param>
		/// <param name="fIsCurrent">if set to <c>true</c> extract from the current side;
		/// <c>false</c> extract from the revision side.</param>
		/// <param name="fFwd">if <c>true</c> scanning forward; otherwise scan backward</param>
		/// <returns>number of extracted added or missing empty ScrVerses</returns>
		/// ------------------------------------------------------------------------------------
		private int ExtractMissingAddedItems(Cluster cluster, int iFrom, int iTo, bool fIsCurrent,
			bool fFwd)
		{
			// Determine index to insert in other.
			int newIndexToInsertAtInOther;
			if (cluster.indexToInsertAtInOther != -1)
				newIndexToInsertAtInOther = cluster.indexToInsertAtInOther;
			else
				newIndexToInsertAtInOther = cluster.ItemList(fIsCurrent)[iFrom].indexInOwner;

			Debug.Assert(newIndexToInsertAtInOther != -1);

			int extracted = 0;
			if (fFwd)
			{
				// Scan forward from end of matching empty paras at beginning of cluster
				for (int iItem = iFrom; iItem < iTo; iItem++)
				{
					if (!cluster.Item(iItem, fIsCurrent).isStanzaBreak)
						break;
					AddMissingAddedCluster(newIndexToInsertAtInOther, iItem, cluster, fIsCurrent);
					extracted++;
				}
			}
			else
			{
				// Scan backward from end of matching empty paras at cluster end
				for (int iItem = iFrom; iItem > iTo; iItem--)
				{
					if (!cluster.Item(iItem, fIsCurrent).isStanzaBreak)
						break;
					// Since we are handling added/missing items at the end of the cluster,
					// we want to set the insertion point at the end of the other side.
					AddMissingAddedCluster(newIndexToInsertAtInOther, iItem, cluster, fIsCurrent);
					extracted++;
				}
			}

			return extracted;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a new cluster for missing/added.
		/// </summary>
		/// <param name="insertInOtherIndex">Index where added/missing cluster should be inserted in other.</param>
		/// <param name="iItem">The index of the item.</param>
		/// <param name="cluster">The cluster.</param>
		/// <param name="fIsCurrent">if set to <c>true</c> extract from the current side;
		/// <c>false</c> extract from the revision side.</param>
		/// ------------------------------------------------------------------------------------
		private void AddMissingAddedCluster(int insertInOtherIndex, int iItem, Cluster cluster, bool fIsCurrent)
		{
			Cluster newCluster = new Cluster();
			newCluster.clusterType = (fIsCurrent) ? ClusterType.AddedToCurrent : ClusterType.MissingInCurrent;
			newCluster.verseRefMin = cluster.verseRefMin;
			newCluster.verseRefMax = cluster.verseRefMax;
			newCluster.indexToInsertAtInOther = insertInOtherIndex;
			newCluster.ItemList(fIsCurrent).Add(cluster.Item(iItem, fIsCurrent));
			m_clusterList.Add(newCluster);

			// Mark the item in the original complex cluster for later deletion
			if (fIsCurrent)
				cluster.itemsCurr[iItem] = null;
			else
				cluster.itemsRev[iItem] = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the overlapped pairs of OverlapInfo proxies, and updates the proxies with that
		/// information.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FindOverlappedPairs()
		{
			// Create each proxy's relationship list
			foreach (OverlapInfo oi in m_proxyListCurr)
				oi.overlappedItemsInOther = new List<OverlapInfo>(4); //initial capacity of 4 seems reasonable
			foreach (OverlapInfo oi in m_proxyListRev)
				oi.overlappedItemsInOther = new List<OverlapInfo>(4); //initial capacity of 4 seems reasonable

			// Compare each pair of proxy elements across the Curr and Rev lists,
			// and fill in each element's overlappedItems list with those proxies
			// in the other list that have a verse ref overlap.
			foreach (OverlapInfo oiCurr in m_proxyListCurr)
			{
				foreach (OverlapInfo oiRev in m_proxyListRev)
				{
					if (IsRefOverlap(oiCurr, oiRev))
					{
						oiCurr.overlappedItemsInOther.Add(oiRev);
						oiRev.overlappedItemsInOther.Add(oiCurr);
					}
				}
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create basic overlap clusters from the OverlapInfo proxies for the Current and
		/// Revision.  Note that FindOverlappedPairs() must be called first.  The clusters
		/// will still need their types and insertIndices determined.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreateBasicOverlapClusters()
		{
			// Find all overlapped pairs
			FindOverlappedPairs();

			// output list that will ultimatly contain all of the clusters we find
			List<Cluster> clusterList = new List<Cluster>();

			// Create destructible copies of each master list, allowing already
			// grouped items to be removed, avoiding possible infinite loops
			// created when the lists are out-of-order
			List<OverlapInfo> proxyListRevCopy = new List<OverlapInfo>(m_proxyListRev.ToArray());
			List<OverlapInfo> proxyListCurrCopy = new List<OverlapInfo>(m_proxyListCurr.ToArray());

			// So long as there are remaining proxies, keep searching for new
			// clusters
			while (proxyListRevCopy.Count > 0 || proxyListCurrCopy.Count > 0)
			{
				// The proxy to start the new cluster search from
				OverlapInfo firstProxy;

				// If both lists have remaining proxies, choose the lowest
				// of the two
				if (proxyListRevCopy.Count > 0 && proxyListCurrCopy.Count > 0)
				{
					//get the next one with the first start reference
					// (note: if refs are equal, doesn't matter which one)
					OverlapInfo oiRev = proxyListRevCopy[0];
					OverlapInfo oiCurr = proxyListCurrCopy[0];
					if (oiRev.verseRefMin < oiCurr.verseRefMin)
						firstProxy = oiRev;
					else
						firstProxy = oiCurr;
				}
				// Otherwise, use whatever remains
				else if (proxyListRevCopy.Count > 0)
					firstProxy = proxyListRevCopy[0];
				else
					firstProxy = proxyListCurrCopy[0];

				// the queue of proxies that will be used form a cluster
				Queue<OverlapInfo> queue = new Queue<OverlapInfo>();

				// list of those proxies that have been added to the queue
				// so that they will not be added again, causing errors
				List<OverlapInfo> visited = new List<OverlapInfo>();

				// list for accumulating proxies (both Curr and Rev) that we
				// find for a cluster
				List<OverlapInfo> proxyListForCluster = new List<OverlapInfo>();

				// Prime the queue by putting the first starting point
				// into it and marking that it's been visited
				queue.Enqueue(firstProxy);
				visited.Add(firstProxy);

				// Now that the resources are set up, begin the cluster
				// search.  The queue is used to make a breadth-first
				// search of the tree of relationships that exists
				// between overlapping proxies, with the visited
				// list making sure that no proxy that has already
				// been included in the cluster will be included again
				// (avoiding a potentially infinite cluster search)
				while (queue.Count > 0)
				{
					// Remove the next item in the queue
					OverlapInfo currentProxy = queue.Dequeue();

					// Remove the current item from it's corresponding list, now that it
					// has been used
					if (currentProxy.bookIsFromRev)
					{
						proxyListRevCopy.Remove(currentProxy);
					}
					else
					{
						proxyListCurrCopy.Remove(currentProxy);
					}

					// Push the current overlap proxy's children onto the queue,
					// so long as they haven't already been there
					foreach (OverlapInfo child in currentProxy.overlappedItemsInOther)
					{
						if (!visited.Contains(child))
						{
							// Enqueue the child and note that it's been visited
							queue.Enqueue(child);
							visited.Add(child);
						}
					}

					// Add the current proxy to the list for the cluster
					proxyListForCluster.Add(currentProxy);
				}

				// Create a cluster with the items we have accumulated
				Cluster cluster = new Cluster();
				foreach (OverlapInfo oi in proxyListForCluster)
				{
					// Update the verse ref range for the cluster
					if (cluster.verseRefMin == 0)
					{
						cluster.verseRefMin = oi.verseRefMin;
						cluster.verseRefMax = oi.verseRefMax;
					}
					else
					{
						cluster.verseRefMin = Math.Min(cluster.verseRefMin, oi.verseRefMin);
						cluster.verseRefMax = Math.Max(cluster.verseRefMax, oi.verseRefMax);
					}

					// Add the item to the cluster lists, according to its book type
					if (oi.bookIsFromRev)
						cluster.itemsRev.Add(oi);
					else
						cluster.itemsCurr.Add(oi);
				}
				// and save this new cluster in our output list of clusters
				clusterList.Add(cluster);
			}

			// save our cluster list
			m_clusterList = clusterList;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create basic overlap clusters from the OverlapInfo proxies for the Current and
		/// Revision.
		/// In this case we include only proxies that are adjacent to one another in the
		/// owner's sequence in the Current or Revision (e.g. adjacent ScrVerses in the same section).
		/// The clusters will still need their types and insert Indices determined.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateBasicAdjacentOverlapClusters()
		{
			// output list that will ultimatly contain all of the clusters we find
			List<Cluster> clusterList = new List<Cluster>();

			// Create destructible copies of each master list, allowing already
			// grouped items to be removed, avoiding possible infinite loops
			// created when the lists are out-of-order
			List<OverlapInfo> proxyListRevCopy = new List<OverlapInfo>(m_proxyListRev.ToArray());
			List<OverlapInfo> proxyListCurrCopy = new List<OverlapInfo>(m_proxyListCurr.ToArray());

			Cluster cluster = null; // the cluster under construction at any given time

			// So long as there are remaining proxies, keep working through them
			while (proxyListRevCopy.Count > 0 || proxyListCurrCopy.Count > 0)
			{
				// The proxy to process this pass- the first in either the Current or Rev processing list
				OverlapInfo firstProxy;

				// If both lists have remaining proxies...
				if (proxyListRevCopy.Count > 0 && proxyListCurrCopy.Count > 0)
				{
					OverlapInfo oiRev = proxyListRevCopy[0];
					OverlapInfo oiCurr = proxyListCurrCopy[0];
					// if we are starting a new cluster...
					if (cluster == null)
					{
						if ((oiRev.isStanzaBreak && !oiCurr.isStanzaBreak) ||
							(!oiRev.isStanzaBreak && oiCurr.isStanzaBreak))
						{
							// only one of the current or revision is a stanza break. Get the stanza break.
							firstProxy = oiRev.isStanzaBreak ? oiRev : oiCurr;
						}

						else
						{
							//get the next one with the earlier start reference
							// (if refs are equal, doesn't matter which one)
							if (oiRev.verseRefMin < oiCurr.verseRefMin)
								firstProxy = oiRev;
							else
								firstProxy = oiCurr;
						}
					}
					else
					{
						// See if either side has a proxy that overlaps our cluster under construction
						if (CanBeIncluded(cluster, oiRev))
							firstProxy = oiRev;
						else if (CanBeIncluded(cluster, oiCurr))
							firstProxy = oiCurr;

						else
						{
							//Neither proxy overlaps with our cluster.
							// save the cluster in progress, and prepare to start a new one
							clusterList.Add(cluster);
							cluster = null;
							continue;
						}
					}
				}
				// Otherwise, use whatever remains
				else if (proxyListRevCopy.Count > 0)
						firstProxy = proxyListRevCopy[0];
				else
						firstProxy = proxyListCurrCopy[0];

				// Now add this proxy to a cluster
				if (cluster == null)
				{
					// This is the first item for this cluster
					cluster = new Cluster();
					AddItemToCluster(cluster, firstProxy);
				}
				else if (CanBeIncluded(cluster, firstProxy))
				{
					// This proxy overlaps our cluster. Grab it.
					AddItemToCluster(cluster, firstProxy);
				}
				else
				{
					//This proxy is NOT overlapping with our cluster.
					// save the cluster in progress
					clusterList.Add(cluster);
					// start a new cluster for this proxy
					cluster = new Cluster();
					AddItemToCluster(cluster, firstProxy);
				}

				// Remove the current proxy from it's corresponding list, now that it
				// has been used
				if (firstProxy.bookIsFromRev)
					proxyListRevCopy.Remove(firstProxy);
				else
					proxyListCurrCopy.Remove(firstProxy);
			}

			// save the final cluster, if any
			if (cluster != null)
			{
				Debug.Assert(cluster.itemsCurr.Count > 0 || cluster.itemsRev.Count > 0);
				clusterList.Add(cluster);
			}

			// save our cluster list
			m_clusterList = clusterList;
		}

		//// This version does extra work so that it never creates a zero-to-many cluster
		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Create basic overlap clusters from the OverlapInfo proxies for the Current and
		///// Revision.
		///// In this case we include only proxies that are adjacent to one another in the
		///// owner's sequence in the Current or Revision.
		///// The clusters will still need their types and insert Indices determined.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//private void CreateBasicAdjacentOverlapClusters()
		//{
		//    // output list that will ultimatly contain all of the clusters we find
		//    List<Cluster> clusterList = new List<Cluster>();

		//    // Create destructible copies of each master list, allowing already
		//    // grouped items to be removed, avoiding possible infinite loops
		//    // created when the lists are out-of-order
		//    List<OverlapInfo> proxyListRevCopy = new List<OverlapInfo>(m_proxyListRev.ToArray());
		//    List<OverlapInfo> proxyListCurrCopy = new List<OverlapInfo>(m_proxyListCurr.ToArray());

		//    Cluster cluster = null; // the cluster under construction at any given time

		//    // So long as there are remaining proxies, keep working through them
		//    while (proxyListRevCopy.Count > 0 || proxyListCurrCopy.Count > 0)
		//    {
		//        // The proxy to process this pass- the first in either the Current or Rev processing list
		//        OverlapInfo firstProxy;

		//        // If both lists have remaining proxies...
		//        if (proxyListRevCopy.Count > 0 && proxyListCurrCopy.Count > 0)
		//        {
		//            OverlapInfo oiRev = proxyListRevCopy[0];
		//            OverlapInfo oiCurr = proxyListCurrCopy[0];
		//            // if we are starting a new cluster...
		//            if (cluster == null)
		//            {
		//                //get the next one with the earlier start reference
		//                // (note: if refs are equal, doesn't matter which one)
		//                if (oiRev.verseRefMin < oiCurr.verseRefMin)
		//                    firstProxy = oiRev;
		//                else
		//                    firstProxy = oiCurr;
		//            }
		//            else
		//            {
		//                // If I have only one item in the current...
		//                if (cluster.itemsCurr.Count == 1 && cluster.itemsRev.Count == 0)
		//                {
		//                    // add a rev item only.
		//                    if (IsRefOverlapAndAdjacent(cluster, oiRev))
		//                        firstProxy = oiRev;
		//                    else
		//                    {
		//                        // For now, we don't handle 0 to many clusters, so we complete
		//                        // this cluster and move on.
		//                        // save the cluster in progress, and prepare to start a new one
		//                        clusterList.Add(cluster);
		//                        cluster = null;
		//                        continue;
		//                    }
		//                }
		//                else if (cluster.itemsRev.Count == 1 && cluster.itemsCurr.Count == 0)
		//                {
		//                    // If I have only one item in the revision...
		//                    if (IsRefOverlapAndAdjacent(cluster, oiCurr))
		//                        firstProxy = oiCurr;
		//                    else
		//                    {
		//                        // For now, we don't handle 0 to many clusters, so we complete
		//                        // this cluster and move on.
		//                        // save the cluster in progress, and prepare to start a new one
		//                        clusterList.Add(cluster);
		//                        cluster = null;
		//                        continue;
		//                    }
		//                }
		//                // See if either side has a proxy that overlaps our cluster under construction
		//                else if (IsRefOverlapAndAdjacent(cluster, oiRev))
		//                {
		//                    firstProxy = oiRev;
		//                }
		//                else if (IsRefOverlapAndAdjacent(cluster, oiCurr))
		//                    firstProxy = oiCurr;

		//                else
		//                {
		//                    //Neither proxy overlaps with our cluster.
		//                    // save the cluster in progress, and prepare to start a new one
		//                    clusterList.Add(cluster);
		//                    cluster = null;
		//                    continue;
		//                }
		//            }
		//        }
		//        // Otherwise, use whatever remains
		//        else if (proxyListRevCopy.Count > 0)
		//        {
		//            // if cluster has only one item in Rev, we need to avoid making a zero-to-many cluster
		//            if (cluster != null && cluster.itemsRev.Count == 1 && cluster.itemsCurr.Count == 0)
		//            {
		//                // save the cluster in progress, and prepare to start a new one
		//                clusterList.Add(cluster);
		//                cluster = null;
		//                continue;
		//            }
		//            else
		//                firstProxy = proxyListRevCopy[0];
		//        }
		//        else
		//        {
		//            // if cluster has only one item in Curr, we need to avoid making a zero-to-many cluster
		//            if (cluster != null && cluster.itemsCurr.Count == 1 && cluster.itemsRev.Count == 0)
		//            {
		//                // save the cluster in progress, and prepare to start a new one
		//                clusterList.Add(cluster);
		//                cluster = null;
		//                continue;
		//            }
		//            else
		//                firstProxy = proxyListCurrCopy[0];
		//        }
		//        // Now add this proxy to a cluster
		//        if (cluster == null)
		//        {
		//            // This is the first item for this cluster
		//            cluster = new Cluster();
		//            AddItemToCluster(cluster, firstProxy);
		//        }
		//        else if (IsRefOverlapAndAdjacent(cluster, firstProxy))
		//        {
		//            // This proxy overlaps our cluster. Grab it.
		//            AddItemToCluster(cluster, firstProxy);
		//        }
		//        else
		//        {
		//            //This proxy is NOT overlapping with our cluster.
		//            // save the cluster in progress
		//            clusterList.Add(cluster);
		//            // start a new cluster for this proxy
		//            cluster = new Cluster();
		//            AddItemToCluster(cluster, firstProxy);
		//        }

		//        // Remove the current proxy from it's corresponding list, now that it
		//        // has been used
		//        if (firstProxy.myBook == OverlapInfo.kRevision)
		//            proxyListRevCopy.Remove(firstProxy);
		//        else
		//            proxyListCurrCopy.Remove(firstProxy);
		//    }

		//    // save the final cluster, if any
		//    if (cluster != null)
		//    {
		//        Debug.Assert(cluster.itemsCurr.Count > 0 || cluster.itemsRev.Count > 0);
		//        clusterList.Add(cluster);
		//    }

		//    // save our cluster list
		//    m_clusterList = clusterList;
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if two OverlapInfos have any reference overlap
		/// </summary>
		/// <param name="oi1">first OverlapInfo to check</param>
		/// <param name="oi2">second overlapInfo to check</param>
		/// <returns>true if there is any reference overlap</returns>
		/// ------------------------------------------------------------------------------------
		protected bool IsRefOverlap(OverlapInfo oi1, OverlapInfo oi2)
		{
			// is oi1 completely before oi2?
			if (oi1.verseRefMax < oi2.verseRefMin)
				return false;
			// is oi2 completely before oi1?
			if (oi2.verseRefMax < oi1.verseRefMin)
				return false;
			// there must be some overlap
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether a given overlap info can be included in the current cluster.
		/// To be included in the cluster, it must meet the following criteria:
		///  * have a reference overlap with the specified cluster,
		///  * be adjacent to and within the same owning sequence (e.g. same StText), and
		/// </summary>
		/// <param name="cluster">The cluster.</param>
		/// <param name="oi">The overlap info.</param>
		/// <returns><c>true</c> if the specified OverlapInfo can be included in the cluster</returns>
		/// ------------------------------------------------------------------------------------
		private bool CanBeIncluded(Cluster cluster, OverlapInfo oi)
		{
			// is oi completely before the cluster range?
			if (oi.verseRefMax < cluster.verseRefMin)
				return false;
			// is cluster range completely before oi?
			if (cluster.verseRefMax < oi.verseRefMin)
				return false;
			// there must be some overlap

			// is oi for a ScrVerse contained in the same StText?
			if (oi.bookIsFromRev)
			{
				if (cluster.itemsRev.Count > 0 &&
					oi.myParaOwner != cluster.itemsRev[cluster.itemsRev.Count - 1].myParaOwner)
				{
					return false;  // paragraph does not have the same owning StText
				}
			}
			else
			{
				if (cluster.itemsCurr.Count > 0 &&
					oi.myParaOwner != cluster.itemsCurr[cluster.itemsCurr.Count - 1].myParaOwner)
				{
					return false;  // not the next item in the owner's sequence
				}
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the item to cluster.
		/// </summary>
		/// <param name="cluster">The cluster.</param>
		/// <param name="oi">The overlap info.</param>
		/// ------------------------------------------------------------------------------------
		private void AddItemToCluster(Cluster cluster, OverlapInfo oi)
		{
			if (oi.bookIsFromRev)
				cluster.itemsRev.Add(oi);
			else
				cluster.itemsCurr.Add(oi);

			// update the cluster reference range
			if (oi.verseRefMin < cluster.verseRefMin || cluster.verseRefMin == 0)
				cluster.verseRefMin = oi.verseRefMin;
			if (oi.verseRefMax > cluster.verseRefMax)
				cluster.verseRefMax = oi.verseRefMax;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines the cluster type for each overlap Cluster in our list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DetermineOverlapClusterTypes()
		{
			foreach (Cluster cluster in m_clusterList)
				cluster.DetermineType();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines the indexToInsertAtInOther for all Missing/Added clusters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DetermineInsertIndices()
		{
			// make shadow copies of our original proxy lists, which we will use to see what
			//  items remain to be processed yet.
			List<OverlapInfo> remainingCurr = new List<OverlapInfo>(m_proxyListCurr.ToArray());
			List<OverlapInfo> remainingRev = new List<OverlapInfo>(m_proxyListRev.ToArray());

			for (int i = 0; i < m_clusterList.Count; i++)
			{
				Cluster cluster = m_clusterList[i];

				// For an AddedInCurrent cluster, set the indexToInsertAtInOther
				if (cluster.clusterType == ClusterType.AddedToCurrent /*||
					cluster.clusterType == ClusterType.OrphansInCurrent*/)
				{
					if (remainingRev.Count > 0)
					{
						// find the owner's index of the next item in the Rev that remains to be processed
						OverlapInfo oiNext = remainingRev[0];
						cluster.indexToInsertAtInOther = m_proxyListRev.IndexOf(oiNext);
					}
					else
						// all Rev items have been processed; get the index at the end of the Rev list
						cluster.indexToInsertAtInOther = m_proxyListRev.Count;
				}

				// For an MissingInCurrent cluster, set the indexToInsertAtInOther
				else if (cluster.clusterType == ClusterType.MissingInCurrent /* ||
					cluster.clusterType == ClusterType.OrphansInRevision */)
				{
					if (remainingCurr.Count > 0)
					{
						// find the owner's index of the next item in the Rev that remains to be processed
						OverlapInfo oiNext = remainingCurr[0];
						cluster.indexToInsertAtInOther = m_proxyListCurr.IndexOf(oiNext);
					}
					else
						// all Curr items have been processed; get the index at the end of the Curr list
						cluster.indexToInsertAtInOther = m_proxyListCurr.Count;
				}


				// remove this cluster's items from our lists of items remaining
				foreach (OverlapInfo oi in cluster.itemsCurr)
					remainingCurr.Remove(oi);
				foreach (OverlapInfo oi in cluster.itemsRev)
					remainingRev.Remove(oi);
			}
		}
	}
	#endregion

	#region SectionHeadCorrelationHelper
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The SectionHeadCorrelationHelper is an implementation class that provides a static
	/// method to build a list of Cluster objects representing correlations between
	/// section heads.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SectionHeadCorrelationHelper
	{
		// Each proxy in these lists represents a scripture section with a
		//  BCV reference range.
		/// <summary>The given list of OverlapInfo proxies for the Current.</summary>
		private List<OverlapInfo> m_proxyListCurr;
		/// <summary>The given list of OverlapInfo proxies for the Revision.</summary>
		private List<OverlapInfo> m_proxyListRev;

		/// <summary>The list of correlation Clusters we will return</summary>
		private List<Cluster> m_clusterList;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// private constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private SectionHeadCorrelationHelper()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This static method steps through all the items in the given overlapCluster (which
		/// has muliple reference overlaps in one or both books)
		/// and determines which section heads should correlate.
		/// </summary>
		/// <param name="overlapCluster">the given overlap cluster</param>
		/// <returns>list of Cluster objects representing correlations between section heads</returns>
		/// ------------------------------------------------------------------------------------
		public static List<Cluster> DetermineSectionHeadCorrelationClusters(Cluster overlapCluster)
		{
			// Deep-copy the lists of rev and curr items recieved from the overlapCluster
			List<OverlapInfo> sectionProxyListCurr = new List<OverlapInfo>();
			List<OverlapInfo> sectionProxyListRev = new List<OverlapInfo>();
			foreach (OverlapInfo oi in overlapCluster.itemsRev)
			{
				sectionProxyListRev.Add(oi.Clone());
			}
			foreach (OverlapInfo oi in overlapCluster.itemsCurr)
			{
				sectionProxyListCurr.Add(oi.Clone());
			}

			// Build the list of section head correlation clusters
			SectionHeadCorrelationHelper shch = new SectionHeadCorrelationHelper();
			shch.DetermineSHCorrelationClusters(sectionProxyListCurr, sectionProxyListRev);
			return shch.m_clusterList;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the central non-static method that creates a list of section head correlation
		/// Clusters for the given proxies. Each proxy represents a scripture section with a BCV
		/// reference range.
		/// </summary>
		/// <param name="proxyListCurr">The given list of OverlapInfo proxies for the Current.</param>
		/// <param name="proxyListRev">The given list of OverlapInfo proxies for the Revision.</param>
		/// ------------------------------------------------------------------------------------
		private void DetermineSHCorrelationClusters(List<OverlapInfo> proxyListCurr, List<OverlapInfo> proxyListRev)
		{
			m_proxyListCurr = proxyListCurr;
			m_proxyListRev = proxyListRev;

			// process the proxy lists to produce our desired list of section clusters
			FindCorrelatedPairs();
			DetermineCorrelatedSectionHeadClusters();
			DetermineCorrelationClusterTypes();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the overlapped pairs of OverlapInfo proxies, and updates the proxies with that
		/// information.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FindCorrelatedPairs()
		{
			// Create each proxy's relationship list
			foreach (OverlapInfo oi in m_proxyListCurr)
				oi.overlappedItemsInOther = new List<OverlapInfo>(4); //initial capacity of 4 seems reasonable
			foreach (OverlapInfo oi in m_proxyListRev)
				oi.overlappedItemsInOther = new List<OverlapInfo>(4); //initial capacity of 4 seems reasonable

			// Compare each pair of proxy elements across the Curr and Rev lists,
			// and fill in each element's overlappedItems list with those proxies
			// in the other list that have a potential section head correlation.
			foreach (OverlapInfo oiCurr in m_proxyListCurr)
			{
				foreach (OverlapInfo oiRev in m_proxyListRev)
				{

					if (PotentialSectionHeadCorrelation(oiCurr, oiRev))
					{
						oiCurr.overlappedItemsInOther.Add(oiRev);
						oiRev.overlappedItemsInOther.Add(oiCurr);
					}
				}
			}

			// If, after the natural correlations have been found, the first
			// two section heads remain uncorrelated, correlate them to each
			// other
			if (m_proxyListRev.Count > 0 && m_proxyListCurr.Count > 0)
			{
				List<OverlapInfo> otherItemsForRev = m_proxyListRev[0].overlappedItemsInOther;
				List<OverlapInfo> otherItemsForCurr = m_proxyListCurr[0].overlappedItemsInOther;
				// Only add a potential correlation if no others exist.  We want
				// to correlate the first two if nothing else correlates, but
				// if we establish a potential correlation when others exist,
				// it will always get the preference (because our algorithm
				// always chooses the first potential)
				if (otherItemsForCurr.Count == 0 && otherItemsForRev.Count == 0)
				{
					otherItemsForCurr.Add(m_proxyListRev[0]);
					otherItemsForRev.Add(m_proxyListCurr[0]);
				}
			}
			// Do the same for the last two section heads.  Note that we only
			// want to make this correlation if the last two remain uncorrelated
			// even after potentially being correlated by the previous if block
			// (as in the case of a one-to-many)
			if (m_proxyListRev.Count > 1 && m_proxyListCurr.Count > 1)
			{
				// Get the indices of the last elements
				int lastRev = m_proxyListRev.Count - 1;
				int lastCurr = m_proxyListCurr.Count - 1;

				List<OverlapInfo> otherItemsForRev = m_proxyListRev[lastRev].overlappedItemsInOther;
				List<OverlapInfo> otherItemsForCurr = m_proxyListCurr[lastCurr].overlappedItemsInOther;

				// Always add a potential correlation for the last pair.
				// This ensures (just as only adding to the front if one
				// doesn't exist ensures) that the final pair will only
				// be correlated if no other correlations were established,
				// in this case also because of the way the algorithm works;
				// since it always chooses the first pair, if this pair is
				// chosen we know by default that there were no others
				otherItemsForCurr.Add(m_proxyListRev[lastRev]);
				otherItemsForRev.Add(m_proxyListCurr[lastCurr]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Builds a cluster list of correlation clusters, based upon the possible correlations
		/// previously established.  Since one section head must correlate to at most one other
		/// section head, even though a number of possible correlations exist, priority is given
		/// to those that come first in the file over those that come later.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DetermineCorrelatedSectionHeadClusters()
		{
			// create an the output list to contain the correlation clusters
			List<Cluster> clusterList = new List<Cluster>();

			// Create destructible copies of each master list, allowing already
			// grouped items to be removed in any order
			List<OverlapInfo> proxyListRevCopy = new List<OverlapInfo>(m_proxyListRev.ToArray());
			List<OverlapInfo> proxyListCurrCopy = new List<OverlapInfo>(m_proxyListCurr.ToArray());

			// So long as there are remaining proxies, keep evaluating them for
			// correlations
			while (proxyListRevCopy.Count > 0 || proxyListCurrCopy.Count > 0)
			{
				// The curr and rev proxies that will form a correlation.
				// One of these may be null if no correlation exists
				OverlapInfo proxyCurr;
				OverlapInfo proxyRev;

				// If both lists have remaining proxies...
				if (proxyListRevCopy.Count > 0 && proxyListCurrCopy.Count > 0)
				{
					// choose whichever next one has the earlier start reference
					// (note: if refs are equal, doesn't matter which one)
					proxyRev = (OverlapInfo)proxyListRevCopy[0];
					proxyCurr = (OverlapInfo)proxyListCurrCopy[0];
					if (proxyRev.verseRefMin < proxyCurr.verseRefMin)
					{
						// Reset the current proxy to the first possible
						// correlating proxy that has not already been
						// used, or null if none exists
						proxyCurr = null;

						foreach (OverlapInfo oi in proxyRev.overlappedItemsInOther)
						{
							if (proxyListCurrCopy.Contains(oi))
							{
								proxyCurr = oi;
								break;
							}
						}
					}
					else
					{
						// Reset the rev proxy to the first possible
						// correlating proxy, or null if none exists
						proxyRev = null;

						foreach (OverlapInfo oi in proxyCurr.overlappedItemsInOther)
						{
							if (proxyListRevCopy.Contains(oi))
							{
								proxyRev = oi;
								break;
							}
						}
					}
				}
				// Otherwise, use whatever remains
				else if (proxyListRevCopy.Count > 0)
				{
					proxyRev = (OverlapInfo)proxyListRevCopy[0];
					proxyCurr = null;
				}
				else
				{
					proxyCurr = (OverlapInfo)proxyListCurrCopy[0];
					proxyRev = null;
				}

				// Build a new correlation cluster
				Cluster correlationCluster = new Cluster();
				// so long as the rev proxy exists, add it to the cluster
				// and remove it from it's original list
				if (proxyRev != null)
				{
					correlationCluster.itemsRev.Add(proxyRev);
					proxyListRevCopy.Remove(proxyRev);
					// Assume (for reference sake) that the rev is the only
					// existing proxy and set the references accordingly
					correlationCluster.verseRefMin = proxyRev.verseRefMin;
					correlationCluster.verseRefMax = proxyRev.verseRefMax;
				}
				// same with the Curr
				if(proxyCurr != null)
				{
					correlationCluster.itemsCurr.Add(proxyCurr);
					proxyListCurrCopy.Remove(proxyCurr);
					// Assume (for reference sake) that the curr is the only
					// existing proxy and set the references accordingly
					correlationCluster.verseRefMin = proxyCurr.verseRefMin;
					correlationCluster.verseRefMax = proxyCurr.verseRefMax;
				}
				// If both a rev proxy and a curr proxy exist, adjust their
				// references (correcting their assumptions)
				if (proxyRev != null && proxyCurr != null)
				{
					correlationCluster.verseRefMin = Math.Min(proxyRev.verseRefMin, proxyCurr.verseRefMin);
					correlationCluster.verseRefMax = Math.Max(proxyRev.verseRefMax, proxyCurr.verseRefMax);
				}
				// Finally, add the newly created cluster to our cluster list
				clusterList.Add(correlationCluster);
			}

			// Hand off our finished list
			m_clusterList = clusterList;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines the cluster type for each correlation Cluster in our list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DetermineCorrelationClusterTypes()
		{
			foreach (Cluster cluster in m_clusterList)
				cluster.DetermineType();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if two OverlapInfos have a potential section head correlation:
		/// the section start refs are within two verses
		/// </summary>
		/// <param name="oi1">first OverlapInfo to check</param>
		/// <param name="oi2">second overlapInfo to check</param>
		/// <returns>true if there is potential correlation</returns>
		/// ------------------------------------------------------------------------------------
		protected bool PotentialSectionHeadCorrelation(OverlapInfo oi1, OverlapInfo oi2)
		{
			// are section start refs are within two verses?
			if (Math.Abs(oi1.verseRefMin - oi2.verseRefMin) <= 2)
				return true;
			// if not they are too far apart
			return false;
		}
	}
	#endregion

#if DEBUG
	#region ClusterListUtils class
	internal static class ClusterListUtils
	{
		public static void AssertValid(this List<OverlapInfo> items)
		{
			if (items.Count <= 1)
				return;

			HashSet<IScrTxtPara> paras = new HashSet<IScrTxtPara>();
			foreach (IScrTxtPara para in items.Select(info => (IScrTxtPara)info.myObj))
			{
				if (paras.Contains(para))
					Debug.Fail("A ParaStructure cluster must have a different paragraph in each ScrVerse");
				paras.Add(para);
			}
		}
	}
	#endregion
#endif
}
