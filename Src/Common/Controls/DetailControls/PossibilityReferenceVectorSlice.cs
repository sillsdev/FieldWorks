using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public class PossibilityReferenceVectorSlice : ReferenceVectorSlice
	{
		protected PossibilityReferenceVectorSlice(Control control, FdoCache cache, ICmObject obj, int flid)
			: base(control, cache, obj, flid)
		{
		}

		public PossibilityReferenceVectorSlice(FdoCache cache, ICmObject obj, int flid)
			: base(new PossibilityVectorReferenceLauncher(), cache, obj, flid)
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

		public override void FinishInit()
		{
			CheckDisposed();

			SetFieldFromConfig();
			base.FinishInit();
		}
	}
}
