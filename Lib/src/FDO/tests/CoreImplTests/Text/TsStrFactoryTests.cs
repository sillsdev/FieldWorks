// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwKernelInterfaces;

namespace SIL.CoreImpl.Text
{
	[TestFixture]
	public class TsStrFactoryTests
	{
		[Test]
		public void MakeString_NonEmptyTextValidWS_ReturnsCorrectString()
		{
			var tsf = new TsStrFactory();
			ITsString tss = tsf.MakeString("text", 1);
			Assert.That(tss.Text, Is.EqualTo("text"));
			Assert.That(tss.get_WritingSystem(0), Is.EqualTo(1));
		}

		[Test]
		public void MakeString_EmptyTextValidWS_ReturnsCorrectString()
		{
			var tsf = new TsStrFactory();
			ITsString tss = tsf.MakeString(string.Empty, 1);
			Assert.That(tss.Text, Is.Null);
			Assert.That(tss.get_WritingSystem(0), Is.EqualTo(1));
		}

		[Test]
		public void MakeString_ZeroWS_Throws()
		{
			var tsf = new TsStrFactory();
			Assert.That(() => tsf.MakeString(string.Empty, 0), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void MakeStringRgch_NonEmptyTextWholeLengthValidWS_ReturnsCorrectString()
		{
			var tsf = new TsStrFactory();
			ITsString tss = tsf.MakeStringRgch("text", 4, 1);
			Assert.That(tss.Text, Is.EqualTo("text"));
			Assert.That(tss.get_WritingSystem(0), Is.EqualTo(1));
		}

		[Test]
		public void MakeStringRgch_NonEmptyTextPartialLengthValidWS_ReturnsCorrectString()
		{
			var tsf = new TsStrFactory();
			ITsString tss = tsf.MakeStringRgch("text", 2, 1);
			Assert.That(tss.Text, Is.EqualTo("te"));
			Assert.That(tss.get_WritingSystem(0), Is.EqualTo(1));
		}

		[Test]
		public void MakeStringRgch_EmptyTextValidWS_ReturnsCorrectString()
		{
			var tsf = new TsStrFactory();
			ITsString tss = tsf.MakeStringRgch(string.Empty, 0, 1);
			Assert.That(tss.Text, Is.Null);
			Assert.That(tss.get_WritingSystem(0), Is.EqualTo(1));
		}

		[Test]
		public void MakeStringRgch_ZeroWS_Throws()
		{
			var tsf = new TsStrFactory();
			Assert.That(() => tsf.MakeStringRgch(string.Empty, 0, -1), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void MakeStringRgch_InvalidLength_Throws()
		{
			var tsf = new TsStrFactory();
			Assert.That(() => tsf.MakeStringRgch(string.Empty, 1, 1), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void MakeStringWithPropsRgch_NonEmptyTextWholeLengthValidProps_ReturnsCorrectString()
		{
			var tsf = new TsStrFactory();
			ITsString tss = tsf.MakeStringWithPropsRgch("text", 4, new TsTextProps(1));
			Assert.That(tss.Text, Is.EqualTo("text"));
			Assert.That(tss.get_WritingSystem(0), Is.EqualTo(1));
		}

		[Test]
		public void MakeStringWithPropsRgch_NonEmptyTextPartialLengthValidProps_ReturnsCorrectString()
		{
			var tsf = new TsStrFactory();
			ITsString tss = tsf.MakeStringWithPropsRgch("text", 2, new TsTextProps(1));
			Assert.That(tss.Text, Is.EqualTo("te"));
			Assert.That(tss.get_WritingSystem(0), Is.EqualTo(1));
		}

		[Test]
		public void MakeStringWithPropsRgch_EmptyTextValidProps_ReturnsCorrectString()
		{
			var tsf = new TsStrFactory();
			ITsString tss = tsf.MakeStringWithPropsRgch(string.Empty, 0, new TsTextProps(1));
			Assert.That(tss.Text, Is.Null);
			Assert.That(tss.get_WritingSystem(0), Is.EqualTo(1));
		}

		[Test]
		public void MakeStringWithPropsRgch_InvalidLength_Throws()
		{
			var tsf = new TsStrFactory();
			Assert.That(() => tsf.MakeStringWithPropsRgch(string.Empty, 1, new TsTextProps(1)), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void MakeStringWithPropsRgch_NullProps_Throws()
		{
			var tsf = new TsStrFactory();
			Assert.That(() => tsf.MakeStringWithPropsRgch(string.Empty, 0, null), Throws.InstanceOf<ArgumentNullException>());
		}

		[Test]
		public void EmptyString_ValidWS_ReturnsCorrectString()
		{
			var tsf = new TsStrFactory();
			ITsString tss = tsf.EmptyString(1);
			Assert.That(tss.Text, Is.Null);
			Assert.That(tss.get_WritingSystem(0), Is.EqualTo(1));
		}

		[Test]
		public void EmptyString_ZeroWS_Throws()
		{
			var tsf = new TsStrFactory();
			Assert.That(() => tsf.EmptyString(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}
	}
}
