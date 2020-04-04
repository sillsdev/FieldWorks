// Copyright (c) 2017-2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)

namespace SIL.FieldWorks.UnicodeCharEditor
{
	/// <summary>
	/// Exception that gets thrown if we can't write a file, probably because it is locked or does not exist
	/// </summary>
	internal sealed class IcuLockedException : UceException
	{
		/// <inheritdoc />
		internal IcuLockedException(ErrorCodes errorCode, string msg) : base(errorCode, msg)
		{
		}
	}
}