using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.Infrastructure
{
	/// <summary>
	/// Add some .Net friendly methods to IFwMetaDataCache.
	/// These are not exposed via COM.
	/// </summary>
	public interface IFwMetaDataCacheManaged : IFwMetaDataCache
	{
		/// <summary>
		/// Gets the list of field identification numbers (in no particular order).
		/// </summary>
		int[] GetFieldIds();

		/// <summary>
		/// Gets the list of class identification numbers (in no particular order).
		/// </summary>
		int[] GetClassIds();

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
		int[] GetFields(int clid, bool includeSuperclasses, int fieldTypes);

		/// <summary>
		/// Get the class Ids of the direct subclasses of the specified class Id.
		/// </summary>
		/// <param name="clid">Class Id to get the subclass Ids of.</param>
		/// <returns>An array of direct subclass class Ids.</returns>
		int[] GetDirectSubclasses(int clid);

		/// <summary>
		/// Get all of the subclass Ids, including the given class Id.
		/// </summary>
		/// <param name="clid">Class Id to get subclass Ids of.</param>
		/// <returns></returns>
		int[] GetAllSubclasses(int clid);

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
		int AddCustomField(string className, string fieldName, CellarPropertyType fieldType, int destinationClass);

		/// <summary>Check if the given flid is Custom, or not.</summary>
		/// <param name='flid'>Field identifier to check.</param>
		/// <returns>'True' if 'flid' is a custom field, otherwise 'false'. </returns>
		bool IsCustom(int flid);

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
		void DeleteCustomField(int flid);

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
		int AddCustomField(string className, string fieldName, CellarPropertyType fieldType, int destinationClass,
			string fieldHelp, int fieldWs, Guid fieldListRoot);

		/// <summary>
		/// Update a user-defined custom field.
		/// </summary>
		/// <param name="flid">field id number</param>
		/// <param name="fieldHelp">help string for the field</param>
		/// <param name="fieldWs">writing system selector for the field</param>
		/// <param name="userLabel">label string set by the user (starts out same as name)</param>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if flid is not valid.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown if flid is not a custom field.
		/// </exception>
		void UpdateCustomField(int flid, string fieldHelp, int fieldWs, string userLabel);

		/// <summary>
		/// Get the (non-virtual) fields which could contain objects of the specified clid, that is,
		/// Fields whose DestinationClass is clid or a BASE class of clid.
		/// Enhance JohnT: it would be consistent, but not currently useful, to have a boolean
		/// saying whether to include base classes.
		/// </summary>
		IEnumerable<int> GetIncomingFields(int clid, int fieldTypes);

		/// <summary>
		/// Return true if the specified class exists.
		/// </summary>
		bool ClassExists(string className);

		/// <summary>
		/// Return true if the specified field (and class) exist.
		/// </summary>
		bool FieldExists(string className, string fieldName, bool includeBaseClasses);

		/// <summary>
		/// Return true if the specified field (and class) exist.
		/// </summary>
		bool FieldExists(int classId, string fieldName, bool includeBaseClasses);

		/// <summary>
		/// Return true if the specified field exists.
		/// </summary>
		bool FieldExists(int flid);

		/// <summary>
		/// Gets the GUID of the CmPossibilityList associated with this field. This is only used by
		/// custom fields.
		/// </summary>
		/// <param name="flid">The flid.</param>
		/// <returns></returns>
		Guid GetFieldListRoot(int flid);
	}

	/// <summary>
	/// Internal interface for an MDC, which allows for communication to a BEP
	/// </summary>
	internal interface IFwMetaDataCacheManagedInternal
	{
		/// <summary>
		/// Add the persisted custom fields.
		/// </summary>
		/// <param name="customFields"></param>
		void AddCustomFields(IEnumerable<CustomFieldInfo> customFields);

		/// <summary>
		/// Get the custom fields
		/// </summary>
		/// <returns></returns>
		IEnumerable<CustomFieldInfo> GetCustomFields();

		/// <summary>Get the ID of the class having the specified name.</summary>
		/// <param name='className'>Name of the class to look for.</param>
		/// <returns>Class identification number, or 0 if not found.</returns>
		int GetClassId(string className);

		/// <summary>
		/// Get properties that can be sorted (e.g., collection and multi str/uni properties).
		/// </summary>
		/// <returns></returns>
		Dictionary<string, Dictionary<string, HashSet<string>>> GetSortableProperties();
	}

	/// <summary>
	/// Class that handles transfer of custom field information between the MDC and a BEP.
	/// </summary>
	internal class CustomFieldInfo
	{
		internal int m_flid;
		internal int m_classid;
		internal string m_classname;
		internal string m_fieldname;
		private string m_label;
		internal string Label
		{
			get
			{
				if (!String.IsNullOrEmpty(m_label))
					return m_label;
				return m_fieldname;
			}
			set { m_label = value; }
		}
		internal CellarPropertyType m_fieldType;
		internal int m_destinationClass;
		internal int m_fieldWs;
		internal string m_fieldHelp;
		internal Guid m_fieldListRoot;
		internal string Key
		{
			get { return m_classname + "^" + m_fieldname; }
		}

		public override bool Equals(object obj)
		{
			if (!(obj is CustomFieldInfo))
				return false;
			return Equals((CustomFieldInfo) obj);
		}
		public bool Equals(CustomFieldInfo obj)
		{
			return obj.m_classname == m_classname
			&& obj.m_destinationClass == m_destinationClass
			&& obj.m_fieldHelp == m_fieldHelp
			&& obj.m_fieldListRoot == m_fieldListRoot
			&& obj.m_fieldname == m_fieldname
			&& obj.m_fieldType == m_fieldType
			&& obj.m_fieldWs == m_fieldWs
			&& obj.m_flid == m_flid
			&& obj.m_label == m_label;
		}

		public override int GetHashCode()
		{
			return m_classname.GetHashCode()
				   ^ m_destinationClass
				   ^ (m_fieldHelp == null ? 0 : m_fieldHelp.GetHashCode())
				   ^ m_fieldListRoot.GetHashCode()
				   ^ m_fieldname.GetHashCode()
				   ^ m_fieldType.GetHashCode()
				   ^ m_fieldWs
				   ^ m_flid
				   ^ Label.GetHashCode();
		}

		internal bool AlmostEquals(CustomFieldInfo obj)
		{
			return obj.m_classname == m_classname
			&& obj.m_destinationClass == m_destinationClass
			&& obj.m_fieldHelp == m_fieldHelp
			&& obj.m_fieldListRoot == m_fieldListRoot
			&& obj.m_fieldname == m_fieldname
			&& obj.m_fieldType == m_fieldType
			&& obj.m_fieldWs == m_fieldWs
			&& (obj.m_flid/1000) == (m_flid/1000)	// compare only class part of flid
			&& obj.m_label == m_label;
		}

	}
}