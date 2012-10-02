// Copyright (c) 2010, SIL International. All Rights Reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
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
