using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.FwUtils;

// Add additional methods/properties to domain object in FdoInterfaceAdditions.cs.
// Add new interfaces to the FdoInterfaceDeclarations file.
namespace SIL.FieldWorks.FDO
{
	#region IFdo Interfaces
	/// <summary>
	/// Interface for the two types of FDO vector classes (sets and collections).
	/// </summary>
	public interface IFdoVector
	{
		/// <summary>
		/// Get an array of all the hvos.
		/// </summary>
		/// <returns></returns>
		int[] ToHvoArray();

		/// <summary>
		/// Get an array of all the Guids.
		/// </summary>
		/// <returns></returns>
		Guid[] ToGuidArray();

		/// <summary>
		/// Allows getting the actual objects, without knowing the type parameter of the collection.
		/// </summary>
		IEnumerable<ICmObject> Objects { get; }
	}

	/// <summary>
	/// Internal interface that supports persistence for FDO vectors.
	/// </summary>
	internal interface IFdoVectorInternal
	{
		/// <summary>
		/// Load/Reconstitute data using XElement.
		/// </summary>
		void LoadFromDataStoreInternal(XElement reader, ICmObjectIdFactory factory);

		/// <summary>Get an XML string that represents the entire instance.</summary>
		/// <param name='writer'>The writer in which the XML is placed.</param>
		/// <remarks>Only to be used by backend provider system.</remarks>
		void ToXMLString(XmlWriter writer);

		/// <summary>
		/// Replace the entire contents of the property with 'newValue'.
		/// </summary>
		/// <param name="newValue"></param>
		/// <param name="useAccessor"></param>
		void ReplaceAll(Guid[] newValue, bool useAccessor);
	}

	/// <summary>
	/// Internal interface implemented by FdoSet and FdoList that allows a special
	/// Clear() operation needed during object deletion.
	/// </summary>
	internal interface IFdoClearForDelete
	{
		/// <summary>
		/// Clear the list or set. Should be called with true argument ONLY by overrides of DeleteObjectBasics.
		/// </summary>
		/// <param name="forDelete"></param>
		void Clear(bool forDelete);
	}

