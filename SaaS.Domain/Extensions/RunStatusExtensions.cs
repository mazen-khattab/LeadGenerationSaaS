using SaaS.Domain.Enums;
using System;

namespace SaaS.Domain.Extensions
{
    public static class RunStatusExtensions
    {
        public static string ToDbString(this RunStatus status) => status switch
        {
            RunStatus.RUNNING => "Running",
            RunStatus.PENDING => "Pending",
            RunStatus.COMPLETED => "Completed",
            RunStatus.FAILED => "Failed",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unmapped RunStatus value.")
        };

        public static RunStatus ParseFromDb(string dbValue) => dbValue switch
        {
            "Running" => RunStatus.RUNNING,
            "Pending" => RunStatus.PENDING,
            "Completed" => RunStatus.COMPLETED,
            "Failed" => RunStatus.FAILED,
            _ => throw new ArgumentOutOfRangeException(nameof(dbValue), dbValue, "Unrecognized RunStatus value from DB.")
        };
    }
}
