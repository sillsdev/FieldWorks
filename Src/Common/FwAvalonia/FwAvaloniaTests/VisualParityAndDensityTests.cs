// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Task 6.9 — control-level visual parity capture: with Skia-backed headless drawing the region
	/// view renders real frames that are saved as parity artifacts (the Avalonia visual lane of the
	/// Path 3 bundle). Stable automation ids on user-facing controls are locked by the other suites.
	/// </summary>
	[TestFixture]
	public class VisualParityCaptureTests
	{
		private static ViewDefinitionModel Definition() => new ViewDefinitionModel(
			"LexEntry", "identity", "detail",
			new List<ViewNode>
			{
				new ViewNode("LexEntry/identity/#0", ViewNodeKind.Field, "Lexeme Form", null, "Form", "multistring",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "LexemeFormEditor", routing: SurfaceRouting.Product)
			},
			new List<ViewDiagnostic>());

		private sealed class Provider : IRegionValueProvider
		{
			public IReadOnlyList<RegionWsValue> GetValues(ViewNode fieldNode)
				=> new List<RegionWsValue> { new RegionWsValue("vern", "casa") };

			public IReadOnlyList<RegionChoiceOption> GetOptions(ViewNode fieldNode) => new List<RegionChoiceOption>();

			public string GetSelectedOptionKey(ViewNode fieldNode) => null;
		}

		[AvaloniaTest]
		public void RegionView_RendersARealFrame_SavedAsParityArtifact()
		{
			var model = LexicalEditRegionMapper.FromViewDefinition(Definition(), new Provider());
			var window = new Window { Content = new LexicalEditRegionView(model), Width = 420, Height = 160 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			using (var frame = window.CaptureRenderedFrame())
			{
				Assert.That(frame, Is.Not.Null, "Skia-backed headless drawing must produce a rendered frame");
				Assert.That(frame.PixelSize.Width, Is.GreaterThan(0));
				Assert.That(frame.PixelSize.Height, Is.GreaterThan(0));

				var artifact = Path.Combine(TestContext.CurrentContext.WorkDirectory,
					"avalonia-region-first-slice.png");
				frame.Save(artifact);
				Assert.That(new FileInfo(artifact).Length, Is.GreaterThan(0),
					"the captured frame is the Avalonia visual lane artifact for Path 3 bundles");
				TestContext.WriteLine($"avalonia visual parity frame: {artifact}");
			}
		}
	}

	/// <summary>
	/// Task 6.6 — screen-local keyboard commands: Enter commits (validation-gated through the same
	/// Save path), Escape cancels.
	/// </summary>
	[TestFixture]
	public class RegionEditorShortcutTests
	{
		private static (LexicalEditRegionView view, FakeRegionEditContext context) ShowEditable()
		{
			var model = new LexicalEditRegionModel("LexEntry", "identity",
				new List<LexicalEditRegionField>(), new List<ViewDiagnostic>());
			var context = new FakeRegionEditContext();
			var view = new LexicalEditRegionView(model, context);
			var window = new Window { Content = view, Width = 400, Height = 160 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (view, context);
		}

		private static void Press(LexicalEditRegionView view, Key key)
		{
			view.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = key,
				Source = view
			});
			Dispatcher.UIThread.RunJobs();
		}

		[AvaloniaTest]
		public void Enter_CommitsThroughTheValidatedSavePath()
		{
			var (view, context) = ShowEditable();
			Press(view, Key.Enter);
			Assert.That(context.CommitCount, Is.EqualTo(1));
		}

		[AvaloniaTest]
		public void Enter_WithValidationErrors_DoesNotCommit()
		{
			var (view, context) = ShowEditable();
			context.ValidateResult = new List<string> { "required" };
			Press(view, Key.Enter);
			Assert.That(context.CommitCount, Is.EqualTo(0));
		}

		[AvaloniaTest]
		public void Escape_Cancels()
		{
			var (view, context) = ShowEditable();
			Press(view, Key.Escape);
			Assert.That(context.CancelCount, Is.EqualTo(1));
		}
	}

	/// <summary>
	/// Task 6.7 — the shared density tokens are a locked gate before broad editor rollout: changing
	/// the compact-baseline values is a reviewed parity decision, not a drive-by style tweak.
	/// </summary>
	[TestFixture]
	public class DensityTokenGateTests
	{
		[Test]
		public void DensityTokens_MatchTheCompactWinFormsBaseline()
		{
			Assert.That(FwAvaloniaDensity.LabelColumnWidth, Is.EqualTo(96d));
			Assert.That(FwAvaloniaDensity.WsAbbrevWidth, Is.EqualTo(28d));
			Assert.That(FwAvaloniaDensity.RowSpacing, Is.EqualTo(1d));
			Assert.That(FwAvaloniaDensity.FieldSpacing, Is.EqualTo(2d));
			Assert.That(FwAvaloniaDensity.EditorPadding, Is.EqualTo(new Thickness(3, 1, 3, 1)));
			Assert.That(FwAvaloniaDensity.SliceMargin, Is.EqualTo(new Thickness(4, 2, 4, 2)));
		}
	}
}
