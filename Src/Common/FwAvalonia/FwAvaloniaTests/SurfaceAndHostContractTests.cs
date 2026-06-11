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
			var decision = _service.Decide("New", "posEdit");
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
	}

	/// <summary>Proves the task 3.5 host/surface contract is satisfiable by a fake host.</summary>
	[TestFixture]
	public class HostSurfaceContractTests
	{
		private sealed class FakeSurface : ILexicalEditSurface
		{
			public FakeSurface(LexicalEditSurfaceKind kind) { Kind = kind; }
			public LexicalEditSurfaceKind Kind { get; }
			public bool IsInitialized { get; private set; }
			public object ShownRecord { get; private set; }
			public bool Visible { get; private set; }
			public void EnsureInitialized() => IsInitialized = true;
			public void ShowRecord(object record) { EnsureInitialized(); ShownRecord = record; Visible = true; }
			public void Hide() => Visible = false;
			public bool TrySetFocus() => Visible;
			public bool TryShowContextMenu(object context) => Visible;
			public void PrepareToReplace() { }
		}

		private sealed class FakeHost : ILexicalEditHost
		{
			private readonly FakeSurface _legacy = new FakeSurface(LexicalEditSurfaceKind.Legacy);
			private readonly FakeSurface _avalonia = new FakeSurface(LexicalEditSurfaceKind.Avalonia);
			public LexicalEditSurfaceKind ActiveSurface { get; private set; } = LexicalEditSurfaceKind.Legacy;
			public System.Collections.Generic.IReadOnlyList<ILexicalEditSurface> Surfaces => new ILexicalEditSurface[] { _legacy, _avalonia };
			public FakeSurface Legacy => _legacy;
			public FakeSurface Avalonia => _avalonia;

			public void ReplaceSurface(LexicalEditSurfaceKind kind, object record)
			{
				var active = kind == LexicalEditSurfaceKind.Avalonia ? _avalonia : _legacy;
				var inactive = kind == LexicalEditSurfaceKind.Avalonia ? _legacy : _avalonia;
				inactive.PrepareToReplace();
				inactive.Hide();
				active.ShowRecord(record);
				ActiveSurface = kind;
			}
		}

		[Test]
		public void ReplaceSurface_ActivatesOnlyTheChosenSurface()
		{
			var host = new FakeHost();
			host.ReplaceSurface(LexicalEditSurfaceKind.Avalonia, "entry-1");

			Assert.That(host.ActiveSurface, Is.EqualTo(LexicalEditSurfaceKind.Avalonia));
			Assert.That(host.Avalonia.Visible, Is.True);
			Assert.That(host.Avalonia.ShownRecord, Is.EqualTo("entry-1"));
			Assert.That(host.Legacy.Visible, Is.False);
			// The inactive (legacy) surface was never driven to show a record.
			Assert.That(host.Legacy.ShownRecord, Is.Null);
		}
	}
}
