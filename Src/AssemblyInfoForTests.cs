// Copyright (c) 2012-2013, SIL International.
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).

using NUnit.Framework;
using SIL.Utils.Attributes;

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
[assembly: SetCompanyAndProductForTests]

// Redirect HKCU if environment variable BUILDAGENT_SUBKEY is set
[assembly: RedirectHKCU]

// Allow creating COM objects from manifest file
[assembly: CreateComObjectsFromManifest]

// Initialize a do-nothing keyboard controller
[assembly: InitializeNoOpKeyboardController]
