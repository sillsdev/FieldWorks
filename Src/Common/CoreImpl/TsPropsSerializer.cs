// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TsPropsSerializer.cs
// Responsibility: SteenwykT
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using System.Xml.Schema;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class for serializing and deserializing TsTextProps stored in XML format
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class TsPropsSerializer
	{
		#region Public Deserialization Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deserializes text properties from the specified XML data.
		/// NOTE: This overload is slower then using the overload that takes an XElement.
		/// </summary>
		/// <param name="xml">The XML data.</param>
		/// <param name="lgwsf">The writing system factory.</param>
		/// <returns>The created TsTextProps. Will never be <c>null</c> because an exception is
		/// thrown if anything is invalid.</returns>
		/// ------------------------------------------------------------------------------------
		public static ITsTextProps DeserializePropsFromXml(string xml, ILgWritingSystemFactory lgwsf)
		{
			return DeserializePropsFromXml(XElement.Parse(xml), lgwsf);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deserializes text properties from the specified XML node.
		/// </summary>
		/// <param name="xml">The XML node.</param>
		/// <param name="lgwsf">The writing system factory.</param>
		/// <returns>The created TsTextProps. Will never be <c>null</c> because an exception is
		/// thrown if anything is invalid.</returns>
		/// ------------------------------------------------------------------------------------
		public static ITsTextProps DeserializePropsFromXml(XElement xml, ILgWritingSystemFactory lgwsf)
		{
			ITsPropsBldr propsBldr = GetPropAttributesForElement(xml, lgwsf);

			foreach (XElement element in xml.Elements())
			{
				switch (element.Name.LocalName)
				{
					case "BulNumFontInfo":
						AddBulletFontInfoToBldr(propsBldr, element, lgwsf);
						break;
					case "WsStyles9999":
						AddWsStyleInfoToBldr(propsBldr, element, lgwsf);
						break;
					default:
						throw new XmlSchemaException("Illegal element in <Props> element: " + element.Name.LocalName);
				}
			}

			return propsBldr.GetTextProps();
		}
		#endregion

		#region Deserialization helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the bullet font information to the specified props builder.
		/// </summary>
		/// <param name="bldr">The props builder.</param>
		/// <param name="bulFontInfo">The bullet font information XML.</param>
		/// <param name="lgwsf">The writing system factory.</param>
		/// ------------------------------------------------------------------------------------
		private static void AddBulletFontInfoToBldr(ITsPropsBldr bldr, XElement bulFontInfo,
			ILgWritingSystemFactory lgwsf)
		{
			int intValue, type, var;
			string strValue;
			ITsPropsBldr fontProps = GetPropAttributesForElement(bulFontInfo, lgwsf);

			// Add the integer properties to the bullet props string
			StringBuilder bulletProps = new StringBuilder(fontProps.IntPropCount * 3 + fontProps.StrPropCount * 20);
			for (int i = 0; i < fontProps.IntPropCount; i++)
			{
				fontProps.GetIntProp(i, out type, out var, out intValue);
				bulletProps.Append((char)type);
				WriteIntToStrBuilder(bulletProps, intValue);
			}

			// Add the string properties to the bullet props string
			for (int i = 0; i < fontProps.StrPropCount; i++)
			{
				fontProps.GetStrProp(i, out type, out strValue);
				bulletProps.Append((char)type);
				bulletProps.Append(strValue);
				bulletProps.Append('\u0000');
			}

			bldr.SetStrPropValue((int)FwTextPropType.ktptBulNumFontInfo, bulletProps.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the ws style info to BLDR.
		/// </summary>
		/// <param name="bldr">The BLDR.</param>
		/// <param name="wsStyleInfo">The ws style info.</param>
		/// <param name="lgwsf">The LGWSF.</param>
		/// ------------------------------------------------------------------------------------
		private static void AddWsStyleInfoToBldr(ITsPropsBldr bldr, XElement wsStyleInfo,
			ILgWritingSystemFactory lgwsf)
		{
			StringBuilder wsStyleProps = new StringBuilder();

			foreach (XElement wsPropInfo in wsStyleInfo.Elements())
			{
				if (wsPropInfo.Name.LocalName != "WsProp")
					throw new XmlSchemaException("Invalid nested element in <WsStyle9999> element: " + wsPropInfo.Name.LocalName);

				// Get the writing system that the prop override is for
				XAttribute wsAttrib = wsPropInfo.Attribute("ws");
				if (wsAttrib == null)
					throw new XmlSchemaException("WsProp must contain a 'ws' attribute");
				wsAttrib.Remove(); // Make sure we don't count it twice
				WriteIntToStrBuilder(wsStyleProps, TsStringSerializer.GetWsForId(wsAttrib.Value, lgwsf));

				// Get the font family for the prop override
				XAttribute fontFamilyAttrib = wsPropInfo.Attribute("fontFamily");
				wsStyleProps.Append((char)((fontFamilyAttrib != null) ? fontFamilyAttrib.Value.Length : 0));
				if (fontFamilyAttrib != null)
				{
					fontFamilyAttrib.Remove(); // Make sure we don't count it twice
					wsStyleProps.Append(fontFamilyAttrib.Value);
				}

				// Get the rest of the props that are available
				ITsPropsBldr wsProps = GetPropAttributesForElement(wsPropInfo, lgwsf);

				// A negative value is specified for string properties, positive value
				// if only integer properties are added.
				wsStyleProps.Append((char)((wsProps.StrPropCount > 0) ? -wsProps.StrPropCount : wsProps.IntPropCount));

				int intValue, type, var;
				string strValue;
				// Add the string properties to the ws props string)
				if (wsProps.StrPropCount > 0)
				{
					for (int i = 0; i < wsProps.StrPropCount; i++)
					{
						wsProps.GetStrProp(i, out type, out strValue);
						wsStyleProps.Append((char)type);
						wsStyleProps.Append((char)strValue.Length);
						wsStyleProps.Append(strValue);
					}
					// Need to add the count of integer properties
					wsStyleProps.Append((char)wsProps.IntPropCount);
				}

				// Add the integer properties to the ws props string
				for (int i = 0; i < wsProps.IntPropCount; i++)
				{
					wsProps.GetIntProp(i, out type, out var, out intValue);
					wsStyleProps.Append((char)type);
					wsStyleProps.Append((char)var);
					WriteIntToStrBuilder(wsStyleProps, intValue);
				}
			}

			bldr.SetStrPropValue((int)FwTextPropType.ktptWsStyle, wsStyleProps.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the prop attributes for element.
		/// </summary>
		/// <param name="xml">The XML.</param>
		/// <param name="lgwsf">The writing system factory.</param>
		/// ------------------------------------------------------------------------------------
		internal static ITsPropsBldr GetPropAttributesForElement(XElement xml, ILgWritingSystemFactory lgwsf)
		{
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			foreach (XAttribute attr in xml.Attributes())
			{
				switch (attr.Name.LocalName)
				{
					case "align":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum,
							GetAlignValueForStr(attr.Value));
						break;
					case "backcolor":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, 0,
							GetColorValueForStr(attr.Value));
						break;
					case "bold":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum,
							GetToggleValueForStr(attr.Value));
						break;
					case "borderBottom":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "borderColor":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptBorderColor, 0,
							GetColorValueForStr(attr.Value));
						break;
					case "borderLeading":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptBorderLeading, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "borderTop":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptBorderTop, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "borderTrailing":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "bulNumScheme":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptBulNumScheme, (int)FwTextPropVar.ktpvEnum,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "bulNumStartAt":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptBulNumStartAt, 0,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "bulNumTxtAft":
						propsBldr.SetStrPropValue((int)FwTextPropType.ktptBulNumTxtAft, attr.Value);
						break;
					case "bulNumTxtBef":
						propsBldr.SetStrPropValue((int)FwTextPropType.ktptBulNumTxtBef, attr.Value);
						break;
					case "charStyle":
						Debug.Fail("We don't support the old charStyle attribute!");
						break;
					case "contextString":
						AddObjDataToBldr(propsBldr, attr, FwObjDataTypes.kodtContextString);
						break;
					case "embedded":
						propsBldr.SetStrPropValue((int)FwTextPropType.ktptObjData,
							(char)FwObjDataTypes.kodtEmbeddedObjectData + attr.Value);
						break;
					case "externalLink":
						propsBldr.SetStrPropValue((int)FwTextPropType.ktptObjData,
							(char)FwObjDataTypes.kodtExternalPathName + attr.Value);
						break;
					case "firstIndent":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptFirstIndent, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, true));
						break;
					case "fontFamily":
						propsBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily, attr.Value);
						break;
					case "fontsize":
						AddSizePropertyToBldr(propsBldr, attr.Value, (int)FwTextPropType.ktptFontSize,
							xml.Attribute("fontsizeUnit"), null);
						break;
					case "fontsizeUnit":
						break; // Ignore. Its handled in the fontsize.
					case "fontVariations":
						propsBldr.SetStrPropValue((int)FwTextPropType.ktptFontVariations, attr.Value);
						break;
					case "forecolor":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, 0,
							GetColorValueForStr(attr.Value));
						break;
					case "italic":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptItalic, (int)FwTextPropVar.ktpvEnum,
							GetToggleValueForStr(attr.Value));
						break;
					case "keepTogether":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptKeepTogether, (int)FwTextPropVar.ktpvEnum,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "keepWithNext":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptKeepWithNext, (int)FwTextPropVar.ktpvEnum,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "leadingIndent":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptLeadingIndent, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "lineHeight":
						AddSizePropertyToBldr(propsBldr, attr.Value, (int)FwTextPropType.ktptLineHeight,
							xml.Attribute("lineHeightUnit"), xml.Attribute("lineHeightType"));
						break;
					case "lineHeightType":
						break; // Ignore. Its handled in the lineHeight
					case "lineHeightUnit":
						break; // Ignore. Its handled in the lineHeight
					case "link":
						AddObjDataToBldr(propsBldr, attr, FwObjDataTypes.kodtNameGuidHot);
						break;
					case "marginBottom":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptMarginBottom, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "marginLeading":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptMarginLeading, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "marginTop":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptMarginTop, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "marginTrailing":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "moveableObj":
						AddObjDataToBldr(propsBldr, attr, FwObjDataTypes.kodtGuidMoveableObjDisp);
						break;
					case "namedStyle":
						propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, attr.Value);
						break;
					case "offset":
						AddSizePropertyToBldr(propsBldr, attr.Value, (int)FwTextPropType.ktptOffset,
							xml.Attribute("offsetUnit"), null, true);
						break;
					case "offsetUnit":
						break; // Ignore. Its handled in the offset
					case "ownlink":
						AddObjDataToBldr(propsBldr, attr, FwObjDataTypes.kodtOwnNameGuidHot);
						break;
					case "padBottom":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptPadBottom, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "padLeading":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptPadLeading, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "padTop":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "padTrailing":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptPadTrailing, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "paracolor":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptParaColor, 0,
							GetColorValueForStr(attr.Value));
						break;
					case "paraStyle":
						Debug.Fail("We don't support the old paraStyle attribute!");
						break;
					case "rightToLeft":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "spaceAfter":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptSpaceAfter, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, true));
						break;
					case "spaceBefore":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptSpaceBefore, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, true));
						break;
					case "spellcheck":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptSpellCheck, (int)FwTextPropVar.ktpvEnum,
							GetSpellCheckValueForStr(attr.Value));
						break;
					case "superscript":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptSuperscript, (int)FwTextPropVar.ktpvEnum,
							GetSuperscriptValueForStr(attr.Value));
						break;
					case "tabDef":
						Debug.Fail("We don't support the tabDef property!");
						break;
					case "tabList":
						Debug.Fail("We don't support the tabList property!");
						break;
					case "tags":
						propsBldr.SetStrPropValue((int)FwTextPropType.ktptTags,
							GetGuidValuesForStr(attr.Value));
						break;
					case "trailingIndent":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptTrailingIndent, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "type":
						Debug.Assert(attr.Value == "chars", "Embedded pictures are not supported!");
						break;
					case "undercolor":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptUnderColor, 0,
							GetColorValueForStr(attr.Value));
						break;
					case "underline":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptUnderline, (int)FwTextPropVar.ktpvEnum,
							GetUnderlineTypeForStr(attr.Value));
						break;
					case "widowOrphan":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptWidowOrphanControl, (int)FwTextPropVar.ktpvEnum,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "ws":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
							TsStringSerializer.GetWsForId(attr.Value, lgwsf));
						break;
					case "wsBase":
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptBaseWs, 0,
							TsStringSerializer.GetWsForId(attr.Value, lgwsf));
						break;
					case "wsStyle":
						Debug.Fail("We don't support the old wsStyle attribute!");
						break;
					case "space":
						break;
					default:
						throw new XmlSchemaException("Unknown Prop attribute: " + attr.Name.LocalName);
				}
			}

			return propsBldr;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the prop attributes for element and applies them to a string builder
		/// (to take effect on the next string added).
		/// </summary>
		/// <param name="xml">The XML.</param>
		/// <param name="lgwsf">The writing system factory.</param>
		/// <param name="strBldr"></param>
		/// <returns>Rather strangely, it returns a boolean indicating whether an ORC is needed if the
		/// text of the run is empty, that is, whether we set an objdata property</returns>
		/// ------------------------------------------------------------------------------------
		internal static bool GetPropAttributesForElement(XElement xml, ILgWritingSystemFactory lgwsf, ITsIncStrBldr strBldr)
		{
			bool isOrcNeeded = false;
			strBldr.ClearProps();
			foreach (XAttribute attr in xml.Attributes())
			{
				switch (attr.Name.LocalName)
				{
					case "align":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum,
							GetAlignValueForStr(attr.Value));
						break;
					case "backcolor":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, 0,
							GetColorValueForStr(attr.Value));
						break;
					case "bold":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum,
							GetToggleValueForStr(attr.Value));
						break;
					case "borderBottom":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "borderColor":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptBorderColor, 0,
							GetColorValueForStr(attr.Value));
						break;
					case "borderLeading":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptBorderLeading, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "borderTop":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptBorderTop, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "borderTrailing":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "bulNumScheme":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptBulNumScheme, (int)FwTextPropVar.ktpvEnum,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "bulNumStartAt":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptBulNumStartAt, 0,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "bulNumTxtAft":
						strBldr.SetStrPropValue((int)FwTextPropType.ktptBulNumTxtAft, attr.Value);
						break;
					case "bulNumTxtBef":
						strBldr.SetStrPropValue((int)FwTextPropType.ktptBulNumTxtBef, attr.Value);
						break;
					case "charStyle":
						Debug.Fail("We don't support the old charStyle attribute!");
						break;
					case "contextString":
						AddObjDataToBldr(strBldr, attr, FwObjDataTypes.kodtContextString);
						isOrcNeeded = true;
						break;
					case "embedded":
						strBldr.SetStrPropValue((int)FwTextPropType.ktptObjData,
							(char)FwObjDataTypes.kodtEmbeddedObjectData + attr.Value);
						break;
					case "externalLink":
						strBldr.SetStrPropValue((int)FwTextPropType.ktptObjData,
							(char)FwObjDataTypes.kodtExternalPathName + attr.Value);
						break;
					case "firstIndent":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptFirstIndent, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, true));
						break;
					case "fontFamily":
						strBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily, attr.Value);
						break;
					case "fontsize":
						AddSizePropertyToBldr(strBldr, attr.Value, (int)FwTextPropType.ktptFontSize,
							xml.Attribute("fontsizeUnit"), null);
						break;
					case "fontsizeUnit":
						break; // Ignore. Its handled in the fontsize.
					case "fontVariations":
						strBldr.SetStrPropValue((int)FwTextPropType.ktptFontVariations, attr.Value);
						break;
					case "forecolor":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, 0,
							GetColorValueForStr(attr.Value));
						break;
					case "italic":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptItalic, (int)FwTextPropVar.ktpvEnum,
							GetToggleValueForStr(attr.Value));
						break;
					case "keepTogether":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptKeepTogether, (int)FwTextPropVar.ktpvEnum,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "keepWithNext":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptKeepWithNext, (int)FwTextPropVar.ktpvEnum,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "leadingIndent":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptLeadingIndent, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "lineHeight":
						AddSizePropertyToBldr(strBldr, attr.Value, (int)FwTextPropType.ktptLineHeight,
							xml.Attribute("lineHeightUnit"), xml.Attribute("lineHeightType"));
						break;
					case "lineHeightType":
						break; // Ignore. Its handled in the lineHeight
					case "lineHeightUnit":
						break; // Ignore. Its handled in the lineHeight
					case "link":
						AddObjDataToBldr(strBldr, attr, FwObjDataTypes.kodtNameGuidHot);
						isOrcNeeded = true;
						break;
					case "marginBottom":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptMarginBottom, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "marginLeading":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptMarginLeading, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "marginTop":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptMarginTop, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "marginTrailing":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "moveableObj":
						AddObjDataToBldr(strBldr, attr, FwObjDataTypes.kodtGuidMoveableObjDisp);
						isOrcNeeded = true;
						break;
					case "namedStyle":
						strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, attr.Value);
						break;
					case "offset":
						AddSizePropertyToBldr(strBldr, attr.Value, (int)FwTextPropType.ktptOffset,
							xml.Attribute("offsetUnit"), null);
						break;
					case "offsetUnit":
						break; // Ignore. Its handled in the offset
					case "ownlink":
						AddObjDataToBldr(strBldr, attr, FwObjDataTypes.kodtOwnNameGuidHot);
						isOrcNeeded = true;
						break;
					case "padBottom":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptPadBottom, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "padLeading":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptPadLeading, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "padTop":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "padTrailing":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptPadTrailing, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "paracolor":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptParaColor, 0,
							GetColorValueForStr(attr.Value));
						break;
					case "paraStyle":
						Debug.Fail("We don't support the old paraStyle attribute!");
						break;
					case "rightToLeft":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "spaceAfter":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptSpaceAfter, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, true));
						break;
					case "spaceBefore":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptSpaceBefore, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, true));
						break;
					case "spellcheck":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptSpellCheck, (int)FwTextPropVar.ktpvEnum,
							GetSpellCheckValueForStr(attr.Value));
						break;
					case "superscript":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptSuperscript, (int)FwTextPropVar.ktpvEnum,
							GetSuperscriptValueForStr(attr.Value));
						break;
					case "tabDef":
						Debug.Fail("We don't support the tabDef property!");
						break;
					case "tabList":
						Debug.Fail("We don't support the tabList property!");
						break;
					case "tags":
						strBldr.SetStrPropValue((int)FwTextPropType.ktptTags,
							GetGuidValuesForStr(attr.Value));
						break;
					case "trailingIndent":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptTrailingIndent, (int)FwTextPropVar.ktpvMilliPoint,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "type":
						Debug.Assert(attr.Value == "chars", "Embedded pictures are not supported!");
						break;
					case "undercolor":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptUnderColor, 0,
							GetColorValueForStr(attr.Value));
						break;
					case "underline":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptUnderline, (int)FwTextPropVar.ktpvEnum,
							GetUnderlineTypeForStr(attr.Value));
						break;
					case "widowOrphan":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptWidowOrphanControl, (int)FwTextPropVar.ktpvEnum,
							GetIntValueFromStr(attr.Value, false));
						break;
					case "ws":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
							TsStringSerializer.GetWsForId(attr.Value, lgwsf));
						break;
					case "wsBase":
						strBldr.SetIntPropValues((int)FwTextPropType.ktptBaseWs, 0,
							TsStringSerializer.GetWsForId(attr.Value, lgwsf));
						break;
					case "wsStyle":
						Debug.Fail("We don't support the old wsStyle attribute!");
						break;
					case "space":
					case "editable":
						break;
					default:
						throw new XmlSchemaException("Unknown Prop attribute: " + attr.Name.LocalName);
				}
			}
			return isOrcNeeded;
		}

		#endregion

		#region Helper Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a size property to the specified props builder with the specified prop type.
		/// This default does not allow negative sizes.
		/// </summary>
		/// <param name="propsBldr">The props builder.</param>
		/// <param name="value">The string representation of the size value.</param>
		/// <param name="proptype">The property type.</param>
		/// <param name="unitAttr">The attribute containing the size unit (can be null).</param>
		/// <param name="typeAttr">The attribute containing the size type (can be null).</param>
		/// ------------------------------------------------------------------------------------
		private static void AddSizePropertyToBldr(ITsPropsBldr propsBldr, string value, int proptype,
			XAttribute unitAttr, XAttribute typeAttr)
		{
			AddSizePropertyToBldr(propsBldr, value, proptype, unitAttr, typeAttr, false);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a size property to the specified props builder with the specified prop type.
		/// </summary>
		/// <param name="propsBldr">The props builder.</param>
		/// <param name="value">The string representation of the size value.</param>
		/// <param name="proptype">The property type.</param>
		/// <param name="unitAttr">The attribute containing the size unit (can be null).</param>
		/// <param name="typeAttr">The attribute containing the size type (can be null).</param>
		/// <param name="allowNegative">true if negative size is allowed (e.g., baseline offset)</param>
		/// ------------------------------------------------------------------------------------
		private static void AddSizePropertyToBldr(ITsPropsBldr propsBldr, string value, int proptype,
			XAttribute unitAttr, XAttribute typeAttr, bool allowNegative)
		{
			int var;
			int intValue = GetSizeValue(value, unitAttr, typeAttr, allowNegative, out var);
			propsBldr.SetIntPropValues(proptype, var, intValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a size property to the specified props builder with the specified prop type.
		/// </summary>
		/// <param name="propsBldr">The props builder.</param>
		/// <param name="value">The string representation of the size value.</param>
		/// <param name="proptype">The property type.</param>
		/// <param name="unitAttr">The attribute containing the size unit (can be null).</param>
		/// <param name="typeAttr">The attribute containing the size type (can be null).</param>
		/// ------------------------------------------------------------------------------------
		private static void AddSizePropertyToBldr(ITsIncStrBldr propsBldr, string value, int proptype,
			XAttribute unitAttr, XAttribute typeAttr)
		{
			int var;
			int intValue = GetSizeValue(value, unitAttr, typeAttr, false, out var);
			propsBldr.SetIntPropValues(proptype, var, intValue);
		}
		private static int GetSizeValue(string value, XAttribute unitAttr, XAttribute typeAttr, bool allowNegative, out int var)
		{
			if (value.EndsWith("mpt"))
			{
				// Value contains optional millipoint specification. Just strip it off.
				value = value.Substring(0, value.IndexOf("mpt"));
			}

			var = GetUnitValueForStr(unitAttr);
			int intValue = GetIntValueFromStr(value, allowNegative);
			if (typeAttr != null)
			{
				switch (typeAttr.Value)
				{
					case "exact":
						if (var == (int)FwTextPropVar.ktpvMilliPoint)
							intValue = -intValue; // negative means "exact" internally.  See FWC-20.
						break;
					case "atLeast":
						break; // This is the default value
					default:
						throw new XmlSchemaException("Invalid type specified: " + typeAttr.Value);
				}
			}
			return intValue;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds object link data to the specified text property builder
		/// </summary>
		/// <param name="propsBldr">The props builder.</param>
		/// <param name="attr">The attribute to process.</param>
		/// <param name="type">The type of object data to be added to the text properties.</param>
		/// ------------------------------------------------------------------------------------
		private static void AddObjDataToBldr(ITsPropsBldr propsBldr, XAttribute attr, FwObjDataTypes type)
		{
			byte[] objData = TsStringUtils.GetObjData(new Guid(attr.Value), (byte)type);
			propsBldr.SetStrPropValueRgch((int)FwTextPropType.ktptObjData, objData, objData.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds object link data to the specified string builder (to affect the next string added)
		/// </summary>
		/// <param name="strBldr">The string builder to be modified.</param>
		/// <param name="attr">The attribute to process.</param>
		/// <param name="type">The type of object data to be added to the text properties.</param>
		/// ------------------------------------------------------------------------------------
		private static void AddObjDataToBldr(ITsIncStrBldr strBldr, XAttribute attr, FwObjDataTypes type)
		{
			byte[] objData = TsStringUtils.GetObjData(new Guid(attr.Value), (byte)type);
			strBldr.SetStrPropValueRgch((int)FwTextPropType.ktptObjData, objData, objData.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes the specified integer value to the specified string builder.
		/// </summary>
		/// <param name="bldr">The string builder.</param>
		/// <param name="toWrite">The integer to write to the string builder.</param>
		/// ------------------------------------------------------------------------------------
		private static void WriteIntToStrBuilder(StringBuilder bldr, int toWrite)
		{
			bldr.Append((char)(toWrite & 0xFFFF));
			bldr.Append((char)((toWrite >> 16) & 0xFFFF));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the superscript value for the specified string.
		/// </summary>
		/// <param name="str">The string.</param>
		/// ------------------------------------------------------------------------------------
		private static int GetSuperscriptValueForStr(string str)
		{
			switch (str)
			{
				case "off": return (int)FwSuperscriptVal.kssvOff;
				case "super": return (int)FwSuperscriptVal.kssvSuper;
				case "sub": return (int)FwSuperscriptVal.kssvSub;
				default:
					throw new XmlSchemaException("Invalid superscript value: " + str);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the spell check value for the specified string.
		/// </summary>
		/// <param name="str">The string.</param>
		/// ------------------------------------------------------------------------------------
		private static int GetSpellCheckValueForStr(string str)
		{
			switch (str)
			{
				case "normal": return (int)SpellingModes.ksmNormalCheck;
				case "forceCheck": return (int)SpellingModes.ksmForceCheck;
				case "doNotCheck": return (int)SpellingModes.ksmDoNotCheck;
				default:
					throw new XmlSchemaException("Invalid spell check value: " + str);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the underline type for the specified string.
		/// </summary>
		/// <param name="str">The string.</param>
		/// ------------------------------------------------------------------------------------
		private static int GetUnderlineTypeForStr(string str)
		{
			switch (str)
			{
				case "none": return (int)FwUnderlineType.kuntNone;
				case "single": return (int)FwUnderlineType.kuntSingle;
				case "double": return (int)FwUnderlineType.kuntDouble;
				case "dashed": return (int)FwUnderlineType.kuntDashed;
				case "dotted": return (int)FwUnderlineType.kuntDotted;
				case "squiggle": return (int)FwUnderlineType.kuntSquiggle;
				case "strikethrough": return (int)FwUnderlineType.kuntStrikethrough;
				default:
					throw new XmlSchemaException("Invalid underline type: " + str);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the unit value for the specified atrribute.
		/// </summary>
		/// <param name="attr">The attribute.</param>
		/// ------------------------------------------------------------------------------------
		private static int GetUnitValueForStr(XAttribute attr)
		{
			if (attr == null)
				return (int)FwTextPropVar.ktpvMilliPoint;

			switch (attr.Value)
			{
				case "mpt": return (int)FwTextPropVar.ktpvMilliPoint;
				case "rel": return (int)FwTextPropVar.ktpvRelative;
				default:
					throw new XmlSchemaException("Invalid value for unit type: " + attr.Value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the GUID values for the specified string containing any number of GUIDs
		/// separated by spaces.
		/// </summary>
		/// <param name="str">The string.</param>
		/// ------------------------------------------------------------------------------------
		private static string GetGuidValuesForStr(string str)
		{
			StringBuilder builder = new StringBuilder();
			foreach (string guid in str.Split(' ', 'I'))
			{
				if (string.IsNullOrEmpty(guid))
					continue; // The split can do this. Ignore it.
				builder.Append(MiscUtils.GetObjDataFromGuid(new Guid(guid)));
			}
			return builder.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the integer value from the specified string.
		/// </summary>
		/// <param name="str">The string.</param>
		/// <param name="fAllowNegative"><c>True</c> to allow negative numbers, false otherwise.
		/// </param>
		/// ------------------------------------------------------------------------------------
		private static int GetIntValueFromStr(string str, bool fAllowNegative)
		{
			if (fAllowNegative)
				return int.Parse(str);

			try
			{
				return (int)uint.Parse(str);
			}
			catch (OverflowException)
			{
				throw new XmlSchemaException("Negative number is not valid for this attribute");
			}
			catch (FormatException)
			{
				throw new XmlSchemaException("Invalid number for this attribute: " + str);
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the integer representation of an 'on', 'off', or 'invert' value
		/// </summary>
		/// <param name="str">The string representation of the value.</param>
		/// ------------------------------------------------------------------------------------
		private static int GetToggleValueForStr(string str)
		{
			switch (str)
			{
				case "on": return (int)FwTextToggleVal.kttvForceOn;
				case "invert": return (int)FwTextToggleVal.kttvInvert;
				case "off": return (int)FwTextToggleVal.kttvOff;
				default:
					throw new XmlSchemaException("Invalid value for toggle attribute. Needed 'on', 'off', or 'invert'");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the color value for the specified string representation.
		/// </summary>
		/// <param name="str">The string representation.</param>
		/// ------------------------------------------------------------------------------------
		private static int GetColorValueForStr(string str)
		{
			switch (str)
			{
				case "black": return (int)FwTextColor.kclrBlack;
				case "blue": return (int)FwTextColor.kclrBlue;
				case "cyan": return (int)FwTextColor.kclrCyan;
				case "green": return (int)FwTextColor.kclrGreen;
				case "magenta": return (int)FwTextColor.kclrMagenta;
				case "red": return (int)FwTextColor.kclrRed;
				case "transparent": return (int)FwTextColor.kclrTransparent;
				case "white": return (int)FwTextColor.kclrWhite;
				case "yellow": return (int)FwTextColor.kclrYellow;
				default:
					try
					{
						return (int)ColorUtil.ConvertRGBtoBGR(uint.Parse(str, NumberStyles.HexNumber));
					}
					catch (FormatException)
					{
						throw new XmlSchemaException("Invalid value for color attribute: " + str);
					}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the alignment value for the specified string representation
		/// </summary>
		/// <param name="str">The string representation.</param>
		/// ------------------------------------------------------------------------------------
		private static int GetAlignValueForStr(string str)
		{
			switch (str)
			{
				case "center": return (int)FwTextAlign.ktalCenter;
				case "justify": return (int)FwTextAlign.ktalJustify;
				case "leading": return (int)FwTextAlign.ktalLeading;
				case "left": return (int)FwTextAlign.ktalLeft;
				case "right": return (int)FwTextAlign.ktalRight;
				case "trailing": return (int)FwTextAlign.ktalTrailing;
				default: throw new XmlSchemaException("Invalid value for align attribute: " + str);
			}
		}
		#endregion
	}
}
