# Generative AI tools

> **Exercise:** generate a RESTful API for a simple task management system (CRUD on tasks with `title`, `description`, `status` and `due_date`, each owned by a user), then show the prompt, the output, and how the output was validated and corrected.

**Tool used:** Claude Code (agentic coding assistant in the terminal/IDE). It reads the repository, runs commands (`dotnet test`, `npm test`, a dev server, a browser) and edits files. The whole project, including this exercise, was built with it. The task-management domain here is the same one the main exercise implements, so the "output" is real code that ships in this repo.

---

## 1. The prompt

I treat the prompt like a ticket for a senior engineer. It states the context, hard constraints, the definition of done, and what *not* to do. Vague prompts ("make me a task API") get the most common answer from the training data: EF Core, an anemic model, no auth and no tests. Here, that answer would break the rules of the exercise.

```text
Role: senior .NET engineer. Build a RESTful task-management API in C# / ASP.NET Core (.NET 11).

Domain
- Task: id, title (required, ≤200), description (optional, ≤2000),
  status (Todo | InProgress | Done, default Todo), due_date (optional date, not in the past
  when set or changed), created_at, updated_at.
- Each task belongs to one user. A user can only list/read/update/delete their own tasks;
  return 404 (not 403) for someone else's task so ids can't be probed.

Architecture (hard constraints)
- Clean Architecture: Domain (no dependencies) ← Application (use cases + repository interfaces)
  ← Infrastructure (implementations) ; Api is the composition root.
- NO Entity Framework, NO Dapper, NO MediatR. Use raw ADO.NET with Microsoft.Data.Sqlite,
  parameterized SQL only.
- Entities protect their invariants (private setters, factory + Update methods that validate).
- Business errors -> typed exceptions -> one IExceptionHandler -> RFC 9457 ProblemDetails
  (400 with field errors, 404, 409, 401). No try/catch in controllers.

Auth
- POST /api/auth/register, POST /api/auth/login -> JWT (HMAC-SHA256, 60 min).
- Hash passwords with PBKDF2-SHA256 (≥600k iterations, per-user salt, constant-time compare).
- The user id comes ONLY from the token's `sub` claim, never from the route or body.

Endpoints
- GET /api/tasks?status=, GET /api/tasks/{id}, POST /api/tasks (201 + Location),
  PUT /api/tasks/{id} (full replace), DELETE /api/tasks/{id} (204).

Tests (write them first, one rule at a time)
- xUnit + NSubstitute + FluentAssertions 7.x (v8 is not free for commercial use).
- Application: unit tests with mocked repositories and a fixed TimeProvider.
- Infrastructure: run the real SQL against a temp SQLite file (don't mock the DB).
- Api: WebApplicationFactory tests for auth (401s), validation (400), CRUD, user isolation.

Definition of done
- `dotnet build` with TreatWarningsAsErrors passes, `dotnet test` is green.
- Seed a demo user + a few tasks for the demo.
- Explain any decision where you deviated from this prompt.
```

Then I worked in small steps (domain → application → infrastructure → API → frontend). After each step I had it run the tests, and I read the diff before moving on. Splitting the work this way keeps each change small enough to actually review.

---

## 2. Representative output

The full code is in this repo. Three excerpts that show the shape:

**Use case with the ownership rule** ([TaskService.cs](../src/TaskManager.Application/Tasks/TaskService.cs))

```csharp
public async Task<TaskDto> UpdateAsync(Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken ct = default)
{
    var task = await GetOwnedAsync(userId, taskId, ct);
    task.Update(request.Title, request.Description, request.Status, request.DueDate, timeProvider.GetUtcNow());

    if (!await tasks.UpdateAsync(task, ct))
        throw new NotFoundException(Resource, taskId);   // deleted between read and write

    return TaskDto.FromEntity(task);
}

private async Task<TaskItem> GetOwnedAsync(Guid userId, Guid taskId, CancellationToken ct)
{
    var task = await tasks.GetByIdAsync(taskId, ct);
    if (task is null || task.UserId != userId)
        throw new NotFoundException(Resource, taskId);   // 404, not 403: don't leak existence
    return task;
}
```

**Hand-written repository** ([SqliteTaskRepository.cs](../src/TaskManager.Infrastructure/Persistence/SqliteTaskRepository.cs))

```csharp
command.CommandText = $"""
    {SelectColumns}
    WHERE UserId = $userId
      AND ($status IS NULL OR Status = $status)
    ORDER BY DueDate IS NULL, DueDate, CreatedAt;
    """;
command.With("$userId", SqliteValues.ToDb(userId));
command.With("$status", status is { } s ? s.ToString() : DBNull.Value);
```

**Thin controller** ([TasksController.cs](../src/TaskManager.Api/Controllers/TasksController.cs))

```csharp
[HttpPost]
public async Task<ActionResult<TaskDto>> Create([FromBody] CreateTaskRequest request, CancellationToken ct)
{
    var task = await taskService.CreateAsync(User.GetUserId(), request, ct);
    return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
}
```

