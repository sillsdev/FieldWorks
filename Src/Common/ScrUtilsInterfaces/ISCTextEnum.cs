// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ISCTextEnum.cs
// Responsibility: TE Team

using System;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ISCTextEnum.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface ISCTextEnum
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the next text segment in the Scripture text object.
		/// </summary>
		/// <returns>null when there's no more segments</returns>
		/// ------------------------------------------------------------------------------------
		ISCTextSegment Next();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the current writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int CurrentWs { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the current note type definition
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int CurrentNoteTypeHvo { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform any necessary cleanup when done
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void Cleanup();
	}
}
