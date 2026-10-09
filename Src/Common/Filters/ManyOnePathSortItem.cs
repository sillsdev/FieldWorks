// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using SIL.LCModel;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// A ManyOnePathSortItem stores the information we need to work with an item in a browse view.
	/// This includes the ID of the item, and a path indicating how we got from one of the
	/// root items for the browse view to the item.
	/// This path is empty when sorting by columns containing simple (or very complex)
	/// properties of the original objects, but may be more complex when sorting by columns
	/// containing related objects, especially ones in many:1 relation with the original.
	/// </summary>
	public class ManyOnePathSortItem : IManyOnePathSortItem
	{
		/// <summary>
		/// The actual item that we are sorting, filtering, etc. by.
		/// </summary>
		int m_hvoItem;

		/// <summary>
		/// Array of objects in the path. m_pathObjects[0] is one of the original list items.
		/// m_pathObjects[n+1] is an object in property m_pathFlids[n] of m_pathObjects[n].
		/// m_hvoItem is an object in property m_pathFlids[last] of m_pathObjects[last].
		/// </summary>
		int[] m_pathObjects;
		int[] m_pathFlids;

		/// <summary>
		/// Construct one.
		/// </summary>
		/// <param name="hvoItem"></param>
		/// <param name="pathObjects"></param>
		/// <param name="pathFlids"></param>
		public ManyOnePathSortItem(int hvoItem, int[] pathObjects, int[] pathFlids)
		{
			Init(hvoItem, pathObjects, pathFlids);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			string result = "ManyOnePathSortItem on " + m_hvoItem;
			if (RootObjectHvo == 0)
				result += " root object null ";
			result += "path ";
			if (m_pathObjects != null)
				foreach(int hvo in m_pathObjects)
					result += hvo + " ";
			return result;
		}

		/// <summary>
		/// This is used for some kinds of desperate verification. We shouldn't have databases with
		/// more than 4 million objects for a while.
		/// </summary>
		public static int MaxObjectId
		{
			get { return 4000000; }
		}

		/// <summary>
		/// Assert that id is valid. (May not catch all problems.)
		/// </summary>
		/// <param name="id"></param>
		public static void AssertValidId(int id)
		{
			if (id > 0 || id <= MaxObjectId)
				return;
			throw new Exception("invalid object id detected: " + id);
		}

		/// <summary>
		/// Assert that this object is OK.
		/// </summary>
		public void AssertValid()
		{
			AssertValidId(m_hvoItem);
			if (RootObjectHvo != 0)
				AssertValidId(RootObjectHvo);
			if (m_pathObjects != null)
				foreach (int hvo in m_pathObjects)
					AssertValidId(hvo);
		}

		/// <summary>
		/// Assert all the MOPSIs in the list are valid.
		/// </summary>
		/// <param name="list"></param>
		public static void AssertValidList(ArrayList list)
		{
			foreach (ManyOnePathSortItem item in list)
				item.AssertValid();
		}

		/// <summary>
		/// Assert all the hvos in the array are valid
		/// </summary>
		/// <param name="hvos"></param>
		public static void AssertValidHvoArray(int[] hvos)
		{
			foreach (int hvo in hvos)
				AssertValidId(hvo);
		}

		void Init(int hvoItem, int[] pathObjects, int[] pathFlids)
		{
			m_hvoItem = hvoItem;
			// Unless they are both null, they must be arrays of the same length.
			// (Another, nastier, exception will be thrown if just one is null.)
			if ((pathObjects != null || pathFlids != null)
				&& pathObjects.Length != pathFlids.Length)
			{
				throw new Exception("ManyOnePathSortItem arrays must be same length");
			}
			m_pathObjects = pathObjects;
			m_pathFlids = pathFlids;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create one, caching the base CmObject.
		/// </summary>
		/// <param name="item">The item.</param>
		/// ------------------------------------------------------------------------------------
		public ManyOnePathSortItem(ICmObject item)
		{
			Init(item.Hvo, null, null);
		}

		/// <summary>
		/// The HVO of the object that is the actual list item.
		/// </summary>
		public int KeyObject
		{
			get { return m_hvoItem; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the actual KeyCmObject, using the caller-supplied cache.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ICmObject KeyObjectUsing(LcmCache cache)
		{
			return cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(KeyObject);
		}

		/// <summary>
		/// Note that this may be null if it has not been initialized or the object has been deleted. This class cannot generate
		/// it from PathObjects(0) because it lacks an LcmCache.
		/// </summary>
		public ICmObject RootObjectUsing(LcmCache cache)
		{
			var hvo = RootObjectHvo;
			if (hvo == 0)
				return null;
			ICmObject result;
			cache.ServiceLocator.ObjectRepository.TryGetObject(hvo, out result);
			return result;
		}

		/// <summary>
		/// A shortcut for PathObjects(0), that is, one of the original items from which we generated our path.
		/// </summary>
		public int RootObjectHvo
		{
			get { return PathObject(0); }
		}

		/// <summary>
		/// One of the objects on the path that leads from an item in the original list
		/// to the KeyObject. As a special case, an index one larger produces the key object
		/// itself.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public int PathObject(int index)
		{
			if (m_pathObjects == null && index == 0)
				return KeyObject;
			if (index == m_pathObjects.Length)
				return KeyObject;
			return m_pathObjects[index];
		}

		/// <summary>
		/// One of the field identifiers on the path that leads from an item in the
		/// original list to the KeyObject.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public int PathFlid(int index)
		{
			return m_pathFlids[index];
		}

		/// <summary>
		/// The number of steps in the path.
		/// </summary>
		public int PathLength
		{
			get
			{
				if (m_pathObjects == null)
					return 0;
				return m_pathObjects.Length;
			}
		}
	}
}
