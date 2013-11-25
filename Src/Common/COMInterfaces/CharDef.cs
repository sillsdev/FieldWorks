// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CharDef.cs
// Responsibility: TE Team

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
