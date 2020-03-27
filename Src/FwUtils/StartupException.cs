// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Exception thrown during FieldWorks startup if something goes wrong
	/// </summary>
	internal sealed class StartupException : Exception
	{
		/// <summary />
		internal StartupException(string message) : this(message, null)
		{
		}

		/// <summary />
		internal StartupException(string message, Exception innerException) :
			this(message, innerException, true)
		{
		}

		/// <summary />
		internal StartupException(string message, bool fReportToUser) :
			this(message, null, fReportToUser)
		{
		}

		/// <summary />
		internal StartupException(string message, Exception innerException, bool fReportToUser) :
			base(message, innerException)
		{
			ReportToUser = fReportToUser;
		}

		/// <summary>
		/// Gets a value indicating whether to report this error to the user or not.
		/// </summary>
		internal bool ReportToUser { get; }
	}
}