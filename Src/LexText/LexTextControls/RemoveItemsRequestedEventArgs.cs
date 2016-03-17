using System;

namespace SIL.FieldWorks.LexText.Controls
{
	public class RemoveItemsRequestedEventArgs : EventArgs
	{
		private readonly bool m_forward;

		public RemoveItemsRequestedEventArgs(bool forward)
		{
			m_forward = forward;
		}

		public bool Forward
		{
			get { return m_forward; }
		}
	}
}