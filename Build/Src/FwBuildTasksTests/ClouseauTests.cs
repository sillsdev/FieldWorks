// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using FwBuildTasks;
using NUnit.Framework;

namespace SIL.FieldWorks.Build.Tasks.FwBuildTasksTests
{
	[TestFixture]
	public class ClouseauTests
	{
		private TestBuildEngine _tbi;
		private Clouseau _task;

		[SetUp]
		public void TestSetup()
		{
			_tbi = new TestBuildEngine();
			_task = new Clouseau { BuildEngine = _tbi };
		}

		[Test]
#if !DEBUG
		[Ignore("This test works only in debug builds")]
#endif
		public void ProperlyImplementedIDisposable_LogsNeitherErrorsNorWarnings()
		{
			_task.InspectType(typeof(ProperlyImplementedIDisposable));
			Assert.That(_tbi.Errors, Is.Empty, string.Join(Environment.NewLine, _tbi.Errors));
			Assert.That(_tbi.Warnings, Is.Empty, string.Join(Environment.NewLine, _tbi.Warnings));
			Assert.That(_tbi.Messages.Count, Is.LessThanOrEqualTo(1), string.Join(Environment.NewLine, _tbi.Messages));
		}

		[Test]
#if !DEBUG
		[Ignore("This test works only in debug builds")]
#endif
		public void ProperlyImplementedIFWDisposable_LogsNeitherErrorsNorWarnings()
		{
			_task.InspectType(typeof(ProperlyImplementedIFWDisposable));
			Assert.That(_tbi.Errors, Is.Empty, string.Join(Environment.NewLine, _tbi.Errors));
			Assert.That(_tbi.Warnings, Is.Empty, string.Join(Environment.NewLine, _tbi.Warnings));
			Assert.That(_tbi.Messages.Count, Is.LessThanOrEqualTo(1), string.Join(Environment.NewLine, _tbi.Messages));
		}

		[Test]
#if !DEBUG
		[Ignore("This test works only in debug builds")]
#endif
		public void ProperlyImplementedWindowsForm_LogsNeitherErrorsNorWarnings()
		{
			_task.InspectType(typeof(ProperlyImplementedWindowsForm));
			Assert.That(_tbi.Errors, Is.Empty, string.Join(Environment.NewLine, _tbi.Errors));
			Assert.That(_tbi.Warnings, Is.Empty, string.Join(Environment.NewLine, _tbi.Warnings));
			Assert.That(_tbi.Messages.Count, Is.LessThanOrEqualTo(1), string.Join(Environment.NewLine, _tbi.Messages));
		}

		[Test]
		public void NoProtectedDisposeBool_LogsError()
		{
			var type = typeof(NoProtectedDisposeBool);
			_task.InspectType(type);
			Assert.That(_tbi.Errors, Is.Not.Empty);
			Assert.That(_tbi.Errors[0], Does.Contain(type.FullName));
		}

		[Test]
		public void WindowsFormWithoutDisposeBool_LogsError()
		{
			var type = typeof(WindowsFormWithoutDisposeBool);
			_task.InspectType(type);
			Assert.That(_tbi.Errors, Is.Not.Empty);
			Assert.That(_tbi.Errors[0], Does.Contain(type.FullName));
		}

		[Test]
		public void WindowsFormWithoutBaseDispose_LogsError()
		{
			var type = typeof(WindowsFormWithoutBaseDispose);
			_task.InspectType(type);
			Assert.That(_tbi.Errors, Is.Not.Empty);
			Assert.That(_tbi.Errors[0], Does.Contain(type.FullName));
		}

		[Test]
#if !DEBUG
		[Ignore("This test works only in debug builds")]
#endif
		public void DisposeBoolDoesNotWriteWarning_LogsError()
		{
			var type = typeof(DisposeBoolDoesNotWriteWarning);
			_task.InspectType(type);
			Assert.That(_tbi.Errors, Is.Not.Empty);
			var error = _tbi.Errors[0];
			Assert.That(error, Does.Contain(type.FullName));
			Assert.That(error, Does.Contain("Missing Dispose() call"));
		}

		[Test]
		public void NoFinalizer_LogsError()
		{
			var type = typeof(NoFinalizer);
			_task.InspectType(type);
			Assert.That(_tbi.Errors, Is.Not.Empty);
			Assert.That(_tbi.Errors[0], Does.Contain(type.FullName));
		}

		[Test]
		public void FinalizerDoesntCallDispose_LogsError()
		{
			var type = typeof(FinalizerDoesntCallDispose);
			_task.InspectType(type);
			Assert.That(_tbi.Errors, Is.Not.Empty);
			Assert.That(_tbi.Errors[0], Does.Contain(type.FullName));
		}

