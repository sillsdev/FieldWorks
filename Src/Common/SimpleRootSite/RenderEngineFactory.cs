using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Common.FwKernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// This class is used by root boxes to get render engines.
	/// </summary>
	public class RenderEngineFactory : FwDisposableBase, IRenderEngineFactory
	{
		private readonly Dictionary<ILgWritingSystem, Dictionary<Tuple<string, bool, bool>, GraphiteEngine>> m_graphiteEngines;
		private readonly Dictionary<ILgWritingSystemFactory, IRenderEngine> m_nonGraphiteEngines;

		/// <summary>
		/// Initializes a new instance of the <see cref="RenderEngineFactory"/> class.
		/// </summary>
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
			LgCharRenderProps chrp = vg.FontCharProperties;
			string fontName = MarshalEx.UShortToString(chrp.szFaceName);
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

				Tuple<string, bool, bool> key = Tuple.Create(fontName, chrp.ttvBold == (int) FwTextToggleVal.kttvForceOn,
					chrp.ttvItalic == (int) FwTextToggleVal.kttvForceOn);
				GraphiteEngine graphiteEngine;
				if (!wsGraphiteEngines.TryGetValue(key, out graphiteEngine))
				{
					graphiteEngine = GraphiteEngineClass.Create();

					string fontFeatures = null;
					if (fontName == ws.DefaultFontName)
						fontFeatures = ws.DefaultFontFeatures;
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
					return graphiteEngine;
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
				if (!MiscUtils.IsUnix)
				{
					nonGraphiteEngine = UniscribeEngineClass.Create();
				}
				else
				{
					// default to the UniscribeEngine unless ROMAN environment variable is set.
					if (Environment.GetEnvironmentVariable("ROMAN") == null)
						nonGraphiteEngine = UniscribeEngineClass.Create();
					else
						nonGraphiteEngine = RomRenderEngineClass.Create();
				}
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
			foreach (Dictionary<Tuple<string, bool, bool>, GraphiteEngine> wsGraphiteEngines in m_graphiteEngines.Values)
				ReleaseRenderEngines(wsGraphiteEngines.Values);
			m_graphiteEngines.Clear();

			ReleaseRenderEngines(m_nonGraphiteEngines.Values);
			m_nonGraphiteEngines.Clear();
		}

		/// <summary>
		/// Clears the renderers associated with the specified writing system factory.
		/// </summary>
		public void ClearRenderEngines(ILgWritingSystemFactory wsf)
		{
			foreach (KeyValuePair<ILgWritingSystem, Dictionary<Tuple<string, bool, bool>, GraphiteEngine>> kvp in m_graphiteEngines
				.Where(kvp => kvp.Key.WritingSystemFactory == wsf).ToArray())
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
			foreach (IRenderEngine renderEngine in renderEngines)
				Marshal.ReleaseComObject(renderEngine);
		}

		/// <summary>
		/// Disposes the managed resources.
		/// </summary>
		protected override void DisposeManagedResources()
		{
			ClearRenderEngines();
		}
	}
}
