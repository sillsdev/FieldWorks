// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2006' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Property.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Xml;
using NVelocity;
using NVelocity.Runtime;

namespace SIL.FieldWorks.FDO.FdoGenerate
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class Property: Base<Class>
	{
		private static int s_id = 1;
		private readonly int m_id;

		/// <summary></summary>
		public enum Card
		{
			/// <summary></summary>
			Unknown,
			/// <summary></summary>
			Basic,
			/// <summary></summary>
			Atomic,
			/// <summary></summary>
			Sequence,
			/// <summary></summary>
			Collection,
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Property"/> class.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="parent">The parent class</param>
		/// ------------------------------------------------------------------------------------
		public Property(XmlElement node, Class parent)
			: base(node, parent)
		{
			m_id = s_id++;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the optional class comment.
		/// </summary>
		/// <value>The optional comment, or an empty string.</value>
		/// ------------------------------------------------------------------------------------
		public string Comment
		{
			get
			{
				return AsMSString("\t\t", m_node.SelectSingleNode("comment"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the optional notes.
		/// </summary>
		/// <value>The optional notes, or an empty string.</value>
		/// ------------------------------------------------------------------------------------
		public string Notes
		{
			get
			{
				return AsMSString("\t\t", m_node.SelectSingleNode("notes"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid.
		/// </summary>
		/// <value>The number.</value>
		/// ------------------------------------------------------------------------------------
		public virtual int Number
		{
			get
			{
				return Parent.Number * 1000 + Convert.ToInt32(m_node.Attributes["num"].Value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the signature.
		/// </summary>
		/// <value>The signature.</value>
		/// ------------------------------------------------------------------------------------
		public virtual string Signature
		{
			get
			{
				string sig = m_node.Attributes["sig"].Value;
				//if (sig == "MultiString" || sig == "MultiUnicode")
				//	sig += "Accessor";

				return sig;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cardinality.
		/// </summary>
		/// <value>The cardinality.</value>
		/// ------------------------------------------------------------------------------------
		public virtual Card Cardinality
		{
			get { return Card.Basic; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the property name with prop type appended.
		/// </summary>
		/// <value>The name of the niuginian prop.</value>
		/// ------------------------------------------------------------------------------------
		public virtual string NiuginianPropName
		{
			get
			{
				return Name;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Back Reference number.
		/// </summary>
		/// <value>0 for non-reference properties.</value>
		/// ------------------------------------------------------------------------------------
		public virtual int BackRefNumber
		{
			get
			{
				return 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type info.
		/// </summary>
		/// <value>The type info.</value>
		/// ------------------------------------------------------------------------------------
		protected virtual TypeInfo TypeInfo
		{
			get { return TypeInfo.TypeInfos[Signature]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the C# type of this property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string CSharpType
		{
			get
			{
				return OverridenType != string.Empty ?
					OverridenType
					: (TypeInfo != null ?
						TypeInfo.CSharpType
						: Signature);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the property is a back reference property.
		/// </summary>
		/// <value><c>true</c> if is a back referecne property; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsBackReferenceProperty
		{
			get
			{
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the class is hand generated.
		/// </summary>
		/// <value><c>true</c> if is hand generated; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool IsHandGenerated
		{
			get
			{
				var context = (VelocityContext)
					RuntimeSingleton.GetApplicationAttribute("FdoGenerate.Context");
				var fdoGenerate = (FdoGenerate)context.Get("fdogenerate");

				var className = Parent.Name;
				return fdoGenerate.Overrides.ContainsKey(className)
						&& fdoGenerate.Overrides[className].Contains(Name);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the overriden type for the property.
		/// </summary>
		/// <value>The overriden type string, or <c>String.Empty</c> if property type is not
		/// overriden.</value>
		/// ------------------------------------------------------------------------------------
		public string OverridenType
		{
			get
			{
				var context = (VelocityContext)
					RuntimeSingleton.GetApplicationAttribute("FdoGenerate.Context");
				var fdoGenerate = (FdoGenerate)context.Get("fdogenerate");

				var className = Parent.Name;
				if (fdoGenerate.IntPropTypeOverrides.ContainsKey(className))
				{
					if (fdoGenerate.IntPropTypeOverrides[className].ContainsKey(Name))
					{
						return fdoGenerate.IntPropTypeOverrides[className][Name];
					}
				}
				return string.Empty;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is big.
		/// </summary>
		/// <value><c>true</c> if this instance is big; otherwise, <c>false</c>.</value>
		public bool IsBig
		{
			get
			{
				var isBig = false;

				// Check optional 'big' attribute.
				var bigAttr = m_node.Attributes["big"];
				if (bigAttr != null)
					isBig = bool.Parse(bigAttr.Value);

				return isBig;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this property's setter is internal.
		/// </summary>
		public bool IsSetterInternal
		{
			get
			{
				bool fSetterInternal = false;

				// Check optional 'setterInternal' attribute.
				var setterInternalAttr = m_node.Attributes["internalSetter"];
				if (setterInternalAttr != null)
					fSetterInternal = bool.Parse(setterInternalAttr.Value);

				return fSetterInternal;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current
		/// <see cref="T:System.Object"></see>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current
		/// <see cref="T:System.Object"></see>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return m_id.ToString();
		}
	}
}
