using System;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.LexText
{
	public class ContextMenuRequestedEventArgs : EventArgs
	{
		private readonly IVwSelection m_selection;

		public ContextMenuRequestedEventArgs(IVwSelection selection)
		{
			m_selection = selection;
		}

		public IVwSelection Selection
		{
			get { return m_selection; }
		}

		public bool Handled { get; set; }
	}
}