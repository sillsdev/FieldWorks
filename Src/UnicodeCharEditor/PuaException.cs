// Copyright (c) 2010-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Text;

namespace SIL.FieldWorks.UnicodeCharEditor
{
	///<summary>
	///</summary>
	public class PuaException : UceException
	{
		///<summary>
		/// Constructor without a message.
		///</summary>
		public PuaException(ErrorCodes errorCode): base(errorCode, null)
		{
		}

		///<summary>
		/// Constructor with a message.
		///</summary>
		public PuaException(ErrorCodes errorCode, string msg): base(errorCode, msg)
		{
		}
	}
}
