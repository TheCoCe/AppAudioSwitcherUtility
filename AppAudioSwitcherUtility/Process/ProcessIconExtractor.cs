using System;
using System.Drawing;

namespace AppAudioSwitcherUtility.Process
{
    public static class ProcessIconExtractor
    {
        private static Icon GetIconFromProcess(int pid)
        {
            string path = ProcessUtilities.GetExecutablePath(pid);
            if(string.IsNullOrEmpty(path)) return null;

            try
            {
                return Icon.ExtractAssociatedIcon(path);
            }
            catch
            {
                return null;
            }
        }

        public static string GetBase64IconFromProcess(int pid)
        {
            Icon icon = GetIconFromProcess(pid);
            if(icon == null) return null;
            
            Bitmap bitmap = icon.ToBitmap();
            
            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            
            byte[] byteImage = stream.ToArray();
            return Convert.ToBase64String(byteImage);
        }
    }
}