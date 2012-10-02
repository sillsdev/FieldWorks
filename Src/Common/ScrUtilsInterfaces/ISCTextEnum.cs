// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ISCTextEnum.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
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
