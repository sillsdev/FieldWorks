// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Areas;
using LanguageExplorer.LcmUi;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;

namespace LanguageExplorer
{
	/// <summary>
	/// LinkHandler handles Hyper linking and history
	/// See the class comment on FwLinkArgs for details on how all the parts of hyperlinking work.
	/// </summary>
	internal sealed class LinkHandler : IFlexComponent, IApplicationIdleEventHandler, IDisposable
	{
		const int kmaxDepth = 50;       // Limit the stacks to 50 elements (LT-729).
		// Used to count the number of times we've been asked to suspend Idle processing.
		private int _countSuspendIdleProcessing;
		private IFwMainWnd _mainWindow;
		private LcmCache _cache;
		private ToolStripButton _toolStripButtonHistoryBack;
		private ToolStripButton _toolStripButtonHistoryForward;
		private ToolStripMenuItem _copyLocationAsHyperlinkToolStripMenuItem;
		private LinkedList<FwLinkArgs> _backStack;
		private LinkedList<FwLinkArgs> _forwardStack;
		private bool _followingLink;
		private int _backStackOrig;
		private FwLinkArgs _linkActive;
		private bool _usingHistory;

		/// <summary>
		/// Initializes a new instance of the <see cref="LinkHandler"/> class.
		/// </summary>
		internal LinkHandler(IFwMainWnd mainWindow, LcmCache cache, ToolStripButton toolStripButtonHistoryBack, ToolStripButton toolStripButtonHistoryForward, ToolStripMenuItem copyLocationAsHyperlinkToolStripMenuItem)
		{
			Guard.AgainstNull(mainWindow, nameof(mainWindow));
			Guard.AgainstNull(cache, nameof(cache));
			Guard.AgainstNull(toolStripButtonHistoryBack, nameof(toolStripButtonHistoryBack));
			Guard.AgainstNull(toolStripButtonHistoryForward, nameof(toolStripButtonHistoryForward));
			Guard.AgainstNull(copyLocationAsHyperlinkToolStripMenuItem, nameof(copyLocationAsHyperlinkToolStripMenuItem));

			_mainWindow = mainWindow;
			_cache = cache;

			_toolStripButtonHistoryBack = toolStripButtonHistoryBack;
			_toolStripButtonHistoryBack.Click += HistoryBack_Clicked;

			_toolStripButtonHistoryForward = toolStripButtonHistoryForward;
			_toolStripButtonHistoryForward.Click += HistoryForward_Clicked;

			_copyLocationAsHyperlinkToolStripMenuItem = copyLocationAsHyperlinkToolStripMenuItem;
			_copyLocationAsHyperlinkToolStripMenuItem.Click += CopyLocationAsHyperlink_Clicked;

			_backStack = new LinkedList<FwLinkArgs>();
			_forwardStack = new LinkedList<FwLinkArgs>();
			CurrentContext = null;

			Application.Idle += Application_Idle;
		}

		private void Application_Idle(object sender, EventArgs e)
		{
			_toolStripButtonHistoryBack.Enabled = _backStack.Any();
			_toolStripButtonHistoryForward.Enabled = _forwardStack.Any();
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

		#region IDisposable & Co. implementation

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }

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

			if (IsDisposed)
			{
				// No need to run more than once.
				return;
			}

