// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Mediator.cs
// Authorship History: John Hatton
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Text;
using System.Windows.Forms;

using SIL.Utils;

namespace XCore
{
	/// <summary>
	/// Add this attribute to a class to say the mediator should dispose of it
	/// if it is still a coleague of the mediator when that is disposed.
	/// "Listeners" should normally have this.
	/// </summary>
	public class MediatorDisposeAttribute : Attribute
	{
	}

	/// <summary>
	/// A comparer for the tuples used - the default comparer for tuples requires
	/// all items to implement IComparable.
	/// </summary>
	internal class TupleComparer : IComparer<Tuple<int, IxCoreColleague>>
	{
		#region IComparer<Tuple<int,IxCoreColleague>> Members

		public int Compare(Tuple<int, IxCoreColleague> x, Tuple<int, IxCoreColleague> y)
		{
			int c = x.Item1 - y.Item1;
			if (c != 0)
				return c;
			// Avoid possible overflow in the subtraction.  See FWNX-896.  I'm not sure why
			// this affects only Linux/Mono unless the Windows/.Net default implementation
			// returns hash codes that are always positive integers.
			Int64 val1 = (Int64)x.Item2.GetHashCode() - (Int64)y.Item2.GetHashCode();
			return (int)(val1 / 2L);
		}

		#endregion
	}

	/// <summary></summary>
	[SuppressMessage("Gendarme.Rules.Design", "UseCorrectDisposeSignaturesRule",
		Justification = "We derive from Component and therefore can't modify the signature of Dispose(bool)")]
	public sealed class Mediator : Component, IFWDisposable
	{
		#region PendingMessageItem

		/// <summary>
		/// This class is used by the message queue system.
		/// </summary>
		private class PendingMessageItem
		{
			public string m_message;
			public object m_parameter;

			public PendingMessageItem(string message, object parameter)
			{
				m_message = message;
				m_parameter = parameter;
			}
		}

		#endregion #region PendingMessageItem

		#region QueueItem class
		/// <summary>
		/// This is a simple class that contains the information that is used in the InvokeRecursively method.
		/// </summary>
		private class QueueItem
		{
			public string m_methodName;
			public Type[] m_parameterTypes;
			public object[] m_parameterList;
			public bool m_stopWhenHandled;
			public bool m_justCheckingForReceivers;

			public QueueItem(string methodName, Type[] parameterTypes, object[] parameterList, bool stopWhenHandled, bool justCheckingForReceivers)
			{
				m_methodName = methodName;
				m_parameterTypes = parameterTypes;
				m_parameterList = parameterList;
				m_stopWhenHandled = stopWhenHandled;
				m_justCheckingForReceivers = justCheckingForReceivers;
			}

			[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
				Justification="See TODO-Linux comment")]
			public override bool Equals(object obj)
			{
				if (obj == null)
					return false;
				if (!(obj is QueueItem))
					return false;

				QueueItem asItem = obj as QueueItem;
				if (m_methodName != asItem.m_methodName)
					return false; // Not the same nethod.
				if (m_parameterTypes.Length != asItem.m_parameterTypes.Length)
					return false; // Not the same number of parameter types.
				if (m_parameterList.Length != asItem.m_parameterList.Length)
					return false; // Not the same number of parameter values.
				if (m_parameterTypes.Length != asItem.m_parameterList.Length)
					return false; // Not the same number of parameter types to values.
				for (int i = 0; i < m_parameterTypes.Length; ++i)
				{
					Type parmType = m_parameterTypes[i];
					object parmValue = m_parameterList[i];
					// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
					// is marked with [MonoTODO] and might not work as expected in 4.0.
					if (asItem.m_parameterTypes[i] != parmType || asItem.m_parameterList[i] != parmValue)
						return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int hashCode = m_methodName.GetHashCode();

				foreach (Type typ in m_parameterTypes)
					hashCode += typ.GetHashCode();
				foreach (object obj in m_parameterList)
					hashCode += obj.GetHashCode();

				return hashCode;
			}

			public override string ToString()
			{
				StringBuilder sb = new StringBuilder("QueueItem: ");
				sb.AppendFormat("{0}:", m_methodName);
				for (int i = 0; i < m_parameterTypes.Length; ++i)
				{
					sb.AppendFormat(" {0}:{1}", m_parameterTypes[i].ToString(), m_parameterList[i].ToString());
				}
				return sb.ToString();
			}
		}
		#endregion

		#region Data members
		// testing to have list of IxCoreColleagues that are disposed now
		private Set<string> m_disposedColleagues = new Set<string>();
		public void AddDisposedColleague(string hashKey)
		{
			CheckDisposed();
			if (!m_disposedColleagues.Contains(hashKey))
			{
				m_disposedColleagues.Add(hashKey);
				Debug.WriteLine("@@Added "+hashKey);
			}
		}
		public bool IsDisposedColleague(string hashKey) { return m_disposedColleagues.Contains(hashKey); }
		//private void ClearDisposedColleagues() { m_disposedColleagues.Clear(); }
		private bool m_processMessages = true;
		private PropertyTable m_propertyTable;
		private CommandSet m_commandSet;
//		private bool m_allowCommandsToExecute;
		private SortedDictionary<Tuple<int, IxCoreColleague>, bool> m_colleagues = new SortedDictionary<Tuple<int, IxCoreColleague>, bool>(new TupleComparer());
		private IxCoreColleague m_temporaryColleague;
		private Dictionary<string, string> m_pathVariables = new Dictionary<string, string>();
		private Dictionary<Type, Dictionary<string, MethodInfo>> m_TypeMethodInfo = new Dictionary<Type, Dictionary<string, MethodInfo>>();
		/// <summary>
		/// Control how much output we send to the application's listeners (e.g. visual studio output window)
		/// </summary>
		private TraceSwitch showPendingMsgsSwitch = new TraceSwitch("ShowPendingMsgs", "All Items on the pending msg queue", "Off");
		private TraceSwitch invokeSwitch = new TraceSwitch("XCore.Mediator_InvokeTrace", "Invoke tracking", "Off");
		#region Data members for Queueing
		/// <summary>This is the message value that is used to communicate the need to process the defered mediator queue</summary>
		public const int WM_BROADCAST_ITEM_INQUEUE = 0x8000+0x77;	// wm_app + 0x77
		private Queue<QueueItem> m_jobs = new Queue<QueueItem>();	// queue to contain the defered broadcasts
		private Queue<QueueItem> m_pendingjobs = new Queue<QueueItem>(); // queue to contain the broadcasts until we have a main window
		private int m_queueLastProcessed;	// size of queue the last time it was processed
		private bool m_RemovedItemLast;
		// flag to let us know when we're already processing an item
		private bool m_processItemBusy;

