using System;
using System.Collections.Generic;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Mocked version of IFwMetaDataCacheManaged for use in data migration tests.
	/// </summary>
	internal sealed class MockMDCForDataMigration : IFwMetaDataCacheManaged
	{
		private readonly Dictionary<int, string> m_classesById = new Dictionary<int, string>();
		private readonly Dictionary<string, int> m_classesByName = new Dictionary<string, int>();
		private readonly Dictionary<int, List<string>> m_subclasses = new Dictionary<int, List<string>>();
		private readonly Dictionary<int, string> m_superclassById = new Dictionary<int, string>();

		private readonly Dictionary<int, MockFieldInfo> m_fieldsById = new Dictionary<int, MockFieldInfo>();
		private readonly Dictionary<int, List<MockFieldInfo>> m_fieldsByClassId = new Dictionary<int, List<MockFieldInfo>>();

		/// <summary>
		/// Add some nice fake class information.
		/// </summary>
		internal void AddClass(int clsid, string className, string superclassName, List<string> directSubclasses)
		{
			m_classesById.Add(clsid, className);
			m_classesByName.Add(className, clsid);
			m_subclasses.Add(clsid, directSubclasses);
			m_superclassById.Add(clsid, superclassName);
		}

		internal struct MockFieldInfo
		{
			internal int m_flid;
			internal string m_name;
			internal CellarPropertyType m_cpt;
			internal int m_destClsid;
			internal bool m_fCustom;
			internal bool m_isVirtual;
			internal string m_fieldHelp;
			internal int m_fieldWs;
			internal Guid m_fieldListRoot;
		}

		internal void AddField(int flid, string fieldName, CellarPropertyType cpt, int destClsid)
		{
			var clsid = flid / 1000;
			var mfi = new MockFieldInfo
				{
					m_flid = flid,
					m_name = fieldName,
					m_cpt = cpt,
					m_destClsid = destClsid,
					m_fCustom = false,
					m_isVirtual = false,
					m_fieldHelp = null,
					m_fieldWs = WritingSystemServices.kwsAnal,
					m_fieldListRoot = Guid.Empty
				};
			m_fieldsById.Add(flid, mfi);
			List<MockFieldInfo> list;
			if (!m_fieldsByClassId.TryGetValue(clsid, out list))
			{
				list = new List<MockFieldInfo>();
				m_fieldsByClassId.Add(clsid, list);
			}
			list.Add(mfi);
		}

		#region Implementation of IFwMetaDataCache

		/// <summary>
		/// Alternative way to initialize, passing an XML file (like Ling.cm).
		/// &lt;class num="int" id="className" base="baseClassName" abstract="true"&gt;
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
			throw new NotSupportedException();
		}

		/// <summary>
		/// Field access methods
		/// Gets the number of "fields" defined for this conceptual model.
		///</summary>
		/// <returns>A System.Int32 </returns>
		public int FieldCount
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Gets the list of field identification numbers (in no particular order). If the array
		/// provided is too small, only an arbitrary set of cflid values is returned. If the array
		/// provided is too large, the excess entries are set to zero.
		///</summary>
		/// <param name='cflid'>The size of the output array. </param>
		/// <param name='rgflid'>An integer array for returning the field identification numbers. </param>
		public void GetFieldIds(int cflid, ArrayPtr rgflid)
		{
			throw new NotSupportedException();
		}

		/// <summary> Gets the name of the class that contains this field. </summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output name of the class that contains the field.
		/// In the database, this is the "Name" column in the Class$ table that corresponds to the
		/// Class column in the Field$ table.</returns>
		public string GetOwnClsName(int luFlid)
		{
			throw new NotImplementedException();
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
		public string GetDstClsName(int luFlid)
		{
			throw new NotImplementedException();
		}

		/// <summary> Gets the "Id" value of the class that contains this field. </summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output "Id" of the class that contains the field. In
		/// the database, this corresponds to the Class column in the Field$ table.</returns>
		public int GetOwnClsId(int luFlid)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the "Id" of the destination class that corresponds to this field. This is the "Id"
		/// of the class that is either owned or referred to by another class.
		///</summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output "Id" of the class that contains the field. In
		/// the database, this corresponds to the DstCls column in the Field$ table. If it is NULL,
		/// (int)1 is returned, which indicates the field holds a basic value instead of an
		/// object.</returns>
		public int GetDstClsId(int luFlid)
		{
			throw new NotImplementedException();
		}

		/// <summary> Gets the name of a field. </summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output name of the field. In the database, this
		/// corresponds to the "Name" column in the Field$ table.</returns>
		public string GetFieldName(int luFlid)
		{
			return m_fieldsById[luFlid].m_name;
		}

		/// <summary> Gets the user label of a field. </summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output Label of the field. In the database, this
		/// corresponds to the "UserLabel" column in the Field$ table.</returns>
		public string GetFieldLabel(int luFlid)
		{
			throw new NotImplementedException();
		}

		/// <summary> Gets the help string of a field. </summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output help string of the field. In the database, this
		/// corresponds to the "HelpString" column in the Field$ table.</returns>
		public string GetFieldHelp(int luFlid)
		{
			throw new NotImplementedException();
		}

		/// <summary> Gets the Xml UI of a field. </summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output name of the field. In the database, this
		/// corresponds to the "XmlUI" column in the Field$ table.</returns>
		public string GetFieldXml(int luFlid)
		{
			throw new NotImplementedException();
		}

		/// <summary> Gets the Ws of the field. </summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output field Ws. In the database, this
		/// corresponds to the "WsSelector" column in the Field$ table.</returns>
		public int GetFieldWs(int luFlid)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the type of the field. This value indicates if the field is a primitive data type
		/// or a MultiStr/MultiTxt value or describes the relationship
		/// between two classes (i.e. owning/reference and atomic/collection/sequence). These
		/// numeric values are defined in the <b>~FWROOT\src\cellar\lib\CmTypes.h</b> file.
		///</summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table.
		/// Historical note: at one point, the result could include the virtual bit, kcptVirtual, or'd
		/// with one of the other kcpt values. This caused endless bugs and has been removed. </param>
		/// <returns>Points to the output field type.</returns>
		public int GetFieldType(int luFlid)
		{
			return (int)(m_fieldsById[luFlid].m_cpt);
		}

		/// <summary>
		/// Given a field id and a class id, this returns true it it is legal to store this class of
		/// object in the field.
		///</summary>
		/// <param name='luFlid'>Field identification number. </param>
		/// <param name='luClid'>Class identification number. </param>
		/// <returns>Points to the output boolean set to true if luClid can be stored in
		/// luFlid, else set to false.</returns>
		public bool get_IsValidClass(int luFlid, int luClid)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Class access methods
		/// Gets the number of "classes" defined for this conceptual model.
		///</summary>
		/// <returns>A System.Int32 </returns>
		public int ClassCount
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Gets the list of class identification numbers (in no particular order). If the array
		/// provided is too small, only an arbitrary subset of cclid values is returned. If the
		/// array provided is too large, the excess entries are set to zero.
		///</summary>
		/// <param name='cclid'>The size of the output array. </param>
		/// <param name='rgclid'>An integer array for returning the class identification numbers. </param>
		public void GetClassIds(int cclid, ArrayPtr rgclid)
		{
			throw new NotSupportedException();
		}

		/// <summary> Gets the name of the class. </summary>
		/// <param name='luClid'>Class identification number. In the database, this corresponds to "Id"
		/// column in the Class$ table. </param>
		/// <returns>Points to the output name of the class with the given
		/// identification number. In the database, this is the "Name" column in the Class$ table.</returns>
		public string GetClassName(int luClid)
		{
			return m_classesById[luClid];
		}

		/// <summary> Indicates whether a class is abstract or concrete. </summary>
		/// <param name='luClid'>Class identification number. In the database, this corresponds to "Id"
		/// column in the Class$ table. </param>
		/// <returns>Points to the output boolean set to "true" if abstract, or set to
		/// "false" for concrete.</returns>
		public bool GetAbstract(int luClid)
		{
			return false;
		}

		/// <summary> Gets the base class id for a given class. </summary>
		/// <param name='luClid'>Class identification number. In the database, this corresponds to "Id"
		/// column in the Class$ table. </param>
		/// <returns>Points to the output base class identification number. In the database,
		/// this corresponds to the "Base" column in the Class$ table.</returns>
		public int GetBaseClsId(int luClid)
		{
			return m_superclassById.ContainsKey(luClid) ? m_classesByName[m_superclassById[luClid]] : 0;
		}

		/// <summary> Gets the name of the base class for a given class. </summary>
		/// <param name='luClid'>Class identification number. In the database, this corresponds to "Id"
		/// column in the Class$ table. </param>
		/// <returns>Points to the output name of the base class. In the database,
		/// this is the "Name" column in the (base) Class$ table that corresponds to the Base column
		/// in the (given) Class$ table.</returns>
		public string GetBaseClsName(int luClid)
		{
			return m_superclassById[luClid];
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
		/// <param name='rgflid'> </param>
		/// <returns></returns>
		public int GetFields(int luClid, bool fIncludeSuperclasses, int grfcpt, int cflidMax, ArrayPtr rgflid)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		///:&gt;:&gt; Reverse access methods
		///:&gt; Get the ID of the class having the specified name. Returns 0 if not found.
		///</summary>
		/// <param name='bstrClassName'> </param>
		/// <returns></returns>
		public int GetClassId(string bstrClassName)
		{
			return m_classesByName[bstrClassName];
		}

		/// <summary>
		/// Gets the field ID given the class and field names. Returns 0 if not found.
		/// Searches superclasses as well as actual class given.
		///</summary>
		/// <param name='bstrClassName'> </param>
		/// <param name='bstrFieldName'> </param>
		/// <param name='fIncludeBaseClasses'> </param>
		/// <returns></returns>
		public int GetFieldId(string bstrClassName, string bstrFieldName, bool fIncludeBaseClasses)
		{
			int clsid = m_classesByName[bstrClassName];
			return GetFieldId2(clsid, bstrFieldName, fIncludeBaseClasses);
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
		public int GetFieldId2(int luClid, string bstrFieldName, bool fIncludeBaseClasses)
		{
			List<MockFieldInfo> list;
			if (m_fieldsByClassId.TryGetValue(luClid, out list))
			{
				foreach (MockFieldInfo mfi in list)
				{
					if (mfi.m_name == bstrFieldName)
						return mfi.m_flid;
				}
				if (fIncludeBaseClasses)
				{
					string baseClass = m_superclassById[luClid];
					if (baseClass != null)
					{
						int clidBase = m_classesByName[baseClass];
						return GetFieldId2(clidBase, bstrFieldName, true);
					}
				}
			}
			return 0;
		}

		/// <summary> Gets the direct subclasses of the given class (not including itself). </summary>
		/// <param name='luClid'></param>
		/// <param name='cluMax'></param>
		/// <param name='cluOut'></param>
		/// <param name='rgluSubclasses'></param>
		public void GetDirectSubclasses(int luClid, int cluMax, out int cluOut, ArrayPtr rgluSubclasses)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Gets all subclasses of the given class, including itself (which is always the first
		/// result in the list, so it can easily be skipped if desired). The list is therefore
		/// a complete list of the classes which are valid to store in a property whose
		/// signature is the class identified by luClid.
		///</summary>
		/// <param name='luClid'> </param>
		/// <param name='cluMax'> </param>
		/// <param name='cluOut'> </param>
		/// <param name='rgluSubclasses'> </param>
		public void GetAllSubclasses(int luClid, int cluMax, out int cluOut, ArrayPtr rgluSubclasses)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Note a virtual property. The type is the simulated type, one of the original types,
		/// NOT with the virtual bit OR'd in.
		///</summary>
		/// <param name='bstrClass'> </param>
		/// <param name='bstrField'> </param>
		/// <param name='luFlid'> </param>
		/// <param name='type'> </param>
		public void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
		{
			var clsid = luFlid / 1000;
			var mfi = new MockFieldInfo
			{
				m_flid = luFlid,
				m_name = bstrField,
				m_cpt = (CellarPropertyType)type,
				m_destClsid = 0,
				m_fCustom = false,
				m_fieldHelp = null,
				m_fieldWs = WritingSystemServices.kwsAnal,
				m_fieldListRoot = Guid.Empty,
				m_isVirtual = true
			};
			m_fieldsById.Add(luFlid, mfi);
			List<MockFieldInfo> list;
			if (!m_fieldsByClassId.TryGetValue(clsid, out list))
			{
				list = new List<MockFieldInfo>();
				m_fieldsByClassId.Add(clsid, list);
			}
			list.Add(mfi);
		}

		/// <summary> </summary>
		/// <param name='luFlid'> </param>
		/// <returns></returns>
		public bool get_IsVirtual(int luFlid)
		{
			return m_fieldsById[luFlid].m_isVirtual;
		}

		/// <summary> Gets the name of a field. </summary>
		/// <param name='luFlid'>Field identification number. In the database, this corresponds to the "Id"
		/// column in the Field$ table. </param>
		/// <returns>Points to the output name of the field. In the database, this
		/// corresponds to the "Name" column in the Field$ table.
		/// This version is allowed to return null (with S_OK) if the field is unknown.</returns>
		public string GetFieldNameOrNull(int luFlid)
		{
			MockFieldInfo mfi;
			return m_fieldsById.TryGetValue(luFlid, out mfi) ? mfi.m_name : null;
		}

		#endregion

		#region Implementation of IFwMetaDataCacheManaged

		/// <summary>
		/// Should return true for value types; but not implemented
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public bool IsValueType(CellarPropertyType type)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Gets the list of field identification numbers (in no particular order).
		/// </summary>
		public int[] GetFieldIds()
		{
			return m_fieldsById.Keys.ToArray();
		}

		/// <summary>
		/// Gets the list of class identification numbers (in no particular order).
		/// </summary>
		public int[] GetClassIds()
		{
			return m_classesById.Keys.ToArray();
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
		public int[] GetFields(int clid, bool includeSuperclasses, int fieldTypes)
		{
			if ((fieldTypes != (int)CellarPropertyTypeFilter.AllAtomic) && (fieldTypes != (int)CellarPropertyTypeFilter.AllBasic))
				throw new NotSupportedException("The 'GetFields' current;y only supports 'CellarPropertyTypeFilter.AllAtomic' and 'CellarPropertyTypeFilter.AllBasic' types of fields");

			var matches = new HashSet<int>();
			List<MockFieldInfo> allPropInfo;
			if (m_fieldsByClassId.TryGetValue(clid, out allPropInfo))
			{
				var matchingFields = new List<MockFieldInfo>(allPropInfo.Where(propInfo => IsSupportedFieldType(propInfo.m_cpt)));
				matches.UnionWith(matchingFields.Select(match => match.m_flid));
			}

			if (includeSuperclasses && m_classesById[clid] != "CmObject")
				matches.UnionWith(GetFields(GetBaseClsId(clid), true, fieldTypes));
			return matches.ToArray();
		}

		private bool IsSupportedFieldType(CellarPropertyType fieldType)
		{
			switch (fieldType)
			{
				case CellarPropertyType.OwningAtomic:
				case CellarPropertyType.ReferenceAtomic:
				case CellarPropertyType.Boolean:
				//case CellarPropertyType.Float: // Not yet supported (as of 23 march 2013)
				case CellarPropertyType.GenDate:
				case CellarPropertyType.Guid:
				case CellarPropertyType.Integer:
				//case CellarPropertyType.Numeric: // Not yet supported (as of 23 march 2013)
				case CellarPropertyType.Time:
					return true;
			}
			return false;
		}

		/// <summary>
		/// Get the class Ids of the direct subclasses of the specified class Id.
		/// </summary>
		/// <param name="clid">Class Id to get the subclass Ids of.</param>
		/// <returns>An array of direct subclass class Ids.</returns>
		public int[] GetDirectSubclasses(int clid)
		{
			return m_subclasses[clid].Select(className => m_classesByName[className]).ToArray();
		}

		/// <summary>
		/// Get all of the subclass Ids, including the given class Id.
		/// </summary>
		/// <param name="clid">Class Id to get subclass Ids of.</param>
		/// <returns></returns>
		public int[] GetAllSubclasses(int clid)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Add a user-defined custom field.
		/// </summary>
		/// <param name="className">Class that gets the new custom field.</param>
		/// <param name="fieldName">Field name for the custom field.</param>
		/// <param name="fieldType">Data type for the custom field.</param>
		/// <param name="destinationClass">Class Id for object type custom properties</param>
		/// <returns>The Id for the new custom field.</returns>
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
		public int AddCustomField(string className, string fieldName, CellarPropertyType fieldType, int destinationClass)
		{
			return AddCustomField(className, fieldName, fieldType, destinationClass, null, 0, Guid.Empty);
		}

		/// <summary>Check if the given flid is Custom, or not.</summary>
		/// <param name='flid'>Field identifier to check.</param>
		/// <returns>'True' if 'flid' is a custom field, otherwise 'false'. </returns>
		public bool IsCustom(int flid)
		{
			return m_fieldsById[flid].m_fCustom;
		}

		public int AddCustomField(string className, string fieldName, CellarPropertyType fieldType,
			int destinationClass, string fieldHelp, int fieldWs, Guid fieldListRoot)
		{
			int clid;
			if (m_classesByName.TryGetValue(className, out clid))
			{
				List<MockFieldInfo> list;
				var flid = (clid * 1000) + 500;
				if (m_fieldsByClassId.TryGetValue(clid, out list))
				{
					var flidMax = flid - 1;
					foreach (MockFieldInfo mfi in list)
					{
						if (mfi.m_fCustom && mfi.m_flid >= flid)
							flidMax = mfi.m_flid;
					}
					flid = flidMax + 1;
				}
				else
				{
					list = new List<MockFieldInfo>();
					m_fieldsByClassId.Add(clid, list);
				}
				var mfiNew = new MockFieldInfo
					{
						m_flid = flid,
						m_name = fieldName,
						m_cpt = fieldType,
						m_destClsid = destinationClass,
						m_fCustom = true,
						m_fieldHelp = fieldHelp,
						m_fieldWs = fieldWs,
						m_fieldListRoot = fieldListRoot
					};
				list.Add(mfiNew);
				m_fieldsById.Add(flid, mfiNew);
				return flid;
			}

			return 0;
		}

		public void DeleteCustomField(int flid)
		{
			throw new NotImplementedException();
		}

		public void UpdateCustomField(int flid, string fieldHelp, int fieldWs, string userLabel)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<int> GetIncomingFields(int clid, int fieldTypes)
		{
			throw new NotImplementedException();
		}

		public bool ClassExists(string className)
		{
			int clid;
			return m_classesByName.TryGetValue(className, out clid);
		}

		public bool FieldExists(string className, string fieldName, bool includeBaseClasses)
		{
			int clid;
			return m_classesByName.TryGetValue(className, out clid) && FieldExists(clid, fieldName, includeBaseClasses);
		}

		public bool FieldExists(int classId, string fieldName, bool includeBaseClasses)
		{
			List<MockFieldInfo> list;
			if (m_fieldsByClassId.TryGetValue(classId, out list))
			{
				if (list.Any(mfi => mfi.m_name == fieldName))
				{
					return true;
				}
				if (includeBaseClasses)
				{
					var baseClass = m_superclassById[classId];
					if (baseClass != null)
						return FieldExists(baseClass, fieldName, true);
				}
			}
			return false;
		}

		public bool FieldExists(int flid)
		{
			MockFieldInfo mfi;
			return m_fieldsById.TryGetValue(flid, out mfi);
		}

		public IEnumerable<int> GetIncomingFields(int clid)
		{
			throw new NotImplementedException();
		}

		public Guid GetFieldListRoot(int luFlid)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}