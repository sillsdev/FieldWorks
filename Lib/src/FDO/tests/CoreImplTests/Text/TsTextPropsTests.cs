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
	public class TsTextPropsTests
	{
		[Test]
		public void IntPropCount_Empty_ReturnsZero()
		{
			TsTextProps tps = TsTextProps.EmptyProps;
			Assert.That(tps.IntPropCount, Is.EqualTo(0));
		}

		[Test]
		public void IntPropCount_NonEmpty_ReturnsCorrectCount()
		{
			TsTextProps tps = CreateNonEmptyTextProps();
			Assert.That(tps.IntPropCount, Is.EqualTo(3));
		}

		[Test]
		public void GetIntProp_ValidIndex_ReturnsCorrectValue()
		{
			TsTextProps tps = CreateNonEmptyTextProps();
			int tpt, var;
			int value = tps.GetIntProp(0, out tpt, out var);
			Assert.That(tpt, Is.EqualTo((int) FwTextPropType.ktptWs));
			Assert.That(var, Is.EqualTo((int) FwTextPropVar.ktpvDefault));
			Assert.That(value, Is.EqualTo(1));

			value = tps.GetIntProp(2, out tpt, out var);
			Assert.That(tpt, Is.EqualTo((int) FwTextPropType.ktptBackColor));
			Assert.That(var, Is.EqualTo((int) FwTextPropVar.ktpvEnum));
			Assert.That(value, Is.EqualTo((int) FwTextColor.kclrYellow));
		}

		[Test]
		public void GetIntProp_IndexOutOfRange_Throws()
		{
			TsTextProps tps = TsTextProps.EmptyProps;
			int tpt, var;
			Assert.That(() => tps.GetIntProp(0, out tpt, out var), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void GetIntPropValues_Exists_ReturnsCorrectValue()
		{
			TsTextProps tps = CreateNonEmptyTextProps();
			int var;
			int value = tps.GetIntPropValues((int) FwTextPropType.ktptWs, out var);
			Assert.That(var, Is.EqualTo((int) FwTextPropVar.ktpvDefault));
			Assert.That(value, Is.EqualTo(1));
		}

		[Test]
		public void GetIntPropValues_DoesNotExist_ReturnsNegativeOne()
		{
			TsTextProps tps = CreateNonEmptyTextProps();
			int var;
			int value = tps.GetIntPropValues((int) FwTextPropType.ktptFontSize, out var);
			Assert.That(var, Is.EqualTo(-1));
			Assert.That(value, Is.EqualTo(-1));
		}

		[Test]
		public void StrPropCount_Empty_ReturnsZero()
		{
			TsTextProps tps = TsTextProps.EmptyProps;
			Assert.That(tps.StrPropCount, Is.EqualTo(0));
		}

		[Test]
		public void StrPropCount_NonEmpty_ReturnsCorrectCount()
		{
			TsTextProps tps = CreateNonEmptyTextProps();
			Assert.That(tps.StrPropCount, Is.EqualTo(2));
		}

		[Test]
		public void GetStrProp_ValidIndex_ReturnsCorrectValue()
		{
			TsTextProps tps = CreateNonEmptyTextProps();
			int tpt;
			string value = tps.GetStrProp(0, out tpt);
			Assert.That(tpt, Is.EqualTo((int) FwTextPropType.ktptFontFamily));
			Assert.That(value, Is.EqualTo("Arial"));

			value = tps.GetStrProp(1, out tpt);
			Assert.That(tpt, Is.EqualTo((int) FwTextPropType.ktptFieldName));
			Assert.That(value, Is.EqualTo("Field"));
		}

		[Test]
		public void GetStrProp_IndexOutOfRange_Throws()
		{
			TsTextProps tps = TsTextProps.EmptyProps;
			int tpt, var;
			Assert.That(() => tps.GetStrProp(0, out tpt), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void GetStrPropValue_Exists_ReturnsCorrectValue()
		{
			TsTextProps tps = CreateNonEmptyTextProps();
			string value = tps.GetStrPropValue((int) FwTextPropType.ktptFontFamily);
			Assert.That(value, Is.EqualTo("Arial"));
		}

		[Test]
		public void GetStrPropValue_DoesNotExist_ReturnsNull()
		{
			TsTextProps tps = CreateNonEmptyTextProps();
			string value = tps.GetStrPropValue((int) FwTextPropType.ktptParaStyle);
			Assert.That(value, Is.Null);
		}

		[Test]
		public void GetBldr_Empty_ReturnsBldrWithCorrectData()
		{
			TsTextProps tps = TsTextProps.EmptyProps;
			Assert.That(tps.GetBldr().GetTextProps(), Is.EqualTo(tps));
		}

		[Test]
		public void GetBldr_NonEmpty_ReturnsBldrWithCorrectData()
		{
			TsTextProps tps = CreateNonEmptyTextProps();
			Assert.That(tps.GetBldr().GetTextProps(), Is.EqualTo(tps));
		}

		[Test]
		public void Equals_SameEmpty_ReturnsTrue()
		{
			TsTextProps tps1 = TsTextProps.EmptyProps;
			TsTextProps tps2 = TsTextProps.EmptyProps;
			Assert.That(tps1.Equals(tps2), Is.True);
		}

		[Test]
		public void Equals_SameNonEmpty_ReturnsTrue()
		{
			TsTextProps tps1 = CreateNonEmptyTextProps();
			TsTextProps tps2 = CreateNonEmptyTextProps();
			Assert.That(tps1.Equals(tps2), Is.True);
		}

		[Test]
		public void Equals_NotSame_ReturnsFalse()
		{
			TsTextProps tps1 = TsTextProps.EmptyProps;
			TsTextProps tps2 = CreateNonEmptyTextProps();
			Assert.That(tps1.Equals(tps2), Is.False);
		}

		private static TsTextProps CreateNonEmptyTextProps()
		{
			var intProps = new Dictionary<int, TsIntPropValue>
			{
				{(int) FwTextPropType.ktptWs, new TsIntPropValue((int) FwTextPropVar.ktpvDefault, 1)},
				{(int) FwTextPropType.ktptBold, new TsIntPropValue((int) FwTextPropVar.ktpvEnum, (int) FwTextToggleVal.kttvForceOn)},
				{(int) FwTextPropType.ktptBackColor, new TsIntPropValue((int) FwTextPropVar.ktpvEnum, (int) FwTextColor.kclrYellow)}
			};

			var strProps = new Dictionary<int, string>
			{
				{(int) FwTextPropType.ktptFontFamily, "Arial"},
				{(int) FwTextPropType.ktptFieldName, "Field"}
			};

			return new TsTextProps(intProps, strProps);
		}
	}
}
