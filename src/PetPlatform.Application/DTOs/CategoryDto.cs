namespace PetPlatform.Application.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int ProductCount { get; set; }
    public ICollection<CategoryDto> Children { get; set; } = new List<CategoryDto>();
}

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
}
