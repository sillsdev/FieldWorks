// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Xml.Linq;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This class adds the ability to test a condition and use it to decide whether to display each item.
	/// </summary>
	internal class ReadOnlyConditionalRootDisplayCommand : ReadOnlyRootDisplayCommand
	{
		XElement m_condition;
		private ISilDataAccess m_sda;
		public ReadOnlyConditionalRootDisplayCommand(string rootLayoutName, SimpleRootSite rootSite, XElement condition, ISilDataAccess sda)
			: base(rootLayoutName, rootSite)
		{
			m_condition = condition;
			Debug.Assert(rootSite is RootSite, "conditional display requires real rootsite with cache");
			m_sda = sda;
		}
		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			if (XmlVc.ConditionPasses(m_condition, hvo, (m_rootSite as RootSite).Cache, m_sda))
			{
				base.PerformDisplay(vc, fragId, hvo, vwenv);
			}
		}

		/// <summary>
		/// Overrides to determine the fields needed for evaluating the condition as well as for displaying the
		/// actual objects.
		/// </summary>
		internal override void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			base.DetermineNeededFields(vc, fragId, info);
			vc.DetermineNeededFieldsFor(m_condition, null, info);
		}

		internal override void DetermineNeededFieldsForClass(XmlVc vc, int fragId, int clsid, NeededPropertyInfo info)
		{
			base.DetermineNeededFieldsForClass(vc, fragId, clsid, info);
			vc.DetermineNeededFieldsFor(m_condition, null, info);
		}
	}
}