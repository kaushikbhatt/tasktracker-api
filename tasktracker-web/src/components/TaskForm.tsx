import React from 'react'
import { Form, Input, Select, DatePicker, type FormInstance } from 'antd'
import dayjs from 'dayjs'
import type { CreateTaskItemDto, UpdateTaskItemDto, UrgencyLevelDto } from '../api/types'

export type TaskFormValues = CreateTaskItemDto | UpdateTaskItemDto

export function TaskForm({ urgencies, initial, form }: { urgencies: UrgencyLevelDto[]; initial?: Partial<TaskFormValues>; form: FormInstance }) {
  React.useEffect(() => {
    form.setFieldsValue({
      title: initial?.title ?? undefined,
      notes: initial?.notes ?? null,
      urgencyLevelId: initial?.urgencyLevelId ?? undefined,
      deadline: initial?.deadline ? dayjs(initial.deadline) : null
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initial])

  return (
    <Form form={form} layout="vertical" name="taskForm">
      <Form.Item name="title" label="Title" rules={[{ required: true, message: 'Title is required' }, { max: 200 }]}> 
        <Input placeholder="Short title" />
      </Form.Item>
      <Form.Item name="notes" label="Notes" rules={[{ max: 500 }]}> 
        <Input.TextArea placeholder="Optional notes" rows={3} />
      </Form.Item>
      <Form.Item name="urgencyLevelId" label="Urgency" rules={[{ required: true, message: 'Select urgency' }]}> 
        <Select options={urgencies.map(u => ({ value: u.id, label: u.name }))} placeholder="Select urgency" />
      </Form.Item>
      <Form.Item name="deadline" label="Deadline"> 
        <DatePicker style={{ width: '100%' }} />
      </Form.Item>
    </Form>
  )
}
