//-------------------------------------------------------------------------------------------------
// <copyright file="ProjectFileXmlWriter.cs" company="Microsoft">
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
// Contains the ProjectFileXmlWriter class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;
	using System.IO;
	using System.Text;
	using System.Xml;

	using Microsoft.VisualStudio.Shell.Interop;

	/// <summary>
	/// Provides a custom XML writer designed to have element attributes on separate lines and
	/// indented to match existing Visual Studio projects.
	/// </summary>
	public sealed class ProjectFileXmlWriter
	{
		#region Member Variables
		//=========================================================================================
		// Member Variables
		//=========================================================================================

		private static readonly Type classType = typeof(ProjectFileXmlWriter);

		private Stack elementStack = new Stack();
		private char indentChar = ' ';
		private int indentation = 4;
		private char quoteChar = '"';
		private TextWriter writer;
		#endregion

		#region Constructors
		//=========================================================================================
		// Constructors
		//=========================================================================================

		public ProjectFileXmlWriter(TextWriter writer)
		{
			this.writer = writer;
		}
		#endregion

		#region Properties
		//=========================================================================================
		// Properties
		//=========================================================================================

		public char IndentChar
		{
			get { return this.indentChar; }
			set { this.indentChar = value; }
		}

		public int Indentation
		{
			get { return this.indentation; }
			set { this.indentation = value; }
		}

		public char QuoteChar
		{
			get { return this.quoteChar; }
			set { this.quoteChar = value; }
		}

		private ElementInfo CurrentElement
		{
			get { return (ElementInfo)this.elementStack.Peek(); }
		}

		private string IndentSpaces
		{
			get { return new string(this.IndentChar, this.Indentation * this.Level); }
		}

		private int Level
		{
			get { return this.elementStack.Count; }
		}
		#endregion

		#region Methods
		//=========================================================================================
		// Methods
		//=========================================================================================

		public void Flush()
		{
			this.writer.Flush();
		}

		public void WriteAttributeString(string name, string value)
		{
			Tracer.Assert(this.elementStack.Count > 0, "How can we be writing attributes when we haven't written a starting element yet: {0}={1}", name, value);

			// Write each attribute on a new line.
			this.writer.WriteLine();
			this.writer.Write(this.IndentSpaces);
			this.writer.Write("{0}={1}{2}{1}", name, this.QuoteChar, value);

			// Remember that we've written some attributes on the current element.
			this.CurrentElement.HasAttributes = true;
		}

		public void WriteAttributeString(string name, bool value)
		{
			this.WriteAttributeString(name, (value ? "true" : "false"));
		}

		public void WriteAttributeString(string name, int value)
		{
			this.WriteAttributeString(name, value.ToString());
		}

		public void WriteEndDocument()
		{
			// Close all of the remaining tags.
			while (this.elementStack.Count > 0)
			{
				this.WriteEndElement();
			}
		}

		public void WriteEndElement()
		{
			Tracer.Assert(this.Level > 0, "The WriteStartElement and WriteEndElement calls are unbalanced. Current level = {0}", this.Level);
			if (this.elementStack.Count > 0)
			{
				ElementInfo element = (ElementInfo)this.elementStack.Pop();
				if (element.HasContent)
				{
					this.writer.WriteLine("{0}</{1}>", this.IndentSpaces, element.Name);
				}
				else
				{
					if (element.HasAttributes)
					{
						this.writer.WriteLine();
					}
					this.writer.WriteLine("{0}/>", (element.HasAttributes ? this.IndentSpaces : " "));
				}
			}
		}

		/// <summary>
		/// Writes the XML declaration (&lt;?xml version="1.0" encoding="UTF-8" ?&gt;).
		/// </summary>
		public void WriteStartDocument()
		{
			this.writer.WriteLine("<?xml version={0}1.0{0} encoding={0}{1}{0} ?>", this.QuoteChar, this.writer.Encoding.WebName);
		}

		public void WriteStartElement(string name)
		{
			// If we were writing the parent element, then we have to add a closing >.
			if (this.elementStack.Count > 0 && !this.CurrentElement.HasContent)
			{
				if (this.CurrentElement.HasAttributes)
				{
					this.writer.WriteLine();
					// Pop the top element long enough to write the closing > with the correct indentation.
					object top = this.elementStack.Pop();
					this.writer.Write(this.IndentSpaces);
					this.elementStack.Push(top);
				}
				this.writer.WriteLine(">");
				this.CurrentElement.HasContent = true;
			}

			// Write the starting <elementName
			this.writer.Write("{0}<{1}", this.IndentSpaces, name);

			// Push the new element onto the stack.
			ElementInfo element = new ElementInfo(name);
			this.elementStack.Push(element);
		}
		#endregion

		#region Classes
		//=========================================================================================
		// Classes
		//=========================================================================================

		private class ElementInfo
		{
			private string name;
			private bool hasAttributes;
			private bool hasContent;

			public ElementInfo(string name)
			{
				this.name = name;
			}

			public string Name
			{
				get { return this.name; }
			}

			public bool HasAttributes
			{
				get { return this.hasAttributes; }
				set { this.hasAttributes = value; }
			}

			public bool HasContent
			{
				get { return this.hasContent; }
				set { this.hasContent = value; }
			}
		}
		#endregion
	}
}
