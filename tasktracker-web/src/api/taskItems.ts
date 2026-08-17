import { http } from './http'
import type { CreateTaskItemDto, PagedResult, PatchTaskItemDto, TaskItemDto, TaskItemQuery, UpdateTaskItemDto, UrgencyLevelDto } from './types'

export async function listTaskItems(q: TaskItemQuery) {
  const params: any = { ...q }
  const res = await http.get<PagedResult<TaskItemDto>>('/task-items', { params })
  return res.data
}

export async function getTaskItem(id: string) {
  const res = await http.get<TaskItemDto>(`/task-items/${id}`)
  return res.data
}

export async function createTaskItem(dto: CreateTaskItemDto) {
  const res = await http.post<TaskItemDto>('/task-items', dto)
  return res.data
}

export async function updateTaskItem(id: string, dto: UpdateTaskItemDto) {
  const res = await http.put<TaskItemDto>(`/task-items/${id}`, dto)
  return res.data
}

export async function patchTaskItem(id: string, dto: PatchTaskItemDto) {
  const res = await http.patch<TaskItemDto>(`/task-items/${id}`, dto)
  return res.data
}

export async function deleteTaskItem(id: string) {
  await http.delete(`/task-items/${id}`)
}

export async function restoreTaskItem(id: string) {
  const res = await http.post<TaskItemDto>(`/task-items/${id}/restore`, {})
  return res.data
}

export async function changeStage(id: string, targetStage: string) {
  const res = await http.post<TaskItemDto>(`/task-items/${id}/stage`, { targetStage })
  return res.data
}

export async function reopen(id: string) {
  const res = await http.post<TaskItemDto>(`/task-items/${id}/reopen`, {})
  return res.data
}

export async function listUrgencyLevels() {
  const res = await http.get<UrgencyLevelDto[]>('/urgency-levels')
  return res.data
}
