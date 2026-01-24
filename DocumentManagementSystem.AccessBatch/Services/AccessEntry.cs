namespace DocumentManagementSystem.AccessBatch.Services;

public sealed record AccessEntry(Guid DocumentId, DateOnly Day, int Count);
