// --------------------------------------------------------------------------------------------
// Copyright (C) 2002 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: fdoVectors.cs
// Responsibility: John Hatton and Randy Regnier
// Last reviewed: never
//
//
// <remarks>
// Implementation of:
//
// FdoVector : Object (Implements IEnumerable)
//		FdoSequence : FdoVector
//			FdoOwningSequence : FdoSequence
//			FdoReferenceSequence : FdoSequence
//		FdoCollection : FdoVector
//			FdoOwningCollection : FdoCollection
//			FdoReferenceCollection : FdoCollection
// FdoVectorEnumerator : System.Collections.IEnumerator
//		FdoObjectSet : FdoVectorEnumerator, IEnumerable
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// Summary description for FDOVector.
	/// </summary>
	public abstract class FdoVector<T> : IEnumerable<T>, IEnumerable
		where T : ICmObject
	{
		#region Data members for FdoVector

		/// <summary></summary>
		protected FdoCache m_cache;
		/// <summary></summary>
		protected int m_hvoObj;
		/// <summary></summary>
		protected int m_flid;
		/// <summary>
		/// For optimization in cases where it is enormously faster to just load all of
		/// them rather selecting each one of the thousands. (E.g. entries of a lexicon)
		/// </summary>
		protected bool m_shouldLoadAllOfType;
		/// <summary>
		/// the csharpType of the signature of this vector.
		/// </summary>
		protected Type m_tSignature;
		/// <summary>
		/// true if this vector (that is, its signature) might contain objects of multiple classes
		/// </summary>
		protected bool m_fAllowsMultipleTypes;

		#endregion	// Data members for FdoVector

		#region Properties

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int HvoObj
		{
			get { return m_hvoObj; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Flid
		{
			get {return m_flid;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Count
		{
			get
			{
				return m_cache.GetVectorSize(m_hvoObj, m_flid);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the vector of objects as an array of IDs. This will NOT reload the vector from
		/// the database (though probably it will load it if it hasn't been loaded at all).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int[] HvoArray
		{
			get
			{
				return m_cache.GetVectorProperty(m_hvoObj, m_flid, false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For optimization in cases where it is enormously faster to just load all of
		/// them rather selecting each one of the thousands. (E.g. entries of a lexicon)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShouldLoadAllOfType
		{
			get
			{
				return m_shouldLoadAllOfType;
			}
			set
			{
				m_shouldLoadAllOfType = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get { return m_cache; }
		}
		#endregion	// Properties

		#region Construction and Initializing

		/// <summary>
		/// construct an object which allows access to a vector attribute
		/// </summary>
		/// <param name="cache">the FdoCache</param>
		/// <param name="hvo">the ID of the object which has this vector attribute</param>
		/// <param name="flid">the field ID of this vector attribute</param>
		protected FdoVector(FdoCache cache, int hvo, int flid)
		{
			if (cache == null)
				throw new System.ArgumentException("the cache parameter cannot be null");
			if (hvo < 1)
				throw new System.ArgumentException("the hvo parameter cannot be < 1");
			if (!cache.IsVectorProperty(flid))
				throw new System.ArgumentException("Field is not a collection or sequence.", "flid");

			uint clid = cache.MetaDataCacheAccessor.GetDstClsId((uint)flid);
			bool fIsAbstract = cache.MetaDataCacheAccessor.GetAbstract(clid);
			Type type = null;
			int countAllDirectSubclasses = 0;
			bool allowsMultipleTypes = false;
			uint[] uIds = new uint[0];
			try
			{
				cache.MetaDataCacheAccessor.GetDirectSubclasses(clid, 0,
					out countAllDirectSubclasses, ArrayPtr.Null);
				// if the class is abstract, and has one subclass, we'll try to use that as our type.
				if (fIsAbstract && countAllDirectSubclasses == 1)
				{
					using (ArrayPtr clids = MarshalEx.ArrayToNative(countAllDirectSubclasses, typeof(uint)))
					{
						cache.MetaDataCacheAccessor.GetDirectSubclasses(clid, countAllDirectSubclasses,
							out countAllDirectSubclasses, clids);
						uIds = (uint[])MarshalEx.NativeToArray(clids, countAllDirectSubclasses, typeof(uint));
					}
				}

			}
			catch (System.Runtime.InteropServices.COMException err)
			{
				string msg = null;
				msg = err.Message;
			}
			if (fIsAbstract && uIds.Length == 1)
			{
				type = CmObject.GetTypeFromFWClassID(cache, (int)uIds[0]);
				allowsMultipleTypes = false;
			}
			else
			{
				type = CmObject.GetTypeFromFWClassID(cache, (int)clid);
				allowsMultipleTypes = countAllDirectSubclasses > 0;
			}
			//if (type != signature)
			//	throw new System.ArgumentException("Wrong Signature for given flid.");
			Type sig = typeof(T);
			bool hasInterfaceT = false;
			foreach (Type intface in type.GetInterfaces())
			{
				if (intface == sig)
				{
					hasInterfaceT = true;
					break;
				}
			}
			if (!hasInterfaceT)
				throw new System.ArgumentException("Wrong Signature for given flid.");

			m_cache = cache;
			m_hvoObj = hvo;
			m_flid = flid;
			m_tSignature = allowsMultipleTypes ? null : type;
			m_fAllowsMultipleTypes = allowsMultipleTypes;
		}


		#endregion	// Construction and Initializing

		#region IEnumerable<> implementation

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public IEnumerator<T> GetEnumerator()
		{
			foreach (int hvo in HvoArray)
			{
				Type csharpType = (m_tSignature != null) ? m_tSignature : CmObject.GetTypeFromFWClassID(m_cache, m_cache.GetClassOfObject(hvo));
				Debug.Assert(csharpType != null);
				// enhance: could use reflection to execute this on the one type,
				// if the list is homogeneous, or even the signature type if it is not.
				// using CM object is clearly slower, though how much?
				yield return (T)CmObject.CreateFromDBObject(m_cache, hvo, csharpType,
					 false/* don't check validity */,
					 false);	// don't load into cache, since it is loaded when the Enumerator
				//  was requested and thus the FdoObjectSet was created.
			}
		}

		#endregion IEnumerable<> implementation

		#region IEnumerable implementation

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion IEnumerable implementation

		#region Main methods

		/// <summary>
		///
		/// </summary>
		public static PropertyInfo HvoArrayPropertyInfo(object obj)
		{
			PropertyInfo pi = obj.GetType().GetProperty("HvoArray");
			Debug.Assert(pi != null);
			return pi;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public T[] ToArray()
		{
			List<T> items = this.ToList();
			return items.ToArray();
		}

		/// <summary>
		/// Returns objects in terms of generic List
		/// </summary>
		/// <returns></returns>
		public List<T> ToList()
		{
			List<T> items = new List<T>(Count);
			foreach (T item in this)
			{
				items.Add(item);
			}
			return items;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove hvoItem from the vector.
		/// [NOTE: This will delete hvoItem, if the vector is an owning vector.]
		/// </summary>
		/// <param name="hvoItem">ID of item to remove from vector.</param>
		/// <exception cref="System.ArgumentException">
		/// Thrown when hvoItem is not in the vector.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public virtual void Remove(int hvoItem)
		{
			// See if it is actually in the vector.
			int ihvo = m_cache.GetObjIndex(m_hvoObj, m_flid, hvoItem);
			if (ihvo == -1)
				throw new System.ArgumentException("Item is not in the vector.", "hvoItem");
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove objItem from the vector.
		/// [NOTE: This will delete objItem, if the vector is an owning vector.]
		/// </summary>
		/// <param name="objItem">The object to remove from the vector.</param>
		/// <exception cref="System.ArgumentNullException">
		/// Thrown when objItem is null.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public virtual void Remove(T objItem)
		{
			if (objItem == null)
				throw new System.ArgumentNullException("objItem");
			Remove(objItem.Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes all elements from the vector. BEWARE: This does not create undo actions!!!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RemoveAll()
		{
			if (HvoArray.Length == 0)
				return;

			// For testing, there may not be a database. In this case, just remove all the
			// items through FDO
			if (m_cache.DatabaseAccessor == null)
			{
				while (HvoArray.Length > 0)
					Remove(HvoArray[0]);
				return;
			}

			// call the DeleteOwnSeq stored procedure
			IOleDbCommand odc;
			m_cache.DatabaseAccessor.CreateCommand(out odc);

			try
			{
				// execute the command on groups of hvos
				string sSqlCommand = "exec DeleteObjects @ntIds = '{0}'";
				int iNextGroup = 0;
				while (true)
				{
					string hvoList = CmObject.MakePartialIdList(ref iNextGroup, HvoArray);
					if (hvoList == string.Empty)
						break;
					odc.ExecCommand(string.Format(sSqlCommand, hvoList),
						(int)SqlStmtType.knSqlStmtStoredProcedure);
				}
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}

			// clear the cache info about this object
			m_cache.VwCacheDaAccessor.ClearInfoAbout(m_hvoObj,
				VwClearInfoAction.kciaRemoveObjectInfoOnly);
		}

		/// <summary>
		/// Check for valid object. May initialize it.
		/// </summary>
		/// <param name="objItem">The object to check.</param>
		/// <param name="iAt"></param>
		/// <exception cref="System.ArgumentNullException">
		/// Thrown when objItem is null.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// Thrown when objItem is not valid.
		/// </exception>
		protected T CheckValidObject(T objItem, int iAt)
		{
			if (objItem == null)
				throw new System.ArgumentNullException("objItem");
			if (!((objItem as CmObject).IsValidObject()))
			{
				return CheckValidObjectCore(objItem, iAt);
			}
			return (T)(object)null;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="objItem"></param>
		/// <param name="iAt"></param>
		/// <returns></returns>
		protected virtual T CheckValidObjectCore(T objItem, int iAt)
		{
			// Owning vectors will override this to do useful work.
			throw new System.ArgumentException("The object is not a valid object.", "objItem");
		}


		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		protected int[] MakeArray(int hvo)
		{
			if (!m_cache.IsValidObject(hvo))
				throw new System.ArgumentException("ID is not legal.", "hvo");
			return new int[]{hvo};
		}


		/// <summary>
		///
		/// </summary>
		/// <param name="ahvo"></param>
		/// <param name="ft"></param>
		/// <param name="flidSrc"></param>
		/// <param name="hvoSrc"></param>
		/// <param name="ihvo"></param>
		protected void ValidateHvoArray(int[] ahvo, FieldType ft,
			out int flidSrc, out int hvoSrc, out int ihvo)
		{
			ihvo = -1;
			// Dig up owning flid of first guy in array.
			flidSrc = m_cache.GetOwningFlidOfObject(ahvo[0]);
			if (flidSrc < 1)
				throw new System.Exception("First object has no valid field ID.");
			// Find owner of first guy in array.
			hvoSrc = m_cache.GetOwnerOfObject(ahvo[0]);
			if (hvoSrc < 1)
				throw new System.Exception("First object has no owner.");
			// Make sure flidDst is a collection.
			FieldType iType = m_cache.GetFieldType(m_flid);
			if (iType != ft)
				throw new System.Exception("Destination flid is not a collection.");
			// Make sure everything in ahvo is owned in flidSrc.
			foreach (int xhvo in ahvo)
			{
				ihvo = m_cache.GetObjIndex(hvoSrc, flidSrc, xhvo);
				if (ihvo == -1)
					throw new System.ArgumentException("Item is not in source collection.", "flidSrc");
			}
		}

		/// <summary>
		/// Tells whether the given object is a member of this collection.
		/// </summary>
		/// <param name="obj">The object to search for.</param>
		/// <returns>True if the object is a member of the collection, else false.</returns>
		// Written: November 19, 2002, John Hatton
		public bool Contains(T obj)
		{
			return this.Contains(obj.Hvo);
		}

		/// <summary>
		/// Tells whether the given object is a member of this collection.
		/// </summary>
		/// <param name="hvo">The database ID of the object to search for.</param>
		/// <returns>True if the object is a member of the collection, else false.</returns>
		// Written: November 19, 2002, John Hatton
		public bool Contains(int hvo)
		{
			for(int i = HvoArray.Length-1; i>=0; i--)
			{
				if(HvoArray[i] == hvo)
					return true;
			}
			return false;
		}

		/// <summary>
		/// This reloads the sequence/collection into the cache.  See LT-8718 for an example of
		/// why this may be needed.
		/// </summary>
		/// <returns>true if the content has changed</returns>
		public bool UpdateIfCached()
		{
			// REVIEW: Is it worth knowing that the vector has changed?  The comparison code
			// could get quite slow for long vectors.
			int[] oldVec = m_cache.GetVectorProperty(m_hvoObj, m_flid, false);
			int cpt = m_cache.MetaDataCacheAccessor.GetFieldType((uint)m_flid);
			m_cache.VwOleDbDaAccessor.UpdatePropIfCached(m_hvoObj, m_flid, cpt, 0);
			int[] newVec = m_cache.GetVectorProperty(m_hvoObj, m_flid, false);
			if (oldVec.Length != newVec.Length)
				return true;
			bool fDiff = false;
			for (int i = 0; i < oldVec.Length; ++i)
			{
				if (oldVec[i] != newVec[i])
				{
					fDiff = true;
					break;
				}
			}
			if (cpt == (int)FieldType.kcptOwningSequence || cpt == (int)FieldType.kcptReferenceSequence)
				return fDiff;
			if (fDiff)
			{
				Debug.Assert(cpt == (int)FieldType.kcptOwningCollection || cpt == (int)FieldType.kcptReferenceCollection);
				// Order doesn't matter for collections, so we'll take a closer look.
				for (int i = 0; i < oldVec.Length; ++i)
				{
					fDiff = true;
					for (int j = 0; i < newVec.Length; ++j)
					{
						if (oldVec[i] == newVec[j])
						{
							fDiff = false;
							newVec[j] = -1;		// mark with invalid value so it won't be reused.
							break;
						}
					}
					if (fDiff)
						return true;
				}
			}
			return false;
		}
		#endregion	// Main methods
	}

	/// <summary>
	///
	/// </summary>
	public abstract class FdoVectorUtils
	{
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="TDerived"></typeparam>
		/// <param name="cmObjects"></param>
		/// <returns></returns>
		public static List<int> ConvertCmObjectsToHvoList<TDerived>(IEnumerable<TDerived> cmObjects)
		   where TDerived : ICmObject
		{
		   return new List<int>(ConvertCmObjectsToHvos<TDerived>(cmObjects));
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cmObjects"></param>
		/// <returns></returns>
		public static IEnumerable<int> ConvertCmObjectsToHvos<TDerived>(IEnumerable<TDerived> cmObjects)
			where TDerived : ICmObject
		{
			foreach (ICmObject cmObject in cmObjects)
				yield return cmObject.Hvo;
		}
	}


	#region Sequences

	/// <summary>
	///
	/// </summary>
	public abstract class FdoSequence<T> : FdoVector<T>
		where T : ICmObject
	{
		#region Construction and Initializing

		/// <summary>
		/// construct an object which allows access to a vector attribute
		/// </summary>
		/// <param name="cache">the FdoCache</param>
		/// <param name="hvo">the ID of the object which has this vector attribute</param>
		/// <param name="flid">the field ID of this vector attribute</param>
		internal FdoSequence(FdoCache cache, int hvo, int flid)
			: base(cache, hvo,  flid)
		{ /* Nothing else to do here. */ }

		#endregion	// Construction and Initializing

		#region Append methods

		/// <summary>
		/// Append object to end of sequence.
		/// </summary>
		/// <param name="objItem">The object to append to the sequence.</param>
		/// <returns>objItem. Useful in: b = v.Append(new Blah())</returns>
		public T Append(T objItem)
		{
			int chvo = m_cache.GetVectorSize(m_hvoObj, m_flid);
			T cmo = CheckValidObject(objItem, chvo);	// May throw an exception.
			if (cmo != null)
				return cmo; // Freshly created and initialized, so quit.
			Append(objItem.Hvo);
			return objItem;
		}


		/// <summary>
		/// Append object to sequence, when given its ID.
		/// </summary>
		/// <param name="hvoItem">ID of object to add to sequence.</param>
		public void Append(int hvoItem)
		{
			int[] ahvo = MakeArray(hvoItem);
			Append(ahvo);
		}


		/// <summary>
		/// Append items in given array to the sequence.
		/// </summary>
		/// <param name="ahvo">Array of items to append.</param>
		/// <exception cref="System.Exception">
		/// Thrown, because suclasses must override this method.
		/// </exception>
		protected virtual void Append(int[] ahvo)
		{
			throw new System.Exception("Subclasses must override this method.");
		}


		#endregion	// Append methods

		#region Insert methods

		/// <summary>
		/// Insert object at the specified location.
		/// </summary>
		/// <param name="objItem">The object to insert.</param>
		/// <param name="iAt">The location at which the object is inserted.</param>
		/// <returns>the obj; useful in: b = v.Insert(new Blah(), 3)</returns>
		public T InsertAt(T objItem, int iAt)
		{
			// Note: The index is checked elsewhere.
			T cmo = CheckValidObject(objItem, iAt);
			if (cmo != null)
				return cmo; // Freshly created and initialized, so quit.
			InsertAt(objItem.Hvo, iAt);
			return objItem;
		}


		/// <summary>
		/// Insert object at the specified location.
		/// </summary>
		/// <param name="hvoItem">The ID of the object to insert.</param>
		/// <param name="iAt">The location at which the object is inserted.</param>
		public void InsertAt(int hvoItem, int iAt)
		{
			// Note: The index is checked elsewhere.
			int[] ahvo = MakeArray(hvoItem);
			InsertAt(ahvo, iAt);
		}


		/// <summary>
		/// Insert items in given array at given index.
		/// </summary>
		/// <param name="ahvo">Array of items to insert.</param>
		/// <param name="iAt">Index at which to insert items.</param>
		/// <exception cref="System.Exception">
		/// Thrown, because subclasses must override this method.
		/// </exception>
		protected virtual void InsertAt(int[] ahvo, int iAt)
		{
			throw new System.Exception("Subclasses must override this method.");
		}


		#endregion	// Insert methods

		#region Remove methods


		/// <summary>
		/// Remove item at given index.
		/// </summary>
		/// <param name="iAt">The index of the item to remove.</param>
		/// <exception cref="System.Exception">
		/// Thrown, because subclasses must override this method.
		/// </exception>
		public virtual void RemoveAt(int iAt)
		{
			throw new System.Exception("Subclasses must override this method.");
		}

		#endregion	// Remove methods

		#region Misc methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends the int method info.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static MethodInfo AppendIntMethodInfo(object obj)
		{
			MethodInfo myAppendMethod = null;
			foreach (MethodInfo method in obj.GetType().GetMethods())
			{
				if (method.IsPublic && method.Name == "Append")
				{
					foreach (ParameterInfo parm in method.GetParameters())
					{
						if (parm.ParameterType == typeof(int))
						{
							myAppendMethod = method;
							break;
						}
					}
				}
				if (myAppendMethod != null)
					break;
			}
			Debug.Assert(myAppendMethod != null, "Couldn't find 'Append' method with in int as a parameter.");
			return myAppendMethod;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the int method info.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static MethodInfo RemoveIntMethodInfo(object obj)
		{
			MethodInfo myRemoveMethod = null;
			foreach (MethodInfo method in obj.GetType().GetMethods())
			{
				if (method.IsPublic && method.Name == "Remove")
				{
					foreach (ParameterInfo parm in method.GetParameters())
					{
						if (parm.ParameterType == typeof(int))
						{
							myRemoveMethod = method;
							break;
						}
					}
				}
				if (myRemoveMethod != null)
					break;
			}
			Debug.Assert(myRemoveMethod != null, "Couldn't find 'Remove' method with in int as a parameter.");
			return myRemoveMethod;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if given index is within legal range.
		/// </summary>
		/// <param name="index">The index to check.</param>
		/// <exception cref="System.IndexOutOfRangeException">
		/// Thrown when the given index is less than zero, or greater than the size of the
		/// sequence.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		protected void CheckIndex(int index)
		{
			if (index < 0 || index >= HvoArray.Length)
			{
				IndexOutOfRangeException ex = new IndexOutOfRangeException();
				ex.Data.Add("FdoSequence.CheckIndex: Parameter", index);
				ex.Data.Add("FdoSequence.CheckIndex: Length of sequence", HvoArray.Length);
				throw ex;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if given index is valid as an insertion point.
		/// </summary>
		/// <param name="index">The index to check.</param>
		/// <exception cref="System.IndexOutOfRangeException">
		/// Thrown when the given index is less than zero, or greater than the size of the
		/// sequence.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		protected void CheckInsertIndex(int index)
		{
			if (index < 0 || index > HvoArray.Length )
			{
				IndexOutOfRangeException ex = new IndexOutOfRangeException();
				ex.Data.Add("FdoSequence.CheckInsertIndex: Parameter", index);
				ex.Data.Add("FdoSequence.CheckInsertIndex: Length of sequence", HvoArray.Length);
				throw ex;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indexer for the vector
		/// </summary>
		/// <example>
		/// 	<code>
		///		//get the third element in the AnthroList vector
		///		CmAnthroItem p = (CmAnthroItem)m_cache.LangProject.AnthroListOA[2];
		///		</code>
		/// </example>
		/// ------------------------------------------------------------------------------------
		public T this[int index]   // indexer declaration
		{
			get
			{
				CheckIndex(index);
				int hvo = HvoArray[index];
				Type csharpType = CmObject.GetTypeFromFWClassID(m_cache,
					m_cache.GetClassOfObject(hvo));
				Debug.Assert(csharpType != null);
				// For performance reasons we don't load everything into the cache if PreloadData
				// is set to false. The C++ cache autoloads missing objects when we try to access them.
				return (T)CmObject.CreateFromDBObject(m_cache, hvo, csharpType,
					false /* don't check validity */, m_cache.PreloadData);
			}
		}

		#endregion	// Misc methods

		#region Properties

		/// <summary>
		/// Get the first item in the collection
		/// </summary>
		/// <returns>the first item, or null if the set is empty</returns>
		public T FirstItem
		{
			get
			{
				if (this.Count> 0)
					return (T)CmObject.CreateFromDBObject(m_cache, HvoArray[0]);
				else
					return (T)(object)null;
			}
		}
		#endregion
	}


	/// <summary>
	///
	/// </summary>
	public class FdoOwningSequence<T> : FdoSequence<T>
		where T : ICmObject
	{
		#region Construction and Initializing

		/// <summary>
		/// construct an object which allows access to a vector attribute
		/// </summary>
		/// <param name="cache">the FdoCache</param>
		/// <param name="hvo">the ID of the object which has this vector attribute</param>
		/// <param name="flid">the field ID of this vector attribute</param>
		public FdoOwningSequence(FdoCache cache, int hvo, int flid)
			: base(cache, hvo,  flid)
		{ /* Nothing else to do here. */ }

		#endregion	// Construction and Initializing

		/// <summary>
		/// Remove hvoItem from the vector.
		/// [NOTE: This will delete hvoItem, if the vector is an owning vector.]
		/// </summary>
		/// <param name="hvoItem">ID of item to remove from vector.</param>
		/// <exception cref="System.ArgumentException">
		/// Thrown when hvoItem is not in the vector.
		/// </exception>
		public override void Remove(int hvoItem)
		{
			base.Remove(hvoItem);
			m_cache.DeleteObject(hvoItem);
		}


		/// <summary>
		/// Remove objItem from the vector.
		/// [NOTE: This will delete objItem, if the vector is an owning vector.]
		/// </summary>
		/// <param name="objItem">The object to remove from the vector.</param>
		/// <exception cref="System.ArgumentNullException">
		/// Thrown when objItem is null.
		/// </exception>
		public override void Remove(T objItem)
		{
			if (objItem == null)
				throw new System.ArgumentNullException("objItem");

			base.Remove(objItem);
			(objItem as CmObject).UnderlyingObjectDeleted();
		}


		/// <summary>
		/// Remove item at given index.
		/// </summary>
		/// <param name="iAt">The index of the item to remove.</param>
		public override void RemoveAt(int iAt)
		{
			CheckIndex(iAt);
			int hvo = m_cache.GetVectorItem(m_hvoObj, m_flid, iAt);
			m_cache.DeleteObject(hvo);
		}


		/// <summary>
		/// Overrides method to initialize new owned object.
		/// </summary>
		/// <param name="objItem">Object to initialize.</param>
		/// <param name="iAt">Location where new object gets inserted.</param>
		/// <returns>The newly initialized object.</returns>
		protected override T CheckValidObjectCore(T objItem, int iAt)
		{
			(objItem as CmObject).InitNew(m_cache, m_hvoObj, m_flid, iAt);
			return objItem;
		}


		/// <summary>
		/// Append an array of objects to the end of the sequence.
		/// </summary>
		/// <param name="ahvo">Array of IDs of objects to append.</param>
		protected override void Append(int[] ahvo)
		{
			if (ahvo.Length == 0)
				return;
			InsertAt(ahvo, Count);
		}


		/// <summary>
		/// Insert an array of objects at the given index.
		/// </summary>
		/// <param name="ahvo">Array of IDs to insert.</param>
		/// <param name="iAt">Location to insert objects.</param>
		protected override void InsertAt(int[] ahvo, int iAt)
		{
			int flidSrc;
			int hvoSrc;
			int ihvo;

			if (iAt != Count)
				CheckIndex(iAt);
			ValidateHvoArray(ahvo, FieldType.kcptOwningSequence, out flidSrc,
				out hvoSrc, out ihvo);
			foreach (int hvo in ahvo)
				m_cache.ChangeOwner(hvo, m_hvoObj, m_flid, iAt++);
		}

	}	// End FdoOwningSequence class

	/// <summary>
	///
	/// </summary>
	public class FdoReferenceSequence<T> : FdoSequence<T>
		where T : ICmObject
	{
		#region Construction and Initializing

		/// <summary>
		/// construct an object which allows access to a vector attribute
		/// </summary>
		/// <param name="cache">the FdoCache</param>
		/// <param name="hvo">the ID of the object which has this vector attribute</param>
		/// <param name="flid">the field ID of this vector attribute</param>
		public FdoReferenceSequence(FdoCache cache, int hvo, int flid)
			: base(cache, hvo,  flid)
		{ /* Nothing else to do here. */ }

		#endregion	// Construction and Initializing

		/// <summary>
		/// Remove hvoItem from the vector.
		/// [NOTE: This will delete hvoItem, if the vector is an owning vector.]
		/// </summary>
		/// <param name="hvoItem">ID of item to remove from vector.</param>
		/// <exception cref="System.ArgumentException">
		/// Thrown when hvoItem is not in the vector.
		/// </exception>
		public override void Remove(int hvoItem)
		{
			while (new List<int>(HvoArray).Contains(hvoItem))
			{
				base.Remove(hvoItem);
				m_cache.RemoveReference(m_hvoObj, m_flid, hvoItem);
			}
		}


		/// <summary>
		/// Remove item at given index.
		/// </summary>
		/// <param name="iAt">The index of the item to remove.</param>
		public override void RemoveAt(int iAt)
		{
			CheckIndex(iAt);
			int hvo = m_cache.GetVectorItem(m_hvoObj, m_flid, iAt);
			Remove(hvo);
		}


		/// <summary>
		/// Append an array of objects to end of sequence.
		/// </summary>
		/// <param name="ahvo">Array of objects IDs to append.</param>
		protected override void Append(int[] ahvo)
		{
			InsertAt(ahvo, -1);
		}


		/// <summary>
		/// Insert an array of objects at the given index.
		/// </summary>
		/// <param name="ahvo">Array of object IDs to insert.</param>
		/// <param name="iAt">Location for insertion.</param>
		protected override void InsertAt(int[] ahvo, int iAt)
		{
			int iMin = iAt;
			if (iAt == -1)
				iMin = Count;
			CheckInsertIndex(iMin);

			m_cache.ReplaceReferenceProperty(m_hvoObj, m_flid, iMin, iMin, ref ahvo);
		}

	}	// End FdoReferenceSequence class


	#endregion	// Sequences

	#region Collections


	/// <summary>
	///
	/// </summary>
	public abstract class FdoCollection<T> : FdoVector<T>
		where T : ICmObject
	{
		#region Construction and Initializing

		/// <summary>
		/// construct an object which allows access to a vector attribute
		/// </summary>
		/// <param name="cache">the FdoCache</param>
		/// <param name="hvo">the ID of the object which has this vector attribute</param>
		/// <param name="flid">the field ID of this vector attribute</param>
		internal FdoCollection(FdoCache cache, int hvo, int flid)
			: base(cache, hvo,  flid)
		{ /* Nothing else to do here. */ }

		#endregion	// Construction and Initializing

		#region Add methods

		/// <summary>
		/// Add object to collection.
		/// </summary>
		/// <param name="objItem">Object to add to collection.</param>
		/// <returns>objItem. Useful in: b = v.Add(new Blah())</returns>
		public T Add(T objItem)
		{
			if (objItem == null)
				throw new System.ArgumentNullException("objItem");

			// Notes:
			//	1. CheckValidObject may throw an exception.
			//	2. cmo will not be null, only if CheckValidObject needed to
			//		initialize the input object. In this case the ownership will have
			//		been set within CheckValidObject, or methods it calls.
			T cmo = CheckValidObject(objItem, 0);
			if (cmo != null)
			{
				m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, m_hvoObj, m_flid, 0, 1, 0);
				return cmo; // Freshly created, and initialized to new owner, so quit.
			}
			Add(objItem.Hvo);
			return objItem;
		}


		/// <summary>
		/// Add collection to collection.
		/// </summary>
		/// <param name="col">A collection to add to this collection.</param>
		/// <exception cref="System.ArgumentNullException">
		/// Thrown when col is null.
		/// </exception>
		public void Add(FdoCollection<T> col)
		{
			if (col == null)
				throw new System.ArgumentNullException("col");
			Add(col.HvoArray);
		}


		/// <summary>
		/// Add object to collection, when given its ID.
		/// </summary>
		/// <param name="hvoItem">ID of object to add to collection.</param>
		/// <exception cref="System.ArgumentException">
		/// Thrown when hvoItme is less than zero.
		/// </exception>
		public void Add(int hvoItem)
		{
			int[] ahvo = MakeArray(hvoItem);
			Add(ahvo);
		}


		/// <summary>
		/// Add array of IDs to collection.
		/// </summary>
		/// <param name="ahvo">Array of IDs to add to collection.</param>
		public virtual void Add(int[] ahvo)
		{
			throw new System.Exception("Subclasses must override this method.");
		}

		#endregion	// Add methods

		/// <summary>
		///
		/// </summary>
		public static MethodInfo AddIntMethodInfo(object obj)
		{
			MethodInfo myAddMethod = null;
			foreach (MethodInfo method in obj.GetType().GetMethods())
			{
				if (method.IsPublic && method.Name == "Add")
				{
					foreach (ParameterInfo parm in method.GetParameters())
					{
						if (parm.ParameterType == typeof(int))
						{
							myAddMethod = method;
							break;
						}
					}
				}
				if (myAddMethod != null)
					break;
			}
			Debug.Assert(myAddMethod != null, "Couldn't find 'Add' method with in int as a parameter.");
			return myAddMethod;
		}

	}	// End FdoCollection class



	/// <summary>
	///
	/// </summary>
	public class FdoOwningCollection<T> : FdoCollection<T>
		where T : ICmObject
	{
		#region Construction and Initializing

		/// <summary>
		/// construct an object which allows access to a vector attribute
		/// </summary>
		/// <param name="cache">the FdoCache</param>
		/// <param name="hvo">the ID of the object which has this vector attribute</param>
		/// <param name="flid">the field ID of this vector attribute</param>
		public FdoOwningCollection(FdoCache cache, int hvo, int flid)
			: base(cache, hvo,  flid)
		{ /* Nothing else to do here. */ }

		#endregion	// Construction and Initializing

		/// <summary>
		/// Remove hvoItem from the vector.
		/// [NOTE: This will delete hvoItem, if the vector is an owning vector.]
		/// </summary>
		/// <param name="hvoItem">ID of item to remove from vector.</param>
		/// <exception cref="System.ArgumentException">
		/// Thrown when hvoItem is not in the vector.
		/// </exception>
		public override void Remove(int hvoItem)
		{
			if (new List<int>(HvoArray).Contains(hvoItem))
			{
				base.Remove(hvoItem);
				m_cache.DeleteObject(hvoItem);
			}
		}

		/// <summary>
		/// Remove objItem from the vector.
		/// [NOTE: This will delete objItem, if the vector is an owning vector.]
		/// </summary>
		/// <param name="objItem">The object to remove from the vector.</param>
		/// <exception cref="System.ArgumentNullException">
		/// Thrown when objItem is null.
		/// </exception>
		public override void Remove(T objItem)
		{
			base.Remove(objItem);
			(objItem as CmObject).UnderlyingObjectDeleted();
		}

		/// <summary>
		/// Overrides method to initialize a new owned object.
		/// </summary>
		/// <param name="objItem">Object to initialize.</param>
		/// <param name="iAt">Index for insertion.</param>
		/// <returns></returns>
		protected override T CheckValidObjectCore(T objItem, int iAt)
		{
			(objItem as CmObject).InitNew(m_cache, m_hvoObj, m_flid, iAt);
			return objItem;
		}

		/// <summary>
		/// Add an array of objects to the collection.
		/// </summary>
		/// <param name="ahvo">Array of objects IDs to add to collection.</param>
		public override void Add(int[] ahvo)
		{
			if (ahvo.Length == 0)
				return;		// Nothing to do.

			int flidSrc;
			int hvoSrc;
			int ihvo;

			ValidateHvoArray(ahvo, FieldType.kcptOwningCollection,
				out flidSrc, out hvoSrc, out ihvo);

			// See if a real change occurs.
			if ((flidSrc == m_flid) && (hvoSrc == m_hvoObj))
				return;		// Nothing to do.

			// Make the move.
			foreach (int i in ahvo)
				m_cache.ChangeOwner(i, m_hvoObj, m_flid);
		}
	}	// End FdoOwningCollection class


	/// <summary>
	///
	/// </summary>
	public class FdoReferenceCollection<T> : FdoCollection<T>
		where T : ICmObject
	{
		#region Construction and Initializing

		/// <summary>
		/// construct an object which allows access to a vector attribute
		/// </summary>
		/// <param name="cache">the FdoCache</param>
		/// <param name="hvo">the ID of the object which has this vector attribute</param>
		/// <param name="flid">the field ID of this vector attribute</param>
		public FdoReferenceCollection(FdoCache cache, int hvo, int flid)
			: base(cache, hvo,  flid)
		{ /* Nothing else to do here. */ }

		#endregion	// Construction and Initializing

		/// <summary>
		/// Remove hvoItem from the vector.
		/// </summary>
		/// <param name="hvoItem">ID of item to remove from vector.</param>
		public override void Remove(int hvoItem)
		{
			if (new List<int>(HvoArray).Contains(hvoItem))
			{
				base.Remove(hvoItem);
				m_cache.RemoveReference(m_hvoObj, m_flid, hvoItem);
			}
		}


		/// <summary>
		/// Add an array of objects to the collection.
		/// </summary>
		/// <param name="ahvo">Array of object IDs to add to the collection.</param>
		public override void Add(int[] ahvo)
		{
			if (ahvo.Length == 0)
				return;		// Nothing to do.

			// Make sure everything in ahvo is owned somewhere.
			foreach (int hvo in ahvo)
			{
				int hvoOwner = m_cache.GetOwnerOfObject(hvo);
				if (hvoOwner == 0 && !m_cache.ClassIsOwnerless(hvo))
					throw new System.Exception("Item is not owned.");
			}
			int chvo = m_cache.GetVectorSize(m_hvoObj, m_flid);
			// Remove duplicates by using a Set, since reference collections can't have them.
			int[] myHvos = HvoArray;
			Set<int> set = new Set<int>(HvoArray);
#if DEBUG
			if (myHvos.Length != set.Count)
				Debug.WriteLine("Reference collection started with some duplicates: 'Add' method.");
#endif
			set.AddRange(ahvo);
			int[] newHvos = set.ToArray();
#if DEBUG
			if (myHvos.Length + ahvo.Length != newHvos.Length)
				Debug.WriteLine("Removed some elements from reference collection 'Add', since some were duplicates.");
#endif
			m_cache.ReplaceReferenceProperty(m_hvoObj, m_flid, 0, chvo, ref newHvos);
		}

		/// <summary>
		/// Answer true if the two collections are equivalent.
		/// We do not allow for duplicates; if the sizes are the same and
		/// every element in one is in the other, we consider them equivalent.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool IsEquivalent(FdoReferenceCollection<T> other)
		{
			if (this.Count != other.Count)
				return false;
			List<int> otherList = new List<int>(other.HvoArray);
			int[] myHvos = HvoArray;
			for (int ihvo = 0; ihvo < myHvos.Length; ++ihvo)
			{
				if (!otherList.Contains(myHvos[ihvo]))
					return false;
				otherList.Remove(myHvos[ihvo]);	// ensure unique matches
			}
			return true;
		}

	}	// End FdoReferenceCollection class


	#endregion	// Collections

	#region Enumerators


	/// <summary>
	/// An enumerator, which is created by FDOVector when the vector is used in a foreach statement.
	/// Upon construction, the entire vector of objects are pre-cached as efficiently as possible.
	/// </summary>
	public class FdoObjectSet<T> : IEnumerable<T>, IEnumerable
		where T : ICmObject
	{
		#region Data members
		/// <summary></summary>
		private FdoCache m_cache;
		/// <summary></summary>
		private int[] m_hvos = new int[0];
		/// <summary></summary>
		private Type m_signature;

		// private T m_type;

		#endregion Data members

		#region Properties

		/// <summary>
		///
		/// </summary>
		public int Count
		{
			get
			{
				return m_hvos.Length;
			}
		}

		/// <summary>
		/// hvos for this object set.
		/// </summary>
		public int[] HvoArray
		{
			get
			{
				return m_hvos;
			}
		}

		/// <summary>
		/// Returns objects in terms of generic List
		/// </summary>
		/// <returns></returns>
		public List<T> ToList()
		{
			List<T> items = new List<T>(Count);
			foreach (T item in this)
			{
				items.Add(item);
			}
			return items;
		}

		/// <summary>
		/// Get the first item in the collection. Only makes much sense if the collection is sorted.
		/// </summary>
		/// <returns>the first item, or null if the set is empty</returns>
		public T FirstItem
		{
			get
			{
				if (m_hvos.GetLength(0) > 0)
					return (T)CmObject.CreateFromDBObject(m_cache, m_hvos[0]);
				else
					return (T)((object)null);
			}
		}

		#endregion Properties

		#region Construction

		/// <summary>
		/// Construct enumerator for an array of object IDs,
		/// which are all the same class of object.
		/// </summary>
		/// <remarks> this constructor actually pre-caches all of the objects in the vector.
		/// This constructor is used by the FdoVector class, not the client.
		/// We must preserve the order and number (even duplictions) of the hvos parameter.
		/// </remarks>
		/// <param name="cache">the FdoCache</param>
		/// <param name="hvos">the IDs of the objects which will be enumerated.</param>
		/// <param name="fLoadData">True to load data automatically. False if everything is preloaded </param>
		/// <param name="signature">the signature, as a csharpType, of this attribute.</param>
		public FdoObjectSet(FdoCache cache, int[] hvos, bool fLoadData, Type signature)
			: this(cache, signature)
		{
			m_hvos = hvos;
			if (fLoadData)
				CmObject.LoadObjectsIntoCache(cache, signature, hvos);
		}

		/// <summary>
		/// Construct enumerator for an array of object IDs,
		/// which are presumed to be of different classes.
		/// </summary>
		/// <remarks>This constructor actually pre-caches all of the objects in the given array.
		/// This constructor is used by the FdoVector class, and in rare cases where everything
		/// is precashed, it can be used by the client.
		/// We must preserve the order and number (even duplictions) of the hvos parameter.
		/// If there are very many objects in the array, we switch to 'load all of type'.
		/// </remarks>
		/// <param name="cache">the FdoCache</param>
		/// <param name="hvos">the IDs of the objects which will be enumerated. </param>
		/// <param name="fLoadData">True to load data automatically. False if everything is preloaded </param>
		public FdoObjectSet(FdoCache cache, int[] hvos, bool fLoadData)
			: this(cache)
		{
			// Since we know what they are, use the array provided,
			// and not those that could be set by LoadObjectsIntoCache,
			// which may not keep them in the same order and number, as in the given array
			m_hvos = hvos;

			if (!fLoadData)
				return; // Everything is preloaded, so we are done.
			LoadObjectsIntoCache();	// Insert the IDs into the query.
		}

		/// <summary>
		/// Construct an enumerator for an arbitrary set of objects returned by a SQL query
		/// </summary>
		/// <remarks> this constructor actually pre-caches all of the objects in the vector.
		/// This constructor may be used by the client.
		/// </remarks>
		/// <param name="cache">the FdoCache</param>
		/// <param name="sqlQuery">a SQL query returning hvos in the first column, and class IDs in the second.
		/// It should be sorted by class ID.
		/// </param>
		/// <param name="fHasOrdinalColumn"></param>
		public FdoObjectSet(FdoCache cache, string sqlQuery, bool fHasOrdinalColumn)
			: this(cache)
		{
			LoadObjectsIntoCache(sqlQuery, fHasOrdinalColumn,
				true);	// Have the method set the m_hvo member.
		}

		/// <summary>
		/// Construct an enumerator for an arbitrary set of objects returned by a SQL query
		/// </summary>
		/// <remarks> this constructor actually pre-caches all of the objects in the vector.
		/// This constructor may be used by the client.
		/// </remarks>
		/// <param name="cache">the FdoCache</param>
		/// <param name="sqlQuery">a SQL query returning hvos in the first column, and class IDs in the second.
		/// It should be sorted by class ID.
		/// </param>
		/// <param name="fHasOrdinalColumn"></param>
		/// <param name="fJustLoadAllOfType">Set to true if you want to just load all of the objects of the type rather than the ones returned by the query.
		/// </param>
		public FdoObjectSet(FdoCache cache, string sqlQuery, bool fHasOrdinalColumn, bool fJustLoadAllOfType)
			: this(cache)
		{
			LoadObjectsIntoCache(sqlQuery, fHasOrdinalColumn,
				true,  fJustLoadAllOfType);	// Have the method set the m_hvo member.
		}

		/// <summary>
		/// Construct an enumerator for an arbitrary set of objects returned by a SQL query
		/// which has an argument.
		/// </summary>
		/// <remarks> this constructor actually pre-caches all of the objects in the vector.
		/// This constructor may be used by the client.
		/// </remarks>
		/// <param name="cache">the FdoCache</param>
		/// <param name="sqlQuery">a SQL query returning hvos in the first column, and class IDs in the second.
		/// It should be sorted by class ID.
		/// </param>
		/// <param name="argument">string to insert in place of '?' in query</param>
		public FdoObjectSet(FdoCache cache, string sqlQuery, string argument)
			: this(cache)
		{
			LoadObjectsIntoCache(sqlQuery, "", false, true, false, false, argument);
		}
		/// <summary>
		/// constructor enumerator
		/// </summary>
		/// <param name="cache">the FdoCache</param>
		/// <param name="signature">the signature, as a csharpType, of this attribute.</param>
		private FdoObjectSet(FdoCache cache, Type signature)
		{
			Debug.Assert(cache != null && signature != null);

			m_cache = cache;
			m_hvos = new int[0];
			m_signature = signature;
		}

		/// <summary>
		/// constructor enumerator
		/// </summary>
		/// <param name="cache">the FdoCache</param>
		private FdoObjectSet(FdoCache cache)
		{
			Debug.Assert(cache != null);

			m_cache = cache;
			m_hvos = new int[0];
			m_signature = null;
		}

		#endregion Construction

		#region IEnumerable<T> and IEnumerable implementation

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public IEnumerator<T> GetEnumerator()
		{
			if (m_hvos == null)
				throw new InvalidOperationException();

			foreach (int hvo in m_hvos)
			{
				Type csharpType = (m_signature != null) ? m_signature : CmObject.GetTypeFromFWClassID(m_cache, m_cache.GetClassOfObject(hvo));
				Debug.Assert(csharpType != null);
				// enhance: could use reflection to execute this on the one type,
				// if the list is homogeneous, or even the signature type if it is not.
				// using CM object is clearly slower, though how much?
				yield return (T)CmObject.CreateFromDBObject(m_cache, hvo, csharpType,
					 false/* don't check validity */,
					 false);	// don't load into cache, since it is loaded when the Enumerator
								//  was requested and thus the FdoObjectSet was created.
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion IEnumerable<T> and IEnumerable implementation

		#region Misc methods

		/// <summary>
		/// Given a list of objects (already stored in m_hvos), create an FdoObject for each of them.
		/// Set m_signature (if they are all the same type).
		/// Warning: for large lists! Loads ALL objects of EVERY class that occurs in m_hvos.
		/// </summary>
		///
		protected void LoadObjectsIntoCache()
		{
			Debug.Assert(m_hvos != null);

			if (m_cache.DatabaseName == null || m_cache.DatabaseName == string.Empty)
				return;

			//The object of each type must be loaded separately because they have different
			//data and thus will have different queries and columnSpec's.

			// maxGroupSize is the size of m_hvos, if known, otherwise use 500, as a starting spot.
			// If there are more than the default of 500, the int array will be 'grown'
			// to fit them all, before caching is done.
			Dictionary<int, List<int>> ht = new Dictionary<int, List<int>>(); // key is clsid, val is a List of id integers.
			Dictionary<int, int> htcount = new Dictionary<int,int>(); // key is clsid, val is # in database.
			bool more;

			// Loop over the objects. For each one we find that we don't know a class of,
			// Retrieve all objects of that class, and record their types.
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			IVwCacheDa cda = m_cache.VwCacheDaAccessor;
			for (int ihvo = 0; ihvo < m_hvos.Length; ihvo++)
			{
				if (m_hvos[ihvo] <= 0)
					continue;			// can't load data from this id from the database!
				int clsid = 0;
				// If we don't already know the class of this object, we read it, and then (in one go) get ALL
				// the other objects of the same class and cache their classes.
				if (!sda.get_IsPropInCache(m_hvos[ihvo], (int)CmObjectFields.kflidCmObject_Class,
					(int)CellarModuleDefns.kcptInteger, 0))
				{
					// The SQL commands must NOT modify the database contents!
					DbOps.ReadOneIntFromCommand(m_cache,
						String.Format("select class$ from CmObject where id = {0}", m_hvos[ihvo]),
						null, out clsid);
					string sql = String.Format("select id from CmObject where class$ = {0}", clsid);
					IOleDbCommand odc = null;
					try
					{
						m_cache.DatabaseAccessor.CreateCommand(out odc);
						odc.ExecCommand(sql, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
						odc.GetRowset(0);
						odc.NextRow(out more);
						int ccls = 0;
						// Walk through the results, pre-caching as we collect sets of homogeneous objects
						using(ArrayPtr rgTmp = MarshalEx.ArrayToNative(1, typeof(int)))
						{
							while (more)
							{
								ccls++;
								bool fIsNull;
								uint cbSpaceTaken;

								// odc.GetColValue calls are all 1-based.
								odc.GetColValue(1,
									rgTmp, rgTmp.Size,
									out cbSpaceTaken, out fIsNull, 0);
								int[] hvosTmp = (int[])MarshalEx.NativeToArray(rgTmp, 1, typeof(int));
								cda.CacheIntProp(hvosTmp[0], (int)CmObjectFields.kflidCmObject_Class, clsid);
								odc.NextRow(out more);
							}
						}
						htcount[clsid] = ccls;
					}
					finally
					{
						DbOps.ShutdownODC(ref odc);
					}
				}
				else
				{
					// already cached...just read it
					clsid = sda.get_IntProp(m_hvos[ihvo], (int)CmObjectFields.kflidCmObject_Class);
				}
				// add m_hvos[ihvo] to the appropriate list in ht.
				List<int> list = null;
				if (ht.ContainsKey(clsid))
				{
					list = ht[clsid];
				}
				else
				{
					// A very rough guess at how many of this type we might have is the remaining number of ids divided
					// by the number of types we've seen, which is one more than the number already recorded.
					// (This also prevents dividing by zero the first time!)
					list = new List<int>((m_hvos.Length - ihvo) / (ht.Count + 1));
					ht[clsid] = list;
				}
				list.Add(m_hvos[ihvo]);
			}

			Type csharpType = null;
			foreach (KeyValuePair<int, List<int>> kvp in ht)
			{
				int clid = kvp.Key;
				List<int> alHomogeneousHvos = kvp.Value;
				int[] homogeneousHvos = alHomogeneousHvos.ToArray();
				csharpType = CmObject.GetTypeFromFWClassID(m_cache, clid);
				int count;
				if (htcount.ContainsKey(clid))
					count = htcount[clid];
				else
					DbOps.ReadOneIntFromCommand(m_cache, String.Format("select count(id) from CmObject where Class$ = {0}", clid), null, out count);
				// Now we know we want to create an object for each hvo in homogeneousHvos,
				// which are all of class clid.
				// Also, we know there are count objects of that class in the whole database.
				// If we want more than half the objects, or more than 400 total, just load them all.
				if (homogeneousHvos.Length > 400 || homogeneousHvos.Length * 2 > count)
					CmObject.LoadObjectsIntoCache(m_cache, csharpType, null, clid); // load them all
				else
					CmObject.LoadObjectsIntoCache(m_cache, csharpType, homogeneousHvos); // load the exact ones we want.
			}

			m_signature = (ht.Count == 1) ? csharpType : null;
		}

		/// <summary>
		/// Given a query which returns the IDs, classes, and optionally the order of 0 or more objects,
		/// load them into the cache.
		/// </summary>
		/// <remarks>Sets m_hvos, m_signature </remarks>
		/// <remarks>WARNING: this does not necessarily preserve the order implied by the qry (probably not fixable)</remarks>
		/// <remarks>WARNING: after this, objects that the query returns more than once will only appear once in the
		///		ObjectSet's hvos! (could be fixed)(relevant for reference sequences).</remarks>
		/// <param name="sqlQuery">a SQL query returning hvos in the first column, and class IDs in the second,
		/// and optionally the ord. It should be sorted by class ID.
		/// </param>
		/// <param name="fHasOrdinalColumn">True if sqlQuery has a third column, which is for order,
		/// otherwise false.</param>
		/// <param name="fSetResults">True if this methods is to set the m_hvo data member,
		/// otherwise false in which case the caller is assumed to have set it.</param>
		protected void LoadObjectsIntoCache(string sqlQuery, bool fHasOrdinalColumn,
			bool fSetResults)
		{
			LoadObjectsIntoCache(sqlQuery, fHasOrdinalColumn, fSetResults, false);
		}

		/// <summary>
		/// Given a query which returns the IDs, classes, and optionally the order of 0 or more objects,
		/// load them into the cache.
		/// </summary>
		/// <remarks>Sets m_hvos, m_signature </remarks>
		/// <remarks>WARNING: this does not necessarily preserve the order implied by the qry (probably not fixable)</remarks>
		/// <remarks>WARNING: after this, objects that the query returns more than once will only appear once in the
		///		ObjectSet's hvos! (could be fixed)(relevant for reference sequences).</remarks>
		/// <param name="sqlQuery">a SQL query returning hvos in the first column, and class IDs in the second,
		/// and optionally the ord. It should be sorted by class ID.
		/// </param>
		/// <param name="fHasOrdinalColumn">True if sqlQuery has a third column, which is for order,
		/// otherwise false.</param>
		/// <param name="fSetResults">True if this methods is to set the m_hvo data member,
		/// otherwise false in which case the caller is assumed to have set it.</param>
		/// <param name="fJustLoadAllOfType">Set to true if you want to just load all of the objects of the type rather than the ones returned by the query.
		/// </param>
		/// <remarks>Note that since the query also tells us what the types are, we still perform the query.
		/// this may be an area for further enhancement, since we could figure out what the types
		/// are, and forego the query entirely.</remarks>
		///
		protected void LoadObjectsIntoCache(string sqlQuery, bool fHasOrdinalColumn,
			bool fSetResults, bool fJustLoadAllOfType)

		{
			// When we have the cache in a smart mode we suppress preloading objects altogether.
			if (!fHasOrdinalColumn && m_cache.VwOleDbDaAccessor.AutoloadPolicy == AutoloadPolicies.kalpLoadAllOfClassForReadOnly)
				LoadObjectsIntoCache(sqlQuery);
			else
				LoadObjectsIntoCache(sqlQuery, "", fHasOrdinalColumn, fSetResults, fJustLoadAllOfType, false, null);
		}

		/// <summary>
		/// Given a query which returns the IDs, classes, and optionally the order of 0 or more objects,
		/// load them into the cache.
		/// </summary>
		/// <remarks>Sets m_hvos, m_signature </remarks>
		/// <remarks>WARNING: this does not necessarily preserve the order implied by the qry (probably not fixable)</remarks>
		/// <remarks>WARNING: after this, objects that the query returns more than once will only appear once in the
		///		ObjectSet's hvos! (could be fixed)(relevant for reference sequences).</remarks>
		/// <param name="prefix">The first part of an SQL query that will include a list of HVOs.
		/// </param>
		/// <param name="postfix">The last part of an SQL query that will include a list of HVOs.
		/// </param>
		/// <param name="fHasOrdinalColumn">True if the query has a third column, which is for order,
		/// otherwise false.</param>
		/// <param name="fSetResults">True if this methods is to set the m_hvo data member,
		/// otherwise false in which case the caller is assumed to have set it.</param>
		/// <param name="fJustLoadAllOfType">Set to true if you want to just load all of the objects of the type rather than the ones returned by the query.
		/// </param>
		/// <param name="fInsertHvosIntoQuery">Set to true if you want to include a string list
		/// of m_hvos between prefix and postfix.
		/// </param>
		/// <param name="argument">if not null, prefix or postfix contains a '?' which will
		/// be replaced by the argument string.</param>
		/// <remarks>Note that since the query also tells us what the types are, we still perform the query.
		/// this may be an area for further enhancement, since we could figure out what the types
		/// are, and forego the query entirely.</remarks>
		///
		protected void LoadObjectsIntoCache(string prefix, string postfix, bool fHasOrdinalColumn,
			bool fSetResults, bool fJustLoadAllOfType, bool fInsertHvosIntoQuery, string argument)
		{
			if (m_cache.DatabaseName == null || m_cache.DatabaseName == string.Empty)
				return;

			// The SQL commands must NOT modify the database contents!
			//The object of each type must be loaded separately because they have different
			//data and thus will have different queries and columnSpec's.

			// maxGroupSize is the size of m_hvos, if known, otherwise use 500, as a starting spot.
			// If there are more than the default of 500, the int array will be 'grown'
			// to fit them all, before caching is done.
			int maxGroupSize = (m_hvos != null && m_hvos.Length != 0) ? m_hvos.Length : 500;
			Dictionary<int, List<int>> instancesOfClass = new Dictionary<int, List<int>>();
			bool more;
			int collectingClassId = 0;
			List<int> alHomogeneousHvos = new List<int>(maxGroupSize);
			SortedList<uint, int> sortedHvos = fHasOrdinalColumn ? new SortedList<uint, int>(maxGroupSize) : null;
			List<int> unsortedHvos = fHasOrdinalColumn ? null : new List<int>(maxGroupSize);

			string sqlQuery;
			int iNextGroup = 0; // index into m_hvos of next group to insert into query
			if (fInsertHvosIntoQuery)
			{
				sqlQuery = String.Format("{0}{1}{2}", prefix, CmObject.MakePartialIdList(ref iNextGroup, m_hvos), postfix);
			}
			else
			{
				sqlQuery = String.Format("{0}{1}", prefix, postfix);
				iNextGroup = m_hvos.Length; // causes break from for loop after first iteration
			}
			Type csharpType = null;
			IOleDbCommand odc = null;
			try
			{
				for( ; ; )
				{
					m_cache.DatabaseAccessor.CreateCommand(out odc);
					if (argument != null)
					{
						odc.SetStringParameter(1, // 1-based parameter index
							(uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
							null, //flags
							argument,
							(uint)argument.Length); // despite doc, impl makes clear this is char count
					}
					odc.ExecCommand(sqlQuery, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
					odc.GetRowset(0);
					odc.NextRow(out more);
					if (!more)
					{
						m_hvos = new int[0];
						m_signature = null;
						// The finally section will be run, which gets rid of it.
						//System.Runtime.InteropServices.Marshal.ReleaseComObject(odc);
						return;	// No rows selected, so quit.
					}

					// Walk through the results, pre-caching as we collect sets of homogeneous objects
					using(ArrayPtr rgTmp = MarshalEx.ArrayToNative(1, typeof(uint)))
					{
						while (more)
						{
							bool fIsNull;
							uint cbSpaceTaken;
							uint ordinal = 0;

							// odc.GetColValue calls are all 1-based.
							odc.GetColValue(1,
								rgTmp, rgTmp.Size,
								out cbSpaceTaken, out fIsNull, 0);
							uint[] hvosTmp = (uint[])MarshalEx.NativeToArray(rgTmp, 1, typeof(uint));
							int hvo = (int)hvosTmp[0];
							odc.GetColValue(2,
								rgTmp, rgTmp.Size,
								out cbSpaceTaken, out fIsNull, 0);
							uint[] clsIds = (uint[])MarshalEx.NativeToArray(rgTmp, 1, typeof(uint));
							int classId = (int)clsIds[0];
							if (collectingClassId == 0)	// First time through, so set up collectingClassId
								collectingClassId = classId;
							if (fHasOrdinalColumn)
							{
								odc.GetColValue(3,
									rgTmp, rgTmp.Size,
									out cbSpaceTaken, out fIsNull, 0);
								uint[] ordinals = (uint[])MarshalEx.NativeToArray(rgTmp, 1, typeof(uint));
								ordinal = ordinals[0];
								sortedHvos.Add(ordinal, hvo); // ordinal always 0 if not sorted.
							}
							else
								unsortedHvos.Add(hvo);

							if (classId != collectingClassId)
							{
								// Start of a new class of objects.
								// ENHANCE RandyR: The next assert really isn't required,
								// since we could join the the two sets of hvos.
								Debug.Assert(!instancesOfClass.ContainsKey(collectingClassId), "You must order the query by Class$.");
								instancesOfClass.Add(collectingClassId, alHomogeneousHvos);
								alHomogeneousHvos = new List<int>(maxGroupSize);
								collectingClassId = classId;
							}
							alHomogeneousHvos.Add((int)hvo);
							odc.NextRow(out more);
						}
					} // end of processing one sql command
					// Release the command; we're done with it.
					DbOps.ShutdownODC(ref odc);
					if (iNextGroup >= m_hvos.Length)
						break;
					else
						sqlQuery = String.Format("{0}{1}{2}", prefix, CmObject.MakePartialIdList(ref iNextGroup, m_hvos), postfix);
				}
				// Add last (or only) set of hvos to Dictionary.
				Debug.Assert(collectingClassId > 0);
				// ENHANCE RandyR: The next assert really isn't required,
				// since we could join the the two sets of hvos.
				Debug.Assert(!instancesOfClass.ContainsKey(collectingClassId), "You must order the query by Class$.");
				instancesOfClass.Add(collectingClassId, alHomogeneousHvos);
				foreach (KeyValuePair<int, List<int>> kvp in instancesOfClass)
				{
					int clid = kvp.Key;
					csharpType = CmObject.GetTypeFromFWClassID(m_cache, clid);
					if (fJustLoadAllOfType)
						CmObject.LoadObjectsIntoCache(m_cache, csharpType, null, clid);
					else
					{
						alHomogeneousHvos = kvp.Value;
						CmObject.LoadObjectsIntoCache(m_cache, csharpType, alHomogeneousHvos.ToArray());
					}
				}
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}

			m_signature = (instancesOfClass.Count == 1) ? csharpType : null;

			if (fSetResults)
			{
				// Now build a normal integer array for m_hvos.
				if (fHasOrdinalColumn)
				{
					m_hvos = new int[sortedHvos.Count];
					sortedHvos.Values.CopyTo(m_hvos, 0);
				}
				else
				{
					m_hvos = unsortedHvos.ToArray();
				}
			}
		}

		/// <summary>
		/// A minimal version that just loads the object sequence and class IDs.
		/// </summary>
		/// <param name="sqlQuery"></param>
		protected void LoadObjectsIntoCache(string sqlQuery)
		{
			if (m_cache.DatabaseName == null || m_cache.DatabaseName == string.Empty)
				return;

			IVwCacheDa cda = m_cache.VwCacheDaAccessor;

			List<int> results = new List<int>();
			int uniformClid = -1;
			IOleDbCommand odc = null;
			try
			{
				m_cache.DatabaseAccessor.CreateCommand(out odc);
				odc.ExecCommand(sqlQuery, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
				odc.GetRowset(0);
				bool more;
				odc.NextRow(out more);
				if (!more)
				{
					m_hvos = new int[0];
					m_signature = null;
					// The finally section will be run, which gets rid of it.
					//System.Runtime.InteropServices.Marshal.ReleaseComObject(odc);
					return;	// No rows selected, so quit.
				}

				using(ArrayPtr rgTmp = MarshalEx.ArrayToNative(1, typeof(uint)))
				{

					while (more)
					{
						bool fIsNull;
						uint cbSpaceTaken;

						// odc.GetColValue calls are all 1-based.
						odc.GetColValue(1,
							rgTmp,
							(uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint)),
							out cbSpaceTaken, out fIsNull, 0);
						uint[] hvosTmp = (uint[])MarshalEx.NativeToArray(rgTmp, 1, typeof(uint));
						int hvo = (int)hvosTmp[0];
						odc.GetColValue(2,
							rgTmp,
							(uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint)),
							out cbSpaceTaken, out fIsNull, 0);
						uint[] clsIds = (uint[])MarshalEx.NativeToArray(rgTmp, 1, typeof(uint));
						int classId = (int)clsIds[0];
						if (uniformClid == -1)
							uniformClid = classId;
						else if (uniformClid != classId)
							uniformClid = 0;
						results.Add(hvo);
						cda.CacheIntProp(hvo, (int)CmObjectFields.kflidCmObject_Class, classId);
						odc.NextRow(out more);
					}
				}
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}

			m_signature = (uniformClid == 0) ? null : CmObject.GetTypeFromFWClassID(m_cache, uniformClid);

			m_hvos = DbOps.ListToIntArray(results);
		}

		#endregion Misc methods
	}

	#endregion	// Enumerators
}
