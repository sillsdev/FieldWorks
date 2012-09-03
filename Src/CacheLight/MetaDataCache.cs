// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2006' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MetaDataCache.cs
// Responsibility: Randy Regnier
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Specialized; // Needed for StringCollection.
using System.Collections.Generic; // Needed for Dictionary.
using System.Runtime.InteropServices; // needed for Marshal
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using System.Text;
using System.IO;

namespace SIL.FieldWorks.CacheLight
{
	/// <summary>
	/// A databaseless MetaDataCache implementation.
	/// </summary>
	public sealed class MetaDataCache : IFwMetaDataCache
	{
		#region Data members

		private readonly StringCollection m_pathnames;
		private readonly Dictionary<int, MetaClassRec> m_metaClassRecords = new Dictionary<int, MetaClassRec>();
		private readonly Dictionary<string, int> m_nameToClid = new Dictionary<string, int>();
		private readonly Dictionary<int, MetaFieldRec> m_metaFieldRecords = new Dictionary<int, MetaFieldRec>();
		private readonly Dictionary<string, int> m_nameToFlid = new Dictionary<string, int>();

		#endregion Data members

		#region Construction

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="MetaDataCache"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MetaDataCache()
		{
			m_pathnames = new StringCollection();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the meta data cache.
		/// </summary>
		/// <param name="mainModelPathname">The main model pathname.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static IFwMetaDataCache CreateMetaDataCache(string mainModelPathname)
		{
			var mdc = new MetaDataCache();
			mdc.InitXml(mainModelPathname, true);
			return mdc;
		}

		#endregion Construction

		#region Properties

		#endregion Properties

		#region Other methods

		private void InstallField(MetaClassRec mcr, int clid, string fieldName, int flid, MetaFieldRec mfr)
		{
			mcr.m_fields.Add(flid);
			m_metaFieldRecords[flid] = mfr;
			m_nameToFlid[MakeFlidKey(clid, fieldName)] = flid;
		}

		private static bool IsObjectFieldType(CellarPropertyType type)
		{
			var isObjectFT = false;

			switch (type)
			{
				case CellarPropertyType.OwningAtomic:
				case CellarPropertyType.OwningCollection:
				case CellarPropertyType.OwningSequence:
				case CellarPropertyType.ReferenceAtomic:
				case CellarPropertyType.ReferenceCollection:
				case CellarPropertyType.ReferenceSequence:
					isObjectFT = true;
					break;
			}

			return isObjectFT;
		}

		private static string MakeFlidKey(int clid, string fieldname)
		{
			return String.Format("{0}{1}{2}", (clid >> 16), (clid & 0xffff), fieldname);
		}

		private static void CheckFlid(int flid)
		{
			// A flid of 0 is not acceptable.
			if (flid == 0)
				throw new ArgumentException("Invalid field identifier.", "flid");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the destination class id for the virtual property represented by the given flid.
		/// </summary>
		/// <param name="flid">The flid.</param>
		/// <param name="dstClass">The destination class.</param>
		/// ------------------------------------------------------------------------------------
		public void SetDstClsId(int flid, int dstClass)
		{
			m_metaFieldRecords[flid].m_dstClsid = dstClass;
		}

		private void GetAllSubclassesForClid(int clid, ICollection<int> allSubclassClids)
		{
			var mcr = m_metaClassRecords[clid];
			for (var i = 0; i < mcr.m_directSubclasses.Count; ++i)
			{
				var subClassClid = mcr.m_directSubclasses[i];
				allSubclassClids.Add(subClassClid);
				GetAllSubclassesForClid(subClassClid, allSubclassClids);
			}
		}

		#endregion Other methods

		#region IFwMetaDataCache implementation

		#region Initialization methods

		/// <summary>Initialize MetaDataCache using an XML file.</summary>
		/// <param name='pathname'>pathname</param>
		/// <param name='clearPrevCache'>clearPrevCache</param>
		/// <remarks>
		/// Any number of files can be loaded using this method, but calls after the first time should use false for clearPrevCache.
		/// See Ling.cm or xmi2cellar3.xml for supported XML data formats.
		/// Note: This may also be used to load persisted custom fields.
		/// </remarks>
		public void InitXml(string pathname, bool clearPrevCache)
		{
			if (pathname == null)
				throw new ArgumentNullException("pathname");

			if (m_pathnames.Contains(pathname))
				throw new ArgumentException(String.Format("File '{0}' has already been processed.", pathname));
			var doc = new XmlDocument();
			using (TextReader xmlStream = FileUtils.OpenFileForRead(pathname, Encoding.UTF8))
			{
				doc.Load(xmlStream);
				var classes = doc.SelectNodes("EntireModel");
				if (classes.Count == 0)
					throw new ArgumentException("No classes found.");
				if (clearPrevCache)
				{
					m_metaFieldRecords.Clear();
					m_metaClassRecords.Clear();
					m_nameToClid.Clear();
					m_nameToFlid.Clear();
				}

				InitBaseClassMetaFields(doc);

				int clid;
				int flid;
				MetaClassRec mcr;
				MetaFieldRec mfr;

				// Spin through each module now.
				foreach (XmlNode newClassNode in doc.DocumentElement)
				{
					clid = XmlUtils.GetMandatoryIntegerAttributeValue(newClassNode, "num");
					if (clid > 0) // Basic initialization has already happened for the base class.
					{
						var newClassName = XmlUtils.GetManditoryAttributeValue(newClassNode, "id");
						// Check to see if the class already exists.
						if (m_nameToClid.ContainsKey(newClassName))
							throw new ArgumentException("Duplicate Cellar Class named; " + newClassName);

						mcr = new MetaClassRec(XmlUtils.GetOptionalAttributeValue(newClassNode, "base", newClassName),
							XmlUtils.GetBooleanAttributeValue(newClassNode, "abstract"), newClassName);
						m_metaClassRecords[clid] = mcr;
						m_nameToClid[newClassName] = clid;
					}
					else
						mcr = m_metaClassRecords[clid];

					// Spin through the properties now.
					var flidBase = clid * 1000;
					foreach (XmlNode fieldNode in newClassNode.SelectNodes("props/*"))
					{
						flid = flidBase + XmlUtils.GetMandatoryIntegerAttributeValue(fieldNode, "num");
						mfr = new MetaFieldRec
						{
							m_fieldName = XmlUtils.GetManditoryAttributeValue(fieldNode, "id"),
							m_ownClsid = clid,
							m_sig = XmlUtils.GetManditoryAttributeValue(fieldNode, "sig")
						};

						if (m_nameToClid.ContainsKey(mfr.m_sig))
						{
							mfr.m_dstClsid = m_nameToClid[mfr.m_sig];
							mfr.m_sig = null;
						}

						// /basic | props/owning | props/rel
						switch (fieldNode.Name)
						{
							default:
								break;
							case "basic":
								switch (mfr.m_sig)
								{
									case "TextPropBinary":
										mfr.m_fieldType = CellarPropertyType.Binary;
										break;
									case "Boolean":
										mfr.m_fieldType = CellarPropertyType.Boolean;
										break;
									case "Integer":
										mfr.m_fieldType = CellarPropertyType.Integer;
										break;
									case "Time":
										mfr.m_fieldType = CellarPropertyType.Time;
										break;
									case "String":
										mfr.m_fieldType = CellarPropertyType.String;
										break;
									case "MultiString":
										mfr.m_fieldType = CellarPropertyType.MultiString;
										break;
									case "Unicode":
										mfr.m_fieldType = CellarPropertyType.Unicode;
										break;
									case "MultiUnicode":
										mfr.m_fieldType = CellarPropertyType.MultiUnicode;
										break;
									case "Guid":
										mfr.m_fieldType = CellarPropertyType.Guid;
										break;
									case "Image":
										mfr.m_fieldType = CellarPropertyType.Image;
										break;
									case "GenDate":
										mfr.m_fieldType = CellarPropertyType.GenDate;
										break;
									case "Binary":
										mfr.m_fieldType = CellarPropertyType.Binary;
										break;
									case "Numeric":
										mfr.m_fieldType = CellarPropertyType.Numeric;
										break;
									case "Float":
										mfr.m_fieldType = CellarPropertyType.Float;
										break;
								}
								mfr.m_sig = null;
								break;
							case "owning":
								switch (XmlUtils.GetManditoryAttributeValue(fieldNode, "card"))
								{
									case "atomic":
										mfr.m_fieldType = CellarPropertyType.OwningAtomic;
										break;
									case "col":
										mfr.m_fieldType = CellarPropertyType.OwningCollection;
										break;
									case "seq":
										mfr.m_fieldType = CellarPropertyType.OwningSequence;
										break;
								}
								break;
							case "rel":
								switch (XmlUtils.GetManditoryAttributeValue(fieldNode, "card"))
								{
									case "atomic":
										mfr.m_fieldType = CellarPropertyType.ReferenceAtomic;
										break;
									case "col":
										mfr.m_fieldType = CellarPropertyType.ReferenceCollection;
										break;
									case "seq":
										mfr.m_fieldType = CellarPropertyType.ReferenceSequence;
										break;
								}
								break;
						}
						// Add mfr.
						InstallField(mcr, clid, mfr.m_fieldName, flid, mfr);
					}
				}

				// Some mfr objects may not have been able to set their m_dstClsid member,
				// if the referenced class had not been loaded yet.
				// So, try to connect them now.
				// If the client is using multiple files, they may not be set until the last file is loaded.
				foreach (var kvp in m_metaFieldRecords)
				{
					mfr = kvp.Value;
					if (mfr.m_sig == null || !m_nameToClid.ContainsKey(mfr.m_sig)) continue;

					mfr.m_dstClsid = m_nameToClid[mfr.m_sig];
					mfr.m_sig = null;
				}

				// Get direct subclass ids.
				// Also set the superclass id.
				foreach (var kvp in m_metaClassRecords)
				{
					mcr = kvp.Value;
					var clidChild = kvp.Key;
					var clidBase = m_nameToClid[mcr.m_superclassName];
					mcr.m_baseClsid = clidBase;
					if (clidChild == clidBase)
						continue; // CmObject.
					if (!m_metaClassRecords.ContainsKey(clidBase)) continue;

					var recBase = m_metaClassRecords[clidBase];
					recBase.m_directSubclasses.Add(clidChild);
				}
			}

			// Keep the good pathname in case we need to reload.
			m_pathnames.Add(pathname);
		}

		/// <summary>
		/// Initialize the meta-data fields required to be on the base class (which must be the base for
		/// all other classes.
		/// </summary>
		/// <param name="doc">The XML document containing the model</param>
		private void InitBaseClassMetaFields(XmlDocument doc)
		{
			int clid = 0;
			int flid;
			MetaClassRec mcr;
			MetaFieldRec mfr;

			if (!m_metaClassRecords.ContainsKey(clid))
			{
				XmlNode baseClassNode = doc.DocumentElement.SelectSingleNode("class[@num='0']");
				// Add CmObject properties.
				string baseClassName = (baseClassNode == null) ? "BaseClass" :
					XmlUtils.GetManditoryAttributeValue(baseClassNode, "id");
				mcr = new MetaClassRec(baseClassName, true, baseClassName);
				flid = (int)CmObjectFields.kflidCmObject_Guid;
				mfr = new MetaFieldRec
				{
					m_fieldType = CellarPropertyType.Guid,
					m_fieldName = "Guid$",
					m_ownClsid = clid
				};
				InstallField(mcr, clid, "Guid$", flid, mfr);

				flid = (int)CmObjectFields.kflidCmObject_Class;
				mfr = new MetaFieldRec
				{
					m_fieldType = CellarPropertyType.Integer,
					m_fieldName = "Class$",
					m_ownClsid = clid
				};
				InstallField(mcr, clid, "Class$", flid, mfr);

				flid = (int)CmObjectFields.kflidCmObject_Owner;
				mfr = new MetaFieldRec
				{
					m_fieldType = CellarPropertyType.ReferenceAtomic,
					m_fieldName = "Owner$",
					m_ownClsid = clid
				};
				InstallField(mcr, clid, "Owner$", flid, mfr);

				flid = (int)CmObjectFields.kflidCmObject_OwnFlid;
				mfr = new MetaFieldRec
				{
					m_fieldType = CellarPropertyType.Integer,
					m_fieldName = "OwnFlid$",
					m_ownClsid = clid
				};
				InstallField(mcr, clid, "OwnFlid$", flid, mfr);

				flid = (int)CmObjectFields.kflidCmObject_OwnOrd;
				mfr = new MetaFieldRec
				{
					m_fieldType = CellarPropertyType.Integer,
					m_fieldName = "OwnOrd$",
					m_ownClsid = clid
				};
				InstallField(mcr, clid, "OwnOrd$", flid, mfr);
				m_metaClassRecords[clid] = mcr;
				m_nameToClid[baseClassName] = clid;
			}
		}

		#endregion Initialization methods

		#region Field access methods

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
				 MarshalTypeRef=typeof(ArrayPtrMarshaler),
				 SizeParamIndex=0)] ArrayPtr/*ULONG[]*/ flids)
		{
			var iflid = 0;
			var ids = new int[countOfOutputArray];
			foreach (var kvp in m_nameToFlid)
			{
				var flid = kvp.Value;
				if (iflid == countOfOutputArray)
					break;
				ids[iflid++] = flid;
			}
			MarshalEx.ArrayToNative(flids, countOfOutputArray, ids);
		}

