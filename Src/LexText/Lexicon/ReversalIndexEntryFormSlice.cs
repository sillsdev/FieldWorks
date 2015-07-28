// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for ReversalIndexEntryFormSlice.
	/// </summary>
	public class ReversalIndexEntryFormSlice : MultiStringSlice
	{
#pragma warning disable 0414
		private XmlNode m_configNode = null;
		private StringTable m_stringTbl = null;
		private IPersistenceProvider m_persistProvider = null;
#pragma warning restore 0414

		public ReversalIndexEntryFormSlice(FdoCache cache, string editor, int flid, XmlNode node,
			ICmObject obj, StringTable stringTbl, IPersistenceProvider persistenceProvider, int ws)
			: base(obj, flid, WritingSystemServices.kwsAllReversalIndex, 0, false, true, true)
		{
			m_configNode = node;
			m_stringTbl = stringTbl;
			m_persistProvider = persistenceProvider;
		}
	}
}
