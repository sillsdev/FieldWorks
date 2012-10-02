using System;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews.Hookups
{
	/// <summary>
	/// This class manages the hookup between one alternative of a multilingual string property and the place where it is displayed.
	/// </summary>
	public class MlsHookup : LiteralStringParaHookup, IDisposable
	{
		public IViewMultiString MultiAccessor { get; private set; }
		private int m_ws;
		public MlsHookup(object target, IViewMultiString accessor, int ws, IStringParaNotification para)
			: base(target, para)
		{
			MultiAccessor = accessor;
			m_ws = ws;
			MultiAccessor.StringChanged += MlsPropChanged;
		}

		public void MlsPropChanged(object modifiedObject, MlsChangedEventArgs args)
		{
			if (args.WsId == m_ws)
				Para.StringChanged(ClientRunIndex, MultiAccessor.get_String(m_ws));
		}

		internal override void InsertText(InsertionPoint ip, string input)
		{
			throw new NotImplementedException("MlsHookup needs to implement InsertText but does not yet");
		}

		internal override bool CanInsertText(InsertionPoint ip)
		{
			return false; // todo: should be able to, when InsertText is implemeneted.
		}

		#region IDisposable Members

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				MultiAccessor.StringChanged -= MlsPropChanged;
		}

		#endregion
	}
}
