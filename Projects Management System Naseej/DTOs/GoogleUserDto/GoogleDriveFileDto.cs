﻿namespace Projects_Management_System_Naseej.DTOs.GoogleUserDto
{
    public class GoogleDriveFileDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string MimeType { get; set; }
        public DateTime? CreatedTime { get; set; }
        public long? Size { get; set; }
        public string WebViewLink { get; set; }
        public List<OwnerDto> Owners { get; set; }

        public int UploadedBy { get; set; }
        public string UploadedByName { get; set; } // Add this
        public string FileExtension { get; set; }
    }
}
