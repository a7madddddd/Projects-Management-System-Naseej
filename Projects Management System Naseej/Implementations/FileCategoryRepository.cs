using Microsoft.EntityFrameworkCore;
using Projects_Management_System_Naseej.DTOs.FileCategoryDTOs;
using Projects_Management_System_Naseej.Models;
using Projects_Management_System_Naseej.Repositories;

namespace Projects_Management_System_Naseej.Implementations
{
    public class FileCategoryRepository : IFileCategoryRepository
    {
        private readonly MyDbContext _context;

        public FileCategoryRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FileCategoryDTO>> GetAllCategoriesAsync()
        {
            return await _context.FileCategories
                .Select(c => new FileCategoryDTO
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    IsActive = c.IsActive ?? false
                })
                .ToListAsync();
        }

        public async Task<FileCategoryDTO> GetCategoryByIdAsync(int categoryId)
        {
            var category = await _context.FileCategories.FindAsync(categoryId);
            if (category == null) return null;

            return new FileCategoryDTO
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description,
                IsActive = category.IsActive ?? false
            };
        }

        public async Task<FileCategoryDTO> CreateCategoryAsync(CreateFileCategory categoryDTO)
        {
            var category = new FileCategory
            {
                CategoryName = categoryDTO.CategoryName,
                Description = categoryDTO.Description,
                IsActive = true
            };

            _context.FileCategories.Add(category);
            await _context.SaveChangesAsync();

            return new FileCategoryDTO
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description,
                IsActive = category.IsActive ?? false
            };
        }

        public async Task<FileCategoryDTO> UpdateCategoryAsync(int categoryId, UpdateFileCategory categoryDTO)
        {
            var category = await _context.FileCategories.FindAsync(categoryId);
            if (category == null) return null;

            category.CategoryName = categoryDTO.CategoryName ?? category.CategoryName;
            category.Description = categoryDTO.Description ?? category.Description;
            category.IsActive = categoryDTO.IsActive ?? category.IsActive;

            _context.FileCategories.Update(category);
            await _context.SaveChangesAsync();

            return new FileCategoryDTO
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description,
                IsActive = category.IsActive ?? false
            };
        }

        public async Task DeleteCategoryAsync(int categoryId)
        {
            var category = await _context.FileCategories.FindAsync(categoryId);
            if (category != null)
            {
                _context.FileCategories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }
    }
}