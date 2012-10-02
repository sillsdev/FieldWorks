//-------------------------------------------------------------------------------------------------
// <copyright file="Inspector.cs" company="Microsoft">
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
// WiX source code inspector.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstaller.Tools
{
	using System;
	using System.Collections;
	using System.IO;
	using System.Xml;

	/// <summary>
	/// WiX source code inspector.
	/// </summary>
	public class Inspector
	{
		private const string WixNamespaceURI = "http://schemas.microsoft.com/wix/2003/01/wi";

		private static bool indentSpecialProcessingInstructions = false;

		private int errors;
		private Hashtable errorsAsWarnings;
		private int indentationAmount;
		private bool fixable;
		private string sourceFile;

		/// <summary>
		/// Instantiate a new Inspector class.
		/// </summary>
		/// <param name="errorsAsWarnings">Test errors to display as warnings.</param>
		/// <param name="indentationAmount">Indentation value to use when validating leading whitespace.</param>
		public Inspector(string[] errorsAsWarnings, int indentationAmount)
		{
			this.errors = 0;
			this.errorsAsWarnings = new Hashtable();
			this.indentationAmount = indentationAmount;
			this.fixable = true;
			this.sourceFile = null;

			if (null != errorsAsWarnings)
			{
				foreach (string error in errorsAsWarnings)
				{
					InspectorTestType itt = GetInspectorTestType(error);

					if (itt != InspectorTestType.Unknown)
					{
						this.errorsAsWarnings.Add(itt, null);
					}
					else // not a known InspectorTestType
					{
						this.OnError(InspectorTestType.InspectorTestTypeUnknown, null, "Unknown error type: '{0}'.", error);
					}
				}
			}
		}

		/// <summary>
		/// Inspector test types.  These are used to condition error messages down to warnings.
		/// </summary>
		private enum InspectorTestType
		{
			/// <summary>
			/// Internal-only: returned when a string cannot be converted to an InspectorTestType.
			/// </summary>
			Unknown,

			/// <summary>
			/// Internal-only: displayed when a string cannot be converted to an InspectorTestType.
			/// </summary>
			InspectorTestTypeUnknown,

			/// <summary>
			/// Displayed when an XML loading exception has occurred.
			/// </summary>
			XmlException,

			/// <summary>
			/// Displayed when a file cannot be accessed; typically when trying to save back a fixed file.
			/// </summary>
			UnauthorizedAccessException,

			/// <summary>
			/// Displayed when the encoding attribute in the XML declaration is not 'UTF-8'.
			/// </summary>
			DeclarationEncodingWrong,

			/// <summary>
			/// Displayed when the XML declaration is missing from the source file.
			/// </summary>
			DeclarationMissing,

			/// <summary>
			/// Displayed when the whitespace preceding a CDATA node is wrong.
			/// </summary>
			WhitespacePrecedingCDATAWrong,

			/// <summary>
			/// Dispalyed when the whitespace preceding a node is wrong.
			/// </summary>
			WhitespacePrecedingNodeWrong,

			/// <summary>
			/// Displayed when an element is not empty as it should be.
			/// </summary>
			NotEmptyElement,

			/// <summary>
			/// Displayed when the whitespace following a CDATA node is wrong.
			/// </summary>
			WhitespaceFollowingCDATAWrong,

			/// <summary>
			/// Displayed when the whitespace preceding an end element is wrong.
			/// </summary>
			WhitespacePrecedingEndElementWrong,

			/// <summary>
			/// Displayed when the xmlns attribute is missing from the document element.
			/// </summary>
			XmlnsMissing,

			/// <summary>
			/// Displayed when the xmlns attribute on the document element is wrong.
			/// </summary>
			XmlnsValueWrong,

			/// <summary>
			/// Displayed when a Category element has an empty AppData attribute.
			/// </summary>
			CategoryAppDataEmpty,

			/// <summary>
			/// Displayed when a Registry element encounters an error while being converted
			/// to a strongly-typed WiX COM element.
			/// </summary>
			COMRegistrationTyper,

			/// <summary>
			/// Displayed when a UpgradeVersion element has an empty RemoveFeatures attribute.
			/// </summary>
			UpgradeVersionRemoveFeaturesEmpty,

			/// <summary>
			/// Displayed when a Feature element contains the deprecated FollowParent attribute.
			/// </summary>
			FeatureFollowParentDeprecated,

			/// <summary>
			/// Displayed when a RadioButton element is missing the Value attribute.
			/// </summary>
			RadioButtonMissingValue,

			/// <summary>
			/// Displayed when a TypeLib element contains a Description element with an empty
			/// string value.
			/// </summary>
			TypeLibDescriptionEmpty,

			/// <summary>
			/// Displayed when a RelativePath attribute occurs on an unadvertised Class element.
			/// </summary>
			ClassRelativePathMustBeAdvertised,

			/// <summary>
			/// Displayed when a Class element has an empty Description attribute.
			/// </summary>
			ClassDescriptionEmpty,

			/// <summary>
			/// Displayed when a ServiceInstall element has an empty LocalGroup attribute.
			/// </summary>
			ServiceInstallLocalGroupEmpty,

			/// <summary>
			/// Displayed when a ServiceInstall element has an empty Password attribute.
			/// </summary>
			ServiceInstallPasswordEmpty,

			/// <summary>
			/// Displayed when a Shortcut element has an empty WorkingDirectory attribute.
			/// </summary>
			ShortcutWorkingDirectoryEmpty,

			/// <summary>
			/// Displayed when a IniFile element has an empty Value attribute.
			/// </summary>
			IniFileValueEmpty,

			/// <summary>
			/// Displayed when a FileSearch element has a Name attribute that contains both the short
			/// and long versions of the file name.
			/// </summary>
			FileSearchNamesCombined,

			/// <summary>
			/// Displays when a WebApplicationExtension element has a deprecated Id attribute.
			/// </summary>
			WebApplicationExtensionIdDeprecated,

			/// <summary>
			/// Displays when a WebApplicationExtension element has an empty Id attribute.
			/// </summary>
			WebApplicationExtensionIdEmpty,

			/// <summary>
			/// Displayed when a Property element has an empty Value attribute.
			/// </summary>
			PropertyValueEmpty,

			/// <summary>
			/// Displayed when a Control element has an empty CheckBoxValue attribute.
			/// </summary>
			ControlCheckBoxValueEmpty,

			/// <summary>
			/// Displayed when a deprecated RadioGroup element is found.
			/// </summary>
			RadioGroupDeprecated,

			/// <summary>
			/// Displayed when a Progress element has an empty TextTemplate attribute.
			/// </summary>
			ProgressTextTemplateEmpty,

			/// <summary>
			/// Displayed when a RegistrySearch element has a Type attribute set to 'registry'.
			/// </summary>
			RegistrySearchTypeRegistryDeprecated,

			/// <summary>
			/// Displayed when an element contains a deprecated src attribute.
			/// </summary>
			SrcIsDeprecated,

			/// <summary>
			/// Displayed when a Component element is missing the required Guid attribute.
			/// </summary>
			RequireComponentGuid,
		}

		/// <summary>
		/// Inspect a file.
		/// </summary>
		/// <param name="sourceFile">The file to inspect.</param>
		/// <param name="fixErrors">Option to fix errors that are found.</param>
		/// <returns>The number of errors found.</returns>
		public int InspectFile(string sourceFile, bool fixErrors)
		{
			XmlTextReader reader = null;
			XmlWriter writer = null;
			LineInfoDocument doc = null;

			try
			{
				// set the instance info
				this.errors = 0;
				this.fixable = true;
				this.sourceFile = sourceFile;

				// load the xml
				reader = new XmlTextReader(this.sourceFile);
				doc = new LineInfoDocument();
				doc.PreserveWhitespace = true;
				doc.Load(reader);
			}
			catch (XmlException xe)
			{
				this.OnError(InspectorTestType.XmlException, null, "The xml is invalid.  Detail: '{0}'", xe.Message);
				return this.errors;
			}
			finally
			{
				if (null != reader)
				{
					reader.Close();
				}
			}

			// inspect the document
			this.InspectDocument(doc);

			// fix errors if necessary
			if (fixErrors && 0 < this.errors)
			{
				if (this.fixable)
				{
					try
					{
						using (StreamWriter sw = File.CreateText(sourceFile))
						{
							writer = new XmlTextWriter(sw);
							doc.WriteTo(writer);
						}
					}
					catch (UnauthorizedAccessException)
					{
						this.OnError(InspectorTestType.UnauthorizedAccessException, null, "Could not write to file.");
					}
					finally
					{
						if (null != writer)
						{
							writer.Close();
						}
					}
				}
				else
				{
					this.OnVerbose(null, "Because this file contains \\r or \\n as part of a text or CDATA node, it cannot be automatically fixed.");
				}
			}

			return this.errors;
		}

		/// <summary>
		/// Get the strongly-typed InspectorTestType for a string representation of the same.
		/// </summary>
		/// <param name="inspectorTestType">The InspectorTestType represented by the string.</param>
		/// <returns>The InspectorTestType value if found; otherwise InspectorTestType.Unknown.</returns>
		private static InspectorTestType GetInspectorTestType(string inspectorTestType)
		{
			foreach (InspectorTestType itt in Enum.GetValues(typeof(InspectorTestType)))
			{
				if (itt.ToString() == inspectorTestType)
				{
					return itt;
				}
			}

			return InspectorTestType.Unknown;
		}

		/// <summary>
		/// Fix the whitespace in a Whitespace node.
		/// </summary>
		/// <param name="indentationAmount">Indentation value to use when validating leading whitespace.</param>
		/// <param name="level">The depth level of the desired whitespace.</param>
		/// <param name="whitespace">The whitespace node to fix.</param>
		private static void FixWhitespace(int indentationAmount, int level, XmlNode whitespace)
		{
			int newLineCount = 0;

			for (int i = 0; i + 1 < whitespace.Value.Length; i++)
			{
				if (Environment.NewLine == whitespace.Value.Substring(i, 2))
				{
					i++; // skip an extra character
					newLineCount++;
				}
			}

			if (0 == newLineCount)
			{
				newLineCount = 1;
			}

			// reset the whitespace value
			whitespace.Value = string.Empty;

			// add the correct number of newlines
			for (int i = 0; i < newLineCount; i++)
			{
				whitespace.Value = string.Concat(whitespace.Value, Environment.NewLine);
			}

			// add the correct number of spaces based on configured indentation amount
			whitespace.Value = string.Concat(whitespace.Value, new string(' ', level * indentationAmount));
		}

		/// <summary>
		/// Determine if the whitespace preceding a node is appropriate for its depth level.
		/// </summary>
		/// <param name="indentationAmount">Indentation value to use when validating leading whitespace.</param>
		/// <param name="level">The depth level that should match this whitespace.</param>
		/// <param name="whitespace">The whitespace to validate.</param>
		/// <returns>true if the whitespace is legal; false otherwise.</returns>
		private static bool IsLegalWhitespace(int indentationAmount, int level, string whitespace)
		{
			// strip off leading newlines; there can be an arbitrary number of these
			while (whitespace.StartsWith(Environment.NewLine))
			{
				whitespace = whitespace.Substring(Environment.NewLine.Length);
			}

			// check the length
			if (whitespace.Length != level * indentationAmount)
			{
				return false;
			}

			// check the spaces
			foreach (char character in whitespace)
			{
				if (' ' != character)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Inspect an XML document.
		/// </summary>
		/// <param name="doc">The XML document to inspect.</param>
		private void InspectDocument(XmlDocument doc)
		{
			// inspect the declaration
			if (XmlNodeType.XmlDeclaration == doc.FirstChild.NodeType)
			{
				XmlDeclaration declaration = (XmlDeclaration)doc.FirstChild;

				if ("UTF-8" != declaration.Encoding)
				{
					this.OnError(InspectorTestType.DeclarationEncodingWrong, declaration, "The XML declaration encoding is not properly set to 'UTF-8'.");
					declaration.Encoding = "UTF-8";
				}
			}
			else // missing declaration
			{
				this.OnError(InspectorTestType.DeclarationMissing, null, "This file is missing an XML declaration on the first line.");
				doc.PrependChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));
			}

			// start inspecting the nodes at the document element
			this.InspectNode(doc.DocumentElement, 0);
		}

		/// <summary>
		/// Inspect a single xml node.
		/// </summary>
		/// <param name="node">The node to inspect.</param>
		/// <param name="level">The depth level of the node.</param>
		private void InspectNode(XmlNode node, int level)
		{
			// detect characters that cannot be preserved with the DOM
			if (XmlNodeType.CDATA == node.NodeType || XmlNodeType.Text == node.NodeType)
			{
				if (0 <= node.Value.IndexOfAny("\r\n".ToCharArray()))
				{
					this.fixable = false;
				}
			}

			// inspect this node's whitespace
			if ((XmlNodeType.Comment == node.NodeType && 0 > node.Value.IndexOf(Environment.NewLine)) ||
				XmlNodeType.CDATA == node.NodeType || XmlNodeType.Element == node.NodeType || XmlNodeType.ProcessingInstruction == node.NodeType)
			{
				this.InspectWhitespace(node, level);
			}

			// inspect this node
			switch (node.NodeType)
			{
				case XmlNodeType.Element:
					switch (node.LocalName)
					{
						case "Binary":
							this.InspectBinaryElement((XmlElement)node);
							break;
						case "Category":
							this.InspectCategoryElement((XmlElement)node);
							break;
						case "Class":
							this.InspectClassElement((XmlElement)node);
							break;
						case "Component":
							this.InspectComponentElement((XmlElement)node);
							break;
						case "Control":
							this.InspectControlElement((XmlElement)node);
							break;
						case "DigitalCertificate":
							this.InspectDigitalCertificateElement((XmlElement)node);
							break;
						case "DigitalSignature":
							this.InspectDigitalSignatureElement((XmlElement)node);
							break;
						case "Directory":
							this.InspectDirectoryElement((XmlElement)node);
							break;
						case "DirectoryRef":
							this.InspectDirectoryRefElement((XmlElement)node);
							break;
						case "ExternalFile":
							this.InspectExternalFileElement((XmlElement)node);
							break;
						case "Feature":
							this.InspectFeatureElement((XmlElement)node);
							break;
						case "File":
							this.InspectFileElement((XmlElement)node);
							break;
						case "FileSearch":
							this.InspectFileSearchElement((XmlElement)node);
							break;
						case "Icon":
							this.InspectIconElement((XmlElement)node);
							break;
						case "IniFile":
							this.InspectIniFileElement((XmlElement)node);
							break;
						case "Media":
							this.InspectMediaElement((XmlElement)node);
							break;
						case "Merge":
							this.InspectMergeElement((XmlElement)node);
							break;
						case "ProgressText":
							this.InspectProgressTextElement((XmlElement)node);
							break;
						case "Property":
							this.InspectPropertyElement((XmlElement)node);
							break;
						case "RadioButton":
							this.InspectRadioButtonElement((XmlElement)node);
							break;
						case "RadioGroup":
							this.InspectRadioGroupElement((XmlElement)node);
							break;
						case "RegistrySearch":
							this.InspectRegistrySearchElement((XmlElement)node);
							break;
						case "ServiceInstall":
							this.InspectServiceInstallElement((XmlElement)node);
							break;
						case "SFPCatalog":
							this.InspectSFPCatalogElement((XmlElement)node);
							break;
						case "Shortcut":
							this.InspectShortcutElement((XmlElement)node);
							break;
						case "TargetImage":
							this.InspectTargetImageElement((XmlElement)node);
							break;
						case "Text":
							this.InspectTextElement((XmlElement)node);
							break;
						case "TypeLib":
							this.InspectTypeLibElement((XmlElement)node);
							break;
						case "UpgradeImage":
							this.InspectUpgradeImageElement((XmlElement)node);
							break;
						case "UpgradeVersion":
							this.InspectUpgradeVersionElement((XmlElement)node);
							break;
						case "WebApplicationExtension":
							this.InspectWebApplicationExtensionElement((XmlElement)node);
							break;
						case "Wix":
							this.InspectWixElement((XmlElement)node);
							break;
					}
					break;
			}

			// inspect all children of this node
			for (int i = 0; i < node.ChildNodes.Count; i++)
			{
				XmlNode child = node.ChildNodes[i];

				if (indentSpecialProcessingInstructions)
				{
					switch (child.LocalName)
					{
						case "else":
						case "endif":
						case "endforeach":
							level--;
							break;
					}
				}

				this.InspectNode(child, level + 1);

				if (indentSpecialProcessingInstructions)
				{
					switch (child.LocalName)
					{
						case "if":
						case "else":
						case "foreach":
							level++;
							break;
					}
				}
			}
		}

		/// <summary>
		/// Inspect the whitespace adjacent to a node.
		/// </summary>
		/// <param name="node">The node to inspect.</param>
		/// <param name="level">The depth level of the node.</param>
		private void InspectWhitespace(XmlNode node, int level)
		{
			// fix the whitespace before this node
			XmlNode whitespace = node.PreviousSibling;
			if (null != whitespace && XmlNodeType.Whitespace == whitespace.NodeType)
			{
				if (XmlNodeType.CDATA == node.NodeType)
				{
					this.OnError(InspectorTestType.WhitespacePrecedingCDATAWrong, node, "There should be no whitespace preceding a CDATA node.");
					whitespace.ParentNode.RemoveChild(whitespace);
				}
				else
				{
					if (!IsLegalWhitespace(this.indentationAmount, level, whitespace.Value))
					{
						this.OnError(InspectorTestType.WhitespacePrecedingNodeWrong, node, "The whitespace preceding this node is incorrect.");
						FixWhitespace(this.indentationAmount, level, whitespace);
					}
				}
			}

			// fix the whitespace inside this node (except for Error which may contain just whitespace)
			if (XmlNodeType.Element == node.NodeType && "Error" != node.LocalName)
			{
				XmlElement element = (XmlElement)node;

				if (!element.IsEmpty && string.Empty == element.InnerXml.Trim())
				{
					this.OnError(InspectorTestType.NotEmptyElement, element, "This should be an empty element since it contains nothing but whitespace.");
					element.IsEmpty = true;
				}
			}

			// fix the whitespace before the end element or after for CDATA nodes
			if (XmlNodeType.CDATA == node.NodeType)
			{
				whitespace = node.NextSibling;
				if (null != whitespace && XmlNodeType.Whitespace == whitespace.NodeType)
				{
					this.OnError(InspectorTestType.WhitespaceFollowingCDATAWrong, node, "There should be no whitespace following a CDATA node.");
					whitespace.ParentNode.RemoveChild(whitespace);
				}
			}
			else if (XmlNodeType.Element == node.NodeType)
			{
				whitespace = node.LastChild;

				// Error may contain just whitespace
				if (null != whitespace && XmlNodeType.Whitespace == whitespace.NodeType && "Error" != node.LocalName)
				{
					if (!IsLegalWhitespace(this.indentationAmount, level, whitespace.Value))
					{
						this.OnError(InspectorTestType.WhitespacePrecedingEndElementWrong, whitespace, "The whitespace preceding this end element is incorrect.");
						FixWhitespace(this.indentationAmount, level, whitespace);
					}
				}
			}
		}

		/// <summary>
		/// Inspects a Binary element.
		/// </summary>
		/// <param name="element">The Binary element to inspect.</param>
		private void InspectBinaryElement(XmlElement element)
		{
			XmlAttribute src = element.GetAttributeNode("src");

			if (null != src)
			{
				this.OnError(InspectorTestType.SrcIsDeprecated, src, "The Binary/@src attribute has been deprecated.  Use the SourceFile attribute instead.");

				XmlAttribute sourceFile = element.OwnerDocument.CreateAttribute("SourceFile");
				sourceFile.Value = src.Value;
				element.Attributes.InsertAfter(sourceFile, src);

				element.Attributes.Remove(src);
			}
		}

		/// <summary>
		/// Inspects a Category element.
		/// </summary>
		/// <param name="element">The Category element to inspect.</param>
		private void InspectCategoryElement(XmlElement element)
		{
			if (element.HasAttribute("AppData"))
			{
				if (String.Empty == element.GetAttribute("AppData"))
				{
					this.OnError(InspectorTestType.CategoryAppDataEmpty, element, "The Category/@AppData attribute's value cannot be an empty string.  If you want the value to be null or empty, simply remove the entire attribute.");
					element.RemoveAttribute("AppData");
				}
			}
		}

		/// <summary>
		/// Inspects a Class element.
		/// </summary>
		/// <param name="element">The Class element to inspect.</param>
		private void InspectClassElement(XmlElement element)
		{
			bool advertised = false;
			XmlAttribute description = element.GetAttributeNode("Description");
			XmlAttribute relativePath = element.GetAttributeNode("RelativePath");

			if (null != description && String.Empty == description.Value)
			{
				this.OnError(InspectorTestType.ClassDescriptionEmpty, element, "The Class/@Description attribute's value cannot be an empty string.");
				element.Attributes.Remove(description);
			}

			if (null != relativePath)
			{
				// check if this element or any of its parents is advertised
				for (XmlNode node = element; null != node; node = node.ParentNode)
				{
					if (node.Attributes != null)
					{
						XmlNode advertise = node.Attributes.GetNamedItem("Advertise");

						if (null != advertise && "yes" == advertise.Value)
						{
							advertised = true;
						}
					}
				}

				if (advertised) // if advertised, then RelativePath="no" is unnecessary since its the default value
				{
					if ("no" == relativePath.Value)
					{
						this.OnVerbose(element, "The Class/@RelativePath attribute with value 'no' is not necessary since this element is advertised.");
						element.Attributes.Remove(relativePath);
					}
				}
				else // if there is no advertising, then the RelativePath attribute is not allowed
				{
					this.OnError(InspectorTestType.ClassRelativePathMustBeAdvertised, element, "The Class/@RelativePath attribute is not supported for non-advertised Class elements.");
					element.Attributes.Remove(relativePath);
				}
			}
		}

		/// <summary>
		/// Inspects a Component element.
		/// </summary>
		/// <param name="element">The Component element to inspect.</param>
		private void InspectComponentElement(XmlElement element)
		{
			XmlAttribute guid = element.GetAttributeNode("Guid");

			if (null == guid)
			{
				this.OnError(InspectorTestType.RequireComponentGuid, guid, "The Component/@Guid attribute is required.  Setting this attribute's value to empty now enables the functionality previously represented by omitting the attribute.  This creates an unmanaged component.  Please note that unmanaged components can be a security vulnerability and should be carefully reviewed.");
				element.SetAttribute("Guid", String.Empty);
			}
		}

		/// <summary>
		/// Inspects a Control element.
		/// </summary>
		/// <param name="element">The Control element to inspect.</param>
		private void InspectControlElement(XmlElement element)
		{
			XmlAttribute checkBoxValue = element.GetAttributeNode("CheckBoxValue");

			if (null != checkBoxValue && String.Empty == checkBoxValue.Value)
			{
				this.OnError(InspectorTestType.IniFileValueEmpty, element, "The Control/@CheckBoxValue attribute's value cannot be the empty string.");
				element.Attributes.Remove(checkBoxValue);
			}
		}

		/// <summary>
		/// Inspects a DigitalCertificate element.
		/// </summary>
		/// <param name="element">The DigitalCertificate element to inspect.</param>
		private void InspectDigitalCertificateElement(XmlElement element)
		{
			XmlAttribute src = element.GetAttributeNode("src");

			if (null != src)
			{
				this.OnError(InspectorTestType.SrcIsDeprecated, src, "The DigitalCertificate/@src attribute has been deprecated.  Use the SourceFile attribute instead.");

				XmlAttribute sourceFile = element.OwnerDocument.CreateAttribute("SourceFile");
				sourceFile.Value = src.Value;
				element.Attributes.InsertAfter(sourceFile, src);

				element.Attributes.Remove(src);
			}
		}

		/// <summary>
		/// Inspects a DigitalSignature element.
		/// </summary>
		/// <param name="element">The DigitalSignature element to inspect.</param>
		private void InspectDigitalSignatureElement(XmlElement element)
		{
			XmlAttribute src = element.GetAttributeNode("src");

			if (null != src)
			{
				this.OnError(InspectorTestType.SrcIsDeprecated, src, "The DigitalSignature/@src attribute has been deprecated.  Use the SourceFile attribute instead.");

				XmlAttribute sourceFile = element.OwnerDocument.CreateAttribute("SourceFile");
				sourceFile.Value = src.Value;
				element.Attributes.InsertAfter(sourceFile, src);

				element.Attributes.Remove(src);
			}
		}

		/// <summary>
		/// Inspects a Directory element.
		/// </summary>
		/// <param name="element">The Directory element to inspect.</param>
		private void InspectDirectoryElement(XmlElement element)
		{
			XmlAttribute src = element.GetAttributeNode("src");

			if (null != src)
			{
				this.OnError(InspectorTestType.SrcIsDeprecated, src, "The Directory/@src attribute has been deprecated.  Use the FileSource attribute instead.");

				XmlAttribute fileSource = element.OwnerDocument.CreateAttribute("FileSource");
				fileSource.Value = src.Value;
				element.Attributes.InsertAfter(fileSource, src);

				element.Attributes.Remove(src);
			}
		}

		/// <summary>
		/// Inspects a DirectoryRef element.
		/// </summary>
		/// <param name="element">The DirectoryRef element to inspect.</param>
		private void InspectDirectoryRefElement(XmlElement element)
		{
			XmlAttribute src = element.GetAttributeNode("src");

			if (null != src)
			{
				this.OnError(InspectorTestType.SrcIsDeprecated, src, "The DirectoryRef/@src attribute has been deprecated.  Use the FileSource attribute instead.");

				XmlAttribute fileSource = element.OwnerDocument.CreateAttribute("FileSource");
				fileSource.Value = src.Value;
				element.Attributes.InsertAfter(fileSource, src);

				element.Attributes.Remove(src);
			}
		}

		/// <summary>
		/// Inspects an ExternalFile element.
		/// </summary>
		/// <param name="element">The ExternalFile element to inspect.</param>
		private void InspectExternalFileElement(XmlElement element)
		{
			XmlAttribute src = element.GetAttributeNode("src");

			if (null != src)
			{
				this.OnError(InspectorTestType.SrcIsDeprecated, src, "The ExternalFile/@src attribute has been deprecated.  Use the Source attribute instead.");

				XmlAttribute source = element.OwnerDocument.CreateAttribute("Source");
				source.Value = src.Value;
				element.Attributes.InsertAfter(source, src);

				element.Attributes.Remove(src);
			}
		}

		/// <summary>
		/// Inspects a Feature element.
		/// </summary>
		/// <param name="element">The Feature element to inspect.</param>
		private void InspectFeatureElement(XmlElement element)
		{
			XmlAttribute followParent = element.GetAttributeNode("FollowParent");
			XmlAttribute installDefault = element.GetAttributeNode("InstallDefault");

			if (null != followParent)
			{
				this.OnError(InspectorTestType.FeatureFollowParentDeprecated, followParent, "The Feature/@FollowParent attribute has been deprecated.  Value of 'yes' should now be represented with InstallDefault='followParent'.");

				// if InstallDefault is present, candle with display an error
				if ("yes" == followParent.Value && null == installDefault)
				{
					installDefault = element.OwnerDocument.CreateAttribute("InstallDefault");
					installDefault.Value = "followParent";
					element.Attributes.Append(installDefault);
				}

				element.Attributes.Remove(followParent);
			}
		}

		/// <summary>
		/// Inspects a File element.
		/// </summary>
		/// <param name="element">The File element to inspect.</param>
		private void InspectFileElement(XmlElement element)
		{
			XmlAttribute src = element.GetAttributeNode("src");

			if (null != src)
			{
				this.OnError(InspectorTestType.SrcIsDeprecated, src, "The File/@src attribute has been deprecated.  Use the Source attribute instead.");

				XmlAttribute source = element.OwnerDocument.CreateAttribute("Source");
				source.Value = src.Value;
				element.Attributes.InsertAfter(source, src);

				element.Attributes.Remove(src);
			}
		}

		/// <summary>
		/// Inspects a FileSearch element.
		/// </summary>
		/// <param name="element">The FileSearch element to inspect.</param>
		private void InspectFileSearchElement(XmlElement element)
		{
			XmlAttribute name = element.GetAttributeNode("Name");
			XmlAttribute longName = element.GetAttributeNode("LongName");

			// check for a short/long filename separator in the Name attribute
			if (null != name && 0 <= name.Value.IndexOf('|'))
			{
				if (null == longName) // long name is not present, split the Name if possible
				{
					string[] names = name.Value.Split("|".ToCharArray());

					// this appears to be splittable
					if (2 == names.Length)
					{
						this.OnError(InspectorTestType.FileSearchNamesCombined, element, "The FileSearch/@Name attribute appears to contain both a short and long file name.  It may only contain an 8.3 file name.  Also use the LongName attribute to specify a longer name.");

						// fix the short name
						name.Value = names[0];

						// insert the new LongName attribute after the previous Name attribute
						longName = element.OwnerDocument.CreateAttribute("LongName");
						longName.Value = names[1];
						element.Attributes.InsertAfter(longName, name);
					}
				}
			}
		}

		/// <summary>
		/// Inspects an Icon element.
		/// </summary>
		/// <param name="element">The Icon element to inspect.</param>
		private void InspectIconElement(XmlElement element)
		{
			XmlAttribute src = element.GetAttributeNode("src");

			if (null != src)
			{
				this.OnError(InspectorTestType.SrcIsDeprecated, src, "The Icon/@src attribute has been deprecated.  Use the SourceFile attribute instead.");

				XmlAttribute sourceFile = element.OwnerDocument.CreateAttribute("SourceFile");
				sourceFile.Value = src.Value;
				element.Attributes.InsertAfter(sourceFile, src);

				element.Attributes.Remove(src);
			}
		}

		/// <summary>
		/// Inspects an IniFile element.
		/// </summary>
		/// <param name="element">The IniFile element to inspect.</param>
		private void InspectIniFileElement(XmlElement element)
		{
			XmlAttribute value = element.GetAttributeNode("Value");

			if (null != value && String.Empty == value.Value)
			{
				this.OnError(InspectorTestType.IniFileValueEmpty, element, "The IniFile/@Value attribute's value cannot be the empty string.");
				element.Attributes.Remove(value);
			}
		}

		/// <summary>
		/// Inspects a Media element.
		/// </summary>
		/// <param name="element">The Media element to inspect.</param>
		private void InspectMediaElement(XmlElement element)
		{
			XmlAttribute src = element.GetAttributeNode("src");

			if (null != src)
			{
				this.OnError(InspectorTestType.SrcIsDeprecated, src, "The Media/@src attribute has been deprecated.  Use the Layout attribute instead.");

				XmlAttribute layout = element.OwnerDocument.CreateAttribute("Layout");
				layout.Value = src.Value;
				element.Attributes.InsertAfter(layout, src);

				element.Attributes.Remove(src);
			}
		}

		/// <summary>
		/// Inspects a Merge element.
		/// </summary>
		/// <param name="element">The Merge element to inspect.</param>
		private void InspectMergeElement(XmlElement element)
		{
			XmlAttribute src = element.GetAttributeNode("src");

			if (null != src)
			{
				this.OnError(InspectorTestType.SrcIsDeprecated, src, "The Merge/@src attribute has been deprecated.  Use the SourceFile attribute instead.");

				XmlAttribute sourceFile = element.OwnerDocument.CreateAttribute("SourceFile");
				sourceFile.Value = src.Value;
				element.Attributes.InsertAfter(sourceFile, src);

				element.Attributes.Remove(src);
			}
		}

		/// <summary>
		/// Inspects a ProgressText element.
		/// </summary>
		/// <param name="element">The ProgressText element to inspect.</param>
		private void InspectProgressTextElement(XmlElement element)
		{
			XmlAttribute template = element.GetAttributeNode("Template");

			if (null != template && String.Empty == template.Value)
			{
				this.OnError(InspectorTestType.ProgressTextTemplateEmpty, element, "The ProgressText/@Template attribute's value cannot be the empty string.");
				element.Attributes.Remove(template);
			}
		}

		/// <summary>
		/// Inspects a Property element.
		/// </summary>
		/// <param name="element">The Property element to inspect.</param>
		private void InspectPropertyElement(XmlElement element)
		{
			XmlAttribute value = element.GetAttributeNode("Value");

			if (null != value && String.Empty == value.Value)
			{
				this.OnError(InspectorTestType.PropertyValueEmpty, element, "The Property/@Value attribute's value cannot be the empty string.");
				element.Attributes.Remove(value);
			}
		}

		/// <summary>
		/// Inspects a RadioButton element.
		/// </summary>
		/// <param name="element">The RadioButton element to inspect.</param>
		private void InspectRadioButtonElement(XmlElement element)
		{
			if (!element.HasAttribute("Value"))
			{
				this.OnError(InspectorTestType.RadioButtonMissingValue, element, "The required attribute RadioButton/@Value is missing.  Inner text has been depreciated in favor of the this attribute.");
				element.SetAttribute("Value", element.InnerText);
				element.InnerText = null;
			}
		}

		/// <summary>
		/// Inspects a RadioGroup element.
		/// </summary>
		/// <param name="element">The RadioGroup element to inspect.</param>
		private void InspectRadioGroupElement(XmlElement element)
		{
			XmlElement radioButtonGroup = element.OwnerDocument.CreateElement("RadioButtonGroup", element.NamespaceURI);

			this.OnError(InspectorTestType.RadioGroupDeprecated, element, "The RadioGroup element is deprecated.  Use RadioButtonGroup instead.");
			element.ParentNode.InsertAfter(radioButtonGroup, element);
			element.ParentNode.RemoveChild(element);

			// move all the attributes from the old element to the new element
			while (element.Attributes.Count > 0)
			{
				XmlAttribute attribute = element.Attributes[0];

				element.Attributes.Remove(attribute);
				radioButtonGroup.Attributes.Append(attribute);
			}

			// move all the attributes from the old element to the new element
			while (element.ChildNodes.Count > 0)
			{
				XmlNode node = element.ChildNodes[0];

				element.RemoveChild(node);
				radioButtonGroup.AppendChild(node);
			}
		}

		/// <summary>
		/// Inspects a RegistrySearch element.
		/// </summary>
		/// <param name="element">The RegistrySearch element to inspect.</param>
		private void InspectRegistrySearchElement(XmlElement element)
		{
			XmlAttribute type = element.GetAttributeNode("Type");

			if (null != type && "registry" == type.Value)
			{
				this.OnError(InspectorTestType.RegistrySearchTypeRegistryDeprecated, element, "The RegistrySearch/@Type attribute's value 'registry' has been deprecated.  Please use the value 'raw' instead.");
				element.SetAttribute("Type", "raw");
			}
		}

		/// <summary>
		/// Inspects a ServiceInstall element.
		/// </summary>
		/// <param name="element">The ServiceInstall element to inspect.</param>
		private void InspectServiceInstallElement(XmlElement element)
		{
			XmlAttribute localGroup = element.GetAttributeNode("LocalGroup");
			XmlAttribute password = element.GetAttributeNode("Password");

			if (null != localGroup && String.Empty == localGroup.Value)
			{
				this.OnError(InspectorTestType.ServiceInstallLocalGroupEmpty, element, "The ServiceInstall/@LocalGroup attribute's value cannot be the empty string.");
				element.Attributes.Remove(localGroup);
			}

			if (null != password && String.Empty == password.Value)
			{
				this.OnError(InspectorTestType.ServiceInstallPasswordEmpty, element, "The ServiceInstall/@Password attribute's value cannot be the empty string.");
				element.Attributes.Remove(password);
			}
		}

		/// <summary>
		/// Inspects a SFPCatalog element.
		/// </summary>
		/// <param name="element">The SFPCatalog element to inspect.</param>
		private void InspectSFPCatalogElement(XmlElement element)
		{
			XmlAttribute src = element.GetAttributeNode("src");

			if (null != src)
			{
				this.OnError(InspectorTestType.SrcIsDeprecated, src, "The SFPCatalog/@src attribute has been deprecated.  Use the SourceFile attribute instead.");

				XmlAttribute sourceFile = element.OwnerDocument.CreateAttribute("SourceFile");
				sourceFile.Value = src.Value;
				element.Attributes.InsertAfter(sourceFile, src);

				element.Attributes.Remove(src);
			}
		}

		/// <summary>
		/// Inspects a Shortcut element.
		/// </summary>
		/// <param name="element">The Shortcut element to inspect.</param>
		private void InspectShortcutElement(XmlElement element)
		{
			XmlAttribute workingDirectory = element.GetAttributeNode("WorkingDirectory");

			if (null != workingDirectory && String.Empty == workingDirectory.Value)
			{
				this.OnError(InspectorTestType.ShortcutWorkingDirectoryEmpty, element, "The Shortcut/@WorkingDirectory attribute's value cannot be the empty string.");
				element.Attributes.Remove(workingDirectory);
			}
		}

		/// <summary>
		/// Inspects a TargetImage element.
		/// </summary>
		/// <param name="element">The TargetImage element to inspect.</param>
		private void InspectTargetImageElement(XmlElement element)
		{
			XmlAttribute src = element.GetAttributeNode("src");

			if (null != src)
			{
				this.OnError(InspectorTestType.SrcIsDeprecated, src, "The TargetImage/@src attribute has been deprecated.  Use the SourceFile attribute instead.");

				XmlAttribute sourceFile = element.OwnerDocument.CreateAttribute("SourceFile");
				sourceFile.Value = src.Value;
				element.Attributes.InsertAfter(sourceFile, src);

				element.Attributes.Remove(src);
			}
		}

		/// <summary>
		/// Inspects a Text element.
		/// </summary>
		/// <param name="element">The Text element to inspect.</param>
		private void InspectTextElement(XmlElement element)
		{
			XmlAttribute src = element.GetAttributeNode("src");

			if (null != src)
			{
				this.OnError(InspectorTestType.SrcIsDeprecated, src, "The Text/@src attribute has been deprecated.  Use the SourceFile attribute instead.");

				XmlAttribute sourceFile = element.OwnerDocument.CreateAttribute("SourceFile");
				sourceFile.Value = src.Value;
				element.Attributes.InsertAfter(sourceFile, src);

				element.Attributes.Remove(src);
			}
		}

		/// <summary>
		/// Inspects a TypeLib element.
		/// </summary>
		/// <param name="element">The TypeLib element to inspect.</param>
		private void InspectTypeLibElement(XmlElement element)
		{
			XmlAttribute description = element.GetAttributeNode("Description");

			if (null != description && string.Empty == description.Value)
			{
				this.OnError(InspectorTestType.TypeLibDescriptionEmpty, element, "The TypeLib/@Description attribute's value cannot be an empty string.  If you want the value to be null or empty, simply drop the entire attribute.");
				element.Attributes.Remove(description);
			}
		}

		/// <summary>
		/// Inspects an UpgradeImage element.
		/// </summary>
		/// <param name="element">The UpgradeImage element to inspect.</param>
		private void InspectUpgradeImageElement(XmlElement element)
		{
			XmlAttribute src = element.GetAttributeNode("src");
			XmlAttribute srcPatch = element.GetAttributeNode("srcPatch");

			if (null != src)
			{
				this.OnError(InspectorTestType.SrcIsDeprecated, src, "The UpgradeImage/@src attribute has been deprecated.  Use the SourceFile attribute instead.");

				XmlAttribute sourceFile = element.OwnerDocument.CreateAttribute("SourceFile");
				sourceFile.Value = src.Value;
				element.Attributes.InsertAfter(sourceFile, src);

				element.Attributes.Remove(src);
			}

			if (null != srcPatch)
			{
				this.OnError(InspectorTestType.SrcIsDeprecated, src, "The UpgradeImage/@srcPatch attribute has been deprecated.  Use the SourcePatch attribute instead.");

				XmlAttribute sourcePatch = element.OwnerDocument.CreateAttribute("SourcePatch");
				sourcePatch.Value = srcPatch.Value;
				element.Attributes.InsertAfter(sourcePatch, srcPatch);

				element.Attributes.Remove(srcPatch);
			}
		}

		/// <summary>
		/// Inspects an UpgradeVersion element.
		/// </summary>
		/// <param name="element">The UpgradeVersion element to inspect.</param>
		private void InspectUpgradeVersionElement(XmlElement element)
		{
			XmlAttribute removeFeatures = element.GetAttributeNode("RemoveFeatures");

			if (null != removeFeatures && string.Empty == removeFeatures.Value)
			{
				this.OnError(InspectorTestType.TypeLibDescriptionEmpty, element, "The UpgradeVersion/@RemoveFeatures attribute's value cannot be an empty string.  If you want the value to be null or empty, simply drop the entire attribute.");
				element.Attributes.Remove(removeFeatures);
			}
		}

		/// <summary>
		/// Inspects a WebApplicationExtension element.
		/// </summary>
		/// <param name="element">The WebApplicationExtension element to inspect.</param>
		private void InspectWebApplicationExtensionElement(XmlElement element)
		{
			XmlAttribute extension = element.GetAttributeNode("Extension");
			XmlAttribute id = element.GetAttributeNode("Id");

			if (null != id)
			{
				// an empty Id attribute value should be replaced with "*"
				if (String.Empty == id.Value)
				{
					this.OnError(InspectorTestType.WebApplicationExtensionIdEmpty, element, "The WebApplicationExtension/@Id attribute's value cannot be an empty string.  Use '*' for the value instead.");
					id.Value = "*";
				}

				// Id has been deprecated, use Extension instead
				if (null == extension)
				{
					this.OnError(InspectorTestType.WebApplicationExtensionIdDeprecated, element, "The WebApplicationExtension/@Id attribute has been deprecated.  Use the Extension attribute instead.");
					extension = element.OwnerDocument.CreateAttribute("Extension");
					extension.Value = id.Value;
					element.Attributes.InsertAfter(extension, id);
					element.Attributes.Remove(id);
				}
			}
		}

		/// <summary>
		/// Inspects a Wix element.
		/// </summary>
		/// <param name="element">The Wix element to inspect.</param>
		private void InspectWixElement(XmlElement element)
		{
			XmlAttribute xmlns = element.GetAttributeNode("xmlns");

			if (null == xmlns)
			{
				this.OnError(InspectorTestType.XmlnsMissing, element, "The xmlns attribute is missing.  It must be present with a value of '{0}'.", WixNamespaceURI);
				element.SetAttribute("xmlns", WixNamespaceURI);
			}
			else if (WixNamespaceURI != xmlns.Value)
			{
				this.OnError(InspectorTestType.XmlnsValueWrong, xmlns, "The xmlns attribute's value is wrong.  It must be '{0}'.", WixNamespaceURI);
				element.SetAttribute("xmlns", WixNamespaceURI);
			}
		}

		/// <summary>
		/// Output an error message to the console.
		/// </summary>
		/// <param name="inspectorTestType">The type of inspector test.</param>
		/// <param name="node">The node that caused the error.</param>
		/// <param name="message">Detailed error message.</param>
		/// <param name="args">Additional formatted string arguments.</param>
		private void OnError(InspectorTestType inspectorTestType, XmlNode node, string message, params object[] args)
		{
			// increase the error count
			this.errors++;

			// set the warning/error part of the message
			string warningError;
			if (this.errorsAsWarnings.Contains(inspectorTestType)) // error as warning
			{
				warningError = "warning";
			}
			else // normal error
			{
				warningError = "error";
			}

			if (null != node)
			{
				Console.WriteLine("{0}({1}) : {2} WXCP{3:0000} : {4} ({5})", this.sourceFile, ((IXmlLineInfo)node).LineNumber, warningError, (int)inspectorTestType, String.Format(message, args), inspectorTestType.ToString());
			}
			else
			{
				string source = (null == this.sourceFile ? "wixcop.exe" : this.sourceFile);

				Console.WriteLine("{0} : {1} WXCP{2:0000} : {3} ({4})", source, warningError, (int)inspectorTestType, String.Format(message, args), inspectorTestType.ToString());
			}
		}

		/// <summary>
		/// Output a message to the console.
		/// </summary>
		/// <param name="node">The node that caused the message.</param>
		/// <param name="message">Detailed message.</param>
		/// <param name="args">Additional formatted string arguments.</param>
		private void OnVerbose(XmlNode node, string message, params string[] args)
		{
			this.errors++;

			if (null != node)
			{
				Console.WriteLine("{0}({1}) : {2}", this.sourceFile, ((IXmlLineInfo)node).LineNumber, String.Format(message, args));
			}
			else
			{
				string source = (null == this.sourceFile ? "wixcop.exe" : this.sourceFile);

				Console.WriteLine("{0} : {1}", source, String.Format(message, args));
			}
		}
	}
}
