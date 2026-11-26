import { useState } from 'react'
import { ApiError } from '../api/http'
import { TaskStatus, type Task, type TaskInput } from '../api/types'
import { Modal } from '../components/Modal'
import { StatusFilter } from '../features/tasks/StatusFilter'
import { TaskCard } from '../features/tasks/TaskCard'
import { TaskForm } from '../features/tasks/TaskForm'
import { useCreateTask, useDeleteTask, useTasks, useUpdateTask } from '../features/tasks/useTasks'

type Editor = { mode: 'create' } | { mode: 'edit'; task: Task } | null

export function TasksPage() {
  const [filter, setFilter] = useState<TaskStatus | null>(null)
  const [editor, setEditor] = useState<Editor>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const tasksQuery = useTasks(filter)
  const createTask = useCreateTask()
  const updateTask = useUpdateTask()
  const deleteTask = useDeleteTask()

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

  function handleToggleDone(task: Task) {
    setActionError(null)
    updateTask.mutate(
      {
        id: task.id,
        input: {
          title: task.title,
          description: task.description,
          dueDate: task.dueDate,
          priority: task.priority,
          status: task.status === TaskStatus.Done ? TaskStatus.Todo : TaskStatus.Done,
        },
      },
      { onError: (error) => setActionError(error.message) },
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
        <button type="button" className="button primary" onClick={() => openEditor({ mode: 'create' })}>
          + New task
        </button>
      </div>

      <StatusFilter value={filter} onChange={setFilter} />

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
          <p>{filter ? 'No tasks with this status.' : 'No tasks yet. Create your first one!'}</p>
        </div>
      ) : (
        <ul className="task-list" aria-label="Tasks">
          {tasksQuery.data.map((task) => (
            <TaskCard
              key={task.id}
              task={task}
              busy={busyId === task.id}
              onToggleDone={handleToggleDone}
              onEdit={(t) => openEditor({ mode: 'edit', task: t })}
              onDelete={handleDelete}
            />
          ))}
        </ul>
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
