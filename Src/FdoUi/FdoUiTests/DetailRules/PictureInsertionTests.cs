// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The Insert Picture rule of a detail row: the field that takes the picture, by the
	/// command's exact class, and the picture it adds.
	/// </summary>
	[TestFixture]
	public class PictureInsertionTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry _entry;
		private ILexSense _sense;
		private ICmPicture _existing;

		// An entry with a stem lexeme form and one sense, which has one picture.
		[SetUp]
		public void MakeEntry()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				_entry.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				_entry.SensesOS.Add(_sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create());
				_sense.PicturesOS.Add(_existing = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create());
			});
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
		public void TargetField_IsTheNamedField_OfAnObjectOfExactlyTheCommandsClass()
		{
			Assert.That(PictureInsertion.TargetField(_sense, "LexSense", "Pictures"), Is.EqualTo(LexSenseTags.kflidPictures));
			Assert.That(PictureInsertion.TargetField(_entry, "LexSense", "Pictures"), Is.EqualTo(0), "another class");
			Assert.That(PictureInsertion.TargetField(_entry.LexemeFormOA, "MoForm", "Form"), Is.EqualTo(0),
				"a subclass does not count");
		}

		[Test]
		public void TargetField_IsZero_ForAClassOrFieldTheModelLacks_AndForNoObject()
		{
			Assert.That(PictureInsertion.TargetField(_sense, "NoSuchClass", "Pictures"), Is.EqualTo(0));
			Assert.That(PictureInsertion.TargetField(_sense, "LexSense", "NoSuchField"), Is.EqualTo(0));
			Assert.That(PictureInsertion.TargetField(_sense, null, "Pictures"), Is.EqualTo(0));
			Assert.That(PictureInsertion.TargetField(null, "LexSense", "Pictures"), Is.EqualTo(0));
		}

		[Test]
		public void Insert_AddsAPictureOfTheFile_Last_InOneUndoableStep()
		{
			var file = Path.Combine(Path.GetTempPath(), "inserted-picture.jpg");

			var picture = PictureInsertion.Insert(_sense, LexSenseTags.kflidPictures, file, "Undo insert", "Redo insert");

			Assert.That(_sense.PicturesOS, Is.EqualTo(new[] { _existing, picture }));
			Assert.That(picture.PictureFileRA.InternalPath, Does.EndWith("inserted-picture.jpg"));
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo("Undo insert"));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_sense.PicturesOS, Is.EqualTo(new[] { _existing }), "one Undo removes it");
		}
	}
}
