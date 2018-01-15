// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// DisplayCommand that displays the current object by displaying a specified string property.
	/// </summary>
	internal class DisplayStringCommand : DisplayCommand
	{
		int m_tag;
		public DisplayStringCommand(int tag)
		{
			m_tag = tag;
		}

		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			vwenv.AddStringProp(m_tag, vc);
		}
		internal override void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			info.AddAtomicField(m_tag, 0);
		}

		public override bool Equals(object obj)
		{
			var other = obj as DisplayStringCommand;
			return other?.m_tag == m_tag;
		}

		public override int GetHashCode()
		{
			return m_tag;
		}
	}
}