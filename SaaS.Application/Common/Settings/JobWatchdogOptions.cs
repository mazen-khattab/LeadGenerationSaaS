using System;
using System.Collections.Generic;
using System.Text;

namespace SaaS.Infrastructure
{
    public class JobWatchdogOptions
    {
        public const string SectionName = "JobWatchdog";

        public int CheckIntervalMinutes { get; set; } = 10;

        public int TimeoutThresholdMinutes { get; set; } = 15;

        /// <summary>
        /// How long we wait for the Node.js worker's liveness endpoint before assuming it's unreachable.
        /// Must stay short - this runs once per candidate-stuck job, inside the watchdog loop.
        /// </summary>
        public int LivenessCheckTimeoutSeconds { get; set; } = 10;
    }
}
