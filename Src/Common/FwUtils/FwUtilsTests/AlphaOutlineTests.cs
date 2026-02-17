// Copyright (c) 2006-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class AlphaOutlineTests // can't derive from BaseTest because of dependencies
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the NumToAlphaOutline method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NumToAlphaOutline()
		{
			Assert.That(AlphaOutline.NumToAlphaOutline(1), Is.EqualTo("A"));
			Assert.That(AlphaOutline.NumToAlphaOutline(26), Is.EqualTo("Z"));
			Assert.That(AlphaOutline.NumToAlphaOutline(27), Is.EqualTo("AA"));
			Assert.That(AlphaOutline.NumToAlphaOutline(52), Is.EqualTo("ZZ"));
			Assert.That(AlphaOutline.NumToAlphaOutline(53), Is.EqualTo("AAA"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AlphaToOutlineNum method with valid values
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AlphaToOutlineNum_Valid()
		{
			Assert.That(AlphaOutline.AlphaOutlineToNum("A"), Is.EqualTo(1));
			Assert.That(AlphaOutline.AlphaOutlineToNum("a"), Is.EqualTo(1));
			Assert.That(AlphaOutline.AlphaOutlineToNum("Z"), Is.EqualTo(26));
			Assert.That(AlphaOutline.AlphaOutlineToNum("AA"), Is.EqualTo(27));
			Assert.That(AlphaOutline.AlphaOutlineToNum("ZZ"), Is.EqualTo(52));
			Assert.That(AlphaOutline.AlphaOutlineToNum("AAA"), Is.EqualTo(53));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AlphaToOutlineNum method with invalid values
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AlphaToOutlineNum_Invalid()
		{
			Assert.That(AlphaOutline.AlphaOutlineToNum(string.Empty), Is.EqualTo(-1));
			Assert.That(AlphaOutline.AlphaOutlineToNum(null), Is.EqualTo(-1));
			Assert.That(AlphaOutline.AlphaOutlineToNum("7"), Is.EqualTo(-1));
			Assert.That(AlphaOutline.AlphaOutlineToNum("A1"), Is.EqualTo(-1));
			Assert.That(AlphaOutline.AlphaOutlineToNum("AB"), Is.EqualTo(-1));
			Assert.That(AlphaOutline.AlphaOutlineToNum("AAC"), Is.EqualTo(-1));
			Assert.That(AlphaOutline.AlphaOutlineToNum("?"), Is.EqualTo(-1));
		}
	}
}
