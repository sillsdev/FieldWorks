// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PubPageInfo.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.FieldWorks.Common.PrintLayout
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Information about the publication page size
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class PubPageInfo
	{
		/// <summary>publication page size name</summary>
		public string Name;
		/// <summary>page height in millipoints</summary>
		public int Height;
		/// <summary>page width in millipoints</summary>
		public int Width;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:PubPageInfo"/> class.
		/// </summary>
		/// <param name="name">The name of the publication page size.</param>
		/// <param name="height">page height in millipoints.</param>
		/// <param name="width">page width in millipoints.</param>
		/// ------------------------------------------------------------------------------------
		public PubPageInfo(string name, int height, int width)
		{
			Name = name;
			Height = height;
			Width = width;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current
		/// <see cref="T:System.Object"></see>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Name;
		}
	}
}
