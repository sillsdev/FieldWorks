// Copyright (c) 2017 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Text;

namespace SIL.FieldWorks.UnicodeCharEditor
{
	/// <summary>
	/// Exception that gets thrown if we can't write a file, probably because it is locked
	/// </summary>
	public class IcuLockedException: UceException
	{
		/// <summary>
		/// Default c'tor
		/// </summary>
		public IcuLockedException(ErrorCodes errorCode): base(errorCode, null)
		{
		}

		///<summary>
		/// Constructor with a message.
		///</summary>
		public IcuLockedException(ErrorCodes errorCode, string msg): base(errorCode, msg)
		{
		}
	}
}
