using System.Reflection;

namespace ShortVideoCreator.Storage;

public static class LocalStorage
{
    //private static readonly string? BasePath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/storage/";
    
    public static string BasePath { get; set; }
    
    private static string WorkPlace { get; set; }
    
    public static void CreateWorkspace()
    {
        BasePath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "/storage/";
        WorkPlace = BasePath + new Random().Next(0, 1000000).ToString("D6") + "/";
    }

    public static string GetPdfFileInputPath()
    {
        return BasePath + "pdf-files/sample-content.pdf";
    }
    
    public static string GetPdfFileInputPath(string jobId)
    {
        return Path.Combine(BasePath , jobId , "content.pdf");
    }
    
    public static string GetVideoLibrary()
    {
        return BasePath + "/video-library/";
    }
    
    public static string GetVideoLibrary(string jobId)
    {
        return Path.Combine(BasePath , jobId , "video.mp4");
    }
    
    public static string GetTextFileOutputPath()
    {
        return WorkPlace + "content.txt";
    }

    public static string GetTextFileOutputPath(string jobId)
    {
        return Path.Combine(BasePath, jobId , "content.txt");
    }
    
    public static string GetProcessedAudioFileName()
    {
        return WorkPlace + "processed_audio.wav";
    }
    
    public static string GetProcessedAudioFileName(string jobId)
    {
        return Path.Combine(BasePath , jobId , "processed_audio.wav");
    }

    public static string GetProcessedVideoFileName()
    {
        return WorkPlace + "processed_video.mp4";
    }
    public static string GetProcessedVideoFileName(string jobId)
    {
        return Path.Combine(BasePath , jobId , "processed_video.mp4");
    }

    public static string GetProcessedSubtitleFileName()
    {
        return WorkPlace + "subtitle.srt";
    }
    public static string GetProcessedSubtitleFileName(string jobId)
    {
        return Path.Combine(BasePath , jobId ,  "subtitle.srt");
    }
}   