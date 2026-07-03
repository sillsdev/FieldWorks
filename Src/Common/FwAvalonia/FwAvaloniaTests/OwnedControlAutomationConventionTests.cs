// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Stage 1.5 (developer-enablement kit) convention guard: owned field controls MUST stamp a stable,
	/// nonlocalized <c>AutomationId</c> derived from the field's stable id, a localized <c>Name</c>, and
	/// per-writing-system value boxes suffixed under that id. The stamping is implemented in
	/// <c>FwFieldControls</c>; this test makes the convention executable (per the migration-program review
	/// finding that conventions were implemented but unenforced), so a future owned control that forgets
	/// an AutomationId fails CI rather than silently breaking automation/accessibility identity.
	/// </summary>
	[TestFixture]
	public class OwnedControlAutomationConventionTests
	{
		private static LexicalEditRegionField MakeTextField(string stableId, string label, string automationId)
		{
			return new LexicalEditRegionField(stableId, label, "Form", null,
				RegionFieldKind.Text, EditorClassification.Known, automationId, null, SurfaceRouting.Inherit,
				new List<RegionWsValue>
				{
					new RegionWsValue("fr", "bonjour", wsTag: "fr")
				}, null, null);
		}

		[AvaloniaTest]
		public void MultiWsTextField_StampsStableAutomationId_AndLocalizedName()
		{
			const string automationId = "LexEntry_Form";
			var field = MakeTextField("LexEntry/Form", "Lexeme Form", automationId);
			var context = new FakeRegionEditContext();
			var fieldControl = new FwMultiWsTextField(field, automationId, context, null);
			var window = new Window { Content = fieldControl, Width = 300, Height = 120 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			Assert.That(AutomationProperties.GetAutomationId(fieldControl), Is.EqualTo(automationId),
				"the field control's AutomationId must be the stable field id (nonlocalized)");
			Assert.That(AutomationProperties.GetName(fieldControl), Is.EqualTo("Lexeme Form"),
				"the field control's automation Name must be the localized label");
		}

		[AvaloniaTest]
		public void MultiWsTextField_PerWritingSystemBox_IsSuffixedUnderTheFieldAutomationId()
		{
			const string automationId = "LexEntry_Form";
			var field = MakeTextField("LexEntry/Form", "Lexeme Form", automationId);
			var context = new FakeRegionEditContext();
			var fieldControl = new FwMultiWsTextField(field, automationId, context, null);
			var window = new Window { Content = fieldControl, Width = 300, Height = 120 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var box = fieldControl.GetVisualDescendants().OfType<TextBox>().First();
			var boxId = AutomationProperties.GetAutomationId(box);
			Assert.That(boxId, Is.Not.Null.And.Not.Empty, "each per-WS value box must carry an AutomationId");
			Assert.That(boxId, Does.StartWith(automationId + "."),
				"per-WS value boxes must be stable-id-suffixed (e.g. '{fieldId}.{ws}') so automation can address each WS row");
		}
	}
}
