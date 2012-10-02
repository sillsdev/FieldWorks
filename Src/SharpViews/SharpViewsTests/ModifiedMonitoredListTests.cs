using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Utilities;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	/// <summary>
	/// Tests the ModifiedMonitoredList
	/// </summary>
	[TestFixture]
	public class ModifiedMonitoredListTests
	{
		[Test]
		public void RaiseChangedTest()
		{
			ModifiedMonitoredList<MockParagraph> modifiedList = new ModifiedMonitoredList<MockParagraph>();
			List<MockParagraph> list = new List<MockParagraph>();
			modifiedList.Changed += RaiseParagraphsChanged;

			MockParagraph para1 = new MockParagraph("Para One");
			MockParagraph para2 = new MockParagraph("Para Two");
			MockParagraph para3 = new MockParagraph("Para Three");
			MockParagraph para4 = new MockParagraph("Para Four");
			MockParagraph para5 = new MockParagraph("Para Five");

			list.Add(para1);
			modifiedList.Add(para1);
			Assert.That(paragraphChangedEvent.FirstChange == 0);
			Assert.That(paragraphChangedEvent.NumberAdded == 1);
			Assert.That(paragraphChangedEvent.NumberDeleted == 0);
			Assert.That(CompareLists(list, modifiedList));

			list.Add(para3);
			modifiedList.Add(para3);
			Assert.That(paragraphChangedEvent.FirstChange == 1);
			Assert.That(paragraphChangedEvent.NumberAdded == 1);
			Assert.That(paragraphChangedEvent.NumberDeleted == 0);
			Assert.That(CompareLists(list, modifiedList));

			list.Add(para5);
			modifiedList.Add(para5);
			Assert.That(paragraphChangedEvent.FirstChange == 2);
			Assert.That(paragraphChangedEvent.NumberAdded == 1);
			Assert.That(paragraphChangedEvent.NumberDeleted == 0);
			Assert.That(CompareLists(list, modifiedList));

			list.Insert(1, para2);
			modifiedList.Insert(1, para2);
			Assert.That(paragraphChangedEvent.FirstChange == 1);
			Assert.That(paragraphChangedEvent.NumberAdded == 1);
			Assert.That(paragraphChangedEvent.NumberDeleted == 0);
			Assert.That(CompareLists(list, modifiedList));

			list.Insert(3, para4);
			modifiedList.Insert(3, para4);
			Assert.That(paragraphChangedEvent.FirstChange == 3);
			Assert.That(paragraphChangedEvent.NumberAdded == 1);
			Assert.That(paragraphChangedEvent.NumberDeleted == 0);
			Assert.That(CompareLists(list, modifiedList));

			list[2] = para2;
			modifiedList[2] = para2;
			Assert.That(paragraphChangedEvent.FirstChange == 2);
			Assert.That(paragraphChangedEvent.NumberAdded == 1);
			Assert.That(paragraphChangedEvent.NumberDeleted == 1);
			Assert.That(CompareLists(list, modifiedList));

			list.Remove(para4);
			modifiedList.Remove(para4);
			Assert.That(paragraphChangedEvent.FirstChange == 3);
			Assert.That(paragraphChangedEvent.NumberAdded == 0);
			Assert.That(paragraphChangedEvent.NumberDeleted == 1);
			Assert.That(CompareLists(list, modifiedList));

			list.RemoveAt(1);
			modifiedList.RemoveAt(1);
			Assert.That(paragraphChangedEvent.FirstChange == 1);
			Assert.That(paragraphChangedEvent.NumberAdded == 0);
			Assert.That(paragraphChangedEvent.NumberDeleted == 1);
			Assert.That(CompareLists(list, modifiedList));

			modifiedList.SimulateChange(0, 2);
			Assert.That(paragraphChangedEvent.FirstChange == 0);
			Assert.That(paragraphChangedEvent.NumberAdded == 2);
			Assert.That(paragraphChangedEvent.NumberDeleted == 2);
			Assert.That(CompareLists(list, modifiedList));

			list.Clear();
			modifiedList.Clear();
			Assert.That(paragraphChangedEvent.FirstChange == 0);
			Assert.That(paragraphChangedEvent.NumberAdded == 0);
			Assert.That(paragraphChangedEvent.NumberDeleted == 3);
			Assert.That(CompareLists(list, modifiedList));

		}

		private ObjectSequenceEventArgs paragraphChangedEvent;

		private void RaiseParagraphsChanged(object obj, ObjectSequenceEventArgs args)
		{
			paragraphChangedEvent = new ObjectSequenceEventArgs(args.FirstChange, args.NumberAdded, args.NumberDeleted);
		}

		class MockParagraph
		{
			public string Text;
			public MockParagraph(string text)
			{
				Text = text;
			}
		}

		private bool CompareLists(List<MockParagraph> list, ModifiedMonitoredList<MockParagraph> modifiedList)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i] != modifiedList[i])
					return false;
			}
			return true;
		}
	}
}
