// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Areas.Grammar;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace LanguageExplorer.Areas.Lists.Tools.FeatureTypesAdvancedEdit
{
	/// <summary />
	internal sealed class FeatureSystemInflectionFeatureListDlgLauncherSlice : MsaInflectionFeatureListDlgLauncherSlice
	{
		public FeatureSystemInflectionFeatureListDlgLauncherSlice()
			: base()
		{
		}

		/// <summary />
		public override void Install(DataTree parent)
		{
			CheckDisposed();

			base.Install(parent);

			FeatureSystemInflectionFeatureListDlgLauncher ctrl = (FeatureSystemInflectionFeatureListDlgLauncher)Control;

			m_flid = GetFlid(m_configurationNode, m_obj);
			if (m_flid != 0)
				m_fs = GetFeatureStructureFromMSA(m_obj, m_flid);
			else
			{
				m_fs = m_obj as IFsFeatStruc;
				m_flid = FsFeatStrucTags.kflidFeatureSpecs;
			}

			ctrl.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
			ctrl.Initialize(PropertyTable.GetValue<FdoCache>("cache"),
				m_fs,
				m_flid,
				"Name",
				ContainingDataTree.PersistenceProvder,
				"Name",
				XmlUtils.GetOptionalAttributeValue(m_configurationNode, "ws", "analysis")); // TODO: Get better default 'best ws'.
		}

		/// <summary>
		/// This method, called once we have a cache and object, is our first chance to
		/// actually create the embedded control.
		/// </summary>
		public override void FinishInit()
		{
			CheckDisposed();
			Control = new FeatureSystemInflectionFeatureListDlgLauncher();
		}
	}
}