using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ScholarPath.Infrastructure.Services
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(IFormFile file, string folderName);
        void DeleteFile(string filePath);
    }

    public class LocalFileService : IFileService
    {
        private readonly string _basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            // Security
            var allowedExtensions = new[] { ".pdf", ".jpg", ".png", ".docx" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension)) throw new Exception("Invalid file type.");
            
            var targetPath = Path.Combine(_basePath, folderName);
            if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);

            // Generate a unique file name
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(targetPath, uniqueFileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return Path.Combine("uploads", folderName, uniqueFileName);
        }

        public void DeleteFile(string filePath) { 
        
            if (string.IsNullOrEmpty(filePath)) return;
            // "uploads/upgrades/filename.jpg"
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath);

            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                }
                catch (IOException ex)
                {
                    throw new Exception("Error deleting file: " + ex.Message);
                }
            }
        }
    }
    }

