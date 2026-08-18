// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The UI-mode properties must be in the PropertyTable BEFORE LoadUI creates
	/// the content views -- RecordEditView resolves its framework during window construction, so a
	/// window created with a persisted UIMode=New must see "New" at that moment or it comes up on
	/// Legacy. FwXWindow.InitMediatorValues seeds via this helper; these tests pin the
	/// helper's normalization and no-broadcast contract.
	/// </summary>
	[TestFixture]
	public class FwXWindowUIModeSeedingTests
	{
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private string m_savedSwitchingVariable;

		[SetUp]
		public void SetUp()
		{
			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
			// Seeding New requires the FW_AVALONIA opt-in. This is setup for the normalization
			// these tests are about, not something they verify.
			m_savedSwitchingVariable =
				Environment.GetEnvironmentVariable(UIModeGates.SwitchingEnabledVariable);
			Environment.SetEnvironmentVariable(UIModeGates.SwitchingEnabledVariable, "1");
		}

		[TearDown]
		public void TearDown()
		{
			Environment.SetEnvironmentVariable(
				UIModeGates.SwitchingEnabledVariable, m_savedSwitchingVariable);
			m_propertyTable.Dispose();
			m_mediator.Dispose();
		}

		[TestCase("New", "New")]
		[TestCase("new", "New")]
		[TestCase("Legacy", "Legacy")]
		[TestCase("", "Legacy")]
		[TestCase(null, "Legacy")]
		[TestCase("garbage", "Legacy")]
		public void SeedUIModeProperties_NormalizesTheModeFailClosed(string persisted, string expected)
		{
			FwXWindow.SeedUIModeProperties(m_propertyTable, persisted, null);

			Assert.That(m_propertyTable.GetStringProperty(
				UIFrameworkResolver.UIModePropertyName, null), Is.EqualTo(expected));
		}

		[Test]
		public void SeedUIModeProperties_SeedsDisabledTools_AndNullBecomesEmpty()
		{
			FwXWindow.SeedUIModeProperties(m_propertyTable, "New", "lexiconEdit,posEdit");
			Assert.That(m_propertyTable.GetStringProperty(
				UIFrameworkResolver.UIModeDisabledToolsPropertyName, null),
				Is.EqualTo("lexiconEdit,posEdit"));

			FwXWindow.SeedUIModeProperties(m_propertyTable, "New", null);
			Assert.That(m_propertyTable.GetStringProperty(
				UIFrameworkResolver.UIModeDisabledToolsPropertyName, null), Is.EqualTo(""));
		}
	}
}
