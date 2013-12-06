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

using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// ScrMarkerMapping holds info about an Import mapping.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class ScrMarkerMapping
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the members of a ScrMarkerMapping from an ImportMappingInfo object
		/// </summary>
		/// <param name="info"></param>
		/// ------------------------------------------------------------------------------------
		internal void InitFromImportMappingInfo(ImportMappingInfo info)
		{
			BeginMarker = info.BeginMarker;
			EndMarker = info.EndMarker;
			Excluded = info.IsExcluded;
			Target = (int)info.MappingTarget;
			Domain = (int)info.Domain;
			StyleRA = info.Style == null ? m_cache.LangProject.TranslatedScriptureOA.FindStyle(info.StyleName) : info.Style;
			WritingSystem = info.WsId;
			NoteTypeRA = info.NoteType;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the members of a ScrMarkerMapping into an ImportMappingInfo object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ImportMappingInfo ToImportMappingInfo(string defaultParaCharsStyleName)
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
					MappingTargetType.TEStyle, domain, defaultParaCharsStyleName,
					WritingSystem, NoteTypeRA);
			}

			return new ImportMappingInfo(BeginMarker, EndMarker, Excluded,
				(MappingTargetType)Target, domain, StyleRA, WritingSystem, NoteTypeRA);
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
	}
}
