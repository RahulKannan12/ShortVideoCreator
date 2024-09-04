using FFMpegCore;
using ShortVideoCreator.Storage;

namespace ShortVideoCreator.VideoProcessing;

public class VideoProcessor
{
   
    public async Task Process(bool contentFromRandomVideoFromLibrary, string jobId = default!)
    {
        //TODO: to add the feature to get the video from the user and pass the name here
        string inputFile;
        inputFile = contentFromRandomVideoFromLibrary ? GetRandomVideoFromLibrary() : GetVideoFromJobFolder(jobId);
        
        // Comparing timespan of audio file and video file to match the content
        //TODO: get the option from user to loop video or trim the video
        var processedAudioFileName = string.IsNullOrEmpty(jobId)
            ? LocalStorage.GetProcessedAudioFileName()
            : LocalStorage.GetProcessedAudioFileName(jobId);
        TimeSpan audioDuration = GetFileDuration(processedAudioFileName);
        TimeSpan videoDuration = GetFileDuration(inputFile);

        if (videoDuration < audioDuration)
        {
            Console.WriteLine("Audio file (content) has more duration, so video will be looped to match the audio duration.");
        }

        var processedVideoFileName = string.IsNullOrEmpty(jobId)
            ? LocalStorage.GetProcessedVideoFileName()
            : LocalStorage.GetProcessedVideoFileName(jobId);
        var processedSubtitleFileName = string.IsNullOrEmpty(jobId)
            ? LocalStorage.GetProcessedSubtitleFileName()
            : LocalStorage.GetProcessedSubtitleFileName(jobId);
        
        //TODO: FFMpeg has many customization options of different font name, alignment, font colour and many more. We can get the options as input and can be feed in as command. 
        if (File.Exists(inputFile))
        {
            FileInfo file = new FileInfo(inputFile);
            await FFMpegArguments.FromFileInput(file, inputOptions =>
            {
                inputOptions.WithCustomArgument("-stream_loop -1");
            }).AddFileInput(processedAudioFileName).OutputToFile(processedVideoFileName, true, options =>
            {
                options.WithCustomArgument(string.Format("-vf \"subtitles={0}:force_style='FontName=Calibri,Alignment=2,OutlineColour=&H00000000,PrimaryColour=&H000000,SecondaryColour=&H00FFFF,BackColour=&H99000000,BorderStyle=0,Outline=1,Shadow=0,Fontsize=24\"", processedSubtitleFileName));
                options.WithCustomArgument("-shortest");
            }).ProcessAsynchronously();
        }
    }

    //TODO: only mp4 files are considered for input, need to extend for other video formats.
    private string GetRandomVideoFromLibrary()
    {
        var files = Directory.GetFiles(LocalStorage.GetVideoLibrary(), "*.mp4");
        if (files.Length == 0)
        {
            throw new FileNotFoundException("No .mp4 files found in the directory.");
        }
        var random = new Random();
        int index = random.Next(files.Length);
        return files[index];
    }
    
    //TODO: partially implemented the logic of getting .mp4 file from the job folder
    private string GetVideoFromJobFolder(string jobId)
    {
        var files = Directory.GetFiles(LocalStorage.GetVideoLibrary(jobId), "*.mp4");
        if (files.Length == 0)
        {
            throw new FileNotFoundException("No .mp4 files found in the directory.");
        }
        return files[0];
    }

    private static TimeSpan GetFileDuration(string inputFilePath)
    { 
        var fileInformation = FFProbe.Analyse(inputFilePath);
        return fileInformation.Duration;
    }
}
