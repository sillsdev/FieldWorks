// Copyright (c) 2022-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.AlloGenService;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.AlloGenServiceTest
{
	[TestFixture]
	class AllomorphCreatorTests : FwTestBase
	{
		protected AllomorphCreator ac;

		[SetUp]
		override public void Setup()
		{
			base.Setup();
			ac = new AllomorphCreator(Cache, writingSystems);
		}

		[Test]
		public void AddAllomorphToEntryTest()
		{
			Assert.NotNull(myCache);
			// following gives a "not in the right state to register a change" message
			// Not sure what to do; we'd have to manuall create an entry with the five vernacular writing systems
			//NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor, () =>
			//{
			//    ILexEntry entry = pm.SingleAllomorphs.ElementAt(96);
			//    Assert.NotNull(entry);
			//    Assert.AreEqual(0, entry.AlternateFormsOS.Count);
			//    Console.WriteLine("entry cf='" + entry.CitationForm.VernacularDefaultWritingSystem.Text);
			//    string akh = "chillinyakh";
			//    string acl = "chillinyacl";
			//    string akl = "chillinyakl";
			//    string ach = "chillinyach";
			//    string ame = "chillinyaa";
			//    IMoStemAllomorph form = ac.CreateAllomorph(entry, akh, acl, akl, ach, ame);
			//    Assert.NotNull(form);
			//    Assert.AreEqual(1, entry.AlternateFormsOS.Count);
			//    Assert.AreEqual(akh, form.Form.get_String(wsForAkh));
			//    Assert.AreEqual(acl, form.Form.get_String(wsForAcl));
			//    Assert.AreEqual(akl, form.Form.get_String(wsForAkl));
			//    Assert.AreEqual(ach, form.Form.get_String(wsForAch));
			//    Assert.AreEqual(ame, form.Form.get_String(wsForAme));

			//});
		}
	}
}
