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
// File: TsStringComparer.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Compare two ITsStrings.
	/// </summary>
	/// <remarks>This class does not check the writing systems of the strings to compare but
	/// uses the writing system passed in to the constructor.</remarks>
	/// ----------------------------------------------------------------------------------------
	public class TsStringComparer : IComparer
	{
		private readonly IWritingSystem m_ws;

		#region Constructors

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TsStringComparer"/> class.
		/// </summary>
		/// <remarks>This version of the constructor uses .NET to compare two ITsStrings.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public TsStringComparer()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TsStringComparer"/> class.
		/// </summary>
		/// <param name="ws">The writing system.</param>
		/// ------------------------------------------------------------------------------------
		public TsStringComparer(IWritingSystem ws)
		{
			m_ws = ws;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the collating engine.
		/// </summary>
		/// <value>The collating engine.</value>
		/// ------------------------------------------------------------------------------------
		public IWritingSystem WritingSystem
		{
			get
			{
				return m_ws;
			}
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
		/// Value Condition
		/// Less than zero = x is less than y.
		/// Zero = x equals y.
		/// Greater than zero = x is greater than y.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">Neither x nor y implements the
		/// <see cref="T:System.IComparable"/> interface.-or- x and y are of different types and
		/// neither one can handle comparisons with the other. </exception>
		/// ------------------------------------------------------------------------------------
		public int Compare(object x, object y)
		{
			if ((!(x is ITsString || x is string) || !(y is ITsString || y is string)) &&
				x != null && y != null)
			{
				throw new ArgumentException();
			}

			string xString = (x is ITsString) ? ((ITsString)x).Text : x as string;
			string yString = (y is ITsString) ? ((ITsString)y).Text : y as string;
			if (xString == string.Empty)
				xString = null;
			if (yString == string.Empty)
				yString = null;

			if (xString == null && yString == null)
				return 0;

			if (xString == null)
				return -1;

			if (yString == null)
				return 1;

			if (m_ws != null)
				return m_ws.Collator.Compare(xString, yString);

			return xString.CompareTo(yString);
		}

		#endregion
	}
}
