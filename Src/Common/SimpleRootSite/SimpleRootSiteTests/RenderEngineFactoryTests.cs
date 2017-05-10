// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using NUnit.Framework;
using SIL.CoreImpl.WritingSystems;
using SIL.FieldWorks.Common.FwKernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	/// <summary>
	///
	/// </summary>
	[TestFixture]
	public class RenderEngineFactoryTests
	{
		/// <summary>
		/// Tests the get_RendererFromChrp method with a normal font.
		/// </summary>
		[Test]
		public void get_Renderer_Uniscribe()
		{
			using (var control = new Form())
			using (var gm = new GraphicsManager(control))
			using (var reFactory = new RenderEngineFactory())
			{
				gm.Init(1.0f);
				try
				{
					var wsManager = new WritingSystemManager();
					CoreWritingSystemDefinition ws = wsManager.Set("en-US");
					var chrp = new LgCharRenderProps { ws = ws.Handle, szFaceName = new ushort[32] };
					MarshalEx.StringToUShort("Arial", chrp.szFaceName);
					gm.VwGraphics.SetupGraphics(ref chrp);
					IRenderEngine engine = reFactory.get_Renderer(ws, gm.VwGraphics);
					Assert.IsNotNull(engine);
					Assert.AreSame(wsManager, engine.WritingSystemFactory);
					Assert.IsInstanceOf(typeof(UniscribeEngine), engine);
					wsManager.Save();
				}
				finally
				{
					gm.Uninit();
				}
			}
		}

		/// <summary>
		/// Tests the get_RendererFromChrp method with a Graphite font.
		/// </summary>
		[Test]
		public void get_Renderer_Graphite()
		{
			using (var control = new Form())
			using (var gm = new GraphicsManager(control))
			using (var reFactory = new RenderEngineFactory())
			{
				gm.Init(1.0f);
				try
				{
					var wsManager = new WritingSystemManager();
					// by default Graphite is disabled
					CoreWritingSystemDefinition ws = wsManager.Set("en-US");
					var chrp = new LgCharRenderProps { ws = ws.Handle, szFaceName = new ushort[32] };
					MarshalEx.StringToUShort("Charis SIL", chrp.szFaceName);
					gm.VwGraphics.SetupGraphics(ref chrp);
					IRenderEngine engine = reFactory.get_Renderer(ws, gm.VwGraphics);
					Assert.IsNotNull(engine);
					Assert.AreSame(wsManager, engine.WritingSystemFactory);
					Assert.IsInstanceOf(typeof(UniscribeEngine), engine);

					ws.IsGraphiteEnabled = true;
					gm.VwGraphics.SetupGraphics(ref chrp);
					engine = reFactory.get_Renderer(ws, gm.VwGraphics);
					Assert.IsNotNull(engine);
					Assert.AreSame(wsManager, engine.WritingSystemFactory);
					Assert.IsInstanceOf(typeof(GraphiteEngine), engine);
					wsManager.Save();
				}
				finally
				{
					gm.Uninit();
				}
			}
		}
	}
}
