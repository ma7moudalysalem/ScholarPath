# Payments module

Owner: see `.specify/specs/`.

## Folder conventions
- `Commands/<UseCase>/Command.cs`, `CommandHandler.cs`, `CommandValidator.cs`
- `Queries/<UseCase>/Query.cs`, `QueryHandler.cs`
- `DTOs/*.cs`
- Each command returns a DTO (not entity)
- Every handler takes `IApplicationDbContext`, `IMapper`, and role-specific services via DI
