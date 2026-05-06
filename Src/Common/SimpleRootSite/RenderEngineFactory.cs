// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.ObjectModel;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// This class is used by root boxes to get render engines.
	/// </summary>
	public class RenderEngineFactory : DisposableBase, IRenderEngineFactory
	{
		private readonly Dictionary<ILgWritingSystem, Dictionary<Tuple<string, bool, bool, string>, Tuple<bool, IRenderEngine>>> m_fontEngines;

		/// <summary>
		/// Initializes a new instance of the <see cref="RenderEngineFactory"/> class.
		/// </summary>
		public RenderEngineFactory()
		{
			m_fontEngines = new Dictionary<ILgWritingSystem, Dictionary<Tuple<string, bool, bool, string>, Tuple<bool, IRenderEngine>>>();
		}

		/// <summary>
		/// Get the engine used to render text with the specified properties. At present only
		/// font, bold, and italic properties are significant.
		/// Font name may be '&lt;default font&gt;' which produces a renderer suitable for the default
		/// font.
		/// </summary>
		public IRenderEngine get_Renderer(ILgWritingSystem ws, IVwGraphics vg)
		{
			LgCharRenderProps chrp = vg.FontCharProperties;
			string fontName = MarshalEx.UShortToString(chrp.szFaceName);
			if (fontName == "<default font>")
			{
				fontName = ws.DefaultFontName;
				MarshalEx.StringToUShort(fontName, chrp.szFaceName);
				vg.SetupGraphics(ref chrp);
			}
			Dictionary<Tuple<string, bool, bool, string>, Tuple<bool, IRenderEngine>> wsFontEngines;
			if (!m_fontEngines.TryGetValue(ws, out wsFontEngines))
			{
				wsFontEngines = new Dictionary<Tuple<string, bool, bool, string>, Tuple<bool, IRenderEngine>>();
				m_fontEngines[ws] = wsFontEngines;
			}
			string fontFeatures = GetFontFeatures(fontName, chrp, ws);
			if (chrp.szFontVar != null)
			{
				MarshalEx.StringToUShort(fontFeatures ?? string.Empty, chrp.szFontVar);
				vg.SetupGraphics(ref chrp);
			}
			var key = Tuple.Create(fontName, chrp.ttvBold == (int)FwTextToggleVal.kttvForceOn,
				chrp.ttvItalic == (int)FwTextToggleVal.kttvForceOn, fontFeatures);
			Tuple<bool, IRenderEngine> fontEngine;
			if (!wsFontEngines.TryGetValue(key, out fontEngine))
			{
				// We don't have a font engine stored for this combination of font face with bold and italic
				// so we will create the engine for it here
				wsFontEngines[key] = GetRenderingEngine(fontName, fontFeatures, vg, ws);
			}
			else if (fontEngine.Item1 == ws.IsGraphiteEnabled)
			{
				// We did have a font engine for this key and IsGraphiteEnabled hasn't changed so use it.
				return fontEngine.Item2;
			}
			else
			{
				// We had a font engine for this key, but IsGraphiteEnabled has changed in the ws.
				// Destroy all the engines associated with this ws and create one for this key.
				ReleaseRenderEngines(wsFontEngines.Values);
				wsFontEngines.Clear();
				var renderingEngine = GetRenderingEngine(fontName, fontFeatures, vg, ws);
				wsFontEngines[key] = renderingEngine;
			}

			return wsFontEngines[key].Item2;
		}

		private static string GetFontFeatures(string fontName, LgCharRenderProps chrp, ILgWritingSystem ws)
		{
			if (fontName == ws.DefaultFontName)
				return FontFeatureSettings.Normalize(ws.DefaultFontFeatures);
			return chrp.szFontVar == null ? string.Empty : FontFeatureSettings.Normalize(MarshalEx.UShortToString(chrp.szFontVar));
		}

		private Tuple<bool, IRenderEngine> GetRenderingEngine(string fontName, string fontFeatures, IVwGraphics vg, ILgWritingSystem ws)
		{
			// NB: Even if the ws claims graphite is enabled, this might not be a graphite font
			if (ws.IsGraphiteEnabled)
			{
				var graphiteEngine = GraphiteEngineClass.Create();

				graphiteEngine.InitRenderer(vg, GraphiteFontFeatures.ConvertFontFeatureCodesToIds(fontFeatures));
				// check if the font is a valid Graphite font
				if (graphiteEngine.FontIsValid)
				{
					graphiteEngine.RenderEngineFactory = this;
					graphiteEngine.WritingSystemFactory = ws.WritingSystemFactory;
					return new Tuple<bool, IRenderEngine>(ws.IsGraphiteEnabled, graphiteEngine);
				}
				// It wasn't really a graphite font - release the graphite one and create a Uniscribe below
				Marshal.ReleaseComObject(graphiteEngine);
			}
			return new Tuple<bool, IRenderEngine>(ws.IsGraphiteEnabled, GetUniscribeEngine(vg, ws, fontFeatures));
		}

		private IRenderEngine GetUniscribeEngine(IVwGraphics vg, ILgWritingSystem ws, string fontFeatures)
		{
			IRenderEngine uniscribeEngine;
			uniscribeEngine = UniscribeEngineClass.Create();
			uniscribeEngine.InitRenderer(vg, fontFeatures);
			uniscribeEngine.RenderEngineFactory = this;
			uniscribeEngine.WritingSystemFactory = ws.WritingSystemFactory;

			return uniscribeEngine;
		}

		/// <summary>
		/// Clears the renderers.
		/// </summary>
		public void ClearRenderEngines()
		{
			foreach (Dictionary<Tuple<string, bool, bool, string>, Tuple<bool, IRenderEngine>> wsGraphiteEngines in m_fontEngines.Values)
				ReleaseRenderEngines(wsGraphiteEngines.Values);
			m_fontEngines.Clear();
		}

		/// <summary>
		/// Clears the renderers associated with the specified writing system factory.
		/// </summary>
		public void ClearRenderEngines(ILgWritingSystemFactory wsf)
		{
			foreach (KeyValuePair<ILgWritingSystem, Dictionary<Tuple<string, bool, bool, string>, Tuple<bool, IRenderEngine>>> kvp in m_fontEngines
				.Where(kvp => kvp.Key.WritingSystemFactory == wsf).ToArray())
			{
				ReleaseRenderEngines(kvp.Value.Values);
				m_fontEngines.Remove(kvp.Key);
			}
		}

		private void ReleaseRenderEngines(IEnumerable<Tuple<bool, IRenderEngine>> renderEngines)
		{
			foreach (var renderEngine in renderEngines)
				Marshal.ReleaseComObject(renderEngine.Item2);
		}

		/// <inheritdoc/>
		protected override void DisposeManagedResources()
		{
			ClearRenderEngines();
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
			base.Dispose(disposing);
		}
	}
}
