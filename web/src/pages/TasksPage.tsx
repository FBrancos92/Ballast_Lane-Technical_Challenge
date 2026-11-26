import { useState } from 'react'
import { ApiError } from '../api/http'
import { TaskStatus, type Task, type TaskInput } from '../api/types'
import { Modal } from '../components/Modal'
import { BulkStatusBar } from '../features/tasks/BulkStatusBar'
import { TaskBoard } from '../features/tasks/TaskBoard'
import { TaskCard } from '../features/tasks/TaskCard'
import { TaskForm } from '../features/tasks/TaskForm'
import {
  useCreateTask,
  useDeleteTask,
  useMoveTask,
  useTasks,
  useUpdateTask,
  useUpdateTasksStatus,
} from '../features/tasks/useTasks'

type Editor = { mode: 'create' } | { mode: 'edit'; task: Task } | null

export function TasksPage() {
  const [editor, setEditor] = useState<Editor>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  // The board shows every status side by side, so it always loads the unfiltered list.
  const tasksQuery = useTasks(null)
  const createTask = useCreateTask()
  const updateTask = useUpdateTask()
  const deleteTask = useDeleteTask()
  const updateStatuses = useUpdateTasksStatus()
  const moveTask = useMoveTask()

  // Selection mode: null when off, otherwise the ids picked so far.
  const [selected, setSelected] = useState<Set<string> | null>(null)
  const visibleTasks = tasksQuery.data ?? []
  // Ignore ids of tasks that have since disappeared (deleted in another tab, for example).
  const selectedIds = selected ? visibleTasks.filter((t) => selected.has(t.id)).map((t) => t.id) : []

  const busyId = updateTask.isPending ? updateTask.variables?.id : deleteTask.isPending ? deleteTask.variables : null
  const saving = createTask.isPending || updateTask.isPending
  const saveError = editor?.mode === 'create' ? createTask.error : editor?.mode === 'edit' ? updateTask.error : null

  function openEditor(next: Editor) {
    createTask.reset()
    updateTask.reset()
    setEditor(next)
  }

  function closeEditor() {
    setEditor(null)
  }

  function handleSave(input: TaskInput) {
    if (!editor) return
    if (editor.mode === 'create') createTask.mutate(input, { onSuccess: closeEditor })
    else updateTask.mutate({ id: editor.task.id, input }, { onSuccess: closeEditor })
  }

  function handleMove(task: Task, status: TaskStatus) {
    setActionError(null)
    moveTask.mutate(
      { task, status },
      { onError: (error) => setActionError(`Could not move "${task.title}": ${error.message}`) },
    )
  }

  function startSelecting() {
    setActionError(null)
    setSelected(new Set())
  }

  function handleSelectChange(task: Task, isSelected: boolean) {
    setSelected((current) => {
      const next = new Set(current)
      if (isSelected) next.add(task.id)
      else next.delete(task.id)
      return next
    })
  }

  function handleApplyStatus(status: TaskStatus) {
    setActionError(null)
    updateStatuses.mutate(
      { ids: selectedIds, status },
      {
        onSuccess: () => setSelected(null),
        onError: (error) => setActionError(error.message),
      },
    )
  }

  function handleDelete(task: Task) {
    setActionError(null)
    deleteTask.mutate(task.id, { onError: (error) => setActionError(error.message) })
  }

  return (
    <section className="tasks-page" aria-labelledby="tasks-heading">
      <div className="page-header">
        <h1 id="tasks-heading">My tasks</h1>
        <div className="page-actions">
          {!selected && visibleTasks.length > 0 && (
            <button type="button" className="button secondary" onClick={startSelecting}>
              Select
            </button>
          )}
          <button type="button" className="button primary" onClick={() => openEditor({ mode: 'create' })}>
            + New task
          </button>
        </div>
      </div>

      {selected && (
        <BulkStatusBar
          selectedCount={selectedIds.length}
          totalCount={visibleTasks.length}
          applying={updateStatuses.isPending}
          onApply={handleApplyStatus}
          onSelectAll={() => setSelected(new Set(visibleTasks.map((t) => t.id)))}
          onClear={() => setSelected(new Set())}
          onCancel={() => setSelected(null)}
        />
      )}

      {actionError && (
        <div className="alert" role="alert">
          {actionError}
        </div>
      )}

      {tasksQuery.isPending ? (
        <p className="empty-state" aria-live="polite">
          Loading tasks…
        </p>
      ) : tasksQuery.isError ? (
        <div className="alert" role="alert">
          Could not load tasks: {tasksQuery.error.message}{' '}
          <button type="button" className="button secondary small" onClick={() => tasksQuery.refetch()}>
            Retry
          </button>
        </div>
      ) : tasksQuery.data.length === 0 ? (
        <div className="empty-state">
          <p>No tasks yet. Create your first one!</p>
        </div>
      ) : (
        <TaskBoard
          tasks={tasksQuery.data}
          onMove={handleMove}
          renderCard={(task) => (
            <TaskCard
              task={task}
              busy={busyId === task.id}
              onEdit={(t) => openEditor({ mode: 'edit', task: t })}
              onDelete={handleDelete}
              selection={selected ? { selected: selected.has(task.id), onChange: handleSelectChange } : undefined}
            />
          )}
        />
      )}

      {editor && (
        <Modal title={editor.mode === 'create' ? 'New task' : 'Edit task'} onClose={closeEditor}>
          {saveError && !(saveError instanceof ApiError && Object.keys(saveError.fieldErrors).length > 0) && (
            <div className="alert" role="alert">
              {saveError.message}
            </div>
          )}
          <TaskForm
            key={editor.mode === 'edit' ? editor.task.id : 'new'}
            initial={editor.mode === 'edit' ? editor.task : undefined}
            submitLabel={editor.mode === 'create' ? 'Create task' : 'Save changes'}
            submitting={saving}
            serverErrors={saveError instanceof ApiError ? saveError.fieldErrors : undefined}
            onSubmit={handleSave}
            onCancel={closeEditor}
          />
        </Modal>
      )}
    </section>
  )
}
