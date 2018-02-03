// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Infrastructure;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.Common.FwUtils;
using Rect = SIL.FieldWorks.Common.ViewsInterfaces.Rect;

namespace LanguageExplorer.Areas.TextsAndWords
{
	internal sealed class TryAWordRootSite : RootSiteControl
	{
		private InterlinVc m_vc;
		private ITsString m_sWordForm;
		private IWfiWordform m_wordform;
		private const int m_kfragSingleInterlinearAnalysisWithLabels = 100013;
		private TryAWordSandbox m_tryAWordSandbox;
		private List<int> m_msaHvoList = new List<int>();
		private bool m_fRootMade;
		private int m_labelWidth;

		public TryAWordRootSite()
		{
		}

		#region Overrides of SimpleRootSite

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			m_cache = PropertyTable.GetValue<LcmCache>("cache");
			VisibleChanged += OnVisibleChanged;
			m_styleSheet = PropertyTable.GetValue<LcmStyleSheet>("FlexStyleSheet");
			var wsObj = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			RightToLeft = wsObj.RightToLeftScript ? RightToLeft.Yes : RightToLeft.No;
			AutoScroll = true;
		}

		#endregion

		#region Dispose

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			base.Dispose(disposing);

			if (disposing)
			{
				if (m_tryAWordSandbox != null)
				{
					if (!Controls.Contains(m_tryAWordSandbox))
					{
						m_tryAWordSandbox.Dispose();
					}
				}

				m_vc?.Dispose();
			}
			m_tryAWordSandbox = null;
			m_vc = null;
			m_wordform = null;
			m_msaHvoList = null;
			m_sWordForm = null;
		}

		#endregion Dispose

		private void OnVisibleChanged(object sender, EventArgs e)
		{
			if (!Visible || !m_fRootMade)
			{
				return;
			}
			DisposeTryAWordSandbox();
			CreateTryAWordSandbox();
			m_fRootMade = false;
		}

		#region Overrides of RootSite
		/// <summary>
		/// Make the root box.
		/// </summary>
		public override void MakeRoot()
		{
			CheckDisposed();

			if (m_cache == null || DesignMode)
			{
				return;
			}

			base.MakeRoot();

			// Theory has it that the slices that have 'true' in this attribute will allow the sandbox to be used.
			// We'll see how the theory goes, when I get to the point of wanting to see the sandbox.
			m_vc = new InterlinVc(m_cache)
			{
				ShowMorphBundles = true,
				ShowDefaultSense = true,
				LineChoices = new EditableInterlinLineChoices(m_cache.LanguageProject, WritingSystemServices.kwsFirstVern, m_cache.DefaultAnalWs)
			};

			m_vc.LineChoices.Add(InterlinLineChoices.kflidWord); // 1
			m_vc.LineChoices.Add(InterlinLineChoices.kflidMorphemes); // 2
			m_vc.LineChoices.Add(InterlinLineChoices.kflidLexEntries); //3
			m_vc.LineChoices.Add(InterlinLineChoices.kflidLexGloss); //4
			m_vc.LineChoices.Add(InterlinLineChoices.kflidLexPos); //5

			m_rootb.DataAccess = m_cache.MainCacheAccessor;
			FixWs(); // AFTER setting DA!

			if (m_wordform != null)
			{
				m_rootb.SetRootObject(m_wordform.Hvo, m_vc, m_kfragSingleInterlinearAnalysisWithLabels, m_styleSheet);
			}

			SetSandboxSize(); // in case we already have a current annotation.
			SetBackgroundColor();
			m_fRootMade = true;
		}

		#endregion Overrides of RootSite
		internal ITsString WordForm
		{
			set
			{
				m_sWordForm = value;
				DisposeTryAWordSandbox();
				CreateTryAWordSandbox();
			}
		}

		private void DisposeTryAWordSandbox()
		{
			CheckDisposed();

			if (m_tryAWordSandbox == null)
			{
				return;
			}
			Controls.Remove(m_tryAWordSandbox);
			m_tryAWordSandbox.Dispose();
			m_tryAWordSandbox = null;
		}

		private void FixWs()
		{
			if (m_wordform == null || m_vc == null || m_rootb == null)
			{
				return;
			}

			int wsPreferred;
			if (m_wordform.Form.TryWs(WritingSystemServices.kwsFirstVern, out wsPreferred))
			{
				m_vc.PreferredVernWs = wsPreferred;
			}
		}
		// Set the size of the sandbox on the VC...if it exists yet.
		private void SetSandboxSize()
		{
			SetSandboxSizeForVc();
			// This should make it big enough not to scroll.
			if (m_tryAWordSandbox?.RootBox != null)
			{
				m_tryAWordSandbox.Size = new Size(m_tryAWordSandbox.RootBox.Width + 1, m_tryAWordSandbox.RootBox.Height + 1);
			}
		}

		// Set the VC size to match the sandbox. Return true if it changed.
		private bool SetSandboxSizeForVc()
		{
			if (m_vc == null || m_tryAWordSandbox == null)
			{
				return false;
			}
			if (!Controls.Contains(m_tryAWordSandbox))
			{
				Controls.Add(m_tryAWordSandbox); // Makes it real and gives it a root box.
			}
			if (m_tryAWordSandbox.RootBox == null)
			{
				m_tryAWordSandbox.MakeRoot();	// adding sandbox to Controls doesn't make rootbox.
			}
			m_tryAWordSandbox.PerformLayout();
			int dpiX, dpiY;
			using (var g = CreateGraphics())
			{
				dpiX = (int)g.DpiX;
				dpiY = (int)g.DpiY;
			}
			var width = m_tryAWordSandbox.RootBox.Width;
			if (width > 10000)
			{
				width = 500; // arbitrary, may allow something to work more or less
			}
#if RANDYTODO
			// TODO: Creating a new Size instance and not using it appears to not help much.
			// Removing it then lets a fair bit of the above code to be removed as also not used/doing anything.
#endif
			var newSize = new Size(width * 72000 / dpiX, m_tryAWordSandbox.RootBox.Height * 72000 / dpiY);
			return true;
		}
		private void SetSandboxLocation()
		{
			m_rootb.Reconstruct();
			using (new HoldGraphics(this))
			{
				Rectangle rcSrcRoot;
				Rectangle rcDstRoot;
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				var rgvsli = new SelLevInfo[1];
				rgvsli[0].ihvo = 0;
				rgvsli[0].tag = m_cache.MetaDataCacheAccessor.GetFieldId2(CmObjectTags.kClassId, "Self", false);
				var sel = RootBox.MakeTextSelInObj(0, rgvsli.Length, rgvsli, 0, null, true, false, false, false, false);
				if (sel == null)
				{
					Debug.WriteLine("Could not make selection in SetSandboxLocation");
					m_tryAWordSandbox.Left = 150;
					return; // can't position it accurately.
				}
				Rect rcPrimary, rcSec;
				bool fSplit, fEndBeforeAnchor;
				sel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary, out rcSec,
					out fSplit, out fEndBeforeAnchor);
				if (m_vc.RightToLeft)
				{
					m_tryAWordSandbox.Left = rcPrimary.right - m_tryAWordSandbox.Width;
					m_labelWidth = m_rootb.Width - rcPrimary.right;
				}
				else
				{
					m_tryAWordSandbox.Left = rcPrimary.left;
					m_labelWidth = rcPrimary.left;
				}
			}
		}


		private void CreateTryAWordSandbox()
		{
			// skip if it's before we've set the word
			if (m_sWordForm == null)
			{
				return;
			}
			// skip if it's before we've set up the rootsite control
			if (m_rootb == null)
			{
				return;
			}
			// skip if we're not visible
			if (!Visible)
			{
				return;
			}
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				m_wordform = WfiWordformServices.FindOrCreateWordform(m_cache, m_sWordForm);
			});

			var analysis = m_vc.GetGuessForWordform(m_wordform, m_cache.DefaultVernWs);
			if (analysis is NullWAG)
			{
				analysis = m_wordform;
			}

			m_rootb.SetRootObject(analysis.Hvo, m_vc, m_kfragSingleInterlinearAnalysisWithLabels, m_styleSheet);
			m_tryAWordSandbox = new TryAWordSandbox(m_cache, StyleSheet, m_vc.LineChoices, analysis)
			{
				Visible = false
			};
			m_tryAWordSandbox.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
			Controls.Add(m_tryAWordSandbox);
			SetSandboxSize();
			SetSandboxLocation();
			m_tryAWordSandbox.Visible = true;
			SetBackgroundColor();
			var height = Math.Max(ScrollRange.Height, m_tryAWordSandbox.Height) + SystemInformation.HorizontalScrollBarHeight;
			if (Height != height)
			{
				Height = height;
			}
			m_tryAWordSandbox.SizeChanged += m_tryAWordSandbox_SizeChanged;
		}

		public override int GetAvailWidth(IVwRootBox prootb)
		{
			return m_tryAWordSandbox == null ? base.GetAvailWidth(prootb) : Math.Max(base.GetAvailWidth(prootb), m_tryAWordSandbox.Width + m_labelWidth);
		}

		void m_tryAWordSandbox_SizeChanged(object sender, EventArgs e)
		{
			if (m_vc.RightToLeft)
			{
				SetSandboxLocation();
			}
			var height = Math.Max(ScrollRange.Height, m_tryAWordSandbox.Height) + SystemInformation.HorizontalScrollBarHeight;
			if (Height != height)
			{
				Height = height;
			}
		}

		protected override bool WantHScroll => true;

		private void SetBackgroundColor()
		{
			BackColor = SystemColors.Control;
			if (m_tryAWordSandbox != null)
			{
				m_tryAWordSandbox.BackColor = SystemColors.Control;
			}
			Parent.BackColor = SystemColors.Control;
		}

		private void GetMSAsFromSandbox()
		{
			m_msaHvoList = m_tryAWordSandbox.MsaHvoList;
		}

		/// <summary>
		/// Get list of Msas defined in sandbox
		/// </summary>
		public List<int> MsaList
		{
			get
			{
				if (m_tryAWordSandbox != null)
				{
					GetMSAsFromSandbox();
				}
				return m_msaHvoList;
			}
		}
	}
}