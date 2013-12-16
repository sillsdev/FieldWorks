// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ParaNodeMap.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// <summary>
	/// Serves as a sort of address for a paragraph within a book that can then be
	/// used to determine the relative positions of paragraphs to each other, along
	/// with information regarding the type of section/book they are in
	/// </summary>
	public class ParaNodeMap : IComparable
	{
		#region Member Variables and Constants
		/// <summary>
		/// Stores, in order, the bookflid which is either a kflidTitle or kflidSections,
		/// the iSection (the index), the sectionflid which is either a flidHeading
		/// or kflidContent, an iPara and an iChar.  Together they describe the location of a
		/// paragraph in relation to other paragraphs in the book, and can be used
		/// to detect jumps from section to section, or paragraph to paragraph
		/// </summary>
		protected int[] m_location = new int[6];

		/// <summary>
		/// The index in the m_location array that the book index should be found at
		/// </summary>
		protected const int kBookIndex = 0;
		/// <summary>
		/// The index in the m_location array to find the bookFlid
		/// </summary>
		protected const int kBookFlidIndex = 1;
		/// <summary>
		/// The index in the m_location array to find the section index
		/// </summary>
		protected const int kSectionIndex = 2;
		/// <summary>
		/// The index in the m_location array to find the sectionFlid
		/// </summary>
		protected const int kSectionFlidIndex = 3;
		/// <summary>
		/// The index in the m_location array to find the paragraph index
		/// </summary>
		protected const int kParaIndex = 4;
		/// <summary>
		/// The index in the m_location array to find the character offset within the para
		/// </summary>
		protected const int kCharIndex = 5;

		// The constants kflidTitle and kflidSections are not guaranteed to be in
		// the same order that we would want them sorted.  Define two constants:
		// kTitle and kSection to ensure that when compared they sort correctly.
		// Internally we will store the data using these constants, translating
		// as necessary to the actual ones when they are requested
		private const int kTitle = 0;
		private const int kSections = 1;
		// Define two similar constants for the kflidHeading and kflidContent
		private const int kHeading = 0;
		private const int kContent = 1;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Protected default constructor for testing purposes, and Clone
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected ParaNodeMap()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new ParaNodeMap containing the location information that
		/// can be extracted from this paragraph (namely whether it is in a title
		/// or section, what section is lives in, whether that section is a heading
		/// or content section, and which paragraph it is within that section)
		/// </summary>
		/// <param name="para">The paragraph to map</param>
		/// ------------------------------------------------------------------------------------
		public ParaNodeMap(IStTxtPara para)
		{
			ConstructFromPara(para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new ParaNodeMap containing the location information that
		/// can be extracted from this ScrVerse (namely whether it is in a title
		/// or section, what section is lives in, whether that section is a heading
		/// or content section, which paragraph it is within that section, and the character
		/// offset in the paragraph)
		/// </summary>
		/// <param name="verse">The given ScrVerse.</param>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		public ParaNodeMap(ScrVerse verse, FdoCache cache)
		{
			// Get para containing the ScrVerse and
			ConstructFromPara(verse.Para);

			// Add character offset of ScrVerse
			m_location[kCharIndex] = verse.VerseStartIndex;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs ParaNode map using the paragraph.
		/// </summary>
		/// <param name="para">The given paragraph.</param>
		/// ------------------------------------------------------------------------------------
		private void ConstructFromPara(IStTxtPara para)
		{
			// Get the iPara first
			m_location[kParaIndex] = para.IndexInOwner;

			// Prepare to get the others
			IStText text = (IStText)para.Owner;

			// If the owner is a book, we know that sections are not relevant
			// to this paragraph and can process accordingly
			if (text.Owner is IScrBook)
			{
				// If we've got a title, we can set the rest in short order
				// (we know that there are no sections)
				// (kflidFootnotes occures in tests, e.g. DomainDataByFlidTests.MakeNewObjectTest_StFootnote)
				Debug.Assert(text.OwningFlid == ScrBookTags.kflidTitle ||
					text.OwningFlid == ScrBookTags.kflidFootnotes);
				m_location[kSectionFlidIndex] = 0;
				m_location[kSectionIndex] = 0;
				m_location[kBookFlidIndex] = kTitle;
				m_location[kBookIndex] = text.Owner.IndexInOwner;
			}
			// Otherwise, we'll need to use the section data
			else if (text.Owner is IScrSection)
			{
				// Since we have sections, get the flid and set others based upon it
				int sectionFlid = text.OwningFlid;
				if (sectionFlid == ScrSectionTags.kflidHeading)
					m_location[kSectionFlidIndex] = kHeading;
				else if (sectionFlid == ScrSectionTags.kflidContent)
					m_location[kSectionFlidIndex] = kContent;
				// If it was not one of these types, something is wrong, it's an invalid
				// flid
				else
					Debug.Assert(false);

				IScrSection section = (IScrSection)text.Owner;
				m_location[kSectionIndex] = section.IndexInOwner;
				m_location[kBookFlidIndex] = kSections;

				m_location[kBookIndex] = section.Owner.IndexInOwner;
			}
			else
				Debug.Assert(false);

			// default char offset
			m_location[kCharIndex] = 0;
		}
		#endregion

		#region properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the bookflid which is either a kflidTitle or a kflidSections
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BookFlid
		{
			get
			{
				if (m_location[kBookFlidIndex] == kTitle)
					return ScrBookTags.kflidTitle;
				else
					return ScrBookTags.kflidSections;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of this paragraph's section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int SectionIndex
		{
			get { return m_location[kSectionIndex]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sectionflid which is either either a kflidHeading or a kflidContent
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int SectionFlid
		{
			get
			{
				if (m_location[kSectionFlidIndex] == kHeading)
					return ScrSectionTags.kflidHeading;
				else
					return ScrSectionTags.kflidContent;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of this paragraph within its owning StText
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ParaIndex
		{
			get { return m_location[kParaIndex]; }
		}
		#endregion

		#region Utility/Comparison Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares this object against the supplied one.  Objects are compared on the
		/// basis of their location, starting from the broadest form (where they are in
		/// the book), to the most specific (which paragraph in a section are they)
		/// </summary>
		/// <param name="o">The object to compare myself to</param>
		/// <returns>
		/// The integer value signifying this object's relationship to the one given.
		/// This relationship is an expression of our relationship to the supplied object.
		/// If this one is bigger than the one given, return 1, if smaller, -1, and if they
		/// are equal, 0
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public int CompareTo(object o)
		{
			// Cast the given object and store it, so we don't have to keep casting it
			ParaNodeMap compareAgainst = (ParaNodeMap)o;

			// Walk down the list, starting at the least level of detail (the book level)
			// moving inwards towards paragraphs
			for (int i = 0; i < this.m_location.Length; i++)
			{
				// If at our current level of the depth, the object given to compare to
				// us is less than us, return 1 to show that we are bigger
				if (compareAgainst.m_location[i] < this.m_location[i])
				{
					return 1;
				}
				// If it is greater than us, return -1
				else if (compareAgainst.m_location[i] > this.m_location[i])
				{
					return -1;
				}
			}

			// If the whole loop ran through and never returned, return 0
			// to signify that the objects are equal
			return 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Two ParaNodeMaps are considered to be equal if they indicate the same exact location
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object o)
		{
			// Cast the given object and store it, so we don't have to keep casting it
			ParaNodeMap compareAgainst = (ParaNodeMap)o;

			// Walk down each list, comparing them side-by-side to see if any of the
			// elements are different
			for (int i = 0; i < this.m_location.Length; i++)
			{
				// If any of the elements in our arrays are different, return false; they aren't
				// equal unless they're equal on every element
				if (compareAgainst.m_location[i] != this.m_location[i])
				{
					return false;
				}
			}

			// If the whole loop ran through we know that the lists have the same values in them,
			// return true--they are equal
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// GetHashCode uses the hash code of the location
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return m_location.GetHashCode();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a deep copy of this ParaNodeMap
		/// </summary>
		/// <returns>The copy</returns>
		/// ------------------------------------------------------------------------------------
		public ParaNodeMap Clone()
		{
			ParaNodeMap clonedMap = new ParaNodeMap();
			clonedMap.m_location = (int[])this.m_location.Clone(); // int[].Clone may only return its reference
			return clonedMap;
		}
		#endregion
	}
}
