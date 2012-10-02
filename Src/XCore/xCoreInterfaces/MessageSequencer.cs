//#define TESTMS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Reflection;

using SIL.FieldWorks.Common.Utils;

namespace XCore
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
	public sealed class MessageSequencer : IFWDisposable
	{
#if TESTMS
		private int m_obj;
		private static bool s_fMatchingHvo;
#endif
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
#if TESTMS
			if (m_master.GetType().Name == "AtomicReferenceView" && m_obj == 0)
			{
				PropertyInfo pi = m_master.GetType().GetProperty("ObjectHvo");
				if (pi != null)
					m_obj = (int)pi.GetValue(m_master, null);
				s_fMatchingHvo = m_obj == 6166; // (m_obj == 6166 || m_obj == 6792);
			}
#endif
#if TESTMS
			Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceWndProc start: " + m_obj.ToString());
#endif
			CheckDisposed();
#if TESTMS
			//Debug.WriteLineIf(m_matchingHvo, "MessageSequencer.SequenceWndProc Must not be disposed.");
#endif

			if (!MethodNeedsSequencing(ref m))
			{
#if TESTMS
				Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceWndProc: MethodNeedsSequencing=false: normal handling of message: " + m.ToString());
#endif
				m_receiver.OriginalWndProc(ref m);
				return; // not a message we care about.
			}
			if (m_fReentering)
			{
#if TESTMS
				Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceWndProc: m_fReentering==true: cache  message: " + m.ToString());
#endif
				m_messages.Add(m); // queue and process at end of outer call
			}
			else
			{
#if TESTMS
				Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceWndProc m_fReentering==false");
#endif
				try
				{
#if TESTMS
					Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceWndProc try: reset m_fReentering to true; original value: " + m_fReentering.ToString());
#endif
					m_fReentering = true;
#if TESTMS
					//Debug.WriteLineIf(m_matchingHvo, "MessageSequencer.SequenceWndProc try: call m_receiver.OriginalWndProc(ref m)");
					Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceWndProc normal handling of watched message start: " + m.ToString());
#endif
					m_receiver.OriginalWndProc(ref m);
#if TESTMS
					//Debug.WriteLineIf(m_matchingHvo, "MessageSequencer.SequenceWndProc try: finished call m_receiver.OriginalWndProc(ref m)");
					Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceWndProc normal handling of watched message end: " + m.ToString());
#endif
					// At this point, we've finished processing the original message.
					// If there are pending messages, run them. Note that they, too, may
					// generate interrupts, so we're still in 'reentrant' mode.
#if TESTMS
					Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceWndProc try: call DoPendingMessages()");
#endif
					// Need to check, since a non-blocking message handler 'out there'
					// could have called PauseMessageQueueing(), which will set it to false.
					DoPendingMessages();
#if TESTMS
					Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceWndProc try: finished call DoPendingMessages()");
#endif
				}
				finally
				{
#if TESTMS
					Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceWndProc finally: reset m_fReentering to false; original value: " + m_fReentering.ToString());
#endif
					m_fReentering = false;
				}
			}
#if TESTMS
			Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceWndProc end: " + m_obj.ToString());
#endif
		}

		// Execute any pending messages.
		private void DoPendingMessages()
		{
#if TESTMS
			Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.DoPendingMessages start: " + m_obj.ToString());
			Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.DoPendingMessages Asserting m_fReentering to be true; " + "original value: " + m_fReentering.ToString());
			if (!m_fReentering)
			{
				Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.DoPendingMessages Failed Assert: Debug.Assert(m_fReentering);");
				Debug.WriteLine(String.Format("Master: {0}", m_master));
			}
#endif
			// This can happen, if the client called PauseMessageQueueing in a non-modal environment,
			// which just kept going. PauseMessageQueueing is only to be used in a modal context, that
			// does not keep going right away.
			Debug.Assert(m_fReentering, "You probably are using PauseMessageQueueing in a non-modal context.");
#if TESTMS
			Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.DoPendingMessages Passed Assert: Debug.Assert(m_fReentering);");
#endif
			// A Refresh message can dispose of this object while this object is handling that message.
			while (m_messages != null && m_messages.Count > 0)
			{
				Message m1 = (Message)m_messages.Remove();
#if TESTMS
				Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.DoPendingMessages handle cached message (start): " + m1.ToString());
#endif
				m_receiver.OriginalWndProc(ref m1);
#if TESTMS
				Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.DoPendingMessages resend message (done): " + m1.ToString());
#endif
			}
#if TESTMS
			Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.DoPendingMessages end: " + m_obj.ToString());
#endif
		}

		/// <summary>
		/// Call as the entire body of your OnPaint, if you want to override OnPaint.
		/// </summary>
		/// <param name="e"></param>
		public void SequenceOnPaint(PaintEventArgs e)
		{
#if TESTMS
			Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceOnPaint start: " + m_obj.ToString());
#endif
			CheckDisposed();
#if TESTMS
			//Debug.WriteLineIf(m_matchingHvo, "MessageSequencer.SequenceOnPaint Must not be disposed.");
#endif
#if TESTMS
			Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceOnPaint check m_fReentering value; original value: " + m_fReentering.ToString());
#endif

			if (m_fReentering)
			{
#if TESTMS
				Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceOnPaint: m_fReentering==true: call m_master.Invalidate()");
#endif
				m_master.Invalidate();
#if TESTMS
				Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceOnPaint finished m_master.Invalidate()");
#endif
			}
			else
			{
#if TESTMS
				Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceOnPaint try: reset m_fReentering to true; original value: " + m_fReentering.ToString());
#endif
				m_fReentering = true;
				try
				{
#if TESTMS
					Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceOnPaint try:  call m_receiver.OriginalOnPaint start: " + e.ToString());
#endif
					m_receiver.OriginalOnPaint(e);
#if TESTMS
					Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceOnPaint try:  call m_receiver.OriginalOnPaint end: " + e.ToString());
#endif
				}
				finally
				{
#if TESTMS
					Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceOnPaint finally: call DoPendingMessages()");
#endif
					DoPendingMessages();
#if TESTMS
					Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceOnPaint try: finished call DoPendingMessages()");
					Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceOnPaint finally: reset m_fReentering to false; original value: " + m_fReentering.ToString());
#endif
					m_fReentering = false;
				}
			}
#if TESTMS
			Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.SequenceOnPaint end: " + m_obj.ToString());
#endif
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
#if TESTMS
			Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.PauseMessageQueueing start: " + m_obj.ToString());
#endif
			CheckDisposed();
#if TESTMS
			//Debug.WriteLineIf(m_matchingHvo, "MessageSequencer.PauseMessageQueueing Must not be disposed.");
			Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.PauseMessageQueueing check m_fReentering vakue; original value: " + m_fReentering.ToString());
#endif

			if (m_fReentering)
			{
#if TESTMS
				Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.PauseMessageQueueing: m_fReentering==true: call DoPendingMessages()");
#endif
				DoPendingMessages();
#if TESTMS
				Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.PauseMessageQueueing: finished call DoPendingMessages()");
				Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.PauseMessageQueueing: reset m_fReentering to false; original value: " + m_fReentering.ToString());
#endif
				m_fReentering = false;
#if TESTMS
				Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.PauseMessageQueueing end returning true: " + m_obj.ToString());
#endif
				return true;
			}
			else
			{
#if TESTMS
				Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.PauseMessageQueueing end returning false: " + m_obj.ToString());
#endif
				return false;
			}
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
#if TESTMS
			Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.ResumeMessageQueueing start: " + m_obj.ToString());
#endif
			CheckDisposed();
#if TESTMS
			//Debug.WriteLineIf(m_matchingHvo, "MessageSequencer.ResumeMessageQueueing Must not be disposed.");
			Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.PauseMessageQueueing: reset m_fReentering to true; original value: " + m_fReentering.ToString());
#endif

			m_fReentering = true;
#if TESTMS
			Debug.WriteLineIf(s_fMatchingHvo, "MessageSequencer.ResumeMessageQueueing end: " + m_obj.ToString());
#endif
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
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
	}

	/// <summary>
	/// Interface for classes wishing to receive sequential messages.
	/// </summary>
	public interface IReceiveSequentialMessages
	{
		/// <summary>
		/// Minimal implementation is base.WndProc(ref m);
		/// </summary>
		/// <param name="m"></param>
		void OriginalWndProc(ref Message m);

		/// <summary>
		/// Minimal implementation is nothing, if you don't override OnPaint.
		/// </summary>
		/// <param name="e"></param>
		void OriginalOnPaint(PaintEventArgs e);

		/// <summary>
		/// Get the actual message sequencer. (This is not used by the sequencer, itself, but is very
		/// useful for helper classes handling controls which may need to interact with the sequencer
		/// if their control has one.)
		/// </summary>
		MessageSequencer Sequencer
		{
			get;
		}
	}

	/// <summary>
	/// This class implements a queue. As far as possible, it attempts to handle the possibility that
	/// any allocation of memory might result in a recursive call to Add.
	/// </summary>
	public class SafeQueue
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
		/// This is virtual so in testing we can deliberately make it do reentrant calls.
		/// </summary>
		/// <param name="length"></param>
		/// <returns></returns>

		protected virtual object[] GetNewArray(int length)
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
