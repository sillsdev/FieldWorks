// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CopyObject.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces; // For ISilDataAccess
using SIL.FieldWorks.FDO.Application; // For ISilDataAccessManaged
using SIL.FieldWorks.FDO.Infrastructure; // For IFwMetaDataCacheManaged

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A class to facilitate deep copying of generic FDO objects.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class CopyObject<TTopLevel> where TTopLevel : ICmObject
	{
		/// <summary>
		/// This is a default delegate for adding the copied object to the source owner.
		/// </summary>
		public static readonly CopiedOwner<TTopLevel> kAddToSourceOwner = x => Debug.Assert(x.IsValidObject);

		internal delegate void CopiedOwner<TObj>(TObj obj) where TObj : ICmObject;

		private readonly FdoCache m_cache;
		private readonly IFdoServiceLocator m_servLoc;
		private readonly IFwMetaDataCacheManaged m_mdc;
		private readonly ISilDataAccess m_sda;
		private readonly CopiedOwner<TTopLevel> m_topLevelOwnerFunct;
		private ICmObject m_topLevelObj;

		// Source FDO object to Copy FDO object map; Key = source HVO, Value = Copied object
		private readonly Dictionary<int, ICmObject> m_sourceToCopyMap = new Dictionary<int, ICmObject>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CopyObject&lt;TTopLevel&gt;"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="topLevelOwnerFunct">The delegate used for adding the created object to
		/// an owner.</param>
		/// ------------------------------------------------------------------------------------
		private CopyObject(FdoCache cache, CopiedOwner<TTopLevel> topLevelOwnerFunct)
		{
			m_cache = cache;
			m_servLoc = m_cache.ServiceLocator;
			m_sda = m_cache.DomainDataByFlid;
			m_mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			m_topLevelOwnerFunct = topLevelOwnerFunct;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do a deep copy of a single FDO object and all owned objects.
		/// 'Outside' references are duplicated. 'Inside' ones are set to the new copies.
		/// Basic/value properties are copied unchanged.
		/// </summary>
		/// <param name="source">FDO object</param>
		/// <param name="ownerFunct">The delegate used for adding the created object to an owner.
		/// </param>
		/// <returns>The copy</returns>
		/// ------------------------------------------------------------------------------------
		internal static TTopLevel CloneFdoObject(TTopLevel source, CopiedOwner<TTopLevel> ownerFunct)
		{
			CopyObject<TTopLevel> copyObject = new CopyObject<TTopLevel>(source.Cache, ownerFunct);
			TTopLevel newObj = copyObject.CloneObjectInternal(source);
			return newObj;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do a deep copy of a collection of FDO objects and all owned objects.
		/// 'Outside' references are duplicated. 'Inside' ones are set to the new copies.
		/// Basic/value properties are copied unchanged.
		/// </summary>
		/// <param name="source">Collection or Sequence of FDO objects to copy</param>
		/// <param name="ownerFunct">The delegate used for adding the created object to an owner.
		/// </param>
		/// <returns>Returned object is a List of ICmObjects, internally.</returns>
		/// ------------------------------------------------------------------------------------
		internal static IEnumerable<TTopLevel> CloneFdoObjects(IEnumerable<TTopLevel> source,
			CopiedOwner<TTopLevel> ownerFunct)
		{
			if (source.Count() == 0)
				return null;
			CopyObject<TTopLevel> copyObject = new CopyObject<TTopLevel>(source.First().Cache, ownerFunct);
			return copyObject.CloneObjectsInternal(source);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Actually does the work of cloning
		/// </summary>
		/// <param name="source">The source objects to copy.</param>
		/// <returns>Returned object is a List of ICmObjects, internally.</returns>
		/// ------------------------------------------------------------------------------------
		private IEnumerable<TTopLevel> CloneObjectsInternal(IEnumerable<TTopLevel> source)
		{
			// Copy all input objects including owned objects recursively
			List<TTopLevel> result = new List<TTopLevel>();
			foreach (TTopLevel obj in source)
			{
				m_topLevelObj = obj;
				result.Add(CloneFdoObjectsRecursively(obj));
			}

			// Set all the reference objects in the copies of these FDO objects
			foreach (TTopLevel srcObj in source)
				SetReferencesRecursively(srcObj);

			// Allow copied object to set side effects
			foreach (TTopLevel srcObj in source)
				srcObj.PostClone(m_sourceToCopyMap);

			// return the new FDO object
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Actually does the work of cloning
		/// </summary>
		/// <param name="source">The source object to copy.</param>
		/// <returns>The copy</returns>
		/// ------------------------------------------------------------------------------------
		private TTopLevel CloneObjectInternal(TTopLevel source)
		{
			m_topLevelObj = source;
			// Copy the input object including owned objects recursively
			CloneFdoObjectsRecursively(source);

			// Set all the reference objects in the copy of this FDO object
			SetReferencesRecursively(source);

			// Allow copied object to set side effects
			source.PostClone(m_sourceToCopyMap);

			return (TTopLevel)m_sourceToCopyMap[source.Hvo]; // if no copy of source is found... throws
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the heart of CopyObject Pass 2.
		/// Sets the references recursively. Needs to go down recursively into
		/// all of the source object's owned properties looking for Reference
		/// properties, since those owned properties will have just been copied too.
		/// </summary>
		/// <param name="source">The source.</param>
		/// ------------------------------------------------------------------------------------
		private void SetReferencesRecursively(ICmObject source)
		{
			// Get the copy of our source object in which to set references.
			ICmObject newObj;
			int hvoSrc = source.Hvo;
			if (!m_sourceToCopyMap.TryGetValue(hvoSrc, out newObj))
				throw new ArgumentOutOfRangeException("source", "Can't find copy of original object in copy map.");

			if (source is ICloneableCmObject)
				return; // Assume that the object handled this itself

			// TODO: Copy references from source object to copied object unless reference is to something in the sourceToCopyMap
			// in that case replace it with a reference to the copied version.
			// We'll need to go down inside of all owned objects to do this too, since they were just copied.
			int srcClsID = source.ClassID;
			int hvoCopy = newObj.Hvo;

			int[] srcFlids = GetAllFieldsFromClassId(srcClsID);

			// Clone each owned flid and copy basic properties
			for (int i = 0; i < srcFlids.Length; i++)
			{
				int thisFlid = srcFlids[i];
				// If thisFlid is part of CmObject, this Flid of copied object has
				//   already been correctly set on creation.
				// Also skip virtual fields; let the object handle its own virtuals.
				if (thisFlid < 200 || m_mdc.get_IsVirtual(thisFlid))
					continue;

				// Skip basic/string properties this pass
				int flidType = m_mdc.GetFieldType(thisFlid);
				if (flidType < (int)CellarPropertyType.MinObj)
					continue;

				// Only have reference and owned props left
				if (m_cache.IsReferenceProperty(thisFlid))
					SetReferencesForReferenceFlid(thisFlid, flidType, hvoSrc, hvoCopy);
				else
					SetReferencesForOwnedFlid(thisFlid, flidType, hvoSrc);
			}
		}

		private void SetReferencesForReferenceFlid(int thisFlid, int flidType, int hvoSrc, int hvoCopy)
		{
			switch (flidType)
			{
				case (int)CellarPropertyType.ReferenceAtomic:
					int hvoAtomic = m_sda.get_ObjectProp(hvoSrc, thisFlid);
					if (hvoAtomic > 0)
					{
						// If we find the object referred to by the RA property in our copy map,
						// put a reference to its copy in our copied object. Otherwise, use the same
						// reference as our source object.
						ICmObject copiedAtomic;
						if (m_sourceToCopyMap.TryGetValue(hvoAtomic, out copiedAtomic))
							m_sda.SetObjProp(hvoCopy, thisFlid, copiedAtomic.Hvo);
						else
							m_sda.SetObjProp(hvoCopy, thisFlid, hvoAtomic);
					}
					break;
				case (int)CellarPropertyType.ReferenceCollection:
				case (int)CellarPropertyType.ReferenceSequence:
					// Handle Reference Vectors
					int cVec = m_sda.get_VecSize(hvoSrc, thisFlid);
					for (int i = 0; i < cVec; i++)
					{
						int hvoVecItem = m_sda.get_VecItem(hvoSrc, thisFlid, i);
						ICmObject copiedVecItem;
						if (m_sourceToCopyMap.TryGetValue(hvoVecItem, out copiedVecItem))
							m_sda.Replace(hvoCopy, thisFlid, i, i, new[] {copiedVecItem.Hvo}, 1);
						else
							m_sda.Replace(hvoCopy, thisFlid, i, i, new[] {hvoVecItem}, 1);
					}
					break;
				default:
					throw new ArgumentException("Non-reference Field in wrong method!", "flidType");
			}
		}

		private void SetReferencesForOwnedFlid(int thisFlid, int flidType, int hvoSrc)
		{
			switch (flidType)
			{
				case (int)CellarPropertyType.OwningAtomic:
					int hvoAtomic = m_sda.get_ObjectProp(hvoSrc, thisFlid);
					if (hvoAtomic > 0)
					{
						ICmObject srcAtomic = m_servLoc.ObjectRepository.GetObject(hvoAtomic);
						if (srcAtomic != null)
							SetReferencesRecursively(srcAtomic);
					}
					break;
				case (int)CellarPropertyType.OwningCollection:
				case (int)CellarPropertyType.OwningSequence:
					// Handle Owned Vectors
					int cVec = m_sda.get_VecSize(hvoSrc, thisFlid);
					for (int i = 0; i < cVec; i++)
					{
						int hvoVecItem = m_sda.get_VecItem(hvoSrc, thisFlid, i);
						ICmObject srcVecItem = m_servLoc.ObjectRepository.GetObject(hvoVecItem);
						if (srcVecItem != null)
							SetReferencesRecursively(srcVecItem);
					}
					break;
				default:
					throw new ArgumentException("Unowned Field in wrong method!", "flidType");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clones all owned objects and source objects (OA, OS, OC)
		/// Does nothing with reference objects (RA, RS, RC), these are in 2nd pass
		/// </summary>
		/// <typeparam name="TObj">The type of object</typeparam>
		/// <param name="source">Collection of FDO objects to clone</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IEnumerable<TObj> CloneFdoObjectsRecursively<TObj>(IEnumerable<TObj> source)
			where TObj : ICmObject
		{
			List<TObj> newHvoColl = new List<TObj>();

			foreach (TObj obj in source)
				newHvoColl.Add(CloneFdoObjectsRecursively(obj));

			return newHvoColl;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the heart of CopyObject Pass 1.
		/// Clones all owned objects (OA, OS, OC) and the source FDO object.
		/// Does nothing with reference objects (RA, RS, RC), these are in 2nd pass.
		/// Basic properties are just copied straight across. Returns the new object.
		/// </summary>
		/// <typeparam name="TObj">The type of object</typeparam>
		/// <param name="srcObj">FDO object to clone</param>
		/// <returns>The cloned object</returns>
		/// ------------------------------------------------------------------------------------
		private TObj CloneFdoObjectsRecursively<TObj>(TObj srcObj) where TObj : ICmObject
		{
			if (srcObj == null)
				return srcObj;

			int srcClsID = srcObj.ClassID;
			// Review: Is this necessary? How could this ever be true?
			if (m_mdc.GetAbstract(srcClsID))
				throw new ArgumentNullException("source", "Source object is Abstract; not copyable!");
			IFdoFactoryInternal srcFactory = (IFdoFactoryInternal)m_servLoc.GetInstance(
				GetServicesFromFWClass.GetFactoryTypeFromFWClassID(m_mdc, srcClsID));
			if (srcFactory == null)
				throw new FDOObjectUninitializedException(String.Format("Failed to find a Factory to create {0}.", srcObj));

			bool fIsTopLevel = ((ICmObject)srcObj == (ICmObject)m_topLevelObj);
			bool fUnOwned = (srcObj.Owner == null);

			ICmObject newObj;
			if (fUnOwned)
				newObj = CreateUnownedObj<TObj>(srcFactory, srcClsID);
			else
			{
				Debug.Assert(!fIsTopLevel || m_topLevelOwnerFunct != null, "An owned top-level object must have a owner function passed in");
				newObj = (fIsTopLevel && m_topLevelOwnerFunct != kAddToSourceOwner) ?
					(ICmObject)CreateTopLevelOwnedObj(srcFactory) :
					CreateOwnedObj(m_servLoc.ObjectRepository, srcObj);
			}
			if (newObj == null)
				throw new FDOObjectUninitializedException(String.Format("Failed to create a healthy FDO Object as a copy of {0}.", srcObj));

			// Record copy in sourceToCopyMap
			int hvoSrc = srcObj.Hvo;
			int hvoNew = newObj.Hvo;
			// Review: This should work okay even if we modify the contents of newObj later, right?
			m_sourceToCopyMap.Add(hvoSrc, newObj);

			if (srcObj is ICloneableCmObject)
			{
				Debug.Assert(newObj is ICloneableCmObject);
				((ICloneableCmObject)srcObj).SetCloneProperties(newObj);
				return (TObj)newObj;
			}

			int[] srcFlids = GetAllFieldsFromClassId(srcClsID);

			// Clone each owned flid and copy basic properties
			var owningInfo = new List<KeyValuePair<int, int>>();
			for (int i = 0; i < srcFlids.Length; i++)
			{
				int thisFlid = srcFlids[i];
				// If thisFlid is part of CmObject, this Flid's already been set on creation
				// Skip reference properties this pass
				// If Flid is Virtual, skip (let the new object determine its own virtual stuff)
				if (thisFlid < 200 || m_cache.IsReferenceProperty(thisFlid) ||
					m_mdc.get_IsVirtual(thisFlid))
					continue;

				int flidType = m_mdc.GetFieldType(thisFlid);
				if (flidType < (int)CellarPropertyType.MinObj)
					HandleBasicOrStringFlid(thisFlid, flidType, hvoSrc, hvoNew);
				else
					// No. Just store the owned stuff, for now.
					// The master xml file now has all props in numerical order,
					// so all basic props has to be copied first.
					//HandleObjFlid(thisFlid, flidType, hvoSrc);
					owningInfo.Add(new KeyValuePair<int, int>(thisFlid, flidType));
			}
			// Now process the owned stuff.
			foreach (var kvp in owningInfo)
				HandleObjFlid(kvp.Key, kvp.Value, hvoSrc);

			return (TObj)newObj;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new top-level owned object.
		/// </summary>
		/// <param name="srcFactory">The factory for creating the object.</param>
		/// <returns>The created object</returns>
		/// ------------------------------------------------------------------------------------
		private TTopLevel CreateTopLevelOwnedObj(IFdoFactoryInternal srcFactory)
		{
			// This is the top-level object we are copying so we need to add it to its
			// correct owner.
			TTopLevel newObj = (TTopLevel)srcFactory.CreateInternal();
			m_topLevelOwnerFunct(newObj);
			return newObj;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new owned object
		/// </summary>
		/// <typeparam name="TObj">The type of object</typeparam>
		/// <param name="srcRepository">The repository that holds the object.</param>
		/// <param name="srcObj">The object (used to create a new object of the same type).</param>
		/// <returns>The created object</returns>
		/// ------------------------------------------------------------------------------------
		private TObj CreateOwnedObj<TObj>(ICmObjectRepository srcRepository, TObj srcObj)
			where TObj : ICmObject
		{
			if (srcObj.Owner == null)
				throw new ArgumentException("Can't create Owned Copy of unowned original!", "srcObj");
			int owningFlidType = m_mdc.GetFieldType(srcObj.OwningFlid);

			// Determine appropriate owner of copy
			// If source's owner has also been copied, we want to add this copy to the copy!
			// If a delegate has not been set, use the source's owner as the copy's owner.
			ICmObject copied;
			if (!m_sourceToCopyMap.TryGetValue(srcObj.Owner.Hvo, out copied))
				copied = (m_topLevelOwnerFunct == kAddToSourceOwner) ? srcObj.Owner : null;

			Debug.Assert(copied != null, "Non top-level objects should have their owner already copied");

			int ord;
			switch (owningFlidType)
			{
				case (int)CellarPropertyType.OwningAtomic:
					ord = -2;
					break;
				case (int)CellarPropertyType.OwningCollection:
					ord = -1;
					break;
				case (int)CellarPropertyType.OwningSequence:
					// put copy at end of sequence until told otherwise
					ord = m_sda.get_VecSize(copied.Hvo, srcObj.OwningFlid);
					break;
				default:
					throw new NotImplementedException("Unsupported owningFlidType.");
			}

			int newHvo = m_sda.MakeNewObject(srcObj.ClassID, copied.Hvo, srcObj.OwningFlid, ord);
			return (TObj)srcRepository.GetObject(newHvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new unowned object
		/// </summary>
		/// <typeparam name="TObj">The type of object</typeparam>
		/// <param name="srcFactory">The factory used to create the object</param>
		/// <param name="classId">The class id of the object</param>
		/// <returns>The created object</returns>
		/// ------------------------------------------------------------------------------------
		private TObj CreateUnownedObj<TObj>(IFdoFactoryInternal srcFactory, int classId)
			where TObj : ICmObject
		{
			TObj obj = (TObj)srcFactory.CreateInternal();

			if (!obj.IsValidObject)
				throw new ArgumentException("Factory failed to create a valid object", "srcFactory");

			return obj;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the flids for basic data (bool, string, int, etc.)
		/// </summary>
		/// <param name="thisFlid">The object type flid.</param>
		/// <param name="flidType">The owning flid type.</param>
		/// <param name="hvoSrc">The hvo of the source object.</param>
		/// <param name="hvoNew">The hvo of the new object.</param>
		/// ------------------------------------------------------------------------------------
		private void HandleBasicOrStringFlid(int thisFlid, int flidType, int hvoSrc, int hvoNew)
		{
			// Basic and String properties are copied
			switch (flidType)
			{
				// Basic Properties follow

				case (int)CellarPropertyType.Binary:
					string flidName = m_mdc.GetFieldName(thisFlid);
					if (flidName == "Rules" || flidName == "StyleRules")
						CopyBinaryAsTextProps(thisFlid, hvoSrc, hvoNew);
					else
						CopyBinaryAsBinary(thisFlid, hvoSrc, hvoNew);
					break;
				case (int)CellarPropertyType.Boolean:
					// Copy boolean value
					m_sda.SetBoolean(hvoNew, thisFlid, m_sda.get_BooleanProp(hvoSrc, thisFlid));
					break;
				case (int)CellarPropertyType.Guid:
					// Copy guid value
					// These are currently used as the ID for an application, or a version number, or a Scripture Check ID)
					m_sda.SetGuid(hvoNew, thisFlid, m_sda.get_GuidProp(hvoSrc, thisFlid));
					break;
				case (int)CellarPropertyType.GenDate: // Fall through, since a GenDate is an int.
				case (int)CellarPropertyType.Integer:
					// Copy integer value
					m_sda.SetInt(hvoNew, thisFlid, m_sda.get_IntProp(hvoSrc, thisFlid));
					break;
				case (int)CellarPropertyType.Time:
					// Copy time value
					m_sda.SetTime(hvoNew, thisFlid, m_sda.get_TimeProp(hvoSrc, thisFlid));
					break;

				// String Properties follow

				case (int)CellarPropertyType.String:
					// Copy string value
					// Review: Please check these next three!
					m_sda.SetString(hvoNew, thisFlid, m_sda.get_StringProp(hvoSrc, thisFlid));
					break;
				case (int)CellarPropertyType.Unicode:
					// Copy Unicode string
					m_sda.set_UnicodeProp(hvoNew, thisFlid, m_sda.get_UnicodeProp(hvoSrc, thisFlid));
					break;
				case (int)CellarPropertyType.MultiString: // Fall through
				case (int)CellarPropertyType.MultiUnicode:
					ITsMultiString sMulti = m_sda.get_MultiStringProp(hvoSrc, thisFlid);
					for (int i = 0; i < sMulti.StringCount; i++)
					{
						int ws;
						ITsString tss = sMulti.GetStringFromIndex(i, out ws);
						m_sda.SetMultiStringAlt(hvoNew, thisFlid, ws, tss);
					}
					break;
				default:
					throw new FDOInvalidFieldTypeException(String.Format("CopyObject: Unsupported field type {0}.", flidType));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies a "binary" Flid as actual binary bytes.
		/// </summary>
		/// <param name="thisFlid">The this flid.</param>
		/// <param name="hvoSrc">The hvo SRC.</param>
		/// <param name="hvoNew">The hvo new.</param>
		/// ------------------------------------------------------------------------------------
		private void CopyBinaryAsBinary(int thisFlid, int hvoSrc, int hvoNew)
		{
			byte[] bdata;
			var cbytes = ((ISilDataAccessManaged)m_sda).get_Binary(hvoSrc, thisFlid, out bdata);
			m_sda.SetBinary(hvoNew, thisFlid, bdata, cbytes);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies a "binary" Flid as ITsTextProps.
		/// </summary>
		/// <param name="thisFlid">The this flid.</param>
		/// <param name="hvoSrc">The hvo SRC.</param>
		/// <param name="hvoNew">The hvo new.</param>
		/// ------------------------------------------------------------------------------------
		private void CopyBinaryAsTextProps(int thisFlid, int hvoSrc, int hvoNew)
		{
			ITsTextProps txtProps = (ITsTextProps)m_sda.get_UnknownProp(hvoSrc, thisFlid);
			// If the above line happens to return null (either it WAS null or it WASN'T an ITsTextProps)
			// then this line will set the copy to null.
			m_sda.SetUnknown(hvoNew, thisFlid, txtProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the object flids (OA, OC, OS).
		/// </summary>
		/// <param name="thisFlid">The object type flid.</param>
		/// <param name="flidType">The owning flid type.</param>
		/// <param name="hvoSrc">The hvo of the source object.</param>
		/// ------------------------------------------------------------------------------------
		private void HandleObjFlid(int thisFlid, int flidType, int hvoSrc)
		{
			// Basic, String and Reference flids should not get here.
			switch (flidType)
			{
				// Owned Properties are recursively cloned
				case (int)CellarPropertyType.OwningAtomic:
					// Clone owned atomic object (and its owned children)
					int hvoAtomic = m_sda.get_ObjectProp(hvoSrc, thisFlid);
					if (hvoAtomic > 0)
					{
						ICmObject srcAtomic = m_servLoc.ObjectRepository.GetObject(hvoAtomic);
						if (srcAtomic != null)
							CloneFdoObjectsRecursively(srcAtomic);
					}
					break;
				case (int)CellarPropertyType.OwningCollection:
				case (int)CellarPropertyType.OwningSequence:
					// Clone owned sequence and owned collection (and their owned children)
					// We can use a list for both cases. Since collections are unordered,
					// it won't matter if they are in an ordered list or an unordered set.
					int cSeq = m_sda.get_VecSize(hvoSrc, thisFlid);
					List<ICmObject> srcSeq = new List<ICmObject>();
					for (int i = 0; i < cSeq; i++)
					{
						int hvoItem = m_sda.get_VecItem(hvoSrc, thisFlid, i);
						srcSeq.Add(m_servLoc.ObjectRepository.GetObject(hvoItem));
					}
					CloneFdoObjectsRecursively((IEnumerable<ICmObject>)srcSeq);
					break;

				default:
					throw new FDOInvalidFieldTypeException(String.Format("CopyObject: Unsupported field type {0}.", flidType));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an array of all the Field tags (flids) for the specified class.
		/// NB. Must run Init method at least once before using this method.
		/// </summary>
		/// <param name="clsid">The CLSID.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private int[] GetAllFieldsFromClassId(int clsid)
		{
			return m_mdc.GetFields(clsid, true, (int)CellarPropertyTypeFilter.All);
		}
	}
}
