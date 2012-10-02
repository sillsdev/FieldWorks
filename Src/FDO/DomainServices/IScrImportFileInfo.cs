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
// File: IScrImportFileInfo.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Information about a file in a project for importing
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IScrImportFileInfo
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of references for the file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ReferenceRange[] BookReferences { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a List of integers representing 1-based canonical book numbers that are
		/// in this file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		System.Collections.Generic.List<int> BooksInFile { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file encoding
		/// </summary>
		/// ------------------------------------------------------------------------------------
		System.Text.Encoding FileEncoding { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file name
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string FileName { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ICU locale of the source to which this file belongs (null for Scripture
		/// source)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string WsId { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether the file is readable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsReadable { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this file is still readable.
		/// </summary>
		/// <remarks>Use this property to check accessibility for the first time or to recheck
		/// accessibility of a file that was initially accessible.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		bool IsStillReadable { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the CmAnnotationDefn of the source to which this file belongs
		/// (only used for Note sources)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		SIL.FieldWorks.FDO.ICmAnnotationDefn NoteType { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs a strict scan.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void PerformStrictScan();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a reference map to a string that indicates the reference range
		/// covered by the map
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string ReferenceRangeAsString { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Re-scan files for mappings. This is mainly to enable us to look for additional
		/// P5-style in-line markers that were not detected previously when the import type is
		/// changing.
		/// </summary>
		/// <param name="scanInlineBackslashMarkers"><c>true</c> to look for backslash markers
		/// in the middle of lines. (Toolbox dictates that fields tagged with backslash markers
		/// must start on a new line, but Paratext considers all backslashes in the data to be
		/// SF markers.)</param>
		/// ------------------------------------------------------------------------------------
		void Rescan(bool scanInlineBackslashMarkers);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the starting reference for the file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		SIL.FieldWorks.Common.ScriptureUtils.ScrReference StartRef { get; }
	}
}
