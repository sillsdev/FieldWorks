using System;
using System.IO;
using DeanJackson.Tools;

namespace BlockCopy
{
	public class MainEntry
	{
		static string SourcePath;
		static string DestinationPath;
		static short BlockSize;
		static byte MaxRetries;
		static bool QuietMode;
		static bool LogToFile;

		// exit codes
		const int IncompleteParameters = 1;
		const int InvalidParameter = 2;
		const int InvalidSourceFile = 3;
		const int InvalidDestination = 4;
		const int ErrorOccurred = 5;

		[STAThread]
		static int Main(string[] args)
		{
			// *******
			// This is the main entry point to the program
			// *******

			int ExitCode = 0;

			ExitCode = ValidateParameters(args);
			if (ExitCode != 0)
				return ExitCode;

			ExitCode = ValidateFilePaths();
			if (ExitCode != 0)
				return ExitCode;

			ExitCode = CopyFile();

			return ExitCode;
		}


		static void ShowUsage()
		{
			// *******
			// This procedure displays the usage instructions and examples to the screen
			// *******

			Console.WriteLine("BCopy (Block Copy) - copies a file using a specified blocksize.\n\n" +
				"Usage: BCOPY source destination [/b:n] [/q] [/r:n] [/f]\n\n" +
				"parameters in brackets are optional\n");

			Console.WriteLine("  {0,-12} Specifies the file to be copied.\n" +
							  "  {1,-12} Specifies the directory and/or filename for the new file.\n" +
				"  {2,-12} Blocksize - kilobytes to copy at a time (max: " + short.MaxValue +" - no commas)\n" +
				"  {3,-12} Quiet - Do not display output to the screen.\n" +
				"  {4,-12} Number of times to retry a failed copy (default: 4  max: 50).\n" +
				"  {5,-12} Log errors to a file (default is the Application event log).",
								"source", "destination", "/b:n", "/q", "/r:n", "/f");

			Console.WriteLine ("\nExamples:\nbcopy c:\\source.txt z:\\destination.txt /b:300 /q /f\nThis copies the source " +
				"file 300kb at a time WITHOUT screen messages\nand errors logged to a file.");

			Console.WriteLine ("\nbcopy c:\\data.dat \\\\server\\share /b:1000 /q /r:5\nThis copies the source " +
				"file 1MB at a time WITHOUT screen messages,\n5 retry attempts and errors logged to the NT Application event log.");

			Console.WriteLine ("\nbcopy c:\\data.dat z:\\NewFile.dat /b:500 /f\nThis copies the source " +
				"file 500kb at a time WITH screen messages\nand errors logged to a file.");
		}


		static int ValidateParameters(string[] parameters)
		{
			// *******
			// This procedure validates the number of parameters and their values
			// *******

			// Input parameter order and description
			//  (1) source - source file
			//  (2) destination - destination file
			//  (3) /b: - blocksize (kilobytes to copy at a time)
			//  (4) /q - quite mode; don't display output
			//  (5) /r: - retry attempts; retries for a failed attempt (max. 50)
			//  (6) /f - log errors to file

			bool BadParm = false;
			string parmItem = null;

			// if the correct number of parameters aren't passed then show usage and exit
			if (parameters.Length < 2 || parameters.Length > 6)
			{
				ShowUsage();
				return IncompleteParameters;
			}

			if (parameters[0].Substring(0, 1) == "/" || parameters[1].Substring(0, 1) == "/")
			{
				ShowUsage();
				return IncompleteParameters;
			}


			// process required parameters
			SourcePath = parameters[0];
			DestinationPath = parameters[1];
			MaxRetries = 4;


			//
			// process optional parameters
			//
			for (byte parmCount = 2; parmCount < parameters.Length && BadParm == false; parmCount++)
			{
				parmItem = parameters[parmCount];
				string parmValue = null;

				// if parameter has a colon, handle it as a value parameter,
				// otherwise as a regular parameter.
				int ColonPosition = parmItem.IndexOf(':', 0);

				if (ColonPosition > 0)
				{
					// handle parameters with a value
					if (parmItem.Length < 4)
					{
						BadParm = true;
						break;
					}
					else
					{
						parmValue = parmItem.Substring(ColonPosition + 1, parmItem.Length - (ColonPosition + 1));

						if (parmItem.StartsWith("/b:"))
						{
							BlockSize = StringTools.ParseShort(parmValue);
							if (BlockSize == 0)
								BadParm = true;
							continue;
						}

						if (parmItem.StartsWith("/r:"))
						{
							MaxRetries = StringTools.ParseByte(parmValue);
							if (MaxRetries == 0 || MaxRetries > 50)
								BadParm = true;
							continue;
						}

						BadParm = true;
					}

				}
				else
					// handle regular parameters
					switch (parmItem)
					{
						case "/q":
							QuietMode = true;
							break;

						case "/f":
							LogToFile = true;
							break;

						default:
							BadParm = true;
							break;
					}
			}

			if (BadParm)
			{
				ProcessError(InvalidParameter, "ProcessParameters()" , "Invalid parameter - " + parmItem);
				return InvalidParameter;
			}
			else
				return 0;

		}


