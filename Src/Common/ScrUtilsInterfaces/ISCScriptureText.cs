// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ISCScriptureText.cs
// Responsibility: TE Team

using System;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface to get an enumerator for scripture text segments
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface ISCScriptureText
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return an enumerator for getting all of the text segments beginning at a
		/// specified scripture reference.
		/// </summary>
		/// <param name="firstRef">First scripture reference to start at</param>
		/// <param name="lastRef">If specified, the sequence will stop just before a verse with
		/// a greater reference is found.</param>
		/// <returns></returns>
		/// <remarks> Segments are returned in the order they appear within the
		/// file. Books and chapter numbers are guaranteed to be increasing.  Verse numbers may
		/// not always be increasing.
		/// The chapter number field of all material found before the first chapter number will
		/// be set to 0.  The verse number for all segments found before the first verse number
		/// in a chapter will be set to 0.</remarks>
		/// ------------------------------------------------------------------------------------
		ISCTextEnum TextEnum(BCVRef firstRef, BCVRef lastRef);
	};
}
