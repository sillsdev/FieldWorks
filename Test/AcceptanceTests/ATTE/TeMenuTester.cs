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
// File: TeMenuTester.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
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
