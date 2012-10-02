using System;
using System.Diagnostics;

// Copyright Copyright © 2002 CyberbrineDreams; for permission to use this fragment contact webmaster@cyberbrinedreams.com
// Very slightly adapted (comments only changed) from http://www.cyberbrinedreams.com/version2/projects/code/stable_sort/

namespace SIL.Utils
{
	/// <summary>
	/// MergeSort - a stable sort for .Net
	///
	/// Mostly a translation of the C++ mergesort from
	/// Robert Sedgewick's Algorithms in C++, 1st edition
	///
	/// This implementations supports Arrays and ArrayLists. Because of .Net limitations
	/// primitive arrays (example: int [] array = new int[10000];) cannot be sorted by this code.
	/// </summary>
	public class MergeSort : IFWDisposable
	{
		#region Data members
		/// <summary>
		/// Implementation for Arrays
		/// </summary>
		private Array secondary = null;

		#endregion Data members

		#region Public Interface
		/// <summary>
		/// Sort an array using the default comparer
		/// </summary>
		/// <param name="array">array to be sorted</param>
		public static void Sort(ref Array array)
		{
			using (MergeSort mergeSort = new MergeSort())
			{
				mergeSort.InternalSort(ref array, 0, array.Length - 1, System.Collections.Comparer.Default);
			}
		}

		/// <summary>
		/// Sort an array using the specified comparer
		/// </summary>
		/// <param name="array">array to be sorted</param>
		/// <param name="compare">comparer to use for sorting</param>
		public static void Sort(ref Array array, System.Collections.IComparer compare)
		{
			using (MergeSort mergeSort = new MergeSort())
			{
				mergeSort.InternalSort(ref array, 0, array.Length - 1, compare);
			}
		}

		/// <summary>
		/// Sort an ArrayList using the default comparer
		/// </summary>
		/// <param name="array">ArrayList to sort</param>
		public static void Sort(ref System.Collections.ArrayList array)
		{
			using (MergeSort mergeSort = new MergeSort())
			{
				mergeSort.InternalSort(ref array, 0, array.Count - 1, System.Collections.Comparer.Default);
			}
		}

		/// <summary>
		/// Sort an ArrayList using the specified comparer
		/// </summary>
		/// <param name="array">ArrayList to sort</param>
		/// <param name="compare">Comparer to used for sorting</param>
		public static void Sort(ref System.Collections.ArrayList array, System.Collections.IComparer compare)
		{
			using (MergeSort mergeSort = new MergeSort())
			{
				mergeSort.InternalSort(ref array, 0, array.Count - 1, compare);
			}
		}
		#endregion

		#region Internal Interface
		/// <summary>
		/// Make one
		/// </summary>
		protected MergeSort()
		{
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~MergeSort()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (secondary != null)
				{
					foreach (Object obj in secondary)
					{
						if (obj is IDisposable)
							(obj as IDisposable).Dispose();
					}
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			secondary = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// The actual implementation
		/// </summary>
		/// <param name="primary"></param>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="compare"></param>
		protected void InternalSort(
		ref Array primary,
		int left,
		int right,
		System.Collections.IComparer compare)
		{
			if (secondary == null || secondary.Length != primary.Length)
				secondary = (Array)primary.Clone();

			if (right > left)
			{
				int middle = (left + right) / 2;
				InternalSort(ref primary, left, middle, compare);
				InternalSort(ref primary, middle + 1, right, compare);

				int i, j, k;
				for (i = middle + 1; i > left; i--)
					secondary.SetValue(primary.GetValue(i - 1), i - 1);
				for (j = middle; j < right; j++)
					secondary.SetValue(primary.GetValue(j + 1), right + middle - j);
				for (k = left; k <= right; k++)
					primary.SetValue(
					(compare.Compare(secondary.GetValue(i), secondary.GetValue(j)) < 0) ?
					secondary.GetValue(i++) :
					secondary.GetValue(j--), k);
			}
		}

		/// <summary>
		/// Implementation for ArrayLists
		/// </summary>
		private System.Collections.ArrayList secondaryList = null;
		/// <summary>
		/// The actual implementation
		/// </summary>
		/// <param name="primary"></param>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="compare"></param>
		protected void InternalSort(
		ref System.Collections.ArrayList primary,
		int left,
		int right,
		System.Collections.IComparer compare)
		{
			if (secondaryList == null || secondaryList.Count != primary.Count)
				secondaryList = (System.Collections.ArrayList)primary.Clone();

			if (right > left)
			{
				int middle = (left + right) / 2;
				InternalSort(ref primary, left, middle, compare);
				InternalSort(ref primary, middle + 1, right, compare);

				int i, j, k;
				for (i = middle + 1; i > left; i--)
					secondaryList[i - 1] = primary[i - 1];
				for (j = middle; j < right; j++)
					secondaryList[right + middle - j] = primary[j + 1];
				for (k = left; k <= right; k++)
					primary[k] = (compare.Compare(secondaryList[i], secondaryList[j]) < 0) ?
					secondaryList[i++] : secondaryList[j--];
			}
		}
		#endregion
	}
}
