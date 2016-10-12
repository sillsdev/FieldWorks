// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
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
		public void SetStrPropValue_NonNullValue_InsertsProperty()
		{
			var tpb = new TsPropsBldr();
			Assert.That(tpb.StrPropCount, Is.EqualTo(0));
			tpb.SetStrPropValue((int) FwTextPropType.ktptFontFamily, "Arial");
			Assert.That(tpb.StrPropCount, Is.EqualTo(1));
			Assert.That(tpb.GetStrPropValue((int) FwTextPropType.ktptFontFamily), Is.EqualTo("Arial"));
		}

		[Test]
		public void SetStrPropValue_NullValue_RemovesProperty()
		{
			var tpb = new TsPropsBldr();
			tpb.SetStrPropValue((int) FwTextPropType.ktptFontFamily, "Arial");
			Assert.That(tpb.StrPropCount, Is.EqualTo(1));
			tpb.SetStrPropValue((int) FwTextPropType.ktptFontFamily, null);
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
