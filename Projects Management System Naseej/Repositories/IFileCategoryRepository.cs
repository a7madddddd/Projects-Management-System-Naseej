using Projects_Management_System_Naseej.DTOs.FileCategoryDTOs;
using Projects_Management_System_Naseej.Models;

namespace Projects_Management_System_Naseej.Repositories
{
    public interface IFileCategoryRepository
    {
        Task<IEnumerable<FileCategoryDTO>> GetAllCategoriesAsync();
        Task<FileCategoryDTO> GetCategoryByIdAsync(int categoryId);
        Task<FileCategoryDTO> CreateCategoryAsync(CreateFileCategory categoryDTO);
        Task<FileCategoryDTO> UpdateCategoryAsync(int categoryId, UpdateFileCategory categoryDTO);
        Task DeleteCategoryAsync(int categoryId);
    }
}
