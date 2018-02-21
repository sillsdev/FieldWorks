// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.WritingSystems;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// Subclass suitable for LabeledMultistringView.
	/// </summary>
	internal class InnerLabeledMultiStringViewVc : LabeledMultiStringVc
	{
		private InnerLabeledMultiStringView m_view;

		public InnerLabeledMultiStringViewVc(int flid, List<CoreWritingSystemDefinition> rgws, int wsUser, bool editable, InnerLabeledMultiStringView view)
			: base(flid, rgws, wsUser, editable, view.WritingSystemFactory.GetWsFromStr("en"))
		{
			m_view = view;
			Debug.Assert(m_view != null);
		}

		internal override void TriggerDisplay(IVwEnv vwenv)
		{
			base.TriggerDisplay(vwenv);
			m_view.TriggerDisplay(vwenv);
		}

		internal override void AddViewWritingSystems(ISet<ILgWritingSystem> visibleWss)
		{
			if (m_view.WritingSystemsToDisplay != null)
			{
				visibleWss.UnionWith(m_view.WritingSystemsToDisplay);
			}
		}

		internal override bool SkipEmptyWritingSystem(ISet<ILgWritingSystem> visibleWss, int i, int hvo)
		{
			// if we have defined writing systems to display, we want to
			// show those, plus other options that have data.
			// otherwise, we'll assume we want to display the given ws fields.
			// (this effectively means that setting WritingSystemsToDisplay to 'null'
			// will display all the ws options in m_rgws. That is also what happens in the base class.)
			if (m_view.WritingSystemsToDisplay != null)
			{
				// if we haven't configured to display this writing system
				// we still want to show it if it has data.
				if (!visibleWss.Contains(m_rgws[i]))
				{
					var result = m_view.Cache.MainCacheAccessor.get_MultiStringAlt(hvo, m_flid, m_rgws[i].Handle);
					if (result == null || result.Length == 0)
					{
						return true;
					}
				}
			}
			return false;
		}
		public override string TextStyle
		{
			get
			{
				var sTextStyle = "Default Paragraph Characters";
				if (m_view != null)
				{
					sTextStyle = m_view.TextStyle;
				}
				return sTextStyle;
			}
			set
			{
				if (m_view != null)
				{
					m_view.TextStyle = value;
				}
			}
		}

	}
}