---

## 3. How I validated the AI's suggestions

I don't take "it compiles" as evidence. I used four checks, from cheapest to most thorough:

1. **The compiler as a gate.** `TreatWarningsAsErrors` plus nullable reference types on every project. Nullable warnings catch many AI slips, such as unchecked `null` from `GetByIdAsync`.
2. **Tests as the spec.** Every rule from the prompt has a named test, for example `Update_another_users_task_throws_not_found_and_saves_nothing`. If a rule has no test, I treat it as not implemented. I read the tests more carefully than the code, because a test that asserts the wrong thing passes and still lies.
3. **Reading the diff with a checklist** of things AI commonly gets wrong:
   - SQL built with string interpolation of *values* (injection)
   - authorization checked in the controller but not the service, or missing on one verb (IDOR)
   - passwords hashed with a fast hash (SHA-256) or without a salt
   - `DateTime.Now` sprinkled around, which makes tests non-deterministic
   - packages that are deprecated, have restrictive licences, or are already in the framework
4. **Running the real thing.** I started the API and the React app and went through login → create (with a validation error) → edit → toggle done → filter → delete. I checked the browser console for warnings and tried the mobile viewport.

## 4. What I corrected or improved

Real issues that came up while building this project, and how they were caught:

| Issue | Caught by | Fix |
| --- | --- | --- |
| `Microsoft.Data.Sqlite` stores a `Guid` parameter as a **BLOB** by default, so the TEXT `Id` column wouldn't match and lookups would silently return nothing. | Reviewing the data layer against the library's behaviour, then the `Ids_and_dates_are_stored_as_readable_text` test | Explicit conversions in `SqliteValues` (`Guid → "D"` string, dates → ISO-8601). Implicit type mapping is never relied on. |
| ASP.NET's JWT handler **renames `sub`** to a long `ClaimTypes.NameIdentifier` URI by default, so `FindFirst("sub")` would return null and every request would fail. | Review, then the integration tests | `MapInboundClaims = false`, and one `GetUserId()` extension that reads `sub`. |
| The first scaffold tried to add `Microsoft.Extensions.*.Abstractions` packages that the .NET 11 SDK already provides (`NU1510` warning, which is an error here). | Build failure | Removed the redundant package references. |
| FluentAssertions v8 changed to a **paid commercial licence**. | My checklist | Pinned to 7.2.0 (Apache 2.0). |
| Edit/Delete buttons used a visually-hidden `<span>` for context. The accessible name came out as `"EditWrite README"`, with no space. | A failing Testing Library test (`getByRole('button', { name: 'Edit Write README' })`) | Switched to `aria-label`. This is why I query by role in UI tests: they caught an accessibility bug that a screenshot wouldn't show. |
| Exporting a constant from a page component broke React Fast Refresh (lint `only-export-components`). | `oxlint` | Moved the constant to its own module. |
| On the login page, pressing Enter after "Fill in" re-clicked that button instead of submitting. | Manual browser test | Focus moves to "Log in" after filling, with a test for it. |
| The integration tests' fixed seed passwords use 600k-iteration PBKDF2, so each test spent about 0.5s hashing. | Slow test run, by design | The test host swaps in a 1k-iteration hasher. Production code is unchanged, and the hasher has its own unit tests. |

## 5. Edge cases, authentication and validation

- **Validation in layers.** The client validates for UX, the domain entity is the source of truth, and the database `CHECK`/`UNIQUE`/`FOREIGN KEY` constraints are the last line of defence. All broken rules are returned at once as field errors, not just the first one.
- **Race conditions.** If two registrations with the same username pass the service check at the same time, the UNIQUE index rejects the second, and the repository maps that SQLite error to a 409. If a task is deleted between the read and the write of an update, the user gets a 404 instead of a silent no-op.
- **Overdue tasks.** You can't *set* a due date in the past, but you can still edit an overdue task without moving its date (`Update_keeps_an_existing_overdue_date`).
- **User enumeration.** Login returns the same 401 for an unknown user and a wrong password, and another user's task returns the same 404 as a missing one.
- **Token lifecycle.** The SPA logs out when the token expires (a timer) or on any 401, and clears the React Query cache so the next user on the same browser never sees stale data.
- **Malformed input.** An unknown enum value (`"status": "Archived"`) or an invalid GUID in the route gets 400/404 from model binding before reaching the business layer. A hostile username containing SQL is stored as plain text (there's a test for it).

## Takeaways

- **Constraints go in the prompt, not in the review.** Writing "no EF, no Dapper, no MediatR", "404 not 403" and "user id only from `sub`" up front saved many corrections later.
- **Small steps, each verified.** Asking for the whole thing at once gives a diff too large to review honestly.
- **The AI is fast at the 80%. My job is the 20%:** library defaults (Guid as BLOB, claim mapping), security, licences and UX details, the things that compile fine and still break.
