// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Container for singletons that need to be disposed before the application exits. When the
	/// application exits (or when SingletonContainer.Release is called from a test) the
	/// SingletonsContainer will dispose all singletons.
	/// </summary>
	/// <remarks>This class is thread-safe. Multiple threads can read from the container
	/// concurrently, only one thread at a time can create new singletons.</remarks>
	/// ----------------------------------------------------------------------------------------
	public static class SingletonsContainer
	{
		#region SingletonsContainerImpl class
		// ReSharper disable MemberHidesStaticFromOuterClass

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implementation of the singletons container
		/// </summary>
		/// <remarks>This class is thread-safe. Multiple threads can read from the container
		/// concurrently, only one thread at a time can create new singletons.</remarks>
		/// ------------------------------------------------------------------------------------
		private class SingletonsContainerImpl
		{
			private readonly Dictionary<string, IDisposable> m_SingletonsToDispose;
			private readonly ReaderWriterLockSlim m_lock;

			/// --------------------------------------------------------------------------------
			/// <summary>Initializes a new instance of the SingletonsContainerImpl class.</summary>
			/// --------------------------------------------------------------------------------
			public SingletonsContainerImpl()
			{
				m_lock = new ReaderWriterLockSlim();
				m_SingletonsToDispose = new Dictionary<string, IDisposable>();
				Application.ApplicationExit += OnApplicationExit;
			}

			private void OnApplicationExit(object sender, EventArgs e)
			{
				DisposeSingletons();
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>Disposes all the singletons stored in this container.</summary>
			/// ------------------------------------------------------------------------------------
			public void DisposeSingletons()
			{
				m_lock.EnterWriteLock();
				try
				{
					foreach (var keyValuePair in m_SingletonsToDispose)
						keyValuePair.Value.Dispose();

					m_SingletonsToDispose.Clear();

					Application.ApplicationExit -= OnApplicationExit;
				}
				finally
				{
					m_lock.ExitWriteLock();
				}
				m_lock.Dispose();
				s_container = null;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>Adds a singleton with the specified key.</summary>
			/// <exception cref="ArgumentException">A singleton with the same name already
			/// exists in the container.</exception>
			/// --------------------------------------------------------------------------------
			public void Add(string key, IDisposable singleton)
			{
				m_lock.EnterWriteLock();
				try
				{
					m_SingletonsToDispose.Add(key, singleton);
				}
				finally
				{
					m_lock.ExitWriteLock();
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Removes the singleton with the specified key. The singleton is not disposed
			/// by the container. It becomes the responsibility of the caller to dispose the
			/// singleton.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public bool Remove(string key)
			{
				m_lock.EnterWriteLock();
				try
				{
					return m_SingletonsToDispose.Remove(key);
				}
				finally
				{
					m_lock.ExitWriteLock();
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>Gets the singleton with the specified key.</summary>
			/// <remarks>
			/// NOTE: there is no setter for Item(). We definitely do NOT want to be able
			/// to replace an existing item. Either the old one would get prematurely disposed
			/// when it is replaced, possibly breaking clients still using it, or it would never
			/// get disposed, causing the problem all this is aimed at preventing.
			/// </remarks>
			/// --------------------------------------------------------------------------------
			public IDisposable Item(string key)
			{
				m_lock.EnterReadLock();
				try
				{
					IDisposable singleton;
					if (!m_SingletonsToDispose.TryGetValue(key, out singleton))
						return null;
					return singleton;
				}
				finally
				{
					m_lock.ExitReadLock();
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>Gets or creates a singleton with the specified key.</summary>
			/// <typeparam name="T">The type of the singleton. Needs to implement
			/// IDisposable.</typeparam>
			/// <param name="key">The key of the singleton.</param>
			/// <param name="createFunc">The create function that is called when the singleton
			/// doesn't exist yet.</param>
			/// <returns></returns>
			/// <remarks>
			/// This method tries to retrieve a previously constructed singleton with the
			/// specified key. If no existing singleton with this key can be found a new one is
			/// constructed by calling <paramref name="createFunc"/> and added to the container.
			/// If a singleton exists with this key but it has the wrong type an
			/// InvalidCastException is thrown.
			/// </remarks>
			/// --------------------------------------------------------------------------------
			public T Get<T>(string key, Func<T> createFunc) where T : IDisposable
			{
				m_lock.EnterUpgradeableReadLock();
				try
				{
					IDisposable singleton;
					if (m_SingletonsToDispose.TryGetValue(key, out singleton))
						return (T)singleton;

					var result = createFunc();
					// next line will enter/exit WriteLock
					Add(key, result);
					return result;
				}
				finally
				{
					m_lock.ExitUpgradeableReadLock();
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>Determines if a singleton of the specified type was created
			/// before.</summary>
			/// <typeparam name="T">The type of the singleton. Needs to implement
			/// IDisposable.</typeparam>
			/// <param name="key">The key of the singleton.</param>
			/// <remarks>This method checks the existance of a previously constructed singleton
			/// of the specified type and key.</remarks>
			/// --------------------------------------------------------------------------------
			public bool Contains<T>(string key) where T : IDisposable
			{
				m_lock.EnterReadLock();
				try
				{
					IDisposable singleton;
					if (m_SingletonsToDispose.TryGetValue(key, out singleton))
						return singleton is T;
					return false;
				}
				finally
				{
					m_lock.ExitReadLock();
				}
			}
		}
		// ReSharper restore MemberHidesStaticFromOuterClass
		#endregion // class SingletonsContainerImpl

		private static SingletonsContainerImpl s_container;

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the SingletonsContainer instance.</summary>
		/// ------------------------------------------------------------------------------------
		private static SingletonsContainerImpl Instance
		{
			get { return s_container ?? (s_container = new SingletonsContainerImpl()); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Releases this instance. To be used in tests.</summary>
		/// ------------------------------------------------------------------------------------
		internal static void Release()
		{
			if (s_container != null)
				s_container.DisposeSingletons();
			s_container = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Adds a singleton with the specified key.</summary>
		/// <exception cref="ArgumentException">A singleton with the same name already exists in
		/// the container.</exception>
		/// ------------------------------------------------------------------------------------
		public static void Add(string key, IDisposable singleton)
		{
			Instance.Add(key, singleton);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Adds a singleton to the container.</summary>
		/// ------------------------------------------------------------------------------------
		public static void Add(IDisposable singleton)
		{
			Add(singleton.GetType().FullName, singleton);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Removes the singleton with the specified key. The singleton is not disposed
		/// by the container. It becomes the responsibility of the caller to dispose the
		/// singleton.</summary>
		/// ------------------------------------------------------------------------------------
		public static bool Remove(string key)
		{
			return Instance.Remove(key);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Removes the specified singleton. The singleton is not disposed
		/// by the container. It becomes the responsibility of the caller to dispose the
		/// singleton.</summary>
		/// ------------------------------------------------------------------------------------
		public static bool Remove(IDisposable singleton)
		{
			return Remove(singleton.GetType().FullName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the singleton with the specified key.</summary>
		/// <remarks>NOTE: there is no setter for Item(). We definitely do NOT want to be able
		/// to replace an existing item. Either the old one would get prematurely disposed when
		/// it is replaced, possibly breaking clients still using it, or it would never get
		/// disposed, causing the problem all this is aimed at preventing.</remarks>
		/// ------------------------------------------------------------------------------------
		public static IDisposable Item(string key)
		{
			if (s_container == null)
				return null;
			return Instance.Item(key);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets or creates a singleton with the specified key.</summary>
		/// <typeparam name="T">The type of the singleton. Needs to implement IDisposable.
		/// </typeparam>
		/// <param name="key">The key of the singleton.</param>
		/// <param name="createFunc">The create function that is called when the singleton
		/// doesn't exist yet.</param>
		/// <remarks>This method tries to retrieve a previously constructed singleton with the
		/// specified key. If no existing singleton with this key can be found a new one is
		/// constructed by calling <paramref name="createFunc"/> and added to the container. If
		/// a singleton exists with this key but it has the wrong type an InvalidCastException
		/// is thrown.</remarks>
		/// ------------------------------------------------------------------------------------
		public static T Get<T>(string key, Func<T> createFunc) where T : IDisposable
		{
			return Instance.Get(key, createFunc);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets or creates a singleton with the specified type.</summary>
		/// <typeparam name="T">The type of the singleton. Needs to implement IDisposable.
		/// </typeparam>
		/// <param name="createFunc">The create function that is called when the singleton
		/// doesn't exist yet.</param>
		/// <remarks>This method tries to retrieve a previously constructed singleton with the
		/// specified type. If no existing singleton with this key can be found a new one is
		/// constructed by calling <paramref name="createFunc"/> and added to the container.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public static T Get<T>(Func<T> createFunc) where T : IDisposable
		{
			return Get(typeof(T).FullName, createFunc);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets or creates a singleton with the specified key.</summary>
		/// <typeparam name="T">The type of the singleton. Needs to implement IDisposable and
		/// have a default constructor.</typeparam>
		/// <param name="key">The key of the singleton.</param>
		/// <remarks>This method tries to retrieve a previously constructed singleton with the
		/// specified key. If no existing singleton with this key can be found a new one is
		/// constructed and added to the container. If a singleton exists with this key but it
		/// has the wrong type an InvalidCastException is thrown.</remarks>
		/// ------------------------------------------------------------------------------------
		public static T Get<T>(string key) where T: IDisposable, new()
		{
			return Get(key, () => new T());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets or creates a singleton of the specified type.</summary>
		/// <typeparam name="T">The type of the singleton. Needs to implement IDisposable and
		/// have a default constructor.</typeparam>
		/// <remarks>This method tries to retrieve a previously constructed singleton of the
		/// specified type. If no existing singleton of this type can be found a new one is
		/// constructed and added to the container.</remarks>
		/// ------------------------------------------------------------------------------------
		public static T Get<T>() where T : IDisposable, new()
		{
			return Get<T>(typeof(T).FullName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Determines if a singleton of the specified type was created before.</summary>
		/// <typeparam name="T">The type of the singleton. Needs to implement
		/// IDisposable.</typeparam>
		/// <remarks>This method checks the existance of a previously constructed singleton of the
		/// specified type.</remarks>
		/// ------------------------------------------------------------------------------------
		public static bool Contains<T>() where T: IDisposable
		{
			return Contains<T>(typeof(T).FullName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Determines if a singleton of the specified type was created before.</summary>
		/// <param name="key">The key of the singleton.</param>
		/// <typeparam name="T">The type of the singleton. Needs to implement
		/// IDisposable.</typeparam>
		/// <remarks>This method checks the existance of a previously constructed singleton of the
		/// specified type and key.</remarks>
		/// ------------------------------------------------------------------------------------
		public static bool Contains<T>(string key) where T: IDisposable
		{
			return Instance.Contains<T>(key);
		}
	}
}