		[Test]
		public void FinalizerCallsDisposeTrue_LogsError()
		{
			var type = typeof(FinalizerCallsDisposeTrue);
			_task.InspectType(type);
			Assert.That(_tbi.Errors, Is.Not.Empty);
			Assert.That(_tbi.Errors[0], Does.Contain(type.FullName));
		}

		[Test]
		public void NonDisposable_LogsNeitherErrorsNorWarnings() {
			_task.InspectType(typeof(NonDisposable));
			Assert.That(_tbi.Errors, Is.Empty, string.Join(Environment.NewLine, _tbi.Errors));
			Assert.That(_tbi.Warnings, Is.Empty, string.Join(Environment.NewLine, _tbi.Warnings));
			Assert.That(_tbi.Messages, Is.Empty, string.Join(Environment.NewLine, _tbi.Messages));
		}

		[Test]
		public void ILReader_LogsNeitherErrorsNorWarnings()
		{
			_task.InspectType(typeof(ILReader));
			Assert.That(_tbi.Errors, Is.Empty, string.Join(Environment.NewLine, _tbi.Errors));
			Assert.That(_tbi.Warnings, Is.Empty, string.Join(Environment.NewLine, _tbi.Warnings));
			Assert.That(_tbi.Messages, Is.Empty, string.Join(Environment.NewLine, _tbi.Messages));
		}

		[TestCase(typeof(ImplIEnumerator<>))]
		[TestCase(typeof(ImplIEnumerator<Type>))]
		public void IEnumeratorT_LogsNoErrors(Type type)
		{
			_task.InspectType(type);
			Assert.That(_tbi.Errors, Is.Empty, string.Join(Environment.NewLine, _tbi.Errors));
			Assert.That(_tbi.Warnings, Is.Not.Empty, "Have you checked IEnumerator<T>'s more rigorously? Please update this test.");
		}

		[Test]
		public void IEnumerable_LogsNeitherErrorsNorWarnings()
		{
			_task.InspectType(Assembly.GetAssembly(typeof(ILReader)).DefinedTypes.First(
				t => t.FullName == "FwBuildTasks.ILReader+<GetEnumerator>d__6"));
			Assert.That(_tbi.Errors, Is.Empty, string.Join(Environment.NewLine, _tbi.Errors));
			Assert.That(_tbi.Warnings, Is.Empty, string.Join(Environment.NewLine, _tbi.Warnings));
			Assert.That(_tbi.Messages, Is.Empty, string.Join(Environment.NewLine, _tbi.Messages));
		}

		[Test]
		public void ImplIEnumerator_LogsOnlyWarnings()
		{
			_task.InspectType(Assembly.GetAssembly(typeof(ImplIEnumerator<Type>)).DefinedTypes.First(t => t.Name == "ImplIEnumerator`1"));
			Assert.That(_tbi.Errors, Is.Empty, string.Join(Environment.NewLine, _tbi.Errors));
			Assert.That(_tbi.Warnings, Is.Not.Empty);
		}

		[Test]
		public void NotDisposable_LogsNeitherErrorsNorWarnings()
		{
			_task.InspectType(typeof(NotDisposable));
			Assert.That(_tbi.Errors, Is.Empty, string.Join(Environment.NewLine, _tbi.Errors));
			Assert.That(_tbi.Warnings, Is.Empty, string.Join(Environment.NewLine, _tbi.Warnings));
			Assert.That(_tbi.Messages, Is.Empty, string.Join(Environment.NewLine, _tbi.Messages));
		}

		[Test]
		public void StaticDispose_LogsError()
		{
			var type = typeof(StaticDispose);
			_task.InspectType(type);
			Assert.That(_tbi.Errors, Is.Not.Empty);
			Assert.That(_tbi.Errors[0], Does.Contain(type.FullName));
		}

		[Test]
#if !DEBUG
		[Ignore("This test works only in debug builds")]
#endif
		public void Derived_LogsNeitherErrorsNorWarnings()
		{
			_task.InspectType(typeof(Derived));
			Assert.That(_tbi.Errors, Is.Empty, string.Join(Environment.NewLine, _tbi.Errors));
			Assert.That(_tbi.Warnings, Is.Empty, string.Join(Environment.NewLine, _tbi.Warnings));
			Assert.That(_tbi.Messages, Is.Empty, string.Join(Environment.NewLine, _tbi.Messages));
		}

