// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SettingsXmlAccessorBase.cs
// Responsibility: FieldWorks Team

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls; // for ProgressDialogWithTask
using SIL.Utils;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Common.Framework
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for FieldWorks classes that process XML "resource" files to load factory
	/// settings into databases.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class SettingsXmlAccessorBase : ExternalSettingsAccessorBase<XmlNode>
	{
		#region Abstract properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the required DTD version.
		/// If the external resource is not an XML file, this can return null (no such
		/// implementations exist for now).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract string DtdRequiredVersion { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the root element in the XmlDocument that contains the styles.
		/// (Currently it may actually be an arbitrary XPath that selectes the root
		/// element that has the DTD attribute and contains the markup element.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract string RootNodeName{ get; }

		#endregion

		#region Protected properties
		#endregion

		#region Abstract and Virtual methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a GUID based on the version attribute node.
		/// </summary>
		/// <param name="baseNode">The base node (by default, this is the node directly
		/// containing the version node, but subclasses can interpret this differently).</param>
		/// <returns>A GUID based on the version attribute node</returns>
		/// ------------------------------------------------------------------------------------
		protected override Guid GetVersion(XmlNode baseNode)
		{
			return new Guid(baseNode.Attributes.GetNamedItem("version").Value);
		}
		#endregion

		#region Static methods
		#endregion

		#region Protected methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the settings file and checks the DTD version.
		/// </summary>
		/// <returns>The root node</returns>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: XmlReaderSettings.DtdProcessing is missing from Mono")]
		protected override XmlNode LoadDoc()
		{
			string sXmlFilePath = DirectoryFinder.FWCodeDirectory + ResourceFilePathFromFwInstall;
			try
			{
				XmlReaderSettings settings = new XmlReaderSettings();
#if !__MonoCS__
				settings.DtdProcessing = DtdProcessing.Parse;
#else
				settings.ProhibitDtd = false;
#endif
				using (XmlReader reader = XmlReader.Create(sXmlFilePath, settings))
				{
					XmlDocument doc = new XmlDocument();
					doc.Load(reader);

					XmlNode root = doc.SelectSingleNode(RootNodeName);
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
				ReportInvalidInstallation(String.Format(FrameworkStrings.ksCannotLoadFile,
					sXmlFilePath, e.Message), e);
			}
			return null; // Can't actually get here. If you're name is Tim, tell it to the compiler.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the DTD version.
		/// </summary>
		/// <param name="rootNode">The root node (which holds the DTD version attribute).</param>
		/// <param name="xmlSettingsFileName">Name of the XML settings file.</param>
		/// ------------------------------------------------------------------------------------
		protected void CheckDtdVersion(XmlNode rootNode, string xmlSettingsFileName)
		{
			XmlNode DtdVersion = rootNode.Attributes.GetNamedItem("DTDver");
			if (DtdVersion == null || DtdVersion.Value != DtdRequiredVersion)
			{
				throw new Exception(String.Format(FrameworkStrings.kstidIncompatibleDTDVersion,
					xmlSettingsFileName, DtdRequiredVersion));
			}
		}
		#endregion
	}
}
