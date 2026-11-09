using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Models;

namespace RaceDay.Services;

// =======================================================
// START: RESULT PROJECTION
// Purpose: Turns result queries into ResultDetailDto objects,
// including the number of finishers per event.
// =======================================================

public static class ResultProjection
{
    public static async Task<List<ResultDetailDto>> ToDtosAsync(
        RaceDayDbContext context,
        IQueryable<Result> query)
    {
        var results = await query
            .AsNoTracking()
            .Include(r => r.Enrolment).ThenInclude(e => e!.Event)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Category)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Participant)
            .ToListAsync();

        var eventIds = results
            .Where(r => r.Enrolment != null)
            .Select(r => r.Enrolment!.EventId)
            .Distinct()
            .ToList();

        var finisherCounts = await context.Results
            .AsNoTracking()
            .Where(r => r.ResultStatus == "Finished" &&
                        eventIds.Contains(r.Enrolment!.EventId))
            .GroupBy(r => r.Enrolment!.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count);

        var dtos = new List<ResultDetailDto>();

        foreach (var result in results)
        {
            var enrolment = result.Enrolment;

            if (enrolment == null)
                continue;

            finisherCounts.TryGetValue(enrolment.EventId, out var total);

            dtos.Add(new ResultDetailDto
            {
                ResultId = result.ResultId,
                EnrolmentId = result.EnrolmentId,
                EventId = enrolment.EventId,
                EventName = enrolment.Event?.EventName ?? string.Empty,
                EventDate = enrolment.Event?.EventDate ?? default,
                CategoryId = enrolment.CategoryId,
                CategoryName = enrolment.Category?.CategoryName ?? string.Empty,
                ParticipantId = enrolment.ParticipantId,
                ParticipantName = enrolment.Participant == null
                    ? string.Empty
                    : enrolment.Participant.FirstName + " " + enrolment.Participant.LastName,
                FinishTime = result.FinishTime,
                Position = result.Position,
                TotalFinishers = total,
                ResultStatus = result.ResultStatus,
                RecordedAt = result.RecordedAt
            });
        }

        return dtos;
    }
}

// =======================================================
// END: RESULT PROJECTION
// =======================================================
