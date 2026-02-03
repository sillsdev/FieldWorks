// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SIL.FieldWorks.Common.RootSites.RenderBenchmark
{
	/// <summary>
	/// Parses trace log files to extract rendering stage durations.
	/// Expects trace entries in the format: [RENDER] Stage=StageName Duration=123.45ms Context=optional
	/// </summary>
	public class RenderTraceParser
	{
		/// <summary>
		/// The regex pattern for parsing render trace entries.
		/// Format: [RENDER] Stage=StageName Duration=123.45ms [Context=value]
		/// </summary>
		private static readonly Regex TraceEntryPattern = new Regex(
			@"\[RENDER\]\s+Stage=(?<stage>\w+)\s+Duration=(?<duration>[\d.]+)ms(?:\s+Context=(?<context>.+))?",
			RegexOptions.Compiled | RegexOptions.IgnoreCase);

		/// <summary>
		/// The regex pattern for parsing timestamped trace entries.
		/// Format: [2026-01-22T12:34:56.789] [RENDER] Stage=...
		/// </summary>
		private static readonly Regex TimestampedEntryPattern = new Regex(
			@"\[(?<timestamp>[\d\-T:.]+)\]\s+\[RENDER\]\s+Stage=(?<stage>\w+)\s+Duration=(?<duration>[\d.]+)ms(?:\s+Context=(?<context>.+))?",
			RegexOptions.Compiled | RegexOptions.IgnoreCase);

		/// <summary>
		/// Known rendering stages in order of expected execution.
		/// </summary>
		public static readonly string[] KnownStages = new[]
		{
			"MakeRoot",
			"Layout",
			"PrepareToDraw",
			"DrawRoot",
			"PropChanged",
			"LazyExpand",
			"Reconstruct"
		};

		/// <summary>
		/// Parses trace entries from a log file.
		/// </summary>
		/// <param name="logFilePath">Path to the trace log file.</param>
		/// <returns>A list of parsed trace events.</returns>
		public List<TraceEvent> ParseFile(string logFilePath)
		{
			if (!File.Exists(logFilePath))
				return new List<TraceEvent>();

			var lines = File.ReadAllLines(logFilePath);
			return ParseLines(lines);
		}

		/// <summary>
		/// Parses trace entries from log content.
		/// </summary>
		/// <param name="logContent">The log content string.</param>
		/// <returns>A list of parsed trace events.</returns>
		public List<TraceEvent> ParseContent(string logContent)
		{
			if (string.IsNullOrEmpty(logContent))
				return new List<TraceEvent>();

			var lines = logContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			return ParseLines(lines);
		}

		/// <summary>
		/// Parses trace entries from an array of log lines.
		/// </summary>
		/// <param name="lines">The log lines.</param>
		/// <returns>A list of parsed trace events.</returns>
		public List<TraceEvent> ParseLines(string[] lines)
		{
			var events = new List<TraceEvent>();
			double cumulativeTime = 0;

			foreach (var line in lines)
			{
				var evt = ParseLine(line, ref cumulativeTime);
				if (evt != null)
				{
					events.Add(evt);
				}
			}

			return events;
		}

		/// <summary>
		/// Parses a single trace log line.
		/// </summary>
		/// <param name="line">The log line.</param>
		/// <param name="cumulativeTime">Running cumulative time for calculating start times.</param>
		/// <returns>The parsed trace event, or null if line doesn't match.</returns>
		public TraceEvent ParseLine(string line, ref double cumulativeTime)
		{
			if (string.IsNullOrWhiteSpace(line))
				return null;

			// Try timestamped pattern first
			var match = TimestampedEntryPattern.Match(line);
			if (!match.Success)
			{
				// Fall back to simple pattern
				match = TraceEntryPattern.Match(line);
			}

			if (!match.Success)
				return null;

			var stage = match.Groups["stage"].Value;
			if (!double.TryParse(match.Groups["duration"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double duration))
				return null;

			var evt = new TraceEvent
			{
				Stage = stage,
				StartTimeMs = cumulativeTime,
				DurationMs = duration
			};

			// Parse context if present
			if (match.Groups["context"].Success && !string.IsNullOrWhiteSpace(match.Groups["context"].Value))
			{
				evt.Context = ParseContext(match.Groups["context"].Value);
			}

			cumulativeTime += duration;
			return evt;
		}

		/// <summary>
		/// Aggregates trace events by stage.
		/// </summary>
		/// <param name="events">The trace events to aggregate.</param>
		/// <returns>A dictionary of stage name to aggregated statistics.</returns>
		public Dictionary<string, StageStatistics> AggregateByStage(IEnumerable<TraceEvent> events)
		{
			var stats = new Dictionary<string, StageStatistics>(StringComparer.OrdinalIgnoreCase);

			foreach (var evt in events)
			{
				if (!stats.TryGetValue(evt.Stage, out var stageStat))
				{
					stageStat = new StageStatistics { Stage = evt.Stage };
					stats[evt.Stage] = stageStat;
				}

				stageStat.Count++;
				stageStat.TotalDurationMs += evt.DurationMs;
				stageStat.MinDurationMs = Math.Min(stageStat.MinDurationMs, evt.DurationMs);
				stageStat.MaxDurationMs = Math.Max(stageStat.MaxDurationMs, evt.DurationMs);
			}

			// Calculate averages
			foreach (var stat in stats.Values)
			{
				stat.AverageDurationMs = stat.Count > 0 ? stat.TotalDurationMs / stat.Count : 0;
			}

			return stats;
		}

		/// <summary>
		/// Gets the top time contributors from trace events.
		/// </summary>
		/// <param name="events">The trace events.</param>
		/// <param name="count">Number of top contributors to return.</param>
		/// <returns>A list of contributors sorted by share percentage.</returns>
		public List<Contributor> GetTopContributors(IEnumerable<TraceEvent> events, int count = 5)
		{
			var eventList = events.ToList();
			var totalTime = eventList.Sum(e => e.DurationMs);

			if (totalTime <= 0)
				return new List<Contributor>();

			var stats = AggregateByStage(eventList);

			return stats.Values
				.OrderByDescending(s => s.TotalDurationMs)
				.Take(count)
				.Select(s => new Contributor
				{
					Stage = s.Stage,
					AverageDurationMs = s.AverageDurationMs,
					SharePercent = s.TotalDurationMs / totalTime * 100
				})
				.ToList();
		}

		/// <summary>
		/// Validates that all expected trace stages are present.
		/// </summary>
		/// <param name="events">The trace events to validate.</param>
		/// <param name="requiredStages">The required stages (defaults to KnownStages).</param>
		/// <returns>A list of missing stage names.</returns>
		public List<string> ValidateStages(IEnumerable<TraceEvent> events, string[] requiredStages = null)
		{
			requiredStages = requiredStages ?? new[] { "Layout", "DrawRoot" }; // Minimum required
			var presentStages = new HashSet<string>(events.Select(e => e.Stage), StringComparer.OrdinalIgnoreCase);

			return requiredStages.Where(s => !presentStages.Contains(s)).ToList();
		}

		private Dictionary<string, string> ParseContext(string contextString)
		{
			var context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			// Parse key=value pairs separated by spaces or semicolons
			var pairs = contextString.Split(new[] { ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var pair in pairs)
			{
				var parts = pair.Split(new[] { '=' }, 2);
				if (parts.Length == 2)
				{
					context[parts[0].Trim()] = parts[1].Trim();
				}
			}

			return context;
		}
	}

	/// <summary>
	/// Contains aggregated statistics for a rendering stage.
	/// </summary>
	public class StageStatistics
	{
		/// <summary>Gets or sets the stage name.</summary>
		public string Stage { get; set; }

		/// <summary>Gets or sets the number of occurrences.</summary>
		public int Count { get; set; }

		/// <summary>Gets or sets the total duration across all occurrences.</summary>
		public double TotalDurationMs { get; set; }

		/// <summary>Gets or sets the average duration.</summary>
		public double AverageDurationMs { get; set; }

		/// <summary>Gets or sets the minimum duration.</summary>
		public double MinDurationMs { get; set; } = double.MaxValue;

		/// <summary>Gets or sets the maximum duration.</summary>
		public double MaxDurationMs { get; set; } = double.MinValue;
	}
}
