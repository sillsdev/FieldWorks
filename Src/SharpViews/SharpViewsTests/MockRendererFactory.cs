// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	class MockRendererFactory : IRendererFactory
	{
		public MockRendererFactory()
		{
			Renderer = new MockRenderEngine();
		}

		Dictionary<int, IRenderEngine> m_renderers = new Dictionary<int, IRenderEngine>();
		internal void SetRenderer(int ws, IRenderEngine renderer)
		{
			m_renderers[ws] = renderer;
		}
		internal MockRenderEngine Renderer { get; private set; }
		public IRenderEngine GetRenderer(int ws, IVwGraphics vg)
		{
			IRenderEngine result;
			if (m_renderers.TryGetValue(ws, out result))
				return result;
			return Renderer;
		}

		public int UserWs
		{
			get { return 1; }
		}

		public bool RightToLeft(int ws)
		{
			return false;
		}
	}
}
