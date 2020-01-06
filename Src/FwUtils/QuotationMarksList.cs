// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Encapsulates a list of QuotationMarks objects.
	/// </summary>
	[XmlType("QuotationMarks")]
	public class QuotationMarksList
	{
		/// <summary />
		[XmlAttribute("LangUsedFrom")]
		public string LocaleOfLangUsedFrom;

		/// <summary />
		[XmlAttribute]
		public ParagraphContinuationType ContinuationType = ParagraphContinuationType.RequireAll;

		/// <summary />
		[XmlAttribute]
		public ParagraphContinuationMark ContinuationMark = ParagraphContinuationMark.Opening;

		[XmlArray("Levels")]
		public List<QuotationMarks> QMarksList = new List<QuotationMarks>();

		/// <summary>
		/// Initializes and returns a new instance of the <see cref="T:QuotationMarksList"/> class.
		/// </summary>
		public static QuotationMarksList NewList()
		{
			var list = new QuotationMarksList();
			list.EnsureLevelExists(2);
			return list;
		}

		/// <summary>
		/// Creates a QuotationMarksList from the specified XML string.
		/// </summary>
		/// <param name="xmlSrc">The XML source string to load.</param>
		/// <param name="wsName">Name of the writing system (used for error reporting).</param>
		public static QuotationMarksList Load(string xmlSrc, string wsName)
		{
			Exception e;
			var list = XmlSerializationHelper.DeserializeFromString<QuotationMarksList>(xmlSrc, out e);
			if (e != null)
			{
				throw new ContinuableErrorException($"Invalid QuotationMarks field while loading the {wsName} writing system.", e);
			}

			return (list == null || list.Levels == 0 ? NewList() : list);
		}

		/// <summary>
		/// Gets the <see cref="T:SIL.FieldWorks.Common.FwUtils.QuotationMarks"/> with the specified index.
		/// </summary>
		public QuotationMarks this[int i] => (i < 0 || i >= QMarksList.Count ? null : QMarksList[i]);

		/// <summary>
		/// Adds the specified quotation mark object.
		/// </summary>
		public void Add(QuotationMarks item)
		{
			QMarksList.Add(item);
		}

		/// <summary>
		/// Removes the QuotationMarks object specified by the index i, from the list.
		/// </summary>
		public void RemoveAt(int i)
		{
			if (i >= 0 && i < QMarksList.Count)
			{
				QMarksList.RemoveAt(i);
			}
		}

		/// <summary>
		/// Removes the specified item from the list.
		/// </summary>
		public void Remove(QuotationMarks item)
		{
			QMarksList.Remove(item);
		}

		/// <summary>
		/// Removes the last item from the list.
		/// </summary>
		public void RemoveLastLevel()
		{
			if (QMarksList.Count > 0)
			{
				QMarksList.RemoveAt(QMarksList.Count - 1);
			}
		}

		/// <summary>
		/// Clears the list of quotation marks.
		/// </summary>
		public void Clear()
		{
			QMarksList.Clear();
		}

		/// <summary>
		/// Returns a copy of the list.
		/// </summary>
		public QuotationMarksList Copy()
		{
			var newList = new QuotationMarksList();
			for (var i = 0; i < Levels; i++)
			{
				var qm = new QuotationMarks
				{
					Opening = this[i].Opening,
					Closing = this[i].Closing
				};
				newList.Add(qm);
			}

			newList.ContinuationMark = ContinuationMark;
			newList.ContinuationType = ContinuationType;
			newList.LocaleOfLangUsedFrom = LocaleOfLangUsedFrom;
			return newList;
		}

		/// <summary>
		/// Ensures that the specified number of levels exists.
		/// </summary>
		public void EnsureLevelExists(int level)
		{
			while (Levels < level)
			{
				AddLevel();
			}
		}

		/// <summary>
		/// Gets a value indicating whether or not the list is equal to the specified list.
		/// </summary>
		public bool Equals(QuotationMarksList list, bool considerParaCont)
		{
			if (list == null || Levels != list.Levels)
			{
				return false;
			}
			if (considerParaCont && (ContinuationType != list.ContinuationType || ContinuationMark != list.ContinuationMark))
			{
				return false;
			}
			for (var i = 0; i < Levels; i++)
			{
				if (!this[i].Equals(list[i]))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Gets an XML string representing the list of quotation marks.
		/// </summary>
		public string XmlString
		{
			get
			{
				var list = TrimmedList;
				return (list.Levels == 0 ? null : XmlSerializationHelper.SerializeToString(TrimmedList));
			}
		}

		/// <summary>
		/// Gets the number of quotation levels.
		/// </summary>
		public int Levels => QMarksList.Count;

		/// <summary>
		/// Gets a copy of the list after having removed empty levels from the end.
		/// </summary>
		public QuotationMarksList TrimmedList
		{
			get
			{
				var tmpList = Copy();
				for (var i = tmpList.Levels - 1; i >= 0; i--)
				{
					if (i == (tmpList.Levels - 1) && tmpList.QMarksList[i].IsEmpty)
					{
						tmpList.RemoveLastLevel();
					}
				}

				return tmpList;
			}
		}

		/// <summary>
		/// Gets a value indicating whether or not the list is empty at all levels.
		/// </summary>
		public bool IsEmpty
		{
			get
			{
				foreach (var qm in QMarksList)
				{
					if (!qm.IsEmpty)
					{
						return false;
					}
				}

				return true;
			}
		}

		/// <summary>
		/// Finds the first level that is followed by a non empty level.
		/// </summary>
		public int FindGap()
		{
			var tmpList = TrimmedList;
			for (var i = 0; i < tmpList.Levels; i++)
			{
				if (i < tmpList.Levels - 1 && tmpList[i].IsEmpty && !tmpList[i + 1].IsEmpty)
				{
					return i + 1;
				}
			}

			return 0;
		}

		/// <summary>
		/// Gets the first quote mark levels in the list that have conflicting opener/closer
		/// combinations
		/// </summary>
		/// <remarks>See tests for handled cases</remarks>
		public InvalidComboInfo InvalidOpenerCloserCombinations
		{
			get
			{
				for (var i = 0; i < Levels; i++)
				{
					for (var j = 0; j < Levels; j++)
					{
						if (i == j || this[i].HasIdenticalOpenerAndCloser && this[j].HasIdenticalOpenerAndCloser && Math.Abs(i - j) != 1)
						{
							continue;
						}
						if (this[i].Opening.Equals(this[j].Closing, StringComparison.Ordinal))
						{
							return new InvalidComboInfo(i, true, j, false, this[i].Opening);
						}
						if (this[i].Closing.Equals(this[j].Opening, StringComparison.Ordinal))
						{
							return new InvalidComboInfo(i, false, j, true, this[i].Closing);
						}
					}
				}

				return null;
			}
		}

		/// <summary>
		/// Gets a value indicating whether or not the list contains a quote mark list with an
		/// empty opening and an empty closing mark.
		/// </summary>
		public bool AnyEmptyRows
		{
			get
			{
				for (var i = 0; i < Levels; i++)
				{
					if (QMarksList[i].IsEmpty)
					{
						return true;
					}
				}

				return false;
			}
		}

		/// <summary>
		/// Adds a new level (if there are fewer than three already) and initializes the
		/// paragraph continuation values in each level. This returns true when a level
		/// was added and false if the list is already full so no more levels can be added.
		/// </summary>
		public void AddLevel()
		{
			var distinctLevels = DistinctLevels;
			var qmarks = new QuotationMarks();
			if (distinctLevels > 1 && !AnyEmptyRows)
			{
				var icopyLev = (Levels % distinctLevels);
				qmarks.Opening = QMarksList[icopyLev].Opening;
				qmarks.Closing = QMarksList[icopyLev].Closing;
			}

			QMarksList.Add(qmarks);
		}

		/// <summary>
		/// Gets the number of unique levels that form a nested chain.
		/// </summary>
		public int DistinctLevels
		{
			get
			{
				if (Levels <= 2)
				{
					return Levels;
				}
				for (var lev = 2; lev < Levels; lev++)
				{
					var foundMatch = true;
					for (var i = 0; i < Levels; i++)
					{
						var iOther = i % lev;
						if (!QMarksList[i].Equals(QMarksList[iOther]))
						{
							foundMatch = false;
							break;
						}
					}
					if (foundMatch)
					{
						return lev;
					}
				}
				return Levels;
			}
		}
	}
}