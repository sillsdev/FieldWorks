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
	/// This class is always used to handle fragid 0.
	/// </summary>
	internal class RootDisplayCommand : DisplayCommand
	{
		string m_rootLayoutName;
		internal SimpleRootSite m_rootSite;

		public RootDisplayCommand(string rootLayoutName, SimpleRootSite rootSite)
			: base()
		{
			m_rootLayoutName = rootLayoutName;
			m_rootSite = rootSite;
		}

		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			var node = vc.GetNodeForPart(hvo, m_rootLayoutName, true);
			ProcessChildren(fragId, vc, vwenv, node, hvo);
		}

		internal override void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			var clsid = info.TargetClass(vc);
			if (clsid == 0)
			{
				return; // or assert? an object prop should have a dest class.
			}
			DetermineNeededFieldsForClass(vc, fragId, clsid, info);
		}

		internal virtual void DetermineNeededFieldsForClass(XmlVc vc, int fragId, int clsid, NeededPropertyInfo info)
		{
			var node = vc.GetNodeForPart(m_rootLayoutName, true, clsid);
			DetermineNeededFieldsForChildren(vc, node, null, info);
		}

		public override bool Equals(object obj)
		{
			var rdcOther = obj as RootDisplayCommand;
			return rdcOther != null && base.Equals(obj) && m_rootLayoutName == rdcOther.m_rootLayoutName;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode() + m_rootLayoutName.GetHashCode();
		}

		internal override void ProcessChildren(int fragId, XmlVc vc, IVwEnv vwenv, XElement node, int hvo)
		{
			// If available, apply defaults from 'Normal' to everything.
			var styleSheet = m_rootSite.StyleSheet;
			if (styleSheet != null)
			{
				vwenv.Props = styleSheet.NormalFontStyle;
			}
			vwenv.OpenDiv();
			base.ProcessChildren(fragId, vc, vwenv, node, hvo);
			vwenv.CloseDiv();
		}
	}
}