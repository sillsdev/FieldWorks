using System;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Scripture;

namespace SIL.FieldWorks.Common.RootSites.RenderBenchmark
{
	/// <summary>
	/// A benchmark view that uses the production-grade StVc (Standard View Constructor)
	/// instead of the simplified DummyBasicViewVc. This ensures rendering matches
	/// actual FieldWorks document views (margins, styles, writing systems).
	/// </summary>
	public class GenericScriptureView : DummyBasicView
	{
		private readonly int m_rootHvo;
		private readonly int m_rootFlid;

		public int RootFragmentId { get; set; } = 1;

		public GenericScriptureView(int hvoRoot, int flid) : base(hvoRoot, flid)
		{
			m_rootHvo = hvoRoot;
			m_rootFlid = flid;
			m_fMakeRootWhenHandleIsCreated = false;
		}

		public override void MakeRoot()
		{
			CheckDisposed();
			MakeRoot(m_rootHvo, m_rootFlid, RootFragmentId);
		}

		protected override VwBaseVc CreateVc(int flid)
		{
			// We define the VC inline or via helper to handle the Book -> Text bridge
			int defaultWs = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			var vc = new GenericScriptureVc("Normal", defaultWs);
			vc.Cache = m_cache; // Inject cache
			return vc;
		}
	}

	/// <summary>
	/// Extends StVc to handle Scripture hierarchy (Book -> Sections -> StText).
	/// When it reaches StText, it falls back to standard StVc formatting.
	/// </summary>
	public class GenericScriptureVc : StVc
	{
		private const int kFragRoot = 1;
		private const int kFragSection = 21;

		public GenericScriptureVc(string style, int ws) : base(style, ws)
		{
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			// Handle Scripture Hierarchy
			switch (frag)
			{
				case 100: // Matches m_frag in RenderBenchmarkTestsBase
					// Assume Root is Book, iterate sections
					vwenv.AddObjVecItems(ScrBookTags.kflidSections, this, kFragSection);
					break;

				case kFragSection:
					// Section: display heading first, then content body
					vwenv.AddObjProp(ScrSectionTags.kflidHeading, this, (int)StTextFrags.kfrText);
					vwenv.AddObjProp(ScrSectionTags.kflidContent, this, (int)StTextFrags.kfrText);
					break;

				default:
					// Delegate to StVc for standard StText/Para handling
					base.Display(vwenv, hvo, frag);
					break;
			}
		}
	}
}
