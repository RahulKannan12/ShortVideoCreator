using System.Text;
using ShortVideoCreator.Storage;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ShortVideoCreator.DocumentProcessing;

public class PdfDocumentProcessor
{
    public void Process(string jobId = default!)
    {
        Console.WriteLine(LocalStorage.GetPdfFileInputPath());
        using PdfDocument document = PdfDocument.Open(Path.Combine(string.IsNullOrEmpty(jobId) ? LocalStorage.GetPdfFileInputPath() : LocalStorage.GetPdfFileInputPath(jobId)));
        FileInfo file = new FileInfo(string.IsNullOrEmpty(jobId) ? LocalStorage.GetTextFileOutputPath() : LocalStorage.GetTextFileOutputPath(jobId));
        file.Directory?.Create();
        if (file.DirectoryName != null)
        {
            using StreamWriter sw = new StreamWriter(Path.Combine(file.DirectoryName, file.Name), Encoding.UTF8 , new FileStreamOptions { Mode = FileMode.OpenOrCreate , Access = FileAccess.ReadWrite});
            foreach (Page page in document.GetPages())
            {
                foreach (Word word in page.GetWords())
                {
                    sw.Write(word.Text);
                    sw.Write(" ");
                }
            }
        }
    }
}