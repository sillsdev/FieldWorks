using System;
using System.Diagnostics;
using System.Xml;
using System.Collections.Generic;
using System.Reflection;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.LangProj;
using SIL.Utils;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// BaseVirtualHandler provides a skeleton of an implementation of IVwVirtualHandler:
	/// basically the common methods that just store various strings, numbers, and booleans.
	///
	/// You MUST override Load() to have a useful implementation. If you set writeable to
	/// true, you must override the appropriate one of the write methods.
	/// </summary>
	public abstract class BaseVirtualHandler : IVwVirtualHandler
	{
		#region Data members

		private int m_tag;
		private string m_className;
		private string m_fieldName;
		private bool m_fWriteable = false;
		private bool m_fComputeEveryTime = false;
		private int m_cpt;
		private List<List<int>> m_fieldPaths = new List<List<int>>();
		/// <summary>
		/// configuration node.
		/// </summary>
		protected XmlNode m_configuration = null;

		#endregion Data members

		#region Constructor
		/// <summary>
		/// Constructor does nothing.
		/// </summary>
		public BaseVirtualHandler()
		{
		}

		/// <summary>
		/// Constructor to set the Classname, FieldName, and ComputeEveryTime properties.
		/// </summary>
		/// <param name="configuration"></param>
		public BaseVirtualHandler(XmlNode configuration)
		{
			ClassName = XmlUtils.GetManditoryAttributeValue(configuration, "modelclass");
			FieldName = XmlUtils.GetManditoryAttributeValue(configuration, "virtualfield");
			ComputeEveryTime = XmlUtils.GetOptionalBooleanAttributeValue(configuration, "computeeverytime", false);
			m_configuration = configuration;
		}

		/// <summary>
		/// Setup DependencyPaths property
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		protected void SetupDependencies(XmlNode configuration, FdoCache cache)
		{
			if (DependencyPaths.Count > 0)
				return;	// already setup.
			string dependsStr = XmlUtils.GetOptionalAttributeValue(configuration, "depends");
			if (dependsStr == null || cache == null)
				return;
			string[] depends = dependsStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string fieldPath in depends)
			{
				string[] fieldtree = fieldPath.Split(new char[] { '.' });
				List<int> tags = new List<int>(fieldtree.Length);
				string srcClassName = this.ClassName;
				foreach (string field in fieldtree)
				{
					int tag = cache.GetFlid(0, srcClassName, field);
					Debug.Assert(tag > 0, String.Format("Invalid dependency field {0} in field path {1}.", field, fieldPath));
					if (tag <= 0)
						break;
					tags.Add(tag);
					if (tags.Count < fieldtree.Length)
					{
						string nextFieldName = fieldtree[tags.Count];
						// make dst class the source class of next field
						uint clsidDst = 0;
						if (field == "OwnerHVO")
						{
							// handle the special case where we need the owning class
							// this is only possible if srcClassName is has a unique owner.
							// find the first class that owns the given 'classId' and the given fieldName.
							clsidDst = GetClassOwningClassAndFieldName(cache, srcClassName, nextFieldName);
						}
						else
						{
							ClassAndPropInfo cpi = cache.GetClassAndPropInfo((uint)tag);
							if (ClassHasField(cache, cache.GetClassName(cpi.signatureClsid), nextFieldName))
							{
								clsidDst = cpi.signatureClsid;
							}
							else if (cpi.isAbstract)
							{
								// find a subclasses that could refer to the nextFieldName.
								clsidDst = GetSubclassOwningNextField(cache, tag, nextFieldName);
							}
						}
						srcClassName = cache.GetClassName(clsidDst);
					}
				}
				m_fieldPaths.Add(tags);
			}
		}

		private static uint GetSubclassOwningNextField(FdoCache cache, int tagOwningSubclasses, string nextFieldName)
		{
			uint clsidDst = 0;
			List<ClassAndPropInfo> subclassesInfo = new List<ClassAndPropInfo>();
			cache.AddClassesForField((uint)tagOwningSubclasses, true, subclassesInfo);
			foreach (ClassAndPropInfo cpiSubclass in subclassesInfo)
			{
				if (ClassHasField(cache, cpiSubclass.signatureClassName, nextFieldName))
				{
					clsidDst = cpiSubclass.signatureClsid;
					break;
				}
			}
			return clsidDst;
		}

		/// <summary>
		/// handle the special case where we need the owning class
		/// this is only possible if srcClassName is has a unique owner.
		/// find the first class that owns the given 'classId' and the given fieldName.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="ownedClassName"></param>
		/// <param name="nextFieldName"></param>
		/// <returns></returns>
		private static uint GetClassOwningClassAndFieldName(FdoCache cache, string ownedClassName, string nextFieldName)
		{
			uint clsidDst = 0;
			uint clsidOwned = cache.MetaDataCacheAccessor.GetClassId(ownedClassName);
			foreach (ClassAndPropInfo cpi in cache.GetFieldsOwningClass(clsidOwned))
			{
				if (ClassHasField(cache, cache.GetClassName(cpi.sourceClsid), nextFieldName))
				{
					clsidDst = cpi.sourceClsid;
					break;
				}
			}
			return clsidDst;
		}

		private static bool ClassHasField(FdoCache cache, string className, string fieldName)
		{
			return cache.GetFlid(0, className, fieldName) > 0;
		}

		#endregion Constructor

		#region Other methods

		/// <summary>
		/// Check to ensure the modelclass and fieldname attributes match the required ClassName and FieldName.
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="className"></param>
		/// <param name="fieldName"></param>
		/// <exception cref="ArgumentException">
		/// Thrown if there is a mis-match between the required names and those given in the XmlNode.
		/// </exception>
		protected void SetAndCheckNames(XmlNode configuration, string className, string fieldName)
		{
			ClassName = className;
			FieldName = fieldName;
			if (configuration != null)
			{
				// Make sure the 'modelclass' and 'virtualfield' attributes match the prescribed ClassName and Fieldname.
				string str = XmlUtils.GetOptionalAttributeValue(configuration, "modelclass", className);
				if (ClassName != str)
					throw new ArgumentException("Invalid 'modelclass' for '" + GetType().Name + "'.");
				str = XmlUtils.GetOptionalAttributeValue(configuration, "virtualfield", fieldName);
				if (FieldName != str)
					throw new ArgumentException("Invalid 'virtualfield' for '" + GetType().Name + "'.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether our Tag has been stored in the cache for the given hvo and ws.
		/// </summary>
		/// <param name="sda">The sda.</param>
		/// <param name="hvo">hvo owner of Tag</param>
		/// <param name="ws">0, if the Tag does not relate to ws.</param>
		/// <returns><c>true</c> if the prop is in the cache; otherwise, <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public bool IsPropInCache(ISilDataAccess sda, int hvo, int ws)
		{
			return sda.get_IsPropInCache(hvo, this.Tag, this.Type, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears the specified cda.
		/// </summary>
		/// <param name="cda">The cda.</param>
		/// <param name="hvo">The HVO.</param>
		/// <param name="ws">The writing system</param>
		/// ------------------------------------------------------------------------------------
		protected internal virtual void Clear(IVwCacheDa cda, int hvo, int ws)
		{
			switch (this.Type)
			{
				case (int)CellarModuleDefns.kcptReferenceSequence:
					cda.CacheVecProp(hvo, Tag, new int[0], 0);
					break;
				case (int)CellarModuleDefns.kcptTime:
					cda.CacheInt64Prop(hvo, Tag, 0);
					break;
				case (int)CellarModuleDefns.kcptReferenceAtom:
					cda.CacheObjProp(hvo, Tag, 0);
					break;
				default:
					Debug.Assert(false);
					break;
			}
		}

		/// <summary>
		/// Many FDO virtual handles store a cache. Override this property as necessary if so.
		/// Note that if Cache returns a disposed cache, the setter will be called with an
		/// up-to-date one when reloading virtuals.
		/// </summary>
		public virtual FdoCache Cache
		{
			get { return null; }
			set { }
		}

		/// <summary>
		/// Notifiers for virtual properties may need to add more items to rghvo and rgtag,
		/// updating chvo appropriately.  See LT-8245.
		/// </summary>
		public virtual void UpdateNotifierLists(ISilDataAccess sda,
			ref int[] rghvo, ref int[] rgtag, ref int chvo)
		{
			// The default implementation does nothing.
		}
		#endregion Other methods

		#region Static methods

		/// <summary>
		/// Looks for a node called "virtuals" and under it collects nodes of type "virtual",
		/// each having at least "modelclass", "virtualfield", "assemblyPath", and "class".
		/// Here 'class' is the name of the C# class that implements the VH, while 'modelclass'
		/// is the name of the virtual property. The node is passed to the VH as a constructor
		/// argument, so other attributes may be required.
		/// </summary>
		/// <returns>the list of installed virtual handlers</returns>
		public static List<IVwVirtualHandler> InstallVirtuals(XmlNode virtualsNode, FdoCache cache)
		{
			return InstallVirtuals(virtualsNode, cache, false);
		}

		/// <summary>
		/// Looks for a node called "virtuals" and under it collects nodes of type "virtual",
		/// each having at least "modelclass", "virtualfield", "assemblyPath", and "class".
		/// Here 'class' is the name of the C# class that implements the VH, while 'modelclass'
		/// is the name of the virtual property. The node is passed to the VH as a constructor
		/// argument, so other attributes may be required.
		/// </summary>
		/// <returns>the list of installed virtual handlers</returns>
		public static List<IVwVirtualHandler> InstallVirtuals(XmlNode virtualsNode, FdoCache cache, bool okToFail)
		{
			List<IVwVirtualHandler> installedHandlers = new List<IVwVirtualHandler>();
			foreach (XmlNode virtualNode in virtualsNode.SelectNodes("virtual"))
			{
				IVwCacheDa cda = cache.MainCacheAccessor as IVwCacheDa;
				IVwVirtualHandler vh = cda.GetVirtualHandlerName(
					XmlUtils.GetManditoryAttributeValue(virtualNode, "modelclass"),
					XmlUtils.GetManditoryAttributeValue(virtualNode, "virtualfield"));
				if (vh != null && vh is BaseVirtualHandler)
				{
					// already exists, hope it's the same one. Make sure its cache is valid.
					(vh as BaseVirtualHandler).Reinitialize(virtualNode, cache);
					installedHandlers.Add(vh);
					continue;
				}

				try
				{
					vh = (IVwVirtualHandler)DynamicLoader.CreateObject(
							virtualNode.SelectSingleNode("dynamicloaderinfo"),
							new object[] { virtualNode, cache });
				}
				catch (Exception err)
				{
					if (!okToFail)
						throw err;
					// Otherwise we're in some special test situation or really just want the IText ones
					// and we ignore the problem.
					continue;
				}
				try
				{
					cda.InstallVirtual(vh);
				}
				catch (Exception err)
				{
					Debug.WriteLine(err.Message);
					throw err;
				}
				installedHandlers.Add(vh);
			}
			return installedHandlers;
		}

		/// <summary>
		/// Install Virtual properties from Fieldworks xml configuration.
		/// </summary>
		/// <param name="fwInstallFile"></param>
		/// <param name="assemblyNamespaces">Comma-separated list of class name prefixes (typically namespace including
		/// trailing period). These are matched against the start of the class attributes of dynamicloaderinfo
		/// children of virtuals/virtual elements to decide which ones are to be loaded.</param>
		/// <param name="cache"></param>
		public static List<IVwVirtualHandler> InstallVirtuals(string fwInstallFile, string[] assemblyNamespaces, FdoCache cache)
		{
			return InstallVirtuals(fwInstallFile, assemblyNamespaces, cache, false);
		}

		/// <summary>
		/// Install Virtual properties from Fieldworks xml configuration.
		/// </summary>
		/// <param name="fwInstallFile"></param>
		/// <param name="assemblyNamespaces">Comma-separated list of class name prefixes (typically namespace including
		/// trailing period). These are matched against the start of the class attributes of dynamicloaderinfo
		/// children of virtuals/virtual elements to decide which ones are to be loaded.</param>
		/// <param name="cache"></param>
		/// <param name="fSkipMissingFiles">true when it is OK for some include files not to be found.</param>
		public static List<IVwVirtualHandler> InstallVirtuals(string fwInstallFile, string[] assemblyNamespaces,
			FdoCache cache, bool fSkipMissingFiles)
		{
			return InstallVirtuals(fwInstallFile, assemblyNamespaces, cache, fSkipMissingFiles, true);
		}

		/// <summary>
		/// Install Virtual properties from Fieldworks xml configuration.
		/// </summary>
		/// <param name="fwInstallFile"></param>
		/// <param name="assemblyNamespaces">Comma-separated list of class name prefixes (typically namespace including
		/// trailing period). These are matched against the start of the class attributes of dynamicloaderinfo
		/// children of virtuals/virtual elements to decide which ones are to be loaded.</param>
		/// <param name="cache"></param>
		/// <param name="fSkipMissingFiles">true when it is OK for some include files not to be found.</param>
		/// <param name="fProcessIncludes">false when you don't want to process configuration includes. helpful for tests
		/// that don't have knowledge of dlls that will get included.</param>
		public static List<IVwVirtualHandler> InstallVirtuals(string fwInstallFile, string[] assemblyNamespaces,
			FdoCache cache, bool fSkipMissingFiles, bool fProcessIncludes)
		{
			string configurationFile = DirectoryFinder.GetFWCodeFile(fwInstallFile);
			XmlDocument configuration = null;
			if (fProcessIncludes)
			{
				configuration = XmlUtils.LoadConfigurationWithIncludes(configurationFile, fSkipMissingFiles);
			}
			else
			{
				configuration = new XmlDocument();
				configuration.Load(configurationFile);
			}

			XmlNode virtuals = configuration.CreateElement("virtuals");
			// get the nodes in original order, so that they will have proper dependency structure.
			XmlNodeList virtualNodes = configuration.SelectNodes("//virtuals/virtual/dynamicloaderinfo");
			foreach (XmlNode node in virtualNodes)
			{
				// filter out the nodes that are not in the requested namespaces
				foreach (string ns in assemblyNamespaces)
				{
					if (node.Attributes["class"].Value.StartsWith(ns))
						virtuals.AppendChild(node.ParentNode);
				}
			}

			return InstallVirtuals(virtuals, cache);
		}

		/// <summary>
		/// Make sure the handler is put in a state where it can be reused.
		/// </summary>
		/// <param name="configurationNode">configuration specifications for the virtual handler</param>
		/// <param name="cache"></param>
		protected virtual void Reinitialize(XmlNode configurationNode, FdoCache cache)
		{
			FdoCache oldCache = this.Cache;
			if (oldCache != null && oldCache.IsDisposed)
				this.Cache = cache;
		}

		/// <summary>
		/// Gets an installed IVwVirtualHandler from the cache.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="modelclass"></param>
		/// <param name="virtualfield"></param>
		/// <returns>The installed IVwVirtualHandler handler.</returns>
		/// <exception cref="ArgumentException">
		/// Thrown if the IVwVirtualHandler IVwVirtualHandler by the two parameters is not found in the givan cache.
		/// </exception>
		public static IVwVirtualHandler GetInstalledHandler(FdoCache cache, string modelclass, string virtualfield)
		{
			IVwVirtualHandler vh =  cache.VwCacheDaAccessor.GetVirtualHandlerName(modelclass, virtualfield);
			if (vh == null)
				throw new ArgumentException(String.Format("Virtual handler '{0}/{1}' not installed.", modelclass, virtualfield));

			return vh;
		}

		/// <summary>
		/// Gets an installed IVwVirtualHandler tag (the virtual flid) from the cache.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="modelclass"></param>
		/// <param name="virtualfield"></param>
		/// <returns>The installed IVwVirtualHandler handler's flid (virtual flid).</returns>
		public static int GetInstalledHandlerTag(FdoCache cache, string modelclass, string virtualfield)
		{
			IVwVirtualHandler handler = cache.VwCacheDaAccessor.GetVirtualHandlerName(modelclass, virtualfield);
			if (handler != null)
				return handler.Tag;
			return 0;
		}

		/// <summary>
		/// Get the Type in the FDO assembly that matches the value found in the 'modelclass' attribute.
		/// That attribute should not have a fully specified namespace+Name, but only the class name.
		/// </summary>
		/// <param name="virtualNode"></param>
		/// <returns></returns>
		public static Type GetTypeFromXml(XmlNode virtualNode)
		{
			string fdoClassName = XmlUtils.GetManditoryAttributeValue(virtualNode, "modelclass");
			foreach(Type typ in Assembly.GetExecutingAssembly().GetTypes())
			{
				if (typ.Name == fdoClassName)
					return typ;
			}

			return null;
		}

		#endregion Static methods

		#region IVwVirtualHandler Members (and related C# property implementations)

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the data for property tag of object hvo (and the specified writing system, if relevant)
		/// into the cache. This is the fundamental method you must implement to make a virual hander.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="ws">The ws.</param>
		/// <param name="_cda">The _cda.</param>
		/// ------------------------------------------------------------------------------------
		public abstract void Load(int hvo, int tag, int ws, IVwCacheDa _cda);

		/// <summary>
		/// (uses Tag internally)
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="ws"></param>
		/// <param name="_cda"></param>
		public void Load(int hvo, int ws, IVwCacheDa _cda)
		{
			Load(hvo, Tag, ws, _cda);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This may be used to pre-load into the cache the value of property tag (and the
		/// specified ws, if applicable) for all the objects in the array. This can sometimes
		/// be more efficient than using Load for each property. This is currently not used
		/// by the framework, though the plan is that certain code may use it when it is known
		/// that values will be needed for the same property of several objects. It is always
		/// safe not to implement it; the worst that happens is that Load is called each time,
		/// with possibly disappointing performance.
		/// </summary>
		/// <param name="chvo">The chvo.</param>
		/// <param name="_rghvo">The _rghvo.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="ws">The ws.</param>
		/// <param name="_cda">The _cda.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void PreLoad(int chvo, int[] _rghvo, int tag, int ws, IVwCacheDa _cda)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the tag that identifies the property to the Views code.
		/// The setter should be called only by the Views subsystem (during InstallVirtual),
		/// and only once.
		/// </summary>
		/// <value>The tag.</value>
		/// ------------------------------------------------------------------------------------
		public int Tag
		{
			get { return m_tag; }
			set
			{
				if (m_tag != 0)
				{
					Debug.Assert(m_tag == 0); // Can only be called once.
				}
				m_tag = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the type of the property. Set once, before installing. One of the
		/// CmTypes.h constants.
		/// Do NOT use kcptVirtual, nor something OR'd with kcptVirtual.
		/// It is recommended but not required that we use kcptReferenceAtomic and kcptRefSequence
		/// rather than the other object types. kcptGuid, kcptBinary, and kcptUnknown are currently
		/// not supported.
		/// </summary>
		/// <value>The type.</value>
		/// ------------------------------------------------------------------------------------
		public int Type
		{
			get { return m_cpt; }
			set
			{
				if (m_tag != 0)
				{
					Debug.Assert(m_tag != 0);
					return;
				}
				m_cpt = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the field. Modifying it after the handler is installed is
		/// not allowed.
		/// </summary>
		/// <value>The name of the field.</value>
		/// ------------------------------------------------------------------------------------
		public string FieldName
		{
			get { return m_fieldName; }
			set {
				if (m_tag != 0)
				{
					Debug.Assert(m_tag != 0);
					return;
				}
				m_fieldName = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the class that has this property. Modifying it after
		/// it is installed is not allowed.
		/// </summary>
		/// <value>The name of the class.</value>
		/// ------------------------------------------------------------------------------------
		public string ClassName
		{
			get { return m_className; }
			set
			{
				if (m_tag != 0)
				{
					Debug.Assert(m_tag != 0);
					return;
				}
				m_className = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets whether the property can be written. Depending on the property type,
		/// you must override one of the Write methods (or Replace) if you set this true.
		/// </summary>
		/// <value><c>true</c> if writeable; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public virtual bool Writeable
		{
			get { return m_fWriteable; }
			set { m_fWriteable = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value if the property should be computed every time it is read.
		/// This means it is removed from the cache immediately after your Load method puts it there.
		/// If you set this true you should not implement PreLoad.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool ComputeEveryTime
		{
			get { return m_fComputeEveryTime; }
			set
			{
				if (m_tag != 0)
				{
					Debug.Assert(m_tag != 0);
					return;
				}
				m_fComputeEveryTime = value;
			}
		}

		static private bool m_fForceBulkLoad = false;

		/// <summary>
		/// This contains the list of virtual handlers which have called SetLoadForAllOfClass(true)
		/// in response to ForceBulkLoadIfPossible.
		/// </summary>
		static protected List<BaseVirtualHandler> m_rgvhBulkForced = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For some purposes (such as Dictionary View), we want to globally enable bulk loading
		/// while the display is happening, but keep it disabled at other times.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if [force bulk load if possible]; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		static public bool ForceBulkLoadIfPossible
		{
			get { return m_fForceBulkLoad; }
			set
			{
				if (m_fForceBulkLoad == value)
					return;
				m_fForceBulkLoad = value;
				if (m_fForceBulkLoad)
				{
					m_rgvhBulkForced = new List<BaseVirtualHandler>();
				}
				else
				{
					Debug.Assert(m_rgvhBulkForced != null);
					for (int i = 0; i < m_rgvhBulkForced.Count; ++i)
						m_rgvhBulkForced[i].SetLoadForAllOfClass(false);
					m_rgvhBulkForced = null;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid paths that the object owning this property may depend upon.
		/// </summary>
		/// <value>The dependency paths.</value>
		/// ------------------------------------------------------------------------------------
		public List<List<int>> DependencyPaths
		{
			get { return m_fieldPaths; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use to try to reload the ws from the configuration node. Default writing systems such as "vernacular" or "analysis"
		/// may change after initialization. (cf. LT-4882).
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="hvo">the object we want to find the ws for magic writing systems (e.g. for best analysis)</param>
		/// <param name="defaultWsId">ws we will use if we can't find one.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual int WsId(FdoCache cache, int hvo, int defaultWsId)
		{
			int ws = 0;
			if (m_configuration != null && cache != null)
			{
				if (hvo != 0)
				{
					int flid = cache.GetFlid(hvo, ClassName, FieldName);
					ws = LangProject.GetWritingSystem(m_configuration, cache, null, hvo, flid, defaultWsId);
				}
				else
				{
					ws = LangProject.GetWritingSystem(m_configuration, cache, null, 0);
				}
			}
			if (ws == 0)
				ws = defaultWsId;
			return ws;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is available for a particular implementatin to use in any way.
		/// The idea is to make an implementation that can do different things depending on some
		/// initialization.
		/// </summary>
		/// <param name="bstrData">The BSTR data.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void Initialize(string bstrData)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When fLoadAll is true, the virtual handler may try to preload all of its data
		/// on the first subsequent call to Load even if ComputeEveryTime is true.  When
		/// fLoadAll is false, the virtual handler will unload (remove from the cache) all
		/// data loaded as a result of a prior call with fLoadAll set to true if ComputeEveryTime
		/// is true. (Note that if ComputeEveryTime is true, the Views code will still
		/// remove the value from the cache after using it. Therefore the handler must retain
		/// its own copy of the preloaded data until the call to SetLoadForAllOfClass(false).
		/// Typically, therefore, data is put into the actual cache only one property at a
		/// time; but it may be loaded into some private memory of the handler in advance,
		/// often with significant time savings. If ComputeEveryTime is false, the first
		/// Load call may simply load the data into the main cache for all objects.)
		/// A handler may also simply ignore this setting, if it will not benefit by
		/// preloading data for all objects.
		/// </summary>
		/// <param name="fLoadAll">if set to <c>true</c> [f load all].</param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetLoadForAllOfClass(bool fLoadAll)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is called by the framework when a writeable virtual property of one of the object
		/// types (owning or reference, atomic, sequence, or collection) is written.
		/// For a sequence (or collection, in whatever order your virtual property keeps it), it
		/// indicates that the objects from ihvoMin to ihvoLim are being replaced by the objects
		/// in rghvo. (chvo, redundantly for C#, gives the number of objects in rghvo).
		/// For an atomic property, rghvo is empty to set it to null, or contains the one object.
		/// Don't depend on ihvoMin and ihvoLim for atomic properties.
		/// The implementation should take whatever steps are needed to store the change.
		/// You can retrieve the old value of the property from the sda.
		/// The framework will automatically update the value in the cache after your method returns,
		/// unless the property is ComputeEveryTime.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="ihvoMin">The ihvo min.</param>
		/// <param name="ihvoLim">The ihvo lim.</param>
		/// <param name="_rghvo">The _rghvo.</param>
		/// <param name="chvo">The chvo.</param>
		/// <param name="_sda">The _sda.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void Replace(int hvo, int tag, int ihvoMin, int ihvoLim,
			int[] _rghvo, int chvo, ISilDataAccess _sda)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is called by the framework when a writeable virtual property of type kcptInt,
		/// kcptInt64, or kcptTime is written.
		/// The implementation should take whatever steps are needed to store the change.
		/// You can retrieve the old value of the property from the sda.
		/// The framework will automatically update the value in the cache after your method returns,
		/// unless the property is ComputeEveryTime.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="val">The val.</param>
		/// <param name="_sda">The _sda.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void WriteInt64(int hvo, int tag, long val, ISilDataAccess _sda)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is called by the framework when a writeable virtual property of type kcptUnicode
		/// is written.  It may eventually also be used for kcptGuid, and conceivably also for kcptBinary.
		/// The implementation should take whatever steps are needed to store the change.
		/// You can retrieve the old value of the property from the sda.
		/// The framework will automatically update the value in the cache after your method returns,
		/// unless the property is ComputeEveryTime.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="bstr">The string.</param>
		/// <param name="_sda">The _sda.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void WriteUnicode(int hvo, int tag, string bstr, ISilDataAccess _sda)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is called by the framework when a writeable virtual property of type kcptString
		/// or kcptMultiString is written. The _unk parameter may be cast to an ITsString and is
		/// the new value.
		/// The ws parameter is meaningful only for multistrings.
		/// (Note: we pass the argument as an object so this method can eventually be extended
		/// to handle other data types, particularly properties of type kcptUnknown.)
		/// The implementation should take whatever steps are needed to store the change.
		/// You can retrieve the old value of the property from the sda.
		/// The framework will automatically update the value in the cache after your method returns,
		/// unless the property is ComputeEveryTime.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="ws">The ws.</param>
		/// <param name="_unk">The _unk.</param>
		/// <param name="_sda">The _sda.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void WriteObj(int hvo, int tag, int ws, object _unk, ISilDataAccess _sda)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method may be implemented to inform callers that the results computed by
		/// a virtual handler are affected by a property change. (Many implementers do not
		/// implement this comprehensively and just return false.)
		/// This default implementation just answers false for everything.
		/// </summary>
		/// <param name="hvoObj">Having the virtual property</param>
		/// <param name="hvoChange">Having the property that changed</param>
		/// <param name="tag">Identifies the property</param>
		/// <param name="ws">Identifies the writing system, if relevant</param>
		/// <returns>
		/// true if the value of the property implemented by this handler depends on
		/// the specified property of the specified object.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool DoesResultDependOnProp(int hvoObj, int hvoChange, int tag, int ws)
		{
			return false;
		}
		#endregion

		/// <summary>
		/// Install a virtual property (often a subclass implementation) from specified XML. Return the tag.
		/// </summary>
		public static int InstallVirtual(FdoCache cache, string source)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(source);
			XmlNode virtualNode = doc.DocumentElement;
			// This one is used so widely we will create it here if not already done.
			IVwVirtualHandler vh = (IVwVirtualHandler)DynamicLoader.CreateObject(
														virtualNode.SelectSingleNode("dynamicloaderinfo"),
														new object[] { virtualNode, cache });
			cache.VwCacheDaAccessor.InstallVirtual(vh);
			return vh.Tag;
		}
	}
}
