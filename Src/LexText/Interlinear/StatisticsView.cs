using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.XWorks;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.IText
{
	public partial class StatisticsView : UserControl, IxCoreContentControl, IFWDisposable
	{
		private bool _shouldNotCall;

		private string _areaName;
		private Mediator mediator;
		private InterlinearTextsRecordClerk clerk;

		public StatisticsView()
		{
			InitializeComponent();
		}

		public string AccName
		{
			get { return ITextStrings.ksTextAreaStatisticsViewName; }
		}

		#region Implementation of IxCoreCtrlTabProvider

		/// <summary>
		/// Gather up suitable targets to Cntrl-(Shift-)Tab into.
		/// </summary>
		/// <param name="targetCandidates">List of places to move to.</param>
		/// <returns>A suitable target for moving to in Ctrl(+Shift)+Tab.
		/// This returned value should also have been added to the main list.</returns>
		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			targetCandidates.Add(this);
			return this;
		}

		#endregion

		#region Implementation of IxCoreColleague

		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();
			this.mediator = mediator; //allows the Cache property to function

			string name = XmlUtils.GetAttributeValue(configurationParameters, "clerk");
			clerk = (InterlinearTextsRecordClerk) (mediator.PropertyTable.GetValue(name) ??
												   RecordClerkFactory.CreateClerk(mediator, configurationParameters, true));

			RebuildStatisticsTable();
			//add ourselves so that we can receive messages (related to the text selection currently misnamed AddTexts)
			mediator.AddColleague(this);
			//add our current state to the history system
			string toolName = mediator.PropertyTable.GetStringProperty("currentContentControl", "");
			mediator.SendMessage("AddContextToHistory", new FwLinkArgs(toolName, Guid.Empty), false);
		}

		private void RebuildStatisticsTable()
		{
			statisticsTable.Controls.Clear();
			//retrieve the default UI font.
			var font = FontHeightAdjuster.GetFontForStyle(StyleServices.NormalStyleName,
														  FontHeightAdjuster.StyleSheetFromMediator(mediator),
														  Cache.DefaultUserWs, Cache.WritingSystemFactory);
			//increase the size of the default UI font and make it bold for the header.
			Font headerFont = new Font(font.FontFamily, font.SizeInPoints + 1.0f, FontStyle.Bold, font.Unit, font.GdiCharSet);

			//refresh the statisticsDescription (in case of font changes)
			statisticsDescription.Text = ITextStrings.ksStatisticsView_HeaderText;
			statisticsDescription.Font = headerFont;
			statisticsDescription.UseMnemonic = false;

			Label segmentCountLabel = new Label
										{
											Font = font,
											AutoSize = true,
											Anchor = AnchorStyles.Right,
											TextAlign = ContentAlignment.MiddleRight,
											Text = ITextStrings.ksStatisticsViewTotalSentencesText
										};
			Label segmentCount = new Label
										{
											Font = font,
											AutoSize = true,
											Anchor = AnchorStyles.Left,
											TextAlign = ContentAlignment.MiddleLeft
										};
			Label totalWordTokenCountLabel = new Label
										{
											Font = font,
											AutoSize = true,
											Anchor = AnchorStyles.Right,
											TextAlign = ContentAlignment.MiddleRight,
											Text = ITextStrings.ksStatisticsViewTotalWordTokensText
										};
			Label totalWordTokenCount = new Label
									{
										Font = font,
										Anchor = AnchorStyles.Left,
										TextAlign = ContentAlignment.MiddleLeft
									};
			Label totalWordTypeCountLabel = new Label
			{
				Font = font,
				AutoSize = true,
				Anchor = AnchorStyles.Right,
				TextAlign = ContentAlignment.MiddleRight,
				Text = ITextStrings.ksStatisticsViewTotalWordTypesText
			};

			Label totalWordTypeCount = new Label
			{
				Font = font,
				Anchor = AnchorStyles.Left,
				TextAlign = ContentAlignment.MiddleLeft
			};
			int row = 0;
			var textList = InterestingTextsDecorator.GetInterestingTextList(mediator, Cache.ServiceLocator);
			int numberOfSegments = 0;
			int wordCount = 0;
			int uniqueWords = 0;

			Dictionary<int, int> languageCount = new Dictionary<int, int>();
			Dictionary<int, Set<String>> languageTypeCount = new Dictionary<int, Set<String>>();
			//for each interesting text
			foreach(var text in textList.InterestingTexts)
			{
				//for every paragraph in the interesting text
				for(int index = 0; index < text.ParagraphsOS.Count; ++index)
				{
					//count the segments in this paragraph
					numberOfSegments += text[index].SegmentsOS.Count;
					//count all the things analyzed as words
					var words = new List<IAnalysis>(text[index].Analyses);
					foreach (var word in words)
					{
						var wordForm = word.Wordform;
						if(wordForm != null)
						{
							var valdWSs = wordForm.Form.AvailableWritingSystemIds;
							foreach (var ws in valdWSs)
							{
								// increase the count of words(tokens) for this language
								int count = 0;
								if(languageCount.TryGetValue(ws, out count))
								{
									languageCount[ws] = count + 1;
								}
								else
								{
									languageCount.Add(ws, 1);
								}
								//increase the count of unique words(types) for this language
								Set<String> pair;
								if(languageTypeCount.TryGetValue(ws, out pair))
								{
									//add the string for this writing system in all lower case to the set, unique count is case insensitive
									pair.Add(word.Wordform.Form.get_String(ws).Text.ToLower());
								}
								else
								{
									//add the string for this writing system in all lower case to the set, unique count is case insensitive
									languageTypeCount.Add(ws, new Set<String> { word.Wordform.Form.get_String(ws).Text.ToLower() });
								}
							}
						}
					}
					words.RemoveAll(item => !item.HasWordform);
					wordCount += words.Count;
				}
			}
			segmentCount.Text = "" + numberOfSegments;
			statisticsTable.Controls.Add(totalWordTypeCountLabel, 0, row);
			statisticsTable.Controls.Add(totalWordTypeCount, 1, row);
			++row;
			//add one row for the unique words in each language.
			foreach (KeyValuePair<int, Set<String>> keyValuePair in languageTypeCount)
			{
				var ws = Cache.WritingSystemFactory.get_EngineOrNull(keyValuePair.Key);
				Label wsLabel = new Label
				{
					Font = font,
					AutoSize = true,
					Anchor = AnchorStyles.Right,
					Text = (ws != null ? ws.ToString() : "#unknown#") + @":",
					TextAlign = ContentAlignment.MiddleRight
				};
				Label wsCount = new Label
				{
					Font = font,
					Anchor = AnchorStyles.Left,
					Text = "" + keyValuePair.Value.Count,
					TextAlign = ContentAlignment.MiddleLeft
				};
				statisticsTable.Controls.Add(wsLabel, 0, row);
				statisticsTable.Controls.Add(wsCount, 1, row);
				++row;
				uniqueWords += keyValuePair.Value.Count; //increase the total of unique words
			}
			//add extra padding row
			statisticsTable.Controls.Add(new Label {Text = @"   "}, 0, row++);

			totalWordTypeCount.Text = "" + uniqueWords; //update the text of the total unique word label
			totalWordTokenCount.Text = "" + wordCount;
			statisticsTable.Controls.Add(totalWordTokenCountLabel, 0, row);
			statisticsTable.Controls.Add(totalWordTokenCount, 1, row);
			++row;
			//add one row for the token count for each language.
			foreach (KeyValuePair<int, int> keyValuePair in languageCount)
			{
				var ws = Cache.WritingSystemFactory.get_EngineOrNull(keyValuePair.Key);
				Label wsLabel = new Label
				{
					Font = font,
					AutoSize = true,
					Anchor = AnchorStyles.Right,
					Text = (ws != null ? ws.ToString() : "#unknown#") + @":",
					TextAlign = ContentAlignment.MiddleRight
				};
				Label wsCount = new Label
				{
					Font = font,
					Anchor = AnchorStyles.Left,
					Text = "" + keyValuePair.Value,
					TextAlign = ContentAlignment.MiddleLeft
				};
				statisticsTable.Controls.Add(wsLabel, 0, row);
				statisticsTable.Controls.Add(wsCount, 1, row);
				++row;
			}
			//add extra padding row between unique words and sentence(segment) count
			statisticsTable.Controls.Add(new Label { Text = @"   " }, 0, row++);

			statisticsTable.Controls.Add(segmentCountLabel, 0, row);
			statisticsTable.Controls.Add(segmentCount, 1, row);
			statisticsDescription.Refresh();
		}

		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			return new IxCoreColleague[] { this };
		}

		public bool ShouldNotCall
		{
			get { return _shouldNotCall; }
		}

		public bool OnDisplayAddTexts(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			display.Enabled = clerk != null;
			display.Visible = display.Enabled;
			return true;
		}

		public bool OnAddTexts(object args)
		{
			bool result = clerk.OnAddTexts(args);
			if(result)
			{
				RebuildStatisticsTable();
			}
			return result;
		}

		#endregion

		#region Implementation of IxCoreContentControl

		public bool PrepareToGoAway()
		{
			CheckDisposed();

			return true;
		}

		public string AreaName
		{
			get { return _areaName; }
		}

		#endregion

		#region Implementation of IFWDisposable

		/// <summary>
		/// This method throws an ObjectDisposedException if IsDisposed returns
		/// true.  This is the case where a method or property in an object is being
		/// used but the object itself is no longer valid.
		///
		/// This method should be added to all public properties and methods of this
		/// object and all other objects derived from it (extensive).
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException("StatisticsView has been disposed.");
		}

		#endregion

		/// <summary>
		/// FDO cache.
		/// </summary>
		protected FdoCache Cache
		{
			get
			{
				return mediator != null ? (FdoCache)mediator.PropertyTable.GetValue("cache") : null;
			}
		}
	}
}
