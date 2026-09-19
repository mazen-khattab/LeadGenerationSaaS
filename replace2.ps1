$files = Get-ChildItem -Path . -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\bin\\" }

$replacements = @(
    @("Features.Runs", "Features.Scrapes"),
    @("Features/Runs", "Features/Scrapes"),
    @("activeRun", "activeScrape"),
    @("CompleteRunCommand", "CompleteScrapeCommand"),
    @("CompleteRunCommandHandler", "CompleteScrapeCommandHandler"),
    @("CompleteRunDb", "CompleteScrapeDb"),
    @("CompleteRunDto", "CompleteScrapeDto"),
    @("CreateRunCommand", "CreateScrapeCommand"),
    @("CreateRunCommandHandler", "CreateScrapeCommandHandler"),
    @("CreateRunDb", "CreateScrapeDb"),
    @("CreateRunDto", "CreateScrapeDto"),
    @("lastRun", "lastScrape"),
    @("NegativeRunsCount", "NegativeScrapesCount"),
    @("NotifyRunCompletedAsync", "NotifyScrapeCompletedAsync"),
    @("ParseFromDbToRunStatus", "ParseFromDbToScrapeStatus"),
    @("previousRun", "previousScrape"),
    @("RunCompleted", "ScrapeCompleted"),
    @("RunConfiguration", "ScrapeConfiguration"),
    @("RunId", "ScrapeId"),
    @("runId", "scrapeId"),
    @("RunStatus", "ScrapeStatus"),
    @("runStatus", "scrapeStatus"),
    @("RunsController", "ScrapesController"),
    @("RunsCount", "ScrapesCount"),
    @("runsCount", "scrapesCount"),
    @("RunStatusExtensions", "ScrapeStatusExtensions"),
    @("RunStatusExtensionsTests", "ScrapeStatusExtensionsTests"),
    @("ShouldCompleteRunWith", "ShouldCompleteScrapeWith"),
    @("ShouldCreateRunAnd", "ShouldCreateScrapeAnd"),
    @("ShouldCreateRunWithout", "ShouldCreateScrapeWithout"),
    @("WithLeadsAndRunsCount", "WithLeadsAndScrapesCount"),
    @("ShouldSendRunCompletedEvent", "ShouldSendScrapeCompletedEvent"),
    @("updatedRun", "updatedScrape"),
    @("WhenPreviousRun", "WhenPreviousScrape"),
    @("WhenRunIs", "WhenScrapeIs"),
    @("WhenRunNot", "WhenScrapeNot"),
    @("run1", "scrape1")
)

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw
    $original = $content
    
    foreach ($pair in $replacements) {
        $key = $pair[0]
        $val = $pair[1]
        $content = $content.Replace($key, $val)
    }
    
    # Plurals with regex to ensure word boundaries (CASE SENSITIVE!)
    $content = $content -creplace "\bRuns\b", "Scrapes"
    $content = $content -creplace "\bruns\b", "scrapes"
    
    # Singulars with regex to ensure word boundaries (avoid app.Run) (CASE SENSITIVE!)
    $content = $content -creplace "(?<!app\.)\bRun\b", "Scrape"
    $content = $content -creplace "\brun\b", "scrape"

    if ($original -cne $content) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
    }
}
