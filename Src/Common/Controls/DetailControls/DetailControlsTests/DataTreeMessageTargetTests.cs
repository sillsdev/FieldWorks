// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using NUnit.Framework;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Message targeting for a hidden <see cref="DataTree"/>: it stays out of the colleague
	/// chain, so a tree its host has hidden answers no command.
	/// </summary>
	[TestFixture]
	public class DataTreeMessageTargetTests
	{
		private DataTree m_dtree;
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private Form m_parent;

		[SetUp]
		public void SetUp()
		{
			m_dtree = new DataTree();
			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
			m_dtree.Init(m_mediator, m_propertyTable, null);
			// Parented but never shown.
			m_parent = new Form();
			m_parent.Controls.Add(m_dtree);
		}

		[TearDown]
		public void TearDown()
		{
			m_parent?.Dispose();
			m_parent = null;
			m_dtree = null; // owned by the form
			m_propertyTable?.Dispose();
			m_propertyTable = null;
			m_mediator?.Dispose();
			m_mediator = null;
		}

		[Test]
		public void HiddenTree_WithNoCurrentSlice_IsNotAMessageTarget()
		{
			Assert.That(m_dtree.Visible, Is.False, "precondition: the tree is not shown");

			Assert.That(m_dtree.GetMessageTargets(), Is.Empty,
				"a hidden tree keeps out of the colleague chain");
		}
	}
}
