// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.LCModel;
using SIL.Xml;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public class PossibilityReferenceVectorSlice : ReferenceVectorSlice
	{
		protected PossibilityReferenceVectorSlice(Control control, LcmCache cache, ICmObject obj, int flid)
			: base(control, cache, obj, flid)
		{
		}

		public PossibilityReferenceVectorSlice(LcmCache cache, ICmObject obj, int flid)
			: base(new PossibilityVectorReferenceLauncher(), cache, obj, flid)
		{
		}

		protected override string BestWsName
		{
			get
			{
				var list = (ICmPossibilityList) m_obj.ReferenceTargetOwner(m_flid);
				var parameters = ConfigurationNode.Element("deParams");
				if (parameters == null)
					return list.IsVernacular ? "best vernoranal" : "best analorvern";

				return XmlUtils.GetOptionalAttributeValue(parameters, "ws", list.IsVernacular ? "best vernoranal" : "best analorvern");
			}
		}

		public override void FinishInit()
		{
			CheckDisposed();

			SetFieldFromConfig();
			base.FinishInit();
		}
	}
}