		/// <summary>Gets the name of the class that contains this field.</summary>
		/// <param name='flid'>Field identification number.</param>
		/// <returns>Name of the class that contains the field.</returns>
		public string GetOwnClsName(int flid)
		{
			// Zero is not acceptable.
			CheckFlid(flid);

			var ulFlid = flid;
			if (!m_metaFieldRecords.ContainsKey(ulFlid))
				throw new ArgumentException("Invalid field identifier", "flid");
			var mfr = m_metaFieldRecords[ulFlid];
			var mcr = m_metaClassRecords[mfr.m_ownClsid];
			return mcr.m_className;
		}

		/// <summary>
		/// Gets the name of the destination class that corresponds to this field.
		/// This is the name of the class that is either owned or referred to by another class.
		/// </summary>
		/// <param name='flid'>Field identification number.</param>
		/// <returns>Name of the destination class.</returns>
		public string GetDstClsName(int flid)
		{
			// Zero is not acceptable.
			CheckFlid(flid);

			var mfr = m_metaFieldRecords[flid];
			var mcr = m_metaClassRecords[mfr.m_dstClsid];
			return mcr.m_className;
		}

		/// <summary>Gets the "Id" value of the class that contains this field.</summary>
		/// <param name='flid'>Field identification number.</param>
		/// <returns>Output "Id" of the class that contains the field.</returns>
		public int GetOwnClsId(int flid)
		{
			// 0 technically says that CmObject implements item.
			// Zero is not acceptable.
			CheckFlid(flid);

			var mfr = m_metaFieldRecords[flid];
			return mfr.m_ownClsid;
		}

