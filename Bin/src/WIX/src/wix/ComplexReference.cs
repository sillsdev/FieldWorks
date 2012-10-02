//-------------------------------------------------------------------------------------------------
// <copyright file="ComplexReference.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Complex reference objects.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Xml;

	/// <summary>
	/// Types of parents in complex reference.
	/// </summary>
	public enum ComplexReferenceParentType
	{
		/// <summary>Unknown complex reference type, default and invalid.</summary>
		Unknown,
		/// <summary>Feature parent of complex reference.</summary>
		Feature,
		/// <summary>ComponentGroup parent of a complex reference.</summary>
		ComponentGroup,
		/// <summary>Module parent of complex reference.</summary>
		Module
	}

	/// <summary>
	/// Types of children in complex refernece.
	/// </summary>
	public enum ComplexReferenceChildType
	{
		/// <summary>Unknown complex reference type, default and invalid.</summary>
		Unknown,
		/// <summary>Component child of complex reference.</summary>
		Component,
		/// <summary>ComponentGroup child of complex reference.</summary>
		ComponentGroup,
		/// <summary>Feature child of complex reference.</summary>
		Feature,
		/// <summary>Fragment child of complex reference.</summary>
		Fragment,
		/// <summary>Module child of complex reference.</summary>
		Module
	}

	/// <summary>
	/// Complex reference objects.
	/// </summary>
	internal class ComplexReference
	{
		private ComplexReferenceParentType parentType;
		private string parentId;
		private string parentLanguage;
		private ComplexReferenceChildType childType;
		private string childId;
		private bool primary;

		private Section section;

		/// <summary>
		/// Creates a new complex reference.
		/// </summary>
		/// <param name="parentType">Parent type of complex reference.</param>
		/// <param name="parentId">Identifier for parent of complex reference.</param>
		/// <param name="parentLanguage">Language for parent of complex reference (only valid when parent is Module).</param>
		/// <param name="childType">Child type of complex reference.</param>
		/// <param name="childId">Identifier for child of complex reference.</param>
		/// <param name="primary">Flag if complex reference is the primary for advertised goop.</param>
		public ComplexReference(ComplexReferenceParentType parentType, string parentId, string parentLanguage, ComplexReferenceChildType childType, string childId, bool primary)
		{
			if (ComplexReferenceParentType.Module != this.parentType && null != this.parentLanguage)
			{
				throw new ArgumentException("ParentLanguage cannot be specified unless the parent is a Module.");
			}

			this.parentType = parentType;
			this.parentId = parentId;
			this.parentLanguage = parentLanguage;
			this.childType = childType;
			this.childId = childId;
			this.primary = primary;

			this.section = null;
		}

		/// <summary>
		/// Gets the parent type of the complex reference.
		/// </summary>
		/// <value>Parent type of the complex reference.</value>
		public ComplexReferenceParentType ParentType
		{
			get { return this.parentType; }
		}

		/// <summary>
		/// Gets the parent identifier of the complex reference.
		/// </summary>
		/// <value>Parent identifier of the complex reference.</value>
		public string ParentId
		{
			get { return this.parentId; }
		}

		/// <summary>
		/// Gets the parent language of the complex reference.
		/// </summary>
		/// <value>Parent language of the complex reference.</value>
		public string ParentLanguage
		{
			get { return this.parentLanguage; }
		}

		/// <summary>
		/// Gets the child type of the complex reference.
		/// </summary>
		/// <value>Child type of the complex reference.</value>
		public ComplexReferenceChildType ChildType
		{
			get { return this.childType; }
		}

		/// <summary>
		/// Gets the child identifier of the complex reference.
		/// </summary>
		/// <value>Child identifier of the complex reference.</value>
		public string ChildId
		{
			get { return this.childId; }
		}

		/// <summary>
		/// Gets if this is the primary complex reference.
		/// </summary>
		/// <value>true if primary complex reference.</value>
		public bool IsPrimary
		{
			get { return this.primary; }
		}

		/// <summary>
		/// Gets and sets the section the primary reference belongs to.
		/// </summary>
		/// <value>Section for the complex reference.</value>
		public Section Section
		{
			get { return this.section; }
			set { this.section = value; }
		}

		/// <summary>
		/// Saves the complex reference to a xml stream.
		/// </summary>
		/// <param name="writer">Xml stream to save complex reference.</param>
		public void Persist(XmlWriter writer)
		{
			writer.WriteStartElement("complexReference");
			switch (this.parentType)
			{
				case ComplexReferenceParentType.ComponentGroup:
					writer.WriteAttributeString("parentType", "componentGroup");
					break;
				case ComplexReferenceParentType.Feature:
					writer.WriteAttributeString("parentType", "feature");
					break;
				case ComplexReferenceParentType.Module:
					writer.WriteAttributeString("parentType", "module");
					break;
				case ComplexReferenceParentType.Unknown:
					writer.WriteAttributeString("parentType", "unknown");
					break;
			}
			writer.WriteAttributeString("parent", this.parentId);
			if (null != this.parentLanguage)
			{
				writer.WriteAttributeString("parentLanguage", this.parentLanguage);
			}
			switch (this.childType)
			{
				case ComplexReferenceChildType.Component:
					writer.WriteAttributeString("childType", "component");
					break;
				case ComplexReferenceChildType.ComponentGroup:
					writer.WriteAttributeString("childType", "componentGroup");
					break;
				case ComplexReferenceChildType.Feature:
					writer.WriteAttributeString("childType", "feature");
					break;
				case ComplexReferenceChildType.Fragment:
					writer.WriteAttributeString("childType", "fragment");
					break;
				case ComplexReferenceChildType.Module:
					writer.WriteAttributeString("childType", "module");
					break;
				case ComplexReferenceChildType.Unknown:
					writer.WriteAttributeString("childType", "unknown");
					break;
			}
			writer.WriteAttributeString("child", this.childId);
			if (this.primary)
			{
				writer.WriteAttributeString("primary", "yes");
			}
			writer.WriteEndElement();
		}
	}
}
