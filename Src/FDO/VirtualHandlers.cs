// --------------------------------------------------------------------------------------------
// Copyright (C) 2002 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: VirtualHandlers.cs
// Responsibility: John Thomson
//
// <remarks>
// Implementation of:
//		Various implementatations of IVirtualHandler, some mainly useful for demos,
//		eventually I hope some of genuine usefulness.
//
// Mainly Demo:
//		StringPropLengthVh: ComputeEveryTime handler for length of string property.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Xml;
using SIL.FieldWorks.Common.Utils;

using SIL.FieldWorks.FDO.Cellar; // for property types.
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls; // for ProgressState
using SIL.Utils;

using SIL.FieldWorks.FDO.LangProj;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// a handler which uses reflection to get a TString C# property, e.g. ShortNameTS
	/// </summary>
	public class TSStringPropertyVirtualHandler : BaseVirtualHandler
	{
		private FdoCache m_cache;
		// Note: Only one of these should be non-null, after the Constructor gets done with its work.
		private PropertyInfo m_propertyInfo;
		private MethodInfo m_methodInfo;

		/// <summary>
		/// constructor
		/// </summary>
		/// <param name="configuration">the XML that configures this handler</param>
		/// <param name="cache"></param>
		public TSStringPropertyVirtualHandler(XmlNode configuration, FdoCache cache) : base(configuration)
		{
			m_cache = cache;
			SetupDependencies(configuration, cache);
			int wsid = this.WsId(cache, 0, 0);
			Type fdoObjType = GetTypeFromXml(configuration);
			string propertyName = XmlUtils.GetManditoryAttributeValue(configuration, "virtualfield");
			if (wsid > 0)
			{
				// Try to find a method that appends 'ForWs' to the property name.
				// It will need to take one int parameter for the ws id, and return an ITSString, as well, in order to work here.
				Type[] parms = new Type[1];
				parms[0] = typeof(int);
				m_methodInfo = fdoObjType.GetMethod(propertyName + "ForWs", parms);
				if (m_methodInfo != null && m_methodInfo.ReturnType != typeof(ITsString))
					m_methodInfo = null;
				Writeable = false;
			}

			if (m_methodInfo == null)
			{
				m_propertyInfo = fdoObjType.GetProperty(propertyName);
				Debug.Assert(m_propertyInfo.PropertyType == typeof(ITsString));
				Writeable = m_propertyInfo.CanWrite;
			}
			Type = (int)CellarModuleDefns.kcptString;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			// JohnT: this is often used in sorting and filtering, it is usually way too expensive
			// to either validate or preload the object. This can cost several queries per item
			// in a 32K item list!
			ICmObject obj = CmObject.CreateFromDBObject(m_cache, hvo, false);
			ITsString tss;
			if (m_methodInfo != null)
			{
				object[] parm = new object[1];
				parm[0] = this.WsId(m_cache, hvo, ws);
				tss = (ITsString)m_methodInfo.Invoke(obj, parm);
			}
			else
			{
				tss = (ITsString)m_propertyInfo.GetValue(obj, null);
			}
			cda.CacheStringProp(hvo, tag, tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}
	}
	/// <summary>
	/// a handler which uses reflection to get a TString from a C# method that takes a ws argument.
	/// The virtualField name gets appended with "ForWs" to get a method that must take an integer
	/// argument and return a TsString.
	/// </summary>
	public class MLStringPropertyVirtualHandler : BaseVirtualHandler
	{
		private FdoCache m_cache;
		private MethodInfo m_methodInfo;

		/// <summary>
		/// constructor
		/// </summary>
		/// <param name="configuration">the XML that configures this handler</param>
		/// <param name="cache"></param>
		public MLStringPropertyVirtualHandler(XmlNode configuration, FdoCache cache) : base(configuration)
		{
			m_cache = cache;
			SetupDependencies(configuration, cache);
			Type fdoObjType = GetTypeFromXml(configuration);
			string methodName = XmlUtils.GetOptionalAttributeValue(configuration, "method");
			if (methodName == null)
			{
				// Try to find a method that appends 'ForWs' to the virtual field name.
				methodName = XmlUtils.GetManditoryAttributeValue(configuration, "virtualfield") + "ForWs";
			}
			// It will need to take one int parameter for the ws id, and return an ITSString, as well, in order to work here.
			Type[] parms = new Type[1];
			parms[0] = typeof(int);
			m_methodInfo = fdoObjType.GetMethod(methodName, parms);
			if (m_methodInfo != null && m_methodInfo.ReturnType != typeof(ITsString))
				m_methodInfo = null;
			Writeable = false;
			Type = (int)CellarModuleDefns.kcptMultiString;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			ICmObject obj = CmObject.CreateFromDBObject(m_cache, hvo);
			ITsString tss;
			object[] parm = new object[1];
			parm[0] = ws;
			tss = (ITsString)m_methodInfo.Invoke(obj, parm);
			if (tss != null)
			{
				cda.CacheStringAlt(hvo, tag, ws, tss);
			}
			else
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				cda.CacheStringAlt(hvo, tag, ws, tsf.MakeString("", ws));
			}
		}
	}

	/// <summary>
	/// a handler which uses reflection to get at the value of a computed integer property
	/// </summary>
	public class IntegerPropertyVirtualHandler : BaseFDOPropertyVirtualHandler
	{
		/// <summary>
		/// stored values from bulk loading due to SetLoadForAllOfClass(true).
		/// </summary>
		Dictionary<int, int> m_bulkValues;
		/// <summary>
		/// Flag whether we've actually tried to bulk load into m_bulkValues.
		/// </summary>
		bool m_fBulkLoaded;

		/// <summary>
		/// constructor
		/// </summary>
		/// <param name="configuration">the XML that configures this handler</param>
		/// <param name="cache"></param>
		public IntegerPropertyVirtualHandler(XmlNode configuration, FdoCache cache) : base(configuration, cache)
		{
			if (m_propertyInfo.PropertyType != typeof(int))
				throw new ArgumentException("Invalid FDO property for 'IntegerPropertyVirtualHandler'", "configuration");

			Type = (int)CellarModuleDefns.kcptInteger;
			Writeable = m_propertyInfo.CanWrite;
		}

		/// <summary>
		/// This method may be implemented to load everything at once, even when
		/// m_fComputeEveryTime is true.
		/// </summary>
		/// <param name="fLoadAll"></param>
		public override void SetLoadForAllOfClass(bool fLoadAll)
		{
			if (fLoadAll)
			{
				if (m_bulkValues == null)
				{
					m_bulkValues = new Dictionary<int, int>();
					m_fBulkLoaded = false;
				}
			}
			else
			{
				m_bulkValues = null;
				m_fBulkLoaded = false;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			if (BaseVirtualHandler.ForceBulkLoadIfPossible &&
				m_bulkMethodInfo != null && m_bulkValues == null)
			{
				SetLoadForAllOfClass(true);
				BaseVirtualHandler.m_rgvhBulkForced.Add(this);
			}
			ICmObject fdoObj = null;
			if (m_cache.IsDummyObject(hvo))
			{
				RequestConversionToRealEventArgs args = new RequestConversionToRealEventArgs(hvo, tag, m_configuration, false);
				OnRequestConversionToReal(this, args);
				fdoObj = args.RealObject;	// null, if no conversion took place.
			}
			if (fdoObj == null)
			{
				fdoObj = CmObject.CreateFromDBObject(m_cache, hvo);
			}
			int nVal = 0;
			if (m_bulkMethodInfo != null && m_bulkValues != null)
			{
				if (!m_fBulkLoaded)
				{
					m_bulkMethodInfo.Invoke(null, new object[] { m_cache, m_bulkValues });
					m_fBulkLoaded = true;
				}
				if (!m_bulkValues.TryGetValue(hvo, out nVal))
					nVal = 0;
			}
			else
			{
				nVal = (int)m_propertyInfo.GetValue(fdoObj, null);
			}
			cda.CacheIntProp(hvo, tag, nVal);
		}

		/// <summary>
		/// Write the integer by invoking the setter.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="val"></param>
		/// <param name="_sda"></param>
		public override void WriteInt64(int hvo, int tag, long val, ISilDataAccess _sda)
		{
			ICmObject fdoObj = CmObject.CreateFromDBObject(m_cache, hvo);
			m_propertyInfo.SetValue(fdoObj, (object)(int)val, null);
		}

	}

	/// <summary>
	/// a handler which simply stores whatever integer is put there, or returns zero if none has been.
	/// It is primarily intended for keeping track of whether PreloadShortName has occurred, though
	/// it could readily be extended (e.g., by using a bitmap) to indicate whether other preloads
	/// have been done. For now, the value is 0 if no preloading has been done and 1 if PreloadShortnames
	/// has occurred.
	/// </summary>
	public class PreloadPerformedVirtualHandler : BaseVirtualHandler
	{
		const string kClassName = "LexDb";
		const string kFieldName = "PreloadPerformed";
		/// <summary>
		/// constructor
		/// </summary>
		public PreloadPerformedVirtualHandler()
		{
			ClassName = kClassName;
			FieldName = kFieldName;
			Type = (int)CellarModuleDefns.kcptInteger;
			Writeable = true;
		}

		/// <summary>
		/// Test whether PreloadShortName has been performed on this lexical database, creating the virtual handler
		/// if necessary.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static bool PreloadPerformed(FdoCache cache)
		{
			IVwVirtualHandler vh = cache.VwCacheDaAccessor.GetVirtualHandlerName(kClassName, kFieldName);
			if (vh == null)
			{
				return false; // handler not even defined, can't have value set.
			}
			return cache.MainCacheAccessor.get_IntProp(cache.LangProject.LexDbOAHvo, vh.Tag) == 1;
		}

		/// <summary>
		/// Record that PreloadShortName has been performed for this database.
		/// </summary>
		/// <param name="cache"></param>
		public static void SetPreloadPerformed(FdoCache cache)
		{
			IVwVirtualHandler vh = cache.VwCacheDaAccessor.GetVirtualHandlerName(kClassName, kFieldName);
			if (vh == null)
			{
				vh = new PreloadPerformedVirtualHandler();
				cache.VwCacheDaAccessor.InstallVirtual(vh);
			}
			cache.MainCacheAccessor.SetInt(cache.LangProject.LexDbOAHvo, vh.Tag, 1);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			cda.CacheIntProp(hvo, tag, 0);
		}

		/// <summary>
		/// Write the integer...nothing to do, it's already written to the cache.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="val"></param>
		/// <param name="_sda"></param>
		public override void WriteInt64(int hvo, int tag, long val, ISilDataAccess _sda)
		{
		}
	}

	/// <summary>
	/// Summary description for VirtualHandlers.
	/// </summary>
	public class StringPropLengthVh : BaseVirtualHandler
	{
		FdoCache m_cache = null;
		int m_flid;
		int m_ws = 0;

		/// <summary>
		/// Construct one specifying the flid and ws (0 if not needed).
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="ws"></param>
		public StringPropLengthVh(int flid, int ws)
		{
			m_flid = flid;
			m_ws = ws;
			Type = (int)CellarModuleDefns.kcptInteger;
		}

		/// <summary>
		/// Initialize it from an XmlNode with attributes class, stringfield, and optionally ws.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="cache"></param>
		public StringPropLengthVh(XmlNode node, FdoCache cache) : base(node)
		{
			m_cache = cache;
			XmlAttribute xa = node.Attributes["modelclass"];
			if (xa == null)
				throw new Exception("modelclass attribute is required");
			string className = xa.Value;
			xa = node.Attributes["stringfield"];
			if (xa == null)
				throw new Exception("stringfield attribute is required");
			string fieldName = xa.Value;
			m_flid = (int)cache.MetaDataCacheAccessor.GetFieldId(className, fieldName, true);
			if (m_flid == 0)
				throw new Exception("Field " + fieldName + " of class " + className + " not known");
			m_ws = this.WsId(cache, 0, 0);
			Type = (int)CellarModuleDefns.kcptInteger;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		/// <summary>
		/// The value of this property is the length of the string property on which it is based.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			ISilDataAccess sda = cda as ISilDataAccess;
			ITsString tss;
			int wsId = this.WsId(m_cache, hvo, ws != 0 ? ws : m_ws);
			if (wsId == 0)
				tss = sda.get_StringProp(hvo, m_flid);
			else
				tss = sda.get_MultiStringAlt(hvo, m_flid, wsId);
			cda.CacheIntProp(hvo, tag, tss.Length);
		}

		/// <summary>
		/// This property may as well be computed every time it is used.
		/// </summary>
		public override bool ComputeEveryTime
		{
			get
			{
				return true;
			}
			set
			{	//review JT(JH): could you add a comment saying why this is not symmetric with the get?
				base.ComputeEveryTime = value;
			}
		}
	}

	/// <summary>
	/// This virtual handler loads the backreferences of a particular atomic forward reference.
	/// By default they come in random order. Todo: devise a strategy to add an order-by or a
	/// post-load sort or both. (I thought about just allowing an 'order by' clause to be appended
	/// to the sql, but typically it requires extra joins to obtain the required keys.)
	/// </summary>
	public class BackRefAtomicVirtualHandler : BaseVirtualHandler
	{
		string m_sql;

		/// <summary>
		/// Construct one by extracting from the "parameters" node the class and field of the forward ref.
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		public BackRefAtomicVirtualHandler(XmlNode configuration, FdoCache cache) : base(configuration)
		{
			string referringClassName =  XmlUtils.GetManditoryAttributeValue(configuration.SelectSingleNode("parameters"), "class");
			string referringPropertyName = XmlUtils.GetManditoryAttributeValue(configuration.SelectSingleNode("parameters"), "field");
			m_sql = "select id from " + referringClassName + " where " + referringPropertyName + " = "; // Id gets appended in the Load method.
			Type = (int)CellarModuleDefns.kcptReferenceCollection;
		}

		/// <summary>
		/// Load the data.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			string sql = m_sql + hvo;
			IVwOleDbDa odd = cda as IVwOleDbDa;
			if (odd == null)
				return; // nothing we can do.
			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctObjVec, 0, tag, 0);
			odd.Load(sql, dcs, hvo, 0, null, false);
		}
	}
	/// <summary>
	/// This virtual handler loads the backreferences of a particular sequence (or collection) forward reference.
	/// By default they come in random order. The 'parameters' element may have an 'order' attribute
	/// to specify the desired order; currently the only one supported is 'DateCreated'.
	/// Enhance: may want something like a property name to order by, and something to indicate how to
	/// collate them, and a ws selector...
	/// </summary>
	public class BackRefSeqVirtualHandler : BaseVirtualHandler
	{
		string m_sql1;
		string m_sql2;
		FdoCache m_cache;

		/* Not used, as of 4/12/06
				/// ------------------------------------------------------------------------------------
				/// <summary>
				/// Constructor that just takes normal parameters
				/// </summary>
				/// <param name="sClassName"></param>
				/// <param name="sPropertyName"></param>
				/// <param name="sOrder"></param>
				/// ------------------------------------------------------------------------------------
				public BackRefSeqVirtualHandler(string sClassName, string sPropertyName, string sOrder)
				{
					Initialize(sClassName, sPropertyName, sOrder);
				}
		*/

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct one by extracting from the "parameters" node the class and field of the forward ref
		/// (and possibly the order, though this is not implemented yet).
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		/// ------------------------------------------------------------------------------------
		public BackRefSeqVirtualHandler(XmlNode configuration, FdoCache cache) : base(configuration)
		{
			m_cache = cache;
			XmlNode node = configuration.SelectSingleNode("parameters");
			Initialize(XmlUtils.GetManditoryAttributeValue(node, "class"),
				XmlUtils.GetManditoryAttributeValue(node, "field"),
				XmlUtils.GetOptionalAttributeValue(node, "order"));
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize this puppy
		/// </summary>
		/// <param name="sClassName"></param>
		/// <param name="sPropertyName"></param>
		/// <param name="sOrder"></param>
		/// ------------------------------------------------------------------------------------
		public void Initialize(string sClassName, string sPropertyName, string sOrder)
		{
			switch(sOrder)
			{
					// Enhance: add options for various possible ways to order it. (Some may require additional
					// parameters, such as a field and/or writing system and/or collation to sort by.)
				default:
					m_sql1 = string.Format("select Src from {0}_{1} where Dst = ",
						sClassName, sPropertyName);
					m_sql2 = "";
					break;
			}
			Type = (int)CellarModuleDefns.kcptReferenceCollection;
		}

		/// <summary>
		/// Load the data.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
#if USEORIGINALCODE
			string sql = m_sql1 + hvo + m_sql2;
			IVwOleDbDa odd = cda as IVwOleDbDa;
			if (odd == null)
				return; // nothing we can do.
			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctObjVec, 0, tag, 0);
			odd.Load(sql, dcs, hvo, 0, null, false);
#else
			string sql = m_sql1 + "? " + m_sql2;
			List<int> hvos = DbOps.ReadIntsFromCommand(m_cache, sql, hvo);
			cda.CacheVecProp(
				hvo,
				tag,
				hvos.ToArray(),
				hvos.Count);
#endif
		}
	}

	// Using FDOSequencePropertyVirtualHandler VH, and the FullConcordanceIds of the wordform now.
	//	/// <summary>
	//	/// This virtual handler loads the CmAnnotations that refer, directly or indirectly,
	//	/// to a particular WfiWordform. That is, the annotations are an instance of the
	//	/// wordform itself, or one of its analyses or their glosses.
	//	/// </summary>
	//	public class WordformAnnotationsVirtualHandler : BaseVirtualHandler
	//	{
	//		/// <summary>
	//		/// Construct one by extracting from the "parameters" node the class and field of the forward ref.
	//		/// </summary>
	//		/// <param name="configuration"></param>
	//		/// <param name="cache"></param>
	//		public WordformAnnotationsVirtualHandler(XmlNode configuration, FdoCache cache) : base(configuration)
	//		{
	//			Type = (int)CellarModuleDefns.kcptReferenceCollection;
	//		}
	//
	//		/// <summary>
	//		/// Load the data.
	//		/// </summary>
	//		/// <param name="hvo"></param>
	//		/// <param name="tag"></param>
	//		/// <param name="ws"></param>
	//		/// <param name="cda"></param>
	//		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
	//		{
	//			IVwOleDbDa odd = cda as IVwOleDbDa;
	//			if (odd == null)
	//				return; // nothing we can do.
	//			// Make the query smart enough that it won't include any CmBaseAnnotations with a
	//			// null BeginObject since this will cause a crash when the user click on one of these
	//			// and the Text pane tries to display it.
	//			string sql =
	//				"select ca.id from CmAnnotation ca" +
	//				" join CmBaseAnnotation cba on cba.id = ca.id and cba.BeginObject is not null" +
	//				" where ca.InstanceOf = " + hvo +
	//				" union select ca.id from CmObject wf" +
	//				" join CmObject wa on wf.id = " + hvo + " and wa.owner$ = wf.id" +
	//				" join CmAnnotation ca on ca.InstanceOf = wa.id" +
	//				" join CmBaseAnnotation cba on cba.id = ca.id and cba.BeginObject is not null" +
	//				" union select ca.id from CmObject wf" +
	//				" join CmObject wa on wf.id = " + hvo + " and wa.owner$ = wf.id" +
	//				" join CmObject wg on wg.owner$ = wa.id" +
	//				" join CmAnnotation ca on ca.InstanceOf = wg.id" +
	//				" join CmBaseAnnotation cba on cba.id = ca.id and cba.BeginObject is not null";
	//			IDbColSpec dcs = DbColSpecClass.Create();
	//			dcs.Push((int)DbColType.koctObjVec, 0, tag, 0);
	//			odd.Load(sql, dcs, hvo, 0, null, false);
	//		}
	//	}

	/// <summary>
	/// Summary description for VirtualHandlers.
	/// </summary>
	/// <remarks>
	/// This will check the actual owner, as well as move up the ownership structure to find a match.
	/// </remarks>
	public class OwnerOfClassVirtualHandler : FDOAtomicPropertyVirtualHandler
	{
		uint m_clid;

		/// <summary>
		/// Construct one.
		/// </summary>
		/// <param name="configuration">'parameters' node we initialize from</param>
		/// <param name="cache"></param>
		public OwnerOfClassVirtualHandler(XmlNode configuration, FdoCache cache) : base(configuration, cache)
		{
		}

		/// <summary>
		/// we may not have a load property, since we do all the loading in Load()
		/// </summary>
		protected override void CheckLoadPropertyType()
		{
			if (m_propertyInfo != null)
				base.CheckLoadPropertyType();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		protected override int SetupDestinationClass(XmlNode configuration, FdoCache cache)
		{
			string ownerClassName = XmlUtils.GetManditoryAttributeValue(configuration.SelectSingleNode("parameters"), "OwnerClass");
			m_clid = m_cache.MetaDataCacheAccessor.GetClassId(ownerClassName);
			m_destinationClassId = (int)m_clid;
			return (int)m_clid;
		}

		int OwnerOf(int hvo, ISilDataAccess sda)
		{
			return sda.get_ObjectProp(hvo, (int)CmObjectFields.kflidCmObject_Owner);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		/// <summary>
		/// The value of this property is the hvo of the owning object that is of class m_clid (or is a subclass of that).
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			ISilDataAccess sda = cda as ISilDataAccess;
			// Loop over owners of hvo. (Review: would it be more useful, or a useful option, to include the hvo itself?)
			for (int hvoOwner = OwnerOf(hvo, sda); hvoOwner != 0; hvoOwner = OwnerOf(hvoOwner, sda))
			{
				uint clidOwner = (uint)sda.get_IntProp(hvoOwner, (int)CmObjectFields.kflidCmObject_Class);
				// Loop over clidOwner and its base classes to see whether any of them matches m_clid
				for (uint clid = clidOwner; clid != 0; )
				{
					if (clid == m_clid)
					{
						// Got the one we want! Cache it and return.
						cda.CacheObjProp(hvo, tag, hvoOwner);
						return;
					}
					// See if base class matches
					clid = m_cache.MetaDataCacheAccessor.GetBaseClsId(clid);
				}
			}
			cda.CacheObjProp(hvo, tag, 0); // Couldn't find such an owner.
		}
	}

	/// <summary>
	/// This handler stores/retrieves a multilingual string value that doesn't exist in the
	/// model.  This is useful for temporary storage of data being entered by the user in some
	/// views prior to its being stored in actual objects.  The owning class must exist, so use
	/// something general like LangProject or LexDb.
	/// </summary>
	public class MultiStringVirtualHandler : BaseVirtualHandler
	{
		ITsStrFactory m_tsf = null;

		/// <summary>
		/// Standard constructor.
		/// </summary>
		/// <param name="configuration">specification that contains information about class, field, etc..</param>
		/// <param name="cache"></param>
		public MultiStringVirtualHandler(XmlNode configuration, FdoCache cache)
			: base(configuration)
		{
			Init();
			SetupDependencies(configuration, cache);
		}


		/// <summary>
		///
		/// </summary>
		/// <param name="className"></param>
		/// <param name="fieldName"></param>
		public MultiStringVirtualHandler(string className, string fieldName) : base()
		{
			this.ClassName = className;
			this.FieldName = fieldName;
			Init();
		}

		private void Init()
		{
			Writeable = true;
			Type = (int)CellarModuleDefns.kcptMultiString;
			m_tsf = TsStrFactoryClass.Create();
		}

		/// <summary>
		/// Cache an empty string if this property was missing from the cache.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			if (ws == 0)
				throw new ArgumentException();

			// Initialize the string properly.
			ITsString tss = m_tsf.MakeString("", ws);
			cda.CacheStringAlt(hvo, tag, ws, tss);
		}

		/// <summary>
		/// This is called by the framework when a writeable virtual property of type kcptString
		/// or kcptMultiString is written. The _unk parameter may be cast to an ITsString and is
		/// the new value.  The ws parameter is meaningful only for multistrings.
		/// The implementation should take whatever steps are needed to store the change.
		/// You can retrieve the old value of the property from the sda.
		/// The framework will automatically update the value in the cache after your method
		/// returns, unless the property is ComputeEveryTime.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="_unk"></param>
		/// <param name="_sda"></param>
		public override void WriteObj(int hvo, int tag, int ws, object _unk,
			ISilDataAccess _sda)
		{
			ITsString tss = _unk as ITsString;
			if (tss == null)
				throw new ArgumentNullException();
		}
	}

	/// <summary>
	/// Base class for LexReferences virtual handlers.
	/// </summary>
	public abstract class BaseLexReferencesVirtualHandler : BaseVirtualHandler
	{
		FdoCache m_cache;
		/// <summary>
		/// stored values from bulk loading due to SetLoadForAllOfClass(true).
		/// </summary>
		Dictionary<int, List<int>> m_bulkValues;
		/// <summary>
		/// Flag whether we've actually tried to bulk load into m_bulkValues.
		/// </summary>
		bool m_fBulkLoaded;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache"></param>
		public BaseLexReferencesVirtualHandler(FdoCache cache)
		{
			m_cache = cache;
		}

		/// <summary>
		/// Construct one.  This constructor is needed for reflected calls, which use both
		/// parameters.
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		public BaseLexReferencesVirtualHandler(XmlNode configuration, FdoCache cache) : base(configuration)
		{
			m_cache = cache;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		/// <summary>
		/// This method may be implemented to load everything at once, even when
		/// m_fComputeEveryTime is true.
		/// </summary>
		/// <param name="fLoadAll"></param>
		public override void SetLoadForAllOfClass(bool fLoadAll)
		{
			if (fLoadAll)
			{
				if (m_bulkValues == null)
				{
					m_bulkValues = new Dictionary<int, List<int>>();
					m_fBulkLoaded = false;
				}
			}
			else
			{
				m_bulkValues = null;
				m_fBulkLoaded = false;
			}
		}

		/// <summary>
		/// Load the back references (to LexReference) for a target into the given IVwCacheDa.
		/// </summary>
		/// <param name="hvo">id of the object for which we want to find backreferences</param>
		/// <param name="tag">virtual flid</param>
		/// <param name="ws">presently not used.</param>
		/// <param name="cda">the cache into which to load the backreferences</param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			if (BaseVirtualHandler.ForceBulkLoadIfPossible && m_bulkValues == null)
			{
				SetLoadForAllOfClass(true);
				BaseVirtualHandler.m_rgvhBulkForced.Add(this);
			}
			List<int> refs = null;
			if (m_bulkValues != null)
			{
				if (!m_fBulkLoaded)
				{
					this.LoadAllLexReferences(m_bulkValues);
					m_fBulkLoaded = true;
				}
				ValidateTargetClass(hvo);
				if (!m_bulkValues.TryGetValue(hvo, out refs))
					refs = new List<int>();
			}
			else
			{
				refs = this.LexReferences(hvo);
			}

			cda.CacheVecProp(hvo, tag, refs.ToArray(), refs.Count);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if this is the correct virtual handler for the targetType;
		/// </summary>
		/// <param name="targetHvo">The target hvo.</param>
		/// ------------------------------------------------------------------------------------
		private void ValidateTargetClass(int targetHvo)
		{
#if DEBUG
			int classId = m_cache.GetClassOfObject(targetHvo);
			string className = m_cache.MetaDataCacheAccessor.GetClassName((uint)classId);
			Debug.Assert(className == this.ClassName, "The target (" + targetHvo + ") is class " + className +
				" which is unsupported by this " + this.ClassName + "." + this.FieldName + " virtual handler.");
#endif
		}

		/// <summary>
		/// Returns the list of backreferences (to LexReference) that refer to the given Target.
		/// </summary>
		/// <param name="targetHvo"></param>
		/// <returns></returns>
		public List<int> LexReferences(int targetHvo)
		{
			ValidateTargetClass(targetHvo);
			string qry = string.Format("SELECT t.Src FROM LexReference_Targets t " +
				"WHERE t.[Dst]={0}", targetHvo);
			return DbOps.ReadIntsFromCommand(m_cache, qry, null);
		}

		/// <summary>
		/// Load all the backreferences (to LexReference) at once, caching values locally.
		/// </summary>
		/// <param name="bulk"></param>
		public void LoadAllLexReferences(Dictionary<int, List<int>> bulk)
		{
			string qry = "SELECT Dst, Src FROM LexReference_Targets ORDER BY Dst";
			DbOps.LoadDictionaryFromCommand(m_cache, qry, null, bulk);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="targetHvo"></param>
		/// <returns></returns>
		public List<int> LexRefTypes(int targetHvo)
		{
			ValidateTargetClass(targetHvo);
			string qry = string.Format("SELECT m.[Src] FROM LexReference_Targets t " +
				"JOIN  LexRefType_Members m on t.[Src]=m.[Dst] " +
				"WHERE t.[Dst]={0}", targetHvo);
			return DbOps.ReadIntsFromCommand(m_cache, qry, null);
		}
	}

	/// <summary>
	/// Back reference for the LexReferences that refer to the LexEntry.
	/// </summary>
	public class LexEntryReferencesVirtualHandler : BaseLexReferencesVirtualHandler
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cache"></param>
		public LexEntryReferencesVirtualHandler(FdoCache cache) : base(cache)
		{
			ClassName = "LexEntry";
			FieldName = "LexEntryReferences";
			InitType();
		}

		/// <summary>
		/// Construct one.  This constructor is needed for reflected calls, which use both
		/// parameters.
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		public LexEntryReferencesVirtualHandler(XmlNode configuration, FdoCache cache) :
			base(configuration, cache)
		{
			InitType();
		}

		private void InitType()
		{
			Type = (int)CellarModuleDefns.kcptReferenceSequence;
		}
	}

	/// <summary>
	/// Back reference for the LexReferences that refer to the LexEntry.
	/// </summary>
	public class LexSenseReferencesVirtualHandler : BaseLexReferencesVirtualHandler
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache"></param>
		public LexSenseReferencesVirtualHandler(FdoCache cache) : base(cache)
		{
			ClassName = "LexSense";
			FieldName = "LexSenseReferences";
			InitType();
		}

		/// <summary>
		/// Construct one.  This constructor is needed for reflected calls, which use both
		/// parameters.
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		public LexSenseReferencesVirtualHandler(XmlNode configuration, FdoCache cache) :
			base(configuration, cache)
		{
			InitType();
		}

		private void InitType()
		{
			Type = (int)CellarModuleDefns.kcptReferenceSequence;
		}
	}

	/// <summary>
	/// Implements a virtual property of CmBaseAnnotation called StringValue. It is a monolingual string
	/// that is the value of the range of the property annotated.
	/// </summary>
	public class AnnotationStringValueVh : BaseVirtualHandler
	{
		/// <summary>
		/// Standard contructor for installed virtual handlers.
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		public AnnotationStringValueVh(XmlNode configuration, FdoCache cache) : base(configuration)
		{
			this.Type = (int)CellarModuleDefns.kcptString;
		}

		/// <summary>
		/// Create one.
		/// </summary>
		public AnnotationStringValueVh()
		{
			this.Type = (int)CellarModuleDefns.kcptString;
			this.ClassName = "CmBaseAnnotation";
			this.FieldName = "StringValue";
		}

		/// <summary>
		/// Load the value into the cache.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			ISilDataAccess sda = cda as ISilDataAccess;
			int ichMin = sda.get_IntProp(hvo,
				(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset);
			int ichLim = sda.get_IntProp(hvo,
				(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset);
			int hvoObj = sda.get_ObjectProp(hvo,
				(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
			int flid = sda.get_IntProp(hvo,
				(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidFlid);
			ITsString tss = sda.get_StringProp(hvoObj, flid);
			Debug.Assert(ichMin < tss.Length);
			Debug.Assert(ichLim <= tss.Length);
			cda.CacheStringProp(hvo, tag, tss.GetSubstring(ichMin, ichLim));
		}
	}

	/// <summary>
	/// This virtual handler expects the object to be a WfiWordform. It returns a subset of
	/// the WfiAnalyses owned by that wordform which are 'attested', that is, it or one of its
	/// WfiGlosses is the InstanceOf of some CmBaseAnnotation. Furthermore, if two or more
	/// WfiAnalyses have the same POS, only one of them is returned.
	/// </summary>
	public class AttestedAnalysesOfWordformUniqPosHandler : BaseVirtualHandler
	{
		FdoCache m_cache;
		/// <summary>
		/// This is constructed in the standard way for a virtual handler invoked from XML.
		/// The configuration parameter is not currently used.
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		public AttestedAnalysesOfWordformUniqPosHandler(XmlNode configuration, FdoCache cache) : base(configuration)
		{
			m_cache = cache;
			Type = (int)CellarModuleDefns.kcptReferenceSequence;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		/// <summary>
		/// Load the data.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			int hvoTwficType = CmAnnotationDefn.Twfic(m_cache).Hvo;
			string sql = string.Format("select min(wa.id), pos.id from "
				+"(select wa.id, wa.Category from WfiWordform wf "
				+"join WfiAnalysis_ wa on wa.owner$ = wf.id "
				+"join WfiGloss_ wg on wg.owner$ = wa.id "
				+"join CmBaseAnnotation_ cba on cba.InstanceOf = wg.id and cba.AnnotationType = {0} "
				+"where wf.id = {1} "
				+"union "
				+"select wa.id, wa.Category from WfiWordform wf "
				+"join WfiAnalysis_ wa on wa.owner$ = wf.id "
				+"join CmBaseAnnotation_ cba on cba.InstanceOf = wa.id and cba.AnnotationType = {0} "
				+"where wf.id = {1}) wa "
				+"join PartOfSpeech pos on wa.Category = pos.id "
				+"group by pos.id", hvoTwficType, hvo);
			int[] vals = DbOps.ReadIntArrayFromCommand(m_cache, sql, null);
			cda.CacheVecProp(hvo, tag, vals, vals.Length);
		}
	}
	/// <summary>
	/// This virtual handler expects the object to be a WfiWordform. It returns a set of POS's
	/// which are 'attested' for that wordform, that is, one of the analyses of the wordform
	/// (which is used in a text) has that POS. 'Used in a text' means that there exists
	/// a CmBaseAnnotation which has the analysis or one of its Glosses as its InstanceOf.
	/// </summary>
	public class AttestedPosHandler : BaseVirtualHandler
	{
		FdoCache m_cache;
		/// <summary>
		/// This is constructed in the standard way for a virtual handler invoked from XML.
		/// The configuration parameter is not currently used.
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		public AttestedPosHandler(XmlNode configuration, FdoCache cache)
			: base(configuration)
		{
			m_cache = cache;
			Type = (int)CellarModuleDefns.kcptReferenceSequence;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		/// <summary>
		/// Load the data.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			if (m_cache.IsDummyObject(hvo)) // lots of dummy wordforms, don't want to go here.
			{
				cda.CacheVecProp(hvo, tag, null, 0);
				return;
			}
			int hvoTwficType = CmAnnotationDefn.Twfic(m_cache).Hvo;
			string sql = string.Format("select distinct wa.Category from WfiAnalysis_ wa "
				+ "where wa.Category is not null and wa.Owner$ = {1} and "
				+	"exists (select id from CmBaseAnnotation_ cba where cba.InstanceOf = wa.id and cba.AnnotationType = {0} "
				+ "union "
				+	"select cba.id from CmBaseAnnotation_ cba "
				+		"join WfiGloss_ wg on cba.InstanceOf = wg.id and wg.owner$ = wa.id and cba.AnnotationType = {0} ) ",
				hvoTwficType, hvo);
			int[] vals = DbOps.ReadIntArrayFromCommand(m_cache, sql, null);
			cda.CacheVecProp(hvo, tag, vals, vals.Length);
		}

		/// <summary>
		/// Called by reflection to preload the property for all objects.
		/// </summary>
		/// <param name="cache"></param>
		public static void PreloadAll(FdoCache cache)
		{
			int hvoTwficType = CmAnnotationDefn.Twfic(cache).Hvo;
			IVwVirtualHandler vh = BaseVirtualHandler.GetInstalledHandler(cache, "WfiWordform", "AttestedPos");
			if (vh == null)
				return; // not installed, can't optimize.
			int tag = vh.Tag;
			string sql = string.Format("select distinct wf.id, wa.Category from WfiWordform wf "
				+ "left outer join WfiAnalysis_ wa on wf.id = wa.owner$ and wa.Category is not null "
				+ "and exists (select id from CmBaseAnnotation_ cba where cba.InstanceOf = wa.id and cba.AnnotationType = {0} "
				+	"union "
				+	"select cba.id from CmBaseAnnotation_ cba "
				+		"join WfiGloss_ wg on cba.InstanceOf = wg.id and wg.owner$ = wa.id and cba.AnnotationType = {0} ) "
				+ "order by wf.id",
				hvoTwficType);
			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
			dcs.Push((int)DbColType.koctObjVec, 1, tag, 0);
			cache.VwOleDbDaAccessor.Load(sql, dcs, 0, 0, null, false);
		}
	}

	/// <summary>
	/// This class generates a string virtual property that is the outline number of the
	/// target object. The XML specifying it should give a class/field combination indicating
	/// the flid to use as the basis of the outline, whether to include a final period,
	/// and whether to include the position in a root property.
	/// </summary>
	public class OutlineNumberHandler : BaseVirtualHandler
	{
		int m_flid;
		bool m_fIncTopOwner = false;
		bool m_fFinalPeriod = false;
		FdoCache m_cache;

		/* Not used as of 4/12/06
				/// <summary>
				/// make one, specifying the variables directly.
				/// </summary>
				/// <param name="flid"></param>
				/// <param name="cache"></param>
				/// <param name="fFinalPeriod"></param>
				/// <param name="fIncTopOwner"></param>
				public OutlineNumberHandler(int flid, FdoCache cache, bool fFinalPeriod, bool fIncTopOwner)
				{
					Init(flid, cache, fFinalPeriod, fIncTopOwner);
				}
		*/

		private void Init(int flid, FdoCache cache, bool fFinalPeriod, bool fIncTopOwner)
		{
			m_flid = flid;
			m_cache = cache;
			m_fIncTopOwner = fIncTopOwner;
			m_fFinalPeriod = fFinalPeriod;
			Type = (int)CellarModuleDefns.kcptString;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		/// <summary>
		/// Initialize it from an XmlNode with attributes ownerclass, ownerfield,
		/// and optionally finalperiod and/or includetopowner.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="cache"></param>
		public OutlineNumberHandler(XmlNode node, FdoCache cache) : base(node)
		{
			string ownerClassName = XmlUtils.GetManditoryAttributeValue(node, "ownerclass");
			string ownerFieldName = XmlUtils.GetManditoryAttributeValue(node, "ownerfield");
			bool fIncTopOwner = XmlUtils.GetOptionalBooleanAttributeValue(node, "includetopowner", false);
			bool fFinalPeriod = XmlUtils.GetOptionalBooleanAttributeValue(node, "finalperiod", false);
			int flid = (int)cache.MetaDataCacheAccessor.GetFieldId(ownerClassName, ownerFieldName, true);
			if (flid == 0)
				throw new Exception("Field " + ownerFieldName + " of class " + ownerClassName + " not known");
			Init(flid, cache, fFinalPeriod, fIncTopOwner);
		}


		/// <summary>
		/// The value of this property is the outline number.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			string outline = m_cache.GetOutlineNumber(hvo, m_flid, m_fFinalPeriod, m_fIncTopOwner);
			cda.CacheStringProp(hvo, tag, m_cache.MakeUserTss(outline));
		}

		/// <summary>
		/// This property may as well be computed every time it is used. It can change easily as
		/// things are moved around, and is not very expensive once the relevant data is cached.
		/// </summary>
		public override bool ComputeEveryTime
		{
			get
			{
				return true;
			}
			set
			{
			}
		}
	}

	/// <summary>
	/// This class generates a string virtual property that is the string representation
	/// of all of the reversal entries for a sense.
	/// </summary>
	public class LexSenseReversalEntriesTextHandler : BaseVirtualHandler
	{
		private FdoCache m_cache;

		/// <summary>
		/// The name this attr usually has.
		/// </summary>
		public const string StandardFieldName = "ReversalEntriesBulkText";

		/// <summary>
		/// Initialize it from an XmlNode with attributes ownerclass, ownerfield,
		/// and optionally finalperiod and/or includetopowner.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="cache"></param>
		public LexSenseReversalEntriesTextHandler(XmlNode node, FdoCache cache)
		{
			m_cache = cache;
			SetAndCheckNames(node, "LexSense", StandardFieldName);
			SetupDependencies(node, cache);
			Type = (int)CellarModuleDefns.kcptMultiString;
			Writeable = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		/// <summary>
		/// The value of this property is the concatenation of the forms of its reversal entries that
		/// have the correct ws.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			cda.CacheStringAlt(hvo, tag, ws, GetValue(hvo, ws));
		}

		/// <summary>
		/// Compute the value for a given hvo and ws, but don't cache it.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		public ITsString GetValue(int hvo, int ws)
		{
			ITsStrBldr tsb = TsStrBldrClass.Create();
			LexSense sense = new LexSense(m_cache, hvo);
			ITsTextProps ttpWs;
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, ws);
			ttpWs = propsBldr.GetTextProps();
			tsb.Replace(0, 0, "", ttpWs); // In case it ends up empty, make sure it's empty in the right Ws.
			foreach (ReversalIndexEntry revEntry in sense.ReversalEntriesRC)
			{
				if (revEntry.WritingSystem == ws)
				{
					if (tsb.Length > 0)
						tsb.Replace(tsb.Length, tsb.Length, "; ", ttpWs);
					tsb.Replace(tsb.Length, tsb.Length, revEntry.LongName, ttpWs);
				}
			}
			ITsString tss = tsb.GetString();
			return tss;
		}

		/// <summary>
		/// Write a new value of the string.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="_unk"></param>
		/// <param name="_sda"></param>
		public override void WriteObj(int hvo, int tag, int ws, object _unk, ISilDataAccess _sda)
		{
			LexSense sense = new LexSense(m_cache, hvo);
			sense.CommitReversalEntriesText((ITsString)_unk, ws);
		}

	}

	/// <summary>
	/// This virtual handler finds all the pictures of all the senses of an entry.
	/// </summary>
	public class EntryPicturesVirtualHandler : BaseVirtualHandler
	{
		/// <summary>
		/// Construct one.
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		public EntryPicturesVirtualHandler(XmlNode configuration, FdoCache cache) : base(configuration)
		{
			Type = (int)CellarModuleDefns.kcptReferenceCollection;
		}

		/// <summary>
		/// Load the data.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			ISilDataAccess sda = cda as ISilDataAccess;
			List<int> pictures = new List<int>();
			GetPictures(sda, hvo, (int)LexEntry.LexEntryTags.kflidSenses, pictures);
			int[] result = DbOps.ListToIntArray(pictures);
			cda.CacheVecProp(hvo, tag, result, result.Length);
		}

		/// <summary>
		/// Process the senses in property flidSub of object hvo, adding HVOs of any pictures
		/// to the list.
		/// </summary>
		/// <param name="sda"></param>
		/// <param name="hvo"></param>
		/// <param name="flidSub"></param>
		/// <param name="pictures"></param>
		private void GetPictures(ISilDataAccess sda, int hvo, int flidSub, List<int> pictures)
		{
			int csense = sda.get_VecSize(hvo, flidSub);
			for (int isense = 0; isense < csense; isense ++)
			{
				int hvoSense = sda.get_VecItem(hvo, flidSub, isense);
				int cpic = sda.get_VecSize(hvoSense, (int)LexSense.LexSenseTags.kflidPictures);
				for (int ipic = 0; ipic < cpic; ipic++)
				{
					pictures.Add(sda.get_VecItem(hvoSense, (int)LexSense.LexSenseTags.kflidPictures, ipic));
				}
				GetPictures(sda, hvoSense, (int)LexSense.LexSenseTags.kflidSenses, pictures);
			}

		}

		/// <summary>
		/// Update rghvo and rgtag to add each sense hvo with the Pictures flid.
		/// </summary>
		public override void UpdateNotifierLists(ISilDataAccess sda,
			ref int[] rghvo, ref int[] rgtag, ref int chvo)
		{
			Debug.Assert(sda != null);
			Debug.Assert(rghvo != null && rghvo.Length == 1);
			Debug.Assert(rgtag != null && rgtag.Length == 1);
			Debug.Assert(chvo == 1);

			int hvoEntry = rghvo[0];
			int csense = sda.get_VecSize(hvoEntry, (int)LexEntry.LexEntryTags.kflidSenses);
			List<int> hvos = new List<int>();
			List<int> tags = new List<int>();
			// keep the existing entry hvo and (virtual) tag.
			hvos.Add(hvoEntry);
			tags.Add(rgtag[0]);
			// also set dependency on the senses themselves.
			hvos.Add(hvoEntry);
			tags.Add((int)LexEntry.LexEntryTags.kflidSenses);
			for (int isense = 0; isense < csense; isense++)
			{
				int hvoSense = sda.get_VecItem(hvoEntry, (int)LexEntry.LexEntryTags.kflidSenses, isense);
				hvos.Add(hvoSense);
				tags.Add((int)LexSense.LexSenseTags.kflidPictures);
			}
			rghvo = hvos.ToArray();
			rgtag = tags.ToArray();
			chvo = hvos.Count;
		}
	}

	/// <summary>
	/// This is used in the XmlBrowseViewBaseVc code to handle the highlighting of the selected
	/// row.  It's perhaps the simplest useful (?) virtual handler.
	/// </summary>
	public class MeVirtualHandler : BaseVirtualHandler
	{
		/// <summary>
		/// Construct one.
		/// </summary>
		public MeVirtualHandler()
		{
			Type = (int)CellarModuleDefns.kcptReferenceAtom;
			ClassName = "CmObject";
			FieldName = "Me";
		}

		/// <summary>
		/// Return the MeVirtualHandler for the supplied cache, creating it if needed.
		/// </summary>
		/// <param name="cda"></param>
		/// <returns></returns>
		public static MeVirtualHandler InstallMe(IVwCacheDa cda)
		{
			MeVirtualHandler vh = (MeVirtualHandler)cda.GetVirtualHandlerName("CmObject", "Me");
			if (vh == null)
			{
				vh = new MeVirtualHandler();
				cda.InstallVirtual(vh);
			}
			return vh;
		}

		/// <summary>
		/// The value of this property is the hvo of itself.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			cda.CacheObjProp(hvo, tag, hvo); // Me == this == myself.
		}
	}

	/// <summary>
	/// These are the arguments needed for requesting to convert a dummy object to a real one.
	/// </summary>
	public class RequestConversionToRealEventArgs : EventArgs
	{
		int m_hvoDummy = 0;
		int m_dataFlid = 0;
		int m_owningFlid = 0;
		bool m_fConvertNow = false;
		XmlNode m_configuration = null;
		ICmObject m_RealObj = null;

		/// <summary>
		///
		/// </summary>
		/// <param name="hvoDummy"></param>
		/// <param name="dataFlid">The field of the hvoDummy that needs the data from the database.
		/// Note: this is different from the flid owning hvoDummy.</param>
		/// <param name="configuration"></param>
		/// <param name="fConvertNow">set to true, if the client will fail without a real object.</param>
		public RequestConversionToRealEventArgs(int hvoDummy, int dataFlid, XmlNode configuration, bool fConvertNow)
		{
			m_hvoDummy = hvoDummy;
			m_dataFlid = dataFlid;
			m_fConvertNow = fConvertNow;
			m_configuration = configuration;
		}

		/// <summary>
		/// The dummy id we request to be converted to a real object.
		/// </summary>
		public int DummyHvo
		{
			get { return m_hvoDummy; }
		}

		/// <summary>
		/// Field owning DummyHvo.
		/// </summary>
		public int OwningFlid
		{
			get { return m_owningFlid; }
			set { m_owningFlid = value; }
		}

		/// <summary>
		/// The field that needs the data from the database.
		/// </summary>
		public int DataFlid
		{
			get { return m_dataFlid; }
		}

		/// <summary>
		/// true, if the client wants the real object now.
		/// false, if it's okay to queue this dummy to be converted for later.
		/// </summary>
		public bool ConvertNow
		{
			get { return m_fConvertNow; }
		}

		/// <summary>
		/// The configuration node for the item requesting the conversion.
		/// </summary>
		public XmlNode Configuration
		{
			get { return m_configuration; }
		}

		/// <summary>
		/// If the dummy object was replaced with a real object.
		/// </summary>
		public ICmObject RealObject
		{
			get { return m_RealObj; }
			set { m_RealObj = value; }
		}
	}

	/// <summary>
	/// This is the event needed for requesting to convert a dummy object into a real one.
	/// </summary>
	public delegate void RequestConversionToRealEventHandler(object sender, RequestConversionToRealEventArgs e);

	/// <summary>
	/// Methods and events for requesting to convert a dummy object to a real one.
	/// </summary>
	public interface IDummyRequestConversion
	{
		/// <summary>
		/// Event requesting an owner to convert the object to a real one.
		/// </summary>
		event RequestConversionToRealEventHandler RequestConversionToReal;
		/// <summary>
		/// Raises the RequestConversionToReal event with the given arguments.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnRequestConversionToReal(object sender, RequestConversionToRealEventArgs e);
	}

	/// <summary>
	/// Abstract class to handle FDO classes with non-standard properties (not built in to the model).
	/// </summary>
	public abstract class BaseFDOPropertyVirtualHandler : BaseVirtualHandler, IDummyRequestConversion
	{

		/// <summary>
		/// Used by DummyRecordList to convert one of its dummy object members to a real one.
		/// </summary>
		public event RequestConversionToRealEventHandler RequestConversionToReal;

		/// <summary>
		/// FDO Cache
		/// </summary>
		protected FdoCache m_cache;
		/// <summary>
		/// Prop info for a property of the FDO class that accesses one value of this virtual.
		/// </summary>
		protected PropertyInfo m_propertyInfo;
		/// <summary>
		/// if no propertyInfo has been specified, load the virtual handler based upon the first dependency path
		/// that matches our target DestinationClassId
		/// </summary>
		protected List<ClassAndPropInfo> m_cpiRealTargetPath = null;
		/// <summary>
		/// Things that do loading could use a progress bar.
		/// </summary>
		protected ProgressState m_progress;
		/// <summary>
		/// Accessor for an optional method that loads all of the virtual property values at once.
		/// </summary>
		protected MethodInfo m_bulkMethodInfo;

		/// <summary>
		/// Specifies a destination class for this virtual property.
		/// </summary>
		protected int m_destinationClassId = 0;
		/// <summary>
		/// Construct one.
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		public BaseFDOPropertyVirtualHandler(XmlNode configuration, FdoCache cache) : base(configuration)
		{
			m_cache = cache;
			Type fdoObjType = GetTypeFromXml(configuration);
			SetupDestinationClass(configuration, cache);
			SetupDependencies(configuration, cache);
			SetupLoadProperty(configuration, fdoObjType);
			string sBulkMethod = XmlUtils.GetOptionalAttributeValue(configuration, "bulkLoadMethod");
			if (!String.IsNullOrEmpty(sBulkMethod))
				m_bulkMethodInfo = fdoObjType.GetMethod(sBulkMethod);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="fdoObjType"></param>
		protected virtual void SetupLoadProperty(XmlNode configuration, Type fdoObjType)
		{
			m_propertyInfo = fdoObjType.GetProperty(XmlUtils.GetManditoryAttributeValue(configuration, "virtualfield"));
			if (m_propertyInfo == null)
			{
				// try to get path to real property from DependencyPaths
				m_cpiRealTargetPath = GetRealPath(true);
			}
		}

		/// <summary>
		/// check that the load property is of the proper type.
		/// </summary>
		protected virtual void CheckLoadPropertyType()
		{
			if (m_propertyInfo == null && m_cpiRealTargetPath == null)
			{
				throw new ArgumentException(String.Format("FDOPropertyVirtual field {0} should have a load property on class ({1}) or else include depends that match DestinationClass.",
					this.FieldName, this.ClassName));
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		protected virtual int SetupDestinationClass(XmlNode configuration, FdoCache cache)
		{
			string destinationClassName = XmlUtils.GetOptionalAttributeValue(configuration, "destinationClass");
			if (!String.IsNullOrEmpty(destinationClassName))
			{
				// figure out the destination class
				m_destinationClassId = (int)cache.MetaDataCacheAccessor.GetClassId(destinationClassName);
			}
			return m_destinationClassId;
		}

		/// <summary>
		/// Show the progress for loading virtual properties.
		/// </summary>
		public ProgressState Progress
		{
			get
			{
				if (m_progress == null)
					m_progress = new NullProgressState();
				return m_progress;
			}
			set { m_progress = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		/// <summary>
		/// The destination class for this virtual property.
		/// </summary>
		public int DestinationClassId
		{
			get { return m_destinationClassId; }
			set { m_destinationClassId = value; }
		}

		/// <summary>
		/// (uses Tag and Cache internally)
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="ws"></param>
		public void Load(int hvo, int ws)
		{
			base.Load(hvo, ws, m_cache.VwCacheDaAccessor);
		}

		/// <summary>
		/// is there a way we can get views code
		/// </summary>
		/// <param name="hvoObj"></param>
		/// <param name="hvoChange"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		public override bool DoesResultDependOnProp(int hvoObj, int hvoChange, int tag, int ws)
		{
			List<int> flids;
			if (m_cache.TryGetDependencies(Tag, out flids))
			{
				return flids.Contains(tag);
			}
			return base.DoesResultDependOnProp(hvoObj, hvoChange, tag, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears the specified HVO.
		/// </summary>
		/// <param name="hvo">The HVO.</param>
		/// <param name="ws">The writing system</param>
		/// ------------------------------------------------------------------------------------
		protected internal void Clear(int hvo, int ws)
		{
			base.Clear(Cache.VwCacheDaAccessor, hvo, ws);
		}


		/// <summary>
		/// Returns the real path to the destination target in the DependencyPaths
		/// </summary>
		/// <param name="fAllowRealReferenceTarget">if <c>false</c>, only return an owning property path to the
		/// destination class. if <c>true</c>, we'll return the first real path,
		/// whether or not its an owning property or a reference property</param>
		/// <returns></returns>
		internal List<ClassAndPropInfo> GetRealPath(bool fAllowRealReferenceTarget)
		{

			// it should match a DependencyPath
			List<ClassAndPropInfo> realPath = new List<ClassAndPropInfo>();
			// first find the first dependency path who owns (or targets) our DestinationClassId.
			foreach (List<int> path in DependencyPaths)
			{
				// the last flid should match our destination class.
				int lastFlid = path[path.Count - 1];
				ClassAndPropInfo lastFlidInfo = Cache.GetClassAndPropInfo((uint)lastFlid);
				if (lastFlidInfo.signatureClsid == DestinationClassId &&
					(fAllowRealReferenceTarget || !lastFlidInfo.isReference))
				{
					// build the path information for this lastFlid.
					foreach (int flidInPath in path)
					{
						if (flidInPath != lastFlid)
						{
							realPath.Add(Cache.GetClassAndPropInfo((uint)flidInPath));
						}
					}
					realPath.Add(lastFlidInfo);
					break;	// found the real path, so we're finished.
				}
			}
			return realPath;
		}

		/// <summary>
		/// Returns the owning path to the destination class target.
		/// </summary>
		/// <returns></returns>
		public List<ClassAndPropInfo> GetRealOwningPath()
		{
			// just get the real owning path.
			return GetRealPath(false);
		}

		/// <summary>
		/// Raises the RequestConversionToReal event with the given arguments.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public void OnRequestConversionToReal(object sender, RequestConversionToRealEventArgs args)
		{
			try
			{
				int owningFlid = args.OwningFlid;
				if (RequestConversionToReal != null)
				{
					RequestConversionToReal(sender, args);
				}
				else
				{
					if (sender == this || args.ConvertNow == true)
					{
						// try the virtual handler of the flid that owns this dummy object.
						if (args.OwningFlid == 0)
							owningFlid = m_cache.GetOwningFlidOfObject(args.DummyHvo);
						IVwVirtualHandler vh = m_cache.VwCacheDaAccessor.GetVirtualHandlerId(owningFlid);
						if (vh != null && sender != this && vh is IDummyRequestConversion)
						{
							(vh as IDummyRequestConversion).OnRequestConversionToReal(this, args);
						}
					}
				}
				if (args.ConvertNow == true && (args.RealObject == null || args.RealObject.IsDummyObject))
				{
					// as a last resort, try to call the owner directly.
					int hvoOwner = m_cache.GetOwnerOfObject(args.DummyHvo);
					Debug.Assert(hvoOwner != 0);
					ICmObject owner = CmObject.CreateFromDBObject(m_cache, hvoOwner);
					if (owner is IDummy)
					{
						args.RealObject = (owner as IDummy).ConvertDummyToReal(owningFlid, args.DummyHvo);
					}
				}
			}
			finally
			{
				if (args.ConvertNow == true && (args.RealObject == null || args.RealObject.IsDummyObject))
					throw new ApplicationException("We couldn't find someone to handle converting this dummy (" + args.DummyHvo + ") to a real.");
			}
		}
	}

	/// <summary>
	/// A generic virtual handler for handling atomic FDO properties.
	/// This class only handles cases where the given FDO property returns one int,
	/// which is treated as a reference property.
	/// </summary>
	public class FDOAtomicPropertyVirtualHandler : BaseFDOPropertyVirtualHandler
	{
		/// <summary>
		/// Construct one.
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		public FDOAtomicPropertyVirtualHandler(XmlNode configuration, FdoCache cache) : base(configuration, cache)
		{
			CheckLoadPropertyType();

			Type = (int)CellarModuleDefns.kcptReferenceAtom;
		}

		/// <summary>
		/// by default we expect a load property, so make sure it's compatible with returning an atom object (ie. int).
		/// </summary>
		protected override void CheckLoadPropertyType()
		{
			if (m_propertyInfo.PropertyType != typeof(int))
				throw new ArgumentException("Invalid FDO property for 'FDOAtomicPropertyVirtualHandler'", "configuration");
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			// Don't try to load anything real from a dummy object.
			if (m_cache.IsDummyObject(hvo))
			{
				cda.CacheVecProp(hvo, tag, new int[0], 0);
				return;
			}
			ICmObject w = CmObject.CreateFromDBObject(m_cache, hvo);
			cda.CacheObjProp(
				hvo,
				tag,
				(int)m_propertyInfo.GetValue(w, null));
		}
	}

	/// <summary>
	/// A generic virtual handler for handling sequence (or collection) FDO properties.
	/// This class only handles cases where the given FDO property returns an array of ints.
	/// These are handled here as reference values, rather than owning.
	/// </summary>
	public class FDOSequencePropertyVirtualHandler : BaseFDOPropertyVirtualHandler
	{
		/// <summary>
		/// stored values from bulk loading due to SetLoadForAllOfClass(true).
		/// </summary>
		Dictionary<int, List<int>> m_bulkValues;
		/// <summary>
		/// Flag whether we've actually tried to bulk load into m_bulkValues.
		/// </summary>
		bool m_fBulkLoaded;

		/// <summary>
		/// Construct one.
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		public FDOSequencePropertyVirtualHandler(XmlNode configuration, FdoCache cache) : base(configuration, cache)
		{
			CheckLoadPropertyType();
			Type = (int)CellarModuleDefns.kcptReferenceSequence;
		}

		/// <summary>
		///
		/// </summary>
		protected override void CheckLoadPropertyType()
		{
			if (m_propertyInfo == null)
				base.CheckLoadPropertyType();
			if (m_propertyInfo != null && m_propertyInfo.PropertyType != typeof(List<int>))
				throw new ArgumentException("Invalid FDO property for 'FDOSequencePropertyVirtualHandler'", "configuration");
		}

		/// <summary>
		/// This method may be implemented to load everything at once, even when
		/// m_fComputeEveryTime is true.
		/// </summary>
		/// <param name="fLoadAll"></param>
		public override void SetLoadForAllOfClass(bool fLoadAll)
		{
			if (fLoadAll)
			{
				if (m_bulkValues == null && m_bulkMethodInfo != null)
				{
					m_bulkValues = new Dictionary<int, List<int>>();
					m_fBulkLoaded = false;
				}
			}
			else
			{
				m_bulkValues = null;
				m_fBulkLoaded = false;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			List<int> hvos;
			this.Load(hvo, tag, ws, cda, out hvos);
		}

		/// <summary>
		/// subclasses that override Load(hvo, tag, ws, cda) really need to override
		/// this one, if they want to take advantage of returning the list.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="ws"></param>
		/// <param name="tag"></param>
		/// <param name="cda"></param>
		/// <param name="hvos"></param>
		protected virtual void Load(int hvo, int tag, int ws, IVwCacheDa cda, out List<int> hvos)
		{
			if (BaseVirtualHandler.ForceBulkLoadIfPossible && m_bulkValues == null)
			{
				SetLoadForAllOfClass(true);
				BaseVirtualHandler.m_rgvhBulkForced.Add(this);
			}
			// Don't try to load anything real from a dummy object.
			// However, if ComputeEveryTime is set for dummy object
			// we'll assume that it's possible to load its data
			// from it's CmObject property.
			if (!ComputeEveryTime &&  m_cache.IsDummyObject(hvo))
			{
				cda.CacheVecProp(hvo, tag, new int[0], 0);
				hvos = new List<int>();
				return;
			}
			if (m_bulkMethodInfo != null && m_bulkValues != null)
			{
				if (!m_fBulkLoaded)
				{
					m_bulkMethodInfo.Invoke(null, new object[] { m_cache, m_bulkValues });
					m_fBulkLoaded = true;
				}
				if (!m_bulkValues.TryGetValue(hvo, out hvos))
					hvos = new List<int>();
			}
			else
			{
				hvos = GetVectorItemsToCache(hvo);
			}
			cda.CacheVecProp(
				hvo,
				tag,
				hvos.ToArray(),
				hvos.Count);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		protected virtual List<int> GetVectorItemsToCache(int hvo)
		{
			List<int> hvoList = new List<int>();
			// set BulkLoadEnabled, if not already loaded.
			Cache.EnableBulkLoadingIfPossible(true);
			try
			{
				if (m_propertyInfo != null)
				{
					ICmObject fdoObj = CmObject.CreateFromDBObject(m_cache, hvo);
					// The List better only contain ints.
					hvoList = (List<int>) m_propertyInfo.GetValue(fdoObj, null);
				}
				else
				{
					// otherwise load objects based on the owning dependency fields matching DestinationClass
					if (m_cpiRealTargetPath != null && m_cpiRealTargetPath.Count > 0)
					{
						// If that path is empty, just return the hvo of the owner.
						// If the path is not empty, add the hvos of the DestinationClass.
						Queue<ClassAndPropInfo> pathToDestination = new Queue<ClassAndPropInfo>(m_cpiRealTargetPath);
						int hvoSrc = hvo;
						CollectListFromPath(hvoSrc, pathToDestination, ref hvoList);

					}
				}
			}
			finally
			{
				Cache.EnableBulkLoadingIfPossible(false);
			}
			return hvoList;
		}

		/// <summary>
		/// Collect a list based upon the given source and path to target items to load.
		/// </summary>
		/// <param name="hvoSrc"></param>
		/// <param name="pathToDestination"></param>
		/// <param name="hvoList"></param>
		private void CollectListFromPath(int hvoSrc, Queue<ClassAndPropInfo> pathToDestination, ref List<int> hvoList)
		{
			if (pathToDestination == null || pathToDestination.Count == 0)
				return; // we've reached the end somehow
			ClassAndPropInfo cpi = pathToDestination.Peek();
			int level = pathToDestination.Count;
			// move to next path level
			Queue<ClassAndPropInfo> nextChildPath = new Queue<ClassAndPropInfo>(pathToDestination.ToArray());
			nextChildPath.Dequeue();
			if (cpi.isVector)
			{
				int[] results = Cache.GetVectorProperty(hvoSrc, (int)cpi.flid, false);
				if (level == 1)
				{
					// cpi must be the destination vector property
					AddResultsToList(hvoSrc, ref hvoList, results);
					return;
				}
				// recurse for each obj in the vector
				foreach (int hvoDst in results)
				{
					CollectListFromPath(hvoDst, nextChildPath, ref hvoList);
				}
				return;
			}
			else
			{
				// just recurse for the atomic obj
				int hvoDst = Cache.GetObjProperty(hvoSrc, (int)cpi.flid);
				CollectListFromPath(hvoDst, nextChildPath, ref hvoList);
			}
		}

		/// <summary>
		/// just add the results to the hvoList, if there were any.
		/// </summary>
		/// <param name="hvoSrc"></param>
		/// <param name="hvoList"></param>
		/// <param name="results"></param>
		protected virtual void AddResultsToList(int hvoSrc, ref List<int> hvoList, int[] results)
		{
			if (results != null && results.Length != 0)
				hvoList.AddRange(results);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// return the list of hvos we loaded.
		/// WARNING: subclasses may need to override Load(int hvo, int tag, int ws, IVwCacheDa cda,
		/// out List&lt;int&gt; hvos) for this TO DO ANYTHING (properly).
		/// </summary>
		/// <param name="hvo">The HVO.</param>
		/// <param name="ws">The writing system</param>
		/// <param name="hvos">The hvos.</param>
		/// ------------------------------------------------------------------------------------
		public void Load(int hvo, int ws, out List<int> hvos)
		{
			this.Load(hvo, Tag, ws, Cache.VwCacheDaAccessor, out hvos);
		}
	}

	/// <summary>
	/// Ghost properties maintains a sequence of items specified by the Destination field
	/// along with objects that own those items but have not yet created them (e.g. Entries
	/// that own empty Pronunciations).
	/// </summary>
	public class FDOGhostSequencePropertyVirtualHandler : FDOSequencePropertyVirtualHandler
	{
		/// <summary>
		/// Accessor for a method to return the class to create for ghost items.
		/// Required for destination classes that are abstract.
		/// </summary>
		protected MethodInfo m_methodInfoClassToCreate;

		/// <summary>
		///
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		public FDOGhostSequencePropertyVirtualHandler(XmlNode configuration, FdoCache cache)
			: base(configuration, cache)
		{
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="fdoObjType"></param>
		protected override void SetupLoadProperty(XmlNode configuration, Type fdoObjType)
		{
			base.SetupLoadProperty(configuration, fdoObjType);
			// if the owning class does not have a load property,
			// then load list based on dependency fields matching DestinationClass
			CheckLoadPropertyType();
			if (m_propertyInfo == null)
			{
				// for owningPaths to the destination class, we may need to setup a way to create the destination class.
				ClassAndPropInfo destinationProperty = m_cpiRealTargetPath[m_cpiRealTargetPath.Count - 1];
				if (!destinationProperty.isReference)
				{
					// abstract DestinationClasses require a ghostCreateClassMethod, otherwise it's optional.
					string methodName;
					if (Cache.GetAbstract(this.DestinationClassId))
						methodName = XmlUtils.GetManditoryAttributeValue(configuration, "ghostCreateClassMethod");
					else
						methodName = XmlUtils.GetOptionalAttributeValue(configuration, "ghostCreateClassMethod");
					if (!String.IsNullOrEmpty(methodName))
					{
						Type fdoGhostOwner = CmObject.GetTypeFromFWClassID(m_cache, (int)m_cpiRealTargetPath[m_cpiRealTargetPath.Count - 1].sourceClsid);
						m_methodInfoClassToCreate = fdoGhostOwner.GetMethod(methodName);
					}
				}
			}
		}

		/// <summary>
		/// If we're trying to add results to a ghost sequence property and we didn't get any results.
		/// We add the owner (hvoSrc) to the list so this row will get ghosted.
		/// </summary>
		/// <param name="hvoSrc"></param>
		/// <param name="hvoList"></param>
		/// <param name="results"></param>
		protected override void AddResultsToList(int hvoSrc, ref List<int> hvoList, int[] results)
		{
			if (results == null || results.Length == 0)
				hvoList.Add(hvoSrc);	// add ghost owner
			else
				base.AddResultsToList(hvoSrc, ref hvoList, results);
		}

		/// <summary>
		/// describes the property that owns the actual (non-ghost) items in the list.
		/// </summary>
		public List<ClassAndPropInfo> RealOwningPath
		{
			get { return m_cpiRealTargetPath; }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <returns>the class to create for the given owner, 0 if a default has not been defined.</returns>
		public int GetDefaultClassToCreateForGhost(int hvoOwner)
		{
			int classToCreate = 0;
			if (m_methodInfoClassToCreate != null)
			{
				ICmObject ghostOwner = CmObject.CreateFromDBObject(m_cache, hvoOwner);
				classToCreate = (int)m_methodInfoClassToCreate.Invoke(ghostOwner, new object[] { });
			}
			return classToCreate;
		}
	}

	/// <summary>
	/// virtual property for loading boolean values.
	/// </summary>
	public class FDOBooleanPropertyVirtualHandler : BaseFDOPropertyVirtualHandler
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		public FDOBooleanPropertyVirtualHandler(XmlNode configuration, FdoCache cache)
			: base(configuration, cache)
		{
			Type = (int)CellarModuleDefns.kcptBoolean;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			ICmObject fdoObj = CmObject.CreateFromDBObject(m_cache, hvo);
			bool val = (bool) m_propertyInfo.GetValue(fdoObj, null);
			cda.CacheBooleanProp(hvo, tag, val);
		}
	}


	/// <summary>
	/// This class is used for properties that will always be loaded by some other process,
	/// but we want to have a VH so the MetaDataCache knows about them (e.g., so ClearInfoAbout will
	/// clear the property).
	/// </summary>
	public class DummyVirtualHandler : BaseVirtualHandler
	{
		// Make one using InstallDummyHandler()
		private DummyVirtualHandler()
		{
		}
		/// <summary>
		/// Return the MeVirtualHandler for the supplied cache, creating it if needed.
		/// </summary>
		/// <param name="cda"></param>
		/// <param name="className">name of class to define prop for</param>
		/// <param name="fieldName">field name for virtual prop</param>
		/// <param name="cpt">type of property, from CellarModuleDefns</param>
		/// <returns></returns>
		public static DummyVirtualHandler InstallDummyHandler(IVwCacheDa cda, string className, string fieldName, int cpt)
		{
			DummyVirtualHandler vh = (DummyVirtualHandler)cda.GetVirtualHandlerName(className, fieldName);
			if (vh == null)
			{
				vh = new DummyVirtualHandler();
				vh.Type = cpt;
				vh.ClassName = className;
				vh.FieldName = fieldName;
				cda.InstallVirtual(vh);
			}
			return vh;
		}

		/// <summary>
		/// DummyVirtualHandler assumes all values are preloaded. If not it will produce a default empty value.
		/// Only certain types are currently handled.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			Clear(cda, hvo, ws);
		}
	}
	/// <summary>
	/// This class is used to indicate whether an object is selected in the focus view.
	/// This is really boolean, but the cache doesn't handle booleans well yet, so we'll
	/// make it an integer property, 0 if selected and 1 if not.
	/// Note that code using this should check focus as well, since an object could be
	/// selected in some other window.
	/// </summary>
	public class ObjectSelectedVirtualHandler : BaseVirtualHandler
	{
		// Make one using InstallDummyHandler()
		private ObjectSelectedVirtualHandler()
		{
		}

		const string className = "CmObject";
		const string fieldName = "IsObjectSelected";

		/// <summary>
		/// Return the VirtualHandler for the supplied cache, creating it if needed.
		/// </summary>
		/// <param name="cda"></param>
		/// <returns></returns>
		public static ObjectSelectedVirtualHandler InstallHandler(IVwCacheDa cda)
		{
			ObjectSelectedVirtualHandler vh = (ObjectSelectedVirtualHandler)cda.GetVirtualHandlerName(className, fieldName);
			if (vh == null)
			{
				vh = new ObjectSelectedVirtualHandler();
				vh.Type = (int)CellarModuleDefns.kcptInteger;
				vh.ClassName = className;
				vh.FieldName = fieldName;
				cda.InstallVirtual(vh);
			}
			return vh;
		}

		/// <summary>
		/// If something hasn't already recorded a value for this property for this object,
		/// we assume it is NOT selected.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			cda.CacheIntProp(hvo, tag, 0);
		}
	}

}
