using SaaS.Domain.Enums;
using System;

namespace SaaS.Domain.Extensions
{
    public static class LeadStatusExtensions
    {
        public static string ToDbString(this LeadStatus status) => status switch
        {
            LeadStatus.PENDING => "Pending",
            LeadStatus.COMPLETED => "Completed",
            LeadStatus.FAILED => "Failed",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unmapped LeadStatus value.")
        };

        public static LeadStatus ParseFromDb(string dbValue) => dbValue switch
        {
            "Pending" => LeadStatus.PENDING,
            "Completed" => LeadStatus.COMPLETED,
            "Failed" => LeadStatus.FAILED,
            _ => throw new ArgumentOutOfRangeException(nameof(dbValue), dbValue, "Unrecognized LeadStatus value from DB.")
        };
    }
}
