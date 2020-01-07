// Copyright (c) 2007-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.LcmUi;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LanguageExplorerTests.LcmUi
{
	/// <summary />
	[TestFixture]
	public class LcmUiTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Tests FindEntryForWordform with empty string (related to TE-5916)
		/// </summary>
		[Test]
		public void FindEntryForWordform_EmptyString()
		{
			using (var lexEntryUi = LexEntryUi.FindEntryForWordform(Cache, TsStringUtils.EmptyString(Cache.DefaultVernWs)))
			{
				Assert.IsNull(lexEntryUi);
			}
		}

		/// <summary>
		/// Tests FindEntryForWordform to make sure it finds matches regardless of case.
		/// </summary>
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
			var entry1 = servLoc.GetInstance<ILexEntryFactory>().Create("Uppercaseword", "Uppercasegloss", new SandboxGenericMSA());
			var entry2 = servLoc.GetInstance<ILexEntryFactory>().Create("lowercaseword", "lowercasegloss", new SandboxGenericMSA());

			// SUT
			// First make sure it works with the same case
			using (var lexEntryUi = LexEntryUi.FindEntryForWordform(Cache, TsStringUtils.MakeString("Uppercaseword", Cache.DefaultVernWs)))
			{
				Assert.IsNotNull(lexEntryUi);
				Assert.AreEqual(entry1.Hvo, lexEntryUi.MyCmObject.Hvo, "Found wrong object");
			}
			using (var lexEntryUi = LexEntryUi.FindEntryForWordform(Cache, TsStringUtils.MakeString("lowercaseword", Cache.DefaultVernWs)))
			{
				Assert.IsNotNull(lexEntryUi);
				Assert.AreEqual(entry2.Hvo, lexEntryUi.MyCmObject.Hvo, "Found wrong object");
			}
			// Now make sure it works with the wrong case
			using (var lexEntryUi = LexEntryUi.FindEntryForWordform(Cache, TsStringUtils.MakeString("uppercaseword", Cache.DefaultVernWs)))
			{
				Assert.IsNotNull(lexEntryUi);
				Assert.AreEqual(entry1.Hvo, lexEntryUi.MyCmObject.Hvo, "Found wrong object");
			}
			using (var lexEntryUi = LexEntryUi.FindEntryForWordform(Cache, TsStringUtils.MakeString("LowerCASEword", Cache.DefaultVernWs)))
			{
				Assert.IsNotNull(lexEntryUi);
				Assert.AreEqual(entry2.Hvo, lexEntryUi.MyCmObject.Hvo, "Found wrong object");
			}
		}

		/// <summary>
		/// Tests that the DeleteUnderlyingObjectMethod actually deletes the object, regardless of what happens in related clean up
		/// </summary>
		[Test]
		public void DeleteCmPictureObject_RelatedCleanUpDoesNotNegateDeletion()
		{
			var obj = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			using (var objectUi = new DummyCmObjectUi(obj))
			{
				Assert.IsTrue(obj.IsValidObject);
				objectUi.SimulateReallyDeleteUnderlyingObject(); // Call ReallyDeleteUnderlyingObject() in CmObjectUi
				Assert.IsFalse(obj.IsValidObject);
			}
		}

		/// <summary>
		/// Dummy class used from testing CmObjectUi
		/// </summary>
		private sealed class DummyCmObjectUi : CmObjectUi
		{
			/// <summary />
			internal DummyCmObjectUi(ICmObject obj)
				: base(obj)
			{
				m_hvo = obj.Hvo;
			}

			/// <summary>
			/// Ignores the related clean up for testing purposes
			/// </summary>
			protected override void DoRelatedCleanupForDeleteObject()
			{
				// skip this method in CmObjectUi class because it invokes message boxes
			}

			internal void SimulateReallyDeleteUnderlyingObject()
			{
				ReallyDeleteUnderlyingObject();
			}
		}
	}
}