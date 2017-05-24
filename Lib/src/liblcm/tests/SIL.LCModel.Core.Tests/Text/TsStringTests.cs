// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;

namespace SIL.LCModel.Core.Text
{
	[TestFixture]
	public class TsStringTests
	{
		private const string EnglishText = "This is a test!";
		private const string SpanishText = "¡Esto es una prueba!";
		// the space goes in the English run
		private const string CombinedText = EnglishText + " " + SpanishText;
		private const int EnglishWS = 1;
		private const int SpanishWS = 2;

		[Test]
		public void Text_Empty_ReturnsNull()
		{
			TsString tss = CreateEmptyString();
			Assert.That(tss.Text, Is.Null);
		}

		[Test]
		public void Text_OneRun_ReturnsRunText()
		{
			TsString tss = CreateOneRunString();
			Assert.That(tss.Text, Is.EqualTo(EnglishText));
		}

		[Test]
		public void Text_TwoRuns_ReturnsRunText()
		{
			TsString tss = CreateTwoRunString();
			Assert.That(tss.Text, Is.EqualTo(CombinedText));
		}

		[Test]
		public void Length_Empty_ReturnsZero()
		{
			TsString tss = CreateEmptyString();
			Assert.That(tss.Length, Is.EqualTo(0));
		}

		[Test]
		public void Length_OneRun_ReturnsCorrectLength()
		{
			TsString tss = CreateOneRunString();
			Assert.That(tss.Length, Is.EqualTo(EnglishText.Length));
		}

		[Test]
		public void Length_TwoRuns_ReturnsCorrectLength()
		{
			TsString tss = CreateTwoRunString();
			Assert.That(tss.Length, Is.EqualTo(CombinedText.Length));
		}

		[Test]
		public void RunCount_Empty_ReturnsOne()
		{
			TsString tss = CreateEmptyString();
			Assert.That(tss.RunCount, Is.EqualTo(1));
		}

		[Test]
		public void RunCount_OneRun_ReturnsOne()
		{
			TsString tss = CreateOneRunString();
			Assert.That(tss.RunCount, Is.EqualTo(1));
		}

		[Test]
		public void RunCount_TwoRuns_ReturnsTwo()
		{
			TsString tss = CreateTwoRunString();
			Assert.That(tss.RunCount, Is.EqualTo(2));
		}

		[Test]
		public void GetChars_Empty_ReturnsNull()
		{
			TsString tss = CreateEmptyString();
			Assert.That(tss.GetChars(0, 0), Is.Null);
		}

		[Test]
		public void GetChars_OneRun_ReturnsCorrectString()
		{
			TsString tss = CreateOneRunString();
			Assert.That(tss.GetChars(1, 7), Is.EqualTo("his is"));
		}

		[Test]
		public void GetChars_TwoRuns_ReturnsCorrectString()
		{
			TsString tss = CreateTwoRunString();
			Assert.That(tss.GetChars(25, 36), Is.EqualTo("una prueba!"));
		}

