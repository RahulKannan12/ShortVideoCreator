using ShortVideoCreator.DocumentProcessing;
using ShortVideoCreator.SpeechProcessing;
using ShortVideoCreator.Storage;
using ShortVideoCreator.VideoProcessing;

namespace ShortVideoCreator;

public class Program
{
    private static async Task Main(string[] args)
    {
        LocalStorage.CreateWorkspace();
        WarmUp();
        PdfDocumentProcessor pdfDocumentProcessor = new PdfDocumentProcessor();
        pdfDocumentProcessor.Process();
        SpeechProcessor speechProcessor = new SpeechProcessor();
        await speechProcessor.Process();
        VideoProcessor videoProcessor = new VideoProcessor();
        await videoProcessor.Process(true);
        //TODO: add option to clean other files, leaving video library and input folder
        //CleanUp();
    }

    private static void WarmUp()
    {
        //Create Storage folder 
        FileInfo basePath = new FileInfo(LocalStorage.BasePath);
        basePath.Directory?.Create();
        
        //Create video library folder
        FileInfo videoLibrary = new FileInfo(LocalStorage.GetVideoLibrary());
        videoLibrary.Directory?.Create();
        
        //Create pdf file (input) folder
        FileInfo inputFileFolder = new FileInfo(LocalStorage.GetPdfFileInputPath());
        inputFileFolder.Directory?.Create();
    }
    
    private static void CleanUp()
    {
        //Delete Storage folder 
        DirectoryInfo basePath = new DirectoryInfo(LocalStorage.BasePath);
        basePath.Delete(true);
    }
}