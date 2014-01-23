// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TeMenuTester.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using NUnit.Framework;
using SIL.FieldWorks.AcceptanceTests.Framework;

namespace SIL.FieldWorks.AcceptanceTests.TE
{
	/// <summary>
	/// Tests of the Menu Framework Standard story (menu) for TE
	/// </summary>
	[TestFixture]
	public class TeMenuTester : MenuTester
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeMenuTester"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public TeMenuTester()
		{
		}

		/// <summary>
		/// AppInteract to use for tests.
		/// </summary>
		protected override AppInteract TestApp
		{
			get { return new TeAppInteract(); }
		}

	}
}
