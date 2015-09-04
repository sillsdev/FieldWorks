// Copyright (c) 2003-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using SIL.Utils;

namespace SIL.CoreImpl
{
	/// <summary>
	/// MessageSequencer is a Delegate that instances of Control subclasses use
	/// to help ensure that certain messages sent to their WndProc are processed
	/// sequentially, despite the perverse behavior of Windows.Forms applications
	/// of pumping messages to WndProc methods unpredictably, interrupting other
	/// activity. This can happen whenever COM objects are "marshalled into or out
	/// of the CLR" as well as when "Managed blocking" occurs. A very detailed
	/// explanation of the problem may be found at
	/// http://blogs.msdn.com/cbrumme/archive/2004/02/02/66219.aspx
	/// (Chris Brumme's web log on Apartments and Pumping in the CLR).
	///
	/// To use a MessageSequencer:
	///
	/// 1. Create an instance in the constructor of a System.Windows.Forms.Control subclass.
	///		Pass 'this' to the constructor. Save in a member variable.
	/// 2. If you don't already override protected virtual void WndProc(ref Message m),
	///		create a trivial override for it (calling base.WndProc).
	///	3. Implement IReceiveSequentialMessages.
	///	4. Cut and paste the body of your implementation of WndProc into the stub of
	///		OriginalWndProc.
	///	5. Replace the body of your WndProc method with a call to m_messageSequencer.SequenceWndProc.
	///	6. If you have subclasses which override WndProc, make your OriginalWndProc virtual,
	///		and have the subclasses override that instead.
	///	7. If you override OnPaint, and want it sequenced, cut and paste the body of your implementation of
	///		protected virtual void OnPaint(PaintEventArgs e);
	///		into the stub of OriginalOnPaint, and replace it with a call to
	///		m_messageSequencer.SequenceOnPaint.
	///	Note that sequencing OnPaint like this is not always desirable; some handlers
	///	may really want to force an immediate paint by calling DoUpdates. If you want to handle
	///	OnPaint yourself, just don't make the changes to OnPaint. In some cases, we've observed
	///	infinite loops from calling Invalidate and aborting OnPaint. This has not been fully tested.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule",
		Justification = "m_master variable is a reference")]
	public sealed class MessageSequencer : IFWDisposable
	{
		private Control m_master;
		private IReceiveSequentialMessages m_receiver;
		private SafeQueue m_messages = new SafeQueue();
		private bool m_fReentering;
		private int m_cLevelsOfSequentialBlockInMessageHandler; // See Begin/EndSequentialBlock.

		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="master"></param>
		public MessageSequencer(Control master)
		{
			if (master.IsDisposed)
				throw new ArgumentException(String.Format("MessageSequencer cannot use a disposed Control: {0}", master.ToString()));

			m_master = master;
			m_receiver = m_master as IReceiveSequentialMessages;
			if (m_receiver == null)
				throw new Exception("Master control for MessageSequencer must implement IReceiveSequentialMessages");
			if (msgs == null)
				CreateSet();
		}

		// Set uses a Dictionary for fast lookup.
		static Set<int> msgs;

		/// <summary>Create the Set just once</summary>
		private static void CreateSet()
		{
			lock(typeof(MessageSequencer))
			{
				if (msgs != null)
					return;
				msgs = new Set<int>(s_seqMessages);
			}
		}

		static int[] s_seqMessages = new int[] {
										  (int) Win32.WinMsgs.WM_KEYDOWN,
										  (int) Win32.WinMsgs.WM_KEYUP,
										  (int) Win32.WinMsgs.WM_CHAR,
										  (int) Win32.WinMsgs.WM_LBUTTONDOWN,
										  (int) Win32.WinMsgs.WM_LBUTTONUP,
										  (int) Win32.WinMsgs.WM_RBUTTONDOWN,
										  (int) Win32.WinMsgs.WM_RBUTTONUP,
										  (int) Win32.WinMsgs.WM_LBUTTONDBLCLK,
										  (int) Win32.WinMsgs.WM_RBUTTONDBLCLK,
										  (int) Win32.WinMsgs.WM_MOUSEMOVE,
										  (int) Win32.WinMsgs.WM_SETFOCUS,
										  (int) Win32.WinMsgs.WM_KILLFOCUS,
		};

		/// <summary>
		/// Answer true if the message is one of the ones we want to force to be sequential.
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		private bool MethodNeedsSequencing(ref Message m)
		{
			// Set uses a Dictionary for fast lookup.
			return msgs.Contains(m.Msg);
		}

		/// <summary>
		/// Call as the entire body of your WndProc.
		/// </summary>
		/// <param name="m"></param>
		public void SequenceWndProc(ref Message m)
		{
			CheckDisposed();

			if (!MethodNeedsSequencing(ref m))
			{
				m_receiver.OriginalWndProc(ref m);
				return; // not a message we care about.
			}
			if (m_fReentering)
			{
				m_messages.Add(m); // queue and process at end of outer call
			}
			else
			{
				try
				{
					m_fReentering = true;
					m_receiver.OriginalWndProc(ref m);
					// At this point, we've finished processing the original message.
					// If there are pending messages, run them. Note that they, too, may
					// generate interrupts, so we're still in 'reentrant' mode.
					// Need to check, since a non-blocking message handler 'out there'
					// could have called PauseMessageQueueing(), which will set it to false.
					DoPendingMessages();
				}
				finally
				{
					m_fReentering = false;
				}
			}
		}

		// Execute any pending messages.
		private void DoPendingMessages()
		{
			// This can happen, if the client called PauseMessageQueueing in a non-modal environment,
			// which just kept going. PauseMessageQueueing is only to be used in a modal context, that
			// does not keep going right away.
			Debug.Assert(m_fReentering, "You probably are using PauseMessageQueueing in a non-modal context.");
			// A Refresh message can dispose of this object while this object is handling that message.
			while (m_messages != null && m_messages.Count > 0)
			{
				Message m1 = (Message)m_messages.Remove();
				m_receiver.OriginalWndProc(ref m1);
			}
		}

		/// <summary>
		/// Call as the entire body of your OnPaint, if you want to override OnPaint.
		/// </summary>
		/// <param name="e"></param>
		public void SequenceOnPaint(PaintEventArgs e)
		{
			CheckDisposed();

			if (m_fReentering)
			{
				m_master.Invalidate();
			}
			else
			{
				m_fReentering = true;
				try
				{
					m_receiver.OriginalOnPaint(e);
				}
				finally
				{
					DoPendingMessages();
					m_fReentering = false;
				}
			}
		}

		/// <summary>
		/// This is called when, during the processing of a sequenced message, we need to
		/// allow other messages to happen. The boolean result tells whether, in fact,
		/// we were in the process of handling a sequenced message. If it is true,
		/// ResumeMessageQueueing should be called before returning from the original
		/// message handler.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This must *not* be called in contexts that immediately keep going.
		/// It must be called if folliwg code blocks such an immedaite return, as might be the
		/// case for a modal context menu.
		/// </remarks>
		public bool PauseMessageQueueing()
		{
			CheckDisposed();

			if (m_fReentering)
			{
				DoPendingMessages();
				m_fReentering = false;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Call if PauseMessageQueueing returns true.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// NB: This and the PauseMessageQueueing work together, but should only be used
		/// in contexts that do not immediately keep going. The pair of methods are only to be used in the
		/// context where there is a block, as in the case of a modal conetext menu.
		/// </remarks>
		public void ResumeMessageQueueing()
		{
			CheckDisposed();

			m_fReentering = true;
		}

		/// <summary>
		/// Answer true if we are in the midst of handling a sequenced message.
		/// </summary>
		public bool IsSequencedMessageInProgress
		{
			get
			{
				CheckDisposed();

				return m_fReentering;
			}
		}

		/// <summary>
		/// Begin a block of code which, even though it is not itself a message handler,
		/// should not be interrupted by other messages that need to be sequential.
		/// This may be called from within a message handler.
		/// EndSequentialBlock must be called without fail (use try...finally) at the end
		/// of the block that needs protection.
		/// </summary>
		/// <returns></returns>
		public void BeginSequentialBlock()
		{
			CheckDisposed();

			if (m_fReentering)
			{
				// already suppressing messages; remember to disregard EndSequentialBlock
				m_cLevelsOfSequentialBlockInMessageHandler++;
			}
			else
				m_fReentering = true;
		}

		/// <summary>
		/// See BeginSequentialBlock.
		/// </summary>
		public void EndSequentialBlock()
		{
			CheckDisposed();

			if (m_cLevelsOfSequentialBlockInMessageHandler > 0)
			{
				m_cLevelsOfSequentialBlockInMessageHandler--;
			}
			else
			{
				// We were not in a message handler when calling BeginSequentialBlock;
				// handle delayed messages now.
				DoPendingMessages();
				m_fReentering = false;
			}
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
		~MessageSequencer()
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
		private void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				if (m_messages != null)
				{
					while (m_messages.Count > 0)
						m_messages.Remove();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_master = null;
			m_receiver = null;
			m_messages = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// This class implements a queue. As far as possible, it attempts to handle the possibility that
		/// any allocation of memory might result in a recursive call to Add.
		/// </summary>
		private sealed class SafeQueue
		{
			// We try to keep this many empty slots in the array to handle reentrant calls to Add.
			const int kmargin = 50;
			object[] m_queue = new object[kmargin * 2];
			// If m_lim >= m_min, items in queue are from m_min to m_lim-1.
			// If m_lim < m_min, items run from m_min to the end, then from 0 to m_lim-1.
			// There is always at least one empty slot in the array.
			int m_lim; // limit of occupied range; index of where to put next item.
			int m_min; // start of occupied range; first object to return.
			bool m_fGrowing = false;

			/// <summary>
			/// Add an object to the (end of the) queue.
			/// </summary>
			/// <param name="obj"></param>
			public void Add(object obj)
			{
				if (FreeSlotCount < 1)
					throw new Exception("Message Sequence Queue overflow");
				// Before anything else change the state of the queue so that it's actually added.
				m_queue[PostIncrement(ref m_lim)] = obj;
				// If we don't have enough slots, first try moving things.
				if (FreeSlotCount < kmargin && !m_fGrowing)
				{
					// grow the array.
					m_fGrowing = true;
					try
					{
						// Recursive call may happen here! Hopefully there's still room to add them.
						object[] newQueue = GetNewArray(m_queue.Length + kmargin * 2);
						m_queue.CopyTo(newQueue, 0);
						if (m_lim < m_min)
						{
							// The elements are supposed to go up to the end of the array.
							for (int i = m_min; i < m_queue.Length; i++)
							{
								newQueue[i + newQueue.Length - m_queue.Length] = m_queue[i];
							}
							m_min += newQueue.Length - m_queue.Length;
						}
						m_queue = newQueue;
					}
					finally
					{
						m_fGrowing = false;
					}
				}
			}

			/// <summary>
			/// Increment the argument and (like n++) return the OLD value.
			/// Wraps around from m_messages.Length to 0.
			/// </summary>
			/// <param name="current"></param>
			/// <returns></returns>
			int PostIncrement(ref int current)
			{
				int ret = current++;
				if (current == m_queue.Length)
					current = 0;
				return ret;
			}

			/// <summary>
			/// Create a new object array of the given size.
			/// </summary>
			/// <param name="length"></param>
			/// <returns></returns>
			private object[] GetNewArray(int length)
			{
				return new object[length];
			}

			/// <summary>
			/// Remove an object from the start of the queue, and return it.
			/// </summary>
			/// <returns></returns>
			public object Remove()
			{
				if (Count == 0)
					throw new Exception("Remove from empty queue");

				return m_queue[PostIncrement(ref m_min)];
			}

			// The number of free slots is one LESS than the length minus what we have.
			int FreeSlotCount
			{
				get { return m_queue.Length - Count - 1; }
			}

			/// <summary>
			/// The number of items in the queue.
			/// </summary>
			public int Count
			{
				get
				{
					if (m_lim < m_min)
						return m_queue.Length - m_min + m_lim;
					return m_lim - m_min;
				}
			}
		}
	}
}
