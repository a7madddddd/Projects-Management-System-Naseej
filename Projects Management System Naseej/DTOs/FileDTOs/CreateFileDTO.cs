namespace Projects_Management_System_Naseej.DTOs.FileDTOs
{
    public class CreateFileDTO
    {
        public string FileName { get; set; }
        public int CategoryId { get; set; }
        public bool IsPublic { get; set; } = false;
        public int? UserId { get; set; } // Add this field
    }
}
