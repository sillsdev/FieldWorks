using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using System.ComponentModel;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;
using SIL.Utils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.IText;
using SIL.FieldWorks.Common.Framework.DetailControls;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// This is the main class for the interlinear text control view of a whole interlinear document
	/// </summary>
	public class AnalysisInterlinearRS : RootSite, INotifyControlInCurrentSlice
	{
		#region Data members

		private InterlinVc m_vc;
		private int m_hvoWfiAnalysis; // Honest-to-goodness, real WfiAnalysis hvo.
		private XmlNode m_configurationNode;
		private OneAnalysisSandbox m_oneAnalSandbox = null;
		private WfiWordform m_wordform;
		private Rect m_rcPrimary;
		private int m_tag;

		#endregion Data members

		#region Properties

		private bool IsEditable
		{
			get
			{
				return XmlUtils.GetBooleanAttributeValue(m_configurationNode.SelectSingleNode("deParams"), "editable");
			}
		}

		#endregion Properties

		#region Construction

		/// <summary>
		/// Make one. Everything interesting happens when it is given a root object, however.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoAnalysis"></param>
		/// <param name="configurationNode"></param>
		/// <param name="stringTable"></param>
		public AnalysisInterlinearRS(FdoCache cache, int hvoAnalysis,
			XmlNode configurationNode, StringTable stringTable) : base(cache)
		{
			m_configurationNode = configurationNode;
			m_hvoWfiAnalysis = hvoAnalysis;
			m_wordform = (WfiWordform)CmObject.CreateFromDBObject(m_fdoCache, m_fdoCache.GetOwnerOfObject(m_hvoWfiAnalysis), false);
			//			RightMouseClickedEvent += new FwRightMouseClickEventHandler(InterlinDocChild_RightMouseClickedEvent);
		}

		#endregion Construction

		#region Dispose

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				//RightMouseClickedEvent -= new FwRightMouseClickEventHandler(InterlinDocChild_RightMouseClickedEvent);
				if (m_oneAnalSandbox != null)
				{
					if (!Controls.Contains(m_oneAnalSandbox))
						m_oneAnalSandbox.Dispose();
				}
				if (m_vc != null)
					m_vc.Dispose();
			}
			m_oneAnalSandbox = null;
			m_vc = null;
			m_configurationNode = null;
			m_wordform = null;
		}

		#endregion Dispose

		#region Overrides of RootSite
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make the root box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			if (m_fdoCache == null || DesignMode)
				return;

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);

			m_vc = new InterlinVc(m_fdoCache);
			// Theory has it that the slices that have 'true' in this attribute will allow the sandbox to be used.
			// We'll see how the theory goes, when I get to the point of wanting to see the sandbox.
			bool isEditable = IsEditable;
			m_vc.ShowMorphBundles = true;
			m_vc.ShowDefaultSense = true;
			// JohnT: kwsVernInParagraph is rather weird here, where we don't have a paragraph, but it allows the
			// VC to deduce the WS of the wordform, not from the paragraph, but from the best vern WS of the wordform itself.
			if (isEditable)
				m_vc.LineChoices = new EditableInterlinLineChoices(LangProject.kwsVernInParagraph, m_fdoCache.DefaultAnalWs);
			else
				m_vc.LineChoices = new InterlinLineChoices(LangProject.kwsVernInParagraph, m_fdoCache.DefaultAnalWs);
			m_vc.LineChoices.Add(InterlinLineChoices.kflidMorphemes); // 1
			m_vc.LineChoices.Add(InterlinLineChoices.kflidLexEntries); //2
			m_vc.LineChoices.Add(InterlinLineChoices.kflidLexGloss); //3
			m_vc.LineChoices.Add(InterlinLineChoices.kflidLexPos); //4

			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;
			FixWs(); // AFTER setting DA!

			int selectorId = 100034; //Convert.ToInt32(XmlUtils.GetManditoryAttributeValue(m_configurationNode.SelectSingleNode("deParams"), "selector"));
			if (m_hvoWfiAnalysis != 0)
				m_rootb.SetRootObject(m_hvoWfiAnalysis, m_vc, selectorId, m_styleSheet);

			base.MakeRoot();

			if (IsEditable)
			{
				m_oneAnalSandbox = new OneAnalysisSandbox(m_fdoCache,
					Mediator,
					StyleSheet,
					m_vc.LineChoices,
					m_hvoWfiAnalysis);
				m_oneAnalSandbox.Visible = false;
				Controls.Add(m_oneAnalSandbox);
				if (m_oneAnalSandbox.RootBox == null)
					m_oneAnalSandbox.MakeRoot();	// adding sandbox to Controls doesn't make rootbox.
				m_tag = MeVirtualHandler.InstallMe(Cache.VwCacheDaAccessor).Tag;
				InitSandbox();
				m_oneAnalSandbox.SizeChanged += (HandleSandboxSizeChanged);
			}
		}

		InterlinearSlice MySlice
		{
			get
			{
				Control parent = Parent;
				while (parent != null)
				{
					if (parent is InterlinearSlice)
						return parent as InterlinearSlice;
					parent = parent.Parent;
				}
				return null;
			}
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			InterlinearSlice slice = MySlice;
			if (slice == null)
				return; // in any case we don't want selections in the interlinear.
			if (slice.ContainingDataTree.CurrentSlice != slice)
				slice.ContainingDataTree.CurrentSlice = slice;
		}

		/// <summary>
		/// Giving it a large maximum width independent of the container causes it to lay out
		/// at full width and scroll horizontally.
		/// </summary>
		/// <param name="prootb"></param>
		/// <returns></returns>
		public override int GetAvailWidth(IVwRootBox prootb)
		{
			return Int32.MaxValue / 2;
		}

		#endregion Overrides of RootSite

		#region INotifyControlInCurrentSlice Members

		/// <summary>
		/// Have the sandbox come and go, as apropriate.
		/// </summary>
		public virtual bool SliceIsCurrent
		{
			set
			{
				CheckDisposed();

				if (value)
				{
					if (IsEditable)
					{
						m_oneAnalSandbox.Visible = true;
						m_oneAnalSandbox.Focus();
					}
				}
				else
				{
					if (IsEditable)
					{
						// JohnT: it's possible the Sandbox is still visible when we Undo the creation of an
						// analysis. At that point it should have no references to MSAs, since anything done to
						// the new analysis has already been undone. But unless we check, the Undo crashes, because
						// the analysis has already been destroyed by the time the slice stops being current.
						if (m_fdoCache.IsValidObject(m_hvoWfiAnalysis))
						{
							// Collect up th3e old MSAs, since they need to go away, if they are unused afterwards.
							List<int> msaHvoList = new List<int>();
							WfiAnalysis anal = (WfiAnalysis)CmObject.CreateFromDBObject(m_fdoCache, m_hvoWfiAnalysis);
							anal.CollectReferencedMsaHvos(msaHvoList);
							m_oneAnalSandbox.UpdateAnalysis(anal);
							MoMorphSynAnalysis.DeleteUnusedMsas(m_fdoCache, msaHvoList);
							//m_fdoCache.LangProject.DefaultUserAgent.SetEvaluation(anal.Hvo, 1, "");
							Debug.Assert(anal.ApprovalStatusIcon == 1, "Analysis must be approved, since it started that way.");
						}

						m_oneAnalSandbox.Visible = false;
						InitSandbox();
					}
				}
			}
		}

		public void HandleSandboxSizeChanged(object sender, EventArgs ea)
		{
			CheckDisposed();

			SetPadding();
		}

		private bool m_fInSizeChanged = false;
		protected override void OnSizeChanged(EventArgs e)
		{
			if (m_fInSizeChanged)
				return;
			m_fInSizeChanged = true;
			try
			{
				base.OnSizeChanged(e);
				SetSandboxLocation();
			}
			finally
			{
				m_fInSizeChanged = false;
			}
		}

		internal Size DesiredSize
		{
			get
			{
				int desiredWidth = m_rootb.Width;
				int desiredHeight = m_rootb.Height;
				if (Controls.Contains(m_oneAnalSandbox))
				{
					desiredWidth = Math.Max(desiredWidth, m_oneAnalSandbox.Left + m_oneAnalSandbox.Width);
					desiredHeight = Math.Max(desiredHeight, m_oneAnalSandbox.Top + m_oneAnalSandbox.Height);
				}
				return new Size(desiredWidth + 5, desiredHeight);
			}
		}

		private void InitSandbox()
		{
			SetSandboxSize();

			m_vc.LeftPadding = 0;
			m_rootb.Reconstruct();
			using (new HoldGraphics(this))
			{
				Rectangle rcSrcRoot;
				Rectangle rcDstRoot;
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				SelLevInfo[] rgvsli = new SelLevInfo[1];
				//rgvsli[1].ihvo = 0; // first morpheme bundle
				//rgvsli[1].tag = (int)WfiAnalysis.WfiAnalysisTags.kflidMorphBundles;
				rgvsli[0].ihvo = 0;
				rgvsli[0].tag = m_tag;
				IVwSelection sel = null;
				sel = RootBox.MakeTextSelInObj(0, rgvsli.Length, rgvsli, 0, null, true, false,false, false, false);
				if (sel == null)
				{
					Debug.WriteLine("Could not make selection in InitSandbox");
					return; // can't position it accurately.
				}
				Rect rcSec;
				bool fSplit, fEndBeforeAnchor;
				sel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out m_rcPrimary, out rcSec,
					out fSplit, out fEndBeforeAnchor);
			}
			SetPadding();
			SetSandboxLocation();
		}

		private void SetSandboxLocation()
		{
			if (m_oneAnalSandbox == null)
				return;

			if (m_vc.RightToLeft)
				m_oneAnalSandbox.Left = 0;
			else
				m_oneAnalSandbox.Left = m_rcPrimary.left;

			// This prevents it from overwriting the labels in the pathological case that all
			// morphemes wrap onto another line.
			m_oneAnalSandbox.Top = m_rcPrimary.top;
		}

		private void SetPadding()
		{
			if (m_oneAnalSandbox == null || !m_vc.RightToLeft)
				return;

			int dpiX;
			using (Graphics g = CreateGraphics())
			{
				dpiX = (int)g.DpiX;
			}
			m_vc.LeftPadding = ((m_oneAnalSandbox.Width - m_rcPrimary.right) * 72000) / dpiX;
			m_rootb.Reconstruct();
		}

		#endregion

		#region Other methods

		private void FixWs()
		{
			if (m_wordform == null)
				return;
			if (m_vc == null)
				return;
			if (m_rootb == null)
				return;

			int wsPreferred;
			if (m_wordform.Form.TryWs(LangProject.kwsFirstVern, out wsPreferred))
			{
				m_vc.PreferredVernWs = wsPreferred;
			}
		}

		// Set the size of the sandbox on the VC...if it exists yet.
		private void SetSandboxSize()
		{
			SetSandboxSizeForVc();
			// This should make it big enough not to scroll.
			if (m_oneAnalSandbox != null && m_oneAnalSandbox.RootBox != null)
				m_oneAnalSandbox.Size = new Size(m_oneAnalSandbox.RootBox.Width + 1, m_oneAnalSandbox.RootBox.Height + 1);
		}

		// Set the VC size to match the sandbox. Return true if it changed.
		private bool SetSandboxSizeForVc()
		{
			if (m_vc == null || m_oneAnalSandbox == null)
				return false;

			m_oneAnalSandbox.PerformLayout();
			int dpiX, dpiY;
			using (Graphics g = CreateGraphics())
			{
				dpiX = (int)g.DpiX;
				dpiY = (int)g.DpiY;
			}
			int width = m_oneAnalSandbox.RootBox.Width;
			if (width > 10000)
			{
				//				Debug.Assert(width < 10000); // Is something taking the full available width of MaxInt/2?
				width = 500; // arbitrary, may allow something to work more or less
			}
			Size newSize = new Size(width * 72000 / dpiX,
				m_oneAnalSandbox.RootBox.Height * 72000 / dpiY);
			if (newSize.Width == m_vc.SandboxSize.Width && newSize.Height == m_vc.SandboxSize.Height)
				return false;

			m_vc.SandboxSize = newSize;
			return true;
		}

		#endregion Other methods
	}
}
