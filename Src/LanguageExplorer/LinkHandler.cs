// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Areas;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;

namespace LanguageExplorer
{
	/// <summary>
	/// LinkHandler handles Hyper linking and history.
	/// See the class comment on FwLinkArgs for details on how all the parts of hyperlinking work.
	/// </summary>
	internal sealed class LinkHandler : IDisposable
	{
		// Limit the stacks to 50 elements (LT-729).
		private const string FollowLink = "FollowLink";
		const int kmaxDepth = 50;
		// Used to count the number of times we've been asked to suspend Idle processing.
		private int _countSuspendIdleProcessing;
		private LcmCache _cache;
		private LinkedList<FwLinkArgs> _backStack;
		private LinkedList<FwLinkArgs> _forwardStack;
		private bool _followingLink;
		private int _backStackOrig;
		private FwLinkArgs _activeFwLinkArgs;
		private bool _usingHistory;
		private IPropertyTable _propertyTable;
		private IPublisher _publisher;
		private ISubscriber _subscriber;

		/// <summary />
		internal LinkHandler(FlexComponentParameters flexComponentParameters, LcmCache cache, GlobalUiWidgetParameterObject globalUiWidgetParameterObject)
		{
			Guard.AgainstNull(flexComponentParameters, nameof(flexComponentParameters));
			Guard.AgainstNull(cache, nameof(cache));
			Guard.AgainstNull(globalUiWidgetParameterObject, nameof(globalUiWidgetParameterObject));

			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(_propertyTable, _publisher, _subscriber));

			_propertyTable = flexComponentParameters.PropertyTable;
			_publisher = flexComponentParameters.Publisher;
			_subscriber = flexComponentParameters.Subscriber;

