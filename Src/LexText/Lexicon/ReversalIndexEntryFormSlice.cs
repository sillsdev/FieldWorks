using System.Xml;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for ReversalIndexEntryFormSlice.
	/// </summary>
	public class ReversalIndexEntryFormSlice : MultiStringSlice
	{
		private XmlNode m_configNode = null;
		private StringTable m_stringTbl = null;
		private IPersistenceProvider m_persistProvider = null;

		public ReversalIndexEntryFormSlice(FdoCache cache, string editor, int flid, XmlNode node,
			CmObject obj, StringTable stringTbl, IPersistenceProvider persistenceProvider, int ws)
			: base(obj.Hvo, flid, LangProject.kwsAllReversalIndex, false, true, true)
		{
			m_configNode = node;
			m_stringTbl = stringTbl;
			m_persistProvider = persistenceProvider;
		}
	}
}