		static int ValidateFilePaths()
		{
			// *******
			// This procedure does basic checking to see if the source file exists and if
			// the destination directory exists.
			// *******

			// validate source file
			try
			{
				if (!File.Exists(SourcePath))
				{
					ProcessError(InvalidSourceFile, "ValidateFilePaths()", "Source file not found");
					return InvalidSourceFile;
				}
			}
			catch (Exception e)
			{
				ProcessError(InvalidSourceFile, "ValidateFilePaths()", e.Message);
				return InvalidSourceFile;
			}


			// if destination is a directory then alter destination path to
			// include the source file.
			try
			{
				if (Directory.Exists(DestinationPath))
				{
					if (DestinationPath.EndsWith("\\"))
						DestinationPath = DestinationPath + Path.GetFileName(SourcePath);
					else
						DestinationPath = DestinationPath + "\\" + Path.GetFileName(SourcePath);
				}
				else if (DestinationPath.EndsWith("\\"))
				{
					// can't end path in \ if it isn't a real directory
					ProcessError(InvalidDestination, "ValidateFilePaths()", "Destination directory not found");
					return InvalidDestination;
				}
				else if (! Directory.Exists(Path.GetDirectoryName(DestinationPath)))
				{
					// can't find destination directory
					ProcessError(InvalidDestination, "ValidateFilePaths()", "Destination directory not found");
					return InvalidDestination;
				}
			}
			catch (Exception e)
			{
				ProcessError(InvalidSourceFile, "ValidateFilePaths()", e.Message);
				return InvalidDestination;
			}

			return 0;
		}


		static int CopyFile()
		{
			// *******
			// This procedure is the heart of this program; it does the actual copying
			// of the file.  If no blocksize parameter was given, it will do a standard
			// Windows copy, rather than copying in blocks.  Either way, retry logic is
			// used and will eventuall give up if we can't succeed.
			// *******

			byte attempts = 0;
			bool QuitProcessing = false;
			bool CopyOK = false;


			// *********************
			// If blocksize is zero then do a standard Windows copy
			// and retry a few times if it fails.
			// *********************
			if (BlockSize == 0)
			{
				while (attempts <= MaxRetries)
				{
					try
					{
						File.Copy(SourcePath, DestinationPath, true);
						if (!QuietMode)
							Console.WriteLine ("File copied successfully.");
						return 0;
					}

					catch (IOException e)
					{
						ProcessError(ErrorOccurred, "CopyFile()", "Error copying file - " + e.Message);
					}
					catch (Exception e)
					{
						ProcessError(ErrorOccurred, "CopyFile()", "Error copying file - " + e.Message);
					}
					finally
					{
						attempts++;
					}
				}
				return ErrorOccurred;  // give up and exit
			}


			// *********************************
			// A blocksize was given, so we'll copy the file a block at a time.
			// Open source file for read access and lock it (no sharing access).
			// *********************************
			FileStream SourceStream = null;
			GetFileStream(SourcePath, FileMode.Open, FileAccess.Read, FileShare.None, MaxRetries, out SourceStream);

			if (SourceStream == null)
				return ErrorOccurred;


			// open destination file for writing
			FileStream DestStream = null;
			GetFileStream(DestinationPath, FileMode.Create, FileAccess.Write, FileShare.None, MaxRetries, out DestStream);

			if (DestStream == null)
			{
				SourceStream.Close();
				return ErrorOccurred;
			}


			//
			// loop through source stream and write bytes to destination
			//
			int BytesRead = 0;
			bool CopyStarted = false;
			byte[] BlockBuffer = new byte[BlockSize * 1024];  //set buffer to bytes (Kilobytes * 1024)
			long PrevDestPosition = 0;

			do
			{
				// read requested number of bytes (blocksize) from source file
				try
				{
					BytesRead = SourceStream.Read(BlockBuffer, 0, BlockBuffer.Length);
				}
				catch (NotSupportedException e)
				{
					ProcessError(ErrorOccurred, "CopyFile()", "Source doesn't support streaming - " + e.Message);
					break;
				}
				catch (IOException e)
				{
					ProcessError(ErrorOccurred, "CopyFile()", "Error reading source - " + e.Message);
					break;
				}
				catch (Exception e)
				{
					ProcessError(ErrorOccurred, "CopyFile()", "Error reading source - " + e.Message);
					break;
				}


				// write to destination file and retry if it fails because of an IO error
				CopyOK = false;
				attempts = 0;

				while (BytesRead !=0 && attempts <= MaxRetries && !CopyOK)
				{
					CopyStarted = true;

					try
					{
						DestStream.Write(BlockBuffer, 0, BytesRead);
						DestStream.Flush();
						PrevDestPosition = DestStream.Position;  // save this file position in case we have a error
						CopyOK = true;
					}
					catch (NotSupportedException e)
					{
						ProcessError(ErrorOccurred, "CopyFile()", "Destination doesn't support streaming - " + e.Message);
						QuitProcessing = true;
						break;
					}
					catch (IOException e)
					{
						// An error occurred so close the current destination stream; the original
						// stream will no longer be valid.
						ProcessError(ErrorOccurred, "CopyFile()", "Error writing block - " + e.Message);
						attempts++;
						DestStream.Close();
						DestStream = null;

						// wait 5 seconds then re-open the destination stream
						System.Threading.Thread.Sleep(5000);
						GetFileStream(DestinationPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, MaxRetries, out DestStream);

						// if we couldn't get the destination stream then abort
						if (DestStream == null)
						{
							QuitProcessing = true;
							break;
						}
						else
							// Go to the last good position in the file so we can rewrite this block.
							// note: if we fail here we don't retry.  If we fail here then the network
							// connection is going up and down too quickly anyway.
							try
							{
								DestStream.Position = PrevDestPosition;
							}
							catch (Exception NoPosition)
							{
								ProcessError(ErrorOccurred, "CopyFile()", "Couldn't reposition destination - " + NoPosition.Message);
								QuitProcessing = true;
								break;
							}
					}
				}
			}
			while (BytesRead !=0 && CopyOK && !QuitProcessing);


			// close the destination and source streams
			if (DestStream != null)
				DestStream.Close();
			SourceStream.Close();


			// If we reached the end of the source file (BytesRead = 0) and we began copying
			// the file, then we know we completed successfully.
			if (BytesRead == 0 && CopyStarted)
			{
				if (!QuietMode)
					Console.WriteLine("File copied in blocks successfully.");

				return 0;
			}
			else
				return ErrorOccurred;
		}


