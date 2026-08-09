TargetGroups feature structure (mirrors ConnectedAccounts pattern)

- Commands/
  - Add/
	- AddGroupCommand.cs
	- AddGroupValidator.cs
	- AddGroupCommandHandler.cs
  - Update/
	- UpdateGroupCommand.cs
	- UpdateGroupValidator.cs
	- UpdateGroupCommandHandler.cs
  - Delete/
	- DeleteGroupCommand.cs
	- DeleteGroupValidator.cs
	- DeleteGroupCommandHandler.cs
- Queries/
  - GetAll/
	- GetAllGroupsQuery.cs
	- GetAllGroupsQueryValidator.cs
	- GetAllGroupsQueryHandler.cs
  - GetById/
	- GetGroupByIdQuery.cs
	- GetGroupByIdQueryValidator.cs
	- GetGroupByIdQueryHandler.cs

DTOs created under SaaS.Application/Common/Dtos:
- AddGroupDto.cs
- UpdateGroupDto.cs
- GroupDto.cs

Handlers use IAppDbContext for data access and return ApiResponse<T> like other features.
