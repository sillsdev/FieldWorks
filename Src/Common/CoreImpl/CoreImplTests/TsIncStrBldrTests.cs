// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	[TestFixture]
	public class TsIncStrBldrTests
	{
		private const string EnglishText = "This is a test!";
		private const int EnglishWS = 1;
		private const int SpanishWS = 2;

		[Test]
		public void Append_EmptyBldrNonEmptyString_AppendsText()
		{
			var tisb = new TsIncStrBldr();
			tisb.Append("text");
			Assert.That(tisb.Text, Is.EqualTo("text"));
			Assert.That(tisb.Runs.Count, Is.EqualTo(1));
		}

		[Test]
		public void Append_EmptyBldrEmptyString_DoesNotAppendText()
		{
			var tisb = new TsIncStrBldr();
			tisb.Append(string.Empty);
			Assert.That(tisb.Text, Is.Null);
			Assert.That(tisb.Runs.Count, Is.EqualTo(0));
		}

		[Test]
		public void Append_EmptyBldrNullString_DoesNotAppendText()
		{
			var tisb = new TsIncStrBldr();
			tisb.Append(null);
			Assert.That(tisb.Text, Is.Null);
			Assert.That(tisb.Runs.Count, Is.EqualTo(0));
		}

		[Test]
		public void Append_EmptyBldrPropSetNonEmptyString_AppendsText()
		{
			var tisb = new TsIncStrBldr();
			tisb.SetIntPropValues((int) FwTextPropType.ktptWs, (int) FwTextPropVar.ktpvDefault, 1);
			tisb.Append("text");
			Assert.That(tisb.Text, Is.EqualTo("text"));
			Assert.That(tisb.Runs.Count, Is.EqualTo(1));
			Assert.That(GetWS(tisb, 0), Is.EqualTo(EnglishWS));
		}

		[Test]
		public void Append_OneRunBldrNonEmptyString_AppendsText()
		{
			TsIncStrBldr tisb = CreateOneRunBldr();
			tisb.Append("text");
			Assert.That(tisb.Text, Is.EqualTo(EnglishText + "text"));
			Assert.That(tisb.Runs.Count, Is.EqualTo(1));
			Assert.That(GetWS(tisb, 0), Is.EqualTo(EnglishWS));
		}

		[Test]
		public void Append_OneRunBldrPropSetNonEmptyString_AppendsText()
		{
			TsIncStrBldr tisb = CreateOneRunBldr();
			tisb.SetIntPropValues((int) FwTextPropType.ktptWs, (int) FwTextPropVar.ktpvDefault, SpanishWS);
			tisb.Append("text");
			Assert.That(tisb.Text, Is.EqualTo(EnglishText + "text"));
			Assert.That(tisb.Runs.Count, Is.EqualTo(2));
			Assert.That(GetWS(tisb, 0), Is.EqualTo(EnglishWS));
			Assert.That(GetWS(tisb, 1), Is.EqualTo(SpanishWS));
			Assert.That(tisb.Runs[0].IchLim, Is.EqualTo(EnglishText.Length));
			Assert.That(tisb.Runs[1].IchLim, Is.EqualTo(tisb.Text.Length));
		}

		[Test]
		public void AppendTsString_EmptyBldrOneRunString_AppendsText()
		{
			var tisb = new TsIncStrBldr();
			tisb.AppendTsString(new TsString("text", EnglishWS));
			Assert.That(tisb.Text, Is.EqualTo("text"));
			Assert.That(tisb.Runs.Count, Is.EqualTo(1));
			Assert.That(GetWS(tisb, 0), Is.EqualTo(EnglishWS));
		}

		[Test]
		public void AppendTsString_EmptyBldrEmptyString_UpdatesProperties()
		{
			var tisb = new TsIncStrBldr();
			tisb.AppendTsString(new TsString(EnglishWS));
			Assert.That(tisb.Text, Is.Null);
			Assert.That(tisb.Runs.Count, Is.EqualTo(0));
			int var;
			Assert.That(tisb.PropsBldr.GetIntPropValues((int) FwTextPropType.ktptWs, out var), Is.EqualTo(EnglishWS));
		}

		[Test]
		public void AppendTsString_OneRunBldrOneRunStringSameProperties_AppendsText()
		{
			TsIncStrBldr tisb = CreateOneRunBldr();
			tisb.AppendTsString(new TsString("text", EnglishWS));
			Assert.That(tisb.Text, Is.EqualTo(EnglishText + "text"));
			Assert.That(tisb.Runs.Count, Is.EqualTo(1));
			Assert.That(GetWS(tisb, 0), Is.EqualTo(EnglishWS));
		}

		[Test]
		public void AppendTsString_OneRunBldrOneRunStringDifferentProperties_AppendsText()
		{
			TsIncStrBldr tisb = CreateOneRunBldr();
			tisb.AppendTsString(new TsString("text", SpanishWS));
			Assert.That(tisb.Text, Is.EqualTo(EnglishText + "text"));
			Assert.That(tisb.Runs.Count, Is.EqualTo(2));
			Assert.That(GetWS(tisb, 0), Is.EqualTo(EnglishWS));
			Assert.That(GetWS(tisb, 1), Is.EqualTo(SpanishWS));
			Assert.That(tisb.Runs[0].IchLim, Is.EqualTo(EnglishText.Length));
			Assert.That(tisb.Runs[1].IchLim, Is.EqualTo(tisb.Text.Length));
			int var;
			Assert.That(tisb.PropsBldr.GetIntPropValues((int) FwTextPropType.ktptWs, out var), Is.EqualTo(SpanishWS));
		}

		[Test]
		public void AppendTsString_NullString_Throws()
		{
			TsIncStrBldr tisb = CreateOneRunBldr();
			Assert.That(() => tisb.AppendTsString(null), Throws.InstanceOf<ArgumentNullException>());
		}

		[Test]
		public void Clear_OneRunBldr_ClearsTextAndRuns()
		{
			TsIncStrBldr tisb = CreateOneRunBldr();
			tisb.Clear();
			Assert.That(tisb.Text, Is.Null);
			Assert.That(tisb.Runs.Count, Is.EqualTo(0));
		}

		[Test]
		public void ClearProps_OneRunBldr_ClearsProperties()
		{
			TsIncStrBldr tisb = CreateOneRunBldr();
			tisb.ClearProps();
			Assert.That(tisb.PropsBldr.IntPropCount, Is.EqualTo(0));
			Assert.That(tisb.PropsBldr.StrPropCount, Is.EqualTo(0));
		}

		private static int GetWS(TsIncStrBldr tisb, int runIndex)
		{
			int var;
			return tisb.Runs[runIndex].TextProps.GetIntPropValues((int) FwTextPropType.ktptWs, out var);
		}

		private static TsIncStrBldr CreateOneRunBldr()
		{
			var intProps = new Dictionary<int, TsIntPropValue>
			{
				{(int) FwTextPropType.ktptWs, new TsIntPropValue((int) FwTextPropVar.ktpvDefault, EnglishWS)}
			};

			return new TsIncStrBldr(EnglishText, new TsTextProps(intProps, null));
		}
	}
}
