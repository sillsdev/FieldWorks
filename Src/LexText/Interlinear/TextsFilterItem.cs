// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using static SIL.FieldWorks.Common.FwUtils.FwUtils;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using XCore;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// This apparently unused class is instantiated by the dynamic loader, specifically as one of the filter options
	/// for the Texts column in various tools in the Words area. It affords an additional way to get at the
	/// texts chooser.
	/// </summary>
	public class TextsFilterItem : NoChangeFilterComboItem
	{
		private Mediator m_mediator;

		public TextsFilterItem(ITsString tssName, LcmCache cache, Mediator mediator) : base(tssName)
		{
			m_mediator = mediator;
		}

		public override bool Invoke()
		{
			Publisher.Publish(new PublisherParameterObject(EventConstants.AddTexts, this));

			return false; // Whatever the user did, we don't currently count it as changing the filter.
		}
	}
}
