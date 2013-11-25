// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IVwGraphicsNet.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections;
using System.Drawing.Text;

using SIL.FieldWorks.Common.COMInterfaces;

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
