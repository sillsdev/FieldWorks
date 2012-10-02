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
// File: Class1.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.FieldWorks.Tools
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Usage statistics of file cache
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Serializable]
	public class Statistics
	{
		/// <summary></summary>
		public int Missed;
		/// <summary></summary>
		public int Hits;
		/// <summary>Local miss, but available remotely</summary>
		public int RemoteHits;

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the counters
		/// </summary>
		/// ------------------------------------------------------------------------------------------
		public void Reset()
		{
			Missed = 0;
			Hits = 0;
			RemoteHits = 0;
		}
	}
}
