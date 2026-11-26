# User story

> **As a** busy professional juggling several commitments,
> **I want to** keep a private list of tasks with a status, a priority and a due date,
> **so that** I can see at a glance what to work on next and nothing slips past its deadline.

## Acceptance criteria

### Account

1. I can create an account with a username, email and password.
   - The username is 3–50 characters: letters, digits, `.`, `_` or `-`. It is unique, ignoring case.
   - The email is valid and unique.
   - The password has at least 8 characters, including a letter and a digit. It is never stored in plain text.
2. I can log in with my username and password. If either is wrong, the message doesn't say which one.
3. I stay logged in after refreshing the page, until my session expires (60 minutes) or I log out.

### Tasks

4. I can **create** a task with a title (required, max 200 characters), an optional description (max 2000), a status (*To do*, *In progress* or *Done*; defaults to *To do*) and an optional due date.
5. A due date cannot be set in the past. If an existing due date has already passed, I can still edit the task without changing that date.
6. I can **see** my tasks on a board with one column per status (*To do*, *In progress*, *Done*), each showing how many tasks it holds. Within a column, the soonest due date comes first and undated tasks come last.
7. I can **edit** any field of my task.
8. I can mark a task done (or not done) with one click.
9. I can **delete** a task after confirming.
10. Open tasks past their due date are highlighted as **overdue**.
11. I only ever see and change **my own** tasks. Another user's task behaves as if it doesn't exist.
12. I can give a task a **priority**: *Low*, *Medium* or *High* (defaults to *Medium*). I choose it when I create or edit a task, each card shows it as a badge, and marking a task done keeps its priority. Tasks created before priorities existed show as *Medium*.
13. I can **select several tasks** and give them all the same status in one action. Either every selected task changes or none does.
14. I can **move a task to another column** by dragging its card there, which changes its status. The card moves at once; if saving fails, it goes back and I see why.

### Quality

15. Invalid input shows a clear message next to the field it concerns.
16. The app works on a phone and on a desktop.

## Mapping to the code

| Criterion | Enforced in | Tested in |
| --- | --- | --- |
| 1 | `User.Create`, `PasswordPolicy`, `AuthService.RegisterAsync`, `Users` UNIQUE indexes | `UserTests`, `AuthServiceTests`, `SqliteUserRepositoryTests`, `AuthEndpointsTests` |
| 2 | `AuthService.LoginAsync`, `Pbkdf2PasswordHasher` | `AuthServiceTests`, `Pbkdf2PasswordHasherTests`, `AuthEndpointsTests` |
| 3 | `sessionStore`, `AuthProvider` | `http.test.ts` |
| 4, 5, 7 | `TaskItem.Create/Update` | `TaskItemTests`, `TaskServiceTests`, `TaskForm.test.tsx` |
| 6 | `SqliteTaskRepository.ListByUserAsync`, `TaskBoard` | `SqliteTaskRepositoryTests`, `TaskEndpointsTests`, `TaskBoard.test.tsx` |
| 8, 9, 10 | `TaskCard`, `TasksPage` | `TaskCard.test.tsx` |
| 11 | `TaskService.GetOwnedAsync`, JWT `sub` claim | `TaskServiceTests`, `TaskEndpointsTests.Users_cannot_see_or_change_each_others_tasks` |
| 12 | `TaskItem.Create/Update`, `TaskService`, `SqliteTaskRepository`, `DatabaseInitializer` (migration), `TaskForm`, `TaskCard`, `TasksPage` | `TaskItemTests`, `TaskServiceTests`, `SqliteTaskRepositoryTests`, `TaskEndpointsTests`, `TaskForm.test.tsx`, `TaskCard.test.tsx` |
| 13 | `TaskItem.ChangeStatus`, `TaskService.UpdateStatusAsync`, `SqliteTaskRepository.UpdateManyAsync` (transaction), `PATCH /api/tasks/status`, `BulkStatusBar`, `TaskCard`, `TasksPage` | `TaskItemTests`, `TaskServiceTests`, `SqliteTaskRepositoryTests`, `TasksControllerTests`, `TaskEndpointsTests`, `BulkStatusBar.test.tsx`, `TaskCard.test.tsx` |
| 14 | `TaskBoard` (drag and drop), `useMoveTask` (optimistic, rolls back on error), `PATCH /api/tasks/status` | `TaskBoard.test.tsx`, `useTasks.test.tsx`, `TaskEndpointsTests` |
| 15 | `ApiExceptionHandler`, `FormField`, `TaskForm` | `TaskEndpointsTests`, `TaskForm.test.tsx` |
