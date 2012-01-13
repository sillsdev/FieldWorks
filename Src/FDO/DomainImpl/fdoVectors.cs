// --------------------------------------------------------------------------------------------
// Copyright (C) 2007 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: fdoVectors.cs
// Responsibility: Randy Regnier
// Last reviewed: never
//
// <remarks>
// Implementation of:
//
// FdoSet<T> : ICollection<T>
//		FdoOwningCollection<T> : FdoSet<T>
//		FdoReferenceCollection<T> : FdoSet<T>
// FdoList<T> : IList<T>
//		FdoOwningSequence<T> : FdoList<T>
//		FdoReferenceSequence<T> : FdoList<T>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// <summary>
	/// Base class for the two main types of sets (Collections) in FDO.
	/// </summary>
	/// <typeparam name="T">Some kind of CmObject class.</typeparam>
	internal abstract class FdoSet<T> : IFdoSet<T>, IFdoVectorInternal, IFdoClearForDelete
		where T : class, ICmObject
	{
		#region Data Members

		/// <summary>The items.</summary>
		/// <remarks>
		/// The Key is a base 64 string representation of the object's guid.
		/// The Value is the object. The Value will be null, if the object
		/// has not been accessed before. On first access, the Value gets set to the object.
		/// </remarks>
		protected readonly HashSet<ICmObjectOrId> m_items = new HashSet<ICmObjectOrId>(new ObjectIdEquater());
		protected readonly IUnitOfWorkService m_uowService;
		private readonly IRepository<T> m_repository;

		/// <summary>The object that has the collection property in 'm_flid'.</summary>
		private readonly ICmObjectInternal m_mainObject;

		/// <summary>The field ID of the collection property.</summary>
		private readonly int m_flid;

		#endregion Data Members

		#region Properties

		/// <summary>
		/// Gets the object that holds the set of objects.
		/// </summary>
		protected internal ICmObjectInternal MainObject
		{
			get { return m_mainObject; }
		}

		/// <summary>
		/// Get the field ID for the vector.
		/// </summary>
		protected internal int Flid
		{
			get { return m_flid; }
		}

		/// <summary>
		/// Gets the synchronization root. This is the object that should be
		/// used for all locking in this set. Used for m_items, since it
		/// can change during FDO reads
		/// </summary>
		/// <value>The synchronization root.</value>
		protected object SyncRoot
		{
			get
			{
				return this;
			}
		}

		#endregion Properties

		#region Construction and Initializing

		/// <summary>
		/// Construct an object which allows access to a collection attribute
		/// </summary>
		/// <param name="uowService"></param>
		/// <param name="repository"></param>
		/// <param name="mainObject">The object that holds the set of objects.</param>
		/// <param name="flid">The field id for the property.</param>
		protected FdoSet(IUnitOfWorkService uowService, IRepository<T> repository, ICmObject mainObject, int flid)
		{
			// JohnT: removed some validation from here; this method is called only by generated code, the risk seems
			// very low, and the validation we were doing took significant time.
			m_uowService = uowService;
			m_repository = repository;
			m_mainObject = (ICmObjectInternal)mainObject;
			m_flid = flid;
		}

		/// <summary>
		/// Make sure the object being added to the vector is
		/// minimally valid. It can't be null or deleted.
		/// <para>
		/// Should be overridden if subclasses want to add more checks,
		/// or if they want do something with a brand new object, like initiaslize it.
		/// </para>
		/// </summary>
		/// <param name="obj">Object to check.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="obj"/> is a null reference.
		/// </exception>
		/// <exception cref="FDOObjectDeletedException">
		/// <paramref name="obj"/> has been deleted.
		/// </exception>
		protected virtual void BasicValidityCheck(T obj)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");
			if (obj.Hvo == (int)SpecialHVOValues.kHvoObjectDeleted)
				throw new FDOObjectDeletedException("Object has been deleted.");
		}

		/// <summary>Get an XML string that represents the entire instance.</summary>
		/// <param name='writer'>The writer in which the XML is placed.</param>
		/// <remarks>Only to be used by backend provider system.</remarks>
		void IFdoVectorInternal.ToXMLString(XmlWriter writer)
		{
			lock (SyncRoot)
			{
				foreach (var idOrObject in m_items)
					ReadWriteServices.WriteObjectReference(
						writer,
						idOrObject.Id.Guid,
						IsOwningVector ? ObjectPropertyType.Owning : ObjectPropertyType.Reference);
			}
		}

		/// <summary>		/// See if vector is for owning properties (true), or refenence properties (false).
		/// </summary>
		protected virtual bool IsOwningVector
		{
			get { return true; }
		}

		/// <summary>
		/// Load/Reconstitute data using XElement.
		/// </summary>
		void IFdoVectorInternal.LoadFromDataStoreInternal(XElement reader, ICmObjectIdFactory factory)
		{
			if (reader == null) throw new ArgumentNullException("reader");
			if (factory == null) throw new ArgumentNullException("factory");
			foreach (var objsurElement in reader.Elements("objsur"))
				m_items.Add(ReadWriteServices.LoadAtomicObjectProperty(objsurElement, factory));
		}

		#endregion Construction and Initializing

		#region Other methods

		internal virtual void AddObject(AddObjectEventArgs args)
		{
			MainObject.AddObjectSideEffects(args);
		}

		/// <summary>
		/// This is overridden in owning collections to delete the object.
		/// It should be called by methods that remove objects, AFTER RemoveObjectSideEffects.
		/// </summary>
		protected virtual void DeleteObject(ICmObjectInternal goner)
		{ /* Do nothing. */ }

		/// <summary>
		/// This is overridden in reference collections to remove incoming references to the object.
		/// It should be called by methods that remove objects, BEFORE RemoveObjectSideEffects.
		/// </summary>
		protected virtual void RemoveRefsTo(ICmObjectInternal goner)
		{ /* Do nothing. */ }

		/// <summary>
		/// Replace the entire contents of the property with 'newValue'.
		/// When useAccessor is true, it calls Add() repeatedly, as when making a primary change to data.
		/// When it is false, it is being used for Undo/Redo, and just changes the list.
		/// </summary>
		void IFdoVectorInternal.ReplaceAll(Guid[] newValue, bool useAccessor)
		{
			if (useAccessor)
			{
				Clear();
				foreach (var guid in newValue)
					Add(m_repository.GetObject(guid));
			}
			else
			{
				ClearForUndo();

				m_items.Clear();
				var idMaker = MainObject.Services.GetInstance<ICmObjectIdFactory>();
				foreach (var guid in newValue)
				{
					ICmObjectId id = idMaker.FromGuid(guid);
					m_items.Add(m_repository.GetObject(id)); // both versions of RestoreObjects need a real object.
				}
				RestoreAfterUndo();
			}
		}

		public virtual void RestoreAfterUndo()
		{
		}

		public virtual void ClearForUndo()
		{
		}

		protected T FluffUpObjectIfNeeded(ICmObjectOrId objOrId)
		{
			// This preliminary check saves us looking up the repository if we don't need it
			// and allows us to replace the ID with the fluffed up one..
			if (objOrId is T)
				return objOrId as T;
			//var result = (T) objOrId.GetObject(MainObject.Services.GetInstance<ICmObjectRepository>());
			var result = (T)objOrId.GetObject(m_mainObject.Cache.ServiceLocator.ObjectRepository);
			// Replace with fluffed up one for next time; we must do the remove or nothing changes, since
			// the set thinks the object is already present.
			m_items.Remove(objOrId);
			m_items.Add(result);
			FluffUpSideEffects(result);
			return result;
		}

		internal virtual void FluffUpSideEffects(object result)
		{
			// Do nothing...see ReferenceCollection
		}
		/// <summary>
		/// Replace the indicated objects (possibly none) with the new objects (possibly none).
		/// In the case of owning properties, the removed objects are really deleted; this code does
		/// not handle the possibility that some of them are included in thingsToAdd.
		/// The code will handle both newly created objects and ones being moved from elsewhere
		/// (but not from the same set, because that doesn't make sense).
		/// </summary>
		public virtual void Replace(IEnumerable<ICmObject> thingsToRemove, IEnumerable<ICmObject> thingsToAdd)
		{
			foreach (var obj in thingsToRemove)
				Remove((T)obj);
			foreach (var obj in thingsToAdd)
				Add((T)obj);
			// Something like this might be more efficient, but it's mostly used for single objects.
			// If we do try something more efficient, need overrides like Add(), e.g. for newly owned objects.
			//ICmObject obj;
			//m_items.AddRange(thingsToAdd.Select(item => (item as ICmObjectInternal).GetSurrogate()));
		}
		/// <summary>
		/// Get an array of all CmObjects.
		/// </summary>
		/// <returns></returns>
		public T[] ToArray()
		{
			// We need the inner ToArray() because FluffUp may modify the collection.
			lock (SyncRoot)
				return (from objOrId in m_items.ToArray() select FluffUpObjectIfNeeded(objOrId)).ToArray();
		}

		#endregion Other methods

		#region ICollection<T> implementation

		/// <summary>
		/// Get the number of CmObjects in the Set.
		/// </summary>
		public int Count
		{
			get
			{
				lock (SyncRoot)
					return m_items.Count;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the Set is readonly.
		/// This is always <c>false</c>
		/// </summary>
		public virtual bool IsReadOnly
		{
			get { return false; }
		}

		/// <summary>
		/// Add an item to the Set.
		/// </summary>
		/// <param name="obj">The item to add.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="obj"/> is a null reference.
		/// </exception>
		/// <exception cref="FDOObjectDeletedException">
		/// <paramref name="obj"/> has been deleted.
		/// </exception>
		public virtual void Add(T obj)
		{
			BasicValidityCheck(obj);

			if (Contains(obj))
				return;

			AddObjectEventArgs eventArgs = new AddObjectEventArgs(obj, Flid, -1);
			MainObject.ValidateAddObject(eventArgs);

			var originalValue = ToGuidArray();

			var cobj = m_items.Count;
			m_items.Add(obj);

			AddObject(eventArgs);

			Guid[] newValue;
			if (m_items.Count > cobj)
			{
				// Copying the array of guids is faster than calling ToGuidArray() again!
				newValue = new Guid[originalValue.Length + 1];
				for (var i = 0; i < originalValue.Length; ++i)
					newValue[i] = originalValue[i];
				newValue[originalValue.Length] = obj.Guid;
			}
			else
			{
				newValue = originalValue;
			}

			m_uowService.RegisterObjectAsModified(m_mainObject, m_flid, originalValue, newValue);
		}

		/// <summary>
		/// Removes all items from the Set.
		/// </summary>
		public void Clear()
		{
			((IFdoClearForDelete)this).Clear(false);
		}
		/// <summary>
		/// Removes all items from the Set. Only DeleteObjectBasics overrides should pass forDeletion true,
		/// which bypasses registering the object as modified (and maybe eventually some validity checks).
		/// </summary>
		void IFdoClearForDelete.Clear(bool forDeletion)
		{
			var originalValue = ToGuidArray();

			ICmObjectRepository repo = MainObject.Services.GetInstance<ICmObjectRepository>();
			var goners = new HashSet<ICmObjectInternal>();
			foreach (var obj in m_items.ToArray())
			{
				var realObj = obj.GetObject(repo);
				m_items.Remove(obj);
				RemoveRefsTo((ICmObjectInternal)realObj); // must call before RemoveObjectSideEffects; only does stuff in ref props
				goners.Add((ICmObjectInternal)realObj);
				MainObject.RemoveObjectSideEffects(new RemoveObjectEventArgs(realObj, Flid, -1));
			}

			// Make sure we update the list before we delete the object so that the
			// undo/redo stack will be able to create/delete stuff in the correct order
			if (!forDeletion)
				m_uowService.RegisterObjectAsModified(MainObject, m_flid, originalValue, ToGuidArray());

			// Must do actual deletion after everything else; only does stuff in owning props.
			foreach (var goner in goners)
				DeleteObject(goner);
		}

		/// <summary>
		/// Determines whether the Set contains the given <paramref name="obj"/>.
		/// </summary>
		/// <param name="obj">The object to locate in the Set.</param>
		/// <returns><c>true</c> if <paramref name="obj"/> is found in the Set; otherwise <c>false</c>.</returns>
		public bool Contains(T obj)
		{
			lock (SyncRoot)
			{
				// Review Damien (JohnT): why does this need to be locked?
				return m_items.Contains(obj);
			}
		}

		/// <summary>
		/// Copies the elements of the Set to an Array, starting at a particular Array index.
		/// </summary>
		/// <param name="array">
		/// The one-dimensional Array that is the destination of the elements copied from the Set.
		/// The Array must have zero-based indexing.
		/// </param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="array"/> is a null reference.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="arrayIndex"/> is less than zero.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// <paramref name="array"/> is multidimensional, or
		/// <paramref name="arrayIndex"/> is equal to or greater than the length of <paramref name="array"/>, or
		/// The number of elements in the Set is greater than the available space from <paramref name="arrayIndex"/>
		/// to the end of the destination <paramref name="array"/>, or
		/// Type T cannot be cast automatically to the type of the destination <paramref name="array"/>.
		/// </exception>
		public void CopyTo(T[] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException();
			if (arrayIndex < 0)
				throw new ArgumentOutOfRangeException();
			// TODO: Check for multidimensional 'array' and throw ArgumentException, if it is.
			lock (SyncRoot)
			{
				//if (arrayIndex >= array.Length || m_items.Count - arrayIndex >= array.Length)
				// equals sign causes spurious ArgumentException when copying entire array
				if (array.Length == 0)
					return;
				if (arrayIndex >= array.Length || m_items.Count - arrayIndex > array.Length)
					throw new ArgumentException();

				int currentIndex = 0;
				int currentcopiedIndex = 0;
				foreach (var objOrId in m_items.ToArray())
				{
					if (currentIndex++ < arrayIndex) continue;
					array.SetValue(FluffUpObjectIfNeeded(objOrId), currentcopiedIndex++);
				}
			}
		}

		/// <summary>
		/// Removes <paramref name="obj"/> from the Set.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns>
		/// <c>true</c> if <paramref name="obj"/> was removed from the Set; otherwise <c>false</c>.
		/// This method returns <c>false</c> if <paramref name="obj"/> was not found in the Set.
		/// </returns>
		public virtual bool Remove(T obj)
		{
			var guidDel = obj.Guid;
			var originalValue = ToGuidArray();

			var retval = m_items.Remove(obj);

			Guid[] newValue;
			if (retval)
			{
				// Copying the array is much faster than calling ToGuidArray() a second time.
				newValue = new Guid[originalValue.Length - 1];
				var i = 0;
				for (; originalValue[i] != guidDel && i < newValue.Length; ++i)
					newValue[i] = originalValue[i];
				for (; i < newValue.Length; ++i)
					newValue[i] = originalValue[i + 1];
			}
			else
			{
				newValue = originalValue;
			}

			// Make sure we update the list before we delete the object so that the
			// undo/redo stack will be able to create/delete stuff in the correct order
			m_uowService.RegisterObjectAsModified(MainObject, m_flid, originalValue, newValue);

			RemoveRefsTo((ICmObjectInternal)obj); // before side effects, so backrefs know it is gone.
			MainObject.RemoveObjectSideEffects(new RemoveObjectEventArgs(obj, Flid, -1));
			DeleteObject((ICmObjectInternal)obj); // after side effects, which may want a useable object.

			return retval;
		}

		#endregion ICollection<T> implementation

		#region IEnumerable<T> implementation

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public IEnumerator<T> GetEnumerator()
		{
			// Fluff them up first, or m_items might change during the loop, which will throw.
			IEnumerable<ICmObjectOrId> items;
			lock (SyncRoot)
			{
				foreach (var objOrId in m_items.ToArray())
					FluffUpObjectIfNeeded(objOrId);
				items = m_items.ToArray();
			}
			foreach (var obj in items)
				yield return (T) obj;
		}

		#endregion IEnumerable<T> implementation

		#region IEnumerable implementation

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion IEnumerable implementation

		#region Object Overrides

		/// <summary>
		/// Get a hashcode based on combining the hashcodes of the
		/// main object and the owning flid.
		/// </summary>
		public override int GetHashCode()
		{
			return m_mainObject.GetHashCode() + m_flid.GetHashCode();
		}

		/// <summary>
		/// Override to show a more sensible string for this class.
		/// </summary>
		public override string ToString()
		{
			return "Set for: " + m_mainObject + " " + m_flid;
		}

		#endregion Object Overrides

		#region Implementation of IFdoVector

		/// <summary>
		/// Get an array of all the hvos.
		/// </summary>
		/// <returns></returns>
		public int[] ToHvoArray()
		{
			lock (SyncRoot)
			{
				return (from idOrObj in m_items.ToArray()
						select FluffUpObjectIfNeeded(idOrObj).Hvo).ToArray();
			}
		}

		/// <summary>
		/// Get the objects in the collection.
		/// </summary>
		public IEnumerable<ICmObject> Objects
		{
			get {
				lock (SyncRoot)
				{
					return (from idOrObj in m_items.ToArray()
							select FluffUpObjectIfNeeded(idOrObj)).ToArray();
				}
			}
		}
		/// <summary>
		/// Get an array of all the Guids.
		/// </summary>
		/// <returns></returns>
		public Guid[] ToGuidArray()
		{
			lock (SyncRoot)
				return (from objOrId in m_items select objOrId.Id.Guid).ToArray();
		}
		#endregion
	}

	/// <summary>
	/// This class supports owning collections.
	/// </summary>
	/// <remarks>
	/// Ownereship, by it nature implies a 'Set'.
	/// This class does not support indexing, as collections are not ordered.
	/// </remarks>
	/// <typeparam name="T">Some kind of CmObject class.</typeparam>
	internal class FdoOwningCollection<T> : FdoSet<T>, IFdoOwningCollection<T>, IFdoOwningCollectionInternal<T>
		where T : class, ICmObject
	{
		#region Construction and Initializing

		/// <summary>
		/// Construct an object which allows access to an owning collection attribute.
		/// </summary>
		internal FdoOwningCollection(IUnitOfWorkService uowService, IRepository<T> repository, ICmObject mainObject, int flid)
			: base(uowService, repository, mainObject, flid)
		{ /* Nothing else to do here. */ }

		/// <summary>
		/// Override the method, so we can make sure it has been initialized,
		/// if it is newly created.
		/// </summary>
		/// <param name="obj"></param>
		protected override void BasicValidityCheck(T obj)
		{
			base.BasicValidityCheck(obj);

			if (obj.Hvo != (int) SpecialHVOValues.kHvoUninitializedObject) return;

			((ICmObjectInternal)obj).InitializeNewCmObject(MainObject.Cache, MainObject, Flid, 0);
		}

		#endregion Construction and Initializing

		/// <summary>
		/// Remove an object without tellikng anyone.
		/// This is done when an object is shifting ownership,
		/// rather than being removed and deleted.
		/// </summary>j
		/// <param name="removee"></param>
		void IFdoOwningCollectionInternal<T>.RemoveOwnee(T removee)
		{
			MainObject.RemoveObjectSideEffects(new RemoveObjectEventArgs(removee, Flid, -1, true));
			m_items.Remove(removee);
		}
		public override void RestoreAfterUndo()
		{
			foreach (var item in m_items)
			{
				ICmObjectInternal obj = item as ICmObjectInternal;
				if (obj != null)
					obj.SetOwnerForUndoRedo(MainObject, Flid, -1);
			}
		}
		/// <summary>
		/// Delete it.
		/// </summary>
		/// <param name="goner"></param>
		protected override void DeleteObject(ICmObjectInternal goner)
		{
			goner.DeleteObject();
		}

		/// <summary>
		/// Add an item to the Set.
		/// </summary>
		/// <param name="obj">The item to add.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="obj"/> is a null reference.
		/// </exception>
		/// <exception cref="FDOObjectDeletedException">
		/// <paramref name="obj"/> has been deleted.
		/// </exception>
		public override void Add(T obj)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");
			if (obj.Hvo > 0 && Contains(obj))
				return; // Nothing to do, so bail out.

			// Will make sure the hvo is good, even if new.
			// base call handles modified registration of MainObject.
			base.Add(obj);
			((ICmObjectInternal)obj).SetOwner(MainObject, Flid, -1);
		}
	}

	/// <summary>
	/// Class for holding reference collections, which is a an unordered Set.
	/// </summary>
	/// <typeparam name="T">Some kind of CmObject class.</typeparam>
	internal class FdoReferenceCollection<T> : FdoSet<T>, IFdoReferenceCollection<T>, IReferenceSource, IVector
		where T : class, ICmObject
	{
		#region Construction and Initializing

		/// <summary>
		/// Construct an object which allows access to a vector attribute
		/// </summary>
		internal FdoReferenceCollection(IUnitOfWorkService uowService, IRepository<T> repository, ICmObject mainObject, int flid)
			: base(uowService, repository, mainObject, flid)
		{ /* Nothing else to do here. */ }

		/// <summary>
		/// Override to make sure the item is valid.
		/// </summary>
		/// <param name="obj"></param>
		protected override void BasicValidityCheck(T obj)
		{
			base.BasicValidityCheck(obj);

			// Do this after the base call, since 'obj' may be null,
			// and we don't want to return a NllReferenceException
			// by asking for its Hvo.
			if (obj.Hvo == (int)SpecialHVOValues.kHvoUninitializedObject)
				throw new FDOObjectUninitializedException("Object has not been initialized.");

		}

		#endregion Construction and Initializing

		/// <summary>
		/// See if vector is for owning properties (true), or refenence properties (false).
		/// </summary>
		protected override bool IsOwningVector
		{
			get { return false; }
		}

		/// <summary>
		/// Undoing a change to a reference collection, we must clear incoming refs
		/// on the old items.
		/// </summary>
		public override void ClearForUndo()
		{
			foreach (var item in m_items)
			{
				if (item is ICmObjectInternal) // otherwise hasn't been counted
					((ICmObjectInternal)item).RemoveIncomingRef(this);
			}
		}

		 /// <summary>
		/// Undoing a change to a reference collection, we must restore incoming refs
		/// on the restored items. Also makes sure they are real references.
		/// </summary>
		public override void RestoreAfterUndo()
		{
			// First we must make sure all the objects are real. This is very like a loop over
			// FluffUpObjectIfNeeded; but do NOT use that, because it will add extra incoming refs
			// for anything that isn't already fluffed.
			var repo = MainObject.Cache.ServiceLocator.ObjectRepository;
			lock (SyncRoot)
			{
				foreach (var objOrId in m_items.ToArray())
				{
					if (objOrId is T)
						continue;
					var result = (T) objOrId.GetObject(repo);
					// Replace with fluffed up one for next time; we must do the remove or nothing changes, since
					// the set thinks the object is already present.
					m_items.Remove(objOrId);
					m_items.Add(result);
				}
			}

			 foreach (var item in m_items)
				((ICmObjectInternal)item).AddIncomingRef(this);
		}

		/// <summary>
		/// Answer true if the two collections are equivalent.
		/// We do not allow for duplicates; if the sizes are the same and
		/// every element in one is in the other, we consider them equivalent.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool IsEquivalent(IFdoReferenceCollection<T> other)
		{
			if (Count != other.Count)
				return false;

			var otherList = new HashSet<ICmObject>(other.Objects);
			foreach (var obj in Objects)
			{
				if (!otherList.Contains(obj))
					return false;
				otherList.Remove(obj);	// ensure unique matches
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the elements of the Set to another set.
		/// </summary>
		/// <param name="dest"></param>
		/// ------------------------------------------------------------------------------------
		public void AddTo(IFdoSet<T> dest)
		{
			if (dest == null)
				throw new ArgumentNullException("dest");

			foreach (var objOrId in m_items.ToArray())
				dest.Add(FluffUpObjectIfNeeded(objOrId));
		}

		#region IReferenceSource Members

		void IReferenceSource.RemoveAReference(ICmObject target)
		{
			Remove((T)target);
		}

		void IReferenceSource.ReplaceAReference(ICmObject target, ICmObject replacement)
		{
			Remove((T)target);
			Add((T)replacement);
		}
		ICmObject IReferenceSource.Source
		{
			get { return MainObject; }
		}

		/// <summary>
		/// a collection which refers to the target at all only needs to check the flid.
		/// </summary>
		bool IReferenceSource.RefersTo(ICmObject target, int flid)
		{
			return flid == Flid;
		}

		#endregion

		internal override void AddObject(AddObjectEventArgs args)
		{
			// This should be done BEFORE base.AddObject, because the caller has already added the object to our value,
			// so the referring object should consistently know that this refers to it. This allows virtual properties
			// in the referred-to object to compute their backrefs properly.
			((ICmObjectInternal)args.ObjectAdded).AddIncomingRef(this);
			base.AddObject(args);
		}

		internal override void FluffUpSideEffects(object thingJustFluffed)
		{
			base.FluffUpSideEffects(thingJustFluffed);
			((ICmObjectInternal)thingJustFluffed).AddIncomingRef(this);
		}

		protected override void RemoveRefsTo(ICmObjectInternal goner)
		{
			base.RemoveRefsTo(goner);
			goner.RemoveIncomingRef(this);
		}
	}

	/// <summary>
	/// A base class used to support indexed, and thus, sequenced, data.
	/// </summary>
	/// <typeparam name="T">Some kind of CmObject class.</typeparam>
	internal abstract class FdoList<T> : IFdoList<T>, IFdoVectorInternal, IFdoClearForDelete
		where T : class, ICmObject
	{
		#region Data Members

		/// <summary>The item's guids *in order*.</summary>
		protected readonly List<ICmObjectOrId> m_items = new List<ICmObjectOrId>();
		protected readonly IUnitOfWorkService m_uowService;
		private readonly IRepository<T> m_repository;

		/// <summary>The object that has the sequence property in 'm_flid'.</summary>
		private readonly ICmObjectInternal m_mainObject;

		/// <summary>The field ID of the sequence property.</summary>
		private readonly int m_flid;

		#endregion Data Members

		#region Properties

		/// <summary>
		/// Gets the object that owns the sequence property.
		/// </summary>
		internal ICmObjectInternal MainObject
		{
			get { return m_mainObject; }
		}

		/// <summary>
		/// Get the field ID for the vector.
		/// </summary>
		internal int Flid
		{
			get { return m_flid; }
		}

		/// <summary>
		/// Gets the synchronization root. This is the object that should be
		/// used for all locking in this list. Used for m_items, since it
		/// can change during FDO reads
		/// </summary>
		/// <value>The synchronization root.</value>
		protected object SyncRoot
		{
			get
			{
				return this;
			}
		}

		#endregion Properties

		#region Construction and Initializing

		/// <summary>
		/// Construct an object which allows access to a collection attribute
		/// </summary>
		internal FdoList(IUnitOfWorkService uowService, IRepository<T> repository, ICmObject mainObject, int flid)
		{
			// JohnT: removed some validation from here; this method is called only by generated code, the risk seems
			// very low, and the validation we were doing took significant time.
			m_uowService = uowService;
			m_repository = repository;
			m_mainObject = (ICmObjectInternal)mainObject;
			m_flid = flid;
		}

		/// <summary>
		/// Make sure the object being added to the vector is
		/// minimally valid. It can't be null or deleted.
		/// <para>
		/// Should be overridden if subclasses want to add more checks,
		/// or if they want do something with a brand new object, like initiaslize it.
		/// </para>
		/// </summary>
		/// <param name="obj">Object to check.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="obj"/> is a null reference.
		/// </exception>
		/// <exception cref="FDOObjectDeletedException">
		/// <paramref name="obj"/> has been deleted.
		/// </exception>
		protected virtual void BasicValidityCheck(T obj)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");
			if (obj.Hvo == (int)SpecialHVOValues.kHvoObjectDeleted)
				throw new FDOObjectDeletedException("Object has been deleted.");
		}

		private void CheckIndex(int index)
		{
			if (index < 0 || index >= m_items.Count)
				throw new ArgumentOutOfRangeException();
		}

		/// <summary>Get an XML string that represents the entire instance.</summary>
		/// <param name='writer'>The writer in which the XML is placed.</param>
		/// <remarks>Only to be used by backend provider system.</remarks>
		void IFdoVectorInternal.ToXMLString(XmlWriter writer)
		{
			lock (SyncRoot)
			{
				foreach (var key in m_items)
					ReadWriteServices.WriteObjectReference(writer,
														   key.Id.Guid,
														   IsOwningVector ? ObjectPropertyType.Owning : ObjectPropertyType.Reference);
			}
		}

		/// <summary>
		/// See if vector is for owning properties (true), or refenence properties (false).
		/// </summary>
		protected virtual bool IsOwningVector
		{
			get { return true; }
		}

		/// <summary>
		/// Load/Reconstitute data using XElement.
		/// </summary>
		void IFdoVectorInternal.LoadFromDataStoreInternal(XElement reader, ICmObjectIdFactory factory)
		{
			if (reader == null) throw new ArgumentNullException("reader");
			if (factory == null) throw new ArgumentNullException("factory");
			foreach (var objsurElement in reader.Elements("objsur"))
				m_items.Add(ReadWriteServices.LoadAtomicObjectProperty(objsurElement, factory));
		}

		#endregion Construction and Initializing

		#region Other methods

		protected T FluffUpObjectIfNeeded(int index)
		{
			var item = m_items[index];
			var result = item as T;
			if (result != null)
				return result;
			//result = (T) item.GetObject(MainObject.Services.GetInstance<ICmObjectRepository>());
			result = (T)item.GetObject(m_mainObject.Cache.ServiceLocator.ObjectRepository);
			FluffUpSideEffects(result);
			m_items[index] = result; // faster next time, and prevents side effects being repeated.
			return result;
		}

		internal virtual void FluffUpSideEffects(object thingJustFluffed)
		{
			// Do nothing...see ReferenceSequence
		}

		/// <summary>
		/// This is overridden in owning collections to delete the object.
		/// It should be called by methods that remove objects, AFTER RemoveObjectSideEffects.
		/// </summary>
		protected virtual void DeleteObject(ICmObjectInternal goner)
		{ /* Do nothing. */ }

		/// <summary>
		/// This is overridden in reference collections to remove incoming references to the object.
		/// It should be called by methods that remove objects, BEFORE RemoveObjectSideEffects.
		/// </summary>
		protected virtual void RemoveRefsTo(ICmObjectInternal goner)
		{ /* Do nothing. */ }

		ICmObjectIdFactory ObjectIdFactory
		{
			get { return MainObject.Services.GetInstance<ICmObjectIdFactory>(); }
		}
		/// <summary>
		/// Replace the entire contents of the property with 'newValue'.
		/// </summary>
		void IFdoVectorInternal.ReplaceAll(Guid[] newValue, bool useAccessor)
		{
			if (useAccessor)
			{
				Clear();
				foreach (var guid in newValue)
					Add(m_repository.GetObject(guid));
			}
			else
			{
				ClearForUndo();
				m_items.Clear();

				if (newValue.Length > 0)
				{
					// There is a pathological case where we are 'restoring' a cleared value
					// on a deleted object and can't obtain the id factory, so we must not
					// do so if we don't need it because there are no values to restore.
					var factory = ObjectIdFactory;
					foreach (var guid in newValue)
					{
						m_items.Add(m_repository.GetObject(guid));
					}
				}
				RestoreAfterUndo();
			}
		}

		public virtual void RestoreAfterUndo()
		{
		}

		public virtual void ClearForUndo()
		{
		}

		/// <summary>
		/// Get an array of all CmObjects.
		/// </summary>
		/// <returns></returns>
		public T[] ToArray()
		{
			lock (SyncRoot)
			{
				var result = new T[m_items.Count];
				for (int i = 0; i < m_items.Count(); i++)
					result[i] = FluffUpObjectIfNeeded(i);
				return result;
			}
		}
		/// <summary>
		/// Get the objects in the collection.
		/// </summary>
		public IEnumerable<ICmObject> Objects
		{
			get
			{
				lock (SyncRoot)
				{
					var result = new ICmObject[m_items.Count];
					for (int i = 0; i < m_items.Count(); i++)
						result[i] = FluffUpObjectIfNeeded(i);
					return result;
				}
			}
		}

		/// <summary>
		/// Replace the indicated number of objects (possibly zero) starting at the indicated position
		/// with the new objects (possibly empty).
		/// In the case of owning properties, the deleted objects are really deleted; this code does
		/// not handle the possibility that some of them are included in thingsToAdd.
		/// The code will handle both newly created objects and ones being moved from elsewhere
		/// (including another location in the same sequence).
		/// </summary>
		public virtual void Replace(int start, int numberToDelete, IEnumerable<ICmObject> thingsToAdd)
		{
			if(start + numberToDelete > Count) {
				throw new IndexOutOfRangeException("You can not replace past the end of the vector.");
			}
			//store the order before we begin the replace
			var originalOrder = ToGuidArray();
			//Make sure we update the list before we do any modifications, some operations in the replace do not
			//trigger this on their own, this call sets the original order up front.
			m_uowService.RegisterObjectAsModified(MainObject, m_flid, originalOrder, ToGuidArray());

			int subsetLength = numberToDelete;
			var deletedSubset = this.Where((obj, index) => index >= start && index < start + subsetLength);

			if (deletedSubset.Count() > 0)
			{
				List<T> removedAndReAdded;
				List<T> removed;
				List<T> newAdditions;
				BuildCollections(thingsToAdd.Cast<T>().ToList(), deletedSubset, out removedAndReAdded, out removed, out newAdditions);
				Debug.Assert(removedAndReAdded.Count + removed.Count() == numberToDelete, "Logic for handling deletes is incomplete, we won't be attempting enough removes.");
				Debug.Assert(removedAndReAdded.Count + newAdditions.Count == thingsToAdd.Count());
				//iterator for the things we're adding
				var itr = thingsToAdd.GetEnumerator();
				//traverse the removed section and the add section simlutaneously
				int index = start;
				bool hasNext = itr.MoveNext(); //get the iterator to the first item.
				while(hasNext || numberToDelete > 0)
				{
					if (index < Count)
					{
						//check the current index for removal
						if (removed.Contains(this[index]))
						{
							removed.Remove(this[index]);
							RemoveAt(index);
							--numberToDelete;
							continue; //we didn't use the iterator, don't advance it
						}
						if (removedAndReAdded.Contains(this[index]))
						{
							removedAndReAdded.Remove(this[index]);
							if (!newAdditions.Contains((T)itr.Current))//the item we are adding is not new
							{
								m_items[index] = (T) itr.Current; //avoid all side effects on a move of an existing item.
							}
							else//the item we are adding is new
							{

								m_items.RemoveAt(index);//remove without side effects
								Insert(index, (T)itr.Current);//add with side effects
								newAdditions.Remove((T) itr.Current);
							}
							++index;
							--numberToDelete;
							hasNext = itr.MoveNext(); //we replaced the current index, this is a removal, and a use of the iterator
							continue;
						}
					}
					if (hasNext) //we have more items to add
					{
						if (removedAndReAdded.Contains((T)itr.Current)) //if the item was just moved, insert without side affects
						{
							removedAndReAdded.Remove((T)itr.Current);
							m_items.Insert(index, itr.Current);
						}
						else //if the item is new use the insert that fires events
						{
							Insert(index, (T)itr.Current);
							newAdditions.Remove((T) itr.Current);
						}
						++index;
						hasNext = itr.MoveNext();
					}
				}
			}
			else //no items removed (possible location for multiple insert optimizations)
			{
				// Do the inserts
				var loc = start + numberToDelete;
				foreach (var obj in thingsToAdd)
				{
					Insert(loc, (T)obj);
					loc++;
				}
			}
			//update the registered undo information with the final order after our replace.
			m_uowService.RegisterObjectAsModified(MainObject, m_flid, originalOrder, ToGuidArray());
		}

		/// <summary>
		/// This method populates the collections necessary for doing the replace
		/// </summary>
		/// <param name="thingsToAdd">items being added</param>
		/// <param name="deletedSubset">items being deleted</param>
		/// <param name="removedAndReAdded">will be filled with items being replaced or empty list</param>
		/// <param name="removed">will be filled in with items being removed or empty list</param>
		/// <param name="newAdditions">will be filled with items being inserted or empty list</param>
		private static void BuildCollections(List<T> thingsToAdd, IEnumerable<T> deletedSubset, out List<T> removedAndReAdded, out List<T> removed, out List<T> newAdditions)
		{
			removed = new List<T>();
			removedAndReAdded = new List<T>();
			newAdditions = new List<T>();
			var additions = new SortedDictionary<Guid, List<T>>();
			var removals = new SortedDictionary<Guid, List<T>>();
			foreach(var add in thingsToAdd)
			{
				if(!additions.ContainsKey(add.Guid))
				{
					additions.Add(add.Guid, new List<T>());
				}
				additions[add.Guid].Add(add);
			}
			foreach(var deleted in deletedSubset)
			{
				if (!removals.ContainsKey(deleted.Guid))
				{
					removals.Add(deleted.Guid, new List<T>());
				}
				removals[deleted.Guid].Add(deleted);
			}
			//simultaneously iterate the addition and removal dictionaries
			var additionItr = additions.GetEnumerator();
			var removalsItr = removals.GetEnumerator();

			bool moreAdds = additionItr.MoveNext();
			bool moreDeletes = removalsItr.MoveNext();
			while(moreAdds || moreDeletes)
			{
				//if there are no more adds, or the remove key is less then the add key these items are not in the adds, remove them
				if (!moreAdds || (moreDeletes && removalsItr.Current.Key.CompareTo(additionItr.Current.Key) < 0))
				{
					//do removes with all the removals
					removed.AddRange(removalsItr.Current.Value);
					moreDeletes = removalsItr.MoveNext();
					continue;
				}
				//if the remove key is greater then these items are not in the removes, add them.
				else if (!moreDeletes || (moreAdds && removalsItr.Current.Key.CompareTo(additionItr.Current.Key) > 0))
				{
					newAdditions.AddRange(additionItr.Current.Value);
					moreAdds = additionItr.MoveNext();
					continue;
				}
				//the keys are the same, some replacements
				//if there are the same number in each list they are all replacements.
				if (removalsItr.Current.Value.Count == additionItr.Current.Value.Count)
				{
					removedAndReAdded.AddRange(removalsItr.Current.Value);
				}
				else
				{
					List<T> removeList = removalsItr.Current.Value;
					List<T> addList = additionItr.Current.Value;
					int theReplacements = Math.Min(removeList.Count, addList.Count);
					removedAndReAdded.AddRange(removeList.GetRange(0, theReplacements));
					removeList.RemoveRange(0, theReplacements);
					addList.RemoveRange(0, theReplacements);
					newAdditions.AddRange(addList);
					removed.AddRange(removeList);
				}
				moreDeletes = removalsItr.MoveNext();
				moreAdds = additionItr.MoveNext();
			}
		}

		#endregion Other methods

		#region IList<T> implementation

		/// <summary>
		/// Get or set the element at the specified <paramref name="index"/>.
		/// </summary>
		/// <param name="index">The zero-based index of the element to get or set.</param>
		/// <returns>The object at the specified <paramref name="index"/>.</returns>
		public virtual T this[int index]
		{
			get
			{
				lock (SyncRoot)
				{
					CheckIndex(index);
					return FluffUpObjectIfNeeded(index);
				}
			}
			set
			{
				BasicValidityCheck(value);

				var originalValue = ToGuidArray();
				var original = FluffUpObjectIfNeeded(index);

				AddObjectEventArgs eventArgs = new AddObjectEventArgs(value, Flid, index);
				MainObject.ValidateAddObject(eventArgs);
				// No need to call CheckIndex, since the next line will do it for us.
				m_items[index] = value;

				MainObject.RemoveObjectSideEffects(new RemoveObjectEventArgs(original, Flid, index));

				// Make sure we update the list before we delete the object so that the
				// undo/redo stack will be able to create/delete stuff in the correct order
				m_uowService.RegisterObjectAsModified(MainObject, m_flid, originalValue, ToGuidArray());

				RemoveRefsTo((ICmObjectInternal)original);
				DeleteObject((ICmObjectInternal)original);
				AddObject(new AddObjectEventArgs(value, Flid, index));
			}
		}

		/// <summary>
		/// Determines the index of <paramref name="obj"/> in the list.
		/// </summary>
		/// <param name="obj">The object to locate in the list.</param>
		/// <returns>The index of <paramref name="obj"/> if found in the list; otherwise -1.</returns>
		/// <remarks>
		/// If <paramref name="obj"/> occurs multiple times in the list,
		/// the <c>IndexOf</c> method always returns the index of the first instance found.
		/// </remarks>
		public int IndexOf(T obj)
		{
			lock (SyncRoot)
			{
				// No need to fluff items up!
				for (int i = 0; i < m_items.Count; i++)
					if (m_items[i].Id == obj.Id)
						return i;
			}
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts an item into the list at the specified <paramref name="index"/>.
		/// </summary>
		/// <param name="index">The zero-based index at which <paramref name="obj"/> is to be inserted.</param>
		/// <param name="obj">The object to insert into the list.</param>
		/// <remarks>
		/// If <paramref name="index"/> equals the number of items in the list,
		/// then <paramref name="obj"/> is appended to the list.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public abstract void Insert(int index, T obj);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts an item into the list at the position specified in
		/// <paramref name="eventArgs"/>.Index.
		/// </summary>
		/// <param name="eventArgs">The <see cref="T:SIL.FieldWorks.FDO.Infrastructure.AddObjectEventArgs"/>
		/// instance containing the event data.</param>
		/// <remarks>
		/// If <paramref name="eventArgs"/>.Index equals the number of items in the list,
		/// then <paramref name="eventArgs"/>.ObjectAdded is appended to the list.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected void Insert(AddObjectEventArgs eventArgs)
		{
			var originalValue = ToGuidArray();

			// No need to call CheckIndex, since the next line will do it for us.
			m_items.Insert(eventArgs.Index, eventArgs.ObjectAdded);

			AddObject(eventArgs);
			m_uowService.RegisterObjectAsModified(MainObject, m_flid, originalValue, ToGuidArray());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the object at the specified <paramref name="index"/>.
		/// </summary>
		/// <param name="index">The zero-based index of the object to remove.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void RemoveAt(int index)
		{
			var originalValue = ToGuidArray();

			// No need to call CheckIndex, since the next line will do it for us.
			var goner = FluffUpObjectIfNeeded(index);
			m_items.RemoveAt(index);
			RemoveRefsTo((ICmObjectInternal)goner); // Before side effects (e.g., target needs to know it is no longer referring).
			MainObject.RemoveObjectSideEffects(new RemoveObjectEventArgs(goner, Flid, index));
			// Make sure we update the list before we delete the object so that the
			// undo/redo stack will be able to create/delete stuff in the correct order
			m_uowService.RegisterObjectAsModified(MainObject, m_flid, originalValue, ToGuidArray());
			DeleteObject((ICmObjectInternal)goner); // After side effects (e.g., side effects may want a valid object).
		}

		#endregion IList<T> implementation

		#region ICollection<T> implementation

		/// <summary>
		/// Get the number of CmObjects in the list.
		/// </summary>
		public int Count
		{
			get
			{
				lock (SyncRoot)
					return m_items.Count;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the list is readonly.
		/// This is always <c>false</c>
		/// </summary>
		public bool IsReadOnly
		{
			get { return false; }
		}

		/// <summary>
		/// Add an item to the end of the list.
		/// </summary>
		/// <param name="obj">The item to add.</param>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="obj"/> is a null reference.
		/// </exception>
		/// <exception cref="FDOObjectDeletedException">
		/// <paramref name="obj"/> has been deleted.
		/// </exception>
		public virtual void Add(T obj)
		{
			AddObjectEventArgs eventArgs = new AddObjectEventArgs(obj, Flid, m_items.Count);
			MainObject.ValidateAddObject(eventArgs);

			BasicValidityCheck(obj);
			var originalValue = ToGuidArray();

			m_items.Add(obj);

			AddObject(eventArgs);
			m_uowService.RegisterObjectAsModified(MainObject, m_flid, originalValue, ToGuidArray());
		}

		internal virtual void AddObject(AddObjectEventArgs args)
		{
			MainObject.AddObjectSideEffects(args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes all items from the list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Clear()
		{
			((IFdoClearForDelete)this).Clear(false);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes all items from the list. Only DeleteObjectBasics overrides should pass forDeletion true,
		/// which bypasses registering the object as modified (and maybe eventually some validity checks).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void IFdoClearForDelete.Clear(bool forDeletion)
		{
			if (m_items.Count == 0)
				return; // already cleared, don't waste time or (especially) register as modified.
			var originalValue = ToGuidArray();

			var goners = new List<ICmObjectInternal>();
			while (m_items.Count > 0)
			{
				var goner = (ICmObjectInternal)FluffUpObjectIfNeeded(0);
				m_items.RemoveAt(0);
				goners.Add(goner);
				RemoveRefsTo(goner); // before side effects, may want consistent backrefs
				MainObject.RemoveObjectSideEffects(new RemoveObjectEventArgs(goner, Flid, 0));
			}

			// Make sure we update the list before we delete the object so that the
			// undo/redo stack will be able to create/delete stuff in the correct order
			if (!forDeletion)
				m_uowService.RegisterObjectAsModified(MainObject, m_flid, originalValue, ToGuidArray());

			foreach (var goner in goners)
				DeleteObject(goner); // after side effects, which may want valid object.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the list contains the given <paramref name="obj"/>.
		/// </summary>
		/// <param name="obj">The object to locate in the Set.</param>
		/// <returns>
		/// 	<c>true</c> if <paramref name="obj"/> is found in the Set; otherwise <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool Contains(T obj)
		{
			return IndexOf(obj) >= 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the elements of the list to an Array, starting at a particular Array index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination of the elements copied from the list.
		/// The Array must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins</param>
		/// <exception cref="ArgumentNullException">
		/// 	<paramref name="array"/> is a null reference.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// 	<paramref name="arrayIndex"/> is less than zero.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// 	<paramref name="array"/> is multidimensional, or
		/// <paramref name="arrayIndex"/> is equal to or greater than the length of <paramref name="array"/>, or
		/// The number of elements in the Set is greater than the available space from <paramref name="arrayIndex"/>
		/// to the end of the destination <paramref name="array"/>, or
		/// Type T cannot be cast automatically to the type of the destination <paramref name="array"/>.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public void CopyTo(T[] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException();
			if (arrayIndex < 0)
				throw new ArgumentOutOfRangeException();
			// TODO: Check for multidimensional 'array' and throw ArgumentException, if it is.
			lock (SyncRoot)
			{
				if (m_items.Count + arrayIndex > array.Length)
					throw new ArgumentOutOfRangeException("arrayIndex");

				int currentcopiedIndex = arrayIndex;
				for (int i = 0; i < m_items.Count(); i++)
					array[currentcopiedIndex++] = FluffUpObjectIfNeeded(i);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the first instance of <paramref name="obj"/> from the list.
		/// </summary>
		/// <param name="obj">Obejct to remove.</param>
		/// <returns>
		/// 	<c>true</c> if <paramref name="obj"/> was removed from the list; otherwise <c>false</c>.
		/// This method returns <c>false</c> if <paramref name="obj"/> was not found in the list.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool Remove(T obj)
		{
			var index = IndexOf(obj);
			if (index < 0)
				return false;

			RemoveAt(index);
			return true;
		}

		#endregion ICollection<T> implementation

		#region IEnumerable<T> implementation

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public IEnumerator<T> GetEnumerator()
		{
			List<T> items;
			lock (SyncRoot)
			{
				items = new List<T>(m_items.Count);
				for (int i = 0; i < m_items.Count; i++)
					items.Add(FluffUpObjectIfNeeded(i));
			}
			foreach (T item in items)
				yield return item;
			// Made a list and cleared to fix memory leaks: arrays aren't cleaned up properly
			// for some reason. Possibly because of the combination of array and yield, but
			// we really aren't sure. (FWR-240)
			items.Clear();
		}

		#endregion IEnumerable<T> implementation

		#region IEnumerable implementation

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion IEnumerable implementation

		#region Object Overrides

		/// <summary>
		/// Get a hashcode based on combining the hashcodes of the
		/// main object and the owning flid.
		/// </summary>
		public override int GetHashCode()
		{
			return m_mainObject.GetHashCode() + m_flid.GetHashCode();
		}

		/// <summary>
		/// Override to show a more sensible string for this class.
		/// </summary>
		public override string ToString()
		{
			return "Sequence for: " + m_mainObject + " " + m_flid;
		}

		#endregion Object Overrides

		#region Implementation of IFdoVector

		/// <summary>
		/// Get an array of all the hvos.
		/// </summary>
		/// <returns></returns>
		public int[] ToHvoArray()
		{
			lock (SyncRoot)
			{
				var result = new int[m_items.Count];
				for (int i = 0; i < m_items.Count(); i++)
					result[i] = FluffUpObjectIfNeeded(i).Hvo;
				return result;
			}
		}

		/// <summary>
		/// Get an array of all the Guids.
		/// </summary>
		/// <returns></returns>
		public Guid[] ToGuidArray()
		{
			lock (SyncRoot)
				return (from idOrObj in m_items select idOrObj.Id.Guid).ToArray();
		}
		#endregion
	}

	/// <summary>
	/// This class provides support for reference sequence properties.
	/// </summary>
	/// <typeparam name="T">Some kind of CmObject class.</typeparam>
	internal class FdoReferenceSequence<T> : FdoList<T>, IFdoList<T>, IFdoReferenceSequence<T>, IReferenceSource, IVector
		where T : class, ICmObject
	{
		#region Construction and Initializing

		/// <summary>
		/// Construct an object which allows access to a reference sequence property.
		/// </summary>
		internal FdoReferenceSequence(IUnitOfWorkService uowService, IRepository<T> repository, ICmObject mainObject, int flid)
			: base(uowService, repository, mainObject, flid)
		{ /* Nothing else to do here. */ }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override the method, so we can make sure
		/// it has been initialized.
		/// We can't put newly created objects in a reference property.
		/// They have to first have owners or be legally ownerless.
		/// </summary>
		/// <param name="obj">The object to check</param>
		/// <exception cref="FDOObjectUninitializedException">Object has not been initialized.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		protected override void BasicValidityCheck(T obj)
		{
			base.BasicValidityCheck(obj);

			if (obj.Hvo == (int)SpecialHVOValues.kHvoUninitializedObject)
				throw new FDOObjectUninitializedException("Object has not been initialized.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts an item into the list at the specified <paramref name="index"/>.
		/// </summary>
		/// <param name="index">The zero-based index at which <paramref name="obj"/> is to be inserted.</param>
		/// <param name="obj">The object to insert into the list.</param>
		/// <remarks>
		/// If <paramref name="index"/> equals the number of items in the list,
		/// then <paramref name="obj"/> is appended to the list.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public override void Insert(int index, T obj)
		{
			BasicValidityCheck(obj);
			AddObjectEventArgs eventArgs = new AddObjectEventArgs(obj, Flid, index);
			MainObject.ValidateAddObject(eventArgs);
			Insert(eventArgs);
		}

		protected override void RemoveRefsTo(ICmObjectInternal goner)
		{
			// This should be done BEFORE base.RemoveObject, because the caller has already removed the object from our value,
			// so the referring object should consistently know that this no longer refers to it. This allows virtual properties
			// in the referred-to object to compute their backrefs properly.
			goner.RemoveIncomingRef(this);
			base.RemoveRefsTo(goner);
		}

		internal override void AddObject(AddObjectEventArgs args)
		{
			// This should be done BEFORE base.AddObject, because the caller has already added the object to our value,
			// so the referring object should consistently know that this refers to it. This allows virtual properties
			// in the referred-to object to compute their backrefs properly.
			((ICmObjectInternal)args.ObjectAdded).AddIncomingRef(this);
			base.AddObject(args);
		}

		internal override void FluffUpSideEffects(object thingJustFluffed)
		{
			base.FluffUpSideEffects(thingJustFluffed);
			((ICmObjectInternal)thingJustFluffed).AddIncomingRef(this);
		}

		#endregion Construction and Initializing

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See if vector is for owning properties (true), or refenence properties (false).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool IsOwningVector
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the elements of the list to the specified list, starting at a particular index.
		/// </summary>
		/// <param name="dest">The dest.</param>
		/// <param name="destIndex">Index of the dest.</param>
		/// ------------------------------------------------------------------------------------
		public void CopyTo(IFdoList<T> dest, int destIndex)
		{
			if (dest == null)
				throw new ArgumentNullException("dest");
			if (destIndex < 0 || destIndex > dest.Count)
				throw new ArgumentOutOfRangeException("destIndex");

			int currentcopiedIndex = destIndex;
			lock (SyncRoot)
			{
				for (int i = 0; i < m_items.Count; i++)
					dest.Insert(currentcopiedIndex++, FluffUpObjectIfNeeded(i));
			}
		}

		#region IList<T> overrides

		/// <summary>
		/// Undoing a change to a reference sequence, we must clear incoming refs
		/// on the old items.
		/// </summary>
		public override void ClearForUndo()
		{
			foreach (var item in m_items)
			{
				if(item is ICmObjectInternal) // otherwise hasn't been counted
					((ICmObjectInternal) item).RemoveIncomingRef(this);
			}
		}

		/// <summary>
		/// Undoing a change to a reference sequence, we must restore incoming refs
		/// on the new items.
		/// </summary>
		public override void RestoreAfterUndo()
		{
			// First we must make sure all the objects are real. This is very like a loop over
			// FluffUpObjectIfNeeded; but do NOT use that, because it will add extra incoming refs
			// for anything that isn't already fluffed.
			var repo = MainObject.Cache.ServiceLocator.ObjectRepository;
			lock (SyncRoot)
			{
				for (int i = 0; i < m_items.Count; i++)
				{
					var objOrId = m_items[i];
					if (objOrId is T)
						continue;
					m_items[i] = objOrId.GetObject(repo);
				}
			}
			foreach (var item in m_items)
				((ICmObjectInternal)item).AddIncomingRef(this);
		}

		/// <summary>
		/// Get or set the element at the specified <paramref name="index"/>.
		/// </summary>
		/// <param name="index"> The zero-based index of the element to get or set.</param>
		/// <returns>The object at the specified <paramref name="index"/>.</returns>
		/// <exception cref="NotSupportedException">
		/// Thrown if the new value is null.
		/// </exception>
		public override T this[int index]
		{
			set
			{
				// Client should use 'RemoveAt' for this.
				if (value == null)
					throw new NotSupportedException("Setting value to null at 'index' is not supported. Use RemoveAt instead.");

				base[index] = value;
			}
		}

		#endregion IList<T> overrides

		#region IReferenceSource Members

		void IReferenceSource.RemoveAReference(ICmObject target)
		{
			Remove((T)target);
		}

		/// <summary>
		/// Assume target occurs at least once; replace it with the specified object.
		/// </summary>
		void IReferenceSource.ReplaceAReference(ICmObject target, ICmObject replacement)
		{
			int index = IndexOf((T)target);
			this[index] = (T) replacement;
		}

		ICmObject IReferenceSource.Source
		{
			get { return MainObject; }
		}

		/// <summary>
		/// a sequence which refers to the target at all only needs to check the flid.
		/// </summary>
		bool IReferenceSource.RefersTo(ICmObject target, int flid)
		{
			return flid == Flid;
		}
		#endregion
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This is really a Set, in that the same object can't be put in more than once.
	/// But, we also need the indexing capability,
	/// in case some object wants to be picky about where it goes,
	/// as is appropriate for a sequence property.
	/// </summary>
	/// <typeparam name="T">Some kind of CmObject class.</typeparam>
	/// ----------------------------------------------------------------------------------------
	internal class FdoOwningSequence<T> : FdoList<T>, IFdoList<T>, IFdoOwningSequence<T>, IFdoOwningSequenceInternal<T>
		where T : class, ICmObject
	{
		#region Construction and Initializing

		/// <summary>
		/// Construct an object which allows access to a reference sequence property.
		/// </summary>
		internal FdoOwningSequence(IUnitOfWorkService uowService, IRepository<T> repository, ICmObject mainObject, int flid)
			: base(uowService, repository, mainObject, flid)
		{ /* Nothing else to do here. */ }

		/// <summary>
		/// Override the method, so we can make sure
		/// it has been initialized.
		/// </summary>
		/// <param name="obj"></param>
		protected override void BasicValidityCheck(T obj)
		{
			base.BasicValidityCheck(obj);
			var index = Count;
			RemoveObjectEventArgs removeArgs;
			PrepareToInsert(index, obj, out removeArgs);
		}

		#endregion Construction and Initializing

		/// <summary>
		/// Delete it.
		/// </summary>
		/// <param name="goner">The goner.</param>
		/// ------------------------------------------------------------------------------------
		protected override void DeleteObject(ICmObjectInternal goner)
		{
			goner.DeleteObject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove an object without telling anyone.
		/// This is done when an object is shifting ownership,
		/// rather than being removed and deleted.
		/// </summary>
		/// <param name="removee"></param>
		/// ------------------------------------------------------------------------------------
		void IFdoOwningSequenceInternal<T>.RemoveOwnee(T removee)
		{
			var index = IndexOf(removee);
			// Review: This EventArgs said ForDeletion = true. I'm changing it so my code will work
			MainObject.RemoveObjectSideEffects(new RemoveObjectEventArgs(removee, Flid, index, false));
			m_items.RemoveAt(index);
		}

		/// <summary>
		/// Replace the indicated number of objects (possibly zero) starting at the indicated position
		/// with the new objects (possibly empty).
		/// In the case of owning properties, the deleted objects are really deleted; this code does
		/// not handle the possibility that some of them are included in thingsToAdd.
		/// The code will handle both newly created objects and ones being moved from elsewhere
		/// (including another location in the same sequence).
		/// </summary>
		public override void Replace(int start, int numberToDelete, IEnumerable<ICmObject> thingsToAdd)
		{
			for (var i = start + numberToDelete - 1; i >= start; i--)
				RemoveAt(i);
			var loc = start;
			foreach (var obj in thingsToAdd)
			{
				Insert(loc, (T)obj);
				// it is possible for owning sequences to not insert at the index that is
				// passed in, so we get the index of the newly inserted item using IndexOf
				// ENHANCE: optimize this, by overriding this method in FdoOwningSequence,
				// so we don't have to call IndexOf
				loc = IndexOf((T)obj) + 1;
			}
		}

		#region IList<T> overrides

		/// <summary>
		/// Get or set the element at the specified <paramref name="index"/>.
		/// </summary>
		/// <param name="index"> The zero-based index of the element to get or set.</param>
		/// <returns>The object at the specified <paramref name="index"/>.</returns>
		public override T this[int index]
		{
			set
			{
				if (value == null)
					throw new ArgumentNullException();

				var isNew = (value.Hvo == (int)SpecialHVOValues.kHvoUninitializedObject);
				// Makes sure the object is valid, even if new.
				// Handles modified registration.
				base[index] = value;

				if (!isNew)
					((ICmObjectInternal)value).SetOwner(MainObject, Flid, index);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts an item into the list at the specified <paramref name="index"/>.
		/// </summary>
		/// <param name="index">The zero-based index at which <paramref name="obj"/> is to be inserted.</param>
		/// <param name="obj">The object to insert into the list.</param>
		/// <remarks>
		/// If <paramref name="index"/> equals the number of items in the list,
		/// then <paramref name="obj"/> is appended to the list.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public override void Insert(int index, T obj)
		{
			RemoveObjectEventArgs removeEventArgs;
			// Keep track of old owner, if there is one, for possible second pass at
			// RemoveObjectSideEffects(). If old owner is null, removeEventArgs will be null too.
			var oldOwner = obj != null ? obj.Owner : null;
			var eventArgs = PrepareToInsert(index, obj, out removeEventArgs);
			if (eventArgs == null)
				return;
			Insert(eventArgs);
			// Owning Insert may have Removed from a previous owner
			if (removeEventArgs != null && removeEventArgs.DelaySideEffects)
				((ICmObjectInternal) oldOwner).RemoveObjectSideEffects(removeEventArgs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validate the insertion and adjust the index if necessary to account for an object
		/// being moved later in the same sequence. Also calls SetOwner to set the object's
		/// members correctly and remove it from its old owner. Caller is responsible for
		/// inserting the object into the correct place in the sequence if necessary.
		/// </summary>
		/// <returns>An <see cref="AddObjectEventArgs"/> object with the parameters indicating
		/// where to insert the object OR <c>null</c> if the caller requested that the object
		/// be inserted right before itself or right after itself.</returns>
		/// ------------------------------------------------------------------------------------
		private AddObjectEventArgs PrepareToInsert(int index, T obj, out RemoveObjectEventArgs removeEventArgs)
		{
			removeEventArgs = null; // If the obj wasn't previously owned, this will remain null
			AddObjectEventArgs eventArgs = new AddObjectEventArgs(obj, Flid, index);
			MainObject.ValidateAddObject(eventArgs);

			if (obj.Hvo == (int)SpecialHVOValues.kHvoUninitializedObject)
			{
				((ICmObjectInternal)obj).InitializeNewCmObject(MainObject.Cache, MainObject, Flid, index);
			}
			else
			{
				var curIndex = IndexOf(obj);
				// if the object already exists in this sequence...
				if (curIndex != -1)
				{
					// Attempting to insert the object either right before itself or right
					// after itself is pointless.
					if (eventArgs.Index == curIndex || eventArgs.Index == curIndex + 1)
						return null;
					// If it is being moved later in the sequence the earlier occurrence
					// will be removed when we call SetOwner, so we decrement the index
					// to compensate.
					if (eventArgs.Index > curIndex)
						eventArgs = new AddObjectEventArgs(obj, Flid, index - 1);
				}
				else
				{
					if (obj.IndexInOwner > -1)
					{
						// So the object exists in A list, just not THIS list.
						// This is the only case where we want a valid RemoveObjectEventArgs

						// N.B.: This could result in RemoveObjectSideEffects() being called twice
						// in the case where an object is being moved from one sequence to another.
						// The override of RemoveObjectSideEffectsInternal() should distinguish
						// which it wants to use by testing the DelaySideEffects boolean property
						// in this case and false in the one which takes place before the actual
						// insert into the new sequence (in SetOwner() below).
						// The purpose of this is to allow the removal of a chart cell part to
						// cause the deletion of the row if it's empty after a MoveTo().
						removeEventArgs = new RemoveObjectEventArgs(obj, Flid, obj.IndexInOwner, false, true);
					}
				}
				((ICmObjectInternal)obj).SetOwner(MainObject, Flid, eventArgs.Index);
			}
			return eventArgs;
		}

		#endregion IList<T> overrides

		#region ICollection<T> overrides

		#endregion ICollection<T> overrides

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the objects from the sequence to the specified sequence.
		/// </summary>
		/// <param name="iStart">Index of first object to move.</param>
		/// <param name="iEnd">Index of last object to move.</param>
		/// <param name="seqDest">The target sequence.</param>
		/// <param name="iDestStart">Index in target sequence of first object moved.</param>
		/// ------------------------------------------------------------------------------------
		public void MoveTo(int iStart, int iEnd, IFdoOwningSequence<T> seqDest, int iDestStart)
		{
			if (iStart < 0 || iStart >= Count || iStart > iEnd)
				throw new ArgumentOutOfRangeException("iStart");

			if (iEnd < 0 || iEnd >= Count)
				throw new ArgumentOutOfRangeException("iEnd");

			if (iDestStart < 0 || iDestStart > seqDest.Count)
				throw new ArgumentOutOfRangeException("iDestStart");

			int itemsToMove = (iEnd - iStart + 1);
			var fsameSeq = false;
			if (this == seqDest && iStart < iDestStart)
				fsameSeq = true;

			while (itemsToMove > 0)
			{
				T item = this[iStart];
				seqDest.Insert(iDestStart, item);
				// In case we are moving within the same sequence in a forward direction,
				// we need to take into account the fact that our index won't change.
				if (!fsameSeq)
					iDestStart++;
				itemsToMove--;
			}
		}
	}

	/// <summary>
	/// Equality comparison for sets of ICmObjectOrId, which make a CmObject equal to an ID if it has that ID.
	/// </summary>
	class ObjectIdEquater : IEqualityComparer<ICmObjectOrId>
	{

		#region IEqualityComparer<ICmObjectOrId> Members

		public bool Equals(ICmObjectOrId x, ICmObjectOrId y)
		{
			return x.Id.Equals(y.Id);
		}
		public int GetHashCode(ICmObjectOrId obj)
		{
			// In some odd test situation, the list may have no items in it,
			// and this comparison is not called.
			// In other test contexts, it is called.
			// When it is called on an empty list, and 'obj'
			// is freshly created, "Id" is null, which results in a Null ref error,
			// when try to get its hash code. Try returning the hash code of an empty guid,
			// to see if that will satisfy the dumb test code.
			// This problem shows up when using R# test runner and running all of the FdoMainVectorTests.
			// The null ref comes up OwningCollectionContainsObject() test.
			// When using the plain nant (nunit) system, there is no problem running all of the test.
			// So, there must be some oddity, in the R# test runner system.
			return (obj.Id == null) ? Guid.Empty.GetHashCode() : obj.Id.GetHashCode();
		}

		#endregion
	}
}
