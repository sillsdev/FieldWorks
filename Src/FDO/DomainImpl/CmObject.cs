// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CmObject.cs
// Responsibility: Randy Regnier
// Last reviewed: never
//
//
// <remarks>
// Implementation of:
//		CmObject : Object
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.IO; // MemoryStream.
using System.Xml.Linq;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// <summary>
	/// Class corresponding to the CmObject type in the FieldWorks database.
	/// </summary>
	/// <remarks>
	/// The FDOGenerated.cs file contains the generated
	/// back reference interface properties.
	/// </remarks>
	internal partial class CmObject : ICmObjectInternal, ICmObjectOrIdInternal, ICmObjectOrSurrogate, IReferenceSource
	{
		#region Data Members

		/// <summary> The FDO cache.</summary>
		protected FdoCache m_cache;
		/// <summary>The HVO of this object</summary>
		protected int m_hvo = (int)SpecialHVOValues.kHvoUninitializedObject;
		/// <summary>The owner. It may be null for objects with no owner.</summary>
		private ICmObjectOrId m_owner;
		/// <summary>The ID of this object</summary>
		protected ICmObjectId m_guid;
		/// <summary>
		/// Contains all known CmObjects with atomic refs to this object, and all, FdoReferenceCollections and FdoReferenceSequences
		/// which contain it. Objects which have not been fluffed (and their collections and sequences) up are not included.
		/// Colletions and Sequences which have not been fully fluffed may not be included. (A potential source is included
		/// iff it has a pointer to the actual object, rather than one to its ObjectId.)
		/// </summary>
		internal SimpleBag<IReferenceSource> m_incomingRefs;

		#endregion Data Members

		#region Property accessors
		/// <summary>
		/// This is useful for the purpose of creating a 'virtual' attribute which displays the same object as if it
		/// were one of its own properties.
		/// PropChanged generation is not necessary. The value of this virtual cannot change.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceAtomic, "CmObject")]
		public ICmObject Self
		{
			get { return this; }
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets delete status for the object.
		/// True means it can be deleted, otherwise false.
		/// An object cannot be deleted if it is already in the process of being deleted.
		/// (This suppresses side effects of ClearIncomingReferences, which otherwise tend to
		/// trigger recursive attempts to delete the object when they detect that it can be
		/// deleted because there are no remaining references to it.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool CanDelete
		{
			get { return !Cache.ObjectsBeingDeleted.Contains(this); }
		}

		/// <summary>
		/// Object owner. May be null.
		/// </summary>
		[VirtualProperty(CellarPropertyType.ReferenceAtomic, "CmObject")]
		public ICmObject Owner
		{
			get
			{
				lock (SyncRoot)
				{
					if (m_owner is ICmObjectId)
						m_owner = m_owner.GetObject(Services.ObjectRepository);
				}
				return m_owner as ICmObject;
			}
		}

		/// <summary>
		/// Get the owner's guid. If owner is not null, this is equivalent to Owner.Guid, except
		/// that it does not force an objectID to be replaced by the full object, which can fail
		/// if we are in the process of deleting the owner.
		/// </summary>
		internal Guid OwnerGuid
		{
			get
			{
				if (m_owner == null)
					return Guid.Empty;
				return m_owner.Id.Guid;
			}
		}

		/// <summary>
		/// Main cache class.
		/// </summary>
		public FdoCache Cache
		{
			get { return m_cache; }
			set
			{
				if (m_cache != null)
					throw new InvalidOperationException("Already have a cache.");
				if (value == null)
					throw new ArgumentException("New value is null.");
				m_cache = value;
			}
		}

		/// <summary>
		/// The objects directly owned by this one.
		/// </summary>
		public IEnumerable<ICmObject> OwnedObjects
		{
			get
			{
				var mdc = (IFwMetaDataCacheManaged)Cache.MetaDataCache;
				var flids = mdc.GetFields(this.ClassID, true, (int)CellarPropertyTypeFilter.AllOwning);
				foreach (var flid in flids)
				{
					var flidType = mdc.GetFieldType(flid);
					switch (flidType)
					{
						case (int)CellarPropertyType.OwningAtomic:
						{
							var hvo = m_cache.DomainDataByFlid.get_ObjectProp(this.Hvo, flid);
							if (hvo != 0)
								yield return m_cache.ServiceLocator.GetObject(hvo);
							break;
						}
						case (int)CellarPropertyType.OwningSequence:
						case (int)CellarPropertyType.OwningCollection:
						{
							var hvos = ((ISilDataAccessManaged)m_cache.DomainDataByFlid).VecProp(this.Hvo, flid);
							foreach (var hvo in hvos)
								yield return m_cache.ServiceLocator.GetObject(hvo);
							break;
						}
					}
				}
			}
		}

		/// <summary>
		/// The objects owned directly or indirectly by this one.
		/// </summary>
		public IEnumerable<ICmObject> AllOwnedObjects
		{
			get
			{
				foreach (var obj in OwnedObjects)
				{
					yield return obj;
					foreach (var child in obj.AllOwnedObjects)
						yield return child;
				}
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Id of the object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Hvo
		{
			get { return m_hvo; }
		}

		/// <summary>
		/// Field ID of the owning object where the object is stored.
		/// </summary>
		[ModelProperty(CellarPropertyType.Integer, (int)CmObjectFields.kflidCmObject_OwnFlid, "Integer")]
		public int OwningFlid
		{
			get
			{
				int dummy;
				return OwningFlidAndIndex(false, out dummy);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Mainly used to obtain both owning flid and the index in the owning collection.
		/// If wantIndex is false or if the longest property is a collection, it is allowed
		/// to assume that the longest owning sequence or collection in the owner contains the
		/// object, if it is not found in a shorter sequence or atomic property, and if the
		/// signature of that property allows it.
		/// </summary>
		/// <param name="wantIndex"><c>true</c> if the index in the owning collection is wanted;
		/// <c>false</c> otherwise</param>
		/// <param name="index">[out] the index of this CmObject in its owning collection.
		/// Index will be -3 if no actual search was done, -2 for atomic, -1 for collections,
		/// or the actual index.</param>
		/// <returns>the field id of the owning object, or 0 if the owning object is null or
		/// cannot be found in any property of its owner.</returns>
		/// ------------------------------------------------------------------------------------
		internal int OwningFlidAndIndex(bool wantIndex, out int index)
		{
			ICmObject owner = Owner;
			index = -3; // a default, signifying we didn't locate it.
			if (owner == null)
				return 0;
			IFwMetaDataCacheManaged mdc = Services.GetInstance<IFwMetaDataCacheManaged>();
			int ownerClass = owner.ClassID;
			// First see if it is one of the atomic properties.
			var ownerInternal = (ICmObjectInternal)owner;
			foreach (int flid in mdc.GetFields(ownerClass, true, (int)CellarPropertyTypeFilter.OwningAtomic))
				if ((!mdc.get_IsVirtual(flid)) && ownerInternal.GetObjectProperty(flid) == this.Hvo)
				{
					if (wantIndex)
						index = -2;
					return flid;
				}
			// and now collections and sequences...
			// If it's not found in any shorter sequence, assume it is in the longest.
			// This saves a good deal of time, though it may lead to our missing some errors.
			int longest = 0;
			int flidLongest = 0;
			foreach (int flid in mdc.GetFields(ownerClass, true,
				(int)(CellarPropertyTypeFilter.OwningSequence | CellarPropertyTypeFilter.OwningCollection)))
			{
				if (mdc.get_IsVirtual(flid))
					continue; // virtual properties should never be considered owning, but for paranoia...
				int cobj = ownerInternal.GetVectorSize(flid);
				if (cobj > longest)
				{
					// If we had a prior longest, we must evaluate it after all.
					if (flidLongest != 0)
						if (DoesPropContainThis(ownerInternal, flidLongest, mdc, wantIndex, out index))
							return flidLongest;
					longest = cobj;
					flidLongest = flid;
					continue;
				}
				// This is a shorter (or equal) vector to the current longest, check it out now.
				if (DoesPropContainThis(ownerInternal, flid, mdc, wantIndex, out index))
					return flid;
			}
			if (flidLongest == 0)
				return 0;
			if (wantIndex)
			{
				if (mdc.GetFieldType(flidLongest) == (int)CellarPropertyType.OwningSequence)
					if (DoesPropContainThis(ownerInternal, flidLongest, mdc, true, out index))
						return flidLongest;
					else
						return 0; // not in any flid.
				else
					index = -1; // marks set; fall through to just return longest.
			}
			if (!mdc.get_IsValidClass(flidLongest, this.ClassID))
				return 0; // it can't be in this property, so isn't anywhere.
			return flidLongest;       // assume it is in the longest property.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if the specified property of the owner contains this.
		/// If wantIndex is true, also return the index (or -1 if a set)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool DoesPropContainThis(ICmObjectInternal ownerInternal, int flid, IFwMetaDataCacheManaged mdc, bool wantIndex, out int index)
		{
			index = -3; // not evaluated.
			if (!mdc.get_IsValidClass(flid, this.ClassID))
				return false; // it can't be in this property.
			var collection = ownerInternal.GetVectorProperty(flid);
			if (!wantIndex)
				return (collection.Contains(this));
			if (mdc.GetFieldType(flid) == (int)CellarPropertyTypeFilter.OwningCollection)
			{
				if (collection.Contains(this))
				{
					index = -1;
					return true;
				}
				return false;
			}
			// owning sequence, and we want the index.
			int temp = Array.IndexOf(collection.ToArray(), this);
			if (temp == -1)
				return false;
			index = temp;
			return true;
		}

		/// <summary>
		/// Owning ord of the owning object where the object is stored. -1 if not in any property. 0 for atomic or set.
		/// </summary>
		[ModelProperty(CellarPropertyType.Integer, (int)CmObjectFields.kflidCmObject_OwnOrd, "Integer")]
		public int OwnOrd
		{
			get
			{
				int result;
				if (OwningFlidAndIndex(true, out result) == 0)
					return -1; // not found.
				if (result < 0)
					return 0; // -1 or -2 indicates atomic or collection, but OwnOrd's contract calls for 0.
				return result;
			}
		}

		/// <summary>
		/// Unique ID of the object. If this will be retained anywhere it could consume memory,
		/// it is probably preferable to use the Id.
		/// </summary>
		[ModelProperty(CellarPropertyType.Guid, (int)CmObjectFields.kflidCmObject_Guid, "Guid")]
		public Guid Guid
		{
			get { return m_guid == null ? Guid.Empty : m_guid.Guid; }
		}

		/// <summary>
		/// The preferred form of the object ID.
		/// </summary>
		public ICmObjectId Id
		{
			get { return m_guid; }
		}

		/// <summary>
		/// Gets the synchronization root. This is the object that should be
		/// used for all locking in this CmObject. Used for locking all lazy
		/// initialized properties, since they can change during FDO reads.
		/// </summary>
		/// <value>The synchronization root.</value>
		protected object SyncRoot
		{
			get
			{
				return this;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an alphabetic version of SortKey2. This should always be used when appending
		/// to another string sort key, so as to get the right order for values greater than 9.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string SortKey2Alpha
		{
			get
			{
				var val = SortKey2;
				if (val < 0)
				{
					var sVal = (0 - val).ToString();
					return "-" + new String('0', 11 - sVal.Length) + sVal;
				}
				else
				{
					var sVal = val.ToString();
					return new String('0', 11 - sVal.Length) + sVal;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sort key for sorting a list of ShortNames.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string SortKey
		{
			get { return ShortName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system for sorting a list of ShortNames.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string SortKeyWs
		{
			get
			{
				var sWs = PreferredWsId;
				if (string.IsNullOrEmpty(sWs))
					sWs = "en";
				return sWs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a secondary sort key for sorting a list of ShortNames.  Defaults to zero.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int SortKey2
		{
			get { return 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a TsString suitable for use in a chooser.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property, if they want to show something other than
		/// the regular ShortNameTSS string.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual ITsString ChooserNameTS
		{
			get { return ShortNameTSS; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine whether the object is valid
		/// (i.e., has its cache (not disposed) and an hvo greater than zero).
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsValidObject
		{
			get
			{
				return (m_cache != null && !m_cache.IsDisposed && m_hvo > 0);
			}
		}

		/// <summary>
		/// Return true if possibleOwner is one of the owners of 'this'.
		/// (Returns false if possibleOwner is null.)
		/// </summary>
		public bool IsOwnedBy(ICmObject possibleOwner)
		{

			for (var current = Owner; current != null; current = current.Owner)
			{
				if (current == possibleOwner)
					return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an ITsString that represents the shortname of this object.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property,
		/// if they want to show something other than
		/// the regular ShortName string.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[VirtualProperty(CellarPropertyType.String)]
		public virtual ITsString ShortNameTSS
		{
			get
			{
				return m_cache.TsStrFactory.MakeString(ShortName, m_cache.DefaultAnalWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a TsString that represents this object as it could be used in a deletion confirmaion dialogue.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property, if they want to show something other than the regular ShortNameTSS.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual ITsString DeletionTextTSS
		{
			get { return ShortNameTSS; }
		}

		/// <summary>
		/// This name is used in the FdoBrowser. It needs to identify objects unambiguously.
		/// </summary>
		public ITsString ObjectIdName
		{
			get
			{
				return Cache.TsStrFactory.MakeString(ShortName + " - " + ClassName + " " + Hvo.ToString(),
					Cache.DefaultUserWs);
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the shortest, non-abbreviated label for the content of this object.
		/// This is the name that you would want to show up in a chooser list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string ShortName
		{
			get
			{
				/* until this method is implemented for all objects that could be displayed in a list,
				 * the following will at least give us something to show for any class that implements
				 * a property called "NameAccessor" (which is pretty common for things that we know will be in a list)
				 */
				var pi = GetType().GetProperty("NameAccessor");
				if (pi != null)
				{
					var obj = pi.GetValue(this, null); // call NameAccessor on the class of this type
					var accessor = obj as MultiUnicodeAccessor;
					if (accessor != null)
					{
						var name = accessor.AnalysisDefaultWritingSystem;
						if (name != null && name.Length > 0)
							return name.Text;
					}
					else if (obj is string)
						return (string)obj;
				}

				//oh well, at least tell us what the type of the class is.
				return String.Format(Strings.ksAX, GetType().Name);
			}
		}

		#endregion Property accessors

		#region Public methods
		/// <summary>
		/// Determine if the object satisfies constraints imposed by the class
		/// </summary>
		/// <param name="flidToCheck">flid to check, or zero, for don't care about the flid.</param>
		/// <param name="createAnnotation">if set to <c>true</c>, an annotation will be created.</param>
		/// <param name="failure">an explanation of what constraint failed, if any. Will be null if the method returns true.</param>
		/// <returns>true if the object is all right</returns>
		public virtual bool CheckConstraints(int flidToCheck, bool createAnnotation, out ConstraintFailure failure)
		{
			failure = null;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// tells whether the given field is required to be non-empty given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <remarks>e.e. MoAdhocProhib makes no sense without "morphemes".
		/// e.g. MoEndoCompound makes no sense without a left and a right compound.
		/// </remarks>
		/// <returns>true, if the field is required.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsFieldRequired(int flid)
		{
			return false;//it is up to subclasses to override and return true where needed.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// tells whether the given field is relevant given the current values of related data items
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="propsToMonitor"></param>
		/// <remarks>e.g. "color" would not be relevant on a part of speech, ever.
		/// e.g.  MoAffixForm.inflection classes are only relevant if the MSAs of the
		/// entry include an inflectional affix MSA.
		/// </remarks>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsFieldRelevant(int flid, HashSet<Tuple<int, int>> propsToMonitor)
		{
			return true;//it is up to subclasses to override and return false where needed.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gives an object an opportunity to do any class-specific side-effect work when it has
		/// been cloned with DomainServices.CopyObject. CopyObject will call this method on each
		/// source object it copies after the copy is complete. The copyMap contains the source
		/// object Hvo as the Key and the copied object as the Value.
		/// </summary>
		/// <param name="copyMap"></param>
		/// ------------------------------------------------------------------------------------
		public virtual void PostClone(Dictionary<int, ICmObject> copyMap)
		{
			// It is up to subclasses to override and provide class-specific side effects.
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return ClassName + " : " + Hvo;
		}

		/// <summary>
		/// Get a string that represents the CmObject in XML.
		/// </summary>
		/// <returns></returns>
		string ICmObjectInternal.ToXmlString()
		{
			return Encoding.UTF8.GetString(((ICmObjectInternal)this).ToXmlBytes());
		}
		 /// <summary>
		/// Get a byte array containing a utf8 string that represents the CmObject in XML.
		/// </summary>
		/// <returns></returns>
		byte[] ICmObjectInternal.ToXmlBytes()
		{
			using (var memoryStream = new MemoryStream())
			{
				using (var writer = FdoXmlServices.CreateWriter(memoryStream))
				{
					writer.WriteStartElement("rt");
					// Add main properties that aren't in the generated stuff.
					writer.WriteAttributeString("guid", Guid.ToString().ToLowerInvariant());
					writer.WriteAttributeString("class", ClassName);
					if (m_owner != null) // nb don't test Owner, that can fail if m_owner is an ID and actual owner being deleted.
					{
						writer.WriteAttributeString("ownerguid", OwnerGuid.ToString().ToLowerInvariant());
					}

					ToXMLStringInternal(writer);
					writer.WriteEndElement();
					writer.Flush();
				}
				return memoryStream.ToArray();
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges object into this object.
		/// For atomic properties, if this object has something in the property, the source
		/// property is ignored. For sequence properties, the objects in the source will be
		/// moved and appended to the properties in this object. Any references to the
		/// source object will be transferred to this object. The source object is not deleted
		/// here in case the caller wants to do any cleanup after the merge, such as copying
		/// back references for owned atomic objects in the source that weren't moved.
		/// </summary>
		/// <param name="objSrc">Object whose properties will be merged into this object's properties</param>
		/// <remarks>
		/// NB: The given object will be deleted in this method, so don't expect it to be valid, afterwards.
		/// Method should NOT be virtual. Override the two-argument version if appropriate.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void MergeObject(ICmObject objSrc)
		{
			MergeObject(objSrc, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges object into this object.
		/// if fLoseNoStringData is false:
		/// For atomic properties, if this object has something in the property, the source
		/// property is ignored. For sequence properties, the objects in the source will be
		/// moved and appended to the properties in this object. Any references to the
		/// source object will be transferred to this object. The source object is deleted
		/// at the end of this method (objSrc.DeleteUnderlyingObject() call).
		/// String properties are copied from the source if the destination (this) has no value
		/// and the source has a value.
		///
		/// if fLoseNoStringData is true, the above is modified as follows:
		/// 1. If a string property has a value in both source and destination, and the values
		/// are different, append the source onto the destination.
		/// 2. If an atomic object property has a value in both source and destination,
		/// recursively merge the value in the source with the value in the destination.
		/// </summary>
		/// <param name="objSrc">Object whose properties will be merged into this object's properties</param>
		/// <remarks>
		/// NB: The given object will be deleted in this method, so don't expect it to be valid, afterwards.
		/// </remarks>
		/// <param name="fLoseNoStringData"></param>
		/// ------------------------------------------------------------------------------------
		public virtual void MergeObject(ICmObject objSrc, bool fLoseNoStringData)
		{
			Debug.Assert(m_cache != null);
			// We don't allow merging items of different classes.
			Debug.Assert(ClassID == objSrc.ClassID);
			if (ClassID != objSrc.ClassID)
				return;

			var mdc = (IFwMetaDataCacheManaged)m_cache.MetaDataCache;
			var flidList = from flid in mdc.GetFields(ClassID, true, (int)CellarPropertyTypeFilter.All)
						   where !m_cache.MetaDataCache.get_IsVirtual(flid)
						   select flid;
			// Process all the fields in the source.
			MergeSelectedPropertiesOfObject(objSrc, fLoseNoStringData, flidList.ToArray());
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges object into this object.
		/// if fLoseNoStringData is false:
		/// For atomic properties, if this object has something in the property, the source
		/// property is ignored. For sequence properties, the objects in the source will be
		/// moved and appended to the properties in this object. Any references to the
		/// source object will be transferred to this object. The source object is deleted
		/// at the end of this method (objSrc.DeleteUnderlyingObject() call).
		/// String properties are copied from the source if the destination (this) has no value
		/// and the source has a value.
		///
		/// if fLoseNoStringData is true, the above is modified as follows:
		/// 1. If a string property has a value in both source and destination, and the values
		/// are different, append the source onto the destination.
		/// 2. If an atomic object property has a value in both source and destination,
		/// recursively merge the value in the source with the value in the destination.
		/// </summary>
		/// <param name="objSrc">Object whose properties will be merged into this object's properties</param>
		/// <remarks>
		/// NB: The given object will be deleted in this method, so don't expect it to be valid, afterwards.
		/// </remarks>
		/// <param name="fLoseNoStringData"></param>
		/// <param name="flidList">List of property flids to consider for merging</param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		public void MergeSelectedPropertiesOfObject(ICmObject objSrc, bool fLoseNoStringData, int[] flidList)
		{
			var mdc = (IFwMetaDataCacheManaged)m_cache.MetaDataCache;
			// Process all the fields in the source.
			foreach (int flid in flidList)
			{
				/* These values will also be returned because they are for most of CmObject's flids.
				 * I think it will do this for each superclass, so there could be some repeats on them.
				 *
				 * pvuFields->Push(101);	// kflidCmObject_Guid
				 * pvuFields->Push(102);	// kflidCmObject_Class
				 * pvuFields->Push(103);	// kflidCmObject_Owner
				 * pvuFields->Push(104);	// kflidCmObject_OwnFlid
				 * pvuFields->Push(105);	// kflidCmObject_OwnOrd
				 * //pvuFields->Push(106);	// kflidCmObject_UpdStmp
				 * //pvuFields->Push(107);	// kflidCmObject_UpdDttm
				 *
				*/
				if (flid < 1000)
					continue; // Do nothing for the CmObject flids.

				var nType = mdc.GetFieldType(flid);
				var fieldname = mdc.GetFieldName(flid);
				//|| fieldname == "DateModified"
				//|| nType == (int)CellarPropertyType.Time // This is handled by a separate connection, so it can time out, if another transaction is open.
				if (fieldname == "DateCreated"
					|| nType == (int)CellarPropertyType.Image // FDO does not support this one.
					|| nType == (int)CellarPropertyType.GenDate) // FDO does not support setter for gendate.
					continue; // Don't mess with this one.

				// Set suffixes on some of the types. We don't put these suffixes on virtual properties.
				if (!m_cache.MetaDataCache.get_IsVirtual(flid))
				{
					switch (nType)
					{
						case (int)CellarPropertyType.OwningAtomic: // 23
							{
								fieldname += "OA";
								break;
							}
						case (int)CellarPropertyType.ReferenceAtomic: // 24
							{
								fieldname += "RA";
								break;
							}
						case (int)CellarPropertyType.OwningCollection: // 25
							{
								fieldname += "OC";
								break;
							}
						case (int)CellarPropertyType.ReferenceCollection: // 26
							{
								fieldname += "RC";
								break;
							}
						case (int)CellarPropertyType.OwningSequence: // 27
							{
								fieldname += "OS";
								break;
							}
						case (int)CellarPropertyType.ReferenceSequence: // 28
							{
								fieldname += "RS";
								break;
							}
					}
				}
				Object myCurrentValue = null;
				MethodInfo mySetMethod = null;
				Object srcCurrentValue = null;

				var pi = this.GetType().GetProperty(fieldname);
				if (pi != null)
				{
					myCurrentValue = pi.GetGetMethod().Invoke(this, null);
					mySetMethod = pi.GetSetMethod();
					srcCurrentValue = objSrc.GetType().GetProperty(fieldname).GetGetMethod().Invoke(objSrc, null);
				}
				else
				{
					// We must have a custom field, and it needs special treatment.
					Debug.Assert(m_cache.GetIsCustomField(flid));
					mySetMethod = null;
#if DEBUG
					var classname = mdc.GetOwnClsName(flid);
					var sView = classname + "_" + fieldname;
#endif
					switch (nType)
					{
						case (int)CellarPropertyType.String:
							myCurrentValue = m_cache.DomainDataByFlid.get_StringProp(Hvo, flid);
							srcCurrentValue = m_cache.DomainDataByFlid.get_StringProp(objSrc.Hvo, flid);
							break;
						case (int)CellarPropertyType.MultiString:
							myCurrentValue = new MultiStringAccessor(this, flid);
							srcCurrentValue = new MultiStringAccessor(objSrc, flid);
							break;
						case (int)CellarPropertyType.MultiUnicode:
							myCurrentValue = new MultiUnicodeAccessor(this, flid);
							srcCurrentValue = new MultiUnicodeAccessor(objSrc, flid);
							break;
					}
				}
				if (srcCurrentValue == null)
					continue; // Nothing to merge.
				Debug.Assert(srcCurrentValue != null);

				/*
				 * NOTE: Each of the cases (except the exception, which can't be tested)
				 * is tested in the MergeObjectsTests class in the unit tests.
				 * If any additions are made, or if some currently unused cases are enabled,
				 * be sure to add them (or enable them) to that class, as well.
				 */
				switch (nType)
				{
					default:
						throw new ApplicationException("Unrecognized data type for merging: " + nType);

					/* 0 -> 9 */
					case (int)CellarPropertyType.Boolean: // 1
						{
							// Can't be null, so we have to live with default of 0 (false).
							// 0 gets replaced with source data, if 1 (true).
							var myBool = (bool)myCurrentValue;
							var srcBool = (bool)srcCurrentValue;
							if (!myBool && srcBool)
							{
								if (mySetMethod != null)
									mySetMethod.Invoke(this, new object[] { srcCurrentValue });
								else
									SetCustomFieldValue(flid, nType, srcCurrentValue);
							}
							break;
						}
					//
					case (int)CellarPropertyType.Integer: // 2 Fall through
						// Setter not implemented in FDO. case (int)CellarPropertyType.GenDate: // 8
						{
							// Can't be null, so we have to live with default of 0.
							// Zero gets replaced with source data, if greater than 0.
							var myInt = (int)myCurrentValue;
							var srcInt = (int)srcCurrentValue;
							if (myInt == 0 && srcInt > 0)
							{
								if (mySetMethod != null)
									mySetMethod.Invoke(this, new object[] { srcCurrentValue });
								else
									SetCustomFieldValue(flid, nType, srcCurrentValue);
							}
							break;
						}
					case (int)CellarPropertyType.Time: // 5
						{
							// If it is DateCreated, we won't even be here,
							// since we will have already skipped it.
							var resetTime = false;
							var srcTime = DateTime.Now;
							// If it is DateModified, always set it to 'now'.
							if (fieldname == "DateModified")
							{
								// Already using 'Now'.
								resetTime = true;
							}
							else
							{
								// Otherwise, a later source will replace an older target.
								var myTime = (DateTime)myCurrentValue;
								srcTime = (DateTime)srcCurrentValue;
								resetTime = (myTime < srcTime);
								if (myTime < srcTime)
								{
									if (mySetMethod != null)
										mySetMethod.Invoke(this, new object[] { srcTime });
									else
										SetCustomFieldValue(flid, nType, srcTime);
								}
							}
							if (resetTime)
							{
								if (mySetMethod != null)
									mySetMethod.Invoke(this, new object[] { srcTime });
								else
									SetCustomFieldValue(flid, nType, srcTime);
							}
							break;
						}
					case (int)CellarPropertyType.Guid: // 6
						{
							// May be null.
							var myGuidValue = (Guid)myCurrentValue;
							var srcGuidValue = (Guid)srcCurrentValue;
							if (myGuidValue == Guid.Empty && srcGuidValue != Guid.Empty)
							{
								if (mySetMethod != null)
								{
									mySetMethod.Invoke(this, new object[] { srcGuidValue });
									mySetMethod.Invoke(objSrc, new object[] { Guid.Empty });
								}
								else
								{
									SetCustomFieldValue(flid, nType, srcGuidValue);
								}
							}
							break;
						}
					//case (int)CellarPropertyType.Image: // 7 Fall through.
					case (int)CellarPropertyType.Binary: // 8
						{
							if (myCurrentValue == null)
							{
								if (mySetMethod != null)
									mySetMethod.Invoke(this, new object[] { srcCurrentValue });
								else
									SetCustomFieldValue(flid, nType, srcCurrentValue);
							}
							break;
						}

					/* 13 -> 20 */
					case (int)CellarPropertyType.String: // 13
						{
							if (MergeStringProp(flid, nType, objSrc, fLoseNoStringData, myCurrentValue, srcCurrentValue))
								break;
							var myTss = myCurrentValue as ITsString;
							myTss = TsStringUtils.MergeString((ITsString)srcCurrentValue, myTss, fLoseNoStringData);
							if (mySetMethod != null)
								mySetMethod.Invoke(this, new object[] { myTss });
							else
								SetCustomFieldValue(flid, nType, myTss);
							break;
						}

					case (int)CellarPropertyType.MultiString: // 14
						{
							if (MergeStringProp(flid, nType, objSrc, fLoseNoStringData, myCurrentValue, srcCurrentValue))
								break;
							var myMsa = myCurrentValue as IMultiStringAccessor;
							myMsa.MergeAlternatives(srcCurrentValue as IMultiStringAccessor, fLoseNoStringData);
							break;
						}

					case (int)CellarPropertyType.Unicode: // 15
						{
							if (MergeStringProp(flid, nType, objSrc, fLoseNoStringData, myCurrentValue, srcCurrentValue))
								break;
							var myUCurrent = myCurrentValue as string;
							var srcUValue = srcCurrentValue as string;
							if (String.IsNullOrEmpty(myUCurrent)
								&& srcUValue != String.Empty)
							{
								if (mySetMethod != null)
									mySetMethod.Invoke(this, new object[] { srcUValue });
								else
									SetCustomFieldValue(flid, nType, srcUValue);
							}
							else if (fLoseNoStringData
								&& !String.IsNullOrEmpty(myUCurrent)
								&& !String.IsNullOrEmpty(srcUValue)
								&& srcUValue != myUCurrent)
							{
								if (mySetMethod != null)
									mySetMethod.Invoke(this, new object[] { myUCurrent + ' ' + srcUValue });
								else
									SetCustomFieldValue(flid, nType, myUCurrent + ' ' + srcUValue);
							}
							break;
						}

					case (int)CellarPropertyType.MultiUnicode: // 16
						{
							if (MergeStringProp(flid, nType, objSrc, fLoseNoStringData, myCurrentValue, srcCurrentValue))
								break;
							var myMua = myCurrentValue as IMultiUnicode;
							myMua.MergeAlternatives(srcCurrentValue as IMultiUnicode, fLoseNoStringData);
							break;
						}

					/* 23 -> 28 */
					case (int)CellarPropertyType.OwningAtomic:
					case (int)CellarPropertyType.ReferenceAtomic: // 24
						{
							var srcObj = srcCurrentValue as ICmObject;
							var currentObj = myCurrentValue as ICmObject;
							if (myCurrentValue == null)
							{
								if (nType == (int)CellarPropertyType.OwningAtomic || mySetMethod != null)
								{
									Debug.Assert(mySetMethod != null);
									mySetMethod.Invoke(this, new object[] { srcObj });
								}
								else
								{
									SetCustomFieldValue(flid, nType, srcObj);
								}
								break;
							}
							// TODO-Linux: System.Boolean System.Type::op_Equality(System.Type,System.Type)
							// is marked with [MonoTODO] and might not work as expected in 4.0.
							else if (fLoseNoStringData && nType == (int)CellarPropertyType.OwningAtomic && srcObj != null
								&& currentObj.GetType() == srcObj.GetType())
							{
								// merge the child objects.
								currentObj.MergeObject(srcObj, true);
							}
							break;
						}

					case (int)CellarPropertyType.OwningCollection: // 25 Fall through, since the collection class knows how to merge itself properly.
					case (int)CellarPropertyType.ReferenceCollection: // 26
					case (int)CellarPropertyType.OwningSequence: // 27 Fall through, since the collection class knows how to merge itself properly.
					case (int)CellarPropertyType.ReferenceSequence: // 28
						{
							var myAddMethod = myCurrentValue.GetType().GetMethod("Add");
							foreach (var input in ((IFdoVector)srcCurrentValue).Objects)
								myAddMethod.Invoke(myCurrentValue, new object[] { input });
							break;
						}
				}
			}

			// Now move all incoming references.
			var cmObject = ((CmObject)objSrc);
			cmObject.EnsureCompleteIncomingRefs();
			ReplaceIncomingReferences(objSrc);
			if (objSrc.IsValidObject) // possibly side effects of ReplaceIncomingReferences will have deleted it already.
				m_cache.DomainDataByFlid.DeleteObj(objSrc.Hvo);
		}

		/// <summary>
		/// Set a value for a custom field.
		/// </summary>
		private void SetCustomFieldValue(int flid, int nType, object newValue)
		{
			switch (nType)
			{
				case (int)CellarPropertyType.Boolean: // 1
					bool fValue = (bool)newValue;
					m_cache.DomainDataByFlid.SetBoolean(this.Hvo, flid, fValue);
					break;
				case (int)CellarPropertyType.Integer: // 2 Fall through
					int nValue = (int)newValue;
					m_cache.DomainDataByFlid.SetInt(this.Hvo, flid, nValue);
					break;
				case (int)CellarPropertyType.Time: // 5
					DateTime dt = (DateTime)newValue;
					SilTime.SetTimeProperty(m_cache.DomainDataByFlid, this.Hvo, flid, dt);
					break;
				case (int)CellarPropertyType.Guid: // 6
					Guid guid = (Guid)newValue;
					m_cache.DomainDataByFlid.SetGuid(this.Hvo, flid, guid);
					break;
				case (int)CellarPropertyType.Image: // 7 Fall through.
				case (int)CellarPropertyType.Binary: // 8
					break;
				case (int)CellarPropertyType.String: // 13
					ITsString tss = (ITsString)newValue;
					m_cache.DomainDataByFlid.SetString(this.Hvo, flid, tss);
					break;
				case (int)CellarPropertyType.Unicode: // 15
					string sValue = (string)newValue;
					m_cache.DomainDataByFlid.SetUnicode(this.Hvo, flid, sValue, sValue.Length);
					break;
				case (int)CellarPropertyType.ReferenceAtomic: // 24
					ICmObject obj = (ICmObject)newValue;
					m_cache.DomainDataByFlid.SetObjProp(this.Hvo, flid, obj.Hvo);
					break;
				default:
					throw new ApplicationException("Unrecognized custom field data type for merging: " + nType.ToString());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override this to provide special handling on a per-property basis for merging
		/// strings. For example, certain Sense properties insert semi-colon.
		/// If it answers false, the default merge is performed; if it answers true,
		/// an appropriate merge is presumed to have already been done.
		/// Note that on multistring properties it is called only once;
		/// override is responsible to merge all writing systems.
		/// </summary>
		/// <param name="flid">Field to merge.</param>
		/// <param name="cpt">Field type (cpt enumeration)</param>
		/// <param name="objSrc">Object being merged into this.</param>
		/// <param name="fLoseNoStringData">Currently always true, supposed to indicate that the merge should not lose anything.</param>
		/// <param name="myCurrentValue">Value for this object obtained from the relevant method for the property.
		/// Depending on the type, may be string, TsStringAccessor, MultiStringAccessor, or MultiUnicodeAccessor.</param>
		/// <param name="srcCurrentValue">Same thing for other.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool MergeStringProp(int flid, int cpt, ICmObject objSrc, bool fLoseNoStringData,
			object myCurrentValue, object srcCurrentValue)
		{
			return false;
		}

		#endregion

		#region Internal API

		/// <summary>
		/// Replace all incoming references to objOld with references to 'this'.
		/// Virtual to allow special handling of certain groups of reference sequences that interact
		/// (e.g. LexEntryRef properties ComponentLexemes and PrimaryLexemes; see LT-14540)
		/// </summary>
		/// <param name="objOld"></param>
		/// <remarks>Assumes that EnsureCompleteIncomingRefs() has already been run on 'objOld'.</remarks>
		internal virtual void ReplaceIncomingReferences(ICmObject objOld)
		{
			ReplaceReferences(objOld, this);
		}

		/// <summary>
		/// Update all existing references to point to objNew instead of objOld (which may be
		/// getting deleted as soon as we return from this method).
		/// </summary>
		/// <param name="objOld"></param>
		/// <param name="objNew"></param>
		internal static void ReplaceReferences(ICmObject objOld, ICmObject objNew)
		{
			var cmObject = ((CmObject)objOld);
			cmObject.EnsureCompleteIncomingRefs();
			// FWR-2969 If merging senses, m_incomingRefs will sometimes get changed
			// by ReplaceAReference.
			var refs = new Set<IReferenceSource>(cmObject.m_incomingRefs);
			foreach (var source in refs)
			{
				source.ReplaceAReference(objOld, objNew);
			}
		}


		/// <summary>
		/// Update all existing references to point to objNew instead of objOld (which may be
		/// getting deleted as soon as we return from this method), provided the new reference is valid.
		/// If the new one is not valid in some instance, go ahead and replace the others.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="objNew"></param>
		/// <param name="objOld"></param>
		internal static void ReplaceReferencesWhereValid(FdoCache cache, ICmObject objOld, ICmObject objNew)
		{
			var cmObject = ((CmObject)objOld);
			cmObject.EnsureCompleteIncomingRefs();
			// FWR-2969 If merging senses, m_incomingRefs will sometimes get changed
			// by ReplaceAReference.
			var refs = new List<IReferenceSource>(cmObject.m_incomingRefs);
			foreach (var source in refs)
			{
				try
				{
					source.ReplaceAReference(objOld, objNew);
				}
				catch (InvalidOperationException)
				{
				}
			}
		}

		/// <summary>
		/// Get the real object corresponding to the HVO.
		/// This is a convenient shortcut which saves duplicating this little code fragment a lot.
		/// </summary>
		internal ICmObject GetObjectFromHvo(int hvo)
		{
			return m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
		}

		/// <summary>
		/// Set the non-model property.
		/// </summary>
		/// <param name="flid">The property to set.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="useAccessor">true to add information to UOW, false to just set it. (Name is strange, but USUALLY
		/// this is achieved by using or bypassing the accessor for a property, so this is the common name
		/// for this argument.)</param>
		/// <remarks>
		/// There will be exceptions thrown, if there is no such property.
		/// </remarks>
		internal void SetNonModelPropertyForSDA(int flid, object newValue, bool useAccessor)
		{
			CheckLegalFlidForObject(flid);
			try
			{
				if (m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>().IsCustom(flid))
					SetCustomPropertyForSDA(flid, newValue, useAccessor);
				else
					SetVirtualPropertyForSDA(flid, newValue, useAccessor);
			}
			catch (FDOInvalidFieldException)
			{
				throw; // Rethrow this exception that one of the methods threw.
			}
			catch (Exception err)
			{
				throw new FDOInvalidFieldException("'flid' is not in the metadata cache, or 'flid' does not match the type of data used. Use an SDA decorator for your madeup property.", err);
			}
		}

		/// <summary>
		/// Set the virtual property
		/// </summary>
		/// <param name="flid">The property to set.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="useAccessor">'true' to send to UOW. 'false' to not send to the UOW.</param>
		/// <remarks>
		/// There will be exceptions thrown, if there is no such property.
		/// </remarks>
		protected virtual void SetVirtualPropertyForSDA(int flid, object newValue, bool useAccessor)
		{
			int flidType;
			var fieldName = CheckVirtualProperty(flid, out flidType);
			var propertySuffix = String.Empty;

			switch ((CellarPropertyType)flidType)
			{
				default:
					throw new ArgumentException("'tag' is not a known type.");
				case CellarPropertyType.Boolean: // Fall through.
				case CellarPropertyType.Integer: // Fall through.
				case CellarPropertyType.Time: // Fall through.
				case CellarPropertyType.Guid: // Fall through.
				case CellarPropertyType.Image: // Fall through.
				case CellarPropertyType.GenDate: // Fall through.
				case CellarPropertyType.Binary: // Fall through.
				case CellarPropertyType.Numeric: // Fall through.
				case CellarPropertyType.Float: // Fall through.
				case CellarPropertyType.Unicode: // Fall through.
				case CellarPropertyType.String: // Fall through.
				case CellarPropertyType.MultiString: // Fall through.
				case CellarPropertyType.MultiUnicode: // Fall through.
				case CellarPropertyType.OwningAtomic: // Fall through.
				case CellarPropertyType.ReferenceAtomic:
					// All non-vectors.
					var propInfo = GetPropertyInfo(fieldName);
					var setter = propInfo.GetSetMethod();
					if (setter == null)
						break; // some virtual we can't modify; should we report this? Generally read-only stuff just quietly doesn't change...
					setter.Invoke(this, (newValue == null) ? null : new[] { newValue });
					break;
				case CellarPropertyType.OwningCollection:
					propertySuffix = "OC";
					goto case CellarPropertyType.ReferenceSequence;
				case CellarPropertyType.OwningSequence:
					propertySuffix = "OS";
					goto case CellarPropertyType.ReferenceSequence;
				case CellarPropertyType.ReferenceCollection:
					propertySuffix = "RC";
					goto case CellarPropertyType.ReferenceSequence;
				case CellarPropertyType.ReferenceSequence:
					if (propertySuffix == String.Empty)
						propertySuffix = "RS";
					// All vectors.
					// Get the 'ReplaceAll' method on the collection.
					// First, get the collection.
					var vectorPropInfo = GetPropertyInfo(fieldName + propertySuffix);
					var vectorRetval = vectorPropInfo.GetGetMethod().Invoke(this, null);
					if (vectorRetval is List<int>)
					{
						// Get the objects (and their hvos) for the guids.
						var objRepository = Cache.ServiceLocator.GetInstance<ICmObjectRepository>();
						var newList = new List<int>();
						foreach (var guid in (Guid[])newValue)
							newList.Add(objRepository.GetObject(guid).Hvo);
						// Set the property.
						// Using reflection lets the property do whatever else it wants in its setter.
						vectorPropInfo.GetSetMethod().Invoke(this, new object[] { newList });
					}
					else
					{
						((IFdoVectorInternal)vectorRetval).ReplaceAll((Guid[])newValue, true);
					}
					break;
			}
		}

		internal IEnumerable<ICmObject> GetNonModelPropertyForSdaAsEnumerable(int flid)
		{
			var retval = GetNonModelPropertyForSDA(flid);
			return (retval as IEnumerable<ICmObject>) ?? ((IFdoVector)retval).Objects;
		}

		/// <summary>
		/// 'flid' may be a virtual or custom property. Be careful with collections, the result could
		/// be either IEnumerable of ICmObject (usual for virtuals) or one of the classes
		/// that implement our sequence and collection properties (when custom).
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		internal object GetNonModelPropertyForSDA(int flid)
		{
			CheckLegalFlidForObject(flid);
			try
			{
				return m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>().IsCustom(flid)
					? GetCustomPropertyForSDA(flid)
					: GetVirtualPropertyForSDA(flid);
			}
			catch (FDOInvalidFieldException)
			{
				throw; // Rethrow this exception that one of the methods threw.
			}
			catch (Exception err)
			{
				throw new FDOInvalidFieldException("'flid' is not in the metadata cache. Use an SDA decorator for your madeup property.", err);
			}
		}

		/// <summary>
		/// Initialize a new ownerless object.
		/// </summary>
		/// <param name="cache"></param>
		void ICmObjectInternal.InitializeNewOwnerlessCmObject(FdoCache cache)
		{
			if (cache == null) throw new ArgumentNullException("cache");
			var servLoc = cache.ServiceLocator;
			if (Hvo != (int)SpecialHVOValues.kHvoUninitializedObject)
				throw new ArgumentException("New object already has a valid HVO.");
			if (Hvo == (int)SpecialHVOValues.kHvoObjectDeleted)
				throw new FDOObjectDeletedException("'New' object has been deleted.");
			if (m_cache != null)
				throw new ArgumentException("New object already has a cache.");

			m_cache = cache;
			m_hvo = ((IDataReader)cache.ServiceLocator.GetInstance<IDataSetup>()).GetNextRealHvo();
			m_guid = Services.GetInstance<ICmObjectIdFactory>().FromGuid(Guid.NewGuid());
			((IServiceLocatorInternal)servLoc).UnitOfWorkService.RegisterObjectAsCreated(this);
			SetDefaultValuesAfterInit();
		}

		/// <summary>
		/// A very special case, an ownerless object created with a constructor that predetermines the
		/// guid (and also sets the cache, hvo, and calls RegisterObjectAsCreated).
		/// However, since the object will be unowned, SetDefaultValuesAfterInit will not be
		/// called when setting the owner, so we need to do it here.
		/// </summary>
		void ICmObjectInternal.InitializeNewOwnerlessCmObjectWithPresetGuid()
		{
			SetDefaultValuesAfterInit();
		}

		/// <summary>
		/// Initialize a CmObject that was created using the default Constructor.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="owner"></param>
		/// <param name="owningFlid"></param>
		/// <param name="ord"></param>
		/// <remarks>
		/// This method should only be called by generated code 'setters'.
		/// NB: This method should not be called on any unowned object.
		/// </remarks>
		void ICmObjectInternal.InitializeNewCmObject(FdoCache cache, ICmObject owner, int owningFlid, int ord)
		{
			if (Hvo != (int)SpecialHVOValues.kHvoUninitializedObject)
				throw new ArgumentException("New object already has a valid HVO.");
			if (Hvo == (int)SpecialHVOValues.kHvoObjectDeleted)
				throw new FDOObjectDeletedException("'New' object has been deleted.");
			if (m_cache != null)
				throw new ArgumentException("New object already has a cache.");
			if (cache == null)
				throw new ArgumentNullException("cache");
			if (owner == null)
				throw new ArgumentNullException("owner");

			m_cache = cache;
			m_hvo = ((IDataReader)Services.DataSetup).GetNextRealHvo();
			// 'owner' better not be null, or be ready to catch an exception.
			(this as ICmObjectInternal).SetOwner(owner, owningFlid, ord);
			m_guid = Services.CmObjectIdFactory.NewId();
			((IServiceLocatorInternal)Services).UnitOfWorkService.RegisterObjectAsCreated(this);
			SetDefaultValuesAfterInit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the default values after the initialization of a CmObject. At the point that
		/// this method is called, the object should have an HVO, Guid, and a cache set.
		/// </summary>
		/// <remarks>
		/// [NB: This is *not* for use during object reconstitution.
		/// Use <see cref="DoAdditionalReconstruction"/> during object reconstitution.]
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual void SetDefaultValuesAfterInit()
		{
			// Default is to do nothing.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method checks the core validity of a CmObject.
		/// It checks the FdoCache and the Hvo, to make sure they are usable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void ICmObjectInternal.CheckBasicObjectState()
		{
			if (m_cache == null)
				throw new FDOCacheUnusableException("There is no FdoCache for this object.");
			if (m_cache.IsDisposed)
				throw new FDOCacheUnusableException("The FdoCache has been disposed, so this object cannot be used.");
			if (m_hvo == (int)SpecialHVOValues.kHvoUninitializedObject)
				throw new FDOObjectUninitializedException("Object has no valid HVO.");
			if (m_hvo == (int)SpecialHVOValues.kHvoObjectDeleted)
				throw new FDOObjectDeletedException("Object has been deleted, and cannot be used.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validation done before adding an object to some vector flid.
		/// </summary>
		/// <exception cref="InvalidOperationException">The addition is not valid</exception>
		/// ------------------------------------------------------------------------------------
		void ICmObjectInternal.ValidateAddObject(AddObjectEventArgs e)
		{
			ValidateAddObjectInternal(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Need this version, because the ICmObjectInternal.ValidateAddObject version
		/// can't be virtual, and we want subclasses to be able to override the method.
		/// </summary>
		/// <exception cref="InvalidOperationException">The addition is not valid</exception>
		/// ------------------------------------------------------------------------------------
		protected virtual void ValidateAddObjectInternal(AddObjectEventArgs e)
		{
			// Do nothing.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle any side effects of adding an object to some vector flid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void ICmObjectInternal.AddObjectSideEffects(AddObjectEventArgs e)
		{
			AddObjectSideEffectsInternal(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Need this version, because the ICmObjectInternal.AddObjectSideEffects version
		/// can't be virtual, and we want subclasses to be able to override the method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void AddObjectSideEffectsInternal(AddObjectEventArgs e)
		{
			// Do nothing.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle any side effects of removing an object from some vector flid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void ICmObjectInternal.RemoveObjectSideEffects(RemoveObjectEventArgs e)
		{
			RemoveObjectSideEffectsInternal(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Need this version, because the ICmObjectInternal.RemoveObjectSideEffects version
		/// can't be virtual, and we want subclasses to be able to override the method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			// Do nothing.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle any side effects of changing (add/remove/modify) an alternative ITsString.
		/// </summary>
		/// <param name="multiAltFlid">The flid that was changed</param>
		/// <param name="alternativeWs">The WS of the alternative that was changed</param>
		/// <param name="originalValue">Original value. (May be null.)</param>
		/// <param name="newValue">New value. (May be null.)</param>
		/// ------------------------------------------------------------------------------------
		void ICmObjectInternal.ITsStringAltChangedSideEffects(int multiAltFlid, IWritingSystem alternativeWs, ITsString originalValue, ITsString newValue)
		{
			if (alternativeWs == null) throw new ArgumentNullException("alternativeWs");

			ITsStringAltChangedSideEffectsInternal(multiAltFlid, alternativeWs, originalValue, newValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Subclasses should override this, if they need to have side effects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void ITsStringAltChangedSideEffectsInternal(int multiAltFlid, IWritingSystem alternativeWs, ITsString originalValue, ITsString newValue)
		{ /* Do nothing. */ }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This sets the hvo to the given hvo and sets the cache, which may be null.
		/// </summary>
		/// <param name="cache">FdoCache or null</param>
		/// <param name="newHvo">Original HVO, SpecialHVOValues.kHvoUninitializedObject, or SpecialHVOValues.kHvoObjectDeleted</param>
		/// ------------------------------------------------------------------------------------
		void ICmObjectInternal.ResetForUndoRedo(FdoCache cache, int newHvo)
		{
			m_cache = cache;
			m_hvo = newHvo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// (Re)-Set owner in new field.
		/// Removes from old owner if it is a different object or flid, or a different index.
		/// To prevent removal from the same set when adding to a set, pass -1 for the new ord.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void ICmObjectInternal.SetOwner(ICmObject owner, int owningFlid, int ord)
		{
			ValidateOwnership(owner, owningFlid);

			var oldOwner = Owner as CmObject;
			int oldOwnOrd = -3; // not found
			int oldOwningFlid = 0;
			if (oldOwner != null)
				oldOwningFlid = OwningFlidAndIndex(true, out oldOwnOrd);

			// Tell old owner of its loss (but only if it HAD a previous owner).
			if (oldOwner != null && (oldOwner != owner || oldOwningFlid != owningFlid || (oldOwnOrd != ord && ord != -1)))
				oldOwner.RemoveOwnee(oldOwningFlid, this);

			// Now actually change it. It's important to do this after RemoveOwnee, because side effects of
			// remove may require it to still know its old owner (e.g., to figure its old OwningFlid).
			m_owner = owner; // Maybe be null.
			// Caller is expected to have added 'this' to the actual new owning property.

			if (oldOwner == null || oldOwner == owner)
				return;
			// We need to register the added object as changed, even though the only thing that has
			// changed in it is the link to its owner.  Registering the Owner field as changed
			// doesn't work, because a later Undo / Redo will fail.  (Those operations don't recognize
			// the flid for Owner, and probably shouldn't do anything with it anyway.)
			// If the object is not registered as changed, its original XML (including the old, possibly
			// deleted, owner's guid) will be written out.  This results in interesting (and almost
			// unfixable) crashes the next time the project is opened.
			Cache.ServiceLocator.GetInstance<IUnitOfWorkService>().RegisterObjectOwnershipChange(this, oldOwner.Guid, oldOwningFlid, owningFlid);

		}

		/// <summary>
		/// Throw an exception if the desired ownership change is invalid.
		/// </summary>
		private void ValidateOwnership(ICmObject owner, int owningFlid)
		{
			// Do validity checks with owner.
			if (Hvo == (int)SpecialHVOValues.kHvoUninitializedObject)
				throw new FDOObjectUninitializedException("Owned object has no valid HVO.");
			if (Hvo == (int)SpecialHVOValues.kHvoObjectDeleted)
				throw new FDOObjectDeletedException("Owned object has been deleted.");
			if (owner == null)
				throw new ArgumentNullException("owner");
			if (owner.Hvo == (int)SpecialHVOValues.kHvoUninitializedObject)
				throw new FDOObjectUninitializedException("Owning object has no valid HVO.");
			if (owner.Hvo == (int)SpecialHVOValues.kHvoObjectDeleted)
				throw new FDOObjectDeletedException("Owning object has been deleted.");
			if (Cache != owner.Cache)
				throw new ArgumentException("'ownee' and 'owner' have different caches.");
			if (OwnershipStatus == ClassOwnershipStatus.kOwnerProhibited)
				throw new InvalidOperationException(GetType().Name + " can not be owned!");

			var servLoc = Cache.ServiceLocator;
			var mdc = servLoc.MetaDataCache;
			if (owningFlid == 0)
				return; // can't do any more validation; this occurs when move is caused by reconciling changes made in another client.
			// Make sure owningFlid is an owning property.
			CellarPropertyType flidType = (CellarPropertyType)mdc.GetFieldType(owningFlid);
			if ((flidType == CellarPropertyType.OwningCollection) ||
				(flidType == CellarPropertyType.OwningSequence) ||
				(flidType == CellarPropertyType.OwningAtomic))
			{
				// Make sure new owner has the owningFlid.
				var flids = mdc.GetFields(owner.ClassID, true, (int)CellarPropertyTypeFilter.All);

				if (!flids.Contains(owningFlid))
					throw new ArgumentException(String.Format("Invalid 'owningFlid' ({0}) for 'owner' ({1})", owningFlid, owner.Hvo));

				// Make sure ownee can go in owningFlid.
				if (!mdc.get_IsValidClass(owningFlid, ClassID))
					throw new ArgumentException(String.Format("Cannot put class '{0}' in the field '{1}'.", ClassID, owningFlid));
			}
			else
			{
				throw new ArgumentException(String.Format("Invalid 'owningFlid' ({0}) for 'owner' ({1})", owningFlid, owner.Hvo));
			}
		}

		/// <summary>
		/// Restore the owner in the restored field.
		/// </summary>
		/// <remarks>
		/// This is used only for undo/redo, where the old owner and the new owner are handled
		/// separately and independently.
		/// </remarks>
		void ICmObjectInternal.SetOwnerForUndoRedo(ICmObject owner, int owningFlid, int ord)
		{
			ValidateOwnership(owner, owningFlid);
			m_owner = owner;
			// Caller is expected to have added 'this' to the actual new owning property.
		}

		/// <summary>
		/// Delete the recipient object. This is mainly used for unowned objects, but will correctly delete an owned one, too.
		/// </summary>
		public void Delete()
		{
			var goner = (ICmObjectInternal)this;
			if (Owner == null) // Ownerless
			{
				// just delete it.
				goner.DeleteObject();
			}
			else
			{
				ICmObjectInternal owner = (ICmObjectInternal)Owner;

				// Use SetProperty/Replace to remove it, which will come back into
				// this method with hvoOwner being kFDOFrameworkDeletingObjectHvo.
				int tag = OwningFlid;
				if(tag == 0)
				{
					throw new FDOInvalidFieldException(String.Format("Error when deleting object of class {0}, we were not found in our owner {1}.", goner.GetType().FullName, owner.GetType().FullName));
				}
				switch ((CellarPropertyType)Services.MetaDataCache.GetFieldType(tag))
				{
					default:
						throw new InvalidOperationException("OwningFlid is not an owning field type.");
					case CellarPropertyType.OwningAtomic:
						owner.SetProperty(tag, (ICmObject)null, true);
						break;
					case CellarPropertyType.OwningCollection:
						owner.Replace(tag, new[] { goner }, Enumerable.Empty<ICmObject>());
						break;
					case CellarPropertyType.OwningSequence:
						owner.Replace(tag, goner.OwnOrd, 1, Enumerable.Empty<ICmObject>());
						break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete the object.
		/// Should only be called by the FDO cache or the vector code!
		/// Any other caller won't know what to do with the surrogate.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void ICmObjectInternal.DeleteObject()
		{
			var cache = Cache; // will be cleared before final use!
			Debug.Assert(!Cache.ObjectsBeingDeleted.Contains(this));
			cache.ObjectsBeingDeleted.Add(this);
			try
			{
				// Get it registered with the UOW.
				// The UOW needs the ORIGINAL XML, before any changes are made, in order to restore it
				// properly if the deletion is undone.
				var oldXml = ((ICmObjectInternal)this).ToXmlString();
				OnBeforeObjectDeleted();
				ClearIncomingReferences();
				DeleteObjectBasics();

				// Must register as deleted AFTER doing all the side effects (in case another object's
				// side effects need to retrieve this one), but BEFORE we wipe its HVO.
				((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterObjectAsDeleted(this, oldXml);

				m_owner = null;
				m_cache = null;
				// NB: This must be done after registering it, so the provider can use its good hvo.
				m_hvo = (int)SpecialHVOValues.kHvoObjectDeleted;
		   }
			finally
			{
				cache.ObjectsBeingDeleted.Remove(this);
			}
		}

		/// <summary>
		/// Remove all references to this object.
		/// </summary>
		private void ClearIncomingReferences()
		{
			EnsureCompleteIncomingRefs();
			foreach (var referrer in m_incomingRefs.ToArray()) // need to copy it because it will get modified.
				referrer.RemoveAReference(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This provides a hook point for subclasses to do anything else they need to do when
		/// this object is being deleted (i.e., after the unit of work has been started but
		/// BEFORE we actually blow away the member variables and remove all knowledge of this
		/// object from the cache).
		/// Note that we do NOT have OnAfterObjectDeleted. This was removed, since after we
		/// have cleaned up incoming refs, cleared all properties, and removed the object's
		/// ID, cache, and service locator, there's nothing useful you can do with it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnBeforeObjectDeleted()
		{
		}


		/// <summary>
		/// Registers in the current UOW any virtual changes needed as a result of the creation
		/// of this new object.
		/// This could plausibly be called from SetDefaultValuesAfterInit, but it is rarely needed
		/// and it costs something to locate the UOW service. So currently it is called from
		/// overrides of SetDefaultValuesAfterInit where needed.
		/// </summary>
		internal virtual void RegisterVirtualsModifiedForObjectCreation(IUnitOfWorkService uow)
		{
		}

		/// <summary>
		/// Registers in the current UOW any virtual changes needed as a result of the deletion
		/// of this object.
		/// This could plausibly be called from OnBeforeObjectDeleted, but it is rarely needed
		/// and it costs something to locate the UOW service. So currently it is called from
		/// overrides of OnBeforeObjectDeleted where needed.
		/// </summary>
		internal virtual void RegisterVirtualsModifiedForObjectDeletion(IUnitOfWorkService uow)
		{
		}

		/// <summary>
		/// Initialize an object from a data store.
		/// </summary>
		void ICmObjectInternal.LoadFromDataStore(FdoCache cache, XElement reader, LoadingServices loadingServices)
		{
			if (cache == null) throw new ArgumentNullException("cache");
			if (reader == null) throw new ArgumentNullException("reader");
			if (loadingServices == null) throw new ArgumentNullException("loadingServices");

			var str = reader.Name.LocalName;
			if (str != "rt")
				throw new ArgumentException("XML not recognized.");

			// Read the attributes of the <rt> element.
			// 'guid' is required.
			var attr = reader.Attribute("guid");
			m_guid = loadingServices.m_objIdFactory.FromGuid(new Guid(attr.Value));
			var alreadyBootstrapped = (m_cache != null);
			if (!alreadyBootstrapped)
			{
				// LgWritingSystems already have these,
				// and we don't want to change the hvo.
				m_cache = cache;
				m_hvo = ((IDataReader)loadingServices.m_dataSetup).GetOrAssignHvoFor(m_guid);
			}
			attr = reader.Attribute("ownerguid");
			if (attr != null)
				m_owner = loadingServices.m_objIdFactory.FromGuid(new Guid(attr.Value));

			reader.RemoveAttributes();
			LoadFromDataStoreInternal(reader, loadingServices);

			if (reader.HasElements)
			{
				Logger.WriteEvent("Parsing failed for " + GetType().Name + ". Remaining XML = " + reader);
				throw new InvalidOperationException("Data migration needed. GUID = " + m_guid.Guid +
					" Class = " + GetType().Name);
			}

			// This object is not being initialized,
			// as might be done for a new object,
			// as for the other callers of the SetDefaultValuesAfterInit method.
			//SetDefaultValuesAfterInit();
			DoAdditionalReconstruction();
		}

		/// <summary>
		/// Do any additional Reconstruction work,
		/// after everything else has been done.
		/// </summary>
		/// <remarks>
		/// [NB: This method is *not* for use during original object creation work.
		/// Use <see cref="SetDefaultValuesAfterInit"/> during that first initialization.]
		/// </remarks>
		protected virtual void DoAdditionalReconstruction()
		{ /* Do nothing. */}

		/// <summary>
		///
		/// </summary>
		bool ICmObjectInternal.HasOwner
		{
			get { return Owner == null; }
		}

		/// <summary>
		/// Get a Binary type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		byte[] ICmObjectInternal.GetBinaryProperty(int flid)
		{
			return GetBinaryPropertyInternal(flid);
		}

		/// <summary>
		/// Set a Binary type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The property to read.</param>
		/// <param name="useAccessor"></param>
		void ICmObjectInternal.SetProperty(int flid, byte[] newValue, bool useAccessor)
		{
			SetPropertyInternal(flid, newValue, useAccessor);
		}

		/// <summary>
		/// Get a Boolean type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		bool ICmObjectInternal.GetBoolProperty(int flid)
		{
			return GetBoolPropertyInternal(flid);
		}

		/// <summary>
		/// Set a Boolean type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		void ICmObjectInternal.SetProperty(int flid, bool newValue, bool useAccessor)
		{
			SetPropertyInternal(flid, newValue, useAccessor);
		}

		/// <summary>
		/// Replace items in a sequence.
		/// </summary>
		void ICmObjectInternal.Replace(int flid, int start, int numberToDelete, IEnumerable<ICmObject> thingsToAdd)
		{
			ReplaceInternal(flid, start, numberToDelete, thingsToAdd);
		}

		/// <summary>
		/// Replace items in a collection. (NOT currently implemented for sequences).
		/// </summary>
		void ICmObjectInternal.Replace(int flid, IEnumerable<ICmObject> thingsToRemove, IEnumerable<ICmObject> thingsToAdd)
		{
			ReplaceInternal(flid, thingsToRemove, thingsToAdd);
		}

		/// <summary>
		/// Get a DateTime type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		DateTime ICmObjectInternal.GetTimeProperty(int flid)
		{
			return GetTimePropertyInternal(flid);
		}

		/// <summary>
		/// Set a DateTime type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		void ICmObjectInternal.SetProperty(int flid, DateTime newValue, bool useAccessor)
		{
			SetPropertyInternal(flid, newValue, useAccessor);
		}

		/// <summary>
		/// Get a Guid type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		Guid ICmObjectInternal.GetGuidProperty(int flid)
		{
			return GetGuidPropertyInternal(flid);
		}

		/// <summary>
		/// Set a Guid type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		void ICmObjectInternal.SetProperty(int flid, Guid newValue, bool useAccessor)
		{
			SetPropertyInternal(flid, newValue, useAccessor);
		}

		/// <summary>
		/// Get an integer (int32) type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		int ICmObjectInternal.GetIntegerValue(int flid)
		{
			return GetIntegerValueInternal(flid);
		}

		/// <summary>
		/// Set an integer (int32) type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		void ICmObjectInternal.SetProperty(int flid, int newValue, bool useAccessor)
		{
			SetPropertyInternal(flid, newValue, useAccessor);
		}

		/// <summary>
		/// Get a GenDate type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		GenDate ICmObjectInternal.GetGenDateProperty(int flid)
		{
			return GetGenDatePropertyInternal(flid);
		}

		/// <summary>
		/// Set a GenDate type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		void ICmObjectInternal.SetProperty(int flid, GenDate newValue, bool useAccessor)
		{
			SetPropertyInternal(flid, newValue, useAccessor);
		}

		/// <summary>
		/// Get the value of an atomic reference or owning property, including owner.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		int ICmObjectInternal.GetObjectProperty(int flid)
		{
			return GetObjectPropertyInternal(flid);
		}

		/// <summary>
		/// Set the value of an atomic reference or owning property, including owner.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value (may be null).</param>
		/// <param name="useAccessor"></param>
		void ICmObjectInternal.SetProperty(int flid, ICmObject newValue, bool useAccessor)
		{
			SetPropertyInternal(flid, newValue, useAccessor);
		}

		/// <summary>
		/// Get an ITsString type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		ITsString ICmObjectInternal.GetITsStringProperty(int flid)
		{
			return GetITsStringPropertyInternal(flid);
		}

		/// <summary>
		/// Get a string type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		string ICmObjectInternal.GetStringProperty(int flid)
		{
			return GetStringPropertyInternal(flid);
		}

		/// <summary>
		/// Set a string type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		void ICmObjectInternal.SetProperty(int flid, string newValue, bool useAccessor)
		{
			SetPropertyInternal(flid, newValue, useAccessor);
		}

		/// <summary>
		/// Get an ITsTextProps type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		ITsTextProps ICmObjectInternal.GetITsTextPropsProperty(int flid)
		{
			return GetITsTextPropsPropertyInternal(flid);
		}

		/// <summary>
		/// Set an ITsTextProps type property.
		/// </summary>
		/// <param name="flid">The property to set.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="useAccessor"></param>
		void ICmObjectInternal.SetProperty(int flid, ITsTextProps newValue, bool useAccessor)
		{
			SetPropertyInternal(flid, newValue, useAccessor);
		}

		/// <summary>
		/// Set an ITsString type property.
		/// </summary>
		/// <param name="flid">The property to set.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="useAccessor"></param>
		void ICmObjectInternal.SetProperty(int flid, ITsString newValue, bool useAccessor)
		{
			SetPropertyInternal(flid, newValue, useAccessor);
		}
		/// <summary>
		/// Get an ITsMultiString type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		ITsMultiString ICmObjectInternal.GetITsMultiStringProperty(int flid)
		{
			return GetITsMultiStringPropertyInternal(flid);
		}

		/// <summary>
		/// Get the size of a vector (seq or col) property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		int ICmObjectInternal.GetVectorSize(int flid)
		{
			return GetVectorSizeInternal(flid);
		}

		/// <summary>
		/// Get an vector (seq or col) property item at the given index.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="index">The property to read.</param>
		int ICmObjectInternal.GetVectorItem(int flid, int index)
		{
			return GetVectorItemInternal(flid, index);
		}

		/// <summary>
		/// Get the index of 'hvo' in 'flid'.
		/// Returns -1 if 'hvo' is not in 'flid'.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="hvo">The object to get the index of.</param>
		/// <remarks>
		/// If 'flid' is for a collection, then the returned index is
		/// essentially meaningless, as collections are unordered sets.
		/// </remarks>
		int ICmObjectInternal.GetObjIndex(int flid, int hvo)
		{
			return GetObjIndexInternal(flid, hvo);
		}

		/// <summary>
		/// Get the hvos in a vector (collection or sequence) type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		IEnumerable<ICmObject> ICmObjectInternal.GetVectorProperty(int flid)
		{
			return GetVectorPropertyInternal(flid);
		}

		/// <summary>
		/// Set an vector (col or seq) type property.
		/// </summary>
		/// <param name="flid">The property to set.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="useAccessor"></param>
		void ICmObjectInternal.SetProperty(int flid, Guid[] newValue, bool useAccessor)
		{
			SetPropertyInternal(flid, newValue, useAccessor);
		}

		/// <summary>
		/// Set a custom property.
		/// </summary>
		/// <param name="flid">The property to set.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="useAccessor"></param>
		void ICmObjectInternal.SetProperty(int flid, object newValue, bool useAccessor)
		{
			SetCustomPropertyForSDA(flid, newValue, useAccessor);
		}

		/// <summary>Remove an obejct from its old owning flid.</summary>
		/// <param name='owningFlid'>The previous owning flid.</param>
		/// <param name='removee'>The object that was owned in 'owningFlid'.</param>
		internal virtual void RemoveOwnee(int owningFlid, ICmObject removee)
		{
			throw new ArgumentException("'owningFlid' not valid for this class of object.");
		}

		/// <summary>
		/// Remove one reference to the target object from one of your atomic reference properties.
		/// (It doesn't matter which one because this is called repeatedly, once for each reference to the target.)
		/// Generated code contains a suitable override of this method for each class that has reference atomic properties.
		/// </summary>
		internal virtual void RemoveAReferenceCore(ICmObject target)
		{
		}
		/// <summary>
		/// Replace one reference to the target object from one of your atomic reference properties with the replacement.
		/// (It doesn't matter which one because this is called repeatedly, once for each reference to the target.)
		/// Generated code contains a suitable override of this method for each class that has reference atomic properties.
		/// </summary>
		internal virtual void ReplaceAReferenceCore(ICmObject target, ICmObject replacement)
		{
		}

		#endregion Internal API

		#region Protected API

		#region ISilDataAccess related methods

		/// <summary>
		/// Get the value of an atomic reference or owning property, including owner.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected virtual int GetObjectPropertyInternal(int flid)
		{
			switch (flid)
			{
				default:
					return (int)GetNonModelPropertyForSDA(flid);
				case (int)CmObjectFields.kflidCmObject_Owner:
					return Owner == null ? FdoCache.kNullHvo : Owner.Hvo;
			}
		}

		/// <summary>
		/// Set the value of an atomic reference or owning property, including owner.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value (may be null).</param>
		/// <param name="useAccessor"></param>
		protected virtual void SetPropertyInternal(int flid, ICmObject newValue, bool useAccessor)
		{
			SetNonModelPropertyForSDA(flid, newValue, useAccessor);
		}

		/// <summary>
		/// Get the size of a vector (seq or col) property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected virtual int GetVectorSizeInternal(int flid)
		{
			return GetNonModelPropertyForSdaAsEnumerable(flid).Count();
		}

		/// <summary>
		/// Get an integer type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="index">The property to read.</param>
		protected virtual int GetVectorItemInternal(int flid, int index)
		{
			return GetNonModelPropertyForSdaAsEnumerable(flid).ElementAt(index).Hvo;
		}

		/// <summary>
		/// Get the index of 'hvo' in 'flid'.
		/// Returns -1 if 'hvo' is not in 'flid'.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="hvo">The object to get the index of.</param>
		/// <remarks>
		/// If 'flid' is for a collection, then the returned index is
		/// essentially meaningless, as collections are unordered sets.
		/// </remarks>
		protected virtual int GetObjIndexInternal(int flid, int hvo)
		{
			int index = 0;
			foreach (var obj in GetNonModelPropertyForSdaAsEnumerable(flid))
			{
				if (obj.Hvo == hvo)
					return index;
				index++;
			}
			return -1;
		}

		/// <summary>
		/// Get the hvos in a vector (collection or sequence) type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected virtual IEnumerable<ICmObject> GetVectorPropertyInternal(int flid)
		{
			return GetNonModelPropertyForSdaAsEnumerable(flid);
		}

		/// <summary>
		/// Set an vector (col or seq) type property.
		/// </summary>
		/// <param name="flid">The property to set.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="useAccessor"></param>
		protected virtual void SetPropertyInternal(int flid, Guid[] newValue, bool useAccessor)
		{
			if (m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>().IsCustom(flid))
			{
				var vector = GetNonModelPropertyForSDA(flid) as IFdoVectorInternal;
				if (vector == null)
					throw new InvalidOperationException("Attempted to set a vector type property that is neither a sequence nor a collection: " + flid);
				vector.ReplaceAll(newValue, useAccessor);
			}
			else
			{
				SetVirtualPropertyForSDA(flid, newValue, useAccessor);
			}
		}

		/// <summary>
		/// Replace items in a sequence.
		/// </summary>
		protected virtual void ReplaceInternal(int flid, int start, int numberToDelete, IEnumerable<ICmObject> thingsToAdd)
		{
			var seq = GetNonModelPropertyForSDA(flid);
			if (seq is IFdoList<ICmObject>)
				((IFdoList<ICmObject>)seq).Replace(start, numberToDelete, thingsToAdd);
			else
				throw new InvalidOperationException("Attempted to perform Replace on a property that is not a known sequence: " + flid);
		}

		/// <summary>
		/// Replace items in a collection. (NOT currently implemented for sequences).
		/// </summary>
		protected virtual void ReplaceInternal(int flid, IEnumerable<ICmObject> thingsToRemove, IEnumerable<ICmObject> thingsToAdd)
		{
			var set = GetNonModelPropertyForSDA(flid);
			if (set is IReplaceInSet)
				((IReplaceInSet)set).Replace(thingsToRemove, thingsToAdd);
			else
				throw new InvalidOperationException("Attempted to perform Replace on a property that is not a known collection: " + flid);
		}


		/// <summary>
		/// Get an integer (int32) type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected virtual int GetIntegerValueInternal(int flid)
		{
			switch (flid)
			{
				default:
					return (int)GetNonModelPropertyForSDA(flid);
				case (int)CmObjectFields.kflidCmObject_Class:
					return ClassID;
				case (int)CmObjectFields.kflidCmObject_OwnFlid:
					return (int)OwningFlid;
				case (int)CmObjectFields.kflidCmObject_OwnOrd:
					return OwnOrd;
			}
		}

		/// <summary>
		/// Set an integer (int32) type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		protected virtual void SetPropertyInternal(int flid, int newValue, bool useAccessor)
		{
			SetNonModelPropertyForSDA(flid, newValue, useAccessor);
			// Don't set these properties.
			//case (int)CmObjectFields.kflidCmObject_Class:
			//case (int)CmObjectFields.kflidCmObject_OwnFlid:
			//case (int)CmObjectFields.kflidCmObject_OwnOrd:
		}

		/// <summary>
		/// Get a Boolean type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected virtual bool GetBoolPropertyInternal(int flid)
		{
			return (bool)GetNonModelPropertyForSDA(flid);
		}

		/// <summary>
		/// Set a Boolean type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		protected virtual void SetPropertyInternal(int flid, bool newValue, bool useAccessor)
		{
			SetNonModelPropertyForSDA(flid, newValue, useAccessor);
		}

		/// <summary>
		/// Get a Guid type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected virtual Guid GetGuidPropertyInternal(int flid)
		{
			switch (flid)
			{
				default:
					return (Guid)GetNonModelPropertyForSDA(flid);
				case (int)CmObjectFields.kflidCmObject_Guid:
					return Guid;
			}
		}

		/// <summary>
		/// Set a Guid type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		protected virtual void SetPropertyInternal(int flid, Guid newValue, bool useAccessor)
		{
			SetNonModelPropertyForSDA(flid, newValue, useAccessor);
			// Don't set these properties.
			//case (int)CmObjectFields.kflidCmObject_Guid:
		}

		/// <summary>
		/// Get a DateTime type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected virtual DateTime GetTimePropertyInternal(int flid)
		{
			return (DateTime)GetNonModelPropertyForSDA(flid);
		}

		/// <summary>
		/// Set a DateTime type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		protected virtual void SetPropertyInternal(int flid, DateTime newValue, bool useAccessor)
		{
			SetNonModelPropertyForSDA(flid, newValue, useAccessor);
		}

		/// <summary>
		/// Get a Binary type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected virtual byte[] GetBinaryPropertyInternal(int flid)
		{
			var retval = GetNonModelPropertyForSDA(flid);
			return (retval == null) ? null : (byte[])retval;
		}

		/// <summary>
		/// Set a Binary type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The property to read.</param>
		/// <param name="useAccessor"></param>
		protected virtual void SetPropertyInternal(int flid, byte[] newValue, bool useAccessor)
		{
			SetNonModelPropertyForSDA(flid, newValue, useAccessor);
		}

		/// <summary>
		/// Get an ITsString type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected virtual ITsString GetITsStringPropertyInternal(int flid)
		{
			var retval = GetNonModelPropertyForSDA(flid);
			return (retval == null) ? null : (ITsString)retval;
		}

		/// <summary>
		/// Set an ITsString type property.
		/// </summary>
		/// <param name="flid">The property to set.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="useAccessor"></param>
		protected virtual void SetPropertyInternal(int flid, ITsString newValue, bool useAccessor)
		{
			SetNonModelPropertyForSDA(flid, newValue, useAccessor);
		}

		/// <summary>
		/// Get an ITsMultiString type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected virtual ITsMultiString GetITsMultiStringPropertyInternal(int flid)
		{
			var retval = GetNonModelPropertyForSDA(flid);
			return (retval == null) ? null : (ITsMultiString)retval;
		}

		/// <summary>
		/// Get a string type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected virtual string GetStringPropertyInternal(int flid)
		{
			var retval = GetNonModelPropertyForSDA(flid);
			return (retval == null) ? null : (string)retval;
		}

		/// <summary>
		/// Set a string type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		protected virtual void SetPropertyInternal(int flid, string newValue, bool useAccessor)
		{
			SetNonModelPropertyForSDA(flid, newValue, useAccessor);
		}

		/// <summary>
		/// Get an ITsTextProps type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected virtual ITsTextProps GetITsTextPropsPropertyInternal(int flid)
		{
			var retval = GetNonModelPropertyForSDA(flid);
			return (retval == null) ? null : (ITsTextProps)retval;
		}

		/// <summary>
		/// Set an ITsTextProps type property.
		/// </summary>
		/// <param name="flid">The property to set.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="useAccessor"></param>
		protected virtual void SetPropertyInternal(int flid, ITsTextProps newValue, bool useAccessor)
		{
			SetNonModelPropertyForSDA(flid, newValue, useAccessor);
		}

		/// <summary>
		/// Get a GenDate type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected virtual GenDate GetGenDatePropertyInternal(int flid)
		{
			return (GenDate)GetNonModelPropertyForSDA(flid);
		}

		/// <summary>
		/// Set a GenDate type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		protected virtual void SetPropertyInternal(int flid, GenDate newValue, bool useAccessor)
		{
			SetNonModelPropertyForSDA(flid, newValue, useAccessor);
		}

		#endregion ISilDataAccess related methods

		/// <summary>Get an XML string that represents the entire instance.</summary>
		/// <param name='writer'>The writer in which the XML is placed.</param>
		/// <remarks>Only to be used by backend provider system.</remarks>
		protected virtual void ToXMLStringInternal(XmlWriter writer)
		{
			UserDefinedProperties(writer);
		}

		/// <summary>Reconstruct an instance from data in some data store.</summary>
		/// <remarks>Only to be used by backend provider system.</remarks>
		protected virtual void LoadFromDataStoreInternal(XElement root, LoadingServices loadingServices)
		{
			UserDefinedProperties(root, loadingServices);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete the object.
		/// </summary>
		/// <remarks>
		/// NB: This method should *never, ever* be used directly by FDO clients.
		/// It is used by FDO to handle side effects of setting an atomic owning property,
		/// or removing an object from an owning sequence/collection vector.
		/// The FDO code generator will override this method to do class-specific
		/// deletions, such as deleting owned objects.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual void DeleteObjectBasics()
		{
			// Clean up optional custom properties, especially owning & ref props.
			// Only owning & ref props are worth messing with,
			// since that is all that is processed in the switch.
			// Register deletions and ref prop mods, as well.
			var mdc = Cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			foreach (var flid in mdc.GetFields(ClassID, true, (int)(CellarPropertyTypeFilter.AllOwning | CellarPropertyTypeFilter.AllReference)))
			{
				if (!mdc.IsCustom(flid))
					continue;
				var key = new Tuple<ICmObject, int>(this, flid);
				object data;
				if (!m_cache.CustomProperties.TryGetValue(key, out data))
					continue; // could have this property, but don't.
				CellarPropertyType dataType = (CellarPropertyType)mdc.GetFieldType(flid);
				switch (dataType)
				{
					default:
						break; // Skip these ones.
					case CellarPropertyType.OwningAtomic:
						// Delete object.
						var goner = (((ICmObjectOrId)data).GetObject(Services.ObjectRepository));
						((ICmObjectInternal)goner).DeleteObject();
						break;
					//case CellarPropertyType.ReferenceAtomic: // Skip it.
					//    break;
					case CellarPropertyType.OwningCollection:
						// Delete objects.
						((FdoOwningCollection<ICmObject>)data).Clear();
						break;
					case CellarPropertyType.OwningSequence:
						// Delete objects.
						((FdoOwningSequence<ICmObject>)data).Clear();
						break;
					case CellarPropertyType.ReferenceCollection:
						((FdoReferenceCollection<ICmObject>)data).Clear();
						break;
					case CellarPropertyType.ReferenceSequence:
						((FdoReferenceSequence<ICmObject>)data).Clear();
						break;
				}
				m_cache.CustomProperties.Remove(key);
			}
		}

		void ICmObjectInternal.ClearIncomingRefsOnOutgoingRefs()
		{
			ClearIncomingRefsOnOutgoingRefsInternal();
		}

		/// <summary>
		/// Remove the recipient from the incomingRefs colletions of everything it refers to.
		/// (Used ONLY as part of Undoing object creation, or redoing deletion.)
		/// </summary>
		protected virtual void ClearIncomingRefsOnOutgoingRefsInternal()
		{
			if (Cache == null) // guard which may help with LT-12331. SHOULD never be called on already deleted object.
			{
				Debug.Fail("Clearing incoming refs on outgoing refs for an object already deleted");
				return;
			}
			var mdc = Cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			foreach (var flid in mdc.GetFields(ClassID, true, (int)CellarPropertyTypeFilter.AllReference))
			{
				if (!mdc.IsCustom(flid))
					continue;
				var key = new Tuple<ICmObject, int>(this, flid);
				object data;
				if (!m_cache.CustomProperties.TryGetValue(key, out data))
					continue; // could have this property, but don't.
				CellarPropertyType dataType = (CellarPropertyType)mdc.GetFieldType(flid);
				switch (dataType)
				{
					default:
						break; // Skip these ones.
					case CellarPropertyType.ReferenceAtomic:
						if (data is ICmObjectInternal)
							((ICmObjectInternal)data).RemoveIncomingRef(this);
						break;
					case CellarPropertyType.ReferenceCollection:
						((FdoReferenceCollection<ICmObject>)data).ClearForUndo();
						break;
					case CellarPropertyType.ReferenceSequence:
						((FdoReferenceSequence<ICmObject>)data).ClearForUndo();
						break;
				}
			}
		}
		void ICmObjectInternal.RestoreIncomingRefsOnOutgoingRefs()
		{
			RestoreIncomingRefsOnOutgoingRefsInternal();
		}

		/// <summary>
		/// Restore the recipient to the incomingRefs colletions of everything it refers to.
		/// (Used ONLY as part of Redoing object creation, or Undoing deletion.)
		/// </summary>
		protected virtual void RestoreIncomingRefsOnOutgoingRefsInternal()
		{
			var mdc = Cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			foreach (var flid in mdc.GetFields(ClassID, true, (int)CellarPropertyTypeFilter.AllReference))
			{
				if (!mdc.IsCustom(flid))
					continue;
				var key = new Tuple<ICmObject, int>(this, flid);
				object data;
				if (!m_cache.CustomProperties.TryGetValue(key, out data))
					continue; // could have this property, but don't.
				CellarPropertyType dataType = (CellarPropertyType)mdc.GetFieldType(flid);
				switch (dataType)
				{
					default:
						break; // Skip these ones.
					case CellarPropertyType.ReferenceAtomic:
						if (data is ICmObjectInternal)
							((ICmObjectInternal)data).AddIncomingRef(this);
						break;
					case CellarPropertyType.ReferenceCollection:
						((FdoReferenceCollection<ICmObject>)data).RestoreAfterUndo();
						break;
					case CellarPropertyType.ReferenceSequence:
						((FdoReferenceSequence<ICmObject>)data).RestoreAfterUndo();
						break;
				}
			}
		}
		/// <summary>
		/// Get the preferred writing system identifier for the class.
		/// </summary>
		protected virtual string PreferredWsId
		{
			get { return Services.WritingSystems.DefaultAnalysisWritingSystem.Id; }
		}

		/// <summary>
		/// This method is used by the SDA-friendly methods to get at properties
		/// that are not part of the model, but that are in the metadata cache (MDC).
		/// </summary>
		/// <param name="flid">Virtual field id.</param>
		/// <returns></returns>
		protected virtual object GetVirtualPropertyForSDA(int flid)
		{
			int flidType;
			var fieldName = CheckVirtualProperty(flid, out flidType);
			var propInfo = GetPropertyInfo(fieldName);
			var retval = propInfo.GetGetMethod().Invoke(this, null);

			// Possibly adjust retval depending on flidType.
			switch ((CellarPropertyType)flidType)
			{
				default:
					throw new ArgumentException("'tag' is not a known type.");
				case CellarPropertyType.Boolean: // Fall through.
				case CellarPropertyType.Integer: // Fall through.
				case CellarPropertyType.Time: // Fall through.
				case CellarPropertyType.Guid: // Fall through.
				case CellarPropertyType.Image: // Fall through.
				case CellarPropertyType.GenDate: // Fall through.
				case CellarPropertyType.Binary: // Fall through.
				case CellarPropertyType.Numeric: // Fall through.
				case CellarPropertyType.Float: // Fall through.
				case CellarPropertyType.Unicode: // Fall through.
				case CellarPropertyType.String: // Fall through.
				case CellarPropertyType.MultiString: // Fall through.
				case CellarPropertyType.MultiUnicode: // Fall through.
					// Do nothing.
					break;
				case CellarPropertyType.OwningAtomic:
				case CellarPropertyType.ReferenceAtomic:
					retval = (retval == null) ? 0 : ((ICmObject)retval).Hvo; // SDA wants the hvo, not the object.
					break;
				case CellarPropertyType.OwningCollection:
				case CellarPropertyType.OwningSequence:
				case CellarPropertyType.ReferenceCollection:
				case CellarPropertyType.ReferenceSequence:
					// The value should already be IEnumerable of some type of CmObject for lists.
					if (retval == null)
						return new ICmObject[0];
					// In C# 4.0, this will succeed for any IEnumerable<X> where X is a subclass of ICmObject.
					// Read callers know how to get Objects from IFdoVector. Write callers may try casting
					// to another type, in case we want to handle writeable virtuals.
					if (retval is IEnumerable<ICmObject> || retval is IFdoVector)
						return retval;
					// This will handle almost any collection of ICmObjects; if it's an enumerable of something else,
					// we'll crash, but then, we would have anyway on the following line.
					if (retval is IEnumerable)
						return (retval as IEnumerable).Cast<ICmObject>();
					throw new NotImplementedException("Invalid value type for collection/sequence property returned from invoked method: " + retval.GetType() + ". (int collections are no longer supported).");
			}

			return retval;
		}

		#endregion Protected API

		#region Private API

		private PropertyInfo GetPropertyInfo(string fieldName)
		{
			var propInfo = GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
			if (propInfo == null)
				throw new InvalidOperationException("Property not found. Probably there is a mis-match between the metadata cache name and the code property name.");
			return propInfo;
		}

		private void CheckLegalFlidForObject(int flid)
		{
			// Check that 'flid' is a property of this class of object.
			var mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			var flids = mdc.GetFields(ClassID, true, (int)CellarPropertyTypeFilter.All);
			if (!flids.Contains(flid))
				throw new FDOInvalidFieldException("'flid' is not defined for this class. Use an SDA decorator for your made up property.");
		}

		private string CheckVirtualProperty(int flid, out int flidType)
		{
			var mdc = Cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			// The MDC methods will throw an exception if 'flid' not found, which is a good thing.
			// The exception means this 'flid' is not a legal part of what the domain has to support.
			try
			{
				var isVirtualProperty = mdc.get_IsVirtual(flid);
				if (!isVirtualProperty)
					throw new FDOInvalidFieldException("'flid' must be a virtual field to be used in this method.");
				flidType = mdc.GetFieldType(flid);
				return mdc.GetFieldName(flid);
			}
			catch (FDOInvalidFieldException)
			{
				throw;
			}
			catch
			{
				throw new FDOInvalidFieldException("'flid' is not in the metadata cache. Use an SDA decorator for your madeup property.");
			}
		}

		/// <summary>
		/// This method is used by the SDA-friendly methods to get at properties
		/// that are not part of the model, but that are in the metadata cache (MDC) as user-defined custom fields.
		/// </summary>
		/// <param name="flid">Custom field id.</param>
		/// <returns>The value, or null.</returns>
		private object GetCustomPropertyForSDA(int flid)
		{
			object retval;

			var key = new Tuple<ICmObject, int>(this, flid);

			m_cache.CustomProperties.TryGetValue(key, out retval); // May not be in the dictionary.

			var mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			var dataType = (CellarPropertyType)mdc.GetFieldType(flid);
			if (retval == null)
			{
				// Provide defaults for value type data.
				// Also, provide empty collection objects for vectors,
				// MultiString and MultiUnicode data types.
				switch (dataType)
				{
					default:
						return retval;
					//case CellarPropertyType.Float: // Not in model.
					//	retval = float.MinValue;
					//	break;
					// case CellarPropertyType.Image: // Not in model.
					// case CellarPropertyType.Numeric: // Not in model.
					case CellarPropertyType.Guid:
						retval = Guid.Empty;
						break;
					case CellarPropertyType.Boolean:
						retval = false;
						break;
					case CellarPropertyType.GenDate:
						retval = new GenDate();
						break;
					case CellarPropertyType.Integer:
						retval = 0;
						break;
					case CellarPropertyType.Time:
						retval = new DateTime();
						break;
					case CellarPropertyType.OwningAtomic: // Fall through
					case CellarPropertyType.ReferenceAtomic:
						// Use 0 for null object prop, but do NOT put it in the cache, which should contain
						// null or a CmObject.
						return 0;
					case CellarPropertyType.MultiString:
						retval = new MultiStringAccessor(this, flid);
						break;
					case CellarPropertyType.MultiUnicode:
						retval = new MultiUnicodeAccessor(this, flid);
						break;
					case CellarPropertyType.OwningCollection:
						retval = new FdoOwningCollection<ICmObject>(
							((IServiceLocatorInternal)Cache.ServiceLocator).UnitOfWorkService,
							Cache.ServiceLocator.GetInstance<ICmObjectRepository>(),
							this, flid);
						break;
					case CellarPropertyType.OwningSequence:
						retval = new FdoOwningSequence<ICmObject>(
							((IServiceLocatorInternal)Cache.ServiceLocator).UnitOfWorkService,
							Cache.ServiceLocator.GetInstance<ICmObjectRepository>(),
							this, flid);
						break;
					case CellarPropertyType.ReferenceCollection:
						retval = new FdoReferenceCollection<ICmObject>(
							((IServiceLocatorInternal)Cache.ServiceLocator).UnitOfWorkService,
							Cache.ServiceLocator.GetInstance<ICmObjectRepository>(),
							this, flid);
						break;
					case CellarPropertyType.ReferenceSequence:
						retval = new FdoReferenceSequence<ICmObject>(
							((IServiceLocatorInternal)Cache.ServiceLocator).UnitOfWorkService,
							Cache.ServiceLocator.GetInstance<ICmObjectRepository>(),
							this, flid);
						break;
				}

				// Add it to Dictionary for next time.
				m_cache.CustomProperties[key] = retval;
			}
			else
			{
				// For GenDate types, we may need to convert from an integer to a GenDate object.
				// For atomic owning/ref types, fetch the object's hvo from the surrogate.
				// Otherwise, just return the stored value.
				switch (dataType)
				{
					case CellarPropertyType.GenDate:
						if (!(retval is GenDate))
							retval = GetGenDateFromInt((int)retval);
						break;
					case CellarPropertyType.OwningAtomic: // Fall through
					case CellarPropertyType.ReferenceAtomic:
						retval = ((ICmObjectOrId)retval).GetObject(Services.ObjectRepository).Hvo;
						break;
				}
			}

			return retval;
		}

		/// <summary>
		/// Given its integer representation, return a GenDate object.
		/// </summary>
		internal static GenDate GetGenDateFromInt(int nVal)
		{
			var fAD = true;
			if (nVal < 0)
			{
				fAD = false;
				nVal = -nVal;
			}
			var prec = nVal % 10;
			nVal /= 10;
			var day = nVal % 100;
			nVal /= 100;
			var month = nVal % 100;
			var year = nVal / 100;
			return new GenDate((GenDate.PrecisionType)prec, month, day, year, fAD);
		}

		/// <summary>
		/// This method is used by the SDA-friendly methods to get at properties
		/// that are not part of the model, but that are in the metadata cache (MDC) as user-defined custom fields.
		/// </summary>
		/// <param name="flid">Custom field id.</param>
		/// <param name="newValue">New value. (May be null.)</param>
		/// <param name="useAccessor">'true' to send to UOW. 'false' to not send to the UOW.</param>
		private void SetCustomPropertyForSDA(int flid, object newValue, bool useAccessor)
		{
			var key = new Tuple<ICmObject, int>(this, flid);
			object oldValue;
			m_cache.CustomProperties.TryGetValue(key, out oldValue);

			// Sanity checks.
			var servLoc = m_cache.ServiceLocator;
			var mdc = servLoc.GetInstance<IFwMetaDataCacheManaged>();
			var dataType = (CellarPropertyType)mdc.GetFieldType(flid);
			switch (dataType)
			{
				default:
					break;
				//case CellarPropertyType.Float: // Not in model.
				//	retval = float.MinValue;
				//	break;
				// case CellarPropertyType.Image: // Not in model.
				// case CellarPropertyType.Numeric: // Not in model.
				case CellarPropertyType.Guid:
					if (newValue == null)
						newValue = Guid.Empty;
					else if (!(newValue is Guid))
						throw new ArgumentException("Can't set Guid property to non-Guid value.");
					break;
				case CellarPropertyType.Boolean:
					if (newValue == null)
						newValue = false;
					else if (!(newValue is bool))
						throw new ArgumentException("Can't set boolean property to non-boolean value.");
					break;
				case CellarPropertyType.GenDate:
					if (newValue == null)
						newValue = new GenDate();
					else if (!(newValue is GenDate))
						throw new ArgumentException("Can't set GenDate property to non-GenDate value.");
					break;
				case CellarPropertyType.Integer:
					if (newValue == null)
						newValue = 0;
					else if (!(newValue is int))
						throw new ArgumentException("Can't set int property to non-int value.");
					break;
				case CellarPropertyType.Time:
					if (newValue == null)
						newValue = new DateTime();
					else if (!(newValue is DateTime))
						throw new ArgumentException("Can't set Time property to non-DateTime value.");
					break;
				case CellarPropertyType.OwningAtomic:
					if (newValue != null)
					{
						// set the owner for the new value
						ICmObjectInternal obj;
						var id = newValue as ICmObjectId;
						if (id != null)
							obj = (ICmObjectInternal)m_cache.ServiceLocator.GetObject(id);
						else
							obj = newValue as ICmObjectInternal;

						if (obj == null)
							throw new ArgumentException("New value is not an ICmObject.");

						if (obj.Hvo == (int)SpecialHVOValues.kHvoUninitializedObject)
							// The new value was created using new Foo().
							obj.InitializeNewCmObject(m_cache, this, flid, 0);
						else
						{
							// The new value is already owned by some other object. (or we're doing an Undo/Redo)
							if (useAccessor)
								obj.SetOwner(this, flid, 0);
							else
								((CmObject) obj).m_owner = this;
						}
					}
					break;
				case CellarPropertyType.ReferenceAtomic:
					if (newValue != null)
					{
						// Make sure the newValue is legal for the property.
						// 'newValue' is the object not its hvo int.
						// It might be a CmObjectId on Redo.
						if (!(newValue is ICmObjectId))
						{
							if (!(newValue is ICmObject))
								throw new ArgumentException("New value is not an ICmObject.");
							var newValueAsCmObj = (ICmObject)newValue;
							if (!mdc.get_IsValidClass(flid, newValueAsCmObj.ClassID))
								throw new ArgumentException("Cannot put the new value in this kind of property.");
						}
					}
					break;
				case CellarPropertyType.MultiString: // Fall through.
				case CellarPropertyType.MultiUnicode: // Fall through.
				case CellarPropertyType.OwningCollection: // Fall through.
				case CellarPropertyType.OwningSequence: // Fall through.
				case CellarPropertyType.ReferenceCollection: // Fall through.
				case CellarPropertyType.ReferenceSequence:
					throw new NotSupportedException("Directly setting this property is not supported. Use the returned vector or accessor instead.");
			}

			m_cache.CustomProperties[key] = newValue;

			if (useAccessor)
			{
				// Send off to the UOW.
				((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterObjectAsModified(this, flid, oldValue, newValue);
				// Change needed here to keep track of incoming references.
				if (dataType == CellarPropertyType.ReferenceAtomic)
				{
					if (oldValue != null)
					{
						var obj = GetRealObject(oldValue);
						obj.RemoveIncomingRef(this);
					}
					if (newValue != null)
					{
						var obj = GetRealObject(newValue);
						obj.AddIncomingRef(this);
					}
				}
				if (dataType == CellarPropertyType.OwningAtomic && oldValue != null)
				{
					// delete the old owned object
					var obj = GetRealObject(oldValue);
					obj.DeleteObject();
				}
			}
		}

		private ICmObjectInternal GetRealObject(object oldValue)
		{
			var id = oldValue as ICmObjectId;
			if (id != null)
				return (ICmObjectInternal)m_cache.ServiceLocator.GetObject(id);
			else
				return (ICmObjectInternal)oldValue;
		}

		/// <summary>
		/// Reconstitute the optional user-defined custom properties.
		/// </summary>
		/// <param name="writer"></param>
		private void UserDefinedProperties(XmlWriter writer)
		{
			var mdc = Cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			var wsf = Cache.WritingSystemFactory;
			foreach (var flid in mdc.GetFields(ClassID, true, (int)CellarPropertyTypeFilter.All))
			{
				// When deleting several custom fields at once, races can occur...
				if (!mdc.FieldExists(flid) || !mdc.IsCustom(flid))
					continue;

				var dataType = (CellarPropertyType)mdc.GetFieldType(flid);
				var key = new Tuple<ICmObject, int>(this, flid);
				object data;
				var isValueType = mdc.IsValueType(dataType);
				if (!m_cache.CustomProperties.TryGetValue(key, out data) && !isValueType)
					continue; // could have this property, but don't.
				if (data == null)
					if (isValueType)
					{
						data = GetDefaultValueData(dataType);
					}
					else
					{
						continue; // a pre-existing reference or string property is now null; don't write it out
					}
				// Write the custom field.
				writer.WriteStartElement("Custom");

				var fieldname = mdc.GetFieldName(flid);
				writer.WriteAttributeString("name", fieldname);
				switch (dataType)
				{
					default:
						throw new InvalidOperationException("Data type not recognized.");
					//case CellarPropertyType.Float: // Not in model.
					//	retval = float.MinValue;
					//	break;
					// case CellarPropertyType.Image: // Not in model.
					// case CellarPropertyType.Numeric: // Not in model.
					case CellarPropertyType.Guid: // Fall through.
					case CellarPropertyType.Boolean: // Fall through.
					case CellarPropertyType.Integer:
						writer.WriteAttributeString("val", data.ToString());
						break;
					case CellarPropertyType.GenDate:
						if (data is GenDate)
							ReadWriteServices.WriteGenDateAttribute(writer, "val", (GenDate)data);
						else // probably an integer...
							writer.WriteAttributeString("val", data.ToString());
						break;
					case CellarPropertyType.Time: // DateTime.
						// Write date in non-lacale, transportable format.
						var date = (DateTime)data;
						writer.WriteAttributeString("val",
							String.Format("{0}-{1}-{2} {3}:{4}:{5}.{6}",
								date.Year, date.Month, date.Day,
								date.Hour, date.Minute, date.Second, date.Millisecond));
						break;
					case CellarPropertyType.Unicode:
						writer.WriteStartElement("Uni"); // Open Uni element.
						writer.WriteString(Icu.Normalize((string)data, Icu.UNormalizationMode.UNORM_NFC));
						writer.WriteEndElement(); // Close Uni element.
						break;
					case CellarPropertyType.String:
						var tsString = (ITsString)data;
						int temp;
						var wsHvo = tsString.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out temp);
						writer.WriteRaw(TsStringUtils.GetXmlRep(tsString, wsf, wsHvo));
						break;
					case CellarPropertyType.Binary:
						// Currently no Custom fields use Binary; The model uses it for StStyle.Rules and StPara.StyleRules
						var byteArray = "";
						foreach (byte val in (byte[])data)
						{
							if (byteArray.Length > 0)
								byteArray += ".";
							byteArray += val.ToString();
						}
						writer.WriteStartElement("Binary"); // Open Binary element.
						writer.WriteString(byteArray);
						writer.WriteEndElement(); // Close Binary element.
						break;

					case CellarPropertyType.OwningAtomic: // Fall through.
					case CellarPropertyType.ReferenceAtomic:
						var id = (ICmObjectOrId)data;
						id.Id.ToXMLString(dataType == CellarPropertyType.OwningAtomic, writer);
						break;
					case CellarPropertyType.MultiString: // Fall through.
					case CellarPropertyType.MultiUnicode:
						var multiAccessor = (MultiAccessor)data;
						multiAccessor.ToXMLString(writer);
						break;
					case CellarPropertyType.OwningCollection: // Fall through.
					case CellarPropertyType.OwningSequence: // Fall through.
					case CellarPropertyType.ReferenceCollection: // Fall through.
					case CellarPropertyType.ReferenceSequence:
						var refVector = (IFdoVectorInternal)data;
						refVector.ToXMLString(writer);
						break;
				}

				writer.WriteEndElement(); // End Custom element.
			}
		}

		private object GetDefaultValueData(CellarPropertyType dataType)
		{
			switch (dataType)
			{
				default:
					throw new InvalidOperationException("GetDefaultValueData only handles Value types.");
				case CellarPropertyType.Guid:
					return Guid.Empty;
				case CellarPropertyType.Binary:
					return new byte[0];
				case CellarPropertyType.Boolean:
					return false;
				case CellarPropertyType.Integer:
					return 0;
				case CellarPropertyType.GenDate:
					return new GenDate();
				case CellarPropertyType.Time:
					return new DateTime();
			}
		}

		/// <summary>
		/// Reconstitute the optional user-defined custom properties.
		/// </summary>
		private void UserDefinedProperties(XElement rtElement, LoadingServices loadingServices)
		{
			if (!rtElement.HasElements)
				return;

			// Reconstitute the data for each <Custom> element.
			// Attrs: 'name' (required), 'val' (required for basic data types, absent for others)
			// Non-basic data types will have nested content appropriate for each data type.
			// If non-basic data types do NOT have nested content, this should not crash.
			//var servLoc = Cache.ServiceLocator;
			var mdc = loadingServices.m_mdcManaged;
			var wsf = loadingServices.m_wsf;
			var tsf = loadingServices.m_tsf;
			var uowService = loadingServices.m_uowService;
			var surrRepos = loadingServices.m_surrRepository;
			var cmObjRepos = loadingServices.m_cmObjRepository;

			var customElements = rtElement.Elements("Custom");
			foreach (var customPropertyElement in customElements)
			{
				var fieldName = customPropertyElement.Attribute("name").Value;
				var flid = mdc.GetFieldId2(ClassID, fieldName, true);
				var dataType = (CellarPropertyType)mdc.GetFieldType(flid);
				object data = null;
				XElement myElement = null;
				switch (dataType)
				{
					default:
						throw new InvalidOperationException("Data type not recognized.");
					//case CellarPropertyType.Float: // Not in model.
					//	retval = float.MinValue;
					//	break;
					// case CellarPropertyType.Image: // Not in model.
					// case CellarPropertyType.Numeric: // Not in model.
					case CellarPropertyType.Guid: // Fall through.
					case CellarPropertyType.Boolean: // Fall through.
					case CellarPropertyType.GenDate: // Fall through.
					case CellarPropertyType.Integer:
						var attr = customPropertyElement.Attribute("val");
						switch (dataType)
						{
							case CellarPropertyType.Guid:
								data = new Guid(attr.Value);
								break;
							case CellarPropertyType.Boolean:
								data = bool.Parse(attr.Value);
								break;
							case CellarPropertyType.GenDate: // Fall through.
							case CellarPropertyType.Integer:
								data = Int32.Parse(attr.Value);
								break;
						}
						break;
					case CellarPropertyType.Time: // DateTime.
						// Read date in non-locale, transportable format.
						var dtParts = customPropertyElement.Attribute("val").Value.Split(new[] { '-', ' ', ':', '.' });
						data = new DateTime(
									Int32.Parse(dtParts[0]),
									Int32.Parse(dtParts[1]),
									Int32.Parse(dtParts[2]),
									Int32.Parse(dtParts[3]),
									Int32.Parse(dtParts[4]),
									Int32.Parse(dtParts[5]),
									Int32.Parse(dtParts[6]));
						break;
					case CellarPropertyType.Unicode:
						myElement = customPropertyElement.Element("Uni");
						if (myElement != null)
							data = Icu.Normalize(myElement.Value, Icu.UNormalizationMode.UNORM_NFD);
						break;
					case CellarPropertyType.String:
						if (customPropertyElement.HasElements)
							data = TsStringSerializer.DeserializeTsStringFromXml(
								(XElement)customPropertyElement.FirstNode, wsf);
						break;
					case CellarPropertyType.Binary:
						myElement = customPropertyElement.Element("Binary");
						if (myElement != null)
						{
							var byteArray = myElement.Value;
							if (byteArray.Length > 0)
							{
								var tokens = byteArray.Split('.');
								var bytes = new byte[tokens.Length];
								for (var i = 0; i < tokens.Length; ++i)
								{
									byte b;
									Byte.TryParse(tokens[i], out b);
									bytes[i] = b;
								}
								data = bytes;
							}

						}
						break;
					case CellarPropertyType.OwningAtomic: // Fall through
					case CellarPropertyType.ReferenceAtomic:
						myElement = customPropertyElement.Element("objsur");
						if (myElement != null)
							data = surrRepos.GetId(myElement);
						break;
					case CellarPropertyType.MultiString:
						data = new MultiStringAccessor(this, flid);
						((MultiAccessor)data).LoadFromDataStoreInternal(customPropertyElement, wsf, tsf);
						break;
					case CellarPropertyType.MultiUnicode:
						data = new MultiUnicodeAccessor(this, flid);
						((MultiAccessor)data).LoadFromDataStoreInternal(customPropertyElement, wsf, tsf);
						break;
					case CellarPropertyType.OwningCollection:
						data = new FdoOwningCollection<ICmObject>(
							uowService,
							cmObjRepos,
							this, flid);
						((IFdoVectorInternal)data).LoadFromDataStoreInternal(customPropertyElement, loadingServices.m_objIdFactory);
						break;
					case CellarPropertyType.OwningSequence:
						data = new FdoOwningSequence<ICmObject>(
							uowService,
							cmObjRepos,
							this, flid);
						((IFdoVectorInternal)data).LoadFromDataStoreInternal(customPropertyElement, loadingServices.m_objIdFactory);
						break;
					case CellarPropertyType.ReferenceCollection:
						data = new FdoReferenceCollection<ICmObject>(
							uowService,
							cmObjRepos,
							this, flid);
						((IFdoVectorInternal)data).LoadFromDataStoreInternal(customPropertyElement, loadingServices.m_objIdFactory);
						break;
					case CellarPropertyType.ReferenceSequence:
						data = new FdoReferenceSequence<ICmObject>(
							uowService,
							cmObjRepos,
							this, flid);
						((IFdoVectorInternal)data).LoadFromDataStoreInternal(customPropertyElement, loadingServices.m_objIdFactory);
						break;
				}
				if (data != null)
				{
					var key = new Tuple<ICmObject, int>(this, flid);
					// Usually Add would work, but this routine is also used to restore things on Undo,
					// when the key may already have a value, which Add does not allow.
					lock (m_cache)
					{
						m_cache.CustomProperties[key] = data;
					}
				}
			}
			customElements.Remove();
		}

		#endregion Private API

		#region ICmObject Members (partial)

		/// <summary>
		/// Get the services (strictly the service locator, but this is shorter).
		/// </summary>
		public IFdoServiceLocator Services
		{
			get { return m_cache.ServiceLocator; }
		}
		internal IServiceLocatorInternal InternalServices
		{
			get { return (IServiceLocatorInternal)m_cache.ServiceLocator; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the closest owner, if any, of the specified class; if there is none answer
		/// null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ICmObject OwnerOfClass(int clsid)
		{
			return (Owner == null || Owner.ClassID == clsid) ? Owner : Owner.OwnerOfClass(clsid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the closest owner, if any, of the specified class; if there is none answer
		/// null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public T OwnerOfClass<T>() where T : ICmObject
		{
			return (Owner == null || Owner is T) ? (T)Owner : Owner.OwnerOfClass<T>();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the index of this object in the owner's collection.
		/// </summary>
		/// <returns>Index in owner's collection, or -1 if not in collection.</returns>
		/// ------------------------------------------------------------------------------------
		public int IndexInOwner
		{
			get
			{
				int index;
				int owningFlid = OwningFlidAndIndex(true, out index);
				if (owningFlid == 0)
					return -1; // not owned anywhere.
				if (index == -1)
				{
					// a set: the contract of this method requires it to get an index anyway.
					return ((ICmObjectInternal)Owner).GetObjIndex(owningFlid, Hvo);
				}
				if (index < 0)
					return -1; // some weird case, check for paranoia.
				return index;
			}
		}

		/// <summary>
		/// Gets the object which, for the indicated property of the recipient, the user is
		/// most likely to want to edit if the ReferenceTargetCandidates do not include the
		/// target he wants.
		/// The canonical example, supported by the default implementation of
		/// ReferenceTargetCandidates, is a possibility list, where the targets are the items.
		/// Subclasses which have reference properties edited by the simple list chooser
		/// should generally override either this or ReferenceTargetCandidates or both.
		/// The implementations of the two should naturally be consistent.
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		public virtual ICmObject ReferenceTargetOwner(int flid)
		{
			return ReferenceTargetServices.CmObjectReferenceTargetOwner(m_cache, flid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a set of CmObjects that are suitable for targets to a reference property.
		/// Subclasses should override this method to return a sensible list of objects.
		/// Alternatively, or as well, they should override ReferenceTargetOwner (the latter
		/// alone may be overridden if the candidates are the items in a possibility list,
		/// independent of the recipient object).
		/// </summary>
		/// <param name="flid">The reference property that can store the objects.</param>
		/// <returns>A set of objects</returns>
		/// ------------------------------------------------------------------------------------
		public virtual IEnumerable<ICmObject> ReferenceTargetCandidates(int flid)
		{
			var owner = ReferenceTargetOwner(flid) as ICmPossibilityList;
			if (owner != null)
				return owner.PossibilitiesOS.Cast<ICmObject>();

			var type = (CellarPropertyType)m_cache.MetaDataCache.GetFieldType(flid);
			if (type == CellarPropertyType.ReferenceSequence || type == CellarPropertyType.ReferenceCollection)
				return GetVectorPropertyInternal(flid);
			return new ICmObject[0]; // easiest empty enumeration.
		}

		#endregion

		#region ICmObjectOrId Members

		ICmObjectId ICmObjectOrId.Id
		{
			get { return m_guid; }
		}

		ICmObject ICmObjectOrId.GetObject(ICmObjectRepository repo)
		{
			return this;
		}

		/// <summary>
		/// Don't need the argument for this implementation, just return it.
		/// </summary>
		int ICmObjectOrIdInternal.GetHvo(Infrastructure.Impl.IdentityMap identityMap)
		{
			return Hvo;
		}

		#endregion

		// These methods allow a real CmObject to be used where we would sometimes use a surrogate.
		#region ICmObjectOrSurrogate Members

		string ICmObjectOrSurrogate.XML
		{
			get { return ((ICmObjectInternal)this).ToXmlString(); }
		}

		ICmObjectId ICmObjectOrSurrogate.Id
		{
			get { return Id; }
		}

		string ICmObjectOrSurrogate.Classname
		{
			get { return ClassName; }
		}

		byte[] ICmObjectOrSurrogate.XMLBytes
		{
			get { return ((ICmObjectInternal)this).ToXmlBytes(); }
		}

		/// <summary>
		/// This method is common to objects and surrogates, and on a surrogate, gets the object that
		/// it stands for. If we already have an object, we just want that object (i.e., this).
		/// </summary>
		ICmObject ICmObjectOrSurrogate.Object
		{
			get { return this; }
		}

		/// <summary>
		/// An actual object can never be a surrogate that hasn't been reconstituted!
		/// </summary>
		bool ICmObjectOrSurrogate.HasObject
		{
			get { return true; }
		}

		#endregion

		#region IReferenceSource Members

		/// <summary>
		/// Implementation is virtual (and internal, and generated).
		/// </summary>
		/// <param name="target"></param>
		void IReferenceSource.RemoveAReference(ICmObject target)
		{
			RemoveAReferenceCore(target);
		}

		/// <summary>
		/// Implementation is virtual (and internal, and generated).
		/// </summary>
		void IReferenceSource.ReplaceAReference(ICmObject target, ICmObject replacement)
		{
			ReplaceAReferenceCore(target, replacement);
		}

		ICmObject IReferenceSource.Source
		{
			get { return this; }
		}

		bool IReferenceSource.RefersTo(ICmObject target, int flid)
		{
			// Always ensure the flid is valid.
			var propsToMonitor = new HashSet<Tuple<int, int>>();
			return ( IsFieldRelevant(flid, propsToMonitor) && ((this as ICmObjectInternal).GetObjectProperty(flid) == target.Hvo) );
		}

		#endregion

		/// <summary>
		/// Get all the objects that refer to this.
		/// </summary>
		public HashSet<ICmObject> ReferringObjects
		{
			get
			{
				EnsureCompleteIncomingRefs();
				var result = new HashSet<ICmObject>();
				foreach (var referrer in m_incomingRefs)
					result.Add(referrer.Source);
				return result;
			}
		}

		/// <summary>
		/// Ensure that all objects that refer to this one are properly inclued in its m_incomingRefs collection.
		/// </summary>
		public void EnsureCompleteIncomingRefs()
		{
			if (Services.ObjectRepository.WasCreatedThisSession(this))
				return; // can't have refs from objects that aren't fluffed up if it is new.
			var objrepo = (ICmObjectRepositoryInternal)Services.ObjectRepository;
			var mdc = Services.MetaDataCache;
			foreach (var flid in mdc.GetIncomingFields(ClassID, (int)CellarPropertyTypeFilter.AllReference))
			{
				try
				{
					objrepo.EnsureCompleteIncomingRefsFrom(flid);
				}
				catch (FDOInvalidFieldException e)
				{
					// This could happen if the (custom) flid was deleted during this session.
					// Go to the next flid.
				}
			}
		}

		/// <summary>
		/// This method should be called ONLY(!!) by generated get acessors for atomic reference properties,
		/// when the value stored in the private variable is a CmObjectID. It returns the necessary object,
		/// but also does any side effects needed when the id is being replaced with a real reference.
		/// Currently that includes recording the referring object as an incoming reference of the target.
		/// It returns the
		/// </summary>
		internal ICmObject ConvertIdToAtomicRef(ICmObjectId id)
		{
			var result = Services.GetInstance<ICmObjectRepository>().GetObject(id);
			((ICmObjectInternal)result).AddIncomingRef(this);
			return result;
		}

		internal ICmObject ConvertIdToObject(ICmObjectId id)
		{
			return Services.GetInstance<ICmObjectRepository>().GetObject(id);
		}

		/// <summary>
		/// Add to the collector all the objects to which you have references.
		/// </summary>
		public void AllReferencedObjects(List<ICmObject> collector)
		{
			AddAllReferencedObjectsInternal(collector);
		}

		// subclass methods are generated.
		internal virtual void AddAllReferencedObjectsInternal(List<ICmObject> collector)
		{

		}

		#region ICmObjectInternal Members


		void ICmObjectInternal.AddIncomingRef(IReferenceSource source)
		{
			m_incomingRefs.Add(source);
		}

		void ICmObjectInternal.RemoveIncomingRef(IReferenceSource source)
		{
			m_incomingRefs.Remove(source);
		}

		/// <summary>
		/// Enumerate all the CmObjects that refer to the target. (At most once each, even if
		/// a given target has multiple references.)
		/// </summary>
		IEnumerable<ICmObject> ICmObjectInternal.IncomingRefsFrom(int flid)
		{
			return (from referrer in m_incomingRefs where referrer.RefersTo(this, flid) select referrer.Source).Distinct();
		}

		/// <summary>
		/// Returns a count of the incoming references that are NOT from one of the specified objects.
		/// </summary>
		int ICmObjectInternal.IncomingRefsNotFrom(HashSet<ICmObject> sources)
		{
			return (from referrer in m_incomingRefs where !sources.Contains(referrer.Source) select referrer).Count();
		}

		/// <summary>
		/// On objects that have a DateModified property, update it to Now. Other objects do nothing.
		/// </summary>
		void ICmObjectInternal.UpdateDateModified()
		{
			UpdateDateModifiedInternal();
		}

		/// <summary>
		/// Generated code overrides this in classes that have a DateModified property.
		/// </summary>
		internal virtual void UpdateDateModifiedInternal()
		{
			// The default does nothing.
		}

		/// <summary>
		/// Add to the set the object whose DateModified should be updated, given that the recipient
		/// has been modified. The object to add may be the recipient or one of its owners, the
		/// closest one that has a DateModified property.
		/// </summary>
		void ICmObjectInternal.CollectDateModifiedObject(HashSet<ICmObjectInternal> owners)
		{
			CollectDateModifiedObjectInternal(owners);
		}

		/// <summary>
		/// Add to the set the object whose DateModified should be updated, given that the recipient
		/// has been modified. The object to add may be the recipient or one of its owners, the
		/// closest one that has a DateModified property.
		/// Generated code overrides this in classes that have a DateModified property.
		/// </summary>
		internal virtual void CollectDateModifiedObjectInternal(HashSet<ICmObjectInternal> owners)
		{
			var owner = Owner as CmObject;
			if (owner != null)
				owner.CollectDateModifiedObjectInternal(owners);
		}

		#endregion
	}
}
