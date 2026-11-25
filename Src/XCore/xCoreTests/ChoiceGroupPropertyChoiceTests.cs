// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using static SIL.FieldWorks.Common.FwUtils.FwUtils;

namespace XCore
{
	/// <summary>
	/// How <see cref="ChoiceGroup.ChooseSinglePropertyAtomicValue"/> tells listeners about the
	/// choice it stores: through Pub/Sub for the properties it publishes, through the Mediator
	/// for the others.
	/// </summary>
	[TestFixture]
	public class ChoiceGroupPropertyChoiceTests
	{
		private const string PublishedProperty = PropertyConstants.CurrentFilterForRecordClerkPrefix + "things";
		private const string OtherProperty = "chosenThing";

		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private PropertyChangeSpy m_spy;
		private readonly StubWindow m_window = new StubWindow();
		private readonly StubWindow m_otherWindow = new StubWindow();
		private readonly List<object> m_heard = new List<object>();
		private readonly List<object> m_heardInOtherWindow = new List<object>();

		[SetUp]
		public void SetUp()
		{
			m_mediator = new Mediator();
			m_spy = new PropertyChangeSpy();
			m_mediator.AddColleague(m_spy);
			// With a local settings id, listeners are told the property's own name.
			m_propertyTable = new PropertyTable(m_mediator) { LocalSettingsId = "TestLocal" };
			m_propertyTable.SetProperty("window", m_window, false);
			m_heard.Clear();
			m_heardInOtherWindow.Clear();
			foreach (var property in new[] { PublishedProperty, OtherProperty })
			{
				Subscriber.Subscribe(PropertyConstants.PropertyChanged(property), Hear, m_window);
				Subscriber.Subscribe(PropertyConstants.PropertyChanged(property), HearInOtherWindow, m_otherWindow);
			}
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var property in new[] { PublishedProperty, OtherProperty })
			{
				Subscriber.Unsubscribe(PropertyConstants.PropertyChanged(property), Hear);
				Subscriber.Unsubscribe(PropertyConstants.PropertyChanged(property), HearInOtherWindow);
			}
			m_mediator.RemoveColleague(m_spy);
			m_propertyTable.Dispose();
			m_mediator.Dispose();
		}

		[Test]
		public void Choice_OfAPublishedProperty_PublishesTheNewValueInItsOwnWindow()
		{
			// SUT
			Choose(PublishedProperty, "first");

			Assert.That(m_heard, Is.EqualTo(new object[] { "first" }));
			Assert.That(m_heardInOtherWindow, Is.Empty);
			DrainMediator();
			Assert.That(m_spy.Names, Is.EqualTo(new[] { PublishedProperty + "Parameters" }),
				"the Mediator is not told of the choice as well");
		}

		[Test]
		public void Choice_OfTheCurrentValue_PublishesNothing()
		{
			Choose(PublishedProperty, "first");
			m_heard.Clear();

			// SUT
			Choose(PublishedProperty, "first");

			Assert.That(m_heard, Is.Empty);
		}

		[Test]
		public void Choice_OfAnotherProperty_GoesToTheMediatorOnly()
		{
			// SUT
			Choose(OtherProperty, "first");

			Assert.That(m_heard, Is.Empty);
			DrainMediator();
			Assert.That(m_spy.Names, Is.EqualTo(new[] { OtherProperty + "Parameters", OtherProperty }));
		}

		private void Choose(string property, string value)
		{
			ChoiceGroup.ChooseSinglePropertyAtomicValue(m_mediator, m_propertyTable, value, null, property,
				PropertyTable.SettingsGroup.LocalSettings);
		}

		private void DrainMediator()
		{
			for (var i = 0; m_mediator.JobItems > 0; i++)
			{
				Assert.That(i, Is.LessThan(100), "Mediator queue did not drain");
				m_mediator.ProcessItem();
			}
		}

		private void Hear(object newValue)
		{
			m_heard.Add(newValue);
		}

		private void HearInOtherWindow(object newValue)
		{
			m_heardInOtherWindow.Add(newValue);
		}

		/// <summary>A main window as far as the property table and Pub/Sub can tell.</summary>
		private sealed class StubWindow : IxWindow
		{
			public Mediator Mediator => null;
			public PropertyTable PropTable => null;
			public void SuspendIdleProcessing() { }
			public void ResumeIdleProcessing() { }
			public void SuspendWindowSizePersistence() { }
			public void ResumeWindowSizePersistence() { }
		}

		/// <summary>Records every OnPropertyChanged name the Mediator delivers.</summary>
		private sealed class PropertyChangeSpy : IxCoreColleague
		{
			public List<string> Names { get; } = new List<string>();
			public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters) { }
			public IxCoreColleague[] GetMessageTargets() => new IxCoreColleague[] { this };
			public bool ShouldNotCall => false;
			public int Priority => (int)ColleaguePriority.Medium;
			public void OnPropertyChanged(string name) => Names.Add(name);
		}
	}
}
