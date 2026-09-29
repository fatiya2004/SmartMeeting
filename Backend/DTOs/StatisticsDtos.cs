namespace SmartMeeting.Api.DTOs;

public class DashboardStatsDto
{
    public int MeetingsToday { get; set; }
    public int MeetingsThisWeek { get; set; }
    public int PendingRequests { get; set; }
    public int UrgentMeetings { get; set; }
    public int ActiveRooms { get; set; }
    public double RoomOccupancyRate { get; set; }
    public List<MeetingDto> UpcomingMeetings { get; set; } = new();
}

public class LabelValueDto
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
}

public class GlobalStatsDto
{
    public int TotalMeetings { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Pending { get; set; }
    public double EstimatedHoursSaved { get; set; }
    public List<LabelValueDto> MeetingsPerDay { get; set; } = new();
    public List<LabelValueDto> MeetingsPerPriority { get; set; } = new();
    public List<LabelValueDto> RoomOccupancy { get; set; } = new();
}
