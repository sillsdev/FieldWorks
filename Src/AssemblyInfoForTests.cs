// Copyright (c) 2012-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.Attributes;
using SIL.FieldWorks.Common.FwUtils.Attributes;
using SIL.LCModel.Utils.Attributes;
using SIL.TestUtilities;
using System.Reflection;

// This file is for test fixtures for UI related projects, i.e. projects that do
// reference System.Windows.Forms et al.

[assembly: RequiresSTAOnWindows]

// Set stub for messagebox so that we don't pop up a message box when running tests.
[assembly: SetMessageBoxAdapter]

// Cleanup all singletons after running tests
[assembly: CleanupSingletons]

// Initialize registry helper
[assembly: InitializeFwRegistryHelper]

// Redirect HKCU if environment variable BUILDAGENT_SUBKEY is set
[assembly: RedirectHKCU]

// Initialize a do-nothing keyboard controller
[assembly: InitializeNoOpKeyboardController]

// Suppresses error beeps
[assembly: SuppressErrorBeeps]

// Handles any unhandled exceptions thrown on Windows Forms threads
[assembly: HandleApplicationThreadException]

// Initialize ICU
[assembly: InitializeIcu(IcuVersion = 70)]

// Turns the SLDR API into offline mode
[assembly: OfflineSldr]

// Allow creating COM objects from manifest file important that it comes after InitializeIcu
[assembly: CreateComObjectsFromManifest]

// This is for testing VersionInfoProvider in FwUtils
[assembly: AssemblyInformationalVersion("9.0.6 45470 Alpha")]