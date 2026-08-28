using SaaS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Domain.Extensions
{
    /// <summary>
    /// Central place mapping JobStatus -> the exact string stored in the DB.
    /// Do NOT use JobStatus.SomeValue.ToString() directly anywhere else in the codebase -
    /// enum member casing (e.g. PROCESSING) does not necessarily match the documented
    /// DB values ("Processing"). Always go through ToDbString()/ParseFromDb() instead.
    /// </summary>
    public static class JobStatusExtensions
    {
        public static string ToDbString(this JobStatus status) => status switch
        {
            JobStatus.PENDING => "Pending",
            JobStatus.PROCESSING => "Processing",
            JobStatus.COMPLETED => "Completed",
            JobStatus.FAILED => "Failed",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unmapped JobStatus value.")
        };

        public static JobStatus ParseFromDb(string dbValue) => dbValue switch
        {
            "Pending" => JobStatus.PENDING,
            "Processing" => JobStatus.PROCESSING,
            "Completed" => JobStatus.COMPLETED,
            "Failed" => JobStatus.FAILED,
            _ => throw new ArgumentOutOfRangeException(nameof(dbValue), dbValue, "Unrecognized Job status value from DB.")
        };
    }

}
