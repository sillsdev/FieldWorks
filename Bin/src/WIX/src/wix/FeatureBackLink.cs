//-------------------------------------------------------------------------------------------------
// <copyright file="FeatureBacklink.cs" company="Microsoft">
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
// Object to connect advertised resources in components to features.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Xml;

	/// <summary>
	/// Type of the feature backlink.
	/// </summary>
	public enum FeatureBacklinkType
	{
		/// <summary>Unknown feature back link type, default and invalid.</summary>
		Unknown,
		/// <summary>Class feature back link type.</summary>
		Class,
		/// <summary>Extension feature back link type.</summary>
		Extension,
		/// <summary>Shortcut feature back link type.</summary>
		Shortcut,
		/// <summary>PublishComponent feature back link type.</summary>
		PublishComponent,
		/// <summary>Typelib feature back link type.</summary>
		TypeLib,
		/// <summary>Assembly feature back link type.</summary>
		Assembly,
	}

	/// <summary>
	/// Internal object to connect advertised resources in components to features.
	/// </summary>
	public sealed class FeatureBacklink
	{
		private string componentId;
		private FeatureBacklinkType type;
		private string targetSymbol;

		/// <summary>
		/// Creates a feature back link to the specified
		/// </summary>
		/// <param name="componentId">Identifier of component to refer to </param>
		/// <param name="type">Type of feature backlink.</param>
		/// <param name="target">Symbol that refers to the feature.</param>
		public FeatureBacklink(string componentId, FeatureBacklinkType type, Symbol target) :
			this(componentId, type, target.RowId)
		{
		}

		/// <summary>
		/// Creates a feature back link to the specified
		/// </summary>
		/// <param name="componentId">Identifier of component to refer to </param>
		/// <param name="type">Type of feature backlink.</param>
		/// <param name="targetSymbol">Symbol of target that refers to the feature.</param>
		public FeatureBacklink(string componentId, FeatureBacklinkType type, string targetSymbol)
		{
			this.componentId = componentId;
			this.type = type;
			this.targetSymbol = targetSymbol;
		}

		/// <summary>
		/// Gets the identifier of the component.
		/// </summary>
		/// <value>Identifier of the component's feature backlink.</value>
		public string Component
		{
			get { return this.componentId; }
		}

		/// <summary>
		/// Gets the type of this backlink.
		/// </summary>
		/// <value>Type of this backlink.</value>
		public FeatureBacklinkType Type
		{
			get { return this.type; }
		}

		/// <summary>
		/// Gets the target symbol for this backlink.
		/// </summary>
		/// <value>Target symbol for this backlink.</value>
		public string Target
		{
			get { return this.targetSymbol; }
		}

		/// <summary>
		/// Gets the reference for this FeatureBacklink.
		/// </summary>
		/// <value>Reference for the feature backlink.</value>
		public Reference Reference
		{
			get { return new Reference(this.type.ToString(), this.targetSymbol); }
		}

		/// <summary>
		/// Saves the complex reference to a xml stream.
		/// </summary>
		/// <param name="writer">Xml stream to save complex reference.</param>
		public void Persist(XmlWriter writer)
		{
			if (null == writer)
			{
				throw new ArgumentNullException("writer");
			}

			writer.WriteStartElement("featureBacklink");
			writer.WriteAttributeString("type", this.type.ToString());
			writer.WriteAttributeString("targetSymbol", this.targetSymbol);
			writer.WriteAttributeString("component", this.componentId);
			writer.WriteEndElement();
		}
	}
}
