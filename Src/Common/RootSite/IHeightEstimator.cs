// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IHeightEstimator.cs
// Responsibility:
// --------------------------------------------------------------------------------------------
using System;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IHeightEstimator
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Estimate the height of an object in points.
		/// </summary>
		/// <param name="hvo">The hvo of the object</param>
		/// <param name="frag">The frag used to display the object</param>
		/// <param name="availableWidth"></param>
		/// <returns>The estimated height for the specified object in points</returns>
		/// ------------------------------------------------------------------------------------
		int EstimateHeight(int hvo, int frag, int availableWidth);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the average paragraph height in points.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int AverageParaHeight { get; }
	}
}
