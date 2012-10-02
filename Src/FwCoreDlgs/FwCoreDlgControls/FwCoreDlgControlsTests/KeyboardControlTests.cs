// Copyright (c) 2010, SIL International. All Rights Reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// Original author: MarkS 2010-11-22 KeyboardControlTests.cs

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgControlsTests
{
	/// <summary/>
	[TestFixture]
	public class KeyboardControlTests
	{
		/// <summary>
		/// Get available keyboards. Don't run automatically since automated test
		/// environment may not have the right keyboards set.
		/// </summary>
		[Test]
		[Category("ByHand")]
		[Platform(Include = "Linux", Reason = "Linux specific test")]
		public void GetAvailableKeyboards_GetsKeyboards()
		{
			var expectedKeyboards = new List<string>();
			expectedKeyboards.Add("ispell (m17n)");

			List<string> actualKeyboards = ReflectionHelper.CallStaticMethod("FwCoreDlgControls.dll",
				"SIL.FieldWorks.FwCoreDlgControls.KeyboardControl", "GetAvailableKeyboards",
				new object[] {null}) as List<string>;

			Assert.That(actualKeyboards, Is.EquivalentTo(expectedKeyboards),
				"Available keyboards does not match expected.");
		}
	}
}