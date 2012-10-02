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
// File: Paratex6Proxy.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using Microsoft.Win32;
using System.Collections;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;
using System.Windows.Forms;
using System.Collections.Generic;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Paratex6Proxy represents a Paratext 6/7 project that can be accessed by a Scripture
	/// import source.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class Paratext6Proxy : IParatextAdapter
	{
		private static string s_projectDir = null;
		private readonly ThreadHelper m_threadHelper;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Paratext6Proxy"/> class.
		/// </summary>
		/// <param name="threadHelper">The thread helper to invoke actions on the main UI thread
		/// </param>
		/// ------------------------------------------------------------------------------------
		public Paratext6Proxy(ThreadHelper threadHelper)
		{
			m_threadHelper = threadHelper;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Paratext 6/7 project directory
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ProjectDir
		{
			get
			{
				if (s_projectDir == null)
				{
					using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\ScrChecks\1.0\Settings_Directory"))
					s_projectDir = (key == null) ? string.Empty : (string)key.GetValue(string.Empty);
				}
				return s_projectDir;
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
		public bool LoadProjectMappings(string project,
			ScrMappingList mappingList, ImportDomain domain)
		{
			// If the new project ID is null, then do not load mappings.
			if (project == null)
				return false;

			return m_threadHelper.Invoke(() =>
			{
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
					"Got {0} exception loading Paratext project (ScrImportP6Project.LoadParatextMappings):{2}{1}",
					ex.GetType(), ex.Message, Environment.NewLine));
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
					if (!String.IsNullOrEmpty(tag.Endmarker))
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
						startRefPT, endRefPT,
						0, //scTitle | scSection | scVerseText | scNoteText | scOther)
						0);

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
					"Got {0} exception loading Paratext mappings (ScrImportP6Project.LoadParatextMappings):{2}{1}",
					ex.GetType(), ex.Message, Environment.NewLine));
				return false;
			}

			return true;
			});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a list of book IDs that exist for the given Paratext project.
		/// </summary>
		/// <returns>A List of integers representing 1-based canonical book numbers that exist
		/// in any source represented by these import settings</returns>
		/// <remark>The returned list will be empty if there is a problem with the Paratext
		/// installation.</remark>
		/// ------------------------------------------------------------------------------------
		public List<int> GetProjectBooks(string projectId)
		{
			List<int> booksPresent = new List<int>();
			m_threadHelper.Invoke(() =>
			{
			try
			{
				SCRIPTUREOBJECTSLib.ISCScriptureText3 scParatextText = new SCRIPTUREOBJECTSLib.SCScriptureTextClass();
				scParatextText.Load(projectId);
				string stringList = scParatextText.BooksPresent;

				for (int i = 0; i < BCVRef.LastBook && i < stringList.Length; i++)
				{
					if (stringList[i] == '1')
						booksPresent.Add(i + 1);
				}

			}
			catch
			{
				// ignore error - probably Paratext installation problem. Caller can check number
				// of books present.
			}
			});

			return booksPresent;
		}
	}
}
