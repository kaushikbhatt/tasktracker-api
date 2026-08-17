export type TaskStage = 'Started' | 'InProgress' | 'Finished';

export interface TaskItemDto {
  id: string;
  title: string;
  notes?: string | null;
  stage: TaskStage;
  urgencyLevelId: number;
  urgencyLevelName: string;
  deadline?: string | null; // ISO string
  createdAtUtc: string; // ISO
  updatedAtUtc: string; // ISO
  isDeleted: boolean;
}

export interface UrgencyLevelDto {
  id: number;
  name: string;
  sortOrder: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface TaskItemQuery {
  stage?: TaskStage;
  urgencyLevelId?: number;
  deadlineFrom?: string; // ISO date
  deadlineTo?: string;   // ISO date
  includeDeleted?: boolean;
  sortBy?: 'urgency' | 'deadline';
  sortDescending?: boolean;
  page?: number;
  pageSize?: number;
}

export interface CreateTaskItemDto {
  title: string;
  notes?: string | null;
  urgencyLevelId: number;
  deadline?: string | null;
}

export interface UpdateTaskItemDto {
  title: string;
  notes?: string | null;
  urgencyLevelId: number;
  deadline?: string | null;
}

export interface PatchTaskItemDto {
  title?: string | null;
  notes?: string | null;
  urgencyLevelId?: number;
  deadline?: string | null;
}
