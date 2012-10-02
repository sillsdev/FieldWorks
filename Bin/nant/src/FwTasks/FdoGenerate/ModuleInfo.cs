// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ModuleInfo.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.FieldWorks.FDO.FdoGenerate
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Additional information about the module which gets read from an XML file.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public struct ModuleInfo
	{
		/// <summary>The name of the module</summary>
		public string Name;
		/// <summary>The name of the assembly</summary>
		public string Assembly;
		/// <summary>The path to the assembly directory, relative to FDO</summary>
		public string Path;
	}
}
