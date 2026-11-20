// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The composer hands the Avalonia detail view the font the project's UiElement style gives
	/// the UI writing system, the font the WinForms detail view draws section headings in.
	/// </summary>
	[TestFixture]
	public class DetailUiElementFontTests : MemoryOnlyBackendProviderTestBase
	{
		private void AddUiElementStyle(string fontName, int sizeMillipoints = 0)
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var style = Cache.LangProject.StylesOC.FirstOrDefault(
					s => s.Name == StyleServices.UiElementStylename);
				if (style == null)
				{
					style = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
					Cache.LangProject.StylesOC.Add(style);
					style.Name = StyleServices.UiElementStylename;
					style.Type = StyleType.kstCharacter;
					style.Context = ContextValues.Internal;
					style.Function = FunctionValues.Prose;
					style.Structure = StructureValues.Body;
				}
				// The same per-writing-system override string FlexStyles.xml loads into the
				// style.
				var font = new FontInfo { m_fontName = new InheritableStyleProp<string>(fontName) };
				if (sizeMillipoints > 0)
					font.m_fontSize = new InheritableStyleProp<int>(sizeMillipoints);
				var overrides = new Dictionary<int, FontInfo> { { Cache.DefaultUserWs, font } };
				var props = TsStringUtils.MakePropsBldr();
				props.SetStrPropValue((int)FwTextPropType.ktptWsStyle,
					BaseStyleInfo.GetOverridesString(overrides));
				style.Rules = props.GetTextProps();
			});
		}

		[Test]
		public void NoUiElementStyle_GivesNoFont()
		{
			Assert.That(DetailComposer.UiElementFontName(Cache), Is.Null);
		}

		[Test]
		public void UiElementFontForTheUserWritingSystem_IsReturned()
		{
			AddUiElementStyle("Microsoft Sans Serif");

			Assert.That(DetailComposer.UiElementFontName(Cache), Is.EqualTo("Microsoft Sans Serif"));
		}

		[Test]
		public void NoUiElementStyle_GivesNoSize()
		{
			Assert.That(DetailComposer.UiElementFontSize(Cache), Is.EqualTo(0));
		}

		[Test]
		public void UiElementSizeForTheUserWritingSystem_IsReturnedInDeviceIndependentUnits()
		{
			AddUiElementStyle("Microsoft Sans Serif", 12000);

			Assert.That(DetailComposer.UiElementFontSize(Cache), Is.EqualTo(16.0).Within(0.001),
				"12 pt is 16 device-independent units");
		}

		[Test]
		public void UiElementStyleWithoutASize_GivesNoSize()
		{
			AddUiElementStyle("Microsoft Sans Serif");

			Assert.That(DetailComposer.UiElementFontSize(Cache), Is.EqualTo(0));
		}

		[Test]
		public void UiElementDefaultFont_GivesNoFont()
		{
			AddUiElementStyle(StyleServices.DefaultFont);

			Assert.That(DetailComposer.UiElementFontName(Cache), Is.Null);
		}

		// A LiteralString summary part composes as a header row, which the data tree draws in the
		// model's UiElement font.
		[Test]
		public void PublicationSettingsHeading_ComposesAsAHeaderInTheUiElementFont()
		{
			AddUiElementStyle("Microsoft Sans Serif", 12000);
			ILexEntry entry = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create());

			var model = DetailComposer.Compose(entry, Cache).Model;

			var heading = model.Fields.FirstOrDefault(f => f.Label == "Publication Settings");
			Assert.That(heading, Is.Not.Null, "precondition: the entry composes the heading");
			Assert.That(heading.Kind, Is.EqualTo(DetailFieldKind.Header));
			Assert.That(model.UiElementFontFamily, Is.EqualTo("Microsoft Sans Serif"));
			Assert.That(model.UiElementFontSize, Is.EqualTo(16.0).Within(0.001));
		}
	}
}
