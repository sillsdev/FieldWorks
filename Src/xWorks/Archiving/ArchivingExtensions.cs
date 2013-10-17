using System.Text;

namespace SIL.FieldWorks.XWorks.Archiving
{
	public static class ArchivingExtensions
	{
		public static void AppendLineFormat(this StringBuilder sb, string format, object[] args, string delimiter)
		{
			if (sb.Length != 0) sb.Append(delimiter);
			sb.AppendFormat(format, args);
		}
	}
}
