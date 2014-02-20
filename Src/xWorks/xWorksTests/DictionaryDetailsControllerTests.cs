// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class DictionaryDetailsControllerTests
	{
		// Sense tests

		[Test]
		public void SenseLoadsParagraphStyles()
		{

		}

		[Test]
		public void NonSenseLoadsCharacterStyles()
		{

		}

		// List tests (applicable to Writing System as well)

		[Test]
		public void CannotUncheckOnlyCheckedItemInList()
		{

		}

		[Test]
		public void CannotMoveTopItemUp()
		{

		}

		[Test]
		public void CannotMoveBottomItemDown()
		{

		}

		// Writing System tests:

		[Test]
		// REVIEW (Hasso) 2014.02: would we like to permit checking both defaults?  Default Anal + named Vernac?
		public void CheckDefaultWsUnchecksAllOthers()
		{

		}

		[Test]
		public void CheckNamedWsUnchecksDefault()
		{

		}

		[Test]
		public void CheckNamedWsPreservesOtherNamedWss()
		{

		}

		[Test]
		public void CannotReorderDefaultWs()
		{

		}

		[Test]
		public void CannotMoveNamedWsAboveDefault()
		{

		}
	}
}
