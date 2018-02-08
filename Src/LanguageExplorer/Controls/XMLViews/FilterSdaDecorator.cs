// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.LCModel.Application;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This decorator modifies certain sequence properties by filtering from them items not in a master list.
	/// It is not currently used (as of LT-10260) since the new DictionaryPublicationDecorator achieves the purpose
	/// more effectively. However for now I'm keeping the source code in case we find a reason again to hide
	/// references to filtered-out items. To enable it, uncomment stuff in XmlSeqView.GetSda().
	/// </summary>
	public class FilterSdaDecorator : DomainDataByFlidDecoratorBase
	{
		private int m_mainFlid;
		private int m_hvoRoot;
		private Dictionary<int, ITestItem> m_filterFlids = new Dictionary<int, ITestItem>();
		private readonly HashSet<int> m_validHvos;

		/// <summary>
		/// Make one that wraps the specified cache and passes items in the specified property of the specified root object.
		/// </summary>
		public FilterSdaDecorator(ISilDataAccessManaged domainDataByFlid, int mainFlid, int hvoRoot)
			: base(domainDataByFlid)
		{
			m_mainFlid = mainFlid;
			m_hvoRoot = hvoRoot;
			var chvoReal = BaseSda.get_VecSize(m_hvoRoot, m_mainFlid);
			using (var arrayPtr = MarshalEx.ArrayToNative<int>(chvoReal))
			{
				BaseSda.VecProp(m_hvoRoot, m_mainFlid, chvoReal, out chvoReal, arrayPtr);
				m_validHvos = new HashSet<int>(MarshalEx.NativeToArray<int>(arrayPtr, chvoReal));
			}
		}

		/// <summary>
		/// Set the filter flids from a string that contains semi-colon-separated sequence of Class.Field strings.
		/// </summary>
		public void SetFilterFlids(string input)
		{
			foreach (var field in input.Split(';'))
			{
				var parts = field.Trim().Split(':');
				if (parts.Length != 2)
				{
					throw new ArgumentException("Expected sequence of class.field:class.field;class.field:class.field but got " + input);
				}
				var flidMain = Flid(parts[0]);
				var flidRel = Flid(parts[1]);
				m_filterFlids[flidMain] = new TestItemAtomicFlid(flidRel);
			}
		}

		private int Flid(string field)
		{
			var parts = field.Trim().Split('.');
			if (parts.Length != 2)
			{
				throw new ArgumentException("Expected class.field but got " + field);
			}
			return MetaDataCache.GetFieldId(parts[0], parts[1], true);

		}

		/// <summary>
		/// Override to filter the specified properties.
		/// </summary>
		public override int get_VecItem(int hvo, int tag, int index)
		{
			ITestItem tester;
			if (!m_filterFlids.TryGetValue(tag, out tester))
			{
				return base.get_VecItem(hvo, tag, index);
			}
			var iresult = 0;
			foreach (var candidateHvo in BaseSda.VecProp(hvo, tag))
			{
				if (!tester.Test(candidateHvo, BaseSda, m_validHvos))
				{
					continue;
				}
				if (iresult == index)
				{
					return candidateHvo;
				}
				iresult++;
			}
			throw new IndexOutOfRangeException($"filtered vector does not contain that many items (wanted {index} but have only {iresult})");
		}

		/// <summary>
		/// Override to filter the specified properties.
		/// </summary>
		public override int get_VecSize(int hvo, int tag)
		{
			ITestItem tester;
			return !m_filterFlids.TryGetValue(tag, out tester) ? base.get_VecSize(hvo, tag) : BaseSda.VecProp(hvo, tag).Count(candidateHvo => tester.Test(candidateHvo, BaseSda, m_validHvos));
		}

		/// <summary>
		/// Override to filter the specified properties.
		/// </summary>
		public override void VecProp(int hvo, int tag, int chvoMax, out int chvo, ArrayPtr rghvo)
		{
			ITestItem tester;
			if (!m_filterFlids.TryGetValue(tag, out tester))
			{
				base.VecProp(hvo, tag, chvoMax, out chvo, rghvo);
				return;
			}
			var results = new List<int>(chvoMax);
			results.AddRange(BaseSda.VecProp(hvo, tag).Where(candidateHvo => tester.Test(candidateHvo, BaseSda, m_validHvos)));
			chvo = results.Count;
			MarshalEx.ArrayToNative(rghvo, chvoMax, results.ToArray());
		}
	}
}