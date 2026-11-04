// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// Where a command that acts on the shown record applies, by the record's class and the
	/// tool the command is restricted to.
	/// </summary>
	[TestFixture]
	public class ShownRecordCommandScopeTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry _entry;

		[SetUp]
		public void MakeEntry()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => _entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create());
		}

		[TearDown]
		public void DeleteEntry()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				if (_entry.IsValidObject)
					_entry.Delete();
			});
		}

		[Test]
		public void Applies_ToARecordOfTheCommandsClass_InItsTool()
		{
			Assert.That(ShownRecordCommandScope.Applies(_entry, "LexEntry", "lexiconEdit", "lexiconEdit"), Is.True);
			Assert.That(ShownRecordCommandScope.Applies(_entry, "LexEntry", null, "notebookEdit"), Is.True, "no tool named");
			Assert.That(ShownRecordCommandScope.Applies(_entry, "LexEntry", "lexiconEdit", "lexiconBrowse"), Is.False);
			Assert.That(ShownRecordCommandScope.Applies(_entry, "LexSense", null, "lexiconEdit"), Is.False);
			Assert.That(ShownRecordCommandScope.Applies(null, "LexEntry", null, "lexiconEdit"), Is.False);
		}
	}
}
