// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Seams;

namespace FwAvaloniaTests
{
	[TestFixture]
	public class LexicalEditSurfaceSelectionServiceTests
	{
		private readonly LexicalEditSurfaceSelectionService _service = new LexicalEditSurfaceSelectionService();

		[Test]
		public void NewMode_SupportedTool_IsSupportedAvalonia()
		{
			var decision = _service.Decide("New", "lexiconEdit");
			Assert.That(decision.Surface, Is.EqualTo(LexicalEditSurface.Avalonia));
			Assert.That(decision.Behavior, Is.EqualTo(HostUiBehavior.SupportedAvalonia));
		}

		[Test]
		public void NewMode_UnmigratedTool_IsExplicitLegacyFallback()
		{
			// domainTypeEdit (a Lists CmPossibility tool) is not yet registered for the Avalonia edit surface
			// (pending the §20.3.1 F-4 predicate), so New mode is an explicit legacy fallback. (posEdit/notebookEdit
			// are now registered — §20.3 — and resolve to Avalonia; covered by RecordEditViewSwitchTests.)
			var decision = _service.Decide("New", "domainTypeEdit");
			Assert.That(decision.Surface, Is.EqualTo(LexicalEditSurface.WinForms));
			Assert.That(decision.Behavior, Is.EqualTo(HostUiBehavior.ExplicitLegacyFallback));
		}

		[Test]
		public void LegacyMode_SupportedTool_IsLegacyActive()
		{
			var decision = _service.Decide("Legacy", "lexiconEdit");
			Assert.That(decision.Surface, Is.EqualTo(LexicalEditSurface.WinForms));
			Assert.That(decision.Behavior, Is.EqualTo(HostUiBehavior.LegacyActive));
		}

		[Test]
		public void Override_ForcesAvalonia_ForSupportedTool()
		{
			var decision = _service.Decide("Legacy", "lexiconEdit", overrideEnabled: true);
			Assert.That(decision.Surface, Is.EqualTo(LexicalEditSurface.Avalonia));
			Assert.That(decision.Behavior, Is.EqualTo(HostUiBehavior.SupportedAvalonia));
		}

		[Test]
		public void EveryDecision_HasAReason()
		{
			Assert.That(_service.Decide("New", "lexiconEdit").Reason, Is.Not.Empty);
			Assert.That(_service.Decide("New", "posEdit").Reason, Is.Not.Empty);
			Assert.That(_service.Decide("Legacy", "lexiconEdit").Reason, Is.Not.Empty);
		}
	}

	[TestFixture]
	public class ActiveHostContractTests
	{
		[Test]
		public void Legacy_PermitsLegacyDataTreeDrive()
		{
			var contract = ActiveHostContract.ForLegacy();
			Assert.That(contract.PermitsLegacyDataTreeDrive(), Is.True);
			Assert.That(contract.PermitsLegacyDataTreeDrive("anything"), Is.True);
		}

		[Test]
		public void Avalonia_ForbidsLegacyDataTreeDrive_ByDefault()
		{
			var contract = ActiveHostContract.ForAvalonia();
			Assert.That(contract.PermitsLegacyDataTreeDrive(), Is.False);
			Assert.That(contract.PermitsLegacyDataTreeDrive("baseline-compare"), Is.False);
			Assert.That(() => contract.AssertLegacyDataTreeDriveAllowed(), Throws.InvalidOperationException);
		}

		[Test]
		public void Avalonia_PermitsLegacyDrive_OnlyForApprovedBaselineAdapter()
		{
			var contract = ActiveHostContract.ForAvalonia("baseline-compare");
			Assert.That(contract.PermitsLegacyDataTreeDrive("baseline-compare"), Is.True);
			Assert.That(contract.PermitsLegacyDataTreeDrive("other"), Is.False);
			Assert.That(contract.PermitsLegacyDataTreeDrive(), Is.False);
		}

		// Section 13.4: "command-menu-routing" is the approved adapter under which RecordEditView
		// lazily initializes the HIDDEN legacy DataTree + DTMenuHandler purely as the command-target
		// colleague chain for context menus — never shown, never the active surface.
		[Test]
		public void Avalonia_CommandMenuRouting_IsAnApprovableAdapter_ForContextMenuCommands()
		{
			var contract = ActiveHostContract.ForAvalonia("command-menu-routing");
			Assert.That(contract.PermitsLegacyDataTreeDrive("command-menu-routing"), Is.True);
			Assert.DoesNotThrow(() => contract.AssertLegacyDataTreeDriveAllowed("command-menu-routing"));
			Assert.That(contract.PermitsLegacyDataTreeDrive(), Is.False,
				"undeclared legacy drives stay forbidden while Avalonia is active");
		}
	}
}
