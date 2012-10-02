// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2006' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrMappingList.cs
// Responsibility: TomB
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.IO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// ScrMappingList is a sorted list that contains ImportMappingInfo objects.
	/// The list is sorted by begin marker.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrMappingList : IEnumerable
	{
		#region data members
		private MappingSet m_mappingSet;
		private SortedList<string, ImportMappingInfo> m_list = new SortedList<string, ImportMappingInfo>();
		private bool m_fMappingDeleted = false;

		private IVwStylesheet m_stylesheet;

		private static Dictionary<string, string> s_defaultMappings = new Dictionary<string, string>();
		private static Dictionary<string, string> s_defaultProperties = new Dictionary<string, string>();
		private static Dictionary<string, string> s_defaultExclusions = new Dictionary<string, string>();
		#endregion

		#region public static readonly members
		/// <summary>Book marker</summary>
		public static readonly string MarkerBook = @"\id";
		/// <summary>Chapter marker</summary>
		public static readonly string MarkerChapter = @"\c";
		/// <summary>Verse marker</summary>
		public static readonly string MarkerVerse = @"\v";
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrMappingList"/> class.
		/// </summary>
		/// <param name="mappingSet">Indicates which type of mapping group this list represents
		/// </param>
		/// <param name="stylesheet">The stylesheet</param>
		/// ------------------------------------------------------------------------------------
		public ScrMappingList(MappingSet mappingSet, IVwStylesheet stylesheet)
		{
			m_mappingSet = mappingSet;
			m_stylesheet = stylesheet;
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a mapping to the mapping list.
		/// </summary>
		/// <param name="mapping">mapping info to add to the list</param>
		/// ------------------------------------------------------------------------------------
		public void Add(ImportMappingInfo mapping)
		{
			if (mapping == null)
				throw new ArgumentNullException();
			if (mapping.BeginMarker == null || mapping.BeginMarker == string.Empty)
				throw new ArgumentException("Begin marker must be set before adding mapping to the list.");

			if (mapping.BeginMarker == MarkerChapter)
			{
				// REVIEW: Do we also need to do this for the Annotations domain. What does the importer expect?
				mapping.StyleName = ScrStyleNames.ChapterNumber;
			}
			else if (mapping.BeginMarker == MarkerVerse)
			{
				// REVIEW: Do we also need to do this for the Annotations domain. What does the importer expect?
				mapping.StyleName = ScrStyleNames.VerseNumber;
			}
			else if (mapping.BeginMarker == MarkerBook)
			{
				// \id markers do not have a style or domain
				mapping.Domain = MarkerDomain.Default;
				mapping.StyleName = null;
			}
			else
			{
				switch (m_mappingSet)
				{
					case MappingSet.Main:
						// If this is a BT marker but it's already in the list in the Default domain, clear the
						// BT flag and make sure it isn't being redefined.
						if ((mapping.Domain & MarkerDomain.BackTrans) != 0 && m_list.ContainsKey(mapping.BeginMarker))
						{
							ImportMappingInfo existingMapping = this[mapping.BeginMarker];
							if ((existingMapping.Domain & MarkerDomain.BackTrans) == 0)
								mapping.Domain = existingMapping.Domain;
						}
						break;

					case MappingSet.Notes:
						// If the mapping is for the annotations domain and it is marked as Note, then
						// set it to Default.
						if (mapping.Domain == MarkerDomain.Note)
							mapping.Domain = MarkerDomain.Default;
						if (mapping.Domain != MarkerDomain.Default)
							throw new ArgumentException("Invalid mapping domain");
						break;
				}
			}
			m_list[mapping.BeginMarker] = mapping;
			mapping.HasChanged = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a default mapping for the given Standard Format marker if it does not exist yet.
		/// </summary>
		/// <param name="marker">The SF marker</param>
		/// <param name="domain">The import domain from which this marker originates</param>
		/// <param name="fAutoMapBtMarkers">Indicates whether markers beginning with "bt" should
		/// be treated as back-translation markers if possible.</param>
		/// <returns>The newly added mapping info, or the existing one if already in the list
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo AddDefaultMappingIfNeeded(string marker, ImportDomain domain,
			bool fAutoMapBtMarkers)
		{
			return AddDefaultMappingIfNeeded(marker, null, domain, fAutoMapBtMarkers, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a default mapping for the given Standard Format marker if it does not exist yet.
		/// </summary>
		/// <param name="marker">The SF marker</param>
		/// <param name="endMarker">The end marker (or null)</param>
		/// <param name="importDomain">The import domain from which this marker originates</param>
		/// <param name="fAutoMapBtMarkers">Indicates whether markers beginning with "bt" should
		/// be treated as back-translation markers if possible.</param>
		/// <param name="isInUse">Indicates whether this marker is actually in use in one or more
		/// of the import files (for P6, this is currently always false since we're getting the
		/// markers from the STY file -- we come back in a second pass and set it to true if we
		/// find it in a file).</param>
		/// <returns>The newly added mapping info, or the existing one if already in the list
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo AddDefaultMappingIfNeeded(string marker, string endMarker,
			ImportDomain importDomain, bool fAutoMapBtMarkers, bool isInUse)
		{
			// Look for the marker - if it is found, then we are done
			if (this[marker] != null)
				return this[marker];

			// Read the TEStyles XML file to generate a table of mappings
			if (s_defaultMappings.Count == 0)
				ReadDefaultMappings();

			string styleName;
			bool excluded;
			MappingTargetType target;
			MarkerDomain markerDomain = (importDomain != ImportDomain.BackTrans) ?
				MarkerDomain.Default : MarkerDomain.BackTrans;

			if (importDomain == ImportDomain.Annotations)
			{
				// TODO (TE-5004): Map \rem (and possibly other markers?) automatically in annotations
				// domain. Probably need to have a separate import mapping set in TeStyles.xml that has
				// default mappings for the Annotations domain.
				styleName = null;
				excluded = s_defaultExclusions.ContainsKey(marker); //Make sure to check exclusions (TE-5703)
				target = MappingTargetType.TEStyle;
			}
			else if (!GetDefaultMapping(marker, out styleName, out excluded, out target, ref markerDomain))
			{
				if (fAutoMapBtMarkers && importDomain == ImportDomain.Main &&
					marker.StartsWith(@"\bt") && marker != @"\btc")
				{
					// pick out the corresponding vernacular marker. "\btblah" -> "\blah"
					string correspondingVernMarker = marker.Remove(1, 2);

					// if domain is DeprecatedScripture and the corresponding vernacular marker is defined...
					ImportMappingInfo correspondingVernMarkerInfo = this[correspondingVernMarker];
					if (correspondingVernMarkerInfo != null &&
						(correspondingVernMarkerInfo.Domain & MarkerDomain.DeprecatedScripture) != 0)
					{
						// clear the DeprecatedScripture bit.
						correspondingVernMarkerInfo.Domain ^= MarkerDomain.DeprecatedScripture;
					}
					if (correspondingVernMarkerInfo != null)
					{
						// If the corresponding vernacular marker is already defined...
						if (correspondingVernMarkerInfo.Domain != MarkerDomain.Note &&
							(correspondingVernMarkerInfo.Domain & MarkerDomain.BackTrans) == 0 &&
							(correspondingVernMarkerInfo.MappingTarget != MappingTargetType.TEStyle ||
							correspondingVernMarkerInfo.StyleName != null))
						{
							styleName = correspondingVernMarkerInfo.StyleName;
							target = correspondingVernMarkerInfo.MappingTarget;
							markerDomain = correspondingVernMarkerInfo.Domain;

							// We only want to map to the BackTrans domain when mapping to a paragraph
							// style because character styles automatically assume the domain of their
							// containing paragraphs.
							if (m_stylesheet == null || styleName == null ||
								m_stylesheet.GetType(styleName) == (int)StyleType.kstParagraph)
							{
								markerDomain |= MarkerDomain.BackTrans;
							}
						}
					}
					else if (GetDefaultMapping(correspondingVernMarker, out styleName, out excluded, out target,
						ref markerDomain))
					{
						// The corresponding vernacular marker has default mapping info so make this marker
						// a back translation of it - unless it is an annotation or BT.
						if (markerDomain == MarkerDomain.Note || markerDomain == MarkerDomain.BackTrans)
						{
							styleName = null;
							excluded = false;
							target = MappingTargetType.TEStyle;
							markerDomain = MarkerDomain.Default;
						}
						else
							markerDomain |= MarkerDomain.BackTrans;
					}
				}
			}
			// Create a mapping for the marker using the default mapping
			ImportMappingInfo newMapping = new ImportMappingInfo(marker, endMarker, excluded, target,
				markerDomain, styleName, null, null, isInUse, importDomain);
			Add(newMapping);
			return newMapping;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete an ImportMappingInfo object from the list
		/// </summary>
		/// <param name="mapping">mapping object to delete</param>
		/// ------------------------------------------------------------------------------------
		public void Delete(ImportMappingInfo mapping)
		{
			if (m_list.ContainsKey(mapping.BeginMarker))
			{
				m_list.Remove(mapping.BeginMarker);
				m_fMappingDeleted = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the in use flags to false for all mappings in the list.
		/// Use this for non-Paratext 6.
		/// </summary>
		/// <param name="importDomain">Import domain</param>
		/// <param name="icuLocale">ICU locale of the import source</param>
		/// <param name="noteType">The default note type for the import source</param>
		/// ------------------------------------------------------------------------------------
		internal void ResetInUseFlags(ImportDomain importDomain, string icuLocale,
			ICmAnnotationDefn noteType)
		{
			foreach (ImportMappingInfo mapping in m_list.Values)
			{
				mapping.SetIsInUse(importDomain, icuLocale, noteType, false);
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the in use flags to false for all mappings in the list.
		/// Use this for Paratext 6.
		/// </summary>
		/// <param name="importDomain">Import domain</param>
		/// ------------------------------------------------------------------------------------
		internal void ResetInUseFlags(ImportDomain importDomain)
		{
			foreach (ImportMappingInfo mapping in m_list.Values)
			{
				mapping.SetIsInUse(importDomain, false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the in use flags to false for all mappings in the list.
		/// Use this when switching between Paratext 6 and non-P6.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void ResetInUseFlags()
		{
			foreach (ImportMappingInfo mapping in m_list.Values)
			{
				mapping.IsInUse = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the in use flags for inline mappings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void ResetInUseFlagsForInlineMappings()
		{
			foreach (ImportMappingInfo mapping in m_list.Values)
			{
				if (mapping.IsInline)
				{
					mapping.IsInUse = false;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an enumerator so clients can use foreach to get the ImportMappingInfo
		/// objects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerator GetEnumerator()
		{
			return m_list.Values.GetEnumerator();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reset the change state flags for all of the mappings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ResetChangedFlags()
		{
			foreach (ImportMappingInfo info in m_list.Values)
				info.HasChanged = false;
			m_fMappingDeleted = false;
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a count of the number of ImportMappingInfo items in the list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Count
		{
			get { return m_list.Count; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve an ImportMappingInfo from the list at the given index
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo this[int index]
		{
			get { return m_list.Values[index]; }
			set { throw new NotImplementedException("Use the Add method instead, please."); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve an ImportMappingInfo from the list having the given begin marker
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo this[string beginMarker]
		{
			get
			{
				ImportMappingInfo mapping;
				m_list.TryGetValue(beginMarker, out mapping);
				return mapping;
			}
			set { Add(value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if any of the mappings in the list have changed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool HasChanged
		{
			get
			{
				if (m_fMappingDeleted)
					return true;

				foreach (ImportMappingInfo info in m_list.Values)
					if (info.HasChanged)
						return true;
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets stylesheet.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwStylesheet StyleSheet
		{
			get { return m_stylesheet; }
			set { m_stylesheet = value; }
		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read the TEStyles.xml file to get the default marker mappings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void ReadDefaultMappings()
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(DirectoryFinder.TeStylesPath);
			XmlNode mappingNode = doc.SelectSingleNode("Styles/ImportMappingSets/ImportMapping[@name='TE Default']");
			foreach (XmlNode mapNode in mappingNode.SelectNodes("mapping"))
			{
				string marker = @"\" + mapNode.Attributes["id"].Value;
				string type = mapNode.Attributes["type"].Value;
				if (type == "style")
				{
					string styleName = mapNode.Attributes["styleName"].Value.Replace("_", " ");
					s_defaultMappings.Add(marker, styleName);
				}
				else if (type == "property")
					s_defaultProperties.Add(marker, mapNode.Attributes["propertyName"].Value);
				else if (type == "excluded")
					s_defaultExclusions.Add(marker, string.Empty);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine the default mapping for the given marker
		/// </summary>
		/// <param name="marker">The begin marker</param>
		/// <param name="styleName">The stylename, or null (output)</param>
		/// <param name="excluded">Flag indicating whether data marked up with this marker
		/// should be excluded (output)</param>
		/// <param name="target">Target type (output)</param>
		/// <param name="markerDomain"></param>
		/// <returns><c>true</c> if a default mapping was found for the given marker;
		/// <c>false</c> otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		private bool GetDefaultMapping(string marker, out string styleName, out bool excluded,
			out MappingTargetType target, ref MarkerDomain markerDomain)
		{
			styleName = null;
			excluded = false;
			target = MappingTargetType.TEStyle;

			if (s_defaultMappings.ContainsKey(marker))
			{
				styleName = s_defaultMappings[marker];
				markerDomain = ScriptureServices.GetDefaultDomainForStyle(m_stylesheet, styleName);
				return true;
			}
			if (s_defaultExclusions.ContainsKey(marker))
			{
				excluded = true;
				return true;
			}
			if (s_defaultProperties.ContainsKey(marker))
			{
				switch (s_defaultProperties[marker])
				{
					case "Figure":
						target = MappingTargetType.Figure;
						return true;

					case "FigureCaption":
						target = MappingTargetType.FigureCaption;
						return true;

					case "FigureCopyright":
						target = MappingTargetType.FigureCopyright;
						return true;

					case "FigureDescription":
						target = MappingTargetType.FigureDescription;
						return true;

					case "FigureFilename":
						target = MappingTargetType.FigureFilename;
						return true;

					case "FigureLayoutPosition":
						target = MappingTargetType.FigureLayoutPosition;
						return true;

					case "FigureRefRange":
						target = MappingTargetType.FigureRefRange;
						return true;

					case "FigureScale":
						target = MappingTargetType.FigureScale;
						return true;

					case "ChapterLabel":
						target = MappingTargetType.ChapterLabel;
						return true;

					case "TitleShort":
						target = MappingTargetType.TitleShort;
						return true;

					case "DefaultParagraphCharacters":
						target = MappingTargetType.TEStyle;
						styleName = ResourceHelper.DefaultParaCharsStyleName;
						return true;

					case "DefaultFootnoteCharacters":
						target = MappingTargetType.TEStyle;
						styleName = ResourceHelper.DefaultParaCharsStyleName;
						markerDomain = MarkerDomain.Footnote;
						return true;
				}
			}
			return false;
		}
		#endregion
	}
}
