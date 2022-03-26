using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services
{
    public interface IFileService
    {
        Task<byte[]> ProcessFormFileAsync(IFormFile formFile, ModelStateDictionary modelState, List<string> permittedExtensions, long fileSizeLimit, string modelName = "File");
        bool CheckFileExtension(string fileName, List<string> permittedExtensions);
    }
}