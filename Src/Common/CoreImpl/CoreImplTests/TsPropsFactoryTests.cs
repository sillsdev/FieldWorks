// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	[TestFixture]
	public class TsPropsFactoryTests
	{
		[Test]
		public void MakeProps_NonNullStyle_CreatesTextProps()
		{
			var tpf = new TsPropsFactory();
			ITsTextProps tps = tpf.MakeProps("Style", 2, 1);
			Assert.That(tps.IntPropCount, Is.EqualTo(1));
			int var;
			Assert.That(tps.GetIntPropValues((int) FwTextPropType.ktptWs, out var), Is.EqualTo(2));
			Assert.That(var, Is.EqualTo(1));
			Assert.That(tps.StrPropCount, Is.EqualTo(1));
			Assert.That(tps.GetStrPropValue((int) FwTextPropType.ktptNamedStyle), Is.EqualTo("Style"));
		}

		[Test]
		public void MakeProps_NullStyle_CreatesTextPropsWithoutStyle()
		{
			var tpf = new TsPropsFactory();
			ITsTextProps tps = tpf.MakeProps(null, 2, 1);
			Assert.That(tps.IntPropCount, Is.EqualTo(1));
			int var;
			Assert.That(tps.GetIntPropValues((int) FwTextPropType.ktptWs, out var), Is.EqualTo(2));
			Assert.That(var, Is.EqualTo(1));
			Assert.That(tps.StrPropCount, Is.EqualTo(0));
		}

		[Test]
		public void MakeProps_InvalidWS_Throws()
		{
			var tpf = new TsPropsFactory();
			Assert.That(() => tpf.MakeProps("Style", -1, -1), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}
	}
}