		/// <summary>Gets the "Id" of the destination class that corresponds to this field.</summary>
		/// <param name='flid'>Field identification number.</param>
		/// <returns>Output "Id" of the class that contains the field.</returns>
		public int GetDstClsId(int flid)
		{
			// Zero is not acceptable.
			CheckFlid(flid);

			var mfr = m_metaFieldRecords[flid];
			return mfr.m_dstClsid;
		}

		/// <summary>Gets the name of a field.</summary>
		/// <param name='flid'>Field identification number.</param>
		/// <returns>Output name of the field.</returns>
		public string GetFieldName(int flid)
		{
			var mfr = m_metaFieldRecords[flid];
			return mfr.m_fieldName;
		}

		/// <summary>Gets the name of a field.</summary>
		/// <param name='flid'>Field identification number.</param>
		/// <returns>Output name of the field.</returns>
		public string GetFieldNameOrNull(int flid)
		{
			MetaFieldRec mfr;
			return m_metaFieldRecords.TryGetValue(flid, out mfr) ? mfr.m_fieldName : null;
		}

		/// <summary>Gets the user label of a field.</summary>
		/// <param name='flid'>Field identification number.</param>
		/// <returns>Output Label of the field. (May be null, if not found.)</returns>
		public string GetFieldLabel(int flid)
		{
			var mfr = m_metaFieldRecords[flid];
			return mfr.m_fieldLabel;
		}

