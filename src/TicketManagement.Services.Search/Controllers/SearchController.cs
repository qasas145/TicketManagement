using Microsoft.AspNetCore.Mvc;
using TicketManagement.Services.Search.DTOs;
using TicketManagement.Services.Search.Services;

namespace TicketManagement.Services.Search.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<SearchResponse>> Search([FromQuery] SearchRequest request)
    {
        var result = await _searchService.SearchEventsAsync(request);
        return Ok(result);
    }
}

