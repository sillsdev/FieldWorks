using System;
using System.Linq;
using SIL.WritingSystems;

namespace SIL.CoreImpl
{
	/// <summary>
	/// A memory-based writing system store.
	/// </summary>
	public class MemoryWritingSystemRepository : LocalWritingSystemRepositoryBase<CoreWritingSystemDefinition>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MemoryWritingSystemRepository"/> class.
		/// </summary>
		public MemoryWritingSystemRepository(IWritingSystemRepository<CoreWritingSystemDefinition> globalRepository = null)
			: base(globalRepository)
		{
		}

		/// <summary>
		/// Creates the writing system factory.
		/// </summary>
		/// <returns></returns>
		protected override IWritingSystemFactory<CoreWritingSystemDefinition> CreateWritingSystemFactory()
		{
			return new CoreWritingSystemFactory();
		}

		/// <summary>
		/// This is used by the orphan finder, which we don't use (yet). It tells whether, typically in the scope of some
		/// current change log, a writing system ID has changed to something else...call WritingSystemIdHasChangedTo
		/// to find out what.
		/// </summary>
		public override bool WritingSystemIdHasChanged(string id)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// This is used by the orphan finder, which we don't use (yet). It tells what, typically in the scope of some
		/// current change log, a writing system ID has changed to.
		/// </summary>
		public override string WritingSystemIdHasChangedTo(string id)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Saves this instance.
		/// </summary>
		public override void Save()
		{
			foreach (string id in AllWritingSystems.Where(ws => ws.MarkedForDeletion).Select(ws => ws.Id).ToArray())
				Remove(id);

			foreach (CoreWritingSystemDefinition ws in AllWritingSystems.Where(CanSet).ToArray())
			{
				Set(ws);
				ws.AcceptChanges();
				OnChangeNotifySharedStore(ws);
			}

			if (Properties.Settings.Default.UpdateGlobalWSStore)
				base.Save();
		}
	}
}
