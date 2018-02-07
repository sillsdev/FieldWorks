// Copyright (c) 2017-2018 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)

namespace SIL.FieldWorks.UnicodeCharEditor
{
	/// <summary>
	/// Exception that gets thrown if we can't write a file, probably because it is locked or does not exist
	/// </summary>
	public class IcuLockedException: UceException
	{
		/// <inheritdoc />
		public IcuLockedException(ErrorCodes errorCode): base(errorCode, null)
		{
		}

		/// <inheritdoc />
		public IcuLockedException(ErrorCodes errorCode, string msg): base(errorCode, msg)
		{
		}
	}
}
