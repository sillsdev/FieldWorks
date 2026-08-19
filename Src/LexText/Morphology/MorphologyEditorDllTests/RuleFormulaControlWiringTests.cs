// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Constructing each real rule formula control must produce a read-only rootsite, since a
	/// rule cell is modifiable only by chooser-insert and delete.
	/// </summary>
	[TestFixture]
	public class RuleFormulaControlWiringTests
	{
		[Test]
		public void RegRuleFormulaControl_RootSiteIsReadOnly()
		{
			using (var control = new RegRuleFormulaControl(null))
			{
				Assert.That(control.RootSite.ReadOnlyView, Is.True,
					"RegRuleFormulaControl must wire up a read-only rootsite");
			}
		}

		[Test]
		public void MetaRuleFormulaControl_RootSiteIsReadOnly()
		{
			using (var control = new MetaRuleFormulaControl(null))
			{
				Assert.That(control.RootSite.ReadOnlyView, Is.True,
					"MetaRuleFormulaControl must wire up a read-only rootsite");
			}
		}

		[Test]
		public void AffixRuleFormulaControl_RootSiteIsReadOnly()
		{
			using (var control = new AffixRuleFormulaControl(null))
			{
				Assert.That(control.RootSite.ReadOnlyView, Is.True,
					"AffixRuleFormulaControl must wire up a read-only rootsite");
			}
		}
	}
}
