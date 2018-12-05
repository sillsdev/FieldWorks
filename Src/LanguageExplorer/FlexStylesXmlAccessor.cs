// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.XPath;
using SIL.Code;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.Reporting;
using SIL.Xml;
using StyleInfo = LanguageExplorer.Controls.Styles.StyleInfo;

namespace LanguageExplorer
{
	/// <summary>
	/// Load the Flex factory styles or serializing to xml.
	/// </summary>
	/// <remarks>
	/// The class has to be public in order for the serialization to work.
	/// </remarks>
	[XmlRoot(ElementName = "Styles")]
	public sealed class FlexStylesXmlAccessor : IXmlSerializable
	{
		#region Data members
#if DEBUG
		private bool _versionUpdated;

#endif
		/// <summary>The progress dialog (may be null)</summary>
		private IProgress _progressDlg;
		/// <summary>The XElement from which to get the style info</summary>
		private XElement _sourceStyles;
		/// <summary>Styles to be renamed</summary>
		private Dictionary<string, string> _styleReplacements = new Dictionary<string, string>();
		/// <summary>Collection of styles in the DB</summary>
		private ILcmOwningCollection<IStStyle> _databaseStyles;
		Dictionary<IStStyle, IStStyle> _replacedStyles = new Dictionary<IStStyle, IStStyle>();
		/// <summary>Dictionary of style names to StStyle objects representing the initial
		/// collection of styles in the DB</summary>
		private Dictionary<string, IStStyle> _originalStyles = new Dictionary<string, IStStyle>();
		/// <summary>
		/// Dictionary of style names to StStyle objects representing the collection of
		/// styles that should be factory styles in the DB (i.e., any factory styles in the
		/// original Dictionary that are not also in this Dictionary need to be removed or turned
		/// into user-defined styles).
		/// </summary>
		private Dictionary<string, IStStyle> _updatedStyles = new Dictionary<string, IStStyle>();
		/// <summary>
		/// Maps from style name to ReservedStyleInfo.
		/// </summary>
		private Dictionary<string, ReservedStyleInfo> _reservedStyles = new Dictionary<string, ReservedStyleInfo>();
		/// <summary>
		/// This indicates if the style file being imported contains ALL styles, or if it should be considered a partial set.
		/// If it is a partial set we don't want to delete the missing styles.
		/// </summary>
		private bool _deleteMissingStyles;
		private readonly ILexDb _lexicon;
		private string _sourceDocumentPath;

		#endregion

		#region Construction

		/// <summary />
		private FlexStylesXmlAccessor()
		{ /* Parameterless constructor for the purposes of Xml serialization */ }

		/// <summary/>
		public FlexStylesXmlAccessor(ILexDb lexicon, bool loadDocument = false, string sourceDocument = null, bool prepareForTests = false)
		{
			Guard.AgainstNull(lexicon, nameof(lexicon));

			Cache = lexicon.Cache;
			_sourceDocumentPath = sourceDocument;
			_lexicon = lexicon;
			if (loadDocument)
			{
				_sourceStyles = LoadDoc();
				if (!string.IsNullOrEmpty(sourceDocument))
				{
					CreateStyles(new ConsoleProgress(), Cache.LangProject.StylesOC, _sourceStyles, false);
				}
			}
			else if (prepareForTests)
			{
				// Only to be used in basic tests that do not do the "loadDocument" option.
				_databaseStyles = Cache.LangProject.StylesOC;
				if (Cache.LangProject.TranslatedScriptureOA != null)
				{
					MoveStylesFromScriptureToLangProject();
				}
				// see class comment. This would not be normal behavior for a StylesXmlAccessor subclass constructor.
				foreach (var sty in _databaseStyles)
				{
					_originalStyles[sty.Name] = sty;
				}
			}
		}

		#endregion

		#region IXmlSerializable implementation

		/// <inheritdoc />
		public XmlSchema GetSchema()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Currently the reading is handled by CreateStyles
		/// </summary>
		/// <param name="reader"></param>
		public void ReadXml(XmlReader reader)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc />
		public void WriteXml(XmlWriter writer)
		{
			writer.WriteAttributeString("DTDver", DtdRequiredVersion);
			writer.WriteAttributeString("label", "Flex Dictionary");
			writer.WriteAttributeString("date", DateTime.UtcNow.ToString("yyyy-MM-dd"));
			writer.WriteStartElement("markup");
			writer.WriteAttributeString("version", GetVersion(_sourceStyles).ToString());
			foreach (var style in StyleCollection)
			{
				if (DictionaryConfigurationServices.UnsupportedStyles.Contains(style.Name))
				{
					continue;
				}
				var exportStyle = new ExportStyleInfo(style, style.Rules);
				WriteStyleXml(exportStyle, writer);
			}
			writer.WriteEndElement(); // markup
		}

		#endregion

		#region Properties

		/// <summary>
		/// The name (no path, no extension) of the settings file.
		/// For example, "FlexStyles"
		/// </summary>
		private string ResourceName => "FlexStyles";

		/// <summary>
		/// Gets the name (no path) of the settings file. This is the resource name with the
		/// correct file extension appended.
		/// For example, "FlexStyles.xml"
		/// If the external resource is not an XML file, override this to append an extension
		/// other than ".xml".
		/// </summary>
		private string ResourceFileName => $"{ResourceName}.xml";

		/// <summary>
		/// Gets the LcmCache
		/// </summary>
		private LcmCache Cache { get; }

		/// <summary>
		/// The collection that owns the styles; for example, Scripture.StylesOC.
		/// </summary>
		private ILcmOwningCollection<IStStyle> StyleCollection => Cache.LangProject.StylesOC;

		/// <summary>
		/// Gets the required DTD version.
		/// If the external resource is not an XML file, this can return null (no such
		/// implementations exist for now).
		/// </summary>
		private const string DtdRequiredVersion = "1610190E-D7A3-42D7-8B48-C0C49320435F";

		#endregion

		/// <summary>
		/// Special overridable method to allow application-specific overrides to allow a
		/// particular style to be renamed.
		/// </summary>
		/// <param name="styleName">Name of the original style.</param>
		/// <param name="replStyleName">Name of the replacement style.</param>
		/// <returns>The default always returns <c>false</c>; but an application may
		/// override this to return <c>true</c> for a specific pair of style names.</returns>
		private bool StyleReplacementAllowed(string styleName, string replStyleName)
		{
			return (styleName == "External Link" && replStyleName == "Hyperlink")
				   || (styleName == "Internal Link" && replStyleName == "Hyperlink")
				   || (styleName == "Language Code" && replStyleName == "Writing System Abbreviation");
		}

		/// <summary>
		/// If the current stylesheet version in the Db doesn't match that of the current XML
		/// file, update the DB.
		/// </summary>
		internal static void EnsureCurrentStylesheet(ILangProject lp, IThreadedProgress progressDlg)
		{
			// We don't need to establish a NonUndoableUnitOfWork here because caller has already
			// done it and if not, the internal code of StylesXmlAccessor will do it for us.
			var acc = new FlexStylesXmlAccessor(lp.LexDbOA);
			acc.EnsureCurrentResource(progressDlg);
		}

		/// <summary>
		/// Determines whether the given style is (possibly) in use.
		/// </summary>
		/// <param name="style">The style.</param>
		/// <returns><c>true</c> if there is any reasonable chance the given style is in use
		/// somewhere in the project data; <c>false</c> if the style has never been used and
		/// there is no real possibility it could be in the data.</returns>
		private bool StyleIsInUse(IStStyle style)
		{
			return style.Name == "External Link" || style.InUse;
		}

		/// <summary>
		/// Set the properties of a StyleInfo to the factory default settings
		/// </summary>
		internal void SetPropsToFactorySettings(StyleInfo styleInfo)
		{
			ResetProps(styleInfo);
		}

