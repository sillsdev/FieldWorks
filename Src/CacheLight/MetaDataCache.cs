// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using SIL.Code;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;
using SIL.Xml;

namespace SIL.FieldWorks.CacheLight
{
	/// <summary>
	/// A database-less MetaDataCache implementation.
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

		/// <summary />
		public MetaDataCache()
		{
			m_pathnames = new StringCollection();
		}

		/// <summary>
		/// Creates the meta data cache.
		/// </summary>
		public static IFwMetaDataCache CreateMetaDataCache(string mainModelPathname)
		{
			var mdc = new MetaDataCache();
			mdc.InitXml(mainModelPathname, true);
			return mdc;
		}

		#endregion Construction

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
			return $"{(clid >> 16)}{(clid & 0xffff)}{fieldname}";
		}

		private static void CheckFlid(int flid)
		{
			// A flid of 0 is not acceptable.
			Guard.AssertThat(flid != 0, $"Invalid field identifier '{flid}'.");
		}

		/// <summary>
		/// Sets the destination class id for the virtual property represented by the given flid.
		/// </summary>
		public void SetDstClsId(int flid, int dstClass)
		{
			m_metaFieldRecords[flid].m_dstClsid = dstClass;
		}

		private void GetAllSubclassesForClid(int clid, ICollection<int> allSubclassClids)
		{
			var mcr = m_metaClassRecords[clid];
			foreach (var subClassClid in mcr.m_directSubclasses)
			{
				allSubclassClids.Add(subClassClid);
				GetAllSubclassesForClid(subClassClid, allSubclassClids);
			}
		}

		#endregion Other methods

		#region IFwMetaDataCache implementation

		#region Initialization methods

		/// <inheritdoc />
		public void InitXml(string pathname, bool clearPrevCache)
		{
			Guard.AgainstNullOrEmptyString(pathname, nameof(pathname));
			Guard.AssertThat(File.Exists(pathname), $"'{pathname}' does not exist.");
			if (m_pathnames.Contains(pathname))
			{
				throw new ArgumentException($"File '{pathname}' has already been processed.");
			}

			var doc = XDocument.Load(pathname);
			var classElements = doc.Root.Elements("class").ToList();
			if (!classElements.Any())
			{
				throw new ArgumentException("No classes found.");
			}

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
			// Spin through each class now.
			foreach (var newClassNode in classElements)
			{
				clid = XmlUtils.GetMandatoryIntegerAttributeValue(newClassNode, "num");
				if (clid > 0) // Basic initialization has already happened for the base class.
				{
					var newClassName = XmlUtils.GetMandatoryAttributeValue(newClassNode, "id");
					// Check to see if the class already exists.
					if (m_nameToClid.ContainsKey(newClassName))
					{
						throw new ArgumentException("Duplicate Cellar Class named; " + newClassName);
					}
					mcr = new MetaClassRec(XmlUtils.GetOptionalAttributeValue(newClassNode, "base", newClassName), XmlUtils.GetBooleanAttributeValue(newClassNode, "abstract"), newClassName);
					m_metaClassRecords[clid] = mcr;
					m_nameToClid[newClassName] = clid;
				}
				else
				{
					mcr = m_metaClassRecords[clid];
				}

				// Spin through the properties now.
				var flidBase = clid * 1000;
				if (newClassNode.Element("props") != null)
				{
					foreach (var fieldNode in newClassNode.Element("props").Elements())
					{
						flid = flidBase + XmlUtils.GetMandatoryIntegerAttributeValue(fieldNode, "num");
						mfr = new MetaFieldRec
						{
							m_fieldName = XmlUtils.GetMandatoryAttributeValue(fieldNode, "id"),
							m_ownClsid = clid,
							m_sig = XmlUtils.GetMandatoryAttributeValue(fieldNode, "sig")
						};

						if (m_nameToClid.ContainsKey(mfr.m_sig))
						{
							mfr.m_dstClsid = m_nameToClid[mfr.m_sig];
							mfr.m_sig = null;
						}

						// /basic | props/owning | props/rel
						switch (fieldNode.Name.LocalName)
						{
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
								switch (XmlUtils.GetMandatoryAttributeValue(fieldNode, "card"))
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
								switch (XmlUtils.GetMandatoryAttributeValue(fieldNode, "card"))
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
			}

			// Some mfr objects may not have been able to set their m_dstClsid member,
			// if the referenced class had not been loaded yet.
			// So, try to connect them now.
			// If the client is using multiple files, they may not be set until the last file is loaded.
			foreach (var kvp in m_metaFieldRecords)
			{
				mfr = kvp.Value;
				if (mfr.m_sig == null || !m_nameToClid.ContainsKey(mfr.m_sig))
				{
					continue;
				}

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
				{
					continue; // CmObject.
				}
				if (!m_metaClassRecords.ContainsKey(clidBase))
				{
					continue;
				}

				var recBase = m_metaClassRecords[clidBase];
				recBase.m_directSubclasses.Add(clidChild);
			}

			// Keep the good pathname in case we need to reload.
			m_pathnames.Add(pathname);
		}

		/// <summary>
		/// Initialize the meta-data fields required to be on the base class (which must be the base for
		/// all other classes.
		/// </summary>
		private void InitBaseClassMetaFields(XDocument doc)
		{
			var clid = 0;
			if (!m_metaClassRecords.ContainsKey(clid))
			{
				var baseClassNode = doc.Root.Elements("class").First(e => e.Attribute("num").Value == "0");
				// Add CmObject properties.
				var baseClassName = (baseClassNode == null) ? "BaseClass" : XmlUtils.GetMandatoryAttributeValue(baseClassNode, "id");
				var mcr = new MetaClassRec(baseClassName, true, baseClassName);
				var flid = (int)CmObjectFields.kflidCmObject_Guid;
				var mfr = new MetaFieldRec
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

		/// <inheritdoc />
		public int FieldCount => m_metaFieldRecords.Count;

		/// <inheritdoc />
		public void GetFieldIds(int countOfOutputArray, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 0)] ArrayPtr/*ULONG[]*/ flids)
		{
			var iflid = 0;
			var ids = new int[countOfOutputArray];
			foreach (var kvp in m_nameToFlid)
			{
				var flid = kvp.Value;
				if (iflid == countOfOutputArray)
				{
					break;
				}

				ids[iflid++] = flid;
			}
			MarshalEx.ArrayToNative(flids, countOfOutputArray, ids);
		}

		/// <inheritdoc />
		public string GetOwnClsName(int flid)
		{
			// Zero is not acceptable.
			CheckFlid(flid);

			var ulFlid = flid;
			if (!m_metaFieldRecords.ContainsKey(ulFlid))
			{
				throw new ArgumentException("Invalid field identifier", nameof(flid));
			}
			var mfr = m_metaFieldRecords[ulFlid];
			var mcr = m_metaClassRecords[mfr.m_ownClsid];
			return mcr.m_className;
		}

		/// <inheritdoc />
		public string GetDstClsName(int flid)
		{
			// Zero is not acceptable.
			CheckFlid(flid);

			var mfr = m_metaFieldRecords[flid];
			var mcr = m_metaClassRecords[mfr.m_dstClsid];
			return mcr.m_className;
		}

		/// <inheritdoc />
		public int GetOwnClsId(int flid)
		{
			// 0 technically says that CmObject implements item.
			// Zero is not acceptable.
			CheckFlid(flid);

			var mfr = m_metaFieldRecords[flid];
			return mfr.m_ownClsid;
		}

		/// <inheritdoc />
		public int GetDstClsId(int flid)
		{
			// Zero is not acceptable.
			CheckFlid(flid);

			var mfr = m_metaFieldRecords[flid];
			return mfr.m_dstClsid;
		}

		/// <inheritdoc />
		public string GetFieldName(int flid)
		{
			var mfr = m_metaFieldRecords[flid];
			return mfr.m_fieldName;
		}

		/// <inheritdoc />
		public string GetFieldNameOrNull(int flid)
		{
			MetaFieldRec mfr;
			return m_metaFieldRecords.TryGetValue(flid, out mfr) ? mfr.m_fieldName : null;
		}

		/// <inheritdoc />
		public string GetFieldLabel(int flid)
		{
			var mfr = m_metaFieldRecords[flid];
			return mfr.m_fieldLabel;
		}

		/// <inheritdoc />
		public string GetFieldHelp(int flid)
		{
			var mfr = m_metaFieldRecords[flid];
			return mfr.m_fieldHelp;
		}

		/// <inheritdoc />
		public string GetFieldXml(int flid)
		{
			var mfr = m_metaFieldRecords[flid];
			return mfr.m_fieldXml;
		}

		/// <inheritdoc />
		public int GetFieldWs(int flid)
		{
			var mfr = m_metaFieldRecords[flid];
			return mfr.m_fieldWs;
		}

		/// <inheritdoc />
		public int GetFieldType(int flid)
		{
			var mfr = m_metaFieldRecords[flid];
			return (int)mfr.m_fieldType;
		}

		/// <inheritdoc />
		public bool get_IsValidClass(int flid, int clid)
		{
			if (!m_metaFieldRecords.ContainsKey(flid))
			{
				throw new ArgumentException("Invalid field identifier", nameof(flid));
			}
			var mfr = m_metaFieldRecords[flid];
			if (mfr.m_dstClsid == clid)
			{
				return IsObjectFieldType(mfr.m_fieldType);
			}

			// Check superclasses.
			do
			{
				var mcr = m_metaClassRecords[clid];
				clid = mcr.m_baseClsid;
				if (mfr.m_dstClsid == clid)
				{
					return IsObjectFieldType(mfr.m_fieldType);
				}
			} while (clid != 0);
			return false;
		}

		#endregion Field access methods

		#region Class access methods

		/// <inheritdoc />
		public int ClassCount => m_metaClassRecords.Count;

		/// <inheritdoc />
		public void GetClassIds(int arraySize, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 0)] ArrayPtr/*ULONG[]*/ clids)
		{
			var iclid = 0;
			var ids = new int[arraySize];
			foreach (var kvp in m_nameToClid)
			{
				if (iclid == arraySize)
				{
					break;
				}
				ids[iclid++] = kvp.Value;
			}
			MarshalEx.ArrayToNative(clids, arraySize, ids);
		}

		/// <inheritdoc />
		public string GetClassName(int clid)
		{
			return m_metaClassRecords[clid].m_className;
		}

		/// <inheritdoc />
		public bool GetAbstract(int clid)
		{
			return m_metaClassRecords[clid].m_abstract;
		}

		/// <inheritdoc />
		public int GetBaseClsId(int clid)
		{
			Guard.AssertThat(clid != 0, "CmObject has no base class.");

			return m_metaClassRecords[clid].m_baseClsid;
		}

		/// <inheritdoc />
		public string GetBaseClsName(int clid)
		{
			Guard.AssertThat(clid != 0, "CmObject has no base class.");

			var mcr = m_metaClassRecords[clid];
			mcr = m_metaClassRecords[mcr.m_baseClsid];
			return mcr.m_className;
		}

		/// <inheritdoc />
		public int GetFields(int clid, bool includeSuperclasses, int fieldTypes, int countFlidMax, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 3)] ArrayPtr/*ULONG[]*/ flids)
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
				foreach (var flid in mcr.m_fields)
				{
					var mfr = m_metaFieldRecords[flid];
					if (fieldTypes != (int)CellarPropertyTypeFilter.All)
					{
						// Look up field type and see if it matches
						var fcpt = 1 << (int)mfr.m_fieldType;
						if ((fieldTypes & fcpt) == 0)
						{
							continue; // don't return this one
						}
					}
					countFlids++;
					if (countFlidMax <= 0)
					{
						continue;
					}
					if (countFlids > countFlidMax)
					{
						throw new ArgumentException("Output array is too small.", nameof(countFlidMax));
					}
					ids[iflid++] = flid;
				}
				if (!includeSuperclasses)
				{
					break;
				}
				if (currentClid == 0) // just processed the base object
				{
					break;
				}
				currentClid = mcr.m_baseClsid;
			}
			if (iflid > 0)
			{
				MarshalEx.ArrayToNative(flids, countFlidMax, ids);
			}

			return countFlids;
		}

		#endregion Class access methods

		#region Reverse access methods

		/// <inheritdoc />
		public int GetClassId(string className)
		{
			Guard.AssertThat(m_nameToClid.ContainsKey(className), "Invalid classname.");

			return m_nameToClid[className];
		}

		/// <inheritdoc />
		public int GetFieldId(string className, string fieldName, bool includeBaseClasses)
		{
			return GetFieldId2(GetClassId(className), fieldName, includeBaseClasses);
		}

		/// <inheritdoc />
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
				flid = GetFieldId2(superclassId, fieldName, true);
			}

			return flid;
		}

		/// <inheritdoc />
		public void GetDirectSubclasses(int clid, int countMaximumToReturn, out int countDirectSubclasses, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 1)] ArrayPtr/*ULONG[]*/ subclasses)
		{
			countDirectSubclasses = 0; // Start with 0 for output parameter.
			var ids = new int[countMaximumToReturn];
			if (!m_metaClassRecords.ContainsKey(clid))
			{
				throw new ArgumentException("Class not found.");
			}
			var mcr = m_metaClassRecords[clid];
			countDirectSubclasses = mcr.m_directSubclasses.Count;
			if (countMaximumToReturn == 0)
			{
				return; // Client only wanted the count.
			}
			if (countMaximumToReturn < countDirectSubclasses)
			{
				throw new ArgumentException("Output array is too small.", nameof(countMaximumToReturn));
			}
			var iSubclassClid = 0;
			foreach (var directSubclass in mcr.m_directSubclasses)
			{
				ids[iSubclassClid++] = directSubclass;
			}

			MarshalEx.ArrayToNative(subclasses, countMaximumToReturn, ids);
		}

		/// <inheritdoc />
		public void GetAllSubclasses(int clid, int countMaximumToReturn, out int countAllSubclasses, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ArrayPtrMarshaler), SizeParamIndex = 1)] ArrayPtr/*ULONG[]*/ subclasses)
		{
			// It's easier to just use the maximum than to fret about the right count.
			var ids = new int[countMaximumToReturn];
			var allSubclassClids = new List<int>(countMaximumToReturn) { clid };
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

		/// <inheritdoc />
		public void AddVirtualProp(string className, string fieldName, int virtualFlid, int iFieldType)
		{
			var fieldType = (CellarPropertyType)iFieldType;

			// Do various tests to make sure:
			// 1. The given className exists,
			// 2. The given fieldName does NOT exist for the given class,
			// 3. The given virtualFlid does NOT exist, and finally
			// 4. The fieldType is a legal value.
			// Will throw, if the classname doesn't exist,
			// dealing with #1, above.
			if (!m_nameToClid.ContainsKey(className))
			{
				throw new ArgumentException("Class not found.");
			}
			var clid = m_nameToClid[className];

			// Check condition #2, above.
			// JohnT changed the last argument to 'false'. MDC should allow override virtual handlers.
			var flid = GetFieldId2(clid, fieldName, false);
			if (flid > 0)
			{
				throw new ArgumentException("Field name already exists.", nameof(fieldName));
			}
			// Check condition #3, above.
			MetaFieldRec mfr;
			if (m_metaFieldRecords.ContainsKey(virtualFlid))
			{
				throw new ArgumentException("Field number already in use.", nameof(virtualFlid));
			}

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
						m_fType = FieldType.Virtual
					};
					var mcr = m_metaClassRecords[clid];
					InstallField(mcr, clid, mfr.m_fieldName, virtualFlid, mfr);
					break;
			}
		}

		/// <inheritdoc />
		public bool get_IsVirtual(int flid)
		{
			var mfr = m_metaFieldRecords[flid];
			return mfr.m_fType == FieldType.Virtual;
		}

		#endregion Virtual access methods

		#endregion IFwMetaDataCache implementation

		private sealed class MetaClassRec
		{
			internal string m_superclassName;
			internal int m_baseClsid;
			internal bool m_abstract;
			internal string m_className;
			internal List<int> m_fields = new List<int>(); // collection of flid ids.
			internal List<int> m_directSubclasses = new List<int>(); // collection of class ids.

			internal MetaClassRec(string superclassName, bool isAbstract, string className)
			{
				m_baseClsid = 0;
				m_abstract = false;
				m_className = null;
				m_superclassName = superclassName;
				m_abstract = isAbstract;
				m_className = className;
			}
		}

		private enum FieldType
		{
			Model = 0,
			Custom = 1,
			Virtual = 2
		}

		private sealed class MetaFieldRec
		{
			internal CellarPropertyType m_fieldType;
			internal int m_ownClsid;
			internal int m_dstClsid;
			internal string m_fieldName;
			internal string m_fieldLabel;
			internal string m_fieldHelp;
			internal int m_fieldWs;
			internal string m_fieldXml;
			internal string m_sig;
			internal FieldType m_fType = FieldType.Model;

			internal MetaFieldRec()
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
}