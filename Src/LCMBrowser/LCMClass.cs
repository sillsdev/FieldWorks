// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;

namespace LCMBrowser
{
	/// <summary />
	public class LCMClass
	{
		private Type m_classType;
		private string m_className;

		/// <summary />
		public LCMClass()
		{
		}

		/// <summary />
		public LCMClass(string className) : this()
		{
			ClassName = className;
			InitPropsForType(m_classType);
		}

		/// <summary />
		public LCMClass(Type type) : this(type.Name)
		{
			InitPropsForType(type);
		}

		/// <summary>
		/// Clones this instance.
		/// </summary>
		public LCMClass Clone()
		{
			// Make copies of all the class' properties.
			var props = new List<LCMClassProperty>();
			foreach (var clsProp in Properties)
			{
				props.Add(new LCMClassProperty { Name = clsProp.Name, Displayed = clsProp.Displayed });
			}

			var cls = new LCMClass(m_classType)
			{
				Properties = props
			};
			return cls;
		}

		/// <summary />
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

		/// <summary />
		[XmlElement("property")]
		public List<LCMClassProperty> Properties { get; set; } = new List<LCMClassProperty>();

		/// <summary>
		/// Gets the type of the class.
		/// </summary>
		public Type ClassType
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
		public void InitPropsForType(Type type)
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
		public bool IsPropertyDisplayed(string propName)
		{
			foreach (var prop in Properties)
			{
				if (prop.Name == propName)
				{
					return prop.Displayed;
				}
			}

			return false;
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