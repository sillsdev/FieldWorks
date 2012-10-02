using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// This decorator modifies certain sequence properties by filtering from them items not in a master list.
	/// </summary>
	public class FilterSdaDecorator : DomainDataByFlidDecoratorBase
	{
		private int m_mainFlid;
		private int m_hvoRoot;
		private Dictionary<int, ITestItem> m_filterFlids = new Dictionary<int, ITestItem>();
		Set<int> m_validHvos = new Set<int>();

		/// <summary>
		/// Make one that wraps the specified cache and passes items in the specified property of the specified root object.
		/// </summary>
		public FilterSdaDecorator(ISilDataAccessManaged domainDataByFlid, int mainFlid, int hvoRoot)
			: base(domainDataByFlid)
		{
			m_mainFlid = mainFlid;
			m_hvoRoot = hvoRoot;
			int chvoReal = BaseSda.get_VecSize(m_hvoRoot, m_mainFlid);
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(chvoReal))
			{
				BaseSda.VecProp(m_hvoRoot, m_mainFlid, chvoReal, out chvoReal, arrayPtr);
				m_validHvos = new Set<int>(MarshalEx.NativeToArray<int>(arrayPtr, chvoReal));
			}
		}

		/// <summary>
		/// Set the filter flids from a string that contains semi-colon-separated sequence of Class.Field strings.
		/// </summary>
		/// <param name="input"></param>
		public void SetFilterFlids(string input)
		{
			foreach (string field in input.Split(';'))
			{
				string[] parts = field.Trim().Split(':');
				if (parts.Length != 2)
				   throw new ArgumentException("Expected sequence of class.field:class.field;class.field:class.field but got " + input);
				int flidMain = Flid(parts[0]);
				int flidRel = Flid(parts[1]);
				m_filterFlids[flidMain] = new TestItemAtomicFlid(flidRel);
			}
		}

		int Flid(string field)
		{
			string[] parts = field.Trim().Split('.');
			if (parts.Length != 2)
				throw new ArgumentException("Expected class.field but got " + field);
			return(int)MetaDataCache.GetFieldId(parts[0], parts[1], true);

		}

		/// <summary>
		/// Override to filter the specified properties.
		/// </summary>
		public override int get_VecItem(int hvo, int tag, int index)
		{
			ITestItem tester;
			if (!m_filterFlids.TryGetValue(tag, out tester))
				return base.get_VecItem(hvo, tag, index);
			int chvoReal = BaseSda.get_VecSize(hvo, tag);
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(chvoReal))
			{
				BaseSda.VecProp(hvo, tag, chvoReal, out chvoReal, arrayPtr);
				int[] candidates = MarshalEx.NativeToArray<int>(arrayPtr, chvoReal);
				int iresult = 0;
				for (int icandidate = 0; icandidate < candidates.Length; icandidate++)
				{
					if (tester.Test(candidates[icandidate], BaseSda, m_validHvos))
					{
						if (iresult == index)
							return candidates[icandidate];
						iresult++;
					}
				}
				throw new IndexOutOfRangeException("filtered vector does not contain that many items (wanted " + index +
												  " but have only " + iresult + ")");
			}
		}

		/// <summary>
		/// Override to filter the specified properties.
		/// </summary>
		public override int get_VecSize(int hvo, int tag)
		{
			ITestItem tester;
			if (!m_filterFlids.TryGetValue(tag, out tester))
				return base.get_VecSize(hvo, tag);
			int chvoReal = BaseSda.get_VecSize(hvo, tag);
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(chvoReal))
			{
				BaseSda.VecProp(hvo, tag, chvoReal, out chvoReal, arrayPtr);
				int[] candidates = MarshalEx.NativeToArray<int>(arrayPtr, chvoReal);
				int iresult = 0;
				for (int icandidate = 0; icandidate < candidates.Length; icandidate++)
				{
					if (tester.Test(candidates[icandidate], BaseSda, m_validHvos))
						iresult++;
				}
				return iresult;
			}
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
			int chvoReal = BaseSda.get_VecSize(hvo, tag);
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(chvoReal))
			{
				BaseSda.VecProp(hvo, tag, chvoReal, out chvoReal, arrayPtr);
				int[] candidates = MarshalEx.NativeToArray<int>(arrayPtr, chvoReal);
				int[] results = new int[chvoMax];
				int iresult = 0;
				for (int icandidate = 0; icandidate < candidates.Length; icandidate++)
				{
					if (tester.Test(candidates[icandidate], BaseSda, m_validHvos))
						results[iresult++] = candidates[icandidate];
				}
				chvo = iresult;
				MarshalEx.ArrayToNative(rghvo, chvoMax, results);
			}
		}
	}

	/// <summary>
	/// Test an item in a filtered property to see whether it should be included.
	/// </summary>
	interface ITestItem
	{
		bool Test(int hvo, ISilDataAccess sda, Set<int> validHvos);
	}

	/// <summary>
	/// Test an item by following the specified flid (an atomic object property).
	/// Pass if the destination is in the validHvos set.
	/// </summary>
	class TestItemAtomicFlid : ITestItem
	{
		private int m_flid;
		public TestItemAtomicFlid(int flid)
		{
			m_flid = flid;
		}
		public bool Test(int hvo, ISilDataAccess sda, Set<int> validHvos)
		{
			return validHvos.Contains(sda.get_ObjectProp(hvo, m_flid));
		}
	}
}
