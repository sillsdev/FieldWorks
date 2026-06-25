// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Avalonia.Controls;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.Reporting;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 3.1) — the Basic IPA Symbol editor plugin: claims the legacy
	/// <c>SIL.FieldWorks.XWorks.MorphologyEditor.BasicIPASymbolSlice</c> and renders a phoneme's
	/// <c>BasicIPASymbol</c> as an editable entry (<see cref="BasicIPASymbolEditor"/>). LCModel read/write
	/// lives here (design Decision 1), including derive-on-commit (<see cref="BasicIpaSymbolDeriver"/> fills
	/// empty Description/Features from BasicIPAInfo.xml — legacy parity).
	/// </summary>
	public sealed class BasicIpaSymbolRegionEditorPlugin : IRegionEditorPlugin
	{
		public const string BasicIPASymbolSliceClassName =
			"SIL.FieldWorks.XWorks.MorphologyEditor.BasicIPASymbolSlice";

		public string LegacyClassName => BasicIPASymbolSliceClassName;

		public Control BuildControl(RegionEditorBuildContext context)
		{
			var phoneme = context?.Target as IPhPhoneme;
			var cache = context?.Cache;
			if (phoneme == null || cache == null)
				return null;
			try
			{
				var symbol = phoneme.BasicIPASymbol?.Text ?? string.Empty;
				var host = context.EditContext;
				var sink = host == null ? null : new BasicIpaSymbolEditSink(phoneme, cache, host);
				return new BasicIPASymbolEditor(symbol, sink);
			}
			catch (Exception e)
			{
				Logger.WriteEvent($"BasicIpaSymbolRegionEditorPlugin: IPA symbol editor unavailable for '{phoneme.Guid}': {e}");
				return null;
			}
		}
	}

	/// <summary>
	/// avalonia-rule-formula-editor (task 3.1) — writes the phoneme's <c>BasicIPASymbol</c> (pronunciation
	/// writing system) through the region's shared fenced session as one undo step, then runs the
	/// derive-on-commit (<see cref="BasicIpaSymbolDeriver"/>) in the same UOW.
	/// </summary>
	internal sealed class BasicIpaSymbolEditSink : IBasicIpaSymbolCommandSink
	{
		private readonly IPhPhoneme _phoneme;
		private readonly LcmCache _cache;
		private readonly IRegionEditContext _host;

		public BasicIpaSymbolEditSink(IPhPhoneme phoneme, LcmCache cache, IRegionEditContext host)
		{
			_phoneme = phoneme;
			_cache = cache;
			_host = host;
		}

		public bool Commit(string symbol)
		{
			var ws = _cache.DefaultPronunciationWs > 0 ? _cache.DefaultPronunciationWs : _cache.DefaultVernWs;
			bool Write()
			{
				_phoneme.BasicIPASymbol = TsStringUtils.MakeString(symbol ?? string.Empty, ws);
				// Derive-on-commit: fill empty Description + Features from BasicIPAInfo.xml (legacy parity).
				BasicIpaSymbolDeriver.Derive(_phoneme, _cache);
				return true;
			}

			if (_host is RegionEditContextBase fenced)
			{
				var ok = fenced.Stage(Write, "Basic IPA Symbol");
				if (ok)
					fenced.Commit();
				return ok;
			}
			return Write();
		}
	}
}
