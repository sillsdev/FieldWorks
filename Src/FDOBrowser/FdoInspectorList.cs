using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Application;
using SIL.Utils;
using System.Reflection;
using SIL.FieldWorks.FDO.Infrastructure;
using System.Collections;
using SIL.ObjectBrowser;

namespace FDOBrowser
{
	#region FdoInspectorList class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FdoInspectorList : GenericInspectorObjectList
	{
		private FdoCache m_cache;
		private IFwMetaDataCacheManaged m_mdc;
		private Dictionary<CellarPropertyType, string> m_fldSuffix;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not to use the field definitions
		/// found in the meta data cache when querying an object for it's properties. If this
		/// value is false, then all of the properties found via .Net reflection are loaded
		/// into inspector objects. Otherwise, only those properties specified in the meta
		/// data cache are loaded.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool UseMetaDataCache { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FdoInspectorList"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoInspectorList(FdoCache cache)
		{
			m_cache = cache;
			m_mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			m_fldSuffix = new Dictionary<CellarPropertyType, string>();
			m_fldSuffix[CellarPropertyType.OwningCollection] = "OC";
			m_fldSuffix[CellarPropertyType.OwningSequence] = "OS";
			m_fldSuffix[CellarPropertyType.ReferenceCollection] = "RC";
			m_fldSuffix[CellarPropertyType.ReferenceSequence] = "RS";
			m_fldSuffix[CellarPropertyType.OwningAtomic] = "OA";
			m_fldSuffix[CellarPropertyType.ReferenceAtomic] = "RA";
		}

		#region overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the list using the specified top level object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void Initialize(object topLevelObj)
		{
			Initialize(topLevelObj, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the list using the specified top level object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Initialize(object topLevelObj,
			Dictionary<object, IInspectorObject> iobectsToKeep)
		{
			base.Initialize(topLevelObj);

			foreach (IInspectorObject io in this)
			{
				if (io.Object != null && io.Object.GetType().GetInterface("IRepository`1") != null)
				{
					PropertyInfo pi = io.Object.GetType().GetProperty("Count");
					int count = (int)pi.GetValue(io.Object, null);
					io.DisplayValue = FormatCountString(count);
					io.DisplayName = io.DisplayType;
					io.HasChildren = (count > 0);
				}
			}

			Sort(CompareInspectorObjectNames);

			if (iobectsToKeep != null)
				SwapInspectorObjects(iobectsToKeep);
		}

		private void SwapInspectorObjects(Dictionary<object, IInspectorObject> iobectsToKeep)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of IInspectorObject objects for the properties of the specified object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override List<IInspectorObject> GetInspectorObjects(object obj, int level)
		{
			if (obj != null)
			{
				object tmpObj = obj;
				IInspectorObject io = obj as IInspectorObject;
				if (io != null)
					tmpObj = io.Object;

				if (tmpObj != null)
				{
					if (tmpObj.GetType().GetInterface("IRepository`1") != null)
						return GetInspectorObjectsForRepository(tmpObj, io, level);

					if (tmpObj is IMultiAccessorBase)
						return GetInspectorObjectsForMultiString(tmpObj as IMultiAccessorBase, io, level);

					if (ObjectBrowser.m_virtualFlag == false && io != null && io.ParentInspectorObject != null &&
						io.ParentInspectorObject.DisplayName == "Values" &&
						io.ParentInspectorObject.ParentInspectorObject.DisplayType == "MultiUnicodeAccessor")
						return GetInspectorObjectsForUniRuns(tmpObj as ITsString, io as IInspectorObject, level);

					if (tmpObj is ITsString)
						return GetInspectorObjectsForTsString(tmpObj as ITsString, io, level);

					if (ObjectBrowser.m_virtualFlag == false && tmpObj is FDOBrowser.TextProps)
						return GetInspectorObjectsForTextProps(tmpObj as TextProps, io, level);

					if (ObjectBrowser.m_virtualFlag == false && io != null && io.DisplayName == "Values" &&
						(io.ParentInspectorObject.DisplayType == "MultiUnicodeAccessor" ||
						 io.ParentInspectorObject.DisplayType == "MultiStringAccessor"))
						return GetInspectorObjectsForValues(tmpObj, io as IInspectorObject, level);

					if (io != null && io.DisplayName.EndsWith("RC") && io.Flid > 0 && m_mdc.IsCustom(io.Flid))
						return GetInspectorObjectsForCustomRC(tmpObj, io, level);
				}
			}

			return BaseGetInspectorObjects(obj, level);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the inspector objects for the specified repository object;
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private List<IInspectorObject> GetInspectorObjectsForRepository(object obj,
			IInspectorObject ioParent, int level)
		{
			int i = 0;
			List<IInspectorObject> list = new List<IInspectorObject>();
			foreach (object instance in GetRepositoryInstances(obj))
			{
				IInspectorObject io = CreateInspectorObject(instance, obj, ioParent, level);

				if (ObjectBrowser.m_virtualFlag == false && obj.ToString().IndexOf("LexSenseRepository") > 0)
				{
					ILexSense tmpObj = io.Object as ILexSense;
					io.DisplayValue = tmpObj.FullReferenceName.Text;
					io.DisplayName = string.Format("[{0}]: {1}", i++, GetObjectOnly(tmpObj.ToString()));
				}
				else if (ObjectBrowser.m_virtualFlag == false && obj.ToString().IndexOf("LexEntryRepository") > 0 )
				{
					ILexEntry tmpObj = io.Object as ILexEntry;
					io.DisplayValue = tmpObj.HeadWord.Text;
					io.DisplayName = string.Format("[{0}]: {1}", i++, GetObjectOnly(tmpObj.ToString()));
				}
				else
					io.DisplayName = string.Format("[{0}]", i++);

				list.Add(io);
			}

			i = IndexOf(obj);
			if (i >= 0)
			{
				this[i].DisplayValue = FormatCountString(list.Count);
				this[i].HasChildren = (list.Count > 0);
			}

			return list;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process lines that have a DateTime type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private List<IInspectorObject> GetInspectorObjectForDateTime(DateTime tmpObj,
			IInspectorObject ioParent, int level)
		{
			List<IInspectorObject> list = new List<IInspectorObject>();

			IInspectorObject io = CreateInspectorObject(tmpObj, null, ioParent, level);
			//io.DisplayName = tmpObj.;
			//io.DisplayValue = FormatCountString(allStrings.Count);
			io.HasChildren = false;
			list.Add(io);

			return list;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the inspector objects for the specified MultiString.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private List<IInspectorObject> GetInspectorObjectsForMultiString(IMultiAccessorBase msa,
			IInspectorObject ioParent, int level)
		{
			List<IInspectorObject> list = new List<IInspectorObject>();
			int ws;

			if (ObjectBrowser.m_virtualFlag == false)
				list = GetMultiStringInspectorObjects(msa, ioParent, level);
			else
				list = BaseGetInspectorObjects(msa, level);

			Dictionary<int, string> allStrings = new Dictionary<int, string>();
			try
			{
				// Put this in a try/catch because VirtualStringAccessor
				// didn't implement StringCount when this was written.
				for (int i = 0; i < msa.StringCount; i++)
				{
					ITsString tss = msa.GetStringFromIndex(i, out ws);
					allStrings[ws] = tss.Text;
				}
			}
			catch { }

			if (ObjectBrowser.m_virtualFlag == true)
			{
				IInspectorObject io = CreateInspectorObject(allStrings, msa, ioParent, level);
				io.DisplayName = "AllStrings";
				io.DisplayValue = FormatCountString(allStrings.Count);
				io.HasChildren = (allStrings.Count > 0);
				list.Insert(0, io);
				list.Sort((x, y) => x.DisplayName.CompareTo(y.DisplayName));
			}
			return list;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of IInspectorObject objects (same as base), but includes s lot of
		/// specifics if you choose not to see virtual fields.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private List<IInspectorObject> GetMultiStringInspectorObjects(object obj, IInspectorObject ioParent, int level)
		{
			if (ioParent != null)
				obj = ioParent.Object;

			List<IInspectorObject> list = new List<IInspectorObject>();

			ICollection collection = obj as ICollection;
			if (collection != null)
			{
				int i = 0;
				foreach (object item in collection)
				{
					IInspectorObject io = CreateInspectorObject(item, obj, ioParent, level);

					io.DisplayName = string.Format("[{0}]", i++);
					list.Add(io);
				}

				return list;
			}

			PropertyInfo[] props = GetPropsForObj(obj);
			foreach (PropertyInfo pi in props)
			{
				try
				{
					object propObj = pi.GetValue(obj, null);
					IInspectorObject Itmp = CreateInspectorObject(pi, propObj, obj, ioParent, level);

					if ((obj.ToString().IndexOf("MultiUnicodeAccessor") > 0 && Itmp.DisplayName != "Values") ||
						(obj.ToString().IndexOf("MultiStringAccessor") > 0  && Itmp.DisplayName != "Values"))
						continue;
					else
					{
						ICollection Itmp4 = Itmp.Object as ICollection;
						if (Itmp4 != null)
						{
							Itmp.DisplayValue = "Count = " + Itmp4.Count;
							Itmp.HasChildren = (Itmp4.Count > 0);
						}
					}
					list.Add(Itmp);
				}
				catch (Exception e)
				{
					continue;
				}
			}

			list.Sort(CompareInspectorObjectNames);
			return list;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the inspector objects for the specified TsString.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private List<IInspectorObject> GetInspectorObjectsForTsString(ITsString tss,
			IInspectorObject ioParent, int level)
		{
			List<IInspectorObject> list = new List<IInspectorObject>();

			int runCount = tss.RunCount;

			List<TsStringRunInfo> tssriList = new List<TsStringRunInfo>();
			for (int i = 0; i < runCount; i++)
				tssriList.Add(new TsStringRunInfo(i, tss, m_cache));

			IInspectorObject io = CreateInspectorObject(tssriList, tss, ioParent, level);

			io.DisplayName = "Runs";
			io.DisplayValue = FormatCountString(tssriList.Count);
			io.HasChildren = (tssriList.Count > 0);
			list.Add(io);

			if (ObjectBrowser.m_virtualFlag == true)
			{
				io = CreateInspectorObject(tss.Length, tss, ioParent, level);

				io.DisplayName = "Length";
				list.Add(io);

				io = CreateInspectorObject(tss.Text, tss, ioParent, level);

				io.DisplayName = "Text";
				list.Add(io);
			}

			return list;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the inspector objects for the specified TextProps.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private List<IInspectorObject> GetInspectorObjectsForTextProps(TextProps txp,
			IInspectorObject ioParent, int level)
		{
			int saveIntPropCount = 0, saveStrPropCount = 0;

			//IInspectorObject ioParent = txp as IInspectorObject;
			if (ioParent != null)
				txp = ioParent.Object as TextProps;

			List<IInspectorObject> list = new List<IInspectorObject>();
			IInspectorObject io;
			ICollection txp1 = txp as ICollection;

			if (txp1 != null)
			{
				int i = 0;
				foreach (object item in txp1)
				{
					io = CreateInspectorObject(item, txp, ioParent, level);
					io.DisplayName = string.Format("[{0}]", i++);
					list.Add(io);
				}

				return list;
			}

			PropertyInfo[] props = GetPropsForObj(txp);
			foreach (PropertyInfo pi in props)
			{
				if (pi.Name != "IntProps" && pi.Name != "StrProps" && pi.Name != "IntPropCount" && pi.Name != "StrPropCount")
					continue;
				else
					switch (pi.Name)
					{
						case "IntProps":
							object propObj = pi.GetValue(txp, null);
							io = CreateInspectorObject(pi, propObj, txp, ioParent, level);
							io.DisplayValue = "Count = " + saveIntPropCount.ToString();
							io.HasChildren = (saveIntPropCount > 0);
							list.Add(io);
							break;
						case "StrProps":
							object propObj1 = pi.GetValue(txp, null);
							io = CreateInspectorObject(pi, propObj1, txp, ioParent, level);
							io.DisplayValue = "Count = " + saveStrPropCount.ToString();
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of IInspectorObject objects representing all the properties for the
		/// specified object, which is assumed to be at the specified level.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual List<IInspectorObject> GetInspectorObjectsForValues(object obj, IInspectorObject ioParent, int level)
		{
			if (ioParent != null)
				obj = ioParent.Object;

			List<IInspectorObject> list = new List<IInspectorObject>();

			IMultiAccessorBase multiStr = ioParent.OwningObject as IMultiAccessorBase;
			if (multiStr != null)
			{
				foreach (int ws in multiStr.AvailableWritingSystemIds)
				{
					IWritingSystem wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(ws);
					IInspectorObject ino = CreateInspectorObject(multiStr.get_String(ws), obj, ioParent, level);
					ino.DisplayName = wsObj.DisplayLabel;
					list.Add(ino);
				}
				return list;
			}

			PropertyInfo[] props = GetPropsForObj(obj);
			foreach (PropertyInfo pi in props)
			{
				try
				{
					object propObj = pi.GetValue(obj, null);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Condenses the 'Run' information for MultiUnicodeAccessor entries because
		/// there will only be 1 run,
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual List<IInspectorObject> GetInspectorObjectsForUniRuns(ITsString obj, IInspectorObject ioParent, int level)
		{
			List<IInspectorObject> list = new List<IInspectorObject>();

			if (obj != null)
			{
				IInspectorObject ino = CreateInspectorObject(obj, ioParent.OwningObject, ioParent, level);

				ino.DisplayName = "Writing System";
				ino.DisplayValue = obj.get_WritingSystemAt(0).ToString();
				ino.HasChildren = false;
				list.Add(ino);

				TsStringRunInfo tss = new TsStringRunInfo(0, obj, m_cache);

				ino = CreateInspectorObject(tss, obj, ioParent, level);

				ino.DisplayName = "Text";
				ino.DisplayValue = tss.Text;
				ino.HasChildren = false;
				list.Add(ino);
			}
			return list;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the reference collectiomn list for ther custom reference collection.,
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual List<IInspectorObject> GetInspectorObjectsForCustomRC(object obj, IInspectorObject ioParent, int level)
		{
			List<IInspectorObject> list = new List<IInspectorObject>();
			int n = 0;
			int hvoNum = 0;
			;
			if (obj == null)
				return null;

			// Inspectors for custom reference collections are supposed to be configured with
			// obj being an array of the HVOs.
			var collection = obj as ICollection;

			if (collection == null)
			{
				MessageBox.Show("Custom Reference collection not properly configured with array of HVOs");
				return null;
			}
			// Just like an ordinary reference collection, we want to make one inspector for each
			// item in the collection, where the first argument to CreateInspectorObject is the
			// cmObject. Keep this code in sync with BaseGetInspectorObjects.
			foreach (int hvoItem in collection)
			{
				hvoNum = Int32.Parse(hvoItem.ToString());
				var objItem = m_cache.ServiceLocator.GetObject(hvoNum);
					IInspectorObject io = CreateInspectorObject(objItem, obj, ioParent, level);
					io.DisplayName = string.Format("[{0}]", n++);
					list.Add(io);
			}
			return list;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of IInspectorObject objects representing all the properties for the
		/// specified object, which is assumed to be at the specified level.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual List<IInspectorObject> BaseGetInspectorObjects(object obj, int level)
		{
			IInspectorObject ioParent = obj as IInspectorObject;
			IInspectorObject io1;

			if (ioParent != null)
				obj = ioParent.Object;

			List<IInspectorObject> list = new List<IInspectorObject>();

			ICollection collection = obj as ICollection;
			if (collection != null)
			{
				int i = 0;
				foreach (object item in collection)
				{
					IInspectorObject io = CreateInspectorObject(item, obj, ioParent, level);
					io.DisplayName = string.Format("[{0}]", i++);
					list.Add(io);
				}

				return list;
			}

			PropertyInfo[] props = GetPropsForObj(obj);
			foreach (PropertyInfo pi in props)
			{
				try
				{
					object propObj = pi.GetValue(obj, null);
					io1 = CreateInspectorObject(pi, propObj, obj, ioParent, level);
					if (io1.DisplayType == "System.DateTime")
						io1.HasChildren = false;
					list.Add(io1);
				}
				catch (Exception e)
				{
					list.Add(CreateExceptionInspectorObject(e, obj, pi.Name, level, ioParent));
				}
			}

			if (FDOBrowserForm.CFields != null && FDOBrowserForm.CFields.Count > 0 && obj != null)
				foreach (CustomFields cf2 in FDOBrowserForm.CFields)
				{
					if (obj.ToString().Contains(m_mdc.GetClassName(cf2.ClassID)))
					{
						IInspectorObject io = CreateCustomInspectorObject(obj, ioParent, level, cf2);
						list.Add(io);
					}
				}

			list.Sort(CompareInspectorObjectNames);
			return list;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the properties specified in the meta data cache for the specified object .
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override PropertyInfo[] GetPropsForObj(object obj)
		{
			if (m_mdc != null && obj is ICmObject && UseMetaDataCache)
				return GetFieldsFromMetaDataCache(obj as ICmObject);

			PropertyInfo[] propArray = base.GetPropsForObj(obj);

			ICmObject cmObj = obj as ICmObject;

			List<PropertyInfo> props = new List<PropertyInfo>(propArray);

			if (m_mdc == null || cmObj == null)
				return propArray;

			RevisePropsList(cmObj, ref props);
			return props.ToArray();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the fields from meta data cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private PropertyInfo[] GetFieldsFromMetaDataCache(ICmObject cmObj)
		{
			List<PropertyInfo> props = new List<PropertyInfo>();
			if (cmObj != null)
			{
				int[] flids = m_mdc.GetFields(cmObj.ClassID, true, (int)CellarPropertyTypeFilter.All);
				BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty;

				// Get only the fields for the object that are specified in the meta data cache.
				foreach (int flid in flids)
				{
					string fieldName = m_mdc.GetFieldName(flid);
					CellarPropertyType fieldType = (CellarPropertyType)m_mdc.GetFieldType(flid);

					string suffix;
					if (m_fldSuffix.TryGetValue(fieldType, out suffix))
						fieldName += suffix;

					PropertyInfo pi = cmObj.GetType().GetProperty(fieldName, flags);
					if (pi != null)
						props.Add(pi);
				}
			}

			return (props.Count > 0 ? props.ToArray() : base.GetPropsForObj(cmObj));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create InspectorObjects for the custom fields for the current object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IInspectorObject CreateCustomInspectorObject(object obj, IInspectorObject ParentIo,
								 int level, CustomFields cf)
		{
			ICmObject to = obj as ICmObject;
			var mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			ICmPossibility fColl = null;
			int mws = 0;
			var iValue = "";
			string className = mdc.GetClassName(cf.ClassID);
			int Flid = cf.FieldID;
			IInspectorObject io = null;
			if (obj != null)
			{
				switch (cf.Type)
				{
					case "ITsString":
						ITsString oValue = m_cache.DomainDataByFlid.get_StringProp(to.Hvo, Flid);
						io = base.CreateInspectorObject(null, oValue, obj, ParentIo, level);
						iValue = oValue.Text;
						io.HasChildren = false;
						io.DisplayName = cf.Name;
						break;
					case "System.Int32":
						int sValue = m_cache.DomainDataByFlid.get_IntProp(to.Hvo, Flid);
						io = base.CreateInspectorObject(null, sValue, obj, ParentIo, level);
						iValue = sValue.ToString();
						io.HasChildren = false;
						io.DisplayName = cf.Name;
						break;
					case "SIL.FieldWorks.Common.FwUtils.GenDate":
						// tried get_TimeProp, get_UnknowbProp, get_Prop
						GenDate genObj = ((ISilDataAccessManaged)m_cache.DomainDataByFlid).get_GenDateProp(to.Hvo, Flid);
						io = base.CreateInspectorObject(null, genObj, obj, ParentIo, level);
						iValue = genObj.ToString();
						io.HasChildren = true;
						io.DisplayName = cf.Name;
						break;
					case "FdoReferenceCollection<ICmPossibility>":	// ReferenceCollection
						int count = m_cache.DomainDataByFlid.get_VecSize(to.Hvo, Flid);
						iValue = "Count = " + count.ToString();
						var objects = ((ISilDataAccessManaged) m_cache.DomainDataByFlid).VecProp(to.Hvo, Flid);
						objects.Initialize();
						//int rcHvo = m_cache.DomainDataByFlid.get_ObjectProp(to.Hvo, Flid);
						//IFdoReferenceCollection<ICmPossibility> RCObj = (rcHvo == 0? null: (IFdoReferenceCollection<ICmPossibility>)m_cache.ServiceLocator.GetObject(rcHvo));
						io = base.CreateInspectorObject(null, objects, obj, ParentIo, level);
						io.HasChildren = (count > 0? true: false);
						io.DisplayName = cf.Name+ "RC";
						break;
					case "ICmPossibility":	// ReferenceAtomic
						int rValue = m_cache.DomainDataByFlid.get_ObjectProp(to.Hvo, Flid);
						ICmPossibility posObj = (rValue == 0? null: (ICmPossibility)m_cache.ServiceLocator.GetObject(rValue));
						io = base.CreateInspectorObject(null, posObj, obj, ParentIo, level);
						iValue = (posObj == null? "null": posObj.NameHierarchyString);
						io.HasChildren = (posObj == null? false: true);
						io.DisplayName = cf.Name+ "RA";
						break;
					case "IStText":	//    multi-paragraph text (OA) StText)
						int mValue = m_cache.DomainDataByFlid.get_ObjectProp(to.Hvo, Flid);
						IStText paraObj = (mValue == 0? null: (IStText)m_cache.ServiceLocator.GetObject(mValue));
						io = base.CreateInspectorObject(null, paraObj, obj, ParentIo, level);
						iValue = (paraObj == null? "null": "StText: " + paraObj.Hvo.ToString());
						io.HasChildren = (mValue > 0? true: false);
						io.DisplayName = cf.Name + "OA";
						break;
					default:
						MessageBox.Show(string.Format("The type of the custom field is {0}", cf.Type));
						break;
				}
			}

			io.DisplayType = cf.Type;
			io.DisplayValue = (iValue ?? "null");
			io.Flid = cf.FieldID;

			return io;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes properties from the specified list of properties, those properties the
		/// user has specified he doesn't want to see in the browser.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RevisePropsList(ICmObject cmObj, ref List<PropertyInfo> props)
		{
			int indx = 0, flid = 0;
			string work = "";

			if (cmObj == null)
				return;

			for (int i = props.Count - 1; i >= 0; i--)
			{
				if (props[i].Name == "Guid")
					continue;

				if (!FDOClassList.IsPropertyDisplayed(cmObj, props[i].Name))
				{
					props.RemoveAt(i);
					continue;
				}

				work = FDOBrowserForm.StripOffTypeChars(props[i].Name);

				try
				{
					flid = m_mdc.GetFieldId2(cmObj.ClassID, work, true);
				}
				catch (FDOInvalidFieldException)
				{
					if (ObjectBrowser.m_virtualFlag == false)
					{
						props.RemoveAt(i);
						continue;
					}
				}

				if (ObjectBrowser.m_virtualFlag == false && flid >= 20000000 && flid < 30000000)
				{
					props.RemoveAt(i);
					continue;
				}

				if (ObjectBrowser.m_virtualFlag == false && m_mdc.get_IsVirtual(flid))
				{
					props.RemoveAt(i);
					continue;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an inspector object for the specified property info., checking for various
		/// FDO interface types.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override IInspectorObject CreateInspectorObject(PropertyInfo pi,
			object obj, object owningObj, IInspectorObject ioParent, int level)
		{
			IInspectorObject io = base.CreateInspectorObject(pi, obj, owningObj, ioParent, level);

			if (pi == null && io != null)
				io.DisplayType = StripOffFDONamespace(io.DisplayType);

			else if (pi != null && io == null)
				io.DisplayType = pi.PropertyType.Name;

			else if (pi != null && io != null)
				io.DisplayType = (io.DisplayType == "System.__ComObject" ?
				pi.PropertyType.Name : StripOffFDONamespace(io.DisplayType));

			if (obj == null)
				return io;

			if (obj is char)
			{
				io.DisplayValue = string.Format("'{0}'   (U+{1:X4})", io.DisplayValue, (int)((char)obj));
				return io;
			}

			if (obj is IFdoVector)
			{
				MethodInfo mi = obj.GetType().GetMethod("ToArray");
				try
				{
					ICmObject[] array = mi.Invoke(obj, null) as ICmObject[];
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

			string fmtAppend = "{0}, {{{1}}}";
			string fmtReplace = "{0}";
			string fmtStrReplace = "\"{0}\"";

			if (obj is ICmFilter)
			{
				ICmFilter filter = (ICmFilter)obj;
				io.DisplayValue = string.Format(fmtAppend, io.DisplayValue, filter.Name);
			}
			else if (obj is IMultiAccessorBase)
			{
				IMultiAccessorBase str = (IMultiAccessorBase)obj;
				io.DisplayValue = string.Format(fmtReplace,
					str.AnalysisDefaultWritingSystem.Text);
			}
			else if (obj is ITsString)
			{
				ITsString str = (ITsString)obj;
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
				IPhNCSegments seg = (IPhNCSegments)obj;
				io.DisplayValue = string.Format(fmtAppend, io.DisplayValue,
					seg.Name.AnalysisDefaultWritingSystem.Text);
			}
			else if (obj is IPhEnvironment)
			{
				IPhEnvironment env = (IPhEnvironment)obj;
				io.DisplayValue = string.Format("{0}, {{Name: {1}, Pattern: {2}}}",
					io.DisplayValue, env.Name.AnalysisDefaultWritingSystem.Text,
					env.StringRepresentation.Text);
			}
			else if (obj is IMoEndoCompound)
			{
				IMoEndoCompound moendo = (IMoEndoCompound)obj;
				io.DisplayValue = string.Format(fmtAppend, io.DisplayValue,
					moendo.Name.AnalysisDefaultWritingSystem.Text);
			}
			else if (obj.GetType().GetInterface("IRepository`1") != null)
			{
				io.DisplayName = io.DisplayType;
			}

			return io;
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string StripOffFDONamespace(string type)
		{
			if (string.IsNullOrEmpty(type))
				return string.Empty;

			if (!type.StartsWith("SIL.FieldWorks.FDO"))
				return type;

			type = type.Replace("SIL.FieldWorks.FDO.", string.Empty);
			type = type.Replace("DomainImpl.", string.Empty);
			type = type.Replace("Infrastructure.", string.Empty);
			type = type.Replace("Impl.", string.Empty);

			return CleanupGenericListType(type);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of all the instances in the specified repository.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private List<object> GetRepositoryInstances(object repository)
		{
			List<object> list = new List<object>();

			try
			{
				// Get an object that represents all the repository's collection of instances
				object repoInstances = null;
				foreach (MethodInfo mi in repository.GetType().GetMethods())
				{
					if (mi.Name == "AllInstances")
					{
						repoInstances = mi.Invoke(repository, null);
						break;
					}
				}

				if (repoInstances == null)
				{
					throw new MissingMethodException(string.Format(
						"Repository {0} is missing 'AllInstances' method.", repository.GetType().Name));
				}

				IEnumerable ienum = repoInstances as IEnumerable;
				if (ienum == null)
				{
					throw new NullReferenceException(string.Format(
						"Repository {0} is not an IEnumerable", repository.GetType().Name));
				}

				IEnumerator enumerator = ienum.GetEnumerator();
				while (enumerator.MoveNext())
					list.Add(enumerator.Current);
			}
			catch (Exception e)
			{
				list.Add(e);
			}

			return list;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the item in the list having the specified hvo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int GotoGuid(Guid guid)
		{
			try
			{
				ICmObject obj =
					m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(guid);

				List<ICmObject> ownerTree = new List<ICmObject>();
				ownerTree.Add(obj);
				while (obj.Owner != null)
				{
					obj = obj.Owner;
					ownerTree.Add(obj);
				}

				int index = -1;
				for (int i = ownerTree.Count - 2; i >= 0; i--)
					index = ExpandObject(ownerTree[i]);

				return index;
			}
			catch { }

			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expands the row corresponding to the specified CmObject.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int ExpandObject(ICmObject obj)
		{
			for (int i = 0; i < Count; i++)
			{
				object rowObj = this[i].OriginalObject;
				if (rowObj == obj)
				{
					if (!IsExpanded(i))
						base.ExpandObject(i);

					return i;
				}
				else if (rowObj is IFdoVector)
				{
					int index = FindObjInVector(obj, rowObj as IFdoVector);
					if (index >= 0)
					{
						if (!IsExpanded(i))
							base.ExpandObject(i);

						index += (i + 1);
						if (!IsExpanded(index))
							base.ExpandObject(index);

						return index;
					}
				}
			}

			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the index of the specified CmObject's guid in the specified IFdoVector.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int FindObjInVector(ICmObject obj, IFdoVector vect)
		{
			Guid[] guids = vect.ToGuidArray();
			for (int i = 0; i < guids.Length; i++)
			{
				if (obj.Guid == guids[i])
					return i;
			}

			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returbs the object number only (as a string).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string GetObjectOnly(string objectName)
		{
			int idx = objectName.IndexOf(":");

			return (idx <= 0 ? "" : objectName.Substring(idx + 1));
		}}

	#endregion

	#region TsStringRunInfo class
	/// ------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class TsStringRunInfo
	{
		/// <summary></summary>
		public string Text { get; set; }
		/// <summary></summary>
		public TextProps TextProps { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TsStringRunInfo"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TsStringRunInfo(int irun, ITsString tss, FdoCache cache)
		{
			Text = "\"" + (tss.get_RunText(irun) ?? string.Empty) + "\"";
			TextProps = new TextProps(irun, tss, cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents this instance.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return (Text ?? string.Empty);
		}
	}

	#endregion

	#region TextProps
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TextProps
	{
		/// <summary></summary>
		public int StrPropCount { get; set; }
		/// <summary></summary>
		public int IntPropCount { get; set; }
		/// <summary></summary>
		public int IchMin { get; set; }
		/// <summary></summary>
		public int IchLim { get; set; }
		/// <summary></summary>
		public TextStrPropInfo[] StrProps { get; set; }
		/// <summary></summary>
		public TextIntPropInfo[] IntProps { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TextProps"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TextProps(int irun, ITsString tss, FdoCache cache)
		{
			TsRunInfo runinfo;
			ITsTextProps ttp = tss.FetchRunInfo(irun, out runinfo);
			IchMin = runinfo.ichMin;
			IchLim = runinfo.ichLim;
			SetProps(ttp, cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TextProps"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TextProps(ITsTextProps ttp, FdoCache cache)
		{
			StrPropCount = ttp.StrPropCount;
			IntPropCount = ttp.IntPropCount;
			SetProps(ttp, cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the int and string properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetProps(ITsTextProps ttp, FdoCache cache)
		{
			// Get the string properties.
			StrPropCount = ttp.StrPropCount;
			StrProps = new TextStrPropInfo[StrPropCount];
			for (int i = 0; i < StrPropCount; i++)
				StrProps[i] = new TextStrPropInfo(ttp, i);

			// Get the integer properties.
			IntPropCount = ttp.IntPropCount;
			IntProps = new TextIntPropInfo[IntPropCount];
			for (int i = 0; i < IntPropCount; i++)
				IntProps[i] = new TextIntPropInfo(ttp, i, cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return string.Format(
				"{{IchMin={0}, IchLim={1}, StrPropCount={2}, IntPropCount={3}}}",
				IchMin, IchLim, StrPropCount, IntPropCount);
		}
	}

	#endregion

	#region TextStrPropInfo class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TextStrPropInfo
	{
		/// <summary></summary>
		public FwTextPropType Type { get; set; }
		/// <summary></summary>
		public string Value { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TextStrPropInfo"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TextStrPropInfo(ITsTextProps props, int iprop)
		{
			int tpt;
			Value = props.GetStrProp(iprop, out tpt);
			Type = (FwTextPropType)tpt;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			 return Value + "  (" + Type + ")";
		}
	}

	#endregion

	#region TextIntPropInfo class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TextIntPropInfo
	{
		/// <summary></summary>
		public FwTextPropVar Variant { get; set; }
		/// <summary></summary>
		public FwTextPropType Type { get; set; }
		/// <summary></summary>
		public int Value { get; set; }

		private string m_toStringValue = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TextIntPropInfo"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TextIntPropInfo(ITsTextProps props, int iprop, FdoCache cache)
		{
			int nvar;
			int tpt;
			Value = props.GetIntProp(iprop, out tpt, out nvar);
			Type = (FwTextPropType)tpt;
			Variant = (FwTextPropVar)nvar;

			m_toStringValue = Value + "  (" + Type + ")";

			if (tpt == (int)FwTextPropType.ktptWs)
			{
				IWritingSystem ws = cache.ServiceLocator.WritingSystemManager.Get(Value);
				m_toStringValue += "  {" + ws + "}";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return m_toStringValue;
		}
	}

	#endregion
}