		#endregion
		private long m_SavedCalls;	// number of calls to Type.GetMethod that are saved (just informational).
		private long m_MethodsCount;	// max depth on the methods of all colleagues
#if DEBUG
		private long m_MethodChecks;	// total number of calls to the IsMethodNOTonColleague method
#endif


		/// <summary>keeps a list of classes (colleagues) and the methods that it doesn't contain</summary>
		private Dictionary<string, Set<string>> m_MethodsNOTonColleagues;	// key=colleague.ToString(), value=Set of methods of methods
		/// <summary>Set of method names that are implemented by any colleague</summary>
		private Set<string> m_MethodsOnAnyColleague;

		private readonly IdleQueue m_idleQueue = new IdleQueue();
		#endregion

		#region Construction and Initialization
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Mediator"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public Mediator()
		{
			m_propertyTable = new PropertyTable(this);
			m_MethodsOnAnyColleague = new Set<string>();
//			m_allowCommandsToExecute = false;

			// NOTE: to set the trace level, create a config file like the following and set
			// it there. Values are: Off=0, Error=1, Warning=2, Info=3, Verbose=4.
			// <configuration>
			//	<system.diagnostics>
			//	   <switches>
			//		  <add name="Mediator" value="3" />
			//	   </switches>
			//	</system.diagnostics>
			// </configuration>

			// OR you can temporarily uncomment one of the following:

			//invokeSwitch.Level = TraceLevel.Info;	//some output. Will tell you what methods are being invoke, on whom.

			//NB: To make this much use, you'll need to disable the IDLE processing in XWindow.
			//otherwise, XCore will spend all of its time tracing things here during idle time
			//	and you won't be able to find the bug You are seeking.

			/* -----------------------------------------------------------------
			 * TraceLevel.Verbose will show something like this (BUT SO MUCH IT'S HARDLY USEABLE):

			  Looking for listeners for Msg: DisplayWritingSystemList
				[XCore.MockupDialogLauncher
					{XCore.MockupDialogLauncher
						Checking : XCore.MockupDialogLauncher
					}
				]
				[SIL.FieldWorks.WordWorks.Parser.ParserListener
					{SIL.FieldWorks.WordWorks.Parser.ParserListener
						Checking : SIL.FieldWorks.WordWorks.Parser.ParserListener
					}
				]

				[SIL.FieldWorks.XWorks.WritingSystemListHandler
					{SIL.FieldWorks.XWorks.WritingSystemListHandler
						Checking : SIL.FieldWorks.XWorks.WritingSystemListHandler
						Invoking method: SIL.FieldWorks.XWorks.WritingSystemListHandler
						Invoking Method: OnDisplayWritingSystemList on SIL.FieldWorks.XWorks.WritingSystemListHandler
					}
				]
			*/

		}

		/// <summary>
		///
		/// </summary>
		/// <remarks> this needs to be done separately from construction because the mediator is
		/// handed to the CommandSet when it is constructed.</remarks>
		/// <param name="commandset"></param>
		public void Initialize(CommandSet commandset)
		{
			CheckDisposed();
			m_commandSet = commandset;
		}

		////public bool AllowCommandsToExecute
		////{
		////	get { return m_allowCommandsToExecute; }
		////	set { m_allowCommandsToExecute = value; }
		////}

		#endregion

		#region IDisposable & Co. implementation
		// Region last reviewed: Oct. 13, 2005 (RandyR).

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
		/// Throw if the IsDisposed property is true
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException("Mediator", "This object is being used after it has been disposed: this is an Error.");
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
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Can be called more than once, but not run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.

				// Use a copy of the m_colleagues Set,
				// since the Dispose methods on the colleague should remove itself from m_colleagues,
				// which will cause an exception to be throw (list changed while spinning through it.
				Set<IxCoreColleague> copyOfColleagues = new Set<IxCoreColleague>();
				foreach (var key in m_colleagues.Keys)
				{
					copyOfColleagues.Add(key.Item2);
				}
				m_colleagues.Clear(); // Just get rid of them now.
				foreach (IxCoreColleague icc in copyOfColleagues)
				{
					if (icc is IDisposable)
					{
						// Is the class marked with [XCore.MediatorDispose],
						// or is it in the temporary colleague holder?
						object[] attrs = icc.GetType().GetCustomAttributes(typeof(MediatorDisposeAttribute), true);
						if ((attrs != null && attrs.Length > 0)
							|| m_temporaryColleague == icc)
						{
							(icc as IDisposable).Dispose();
						}
					}
				}
				copyOfColleagues.Clear();
				if (m_propertyTable != null)
					m_propertyTable.Dispose();
				if (m_commandSet != null)
					m_commandSet.Dispose();
				if (m_pathVariables != null)
					m_pathVariables.Clear();
				if (m_disposedColleagues != null)
					m_disposedColleagues.Clear();
				if (m_jobs != null)
					m_jobs.Clear();
				if (m_pendingjobs != null)
					m_pendingjobs.Clear();
				if (m_MethodsOnAnyColleague != null)
					m_MethodsOnAnyColleague.Clear();

				m_idleQueue.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mainWndPtr = IntPtr.Zero;
			m_MethodsOnAnyColleague = null;
			m_pendingjobs = null;
			m_jobs = null;
			m_disposedColleagues = null;
			m_temporaryColleague = null;
			m_propertyTable = null;
			m_commandSet = null;
			m_colleagues = null;
			m_pathVariables = null;
/* It is illegal to try to access managed stuff in this part of the Dispose method.
#if DEBUG
			//DebugMsg("-- Number of calls to the InvokeRecursively method = " + m_invokeCount.ToString());
			DebugMsg("-- Number of saved calls to Type.GetMethod = " + m_SavedCalls.ToString());
			DebugMsg("-- Mediator MsgHash info: count=" + m_MethodsNOTonColleagues.Count + " mx depth=" + m_MethodsCount);
			DebugMsg("-- Mediator  - Calls to check for method on colleague: " + m_MethodChecks);
#endif
*/
			m_isDisposed = true;

			base.Dispose(disposing);
		}

		#endregion IDisposable & Co. implementation

		#region Defered processing Queue items & implementation

		/// <summary>
		/// Gets the idle queue.
		/// </summary>
		/// <value>The idle queue.</value>
		public IdleQueue IdleQueue
		{
			get
			{
				CheckDisposed();
				return m_idleQueue;
			}
		}

		/// <summary>
		/// Retrieve a copy of the current queue of pending messages
		/// </summary>
		public bool IsMessageInPendingQueue(string message)
		{
			CheckDisposed();
			foreach (var task in m_idleQueue)
			{
				var pmi = task.Parameter as PendingMessageItem;
				if (pmi != null && pmi.m_message == message)
					return true;
			}
			return false;
		}

