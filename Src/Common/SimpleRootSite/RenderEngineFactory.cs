using System;
using System.Collections.Generic;
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
		private readonly Dictionary<string, Dictionary<Tuple<string, bool, bool>, GraphiteEngine>> m_graphiteEngines;
		private IRenderEngine m_nonGraphiteEngine;

		/// <summary>
		/// Initializes a new instance of the <see cref="RenderEngineFactory"/> class.
		/// </summary>
		public RenderEngineFactory()
		{
			m_graphiteEngines = new Dictionary<string, Dictionary<Tuple<string, bool, bool>, GraphiteEngine>>();
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
				if (!m_graphiteEngines.TryGetValue(ws.Id, out wsGraphiteEngines))
				{
					wsGraphiteEngines = new Dictionary<Tuple<string, bool, bool>, GraphiteEngine>();
					m_graphiteEngines[ws.Id] = wsGraphiteEngines;
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
						graphiteEngine = null;
					}
				}

				if (graphiteEngine != null)
					return graphiteEngine;
			}
			else
			{
				Dictionary<Tuple<string, bool, bool>, GraphiteEngine> wsGraphiteEngines;
				if (m_graphiteEngines.TryGetValue(ws.Id, out wsGraphiteEngines))
				{
					ReleaseGraphiteRenderEngines(wsGraphiteEngines.Values);
					m_graphiteEngines.Remove(ws.Id);
				}
			}

			if (m_nonGraphiteEngine == null)
			{
				if (!MiscUtils.IsUnix)
				{
					m_nonGraphiteEngine = UniscribeEngineClass.Create();
				}
				else
				{
					// default to the UniscribeEngine unless ROMAN environment variable is set.
					if (Environment.GetEnvironmentVariable("ROMAN") == null)
						m_nonGraphiteEngine = UniscribeEngineClass.Create();
					else
						m_nonGraphiteEngine = RomRenderEngineClass.Create();
				}
				m_nonGraphiteEngine.InitRenderer(vg, null);
				m_nonGraphiteEngine.RenderEngineFactory = this;
				m_nonGraphiteEngine.WritingSystemFactory = ws.WritingSystemFactory;
			}

			return m_nonGraphiteEngine;
		}

		/// <summary>
		/// Clears the renderers.
		/// </summary>
		public void ClearRenderEngines()
		{
			foreach (Dictionary<Tuple<string, bool, bool>, GraphiteEngine> wsGraphiteEngines in m_graphiteEngines.Values)
				ReleaseGraphiteRenderEngines(wsGraphiteEngines.Values);
			m_graphiteEngines.Clear();

			if (m_nonGraphiteEngine != null)
			{
				Marshal.ReleaseComObject(m_nonGraphiteEngine);
				m_nonGraphiteEngine = null;
			}
		}

		private void ReleaseGraphiteRenderEngines(IEnumerable<GraphiteEngine> graphiteEngines)
		{
			foreach (GraphiteEngine graphiteEngine in graphiteEngines)
				Marshal.ReleaseComObject(graphiteEngine);
		}

		/// <summary>
		/// Disposes the managed resources.
		/// </summary>
		protected override void DisposeManagedResources()
		{
			base.DisposeManagedResources();

			ClearRenderEngines();
		}
	}
}
