// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.CoreImpl.KernelInterfaces;

namespace SIL.CoreImpl.Text
{
	[TestFixture]
	public class TsStrBldrTests
	{
		private const string EnglishFont1Text = "This is a test!";
		private const string EnglishFont2Text = "How are you today?";
		// the space goes in the first run
		private const string MixedFontText = EnglishFont1Text + " " + EnglishFont2Text;
		private const string SpanishText = "¡Esto es una prueba!";
		// the space goes in the English run
		private const string MixedWSText = EnglishFont1Text + " " + SpanishText;
		private const int EnglishWS = 1;
		private const int SpanishWS = 2;
		private const int FrenchWS = 3;
		private const string Font1 = "Times New Roman";
		private const string Font2 = "Arial";
		private const string Font3 = "Courier New";

		[Test]
		public void Replace_EmptyBldrNonEmptyText_InsertsText()
		{
			TsStrBldr tsb = CreateEmptyBldr();
			tsb.Replace(0, 0, "text", new TsTextProps(FrenchWS));
			Assert.That(tsb.Text, Is.EqualTo("text"));
			Assert.That(tsb.RunCount, Is.EqualTo(1));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(FrenchWS));
		}

		[Test]
		public void Replace_EmptyBldrEmptyText_UpdatesProperties()
		{
			TsStrBldr tsb = CreateEmptyBldr();
			tsb.Replace(0, 0, string.Empty, new TsTextProps(FrenchWS));
			Assert.That(tsb.Text, Is.Null);
			Assert.That(tsb.RunCount, Is.EqualTo(1));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(FrenchWS));
		}

		[Test]
		public void Replace_OneRunBldrEmptyTextEmptyRange_DoNotUpdateProperties()
		{
			TsStrBldr tsb = CreateOneRunBldr();
			tsb.Replace(0, 0, string.Empty, new TsTextProps(FrenchWS));
			Assert.That(tsb.Text, Is.EqualTo(EnglishFont1Text));
			Assert.That(tsb.RunCount, Is.EqualTo(1));
			int var;
			Assert.That(tsb.get_PropertiesAt(0).GetIntPropValues((int) FwTextPropType.ktptWs, out var), Is.EqualTo(EnglishWS));
		}

		[Test]
		public void Replace_OneRunBldrNonEmptyTextEmptyRangeNullProps_InsertsText()
		{
			TsStrBldr tsb = CreateOneRunBldr();
			tsb.Replace(tsb.Length, tsb.Length, " text", null);
			Assert.That(tsb.Text, Is.EqualTo(EnglishFont1Text + " text"));
			Assert.That(tsb.RunCount, Is.EqualTo(1));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void Replace_OneRunBldrEmptyTextFullRange_RemovesText()
		{
			TsStrBldr tsb = CreateOneRunBldr();
			tsb.Replace(0, tsb.Length, string.Empty, new TsTextProps(FrenchWS));
			Assert.That(tsb.Text, Is.Null);
			Assert.That(tsb.RunCount, Is.EqualTo(1));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(FrenchWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void Replace_OneRunBldrEmptyTextPartialRange_RemovesText()
		{
			TsStrBldr tsb = CreateOneRunBldr();
			tsb.Replace(5, 8, string.Empty, new TsTextProps(FrenchWS));
			Assert.That(tsb.Text, Is.EqualTo("This a test!"));
			Assert.That(tsb.RunCount, Is.EqualTo(1));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void Replace_OneRunBldrNullTextPartialRange_RemovesText()
		{
			TsStrBldr tsb = CreateOneRunBldr();
			tsb.Replace(5, 8, null, new TsTextProps(FrenchWS));
			Assert.That(tsb.Text, Is.EqualTo("This a test!"));
			Assert.That(tsb.RunCount, Is.EqualTo(1));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void Replace_OneRunBldrNonEmptyTextPartialRange_ReplacesText()
		{
			TsStrBldr tsb = CreateOneRunBldr();
			tsb.Replace(5, 7, "was", new TsTextProps(FrenchWS));
			Assert.That(tsb.Text, Is.EqualTo("This was a test!"));
			Assert.That(tsb.RunCount, Is.EqualTo(3));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(FrenchWS));
			Assert.That(GetWS(tsb, 2), Is.EqualTo(EnglishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(5));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(5));
			Assert.That(ichLim, Is.EqualTo(8));
			tsb.GetBoundsOfRun(2, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(8));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void Replace_OneRunBldrNonEmptyTextFullRange_ReplacesText()
		{
			TsStrBldr tsb = CreateOneRunBldr();
			tsb.Replace(0, tsb.Length, "A new text.", new TsTextProps(FrenchWS));
			Assert.That(tsb.Text, Is.EqualTo("A new text."));
			Assert.That(tsb.RunCount, Is.EqualTo(1));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(FrenchWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void Replace_TwoRunBldrNonEmptyTextOverlapsBoth_ReplacesText()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.Replace(5, 28, "was", new TsTextProps(FrenchWS));
			Assert.That(tsb.Text, Is.EqualTo("This was prueba!"));
			Assert.That(tsb.RunCount, Is.EqualTo(3));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(FrenchWS));
			Assert.That(GetWS(tsb, 2), Is.EqualTo(SpanishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(5));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(5));
			Assert.That(ichLim, Is.EqualTo(8));
			tsb.GetBoundsOfRun(2, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(8));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void Replace_TwoRunBldrNonEmptyTextOverlapsBothSamePropertiesFirst_ReplacesText()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.Replace(5, 28, "was", new TsTextProps(EnglishWS));
			Assert.That(tsb.Text, Is.EqualTo("This was prueba!"));
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(SpanishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(8));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(8));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void Replace_TwoRunBldrNonEmptyTextOverlapsBothSamePropertiesSecond_ReplacesText()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.Replace(5, 28, "was", new TsTextProps(SpanishWS));
			Assert.That(tsb.Text, Is.EqualTo("This was prueba!"));
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(SpanishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(5));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(5));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void Replace_TwoRunBldrEmptyTextOverlapsBoth_RemovesText()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.Replace(5, 29, String.Empty, null);
			Assert.That(tsb.Text, Is.EqualTo("This prueba!"));
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(SpanishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(5));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(5));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void Replace_IchMinOutOfRange_Throws()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			Assert.That(() => tsb.Replace(-1, 36, "text", new TsTextProps(FrenchWS)), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void Replace_IchLimOutOfRange_Throws()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			Assert.That(() => tsb.Replace(25, 37, "text", new TsTextProps(FrenchWS)), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void Replace_IchMinGreaterThanIchLim_Throws()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			Assert.That(() => tsb.Replace(25, 24, "text", new TsTextProps(FrenchWS)), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void ReplaceTsString_OneRunBldrStringNullEmptyRange_DoesNotUpdateText()
		{
			TsStrBldr tsb = CreateOneRunBldr();
			tsb.ReplaceTsString(1, 1, null);
			Assert.That(tsb.Text, Is.EqualTo(EnglishFont1Text));
		}

		[Test]
		public void ReplaceTsString_EmptyBldrNonEmptyText_InsertsText()
		{
			TsStrBldr tsb = CreateEmptyBldr();
			tsb.ReplaceTsString(0, 0, new TsString("text", FrenchWS));
			Assert.That(tsb.Text, Is.EqualTo("text"));
			Assert.That(tsb.RunCount, Is.EqualTo(1));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(FrenchWS));
		}

		[Test]
		public void ReplaceTsString_EmptyBldrEmptyText_UpdatesProperties()
		{
			TsStrBldr tsb = CreateEmptyBldr();
			tsb.ReplaceTsString(0, 0, new TsString(FrenchWS));
			Assert.That(tsb.Text, Is.Null);
			Assert.That(tsb.RunCount, Is.EqualTo(1));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(FrenchWS));
		}

		[Test]
		public void ReplaceTsString_OneRunBldrEmptyTextEmptyRange_DoNotUpdateProperties()
		{
			TsStrBldr tsb = CreateOneRunBldr();
			tsb.ReplaceTsString(0, 0, new TsString(FrenchWS));
			Assert.That(tsb.Text, Is.EqualTo(EnglishFont1Text));
			Assert.That(tsb.RunCount, Is.EqualTo(1));
			int var;
			Assert.That(tsb.get_PropertiesAt(0).GetIntPropValues((int) FwTextPropType.ktptWs, out var), Is.EqualTo(EnglishWS));
		}

		[Test]
		public void ReplaceTsString_IchMinOutOfRange_Throws()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			Assert.That(() => tsb.ReplaceTsString(-1, 36, new TsString("text", FrenchWS)), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void ReplaceTsString_IchLimOutOfRange_Throws()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			Assert.That(() => tsb.ReplaceTsString(25, 37, new TsString("text", FrenchWS)), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void ReplaceTsString_IchMinGreaterThanIchLim_Throws()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			Assert.That(() => tsb.ReplaceTsString(25, 24, new TsString("text", FrenchWS)), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void SetProperties_EmptyBldr_UpdatesProperties()
		{
			TsStrBldr tsb = CreateEmptyBldr();
			Assert.That(GetWS(tsb, 0), Is.EqualTo(-1));
			tsb.SetProperties(0, 0, new TsTextProps(EnglishWS));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
		}

		[Test]
		public void SetProperties_OneRunBldrEmptyRange_DoesNotUpdateProperties()
		{
			TsStrBldr tsb = CreateOneRunBldr();
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			tsb.SetProperties(0, 0, new TsTextProps(SpanishWS));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
		}

		[Test]
		public void SetProperties_OneRunBldrFullRange_UpdatesProperties()
		{
			TsStrBldr tsb = CreateOneRunBldr();
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			tsb.SetProperties(0, tsb.Length, new TsTextProps(SpanishWS));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(SpanishWS));
		}

		[Test]
		public void SetProperties_OneRunBldrPartialRangeFromStartToMiddle_CreatesOneExtraRun()
		{
			TsStrBldr tsb = CreateOneRunBldr();
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			Assert.That(tsb.RunCount, Is.EqualTo(1));
			tsb.SetProperties(0, 1, new TsTextProps(SpanishWS));
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(SpanishWS));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(EnglishWS));
		}

		[Test]
		public void SetProperties_OneRunBldrPartialRangeFromMiddleToEnd_CreatesOneExtraRun()
		{
			TsStrBldr tsb = CreateOneRunBldr();
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			Assert.That(tsb.RunCount, Is.EqualTo(1));
			tsb.SetProperties(1, tsb.Length, new TsTextProps(SpanishWS));
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(SpanishWS));
		}

		[Test]
		public void SetProperties_OneRunBldrPartialRangeFromMiddleToMiddle_CreatesTwoExtraRuns()
		{
			TsStrBldr tsb = CreateOneRunBldr();
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			Assert.That(tsb.RunCount, Is.EqualTo(1));
			tsb.SetProperties(1, tsb.Length - 1, new TsTextProps(SpanishWS));
			Assert.That(tsb.RunCount, Is.EqualTo(3));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(SpanishWS));
			Assert.That(GetWS(tsb, 2), Is.EqualTo(EnglishWS));
		}

		[Test]
		public void SetProperties_TwoRunBldrRangeOverlapsBoth_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetProperties(6, 27, new TsTextProps(FrenchWS));
			Assert.That(tsb.RunCount, Is.EqualTo(3));
			Assert.That(tsb.get_RunText(1), Is.EqualTo("s a test! ¡Esto es un"));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(FrenchWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(6));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(6));
			Assert.That(ichLim, Is.EqualTo(27));
			tsb.GetBoundsOfRun(2, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(27));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetProperties_TwoRunBldrRangeOverlapsFirstReplacesSecond_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetProperties(6, tsb.Length, new TsTextProps(FrenchWS));
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(tsb.get_RunText(1), Is.EqualTo("s a test! ¡Esto es una prueba!"));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(FrenchWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(6));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(6));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetProperties_TwoRunBldrRangeReplacesFirstOverlapsSecond_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetProperties(0, 27, new TsTextProps(FrenchWS));
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(tsb.get_RunText(0), Is.EqualTo("This is a test! ¡Esto es un"));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(FrenchWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(27));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(27));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetProperties_TwoRunBldrRangeOverlapsBothSamePropertiesAsFirst_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetProperties(6, 27, new TsTextProps(EnglishWS));
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(tsb.get_RunText(0), Is.EqualTo("This is a test! ¡Esto es un"));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(27));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(27));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetProperties_TwoRunBldrRangeOverlapsBothSamePropertiesAsLast_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetProperties(6, 27, new TsTextProps(SpanishWS));
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(tsb.get_RunText(1), Is.EqualTo("s a test! ¡Esto es una prueba!"));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(SpanishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(6));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(6));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetProperties_TwoRunBldrReplacesFirstWithoutOverlappingSecond_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetProperties(0, EnglishFont1Text.Length + 1, new TsTextProps(FrenchWS));
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(tsb.get_RunText(0), Is.EqualTo("This is a test! "));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(FrenchWS));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(SpanishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(EnglishFont1Text.Length + 1));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(EnglishFont1Text.Length + 1));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetProperties_TwoRunBldrReplacesFirstAndOverlapsSecondByOneCharacter_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetProperties(0, EnglishFont1Text.Length + 2, new TsTextProps(FrenchWS));
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(tsb.get_RunText(0), Is.EqualTo("This is a test! ¡"));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(FrenchWS));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(SpanishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(EnglishFont1Text.Length + 2));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(EnglishFont1Text.Length + 2));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetProperties_TwoRunBldrAlmostReplacesFirstButLeavesJustOneCharacter_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetProperties(0, EnglishFont1Text.Length, new TsTextProps(FrenchWS));
			Assert.That(tsb.RunCount, Is.EqualTo(3));
			Assert.That(tsb.get_RunText(0), Is.EqualTo("This is a test!"));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(FrenchWS));
			Assert.That(tsb.get_RunText(1), Is.EqualTo(" "));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(EnglishWS));
			Assert.That(GetWS(tsb, 2), Is.EqualTo(SpanishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(EnglishFont1Text.Length));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(EnglishFont1Text.Length));
			Assert.That(ichLim, Is.EqualTo(EnglishFont1Text.Length + 1));
			tsb.GetBoundsOfRun(2, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(EnglishFont1Text.Length + 1));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetProperties_TwoRunBldrReplacesFirstWithoutOverlappingSecondSamePropertiesAsFirst_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetProperties(0, EnglishFont1Text.Length + 1, new TsTextProps(EnglishWS));
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(tsb.get_RunText(0), Is.EqualTo("This is a test! "));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			Assert.That(tsb.get_RunText(1), Is.EqualTo("¡Esto es una prueba!"));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(SpanishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(EnglishFont1Text.Length + 1));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(EnglishFont1Text.Length + 1));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetProperties_TwoRunBldrReplacesFirstAndOverlapsSecondByOneCharacterSamePropertiesAsFirst_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetProperties(0, EnglishFont1Text.Length + 2, new TsTextProps(EnglishWS));
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(tsb.get_RunText(0), Is.EqualTo("This is a test! ¡"));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			Assert.That(tsb.get_RunText(1), Is.EqualTo("Esto es una prueba!"));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(SpanishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(EnglishFont1Text.Length + 2));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(EnglishFont1Text.Length + 2));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetProperties_TwoRunBldrAlmostReplacesFirstButLeavesJustOneCharacterSamePropertiesAsFirst_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetProperties(0, EnglishFont1Text.Length, new TsTextProps(EnglishWS));
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(tsb.get_RunText(0), Is.EqualTo("This is a test! "));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			Assert.That(tsb.get_RunText(1), Is.EqualTo("¡Esto es una prueba!"));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(SpanishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(EnglishFont1Text.Length + 1));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(EnglishFont1Text.Length + 1));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetProperties_TwoRunBldrReplacesFirstWithoutOverlappingSecondSamePropertiesAsSecond_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetProperties(0, EnglishFont1Text.Length + 1, new TsTextProps(SpanishWS));
			Assert.That(tsb.RunCount, Is.EqualTo(1));
			Assert.That(tsb.get_RunText(0), Is.EqualTo("This is a test! ¡Esto es una prueba!"));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(SpanishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetProperties_TwoRunBldrReplacesFirstAndOverlapsSecondByOneCharacterSamePropertiesAsSecond_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetProperties(0, EnglishFont1Text.Length + 2, new TsTextProps(SpanishWS));
			Assert.That(tsb.RunCount, Is.EqualTo(1));
			Assert.That(tsb.get_RunText(0), Is.EqualTo("This is a test! ¡Esto es una prueba!"));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(SpanishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetProperties_TwoRunBldrAlmostReplacesFirstButLeavesJustOneCharacterSamePropertiesAsSecond_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetProperties(0, EnglishFont1Text.Length, new TsTextProps(SpanishWS));
			Assert.That(tsb.RunCount, Is.EqualTo(3));
			Assert.That(tsb.get_RunText(0), Is.EqualTo("This is a test!"));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(SpanishWS));
			Assert.That(tsb.get_RunText(1), Is.EqualTo(" "));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(EnglishWS));
			Assert.That(GetWS(tsb, 2), Is.EqualTo(SpanishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(EnglishFont1Text.Length));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(EnglishFont1Text.Length));
			Assert.That(ichLim, Is.EqualTo(EnglishFont1Text.Length + 1));
			tsb.GetBoundsOfRun(2, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(EnglishFont1Text.Length + 1));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetProperties_IchMinOutOfRange_Throws()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			Assert.That(() => tsb.SetProperties(-1, 36, new TsTextProps(FrenchWS)), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void SetProperties_IchLimOutOfRange_Throws()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			Assert.That(() => tsb.SetProperties(25, 37, new TsTextProps(FrenchWS)), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void SetProperties_IchMinGreaterThanIchLim_Throws()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			Assert.That(() => tsb.SetProperties(25, 24, new TsTextProps(FrenchWS)), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void SetProperties_TextPropsNull_Throws()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			Assert.That(() => tsb.SetProperties(0, tsb.Length, null), Throws.InstanceOf<ArgumentNullException>());
		}

		[Test]
		public void SetIntPropValues_EmptyBldr_UpdatesProperties()
		{
			TsStrBldr tsb = CreateEmptyBldr();
			Assert.That(GetWS(tsb, 0), Is.EqualTo(-1));
			tsb.SetIntPropValues(0, 0, (int) FwTextPropType.ktptWs, (int) FwTextPropVar.ktpvDefault, EnglishWS);
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
		}

		[Test]
		public void SetIntPropValues_OneRunBldrEmptyRange_DoesNotUpdateProperties()
		{
			TsStrBldr tsb = CreateOneRunBldr();
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			tsb.SetIntPropValues(0, 0, (int) FwTextPropType.ktptWs, (int) FwTextPropVar.ktpvDefault, SpanishWS);
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
		}

		[Test]
		public void SetIntPropValues_OneRunBldrFullRange_UpdatesProperties()
		{
			TsStrBldr tsb = CreateOneRunBldr();
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			tsb.SetIntPropValues(0, tsb.Length, (int) FwTextPropType.ktptWs, (int) FwTextPropVar.ktpvDefault, SpanishWS);
			Assert.That(GetWS(tsb, 0), Is.EqualTo(SpanishWS));
		}

		[Test]
		public void SetIntPropValues_TwoRunBldrRangeOverlapsBoth_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetIntPropValues(6, 27, (int) FwTextPropType.ktptWs, (int) FwTextPropVar.ktpvDefault, FrenchWS);
			Assert.That(tsb.RunCount, Is.EqualTo(3));
			Assert.That(tsb.get_RunText(1), Is.EqualTo("s a test! ¡Esto es un"));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(FrenchWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(6));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(6));
			Assert.That(ichLim, Is.EqualTo(27));
			tsb.GetBoundsOfRun(2, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(27));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetIntPropValues_TwoRunBldrRangeOverlapsBothRemoveProperty_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetIntPropValues(6, 27, (int) FwTextPropType.ktptWs, -1, -1);
			Assert.That(tsb.RunCount, Is.EqualTo(3));
			Assert.That(tsb.get_RunText(1), Is.EqualTo("s a test! ¡Esto es un"));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(-1));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(6));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(6));
			Assert.That(ichLim, Is.EqualTo(27));
			tsb.GetBoundsOfRun(2, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(27));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetIntPropValues_TwoRunBldrRangeOverlapsFirstReplacesSecond_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetIntPropValues(6, tsb.Length, (int) FwTextPropType.ktptWs, (int) FwTextPropVar.ktpvDefault, FrenchWS);
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(tsb.get_RunText(1), Is.EqualTo("s a test! ¡Esto es una prueba!"));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(FrenchWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(6));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(6));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetIntPropValues_TwoRunBldrRangeReplacesFirstOverlapsSecond_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetIntPropValues(0, 27, (int) FwTextPropType.ktptWs, (int) FwTextPropVar.ktpvDefault, FrenchWS);
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(tsb.get_RunText(0), Is.EqualTo("This is a test! ¡Esto es un"));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(FrenchWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(27));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(27));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetIntPropValues_TwoRunBldrRangeOverlapsBothSamePropertiesAsFirst_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetIntPropValues(6, 27, (int) FwTextPropType.ktptWs, (int) FwTextPropVar.ktpvDefault, EnglishWS);
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(tsb.get_RunText(0), Is.EqualTo("This is a test! ¡Esto es un"));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(EnglishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(27));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(27));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetIntPropValues_TwoRunBldrRangeOverlapsBothSamePropertiesAsLast_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.SetIntPropValues(6, 27, (int) FwTextPropType.ktptWs, (int) FwTextPropVar.ktpvDefault, SpanishWS);
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(tsb.get_RunText(1), Is.EqualTo("s a test! ¡Esto es una prueba!"));
			Assert.That(GetWS(tsb, 1), Is.EqualTo(SpanishWS));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(6));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(6));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetIntPropValues_IchMinOutOfRange_Throws()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			Assert.That(() => tsb.SetIntPropValues(-1, 36, (int) FwTextPropType.ktptWs, (int) FwTextPropVar.ktpvDefault, FrenchWS), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void SetIntPropValues_IchLimOutOfRange_Throws()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			Assert.That(() => tsb.SetIntPropValues(25, 37, (int) FwTextPropType.ktptWs, (int) FwTextPropVar.ktpvDefault, FrenchWS), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void SetIntPropValues_IchMinGreaterThanIchLim_Throws()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			Assert.That(() => tsb.SetIntPropValues(25, 24, (int) FwTextPropType.ktptWs, (int) FwTextPropVar.ktpvDefault, FrenchWS), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void SetStrPropValue_EmptyBldr_UpdatesProperties()
		{
			TsStrBldr tsb = CreateEmptyBldr();
			Assert.That(GetFont(tsb, 0), Is.Null);
			tsb.SetStrPropValue(0, 0, (int) FwTextPropType.ktptFontFamily, Font1);
			Assert.That(GetFont(tsb, 0), Is.EqualTo(Font1));
		}

		[Test]
		public void SetStrPropValue_OneRunBldrEmptyRange_DoesNotUpdateProperties()
		{
			TsStrBldr tsb = CreateOneRunBldr();
			Assert.That(GetFont(tsb, 0), Is.EqualTo(Font1));
			tsb.SetStrPropValue(0, 0, (int) FwTextPropType.ktptFontFamily, Font2);
			Assert.That(GetFont(tsb, 0), Is.EqualTo(Font1));
		}

		[Test]
		public void SetStrPropValue_OneRunBldrFullRange_UpdatesProperties()
		{
			TsStrBldr tsb = CreateOneRunBldr();
			Assert.That(GetFont(tsb, 0), Is.EqualTo(Font1));
			tsb.SetStrPropValue(0, tsb.Length, (int) FwTextPropType.ktptFontFamily, Font2);
			Assert.That(GetFont(tsb, 0), Is.EqualTo(Font2));
		}

		[Test]
		public void SetStrPropValue_TwoRunBldrRangeOverlapsBoth_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedFontBldr();
			tsb.SetStrPropValue(6, 27, (int) FwTextPropType.ktptFontFamily, Font3);
			Assert.That(tsb.RunCount, Is.EqualTo(3));
			Assert.That(tsb.get_RunText(1), Is.EqualTo("s a test! How are you"));
			Assert.That(GetFont(tsb, 1), Is.EqualTo(Font3));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(6));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(6));
			Assert.That(ichLim, Is.EqualTo(27));
			tsb.GetBoundsOfRun(2, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(27));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetStrPropValue_TwoRunBldrRangeOverlapsBothRemoveProperty_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedFontBldr();
			tsb.SetStrPropValue(6, 27, (int) FwTextPropType.ktptFontFamily, null);
			Assert.That(tsb.RunCount, Is.EqualTo(3));
			Assert.That(tsb.get_RunText(1), Is.EqualTo("s a test! How are you"));
			Assert.That(GetFont(tsb, 1), Is.Null);
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(6));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(6));
			Assert.That(ichLim, Is.EqualTo(27));
			tsb.GetBoundsOfRun(2, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(27));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetStrPropValue_TwoRunBldrRangeOverlapsFirstReplacesSecond_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedFontBldr();
			tsb.SetStrPropValue(6, tsb.Length, (int) FwTextPropType.ktptFontFamily, Font3);
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(tsb.get_RunText(1), Is.EqualTo("s a test! How are you today?"));
			Assert.That(GetFont(tsb, 1), Is.EqualTo(Font3));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(6));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(6));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetStrPropValue_TwoRunBldrRangeReplacesFirstOverlapsSecond_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedFontBldr();
			tsb.SetStrPropValue(0, 27, (int) FwTextPropType.ktptFontFamily, Font3);
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(tsb.get_RunText(0), Is.EqualTo("This is a test! How are you"));
			Assert.That(GetFont(tsb, 0), Is.EqualTo(Font3));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(27));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(27));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetStrPropValue_TwoRunBldrRangeOverlapsBothSamePropertiesAsFirst_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedFontBldr();
			tsb.SetStrPropValue(6, 27, (int) FwTextPropType.ktptFontFamily, Font1);
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(tsb.get_RunText(0), Is.EqualTo("This is a test! How are you"));
			Assert.That(GetFont(tsb, 0), Is.EqualTo(Font1));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(27));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(27));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetStrPropValue_TwoRunBldrRangeOverlapsBothSamePropertiesAsLast_UpdatesProperties()
		{
			TsStrBldr tsb = CreateMixedFontBldr();
			tsb.SetStrPropValue(6, 27, (int) FwTextPropType.ktptFontFamily, Font2);
			Assert.That(tsb.RunCount, Is.EqualTo(2));
			Assert.That(tsb.get_RunText(1), Is.EqualTo("s a test! How are you today?"));
			Assert.That(GetFont(tsb, 1), Is.EqualTo(Font2));
			int ichMin, ichLim;
			tsb.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(6));
			tsb.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(6));
			Assert.That(ichLim, Is.EqualTo(tsb.Length));
		}

		[Test]
		public void SetStrPropValue_IchMinOutOfRange_Throws()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			Assert.That(() => tsb.SetStrPropValue(-1, 36, (int) FwTextPropType.ktptFontFamily, Font1), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void SetStrPropValue_IchLimOutOfRange_Throws()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			Assert.That(() => tsb.SetStrPropValue(25, 37, (int) FwTextPropType.ktptFontFamily, Font1), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void SetStrPropValue_IchMinGreaterThanIchLim_Throws()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			Assert.That(() => tsb.SetStrPropValue(25, 24, (int) FwTextPropType.ktptFontFamily, Font1), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void Clear_TwoRunBldr_ClearsState()
		{
			TsStrBldr tsb = CreateMixedWSBldr();
			tsb.Clear();
			Assert.That(tsb.Text, Is.Null);
			Assert.That(tsb.RunCount, Is.EqualTo(1));
			Assert.That(GetWS(tsb, 0), Is.EqualTo(-1));
		}

		private static int GetWS(TsStrBldr tsb, int runIndex)
		{
			int var;
			return tsb.get_Properties(runIndex).GetIntPropValues((int) FwTextPropType.ktptWs, out var);
		}

		private static string GetFont(TsStrBldr tsb, int runIndex)
		{
			return tsb.get_Properties(runIndex).GetStrPropValue((int) FwTextPropType.ktptFontFamily);
		}

		private static TsStrBldr CreateEmptyBldr()
		{
			return new TsStrBldr();
		}

		private static TsStrBldr CreateOneRunBldr()
		{
			var intProps = new Dictionary<int, TsIntPropValue>
			{
				{(int) FwTextPropType.ktptWs, new TsIntPropValue((int) FwTextPropVar.ktpvDefault, EnglishWS)}
			};

			var strProps = new Dictionary<int, string>
			{
				{(int) FwTextPropType.ktptFontFamily, Font1}
			};

			return new TsStrBldr(EnglishFont1Text, new TsTextProps(intProps, strProps));
		}

		private static TsStrBldr CreateMixedWSBldr()
		{
			var runs = new List<TsRun>();

			var intProps1 = new Dictionary<int, TsIntPropValue>
			{
				{(int) FwTextPropType.ktptWs, new TsIntPropValue((int) FwTextPropVar.ktpvDefault, EnglishWS)}
			};

			runs.Add(new TsRun(EnglishFont1Text.Length + 1, new TsTextProps(intProps1, null)));

			var intProps2 = new Dictionary<int, TsIntPropValue>
			{
				{(int) FwTextPropType.ktptWs, new TsIntPropValue((int) FwTextPropVar.ktpvDefault, SpanishWS)}
			};

			runs.Add(new TsRun(MixedWSText.Length, new TsTextProps(intProps2, null)));

			return new TsStrBldr(MixedWSText, runs);
		}

		private static TsStrBldr CreateMixedFontBldr()
		{
			var runs = new List<TsRun>();

			var intProps = new Dictionary<int, TsIntPropValue>
			{
				{(int) FwTextPropType.ktptWs, new TsIntPropValue((int) FwTextPropVar.ktpvDefault, EnglishWS)}
			};

			var strProps = new Dictionary<int, string>
			{
				{(int) FwTextPropType.ktptFontFamily, Font1}
			};

			runs.Add(new TsRun(EnglishFont1Text.Length + 1, new TsTextProps(intProps, strProps)));

			strProps = new Dictionary<int, string>
			{
				{(int) FwTextPropType.ktptFontFamily, Font2}
			};

			runs.Add(new TsRun(MixedFontText.Length, new TsTextProps(intProps, strProps)));

			return new TsStrBldr(MixedFontText, runs);
		}
	}
}
