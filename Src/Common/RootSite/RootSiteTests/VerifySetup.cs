// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using DiffEngine;
using NUnit.Framework;
using VerifyTests;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Assembly-level setup for Verify snapshot testing.
	/// Uses [SetUpFixture] because FieldWorks targets C# 8.0 (LangVersion=8.0)
	/// which does not support [ModuleInitializer] (requires C# 9).
	/// </summary>
	[SetUpFixture]
	public class VerifySetup
	{
		[OneTimeSetUp]
		public void Setup()
		{
			// Don't auto-open diff tool in CI environments
			if (Environment.GetEnvironmentVariable("CI") != null)
			{
				DiffRunner.Disabled = true;
			}
		}
	}
}
