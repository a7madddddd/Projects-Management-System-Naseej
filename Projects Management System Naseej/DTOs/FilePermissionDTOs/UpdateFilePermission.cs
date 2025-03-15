namespace Projects_Management_System_Naseej.DTOs.FilePermissionDTOs
{
    public class UpdateFilePermission
    {
        public bool? CanView { get; set; }
        public bool? CanEdit { get; set; }
        public bool? CanUpload { get; set; }
        public bool? CanDownload { get; set; }
        public bool? CanDelete { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
