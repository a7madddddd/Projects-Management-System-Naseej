namespace Projects_Management_System_Naseej.Repositories
{
    public interface IFileTypeHandler
    {
        Task<bool> CanHandleAsync(Stream fileStream, string fileName);
        Task<byte[]> ConvertToAsync(Stream fileStream, string targetExtension);

    }
}
