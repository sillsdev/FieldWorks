using System;

namespace LanguageExplorer.Controls.LexText
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