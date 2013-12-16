// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ImportStyleProxy.cs
// Responsibility: TE Team
//
// <remarks>
// Implementation of ImportStyleProxy
// </remarks>
// --------------------------------------------------------------------------------------------
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE
{
	#region ImportStyleProxy
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Objects of this class represent a style (real or potential) that is mapped to an import
	/// tag. A hash map (eg m_hmstuisy) owns these proxies and provides the mapping.
	/// </summary>
	/// <remarks>
	/// Note on implementation of end markers:
	/// For proxies that represent a char style or a footnote start, an end marker may
	/// optionally be saved in the proxy's member variables. In addition, another proxy must be
	/// created to map the end marker itself, merely for purpose of identifying the marker as
	/// having the context EndMarker.
	/// When iterating through the scriptureObject's tags (using IScScriptureText.NthTag),
	/// a tag which has an end marker is followed immediately by an extra tag for the
	/// end marker itself. This extra tag should be used to create the respective end marker
	/// proxy.
	/// </remarks>
	/// ---------------------------------------------------------------------------------------
	public class ImportStyleProxy : IParaStylePropsProxy
	{
		#region Member Variables
		/// <summary>reference to the TE stylesheet</summary>
		/// <remarks>This shouldn't be static because it's different between databases</remarks>
		private FwStyleSheet m_FwStyleSheet;
		/// <summary>
		/// style proxy name (real or potential style name)</summary>
		protected string m_sStyleName;
		/// <summary></summary>
		protected MarkerDomain m_domain = MarkerDomain.Default;
		/// <summary></summary>
		protected bool m_excluded;
		/// <summary></summary>
		protected MappingTargetType m_target = MappingTargetType.TEStyle;
		/// <summary>
		/// The FDO style represented by this proxy. This will be null if this proxy is not
		/// for a real style.
		/// </summary>
		/// <remarks>If this is a proxy for a potential style, then when it is really needed
		/// during import, we add the style to the stylesheet on the fly and set this member.
		/// </remarks>
		protected IStStyle m_style;
		/// <summary>
		/// Context of the style, used to control processing during import</summary>
		protected ContextValues m_Context;
		/// <summary>
		/// style type, either kstCharacter or kstParagraph </summary>
		protected StyleType m_StyleType;
		/// <summary>Type of annotation, if this proxy is to be used for creating annotations</summary>
		protected ICmAnnotationDefn m_annotationType = null;

		/// <summary>
		/// Writing system used for text of this style.
		/// </summary>
		/// <remarks>Usually this is the default vernacular ws for the Vernacular domain and
		/// default analysis ws for other domains.  It is set to -1 for the "unknown" domain,
		/// which means that it is dynamically inherited from the preceding text.
		/// </remarks>
		protected int m_ws;
		/// <summary>
		/// If true, this style is used in scripture text- not translation notes, etc.
		/// A "scripture style" utilizes the vernacular writing system in the main translation,
		/// and the analysis writing system in a back translation.
		/// </summary>
		protected bool m_fIsScriptureStyle;

		/// <summary>end marker for this tag, if char style or footnote start</summary>
		protected string m_sEndMarker;

		/// <summary>
		/// TextProps for a run of text to be tagged with this style.
		/// For char style proxy, this contains ws and char style name; for para style this
		/// contains ws only. These props are applied to the run in an ITsStringBuilder.
		/// </summary>
		protected ITsTextProps m_ttpRunProps;

		/// <summary>
		/// TextProps for a paragraph of text to be tagged with this (paragraph) style.
		/// For char style proxy, this is empty and not used; for para style this contains the
		/// paragraph style name. These props are used in the StyleRules item of an StPara.
		/// The data format is serialized bytes of an ITsTextProps.
		/// </summary>
		protected byte [] m_rgbParaProps;
		private const int kcbFmtBufMax = 1024;

		/// <summary>
		/// Formatting properties for this (potential) style.
		/// If style name is not yet in TE stylesheet, we hold type and formatting info here
		/// for if/when the style must be added.
		/// </summary>
		protected ITsTextProps m_ttpFormattingProps;
		private StructureValues m_structure = StructureValues.Undefined;
		private FunctionValues m_function = FunctionValues.Prose;
		#endregion

		#region constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor with default ContextValues (this will be set to ContextValues.General unless
		/// DB has an actual value that is different).
		/// </summary>
		/// <param name="sStyleName">Name of the style.</param>
		/// <param name="styleType">kstCharacter or kstParagraph</param>
		/// <param name="ws">character or paragraph writing system</param>
		/// <param name="styleSheet">The style sheet</param>
		/// ------------------------------------------------------------------------------------
		public ImportStyleProxy(string sStyleName, StyleType styleType, int ws,
			FwStyleSheet styleSheet) :
			this (sStyleName, styleType, ws, ContextValues.Text, MarkerDomain.Default, styleSheet)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor with ContextValues as a parameter.
		/// </summary>
		/// <param name="sStyleName">Name of the style.</param>
		/// <param name="styleType">kstCharacter or kstParagraph</param>
		/// <param name="ws">character or paragraph writing system</param>
		/// <param name="context">Context that will be used if this is a new style (otherwise existing
		/// context in DB will be used), see ContextValues for possible types</param>
		/// <param name="styleSheet">The style sheet</param>
		/// ------------------------------------------------------------------------------------
		public ImportStyleProxy(string sStyleName, StyleType styleType, int ws,
			ContextValues context, FwStyleSheet styleSheet)
			: this(sStyleName, styleType, ws, context, MarkerDomain.Default, styleSheet)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor with ContextValues and MarkerDomain as a parameters.
		/// </summary>
		/// <param name="mapping">The Scr marker mapping.</param>
		/// <param name="ws">character or paragraph writing system</param>
		/// <param name="styleSheet">The style sheet</param>
		/// ------------------------------------------------------------------------------------
		public ImportStyleProxy(ImportMappingInfo mapping, int ws, FwStyleSheet styleSheet) :
			this(mapping.StyleName,	mapping.IsInline ? StyleType.kstCharacter : StyleType.kstParagraph,
				ws, ContextValues.General, mapping.Domain, styleSheet)
		{
			Excluded = mapping.IsExcluded;
			MappingTarget = mapping.MappingTarget;
			m_annotationType = mapping.NoteType;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor with ContextValues and MarkerDomain as a parameters.
		/// </summary>
		/// <param name="sStyleName">Name of the style.</param>
		/// <param name="styleType">kstCharacter or kstParagraph</param>
		/// <param name="ws">character or paragraph writing system</param>
		/// <param name="context">Context that will be used if this is a new style (otherwise existing
		/// context in DB will be used), see ContextValues for possible types</param>
		/// <param name="domain">The marker domain to use</param>
		/// <param name="styleSheet">The style sheet</param>
		/// ------------------------------------------------------------------------------------
		public ImportStyleProxy(string sStyleName, StyleType styleType, int ws,
			ContextValues context, MarkerDomain domain, FwStyleSheet styleSheet)
		{
			m_FwStyleSheet = styleSheet;
			m_domain = domain;
			Debug.Assert(m_FwStyleSheet != null);

			m_ttpFormattingProps = null;
			m_fIsScriptureStyle = true; //default
			m_sEndMarker = null; //default

			if (context == ContextValues.EndMarker)
			{	// this proxy represents an end marker - not a style; set bogus info
				sStyleName = "End"; //name does not matter
				styleType = StyleType.kstCharacter;
			}
			else if (sStyleName != null)
			{
				// Determine whether style exists in the StyleSheet
				Debug.Assert(ws != 0);
				m_style = m_FwStyleSheet.FindStyle(sStyleName);
				if (m_style != null)
				{
					// If this is an existing style, the actual type, context, structure, and
					// function always override the requested values.
					styleType = m_style.Type;
					context = (ContextValues)m_style.Context;
					m_structure = (StructureValues)m_style.Structure;
					m_function = (FunctionValues)m_style.Function;
				}
			}

			m_sStyleName = sStyleName;
			m_StyleType = styleType;
			m_ws = ws;
			m_Context = context;

//			//force StartOfFootnote marker to be processed as a para style proxy having para props
//			if (context == StyleRole.StartOfFootnote)
//				m_StyleType = StyleType.kstParagraph;

			//set the text property vars for this proxy
			SetTextProps();
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The domain for the style
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MarkerDomain Domain
		{
			get {return m_domain;}
			set {m_domain = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the excluded state of the style
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Excluded
		{
			get { return m_excluded; }
			set { m_excluded = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// gets/sets the mapping target
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MappingTargetType MappingTarget
		{
			get { return m_target; }
			set { m_target = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the TE style id (currently a string, but someday maybe an int)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string StyleId
		{
			get	{return m_sStyleName;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FunctionValues Function
		{
			get {return m_function;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StructureValues Structure
		{
			get {return m_structure;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ContextValues Context
		{
			get	{return m_Context;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StyleType StyleType
		{
			get {return m_StyleType;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writing system for this style proxy. If -1, then the WS should be based on the
		/// context
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int WritingSystem
		{
			get {return m_ws;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// End marker for this style proxy. Used optionally if this is a char style or a
		/// footnote start.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string EndMarker
		{
			get {return m_sEndMarker;}
			set
			{
				if (m_StyleType == StyleType.kstCharacter ||
					m_Context == ContextValues.Note)
				{
					Debug.Assert(value != null);
					m_sEndMarker = value;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsUnknownMapping
		{
			get	{return (m_style == null);}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the character properties for this proxy, creating a real style on-the-fly if
		/// necessary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsTextProps TsTextProps
		{
			get
			{
				AddStyleToStylesheet();
				return m_ttpRunProps;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the paragraph properties for this proxy as a serialized array of bytes,
		/// creating a real style on-the-fly if necessary.
		/// </summary>
		/// <remarks>REVIEW: This won't be necessary if we decide to replace it with Props.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public byte[] ParaProps
		{
			get
			{
				AddStyleToStylesheet();
				return m_rgbParaProps;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style associated with this proxy, if one exists. If this is a proxy for a
		/// non-existent style, this returns null (the style will NOT be created).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IStStyle Style
		{
			get { return m_style; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the note.
		/// </summary>
		/// <value>The type of the note.</value>
		/// ------------------------------------------------------------------------------------
		public ICmAnnotationDefn NoteType
		{
			get { return m_annotationType; }
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the format vars for this potential style used when this is an undefined
		/// mapping, to prepare for possibly adding a real style
		/// </summary>
		/// <param name="tsTextPropsFormat">Formatting properties for this (potential) style
		/// </param>
		/// <param name="fIsScriptureStyle">true if style is used in Scripture; false otherwise
		/// </param>
		/// ------------------------------------------------------------------------------------
		public void SetFormat(ITsTextProps tsTextPropsFormat, bool fIsScriptureStyle)
		{
			Debug.Assert(tsTextPropsFormat != null);
			m_ttpFormattingProps = tsTextPropsFormat;
			m_fIsScriptureStyle = fIsScriptureStyle;
		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used by constructor.
		/// Sets the text property vars for this proxy, from the name, type, and ws
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetTextProps()
		{
			if (m_Context == ContextValues.EndMarker || m_sStyleName == null || m_sStyleName == string.Empty)
			{	// props are not relevant for end markers or markers with no style name
				m_ttpRunProps = m_ws == 0 ? null : StyleUtils.CharStyleTextProps(null, m_ws);
				m_rgbParaProps = null;
				return;
			}
			Debug.Assert(m_StyleType == StyleType.kstCharacter || m_StyleType == StyleType.kstParagraph);
			Debug.Assert(m_ws != 0);

			// For char style, the run props contain writing system & char style name; for para
			// style, they contain only the writing system.
			m_ttpRunProps = StyleUtils.CharStyleTextProps(
				(m_StyleType == StyleType.kstCharacter) ? m_sStyleName : null, m_ws);

			// For char style, the paragraph props are empty; for para style, they contain the
			// para style name.
			if (m_StyleType == StyleType.kstParagraph)
			{
				ITsTextProps props = StyleUtils.ParaStyleTextProps(m_sStyleName);
				using (ArrayPtr rgbFmtBufPtr = MarshalEx.ArrayToNative<byte>(kcbFmtBufMax))
				{
					int nBytes = props.SerializeRgb(rgbFmtBufPtr, kcbFmtBufMax);
					m_rgbParaProps = MarshalEx.NativeToArray<byte>(rgbFmtBufPtr, nBytes);
				}
			}
			else
				m_rgbParaProps = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a new real style in the stylesheet for this proxy, if needed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddStyleToStylesheet()
		{
			if (m_style != null || m_Context == ContextValues.EndMarker || m_sStyleName == null)
				return;
			// If m_ttpFormattingProps has not been set up, initialize it now
			if (m_ttpFormattingProps == null)
			{
				ITsPropsBldr tsPropsBldr = TsPropsBldrClass.Create();
				m_ttpFormattingProps = tsPropsBldr.GetTextProps(); // default properties
			}

			// Get an hvo for the new style
			int hvoStyle = m_FwStyleSheet.MakeNewStyle();
			m_style = m_FwStyleSheet.Cache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(hvoStyle);

			// PutStyle() adds the style to the stylesheet. we'll give it the properties we
			// are aware of.
			m_FwStyleSheet.PutStyle(m_sStyleName, string.Empty, hvoStyle, 0,
				m_StyleType == StyleType.kstParagraph ? hvoStyle : 0, (int)m_StyleType, false, false, m_ttpFormattingProps);

			// base the new style on "Paragraph"
			if (m_StyleType == StyleType.kstParagraph)
			{
				m_style.BasedOnRA = m_FwStyleSheet.FindStyle(ScrStyleNames.NormalParagraph);
				m_style.Context = m_style.BasedOnRA.Context;
				m_style.Structure = m_style.BasedOnRA.Structure;
				m_style.Function = m_style.BasedOnRA.Function;
			}
		}
		#endregion
	}
	#endregion
}
