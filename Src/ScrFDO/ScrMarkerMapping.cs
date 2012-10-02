// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrMarkerMapping.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.Scripture
{
	#region MarkerDomain enum
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Marker domains to indicate where markers are imported to
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Flags]
	public enum MarkerDomain
	{
		/// <summary>Default domain, based on context</summary>
		Default = 0x00,
		/// <summary>Don't use this for anything except blob conversion and interpreting old
		/// data when loading mapping lists from Db into memory - use Default</summary>
		DeprecatedScripture = 0x01, //
		/// <summary>back translation domain</summary>
		BackTrans = 0x02,
		/// <summary>Scripture annotations domain</summary>
		Note = 0x04,
		/// <summary>footnote domain</summary>
		Footnote = 0x08,
	}
	#endregion

	#region MappingTargetType
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This is used both in defining mapping properties for this class AND also for the
	/// ImportStyleProxy.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum MappingTargetType
	{
		/// <summary>the typical and default case</summary>
		TEStyle = 0,
		/// <summary>is a USFM-style picture/graphic mapping (vertical bars separate parameters)</summary>
		Figure = 1,
		/// <summary>The vernacular translation of "Chapter"</summary>
		ChapterLabel = 2,
		/// <summary>Short name of the book, suitable for displaying on page headers</summary>
		TitleShort = 3,
		/// <summary>Used for the default paragraph characters. This is only used
		/// when the mapping is saved to the database. In the future, this may
		/// also be used in code.</summary>
		DefaultParaChars = 4,
		/// <summary>Caption of a picture</summary>
		FigureCaption,
		/// <summary>Copyright line for a picture</summary>
		FigureCopyright,
		/// <summary>Non-publishable description for a picture</summary>
		FigureDescription,
		/// <summary>Filename of a picture</summary>
		FigureFilename,
		/// <summary>Indication of where/how picture should be laid out (col, span, right,
		/// left, fill-col?, fill-span?, full-page?)</summary>
		FigureLayoutPosition,
		/// <summary>Reference range to which a picture applies (e.g., MRK 1--2,
		/// JHN 3:16-19)</summary>
		FigureRefRange,
		/// <summary>Scale factor for a picture (an integral percentage)</summary>
		FigureScale
	}
	#endregion

	#region ImportMappingInfo class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// In-memory information about mappings
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ImportMappingInfo
	{
		#region data members
		/// <summary></summary>
		private string m_beginMarker;
		/// <summary></summary>
		private string m_endMarker;
		/// <summary></summary>
		private bool m_isExcluded;
		/// <summary></summary>
		private MappingTargetType m_mappingTarget;
		/// <summary></summary>
		private MarkerDomain m_domain;
		/// <summary></summary>
		private string m_styleName;
		/// <summary></summary>
		private IStStyle m_style;
		/// <summary></summary>
		private string m_icuLocale;
		/// <summary></summary>
		private Dictionary<object, bool> m_inUse = new Dictionary<object, bool>();
		/// <summary>Used for annotation mappings</summary>
		private ICmAnnotationDefn m_noteType;
		/// <summary>Indicator to tell when the mapping info has been changed.</summary>
		private bool m_hasChanged = false;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Mega constructor with style
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo(string beginMarker, string endMarker, bool isExcluded,
			MappingTargetType mappingTarget, MarkerDomain domain, IStStyle style,
			string icuLocale, ICmAnnotationDefn noteType) :
			this(beginMarker, endMarker, isExcluded, mappingTarget, domain,
			style == null ? null : style.Name, icuLocale, noteType)
		{
			m_style = style;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Mega constructor with style name
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo(string beginMarker, string endMarker, bool isExcluded,
			MappingTargetType mappingTarget, MarkerDomain domain, string styleName,
			string icuLocale, ICmAnnotationDefn noteType) :
			this(beginMarker, endMarker, isExcluded, mappingTarget, domain, styleName,
			icuLocale, noteType, false, ImportDomain.Main /*This will be ignored*/)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Mega constructor with style name
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo(string beginMarker, string endMarker, bool isExcluded,
			MappingTargetType mappingTarget, MarkerDomain domain, string styleName,
			string icuLocale, ICmAnnotationDefn noteType, bool isInUse, ImportDomain importDomain)
		{
			m_beginMarker = beginMarker;
			m_endMarker = endMarker;
			m_isExcluded = isExcluded;
			m_mappingTarget = mappingTarget;
			m_domain = domain;
			m_styleName = styleName;
			m_style = null;
			m_icuLocale = icuLocale;
			m_noteType = noteType;
			if (isInUse)
			{
				m_inUse[ScrImportSet.CreateImportSourceKey(importDomain, icuLocale,
					noteType == null ? 0 : noteType.Hvo)] = isInUse;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Mini constructor (used for testing)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo(string beginMarker, string endMarker, bool isExcluded,
			MappingTargetType mappingTarget, MarkerDomain domain, string styleName,
			string icuLocale) : this(beginMarker, endMarker, isExcluded, mappingTarget,
				domain, styleName, icuLocale, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor (used for testing) for mapping a non-inline marker to a style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo(string beginMarker, MarkerDomain domain, string styleName,
			string icuLocale, ICmAnnotationDefn noteType) : this(beginMarker, null, false,
			MappingTargetType.TEStyle, domain, styleName, icuLocale, noteType)
		{
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor (used for testing) for mapping an inline marker to a style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo(string beginMarker, string endMarker, MarkerDomain domain,
			string styleName, string icuLocale, ICmAnnotationDefn noteType) : this(beginMarker,
			endMarker, false, MappingTargetType.TEStyle, domain, styleName, icuLocale, noteType)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Micro constructor (used for testing)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo(string beginMarker, string endMarker, string styleName) :
			this(beginMarker, endMarker, false, MappingTargetType.TEStyle, MarkerDomain.Default,
			styleName, null, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy constructor
		/// </summary>
		/// <param name="copy"></param>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo(ImportMappingInfo copy)
		{
			m_beginMarker = copy.m_beginMarker;
			m_endMarker = copy.m_endMarker;
			m_isExcluded = copy.m_isExcluded;
			m_mappingTarget = copy.m_mappingTarget;
			m_domain = copy.m_domain;
			m_styleName = copy.m_styleName;
			m_style = copy.m_style;
			m_icuLocale = copy.m_icuLocale;
			m_noteType = copy.m_noteType;
		}
		#endregion

		#region Public properties (and one internal setter)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the begin marker
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string BeginMarker
		{
			get { return m_beginMarker; }
			set
			{
				m_beginMarker = value;
				m_hasChanged = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the end marker
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string EndMarker
		{
			get { return m_endMarker; }
			set
			{
				m_endMarker = value;
				m_hasChanged = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets whether this marker is excluded
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsExcluded
		{
			get { return m_isExcluded; }
			set
			{
				m_isExcluded = value;
				m_hasChanged = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the mapping target
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MappingTargetType MappingTarget
		{
			get { return m_mappingTarget; }
			set
			{
				m_mappingTarget = value;
				m_hasChanged = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the marker domain
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MarkerDomain Domain
		{
			get { return m_domain; }
			set
			{
				m_domain = value;
				m_hasChanged = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the style name
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string StyleName
		{
			get { return m_styleName; }
			set
			{
				m_styleName = value;
				// Prevent use of old style property when new style is being specified
				if (m_style != null && m_style.Name != value)
					m_style = null;
				m_hasChanged = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style that this marker maps to. This will not be valid until the settings
		/// are saved and the style is hooked up based on the style name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IStStyle Style
		{
			get { return m_style; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the style for the mapping. This is internal because we want all external
		/// processes to set the style name instead but ScrImportSet needs to be able to
		/// set the style when saving the mapping info to a ScrMarkerMapping object.
		/// </summary>
		/// <param name="style"></param>
		/// ------------------------------------------------------------------------------------
		internal void SetStyle(IStStyle style)
		{
			Debug.Assert(style == null || m_styleName == style.Name);
			m_style = style;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the ICU locale
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string IcuLocale
		{
			get { return m_icuLocale; }
			set
			{
				m_icuLocale = value;
				m_hasChanged = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The note type that will be created for data marked with this marker.
		/// </summary>
		/// <remarks>Used only for annotation mappings</remarks>
		/// ------------------------------------------------------------------------------------
		public ICmAnnotationDefn NoteType
		{
			get { return m_noteType; }
			set
			{
				m_noteType = value;
				m_hasChanged = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether or not this mapping is for an in-line marker
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsInline
		{
			get { return m_endMarker != null && m_endMarker != string.Empty; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether the style represented by this mapping is a paragraph style
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if the m_style member has not
		/// been set to a non-null value (either in the constructor or as a side-effect of
		/// saving the ScrImportSet)</exception>
		/// ------------------------------------------------------------------------------------
		public bool IsParagraphStyle
		{
			get
			{
				if (m_style == null)
					throw new InvalidOperationException("Can't get the style when the settings have not been saved.");
				return (m_style.Type == StyleType.kstParagraph);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tells if the marker is in use.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsInUse
		{
			get
			{
				foreach (bool f in m_inUse.Values)
				{
					if (f)
						return true;
				}
				return false;
			}
			set
			{
				if (value)
				{
					throw new ArgumentException("Use SetInUse to set this property to true.");
				}
				else
				{
					//We are blowing away the dictionary to make IsInUse false for all input sources.
					m_inUse = new Dictionary<object, bool>();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the state of the changed flag. This indicates if the mapping has been edited
		/// since it was created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool HasChanged
		{
			get { return m_hasChanged; }
			set { m_hasChanged = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets flag to indicate whether this mapping's begin marker is in use in the given
		/// import domain -- Use this version only for Paratext 6.
		/// </summary>
		/// <param name="importDomain">Import domain</param>
		/// <param name="value"><c>true</c> if the marker is in use</param>
		/// ------------------------------------------------------------------------------------
		public void SetIsInUse(ImportDomain importDomain, bool value)
		{
			m_inUse[importDomain] = value;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets flag to indicate whether this mapping's begin marker is in use in the given
		/// import source -- Use this version only for non-Paratext 6.
		/// </summary>
		/// <param name="importDomain">Import domain of the source</param>
		/// <param name="icuLocale">ICU locale of the import source</param>
		/// <param name="hvoNoteType">ID of the default note type for the import source</param>
		/// <param name="value"><c>true</c> if the marker is in use</param>
		/// ------------------------------------------------------------------------------------
		public void SetIsInUse(ImportDomain importDomain, string icuLocale, int hvoNoteType,
			bool value)
		{
			m_inUse[ScrImportSet.CreateImportSourceKey(importDomain, icuLocale, hvoNoteType)] = value;
		}
		#endregion

		#region operator overloads
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override of Equals method
		/// </summary>
		/// <param name="obj">Object to compare against</param>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			if (obj is ScrMarkerMapping)
				return this == (ScrMarkerMapping)obj;
			return base.Equals(obj);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Have to override this, but don't know why
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare a ScrMarkerMapping to an ImportMappingInfo object for equality.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator ==(ScrMarkerMapping mapping, ImportMappingInfo info)
		{
			if (object.Equals(mapping, null) && object.Equals(info, null))
				return true;
			if (object.Equals(mapping, null) || object.Equals(info, null))
				return false;

			return (mapping.BeginMarker == info.BeginMarker &&
				mapping.EndMarker == info.EndMarker &&
				mapping.StyleName == info.StyleName &&
				mapping.Excluded == info.IsExcluded &&
				mapping.Target == (int)info.MappingTarget &&
				mapping.Domain == (int)info.Domain &&
				mapping.ICULocale == info.IcuLocale &&
				mapping.NoteTypeRA == info.NoteType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare an ImportMappingInfo to a ScrMarkerMapping object for equality.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator ==(ImportMappingInfo info, ScrMarkerMapping mapping)
		{
			return mapping == info;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare a ScrMarkerMapping to an ImportMappingInfo object for inequality.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator !=(ScrMarkerMapping mapping, ImportMappingInfo info)
		{
			return !(mapping == info);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare an ImportMappingInfo to a ScrMarkerMapping object for inequality.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator !=(ImportMappingInfo info, ScrMarkerMapping mapping)
		{
			return !(mapping == info);
		}
		#endregion
	}
	#endregion

	#region ScrMarkerMapping class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// ScrMarkerMapping holds info about an Import mapping.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ScrMarkerMapping : CmObject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the members of a ScrMarkerMapping from an ImportMappingInfo object
		/// </summary>
		/// <param name="info"></param>
		/// ------------------------------------------------------------------------------------
		public void InitFromImportMappingInfo(ImportMappingInfo info)
		{
			BeginMarker = info.BeginMarker;
			EndMarker = info.EndMarker;
			Excluded = info.IsExcluded;
			Target = (int)info.MappingTarget;
			Domain = (int)info.Domain;
			StyleRA = info.Style == null ? m_cache.LangProject.TranslatedScriptureOA.FindStyle(info.StyleName) : info.Style;
			ICULocale = info.IcuLocale;
			NoteTypeRA = info.NoteType;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the members of a ScrMarkerMapping into an ImportMappingInfo object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo ToImportMappingInfo()
		{
			MarkerDomain domain = (MarkerDomain)Domain;
			if ((domain & MarkerDomain.DeprecatedScripture) != 0)
			{
				// If this mapping is for DeprecatedScripture, then clear
				// the DeprecatedScripture bit.
				domain ^= MarkerDomain.DeprecatedScripture;
			}

			if (Target == (int)MappingTargetType.DefaultParaChars)
			{
				return new ImportMappingInfo(BeginMarker, EndMarker, Excluded,
					MappingTargetType.TEStyle, domain, FdoResources.DefaultParaCharsStyleName,
					ICULocale, NoteTypeRA);
			}

			return new ImportMappingInfo(BeginMarker, EndMarker, Excluded,
				(MappingTargetType)Target, domain, StyleRA, ICULocale, NoteTypeRA);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the style name from the attached style if one exists
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string StyleName
		{
			get { return (StyleRA == null ? null : StyleRA.Name); }
		}
		#region Static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the default marker domain for the given style name
		/// </summary>
		/// <param name="styleSheet">style sheet to use for determining the domain</param>
		/// <param name="styleName">The style name</param>
		/// <returns>the default marker domain</returns>
		/// ------------------------------------------------------------------------------------
		public static MarkerDomain GetDefaultDomainForStyle(IVwStylesheet styleSheet,
			string styleName)
		{
			try
			{
				// Footnote target ref and marker styles are always footnote domain
				if (styleName == ScrStyleNames.FootnoteTargetRef ||
					styleName == ScrStyleNames.FootnoteMarker)
					return MarkerDomain.Footnote;

				// Chapter and verse numbers are always Default domain
				if (styleName == ScrStyleNames.ChapterNumber ||
					styleName == ScrStyleNames.VerseNumber)
					return MarkerDomain.Default;

				if (styleSheet == null)
				{
					// REVIEW: JohnW - Seems like this should not be valid since we won't get
					//         the mapping right. Tried putting a Debug.Fail here to see if
					//         I could eliminate this check, but tests will have to be
					//         changed and the code to update existing settings will need
					//         to be changed.
					return MarkerDomain.Default;
				}

				// Base the domain on the style context
				switch ((ContextValues)styleSheet.GetContext(styleName))
				{
					case ContextValues.Note:
						return MarkerDomain.Footnote;
					case ContextValues.Annotation:
						return MarkerDomain.Note;
					default:
						return MarkerDomain.Default;
				}
			}
			catch
			{
				// This is probably in a test, so we don't care. (Or maybe something really
				// bad happened, like the style was deleted or renamed.)
				return MarkerDomain.Default;
			}
		}
		#endregion
	}
	#endregion
}
