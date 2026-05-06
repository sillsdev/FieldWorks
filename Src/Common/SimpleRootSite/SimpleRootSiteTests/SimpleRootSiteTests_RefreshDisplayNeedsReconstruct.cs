// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using Moq;
using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	internal class RefreshDisplayDummyRootSite : DummyRootSite
	{
		public bool RefreshPending => m_fRefreshPending;

		public void SetRootBoxDataAccessForTest(ISilDataAccess dataAccess)
		{
			SetRootBoxDataAccess(dataAccess);
		}

		public void SetRootBoxDataAccessAndRefreshForTest(ISilDataAccess dataAccess)
		{
			SetRootBoxDataAccessAndRefresh(dataAccess);
		}

		public void NotifyDataAccessSemanticsChangedForTest()
		{
			NotifyDataAccessSemanticsChanged();
		}

		protected override SelectionRestorer CreateSelectionRestorer()
		{
			return null;
		}
	}

	[TestFixture]
	public class RefreshDisplayNeedsReconstructTests
	{
		private RefreshDisplayDummyRootSite m_site;
		private Mock<IVwRootBox> m_rootbMock;
		private Form m_form;

		[SetUp]
		public void Setup()
		{
			m_site = new RefreshDisplayDummyRootSite();
			m_rootbMock = new Mock<IVwRootBox>(MockBehavior.Strict);
			m_form = new Form();
			m_form.Controls.Add(m_site);
			m_site.Dock = DockStyle.Fill;
			m_form.Show();
			m_site.CreateControl();

			m_rootbMock.SetupGet(rb => rb.Site).Returns(m_site);
			m_rootbMock.SetupGet(rb => rb.DataAccess).Returns((ISilDataAccess)null);
			m_rootbMock.Setup(rb => rb.LoseFocus()).Returns(true);
			m_rootbMock.Setup(rb => rb.Activate(It.IsAny<VwSelectionState>()));
			m_rootbMock.Setup(rb => rb.Close());
			m_site.RootBox = m_rootbMock.Object;
		}

		[TearDown]
		public void TearDown()
		{
			m_form?.Close();
			m_form?.Dispose();
			m_site?.Dispose();
		}

		[Test]
		public void RefreshDisplay_SkipsReconstruct_WhenRootBoxDoesNotNeedReconstruct()
		{
			m_rootbMock.SetupGet(rb => rb.NeedsReconstruct).Returns(false);

			Assert.That(m_site.RefreshDisplay(), Is.False);
			Assert.That(m_site.RefreshPending, Is.False);
			m_rootbMock.Verify(rb => rb.Reconstruct(), Times.Never);
		}

		[Test]
		public void RefreshDisplay_Reconstructs_WhenRootBoxNeedsReconstruct()
		{
			m_rootbMock.SetupGet(rb => rb.NeedsReconstruct).Returns(true);
			m_rootbMock.Setup(rb => rb.Reconstruct());

			Assert.That(m_site.RefreshDisplay(), Is.False);
			Assert.That(m_site.RefreshPending, Is.False);
			m_rootbMock.Verify(rb => rb.Reconstruct(), Times.Once);
		}

		[Test]
		public void NotifyDataAccessSemanticsChanged_Reconstructs_WhenRootBoxDoesNotNeedReconstruct()
		{
			m_rootbMock.SetupGet(rb => rb.NeedsReconstruct).Returns(false);
			m_rootbMock.Setup(rb => rb.Reconstruct());

			m_site.NotifyDataAccessSemanticsChangedForTest();

			Assert.That(m_site.RefreshPending, Is.False);
			m_rootbMock.Verify(rb => rb.Reconstruct(), Times.Once);
		}

		[Test]
		public void NotifyDataAccessSemanticsChanged_DefersUntilVisible()
		{
			m_rootbMock.SetupGet(rb => rb.NeedsReconstruct).Returns(false);
			m_rootbMock.Setup(rb => rb.Reconstruct());
			m_site.Visible = false;

			m_site.NotifyDataAccessSemanticsChangedForTest();

			Assert.That(m_site.RefreshPending, Is.True);
			m_rootbMock.Verify(rb => rb.Reconstruct(), Times.Never);

			m_site.Visible = true;
			Assert.That(m_site.RefreshDisplay(), Is.False);
			Assert.That(m_site.RefreshPending, Is.False);
			m_rootbMock.Verify(rb => rb.Reconstruct(), Times.Once);
		}

		[Test]
		public void SetRootBoxDataAccess_DoesNotReconstruct_WhenSwapIsCheap()
		{
			var replacementDataAccess = Mock.Of<ISilDataAccess>();
			m_rootbMock.SetupSet(rb => rb.DataAccess = replacementDataAccess);
			m_rootbMock.SetupGet(rb => rb.NeedsReconstruct).Returns(false);

			m_site.SetRootBoxDataAccessForTest(replacementDataAccess);

			Assert.That(m_site.RefreshPending, Is.False);
			m_rootbMock.VerifySet(rb => rb.DataAccess = replacementDataAccess, Times.Once);
			m_rootbMock.Verify(rb => rb.Reconstruct(), Times.Never);
		}

		[Test]
		public void SetRootBoxDataAccessAndRefresh_Reconstructs_WhenSwapChangesDisplaySemantics()
		{
			var replacementDataAccess = Mock.Of<ISilDataAccess>();
			m_rootbMock.SetupSet(rb => rb.DataAccess = replacementDataAccess);
			m_rootbMock.SetupGet(rb => rb.NeedsReconstruct).Returns(false);
			m_rootbMock.Setup(rb => rb.Reconstruct());

			m_site.SetRootBoxDataAccessAndRefreshForTest(replacementDataAccess);

			Assert.That(m_site.RefreshPending, Is.False);
			m_rootbMock.VerifySet(rb => rb.DataAccess = replacementDataAccess, Times.Once);
			m_rootbMock.Verify(rb => rb.Reconstruct(), Times.Once);
		}
	}
}
