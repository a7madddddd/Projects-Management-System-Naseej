namespace Projects_Management_System_Naseej.DTOs.FileDTOs
{
    public class FileDTO
    {
        public int FileId { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public int? CategoryId { get; set; }
        public int UploadedBy { get; set; }
        public DateTime UploadDate { get; set; }
        public int? LastModifiedBy { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsPublic { get; set; }
        public string? GoogleDriveFileId { get; set; }

        public bool? IsSyncedWithGoogleDrive { get; set; }
    }
}
