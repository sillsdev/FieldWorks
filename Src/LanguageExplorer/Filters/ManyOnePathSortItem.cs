// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
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
	internal sealed class ManyOnePathSortItem : IManyOnePathSortItem
	{
		/// <summary>
		/// Array of objects in the path. _pathObjects[0] is one of the original list items.
		/// m_pathObjects[n+1] is an object in property _pathFlids[n] of _pathObjects[n].
		/// m_hvoItem is an object in property _pathFlids[last] of _pathObjects[last].
		/// </summary>
		private readonly int[] _pathObjects;
		private readonly int[] _pathFlids;
		private readonly int _hvoItem;
		private IManyOnePathSortItem AsInterface => this;

		/// <summary />
		internal ManyOnePathSortItem(int hvoItem, int[] pathObjects = null, int[] pathFlids = null)
		{
			_hvoItem = hvoItem;
			// Unless they are both null, they must be arrays of the same length.
			// (Another, nastier, exception will be thrown if just one is null.)
			if ((pathObjects?.Length ?? 0) != (pathFlids?.Length ?? 0))
			{
				throw new Exception("ManyOnePathSortItem arrays must be same length");
			}
			_pathObjects = pathObjects;
			_pathFlids = pathFlids;
		}

		/// <summary>
		/// Create one, caching the base CmObject.
		/// </summary>
		internal ManyOnePathSortItem(ICmObject item)
			: this(item.Hvo)
		{
		}

		#region IManyOnePathSortItem implementation

		/// <summary>
		/// Note that this may be null if it has not been initialized or the object has been deleted. This class cannot generate
		/// it from PathObjects(0) because it lacks an LcmCache.
		/// </summary>
		ICmObject IManyOnePathSortItem.RootObjectUsing(LcmCache cache)
		{
			var hvo = AsInterface.RootObjectHvo;
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
		int IManyOnePathSortItem.RootObjectHvo => AsInterface.PathObject(0);

		/// <summary>
		/// The HVO of the object that is the actual list item.
		/// </summary>
		int IManyOnePathSortItem.KeyObject => _hvoItem;

		/// <summary>
		/// Gets the actual KeyCmObject, using the caller-supplied cache.
		/// </summary>
		ICmObject IManyOnePathSortItem.KeyObjectUsing(LcmCache cache)
		{
			return cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(_hvoItem);
		}

		/// <summary>
		/// The number of steps in the path.
		/// </summary>
		int IManyOnePathSortItem.PathLength => _pathObjects?.Length ?? 0;

		/// <summary>
		/// One of the objects on the path that leads from an item in the original list
		/// to the HvoItem. As a special case, an index one larger (e.g.: length of _pathObjects) produces the key object
		/// itself.
		/// </summary>
		int IManyOnePathSortItem.PathObject(int index)
		{
			return _pathObjects == null && index == 0 ? _hvoItem : index == _pathObjects.Length ? _hvoItem : _pathObjects[index];
		}

		/// <summary>
		/// One of the field identifiers on the path that leads from an item in the
		/// original list to the KeyObject.
		/// </summary>
		int IManyOnePathSortItem.PathFlid(int index)
		{
			return _pathFlids[index];
		}

		/// <summary>
		/// A representation that can be used to create an equivalent LazyManyOnePathSortItem.
		/// Keep consistent with the LazyManyOnePathSortItem constructor and PersistData.
		/// </summary>
		string IManyOnePathSortItem.PersistData(ICmObjectRepository repo)
		{
			var builder = new StringBuilder();
			builder.Append(PersistGuid(repo.GetObject(_hvoItem).Guid));
			if (AsInterface.PathLength > 0)
			{
				for (var i = 0; i < _pathObjects.Length; i++)
				{
					builder.Append(";");
					builder.Append(_pathFlids[i]);
					builder.Append(";");
					builder.Append(PersistGuid(repo.GetObject(_pathObjects[i]).Guid));
				}
			}
			return builder.ToString();
		}

		#endregion

		private static string PersistGuid(Guid guid)
		{
			return Convert.ToBase64String(guid.ToByteArray());
		}

		/// <summary>
		/// Write a collection of IManyOneSortItems in a form that can be reconstituted by ReadItems.
		/// </summary>
		internal static void WriteItems(List<IManyOnePathSortItem> items, StreamWriter output, ICmObjectRepository repo)
		{
			foreach (var item in items)
			{
				output.WriteLine(item.PersistData(repo));
			}
			output.Flush();
		}

		/// <summary>
		/// Build a collection of IManyOneSortItems from data written by WriteItems.
		/// </summary>
		internal static IReadOnlyList<IManyOnePathSortItem> ReadItems(StreamReader input, ICmObjectRepository repo)
		{
			try
			{
				var result = new List<IManyOnePathSortItem>();
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

		/// <summary>
		/// Returns a <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.
		/// </summary>
		public override string ToString()
		{
			var result = "ManyOnePathSortItem on " + _hvoItem;
			if (AsInterface.RootObjectHvo == 0)
			{
				result += " root object null ";
			}
			result += "path ";
			if (_pathObjects != null)
			{
				result = _pathObjects.Aggregate(result, (current, hvo) => current + (hvo + " "));
			}
			return result;
		}

		/// <summary>
		/// This alternative implementation is used when restoring a saved list of mopsis.
		/// The main point of it is to be able to restore a list of Imopsis, but NOT actually create the
		/// objects until they are needed for something. Accordingly, it mainly stores ICmObjectIds
		/// rather than CmObjects or HVOs, though the variables are ICmObjectOrIds so we can
		/// retrieve the objects efficiently once they are real.
		/// </summary>
		private sealed class LazyManyOnePathSortItem : IManyOnePathSortItem
		{
			/// <summary>
			/// The actual item that we are sorting, filtering, etc. by.
			/// </summary>
			private ICmObjectOrId _item;
			/// <summary>
			/// The repository that can interpret ICmObjectOrIds and give ICmObjects.
			/// </summary>
			private readonly ICmObjectRepository _repo;
			/// <summary>
			/// Array of objects in the path. _pathObjects[0] is one of the original list items.
			/// _pathObjects[n+1] is an object in property _pathFlids[n] of _pathObjects[n].
			/// _item is an object in property _pathFlids[last] of _pathObjects[last].
			/// </summary>
			private readonly ICmObjectOrId[] _pathObjects;
			private readonly int[] _pathFlids;
			private IManyOnePathSortItem AsInterface => this;

			internal LazyManyOnePathSortItem(string persistInfo, ICmObjectRepository repo)
			{
				_repo = repo;
				var chunks = persistInfo.Split(';');
				_item = ParseGuidRep(repo, chunks[0]);
				if (chunks.Length > 1)
				{
					var pathLen = chunks.Length / 2;
					_pathObjects = new ICmObjectOrId[pathLen];
					_pathFlids = new int[pathLen];
					for (var i = 0; i < pathLen; i++)
					{
						_pathFlids[i] = int.Parse(chunks[i * 2 + 1]);
						_pathObjects[i] = ParseGuidRep(repo, chunks[i * 2 + 2]);
					}
				}
			}

			#region IManyOnePathSortItem implementation

			ICmObject IManyOnePathSortItem.RootObjectUsing(LcmCache cache)
			{
				if (_pathObjects == null)
				{
					return RealKeyObject();
				}
				var result = _pathObjects[0].GetObject(_repo);
				// makes future updates more efficient, if it was an ID.
				// I believe locking is not necessary, since even if two threads update this,
				// both will update it to the same thing.
				_pathObjects[0] = result;
				return result;
			}

			int IManyOnePathSortItem.RootObjectHvo
			{
				get
				{
					var objOrId = _pathObjects == null ? _item : _pathObjects[0];
					return _repo.GetHvoFromObjectOrId(objOrId);
				}
			}

			int IManyOnePathSortItem.KeyObject => _repo.GetHvoFromObjectOrId(_item);

			ICmObject IManyOnePathSortItem.KeyObjectUsing(LcmCache cache)
			{
				return RealKeyObject();
			}

			int IManyOnePathSortItem.PathLength => _pathObjects?.Length ?? 0;

			int IManyOnePathSortItem.PathObject(int index)
			{
				return _pathObjects == null && index == 0 ? AsInterface.KeyObject : index == _pathObjects.Length ? AsInterface.KeyObject : _repo.GetHvoFromObjectOrId(_pathObjects[index]);
			}

			int IManyOnePathSortItem.PathFlid(int index)
			{
				return _pathFlids[index];
			}

			// Add above.
			/// <summary>
			/// A representation that can be used to create an equivalent LazyManyOnePathSortItem later.
			/// Keep consistent with the ManyOnePathSortItem PersistData (and our constructor).
			/// </summary>
			string IManyOnePathSortItem.PersistData(ICmObjectRepository repo)
			{
				var builder = new StringBuilder();
				builder.Append(PersistGuid(_item));
				if (AsInterface.PathLength > 0)
				{
					for (var i = 0; i < _pathObjects.Length; i++)
					{
						builder.Append(";");
						builder.Append(_pathFlids[i]);
						builder.Append(";");
						builder.Append(PersistGuid(_pathObjects[i]));
					}
				}
				return builder.ToString();
			}

			#endregion

			private static string PersistGuid(ICmObjectOrId item)
			{
				return Convert.ToBase64String(item.Id.Guid.ToByteArray());
			}

			private static ICmObjectOrId ParseGuidRep(ICmObjectRepository repo, string chunk)
			{
				var result = repo.GetObjectOrIdWithHvoFromGuid(new Guid(Convert.FromBase64String(chunk)));
				if (result == null)
				{
					throw new InvalidObjectGuidException();
				}
				return result;
			}

			private ICmObject RealKeyObject()
			{
				var temp = _item.GetObject(_repo);
				_item = temp; // locking not needed, all threads will update to same thing
				return temp;
			}
		}
	}
}