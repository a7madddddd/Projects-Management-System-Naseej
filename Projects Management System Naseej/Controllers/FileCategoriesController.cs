using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Projects_Management_System_Naseej.DTOs.FileCategoryDTOs;
using Projects_Management_System_Naseej.Repositories;

namespace Projects_Management_System_Naseej.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileCategoriesController : ControllerBase
    {
        private readonly IFileCategoryRepository _fileCategoryRepository;

        public FileCategoriesController(IFileCategoryRepository fileCategoryRepository)
        {
            _fileCategoryRepository = fileCategoryRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FileCategoryDTO>>> GetAllCategories()
        {
            try
            {
                var categories = await _fileCategoryRepository.GetAllCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving file categories.");
            }
        }

        [HttpGet("{categoryId}")]
        public async Task<ActionResult<FileCategoryDTO>> GetCategoryById(int categoryId)
        {
            try
            {
                var category = await _fileCategoryRepository.GetCategoryByIdAsync(categoryId);
                if (category == null)
                {
                    return NotFound($"File category with ID {categoryId} not found.");
                }
                return Ok(category);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while retrieving the file category.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<FileCategoryDTO>> CreateCategory(CreateFileCategory categoryDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdCategory = await _fileCategoryRepository.CreateCategoryAsync(categoryDTO);
                return CreatedAtAction(nameof(GetCategoryById), new { categoryId = createdCategory.CategoryId }, createdCategory);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while creating the file category.");
            }
        }

        [HttpPut("{categoryId}")]
        public async Task<ActionResult<FileCategoryDTO>> UpdateCategory(int categoryId, UpdateFileCategory categoryDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updatedCategory = await _fileCategoryRepository.UpdateCategoryAsync(categoryId, categoryDTO);
                if (updatedCategory == null)
                {
                    return NotFound($"File category with ID {categoryId} not found.");
                }
                return Ok(updatedCategory);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while updating the file category.");
            }
        }

        [HttpDelete("{categoryId}")]
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            try
            {
                await _fileCategoryRepository.DeleteCategoryAsync(categoryId);
                return NoContent();
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while deleting the file category.");
            }
        }
    }
}
