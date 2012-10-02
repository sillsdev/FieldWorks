//-------------------------------------------------------------------------------------------------
// <copyright file="XmlSchemaCompiler.cs" company="Microsoft">
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
// Compile an xsd schema into several documentation files.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.DocCompiler
{
	using System;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Xml;
	using System.Xml.Schema;

	/// <summary>
	/// Compile an xsd schema into several documentation files.
	/// </summary>
	public sealed class XmlSchemaCompiler
	{
		private const string XHtmlNamespace = "http://www.w3.org/1999/xhtml";
		private const string XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";
		private const string XmlSchemaExtensionNamespace = "http://schemas.microsoft.com/wix/2005/XmlSchemaExtension";

		// TODO: remove these regular expressions and replace with better logic for writing documentation
		private static Regex htmlPrefix = new Regex("html:", RegexOptions.Compiled);

		private Hashtable elements;
		private XmlSchema mainSchema;
		private string outputDir;
		private StringCollection schemaFiles;
		private XmlSchemaCollection schemas;
		private string versionNumber;

		/// <summary>
		/// Instantiate a new XmlSchemaCompiler class.
		/// </summary>
		/// <param name="outputDir">The output directory for all compiled files.</param>
		/// <param name="versionNumber">The version number to append to Elements.</param>
		public XmlSchemaCompiler(string outputDir, string versionNumber)
		{
			this.outputDir = outputDir;
			this.versionNumber = versionNumber;

			this.elements = new Hashtable();
			this.schemaFiles = new StringCollection();
			this.schemas = new XmlSchemaCollection();
		}

		/// <summary>
		/// Compile the schema nodes to the output directory.
		/// </summary>
		/// <param name="schemaNodes">The schema nodes.</param>
		public void CompileSchemas(XmlNodeList schemaNodes)
		{
			// clear out the previously indexed elements
			this.elements.Clear();

			// sort the schemas
			ArrayList sortedSchemaElements = new ArrayList();
			foreach (XmlElement schemaElement in schemaNodes)
			{
				string main = schemaElement.GetAttribute("Main");

				if (main == "yes")
				{
					sortedSchemaElements.Insert(0, schemaElement);
				}
				else
				{
					sortedSchemaElements.Add(schemaElement);
				}
			}

			// add the schemas in order
			foreach (XmlElement schemaElement in sortedSchemaElements)
			{
				string sourceFile = schemaElement.GetAttribute("SourceFile");

				XmlTextReader reader = null;
				try
				{
					reader = new XmlTextReader(sourceFile);
					XmlSchema schema = XmlSchema.Read(reader, null);

					// keep track of the main schema
					if (this.mainSchema == null)
					{
						this.mainSchema = schema;
					}

					// add the schema to the collection
					this.schemas.Add(schema);

					// index the elements
					foreach (XmlSchemaElement element in schema.Elements.Values)
					{
						this.IndexElement(null, element);
					}

					// write hhc and hhk fragments
					this.ExpandTopics(schema, schemaElement);

					// write schema doc
					this.WriteSchemaDoc(schema);

					// write simple type docs
					foreach (XmlSchemaType type in schema.SchemaTypes.Values)
					{
						if (type is XmlSchemaSimpleType)
						{
							this.WriteSimpleTypeDoc(schema, (XmlSchemaSimpleType)type);
						}
					}
				}
				finally
				{
					if (reader != null)
					{
						reader.Close();
					}
				}
			}

			// write element docs
			foreach (DictionaryEntry entry in this.elements)
			{
				XmlQualifiedName qualifiedName = (XmlQualifiedName)entry.Key;
				XmlSchemaElementInfo elementInfo = (XmlSchemaElementInfo)entry.Value;

				// find the parent schema and element for the qualified name
				XmlSchema schema = this.schemas[qualifiedName.Namespace];
				XmlSchemaElement element = (XmlSchemaElement)schema.Elements[qualifiedName];

				this.WriteElementDoc(schema, element, elementInfo);
			}
		}

		/// <summary>
		/// Capitalize the first character of a string.
		/// </summary>
		/// <param name="str">The string to capitalize.</param>
		/// <returns>The capitalized string.</returns>
		private static string Capitalize(string str)
		{
			if (str.Length > 0)
			{
				str = String.Concat(Char.ToUpper(str[0], CultureInfo.InvariantCulture), str.Substring(1));
			}

			return str;
		}

		/// <summary>
		/// Get the description from an xml schema object.
		/// </summary>
		/// <param name="annotated">The xml schema object.</param>
		/// <returns>The description of the object.</returns>
		private static string GetDescription(XmlSchemaAnnotated annotated)
		{
			if (annotated.Annotation == null)
			{
				return null;
			}

			return GetDescription(annotated.Annotation);
		}

		/// <summary>
		/// Get the description from an xml schema object.
		/// </summary>
		/// <param name="annotation">The xml schema object.</param>
		/// <returns>The description of the object.</returns>
		private static string GetDescription(XmlSchemaAnnotation annotation)
		{
			StringBuilder documentation = new StringBuilder();

			// retrieve the documentation nodes
			foreach (XmlSchemaObject obj in annotation.Items)
			{
				XmlSchemaDocumentation doc = obj as XmlSchemaDocumentation;

				if (doc != null)
				{
					foreach (XmlNode node in doc.Markup)
					{
						if (node is XmlText)
						{
							documentation.Append(((XmlText)node).OuterXml);
						}
						else if (node is XmlElement)
						{
							documentation.Append(((XmlElement)node).OuterXml);
						}
					}
				}
			}

			documentation.Replace("\t", String.Empty);
			documentation.Replace(String.Concat(Environment.NewLine, Environment.NewLine), "</p><p>");
			documentation.Replace(Environment.NewLine, " ");
			return htmlPrefix.Replace(documentation.ToString(), String.Empty);
		}

		/// <summary>
		/// Get the nicely formatted name of the schema.
		/// </summary>
		/// <param name="schema">The schema.</param>
		/// <returns>A nicely formatted name of the schema.</returns>
		private static string GetSchemaName(XmlSchema schema)
		{
			string name = Path.GetFileNameWithoutExtension(schema.SourceUri);

			return String.Concat(Char.ToUpper(name[0]), name.Substring(1));
		}

		/// <summary>
		/// Create an XmlTextWriter for writing an html document.
		/// </summary>
		/// <param name="file">The file which will contain the document.</param>
		/// <param name="title">The title of the document.</param>
		/// <param name="headerClass">The class for the header element.</param>
		/// <returns>The XmlTextWriter for the document.</returns>
		private static XmlTextWriter StartHtmlWriter(string file, string title, string headerClass)
		{
			XmlTextWriter writer = new XmlTextWriter(file, Encoding.UTF8);
			writer.Formatting = Formatting.Indented;

			writer.WriteDocType("html", "-//W3C//DTD XHTML 1.0 Transitional//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", null);


			writer.WriteStartElement("html", XHtmlNamespace);

			writer.WriteStartElement("head");

			writer.WriteStartElement("link");
			writer.WriteAttributeString("rel", "stylesheet");
			writer.WriteAttributeString("type", "text/css");
			writer.WriteAttributeString("href", "style.css");
			writer.WriteEndElement();

			writer.WriteStartElement("title");
			writer.WriteString(title);
			writer.WriteEndElement();

			// end head element
			writer.WriteEndElement();

			writer.WriteStartElement("body");
			writer.WriteAttributeString("class", "schema");

			writer.WriteStartElement("div");
			writer.WriteAttributeString("class", headerClass);
			writer.WriteStartElement("h1");
			writer.WriteAttributeString("class", "schema");
			writer.WriteString(title);
			writer.WriteEndElement();
			writer.WriteEndElement();

			writer.WriteStartElement("div");
			writer.WriteAttributeString("class", "schemaMain");

			return writer;
		}

		/// <summary>
		/// End elements created in StartHtmlWriter.
		/// </summary>
		/// <param name="writer">The html writer.</param>
		private static void EndHtmlWriter(XmlTextWriter writer)
		{
			// end div element
			writer.WriteEndElement();

			// end body element
			writer.WriteEndElement();

			// end html element
			writer.WriteEndElement();
		}

		/// <summary>
		/// Write an html anchor link.
		/// </summary>
		/// <param name="href">The address of the link.</param>
		/// <param name="text">The text of the link.</param>
		/// <param name="linkClass">The class for the link.</param>
		/// <param name="writer">The html writer.</param>
		private static void WriteLink(string href, string text, string linkClass, XmlTextWriter writer)
		{
			writer.WriteStartElement("a");
			writer.WriteAttributeString("href", href);
			if (linkClass != null)
			{
				writer.WriteAttributeString("class", linkClass);
			}
			writer.WriteString(text);
			writer.WriteEndElement();
		}

		/// <summary>
		/// Expand the schema node into a hierarchy of topic nodes.
		/// </summary>
		/// <param name="schema">The schema to expand.</param>
		/// <param name="schemaElement">The schema node to replace.</param>
		private void ExpandTopics(XmlSchema schema, XmlElement schemaElement)
		{
			string schemaName = GetSchemaName(schema);

			// sort the element names
			SortedList qualifiedNames = new SortedList();
			foreach (XmlSchemaElement element in schema.Elements.Values)
			{
				qualifiedNames.Add(element.QualifiedName.Name, element.QualifiedName);
			}

			// create the main topic
			XmlElement mainTopicElement = schemaElement.OwnerDocument.CreateElement("Topic", DocCompiler.DocCompilerNamespace);
			mainTopicElement.SetAttribute("Title", String.Concat(schemaName, " Schema"));
			mainTopicElement.SetAttribute("SourceFile", String.Concat("html/", this.GetSchemaHtmlFileName(schema, "index")));
			mainTopicElement.SetAttribute("DestinationFile", String.Concat("html/", this.GetSchemaHtmlFileName(schema, "index")));
			schemaElement.ParentNode.InsertAfter(mainTopicElement, schemaElement);
			schemaElement.ParentNode.RemoveChild(schemaElement);

			// create a topic for each element
			foreach (XmlQualifiedName qualifiedName in qualifiedNames.Values)
			{
				if (schema.TargetNamespace == qualifiedName.Namespace)
				{
					XmlElement topicElement = schemaElement.OwnerDocument.CreateElement("Topic", DocCompiler.DocCompilerNamespace);
					topicElement.SetAttribute("Title", String.Concat(qualifiedName.Name, " Element"));
					topicElement.SetAttribute("SourceFile", String.Concat("html/", this.GetSchemaHtmlFileName(schema, qualifiedName.Name)));
					topicElement.SetAttribute("DestinationFile", String.Concat("html/", this.GetSchemaHtmlFileName(schema, qualifiedName.Name)));
					mainTopicElement.AppendChild(topicElement);
				}
			}
		}

		/// <summary>
		/// Index an element. This finds the parents for all elements.
		/// </summary>
		/// <param name="parentElement">The parent element of the element to index.</param>
		/// <param name="element">The element to index.</param>
		private void IndexElement(XmlSchemaElement parentElement, XmlSchemaElement element)
		{
			XmlSchemaElementInfo elementInfo = (XmlSchemaElementInfo)this.elements[element.QualifiedName];

			if (elementInfo == null)
			{
				elementInfo = new XmlSchemaElementInfo();
				this.elements.Add(element.QualifiedName, elementInfo);
			}

			// index the parent
			if (parentElement != null)
			{
				elementInfo.AddParent(parentElement.QualifiedName);
			}

			// do not index an element if it has already been indexed
			if (!elementInfo.Indexed)
			{
				// retrieve the real element (not just a reference to it)
				if (element.QualifiedName.Namespace != null)
				{
					element = (XmlSchemaElement)this.schemas[element.QualifiedName.Namespace].Elements[element.QualifiedName];
				}

				// mark the element as indexed early in case it references itself
				elementInfo.Indexed = true;

				// retrieve explicitly-specified parent elements
				if (element.Annotation != null)
				{
					foreach (XmlSchemaObject obj in element.Annotation.Items)
					{
						XmlSchemaAppInfo appInfo = obj as XmlSchemaAppInfo;

						if (appInfo != null)
						{
							foreach (XmlNode node in appInfo.Markup)
							{
								XmlElement markupElement = node as XmlElement;

								if (markupElement != null && markupElement.LocalName == "parent" && markupElement.NamespaceURI == XmlSchemaExtensionNamespace)
								{
									string parentNamespace = markupElement.GetAttribute("namespace");
									string parentRef = markupElement.GetAttribute("ref");

									if (parentNamespace.Length == 0)
									{
										throw new ApplicationException("The parent element is missing the namespace attribute.");
									}

									if (parentRef.Length == 0)
									{
										throw new ApplicationException("The parent element is missing the ref attribute.");
									}

									XmlQualifiedName parentQualifiedName = new XmlQualifiedName(parentRef, parentNamespace);

									// add the explicit parent to the list of parents for this element
									elementInfo.AddParent(parentQualifiedName);

									// add this element to the extended list of children for its parent
									XmlSchemaElementInfo parentElementInfo = (XmlSchemaElementInfo)this.elements[parentQualifiedName];
									if (parentElementInfo == null)
									{
										parentElementInfo = new XmlSchemaElementInfo();
										this.elements.Add(parentQualifiedName, parentElementInfo);
									}
									parentElementInfo.AddExtendedChild(element.QualifiedName);
								}
							}
						}
					}
				}

				if (element.ElementType is XmlSchemaComplexType)
				{
					XmlSchemaComplexType complexType = (XmlSchemaComplexType)element.ElementType;

					if (complexType.Particle != null)
					{
						this.IndexParticle(element, complexType.Particle);
					}
				}
			}
		}

		/// <summary>
		/// Index a particle. This finds the parents for all elements.
		/// </summary>
		/// <param name="parentElement">The parent element of the particle to index.</param>
		/// <param name="particle">The particle to index.</param>
		private void IndexParticle(XmlSchemaElement parentElement, XmlSchemaParticle particle)
		{
			if (particle is XmlSchemaAny)
			{
				// ignore
			}
			else if (particle is XmlSchemaChoice)
			{
				XmlSchemaChoice choice = (XmlSchemaChoice)particle;

				foreach (XmlSchemaParticle childParticle in choice.Items)
				{
					this.IndexParticle(parentElement, childParticle);
				}
			}
			else if (particle is XmlSchemaElement)
			{
				XmlSchemaElement element = (XmlSchemaElement)particle;

				// locally defined elements are not supported
				if (element.QualifiedName.Namespace.Length == 0)
				{
					throw new ApplicationException(String.Format("Locally defined element '{0}' is not supported.  Please define at a global scope.", element.QualifiedName.Name));
				}

				this.IndexElement(parentElement, element);
			}
			else if (particle is XmlSchemaSequence)
			{
				XmlSchemaSequence sequence = (XmlSchemaSequence)particle;

				foreach (XmlSchemaParticle childParticle in sequence.Items)
				{
					this.IndexParticle(parentElement, childParticle);
				}
			}
			else
			{
				throw new ApplicationException(String.Format("Unknown particle type: {0}.", particle.GetType().ToString()));
			}
		}

		/// <summary>
		/// Write the documentation file for the schema.
		/// </summary>
		/// <param name="schema">The schema.</param>
		private void WriteSchemaDoc(XmlSchema schema)
		{
			string htmlFile = this.GetSchemaHtmlFile(schema, "index");

			// find the root element(s)
			SortedList rootElementQualifiedNames = new SortedList();
			SortedList elementQualifiedNames = new SortedList();
			foreach (DictionaryEntry entry in this.elements)
			{
				XmlQualifiedName qualifiedName = (XmlQualifiedName)entry.Key;
				XmlSchemaElementInfo elementInfo = (XmlSchemaElementInfo)entry.Value;

				if (qualifiedName.Namespace == schema.TargetNamespace)
				{
					if (elementInfo.Parents.Count == 0)
					{
						rootElementQualifiedNames.Add(qualifiedName.Name, qualifiedName);
					}
					elementQualifiedNames.Add(qualifiedName.Name, qualifiedName);
				}
			}

			XmlTextWriter writer = null;
			try
			{
				string headerClass = (schema == this.mainSchema ? "normalHeader" : "extensionHeader");
				writer = StartHtmlWriter(htmlFile, String.Format("{0} Schema", GetSchemaName(schema)), headerClass);

				// description
				if (schema.Items.Count > 0 && schema.Items[0] is XmlSchemaAnnotation)
				{
					string description = GetDescription((XmlSchemaAnnotation)schema.Items[0]);

					writer.WriteStartElement("p");
					writer.WriteRaw(description);
					writer.WriteEndElement();
				}

				writer.WriteStartElement("dl");

				// root elements
				if (rootElementQualifiedNames.Count > 0)
				{
					writer.WriteStartElement("dt");
					if (rootElementQualifiedNames.Count == 1)
					{
						writer.WriteString("Root Element");
					}
					else
					{
						writer.WriteString("Root Elements");
					}
					writer.WriteEndElement();

					writer.WriteStartElement("dd");
					writer.WriteStartElement("ul");
					foreach (XmlQualifiedName rootElementQualifiedName in rootElementQualifiedNames.Values)
					{
						writer.WriteStartElement("li");
						this.WriteElementLink(rootElementQualifiedName, writer);
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
					writer.WriteEndElement();
				}

				// target namespace
				writer.WriteStartElement("dt");
				writer.WriteString("Target Namespace");
				writer.WriteEndElement();
				writer.WriteStartElement("dd");
				writer.WriteString(schema.TargetNamespace);
				writer.WriteEndElement();

				// document should look like
				if (rootElementQualifiedNames.Count > 0)
				{
					writer.WriteStartElement("dt");
					writer.WriteString("Document Should Look Like");
					writer.WriteEndElement();

					writer.WriteStartElement("dd");
					writer.WriteStartElement("ul");
					foreach (XmlQualifiedName rootElementQualifiedName in rootElementQualifiedNames.Values)
					{
						writer.WriteStartElement("li");
						writer.WriteString("<?xml version=\"1.0\"?>");
						writer.WriteStartElement("br");
						writer.WriteEndElement();

						writer.WriteString("<");
						this.WriteElementLink(rootElementQualifiedName, writer);
						writer.WriteString(String.Format(" xmlns=\"{0}\">", schema.TargetNamespace));
						writer.WriteStartElement("br");
						writer.WriteEndElement();

						writer.WriteString(".");
						writer.WriteStartElement("br");
						writer.WriteEndElement();

						writer.WriteString(".");
						writer.WriteStartElement("br");
						writer.WriteEndElement();

						writer.WriteString(".");
						writer.WriteStartElement("br");
						writer.WriteEndElement();

						writer.WriteString(String.Format("</{0}>", rootElementQualifiedName.Name));

						writer.WriteEndElement();
					}
					writer.WriteEndElement();
					writer.WriteEndElement();
				}

				// elements
				if (elementQualifiedNames.Count > 0)
				{
					writer.WriteStartElement("dt");
					writer.WriteString("All Elements");
					writer.WriteEndElement();

					writer.WriteStartElement("dd");
					writer.WriteStartElement("ul");
					foreach (XmlQualifiedName elementQualifiedName in elementQualifiedNames.Values)
					{
						writer.WriteStartElement("li");
						this.WriteElementLink(elementQualifiedName, writer);
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
					writer.WriteEndElement();
				}

				// end dl element
				writer.WriteEndElement();

				EndHtmlWriter(writer);
			}
			finally
			{
				if (writer != null)
				{
					writer.Close();
				}
			}
		}

		/// <summary>
		/// Write a documentation file for a simple type.
		/// </summary>
		/// <param name="schema">Parent schema of the simple type.</param>
		/// <param name="simpleType">The simple type.</param>
		private void WriteSimpleTypeDoc(XmlSchema schema, XmlSchemaSimpleType simpleType)
		{
			string htmlFile = this.GetSchemaHtmlFile(schema, String.Concat("simple_type_", simpleType.Name));

			XmlTextWriter writer = null;
			try
			{
				writer = StartHtmlWriter(htmlFile, String.Format("{0} (Simple Type)", simpleType.Name), "simpleTypeHeader");

				writer.WriteStartElement("dl");

				// description
				this.WriteDescription(simpleType, writer);

				// details
				if (simpleType.Content is XmlSchemaSimpleTypeRestriction)
				{
					XmlSchemaSimpleTypeRestriction simpleTypeRestriction = (XmlSchemaSimpleTypeRestriction)simpleType.Content;

					if (simpleTypeRestriction.Facets.Count > 0)
					{
						XmlSchemaObject firstFacet = simpleTypeRestriction.Facets[0];

						if (firstFacet is XmlSchemaEnumerationFacet)
						{
							writer.WriteStartElement("dt");
							writer.WriteString("Enumeration Type");
							writer.WriteEndElement();

							writer.WriteStartElement("dd");
							bool first = true;
							writer.WriteString("Possible values: {");
							foreach (XmlSchemaEnumerationFacet enumerationFacet in simpleTypeRestriction.Facets)
							{
								if (first)
								{
									first = false;
								}
								else
								{
									writer.WriteString(", ");
								}
								writer.WriteString(enumerationFacet.Value);
							}
							writer.WriteString("}");
							writer.WriteEndElement();
						}
						else if (firstFacet is XmlSchemaPatternFacet)
						{
							XmlSchemaPatternFacet patternFacet = (XmlSchemaPatternFacet)firstFacet;

							writer.WriteStartElement("dt");
							writer.WriteString("Pattern Type");
							writer.WriteEndElement();

							writer.WriteStartElement("dd");
							writer.WriteString(String.Format("Must match the regular expression: '{0}'.", patternFacet.Value));
							writer.WriteEndElement();
						}
						else // some other base
						{
							writer.WriteStartElement("dt");
							if (simpleTypeRestriction.BaseTypeName.Namespace == XmlSchemaNamespace)
							{
								writer.WriteString("xs:");
							}
							writer.WriteString(simpleTypeRestriction.BaseTypeName.Name);
							writer.WriteString(" Type");
							writer.WriteEndElement();

							writer.WriteStartElement("dd");
							writer.WriteStartElement("ul");
							writer.WriteStartElement("li");
							foreach (XmlSchemaFacet facet in simpleTypeRestriction.Facets)
							{
								if (facet is XmlSchemaMaxInclusiveFacet)
								{
									writer.WriteString(String.Format("xs:maxInclusive value='{0}'", ((XmlSchemaMaxInclusiveFacet)facet).Value));
								}
								else
								{
									throw new ApplicationException(String.Format("Unknown simple type restriction facet type: '{0}'.", facet.GetType().ToString()));
								}
							}
							writer.WriteEndElement();
							writer.WriteEndElement();
							writer.WriteEndElement();
						}
					}
				}
				else
				{
					throw new ApplicationException(String.Format("Unknown simple type content type: '{0}'.", simpleType.GetType().ToString()));
				}

				// see also
				this.WriteSeeAlso(schema, simpleType, writer);

				// end dl element
				writer.WriteEndElement();

				EndHtmlWriter(writer);
			}
			finally
			{
				if (writer != null)
				{
					writer.Close();
				}
			}
		}

		/// <summary>
		/// Write the documentation file for an element.
		/// </summary>
		/// <param name="schema">The parent schema of the element.</param>
		/// <param name="element">The element.</param>
		/// <param name="elementInfo">Extra information about the element.</param>
		private void WriteElementDoc(XmlSchema schema, XmlSchemaElement element, XmlSchemaElementInfo elementInfo)
		{
			string htmlFile = this.GetSchemaHtmlFile(schema, element.Name);

			XmlTextWriter writer = null;
			try
			{
				string headerClass = (schema == this.mainSchema ? "elementHeader" : "extensionHeader");
				writer = StartHtmlWriter(htmlFile, String.Concat(element.Name, " Element"), headerClass);

				writer.WriteStartElement("dl");
				this.WriteDescription(element, writer);
				this.WriteWIReferences(element, writer);
				this.WriteParents(elementInfo.Parents, writer);
				this.WriteInnerText(element, writer);
				this.WriteChildren(element, writer);
				this.WriteAttributes(element, writer);
				this.WriteRemarks(element, writer);
				this.WriteSeeAlso(schema, element, writer);

				// only write the version number if it is set
				if (this.versionNumber != null)
				{
					this.WriteVersion(writer);
				}

				writer.WriteEndElement();

				EndHtmlWriter(writer);
			}
			finally
			{
				if (writer != null)
				{
					writer.Close();
				}
			}
		}

		/// <summary>
		/// Write the description of the xml schema annotated object.
		/// </summary>
		/// <param name="annotated">The xml schema annotated object.</param>
		/// <param name="writer">The html writer.</param>
		private void WriteDescription(XmlSchemaAnnotated annotated, XmlTextWriter writer)
		{
			string deprecatedDescription = GetDeprecatedDescription(annotated);
			string description = GetDescription(annotated);

			writer.WriteStartElement("dt");
			writer.WriteString("Description");
			writer.WriteEndElement();

			writer.WriteStartElement("dd");
			writer.WriteStartElement("p");
			if (deprecatedDescription != null)
			{
				writer.WriteRaw(deprecatedDescription);
			}
			else if (description != null && description.Length > 0)
			{
				writer.WriteRaw(description);
			}
			else
			{
				writer.WriteString("None");
			}
			writer.WriteEndElement();
			writer.WriteEndElement();
		}

		/// <summary>
		/// Write the Windows Installer references of the xml schema annotated object.
		/// </summary>
		/// <param name="annotated">The xml schema annotated object.</param>
		/// <param name="writer">The html writer.</param>
		private void WriteWIReferences(XmlSchemaAnnotated annotated, XmlTextWriter writer)
		{
			writer.WriteStartElement("dt");
			writer.WriteString("Windows Installer references");
			writer.WriteEndElement();

			writer.WriteStartElement("dd");

			bool first = true;
			if (annotated.Annotation != null)
			{
				foreach (XmlSchemaObject obj in annotated.Annotation.Items)
				{
					XmlSchemaAppInfo appInfo = obj as XmlSchemaAppInfo;

					if (appInfo != null)
					{
						foreach (XmlNode node in appInfo.Markup)
						{
							XmlElement element = node as XmlElement;

							if (element != null && element.LocalName == "msiRef" && element.NamespaceURI == XmlSchemaExtensionNamespace)
							{
								string table = element.GetAttribute("table");

								if (!first)
								{
									writer.WriteString(", ");
								}

								WriteLink(String.Format("http://msdn.microsoft.com/library/default.asp?url=/library/en-us/msi/setup/{0}_table.asp?frame=true&hidetoc=true", table.ToLower()), String.Concat(table, " Table"), null, writer);

								first = false;
							}
						}
					}
				}
			}

			if (first)
			{
				writer.WriteString("None");
			}

			writer.WriteEndElement();
		}

		/// <summary>
		/// Write the parent elements.
		/// </summary>
		/// <param name="parentElements">The parent elements.</param>
		/// <param name="writer">The html writer.</param>
		private void WriteParents(ICollection parentElements, XmlTextWriter writer)
		{
			writer.WriteStartElement("dt");
			writer.WriteString("Parents");
			writer.WriteEndElement();

			writer.WriteStartElement("dd");
			if (parentElements.Count > 0)
			{
				// write the parent elements
				bool first = true;
				foreach (XmlQualifiedName qualifiedName in parentElements)
				{
					if (!first)
					{
						writer.WriteString(", ");
					}
					this.WriteElementLink(qualifiedName, writer);
					first = false;
				}
			}
			else
			{
				writer.WriteString("None");
			}
			writer.WriteEndElement();
		}

		/// <summary>
		/// Write the inner text of the element.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="writer">The html writer.</param>
		private void WriteInnerText(XmlSchemaElement element, XmlTextWriter writer)
		{
			if (element.ElementType is XmlSchemaComplexType)
			{
				XmlSchemaComplexType complexType = (XmlSchemaComplexType)element.ElementType;
				string description = GetDescription(complexType);

				writer.WriteStartElement("dt");

				if (complexType.ContentType == XmlSchemaContentType.Mixed || complexType.ContentType == XmlSchemaContentType.TextOnly)
				{
					if (complexType.ContentModel is XmlSchemaSimpleContent)
					{
						XmlSchemaSimpleContent simpleContent = (XmlSchemaSimpleContent)complexType.ContentModel;

						if (description == null || description.Length == 0)
						{
							description = GetDescription(simpleContent.Content);
						}

						if (simpleContent.Content is XmlSchemaSimpleContentExtension)
						{
							XmlSchemaSimpleContentExtension simpleContentExtension = (XmlSchemaSimpleContentExtension)simpleContent.Content;

							if (simpleContentExtension.BaseTypeName.Namespace == XmlSchemaNamespace)
							{
								writer.WriteString(String.Concat("Inner Text (xs:", simpleContentExtension.BaseTypeName.Name, ")"));
							}
							else
							{
								writer.WriteString(String.Concat("Inner Text (", simpleContentExtension.BaseTypeName.Name, ")"));
							}
						}
					}
					else
					{
						writer.WriteString("Inner Text (xs:string)");
					}
					writer.WriteEndElement();

					writer.WriteStartElement("dd");
					if (description != null && description.Length > 0)
					{
						writer.WriteRaw(description);
					}
					else
					{
						writer.WriteString("This element may have inner text.");
					}
				}
				else
				{
					writer.WriteString("Inner Text");
					writer.WriteEndElement();

					writer.WriteStartElement("dd");
					writer.WriteString("None");
				}

				writer.WriteEndElement();
			}
		}

		/// <summary>
		/// Write the children elements of the element.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="writer">The html writer.</param>
		private void WriteChildren(XmlSchemaElement element, XmlTextWriter writer)
		{
			if (element.ElementType is XmlSchemaComplexType)
			{
				XmlSchemaComplexType complexType = (XmlSchemaComplexType)element.ElementType;

				writer.WriteStartElement("dt");
				writer.WriteString("Children");
				writer.WriteEndElement();

				writer.WriteStartElement("dd");
				if (complexType.Particle != null)
				{
					XmlSchemaElementInfo elementInfo = (XmlSchemaElementInfo)this.elements[element.QualifiedName];

					this.WriteParticle(elementInfo, null, complexType.Particle, writer);
				}
				else
				{
					writer.WriteString("None");
				}
				writer.WriteEndElement();
			}
		}

		/// <summary>
		/// Write the particle.
		/// </summary>
		/// <param name="ownerElementInfo">Extra information about the owner element.</param>
		/// <param name="parentParticle">The parent of the particle being written.</param>
		/// <param name="particle">The particle.</param>
		/// <param name="writer">The html writer.</param>
		private void WriteParticle(XmlSchemaElementInfo ownerElementInfo, XmlSchemaParticle parentParticle, XmlSchemaParticle particle, XmlTextWriter writer)
		{
			if (particle is XmlSchemaAny)
			{
				XmlSchemaAny any = (XmlSchemaAny)particle;

				writer.WriteStartElement("span");
				writer.WriteAttributeString("class", "extension");
				writer.WriteString(String.Format("Any Element namespace='{0}' processContents='{1}'", any.Namespace, any.ProcessContents.ToString()));
				writer.WriteEndElement();

				if (ownerElementInfo.ExtendedChildren.Count > 0)
				{
					writer.WriteStartElement("ul");
					foreach (XmlQualifiedName childQualifiedName in ownerElementInfo.ExtendedChildren)
					{
						writer.WriteStartElement("li");
						this.WriteElementLink(childQualifiedName, writer);
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
				}
			}
			else if (particle is XmlSchemaChoice)
			{
				XmlSchemaChoice choice = (XmlSchemaChoice)particle;

				// sort element children
				SortedList children = new SortedList();
				foreach (XmlSchemaParticle childParticle in choice.Items)
				{
					if (childParticle is XmlSchemaElement)
					{
						children.Add(((XmlSchemaElement)childParticle).QualifiedName.Name, childParticle);
					}
					else if (childParticle is XmlSchemaAny)
					{
						children.Add("ZZZ", childParticle);
					}
					else // sort non-element children by their line number
					{
						children.Add(String.Concat("Z", childParticle.LineNumber.ToString(CultureInfo.InvariantCulture)), childParticle);
					}
				}

				writer.WriteString(String.Format("Choice of elements (min: {0}, max: {1})", (choice.MinOccurs == Decimal.MaxValue ? "unbounded" : choice.MinOccurs.ToString(CultureInfo.InvariantCulture)), (choice.MaxOccurs == Decimal.MaxValue ? "unbounded" : choice.MaxOccurs.ToString(CultureInfo.InvariantCulture))));

				writer.WriteStartElement("ul");
				foreach (XmlSchemaParticle childParticle in children.Values)
				{
					writer.WriteStartElement("li");
					this.WriteParticle(ownerElementInfo, particle, childParticle, writer);
					writer.WriteEndElement();
				}
				writer.WriteEndElement();
			}
			else if (particle is XmlSchemaElement)
			{
				XmlSchemaElement element = (XmlSchemaElement)particle;

				this.WriteElementLink(element.QualifiedName, writer);
				writer.WriteString(" (min: ");
				if (element.MinOccursString != null)
				{
					writer.WriteString(element.MinOccursString);
				}
				else if (parentParticle != null)
				{
					if (parentParticle.MinOccursString != null)
					{
						writer.WriteString(parentParticle.MinOccursString);
					}
					else
					{
						writer.WriteString(parentParticle.MinOccurs.ToString(CultureInfo.InvariantCulture));
					}
				}
				writer.WriteString(", max: ");
				if (element.MaxOccursString != null)
				{
					writer.WriteString(element.MaxOccursString);
				}
				else if (parentParticle != null)
				{
					if (parentParticle.MaxOccursString != null)
					{
						writer.WriteString(parentParticle.MaxOccursString);
					}
					else
					{
						writer.WriteString(parentParticle.MaxOccurs.ToString(CultureInfo.InvariantCulture));
					}
				}
				writer.WriteString(")");

				string description = GetDescription(element);
				if (description != null && description.Length > 0)
				{
					writer.WriteString(": ");
					writer.WriteString(description);
				}
			}
			else if (particle is XmlSchemaSequence)
			{
				XmlSchemaSequence sequence = (XmlSchemaSequence)particle;

				writer.WriteString(String.Format("Sequence (min: {0}, max: {1})", (sequence.MinOccurs == Decimal.MaxValue ? "unbounded" : sequence.MinOccurs.ToString(CultureInfo.InvariantCulture)), (sequence.MaxOccurs == Decimal.MaxValue ? "unbounded" : sequence.MaxOccurs.ToString(CultureInfo.InvariantCulture))));

				writer.WriteStartElement("ol");
				foreach (XmlSchemaParticle childParticle in sequence.Items)
				{
					writer.WriteStartElement("li");
					this.WriteParticle(ownerElementInfo, particle, childParticle, writer);
					writer.WriteEndElement();
				}
				writer.WriteEndElement();
			}
			else
			{
				throw new ApplicationException(String.Format("Unknown particle type: {0}.", particle.GetType().ToString()));
			}
		}

		/// <summary>
		/// Write the attribute of the element.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="writer">The html writer.</param>
		private void WriteAttributes(XmlSchemaElement element, XmlTextWriter writer)
		{
			XmlSchemaAnyAttribute anyAttribute = null;
			XmlSchemaObjectCollection attributes = null;

			if (element.ElementType is XmlSchemaComplexType)
			{
				XmlSchemaComplexType complexType = element.ElementType as XmlSchemaComplexType;

				if (complexType.ContentModel != null)
				{
					if (complexType.ContentModel is XmlSchemaSimpleContent)
					{
						XmlSchemaSimpleContent simpleContent = (XmlSchemaSimpleContent)complexType.ContentModel;

						if (simpleContent.Content != null)
						{
							if (simpleContent.Content is XmlSchemaSimpleContentExtension)
							{
								XmlSchemaSimpleContentExtension simpleContentExtension = (XmlSchemaSimpleContentExtension)simpleContent.Content;

								anyAttribute = simpleContentExtension.AnyAttribute;
								attributes = simpleContentExtension.Attributes;
							}
						}
					}
				}
				else
				{
					anyAttribute = complexType.AnyAttribute;
					attributes = complexType.Attributes;
				}

				writer.WriteStartElement("dt");
				writer.WriteString("Attributes");
				writer.WriteEndElement();

				writer.WriteStartElement("dd");
			}

			if (attributes != null && attributes.Count > 0)
			{
				// attributes table header
				writer.WriteStartElement("table");
				writer.WriteStartElement("tr");
				writer.WriteStartElement("th");
				writer.WriteAttributeString("width", "15%");
				writer.WriteString("Name");
				writer.WriteEndElement();
				writer.WriteStartElement("th");
				writer.WriteAttributeString("width", "15%");
				writer.WriteString("Type");
				writer.WriteEndElement();
				writer.WriteStartElement("th");
				writer.WriteAttributeString("width", "65%");
				writer.WriteString("Description");
				writer.WriteEndElement();
				writer.WriteStartElement("th");
				writer.WriteAttributeString("width", "15%");
				writer.WriteString("Required");
				writer.WriteEndElement();
				writer.WriteEndElement();

				// sort the attributes
				SortedList sortedAttributes = new SortedList();
				foreach (XmlSchemaObject obj in attributes)
				{
					if (obj is XmlSchemaAttribute)
					{
						XmlSchemaAttribute attribute = (XmlSchemaAttribute)obj;

						// always write an "Id" attribute first
						if (attribute.Name == "Id" && attribute.Use == XmlSchemaUse.Required)
						{
							this.WriteAttribute(attribute, writer);
						}
						else
						{
							sortedAttributes.Add(attribute.Name, attribute);
						}
					}
					else if (obj is XmlSchemaAttributeGroupRef)
					{
						XmlSchemaAttributeGroupRef attributeGroupRef = (XmlSchemaAttributeGroupRef)obj;
						XmlSchema schema = this.schemas[attributeGroupRef.RefName.Namespace];
						XmlSchemaAttributeGroup attributeGroup = (XmlSchemaAttributeGroup)schema.AttributeGroups[attributeGroupRef.RefName];

						foreach (XmlSchemaAttribute attribute in attributeGroup.Attributes)
						{
							sortedAttributes.Add(attribute.Name, attribute);
						}
					}
					else
					{
						throw new NotImplementedException(String.Format("Support for '{0}' has not yet been implemented.", obj.GetType().ToString()));
					}
				}

				// write the attributes
				foreach (XmlSchemaAttribute attribute in sortedAttributes.Values)
				{
					this.WriteAttribute(attribute, writer);
				}

				if (anyAttribute != null)
				{
					writer.WriteStartElement("tr");

					writer.WriteStartElement("td");
					writer.WriteAttributeString("colspan", "4");

					writer.WriteStartElement("span");
					writer.WriteAttributeString("class", "extension");
					writer.WriteString(String.Format("Any attribute namespace='{0}' processContents='{1}'", anyAttribute.Namespace, anyAttribute.ProcessContents.ToString().ToLower()));
					writer.WriteEndElement();

					writer.WriteEndElement();

					writer.WriteEndElement();
				}

				writer.WriteEndElement();
			}
			else if (element.ElementType is XmlSchemaComplexType)
			{
				writer.WriteString("None");
			}

			if (element.ElementType is XmlSchemaComplexType)
			{
				writer.WriteEndElement();
			}
		}

		/// <summary>
		/// Write an attribute.
		/// </summary>
		/// <param name="attribute">The attribute.</param>
		/// <param name="writer">The html writer.</param>
		private void WriteAttribute(XmlSchemaAttribute attribute, XmlTextWriter writer)
		{
			string type = "&nbsp;";
			string deprecatedDescription = GetDeprecatedDescription(attribute);
			StringBuilder description = new StringBuilder(GetDescription(attribute));

			if (attribute.SchemaTypeName.Namespace == XmlSchemaNamespace)
			{
				type = Capitalize(attribute.SchemaTypeName.Name);
			}
			else if (!attribute.SchemaTypeName.IsEmpty)
			{
				XmlSchema schema = this.schemas[attribute.SchemaTypeName.Namespace];
				XmlSchemaType schemaType = (XmlSchemaType)schema.SchemaTypes[attribute.SchemaTypeName];

				if (schemaType != null)
				{
					type = String.Format("<a href=\"{0}\">{1}</a>", this.GetSchemaHtmlFileName(schema, String.Concat("simple_type_", schemaType.Name)), Capitalize(schemaType.Name));
				}
			}
			else if (attribute.SchemaType != null)
			{
				if (attribute.SchemaType.Content is XmlSchemaSimpleTypeRestriction)
				{
					XmlSchemaSimpleTypeRestriction simpleTypeRestriction = (XmlSchemaSimpleTypeRestriction)attribute.SchemaType.Content;

					if (simpleTypeRestriction.Facets.Count > 1)
					{
						type = "Enumeration";

						if (description.Length > 0)
						{
							description.Append("  ");
						}

						description.Append("This attribute's value should be one of the following:");

						description.Append("<dl>");
						foreach (XmlSchemaFacet facet in simpleTypeRestriction.Facets)
						{
							description.AppendFormat("<dt class=\"enumerationValue\"><dfn>{0}</dfn></dt>", facet.Value);
							description.AppendFormat("<dd>{0}</dd>", GetDescription(facet));
						}
						description.Append("</dl>");
					}
					else
					{
						type = Capitalize(simpleTypeRestriction.BaseTypeName.Name);

						if (description.Length > 0)
						{
							description.Append("  ");
						}

						description.AppendFormat("Pattern: '{0}'.", ((XmlSchemaFacet)simpleTypeRestriction.Facets[0]).Value);
					}
				}
				else if (attribute.SchemaType.Content is XmlSchemaSimpleTypeList)
				{
					XmlSchemaSimpleTypeList simpleTypeList = (XmlSchemaSimpleTypeList)attribute.SchemaType.Content;

					type = "List";

					if (simpleTypeList.ItemType.Content is XmlSchemaSimpleTypeRestriction)
					{
						XmlSchemaSimpleTypeRestriction simpleTypeRestriction = (XmlSchemaSimpleTypeRestriction)simpleTypeList.ItemType.Content;

						if (simpleTypeRestriction.Facets.Count > 1)
						{
							if (description.Length > 0)
							{
								description.Append("  ");
							}

							description.Append("This attribute's value should be a space-delimited list containg one or more of the following:");

							description.Append("<dl>");
							foreach (XmlSchemaFacet facet in simpleTypeRestriction.Facets)
							{
								description.AppendFormat("<dt class=\"enumerationValue\"><dfn>{0}</dfn></dt>", facet.Value);
								description.AppendFormat("<dd>{0}</dd>", GetDescription(facet));
							}
							description.Append("</dl>");
						}
						else
						{
							throw new NotImplementedException("A simple type with only one facet is not supported here.");
						}
					}
					else
					{
						throw new NotImplementedException(String.Format("The type '{0}' is not supported here.", simpleTypeList.ItemType.Content.GetType().ToString()));
					}
				}
			}

			writer.WriteStartElement("tr");

			writer.WriteStartElement("td");
			writer.WriteString(attribute.Name);
			writer.WriteEndElement();

			writer.WriteStartElement("td");
			writer.WriteRaw(type);
			writer.WriteEndElement();

			writer.WriteStartElement("td");
			if (deprecatedDescription != null)
			{
				writer.WriteRaw(deprecatedDescription);
			}
			else if (description.Length > 0)
			{
				writer.WriteRaw(description.ToString());
			}
			else
			{
				writer.WriteRaw("&nbsp;");
			}
			writer.WriteEndElement();

			writer.WriteStartElement("td");
			if (attribute.Use == XmlSchemaUse.Required)
			{
				writer.WriteString("Yes");
			}
			else
			{
				writer.WriteRaw("&nbsp;");
			}
			writer.WriteEndElement();

			writer.WriteEndElement();
		}

		/// <summary>
		/// Write the remarks of the xml schema annotated object.
		/// </summary>
		/// <param name="annotated">The xml schema annotated object.</param>
		/// <param name="writer">The html writer.</param>
		private void WriteRemarks(XmlSchemaAnnotated annotated, XmlTextWriter writer)
		{
			if (annotated.Annotation != null)
			{
				foreach (XmlSchemaObject obj in annotated.Annotation.Items)
				{
					XmlSchemaAppInfo appInfo = obj as XmlSchemaAppInfo;

					if (appInfo != null)
					{
						foreach (XmlNode node in appInfo.Markup)
						{
							XmlElement element = node as XmlElement;

							if (element != null && element.LocalName == "remarks" && element.NamespaceURI == XmlSchemaExtensionNamespace)
							{
								writer.WriteStartElement("dt");
								writer.WriteString("Remarks");
								writer.WriteEndElement();

								writer.WriteStartElement("dd");
								writer.WriteRaw(htmlPrefix.Replace(element.InnerXml.Trim().Replace("\t", String.Empty).Replace(Environment.NewLine, " "), String.Empty));
								writer.WriteEndElement();
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Write the see also of the xml schema annotated object.
		/// </summary>
		/// <param name="schema">The parent schema of the xml schema annotated object.</param>
		/// <param name="annotated">The xml schema annoated object.</param>
		/// <param name="writer">The html writer.</param>
		private void WriteSeeAlso(XmlSchema schema, XmlSchemaAnnotated annotated, XmlTextWriter writer)
		{
			string schemaName = GetSchemaName(schema);

			writer.WriteStartElement("dt");
			writer.WriteString("See Also");
			writer.WriteEndElement();

			writer.WriteStartElement("dd");
			WriteLink(this.GetSchemaHtmlFileName(schema, "index"), String.Concat(schemaName, " Schema"), null, writer);

			// retrieve the SeeAlso nodes
			if (annotated.Annotation != null)
			{
				foreach (XmlSchemaObject obj in annotated.Annotation.Items)
				{
					XmlSchemaAppInfo appInfo = obj as XmlSchemaAppInfo;

					if (appInfo != null)
					{
						foreach (XmlNode node in appInfo.Markup)
						{
							XmlElement element = node as XmlElement;

							if (element != null && element.LocalName == "seeAlso" && element.NamespaceURI == XmlSchemaExtensionNamespace)
							{
								string reference = element.GetAttribute("ref");

								writer.WriteString(", ");
								this.WriteElementLink(new XmlQualifiedName(reference, schema.TargetNamespace), writer);
							}
						}
					}
				}
			}

			writer.WriteEndElement();
		}

		/// <summary>
		/// Write the version.
		/// </summary>
		/// <param name="writer">The html writer.</param>
		private void WriteVersion(XmlTextWriter writer)
		{
			writer.WriteStartElement("div");
			writer.WriteAttributeString("id", "footer");

			writer.WriteString("Version ");
			writer.WriteString(this.versionNumber);

			writer.WriteEndElement();
		}

		/// <summary>
		/// Gets the file name to use for an html file for a particular schema.
		/// </summary>
		/// <param name="schema">The schema generating the html file.</param>
		/// <param name="suffix">The file name suffix to append.</param>
		/// <returns>The html file name.</returns>
		private string GetSchemaHtmlFileName(XmlSchema schema, string suffix)
		{
			string schemaName = GetSchemaName(schema);

			return String.Format("{0}_xsd_{1}.htm", schemaName.ToLower(), suffix.ToLower());
		}

		/// <summary>
		/// Gets the path on disk for a new html file for a particular schema.
		/// </summary>
		/// <param name="schema">The schema generating the html file.</param>
		/// <param name="suffix">The file name suffix to append.</param>
		/// <returns>The path to the html file.</returns>
		private string GetSchemaHtmlFile(XmlSchema schema, string suffix)
		{
			string htmlDir = Path.Combine(this.outputDir, "html");

			return Path.Combine(htmlDir, this.GetSchemaHtmlFileName(schema, suffix));
		}

		/// <summary>
		/// Writes a link for an element to the html writer.
		/// </summary>
		/// <param name="qualifiedName">The qualified name of the element.</param>
		/// <param name="writer">The html writer.</param>
		private void WriteElementLink(XmlQualifiedName qualifiedName, XmlTextWriter writer)
		{
			XmlSchema schema = this.schemas[qualifiedName.Namespace];
			string cssClass = (schema != this.mainSchema ? "extension" : null);

			WriteLink(this.GetSchemaHtmlFileName(schema, qualifiedName.Name), qualifiedName.Name, cssClass, writer);
		}

		/// <summary>
		/// Gets a link for an element.
		/// </summary>
		/// <param name="qualifiedName">The qualified name of the element.</param>
		private string GetElementLink(XmlQualifiedName qualifiedName)
		{
			XmlSchema schema = this.schemas[qualifiedName.Namespace];
			string cssClass = (schema != this.mainSchema ? "extension" : null);

			if (cssClass != null)
			{
				return String.Format("<a href=\"{0}\" class=\"{1}\">{2}</a>", this.GetSchemaHtmlFileName(schema, qualifiedName.Name), cssClass, qualifiedName.Name);
			}
			else
			{
				return String.Format("<a href=\"{0}\">{1}</a>", this.GetSchemaHtmlFileName(schema, qualifiedName.Name), qualifiedName.Name);
			}
		}

		/// <summary>
		/// If the schema annotated item is deprecated, get its deprecation description.
		/// </summary>
		/// <param name="annotated">The annotated item.</param>
		/// <returns>The deprecation description if the item is deprecated; null otherwise.</returns>
		private string GetDeprecatedDescription(XmlSchemaAnnotated annotated)
		{
			if (annotated.Annotation == null)
			{
				return null;
			}

			foreach (XmlSchemaObject obj in annotated.Annotation.Items)
			{
				if (obj is XmlSchemaAppInfo)
				{
					XmlSchemaAppInfo appInfo = (XmlSchemaAppInfo)obj;

					foreach (XmlNode node in appInfo.Markup)
					{
						XmlElement element = node as XmlElement;

						if (element != null && element.LocalName == "deprecated" && element.NamespaceURI == XmlSchemaExtensionNamespace)
						{
							string newNamespace = element.GetAttribute("namespace");
							string newReference = element.GetAttribute("ref");

							if (newReference != null && 0 < newReference.Length)
							{
								if (annotated is XmlSchemaAttribute)
								{
									return String.Format("This attribute has been deprecated; please use the {0} attribute instead.", newReference);
								}
								else if (annotated is XmlSchemaElement)
								{
									XmlSchemaElement schemaElement = (XmlSchemaElement)annotated;

									if (newNamespace.Length == 0)
									{
										newNamespace = schemaElement.QualifiedName.Namespace;
									}

									return String.Format("This element has been deprecated; please use the {0} element instead.", this.GetElementLink(new XmlQualifiedName(newReference, newNamespace)));
								}
								else
								{
									throw new InvalidOperationException(String.Format("Unsupported deprecated element found inside '{0}'.", annotated.GetType().ToString()));
								}
							}
							else
							{
								if (annotated is XmlSchemaAttribute)
								{
									return "This attribute has been deprecated.";
								}
								else if (annotated is XmlSchemaElement)
								{
									return "This element has been deprecated.";
								}
								else
								{
									throw new InvalidOperationException(String.Format("Unsupported deprecated element found inside '{0}'.", annotated.GetType().ToString()));
								}
							}
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Extra information about a schema element.
		/// </summary>
		private class XmlSchemaElementInfo
		{
			private SortedList extendedChildren;
			private bool indexed;
			private SortedList parents;

			/// <summary>
			/// Instantiates a new XmlSchemaElementInfo class.
			/// </summary>
			public XmlSchemaElementInfo()
			{
				this.extendedChildren = new SortedList();
				this.parents = new SortedList();
			}

			/// <summary>
			/// Gets the extended children of the element.
			/// </summary>
			/// <value>The extended children of the element.</value>
			public ICollection ExtendedChildren
			{
				get { return this.extendedChildren.Values; }
			}

			/// <summary>
			/// Gets or sets the indexed state of the element.
			/// </summary>
			/// <value>The indexed state of the element.</value>
			public bool Indexed
			{
				get { return this.indexed; }
				set { this.indexed = value; }
			}

			/// <summary>
			/// Gets the parents of the element.
			/// </summary>
			/// <value>The parents of the element.</value>
			public ICollection Parents
			{
				get { return this.parents.Values; }
			}

			/// <summary>
			/// Adds an extended child element to the element.
			/// </summary>
			/// <param name="childQualifiedName">The qualified name of the extended child element.</param>
			public void AddExtendedChild(XmlQualifiedName childQualifiedName)
			{
				this.extendedChildren.Add(GetKey(childQualifiedName), childQualifiedName);
			}

			/// <summary>
			/// Adds a parent element to the element.
			/// </summary>
			/// <param name="parentQualifiedName">The qualified name of the parent element.</param>
			public void AddParent(XmlQualifiedName parentQualifiedName)
			{
				string key = GetKey(parentQualifiedName);

				if (!this.parents.Contains(key))
				{
					this.parents.Add(key, parentQualifiedName);
				}
			}

			/// <summary>
			/// Gets the key for storing parent and child information.
			/// </summary>
			/// <param name="qualifiedName">The qualified name used to generate the key.</param>
			/// <returns>The key to store parent and child information.</returns>
			private static string GetKey(XmlQualifiedName qualifiedName)
			{
				return String.Concat(qualifiedName.Name, ",", qualifiedName.Namespace);
			}
		}
	}
}
