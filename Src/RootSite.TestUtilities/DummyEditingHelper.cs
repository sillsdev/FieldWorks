// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace RootSite.TestUtilities
{
	/// <summary />
	public class DummyEditingHelper : RootSiteEditingHelper
	{
		internal IVwSelection m_mockedSelection = null;
		internal bool m_fOverrideGetParaPropStores = false;

		/// <summary />
		public DummyEditingHelper(LcmCache cache, IEditingCallbacks callbacks)
			: base(cache, callbacks)
		{
		}

		/// <summary>
		/// Overridden so that it works if we don't display the view
		/// </summary>
		/// <returns>Returns <c>true</c> if pasting is possible.</returns>
		public override bool CanPaste()
		{
			var fVisible = Control.Visible;
			Control.Visible = true;
			var fReturn = base.CanPaste();
			Control.Visible = fVisible;
			return fReturn;
		}

		/// <summary>
		/// Gets the selection from the root box that is currently being edited (can be null).
		/// </summary>
		public override IVwSelection RootBoxSelection => m_mockedSelection ?? base.RootBoxSelection;

		/// <summary>
		/// Gets an array of property stores, one for each paragraph in the given selection.
		/// </summary>
		protected override void GetParaPropStores(IVwSelection vwsel, out IVwPropertyStore[] vqvps)
		{
			if (m_fOverrideGetParaPropStores)
			{
				vqvps = new IVwPropertyStore[1];
			}
			else
			{
				base.GetParaPropStores(vwsel, out vqvps);
			}
		}

		/// <summary>
		/// Gets the caption props.
		/// </summary>
		public override ITsTextProps CaptionProps
		{
			get
			{
				var bldr = TsStringUtils.MakePropsBldr();
				bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Figure caption");
				return bldr.GetTextProps();
			}
		}
	}
}