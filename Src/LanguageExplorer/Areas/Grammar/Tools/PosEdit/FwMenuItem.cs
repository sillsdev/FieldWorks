// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Areas.Grammar.Tools.PosEdit
{
	/// <summary>
	/// This class stores the information needed for one menu item in a menu that must be displayed
	/// using views code (in order to handle multiple writing systems/fonts within each menu item).
	/// </summary>
	internal class FwMenuItem
	{
		public FwMenuItem(ITsString tssItem, XElement xnConfig, bool fEnabled)
		{
			Label = tssItem;
			ConfigurationNode = xnConfig;
			Enabled = fEnabled;
		}

		public ITsString Label { get; }

		public string Message => XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "message");

		public bool Enabled { get; }

		public XElement ConfigurationNode { get; }
	}
}