// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.LCModel;
using System.Reflection;
using SIL.LCModel.Infrastructure;
using System.Collections;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;

namespace LCMBrowser
{
	/// <summary />
	public class LcmInspectorList : GenericInspectorObjectList
	{
		private LcmCache m_cache;
		private IFwMetaDataCacheManaged m_mdc;
		private Dictionary<CellarPropertyType, string> m_fldSuffix;

		/// <summary>
		/// Gets or sets a value indicating whether or not to use the field definitions
		/// found in the meta data cache when querying an object for it's properties. If this
		/// value is false, then all of the properties found via .Net reflection are loaded
		/// into inspector objects. Otherwise, only those properties specified in the meta
		/// data cache are loaded.
		/// </summary>
		public bool UseMetaDataCache { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:LcmInspectorList"/> class.
		/// </summary>
		public LcmInspectorList(LcmCache cache)
		{
			m_cache = cache;
			m_mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			m_fldSuffix = new Dictionary<CellarPropertyType, string>
			{
				[CellarPropertyType.OwningCollection] = "OC",
				[CellarPropertyType.OwningSequence] = "OS",
				[CellarPropertyType.ReferenceCollection] = "RC",
				[CellarPropertyType.ReferenceSequence] = "RS",
				[CellarPropertyType.OwningAtomic] = "OA",
				[CellarPropertyType.ReferenceAtomic] = "RA"
			};
		}

		#region overridden methods
		/// <summary>
		/// Initializes the list using the specified top level object.
		/// </summary>
		public override void Initialize(object topLevelObj)
		{
			Initialize(topLevelObj, null);
		}

		/// <summary>
		/// Initializes the list using the specified top level object.
		/// </summary>
		public void Initialize(object topLevelObj, Dictionary<object, IInspectorObject> iobectsToKeep)
		{
			base.Initialize(topLevelObj);

			foreach (var io in this)
			{
				if (io.Object == null || io.Object.GetType().GetInterface("IRepository`1") == null)
				{
					continue;
				}
				var pi = io.Object.GetType().GetProperty("Count");
				var count = (int)pi.GetValue(io.Object, null);
				io.DisplayValue = FormatCountString(count);
				io.DisplayName = io.DisplayType;
				io.HasChildren = (count > 0);
			}

			Sort(CompareInspectorObjectNames);

			if (iobectsToKeep != null)
			{
				SwapInspectorObjects(iobectsToKeep);
			}
		}

		private void SwapInspectorObjects(Dictionary<object, IInspectorObject> iobectsToKeep)
		{
		}

		/// <summary>
		/// Gets a list of IInspectorObject objects for the properties of the specified object.
		/// </summary>
		protected override List<IInspectorObject> GetInspectorObjects(object obj, int level)
		{
			if (obj == null)
			{
				return BaseGetInspectorObjects(null, level);
			}
			var tmpObj = obj;
			var io = obj as IInspectorObject;
			if (io != null)
			{
				tmpObj = io.Object;
			}

			if (tmpObj == null)
			{
				return BaseGetInspectorObjects(obj, level);
			}

			if (tmpObj.GetType().GetInterface("IRepository`1") != null)
			{
				return GetInspectorObjectsForRepository(tmpObj, io, level);
			}

			if (tmpObj is IMultiAccessorBase)
			{
				return GetInspectorObjectsForMultiString(tmpObj as IMultiAccessorBase, io, level);
			}

			if (LCMBrowserForm.m_virtualFlag == false && io != null && io.ParentInspectorObject != null &&
				io.ParentInspectorObject.DisplayName == "Values" &&
				io.ParentInspectorObject.ParentInspectorObject.DisplayType == "MultiUnicodeAccessor")
			{
				return GetInspectorObjectsForUniRuns(tmpObj as ITsString, io as IInspectorObject, level);
			}

			if (tmpObj is ITsString)
			{
				return GetInspectorObjectsForTsString(tmpObj as ITsString, io, level);
			}

			if (LCMBrowserForm.m_virtualFlag == false && tmpObj is TextProps)
			{
				return GetInspectorObjectsForTextProps(tmpObj as TextProps, io, level);
			}

			if (LCMBrowserForm.m_virtualFlag == false && io != null && io.DisplayName == "Values" &&
				(io.ParentInspectorObject.DisplayType == "MultiUnicodeAccessor" ||
				 io.ParentInspectorObject.DisplayType == "MultiStringAccessor"))
			{
				return GetInspectorObjectsForValues(tmpObj, io as IInspectorObject, level);
			}

			return io != null && io.DisplayName.EndsWith("RC") && io.Flid > 0 && m_mdc.IsCustom(io.Flid)
				? GetInspectorObjectsForCustomRC(tmpObj, io, level)
				: BaseGetInspectorObjects(obj, level);
		}

		/// <summary>
		/// Gets the inspector objects for the specified repository object;
		/// </summary>
		private List<IInspectorObject> GetInspectorObjectsForRepository(object obj, IInspectorObject ioParent, int level)
		{
			var i = 0;
			var list = new List<IInspectorObject>();
			foreach (var instance in GetRepositoryInstances(obj))
			{
				var io = CreateInspectorObject(instance, obj, ioParent, level);

				if (LCMBrowserForm.m_virtualFlag == false && obj.ToString().IndexOf("LexSenseRepository") > 0)
				{
					var tmpObj = io.Object as ILexSense;
					io.DisplayValue = tmpObj.FullReferenceName.Text;
					io.DisplayName = $"[{i++}]: {GetObjectOnly(tmpObj.ToString())}";
				}
				else if (LCMBrowserForm.m_virtualFlag == false && obj.ToString().IndexOf("LexEntryRepository") > 0 )
				{
					var tmpObj = io.Object as ILexEntry;
					io.DisplayValue = tmpObj.HeadWord.Text;
					io.DisplayName = $"[{i++}]: {GetObjectOnly(tmpObj.ToString())}";
				}
				else
				{
					io.DisplayName = $"[{i++}]";
				}

				list.Add(io);
			}

			i = IndexOf(obj);
			if (i < 0)
			{
				return list;
			}
			this[i].DisplayValue = FormatCountString(list.Count);
			this[i].HasChildren = (list.Count > 0);

			return list;
		}

		/// <summary>
		/// Process lines that have a DateTime type.
		/// </summary>
		private List<IInspectorObject> GetInspectorObjectForDateTime(DateTime tmpObj, IInspectorObject ioParent, int level)
		{
			var list = new List<IInspectorObject>();
			var io = CreateInspectorObject(tmpObj, null, ioParent, level);
			io.HasChildren = false;
			list.Add(io);

			return list;
		}

		/// <summary>
		/// Gets the inspector objects for the specified MultiString.
		/// </summary>
		private List<IInspectorObject> GetInspectorObjectsForMultiString(IMultiAccessorBase msa, IInspectorObject ioParent, int level)
		{
			var list = LCMBrowserForm.m_virtualFlag ? BaseGetInspectorObjects(msa, level) : GetMultiStringInspectorObjects(msa, ioParent, level);
			var allStrings = new Dictionary<int, string>();
			try
			{
				// Put this in a try/catch because VirtualStringAccessor
				// didn't implement StringCount when this was written.
				for (var i = 0; i < msa.StringCount; i++)
				{
					int ws;
					var tss = msa.GetStringFromIndex(i, out ws);
					allStrings[ws] = tss.Text;
				}
			}
			catch { }

			if (!LCMBrowserForm.m_virtualFlag)
			{
				return list;
			}
			var io = CreateInspectorObject(allStrings, msa, ioParent, level);
			io.DisplayName = "AllStrings";
			io.DisplayValue = FormatCountString(allStrings.Count);
			io.HasChildren = (allStrings.Count > 0);
			list.Insert(0, io);
			list.Sort((x, y) => x.DisplayName.CompareTo(y.DisplayName));
			return list;
		}

		/// <summary>
		/// Gets a list of IInspectorObject objects (same as base), but includes s lot of
		/// specifics if you choose not to see virtual fields.
		/// </summary>
		private List<IInspectorObject> GetMultiStringInspectorObjects(object obj, IInspectorObject ioParent, int level)
		{
			if (ioParent != null)
			{
				obj = ioParent.Object;
			}

			var list = new List<IInspectorObject>();
			var collection = obj as ICollection;
			if (collection != null)
			{
				var i = 0;
				foreach (var item in collection)
				{
					var io = CreateInspectorObject(item, obj, ioParent, level);
					io.DisplayName = $"[{i++}]";
					list.Add(io);
				}

				return list;
			}

			foreach (var pi in GetPropsForObj(obj))
			{
				try
				{
					var propObj = pi.GetValue(obj, null);
					var Itmp = CreateInspectorObject(pi, propObj, obj, ioParent, level);

					if ((obj.ToString().IndexOf("MultiUnicodeAccessor") > 0 && Itmp.DisplayName != "Values") ||
						(obj.ToString().IndexOf("MultiStringAccessor") > 0 && Itmp.DisplayName != "Values"))
					{
						continue;
					}
					var itmp4 = Itmp.Object as ICollection;
					if (itmp4 != null)
					{
						Itmp.DisplayValue = $"Count = {itmp4.Count}";
						Itmp.HasChildren = (itmp4.Count > 0);
					}
					list.Add(Itmp);
				}
				catch (Exception e)
				{
				}
			}

			list.Sort(CompareInspectorObjectNames);
			return list;
		}

		/// <summary>
		/// Gets the inspector objects for the specified TsString.
		/// </summary>
		private List<IInspectorObject> GetInspectorObjectsForTsString(ITsString tss, IInspectorObject ioParent, int level)
		{
			var list = new List<IInspectorObject>();
			var runCount = tss.RunCount;
			var tssriList = new List<TsStringRunInfo>();
			for (var i = 0; i < runCount; i++)
			{
				tssriList.Add(new TsStringRunInfo(i, tss, m_cache));
			}

			var io = CreateInspectorObject(tssriList, tss, ioParent, level);
			io.DisplayName = "Runs";
			io.DisplayValue = FormatCountString(tssriList.Count);
			io.HasChildren = (tssriList.Count > 0);
			list.Add(io);

			if (!LCMBrowserForm.m_virtualFlag)
			{
				return list;
			}
			io = CreateInspectorObject(tss.Length, tss, ioParent, level);
			io.DisplayName = "Length";
			list.Add(io);
			io = CreateInspectorObject(tss.Text, tss, ioParent, level);
			io.DisplayName = "Text";
			list.Add(io);

			return list;
		}

		/// <summary>
		/// Gets the inspector objects for the specified TextProps.
		/// </summary>
		private List<IInspectorObject> GetInspectorObjectsForTextProps(TextProps txp, IInspectorObject ioParent, int level)
		{
			if (ioParent != null)
			{
				txp = ioParent.Object as TextProps;
			}

			var list = new List<IInspectorObject>();
			var txp1 = txp as ICollection;

			if (txp1 != null)
			{
				var i = 0;
				foreach (var item in txp1)
				{
					var io = CreateInspectorObject(item, txp, ioParent, level);
					io.DisplayName = $"[{i++}]";
					list.Add(io);
				}

				return list;
			}

			var saveIntPropCount = 0;
			var saveStrPropCount = 0;
			foreach (var pi in GetPropsForObj(txp))
			{
				if (pi.Name != "IntProps" && pi.Name != "StrProps" && pi.Name != "IntPropCount" && pi.Name != "StrPropCount")
				{
					continue;
				}
				IInspectorObject io;
				switch (pi.Name)
				{
					case "IntProps":
						var propObj = pi.GetValue(txp, null);
						io = CreateInspectorObject(pi, propObj, txp, ioParent, level);
						io.DisplayValue = "Count = " + saveIntPropCount;
						io.HasChildren = (saveIntPropCount > 0);
						list.Add(io);
						break;
					case "StrProps":
						var propObj1 = pi.GetValue(txp, null);
						io = CreateInspectorObject(pi, propObj1, txp, ioParent, level);
						io.DisplayValue = "Count = " + saveStrPropCount;
						io.HasChildren = (saveStrPropCount > 0);
						list.Add(io);
						break;
					case "StrPropCount":
						saveStrPropCount = (int)pi.GetValue(txp, null);
						break;
					case "IntPropCount":
						saveIntPropCount = (int)pi.GetValue(txp, null);
						break;
				}
			}

			list.Sort(CompareInspectorObjectNames);
			return list;
		}

		/// <summary>
		/// Gets a list of IInspectorObject objects representing all the properties for the
		/// specified object, which is assumed to be at the specified level.
		/// </summary>
		protected virtual List<IInspectorObject> GetInspectorObjectsForValues(object obj, IInspectorObject ioParent, int level)
		{
			if (ioParent != null)
			{
				obj = ioParent.Object;
			}

			var list = new List<IInspectorObject>();
			var multiStr = ioParent.OwningObject as IMultiAccessorBase;
			if (multiStr != null)
			{
				foreach (var ws in multiStr.AvailableWritingSystemIds)
				{
					var wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(ws);
					var ino = CreateInspectorObject(multiStr.get_String(ws), obj, ioParent, level);
					ino.DisplayName = wsObj.DisplayLabel;
					list.Add(ino);
				}
				return list;
			}

			var props = GetPropsForObj(obj);
			foreach (var pi in props)
			{
				try
				{
					var propObj = pi.GetValue(obj, null);
					list.Add(CreateInspectorObject(pi, propObj, obj, ioParent, level));
				}
				catch (Exception e)
				{
					list.Add(CreateExceptionInspectorObject(e, obj, pi.Name, level, ioParent));
				}
			}

			list.Sort(CompareInspectorObjectNames);
			return list;
		}

		/// <summary>
		/// Condenses the 'Run' information for MultiUnicodeAccessor entries because
		/// there will only be 1 run,
		/// </summary>
		protected virtual List<IInspectorObject> GetInspectorObjectsForUniRuns(ITsString obj, IInspectorObject ioParent, int level)
		{
			var list = new List<IInspectorObject>();

			if (obj == null)
			{
				return list;
			}
			var ino = CreateInspectorObject(obj, ioParent.OwningObject, ioParent, level);

			ino.DisplayName = "Writing System";
			ino.DisplayValue = obj.get_WritingSystemAt(0).ToString();
			ino.HasChildren = false;
			list.Add(ino);

			var tss = new TsStringRunInfo(0, obj, m_cache);

			ino = CreateInspectorObject(tss, obj, ioParent, level);
			ino.DisplayName = "Text";
			ino.DisplayValue = tss.Text;
			ino.HasChildren = false;
			list.Add(ino);
			return list;
		}

		/// <summary>
		/// Create the reference collectiomn list for ther custom reference collection.,
		/// </summary>
		protected virtual List<IInspectorObject> GetInspectorObjectsForCustomRC(object obj, IInspectorObject ioParent, int level)
		{
			if (obj == null)
			{
				return null;
			}

			// Inspectors for custom reference collections are supposed to be configured with
			// obj being an array of the HVOs.
			var collection = obj as ICollection;
			if (collection == null)
			{
				MessageBox.Show("Custom Reference collection not properly configured with array of HVOs");
				return null;
			}
			var list = new List<IInspectorObject>();
			var n = 0;
			// Just like an ordinary reference collection, we want to make one inspector for each
			// item in the collection, where the first argument to CreateInspectorObject is the
			// cmObject. Keep this code in sync with BaseGetInspectorObjects.
			foreach (int hvoItem in collection)
			{
				var hvoNum = int.Parse(hvoItem.ToString());
				var objItem = m_cache.ServiceLocator.GetObject(hvoNum);
				var io = CreateInspectorObject(objItem, obj, ioParent, level);
				io.DisplayName = $"[{n++}]";
				list.Add(io);
			}
			return list;
		}

		/// <summary>
		/// Gets a list of IInspectorObject objects representing all the properties for the
		/// specified object, which is assumed to be at the specified level.
		/// </summary>
		protected virtual List<IInspectorObject> BaseGetInspectorObjects(object obj, int level)
		{
			var ioParent = obj as IInspectorObject;
			if (ioParent != null)
			{
				obj = ioParent.Object;
			}

			var list = new List<IInspectorObject>();
			var collection = obj as ICollection;
			if (collection != null)
			{
				var i = 0;
				foreach (var item in collection)
				{
					var io = CreateInspectorObject(item, obj, ioParent, level);
					io.DisplayName = $"[{i++}]";
					list.Add(io);
				}
				return list;
			}

			foreach (var pi in GetPropsForObj(obj))
			{
				try
				{
					var propObj = pi.GetValue(obj, null);
					var io1 = CreateInspectorObject(pi, propObj, obj, ioParent, level);
					if (io1.DisplayType == "System.DateTime")
					{
						io1.HasChildren = false;
					}
					list.Add(io1);
				}
				catch (Exception e)
				{
					list.Add(CreateExceptionInspectorObject(e, obj, pi.Name, level, ioParent));
				}
			}

			if (LCMBrowserForm.CFields != null && LCMBrowserForm.CFields.Count > 0 && obj != null)
			{
				foreach (var cf2 in LCMBrowserForm.CFields)
				{
					if (!obj.ToString().Contains(m_mdc.GetClassName(cf2.ClassID)))
					{
						continue;
					}
					var io = CreateCustomInspectorObject(obj, ioParent, level, cf2);
					list.Add(io);
				}
			}

			list.Sort(CompareInspectorObjectNames);
			return list;
		}

		/// <summary>
		/// Gets the properties specified in the meta data cache for the specified object .
		/// </summary>
		protected override PropertyInfo[] GetPropsForObj(object obj)
		{
			if (m_mdc != null && obj is ICmObject && UseMetaDataCache)
			{
				return GetFieldsFromMetaDataCache(obj as ICmObject);
			}

			var propArray = base.GetPropsForObj(obj);
			var cmObj = obj as ICmObject;
			var props = new List<PropertyInfo>(propArray);

			if (m_mdc == null || cmObj == null)
			{
				return propArray;
			}

			RevisePropsList(cmObj, ref props);
			return props.ToArray();
		}

		/// <summary>
		/// Gets the fields from meta data cache.
		/// </summary>
		private PropertyInfo[] GetFieldsFromMetaDataCache(ICmObject cmObj)
		{
			if (cmObj == null)
			{
				return base.GetPropsForObj(cmObj);
			}
			var props = new List<PropertyInfo>();
			var flids = m_mdc.GetFields(cmObj.ClassID, true, (int)CellarPropertyTypeFilter.All);
			var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty;

			// Get only the fields for the object that are specified in the meta data cache.
			foreach (var flid in flids)
			{
				var fieldName = m_mdc.GetFieldName(flid);
				var fieldType = (CellarPropertyType)m_mdc.GetFieldType(flid);
				string suffix;
				if (m_fldSuffix.TryGetValue(fieldType, out suffix))
				{
					fieldName += suffix;
				}

				var pi = cmObj.GetType().GetProperty(fieldName, flags);
				if (pi != null)
				{
					props.Add(pi);
				}
			}

			return (props.Count > 0 ? props.ToArray() : base.GetPropsForObj(cmObj));
		}

		/// <summary>
		/// Create InspectorObjects for the custom fields for the current object.
		/// </summary>
		private IInspectorObject CreateCustomInspectorObject(object obj, IInspectorObject parentIo, int level, CustomFields cf)
		{
			var to = obj as ICmObject;
			var managedSilDataAccess = m_cache.GetManagedSilDataAccess();
			var iValue = string.Empty;
			var fieldId = cf.FieldID;
			IInspectorObject io = null;
			if (obj != null)
			{
				switch (cf.Type)
				{
					case "ITsString":
						var oValue = m_cache.DomainDataByFlid.get_StringProp(to.Hvo, fieldId);
						io = base.CreateInspectorObject(null, oValue, obj, parentIo, level);
						iValue = oValue.Text;
						io.HasChildren = false;
						io.DisplayName = cf.Name;
						break;
					case "System.Int32":
						var sValue = m_cache.DomainDataByFlid.get_IntProp(to.Hvo, fieldId);
						io = base.CreateInspectorObject(null, sValue, obj, parentIo, level);
						iValue = sValue.ToString();
						io.HasChildren = false;
						io.DisplayName = cf.Name;
						break;
					case "SIL.FieldWorks.Common.FwUtils.GenDate":
						// tried get_TimeProp, get_UnknowbProp, get_Prop
						var genObj = managedSilDataAccess.get_GenDateProp(to.Hvo, fieldId);
						io = base.CreateInspectorObject(null, genObj, obj, parentIo, level);
						iValue = genObj.ToString();
						io.HasChildren = true;
						io.DisplayName = cf.Name;
						break;
					case "LcmReferenceCollection<ICmPossibility>":	// ReferenceCollection
						var count = m_cache.DomainDataByFlid.get_VecSize(to.Hvo, fieldId);
						iValue = $"Count = {count}";
						var objects = m_cache.GetManagedSilDataAccess().VecProp(to.Hvo, fieldId);
						objects.Initialize();
						io = base.CreateInspectorObject(null, objects, obj, parentIo, level);
						io.HasChildren = count > 0;
						io.DisplayName = $"{cf.Name}RC";
						break;
					case "ICmPossibility":	// ReferenceAtomic
						var rValue = m_cache.DomainDataByFlid.get_ObjectProp(to.Hvo, fieldId);
						var posObj = (rValue == 0? null: (ICmPossibility)m_cache.ServiceLocator.GetObject(rValue));
						io = base.CreateInspectorObject(null, posObj, obj, parentIo, level);
						iValue = (posObj == null? "null": posObj.NameHierarchyString);
						io.HasChildren = posObj != null;
						io.DisplayName = $"{cf.Name}RA";
						break;
					case "IStText":	//    multi-paragraph text (OA) StText)
						var mValue = m_cache.DomainDataByFlid.get_ObjectProp(to.Hvo, fieldId);
						var paraObj = (mValue == 0? null: (IStText)m_cache.ServiceLocator.GetObject(mValue));
						io = base.CreateInspectorObject(null, paraObj, obj, parentIo, level);
						iValue = (paraObj == null? "null": "StText: " + paraObj.Hvo.ToString());
						io.HasChildren = mValue > 0;
						io.DisplayName = $"{cf.Name}OA";
						break;
					default:
						MessageBox.Show($@"The type of the custom field is {cf.Type}");
						break;
				}
			}

			io.DisplayType = cf.Type;
			io.DisplayValue = (iValue ?? "null");
			io.Flid = cf.FieldID;

			return io;
		}

		/// <summary>
		/// Removes properties from the specified list of properties, those properties the
		/// user has specified he doesn't want to see in the browser.
		/// </summary>
		private void RevisePropsList(ICmObject cmObj, ref List<PropertyInfo> props)
		{
			if (cmObj == null)
			{
				return;
			}

			for (var i = props.Count - 1; i >= 0; i--)
			{
				if (props[i].Name == "Guid")
				{
					continue;
				}

				if (!LCMClassList.IsPropertyDisplayed(cmObj, props[i].Name))
				{
					props.RemoveAt(i);
					continue;
				}

				var work = LCMBrowserForm.StripOffTypeChars(props[i].Name);

				var flid = 0;
				if (m_mdc.FieldExists(cmObj.ClassID, work, true))
				{
					flid = m_mdc.GetFieldId2(cmObj.ClassID, work, true);
				}
				else
				{
					if (LCMBrowserForm.m_virtualFlag == false)
					{
						props.RemoveAt(i);
						continue;
					}
				}

				if (LCMBrowserForm.m_virtualFlag == false && flid >= 20000000 && flid < 30000000)
				{
					props.RemoveAt(i);
					continue;
				}

				if (LCMBrowserForm.m_virtualFlag == false && m_mdc.get_IsVirtual(flid))
				{
					props.RemoveAt(i);
				}
			}
		}

		/// <summary>
		/// Gets an inspector object for the specified property info., checking for various
		/// LCM interface types.
		/// </summary>
		protected override IInspectorObject CreateInspectorObject(PropertyInfo pi, object obj, object owningObj, IInspectorObject ioParent, int level)
		{
			var io = base.CreateInspectorObject(pi, obj, owningObj, ioParent, level);

			if (pi == null && io != null)
			{
				io.DisplayType = StripOffLCMNamespace(io.DisplayType);
			}

			else if (pi != null && io == null)
			{
				io.DisplayType = pi.PropertyType.Name;
			}

			else if (pi != null && io != null)
			{
				io.DisplayType = (io.DisplayType == "System.__ComObject" ?
				pi.PropertyType.Name : StripOffLCMNamespace(io.DisplayType));
			}

			if (obj == null)
			{
				return io;
			}

			if (obj is char)
			{
				io.DisplayValue = $"'{io.DisplayValue}'   (U+{(int) (char) obj:X4})";
				return io;
			}

			if (obj is ILcmVector)
			{
				var mi = obj.GetType().GetMethod("ToArray");
				try
				{
					var array = mi.Invoke(obj, null) as ICmObject[];
					io.Object = array;
					io.DisplayValue = FormatCountString(array.Length);
					io.HasChildren = (array.Length > 0);
				}
				catch (Exception e)
				{
					io = CreateExceptionInspectorObject(e, obj, pi.Name, level, ioParent);
				}
			}
			else if (obj is ICollection<ICmObject>)
			{
				var array = ((ICollection<ICmObject>)obj).ToArray();
				io.Object = array;
				io.DisplayValue = FormatCountString(array.Length);
				io.HasChildren = array.Length > 0;
			}

			const string fmtAppend = "{0}, {{{1}}}";
			const string fmtReplace = "{0}";
			const string fmtStrReplace = "\"{0}\"";

			if (obj is ICmFilter)
			{
				var filter = (ICmFilter)obj;
				io.DisplayValue = string.Format(fmtAppend, io.DisplayValue, filter.Name);
			}
			else if (obj is IMultiAccessorBase)
			{
				var str = (IMultiAccessorBase)obj;
				io.DisplayValue = string.Format(fmtReplace, str.AnalysisDefaultWritingSystem.Text);
			}
			else if (obj is ITsString)
			{
				var str = (ITsString)obj;
				io.DisplayValue = string.Format(fmtStrReplace, str.Text);
				io.HasChildren = true;
			}
			else if (obj is ITsTextProps)
			{
				io.Object = new TextProps(obj as ITsTextProps, m_cache);
				io.DisplayValue = string.Empty;
				io.HasChildren = true;
			}
			else if (obj is IPhNCSegments)
			{
				var seg = (IPhNCSegments)obj;
				io.DisplayValue = string.Format(fmtAppend, io.DisplayValue, seg.Name.AnalysisDefaultWritingSystem.Text);
			}
			else if (obj is IPhEnvironment)
			{
				var env = (IPhEnvironment)obj;
				io.DisplayValue = $"{io.DisplayValue}, {{Name: {env.Name.AnalysisDefaultWritingSystem.Text}, Pattern: {env.StringRepresentation.Text}}}";
			}
			else if (obj is IMoEndoCompound)
			{
				var moendo = (IMoEndoCompound)obj;
				io.DisplayValue = string.Format(fmtAppend, io.DisplayValue, moendo.Name.AnalysisDefaultWritingSystem.Text);
			}
			else if (obj.GetType().GetInterface("IRepository`1") != null)
			{
				io.DisplayName = io.DisplayType;
			}

			return io;
		}

		#endregion

		/// <summary />
		private string StripOffLCMNamespace(string type)
		{
			if (string.IsNullOrEmpty(type))
			{
				return string.Empty;
			}

			if (!type.StartsWith("SIL.LCModel"))
			{
				return type;
			}

			type = type.Replace("SIL.LCModel.Infrastructure.Impl.", string.Empty);
			type = type.Replace("SIL.LCModel.Infrastructure.", string.Empty);
			type = type.Replace("SIL.LCModel.DomainImpl.", string.Empty);
			type = type.Replace("SIL.LCModel.", string.Empty);

			return CleanupGenericListType(type);
		}

		/// <summary>
		/// Gets a list of all the instances in the specified repository.
		/// </summary>
		private List<object> GetRepositoryInstances(object repository)
		{
			var list = new List<object>();

			try
			{
				// Get an object that represents all the repository's collection of instances
				object repoInstances = null;
				foreach (var mi in repository.GetType().GetMethods())
				{
					if (mi.Name == "AllInstances")
					{
						repoInstances = mi.Invoke(repository, null);
						break;
					}
				}

				if (repoInstances == null)
				{
					throw new MissingMethodException($"Repository {repository.GetType().Name} is missing 'AllInstances' method.");
				}

				var ienum = repoInstances as IEnumerable;
				if (ienum == null)
				{
					throw new NullReferenceException($"Repository {repository.GetType().Name} is not an IEnumerable");
				}

				var enumerator = ienum.GetEnumerator();
				while (enumerator.MoveNext())
				{
					list.Add(enumerator.Current);
				}
			}
			catch (Exception e)
			{
				list.Add(e);
			}

			return list;
		}

		/// <summary>
		/// Finds the item in the list having the specified hvo.
		/// </summary>
		public int GotoGuid(Guid guid)
		{
			ICmObject obj;
			if (!m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(guid, out obj))
			{
				return -1;
			}
			var ownerTree = new List<ICmObject>
			{
				obj
			};
			while (obj.Owner != null)
			{
				obj = obj.Owner;
				ownerTree.Add(obj);
			}

			var index = -1;
			for (var i = ownerTree.Count - 2; i >= 0; i--)
			{
				index = ExpandObject(ownerTree[i]);
			}

			return index;
		}

		/// <summary>
		/// Expands the row corresponding to the specified CmObject.
		/// </summary>
		private int ExpandObject(ICmObject obj)
		{
			for (var i = 0; i < Count; i++)
			{
				var rowObj = this[i].OriginalObject;
				if (rowObj == obj)
				{
					if (!IsExpanded(i))
					{
						base.ExpandObject(i);
					}

					return i;
				}

				if (!(rowObj is ILcmVector))
				{
					continue;
				}
				var index = FindObjInVector(obj, rowObj as ILcmVector);
				if (index < 0)
				{
					continue;
				}

				if (!IsExpanded(i))
				{
					base.ExpandObject(i);
				}

				index += (i + 1);
				if (!IsExpanded(index))
				{
					base.ExpandObject(index);
				}

				return index;
			}

			return -1;
		}

		/// <summary>
		/// Finds the index of the specified CmObject's guid in the specified ILcmVector.
		/// </summary>
		private int FindObjInVector(ICmObject obj, ILcmVector vect)
		{
			var guids = vect.ToGuidArray();
			for (var i = 0; i < guids.Length; i++)
			{
				if (obj.Guid == guids[i])
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Returns the object number only (as a string).
		/// </summary>
		private string GetObjectOnly(string objectName)
		{
			var idx = objectName.IndexOf(":");

			return (idx <= 0 ? string.Empty : objectName.Substring(idx + 1));
		}
	}
}