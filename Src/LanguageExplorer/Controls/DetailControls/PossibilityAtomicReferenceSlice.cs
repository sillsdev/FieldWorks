// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class PossibilityAtomicReferenceSlice : AtomicReferenceSlice
	{
		internal PossibilityAtomicReferenceSlice(LcmCache cache, ICmObject obj, int flid)
			: this(new PossibilityAtomicReferenceLauncher(), cache, obj, flid)
		{
		}

		protected PossibilityAtomicReferenceSlice(PossibilityAtomicReferenceLauncher launcher, LcmCache cache, ICmObject obj, int flid)
			: base(launcher, cache, obj, flid)
		{
		}

		protected override string BestWsName
		{
			get
			{
				var list = (ICmPossibilityList) Object.ReferenceTargetOwner(m_flid);
				var parameters = ConfigurationNode.Element("deParams");
				if (parameters == null)
				{
					return list.IsVernacular ? "best vernoranal" : "best analorvern";
				}

				return XmlUtils.GetOptionalAttributeValue(parameters, "ws", list.IsVernacular ? "best vernoranal" : "best analorvern");
			}
		}

		/// <summary>
		/// Override method to add suitable control.
		/// </summary>
		public override void FinishInit()
		{
			// REVIEW (DamienD): do we need to do this?
			SetFieldFromConfig();
			base.FinishInit();
		}
	}
}