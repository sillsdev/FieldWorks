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
// File: IParatextAdapter.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using SIL.FieldWorks.FDO;
using System.Collections.Generic;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// IParatex6Proxy defines an interface for objects which represent a Paratext 6/7 project
	/// that can be accessed by a Scripture import source.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IParatextAdapter
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Paratext project directory
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string ProjectDir { get; }

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
		bool LoadProjectMappings(string project,
			ScrMappingList mappingList, ImportDomain domain);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a list of book IDs that exist for the given Paratext project.
		/// </summary>
		/// <returns>A List of integers representing 1-based canonical book numbers that exist
		/// in any source represented by these import settings</returns>
		/// <remark>The returned list will be empty if there is a problem with the Paratext
		/// installation.</remark>
		/// ------------------------------------------------------------------------------------
		List<int> GetProjectBooks(string projectId);
	}
}