		/// <summary>
		/// Loads the settings file and checks the DTD version.
		/// </summary>
		/// <returns>The root element</returns>
		private XElement LoadDoc()
		{
			var sXmlFilePath = _sourceDocumentPath ?? Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer", ResourceFileName);
			try
			{
				var settings = new XmlReaderSettings
				{
					DtdProcessing = DtdProcessing.Parse
				};
				using (var reader = XmlReader.Create(sXmlFilePath, settings))
				{
					var doc = XDocument.Load(reader);

					var root = doc.Root;
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
				ReportInvalidInstallation(string.Format(LanguageExplorerResources.ksCannotLoadFile, sXmlFilePath, e.Message), e);
			}
			return null; // Can't actually get here. If you're name is Tim, tell it to the compiler.
		}

		private void WriteStyleXml(ExportStyleInfo style, XmlWriter writer)
		{
			writer.WriteStartElement("tag");
			writer.WriteAttributeString("id", GetStyleId(style));
			writer.WriteAttributeString("guid", style.RealStyle.Guid.ToString());
			writer.WriteAttributeString("userlevel", style.UserLevel.ToString());
			writer.WriteAttributeString("context", GetStyleContext(style));
			writer.WriteAttributeString("type", GetStyleType(style));

			if (GetStyleType(style) == "character" && style.InheritsFrom != null)
			{
				// LT-18267 Character styles put their basedOn in a different place
				// than paragraph styles.
				writer.WriteAttributeString("basedOn", GetStyleId(style.InheritsFrom));
			}

			WriteUsageElement(style.RealStyle.Usage, writer);
			WriteFontAndParagraphRulesXml(style, writer, style.InheritsFrom, style.NextStyle);
			writer.WriteEndElement(); // tag
		}

		private static string GetStyleType(ExportStyleInfo style)
		{
			switch (style.RealStyle.Type)
			{
				case StyleType.kstCharacter:
					return "character";
				case StyleType.kstParagraph:
					return "paragraph";
			}
			return style.RealStyle.Type.ToString();
		}

		///<remarks>The first letter for the context is supposed to be lower case</remarks>
		private static string GetStyleContext(ExportStyleInfo style)
		{
			var contextString = style.Context.ToString();
			if (string.IsNullOrEmpty(contextString))
			{
				throw new ArgumentException("The context in the style is invalid", nameof(style));
			}
			return contextString.Substring(0, 1).ToLowerInvariant() + contextString.Substring(1);
		}

		private void WriteUsageElement(IMultiUnicode styleUsage, XmlWriter writer)
		{
			foreach (var wsId in styleUsage.AvailableWritingSystemIds)
			{
				writer.WriteStartElement("usage");
				writer.WriteAttributeString("wsId", Cache.WritingSystemFactory.GetStrFromWs(wsId));
				writer.WriteString(styleUsage.get_String(wsId).Text);
				writer.WriteEndElement(); // usage
			}
		}

		private void WriteFontAndParagraphRulesXml(ExportStyleInfo style, XmlWriter writer, string basedOnStyle, BaseStyleInfo nextStyle)
		{
			if (style.FontInfoForWs(-1) == null)
			{
				writer.WriteStartElement("font");
				writer.WriteEndElement();
				return;
			}
			// Generate the font info (the font element is required by the DTD even if it has no attributes)
			writer.WriteStartElement("font");
			var fontProps = CollectFontProps(style.FontInfoForWs(-1));
			foreach (var prop in fontProps)
			{
				writer.WriteAttributeString(prop.Item1, prop.Item2);
			}
			foreach (var writingSystem in Cache.LangProject.AllWritingSystems)
			{
				var wsOverrideProps = CollectFontProps(style.FontInfoForWs(writingSystem.Handle)).ToList();
				if (wsOverrideProps.Any())
				{
					writer.WriteStartElement("override");
					writer.WriteAttributeString("wsId", writingSystem.LanguageTag);
					foreach (var prop in wsOverrideProps)
					{
						writer.WriteAttributeString(prop.Item1, prop.Item2);
					}
					writer.WriteEndElement();
				}
			}
			writer.WriteEndElement(); // font
			var paragraphProps = CollectParagraphProps(style, basedOnStyle, nextStyle).ToList();
			if (paragraphProps.Any())
			{
				writer.WriteStartElement("paragraph");
				foreach (var prop in paragraphProps)
				{
					writer.WriteAttributeString(prop.Item1, prop.Item2);
				}

				//Bullet/Number FontInfo
				try
				{
					var bulletPropertyMap = new Dictionary<string, string>
					{
						{"numberscheme", "bulNumScheme"},
						{"startat", "bulNumStartAt"},
						{"textafter", "bulNumTxtAft"},
						{"textbefore", "bulNumTxtBef"},
						{"bulletcustom", "bulCusTxt"}
					};
					var bulNumParaProperty = CollectBulletProps(style.BulletInfo);
					foreach (var prop in bulNumParaProperty)
					{
						var propName = prop.Item1;
						if (bulletPropertyMap.ContainsKey(propName.ToLower()))
						{
							propName = bulletPropertyMap[propName.ToLower()];
						}
						writer.WriteAttributeString(propName, prop.Item2);
					}
					// Generate the font info (the font element is required by the DTD even if it has no attributes)
					writer.WriteStartElement("BulNumFontInfo");
					foreach (var prop in CollectFontProps(style.BulletInfo.FontInfo))
					{
						writer.WriteAttributeString(prop.Item1, prop.Item2);
					}
					writer.WriteEndElement(); // bullet
				}
				catch { }
				writer.WriteEndElement(); // paragraph
			}
		}

		/// <summary>
		/// Collects the font info for the style in tuples of attribute name, attribute value
		/// </summary>
		private IEnumerable<Tuple<string, string>> CollectFontProps(FontInfo styleRules)
		{
			var fontProperties = new List<Tuple<string, string>>();
			if (styleRules.FontSize.ValueIsSet)
			{
				fontProperties.Add(new Tuple<string, string>("size", styleRules.FontSize.Value / 1000 + " pt"));
			}
			if (styleRules.FontName.ValueIsSet)
			{
				fontProperties.Add(new Tuple<string, string>("family", styleRules.FontName.Value));
			}
			if (styleRules.Bold.ValueIsSet)
			{
				fontProperties.Add(new Tuple<string, string>("bold", styleRules.Bold.Value.ToString().ToLowerInvariant()));
			}
			if (styleRules.Italic.ValueIsSet)
			{
				fontProperties.Add(new Tuple<string, string>("italic", styleRules.Italic.Value.ToString().ToLowerInvariant()));
			}
			GetColorValueAttribute("backcolor", styleRules.BackColor, fontProperties);
			GetColorValueAttribute("color", styleRules.FontColor, fontProperties);
			GetColorValueAttribute("underlineColor", styleRules.UnderlineColor, fontProperties);
			if (styleRules.Underline.ValueIsSet)
			{
				string underLineValue;
				switch (styleRules.Underline.Value)
				{
					case FwUnderlineType.kuntStrikethrough:
						underLineValue = "strikethrough";
						break;
					case FwUnderlineType.kuntSingle:
						underLineValue = "single";
						break;
					case FwUnderlineType.kuntDouble:
						underLineValue = "double";
						break;
					case FwUnderlineType.kuntDashed:
						underLineValue = "dashed";
						break;
					case FwUnderlineType.kuntDotted:
						underLineValue = "dotted";
						break;
					case FwUnderlineType.kuntSquiggle:
						underLineValue = "squiggle";
						break;
					default:
						underLineValue = "none";
						break;
				}
				fontProperties.Add(new Tuple<string, string>("underline", underLineValue));
			}
			return fontProperties;
		}

		/// <summary>
		/// Collects the paragraph info for the style in tuples of attribute name, attribute value
		/// </summary>
		private IEnumerable<Tuple<string, string>> CollectParagraphProps(ExportStyleInfo styleRules, string basedOnStyle, BaseStyleInfo nextStyle)
		{
			var paragraphProps = new List<Tuple<string, string>>();
			GetPointPropAttribute((int)FwTextPropType.ktptSpaceBefore, "spaceBefore", styleRules.RealStyle.Rules, paragraphProps);
			GetPointPropAttribute((int)FwTextPropType.ktptSpaceAfter, "spaceAfter", styleRules.RealStyle.Rules, paragraphProps);
			GetPointPropAttribute((int)FwTextPropType.ktptLeadingIndent, "indentLeft", styleRules.RealStyle.Rules, paragraphProps);
			GetPointPropAttribute((int)FwTextPropType.ktptTrailingIndent, "indentRight", styleRules.RealStyle.Rules, paragraphProps);
			GetColorValueAttribute((int)FwTextPropType.ktptBackColor, "background", styleRules.RealStyle.Rules, paragraphProps);
			if (basedOnStyle != null)
			{
				paragraphProps.Add(new Tuple<string, string>("basedOn", GetStyleId(basedOnStyle)));
			}
			if (nextStyle != null)
			{
				paragraphProps.Add(new Tuple<string, string>("next", GetStyleId(nextStyle)));
			}
			if (styleRules.HasFirstLineIndent)
			{
				// hanging and firstLine are stored in an overloaded property value, negative for hanging, positive for firstline
				paragraphProps.Add(styleRules.FirstLineIndent < 0
					? new Tuple<string, string>("hanging", -(styleRules.FirstLineIndent / 1000) + " pt")
					: new Tuple<string, string>("firstLine", styleRules.FirstLineIndent / 1000 + " pt"));
			}
			if (styleRules.HasAlignment)
			{
				var alignment = styleRules.Alignment;
				var alignValue = "none";
				switch (alignment)
				{
					case FwTextAlign.ktalCenter:
						alignValue = "center";
						break;
					case FwTextAlign.ktalLeft:
						alignValue = "left";
						break;
					case FwTextAlign.ktalRight:
						alignValue = "right";
						break;
					case FwTextAlign.ktalJustify:
						alignValue = "full";
						break;
				}
				paragraphProps.Add(new Tuple<string, string>("alignment", alignValue));
			}

			if (!styleRules.HasLineSpacing)
			{
				return paragraphProps;
			}
			string lineSpaceType;
			// relative is used for single, 1.5, double space
			if (styleRules.LineSpacing.m_relative)
			{
				lineSpaceType = "rel";
			}
			else if (styleRules.LineSpacing.m_lineHeight <= 0)
			{
				// for historical reasons negative values mean exact, and positive mean at least
				// (see: Framework\StylesXmlAccessor.cs SetParagraphProperties())
				lineSpaceType = "exact";
			}
			else
			{
				lineSpaceType = "atleast";
			}
			var lineSpace = Math.Abs(styleRules.LineSpacing.m_lineHeight) / 1000 + " pt";
			paragraphProps.Add(new Tuple<string, string>("lineSpacing", lineSpace));
			paragraphProps.Add(new Tuple<string, string>("lineSpacingType", lineSpaceType));

			return paragraphProps;
		}

		/// <summary>
		/// Collects the bullet info for the style in tuples of attribute name, attribute value
		/// </summary>
		private IEnumerable<Tuple<string, string>> CollectBulletProps(BulletInfo styleRules)
		{
			var bulletProperties = new List<Tuple<string, string>>();
			if (styleRules.m_numberScheme.ToString().Length > 0)
			{
				string bulletNumberScheme;
				switch (styleRules.m_numberScheme)
				{
					case VwBulNum.kvbnNone:
						bulletNumberScheme = "None";
						break;
					case VwBulNum.kvbnArabic:
						bulletNumberScheme = "Arabic";
						break;
					case VwBulNum.kvbnRomanUpper:
						bulletNumberScheme = "RomanUpper";
						break;
					case VwBulNum.kvbnRomanLower:
						bulletNumberScheme = "RomanLower";
						break;
					case VwBulNum.kvbnLetterUpper:
						bulletNumberScheme = "LetterUpper";
						break;
					case VwBulNum.kvbnLetterLower:
						bulletNumberScheme = "LetterLower";
						break;
					case VwBulNum.kvbnArabic01:
						bulletNumberScheme = "Arabic01";
						break;
					case VwBulNum.kvbnBullet:
						bulletNumberScheme = "Custom";
						break;
					default:
						bulletNumberScheme = styleRules.m_numberScheme.ToString();
						break;
				}
				bulletProperties.Add(new Tuple<string, string>("numberScheme", bulletNumberScheme));
			}
			if (!string.IsNullOrEmpty(styleRules.m_bulletCustom))
			{
				bulletProperties.Add(new Tuple<string, string>("bulletCustom", styleRules.m_bulletCustom.ToLowerInvariant()));
			}
			if (styleRules.m_start.ToString().Length > 0)
			{
				bulletProperties.Add(new Tuple<string, string>("startAt", styleRules.m_start.ToString().ToLowerInvariant()));
			}
			if (!string.IsNullOrEmpty(styleRules.m_textBefore))
			{
				bulletProperties.Add(new Tuple<string, string>("textBefore", styleRules.m_textBefore.ToLowerInvariant()));
			}
			if (!string.IsNullOrEmpty(styleRules.m_textAfter))
			{
				bulletProperties.Add(new Tuple<string, string>("textAfter", styleRules.m_textAfter.ToLowerInvariant()));
			}
			return bulletProperties;
		}

		/// <summary>
		/// Takes the property identifier integer and the attribute name that we want to use in the xml and generates a tuple
		/// with the attribute name and value if this property is set in the style rules. This method assumes the property is
		/// for a size value stored in millipoints.
		/// </summary>
		private static void GetPointPropAttribute(int property, string attributeName, ITsTextProps styleRules, List<Tuple<string, string>> resultsList)
		{
			if (styleRules == null)
			{
				return;
			}
			int hasProperty;
			var propValue = styleRules.GetIntPropValues(property, out hasProperty);
			if (hasProperty != -1)
			{
				resultsList.Add(new Tuple<string, string>(attributeName, propValue / 1000 + " pt"));
			}
		}

		/// <summary>
		/// Takes the property identifier integer  and the attribute name that we want to use in the xml and generates a tuple
		/// with the attribute name and value if this property is set in the style rules. This method assumes the property
		/// is for a color value.
		/// </summary>
		private static void GetColorValueAttribute(int property, string attributeName, ITsTextProps styleRules, List<Tuple<string, string>> resultsList)
		{
			if (styleRules == null)
			{
				return;
			}
			int hasColor;
			var colorValueBGR = styleRules.GetIntPropValues(property, out hasColor);
			if (hasColor == -1)
			{
				return;
			}
			var color = Color.FromArgb((int)ColorUtil.ConvertRGBtoBGR((uint)colorValueBGR)); // convert BGR to RGB
			GetColorValueFromSystemColor(attributeName, color, resultsList);
		}

		private void GetColorValueAttribute(string attributeName, IStyleProp<Color> fontColor, List<Tuple<string, string>> resultsList)
		{
			if (fontColor.ValueIsSet)
			{
				var color = fontColor.Value;
				GetColorValueFromSystemColor(attributeName, color, resultsList);
			}
		}

		/// <summary>
		/// Takes a system color and writes out a string if it is a known color, or an RGB value that the import code can read
		/// </summary>
		private static void GetColorValueFromSystemColor(string attributeName, Color color, List<Tuple<string, string>> resultsList)
		{
			if (color.IsEmpty)
			{
				return;
			}
			var colorString = color.IsKnownColor ? color.Name.ToLowerInvariant() : $"({color.R},{color.G},{color.B})";
			resultsList.Add(new Tuple<string, string>(attributeName, colorString));
		}

		/// <summary>
		/// Converts the style name into the 'id' attribute expected by the code that reads in stylesheet files
		/// </summary>
		private static string GetStyleId(BaseStyleInfo style) { return GetStyleId(style.Name); }

		/// <summary>
		/// Converts the style name into the 'id' attribute expected by the code that reads in stylesheet files
		/// </summary>
		private static string GetStyleId(string styleName)
		{
			return styleName.Replace(' ', '_');
		}

		/// <summary />
		private Guid GetVersion(XElement baseElement)
		{
			return new Guid(GetMarkupElement(baseElement).Attribute("version").Value);
		}

		/// <summary>
		/// Gets the markup element from the given root element.
		/// </summary>
		private static XElement GetMarkupElement(XElement rootElement)
		{
			return rootElement.Element("markup");
		}

		/// <summary>
		/// Checks the DTD version.
		/// </summary>
		/// <param name="rootElement">The root element (which holds the DTD version attribute).</param>
		/// <param name="xmlSettingsFileName">Name of the XML settings file.</param>
		private void CheckDtdVersion(XElement rootElement, string xmlSettingsFileName)
		{
			var dtdVersion = rootElement.Attribute("DTDver");
			if (dtdVersion == null || dtdVersion.Value != DtdRequiredVersion)
			{
				throw new Exception(string.Format(LanguageExplorerResources.kstidIncompatibleDTDVersion, xmlSettingsFileName, DtdRequiredVersion));
			}
		}

		/// <summary>
		/// Determines whether the specified resource is out-of-date (or not created).
		/// </summary>
		/// <param name="newVersion">The latest version (i.e., the version from the resource
		/// file).</param>
		private bool IsResourceOutdated(Guid newVersion)
		{
			// Get the current version of the settings used in this project.
			var resource = Resource;
			return resource == null || newVersion != resource.Version;
		}

		/// <summary>
		/// Sets the new resource version in the DB.
		/// </summary>
		private void SetNewResourceVersion(Guid newVersion)
		{
			var resource = Resource;
			if (resource == null)
			{
				// Resource does not exist yet. Add it to the collection.
				var newResource = Cache.ServiceLocator.GetInstance<ICmResourceFactory>().Create();
				_lexicon.ResourcesOC.Add(newResource);
				newResource.Name = ResourceName;
				newResource.Version = newVersion;
#if DEBUG
				_versionUpdated = true;
#endif
				return;
			}

			resource.Version = newVersion;
#if DEBUG
			_versionUpdated = true;
#endif
		}

		/// <summary>
		/// Load all styles from the XML file and create styles in the database for them.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">The parameters. First parameter is the style objects
		/// (a LcmOwningCollection&lt;IStStyle&gt;), second is the styles (an XElement).</param>
		private object CreateStyles(IProgress progressDlg, params object[] parameters)
		{
			Debug.Assert(parameters.Length == 3);
			_databaseStyles = (ILcmOwningCollection<IStStyle>)parameters[0];
			_sourceStyles = (XElement)parameters[1];
			_deleteMissingStyles = (bool)parameters[2];
			_progressDlg = progressDlg;

			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor, CreateStyles);

			return null;
		}

