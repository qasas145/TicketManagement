using TicketManagement.Services.Search.DTOs;

namespace TicketManagement.Services.Search.Services;

public interface ISearchService
{
    Task<SearchResponse> SearchEventsAsync(SearchRequest request);
}

