using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// This apparently unused class is instantiated by the dynamic loader, specifically as one of the filter options
	/// for the Texts column in various tools in the Words area. It affords an additional way to get at the
	/// texts chooser.
	/// </summary>
	public class TextsFilterItem : NoChangeFilterComboItem
	{
		private readonly IPublisher m_publisher;

		public TextsFilterItem(ITsString tssName, IPublisher publisher) : base(tssName)
		{
			m_publisher = publisher;
		}

		public override bool Invoke()
		{
			// Not sure this can happen but play safe.
			if (m_publisher != null)
			{
				m_publisher.Publish("ProgressReset", this);
				m_publisher.Publish("AddTexts", this);
				m_publisher.Publish("ProgressReset", this);
			}

			return false; // Whatever the user did, we don't currently count it as changing the filter.
		}
	}
}
