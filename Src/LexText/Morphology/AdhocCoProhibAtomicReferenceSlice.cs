using System;
using System.Diagnostics;


using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.Utils;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Summary description for AdhocCoProhibAtomicReferenceSlice.
	/// </summary>
	public class AdhocCoProhibAtomicReferenceSlice : AtomicReferenceSlice
	{
		int m_dxLastWidth = 0; // remember last width passed to OnSizeChanged.
		/// <summary>
		/// Default Constructor.
		/// </summary>
		public AdhocCoProhibAtomicReferenceSlice() : base()
		{
		}

		/// <summary>
		/// Override method to add suitable control.
		/// </summary>
		public override void FinishInit()
		{
			CheckDisposed();
			Debug.Assert(m_cache != null);
			Debug.Assert(m_configurationNode != null);

			base.FinishInit();
			m_fieldName = XmlUtils.GetManditoryAttributeValue(m_configurationNode, "field");
			string className = m_cache.MetaDataCacheAccessor.GetClassName((uint)m_obj.ClassID);

			m_flid = AutoDataTreeMenuHandler.ContextMenuHelper.GetFlid(m_cache.MetaDataCacheAccessor,
				className, m_fieldName);
			AdhocCoProhibAtomicLauncher launcher = new AdhocCoProhibAtomicLauncher();
			launcher.Initialize(m_cache, m_obj, m_flid, m_fieldName,
				null,
				Mediator,
				DisplayNameProperty,
				BestWsName);
			// We don't want to be visible until later, since otherwise we get a temporary
			// display in the wrong place with the wrong size that serves only to annoy the
			// user.  See LT-1518 "The drawing of the DataTree for Lexicon/Advanced Edit draws
			// some initial invalid controls."  Becoming visible when we set the width and
			// height seems to delay things enough to avoid this visual clutter.
			launcher.Visible = false;
			Control = launcher;

			//We need to set the Font so the height of this slice will be
			//set appropriately to fit the text.
			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromMediator(Mediator);
			int fontHeight = FontHeightAdjuster.GetFontHeightForStyle(
					"Normal", stylesheet,
					m_cache.LangProject.DefaultVernacularWritingSystem,
					m_cache.LanguageWritingSystemFactoryAccessor);
			this.Font = new System.Drawing.Font(m_cache.LangProject.DefaultVernacularWritingSystemFont, fontHeight / 1000);
		}

		/// <summary>
		/// Override method to make the right control.
		/// </summary>
		/// <param name="persistenceProvider"></param>
		protected override void SetupControls(IPersistenceProvider persistenceProvider, Mediator mediator, StringTable stringTbl)
		{
			Debug.Assert(false, "This should never be called.");
		}

		/// <summary>
		/// This code exactly matches code in ReferenceVectorSlice; could we refactor somehow?
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			if (this.Width == m_dxLastWidth)
				return;

			m_dxLastWidth = Width; // BEFORE doing anything, actions below may trigger recursive call.
			ReferenceLauncher rl = (ReferenceLauncher)this.Control;
			SimpleRootSite view = (SimpleRootSite)rl.MainControl;
			view.PerformLayout();
			int h1 = view.RootBox.Height;
			int hNew = Math.Max(h1, ContainingDataTree.GetMinFieldHeight());
			//When the slice is first created view.RootBox.Height is 0 and
			//GetMinFieldHeight returns a constant of 18 so this is too short
			//especially with Arabic scripts  (see LT-7327)
			hNew = Math.Max(hNew, Font.Height) + 3;
			if (hNew != this.Height)
			{
				this.Height = hNew;
			}
		}

		public override void ShowSubControls()
		{
			CheckDisposed();
			base.ShowSubControls ();
			this.Control.Visible = true;
		}
	}
}
