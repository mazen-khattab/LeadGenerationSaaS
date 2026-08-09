# Phase 3 — CQRS Implementation Report

Summary of new application-layer CQRS changes applied to the workspace (clean-architecture, .NET 10).

## High-level changes
- Implemented logout flows for Users and System Admins with soft-revocation using EF Core ExecuteUpdateAsync (no entity load).
- Added user settings create + read (BYOK) with AES encryption via IEncryptionService; masked display for reads.
- Added connected account creation (cookies encrypted) and target group creation (dynamic JSON config).
- All async operations accept CancellationToken. Read queries use AsNoTracking() where appropriate.
- Build verified: succeeded with 5 warnings (existing warnings unrelated to these changes).

## Files added
- SaaS.Application.Features.Auth.Commands.LogoutUserCommand.cs
  - record LogoutUserCommand(Guid UserId, string? RefreshToken) : IRequest
- SaaS.Application.Features.Auth.Commands.LogoutUserCommandHandler.cs
  - Resets User.CurrentSessionToken via ExecuteUpdateAsync.
  - Soft-revokes UserRefreshToken (Expires/IsActive) via ExecuteUpdateAsync when RefreshToken supplied.
  - Calls token service to remove auth cookies.

- SaaS.Application.Features.AdminAuth.Commands.LogoutAdminCommand.cs
  - record LogoutAdminCommand(Guid AdminId, string? RefreshToken) : IRequest
- SaaS.Application.Features.AdminAuth.Commands.LogoutAdminCommandHandler.cs
  - Soft-revokes SystemAdminRefreshTokens via ExecuteUpdateAsync when RefreshToken supplied.
  - Calls token service to remove auth cookies.

- SaaS.Application.Features.Settings.Commands.CreateUserSettingsCommand.cs
  - record CreateUserSettingsCommand(Guid UserId, string? OpenAiApiKey, string? ApifyToken, int DailyLeadLimit) : IRequest<Guid>
- SaaS.Application.Features.Settings.Commands.CreateUserSettingsCommandHandler.cs
  - Encrypts non-empty keys with IEncryptionService.Encrypt and upserts UserSetting; stores UTC timestamps where relevant.

- SaaS.Application.Features.Settings.Queries.GetUserSettingsQuery.cs
  - record GetUserSettingsQuery(Guid UserId) : IRequest<UserSettingsDto>
  - record UserSettingsDto(bool HasOpenAiKey, string MaskedOpenAiKey, bool HasApifyToken, string MaskedApifyToken, int DailyLeadLimit)
- SaaS.Application.Features.Settings.Queries.GetUserSettingsQueryHandler.cs
  - Queries UserSettings with AsNoTracking().
  - Decrypts and masks keys using IEncryptionService.Decrypt in private MaskKey method (reveals last 4 characters, masks the rest).

- SaaS.Application.Features.ConnectedAccounts.Commands.AddConnectedAccountCommand.cs
  - record AddConnectedAccountCommand(Guid UserId, string AccountName, string Provider, string RawCookiesJson) : IRequest<Guid>
- SaaS.Application.Features.ConnectedAccounts.Commands.AddConnectedAccountCommandHandler.cs
  - Encrypts RawCookiesJson and persists ConnectedAccount with CookiesExpireDate = UtcNow + 7 days and IsActive = true.

- SaaS.Application.Features.TargetGroups.Commands.AddTargetGroupCommand.cs
  - record AddTargetGroupCommand(Guid UserId, string GroupName, string TargetUrl, string ConfigJson) : IRequest<Guid>
- SaaS.Application.Features.TargetGroups.Commands.AddTargetGroupCommandHandler.cs
  - Persists TargetGroup with provided dynamic ConfigJson and IsActive = true.

## Security & performance notes
- ExecuteUpdateAsync used for token/session updates to avoid loading tokens into memory and to improve perf.
- Encryption performed with the project's IEncryptionService (AES-256 wrapper) for BYOK and cookies storage.
- Read queries use AsNoTracking() to reduce change-tracking overhead.
- No plaintext secrets are stored. Masking only applied to decrypted values for presentation.

## Build & verification
- dotnet build MultiBotSaaS.slnx completed successfully (reported 5 warnings during build).

## Where to review
- New files are under: SaaS.Application/Features/{Auth,AdminAuth,Settings,ConnectedAccounts,TargetGroups}/Commands and SaaS.Application/Features/Settings/Queries.

End of report.
