// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;

namespace FwBuildTasks
{
	/// <summary>
	/// Characterizes GenerateTokenKeys.ParseThickness against the Avalonia Thickness.Parse
	/// grammar it replaced, so a baked constant keeps matching what Avalonia resolves at runtime
	/// for the same token text.
	/// </summary>
	[TestFixture]
	public class GenerateTokenKeysTests
	{
		[TestCase("12", 12, 12, 12, 12)]
		[TestCase("12,4", 12, 4, 12, 4)]
		[TestCase("12,4,8,2", 12, 4, 8, 2)]
		public void ParseThickness_CommaSeparated_ExpandsLikeAvalonia(
			string text, double left, double top, double right, double bottom)
		{
			AssertSides(text, left, top, right, bottom);
		}

		/// <remarks>The form a comma-splitting parser gets wrong; Avalonia accepts it.</remarks>
		[TestCase("8 4 8 4", 8, 4, 8, 4)]
		[TestCase("8 4", 8, 4, 8, 4)]
		[TestCase("12, 4", 12, 4, 12, 4)]
		[TestCase("  12 , 4  ", 12, 4, 12, 4)]
		public void ParseThickness_WhitespaceSeparated_ExpandsLikeAvalonia(
			string text, double left, double top, double right, double bottom)
		{
			AssertSides(text, left, top, right, bottom);
		}

		[TestCase("-2", -2, -2, -2, -2)]
		[TestCase("1.5,0.25", 1.5, 0.25, 1.5, 0.25)]
		[TestCase("1e2,0", 100, 0, 100, 0)]
		public void ParseThickness_InvariantNumberForms_AreAccepted(
			string text, double left, double top, double right, double bottom)
		{
			AssertSides(text, left, top, right, bottom);
		}

		[Test]
		public void ParseThickness_NaN_IsAccepted()
		{
			Assert.That(GenerateTokenKeys.ParseThickness("NaN").Left, Is.NaN);
		}

		[TestCase("1,2,3", TestName = "ParseThickness_ThreeValues_Throws")]
		[TestCase("1,2,3,4,5", TestName = "ParseThickness_FiveValues_Throws")]
		[TestCase("12,,4", TestName = "ParseThickness_EmptyComponent_Throws")]
		[TestCase("12,", TestName = "ParseThickness_TrailingSeparator_Throws")]
		[TestCase("wide", TestName = "ParseThickness_NonNumeric_Throws")]
		public void ParseThickness_MalformedInput_Throws(string text)
		{
			Assert.That(() => GenerateTokenKeys.ParseThickness(text), Throws.InstanceOf<FormatException>());
		}

		/// <summary>The generated literal collapses to the shortest equivalent
		/// constructor.</summary>
		[TestCase("12", "new Thickness(12)")]
		[TestCase("12,4", "new Thickness(12, 4)")]
		[TestCase("12,4,8,2", "new Thickness(12, 4, 8, 2)")]
		[TestCase("1.5", "new Thickness(1.5)")]
		public void ThicknessLiteral_RoundTripsTokenText(string text, string expected)
		{
			Assert.That(GenerateTokenKeys.ThicknessLiteral(GenerateTokenKeys.ParseThickness(text)),
				Is.EqualTo(expected));
		}

		private static void AssertSides(string text, double left, double top, double right, double bottom)
		{
			var parsed = GenerateTokenKeys.ParseThickness(text);
			Assert.Multiple(() =>
			{
				Assert.That(parsed.Left, Is.EqualTo(left), "Left");
				Assert.That(parsed.Top, Is.EqualTo(top), "Top");
				Assert.That(parsed.Right, Is.EqualTo(right), "Right");
				Assert.That(parsed.Bottom, Is.EqualTo(bottom), "Bottom");
			});
		}
	}
}
