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
// File: FdoResources.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Resources;

namespace SIL.FieldWorks.FDO
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FdoResources
	{
		private static ResourceManager s_resources =
			new ResourceManager("SIL.FieldWorks.Fdo.FdoResources",
			System.Reflection.Assembly.GetExecutingAssembly());

		private static string s_defParaChars = null;
		private static string s_defParaCharsUsage = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the resource string with the specified id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string ResourceString(string id)
		{
			return s_resources.GetString(id);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the default para chars style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string DefaultParaCharsStyleName
		{
			get
			{
				if (s_defParaChars == null)
					s_defParaChars = ResourceString("kstidDefaultParaChars");
				return s_defParaChars;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the usage information for default para chars style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string DefaultParaCharsStyleUsage
		{
			get
			{
				if (s_defParaCharsUsage == null)
					s_defParaCharsUsage = ResourceString("kstidDefaultParaCharsUsage");
				return s_defParaCharsUsage;
			}
		}
	}
}
