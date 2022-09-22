// Copyright (c) 2021-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;

namespace SIL.FieldWorks.FwCoreDlgs
{
	[TestFixture]
	internal class ViewHiddenWritingSystemsModelTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		[Test]
		public void ViewHiddenWritingSystemsModel_ShowsOnlyHiddenWSs()
		{
			// set up languages (in addition to English): one in both lists, one unique to each list, and one in neither list
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("en", out var wsEn);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("de", out var wsBoth);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out var wsAnal);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("fr", out var wsVern);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("pt", out var wsNeither);
			Cache.LangProject.AnalysisWritingSystems.Add(wsAnal);
			Cache.LangProject.AnalysisWritingSystems.Add(wsBoth);
			Cache.LangProject.VernacularWritingSystems.Add(wsVern);
			Cache.LangProject.VernacularWritingSystems.Add(wsBoth);
			// set up data in each language
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry.CitationForm.set_String(wsVern.Handle, "Citation");
			entry.CitationForm.set_String(wsBoth.Handle, "Citation");
			entry.SummaryDefinition.set_String(wsEn.Handle, "Definition");
			entry.SummaryDefinition.set_String(wsAnal.Handle, "Definition");
			entry.SummaryDefinition.set_String(wsBoth.Handle, "Definition");
			entry.SummaryDefinition.set_String(wsNeither.Handle, "Definition");
			Cache.ActionHandlerAccessor.EndUndoTask();

			// SUT
			var testModel = new ViewHiddenWritingSystemsModel(FwWritingSystemSetupModel.ListType.Analysis, Cache);
			Assert.That(testModel.Items.Select(i => i.WS.Id), Is.EquivalentTo(new[] { "fr", "pt" }));

