// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2008' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FocusMessageHandling.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	/// <summary>Handler for reference changing events.</summary>
	public delegate void ReferenceChangedHandler(object sender, ScrReference newRef, ITsString selectedText);
	/// <summary>Handler for annotations changing events</summary>
	public delegate void AnnotationChangedHandler(object sender, IScrScriptureNote newAnn);
	/// <summary>Handler for IP location in TE editing view has changed</summary>
	public delegate void ScrEditingLocationChangedHandler(object sender, TeEditingHelper editingHelper);

	#region FocusMessageHandling class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class that deals with handling the focus messages for synchronized scrolling
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FocusMessageHandling: IDisposable
	{
		#region Constants
		private const bool StartLibronix = false;
		private const int LibronixLinkSet = 0;
		#endregion

		#region Member variables
		private ILogosPositionHandler m_positionHandler;
		private EventHandler<PositionChangedEventArgs> m_eventHandler;
		private IScrRefTracker m_refTracker;

		/// <summary>Set to true while we are processing a received sync message</summary>
		private bool m_fProcessingSyncMessage;

		/// <summary>Queued Scripture reference in English versification scheme. If we get
		/// another sync message while we are processing sync messages, we store it in this
		/// variable so that we can update to the latest position.</summary>
		private ScrReference m_queuedReference;

		/// <summary>Set to true if we just sent a Santa Fe sync message and we should ignore
		/// the next received message (because we also sent to ourself)</summary>
		private bool m_fIgnoreNextRecvdSantaFeSyncMessage;
		/// <summary>Set to true if we just sent a Libronix sync message and we should ignore
		/// the next received message (because Libronix will respond with a PositionChanged
		/// event.</summary>
		private bool m_fIgnoreNextRecvdLibronixSyncMessage;
		/// <summary>Set to true to ignore any sync messages sent or received</summary>
		private bool m_fIgnoreAnySyncMessages;

		/// <summary>
		/// This event gets fired when we're receiving a message from outside that
		/// an application has gone to a specific reference.
		/// </summary>
		public event ReferenceChangedHandler ReferenceChanged;

		/// <summary>
		/// This event gets fired when the annotation in the notes window has changed.
		/// </summary>
		public event AnnotationChangedHandler AnnotationChanged;

		/// <summary>
		/// This event gets fired when the IP in one of the scripture views changes.
		/// </summary>
		public event ScrEditingLocationChangedHandler ScrEditingLocationChanged;

		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FocusMessageHandling"/> class.
		/// </summary>
		/// <param name="refTracker">The scripture reference tracker to be used for handling
		/// reference changes</param>
		/// ------------------------------------------------------------------------------------
		public FocusMessageHandling(IScrRefTracker refTracker)
		{
			if (refTracker == null)
				throw new ArgumentNullException("refTracker");
			m_refTracker = refTracker;
			LogosPositionHandlerFactory.Created += OnLogosPositionHandlerCreated;
		}

		#endregion

		#region Dispose related methods
		#if DEBUG
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="T:FocusMessageHandling"/> is reclaimed by garbage collection.
		/// </summary>
		/// <developernote>
		/// We don't implement a finalizer in production code. Finalizers should only be
		/// implemented if we have to free unmanaged resources. We have COM objects, but those
		/// are wrapped in a managed wrapper, so they aren't considered unmanaged here.
		/// <see href="http://code.logos.com/blog/2008/02/the_dispose_pattern.html"/>
		/// </developernote>
		/// ------------------------------------------------------------------------------------
		~FocusMessageHandling()
		{
			Dispose(false);
		}
		#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="fDisposeManagedObjs"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool fDisposeManagedObjs)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposeManagedObjs, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposeManagedObjs)
			{
				LogosPositionHandlerFactory.Created -= OnLogosPositionHandlerCreated;

				if (m_positionHandler != null)
				{
					m_positionHandler.PositionChanged -= m_eventHandler;
					m_positionHandler.Dispose();
				}
			}

			m_positionHandler = null;
			m_eventHandler = null;
			m_refTracker = null;
		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Libronix linker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ILogosPositionHandler LibronixLinker
		{
			get
			{
				if (MiscUtils.IsUnix)
					return null;

				if (m_positionHandler == null && !LogosPositionHandlerFactory.IsNotInstalled)
				{
					try
					{
						m_positionHandler = LogosPositionHandlerFactory.CreateInstance(
							StartLibronix, LibronixLinkSet, false);
						if (m_positionHandler != null)
						{
							OnLogosPositionHandlerCreated(null,
								new CreatedEventArgs {PositionHandler = m_positionHandler});
						}
					}
					catch (ApplicationException)
					{
					}
				}
				return m_positionHandler;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the logos position handler got created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OnLogosPositionHandlerCreated(object sender, CreatedEventArgs e)
		{
			m_positionHandler = e.PositionHandler;
			m_eventHandler = new EventHandler<PositionChangedEventArgs>(OnPositionChanged);
			m_positionHandler.PositionChanged += m_eventHandler;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether [enable libronix linking].
		/// </summary>
		/// <value>
		/// 	<c>true</c> if [enable libronix linking]; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool EnableLibronixLinking
		{
			set
			{
				if (value)
				{
					// creates the object and thus enables polling.
					var dummy = LibronixLinker;
				}
				else if (m_positionHandler != null)
				{
					m_positionHandler.Dispose();
					m_positionHandler = null;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the position in Libronix changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:SIL.Utils.PositionChangedEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnPositionChanged(object sender, PositionChangedEventArgs e)
		{
			if (!TeProjectSettings.ReceiveSyncMessages || m_fIgnoreNextRecvdLibronixSyncMessage ||
				m_fProcessingSyncMessage || m_fIgnoreAnySyncMessages)
			{
				if (m_fProcessingSyncMessage)
					m_queuedReference = new ScrReference(e.BcvRef, ScrVers.English);

				m_fIgnoreNextRecvdLibronixSyncMessage = false;
			}
			else
				ProcessReceivedMessage(new ScrReference(e.BcvRef, ScrVers.English));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the received sync message.
		/// </summary>
		/// <param name="reference">The reference in English versification scheme.</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessReceivedMessage(ScrReference reference)
		{
			Debug.Assert(reference.Versification == ScrVers.English);

			// While we process the given reference we might get additional synch events, the
			// most recent of which we store in m_queuedReference. If we're done
			// and we have a new reference in m_queuedReference we process that one, etc.
			for (; reference != null; reference = m_queuedReference)
			{
				m_queuedReference = null;
				m_fProcessingSyncMessage = true;

				try
				{
					if (ReferenceChanged != null && m_refTracker != null &&
						m_refTracker.CurrentRef != reference && reference.Valid)
					{
						ReferenceChanged(null, new ScrReference(reference), null);
					}
				}
				finally
				{
					m_fProcessingSyncMessage = false;
				}
			}
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Notifies anyone who cares to synchronize to the specified annotation's begin
		/// reference.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="annotation">The annotation.</param>
		/// <param name="scr">The IScrScripture to which the annotation belongs.</param>
		/// ------------------------------------------------------------------------------------
		public void SyncToAnnotation(object sender, IScrScriptureNote annotation, IScripture scr)
		{
			Debug.Assert(annotation != null);

			if (m_fIgnoreAnySyncMessages || m_fProcessingSyncMessage)
				return;

			if (AnnotationChanged != null)
				AnnotationChanged(sender, annotation);

			var scrRef = new ScrReference(annotation.BeginRef, scr.Versification);
			if (scrRef.Valid || scrRef.IsBookTitle)
			{
				m_refTracker.SetCurrentReference(new ScrReference(scrRef));
				SendExternalSyncMessage(scrRef);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Broadcasts a message telling listeners to synchronize to the specified reference
		/// and selected text.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="reference">The reference.</param>
		/// <param name="tssSelectedText">The selected text.</param>
		/// <param name="fSendInternalOnly"><c>true</c> to indicate that this message should
		/// not be broadcast to external apps (e.g., Libronix and Santa Fe apps).</param>
		/// ------------------------------------------------------------------------------------
		public void SyncToReference(object sender, ScrReference reference,
			ITsString tssSelectedText, bool fSendInternalOnly)
		{
			if ((!reference.Valid && !reference.IsBookTitle) || m_fIgnoreAnySyncMessages || m_fProcessingSyncMessage)
				return;

			// Make a new copy because otherwise changing the versification will
			// change the reference passed to us and that could mess up the caller.
			var scrRef = new ScrReference(reference);

			if (ReferenceChanged != null)
				ReferenceChanged(sender, new ScrReference(scrRef), tssSelectedText);

			if (!fSendInternalOnly)
			{
				m_refTracker.SetCurrentReference(new ScrReference(scrRef));
				SendExternalSyncMessage(scrRef);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Syncs to the current Scripture reference location of the given editing helper.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="editingHelper">The editing helper of the scripture view in which
		/// the IP location changed.</param>
		/// <param name="fSendInternalOnly"><c>true</c> to indicate that this message should
		/// not be broadcast to external apps (e.g., Libronix and Santa Fe apps).</param>
		/// ------------------------------------------------------------------------------------
		public void SyncToScrLocation(object sender, TeEditingHelper editingHelper,
			bool fSendInternalOnly)
		{
			Debug.Assert(editingHelper != null);

			if (m_fIgnoreAnySyncMessages || m_fProcessingSyncMessage)
				return;

			if (ScrEditingLocationChanged != null)
				ScrEditingLocationChanged(sender, editingHelper);

			var scrRef = new ScrReference(editingHelper.CurrentStartRef);
			if (!fSendInternalOnly && (scrRef.Valid || scrRef.IsBookTitle))
			{
				m_refTracker.SetCurrentReference(new ScrReference(scrRef));
				SendExternalSyncMessage(scrRef);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Receive from Santa Fe windows any changes in Scripture Reference.
		/// </summary>
		/// <param name="msg">The windows message.</param>
		/// ------------------------------------------------------------------------------------
		public void ReceiveFocusMessage(Message msg)
		{
			// Always assume the English versification scheme for passing references.
			var scrRef = new ScrReference(
				SantaFeFocusMessageHandler.ReceiveFocusMessage(msg), ScrVers.English);

			if (!TeProjectSettings.ReceiveSyncMessages || m_fIgnoreNextRecvdSantaFeSyncMessage ||
				m_fProcessingSyncMessage || m_fIgnoreAnySyncMessages)
			{
				if (m_fProcessingSyncMessage)
					m_queuedReference = scrRef;

				m_fIgnoreNextRecvdSantaFeSyncMessage = false;
				return;
			}

			ProcessReceivedMessage(scrRef);
		}
		#endregion

		#region Misc. private sync. message sending methods.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sends a sync. message to external application who may be monitoring a Santa Fe
		/// message (e.g. Paratext) or a Libronix message.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SendExternalSyncMessage(ScrReference scrRef)
		{
			if (!TeProjectSettings.SendSyncMessages)
				return;

			m_fIgnoreNextRecvdSantaFeSyncMessage = true;
			m_fIgnoreNextRecvdLibronixSyncMessage = true;
			VersificationTable.Get(ScrVers.English).ChangeVersification(scrRef);
			SantaFeFocusMessageHandler.SendFocusMessage(scrRef.ToString());

			// Make a new copy each time in case callee does something that modifies it.
			SyncLibronix(new ScrReference(scrRef));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send a synchronize message to Libronix for the specified reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SyncLibronix(ScrReference reference)
		{
			Debug.Assert(reference.Versification == ScrVers.English);

			if (LibronixLinker != null)
				LibronixLinker.SetReference(reference.BBCCCVVV);
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to ignore any sync messages.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IgnoreAnySyncMessages
		{
			get { return m_fIgnoreAnySyncMessages; }
			set { m_fIgnoreAnySyncMessages = value; }
		}

		#endregion
	}
	#endregion

	#region IgnoreSynchMessages class
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Helper class to allow temporary ignorage of synch messages
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public sealed class IgnoreSynchMessages : IDisposable
	{
		private readonly IScrRefTracker m_refTracker;
		private readonly bool m_origIgnoreAnySyncMessages;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:IgnoreSynchMessages"/> class.
		/// </summary>
		/// <param name="mainWnd">The main window (which is useful to us only if it turns
		/// out to implement IScrRefTracker.</param>
		/// --------------------------------------------------------------------------------
		public IgnoreSynchMessages(FwMainWnd mainWnd)
		{
			m_refTracker = mainWnd as IScrRefTracker;
			if (m_refTracker != null)
			{
				m_origIgnoreAnySyncMessages = m_refTracker.IgnoreAnySyncMessages;
				m_refTracker.IgnoreAnySyncMessages = true;
			}
		}

		#region IDisposable Members
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Resets the IgnoreAnySyncMessages setting.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public void Dispose()
		{
			if (m_refTracker != null)
				m_refTracker.IgnoreAnySyncMessages = m_origIgnoreAnySyncMessages;
			GC.SuppressFinalize(this);
		}
		#endregion
	}
	#endregion
}