		[Test]
		public void GetChars_IchMinOutOfRange_Throws()
		{
			TsString tss = CreateTwoRunString();
			Assert.That(() => tss.GetChars(-1, 36), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void GetChars_IchLimOutOfRange_Throws()
		{
			TsString tss = CreateTwoRunString();
			Assert.That(() => tss.GetChars(25, 37), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void GetChars_IchMinGreaterThanIchLim_Throws()
		{
			TsString tss = CreateTwoRunString();
			Assert.That(() => tss.GetChars(25, 24), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void FetchChars_Empty_ReturnsEmptyString()
		{
			TsString tss = CreateEmptyString();
			using (ArrayPtr rgch = MarshalEx.StringToNative(0, true))
			{
				tss.FetchChars(0, 0, rgch);
				string str = MarshalEx.NativeToString(rgch, 0, true);
				Assert.That(str, Is.EqualTo(string.Empty));
			}
		}

		[Test]
		public void FetchChars_OneRun_ReturnsCorrectString()
		{
			TsString tss = CreateOneRunString();
			using (ArrayPtr rgch = MarshalEx.StringToNative(6, true))
			{
				tss.FetchChars(1, 7, rgch);
				string str = MarshalEx.NativeToString(rgch, 6, true);
				Assert.That(str, Is.EqualTo("his is"));
			}
		}

		[Test]
		public void FetchChars_TwoRuns_ReturnsCorrectString()
		{
			TsString tss = CreateTwoRunString();
			using (ArrayPtr rgch = MarshalEx.StringToNative(11, true))
			{
				tss.FetchChars(25, 36, rgch);
				string str = MarshalEx.NativeToString(rgch, 11, true);
				Assert.That(str, Is.EqualTo("una prueba!"));
			}
		}

		[Test]
		public void FetchChars_IchMinOutOfRange_Throws()
		{
			TsString tss = CreateTwoRunString();
			using (ArrayPtr rgch = MarshalEx.StringToNative(10, true))
				Assert.That(() => tss.FetchChars(-1, 36, rgch), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void FetchChars_IchLimOutOfRange_Throws()
		{
			TsString tss = CreateTwoRunString();
			using (ArrayPtr rgch = MarshalEx.StringToNative(10, true))
				Assert.That(() => tss.FetchChars(25, 37, rgch), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void FetchChars_IchMinGreaterThanIchLim_Throws()
		{
			TsString tss = CreateTwoRunString();
			using (ArrayPtr rgch = MarshalEx.StringToNative(10, true))
				Assert.That(() => tss.FetchChars(25, 24, rgch), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void FetchChars_ArrayIsNull_Throws()
		{
			TsString tss = CreateTwoRunString();
			Assert.That(() => tss.FetchChars(0, 5, ArrayPtr.Null), Throws.InstanceOf<ArgumentNullException>());
		}

		[Test]
		public void LockText_Empty_ReturnsEmptyString()
		{
			TsString tss = CreateEmptyString();
			string text;
			int len;
			tss.LockText(out text, out len);
			Assert.That(text, Is.EqualTo(string.Empty));
			Assert.That(len, Is.EqualTo(0));
			tss.UnlockText(text);
		}

		[Test]
		public void LockText_OneRun_ReturnsCorrectString()
		{
			TsString tss = CreateOneRunString();
			string text;
			int len;
			tss.LockText(out text, out len);
			Assert.That(text, Is.EqualTo(EnglishText));
			Assert.That(len, Is.EqualTo(EnglishText.Length));
			tss.UnlockText(text);
		}

		[Test]
		public void LockText_TwoRuns_ReturnsCorrectString()
		{
			TsString tss = CreateTwoRunString();
			string text;
			int len;
			tss.LockText(out text, out len);
			Assert.That(text, Is.EqualTo(CombinedText));
			Assert.That(len, Is.EqualTo(CombinedText.Length));
			tss.UnlockText(text);
		}

		[Test]
		public void get_RunAt_Empty_ReturnsZero()
		{
			TsString tss = CreateEmptyString();
			Assert.That(tss.get_RunAt(0), Is.EqualTo(0));
		}

		[Test]
		public void get_RunAt_OneRun_ReturnsZero()
		{
			TsString tss = CreateOneRunString();
			Assert.That(tss.get_RunAt(3), Is.EqualTo(0));
		}

		[Test]
		public void get_RunAt_TwoRunsFirstRun_ReturnsZero()
		{
			TsString tss = CreateTwoRunString();
			Assert.That(tss.get_RunAt(15), Is.EqualTo(0));
		}

		[Test]
		public void get_RunAt_TwoRunsSecondRun_ReturnsOne()
		{
			TsString tss = CreateTwoRunString();
			Assert.That(tss.get_RunAt(16), Is.EqualTo(1));
		}

		[Test]
		public void get_RunAt_IchOutOfRange_Throws()
		{
			TsString tss = CreateEmptyString();
			Assert.That(() => tss.get_RunAt(1), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void GetBoundsOfRun_Empty_ReturnsCorrectBounds()
		{
			TsString tss = CreateEmptyString();
			int ichMin, ichLim;
			tss.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(0));
		}

		[Test]
		public void GetBoundsOfRun_OneRun_ReturnsCorrectBounds()
		{
			TsString tss = CreateOneRunString();
			int ichMin, ichLim;
			tss.GetBoundsOfRun(0, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(0));
			Assert.That(ichLim, Is.EqualTo(15));
		}

		[Test]
		public void GetBoundsOfRun_TwoRuns_ReturnsCorrectBounds()
		{
			TsString tss = CreateTwoRunString();
			int ichMin, ichLim;
			tss.GetBoundsOfRun(1, out ichMin, out ichLim);
			Assert.That(ichMin, Is.EqualTo(16));
			Assert.That(ichLim, Is.EqualTo(36));
		}

		[Test]
		public void GetBoundsOfRun_RunIndexOutOfRange_Throws()
		{
			TsString tss = CreateOneRunString();
			int ichMin, ichLim;
			Assert.That(() => tss.GetBoundsOfRun(1, out ichMin, out ichLim), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void FetchRunInfo_Empty_ReturnsCorrectRunInfo()
		{
			TsString tss = CreateEmptyString();
			TsRunInfo tri;
			tss.FetchRunInfo(0, out tri);
			Assert.That(tri, Is.EqualTo(new TsRunInfo {ichMin = 0, ichLim = 0, irun = 0}));
		}

		[Test]
		public void FetchRunInfo_OneRun_ReturnsCorrectRunInfo()
		{
			TsString tss = CreateOneRunString();
			TsRunInfo tri;
			tss.FetchRunInfo(0, out tri);
			Assert.That(tri, Is.EqualTo(new TsRunInfo {ichMin = 0, ichLim = 15, irun = 0}));
		}

		[Test]
		public void FetchRunInfo_TwoRuns_ReturnsCorrectRunInfo()
		{
			TsString tss = CreateTwoRunString();
			TsRunInfo tri;
			tss.FetchRunInfo(1, out tri);
			Assert.That(tri, Is.EqualTo(new TsRunInfo {ichMin = 16, ichLim = 36, irun = 1}));
		}

		[Test]
		public void FetchRunInfo_RunIndexOutOfRange_Throws()
		{
			TsString tss = CreateTwoRunString();
			TsRunInfo tri;
			Assert.That(() => tss.FetchRunInfo(2, out tri), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void FetchRunInfoAt_Empty_ReturnsCorrectRunInfo()
		{
			TsString tss = CreateEmptyString();
			TsRunInfo tri;
			tss.FetchRunInfoAt(0, out tri);
			Assert.That(tri, Is.EqualTo(new TsRunInfo {ichMin = 0, ichLim = 0, irun = 0}));
		}

		[Test]
		public void FetchRunInfoAt_OneRun_ReturnsCorrectRunInfo()
		{
			TsString tss = CreateOneRunString();
			TsRunInfo tri;
			tss.FetchRunInfoAt(3, out tri);
			Assert.That(tri, Is.EqualTo(new TsRunInfo {ichMin = 0, ichLim = 15, irun = 0}));
		}

		[Test]
		public void FetchRunInfoAt_TwoRuns_ReturnsCorrectRunInfo()
		{
			TsString tss = CreateTwoRunString();
			TsRunInfo tri;
			tss.FetchRunInfoAt(18, out tri);
			Assert.That(tri, Is.EqualTo(new TsRunInfo {ichMin = 16, ichLim = 36, irun = 1}));
		}

		[Test]
		public void FetchRunInfoAt_IchOutOfRange_Throws()
		{
			TsString tss = CreateTwoRunString();
			TsRunInfo tri;
			Assert.That(() => tss.FetchRunInfoAt(37, out tri), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void get_Properties_Empty_ReturnsCorrectProperties()
		{
			TsString tss = CreateEmptyString();
			ITsTextProps tps = tss.get_Properties(0);
			int var;
			Assert.That(tps.GetIntPropValues((int) FwTextPropType.ktptWs, out var), Is.EqualTo(EnglishWS));
		}

		[Test]
		public void get_Properties_OneRun_ReturnsCorrectProperties()
		{
			TsString tss = CreateOneRunString();
			ITsTextProps tps = tss.get_Properties(0);
			int var;
			Assert.That(tps.GetIntPropValues((int) FwTextPropType.ktptWs, out var), Is.EqualTo(EnglishWS));
		}

		[Test]
		public void get_Properties_TwoRuns_ReturnsCorrectProperties()
		{
			TsString tss = CreateTwoRunString();
			ITsTextProps tps = tss.get_Properties(1);
			int var;
			Assert.That(tps.GetIntPropValues((int) FwTextPropType.ktptWs, out var), Is.EqualTo(SpanishWS));
		}

		[Test]
		public void get_Properties_RunIndexOutOfRange_Throws()
		{
			TsString tss = CreateTwoRunString();
			Assert.That(() => tss.get_Properties(2), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void get_PropertiesAt_Empty_ReturnsCorrectProperties()
		{
			TsString tss = CreateEmptyString();
			ITsTextProps tps = tss.get_PropertiesAt(0);
			int var;
			Assert.That(tps.GetIntPropValues((int) FwTextPropType.ktptWs, out var), Is.EqualTo(EnglishWS));
		}

		[Test]
		public void get_PropertiesAt_OneRun_ReturnsCorrectProperties()
		{
			TsString tss = CreateOneRunString();
			ITsTextProps tps = tss.get_PropertiesAt(15);
			int var;
			Assert.That(tps.GetIntPropValues((int) FwTextPropType.ktptWs, out var), Is.EqualTo(EnglishWS));
		}

		[Test]
		public void get_PropertiesAt_TwoRuns_ReturnsCorrectProperties()
		{
			TsString tss = CreateTwoRunString();
			ITsTextProps tps = tss.get_PropertiesAt(20);
			int var;
			Assert.That(tps.GetIntPropValues((int) FwTextPropType.ktptWs, out var), Is.EqualTo(SpanishWS));
		}

		[Test]
		public void get_PropertiesAt_IchOutOfRange_Throws()
		{
			TsString tss = CreateTwoRunString();
			Assert.That(() => tss.get_Properties(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void get_RunText_Empty_ReturnsNull()
		{
			TsString tss = CreateEmptyString();
			Assert.That(tss.get_RunText(0), Is.Null);
		}

		[Test]
		public void get_RunText_OneRun_ReturnsCorrectString()
		{
			TsString tss = CreateOneRunString();
			Assert.That(tss.get_RunText(0), Is.EqualTo(EnglishText));
		}

		[Test]
		public void get_RunText_TwoRuns_ReturnsCorrectString()
		{
			TsString tss = CreateTwoRunString();
			Assert.That(tss.get_RunText(1), Is.EqualTo(SpanishText));
		}

		[Test]
		public void get_RunText_RunIndexOutOfRange_Throws()
		{
			TsString tss = CreateTwoRunString();
			Assert.That(() => tss.get_RunText(2), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void Equals_EmptySame_ReturnsTrue()
		{
			TsString tss1 = CreateEmptyString();
			TsString tss2 = CreateEmptyString();
			Assert.That(tss1.Equals(tss2), Is.True);
		}

		[Test]
		public void Equals_TwoRunsSame_ReturnsTrue()
		{
			TsString tss1 = CreateTwoRunString();
			TsString tss2 = CreateTwoRunString();
			Assert.That(tss1.Equals(tss2), Is.True);
		}

		[Test]
		public void Equals_NotSame_ReturnsFalse()
		{
			TsString tss1 = CreateTwoRunString();
			TsString tss2 = CreateOneRunString();
			Assert.That(tss1.Equals(tss2), Is.False);
		}

		[Test]
		public void GetBldr_Empty_ReturnsBldrWithCorrectData()
		{
			TsString tss = CreateEmptyString();
			Assert.That(tss.GetBldr().GetString(), Is.EqualTo(tss));
		}

		[Test]
		public void GetBldr_OneRun_ReturnsBldrWithCorrectData()
		{
			TsString tss = CreateOneRunString();
			Assert.That(tss.GetBldr().GetString(), Is.EqualTo(tss));
		}

		[Test]
		public void GetBldr_TwoRuns_ReturnsBldrWithCorrectData()
		{
			TsString tss = CreateTwoRunString();
			Assert.That(tss.GetBldr().GetString(), Is.EqualTo(tss));
		}

		[Test]
		public void GetIncBldr_Empty_ReturnsIncBldrWithCorrectData()
		{
			TsString tss = CreateEmptyString();
			Assert.That(tss.GetIncBldr().GetString(), Is.EqualTo(tss));
		}

		[Test]
		public void GetIncBldr_OneRun_ReturnsIncBldrWithCorrectData()
		{
			TsString tss = CreateOneRunString();
			Assert.That(tss.GetIncBldr().GetString(), Is.EqualTo(tss));
		}

		[Test]
		public void GetIncBldr_TwoRuns_ReturnsIncBldrWithCorrectData()
		{
			TsString tss = CreateTwoRunString();
			Assert.That(tss.GetIncBldr().GetString(), Is.EqualTo(tss));
		}

		[Test]
		public void GetSubstring_WholeString_ReturnsSameString()
		{
			TsString tss = CreateOneRunString();
			Assert.That(tss.GetSubstring(0, tss.Length), Is.EqualTo(tss));
		}

		[Test]
		public void GetSubstring_WholeRun_ReturnsCorrectString()
		{
			TsString tss = CreateTwoRunString();

			ITsString substring = tss.GetSubstring(tss.get_MinOfRun(1), tss.get_LimOfRun(1));
			Assert.That(substring.Text, Is.EqualTo(SpanishText));
			Assert.That(substring.RunCount, Is.EqualTo(1));
			Assert.That(substring.get_LimOfRun(0), Is.EqualTo(substring.Length));
			Assert.That(substring.get_WritingSystem(0), Is.EqualTo(SpanishWS));
		}

		[Test]
		public void GetSubstring_PartialRun_ReturnsCorrectString()
		{
			TsString tss = CreateTwoRunString();

			ITsString substring = tss.GetSubstring(tss.get_MinOfRun(1) + 1, tss.get_LimOfRun(1) - 1);
			Assert.That(substring.Text, Is.EqualTo("Esto es una prueba"));
			Assert.That(substring.RunCount, Is.EqualTo(1));
			Assert.That(substring.get_LimOfRun(0), Is.EqualTo(substring.Length));
			Assert.That(substring.get_WritingSystem(0), Is.EqualTo(SpanishWS));
		}

		[Test]
		public void GetSubstring_PartOfTwoRuns_ReturnsCorrectString()
		{
			TsString tss = CreateTwoRunString();

			ITsString substring = tss.GetSubstring(7, 28);
			Assert.That(substring.Text, Is.EqualTo(" a test! ¡Esto es una"));
			Assert.That(substring.RunCount, Is.EqualTo(2));
			Assert.That(substring.get_WritingSystem(0), Is.EqualTo(EnglishWS));
			Assert.That(substring.get_LimOfRun(0), Is.EqualTo(9));
			Assert.That(substring.get_WritingSystem(1), Is.EqualTo(SpanishWS));
		}

		[Test]
		public void GetSubstring_IchMinOutOfRange_Throws()
		{
			TsString tss = CreateTwoRunString();
			Assert.That(() => tss.GetSubstring(-1, 36), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void GetSubstring_IchLimOutOfRange_Throws()
		{
			TsString tss = CreateTwoRunString();
			Assert.That(() => tss.GetSubstring(25, 37), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void GetSubstring_IchMinGreaterThanIchLim_Throws()
		{
			TsString tss = CreateTwoRunString();
			Assert.That(() => tss.GetSubstring(25, 24), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		private static TsString CreateEmptyString()
		{
			return new TsString(1);
		}

		private static TsString CreateOneRunString()
		{
			var intProps = new Dictionary<int, TsIntPropValue>
			{
				{(int) FwTextPropType.ktptWs, new TsIntPropValue((int) FwTextPropVar.ktpvDefault, EnglishWS)},
				{(int) FwTextPropType.ktptBold, new TsIntPropValue((int) FwTextPropVar.ktpvEnum, (int) FwTextToggleVal.kttvForceOn)}
			};
			return new TsString(EnglishText, new TsTextProps(intProps, null));
		}

		private static TsString CreateTwoRunString()
		{
			var runs = new List<TsRun>();

			var intProps1 = new Dictionary<int, TsIntPropValue>
			{
				{(int) FwTextPropType.ktptWs, new TsIntPropValue((int) FwTextPropVar.ktpvDefault, EnglishWS)},
				{(int) FwTextPropType.ktptBold, new TsIntPropValue((int) FwTextPropVar.ktpvEnum, (int) FwTextToggleVal.kttvForceOn)}
			};

			runs.Add(new TsRun(EnglishText.Length + 1, new TsTextProps(intProps1, null)));

			var intProps2 = new Dictionary<int, TsIntPropValue>
			{
				{(int) FwTextPropType.ktptWs, new TsIntPropValue((int) FwTextPropVar.ktpvDefault, SpanishWS)},
				{(int) FwTextPropType.ktptUnderline, new TsIntPropValue((int) FwTextPropVar.ktpvEnum, (int) FwTextToggleVal.kttvForceOn)}
			};

			runs.Add(new TsRun(CombinedText.Length, new TsTextProps(intProps2, null)));

			return new TsString(CombinedText, runs);
		}

	}
}
