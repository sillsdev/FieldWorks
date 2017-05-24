// Copyright (c) 2013-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.LCModel
{
	/// <summary>
	/// Exception thrown when we try to open a project that is locked
	/// </summary>
	public class LcmFileLockedException : LcmInitializationException
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LcmFileLockedException"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		/// ------------------------------------------------------------------------------------
		public LcmFileLockedException(string message)
			: base(message)
		{
		}
	}
}