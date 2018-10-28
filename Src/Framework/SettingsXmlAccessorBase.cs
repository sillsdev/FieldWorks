// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml;
using System.Xml.Schema;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Base class for FieldWorks classes that process XML "resource" files to load factory
	/// settings into databases.
	/// </summary>
	public abstract class SettingsXmlAccessorBase : ExternalSettingsAccessorBase<XmlNode>
	{
		#region Abstract properties

		/// <summary>
		/// Gets the required DTD version.
		/// If the external resource is not an XML file, this can return null (no such
		/// implementations exist for now).
		/// </summary>
		protected abstract string DtdRequiredVersion { get; }

		/// <summary>
		/// The name of the root element in the XmlDocument that contains the styles.
		/// (Currently it may actually be an arbitrary XPath that selects the root
		/// element that has the DTD attribute and contains the markup element.)
		/// </summary>
		protected abstract string RootNodeName { get; }

		#endregion

		#region Abstract and Virtual methods

		/// <inheritdoc />
		protected override Guid GetVersion(XmlNode baseNode)
		{
			return new Guid(baseNode.Attributes.GetNamedItem("version").Value);
		}
		#endregion

		#region Protected methods

		/// <inheritdoc />
		protected override XmlNode LoadDoc(string xmlLocation = null)
		{
			var sXmlFilePath = xmlLocation ?? FwDirectoryFinder.CodeDirectory + ResourceFilePathFromFwInstall;
			try
			{
				var settings = new XmlReaderSettings
				{
					DtdProcessing = DtdProcessing.Parse
				};
				using (var reader = XmlReader.Create(sXmlFilePath, settings))
				{
					var doc = new XmlDocument();
					doc.Load(reader);

					var root = doc.SelectSingleNode(RootNodeName);
					CheckDtdVersion(root, ResourceFileName);
					return root;
				}
			}
			catch (XmlSchemaException e)
			{
				ReportInvalidInstallation(e.Message, e);
			}
			catch (Exception e)
			{
				ReportInvalidInstallation(string.Format(FrameworkStrings.ksCannotLoadFile, sXmlFilePath, e.Message), e);
			}
			return null; // Can't actually get here. If you're name is Tim, tell it to the compiler.
		}

		/// <summary>
		/// Checks the DTD version.
		/// </summary>
		/// <param name="rootNode">The root node (which holds the DTD version attribute).</param>
		/// <param name="xmlSettingsFileName">Name of the XML settings file.</param>
		protected void CheckDtdVersion(XmlNode rootNode, string xmlSettingsFileName)
		{
			var dtdVersion = rootNode.Attributes.GetNamedItem("DTDver");
			if (dtdVersion == null || dtdVersion.Value != DtdRequiredVersion)
			{
				throw new Exception(string.Format(FrameworkStrings.kstidIncompatibleDTDVersion, xmlSettingsFileName, DtdRequiredVersion));
			}
		}
		#endregion
	}
}