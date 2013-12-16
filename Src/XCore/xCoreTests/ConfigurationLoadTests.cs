// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ConfigurationLoadTests.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>

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
