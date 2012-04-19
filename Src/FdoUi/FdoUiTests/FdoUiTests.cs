// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FdoUiTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------

using NUnit.Framework;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.FdoUi
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FdoUiTests : MemoryOnlyBackendProviderTestBase
	{
#if WANTTESTPORT // (FLEx) Some code isnt happy with an empty form for a wordform.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DisplayOrCreateEntry method with an empty string (TE-5916)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DisplayOrCreateEntry_EmptyString()
		{
			// Create a WfiWordform with some string.
			// We need this wordform to get a real hvo, flid, and ws.
			// Make Spanish be the vern ws.
			LgWritingSystem spanish = Cache.GetObject(Cache.WritingSystemFactory.GetWsFromStr("es")) as LgWritingSystem;
			Cache.LanguageProject.VernWssRC.Add(spanish);
			Cache.LanguageProject.CurVernWssRS.Add(spanish);
			Cache.LanguageProject.CurAnalysisWssRS.Remove(spanish);
			Cache.LanguageProject.AnalysisWssRC.Remove(spanish);
			int defVernWs = spanish.Hvo;
			ITsString form = Cache.TsStrFactory.MakeString(String.Empty, defVernWs);
			WfiWordform wf = new WfiWordform();
			Cache.LanguageProject.WordformInventoryOA.WordformsOC.Add(wf);
			wf.FormAccessor.set_String(defVernWs, form);

			// We shouldn't get an exception if we call DisplayOrCreateEntry with an empty string
			LexEntryUi.DisplayOrCreateEntry(Cache, wf.Hvo,
				(int)WfiWordform.WfiWordformTags.kflidForm, defVernWs, 0, 0,
				null, null, null, null);
		}
#endif

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
	}
}
