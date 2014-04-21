// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IScrImportFileInfo.cs
// Responsibility: TE Team

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
		ICmAnnotationDefn NoteType { get; }

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
		SILUBS.SharedScrUtils.ScrReference StartRef { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Rechecks the accessibility of a file that might have been initially determined to
		/// be inaccessible. If the file was inaccessible but is now accessible, it will be
		/// properly initialized so all the cached info will be valid.
		/// </summary>
		/// <remarks>Use <seealso cref="IsStillReadable"/> to recheck accessibility of a file
		/// that was initially determined to be accessible. Use <seealso cref="IsReadable"/> to
		/// access the cached value.</remarks>
		/// <returns><c>true</c> if the file is currently accessible</returns>
		/// ------------------------------------------------------------------------------------
		bool RecheckAccessibility();
	}
}
