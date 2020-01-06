// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using FieldWorks.TestUtilities.Attributes;
using SIL.LCModel.Core.Attributes;
using SIL.LCModel.Utils.Attributes;
using SIL.TestUtilities;

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

// Allow creating COM objects from manifest file
[assembly: CreateComObjectsFromManifest]

// Initialize a do-nothing keyboard controller
[assembly: InitializeNoOpKeyboardController]

// Suppresses error beeps
[assembly: SuppressErrorBeeps]

// Handles any unhandled exceptions thrown on Windows Forms threads
[assembly: HandleApplicationThreadException]

// NOTE: it is important that OfflineSldr comes before InitializeIcu!
// Turns the SLDR API into offline mode
[assembly: OfflineSldr]

// Initialize ICU
[assembly: InitializeIcu(IcuVersion = 54)]
