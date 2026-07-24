// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The UI-mode properties must be in the PropertyTable BEFORE LoadUI creates
	/// the content views — RecordEditView resolves its surface during window construction, so a
	/// window created with a persisted UIMode=New must see "New" at that moment or it comes up on
	/// the Legacy surface. FwXWindow.InitMediatorValues seeds via this helper; these tests pin the
	/// helper's normalization and no-broadcast contract.
	/// </summary>
	[TestFixture]
	public class FwXWindowUIModeSeedingTests
	{
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;

		[SetUp]
		public void SetUp()
		{
			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
		}

		[TearDown]
		public void TearDown()
		{
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
				LexicalEditSurfaceResolver.UIModePropertyName, null), Is.EqualTo(expected));
		}

		[Test]
		public void SeedUIModeProperties_SeedsDisabledTools_AndNullBecomesEmpty()
		{
			FwXWindow.SeedUIModeProperties(m_propertyTable, "New", "lexiconEdit,posEdit");
			Assert.That(m_propertyTable.GetStringProperty(
				LexicalEditSurfaceResolver.UIModeDisabledToolsPropertyName, null),
				Is.EqualTo("lexiconEdit,posEdit"));

			FwXWindow.SeedUIModeProperties(m_propertyTable, "New", null);
			Assert.That(m_propertyTable.GetStringProperty(
				LexicalEditSurfaceResolver.UIModeDisabledToolsPropertyName, null), Is.EqualTo(""));
		}
	}
}
