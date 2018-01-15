// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This is used for the root when we want to suppress editing.
	/// </summary>
	internal class ReadOnlyRootDisplayCommand : RootDisplayCommand
	{

		public ReadOnlyRootDisplayCommand(string rootLayoutName, SimpleRootSite rootSite)
			: base(rootLayoutName, rootSite)
		{
		}

		internal override void ProcessChildren(int fragId, XmlVc vc, IVwEnv vwenv, XElement node, int hvo)
		{
			// Suppress editing for the whole view. Easiest thing is to insert another div.
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			base.ProcessChildren(fragId, vc, vwenv, node, hvo);
		}
	}
}