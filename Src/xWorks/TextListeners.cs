using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FdoUi;
using XCore;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Populate the writing systems combo box via the writing system list
	/// </summary>
	/// <remarks>TODO: make an xcore property for controlling the current WritingSystemSet.</remarks>
	[XCore.MediatorDispose]
	public class WritingSystemListHandler : IxCoreColleague, IFWDisposable
	{
		protected Mediator m_mediator;

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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LinkListener"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public WritingSystemListHandler()
		{
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
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

		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();

			m_mediator = mediator;
			mediator.AddColleague(this);
			FdoCache cache = (FdoCache) m_mediator.PropertyTable.GetValue("cache");
			//don't know just what good having this default is, but it's at least safer
			mediator.PropertyTable.SetProperty("WritingSystemHvo", cache.LangProject.DefaultAnalysisWritingSystem.ToString());
			mediator.PropertyTable.SetPropertyPersistence("WritingSystemHvo", false);
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

		private int WritingSystemHvo
		{
			get
			{
				string s = (string)m_mediator.PropertyTable.GetValue("WritingSystemHvo", "-1");
				return int.Parse(s);
			}
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
		/// <param name="parameters"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayWritingSystemList(object parameter, ref UIListDisplayProperties display)
		{
			CheckDisposed();

			display.List.Clear();
			FdoCache cache = (FdoCache) m_mediator.PropertyTable.GetValue("cache");
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
					AddWritingSystemList(display,  cache.LangProject.GetAllNamedWritingSystems());
					break;
				case WritingSystemSet.AllCurrent:
					AddWritingSystemList(display,  cache.LangProject.GetActiveNamedWritingSystems());
					break;
				case WritingSystemSet.CurrentAnalysis:
					AddWritingSystemList(display,  cache.LangProject.CurAnalysisWssRS);
					break;
				case WritingSystemSet.CurrentVernacular:
					AddWritingSystemList(display,  cache.LangProject.CurVernWssRS);
					break;
				case WritingSystemSet.CurrentPronounciation:
					AddWritingSystemList(display, cache.LangProject.GetPronunciationWritingSystems());
					string sValue = CmObject.JoinIds(cache.LangProject.CurPronunWssRS.HvoArray, ",");
					m_mediator.PropertyTable.SetProperty("PronunciationWritingSystemHvos", sValue);
					break;
			}
			return true;//we handled this, no need to ask anyone else.
		}
		private void AddWritingSystemList(UIListDisplayProperties display,
			FdoReferenceSequence<ILgWritingSystem> list)
		{
			foreach (ILgWritingSystem ws in list)
			{
				display.List.Add(ws.ShortName, ws.Hvo.ToString(), null, null);
			}
		}

		private void AddWritingSystemList(UIListDisplayProperties display, Set<NamedWritingSystem> set)
		{
			List<NamedWritingSystem> list = new List<NamedWritingSystem>(set.ToArray()); // A Set doesn't know about sorting, so use a list.
			list.Sort();
			foreach(NamedWritingSystem ws in list)
			{
				display.List.Add(ws.Name, ws.Hvo.ToString(), null, null);
			}
		}
	}
}
