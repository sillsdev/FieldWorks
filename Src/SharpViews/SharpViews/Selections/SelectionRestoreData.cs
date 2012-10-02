using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.SharpViews.Paragraphs;

namespace SIL.FieldWorks.SharpViews.Selections
{
	public interface ISelectionRestoreData
	{
		Selection RestoreSelection();
	}

	public class InsertionPointRestoreData : ISelectionRestoreData
	{
		private InsertionPoint StoredInsertionPoint;
		private List<int> Indexes = new List<int>();
		private int runIndex;

		public InsertionPointRestoreData(InsertionPoint ip)
		{
			StoredInsertionPoint = new InsertionPoint(ip.Hookup, ip.StringPosition, ip.AssociatePrevious);

			var parents = FindParents(StoredInsertionPoint.Para).Reverse();
			var childrenList = StoredInsertionPoint.RootBox.Children.ToList();
			foreach (GroupBox box in parents)
			{
				Indexes.Add(childrenList.IndexOf(box));
				childrenList = box.Children.ToList();
			}
			runIndex = StoredInsertionPoint.Para.Source.ClientRuns.IndexOf(StoredInsertionPoint.ContainingRun);
		}

		public Selection RestoreSelection()
		{
			Box box = StoredInsertionPoint.RootBox;
			foreach (int index in Indexes)
			{
				if (box is GroupBox)
				{
					List<Box> childrenList = ((GroupBox)box).Children.ToList();
					box = childrenList[index];
				}
			}
			return new InsertionPoint(((ParaBox)box).Source.ClientRuns[runIndex].Hookup, StoredInsertionPoint.StringPosition,
									  StoredInsertionPoint.AssociatePrevious);
		}

		private static IEnumerable<Box> FindParents(Box current)
		{
			while (true)
			{
				if (current is RootBox)
					break;
				yield return current;
				current = current.Container;
			}
		}
	}

	public class RangeRestoreData : ISelectionRestoreData
	{
		private InsertionPointRestoreData StartRestoreData;
		private InsertionPointRestoreData EndRestoreData;

		public RangeRestoreData(RangeSelection rangeSelection)
		{
			StartRestoreData = new InsertionPointRestoreData(rangeSelection.Start);
			EndRestoreData = new InsertionPointRestoreData(rangeSelection.End);
		}

		public Selection RestoreSelection()
		{
			return new RangeSelection((InsertionPoint)StartRestoreData.RestoreSelection(),
									  (InsertionPoint)EndRestoreData.RestoreSelection());
		}
	}
}
