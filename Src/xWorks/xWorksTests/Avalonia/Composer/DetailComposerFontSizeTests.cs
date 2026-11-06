// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The Avalonia detail view shows its data at the project's Normal style size, as the
	/// WinForms slices do, so changing Normal in Format > Styles resizes it (LT-22811).
	/// </summary>
	[TestFixture]
	public class DetailComposerFontSizeTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		// Avalonia sizes text in device-independent units, 96 to the inch.
		private const double UnitsPerPoint = 96.0 / 72.0;

		private ILexEntry m_entry;

		public override void TestSetup()
		{
			base.TestSetup();
			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			m_entry.LexemeFormOA = morph;
			morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			m_entry.SensesOS.Add(sense);
			sense.Gloss.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("house", Cache.DefaultAnalWs));
		}

		// Normal with the given size for all writing systems, created if the project has none.
		private IStStyle NormalStyle(int millipoints)
		{
			var normal = Cache.LangProject.StylesOC
				.FirstOrDefault(s => s.Name == StyleServices.NormalStyleName);
			if (normal == null)
			{
				normal = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
				Cache.LangProject.StylesOC.Add(normal);
				normal.Name = StyleServices.NormalStyleName;
				normal.Type = StyleType.kstParagraph;
				normal.IsBuiltIn = true;
			}
			var props = TsStringUtils.MakePropsBldr();
			props.SetIntPropValues((int)FwTextPropType.ktptFontSize,
				(int)FwTextPropVar.ktpvMilliPoint, millipoints);
			normal.Rules = props.GetTextProps();
			return normal;
		}

		private DetailWsValue Value(ComposedDetail composed, string text)
			=> composed.Model.Fields.Where(f => f.Values != null).SelectMany(f => f.Values)
				.Single(v => v.Value == text);

		[Test]
		public void Compose_ValuesShowAtTheNormalStyleSize()
		{
			NormalStyle(20000);

			var composed = DetailComposer.Compose(m_entry, Cache);

			Assert.That(Value(composed, "house").FontSize, Is.EqualTo(20 * UnitsPerPoint).Within(0.01));
			Assert.That(composed.Model.ValueFontSize, Is.EqualTo(20 * UnitsPerPoint).Within(0.01),
				"values with no writing system of their own show at the same size");
			Assert.That(Value(composed, "casa").FontSize, Is.EqualTo(1.2 * 20 * UnitsPerPoint).Within(0.01),
				"the lexeme form keeps its 120% emphasis, now of Normal's size");
		}

		[Test]
		public void Compose_UsesNormalsOwnSizeForAWritingSystem()
		{
			var normal = NormalStyle(20000);
			var info = new StyleInfo(normal);
			info.FontInfoForWs(Cache.DefaultVernWs).m_fontSize.ExplicitValue = 30000;
			info.SaveToDB(normal, true, true);

			var composed = DetailComposer.Compose(m_entry, Cache);

			Assert.That(Value(composed, "casa").FontSize, Is.EqualTo(1.2 * 30 * UnitsPerPoint).Within(0.01),
				"the vernacular lexeme form uses Normal's vernacular size");
			Assert.That(Value(composed, "house").FontSize, Is.EqualTo(20 * UnitsPerPoint).Within(0.01),
				"the analysis gloss keeps Normal's size for all writing systems");
		}

		// The sizes are reused between composes, so this pins that a Styles-dialog change to
		// Normal still reaches the next compose.
		[Test]
		public void Compose_AfterNormalChanges_UsesTheNewSize()
		{
			NormalStyle(20000);
			DetailComposer.Compose(m_entry, Cache);
			NormalStyle(30000);

			var composed = DetailComposer.Compose(m_entry, Cache);

			Assert.That(Value(composed, "house").FontSize, Is.EqualTo(30 * UnitsPerPoint).Within(0.01));
		}

		[Test]
		public void ErrorFallback_ShowsValuesAtTheNormalStyleSize()
		{
			NormalStyle(20000);

			var model = LexiconEditErrorFallback.Build(m_entry, Cache);

			Assert.That(model.ValueFontSize, Is.EqualTo(20 * UnitsPerPoint).Within(0.01));
			var gloss = model.Fields.Single(f => f.Field == "Gloss").Values
				.Single(v => v.Value == "house");
			Assert.That(gloss.FontSize, Is.EqualTo(20 * UnitsPerPoint).Within(0.01));
		}

		[Test]
		public void Compose_WithNoNormalStyle_ShowsValuesAtTenPoints()
		{
			var normal = Cache.LangProject.StylesOC
				.FirstOrDefault(s => s.Name == StyleServices.NormalStyleName);
			if (normal != null)
				Cache.LangProject.StylesOC.Remove(normal);

			var composed = DetailComposer.Compose(m_entry, Cache);

			Assert.That(Value(composed, "house").FontSize, Is.EqualTo(10 * UnitsPerPoint).Within(0.01));
		}
	}
}
