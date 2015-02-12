using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.Utils;
using SIL.WritingSystems;

namespace SIL.CoreImpl
{
	/// <summary>
	/// A file-based local writing system store.
	/// </summary>
	public class LocalFileWritingSystemStore : LdmlInFolderWritingSystemRepository, IFwWritingSystemStore
	{
		private readonly IFwWritingSystemStore m_globalStore;

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalFileWritingSystemStore"/> class.
		/// </summary>
		/// <param name="path">The path.</param>
		public LocalFileWritingSystemStore(string path) : this(path, Enumerable.Empty<ICustomDataMapper>(), null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalFileWritingSystemStore"/> class.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <param name="customDataMappers">The custom data mappers.</param>
		/// <param name="globalStore">The global store.</param>
		public LocalFileWritingSystemStore(string path, IEnumerable<ICustomDataMapper> customDataMappers, IFwWritingSystemStore globalStore)
			: base(path, customDataMappers.ToArray(), WritingSystemCompatibility.Strict)
		{
			m_globalStore = globalStore;
			ReadGlobalWritingSystemsToIgnore();
		}

		/// <summary>
		/// Creates a new writing system definition.
		/// </summary>
		/// <returns></returns>
		public override WritingSystemDefinition CreateNew()
		{
			return new WritingSystem();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <param name="dateModified">The date modified.</param>
		public override void LastChecked(string identifier, DateTime dateModified)
		{
			base.LastChecked(identifier, dateModified);
			WriteGlobalWritingSystemsToIgnore();
		}

		/// <summary>
		/// Removes the specified identifier.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		public override void Remove(string identifier)
		{
			int wsIgnoreCount = WritingSystemsToIgnore.Count;
			base.Remove(identifier);
			if (wsIgnoreCount != WritingSystemsToIgnore.Count)
				WriteGlobalWritingSystemsToIgnore();
		}

		/// <summary>
		/// Saves this instance.
		/// </summary>
		public override void Save()
		{
			int wsIgnoreCount = WritingSystemsToIgnore.Count;
			base.Save();
			if (wsIgnoreCount != WritingSystemsToIgnore.Count)
				WriteGlobalWritingSystemsToIgnore();
			if (m_globalStore != null)
				m_globalStore.Save();
		}

		/// <summary>
		/// Gets the specified writing system if it exists.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <param name="ws">The writing system.</param>
		/// <returns></returns>
		public bool TryGet(string identifier, out WritingSystemDefinition ws)
		{
			if (Contains(identifier))
			{
				ws = Get(identifier);
				return true;
			}

			ws = null;
			return false;
		}

		/// <summary>
		/// Return true if it will be possible (absent someone changing permissions while we aren't looking)
		/// to save changes to the specified writing system.
		/// </summary>
		public bool CanSave(WritingSystemDefinition ws, out string filePath)
		{
			var folderPath = PathToWritingSystems;
			var filename = GetFileNameFromIdentifier(ws.StoreID);
			filePath = Path.Combine(folderPath, filename);
			if (File.Exists(filePath))
			{
				try
				{
					using (var stream = File.Open(filePath, FileMode.Open))
						stream.Close();
					// don't really want to change anything
				}
				catch (UnauthorizedAccessException)
				{
					return false;
				}
			}

			else if (Directory.Exists(folderPath))
			{
				try
				{
					// See whether we're allowed to create the file (but if so, get rid of it).
					// Pathologically we might have create but not delete permission...if so,
					// we'll create an empty file and report we can't save. I don't see how to
					// do better.
					using (var stream = File.Create(filePath))
						stream.Close();
					File.Delete(filePath);
				}
				catch (UnauthorizedAccessException)
				{
					return false;
				}
			}
			else
			{
				try
				{
					Directory.CreateDirectory(folderPath);
					// Don't try to clean it up again. This is a vanishingly rare case,
					// I don't think it's even possible to create a writing system store without
					// the directory existing.
				}
				catch (UnauthorizedAccessException)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="ws">The ws.</param>
		protected override void OnChangeNotifySharedStore(WritingSystemDefinition ws)
		{
			base.OnChangeNotifySharedStore(ws);

			if (m_globalStore != null)
			{
				WritingSystemDefinition globalWs;
				if (m_globalStore.TryGet(ws.Id, out globalWs))
				{
					if (ws.DateModified > globalWs.DateModified)
					{
						WritingSystemDefinition newWs = ws.Clone();
						try
						{
							m_globalStore.Remove(ws.Id);
							m_globalStore.Set(newWs);
						}
						catch (UnauthorizedAccessException)
						{
							// Live with it if we can't update the global store. In a CS world we might
							// well not have permission.
						}
					}
				}

				else
				{
					m_globalStore.Set(ws.Clone());
				}
			}
		}

		private void WriteGlobalWritingSystemsToIgnore()
		{
			if (m_globalStore == null)
				return;

			string path = Path.Combine(PathToWritingSystems, "WritingSystemsToIgnore.xml");

			if (WritingSystemsToIgnore.Count == 0)
			{
				if (File.Exists(path))
				{
					try
					{
						File.Delete(path);
					}
					catch (UnauthorizedAccessException)
					{
						var msg = string.Format(CoreImplStrings.ksCannotWriteWritingSystemInfo, path);
						MessageBoxUtils.Show(Form.ActiveForm, msg, CoreImplStrings.ksWarning,
							MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
				}
			}

			else
			{
				var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("WritingSystems", WritingSystemsToIgnore.Select(ignoredWs => new XElement("WritingSystem", new XAttribute("id", ignoredWs.Key), new XAttribute("dateModified", ignoredWs.Value.ToString("s"))))));
				try
				{
					doc.Save(path);
				}
				catch (UnauthorizedAccessException)
				{
					var msg = string.Format(CoreImplStrings.ksCannotWriteWritingSystemInfo, path);
					MessageBox.Show(Form.ActiveForm, msg, CoreImplStrings.ksWarning,
						MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
		}

		private void ReadGlobalWritingSystemsToIgnore()
		{
			string path = Path.Combine(PathToWritingSystems, "WritingSystemsToIgnore.xml");
			if (m_globalStore == null || !File.Exists(path))
				return;

			XElement wssElem = XElement.Load(path);
			foreach (XElement wsElem in wssElem.Elements("WritingSystem"))
			{
				DateTime dateModified = DateTime.ParseExact((string) wsElem.Attribute("dateModified"), "s", null, DateTimeStyles.AdjustToUniversal);
				WritingSystemsToIgnore[(string)wsElem.Attribute("id")] = dateModified;
			}
		}
	}
}