		public bool ProcessMessages
		{
			get
			{
				// JohnT: this is called very often, and the time to CheckDisposed starts to get significant.
				// There appears to be no danger in not checking it. All internal callers call CheckDisposed() somewhere
				// before calling this. The one external caller will die soon after if it needs to, since it calls the setter.
				//CheckDisposed();
				return m_processMessages;
			}
			set
			{
				CheckDisposed();
				m_processMessages = value;
			}
		}

		/// <summary>
		/// Set the handle for the main window
		/// </summary>
		public Form MainWindow
		{
			set
			{
				CheckDisposed();

				if (value == null)
					m_mainWndPtr = IntPtr.Zero;
				else if (value.IsHandleCreated)
					m_mainWndPtr = value.Handle;
				else
					throw new ArgumentException("Form has to have valid handle to use this property.");
			}
		}

		/// <summary>
		/// Set this to true immediately if the mediator needs to post messages to one specific window.  If this is set
		/// to true, then you must set the MainWindow property to the handle of the main window.  Then, after the application
		/// has sufficiently initialized, call the BroadcastPendingItems method.  Note that while the OnHandleCreated method
		/// is a good place to set the MainWindow property, it _will not_ work for calling BroadcastPendingItems().
		/// BroadcastPendingItems() will need to be called some time after the OnHandleCreated method is finished.
		/// No broadcasts will be sent until both MainWindow has been set and BroadcastPendingItems() has been called.
		/// </summary>
		public bool SpecificToOneMainWindow
		{
			set
			{
				CheckDisposed();

				// If we are specific to one main window, then it is not safe to broadcast until BroadcastPendingItems() is called
				m_specificToOneMainWindow = value;
				m_safeToBroadcast = !value;
			}
		}

		// flag set if we are going to have a specific m_mainWindowHandler but don't yet.
		bool m_specificToOneMainWindow = false;
		private bool m_safeToBroadcast = true;
		private IntPtr m_mainWndPtr = IntPtr.Zero;

		/// <summary>This posts a WM_BROADCAST... msg to the main app window</summary>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "Add TODO-Linux comment")]
		private void AddWindowMessage()
		{
			if (!ProcessMessages)
				return;

			// If the application's main windows all use the same mediator (e.g., apps like TE based
			// on FwMainWindow), m_mainWndPtr can be left zero; whatever is the current main window
			// can be allowed to send the ProcessMessages call back to its mediator (which will be this).
			// If different windows use different mediators, then each mediator needs to know which main window
			// it can post messages to. This is currently set up when xWindow creates a mediator;
			// if any other base class is used for windows which have a mediator each, another means will
			// be needed.
			IntPtr mainWndPtr;
			if (m_specificToOneMainWindow)
				mainWndPtr = m_mainWndPtr;
			else
			{
				// TODO-Linux: process.MainWindowHandle is not yet implemented in Mono
				using (var process = Process.GetCurrentProcess())
					mainWndPtr = process.MainWindowHandle;
			}
			if (mainWndPtr != IntPtr.Zero)
				Win32.PostMessage(mainWndPtr, Mediator.WM_BROADCAST_ITEM_INQUEUE, 0, 0);
		}

		/// <summary>Add an item to the queue and let the app know an item is present to be processed.</summary>
		/// <param name="item"></param>
		private void AddQueueItem(QueueItem item)
		{
			if (m_specificToOneMainWindow && (m_mainWndPtr == IntPtr.Zero || !m_safeToBroadcast))
			{
				//if (!m_pendingjobs.Contains(item))
				//{
					m_pendingjobs.Enqueue(item);
				//}
//#if DEBUG
//				else
//				{
//					Debug.WriteLine("Found duplicate pending jobs: " + item.ToString());
//				}
//#endif
			}
			else
			{
				// Review TE team(JohnT/CurtisH): do you need to use pending jobs if Process.GetCurrentProcess().MainWindowHandle is zero?
				//if (!m_jobs.Contains(item))
				//{
					m_jobs.Enqueue(item);	// add the item to the queue
					m_RemovedItemLast = false;
					AddWindowMessage();		// now post the msg to allow future processing of the queue
				//}
//#if DEBUG
//				else
//				{
//					Debug.WriteLine("Found duplicate jobs: " + item.ToString());
//				}
//#endif

			}
		}

		public void BroadcastPendingItems()
		{
			CheckDisposed();

			if (!ProcessMessages)
				return;

			// Make a copy of the queue to prevent an infinitely long queue where we just add the items back into the queue
			// inside the AddQueueItem as we dequeue them here.
			Queue<QueueItem> pendingjobs = new Queue<QueueItem>(m_pendingjobs.ToArray());

			// We can actually broadcast now
			m_safeToBroadcast = true;

			while (pendingjobs.Count > 0)
			{
				QueueItem pending = m_pendingjobs.Dequeue();
				QueueItem pendingCopy = pendingjobs.Dequeue();
				Debug.Assert(pending == pendingCopy);
				AddQueueItem(pendingCopy);
			}
		}

		public int JobItems
		{
			get { return m_jobs.Count; }
		}	// queue count

		/// <summary>Debugging method to show the items on the queue that are new.</summary>
		private void ShowJobItems(int startPos)
		{
			if (showPendingMsgsSwitch.Level == TraceLevel.Verbose)	// only do this if verbose setting
			{
				int pos = 0;
				foreach (QueueItem item in m_jobs)
				{
					if (pos >= startPos || m_RemovedItemLast)
					{
						Trace.WriteLine("Job Queue: " + pos + " - [" +
							item.m_methodName + "]<" +
							(item.m_parameterList[0] == null ? "NULL" : item.m_parameterList[0].ToString()) + ">",
							showPendingMsgsSwitch.DisplayName);
					}
					pos++;
				}
			}
		}

//		[Conditional("DEBUG")]
		private void DebugMsg(string msg)
		{
			// create the initial info:
			// datetime threadid threadpriority: msg
			System.Text.StringBuilder msgOut = new System.Text.StringBuilder();
			msgOut.Append(DateTime.Now.ToString("HH:mm:ss"));
			msgOut.Append("-");
			msgOut.Append(Thread.CurrentThread.GetHashCode());
			msgOut.Append("-");
			msgOut.Append(Thread.CurrentThread.Priority);
			msgOut.Append(": ");
			msgOut.Append(msg);
			System.Diagnostics.Debug.WriteLine(msgOut.ToString());
		}

		private string BuildDebugMsg(string msg)
		{
			// create the initial info:
			// datetime threadid threadpriority: msg
			System.Text.StringBuilder msgOut = new System.Text.StringBuilder();
			msgOut.Append(DateTime.Now.ToString("HH:mm:ss"));
			msgOut.Append("-");
			msgOut.Append(Thread.CurrentThread.GetHashCode());
			msgOut.Append("-");
			msgOut.Append(Thread.CurrentThread.Priority);
			msgOut.Append(": ");
			msgOut.Append(msg);
			return msgOut.ToString();
		}

