// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FdoUiTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------

using System;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.FdoUi
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FdoUiTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FindEntryForWordform with empty string (related to TE-5916)
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindEntryForWordform_EmptyString()
		{
			using (var lexEntryUi = LexEntryUi.FindEntryForWordform(Cache,
				Cache.TsStrFactory.MakeString(string.Empty, Cache.DefaultVernWs)))
			{
				Assert.IsNull(lexEntryUi);
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FindEntryForWordform to make sure it finds matches regardless of case.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindEntryNotMatchingCase()
		{
			// Setup
			var servLoc = Cache.ServiceLocator;
			var langProj = Cache.LangProject;
			var lexDb = langProj.LexDbOA;
			// Create a WfiWordform with some string.
			// We need this wordform to get a real hvo, flid, and ws.
			// Make Spanish be the vern ws.
			var spanish = servLoc.WritingSystemManager.Get("es");
			langProj.AddToCurrentVernacularWritingSystems(spanish);
			langProj.CurAnalysisWss = "en";
			langProj.DefaultVernacularWritingSystem = spanish;
			var defVernWs = spanish.Handle;
			var entry1 = servLoc.GetInstance<ILexEntryFactory>().Create(
				"Uppercaseword", "Uppercasegloss", new SandboxGenericMSA());
			var entry2 = servLoc.GetInstance<ILexEntryFactory>().Create(
				"lowercaseword", "lowercasegloss", new SandboxGenericMSA());

			// SUT
			// First make sure it works with the same case
			using (var lexEntryUi = LexEntryUi.FindEntryForWordform(Cache,
				Cache.TsStrFactory.MakeString("Uppercaseword", Cache.DefaultVernWs)))
			{
				Assert.IsNotNull(lexEntryUi);
				Assert.AreEqual(entry1.Hvo, lexEntryUi.Object.Hvo, "Found wrong object");
			}
			using (var lexEntryUi = LexEntryUi.FindEntryForWordform(Cache,
				Cache.TsStrFactory.MakeString("lowercaseword", Cache.DefaultVernWs)))
			{
				Assert.IsNotNull(lexEntryUi);
				Assert.AreEqual(entry2.Hvo, lexEntryUi.Object.Hvo, "Found wrong object");
			}
			// Now make sure it works with the wrong case
			using (var lexEntryUi = LexEntryUi.FindEntryForWordform(Cache,
				Cache.TsStrFactory.MakeString("uppercaseword", Cache.DefaultVernWs)))
			{
				Assert.IsNotNull(lexEntryUi);
				Assert.AreEqual(entry1.Hvo, lexEntryUi.Object.Hvo, "Found wrong object");
			}
			using (var lexEntryUi = LexEntryUi.FindEntryForWordform(Cache,
				Cache.TsStrFactory.MakeString("LowerCASEword", Cache.DefaultVernWs)))
			{
				Assert.IsNotNull(lexEntryUi);
				Assert.AreEqual(entry2.Hvo, lexEntryUi.Object.Hvo, "Found wrong object");
			}
		}
	}
}
