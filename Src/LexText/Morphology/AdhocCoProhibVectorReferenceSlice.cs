using System;
using System.Diagnostics;

using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Utils;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Summary description for AdhocCoProhibVectorReferenceSlice.
	/// </summary>
	public class AdhocCoProhibVectorReferenceSlice : ReferenceVectorSlice
	{
		/// <summary>
		/// Default Constructor.
		/// </summary>
		public AdhocCoProhibVectorReferenceSlice()
		{
		}

		/// <summary>
		/// Override method and assert, since it shouldn't be called, as this class is a custom created slice.
		/// </summary>
		/// <param name="persistenceProvider"></param>
		protected override void SetupControls(IPersistenceProvider persistenceProvider, Mediator mediator, StringTable stringTbl)
		{
			Debug.Assert(false, "This should never be called.");
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
			AdhocCoProhibVectorLauncher launcher = new AdhocCoProhibVectorLauncher();
			string className = m_cache.MetaDataCacheAccessor.GetClassName((uint)m_obj.ClassID);
			m_fieldName = XmlUtils.GetManditoryAttributeValue(m_configurationNode, "field");
			m_flid = AutoDataTreeMenuHandler.ContextMenuHelper.GetFlid(m_cache.MetaDataCacheAccessor,
				className, m_fieldName);
			launcher.Initialize(m_cache, m_obj, m_flid, m_fieldName,
				null, Mediator,
				DisplayNameProperty,
				BestWsName);
			// We don't want to be visible until later, since otherwise we get a temporary
			// display in the wrong place with the wrong size that serves only to annoy the
			// user.  See LT-1518 "The drawing of the DataTree for Lexicon/Advanced Edit draws
			// some initial invalid controls."  Becoming visible when we set the width and
			// height seems to delay things enough to avoid this visual clutter.
			launcher.Visible = false;
			Control = launcher;
			launcher.ViewSizeChanged += new FwViewSizeChangedEventHandler(this.OnViewSizeChanged);
			VectorReferenceView view = (VectorReferenceView)launcher.MainControl;
			view.ViewSizeChanged += new FwViewSizeChangedEventHandler(this.OnViewSizeChanged);
		}
	}
}
