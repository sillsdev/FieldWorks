// Copyright (c) 2012-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.CoreImpl.Attributes;

// This file is for test fixtures for UI independent projects, i.e. projects that don't
// reference System.Windows.Forms et al.

// Cleanup all singletons after running tests
[assembly: CleanupSingletons]

// Redirect HKCU if environment variable BUILDAGENT_SUBKEY is set
[assembly: RedirectHKCU]

// Set ICU_DATA env variable
[assembly: SetIcuEnvForTests]
