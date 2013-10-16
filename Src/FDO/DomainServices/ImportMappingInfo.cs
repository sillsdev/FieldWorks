// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ImportMappingInfo.cs
// Responsibility: shaneyfelt
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.DomainServices
{
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
		private string m_wsId;
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
			string wsId, ICmAnnotationDefn noteType) :
			this(beginMarker, endMarker, isExcluded, mappingTarget, domain,
			style == null ? null : style.Name, wsId, noteType)
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
			string wsId, ICmAnnotationDefn noteType) :
			this(beginMarker, endMarker, isExcluded, mappingTarget, domain, styleName,
			wsId, noteType, false, ImportDomain.Main /*This will be ignored*/)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Mega constructor with style name
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo(string beginMarker, string endMarker, bool isExcluded,
			MappingTargetType mappingTarget, MarkerDomain domain, string styleName,
			string wsId, ICmAnnotationDefn noteType, bool isInUse, ImportDomain importDomain)
		{
			m_beginMarker = beginMarker;
			m_endMarker = endMarker;
			m_isExcluded = isExcluded;
			m_mappingTarget = mappingTarget;
			m_domain = domain;
			m_styleName = styleName;
			m_style = null;
			m_wsId = wsId;
			m_noteType = noteType;
			if (isInUse)
			{
				m_inUse[ScriptureServices.CreateImportSourceKey(importDomain, wsId,
					noteType)] = isInUse;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Mini constructor (used for testing)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo(string beginMarker, string endMarker, bool isExcluded,
			MappingTargetType mappingTarget, MarkerDomain domain, string styleName,
			string wsId)
			: this(beginMarker, endMarker, isExcluded, mappingTarget,
				domain, styleName, wsId, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor (used for testing) for mapping a non-inline marker to a style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo(string beginMarker, MarkerDomain domain, string styleName,
			string wsId, ICmAnnotationDefn noteType)
			: this(beginMarker, null, false,
				MappingTargetType.TEStyle, domain, styleName, wsId, noteType)
		{
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor (used for testing) for mapping an inline marker to a style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo(string beginMarker, string endMarker, MarkerDomain domain,
			string styleName, string wsId, ICmAnnotationDefn noteType)
			: this(beginMarker,
				endMarker, false, MappingTargetType.TEStyle, domain, styleName, wsId, noteType)
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
			m_wsId = copy.m_wsId;
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
		/// Gets/sets the writing system identifier
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string WsId
		{
			get { return m_wsId; }
			set
			{
				m_wsId = value;
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
		/// <param name="noteType">The default note type for the import source</param>
		/// <param name="value"><c>true</c> if the marker is in use</param>
		/// ------------------------------------------------------------------------------------
		public void SetIsInUse(ImportDomain importDomain, string icuLocale,
			ICmAnnotationDefn noteType, bool value)
		{
			m_inUse[ScriptureServices.CreateImportSourceKey(importDomain, icuLocale, noteType)] = value;
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
			if (obj is IScrMarkerMapping)
				return this == (IScrMarkerMapping)obj;
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
		public static bool operator ==(IScrMarkerMapping mapping, ImportMappingInfo info)
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
				mapping.WritingSystem == info.WsId &&
				mapping.NoteTypeRA == info.NoteType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare an ImportMappingInfo to a ScrMarkerMapping object for equality.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator ==(ImportMappingInfo info, IScrMarkerMapping mapping)
		{
			return mapping == info;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare a ScrMarkerMapping to an ImportMappingInfo object for inequality.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator !=(IScrMarkerMapping mapping, ImportMappingInfo info)
		{
			return !(mapping == info);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare an ImportMappingInfo to a ScrMarkerMapping object for inequality.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool operator !=(ImportMappingInfo info, IScrMarkerMapping mapping)
		{
			return !(mapping == info);
		}
		#endregion
	}
}
