// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// Main class for displaying the AtomicReferenceSlice.
	/// </summary>
	internal class PossibilityAtomicReferenceView : AtomicReferenceView
	{
		public const int kflidFake = -2222;
		private PossibilityAtomicReferenceViewSdaDecorator m_sda;

		#region Construction, initialization, and disposal

		protected override void SetRootBoxObj()
		{
			if (m_rootObj != null && m_rootObj.IsValidObject)
			{
				var hvo = m_cache.DomainDataByFlid.get_ObjectProp(m_rootObj.Hvo, m_rootFlid);
				if (hvo != 0)
				{
					var obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
					var label = ObjectLabel.CreateObjectLabel(m_cache, obj, m_displayNameProperty, m_displayWs);
					m_sda.Tss = label.AsTss;
				}
				else
				{
					var list = (ICmPossibilityList)m_rootObj.ReferenceTargetOwner(m_rootFlid);
					var ws = list.IsVernacular ? m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle : m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
					if (list.PossibilitiesOS.Count > 0)
					{
						var label = ObjectLabel.CreateObjectLabel(m_cache, list.PossibilitiesOS[0], m_displayNameProperty, m_displayWs);
						ws = label.AsTss.get_WritingSystem(0);
					}
					m_sda.Tss = TsStringUtils.EmptyString(ws);
				}
			}
			base.SetRootBoxObj();
		}

		#endregion // Construction, initialization, and disposal

		#region RootSite required methods

		protected override ISilDataAccess GetDataAccess()
		{
			m_sda = new PossibilityAtomicReferenceViewSdaDecorator(m_cache.GetManagedSilDataAccess());
			return m_sda;
		}

		public override void SetReferenceVc()
		{
			m_atomicReferenceVc = new PossibilityAtomicReferenceVc(m_cache, m_rootFlid, m_displayNameProperty);
		}

		#endregion // RootSite required methods

		#region other overrides

		protected override void EnsureDefaultSelection()
		{
			RootBox?.MakeSimpleSel(true, true, true, true);
		}

		protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			// do nothing
		}

		#endregion
	}
}