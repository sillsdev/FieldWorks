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
// File: TeTestsBase.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using Microsoft.Win32;
using NUnit.Framework;
using SIL.FieldWorks.AcceptanceTests.Framework;

namespace SIL.FieldWorks.AcceptanceTests.TE
{
	/// <summary>
	/// Base class for TE acceptance tests. Starts and exits TE at start/end of each test.
	/// </summary>
	public class TeTestsBase
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeTestsBase"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public TeTestsBase(bool fStartAppOnce)
		{
			m_fStartAppOnce = fStartAppOnce;
		}

		/// <summary>Application to test</summary>
		protected TeAppInteract m_app;

		/// <summary><c>true</c> to start application in FixtureSetUp, otherwise start
		/// app for each test in SetUp.</summary>
		protected bool m_fStartAppOnce;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start the TE application
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public virtual void FixtureInit()
		{
			// set current view to DraftView in registry
//			RegistryKey key = Registry.CurrentUser.OpenSubKey(
//				@"Software\SIL\Fieldworks\Translation Editor", true);
//			key.SetValue("sideBarFwViewTabState", 2);
//			key.Close();

			if (m_fStartAppOnce)
			{
				m_app = new TeAppInteract();
				m_app.Start();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close the TE application
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public virtual void FixtureShutdown()
		{
			if (m_fStartAppOnce)
				m_app.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start the TE application
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public virtual void Init()
		{
			if (!m_fStartAppOnce)
			{
				m_app = new TeAppInteract();
				m_app.Start();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close the TE application
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public virtual void Shutdown()
		{
			if (!m_fStartAppOnce)
				m_app.Exit();
		}
	}
}
