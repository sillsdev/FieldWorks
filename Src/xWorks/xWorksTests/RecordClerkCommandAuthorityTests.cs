// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The answers of the active record clerk to the commands that act on its record list,
	/// asked through the command authorities of its window.
	/// </summary>
	[TestFixture]
	public class RecordClerkCommandAuthorityTests : XWorksAppTestBase
	{
		private const string RulesTool = "PhonologicalRuleEdit";
		private const string EnvironmentsTool = "EnvironmentEdit";
		private const string LocationsTool = "locationsEdit";
		private const string AcademicDomainsTool = "domainTypeEdit";
		private const string CollectWordsTool = "rapidDataEntry";
		private const string WordListConcordanceTool = "wordListConcordance";

		private MockFwXWindow m_mockWindow;
		private readonly List<MockFwXWindow> m_windows = new List<MockFwXWindow>();
		private readonly List<ICmObject> m_created = new List<ICmObject>();

		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
		}

		[SetUp]
		public void OpenWindow()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var lp = Cache.LangProject;
				var rules = Cache.ServiceLocator.GetInstance<IPhRegularRuleFactory>();
				for (var i = 0; i < 3; i++)
					lp.PhonologicalDataOA.PhonRulesOS.Add(rules.Create());

				var lists = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>();
				if (lp.LocationsOA == null)
					lp.LocationsOA = lists.Create();
				if (lp.LexDbOA.DomainTypesOA == null)
					lp.LexDbOA.DomainTypesOA = lists.Create();
				if (lp.SemanticDomainListOA == null)
					lp.SemanticDomainListOA = lists.Create();
				for (var i = 0; i < 2; i++)
				{
					Named(Cache.ServiceLocator.GetInstance<ICmLocationFactory>().Create(),
						lp.LocationsOA, "Place" + i);
					Named(Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create(),
						lp.LexDbOA.DomainTypesOA, "Field" + i);
				}

				// Three domains, the first with three words, and three wordforms.
				ICmSemanticDomain firstDomain = null;
				for (var i = 0; i < 3; i++)
				{
					var domain = Cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>().Create();
					Named(domain, lp.SemanticDomainListOA, "Domain" + i);
					firstDomain = firstDomain ?? domain;
				}
				for (var i = 0; i < 3; i++)
				{
					var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
					var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
					entry.SensesOS.Add(sense);
					sense.SemanticDomainsRC.Add(firstDomain);
					m_created.Add(entry);
					m_created.Add(Cache.ServiceLocator.GetInstance<IWfiWordformFactory>()
						.Create(TsStringUtils.MakeString("word" + i, Cache.DefaultVernWs)));
				}
			});
			m_mockWindow = NewWindow();
			m_window = m_mockWindow;
		}

		[TearDown]
		public void CloseWindows()
		{
			foreach (var window in m_windows)
			{
				window.PropTable.RemoveLocalAndGlobalSettings();
				window.Mediator.RemoveColleague(window);
				window.Dispose();
			}
			m_windows.Clear();
			m_mockWindow = null;
			m_window = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var lp = Cache.LangProject;
				lp.PhonologicalDataOA.PhonRulesOS.Clear();
				lp.PhonologicalDataOA.EnvironmentsOS.Clear();
				foreach (var obj in m_created.Where(obj => obj.IsValidObject))
					obj.Delete();
				lp.LocationsOA.PossibilitiesOS.Clear();
				lp.LexDbOA.DomainTypesOA.PossibilitiesOS.Clear();
				lp.SemanticDomainListOA.PossibilitiesOS.Clear();
			});
			m_created.Clear();
		}

		[TestCase(RulesTool)]
		[TestCase(EnvironmentsTool)]
		[TestCase(LocationsTool)]
		[TestCase(AcademicDomainsTool)]
		[TestCase(WordListConcordanceTool)]
		public void TheActiveClerk_ShowsItsCommands_AsTheMediatorShowsThem(string tool)
		{
			m_mockWindow.ActivateTool(tool);
			var mediator = m_mockWindow.Mediator;
			var mediatorMessages = RecordClerkCommandAuthority.AnsweredMessages
				.Where(message => mediator.HasReceiver(message)).ToList();
			Assert.That(mediatorMessages, Does.Contain(RecordClerkCommandAuthority.InsertItemMessage)
				.And.Contain(RecordClerkCommandAuthority.NextRecordMessage), "the mediator reaches the clerk for these too");

			var compared = 0;
			foreach (var command in CommandsSending(mediatorMessages))
			{
				Assert.That(Authorities(m_mockWindow).UpdateDisplay(command, NewDisplay(command)), Is.True,
					command.Id + " is answered");

				var viaMediator = CommandChoice.QueryDisplayProperties(mediator, command, false, command.Label);
				var viaAuthority = Display(m_mockWindow, command);
				Assert.That(viaAuthority.Enabled, Is.EqualTo(viaMediator.Enabled), command.Id + " enabled");
				Assert.That(viaAuthority.Visible, Is.EqualTo(viaMediator.Visible), command.Id + " visible");
				Assert.That(viaAuthority.Text, Is.EqualTo(viaMediator.Text), command.Id + " text");
				compared++;
			}
			Assert.That(compared, Is.GreaterThanOrEqualTo(39), "the 35 insert and 4 navigation commands");
		}

		[Test]
		public void AnInsertCommand_InsertsOneItem_IntoTheActiveClerksList()
		{
			m_mockWindow.ActivateTool(EnvironmentsTool);
			var environments = Cache.LangProject.PhonologicalDataOA.EnvironmentsOS;
			var before = environments.Count;
			var command = CommandOf("CmdInsertPhEnvironment");
			Assert.That(Display(m_mockWindow, command).Enabled, Is.True);

			Assert.That(Authorities(m_mockWindow).Execute(command), Is.True);

			Assert.That(environments.Count, Is.EqualTo(before + 1));
			Assert.That(m_mockWindow.ActiveClerk.CurrentObject, Is.EqualTo(environments.Last()));
		}

		[Test]
		public void AnInsertCommand_ForAClassTheListCannotHold_IsDisabled()
		{
			m_mockWindow.ActivateTool(EnvironmentsTool);

			Assert.That(Display(m_mockWindow, CommandOf("CmdInsertPhRegularRule")).Enabled, Is.False);
		}

		[Test]
		public void TheRecordCommands_MoveTheActiveClerk()
		{
			m_mockWindow.ActivateTool(RulesTool);
			var clerk = m_mockWindow.ActiveClerk;
			var authorities = Authorities(m_mockWindow);
			Assert.That(authorities.Execute(CommandOf("CmdFirstRecord")), Is.True);
			Assert.That(clerk.CurrentIndex, Is.EqualTo(0));
			Assert.That(Display(m_mockWindow, CommandOf("CmdFirstRecord")).Enabled, Is.False, "already on the first record");
			Assert.That(Display(m_mockWindow, CommandOf("CmdPreviousRecord")).Enabled, Is.False);
			Assert.That(Display(m_mockWindow, CommandOf("CmdNextRecord")).Enabled, Is.True);

			Assert.That(authorities.Execute(CommandOf("CmdNextRecord")), Is.True);
			Assert.That(clerk.CurrentIndex, Is.EqualTo(1));

			Assert.That(authorities.Execute(CommandOf("CmdLastRecord")), Is.True);
			Assert.That(clerk.CurrentIndex, Is.EqualTo(2));
			Assert.That(Display(m_mockWindow, CommandOf("CmdLastRecord")).Enabled, Is.False, "already on the last record");

			Assert.That(authorities.Execute(CommandOf("CmdPreviousRecord")), Is.True);
			Assert.That(clerk.CurrentIndex, Is.EqualTo(1));
		}

		/// <summary>
		/// Collect Words shows two record lists: the semantic domains, whose clerk is the
		/// active one, and the words of the current domain.
		/// </summary>
		[Test]
		public void InCollectWords_TheRecordCommands_MoveTheSemanticDomains_NotTheWords()
		{
			OpenCollectWords();
			var domains = RecordClerk.FindClerk(m_mockWindow.PropTable, "SemanticDomainList");
			var words = RecordClerk.FindClerk(m_mockWindow.PropTable, "RDEwords");
			Assert.That(m_mockWindow.ActiveClerk, Is.SameAs(domains));
			Assert.That(domains.CurrentIndex, Is.EqualTo(0));
			Assert.That(words.ListSize, Is.EqualTo(3), "the first domain has three words");
			var wordIndex = words.CurrentIndex;

			Assert.That(Display(m_mockWindow, CommandOf("CmdNextRecord")).Enabled, Is.True);
			Assert.That(Authorities(m_mockWindow).Execute(CommandOf("CmdLastRecord")), Is.True);

			Assert.That(domains.CurrentIndex, Is.EqualTo(2));
			Assert.That(words.CurrentIndex, Is.EqualTo(wordIndex), "no command moved the word list");
			Assert.That(Display(m_mockWindow, CommandOf("CmdNextRecord")).Enabled, Is.False, "on the last domain");
		}

		/// <summary>
		/// The mediator asks both clerks of Collect Words about an insert command; only the
		/// active one answers through its authority.
		/// </summary>
		[Test]
		public void InCollectWords_TheInsertCommands_ShowAsTheMediatorShowsThem()
		{
			OpenCollectWords();
			var mediator = m_mockWindow.Mediator;

			var compared = 0;
			foreach (var command in CommandsSending(new[] { RecordClerkCommandAuthority.InsertItemMessage }))
			{
				var viaMediator = CommandChoice.QueryDisplayProperties(mediator, command, false, command.Label);
				var viaAuthority = Display(m_mockWindow, command);
				Assert.That(viaAuthority.Enabled, Is.EqualTo(viaMediator.Enabled), command.Id + " enabled");
				Assert.That(viaAuthority.Visible, Is.EqualTo(viaMediator.Visible), command.Id + " visible");
				compared++;
			}
			Assert.That(compared, Is.EqualTo(35));
		}

		[Test]
		public void AClerkThatIsNotTheActiveClerk_AnswersNothing()
		{
			m_mockWindow.ActivateTool(RulesTool);
			var rulesClerk = m_mockWindow.ActiveClerk;
			m_mockWindow.ActivateTool(EnvironmentsTool);
			Assert.That(m_mockWindow.ActiveClerk, Is.Not.SameAs(rulesClerk));
			var authority = new RecordClerkCommandAuthority(rulesClerk);
			var command = CommandOf("CmdNextRecord");

			Assert.That(authority.UpdateDisplay(command, NewDisplay(command)), Is.False);
			Assert.That(authority.Execute(command), Is.False);
		}

		[Test]
		public void AfterAToolSwitch_TheNewToolsClerkAnswers()
		{
			m_mockWindow.ActivateTool(RulesTool);
			var rulesClerk = m_mockWindow.ActiveClerk;
			Authorities(m_mockWindow).Execute(CommandOf("CmdFirstRecord"));
			m_mockWindow.ActivateTool(EnvironmentsTool);

			Assert.That(Display(m_mockWindow, CommandOf("CmdInsertPhEnvironment")).Enabled, Is.True);
			Assert.That(Display(m_mockWindow, CommandOf("CmdInsertPhRegularRule")).Enabled, Is.False);
			Authorities(m_mockWindow).Execute(CommandOf("CmdLastRecord"));
			Assert.That(rulesClerk.CurrentIndex, Is.EqualTo(0), "the clerk of the tool that went away does not move");
		}

		[Test]
		public void AClerkOutOfUse_AnswersNothing_HoweverOftenItWasActivated()
		{
			m_mockWindow.ActivateTool(RulesTool);
			var clerk = m_mockWindow.ActiveClerk;
			var command = CommandOf("CmdNextRecord");
			clerk.ActivateUI(true);
			Assert.That(Authorities(m_mockWindow).UpdateDisplay(command, NewDisplay(command)), Is.True);

			clerk.BecomeInactive();

			Assert.That(Authorities(m_mockWindow).UpdateDisplay(command, NewDisplay(command)), Is.False);
			Assert.That(Authorities(m_mockWindow).Execute(command), Is.False);
		}

		[Test]
		public void ADuplicateCommand_DuplicatesTheCurrentItem()
		{
			m_mockWindow.ActivateTool(AcademicDomainsTool);
			var items = Cache.LangProject.LexDbOA.DomainTypesOA.PossibilitiesOS;
			var command = CommandOf("CmdDuplicatePossibility");
			Assert.That(m_mockWindow.Mediator.HasReceiver(RecordClerkCommandAuthority.DuplicateItemMessage), Is.False,
				"only the clerk's authority runs it");
			Assert.That(Display(m_mockWindow, command).Enabled, Is.True);

			Assert.That(Authorities(m_mockWindow).Execute(command), Is.True);

			Assert.That(items.Count, Is.EqualTo(3));
		}

		[Test]
		public void ADuplicateCommand_IsDisabled_InAListItsNoCopyParameterNames()
		{
			m_mockWindow.ActivateTool(LocationsTool);

			Assert.That(Display(m_mockWindow, CommandOf("CmdInsertLocation")).Enabled, Is.True);
			Assert.That(Display(m_mockWindow, CommandOf("CmdDuplicateLocation")).Enabled, Is.False);
		}

		[Test]
		public void ADuplicateCommand_IsDisabled_ForAClassTheListCannotHold()
		{
			m_mockWindow.ActivateTool(RulesTool);
			var command = CommandOf("CmdDuplicatePossibility");

			Assert.That(Authorities(m_mockWindow).UpdateDisplay(command, NewDisplay(command)), Is.True);
			Assert.That(Display(m_mockWindow, command).Enabled, Is.False);
		}

		[Test]
		public void EachWindow_AsksItsOwnClerk()
		{
			m_mockWindow.ActivateTool(RulesTool);
			var second = NewWindow();
			second.ActivateTool(RulesTool);
			var firstClerk = m_mockWindow.ActiveClerk;
			var secondClerk = second.ActiveClerk;
			Assert.That(secondClerk, Is.Not.SameAs(firstClerk));
			Authorities(m_mockWindow).Execute(CommandOf(m_mockWindow, "CmdFirstRecord"));
			Authorities(second).Execute(CommandOf(second, "CmdFirstRecord"));

			Assert.That(Authorities(second).Execute(CommandOf(second, "CmdLastRecord")), Is.True);

			Assert.That(secondClerk.CurrentIndex, Is.EqualTo(2));
			Assert.That(firstClerk.CurrentIndex, Is.EqualTo(0), "the other window's clerk does not move");
		}

		private void OpenCollectWords()
		{
			// The tool's introductory message box would block a headless run.
			var configuration = m_mockWindow.PropTable.GetValue<XmlNode>("WindowConfiguration");
			foreach (XmlElement parameters in configuration.SelectNodes(
				"//tool[@value='" + CollectWordsTool + "']//parameters[@msgBoxTrigger]"))
			{
				parameters.RemoveAttribute("msgBoxTrigger");
			}
			m_mockWindow.ActivateTool(CollectWordsTool);
		}

		private void Named(ICmPossibility item, ICmPossibilityList list, string name)
		{
			list.PossibilitiesOS.Add(item);
			var ws = Cache.DefaultAnalWs;
			item.Name.set_String(ws, TsStringUtils.MakeString(name, ws));
			item.Abbreviation.set_String(ws, TsStringUtils.MakeString(name, ws));
		}

		private MockFwXWindow NewWindow()
		{
			var window = new MockFwXWindow(m_application, m_configFilePath);
			window.Init(Cache);
			window.PropTable.RemoveLocalAndGlobalSettings();
			window.ProcessPendingItems();
			window.LoadUI(m_configFilePath);
			m_windows.Add(window);
			return window;
		}

		private static CommandAuthorities Authorities(MockFwXWindow window) => CommandAuthorities.Of(window.PropTable);

		private Command CommandOf(string id) => CommandOf(m_mockWindow, id);

		private static Command CommandOf(MockFwXWindow window, string id) => (Command)window.Mediator.CommandSet[id];

		private IEnumerable<Command> CommandsSending(ICollection<string> messages)
			=> m_mockWindow.Mediator.CommandSet.Values.Cast<Command>().Where(command => messages.Contains(command.MessageString));

		private static UIItemDisplayProperties NewDisplay(Command command)
			=> new UIItemDisplayProperties(null, command.Label, false, command.IconName, false);

		// What a toolbar, menu or shortcut is told about the command.
		private static UIItemDisplayProperties Display(MockFwXWindow window, Command command)
			=> CommandChoice.QueryDisplayProperties(window.Mediator, window.PropTable, command, false, command.Label);
	}
}
