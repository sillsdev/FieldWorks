// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Text;
using SIL.LCModel;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// This alternative implementation is used when restoring a saved list of mopsis.
	/// The main point of it is to be able to restore a list of Imopsis, but NOT actually create the
	/// objects until they are needed for something. Accordingly, it mainly stores ICmObjectIds
	/// rather than CmObjects or HVOs, though the variables are ICmObjectOrIds so we can
	/// retrieve the objects efficiently once they are real.
	/// </summary>
	internal class LazyManyOnePathSortItem : IManyOnePathSortItem
	{
		/// <summary>
		/// The actual item that we are sorting, filtering, etc. by.
		/// </summary>
		ICmObjectOrId m_item;

		/// <summary>
		/// The repository that can interpret ICmObjectOrIds and give ICmObjects.
		/// </summary>
		private ICmObjectRepository m_repo;

		/// <summary>
		/// Array of objects in the path. m_pathObjects[0] is one of the original list items.
		/// m_pathObjects[n+1] is an object in property m_pathFlids[n] of m_pathObjects[n].
		/// m_item is an object in property m_pathFlids[last] of m_pathObjects[last].
		/// </summary>
		ICmObjectOrId[] m_pathObjects;
		int[] m_pathFlids;

		public LazyManyOnePathSortItem(string persistInfo, ICmObjectRepository repo)
		{
			m_repo = repo;
			var chunks = persistInfo.Split(';');
			m_item = ParseGuidRep(repo, chunks[0]);
			if (chunks.Length > 1)
			{
				var pathLen = chunks.Length/2;
				m_pathObjects = new ICmObjectOrId[pathLen];
				m_pathFlids = new int[pathLen];
				for (var i = 0; i < pathLen; i++)
				{
					m_pathFlids[i] = int.Parse(chunks[i*2 + 1]);
					m_pathObjects[i] = ParseGuidRep(repo, chunks[i * 2 + 2]);
				}
			}
		}

		/// <summary>
		/// A representation that can be used to create an equivalent LazyManyOnePathSortItem later.
		/// Keep consistent with the ManyOnePathSortItem PersistData (and our constructor).
		/// </summary>
		public string PersistData(ICmObjectRepository repo)
		{
			var builder = new StringBuilder();
			builder.Append(PersistGuid(m_item));
			if (PathLength > 0)
			{
				for (var i = 0; i < m_pathObjects.Length; i++)
				{
					builder.Append(";");
					builder.Append(m_pathFlids[i]);
					builder.Append(";");
					builder.Append(PersistGuid(m_pathObjects[i]));
				}
			}
			return builder.ToString();
		}

		private static string PersistGuid(ICmObjectOrId item)
		{
			return Convert.ToBase64String(item.Id.Guid.ToByteArray());
		}

		private ICmObjectOrId ParseGuidRep(ICmObjectRepository repo, string chunk)
		{
			var result = repo.GetObjectOrIdWithHvoFromGuid(new Guid(Convert.FromBase64String(chunk)));
			if (result == null)
			{
				throw new InvalidObjectGuidException();
			}
			return result;
		}

		public ICmObject RootObjectUsing(LcmCache cache)
		{
			if (m_pathObjects == null)
			{
				return RealKeyObject();
			}
			var result = m_pathObjects[0].GetObject(m_repo);
			// makes future updates more efficient, if it was an ID.
			// I believe locking is not necessary, since even if two threads update this,
			// both will update it to the same thing.
			m_pathObjects[0] = result;
			return result;
		}

		public int RootObjectHvo
		{
			get
			{
				var objOrId = m_pathObjects == null ? m_item : m_pathObjects[0];
				return m_repo.GetHvoFromObjectOrId(objOrId);
			}
		}

		public int KeyObject => m_repo.GetHvoFromObjectOrId(m_item);

		public ICmObject KeyObjectUsing(LcmCache cache)
		{
			return RealKeyObject();
		}

		private ICmObject RealKeyObject()
		{
			var temp = m_item.GetObject(m_repo);
			m_item = temp; // locking not needed, all threads will update to same thing
			return temp;
		}

		public int PathLength => m_pathObjects?.Length ?? 0;

		public int PathObject(int index)
		{
			return m_pathObjects == null && index == 0
				? KeyObject
				: (index == m_pathObjects.Length ? KeyObject : m_repo.GetHvoFromObjectOrId(m_pathObjects[index]));
		}

		public int PathFlid(int index)
		{
			return m_pathFlids[index];
		}
	}
}