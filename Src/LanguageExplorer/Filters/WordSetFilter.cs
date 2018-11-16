// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Xml.Linq;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// A filter for selecting a group of wordform spaced on a "IWfiWordSet"
	/// </summary>
	public class WordSetFilter : RecordFilter
	{
		/// <summary />
		protected int[] m_hvos;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:WordSetFilter"/> class.
		/// </summary>
		public WordSetFilter(IWfiWordSet wordSet)
		{
			id = wordSet.Hvo.ToString();
			Name = wordSet.Name.AnalysisDefaultWritingSystem.Text;
			LoadCases(wordSet);
		}

		private void LoadCases(IWfiWordSet wordSet)
		{
			m_hvos = wordSet.CasesRC.ToHvoArray();
		}

		/// <summary>
		/// Sync the word references to the state of the word list in the database.
		/// This is what we need to do when restoring our Filter from xml to make sure
		/// the ids are valid.
		/// </summary>
		internal void ReloadWordSet(LcmCache cache)
		{
			var hvo = int.Parse(id);
			var wordSet = cache.ServiceLocator.GetObject(hvo) as IWfiWordSet;
			LoadCases(wordSet);
		}

		/// <summary>
		/// Default constructor for IPersistAsXml
		/// </summary>
		public WordSetFilter()
		{
		}

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement element)
		{
			base.PersistAsXml (element);
			XmlUtils.SetAttribute(element, "id", id);
			XmlUtils.SetAttribute(element, "wordlist", XmlUtils.MakeStringFromList(m_hvos.ToList()));
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement element)
		{
			base.InitXml (element);
			id = XmlUtils.GetMandatoryAttributeValue(element, "id");
			m_hvos = XmlUtils.GetMandatoryIntegerListAttributeValue(element, "wordlist");
		}

		/// <summary>
		/// Test to see if this filter matches the other filter.
		/// </summary>
		public override bool SameFilter(RecordFilter other)
		{
			return other is WordSetFilter && other.id == id && other.Name == Name;
		}

		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		public override bool Accept (IManyOnePathSortItem item)
		{
			var hvo = item.KeyObject;

			for (var i = m_hvos.Length-1; i>=0; i--)
			{
				if (m_hvos[i] == hvo)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// This is always set by the user.
		/// </summary>
		public override bool IsUserVisible => true;
	}
}
