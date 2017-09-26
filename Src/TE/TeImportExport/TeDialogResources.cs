// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TeDialogResources.cs
// Responsibility: TE team
//
// <remarks>
// </remarks>

using System;
using System.Resources;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for DlgResources.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DlgResources
	{
		private static ResourceManager m_resources =
			new ResourceManager("SIL.FieldWorks.TE.TeDialogStrings",
			System.Reflection.Assembly.GetExecutingAssembly());

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the resource string from DialogResources.resx associated with the specified id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string ResourceString(string id)
		{
			return m_resources.GetString(id);
		}
	}
}
