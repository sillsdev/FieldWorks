// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Class.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using NVelocity;
using NVelocity.Runtime;

namespace SIL.FieldWorks.FDO.FdoGenerate
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a Class description in the XMI file
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class Class: Base<CellarModule>, SIL.FieldWorks.FDO.FdoGenerate.IClass
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Class"/> class.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="parent">The model</param>
		/// ------------------------------------------------------------------------------------
		public Class(XmlElement node, CellarModule parent)
			: base(node, parent)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of the class.
		/// </summary>
		/// <value>The number.</value>
		/// ------------------------------------------------------------------------------------
		public int Number
		{
			get
			{
				return Parent.Number * 1000 + Convert.ToInt32(m_node.Attributes["num"].Value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is abstract.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is abstract; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool IsAbstract
		{
			get { return Convert.ToBoolean(m_node.Attributes["abstract"].Value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the abbreviation.
		/// </summary>
		/// <value>The abbreviation.</value>
		/// ------------------------------------------------------------------------------------
		public string Abbreviation
		{
			get { return m_node.Attributes["abbr"].Value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the base class.
		/// </summary>
		/// <value>The name of the base class.</value>
		/// ------------------------------------------------------------------------------------
		private string InternalBaseClassName
		{
			get
			{
				XmlAttribute attribute = m_node.Attributes["base"];
				if (attribute == null)
					return string.Empty;
				return attribute.Value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the base class (and namespace if necessary).
		/// </summary>
		/// <value>The name of the base class.</value>
		/// ------------------------------------------------------------------------------------
		public string BaseClassName
		{
			get
			{
				Class baseClass = BaseClass;
				if (baseClass == null)
					return string.Empty;

				// Check to see if base class is in same module
				if (baseClass.Parent.Name == Parent.Name)
					return baseClass.Name;

				return baseClass.GetRelativeQualifiedSignature(Parent);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the base class.
		/// </summary>
		/// <value>The base class.</value>
		/// ------------------------------------------------------------------------------------
		public Class BaseClass
		{
			get
			{
				string baseClassName = InternalBaseClassName;
				if (baseClassName == string.Empty)
					return null;

				foreach (CellarModule module in Parent.Parent.Modules)
				{
					if (module.Classes.Contains(baseClassName))
						return module.Classes[baseClassName];
				}
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the depth.
		/// </summary>
		/// <value>The depth.</value>
		/// ------------------------------------------------------------------------------------
		public int Depth
		{
			get
			{
				XmlAttribute attribute = m_node.Attributes["depth"];
				if (attribute == null)
					return 0;
				return Convert.ToInt32(attribute.Value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the properties.
		/// </summary>
		/// <value>The properties.</value>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> Properties
		{
			get
			{
				StringKeyCollection<Property> properties = new StringKeyCollection<Property>();
				foreach (XmlElement elem in m_node.FirstChild.ChildNodes)
				{
					if (elem.Name == "basic")
						properties.Add(new Property(elem, this));
					else
						properties.Add(new RelationalProperty(elem, this));
				}

				return properties;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sub classes.
		/// </summary>
		/// <value>The sub classes.</value>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Class> SubClasses
		{
			get
			{
				StringKeyCollection<Class> subClasses = new StringKeyCollection<Class>();
				foreach (CellarModule module in Parent.Parent.Modules)
				{
					foreach (Class cls in module.Classes)
					{
						if (cls.InternalBaseClassName == Name)
							subClasses.Add(cls);
					}
				}
				return subClasses;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is ownerless.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is ownerless; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public string IsOwnerless
		{
			get
			{
				string query = string.Format("count(//class/props/owning[@sig = '{0}'])", Name);
				int count = Convert.ToInt32(m_node.CreateNavigator().Evaluate(query));
				if (count == 0)
					return "true";
				return "false";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the relative qualified signature.
		/// </summary>
		/// <param name="desiredModule">The desired module.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string GetRelativeQualifiedSignature(CellarModule desiredModule)
		{
			return GetRelativeQualifiedSignature(desiredModule, string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the relative qualified signature.
		/// </summary>
		/// <param name="desiredModule">The desired module.</param>
		/// <param name="classNamePrefix">Prefix for class (if any usually 'Base')</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string GetRelativeQualifiedSignature(CellarModule desiredModule,
			string classNamePrefix)
		{
			return GetQualifiedSignature(desiredModule, classNamePrefix, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the relative qualified signature.
		/// </summary>
		/// <param name="desiredModule">The desired module.</param>
		/// <param name="classNamePrefix">Prefix for class (if any usually 'Base')</param>
		/// <param name="fRelative"><c>true</c> to return signature relative to current
		/// class.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string GetQualifiedSignature(CellarModule desiredModule, string classNamePrefix,
			bool fRelative)
		{
			StringBuilder bldr = new StringBuilder();
			if (desiredModule == null || Parent.Name != desiredModule.Name)
			{
				if (!fRelative || Parent.Name != desiredModule.Name)
					bldr.Append("SIL.FieldWorks.FDO.");
				if (Name != "CmObject")
				{
					bldr.Append(Parent.Name);
					bldr.Append(".");
				}
			}
			if (Name != "CmObject")
				bldr.Append(classNamePrefix);

			bldr.Append(Name);
			return bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the qualified signature.
		/// </summary>
		/// <param name="classNamePrefix">The class name prefix.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string GetQualifiedSignature(string classNamePrefix)
		{
			return GetQualifiedSignature(null, classNamePrefix, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the subclass cases.
		/// </summary>
		/// <param name="desiredModule">The desired module.</param>
		/// <param name="prefix">A prefix string to put in front of each line.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string SubclassCases(CellarModule desiredModule, string prefix)
		{
			StringBuilder bldr = new StringBuilder();
			foreach (Class cls in SubClasses)
			{
				string sig = cls.GetRelativeQualifiedSignature(desiredModule);
				bldr.AppendLine(string.Format("{0}case {1}.kclsid{2}:", prefix, sig, cls.Name));
				bldr.AppendLine(string.Format("{0}\treturn new {1}(cache, hvo);", prefix, sig));
				string subSubs = cls.SubclassCases(desiredModule, prefix);
				if (subSubs.Length > 0)
					bldr.AppendLine(subSubs);
			}
			return bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the column specs.
		/// </summary>
		/// <param name="prefix">A prefix string to put in front of each line.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string ProcessColumnSpecs(string prefix)
		{
			// We have to sort the properties in order to match the order of the columns in
			// the corresponding ClassName_ view.
			SortedList<int, Property> sortedProps = new SortedList<int,Property>();
			foreach(Property prop in Properties)
				sortedProps.Add(prop.Number, prop);

			StringBuilder bldr = new StringBuilder();
			foreach (Property prop in sortedProps.Values)
			{
				string line = prop.OutputColumnSpec(prefix);
				if (line.Length > 0)
				{
					bldr.Append(prefix);
					bldr.Append(line);
				}
			}
			return bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the owning atomic flid.
		/// </summary>
		/// <param name="prefix">The prefix.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string ProcessOwningAtomicFlid(string prefix)
		{
			if (InternalBaseClassName == string.Empty)
				return string.Empty;

			StringBuilder bldr = new StringBuilder();
			bldr.Append(BaseClass.ProcessOwningAtomicFlid(prefix));

			foreach (Property prop in Properties)
			{
				string flid = prop.ProcessOwningAtomicFlid(prefix);
				if (flid.Length > 0)
				{
					if (bldr.Length > 0)
						bldr.AppendLine(",");
					bldr.Append(flid);
				}
			}

			return bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the vector of flids.
		/// </summary>
		/// <param name="prefix">The prefix.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string ProcessVectorFlids(string prefix)
		{
			if (InternalBaseClassName == string.Empty)
				return string.Empty;

			StringBuilder bldr = new StringBuilder();
			bldr.Append(BaseClass.ProcessVectorFlids(prefix));

			foreach (Property prop in Properties)
			{
				string flid = prop.ProcessVectorFlids(prefix);
				if (flid.Length > 0)
				{
					if (bldr.Length > 0)
						bldr.AppendLine(",");
					bldr.Append(flid);
				}
			}

			return bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Generates a list of  sequel view names needed for caching vector properties
		/// </summary>
		/// <param name="prefix">The prefix.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string ProcessVectorViewNames(string prefix)
		{
			if (InternalBaseClassName == string.Empty)
				return string.Empty;

			StringBuilder bldr = new StringBuilder();
			bldr.Append(BaseClass.ProcessVectorViewNames(prefix));

			foreach (Property prop in Properties)
			{
				string flid = prop.ProcessVectorViewNames(prefix);
				if (flid.Length > 0)
				{
					if (bldr.Length > 0)
						bldr.AppendLine(",");
					bldr.Append(flid);
				}
			}

			return bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Generates a list of isSequence bools needed for caching vector properties
		/// </summary>
		/// <param name="prefix">The prefix.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string ProcessVectorIsSequence(string prefix)
		{
			if (InternalBaseClassName == string.Empty)
				return string.Empty;

			StringBuilder bldr = new StringBuilder();
			bldr.Append(BaseClass.ProcessVectorIsSequence(prefix));

			foreach (Property prop in Properties)
			{
				string flid = prop.ProcessVectorIsSequence(prefix);
				if (flid.Length > 0)
				{
					if (bldr.Length > 0)
						bldr.AppendLine(",");
					bldr.Append(flid);
				}
			}

			return bldr.ToString();
		}
	}
}
