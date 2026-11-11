// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.LCModel;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class WritingSystemListHandlerTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void Init_SeedsTheDefaultAnalysisWritingSystemWithoutQueueingAChange()
		{
			using (var mediator = new Mediator())
			using (var propertyTable = new PropertyTable(mediator))
			using (var handler = new WritingSystemListHandler())
			{
				propertyTable.SetProperty("cache", Cache, false);
				propertyTable.SetPropertyDispose("cache", false);

				handler.Init(mediator, propertyTable, null);

				Assert.That(mediator.JobItems, Is.Zero,
					"the combo reads the value when the toolbar next updates");
				Assert.That(propertyTable.GetStringProperty("WritingSystemHvo", null),
					Is.EqualTo(Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle.ToString()));
			}
		}
	}
}
