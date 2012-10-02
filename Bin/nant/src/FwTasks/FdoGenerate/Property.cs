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
// File: Property.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
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
		/// Initializes a new instance of the <see cref="T:Property"/> class.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="parent">The parent class</param>
		/// ------------------------------------------------------------------------------------
		public Property(XmlElement node, Class parent)
			: base(node, parent)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid.
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
		/// Gets the signature.
		/// </summary>
		/// <value>The signature.</value>
		/// ------------------------------------------------------------------------------------
		public string Signature
		{
			get { return m_node.Attributes["sig"].Value; }
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
		/// Outputs the column spec.
		/// </summary>
		/// <param name="prefix"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual string OutputColumnSpec(string prefix)
		{
			TypeInfo typeInfo = TypeInfo.TypeInfos[Signature];
			StringBuilder bldr = new StringBuilder();

			if (typeInfo.RetrievalType != "special")
			{
				bldr.AppendLine(string.Format("// {0}: {1}", Name, Signature));
				bldr.Append(prefix);
				bldr.AppendLine(string.Format("cs.Push((int)DbColType.{0}, 1, {1}, 0);",
					typeInfo.ColumnType, FlidLine));

				if (Signature == "String")
				{
					bldr.Append(prefix);
					bldr.AppendLine(string.Format("cs.Push((int)DbColType.koctFmt, 1, {0}, 0);",
						FlidLine));
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
		public virtual string ProcessOwningAtomicFlid(string prefix)
		{
			return string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the vector of flids.
		/// </summary>
		/// <param name="prefix">The prefix.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual string ProcessVectorFlids(string prefix)
		{
			return string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Generates a list of  sequel view names needed for caching vector properties
		/// </summary>
		/// <param name="prefix">The prefix.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual string ProcessVectorViewNames(string prefix)
		{
			return string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Generates a list of isSequence bools needed for caching vector properties
		/// </summary>
		/// <param name="prefix">The prefix.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual string ProcessVectorIsSequence(string prefix)
		{
			return string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the property name with prop type appended.
		/// </summary>
		/// <value>The name of the niuginian prop.</value>
		/// ------------------------------------------------------------------------------------
		public virtual string NiuginianPropName
		{
			get { return Name; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the view.
		/// </summary>
		/// <value>The name of the view.</value>
		/// ------------------------------------------------------------------------------------
		public string ViewName
		{
			get { return string.Format("\"{0}_{1}\"", Parent.Name, Name); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid line in the way it's used in generated code.
		/// </summary>
		/// <value>The flid line.</value>
		/// ------------------------------------------------------------------------------------
		public string FlidLine
		{
			get { return string.Format("(int){0}Tags.kflid{1}", Parent.Name, Name); }
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
				if (OverridenType != string.Empty)
					return OverridenType;
				if (TypeInfo != null)
					return TypeInfo.CSharpType;
				// might also be a Cellar class, so just return Signature
				return Signature;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the get set method.
		/// </summary>
		/// <value>The get set method.</value>
		/// ------------------------------------------------------------------------------------
		public string GetSetMethod
		{
			get
			{
				if (TypeInfo != null)
					return TypeInfo.GetSetMethod;
				return string.Empty;
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
				VelocityContext context = (VelocityContext)
					RuntimeSingleton.GetApplicationAttribute("FdoGenerate.Context");
				FdoGenerate fdoGenerate = (FdoGenerate)context.Get("fdogenerate");

				string className = Parent.Name;
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
				VelocityContext context = (VelocityContext)
					RuntimeSingleton.GetApplicationAttribute("FdoGenerate.Context");
				FdoGenerate fdoGenerate = (FdoGenerate)context.Get("fdogenerate");

				string className = Parent.Name;
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
	}
}
