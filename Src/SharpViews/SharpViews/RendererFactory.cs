using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.SharpViews
{
	/// <summary>
	/// The default implementation of IRendererFactory wraps a writing system factory.
	/// </summary>
	class RendererFactory : IRendererFactory
	{
		private ILgWritingSystemFactory m_wsf;

		public RendererFactory(ILgWritingSystemFactory wsf)
		{
			m_wsf = wsf;
		}

		/// <summary>
		/// Get a suitable rendering engine for the specified writing system when drawing in the specified graphics environment.
		/// </summary>
		public IRenderEngine GetRenderer(int ws, IVwGraphics vg)
		{
			if (ws == 0)
				return m_wsf.get_Renderer(m_wsf.UserWs, vg);
			return m_wsf.get_Renderer(ws, vg);
		}

		public int UserWs
		{
			get { return m_wsf.UserWs; }
		}

		/// <summary>
		/// Return true if the main direction of the script is right-to-left.
		/// </summary>
		public bool RightToLeft(int ws)
		{
			var wsEngine = m_wsf.get_EngineOrNull(ws);
			if (wsEngine == null)
				return false;
			return wsEngine.RightToLeftScript;
		}
	}
}
