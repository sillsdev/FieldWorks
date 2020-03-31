// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace LCMBrowser
{
	/// <summary />
	internal sealed class LCMClass
	{
		private Type m_classType;
		private string m_className;

		/// <summary />
		internal LCMClass(string className)
		{
			ClassName = className;
			InitPropsForType(m_classType);
		}

		/// <summary />
		internal LCMClass(Type type) : this(type.Name)
		{
			InitPropsForType(type);
		}

		/// <summary />
		[XmlAttribute]
		internal string ClassName
		{
			get => m_className;
			set
			{
				m_className = value;
				m_classType = LCMClassList.GetLCMClassType(value);
			}
		}

		/// <summary />
		[XmlElement("property")]
		internal List<LCMClassProperty> Properties { get; set; } = new List<LCMClassProperty>();

		/// <summary>
		/// Gets the type of the class.
		/// </summary>
		internal Type ClassType
		{
			get
			{
				if (m_classType == null && m_className != null)
				{
					m_classType = LCMClassList.GetLCMClassType(m_className);
				}

				return m_classType;
			}
		}

		/// <summary>
		/// Initializes the Properties list for the specified type.
		/// </summary>
		private void InitPropsForType(Type type)
		{
			Properties.Clear();
			const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
			var props = type.GetProperties(flags);

			foreach (var pi in props)
			{
				Properties.Add(new LCMClassProperty { Name = pi.Name, Displayed = true });
			}

			Properties.Sort((p1, p2) => p1.Name.CompareTo(p2.Name));
		}

		/// <summary>
		/// Determines whether or not the specified property is displayed.
		/// </summary>
		internal bool IsPropertyDisplayed(string propName)
		{
			return Properties.Where(prop => prop.Name == propName).Select(prop => prop.Displayed).FirstOrDefault();
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		public override string ToString()
		{
			return ClassName;
		}
	}
}