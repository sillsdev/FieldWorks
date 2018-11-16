// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Exception thrown during FieldWorks startup if something goes wrong
	/// </summary>
	public class StartupException : Exception
	{
		/// <summary />
		public StartupException(string message) : this(message, null)
		{
		}

		/// <summary />
		public StartupException(string message, Exception innerException) :
			this(message, innerException, true)
		{
		}

		/// <summary />
		public StartupException(string message, bool fReportToUser) :
			this(message, null, fReportToUser)
		{
		}

		/// <summary />
		public StartupException(string message, Exception innerException, bool fReportToUser) :
			base(message, innerException)
		{
			ReportToUser = fReportToUser;
		}

		/// <summary>
		/// Gets or sets a value indicating whether to report this error to the user or not.
		/// </summary>
		public bool ReportToUser { get; }
	}
}