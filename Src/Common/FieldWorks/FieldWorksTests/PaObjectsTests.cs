// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.PaObjects;

namespace SIL.FieldWorks
{
	/// <summary>
	/// Unit tests for the PaObjects (Phonology Assistant model objects)
	/// </summary>
	public class PaObjectsTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private int _enWsId;

		/// <summary/>
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			var enWs = Cache.WritingSystemFactory.get_Engine("en");
			Debug.Assert(enWs != null, "enWs != null");
			_enWsId = enWs.Handle;
		}

		/// <summary/>
		[Test]
		public void PaLexEntry_EtymologyEmptyWorks()
		{
			var entry = CreateLexEntry();
			// SUT
			var paEntry = new PaLexEntry(entry);
			Assert.Null(paEntry.xEtymology);
		}

		/// <summary/>
		[Test]
		public void PaLexEntry_EtymologySingleItemWorks()
		{
			var entry = CreateLexEntry();
			var etymology = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
			entry.EtymologyOS.Add(etymology);
			var firstForm = Cache.TsStrFactory.MakeString("FirstForm", _enWsId);
			etymology.Form.set_String(_enWsId, firstForm);
			// SUT
			var paEntry = new PaLexEntry(entry);
			Assert.NotNull(paEntry.xEtymology);
			Assert.That(paEntry.xEtymology.Texts.Contains(firstForm.Text));
		}

		private ILexEntry CreateLexEntry()
		{
			var factory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var entry = factory.Create();
			return entry;
		}

		/// <summary/>
		[Test]
		public void PaLexEntry_EtymologyMultipleItemsWorks()
		{
			var entry = CreateLexEntry();
			var etymology = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
			entry.EtymologyOS.Add(etymology);
			var firstForm = Cache.TsStrFactory.MakeString("FirstForm", _enWsId);
			etymology.Form.set_String(_enWsId, firstForm);

			etymology = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
			entry.EtymologyOS.Add(etymology);
			var secondForm = Cache.TsStrFactory.MakeString("SecondForm", _enWsId);
			etymology.Form.set_String(_enWsId, secondForm);
			// SUT
			var paEntry = new PaLexEntry(entry);
			Assert.NotNull(paEntry.xEtymology);
			Assert.That(paEntry.xEtymology.Texts.Contains(firstForm.Text + ", " + secondForm.Text));
		}
	}
}