		/// <summary>Gets the help string of a field.p</summary>
		/// <param name='flid'>Field identification number.</param>
		/// <returns>Output help string of the field. (May be null, if not found.)</returns>
		public string GetFieldHelp(int flid)
		{
			var mfr = m_metaFieldRecords[flid];
			return mfr.m_fieldHelp;
		}

		/// <summary>Gets the Xml UI of a field.</summary>
		/// <param name='flid'>Field identification number.</param>
		/// <returns>Output name of the field. (May be null, if not found.)</returns>
		public string GetFieldXml(int flid)
		{
			var mfr = m_metaFieldRecords[flid];
			return mfr.m_fieldXml;
		}

		/// <summary>Gets the Ws of the field.</summary>
		/// <param name='flid'>Field identification number.</param>
		/// <returns>Output field Ws. (May be 0, if not found.)</returns>
		public int GetFieldWs(int flid)
		{
			var mfr = m_metaFieldRecords[flid];
			return mfr.m_fieldWs;
		}

		/// <summary>
		/// Gets the type of the field.
		/// </summary>
		/// <param name='flid'>Field identification number.</param>
		/// <returns>Output field type</returns>
		/// <remarks>
		/// This type value indicates if the field is a primitive data type
		/// or a MultiStr/MultiTxt value or describes the relationship
		/// between two classes (i.e. owning/reference and atomic/collection/sequence).
		/// These numeric values are defined in the 'FWROOT\src\cellar\lib\CmTypes.h' file.
		/// </remarks>
		public int GetFieldType(int flid)
		{
			var mfr = m_metaFieldRecords[flid];
			return (int)mfr.m_fieldType;
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
			if (!m_metaFieldRecords.ContainsKey(flid))
				throw new ArgumentException("Invalid field identifier", "flid");
			var mfr = m_metaFieldRecords[flid];
			//if (mfr.m_dstClsid <= 0)
			//	return false;
			if (mfr.m_dstClsid == clid)
				return IsObjectFieldType(mfr.m_fieldType);

			// Check superclasses.
			MetaClassRec mcr;
			do
			{
				mcr = m_metaClassRecords[clid];
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
				 MarshalTypeRef=typeof(ArrayPtrMarshaler),
				 SizeParamIndex=0)] ArrayPtr/*ULONG[]*/ clids)
		{
			var iclid = 0;
			var ids = new int[arraySize];
			foreach (var kvp in m_nameToClid)
			{
				if (iclid == arraySize)
					break;
				ids[iclid++] = kvp.Value;
			}
			MarshalEx.ArrayToNative(clids, arraySize, ids);
		}

		/// <summary>Gets the name of the class.</summary>
		/// <param name='clid'>Class identification number.</param>
		/// <returns>Output name of the class with the given identification number.</returns>
		public string GetClassName(int clid)
		{
			return m_metaClassRecords[clid].m_className;
		}

		/// <summary>Indicates whether a class is abstract or concrete.</summary>
		/// <param name='clid'>Class identification number.</param>
		/// <returns>'true' if abstract, otherwise 'false'.</returns>
		public bool GetAbstract(int clid)
		{
			return m_metaClassRecords[clid].m_abstract;
		}

		/// <summary>Gets the base class id for a given class.</summary>
		/// <param name='clid'>Class identification number.</param>
		/// <returns>Output base class identification number.</returns>
		public int GetBaseClsId(int clid)
		{
			if (clid == 0)
				throw new ArgumentException("CmObject has no base class.");
			return m_metaClassRecords[clid].m_baseClsid;
		}

		/// <summary>Gets the name of the base class for a given class.</summary>
		/// <param name='clid'>Class identification number.</param>
		/// <returns>Output name of the base class.</returns>
		public string GetBaseClsName(int clid)
		{
			if (clid == 0)
				throw new ArgumentException("CmObject has no base class.");
			var mcr = m_metaClassRecords[clid];
			mcr = m_metaClassRecords[mcr.m_baseClsid];
			return mcr.m_className;
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
				 MarshalTypeRef=typeof(ArrayPtrMarshaler),
				 SizeParamIndex=3)] ArrayPtr/*ULONG[]*/ flids)
		{
			var countFlids = 0;
			var iflid = 0;
			var currentClid = clid;
			var ids = new int[countFlidMax];
			// This loop executes once if fIncludeSuperclasses is false, otherwise over clid
			// and all superclasses.
			for (; ; )
			{
				var mcr = m_metaClassRecords[currentClid];
				for (var i = 0; i < mcr.m_fields.Count; ++i)
				{
					var flid = mcr.m_fields[i];
					var mfr = m_metaFieldRecords[flid];
					if (fieldTypes != (int)CellarPropertyTypeFilter.All)
					{
						// Look up field type and see if it matches
						var fcpt = 1 << (int)mfr.m_fieldType;
						if ((fieldTypes & fcpt) == 0)
							continue; // don't return this one
					}
					countFlids++;
					if (countFlidMax <= 0) continue;

					if (countFlids > countFlidMax)
						throw new ArgumentException("Output array is too small.", "countFlidMax");
					ids[iflid++] = flid;
				}

				if (!includeSuperclasses)
					break;
				if (currentClid == 0) // just processed the base object
					break;
				currentClid = mcr.m_baseClsid;
			}
			if (iflid > 0)
				MarshalEx.ArrayToNative(flids, countFlidMax, ids);

			return countFlids;
		}

		#endregion Class access methods

		#region Reverse access methods

		/// <summary>Get the ID of the class having the specified name.</summary>
		/// <param name='className'>Name of the class to look for.</param>
		/// <returns>Class identification number, or 0 if not found.</returns>
		public int GetClassId(string className)
		{
			if (!m_nameToClid.ContainsKey(className))
				throw new ArgumentException("Invalid classname.");
			return m_nameToClid[className];
		}

		/// <summary>Gets the field ID given the class and field names, or 0 if not found.</summary>
		/// <param name='className'>Name of the class that should contain the fieldname.</param>
		/// <param name='fieldName'>FieldName to look for.</param>
		/// <param name='includeBaseClasses'>'True' to look in superclasses,
		/// otherwise 'false' to just look in the given class.</param>
		/// <returns>Field identification number, or 0 if not found.</returns>
		public int GetFieldId(string className, string fieldName, bool includeBaseClasses)
		{
			return GetFieldId2(GetClassId(className), fieldName, includeBaseClasses);
		}

		/// <summary>Gets the field ID given the class ID and field name, or  0 if not found.</summary>
		/// <param name='clid'>ID  of the class that should contain the fieldname.</param>
		/// <param name='fieldName'>FieldName to look for.</param>
		/// <param name='includeBaseClasses'>'True' to look in superclasses,
		/// otherwise 'false' to just look in the given class.</param>
		/// <returns>Field identification number, or 0 if not found.</returns>
		public int GetFieldId2(int clid, string fieldName, bool includeBaseClasses)
		{
			var flid = 0; // Start on the pessimistic side.
			var flidKey = MakeFlidKey(clid, fieldName);
			if (m_nameToFlid.ContainsKey(flidKey))
			{
				flid = m_nameToFlid[flidKey];
			}
			else if (includeBaseClasses && clid > 0)
			{
				var superclassId = GetBaseClsId(clid);
				flid = GetFieldId2(superclassId, fieldName, includeBaseClasses);
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
				 MarshalTypeRef=typeof(ArrayPtrMarshaler),
				 SizeParamIndex=1)] ArrayPtr/*ULONG[]*/ subclasses)
		{
			countDirectSubclasses = 0; // Start with 0 for output parameter.
			var ids = new int[countMaximumToReturn];
			if (!m_metaClassRecords.ContainsKey(clid))
				throw new ArgumentException("Class not found.");
			var mcr = m_metaClassRecords[clid];
			countDirectSubclasses = mcr.m_directSubclasses.Count;
			if (countMaximumToReturn == 0)
				return; // Client only wanted the count.
			if (countMaximumToReturn < countDirectSubclasses)
				throw new ArgumentException("Output array is too small.", "countMaximumToReturn");
			var iSubclassClid = 0;
			for (var i = 0; i < mcr.m_directSubclasses.Count; ++i)
				ids[iSubclassClid++] = mcr.m_directSubclasses[i];
			MarshalEx.ArrayToNative(subclasses, countMaximumToReturn, ids);
		}

		/// <summary>
		/// Gets all subclasses of the given class,
		/// including itself (which is always the first result in the list,
		/// so it can easily be skipped if desired).
		/// </summary>
		/// <param name='clid'>Class indentifier to work with.</param>
		/// <param name='countMaximumToReturn'>Count of the maximum number of subclass IDs to return (Size of the array.)
		/// When set to zero, countAllSubclasses will contain the full count, so a second call can use the right sized array.</param>
		/// <param name='countAllSubclasses'>Count of how many subclass IDs are the output array.</param>
		/// <param name='subclasses'>Array of subclass IDs.</param>
		/// <remarks>
		/// The list is therefore a complete list of the classes which are valid to store in a property whose
		/// signature is the class identified by 'clid'.
		/// </remarks>
		public void GetAllSubclasses(int clid,
			int countMaximumToReturn, out int countAllSubclasses,
			[MarshalAs(UnmanagedType.CustomMarshaler,
				 MarshalTypeRef=typeof(ArrayPtrMarshaler),
				 SizeParamIndex=1)] ArrayPtr/*ULONG[]*/ subclasses)
		{
			// It's easier to just use the maximum than to fret about the right count.
			var ids = new int[countMaximumToReturn];
			var allSubclassClids = new List<int>(countMaximumToReturn) {clid};
			GetAllSubclassesForClid(clid, allSubclassClids);
			var iSubclassClid = 0;
			countAllSubclasses = Math.Min(countMaximumToReturn, allSubclassClids.Count);
			while (iSubclassClid < countAllSubclasses)
			{
				ids[iSubclassClid] = allSubclassClids[iSubclassClid];
				++iSubclassClid;
			}
			MarshalEx.ArrayToNative(subclasses, countMaximumToReturn, ids);
		}

		#endregion Reverse access methods

		#region Virtual access methods

		/// <summary>Add a virtual property (field) to a class.</summary>
		/// <param name='className'>Name of the class that gets the new virtual property</param>
		/// <param name='fieldName'>Name of the new virtual Field.</param>
		/// <param name='virtualFlid'>Field identifier for the enw virtual field</param>
		/// <param name='iFieldType'>
		/// This type value indicates if the field is a primitive data type
		/// or a MultiStr/MultiTxt value or describes the relationship
		/// between two classes (i.e. owning/reference and atomic/collection/sequence).
		/// These numeric values are defined in CoreImpl\CellarPropertyType.cs.
		/// It must NOT have the virtual bit OR'd in.
		/// </param>
		public void AddVirtualProp(string className, string fieldName, int virtualFlid, int iFieldType)
		{
			CellarPropertyType fieldType = (CellarPropertyType)iFieldType;

			// Do various tests to make sure:
			// 1. The given className exists,
			// 2. The given fieldName does NOT exist for the given class,
			// 3. The given virtualFlid does NOT exist, and finally
			// 4. The fieldType is a legal value.
			// Will throw, if the classname doesn't exist,
			// dealing with #1, above.
			if (!m_nameToClid.ContainsKey(className))
				throw new ArgumentException("Class not found.");
			var clid = m_nameToClid[className];

			// Check condition #2, above.
			// JohnT changed the last argument to 'false'. MDC should allow override virtual handlers.
			var flid = GetFieldId2(clid, fieldName, false);
			if (flid > 0)
				throw new ArgumentException("Field name already exists.", "fieldName");

			// Check condition #3, above.
			MetaFieldRec mfr;
			if (m_metaFieldRecords.ContainsKey(virtualFlid))
				throw new ArgumentException("Field number already in use.", "virtualFlid");

			// Test condition #4, above.
			// May throw if fieldType isn't valid.
			switch (fieldType)
			{
				default:
					throw new ArgumentException("Invalid field type.", "fieldType");
				case CellarPropertyType.Boolean:
				case CellarPropertyType.Integer:
				case CellarPropertyType.Numeric:
				case CellarPropertyType.Float:
				case CellarPropertyType.Time:
				case CellarPropertyType.Guid:
				case CellarPropertyType.Image:
				case CellarPropertyType.GenDate:
				case CellarPropertyType.Binary:
				case CellarPropertyType.String:
				case CellarPropertyType.MultiString:
				case CellarPropertyType.Unicode:
				case CellarPropertyType.MultiUnicode:
				case CellarPropertyType.OwningAtomic:
				case CellarPropertyType.ReferenceAtomic:
				case CellarPropertyType.OwningCollection:
				case CellarPropertyType.ReferenceCollection:
				case CellarPropertyType.OwningSequence:
				case CellarPropertyType.ReferenceSequence:
					mfr = new MetaFieldRec
							{
								m_fieldType = fieldType,
								m_fieldName = fieldName,
								m_ownClsid = clid,
								m_fType = FieldType.virt
							};
					MetaClassRec mcr = m_metaClassRecords[clid];
					InstallField(mcr, clid, mfr.m_fieldName, virtualFlid, mfr);
					break;
			}
		}

		/// <summary>Check if the given flid is virtual or regular.</summary>
		/// <param name='flid'>Field identifier to check.</param>
		/// <returns>'True' if 'flid' is a virtual field, otherwise 'false'. </returns>
		public bool get_IsVirtual(int flid)
		{
			MetaFieldRec mfr = m_metaFieldRecords[flid];
			return mfr.m_fType == FieldType.virt;
		}

		#endregion Virtual access methods

		#endregion IFwMetaDataCache implementation
	}

	internal class MetaClassRec
	{
		public string m_superclassName;
		public int m_baseClsid;
		public bool m_abstract;
		public string m_className;
		public List<int> m_fields = new List<int>(); // collection of flid ids.
		public List<int> m_directSubclasses = new List<int>(); // collection of class ids.

		public MetaClassRec()
		{
			m_baseClsid = 0;
			m_abstract = false;
			m_className = null;
		}

		public MetaClassRec(string superclassName, bool isAbstract, string className)
		{
			m_superclassName = superclassName;
			m_abstract = isAbstract;
			m_className = className;
		}
	}

	internal enum FieldType
	{
		model = 0,
		custom = 1,
		virt = 2
	}

	internal class MetaFieldRec
	{
		public CellarPropertyType m_fieldType;
		public int m_ownClsid;
		public int m_dstClsid;
		public string m_fieldName;
		public string m_fieldLabel;
		public string m_fieldHelp;
		public int m_fieldWs;
		public string m_fieldXml;
		public string m_sig;
		public FieldType m_fType = FieldType.model;

		public MetaFieldRec()
		{
			m_fieldType = 0;
			m_ownClsid = 0;
			m_dstClsid = 0;
			m_fieldWs = 0;
			m_fieldName = null;
			m_fieldLabel = null;
			m_fieldHelp = null;
			m_fieldXml = null;
		}
	}
}