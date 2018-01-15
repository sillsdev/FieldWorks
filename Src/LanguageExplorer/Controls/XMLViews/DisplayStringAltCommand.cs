// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// DisplayCommand that displays the current object by displaying a specified ws of a specified
	/// multilingual property.
	/// </summary>
	internal class DisplayStringAltCommand : DisplayCommand
	{
		int m_ws;
		int m_tag;
		private XElement m_caller;
		public DisplayStringAltCommand(int tag, int ws, XElement caller)
		{
			m_tag = tag;
			m_ws = ws;
			m_caller = caller;
		}

		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			if (m_caller != null)
			{
				vc.MarkSource(vwenv, m_caller);
			}
			vwenv.AddStringAltMember(m_tag, m_ws, vc);
		}

		public override bool Equals(object obj)
		{
			var other = obj as DisplayStringAltCommand;
			if (other == null)
			{
				return false;
			}
			return other.m_tag == m_tag && other.m_ws == m_ws && other.m_caller == m_caller;
		}

		public override int GetHashCode()
		{
			return m_tag + m_ws + (m_caller == null ? 0 : m_caller.GetHashCode());
		}

		internal override void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			info.AddAtomicField(m_tag, m_ws);
		}
	}
}