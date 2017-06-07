// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using NUnit.Framework;
using SIL.CoreImpl.Text;
using SIL.FieldWorks.Common.FwKernelInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainServices;

namespace LanguageExplorerTests.Interlinear
{
	/// <summary>
	/// Test a class which decorates StTxtPara.Contents to make zero-width spaces visible.
	/// </summary>
	[TestFixture]
	public class ShowSpaceDecoratorTests
	{
		private string zws = AnalysisOccurrence.KstrZws;

		[Test]
		public void DecoratorDoesNothingWhenTurnedOff()
		{
			var mockDa = new MockDa();
			var underlyingValue = "hello" + zws + "world";
			mockDa.StringValues[new Tuple<int, int>(27, StTxtParaTags.kflidContents)] = TsStringUtils.MakeString(underlyingValue, 77);
			var decorator = new ShowSpaceDecorator(mockDa);

			var tss = decorator.get_StringProp(27, StTxtParaTags.kflidContents);
			Assert.That(tss.Text, Is.EqualTo(underlyingValue));
			VerifyNoBackColor(tss);
		}

		[Test]
		public void DecoratorGetHandlesEmptyStringWhenTurnedOn()
		{
			var mockDa = new MockDa();
			var underlyingValue = "";
			mockDa.StringValues[new Tuple<int, int>(27, StTxtParaTags.kflidContents)] = TsStringUtils.MakeString(underlyingValue, 77);
			var decorator = new ShowSpaceDecorator(mockDa);
			decorator.ShowSpaces = true;

			var tss = decorator.get_StringProp(27, StTxtParaTags.kflidContents);
			Assert.That(string.IsNullOrEmpty(tss.Text));
			VerifyNoBackColor(tss);
		}

		[Test]
		public void DecoratorReplacesZwsWithGreySpaceWhenTurnedOn()
		{
			var mockDa = new MockDa();
			var underlyingValue = zws + "hello" + zws + "world" + zws + "today";
			mockDa.StringValues[new Tuple<int, int>(27, StTxtParaTags.kflidContents)] = TsStringUtils.MakeString(underlyingValue, 77);
			var decorator = new ShowSpaceDecorator(mockDa);
			decorator.ShowSpaces = true;

			var tss = decorator.get_StringProp(27, StTxtParaTags.kflidContents);
			Assert.That(tss.Text, Is.EqualTo(" hello world today"));
			VerifyBackColor(tss, new[] { ShowSpaceDecorator.KzwsBackColor, -1, ShowSpaceDecorator.KzwsBackColor, -1, ShowSpaceDecorator.KzwsBackColor, -1 });
		}

		[Test]
		public void DecoratorReplacesGreySpaceWithZwsWhenTurnedOn()
		{
			var mockDa = new MockDa();
			var underlyingValue = "hello world today keep these spaces";
			var bldr = TsStringUtils.MakeString(underlyingValue, 77).GetBldr();
			bldr.SetIntPropValues("hello world".Length, "hello world".Length + 1, (int) FwTextPropType.ktptBackColor,
				(int) FwTextPropVar.ktpvDefault, ShowSpaceDecorator.KzwsBackColor);
			bldr.SetIntPropValues("hello".Length, "hello".Length + 1, (int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault,
					ShowSpaceDecorator.KzwsBackColor);
			var decorator = new ShowSpaceDecorator(mockDa);
			decorator.ShowSpaces = true;
			decorator.SetString(27, StTxtParaTags.kflidContents, bldr.GetString());
			var tss = mockDa.StringValues[new Tuple<int, int>(27, StTxtParaTags.kflidContents)];
			Assert.That(tss.Text, Is.EqualTo("hello" + zws + "world" + zws + "today keep these spaces"));
			VerifyNoBackColor(tss);
		}

		[Test]
		public void DecoratorSetHandlesEmptyStringWhenTurnedOn()
		{
			var mockDa = new MockDa();
			var underlyingValue = "";
			var bldr = TsStringUtils.MakeString(underlyingValue, 77).GetBldr();
			var decorator = new ShowSpaceDecorator(mockDa);
			decorator.ShowSpaces = true;
			decorator.SetString(27, StTxtParaTags.kflidContents, bldr.GetString());
			var tss = mockDa.StringValues[new Tuple<int, int>(27, StTxtParaTags.kflidContents)];
			Assert.That(string.IsNullOrEmpty(tss.Text));
			VerifyNoBackColor(tss);
		}

		private void VerifyNoBackColor(ITsString tss)
		{
			for (int irun = 0; irun < tss.RunCount; irun++)
			{
				int nVar;
				Assert.That(tss.get_Properties(irun).GetIntPropValues((int)FwTextPropType.ktptBackColor, out nVar), Is.EqualTo(-1));
				Assert.That(nVar, Is.EqualTo(-1));
			}
		}

		private void VerifyBackColor(ITsString tss, int[] colors)
		{
			Assert.That(tss.RunCount, Is.EqualTo(colors.Length));
			for (int irun = 0; irun < tss.RunCount; irun++)
			{
				int nVar;
				Assert.That(tss.get_Properties(irun).GetIntPropValues((int)FwTextPropType.ktptBackColor, out nVar), Is.EqualTo(colors[irun]));
				if (colors[irun] == -1)
					Assert.That(nVar, Is.EqualTo(-1));
				else
					Assert.That(nVar, Is.EqualTo((int)FwTextPropVar.ktpvDefault));
			}
		}
	}

	class MockDa : SilDataAccessManagedBase
	{
		public Dictionary<Tuple<int, int>, ITsString> StringValues = new Dictionary<Tuple<int, int>, ITsString>();
		public override ITsString get_StringProp(int hvo, int tag)
		{
			return StringValues[new Tuple<int, int>(hvo, tag)];
		}

		public override void SetString(int hvo, int tag, ITsString tss)
		{
			StringValues[new Tuple<int, int>(hvo, tag)] = tss;
		}
	}
}
