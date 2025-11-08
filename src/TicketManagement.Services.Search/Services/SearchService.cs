using Microsoft.EntityFrameworkCore;
using TicketManagement.Services.Events.Data;
using TicketManagement.Services.Search.DTOs;
using TicketManagement.Shared.Models;

namespace TicketManagement.Services.Search.Services;

public class SearchService : ISearchService
{
    private readonly EventsDbContext _eventsContext;
    private readonly ILogger<SearchService> _logger;

    public SearchService(EventsDbContext eventsContext, ILogger<SearchService> logger)
    {
        _eventsContext = eventsContext;
        _logger = logger;
    }

    public async Task<SearchResponse> SearchEventsAsync(SearchRequest request)
    {
        try
        {
            var query = _eventsContext.Events.AsQueryable();

            // Filter by status - only show events that are on sale or upcoming
            query = query.Where(e => e.Status == EventStatus.OnSale || e.Status == EventStatus.Upcoming);

            // Text search in event name and venue
            if (!string.IsNullOrWhiteSpace(request.Q))
            {
                var searchTerm = request.Q.ToLower();
                query = query.Where(e => 
                    e.EventName.ToLower().Contains(searchTerm) ||
                    (e.VenueName != null && e.VenueName.ToLower().Contains(searchTerm)));
            }

            // Filter by city (venue name contains city)
            if (!string.IsNullOrWhiteSpace(request.City))
            {
                var city = request.City.ToLower();
                query = query.Where(e => e.VenueName != null && e.VenueName.ToLower().Contains(city));
            }

            // Category filtering would be implemented when Event entity includes Category field
            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                // For now, filter by category name in event name or venue (basic implementation)
                var category = request.Category.ToLower();
                query = query.Where(e => 
                    e.EventName.ToLower().Contains(category) ||
                    (e.VenueName != null && e.VenueName.ToLower().Contains(category)));
            }

            // Filter by date range
            if (request.DateFrom.HasValue)
            {
                query = query.Where(e => e.EventDate >= request.DateFrom.Value);
            }

            if (request.DateTo.HasValue)
            {
                query = query.Where(e => e.EventDate <= request.DateTo.Value);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Pagination
            var page = request.Page ?? 0;
            var size = request.Size ?? 20;
            var skip = page * size;

            var events = await query
                .OrderBy(e => e.EventDate)
                .Skip(skip)
                .Take(size)
                .Select(e => new EventSummary
                {
                    EventId = e.EventId,
                    Name = e.EventName,
                    City = e.VenueName ?? "Unknown",
                    Venue = e.VenueName ?? "Unknown",
                    Category = "CONCERT", // Default category, can be extended
                    Date = e.EventDate,
                    MinPrice = 0, // Would need to query inventory service for actual min price
                    Availability = e.Status == EventStatus.OnSale ? "ON_SALE" : 
                                  e.Status == EventStatus.SoldOut ? "SOLD_OUT" : 
                                  e.Status == EventStatus.Cancelled ? "CANCELLED" : "UPCOMING"
                })
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / size);

            return new SearchResponse
            {
                Results = events,
                Page = new PageInfo
                {
                    Number = page,
                    Size = size,
                    TotalElements = totalCount,
                    TotalPages = totalPages
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching events");
            throw;
        }
    }
}

