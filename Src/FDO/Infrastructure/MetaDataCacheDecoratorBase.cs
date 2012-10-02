using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.Infrastructure
{
	/// <summary>
	/// Implementation of the IFwMetaDataCacheManaged interface which works with a stateful FDO system for most data issues.
	/// For cases where there is a need to store 'fake' data (i.e., not properties of CmObjects),
	/// clients should subclass this class and override the relevant methods
	/// and read/write that fake meta data in their own internal caches. They should pass the request through to
	/// the FdoMetaDataCache (IFwMetaDataCacheManaged) for regular metadata access.
	/// </summary>
	public abstract class FdoMetaDataCacheDecoratorBase : IFwMetaDataCacheManaged
	{
		private readonly IFwMetaDataCacheManaged m_metaDataCache;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="metaDataCache">The FDO FdoMetaDataCache implementation,
		/// which is used to get the basic FDO data.</param>
		/// <remarks>
		/// The hvo values are true 'handles' in that they are valid for one session,
		/// but may not be the same integer for another session for the 'same' object.
		/// Therefore, one should not use them for multi-session identity.
		/// CmObject identity can only be guaranteed by using their Guids (or using '==' in code).
		/// </remarks>
		protected FdoMetaDataCacheDecoratorBase(IFwMetaDataCacheManaged metaDataCache)
		{
			if (metaDataCache == null) throw new ArgumentNullException("metaDataCache");

			m_metaDataCache = metaDataCache;
		}

		#region Implementation of IFwMetaDataCache

		/// <summary>
		/// Alternative way to initialize, passing an XML file (like Ling.cm).
		///&lt;class num="int" id="className" base="baseClassName" abstract="true"&gt;
		/// &lt;props&gt;
		/// &lt;basic num="int" id="FieldName" sig="Boolean/Integer/Time/String/MultiString/MultiUnicode" /&gt;
		/// &lt;rel/owning num="int" id="FieldName" card="atomic/seq/col" sig="classname"/&gt;
		/// currently doesn't initialize some less essential stuff like help strings and labels.
		/// Set fClearPrevCache to false to read in multiple XML files.
		/// Enhance JohnT: support attributes to handle these.
		///</summary>
		/// <param name='bstrPathname'> </param>
		/// <param name='fClearPrevCache'> </param>
		public void InitXml(string bstrPathname, bool fClearPrevCache)
		{
			throw new NotSupportedException("'InitXml' not supported, because this class loads the model itself.");
		}

		/// <summary>
		///:&gt;:&gt; Field access methods
		///:&gt; Gets the number of "fields" defined for this conceptual model.
		///</summary>
		/// <returns>A System.Int32 </returns>
		public virtual int FieldCount
		{
			get { return m_metaDataCache.FieldCount; }
		}

		/// <summary>
		/// Gets the list of field identification numbers (in no particular order). If the array
		/// provided is too small, only an arbitrary set of cflid values is returned. If the array
		/// provided is too large, the excess entries are set to zero.
		///</summary>
		/// <param name='cflid'>The size of the output array. </param>
		/// <param name='rgflid'>An integer array for returning the field identification numbers. </param>
		public virtual void GetFieldIds(int cflid, ArrayPtr rgflid)
		{
			m_metaDataCache.GetFieldIds(cflid, rgflid);
		}

		/// <summary> Gets the name of the class that contains this field. </summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output name of the class that contains the field.
		/// In the database, this is the "Name" column in the Class$ table that corresponds to the
		/// Class column in the Field$ table.</returns>
		public virtual string GetOwnClsName(int luFlid)
		{
			return m_metaDataCache.GetOwnClsName(luFlid);
		}

		/// <summary>
		/// Gets the name of the destination class that corresponds to this field. This is the name
		/// of the class that is either owned or referred to by another class.
		///</summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output name of the destination class. In the
		/// database, this is the "Name" column in the Class$ table that corresponds to the DstCls
		/// column in the Field$ table.</returns>
		public virtual string GetDstClsName(int luFlid)
		{
			return m_metaDataCache.GetDstClsName(luFlid);
		}

		/// <summary> Gets the "Id" value of the class that contains this field. </summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output "Id" of the class that contains the field. In
		/// the database, this corresponds to the Class column in the Field$ table.</returns>
		public virtual int GetOwnClsId(int luFlid)
		{
			return m_metaDataCache.GetOwnClsId(luFlid);
		}

		/// <summary>
		/// Gets the "Id" of the destination class that corresponds to this field. This is the "Id"
		/// of the class that is either owned or referred to by another class.
		///</summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output "Id" of the class that contains the field. In
		/// the database, this corresponds to the DstCls column in the Field$ table. If it is NULL,
		/// (ULONG)1 is returned, which indicates the field holds a basic value instead of an
		/// object.</returns>
		public virtual int GetDstClsId(int luFlid)
		{
			return m_metaDataCache.GetDstClsId(luFlid);
		}

		/// <summary> Gets the name of a field. </summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output name of the field. In the database, this
		/// corresponds to the "Name" column in the Field$ table.</returns>
		public virtual string GetFieldName(int luFlid)
		{
			return m_metaDataCache.GetFieldName(luFlid);
		}

		/// <summary> Gets the user label of a field. </summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output Label of the field. In the database, this
		/// corresponds to the "UserLabel" column in the Field$ table.</returns>
		public virtual string GetFieldLabel(int luFlid)
		{
			return m_metaDataCache.GetFieldLabel(luFlid);
		}

		/// <summary> Gets the help string of a field. </summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output help string of the field. In the database, this
		/// corresponds to the "HelpString" column in the Field$ table.</returns>
		public virtual string GetFieldHelp(int luFlid)
		{
			return m_metaDataCache.GetFieldHelp(luFlid);
		}

		/// <summary> Gets the Xml UI of a field. </summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output name of the field. In the database, this
		/// corresponds to the "XmlUI" column in the Field$ table.</returns>
		public virtual string GetFieldXml(int luFlid)
		{
			return m_metaDataCache.GetFieldXml(luFlid);
		}

		/// <summary> Gets the listRoot of the field. </summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output field ListRoot. In the database, this
		/// corresponds to the "ListRootId" column in the Field$ table.</returns>
		public virtual Guid GetFieldListRoot(int luFlid)
		{
			return m_metaDataCache.GetFieldListRoot(luFlid);
		}

		/// <summary> Gets the Ws of the field. </summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output field Ws. In the database, this
		/// corresponds to the "WsSelector" column in the Field$ table.</returns>
		public virtual int GetFieldWs(int luFlid)
		{
			return m_metaDataCache.GetFieldWs(luFlid);
		}

		/// <summary>
		/// Gets the type of the field. This value indicates if the field is a primitive data type
		/// or a MultiStr/MultiBigStr/MultiTxt/MultiBigTxt value or describes the relationship
		/// between two classes (i.e. owning/reference and atomic/collection/sequence). These
		/// numeric values are defined in the <b>~FWROOT\src\cellar\lib\CmTypes.h</b> file.
		///</summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table.
		/// Historical note: at one point, the result could include the virtual bit, kcptVirtual, or'd
		/// with one of the other kcpt values. This caused endless bugs and has been removed. </param>
		/// <returns>Points to the output field type.</returns>
		public virtual int GetFieldType(int luFlid)
		{
			return m_metaDataCache.GetFieldType(luFlid);
		}

		/// <summary>
		/// Given a field id and a class id, this returns true it it is legal to store this class of
		/// object in the field.
		///</summary>
		/// <param name='luFlid'>Field identification number. </param>
		/// <param name='luClid'>Class identification number. </param>
		/// <returns>Points to the output boolean set to true if luClid can be stored in
		/// luFlid, else set to false.</returns>
		public virtual bool get_IsValidClass(int luFlid, int luClid)
		{
			return m_metaDataCache.get_IsValidClass(luFlid, luClid);
		}

		/// <summary>
		///:&gt;:&gt; Class access methods
		///:&gt; Gets the number of "classes" defined for this conceptual model.
		///</summary>
		/// <returns>A System.Int32 </returns>
		public virtual int ClassCount
		{
			get { return m_metaDataCache.ClassCount; }
		}

		/// <summary>
		/// Gets the list of class identification numbers (in no particular order). If the array
		/// provided is too small, only an arbitrary subset of cclid values is returned. If the
		/// array provided is too large, the excess entries are set to zero.
		///</summary>
		/// <param name='cclid'>The size of the output array. </param>
		/// <param name='rgclid'>An integer array for returning the class identification numbers. </param>
		public virtual void GetClassIds(int cclid, ArrayPtr rgclid)
		{
			m_metaDataCache.GetClassIds(cclid, rgclid);
		}

		/// <summary> Gets the name of the class. </summary>
		/// <param name='luClid'>Class identification number. In the database, this corresponds to "Id"
		/// column in the Class$ table. </param>
		/// <returns>Points to the output name of the class with the given
		/// identification number. In the database, this is the "Name" column in the Class$ table.</returns>
		public virtual string GetClassName(int luClid)
		{
			return m_metaDataCache.GetClassName(luClid);
		}

		/// <summary> Indicates whether a class is abstract or concrete. </summary>
		/// <param name='luClid'>Class identification number. In the database, this corresponds to "Id"
		/// column in the Class$ table. </param>
		/// <returns>Points to the output boolean set to "true" if abstract, or set to
		/// "false" for concrete.</returns>
		public virtual bool GetAbstract(int luClid)
		{
			return m_metaDataCache.GetAbstract(luClid);
		}

		/// <summary> Gets the base class id for a given class. </summary>
		/// <param name='luClid'>Class identification number. In the database, this corresponds to "Id"
		/// column in the Class$ table. </param>
		/// <returns>Points to the output base class identification number. In the database,
		/// this corresponds to the "Base" column in the Class$ table.</returns>
		public virtual int GetBaseClsId(int luClid)
		{
			return m_metaDataCache.GetBaseClsId(luClid);
		}

		/// <summary> Gets the name of the base class for a given class. </summary>
		/// <param name='luClid'>Class identification number. In the database, this corresponds to "Id"
		/// column in the Class$ table. </param>
		/// <returns>Points to the output name of the base class. In the database,
		/// this is the "Name" column in the (base) Class$ table that corresponds to the Base column
		/// in the (given) Class$ table.</returns>
		public virtual string GetBaseClsName(int luClid)
		{
			return m_metaDataCache.GetBaseClsName(luClid);
		}

		/// <summary>
		/// Gets a list of the fields for the specified class.
		/// Gets all fields whose types match the specified argument, which should be a combination
		/// of the fcpt values defined in CmTypes.h, e.g., to get all owning properties
		/// pass kfcptOwningCollection | kfcptOwningAtom | kfcptOwningSequence.
		/// Returns E_FAIL if the array is too small. cflidMax 0 may be passed to obtain the required
		/// size.
		/// Fields of superclasses are also returned, if the relevant flag is true.
		/// [Note: The special CmObject fields are not returned, for now,
		/// but the plan to include them before too long.]
		///</summary>
		/// <param name='luClid'> </param>
		/// <param name='fIncludeSuperclasses'> </param>
		/// <param name='grfcpt'> </param>
		/// <param name='cflidMax'> </param>
		/// <param name='_rgflid'> </param>
		/// <returns></returns>
		public virtual int GetFields(int luClid, bool fIncludeSuperclasses, int grfcpt, int cflidMax, ArrayPtr _rgflid)
		{
			return m_metaDataCache.GetFields(luClid, fIncludeSuperclasses, grfcpt, cflidMax, _rgflid);
		}

		/// <summary>
		///:&gt;:&gt; Reverse access methods
		///:&gt; Get the ID of the class having the specified name. Returns 0 if not found.
		///</summary>
		/// <param name='bstrClassName'> </param>
		/// <returns></returns>
		public virtual int GetClassId(string bstrClassName)
		{
			return m_metaDataCache.GetClassId(bstrClassName);
		}

		/// <summary>
		/// Gets the field ID given the class and field names. Returns 0 if not found.
		/// Searches superclasses as well as actual class given.
		///</summary>
		/// <param name='bstrClassName'> </param>
		/// <param name='bstrFieldName'> </param>
		/// <param name='fIncludeBaseClasses'> </param>
		/// <returns></returns>
		public virtual int GetFieldId(string bstrClassName, string bstrFieldName, bool fIncludeBaseClasses)
		{
			return m_metaDataCache.GetFieldId(bstrClassName, bstrFieldName, fIncludeBaseClasses);
		}

		/// <summary>
		/// This is more efficient if the client already has the classID specified classID and field name.
		/// Returns 0 if not found.
		/// Searches superclasses as well as actual class given.
		///</summary>
		/// <param name='luClid'> </param>
		/// <param name='bstrFieldName'> </param>
		/// <param name='fIncludeBaseClasses'> </param>
		/// <returns></returns>
		public virtual int GetFieldId2(int luClid, string bstrFieldName, bool fIncludeBaseClasses)
		{
			return m_metaDataCache.GetFieldId2(luClid, bstrFieldName, fIncludeBaseClasses);
		}

		/// <summary> Gets the direct subclasses of the given class (not including itself). </summary>
		/// <param name='luClid'> </param>
		/// <param name='cluMax'> </param>
		/// <param name='_cluOut'> </param>
		/// <param name='_rgluSubclasses'> </param>
		public virtual void GetDirectSubclasses(int luClid, int cluMax, out int _cluOut, ArrayPtr _rgluSubclasses)
		{
			m_metaDataCache.GetDirectSubclasses(luClid, cluMax, out _cluOut, _rgluSubclasses);
		}

		/// <summary>
		/// Gets all subclasses of the given class, including itself (which is always the first
		/// result in the list, so it can easily be skipped if desired). The list is therefore
		/// a complete list of the classes which are valid to store in a property whose
		/// signature is the class identified by luClid.
		///</summary>
		/// <param name='luClid'> </param>
		/// <param name='cluMax'> </param>
		/// <param name='_cluOut'> </param>
		/// <param name='_rgluSubclasses'> </param>
		public virtual void GetAllSubclasses(int luClid, int cluMax, out int _cluOut, ArrayPtr _rgluSubclasses)
		{
			m_metaDataCache.GetAllSubclasses(luClid, cluMax, out _cluOut, _rgluSubclasses);
		}

		/// <summary>
		/// A virtual property. The type can be one of the original types, or any other made up type.
		///</summary>
		/// <param name='bstrClass'> </param>
		/// <param name='bstrField'> </param>
		/// <param name='luFlid'> </param>
		/// <param name='type'> </param>
		/// <remarks>
		/// This method must be overridden by subclasses, so they can add non-model properties.
		/// </remarks>
		public abstract void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type);

		/// <summary> </summary>
		/// <param name='luFlid'> </param>
		/// <returns></returns>
		public virtual bool get_IsVirtual(int luFlid)
		{
			return m_metaDataCache.get_IsVirtual(luFlid);
		}

		/// <summary> Gets the name of a field. </summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output name of the field. In the database, this
		/// corresponds to the "Name" column in the Field$ table.
		/// This version is allowed to return null (with S_OK) if the field is unknown.</returns>
		public virtual string GetFieldNameOrNull(int luFlid)
		{
			return m_metaDataCache.GetFieldNameOrNull(luFlid);
		}

		#endregion

		#region Implementation of IFwMetaDataCacheManaged

		/// <summary>
		/// Gets the list of field identification numbers (in no particular order).
		/// </summary>
		[ComVisible(false)]
		public virtual int[] GetFieldIds()
		{
			return m_metaDataCache.GetFieldIds();
		}

		/// <summary>
		/// Gets the list of class identification numbers (in no particular order).
		/// </summary>
		[ComVisible(false)]
		public virtual int[] GetClassIds()
		{
			return m_metaDataCache.GetClassIds();
		}

		/// <summary>
		/// Get the field Ids for the specified class, and optionally its superclasses.
		/// </summary>
		/// <param name="clid">Class identification number.</param>
		/// <param name="includeSuperclasses">'True' to also get superclass fields.</param>
		/// <param name='fieldTypes'>
		/// Gets all fields whose types match the specified argument,
		/// which should be a combination of the fcpt values defined in CmTypes.h, e.g.,
		/// to get all owning properties pass kfcptOwningCollection | kfcptOwningAtom | kfcptOwningSequence.
		/// </param>
		/// <returns>The field Ids.</returns>
		[ComVisible(false)]
		public virtual int[] GetFields(int clid, bool includeSuperclasses, int fieldTypes)
		{
			return m_metaDataCache.GetFields(clid, includeSuperclasses, fieldTypes);
		}

		/// <summary>
		/// Get the class Ids of the direct subclasses of the specified class Id.
		/// </summary>
		/// <param name="clid">Class Id to get the subclass Ids of.</param>
		/// <returns>An array of direct subclass class Ids.</returns>
		[ComVisible(false)]
		public virtual int[] GetDirectSubclasses(int clid)
		{
			return m_metaDataCache.GetDirectSubclasses(clid);
		}

		/// <summary>
		/// Get all of the subclass Ids, including the given class Id.
		/// </summary>
		/// <param name="clid">Class Id to get subclass Ids of.</param>
		/// <returns></returns>
		[ComVisible(false)]
		public virtual int[] GetAllSubclasses(int clid)
		{
			return m_metaDataCache.GetAllSubclasses(clid);
		}

		/// <summary>
		/// Add a user-defined custom field.
		/// </summary>
		/// <param name="className">Class that gets the new custom field.</param>
		/// <param name="fieldName">Field name for the custom field.</param>
		/// <param name="fieldType">Data type for the custom field.</param>
		/// <param name="destinationClass">Class Id for object type custom properties</param>
		/// <returns>The Id for tne new custom field.</returns>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if 'fieldType' is an object type (owning/reference and atomic/collection/sequence) property,
		/// but 'destinationClass' does not match a class in the model.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// Thrown if 'className' or 'fieldName' are null or empty.
		/// </exception>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if class is not defined.
		/// </exception>
		/// <remarks>
		/// If 'fieldType' is a basic property type, then 'destinationClass' is ignored.
		///
		/// If 'fieldType' is an object type (owning/reference and atomic/collection/sequence) property,
		/// then 'destinationClass' must match a class in the model.
		/// </remarks>
		[ComVisible(false)]
		public virtual int AddCustomField(string className, string fieldName, CellarPropertyType fieldType, int destinationClass)
		{
			return m_metaDataCache.AddCustomField(className, fieldName, fieldType, destinationClass);
		}

		/// <summary>Check if the given flid is Custom, or not.</summary>
		/// <param name='flid'>Field identifier to check.</param>
		/// <returns>'True' if 'flid' is a custom field, otherwise 'false'. </returns>
		[ComVisible(false)]
		public virtual bool IsCustom(int flid)
		{
			return m_metaDataCache.IsCustom(flid);
		}

		/// <summary>
		/// If the given field exists and is a custom field, delete it.  This requires
		/// any data in that field to also be deleted!
		/// </summary>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if flid is not valid.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown if flid is not a custom field.
		/// </exception>
		/// <remarks>
		/// All of the data for this field should be removed before calling this method!
		/// </remarks>
		[ComVisible(false)]
		public void DeleteCustomField(int flid)
		{
			m_metaDataCache.DeleteCustomField(flid);
		}

		/// <summary>
		/// Add a user-defined custom field.
		/// </summary>
		/// <param name="className">Class that gets the new custom field.</param>
		/// <param name="fieldName">Field name for the custom field.</param>
		/// <param name="fieldType">Data type for the custom field.</param>
		/// <param name="destinationClass">Class Id for object type custom properties</param>
		/// <param name="fieldHelp">help string for the field</param>
		/// <param name="fieldWs">writing system selector for the field</param>
		/// <param name="fieldListRoot">The GUID of the root CmPossibilityList.</param>
		/// <returns>The Id for tne new custom field.</returns>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if 'fieldType' is an object type (owning/reference and atomic/collection/sequence) property,
		/// but 'destinationClass' does not match a class in the model.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// Thrown if 'className' or 'fieldName' are null or empty.
		/// </exception>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if class is not defined.
		/// </exception>
		/// <remarks>
		/// If 'fieldType' is a basic property type, then 'destinationClass' is ignored.
		/// If 'fieldType' is an object type (owning/reference and atomic/collection/sequence) property,
		/// then 'destinationClass' must match a class in the model.
		/// </remarks>
		[ComVisible(false)]
		public int AddCustomField(string className, string fieldName, CellarPropertyType fieldType,
			int destinationClass, string fieldHelp, int fieldWs, Guid fieldListRoot)
		{
			return m_metaDataCache.AddCustomField(className, fieldName, fieldType, destinationClass,
				fieldHelp, fieldWs, fieldListRoot);
		}

		/// <summary>
		/// Update a user-defined custom field.
		/// </summary>
		/// <param name="flid">field id number</param>
		/// <param name="fieldHelp">help string for the field</param>
		/// <param name="fieldWs">writing system selector for the field</param>
		/// <param name="userLabel">label chosen by user (starts out same as name)</param>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if flid is not valid.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown if flid is not a custom field.
		/// </exception>
		[ComVisible(false)]
		public void UpdateCustomField(int flid, string fieldHelp, int fieldWs, string userLabel)
		{
			m_metaDataCache.UpdateCustomField(flid, fieldHelp, fieldWs, userLabel);
		}

		/// <summary>
		/// Pass it on. Only real fields are interesting here.
		/// </summary>
		public IEnumerable<int> GetIncomingFields(int clid, int fieldTypes)
		{
			return m_metaDataCache.GetIncomingFields(clid, fieldTypes);
		}

		/// <summary>
		/// Pass it on by default.
		/// </summary>
		public virtual bool ClassExists(string className)
		{
			return m_metaDataCache.ClassExists(className);
		}

		/// <summary>
		/// Pass it on by default.
		/// </summary>
		public bool FieldExists(string className, string fieldName, bool includeBaseClasses)
		{
			return m_metaDataCache.FieldExists(className, fieldName, includeBaseClasses);
		}

		/// <summary>
		/// Return true if the specified field exists.
		/// </summary>
		public bool FieldExists(int flid)
		{
			return m_metaDataCache.FieldExists(flid);
		}
		/// <summary>
		/// Pass it on by default.
		/// </summary>
		public bool FieldExists(int classId, string fieldName, bool includeBaseClasses)
		{
			return m_metaDataCache.FieldExists(classId, fieldName, includeBaseClasses);
		}

		#endregion
	}
}