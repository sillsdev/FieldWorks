// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ParagraphCounterRepository.cs

using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	#region ParagraphCounterRepository class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class ParagraphCounterRepository : IParagraphCounterRepository
	{
		private readonly Dictionary<int, IParagraphCounter> m_paraCounters = new Dictionary<int, IParagraphCounter>();
		private readonly Dictionary<int, Type> m_viewIdToCounterType = new Dictionary<int, Type>();
		private readonly FdoCache m_cache;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ParagraphCounterRepository"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		internal ParagraphCounterRepository(FdoCache cache)
		{
			m_cache = cache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unregisters the view type id.
		/// </summary>
		/// <param name="viewTypeId">The view type id.</param>
		/// ------------------------------------------------------------------------------------
		public void UnregisterViewTypeId(int viewTypeId)
		{
			m_viewIdToCounterType.Remove(viewTypeId);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Registers the type used to create new para counters for the specified view type id
		/// </summary>
		/// <typeparam name="T">The class of para counter to create</typeparam>
		/// <param name="viewTypeId">The view type id.</param>
		/// ------------------------------------------------------------------------------------
		public void RegisterViewTypeId<T>(int viewTypeId) where T : IParagraphCounter
		{
			Debug.Assert(!m_viewIdToCounterType.ContainsKey(viewTypeId));

			m_viewIdToCounterType[viewTypeId] = typeof(T);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the paragraph counter for the gieven type of view
		/// </summary>
		/// <param name="viewTypeId">An identifier for a group of views that share the same
		/// height estimator</param>
		/// ------------------------------------------------------------------------------------
		public IParagraphCounter GetParaCounter(int viewTypeId)
		{
			IParagraphCounter counter;
			if (m_paraCounters.TryGetValue(viewTypeId, out counter))
				return counter;

			Type paraCounterType;
			if (!m_viewIdToCounterType.TryGetValue(viewTypeId, out paraCounterType))
				return null;

			counter = (IParagraphCounter)Activator.CreateInstance(paraCounterType, m_cache);
			Debug.Assert(counter != null);
			m_paraCounters[viewTypeId] = counter;
			return counter;
		}
	}
	#endregion
}
