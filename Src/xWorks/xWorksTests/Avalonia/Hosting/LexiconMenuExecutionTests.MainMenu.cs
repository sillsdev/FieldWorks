// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The Insert menu's and pane bar's inserts and conversions in Lexicon Edit, and the View
	/// menu's toggles, answered by the view's command authority: no hidden tree is built and
	/// the mediator is never asked. Commands act on the current row, else the shown entry.
	/// </summary>
	public partial class LexiconMenuExecutionTests
	{
		private static readonly string[] s_lexiconMainMenuCommandIds =
		{
			"CmdInsertSense", "CmdInsertSubsense", "CmdInsertVariant", "CmdDataTree-Insert-AlternateForm",
			"CmdDataTree-Insert-Pronunciation", "CmdInsertMediaFile", "CmdDataTree-Insert-Etymology", "CmdInsertPicture",
			"CmdInsertExtNote", "CmdChangeToComplexForm", "CmdChangeToVariant"
		};

		[Test]
		public void TheCommandAuthority_AnswersTheInsertAndConversionMessages_AndTheViewToggles()
		{
			var authority = m_view.CreateCommandAuthority();

			Assert.That(authority.AnsweredMessages, Is.EquivalentTo(new[]
			{
				RecordEditView.InsertMessage, RecordEditView.InsertMediaFileMessage, RecordEditView.InsertPictureMessage,
				RecordEditView.InsertVariantOfShownEntryMessage, RecordEditView.ConvertIntoComplexFormMessage,
				RecordEditView.ConvertIntoVariantMessage
			}));
			Assert.That(authority.AnsweredProperties, Is.EquivalentTo(new[]
			{
				PropertyConstants.ShowHiddenFields, RecordEditView.ShowDictionaryPreviewProperty
			}));
			Assert.That(CommandAuthorities.Of(m_propertyTable), Is.Not.Null, "the active view registers its authority");
		}

		[Test]
		public void SwitchingToTheLegacyUI_LeavesTheCommandsToTheMediator()
		{
			var display = new UIItemDisplayProperties(null, "Sense", false, null, false);
			Assert.That(CommandAuthorities.Of(m_propertyTable).UpdateDisplay(CommandOf("CmdInsertSense"), display), Is.True);

			m_propertyTable.SetProperty("UIMode", "Legacy", true);
			DrainMediatorAndIdleQueues();

			Assert.That(CommandAuthorities.Of(m_propertyTable).UpdateDisplay(CommandOf("CmdInsertSense"), display), Is.False,
				"the view unregisters with its Avalonia detail view");
		}

		[Test]
		public void EveryInsertAndConversion_IsShown_WithoutTheHiddenTreeOrTheMediator()
		{
			var spy = new MainMenuSpyColleague();
			Window.Mediator.AddColleague(spy);
			try
			{
				foreach (var id in s_lexiconMainMenuCommandIds)
				{
					var display = new UIItemDisplayProperties(null, id, false, null, false);
					Assert.That(CommandAuthorities.Of(m_propertyTable).UpdateDisplay(CommandOf(id), display), Is.True,
						"{0} is answered natively", id);
				}
				Assert.That(PaneBarItem("CmdChangeToVariant").GetDisplayProperties().Enabled, Is.True);
				Assert.That(InsertMenuItem("CmdInsertSense").GetDisplayProperties().Enabled, Is.True);
			}
			finally
			{
				Window.Mediator.RemoveColleague(spy);
			}

			Assert.That(spy.Asked, Is.Empty, "the mediator is never asked");
			Assert.That(HiddenTreeBuilt, Is.False);
		}

		[Test]
		public void WithNoCurrentRow_TheEntryCommandsAreEnabled_AndTheSenseOnesAreNot()
		{
			m_view.CurrentRowSource = () => null;

			Assert.That(EnabledMainMenuCommands(), Is.EquivalentTo(new[]
			{
				"CmdInsertSense", "CmdInsertVariant", "CmdDataTree-Insert-AlternateForm", "CmdDataTree-Insert-Pronunciation",
				"CmdInsertMediaFile", "CmdDataTree-Insert-Etymology", "CmdChangeToVariant"
			}), "the entry is already a complex form, so it cannot become one");
			Assert.That(m_view.MainMenuRow().ObjectHvo, Is.EqualTo(m_entry.Hvo), "the commands act on the shown entry");
		}

		[Test]
		public void TheSenseInserts_NeedTheCurrentRowsObjectToBeASense()
		{
			m_view.CurrentRowSource = () => HeaderRowOf(m_sense1);
			Assert.That(EnabledMainMenuCommands(), Is.SupersetOf(new[] { "CmdInsertSubsense", "CmdInsertPicture", "CmdInsertExtNote" }));

			m_view.CurrentRowSource = () => Rows(m_entry).First(f => f.ObjectHvo == m_example.Hvo && f.Kind != DetailFieldKind.Header);
			var enabled = EnabledMainMenuCommands();
			Assert.That(enabled, Does.Not.Contain("CmdInsertSubsense").And.Not.Contain("CmdInsertPicture")
				.And.Not.Contain("CmdInsertExtNote"), "an example's row is no sense's own row");
			Assert.That(enabled, Does.Contain("CmdInsertSense"));
		}

		[Test]
		public void InsertSense_FromTheInsertMenu_GoesAfterTheCurrentRowsSense_ElseLast()
		{
			m_view.CurrentRowSource = () => HeaderRowOf(m_sense1);
			var spy = new MainMenuSpyColleague();
			Window.Mediator.AddColleague(spy);
			try
			{
				InsertMenuItem("CmdInsertSense").OnClick(null, null);
				DrainMediatorAndIdleQueues();
			}
			finally
			{
				Window.Mediator.RemoveColleague(spy);
			}

			Assert.That(m_entry.SensesOS, Has.Count.EqualTo(3));
			Assert.That(m_entry.SensesOS[0], Is.SameAs(m_sense1));
			Assert.That(m_entry.SensesOS[2], Is.SameAs(m_sense2), "the new sense goes right after the current one");
			Assert.That(spy.Asked, Is.Empty, "the click never reaches the mediator");
			Assert.That(HiddenTreeBuilt, Is.False);

			m_view.CurrentRowSource = () => null;
			RunMainMenuCommand("CmdInsertSense");

			Assert.That(m_entry.SensesOS, Has.Count.EqualTo(4));
			Assert.That(m_entry.SensesOS[2], Is.SameAs(m_sense2), "with no current row the sense goes last");
		}

		[Test]
		public void AVariantsRow_CountsAsNoCurrentRow()
		{
			m_view.CurrentRowSource = VariantFormRow;
			Assert.That(m_view.MainMenuRow().ObjectHvo, Is.EqualTo(m_entry.Hvo), "the variant's rows belong to another entry");

			RunMainMenuCommand("CmdDataTree-Insert-Pronunciation");

			Assert.That(m_entry.PronunciationsOS, Has.Count.EqualTo(3), "the shown entry takes the pronunciation");
			Assert.That(m_variant.PronunciationsOS, Is.Empty);
		}

		[Test]
		public void InsertSubsenseAndExtendedNote_FromTheMainMenu_AddToTheCurrentSense()
		{
			m_view.CurrentRowSource = () => HeaderRowOf(m_sense2);

			RunMainMenuCommand("CmdInsertSubsense");
			RunMainMenuCommand("CmdInsertExtNote");

			Assert.That(m_sense2.SensesOS, Has.Count.EqualTo(1));
			Assert.That(m_sense2.ExtendedNoteOS, Has.Count.EqualTo(1));
			Assert.That(m_sense1.SensesOS, Is.Empty);
		}

		[Test]
		public void InsertPicture_FromTheInsertMenu_AddsThePictureToTheCurrentSense_InOneUndoableStep()
		{
			m_view.CurrentRowSource = () => HeaderRowOf(m_sense2);
			m_view.PictureFileChosen = picture =>
			{
				Assert.That(picture, Is.Null, "the dialog opens for a new picture");
				return Path.Combine(Path.GetTempPath(), "main-menu-picture.jpg");
			};

			RunMainMenuCommand("CmdInsertPicture");

			Assert.That(m_sense2.PicturesOS, Has.Count.EqualTo(1));
			Assert.That(m_sense2.PicturesOS[0].PictureFileRA.InternalPath, Does.EndWith("main-menu-picture.jpg"));
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo(xWorksStrings.ksUndoInsertPicture));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_sense2.PicturesOS, Is.Empty);
		}

		[Test]
		public void InsertSoundOrMovie_FromTheMainMenu_LinksToTheCurrentPronunciation_ElseTheFirst()
		{
			m_view.MediaFilesToInsert = () => new[] { "main-menu.wav" };
			m_view.CurrentRowSource = () => Rows(m_entry).Single(f => f.Field == "Form" && f.ObjectHvo == m_pronunciation2.Hvo);

			RunMainMenuCommand("CmdInsertMediaFile");

			Assert.That(m_pronunciation2.MediaFilesOS, Has.Count.EqualTo(1));
			Assert.That(m_pronunciation1.MediaFilesOS, Is.Empty);

			m_view.CurrentRowSource = () => null;
			RunMainMenuCommand("CmdInsertMediaFile");

			Assert.That(m_pronunciation1.MediaFilesOS, Has.Count.EqualTo(1), "with no current row the entry's first takes it");
		}

		[Test]
		public void InsertVariant_FromTheInsertMenu_AddsAVariantOfTheShownEntry_WhateverRowIsCurrent()
		{
			ILexEntry asked = null;
			m_view.VariantToInsert = entry =>
			{
				asked = entry;
				return null;
			};
			m_view.CurrentRowSource = () => HeaderRowOf(m_sense1);

			RunMainMenuCommand("CmdInsertVariant");

			Assert.That(asked, Is.SameAs(m_entry));
		}

		[Test]
		public void LexemeFormHasComponents_MakesTheEntryAComplexForm_InOneUndoableStep()
		{
			ShowEntry(m_spare);
			var command = CommandOf("CmdChangeToComplexForm");

			RunMainMenuCommand("CmdChangeToComplexForm");

			Assert.That(m_spare.EntryRefsOS, Has.Count.EqualTo(1));
			var entryRef = m_spare.EntryRefsOS[0];
			Assert.That(entryRef.RefType, Is.EqualTo(LexEntryRefTags.krtComplexForm));
			Assert.That(entryRef.ComplexEntryTypesRS.Single().Guid, Is.EqualTo(LexEntryTypeTags.kguidLexTypeUnspecifiedComplexForm));
			Assert.That(Rows(m_spare).Any(f => f.ObjectHvo == entryRef.Hvo && f.Field == "ComponentLexemes"), Is.True,
				"the row the conversion focuses is shown");
			Assert.That(MainMenuDisplay("CmdChangeToComplexForm").Enabled, Is.False, "a complex form cannot become one again");
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo(command.UndoText));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_spare.EntryRefsOS, Is.Empty);
		}

		[Test]
		public void LexemeFormIsAVariant_AddsAVariantRefOfTheUnspecifiedType_AfterTheOthers()
		{
			RunMainMenuCommand("CmdChangeToVariant");

			Assert.That(m_entry.EntryRefsOS, Has.Count.EqualTo(4));
			var entryRef = m_entry.EntryRefsOS[3];
			Assert.That(entryRef.RefType, Is.EqualTo(LexEntryRefTags.krtVariant));
			Assert.That(entryRef.VariantEntryTypesRS.Single().Guid, Is.EqualTo(LexEntryTypeTags.kguidLexTypeUnspecifiedVar));
			Assert.That(Rows(m_entry).Any(f => f.ObjectHvo == entryRef.Hvo && f.Field == "ComponentLexemes"), Is.True,
				"the row the conversion focuses is shown");
		}

		[Test]
		public void ShowHiddenFields_FromTheViewMenu_FlipsThisToolsSetting_WithoutTheHiddenTreeOrTheMediator()
		{
			var item = ViewMenuItem(PropertyConstants.ShowHiddenFields);
			var rowsBefore = ShownRows().Count;
			var spy = new MainMenuSpyColleague();
			Window.Mediator.AddColleague(spy);
			try
			{
				var display = item.GetDisplayProperties();
				Assert.That(display.Enabled, Is.True);
				Assert.That(display.Checked, Is.False);

				item.OnClick(null, null);
				DrainMediatorAndIdleQueues();

				Assert.That(item.GetDisplayProperties().Checked, Is.True);
			}
			finally
			{
				Window.Mediator.RemoveColleague(spy);
			}

			Assert.That(m_propertyTable.GetBoolProperty("ShowHiddenFields-lexiconEdit", false,
				PropertyTable.SettingsGroup.LocalSettings), Is.True);
			Assert.That(m_propertyTable.GetBoolProperty(PropertyConstants.ShowHiddenFields, false), Is.False,
				"the View menu's own property is left alone");
			Assert.That(ShownRows().Count, Is.GreaterThan(rowsBefore), "the view shows the hidden fields");
			Assert.That(spy.Asked, Is.Empty);
			Assert.That(HiddenTreeBuilt, Is.False);
		}

		[Test]
		public void ShowDictionaryPreview_IsEnabledOnlyInTheLexiconEditTools()
		{
			var item = ViewMenuItem(RecordEditView.ShowDictionaryPreviewProperty);
			m_propertyTable.SetProperty("areaChoice", "lexicon", false);
			m_propertyTable.SetProperty("ToolForAreaNamed_lexicon", "lexiconEdit", false);
			Assert.That(item.GetDisplayProperties().Enabled, Is.True);

			m_propertyTable.SetProperty("ToolForAreaNamed_lexicon", "reversalToolEditComplete", false);
			Assert.That(item.GetDisplayProperties().Enabled, Is.False);
		}

		private XWindow Window => m_propertyTable.GetValue<XWindow>("window");

		private bool HiddenTreeBuilt
			=> (bool)typeof(RecordEditView).GetField("m_dataTreeInitialized", BindingFlags.Instance | BindingFlags.NonPublic)
				.GetValue(m_view);

		private Command CommandOf(string id) => (Command)Window.Mediator.CommandSet[id];

		// The command's display as the main menu, which hides items by default, asks for it.
		private UIItemDisplayProperties MainMenuDisplay(string id)
			=> CommandChoice.QueryDisplayProperties(Window.Mediator, m_propertyTable, CommandOf(id), false, id);

		private IEnumerable<string> EnabledMainMenuCommands()
			=> s_lexiconMainMenuCommandIds.Where(id => MainMenuDisplay(id).Enabled).ToList();

		// Runs the command as a main-menu click does.
		private void RunMainMenuCommand(string id)
		{
			CommandOf(id).InvokeCommand(CommandAuthorities.Of(m_propertyTable));
			DrainMediatorAndIdleQueues();
		}

		private ChoiceBase InsertMenuItem(string commandId)
			=> ChoiceOf(string.Format("//menu[@id='Insert']/item[@command='{0}']", commandId));

		private ChoiceBase PaneBarItem(string commandId)
			=> ChoiceOf(string.Format("//menu[@id='PaneBar-LexicalDetail']//item[@command='{0}']", commandId));

		private ChoiceBase ViewMenuItem(string boolProperty)
			=> ChoiceOf(string.Format("//menu[@id='View']/item[@boolProperty='{0}']", boolProperty));

		// The choice the window's configuration item makes, as a menu builds it.
		private ChoiceBase ChoiceOf(string xpath)
		{
			var node = m_propertyTable.GetValue<XmlNode>("WindowConfiguration").SelectSingleNode(xpath);
			Assert.That(node, Is.Not.Null, "the configuration has {0}", xpath);
			return ChoiceBase.Make(Window.Mediator, m_propertyTable, node, null, null);
		}

		// Answers nothing, only records each message of these commands that reaches it.
		private sealed class MainMenuSpyColleague : IxCoreColleague
		{
			public List<string> Asked { get; } = new List<string>();
			public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters) { }
			public IxCoreColleague[] GetMessageTargets() => new IxCoreColleague[] { this };
			public bool ShouldNotCall => false;
			public int Priority => (int)ColleaguePriority.High;
			public bool OnDisplayDataTreeInsert(object command, ref UIItemDisplayProperties display) => Record("DisplayDataTreeInsert");
			public bool OnDataTreeInsert(object command) => Record("DataTreeInsert");
			public bool OnDisplayInsertMediaFile(object command, ref UIItemDisplayProperties display) => Record("DisplayInsertMediaFile");
			public bool OnDisplayInsertPicture(object command, ref UIItemDisplayProperties display) => Record("DisplayInsertPicture");
			public bool OnDisplayInsertItemViaBackrefVector(object command, ref UIItemDisplayProperties display)
				=> Record("DisplayInsertItemViaBackrefVector");
			public bool OnDisplayConvertEntryIntoComplexForm(object command, ref UIItemDisplayProperties display)
				=> Record("DisplayConvertEntryIntoComplexForm");
			public bool OnDisplayConvertEntryIntoVariant(object command, ref UIItemDisplayProperties display)
				=> Record("DisplayConvertEntryIntoVariant");
			public bool OnDisplayShowHiddenFields(object command, ref UIItemDisplayProperties display)
				=> Record("DisplayShowHiddenFields");
			public bool OnDisplayShow_DictionaryPubPreview(object command, ref UIItemDisplayProperties display)
				=> Record("DisplayShow_DictionaryPubPreview");

			private bool Record(string message)
			{
				Asked.Add(message);
				return false;
			}
		}
	}
}
