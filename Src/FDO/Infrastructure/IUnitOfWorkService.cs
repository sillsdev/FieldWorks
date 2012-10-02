using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Infrastructure.Impl;

namespace SIL.FieldWorks.FDO.Infrastructure
{
	/// <summary>
	/// Interface for the UOW manager, which allows for registereing ICmObjects with a UOW.
	/// </summary>
	internal interface IUnitOfWorkService
	{
		/// <summary>
		/// Return true if the object is newly created this UOW. Virtual PropChange information
		/// is typically not needed in this case.
		/// </summary>
		bool IsNew(ICmObject obj);
		/// <summary>
		/// Add a newly created object to the Undo/Redo system.
		/// </summary>
		/// <param name="newby"></param>
		void RegisterObjectAsCreated(ICmObject newby);

		/// <summary>
		/// Add a newly deleted object to the Undo/Redo system.
		/// </summary>
		/// <param name="goner"></param>
		/// <param name="xmlStateBeforeDeletion">Old before State XML (in case it gets undeleted).</param>
		void RegisterObjectAsDeleted(ICmObject goner, string xmlStateBeforeDeletion);

		/// <summary>
		/// Register an object as having its ownership changed.  This won't cause either an Undo or Redo, or
		/// a PropChanged, but will cause the modified XML to be written out.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="ownerBeforeChange">Owner prior to change</param>
		/// <param name="owningFlidBeforeChange">Owning field id prior to change</param>
		/// <param name="owningFlid">Owning field id after change</param>
		void RegisterObjectOwnershipChange(ICmObject dirtball, Guid ownerBeforeChange, int owningFlidBeforeChange,
			int owningFlid);

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, Guid[] originalValue, Guid[] newValue);

		/// <summary>
		/// Register an object as having a modified atomic reference.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void RegisterObjectAsModifiedRef(ICmObject dirtball, int modifiedFlid, Guid originalValue, Guid newValue);

		/// <summary>
		/// Register an object as having experienced a modification of a virtual property. This causes a PropChanged to be sent,
		/// but no actual data change to be saved.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void RegisterVirtualAsModified(ICmObject dirtball, int modifiedFlid, Guid[] originalValue, Guid[] newValue);

		/// <summary>
		/// Often a more convenient way to register a virtual as modified.
		/// </summary>
		void RegisterVirtualAsModified(ICmObject dirtball, string virtualPropName, IEnumerable<ICmObject> newValue);
		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, Guid originalValue, Guid newValue);

		/// <summary>
		/// Register an object as having experienced a modification of a virtual property. This causes a PropChanged to be sent,
		/// but no actual data change to be saved.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="reader">Function to read the current value</param>
		/// <param name="added">items added</param>
		/// <param name="removed">items removed</param>
		void RegisterVirtualCollectionAsModified<T>(ICmObject dirtball, int modifiedFlid, Func<IEnumerable<T>> reader,
			IEnumerable<T> added, IEnumerable<T> removed);

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, ITsString originalValue, ITsString newValue);

		/// <summary>
		/// Register an object as having experienced a modification of a virtual property. This causes a PropChanged to be sent,
		/// but no actual data change to be saved.
		/// </summary>
		void RegisterVirtualAsModified(ICmObject dirtball, int modifiedFlid, ITsString originalValue, ITsString newValue);

		/// <summary>
		/// Register an object as having experienced a modification of a string virtual property. This causes a PropChanged to be sent,
		/// but no actual data change to be saved.
		/// </summary>
		void RegisterVirtualAsModified(ICmObject dirtball, string virtualPropName, ITsString newValue);

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="ws">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, int ws, ITsString originalValue, ITsString newValue);

		/// <summary>
		/// Register an object as having experienced a modification of a multistring virtual property. This causes a PropChanged to be sent,
		/// but no actual data change to be saved.
		/// </summary>
		void RegisterVirtualAsModified(ICmObject dirtball, int modifiedFlid, int ws, ITsString originalValue,
			ITsString newValue);

		/// <summary>
		/// Register an object as having experienced a modification of a multistring alternative. This causes a PropChanged to be sent,
		/// but no actual data change to be saved.
		/// </summary>
		void RegisterVirtualAsModified(ICmObject dirtball, string virtualPropName, int ws);

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, ITsTextProps originalValue, ITsTextProps newValue);

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, string originalValue, string newValue);

		/// <summary>
		/// Register an object as having experienced a modification of a virtual property. This causes a PropChanged to be sent,
		/// but no actual data change to be saved.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void RegisterVirtualAsModified(ICmObject dirtball, int modifiedFlid, string originalValue, string newValue);

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, bool originalValue, bool newValue);

		/// <summary>
		/// Register an object as having experienced a modification of a virtual property. This causes a PropChanged to be sent,
		/// but no actual data change to be saved.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void RegisterVirtualAsModified(ICmObject dirtball, int modifiedFlid, bool originalValue, bool newValue);

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, DateTime originalValue, DateTime newValue);

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, byte[] originalValue, byte[] newValue);

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, int originalValue, int newValue);

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void RegisterVirtualAsModified(ICmObject dirtball, int modifiedFlid, int originalValue, int newValue);

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, object originalValue, object newValue);

		/// <summary>
		/// Register an object as having been modified.
		/// </summary>
		/// <param name="dirtball">Modified object</param>
		/// <param name="modifiedFlid">Modified flid</param>
		/// <param name="originalValue">Original value</param>
		/// <param name="newValue">New value</param>
		void RegisterObjectAsModified(ICmObject dirtball, int modifiedFlid, GenDate originalValue, GenDate newValue);

		/// <summary>
		/// Register the object as being modfied, even though no particular field is changed.
		/// This is used when we need to write a new representation of it, e.g., when WS tags have changed.
		/// </summary>
		void RegisterObjectAsModified(ICmObject dirtball);

		/// <summary>
		/// Registers an object as containing data for a custom field that was modified or deleted.
		/// </summary>
		/// <param name="dirtball">The modified object.</param>
		/// <param name="modifiedFlid">The modified flid.</param>
		void RegisterCustomFieldAsModified(ICmObject dirtball, int modifiedFlid);

		/// <summary>
		/// Get the unsaved UOWs from all the undo stacks, in the order they occurred.
		/// </summary>
		List<FdoUnitOfWork> UnsavedUnitsOfWork { get; }

		/// <summary>
		/// Gather the lists of modified etc. objects that must be saved. (This is put in the interface just for testing.)
		/// </summary>
		void GatherChanges(HashSet<ICmObjectId> newbies, HashSet<ICmObjectOrSurrogate> dirtballs,
			HashSet<ICmObjectId> goners);
	}
}
