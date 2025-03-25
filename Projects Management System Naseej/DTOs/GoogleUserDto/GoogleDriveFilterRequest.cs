namespace Projects_Management_System_Naseej.DTOs.GoogleUserDto
{
    public class GoogleDriveFilterRequest
    {
        public string SearchQuery { get; set; }
        public string MimeType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public long? MinSize { get; set; }
        public long? MaxSize { get; set; }
        public int PageSize { get; set; } = 10;
        public int PageNumber { get; set; } = 1;
    }
}
