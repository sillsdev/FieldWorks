// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.LCModel.Utils;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Tests for CharContextCtrl.
	/// </summary>
	[TestFixture]
	public class CharContextCtrlTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Tests the NormalizeFileData method against some Hebrew data that normalizes
		/// differently in ICU and .Net
		/// </summary>
		[Test]
		public void NormalizeFileData_Hebrew()
		{
			using (var ctrl = new CharContextCtrl())
			{
				ctrl.Initialize(Cache, Cache.ServiceLocator.WritingSystems,
				null, null, null, null);

				// First string is the normalized order that ICU produces.
				// Second string is the normalized order that .Net produces.
				ReflectionHelper.SetField(ctrl, "m_fileData", new[] { "\u05E9\u05c1\u05b4\u0596", "\u05E9\u05b4\u05c1\u0596" });

				ReflectionHelper.CallMethod(ctrl, "NormalizeFileData");

				var results = (string[])ReflectionHelper.GetField(ctrl, "m_fileData");
				Assert.AreEqual(2, results.Length);
				Assert.AreEqual("\u05E9\u05c1\u05b4\u0596", results[0], "Expect ICU-style normalization");
				Assert.AreEqual("\u05E9\u05c1\u05b4\u0596", results[1], "Expect ICU-style normalization");
			}
		}

		/// <summary>
		/// Tests the NormalizeFileData method against some data that normalizes differently in
		/// NFKD (compatibility decomposition) and NFD (canonical decomposition). This is to
		/// ensure that we're doing NFD. TE-8384
		/// </summary>
		[Test]
		public void NormalizeFileData_EnsureNFD()
		{
			using (var ctrl = new CharContextCtrl())
			{
				ctrl.Initialize(Cache, Cache.ServiceLocator.WritingSystems, null, null, null, null);

				// First string is the normalized order that ICU produces.
				// Second string is the normalized order that .Net produces.
				ReflectionHelper.SetField(ctrl, "m_fileData", new[] { "\u2074" });

				ReflectionHelper.CallMethod(ctrl, "NormalizeFileData");

				var results = (string[])ReflectionHelper.GetField(ctrl, "m_fileData");
				Assert.AreEqual(1, results.Length);
				Assert.AreEqual("\u2074", results[0], "Expect ICU-style normalization");
			}
		}
	}
}