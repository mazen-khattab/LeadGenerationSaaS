using SaaS.Domain.Enums;
using System;

namespace SaaS.Domain.Extensions
{
    public static class AccountStatusExtensions
    {
        public static string ToDbString(this AccountStatus status) => status switch
        {
            AccountStatus.ACTIVE => "Active",
            AccountStatus.BUSY => "Busy",
            AccountStatus.COOLING_DOWN => "CoolingDown",
            AccountStatus.ACCOUNT_FLAGGED => "AccountFlagged",
            AccountStatus.BANNED => "Banned",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unmapped AccountStatus value.")
        };

        public static AccountStatus ParseFromDb(string dbValue) => dbValue switch
        {
            "Active" => AccountStatus.ACTIVE,
            "Busy" => AccountStatus.BUSY,
            "CoolingDown" => AccountStatus.COOLING_DOWN,
            "AccountFlagged" => AccountStatus.ACCOUNT_FLAGGED,
            "Banned" => AccountStatus.BANNED,
            _ => throw new ArgumentOutOfRangeException(nameof(dbValue), dbValue, "Unrecognized AccountStatus value from DB.")
        };
    }
}
