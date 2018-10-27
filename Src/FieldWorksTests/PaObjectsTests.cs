// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using NUnit.Framework;
using SIL.FieldWorks.PaObjects;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.PaToFdoInterfaces;

namespace SIL.FieldWorks
{
	/// <summary>
	/// Unit tests for the PaObjects (Phonology Assistant model objects)
	/// </summary>
	public class PaObjectsTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private int _enWsId;

		/// <summary />
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			var enWs = Cache.WritingSystemFactory.get_Engine("en");
			Debug.Assert(enWs != null, "enWs != null");
			_enWsId = enWs.Handle;
		}

		/// <summary />
		[Test]
		public void PaLexEntry_EtymologyEmptyWorks()
		{
			var entry = CreateLexEntry();
			// SUT
			var paEntry = new PaLexEntry(entry);
			Assert.Null(paEntry.Etymology);
		}

		/// <summary />
		[Test]
		public void PaLexEntry_EtymologySingleItemWorks()
		{
			var entry = CreateLexEntry();
			var etymology = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
			entry.EtymologyOS.Add(etymology);
			var firstForm = TsStringUtils.MakeString("FirstForm", _enWsId);
			etymology.Form.set_String(_enWsId, firstForm);
			// SUT
			var paEntry = new PaLexEntry(entry);
			Assert.NotNull(paEntry.Etymology);
			Assert.That(((PaMultiString)paEntry.Etymology).Texts.Contains(firstForm.Text));
		}

		private ILexEntry CreateLexEntry()
		{
			var factory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			return factory.Create();
		}

		/// <summary />
		[Test]
		public void PaLexEntry_EtymologyMultipleItemsWorks()
		{
			var entry = CreateLexEntry();
			var etymology = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
			entry.EtymologyOS.Add(etymology);
			var firstForm = TsStringUtils.MakeString("FirstForm", _enWsId);
			etymology.Form.set_String(_enWsId, firstForm);

			etymology = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
			entry.EtymologyOS.Add(etymology);
			var secondForm = TsStringUtils.MakeString("SecondForm", _enWsId);
			etymology.Form.set_String(_enWsId, secondForm);
			// SUT
			IPaLexEntry paEntry = new PaLexEntry(entry);
			Assert.NotNull(paEntry.Etymology);
			Assert.That(((PaMultiString)paEntry.Etymology).Texts.Contains(firstForm.Text + ", " + secondForm.Text));
		}
	}
}
