namespace ShortVideoCreator.Razor.FrontEnd.Helpers;

using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public static class FileHelpers
    {

        public static async Task<byte[]> ProcessFormFile<T>(IFormFile formFile, 
            ModelStateDictionary modelState, 
            long sizeLimit)
        {
            var fieldDisplayName = string.Empty;

            // Use reflection to obtain the display name for the model
            // property associated with this IFormFile. If a display
            // name isn't found, error messages simply won't show
            // a display name.
            MemberInfo property =
                typeof(T).GetProperty(
                    formFile.Name.Substring(formFile.Name.IndexOf(".",
                    StringComparison.Ordinal) + 1));

            if (property != null)
            {
                if (property.GetCustomAttribute(typeof(DisplayAttribute)) is
                    DisplayAttribute displayAttribute)
                {
                    fieldDisplayName = $"{displayAttribute.Name} ";
                }
            }

            // Don't trust the file name sent by the client. To display
            // the file name, HTML-encode the value.
            var trustedFileNameForDisplay = WebUtility.HtmlEncode(
                formFile.FileName);

            // Check the file length. This check doesn't catch files that only have 
            // a BOM as their content.
            if (formFile.Length == 0)
            {
                modelState.AddModelError(formFile.Name, 
                    $"{fieldDisplayName}({trustedFileNameForDisplay}) is empty.");

                return Array.Empty<byte>();
            }
            
            if (formFile.Length > sizeLimit)
            {
                var megabyteSizeLimit = sizeLimit / 1048576;
                modelState.AddModelError(formFile.Name,
                    $"{fieldDisplayName}({trustedFileNameForDisplay}) exceeds " +
                    $"{megabyteSizeLimit:N1} MB.");

                return Array.Empty<byte>();
            }

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await formFile.CopyToAsync(memoryStream);
                    
                    if (memoryStream.Length == 0)
                    {
                        modelState.AddModelError(formFile.Name,
                            $"{fieldDisplayName}({trustedFileNameForDisplay}) is empty.");
                    }

                    if (!IsValidFileExtensionAndSignature())
                    {
                        modelState.AddModelError(formFile.Name,
                            $"{fieldDisplayName}({trustedFileNameForDisplay}) file " +
                            "type isn't permitted or the file's signature " +
                            "doesn't match the file's extension.");
                    }
                    else
                    {
                        return memoryStream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                modelState.AddModelError(formFile.Name,
                    $"{fieldDisplayName}({trustedFileNameForDisplay}) upload failed. ");
                // Log the exception
            }

            return Array.Empty<byte>();
        }

        private static bool IsValidFileExtensionAndSignature()
        {
            //TODO: to implement file extensions and signature checks
            return true;
        }
    }