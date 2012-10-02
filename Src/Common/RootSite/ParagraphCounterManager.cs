// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ParagraphCounterManager.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Collections.Generic;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.RootSites
{
	#region IParagraphCounter
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IParagraphCounter
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of paragraphs that make up an object when displayed as the specified
		/// frag
		/// </summary>
		/// <param name="hvo">The hvo of the object</param>
		/// <param name="frag">The frag used to display the object</param>
		/// <returns>The number of paragraphs that make up the specified object for the
		/// specified frag</returns>
		/// ------------------------------------------------------------------------------------
		int GetParagraphCount(int hvo, int frag);
	}
	#endregion

	#region ParagraphCounterManager class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ParagraphCounterManager
	{
		#region ParaCounterKey struct
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Keys for entries in the ParagraphCounterManager hash table
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		private struct ParaCounterKey
		{
			private readonly FdoCache m_cache;
			private readonly int m_viewTypeId;

			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// <param name="cache">The Fdo cache</param>
			/// <param name="viewTypeId">An identifier for a group of views that share the same
			/// height estimates</param>
			/// ------------------------------------------------------------------------------------
			public ParaCounterKey(FdoCache cache, int viewTypeId)
			{
				m_cache = cache;
				m_viewTypeId = viewTypeId;
			}
		}
		#endregion

		/// <summary>
		/// Hashtable containing a hashtable of ParagraphCounters
		/// Key = ParaCounterKey
		/// Value = IParagraphCounter
		/// </summary>
		private static Dictionary<ParaCounterKey, IParagraphCounter> s_paraCounters = new Dictionary<ParaCounterKey, IParagraphCounter>();

		/// <summary>The type of object used to create a new ParaCounter</summary>
		/// <remarks>
		/// We use the same para counter within one application, so it can be
		/// safely static. We can't hardcode the type here because we can't reference application
		/// specific stuff.
		/// </remarks>
		private static Type s_paraCounterType;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Only static methods - so no need to instantiate this class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ParagraphCounterManager()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the type used to create new para counters
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Type ParagraphCounterType
		{
			set { s_paraCounterType = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cache">The Fdo cache</param>
		/// <param name="viewTypeId">An identifier for a group of views that share the same
		/// height estimator</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static IParagraphCounter GetParaCounter(FdoCache cache, int viewTypeId)
		{
			ParaCounterKey key = new ParaCounterKey(cache, viewTypeId);
			IParagraphCounter counter = null;
			if (!s_paraCounters.ContainsKey(key) && s_paraCounterType != null)
			{
				counter = (IParagraphCounter)Activator.CreateInstance(s_paraCounterType,
					new object[] { cache });
				Debug.Assert(counter != null);
				s_paraCounters[key] = counter;
			}
			// May need another try to get it, since it may not get created, above.
			if (counter == null)
				s_paraCounters.TryGetValue(key, out counter);
			return counter;
		}
	}
	#endregion
}
