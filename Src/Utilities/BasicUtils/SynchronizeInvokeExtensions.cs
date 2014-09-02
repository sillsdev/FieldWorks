using System;
using System.ComponentModel;

namespace SIL.Utils
{
	/// <summary>
	/// ISynchronizeInvoke extension methods
	/// </summary>
	public static class SynchronizeInvokeExtensions
	{
		/// <summary>
		/// Invokes the specified action synchronously on the thread on which this ThreadHelper
		/// was created. If the calling thread is the thread on which this ThreadHelper was
		/// created, no invoke is performed.
		/// </summary>
		/// <param name="si">The synchronize invoke object.</param>
		/// <param name="action">The action.</param>
		public static void Invoke(this ISynchronizeInvoke si, Action action)
		{
			if (si.InvokeRequired)
				si.Invoke(action, null);
			else
				action();
		}

		/// <summary>
		/// Invokes the specified function synchronously on the thread on which this
		/// ThreadHelper was created. If the calling thread is the thread on which this
		/// ThreadHelper was created, no invoke is performed.
		/// </summary>
		/// <param name="si">The synchronize invoke object.</param>
		/// <param name="func">The function.</param>
		public static TResult Invoke<TResult>(this ISynchronizeInvoke si, Func<TResult> func)
		{
			if (si.InvokeRequired)
				return (TResult) si.Invoke(func, null);
			return func();
		}

		/// <summary>
		/// Invokes the specified action, asynchronously or not on the thread on which this
		/// ThreadHelper was created
		/// </summary>
		/// <param name="si">The synchronize invoke object.</param>
		/// <param name="invokeAsync">if set to <c>true</c> invoke asynchronously.</param>
		/// <param name="action">The action to perform.</param>
		public static void Invoke(this ISynchronizeInvoke si, bool invokeAsync, Action action)
		{
			if (invokeAsync)
				si.InvokeAsync(action);
			else
				si.Invoke(action);
		}

		/// <summary>
		/// Executes the specified method asynchronously. The action will typically be called
		/// when the the Application.Run() loop regains control or the next call to
		/// Application.DoEvents() at some unspecified time in the future.
		/// </summary>
		public static void InvokeAsync(this ISynchronizeInvoke si, Action action)
		{
			si.BeginInvoke(action, null);
		}

		/// <summary>
		/// Executes the specified method asynchronously. The action will typically be called
		/// when the the Application.Run() loop regains control or the next call to
		/// Application.DoEvents() at some unspecified time in the future.
		/// </summary>
		public static void InvokeAsync<T>(this ISynchronizeInvoke si, Action<T> action, T param1)
		{
			si.BeginInvoke(action, new object[] {param1});
		}

		/// <summary>
		/// Executes the specified method asynchronously. The action will typically be called
		/// when the the Application.Run() loop regains control or the next call to
		/// Application.DoEvents() at some unspecified time in the future.
		/// </summary>
		public static void InvokeAsync<T1, T2>(this ISynchronizeInvoke si, Action<T1, T2> action, T1 param1, T2 param2)
		{
			si.BeginInvoke(action, new object[] {param1, param2});
		}

		/// <summary>
		/// Executes the specified method asynchronously. The action will typically be called
		/// when the the Application.Run() loop regains control or the next call to
		/// Application.DoEvents() at some unspecified time in the future.
		/// </summary>
		public static void InvokeAsync<T1, T2, T3>(this ISynchronizeInvoke si, Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3)
		{
			si.BeginInvoke(action, new object[] {param1, param2, param3});
		}
	}
}
