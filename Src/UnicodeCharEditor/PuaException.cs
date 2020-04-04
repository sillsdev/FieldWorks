// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.UnicodeCharEditor
{
	///<summary />
	internal sealed class PuaException : UceException
	{
		///<summary />
		internal PuaException(ErrorCodes errorCode, string msg) : base(errorCode, msg)
		{
		}
	}
}