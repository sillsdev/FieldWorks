// --------------------------------------------------------------------------------------------
// Copyright (C) 2008-2009 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: CmObjectIdentityMap.cs
// Responsibility: Randy Regnier
// Last reviewed: never
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// This class implements the "Identity Map" pattern,
	/// which keeps track of all CmObject instances that have been
	/// retrieved from the data store, or which have been newly created.
	///
	/// The purpose of this Identity Map is to instantiate, one, and only one,
	/// CmObject per Guid identifier. Thus, anyone who asks for one with some Guid,
	/// gets the same exact object.
	/// </summary>
	internal sealed class IdentityMap : ICmObjectIdFactory, IDisposable
	{
		/// <summary>This Dictionary is needed to get at the CmObject, when its HVO is given.
		/// The target is usually the actual CmObject, but occasionally, especially for large
		/// browse lists, it is useful to assign HVOs for objects that haven't been fluffed.</summary>
		private Dictionary<int, ICmObjectOrId> m_extantObjectsByHvo = new Dictionary<int, ICmObjectOrId>();
		/// <summary>
		/// These are all of the surrogates (or their real objects) in the system.
		/// Usually a surrogate is replaced with the real object when it is reconstituted (unless RetainSurrogates is set true).
		/// Usually, therefore, a remaining surrogate does not have its Object set; but there are exceptions when
		/// RetainSurrogates is true.
		/// It is also possible (though currently unusual) for the value to be a CmObjectId. This is done when we need
		/// to record a canonical version of a particular ID, but do not (yet) have either a corresponding surrogate
		/// or a corresponding CmObject. These should generally be filtered out when returning collections derived from
		/// the dictionary or testing membership in it.
		/// Because all three classes (CmObject, CmObjectSurrogate, and CmObjectId) know about a CmObjectId, the dictionary
		/// can serve a secondary purpose in ensuring that we only make one instance of CmObjectId for each GUID (for a
		/// given database). This is important to conserve memory, one of the main purposes of CmObjectId. It also conserves
		/// significant memory (12M, for a middling-large project) to violate the SRP in a controlled way here and use
		/// this very large dictionary for two purposes.
		/// </summary>
		private Dictionary<ICmObjectId, ICmObjectOrSurrogate> m_IdentityMap = new Dictionary<ICmObjectId, ICmObjectOrSurrogate>();
		private readonly Dictionary<int, HashSet<ICmObjectOrSurrogate>> m_clsid2Surrogates = new Dictionary<int, HashSet<ICmObjectOrSurrogate>>();
		private readonly IFwMetaDataCacheManaged m_mdc;

		/// <summary>synchronization root</summary>
		private readonly object m_syncRoot = new object();

		private int m_nextHvo = 1;

		/// <summary>
		/// Constructor
		/// </summary>
		internal IdentityMap(IFwMetaDataCacheManaged mdc)
		{
			if (mdc == null) throw new ArgumentNullException("mdc");

			m_mdc = mdc;
			foreach (var clsid in mdc.GetClassIds())
				m_clsid2Surrogates.Add(clsid, new HashSet<ICmObjectOrSurrogate>(new ObjectSurrogateEquater()));
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~IdentityMap()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed
		{
			get;
			private set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases the memory stored in the IdentityMap. This is called automatically from
		/// StructureMap when the StructureMap container is disposed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		private void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().ToString() + " *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				if (m_extantObjectsByHvo != null)
					m_extantObjectsByHvo.Clear();
				if (m_IdentityMap != null)
					m_IdentityMap.Clear();
				m_clsid2Surrogates.Clear();
			}
			m_extantObjectsByHvo = null;
			m_IdentityMap = null;
			IsDisposed = true;
		}
		#endregion

		/// <summary>
		/// Gets the synchronization root. This is the object that should be
		/// used for all locking in this identity map. Used for m_extantObjectsByHvo
		/// and m_ownerlessObjects, since it can change during FDO reads.
		/// WARNING: it is dangerous to try to obtain the object of a surrogate while
		/// holding this lock, since fluffing a surrogate requires the identity map synclock.
		/// Deadlocks are possible.
		/// </summary>
		/// <value>The synchronization root.</value>
		private object SyncRoot
		{
			get
			{
				return m_syncRoot;
			}
		}

		/// <summary>
		/// Get the next higher Hvo.
		/// </summary>
		/// <remarks>
		/// This property is not to be called except on object creation, or when setting up an HVO
		/// for a CmObjectIdWithHvo.
		/// The objects can be newly created or re-created,
		/// as is the case for loading the XML data.
		/// </remarks>
		public int GetNextRealHvo()
		{
			return m_nextHvo++;
		}

		/// <summary>
		/// Clear out all internal caches, since a data migration is in process,
		/// and the contents will all be replaced soon.
		/// </summary>
		internal void Clear()
		{
			m_extantObjectsByHvo.Clear();
			m_IdentityMap.Clear();
			// No. We want to keep the class name keys.
			//m_clsid2Surrogates.Clear();
			foreach (var classSet in m_clsid2Surrogates.Values)
				classSet.Clear();
		}

		/// <summary>
		/// Get a copy of the Set of ICmObjectOrSurrogate instances of the given classname.
		/// </summary>
		internal IEnumerable<ICmObjectOrSurrogate> AllObjectsOrSurrogates(string classname)
		{
			return AllObjectsOrSurrogates(m_mdc.GetClassId(classname));
		}

		/// <summary>
		/// Enumerate over all known objects (or their surrogates).
		/// </summary>
		internal IEnumerable<ICmObjectOrSurrogate> AllObjectsOrSurrogates()
		{
			IEnumerable<ICmObjectOrSurrogate> objectsOrSurrogates;
			lock (SyncRoot)
				objectsOrSurrogates = m_IdentityMap.Values.ToArray();

			return objectsOrSurrogates.Where(item => !(item is CmObjectId));
		}

		/// <summary>
		/// Get a copy of the Set of ICmObjectOrSurrogate instances of the given classname.
		/// </summary>
		/// <remarks>
		/// A clsid of int.MaxValue means to return all surrogates of objects
		/// of all classes.
		/// </remarks>
		internal IEnumerable<ICmObjectOrSurrogate> AllObjectsOrSurrogates(int clsid)
		{
			if (clsid == int.MaxValue)
			{
				foreach (var item in AllObjectsOrSurrogates())
					yield return item;
			}
			else
			{

				foreach (int subclassid in AllSubclasses(clsid))
				{
					IEnumerable<ICmObjectOrSurrogate> objectsOrSurrogates;
					lock (SyncRoot)
						objectsOrSurrogates = m_clsid2Surrogates[subclassid].ToArray();
					foreach (ICmObjectOrSurrogate item in objectsOrSurrogates)
						yield return item;
				}
			}
		}

		IEnumerable<int> AllSubclasses(int clsid)
		{
			yield return clsid;
			foreach (var subclassId in m_mdc.GetDirectSubclasses(clsid))
				foreach (var item in AllSubclasses(subclassId))
					yield return item;
		}

		/// <summary>
		/// Return true if we know about this ID.
		/// </summary>
		internal bool HasObject(int hvo)
		{
			lock (SyncRoot)
				return m_extantObjectsByHvo.ContainsKey(hvo);
		}

		/// <summary>
		/// Return true if we know about this ID.
		/// </summary>
		internal bool HasObject(Guid guid)
		{
			lock (SyncRoot)
			{
				ICmObjectOrSurrogate item;
				return m_IdentityMap.TryGetValue(FromGuid(guid), out item) && !(item is CmObjectId);
			}
		}

		/// <summary>
		/// Get the ICmObject with the given HVO.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		internal ICmObject GetObject(int hvo)
		{
			ICmObject obj;
			ICmObjectOrSurrogate objOrSurr;
			lock (SyncRoot)
			{
				// Optimize (JohnT): could we first test this outside the syncroot?
				// I don't think so, because one thread (say) growing the dictionary while another tries a lookup
				// is probably bad.
				ICmObjectOrId objOrId;
				var hvoPresent = m_extantObjectsByHvo.TryGetValue(hvo, out objOrId);
				if(!hvoPresent)
				{
					// The purpose of the following section is to try and display as much useful information
					// as we can think of in the case of an unexpected crash. Many crashes which source from
					// this code are irreproducible or require extremely picky steps.

					// display information about the hvo we looked up and the next available hvo.
					// If the object is newly created it hints at a race condition.
					var message = String.Format("Internal timing or data error [Unable to find hvo {0} in the object dictionary. {1} is the next available hvo]",
													hvo, m_nextHvo);
					throw new KeyNotFoundException(message);
				}

				// It's tempting to put this logic in a method of ICmObjectOrIdInternal,
				// with different implemetations in the concrete subclasses, but that
				// doesn't fit well with updating the map if we do have to go get a real
				// object. We could make a GetObject(identitymap) on both classes, but one
				// would ignore the map, while the other would have to modify it, violating
				// command/query separation even worse than this method, and would
				// be dangerous to use outside a SyncRoot lock. On the whole
				// I think this is less messy.
				obj = objOrId as ICmObject;
				if (obj != null)
					return obj;
				objOrSurr = GetObjectOrSurrogate((ICmObjectId) objOrId);
			}
			// Must be outside the lock to prevent deadlocks, since it can fluff a surrogate.
			obj = objOrSurr.Object;
			lock (SyncRoot)
			{
				m_extantObjectsByHvo[hvo] = obj; // find the real one directly next time
				return obj;
			}
		}

		/// <summary>
		/// If the identity map for this ID contains a CmObject, return it.
		/// </summary>
		internal ICmObject GetObjectIfFluffed(ICmObjectId id)
		{
			lock (SyncRoot)
			{
				ICmObjectOrSurrogate result;
				if (m_IdentityMap.TryGetValue(id, out result) && result is ICmObject)
					return (ICmObject) result;
			}
			return null;
		}

		/// <summary>
		/// Get the ICmObject with the given Guid.
		/// </summary>
		/// <param name="guid"></param>
		/// <returns>The ICmObject with the given Guid, or null, if not in Identity Map.</returns>
		internal ICmObject GetObject(Guid guid)
		{
			// This will ensure all Object info is in this map,
			// if it has to reconstitute it, it will call RegisterSurrogate,
			// which will make sure it is in all the correct places in this map.
			ICmObjectOrSurrogate objOrSurr;
			lock (SyncRoot)
				objOrSurr = GetObjectOrSurrogate(FromGuid(guid));
			return objOrSurr.Object;
		}

		private ICmObjectId FromGuid(Guid guid)
		{
			return ((ICmObjectIdFactory)this).FromGuid(guid);
		}

		/// <summary>
		/// Get the ICmObject with the given Guid.
		/// </summary>
		/// <param name="guid"></param>
		/// <returns>The ICmObject with the given Guid, or null, if not in Identity Map.</returns>
		internal ICmObject GetObject(ICmObjectId guid)
		{
			// This will ensure all Object info is in this map,
			// if it has to reconstitute it, it will call RegisterSurrogate,
			// which will make sure it is in all the correct places in this map.
			ICmObjectOrSurrogate objOrSurr;
			lock (SyncRoot)
				objOrSurr = GetObjectOrSurrogate(guid);
			return objOrSurr.Object;
		}

		/// <summary>
		/// Attempts to get the ICmObject with the given Guid.
		/// </summary>
		/// <param name="hvo">The unique id of an object.</param>
		/// <param name="obj">The ICmObject with the given Guid, or null, if not in Identity Map.</param>
		/// <returns>True if the object was found, false otherwise</returns>
		internal bool TryGetObject(int hvo, out ICmObject obj)
		{
			ICmObjectOrId objOrId;
			bool found;
			lock (SyncRoot)
			{
				found = m_extantObjectsByHvo.TryGetValue(hvo, out objOrId);
			}
			if (found)
			{
				obj = objOrId as ICmObject;
				if (obj != null)
					return true;
				if (TryGetObject(objOrId.Id.Guid, out obj))
					return true;
			}
			obj = null;
			return false;
		}

		/// <summary>
		/// Attempts to get the ICmObject with the given Guid.
		/// </summary>
		/// <param name="guid">The GUID that identifies a CmObject.</param>
		/// <param name="obj">The ICmObject with the given Guid, or null, if not in Identity Map.</param>
		/// <returns>True if the object was found, false otherwise</returns>
		internal bool TryGetObject(Guid guid, out ICmObject obj)
		{
			ICmObjectOrSurrogate objOrSur;
			bool fGotOne;
			lock (SyncRoot)
			{
				fGotOne = m_IdentityMap.TryGetValue(FromGuid(guid), out objOrSur) && !(objOrSur is CmObjectId);
			}
			if (fGotOne)
			{
				// Getting the surrogate's object must be done outside the SyncRoot lock, or we can deadlock,
				// as fluffing the surrogate needs the lock on the identity map.
				obj = objOrSur.Object;
				return true;
			}
			obj = null;
			return false;
		}

		/// <summary>
		/// Get the ICmObjectSurrogate with the given Guid.
		/// </summary>
		/// <param name="guid"></param>
		/// <returns>The ICmObjectSurrogate with the given Guid.</returns>
		/// <exception cref="KeyNotFoundException">If the guid is not in the map</exception>
		internal ICmObjectOrSurrogate GetObjectOrSurrogate(ICmObjectId guid)
		{
			var result = m_IdentityMap[guid];
			if (result is CmObjectId)
				throw new KeyNotFoundException("Key " + guid.Guid + " not found in identity map (actually just an ID is present)");
			return result;
		}

		/// <summary>
		/// Get the ICmObjectSurrogate for the given CmObjectId.
		/// </summary>
		internal ICmObjectSurrogate GetSurrogate(ICmObjectId id)
		{
			lock (SyncRoot)
				return (ICmObjectSurrogate)GetObjectOrSurrogate(id);
		}
		/// <summary>
		/// Get the ICmObjectSurrogate for the given CmObject.
		/// </summary>
		internal ICmObjectSurrogate GetSurrogate(ICmObject obj)
		{
			lock (SyncRoot)
				return (ICmObjectSurrogate)GetObjectOrSurrogate(obj.Id);
		}

		/// <summary>
		/// Get the ICmObjectSurrogate for the given guid.
		/// </summary>
		internal ICmObjectSurrogate GetSurrogate(Guid guid)
		{
			lock (SyncRoot)
				return (ICmObjectSurrogate)GetObjectOrSurrogate(FromGuid(guid));
		}

		private void RegisterObject(ICmObject obj)
		{
			m_IdentityMap[obj.Id] = (ICmObjectOrSurrogate)obj;
			m_clsid2Surrogates[obj.ClassID].Add((ICmObjectOrSurrogate)obj);
		}

		private void RegisterSurrogate(ICmObjectSurrogate surrogate)
		{
			m_IdentityMap[surrogate.Id] = surrogate;
			m_clsid2Surrogates[m_mdc.GetClassId(surrogate.Classname)].Add(surrogate);
		}

		/// <summary>
		/// Register an inactive (as no object) surrogate from the data store.
		/// </summary>
		internal void RegisterInactiveSurrogate(ICmObjectSurrogate surrogate)
		{
			if (surrogate == null) throw new ArgumentNullException("surrogate");
			if (surrogate.HasObject)
				throw new InvalidOperationException("Has already been reconstituted.");

			RegisterSurrogate(surrogate);
		}

		/// <summary>
		/// Register an inactive (as no object) surrogate from the data store.
		/// The data needs to be converted, so do the bare minimum registration.
		/// </summary>
		internal void RegisterSurrogateForConversion(ICmObjectSurrogate surrogate)
		{
			if (surrogate == null) throw new ArgumentNullException("surrogate");
			if (surrogate.HasObject)
				throw new InvalidOperationException("Has already been reconstituted.");

			m_IdentityMap[surrogate.Id] = surrogate;
		}

		/// <summary>
		/// Add all surrogates to m_clsid2Surrogates, now that their class names have stabilized.
		/// </summary>
		/// <param name="goners">
		/// Set of surrogates that were removed in data migration.
		/// These will be removed, not registered.
		/// </param>
		internal void FinishRegisteringAfterDataMigration(HashSet<ICmObjectId> goners)
		{
			if (goners == null) throw new ArgumentNullException("goners");

			foreach (var surrogate in m_IdentityMap.Values)
			{
				if (goners.Contains(surrogate.Id) || surrogate is CmObjectId) continue; // Skip it.

				m_clsid2Surrogates[m_mdc.GetClassId(surrogate.Classname)].Add(surrogate);
			}
		}

		/// <summary>
		/// Remove deleted objects from the identity map.
		/// </summary>
		/// <param name="goners"></param>
		internal void RemoveGoners(HashSet<ICmObjectId> goners)
		{
			foreach (var goner in goners)
			{
				var didRemoveIt = m_IdentityMap.Remove(goner);
			}
		}

		/// <summary>
		/// Register an activated (has reconstituted object) surrogate from the data store.
		/// </summary>
		internal void RegisterActivatedSurrogate(ICmObjectSurrogate surrogate)
		{
			if (surrogate == null) throw new ArgumentNullException("surrogate");
			if (!surrogate.HasObject)
				throw new InvalidOperationException("Has not been reconstituted.");

			ICmObject obj = surrogate.Object;
			var realObj = (ICmObjectOrSurrogate) obj;
			lock (SyncRoot)
			{
				if (!m_IdentityMap.ContainsKey(surrogate.Id))
					throw new InvalidOperationException("Has not been registered.");

				// Replace the surrogate with the real object in all the places that we store it so it can be
				// garbage collected.
				m_IdentityMap[surrogate.Id] = realObj;
				var occurrenceSet = m_clsid2Surrogates[m_mdc.GetClassId(surrogate.Classname)];
				// These objects are considered equal by the set Equater; but calling Add on a set
				// does not replace the existing object with an equal one. So we must remove and re-add.
				occurrenceSet.Remove(surrogate);
				occurrenceSet.Add(realObj);

				RegisterObjectHvo(obj);
			}
		}

		/// <summary>
		/// Register a newly created object.
		/// </summary>
		internal void RegisterObjectAsCreated(ICmObject newby)
		{
			RegisterObject(newby);
			RegisterObjectHvo(newby);
		}

		private void RegisterObjectHvo(ICmObject obj)
		{
			((ICmObjectInternal)obj).CheckBasicObjectState();

			// Don't use Add, it's now possible that we already registered a link from the HVO
			// to a CmObjectIdWithHvo.
			m_extantObjectsByHvo[obj.Hvo] = obj;
		}

		/// <summary>
		/// Remove an object from the Identity Map.
		/// </summary>
		/// <param name="goner"></param>
		internal void UnregisterObject(ICmObject goner)
		{
			m_extantObjectsByHvo.Remove(goner.Hvo);
			m_IdentityMap.Remove(goner.Id);
			HashSet<ICmObjectOrSurrogate> surrogates;
			if (m_clsid2Surrogates.TryGetValue(goner.ClassID, out surrogates))
				surrogates.Remove((ICmObjectOrSurrogate)goner);
		}

		/// <summary>
		/// Reregisters an object that has been previously unregistered.
		/// </summary>
		/// <param name="obj">The obj.</param>
		internal void ReregisterObject(ICmObject obj)
		{
			m_extantObjectsByHvo[obj.Hvo] = obj;
			m_IdentityMap[obj.Id] = (ICmObjectOrSurrogate)obj;
			HashSet<ICmObjectOrSurrogate> surrogates;
			if (m_clsid2Surrogates.TryGetValue(obj.ClassID, out surrogates))
				surrogates.Add((ICmObjectOrSurrogate)obj);
		}

		/// <summary>
		/// Finish removing some data (FdoData stuff).
		/// </summary>
		internal void FinishUnregisteringObjects()
		{
		}

		#region ICmObjectIdFactory Members

		ICmObjectId ICmObjectIdFactory.FromBase64String(string guid)
		{
			return CmObjectId.FromGuid(GuidServices.GetGuid(guid), this);
		}

		ICmObjectId ICmObjectIdFactory.FromGuid(Guid guid)
		{
			return CmObjectId.FromGuid(guid, this);
		}

		/// <summary>
		/// If we already know an HVO for this ID, answer it. Otherwise, assign a new one.
		/// Don't set up the association, because it will be established by assigning the HVO to
		/// a new (or restored) CmObject, and registering that.
		/// Assume we do NOT already have a CmObject for this ID; this method is reserved
		/// for use in the process of creating CmObjects.
		/// </summary>
		internal int GetOrAssignHvoFor(ICmObjectId id)
		{
			lock (SyncRoot)
			{
				ICmObjectOrSurrogate canonicalItem = m_IdentityMap[id];
				if (canonicalItem is CmObjectSurrogate)
				{
					ICmObjectId canonicalId = ((CmObjectSurrogate) canonicalItem).Id;
					if (canonicalId is CmObjectIdWithHvo)
						return ((CmObjectIdWithHvo) canonicalId).Hvo;
				}
				Debug.Assert(!(canonicalItem is ICmObject));
				if (canonicalItem is CmObjectIdWithHvo)
					return ((CmObjectIdWithHvo) canonicalItem).Hvo;
				return GetNextRealHvo();
			}
		}

		/// <summary>
		/// This method should be used when it is desirable to have HVOs associated with
		/// Guids for objects which may not yet have been fluffed up. The ObjectOrId may be
		/// passed to GetHvoFromObjectOrId to get an HVO; anything that actually uses
		/// the HVO will result in the object being fluffed, but that can be delayed (e.g.,
		/// when persisting a pre-sorted list of guids).
		/// It will return null if the guid does not correspond to a real object (that is,
		/// for success the identity map must contain either a CmObject or a surrogate, not
		/// just a CmObjectId.)
		/// </summary>
		internal ICmObjectOrId GetObjectOrIdWithHvoFromGuid(Guid guid)
		{
			ICmObjectOrSurrogate canonicalItem;
			lock (SyncRoot)
			{
				var oldCanonicalId = CmObjectId.FromGuid(guid, this);
				canonicalItem = m_IdentityMap[oldCanonicalId];
			}
			if (canonicalItem is ICmObject)
				return (ICmObject)canonicalItem;
			if (canonicalItem is CmObjectSurrogate)
				return ((CmObjectSurrogate) canonicalItem).ObjectOrIdWithHvo;
			// If it's neither, it doesn't map to a valid object, and we don't want to
			// assign it an HVO.
			return null;
		}

		internal CmObjectIdWithHvo CreateObjectIdWithHvo(Guid guid)
		{
			lock (SyncRoot)
			{
				var newCanonicalId = new CmObjectIdWithHvo(guid, GetNextRealHvo());
				m_extantObjectsByHvo[newCanonicalId.Hvo] = newCanonicalId;
				return newCanonicalId;
			}
		}

		internal ICmObjectId GetCanonicalID(CmObjectId candidate)
		{
			lock (SyncRoot)
			{
				ICmObjectOrSurrogate existing;
				if (!m_IdentityMap.TryGetValue(candidate, out existing))
				{
					m_IdentityMap[candidate] = candidate;
					existing = candidate;
				}
				return existing.Id;
			}
		}

		ICmObjectId ICmObjectIdFactory.NewId()
		{
			return CmObjectId.FromGuid(Guid.NewGuid(), this);
		}

		#endregion

		/// <summary>
		/// Adjust the capacity of the dictionaries etc. that depend on the count of objects.
		/// The given number will typically be added.
		/// This is basically used to set the initial size of the dictionary when a new backend provider
		/// is created. Copying the old dictionary is for paranoia. It would be better to create it the
		/// right size to begin with, but the code paths are very convoluted, as the identity map is
		/// created by the DI container.
		/// </summary>
		internal void ExpectAdditionalObjects(int expectedGrowth)
		{
			m_IdentityMap = GrowDictionary(m_IdentityMap, expectedGrowth);
			m_extantObjectsByHvo = GrowDictionary(m_extantObjectsByHvo, expectedGrowth);
		}


		/// <summary>
		/// We expect to add about expectedGrowth items to the input dictionary.
		/// Answer a substitute (or perhaps the same) dictionary, which will typically replace the old one,
		/// containing the current contents of the old one but large enough to hold the new items.
		///
		/// </summary>
		private static Dictionary<TKey, TValue> GrowDictionary<TKey, TValue>(Dictionary<TKey, TValue> input, int expectedGrowth)
		{
			// If we often call this without increasing the size much, we may want to optimize and just answer
			// the original if the new size is not much different (say, less than double). However, currently
			// we typically call it with empty input and very large expectedGrowth.
			var result = new Dictionary<TKey, TValue>(input.Count + expectedGrowth);
			foreach (var item in input)
				result.Add(item.Key, item.Value);
			return result;
		}
	}
}
