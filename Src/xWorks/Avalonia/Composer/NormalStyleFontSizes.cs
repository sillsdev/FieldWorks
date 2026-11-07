// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The project's Normal style font size for each writing system, in Avalonia
	/// device-independent units. Detail-view data shows at this size, as the WinForms slices
	/// draw unstyled text in Normal (LT-22811).
	/// </summary>
	internal sealed class NormalStyleFontSizes
	{
		// The size Normal falls back to when the project's style sets none, in millipoints.
		private const int DefaultMillipoints = 10000;
		private const double MillipointsPerPoint = 1000.0;
		private const double UnitsPerPoint = 96.0 / 72.0;

		private static readonly ConditionalWeakTable<LcmCache, NormalStyleFontSizes> s_byProject =
			new ConditionalWeakTable<LcmCache, NormalStyleFontSizes>();

		private readonly ITsTextProps _rules;
		private readonly BaseStyleInfo _normal;
		private readonly Dictionary<int, double> _sizes = new Dictionary<int, double>();

		/// <summary>
		/// The sizes for <paramref name="cache"/>'s project as its Normal style is now. Reuses
		/// the last result until the style's rules change, as saving the Styles dialog does.
		/// </summary>
		public static NormalStyleFontSizes ForProject(LcmCache cache)
		{
			var rules = NormalStyle(cache)?.Rules;
			if (s_byProject.TryGetValue(cache, out var sizes) && ReferenceEquals(sizes._rules, rules))
				return sizes;
			sizes = new NormalStyleFontSizes(cache, rules);
			s_byProject.Remove(cache);
			s_byProject.Add(cache, sizes);
			return sizes;
		}

		private NormalStyleFontSizes(LcmCache cache, ITsTextProps rules)
		{
			_rules = rules;
			if (rules == null)
				return;
			var styleSheet = new LcmStyleSheet();
			styleSheet.Init(cache, cache.LangProject.Hvo, LangProjectTags.kflidStyles);
			if (styleSheet.Styles.Contains(StyleServices.NormalStyleName))
				_normal = styleSheet.Styles[StyleServices.NormalStyleName];
		}

		private static IStStyle NormalStyle(LcmCache cache)
			=> cache.LangProject?.StylesOC.FirstOrDefault(s => s.Name == StyleServices.NormalStyleName);

		/// <summary>
		/// Normal's size for the writing system with handle <paramref name="ws"/>: its own size
		/// when Normal sets one for it, otherwise Normal's size for all writing systems.
		/// </summary>
		public double For(int ws)
		{
			if (!_sizes.TryGetValue(ws, out var size))
			{
				size = Millipoints(ws) / MillipointsPerPoint * UnitsPerPoint;
				_sizes[ws] = size;
			}
			return size;
		}

		/// <summary>
		/// Normal's size for the writing system with handle <paramref name="ws"/>, scaled by a
		/// part's fontsize <paramref name="percent"/>; 0 or less leaves it unscaled.
		/// </summary>
		public double For(int ws, int percent) => For(ws) * (percent > 0 ? percent / 100.0 : 1.0);

		private int Millipoints(int ws)
		{
			if (_normal == null)
				return DefaultMillipoints;
			var own = _normal.FontInfoForWs(ws)?.m_fontSize;
			if (own != null && own.ValueIsSet && own.Value > 0)
				return own.Value;
			var all = _normal.FontInfoForWs(-1)?.m_fontSize;
			return all != null && all.ValueIsSet && all.Value > 0 ? all.Value : DefaultMillipoints;
		}
	}
}
