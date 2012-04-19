// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2003' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// Authorship History: John Hatton
// Last reviewed:
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using SIL.Utils;
using System.Windows.Forms;


namespace XCore
{

	/// <summary>
	/// Summary description for IContextHelper.
	/// </summary>
	public interface IContextHelper
	{
		Control ParentControl
		{
			set;
		}

		string GetToolTip(string id);

	}

	/// summary>
	/// adapts DotNetBar to provide context help
	/// /summary>
	[XCore.MediatorDispose]
	[SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule",
		Justification = "variable is a reference; it is owned by parent")]
	public abstract class BaseContextHelper : IContextHelper, IxCoreColleague, IFWDisposable
	{
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
					m_mediator.PropertyTable.SetProperty("ContextHelper", null, false);
					m_mediator.PropertyTable.SetPropertyPersistence("ContextHelper",false);
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

		abstract protected  void SetHelps(Control target,string caption, string text );

		abstract public  Control ParentControl
		{
			set;
		}

		protected abstract  bool ShowAlways
		{
			set;
		}

		#region IxCoreColleague
		/// <summary/>
		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();
			m_mediator = mediator;
			mediator.AddColleague(this);
			mediator.PropertyTable.SetProperty("ContextHelper", this,false);
			mediator.PropertyTable.SetPropertyPersistence("ContextHelper",false);

			ParentControl = (Control)m_mediator.PropertyTable.GetValue("window");
			m_document= new XmlDocument();

			//we use the  directory of the file which held are parameters as the starting point
			//of the path we were given.
			string path = XmlUtils.GetManditoryAttributeValue(configurationParameters,
				"contextHelpPath");
			var configParamatersBasePath = FileUtils.StripFilePrefix(configurationParameters.BaseURI);
			path = Path.Combine(Path.GetDirectoryName(configParamatersBasePath), path);
			m_document.Load(path);
			//m_items = m_document.SelectNodes("strings/item");

			ShowAlways = m_mediator.PropertyTable.GetBoolProperty("ShowBalloonHelp", true);
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
			string text = m_mediator.StringTbl.LocalizeContextHelpString(id);
			if (String.IsNullOrEmpty(text))
				GetHelpText(id, ref caption, out  text);
			return text;
		}

		protected bool GetHelpText (string id, ref string caption, out string text)
		{
			text  ="";
			if (id == null)
				id = "MissingHelpId";

			XmlNode match = null;
			if (!m_helpIdToElt.ContainsKey(id))
			{
				// Not in Dictioanry, so try and get a new one, or null.
				match = m_document.SelectSingleNode(String.Format("strings/item[@id='{0}']", id));
				m_helpIdToElt[id] = match; // even if null!
			}
			match = m_helpIdToElt[id];

			if (match != null)
			{
				text = match.InnerText;
				text = m_mediator.StringTbl.LocalizeLiteralValue(text);
				if (String.IsNullOrEmpty(caption))
				{
					caption = XmlUtils.GetOptionalAttributeValue(match, "caption", "");
					caption = m_mediator.StringTbl.LocalizeAttributeValue(caption);
				}
				else
				{
					string sCaptionFormat = XmlUtils.GetOptionalAttributeValue(match, "captionformat");
					sCaptionFormat = m_mediator.StringTbl.LocalizeAttributeValue(sCaptionFormat);
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

		//this will just give each sub controlled the same helped message. If you want to have
		//different messages for individual controls, you need register them separately.
		protected void AddControls (Control parent, string helpid, string caption, string text)
		{
			foreach(Control child in parent.Controls)
			{
				SetHelps(child,caption, text);
				AddControls(child, helpid, caption, text);
			}

		}
		/// <summary>
		/// register a control with the context of system
		/// </summary>
		/// <param name="argument"></param>
		/// <returns>always returns true</returns>
		//arguments:
		//	0) the target control
		//	1) the id to use when looking up the help text
		//	2) (optional) a string to concatenate to the Id for a more specific help message, if it exists.
		public bool OnRegisterHelpTargetWithId(object argument)
		{
			CheckDisposed();
			object[] arguments= (object[])argument;
			System.Diagnostics.Debug.Assert( arguments.Length == 3 || arguments.Length == 4,"OnRegisterHelpTargetWithId Expects  three or four arguments");

			Control target = (Control) arguments[0];
			string caption = (string) arguments[1];
			string helpid = (string) arguments[2];
			//this can be used for some elements of the control, to give them their own help message.
			string helpSubId ="";
			if (arguments.Length == 4)
				helpSubId = (string) arguments[3];

			string text;
			//first tried to see if there is an item for the fully qualified if
			if (!GetHelpText(helpid+ helpSubId, ref caption, out text))
			{
				GetHelpText(helpid, ref caption, out text);//just use the id without the qualifier
			}
			else helpid= helpid+ helpSubId; //this is what will be fed to any sub controls

			SetHelps(target, caption, text);

			AddControls(target, helpid, caption, text);
			return true;	//we handled this.
		}

		/// summary>
		/// Receives the broadcast message "PropertyChanged"
		/// /summary>
		public void OnPropertyChanged(string name)
		{
			CheckDisposed();

			//			Debug.WriteLine("record clerk ("+this.m_vectorName + ") saw OnPropertyChanged " + name);
			switch(name)
			{
				case "ShowBalloonHelp":
					ShowAlways=m_mediator.PropertyTable.GetBoolProperty(name, true);
					break;
				default:
					break;
			}
		}
	}
}
