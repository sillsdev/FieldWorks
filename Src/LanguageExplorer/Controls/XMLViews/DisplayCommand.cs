// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// this class contains the info used as a value in m_idToDisplayInfo
	/// and as a key in m_displayInfoToId.
	/// </summary>
	public abstract class DisplayCommand
	{
		internal abstract void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv);

		/// <summary>
		/// Add to info as much useful information as possible about fields and children
		/// that it might be useful to preload in order to handle this command on various
		/// objects that can occur in the property that is the source for info.
		/// </summary>
		internal virtual void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			// Default does nothing.
		}

		internal virtual void ProcessChildren(int fragId, XmlVc vc, IVwEnv vwenv, XElement node, int hvo)
		{
			++MainCallerDisplayCommand.displayLevel;
			vc.ProcessChildren(node, vwenv, hvo);
			--MainCallerDisplayCommand.displayLevel;
		}

		internal virtual void ProcessChildren(int fragId, XmlVc vc, IVwEnv vwenv, XElement node, int hvo, XElement caller)
		{
			++MainCallerDisplayCommand.displayLevel;
			vc.ProcessChildren(node, vwenv, hvo, caller);
			--MainCallerDisplayCommand.displayLevel;
		}

		// Gather up info about what fields are needed for the specified node.
		internal void DetermineNeededFieldsForChildren(XmlVc vc, XElement node, XElement caller, NeededPropertyInfo info)
		{
			vc.DetermineNeededFieldsForChildren(node, caller, info);
		}
	}
}