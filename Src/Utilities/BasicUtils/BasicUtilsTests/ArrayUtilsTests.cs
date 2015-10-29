// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SIL.Utils
{
	/// <summary>
	/// Test the ArrayUtils methods
	/// </summary>
	[TestFixture]
	public class ArrayUtilsTests
	{
		[Test]
		public void AreEqual()
		{
			Assert.That(ArrayUtils.AreEqual(new int[0],new int[0]), Is.True);
			Assert.That(ArrayUtils.AreEqual(new int[0], new [] {1, 2}), Is.False);
			Assert.That(ArrayUtils.AreEqual(new[] { 1, 2 }, new[] { 1, 2 }), Is.True);
			Assert.That(ArrayUtils.AreEqual(new[] { 1}, new[] { 1, 2 }), Is.False);
			Assert.That(ArrayUtils.AreEqual(new[] { 2, 2 }, new[] { 1, 2 }), Is.False);
			Assert.That(ArrayUtils.AreEqual(new[] { 1, 2 }, new[] { 1, 3 }), Is.False);
			Assert.That(ArrayUtils.AreEqual(new int[0], new[] { 1}), Is.False);
			Assert.That(ArrayUtils.AreEqual(new[] { 1 }, new int[0]), Is.False);
			Assert.That(ArrayUtils.AreEqual(new[] { 1, 2 }, new[] { 1 }), Is.False);
			Assert.That(ArrayUtils.AreEqual(null, null), Is.True);
			Assert.That(ArrayUtils.AreEqual(null, new[] { 1 }), Is.False);
			Assert.That(ArrayUtils.AreEqual(new[] { 1, 2 }, null), Is.False);
			// These two are dubious, but let's lock in the current behavior unless we really decide to change it.
			Assert.That(ArrayUtils.AreEqual(new int[0], null), Is.False);
			Assert.That(ArrayUtils.AreEqual(null, new int[0]), Is.False);
		}
	}
}
