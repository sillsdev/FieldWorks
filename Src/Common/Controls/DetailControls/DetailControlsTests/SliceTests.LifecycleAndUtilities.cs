// Copyright (c) 2025 SIL International
// This software is licensed under the LGPL, version 2.1 or later.
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public partial class SliceTests
	{
		#region Characterization Tests — Lifecycle

		[Test]
		public void Constructor_SetsVisibleFalse()
		{
			using (var slice = new Slice())
			{
				Assert.That(slice.Visible, Is.False,
					"New slices should start invisible");
			}
		}

		[Test]
		public void CheckDisposed_AfterDispose_Throws()
		{
			var slice = new Slice();
			slice.Dispose();

			Assert.Throws<ObjectDisposedException>(() => slice.CheckDisposed());
		}

		#endregion

		#region Characterization Tests — Expansion

		[Test]
		public void Expansion_DefaultIsFixed()
		{
			using (var slice = new Slice())
			{
				Assert.That(slice.Expansion, Is.EqualTo(DataTree.TreeItemState.ktisFixed),
					"Default expansion should be Fixed");
			}
		}

		[Test]
		public void ExpansionStateKey_NullForFixedSlices()
		{
			using (var slice = new Slice())
			{
				Assert.That(slice.ExpansionStateKey, Is.Null,
					"Fixed slices should have null ExpansionStateKey");
			}
		}

		[Test]
		public void ExpansionStateKey_NonNullForExpandedWithObject()
		{
			m_DataTree = new DataTree();
			m_Slice = GenerateSlice(Cache, m_DataTree);
			var obj = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			m_Slice.Object = obj;
			m_Slice.Expansion = DataTree.TreeItemState.ktisExpanded;

			Assert.That(m_Slice.ExpansionStateKey, Is.Not.Null,
				"Expanded slice with an object should have a non-null ExpansionStateKey");
			Assert.That(m_Slice.ExpansionStateKey, Does.StartWith("expand"),
				"ExpansionStateKey should start with 'expand'");
		}

		#endregion

		#region Characterization Tests — Static Utilities

		[Test]
		public void StartsWith_BoxedIntEquality()
		{
			var target = new object[] { 1, 2, 3, "extra" };
			var match = new object[] { 1, 2, 3 };

			Assert.That(Slice.StartsWith(target, match), Is.True,
				"StartsWith should handle boxed int equality");
		}

		[Test]
		public void StartsWith_MatchLongerThanTarget_ReturnsFalse()
		{
			var target = new object[] { 1, 2 };
			var match = new object[] { 1, 2, 3 };

			Assert.That(Slice.StartsWith(target, match), Is.False);
		}

		[Test]
		public void StartsWith_MismatchedElements_ReturnsFalse()
		{
			var target = new object[] { 1, 2, 3 };
			var match = new object[] { 1, 99, 3 };

			Assert.That(Slice.StartsWith(target, match), Is.False);
		}

		[Test]
		public void ExtraIndent_TrueAttribute_ReturnsOne()
		{
			var node = CreateXmlElementFromOuterXmlOf("<indent indent=\"true\" />");
			Assert.That(Slice.ExtraIndent(node), Is.EqualTo(1));
		}

		[Test]
		public void ExtraIndent_NoAttribute_ReturnsZero()
		{
			var node = CreateXmlElementFromOuterXmlOf("<indent />");
			Assert.That(Slice.ExtraIndent(node), Is.EqualTo(0));
		}

		#endregion

		#region Characterization Tests — Weight

		[Test]
		public void Weight_SetAndGet()
		{
			using (var slice = new Slice())
			{
				slice.Weight = ObjectWeight.heavy;
				Assert.That(slice.Weight, Is.EqualTo(ObjectWeight.heavy));

				slice.Weight = ObjectWeight.light;
				Assert.That(slice.Weight, Is.EqualTo(ObjectWeight.light));

				slice.Weight = ObjectWeight.field;
				Assert.That(slice.Weight, Is.EqualTo(ObjectWeight.field));
			}
		}

		#endregion
	}
}
