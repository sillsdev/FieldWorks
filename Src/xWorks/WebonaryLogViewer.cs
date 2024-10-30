using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Windows.Forms;
using Newtonsoft.Json;
using SIL.FieldWorks.Common.FwUtils;
using SIL.IO;
using SIL.Windows.Forms.CheckedComboBox;
using static SIL.FieldWorks.XWorks.WebonaryUploadLog;

namespace SIL.FieldWorks.XWorks
{
	public partial class WebonaryLogViewer : Form
	{
		private List<WebonaryUploadLog.UploadLogEntry> _logEntries = new List<UploadLogEntry>();
		private readonly ResourceManager _resourceManager;
		private string logFilePath;

		private class ComboBoxItem
		{
			public string Text { get; set; }
			public WebonaryStatusCondition Value { get; set; }

			public ComboBoxItem(string text, WebonaryStatusCondition value)
			{
				Text = text;
				Value = value;
			}

			public override string ToString()
			{
				return Text;
			}
		}

		public WebonaryLogViewer(string filePath)
		{
			InitializeComponent();
			_resourceManager = new ResourceManager("SIL.FieldWorks.XWorks.WebonaryLogViewer", typeof(WebonaryLogViewer).Assembly);

			// Set localized text for UI elements
			loadDataGridView(filePath);
			logFilePath = filePath;
			saveLogButton.Click += SaveLogButton_Click;
			filterBox.Items.AddRange(new [] {new ComboBoxItem(xWorksStrings.WebonaryLogViewer_Full_Log, WebonaryStatusCondition.None),
				new ComboBoxItem(xWorksStrings.WebonaryLogViewer_Rejected_Files, WebonaryStatusCondition.FileRejected),
				new ComboBoxItem(xWorksStrings.WebonaryLogViewer_Errors_Warnings, WebonaryStatusCondition.Error)});
			filterBox.SelectedIndex = 0;
			filterBox.SelectedIndexChanged += FilterListBox_SelectedIndexChanged;
		}

		private void loadDataGridView(string filePath)
		{
			try
			{
				// Read and deserialize JSON from file
				foreach (var line in File.ReadLines(filePath).Where(line => !string.IsNullOrWhiteSpace(line)))
				{
					try
					{
						var entry = JsonConvert.DeserializeObject<UploadLogEntry>(line);
						_logEntries.Add(entry);
					}
					catch (JsonException ex)
					{
						Console.WriteLine($"Error deserializing line: {ex.Message}");
					}
				}
				// Bind data to DataGridView
				logEntryView.DataSource = _logEntries;
				logEntryView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
				// The messages column should show all the content and fill the remaining space
				logEntryView.Columns[logEntryView.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
				logEntryView.Columns[logEntryView.Columns.Count - 1].DefaultCellStyle.WrapMode = DataGridViewTriState.True;

			}
			catch (Exception ex)
			{
				// Log file not found or empty, just show an empty grid
			}
		}

		private void FilterListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Filter logic based on selected option
			var selectedFilter = ((ComboBoxItem)filterBox.SelectedItem).Value;
			switch (selectedFilter)
			{
				case WebonaryStatusCondition.FileRejected:
				case WebonaryStatusCondition.Error:
					logEntryView.DataSource = _logEntries.Where(entry => entry.Status == selectedFilter).ToList();
					break;
				default:
					logEntryView.DataSource = _logEntries;
					break;
			}
		}

		private void SaveLogButton_Click(object sender, EventArgs e)
		{
			using (SaveFileDialog saveFileDialog = new SaveFileDialog())
			{
				saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
				saveFileDialog.Title = xWorksStrings.WebonaryLogViewer_Save_a_copy;

				if (saveFileDialog.ShowDialog() == DialogResult.OK)
				{
					try
					{
						RobustFile.Copy(logFilePath, saveFileDialog.FileName, true);
					}
					catch (Exception ex)
					{
						MessageBoxUtils.Show(xWorksStrings.WebonaryLogViewer_CopyFileError, ex.Message);
					}
				}
			}
		}
	}
}