		/// <summary>
		/// Reset the properties of a StyleInfo to the factory default settings
		/// </summary>
		private void ResetProps(StyleInfo styleInfo)
		{
			styleInfo.ResetAllPropertiesToFactoryValues(() =>
			{
				var styleElement = LoadDoc().XPathSelectElement("markup/tag[@id='" + styleInfo.Name.Replace(" ", "_") + "']");
				SetFontProperties(styleInfo.Name, styleElement, (tpt, iVar, iVal) => styleInfo.SetExplicitFontIntProp(tpt, iVal), (tpt, sVal) =>
				{
					if (tpt == (int)FwTextPropType.ktptWsStyle)
					{
						styleInfo.ProcessWsSpecificOverrides(sVal);
					}
					else
					{
						throw new InvalidEnumArgumentException("tpt", tpt, typeof(FwTextPropType));
					}
				},
				OverwriteOptions.All);
				if (styleInfo.IsParagraphStyle)
				{
					SetParagraphProperties(styleInfo.Name, styleElement, (tpt, iVar, iVal) =>
					{
						if (!styleInfo.SetExplicitParaIntProp(tpt, iVar, iVal))
						{
							throw new InvalidEnumArgumentException("tpt", tpt, typeof(FwTextPropType));
						}
					},
					(tpt, sVal) =>
					{
						if (tpt == (int)FwTextPropType.ktptWsStyle)
						{
							styleInfo.ProcessWsSpecificOverrides(sVal);
						}
						else
						{
							throw new InvalidEnumArgumentException("tpt", tpt, typeof(FwTextPropType));
						}
					},
					OverwriteOptions.All);
				}
			});
		}

		/// <summary>
		/// Create a set of Scripture styles based on the given XML element.
		/// </summary>
		private void CreateStyles()
		{
			var label = XmlUtils.GetOptionalAttributeValue(_sourceStyles, "label", string.Empty);
			_progressDlg.Title = string.Format(ResourceHelper.GetResourceString("kstidCreatingStylesCaption"), label);
			_progressDlg.Message = string.Format(ResourceHelper.GetResourceString("kstidCreatingStylesStatusMsg"), string.Empty);
			_progressDlg.Position = 0;

			// Move all styles from Scripture into LangProject if the Scripture object exists
			MoveStylesFromScriptureToLangProject();

			// Populate hashtable with initial set of styles
			// these are NOT from the *Styles.xml files or from TeStylesXmlAccessor.InitReservedStyles()
			// They are from loading scripture styles in TE tests only.
			foreach (var sty in _databaseStyles)
			{
				_originalStyles[sty.Name] = sty;
			}

			//Select all styles.
			var markup = GetMarkupElement(_sourceStyles);
			var tagList = markup.Elements("tag").ToList();

			_progressDlg.Minimum = 0;
			_progressDlg.Maximum = tagList.Count() * 2;

			// First pass to create styles and set general properties.
			CreateAndUpdateStyles(tagList);

			// Second pass to set up "based-on" and "next" styles
			SetBasedOnAndNextProps(tagList);

			// Third pass to delete (and possibly prepare to rename) any styles that used to be
			// factory styles but aren't any longer
			if (_deleteMissingStyles)
			{
				DeleteDeprecatedStylesAndDetermineReplacements();
			}

			// Final step is to walk through the DB and relace any retired styles
			ReplaceFormerStyles();

			// Finally, update styles version in database.
			SetNewResourceVersion(GetVersion(_sourceStyles));
		}

		/// <summary>
		/// Moves styles that were specific to Scripture into the language project. Projects will have just one style sheet
		/// that will be used throughout the project, including imported scripture.
		/// </summary>
		private void MoveStylesFromScriptureToLangProject()
		{
			var scr = Cache.LangProject.TranslatedScriptureOA;
			if (scr == null)
			{
				return;
			}
			foreach (var style in scr.StylesOC)
			{
				if (_databaseStyles.Any(st => st.Name == style.Name))
				{
					// We found a style with the same name as one already in out language project. Just use the one we already have.
					var flexStyle = _databaseStyles.First(st => st.Name == style.Name);
					DomainObjectServices.ReplaceReferencesWhereValid(style, flexStyle);
					scr.StylesOC.Remove(style);
					continue;
				}
				// Adding the style to our database will automatically remove the style from Scripture.
				_databaseStyles.Add(style);
			}
		}