			_cache = cache;
			_propertyTable.SetProperty(LanguageExplorerConstants.LinkHandler, this);
			_subscriber.Subscribe(FollowLink, FollowLink_Handler);
			_subscriber.Subscribe(FwUtils.HandleLocalHotlink, HandleLocalHotlink_Handler);
			// CmdHistoryBack and CmdHistoryForward are on the standard tool strip
			var standardToolBarDictionary = globalUiWidgetParameterObject.GlobalToolBarItems[ToolBar.Standard];
			standardToolBarDictionary.Add(Command.CmdHistoryBack, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(HistoryBack_Clicked, ()=> CanCmdHistoryBack));
			standardToolBarDictionary.Add(Command.CmdHistoryForward, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(HistoryForward_Clicked, () => CanCmdHistoryForward));
			// CmdCopyLocationAsHyperlink is on the Edit menu.
			globalUiWidgetParameterObject.GlobalMenuItems[MainMenu.Edit].Add(Command.CmdCopyLocationAsHyperlink, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CopyLocationAsHyperlink_Clicked, ()=> UiWidgetServices.CanSeeAndDo));
			_backStack = new LinkedList<FwLinkArgs>();
			_forwardStack = new LinkedList<FwLinkArgs>();
			CurrentContext = null;
		}

		private static void Push(LinkedList<FwLinkArgs> stack, FwLinkArgs context)
		{
			stack.AddLast(context);
			while (stack.Count > kmaxDepth)
			{
				stack.RemoveFirst();
			}
		}

		private static FwLinkArgs Pop(LinkedList<FwLinkArgs> stack)
		{
			var lnk = stack.Last.Value;
			stack.RemoveLast();
			return lnk;
		}

		/// <summary>
		/// Return the current link.
		/// </summary>
		public FwLinkArgs CurrentContext { get; private set; }

		/// <summary>
		/// Handle the specified link.
		/// </summary>
		private void HandleLocalHotlink_Handler(object source)
		{
			var localLinkArgs = (LocalLinkArgs)source;
			var url = localLinkArgs.Link;
			if (!url.StartsWith(FwLinkArgs.kFwUrlPrefix))
			{
				return; // we can't handle it, but no other colleague can either. Needs to launch whatever can (see VwBaseVc.DoHotLinkAction).
			}
			try
			{
				var fwAppArgs = new FwAppArgs(url);
				if (SameDatabase(fwAppArgs, _cache.ProjectId))
				{
					FollowLink_Handler(fwAppArgs);
					localLinkArgs.LinkHandledLocally = true;
				}
			}
			catch (Exception)
			{
				// Something went wrong, probably its not a kind of link we understand.
			}
		}

		private static bool SameDatabase(FwAppArgs fwAppArgs, IProjectIdentifier projectIdentifier)
		{
			return fwAppArgs.Database == "this$"
				   || string.Equals(fwAppArgs.Database, projectIdentifier.Name, StringComparison.InvariantCultureIgnoreCase)
				   || string.Equals(fwAppArgs.Database, projectIdentifier.Path, StringComparison.InvariantCultureIgnoreCase)
				   || string.Equals(Path.GetFileName(fwAppArgs.Database), projectIdentifier.Name, StringComparison.InvariantCultureIgnoreCase);
		}

		/// <summary>
		/// Add a new link to the history.
		/// </summary>
		internal void AddLinkToHistory(FwLinkArgs newHistoryLink)
		{
			if (newHistoryLink.EssentiallyEquals(CurrentContext))
			{
				return;
			}
			if (CurrentContext != null && (!_forwardStack.Any() || !CurrentContext.EssentiallyEquals(_forwardStack.Last.Value)))
			{
				Push(_backStack, CurrentContext);
			}
			// Try to omit intermediate targets which are added to the stack when switching
			// tools.  This doesn't work in OnFollowLink() because the behavior of following
			// the link is not synchronous even when SendMessage is used at the first two
			// levels of handling.
			if (_followingLink && newHistoryLink.EssentiallyEquals(_activeFwLinkArgs))
			{
				var howManyAdded = _backStack.Count - _backStackOrig;
				for (; howManyAdded > 1; --howManyAdded)
				{
					_backStack.RemoveLast();
				}
				_followingLink = false;
				_backStackOrig = 0;
				_activeFwLinkArgs = null;
			}
			// The forward stack should be cleared by jump operations that are NOT spawned by
			// a Back or Forward (ie, history) operation.  This is the standard behavior in
			// browsers, for example (as far as I know).
			if (_usingHistory)
			{
				if (newHistoryLink.EssentiallyEquals(_activeFwLinkArgs))
				{
					_usingHistory = false;
					_activeFwLinkArgs = null;
				}
			}
			else
			{
				_forwardStack.Clear();
			}
			CurrentContext = newHistoryLink;
		}

		/// <summary>
		/// Handle the "Copy Location as Hyperlink" menu
		/// </summary>
		private void CopyLocationAsHyperlink_Clicked(object sender, EventArgs e)
		{
			if (CurrentContext == null)
			{
				return;
			}
			var fwAppArgs = new FwAppArgs(_cache.ProjectId.Handle, CurrentContext.ToolName, CurrentContext.TargetGuid);
			ClipboardUtils.SetDataObject(fwAppArgs.ToString(), true);
		}

		private Tuple<bool, bool> CanCmdHistoryBack => new Tuple<bool, bool>(true, _backStack.Any());

		/// <summary />
		private void HistoryBack_Clicked(object sender, EventArgs e)
		{
			if (!_backStack.Any())
			{
				return;
			}
			if (CurrentContext != null)
			{
				Push(_forwardStack, CurrentContext);
			}
			_usingHistory = true;
			_activeFwLinkArgs = Pop(_backStack);
			FollowActiveLink();
		}

		private Tuple<bool, bool> CanCmdHistoryForward => new Tuple<bool, bool>(true, _forwardStack.Any());

		/// <summary />
		private void HistoryForward_Clicked(object sender, EventArgs e)
		{
			if (!_forwardStack.Any())
			{
				return;
			}
			_usingHistory = true;
			_activeFwLinkArgs = Pop(_forwardStack);
			FollowActiveLink();
		}

		/// <summary>
		/// NOTE: This will not handle link requests for other databases/applications. To handle other
		/// databases or applications, pass a FwAppArgs to the IFieldWorksManager.HandleLinkRequest method.
		/// </summary>
		private void FollowLink_Handler(object lnk)
		{
			_followingLink = true;
			_backStackOrig = _backStack.Count;
			_activeFwLinkArgs = (FwLinkArgs)lnk;
			FollowActiveLink();
		}

		private void FollowActiveLink()
		{
			try
			{
				if (_activeFwLinkArgs.TargetGuid != Guid.Empty)
				{
					_propertyTable.SetProperty(LanguageExplorerConstants.SuspendLoadingRecordUntilOnJumpToRecord, $"{_activeFwLinkArgs.ToolName},{_activeFwLinkArgs.TargetGuid}", settingsGroup: SettingsGroup.LocalSettings);
				}
				var messages = new List<string>();
				var newValues = new List<object>();
				messages.Add(LanguageExplorerConstants.SetToolFromName);
				newValues.Add(_activeFwLinkArgs.ToolName);
				// Note: It can be Guid.Empty in cases where it was never set,
				// or more likely, when the HVO was set to -1.
				if (_activeFwLinkArgs.TargetGuid != Guid.Empty)
				{
					var cmObject = _cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(_activeFwLinkArgs.TargetGuid);
					if (cmObject is IReversalIndexEntry && _activeFwLinkArgs.ToolName == AreaServices.ReversalEditCompleteMachineName)
					{
						// For the reversal index tool, just getting the tool right isn't enough.  We
						// also need to be showing the proper index.  (See FWR-1105.)
						// 'guid' can be 'Guid.Empty'.
						var guid = ReversalIndexServices.GetObjectGuidIfValid(_propertyTable, LanguageExplorerConstants.ReversalIndexGuid);
						if (!guid.Equals(cmObject.Owner.Guid))
						{
							_propertyTable.SetProperty(LanguageExplorerConstants.ReversalIndexGuid, cmObject.Owner.Guid.ToString(), true, settingsGroup: SettingsGroup.LocalSettings);
						}
					}
					messages.Add(LanguageExplorerConstants.JumpToRecord);
					newValues.Add(cmObject.Hvo);
				}
				messages.Add(LanguageExplorerConstants.LinkFollowed);
				newValues.Add(_activeFwLinkArgs);
				_publisher.Publish(messages, newValues);
			}
			catch(Exception err)
			{
				var message = !string.IsNullOrEmpty(err.InnerException?.Message) ? string.Format(LanguageExplorerResources.UnableToFollowLink0, err.InnerException.Message) : LanguageExplorerResources.UnableToFollowLink;
				MessageBox.Show(message, LanguageExplorerResources.FailedJump, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		internal static void PublishFollowLinkMessage(IPublisher publisher, FwLinkArgs linkArgsForJump)
		{
			var commands = new List<string>
			{
				FwUtils.AboutToFollowLink,
				FollowLink
			};
			var parms = new List<object>
			{
				null,
				linkArgsForJump
			};
			publisher.Publish(commands, parms);
		}

		#region IDisposable & Co. implementation

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		private bool IsDisposed { get; set; }

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~LinkHandler()
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
			// Therefore, you should call GC.SuppressFinalize to
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
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				_propertyTable.RemoveProperty(LanguageExplorerConstants.LinkHandler);
				_subscriber.Unsubscribe(FollowLink, FollowLink_Handler);
				_subscriber.Unsubscribe(FwUtils.HandleLocalHotlink, HandleLocalHotlink_Handler);
				_backStack?.Clear();
				_forwardStack?.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			_cache = null;
			_backStack = null;
			_forwardStack = null;
			CurrentContext = null;
			_activeFwLinkArgs = null;
			_propertyTable = null;
			_publisher = null;
			_subscriber = null;

			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation
	}
}