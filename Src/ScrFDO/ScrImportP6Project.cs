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
// File: ScrImportP6Project.cs
// Responsibility: TomB
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using Microsoft.Win32;
using System.Collections;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;
using System.Windows.Forms;

namespace SIL.FieldWorks.FDO.Scripture
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// ScrImportP6Project represents a Paratext 6 project for a Scripture import source.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ScrImportP6Project : ScrImportSource
	{
		private static string s_projectDir = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Paratext project directory
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string ProjectDir
		{
			get
			{
				if (s_projectDir == null)
				{
					RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\ScrChecks\1.0\Settings_Directory");
					if (key == null)
						s_projectDir = string.Empty;
					else
						s_projectDir = (string)key.GetValue(string.Empty);
				}
				return s_projectDir;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the mappings for a paratext project into the specified list.
		/// </summary>
		/// <param name="project">Paratext project ID</param>
		/// <param name="mappingList">ScrMappingList to which new mappings will be added</param>
		/// <param name="domain">The import domain for which this project is the source</param>
		/// <returns><c>true</c> if the Paratext mappings were loaded successfully; <c>false</c>
		/// otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public static bool LoadParatextMappings(string project, ScrMappingList mappingList,
			ImportDomain domain)
		{
			// If the new project ID is null, then do not load mappings.
			if (project == null)
				return false;

			// Load the tags from the paratext project and create mappings for them.
			SCRIPTUREOBJECTSLib.ISCScriptureText3 scParatextText = null;
			try
			{
				scParatextText = new SCRIPTUREOBJECTSLib.SCScriptureTextClass();
				scParatextText.Load(project);
			}
			catch (Exception ex)
			{
				Logger.WriteEvent(string.Format(
					"Got {0} exception loading paratext mappings (ScrImportP6Project.LoadParatextMappings):\n{1}",
					ex.GetType(), ex.Message));
				return false;
			}

			// TE-5802
			try
			{
				for (int i = 0; true; i++)
				{
					SCRIPTUREOBJECTSLib.ISCTag tag = scParatextText.NthTag(i);
					if (tag == null)
						break;
					string marker = @"\" + tag.Marker;
					string endMarker = string.Empty;
					if (tag.Endmarker != string.Empty && tag.Endmarker != null)
						endMarker = @"\" + tag.Endmarker;

					// When the nth marker has an end marker, the nth + 1 marker will be
					// that end marker. Therefore, we have to skip those "end style" markers.
					if (tag.StyleType == SCRIPTUREOBJECTSLib.SCStyleType.scEndStyle)
						continue;

					// Create a new mapping for this marker.
					mappingList.AddDefaultMappingIfNeeded(marker, endMarker, domain, false, false);
				}
				SCRIPTUREOBJECTSLib.SCReference startRefPT = new SCRIPTUREOBJECTSLib.SCReference();
				SCRIPTUREOBJECTSLib.SCReference endRefPT = new SCRIPTUREOBJECTSLib.SCReference();
				startRefPT.Parse("GEN 1:0");
				endRefPT.Parse("REV 22:21");
				SCRIPTUREOBJECTSLib.ISCTextEnum scParatextTextEnum = scParatextText.TextEnum(
					(SCRIPTUREOBJECTSLib.SCReference)startRefPT, (SCRIPTUREOBJECTSLib.SCReference)endRefPT,
					(SCRIPTUREOBJECTSLib.SCTextType)0, //scTitle | scSection | scVerseText | scNoteText | scOther)
					(SCRIPTUREOBJECTSLib.SCTextProperties)0);

				SCRIPTUREOBJECTSLib.SCTextSegment scParatextTextSegment = new SCRIPTUREOBJECTSLib.SCTextSegmentClass();
				mappingList.ResetInUseFlags(domain);

				while (scParatextTextEnum.Next(scParatextTextSegment) != 0)
				{
					string sMarker = @"\" + scParatextTextSegment.Tag.Marker;
					ImportMappingInfo mapping = mappingList[sMarker];
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
			catch (Exception ex)
			{
				Logger.WriteEvent(string.Format(
					"Got {0} exception loading paratext mappings (ScrImportP6Project.LoadParatextMappings):\n{1}",
					ex.GetType(), ex.Message));
				return false;
			}

			return true;
		}
	}
}
