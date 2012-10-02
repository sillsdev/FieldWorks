// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScriptureReferenceComparer.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Diagnostics;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Comparer for scripture references. This allows scripture references to be sorted in
	/// the order they appear in the Bible.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScriptureReferenceComparer : IComparer
	{
		#region Data members
		IScrProjMetaDataProvider m_scrProj;
		private bool m_failOnInvalidRef = false;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScriptureReferenceComparer"/> class.
		/// </summary>
		/// <param name="scrProj">An object which can provide metadata about the Scripture
		/// project (needed to get the current versification when constructing new
		/// ScrReferences).</param>
		/// ------------------------------------------------------------------------------------
		public ScriptureReferenceComparer(IScrProjMetaDataProvider scrProj)
		{
			m_scrProj = scrProj;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for ScriptureReferenceComparer.
		/// Use this constructor when creating a comparer for the scripture checks result grid,
		/// since the scripture checks result grid may contain invalid references. We don't want
		/// to throw an assertion in that case.
		/// </summary>
		/// <param name="scrProj">An object which can provide metadata about the Scripture
		/// project (needed to get the current versification when constructing new
		/// ScrReferences).</param>
		/// <param name="failOnInvalidRef">Flag indicating whether to thow an ArgumentException
		/// if one or both of the references is invalid.</param>
		/// ------------------------------------------------------------------------------------
		public ScriptureReferenceComparer(IScrProjMetaDataProvider scrProj,
			bool failOnInvalidRef) : this(scrProj)
		{
			m_failOnInvalidRef = failOnInvalidRef;
		}
		#endregion

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
			if (!(x is string || x is int || x is ScrReference) ||
				!(y is string || y is int || y is ScrReference))
			{
				throw new ArgumentException();
			}

			if (x == null && y == null)
				return 0;
			if (x == null)
				return -1;
			if (y == null)
				return 1;

			ScrReference xRef;
			ScrReference yRef;

			if (x is string)
				xRef = new ScrReference((string)x, m_scrProj.Versification);
			else if (x is int)
				xRef = new ScrReference((int)x, m_scrProj.Versification);
			else
				xRef = (ScrReference)x;

			if (y is string)
				yRef = new ScrReference((string)y, m_scrProj.Versification);
			else if (y is int)
				yRef = new ScrReference((int)y, m_scrProj.Versification);
			else
				yRef = (ScrReference)y;

			if (m_failOnInvalidRef)
			{
				if (!xRef.Valid && !xRef.IsBookTitle)
					throw new ArgumentException("Invalid reference", "x");
				if (!yRef.Valid && !xRef.IsBookTitle)
					throw new ArgumentException("Invalid reference", "y");
			}

			return xRef.CompareTo(yRef);
		}

		#endregion
	}
}
