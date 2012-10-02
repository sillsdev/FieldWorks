using System;
using System.Xml;
using System.Xml.Linq;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;
using System.Collections.Generic;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// <summary>
	/// Read/Write helper class for load/save CmObjects.
	/// </summary>
	internal static class ReadWriteServices
	{
		/// <summary>
		/// Common code for loading a MultiUnicodeAccessor.
		/// </summary>
		internal static void LoadMultiUnicodeAccessor(ICmObject obj, int flid, XElement reader,
			ref IMultiUnicode multiUnicodeProperty,
			ILgWritingSystemFactory wsf, ITsStrFactory tsf)
		{
			if (obj == null) throw new ArgumentNullException("obj");
			if (reader == null) throw new ArgumentNullException("reader");

			// Deal with MultiUnicode data type.
			multiUnicodeProperty = new MultiUnicodeAccessor(obj, flid);
			((MultiAccessor)multiUnicodeProperty).LoadFromDataStoreInternal(reader, wsf, tsf);
		}

		/// <summary>
		/// Common code for loading a MultiStringAccessor.
		/// </summary>
		internal static void LoadMultiStringAccessor(ICmObject obj, int flid, XElement reader,
			ref IMultiString multiStringProperty,
			ILgWritingSystemFactory wsf, ITsStrFactory tsf)
		{
			if (obj == null) throw new ArgumentNullException("obj");
			if (reader == null) throw new ArgumentNullException("reader");

			// Deal with MultiUnicode data type.
			multiStringProperty = new MultiStringAccessor(obj, flid);
			((MultiAccessor)multiStringProperty).LoadFromDataStoreInternal(reader, wsf, tsf);
		}

		/// <summary>
		/// Load plain C# string. Interpreset XML entities as the appropriate characters.
		/// </summary>
		internal static string LoadUnicodeString(XElement reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			return Icu.Normalize(reader.Element("Uni").Value, Icu.UNormalizationMode.UNORM_NFD);	// return NFD.
		}

		/// <summary>
		/// Load Text Prop property.
		/// </summary>
		internal static ITsTextProps LoadTextPropBinary(XElement reader, ILgWritingSystemFactory wsf)
		{
			if (!reader.HasElements)
				return null;
			return StringUtils.GetTextProps((XElement)reader.FirstNode, wsf);
		}

		/// <summary>
		/// Load an integer.
		/// </summary>
		internal static int LoadInteger(XElement reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			return Int32.Parse(reader.Attribute("val").Value);
		}

		/// <summary>
		/// Load a byte array.
		/// </summary>
		/// <returns>Null or a byte array for the property.</returns>
		internal static byte[] LoadByteArray(XElement reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			byte[] retval = null;
			var byteArray = reader.Element("Binary").Value;
			if (byteArray.Length > 0)
			{
				var tokens = byteArray.Split('.');
				retval = new byte[tokens.Length];
				for (var i = 0; i < tokens.Length; ++i)
				{
					byte b;
					Byte.TryParse(tokens[i], out b);
					retval[i] = b;
				}
			}
			return retval;
		}

		/// <summary>
		/// Load a boolean.
		/// </summary>
		internal static bool LoadBoolean(XElement reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			return bool.Parse(reader.Attribute("val").Value);
		}

		/// <summary>
		/// Load a Guid.
		/// </summary>
		internal static Guid LoadGuid(XElement reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			return new Guid(reader.Attribute("val").Value);
		}

		/// <summary>
		/// Load a DateTime property.
		/// </summary>
		internal static DateTime LoadDateTime(XElement reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			var dtParts = reader.Attribute("val").Value.Split(new[] { '-', ' ', ':', '.' });
			var asUtc = new DateTime(
				Int32.Parse(dtParts[0]),
				Int32.Parse(dtParts[1]),
				Int32.Parse(dtParts[2]),
				Int32.Parse(dtParts[3]),
				Int32.Parse(dtParts[4]),
				Int32.Parse(dtParts[5]),
				Int32.Parse(dtParts[6]));
			return asUtc.ToLocalTime(); // Return local time, not UTC, which is what is stored.
		}

		/// <summary>
		/// Load a GenDate.
		/// </summary>
		internal static GenDate LoadGenDate(XElement reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			var genDateStr = reader.Attribute("val").Value;
			if (!string.IsNullOrEmpty(genDateStr) && Convert.ToInt32(genDateStr) != 0)
			{
				var ad = true;
				if (genDateStr.StartsWith("-"))
				{
					ad = false;
					genDateStr = genDateStr.Substring(1);
				}
				genDateStr = genDateStr.PadLeft(9, '0');
				var year = Convert.ToInt32(genDateStr.Substring(0, 4));
				var month = Convert.ToInt32(genDateStr.Substring(4, 2));
				var day = Convert.ToInt32(genDateStr.Substring(6, 2));
				var precision = (GenDate.PrecisionType)Convert.ToInt32(genDateStr.Substring(8, 1));
				return new GenDate(precision, month, day, year, ad);
			}

			return new GenDate();
		}

		/// <summary>
		/// Read the guid from the 'objsur' element.
		/// </summary>
		internal static ICmObjectId LoadAtomicObjectProperty(XElement reader, ICmObjectIdFactory factory)
		{
			if (reader == null)
				return null;
			if (reader.Name.LocalName != "objsur")
				throw new ArgumentException("Wrong level of xml element.");

			return factory.FromGuid(new Guid(reader.Attribute("guid").Value));
		}

		/// <summary>
		/// Write out MultiUnicode or MultiString property.
		/// </summary>
		internal static void WriteMultiFoo(XmlWriter writer, string elementName, MultiAccessor multiProperty)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (string.IsNullOrEmpty(elementName)) throw new ArgumentNullException("elementName");

			if (multiProperty == null || multiProperty.StringCount <= 0) return;

			writer.WriteStartElement(elementName); // Open prop. element.
			multiProperty.ToXMLString(writer);
			writer.WriteEndElement(); // Close prop. element.
		}

		/// <summary>
		/// Write out a byte array property.
		/// </summary>
		internal static void WriteByteArray(XmlWriter writer, string elementName, byte[] dataProperty)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (string.IsNullOrEmpty(elementName)) throw new ArgumentNullException("elementName");

			if (dataProperty == null || dataProperty.Length == 0) return;

			writer.WriteStartElement(elementName); // Open prop. element.
			var byteArray = "";
			foreach (var val in dataProperty)
			{
				if (byteArray.Length > 0)
					byteArray += ".";
				byteArray += val.ToString();
			}
			writer.WriteStartElement("Binary"); // Open Binary element.
			writer.WriteString(byteArray);
			writer.WriteEndElement(); // Close Binary element.
			writer.WriteEndElement(); // Close prop. element.
		}

		/// <summary>
		/// Write a GenDate value to an element.
		/// </summary>
		/// <param name="writer">The writer.</param>
		/// <param name="elementName">Name of the element.</param>
		/// <param name="dataProperty">The GenDate.</param>
		internal static void WriteGenDate(XmlWriter writer, string elementName, GenDate dataProperty)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (string.IsNullOrEmpty(elementName)) throw new ArgumentNullException("elementName");

			writer.WriteStartElement(elementName);
			WriteGenDateAttribute(writer, "val", dataProperty);
			writer.WriteEndElement();
		}

		/// <summary>
		/// Writes a GenDate value to an attribute.
		/// </summary>
		/// <param name="writer">The writer.</param>
		/// <param name="attrName">Name of the attribute.</param>
		/// <param name="dataProperty">The GenDate.</param>
		internal static void WriteGenDateAttribute(XmlWriter writer, string attrName, GenDate dataProperty)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (string.IsNullOrEmpty(attrName)) throw new ArgumentNullException("attrName");

			var genDateStr = "0";
			if (!dataProperty.IsEmpty)
			{
				genDateStr = string.Format("{0}{1:0000}{2:00}{3:00}{4}", dataProperty.IsAD ? "" : "-", dataProperty.Year,
					dataProperty.Month, dataProperty.Day, (int)dataProperty.Precision);
			}
			writer.WriteAttributeString(attrName, genDateStr);
		}

		/// <summary>
		/// Write a DateTime value
		/// </summary>
		internal static void WriteDateTime(XmlWriter writer, string elementName, DateTime dataProperty)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (string.IsNullOrEmpty(elementName)) throw new ArgumentNullException("elementName");

			writer.WriteStartElement(elementName); // Open prop. element.
			writer.WriteAttributeString("val", FormatDateTime(dataProperty));
			writer.WriteEndElement(); // Close prop. element.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Formats the date time (converted to UTC).
		/// </summary>
		/// <param name="dataProperty">The date/time to format -- will be converted to Universal
		/// time.</param>
		/// <returns>A date/time string in a format suitable for parsing by LoadDateTime</returns>
		/// ------------------------------------------------------------------------------------
		internal static string FormatDateTime(DateTime dataProperty)
		{
			dataProperty = dataProperty.ToUniversalTime(); // Store it as UTC.
			return String.Format("{0}-{1}-{2} {3}:{4}:{5}.{6}",
				dataProperty.Year,
				dataProperty.Month,
				dataProperty.Day,
				dataProperty.Hour,
				dataProperty.Minute,
				dataProperty.Second,
				dataProperty.Millisecond);
		}

		internal static void WriteGuid(XmlWriter writer, string elementName, Guid dataProperty)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (string.IsNullOrEmpty(elementName)) throw new ArgumentNullException("elementName");

			writer.WriteStartElement(elementName); // Open prop. element.
			writer.WriteAttributeString("val", dataProperty.ToString().ToLowerInvariant());
			writer.WriteEndElement(); // Close prop. element.
		}

		/// <summary>
		/// Write other value data type values.
		/// </summary>
		internal static void WriteOtherValueTypeData(XmlWriter writer, string elementName, string dataProperty)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (string.IsNullOrEmpty(elementName)) throw new ArgumentNullException("elementName");

			writer.WriteStartElement(elementName); // Open prop. element.
			writer.WriteAttributeString("val", dataProperty);
			writer.WriteEndElement(); // Close prop. element.
		}

		/// <summary>
		/// Write ordinary C# string. Angle brackets and ampersand are converted to XML entities.
		/// </summary>
		internal static void WriteUnicodeString(XmlWriter writer, string elementName, string propertyData)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (string.IsNullOrEmpty(elementName)) throw new ArgumentNullException("elementName");

			if (string.IsNullOrEmpty(propertyData)) return;

			writer.WriteStartElement(elementName); // Open prop. element.
			writer.WriteStartElement("Uni"); // Open Uni element.
			writer.WriteString(Icu.Normalize(propertyData, Icu.UNormalizationMode.UNORM_NFC)); // Store NFC.
			writer.WriteEndElement(); // Close Uni element.
			writer.WriteEndElement(); // Close prop. element.
		}

		/// <summary>
		/// Write ITsString property.
		/// </summary>
		internal static void WriteITsString(XmlWriter writer, FdoCache cache, string elementName, ITsString propertyData)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (cache == null) throw new ArgumentNullException("cache");
			if (string.IsNullOrEmpty(elementName)) throw new ArgumentNullException("elementName");

			if (propertyData == null) return;

			writer.WriteStartElement(elementName); // Open prop. element.
			ILgWritingSystemFactory wsf = cache.WritingSystemFactory;
			writer.WriteRaw(StringUtils.GetXmlRep(propertyData, wsf, 0));
			writer.WriteEndElement(); // Close prop. element.
		}

		/// <summary>
		/// Write out an atomic object property.
		/// </summary>
		internal static void WriteAtomicObjectProperty(XmlWriter writer, string elementName, ObjectPropertyType propertyType, ICmObject propertyData)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (string.IsNullOrEmpty(elementName)) throw new ArgumentNullException("elementName");

			if (propertyData == null) return;

			writer.WriteStartElement(elementName); // Open prop. element.
			WriteObjectReference(writer, propertyData, propertyType);
			writer.WriteEndElement(); // Close prop. element.
		}

		/// <summary>
		/// Write out one object reference (owing or real reference).
		/// </summary>
		internal static void WriteObjectReference(XmlWriter writer, ICmObject propertyData, ObjectPropertyType propertyType)
		{
			writer.WriteStartElement("objsur");
			writer.WriteAttributeString("guid", propertyData.Guid.ToString().ToLowerInvariant());
			var val = "o";
			if (propertyType == ObjectPropertyType.Reference)
				val = "r";
			writer.WriteAttributeString("t", val);
			writer.WriteEndElement();
		}

		/// <summary>
		/// Write out one object reference (owing or real reference).
		/// </summary>
		internal static void WriteObjectReference(XmlWriter writer, Guid propertyGuid, ObjectPropertyType propertyType)
		{
			writer.WriteStartElement("objsur");
			writer.WriteAttributeString("guid", propertyGuid.ToString().ToLowerInvariant());
			var val = "o";
			if (propertyType == ObjectPropertyType.Reference)
				val = "r";
			writer.WriteAttributeString("t", val);
			writer.WriteEndElement();
		}

		/// <summary>
		/// Write vector property.
		/// </summary>
		internal static void WriteVectorProperty<T>(XmlWriter writer, string elementName, ICollection<T> propertyData) where T : ICmObject
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (string.IsNullOrEmpty(elementName)) throw new ArgumentNullException("elementName");

			if (propertyData == null || propertyData.Count == 0) return;

			writer.WriteStartElement(elementName); // Open prop. element.
			((IFdoVectorInternal)propertyData).ToXMLString(writer);
			writer.WriteEndElement(); // Close prop. element.
		}

		/// <summary>
		/// Write Text Prop property.
		/// </summary>
		internal static void WriteTextPropBinary(XmlWriter writer, ILgWritingSystemFactory wsf, string elementName, ITsTextProps propertyData)
		{
			if (writer == null) throw new ArgumentNullException("writer");
			if (string.IsNullOrEmpty(elementName)) throw new ArgumentNullException("elementName");

			if (propertyData == null) return;

			writer.WriteStartElement(elementName); // Open prop. element.
			writer.WriteRaw(StringUtils.GetXmlRep(propertyData, wsf));
			writer.WriteEndElement(); // Close prop. element.
		}
	}
}
