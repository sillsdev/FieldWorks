// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>Checks interface font selection, bundled faces and glyph rendering.</summary>
	[TestFixture]
	public class FwUiFontTests
	{
		private static IGlyphTypeface Resolve(FontWeight weight, FontStyle style = FontStyle.Normal)
		{
			var bundled = new FontFamily(FwUiFont.CollectionKey + "#Andika");
			Assert.That(FontManager.Current.TryGetGlyphTypeface(new Typeface(bundled, style, weight), out var face),
				Is.True, "Andika " + weight + " " + style + " must resolve from the bundled collection");
			return face;
		}

		// Shapes the text in the interface font and returns the typeface and glyph id of each
		// character, so a missing glyph (id 0) or a wrong face fails with the language named.
		private static List<(IGlyphTypeface Face, ushort Glyph)> Shape(FontFamily family, string text)
		{
			var layout = new TextLayout(text, new Typeface(family), 13, Brushes.Black);
			var result = new List<(IGlyphTypeface, ushort)>();
			foreach (var run in layout.TextLines.SelectMany(l => l.TextRuns).OfType<ShapedTextRun>())
				foreach (var info in run.GlyphRun.GlyphInfos)
					result.Add((run.GlyphRun.GlyphTypeface, info.GlyphIndex));
			return result;
		}

		/// <summary>UI languages of the Localizations/l10ns satellites, with a sample string in
		/// each one's script. Five have explicit fallback lists.</summary>
		private static readonly object[] FallbackLanguages =
		{
			new object[] { "fa", "\u0641\u0627\u06CC\u0644" },
			new object[] { "hi", "\u092B\u093C\u093E\u0907\u0932" },
			new object[] { "ko", "\uD30C\uC77C" },
			new object[] { "te", "\u0C2B\u0C48\u0C32\u0C4D" },
			new object[] { "zh-CN", "\u6587\u4EF6" },
		};

		private static string Sample(string tag)
			=> (string)FallbackLanguages.Cast<object[]>().Single(r => (string)r[0] == tag)[1];

		private static readonly object[] LanguagesWithoutFallback =
		{
			new object[] { "az", "\u018Flav\u0259 et \u015F\u011F\u0131" },
			new object[] { "es", "A\u00F1adir \u00BF\u00E1\u00E9\u00ED\u00F3\u00FA\u00FC" },
			new object[] { "fr", "Fichier \u00E0\u00E9\u00E8\u00EA\u00E7\u0153" },
			new object[] { "hu", "F\u00E1jl \u0151\u0171\u0150\u0170" },
			new object[] { "id", "Tambah berkas" },
			new object[] { "ms", "Tambah fail" },
			new object[] { "pt", "A\u00E7\u00E3o \u00E3\u00F5" },
			new object[] { "ru", "\u0424\u0430\u0439\u043B \u0401\u0451" },
			new object[] { "rw", "Dosiye" },
			new object[] { "tr", "Dosyas\u0131 \u011F\u015F\u0130" },
			new object[] { "vi", "T\u1EC7p tin \u1EDF \u1EBF \u1EEF" },
		};

		private static void AssertUsesInterfaceFont(Control control)
		{
			Assert.That(control.GetValue(TextElement.FontFamilyProperty), Is.EqualTo(FwUiFont.UiFontFamily),
				control.GetType().Name + " must inherit the interface font family");
			var latin = Shape(control.GetValue(TextElement.FontFamilyProperty), "Gloss");
			Assert.That(latin, Is.Not.Empty);
			Assert.That(latin.All(g => g.Glyph != 0), Is.True);
		}

		[AvaloniaTest]
		public void UiFontFamilyToken_NamesSegoeUi()
		{
			Assert.That(FwUiFont.UiFontFamily.FamilyNames.First(), Is.EqualTo("Segoe UI"));
		}

		[AvaloniaTest]
		public void PlainFamilyName_IsNotCapturedByThePrivateCollection()
		{
			Assert.That(new FontFamily("Andika").Key, Is.Null);
		}

		[AvaloniaTest]
		public void BundledFaces_ResolveForEveryWeightInUse()
		{
			foreach (var weight in new[] { FontWeight.Normal, FontWeight.SemiBold, FontWeight.Bold })
				Assert.That(Resolve(weight).FamilyName, Does.StartWith("Andika"));
			Assert.That(Resolve(FontWeight.Normal, FontStyle.Italic).FamilyName, Is.EqualTo("Andika"));
		}

		private static void AssertFallbackLanguageDrawsItsSample(string tag, string sample)
		{
			var culture = new CultureInfo(tag);
			Assert.That(FwUiFont.FallbackFamiliesFor(culture), Is.Not.Empty, tag);
			var shaped = Shape(FwUiFont.FamilyFor(culture), sample);

			Assert.That(shaped, Is.Not.Empty, tag);
			foreach (var (face, glyph) in shaped)
			{
				Assert.That(glyph, Is.Not.EqualTo((ushort)0), tag + " has a missing glyph in " + face.FamilyName);
			}
		}

		[AvaloniaTest]
		public void Persian_SampleResolvesToFaceWithItsGlyphs() => AssertFallbackLanguageDrawsItsSample("fa", Sample("fa"));

		[AvaloniaTest]
		public void Hindi_SampleResolvesToFaceWithItsGlyphs() => AssertFallbackLanguageDrawsItsSample("hi", Sample("hi"));

		[AvaloniaTest]
		public void Korean_SampleResolvesToFaceWithItsGlyphs() => AssertFallbackLanguageDrawsItsSample("ko", Sample("ko"));

		[AvaloniaTest]
		public void Telugu_SampleResolvesToFaceWithItsGlyphs() => AssertFallbackLanguageDrawsItsSample("te", Sample("te"));

		[AvaloniaTest]
		public void SimplifiedChinese_SampleResolvesToFaceWithItsGlyphs() => AssertFallbackLanguageDrawsItsSample("zh-CN", Sample("zh-CN"));

		[AvaloniaTest]
		public void LanguagesWithoutNamedFallback_DrawTheirSamples()
		{
			foreach (object[] row in LanguagesWithoutFallback)
			{
				var tag = (string)row[0];
				var culture = new CultureInfo(tag);
				Assert.That(FwUiFont.FallbackFamiliesFor(culture), Is.Empty, tag);
				var shaped = Shape(FwUiFont.FamilyFor(culture), ((string)row[1]).Replace(" ", ""));

				Assert.That(shaped, Is.Not.Empty, tag);
				foreach (var (face, glyph) in shaped)
				{
					Assert.That(glyph, Is.Not.EqualTo((ushort)0), tag);
				}
			}
		}

		[AvaloniaTest]
		public void FiveLanguages_HaveNamedFallbacks()
		{
			foreach (object[] row in FallbackLanguages)
				Assert.That(FwUiFont.FallbackFamiliesFor(new CultureInfo((string)row[0])), Is.Not.Empty);
			Assert.That(FwUiFont.FallbackFamiliesFor(CultureInfo.InvariantCulture), Is.Empty);
		}

		[TestCase("ar", "\u0645\u0644\u0641")]
		[TestCase("bn", "\u09AC\u09BE\u0982\u09B2\u09BE")]
		[TestCase("km", "\u1781\u17D2\u1798\u17C2\u179A")]
		[TestCase("ml", "\u0D2E\u0D32\u0D2F\u0D3E\u0D33\u0D02")]
		[TestCase("my", "\u1019\u103C\u1014\u103A\u1019\u102C")]
		[TestCase("ne", "\u0928\u0947\u092A\u093E\u0932\u0940")]
		[TestCase("ta", "\u0BA4\u0BAE\u0BBF\u0BB4\u0BCD")]
		[TestCase("th", "\u0E44\u0E17\u0E22")]
		[TestCase("ur", "\u0627\u0631\u062F\u0648")]
		[AvaloniaTest]
		public void InstallerLanguage_PlatformFallbackDrawsSample(string tag, string sample)
		{
			var glyphs = Shape(FwUiFont.FamilyFor(new CultureInfo(tag)), sample);
			Assert.That(glyphs, Is.Not.Empty, tag);
			Assert.That(glyphs.All(g => g.Glyph != 0), Is.True, tag);
		}

		[AvaloniaTest]
		public void ApplyTo_CalledAgain_ChangesNothing()
		{
			FwUiFont.ApplyTo(Application.Current);
			Assert.That(Resolve(FontWeight.Normal).FamilyName, Is.EqualTo("Andika"));
		}

		[AvaloniaTest]
		public void TextInAWindow_InheritsTheInterfaceFont()
		{
			var panel = new StackPanel();
			var text = new TextBlock { Text = "Lexeme Form" };
			var button = new Button { Content = "OK" };
			var label = new Label { Content = "Gloss" };
			panel.Children.Add(text);
			panel.Children.Add(button);
			panel.Children.Add(label);
			var window = new Window { Content = panel };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			AssertUsesInterfaceFont(text);
			AssertUsesInterfaceFont(button);
			AssertUsesInterfaceFont(label);
		}

		[AvaloniaTest]
		public void TextInAnEmbeddableControlRoot_InheritsTheInterfaceFont()
		{
			// The WinForms-hosted production root. If it cannot be shown headless, the same
			// guarantee is the application-level resources Semi's root theme reads.
			var app = Application.Current;
			foreach (var key in new[] { "DefaultFontFamily", "ContentControlThemeFontFamily", "SemiFontFamilyRegular" })
			{
				Assert.That(app.TryGetResource(key, app.ActualThemeVariant, out var value), Is.True, key);
				Assert.That(value, Is.EqualTo(FwUiFont.UiFontFamily), key);
			}
		}

		[AvaloniaTest]
		public void FlyoutText_InheritsTheInterfaceFont()
		{
			var text = new TextBlock { Text = "Insert" };
			var anchor = new Button { Content = "Menu", Flyout = new Flyout { Content = text } };
			var window = new Window { Content = anchor };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			anchor.Flyout.ShowAt(anchor);
			Dispatcher.UIThread.RunJobs();

			AssertUsesInterfaceFont(text);
		}

		[AvaloniaTest]
		public void ContextMenuItem_InheritsTheInterfaceFont()
		{
			var item = new MenuItem { Header = "Copy" };
			var anchor = new Button { Content = "Target", ContextMenu = new ContextMenu { Items = { item } } };
			var window = new Window { Content = anchor };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			anchor.ContextMenu.Open(anchor);
			Dispatcher.UIThread.RunJobs();

			AssertUsesInterfaceFont(item);
		}

		[AvaloniaTest]
		public void WsAbbrevColumnWidth_MeasuresInTheUiFont()
		{
			const string abbrev = "Sen-Sup";
			var field = new DetailField("f1", "Form", "Form", null, DetailFieldKind.Text,
				EditorClassification.Known, "f1", null, HostRouting.Inherit,
				new List<DetailWsValue> { new DetailWsValue(abbrev, "barigi") }, null, null, isEditable: true);
			field.IsMultiStringRow = true;
			var model = new DetailModel("LexEntry", "Normal", new List<DetailField> { field },
				new List<ViewDiagnostic>());

			var formatted = new FormattedText(abbrev, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
				new Typeface(FwUiFont.UiFontFamily), FwAvaloniaDensity.WsAbbrevFontSize,
				FwAvaloniaDensity.PickerForegroundBrush);
			var expected = Math.Max(FwAvaloniaDensity.WsAbbrevWidth,
				Math.Min(formatted.Width + FwAvaloniaDensity.WsAbbrevGutter, FwAvaloniaDensity.WsAbbrevMaxWidth));

			Assert.That(FwMultiWsTextField.ComputeWsAbbrevColumnWidth(model), Is.EqualTo(expected).Within(0.001));
		}
	}
}
