using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Tests the basic generated code for atomic properties. There should be a lot more, and maybe are somewhere,
	/// but I can't find them.
	/// </summary>
	[TestFixture]
	public class AtomicPropertyTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Moving an object from elsewhere to an owning atomic property should mark the object as modified.
		/// </summary>
		[Test]
		public void MovingToAtomicOwnedObject_MarksMovedObjectModified()
		{
			var text = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			var origRecord = Cache.ServiceLocator.GetInstance<IRnGenericRecFactory>().Create();
			Cache.LangProject.ResearchNotebookOA.RecordsOC.Add(origRecord);
			origRecord.ConclusionsOA = text;
			var newRecord = Cache.ServiceLocator.GetInstance<IRnGenericRecFactory>().Create();
			Cache.LangProject.ResearchNotebookOA.RecordsOC.Add(newRecord);
			m_actionHandler.EndUndoTask();

			UndoableUnitOfWorkHelper.Do("undoIt", "redoIt", m_actionHandler, () => newRecord.ConclusionsOA = text);
			var uowService = Cache.ServiceLocator.GetInstance<IUnitOfWorkService>();
			HashSet<ICmObjectId> newbies = new HashSet<ICmObjectId>();
			HashSet<ICmObjectOrSurrogate> dirtballs = new HashSet<ICmObjectOrSurrogate>();
			HashSet<ICmObjectId> goners = new HashSet<ICmObjectId>();
			uowService.GatherChanges(newbies,dirtballs, goners);

			Assert.That(dirtballs.Contains((ICmObjectOrSurrogate)text));
		}
	}
}