		/// <summary>
		/// First pass of style creation phase: create any new styles and set/update general
		/// properties
		/// </summary>
		/// <param name="tagList">List of XML element representing factory styles to create</param>
		private void CreateAndUpdateStyles(List<XElement> tagList)
		{
			foreach (var styleTag in tagList)
			{
				var styleName = GetStyleName(styleTag);
				// Don't create a style for certain excluded contexts
				var context = GetContext(styleTag, styleName);
				_progressDlg.Step(0);
				_progressDlg.Message = string.Format(ResourceHelper.GetResourceString("kstidCreatingStylesStatusMsg"), styleName);
				var styleType = GetType(styleTag, styleName, context);
				var structure = GetStructure(styleTag, styleName);
				var function = GetFunction(styleTag, styleName);
				var atGuid = styleTag.Attribute("guid");

				if (atGuid == null || string.IsNullOrEmpty(atGuid.Value))
				{
					ReportInvalidInstallation(string.Format(ResourceHelper.GetResourceString("ksNoGuidOnFactoryStyle"), styleName));
				}
				var factoryGuid = new Guid(atGuid.Value);
				var style = FindOrCreateStyle(styleName, styleType, context, structure, function, factoryGuid);
				if (_reservedStyles.ContainsKey(styleName))
				{
					var info = _reservedStyles[styleName];
					info.created = true;
				}

				// set the user level
				style.UserLevel = int.Parse(styleTag.Attribute("userlevel").Value);

				// Set the usage info
				foreach (var usage in styleTag.Elements("usage"))
				{
					var ws = GetWs(usage);
					var usageInfo = usage.GetInnerText();
					if (ws > 0 && !string.IsNullOrEmpty(usageInfo))
					{
						style.Usage.set_String(ws, TsStringUtils.MakeString(usageInfo, ws));
					}
				}

				// If the user has modified the style manually, we don't want to overwrite it
				// with the standard definition.
				// Enhance JohnT: possibly there should be some marker in the XML to indicate that
				// a style has changed so drastically that it SHOULD overwrite the user modifications?
				const OverwriteOptions option = OverwriteOptions.All;
				// Get props builder with default Text Properties
				var propsBldr = TsStringUtils.MakePropsBldr();
				if (style.IsModified)
				{
					continue;
				}

				SetFontProperties(styleName, styleTag,
					(tpt, nVar, nVal) => _progressDlg.SynchronizeInvoke.Invoke(() => propsBldr.SetIntPropValues(tpt, nVar, nVal)),
					(tpt, sVal) => _progressDlg.SynchronizeInvoke.Invoke(() => propsBldr.SetStrPropValue(tpt, sVal)), option);

				// Get paragraph properties
				if (style.Type == StyleType.kstParagraph)
				{
					SetParagraphProperties(styleName, styleTag,
						(tpt, nVar, nVal) => _progressDlg.SynchronizeInvoke.Invoke(() => propsBldr.SetIntPropValues(tpt, nVar, nVal)),
						(tpt, sVal) => _progressDlg.SynchronizeInvoke.Invoke(() => propsBldr.SetStrPropValue(tpt, sVal)), option);
				}
				style.Rules = propsBldr.GetTextProps();
			}
		}

		/// <summary>
		/// Find the existing style if there is one; otherwise, create new style object
		/// These styles are defined in FlexStyles.xml which have fixed guids
		/// All guids of factory styles changed with release 7.3
		/// </summary>
		/// <remarks>
		/// NB: This is only internal because a test uses it. Otherwise, it could be private.
		/// </remarks>
		internal IStStyle FindOrCreateStyle(string styleName, StyleType styleType, ContextValues context, StructureValues structure, FunctionValues function, Guid factoryGuid)
		{
			IStStyle style;
			var fUsingExistingStyle = false;
			// EnsureCompatibleFactoryStyle will rename an incompatible user style to prevent collisions,
			// but it is our responsibility to update the GUID on a compatible user style.
			if (_originalStyles.TryGetValue(styleName, out style) && EnsureCompatibleFactoryStyle(style, styleType, context, structure, function))
			{
				// A style with the same name already exists in the project.
				// It may be a user style or a factory style, but it has compatible context, structure, and function.
				if (style.Guid != factoryGuid) // This is a user style; give it the factory GUID and update all usages
				{
					// create a new style with the correct guid.
					var oldStyle = style; // REVIEW LastufkaM 2012.05: is there a copy constructor?
					style = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create(Cache, factoryGuid);
					// Before we set any data on the new style we should give it an owner.
					// Don't delete the old one yet, though, because we want to copy things from it (and references to it).
					var owningCollection = ((ILangProject)oldStyle.Owner).StylesOC;
					owningCollection.Add(style);

					style.IsBuiltIn = true; // whether or not it was before, it is now.
					style.IsModified = oldStyle.IsModified;
					style.Name = oldStyle.Name;
					style.Type = oldStyle.Type;
					style.Context = oldStyle.Context;
					style.Structure = oldStyle.Structure;
					style.Function = oldStyle.Function;
					style.Rules = oldStyle.Rules;
					style.BasedOnRA = oldStyle.BasedOnRA;
					style.IsPublishedTextStyle = oldStyle.IsPublishedTextStyle;
					style.NextRA = oldStyle.NextRA;
					style.UserLevel = oldStyle.UserLevel;

					// Anywhere the obsolete style object is used (e.g., in BasedOn or Next of other styles),
					// switch to refer to the new one. It's important to do this AFTER setting all the properties,
					// because validation of setting various references to this style depends on some of these properties.
					// (Also, oldNextRA might be oldStyle itself.)
					// It must be done AFTER the new style has an owner, but BEFORE the old one is deleted (and all refs
					// to it go away).
					// In pathological cases this might not be valid (e.g., the old stylesheet may somehow have invalid
					// arrangements of NextStyle). If so, just let those references stay for now (and be cleared when the old style
					// is deleted).
					DomainObjectServices.ReplaceReferencesWhereValid(oldStyle, style);
					owningCollection.Remove(oldStyle);
				}
				_updatedStyles[styleName] = style; // REVIEW (Hasso) 2017.04: any reason this is shoved in the middle here? Parallel or UOW reasons, perhaps?
			}
			else
			{
				// These factory styles aren't in the project yet.
				// WARNING: Using this branch may create ownerless StStyle objects! Shouldn't be possible!
				style = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create(Cache, factoryGuid);
				_databaseStyles.Add(style);
				if (style.Owner == null)
				{
					throw new ApplicationException("StStyle objects must be owned!");
				}
				_updatedStyles[styleName] = style; // REVIEW (Hasso) 2017.04: any reason this is shoved in the middle here? Parallel or UOW reasons, perhaps?

				// Set properties not passed in as parameters
				style.IsBuiltIn = true;
				style.IsModified = false; // not found in our database, so use everything from the XML

				// Set the style name, type, context, structure, and function
				style.Name = styleName;
				style.Type = styleType;
				style.Context = context;
				style.Structure = structure;
				style.Function = function;
			}
			return style;
		}

		/// <summary>
		/// Throws an exception. Release mode overrides the message.
		/// </summary>
		/// <param name="message">The message to display (in debug mode)</param>
		/// <param name="e">Optional inner exception</param>
		private static void ReportInvalidInstallation(string message, Exception e = null)
		{
			Logger.WriteEvent(message); // This is so we get the actual error in release builds
#if !DEBUG
			message = ResourceHelper.GetResourceString("kstidInvalidInstallation");
#endif
			throw new InstallationException(message, e);
		}

		/// <summary>
		/// Make sure the stylesheet for the specified object is current.
		/// </summary>
		/// <param name="progressDlg">The progress dialog if one is already up.</param>
		private void EnsureCurrentResource(IThreadedProgress progressDlg)
		{
			var doc = LoadDoc();
			Guid newVersion;
			try
			{
				newVersion = GetVersion(doc);
			}
			catch (Exception e)
			{
				ReportInvalidInstallation(string.Format(LanguageExplorerResources.ksInvalidResourceFileVersion, ResourceFileName), e);
				newVersion = Guid.Empty;
			}

			// Re-load the factory settings if they are not at current version.
			if (IsResourceOutdated(newVersion))
			{
				progressDlg.RunTask(CreateStyles, StyleCollection, doc, true);
#if DEBUG
				Debug.Assert(_versionUpdated);
#endif
			}
		}

		/// <summary>
		/// Gets the requested resource.
		/// </summary>
		private ICmResource Resource => _lexicon.ResourcesOC.ToArray().FirstOrDefault(res => res.Name.Equals(ResourceName));

		/// <summary>
		/// Interpret the based on attribute
		/// </summary>
		/// <param name="element">Element that better have a "context" attribute</param>
		/// <param name="styleName">Style name being processed (for error reporting purposes)
		/// </param>
		/// <returns>The name of the based-on style</returns>
		private string GetBasedOn(XElement element, string styleName)
		{
			return _reservedStyles.ContainsKey(styleName) ? _reservedStyles[styleName].basedOn : element.Attribute("basedOn")?.Value.Replace("_", " ");
		}

		/// <summary>
		/// Interpret the context attribute as a ContextValues value
		/// </summary>
		/// <param name="element">Element that better have a "context" attribute</param>
		/// <param name="styleName">Style name being processed (for error reporting purposes)
		/// </param>
		/// <returns>The context of the style</returns>
		private ContextValues GetContext(XElement element, string styleName)
		{
			if (_reservedStyles.ContainsKey(styleName))
			{
				return _reservedStyles[styleName].context;
			}
			var sContext = element.Attribute("context").Value;
			// EndMarker was left out of the original conversion and would have raised an exception.
			if (sContext == "back")
			{
				sContext = "BackMatter";
			}
			try
			{   // convert the string to a valid enum case insensitive
				return (ContextValues)Enum.Parse(typeof(ContextValues), sContext, true);
			}
			catch (Exception ex)
			{
				Debug.Assert(false, $"Unrecognized context attribute for style {styleName} in {ResourceFileName}: {sContext}");
				throw new Exception(ResourceHelper.GetResourceString("kstidInvalidInstallation"));
			}
		}

