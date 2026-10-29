// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.XWorks.LexEd;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using XCore;

namespace LexEdDllTests
{
	[TestFixture]
	public class ReversalListenerTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void Init_NoReversalIndexSelected_SelectsTheFirstIndexOnceItsTaskHasEnded()
		{
			var repository = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			IReversalIndex index = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => index = repository.FindOrCreateIndexForWs(Cache.DefaultAnalWs));
			using (var mediator = new Mediator())
			using (var propertyTable = new PropertyTable(mediator))
			using (var listener = new ReversalListener())
			{
				propertyTable.SetProperty("cache", Cache, false);
				propertyTable.SetPropertyDispose("cache", false);

				listener.Init(mediator, propertyTable, null);

				Assert.That(propertyTable.GetStringProperty("ReversalIndexGuid", null),
					Is.EqualTo(index.Guid.ToString()));
				Assert.That(Cache.ActionHandlerAccessor.CurrentDepth, Is.EqualTo(0),
					"Init ends the task it opened");
			}
		}

		[Test]
		public void Init_NoReversalIndexSelected_SelectsTheFirstIndexWithoutQueueingAChange()
		{
			var repository = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			IReversalIndex index = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => index = repository.FindOrCreateIndexForWs(Cache.DefaultAnalWs));
			using (var mediator = new Mediator())
			using (var propertyTable = new PropertyTable(mediator))
			using (var listener = new ReversalListener())
			{
				propertyTable.SetProperty("cache", Cache, false);
				propertyTable.SetPropertyDispose("cache", false);
				// Init reads it with a default; an existing empty value means no index.
				propertyTable.SetProperty("ReversalIndexGuid", "", false);

				listener.Init(mediator, propertyTable, null);

				Assert.That(mediator.JobItems, Is.Zero,
					"the first index is chosen before any clerk exists to hear the change");
				Assert.That(propertyTable.GetStringProperty("ReversalIndexGuid", null),
					Is.EqualTo(index.Guid.ToString()));
			}
		}
	}
}
