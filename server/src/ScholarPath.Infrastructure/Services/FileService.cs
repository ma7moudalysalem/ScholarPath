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
            // 1. التحقق من الامتدادات المسموحة (Security)
            var allowedExtensions = new[] { ".pdf", ".jpg", ".png", ".docx" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension)) throw new Exception("Invalid file type.");

            // 2. إنشاء المسار
            var targetPath = Path.Combine(_basePath, folderName);
            if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);

            // 3. توليد اسم فريد للملف لمنع التكرار
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(targetPath, uniqueFileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return Path.Combine("uploads", folderName, uniqueFileName);
        }

        public void DeleteFile(string filePath) { 
        
            if (string.IsNullOrEmpty(filePath)) return;

            // 1. تحويل المسار النسبي (المخزن في القاعدة) إلى مسار كامل على الجهاز
            // filePath عادة يكون: "uploads/upgrades/filename.jpg"
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath);

            // 2. التحقق من وجود الملف قبل محاولة حذفه لتجنب الأخطاء
            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                }
                catch (IOException ex)
                {
                    // هنا يمكنك تسجيل الخطأ (Logging) إذا كان الملف قيد الاستخدام مثلاً
                    throw new Exception("Error deleting file: " + ex.Message);
                }
            }
        }
    }
    }