		/// <summary>
		/// Interpret the use attribute as a StructureValues value
		/// </summary>
		/// <param name="element">Element that better have a "structure" attribute</param>
		/// <param name="styleName">Style name being processed (for error reporting purposes)
		/// </param>
		/// <returns>The structure of the style</returns>
		private StructureValues GetStructure(XElement element, string styleName)
		{
			if (_reservedStyles.ContainsKey(styleName))
			{
				return _reservedStyles[styleName].structure;
			}
			var attribute = element.Attribute("structure");
			var sStructure = attribute?.Value;
			if (sStructure == null)
			{
				return StructureValues.Undefined;
			}
			switch (sStructure)
			{
				case "heading":
					return StructureValues.Heading;
				case "body":
					return StructureValues.Body;
				default:
					Debug.Assert(false, $"Unrecognized structure attribute for style {styleName} in {ResourceFileName}: {sStructure}");
					throw new Exception(ResourceHelper.GetResourceString("kstidInvalidInstallation"));
			}
		}

		/// <summary>
		/// Interpret the use attribute as a FunctionValues value
		/// </summary>
		/// <param name="element">Element that better have a "use" attribute</param>
		/// <param name="styleName">Style name being processed (for error reporting purposes)
		/// </param>
		/// <returns>The function of the style</returns>
		private FunctionValues GetFunction(XElement element, string styleName)
		{
			if (_reservedStyles.ContainsKey(styleName))
			{
				return _reservedStyles[styleName].function;
			}
			var attribute = element.Attribute("use");
			var sFunction = attribute?.Value;
			if (sFunction == null)
			{
				return FunctionValues.Prose;
			}
			switch (sFunction)
			{
				case "prose":
				case "proseSentenceInitial":
				case "title":
				case "properNoun":
				case "special":
					return FunctionValues.Prose;
				case "line":
				case "lineSentenceInitial":
					return FunctionValues.Line;
				case "list":
					return FunctionValues.List;
				case "table":
					return FunctionValues.Table;
				case "chapter":
					return FunctionValues.Chapter;
				case "verse":
					return FunctionValues.Verse;
				case "footnote":
					return FunctionValues.Footnote;
				case "stanzabreak":
					return FunctionValues.StanzaBreak;
				default:
					Debug.Assert(false, $"Unrecognized use attribute for style {styleName} in {ResourceFileName}: {sFunction}");
					throw new Exception(ResourceHelper.GetResourceString("kstidInvalidInstallation"));
			}
		}

		/// <summary>
		/// Interpret the type attribute as a StyleType value
		/// </summary>
		/// <param name="element">Element that better have a "type" attribute</param>
		/// <param name="styleName">Style name being processed (for error reporting purposes)
		/// </param>
		/// <param name="context"></param>
		/// <returns>The type of the style</returns>
		private StyleType GetType(XElement element, string styleName, ContextValues context)
		{
			if (_reservedStyles.ContainsKey(styleName))
			{
				return _reservedStyles[styleName].styleType;
			}
			var sType = element.Attribute("type").Value;
			if (context != ContextValues.InternalConfigureView &&
				context != ContextValues.Internal &&
				context != ContextValues.General &&
				context != ContextValues.Book &&
				context != ContextValues.Text &&
				context != ContextValues.PsuedoStyle &&
				context != ContextValues.InternalMappable &&
				context != ContextValues.Note &&
				context != ContextValues.Title)
			{
				ReportInvalidInstallation($"Style {styleName} is illegally defined with context '{context}' in {ResourceFileName}.");
			}
			switch (sType)
			{
				case "paragraph":
					return StyleType.kstParagraph;
				case "character":
					return StyleType.kstCharacter;
				default:
					Debug.Assert(false, $"Unrecognized type attribute for style {styleName} in {ResourceFileName}: {sType}");
					throw new Exception(ResourceHelper.GetResourceString("kstidInvalidInstallation"));
			}
		}

		/// <summary>
		/// Determine whether the given style is compatible with the given type, context, structure, and function.
		/// If the style is a factory style, and the context, structure, and function can't be adjusted to match, report an invalid installation.
		/// If the style is NOT a factory style, and the context, structure, and function don't all match, rename it to prevent collisions.
		/// If the style is not a factory style, but it is compatible, it is the CLIENT's responsibility to make adjustments.
		/// </summary>
		/// <param name="style">Style to check.</param>
		/// <param name="type">Style type we want</param>
		/// <param name="context">The context we want</param>
		/// <param name="structure">The structure we want</param>
		/// <param name="function">The function we want</param>
		/// <returns>True if the style can be used as-is or redefined as requested; False otherwise</returns>
		private bool EnsureCompatibleFactoryStyle(IStStyle style, StyleType type, ContextValues context, StructureValues structure, FunctionValues function)
		{
			// Handle an incompatible Style by renaming a conflicting User style or reporting an invalid installation for an incompatible built-in style.
			if (style.Type != type || !CompatibleContext(style.Context, context) || style.Structure != structure || style.Function != function)
			{
				if (style.IsBuiltIn)
				{
					ReportInvalidInstallation(string.Format(LanguageExplorerResources.ksCannotRedefineFactoryStyle, style.Name, ResourceFileName));
				}
				// If style is in use, add it to the list so we can search through all
				// paragraphs and replace it with a new renamed style (and rename the style
				// itself, too);
				if (StyleIsInUse(style))
				{
					// ENHANCE: Prompt user to pick a different name to rename to
					// TODO: Check for collision - make sure we get a unique name
					var sNewName = style.Name + ResourceHelper.GetResourceString("kstidUserStyleSuffix");
					_styleReplacements[style.Name] = sNewName;
					style.Name = sNewName;
				}
				return false;
			}

			// Update context and function as needed
			if (style.Context != context)
			{
				style.Context = context;
			}
			if (style.Function != function)
			{
				style.Function = function;
			}
			return true;
		}

		/// <summary>
		/// Determine whether the newly proposed context for a style is compatible with its
		/// current context.
		/// </summary>
		/// <param name="currContext">The existing context of the style</param>
		/// <param name="proposedContext">The context we want</param>
		/// <returns><c>true </c>if the passed in context can be upgraded as requested;
		/// <c>false</c> otherwise.</returns>
		public static bool CompatibleContext(ContextValues currContext, ContextValues proposedContext)
		{
			if (currContext == proposedContext)
			{
				return true;
			}
			// Internal and InternalMappable are mutually compatible
			if (currContext == ContextValues.InternalMappable && proposedContext == ContextValues.Internal ||
				proposedContext == ContextValues.InternalMappable && currContext == ContextValues.Internal)
			{
				return true;
			}
			// A (character) style having a specific Context can be made General
			return (proposedContext == ContextValues.General);
		}

		/// <summary>
		/// Third pass of style creation phase: delete (or make as user-defined styles) any
		/// styles that used to be factory styles but aren't any longer. If a deprecated style
		/// is being renamed or replaced with another style (which should already be created by
		/// now), add the replacement to the list (final step of style update process will be to
		/// crawl through the DB and replace all uses).
		/// </summary>
		private void DeleteDeprecatedStylesAndDetermineReplacements()
		{
			_progressDlg.Maximum += Math.Max(0, _originalStyles.Count - _updatedStyles.Count);

			foreach (var style in _originalStyles.Values)
			{
				var styleName = style.Name;
				if (style.IsBuiltIn && !_updatedStyles.ContainsKey(styleName))
				{
					_progressDlg.Step(0);
					_progressDlg.Message = string.Format(ResourceHelper.GetResourceString("kstidDeletingStylesStatusMsg"), styleName);
					var oldContext = style.Context;
					var fStyleInUse = StyleIsInUse(style);

					// if the style is in use, set things up to replace/remove it in the data
					if (fStyleInUse)
					{
						// If the factory style has been renamed or replaced with another
						// factory style, then all instances of it have to be converted, so
						// add it to the replacement list.
						var change = _sourceStyles.XPathSelectElement("replacements/change[@old='" + styleName.Replace(" ", "_") + "']");
						if (change != null)
						{
							var replStyleName = change.Attribute("new").Value.Replace("_", " ");
							var repl = _updatedStyles[replStyleName];
							if (!CompatibleContext(oldContext, repl.Context) && !StyleReplacementAllowed(styleName, replStyleName))
							{
								ReportInvalidInstallation(string.Format(LanguageExplorerResources.ksCannotReplaceXwithY, styleName, replStyleName, ResourceFileName));
							}

							_styleReplacements[styleName] = replStyleName;
						}
						else
						{
							// TODO: If the style is in use then it needs to be changed to
							// a user-defined style instead of being deleted, unless it's an internal style.
							var fIsCharStyle = style.Type == StyleType.kstCharacter;
							// Note: Instead of delete we replace the old style with the default style
							// for the correct context. Otherwise, deleting a style always sets the style
							// to "Normal", which is wrong in TE where the a) the default style is "Paragraph"
							// and b) the default style for a specific paragraph depends on the current
							// context (e.g. in an intro paragraph the default paragraph style is
							// "Intro Paragraph" instead of "Paragraph"). This fixes TE-5873.
							_styleReplacements[styleName] = fIsCharStyle ? string.Empty : StyleServices.NormalStyleName;
						}
					}

					_databaseStyles.Remove(style);
				}
			}
		}

		/// <summary>
		/// Final step of style update process is to crawl through the DB and replace all uses of
		/// any deprecated or renamed styles
		/// </summary>
		private void ReplaceFormerStyles()
		{
			if (_styleReplacements.Count == 0)
			{
				return;
			}
			var nPrevMax = _progressDlg.Maximum;
			_progressDlg.Maximum = nPrevMax + 1;
			_progressDlg.Position = nPrevMax;
			_progressDlg.Message = ResourceHelper.GetResourceString("kstidReplacingStylesStatusMsg");
			StringServices.ReplaceStyles(Cache, _styleReplacements);
			_progressDlg.Position = _progressDlg.Maximum;
		}

