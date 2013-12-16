// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: EmptyTePrintLayoutVc.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class EmptyTePrintLayoutVc : StVc
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the main (not very) interesting method of displaying objects and fragments
		/// of them.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
		}
	}
}
