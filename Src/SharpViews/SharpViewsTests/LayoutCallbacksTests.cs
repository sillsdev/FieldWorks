using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	/// <summary>
	/// Test the LayoutCallbacks class.
	/// </summary>
	[TestFixture]
	public class LayoutCallbacksTests
	{
		[Test]
		public void Invalidate()
		{
			var site = new MockSite();
			var root = new RootBox(new AssembledStyles());
			root.Site = site;
			using (var lc = new LayoutCallbacks(root))
			{
				lc.Invalidate(new Rectangle(10, 13, 17, 19));
				lc.Invalidate(new Rectangle(9, 8, 5, 4));
				Assert.That(site.RectsInvalidated, Is.Empty);
			}
			Assert.That(site.RectsInvalidated, Has.Member(new Rectangle(10, 13, 17, 19)));
			Assert.That(site.RectsInvalidated, Has.Member(new Rectangle(9, 8, 5, 4)));
		}
		[Test]
		public void InvalidateInRoot()
		{
			var site = new MockSite();
			var root = new RootBox(new AssembledStyles());
			root.Site = site;
			using (var lc = new LayoutCallbacks(root))
			{
				lc.InvalidateInRoot(new Rectangle(10, 13, 17, 19));
				lc.InvalidateInRoot(new Rectangle(9, 8, 5, 4));
				Assert.That(site.RectsInvalidatedInRoot, Is.Empty);
			}
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(new Rectangle(10, 13, 17, 19)));
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(new Rectangle(9, 8, 5, 4)));
		}

		[Test]
		public void LazyBoxExpanded()
		{
			var root = new RootBox(new AssembledStyles());
			root.RaiseLazyExpanded(new RootBox.LazyExpandedEventArgs()); // make sure harmless with no subscribers
			root.LazyExpanded += root_LazyExpanded;
			using (var lc = new LayoutCallbacks(root))
			{
				lc.RaiseLazyExpanded(10, 17, 5);
				lc.RaiseLazyExpanded(5, 6, -2);
				Assert.That(m_expandArgs, Is.Empty);
			}
			VerifyExpandArgs(0, 10, 17, 5);
			VerifyExpandArgs(1, 5, 6, -2);
		}

		private void VerifyExpandArgs(int index, int top, int bottom, int delta)
		{
			Assert.That(m_expandArgs[index].EstimatedTop, Is.EqualTo(top));
			Assert.That(m_expandArgs[index].EstimatedBottom, Is.EqualTo(bottom));
			Assert.That(m_expandArgs[index].DeltaHeight, Is.EqualTo(delta));
		}

		private List<RootBox.LazyExpandedEventArgs> m_expandArgs = new List<RootBox.LazyExpandedEventArgs>();
		void root_LazyExpanded(object sender, RootBox.LazyExpandedEventArgs e)
		{
			m_expandArgs.Add(e);
		}
	}
}
