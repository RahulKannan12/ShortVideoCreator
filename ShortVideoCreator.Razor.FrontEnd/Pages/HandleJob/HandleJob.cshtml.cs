using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShortVideoCreator.DocumentProcessing;
using ShortVideoCreator.SpeechProcessing;
using ShortVideoCreator.Storage;
using ShortVideoCreator.VideoProcessing;

namespace ShortVideoCreator.Razor.FrontEnd.Pages.HandleJob;

public class HandleJob : PageModel
{
    [BindProperty]
    public string JobId { get; set; }
    
    private readonly string _targetFilePath;

    public HandleJob(IConfiguration config)
    {
        _targetFilePath = config.GetValue<string>("ContentProcessingFilePath");
    }

    
    
    public void OnGet(string jobId)
    {
        JobId = jobId;
    }
    
    public async Task<IActionResult> OnPostUploadAsync()
    {
        LocalStorage.BasePath = _targetFilePath;
        PdfDocumentProcessor pdfDocumentProcessor = new PdfDocumentProcessor();
        pdfDocumentProcessor.Process(JobId);
        SpeechProcessor speechProcessor = new SpeechProcessor();
        await speechProcessor.Process(JobId);
        VideoProcessor videoProcessor = new VideoProcessor();
        await videoProcessor.Process(true, JobId);
        var filePath = Path.Combine(
            _targetFilePath, LocalStorage.GetProcessedVideoFileName(JobId));
        byte[] bytes = await System.IO.File.ReadAllBytesAsync(filePath);  
        //Send the File to Download.  
        return File(bytes, "application/octet-stream", "output.mp4");  
    }
}