// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FdoIFwMetaDataCache.cs
// Responsibility: Randy Regnier
// Last reviewed: never

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices; // Needed for Marshal
using System.Reflection; // To load meta data cache file data.
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
//using SIL.Utils;

/* From Field$ table
 *
 * Id - Standard Flid: MDC:int/DB:int *All
 * Type, Field Type: int *All
 * Class, Id of class with field: MDC:int/DB:int *All
 * Name, String/DB:nvarcher(100) *All
 *
 * DstCls, Signature of object in the field. *Null/0 for non-model object).
 *
 * Custom, byte/DB:tinyint *Custom
 * CustomId, Guid *Custom
 * Min, long/DB:bigint *Custom
 * Max, long/DB:bigint *Custom
 * Big, bool/DB:bit *Custom
 * UserLabel, String/DB:nvarcher(100) *Custom
 * HelpString, String/DB:nvarcher(100) *Custom
 * ListRootId, int *Custom
 * WsSelector, int *Custom
 * XmlUI, string/DB:ntext *Custom
*/

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// Implementation of the IFwMetaDataCache interface for FdoCache.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	/// <remarks>
	/// We add 'extra' for virtual, virtual generated (back references),
	/// and user created custom fields. This addon fluff should keep us well within
	/// the Int32 Max size of 2,147,483,647, even without having to use
	/// 0 or the negative values.
	/// 1,000,000,000 addon for virtual generated properties
	/// 1,100,000,000 addon for virtual hand made properties.
	/// 2,000,000,000 addon for custom properties.
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(true)]
	internal sealed class FdoMetaDataCache : IFwMetaDataCacheManaged, IFwMetaDataCacheManagedInternal
	{
		#region Data Members for IFwMetaDataCache Support

		private readonly bool m_initialized = true;
		private readonly Dictionary<int, MetaClassRec> m_metaClassRecords = new Dictionary<int, MetaClassRec>();
		private readonly Dictionary<string, int> m_nameToClid = new Dictionary<string, int>();
		private readonly Dictionary<int, MetaFieldRec> m_metaFieldRecords = new Dictionary<int, MetaFieldRec>();
		private readonly Dictionary<string, int> m_nameToFlid = new Dictionary<string, int>();
		private readonly Dictionary<int, int> m_clidToNextCustomFlid = new Dictionary<int, int>();
		private readonly HashSet<MetaFieldRec> m_customFields = new HashSet<MetaFieldRec>();

		#endregion Data Members for IFwMetaDataCache Support

		#region Construction

		/// <summary>
		/// Constructor.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		internal FdoMetaDataCache()
		{
			m_initialized = false;

			// Reset static counters for attrs.
			VirtualPropertyAttribute.ResetFlidCounter();
			FieldDescription.ClearDataAbout();

			var cmObjectTypes = new List<Type>();
			foreach (var fdoType in Assembly.GetExecutingAssembly().GetTypes())
			{
				// TODO-Linux: System.Boolean System.Type::op_Equality(System.Type,System.Type)
				// is marked with [MonoTODO] and might not work as expected in 4.0.
				if (fdoType.Namespace != "SIL.FieldWorks.FDO.DomainImpl"
					|| !fdoType.IsClass
					|| fdoType.GetInterface("ICmObject") == null)
				{
					// Skip irrelvant stuff.
					continue;
				}

				cmObjectTypes.Add(fdoType);
			}
			CmObjectSurrogate.InitializeConstructors(cmObjectTypes);

			InitializeMetaDataCache(cmObjectTypes);

			m_initialized = true;
		}

		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		private void InitializeMetaDataCache(IEnumerable<Type> cmObjectTypes)
		{
			//AddClassesAndProps(cmObjectTypes);
#if !__MonoCS__
			foreach (var fdoType in cmObjectTypes)
			{
				// Cache classes.
				var classAttrs = fdoType.GetCustomAttributes(typeof(ModelClassAttribute), false);
				if (classAttrs == null || classAttrs.Length <= 0)
					continue; // ScrFootnote does not have the 'ModelClassAttribute'.
				//throw new InvalidOperationException("CmObjects must use 'ModelClassAttribute'.");

				// Add its class information.
				AddClass(fdoType.Name,
						 ((ModelClassAttribute)classAttrs[0]).Clsid,
						 fdoType.Name == "CmObject" ? "CmObject" : fdoType.BaseType.Name,
						 fdoType.IsAbstract);
#else
			foreach (var fdoType in cmObjectTypes)
			{
				// Cache classes.
				var classAttrs = fdoType.GetCustomAttributes(typeof(ModelClassAttribute), false);
				if (classAttrs == null || classAttrs.Length <= 0)
					continue; // ScrFootnote does not have the 'ModelClassAttribute'.
				// TODO-Linux: work around for https://bugzilla.novell.com/show_bug.cgi?id=539288
				// Add its class information.
				AddClass1(fdoType.Name,
						 ((ModelClassAttribute)classAttrs[0]).Clsid,
						 fdoType.Name == "CmObject" ? "CmObject" : fdoType.BaseType.Name,
						 fdoType.IsAbstract);
			}

			foreach (var fdoType in cmObjectTypes)
			{
				// Cache classes.
				var classAttrs = fdoType.GetCustomAttributes(typeof(ModelClassAttribute), false);
				if (classAttrs == null || classAttrs.Length <= 0)
					continue; // ScrFootnote does not have the 'ModelClassAttribute'.

				AddClass2(fdoType.Name,
						 ((ModelClassAttribute)classAttrs[0]).Clsid,
						 fdoType.Name == "CmObject" ? "CmObject" : fdoType.BaseType.Name,
						 fdoType.IsAbstract);
#endif

				// Cache properties.
				// Regular foreach loop is faster.
				//PropertyInfo[] pis = fdoType.GetProperties();
				//for (int i = 0; i < pis.Length; ++i)
				//{
				//	PropertyInfo pi = pis[i];
				foreach (var pi in fdoType.GetProperties())
				{
					var decType = pi.DeclaringType;
					// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
					// is marked with [MonoTODO] and might not work as expected in 4.0.
					if (decType != fdoType) continue;

					var customAttributes = pi.GetCustomAttributes(false);
					if (customAttributes.Length <= 0) continue;

					var customAttribute = customAttributes[0];
					int attrFlid;
					string fieldSignature;
					CellarPropertyType flidType;
					FieldSource source;
					switch (customAttribute.GetType().Name)
					{
						default:
							continue;
						case "ModelPropertyAttribute":
							// Add hand made virtual property to MDC.
							var modelAttr = (ModelPropertyAttribute)customAttribute;
							attrFlid = modelAttr.Flid;
							fieldSignature = modelAttr.Signature;
							flidType = modelAttr.FlidType;
							source = FieldSource.kModel;
							break;
						case "VirtualPropertyAttribute":
							// Add hand made virtual property to MDC.
							var vAttr = (VirtualPropertyAttribute)customAttribute;
							attrFlid = vAttr.Flid;
							fieldSignature = vAttr.Signature;
							flidType = vAttr.FlidType;
							source = FieldSource.kVirtual;
							break;
					}

					AddField(fdoType.Name,
							 attrFlid,
							 PlainFieldName(pi.Name),
							 null, null, null,
							 fieldSignature,
							 flidType,
							 source,
							 Guid.Empty, 0);
				}
			}

			// mfr objects have not set their m_dstClsid member.
			// Connect them now.
			foreach (var mfr in m_metaFieldRecords.Values)
				ConnectMetaFieldRec(mfr);
		}

		private void ConnectMetaFieldRec(MetaFieldRec mfr)
		{
			int clid;
			if (mfr.m_sig == null || !m_nameToClid.TryGetValue(mfr.m_sig, out clid)) return;

			SetDestClass(mfr, clid);
			mfr.m_sig = null;
		}

		private void SetDestClass(MetaFieldRec mfr, int clid)
		{
			mfr.m_dstClsid = clid;
			if (!mfr.Virtual)
				m_metaClassRecords[clid].m_incomingFields.Add(mfr);
		}

		// Remove extra fieldname information that is not part of model.
		private static string PlainFieldName(string fieldname)
		{
			if (fieldname.EndsWith("OA") || fieldname.EndsWith("OS") || fieldname.EndsWith("OC")
				|| fieldname.EndsWith("RA") || fieldname.EndsWith("RS") || fieldname.EndsWith("RC"))
			{
				return fieldname.Substring(0, fieldname.Length - 2);
			}
			return fieldname;
		}

		#endregion Construction

		#region IFwMetaDataCache implementation

		#region Initialization methods

		/// <summary>
		///
		/// </summary>
		/// <param name="bstrPathname"></param>
		/// <param name="fClearPrevCache"></param>
		public void InitXml(string bstrPathname, bool fClearPrevCache)
		{
			throw new NotSupportedException("'InitXml' not supported, because this class loads the model itself.");
		}

		#endregion Initialization methods

		#region Field access methods

		/// <summary> Get the source of the field. </summary>
		public FieldSource get_FieldSource(int flid)
		{
			CheckFlid(flid);

			return m_metaFieldRecords[flid].m_fieldSource;
		}

		/// <summary>
		///
		/// </summary>
		public string GetFieldNameOrNull(int flid)
		{
			if (!m_metaFieldRecords.ContainsKey(flid))
				return null;
			return GetFieldName(flid);
		}

		/// <summary>Gets the number of "fields" defined for this conceptual model.</summary>
		/// <returns>Count of fields in the entire model.</returns>
		public int FieldCount
		{
			get { return m_metaFieldRecords.Count; }
		}

		/// <summary>Gets the list of field identification numbers (in no particular order).</summary>
		/// <param name='countOfOutputArray'>The size of the output array.</param>
		/// <param name='flids'>An integer array for returning the field identification numbers.</param>
		/// <remarks>
		/// If the array provided is too small, only an arbitrary subset of cclid values is returned.
		/// If the array provided is too large, the excess entries are set to zero.
		/// To ensure a complete set of field ids is returned, first use the get_FieldCount() method to get the full count.
		/// </remarks>
		public void GetFieldIds(int countOfOutputArray,
								[MarshalAs(UnmanagedType.CustomMarshaler,
									MarshalTypeRef = typeof(ArrayPtrMarshaler),
									SizeParamIndex = 0)] ArrayPtr/*ULONG[]*/ flids)
		{
			var iflid = 0;
			var uIds = new int[countOfOutputArray];
			foreach (var flid in GetFieldIds())
			{
				if (iflid == countOfOutputArray)
					break;
				uIds[iflid++] = flid;
			}
			MarshalEx.ArrayToNative(flids, countOfOutputArray, uIds);
		}

		/// <summary>Gets the name of the class that contains this field.</summary>
		/// <param name='flid'>Field identification number.</param>
		public string GetOwnClsName(int flid)
		{
			CheckFlid(flid);

			return m_metaClassRecords[m_metaFieldRecords[flid].m_ownClsid].m_className;
		}

		/// <summary>
		/// Gets the name of the destination class that corresponds to this field.
		/// This is the name of the class that is either owned or referred to by another class.
		/// </summary>
		/// <param name='flid'>Field identification number.</param>
		public string GetDstClsName(int flid)
		{
			CheckFlid(flid);

			return m_metaClassRecords[m_metaFieldRecords[flid].m_dstClsid].m_className;
		}

		/// <summary>Gets the "Id" value of the class that contains this field.</summary>
		/// <param name='flid'>Field identification number.</param>
		public int GetOwnClsId(int flid)
		{
			CheckFlid(flid);

			return m_metaFieldRecords[flid].m_ownClsid;
		}

		/// <summary>Gets the "Id" of the destination class that corresponds to this field.</summary>
		/// <param name='flid'>Field identification number.</param>
		public int GetDstClsId(int flid)
		{
			CheckFlid(flid);

			return m_metaFieldRecords[flid].m_dstClsid;
		}

		/// <summary>Gets the name of a field.</summary>
		/// <param name='flid'>Field identification number.</param>
		public string GetFieldName(int flid)
		{
			CheckFlid(flid);

			return m_metaFieldRecords[flid].m_fieldName;
		}

		/// <summary>Gets the user label of a field.</summary>
		/// <param name='flid'>Field identification number.</param>
		public string GetFieldLabel(int flid)
		{
			CheckFlid(flid);

			return m_metaFieldRecords[flid].m_fieldLabel;
		}

		/// <summary>Gets the help string of a field.p</summary>
		/// <param name='flid'>Field identification number.</param>
		public string GetFieldHelp(int flid)
		{
			CheckFlid(flid);

			return m_metaFieldRecords[flid].m_fieldHelp;
		}

		/// <summary>Gets the Xml UI of a field.</summary>
		/// <param name='flid'>Field identification number.</param>
		public string GetFieldXml(int flid)
		{
			CheckFlid(flid);

			return m_metaFieldRecords[flid].m_fieldXml;
		}

		/// <summary>
		/// Gets the GUID of the CmPossibilityList associated with this field. This is only used by
		/// custom fields.
		/// </summary>
		/// <param name="flid">The flid.</param>
		/// <returns></returns>
		public Guid GetFieldListRoot(int flid)
		{
			CheckFlid(flid);

			return m_metaFieldRecords[flid].m_fieldListRoot;
		}

		/// <summary>Gets the Ws of the field.</summary>
		/// <param name='flid'>Field identification number.</param>
		public int GetFieldWs(int flid)
		{
			CheckFlid(flid);

			return m_metaFieldRecords[flid].m_fieldWs;
		}

		/// <summary>
		/// Gets the type of the field. If the field is unknown, return zero.
		/// </summary>
		/// <param name='flid'>Field identification number.</param>
		/// <remarks>
		/// This type value indicates if the field is a primitive data type
		/// or a MultiStr/MultiTxt value or describes the relationship
		/// between two classes (i.e. owning/reference and atomic/collection/sequence).
		/// These numeric values are defined in the 'FWROOT\Src\Cellar\lib\CmTypes.h' file.
		/// </remarks>
		public int GetFieldType(int flid)
		{
			CheckFlid(flid);

			return (int)m_metaFieldRecords[flid].m_fieldType;
		}

		/// <summary>
		/// Given a field id and a class id,
		/// this returns true if it is legal to store this class of object
		/// (or any of its superclasses) in the field.
		/// </summary>
		/// <param name='flid'>Field identification number.</param>
		/// <param name='clid'>Class identification number.</param>
		/// <returns>A System.Boolean</returns>
		public bool get_IsValidClass(int flid, int clid)
		{
			CheckClid(clid);
			CheckFlid(flid);

			var mfr = m_metaFieldRecords[flid];
			if (mfr.m_dstClsid == clid)
				return IsObjectFieldType(mfr.m_fieldType);

			// Check superclasses.
			do
			{
				var mcr = m_metaClassRecords[clid];
				clid = mcr.m_baseClsid;
				if (mfr.m_dstClsid == clid)
					return IsObjectFieldType(mfr.m_fieldType);
			} while (clid != 0);

			return false;
		}

		#endregion Field access methods

		#region Class access methods

		/// <summary>Gets the number of "classes" defined for this conceptual model.</summary>
		/// <returns>Count of classes.</returns>
		public int ClassCount
		{
			get { return m_metaClassRecords.Count; }
		}

		/// <summary>
		/// Gets the list of class identification numbers (in no particular order).
		/// </summary>
		/// <param name='arraySize'>The size of the output array.</param>
		/// <param name='clids'>An integer array for returning the class identification numbers.</param>
		/// <remarks>
		/// If the array provided is too small, only an arbitrary subset of clid values is returned.
		/// If the array provided is too large, the excess entries are set to zero.
		/// </remarks>
		public void GetClassIds(int arraySize,
								[MarshalAs(UnmanagedType.CustomMarshaler,
									MarshalTypeRef = typeof(ArrayPtrMarshaler),
									SizeParamIndex = 0)] ArrayPtr/*ULONG[]*/ clids)
		{
			var iclid = 0;
			var uIds = new int[arraySize];
			foreach (var clid in GetClassIds())
			{
				if (iclid == arraySize)
					break;
				uIds[iclid++] = clid;
			}
			MarshalEx.ArrayToNative(clids, arraySize, uIds);
		}

		/// <summary>Gets the name of the class.</summary>
		/// <param name='clid'>Class identification number.</param>
		public string GetClassName(int clid)
		{
			CheckClid(clid);

			return m_metaClassRecords[clid].m_className;
		}

		/// <summary>Indicates whether a class is abstract or concrete.</summary>
		/// <param name='clid'>Class identification number.</param>
		public bool GetAbstract(int clid)
		{
			CheckClid(clid);

			return m_metaClassRecords[clid].m_abstract;
		}

		/// <summary>Gets the base class id for a given class.</summary>
		/// <param name='clid'>Class identification number.</param>
		public int GetBaseClsId(int clid)
		{
			if (clid == 0)
				throw new ArgumentException("CmObject has no base class.");
			CheckClid(clid);

			return m_metaClassRecords[clid].m_baseClsid;
		}

		/// <summary>Gets the name of the base class for a given class.</summary>
		/// <param name='clid'>Class identification number.</param>
		public string GetBaseClsName(int clid)
		{
			if (clid == 0)
				throw new ArgumentException("CmObject has no base class.");
			CheckClid(clid);

			return m_metaClassRecords[m_metaClassRecords[clid].m_baseClsid].m_className;
		}

		/// <summary>
		/// Gets a list of the fields for the specified class.
		/// Fields of superclasses are also returned, if the relevant flag is true.
		/// </summary>
		/// <param name='clid'>Class identification number.</param>
		/// <param name='includeSuperclasses'>'True' to also get superclass fields.</param>
		/// <param name='fieldTypes'>
		/// Gets all fields whose types match the specified argument,
		/// which should be a combination of the fcpt values defined in CmTypes.h, e.g.,
		/// to get all owning properties pass kfcptOwningCollection | kfcptOwningAtom | kfcptOwningSequence.
		/// </param>
		/// <param name='countFlidMax'>
		/// Size of the 'flids' array.
		/// (Use 0 to get the size to use in a second call to actually get them.)</param>
		/// <param name='flids'>Array of flids.</param>
		/// <returns>
		/// Count of flids that are returned,
		/// or that could be returned, if 'countFlidMax' is 0.</returns>
		/// <exception cref="ArgumentException">
		/// Thrown if the output array is too small.
		/// </exception>
		public int GetFields(int clid, bool includeSuperclasses, int fieldTypes, int countFlidMax,
							 [MarshalAs(UnmanagedType.CustomMarshaler,
								MarshalTypeRef = typeof(ArrayPtrMarshaler),
								SizeParamIndex = 3)] ArrayPtr/*ULONG[]*/ flids)
		{
			CheckClid(clid);

			var countFlids = 0;
			var iflid = 0;
			var uIds = new int[countFlidMax];

			foreach (var flid in GetFields(clid, includeSuperclasses, fieldTypes))
			{
				var mfr = m_metaFieldRecords[flid];
				if (fieldTypes != (int)CellarPropertyTypeFilter.All)
				{
					// Look up field type and see if it matches
					var flidType = mfr.m_fieldType;
					var fcpt = 1 << (int)flidType;
					if ((fieldTypes & fcpt) == 0)
						continue; // don't return this one
				}
				countFlids++;
				if (countFlidMax <= 0) continue;

				if (countFlids > countFlidMax)
					throw new ArgumentException("Output array is too small.", "countFlidMax");
				uIds[iflid++] = flid;
			}
			if (iflid > 0)
				MarshalEx.ArrayToNative(flids, countFlidMax, uIds);

			return countFlids;
		}

		#endregion Class access methods

		#region Reverse access methods

		/// <summary>Get the ID of the class having the specified name.</summary>
		/// <param name='className'>Name of the class to look for.</param>
		/// <returns>Class identification number, or 0 if not found.</returns>
		public int GetClassId(string className)
		{
			CheckClid(className);

			return m_nameToClid[className];
		}

		/// <summary>
		/// Return true if the specified class exists.
		/// </summary>
		public bool ClassExists(string className)
		{
			return m_nameToClid.ContainsKey(className);
		}

		/// <summary>
		/// Return true if the specified field (and class) exist.
		/// </summary>
		public bool FieldExists(string className, string fieldName, bool includeBaseClasses)
		{
			if (!ClassExists(className))
				return false;
			return FieldExists(GetClassId(className), fieldName, includeBaseClasses);
		}

		/// <summary>
		/// Return true if the specified field (and class) exist.
		/// </summary>
		public bool FieldExists(int clid, string fieldName, bool includeBaseClasses)
		{
			if (!m_metaClassRecords.ContainsKey(clid))
				return false;
			return GetFlid(clid, fieldName, includeBaseClasses) != 0;
		}

		/// <summary>
		/// Return true if the specified field exists.
		/// </summary>
		public bool FieldExists(int flid)
		{
			return m_metaFieldRecords.ContainsKey(flid);
		}

		/// <summary>Gets the field ID given the class and field names (throws if invalid).</summary>
		/// <param name='className'>Name of the class that should contain the fieldname.</param>
		/// <param name='fieldName'>FieldName to look for.</param>
		/// <param name='includeBaseClasses'>'True' to look in superclasses,
		/// otherwise 'false' to just look in the given class.</param>
		/// <returns>Field identification number (throws if invalid)</returns>
		public int GetFieldId(string className, string fieldName, bool includeBaseClasses)
		{
			return GetFieldId2(GetClassId(className), fieldName, includeBaseClasses);
		}

		/// <summary>Gets the field ID given the class ID and field name (throws if invalid).</summary>
		/// <param name='clid'>ID  of the class that should contain the fieldname.</param>
		/// <param name='fieldName'>FieldName to look for.</param>
		/// <param name='includeBaseClasses'>'True' to look in superclasses,
		/// otherwise 'false' to just look in the given class.</param>
		/// <returns>Field identification number (throws if invalid).</returns>
		public int GetFieldId2(int clid, string fieldName, bool includeBaseClasses)
		{
			CheckClid(clid);

			int flid = GetFlid(clid, fieldName, includeBaseClasses);

			if (flid == 0)
				throw new FDOInvalidFieldException("Fieldname '" + fieldName + "' does not exist. Consider using a decorator or review the fieldname for typos.");
			return flid;
		}

		private int GetFlid(int clid, string fieldName, bool includeBaseClasses)
		{
			int flid; // Start on the pessimistic side.
			var flidKey = MakeFlidKey(clid, fieldName);
			if (!m_nameToFlid.TryGetValue(flidKey, out flid))
			{
				if (includeBaseClasses && clid > 0)
					flid = GetFlid(GetBaseClsId(clid), fieldName, true);
			}
			return flid;
		}

		/// <summary>Gets the direct subclasses of the given class (not including itself).</summary>
		/// <param name='clid'>Class indentifier to work with.</param>
		/// <param name='countMaximumToReturn'>Count of the maximum number of subclass IDs to return (Size of the array.)
		/// When set to zero, countDirectSubclasses will contain the full count, so a second call can use the right sized array.</param>
		/// <param name='countDirectSubclasses'>Count of how many subclass IDs are the output array.</param>
		/// <param name='subclasses'>Array of subclass IDs.</param>
		public void GetDirectSubclasses(int clid,
										int countMaximumToReturn, out int countDirectSubclasses,
										[MarshalAs(UnmanagedType.CustomMarshaler,
											MarshalTypeRef = typeof(ArrayPtrMarshaler),
											SizeParamIndex = 1)] ArrayPtr/*int[]*/ subclasses)
		{
			CheckClid(clid);

			var mcr = m_metaClassRecords[clid];
			countDirectSubclasses = mcr.m_directSubclasses.Count;

			if (countMaximumToReturn == 0)
				return; // Client only wanted the count.

			if (countMaximumToReturn < countDirectSubclasses)
				throw new ArgumentException("Output array is too small.", "countMaximumToReturn");

			MarshalEx.ArrayToNative(subclasses,
									countMaximumToReturn,
									GetDirectSubclasses(clid));
		}

		/// <summary>
		/// Gets all subclasses of the given class,
		/// including itself (which is always the first result in the list,
		/// so it can easily be skipped if desired).
		/// </summary>
		/// <param name='clid'>Class indentifier to work with.</param>
		/// <param name='countMaximumToReturn'>Count of the maximum number of subclass IDs to return (Size of the array.)
		/// When set to zero, countDirectSubclasses will contain the full count, so a second call can use the right sized array.</param>
		/// <param name='countAllSubclasses'>Count of how many subclass IDs are the output array.</param>
		/// <param name='subclasses'>Array of subclass IDs.</param>
		/// <remarks>
		/// The list is therefore a complete list of the classes which are valid to store in a property whose
		/// signature is the class identified by 'clid'.
		/// </remarks>
		public void GetAllSubclasses(int clid,
									 int countMaximumToReturn, out int countAllSubclasses,
									 [MarshalAs(UnmanagedType.CustomMarshaler,
										MarshalTypeRef = typeof(ArrayPtrMarshaler),
										SizeParamIndex = 1)] ArrayPtr/*int[]*/ subclasses)
		{
			CheckClid(clid);

			countAllSubclasses = 0; // Start with 0 for output parameter.
			var allSubclassClids = GetAllSubclasses(clid);
			var uIds = new int[countMaximumToReturn];
			var iSubclassClid = 0;
			var countAllSubclassesActual = allSubclassClids.Length;
			while (iSubclassClid < countMaximumToReturn && iSubclassClid < countAllSubclassesActual)
			{
				uIds[iSubclassClid] = allSubclassClids[iSubclassClid];
				++iSubclassClid;
				countAllSubclasses++;
			}
			MarshalEx.ArrayToNative(subclasses, countMaximumToReturn, uIds);
		}

		#endregion Reverse access methods

		#region Virtual access methods

		/// <summary>Add a virtual property (field) to a class.</summary>
		/// <param name='className'>Name of the class that gets the new virtual property</param>
		/// <param name='fieldName'>Name of the new virtual Field.</param>
		/// <param name='virtualFlid'>Field identifier for the enw virtual field</param>
		/// <param name='fieldType'>
		/// This type value indicates if the field is a primitive data type
		/// or a MultiStr/MultiBigStr/MultiTxt/MultiBigTxt value or describes the relationship
		/// between two classes (i.e. owning/reference and atomic/collection/sequence).
		/// These numeric values are defined in the 'FWROOT\src\cellar\lib\CmTypes.h' file.
		/// It must NOT have the virtual bit OR'd in.
		/// </param>
		public void AddVirtualProp(string className, string fieldName, int virtualFlid, int fieldType)
		{
			throw new NotSupportedException("'AddVirtualProp' not supported, because this class loads the model itself.");
		}

		/// <summary>Check if the given flid is virtual or regular.</summary>
		/// <param name='flid'>Field identifier to check.</param>
		/// <returns>'True' if 'flid' is a virtual field, otherwise 'false'. </returns>
		public bool get_IsVirtual(int flid)
		{
			CheckFlid(flid);

			return m_metaFieldRecords[flid].Virtual;
		}

		#endregion Virtual access methods

		#endregion IFwMetaDataCache implementation

		#region Managed extentions to IFwMetaDataCache interface

		/// <summary>
		/// Gets the list of field identification numbers (in no particular order).
		/// </summary>
		/// <remarks>
		/// This is a non-COM version of GetFieldIds. Managed clients should cast 'this'
		/// into FdoMetaDataCache, and use this method.
		/// </remarks>
		[ComVisible(false)]
		public int[] GetFieldIds()
		{
			var uIds = new int[m_nameToFlid.Values.Count];
			m_nameToFlid.Values.CopyTo(uIds, 0);
			return uIds;
		}

		/// <summary>
		/// Gets the list of class identification numbers (in no particular order).
		/// </summary>
		[ComVisible(false)]
		public int[] GetClassIds()
		{
			var clids = new HashSet<int>();
			foreach (var clid in m_nameToClid.Values)
				clids.Add(clid);

			return clids.ToArray();
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
		public int[] GetFields(int clid, bool includeSuperclasses, int fieldTypes)
		{
			CheckClid(clid);

			var currentClid = clid;
			var uFlids = new HashSet<int>();
			// This loop executes once if fIncludeSuperclasses is false, otherwise over clid
			// and all superclasses.
			for (; ; )
			{
				var mcr = m_metaClassRecords[currentClid];
				var cnt = mcr.m_uFields.Count;
				for (var i = 0; i < cnt; ++i)
				{
					var mfr = mcr.m_uFields[i];
					if (!IsMatchingFieldType(fieldTypes, mfr))
						continue;
					uFlids.Add(mfr.m_flid);
				}

				if (!includeSuperclasses)
					break;
				if (currentClid == 0) // just processed CmObject
					break;
				currentClid = mcr.m_baseClsid;
			}

			return uFlids.ToArray();
		}

		private static bool IsMatchingFieldType(int fieldTypes, MetaFieldRec mfr)
		{
			if (fieldTypes != (int)CellarPropertyTypeFilter.All)
			{
				// Look up field type and see if it matches
				var flidType = mfr.m_fieldType;
				var fcpt = 1 << (int)flidType;
				if ((fieldTypes & fcpt) == 0)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Get the class Ids of the direct subclasses of the specified class Id.
		/// </summary>
		/// <param name="clid">Class Id to get the subclass Ids of.</param>
		/// <returns>An array of direct subclass class Ids.</returns>
		[ComVisible(false)]
		public int[] GetDirectSubclasses(int clid)
		{
			CheckClid(clid);

			var mcr = m_metaClassRecords[clid];
			var cnt = mcr.m_directSubclasses.Count;
			var clids = new HashSet<int>();
			for (var i = 0; i < cnt; ++i)
				clids.Add(mcr.m_directSubclasses[i].m_clid);

			return clids.ToArray();
		}

		/// <summary>
		/// Get all of the subclass Ids, including the given class Id.
		/// </summary>
		/// <param name="clid">Class Id to get subclass Ids of.</param>
		/// <returns></returns>
		[ComVisible(false)]
		public int[] GetAllSubclasses(int clid)
		{
			CheckClid(clid);

			var allSubclassClids = new HashSet<int> { clid };

			GetAllSubclassesForClid(clid, allSubclassClids);

			return allSubclassClids.ToArray();
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
		public int AddCustomField(string className, string fieldName, CellarPropertyType fieldType, int destinationClass)
		{
			CheckClid(className);
			var clid = m_nameToClid[className];

			if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException("fieldName");
			int flid;

			// Don't allow custom field with same name as non-custom field
			if (m_nameToFlid.TryGetValue(MakeFlidKey(clid, fieldName), out flid) && !IsCustom(flid))
				throw new FDOInvalidFieldException("Field already exists: " + fieldName, fieldName);

			// FWR-2804 - in case a custom field already exists that originally had this name,
			// make sure we use a unique name this time.
			var uniqueName = AssureUniqueFieldName(clid, fieldName);

			// Get custom flid for clid.
			if (m_clidToNextCustomFlid.TryGetValue(clid, out flid))
			{
				// Bump it up by 1.
				flid += 1;
				m_clidToNextCustomFlid[clid] = flid;
			}
			else
			{
				// Start with 500.
				flid = (clid*1000) + 500;
				m_clidToNextCustomFlid.Add(clid, flid);
			}
			string fieldSig = null;
			MetaClassRec mcrSig;
			if (m_metaClassRecords.TryGetValue(destinationClass, out mcrSig))
				fieldSig = mcrSig.m_className;
			AddField(className,
					 flid, uniqueName,
					 null, null, null,
					 fieldSig,
					 fieldType, FieldSource.kCustom, Guid.Empty, 0);

			var mfr = m_metaFieldRecords[flid];
			mfr.m_fieldLabel = fieldName;		// user label is original proposed name.
			m_customFields.Add(mfr);
			ConnectMetaFieldRec(mfr);

			return flid;
		}

		private string AssureUniqueFieldName(int clid, string proposedName)
		{
			// Handles case where user changed another field's userlabel and now wants to reuse
			// the old name for a new field.
			var result = proposedName;
			var defaultLabel = result;
			var extraId = 0;
			while (!IsFieldNameUnique(clid, result))
			{
				extraId++;
				result = defaultLabel + extraId;
			}
			return result;
		}

		private bool IsFieldNameUnique(int clid, string proposedName)
		{
			int dummyFlid;
			return !m_nameToFlid.TryGetValue(MakeFlidKey(clid, proposedName), out dummyFlid);
		}

		/// <summary>Check if the given flid is Custom, or not.</summary>
		/// <param name='flid'>Field identifier to check.</param>
		/// <returns>'True' if 'flid' is a custom field, otherwise 'false'. </returns>
		[ComVisible(false)]
		public bool IsCustom(int flid)
		{
			CheckFlid(flid);

			return m_metaFieldRecords[flid].m_fieldSource == FieldSource.kCustom;
		}


		/// <summary>
		/// If the given field exists and is a custom field, delete it.
		/// </summary>
		/// <exception cref="FDOInvalidFieldException">
		/// Thrown if flid is not a part of the model.
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
			CheckFlid(flid);

			var mfr = m_metaFieldRecords[flid];
			if (mfr.m_fieldSource != FieldSource.kCustom)
				throw new ArgumentException(String.Format("{0} is not a custom field!", flid));
			m_customFields.Remove(mfr);
			var key = MakeFlidKey(mfr.m_ownClsid, mfr.m_fieldName);
			m_nameToFlid.Remove(key);
			m_metaFieldRecords.Remove(flid);
			var mcr = m_metaClassRecords[mfr.m_ownClsid];
			mcr.m_uFields.Remove(mfr);
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
		///
		/// If 'fieldType' is an object type (owning/reference and atomic/collection/sequence) property,
		/// then 'destinationClass' must match a class in the model.
		/// </remarks>
		[ComVisible(false)]
		public int AddCustomField(string className, string fieldName, CellarPropertyType fieldType,
			int destinationClass, string fieldHelp, int fieldWs, Guid fieldListRoot)
		{
			return AddCustomField(className, fieldName, fieldName, fieldType, destinationClass, fieldHelp, fieldWs, fieldListRoot);
		}

		/// <summary>
		/// This overload allows the label to be specified separately.
		/// </summary>
		public int AddCustomField(string className, string fieldName, string label, CellarPropertyType fieldType,
			int destinationClass, string fieldHelp, int fieldWs, Guid fieldListRoot)
		{
			var flid = AddCustomField(className, fieldName, fieldType, destinationClass);
			m_metaFieldRecords[flid].m_fieldHelp = fieldHelp;
			m_metaFieldRecords[flid].m_fieldWs = fieldWs;
			m_metaFieldRecords[flid].m_fieldLabel = label;	// user label same as field name now.
			m_metaFieldRecords[flid].m_fieldListRoot = fieldListRoot;
			return flid;
		}

		/// <summary>
		/// Update a user-defined custom field.
		/// </summary>
		/// <param name="flid">field id number</param>
		/// <param name="fieldHelp">help string for the field</param>
		/// <param name="fieldWs">writing system selector for the field</param>
		/// <param name="userLabel">label chosen by user (starts out same as name)</param>
		/// <exception cref="FDOInvalidFieldException">
		/// Thrown if flid is not in the model at all.  (Even user-defined custom fields are in the model.)
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown if flid is not a custom field.
		/// </exception>
		[ComVisible(false)]
		public void UpdateCustomField(int flid, string fieldHelp, int fieldWs, string userLabel)
		{
			CheckFlid(flid);

			if (m_metaFieldRecords[flid].m_fieldSource != FieldSource.kCustom)
				throw new ArgumentException(String.Format("{0} is not a custom field!", flid));
			m_metaFieldRecords[flid].m_fieldHelp = fieldHelp;
			m_metaFieldRecords[flid].m_fieldWs = fieldWs;
			m_metaFieldRecords[flid].m_fieldLabel = userLabel;
		}

		/// <summary>
		/// Get (non-virtual) fields that have the specified class or a base class as their signature.
		/// </summary>
		public IEnumerable<int> GetIncomingFields(int clid, int fieldTypes)
		{
			int currentClid = clid;
			for ( ; ;)
			{
				var metaClassRec = m_metaClassRecords[currentClid];
				foreach (MetaFieldRec mfr in metaClassRec.m_incomingFields)
				{
					if (IsMatchingFieldType(fieldTypes, mfr))
						yield return mfr.m_flid;
				}
				if (currentClid == 0)
					break; // yes, we DO have some properties with signature CmObject, at least for now.
				currentClid = metaClassRec.m_baseClsid;
			}
		}

		#endregion Managed extentions to IFwMetaDataCache interface

		#region Implementation of IFwMetaDataCacheManagedInternal

		/// <summary>
		/// Add the persisted custom fields (only from a BEP).
		/// </summary>
		/// <param name="customFields"></param>
		void IFwMetaDataCacheManagedInternal.AddCustomFields(IEnumerable<CustomFieldInfo> customFields)
		{
			if (customFields == null) throw new ArgumentNullException("customFields");

			foreach (var customField in customFields)
			{
				AddCustomField(customField.m_classname, customField.m_fieldname, customField.Label, customField.m_fieldType,
							   customField.m_destinationClass, customField.m_fieldHelp, customField.m_fieldWs,
							   customField.m_fieldListRoot);
			}
		}

		/// <summary>
		/// Get the custom fields
		/// </summary>
		/// <returns></returns>
		IEnumerable<CustomFieldInfo> IFwMetaDataCacheManagedInternal.GetCustomFields()
		{
			var retval = new List<CustomFieldInfo>();
			foreach (var customMfr in m_customFields)
			{
				var cfi = new CustomFieldInfo
							{
								m_classid = customMfr.m_ownClsid,
								m_classname = m_metaClassRecords[customMfr.m_ownClsid].m_className,
								m_fieldname = customMfr.m_fieldName,
								m_fieldType = (CellarPropertyType) customMfr.m_fieldType,
								m_flid = customMfr.m_flid,
								m_destinationClass = customMfr.m_dstClsid,
								m_fieldWs = customMfr.m_fieldWs,
								m_fieldHelp = customMfr.m_fieldHelp,
								m_fieldListRoot = customMfr.m_fieldListRoot,
								Label = customMfr.m_fieldLabel
							};
				retval.Add(cfi);
			}
			return retval;
		}

		/// <summary>
		/// Get properties that can be sorted (e.g., collection and multi str/uni properties).
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, Dictionary<string, HashSet<string>>> GetSortableProperties()
		{
			var result = new Dictionary<string, Dictionary<string, HashSet<string>>>(300, StringComparer.OrdinalIgnoreCase);
			foreach (var concreteClassId in GetClassIds().Where(concreteClassId => !GetAbstract(concreteClassId)))
			{
				var propData = new Dictionary<string, HashSet<string>>(3, StringComparer.OrdinalIgnoreCase);
				result.Add(GetClassName(concreteClassId), propData);

				var collData = new HashSet<string>();
				propData.Add("Collections", collData);
				var multiAltData = new HashSet<string>();
				propData.Add("MultiAlt", multiAltData);

				foreach (var propId in GetFields(concreteClassId, true, (int)CellarPropertyTypeFilter.All))
				{
					var fieldType = (CellarPropertyType)GetFieldType(propId);
					switch (fieldType)
					{
						case CellarPropertyType.OwningCollection:
						case CellarPropertyType.ReferenceCollection:
							collData.Add(GetFieldName(propId));
							break;
						case CellarPropertyType.MultiString:
						case CellarPropertyType.MultiUnicode:
							multiAltData.Add(GetFieldName(propId));
							break;
					}
				}
			}
			return result;
		}

		#endregion

		#region Helper classes, enums, and methods

		#region IFwMetaDataCache helper methods

#if !__MonoCS__
		/// <summary> Add a class to the MetaDataCache. </summary>
		/// <param name='className'> </param>
		/// <param name='clid'> </param>
		/// <param name='superclassName'> </param>
		/// <param name='isAbstract'> </param>
		private void AddClass(string className, int clid, string superclassName, bool isAbstract)
		{
			var mcr = new MetaClassRec(superclassName, isAbstract, className) {m_clid = clid};

			// These will throw if it is already in the dictionary.
			m_metaClassRecords.Add(clid, mcr);
			m_nameToClid.Add(mcr.m_className, clid);

			// Set superclass info, except for top CmObject superclass.
			if (className == "CmObject") return;

			mcr.m_baseClsid = m_nameToClid[mcr.m_superclassName];
			var mcrSuperclass = m_metaClassRecords[mcr.m_baseClsid];
			mcrSuperclass.m_directSubclasses.Add(mcr);
		}
#else // TODO-Linux: workaround for https://bugzilla.novell.com/show_bug.cgi?id=539288

		/// <summary> Add a class to the MetaDataCache. </summary>
		/// <param name='bstrClassName'> </param>
		/// <param name='luClid'> </param>
		/// <param name='bstrSuperclassName'> </param>
		/// <param name='isAbstract'> </param>
		private void AddClass1(string bstrClassName, int luClid, string bstrSuperclassName, bool isAbstract)
		{
			var mcr = new MetaClassRec(bstrSuperclassName, isAbstract, bstrClassName) {m_clid = luClid};

			// These will throw if it is already in the dictionary.
			m_metaClassRecords.Add(luClid, mcr);
			m_nameToClid.Add(mcr.m_className, luClid);
		}

		private void AddClass2(string bstrClassName, int luClid, string bstrSuperclassName, bool isAbstract)
		{
			var mcr = m_metaClassRecords[luClid];

			// Set superclass info, except for top CmObject superclass.
			if (bstrClassName == "CmObject") return;

			mcr.m_baseClsid = m_nameToClid[mcr.m_superclassName];
			var mcrSuperclass = m_metaClassRecords[mcr.m_baseClsid];
			mcrSuperclass.m_directSubclasses.Add(mcr);
		}
#endif

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Add a field to the MetaDataCache.
		/// </summary>
		/// <param name="className">Name of the class.</param>
		/// <param name="flid">The flid.</param>
		/// <param name="fieldName">Name of the field.</param>
		/// <param name="fieldLabel">The field label.</param>
		/// <param name="fieldHelp">The field help.</param>
		/// <param name="fieldXml">The field XML.</param>
		/// <param name="fieldSignature">The field signature.</param>
		/// <param name="type">The type.</param>
		/// <param name="fsSource">The fs source.</param>
		/// <param name="fieldListRoot">The field list root.</param>
		/// <param name="fieldWs">The field ws.</param>
		/// --------------------------------------------------------------------------------
		private void AddField(string className, int flid, string fieldName, string fieldLabel,
			string fieldHelp, string fieldXml, string fieldSignature, CellarPropertyType type,
			FieldSource fsSource, Guid fieldListRoot, int fieldWs)
		{
			// Will throw if class is not in one or the other Dictionaries.
			var clid = m_nameToClid[className];
			var mcr = m_metaClassRecords[clid];
			var mfr = new MetaFieldRec
						{
							m_flid = flid,
							m_fieldLabel = fieldLabel,
							m_fieldHelp = fieldHelp,
							m_fieldXml = fieldXml,
							m_fieldListRoot = fieldListRoot,
							m_fieldWs = fieldWs,
							m_sig = null,
							m_fieldSource = fsSource
						};
			switch (type)
			{
				default:
					break;
				case CellarPropertyType.OwningAtomic: // Fall through
				case CellarPropertyType.OwningCollection: // Fall through
				case CellarPropertyType.OwningSequence: // Fall through
				case CellarPropertyType.ReferenceAtomic: // Fall through
				case CellarPropertyType.ReferenceCollection: // Fall through
				case CellarPropertyType.ReferenceSequence:
					mfr.m_sig = fieldSignature;
					// It may not be present yet when the whole MDC is being initialized via 'Init',
					// or the Constructor, as the case may be.
					// Only mess with setting this after intitialization of the main model.
					// Once all those model classes are in, a client can take his lumps
					// if this throws an exception.
					if (m_initialized)
					{
						if (string.IsNullOrEmpty(fieldSignature))
							throw new KeyNotFoundException("'bstrFieldSignature' is a null or empty key.");
						// Note that m_fieldSource must be set before calling this.
						SetDestClass(mfr, m_nameToClid[fieldSignature]);
					}
					break;
			}
			mfr.m_fieldType = type;
			mfr.m_fieldName = fieldName;
			mfr.m_ownClsid = clid;
			mcr.AddField(mfr);
			m_metaFieldRecords[flid] = mfr;
			m_nameToFlid[MakeFlidKey(clid, fieldName)] = flid;
		}

		/// <summary>
		/// Set of all basic (C# value types) data types.
		/// </summary>
		/// <remarks>
		/// Float and Numeric are not used in the model as of 23 March 2013.
		/// </remarks>
		public static readonly HashSet<CellarPropertyType> PropertyTypesForValueTypeData = new HashSet<CellarPropertyType>()
		{
			CellarPropertyType.Boolean,
			CellarPropertyType.GenDate,
			CellarPropertyType.Guid,
			CellarPropertyType.Integer,
			CellarPropertyType.Float,
			CellarPropertyType.Numeric,
			CellarPropertyType.Time
		};

		/// <summary>
		/// Returns true for Boolean, GenDate, Guid, Integer, Float, Numeric, and Time
		/// </summary>
		/// <param name="type"></param>
		/// <remarks>
		/// Float and Numeric are not used in the model, as of 23 March 2013.
		/// </remarks>
		/// <returns></returns>
		public bool IsValueType(CellarPropertyType type)
		{
			return PropertyTypesForValueTypeData.Contains(type);
		}

		private static bool IsObjectFieldType(CellarPropertyType type)
		{
			var isObjectFieldType = false;

			switch (type)
			{
				case CellarPropertyType.OwningAtomic:
				case CellarPropertyType.OwningCollection:
				case CellarPropertyType.OwningSequence:
				case CellarPropertyType.ReferenceAtomic:
				case CellarPropertyType.ReferenceCollection:
				case CellarPropertyType.ReferenceSequence:
					isObjectFieldType = true;
					break;
			}

			return isObjectFieldType;
		}

		private string MakeFlidKey(int clid, string fieldname)
		{
			CheckClid(clid);

			// Concatenate is faster than Format call here.
			//return String.Format("{0}{1}{2}", (clid >> 16), (clid & 0xffff), fieldname);

			// SB is a bit slower.
			//StringBuilder sb = new StringBuilder((clid >> 16).ToString(), 1000);
			//sb.Append((clid & 0xffff).ToString());
			//sb.Append(fieldname);
			//return sb.ToString();

			//return (clid >> 16).ToString() + (clid & 0xffff).ToString() + fieldname;

			return clid + fieldname;
		}

		private void CheckFlid(int flid)
		{
			if (!m_metaFieldRecords.ContainsKey(flid))
				throw new FDOInvalidFieldException("Invalid field identifier: " + flid, "flid");
		}

		private void CheckClid(int clid)
		{
			if (!m_metaClassRecords.ContainsKey(clid))
				throw new FDOInvalidClassException("Invalid class identifier: " + clid, "clid");
		}

		private void CheckClid(string className)
		{
			if (!m_nameToClid.ContainsKey(className))
				throw new FDOInvalidClassException("Invalid class name: " + className, className);
		}

		private void GetAllSubclassesForClid(int clid, ICollection<int> allSubclassClids)
		{
			CheckClid(clid);

			var mcr = m_metaClassRecords[clid];
			var cnt = mcr.m_directSubclasses.Count;
			for (var i = 0; i < cnt; ++i)
			{
				var subClassClid = mcr.m_directSubclasses[i].m_clid;
				allSubclassClids.Add(subClassClid);
				GetAllSubclassesForClid(subClassClid, allSubclassClids);
			}
		}

		#endregion IFwMetaDataCache helper methods

		#region MetaClassRec helper class

		private class MetaClassRec
		{
			internal int m_clid;
			internal readonly string m_superclassName;
			internal int m_baseClsid; // Superclass
			internal readonly bool m_abstract;
			internal readonly string m_className;
			internal readonly List<MetaFieldRec> m_uFields = new List<MetaFieldRec>();
			internal readonly List<MetaClassRec> m_directSubclasses = new List<MetaClassRec>(); // collection of class recs.
			// non-virtua fields that have this class as their destination class. Can only be object properties.
			internal readonly List<MetaFieldRec> m_incomingFields = new List<MetaFieldRec>();

			internal MetaClassRec(string superclassName, bool isAbstract, string className)
			{
				m_superclassName = superclassName;
				m_abstract = isAbstract;
				m_className = className;
			}

			internal void AddField(MetaFieldRec mfr)
			{
				m_uFields.Add(mfr);
			}
		}

		#endregion MetaClassRec helper class

		#region MetaFieldRec helper class

		private class MetaFieldRec
		{
			internal int m_flid;
			internal CellarPropertyType m_fieldType;
			internal int m_ownClsid;
			internal int m_dstClsid;
			internal string m_fieldName;
			internal string m_fieldLabel;
			internal string m_fieldHelp;
			internal Guid m_fieldListRoot; // GUID of some CmPossibilityList
			internal int m_fieldWs; // HVO of some ILgWritingSystem
			internal string m_fieldXml;
			internal string m_sig;
			internal FieldSource m_fieldSource = FieldSource.kModel;

			internal bool Virtual
			{
				get { return m_fieldSource == FieldSource.kVirtual; }
			}
		}

		#endregion MetaFieldRec helper class

		#endregion Helper classes, enums, and methods
	}
}
