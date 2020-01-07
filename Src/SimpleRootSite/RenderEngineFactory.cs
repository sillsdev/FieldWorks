// Copyright (c) 2016-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;
using SIL.ObjectModel;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// This class is used by root boxes to get render engines.
	/// </summary>
	public class RenderEngineFactory : DisposableBase, IRenderEngineFactory
	{
		private readonly Dictionary<ILgWritingSystem, Dictionary<Tuple<string, bool, bool>, GraphiteEngine>> m_graphiteEngines;
		private readonly Dictionary<ILgWritingSystemFactory, IRenderEngine> m_nonGraphiteEngines;

		/// <summary />
		public RenderEngineFactory()
		{
			m_graphiteEngines = new Dictionary<ILgWritingSystem, Dictionary<Tuple<string, bool, bool>, GraphiteEngine>>();
			m_nonGraphiteEngines = new Dictionary<ILgWritingSystemFactory, IRenderEngine>();
		}

		/// <summary>
		/// Get the engine used to render text with the specified properties. At present only
		/// font, bold, and italic properties are significant.
		/// Font name may be '&lt;default font&gt;' which produces a renderer suitable for the default
		/// font.
		/// </summary>
		public IRenderEngine get_Renderer(ILgWritingSystem ws, IVwGraphics vg)
		{
			var chrp = vg.FontCharProperties;
			var fontName = MarshalEx.UShortToString(chrp.szFaceName);
			if (fontName == "<default font>")
			{
				fontName = ws.DefaultFontName;
				MarshalEx.StringToUShort(fontName, chrp.szFaceName);
				vg.SetupGraphics(ref chrp);
			}

			if (ws.IsGraphiteEnabled)
			{
				Dictionary<Tuple<string, bool, bool>, GraphiteEngine> wsGraphiteEngines;
				if (!m_graphiteEngines.TryGetValue(ws, out wsGraphiteEngines))
				{
					wsGraphiteEngines = new Dictionary<Tuple<string, bool, bool>, GraphiteEngine>();
					m_graphiteEngines[ws] = wsGraphiteEngines;
				}
				var key = Tuple.Create(fontName, chrp.ttvBold == (int)FwTextToggleVal.kttvForceOn, chrp.ttvItalic == (int)FwTextToggleVal.kttvForceOn);
				GraphiteEngine graphiteEngine;
				if (!wsGraphiteEngines.TryGetValue(key, out graphiteEngine))
				{
					graphiteEngine = GraphiteEngineClass.Create();
					var fontFeatures = fontName == ws.DefaultFontName ? ws.DefaultFontFeatures : null;
					graphiteEngine.InitRenderer(vg, fontFeatures);
					// check if the font is a valid Graphite font
					if (graphiteEngine.FontIsValid)
					{
						graphiteEngine.RenderEngineFactory = this;
						graphiteEngine.WritingSystemFactory = ws.WritingSystemFactory;
						wsGraphiteEngines[key] = graphiteEngine;
					}
					else
					{
						Marshal.ReleaseComObject(graphiteEngine);
						graphiteEngine = null;
					}
				}
				if (graphiteEngine != null)
				{
					return graphiteEngine;
				}
			}
			else
			{
				Dictionary<Tuple<string, bool, bool>, GraphiteEngine> wsGraphiteEngines;
				if (m_graphiteEngines.TryGetValue(ws, out wsGraphiteEngines))
				{
					ReleaseRenderEngines(wsGraphiteEngines.Values);
					m_graphiteEngines.Remove(ws);
				}
			}
			IRenderEngine nonGraphiteEngine;
			if (!m_nonGraphiteEngines.TryGetValue(ws.WritingSystemFactory, out nonGraphiteEngine))
			{
				nonGraphiteEngine = UniscribeEngineClass.Create();
				nonGraphiteEngine.InitRenderer(vg, null);
				nonGraphiteEngine.RenderEngineFactory = this;
				nonGraphiteEngine.WritingSystemFactory = ws.WritingSystemFactory;
				m_nonGraphiteEngines[ws.WritingSystemFactory] = nonGraphiteEngine;
			}

			return nonGraphiteEngine;
		}

		/// <summary>
		/// Clears the renderers.
		/// </summary>
		public void ClearRenderEngines()
		{
			foreach (var wsGraphiteEngines in m_graphiteEngines.Values)
			{
				ReleaseRenderEngines(wsGraphiteEngines.Values);
			}
			m_graphiteEngines.Clear();

			ReleaseRenderEngines(m_nonGraphiteEngines.Values);
			m_nonGraphiteEngines.Clear();
		}

		/// <summary>
		/// Clears the renderers associated with the specified writing system factory.
		/// </summary>
		public void ClearRenderEngines(ILgWritingSystemFactory wsf)
		{
			foreach (var kvp in m_graphiteEngines.Where(kvp => kvp.Key.WritingSystemFactory == wsf).ToArray())
			{
				ReleaseRenderEngines(kvp.Value.Values);
				m_graphiteEngines.Remove(kvp.Key);
			}
			IRenderEngine nonGraphiteRenderEngine;
			if (m_nonGraphiteEngines.TryGetValue(wsf, out nonGraphiteRenderEngine))
			{
				Marshal.ReleaseComObject(nonGraphiteRenderEngine);
				m_nonGraphiteEngines.Remove(wsf);
			}
		}

		private void ReleaseRenderEngines(IEnumerable<IRenderEngine> renderEngines)
		{
			foreach (var renderEngine in renderEngines)
			{
				Marshal.ReleaseComObject(renderEngine);
			}
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