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
// File: ImageDataDirectory.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.FieldWorks.Tools
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents the ImageDataDirectory element in a PE file
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal struct ImageDataDirectory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ImageDataDirectory"/> class.
		/// </summary>
		/// <param name="virtAddr">The virt addr.</param>
		/// <param name="size">The size.</param>
		/// <param name="index">The index of the directory</param>
		/// ------------------------------------------------------------------------------------
		public ImageDataDirectory(int virtAddr, int size, int index)
		{
			VirtualAddress = virtAddr;
			Size = size;
			Index = index;
		}

		/// <summary></summary>
		public int VirtualAddress;
		/// <summary></summary>
		public int Size;
		/// <summary></summary>
		public int Index;
	}

}
