// Copyright (c) 2003-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using SIL.LCModel.Utils;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace XCore
{
	/// <summary>
	/// Summary description for IContextHelper.
	/// </summary>
	public interface IContextHelper
	{
		string GetToolTip(string id);
	}

	/// summary>
	/// adapts DotNetBar to provide context help
	/// /summary>
	[XCore.MediatorDispose]
	public abstract class BaseContextHelper : IContextHelper, IxCoreColleague, IDisposable
	{
		protected PropertyTable m_propertyTable;
		protected Mediator m_mediator;
		protected XmlDocument m_document;
		protected Dictionary<string, XmlNode> m_helpIdToElt = new Dictionary<string, XmlNode>();

		public BaseContextHelper()
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
		~BaseContextHelper()
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
				if (m_mediator != null)
				{
					m_mediator.RemoveColleague(this);
					m_propertyTable.SetProperty("ContextHelper", null, false);
					m_propertyTable.SetPropertyPersistence("ContextHelper", false);
				}
				if (m_helpIdToElt != null)
					m_helpIdToElt.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mediator = null;
			m_helpIdToElt = null;
			m_document = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region IxCoreColleague
		/// <summary/>
		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			CheckDisposed();
			m_mediator = mediator;
			m_propertyTable = propertyTable;
			mediator.AddColleague(this);
			m_propertyTable.SetProperty("ContextHelper", this, false);
			m_propertyTable.SetPropertyPersistence("ContextHelper", false);

			m_document= new XmlDocument();

			//we use the  directory of the file which held are parameters as the starting point
			//of the path we were given.
			string path = XmlUtils.GetMandatoryAttributeValue(configurationParameters,
				"contextHelpPath");
			var configParamatersBasePath = FileUtils.StripFilePrefix(configurationParameters.BaseURI);
			path = Path.Combine(Path.GetDirectoryName(configParamatersBasePath), path);
			m_document.Load(path);
		}

		public	IxCoreColleague[] GetMessageTargets()
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

		public abstract int Priority { get; }

		#endregion

		public string GetToolTip(string id)
		{
			CheckDisposed();
			string caption = "";
			string text = StringTable.Table.LocalizeContextHelpString(id);
			if (String.IsNullOrEmpty(text))
				GetHelpText(id, ref caption, out  text);
			return text;
		}

		private bool GetHelpText (string id, ref string caption, out string text)
		{
			text  ="";
			if (id == null)
				id = "MissingHelpId";

			XmlNode match = null;
			if (!m_helpIdToElt.ContainsKey(id))
			{
				// Not in Dictionary, so try and get a new one, or null.
				match = m_document.SelectSingleNode(String.Format("strings/item[@id='{0}']", id));
				m_helpIdToElt[id] = match; // even if null!
			}
			match = m_helpIdToElt[id];

			if (match != null)
			{
				text = match.InnerText;
				text = StringTable.Table.LocalizeLiteralValue(text);
				if (String.IsNullOrEmpty(caption))
				{
					caption = XmlUtils.GetOptionalAttributeValue(match, "caption", "");
					caption = StringTable.Table.LocalizeAttributeValue(caption);
				}
				else
				{
					string sCaptionFormat = XmlUtils.GetOptionalAttributeValue(match, "captionformat");
					sCaptionFormat = StringTable.Table.LocalizeAttributeValue(sCaptionFormat);
					if (!String.IsNullOrEmpty(sCaptionFormat) && sCaptionFormat.IndexOf("{0}") >= 0)
						caption = String.Format(sCaptionFormat, caption);
				}
				// Insert some command specific text if the tooltip needs formatting.
				if (m_mediator.CommandSet != null)
				{
					Command command = m_mediator.CommandSet[id] as Command;
					if (command != null && command.ToolTipInsert != null &&
						command.ToolTipInsert != string.Empty)
					{
						string formattedText = String.Format(text, command.ToolTipInsert);
						if (formattedText != string.Empty && formattedText != text)
							text = formattedText;
					}
				}
			}

			if (text.Contains("{0}"))
			{
				// indicates the target wants to customize it.
				Command command = m_mediator.CommandSet[id] as Command;
				if (command != null && !string.IsNullOrEmpty(command.MessageString))
				{
					var holder = new ToolTipHolder();
					holder.ToolTip = text;
					m_mediator.SendMessage(command.MessageString + "ToolTip", holder);
					text = holder.ToolTip;
				}
			}

			if (text == "" && id != "NoHelp")
			{
				// We don't have text, so see if we want to display a "NoHelp" message instead.
				string noHelpCaption = caption;
				GetHelpText("NoHelp", ref noHelpCaption, out text);
				// if we got a NoHelp text, see if we can format it to insert debug info.
				if (text != string.Empty)
				{
					string formattedText = String.Format(text, id, caption);
					if (formattedText != string.Empty && formattedText != text)
						text = formattedText;
				}
				if (match == null)
				{
					if (noHelpCaption != string.Empty)
						caption = noHelpCaption;
					else
						caption = xCoreInterfaces.HelpFileProblem;
				}
			}
			else if (match != null)
			{
				// we matched something.
				return true;
			}
			return false;	//there was no real match
		}
	}
}
