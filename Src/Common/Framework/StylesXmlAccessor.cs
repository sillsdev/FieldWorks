// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StylesXmlAccessor.cs
// Responsibility: FieldWorks Team
// ---------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Controls; // for BackgroundTaskInvoker
using SIL.FieldWorks.Resources;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// A class that supports having a collection of factory styles defined in an XML file.
	/// A static method can be called to update the database styles if they are out of date
	/// with respect to the file.
	///
	/// Note: This class was refactored from TeStylesXmlAccessor, which originally included
	/// all its functionality. Some traces of TE names may remain. The TeStylesXmlAccessor unit
	/// test tests much of the functionality of this class.
	/// </summary>
	public abstract class StylesXmlAccessor : SettingsXmlAccessorBase
	{
		#region Data members
		/// <summary>The progress dialog (may be null)</summary>
		protected IAdvInd4 m_progressDlg;
		/// <summary>The XmlNode from which to get the style info</summary>
		protected XmlNode m_sourceStyles;
		/// <summary>Array of styles that need to be renamed</summary>
		protected List<StyleReplacement> m_replacedStyles = new List<StyleReplacement>();
		/// <summary>Array of styles to be deleted</summary>
		protected List<string> m_deletedStyles = new List<string>();
		/// <summary>Array of styles that the user has modified</summary>
		protected List<string> m_userModifiedStyles = new List<string>();
		/// <summary>Collection of styles in the DB</summary>
		protected FdoOwningCollection<IStStyle> m_databaseStyles;

		private LgWritingSystemCollection m_lgwsCollection;
		/// <summary>Dictionary of ICU locales to Ws ids for better performance</summary>
		private Dictionary<string, int> m_htIcuToWs;
		/// <summary>Dictionary of style names to StStyle objects representing the initial
		/// collection of styles in the DB</summary>
		protected Dictionary<string, IStStyle> m_htOrigStyles = new Dictionary<string, IStStyle>();
		/// <summary>
		/// Dictionary of style names to StStyle objects representing the collection of
		/// styles that should be factory styles in the DB (i.e., any factory styles in the
		/// original Dictionary that are not also in this Dictionary need to be removed or turned
		/// into user-defined styles).
		/// </summary>
		protected Dictionary<string, IStStyle> m_htUpdatedStyles = new Dictionary<string, IStStyle>();

		/// <summary>
		/// Maps from style name to ReservedStyleInfo.
		/// </summary>
		protected static Dictionary<string, ReservedStyleInfo> s_htReservedStyles = new Dictionary<string, ReservedStyleInfo>();
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor is protected so only derived classes can create an instance
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected StylesXmlAccessor()
		{
		}
		#endregion

		#region Abstract properties
		/// <summary>
		/// The collection that owns the styles; for example, Scripture.StylesOC.
		/// </summary>
		protected abstract FdoOwningCollection<IStStyle> StyleCollection { get; }

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the required DTD version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string DtdRequiredVersion
		{
			get { return "AFE66B5C-2C4A-4872-B78B-45065E6EC750"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the root element in the XmlDocument that contains the styles.
		/// May actually be an arbitrary XPath that selectes the root element that has the
		/// DTDVer attribute and contains the "markup" element.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string RootNodeName
		{
			get { return "Styles"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// List of user-modified styles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual List<string> UserModifiedStyles
		{
			get { return m_userModifiedStyles; }
		}
		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a GUID based on the version attribute node.
		/// </summary>
		/// <param name="baseNode">The node containing the markup node</param>
		/// <returns>A GUID based on the version attribute node</returns>
		/// ------------------------------------------------------------------------------------
		protected override Guid GetVersion(XmlNode baseNode)
		{
			return base.GetVersion(GetMarkupNode(baseNode));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the markup node from the given root node.
		/// </summary>
		/// <param name="rootNode">The root node.</param>
		/// ------------------------------------------------------------------------------------
		public static XmlNode GetMarkupNode(XmlNode rootNode)
		{
			return rootNode.SelectSingleNode("markup");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process the resources (e.g., create styles or add publication info).
		/// </summary>
		/// <param name="dlg">The progress dialog manager.</param>
		/// <param name="progressDlg">The progress dialog box itself.</param>
		/// <param name="doc">The loaded XML document that has the settings.</param>
		/// ------------------------------------------------------------------------------------
		protected override void ProcessResources(ProgressDialogWithTask dlg,
			IAdvInd4 progressDlg, XmlNode doc)
		{
			dlg.RunTask(progressDlg, true, new BackgroundTaskInvoker(CreateStyles),
				StyleCollection, doc);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Complain if the context is not valid for the tool that is loading the styles.
		/// This default implementation allows anything.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="styleName"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual void ValidateContext(ContextValues context, string styleName)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load all styles from the XML file and create styles in the database for them.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">The parameters. First parameter is the style objects
		/// (a FdoOwningCollection&lt;IStStyle&gt;), second is the styles (an XmlNode).</param>
		/// ------------------------------------------------------------------------------------
		protected object CreateStyles(IAdvInd4 progressDlg, params object[] parameters)
		{
			Debug.Assert(parameters.Length == 2);
			m_databaseStyles = (FdoOwningCollection<IStStyle>)parameters[0];
			m_sourceStyles = (XmlNode)parameters[1];
			m_progressDlg = progressDlg;

			CreateStyles();

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a set of Scripture styles based on the given XML node.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreateStyles()
		{
			string label = SIL.Utils.XmlUtils.GetOptionalAttributeValue(m_sourceStyles, "label", "");
			m_progressDlg.Title = String.Format(ResourceHelper.GetResourceString("kstidCreatingStylesCaption"), label);
			m_progressDlg.Message =
				string.Format(ResourceHelper.GetResourceString("kstidCreatingStylesStatusMsg"),
				string.Empty);
			m_progressDlg.Position = 0;

			// Populate hashtable with initial set of styles
			foreach (IStStyle sty in m_databaseStyles)
				m_htOrigStyles[sty.Name] = sty;

			//Select all styles.
			XmlNode markup = GetMarkupNode(m_sourceStyles);
			XmlNodeList tagList = markup.SelectNodes("tag");

			m_progressDlg.SetRange(0, tagList.Count * 2);

			// First pass to create styles and set general properties.
			CreateAndUpdateScrStyles(tagList);

			CreateAnyReservedStylesThatDontAlreadyHaveTheGoodFortuneOfExisting();

			// Second pass to set up "based-on" and "next" styles
			SetBasedOnAndNextProps(tagList);

			// Third pass to delete (and possibly prepare to rename) any styles that used to be
			// factory styles but aren't any longer
			DeleteDeprecatedStylesAndDetermineReplacements();

			// Final step is to walk through the DB and relace any retired styles
			ReplaceFormerStyles();

			// Finally, update styles version in database.
			SetNewResourceVersion(GetVersion(m_sourceStyles));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// You know.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateAnyReservedStylesThatDontAlreadyHaveTheGoodFortuneOfExisting()
		{
			foreach (KeyValuePair<string, ReservedStyleInfo> kvp in s_htReservedStyles)
			{
				string styleName = kvp.Key;
				ReservedStyleInfo info = kvp.Value;
				if (!info.created)
				{
					m_progressDlg.Message =
						string.Format(ResourceHelper.GetResourceString("kstidCreatingStylesStatusMsg"),
						styleName);

					// Find the existing style if there is one; otherwise, create new style object
					IStStyle style = FindOrCreateStyle(styleName, info.styleType, info.context,
						info.structure, info.function);

					// Avoid nasty crashing problems and take some decently haphazard stab at
					// getting the right "Normal" font face (all the other built-in default
					// properties are already okay).
					if (IsNormalStyle(styleName))
					{
						ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
						propsBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily,
							AppDefaultFont);
						style.Rules = propsBldr.GetTextProps();
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the application specific default font. This is usually the default font, but
		/// in the case of TE it is the body font.
		/// </summary>
		/// <value>The app default font.</value>
		/// ------------------------------------------------------------------------------------
		protected virtual string AppDefaultFont
		{
			get { return StStyle.DefaultFont; }
		}

		/// <summary>
		/// Return true if this is the 'normal' style that all others inherit from. TE overrides.
		/// </summary>
		/// <param name="styleName"></param>
		/// <returns></returns>
		protected virtual bool IsNormalStyle(string styleName)
		{
			return styleName == "Normal";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Define reserved styles. By default there is nothing to do.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void InitReservedStyles()
		{
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// First pass of style creation phase: create any new styles and set/update general
		/// properties
		/// </summary>
		/// <param name="tagList">List of XML nodes representing factory styles to create</param>
		/// -------------------------------------------------------------------------------------
		private void CreateAndUpdateScrStyles(XmlNodeList tagList)
		{
			InitReservedStyles();

			foreach (XmlNode styleTag in tagList)
			{
				XmlAttributeCollection attributes = styleTag.Attributes;
				string styleName = GetStyleName(attributes);

				// Don't create a style for certain excluded contexts
				ContextValues context = GetContext(attributes, styleName);
				if (IsExcludedContext(context))
					continue;

				m_progressDlg.Step(0);
				m_progressDlg.Message =
					string.Format(ResourceHelper.GetResourceString("kstidCreatingStylesStatusMsg"),
					styleName);

				StyleType styleType = GetType(attributes, styleName, context);
				StructureValues structure = GetStructure(attributes, styleName);
				FunctionValues function = GetFunction(attributes, styleName);

				StStyle style = (StStyle)FindOrCreateStyle(styleName, styleType, context, structure,
					function);

				if (s_htReservedStyles.ContainsKey(styleName))
				{
					ReservedStyleInfo info = s_htReservedStyles[styleName];
					info.created = true;
				}

				// set the user level
				style.UserLevel = int.Parse(attributes.GetNamedItem("userlevel").Value);

				// Set the usage info
				foreach (XmlNode usage in styleTag.SelectNodes("usage"))
				{
					int ws = GetWs(usage.Attributes);
					string usageInfo = usage.InnerText;
					if (ws > 0 && usageInfo != null && usageInfo != string.Empty)
						style.Usage.SetAlternative(usageInfo, ws);
				}

				// If the user has modified the style manually, we don't want to overwrite it
				// with the standard definition.
				// Enhance JohnT: possibly there should be some marker in the XML to indicate that
				// a style has changed so drastically that it SHOULD overwrite the user modifications?
				if (style.IsModified)
				{
					m_userModifiedStyles.Add(style.Name);
					continue;
				}

				// Get props builder with default Text Properties
				ITsPropsBldr propsBldr = TsPropsBldrClass.Create();

				SetFontProperties(styleName, styleTag, propsBldr);

				// Get paragraph properties
				if (style.Type == StyleType.kstParagraph)
					SetParagraphProperties(styleName, styleTag, propsBldr);

				style.Rules = propsBldr.GetTextProps();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the existing style if there is one; otherwise, create new style object
		/// </summary>
		/// <param name="styleName">Name of style</param>
		/// <param name="styleType">Type of style (para or char)</param>
		/// <param name="context">Context</param>
		/// <param name="structure">Structure</param>
		/// <param name="function">Function</param>
		/// <returns>A new or existing style</returns>
		/// ------------------------------------------------------------------------------------
		private IStStyle FindOrCreateStyle(string styleName, StyleType styleType,
			ContextValues context, StructureValues structure, FunctionValues function)
		{
			IStStyle style = null;
			bool fUsingExistingStyle = false;
			if (m_htOrigStyles.ContainsKey(styleName))
			{
				style = m_htOrigStyles[styleName];
				// Make sure existing style has compatible context, structure, and function.
				// If not, get a new StStyle object that we can define.
				int hvo = style.Hvo;
				style = EnsureCompatibleFactoryStyle(style, styleType, context, structure,
					function);
				fUsingExistingStyle = (hvo == style.Hvo);
			}
			else
			{
				style = m_databaseStyles.Add(new StStyle());
			}

			m_htUpdatedStyles[styleName] = style;

			if (!fUsingExistingStyle)
			{
				// Set non-variable properties
				style.IsBuiltIn = true;
				style.IsModified = false;

				// Set the style name, type, context, structure, and function
				style.Name = styleName;
				style.Type = styleType;
				style.Context = context;
				style.Structure = structure;
				style.Function = function;
			}
			return style;
		}

		#region BasedOn Context, Structure, Function, and type interpreters
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Interpret the based on attribute
		/// </summary>
		/// <param name="attributes">Collection of attributes that better have a "context"
		/// attribute</param>
		/// <param name="styleName">Stylename being processed (for error reporting purposes)
		/// </param>
		/// <returns>The name of the based-on style</returns>
		/// ------------------------------------------------------------------------------------
		private static string GetBasedOn(XmlAttributeCollection attributes, string styleName)
		{
			if (s_htReservedStyles.ContainsKey(styleName))
			{
				return s_htReservedStyles[styleName].basedOn;
			}
			XmlNode basedOn = attributes.GetNamedItem("basedOn");
			return (basedOn == null) ? null : basedOn.Value.Replace("_", " ");
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Interpret the context attribute as a ContextValues value
		/// </summary>
		/// <param name="attributes">Collection of attributes that better have a "context"
		/// attribute</param>
		/// <param name="styleName">Stylename being processed (for error reporting purposes)
		/// </param>
		/// <returns>The context of the style</returns>
		/// -------------------------------------------------------------------------------------
		private static ContextValues GetContext(XmlAttributeCollection attributes,
			string styleName)
		{
			if (s_htReservedStyles.ContainsKey(styleName))
			{
				return s_htReservedStyles[styleName].context;
			}

			string sContext = attributes.GetNamedItem("context").Value;
			switch(sContext)
			{
				case "annotation":
					return ContextValues.Annotation;
				case "back":
					return ContextValues.BackMatter;
				case "book":
					return ContextValues.Book;
				case "general":
					return ContextValues.General;
				case "internal":
					return ContextValues.Internal;
				case "internalMappable":
					return ContextValues.InternalMappable;
				case "intro":
					return ContextValues.Intro;
				case "introtitle":
					return ContextValues.IntroTitle;
				case "note":
					return ContextValues.Note;
				case "publication":
					return ContextValues.Publication;
				case "text":
					return ContextValues.Text;
				case "title":
					return ContextValues.Title;
				case "backTranslation":
					return ContextValues.BackTranslation;
				case "internalConfigureView":
					return ContextValues.InternalConfigureView;
				case "psuedoStyle":
					return ContextValues.PsuedoStyle;

				default:
					Debug.Assert(false, "Unrecognized context attribute for style " + styleName +
						" in Testyles.xml: " + sContext);
					throw new Exception(ResourceHelper.GetResourceString("kstidInvalidInstallation"));
			}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Interpret the use attribute as a StructureValues value
		/// </summary>
		/// <param name="attributes">Collection of attributes that better have a "structure"
		/// attribute</param>
		/// <param name="styleName">Stylename being processed (for error reporting purposes)
		/// </param>
		/// <returns>The structure of the style</returns>
		/// -------------------------------------------------------------------------------------
		private static StructureValues GetStructure(XmlAttributeCollection attributes,
			string styleName)
		{
			if (s_htReservedStyles.ContainsKey(styleName))
			{
				return s_htReservedStyles[styleName].structure;
			}

			XmlNode node = attributes.GetNamedItem("structure");
			string sStructure = (node != null) ? node.Value : null;

			if (sStructure == null)
				return StructureValues.Undefined;

			switch(sStructure)
			{
				case "heading":
					return StructureValues.Heading;
				case "body":
					return StructureValues.Body;
				default:
					Debug.Assert(false, "Unrecognized structure attribute for style " + styleName +
						" in Testyles.xml: " + sStructure);
					throw new Exception(ResourceHelper.GetResourceString("kstidInvalidInstallation"));
			}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Interpret the use attribute as a FunctionValues value
		/// </summary>
		/// <param name="attributes">Collection of attributes that better have a "use"
		/// attribute</param>
		/// <param name="styleName">Stylename being processed (for error reporting purposes)
		/// </param>
		/// <returns>The function of the style</returns>
		/// -------------------------------------------------------------------------------------
		private static FunctionValues GetFunction(XmlAttributeCollection attributes,
			string styleName)
		{
			if (s_htReservedStyles.ContainsKey(styleName))
			{
				return s_htReservedStyles[styleName].function;
			}

			XmlNode node = attributes.GetNamedItem("use");
			string sFunction = (node != null) ? node.Value : null;
			if (sFunction == null)
				return FunctionValues.Prose;

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
					Debug.Assert(false, "Unrecognized use attribute for style " + styleName +
						" in Testyles.xml: " + sFunction);
					throw new Exception(ResourceHelper.GetResourceString("kstidInvalidInstallation"));
			}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Interpret the type attribute as a StyleType value
		/// </summary>
		/// <param name="attributes">Collection of attributes that better have a "type"
		/// attribute</param>
		/// <param name="styleName">Stylename being processed (for error reporting purposes)
		/// </param>
		/// <param name="context"></param>
		/// <returns>The type of the style</returns>
		/// -------------------------------------------------------------------------------------
		public StyleType GetType(XmlAttributeCollection attributes, string styleName,
			ContextValues context)
		{
			if (s_htReservedStyles.ContainsKey(styleName))
				return s_htReservedStyles[styleName].styleType;
			string sType = attributes.GetNamedItem("type").Value;
			ValidateContext(context, styleName);
			switch(sType)
			{
				case "paragraph":
					ValidateParagraphContext(context, styleName);
					return StyleType.kstParagraph;
				case "character":
					return StyleType.kstCharacter;
				default:
					Debug.Assert(false, "Unrecognized type attribute for style " + styleName +
						" in Testyles.xml: " + sType);
					throw new Exception(ResourceHelper.GetResourceString("kstidInvalidInstallation"));
			}
		}

		/// <summary>
		/// Throw an exception if the specified context is not valid for the specified paragraph style.
		/// TE overrides to forbid 'general' paragraph styles. This default does nothing.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="styleName"></param>
		protected virtual void ValidateParagraphContext(ContextValues context, string styleName)
		{
		}
		#endregion

		#region Style upgrade stuff
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// If existing style is NOT a factory style, we're about to "clobber" a user style. If
		/// the context, structure, and function don't match (and maybe even if they do), this
		/// could be pretty bad. Store info necessary to rename their style and change all the
		/// uses of it.
		/// </summary>
		/// <param name="style">Style to check.</param>
		/// <param name="type">Style type we want</param>
		/// <param name="context">The context we want</param>
		/// <param name="structure">The structure we want</param>
		/// <param name="function">The function we want</param>
		/// <returns>Either the passed in style, or a new style if the passed in one cannot be
		/// redefined as requested.</returns>
		/// -------------------------------------------------------------------------------------
		public IStStyle EnsureCompatibleFactoryStyle(IStStyle style, StyleType type,
			ContextValues context, StructureValues structure, FunctionValues function)
		{
			if (style.IsBuiltIn &&
				(style.Context != context ||
				style.Function != function) &&
				IsValidInternalStyleContext(style, context))
			{
				// For now, at least, this method only deals with context changes. Theoretically,
				// we could in the future have a function, type, or structure change that would
				// require some special action.
				ChangeFactoryStyleToInternal(style, context);
				if (style.Type != type)
					style.Type = type;
				// Structure and function are probably meaningless for internal styles, but just
				// to be sure...
				if (style.Structure != structure)
					style.Structure = structure;
				if (style.Function != function)
					style.Function = function;
				return style;
			}

			if (style.Type != type ||
				!CompatibleContext(style.Context, context) ||
				style.Structure != structure ||
				!CompatibleFunction(style.Function, function))
			{
				if (style.IsBuiltIn)
					ReportInvalidInstallation(String.Format(
						FrameworkStrings.ksCannotRedefineFactoryStyle, style.Name, ResourceFileName));

				// If style is in use, add it to the list so we can search through all
				// paragraphs and replace it with a new renamed style (and rename the style
				// itself, too);
				if (StyleIsInUse(style))
				{
					// ENHANCE: Prompt user to pick a different name to rename to
					// TODO: Check for collision - make sure we get a unique name
					string sNewName = style.Name +
						ResourceHelper.GetResourceString("kstidUserStyleSuffix");
					m_replacedStyles.Add(new StyleReplacement(style.Name, sNewName));
					style.Name = sNewName;
				}
				IStStyle newFactoryStyle = new StStyle();
				m_databaseStyles.Add(newFactoryStyle);
				return newFactoryStyle;
			}
			if (style.Context != context)
				style.Context = context;
			if (style.Function != function)
				style.Function = function;
			return style;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// If the proposed context for a style is internal or internalMappable, make sure the
		/// program actually expects and supports this context for this style.
		/// </summary>
		/// <param name="style">The style being updated</param>
		/// <param name="proposedContext">The proposed context for the style</param>
		/// <returns><c>true</c>if the proposed context is internal or internal mappable and
		/// the program recognizes it as a valid</returns>
		/// -------------------------------------------------------------------------------------
		public virtual bool IsValidInternalStyleContext(IStStyle style,
			ContextValues proposedContext)
		{
			// By default we don't recognize any style as 'internal'. TE overrides.
			return false;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Update the style context and do any special processing needed to deal with existing
		/// data that may be marked with the given style. (Since it was previous not an internal
		/// style, it is possible the user has used it in ways that would be incompatible with
		/// its intended use.) Any time a factory style is changesd to an internal context,
		/// specific code must be written here to deal with it. Some possible options for dealing
		/// with this scenario are:
		/// * Delete any data previously marked with the style (and possibly set some other
		///   object properties)
		/// * Add to the m_deletedStyles or m_replacedStyles arrays so existing data will be
		///   marked with a different style (note that this will only work if no existing data
		///   should be preserved with the style).
		/// </summary>
		/// <param name="style">The style being updated</param>
		/// <param name="context">The context (either internal or internal mappable) that the
		/// style is to be given</param>
		/// -------------------------------------------------------------------------------------
		protected virtual void ChangeFactoryStyleToInternal(IStStyle style, ContextValues context)
		{
			// By default nothing to do.
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Detemine whether the newly proposed context for a style is compatible with its
		/// current context.
		/// </summary>
		/// <param name="currContext">The existing context of the style</param>
		/// <param name="proposedContext">The context we want</param>
		/// <returns><c>true </c>if the passed in context can be upgraded as requested;
		/// <c>false</c> otherwise.</returns>
		/// -------------------------------------------------------------------------------------
		public static bool CompatibleContext(ContextValues currContext, ContextValues proposedContext)
		{
			if (currContext == proposedContext)
				return true;
			// Internal and InternalMappable are mutually compatible
			if ((currContext == ContextValues.InternalMappable && proposedContext == ContextValues.Internal) ||
				(proposedContext == ContextValues.InternalMappable && currContext == ContextValues.Internal))
				return true;
			// A (character) style having a specific Context can be made General
			return (proposedContext == ContextValues.General);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Detemine whether the newly proposed function for a style is compatible with its
		/// current function.
		/// </summary>
		/// <param name="currFunction">The existing function of the style</param>
		/// <param name="proposedFunction">The function we want</param>
		/// <returns><c>true </c>if the passed in function can be upgraded as requested;
		/// <c>false</c> otherwise.</returns>
		/// -------------------------------------------------------------------------------------
		public virtual bool CompatibleFunction(FunctionValues currFunction, FunctionValues proposedFunction)
		{
			return (currFunction == proposedFunction);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Third pass of style creation phase: delete (or make as user-defined styles) any
		/// styles that used to be factory styles but aren't any longer. If a deprecated style
		/// is being renamed or replaced with another style (which should already be created by
		/// now), add the replacement to the list (final step of style update process will be to
		/// crawl through the DB and replace all uses).
		/// </summary>
		/// -------------------------------------------------------------------------------------
		protected void DeleteDeprecatedStylesAndDetermineReplacements()
		{
			int nMin, nMax;
			m_progressDlg.GetRange(out nMin, out nMax);
			m_progressDlg.SetRange(nMin, nMax + Math.Max(0, m_htOrigStyles.Count - m_htUpdatedStyles.Count));

			foreach (IStStyle style in m_htOrigStyles.Values)
			{
				string styleName = style.Name;
				if (style.IsBuiltIn && !m_htUpdatedStyles.ContainsKey(styleName))
				{
					m_progressDlg.Step(0);
					m_progressDlg.Message =
						string.Format(ResourceHelper.GetResourceString("kstidDeletingStylesStatusMsg"),
						styleName);

					ContextValues oldContext = (ContextValues)style.Context;
					bool fStyleInUse = StyleIsInUse(style);

					// TODO: If the style is in use then it needs to be changed to
					// a user-defined style instead of being deleted, unless it's an internal style.
					m_databaseStyles.Remove(style);

					// if the style is in use, set things up to replace/remove it in the data
					if (fStyleInUse)
					{
						// If the factory style has been renamed or replaced with another
						// factory style, then all instances of it have to be converted, so
						// add it to the replacement list.
						XmlNode change = m_sourceStyles.SelectSingleNode(
							"replacements/change[@old='" + styleName.Replace(" ", "_") + "']");
						if (change != null)
						{
							string replStyleName =
								change.Attributes.GetNamedItem("new").Value.Replace("_", " ");
							IStStyle repl = m_htUpdatedStyles[replStyleName];
							if (!CompatibleContext(oldContext, (ContextValues)repl.Context) &&
								!StyleReplacementAllowed(styleName, replStyleName))
								ReportInvalidInstallation(String.Format(
									FrameworkStrings.ksCannotReplaceXwithY, styleName, replStyleName, ResourceFileName));

							m_replacedStyles.Add(new StyleReplacement(styleName, replStyleName));
						}
						else
						{
							m_deletedStyles.Add(styleName);
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Special overridable method to allow application-specific overrides to allow a
		/// particular style to be renamed.
		/// </summary>
		/// <param name="styleName">Name of the original style.</param>
		/// <param name="replStyleName">Name of the replacement style.</param>
		/// <returns>The default always returns <c>false</c>; but an application may
		/// override this to return <c>true</c> for a specific pair of stylenames.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool StyleReplacementAllowed(string styleName, string replStyleName)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given style is (possibly) in use.
		/// </summary>
		/// <remarks>This method is virtual to allow for applications (such as FLEx) that may
		/// not have made good use of the InUse property of styles.</remarks>
		/// <param name="style">The style.</param>
		/// <returns><c>true</c> if there is any reasonable chance the given style is in use
		/// somewhere in the project data; <c>false</c> if the style has never been used and
		/// there is no real possibility it could be in the data.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool StyleIsInUse(IStStyle style)
		{
			return style.InUse;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Final step of style update process is to crawl through the DB and replace all uses of
		/// any deprecated or renamed styles
		/// </summary>
		/// -------------------------------------------------------------------------------------
		protected virtual void ReplaceFormerStyles()
		{
			if (m_replacedStyles.Count == 0 && m_deletedStyles.Count == 0)
				return;

			int nMin, nMax;
			m_progressDlg.GetRange(out nMin, out nMax);
			m_progressDlg.SetRange(nMin, nMax + 1);
			m_progressDlg.Position = nMax;
			m_progressDlg.Message = ResourceHelper.GetResourceString("kstidReplacingStylesStatusMsg");

			IFwDbMergeStyles styleMerger = FwDbMergeStylesClass.Create();
			// TODO: Figure out how/if to pass a log file stream
			Guid clsidApp = FwApp.App.SyncGuid;
			styleMerger.Initialize(ResourceOwner.Cache.ServerName, ResourceOwner.Cache.DatabaseName, null,
				ResourceOwner.Hvo, ref clsidApp);
			foreach (StyleReplacement repl in m_replacedStyles)
				styleMerger.AddStyleReplacement(repl.oldStyle, repl.newStyle);
			foreach (string sDeletedStyle in m_deletedStyles)
				styleMerger.AddStyleDeletion(sDeletedStyle);
			uint hWnd = 0;
			if (m_progressDlg is Control)
				hWnd = (uint)((Control)m_progressDlg).Handle.ToInt32();
			styleMerger.Process(hWnd);
		}
		#endregion

		#region Style creation methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read the font properties from the XML node and set the properties in the given
		/// props builder.
		/// </summary>
		/// <param name="styleName">Name of style being created/updated (for error reporting)
		/// </param>
		/// <param name="styleTag">XML node that has the font properties</param>
		/// <param name="propsBldr">the props builder to store the props</param>
		/// ------------------------------------------------------------------------------------
		protected void SetFontProperties(string styleName, XmlNode styleTag,
			ITsPropsBldr propsBldr)
		{
			// Get character properties
			XmlAttributeCollection fontAttributes =
				styleTag.SelectSingleNode("font").Attributes;

			XmlNode node = fontAttributes.GetNamedItem("italic");
			if (node != null)
			{
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptItalic,
					(int)FwTextPropVar.ktpvEnum,
					GetBoolAttribute(fontAttributes, "italic", styleName, ResourceFileName) ?
					(int)FwTextToggleVal.kttvInvert :
					(int)FwTextToggleVal.kttvOff);
			}

			node = fontAttributes.GetNamedItem("bold");
			if (node != null)
			{
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptBold,
					(int)FwTextPropVar.ktpvEnum,
					GetBoolAttribute(fontAttributes, "bold", styleName, ResourceFileName) ?
					(int)FwTextToggleVal.kttvInvert :
					(int)FwTextToggleVal.kttvOff);
			}

			node = fontAttributes.GetNamedItem("superscript");
			if (node != null)
			{
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptSuperscript,
					(int)FwTextPropVar.ktpvEnum,
					GetBoolAttribute(fontAttributes, "superscript", styleName, ResourceFileName) ?
					(int)FwSuperscriptVal.kssvSuper :
					(int)FwSuperscriptVal.kssvOff);
			}

			node = fontAttributes.GetNamedItem("size");
			if (node != null)
			{
				int nSize = InterpretMeasurementAttribute(node.Value, "size", styleName, ResourceFileName);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptFontSize,
					(int)FwTextPropVar.ktpvMilliPoint, nSize);
			}

			node = fontAttributes.GetNamedItem("color");
			string sColor = (node == null ? "default" : node.Value);
			if (sColor != "default")
			{
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptForeColor,
					(int)FwTextPropVar.ktpvDefault,
					ColorVal(sColor, styleName));
			}

			node = fontAttributes.GetNamedItem("underlineColor");
			sColor = (node == null ? "default" : node.Value);
			if (sColor != "default")
			{
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptUnderColor,
					(int)FwTextPropVar.ktpvDefault,
					ColorVal(sColor, styleName));
			}

			node = fontAttributes.GetNamedItem("underline");
			string sUnderline = (node == null) ? null : node.Value;
			if (sUnderline != null)
			{
				int unt = InterpretUnderlineType(sUnderline);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptUnderline,
					(int)FwTextPropVar.ktpvEnum,
					unt);
			}

			node = fontAttributes.GetNamedItem("spellcheck");
			bool fSpellcheck = (node == null ? true : (node.Value == "true"));
			if (!fSpellcheck)
			{
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptSpellCheck,
					(int)FwTextPropVar.ktpvEnum, (int)SpellingModes.ksmDoNotCheck);
			}

			// TODO: Handle dropcap attribute

			node = fontAttributes.GetNamedItem("type");
			if (node != null && node.Value != string.Empty)
			{
				string sFontFamily = node.Value;
				switch (sFontFamily)
				{
					case "heading":
						propsBldr.SetStrPropValue(
							(int)FwTextPropType.ktptFontFamily,
							StStyle.DefaultHeadingFont);
						break;
					case "default":
						propsBldr.SetStrPropValue(
							(int)FwTextPropType.ktptFontFamily,
							StStyle.DefaultFont);
						break;
					case "publication":
						propsBldr.SetStrPropValue(
							(int)FwTextPropType.ktptFontFamily,
							StStyle.DefaultPubFont);
						break;
					default:
						ReportInvalidInstallation(String.Format(
							FrameworkStrings.ksUnknownFontType, styleName, ResourceFileName));
						break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Color value may be (red, green, blue) or one of the KnownColor values.
		/// Adapted from XmlVc routine.
		/// </summary>
		/// <param name="val">Value to interpret (a color name or (red, green, blue).</param>
		/// <param name="styleName">name of the style (for error reporting)</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		int ColorVal(string val, string styleName)
		{
			if (val[0] == '(')
			{
				int firstComma = val.IndexOf(',');
				int red = Convert.ToInt32(val.Substring(1,firstComma - 1));
				int secondComma = val.IndexOf(',', firstComma + 1);
				int green = Convert.ToInt32(val.Substring(firstComma + 1, secondComma - firstComma - 1));
				int blue = Convert.ToInt32(val.Substring(secondComma + 1, val.Length - secondComma - 2));
				return red + (blue * 256 + green) * 256;
			}
			Color col = Color.FromName(val);
			if (col.ToArgb() == 0)
			{
				ReportInvalidInstallation(String.Format(
					FrameworkStrings.ksUnknownUnderlineColor, styleName, ResourceFileName));
			}
			return col.R + (col.B * 256 + col.G) * 256;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Interpret an underline type string as an FwUnderlineType.
		/// Note that this is a duplicate of the routine on XmlVc (due to avoiding assembly references). Keep in sync.
		/// </summary>
		/// <param name="strVal"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public int InterpretUnderlineType(string strVal)
		{
			int val = (int)FwUnderlineType.kuntSingle; // default
			switch(strVal)
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
				default:
					Debug.Assert(false, "Expected value single, none, double, dotted, dashed, or squiggle");
					break;
			}
			return val;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read the paragraph properties from the XML node and set the properties in the given
		/// props builder.
		/// </summary>
		/// <param name="styleName">Name of style being created/updated (for error reporting)
		/// </param>
		/// <param name="styleTag">XML node that has the paragraph properties</param>
		/// <param name="propsBldr">the props builder to store the props</param>
		/// ------------------------------------------------------------------------------------
		protected void SetParagraphProperties(string styleName, XmlNode styleTag,
			ITsPropsBldr propsBldr)
		{
			XmlNode node = styleTag.SelectSingleNode("paragraph");
			if (node == null)
			{
				ReportInvalidInstallation(String.Format(
					FrameworkStrings.ksMissingParagraphNode, styleName, ResourceFileName));
			}
			XmlAttributeCollection paraAttributes = node.Attributes;

			// Set alignment
			node = paraAttributes.GetNamedItem("alignment");
			if (node != null)
			{
				string sAlign = node.Value;
				int nAlign = (int)FwTextAlign.ktalLeading;
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
						ReportInvalidInstallation(String.Format(
							FrameworkStrings.ksUnknownAlignmentValue, styleName, ResourceFileName));
						break;
				}
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptAlign,
					(int)FwTextPropVar.ktpvEnum, nAlign);
			}

			node = paraAttributes.GetNamedItem("background");
			if (node != null && node.Value != "white")
				ReportInvalidInstallation(String.Format(
					FrameworkStrings.ksUnknownBackgroundValue, styleName, ResourceFileName));

			// set leading indentation
			node = paraAttributes.GetNamedItem("indentLeft");
			if (node != null)
			{
				int nLeftIndent = InterpretMeasurementAttribute(node.Value, "indentLeft",
					styleName, ResourceFileName);
				propsBldr.SetIntPropValues(
					(int)FwTextPropType.ktptLeadingIndent,
					(int)FwTextPropVar.ktpvMilliPoint, nLeftIndent);
			}

			// Set trailing indentation
			node = paraAttributes.GetNamedItem("indentRight");
			if (node != null)
			{
				int nRightIndent = InterpretMeasurementAttribute(node.Value, "indentRight",
					styleName, ResourceFileName);
				propsBldr.SetIntPropValues(
					(int)FwTextPropType.ktptTrailingIndent,
					(int)FwTextPropVar.ktpvMilliPoint, nRightIndent);
			}

			// Set first-line/hanging indentation
			int nFirstIndent = 0;
			bool fFirstLineOrHangingIndentSpecified = false;
			node = paraAttributes.GetNamedItem("firstLine");
			if (node != null)
			{
				nFirstIndent = InterpretMeasurementAttribute(node.Value, "firstLine",
					styleName, ResourceFileName);
				fFirstLineOrHangingIndentSpecified = true;
			}
			int nHangingIndent = 0;
			node = paraAttributes.GetNamedItem("hanging");
			if (node != null)
			{
				nHangingIndent = InterpretMeasurementAttribute(node.Value, "hanging",
					styleName, ResourceFileName);
				fFirstLineOrHangingIndentSpecified = true;
			}

			if (nFirstIndent != 0 && nHangingIndent != 0)
				ReportInvalidInstallation(String.Format(
					FrameworkStrings.ksInvalidFirstLineHanging, styleName, ResourceFileName));

			nFirstIndent -= nHangingIndent;
			if (fFirstLineOrHangingIndentSpecified)
			{
				propsBldr.SetIntPropValues(
					(int)FwTextPropType.ktptFirstIndent,
					(int)FwTextPropVar.ktpvMilliPoint,
					nFirstIndent);
			}

			// Set space before
			node = paraAttributes.GetNamedItem("spaceBefore");
			if (node != null)
			{
				int nSpaceBefore = InterpretMeasurementAttribute(node.Value, "spaceBefore",
					styleName, ResourceFileName);
				propsBldr.SetIntPropValues(
					(int)FwTextPropType.ktptSpaceBefore,
					(int)FwTextPropVar.ktpvMilliPoint, nSpaceBefore);
			}

			// Set space after
			node = paraAttributes.GetNamedItem("spaceAfter");
			if (node != null)
			{
				int nSpaceAfter = InterpretMeasurementAttribute(node.Value, "spaceAfter",
					styleName, ResourceFileName);
				propsBldr.SetIntPropValues(
					(int)FwTextPropType.ktptSpaceAfter,
					(int)FwTextPropVar.ktpvMilliPoint, nSpaceAfter);
			}

			// Set lineSpacing
			node = paraAttributes.GetNamedItem("lineSpacingType");
			string sLineSpacingType = "";
			if (node != null)
			{
				sLineSpacingType = node.Value;
				switch (sLineSpacingType)
				{
						//verify valid line spacing types
					case "atleast":
						break;
					case "exact":
						break;
					default:
						ReportInvalidInstallation(String.Format(
							FrameworkStrings.ksUnknownLineSpacingValue, styleName, ResourceFileName));
						break;
				}
			}

			node = paraAttributes.GetNamedItem("lineSpacing");
			if (node != null)
			{
				int lineSpacing = InterpretMeasurementAttribute(node.Value, "lineSpacing", styleName, ResourceFileName);
				if (lineSpacing < 0)
				{
					ReportInvalidInstallation(String.Format(
						FrameworkStrings.ksNegativeLineSpacing, styleName, ResourceFileName));
				}
				if(sLineSpacingType == "exact")
				{
					lineSpacing *= -1; // negative lineSpacing indicates exact line spacing
				}

				propsBldr.SetIntPropValues(
					(int)FwTextPropType.ktptLineHeight,
					(int)FwTextPropVar.ktpvMilliPoint, lineSpacing);
			}

			// Set borders
			node = paraAttributes.GetNamedItem("border");
			if (node != null)
			{
				int nBorder = 0;
				switch (node.Value)
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
						ReportInvalidInstallation(String.Format(
							FrameworkStrings.ksUnknownBorderValue, styleName, ResourceFileName));
						break;
				}
				propsBldr.SetIntPropValues(nBorder, (int)FwTextPropVar.ktpvDefault,
					500);
			}

			node = paraAttributes.GetNamedItem("keepWithNext");
			if (node != null)
			{
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptKeepWithNext,
					(int)FwTextPropVar.ktpvEnum,
					GetBoolAttribute(paraAttributes, "keepWithNext", styleName, ResourceFileName) ?
					(int)FwTextToggleVal.kttvForceOn :
					(int)FwTextToggleVal.kttvOff);
			}

			node = paraAttributes.GetNamedItem("keepTogether");
			if (node != null)
			{
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptKeepTogether,
					(int)FwTextPropVar.ktpvEnum,
					GetBoolAttribute(paraAttributes, "keepTogether", styleName, ResourceFileName) ?
					(int)FwTextToggleVal.kttvForceOn :
					(int)FwTextToggleVal.kttvOff);
			}

			node = paraAttributes.GetNamedItem("widowOrphan");
			if (node != null)
			{
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptWidowOrphanControl,
					(int)FwTextPropVar.ktpvEnum,
					GetBoolAttribute(paraAttributes, "widowOrphan", styleName, ResourceFileName) ?
					(int)FwTextToggleVal.kttvForceOn :
					(int)FwTextToggleVal.kttvOff);
			}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Get the ws value (hvo) from the iculocale contained in the given attributes
		/// </summary>
		/// <param name="attribs">Collection of attributes that better have an "iculocale"
		/// attribute</param>
		/// <returns></returns>
		/// -------------------------------------------------------------------------------------
		private int GetWs(XmlAttributeCollection attribs)
		{
			if (m_htIcuToWs == null)
			{
				m_lgwsCollection = ResourceOwner.Cache.LanguageEncodings;
				m_htIcuToWs = new Dictionary<string, int>(1);
			}
			string iculocale = attribs.GetNamedItem("iculocale").Value;
			if (iculocale == null || iculocale == string.Empty)
				return 0;
			if (!m_htIcuToWs.ContainsKey(iculocale))
				m_htIcuToWs[iculocale] = m_lgwsCollection.GetWsFromIcuLocale(iculocale);
			return m_htIcuToWs[iculocale];
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Second pass of style creation phase: set up "based-on" and "next" styles
		/// </summary>
		/// <param name="tagList">List of XML nodes representing factory styles to create</param>
		/// -------------------------------------------------------------------------------------
		private void SetBasedOnAndNextProps(XmlNodeList tagList)
		{
			foreach (XmlNode styleTag in tagList)
			{
				XmlAttributeCollection attributes = styleTag.Attributes;

				string styleName = GetStyleName(attributes);
				ContextValues context = GetContext(attributes, styleName);
				if (IsExcludedContext(context))
					continue;

				IStStyle style = m_htUpdatedStyles[styleName];
				// No need now to do the assert,
				// since the Dictionary will throw an exception,
				// if the key isn't present.
				//Debug.Assert(style != null);

				m_progressDlg.Step(0);
				m_progressDlg.Message =
					string.Format(ResourceHelper.GetResourceString("kstidUpdatingStylesStatusMsg"),
					styleName);

				if (style.Type == StyleType.kstParagraph)
				{
					XmlAttributeCollection paraAttributes =
						styleTag.SelectSingleNode("paragraph").Attributes;


					if (styleName != StStyle.NormalStyleName)
					{
						string sBasedOnStyleName = GetBasedOn(paraAttributes, styleName);

						if (sBasedOnStyleName == null || sBasedOnStyleName == string.Empty)
							ReportInvalidInstallation(String.Format(
								FrameworkStrings.ksMissingBasedOnStyle, styleName, ResourceFileName));

						if (!m_htUpdatedStyles.ContainsKey(sBasedOnStyleName))
							ReportInvalidInstallation(String.Format(
								FrameworkStrings.ksUnknownBasedOnStyle, styleName, sBasedOnStyleName));

						IStStyle basedOnStyle = m_htUpdatedStyles[sBasedOnStyleName];
						if (basedOnStyle.Hvo == style.Hvo)
							ReportInvalidInstallation(String.Format(
								FrameworkStrings.ksNoBasedOnSelf, styleName, ResourceFileName));

						style.BasedOnRA = basedOnStyle;
					}

					string sNextStyleName = null;
					if (s_htReservedStyles.ContainsKey(styleName))
					{
						ReservedStyleInfo info = s_htReservedStyles[styleName];
						sNextStyleName = info.nextStyle;
					}
					else
					{
						XmlNode next = paraAttributes.GetNamedItem("next");
						if (next != null)
							sNextStyleName = next.Value.Replace("_", " ");
					}

					if (sNextStyleName != null && sNextStyleName != string.Empty)
					{
						if (!m_htUpdatedStyles.ContainsKey(sNextStyleName))
							ReportInvalidInstallation(String.Format(
								FrameworkStrings.ksUnknownNextStyle, styleName, sNextStyleName, ResourceFileName));

						if (m_htUpdatedStyles.ContainsKey(sNextStyleName))
							style.NextRA = m_htUpdatedStyles[sNextStyleName];
						else
							style.NextRA = null;
					}
				}
			}
			SetBasedOnAndNextPropsReserved();
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Second pass of style creation phase for reserved styles that weren't in the external
		/// XML stylesheet: set up "based-on" and "next" styles
		/// </summary>
		/// -------------------------------------------------------------------------------------
		private void SetBasedOnAndNextPropsReserved()
		{
			foreach (string styleName in s_htReservedStyles.Keys)
			{
				ReservedStyleInfo info = s_htReservedStyles[styleName];
				if (!info.created)
				{
					IStStyle style = m_htUpdatedStyles[styleName];
					// No need now to do the assert,
					// since the Dictionary will throw an exception,
					// if the key isn't present.
					//Debug.Assert(style != null);

					if (style.Type == StyleType.kstParagraph)
					{
						m_progressDlg.Message =
							string.Format(ResourceHelper.GetResourceString("kstidUpdatingStylesStatusMsg"),
							styleName);

						IStStyle newStyle = null;
						if (styleName != StStyle.NormalStyleName)
						{
							if (m_htUpdatedStyles.ContainsKey(info.basedOn))
								newStyle = m_htUpdatedStyles[info.basedOn];
							style.BasedOnRA = newStyle;
						}

						newStyle = null;
						if (m_htUpdatedStyles.ContainsKey(info.nextStyle))
							newStyle = m_htUpdatedStyles[info.nextStyle];
						style.NextRA = newStyle;
					}
				}
			}
		}
		#endregion

		#region Read XML Attributes
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Interprets a given attribute as a boolean value
		/// </summary>
		/// <param name="attributes">Collection of attributes to look in</param>
		/// <param name="sAttrib">Named attribute</param>
		/// <param name="styleName">The name of the style to which this attribute pertains (used
		/// only for debug error reporting)</param>
		/// <param name="fileName">Name of XML file (for error reporting)</param>
		/// <returns>true if attribute value is "yes" or "true"</returns>
		/// ------------------------------------------------------------------------------------
		static public bool GetBoolAttribute(XmlAttributeCollection attributes, string sAttrib,
			string styleName, string fileName)
		{
			string sVal = attributes.GetNamedItem(sAttrib).Value;
			if (sVal == "yes" || sVal == "true")
				return true;
			else if (sVal == "no" || sVal == "false" || sVal == String.Empty)
				return false;

			ReportInvalidInstallation(String.Format(
				FrameworkStrings.ksUnknownStyleAttribute, sAttrib, styleName, fileName));
			return false; // Can't actually get here, but don't tell the compiler that!
		}

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		static public int InterpretMeasurementAttribute(string sSize, string sAttrib,
			string styleName, string fileName)
		{
			sSize = sSize.Trim();
			if (sSize.Length >= 4)
			{
				string number = sSize.Substring(0, sSize.Length - 3);
				if (sSize.EndsWith(" pt"))
					return (int)(double.Parse(number, new CultureInfo("en-US")) * 1000.0);
				else if (sSize.EndsWith(" in"))
					return (int)(double.Parse(number, new CultureInfo("en-US")) * 72000.0);
				else
					ReportInvalidInstallation(String.Format(
						FrameworkStrings.ksUnknownAttrUnits, sAttrib, styleName, fileName));
			}
			return 0;
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns whether or not the context of the specified style is excluded (i.e., should
		/// not result in the creation of a database Style). Default is to exclude nothing.
		/// </summary>
		/// <param name="context">The context to test</param>
		/// <returns>True if the context is excluded, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool IsExcludedContext(ContextValues context)
		{
			return false;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves a valid TE style name from the specified attributes.
		/// </summary>
		/// <param name="attributes">The attributes containing the style id to use</param>
		/// <returns>a valid TE style name</returns>
		/// ------------------------------------------------------------------------------------
		private static string GetStyleName(XmlAttributeCollection attributes)
		{
			return attributes.GetNamedItem("id").Value.Replace("_", " ");
		}
		#endregion
	}

	#region struct StyleReplacement
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A nice little struct for holding an old and new filename
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public struct StyleReplacement
	{
		/// <summary>Old style name</summary>
		public string oldStyle;
		/// <summary>New style name</summary>
		public string newStyle;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes one
		/// </summary>
		/// <param name="oldName">Old style name</param>
		/// <param name="newName">New style name</param>
		/// ------------------------------------------------------------------------------------
		public StyleReplacement(string oldName, string newName)
		{
			oldStyle = oldName;
			newStyle = newName;
		}
	}
	#endregion

	#region ReservedStyleInfo class
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Holds info about certain reserved styles. This info overrides any conflicting info
	/// in the external XML stylesheet
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class ReservedStyleInfo
	{
		/// <summary> </summary>
		public bool created = false;
		/// <summary> </summary>
		public ContextValues context;
		/// <summary> </summary>
		public StructureValues structure;
		/// <summary> </summary>
		public FunctionValues function;
		/// <summary> </summary>
		public StyleType styleType;
		/// <summary> </summary>
		public string nextStyle;
		/// <summary> </summary>
		public string basedOn;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// General-purpose constructor
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="structure">Structure</param>
		/// <param name="function">Function</param>
		/// <param name="styleType">Paragraph or character</param>
		/// <param name="nextStyle">Name of "Next" style, or null if this is info about a
		/// character style</param>
		/// <param name="basedOn">Name of base style, or null if this is info about a
		/// character style </param>
		/// --------------------------------------------------------------------------------
		public ReservedStyleInfo(ContextValues context, StructureValues structure,
			FunctionValues function, StyleType styleType, string nextStyle,
			string basedOn)
		{
			this.context = context;
			this.structure = structure;
			this.function = function;
			this.styleType = styleType;
			this.nextStyle = nextStyle;
			this.basedOn = basedOn;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for character style info
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="structure">Structure</param>
		/// <param name="function">Function</param>
		/// --------------------------------------------------------------------------------
		public ReservedStyleInfo(ContextValues context, StructureValues structure,
			FunctionValues function) : this(context, structure, function,
			StyleType.kstCharacter, null, null)
		{
		}
	}
	#endregion
}