		/// <summary>
		/// Read the font properties from the XML element and set the properties in the given
		/// props builder.
		/// </summary>
		/// <param name="styleName">Name of style being created/updated (for error reporting)</param>
		/// <param name="styleTag">XML element that has the font properties</param>
		/// <param name="setIntProp">the delegate to set each int property</param>
		/// <param name="setStrProp">the delegate to set each string property</param>
		/// <param name="options">Indicates which properties to overwrite.</param>
		private void SetFontProperties(string styleName, XElement styleTag, Action<int, int, int> setIntProp, Action<int, string> setStrProp, OverwriteOptions options)
		{
			// Get character properties
			var fontElement = styleTag.Element("font");
			var attr = fontElement.Attribute("spellcheck");
			var fSpellcheck = (attr == null || attr.Value == "true");
			// The default is to do normal spell-checking, so we only need to set this property
			// if we want to suppress spell-checking or if we're forcing an existing
			// user-modified style to have the correct value.
			if (!fSpellcheck || options == OverwriteOptions.FunctionalPropertiesOnly)
			{
				setIntProp((int)FwTextPropType.ktptSpellCheck, (int)FwTextPropVar.ktpvEnum, (int)(fSpellcheck ? SpellingModes.ksmNormalCheck : SpellingModes.ksmDoNotCheck));
			}
			if (options == OverwriteOptions.FunctionalPropertiesOnly)
			{
				return;
			}
			attr = fontElement.Attribute("italic");
			if (attr != null)
			{
				setIntProp((int)FwTextPropType.ktptItalic, (int)FwTextPropVar.ktpvEnum, GetBoolAttribute(fontElement, "italic", styleName, ResourceFileName) ? (int)FwTextToggleVal.kttvInvert : (int)FwTextToggleVal.kttvOff);
			}

			attr = fontElement.Attribute("bold");
			if (attr != null)
			{
				setIntProp((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, GetBoolAttribute(fontElement, "bold", styleName, ResourceFileName) ? (int)FwTextToggleVal.kttvInvert : (int)FwTextToggleVal.kttvOff);
			}

			// superscript and subscript should be considered mutually exclusive.
			// Results of setting one to true and the other to false may not be intuitive.
			attr = fontElement.Attribute("superscript");
			if (attr != null)
			{
				setIntProp((int)FwTextPropType.ktptSuperscript, (int)FwTextPropVar.ktpvEnum, GetBoolAttribute(fontElement, "superscript", styleName, ResourceFileName) ? (int)FwSuperscriptVal.kssvSuper : (int)FwSuperscriptVal.kssvOff);
			}

			attr = fontElement.Attribute("subscript");
			if (attr != null)
			{
				setIntProp((int)FwTextPropType.ktptSuperscript, (int)FwTextPropVar.ktpvEnum, GetBoolAttribute(fontElement, "subscript", styleName, ResourceFileName) ? (int)FwSuperscriptVal.kssvSub : (int)FwSuperscriptVal.kssvOff);
			}

			attr = fontElement.Attribute("size");
			if (attr != null)
			{
				var nSize = InterpretMeasurementAttribute(attr.Value, "size", styleName, ResourceFileName);
				setIntProp((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, nSize);
			}

			attr = fontElement.Attribute("color");
			var sColor = (attr == null ? "default" : attr.Value);
			if (sColor != "default")
			{
				setIntProp((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, ColorVal(sColor, styleName));
			}

			attr = fontElement.Attribute("underlineColor");
			sColor = (attr == null ? "default" : attr.Value);
			if (sColor != "default")
			{
				setIntProp((int)FwTextPropType.ktptUnderColor, (int)FwTextPropVar.ktpvDefault, ColorVal(sColor, styleName));
			}

			attr = fontElement.Attribute("underline");
			var sUnderline = attr?.Value;
			if (sUnderline != null)
			{
				var unt = InterpretUnderlineType(sUnderline);
				setIntProp((int)FwTextPropType.ktptUnderline, (int)FwTextPropVar.ktpvEnum, unt);
			}

			var overrides = new Dictionary<int, FontInfo>();
			foreach (var child in fontElement.Elements())
			{
				if (child.Name == "override") // skip comments
				{
					var wsId = GetWs(child);
					if (wsId == 0)
					{
						continue; // WS not in use in this project?
					}
					var fontInfo = new FontInfo();
					var family = XmlUtils.GetOptionalAttributeValue(child, "family");
					if (family != null)
					{
						fontInfo.m_fontName = new InheritableStyleProp<string>(family);
					}
					var sizeText = XmlUtils.GetOptionalAttributeValue(child, "size");
					if (sizeText != null)
					{
						var nSize = InterpretMeasurementAttribute(sizeText, "override.size", styleName, ResourceFileName);
						fontInfo.m_fontSize = new InheritableStyleProp<int>(nSize);
					}
					var color = XmlUtils.GetOptionalAttributeValue(child, "color");
					if (color != null)
					{
						Color parsedColor;
						if (color.StartsWith("("))
						{
							var colorVal = ColorVal(color, styleName);
							parsedColor = Color.FromArgb(colorVal);
						}
						else
						{
							parsedColor = Color.FromName(color);
						}
						fontInfo.m_fontColor = new InheritableStyleProp<Color>(parsedColor);
					}
					var bold = XmlUtils.GetOptionalAttributeValue(child, "bold");
					if (bold != null)
					{
						fontInfo.m_bold = new InheritableStyleProp<bool>(bool.Parse(bold));
					}
					var italic = XmlUtils.GetOptionalAttributeValue(child, "italic");
					if (italic != null)
					{
						fontInfo.m_italic = new InheritableStyleProp<bool>(bool.Parse(italic));
					}
					overrides[wsId] = fontInfo;
				}
			}
			if (overrides.Count > 0)
			{
				var overridesString = BaseStyleInfo.GetOverridesString(overrides);
				if (!string.IsNullOrEmpty(overridesString))
				{
					setStrProp((int)FwTextPropType.ktptWsStyle, overridesString);
				}
			}
			// TODO: Handle dropcap attribute
		}

		/// <summary>
		/// Color value may be (red, green, blue) or one of the KnownColor values.
		/// Adapted from XmlVc routine.
		/// </summary>
		/// <param name="val">Value to interpret (a color name or (red, green, blue).</param>
		/// <param name="styleName">name of the style (for error reporting)</param>
		/// <returns>the color as a BGR 6-digit hex int</returns>
		private int ColorVal(string val, string styleName)
		{
			if (val[0] == '(')
			{
				var firstComma = val.IndexOf(',');
				var red = Convert.ToInt32(val.Substring(1, firstComma - 1));
				var secondComma = val.IndexOf(',', firstComma + 1);
				var green = Convert.ToInt32(val.Substring(firstComma + 1, secondComma - firstComma - 1));
				var blue = Convert.ToInt32(val.Substring(secondComma + 1, val.Length - secondComma - 2));
				return (blue * 256 + green) * 256 + red;
			}
			var col = Color.FromName(val);
			if (col.ToArgb() == 0)
			{
				ReportInvalidInstallation(string.Format(LanguageExplorerResources.ksUnknownUnderlineColor, styleName, ResourceFileName));
			}
			return (col.B * 256 + col.G) * 256 + col.R;
		}

		/// <summary>
		/// Interpret an underline type string as an FwUnderlineType.
		/// Note that this is a duplicate of the routine on XmlVc (due to avoiding assembly references). Keep in sync.
		/// </summary>
		private static int InterpretUnderlineType(string strVal)
		{
			var val = (int)FwUnderlineType.kuntSingle; // default
			switch (strVal)
			{
				case "single":
				case null:
					val = (int)FwUnderlineType.kuntSingle;
					break;
				case "none":
					val = (int)FwUnderlineType.kuntNone;
					break;
				case "double":
					val = (int)FwUnderlineType.kuntDouble;
					break;
				case "dotted":
					val = (int)FwUnderlineType.kuntDotted;
					break;
				case "dashed":
					val = (int)FwUnderlineType.kuntDashed;
					break;
				case "squiggle":
					val = (int)FwUnderlineType.kuntSquiggle;
					break;
				case "strikethrough":
					val = (int)FwUnderlineType.kuntStrikethrough;
					break;
				default:
					Debug.Assert(false, "Expected value single, none, double, dotted, dashed, strikethrough, or squiggle");
					break;
			}
			return val;
		}

		/// <summary>
		/// Read the paragraph properties from the XML element and set the properties in the given
		/// props builder.
		/// </summary>
		/// <param name="styleName">Name of style being created/updated (for error reporting)
		/// </param>
		/// <param name="styleTag">XML element that has the paragraph properties</param>
		/// <param name="setIntProp">the delegate to set each int property</param>
		/// <param name="setStrProp">the delegate to set each string property</param>
		/// <param name="options">Indicates which properties to overwrite.</param>
		private void SetParagraphProperties(string styleName, XElement styleTag, Action<int, int, int> setIntProp, Action<int, string> setStrProp, OverwriteOptions options)
		{
			var paragraphElement = styleTag.Element("paragraph");
			if (paragraphElement == null)
			{
				ReportInvalidInstallation(string.Format(LanguageExplorerResources.ksMissingParagraphElement, styleName, ResourceFileName));
			}
			var attr = paragraphElement.Attribute("keepWithNext");
			if (attr != null)
			{
				setIntProp((int)FwTextPropType.ktptKeepWithNext,
					(int)FwTextPropVar.ktpvEnum,
					GetBoolAttribute(paragraphElement, "keepWithNext", styleName, ResourceFileName) ?
					(int)FwTextToggleVal.kttvForceOn :
					(int)FwTextToggleVal.kttvOff);
			}
			attr = paragraphElement.Attribute("keepTogether");
			if (attr != null)
			{
				setIntProp((int)FwTextPropType.ktptKeepTogether,
					(int)FwTextPropVar.ktpvEnum,
					GetBoolAttribute(paragraphElement, "keepTogether", styleName, ResourceFileName) ?
					(int)FwTextToggleVal.kttvForceOn :
					(int)FwTextToggleVal.kttvOff);
			}

			attr = paragraphElement.Attribute("widowOrphan");
			if (attr != null)
			{
				setIntProp((int)FwTextPropType.ktptWidowOrphanControl,
					(int)FwTextPropVar.ktpvEnum,
					GetBoolAttribute(paragraphElement, "widowOrphan", styleName, ResourceFileName) ?
					(int)FwTextToggleVal.kttvForceOn :
					(int)FwTextToggleVal.kttvOff);
			}

			if (options == OverwriteOptions.FunctionalPropertiesOnly)
			{
				return;
			}
			// Set alignment
			attr = paragraphElement.Attribute("alignment");
			if (attr != null)
			{
				var sAlign = attr.Value;
				var nAlign = (int)FwTextAlign.ktalLeading;
				switch (sAlign)
				{
					case "left":
						break;
					case "center":
						nAlign = (int)FwTextAlign.ktalCenter;
						break;
					case "right":
						nAlign = (int)FwTextAlign.ktalTrailing;
						break;
					case "full":
						nAlign = (int)FwTextAlign.ktalJustify;
						break;
					default:
						ReportInvalidInstallation(string.Format(LanguageExplorerResources.ksUnknownAlignmentValue, styleName, ResourceFileName));
						break;
				}
				setIntProp((int)FwTextPropType.ktptAlign,
					(int)FwTextPropVar.ktpvEnum, nAlign);
			}
			attr = paragraphElement.Attribute("background");
			var sColor = (attr == null ? "default" : attr.Value);
			if (sColor != "default")
			{
				setIntProp((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, ColorVal(sColor, styleName));
			}

			// set leading indentation
			attr = paragraphElement.Attribute("indentLeft");
			if (attr != null)
			{
				var nLeftIndent = InterpretMeasurementAttribute(attr.Value, "indentLeft", styleName, ResourceFileName);
				setIntProp((int)FwTextPropType.ktptLeadingIndent, (int)FwTextPropVar.ktpvMilliPoint, nLeftIndent);
			}

			// Set trailing indentation
			attr = paragraphElement.Attribute("indentRight");
			if (attr != null)
			{
				var nRightIndent = InterpretMeasurementAttribute(attr.Value, "indentRight", styleName, ResourceFileName);
				setIntProp((int)FwTextPropType.ktptTrailingIndent, (int)FwTextPropVar.ktpvMilliPoint, nRightIndent);
			}

			// Set first-line/hanging indentation
			var nFirstIndent = 0;
			var fFirstLineOrHangingIndentSpecified = false;
			attr = paragraphElement.Attribute("firstLine");
			if (attr != null)
			{
				nFirstIndent = InterpretMeasurementAttribute(attr.Value, "firstLine", styleName, ResourceFileName);
				fFirstLineOrHangingIndentSpecified = true;
			}
			var nHangingIndent = 0;
			attr = paragraphElement.Attribute("hanging");
			if (attr != null)
			{
				nHangingIndent = InterpretMeasurementAttribute(attr.Value, "hanging", styleName, ResourceFileName);
				fFirstLineOrHangingIndentSpecified = true;
			}

			if (nFirstIndent != 0 && nHangingIndent != 0)
			{
				ReportInvalidInstallation(string.Format(LanguageExplorerResources.ksInvalidFirstLineHanging, styleName, ResourceFileName));
			}

			nFirstIndent -= nHangingIndent;
			if (fFirstLineOrHangingIndentSpecified)
			{
				setIntProp((int)FwTextPropType.ktptFirstIndent, (int)FwTextPropVar.ktpvMilliPoint, nFirstIndent);
			}

			// Set space before
			attr = paragraphElement.Attribute("spaceBefore");
			if (attr != null)
			{
				var nSpaceBefore = InterpretMeasurementAttribute(attr.Value, "spaceBefore", styleName, ResourceFileName);
				setIntProp((int)FwTextPropType.ktptSpaceBefore, (int)FwTextPropVar.ktpvMilliPoint, nSpaceBefore);
			}

			// Set space after
			attr = paragraphElement.Attribute("spaceAfter");
			if (attr != null)
			{
				var nSpaceAfter = InterpretMeasurementAttribute(attr.Value, "spaceAfter", styleName, ResourceFileName);
				setIntProp((int)FwTextPropType.ktptSpaceAfter, (int)FwTextPropVar.ktpvMilliPoint, nSpaceAfter);
			}

			attr = paragraphElement.Attribute("lineSpacing");
			if (attr != null)
			{
				var lineSpacing = InterpretMeasurementAttribute(attr.Value, "lineSpacing", styleName, ResourceFileName);
				if (lineSpacing < 0)
				{
					ReportInvalidInstallation(string.Format(LanguageExplorerResources.ksNegativeLineSpacing, styleName, ResourceFileName));
				}

				// Set lineSpacing
				attr = paragraphElement.Attribute("lineSpacingType");
				if (attr != null)
				{
					var sLineSpacingType = attr.Value;
					switch (sLineSpacingType)
					{
						// verify valid line spacing types
						case "atleast":
							setIntProp((int)FwTextPropType.ktptLineHeight, (int)FwTextPropVar.ktpvMilliPoint, lineSpacing);
							break;
						case "exact":
							lineSpacing *= -1; // negative lineSpacing indicates exact line spacing
							setIntProp((int)FwTextPropType.ktptLineHeight, (int)FwTextPropVar.ktpvMilliPoint, lineSpacing);
							break;
						case "rel":
							setIntProp((int)FwTextPropType.ktptLineHeight, (int)FwTextPropVar.ktpvRelative, lineSpacing);
							break;
						default:
							ReportInvalidInstallation(string.Format(LanguageExplorerResources.ksUnknownLineSpacingValue, styleName, ResourceFileName));
							break;
					}
				}
			}

			// Set borders
			attr = paragraphElement.Attribute("border");
			if (attr != null)
			{
				var nBorder = 0;
				switch (attr.Value)
				{
					case "top":
						nBorder = (int)FwTextPropType.ktptBorderTop;
						break;
					case "bottom":
						nBorder = (int)FwTextPropType.ktptBorderBottom;
						break;
					case "leading":
						nBorder = (int)FwTextPropType.ktptBorderLeading;
						break;
					case "trailing":
						nBorder = (int)FwTextPropType.ktptBorderTrailing;
						break;
					default:
						ReportInvalidInstallation(string.Format(LanguageExplorerResources.ksUnknownBorderValue, styleName, ResourceFileName));
						break;
				}
				setIntProp(nBorder, (int)FwTextPropVar.ktpvDefault, 500);
			}

			attr = paragraphElement.Attribute("bulNumScheme");
			if (attr != null)
			{
				setIntProp((int)FwTextPropType.ktptBulNumScheme, (int)FwTextPropVar.ktpvEnum, InterpretBulNumSchemeAttribute(attr.Value, styleName, ResourceFileName));
			}
			attr = paragraphElement.Attribute("bulNumStartAt");
			if (attr != null)
			{
				int nVal;
				if (!int.TryParse(attr.Value, out nVal))
				{
					ReportInvalidInstallation(string.Format(LanguageExplorerResources.ksUnknownBulNumStartAtValue, styleName, ResourceFileName));
					nVal = 0;
				}
				setIntProp((int)FwTextPropType.ktptBulNumStartAt, (int)FwTextPropVar.ktpvDefault, nVal);
			}

			attr = paragraphElement.Attribute("bulNumTxtAft");
			if (attr?.Value.Length > 0)
			{
				setStrProp((int)FwTextPropType.ktptBulNumTxtAft, attr.Value);
			}

			attr = paragraphElement.Attribute("bulNumTxtBef");
			if (attr?.Value.Length > 0)
			{
				setStrProp((int)FwTextPropType.ktptBulNumTxtBef, attr.Value);
			}

			attr = paragraphElement.Attribute("bulCusTxt");
			if (attr?.Value.Length > 0)
			{
				setStrProp((int)FwTextPropType.ktptCustomBullet, attr.Value);
			}

			//Bullet Font Info
			var bulletFontInfoElement = paragraphElement.Element("BulNumFontInfo");
			if (bulletFontInfoElement == null || !bulletFontInfoElement.HasElements)
			{
				return;
			}
			SetBulNumFontInfoProperties(styleName, setStrProp, bulletFontInfoElement);
		}

		/// <summary>
		/// Read the BulNumFontInfo properties from the XML element and set the properties in the given
		/// props builder.
		/// </summary>
		/// <param name="styleName">Name of style being created/updated (for error reporting) </param>
		/// <param name="setStrProp">the delegate to set each string property</param>
		/// <param name="bulletFontInfoElement">BulNumFontInfo element from Xml document</param>
		private void SetBulNumFontInfoProperties(string styleName, Action<int, string> setStrProp, XElement bulletFontInfoElement)
		{
			var propsBldr = TsStringUtils.MakePropsBldr();
			int type;
			if (!bulletFontInfoElement.HasAttributes)
			{
				return;
			}
			var attr = bulletFontInfoElement.Attribute("italic");
			if (attr != null)
			{
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptItalic, (int)FwTextPropVar.ktpvEnum,
					GetBoolAttribute(bulletFontInfoElement, "italic", styleName, ResourceFileName)
						? (int)FwTextToggleVal.kttvForceOn
						: (int)FwTextToggleVal.kttvOff);
			}

			attr = bulletFontInfoElement.Attribute("bold");
			if (attr != null)
			{
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum,
					GetBoolAttribute(bulletFontInfoElement, "bold", styleName, ResourceFileName)
						? (int)FwTextToggleVal.kttvForceOn
						: (int)FwTextToggleVal.kttvOff);
			}

			attr = bulletFontInfoElement.Attribute("size");
			if (attr != null)
			{
				var nSize = InterpretMeasurementAttribute(attr.Value, "size", styleName, ResourceFileName);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, nSize);
			}

			attr = bulletFontInfoElement.Attribute("color");
			var sbColor = (attr == null ? "default" : attr.Value);
			if (sbColor != "default")
			{
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, ColorVal(sbColor, styleName));
			}

			attr = bulletFontInfoElement.Attribute("underlineColor");
			sbColor = (attr == null ? "default" : attr.Value);
			if (sbColor != "default")
			{
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptUnderColor, (int)FwTextPropVar.ktpvDefault, ColorVal(sbColor, styleName));
			}

