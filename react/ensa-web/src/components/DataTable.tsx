import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

export interface Column<T> {
  /** Stable identifier used as the React key — never shown to the user. */
  key: string
  /** Already translated column header. */
  header: string
  render: (row: T) => ReactNode
  width?: string
  align?: 'start' | 'center' | 'end'
}

interface DataTableProps<T> {
  columns: Column<T>[]
  rows: T[] | undefined
  rowKey: (row: T) => string | number
  isLoading?: boolean
  error?: string | null
  /** Overrides the default `table.empty` text. */
  emptyMessage?: string
  /** Accessible name of the table. */
  label: string
}

export default function DataTable<T>({
  columns,
  rows,
  rowKey,
  isLoading,
  error,
  emptyMessage,
  label,
}: DataTableProps<T>) {
  const { t } = useTranslation()

  if (isLoading) return <Spinner />

  if (error) {
    return (
      <div
        className="alert border-0 m-0"
        style={{ backgroundColor: 'var(--kt-danger-light)', color: 'var(--kt-danger)' }}
        role="alert"
      >
        {error}
      </div>
    )
  }

  if (!rows?.length) {
    return (
      <div className="text-center py-5" style={{ color: 'var(--kt-gray-500)' }}>
        {emptyMessage ?? t('table.empty')}
      </div>
    )
  }

  return (
    <div className="table-responsive">
      <table className="table table-hover align-middle mb-0" aria-label={label}>
        <thead>
          <tr>
            {columns.map((column) => (
              <th
                key={column.key}
                scope="col"
                style={{ width: column.width }}
                className={column.align ? `text-${column.align}` : undefined}
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={rowKey(row)}>
              {columns.map((column) => (
                <td key={column.key} className={column.align ? `text-${column.align}` : undefined}>
                  {column.render(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

/** Centred loading indicator with a translated screen-reader label. */
export function Spinner() {
  const { t } = useTranslation()
  return (
    <div className="text-center py-5">
      <div className="spinner-border text-primary" role="status">
        <span className="visually-hidden">{t('common.loading')}</span>
      </div>
    </div>
  )
}

interface PaginationProps {
  total: number
  page: number
  pageSize: number
  onPageChange: (nextPage: number) => void
}

export function Pagination({ total, page, pageSize, onPageChange }: PaginationProps) {
  const { t } = useTranslation()

  const pageCount = Math.max(1, Math.ceil(total / pageSize))
  if (pageCount <= 1) return null

  const first = (page - 1) * pageSize + 1
  const last = Math.min(page * pageSize, total)

  return (
    <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 pt-3">
      <span style={{ color: 'var(--kt-gray-500)', fontSize: '0.875rem' }}>
        {t('pagination.summary', { total, first, last })}
      </span>
      <nav aria-label={t('pagination.label')}>
        <ul className="pagination pagination-sm mb-0">
          <li className={`page-item ${page <= 1 ? 'disabled' : ''}`}>
            <button className="page-link" type="button" onClick={() => onPageChange(page - 1)}>
              {t('pagination.previous')}
            </button>
          </li>
          <li className="page-item disabled">
            <span className="page-link">{t('pagination.position', { page, pageCount })}</span>
          </li>
          <li className={`page-item ${page >= pageCount ? 'disabled' : ''}`}>
            <button className="page-link" type="button" onClick={() => onPageChange(page + 1)}>
              {t('pagination.next')}
            </button>
          </li>
        </ul>
      </nav>
    </div>
  )
}

export function PageTitle({
  title,
  description,
  action,
}: {
  title: string
  description?: string
  action?: ReactNode
}) {
  return (
    <div className="d-flex flex-wrap align-items-center justify-content-between gap-3 mb-4">
      <div>
        <h1 className="h3 fw-bold mb-1" style={{ color: 'var(--kt-gray-900)' }}>
          {title}
        </h1>
        {description && (
          <p className="mb-0" style={{ color: 'var(--kt-gray-500)' }}>
            {description}
          </p>
        )}
      </div>
      {action}
    </div>
  )
}

/** Inline error panel used by pages that render outside a DataTable. */
export function ErrorPanel({ message }: { message: string }) {
  return (
    <div
      className="alert border-0"
      style={{ backgroundColor: 'var(--kt-danger-light)', color: 'var(--kt-danger)' }}
      role="alert"
    >
      {message}
    </div>
  )
}
