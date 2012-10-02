using System;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Selections;
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews.Hookups
{

	/// <summary>
	/// This is a cut-down version of StringHookup used in selection of client runs that don't have natural hookups,
	/// such as literal strings. It never sends change notifcations, so doesn't need delegates for updates and notifications.
	/// </summary>
	public class LiteralStringParaHookup : Hookup
	{
		public LiteralStringParaHookup(object target, IStringParaNotification para)
			: base(target)
		{
			Para = para;
		}
		/// <summary>
		/// The paragraph displaying the string this hookup connects to.
		/// </summary>
		public IStringParaNotification Para { get; set; }
		public ParaBox ParaBox { get { return Para as ParaBox; } }
		/// <summary>
		/// A string para hookup always connects a paragraph to the source of one client run. This index tells
		/// which client run within the paragraph it is.
		/// </summary>
		public int ClientRunIndex { get; set; }

		/// <summary>
		/// This kind of hookup can really do it!
		/// </summary>
		public override InsertionPoint SelectAtEnd()
		{
			return new InsertionPoint(this, ParaBox.Source.ClientRuns[ClientRunIndex].Length, true);
		}
		// A generic hookup isn't able to do this, but various subclasses can.
		internal virtual void InsertText(InsertionPoint ip, string input)
		{
		}

		/// <summary>
		/// A generic hookup knows it can't, but various subclasses can.
		/// </summary>
		internal virtual bool CanInsertText(InsertionPoint ip)
		{
			return false;
		}

		/// <summary>
		/// The text of the client run which this hookup connects.
		/// </summary>
		internal string Text
		{
			get
			{
				if (ParaBox == null)
					return "";
				return ParaBox.Source.ClientRuns[ClientRunIndex].Text;
			}

		}

		/// <summary>
		/// Return true if we can delete the specified range. A LiteralStringHookup never can,
		/// but some subclasses can.
		/// </summary>
		internal virtual bool CanDelete(InsertionPoint start, InsertionPoint end)
		{
			return false;
		}

		/// <summary>
		/// Delete the specified range (some subclasses actually can).
		/// Todo JohnT: more should be able to.
		/// </summary>
		internal virtual void Delete(InsertionPoint start, InsertionPoint end)
		{
		}
	}

	public class StringHookup : LiteralStringParaHookup
	{
		// The delegates that allow us to get the value of the property, request notification of changes to it, and remove that request.
		Func<string> Reader { get; set; }
		public Action<string> Writer { get; set; }
		Action<StringHookup> AddHook { get; set; }
		Action<StringHookup> RemoveHook { get; set; }

		public StringHookup(object target, Func<string> reader, Action<StringHookup> hookAdder, Action<StringHookup> hookRemover,
			IStringParaNotification para)
			: base(target, para)
		{
			Reader = reader;
			AddHook = hookAdder;
			RemoveHook = hookRemover;
			AddHook(this);
		}
		public void StringPropChanged(object modifiedObject, EventArgs args)
		{
			Para.StringChanged(ClientRunIndex, Reader());
		}
		#region IDisposable Members

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				RemoveHook(this);
		}

		#endregion

		internal override void InsertText(InsertionPoint ip, string input)
		{
			string oldValue = ((StringClientRun)ParaBox.Source.ClientRuns[ClientRunIndex]).Contents;
			string newValue = oldValue.Insert(ip.StringPosition, input);
			Writer(newValue);
		}

		internal override bool CanInsertText(InsertionPoint ip)
		{
			return Writer != null && ParaBox != null;
		}

		internal override bool CanDelete(InsertionPoint start, InsertionPoint end)
		{
			return Writer != null && ParaBox != null && start.Hookup == this
				&& end.Hookup == this && start.StringPosition < end.StringPosition;
		}

		internal override void Delete(InsertionPoint start, InsertionPoint end)
		{
			string oldValue = ((StringClientRun)ParaBox.Source.ClientRuns[ClientRunIndex]).Contents;
			int newPos = start.StringPosition;
			string newValue = oldValue.Remove(newPos, end.StringPosition - newPos);
			Writer(newValue);
		}
	}

	/// <summary>
	/// Interface used to notify paragraph that the index'th string has changed. The only real implementation is ParaBox,
	/// and various kinds of selection assume they can cast this to ParaBox. However, it's convenient to have the interface to mock
	/// for testing the hookup itself.
	/// </summary>
	public interface IStringParaNotification
	{
		void StringChanged(int clientRunIndex, ITsString newValue);
		void StringChanged(int clientRunIndex, string newValue);
	}
}