		/// <summary>Process one of the items in the deferred queue and don't be re-entrant.</summary>
		public bool ProcessItem()
		{
			CheckDisposed();

			if (!ProcessMessages)
				return true;

			bool success = true;	// by default we set our status to having processed an item
			try
			{
				if (m_processItemBusy)	// we're already working, so ignore this call (it will come back later).
				{
					Trace.WriteLineIf(showPendingMsgsSwitch.TraceInfo,
						BuildDebugMsg("Mediator::ProcessItem is busy, come back later."),
						showPendingMsgsSwitch.DisplayName);
					success = false;	// don't reset the busy flag on the way out
					return false;
				}
				m_processItemBusy = true;	// we're now processing an item so set the flag
				if (JobItems == 0)		// only process if the queue isn't empty
				{
					Trace.WriteLineIf(showPendingMsgsSwitch.TraceInfo,
						BuildDebugMsg("Job Queue: Empty"),
						showPendingMsgsSwitch.DisplayName);
					return false;
				}
				m_RemovedItemLast = true;

				// show the queue if it has grown sense last processed (1 or more new itesm added)
				if (m_queueLastProcessed < JobItems)
					ShowJobItems(m_queueLastProcessed);

				QueueItem item = m_jobs.Dequeue();	// extract the item
				m_queueLastProcessed = JobItems;	// save the count for future use
				if (item == null)
					return false;

				Trace.WriteLineIf(showPendingMsgsSwitch.TraceInfo,
					BuildDebugMsg("Job Queue: Processing - [" +	item.m_methodName + "]<" +
					(item.m_parameterList[0] == null ? "NULL" : item.m_parameterList[0].ToString()) + ">"),
					showPendingMsgsSwitch.DisplayName);

				// now call invoke method
				bool rval = InvokeOnColleagues(item.m_methodName, item.m_parameterTypes, item.m_parameterList, item.m_stopWhenHandled, item.m_justCheckingForReceivers);
				AddWindowMessage();	// make sure windows doesn't combine 'n' of our messages into one
				return rval;		// result of invoke method
			}
			catch (DisposedInAnotherFrameException)
			{
				Debug.WriteLine("EXCEPTION: Caught case where the Mediator was disposed of while processing...");
				return true;
			}
			finally
			{
				// always be sure to turn off the processing flag if we're done (success==true)
				if (success)
					m_processItemBusy = false;
			}
		}
		#endregion

		#region MethodNOTonColleague helper methods

		#endregion

