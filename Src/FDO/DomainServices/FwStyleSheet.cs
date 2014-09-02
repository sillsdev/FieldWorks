// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwStyleSheet.cs
// Responsibility: FW Team
//
// <remarks>
// Implementation of FwStyleSheet
// This class provides a stylesheet (a collection of styles) which is managed by
// FwStylesDlg and passed to the initialization method of a VwRootBox to control
// the appearance of things.
// This class inherits from IVwStyleSheet in order to communicate with the Views code.
//
// FwStylesheet uses FDO cache, and its database, to maintain a collection of StStyles
// owned in a particular property of a particular object.
// Note that the former implementation was of two classes: AfStylesheet and AfDbStylesheet.
// AfStylesheet used an ISilDataAccess only, while FwDbStylesheet added the extra
// capabilities of CustViewDa to initially load the styles from the database.
// </remarks>

using System;
using System.Drawing;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Application;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class provides a stylesheet, maintaining a collection of <see cref="IStStyle"/>.
	/// The StStyle objects, defined by the FDO, maintain the style information in
	/// object/property form in an FDO cache.
	/// The FwStylesheet maintains a hash table of style names to fully derived ITsTextProps
	/// to provide fast access to the text props needed by a View.
	/// </summary>
	/// <remarks>
	///	<para>Main items: <see cref="IStStyle"/>.</para>
	///	<para>Interesting properties:</para>
	///	<list type="bullet">
	///		<item><see cref="IStStyle.Name"/> - the key for looking up the style in the style sheet.</item>
	///		<item><see cref="IStStyle.BasedOnRA"/> - used to maintain derived styles.</item>
	///		<item><see cref="IStStyle.NextRA"/> - the basis for implementing <see cref="GetNextStyle"/>.</item>
	///		<item><see cref="IStStyle.Type"/> - tells us whether to allow paragraph-level info.</item>
	///		<item><see cref="IStStyle.IsBuiltIn"/> - tells us if style is a predefined style.</item>
	///		<item><see cref="IStStyle.IsModified"/> - tells us if user has modified the predefined
	///			style.</item>
	///		<item><see cref="IStStyle.Rules"/> - binary data, actually an ITsTextProps, contains the style
	///			rules.</item>
	///	</list>
	///	</remarks>
	/// ----------------------------------------------------------------------------------------
	public class FwStyleSheet : IVwStylesheet, IStylesheet
	{
		#region NormalizationIndependentStringComparer
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare two strings independent of normalization
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class NormalizationIndependentStringComparer : EqualityComparer<string>
		{

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// When overridden in a derived class, determines whether two objects of type T
			/// are equal.
			/// </summary>
			/// <param name="x">The first object to compare.</param>
			/// <param name="y">The second object to compare.</param>
			/// <returns>
			/// true if the specified objects are equal; otherwise, false.
			/// </returns>
			/// --------------------------------------------------------------------------------
			public override bool Equals(string x, string y)
			{
				if (x == null && y == null)
					return true;
				if (x == null || y == null)
					return false;
				return x.Normalize().Equals(y.Normalize());
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// When overridden in a derived class, serves as a hash function for the specified
			/// object for hashing algorithms and data structures, such as a hash table.
			/// </summary>
			/// <param name="obj">The object for which to get a hash code.</param>
			/// <returns>A hash code for the specified object.</returns>
			/// <exception cref="T:System.ArgumentNullException">The type of obj is a reference
			/// type and obj is null.</exception>
			/// --------------------------------------------------------------------------------
			public override int GetHashCode(string obj)
			{
				if (obj == null)
					return 0;
				return obj.Normalize().GetHashCode();
			}
		}
		#endregion

		#region Class StyleInfoCollection
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides a collection of style infos.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public class StyleInfoCollection : KeyedCollection<string, BaseStyleInfo>
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="StyleInfoCollection"/> class.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public StyleInfoCollection()
				: base(new NormalizationIndependentStringComparer())
			{
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// When implemented in a derived class, extracts the key from the specified element.
			/// </summary>
			/// <param name="item">The element from which to extract the key.</param>
			/// <returns>The key for the specified element.</returns>
			/// --------------------------------------------------------------------------------
			protected override string GetKeyForItem(BaseStyleInfo item)
			{
				return item.Name.Normalize();
			}
		}
		#endregion // Class StyleInfoCollection

		#region Member variables
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Name of style used in Find/Replace dialog to represent no character style
		/// </summary>
		/// <remarks>The value of this constant must match the value used in VwPattern.cpp</remarks>
		/// ------------------------------------------------------------------------------------
		public const string kstrDefaultCharStyle = "<!default chars!>";
		/// <summary>our cache</summary>
		protected FdoCache m_fdoCache;
		/// <summary>owner of styles and this style sheet</summary>
		private int m_hvoStylesOwner;
		/// <summary>The field ID of the owner(m_hvoStylesOwner)'s collection of StStyles.</summary>
		private int m_tagStylesList;
		private FwStyleSheet m_styleSheetWithUiStyle;

		/// <summary>Collection of styles</summary>
		protected StyleInfoCollection m_StyleInfos;

		/// <summary>The style rules returned by "NormalFontStyle"</summary>
		private ITsTextProps m_ttpNormalFont;

		// REVIEW TomB: What module should define this?
		private const int kcbFmtBufMax = 1024;

		/// <summary>Occurs when the style sheet initializes</summary>
		public event EventHandler InitStylesheet;
		#endregion // Member variables

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Property for retrieving a (possibly cached) style sheet that contains a special
		/// style useful for retrieving font information for a UI writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private FwStyleSheet StyleSheetWithUiStyle
		{
			get
			{
				if (m_styleSheetWithUiStyle == null)
				{
					if (FindStyle(StyleServices.UiElementStylename) != null)
						m_styleSheetWithUiStyle = this;
					else
					{
						m_styleSheetWithUiStyle = new FwStyleSheet();
						m_styleSheetWithUiStyle.Init(Cache, Cache.LangProject.Hvo, LangProjectTags.kflidStyles);
					}
				}
				return m_styleSheetWithUiStyle;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The FDO cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get {return m_fdoCache;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method represents a compromise. We have a problem when we merge or rename styles,
		/// because currently the string actually contains the style name. So we have to 'crawl' the
		/// database, finding strings that contain the relevant style. But some style names may be
		/// used in more than one stylesheet. So we have to know what part of the database to search
		/// for modified styles.
		///
		/// So far, it has worked adequately to specify a single root object, and search everything
		/// it owns for uses of the modified style.
		///
		/// Except for Data Notebook, the object that is the root of this search is also the one
		/// (that is, either Scripture or the Lexical database) that owns the styles.
		///
		/// So, we can return the style owner for this method.
		///
		/// In time, we may need a setter and a separate member variable.
		/// Or, we may need a list of root objects.
		/// Or, we may finally get around to identifying styles within strings using their GUID,
		/// so we know exactly which style object is intended.
		/// For now, this is good enough.
		///
		/// Note: The main reason this works now and could continue to work going forward is
		/// that the C++ code that makes us of this information (i.e., the string walker) has
		/// some complex (kludgy) logic that has special handling to cover the special cases of
		/// each app. See the long comment near the start of the FwDbMergeStyles::Process()
		/// method in FwDbMergeStyles.cpp.
		/// </summary>
		/// <remarks>Now we're also using it for a legitimate purpose, too.</remarks>
		/// ------------------------------------------------------------------------------------
		public int RootObjectHvo
		{
			get { return m_hvoStylesOwner; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The field ID of the owner(m_hvoStylesOwner)'s collection of StStyles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int StyleListTag
		{
			get { return m_tagStylesList; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the styles.
		/// </summary>
		/// <value>The styles.</value>
		/// ------------------------------------------------------------------------------------
		public StyleInfoCollection Styles
		{
			get { return m_StyleInfos; }
		}
		#endregion

		#region Constructor, Init, Load
		///   --------------------------------------------------------------------------------------------
		///   <summary>
		///  	FwStyleSheet.Init() sets the FdoCache, the hvo of the owning object, and the tag
		///  	specifying the owner's property which holds the collection of StStyle objects.
		///  	Then the internal collections are loaded.
		///   </summary>
		/// <param name="cache">the FDO cache</param>
		/// <param name="hvoStylesOwner">the owning object</param>
		/// <param name="tagStylesList">the owner(hvoStylesOwner)'s field ID which holds the collection
		///     of StStyle objects</param>
		/// --------------------------------------------------------------------------------------------
		public void Init(FdoCache cache, int hvoStylesOwner, int tagStylesList)
		{
			m_fdoCache = cache;
			m_hvoStylesOwner = hvoStylesOwner;
			m_tagStylesList = tagStylesList;

			LoadStyles();

			if (InitStylesheet != null)
				InitStylesheet(this, EventArgs.Empty);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// LoadStyles is used by Init and FullRefresh to populate the array of styles and the
		/// hashmap of computed style rules.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected void LoadStyles()
		{
			m_StyleInfos = new StyleInfoCollection();

			// Use FdoCache to create an array of style objects, one for each style
			// in the collection owned by m_hvoStylesOwner.
			foreach (var hvo in ((ISilDataAccessManaged)m_fdoCache.DomainDataByFlid).VecProp(m_hvoStylesOwner, m_tagStylesList))
			{
				var style = m_fdoCache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(hvo);
				m_StyleInfos.Add(new BaseStyleInfo(style));
			}

			ComputeDerivedStyles();
		}
		#endregion

		#region Methods of IVwStylesheet
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the default paragraph style to use as the base for new styles
		/// (Usually "Normal")
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string GetDefaultBasedOnStyleName()
		{
			return StyleServices.NormalStyleName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style name that is the default style to use for the given context
		/// </summary>
		/// <param name="nContext">the context</param>
		/// <param name="fCharStyle">set to <c>true</c> for character styles; otherwise
		/// <c>false</c>.</param>
		/// <returns>
		/// Name of the style that is the default for the context
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public virtual string GetDefaultStyleForContext(int nContext, bool fCharStyle)
		{
			return GetDefaultBasedOnStyleName();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Store a style. In particular, store the details in the FDO cache and the derived
		/// TsTextProps in m_StyleInfos.
		/// </summary>
		///
		/// <param name="sName">The style name</param>
		/// <param name="sUsage">The usage information for the style</param>
		/// <param name="hvoStyle">The style to be stored</param>
		/// <param name="hvoBasedOn">What the style is based on</param>
		/// <param name="hvoNext">The next Style</param>
		/// <param name="nType">The Style type</param>
		/// <param name="fBuiltIn">True if predefined style</param>
		/// <param name="fModified">True if user has modified predefined style</param>
		/// <param name="ttp">TextProps, contains the formatting of the style</param>
		/// -----------------------------------------------------------------------------------
		public virtual void PutStyle(string sName, string sUsage, int hvoStyle, int hvoBasedOn,
			int hvoNext, int nType, bool fBuiltIn, bool fModified, ITsTextProps ttp)
		{
			IStStyle fdoStyle = null; // our local reference
			m_ttpNormalFont = null;  // may have been changed, recompute if needed.

			SetupUndoStylesheet(true);	// to perform ComputeDerivedStyles when undoing

			// Get the matching StStyle from m_StyleInfos, if it's there
			foreach (BaseStyleInfo styleInfo in m_StyleInfos)
			{
				if (styleInfo.RealStyle.Hvo == hvoStyle)
				{
					fdoStyle = styleInfo.RealStyle;
					break;
				}
			}
			// If the hvoStyle is not in m_StyleInfos, this is a new style;
			// Get it and insert it into m_StyleInfos
			if (fdoStyle == null)
				fdoStyle = m_fdoCache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(hvoStyle);

			// Save the style properties in the fdocache's style object
			fdoStyle.Name = sName;
			fdoStyle.Usage.UserDefaultWritingSystem = m_fdoCache.TsStrFactory.MakeString(sUsage, m_fdoCache.WritingSystemFactory.UserWs);
			if (hvoBasedOn > 0)
				fdoStyle.BasedOnRA = m_fdoCache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(hvoBasedOn);
			if (hvoNext > 0)
				fdoStyle.NextRA = m_fdoCache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(hvoNext);
			fdoStyle.Type = (StyleType)nType;
			fdoStyle.IsBuiltIn = fBuiltIn;
			fdoStyle.IsModified = fModified;
			fdoStyle.Rules = ttp;

			if (!fBuiltIn && hvoBasedOn != 0)
				CopyStyleContextValues(hvoStyle, hvoBasedOn);

			if (!m_StyleInfos.Contains(sName))
			{
				m_StyleInfos.Add(new BaseStyleInfo(fdoStyle));
				SetupUndoInsertedStyle(hvoStyle, sName);
			}

			// Recompute our hashmap, including effects on all derived styles.
			ComputeDerivedStyles();

			SetupUndoStylesheet(false);	// to perform ComputeDerivedStyles when redoing
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the properties for the style named sName. Must do it fast.
		/// </summary>
		///
		/// <param name="cch">Length of the style name - not used</param>
		/// <param name="sName">The style name</param>
		/// <returns>TextProps with retrieved properties, or null if not found</returns>
		/// ------------------------------------------------------------------------------------
		public ITsTextProps GetStyleRgch(int cch, string sName)
		{
			if (m_StyleInfos == null || !m_StyleInfos.Contains(sName))
				return null;
			return m_StyleInfos[sName].TextProps;
			//ThrowInternalError(E_INVALIDARG);?
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the next style that will be used if the user types a CR at the end of this
		/// paragraph. If the input is null, return "Normal".
		/// </summary>
		///
		/// <param name="sName">Name of the style for this paragraph.</param>
		/// <returns>Name of the style for the next paragraph.</returns>
		/// -----------------------------------------------------------------------------------
		public string GetNextStyle(string sName)
		{
			var style = FindStyle(sName);
			if (style != null && style.NextRA != null)
				return style.NextRA.Name;
			return StyleServices.NormalStyleName; //ThrowInternalError(E_INVALIDARG);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the basedOn style name for the style.
		/// </summary>
		///
		/// <param name="sName">Name of the style</param>
		/// <returns>Name of the BasedOn style if available, otherwise empty string</returns>
		/// -----------------------------------------------------------------------------------
		public string GetBasedOn(string sName)
		{
			var style = FindStyle(sName);
			if (style != null && style.BasedOnRA != null)
				return style.BasedOnRA.Name;
			return string.Empty; //ThrowInternalError(E_INVALIDARG);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the type for the style
		/// </summary>
		///
		/// <param name="sName">Style name</param>
		/// <returns>Returns type of the style (0 by default, which is kstParagraph)</returns>
		/// -----------------------------------------------------------------------------------
		public int GetType(string sName)
		{
			var style = FindStyle(sName);
			if (style != null)
				return (int)style.Type;

			if (sName == kstrDefaultCharStyle || sName == StyleUtils.DefaultParaCharsStyleName)
				return (int)StyleType.kstCharacter;

			return 0; //ThrowInternalError(E_INVALIDARG);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the context for the style
		/// </summary>
		/// <param name="sName">Style name</param>
		/// <returns>Returns context of the style (General by default)</returns>
		/// -----------------------------------------------------------------------------------
		public int GetContext(string sName)
		{
			var style = FindStyle(sName);
			if (style != null)
				return (int)style.Context;

			return (int)ContextValues.General; //ThrowInternalError(E_INVALIDARG);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the function for the style
		/// </summary>
		/// <param name="sName">Style name</param>
		/// <returns>Returns function of the style (Prose by default)</returns>
		/// -----------------------------------------------------------------------------------
		public FunctionValues GetFunction(string sName)
		{
			var style = FindStyle(sName);
			if (style != null)
				return style.Function;

			return FunctionValues.Prose; //ThrowInternalError(E_INVALIDARG);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Is the style named sName a predefined style?
		/// </summary>
		///
		/// <param name="sName"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public bool IsBuiltIn(string sName)
		{
			var style = FindStyle(sName);
			if (style != null)
				return style.IsBuiltIn;
			return false; //ThrowInternalError(E_INVALIDARG);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Was the (predefined) style named sName changed by the user?
		/// </summary>
		///
		/// <param name="sName"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public bool IsModified(string sName)
		{
			var style = FindStyle(sName);
			if (style != null)
				return style.IsModified;
			return false;
		}

		//TODO EberhardB: add method for Usage property

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return the associated Data Access object
		/// </summary>
		///
		/// <returns>Return the associated Data Access object</returns>
		/// -----------------------------------------------------------------------------------
		public ISilDataAccess DataAccess
		{
			get { return m_fdoCache.DomainDataByFlid; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new style and return its HVO.
		/// </summary>
		///
		/// <returns>The hvo of the new object, or 0 if not created.</returns>
		/// -----------------------------------------------------------------------------------
		public int MakeNewStyle()
		{
			m_ttpNormalFont = null;  // may have been changed, recompute if needed.

			return GetNewStyleHvo(); //create the style
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return an HVO for a newly created style, using the FDO cache.
		/// Create a new StStyle object in the FDO cache, and return its new hvo.
		/// Insert the new style into its owner property (e.g. the StylesList property
		///  of LangProject or Scripture).
		/// This is a default implementation using the FDO cache. Specialized style sheets
		/// may derive their own version.
		/// </summary>
		///
		/// <returns>The hvo of the new object, or 0 if not created.</returns>
		/// -----------------------------------------------------------------------------------
		protected virtual int GetNewStyleHvo()
		{
			return m_fdoCache.DomainDataByFlid.MakeNewObject(StStyleTags.kClassId, m_hvoStylesOwner, m_tagStylesList, -1);
			// This code sets up the style but does not add it to its parent object's styles list!
			//var newStyle = m_fdoCache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
			//(ICmObjectInternal)newStyle).InitializeNewCmObject(m_fdoCache,
			//    m_fdoCache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_hvoStylesOwner),
			//    (int)m_tagStylesList,
			//    0);
			//return newStyle.Hvo;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Delete a style from a stylesheet. It is assumed the caller has already changed any
		/// references to this style.
		/// </summary>
		/// <param name="hvoStyle">ID of style to delete.</param>
		/// -----------------------------------------------------------------------------------
		public virtual void Delete(int hvoStyle)
		{
			Debug.Assert(m_fdoCache != null);
			m_ttpNormalFont = null;  // may have been changed, recompute if needed.

			foreach (BaseStyleInfo styleInfo in m_StyleInfos)
			{
				var style = styleInfo.RealStyle;
				if (style.Hvo == hvoStyle)
				{
					if (style.IsBuiltIn)
					{
						throw new ArgumentException(string.Format(
							"Can not delete built-in style: {0} ({1})", styleInfo.Name, hvoStyle));
					}
					else
					{
						m_StyleInfos.Remove(styleInfo);

						SetupUndoStylesheet(true); // to perform ComputeDerivedStyles when undoing
						m_fdoCache.DomainDataByFlid.DeleteObj(hvoStyle);
						SetupUndoDeletedStyle(hvoStyle);
						SetupUndoStylesheet(false);	// to perform ComputeDerivedStyles when redoing
						return;
					}
				}
			}

			throw new ArgumentException(string.Format(
				"Could not find style to be deleted: {0}", hvoStyle));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get number of styles in sheet.
		/// </summary>
		/// <returns>Number of styles in sheet.</returns>
		/// -----------------------------------------------------------------------------------
		public int CStyles
		{
			get { return m_StyleInfos.Count; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the HVO of the Nth style (in an arbitrary order).
		/// </summary>
		///
		/// <param name="iStyle">Index of the style</param>
		/// <returns>HVO</returns>
		/// -----------------------------------------------------------------------------------
		public int get_NthStyle(int iStyle)
		{
			Debug.Assert(0 <= iStyle && iStyle < m_StyleInfos.Count);
			return m_StyleInfos[iStyle].RealStyle.Hvo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the StStyle object of the Nth style (in an arbitrary order).
		/// </summary>
		/// <param name="iStyle">Index of the style</param>
		/// <returns>The StStyle object</returns>
		/// ------------------------------------------------------------------------------------
		public IStStyle get_NthStyleObject(int iStyle)
		{
			Debug.Assert(0 <= iStyle && iStyle < m_StyleInfos.Count);
			return m_StyleInfos[iStyle].RealStyle;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the name of the Nth style (in an arbitrary order).
		/// </summary>
		///
		/// <param name="iStyle">Index of the style</param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public string get_NthStyleName(int iStyle)
		{
			Debug.Assert(0 <= iStyle && iStyle < m_StyleInfos.Count);
			return m_StyleInfos[iStyle].Name;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// A special style that contains only the parts of "Normal" that relate to the Font
		/// tab. This is automatically maintained as "Normal" is edited. If there is no "Normal"
		/// style, return NULL. This is not currently considered an error.
		/// </summary>
		///
		/// <returns>Normal font style props</returns>
		/// -----------------------------------------------------------------------------------
		public ITsTextProps NormalFontStyle
		{
			get
			{
				if (m_ttpNormalFont == null)
				{
					ITsTextProps ttpNormal = GetStyleRgch(0, StyleServices.NormalStyleName);
					if (ttpNormal == null) // if no Normal style
						return null; //not an error
					ITsPropsBldr tpb = TsPropsBldrClass.Create();
					CopyStringProp(ttpNormal, tpb, (int)FwTextStringProp.kstpWsStyle);
					CopyStringProp(ttpNormal, tpb, (int)FwTextPropType.ktptFontFamily);
					CopyStringProp(ttpNormal, tpb, (int)FwTextPropType.ktptFontVariations);
					CopyIntProp(ttpNormal, tpb, (int)FwTextPropType.ktptUnderColor);
					CopyIntProp(ttpNormal, tpb, (int)FwTextPropType.ktptUnderline);
					CopyIntProp(ttpNormal, tpb, (int)FwTextPropType.ktptFontSize);
					CopyIntProp(ttpNormal, tpb, (int)FwTextPropType.ktptForeColor);
					CopyIntProp(ttpNormal, tpb, (int)FwTextPropType.ktptItalic);
					CopyIntProp(ttpNormal, tpb, (int)FwTextPropType.ktptBackColor);
					CopyIntProp(ttpNormal, tpb, (int)FwTextPropType.ktptBold);
					CopyIntProp(ttpNormal, tpb, (int)FwTextPropType.ktptOffset);
					CopyIntProp(ttpNormal, tpb, (int)FwTextPropType.ktptSuperscript);

					if (tpb.IntPropCount != 0 || tpb.StrPropCount != 0)
						m_ttpNormalFont = tpb.GetTextProps();
				}
				return m_ttpNormalFont;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the font size of the Normal style in millipoints. If the Normal style does not
		/// exist, or if it has no explicit size set, this returns the default size (10 pts.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int NormalFontSize
		{
			get
			{
				if (NormalFontStyle != null)
				{
					int var;
					int fontSize = NormalFontStyle.GetIntPropValues(
						(int)FwTextPropType.ktptFontSize, out var);
					if (fontSize != -1)
						return fontSize;
				}
				return FontInfo.kDefaultFontSize * 1000;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the line spacing of the Normal style, either in millipoints or as a relative
		/// value which is a proportion of the normal line height. Negative millipoint values
		/// indicates an exact line height. If the Normal style does not exist, or if it has no
		/// explicit line spacing set, this returns the default, which may vary by application.
		/// </summary>
		/// <param name="var">The variation, either millipoints (default) or relative.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int GetNormalLineSpacing(out FwTextPropVar var)
		{
			var = FwTextPropVar.ktpvDefault;

			ITsTextProps ttpNormal = GetStyleRgch(0, StyleServices.NormalStyleName);
			if (ttpNormal != null)
			{
				int iVar;
				int lineSpacing = ttpNormal.GetIntPropValues(
					(int)FwTextPropType.ktptLineHeight, out iVar);
				if (lineSpacing != -1)
				{
					var = (FwTextPropVar)iVar;
					return lineSpacing;
				}
			}
			return DefaultLineSpacing;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default line spacing in millipoints.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual int DefaultLineSpacing
		{
			get { return 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the integer property <paramref name="propId"/>from <paramref name="ttp"/>
		/// into the property builder <paramref name="tpb"/>.
		/// </summary>
		/// <param name="ttp">The text props.</param>
		/// <param name="tpb">The property builder.</param>
		/// <param name="propId">The id of the desired integer property.</param>
		/// ------------------------------------------------------------------------------------
		private void CopyIntProp(ITsTextProps ttp, ITsPropsBldr tpb, int propId)
		{
			int val, var;
			val = ttp.GetIntPropValues(propId, out var);
			if (var != -1)
				tpb.SetIntPropValues(propId, var, val);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the string property <paramref name="propId"/>from <paramref name="ttp"/>
		/// into the property builder <paramref name="tpb"/>.
		/// </summary>
		/// <param name="ttp">The text props.</param>
		/// <param name="tpb">The property builder.</param>
		/// <param name="propId">The id of the desired string property.</param>
		/// ------------------------------------------------------------------------------------
		private void CopyStringProp(ITsTextProps ttp, ITsPropsBldr tpb, int propId)
		{
			string val;
			val = ttp.GetStrPropValue(propId);
			if (val != null)
				tpb.SetStrPropValue(propId, val);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return true if the given style is one that is protected within the style sheet.
		/// </summary>
		///
		/// <param name="sName">Name of style</param>
		/// <returns>True if style is protected</returns>
		/// <remarks>This is a default implementation that returns the built-in flag.
		/// Specialized style sheets may derive their own version.
		/// </remarks>
		/// -----------------------------------------------------------------------------------
		public bool get_IsStyleProtected(string sName)
		{
			return IsBuiltIn(sName);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Save in the cache a (subtly modified) new set of text props
		/// without recording an undo action.
		/// </summary>
		///
		/// <param name="cch"></param>
		/// <param name="sName"></param>
		/// <param name="hvoStyle"></param>
		/// <param name="ttp"></param>
		/// -----------------------------------------------------------------------------------
		public void CacheProps(int cch, string sName, int hvoStyle, ITsTextProps ttp)
		{
			// REVIEW TomB:
			// AfStylesDlg::FullyInitializeNormalStyle is the only place that calls CacheProps.
			// AfStylesheet::CacheProps appears to put the new props in the hashmap,
			// then it re-computes the hashmap. What's up with that?
			// Also, AfStylesheet::PutStyle uses m_qsda->SetUnknown, while
			// AfStylesheet::CacheProps uses qvcd->CacheUnknown.
			// should something be different here?

			//			StStyle fdoStyle =
			//			fdoStyle.Rules = ttp;

			// Recompute our hashmap, including effects on all derived styles.
			//ComputeDerivedStyles();
		}

		#endregion

		#region Other Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a font face name for a style. This is the real font face name, not the magic
		/// font name.
		/// </summary>
		/// <param name="name">Style name whose font face name to return.</param>
		/// <param name="iws">Writing system</param>
		/// <param name="cache">Cache from which the writing system factory is obtained.
		/// </param>
		/// <returns>The font face name of the specified style name and writing system.</returns>
		/// ------------------------------------------------------------------------------------
		public string GetFaceNameFromStyle(string name, int iws, FdoCache cache)
		{
			return GetFaceNameFromStyle(name, iws, cache.WritingSystemFactory);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a font face name for a style. This is the real font face name, not the magic
		/// font name.
		/// </summary>
		/// <param name="stylename">Style name whose font face name to return.</param>
		/// <param name="iws">Writing system</param>
		/// <param name="wsf">Writing System Factory (probably from the cache).</param>
		/// <returns>The font face name of the specified style name and writing system.</returns>
		/// ------------------------------------------------------------------------------------
		public string GetFaceNameFromStyle(string stylename, int iws, ILgWritingSystemFactory wsf)
		{
			LgCharRenderProps chrps = GetStyleChrps(stylename, iws, wsf);
			return GetFaceNameFromChrps(chrps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper method tpo get the face name from the properties used to render characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string GetFaceNameFromChrps(LgCharRenderProps chrps)
		{
			return MarshalEx.UShortToString(chrps.szFaceName).Trim();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the information about the properties that will be used to render characters in
		/// the given writing system for the given style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private LgCharRenderProps GetStyleChrps(string stylename, int ws, ILgWritingSystemFactory wsf)
		{
			ITsPropsBldr ttpBldr = TsPropsBldrClass.Create();
			if (stylename != null)
				ttpBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, stylename);

			// Make sure we handle the magic writing systems
			if (ws == WritingSystemServices.kwsAnal)
				ws = m_fdoCache.DefaultAnalWs;
			else if (ws == WritingSystemServices.kwsVern)
				ws = m_fdoCache.DefaultVernWs;

			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, ws);
			ITsTextProps ttp = ttpBldr.GetTextProps();

			// JohnT: I'm not completely sure why this is necessary, since the property store is created
			// and used entirely on the current thread. However, the stylesheet (this) is typically
			// created on the main UI thread, and apparently the linkage from the property store to 'this'
			// when we set its StyleSheet to be 'this' fails. It fails in a very bizarre way, telling us
			// that the property store does not implement IVwPropertyStore. See FWR-1918.
			// Some more sophisticated trick may be needed if it is ever the case that the stylesheet
			// is NOT created on the main UI thread.
			return m_fdoCache.ServiceLocator.GetInstance<IFdoUI>().SynchronizeInvoke.Invoke(() => GetChrps(ws, ttp, wsf));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the font face name of the special UI style's font using the specified cache and
		/// writing system.
		/// </summary>
		/// <param name="ws">The writing system identifier.</param>
		/// <param name="overrideDefaultSize">If 0, the default size from the style sheet will
		/// be used. If greater than 0, this represents the em-size, in points, of the font to
		/// return.</param>
		/// ------------------------------------------------------------------------------------
		public Font GetUiFontForWritingSystem(int ws, float overrideDefaultSize)
		{
			LgCharRenderProps chrps = StyleSheetWithUiStyle.GetStyleChrps(StyleServices.UiElementStylename, ws,
				m_fdoCache.WritingSystemFactory);
			string faceName = GetFaceNameFromChrps(chrps);
			// ENHANCE: Handle irregular font styles
			return new Font(faceName,
				overrideDefaultSize > 0 ? overrideDefaultSize : (chrps.dympHeight / 1000f));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the character rendering properties for the given writing system and text props.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private LgCharRenderProps GetChrps(int iws, ITsTextProps ttp, ILgWritingSystemFactory wsf)
		{
			IVwPropertyStore vwps = VwPropertyStoreClass.Create();
			vwps.Stylesheet = this;
			LgCharRenderProps chrps = vwps.get_ChrpFor(ttp);
			ILgWritingSystem ws = wsf.get_EngineOrNull(iws);
			ws.InterpretChrp(ref chrps);
			return chrps;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find the style with specified name
		/// </summary>
		/// <param name="name">Stylename to find</param>
		/// <returns>StStyle if found, otherwise null.</returns>
		/// <remarks>Virtual to allow dynamic mocks to override it</remarks>
		/// -----------------------------------------------------------------------------------
		public virtual IStStyle FindStyle(string name)
		{
			if (name == null)
				return null;	// can't find.
			string sNormalizedName = name.Normalize();
			// ENHANCE (TimS): We might consider only doing the lookup once.
			if (m_StyleInfos.Contains(sNormalizedName))
				return m_StyleInfos[sNormalizedName].RealStyle;

			return null; //not found
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified style exists in the underlying DB. Unlike
		/// FindStyle, this does not just check the in-memory list. Its purpose is to allow the
		/// caller to catch and prevent the creation of a duplicate style. It should never be
		/// called for a style already known to exist in the stylesheet.
		/// </summary>
		/// <param name="styleInfoTable">The style info table, containing 0 or more new styles</param>
		/// ------------------------------------------------------------------------------------
		public void CheckForDuplicates(StyleInfoTable styleInfoTable)
		{
			bool fStylesheetReloaded = false;

			foreach (BaseStyleInfo styleInfo in styleInfoTable.Values)
			{
				if (styleInfo.RealStyle == null)
				{
					if (!fStylesheetReloaded)
					{
						LoadStyles();
						fStylesheetReloaded = true;
					}
					var style = FindStyle(styleInfo.Name);
					if (style != null)
					{
						// Context, etc. for a new style are always derived from the base
						// style. Since these values have not yet been set for this style,
						// we use the base
						BaseStyleInfo basedOn = styleInfo.RealBasedOnStyleInfo;
						// We found a duplicate - must have been created on another machine.
						// See if it is compatible with the one this user is trying to add.
						if (basedOn.IsParagraphStyle != (style.Type == StyleType.kstParagraph) ||
							basedOn.Context != style.Context ||
							basedOn.Structure != style.Structure ||
							basedOn.Function != style.Function)
						{
							styleInfo.IsValid = false;
							throw new IncompatibleStyleExistsException(string.Format(
								Strings.ksIncompatibleStyleExists,
								style.Name));
						}
						else
						{
							styleInfo.RealStyle = style;
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies context information (Context, Structure and Function) from based on style
		/// and cascades change to other styles that are based on the style being changed.
		/// </summary>
		/// <param name="hvoStyle">style being changed</param>
		/// <param name="hvoBasedOn"></param>
		/// ------------------------------------------------------------------------------------
		private void CopyStyleContextValues(int hvoStyle, int hvoBasedOn)
		{
			CopyStyleContextValues(m_fdoCache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(hvoStyle),
				m_fdoCache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(hvoBasedOn));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies context information (Context, Structure and Function) from based on style
		/// and cascades change to other styles that are based on the style being changed.
		/// </summary>
		/// <param name="changedStyle">style being changed</param>
		/// <param name="basedOnStyle"></param>
		/// ------------------------------------------------------------------------------------
		private void CopyStyleContextValues(IStStyle changedStyle, IStStyle basedOnStyle)
		{
			ContextValues newContext = ContextValues.General;
			StructureValues newStructure = StructureValues.Undefined;
			FunctionValues newFunction = FunctionValues.Prose;
			if (basedOnStyle != null)
			{
				newContext = basedOnStyle.Context;
				newStructure = basedOnStyle.Structure;
				newFunction = basedOnStyle.Function;
			}
			if (newContext != changedStyle.Context ||
				newStructure != changedStyle.Structure ||
				newFunction != changedStyle.Function)
			{
				changedStyle.Context = newContext;
				changedStyle.Structure = newStructure;
				changedStyle.Function = newFunction;
				foreach (BaseStyleInfo styleInfo in m_StyleInfos)
				{
					if (styleInfo.RealStyle.BasedOnRA == changedStyle)
						CopyStyleContextValues(styleInfo.RealStyle, changedStyle);
				}
			}
		}
		#endregion

		#region Compute Derived Styles
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Recalculate the text properties with the recomputed derived style properties.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected void ComputeDerivedStyles()
		{
			// Clear the text props for all styles so that we can recalculate them
			foreach (BaseStyleInfo styleInfo in m_StyleInfos)
			{
				var styleBasedOn = styleInfo.RealStyle.BasedOnRA;
				if ((styleBasedOn == null) || styleBasedOn == styleInfo.RealStyle)
				{
					// If not based on anything, or based on itself; use as is with any applicable overrides.
					var bldr = (styleInfo.RealStyle.Rules == null) ? TsPropsBldrClass.Create() :
						styleInfo.RealStyle.Rules.GetBldr();
					ApplyProgrammaticPropOverrides(styleInfo.Name, bldr);
					styleInfo.TextProps = bldr.GetTextProps();
				}
				else
					styleInfo.TextProps = null;
			}

			foreach (BaseStyleInfo styleInfo in m_StyleInfos)
			{
				// If our hash table doesn't have this style yet...
				// (it might already be there because we may have already computed it if
				//  a style already computed was based on it)
				if (styleInfo.TextProps == null)
				{
					// Compute and save the fully derived props for this style
					ComputeDerivedStyle(m_StyleInfos.Count, styleInfo);
				}
			}
			m_ttpNormalFont = null; // may have been changed, recompute if needed.
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Compute the actual meaning (total derived properties) of a style, given its
		/// definition (ttp) and the definitions of the other styles.
		/// As a side effect, this computes the actual meaning of any style on which this is
		/// based that is not already computed.
		/// </summary>
		/// <param name="cRecurLevel">recursion level permitted - initially use count of
		/// styles</param>
		/// <param name="styleInfo"> the StStyle we are to compute</param>
		/// -----------------------------------------------------------------------------------
		private void ComputeDerivedStyle(int cRecurLevel, BaseStyleInfo styleInfo)
		{
			// Initialize text props with style rules - will probably get changed later
			// in this method, but gives us a good start.
			styleInfo.TextProps = styleInfo.RealStyle.Rules;
			int hvoBasedOn = styleInfo.RealStyle.BasedOnRA.Hvo;

			// Find the style it is based on.
			BaseStyleInfo styleBasedOnInfo = null;
			foreach(BaseStyleInfo styleTemp in m_StyleInfos)
			{
				if (hvoBasedOn == styleTemp.RealStyle.Hvo)
				{
					styleBasedOnInfo = styleTemp;
					break;
				}
			}

			// If missing base style, treat as based on nothing.
			if (styleBasedOnInfo == null)
				return;

			// Check for infinite loop.
			if (cRecurLevel <= 0)
			{
				//REVIEW: issue a warning?
				// was ThrowInternalError(E_INVALIDARG,"Loop in style based-on");
				return;
			}

			// Get the total effect of the base style.
			if (styleBasedOnInfo.TextProps == null)
			{
				// We haven't already computed it. Do so now.
				//recurse to get the total effect
				ComputeDerivedStyle(cRecurLevel - 1, styleBasedOnInfo);
			}

			// OK, we got the base style. Now compute the effect of our own defn
			styleInfo.TextProps = ComputeInheritance(styleBasedOnInfo.TextProps,
				styleInfo);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Compute the net effect of an override on a base style.
		/// ENHANCE JohnT (version 2 or later): When we make use of property settings that are
		/// non-absolute, we will need more subtle algorithms here.
		/// </summary>
		/// <param name="ttpBase">Based-On TextProperties of the style</param>
		/// <param name="styleInfo">Style info for which properties are to be computed</param>
		/// <returns>The net TextProperties including all inheritance, any overridden
		/// properties based on the style info, and any in-memory-only overriden properties
		/// (as needed for a particular print layout or whatever)</returns>
		/// -----------------------------------------------------------------------------------
		private ITsTextProps ComputeInheritance(ITsTextProps ttpBase, BaseStyleInfo styleInfo)
		{
			Debug.Assert(ttpBase != null);
			ITsTextProps ttpOverride = styleInfo.TextProps;
			if (ttpOverride == null)
				return ttpBase;

			// make a props builder set up with the base props
			ITsPropsBldr tpb = ttpBase.GetBldr();

			ApplyPropsToBuilder(ttpOverride, tpb, ttpBase);
			ApplyProgrammaticPropOverrides(styleInfo.Name, tpb);

			return tpb.GetTextProps();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applies the overridden int and string props to the given props builder.
		/// </summary>
		/// <param name="ttpOverride">The props representing the overriden int props.</param>
		/// <param name="tpb">The props builder.</param>
		/// <param name="ttpBase">if not set to <c>null</c> check for and do special processing
		/// for overrides specific to particular writing-systems.</param>
		/// ------------------------------------------------------------------------------------
		protected void ApplyPropsToBuilder(ITsTextProps ttpOverride, ITsPropsBldr tpb,
			ITsTextProps ttpBase)
		{
			// merge the integer properties
			int cip = ttpOverride.IntPropCount;
			// The call to MergeIntProp was commented out to fix inheritance logic
			// per TE-1060 (TE-1810)
			for (int iprop = 0; iprop < cip; iprop++)
			{
				int tptPropType, nVar, nVal;
				nVal = ttpOverride.GetIntProp(iprop, out tptPropType, out nVar);
				//int nVarBase, nValBase;
				//nValBase = ttpBase.GetIntPropValues(tptPropType, out nVarBase);
				//MergeIntProp(tptPropType, nVarBase, nValBase, ref nVar, ref nVal); // Special case merging
				tpb.SetIntPropValues(tptPropType, nVar, nVal);
			}

			// merge the string properties
			int csp = ttpOverride.StrPropCount;
			for (int iprop = 0; iprop < csp; iprop++)
			{
				string sOverride;
				int tptPropType;
				sOverride = ttpOverride.GetStrProp(iprop, out tptPropType);
				if (ttpBase != null)
				{
					// Any special cases?
					switch (tptPropType)
					{
						default:
							// Most properties require no special handling.
							break;
						case (int)FwTextStringProp.kstpWsStyle:
							string sBase = ttpBase.GetStrPropValue(
								(int)FwTextPropType.ktptWsStyle);
							ComputeWsStyleInheritance(sBase, ref sOverride);
							break;
					}
				}
				tpb.SetStrPropValue(tptPropType, sOverride);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows derived classes to override any needed properties for the given style.
		/// </summary>
		/// <param name="styleName">Name of the style for which overrides are being computed.
		/// </param>
		/// <param name="tpb">The text props builder containing all the inherited and explict
		/// properties for this style.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void ApplyProgrammaticPropOverrides(string styleName, ITsPropsBldr tpb)
		{
			// Default implentation is a no-op
		}

//		/// -----------------------------------------------------------------------------------
//		/// <summary>
//		/// MergeIntProp()
//		/// MergIntProp was commented out to fix inheritance logic per TE-1060 (TE-1810)
//		///
//		/// Compute the effect of merging a base and overridden integer property.
//		/// nVar and nVal indicate the override and are changed to reflect any modified value.
//		/// ENHANCE JohnT (version 2 or later): when we make use of property settings that are
//		/// non-absolute, we will need more subtle algorithms here.
//		/// </summary>
//		/// <param name="tptPropType">Text property type</param>
//		/// <param name="nVarBase">Base Variation to be merged</param>
//		/// <param name="nValBase"></param>
//		/// <param name="nVar">Overridden Variation to be merged and returned</param>
//		/// <param name="nVal">Overridden Value to be merged and returned</param>
//		/// -----------------------------------------------------------------------------------
//		private void MergeIntProp(int tptPropType, int nVarBase, int nValBase,
//			ref int nVar, ref int nVal)
//		{
//			switch(tptPropType)
//			{
//				default:
//					// Most properties require no special handling.
//					// (Until we start making use of relative values...)
//					break;
//				case (int)FwTextPropType.ktptItalic:
//				case (int)FwTextPropType.ktptBold:
//					Debug.Assert(nVar == (int)FwTextPropVar.ktpvEnum);
//					// These cases use kttvInvert as a possible value. If this is present
//					// it needs to be properly combined with the base setting.
//					if (nVal == (int)FwTextToggleVal.kttvInvert)
//					{
//						switch(nValBase)
//						{
//							default:
//								// Was not specified: just use the invert setting.
//								break;
//							case (int)FwTextToggleVal.kttvOff:
//								// Was explicitly off: turn on.
//								nVal = (int)FwTextToggleVal.kttvForceOn;
//								break;
//							case (int)FwTextToggleVal.kttvForceOn:
//								// Was on: force off.
//								nVal = (int)FwTextToggleVal.kttvOff;
//								break;
//							case (int)FwTextToggleVal.kttvInvert:
//								// Inverting "invert" cancels it, making a ttp that does not change
//								// this property at all.
//								//REVIEW TomB: this sets a non-enum value
//								nVal = nVar = -1;
//								break;
//						}
//					}
//					break;
//			}
//		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Compute the net effect of an override on a base for the kspWsStyle string property.
		///
		/// NOTE: This routine must be kept in sync with FmtFntDlg::DecodeFontPropsString.
		///
		/// </summary>
		/// <param name="sBase">the WsStyle string prop of the base style</param>
		/// <param name="sOverride">on input,the WsStyle string prop of the override style;
		/// on output, the resultant combination WsStyle string prop</param>
		/// -----------------------------------------------------------------------------------
		private void ComputeWsStyleInheritance(string sBase, ref string sOverride)
		{
			// for now, we do nothing

			//TODO TomB (BryanW): Since this ComputeWsStyleInheritance function is deeply
			// involved in the binary format of the WsStyle string properties
			// (see AfStyleFntDlg::SetDlgValues)
			// it doesn't seem wise to write yet another peripheral function here
			// whose logic must be kept in synch.
			// It is hoped that the Dallas team will consolidated the WsStyle logic
			// into a wrapped object.
		}
		#endregion

		#region Undo stuff
		/*----------------------------------------------------------------------------------------------
			Create an undo action reflecting the need for a ComputeDerivedStyles if anything is
			undone. We surround a block of changes with two of these undo-actions: the first is
			run in undo mode, the last in redo mode. This (hopefully) keeps us from calling
			ComputeDerivedStyles unnecessarily often but makes sure it gets done after all the other
			changes have happened.
		----------------------------------------------------------------------------------------------*/
		private void SetupUndoStylesheet(bool fFirst)
		{
			//			IActionHandlerPtr qacth;
			//			CheckHr(m_qsda->GetActionHandler(&qacth));
			//			if (!qacth)
			//				return;
			//
			//			VwUndoDaPtr quda = dynamic_cast<VwUndoDa *>(m_qsda.Ptr());
			//			if (!quda)
			//				return;
			//
			//			VwUndoStylesheetActionPtr quact;
			//			quact.Attach(NewObj VwUndoStylesheetAction(quda, this, fFirst));
			//
			//			CheckHr(qacth->AddAction(quact));
		}

		/*----------------------------------------------------------------------------------------------
			Create an undo action reflecting the creation of a new style.
		----------------------------------------------------------------------------------------------*/
		private void SetupUndoInsertedStyle(int hvoStyle, string sName)
		{
			//			IActionHandlerPtr qacth;
			//			CheckHr(m_qsda->GetActionHandler(&qacth));
			//			if (!qacth)
			//				return;
			//
			//			VwUndoDaPtr quda = dynamic_cast<VwUndoDa *>(m_qsda.Ptr());
			//			if (!quda)
			//				return;
			//
			//			VwUndoStyleActionPtr quact;
			//			quact.Attach(NewObj VwUndoStyleAction(quda, this, hvoStyle, stuName, false));
			//
			//			CheckHr(qacth->AddAction(quact));
		}

		/*----------------------------------------------------------------------------------------------
			Create an undo action reflection the deletion of a style.
		----------------------------------------------------------------------------------------------*/
		private void SetupUndoDeletedStyle(int hvoStyle)
		{
			//			IActionHandlerPtr qacth;
			//			CheckHr(m_qsda->GetActionHandler(&qacth));
			//			if (!qacth)
			//				return;
			//
			//			VwUndoDaPtr quda = dynamic_cast<VwUndoDa *>(m_qsda.Ptr());
			//			if (!quda)
			//				return;
			//
			//			SmartBstr sbstrName;
			//			CheckHr(m_qsda->get_UnicodeProp(hvoStyle, kflidStStyle_Name, &sbstrName));
			//			StrUni stuName(sbstrName.Chars(), sbstrName.Length());
			//			VwUndoStyleActionPtr quact;
			//			quact.Attach(NewObj VwUndoStyleAction(quda, this, hvoStyle, stuName, true));
			//
			//			CheckHr(qacth->AddAction(quact));
		}

		/*----------------------------------------------------------------------------------------------
			Undo the insertion of a style.
		----------------------------------------------------------------------------------------------*/
		private void UndoInsertedStyle(int hvoStyle)
		{
			//			for (int istyle = 0; istyle < m_vhcStyles.Size(); istyle++)
			//			{
			//				if (hvoStyle == m_vhcStyles[istyle].hvo)
			//				{
			//					m_vhcStyles.Delete(istyle);
			//
			//		//			CheckHr(m_qsda->get_UnicodeProp(hvoStyle, kflidStStyle_Name, &sbstrName));
			//		//			StrUni stuName(sbstrName.Chars(), sbstrName.Length());
			//					m_hmstuttpStyles.Delete(stuName);
			//
			//					// Don't do this until things are thoroughly set up; the UndoStylesheetActions
			//					// should take care of it.
			//		//			ComputeDerivedStyles();
			//					return S_OK;
			//				}
			//			}
			//			Assert(false);
			//			return E_UNEXPECTED;
		}

		/*----------------------------------------------------------------------------------------------
			Undo the deletion of a style.
		----------------------------------------------------------------------------------------------*/
		private void UndoDeletedStyle(int hvoStyle)
		{
			//			HvoClsid hc;
			//			hc.clsid = kclidStStyle;
			//			hc.hvo = hvoStyle;
			//			m_vhcStyles.Replace(m_vhcStyles.Size(), m_vhcStyles.Size(), &hc, 1);
			//
			//			// Don't do this until things are thoroughly set up; the UndoStylesheetActions should
			//			// take care of it.
			//		//	ComputeDerivedStyles();
			//
			//			return S_OK;
		}
		#endregion

		/// <summary>
		/// IStylesheet implementation of looking up a particular style.
		/// </summary>
		public IStyle Style(string name)
		{
			if (!m_StyleInfos.Contains(name))
				return null;
			return m_StyleInfos[name];
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A stylesheet that allows for properties of styles to be overridden in memory for the
	/// purpose of formatting special views (e.g. print layouts).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class InMemoryStyleSheet : FwStyleSheet
	{
		private Dictionary<string, ITsPropsBldr> m_propOverrides = new Dictionary<string, ITsPropsBldr>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overrides any needed properties for the given style.
		/// </summary>
		/// <param name="styleName">Name of the style for which overrides are being computed.
		/// </param>
		/// <param name="tpb">The text props builder containing all the inherited and explict
		/// properties for this style.</param>
		/// ------------------------------------------------------------------------------------
		protected override void ApplyProgrammaticPropOverrides(string styleName, ITsPropsBldr tpb)
		{
			ITsPropsBldr overridenProps;
			if (!m_propOverrides.TryGetValue(styleName, out overridenProps))
				return;
			ApplyPropsToBuilder(overridenProps.GetTextProps(), tpb, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the int prop override.
		/// </summary>
		/// <param name="styleName">Name of the style.</param>
		/// <param name="prop">The type of property being overridden.</param>
		/// <param name="var">The variant.</param>
		/// <param name="val">The value of the property.</param>
		/// ------------------------------------------------------------------------------------
		public void AddIntPropOverride(string styleName, FwTextPropType prop, FwTextPropVar var,
			int val)
		{
			ITsPropsBldr overridenProps = GetOrCreatePropOverride(styleName);
			overridenProps.SetIntPropValues((int)prop, (int)var, val);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the string prop override.
		/// </summary>
		/// <param name="styleName">Name of the style.</param>
		/// <param name="prop">The type of property being overridden.</param>
		/// <param name="val">The value of the property.</param>
		/// ------------------------------------------------------------------------------------
		public void AddStrPropOverride(string styleName, FwTextPropType prop, string val)
		{
			var overridenProps = GetOrCreatePropOverride(styleName);
			overridenProps.SetStrPropValue((int)prop, val);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or creates the props builder object with the property overrides for the given
		/// style.
		/// </summary>
		/// <param name="styleName">Name of the style.</param>
		/// ------------------------------------------------------------------------------------
		private ITsPropsBldr GetOrCreatePropOverride(string styleName)
		{
			ITsPropsBldr overridenProps;
			if (!m_propOverrides.TryGetValue(styleName, out overridenProps))
			{
				overridenProps = TsPropsBldrClass.Create();
				m_propOverrides[styleName] = overridenProps;
			}
			return overridenProps;
		}

		#region Overriden methods to prevent misuse of this in-memory class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to prevent caller from using this class to store a style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void PutStyle(string sName, string sUsage, int hvoStyle, int hvoBasedOn, int hvoNext, int nType, bool fBuiltIn, bool fModified, ITsTextProps ttp)
		{
			throw new InvalidOperationException("Cannot save styles using an in-memory stylesheet");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to prevent caller from using this class to create a style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override int GetNewStyleHvo()
		{
			throw new InvalidOperationException("Cannot create styles using an in-memory stylesheet");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to prevent caller from using this class to delete a style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void Delete(int hvoStyle)
		{
			throw new InvalidOperationException("Cannot delete styles using an in-memory stylesheet");
		}
		#endregion
	}

	/// <summary>
	/// Exception for handling case when an incompatible style already exists
	/// </summary>
	public class IncompatibleStyleExistsException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IncompatibleStyleExistsException"/> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public IncompatibleStyleExistsException(string message) : base(message)
		{
		}
	}
}
