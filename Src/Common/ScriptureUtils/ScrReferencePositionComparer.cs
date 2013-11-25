// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ScrReferencePositionComparer.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Text;
using SILUBS.SharedScrUtils;
using System.Collections;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Comparer for scripture references. This allows scripture references to be sorted in
	/// the order they appear in the Bible. The character offset within the verse is also
	/// used, if necessary, to sort the references.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrReferencePositionComparer : IComparer
	{
		#region Data members
		//ScriptureReferenceComparer m_scrRefComparer;
		//private bool m_assertOnInvalidRef = true;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrReferencePositionComparer"/> class.
		/// </summary>
		/// <param name="scrProj">An object which can provide metadata about the Scripture
		/// project (needed to get the current versification when constructing new
		/// ScrReferences).</param>
		/// <param name="assertOnInvalidRef">Flag indicating whether to complain about invalid
		/// references.</param>
		/// ------------------------------------------------------------------------------------
		public ScrReferencePositionComparer(IScrProjMetaDataProvider scrProj,
			bool assertOnInvalidRef)
		{
			//m_assertOnInvalidRef = assertOnInvalidRef;
			//m_scrRefComparer = new ScriptureReferenceComparer(scrProj, assertOnInvalidRef);
		}

		#region IComparer Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than, equal
		/// to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>
		/// Value				Condition
		/// Less than zero		<paramref name="x"/> is less than <paramref name="y"/>.
		/// Zero				<paramref name="x"/> equals <paramref name="y"/>.
		/// Greater than zero	<paramref name="x"/> is greater than <paramref name="y"/>.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">Neither <paramref name="x"/> nor
		/// <paramref name="y"/> implements the <see cref="T:System.IComparable"/> interface.
		/// -or- <paramref name="x"/> and <paramref name="y"/> are of different types and neither
		/// one can handle comparisons with the other. </exception>
		/// ------------------------------------------------------------------------------------
		public int Compare(object x, object y)
		{
			if (!(x is ReferencePositionType) || !(y is ReferencePositionType))
			{
				throw new ArgumentException();
			}

			if (x == null && y == null)
				return 0;
			if (x == null)
				return -1;
			if (y == null)
				return 1;

			ReferencePositionType xRefPos;
			ReferencePositionType yRefPos;

			xRefPos = (ReferencePositionType)x;
			yRefPos = (ReferencePositionType)y;

			int refCompareVal = xRefPos.m_reference.CompareTo(yRefPos.m_reference);

			// If the Scripture references are not the same...
			if (refCompareVal != 0)
				return refCompareVal; // return comparison for Scripture references

			// If the sections are not the same...
			if (xRefPos.m_iSection != yRefPos.m_iSection)
			{
				return (xRefPos.m_iSection == -1 ? 1:
					xRefPos.m_iSection.CompareTo(yRefPos.m_iSection)); // return section comparison
			}

			// If the paragraphs are not the same...
			if (xRefPos.m_iPara != yRefPos.m_iPara)
			{
				return (xRefPos.m_iPara == -1 ? 1 :
					xRefPos.m_iPara.CompareTo(yRefPos.m_iPara)); // return paragraph comparison
			}

			// If the character offsets are not the same...
			if (xRefPos.m_ich != yRefPos.m_ich)
				return xRefPos.m_ich.CompareTo(yRefPos.m_ich); // return character index comparison

			return 0;
		}
		#endregion
	}

	#region ReferencePositionType
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// A class containing information about the Scripture reference and character offset
	/// within the verse.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class ReferencePositionType
	{
		/// <summary>Scripture reference</summary>
		public int m_reference;
		/// <summary>index of the section, necessary for sorting in case the a verse spans
		/// a section boundary</summary>
		public int m_iSection;
		/// <summary>index of the paragraph within the section, necessary for sorting in case
		/// a verse spans a paragraph boundary.</summary>
		public int m_iPara;
		/// <summary>Character offset of the reference within the verse</summary>
		public int m_ich;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ReferencePositionType"/> class.
		/// </summary>
		/// <param name="reference">Integer representation of the Scripture reference.</param>
		/// <param name="iSection">The zero-based index of the section.</param>
		/// <param name="iPara">The zero-based index of the paragraph.</param>
		/// <param name="ich">The zero-based character offset within the verse.</param>
		/// --------------------------------------------------------------------------------
		public ReferencePositionType(int reference, int iSection, int iPara, int ich)
		{
			m_reference = reference;
			m_iSection = iSection;
			m_iPara = iPara;
			m_ich = ich;
		}
	}
	#endregion
}
