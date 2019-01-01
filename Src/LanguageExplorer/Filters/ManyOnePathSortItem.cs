// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using SIL.LCModel;

namespace LanguageExplorer.Filters
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
		/// Array of objects in the path. m_pathObjects[0] is one of the original list items.
		/// m_pathObjects[n+1] is an object in property m_pathFlids[n] of m_pathObjects[n].
		/// m_hvoItem is an object in property m_pathFlids[last] of m_pathObjects[last].
		/// </summary>
		private int[] m_pathObjects;
		private int[] m_pathFlids;

		/// <summary />
		public ManyOnePathSortItem(int hvoItem, int[] pathObjects, int[] pathFlids)
		{
			Init(hvoItem, pathObjects, pathFlids);
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override string ToString()
		{
			var result = "ManyOnePathSortItem on " + KeyObject;
			if (RootObjectHvo == 0)
			{
				result += " root object null ";
			}
			result += "path ";
			if (m_pathObjects != null)
			{
				result = m_pathObjects.Aggregate(result, (current, hvo) => current + (hvo + " "));
			}
			return result;
		}

		/// <summary>
		/// This is used for some kinds of desperate verification. We shouldn't have databases with
		/// more than 4 million objects for a while.
		/// </summary>
		public static int MaxObjectId => 4000000;

		/// <summary>
		/// Assert that id is valid. (May not catch all problems.)
		/// </summary>
		public static void AssertValidId(int id)
		{
			if (id > 0 || id <= MaxObjectId)
			{
				return;
			}
			throw new Exception("invalid object id detected: " + id);
		}

		/// <summary>
		/// Assert that this object is OK.
		/// </summary>
		public void AssertValid()
		{
			AssertValidId(KeyObject);
			if (RootObjectHvo != 0)
			{
				AssertValidId(RootObjectHvo);
			}
			if (m_pathObjects != null)
			{
				foreach (var hvo in m_pathObjects)
				{
					AssertValidId(hvo);
				}
			}
		}

		/// <summary>
		/// Assert all the MOPSIs in the list are valid.
		/// </summary>
		public static void AssertValidList(ArrayList list)
		{
			foreach (ManyOnePathSortItem item in list)
			{
				item.AssertValid();
			}
		}

		/// <summary>
		/// Assert all the hvos in the array are valid
		/// </summary>
		public static void AssertValidHvoArray(int[] hvos)
		{
			foreach (var hvo in hvos)
			{
				AssertValidId(hvo);
			}
		}

		private void Init(int hvoItem, int[] pathObjects, int[] pathFlids)
		{
			KeyObject = hvoItem;
			// Unless they are both null, they must be arrays of the same length.
			// (Another, nastier, exception will be thrown if just one is null.)
			if ((pathObjects != null || pathFlids != null) && pathObjects.Length != pathFlids.Length)
			{
				throw new Exception("ManyOnePathSortItem arrays must be same length");
			}
			m_pathObjects = pathObjects;
			m_pathFlids = pathFlids;
		}

		/// <summary>
		/// Create one, caching the base CmObject.
		/// </summary>
		public ManyOnePathSortItem(ICmObject item)
		{
			Init(item.Hvo, null, null);
		}

		/// <summary>
		/// The HVO of the object that is the actual list item.
		/// </summary>
		public int KeyObject { get; private set; }

		/// <summary>
		/// Gets the actual KeyCmObject, using the caller-supplied cache.
		/// </summary>
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
			{
				return null;
			}
			ICmObject result;
			cache.ServiceLocator.ObjectRepository.TryGetObject(hvo, out result);
			return result;
		}

		/// <summary>
		/// A shortcut for PathObjects(0), that is, one of the original items from which we generated our path.
		/// </summary>
		public int RootObjectHvo => PathObject(0);

		/// <summary>
		/// One of the objects on the path that leads from an item in the original list
		/// to the KeyObject. As a special case, an index one larger produces the key object
		/// itself.
		/// </summary>
		public int PathObject(int index)
		{
			if (m_pathObjects == null && index == 0)
			{
				return KeyObject;
			}
			return index == m_pathObjects.Length ? KeyObject : m_pathObjects[index];
		}

		/// <summary>
		/// One of the field identifiers on the path that leads from an item in the
		/// original list to the KeyObject.
		/// </summary>
		public int PathFlid(int index)
		{
			return m_pathFlids[index];
		}

		/// <summary>
		/// The number of steps in the path.
		/// </summary>
		public int PathLength => m_pathObjects?.Length ?? 0;

		private static string PersistGuid(Guid guid)
		{
			return Convert.ToBase64String(guid.ToByteArray());
		}

		/// <summary>
		/// A representation that can be used to create an equivalent LazyManyOnePathSortItem.
		/// Keep consistent with the LazyManyOnePathSortItem constructor and PersisData.
		/// </summary>
		public string PersistData(ICmObjectRepository repo)
		{
			var builder = new StringBuilder();
			builder.Append(PersistGuid(repo.GetObject(KeyObject).Guid));
			if (PathLength > 0)
			{
				for (var i = 0; i < m_pathObjects.Length; i++)
				{
					builder.Append(";");
					builder.Append(m_pathFlids[i]);
					builder.Append(";");
					builder.Append(PersistGuid(repo.GetObject(m_pathObjects[i]).Guid));
				}
			}
			return builder.ToString();
		}

		/// <summary>
		/// Write a collection of IManyOneSortItems in a form that can be reconstituted by ReadItems.
		/// </summary>
		public static void WriteItems(ArrayList items, StreamWriter output, ICmObjectRepository repo)
		{
			foreach (IManyOnePathSortItem item in items)
			{
				output.WriteLine(item.PersistData(repo));
			}
			output.Flush();
		}

		/// <summary>
		/// Build a collection of IManyOneSortItems from data written by WriteItems.
		/// </summary>
		public static ArrayList ReadItems(StreamReader input, ICmObjectRepository repo)
		{
			try
			{
				var result = new ArrayList();
				while (!input.EndOfStream)
				{
					result.Add(new LazyManyOnePathSortItem(input.ReadLine(), repo));
				}
				return result;

			}
			// If we find a guid we don't recognize as a valid object, the actual data is somehow
			// not consistent with what we saved, so discard the saved information.
			catch (InvalidObjectGuidException)
			{
				return null;
			}
			// Likewise, if anything goes wrong with reading the file, we'll just rebuild the index.
			catch (IOException)
			{
				return null;
			}
			// Also if the file has been corrupted somehow with invalid Base64 data, we'll rebuild
			// the index.  See FWR-1110.
			catch (FormatException)
			{
				return null;
			}
			// This occurs if an input line has an even number of chunks (semi-colon-separated). LT-11240.
			// This again indicates the file is corrupt and we will just rebuild the index.
			catch (IndexOutOfRangeException)
			{
				return null;
			}
			// If the string representing a GUID that we read from the file doesn't produce
			// a byte array exactly 8 bytes long, we get this (FWR-2890). Similar to FormatException,
			// if the file is corrupt we'll just rebuild the index.
			// Review JohnT: is this a case where we should just catch all exceptions? But then
			// if someone introduces a defect the program may just slow down without our ever
			// realizing why.
			catch (ArgumentException)
			{
				return null;
			}
		}
	}
}