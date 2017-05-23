// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TsStringSerializer.cs
// Responsibility: FW Team
//
// <remarks>
// </remarks>

using System;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using SIL.CoreImpl.KernelInterfaces;
using SIL.Utils;

namespace SIL.CoreImpl.Text
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class for serializing and deserializing TsStrings stored in XML format
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class TsStringSerializer
	{
		#region Public Serialization Methods

		/// <summary>
		/// Serializes the <see cref="ITsString"/> to XML.
		/// </summary>
		public static string SerializeTsStringToXml(ITsString tss, ILgWritingSystemFactory lgwsf, int ws = 0, bool writeObjData = true, bool indent = false)
		{
			// We export only the NFSC form (NFC with exceptions for the parallel style information)
			ITsString normalizedTss = tss.get_NormalizedForm(FwNormalizationMode.knmNFSC);

			var xml = new StringBuilder();
			var settings = new XmlWriterSettings
			{
				OmitXmlDeclaration = true,
				Indent = true,
				IndentChars = indent ? "  " : string.Empty,
				NewLineChars = Environment.NewLine
			};
			using (var writer = XmlWriter.Create(xml, settings))
			{
				if (ws > 0)
				{
					string id = lgwsf.GetStrFromWs(ws);
					writer.WriteStartElement("AStr");
					writer.WriteAttributeString("ws", Icu.Normalize(id, Icu.UNormalizationMode.UNORM_NFC));
				}
				else
				{
					writer.WriteStartElement("Str");
				}

				// Write the properties and text for each run
				string fieldName = null;
				for (int i = 0; i < normalizedTss.RunCount; i++)
				{
					TsRunInfo tri;
					ITsTextProps textProps = normalizedTss.FetchRunInfo(i, out tri);
					string objDataStr;
					if (textProps.TryGetStringValue(FwTextPropType.ktptObjData, out objDataStr) && !writeObjData)
					{
						var chType = (FwObjDataTypes) objDataStr[0];
						if (chType == FwObjDataTypes.kodtPictEvenHot || chType == FwObjDataTypes.kodtPictOddHot
							|| chType == FwObjDataTypes.kodtNameGuidHot || chType == FwObjDataTypes.kodtOwnNameGuidHot)
						{
							continue;
						}
					}

					string runFieldName;
					if (textProps.TryGetStringValue(FwTextPropType.ktptFieldName, out runFieldName) && fieldName != runFieldName)
					{
						if (!string.IsNullOrEmpty(fieldName))
							writer.WriteEndElement();
						if (!string.IsNullOrEmpty(runFieldName))
						{
							writer.WriteStartElement("Field");
							writer.WriteAttributeString("name", runFieldName);
						}
						fieldName = runFieldName;
					}

					bool markItem;
					FwTextPropVar var;
					int markItemValue;
					if (textProps.TryGetIntValue(FwTextPropType.ktptMarkItem, out var, out markItemValue)
						&& var == FwTextPropVar.ktpvEnum && markItemValue == (int) FwTextToggleVal.kttvForceOn)
					{
						writer.WriteStartElement("Item");
						writer.WriteStartElement("Run");
						markItem = true;
					}
					else
					{
						writer.WriteStartElement("Run");
						markItem = false;
					}

					for (int j = 0; j < textProps.IntPropCount; j++)
					{
						FwTextPropType tpt;
						int value = textProps.GetIntProperty(j, out tpt, out var);
						if (tpt != FwTextPropType.ktptMarkItem)
							TsPropsSerializer.WriteIntProperty(writer, lgwsf, tpt, var, value);
					}

					byte[] pict = null;
					bool hotGuid = false;
					for (int j = 0; j < textProps.StrPropCount; j++)
					{
						FwTextPropType tpt;
						string value = textProps.GetStringProperty(j, out tpt);
						TsPropsSerializer.WriteStringProperty(writer, tpt, value);
						if (tpt == FwTextPropType.ktptObjData && !string.IsNullOrEmpty(value))
						{
							switch ((FwObjDataTypes) value[0])
							{
								// The element data associated with a picture is the actual picture data
								// since it is much too large to want embedded as an XML attribute value.
								// (This is an antique kludge that isn't really used in practice, but some
								// of our test data still exercises it.)
								case FwObjDataTypes.kodtPictEvenHot:
								case FwObjDataTypes.kodtPictOddHot:
									pict = Encoding.Unicode.GetBytes(value.Substring(1));
									break;
								// The generated XML contains both the link value as an attribute and the
								// (possibly edited) display string as the run's element data.
								case FwObjDataTypes.kodtExternalPathName:
									break;
								// used ONLY in the clipboard...contains XML representation of (currently) a footnote.
								case FwObjDataTypes.kodtEmbeddedObjectData:
									break;
								// The string data associated with this run is assumed to be a dummy magic
								// character that flags (redundantly for XML) that the actual data to
								// display is based on the ktptObjData attribute.
								case FwObjDataTypes.kodtNameGuidHot:
								case FwObjDataTypes.kodtOwnNameGuidHot:
								case FwObjDataTypes.kodtContextString:
								case FwObjDataTypes.kodtGuidMoveableObjDisp:
									hotGuid = true;
									break;
							}
						}
					}

					if (pict != null)
					{
						// Write the bytes of the picture data
						var sb = new StringBuilder();
						for (int j = 0; j < pict.Length; j++)
						{
							sb.Append(pict[j].ToString("X2"));
							if (j % 32 == 31)
								sb.AppendLine();
						}
						writer.WriteString(sb.ToString());
					}
					else if (hotGuid)
					{
						writer.WriteString(string.Empty);
					}
					else
					{
						string runText = normalizedTss.get_RunText(i) ?? string.Empty;
						if (runText != string.Empty && runText.All(char.IsWhiteSpace))
							writer.WriteAttributeString("xml", "space", "", "preserve");
						// TODO: should we escape quotation marks? this is not necessary but different than the behavior of the C++ implementation
						writer.WriteString(Icu.Normalize(runText, Icu.UNormalizationMode.UNORM_NFC));
					}

					writer.WriteEndElement();
					if (markItem)
						writer.WriteEndElement();
				}
				if (!string.IsNullOrEmpty(fieldName))
					writer.WriteEndElement();
				writer.WriteEndElement();
			}
			return xml.ToString();
		}

		#endregion

		#region Serialization Helper Methods

		#endregion

		#region Public Deserialization Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deserializes a TsString from the specified XML data.
		/// NOTE: This overload is slower then using the overload that takes an XElement.
		/// </summary>
		/// <param name="xml">The XML data.</param>
		/// <param name="lgwsf">The writing system factory.</param>
		/// <returns>The created TsString. Will never be <c>null</c> because an exception is
		/// thrown if anything is invalid.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static ITsString DeserializeTsStringFromXml(string xml, ILgWritingSystemFactory lgwsf)
		{
			return DeserializeTsStringFromXml(XElement.Parse(xml, LoadOptions.PreserveWhitespace), lgwsf);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deserializes a TsString from the XML node.
		/// </summary>
		/// <param name="xml">The XML node.</param>
		/// <param name="lgwsf">The writing system factory.</param>
		/// <returns>The created TsString. Will never be <c>null</c> because an exception is
		/// thrown if anything is invalid.</returns>
		/// ------------------------------------------------------------------------------------
		public static ITsString DeserializeTsStringFromXml(XElement xml, ILgWritingSystemFactory lgwsf)
		{
			if (xml != null)
			{
				switch (xml.Name.LocalName)
				{
					case "AStr":
					case "Str":
						return HandleSimpleString(xml, lgwsf) ?? HandleComplexString(xml, lgwsf);
					default:
						throw new XmlSchemaException("TsString XML must contain a <Str> or <AStr> root element");
				}
			}
			return null;
		}

		#endregion

		#region Deserialization Helper Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles a complex string that contains multiple runs with optional multiple
		/// text props applied.
		/// </summary>
		/// <param name="xml">The XML.</param>
		/// <param name="lgwsf">The writing system factory.</param>
		/// <returns>The created TsString</returns>
		/// ------------------------------------------------------------------------------------
		private static ITsString HandleComplexString(XElement xml, ILgWritingSystemFactory lgwsf)
		{
			var runs = xml.Elements("Run");
			if (runs.Count() == 0)
			{
				if (xml.Name.LocalName == "AStr" && xml.Attributes().Count() == 1)
				{
					// This duplicates a little bit of code from HandleSimpleRun, but I wanted to keep that really simple
					// and fast, and this case hardly ever happens...maybe not at all in real life.
					XAttribute wsAttribute = xml.Attributes().First();
					if (wsAttribute.Name.LocalName != "ws")
						return null; // we handle only single runs with only the ws attribute.
					// Make sure the text is in the decomposed form (FWR-148)
					string runText = Icu.Normalize(xml.Value, Icu.UNormalizationMode.UNORM_NFD);
					return TsStringUtils.MakeString(runText, GetWsForId(wsAttribute.Value, lgwsf));
				}
				return null;	// If we don't have any runs, we don't have a string!
			}

			var strBldr = TsStringUtils.MakeIncStrBldr();

			foreach (XElement runElement in runs)
			{
				if (runElement == null)
					throw new XmlSchemaException("TsString XML must contain a <Run> element contained in a <" + xml.Name.LocalName + "> element");
				string runText = runElement.Value;
				if (runElement.Attribute("ws") == null && (runText.Length == 0 || runText[0] > 13))
					throw new XmlSchemaException("Run element must contain a ws attribute. Run text: " + runElement.Value);

				// Make sure the text is in the decomposed form (FWR-148)
				runText = Icu.Normalize(runText, Icu.UNormalizationMode.UNORM_NFD);
				bool isOrcNeeded = TsPropsSerializer.GetPropAttributesForElement(runElement, lgwsf, strBldr);

				// Add an ORC character, if needed, for the run
				if (runText.Length == 0 && isOrcNeeded)
					runText = StringUtils.kszObject;

				// Add the text with the properties to the builder
				strBldr.Append(runText);
			}

			return strBldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles creating simple TsStrings that contain one run with only a writing system.
		/// </summary>
		/// <param name="rootXml">The element including the Str or AStr tag</param>
		/// <param name="lgwsf">The writing system factory.</param>
		/// <returns>The created TsString or null if the XML was too complext to be handled
		/// by this method.</returns>
		/// ------------------------------------------------------------------------------------
		private static ITsString HandleSimpleString(XElement rootXml, ILgWritingSystemFactory lgwsf)
		{
			if (rootXml.Elements().Count() != 1)
				return null;

			XElement textElement = rootXml.Elements().First();
			if (textElement.Name.LocalName != "Run")
				return null; // probably an error, anyway not simple case we are optimizing.

			int cTextElementAtribs = textElement.Attributes().Count();
			if (cTextElementAtribs != 1)
				return null; // Way too complex for this simple case

			XAttribute wsAttribute = textElement.Attributes().First();
			if (wsAttribute.Name.LocalName != "ws")
				return null; // we handle only single runs with only the ws attribute.
			// Make sure the text is in the decomposed form (FWR-148)
			string runText = Icu.Normalize(textElement.Value, Icu.UNormalizationMode.UNORM_NFD);
			return TsStringUtils.MakeString(runText, GetWsForId(wsAttribute.Value, lgwsf));
		}
		#endregion

		#region Internal methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ws handle for specified writing system identifier.
		/// </summary>
		/// <param name="wsId">The writing system identifier.</param>
		/// <param name="lgwsf">The writing system factory.</param>
		/// ------------------------------------------------------------------------------------
		internal static int GetWsForId(string wsId, ILgWritingSystemFactory lgwsf)
		{
			try
			{
				ILgWritingSystem ws = lgwsf.get_Engine(wsId);
				return ws.Handle;
			}
			catch (Exception e)
			{
				throw new XmlSchemaException("Unable to create writing system: " + wsId, e);
			}
		}
		#endregion
	}
}
