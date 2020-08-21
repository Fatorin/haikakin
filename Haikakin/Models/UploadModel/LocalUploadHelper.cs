using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Haikakin.Models.UploadModel
{
    public static class LocalUploadHelper
    {
        public static async Task<LocalUploadResponse> ImageUpload(IFormFile file, string imageUrl)
        {
            var response = new LocalUploadResponse();
            var folderPathPrefix = $"/var/www";
            var folderPath = $"/UploadImages/";

            //沒資料夾就不產生
            if (!Directory.Exists($"{folderPathPrefix}{folderPath}"))
            {
                response.Result = false;
                return response;
            }

            string fileFullPath;
            if (!string.IsNullOrEmpty(imageUrl) && File.Exists($"{ folderPathPrefix }{imageUrl}"))
            {
                //資料庫裡面有就保留原檔名
                fileFullPath = $"{folderPathPrefix}{imageUrl}";
                response.SaveUrl = imageUrl;
            }
            else
            {
                var filePath = Path.GetRandomFileName().Replace(".", "");
                var fileExt = Path.GetExtension(file.FileName);
                fileFullPath = $"{folderPathPrefix}{folderPath}{filePath}{fileExt}";
                //新增時檔名重複檢查
                while (File.Exists(fileFullPath))
                {
                    filePath = $"{filePath}{new Random().Next(100)}";
                    fileFullPath = $"{folderPathPrefix}{folderPath}{filePath}{fileExt}";
                }
                response.SaveUrl = $"{folderPath}{filePath}{fileExt}";
            }

            using (var stream = File.Create($"{fileFullPath}"))
            {
                await file.CopyToAsync(stream).ConfigureAwait(true);
            }

            response.Result = true;
            return response;
        }
    }

    public class LocalUploadResponse
    {
        public string SaveUrl { get; set; }
        public bool Result { get; set; }
    }
}
