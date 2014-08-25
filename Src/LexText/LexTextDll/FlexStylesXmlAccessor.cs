using System;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks.LexText
{
	/// <summary>
	/// Specialization of StylesXmlAccessor for loading the Flex factory styles.
	/// </summary>
	public class FlexStylesXmlAccessor : StylesXmlAccessor
	{
		readonly ILexDb m_lexicon;

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="lexicon">The lexical database</param>
		/// -------------------------------------------------------------------------------------
		public FlexStylesXmlAccessor(ILexDb lexicon)
			: base(lexicon.Cache)
		{
			m_lexicon = lexicon;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Required implementation of abstract method gives relative path to configuration file
		/// from the FieldWorks install folder.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		protected override string ResourceFilePathFromFwInstall
		{
			get { return @"/Language Explorer/" + ResourceFileName; }
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Required implementation of abstract method gives name of the Flex styles sheet
		/// resource
		/// </summary>
		/// -------------------------------------------------------------------------------------
		protected override string ResourceName
		{
			get { return "FlexStyles"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the resource list in which the CmResources are owned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override IFdoOwningCollection<ICmResource> ResourceList
		{
			get { return m_lexicon.ResourcesOC; }
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Required implementation of abstract method gives style collection.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		protected override IFdoOwningCollection<IStStyle> StyleCollection
		{
			get { return m_cache.LangProject.StylesOC; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the FdoCache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override FdoCache Cache
		{
			get { return m_lexicon.Cache; }
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
		protected override bool StyleReplacementAllowed(string styleName, string replStyleName)
		{
			return (styleName == "External Link" && replStyleName == "Hyperlink") ||
				(styleName == "Internal Link" && replStyleName == "Hyperlink") ||
				(styleName == "Language Code" && replStyleName == "Writing System Abbreviation");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the current stylesheet version in the Db doesn't match that of the current XML
		/// file, update the DB.
		/// </summary>
		/// <param name="lp">The language project</param>
		/// <param name="progressDlg">The progress dialog.</param>
		/// ------------------------------------------------------------------------------------
		public static void EnsureCurrentStylesheet(ILangProject lp, IThreadedProgress progressDlg)
		{
			// We don't need to establish a NonUndoableUnitOfWork here because caller has already
			// done it and if not, the internal code of StylesXmlAccessor will do it for us.
			var acc = new FlexStylesXmlAccessor(lp.LexDbOA);
			acc.EnsureCurrentResource(progressDlg);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Complain if the context is not valid for the tool that is loading the styles.
		/// Flex currently allows general styles and its own special one.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="styleName"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override void ValidateContext(ContextValues context, string styleName)
		{
			if (context != ContextValues.InternalConfigureView &&
				context != ContextValues.Internal &&
				context != ContextValues.General)
				ReportInvalidInstallation(String.Format(
					"Style {0} is illegally defined with context '{1}' in {2}.",
					styleName, context, ResourceFileName));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given style is (possibly) in use.
		/// </summary>
		/// <remarks>This override is needed because previously hotlinks in FLEx were
		/// not have made good use of the InUse property of styles.</remarks>
		/// <param name="style">The style.</param>
		/// <returns><c>true</c> if there is any reasonable chance the given style is in use
		/// somewhere in the project data; <c>false</c> if the style has never been used and
		/// there is no real possibility it could be in the data.</returns>
		/// ------------------------------------------------------------------------------------
		protected override bool StyleIsInUse(IStStyle style)
		{
			return (style.Name == "External Link" || base.StyleIsInUse(style));
		}
	}
}
