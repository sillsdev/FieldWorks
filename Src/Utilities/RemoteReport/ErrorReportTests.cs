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
using NUnit.Framework;

namespace SIL.Utils
{
	/// <summary>
	/// Summary description for ErrorReportTests.
	/// </summary>
	[TestFixture]
	public class ErrorReportTests
	{
		public ErrorReportTests()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void Test()
		{
			using (ErrorReport report = new ErrorReport())
			{
				report.ShowDialog();
			}
		}
	}
}
