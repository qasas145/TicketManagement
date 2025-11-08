namespace TicketManagement.Services.Search.DTOs;

public class SearchRequest
{
    public string? Q { get; set; }
    public string? City { get; set; }
    public string? Category { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? Page { get; set; }
    public int? Size { get; set; }
}

public class SearchResponse
{
    public List<EventSummary> Results { get; set; } = new();
    public PageInfo Page { get; set; } = new();
}

public class EventSummary
{
    public long EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal MinPrice { get; set; }
    public string Availability { get; set; } = string.Empty;
}

public class PageInfo
{
    public int Number { get; set; }
    public int Size { get; set; }
    public long TotalElements { get; set; }
    public int TotalPages { get; set; }
}

