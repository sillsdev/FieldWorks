using System;
using System.Collections;
using System.Diagnostics;
using SIL.FieldWorks.FDO;
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
	public class ManyOnePathSortItem
	{
		/// <summary>
		/// The actual item that we are sorting, filtering, etc. by.
		/// </summary>
		int m_hvoItem;
		/// <summary>
		/// Optionally, the item can store the root CmObject. This is the CmObject that
		/// corresponds to PathObjects(0).
		/// </summary>
		ICmObject m_rootObject;

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
			if (m_rootObject == null)
				result += " root object null ";
			else if (m_rootObject.IsValidObject())
				result += " root object valid ";
			else
				result += " root object invalid ";
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
			if (m_rootObject != null)
				AssertValidId(m_rootObject.Hvo);
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
			m_rootObject = item;
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
		/// Keys the cm object using.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ICmObject KeyCmObjectUsing(FdoCache cache)
		{
			// In the common case that there is no path, we can just return our root item,
			// if we have one.
			if (PathLength == 0 && m_rootObject != null)
				return m_rootObject;
			return CmObject.CreateFromDBObject(cache, m_hvoItem, false);
		}

		/// <summary>
		/// Get the root cm object. This is possible only if the item was initialized with
		/// a RootObject.
		/// </summary>
		public ICmObject KeyCmObject
		{
			get
			{
				if (PathLength == 0 && m_rootObject != null)
					return m_rootObject;
				Debug.Assert(m_rootObject != null);
				return CmObject.CreateFromDBObject(m_rootObject.Cache, m_hvoItem, false);
			}
		}

		/// <summary>
		/// The CmObject corresponding to PathObjects(0).
		/// Note that this may be null if it has not been initialized. This class cannot generate
		/// it from PathObjects(0) because it lacks an FdoCache.
		/// </summary>
		public ICmObject RootObject
		{
			get { return m_rootObject; }
			set
			{
				Debug.Assert(value.Hvo == PathObject(0));
				m_rootObject = value;
			}
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
