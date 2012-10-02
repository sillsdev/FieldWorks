using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// This is a generic date slice.
	/// </summary>
	public class GenDateSlice : FieldSlice
	{
		public GenDateSlice(FdoCache cache, ICmObject obj, int flid)
			: base(new GenDateLauncher(), cache, obj, flid)
		{
		}

		public override void FinishInit()
		{
			base.FinishInit();
			// have chooser title use the same text as the label
			if (Mediator != null && Mediator.HasStringTable)
				m_fieldName = XmlUtils.GetLocalizedAttributeValue(Mediator.StringTbl,
					m_configurationNode, "label", m_fieldName);
			else if (StringTbl != null)
				m_fieldName = XmlUtils.GetLocalizedAttributeValue(StringTbl,
					m_configurationNode, "label", m_fieldName);
			else
				m_fieldName = XmlUtils.GetOptionalAttributeValue(
					m_configurationNode, "label", m_fieldName);

			((GenDateLauncher)Control).Initialize(m_cache, m_obj, m_flid, m_fieldName, m_persistenceProvider, Mediator,
				"", "analysis");
		}

		protected override void UpdateDisplayFromDatabase()
		{
			((GenDateLauncher)Control).UpdateDisplayFromDatabase();
		}
	}
}
