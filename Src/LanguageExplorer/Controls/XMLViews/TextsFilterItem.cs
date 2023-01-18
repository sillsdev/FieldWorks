// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This apparently unused class is instantiated by the dynamic loader, specifically as one of the filter options
	/// for the Texts column in various tools in the Words area. It affords an additional way to get at the
	/// texts chooser.
	/// </summary>
	internal sealed class TextsFilterItem : NoChangeFilterComboItem
	{
		private readonly IPublisher m_publisher;

		internal TextsFilterItem(ITsString tssName, IPublisher publisher) : base(tssName)
		{
			m_publisher = publisher;
		}

		internal override bool Invoke()
		{
			// Not sure this can happen but play safe.
			if (m_publisher != null)
			{
				m_publisher.Publish(new List<PublisherParameterObject>
				{
					new PublisherParameterObject("ProgressReset", this),
					new PublisherParameterObject("AddTexts", this),
					new PublisherParameterObject("ProgressReset", this)
				});
			}
			return false; // Whatever the user did, we don't currently count it as changing the filter.
		}
	}
}