		static void GetFileStream(string filePath, FileMode modeType, FileAccess accessType, FileShare shareType, byte retries, out FileStream outStream)
		{
			// *******
			// This procedure retrieves an open handle (or "stream") to a file.  It's
			// used for both the source and destination files.  We use the retry
			// logic here and give up after all retry attempts have been made.
			// *******

			byte attempts = 0;

			while (attempts <= retries)
			{
				// If we're on the last attempt, then wait 10 seconds before trying
				// so hopefully things will clear up and we won't have to abort.
				if (attempts == retries)
					System.Threading.Thread.Sleep(10000);

				try
				{
					outStream = new FileStream(filePath, modeType, accessType, shareType);
					return;
				}
				catch (ArgumentException e)
				{
					ProcessError(ErrorOccurred, "GetFileStream()", "Invalid parameter - " + e.Message);
					break;
				}
				catch (System.Security.SecurityException e)
				{
					ProcessError(ErrorOccurred, "GetFileStream()", "Permission denied - " + e.Message);
					break;
				}
				catch (DirectoryNotFoundException e)
				{
					ProcessError(ErrorOccurred, "GetFileStream()", e.Message);
					break;
				}
				catch (FileNotFoundException e)
				{
					ProcessError(ErrorOccurred, "GetFileStream()", e.Message);
					break;
				}
				catch (UnauthorizedAccessException e)
				{
					ProcessError(ErrorOccurred, "GetFileStream()", e.Message);
					break;
				}
				catch (IOException e)
				{
					ProcessError(ErrorOccurred, "GetFileStream()", filePath + " - " + "(IO) " + e.Message);

					outStream = null;
					if (++attempts <= retries)
						System.Threading.Thread.Sleep(4000);  // wait a few seconds before trying again
				}
				catch (Exception e)
				{
					ProcessError(ErrorOccurred, "GetFileStream()", filePath + " - " + "(generic) " + e.Message);
					break;
				}
			}

			outStream = null;
			return;
		}


		static void ProcessError(int number, string source, string message)
		{
			// *******
			// This procedure handles all errors including incorrect parameters.
			// *******

			// If logging to a file was selected by a parameter then use the file,
			// otherwise use the NT Application Event log.
			if (LogToFile)
				StringTools.LogErrorToFile("BCopy_Log.txt", "BCopy Logs", number, source, message, "source: " + SourcePath +
					"  destination: " + DestinationPath + "  blocksize: " + BlockSize);
			else
				StringTools.LogToEventLog("BCopy", message, System.Diagnostics.EventLogEntryType.Error, number);

			// display message to screen unless running in quiet mode
			if (!QuietMode)
				Console.WriteLine(message);
		}
	}

}