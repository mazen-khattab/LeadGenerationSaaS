$renameList = @(
    @("SaaS.Api\Controllers\v1\RunsController.cs", "ScrapesController.cs"),
    @("SaaS.Application\Common\Dtos\CompleteRunDto.cs", "CompleteScrapeDto.cs"),
    @("SaaS.Application\Common\Dtos\CreateRunDto.cs", "CreateScrapeDto.cs"),
    @("SaaS.Application\Features\Runs", "Scrapes"),
    @("SaaS.Application.UnitTests\Features\Runs", "Scrapes"),
    @("SaaS.Domain.UnitTests\Extensions\RunStatusExtensionsTests.cs", "ScrapeStatusExtensionsTests.cs"),
    @("SaaS.Domain\Entities\Run.cs", "Scrape.cs"),
    @("SaaS.Domain\Enums\RunStatus.cs", "ScrapeStatus.cs"),
    @("SaaS.Domain\Extensions\RunStatusExtensions.cs", "ScrapeStatusExtensions.cs"),
    @("SaaS.Infrastructure\Persistence\Configurations\RunConfiguration.cs", "ScrapeConfiguration.cs")
)

foreach ($item in $renameList) {
    if (Test-Path $item[0]) {
        Rename-Item -Path $item[0] -NewName $item[1]
    }
}

Get-ChildItem -Path "SaaS.Application\Features\Scrapes" -Recurse | Where-Object { $_.Name -match "Run" } | Rename-Item -NewName { $_.Name -replace "Run", "Scrape" }
Get-ChildItem -Path "SaaS.Application.UnitTests\Features\Scrapes" -Recurse | Where-Object { $_.Name -match "Run" } | Rename-Item -NewName { $_.Name -replace "Run", "Scrape" }
