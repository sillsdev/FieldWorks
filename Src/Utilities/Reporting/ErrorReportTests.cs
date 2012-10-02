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
// File: ErrorReportTests.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Xml;
// NOTE: This test should be moved to a separate project, otherwise we require nunit.framework
// to be installed on the end user's machine.
//using NUnit.Framework;

namespace SIL.Utils
{
	/// <summary>
	/// Summary description for ErrorReportTests.
	/// </summary>
//DON't WANT THIS RUNNING AS IT REQUIRES USER INPUT	[TestFixture]
	public class ErrorReportTests
	{
		/// <summary>
		///
		/// </summary>
//		[Test]
//		public void TestNoInteraction()
//		{
//			try
//			{
//				ErrorReporter.GetReporter().OkToInteractWithUser = false;
//				throw new System.Exception("the sky is falling!");
//			}
//			catch (Exception error)
//			{
//				ErrorReporter.ReportException (error);
//			}
//		}
//				[Test]
				public void TestSend()
				{
					ErrorReporter.ReportException(new System.Exception("Testing"));
				}
	}

}
