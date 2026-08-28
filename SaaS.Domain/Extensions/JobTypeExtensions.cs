using SaaS.Domain.Enums;
using System;

namespace SaaS.Domain.Extensions
{
    public static class JobTypeExtensions
    {
        public static string ToDbString(this JobType type) => type switch
        {
            JobType.MESSAGING => "Messaging",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unmapped JobType value.")
        };

        public static JobType ParseFromDb(string dbValue) => dbValue switch
        {
            "Messaging" => JobType.MESSAGING,
            _ => throw new ArgumentOutOfRangeException(nameof(dbValue), dbValue, "Unrecognized JobType value from DB.")
        };
    }
}
