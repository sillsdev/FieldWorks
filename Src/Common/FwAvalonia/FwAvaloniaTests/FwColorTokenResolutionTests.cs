// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Headless.NUnit;
using Avalonia.Media;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Proves the shared FwAvaloniaTheme token dictionaries (Src/Common/FwAvaloniaTheme/Tokens/)
	/// actually flow through Application.Resources into FwAvaloniaDensity properties, under the
	/// same headless app (TestAppBuilder -> FwAvaloniaApp) the rest of the suite uses. If the
	/// merge/wiring in FwAvaloniaApp.Initialize() is ever broken, these must fail loudly rather
	/// than silently pass via a hardcoded fallback -- FwAvaloniaDensity has none.
	/// </summary>
	[TestFixture]
	public class FwColorTokenResolutionTests
	{
		[AvaloniaTest]
		public void LabelBrush_ResolvesFromMergedThemeDictionary()
		{
			var brush = FwAvaloniaDensity.LabelBrush as SolidColorBrush;

			Assert.That(brush, Is.Not.Null, "LabelBrush must resolve to a SolidColorBrush");
			Assert.That(brush.Color, Is.EqualTo(Color.FromRgb(0x69, 0x69, 0x69)));
		}

		[AvaloniaTest]
		public void LabelColumnWidth_ResolvesFromMergedDataTreeTokenDictionary()
		{
			Assert.That(FwAvaloniaDensity.LabelColumnWidth, Is.EqualTo(150d));
		}
	}
}
