// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LoggerTest.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for Logger.
	/// </summary>
	[TestFixture]
	public class LoggerTest
	{
		public LoggerTest()
		{
		}
		[Ignore("This test passes only when it fails! So to run, comment out this C# attribute.")]
		[Test]
		public void WriteFailTest()
		{
//			Logger log = new Logger();
//			log.WriteFail("The WriteFailTest is supposed to fail!");
		}
	}
}
