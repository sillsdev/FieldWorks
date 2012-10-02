using System;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Selections;
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews.Hookups
{
	public class TssHookup : LiteralStringParaHookup
	{
		// The delegates that allow us to get the value of the property, request notification of changes to it, and remove that request.
		Func<ITsString> Reader { get; set; }
		public Action<ITsString> Writer { get; set; }
		Action<TssHookup> AddHook { get; set; }
		Action<TssHookup> RemoveHook { get; set; }

		public TssHookup(TssHookupAdapter propAdapter, IStringParaNotification para)
			: base(propAdapter.Target, para)
		{
			Reader = propAdapter.Reader;
			AddHook = propAdapter.AddHook;
			RemoveHook = propAdapter.RemoveHook;
			AddHook(this);
		}
		public TssHookup(object target, Func<ITsString> reader, Action<TssHookup> hookAdder,
			Action<TssHookup> hookRemover, IStringParaNotification para)
			: base(target, para)
		{
			Reader = reader;
			AddHook = hookAdder;
			RemoveHook = hookRemover;
			AddHook(this);
		}

		public override void PropChanged(object sender, EventArgs args)
		{
			TssPropChanged(sender, args);
		}

		public void TssPropChanged(object modifiedObject, EventArgs args)
		{
			Para.StringChanged(ClientRunIndex, Reader());
		}
		#region IDisposable Members

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				RemoveHook(this);
		}

		internal override void InsertText(InsertionPoint ip, string input)
		{
			var bldr = ((TssClientRun)ParaBox.Source.ClientRuns[ClientRunIndex]).Tss.GetBldr();
			// Where there is a choice, we want the new text to have the properties of the neighbor
			// character that the IP is most closely associated with.
			ITsTextProps props;
			if (ip.StringPosition > 0 && ip.AssociatePrevious)
				props = bldr.get_PropertiesAt(ip.StringPosition - 1);
			else
				props = bldr.get_PropertiesAt(ip.StringPosition); // might be the lim, but that's OK.
			// Enhance JohnT: there may possibly be some special caes, e.g., where the indicated character
			// is an ORC linked to certain kinds of data or a verse number, where we don't want to copy all
			// the properties.
			bldr.Replace(ip.StringPosition, ip.StringPosition, input, props);
			Writer(bldr.GetString());
		}

		internal override bool CanInsertText(InsertionPoint ip)
		{
			return Writer != null && ParaBox != null; // todo: test this case
		}

		internal override bool CanDelete(InsertionPoint start, InsertionPoint end)
		{
			return Writer != null && ParaBox != null && start.Hookup == this
				&& end.Hookup == this && start.StringPosition < end.StringPosition;
		}

		internal override void Delete(InsertionPoint start, InsertionPoint end)
		{
			var bldr = ((TssClientRun)ParaBox.Source.ClientRuns[ClientRunIndex]).Tss.GetBldr();
			int newPos = start.StringPosition;
			bldr.Replace(newPos, end.StringPosition, "", null);
			Writer(bldr.GetString());
		}
		#endregion
	}
}
