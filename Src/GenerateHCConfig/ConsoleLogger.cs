using System;
using System.ComponentModel;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.WordWorks.Parser;

namespace GenerateHCConfig
{
	internal class ConsoleLogger : IFdoUI, IHCLoadErrorLogger
	{
		private readonly ISynchronizeInvoke m_synchronizeInvoke;

		public ConsoleLogger(ISynchronizeInvoke synchronizeInvoke)
		{
			m_synchronizeInvoke = synchronizeInvoke;
		}

		public ISynchronizeInvoke SynchronizeInvoke
		{
			get { return m_synchronizeInvoke; }
		}

		public bool ConflictingSave()
		{
			throw new NotImplementedException();
		}

		public bool ConnectionLost()
		{
			Console.WriteLine("Connection lost.");
			return false;
		}

		public DateTime LastActivityTime
		{
			get { return DateTime.Now; }
		}

		public FileSelection ChooseFilesToUse()
		{
			throw new NotImplementedException();
		}

		public bool RestoreLinkedFilesInProjectFolder()
		{
			throw new NotImplementedException();
		}

		public YesNoCancel CannotRestoreLinkedFilesToOriginalLocation()
		{
			throw new NotImplementedException();
		}

		public void DisplayMessage(MessageType type, string message, string caption, string helpTopic)
		{
			Console.WriteLine(message);
		}

		public void ReportException(Exception error, bool isLethal)
		{
			Console.WriteLine(error.Message);
		}

		public void ReportDuplicateGuids(string errorText)
		{
			Console.WriteLine(errorText);
		}

		public void DisplayCircularRefBreakerReport(string msg, string caption)
		{
			Console.WriteLine("{0}: {1}", caption, msg);
		}

		public bool Retry(string msg, string caption)
		{
			throw new NotImplementedException();
		}

		public bool OfferToRestore(string projectPath, string backupPath)
		{
			throw new NotImplementedException();
		}

		public void Exit()
		{
			Environment.Exit(1);
		}

		public void InvalidShape(string str, int errorPos, IMoMorphSynAnalysis msa)
		{
			Console.WriteLine("The form \"{0}\" contains an undefined phoneme at {1}.", str, errorPos);
		}

		public void InvalidAffixProcess(IMoAffixProcess affixProcess, bool isInvalidLhs, IMoMorphSynAnalysis msa)
		{
			Console.WriteLine("The affix process \"{0}\" is invalid.", affixProcess.Form.BestVernacularAlternative.Text);
		}

		public void InvalidPhoneme(IPhPhoneme phoneme)
		{
			Console.WriteLine("The phoneme \"{0}\" does not contain any valid graphemes.", phoneme.Name.BestAnalysisVernacularAlternative.Text);
		}

		public void DuplicateGrapheme(IPhPhoneme phoneme)
		{
			Console.WriteLine("The phoneme \"{0}\" has the same grapheme as another phoneme.", phoneme.Name.BestAnalysisVernacularAlternative.Text);
		}

		public void InvalidEnvironment(IMoForm form, IPhEnvironment env, string reason, IMoMorphSynAnalysis msa)
		{
			Console.WriteLine("The environment \"{0}\" is invalid. Reason: {1}", env.StringRepresentation.Text, reason);
		}

		public void InvalidReduplicationForm(IMoForm form, string reason, IMoMorphSynAnalysis msa)
		{
			Console.WriteLine("The reduplication form \"{0}\" is invalid. Reason: {1}", form.Form.VernacularDefaultWritingSystem.Text, reason);
		}
	}
}
