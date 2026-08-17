import React, { useEffect, useMemo, useState } from 'react'
import { changeStage, listTaskItems, listUrgencyLevels, reopen, restoreTaskItem, deleteTaskItem, createTaskItem, updateTaskItem } from '../api/taskItems'
import type { TaskItemDto, TaskItemQuery, TaskStage, UrgencyLevelDto } from '../api/types'
import dayjs from 'dayjs'
import { Table, Tag, Space, Button, Select, Modal, Form, message, Pagination, Typography, DatePicker, Checkbox, notification } from 'antd'
import { ExclamationCircleOutlined } from '@ant-design/icons'
import { TaskForm } from '../components/TaskForm'

const { Title } = Typography
const stages: TaskStage[] = ['Started', 'InProgress', 'Finished']
const stageLabel = (s: TaskStage) => (s === 'InProgress' ? 'In Progress' : s)

export default function App() {
  const [items, setItems] = useState<TaskItemDto[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(false)

  const [q, setQ] = useState<TaskItemQuery>({ page: 1, pageSize: 20 })
  const [urgencies, setUrgencies] = useState<UrgencyLevelDto[]>([])

  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<TaskItemDto | null>(null)
  const [form] = Form.useForm()

  const rangeValue = useMemo(() => {
    const from = q.deadlineFrom ? dayjs(q.deadlineFrom) : null
    const to = q.deadlineTo ? dayjs(q.deadlineTo) : null
    return (from || to) ? [from, to] as any : undefined
  }, [q.deadlineFrom, q.deadlineTo])

  useEffect(() => { listUrgencyLevels().then(setUrgencies).catch(() => message.error('Failed to load urgencies')) }, [])

  useEffect(() => {
    const run = async () => {
      setLoading(true)
      try {
        const res = await listTaskItems(q)
        setItems(res.items)
        setTotal(res.totalCount)
      } catch {
        message.error('Failed to load tasks')
      } finally { setLoading(false) }
    }
    run().catch(() => {})
  }, [q])

  const totalPages = useMemo(() => Math.ceil(total / (q.pageSize || 1)), [total, q.pageSize])

  const onChangeStage = async (t: TaskItemDto, target: TaskStage) => {
    try {
      await changeStage(t.id, target)
      setQ({ ...q })
      notification.success({
        message: 'Task stage updated',
        description: `“${t.title}” moved to ${stageLabel(target)}.`
      })
    } catch (e: any) {
      message.error(e?.response?.data?.message || 'Unable to change task stage')
    }
  }

  const onReopen = async (t: TaskItemDto) => {
    try {
      await reopen(t.id)
      setQ({ ...q })
      notification.success({ message: 'Task reopened', description: `“${t.title}” is now In Progress.` })
    } catch {
      message.error('Unable to reopen task')
    }
  }

  const confirmDelete = (t: TaskItemDto) => {
    Modal.confirm({
      title: 'Delete task',
      icon: <ExclamationCircleOutlined style={{ color: '#faad14' }} />,
      content: <>This will soft-delete the task “{t.title}”. You can restore it later.</>,
      okText: 'Delete',
      okButtonProps: { danger: true },
      cancelText: 'Cancel',
      onOk: async () => {
        try {
          await deleteTaskItem(t.id)
          setQ({ ...q })
          notification.success({ message: 'Task deleted', description: `“${t.title}” was soft-deleted.` })
        } catch { message.error('Unable to delete task') }
      }
    })
  }

  const confirmRestore = (t: TaskItemDto) => {
    Modal.confirm({
      title: 'Restore task',
      icon: <ExclamationCircleOutlined style={{ color: '#1677ff' }} />,
      content: <>The task “{t.title}” will be restored and visible again.</>,
      okText: 'Restore',
      cancelText: 'Cancel',
      onOk: async () => {
        try {
          await restoreTaskItem(t.id)
          setQ({ ...q })
          notification.success({ message: 'Task restored', description: `“${t.title}” was restored.` })
        } catch { message.error('Unable to restore task') }
      }
    })
  }

  const openCreate = () => { setEditing(null); form.resetFields(); setModalOpen(true) }
  const openEdit = (t: TaskItemDto) => { setEditing(t); setModalOpen(true); form.setFieldsValue({ title: t.title, notes: t.notes, urgencyLevelId: t.urgencyLevelId, deadline: t.deadline ? dayjs(t.deadline) : null }) }

  const onSubmit = async () => {
    try {
      const vals = await form.validateFields()
      const dto: any = {
        title: vals.title.trim(),
        notes: vals.notes ?? null,
        urgencyLevelId: vals.urgencyLevelId,
        deadline: vals.deadline ? vals.deadline.toISOString() : null
      }
      if (editing) {
        await updateTaskItem(editing.id, dto)
        notification.success({ message: 'Task updated', description: `“${dto.title}” was updated successfully.` })
      } else {
        await createTaskItem(dto)
        notification.success({ message: 'Task added', description: `“${dto.title}” was created successfully.` })
      }
      setModalOpen(false); setQ({ ...q })
    } catch (e: any) {
      if (e?.errorFields) return // antd validation error
      message.error(e?.response?.data?.message || 'Save failed')
    }
  }

  return (
        <div className="app-container">
      <div className="app-header">
        <Title level={3}>Task Tracker</Title>
      </div>

      <Space wrap size="middle" style={{ marginBottom: 12 }}>
       <Select
        placeholder="Stage"
        style={{ width: 160 }}
        value={(q.stage ?? 'ALL') as any}
        onChange={v => setQ({ ...q, page: 1, stage: v === 'ALL' ? undefined : (v as TaskStage) })}
        options={[{ value: 'ALL', label: 'All' }, ...stages.map(s => ({ value: s, label: s }))]}
        />
      <Select
        placeholder="Urgency"
        style={{ width: 160 }}
        value={(q.urgencyLevelId ?? 0) as any}
        onChange={v => setQ({ ...q, page: 1, urgencyLevelId: v === 0 ? undefined : (v as number) })}
        options={[{ value: 0, label: 'All' }, ...urgencies.map(u => ({ value: u.id, label: u.name }))]}
       />
      <Select
        placeholder="Sort by"
        style={{ width: 160 }}
        value={(q.sortBy ?? 'DEFAULT') as any}
        onChange={v => setQ({ ...q, page: 1, sortBy: v === 'DEFAULT' ? undefined : (v as 'urgency' | 'deadline') })}
        options={[{ value: 'DEFAULT', label: '(default)' }, { value: 'urgency', label: 'Urgency' }, { value: 'deadline', label: 'Deadline' }]}
        />
                <DatePicker.RangePicker
          placeholder={["Deadline from", "Deadline to"]}
          value={rangeValue}
          onChange={(vals) => {
            if (!vals || (!vals[0] && !vals[1])) {
              setQ({ ...q, page: 1, deadlineFrom: undefined, deadlineTo: undefined })
            } else {
              setQ({
                ...q,
                page: 1,
                deadlineFrom: vals?.[0] ? vals[0]!.startOf('day').toISOString() : undefined,
                deadlineTo: vals?.[1] ? vals[1]!.endOf('day').toISOString() : undefined
              })
            }
          }}
        />
        <Checkbox
        checked={!!q.includeDeleted}
        onChange={e => setQ({ ...q, page: 1, includeDeleted: e.target.checked })}
        >Include deleted</Checkbox>
        <Button type="primary" onClick={openCreate}>+ New Task</Button>
        <Button onClick={() => setQ({ page: 1, pageSize: q.pageSize, stage: undefined, urgencyLevelId: undefined, sortBy: undefined, deadlineFrom: undefined, deadlineTo: undefined, includeDeleted: false })}>Reset</Button>
      </Space>

      <Table
        rowKey="id"
        loading={loading}
        dataSource={items}
        pagination={false}
        columns={[
          { title: 'Title', dataIndex: 'title' },
          { title: 'Stage', dataIndex: 'stage', render: (s: TaskStage) => <Tag color={s === 'Finished' ? 'green' : s === 'InProgress' ? 'blue' : 'default'}>{s}</Tag> },
          { title: 'Urgency', render: (_: any, r: TaskItemDto) => r.urgencyLevelName || r.urgencyLevelId },
          { title: 'Deadline', render: (_: any, r: TaskItemDto) => r.deadline ? dayjs(r.deadline).format('YYYY-MM-DD') : '-' },
          { title: 'Updated', render: (_: any, r: TaskItemDto) => dayjs(r.updatedAtUtc).format('YYYY-MM-DD') },
          {
            title: 'Actions',
            render: (_: any, r: TaskItemDto) => (
              <Space>
                {r.stage !== 'Finished' && <>
                  <Button size="small" disabled={r.stage === 'InProgress'} onClick={() => onChangeStage(r, 'InProgress')}>To InProgress</Button>
                  <Button size="small" disabled={r.stage === 'Finished'} onClick={() => onChangeStage(r, 'Finished')}>To Finished</Button>
                </>}
                {r.stage === 'Finished' && <Button size="small" onClick={() => onReopen(r)}>Reopen</Button>}
                <Button size="small" onClick={() => openEdit(r)}>Edit</Button>
                {!r.isDeleted && (
                  <Button size="small" danger onClick={() => confirmDelete(r)}>Delete</Button>
                )}
                {q.includeDeleted && r.isDeleted ? (
                  <Button size="small" onClick={() => confirmRestore(r)}>Restore</Button>
                ) : null}
              </Space>
            )
          }
        ]}
      />

      <div style={{ marginTop: 12 }}>
        <Pagination
          current={q.page}
          pageSize={q.pageSize}
          total={total}
          showSizeChanger
          pageSizeOptions={[10,20,50,100] as any}
          onChange={(page, pageSize) => setQ({ ...q, page, pageSize })}
        />
      </div>

      <Modal
        open={modalOpen}
        title={editing ? 'Edit Task' : 'New Task'}
        onCancel={() => setModalOpen(false)}
        onOk={onSubmit}
        destroyOnClose
      >
        <TaskForm urgencies={urgencies} initial={editing || undefined} form={form} />
      </Modal>
    </div>
  )
}
