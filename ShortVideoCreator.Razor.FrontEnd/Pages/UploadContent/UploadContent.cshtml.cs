using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShortVideoCreator.Razor.FrontEnd.Helpers;

namespace ShortVideoCreator.Razor.FrontEnd.Pages.UploadContent;

public class UploadContentModel : PageModel
{
    private readonly long _fileSizeLimit;
    private readonly string _targetFilePath;

    public UploadContentModel(IConfiguration config)
    {
        _fileSizeLimit = config.GetValue<long>("FileSizeLimit");
        
        _targetFilePath = config.GetValue<string>("ContentProcessingFilePath");
    }

    
    
    [BindProperty]
    public UploadContent UploadContent { get; set; }

    public string Result { get; private set; }
    
    public void OnGet()
    {
        
    }
    
    public async Task<IActionResult> OnPostUploadAsync()
    {
        if (!ModelState.IsValid)
        {
            Result = "Please correct the form.";
        
            return Page();
        }

        var formFileContent = 
            await FileHelpers.ProcessFormFile<UploadContent>(
                UploadContent.FormFiles, ModelState, 
                _fileSizeLimit);

        if (!ModelState.IsValid)
        {
            Result = "Please correct the form.";
        
            return Page();
        }


        var trustedFileNameForFileStorage = "content";
        var jobId = new Random().Next(0, 1000000).ToString("D6");
        var filePath = Path.Combine(
            _targetFilePath, jobId, trustedFileNameForFileStorage+Path.GetExtension(UploadContent.FormFiles.FileName));
        FileInfo file = new FileInfo(filePath);
        file.Directory?.Create();
        using (var fileStream = System.IO.File.Create(filePath))
        {
            await fileStream.WriteAsync(formFileContent);
            
        }

        return RedirectToPage("../HandleJob/HandleJob", new { jobId = jobId});
    }
}

public class UploadContent
{
    [Required]
    [Display(Name="File")]
    public IFormFile FormFiles { get; set; }
    

}