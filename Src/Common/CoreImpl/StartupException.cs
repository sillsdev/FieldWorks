// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: StartupException.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Exception thrown during FieldWorks startup if something goes wrong
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class StartupException : Exception
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StartupException"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		/// ------------------------------------------------------------------------------------
		public StartupException(string message) : this(message, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StartupException"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="innerException">The inner exception.</param>
		/// ------------------------------------------------------------------------------------
		public StartupException(string message, Exception innerException) :
			this(message, innerException, true)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StartupException"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="fReportToUser">True to report this error to the user, false otherwise
		/// </param>
		/// ------------------------------------------------------------------------------------
		public StartupException(string message, bool fReportToUser) :
			this(message, null, fReportToUser)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StartupException"/> class.
		/// </summary>
		/// <param name="innerException">The inner exception.</param>
		/// ------------------------------------------------------------------------------------
		public StartupException(Exception innerException) : this(innerException, true)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StartupException"/> class.
		/// </summary>
		/// <param name="innerException">The inner exception.</param>
		/// <param name="fReportToUser">True to report this error to the user, false otherwise
		/// </param>
		/// ------------------------------------------------------------------------------------
		public StartupException(Exception innerException, bool fReportToUser) :
			this(null, innerException, fReportToUser)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StartupException"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="innerException">The inner exception.</param>
		/// <param name="fReportToUser">True to report this error to the user, false otherwise
		/// </param>
		/// ------------------------------------------------------------------------------------
		public StartupException(string message, Exception innerException, bool fReportToUser) :
			base(message, innerException)
		{
			ReportToUser = fReportToUser;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to report this error to the user or not.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ReportToUser
		{
			get;
			private set;
		}
	}
}
