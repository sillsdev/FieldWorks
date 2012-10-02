// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IVwGraphicsNet.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections;
using System.Drawing.Text;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Summary description for IVwGraphicsNet.
	/// </summary>
	[GuidAttribute("8545CF33-4DB6-4908-8B4D-6FCC392D387C")]
	public interface IVwGraphicsNet
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Interface for .Net implementations of IVwGraphics classes
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="parent"></param>
		/// ------------------------------------------------------------------------------------
		void Initialize(Graphics graphics, Control parent);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Interface for .Net implementations of IVwGraphics classes
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		Bitmap GetBitmap();
	}
}
