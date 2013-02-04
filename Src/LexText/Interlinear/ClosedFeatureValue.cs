using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.IText
{
	public class ClosedFeatureValue
	{
		private readonly IFsSymFeatVal m_symbol;
		private readonly bool m_negate;

		public ClosedFeatureValue(IFsSymFeatVal symbol, bool negate)
		{
			m_symbol = symbol;
			m_negate = negate;
		}

		public IFsSymFeatVal Symbol
		{
			get { return m_symbol; }
		}

		public bool Negate
		{
			get { return m_negate; }
		}
	}
}
