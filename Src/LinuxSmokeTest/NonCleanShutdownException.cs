// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// Original author: Tom Hindle 2010-12-30

using System;

namespace LinuxSmokeTest
{
	/// <summary>
	/// Exception that is thrown when FieldWorks Application was closed uncleanly.
	/// </summary>
	public class NonCleanShutdownException : ApplicationException
	{
		public NonCleanShutdownException() : base("Application shutdown wasn't clean")
		{
		}
	}
}
