// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LCMBrowser
{
	/// <summary />
	public static class LCMClassList
	{
		private static Dictionary<Type, LCMClass> s_LCMClassesByType;
		private static Assembly s_asmLCM;
		private static string s_settingFileName;

		/// <summary />
		static LCMClassList()
		{
			try
			{
				s_settingFileName = Path.Combine(System.Windows.Forms.Application.LocalUserAppDataPath, "ClassSettings.xml");

				FindLCMAssembly();
				LoadAllLCMClasses();
				LoadCmObjectProperties();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		/// <summary>
		/// Finds the LCM assembly.
		/// </summary>
		private static void FindLCMAssembly()
		{
			// Find the SIL.LCModel.dll assembly.
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (asm.FullName.StartsWith("SIL.LCModel,"))
				{
					s_asmLCM = asm;
					return;
				}
			}
		}

		/// <summary>
		/// Reloads from the persisted storage, all LCM classes and the displayed state of
		/// all their properties.
		/// </summary>
		public static void Reset()
		{
			LoadAllLCMClasses();
		}

		/// <summary>
		/// Loads all LCM classes.
		/// </summary>
		private static void LoadAllLCMClasses()
		{
			LoadLCMClassNames();

			if (File.Exists(s_settingFileName))
			{
				AllLcmClasses = XmlSerializationHelper.DeserializeFromFile<List<LCMClass>>(s_settingFileName);

				if (AllLcmClasses != null)
				{
					// Go through the list of deserialized classes and make sure there aren't
					// any that used to be in the meta data cache but are no longer there.
					for (var i = AllLcmClasses.Count - 1; i >= 0; i--)
					{
						if (!AllLcmClassNames.Contains(AllLcmClasses[i].ClassName))
						{
							AllLcmClasses.RemoveAt(i);
						}
					}

					// Go through the LCM class names from the meta data cache and
					// make sure all of them are found in the list just deserialized.
					foreach (var name in AllLcmClassNames)
					{
						// Search the deserialized list for the class name.
						if (!AllLcmClasses.Any(cls => cls.ClassName == name))
						{
							// If the class was not found in the deserialized list,
							// then add it to the list.
							AllLcmClasses.Add(new LCMClass(GetLCMClassType(name)));
						}
					}
				}
			}

			if (AllLcmClasses == null)
			{
				AllLcmClasses = new List<LCMClass>();
				foreach (var name in AllLcmClassNames)
				{
					AllLcmClasses.Add(GetLCMClassProperties(name));
				}

				AllLcmClasses.Sort((c1, c2) => c1.ClassName.CompareTo(c2.ClassName));
			}

			// Store the classes in a list accessible by the class' type.
			s_LCMClassesByType = new Dictionary<Type, LCMClass>();
			foreach (var cls in AllLcmClasses)
			{
				s_LCMClassesByType[cls.ClassType] = cls;
			}
		}

		/// <summary>
		/// Loads the list of all LCM class names from the meta data cache.
		/// </summary>
		private static void LoadLCMClassNames()
		{
			if (AllLcmClassNames != null)
			{
				return;
			}

			using (var threadHelper = new ThreadHelper())
			using (var cache = LcmCache.CreateCacheWithNoLangProj(new BrowserProjectId(BackendProviderType.kMemoryOnly, null), "en", new SilentLcmUI(threadHelper), FwDirectoryFinder.LcmDirectories, new LcmSettings()))
			{
				var mdc = cache.GetManagedMetaDataCache();
				AllLcmClassNames = new List<string>();

				foreach (var clsid in mdc.GetClassIds())
				{
					AllLcmClassNames.Add(mdc.GetClassName(clsid));
				}

				AllLcmClassNames.Sort((x, y) => x.CompareTo(y));
			}
		}

		/// <summary>
		/// Loads a table with the properties found in the CmObject class.
		/// </summary>
		private static void LoadCmObjectProperties()
		{
			var clsProps = GetLCMClassProperties("CmObject");
			CmObjectProperties = new List<string>();
			foreach (var prop in clsProps.Properties)
			{
				CmObjectProperties.Add(prop.Name);
			}

			CmObjectProperties.Sort((x, y) => x.CompareTo(y));
		}

		/// <summary>
		/// Saves this instance.
		/// </summary>
		public static void Save()
		{
			XmlSerializationHelper.SerializeToFile(s_settingFileName, AllLcmClasses);
		}

		/// <summary>
		/// Gets the loaded LCM assembly.
		/// </summary>
		public static Assembly LCMAssembly
		{
			get
			{
				if (s_asmLCM == null)
				{
					FindLCMAssembly();
				}

				return s_asmLCM;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether or not to show CmObject properties for
		/// classes derived from CmObject.
		/// </summary>
		public static bool ShowCmObjectProperties { get; set; } = false;

		/// <summary>
		/// Gets a list of all the LCM classes.
		/// </summary>
		public static List<string> AllLcmClassNames { get; private set; }

		/// <summary>
		/// Gets a list of all the LCM classes and their properties.
		/// </summary>
		public static List<LCMClass> AllLcmClasses { get; private set; }

		/// <summary>
		/// Gets the cm object properties.
		/// </summary>
		public static List<string> CmObjectProperties { get; private set; }

		/// <summary>
		/// Determines whether or not the specified property name is a CmObject property.
		/// </summary>
		public static bool IsCmObjectProperty(string propName)
		{
			return CmObjectProperties.Contains(propName);
		}

		/// <summary>
		/// Determines whether or not the specified property name of the specified object
		/// should be displayed.
		/// </summary>
		public static bool IsPropertyDisplayed(ICmObject cmObj, string propName)
		{
			if (!ShowCmObjectProperties && IsCmObjectProperty(propName))
			{
				return false;
			}
			LCMClass cls;
			return s_LCMClassesByType.TryGetValue(cmObj.GetType(), out cls) && cls.IsPropertyDisplayed(propName);
		}

		/// <summary>
		/// Gets the properties for the specified class.
		/// </summary>
		public static Type GetLCMClassType(string className)
		{
			return s_asmLCM.GetTypes().FirstOrDefault(type => type.Name == className);
		}

		/// <summary>
		/// Gets a list of all the repository types found in LCM.
		/// </summary>
		public static IEnumerable<Type> RepositoryTypes => s_asmLCM.GetTypes().Where(type => type.IsInterface && type.GetInterface("IRepository`1") != null);

		/// <summary>
		/// Gets the properties for the specified class.
		/// </summary>
		public static LCMClass GetLCMClassProperties(string className)
		{
			var type = GetLCMClassType(className);
			return (type != null ? new LCMClass(type) : null);
		}
	}
}