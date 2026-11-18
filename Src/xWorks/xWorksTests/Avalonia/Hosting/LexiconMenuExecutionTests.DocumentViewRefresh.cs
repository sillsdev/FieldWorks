// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwUtils;
using static SIL.FieldWorks.Common.FwUtils.FwUtils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// A move within a sequence asks a document view in the window to regenerate, so a
	/// preview beside the detail view shows the new order.
	/// </summary>
	public partial class LexiconMenuExecutionTests
	{
		[Test]
		public void MoveDetailObject_AsksADocumentViewToRefresh_OnlyWhenTheObjectMoved()
		{
			var requests = 0;
			Action<object> countRequest = request => requests++;
			Subscriber.Subscribe(EventConstants.RefreshCurrentView, countRequest, m_propertyTable.GetWindow());
			try
			{
				// SUT
				var moved = m_view.MoveDetailObject(PronunciationRow(), up: true);

				Assert.That(moved, Is.True);
				Assert.That(requests, Is.EqualTo(1));

				// SUT: the pronunciation is first now, so it cannot move up again
				DrainMediatorAndIdleQueues();
				moved = m_view.MoveDetailObject(PronunciationRow(), up: true);

				Assert.That(moved, Is.False);
				Assert.That(requests, Is.EqualTo(1), "nothing moved, so there is nothing to regenerate");
			}
			finally
			{
				Subscriber.Unsubscribe(EventConstants.RefreshCurrentView, countRequest);
			}
		}

		private DetailField PronunciationRow()
			=> Rows(m_entry).Single(f => f.Field == "Form" && f.ObjectHvo == m_pronunciation2.Hvo);
	}
}
