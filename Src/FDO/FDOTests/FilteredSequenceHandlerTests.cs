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
// File: FilteredSequenceHandlerTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the FilteredSequenceHandler class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FilteredSequenceHandlerTests : InMemoryFdoTestBase
	{
		#region Class DummyRow
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dummy row class that adds an arbitrary Id that we can use in our tests. This gives
		/// us a number that is independent of the object index (which changes when we add or
		/// delete objects).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class DummyRow : CmRow
		{
			private static int s_id = 0;
			private int m_id;
			private static Dictionary<int, int> s_HvoIds = new Dictionary<int, int>();

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="DummyRow"/> class.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public DummyRow(): base()
			{
				m_id = s_id++;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="DummyRow"/> class.
			/// </summary>
			/// <param name="id">The id.</param>
			/// --------------------------------------------------------------------------------
			public DummyRow(int id)
				: base()
			{
				m_id = id;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="DummyRow"/> class.
			/// </summary>
			/// <param name="cache">The cache.</param>
			/// <param name="hvo">The hvo.</param>
			/// --------------------------------------------------------------------------------
			public DummyRow(FdoCache cache, int hvo): base(cache, hvo)
			{
				m_id = s_HvoIds[hvo];
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Inits the new.
			/// </summary>
			/// <param name="fcCache">The fc cache.</param>
			/// --------------------------------------------------------------------------------
			protected override void InitNew(FdoCache fcCache)
			{
				base.InitNew(fcCache);
				s_HvoIds.Add(Hvo, m_id);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Inits the new.
			/// </summary>
			/// <param name="fcCache">The fc cache.</param>
			/// <param name="hvoOwner">The hvo owner.</param>
			/// <param name="flidOwning">The flid owning.</param>
			/// <param name="ihvo">The ihvo.</param>
			/// --------------------------------------------------------------------------------
			protected override void InitNew(FdoCache fcCache, int hvoOwner, int flidOwning, int ihvo)
			{
				base.InitNew(fcCache, hvoOwner, flidOwning, ihvo);
				s_HvoIds.Add(Hvo, m_id);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Resets the static id.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public static void ResetId()
			{
				s_id = 0;
				s_HvoIds.Clear();
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the id used for testing.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int Id
			{
				get { return m_id; }
			}
		}
		#endregion

		#region Class DummyChangeWatcher
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dummy change watcher for filtered texts
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class DummyChangeWatcher : ChangeWatcher
		{
			internal int m_ivMin = -1;
			internal int m_cvIns;
			internal int m_cvDel;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="DummyChangeWatcher"/> class.
			/// </summary>
			/// <param name="handler">The filtered sequence handler.</param>
			/// --------------------------------------------------------------------------------
			public DummyChangeWatcher(FilteredSequenceHandler handler)
				: base(handler.Cache, handler.Tag)
			{
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Does the effects of prop change.
			/// </summary>
			/// <param name="hvo">The hvo.</param>
			/// <param name="ivMin">The iv min.</param>
			/// <param name="cvIns">The cv ins.</param>
			/// <param name="cvDel">The cv del.</param>
			/// --------------------------------------------------------------------------------
			protected override void DoEffectsOfPropChange(int hvo, int ivMin, int cvIns, int cvDel)
			{
				m_ivMin = ivMin;
				m_cvIns = cvIns;
				m_cvDel = cvDel;
			}
		}
		#endregion

		#region Class OddFilter
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implements a filter for testing that ignores any DummyRow object with an odd Id
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal class OddRowFilter : IFilter
		{
			#region Data members
			private FdoCache m_cache;
			private int m_flid;
			private bool m_fInitCriteriaCalled = false;
			#endregion

			#region Constructor
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="OddRowFilter"/> class.
			/// </summary>
			/// <param name="cache">The cache.</param>
			/// <param name="flid">The flid.</param>
			/// --------------------------------------------------------------------------------
			public OddRowFilter(FdoCache cache, int flid)
			{
				m_cache = cache;
				m_flid = flid;
			}
			#endregion

			#region IFilter Members
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Inits the criteria.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public void InitCriteria()
			{
				if (m_fInitCriteriaCalled)
					throw new InvalidOperationException("InitCriteria already called for the OddRowFilter class.");
				m_fInitCriteriaCalled = true;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Tests the object to see if it matches the criteria.
			/// </summary>
			/// <param name="hvoObj">The hvo obj.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			public bool MatchesCriteria(int hvoObj)
			{
				if (!m_fInitCriteriaCalled)
					throw new InvalidOperationException("InitCriteria not called for the OddRowFilter class.");

				DummyRow text = new DummyRow(m_cache, hvoObj);
				return (text.Id % 2) == 0;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the name.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public string Name
			{
				get { return GetType().Name; }
			}

			#endregion
		}
		#endregion

		#region Data members
		private ICmFilter m_filter;
		#endregion

		#region Setup

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the FDO cache and open database
		/// </summary>
		/// <remarks>This method is called before each test</remarks>
		/// ------------------------------------------------------------------------------------
		public override void Initialize()
		{
			DummyRow.ResetId();
			base.Initialize();
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows subclasses to do other stuff to initialize the cache before it gets used
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void InitializeCache()
		{
			m_inMemoryCache.InitializeLexDb();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to make the test data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_filter = new CmFilter();
			Cache.LangProject.FiltersOC.Add(m_filter);
			AddRow(m_filter, new DummyRow());
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the specified row to the filter and issues a PropChanged.
		/// </summary>
		/// <param name="filter">The filter.</param>
		/// <param name="dummyRow">The dummy row.</param>
		/// ------------------------------------------------------------------------------------
		private void AddRow(ICmFilter filter, DummyRow dummyRow)
		{
			filter.RowsOS.Append(dummyRow);
			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, dummyRow.OwnerHVO,
				dummyRow.OwningFlid, filter.RowsOS.Count - 1, 1, 0);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the virtual index
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetVirtualIndex()
		{
			FilteredSequenceHandler handler = new FilteredSequenceHandler(Cache,
				LangProject.kClassId, 1, new OddRowFilter(Cache,
				(int)CmFilter.CmFilterTags.kflidRows), null,
				new SimpleFlidProvider((int)CmFilter.CmFilterTags.kflidRows));

			// setup
			// Added by setup:         0, virtual index 0
			AddRow(m_filter, new DummyRow());	// 1, virtual index -1
			AddRow(m_filter, new DummyRow());	// 2, virtual index 1
			AddRow(m_filter, new DummyRow());	// 3, virtual index -1

			int hvoFilter = m_filter.Hvo;
			handler.Load(hvoFilter, handler.Tag, 0, Cache.VwCacheDaAccessor);

			// Test the GetVirtualIndex method
			Assert.AreEqual(0, handler.GetVirtualIndex(hvoFilter, 0));
			Assert.AreEqual(-1, handler.GetVirtualIndex(hvoFilter, 1));
			Assert.AreEqual(1, handler.GetVirtualIndex(hvoFilter, 2));
			Assert.AreEqual(-1, handler.GetVirtualIndex(hvoFilter, 3));
		}
	}
}
