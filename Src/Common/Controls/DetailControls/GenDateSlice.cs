// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// This is a generic date slice.
	/// </summary>
	public class GenDateSlice : FieldSlice
	{
		public GenDateSlice(LcmCache cache, ICmObject obj, int flid)
			: base(new GenDateLauncher(), cache, obj, flid)
		{
		}

		public override void FinishInit()
		{
			base.FinishInit();
			// have chooser title use the same text as the label
			m_fieldName = XmlUtils.GetLocalizedAttributeValue(m_configurationNode, "label", m_fieldName);

			((GenDateLauncher)Control).Initialize(m_cache, m_obj, m_flid, m_fieldName, m_persistenceProvider, Mediator, m_propertyTable,
				"", "analysis");
		}

		protected override void UpdateDisplayFromDatabase()
		{
			((GenDateLauncher)Control).UpdateDisplayFromDatabase();
		}
	}
}
