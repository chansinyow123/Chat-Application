using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Static
{
    public static class FileExtension
    {
        public static readonly List<string> Image = new List<string> { ".jpg", ".jpeg", ".png", ".gif" };
        public static readonly List<string> Audio = new List<string> { ".mp3", ".wav" };
        public static readonly List<string> Video = new List<string> { ".mp4" };
        public static readonly List<string> Document = new List<string>{ ".pdf", ".zip", ".txt", ".doc", ".docx" };
        public static readonly List<string> All = Image.Concat(Audio).Concat(Video).Concat(Document).ToList();
    }
}
