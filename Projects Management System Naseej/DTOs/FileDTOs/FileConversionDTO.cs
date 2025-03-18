namespace Projects_Management_System_Naseej.DTOs.FileDTOs
{
    public class FileConversionDTO
    {
        public int FileId { get; set; }
        public string OriginalExtension { get; set; }
        public string TargetExtension { get; set; }
        public string ConvertedFilePath { get; set; }
        public string FileName { get; set; } = null!;

    }
}
