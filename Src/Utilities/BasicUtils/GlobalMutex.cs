using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
#if __MonoCS__
using System.IO;
using System.Runtime.InteropServices;
using Mono.Unix.Native;
#endif

namespace SIL.Utils
{
	/// <summary>
	/// This is a cross-platform, global, named mutex that can be used to synchronize access to data across processes.
	/// It supports reentrant locking.
	///
	/// This is needed because Mono does not support system-wide, named mutexes. Mono does implement the Mutex class,
	/// but even when using the constructors with names, it only works within a single process.
	/// </summary>
	public class GlobalMutex : FwDisposableBase
	{
		private readonly IGlobalMutexAdapter m_adapter;
		private readonly string m_name;
		private bool m_initialized;

		/// <summary>
		/// Initializes a new instance of the <see cref="GlobalMutex"/> class.
		/// </summary>
		public GlobalMutex(string name)
		{
			m_name = name;
#if __MonoCS__
			m_adapter = new LinuxGlobalMutexAdapter(name);
#else
			m_adapter = new WindowsGlobalMutexAdapter(name);
#endif
		}

		/// <summary>
		/// Gets the mutex name.
		/// </summary>
		public string Name
		{
			get
			{
				CheckDisposed();

				return m_name;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this mutex is initialized.
		/// </summary>
		public bool IsInitialized
		{
			get
			{
				CheckDisposed();

				return m_initialized;
			}
		}

		/// <summary>
		/// Unlinks or removes the mutex from the system. This only has an effect on Linux.
		/// Windows will automatically unlink the mutex. This can be called while the mutex is locked.
		/// </summary>
		public bool Unlink()
		{
			CheckDisposed();

			return m_adapter.Unlink();
		}

		/// <summary>
		/// Initializes this mutex.
		/// </summary>
		public bool Initialize()
		{
			CheckDisposed();

			bool res = m_adapter.Init(false);
			m_initialized = true;
			return res;
		}

		/// <summary>
		/// Initializes and locks this mutex. On Windows, this is an atomic operation, so the "createdNew"
		/// variable is guaranteed to return a correct value. On Linux, this is not an atomic operation,
		/// so "createdNew" is guaranteed to be correct only if it returns true.
		/// </summary>
		public IDisposable InitializeAndLock(out bool createdNew)
		{
			CheckDisposed();

			createdNew = m_adapter.Init(true);
			return new ReleaseDisposable(m_adapter);
		}

		/// <summary>
		/// Initializes and locks this mutex. This is an atomic operation on Windows, but not Linux.
		/// </summary>
		/// <returns></returns>
		public IDisposable InitializeAndLock()
		{
			bool createdNew;
			return InitializeAndLock(out createdNew);
		}

		/// <summary>
		/// Locks this mutex.
		/// </summary>
		public IDisposable Lock()
		{
			CheckDisposed();

			m_adapter.Wait();
			return new ReleaseDisposable(m_adapter);
		}

		/// <summary>
		/// Disposes managed resources.
		/// </summary>
		protected override void DisposeManagedResources()
		{
			m_adapter.Dispose();
			m_initialized = false;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule",
			Justification="m_adapter is a reference.")]
		private sealed class ReleaseDisposable : IDisposable
		{
			private readonly IGlobalMutexAdapter m_adapter;

			public ReleaseDisposable(IGlobalMutexAdapter adapter)
			{
				m_adapter = adapter;
			}

			public void Dispose()
			{
				m_adapter.Release();
			}
		}

		private interface IGlobalMutexAdapter : IDisposable
		{
			bool Init(bool initiallyOwned);
			void Wait();
			void Release();
			bool Unlink();
		}

#if __MonoCS__
		/// <summary>
		/// On Linux, the global mutex is implemented using file locks.
		/// </summary>
		private class LinuxGlobalMutexAdapter : FwDisposableBase, IGlobalMutexAdapter
		{
			private int m_handle;
			private readonly string m_name;
			private readonly object m_syncObject = new object();
			private readonly ThreadLocal<int> m_waitCount = new ThreadLocal<int>();

			private const int LOCK_EX = 2;
			private const int LOCK_UN = 8;

			[DllImport("libc", SetLastError = true)]
			private static extern int flock(int handle, int operation);

			public LinuxGlobalMutexAdapter(string name)
			{
				m_name = Path.Combine("/var/lock", name);
			}

			public bool Init(bool initiallyOwned)
			{
				m_handle = Syscall.open(m_name, OpenFlags.O_CREAT | OpenFlags.O_EXCL, FilePermissions.S_IWUSR | FilePermissions.S_IRUSR);
				bool result;
				if (m_handle != -1)
				{
					result = true;
				}
				else
				{
					Errno errno = Syscall.GetLastError();
					if (errno != Errno.EEXIST)
						throw new NativeException((int) errno);
					m_handle = Syscall.open(m_name, OpenFlags.O_CREAT, FilePermissions.S_IWUSR | FilePermissions.S_IRUSR);
					if (m_handle == -1)
						throw new NativeException((int) Syscall.GetLastError());
					result = false;
				}
				if (initiallyOwned)
					Wait();
				return result;
			}

			public void Wait()
			{
				if (m_waitCount.Value == 0)
				{
					Monitor.Enter(m_syncObject);
					if (flock(m_handle, LOCK_EX) == -1)
						throw new NativeException(Marshal.GetLastWin32Error());
				}
				m_waitCount.Value++;
			}

			public void Release()
			{
				m_waitCount.Value--;
				if (m_waitCount.Value == 0)
				{
					if (flock(m_handle, LOCK_UN) == -1)
						throw new NativeException(Marshal.GetLastWin32Error());
					Monitor.Exit(m_syncObject);
				}
			}

			public bool Unlink()
			{
				lock (m_syncObject)
				{
					if (Syscall.unlink(m_name) == -1)
					{
						Errno errno = Syscall.GetLastError();
						if (errno == Errno.ENOENT)
							return false;
						throw new NativeException((int) errno);
					}
					return true;
				}
			}

			protected override void DisposeUnmanagedResources()
			{
				Syscall.close(m_handle);
			}
		}

#else
		/// <summary>
		/// On Windows, the global mutex is implemented using a named mutex.
		/// </summary>
		private class WindowsGlobalMutexAdapter : FwDisposableBase, IGlobalMutexAdapter
		{
			private readonly string m_name;
			private Mutex m_mutex;

			public WindowsGlobalMutexAdapter(string name)
			{
				m_name = name;
			}

			public bool Init(bool initiallyOwned)
			{
				bool createdNew;
				m_mutex = new Mutex(initiallyOwned, m_name, out createdNew);
				if (initiallyOwned && !createdNew)
					Wait();
				return createdNew;
			}

			public void Wait()
			{
				m_mutex.WaitOne();
			}

			public void Release()
			{
				m_mutex.ReleaseMutex();
			}

			public bool Unlink()
			{
				return true;
			}

			protected override void DisposeManagedResources()
			{
				m_mutex.Dispose();
			}
		}
#endif
	}
}