		#region command dispatching stuff
		/// <summary>
		/// call this method on any object (which implements it) that we can find
		/// Note that unlike SendMessage(),
		/// this does not care if anyone claims to have "handled" the message.
		/// It will keep sending messages to everyone.
		/// </summary>
		/// <param name="methodName"></param>
		public void BroadcastString(string methodName, string stringParam)
		{
			CheckDisposed();

			if (!ProcessMessages)
				return;

			if (m_colleagues.Count == 0)
				return;		// no one to broadcast to...
#if false
			TraceVerboseLine("Broadcasting  " + methodName);
			InvokeOnColleagues(methodName, new Type[] {typeof(string)}, new Object[] { stringParam }, false, false);
#else
			AddQueueItem(new QueueItem(methodName, new Type[] {typeof(string)}, new Object[] { stringParam }, false, false));
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This version is used to invoke commands
		/// </summary>
		/// <param name="messageName"></param>
		/// <param name="parameter"></param>
		/// <returns><c>true</c> if the message was handled, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool SendMessage(string messageName, object parameter)
		{
			CheckDisposed();

			if (!ProcessMessages)
				return true;

			bool result = false;
			try
			{
				/// Have seen a stack situation that the Mediator has been disposed of after returning from this call...
				///

				result = SendMessage(messageName, parameter, true);
			}
			catch (DisposedInAnotherFrameException)
			{
				Debug.WriteLine("EXCEPTION: Caught case where the Mediator was disposed of while processing...");
				result = true;	// don't process any more
			}
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This version is used to invoke commands
		/// </summary>
		/// <param name="messageName"></param>
		/// <param name="parameter"></param>
		/// <param name="fLogIt">True to log the call (if its not an update or idle), false
		/// otherwise</param>
		/// <returns><c>true</c> if the message was handled, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool SendMessage(string messageName, object parameter, bool fLogIt)
		{
			CheckDisposed();

			if (!ProcessMessages)
				return true;
#if DEBUG
			if(messageName.Substring(0,2) == "On")
				Debug.Fail("The convention is to send messages without the 'On' prefix. " +
					"That is added by the message sending code.");
#endif
			if (messageName != "Idle")
			{
				Trace.WriteLineIf(showPendingMsgsSwitch.TraceVerbose,
					BuildDebugMsg("SendMessage::Looking for listeners for Msg: " + messageName),
					showPendingMsgsSwitch.DisplayName);
			}
			string methodName = "On" + messageName;
			// Logging
			if (!messageName.StartsWith("Update") && messageName != "Idle" && fLogIt)
			{
				// We want to log the method if any colleague handles it.
				// So we check the list of methods known-to-us first. If we don't find it,
				// we check if any colleague implements it by pretending to invoke it.
				// If any colleague implements it, we'll log it; if nobody implements it,
				// it's no use to go any further.
				if (!m_MethodsOnAnyColleague.Contains(methodName))
				{
					// Didn't find it so far, so it must be a method not previously called.
					// Check if anybody implements it.
					if (!InvokeOnColleagues(methodName, new Type[] {typeof(object)},
						new Object[] { new Object() }, true, true))		//we are just checking, don't invoke anything
					{
						return false;
					}
				}
				if (messageName != "DeleteRecordToolTip")	// some messages we don't want to log.
				{
					Logger.WriteEvent("Invoked " + messageName);
				}
			}

			return InvokeOnColleagues(methodName, new Type[] { typeof(object) },
				new Object[] { parameter }, true, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="messageName"></param>
		/// <param name="parameter"></param>
		/// <param name="returnValue"></param>
		/// ------------------------------------------------------------------------------------
		public void SendMessage(string messageName, object parameter,
			ref UIItemDisplayProperties returnValue)
		{
			CheckDisposed();

			if (!ProcessMessages)
				return;

			//this happens on OnIdle(), so
			// it's better to leave commented out
			//TraceVerboseLine("Mediator Looking for listeners for Msg: "+messageName);
			//NB: I could not figure out how to get this to find the method only if the method includes a
			// "ref" parameter, as is the case here, if I specified the types.
			//	Therefore, we supply "null" here.

			try
			{
				InvokeOnColleagues("On" + messageName,
					null, // types see note above
					new Object[] { parameter, returnValue },
					true, false);
			}
			catch (DisposedInAnotherFrameException)
			{
				Debug.WriteLine("EXCEPTION: Caught case where the Mediator was disposed of while processing...");
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks> see all comments in the other SendMessage() method, which apply here.</remarks>
		/// <param name="messageName"></param>
		/// <param name="parameter"></param>
		/// <param name="returnValue"></param>
		public void SendMessage(string messageName, object parameter,
			ref UIListDisplayProperties returnValue)
		{
			CheckDisposed();

			if (!ProcessMessages)
				return;

			Trace.WriteLineIf(showPendingMsgsSwitch.TraceVerbose,
				BuildDebugMsg("SendMessage2::Looking for listeners for Msg: " + messageName),
				showPendingMsgsSwitch.DisplayName);

			//NB: I could not figure out how to get this to find the method only if the method
			// includes a "ref" parameter, as is the case here, if I specified the types.
			//	Therefore, we supply "null" here.

			try
			{
				InvokeOnColleagues("On" + messageName,
					null, // types see note above
					new Object[] { parameter, returnValue },
					true, false);
			}
			catch (DisposedInAnotherFrameException)
			{
				Debug.WriteLine("EXCEPTION: Caught case where the Mediator was disposed of while processing...");
			}
		}

		/// <summary>
		/// This method is a cross of the broadcast and sendmessage types.  It will send
		/// the message to everyone right now with out stoping when it's handled.  This is
		/// provided for places where the previous broadcast functionality is needed, or
		/// when it's not clear and is desired as a first step.
		/// </summary>
		/// <param name="messageName"></param>
		/// <param name="parameter"></param>
		/// <returns>true if one or more colleagues handled the message, else false</returns>
		public bool SendMessageToAllNow(string messageName, object parameter)
		{
			CheckDisposed();

			if (!ProcessMessages)
				return true;

			bool result = false;
			try
			{
				/// Have seen a stack situation that the Mediator has been disposed of after returning from this call...
				///
				result = InvokeOnColleagues("On" + messageName, new Type[] { typeof(object) },
					new Object[] { parameter }, false, false);
			}
			catch (DisposedInAnotherFrameException)
			{
				Debug.WriteLine("EXCEPTION: Caught case where the Mediator was disposed of while processing...");
				result = true;	// don't process any more
			}
			return result;
		}

		/// <summary>
		/// This method is a replacement message for the SendMessage when the return value isn't
		/// actually used.  It allows those messages to be defered for a different message (later).
		/// </summary>
		/// <param name="messageName"></param>
		/// <param name="parameter"></param>
		public void SendMessageDefered(string messageName, object parameter)
		{
			CheckDisposed();

			if (!ProcessMessages)
				return;

#if DEBUG
			if(messageName.Substring(0,2) == "On")
				Debug.Fail("The convention is to send messages without the 'On' prefix. " +
					"That is added by the message sending code.");
#endif
			AddQueueItem(new QueueItem("On" + messageName, new Type[] {typeof(object)},
				new Object[] { parameter }, true, false));
		}

#if TESTING_PCDEFERED
		/// <summary>
		///
		/// </summary>
		/// <param name="odde"></param>
		/// <param name="nchng"></param>
		/// <param name="pct"></param>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		public void PropChangedDefered( PropChangedDelegate pcDelegate,
//										int pct,	//FwViews.PropChangeType pct,
										int hvo,
										int tag,
										int ivMin,
										int cvIns,
										int cvDel)
		{
//			odde.PropChanged(nchng, (int)pct, hvo, tag, ivMin, cvIns, cvDel);
			pcDelegate(hvo, tag, ivMin, cvIns, cvDel);
		}
		public delegate void PropChangedDelegate(int hvo, int tag, int ivMin, int cvIns, int cvDel);
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Post a command to be sent during an upcoming idle moment.
		/// </summary>
		/// <param name="messageName"></param>
		/// <param name="parameter"></param>
		/// ------------------------------------------------------------------------------------
		public void PostMessage(string messageName, object parameter)
		{
			CheckDisposed();

			if (!ProcessMessages)
				return;

			m_idleQueue.Add(IdleQueuePriority.Medium, PostMessageOnIdle, new PendingMessageItem(messageName, parameter), false);
		}

		bool PostMessageOnIdle(object parameter)
		{
			var pmi = (PendingMessageItem) parameter;
			SendMessage(pmi.m_message, pmi.m_parameter);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This version is used to broadcast changes that might affect several objects.
		/// Each of them gets to see it, even if one (or more) claim to have handled it.
		/// </summary>
		/// <param name="messageName"></param>
		/// <param name="parameter"></param>
		/// <returns><c>true</c> if the message was handled, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool BroadcastMessage(string messageName, object parameter)
		{
			CheckDisposed();

#if DEBUG
			if(messageName.Substring(0,2) == "On")
				Debug.Fail("The convention is to send messages without the 'On' prefix. " +
					"That is added by the message sending code.");
#endif
			if (messageName != "Idle")
			{
				Trace.WriteLineIf(showPendingMsgsSwitch.TraceVerbose,
					BuildDebugMsg("BroadcastMessage::Looking for listeners for Msg: " + messageName),
					showPendingMsgsSwitch.DisplayName);
			}
#if false
			return InvokeOnColleagues("On" + messageName, new Type[] {typeof(object)},
				new Object[] { parameter }, false, false);
#else
			AddQueueItem(new QueueItem("On"+messageName, new Type[] {typeof(object)},
				new Object[] { parameter }, false, false));
			return false;
#endif
		}

		/// <summary>
		/// This is a deferred message, but it's not sent to everyone, it is sent until
		/// it is handled.
		/// </summary>
		/// <param name="messageName"></param>
		/// <param name="parameter"></param>
		/// <returns></returns>
		public bool BroadcastMessageUntilHandled(string messageName, object parameter)
		{
			CheckDisposed();

#if DEBUG
			if(messageName.Substring(0,2) == "On")
				Debug.Fail("The convention is to send messages without the 'On' prefix. " +
					"That is added by the message sending code.");
#endif

			AddQueueItem(new QueueItem("On"+messageName, new Type[] {typeof(object)},
				new Object[] { parameter }, true, false));
			return false;
		}

		/// <summary>
		/// Check to see if there would be someone who would receive this command if it was given right now.
		/// </summary>
		/// <param name="messageName"></param>
		public bool HasReceiver(string messageName)
		{
			CheckDisposed();

			if (messageName == null)
				return false; // As might be for a menu separator bar.
#if DEBUG
			if(messageName.Substring(0,2) == "On")
				Debug.Fail("The convention is to send messages without the 'On' prefix.  That is added by the message sending code.");
#endif
			if (messageName != "Idle")
			{
				Trace.WriteLineIf(showPendingMsgsSwitch.TraceVerbose,
					BuildDebugMsg("HasReceiver::Checking for listeners for Msg: " + messageName),
					showPendingMsgsSwitch.DisplayName);
			}


			return InvokeOnColleagues("On"+messageName, new Type[] {typeof(object)},
				new Object[] { new Object() }, //we just make a dummy object here, since we won't really be invoking anything
				true,	//meaningless in this context
				true);		//we are just checking, don't invoke anything
		}

		/// <returns>true if the message was canceled, otherwise false.</returns>
		public bool SendCancellableMessage(string messageName, object parameter)
		{
			CheckDisposed();
#if DEBUG
			if(messageName.Substring(0,2) == "On")
				Debug.Fail("The convention is to send messages without the 'On' prefix.  That is added by the message sending code.");
#endif

			System.ComponentModel.CancelEventArgs cancelArguments = new  System.ComponentModel.CancelEventArgs(false);
			InvokeOnColleagues("On"+messageName,
				null, // types see note above
				new Object[] { parameter, cancelArguments },
				true, false);
			return cancelArguments.Cancel;
		}

		private bool InvokeOnColleagues(string methodName, Type[] parameterTypes,
			object[] parameterList, bool stopWhenHandled, bool justCheckingForReceivers)
		{
			CheckDisposed();

			if (!ProcessMessages)
				return true;

			bool handled = false;
			if (invokeSwitch.TraceVerbose)
			{
				Trace.Indent();
				Trace.WriteLine(BuildDebugMsg("InvokeOnColleagues: methodName=<" + methodName + ">"),
					invokeSwitch.DisplayName);
			}

			try
			{
				//we need to copy our list of colleagues because the list of colleagues may change while we are
				//iterating over this list, which is not allowed if we simply use an enumerater.
				IList<Tuple<int, IxCoreColleague>> targets = new List<Tuple<int, IxCoreColleague>>(m_colleagues.Keys);
				//to catch infinite loops
				var previous = new HashSet<object>(); // Set of IxCoreColleague targets we have tried to send the message to.

				// ms-help://MS.MSDNQTR.v80.en/MS.MSDN.v80/MS.NETDEV.v10.en/dnpag/html/scalenetchapt13.htm
				// .."foreach introduces both managed heap and virtual function overhead..
				// This can be a significant factor in performance-sensitive regions of your application."
				// This section of code is indeed a very performance-sensitive region - so lets pull out
				// the foreach
				// Followup note by RandyR: by the time we get to .Net 2.0, such time savings have become myths,
				// at least for object arryas such as targets.
				// The jury is still out on any performance penalty of foreach when using generics.
				for (int index = 0; index < targets.Count; index++) // foreach (IxCoreColleague host in targets)
				{
					if (!ProcessMessages)
						return true;

					IxCoreColleague host = targets[index].Item2;
					//colleagues can be removed when something (like the window) handles this event
					//so make sure this guy is still legit!
					if (!m_colleagues.ContainsKey(targets[index]))
						continue;

					if (invokeSwitch.TraceVerbose)
						Trace.WriteLine("[" + host.ToString(), invokeSwitch.DisplayName);

					if (invokeSwitch.TraceError) // any tracing at all.
						Trace.Indent();
					handled = InvokeRecursively(host, methodName, parameterTypes, parameterList, previous, stopWhenHandled, justCheckingForReceivers);

					if (invokeSwitch.TraceError) // any tracing at all.
						Trace.Unindent();

					if (!ProcessMessages)
						return true;

					if (invokeSwitch.TraceVerbose)
					{
						Trace.WriteLine("host:" + host.ToString() + " handled methodName=" + handled, invokeSwitch.DisplayName);
						Trace.WriteLine("]", invokeSwitch.DisplayName);
					}

					if (handled)
					{
						m_MethodsOnAnyColleague.Add(methodName);
						if (stopWhenHandled)
						{
							Trace.WriteLineIf(invokeSwitch.TraceVerbose, "-->handled=true And stopWhenHandled=true", invokeSwitch.DisplayName);
							break;
						}
						else
						{
							Trace.WriteLineIf(invokeSwitch.TraceVerbose, "-->handled=true", invokeSwitch.DisplayName);
						}
					}
				}
			}
			finally
			{
				if (invokeSwitch.TraceVerbose)
					Trace.Unindent();
			}
			return handled;
		}

		static long m_invokeCount = 0;
		//static System.Diagnostics.Stopwatch ttime = new Stopwatch();
		/// <summary>
		///
		/// </summary>
		/// <param name="colleague"></param>
		/// <param name="methodName"></param>
		/// <param name="parameterTypes"></param>
		/// <param name="parameterList"></param>
		/// <param name="previous">to catch infinite loops</param>
		private bool InvokeRecursively(IxCoreColleague colleague, string methodName, Type[] parameterTypes,
			object[] parameterList, HashSet<object> previous, bool stopWhenHandled, bool justCheckingForReceivers)
		{

			if (!ProcessMessages)
				return true;

			bool handled = false;
//			Trace.Indent();
			if (invokeSwitch.TraceVerbose)
			{
				Trace.WriteLine("InvokeRecursively: methodName=<" + methodName + "> colleague=<" + colleague.ToString() + ">", invokeSwitch.DisplayName);
			}
			m_invokeCount++;
			//////// THIS IS TESTING CODE ADDED AND COMMENTED AND NEEDS TO BE REVERTED WHEN DONE>>>>>>>>
			//////// ><<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<>
			////////ttime.Start();
			////////ttime.Stop();
			////////if ((m_invokeCount % 1000) == 0)
			////////{
			////////	TimeSpan ts = ttime.Elapsed;
			////////	string tsString = String.Format("{0:00}.{1:0000}({2})",
			////////		ts.Seconds,	ts.Milliseconds, ts.Ticks );
			////////	Trace.WriteLine(tsString + " ColleagueHasBeenDisposed(" + colleague.ToString() + ")="+chbDisposed.ToString());
			////////	ttime.Reset();
			////////}
			if (colleague.ShouldNotCall)
			{
				DebugMsg("_+_+_+_+ InvokeRecursively called on colleague that is disposed/disposing: " + colleague.ToString());
				return false;	// stop the processing
			}

			IxCoreColleague[] targets = colleague.GetMessageTargets();
			// Try following the 'Code Performance' guidelines which says that
			// .."foreach introduces both managed heap and virtual function overhead..
			// This can be a significant factor in performance-sensitive regions of your application."
			// This section of code is indeed a very performance-sensitive region!
			//
			for (int index = 0; index < targets.Length; index++) // foreach(IxCoreColleague target in targets)
			{
				if (!ProcessMessages)
					return true;

				IxCoreColleague target = targets[index];
				if(target == null)
				{
					//Debug.WriteLine("Warning, target null.");
					continue;
				}

				//this section is the leaf of the search tree
				if (target == colleague)
				{
					//Check to see whether we have encountered this target before.
					//how can we encounter the same one twice?
					//This will happen when more than one colleague includes some shared object as one of its children.
					//in xWorks, this happens with the RecordClerk, which is not a top-level colleague on its own.
					// The following is logically equivalent to
					//if (previous.Contains(target))
					//{
					//	break;
					//}
					//previous.Add(target);
					// but faster.
					int oldCount = previous.Count;
					previous.Add(target);
					if (oldCount == previous.Count)
						break; // it was already present, that is, we've processed it before.

					MethodInfo mi = CheckForMatchingMessage(colleague, methodName, parameterTypes);
					if (mi != null)
					{
						if (justCheckingForReceivers)
						{
							handled = true;
							break;
						}
						else
						{
							if (methodName == "OnMasterRefresh")
							{
								InvokeMethod(target, mi, parameterList);
								handled = true;
							}
							else
							{
								object o = InvokeMethod(target, mi, parameterList);
								handled = (o != null) ? (bool) o : false;
							}
						}
					}
					else
					{
						m_SavedCalls++;
					}
				}
				else //not at a leaf yet, keep going down the tree
					handled = InvokeRecursively(target, methodName, parameterTypes, parameterList, previous, stopWhenHandled, justCheckingForReceivers);

				if(handled && stopWhenHandled)
				{
					Trace.WriteLineIf(invokeSwitch.TraceVerbose, "-->handled=true And stopWhenHandled=true", invokeSwitch.DisplayName);
					break;
				}
				else if(handled)
					Trace.WriteLineIf(invokeSwitch.TraceVerbose, "-->handled=true", invokeSwitch.DisplayName);

			}
//			TraceVerboseLine("}");
//			Trace.Unindent();
			return handled;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="target"></param>
		/// <param name="methodName"></param>
		/// <param name="parameterTypes">Currently, use null here if you have a ref param</param>
		/// <param name="parameterList"></param>
		/// <returns>null or the MethodInfo if a matching one was found</returns>
		private MethodInfo CheckForMatchingMessage(IxCoreColleague target, string methodName, Type[] parameterTypes)
		{
			//NB: I did not think about these flags; I just copied them from an example
			BindingFlags flags =
				BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;

			Type type = target.GetType();
			MethodInfo mi;
			//
			// By using the JetBrains dotTrace profiler, the addition of m_TypeMethodInfo is saving
			// a significant (>20%) amount of time by not having to make as many calls to the expensive
			// System.Type.GetMethod()
			//
			if (parameterTypes == null) // callers currently must use null here if they have a "ref" param (TODO)
			{
				Dictionary<string, MethodInfo> methodDict;
				if (m_TypeMethodInfo.TryGetValue(type, out methodDict))
				{
					if (methodDict.TryGetValue(methodName, out mi))
						return mi;
				}
				else
				{
					methodDict = new Dictionary<string, MethodInfo>();
					m_TypeMethodInfo[type] = methodDict;
				}
				mi = type.GetMethod(methodName, flags);
				methodDict[methodName] = mi;
			}
			else
			{
				var key = parameterTypes.Length + methodName; // method name could end with number, but not start.
				Dictionary<string, MethodInfo> methodDict;
				if (m_TypeMethodInfo.TryGetValue(type, out methodDict))
				{
					if (methodDict.TryGetValue(key, out mi))
						return mi;
				}
				else
				{
					methodDict = new Dictionary<string, MethodInfo>();
					m_TypeMethodInfo[type] = methodDict;
				}
				mi = type.GetMethod(methodName, flags, null, parameterTypes, null);
				methodDict[key] = mi;
			}
			return mi;
		}

		/// <summary>
		/// Exception that is a special case where the Mediator is working,
		/// but due to re-entrance has been disposed of in a stack frame that
		/// is no longer active, but as a result we are in a disposed of state
		/// and should go straight to the end of our current call frame: SendMessage
		/// </summary>
		class DisposedInAnotherFrameException : Exception {}

		private object InvokeMethod(IxCoreColleague target, MethodInfo mi, object[] parameterList)
		{
			if (!ProcessMessages)
				return null;

			try
			{
				Trace.Indent();

				if (invokeSwitch.TraceInfo)
				{
					if (parameterList.Length > 0 && parameterList[0] != null)
						Trace.WriteLine(" Invoking Method: " + mi.Name + "('" + parameterList[0].ToString() + "')" + " on " + target.ToString(), invokeSwitch.DisplayName);
					else
						Trace.WriteLine(" Invoking Method: " + mi.Name + " on " + target.ToString(), invokeSwitch.DisplayName);
				}
#if false
				string objName = "";
				objName = target.ToString() + target.GetHashCode().ToString();
				// DLH TESTING - not finished yet...
				if (IsDisposedColleague(objName))
				{
					Debug.WriteLine("##Not Invoking disposed object:"+objName);
					return null;
				}
#endif
				/// *****************************************************************************
				/// Have seen a stack situation that the Mediator has been disposed of after
				/// returning from this call - IOW's the following call allows re-entrance.
				/// That's why the exception follows to handle a known case when processing the
				/// ExitApplication msg.
				/// *****************************************************************************
				object returnValue = mi.Invoke(target, parameterList);
				if (m_isDisposed)
					throw new DisposedInAnotherFrameException();

				if (target == m_temporaryColleague && !mi.Name.StartsWith("OnDisplay"))
				{
					RemoveColleague(m_temporaryColleague);
					m_temporaryColleague = null;	// only keep one temporary colleague at a time (menu based)
				}

				Trace.Unindent();

				return returnValue;
			}
				//note that we don't want to catch just any kind of the exception here,
				//most exceptions will be invoked by the method that we actually called.
				//the only exceptions we want to catch are the ones that just mean that we failed to find
				//a suitable method. These we can report if in debug-mode, otherwise ignore.
			catch(System.ArgumentException error)
			{
				//I once spent close to an hour wondering what was causing the failure here.
				//The exception message was "Object type cannot be converted to target type."
				//the answer was that I had made the signature be a UIListDisplayProperties
				//when it should have been a UIItemDisplayProperties. The two can be pretty hard to
				//distinguish visually. (John Hatton)
				Debug.Fail("The method '"+mi.Name+"' was found but couldn't be invoked. Maybe has the wrong signature?", error.Message);
			}
//			catch(ConfigurationException error)
//			{
//				throw error; //already has good user notification message in it
//			}
//			catch(RuntimeConfigurationException error)
//			{
//				throw error; //already has good user notification message in it
//			}
			catch(TargetInvocationException error)
			{
				Exception inner = error.InnerException;	//unwrap, for example, a ConfigurationException
				// See LT-1629 "Closing one db while opening another crashes".  The following
				// two lines  appear to fix this bug, although I'm not too happy about this
				// asynchronous behavior appearing where we (or at least I) don't expect it.
				// Unfortunately, that's inherent with message handling architecture when
				// handling one message allows other messages to be handled before it finishes.
				// - SteveMc
				if (inner is System.NullReferenceException &&
					mi.Name == "OnChooseLangProject")
				{
					// We probably closed the target's window after choosing another project,
					// but before getting to this point in processing the ChooseLP message.  So
					// ignore the exception.
					return null;
				}
				string s = "Something went wrong trying to invoke "+ target.ToString() +":"+ mi.Name +"().";
				//if we just send on the error, then the caller
				//will find it more easy to trap particular kind of exceptions. On the other hand,
				//if the exception makes it all the way to the user, then we will really want to see this extra string (s)
				//at the top level.

				throw new Exception(s, inner);

				//if we were to just bring up the green box), then that makes it impossible for the caller to catch
				//the exception. In particular, the link-jumping column can fail if the object it is trying to jump to
				//has been deleted. This is really not an "error", and if we let the exception get back to the
				//jumping code, then it can notify the user more calmly.
				//SIL.Utils.ErrorReporter.ReportException(new ApplicationException(s, inner));
			}
			return null;
		}

		#endregion

		#region  properties

		public CommandSet CommandSet
		{
			get
			{
				CheckDisposed();
				return m_commandSet;
			}
		}

		public PropertyTable PropertyTable
		{
			get
			{
				CheckDisposed();
				return m_propertyTable;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the help topic provider from the mediator's properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IHelpTopicProvider HelpTopicProvider
		{
			get
			{
				CheckDisposed();
				return (IHelpTopicProvider)PropertyTable.GetValue("HelpTopicProvider");
			}
			set
			{
				CheckDisposed();
				PropertyTable.SetProperty("HelpTopicProvider", value);
				PropertyTable.SetPropertyPersistence("HelpTopicProvider", false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the feedback information provider from the mediator's properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IFeedbackInfoProvider FeedbackInfoProvider
		{
			get
			{
				CheckDisposed();
				return (IFeedbackInfoProvider)PropertyTable.GetValue("FeedbackInfoProvider");
			}
			set
			{
				CheckDisposed();
				PropertyTable.SetProperty("FeedbackInfoProvider", value);
				PropertyTable.SetPropertyPersistence("FeedbackInfoProvider", false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// a look up table for getting the correct version of strings that the user will see.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringTable StringTbl
		{
			get
			{
				CheckDisposed();
				object table = PropertyTable.GetValue("stringTable");
				if (table == null)
					throw new ConfigurationException("Could not get the StringTable. Make sure there is at least an 'strings-en.xml' in the configuration directory.");
				return (StringTable)table;
			}
			set
			{
				PropertyTable.SetProperty("stringTable", value);
				PropertyTable.SetPropertyPersistence("stringTable", false);
			}
		}

		/// <summary>
		/// This property can be used to see if the String Table has been set
		/// without triggering an exception.
		/// </summary>
		public bool IsStringTableSet
		{
			get
			{
				try
				{
					var table = this.StringTbl;
					return true;
				}
				catch (ConfigurationException)
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Check whether we have a string table stored and available for use.
		/// </summary>
		public bool HasStringTable
		{
			get
			{
				object table = PropertyTable.GetValue("stringTable");
				return table != null;
			}
		}

		public Dictionary<string, string> PathVariables
		{
			get
			{
				CheckDisposed();
				return m_pathVariables;
			}
		}

		#endregion

		#region Other methods
		public string GetRealPath(string path)
		{
			CheckDisposed();
			foreach(KeyValuePair<string, string> kvp in  m_pathVariables)
			{
				string key = kvp.Key;
				if (path.Contains(key))
				{
					path = path.Replace(key, kvp.Value);
					break;
				}
			}
			return path;
		}
		#endregion

		#region Colleague handling methods

		public string GetColleaguesDumpString()
		{
			CheckDisposed();

			StringBuilder sb = new StringBuilder("");
			foreach (Tuple<int, IxCoreColleague> icc in m_colleagues.Keys)
			{
				sb.AppendLine(icc.ToString());
			}
			return sb.ToString();
		}

		public void AddTemporaryColleague(IxCoreColleague colleague)
		{
			CheckDisposed();

			if (m_temporaryColleague != null)
				RemoveColleague(m_temporaryColleague);

			m_temporaryColleague = colleague;	// only keep one temporary colleague at a time (menu based)
			AddColleague(m_temporaryColleague);
		}

		public void AddColleague(IxCoreColleague colleague)
		{
			CheckDisposed();

			Tuple<int, IxCoreColleague> pair = new Tuple<int, IxCoreColleague>(colleague.Priority, colleague);
			// Note: m_colleagues is now a Dictionary of Tuples, so would ignore the attempt to add it again.
			// The problem with that is it is really a programming error to add them more than once.
			// So, we will keep the exception.
			if (m_colleagues.ContainsKey(pair))
				throw new ApplicationException ("This object is already in the list of colleagues.");

			m_colleagues.Add(pair, true);
		}

		public void RemoveColleague(IxCoreColleague colleague)
		{
			CheckDisposed();
			// No need to check if m_colleagues is null.
			// if it hasn't been disposed it will always be non-null.
			// If it has been disposed, then the caller needs be fixed to prevent calling disposed objects.
			m_colleagues.Remove(new Tuple<int, IxCoreColleague>(colleague.Priority, colleague));
		}
		#endregion

		#region Trace and Log methods
#if false
		private void TraceVerboseLine(string s)
		{
			if (m_traceSwitch != null && m_traceSwitch.TraceVerbose)
				Trace.WriteLine("MID="+System.Threading.Thread.CurrentThread.GetHashCode()+": "+s);
		}
		private void TraceInfoLine(string s)
		{
			if (m_traceSwitch != null && m_traceSwitch.TraceInfo || m_traceSwitch.TraceVerbose)
				Trace.WriteLine("MID="+System.Threading.Thread.CurrentThread.GetHashCode()+": "+s);
		}
		public TraceLevel TraceLevel
		{
			set
			{
				CheckDisposed();
				m_traceSwitch.Level = value;
			}
			get
			{
				CheckDisposed();
				return m_traceSwitch.Level;
			}
		}
#endif
		#endregion
	}
}
