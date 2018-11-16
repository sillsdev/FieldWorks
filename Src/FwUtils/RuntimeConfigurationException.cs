// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Use this exception when the format of the configuration XML
	/// may be fine, but there is a run-time linking problem with an assembly or class that was specified.
	/// </summary>
	public class RuntimeConfigurationException : ApplicationException
	{
		public RuntimeConfigurationException(string message) :base(message)
		{
		}

		/// <summary>
		/// Use this one if you are inside of a catch block where you have access to the original exception
		/// </summary>
		public RuntimeConfigurationException(string message, Exception innerException) :base(message, innerException)
		{
		}
	}
}