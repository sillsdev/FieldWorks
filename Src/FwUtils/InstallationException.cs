// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// An InstallationException is thrown when an error is encountered
	/// that is the result of a program installation problem.  For example
	/// missing files, corrupt COM interfaces, other nasty things.
	/// </summary>
	public class InstallationException : Exception
	{
		/// <summary />
		public InstallationException() : base(null)
		{
		}

		/// <summary />
		public InstallationException(Exception e) :
			base(ResourceHelper.GetResourceString("kstidInvalidInstallation"), e)
		{
		}

		/// <summary />
		public InstallationException(string message, Exception e) :
			base(message, e)
		{
		}
	}
}