		[Test]
		public void DerivedWithoutMethod_LogsNeitherErrorsNorWarnings()
		{
			_task.InspectType(typeof(DerivedWithoutMethod));
			Assert.That(_tbi.Errors, Is.Empty, string.Join(Environment.NewLine, _tbi.Errors));
			Assert.That(_tbi.Warnings, Is.Empty, string.Join(Environment.NewLine, _tbi.Warnings));
			Assert.That(_tbi.Messages, Is.Empty, string.Join(Environment.NewLine, _tbi.Messages));
		}

		[Test]
		public void DerivedWithoutBaseCall_LogsError()
		{
			var type = typeof(DerivedWithoutBaseCall);
			_task.InspectType(type);
			Assert.That(_tbi.Errors, Is.Not.Empty);
			Assert.That(_tbi.Errors[0], Does.Contain(type.FullName));
		}

		[Test]
#if !DEBUG
		[Ignore("This test works only in debug builds")]
#endif
		public void DerivedDerived_LogsNeitherErrorsNorWarnings()
		{
			_task.InspectType(typeof(DerivedDerived));
			Assert.That(_tbi.Errors, Is.Empty, string.Join(Environment.NewLine, _tbi.Errors));
			Assert.That(_tbi.Warnings, Is.Empty, string.Join(Environment.NewLine, _tbi.Warnings));
			Assert.That(_tbi.Messages, Is.Empty, string.Join(Environment.NewLine, _tbi.Messages));
		}

		[Test]
#if !DEBUG
		[Ignore("This test works only in debug builds")]
#endif
		public void DerivedControlWithoutMessage_LogsError()
		{
			var type = typeof(DerivedControlWithoutMessage);
			_task.InspectType(type);
			Assert.That(_tbi.Errors, Is.Not.Empty);
			Assert.That(_tbi.Errors[0], Does.Contain(type.FullName));
		}

		[Test]
		public void OtherDerivedControlWithoutMethod_LogsError()
		{
			var type = typeof(OtherDerivedControlWithoutMethod);
			_task.InspectType(type);
			Assert.That(_tbi.Errors, Is.Not.Empty);
			Assert.That(_tbi.Errors[0], Does.Contain(type.FullName));
		}

		[Test]
		public void OtherDerivedControlWithoutBaseCall_LogsError()
		{
			var type = typeof(OtherDerivedControlWithoutBaseCall);
			_task.InspectType(type);
			Assert.That(_tbi.Errors, Is.Not.Empty);
			Assert.That(_tbi.Errors[0], Does.Contain(type.FullName));
		}

		[Test]
		public void Empty_LogsNeitherErrorsNorWarnings()
		{
			_task.InspectType(typeof(Empty));
			Assert.That(_tbi.Errors, Is.Empty, string.Join(Environment.NewLine, _tbi.Errors));
			Assert.That(_tbi.Warnings, Is.Empty, string.Join(Environment.NewLine, _tbi.Warnings));
			Assert.That(_tbi.Messages, Is.Empty, string.Join(Environment.NewLine, _tbi.Messages));
		}

		[Test]
#if !DEBUG
		[Ignore("This test works only in debug builds")]
#endif
		public void DisposableWithoutMessageDerivedFromEmpty_LogsError()
		{
			var type = typeof(DisposableWithoutMessageDerivedFromEmpty);
			_task.InspectType(type);
			Assert.That(_tbi.Errors, Is.Not.Empty);
			Assert.That(_tbi.Errors[0], Does.Contain(type.FullName));
		}

		[Test]
#if !DEBUG
		[Ignore("This test works only in debug builds")]
#endif
		public void NoBody_LogsError()
		{
			var type = typeof(NoBody);
			_task.InspectType(type);
			Assert.That(_tbi.Errors, Is.Not.Empty, "abstract classes are not excused from implementing our boilerplate Disposable requirements");
			Assert.That(_tbi.Errors[0], Does.Contain(type.FullName));
		}

		[Test]
#if !DEBUG
		[Ignore("This test works only in debug builds")]
#endif
		public void DisposableWithoutMessageDerivedFromAbstract_LogsError()
		{
			var type = typeof(DerivedFromBadImpl);
			_task.InspectType(type);
			Assert.That(_tbi.Errors, Is.Empty, "Derived classes should not be reprimanded for their base classes' errors. The base classes should be fixed");
			Assert.That(_tbi.Warnings, Is.Empty, string.Join(Environment.NewLine, _tbi.Warnings));
			Assert.That(_tbi.Messages, Is.Empty, string.Join(Environment.NewLine, _tbi.Messages));
		}

		#region test types
		// ReSharper disable ClassWithVirtualMembersNeverInherited.Local
		// ReSharper disable UnusedMember.Local
		// ReSharper disable EmptyDestructor
		// ReSharper disable RedundantOverriddenMember
		// Justification: Clouseau reflectively verifies the presence of `protected virtual void Dispose(bool disposing)` and other methods
		private class ProperlyImplementedIDisposable : IDisposable
		{
			private string _part;
			~ProperlyImplementedIDisposable()
			{
				Dispose(false);
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			protected virtual void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ******");
				if (disposing && _part != null)
					_part = null;
			}
		}