			// SUT
			testModel = new ViewHiddenWritingSystemsModel(FwWritingSystemSetupModel.ListType.Vernacular, Cache);
			Assert.That(testModel.Items.Select(i => i.WS.Id), Is.EquivalentTo(new[] { "en", "es", "pt" }));
		}

		[Test]
		public void ViewHiddenWritingSystemsModel_PrefersOverrideExistingWSsList()
		{
			// set up languages (in addition to English): one in both lists, one unique to each list, and one in neither list
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("en", out var wsEn);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("de", out var wsBoth);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out var wsAnal);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("fr", out var wsVern);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("pt", out var wsNeither);
			Cache.LangProject.AnalysisWritingSystems.Add(wsNeither);
			Cache.LangProject.VernacularWritingSystems.Add(wsNeither);
			// set up data in each language
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry.CitationForm.set_String(wsVern.Handle, "Citation");
			entry.CitationForm.set_String(wsBoth.Handle, "Citation");
			entry.SummaryDefinition.set_String(wsEn.Handle, "Definition");
			entry.SummaryDefinition.set_String(wsAnal.Handle, "Definition");
			entry.SummaryDefinition.set_String(wsBoth.Handle, "Definition");
			entry.SummaryDefinition.set_String(wsNeither.Handle, "Definition");
			Cache.ActionHandlerAccessor.EndUndoTask();

			// SUT
			var testModel = new ViewHiddenWritingSystemsModel(FwWritingSystemSetupModel.ListType.Analysis, Cache, new[] { wsBoth, wsAnal });
			Assert.That(testModel.Items.Select(i => i.WS.Id), Is.EquivalentTo(new[] { "en", "fr", "pt" }));

			// SUT
			testModel = new ViewHiddenWritingSystemsModel(FwWritingSystemSetupModel.ListType.Vernacular, Cache, new[] { wsEn, wsBoth, wsVern });
			Assert.That(testModel.Items.Select(i => i.WS.Id), Is.EquivalentTo(new[] { "es", "pt" }));
		}

		[Test]
		public void ListItem_FormatDisplayLabel([Values("en", "fr-CA", "el-Latn")] string id)
		{
			Cache.ServiceLocator.WritingSystemManager.GetOrSet(id, out var ws);
			var testModel = new ViewHiddenWritingSystemsModel(FwWritingSystemSetupModel.ListType.Analysis, Cache);
			Assert.AreEqual($"[{ws.Abbreviation}] {ws.DisplayLabel}", testModel.IntToListItem(ws.Handle).FormatDisplayLabel(null));
		}

		[Test]
		public void ListItem_FormatDisplayLabel_IncludesTags()
		{
			var ws = GetOrSetWs("en-CA");
			var wsAbbrAndLabel = $"[{ws.Abbreviation}] {ws.DisplayLabel}";
			Assert.AreEqual(string.Format(FwCoreDlgs.XWillBeAdded, wsAbbrAndLabel),
				new HiddenWSListItemModel(ws, false) { WillAdd = true }.FormatDisplayLabel(null));
			Assert.AreEqual(string.Format(FwCoreDlgs.XWillBeDeleted, wsAbbrAndLabel),
				new HiddenWSListItemModel(ws, false) { WillDelete = true }.FormatDisplayLabel(null));
			Assert.AreEqual(string.Format(FwCoreDlgs.XInTheXList, wsAbbrAndLabel, "Analysis"),
				new HiddenWSListItemModel(ws, true).FormatDisplayLabel("Analysis"));
			Assert.AreEqual(string.Format(FwCoreDlgs.XWillBeAdded, string.Format(FwCoreDlgs.XInTheXList, wsAbbrAndLabel, "Vernacular")),
				new HiddenWSListItemModel(ws, true) { WillAdd = true }.FormatDisplayLabel("Vernacular"));
		}

		[Test]
		public void IntToListItem_InOppositeList()
		{
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("en", out var wsEn);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("fr", out var wsFr);

			var testModel = new ViewHiddenWritingSystemsModel(FwWritingSystemSetupModel.ListType.Analysis, Cache);
			Assert.True(testModel.IntToListItem(wsFr.Handle).InOppositeList, "French is in the Vernacular list");
			Assert.False(testModel.IntToListItem(wsEn.Handle).InOppositeList, "English is not in the Vernacular list");

			testModel = new ViewHiddenWritingSystemsModel(FwWritingSystemSetupModel.ListType.Vernacular, Cache);
			Assert.False(testModel.IntToListItem(wsFr.Handle).InOppositeList, "French is not in the Analysis list");
			Assert.True(testModel.IntToListItem(wsEn.Handle).InOppositeList, "English is in the Analysis list");
		}

		[Test]
		public void Add()
		{
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("hid-x-will-add", out var wsHid);
			var itemHid = new HiddenWSListItemModel(wsHid, true);
			var itemEn = new HiddenWSListItemModel(Cache.LangProject.AnalysisWritingSystems.First(), true);
			var testModel = new ViewHiddenWritingSystemsModel(FwWritingSystemSetupModel.ListType.Analysis);
			testModel.Items.Add(itemHid);
			testModel.Items.Add(itemEn);

			// SUT
			testModel.Add(itemHid);

			Assert.That(testModel.DeletedWritingSystems, Is.Empty, "No WS's should be in the list to be deleted");
			Assert.That(testModel.AddedWritingSystems, Is.EquivalentTo(new[] { wsHid }), "The hidden WS should be in the list to be added");
		}

		[Test]
		public void Delete()
		{
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("hid", out var wsHid);
			var itemHid = new HiddenWSListItemModel(wsHid, false);
			var itemEn = new HiddenWSListItemModel(Cache.LangProject.AnalysisWritingSystems.First(), false);
			string confirmDeleteLabel = null;
			var testModel = new ViewHiddenWritingSystemsModel(FwWritingSystemSetupModel.ListType.Analysis)
			{
				ConfirmDeleteWritingSystem = label =>
				{
					confirmDeleteLabel = label;
					return true;
				}
			};
			testModel.Items.Add(itemHid);
			testModel.Items.Add(itemEn);

			// SUT
			testModel.Delete(itemHid);

			Assert.That(confirmDeleteLabel, Does.EndWith(wsHid.DisplayLabel));
			Assert.That(testModel.DeletedWritingSystems, Is.EquivalentTo(new[] { wsHid }), "The hidden WS should be in the list to be deleted");
			Assert.That(testModel.AddedWritingSystems, Is.Empty, "Nothing should be in the list to be added");
		}

		[Test]
		public void Delete_UserRepents_NothingHappens()
		{
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("hid", out var wsHid);
			var itemHid = new HiddenWSListItemModel(wsHid, false);
			var itemEn = new HiddenWSListItemModel(Cache.LangProject.AnalysisWritingSystems.First(), false);
			string confirmDeleteLabel = null;
			var testModel = new ViewHiddenWritingSystemsModel(FwWritingSystemSetupModel.ListType.Analysis)
			{
				ConfirmDeleteWritingSystem = label =>
				{
					confirmDeleteLabel = label;
					return false;
				}
			};
			testModel.Items.Add(itemHid);
			testModel.Items.Add(itemEn);

			// SUT
			testModel.Delete(itemHid);

			Assert.That(confirmDeleteLabel, Does.EndWith(wsHid.DisplayLabel));
			Assert.That(testModel.Items, Is.EquivalentTo(new[] { itemEn, itemHid }), "Both WS's remain visible in the dialog");
			Assert.That(testModel.DeletedWritingSystems, Is.Empty, "Nothing should be in the list to be deleted");
			Assert.That(testModel.AddedWritingSystems, Is.Empty, "Nothing should be in the list to be added");
		}

		[Test]
		public void Delete_WSInOppositeList_NothingHappens()
		{
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("hid", out var wsHid);
			var itemHid = new HiddenWSListItemModel(wsHid, true);
			var itemEn = new HiddenWSListItemModel(Cache.LangProject.AnalysisWritingSystems.First(), false);
			var testModel = new ViewHiddenWritingSystemsModel(FwWritingSystemSetupModel.ListType.Analysis);
			testModel.Items.Add(itemHid);
			testModel.Items.Add(itemEn);

			// SUT
			testModel.Delete(itemHid);

			Assert.That(testModel.Items, Is.EquivalentTo(new[] { itemEn, itemHid }), "Both WS's remain visible in the dialog");
			Assert.That(testModel.DeletedWritingSystems, Is.Empty, "Nothing should be in the list to be deleted");
			Assert.That(testModel.AddedWritingSystems, Is.Empty, "Nothing should be in the list to be added");
		}

		private CoreWritingSystemDefinition GetOrSetWs(string code)
		{
			Cache.ServiceLocator.WritingSystemManager.GetOrSet(code, out var ws);
			return ws;
		}
	}
}