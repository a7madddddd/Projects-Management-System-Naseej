namespace Projects_Management_System_Naseej.DTOs.GoogleUserDto
{
    public class GoogleDriveListRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public string SearchQuery { get; set; } = "";

    }
}
