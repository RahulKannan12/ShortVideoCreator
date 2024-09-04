// NOTE : This Code is sourced from scenarios/csharp/dotnetcore/captioning/captioning/CaptionHelper.cs
// Made few minor modifications to suit the requirement


using Microsoft.CognitiveServices.Speech;

namespace ShortVideoCreator.SpeechProcessing.Captions
{
    
    public struct Caption
    {
        public string? Language;
        public int Sequence;
        public TimeSpan Begin;
        public TimeSpan End;
        public string Text;
    }

    public class CaptionHelper
    {
        private readonly static int defaultMaxLineLengthSBCS = 37;
        private readonly static int defaultMaxLineLengthMBCS = 30;
        
        public static IEnumerable<Caption> GetCaptions(string? language, int maxWidth, int maxHeight, IEnumerable<object> results)
        {
            var helper = new CaptionHelper(language, maxWidth, maxHeight, results);
            return helper.GetCaptions();
        }

        public CaptionHelper(string? language, int maxWidth, int maxHeight, IEnumerable<object> results)
        {
            this._language = language;

            this._maxWidth = maxWidth;
            this._maxHeight = maxHeight;
            this._results = results;

            // consider adapting to use http://unicode.org/reports/tr29/#Sentence_Boundaries
            var iso639 = _language?.Split('-').FirstOrDefault();
            this._firstPassTerminators = iso639 switch
            {
                "zh" => new string[]{"，", "、", "；", "？", "！", "?", "!", ",", ";"},
                _ => new string[]{"?", "!", ",", ";"}
            };
            this._secondPassTerminators = iso639 switch
            {
                "zh" => new string[]{"。", " "},
                _ => new string[]{" ", "."}
            };

            if (maxWidth == defaultMaxLineLengthSBCS && iso639 == "zh")
            {
                this._maxWidth = defaultMaxLineLengthMBCS;
            }
        }

        public IEnumerable<Caption> GetCaptions()
        {
            EnsureCaptions();
            return _captions!;
        }

        private void EnsureCaptions()
        {
            if (_captions == null)
            {
                _captions = new List<Caption>();
                AddCaptionsForAllResults();
            }
        }

        private void AddCaptionsForAllResults()
        {
            foreach (RecognitionResult result in _results)
            {
                if (result.OffsetInTicks <= 0 || !IsFinalResult(result)) continue;

                var text = GetTextOrTranslation(result);
                if (string.IsNullOrEmpty(text)) continue;

                AddCaptionsForFinalResult(result, text);
            }
        }

        private string? GetTextOrTranslation(RecognitionResult result)
        {
            return result.Text;
        }

        private void AddCaptionsForFinalResult(RecognitionResult result, string text)
        {
            var captionStartsAt = 0;
            var captionLines = new List<string>();

            var index = 0;
            while (index < text.Length)
            {
                index = SkipSkippable(text, index);

                int lineLength = GetBestWidth(text, index);
                captionLines.Add(text.Substring(index, lineLength).Trim());
                index = index + lineLength;

                var isLastCaption = index >= text.Length;
                var maxCaptionLines = captionLines.Count >= _maxHeight;

                var addCaption = isLastCaption || maxCaptionLines;
                if (addCaption)
                {
                    var captionText = string.Join('\n', captionLines.ToArray());
                    captionLines.Clear();

                    var captionSequence = _captions!.Count + 1;
                    var isFirstCaption = captionStartsAt == 0;

                    (var captionBegin, var captionEnd) = isFirstCaption && isLastCaption
                        ? GetFullResultCaptionTiming(result)
                        : GetPartialResultCaptionTiming(result, text, captionText, captionStartsAt, index - captionStartsAt);

                    var caption = new Caption()
                    {
                        Language = _language,
                        Sequence = captionSequence,
                        Begin = captionBegin,
                        End = captionEnd,
                        Text = captionText
                    };
                    _captions.Add(caption);
                    
                    captionStartsAt = index;
                }
            }
        }

        private int GetBestWidth(string text, int startIndex)
        {
            var remaining = text.Length - startIndex;
            var bestWidth = remaining < _maxWidth
                ? remaining
                : FindBestWidth(_firstPassTerminators, text, startIndex);

            if (bestWidth < 0)
            {
                bestWidth = FindBestWidth(_secondPassTerminators, text, startIndex);
            }

            if (bestWidth < 0)
            {
                bestWidth = _maxWidth;
            }

            return bestWidth;
        }

        private int FindBestWidth(IEnumerable<string> terminators, string text, int startAt)
        {
            var remaining = text.Length - startAt;
            var checkChars = Math.Min(remaining, _maxWidth);

            var bestWidth = -1;
            foreach (var terminator in terminators)
            {
                var index = text.LastIndexOf(terminator, startAt + checkChars, checkChars);
                var width = index - startAt;
                if (width > bestWidth)
                {
                    bestWidth = width + terminator.Length;
                }
            }

            return bestWidth;
        }

        private int SkipSkippable(string text, int startIndex)
        {
            var index = startIndex;
            while (text.Length > index && text[index] == ' ')
                index++;

            return index;
        }

        private (TimeSpan captionBegin, TimeSpan captionEnd) GetFullResultCaptionTiming(RecognitionResult result)
        {
            var resultBegin = TimeSpan.FromTicks(result.OffsetInTicks);
            var resultEnd = resultBegin + result.Duration;
            return (resultBegin, resultEnd);
        }

        private (TimeSpan captionBegin, TimeSpan captionEnd) GetPartialResultCaptionTiming(RecognitionResult result, string text, string captionText, int captionStartsAt, int captionLength)
        {
            (var resultBegin, var resultEnd) = GetFullResultCaptionTiming(result);
            var resultDuration = resultEnd.Subtract(resultBegin);

            var textLength = text.Length;
            var partialBegin = resultBegin + resultDuration * captionStartsAt / textLength;
            var partialEnd = resultBegin + resultDuration * (captionStartsAt + captionLength) / textLength;

            return (partialBegin, partialEnd);
        }

        private static bool IsFinalResult(object result)
        {
            RecognitionResult? final = result as RecognitionResult;
            return final?.Reason == ResultReason.RecognizedSpeech ||
                   final?.Reason == ResultReason.RecognizedIntent ||
                   final?.Reason == ResultReason.TranslatedSpeech;
        }

        private readonly string? _language;
        private readonly string[] _firstPassTerminators;
        private readonly string[] _secondPassTerminators;

        private readonly int _maxWidth;
        private readonly int _maxHeight;
        private readonly IEnumerable<object> _results;

        private List<Caption>? _captions;
    }
    
    public static class LinqHelper
    {
        // Transform list {1,2,3,4} into {(1,2),(2,3),(3,4)}.
        public static IEnumerable<(T, T)> Pairwise<T>(this IEnumerable<T> xs)
        {
            return xs.Zip(xs.Skip(1), (a, b) => (a, b));
        }
    }
}