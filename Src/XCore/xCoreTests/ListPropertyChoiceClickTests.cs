// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml;
using NUnit.Framework;

namespace XCore
{
	/// <summary>
	/// How <see cref="ListPropertyChoice.OnClick"/> recovers from a click whose handling throws.
	/// </summary>
	[TestFixture]
	public class ListPropertyChoiceClickTests
	{
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private FailsOnceColleague m_colleague;
		private ListPropertyChoice m_choice;

		[SetUp]
		public void SetUp()
		{
			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
			m_colleague = new FailsOnceColleague();
			m_mediator.AddColleague(m_colleague);

			var doc = new XmlDocument();
			doc.LoadXml("<list id=\"ToolsList\" behavior=\"command\" message=\"ToolCommand\"/>");
			var group = new ChoiceGroup(m_mediator, m_propertyTable, null, doc.DocumentElement, null);
			var item = new ListItem { label = "Tool", value = "tool", enabled = true };
			m_choice = new ListPropertyChoice(m_mediator, m_propertyTable, item, null, group);
		}

		[TearDown]
		public void TearDown()
		{
			m_mediator.RemoveColleague(m_colleague);
			m_propertyTable.Dispose();
			m_mediator.Dispose();
		}

		[Test]
		public void ClickAfterAClickThatThrew_IsHandled()
		{
			Assert.That(() => m_choice.OnClick(null, EventArgs.Empty), Throws.Exception);

			m_choice.OnClick(null, EventArgs.Empty);

			Assert.That(m_colleague.Calls, Is.EqualTo(2));
		}

		/// <summary>
		/// Answers the list's command, throwing the first time only.
		/// </summary>
		private sealed class FailsOnceColleague : IxCoreColleague
		{
			public int Calls { get; private set; }
			public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters) { }
			public IxCoreColleague[] GetMessageTargets() => new IxCoreColleague[] { this };
			public bool ShouldNotCall => false;
			public int Priority => (int)ColleaguePriority.Medium;

			public bool OnToolCommand(object parameter)
			{
				Calls++;
				if (Calls == 1)
					throw new InvalidOperationException("first click fails");
				return true;
			}
		}
	}
}
