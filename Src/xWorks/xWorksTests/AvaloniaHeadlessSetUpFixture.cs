// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia;
using Avalonia.Headless;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;

// An assembly-level [SetUpFixture] in the GLOBAL namespace runs its OneTimeSetUp once for the whole
// assembly, BEFORE any test in any namespace. We use that to force the headless Avalonia platform
// before the first product surface (RecordEditView/RecordBrowseView) constructs an Avalonia host.

[SetUpFixture]
public sealed class AvaloniaHeadlessSetUpFixture
{
	/// <summary>
	/// Forces FieldWorks' in-process Avalonia runtime onto the HEADLESS windowing platform for the whole
	/// xWorksTests run. Several integration tests drive the real product surface
	/// (RecordEditView/RecordBrowseView, the region/browse hosts, AvaloniaDialogHost), which funnel
	/// through <see cref="FwAvaloniaRuntime.EnsureInitialized"/>. In production that builds the REAL Win32
	/// Avalonia platform, so any region flyout, dialog, or popup raised during a test becomes a real
	/// on-screen OS window that flashes for a fraction of a second and can steal keypresses from whatever
	/// the developer is doing. Mirroring the dedicated FwAvaloniaTests headless builder (Skia + headless,
	/// drawing on) keeps that content off-screen while still constructing and laying it out, so the
	/// logic-only assertions these integration tests make are unaffected.
	///
	/// The production FwAvalonia DLL never references Avalonia.Headless; the headless builder is supplied
	/// here from the test assembly through the <see cref="FwAvaloniaRuntime.AppBuilderOverride"/> hook.
	/// </summary>
	[OneTimeSetUp]
	public void ForceHeadlessAvaloniaPlatform()
	{
		FwAvaloniaRuntime.AppBuilderOverride = BuildHeadlessAvaloniaApp;
	}

	internal static AppBuilder BuildHeadlessAvaloniaApp()
		=> AppBuilder.Configure<FwAvaloniaApp>()
			.UseSkia()
			.UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
