using System;
using System.Diagnostics;
using System.Xml;

using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Summary description for MorphTypeAtomicReferenceSlice.
	/// </summary>
	public class MorphTypeAtomicReferenceSlice : AtomicReferenceSlice
	{
		int m_dxLastWidth = 0; // remember last width passed to OnSizeChanged.
		/// <summary>
		/// Default Constructor.
		/// </summary>
		public MorphTypeAtomicReferenceSlice() : base()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="MorphTypeAtomicReferenceSlice"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public MorphTypeAtomicReferenceSlice(FdoCache cache, ICmObject obj, int flid,
			XmlNode configurationNode, IPersistenceProvider persistenceProvider,
			Mediator mediator, StringTable stringTbl)
			: base(cache, obj, flid, configurationNode, persistenceProvider, mediator, stringTbl)
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
			MorphTypeAtomicLauncher launcher = new MorphTypeAtomicLauncher();
			launcher.Initialize(m_cache, m_obj, m_flid, m_fieldName,
				null,
				Mediator,
				DisplayNameProperty,
				BestWsName); // TODO: Get better default 'best ws'.
			launcher.ConfigurationNode = m_configurationNode;
			// We don't want to be visible until later, since otherwise we get a temporary
			// display in the wrong place with the wrong size that serves only to annoy the
			// user.  See LT-1518 "The drawing of the DataTree for Lexicon/Advanced Edit draws
			// some initial invalid controls."  Becoming visible when we set the width and
			// height seems to delay things enough to avoid this visual clutter.
			launcher.Visible = false;
			Control = launcher;
		}

		/// <summary>
		/// Override method to make the right control.  The control is actually made in FinishInit()
		/// for this class.  DON'T CALL THE BASE CLASS METHOD!  (See LT-5345.)
		/// </summary>
		/// <param name="persistenceProvider"></param>
		protected override void SetupControls(IPersistenceProvider persistenceProvider, Mediator mediator, StringTable stringTbl)
		{
		}

		/// <summary>
		/// This code exactly matches code in ReferenceVectorSlice; could we refactor somehow?
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged (e);
			if (this.Width == m_dxLastWidth)
				return;
			m_dxLastWidth = Width; // BEFORE doing anything, actions below may trigger recursive call.
			ReferenceLauncher rl = (ReferenceLauncher)this.Control;
			SimpleRootSite view = (SimpleRootSite)rl.MainControl;
			view.PerformLayout();
			int h1 = view.RootBox.Height;
			int hNew = Math.Max(h1, ContainingDataTree.GetMinFieldHeight()) + 3;
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
