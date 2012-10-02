// ---------------------------------------------------------
// Windows Forms CommandBar Control
// Copyright (C) 2001-2003 Lutz Roeder. All rights reserved.
// http://www.aisto.com/roeder
// roeder@aisto.com
// ---------------------------------------------------------
namespace Reflector.UserInterface
{
	using System;
	using System.Collections;
	using System.Windows.Forms;

	public class CommandBarCollection : ICollection, IDisposable
	{
		private CommandBarManager commandBarManager;
		private ArrayList bands = new ArrayList();

		public CommandBarCollection(CommandBarManager commandBarManager)
		{
			this.commandBarManager = commandBarManager;
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~CommandBarCollection()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed
		{
			get;
			private set;
		}

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				foreach (var band in bands)
				{
					var disposable = band as IDisposable;
					if (disposable != null)
						disposable.Dispose();
				}
				bands.Clear();
			}
			bands = null;
			commandBarManager = null;
			IsDisposed = true;
		}
		#endregion

		public IEnumerator GetEnumerator()
		{
			return this.bands.GetEnumerator();
		}

		public int Count
		{
			get { return this.bands.Count; }
		}

		public bool IsSynchronized
		{
			get { return false; }
		}

		public object SyncRoot
		{
			get { throw new NotImplementedException(); }
		}

		public void CopyTo(Array array, int index)
		{
			this.bands.CopyTo(array, index);
		}

		public void CopyTo(CommandBar[] array, int index)
		{
			this.bands.CopyTo(array, index);
		}

		public int Add(CommandBar commandBar)
		{
			if (!this.Contains(commandBar))
			{
				int index = this.bands.Add(commandBar);
				this.commandBarManager.AddCommandBar();
				return index;
			}

			return -1;
		}

		public void Clear()
		{
			while (this.Count > 0)
			{
				this.RemoveAt(0);
			}
		}

		public bool Contains(CommandBar commandBar)
		{
			return this.bands.Contains(commandBar);
		}

		public int IndexOf(CommandBar commandBar)
		{
			return this.bands.IndexOf(commandBar);
		}

		public void Remove(CommandBar commandBar)
		{
			this.bands.Remove(commandBar);
			this.commandBarManager.RemoveCommandBar();
		}

		public void RemoveAt(int index)
		{
			this.bands.RemoveAt(index);
			this.commandBarManager.RemoveCommandBar();
		}

		public CommandBar this[int index]
		{
			get { return (CommandBar) this.bands[index]; }
		}
	}
}
