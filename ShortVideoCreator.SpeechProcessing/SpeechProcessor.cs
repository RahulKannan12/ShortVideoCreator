using System.Text;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using ShortVideoCreator.SpeechProcessing.Captions;
using ShortVideoCreator.Storage;
using SpeechSynthesizer = Microsoft.CognitiveServices.Speech.SpeechSynthesizer;

namespace ShortVideoCreator.SpeechProcessing;

public class SpeechProcessor
{
    // NOTE : SpeechKey - required for speech synthesis and recognizing
    //TODO: To be received from a private vault
    private const string SpeechKey = "*******************************";
    private const string SpeechRegion = "eastus";
    private readonly List<SpeechRecognitionResult> _offlineResults = new();
    
    public async Task Process(string jobId = default!)
    {
        var processedAudioFile = new FileInfo(
            string.IsNullOrEmpty(jobId) ? LocalStorage.GetProcessedAudioFileName() : LocalStorage.GetProcessedAudioFileName(jobId));
        processedAudioFile.Directory?.Create(); 
        var fs = new FileStream(string.IsNullOrEmpty(jobId) ? LocalStorage.GetTextFileOutputPath() : LocalStorage.GetTextFileOutputPath(jobId), FileMode.OpenOrCreate, FileAccess.ReadWrite);
        string content = await new StreamReader(fs, Encoding.UTF8).ReadToEndAsync();
        
        
        var speechConfig = SpeechConfig.FromSubscription(SpeechKey, SpeechRegion);
        using var audioConfig = AudioConfig.FromWavFileOutput(Path.Combine(processedAudioFile.DirectoryName, processedAudioFile.Name));
        
        //TODO: Voice can be received as input, and passed to synthesizer. Many languages and Speech tones are possible.
        speechConfig.SpeechSynthesisVoiceName = "en-US-AvaMultilingualNeural";

        using var speechSynthesizer = new SpeechSynthesizer(speechConfig, audioConfig);
        await speechSynthesizer.SpeakTextAsync(content);
        
        GenerateSrtFileFromGeneratedAudioFile(Path.Combine(processedAudioFile.DirectoryName, processedAudioFile.Name), string.IsNullOrEmpty(jobId) ? LocalStorage.GetProcessedSubtitleFileName() : LocalStorage.GetProcessedSubtitleFileName(jobId));
    }

    private void GenerateSrtFileFromGeneratedAudioFile(string audioFilePath, string subtitleFilePath)
    {
        var speechConfig = SpeechConfig.FromSubscription(SpeechKey, SpeechRegion);
        var audioConfig = AudioConfig.FromWavFileInput(audioFilePath);
        var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);
        
        if (RecognizeContinuous(speechRecognizer).Result is { } error)
        {
            Console.WriteLine(error);
        }
        else
        {
            Finish(subtitleFilePath);
        }
    }
    
    private IEnumerable<Caption> CaptionsFromOfflineResults()
    {
        IEnumerable<Caption> captions = CaptionHelper.GetCaptions("en-US", 30 , 2, _offlineResults);

        // Save the last caption.
        Caption lastCaption = captions.Last();
        lastCaption.End.Add(new TimeSpan(1000));
        // In offline mode, all captions come from RecognitionResults of type Recognized.
        // Set the end timestamp for each caption to the earliest of:
        // - The end timestamp for this caption plus the remain time.
        // - The start timestamp for the next caption.            
        List<Caption> captions_2 = captions
            .Pairwise()
            .Select(captions =>
            {
                TimeSpan end = captions.Item1.End.Add(new TimeSpan(1000));
                captions.Item1.End = end < captions.Item2.Begin ? end : captions.Item2.Begin;
                return captions.Item1;
            })
            .ToList();
        // Re-add the last caption.
        captions_2.Add(lastCaption);
        return captions_2;
    }
    
    private void WriteToConsoleOrFile(string text, string outputFilePathValue)
    {
        Console.WriteLine(text);
        File.AppendAllText(outputFilePathValue, text );
    }
    
    private string StringFromCaption(Caption caption)
    {
        var retval = new StringBuilder();
        retval.AppendFormat($"{caption.Sequence}{Environment.NewLine}");
        retval.AppendFormat($"{GetTimestamp(caption.Begin, caption.End)}{Environment.NewLine}");
        retval.AppendFormat($"{caption.Text}{Environment.NewLine}{Environment.NewLine}");
        return retval.ToString();
    }
    
    private string GetTimestamp(TimeSpan startTime, TimeSpan endTime)
    {
        return $"{startTime:hh\\:mm\\:ss\\,fff} --> {endTime:hh\\:mm\\:ss\\,fff}";
    }
    
    private void Finish(string subtitleFilePath)
    {
        foreach (Caption caption in CaptionsFromOfflineResults())
        {
            WriteToConsoleOrFile(StringFromCaption(caption), subtitleFilePath);
        }
    }
     private async Task<string?> RecognizeContinuous(SpeechRecognizer speechRecognizer)
        {
            var stopRecognition = new TaskCompletionSource<string?>();

            // We use Recognized results in both offline and real-time modes.
            speechRecognizer.Recognized += (object? sender, SpeechRecognitionEventArgs e) =>
                {
                    if (ResultReason.RecognizedSpeech == e.Result.Reason && e.Result.Text.Length > 0)
                    {
                            _offlineResults.Add(e.Result);
                    }
                    else if (ResultReason.NoMatch == e.Result.Reason)
                    {
                        Console.WriteLine($"NOMATCH: Speech could not be recognized.{Environment.NewLine}");
                    }
                };

            speechRecognizer.Canceled += (object? sender, SpeechRecognitionCanceledEventArgs e) =>
                {
                    if (CancellationReason.EndOfStream == e.Reason)
                    {
                        Console.WriteLine($"End of stream reached.{Environment.NewLine}");
                        stopRecognition.TrySetResult(null); // Notify to stop recognition.
                    }
                    else if (CancellationReason.CancelledByUser == e.Reason)
                    {
                        Console.WriteLine($"User canceled request.{Environment.NewLine}");
                        stopRecognition.TrySetResult(null); // Notify to stop recognition.
                    }
                    else if (CancellationReason.Error == e.Reason)
                    {
                        var error = $"Encountered error.{Environment.NewLine}Error code: {(int)e.ErrorCode}{Environment.NewLine}Error details: {e.ErrorDetails}{Environment.NewLine}";
                        stopRecognition.TrySetResult(error); // Notify to stop recognition.
                    }
                    else
                    {
                        var error = $"Request was cancelled for an unrecognized reason: {(int)e.Reason}.{Environment.NewLine}";
                        stopRecognition.TrySetResult(error); // Notify to stop recognition.
                    }
                };

            // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
            await speechRecognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

            // Waits for completion.
            // Use Task.WaitAny to keep the task rooted.
            Task.WaitAny(new[] { stopRecognition.Task });

            // Stops recognition.
            await speechRecognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            
            return stopRecognition.Task.Result;
        }

}