			attr = bulletFontInfoElement.Attribute("underline");
			var sUnderline = attr?.Value;
			if (sUnderline != null)
			{
				var unt = InterpretUnderlineType(sUnderline);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptUnderline, (int)FwTextPropVar.ktpvEnum, unt);
			}

			attr = bulletFontInfoElement.Attribute("family");
			var sfamily = attr?.Value;
			if (sfamily != null)
			{
				propsBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily, sfamily);
			}

			attr = bulletFontInfoElement.Attribute("forecolor");
			sbColor = attr == null ? "default" : attr.Value;
			if (sbColor != "default")
			{
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, ColorVal(sbColor, styleName));
			}

			attr = bulletFontInfoElement.Attribute("backcolor");
			sbColor = (attr == null ? "default" : attr.Value);
			if (sbColor != "default")
			{
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, ColorVal(sbColor, styleName));
			}

			// Add the integer properties to the bullet props string
			var bulletProps = new StringBuilder(propsBldr.IntPropCount * 3 + propsBldr.StrPropCount * 3);
			for (var i = 0; i < propsBldr.IntPropCount; i++)
			{
				int var;
				var intValue = propsBldr.GetIntProp(i, out type, out var);
				bulletProps.Append((char)type);
				bulletProps.Append((char)(intValue & 0xFFFF));
				bulletProps.Append((char)((intValue >> 16) & 0xFFFF));
			}

			// Add the string properties to the bullet props string
			for (var i = 0; i < propsBldr.StrPropCount; i++)
			{
				var strValue = propsBldr.GetStrProp(i, out type);
				bulletProps.Append((char)type);
				bulletProps.Append(strValue);
				bulletProps.Append('\u0000');
			}

			if (!string.IsNullOrEmpty(bulletProps.ToString()))
			{
				setStrProp((int)FwTextPropType.ktptBulNumFontInfo, bulletProps.ToString());
			}
		}

		/// <summary>
		/// Get the ws value (hvo) from the wsId contained in the given attributes
		/// </summary>
		/// <param name="element">Element that better have an "wsId" attribute</param>
		private int GetWs(XElement element)
		{
			var wsId = element.Attribute("wsId").Value;
			return string.IsNullOrEmpty(wsId) ? 0 : Cache.ServiceLocator.WritingSystemManager.GetWsFromStr(wsId);
		}

		/// <summary>
		/// Second pass of style creation phase: set up "based-on" and "next" styles
		/// </summary>
		/// <param name="tagList">List of XML elements representing factory styles to create</param>
		private void SetBasedOnAndNextProps(List<XElement> tagList)
		{
			foreach (var styleTag in tagList)
			{
				var styleName = GetStyleName(styleTag);
				var style = _updatedStyles[styleName];
				_progressDlg.Step(0);
				_progressDlg.Message = string.Format(ResourceHelper.GetResourceString("kstidUpdatingStylesStatusMsg"), styleName);
				var elementForBasedOn = styleTag;
				if (style.Type == StyleType.kstParagraph)
				{
					elementForBasedOn = styleTag.Element("paragraph");
				}
				else if (style.Type == StyleType.kstCharacter && elementForBasedOn.Attribute("basedOn") == null)
				{
					continue;
				}

				if (styleName != StyleServices.NormalStyleName)
				{
					var sBasedOnStyleName = GetBasedOn(elementForBasedOn, styleName);
					if (string.IsNullOrEmpty(sBasedOnStyleName))
					{
						ReportInvalidInstallation(string.Format(LanguageExplorerResources.ksMissingBasedOnStyle, styleName, ResourceFileName));
					}
					if (!_updatedStyles.ContainsKey(sBasedOnStyleName))
					{
						ReportInvalidInstallation(string.Format(LanguageExplorerResources.ksUnknownBasedOnStyle, styleName, sBasedOnStyleName));
					}
					var basedOnStyle = _updatedStyles[sBasedOnStyleName];
					if (basedOnStyle.Hvo == style.Hvo)
					{
						ReportInvalidInstallation(string.Format(LanguageExplorerResources.ksNoBasedOnSelf, styleName, ResourceFileName));
					}
					style.BasedOnRA = basedOnStyle;
				}

				string sNextStyleName = null;
				if (_reservedStyles.ContainsKey(styleName))
				{
					sNextStyleName = _reservedStyles[styleName].nextStyle;
				}
				else
				{
					var next = elementForBasedOn.Attribute("next");
					if (next != null)
					{
						sNextStyleName = next.Value.Replace("_", " ");
					}
				}
				if (!string.IsNullOrEmpty(sNextStyleName))
				{
					if (!_updatedStyles.ContainsKey(sNextStyleName))
					{
						ReportInvalidInstallation(string.Format(LanguageExplorerResources.ksUnknownNextStyle, styleName, sNextStyleName, ResourceFileName));
					}
					style.NextRA = _updatedStyles.ContainsKey(sNextStyleName) ? _updatedStyles[sNextStyleName] : null;
				}
			}
			SetBasedOnAndNextPropsReserved();
		}

		/// <summary>
		/// Second pass of style creation phase for reserved styles that weren't in the external
		/// XML stylesheet: set up "based-on" and "next" styles
		/// </summary>
		private void SetBasedOnAndNextPropsReserved()
		{
			foreach (var styleName in _reservedStyles.Keys)
			{
				var info = _reservedStyles[styleName];
				if (!info.created)
				{
					var style = _updatedStyles[styleName];
					// No need now to do the assert,
					// since the Dictionary will throw an exception,
					// if the key isn't present.
					//Debug.Assert(style != null);

					if (style.Type == StyleType.kstParagraph)
					{
						_progressDlg.Message = string.Format(ResourceHelper.GetResourceString("kstidUpdatingStylesStatusMsg"), styleName);
						IStStyle newStyle = null;
						if (styleName != StyleServices.NormalStyleName)
						{
							if (_updatedStyles.ContainsKey(info.basedOn))
							{
								newStyle = _updatedStyles[info.basedOn];
							}
							style.BasedOnRA = newStyle;
						}
						newStyle = null;
						if (_updatedStyles.ContainsKey(info.nextStyle))
						{
							newStyle = _updatedStyles[info.nextStyle];
						}
						style.NextRA = newStyle;
					}
				}
			}
		}

		/// <summary>
		/// Interprets a given attribute as a boolean value
		/// </summary>
		/// <param name="element">Element with attributes to look in</param>
		/// <param name="sAttrib">Named attribute</param>
		/// <param name="styleName">The name of the style to which this attribute pertains (used
		/// only for debug error reporting)</param>
		/// <param name="fileName">Name of XML file (for error reporting)</param>
		/// <returns>true if attribute value is "yes" or "true"</returns>
		private static bool GetBoolAttribute(XElement element, string sAttrib, string styleName, string fileName)
		{
			var sVal = element.Attribute(sAttrib).Value;
			switch (sVal)
			{
				case "yes":
				case "true":
					return true;
				case "no":
				case "false":
				case "":
					return false;
				default:
					ReportInvalidInstallation(string.Format(LanguageExplorerResources.ksUnknownStyleAttribute, sAttrib, styleName, fileName));
					return false; // Can't actually get here, but don't tell the compiler that!
			}
		}

		/// <summary>
		/// Interprets a given attribute value as a measurement (in millipoints)
		/// </summary>
		/// <param name="sSize">Attribute value</param>
		/// <param name="sAttrib">The name of the attribute being interpreted (used only for
		/// debug error reporting)</param>
		/// <param name="styleName">The name of the style to which this attribute pertains (used
		/// only for debug error reporting)</param>
		/// <param name="fileName">Name of XML file (for error reporting)</param>
		/// <returns>The value of the attribute interpreted as millipoints</returns>
		private static int InterpretMeasurementAttribute(string sSize, string sAttrib, string styleName, string fileName)
		{
			sSize = sSize.Trim();
			if (sSize.Length >= 4)
			{
				var number = sSize.Substring(0, sSize.Length - 3);
				if (sSize.EndsWith(" pt"))
				{
					return (int)(double.Parse(number, new CultureInfo("en-US")) * 1000.0);
				}
				if (sSize.EndsWith(" in"))
				{
					return (int)(double.Parse(number, new CultureInfo("en-US")) * 72000.0);
				}
				ReportInvalidInstallation(string.Format(LanguageExplorerResources.ksUnknownAttrUnits, sAttrib, styleName, fileName));
			}
			return 0;
		}

		/// <summary>
		/// Interprets a given attribute value as a Bullet/Number scheme.
		/// </summary>
		/// <param name="sScheme">attribute value</param>
		/// <param name="styleName">The name of the style to which this attribute pertains (used
		/// only for debug error reporting)</param>
		/// <param name="fileName">Name of XML file (for error reporting)</param>
		/// <returns>
		/// The value of the attribute interpreted as an enum value (int equivalent)
		/// </returns>
		private static int InterpretBulNumSchemeAttribute(string sScheme, string styleName, string fileName)
		{
			sScheme = sScheme.Trim();
			switch (sScheme)
			{
				case "None": return (int)VwBulNum.kvbnNone;
				case "Arabic": return (int)VwBulNum.kvbnArabic;
				case "Arabic01": return (int)VwBulNum.kvbnArabic01;
				case "LetterUpper": return (int)VwBulNum.kvbnLetterUpper;
				case "LetterLower": return (int)VwBulNum.kvbnLetterLower;
				case "RomanUpper": return (int)VwBulNum.kvbnRomanUpper;
				case "RomanLower": return (int)VwBulNum.kvbnRomanLower;
				case "Custom": return (int)VwBulNum.kvbnBullet;
			}
			int nVal;
			if (sScheme.StartsWith("Bullet:"))
			{
				if (int.TryParse(sScheme.Substring(7), out nVal))
				{
					nVal += (int)VwBulNum.kvbnBulletBase;
					if (nVal >= (int)VwBulNum.kvbnBulletBase && nVal <= (int)VwBulNum.kvbnBulletMax)
					{
						return nVal;
					}
				}
			}
			else if (int.TryParse(sScheme, out nVal))
			{
				if (nVal >= (int)VwBulNum.kvbnBulletBase && nVal <= (int)VwBulNum.kvbnBulletMax)
				{
					return nVal;
				}
			}
			ReportInvalidInstallation(string.Format(LanguageExplorerResources.ksUnknownBulNumSchemeValue, styleName, fileName));
			return (int)VwBulNum.kvbnNone;
		}

		/// <summary>
		/// Retrieves a valid TE style name from the specified attributes.
		/// </summary>
		/// <param name="element">The element containing the style id to use</param>
		/// <returns>a valid style name</returns>
		private static string GetStyleName(XElement element)
		{
			return element.Attribute("id").Value.Replace("_", " ");
		}
	}
}