// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;

namespace XCore
{
	/// <summary>
	/// Command authorities answer menu commands and boolean properties ahead of the mediator:
	/// the most recently registered first, and what one answers never reaches a colleague.
	/// </summary>
	[TestFixture]
	public class CommandAuthoritiesTests
	{
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private Command m_command;
		private SpyColleague m_spy;

		[SetUp]
		public void SetUp()
		{
			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
			m_command = new Command(m_mediator, Node("<command id='CmdTest' label='Test' message='TestMessage'/>"));
			m_spy = new SpyColleague();
			m_mediator.AddColleague(m_spy);
		}

		[TearDown]
		public void TearDown()
		{
			m_mediator.RemoveColleague(m_spy);
			m_command.Dispose();
			m_mediator.Dispose();
			m_propertyTable.Dispose();
		}

		[Test]
		public void TheMostRecentlyRegisteredAuthorityThatAnswers_Decides()
		{
			Assert.That(CommandAuthorities.Of(m_propertyTable), Is.Null, "none registered yet");
			var older = new FakeAuthority { Answers = true, Enabled = true };
			var newer = new FakeAuthority { Answers = true, Enabled = false };
			CommandAuthorities.Register(m_propertyTable, older);
			var newerRegistration = CommandAuthorities.Register(m_propertyTable, newer);

			Assert.That(Display().Enabled, Is.False, "the newer authority answers");
			newer.Answers = false;
			Assert.That(Display().Enabled, Is.True, "a declining authority leaves the command to the older one");
			newer.Answers = true;
			newerRegistration.Dispose();
			Assert.That(Display().Enabled, Is.True, "an unregistered authority is not asked");
			Assert.That(m_spy.Messages, Is.Empty);
		}

		[Test]
		public void AnAnsweredCommand_IsShownAndRun_WithoutTheMediator()
		{
			var authority = new FakeAuthority { Answers = true, Enabled = true };
			CommandAuthorities.Register(m_propertyTable, authority);

			var display = Display();
			m_command.InvokeCommand(CommandAuthorities.Of(m_propertyTable));

			Assert.That(display.Enabled, Is.True);
			Assert.That(authority.Executed, Is.EqualTo(1));
			Assert.That(m_spy.Messages, Is.Empty);
		}

		[Test]
		public void ACommandNoAuthorityAnswers_GoesToTheMediator()
		{
			CommandAuthorities.Register(m_propertyTable, new FakeAuthority { Answers = false });

			Display();
			m_command.InvokeCommand(CommandAuthorities.Of(m_propertyTable));

			Assert.That(m_spy.Messages, Is.EqualTo(new[] { "DisplayTestMessage", "TestMessage" }));
		}

		[Test]
		public void AnAnsweredBoolProperty_IsShownAndToggledByTheAuthority()
		{
			var authority = new FakeAuthority { Answers = true, Enabled = true, Checked = true };
			CommandAuthorities.Register(m_propertyTable, authority);
			var choice = ChoiceBase.Make(m_mediator, m_propertyTable, Node("<item label='Toggle' boolProperty='TestBool'/>"), null, null);

			var display = choice.GetDisplayProperties();
			choice.OnClick(null, null);

			Assert.That(display.Enabled, Is.True);
			Assert.That(display.Checked, Is.True);
			Assert.That(authority.Toggled, Is.EqualTo(1));
			Assert.That(m_propertyTable.GetBoolProperty("TestBool", false), Is.False, "the authority toggled it, not the item");
			Assert.That(m_spy.Messages, Is.Empty);
		}

		[Test]
		public void ABoolPropertyNoAuthorityToggles_IsFlippedByTheItem()
		{
			CommandAuthorities.Register(m_propertyTable, new FakeAuthority { Answers = false });
			var choice = ChoiceBase.Make(m_mediator, m_propertyTable, Node("<item label='Toggle' boolProperty='TestBool'/>"), null, null);

			choice.OnClick(null, null);

			Assert.That(m_propertyTable.GetBoolProperty("TestBool", false), Is.True);
		}

		[Test]
		public void AnItemInAMenuRaisedOnATarget_NeverAsksTheWindowsAuthorities()
		{
			var authority = new FakeAuthority { Answers = true, Enabled = false };
			CommandAuthorities.Register(m_propertyTable, authority);
			var menu = new ChoiceGroup(m_mediator, m_propertyTable, null, Node("<menu id='mnuTest'/>"), null) { IsRaisedOnTarget = true };
			var submenu = new ChoiceGroup(m_mediator, m_propertyTable, null, Node("<menu id='mnuTestSub'/>"), menu);
			var choice = ChoiceBase.Make(m_mediator, m_propertyTable, Node("<item label='Toggle' boolProperty='TestBool'/>"), null,
				submenu);

			choice.GetDisplayProperties();
			choice.OnClick(null, null);

			Assert.That(submenu.IsInMenuRaisedOnTarget, Is.True);
			Assert.That(m_spy.Messages, Is.EqualTo(new[] { "DisplayTestBool" }), "the mediator answers the item");
			Assert.That(authority.Toggled, Is.EqualTo(0));
			Assert.That(m_propertyTable.GetBoolProperty("TestBool", false), Is.True, "the item flipped the property");
		}

		private UIItemDisplayProperties Display()
			=> CommandChoice.QueryDisplayProperties(m_mediator, m_propertyTable, m_command, false, "Test");

		private static XmlNode Node(string xml)
		{
			var document = new XmlDocument();
			document.LoadXml(xml);
			return document.DocumentElement;
		}

		private sealed class FakeAuthority : ICommandAuthority
		{
			public bool Answers { get; set; }
			public bool Enabled { get; set; }
			public bool Checked { get; set; }
			public int Executed { get; private set; }
			public int Toggled { get; private set; }

			public bool UpdateDisplay(Command command, UIItemDisplayProperties display)
			{
				display.Enabled = Enabled;
				return Answers;
			}

			public bool Execute(Command command)
			{
				if (Answers)
					Executed++;
				return Answers;
			}

			public bool UpdatePropertyDisplay(string propertyName, UIItemDisplayProperties display)
			{
				if (!Answers)
					return false;
				display.Enabled = Enabled;
				display.Checked = Checked;
				return true;
			}

			public bool ToggleProperty(string propertyName)
			{
				if (Answers)
					Toggled++;
				return Answers;
			}
		}

		// Answers nothing, only records each message that reaches it.
		private sealed class SpyColleague : IxCoreColleague
		{
			public List<string> Messages { get; } = new List<string>();
			public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters) { }
			public IxCoreColleague[] GetMessageTargets() => new IxCoreColleague[] { this };
			public bool ShouldNotCall => false;
			public int Priority => (int)ColleaguePriority.High;

			public bool OnDisplayTestMessage(object command, ref UIItemDisplayProperties display)
			{
				Messages.Add("DisplayTestMessage");
				return true;
			}

			public bool OnTestMessage(object command)
			{
				Messages.Add("TestMessage");
				return true;
			}

			public bool OnDisplayTestBool(object command, ref UIItemDisplayProperties display)
			{
				Messages.Add("DisplayTestBool");
				return true;
			}
		}
	}
}