	/// <summary>
	/// Interface for the sort of Replace that IFdoSet handles. It used to be part of that interface,
	/// but we need to be able to test and cast in cases where we do not know the type parameter.
	/// </summary>
	public interface IReplaceInSet
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replace the indicated objects (possibly none) with the new objects (possibly none).
		/// In the case of owning properties, the removed objects are really deleted; this code does
		/// not handle the possibility that some of them are included in thingsToAdd.
		/// The code will handle both newly created objects and ones being moved from elsewhere
		/// (but not from the same set, because that doesn't make sense).
		/// </summary>
		/// <param name="thingsToRemove">The things to remove.</param>
		/// <param name="thingsToAdd">The things to add.</param>
		/// ------------------------------------------------------------------------------------
		void Replace(IEnumerable<ICmObject> thingsToRemove, IEnumerable<ICmObject> thingsToAdd);
	}

	///<summary>
	/// Interface for FDO unordered sets (collection properties)
	///</summary>
	///<typeparam name="T"></typeparam>
	public interface IFdoSet<T> : ICollection<T>, IFdoVector, IReplaceInSet
		where T : class, ICmObject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an array of all CmObjects.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		T[] ToArray();

	}

	/// <summary>
	/// Interface for owning collections, which are sets, by definition.
	/// </summary>
	/// <remarks>
	/// This class does not support indexing, as collections are not ordered.
	/// </remarks>
	/// <typeparam name="T">Some kind of CmObject class.</typeparam>
	public interface IFdoOwningCollection<T> : IFdoSet<T>
		where T : class, ICmObject
	{
	}

	/// <summary>
	/// An interface that may be implemented in addition to IPropChanged. If a notifiee
	/// implements this interface, it will be notified at the start and end of a collection of changes.
	/// These calls are made both when broadcasting original changes, and during Undo and Redo.
	/// </summary>
	public interface IBulkPropChanged
	{
		/// <summary>
		/// Called at the start of broadcasting PropChanged messages, passed the count of changes.
		/// Currently this used so as to not doing anything to batch them if there is only one.
		/// </summary>
		void BeginBroadcastingChanges(int count);
		/// <summary>
		/// Called after broadcasting all changes.
		/// </summary>
		void EndBroadcastingChanges();
	}

	/// <summary>
	///
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal interface IFdoOwningCollectionInternal<T>
		where T : class, ICmObject
	{
		/// <summary>
		/// Remove an object without deleting it.
		/// This is done when an object is shifting ownership,
		/// rather than being removed and deleted.
		/// </summary>j
		/// <param name="removee"></param>
		void RemoveOwnee(T removee);
	}

	/// <summary>
	/// Interface for reference collections, which is a an unordered Set.
	/// </summary>
	/// <typeparam name="T">Some kind of CmObject class.</typeparam>
	public interface IFdoReferenceCollection<T> : IFdoSet<T>
		where T : class, ICmObject
	{

		/// <summary>
		/// Answer true if the two collections are equivalent.
		/// We do not allow for duplicates; if the sizes are the same and
		/// every element in one is in the other, we consider them equivalent.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		bool IsEquivalent(IFdoReferenceCollection<T> other);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the elements of the Set to another set.
		/// </summary>
		/// <param name="dest"></param>
		/// ------------------------------------------------------------------------------------
		void AddTo(IFdoSet<T> dest);
	}

	///<summary>
	/// Interface for FDO ordered lists (sequence properties)
	///</summary>
	///<typeparam name="T"></typeparam>
	public interface IFdoList<T> : IList<T>, IFdoVector
		where T : class, ICmObject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an array of all CmObjects.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		T[] ToArray();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replace the indicated number of items (may be zero) starting at the specified index,
		/// and replace them with the specified new items.
		/// In the case of owning collection, the deleted items will truly be deleted;
		/// the code does not handle the case where thingsToAdd includes some of the deleted items.
		/// It will handle both cases where the thingsToAdd are newly created, and where they have to move
		/// from another owner.
		/// </summary>
		/// <param name="start">The start.</param>
		/// <param name="numberToDelete">The number to delete.</param>
		/// <param name="thingsToAdd">The things to add.</param>
		/// ------------------------------------------------------------------------------------
		void Replace(int start , int numberToDelete , IEnumerable<ICmObject>thingsToAdd );
	}

	/// <summary>
	/// This is really a Set, in that the same object can't be put in more than once.
	/// But, we also need the indexing capability,
	/// in case some object wants to be picky about where it goes,
	/// as is appropriate for a sequence property.
	/// </summary>
	/// <typeparam name="T">Some kind of CmObject class.</typeparam>
	public interface IFdoOwningSequence<T> : IFdoList<T>
		where T : class, ICmObject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the objects from the sequence to the specified sequence.
		/// </summary>
		/// <param name="iStart">Index of first object to move.</param>
		/// <param name="iEnd">Index of last object to move.</param>
		/// <param name="seqDest">The target sequence.</param>
		/// <param name="iDestStart">Index in target sequence of first object moved.</param>
		/// ------------------------------------------------------------------------------------
		void MoveTo(int iStart, int iEnd, IFdoOwningSequence<T> seqDest, int iDestStart);
	}

	/// <summary>
	///
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal interface IFdoOwningSequenceInternal<T>
		where T : class, ICmObject
	{
		/// <summary>
		/// Remove an object without deleting it.
		/// This is done when an object is shifting ownership,
		/// rather than being removed and deleted.
		/// </summary>j
		/// <param name="removee"></param>
		void RemoveOwnee(T removee);
	}

	/// <summary>
	/// This interface provides support for reference sequence properties.
	/// </summary>
	/// <typeparam name="T">Some kind of CmObject class.</typeparam>
	public interface IFdoReferenceSequence<T> : IFdoList<T>
		where T : class, ICmObject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the elements of the list to an Array, starting at a particular Array index.
		/// </summary>
		/// <param name="dest">The dest.</param>
		/// <param name="destIndex">Index of the dest.</param>
		/// ------------------------------------------------------------------------------------
		void CopyTo(IFdoList<T> dest, int destIndex);
	}
	#endregion

	#region MultiString Interfaces
	/// <summary>
	///
	/// </summary>
	public interface IMultiString : IMultiStringAccessor
	{
	}

	/// <summary>
	///
	/// </summary>
	public interface IMultiUnicode : IMultiStringAccessor
	{

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		string GetAlternative(SpecialWritingSystemCodes code);
	}

	/// <summary>
	/// Interface that enhances the ITsMultiString interface for use in C#.
	/// </summary>
	public interface IMultiAccessorBase : ITsMultiString
	{
		/// <summary>
		/// The field for which it is an accessor.
		/// </summary>
		int Flid
		{ get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the not found TsString.
		/// </summary>
		/// <value>The not found TSS.</value>
		/// ------------------------------------------------------------------------------------
		ITsString NotFoundTss
		{ get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the string value for the default analysis writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ITsString AnalysisDefaultWritingSystem
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the value in the analysis default writing system to a TsString having
		/// the given simple string marked with the default analysis writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void SetAnalysisDefaultWritingSystem(string val);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the value in the vernacular default writing system to a TsString having
		/// the given simple string marked with the default vernacular writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void SetVernacularDefaultWritingSystem(string val);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the value in the user-interface default writing system to a TsString having
		/// the given simple string marked with the UI writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void SetUserWritingSystem(string val);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the specified alternative to a simple string in the appropriate writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void set_String(int ws, string val);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the string value for the default vernacular writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ITsString VernacularDefaultWritingSystem
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="System.String"/> suitable for displaying in the user interface.
		/// Will fall back to an alternate WS with a real value, but does not do the funky
		/// fall-back to ***. Can return null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string UiString
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the string value for the default user interface writing system.
		/// Get should hardly ever fail to produce something; try for any other if unsuccessful.
		/// NEVER return null; may cause crashes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ITsString UserDefaultWritingSystem
		{
			get;
			set;
		}

		/// <summary>
		/// Get the value in the UI WS, without trying to be smart if there isn't one.
		/// </summary>
		ITsString RawUserDefaultWritingSystem
		{ get; }

		/// <summary>
		/// Get the best analysis/vernacular alternative of this string.
		///	First, we try the best analysis writing systems.
		///	Failing that, we try for the best vernacular writing system.
		///	Failing that, we try for the DefaultUserWs.
		///	Failing that, we give up and use "***".
		/// </summary>
		ITsString BestAnalysisVernacularAlternative
		{ get; }

		/// <summary>
		/// Get the best analysis alternative of this string.
		///	First, we try the current analysis writing systems.
		///	Failing that, we try for the DefaultUserWs.
		///	Failing that, we give up and use "***".
		/// </summary>
		ITsString BestAnalysisAlternative
		{ get; }

		/// <summary>
		/// Get the best vernacular alternative of this string.
		///	First, we try the current vernacular writing systems.
		///	Failing that, we try for the DefaultUserWs.
		///	Failing that, we give up and use "***".
		/// </summary>
		ITsString BestVernacularAlternative
		{ get; }

		/// <summary>
		/// Get the best vernacular/analysis alternative of this string.
		///	First, we try the best vernacular writing systems.
		///	Failing that, we try for the best analysis writing system.
		///	Failing that, we try for the DefaultUserWs.
		///	Failing that, we give up and use "***".
		/// </summary>
		ITsString BestVernacularAnalysisAlternative
		{ get; }

		/// <summary>
		/// Check that the given specified logical ws can make a string for our owning object and return the actual ws.
		/// </summary>
		/// <param name="ws">the logical ws (e.g. LangProject.kwsFirstVernOrAnal)</param>
		/// <param name="actualWs">the actual ws we can make a string with</param>
		/// <returns>true, if we can make a string with the given logical ws.</returns>
		bool TryWs(int ws, out int actualWs);

		/// <summary>
		/// Check that the given specified logical ws can make a string for our owning object and return the actual ws.
		/// </summary>
		/// <param name="ws">the logical ws (e.g. LangProject.kwsFirstVernOrAnal)</param>
		/// <param name="actualWs">the actual ws we can make a string with</param>
		/// <param name="tssActual">tha actual tss associated the the ws.</param>
		/// <returns>true, if we can make a string with the given logical ws.</returns>
		bool TryWs(int ws, out int actualWs, out ITsString tssActual);

		/// <summary>
		/// Returns the WS ids that can be passed to get_String and will return a non-trivial value.
		/// (Actually it isn't guaranteed that it will return a non-trivial value, the implementation might
		/// store some trivial ones. What is guaranteed is that anything that will give a non-trivial value
		/// is included.)
		/// </summary>
		int[] AvailableWritingSystemIds { get; }

		/// <summary>
		/// Like get_String(), but if no value is known for this writing system it answers null
		/// rather than an empty TsString in the WS.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		ITsString StringOrNull(int ws);
	}

	internal interface IMultiAccessorInternal
	{
		/// <summary>
		/// Set an alternate without fanfare, such as prop changes, undo, etc.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="newValue"></param>
		/// <remarks>
		/// This is used by the Undo/Redo mechanism (ONLY).
		/// </remarks>
		void SetAltQuietly(int ws, ITsString newValue);
	}

	/// <summary>
	/// Interface that enhances the ITsMultiString interface for use in C#.
	/// </summary>
	public interface IMultiStringAccessor : IMultiAccessorBase
	{
		/// <summary>
		/// Merge two MultiAccessor objects.
		/// These cases are handled:
		///		1. If an alternative exists in both objects, nothing is merged.
		///		2. If the main object (this) is missing an alternative, and the 'source' has it, then add it to 'this'.
		///		3. If the main object has an alternative, then do nothing.
		/// </summary>
		/// <param name="source"></param>
		void MergeAlternatives(IMultiStringAccessor source);

		/// <summary>
		/// Default uses space to separate.
		/// </summary>
		void MergeAlternatives(IMultiStringAccessor source, bool fConcatenateIfBoth);

		/// <summary>
		/// Merge two MultiUnicodeAccessor objects.
		/// These cases are handled:
		///		1. If an alternative exists in both objects, nothing is merged.
		///		2. If the main object (this) is missing an alternative, and the 'source' has it, then add it to 'this'.
		///		3. If the main object has an alternative, then do nothing.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="fConcatenateIfBoth"></param>
		/// <param name="sep">separator to use if concatenating</param>
		void MergeAlternatives(IMultiStringAccessor source, bool fConcatenateIfBoth, string sep);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Try GetAlternativeTss on the given 'wsPreferred', else return the BestAlternativeTss, giving
		/// preference to vernacular or analysis if 'wsPreferred' is vernacular or analysis.
		/// </summary>
		/// <param name="wsPreferred">The ws preferred.</param>
		/// <param name="wsActual">ws of the best found alternative</param>
		/// ------------------------------------------------------------------------------------
		ITsString GetAlternativeOrBestTss(int wsPreferred, out int wsActual);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the best alternative in the order of preference requested. If none of the
		/// preferred writing systems are available, arbitrarily select one from among the
		/// available alternatives. This ensures that this will not return null unless no
		/// alternatives exist.
		/// </summary>
		/// <param name="wsActual">ws of the best found alternative</param>
		/// <param name="preferences">Writing systems to consider in order of preference (can be
		/// real writing system IDs or magic numbers.</param>
		/// ------------------------------------------------------------------------------------
		ITsString GetBestAlternative(out int wsActual, params int[] preferences);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy all writing system alternatives from the given source into this multi-string
		/// accessor overwriting any that already exist. Note that this can replace a non-empty
		/// alternative with an empty alternative. Will not change/remove pre-existing
		/// alternatives missing in the source (i.e., result will be a superset).
		/// </summary>
		/// <param name="source">The source to copy from</param>
		/// ------------------------------------------------------------------------------------
		void CopyAlternatives(IMultiStringAccessor source);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy all writing system alternatives from the given source into this multi-string
		/// accessor overwriting any that already exist. Will not change/remove pre-existing
		/// alternatives missing in the source (i.e., result will be a superset).
		/// </summary>
		/// <param name="source">The source to copy from</param>
		/// <param name="fIgnoreEmptySourceStrings"><c>True</c> to ignore any source alternatives
		/// that are empty, keeping the current alternaltive.</param>
		/// ------------------------------------------------------------------------------------
		void CopyAlternatives(IMultiStringAccessor source, bool fIgnoreEmptySourceStrings);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends all the alternatives from the source multi string to the alternatives of
		/// this here one.
		/// </summary>
		/// <param name="src">The source.</param>
		/// ------------------------------------------------------------------------------------
		void AppendAlternatives(IMultiStringAccessor src);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends all the alternatives from the source multi string to the alternatives of
		/// this here one.
		/// </summary>
		/// <param name="src">The source.</param>
		/// <param name="fPreventDuplication"><c>true</c> to avoid appending a string that is
		/// identical that matches the end of the existing string.</param>
		/// ------------------------------------------------------------------------------------
		void AppendAlternatives(IMultiStringAccessor src, bool fPreventDuplication);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether the given string occurs (as a substring) in any available
		/// alternative (using StringComparison.InvariantCultureIgnoreCase).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool OccursInAnyAlternative(string s);
	}
	#endregion

	#region Interface IEmbeddedObject
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IEmbeddedObject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text representation of this embedded object (usually used for the clipboard)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string TextRepresentation { get; }
	}
	#endregion

	#region IFdoFactory interfaces
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for internal methods defined on factories.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal interface IFdoFactoryInternal
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Basic creation method for an ICmObject
		/// </summary>
		/// <returns>A new, unowned, ICmObject with a Guid, but no Hvo.</returns>
		/// ------------------------------------------------------------------------------------
		ICmObject CreateInternal();
	}

	/// <summary>
	/// Generic factory interface for ICmObjects.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IFdoFactory<T> where T : ICmObject
	{
		/// <summary>
		/// Basic creation method for an ICmObject.
		/// </summary>
		/// <returns>A new, unowned, ICmObject with a Guid, but no Hvo.</returns>
		T Create();
	}
	#endregion

	#region Interface IVector
	/// <summary>
	/// Internal things both sequences and collections know how to do.
	/// </summary>
	internal interface IVector
	{
		/// <summary>
		/// Do any side effects required during Undo (or Redo deletion).
		/// </summary>
		void ClearForUndo();
		/// <summary>
		/// Do any side effects required after restoring the value on Undo (or Redo creation)
		/// </summary>
		void RestoreAfterUndo();
	}
	#endregion

	#region Interface IRepository
	/// <summary>
	/// Unique functions on the CmObject repository only.
	/// </summary>
	internal interface ICmObjectRepositoryInternal
	{
		/// <summary>
		/// Ensure that all objects having the specified flid have been fluffed up, and all instances of the
		/// specified property have true references to the actual objects, not just objectIDs. This means
		/// that the target object's m_incomingRefs will include a complete collection of references for
		/// this property.
		/// </summary>
		void EnsureCompleteIncomingRefsFrom(int flid);

		void RegisterObjectAsCreated(ICmObject newby);

		/// <summary>
		/// If the identity map for this ID contains a CmObject, return it.
		/// </summary>
		ICmObject GetObjectIfFluffed(ICmObjectId id);

		/// <summary>
		/// Clear any necessary caches when an Undo or Redo occurs.
		/// </summary>
		void ClearCachesOnUndoRedo();

		/// <summary>
		/// Return true if some window has this object in focus, so it should not be deleted automatically at present.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		bool IsFocused(ICmObject obj);

		/// <summary>
		/// When the specified object is no longer focused, try again to delete it.
		/// </summary>
		/// <param name="obj"></param>
		void DeleteFocusedObjectWhenNoLongerFocused(ICmObject obj);
	}

	/// <summary>
	/// Generic Repository interface for ICmObjects
	/// </summary>
	/// <typeparam name="T">Type of ICmObject</typeparam>
	public interface IRepository<T> where T : ICmObject
	{
		/// <summary>
		/// Get the object with the given ID.
		/// </summary>
		T GetObject(ICmObjectId id);
		/// <summary>
		/// Get the object with the given id.
		/// </summary>
		/// <param name="id">The Guid id for the object</param>
		/// <returns>The ICmObject of the given id.</returns>
		/// <exception cref="KeyNotFoundException">Thrown if the object does not exist.</exception>
		T GetObject(Guid id);

		/// <summary>
		/// If the given ID is valid, return true and put the corresponding object in the out param.
		/// Otherwise return false and set the out arguement to null.
		/// </summary>
		bool TryGetObject(Guid guid, out T obj);

		/// <summary>
		/// Get the object with the given HVO.
		/// </summary>
		/// <param name="hvo">The HVO for the object</param>
		/// <returns>The ICmObject of the given HVO.</returns>
		/// <exception cref="KeyNotFoundException">Thrown if the object does not exist.</exception>
		T GetObject(int hvo);

		/// <summary>
		/// If the given HVO is valid, return true and put the corresponding object in the out param.
		/// Otherwise return false and set the out arguement to null.
		/// </summary>
		bool TryGetObject(int hvo, out T obj);

		/// <summary>
		/// Get all instances of the type.
		/// </summary>
		/// <returns>A Set of all instances. (There will be zero, or more instances in the Set.)</returns>
		IEnumerable<T> AllInstances();

		/// <summary>
		/// Get all instances of the specified type. (Typically a subtype, though this is not currently enforced.)
		/// </summary>
		IEnumerable<ICmObject> AllInstances(int classId);

		/// <summary>
		/// Get the count of the objects.
		/// </summary>
		int Count { get; }
	}
	#endregion

	#region Interface ICmObjectInternal
	/// <summary>
	/// Extend the regular ICmObject to add methods useful inside the FDO assembly,
	/// but which are not available to other assemblies.
	/// </summary>
	internal interface ICmObjectInternal : ICmObject
	{
		/// <summary>
		/// Get a string that represents the CmObject in XML.
		/// </summary>
		/// <returns></returns>
		string ToXmlString();

		/// <summary>
		/// Get a byte array that contains a utf8 string that represents the CmObject in XML.
		/// </summary>
		/// <returns></returns>
		byte[] ToXmlBytes();

		/// <summary>
		/// Initialize a new ownerless object.
		/// </summary>
		/// <param name="cache"></param>
		void InitializeNewOwnerlessCmObject(FdoCache cache);

		/// <summary>
		/// A very special case, an ownerless object created with a constructor that predetermines the
		/// guid (and also sets the cache, hvo, and calls RegisterObjectAsCreated).
		/// </summary>
		void InitializeNewOwnerlessCmObjectWithPresetGuid();

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
		void InitializeNewCmObject(FdoCache cache, ICmObject owner, int owningFlid, int ord);

		/// <summary>
		/// This method checks the core validity of a CmObject.
		/// It checks the FdoCache and the Hvo, to make sure they are usable.
		/// </summary>
		void CheckBasicObjectState();

		/// <summary>
		/// Used in Undo and Redo when an object is deleted to clear references on target objects.
		/// </summary>
		void ClearIncomingRefsOnOutgoingRefs();

		/// <summary>
		/// Validation done before adding an object to some vector flid.
		/// </summary>
		/// <exception cref="InvalidOperationException">The addition is not valid</exception>
		void ValidateAddObject(AddObjectEventArgs e);

		/// <summary>
		/// Handle any side effects of adding an object to some vector flid.
		/// </summary>
		void AddObjectSideEffects(AddObjectEventArgs e);

		/// <summary>
		/// Handle any side effects of removing an object from some vector flid.
		/// </summary>
		void RemoveObjectSideEffects(RemoveObjectEventArgs e);

		/// <summary>
		/// Used in Undo and Redo when an object is re-created to restore references on target objects.
		/// </summary>
		void RestoreIncomingRefsOnOutgoingRefs();

		/// <summary>
		/// Handle any side effects of changing (add/remove/modify) an alternative ITsString.
		/// </summary>
		/// <param name="multiAltFlid">The flid that was changed</param>
		/// <param name="alternativeWs">The WS of the alternative that was changed</param>
		/// <param name="originalValue">Original value. (May be null.)</param>
		/// <param name="newValue">New value. (May be null.)</param>
		void ITsStringAltChangedSideEffects(int multiAltFlid, IWritingSystem alternativeWs, ITsString originalValue, ITsString newValue);

		/// <summary>
		/// (Re)-Set owner in new field.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="owningFlid"></param>
		/// <param name="ord"></param>
		void SetOwner(ICmObject owner, int owningFlid, int ord);

		/// <summary>
		/// Restore the owner in the restored field.
		/// </summary>
		/// <remarks>
		/// This is used only for undo/redo, where the old owner and the new owner are handled
		/// separately and independently.
		/// </remarks>
		void SetOwnerForUndoRedo(ICmObject owner, int owningFlid, int ord);

		/// <summary>
		/// Delete the object.
		/// Should only be called by the FDO cache or the vector code!
		/// Any other caller won't know what to do with the surrogate.
		/// </summary>
		void DeleteObject();

		/// <summary>
		/// Initialize an object from a data store.
		/// </summary>
		void LoadFromDataStore(FdoCache cache, XElement reader, LoadingServices loadingServices);

		/// <summary>
		///
		/// </summary>
		bool HasOwner
		{ get; }

		/// <summary>
		/// Get a Binary type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		byte[] GetBinaryProperty(int flid);
		/// <summary>
		/// Set a Binary type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The property to read.</param>
		/// <param name="useAccessor"></param>
		void SetProperty(int flid, byte[] newValue, bool useAccessor);

		/// <summary>
		/// Get a Boolean type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		bool GetBoolProperty(int flid);
		/// <summary>
		/// Set a Boolean type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		void SetProperty(int flid, bool newValue, bool useAccessor);

		/// <summary>
		/// Get a DateTime type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		DateTime GetTimeProperty(int flid);
		/// <summary>
		/// Set a DateTime type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		void SetProperty(int flid, DateTime newValue, bool useAccessor);

		/// <summary>
		/// Get a Guid type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		Guid GetGuidProperty(int flid);
		/// <summary>
		/// Set a Guid type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		void SetProperty(int flid, Guid newValue, bool useAccessor);

		/// <summary>
		/// Get an integer (int32) type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		int GetIntegerValue(int flid);
		/// <summary>
		/// Set an integer (int32) type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		void SetProperty(int flid, int newValue, bool useAccessor);

		/// <summary>
		/// Get a GenDate type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		GenDate GetGenDateProperty(int flid);
		/// <summary>
		/// Set a GenDate type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		void SetProperty(int flid, GenDate newValue, bool useAccessor);

		/// <summary>
		/// Get the value of an atomic reference or owning property, including owner.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		int GetObjectProperty(int flid);
		/// <summary>
		/// Set the value of an atomic reference or owning property, including owner.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value (may be null).</param>
		/// <param name="useAccessor"></param>
		void SetProperty(int flid, ICmObject newValue, bool useAccessor);

		/// <summary>
		/// Get an ITsString type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		ITsString GetITsStringProperty(int flid);
		/// <summary>
		/// Get a string type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		string GetStringProperty(int flid);
		/// <summary>
		/// Set a string type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The new property value.</param>
		/// <param name="useAccessor"></param>
		void SetProperty(int flid, string newValue, bool useAccessor);
		/// <summary>
		/// Get an ITsTextProps type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		ITsTextProps GetITsTextPropsProperty(int flid);
		/// <summary>
		/// Set an ITsTextProps type property.
		/// </summary>
		/// <param name="flid">The property to set.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="useAccessor"></param>
		void SetProperty(int flid, ITsTextProps newValue, bool useAccessor);
		/// <summary>
		/// Set an ITsString type property.
		/// </summary>
		/// <param name="flid">The property to set.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="useAccessor"></param>
		void SetProperty(int flid, ITsString newValue, bool useAccessor);
		/// <summary>
		/// Get an ITsMultiString type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		ITsMultiString GetITsMultiStringProperty(int flid);

		/// <summary>
		/// Get the size of a vector (seq or col) property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		int GetVectorSize(int flid);
		/// <summary>
		/// Get an vector (seq or col) property item at the given index.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="index">The property to read.</param>
		int GetVectorItem(int flid, int index);
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
		int GetObjIndex(int flid, int hvo);
		/// <summary>
		/// Get the objects in a vector (collection or sequence) type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		IEnumerable<ICmObject> GetVectorProperty(int flid);

		/// <summary>
		/// On objects that have a DateModified property, update it to Now. Other objects do nothing.
		/// </summary>
		void UpdateDateModified();

		/// <summary>
		/// Add to the set the object whose DateModified should be updated, given that the recipient
		/// has been modified. The object to add may be the recipient or one of its owners, the
		/// closest one that has a DateModified property.
		/// </summary>
		void CollectDateModifiedObject(HashSet<ICmObjectInternal> owners);

		/// <summary>
		/// Replace items in a sequence.
		/// </summary>
		void Replace(int flid, int start, int numberToDelete, IEnumerable<ICmObject> thingsToAdd);
		/// <summary>
		/// Replace items in a collection. (NOT currently implemented for sequences).
		/// </summary>
		void Replace(int flid, IEnumerable<ICmObject> thingsToRemove, IEnumerable<ICmObject> thingsToAdd);
		/// <summary>
		/// Set an vector (col or seq) type property.
		/// </summary>
		/// <param name="flid">The property to set.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="useAccessor"></param>
		void SetProperty(int flid, Guid[] newValue, bool useAccessor);
		/// <summary>
		/// Set a custom property.
		/// </summary>
		/// <param name="flid">The property to set.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="useAccessor"></param>
		void SetProperty(int flid, object newValue, bool useAccessor);

		/// <summary>
		/// This sets the hvo to the given hvo and sets the cache, which may be null.
		/// </summary>
		/// <param name="cache">FdoCache or null</param>
		/// <param name="newHvo">Original HVO, SpecialHVOValues.kHvoUninitializedObject, or SpecialHVOValues.kHvoObjectDeleted</param>
		void ResetForUndoRedo(FdoCache cache, int newHvo);

		void AddIncomingRef(IReferenceSource source);
		void RemoveIncomingRef(IReferenceSource source);
		/// <summary>
		/// Enumerate all the CmObjects that refer to the target. (At most once each, even if
		/// a given target has multiple references.)
		/// </summary>
		IEnumerable<ICmObject> IncomingRefsFrom(int flid);

		/// <summary>
		/// Returns a count of the incoming references that are NOT from one of the specified objects.
		/// </summary>
		int IncomingRefsNotFrom(HashSet<ICmObject> sources);
	}
	#endregion

	#region IUndoStackManager
	/// <summary>
	/// Interface used to get, dispose, and activate additional undo stacks.
	/// </summary>
	public interface IUndoStackManager
	{
		/// <summary>
		/// Creates a new undo stack (typically for a new main window). Does NOT make it current!
		/// </summary>
		/// <returns></returns>
		IActionHandler CreateUndoStack();

		/// <summary>
		/// Set the current undo stack (typically when the window is activated).
		/// </summary>
		void SetCurrentStack(IActionHandler stack);

		/// <summary>
		/// Inform that the stack is no longer needed (typically when the window is closed/disposed).
		/// </summary>
		void DisposeStack(IActionHandler stack);

		/// <summary>
		/// Gets a value indicating whether any of the call stacks have unsaved changes.
		/// </summary>
		bool HasUnsavedChanges { get; }

		/// <summary>
		/// Save everything that needs it to whatever backend is in use. Does NOT clear undo stacks.
		/// </summary>
		/// <remarks>Not sure this belongs in this interface, but not sure where else, either.</remarks>
		void Save();

		/// <summary>
		/// Stops the timer that periodically saves changes.
		/// </summary>
		void StopSaveTimer();

		/// <summary>
		/// Event triggered at the start of the Save process.
		/// </summary>
		event EventHandler<SaveEventArgs> OnSave;

		/// <summary>
		/// Call this as part of a global Refresh. If it's possible for the backend to get into a
		/// state where it needs to copy changes from the backing store to the in-memory objects,
		/// do so. This might involve undoing some changes, or changing the state of which changes
		/// are considered Saved or Undoable.
		/// </summary>
		void Refresh();
	}

	/// <summary>
	/// EventArgs for the OnSaved event.
	/// </summary>
	public class SaveEventArgs : EventArgs
	{
		/// <summary>
		/// True if any of the changes being saved are undoable. If this is false, the user is
		/// probably not aware that anything has changed that he might want to save.
		/// </summary>
		public bool UndoableChanges { get; internal set; }

		/// <summary>
		/// The cache being saved.
		/// </summary>
		public FdoCache Cache { get; internal set; }
	}
	#endregion

	/// <summary>
	/// Interface implemented by the UnitOfWorkService for managing reconciliation of changes made
	/// by another client.
	/// </summary>
	internal interface IReconcileChanges
	{
		/// <summary>
		/// Verifies that the changes set up when constructing the reconciler can be safely merged with any
		/// currently unsaved changes.
		/// </summary>
		bool OkToReconcileChanges();

		/// <summary>
		/// Given the changes indicated by the three lists made on some other client (which have been
		/// set up when the reconciler was created and verified using OkToReconcileChanges),
		/// make a non-undoable UOW which reflects those changes, and any changes to the unsaved UOWs
		/// that may be required.
		/// </summary>
		void ReconcileForeignChanges();
	}

	#region Interface IReferenceSource
	/// <summary>
	/// Behavior common to objects that can occur in CmObject.m_incomingRefs, that is, that can be the direct source of
	/// references to a CmObject. Implemented (explicitly) by CmObject and the two classes of reference collection.
	/// </summary>
	internal interface IReferenceSource
	{
		/// <summary>
		/// Remove one reference to the specified target object. This is typically used when deleting the target,
		/// and is called on each item in m_incomingRefs so as to eventually clear all incoming references.
		/// Thus, it does not matter which reference is deleted first, if there is more than one.
		/// (It is possible that it would be more efficient to have it remove all of them in one go. But this does not
		/// fit well with m_incomingRefs being a bag, which it needs to be so we can remove just one occurrence
		/// when modifying the referring property.)
		/// </summary>
		/// <param name="target"></param>
		void RemoveAReference(ICmObject target);

		/// <summary>
		/// Replace one reference to the specified target object with the replacement. This is typically used when
		/// merging the target with the replacement,
		/// and is called on each item in m_incomingRefs so as to eventually replace all incoming references.
		/// Thus, it does not matter which reference is changed first, if there is more than one.
		/// (It is possible that it would be more efficient to have it replace all of them in one go. But this does not
		/// fit well with m_incomingRefs being a bag, which it needs to be so we can remove just one occurrence
		/// when modifying the referring property.)
		/// </summary>
		void ReplaceAReference(ICmObject target, ICmObject replacement);

		ICmObject Source { get; }

		/// <summary>
		/// Answer true if the reference source contains a reference to the specified target in the specified flid.
		/// This should only be called on sources which do indeed refer to the target (that is, items in
		/// m_incomingRefs of target), so collections will assume the answer is true if their flid matches.
		/// CmObjects must check that the specified flid contains the target.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="flid"></param>
		/// <returns></returns>
		bool RefersTo(ICmObject target, int flid);
	}
	#endregion

	#region Interface IStoresFdoCache
	/// <summary>
	/// This interface is common to objects which can be initialized with an FdoCache.
	/// </summary>
	public interface IStoresFdoCache
	{
		/// <summary>
		/// Set the cache. This may be used on initializers which only optionally pass
		/// information on to a child object, so there is no getter.
		/// </summary>
		FdoCache Cache { set; }
	}
	#endregion Interface IStoresFdoCache

	#region Interface IStoresDataAccess
	/// <summary>
	/// This interface is common to objects which can be initialized with a DataAccess (often overriding the one obtained
	/// from IStoresFdoCache).
	/// </summary>
	public interface IStoresDataAccess
	{
		/// <summary>
		/// Set the SDA. This may be used on initializers which only optionally pass
		/// information on to a child object, so there is no getter.
		/// </summary>
		ISilDataAccess DataAccess { set; }
	}
	#endregion Interface IStoresFdoCache

	#region Interface ICmPossibilitySupplier
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Inteface need to get a CmPossibility for filtering
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface ICmPossibilitySupplier
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the single chosen CmPossibility, or null to cancel filter.
		/// </summary>
		/// <param name="list">CmPossibilityList from which an item can be chosen</param>
		/// <param name="initialSelected">Default selected CmPossibilty, or null if no default selection</param>
		/// <returns>Return the selected CmPossibility, or null if none selected.</returns>
		/// ------------------------------------------------------------------------------------
		ICmPossibility GetPossibility(ICmPossibilityList list, ICmPossibility initialSelected);
	}
	#endregion

	#region Interface IFilter
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This interface allows the FilteredSequenceHandler to filter objects. It calls
	/// MatchesCriteria once for every object in the collection. Depending on the result of this
	/// method the object is discarded or included in the filtered collection.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IFilter
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string FilterName
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the filter so it can check for matches. This must be called once before
		/// calling <see cref="MatchesCriteria"/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void InitCriteria();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the given object agains the filter criteria
		/// </summary>
		/// <remarks>currently only handles basic filters (single cell)</remarks>
		/// <param name="hvoObj">ID of object to check against the filter criteria</param>
		/// <returns><c>true</c> if the object passes the filter criteria; otherwise
		/// <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		bool MatchesCriteria(int hvoObj);
	}
	#endregion

	#region Interface ISortSpec
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface ISortSpec
	{
		// not implemented yet
	}
	#endregion

	#region Interface IFlidProvider
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface used as a callback from FilteredSequenceHandler. Its purpose is to provide
	/// a flid for a given HVO. A simple implementation (as in SimpleFlidProvider) always
	/// returns a constant flid. More complex implementations (e.g. in UserView) return
	/// different flids based on the HVO.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IFlidProvider
	{
		/// ----------------------------------------------------------------------------------------
		/// <summary>Gets a flid for an object that owns the the virtual property
		/// (e.g., for Consultant Notes, this should be the HVO of a ScrBookAnnotations object)
		/// </summary>
		/// <param name="hvoPropOwner">HVO of the object that owns the collection to be
		/// filtered</param>
		/// <returns>The desired flid</returns>
		/// ----------------------------------------------------------------------------------------
		int GetFlidForPropOwner(int hvoPropOwner);
	}
	#endregion

	#region Interface ITssValue
	/// <summary>
	/// This interface should be implemented by items in the list, unless they ARE ITsStrings.
	/// Eventually we may also allow items that merely have a property that returns an ITsString.
	/// </summary>
	public interface ITssValue
	{
		/// <summary>
		/// Get a TsString representation of the object.
		/// </summary>
		ITsString AsTss { get;}
	}

	#endregion Interface ITssValue

	#region interface IOverlappingFileResolver
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface need to get a file to remove when there's overlap in the reference ranges.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IOverlappingFileResolver
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine which file to remove from a pair of files which have overlapping
		/// references.
		/// </summary>
		/// <param name="file1">file info 1</param>
		/// <param name="file2">file info 2</param>
		/// <returns>The file to remove</returns>
		/// ------------------------------------------------------------------------------------
		IScrImportFileInfo ChooseFileToRemove(IScrImportFileInfo file1, IScrImportFileInfo file2);
	}
	#endregion

	#region Interface IParaStylePropsProxy
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IParaStylePropsProxy
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the TE style id (currently a string, but someday maybe an int)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string StyleId { get; }
	}
	#endregion

	#region Interface ICloneableCmObject
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface ICloneableCmObject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the created cloned object with the details of this object
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		void SetCloneProperties(ICmObject clone);
	}
	#endregion

	#region Interface IDummy
	/// <summary>
	/// Interface for owners to convert dummy object member to a real one.
	/// </summary>
	public interface IDummy
	{
		/// <summary>
		/// Allows owners to convert dummy object member to a real one.
		/// </summary>
		/// <param name="owningFlid"></param>
		/// <param name="hvoDummy"></param>
		/// <returns></returns>
		ICmObject ConvertDummyToReal(int owningFlid, int hvoDummy);

		/// <summary>
		/// Notify owners that their dummy objects are about to become invalid.
		/// </summary>
		/// <param name="args"></param>
		bool OnPrepareToRefresh(object args);
	}
	#endregion

	#region Miscellaneous Interface ICmObject*
	/// <summary>
	/// The public face of CmObjectId, basically a guid, but CmObject also implements this.
	/// </summary>
	public interface ICmObjectId : ICmObjectOrId
	{
		/// <summary>
		/// Get the guid that the object ID wraps.
		/// </summary>
		Guid Guid { get; }

		/// <summary>
		/// Add to the writer the standard XML representation of a reference to this object,
		/// marked to indicate whether it is an owned or referenced object.
		/// </summary>
		void ToXMLString(bool owning, XmlWriter writer);
	}

	/// <summary>
	/// Functions required of both CmObjects and CmObjectIds, but which we don't want to make public.
	/// </summary>
	internal interface ICmObjectOrIdInternal
	{
		/// <summary>
		/// This allows the GetHvoFromObjectOrId method of FDOBackendProvider to be implemented as a virtual method
		/// call rather than a mess of "if X is Y...."
		/// </summary>
		int GetHvo(Infrastructure.Impl.IdentityMap map);
	}

	/// <summary>
	/// An interface for fields that can hold either some kind of CmObject or an ICmObjectId.
	/// </summary>
	public interface ICmObjectOrId
	{
		/// <summary>
		/// The ID.
		/// </summary>
		ICmObjectId Id { get;}
		/// <summary>
		/// To be sure of getting an object, we have to pass a repo, in case the recipient is
		/// actually just an ID.
		/// </summary>
		ICmObject GetObject(ICmObjectRepository repo);
	}

	/// <summary>
	/// The thing that knows various ways to get ICmObjectIds. Ensures each is canonical for the repository that
	/// contains it, so we can generally use referential equality and don't waste space on multiple copies.
	/// </summary>
	public interface ICmObjectIdFactory
	{
		/// <summary>
		/// Make one from a base-64 string representation of the guid.
		/// </summary>
		ICmObjectId FromBase64String(string guid);
		/// <summary>
		/// Make one from a guid.
		/// </summary>
		ICmObjectId FromGuid(Guid guid);
		/// <summary>
		/// Make a brand-new one, with a never-before-used-in-the-universe guid.
		/// </summary>
		ICmObjectId NewId();
	}
	#endregion

	#region Interface IWritingSystemContainer
	/// <summary>
	/// This interface represents various writing systems in FDO.
	/// </summary>
	public interface IWritingSystemContainer
	{
		/// <summary>
		/// Gets all writing systems.
		/// </summary>
		/// <value>All writing systems.</value>
		IEnumerable<IWritingSystem> AllWritingSystems { get; }

		/// <summary>
		/// Gets the analysis writing systems.
		/// </summary>
		/// <value>The analysis writing systems.</value>
		ICollection<IWritingSystem> AnalysisWritingSystems { get; }

		/// <summary>
		/// Gets the vernacular writing systems.
		/// </summary>
		/// <value>The vernacular writing systems.</value>
		ICollection<IWritingSystem> VernacularWritingSystems { get; }

		/// <summary>
		/// Gets the current analysis writing systems. Please don't Add directly to
		/// this list. Instead use AddToCurrentAnalysisWritingSystems. When removing
		/// from this list, please first remove from AnalysisWritingSystems.
		/// </summary>
		/// <value>The current analysis writing systems.</value>
		IList<IWritingSystem> CurrentAnalysisWritingSystems { get; }

		/// <summary>
		/// Gets the current vernacular writing systems. Please don't Add directly to
		/// this list. Instead use AddToCurrentVernacularWritingSystems. When removing
		/// from this list, please first remove from VernacularWritingSystems.
		/// </summary>
		/// <value>The current vernacular writing systems.</value>
		IList<IWritingSystem> CurrentVernacularWritingSystems { get; }

		/// <summary>
		/// Gets the current pronunciation writing systems.
		/// </summary>
		/// <value>The current pronunciation writing systems.</value>
		IList<IWritingSystem> CurrentPronunciationWritingSystems { get; }

		/// <summary>
		/// Gets the default analysis writing system.
		/// </summary>
		/// <value>The default analysis writing system.</value>
		IWritingSystem DefaultAnalysisWritingSystem { get; set; }

		/// <summary>
		/// Gets the default vernacular writing system.
		/// </summary>
		/// <value>The default vernacular writing system.</value>
		IWritingSystem DefaultVernacularWritingSystem { get; set; }

		/// <summary>
		/// Gets the default pronunciation writing system.
		/// </summary>
		/// <value>The default pronunciation writing system.</value>
		IWritingSystem DefaultPronunciationWritingSystem { get; }

		/// <summary>
		/// Adds the given writing system to the current analysis writing systems
		/// and also to the collection of all analysis writing systems if necessary.
		/// </summary>
		/// <param name="ws">The writing system to add.</param>
		void AddToCurrentAnalysisWritingSystems(IWritingSystem ws);

		/// <summary>
		/// Adds the given writing system to the current vernacular writing systems
		/// and also to the collection of all vernacular writing systems if necessary.
		/// </summary>
		/// <param name="ws">The writing system to add.</param>
		void AddToCurrentVernacularWritingSystems(IWritingSystem ws);
	}
	#endregion

	#region Interface IPropertyChangeNotifier
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a CmObject that wants to be notified of changes that were made to it's
	/// properties at the end of a UOW for the purpose of firing any events (which can't be
	/// fired during a UOW) for external listeners that might need to be notified of the change.
	/// For example: Scripture uses this notification to let listeners know of changes to the
	/// number of books.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal interface IPropertyChangeNotifier
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to tell the notifier that the UOW is complete so that it can notify anyone
		/// who cares about the change.
		/// </summary>
		/// <param name="flid">The flid of the changed property.</param>
		/// ------------------------------------------------------------------------------------
		void NotifyOfChangedProperty(int flid);
	}
	#endregion

	#region Interface IParagraphCounter
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IParagraphCounter
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of paragraphs that make up an object when displayed as the specified
		/// frag
		/// </summary>
		/// <param name="hvo">The hvo of the object</param>
		/// <param name="frag">The frag used to display the object</param>
		/// <returns>The number of paragraphs that make up the specified object for the
		/// specified frag</returns>
		/// ------------------------------------------------------------------------------------
		int GetParagraphCount(int hvo, int frag);
	}
	#endregion
}
