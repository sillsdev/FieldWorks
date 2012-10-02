// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BackReferenceProperty.cs
// Responsibility: Randy Regnier
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Text;
using System.Xml;

namespace SIL.FieldWorks.FDO.FdoGenerate
{
	/// <summary>
	/// Class for back reference properties.
	/// </summary>
	public class BackReferenceProperty : Property
	{
		private readonly int m_forwardRefNumber;
		private readonly int m_backRefNumber;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="BackReferenceProperty"/> class.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="parent">The parent class</param>
		/// ------------------------------------------------------------------------------------
		public BackReferenceProperty(XmlElement node, Class parent)
			: base(node, parent)
		{
			// Module#; Parent.Number * 1000 + Convert.ToInt32(m_node.Attributes["num"].Value);
			// Class#: Parent.Number * 1000 + Convert.ToInt32(m_node.Attributes["num"].Value);
			// m_forwardRefNumber = Module# + Class# + Prop#; == flid;
			XmlNode classNode = m_node.ParentNode.ParentNode;
			XmlNode moduleNode = classNode.ParentNode;
			int moduleId = int.Parse(moduleNode.Attributes["num"].Value) * 1000;
			int clsid = moduleId + int.Parse(classNode.Attributes["num"].Value);
			m_forwardRefNumber = (clsid * 1000) + Convert.ToInt32(m_node.Attributes["num"].Value);
			m_backRefNumber = m_forwardRefNumber + Class.kTenMillion;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the signature form the class node.
		/// </summary>
		/// <value>The signature.</value>
		/// ------------------------------------------------------------------------------------
		public override string Signature
		{
			get { return m_node.ParentNode.ParentNode.Attributes["id"].Value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this property is owning.
		/// </summary>
		/// <value><c>false</c></value>
		/// ------------------------------------------------------------------------------------
		public bool IsOwning
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the property is a back reference property.
		/// </summary>
		/// <value><c>true</c> if is a back referecne property; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public override bool IsBackReferenceProperty
		{
			get
			{
				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cardinality.
		/// </summary>
		/// <value>The cardinality, which is always <c>Card.Collection</c>.</value>
		/// ------------------------------------------------------------------------------------
		public override Card Cardinality
		{
			get { return Card.Collection; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid.
		/// </summary>
		/// <value>The number.</value>
		/// ------------------------------------------------------------------------------------
		public override int Number
		{
			get { return m_backRefNumber; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Back Reference number.
		/// (In this case it is the forward reference number.)
		/// </summary>
		/// <value>0 for non-reference properties.</value>
		/// ------------------------------------------------------------------------------------
		public override int BackRefNumber
		{
			get { return m_forwardRefNumber; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the property name with prop type appended.
		/// </summary>
		/// <value>The name of the niuginian prop.</value>
		/// ------------------------------------------------------------------------------------
		public override string NiuginianPropName
		{
			get
			{
				var bldr = new StringBuilder();
				bldr.Append("RefsFrom_");
				bldr.Append(m_node.ParentNode.ParentNode.Attributes["id"].Value);
				bldr.Append("_");
				bldr.Append(m_node.Attributes["id"].Value);
				return bldr.ToString();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the C# type of this property.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public override string CSharpType
		{
			get { return "FdoBackReferenceCollection"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type info.
		/// </summary>
		/// <value>The type info.</value>
		/// ------------------------------------------------------------------------------------
		protected override TypeInfo TypeInfo
		{
			get { return TypeInfo.TypeInfos["vector"]; }
		}
	}
}
