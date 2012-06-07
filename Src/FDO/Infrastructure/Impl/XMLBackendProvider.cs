// --------------------------------------------------------------------------------------------
// Copyright (C) 2010 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: XMLBackendProvider.cs
// Responsibility: FW Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// A subclass of the FDOBackendProvider
	/// which handles an XML file-based system.
	/// </summary>
	internal class XMLBackendProvider : FDOBackendProvider
	{
		#region CommitWork class
		class CommitWork
		{
			public CommitWork(HashSet<ICmObjectOrSurrogate> newbies,
				HashSet<ICmObjectOrSurrogate> dirtballs, HashSet<ICmObjectId> goners, IEnumerable<CustomFieldInfo> customFields)
			{
				Newbies = new SortedDictionary<Guid, byte[]>();
				foreach (ICmObjectOrSurrogate newby in newbies)
					Newbies.Add(newby.Id.Guid, newby.XMLBytes);
				Dirtballs = new Dictionary<Guid, byte[]>(dirtballs.Count);
				foreach (ICmObjectOrSurrogate dirtball in dirtballs)
					Dirtballs.Add(dirtball.Id.Guid, dirtball.XMLBytes);
				// JohnT: strangely, this may actually help reduce the chance of running out of memory,
				// as compared to simply Goners = new HashSet<Guid>(goners.Select(id => id.Guid)).
				// The problem is that a hashset created on an enumeration can't set its initial size,
				// and therefore grows stepwise, and if it gets very large may allocate successively
				// larger but not reusable chunks of large object heap, and eventually run out of
				// address space (before it runs out of actual memory). The list can be forced to start
				// out the right size, and when a Hashset is passed a simple object with a known size, it will
				// allocate a single chunk the right size to begin with.
				// The old code would probably be marginally faster for small collections, but it won't
				// make much difference there anyway.
				var gonerList = new List<Guid>(goners.Count);
				gonerList.AddRange(goners.Select(id => id.Guid));
				Goners = new HashSet<Guid>(gonerList);
				CustomFields = customFields.ToList();
			}

			public SortedDictionary<Guid, byte[]> Newbies
			{
				get; private set;
			}

			public Dictionary<Guid, byte[]> Dirtballs
			{
				get; private set;
			}

			public HashSet<Guid> Goners
			{
				get; private set;
			}

			public List<CustomFieldInfo> CustomFields
			{
				get; private set;
			}

			public void Combine(CommitWork work)
			{
				// New objects can't possibly be in more than one UOW,
				// so just add all of them.
				foreach (KeyValuePair<Guid, byte[]> newby in work.Newbies)
					Newbies.Add(newby.Key, newby.Value);

				foreach (KeyValuePair<Guid, byte[]> dirtball in work.Dirtballs)
				{
					if (Newbies.ContainsKey(dirtball.Key))
						// use the updated XML string from the dirtball if it is new
						Newbies[dirtball.Key] = dirtball.Value;
					else
						// otherwise the newer modifications trump the older modifications
						Dirtballs[dirtball.Key] = dirtball.Value;
				}

				foreach (Guid gonerId in work.Goners)
				{
					// Deleting trumps modified.
					Dirtballs.Remove(gonerId);

					// Only add it to deleted list, if it is *not* in the new list.
					// An object that is both new and deleted has not been saved in the store,
					// so no further action is needed.
					if (!Newbies.Remove(gonerId))
						Goners.Add(gonerId);
				}

				// just use the latest list of custom fields
				CustomFields = work.CustomFields;
			}
		}
		#endregion

		#region Member variables
		private DateTime m_lastWriteTime;
		private ConsumerThread<int, CommitWork> m_thread;
		private FileStream m_lockFile;
		private bool m_needConversion; // communicates to MakeSurrogate that we're reading an old version.
		private int m_startupVersionNumber;

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		internal XMLBackendProvider(FdoCache cache, IdentityMap identityMap,
			ICmObjectSurrogateFactory surrogateFactory, IFwMetaDataCacheManagedInternal mdc,
			IDataMigrationManager dataMigrationManager) :
			base(cache, identityMap, surrogateFactory, mdc, dataMigrationManager)
		{
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + "******************");
			if (IsDisposed)
				return;

			if (disposing)
			{
				CompleteAllCommits();
				// Make sure the commit thread is stopped. (FWR-3179)
				if (m_thread != null)
				{
					m_thread.StopOnIdle(); // CompleteAllCommits should wait until idle, but just in case...
					m_thread.Dispose();
				}
				UnlockProject();
			}
			m_thread = null;

			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start the BEP.
		/// </summary>
		/// <param name="currentModelVersion">The current model version.</param>
		/// <returns>The data store's current version number.</returns>
		/// ------------------------------------------------------------------------------------
		protected override int StartupInternal(int currentModelVersion)
		{
			BasicInit();
			for (; ; ) // Loop is used to retry if we get a corrupt file and restore backup.
			{
				var fileSize = new FileInfo(ProjectId.Path).Length;
				// This arbitrary length is based on two large databases, one 360M with 474 bytes/object, and one 180M with 541.
				// It's probably not perfect, but we're mainly trying to prevent fragmenting the large object heap
				// by growing it MANY times.
				var estimatedObjectCount = (int)(fileSize/400);
				m_identityMap.ExpectAdditionalObjects(estimatedObjectCount);

				if (!FileUtils.FileExists(ProjectId.Path))
					throw new InvalidOperationException("System does not exist.");

				try
				{
					// We need to reorder the entire file to be nice to Mercurial,
					// but only if the current version is less than "7000048".

					// Step 0: Get the version number.
					m_startupVersionNumber = GetActualModelVersionNumber(ProjectId.Path);
					var useLocalTempFile = !IsLocalDrive(ProjectId.Path);

					// Step 1:
					if (m_startupVersionNumber < 7000048)
					{
#if DEBUG
						var reorderWatch = new Stopwatch();
						reorderWatch.Start();
#endif
						var tempPathname = useLocalTempFile ? Path.GetTempFileName() : Path.ChangeExtension(ProjectId.Path, "tmp");
						// Rewrite the file in the prescribed order.
						using (var writer = FdoXmlServices.CreateWriter(tempPathname))
						{
							FdoXmlServices.WriteStartElement(writer, m_startupVersionNumber); // Use version from old file, so DM can be done, if needed.

							DataSortingService.SortEntireFile(m_mdcInternal.GetSortableProperties(), writer, ProjectId.Path);

							writer.WriteEndElement(); // 'languageproject'
							writer.Close();
						}

#if DEBUG
						reorderWatch.Stop();
						Debug.WriteLine("Reordering entire file took " + reorderWatch.ElapsedMilliseconds + " ms.");
						//Debug.Assert(false, "Force a stop.");
#endif
						// Copy reordered file to ProjectId.Path.
						CopyTempFileToOriginal(useLocalTempFile, ProjectId.Path, tempPathname);
					}

					// Step 2: Go on one's merry way....
					using (var reader = FdoXmlServices.CreateReader(ProjectId.Path))
					{
						reader.MoveToContent();
						m_needConversion = (m_startupVersionNumber != currentModelVersion);

						// Optional AdditionalFields element.
						if (reader.Read() && reader.LocalName == "AdditionalFields")
						{
							var cfiList = new List<CustomFieldInfo>();
							while (reader.Read() && reader.LocalName == "CustomField")
							{
								if (!reader.IsStartElement())
									continue;

								var cfi = new CustomFieldInfo();
								reader.MoveToAttribute("class");
								cfi.m_classname = reader.Value;
								if (reader.MoveToAttribute("destclass"))
									cfi.m_destinationClass = Int32.Parse(reader.Value);
								if (reader.MoveToAttribute("helpString"))
									cfi.m_fieldHelp = reader.Value;
								if (reader.MoveToAttribute("label"))
									cfi.Label = reader.Value;
								if (reader.MoveToAttribute("listRoot"))
									cfi.m_fieldListRoot = new Guid(reader.Value);
								reader.MoveToAttribute("name");
								cfi.m_fieldname = reader.Value;
								reader.MoveToAttribute("type");
								cfi.m_fieldType = GetFlidTypeFromString(reader.Value);
								if (reader.MoveToAttribute("wsSelector"))
									cfi.m_fieldWs = Int32.Parse(reader.Value);
								reader.MoveToElement();
								cfiList.Add(cfi);
							}
							RegisterOriginalCustomProperties(cfiList);
						}
					}

					Stopwatch watch = new Stopwatch();
					watch.Start();
					using (var er = new ElementReader("<rt ", "</languageproject>", ProjectId.Path, MakeSurrogate))
					{
						er.Run();
					}
					watch.Stop();
					Debug.WriteLine("Making surrogates took " + watch.ElapsedMilliseconds + " ms.");
				}
				catch (ArgumentException e)
				{
					Logger.WriteError(e);
					// Failed to get a version number from the file!
					OfferToRestore(Properties.Resources.kstidInvalidFieldWorksXMLFile);
					continue; // backup restored, if previous call returns.
				}
				catch (XmlException e)
				{
					Logger.WriteError(e);
					// The data is not in the format we expect or not even an XML file
					OfferToRestore(Properties.Resources.kstidInvalidFieldWorksXMLFile);
					continue; // backup restored, if previous call returns.
				}
				catch (IOException e)
				{
					Logger.WriteError(e);
					OfferToRestore(e.Message);
					continue; // backup restored, if previous call returns.
				}
				return m_startupVersionNumber;
			}
		}

		private static int GetActualModelVersionNumber(string path)
		{
			using (var reader = File.OpenText(path))
			{
				var foundStart = false;
				var line = reader.ReadLine();
				while (line != null)
				{
					if (line.Contains("<languageproject"))
					{
						foundStart = true;
					}
					else
					{
						line = reader.ReadLine();
						continue;
					}
					var idx = line.IndexOf("version");
					if (idx > -1)
						return Int32.Parse(line.Substring(idx + 9, 7));
					line = reader.ReadLine();
				}
			}
			throw new ArgumentException("Version not found in file.");
		}

		/// <summary>
		/// Shutdown the BEP.
		/// </summary>
		protected override void ShutdownInternal()
		{
		}

		private void OfferToRestore(string message)
		{
			string backupFilePath = Path.ChangeExtension(ProjectId.Path, "bak");
			if (File.Exists(backupFilePath))
			{
				if (ThreadHelper.ShowMessageBox(null,
					String.Format(Properties.Resources.kstidOfferToRestore, ProjectId.Path, File.GetLastWriteTime(ProjectId.Path),
					backupFilePath, File.GetLastWriteTime(backupFilePath)),
					Properties.Resources.kstidProblemOpeningFile, MessageBoxButtons.YesNo,
					MessageBoxIcon.Error) == DialogResult.Yes)
				{
					string badFilePath = Path.ChangeExtension(ProjectId.Path, "bad");
					if (File.Exists(badFilePath))
						File.Delete(badFilePath);
					File.Move(ProjectId.Path, badFilePath);
					File.Move(backupFilePath, ProjectId.Path);
					m_lastWriteTime = File.GetLastWriteTimeUtc(ProjectId.Path); // otherwise we'll get a "someone else changed it" when we next Save.
					return;
				}
			}
			// No backup, or the user didn't want to try. Show Unable to Open Project dialog box.
			UnlockProject();
			throw new FwStartupException(message);
		}

		private void BasicInit()
		{
			m_lastWriteTime = File.GetLastWriteTimeUtc(ProjectId.Path);
			try
			{
				m_lockFile = LockProject(ProjectId.Path);
			}
			catch (IOException e)
			{
				throw new FwStartupException(String.Format(Properties.Resources.kstidLockFileLocked, ProjectId.Name), e, true);
			}
		}

		public static bool IsFileLocked(string projectPath)
		{
			try
			{
				LockProject(projectPath).Close();
				return false;
			}
			catch (IOException)
			{
				return true;
			}
		}

		private static FileStream LockProject(string projectPath)
		{
			return File.Open(projectPath + ".lock", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
		}

		private void UnlockProject()
		{
			if (m_lockFile != null)
			{
				m_lockFile.Close();
				File.Delete(ProjectId.Path + ".lock");
				m_lockFile = null;
			}
		}

		///// <summary>
		///// Restore from a data migration, which used an XML BEP.
		///// </summary>
		///// <param name="xmlBepPathname"></param>
		//protected override void RestoreWithoutMigration(string xmlBepPathname)
		//{
		//    // Copy original file to backup.
		//    var bakPathname = m_databasePath + FwFileExtensions.ksFwDataFallbackFileExtension;
		//    if (File.Exists(bakPathname))
		//        File.Delete(bakPathname);
		//    File.Copy(m_databasePath, bakPathname);
		//    // Copy/Rename 'xmlBepPathname' as m_pathname.
		//    // Zap file, so copy is happy.
		//    File.Delete(m_databasePath);
		//    File.Copy(xmlBepPathname, m_databasePath);
		//    // Re-load m_pathname
		//    StartupInternal(ModelVersion, new[] { m_databasePath });
		//}

		/// <summary>
		/// Create a LangProject with the BEP.
		/// </summary>
		protected override void CreateInternal()
		{
			// Make sure the directory exists
			if (!String.IsNullOrEmpty(ProjectId.ProjectFolder) && !Directory.Exists(ProjectId.ProjectFolder))
				Directory.CreateDirectory(ProjectId.ProjectFolder);
			BasicInit();

			if (File.Exists(ProjectId.Path))
				throw new InvalidOperationException(ProjectId.Path + " already exists.");
			// Make empty one here, so it will exist on Commit.
			var doc = new XDocument(new XElement("languageproject",
				new XAttribute("version", m_modelVersionOverride.ToString()))); // Include current model version number.
			doc.Save(ProjectId.Path, SaveOptions.None);
			m_lastWriteTime = File.GetLastWriteTimeUtc(ProjectId.Path);
		}

		#region IDataStorer implementation

		/// <summary>
		/// Update the backend store.
		/// </summary>
		/// <param name="newbies">The newly created objects</param>
		/// <param name="dirtballs">The recently modified objects</param>
		/// <param name="goners">The recently deleted objects</param>
		public override bool Commit(HashSet<ICmObjectOrSurrogate> newbies, HashSet<ICmObjectOrSurrogate> dirtballs, HashSet<ICmObjectId> goners)
		{
			IEnumerable<CustomFieldInfo> cfiList;
			if (!HaveAnythingToCommit(newbies, dirtballs, goners, out cfiList) && (m_startupVersionNumber == ModelVersion))
				return true;

			if (m_thread == null || !m_thread.WaitForNextRequest())
			{
				// If thread is already dead, then WaitForNextRequest will return false, but we still have to call Dispose() on it.
				if (m_thread != null)
					m_thread.Dispose();

				m_thread = new ConsumerThread<int, CommitWork>(Work);
				m_thread.Start();
			}

			m_thread.EnqueueWork(new CommitWork(newbies, dirtballs, goners, cfiList));

			return base.Commit(newbies, dirtballs, goners);
		}

		/// <summary>
		/// The XML backend has finished all commits when its write thread is idle.
		/// </summary>
		public override void CompleteAllCommits()
		{
			base.CompleteAllCommits();
			if (m_thread != null)
				m_thread.WaitUntilIdle();
		}

		private static void ReportProblem(string message, string tempPath)
		{
			if (File.Exists(tempPath))
			{
				try
				{
					File.Delete(tempPath);
				}
				catch (IOException)
				{
					// Can't even clean up. Sigh.
				}
			}
			ThreadHelper.ShowMessageBox(null, message, Strings.ksProblemWritingFile,
				MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		/// <summary>
		/// Performs the actual commit operation.
		/// </summary>
		/// <param name="queueAccessor">The queue accessor.</param>
		private void Work(IQueueAccessor<int, CommitWork> queueAccessor)
		{
			CommitWork workItem = null;
			IEnumerable<CommitWork> workItems = queueAccessor.GetAllWorkItems();
			// combine all queued work in to one work item
			foreach (CommitWork curWorkItem in workItems)
			{
				if (workItem == null)
					workItem = curWorkItem;
				else
					workItem.Combine(curWorkItem);
			}

			if (workItem == null)
				return;

			// Check m_lastWriteTime against current mod time,
			// to see if anyone else modified it, while we weren't watching.
			// Can't lock the file, so it is possible someone could be ill-behaved and have modified it.
			var currentWriteTime = File.GetLastWriteTimeUtc(ProjectId.Path);
			if (m_lastWriteTime != currentWriteTime)
			{
				var msg = String.Format(Strings.ksFileModifiedByOther, m_lastWriteTime, currentWriteTime);
				ReportProblem(msg, null);
				return;
			}

			// use a temp file in case the write doesn't work. Want to write temp file to a local drive
			// if the final destination is not a fixed drive writing record by record to a remote drive
			// can be slow.
			bool fUseLocalTempFile = !IsLocalDrive(ProjectId.Path);
			var tempPathname = fUseLocalTempFile ? Path.GetTempFileName() : Path.ChangeExtension(ProjectId.Path, "tmp");

			try
			{
				using (var writer = FdoXmlServices.CreateWriter(tempPathname))
				{
					// Create a reader on the old version
					// so we can move its data to the new file.
					using (var reader = FdoXmlServices.CreateReader(ProjectId.Path))
					{
						reader.MoveToContent();
						var str = reader.LocalName;
						if (str != "languageproject")
							throw new ArgumentException("XML not recognized.");

						FdoXmlServices.WriteStartElement(writer, m_modelVersionOverride);
						if (workItem.CustomFields.Count > 0)
						{
							// Write out optional custom fields
							var customPropertyDeclarations = new XElement("AdditionalFields");
							foreach (var customFieldInfo in workItem.CustomFields)
							{
								customPropertyDeclarations.Add(new XElement("CustomField",
									new XAttribute("name", customFieldInfo.m_fieldname),
									new XAttribute("class", customFieldInfo.m_classname),
									new XAttribute("type", GetFlidTypeAsString(customFieldInfo.m_fieldType)),
									(customFieldInfo.m_destinationClass != 0) ? new XAttribute("destclass", customFieldInfo.m_destinationClass.ToString()) : null,
									(customFieldInfo.m_fieldWs != 0) ? new XAttribute("wsSelector", customFieldInfo.m_fieldWs.ToString()) : null,
									(!String.IsNullOrEmpty(customFieldInfo.m_fieldHelp)) ? new XAttribute("helpString", customFieldInfo.m_fieldHelp) : null,
									(customFieldInfo.m_fieldListRoot != Guid.Empty) ? new XAttribute("listRoot", customFieldInfo.m_fieldListRoot.ToString()) : null,
									(customFieldInfo.Label != customFieldInfo.m_fieldname) ? new XAttribute("label", customFieldInfo.Label) : null));
							}
							DataSortingService.SortCustomPropertiesRecord(customPropertyDeclarations);
							DataSortingService.WriteElement(writer, customPropertyDeclarations);
						}

						var sortableProperties = m_mdcInternal.GetSortableProperties();

						if (reader.IsEmptyElement)
						{
							// Make sure there are no dirtballs or goners.
							if (workItem.Dirtballs.Count > 0)
								throw new InvalidOperationException("There are modified objects in a new DB system.");
							if (workItem.Goners.Count > 0)
								throw new InvalidOperationException("There are deleted objects in a new DB system.");
							// Add all new objects.
							foreach (var newbyXml in workItem.Newbies.Values)
							{
								DataSortingService.WriteElement(writer,
									DataSortingService.SortMainElement(sortableProperties, DataSortingService.Utf8.GetString(newbyXml)));
							}
						}
						else
						{
							// Copy unchanged objects to new file.
							// Replace modified objects with new content.
							// Delete goners.
							var keepReading = reader.Read();
							while (keepReading)
							{
								// Eat optional AdditionalFields element.
								if (reader.LocalName == "AdditionalFields")
								{
									while (reader.LocalName != "rt" && !reader.EOF)
										reader.Read();
								}
								if (reader.EOF)
									break;

								// 'rt' node is current node in reader.
								// Fetch Guid from 'rt' node and see if it is in either
								// of the modified/deleted dictionaries.
								var transferUntouched = true;
								var currentGuid = new Guid(reader.GetAttribute("guid"));

								// Add new items before 'currentGuid', if their guids come before 'currentGuid'.
								// NB: workItem.Newbies will no longer contain items returned by GetLessorNewbies.
								foreach (var newbieXml in DataSortingService.GetLessorNewbies(currentGuid, workItem.Newbies))
								{
									DataSortingService.WriteElement(writer,
										DataSortingService.SortMainElement(sortableProperties, DataSortingService.Utf8.GetString(newbieXml)));
								}

								if (workItem.Goners.Contains(currentGuid))
								{
									// Skip this record, since it has been deleted.
									keepReading = reader.ReadToNextSibling("rt");
									workItem.Goners.Remove(currentGuid);
									transferUntouched = false;
								}
								byte[] dirtballXml;
								if (workItem.Dirtballs.TryGetValue(currentGuid, out dirtballXml))
								{
									// Skip this record, since it has been modified.
									reader.ReadOuterXml();
									//reader.Skip();
									keepReading = reader.IsStartElement();
									// But, add updated data for the modified record.
									DataSortingService.WriteElement(writer,
										DataSortingService.SortMainElement(sortableProperties, DataSortingService.Utf8.GetString(dirtballXml)));
									workItem.Dirtballs.Remove(currentGuid);
									transferUntouched = false;
								}
								if (!transferUntouched) continue;

								// Copy old data into new file, since it has not changed.
								writer.WriteNode(reader, true);
								keepReading = reader.IsStartElement();
							}

							// Add all remaining new records to end of file, since they couldn't be added earlier.
							foreach (var newbieXml in workItem.Newbies.Values)
							{
								DataSortingService.WriteElement(writer,
									DataSortingService.SortMainElement(sortableProperties, DataSortingService.Utf8.GetString(newbieXml)));
							}
						}

						writer.WriteEndElement(); // 'languageproject'
						writer.Close();
					}
				}
			}
			catch (Exception e)
			{
				ReportProblem(String.Format(Strings.ksCannotSave, ProjectId.Path, e.Message), tempPathname);
			}
			CopyTempFileToOriginal(fUseLocalTempFile, ProjectId.Path, tempPathname);
			m_startupVersionNumber = m_modelVersionOverride;
		}

		private void CopyTempFileToOriginal(bool fUseLocalTempFile, string mainPathname, string tempPathname)
		{
			if(!File.Exists(tempPathname))
			{
				//There is no temp file to copy, there was probably an error previous to this call.
				//Nothing to do here, so move along.
				return;
			}
			try
			{
				if (fUseLocalTempFile)
				{
					var tempCopyPathname = Path.ChangeExtension(mainPathname, "tmp");
					if (File.Exists(tempCopyPathname))
						File.Delete(tempCopyPathname);
					// Use copy rather than move so that permissions get created according to destination
					// folder
					File.Copy(tempPathname, tempCopyPathname);
					File.Delete(tempPathname);
					tempPathname = tempCopyPathname;
				}
				var backPathName = Path.ChangeExtension(mainPathname, "bak");
				if (File.Exists(backPathName))
					File.Delete(backPathName);
				File.Move(ProjectId.Path, backPathName);
				File.Move(tempPathname, mainPathname);
			}
			catch (Exception e)
			{
				// Here we keep the temp file...we got far enough that it may be useful.
				ReportProblem(String.Format(Strings.ksCannotWriteBackup, ProjectId.Path, e.Message), null);
				return;
			}
			m_lastWriteTime = File.GetLastWriteTimeUtc(mainPathname);
			// This seems as though it should do nothing, and maybe it does. However .NET says the LastWriteTime can be
			// unreliable depending on the underlying OS. We've had some problems with it apparently being inaccurate (FWR-3190).
			// What I'm trying to do here is guard against the possibility that the system hasn't actually finished all the
			// write operations yet and the last write time will change again when it has.
			// I'm hoping that explicitly setting it as the last thing we do will ensure that the time we set (which is the
			// one we just read) will indeed be the time that is written.
			File.SetLastWriteTimeUtc(mainPathname, m_lastWriteTime);
		}

		private bool IsLocalDrive(string path)
		{
			if (path.StartsWith("\\\\"))
				return false;
			var driveinfo = new DriveInfo(new FileInfo(path).Directory.Root.FullName);
			return driveinfo.DriveType == DriveType.Fixed;
		}

		/// <summary>
		/// Update the version number.
		/// </summary>
		protected override void UpdateVersionNumber()
		{
			// Just store it for the next Commit call,
			// rather than going to all the trouble to do it now.
			m_modelVersionOverride = ModelVersion;
		}

		#endregion IDataStorer implementation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Rename the database, which means renaming the files.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool RenameDatabase(string sNewProjectName)
		{
			bool projectIsInDefaultLocation = DirectoryFinder.IsSubFolderOfProjectsDirectory(ProjectId.ProjectFolder);
			string sNewProjectFolder = projectIsInDefaultLocation ?
				Path.Combine(DirectoryFinder.ProjectsDirectory, sNewProjectName) : ProjectId.ProjectFolder;
			if (FileUtils.NonEmptyDirectoryExists(sNewProjectFolder))
				return false; // Destination directory already exists

			try
			{
				UnlockProject();

				if (projectIsInDefaultLocation)
					Directory.Move(ProjectId.ProjectFolder, sNewProjectFolder);
				string ext = Path.GetExtension(ProjectId.Path);
				string oldFile = Path.Combine(sNewProjectFolder, ProjectId.Name + ext);
				string newFile = Path.Combine(sNewProjectFolder, sNewProjectName + ext);
				File.Move(oldFile, newFile);
				ProjectId.Path = newFile;

				m_stopLoadDomain = true;
				m_lockFile = LockProject(newFile);
			}
			catch
			{
				return false;
			}
			return true;
		}

		static string GetAttribute(byte[] name, byte[] input)
		{
			int start = input.IndexOfSubArray(name);
			if (start == -1)
				return null;
			start += name.Length;
			int end = Array.IndexOf(input, s_close, start);
			if (end == -1)
				return null; // error
			return Encoding.UTF8.GetString(input.SubArray(start, end - start));
		}

		private static readonly byte[] s_guid = Encoding.UTF8.GetBytes("guid=\"");
		private static readonly byte[] s_class = Encoding.UTF8.GetBytes("class=\"");
		private static readonly byte s_close = Encoding.UTF8.GetBytes("\"")[0];

		void MakeSurrogate(byte[] xmlBytes)
		{
			var surrogate = m_surrogateFactory.Create(
				new Guid(GetAttribute(s_guid, xmlBytes)),
				GetAttribute(s_class, xmlBytes),
				xmlBytes);
			if (m_needConversion)
				RegisterSurrogateForConversion(surrogate);
			else
				RegisterInactiveSurrogate(surrogate);

		}
	}

	/// <summary>
	/// Responsible to read a file which has a sequence of similar elements
	/// and pass a byte array of each element to a delgate for processing.
	/// Use low-level byte array methods, overlapping file read with parsing the file
	/// and calling the delegate.
	/// The expectation is that, after some unspecified header material, the file consists
	/// of a sequence of elements, all with the same tag (e.g., "rt" elements in FieldWorks XML backend).
	/// Following the last we expect a close tag for the containing element, e.g., "/languageProjet" in Fieldworks.
	/// These two tags can be configured so the class can be used in other ways.
	/// Enhance JohnT: Experiments on Websters indicate we're spending appreciable time
	/// on the parsing and calling the delegate. We could potentially do the parsing and delegate
	/// calling on different threads, perhaps even (since the order of calling delegates on different
	/// elements does not matter) split the parsing over multiple threads.
	/// </summary>
	class ElementReader : IDisposable
	{
		enum BufferStatus
		{
			kNotInUse,
			kReadingData,
			kBeingProcessed
		}

		private readonly byte[] m_openingMarker;
		private readonly byte[] m_finalClosingTag;
		private const int kbufCount = 2; // number of buffers we use.
		private string m_pathname;
		private FileStream m_reader;
		private byte[][] m_buffers = new byte[kbufCount][];
		private BufferStatus[] m_states = new BufferStatus[kbufCount];
		private int[] m_bufferLengths = new int[kbufCount]; // length of useful data in each buffer; only meaningful if state is being-processed
		private IAsyncResult[] m_tokens = new IAsyncResult[kbufCount]; // token from BeginRead to pass to EndRead when we need the data.
		private int m_nextReadBuffer;  // The buffer we will next read into.
		private int m_currentProcessBuffer; // the buffer we are currently processing
		private Action<byte[]> m_outputHandler;
		private byte[] m_currentBuffer; // one of m_buffers, that contains data we are currently reading.
		private int m_currentIndex; // position of next character to read in m_currentBuffer.
		private int m_currentBufIndex; // index where m_currentBuffer is found in m_buffers.
		private int m_currentBufLength; // number of bytes available in m_currentBuffer.

		// MS doc says this is the smallest buffer that will produce real async reads.
		// We want them relatively small so we can start overlapping quickly.
		private const int kInitBufSize = 65536;


		public ElementReader(string openingMarker, string finalClosingTag, string pathname, Action<byte[]> outputHandler)
		{
			if (!openingMarker.EndsWith(" "))
				openingMarker += " ";
			var enc = Encoding.UTF8;
			m_openingMarker = enc.GetBytes(openingMarker);
			m_finalClosingTag = enc.GetBytes(finalClosingTag);
			m_pathname = pathname;
			m_reader = new FileStream(m_pathname, FileMode.Open, FileAccess.Read, FileShare.None, 4096, true);
			for (int i = 0; i < kbufCount; i++)
			{
				m_buffers[i] = new byte[kInitBufSize];
				m_states[i] = BufferStatus.kNotInUse;
			}
			m_nextReadBuffer = 0;

			m_outputHandler = outputHandler;
		}

		public void Run()
		{
			FillBuffer();
			if (!AdvanceToMarkerElement())
				return; // no elements!
			while (GetRtElement())
			{
			}
		}

		private static byte closeBracket = Encoding.UTF8.GetBytes(">")[0];

		/// <summary>
		/// Called when we have just read the input marker. Advances to the
		/// start of closing tag or just after the next element.
		/// </summary>
		/// <returns>false, if we have found the closing tag, otherwise true</returns>
		private bool GetRtElement()
		{
			// Record a start position (which is AFTER the opening tag).
			// Record the INDEX of the buffer rather than the buffer itself;
			// advancing to the next marker may occasionally involve growing the buffer.
			var startBufIndex = m_currentBufIndex;
			var startIndex = m_currentIndex;

			bool result = AdvanceToMarkerElement();
			var startBuffer = m_buffers[startBufIndex];

			// By default the end of the element is right before the opening tag we just found in the input.
			var endBuffer = m_currentBuffer;
			int limit;
			if (result)
				limit = m_currentIndex - m_openingMarker.Length;
			else
			{
				// we're doing the last element, which ends (with a </openingTag> that we don't detect) just before the
				// closing finalClosingTag. Assuming a basically correct file format, to find the end
				// of the closing tag of the last element we need to go back by the length of the final closing tag
				// (of the enclosing document).
				limit = m_currentIndex - m_finalClosingTag.Length;
			}
			// Further problem: stepping back might have spanned the buffer boundary
			if (limit < 0)
			{
				endBuffer = startBuffer;
				limit += startBuffer.Length;
			}
			// Move back to the closing > (which, again, may be back into the startBuffer).
			while (limit > startIndex || endBuffer != startBuffer)
			{
				if (limit == 0)
				{
					endBuffer = startBuffer;
					limit = startBuffer.Length;
				}
				if (endBuffer[limit-1] == closeBracket)
					break;
				limit--;
			}
			int count = limit - startIndex;
			if (startBuffer != endBuffer)
				count = startBuffer.Length - startIndex + limit; // end of startBuffer plus start of endBuffer
			byte[] xmlBytes = new byte[count + m_openingMarker.Length];
			// startIndex is AFTER the previous match of m_openingMarker, so we need to throw that in.
			Array.Copy(m_openingMarker, 0, xmlBytes, 0, m_openingMarker.Length);
			if (startBuffer == endBuffer)
			{
				Array.Copy(startBuffer, startIndex, xmlBytes, m_openingMarker.Length, count);
				// In at least one pathological case, we may not yet have freed the other buffer.
				int otherBufIndex = NextBuffer(m_currentBufIndex);
				if (m_states[otherBufIndex] == BufferStatus.kBeingProcessed)
				{
					m_states[otherBufIndex] = BufferStatus.kNotInUse;
					StartReadingBuffer();
				}
			}
			else
			{
				Array.Copy(startBuffer, startIndex, xmlBytes, m_openingMarker.Length, startBuffer.Length - startIndex);
				Array.Copy(endBuffer, 0, xmlBytes, m_openingMarker.Length + startBuffer.Length - startIndex, limit);
				// We've now finished processing startBuffer, so it is free to use for more input.
				m_states[NextBuffer(m_currentBufIndex)] = BufferStatus.kNotInUse;
				StartReadingBuffer();
			}
			m_outputHandler(xmlBytes);
			return result;
		}

		// Return true if there are more bytes to read. Refill the buffer if need be.
		// After this is called and returns true, m_currentBuffer[m_currentIndex] contains a valid next character.
		private bool More()
		{
			if (m_currentIndex < m_currentBufLength)
				return true;
			if (m_currentBufLength < m_currentBuffer.Length)
				return false; // hit end of file on last read, since we didn't fill the buffer entirely.
			FillBuffer();
			return m_currentIndex < m_currentBufLength;
		}

		/// <summary>
		/// Advance input, copying characters read to m_output if it is non-null, until we
		/// have successfully read the target marker, or reached EOF. Return true if we found it.
		/// Assumes m_marker is at least two characters. Also expects it to be an XML element marker,
		/// or at least that it's first character does not recur in the marker.
		/// </summary>
		/// <returns></returns>
		private bool AdvanceToMarkerElement()
		{
			var openingAngleBracket	= m_openingMarker[0];
			int openMarkerLengthMinus1 = m_openingMarker.Length - 1;
			while (true)
			{
				// The first condition is redundant, but should be faster to execute than calling More(), and will almost always fail.
				while (m_currentIndex < m_currentBufLength || More())
				{
					var nextByte = m_currentBuffer[m_currentIndex++];
					// FWR-9296: nulls may be in data that was not completely written to disk
					if (nextByte == 0)
						throw new XmlException("null characters not permitted in XML");
					if (nextByte == openingAngleBracket)
						break;
				}

				if (!More())
					return false;
				// Try to match the rest of the marker.
				for (int i = 1;; i++)
				{
					if (!More())
						return false;
					// FWR-9296: nulls may be in data that was not completely written to disk
					if (m_currentBuffer[m_currentIndex] == 0)
						throw new XmlException("null characters not permitted in XML");
					if (m_openingMarker[i] != m_currentBuffer[m_currentIndex++])
						break; // no match, resume searching for opening character.
					if (i == openMarkerLengthMinus1)
						return true; // got it!
				}
			}
		}

		private int NextBuffer(int bufIndex)
		{
			if (bufIndex < 1)
				return bufIndex + 1;
			return 0;
		}

		void StartReadingBuffer()
		{
			var buffer = m_buffers[m_nextReadBuffer];
			if (m_states[m_nextReadBuffer] != BufferStatus.kNotInUse)
				return; // no available buffer, we'll try to fill again when we finish processing one.
			// If we had to extend the other buffer, we should extend this one too, to keep the reading
			// and processing in balance.
			var otherBufferLength = m_buffers[NextBuffer(m_nextReadBuffer)].Length;
			if (buffer.Length < otherBufferLength)
				buffer = m_buffers[m_nextReadBuffer] = new byte[otherBufferLength];
			m_states[m_nextReadBuffer] = BufferStatus.kReadingData;
			m_tokens[m_nextReadBuffer] = m_reader.BeginRead(buffer, 0, buffer.Length, null, null);
		}

		/// <summary>
		/// Get some more data to process. Initially, buffer[m_nextReadBuffer] is in state non-in-use,
		/// and we initiate a read into it. After that, we should typically find that buffer[m_nextReadBuffer]
		/// is in state being-read, and we wait for that read to finish. Then we initiate a new read on the
		/// next buffer, asssuming it is not in use.
		/// </summary>
		private void FillBuffer()
		{
			if (m_states[m_nextReadBuffer] == BufferStatus.kNotInUse)
				StartReadingBuffer();
			if (m_states[m_nextReadBuffer] == BufferStatus.kReadingData)
			{
				m_currentBufIndex = m_nextReadBuffer;
				m_currentBufLength = m_bufferLengths[m_currentBufIndex] = m_reader.EndRead(m_tokens[m_currentBufIndex]);
				m_currentBuffer = m_buffers[m_currentBufIndex];
				m_currentIndex = 0;
				m_states[m_currentBufIndex] = BufferStatus.kBeingProcessed;
				m_nextReadBuffer = NextBuffer(m_nextReadBuffer);
				StartReadingBuffer();
				return;
			}
			// The remaining possibility is that the next buffer we would use is still in use.
			// That is a pathological case that occurs when a single target element extends all the way from
			// somewhere in one buffer through another whole buffer and we still haven't found the end.
			// When this happens we make the buffer bigger and fill it at once (synchronously, basically).
			int oldLength = m_currentBuffer.Length;
			var newBuffer = new byte[oldLength * 2];
			Array.Copy(m_currentBuffer, newBuffer, oldLength); // doc indicates it could be dangerous to overlap this
			var token = m_reader.BeginRead(newBuffer, oldLength, oldLength, null, null);
			m_buffers[m_currentBufIndex] = newBuffer;
			m_currentBuffer = newBuffer;
			int newReadLength = m_reader.EndRead(token);
			m_currentBufLength = m_bufferLengths[m_currentBufIndex] = newReadLength + oldLength;
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~ElementReader()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				m_reader.Dispose();
			}
			m_reader = null;
			IsDisposed = true;
		}
		#endregion
	}
}
