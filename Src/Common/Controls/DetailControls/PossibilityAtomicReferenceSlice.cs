// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public class PossibilityAtomicReferenceSlice : AtomicReferenceSlice
	{
		public PossibilityAtomicReferenceSlice(FdoCache cache, ICmObject obj, int flid)
			: this(new PossibilityAtomicReferenceLauncher(), cache, obj, flid)
		{
		}

		protected PossibilityAtomicReferenceSlice(PossibilityAtomicReferenceLauncher launcher, FdoCache cache, ICmObject obj, int flid)
			: base(launcher, cache, obj, flid)
		{
		}

		protected override string BestWsName
		{
			get
			{
				var list = (ICmPossibilityList) m_obj.ReferenceTargetOwner(m_flid);
				XmlNode parameters = ConfigurationNode.SelectSingleNode("deParams");
				if (parameters == null)
					return list.IsVernacular ? "best vernoranal" : "best analorvern";

				return XmlUtils.GetOptionalAttributeValue(parameters, "ws", list.IsVernacular ? "best vernoranal" : "best analorvern");
			}
		}

		/// <summary>
		/// Override method to add suitable control.
		/// </summary>
		public override void FinishInit()
		{
			CheckDisposed();
			// REVIEW (DamienD): do we need to do this?
			SetFieldFromConfig();
			base.FinishInit();
		}
	}
}
