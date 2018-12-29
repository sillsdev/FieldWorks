// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// XmlVc uses this class and its subclasses to help display things.
	/// </summary>
	internal abstract class DisplayCommand
	{
		private static int _displayLevel;

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
			++_displayLevel;
			vc.ProcessChildren(node, vwenv, hvo);
			--_displayLevel;
		}

		internal virtual void ProcessChildren(int fragId, XmlVc vc, IVwEnv vwenv, XElement node, int hvo, XElement caller)
		{
			++_displayLevel;
			vc.ProcessChildren(node, vwenv, hvo, caller);
			--_displayLevel;
		}

		// Gather up info about what fields are needed for the specified node.
		internal void DetermineNeededFieldsForChildren(XmlVc vc, XElement node, XElement caller, NeededPropertyInfo info)
		{
			vc.DetermineNeededFieldsForChildren(node, caller, info);
		}
	}
}