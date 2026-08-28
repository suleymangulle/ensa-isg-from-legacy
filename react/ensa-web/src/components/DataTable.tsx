import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Alert,
  DataGrid,
  PageHeader,
  Skeleton,
  Spinner as RichSpinner,
  type DataGridColumn,
} from 'rich-react-component'

/**
 * List-screen primitives, built on `rich-react-component`.
 *
 * The library owns the markup — `DataGrid`, `PageHeader`, `Spinner`, `Alert` — and this module
 * owns what the library deliberately does not: the Turkish and English copy. The library's
 * loading text, its pagination labels and its empty-state default are English literals, so the
 * states that carry words are rendered here and only the wordless ones are handed to it.
 *
 * The exported names and their props are unchanged, so every module page keeps working.
 */

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
  /** Placeholder rows drawn while loading. Defaults to five. */
  skeletonRows?: number
}

/** One placeholder row. The grid needs a row object; nothing is read off it but the key. */
interface PlaceholderRow {
  key: number
}

export default function DataTable<T>({
  columns,
  rows,
  rowKey,
  isLoading,
  error,
  emptyMessage,
  label,
  skeletonRows = 5,
}: DataTableProps<T>) {
  const { t } = useTranslation()

  // Loading and failure are rendered here rather than through the grid's own `loading` / `error`
  // props: those render an English "Loading…" and a bare red row.
  //
  // Loading draws the real grid with placeholder cells rather than a spinner in an empty box, so
  // the column headers are readable while the rows arrive and the page does not jump once they do.
  if (isLoading) {
    const placeholders: PlaceholderRow[] = Array.from(
      { length: skeletonRows }, (_, index) => ({ key: index }))

    const placeholderColumns: DataGridColumn<PlaceholderRow>[] = columns.map((column) => ({
      key: column.key,
      header: column.header,
      align: column.align,
      width: column.width,
      render: (row) => <Skeleton width={row.key % 3 === 2 ? '55%' : '80%'} height="1rem" />,
    }))

    return (
      <div role="group" aria-label={label}>
        <span className="visually-hidden" role="status">
          {t('common.loading')}
        </span>
        <DataGrid
          columns={placeholderColumns}
          rows={placeholders}
          rowKey={(row) => row.key}
          emptyText=""
        />
      </div>
    )
  }

  if (error) return <ErrorPanel message={error} flush />

  const gridColumns: DataGridColumn<T>[] = columns.map((column) => ({
    key: column.key,
    header: column.header,
    align: column.align,
    width: column.width,
    render: column.render,
  }))

  return (
    <div role="group" aria-label={label}>
      <DataGrid
        columns={gridColumns}
        rows={rows ?? []}
        rowKey={rowKey}
        emptyText={emptyMessage ?? t('table.empty')}
      />
    </div>
  )
}

/** Centred loading indicator with a translated screen-reader label. */
export function Spinner() {
  const { t } = useTranslation()
  return (
    <div className="text-center py-5">
      <RichSpinner label={t('common.loading')} />
    </div>
  )
}

interface PaginationProps {
  total: number
  page: number
  pageSize: number
  onPageChange: (nextPage: number) => void
}

/**
 * The one control still drawn here rather than taken from the library: `Pagination`'s "Previous"
 * and "Next" labels and its `aria-label` are English literals with no prop to change them, and a
 * Turkish-first product cannot ship an English pager. Swap this body for `RichPagination` the day
 * the library accepts those labels.
 */
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
  return <PageHeader title={title} description={description} actions={action} />
}

/** Inline error panel used by pages that render outside a DataTable. */
export function ErrorPanel({ message, flush }: { message: string; flush?: boolean }) {
  return (
    <Alert variant="danger" className={flush ? 'm-0' : undefined}>
      {message}
    </Alert>
  )
}
