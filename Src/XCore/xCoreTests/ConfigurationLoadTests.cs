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
// File: ConfigurationLoadTests.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

using NUnit.Framework;

namespace XCore
{

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test XWindow methods.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ConfigurationLoadTests  : XWindowTestsBase
	{

		protected override string TestFile
		{
			get { return "includeTest.xml"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test using XmlIncluder to factor parts of the configuration out to other files
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]//[Ignore("Temporary for the sake of NUnit 2.2")]
		public void ConfigurationInclude()
		{
			ITestableUIAdapter adapter = (ITestableUIAdapter) m_window.MenuAdapter;
			Assert.IsTrue(adapter.HasItem("DebugMenu", "ClearFields"));

		}
	}
}
