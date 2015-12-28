// Copyright (c) 2012-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.Utils.Attributes;

// This file is for test fixtures for UI independent projects, i.e. projects that don't
// reference System.Windows.Forms et al.

// On Windows we need STA because of the COM objects. On Linux the tests hang when we use
// STA. Since we don't have a "real" COM implementation we don't really need it on Linux.
#if !__MonoCS__
	[assembly: RequiresSTA]
#endif

// Cleanup all singletons after running tests
[assembly: CleanupSingletons]

// Redirect HKCU if environment variable BUILDAGENT_SUBKEY is set
[assembly: RedirectHKCU]

// Allow creating COM objects from manifest file
[assembly: CreateComObjectsFromManifest]

// Set ICU_DATA environment variable
[assembly: SetIcuDataEnvironmentVariable]
