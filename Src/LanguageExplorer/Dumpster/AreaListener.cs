// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;

namespace LanguageExplorer.Dumpster
{
#if RANDYTODO
	// TODO: I don't expect this class to survive, but its useful code moved elsewhere, as ordinary event handlers.
#endif
	/// <summary>
	/// Summary description for AreaListener.
	/// </summary>
	internal static class AreaListener
	{
		/// <summary>
		/// This is designed to be called by reflection through the mediator, when something typically in xWorks needs to get
		/// the parameter node for a given tool. The last argument is a one-item array used to return the result,
		/// since I don't think we handle Out parameters in our SendMessage protocol.
		/// </summary>
		internal static XElement GetContentControlParameters(XElement windowConfiguration, string areaName, string toolName)
		{
			return null;
		}
	}
}
