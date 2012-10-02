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
// File: FilteredSequenceHandler.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Collections.Generic;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO
{
	#region Interface ICmPossibilitySupplier
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Inteface need to get a CmPossibility for filtering
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface ICmPossibilitySupplier
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the HVO of a single chosen CmPossibility, or 0 to cancel filter.
		/// </summary>
		/// <param name="list">CmPossibilityList from which an item can be chosen</param>
		/// <param name="hvoPoss">HVO of the default CmPossibilty, or 0 if no default</param>
		/// ------------------------------------------------------------------------------------
		int GetPossibility(CmPossibilityList list, int hvoPoss);
		// NB: Using ICmPossibilityList causes Mocks to fail on tests.
	}
	#endregion

	#region Interface IFilter
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This interface allows the FilteredSequenceHandler to filter objects. It calls
	/// MatchesCriteria once for every object in the collection. Depending on the result of this
	/// method the object is discarded or included in the filtered collection.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IFilter
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string Name
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the filter so it can check for matches. This must be called once before
		/// calling <see cref="MatchesCriteria"/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void InitCriteria();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the given object agains the filter criteria
		/// </summary>
		/// <remarks>currently only handles basic filters (single cell)</remarks>
		/// <param name="hvoObj">ID of object to check against the filter criteria</param>
		/// <returns><c>true</c> if the object passes the filter criteria; otherwise
		/// <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		bool MatchesCriteria(int hvoObj);
	}
	#endregion

	#region Interface ISortSpec
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface ISortSpec
	{
		// not implemented yet
	}
	#endregion

	#region Interface IFlidProvider
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface used as a callback from FilteredSequenceHandler. Its purpose is to provide
	/// a flid for a given HVO. A simple implementation (as in SimpleFlidProvider) always
	/// returns a constant flid. More complex implementations (e.g. in UserView) return
	/// different flids based on the HVO.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IFlidProvider
	{
		/// ----------------------------------------------------------------------------------------
		/// <summary>Gets a flid for an object that owns the the virtual property
		/// (e.g., for Consultant Notes, this should be the HVO of a ScrBookAnnotations object)
		/// </summary>
		/// <param name="hvoPropOwner">HVO of the object that owns the collection to be
		/// filtered</param>
		/// <returns>The desired flid</returns>
		/// ----------------------------------------------------------------------------------------
		int GetFlidForPropOwner(int hvoPropOwner);
	}
	#endregion

	#region class SimpleFlidProvider
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A class that implements the IFlidProvider so that it is suitable to be passed in as
	/// a parameter to FilteredSequenceHandler. It simply provides the flid specified in the
	/// constructor.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SimpleFlidProvider: IFlidProvider
	{
		private int m_flid;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:SimpleFlidProvider"/> class.
		/// </summary>
		/// <param name="flid">The flid.</param>
		/// ------------------------------------------------------------------------------------
		public SimpleFlidProvider(int flid)
		{
			m_flid = flid;
		}

		#region IFlidProvider Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a flid for an object that owns the the virtual property
		/// (e.g., for Consultant Notes, this should be the HVO of a ScrBookAnnotations object)
		/// </summary>
		/// <param name="hvoPropOwner">HVO of the object that owns the collection to be
		/// filtered. In this implementation this paramter is ignored.</param>
		/// <returns>The desired flid</returns>
		/// ------------------------------------------------------------------------------------
		public int GetFlidForPropOwner(int hvoPropOwner)
		{
			return m_flid;
		}

		#endregion
	}

	#endregion

	#region Internal VirtualPropChangeWatcher class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This change watcher exists to keep a FilteredSequenceHandler updated when items are
	/// added to or deleted from the filtered property.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class VirtualPropChangeWatcher : ChangeWatcher
	{
		#region Data members
		private FilteredSequenceHandler m_handler;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:VirtualPropChangeWatcher"/> class.
		/// </summary>
		/// <param name="flid">The "real" flid that the filtered sequence handler is filtering.
		/// This must be a sequence property</param>
		/// <param name="handler">The filtered sequence handler for which we are a love slave.
		/// </param>
		/// ------------------------------------------------------------------------------------
		internal VirtualPropChangeWatcher(int flid, FilteredSequenceHandler handler):
			base(handler.Cache, flid)
		{
			m_handler = handler;
		}
		#endregion

		#region Overridden members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fix up the filtered sequence handler in response to added/deleted items in the
		/// sequence it cares about.
		/// </summary>
		/// <param name="hvoOwner">The object that was changed</param>
		/// <param name="ivMin">the starting index where the change occurred</param>
		/// <param name="cvIns">the number of objects inserted</param>
		/// <param name="cvDel">the number of objects deleted</param>
		/// ------------------------------------------------------------------------------------
		protected override void DoEffectsOfPropChange(int hvoOwner, int ivMin, int cvIns, int cvDel)
		{
			if (!m_handler.LoadedObjects.Contains(hvoOwner))
				return;

			// ENHANCE (TE-4794): For now, we'll just recompute the virtual property in response
			// to any addition or deletion, but for performance reasons we might be able to make
			// it better by processing the PropChanged notifications differently if it is
			// possible to figure out quickly where in the filtered sequence to insert or delete.
			// TODO (TE-4081): Handle situation where newly inserted notes might not match filter
			// criteria.
			m_handler.Reinitialize(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to handle PropChanged notifications.
		/// </summary>
		/// <remarks>Default behavior is to ignore PropChanged notifications if the cache is
		/// ignoring them, but we always want to handle them because otherwise Undo/redo won't
		/// work right.</remarks>
		/// ------------------------------------------------------------------------------------
		protected override bool HandlePropChanged
		{
			get { return true; }
		}
		#endregion
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Handles filtering sequences of objects by creating a virtual handler.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FilteredSequenceHandler : BaseVirtualHandler
	{
		#region Member variables
		private IFlidProvider m_flidProvider;
		private FdoCache m_cache;
		private IFilter m_filter;
		private ISortSpec m_sortMethod;
		private Dictionary<int, List<int>> m_filteredObjectsInCache =
			new Dictionary<int, List<int>>();
		private Dictionary<int, VirtualPropChangeWatcher> m_changeWatchers =
			new Dictionary<int, VirtualPropChangeWatcher>();
		#endregion

		#region Constructor & initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FilteredSequenceHandler"/> class.
		/// </summary>
		/// <param name="cache">The FDO cache</param>
		/// <param name="owningClassId">The ID of the class which will own this virtual
		/// property</param>
		/// <param name="filterInstance">Number used to make filters unique for each main
		/// window</param>
		/// <param name="filter">A CmFilter that specifies the details of how to filter the
		/// notes (can be null)</param>
		/// <param name="sortMethod">TODO (TE-3941): use this. A CmSortSpec that specifies the
		/// details of how to sort the notes (can be null)</param>
		/// <param name="flidProvider">The method that provides the flid that is used to
		/// retrieve all records.</param>
		/// ------------------------------------------------------------------------------------
		public FilteredSequenceHandler(FdoCache cache, int owningClassId, int filterInstance,
			IFilter filter, ISortSpec sortMethod, IFlidProvider flidProvider)
		{
			m_cache = cache;
			m_filter = filter;
			m_flidProvider = flidProvider;
			if (m_filter != null)
				m_filter.InitCriteria();
			m_sortMethod = sortMethod;
			ClassName = m_cache.GetClassName((uint)owningClassId);
			FieldName = GetVirtualPropertyName(filter, sortMethod, filterInstance);
			Type = (int) CellarModuleDefns.kcptReferenceSequence;
			ComputeEveryTime = false;
			cache.InstallVirtualProperty(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FilteredSequenceHandler"/> class. Use
		/// this version of the constructor when no filtering or sorting is desired.
		/// </summary>
		/// <param name="cache">The FDO cache</param>
		/// <param name="owningClassId">The ID of the class which will own this virtual
		/// property</param>
		/// <param name="filterInstance">Number used to make filters unique for each main
		/// window</param>
		/// <param name="flidProvider">The method that provides the flid that is used to
		/// retrieve all records.</param>
		/// ------------------------------------------------------------------------------------
		public FilteredSequenceHandler(FdoCache cache, int owningClassId, int filterInstance,
			IFlidProvider flidProvider) :
			this(cache, owningClassId, filterInstance, null, null, flidProvider)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reinitialize the criteria for this handler's filter (ENHANCE: and sort method).
		/// This is typically called when the user chooses a filter (ENHANCE: or sort method)
		/// for which a virtual property has previously been installed. We need to do this
		/// because the filter criteria may have changed or the user may wish to select a
		/// new parameter for the criteria.
		/// </summary>
		/// <param name="fUseExistingCriteria">if set to <c>true</c> using existing criteria.</param>
		/// ------------------------------------------------------------------------------------
		public void Reinitialize(bool fUseExistingCriteria)
		{
			if (m_filter !=  null && !fUseExistingCriteria)
				m_filter.InitCriteria();
			// ENHANCE: It might be possible to avoid clearing the virtual properties for this
			// handler if the filter and sort criteria haven't changed, but this would require us
			// to keep this property up to date even when this handler is not active.
			foreach (int hvoOwner in m_filteredObjectsInCache.Keys)
			{
				m_cache.VwCacheDaAccessor.ClearInfoAbout(hvoOwner,
					VwClearInfoAction.kciaRemoveObjectInfoOnly);
			}
			m_filteredObjectsInCache.Clear();
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the filter associated with this handler.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IFilter Filter
		{
			get { return m_filter; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the list of HVOs of loaded (filtered and/or sorted) objects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal List<int> LoadedObjects
		{
			get { return new List<int>(m_filteredObjectsInCache.Keys); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid provider.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal IFlidProvider FlidProvider
		{
			get { return m_flidProvider; }
		}
		#endregion

		#region static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets virtual property handler corresponding to filter instance.
		/// </summary>
		/// <param name="cache">The FDO Cache</param>
		/// <param name="owningClassId">The ID of the class which will own this virtual
		/// property</param>
		/// <param name="filter">The filter whose virtual handler we want</param>
		/// <param name="filterInstance">Number used to make filters unique for each main
		/// window</param>
		/// <returns>The existing virtual handler for this filter and instance, if any;
		/// otherwise <c>null</c></returns>
		/// <remarks>TODO (TE-3941): pass sort method also (and use it)</remarks>
		/// ------------------------------------------------------------------------------------
		public static FilteredSequenceHandler GetFilterInstance(FdoCache cache, int owningClassId,
			IFilter filter, int filterInstance)
		{
			if (cache == null)
				return null;

			return cache.GetVirtualProperty(cache.GetClassName((uint)owningClassId),
				GetVirtualPropertyName(filter, null, filterInstance)) as FilteredSequenceHandler;
		}
		#endregion

		#region Overridden IVwVirtualHandler Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load filtered/sorted reference sequence for the given object and tag.
		/// </summary>
		/// <param name="hvoPropOwner">HVO of object on which the virtual property is to be created
		/// (e.g., for Consultant Notes, this should be the HVO of a ScrBookAnnotations object)
		/// </param>
		/// <param name="tag">The virtual tag of the filtered/sorted sequence (not a real flid
		/// in the DB)</param>
		/// <param name="ws">Not used</param>
		/// <param name="vwCacheDa">Cache thingy (not an FDO cache, but it better be the same
		/// one our FDO cache refers to or we're in trouble)</param>
		/// ------------------------------------------------------------------------------------
		public override void Load(int hvoPropOwner, int tag, int ws, IVwCacheDa vwCacheDa)
		{
			// Get the records
			FdoVector<ICmObject> collAllItems = GetCollectionOfRecordsToFilter(hvoPropOwner);

			// Evaluate each item in the collection, and build the new filteredList
			List<int> filteredList = new List<int>();	// List of included real indexes
			List<int> filteredHvos = new List<int>();	// List of included HVOs
			for (int i = 0; i < collAllItems.HvoArray.Length; i++)
			{
				int hvoObj = collAllItems.HvoArray[i];
				if (m_filter == null || m_filter.MatchesCriteria(hvoObj))
				{
					filteredList.Add(i);
					filteredHvos.Add(hvoObj);
				}
			}

			int[] hvos = filteredHvos.ToArray();
			vwCacheDa.CacheVecProp(hvoPropOwner, tag, hvos, hvos.Length);
			// remember that we have loaded the virtual property for this object
			m_filteredObjectsInCache[hvoPropOwner] = filteredList;
		}
		#endregion

		#region Other methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the virtual property.
		/// </summary>
		/// <param name="filter">The filter (can be null).</param>
		/// <param name="sortMethod">TODO (TE-3941): use this
		/// The sort method (can be null).</param>
		/// <param name="filterInstance">The filter instance.</param>
		/// ------------------------------------------------------------------------------------
		private static string GetVirtualPropertyName(IFilter filter, ISortSpec sortMethod,
			int filterInstance)
		{
			return ((filter == null || string.IsNullOrEmpty(filter.Name)) ?
				"NoFilter" : filter.Name) + "_" + filterInstance;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index in the virtual property.
		/// </summary>
		/// <param name="hvoPropOwner">The HVO of the CmObject that owns the underlying real
		/// property (and "virtually" owns the virtual property).</param>
		/// <param name="index">The index in the real property.</param>
		/// <returns>
		/// Returns the index in the virtual vector for <paramref name="hvoPropOwner"/>,
		/// or <c>-1</c> if <paramref name="index"/> is not included in the virtual property.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public int GetVirtualIndex(int hvoPropOwner, int index)
		{
			if (m_filter == null)
				return index;

			// Check if vector for hvoPropOwner was loaded previously
			List<int> indexes;
			if (!m_filteredObjectsInCache.TryGetValue(hvoPropOwner, out indexes))
			{
				Load(hvoPropOwner, -1, m_cache.VwCacheDaAccessor);
				indexes = m_filteredObjectsInCache[hvoPropOwner];
			}

			return indexes.IndexOf(index);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return an FdoCollection representing all the records that need to be filtered.
		/// </summary>
		/// <param name="hvoPropOwner">ID of the object that owns the collection to be filtered
		/// </param>
		/// <returns>collection of all items (records) to be filtered</returns>
		/// ------------------------------------------------------------------------------------
		private FdoVector<ICmObject> GetCollectionOfRecordsToFilter(int hvoPropOwner)
		{
			int flid = m_flidProvider.GetFlidForPropOwner(hvoPropOwner);
			if (!m_changeWatchers.ContainsKey(flid))
				m_changeWatchers.Add(flid, new VirtualPropChangeWatcher(flid, this));

			// Return a collection of all items in the vector field
			switch (m_cache.GetFieldType(flid))
			{
				case FieldType.kcptReferenceCollection:
					return new FdoReferenceCollection<ICmObject>(m_cache, hvoPropOwner, flid);
				case FieldType.kcptReferenceSequence:
					return new FdoReferenceSequence<ICmObject>(m_cache, hvoPropOwner, flid);
				case FieldType.kcptOwningSequence:
					return new FdoOwningSequence<ICmObject>(m_cache, hvoPropOwner, flid);
				default:
				case FieldType.kcptOwningCollection:
					return new FdoOwningCollection<ICmObject>(m_cache, hvoPropOwner, flid);
			}
		}
		#endregion
	}
}
