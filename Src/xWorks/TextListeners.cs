using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Populate the writing systems combo box via the writing system list
	/// </summary>
	/// <remarks>TODO: make an xcore property for controlling the current WritingSystemSet.</remarks>
	[MediatorDispose]
	public class WritingSystemListHandler : IxCoreColleague, IFWDisposable
	{
		protected Mediator m_mediator;
		protected PropertyTable m_propertyTable;

		public enum WritingSystemSet {All, AllCurrent, AllAnalysis, AllVernacular, CurrentAnalysis, CurrentVernacular, CurrentPronounciation};
		private WritingSystemSet m_currentSet = WritingSystemSet.AllCurrent;

		public WritingSystemSet CurrentSet
		{
			set
			{
				CheckDisposed();

				m_currentSet = value;
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
		~WritingSystemListHandler()
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
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_mediator !=  null)
					m_mediator.RemoveColleague(this);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mediator = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			CheckDisposed();

			m_mediator = mediator;
			m_propertyTable = propertyTable;
			m_mediator.AddColleague(this);
			FdoCache cache = m_propertyTable.GetValue<FdoCache>("cache");
			//don't know just what good having this default is, but it's at least safer
			m_propertyTable.SetProperty("WritingSystemHvo",
				cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle.ToString(),
				true);
			m_propertyTable.SetPropertyPersistence("WritingSystemHvo", false);
		}

		/// <summary>
		/// return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// </summary>
		/// <returns></returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			return new IxCoreColleague[]{this};
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		public int Priority
		{
			get { return (int)ColleaguePriority.Medium; }
		}

		/// <summary>
		/// Called (by xcore) to control display params of the writing system menu, e.g. whether it should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayWritingSystemHvo(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = false;

			return false;//we get called before the rootsite, so let them have a say.
		}

		/// <summary>
		/// Decode an XML attribute value (presumably) into the proper enumeration value.
		/// </summary>
		/// <param name="wsSet">name of the writing system set</param>
		/// <returns>enumeration value</returns>
		private WritingSystemSet DecodeSetName(string wsSet)
		{
			string wsset = wsSet.ToLowerInvariant();
			switch (wsset)
			{
			case "all":
				return WritingSystemSet.All;
			case "allcurrent":
				return WritingSystemSet.AllCurrent;
			case "allanalysis":
				return WritingSystemSet.AllAnalysis;
			case "allvernacular":
				return WritingSystemSet.AllVernacular;
			case "currentanalysis":
				return WritingSystemSet.CurrentAnalysis;
			case "currentvernacular":
				return WritingSystemSet.CurrentVernacular;
			case "currentpronunciation":
				return WritingSystemSet.CurrentPronounciation;
			default:
				// stick with what we have for input garbage.
				return m_currentSet;
			}
		}
		/// <summary>
		/// this is called when XCore wants to display something that relies on the list with the id "WritingSystemList"
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <param name="display">The display.</param>
		/// <returns></returns>
		public bool OnDisplayWritingSystemList(object parameter, ref UIListDisplayProperties display)
		{
			CheckDisposed();

			display.List.Clear();
			FdoCache cache = m_propertyTable.GetValue<FdoCache>("cache");
			string wsSet = parameter as string;
			WritingSystemSet setToUse = m_currentSet;
			if (wsSet != null)
			{
				// JohnT: This is a patch to fix LT-5059. The problem is that the WritingSystemList set
				// (with the pronunciation subset) is used in the Pronuciation field pull-down menu.
				// All other code that I can find currently does not specify it. Therefore, after this
				// menu is displayed once, m_currentSet is set to pronunciation and stays there.
				// It seems that we actually have no current use for any other subset, nor for remembering
				// the most recent one specified. However, in the interests of making the change minimal,
				// and in case there is some anticipated application of tracking the current set which
				// I am not aware of, I made a way for that one menu item to get the list it needs
				// without making a persistent change.
				string tempPrefix = "temp:";
				bool fTemp = wsSet.StartsWith(tempPrefix);
				if (fTemp)
				{
					setToUse = DecodeSetName(wsSet.Substring(tempPrefix.Length));
				}
				else
				{
					m_currentSet = setToUse = DecodeSetName(wsSet);
				}
			}
			switch (setToUse)
			{
				default:
					throw new NotImplementedException("That writing system set needs to be implemented");
				case WritingSystemSet.All:
					AddWritingSystemList(display, cache.ServiceLocator.WritingSystemManager.LocalWritingSystems);
					break;
				case WritingSystemSet.AllCurrent:
					AddWritingSystemList(display, cache.ServiceLocator.WritingSystems.AllWritingSystems);
					break;
				case WritingSystemSet.CurrentAnalysis:
					AddWritingSystemList(display, cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems);
					break;
				case WritingSystemSet.CurrentVernacular:
					AddWritingSystemList(display, cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems);
					break;
				case WritingSystemSet.CurrentPronounciation:
					AddWritingSystemList(display, cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems);
					string sValue = DomainObjectServices.JoinIds(cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems.Select(ws => ws.Handle).ToArray(), ",");
					m_propertyTable.SetProperty("PronunciationWritingSystemHvos", sValue, true);
					break;
			}
			return true;//we handled this, no need to ask anyone else.
		}
		private static void AddWritingSystemList(UIListDisplayProperties display, IEnumerable<IWritingSystem> list)
		{
			foreach (IWritingSystem ws in list)
			{
				display.List.Add(ws.DisplayLabel, ws.Handle.ToString(), null, null);
			}
		}
	}
	/// <summary>
	/// Dummy handler to disable displaying the combined styles combobox by default.
	/// </summary>
	[MediatorDispose]
	public class CombinedStylesListHandler : IxCoreColleague, IFWDisposable
	{
		public enum StylesSet { All, CharacterOnly };

		protected Mediator m_mediator;

		#region IFWDisposable Members

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

#if DEBUG
		~CombinedStylesListHandler()
		{
			Dispose(false);
		}
#endif

		#endregion

		#region IDisposable Members

		/// <summary>
		/// Clean up everything that we've been using.
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
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_mediator != null)
					m_mediator.RemoveColleague(this);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mediator = null;

			m_isDisposed = true;
		}

		#endregion

		#region IxCoreColleague Members

		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			return new IxCoreColleague[] { this };
		}

		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			CheckDisposed();

			m_mediator = mediator;
			mediator.AddColleague(this);
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		public int Priority
		{
			get { return (int)ColleaguePriority.Medium; }
		}

		#endregion

		/// <summary>
		/// Called (by xcore) to control display params of the Styles menu, e.g. whether it should be enabled
		/// </summary>
		public virtual bool OnDisplayBestStyleName(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = false;
			display.Text = StyleUtils.DefaultParaCharsStyleName;

			return false;		// we get called before the rootsite, so let them have a say.
		}

		/// <summary>
		/// Called when XCore wants to display something that relies on the list with the id
		/// "CombinedStylesList".
		/// </summary>
		public bool OnDisplayCombinedStylesList(object parameter, ref UIListDisplayProperties display)
		{
			CheckDisposed();

			return false;
		}
	}
}
