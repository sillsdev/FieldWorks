using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Xml.Serialization;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO;
using System.Collections;
using System.IO;
using SIL.Utils;

namespace FDOBrowser
{
	#region FDOClassList class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class FDOClassList
	{
		private static List<string> s_cmObjectProperties;
		private static List<string> s_allFDOClassNames;
		private static List<FDOClass> s_allFDOClasses;
		private static Dictionary<Type, FDOClass> s_FDOClassesByType;
		private static Assembly s_asmFDO;
		private static string s_settingFileName;
		private static bool s_showCmObjProps = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FDOClassList"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static FDOClassList()
		{
			s_settingFileName = System.Windows.Forms.Application.LocalUserAppDataPath;
			s_settingFileName = Path.Combine(s_settingFileName, "ClassSettings.xml");

			FindFDOAssembly();
			LoadAllFDOClasses();
			LoadCmObjectProperties();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the FDO assembly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void FindFDOAssembly()
		{
			// Find the FDO.dll assembly.
			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (asm.FullName.StartsWith("FDO,"))
				{
					s_asmFDO = asm;
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
			LoadAllFDOClasses();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads all FDO classes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void LoadAllFDOClasses()
		{
			LoadFDOClassNames();

			if (File.Exists(s_settingFileName))
			{
				s_allFDOClasses =
					XmlSerializationHelper.DeserializeFromFile<List<FDOClass>>(s_settingFileName);

				if (s_allFDOClasses != null)
				{
					// Go through the list of deserialized classes and make sure there aren't
					// any that used to be in the meta data cache but are no longer there.
					for (int i = s_allFDOClasses.Count - 1; i >= 0; i--)
					{
						if (!s_allFDOClassNames.Contains(s_allFDOClasses[i].ClassName))
							s_allFDOClasses.RemoveAt(i);
					}

					// Go through the FDO class names from the meta data cache and
					// make sure all of them are found in the list just deserialized.
					foreach (string name in s_allFDOClassNames)
					{
						// Search the deserialized list for the class name.
						var query = from cls in s_allFDOClasses
									where cls.ClassName == name
									select cls;

						// If the class was not found in the deserialized list,
						// then add it to the list.
						if (query.Count() == 0)
							s_allFDOClasses.Add(new FDOClass(GetFDOClassType(name)));
					}
				}
			}

			if (s_allFDOClasses == null)
			{
				s_allFDOClasses = new List<FDOClass>();
				foreach (string name in AllFDOClassNames)
					s_allFDOClasses.Add(GetFDOClassProperties(name));

				s_allFDOClasses.Sort((c1, c2) => c1.ClassName.CompareTo(c2.ClassName));
			}

			// Store the classes in a list accessible by the class' type.
			s_FDOClassesByType = new Dictionary<Type, FDOClass>();
			foreach (FDOClass cls in s_allFDOClasses)
				s_FDOClassesByType[cls.ClassType] = cls;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the list of all FDO class names from the meta data cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void LoadFDOClassNames()
		{
			if (s_allFDOClassNames != null)
				return;

			using (FdoCache cache = FdoCache.CreateCacheWithNoLangProj(new BrowserProjectId(FDOBackendProviderType.kMemoryOnly, null), "en", null, new FdoUserAction()))
			{
			IFwMetaDataCacheManaged mdc = (IFwMetaDataCacheManaged)cache.MainCacheAccessor.MetaDataCache;
			s_allFDOClassNames = new List<string>();

			foreach (int clsid in mdc.GetClassIds())
				s_allFDOClassNames.Add(mdc.GetClassName(clsid));

			s_allFDOClassNames.Sort((x, y) => x.CompareTo(y));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads a table with the properties found in the CmObject class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void LoadCmObjectProperties()
		{
			FDOClass clsProps = GetFDOClassProperties("CmObject");
			s_cmObjectProperties = new List<string>();
			foreach (FDOClassProperty prop in clsProps.Properties)
				s_cmObjectProperties.Add(prop.Name);

			s_cmObjectProperties.Sort((x, y) => x.CompareTo(y));
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
		public static Assembly FDOAssembly
		{
			get
			{
				if (s_asmFDO == null)
					FindFDOAssembly();

				return s_asmFDO;
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
		public static List<string> AllFDOClassNames
		{
			get	{ return s_allFDOClassNames; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of all the FDO classes and their properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static List<FDOClass> AllFDOClasses
		{
			get	{ return s_allFDOClasses; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cm object properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static List<string> CmObjectProperties
		{
			get	{ return s_cmObjectProperties; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the specified property name is a CmObject property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool IsCmObjectProperty(string propName)
		{
			return s_cmObjectProperties.Contains(propName);
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
				FDOClass cls;
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
		public static Type GetFDOClassType(string className)
		{
			foreach (Type type in s_asmFDO.GetTypes())
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
				return from type in s_asmFDO.GetTypes()
					   where type.IsInterface && type.GetInterface("IRepository`1") != null
					   select type;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the properties for the specified class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static FDOClass GetFDOClassProperties(string className)
		{
			Type type = GetFDOClassType(className);
			return (type != null ? new FDOClass(type) : null);
		}
	}

	#endregion

	#region FDOClass class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FDOClass
	{
		private Type m_classType;
		private string m_className;
		private List<FDOClassProperty> m_properties = new List<FDOClassProperty>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FDOClass"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FDOClass()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FDOClass"/> class.
		/// </summary>
		/// <param name="className">Name of the class.</param>
		/// ------------------------------------------------------------------------------------
		public FDOClass(string className) : this()
		{
			ClassName = className;
			InitPropsForType(m_classType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FDOClass"/> class.
		/// </summary>
		/// <param name="type">The type.</param>
		/// ------------------------------------------------------------------------------------
		public FDOClass(Type type) : this(type.Name)
		{
			InitPropsForType(type);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clones this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FDOClass Clone()
		{
			// Make copies of all the class' properties.
			List<FDOClassProperty> props = new List<FDOClassProperty>();
			foreach (FDOClassProperty clsProp in Properties)
				props.Add(new FDOClassProperty { Name = clsProp.Name, Displayed = clsProp.Displayed });

			FDOClass cls = new FDOClass(m_classType);
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
				m_classType = FDOClassList.GetFDOClassType(value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlElement("property")]
		public List<FDOClassProperty> Properties
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
					m_classType = FDOClassList.GetFDOClassType(m_className);

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
				Properties.Add(new FDOClassProperty {Name = pi.Name, Displayed = true});

			Properties.Sort((p1, p2) => p1.Name.CompareTo(p2.Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the specified property is displayed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsPropertyDisplayed(string propName)
		{
			foreach (FDOClassProperty prop in Properties)
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

	#region FDOClassProperty
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FDOClassProperty
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
		/// Gets or sets a value indicating whether this <see cref="T:FDOClassProperty"/> is displayed.
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
