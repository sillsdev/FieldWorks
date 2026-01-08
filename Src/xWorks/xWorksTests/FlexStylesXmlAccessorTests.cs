// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.XWorks.LexText;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;
using SIL.TestUtilities;

// ReSharper disable InconsistentNaming

namespace SIL.FieldWorks.XWorks
{
	/// <summary/>
	public class FlexStylesXmlAccessorTests : MemoryOnlyBackendProviderTestBase
	{
		private const int CustomRedBGR = 0x0000FE;
		private readonly int NamedRedBGR = (int)ColorUtil.ConvertColorToBGR(Color.Red);

		[TearDown]
		public void TearDown()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => { Cache.LangProject.StylesOC.Clear(); });
		}

		[Test]
		public void WriteStyleXml()
		{
			// Add test styles to the cache
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var styleFactory = Cache.ServiceLocator.GetInstance<IStStyleFactory>();
				styleFactory.Create(Cache.LangProject.StylesOC, "Dictionary-Headword",
					ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Prose, true, 2, true);
				var testStyle = styleFactory.Create(Cache.LangProject.StylesOC, "TestStyle", ContextValues.InternalConfigureView, StructureValues.Undefined,
					FunctionValues.Prose, true, 2, false);
				testStyle.Usage.set_String(Cache.DefaultAnalWs, "Test Style");
				var normalStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Normal", ContextValues.InternalConfigureView, StructureValues.Undefined,
					FunctionValues.Prose, false, 2, true);
				// 'Normal' style has a no properties, but it must still create a paragraph element to be valid.
				var senseStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Dictionary-Sense",
					ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Prose, false, 2, true);
				var propsBldr = TsStringUtils.MakePropsBldr();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, 0x2BACCA); // arbitrary color to create para element
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, NamedRedBGR);
				propsBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Arial");
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptFontSize,
					(int)FwTextPropVar.ktpvMilliPoint, 23000);
				propsBldr.SetStrPropValue((int)FwTextPropType.ktptBulNumFontInfo, "");
				senseStyle.Rules = propsBldr.GetTextProps();
				senseStyle.BasedOnRA = normalStyle;
				// Chapter Number has a unique Function (@use)
				styleFactory.Create(Cache.LangProject.StylesOC, "Chapter Number", ContextValues.Text, StructureValues.Body,
					FunctionValues.Chapter, true, 0, true);
				// Verse Number is unserializable because it is superscript (this may change in the future)
				var verseNumberStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Verse Number", ContextValues.Text, StructureValues.Body,
					FunctionValues.Verse, true, 0, true);
				propsBldr = TsStringUtils.MakePropsBldr();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptSuperscript, (int)FwTextPropVar.ktpvDefault, 1);
				verseNumberStyle.Rules = propsBldr.GetTextProps();
				var styleWithNamedColors = styleFactory.Create(Cache.LangProject.StylesOC, "Nominal", ContextValues.InternalConfigureView, StructureValues.Body,
					FunctionValues.Prose, false, 2, false);
				styleWithNamedColors.BasedOnRA = normalStyle;
				propsBldr = TsStringUtils.MakePropsBldr();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, NamedRedBGR);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, NamedRedBGR);
				styleWithNamedColors.Rules = propsBldr.GetTextProps();
				var styleWithCustomColors = styleFactory.Create(Cache.LangProject.StylesOC, "Abnormal", ContextValues.InternalConfigureView, StructureValues.Heading,
					FunctionValues.Prose, false, 2, false);
				styleWithCustomColors.BasedOnRA = normalStyle;
				propsBldr = TsStringUtils.MakePropsBldr();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, CustomRedBGR);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, CustomRedBGR);
				styleWithCustomColors.Rules = propsBldr.GetTextProps();
			});

			// Export Styles
			var projectStyles = new FlexStylesXmlAccessor(Cache.LangProject.LexDbOA, true);
			var serializer = new XmlSerializer(typeof(FlexStylesXmlAccessor));

			string xmlResult;
			using (var memoryStream = new MemoryStream())
			using (var textWriter = new StreamWriter(memoryStream))
			{
				// SUT
				serializer.Serialize(textWriter, projectStyles);
				textWriter.Flush();

				memoryStream.Position = 0;
				using (var reader = new StreamReader(memoryStream))
				{
					xmlResult = reader.ReadToEnd();
				}
			}

			// Verify XML
			AssertThatXmlIn.String(xmlResult).HasSpecifiedNumberOfMatchesForXpath("//tag", 7);
			// Dictionary-Headword
			AssertThatXmlIn.String(xmlResult).HasSpecifiedNumberOfMatchesForXpath(
				"//tag[@id='Dictionary-Headword' and @userlevel='2' and @context='internalConfigureView' and @type='character' and @structure='body' and @guid][font]",
				1);
			// TestStyle (has usage and empty font)
			AssertThatXmlIn.String(xmlResult).HasSpecifiedNumberOfMatchesForXpath(
				"//tag[@id='TestStyle' and @userlevel='2' and @context='internalConfigureView' and @type='character' and @guid][usage[@wsId='en' and text()='Test Style'] and font]",
				1);
			// Normal (paragraph with empty font and paragraph)
			AssertThatXmlIn.String(xmlResult).HasSpecifiedNumberOfMatchesForXpath(
				"//tag[@id='Normal' and @userlevel='2' and @context='internalConfigureView' and @type='paragraph' and @guid and font and paragraph]",
				1);
			// Dictionary-Sense (paragraph with font attributes and paragraph containing BulNumFontInfo)
			AssertThatXmlIn.String(xmlResult).HasSpecifiedNumberOfMatchesForXpath(
				"//tag[@id='Dictionary-Sense' and @userlevel='2' and @context='internalConfigureView' and @type='paragraph' and @structure='body' and @guid]" +
				"[font[@size='23 pt' and @family='Arial' and @backcolor='(202,172,43)' and @color='red'] and " +
				"paragraph[@background='(202,172,43)' and @basedOn='Normal' and @bulNumScheme='None' and @bulNumStartAt='0' and BulNumFontInfo[@size='10 pt' and " +
				"@family='<default font>' and @bold='false' and @italic='false' and @color='black' and @underlineColor='black' and @underline='none']]]",
				1);
			// Verse Number (character style in 'text' context with use 'verse')
			AssertThatXmlIn.String(xmlResult).HasSpecifiedNumberOfMatchesForXpath(
				@"//tag[@id='Chapter_Number' and @userlevel='0' and @context='text' and @type='character' and @structure='body' and @use='chapter' and @guid][font]",
				1);
			// Nominal (named-color 'red' expected; paragraph basedOn Normal with numeric RGB background)
			AssertThatXmlIn.String(xmlResult).HasSpecifiedNumberOfMatchesForXpath(
				"//tag[@id='Nominal' and @userlevel='2' and @context='internalConfigureView' and @type='paragraph' and @structure='body' and @guid]" +
				"[font[@backcolor='red' and @color='red'] and paragraph[@background='(255,0,0)' and @basedOn='Normal']]",
				1);
			// Abnormal (custom color expected as RGB tuples)
			AssertThatXmlIn.String(xmlResult).HasSpecifiedNumberOfMatchesForXpath(
				"//tag[@id='Abnormal' and @userlevel='2' and @context='internalConfigureView' and @type='paragraph' and @structure='heading' and @guid]" +
				"[font[@backcolor='(254,0,0)' and @color='(254,0,0)'] and paragraph[@background='(254,0,0)' and @basedOn='Normal']]",
				1);
		}
	}
}
