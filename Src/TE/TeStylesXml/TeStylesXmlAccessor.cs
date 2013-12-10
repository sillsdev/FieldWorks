// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2004' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeStylesXmlAccessor.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class containing static methods for accessing the information about factory styles in
	/// the TeStyles.xml file.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeStylesXmlAccessor : StylesXmlAccessor
	{
		#region Data members
		/// <summary>The FDO Scripture object which will own the new styles</summary>
		protected IScripture m_scr;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeStylesXmlAccessor"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected TeStylesXmlAccessor()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeStylesXmlAccessor"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected TeStylesXmlAccessor(IScripture scr) : base(scr.Cache)
		{
			m_scr = scr;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Required implementation of abstract property to get the relative path to stylesheet
		/// configuration file from the FieldWorks install folder.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		protected override string ResourceFilePathFromFwInstall
		{
			get { return Path.DirectorySeparatorChar + ResourceFileName;}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Required implementation of abstract property to get the name of the stylesheet
		/// configuration file.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		protected override string ResourceName
		{
			get { return Path.GetFileNameWithoutExtension(FwDirectoryFinder.kTeStylesFilename); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the resource list in which the CmResources are owned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override IFdoOwningCollection<ICmResource> ResourceList
		{
			get { return m_scr.ResourcesOC; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the FdoCache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override FdoCache Cache
		{
			get { return m_scr.Cache; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The collection that owns the styles; for example, Scripture.StylesOC.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		protected override IFdoOwningCollection<IStStyle> StyleCollection
		{
			get { return m_scr.StylesOC; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Complain if the context is not valid for the tool that is loading the styles.
		/// TE currently allows all but Flex's private context
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override void ValidateContext(ContextValues context, string styleName)
		{
			if (context == ContextValues.InternalConfigureView)
			{
				ReportInvalidInstallation(String.Format(
					"Style {0} is illegally defined with context '{1}' in {2}.",
					styleName, context, ResourceFileName));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Throw an exception if the specified context is not valid for the specified paragraph
		/// style. TE overrides to forbid 'general' paragraph styles.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="styleName"></param>
		/// ------------------------------------------------------------------------------------
		protected override void ValidateParagraphContext(ContextValues context, string styleName)
		{
			if (context == ContextValues.General)
				ReportInvalidInstallation(String.Format(
					"Paragraph style {0} is illegally defined with context '{1}' in TeStyles.xml.",
					styleName, context));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Special overridable method to allow application-specific overrides to allow a
		/// particular style to be renamed.
		/// </summary>
		/// <param name="styleName">Name of the original style.</param>
		/// <param name="replStyleName">Name of the replacement style.</param>
		/// <returns>
		/// <c>true</c> for replacing specific annotation styles with the Remark style;
		/// <c>false</c> (the base implementation) otherwise.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		protected override bool StyleReplacementAllowed(string styleName, string replStyleName)
		{
			if (replStyleName == ScrStyleNames.Remark)
			{
				switch (styleName)
				{
					case "Notation Discussion":
					case "Notation Quote":
					case "Notation Resolution":
					case "Notation Suggestion":
						return true;
				}
			}
			return base.StyleReplacementAllowed(styleName, replStyleName);
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
		protected override bool StyleIsInUse(IStStyle style)
		{
			if ((style.Context == ContextValues.Internal &&
				!ScrStyleNames.InternalStyles.Contains(style.Name)) ||
				(style.Context == ContextValues.InternalMappable &&
				!ScrStyleNames.InternalMappableStyles.Contains(style.Name)))
			{
				return false;
			}
			return style.InUse;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a style has been modified by the user, this method will be called to determine
		/// whether the factory settings or the modified properties should be used.
		/// </summary>
		/// <param name="style">The style.</param>
		/// <param name="styleTag">The style tag.</param>
		/// <returns>OverwriteOptions.Skip to indicate that the caller should not alter the
		/// user-modified style;
		/// OverwriteOptions.FunctionalPropertiesOnly to indicate that the caller should update
		/// the functional properties of the style but leave the user-modified properties that
		/// affect only the appearance;
		/// OverwriteOptions.All to indicate that the caller should proceed with the style
		/// definition update, based on the information in the XML node.</returns>
		/// ------------------------------------------------------------------------------------
		protected override OverwriteOptions OverwriteUserModifications(IStStyle style,
			XmlNode styleTag)
		{
			return OverwriteOptions.FunctionalPropertiesOnly;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// If the current stylesheet version in the Db doesn't match that of the current XML
		/// file, update the DB.
		/// </summary>
		/// <param name="cache">The FDO cache</param>
		/// <param name="progressDlg">The progress dialog from the splash screen</param>
		/// <param name="helpTopicProvider">A Help topic provider that can serve up a help topic
		/// that only exists in TE Help.</param>
		/// -------------------------------------------------------------------------------------
		public static void EnsureCurrentStylesheet(FdoCache cache, IProgress progressDlg,
			IHelpTopicProvider helpTopicProvider)
		{
			TeStylesXmlAccessor acc = new TeStylesXmlAccessor(cache.LangProject.TranslatedScriptureOA);
			acc.EnsureCurrentResource(progressDlg);

			// This class is used specifically for TE styles; FLEx *should* use a different class,
			// but per LT-14704, that is not the case. So always checking for current styles, but
			// suppressing a potentially confusing dialog when TE is not installed.
			if (acc.UserModifiedStyles.Count > 0 && FwUtils.IsTEInstalled)
			{
				using (FwStylesModifiedDlg dlg = new FwStylesModifiedDlg(acc.UserModifiedStyles,
					cache.ProjectId.UiName, helpTopicProvider))
				{
					dlg.ShowDialog();
				}
			}
		}

		#region CreateFactoryScrStyles
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Create factory styles from the TE Styles XML file.
		/// </summary>
		/// <param name="progressDlg">Progress dialog so the user can cancel</param>
		/// <param name="scr">The Scripture</param>
		/// -------------------------------------------------------------------------------------
		public static void CreateFactoryScrStyles(IProgress progressDlg, IScripture scr)
		{
			TeStylesXmlAccessor acc = new TeStylesXmlAccessor(scr);
			acc.CreateStyles(progressDlg, scr.StylesOC, acc.LoadDoc());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Define reserved styles
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void InitReservedStyles()
		{
			base.InitReservedStyles();
			if (m_htReservedStyles.Count > 0)
			{
				foreach (string styleName in m_htReservedStyles.Keys)
					m_htReservedStyles[styleName].created = false;
				return;
			}
			// Reserved paragraph styles
			m_htReservedStyles[ScrStyleNames.Normal] = new ReservedStyleInfo(ContextValues.Internal,
				StructureValues.Undefined, FunctionValues.Prose, StyleType.kstParagraph,
				ScrStyleNames.NormalParagraph, null, "E00CA746-9271-40D0-B450-10087680FC29");

			m_htReservedStyles[ScrStyleNames.NormalParagraph] = new ReservedStyleInfo(
				ContextValues.Text,	StructureValues.Body, FunctionValues.Prose,
				StyleType.kstParagraph, ScrStyleNames.NormalParagraph, ScrStyleNames.Normal, "679AB46A-8A27-4449-91B1-EDAD14669B01");

			m_htReservedStyles[ScrStyleNames.SectionHead] = new ReservedStyleInfo(
				ContextValues.Text, StructureValues.Heading, FunctionValues.Prose,
				StyleType.kstParagraph, ScrStyleNames.NormalParagraph, ScrStyleNames.Normal, "A5A1A249-B888-434D-A839-A2421EC50DBF");

			m_htReservedStyles[ScrStyleNames.IntroParagraph] = new ReservedStyleInfo(
				ContextValues.Intro, StructureValues.Body, FunctionValues.Prose,
				StyleType.kstParagraph, ScrStyleNames.IntroParagraph, ScrStyleNames.NormalParagraph, "265CA52E-2543-46A3-BC8F-9B101F371EDB");

			m_htReservedStyles[ScrStyleNames.IntroSectionHead] = new ReservedStyleInfo(
				ContextValues.Intro, StructureValues.Heading, FunctionValues.Prose,
				StyleType.kstParagraph, ScrStyleNames.IntroParagraph, ScrStyleNames.SectionHead, "EADB9ADE-122A-40AC-8D17-216BEA9EE98F");

			m_htReservedStyles[ScrStyleNames.Remark] = new ReservedStyleInfo(
				ContextValues.Annotation, StructureValues.Undefined, FunctionValues.Prose,
				StyleType.kstParagraph, ScrStyleNames.Remark, ScrStyleNames.Normal, "20978416-EA15-4AB2-AED3-4CE825BB12FD");

			m_htReservedStyles[ScrStyleNames.MainBookTitle] = new ReservedStyleInfo(
				ContextValues.Title, StructureValues.Undefined, FunctionValues.Prose,
				StyleType.kstParagraph, ScrStyleNames.MainBookTitle, ScrStyleNames.SectionHead, "7473C95B-26ED-4FDA-8F31-C8AA0C25F2AA");

			m_htReservedStyles[ScrStyleNames.NormalFootnoteParagraph] = new ReservedStyleInfo(
				ContextValues.Note, StructureValues.Undefined, FunctionValues.Prose,
				StyleType.kstParagraph, ScrStyleNames.NormalFootnoteParagraph, ScrStyleNames.NormalParagraph, "281acf34-3292-43af-8bd2-7441f3e675ec");
			m_htReservedStyles[ScrStyleNames.CrossRefFootnoteParagraph] = new ReservedStyleInfo(
				ContextValues.Note, StructureValues.Undefined, FunctionValues.Prose,
				StyleType.kstParagraph, ScrStyleNames.CrossRefFootnoteParagraph, ScrStyleNames.NormalFootnoteParagraph, "98d64e6e-7862-4327-957d-f1e4a4734ad3");

			m_htReservedStyles[ScrStyleNames.Figure] = new ReservedStyleInfo(
				ContextValues.Internal, StructureValues.Undefined, FunctionValues.Prose,
				StyleType.kstParagraph, ScrStyleNames.Figure, ScrStyleNames.Normal, "a39a474f-2ade-409d-aa11-59660fcc3e20");

			m_htReservedStyles[ScrStyleNames.Header] = new ReservedStyleInfo(
				ContextValues.Internal, StructureValues.Undefined, FunctionValues.Prose,
				StyleType.kstParagraph, ScrStyleNames.Header, ScrStyleNames.Normal, "85324146-31B4-4FE7-BA34-F168CE97F390");

			// Reserved character styles
			m_htReservedStyles[ScrStyleNames.ChapterNumber] = new ReservedStyleInfo(
				ContextValues.Text, StructureValues.Body, FunctionValues.Chapter, "DBF212FE-A9EF-4CC8-A41F-7CAD28A99BD3");

			m_htReservedStyles[ScrStyleNames.VerseNumber] = new ReservedStyleInfo(
				ContextValues.Text, StructureValues.Body, FunctionValues.Verse, "674F0BDD-4240-49D7-9288-A6DCC130FF08");

			m_htReservedStyles[ScrStyleNames.CanonicalRef] = new ReservedStyleInfo(
				ContextValues.InternalMappable, StructureValues.Undefined, FunctionValues.Prose, "46EFE9FA-9EBA-430C-8716-8697DE190F46");

			m_htReservedStyles[ScrStyleNames.FootnoteMarker] = new ReservedStyleInfo(
				ContextValues.Internal, StructureValues.Undefined, FunctionValues.Prose, "1fddd539-729e-4ad4-b904-82897567f7e2");

			m_htReservedStyles[ScrStyleNames.FootnoteTargetRef] = new ReservedStyleInfo(
				ContextValues.InternalMappable, StructureValues.Undefined, FunctionValues.Footnote, "4db811b7-458c-46c7-a124-0f7815131848");

			m_htReservedStyles[ScrStyleNames.UntranslatedWord] = new ReservedStyleInfo(
				ContextValues.BackTranslation, StructureValues.Undefined, FunctionValues.Prose, "18F2BDA0-F1F4-4E68-B045-8FA5939C98E6");

			m_htReservedStyles[ScrStyleNames.NotationTag] = new ReservedStyleInfo(
				ContextValues.Internal, StructureValues.Undefined, FunctionValues.Prose, "B25C3DA1-E828-4984-B483-B29534E0CE09");
		}

		#endregion

		#region Style upgrade stuff
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
		public override bool CompatibleFunction(FunctionValues currFunction, FunctionValues proposedFunction)
		{
			return base.CompatibleFunction(currFunction, proposedFunction) ||
				(currFunction == FunctionValues.Line &&
				proposedFunction == FunctionValues.StanzaBreak);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// For any footnote in the specified book whose first paragraph has a run marked with
		/// the Note Target Reference style, remove that run and set the property on the footnote
		/// to display the target reference in the footnote view.
		/// </summary>
		/// <param name="book">The ScrBook whose footnotes are to be searched</param>
		/// -------------------------------------------------------------------------------------
		private void RemoveDirectUsesOfFootnoteTargetRef(IScrBook book)
		{
			foreach (IStFootnote footnote in book.FootnotesOS)
			{
				// Probably only need to worry about first para
				if (footnote.ParagraphsOS.Count == 0)
					continue;
				IStTxtPara para = (IStTxtPara)footnote.ParagraphsOS[0];
				ITsString tss = para.Contents;
				ITsStrBldr bldr = null;
				for (int iRun = 0; iRun < tss.RunCount; iRun++)
				{
					ITsTextProps props = tss.get_Properties(iRun);
					string style = props.GetStrPropValue(
						(int)FwTextPropType.ktptNamedStyle);
					if (style == ScrStyleNames.FootnoteTargetRef)
					{
						if (bldr == null)
							bldr = tss.GetBldr();
						bldr.Replace(tss.get_MinOfRun(iRun), tss.get_LimOfRun(iRun), string.Empty, null);
					}
				}
				if (bldr != null)
					para.Contents = bldr.GetString();
			}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Update the style context and do any special processing needed to deal with existing
		/// data that may be marked with the given style. (Since it was previously not an
		/// internal style, it is possible the user has used it in ways that would be
		/// incompatible with its intended use.) Any time a factory style is changed to an
		/// internal context, specific code must be written here to deal with it. Some possible
		/// options for dealing with this scenario are:
		/// * Delete any data previously marked with the style (and possibly set some other
		///   object properties)
		/// * Add to the m_styleReplacements dictionary so existing data will be marked with a
		///   different style (note that this will only work if no existing data should be
		///   preserved with the style).
		/// </summary>
		/// <param name="style">The style being updated</param>
		/// <param name="context">The context (either internal or internal mappable) that the
		/// style is to be given</param>
		/// -------------------------------------------------------------------------------------
		protected override void ChangeFactoryStyleToInternal(IStStyle style, ContextValues context)
		{
			if (!CompatibleContext(style.Context, context))
			{
				if (style.Name == ScrStyleNames.FootnoteTargetRef)
				{
					foreach (var book in m_scr.ScriptureBooksOS)
						RemoveDirectUsesOfFootnoteTargetRef(book);
					foreach (var draft in m_scr.ArchivedDraftsOC)
					{
						foreach (var book in draft.BooksOS)
							RemoveDirectUsesOfFootnoteTargetRef(book);
					}
				}
				else if (style.InUse)
				{
					// This is where we should handle any future upgrade issues
				}
			}
			style.Context = context;
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns whether or not the context of the specified style is excluded (i.e., should
		/// not result in the creation of a TE Style).
		/// </summary>
		/// <param name="context">The context to test</param>
		/// <returns>True if the context is excluded, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		protected override bool IsExcludedContext(ContextValues context)
		{
			return (base.IsExcludedContext(context) ||
				context == ContextValues.BackMatter ||
				context == ContextValues.Book ||
				context == ContextValues.Publication ||
				context == ContextValues.IntroTitle ||
				context == ContextValues.PsuedoStyle ||
				context == ContextValues.InternalConfigureView /* Only used in FLEx */);
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
		public override bool IsValidInternalStyleContext(IStStyle style,
			ContextValues proposedContext)
		{
			return ((proposedContext == ContextValues.Internal &&
				ScrStyleNames.InternalStyles.Contains(style.Name)) ||
				(proposedContext == ContextValues.InternalMappable &&
				ScrStyleNames.InternalMappableStyles.Contains(style.Name)));
		}

		/// <summary>
		/// Return true if this is the 'normal' style that all others inherit from. TE overrides.
		/// </summary>
		/// <param name="styleName"></param>
		/// <returns></returns>
		protected override bool IsNormalStyle(string styleName)
		{
			return styleName == ScrStyleNames.Normal;
		}

		#endregion

		#region GetHelpTopicForStyle
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a help topic with an example of how to use the given style. If the given
		/// style is not a factory style, then a general topic about using user-defined styles
		/// is returned.
		/// </summary>
		/// <param name="styleName">The  style name</param>
		/// <returns>A help topic with an example of how to use the given style, or if the given
		/// style is not a factory style, then a general topic about using user-defined styles
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static string GetHelpTopicForStyle(string styleName)
		{
			if (styleName == ResourceHelper.DefaultParaCharsStyleName)
			{
				return @"Redirect.htm#its:Using_Styles.chm::/Using_Styles/Styles_Grouped_by_Type/" +
					"Special_Text_and_Character_Styles/Default_Paragraph_Characters_description.htm";
			}

			string editedName = styleName.Replace(" ", "_");
			XmlNode teStyles = new TeStylesXmlAccessor().LoadDoc();
			try
			{
				XmlNode help =
					teStyles.SelectSingleNode("markup/tag[@id='" + editedName + "']/help");

				XmlNode category = null;
				if (help != null) // without this test it sometimes throws a nullref exception, which is annoying while debugging though caught below.
					category = help.Attributes.GetNamedItem("category");
				if (category != null)
				{
					return @"Redirect.htm#its:Using_Styles.chm::/Using_Styles/Styles_Grouped_by_Type/" +
						category.Value + "/" + editedName +
						((editedName == ScrStyleNames.Normal) ? "_description.htm" : "_example.htm");
				}
				XmlNode topic = null;
				if (help != null) // without this test it sometimes throws a nullref exception, which is annoying while debugging though caught below.
					topic = help.Attributes.GetNamedItem("topic");
				if (topic != null)
				{
					return topic.Value;
				}
			}
			catch
			{
			}
			return @"Advanced_Tasks/Customizing_Styles/User-defined_style.htm";
		}
		#endregion

		#region GetDefaultStyleForContext
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style name that is the default style to use for the given context (this is
		/// the static version)
		/// </summary>
		/// <param name="context">the context</param>
		/// <param name="fCharStyle">set to <c>true</c> for character styles; otherwise
		/// <c>false</c>.</param>
		/// <returns>
		/// Name of the style that is the default for the context
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static string GetDefaultStyleForContext(ContextValues context, bool fCharStyle)
		{
			if (fCharStyle)
			{
				// The current style is a character style, which means it should have a
				// context of "General". It should be impossible to create a TE
				// paragraph style with no specific Scripture context (i.e. a context of
				// General).
				if (context != ContextValues.General)
					throw new ArgumentException("Unexpected context for character style.");

				// The default style for character styles is "Default Paragraph Characters",
				// which is represented by string.Empty (TE-5875)
				return string.Empty;
			}

			switch (context)
			{
				case ContextValues.Annotation:
					return ScrStyleNames.Remark;
				case ContextValues.Intro:
					return ScrStyleNames.IntroParagraph;
				case ContextValues.Note:
					return ScrStyleNames.NormalFootnoteParagraph;
				case ContextValues.Text:
					return ScrStyleNames.NormalParagraph;
				case ContextValues.Title:
					return ScrStyleNames.MainBookTitle;
				default:
					throw new ArgumentException("Unexpected context for paragraph style.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style name that is the default style to use for the given context
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string DefaultStyleForContext(ContextValues context, bool fCharStyle)
		{
			return GetDefaultStyleForContext(context, fCharStyle);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the properties of a StyleInfo to the factory default settings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void SetPropsToFactorySettings(StyleInfo styleInfo)
		{
			TeStylesXmlAccessor acc = new TeStylesXmlAccessor(styleInfo.Cache.LanguageProject.TranslatedScriptureOA);
			acc.ResetProps(styleInfo);
		}
	}
}
