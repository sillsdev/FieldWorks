// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;

namespace SIL.LCModel.Core.Text
{
	[TestFixture]
	public class TsPropsBldrTests
	{
		[Test]
		public void SetIntPropValues_NonNegativeValue_InsertsProperty()
		{
			var tpb = new TsPropsBldr();
			Assert.That(tpb.IntPropCount, Is.EqualTo(0));
			tpb.SetIntPropValues((int) FwTextPropType.ktptWs, (int) FwTextPropVar.ktpvDefault, 1);
			Assert.That(tpb.IntPropCount, Is.EqualTo(1));
			int var;
			Assert.That(tpb.GetIntPropValues((int) FwTextPropType.ktptWs, out var), Is.EqualTo(1));
		}

		[Test]
		public void SetIntPropValues_NegativeValue_RemovesProperty()
		{
			var tpb = new TsPropsBldr();
			tpb.SetIntPropValues((int) FwTextPropType.ktptWs, (int) FwTextPropVar.ktpvDefault, 1);
			Assert.That(tpb.IntPropCount, Is.EqualTo(1));
			tpb.SetIntPropValues((int) FwTextPropType.ktptWs, -1, -1);
			Assert.That(tpb.IntPropCount, Is.EqualTo(0));
		}

		[Test]
		public void SetStrPropValue_NonEmptyValue_InsertsProperty()
		{
			var tpb = new TsPropsBldr();
			Assert.That(tpb.StrPropCount, Is.EqualTo(0));
			tpb.SetStrPropValue((int) FwTextPropType.ktptFontFamily, "Arial");
			Assert.That(tpb.StrPropCount, Is.EqualTo(1));
			Assert.That(tpb.GetStrPropValue((int) FwTextPropType.ktptFontFamily), Is.EqualTo("Arial"));
		}

		[Test]
		public void SetStrPropValue_EmptyValue_InsertsProperty()
		{
			var tpb = new TsPropsBldr();
			Assert.That(tpb.StrPropCount, Is.EqualTo(0));
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, string.Empty);
			Assert.That(tpb.StrPropCount, Is.EqualTo(1));
			Assert.That(tpb.GetStrPropValue((int)FwTextPropType.ktptFontFamily), Is.Null);
		}

		[Test]
		public void SetStrPropValue_NullValue_RemovesProperty()
		{
			var tpb = new TsPropsBldr();
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Arial");
			Assert.That(tpb.StrPropCount, Is.EqualTo(1));
			tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, null);
			Assert.That(tpb.StrPropCount, Is.EqualTo(0));
		}

		[Test]
		public void SetStrPropValueRgch_NonEmptyValue_InsertsProperty()
		{
			var tpb = new TsPropsBldr();
			Assert.That(tpb.StrPropCount, Is.EqualTo(0));
			Guid guid = Guid.NewGuid();
			byte[] bytes = TsStringUtils.GetObjData(guid, FwObjDataTypes.kodtNameGuidHot);
			tpb.SetStrPropValueRgch((int) FwTextPropType.ktptObjData, bytes, bytes.Length);
			Assert.That(tpb.StrPropCount, Is.EqualTo(1));
			string str = tpb.GetStrPropValue((int) FwTextPropType.ktptObjData);
			Assert.That((FwObjDataTypes) str[0], Is.EqualTo(FwObjDataTypes.kodtNameGuidHot));
			Assert.That(MiscUtils.GetGuidFromObjData(str.Substring(1)), Is.EqualTo(guid));
		}

		[Test]
		public void SetStrPropValueRgch_EmptyValue_InsertsProperty()
		{
			var tpb = new TsPropsBldr();
			Assert.That(tpb.StrPropCount, Is.EqualTo(0));
			tpb.SetStrPropValueRgch((int) FwTextPropType.ktptObjData, new byte[0], 0);
			Assert.That(tpb.StrPropCount, Is.EqualTo(1));
			Assert.That(tpb.GetStrPropValue((int)FwTextPropType.ktptObjData), Is.Null);
		}

		[Test]
		public void SetStrPropValueRgch_NullValue_RemovesProperty()
		{
			var tpb = new TsPropsBldr();
			Guid guid = Guid.NewGuid();
			byte[] bytes = TsStringUtils.GetObjData(guid, FwObjDataTypes.kodtNameGuidHot);
			tpb.SetStrPropValueRgch((int)FwTextPropType.ktptObjData, bytes, bytes.Length);
			Assert.That(tpb.StrPropCount, Is.EqualTo(1));
			tpb.SetStrPropValueRgch((int)FwTextPropType.ktptObjData, null, 0);
			Assert.That(tpb.StrPropCount, Is.EqualTo(0));
		}

		[Test]
		public void Clear_NonEmptyBldr_ClearsState()
		{
			var tpb = new TsPropsBldr();
			tpb.SetIntPropValues((int) FwTextPropType.ktptWs, (int) FwTextPropVar.ktpvDefault, 1);
			tpb.SetStrPropValue((int) FwTextPropType.ktptFontFamily, "Arial");
			Assert.That(tpb.IntPropCount, Is.EqualTo(1));
			Assert.That(tpb.StrPropCount, Is.EqualTo(1));
			tpb.Clear();
			Assert.That(tpb.IntPropCount, Is.EqualTo(0));
			Assert.That(tpb.StrPropCount, Is.EqualTo(0));
		}
	}
}
