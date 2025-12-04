// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SIL.LCModel;

namespace SIL.ObjectBrowser
{
	/// <summary>
	/// Represents a single FDO property with its metadata.
	/// </summary>
	public class FDOClassProperty
	{
		public string Name { get; set; }
		public bool Displayed { get; set; }
		public PropertyInfo PropertyInfo { get; set; }

		public FDOClassProperty(PropertyInfo propInfo)
		{
			PropertyInfo = propInfo;
			Name = propInfo.Name;
			Displayed = true;
		}

		public override string ToString()
		{
			return Name;
		}
	}

	/// <summary>
	/// Represents a single FDO class with its properties.
	/// </summary>
	public class FDOClass
	{
		public string ClassName { get; set; }
		public Type ClassType { get; set; }
		public List<FDOClassProperty> Properties { get; set; }

		public FDOClass(Type type)
		{
			ClassType = type;
			ClassName = type.Name;
			Properties = new List<FDOClassProperty>();

			// Get all public properties from the type
			var props = type.GetProperties(
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
			);
			foreach (var prop in props.OrderBy(p => p.Name))
			{
				Properties.Add(new FDOClassProperty(prop));
			}
		}

		public override string ToString()
		{
			return ClassName;
		}
	}

	/// <summary>
	/// Static helper class to manage all FDO classes and their properties.
	/// </summary>
	public static class FDOClassList
	{
		private static List<FDOClass> s_allFDOClasses = null;
		private static HashSet<string> s_cmObjectProperties = null;
		public static bool ShowCmObjectProperties { get; set; } = true;

		static FDOClassList()
		{
			InitializeClasses();
		}

		private static void InitializeClasses()
		{
			s_allFDOClasses = new List<FDOClass>();
			s_cmObjectProperties = new HashSet<string>();

			// Get all types from SIL.LCModel that implement ICmObject
			var lcModelAssembly = typeof(ICmObject).Assembly;
			var cmObjectTypes = lcModelAssembly
				.GetTypes()
				.Where(t =>
					typeof(ICmObject).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract
				)
				.OrderBy(t => t.Name);

			foreach (var type in cmObjectTypes)
			{
				s_allFDOClasses.Add(new FDOClass(type));
			}

			// Common CmObject properties that might be hidden
			s_cmObjectProperties.Add("Guid");
			s_cmObjectProperties.Add("ClassID");
			s_cmObjectProperties.Add("OwningFlid");
			s_cmObjectProperties.Add("OwnFlid");
			s_cmObjectProperties.Add("Owner");
		}

		public static IEnumerable<FDOClass> AllFDOClasses
		{
			get
			{
				if (s_allFDOClasses == null)
					InitializeClasses();
				return s_allFDOClasses;
			}
		}

		public static bool IsCmObjectProperty(string propertyName)
		{
			if (s_cmObjectProperties == null)
				InitializeClasses();
			return s_cmObjectProperties.Contains(propertyName);
		}

		/// <summary>
		/// Save the display settings for all properties.
		/// </summary>
		public static void Save()
		{
			// Placeholder - properties are persisted in the form itself
		}

		/// <summary>
		/// Reset all display settings to defaults.
		/// </summary>
		public static void Reset()
		{
			InitializeClasses();
		}
	}
}