		private class ProperlyImplementedIFWDisposable : IDisposable // TODO
		{
			private string _part;

			public void CheckDisposed()
			{
				if (IsDisposed)
					throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
			}

			public bool IsDisposed { get; private set; }

			~ProperlyImplementedIFWDisposable()
			{
				Dispose(false);
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			protected virtual void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "********* Missing Dispose() call for " + GetType() + " *********"); // kill two more birds: no `.Name`, extra *'s
				if (disposing && !IsDisposed && _part != null)
					_part = null;
				IsDisposed = true;
			}
		}

		/// <summary>
		/// Subclasses of Windows Forms classes need only implement Dispose(bool) { WriteLineIf }; Dispose() and Finalizer are implemented in Form
		/// </summary>
		private class ProperlyImplementedWindowsForm : UserControl
		{
			private IContainer components;

			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ******");
				if (disposing && !IsDisposed)
					components?.Dispose();
				base.Dispose(disposing);
				components = null;
			}
		}

		private class ImplIEnumerator<T> : IEnumerator<T>
		{
			public void Dispose() {}

			public bool MoveNext()
			{
				return false;
			}

			public void Reset() {}

			// ReSharper disable once UnassignedGetOnlyAutoProperty
			public T Current { get; }

			object IEnumerator.Current => Current;
		}

		private class NoProtectedDisposeBool : IDisposable
		{
			public void Dispose() {}
		}

		private class WindowsFormWithoutDisposeBool : Control {}

		/// <summary>
		/// Subclasses of Windows Forms classes need only implement Dispose(bool) { WriteLineIf }; Dispose() and Finalizer are implemented in Form
		/// </summary>
		private class WindowsFormWithoutBaseDispose : Control
		{
			private readonly IContainer components = null;

			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ******");
				if (disposing && !IsDisposed)
					components?.Dispose();
			}
		}

		private class DisposeBoolDoesNotWriteWarning : IDisposable
		{
			~DisposeBoolDoesNotWriteWarning()
			{
				Dispose(false);
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			protected virtual void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "My dog does not bite. " + GetType().Name + " is not my dog.");
			}
		}

		private class NoFinalizer : IDisposable
		{
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			protected virtual void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ******");
			}
		}

		private class FinalizerDoesntCallDispose : IDisposable
		{
			~FinalizerDoesntCallDispose() {}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			protected virtual void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ******");
			}
		}

		private class FinalizerCallsDisposeTrue : IDisposable
		{
			~FinalizerCallsDisposeTrue()
			{
				Dispose(true);
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			protected virtual void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ******");
			}
		}

		private class NotDisposable
		{
			public void Dispose() {} // does something else
		}

		private class NonDisposable
		{
			protected virtual void Dispose(bool dispose) {}
		}

		private class StaticDispose : IDisposable
		{
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			// ReSharper disable once UnusedParameter.Local
			// ReSharper disable once MemberCanBePrivate.Local
			protected static void Dispose(bool disposing) {}
		}

		private class Derived : ProperlyImplementedIDisposable
		{
			protected override void Dispose(bool disposing)
			{
				base.Dispose(disposing);
			}
		}

		private class DerivedWithoutMethod : ProperlyImplementedIDisposable {}

		private class DerivedWithoutBaseCall : ProperlyImplementedIDisposable
		{
			protected override void Dispose(bool disposing) {}
		}

		private class DerivedDerived : Derived
		{
			protected override void Dispose(bool disposing)
			{
				base.Dispose(disposing);
			}
		}

		private class DerivedControlWithoutMessage : Control
		{
			protected override void Dispose(bool releaseAll)
			{
				base.Dispose(releaseAll);
			}
		}

		private class OtherDerivedControlWithoutMethod : DataGridViewColumn {}

		private class OtherDerivedControlWithoutBaseCall : Control
		{
			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
			}
		}

		private class Empty {}

		private class DisposableWithoutMessageDerivedFromEmpty : Empty, IDisposable
		{
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			protected virtual void Dispose(bool disposing) {}
		}

		private abstract class NoBody : IDisposable
		{
			~NoBody()
			{
				Dispose(false);
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			protected abstract void Dispose(bool disposing);
		}

		private class DerivedFromBadImpl : DisposeBoolDoesNotWriteWarning
		{
			protected override void Dispose(bool disposing)
			{
				base.Dispose(disposing);
			}
		}
		#endregion test types
	}
}
