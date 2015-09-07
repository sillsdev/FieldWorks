// Copyright (c) 2013-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// Exception thrown when we try to open a project that is locked
	/// </summary>
	public class FdoFileLockedException : StartupException
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FdoFileLockedException"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		/// ------------------------------------------------------------------------------------
		public FdoFileLockedException(string message)
			: base(message)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FdoFileLockedException"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="fReportToUser">True to report this error to the user, false otherwise
		/// </param>
		/// ------------------------------------------------------------------------------------
		public FdoFileLockedException(string message, bool fReportToUser) :
			base(message, fReportToUser)
		{
		}
	}
}