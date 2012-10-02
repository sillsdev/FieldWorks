// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2006' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ParatexProxy.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using SIL.Utils;
using Paratext;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Paratex6Proxy represents a Paratext 6/7 project that can be accessed by a Scripture
	/// import source.
	/// ENHANCE (TimS): The remaining method (LoadProjecMappings) should somehow make it's way
	/// into ParatextHelper. The only reason it hasn't been done already is because it depends
	/// on ScrMappingList and ImportDomain which are currently defined in FDO. Those classes
	/// could, theoretically, also be moved, but I didn't feel like it. :p
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ParatextProxy : IParatextAdapter
	{
		static ParatextProxy()
		{
			try
			{
				ScrTextCollection.Initialize();
			}
			catch (Exception e)
			{
				Logger.WriteError(e);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the mappings for a Paratext 6/7 project into the specified list.
		/// </summary>
		/// <param name="project">Paratext project ID</param>
		/// <param name="mappingList">ScrMappingList to which new mappings will be added</param>
		/// <param name="domain">The import domain for which this project is the source</param>
		/// <returns><c>true</c> if the Paratext mappings were loaded successfully; <c>false</c>
		/// otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool LoadProjectMappings(string project, ScrMappingList mappingList, ImportDomain domain)
		{
			// If the new project ID is null, then do not load mappings.
			if (project == null)
				return false;

			// Load the tags from the paratext project and create mappings for them.
			ScrText scParatextText;
			try
			{
				scParatextText = ScrTextCollection.Get(project);
			}
			catch (Exception ex)
			{
				Logger.WriteError(ex);
				return false;
			}

			mappingList.ResetInUseFlags(domain);
			try
			{
				foreach (ScrTag tag in scParatextText.DefaultStylesheet.Tags)
				{
					if (tag == null)
						break;
					string marker = @"\" + tag.Marker;
					string endMarker = string.Empty;
					if (!String.IsNullOrEmpty(tag.Endmarker))
						endMarker = @"\" + tag.Endmarker;

					// When the nth marker has an end marker, the nth + 1 marker will be
					// that end marker. Therefore, we have to skip those "end style" markers.
					if (tag.StyleType == ScrStyleType.scEndStyle)
						continue;

					// Create a new mapping for this marker.
					mappingList.AddDefaultMappingIfNeeded(marker, endMarker, domain, false, false);
				}
				ScrParser parser = scParatextText.Parser();
				foreach (int bookNum in scParatextText.BooksPresentSet.SelectedBookNumbers())
				{
					foreach (UsfmToken token in parser.GetUsfmTokens(new VerseRef(bookNum, 0, 0), false, true))
					{
						if (token.Marker == null)
							continue; // Tokens alternate between text and marker types

						ImportMappingInfo mapping = mappingList[@"\" + token.Marker];
						if (mapping != null)
							mapping.SetIsInUse(domain, true);

						// ENHANCE (TE-4408): Consider Detecting markers that occur in the data but are missing
						// from the STY file. How can we write a test for this?
						//else if (ScrImportFileInfo.IsValidMarker(sMarker))
						//{
						//    mappingList.AddDefaultMappingIfNeeded(sMarker,domain, false, true);
						//}
						//else
						//{
						//    throw new ScriptureUtilsException(SUE_ErrorCode.InvalidCharacterInMarker, null, 0,
						//        sMarker + sText, new ScrReference(scParatextTextSegment.FirstReference.BBCCCVVV));
						//}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.WriteError(ex);
				return false;
			}
			return true;
		}
	}
}
