using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShortVideoCreator.Razor.FrontEnd.Helpers;
using ShortVideoCreator.Storage;

namespace ShortVideoCreator.Razor.FrontEnd.Pages.VideoUpload;

public class UploadVideoModel : PageModel
{
    
    private readonly long _fileSizeLimit;
    private readonly string _targetFilePath;

    public UploadVideoModel(IConfiguration config)
    {
        _fileSizeLimit = config.GetValue<long>("FileSizeLimit");

        // To save physical files to a path provided by configuration:
       _targetFilePath = config.GetValue<string>("VideoLibraryFilePath");

        // To save physical files to the temporary files folder, use:
        //_targetFilePath = Path.GetTempPath();
    }
    
    
    [BindProperty]
    public UploadVideo FileUpload { get; set; }

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
            await FileHelpers.ProcessFormFile<UploadVideo>(
                FileUpload.FormFiles, ModelState, 
                _fileSizeLimit);

        if (!ModelState.IsValid)
        {
            Result = "Please correct the form.";
        
            return Page();
        }

        // For the file name of the uploaded file stored
        // server-side, use Path.GetRandomFileName to generate a safe
        // random file name.
        var trustedFileNameForFileStorage = Path.GetRandomFileName();
        var filePath = Path.Combine(
            _targetFilePath, trustedFileNameForFileStorage+"."+Path.GetExtension(FileUpload.FormFiles.FileName));
        
        using (var fileStream = System.IO.File.Create(filePath))
        {
            await fileStream.WriteAsync(formFileContent);

            // To work directly with a FormFile, use the following
            // instead:
            //await FileUpload.FormFile.CopyToAsync(fileStream);
        }

        TempData[Constants.AlertSuccess] = "Upload success";
        return RedirectToPage("../Index");
    }
}

public class UploadVideo
{
    [Required]
    [Display(Name="File")]
    public IFormFile FormFiles { get; set; }
}