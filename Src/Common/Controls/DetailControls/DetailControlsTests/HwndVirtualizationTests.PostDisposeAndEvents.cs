// Copyright (c) 2025 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Xml;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public partial class HwndVirtualizationTests
	{
		#region Category 17: Safe property access after disposal

		/// <summary>
		/// BASELINE: Accessing properties after Dispose throws ObjectDisposedException (via CheckDisposed).
		/// This verifies the guard pattern that virtualization must preserve.
		/// </summary>
		[Test]
		public void PropertyAccess_AfterDispose_ThrowsObjectDisposedException()
		{
			var slice = CreateStandaloneSlice();
			slice.Dispose();

			Assert.Throws<ObjectDisposedException>(() => { var _ = slice.Label; },
				"Accessing Label after Dispose should throw ObjectDisposedException.");
		}

		#endregion

		#region Category 18: Slice with ConfigurationNode

		/// <summary>
		/// BASELINE: ConfigurationNode can be set on a standalone slice.
		/// Layout interpretation reads this to decide slice behavior.
		/// </summary>
		[Test]
		public void ConfigurationNode_SetOnStandaloneSlice()
		{
			using (var slice = CreateStandaloneSlice())
			{
				var doc = new XmlDocument();
				doc.LoadXml("<slice field=\"TestField\" label=\"Test\" />");
				slice.ConfigurationNode = doc.DocumentElement;
				Assert.That(slice.ConfigurationNode, Is.Not.Null);
				Assert.That(slice.ConfigurationNode.OuterXml, Does.Contain("TestField"));
			}
		}

		#endregion

		#region Category 19: IsHeaderNode interaction with SetCurrentState

		/// <summary>
		/// BASELINE: SetCurrentState walks up ParentSlice chain looking for IsHeaderNode.
		/// A standalone slice with no ParentSlice and no header terminates immediately.
		/// </summary>
		[Test]
		public void SetCurrentState_NoParentSlice_DoesNotLoop()
		{
			var dtree = CreatePopulatedDataTree();
			using (var slice = new Slice())
			{
				slice.Install(dtree);
				Assert.DoesNotThrow(() => slice.SetCurrentState(true));
				Assert.DoesNotThrow(() => slice.SetCurrentState(false));
			}
		}

		#endregion

		#region Category 20: Event handler lifecycle

		/// <summary>
		/// BASELINE: A Slice without a content control has no event handler issues on dispose.
		/// ViewSlice subscribes to LayoutSizeChanged, Enter, SizeChanged — these would
		/// fail if the Control is null at dispose time.
		/// </summary>
		[Test]
		public void EventLifecycle_NoControl_DisposeIsSafe()
		{
			var slice = new Slice();
			Assert.That(slice.Control, Is.Null);
			Assert.DoesNotThrow(() => slice.Dispose(),
				"Disposing a slice with no Control should not try to unsubscribe events.");
		}

		/// <summary>
		/// BASELINE: A Slice with a control that is disposed before the slice should not
		/// cause problems.
		/// </summary>
		[Test]
		public void EventLifecycle_ControlDisposedFirst_SafeSliceDispose()
		{
			var ctrl = new TextBox();
			var slice = new Slice(ctrl);
			ctrl.Dispose();
			Assert.DoesNotThrow(() => slice.Dispose(),
				"Disposing a slice whose control was already disposed should not throw.");
		}

		#endregion
	}
}
