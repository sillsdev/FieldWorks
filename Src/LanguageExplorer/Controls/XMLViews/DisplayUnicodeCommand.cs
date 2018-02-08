// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// DisplayCommand that displays the current object by displaying a specified unicode property
	/// as a string in a specified writing system.
	/// </summary>
	internal class DisplayUnicodeCommand : DisplayCommand
	{
		readonly int m_ws;
		readonly int m_tag;

		public DisplayUnicodeCommand(int tag, int ws)
		{
			m_tag = tag;
			m_ws = ws;
		}

		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			vwenv.AddUnicodeProp(m_tag, m_ws, vc);
		}
		internal override void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			info.AddAtomicField(m_tag, 0);
		}

		public override bool Equals(object obj)
		{
			var other = obj as DisplayUnicodeCommand;
			if (other == null)
			{
				return false;
			}
			return other.m_tag == m_tag && other.m_ws == m_ws;
		}

		public override int GetHashCode()
		{
			return m_tag + m_ws;
		}
	}
}