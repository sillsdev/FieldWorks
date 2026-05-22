// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Utils;
using SIL.WritingSystems;

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
					var chrp = new LgCharRenderProps
					{
						ws = ws.Handle,
						szFaceName = new ushort[32],
					};
					MarshalEx.StringToUShort("Arial", chrp.szFaceName);
					gm.VwGraphics.SetupGraphics(ref chrp);
					IRenderEngine engine = reFactory.get_Renderer(ws, gm.VwGraphics);
					Assert.That(engine, Is.Not.Null);
					Assert.That(engine.WritingSystemFactory, Is.SameAs(wsManager));
					Assert.That(engine, Is.InstanceOf(typeof(UniscribeEngine)));
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
					var chrp = new LgCharRenderProps
					{
						ws = ws.Handle,
						szFaceName = new ushort[32],
					};
					MarshalEx.StringToUShort("Charis SIL", chrp.szFaceName);
					gm.VwGraphics.SetupGraphics(ref chrp);
					IRenderEngine engine = reFactory.get_Renderer(ws, gm.VwGraphics);
					Assert.That(engine, Is.Not.Null);
					Assert.That(engine.WritingSystemFactory, Is.SameAs(wsManager));
					Assert.That(engine, Is.InstanceOf(typeof(UniscribeEngine)));

					ws.IsGraphiteEnabled = true;
					gm.VwGraphics.SetupGraphics(ref chrp);
					engine = reFactory.get_Renderer(ws, gm.VwGraphics);
					Assert.That(engine, Is.Not.Null);
					Assert.That(engine.WritingSystemFactory, Is.SameAs(wsManager));
					Assert.That(engine, Is.InstanceOf(typeof(GraphiteEngine)));
					wsManager.Save();
				}
				finally
				{
					gm.Uninit();
				}
			}
		}

		[Test]
		public void get_Renderer_DefaultFontFeatures_CopiesNormalizedFeaturesToGraphics()
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
					ws.DefaultFont = new FontDefinition("Arial") { Features = " smcp = 1, kern=0 " };

					var chrp = CreateCharRenderProps(ws.Handle, "<default font>", string.Empty);
					gm.VwGraphics.SetupGraphics(ref chrp);

					IRenderEngine engine = reFactory.get_Renderer(ws, gm.VwGraphics);
					var graphicsChrp = gm.VwGraphics.FontCharProperties;

					Assert.That(engine, Is.InstanceOf(typeof(UniscribeEngine)));
					Assert.That(
						MarshalEx.UShortToString(graphicsChrp.szFaceName),
						Is.EqualTo("Arial"));
					Assert.That(
						MarshalEx.UShortToString(graphicsChrp.szFontVar),
						Is.EqualTo("kern=0,smcp=1"));
					wsManager.Save();
				}
				finally
				{
					gm.Uninit();
				}
			}
		}

		[Test]
		public void get_Renderer_DefaultFontWithStyleFeatures_PreservesStyleFeatures()
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
					ws.DefaultFont = new FontDefinition("Arial") { Features = string.Empty };

					var chrp = CreateCharRenderProps(ws.Handle, "<default font>", " smcp = 1, kern=0 ");
					gm.VwGraphics.SetupGraphics(ref chrp);

					IRenderEngine engine = reFactory.get_Renderer(ws, gm.VwGraphics);
					var graphicsChrp = gm.VwGraphics.FontCharProperties;

					Assert.That(engine, Is.InstanceOf(typeof(UniscribeEngine)));
					Assert.That(
						MarshalEx.UShortToString(graphicsChrp.szFaceName),
						Is.EqualTo("Arial"));
					Assert.That(
						MarshalEx.UShortToString(graphicsChrp.szFontVar),
						Is.EqualTo("kern=0,smcp=1"));
					wsManager.Save();
				}
				finally
				{
					gm.Uninit();
				}
			}
		}

		[Test]
		public void get_Renderer_OpenTypeFeatures_ArePartOfCacheIdentity()
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

					var firstChrp = CreateCharRenderProps(
						ws.Handle,
						"Arial",
						" smcp = 1, kern=0 ");
					gm.VwGraphics.SetupGraphics(ref firstChrp);
					IRenderEngine first = reFactory.get_Renderer(ws, gm.VwGraphics);

					var equivalentChrp = CreateCharRenderProps(
						ws.Handle,
						"Arial",
						"kern=0,smcp=1");
					gm.VwGraphics.SetupGraphics(ref equivalentChrp);
					IRenderEngine equivalent = reFactory.get_Renderer(ws, gm.VwGraphics);

					var differentChrp = CreateCharRenderProps(
						ws.Handle,
						"Arial",
						"smcp=0,kern=0");
					gm.VwGraphics.SetupGraphics(ref differentChrp);
					IRenderEngine different = reFactory.get_Renderer(ws, gm.VwGraphics);

					Assert.That(equivalent, Is.SameAs(first));
					Assert.That(different, Is.Not.SameAs(first));
					Assert.That(
						MarshalEx.UShortToString(gm.VwGraphics.FontCharProperties.szFontVar),
						Is.EqualTo("kern=0,smcp=0"));
					wsManager.Save();
				}
				finally
				{
					gm.Uninit();
				}
			}
		}

		private static LgCharRenderProps CreateCharRenderProps(
			int ws,
			string fontName,
			string fontFeatures)
		{
			var chrp = new LgCharRenderProps
			{
				ws = ws,
				szFaceName = new ushort[32],
				szFontVar = new ushort[128],
			};
			MarshalEx.StringToUShort(fontName, chrp.szFaceName);
			MarshalEx.StringToUShort(fontFeatures, chrp.szFontVar);
			return chrp;
		}
	}
}
