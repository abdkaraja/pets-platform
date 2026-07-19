namespace PetPlatform.Application.DTOs;

public class ProductFilterDto
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public string? PetType { get; set; }
    public int? BrandId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortBy { get; set; } // "price_asc", "price_desc", "name", "newest"
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}
