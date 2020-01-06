// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SIL.FieldWorks.Common.FwUtils
{
#if RANDYTODO
	// TODO: Integrate this classes' methods with Palaso's SIL.Core.Xml.XmlSerializationHelper
#endif
	public static class XmlSerializationHelper
	{
		#region XmlScrTextReader class
		/// <summary>
		/// Custom XmlTextReader that can preserve whitespace characters (spaces, tabs, etc.)
		/// that are in XML elements. This allows us to properly handle deserialization of
		/// paragraph runs that contain runs that contain only whitespace characters.
		/// </summary>
		private sealed class XmlScrTextReader : XmlTextReader
		{
			private bool m_fKeepWhitespaceInElements;

			/// <summary />
			/// <param name="reader">The stream reader.</param>
			/// <param name="fKeepWhitespaceInElements">if set to <c>true</c>, the reader
			/// will preserve and return elements that contain only whitespace, otherwise
			/// these elements will be ignored during a deserialization.</param>
			internal XmlScrTextReader(TextReader reader, bool fKeepWhitespaceInElements)
				: base(reader)
			{
				m_fKeepWhitespaceInElements = fKeepWhitespaceInElements;
			}

			/// <summary />
			/// <param name="filename">The filename.</param>
			/// <param name="fKeepWhitespaceInElements">if set to <c>true</c>, the reader
			/// will preserve and return elements that contain only whitespace, otherwise
			/// these elements will be ignored during a deserialization.</param>
			internal XmlScrTextReader(string filename, bool fKeepWhitespaceInElements)
				: base(new StreamReader(filename))
			{
				m_fKeepWhitespaceInElements = fKeepWhitespaceInElements;
			}

			/// <summary>
			/// Gets the namespace URI (as defined in the W3C Namespace specification) of the
			/// node on which the reader is positioned.
			/// </summary>
			/// <returns>The namespace URI of the current node; otherwise an empty string.</returns>
			public override string NamespaceURI => string.Empty;

			/// <summary>
			/// Reads the next node from the stream.
			/// </summary>
			public override bool Read()
			{
				// Since we use this class only for deserialization, catch file not found
				// exceptions for the case when the XML file contains a !DOCTYPE declearation
				// and the specified DTD file is not found. (This is because the base class
				// attempts to open the DTD by merely reading the !DOCTYPE node from the
				// current directory instead of relative to the XML document location.)
				try
				{
					return base.Read();
				}
				catch (FileNotFoundException)
				{
					return true;
				}
			}

			/// <summary>
			/// Gets the type of the current node.
			/// </summary>
			public override XmlNodeType NodeType
			{
				get
				{
					if (m_fKeepWhitespaceInElements && (base.NodeType == XmlNodeType.Whitespace || base.NodeType == XmlNodeType.SignificantWhitespace)
					                                && Value.IndexOf('\n') < 0 && Value.Trim().Length == 0)
					{
						// We found some whitespace that was most
						// likely whitespace we want to keep.
						return XmlNodeType.Text;
					}

					return base.NodeType;
				}
			}
		}

		#endregion

		#region Methods for XML serializing and deserializing data

#if RANDYTODO
		// TODO: Move this to Palaso's SIL.Core.Xml.XmlSerializationHelper
		// TODO: Be sure to not expect the "T" methods to serve in all cases.
#endif
		/// <summary>
		/// Deserialize the given xml string into an object of targetType class
		/// </summary>
		public static object DeserializeXmlString(string xml, Type targetType)
		{
			// TODO-Linux: System.Boolean System.Type::op_{Ine,E}quality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			if (string.IsNullOrEmpty(xml) || targetType == null)
			{
				return null;
			}
			using (var stream = new MemoryStream())
			using (var textWriter = new XmlTextWriter(stream, Encoding.UTF8))
			{
				var doc = new XmlDocument();
				doc.LoadXml(xml);
				// get the type from the xml itself.
				// if we can find an existing class/type, we can try to deserialize to it.
				if (targetType != null)
				{
					doc.WriteContentTo(textWriter);
					textWriter.Flush();
					stream.Seek(0, SeekOrigin.Begin);
					var xmlSerializer = new XmlSerializer(targetType);
					try
					{
						return xmlSerializer.Deserialize(stream);
					}
					catch
					{
						// something went wrong trying to deserialize the xml
						// perhaps the structure of the stored data no longer matches the class
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Serializes an object to an XML string.
		/// </summary>
		public static string SerializeToString<T>(T data)
		{
			try
			{
				var output = new StringBuilder();
				using (var writer = new StringWriter(output))
				{
					var nameSpace = new XmlSerializerNamespaces();
					nameSpace.Add(string.Empty, string.Empty);
					var serializer = new XmlSerializer(typeof(T));
					serializer.Serialize(writer, data, nameSpace);
					writer.Close();
				}
				return (output.Length == 0 ? null : output.ToString());
			}
			catch (Exception e)
			{
				Debug.Fail(e.Message);
			}

			return null;
		}

		/// <summary>
		/// Serializes an object to the specified file.
		/// </summary>
		public static bool SerializeToFile<T>(string filename, T data)
		{
			try
			{
				using (TextWriter writer = new StreamWriter(filename))
				{
					var nameSpace = new XmlSerializerNamespaces();
					nameSpace.Add(string.Empty, string.Empty);
					var serializer = new XmlSerializer(typeof(T));
					serializer.Serialize(writer, data, nameSpace);
					writer.Close();
					return true;
				}
			}
			catch (Exception e)
			{
				Debug.Fail(e.Message);
			}

			return false;
		}

		/// <summary>
		/// Deserializes XML from the specified string to an object of the specified type.
		/// </summary>
		public static T DeserializeFromString<T>(string input) where T : class
		{
			Exception e;
			return (DeserializeFromString<T>(input, out e));
		}

		/// <summary>
		/// Deserializes XML from the specified string to an object of the specified type.
		/// </summary>
		public static T DeserializeFromString<T>(string input, bool fKeepWhitespaceInElements)
			where T : class
		{
			Exception e;
			return (DeserializeFromString<T>(input, fKeepWhitespaceInElements, out e));
		}

		/// <summary>
		/// Deserializes XML from the specified string to an object of the specified type.
		/// </summary>
		public static T DeserializeFromString<T>(string input, out Exception e) where T : class
		{
			return DeserializeFromString<T>(input, false, out e);
		}

		/// <summary>
		/// Deserializes XML from the specified string to an object of the specified type.
		/// </summary>
		public static T DeserializeFromString<T>(string input, bool fKeepWhitespaceInElements, out Exception e) where T : class
		{
			T data = null;
			e = null;
			try
			{
				if (string.IsNullOrEmpty(input))
				{
					return null;
				}
				// Whitespace is not allowed before the XML declaration,
				// so get rid of any that exists.
				input = input.TrimStart();
				using (var reader = new XmlScrTextReader(new StringReader(input), fKeepWhitespaceInElements))
				{
					data = DeserializeInternal<T>(reader);
				}
			}
			catch (Exception outEx)
			{
				e = outEx;
			}

			return data;
		}

		/// <summary>
		/// Deserializes XML from the specified file to an object of the specified type.
		/// </summary>
		/// <typeparam name="T">The object type</typeparam>
		/// <param name="filename">The filename from which to load</param>
		public static T DeserializeFromFile<T>(string filename) where T : class
		{
			Exception e;
			return DeserializeFromFile<T>(filename, false, out e);
		}

		/// <summary>
		/// Deserializes XML from the specified file to an object of the specified type.
		/// </summary>
		/// <typeparam name="T">The object type</typeparam>
		/// <param name="filename">The filename from which to load</param>
		/// <param name="fKeepWhitespaceInElements">if set to <c>true</c>, the reader
		/// will preserve and return elements that contain only whitespace, otherwise
		/// these elements will be ignored during a deserialization.</param>
		/// <param name="e">The exception generated during the deserialization.</param>
		public static T DeserializeFromFile<T>(string filename, bool fKeepWhitespaceInElements, out Exception e) where T : class
		{
			T data = null;
			e = null;
			try
			{
				if (!File.Exists(filename))
				{
					return null;
				}
				using (var reader = new XmlScrTextReader(filename, fKeepWhitespaceInElements))
				{
					data = DeserializeInternal<T>(reader);
				}
			}
			catch (Exception outEx)
			{
				e = outEx;
			}

			return data;
		}

		/// <summary>
		/// Deserializes an object using the specified reader.
		/// </summary>
		/// <typeparam name="T">The type of object to deserialize</typeparam>
		/// <param name="reader">The reader.</param>
		/// <returns>The deserialized object</returns>
		private static T DeserializeInternal<T>(XmlScrTextReader reader)
		{
			var deserializer = new XmlSerializer(typeof(T));
			deserializer.UnknownAttribute += deserializer_UnknownAttribute;
			return (T)deserializer.Deserialize(reader);
		}

		/// <summary>
		/// Handles the UnknownAttribute event of the deserializer control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Xml.Serialization.XmlAttributeEventArgs"/>
		/// instance containing the event data.</param>
		private static void deserializer_UnknownAttribute(object sender, XmlAttributeEventArgs e)
		{
			if (e.Attr.LocalName == "lang")
			{
				// This is special handling for the xml:lang attribute that is used to specify
				// the WS for the current paragraph, run in a paragraph, etc. The XmlTextReader
				// treats xml:lang as a special case and basically skips over it (but it does
				// set the current XmlLang to the specified value). This keeps the deserializer
				// from getting xml:lang as an attribute which keeps us from getting these values.
				// The fix for this is to look at the object that is being deserialized and,
				// using reflection, see if it has any fields that have an XmlAttribute looking
				// for the xml:lang and setting it to the value we get here. (TE-8328)
				var obj = e.ObjectBeingDeserialized;
				var type = obj.GetType();
				foreach (var field in type.GetFields())
				{
					var bla = field.GetCustomAttributes(typeof(XmlAttributeAttribute), false);
					if (bla.Length == 1 && ((XmlAttributeAttribute)bla[0]).AttributeName == "xml:lang")
					{
						field.SetValue(obj, e.Attr.Value);
					}
				}
			}
		}
		#endregion
	}
}