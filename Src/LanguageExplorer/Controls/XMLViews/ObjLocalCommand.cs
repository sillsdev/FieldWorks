// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// DisplayCommand that displays the current object by displaying the children of one node, treating another as caller.
	/// Typically at present the node whose children are to be procesed is an "objlocal" node, and the
	/// caller is the "part ref" node that invoked it.
	/// </summary>
	internal class ObjLocalCommand : DisplayCommand
	{
		XElement m_objLocal;
		XElement m_caller;
		public ObjLocalCommand(XElement objLocal, XElement caller)
		{
			m_objLocal = objLocal;
			m_caller = caller;
		}

		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			vc.ProcessChildren(m_objLocal, vwenv, hvo, m_caller);
		}

		internal override void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			var clsid = info.TargetClass(vc);
			if (clsid == 0)
			{
				return; // or assert? an object prop should have a dest class.
			}
			DetermineNeededFieldsForChildren(vc, m_objLocal, m_caller, info);
		}

		public override bool Equals(object obj)
		{
			var other = obj as ObjLocalCommand;
			if (other == null)
			{
				return false;
			}
			return other.m_caller == m_caller && other.m_objLocal == m_objLocal;
		}

		int HashOrZero(XElement node)
		{
			return node?.GetHashCode() ?? 0;
		}

		public override int GetHashCode()
		{
			return HashOrZero(m_objLocal) + HashOrZero(m_caller);
		}
	}
}