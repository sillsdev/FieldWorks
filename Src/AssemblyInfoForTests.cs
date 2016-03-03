// Copyright (c) 2012-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.Utils.Attributes;
using SIL.FieldWorks.Common.FwUtils.Attributes;

// This file is for test fixtures for UI related projects, i.e. projects that do
// reference System.Windows.Forms et al.

// On Windows we need STA because of the COM objects. On Linux the tests hang when we use
// STA. Since we don't have a "real" COM implementation we don't really need it on Linux.
#if !__MonoCS__
	[assembly: RequiresSTA]
#endif

// Set stub for messagebox so that we don't pop up a message box when running tests.
[assembly: SetMessageBoxAdapter]

// Cleanup all singletons after running tests
[assembly: CleanupSingletons]

// Override company and product names
[assembly: SetCompanyAndProductAndIcuEnvForTests]

// Redirect HKCU if environment variable BUILDAGENT_SUBKEY is set
[assembly: RedirectHKCU]

// Allow creating COM objects from manifest file
[assembly: CreateComObjectsFromManifest]

// Initialize a do-nothing keyboard controller
[assembly: InitializeNoOpKeyboardController]