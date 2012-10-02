// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CharDef.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System.Runtime.InteropServices;
using System.Xml.Serialization;

namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a PUA character definition
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(false)]
	public struct CharDef
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor
		/// </summary>
		/// <param name="_code"></param>
		/// <param name="_data"></param>
		/// ------------------------------------------------------------------------------------
		public CharDef(int _code, string _data)
		{
			data = _data;
			// PUA character definitions in xml file need to have at least 4 digits
			// or InstallLanguage will fail.
			code = string.Format("{0:x4}", _code).ToUpper();
		}

		/// <summary></summary>
		[XmlAttribute]
		public string code;

		/// <summary></summary>
		[XmlAttribute]
		public string data;
	}
}
