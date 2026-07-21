// Copyright (c) 2026 SIL Global
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Unit tests for StringSliceUtils.
	/// </summary>
	[TestFixture]
	public class StringSliceUtilsTests
	{
		private const int kwsVern = 101;
		private const int kwsEmptyDefault = 102;
		private const int kwsAnal = 103;

		/// <summary>
		/// A slice created with a specific ws (e.g. a custom field configured as First
		/// Vernacular) should use that ws for typing in an empty field (LT-22630).
		/// </summary>
		[Test]
		public void GetWsForEmptyField_PrefersSpecifiedWs()
		{
			Assert.That(StringSliceUtils.GetWsForEmptyField(kwsVern, kwsEmptyDefault, kwsAnal),
				Is.EqualTo(kwsVern));
		}

		/// <summary>
		/// With no ws specified, the 'wsempty' configuration default should win.
		/// </summary>
		[Test]
		public void GetWsForEmptyField_FallsBackToEmptyDefault()
		{
			Assert.That(StringSliceUtils.GetWsForEmptyField(-1, kwsEmptyDefault, kwsAnal),
				Is.EqualTo(kwsEmptyDefault));
		}

		/// <summary>
		/// With nothing configured, the first analysis ws is the last resort (LT-22145).
		/// </summary>
		[Test]
		public void GetWsForEmptyField_FallsBackToDefaultAnalysis()
		{
			Assert.That(StringSliceUtils.GetWsForEmptyField(-1, 0, kwsAnal), Is.EqualTo(kwsAnal));
		}

		/// <summary>
		/// Magic ws constants are negative and must not be treated as real ws handles.
		/// </summary>
		[Test]
		public void GetWsForEmptyField_IgnoresNonPositiveValues()
		{
			Assert.That(StringSliceUtils.GetWsForEmptyField(-6, -3, kwsAnal), Is.EqualTo(kwsAnal));
		}
	}
}
