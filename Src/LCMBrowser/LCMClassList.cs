// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Infrastructure;
using SIL.LCModel;
using System.IO;

namespace LCMBrowser
{
	#region FDOClassList class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class LCMClassList
	{
		private static List<string> s_allLCMClassNames;
		private static List<LCMClass> s_allFDOClasses;
		private static Dictionary<Type, LCMClass> s_FDOClassesByType;
		private static Assembly s_LCMAssembly;
		private static string s_settingFileName;
		private static bool s_showCmObjProps = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FDOClassList"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static LCMClassList()
		{
			s_settingFileName = System.Windows.Forms.Application.LocalUserAppDataPath;
			s_settingFileName = Path.Combine(s_settingFileName, "ClassSettings.xml");

			FindLCMAssembly();
			LoadAllLCMClasses();
			LoadCmObjectProperties();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the FDO assembly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void FindLCMAssembly()
		{
			// Find the FDO.dll assembly.
			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (asm.FullName.StartsWith("SIL.LCModel,"))
				{
					s_LCMAssembly = asm;
					return;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reloads from the persisted storage, all FDO classes and the displayed state of
		/// all their properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void Reset()
		{
			LoadAllLCMClasses();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads all FDO classes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void LoadAllLCMClasses()
		{
			LoadFDOClassNames();

			if (File.Exists(s_settingFileName))
			{
				s_allFDOClasses =
					XmlSerializationHelper.DeserializeFromFile<List<LCMClass>>(s_settingFileName);

				if (s_allFDOClasses != null)
				{
					// Go through the list of deserialized classes and make sure there aren't
					// any that used to be in the meta data cache but are no longer there.
					for (int i = s_allFDOClasses.Count - 1; i >= 0; i--)
					{
						if (!s_allLCMClassNames.Contains(s_allFDOClasses[i].ClassName))
							s_allFDOClasses.RemoveAt(i);
					}

					// Go through the FDO class names from the meta data cache and
					// make sure all of them are found in the list just deserialized.
					foreach (string name in s_allLCMClassNames)
					{
						// Search the deserialized list for the class name.
						var query = from cls in s_allFDOClasses
									where cls.ClassName == name
									select cls;

						// If the class was not found in the deserialized list,
						// then add it to the list.
						if (query.Count() == 0)
							s_allFDOClasses.Add(new LCMClass(GetLCMClassType(name)));
					}
				}
			}

			if (s_allFDOClasses == null)
			{
				s_allFDOClasses = new List<LCMClass>();
				foreach (string name in AllLcmClassNames)
					s_allFDOClasses.Add(GetLCMClassProperties(name));

				s_allFDOClasses.Sort((c1, c2) => c1.ClassName.CompareTo(c2.ClassName));
			}

			// Store the classes in a list accessible by the class' type.
			s_FDOClassesByType = new Dictionary<Type, LCMClass>();
			foreach (LCMClass cls in s_allFDOClasses)
				s_FDOClassesByType[cls.ClassType] = cls;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the list of all FDO class names from the meta data cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void LoadFDOClassNames()
		{
			if (s_allLCMClassNames != null)
				return;

			using (var threadHelper = new ThreadHelper())
			using (LcmCache cache = LcmCache.CreateCacheWithNoLangProj(new BrowserProjectId(BackendProviderType.kMemoryOnly, null), "en",
				new SilentLcmUI(threadHelper), FwDirectoryFinder.LcmDirectories, new LcmSettings()))
			{
				IFwMetaDataCacheManaged mdc = (IFwMetaDataCacheManaged)cache.MainCacheAccessor.MetaDataCache;
				s_allLCMClassNames = new List<string>();

				foreach (int clsid in mdc.GetClassIds())
					s_allLCMClassNames.Add(mdc.GetClassName(clsid));

				s_allLCMClassNames.Sort((x, y) => x.CompareTo(y));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads a table with the properties found in the CmObject class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void LoadCmObjectProperties()
		{
			LCMClass clsProps = GetLCMClassProperties("CmObject");
			CmObjectProperties = new List<string>();
			foreach (LCMClassProperty prop in clsProps.Properties)
				CmObjectProperties.Add(prop.Name);

			CmObjectProperties.Sort((x, y) => x.CompareTo(y));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void Save()
		{
			XmlSerializationHelper.SerializeToFile(s_settingFileName, AllFDOClasses);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the loaded FDO assembly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Assembly LCMAssembly
		{
			get
			{
				if (s_LCMAssembly == null)
					FindLCMAssembly();

				return s_LCMAssembly;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not to show CmObject properties for
		/// classes derived from CmObject.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ShowCmObjectProperties
		{
			get { return s_showCmObjProps; }
			set { s_showCmObjProps = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of all the FDO classes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static List<string> AllLcmClassNames
		{
			get	{ return s_allLCMClassNames; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of all the FDO classes and their properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static List<LCMClass> AllFDOClasses
		{
			get	{ return s_allFDOClasses; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cm object properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static List<string> CmObjectProperties { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the specified property name is a CmObject property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool IsCmObjectProperty(string propName)
		{
			return CmObjectProperties.Contains(propName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the specified property name of the specified object
		/// should be displayed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool IsPropertyDisplayed(ICmObject cmObj, string propName)
		{
			if (s_showCmObjProps || !IsCmObjectProperty(propName))
			{
				LCMClass cls;
				if (s_FDOClassesByType.TryGetValue(cmObj.GetType(), out cls))
					return cls.IsPropertyDisplayed(propName);
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the properties for the specified class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Type GetLCMClassType(string className)
		{
			foreach (Type type in s_LCMAssembly.GetTypes())
			{
				if (type.Name == className)
					return type;
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of all the repository types found in FDO.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static IEnumerable<Type> RepositoryTypes
		{
			get
			{
				return from type in s_LCMAssembly.GetTypes()
					   where type.IsInterface && type.GetInterface("IRepository`1") != null
					   select type;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the properties for the specified class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static LCMClass GetLCMClassProperties(string className)
		{
			Type type = GetLCMClassType(className);
			return (type != null ? new LCMClass(type) : null);
		}
	}

	#endregion

	#region LCMClass class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class LCMClass
	{
		private Type m_classType;
		private string m_className;
		private List<LCMClassProperty> m_properties = new List<LCMClassProperty>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:LCMClass"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LCMClass()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:LCMClass"/> class.
		/// </summary>
		/// <param name="className">Name of the class.</param>
		/// ------------------------------------------------------------------------------------
		public LCMClass(string className) : this()
		{
			ClassName = className;
			InitPropsForType(m_classType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:LCMClass"/> class.
		/// </summary>
		/// <param name="type">The type.</param>
		/// ------------------------------------------------------------------------------------
		public LCMClass(Type type) : this(type.Name)
		{
			InitPropsForType(type);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clones this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LCMClass Clone()
		{
			// Make copies of all the class' properties.
			List<LCMClassProperty> props = new List<LCMClassProperty>();
			foreach (LCMClassProperty clsProp in Properties)
				props.Add(new LCMClassProperty { Name = clsProp.Name, Displayed = clsProp.Displayed });

			LCMClass cls = new LCMClass(m_classType);
			cls.Properties = props;
			return cls;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute]
		public string ClassName
		{
			get { return m_className; }
			set
			{
				m_className = value;
				m_classType = LCMClassList.GetLCMClassType(value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlElement("property")]
		public List<LCMClassProperty> Properties
		{
			get { return m_properties; }
			set { m_properties = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Type ClassType
		{
			get
			{
				if (m_classType == null && m_className != null)
					m_classType = LCMClassList.GetLCMClassType(m_className);

				return m_classType;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the Properties list for the specified type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitPropsForType(Type type)
		{
			Properties.Clear();
			BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
			PropertyInfo[] props = type.GetProperties(flags);

			foreach (PropertyInfo pi in props)
				Properties.Add(new LCMClassProperty {Name = pi.Name, Displayed = true});

			Properties.Sort((p1, p2) => p1.Name.CompareTo(p2.Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the specified property is displayed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsPropertyDisplayed(string propName)
		{
			foreach (LCMClassProperty prop in Properties)
			{
				if (prop.Name == propName)
					return prop.Displayed;
			}

			return false;
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
			return ClassName;
		}
	}

	#endregion

	#region LCMClassProperty
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class LCMClassProperty
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute]
		public string Name { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:LCMClassProperty"/> is displayed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute]
		public bool Displayed { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Name;
		}
	}

	#endregion
}