			if (disposing)
			{
				Application.Idle -= Application_Idle;
				_toolStripButtonHistoryBack.Click -= HistoryBack_Clicked;
				_toolStripButtonHistoryForward.Click -= HistoryForward_Clicked;
				_copyLocationAsHyperlinkToolStripMenuItem.Click -= CopyLocationAsHyperlink_Clicked;
				_backStack?.Clear();
				_forwardStack?.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			_toolStripButtonHistoryBack = null;
			_toolStripButtonHistoryForward = null;
			_copyLocationAsHyperlinkToolStripMenuItem = null;
			_backStack = null;
			_forwardStack = null;
			CurrentContext = null;
			_linkActive = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// Return the current link.
		/// </summary>
		public FwLinkArgs CurrentContext { get; private set; }

		/// <summary>
		/// Handle the specified link if it is local.
		/// </summary>
		public bool OnHandleLocalHotlink(object source)
		{
			var args = source as LocalLinkArgs;
			if (args == null)
			{
				return true; // we can't handle it, but probably no one else can either. Maybe should crash?
			}
			var url = args.Link;
			if(!url.StartsWith(FwLinkArgs.kFwUrlPrefix))
			{
				return true; // we can't handle it, but no other colleague can either. Needs to launch whatever can (see VwBaseVc.DoHotLinkAction).
			}
			try
			{
				var fwargs = new FwAppArgs(url);
				if (SameDatabase(fwargs, _cache))
				{
					FollowLink_Handler(fwargs);
					args.LinkHandledLocally = true;
				}
			}
			catch (Exception)
			{
				// Something went wrong, probably its not a kind of link we understand.
			}
			return true;
		}

		private static bool SameDatabase(FwAppArgs fwargs, LcmCache cache)
		{
			return fwargs.Database == "this$" ||
				string.Equals(fwargs.Database, cache.ProjectId.Name, StringComparison.InvariantCultureIgnoreCase)
				|| string.Equals(fwargs.Database, cache.ProjectId.Path, StringComparison.InvariantCultureIgnoreCase)
				|| string.Equals(Path.GetFileName(fwargs.Database), cache.ProjectId.Name, StringComparison.InvariantCultureIgnoreCase);
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
			if (_followingLink && newHistoryLink.EssentiallyEquals(_linkActive))
			{
				var howManyAdded = _backStack.Count - _backStackOrig;
				for( ; howManyAdded > 1; --howManyAdded)
				{
					_backStack.RemoveLast();
				}
				_followingLink = false;
				_backStackOrig = 0;
				_linkActive = null;
			}
			// The forward stack should be cleared by jump operations that are NOT spawned by
			// a Back or Forward (ie, history) operation.  This is the standard behavior in
			// browsers, for example (as far as I know).
			if (_usingHistory)
			{
				if (newHistoryLink.EssentiallyEquals(_linkActive))
				{
					_usingHistory = false;
					_linkActive = null;
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
			var args = new FwAppArgs(_cache.ProjectId.Handle, CurrentContext.ToolName, CurrentContext.TargetGuid);
			ClipboardUtils.SetDataObject(args.ToString(), true);
		}

		/// <summary />
		private void HistoryBack_Clicked(object sender, EventArgs e)
		{
			if (!_backStack.Any())
			{
				return;
			}
			if (CurrentContext!= null)
			{
				Push(_forwardStack, CurrentContext);
			}
			_usingHistory = true;
			_linkActive = Pop(_backStack);
			FollowActiveLink();
		}

		/// <summary />
		private void HistoryForward_Clicked(object sender, EventArgs e)
		{
			if (!_forwardStack.Any())
			{
				return;
			}
			_usingHistory = true;
			_linkActive = Pop(_forwardStack);
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
			_linkActive = (FwLinkArgs)lnk;

			FollowActiveLink();
		}

		private void FollowActiveLink()
		{
			try
			{
				if (_linkActive.ToolName == "default")
				{
					// Need some smarts here. The link creator was not sure what tool to use.
					// The object may also be a child we don't know how to jump to directly.
					ICmObject target;
					if (!_cache.ServiceLocator.ObjectRepository.TryGetObject(_linkActive.TargetGuid, out target))
					{
						return; // or message?
					}
					string cantJumpMessage = null;
					var realTarget = GetObjectToShowInTool(target);
					var realTool = string.Empty;
					var majorObject = realTarget.Owner ?? realTarget;
					switch (majorObject.ClassID)
					{
						case ReversalIndexTags.kClassId:
							realTool = AreaServices.ReversalEditCompleteMachineName;
							break;
						case TextTags.kClassId:
							realTool = AreaServices.InterlinearEditMachineName;
							break;
						case LexEntryTags.kClassId:
							realTool = AreaServices.LexiconEditMachineName;
							break;
						case CmPossibilityListTags.kClassId:
							// The area listener knows about the possible list tools.
							// Unfortunately AreaListener is in an assembly we can't reference.
							// But there may be custom ones, so just listing them all here does not seem to be an option,
							// and anyway it would be hard to maintain.
							// Thus we've created this method (on AreaListener) which we call awkwardly through the mediator.
							var parameters = new object[2];
							parameters[0] = majorObject;
							Publisher.Publish("GetToolForList", parameters);
							realTool = (string)parameters[1];
							break;
						case RnResearchNbkTags.kClassId:
							realTool = AreaServices.NotebookEditToolMachineName;
							break;
						case DsConstChartTags.kClassId:
							realTarget = ((IDsConstChart) majorObject).BasedOnRA;
							realTool = AreaServices.InterlinearEditMachineName;
							// Enhance JohnT: do something to make it switch to Discourse tab
							break;
						case ScriptureTags.kClassId:
							cantJumpMessage = LanguageExplorerResources.ksCantJumpToScripture;
							break;
						case LexDbTags.kClassId: // other things owned by this??
						case LangProjectTags.kClassId:
							cantJumpMessage = LanguageExplorerResources.ksCantJumpToLangProj;
							break;
						default:
							cantJumpMessage = string.Format(LanguageExplorerResources.ksCantJumpToObject, _cache.MetaDataCacheAccessor.GetClassName(majorObject.ClassID));
							break; // can't jump to it.
					}
					if (!string.IsNullOrWhiteSpace(cantJumpMessage))
					{
						ShowCantJumpMessage(cantJumpMessage);
						return;
					}
					_linkActive = new FwLinkArgs(realTool, realTarget.Guid);
					// Todo JohnT: need to do something special here if we c
				}
				// It's important to do this AFTER we set the real tool name if it is "default". Otherwise, the code that
				// handles the jump never realizes we have reached the desired tool (as indicated by the value of
				// SuspendLoadingRecordUntilOnJumpToRecord) and we stop recording context history and various similar problems.
				if (_linkActive.TargetGuid != Guid.Empty)
				{
					// allow tools to skip loading a record if we're planning to jump to one.
					// interested tools will need to reset this "JumpToRecord" property after handling OnJumpToRecord.
					PropertyTable.SetProperty("SuspendLoadingRecordUntilOnJumpToRecord",
						$"{_linkActive.ToolName},{_linkActive.TargetGuid}", settingsGroup: SettingsGroup.LocalSettings);
				}

				var messages = new List<string>();
				var newValues = new List<object>();
				// 1. IF _linkActive.ToolName is in a different area, then the old area and its current tool both need to deactivated.
				// 2. THEN the new area needs to be activated along with _linkActive.ToolName, in that area.
				// 3. ELSE the current tool needs to be deactivated and _linkActive.ToolName needs to be activated
				// 4. Don't do this "SetToolFromName" business at all.
				// 5. This class really needs to have the IAreaRepository instance to be able to do all of that (de)-activation
				//		on the area and tool instances.
				//		I suspect IArea and ITool need a new 'IsActive' bool property to get this done and IArea needs to be able to fetch its active tool.
				messages.Add("SetToolFromName"); // Only old handler: AreaListener->OnSetToolFromName
				newValues.Add(_linkActive.ToolName);
				// Note: It can be Guid.Empty in cases where it was never set,
				// or more likely, when the HVO was set to -1.
				if (_linkActive.TargetGuid != Guid.Empty)
				{
					var cmObject = _cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(_linkActive.TargetGuid);
					if (cmObject is IReversalIndexEntry && _linkActive.ToolName == AreaServices.ReversalEditCompleteMachineName)
					{
						// For the reversal index tool, just getting the tool right isn't enough.  We
						// also need to be showing the proper index.  (See FWR-1105.)
						// 'guid' can be 'Guid.Empty'.
						var guid = ReversalIndexEntryUi.GetObjectGuidIfValid(PropertyTable, "ReversalIndexGuid");
						if (!guid.Equals(cmObject.Owner.Guid))
						{
							PropertyTable.SetProperty("ReversalIndexGuid", cmObject.Owner.Guid.ToString(), true);
							messages.Add("ReversalIndexGuid");
							newValues.Add(cmObject.Owner.Guid.ToString());
						}
					}
					messages.Add("JumpToRecord");
					newValues.Add(cmObject.Hvo);
				}
				messages.Add("LinkFollowed");
				newValues.Add(_linkActive);
				Publisher.Publish(messages, newValues);
			}
			catch(Exception err)
			{
				var message = !string.IsNullOrEmpty(err.InnerException?.Message) ? string.Format(LanguageExplorerResources.UnableToFollowLink0, err.InnerException.Message) : LanguageExplorerResources.UnableToFollowLink;
				MessageBox.Show(message, LanguageExplorerResources.FailedJump, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		private void ShowCantJumpMessage(string msg)
		{
			MessageBox.Show(PropertyTable.GetValue<Form>("window") ?? Form.ActiveForm, msg, LanguageExplorerResources.ksCantJumpCaption);
		}

		/// <summary>
		/// Get the object we want to point our tool at. This is typically the one that is one level down from
		/// a CmMajorObject.
		/// </summary>
		private static ICmObject GetObjectToShowInTool(ICmObject start)
		{
			for(var current = start;;current = current.Owner)
			{
				if (current.Owner == null || current.Owner is ICmMajorObject)
				{
					return current;
				}
			}
		}

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			PropertyTable.SetProperty("LinkHandler", this);
			Subscriber.Subscribe("FollowLink", FollowLink_Handler);
		}

		#endregion

		#region Implementation of IApplicationIdleEventHandler
		/// <summary>
		/// Call this for the duration of a block of code where we don't want idle events.
		/// (Note that various things outside our control may pump events and cause the
		/// timer that fires the idle events to be triggered when we are not idle, even in the
		/// middle of processing another event.) Call ResumeIdleProcessing when done.
		/// </summary>
		public void SuspendIdleProcessing()
		{
			_countSuspendIdleProcessing++;
			if (_countSuspendIdleProcessing == 1)
			{
				Application.Idle -= Application_Idle;
			}
		}

		/// <summary>
		/// See SuspendIdleProcessing.
		/// </summary>
		public void ResumeIdleProcessing()
		{
			FwUtils.CheckResumeProcessing(_countSuspendIdleProcessing, GetType().Name);
			if (_countSuspendIdleProcessing > 0)
			{
				_countSuspendIdleProcessing--;
				if (_countSuspendIdleProcessing == 0)
				{
					Application.Idle += Application_Idle;
				}
			}
		}
		#endregion
	}
}
