// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeDialogResources.cs
// Responsibility: TE team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
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
