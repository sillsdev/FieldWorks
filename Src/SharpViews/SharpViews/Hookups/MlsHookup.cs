// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Paragraphs;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews.Hookups
{
	/// <summary>
	/// This class manages the hookup between one alternative of a multilingual string property and the place where it is displayed.
	/// </summary>
	public class MlsHookup : LiteralStringParaHookup
	{
		private IViewMultiString MultiAccessor { get; set; }
		Action<MlsHookup> AddHook { get; set; }
		Action<MlsHookup> RemoveHook { get; set; }

		private int m_ws;
		public MlsHookup(object target, IViewMultiString accessor, int ws, MlsHookupAdapter propAdapter, IStringParaNotification para)
			: base(target, para)
		{
			MultiAccessor = accessor;
			m_ws = ws;
			MultiAccessor.StringChanged += MlsPropChanged;
			AddHook = propAdapter.AddHook;
			RemoveHook = propAdapter.RemoveHook;
			AddHook(this);
		}

		public MlsHookup(object target, IViewMultiString accessor, int ws, Action<MlsHookup> hookAdder,
			Action<MlsHookup> hookRemover, IStringParaNotification para)
			: base(target, para)
		{
			MultiAccessor = accessor;
			m_ws = ws;
			MultiAccessor.StringChanged += MlsPropChanged;
			AddHook = hookAdder;
			RemoveHook = hookRemover;
			AddHook(this);
		}


		public override void PropChanged(object sender, EventArgs args)
		{
			MlsPropChanged(sender, (MlsChangedEventArgs)args);
		}

		public void MlsPropChanged(object modifiedObject, MlsChangedEventArgs args)
		{
			if (args.WsId == m_ws)
			{
				Para.StringChanged(ClientRunIndex, MultiAccessor);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				MultiAccessor.StringChanged -= MlsPropChanged;
		}

		public override string GetStyleNameAt(InsertionPoint ip)
		{
			return ((TssClientRun)ParaBox.Source.ClientRuns[ClientRunIndex]).CharacterStyleNameAt(ip.StringPosition);
		}

		internal override void InsertText(InsertionPoint ip, string input)
		{
			MultiAccessor.set_String(m_ws, ((MlsClientRun)ParaBox.Source.ClientRuns[ClientRunIndex]).Tss);
			var bldr = MultiAccessor.get_String(m_ws).GetBldr();
			// Where there is a choice, we want the new text to have the properties of the neighbor
			// character that the IP is most closely associated with.
			ITsTextProps props;
			if (ip.StringPosition > 0 && ip.AssociatePrevious)
				props = bldr.get_PropertiesAt(ip.StringPosition - 1);
			else
				props = bldr.get_PropertiesAt(ip.StringPosition); // might be the lim, but that's OK.
			if (ip.StyleToBeApplied != null)
			{
				var propsBldr = props.GetBldr();
				propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, ip.StyleToBeApplied.Name);
				props = propsBldr.GetTextProps();
			}

			// Enhance JohnT: there may possibly be some special caes, e.g., where the indicated character
			// is an ORC linked to certain kinds of data or a verse number, where we don't want to copy all
			// the properties.
			bldr.Replace(ip.StringPosition, ip.StringPosition, input, props);
			MultiAccessor.set_String(m_ws, bldr.GetString());
		}

		internal override void InsertText(InsertionPoint ip, ITsString input)
		{
			var bldr = ((TssClientRun)ParaBox.Source.ClientRuns[ClientRunIndex]).Tss.GetBldr();
			bldr.ReplaceTsString(ip.StringPosition, ip.StringPosition, input);
			MultiAccessor.set_String(m_ws, bldr.GetString());
		}

		internal override bool CanInsertText(InsertionPoint ip)
		{
			return ParaBox != null; // todo: test this casel
		}

		internal override bool CanDelete(InsertionPoint start, InsertionPoint end)
		{
			return start != null && end != null && ParaBox != null && start.Hookup == this
				   && end.Hookup == this && start.StringPosition < end.StringPosition;
		}

		internal override void Delete(InsertionPoint start, InsertionPoint end)
		{
			if (CanDelete(start, end))
			{
				MultiAccessor.set_String(m_ws, ((MlsClientRun) ParaBox.Source.ClientRuns[ClientRunIndex]).Tss);
				var bldr = MultiAccessor.get_String(m_ws).GetBldr();
				int newPos = start.StringPosition;
				bldr.Replace(newPos, end.StringPosition, "", null);
				MultiAccessor.set_String(m_ws, bldr.GetString());
			}
		}

		internal override bool CanApplyStyle(InsertionPoint start, InsertionPoint end, string style)
		{
			return start != null && end != null && ParaBox != null && start.Hookup == this
				   && end.Hookup == this && style != null && ParaBox.Style.Stylesheet.Style(style) != null;
		}

		internal override void ApplyStyle(InsertionPoint start, InsertionPoint end, string style)
		{
			if (!CanApplyStyle(start, end, style))
				return;

			MultiAccessor.set_String(m_ws, ((MlsClientRun) ParaBox.Source.ClientRuns[ClientRunIndex]).Tss);
			var bldr = MultiAccessor.get_String(m_ws).GetBldr();
			int newPos = start.StringPosition;
			bldr.SetStrPropValue(newPos, end.StringPosition, (int) FwTextPropType.ktptNamedStyle, style);
			MultiAccessor.set_String(m_ws, bldr.GetString());
		}
	}
}
