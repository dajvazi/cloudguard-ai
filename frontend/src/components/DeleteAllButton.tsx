import { useState } from 'react'
import { Trash2, RefreshCw } from 'lucide-react'
import type { PurgeResult } from '../api/client'
import './DeleteAllButton.css'

interface DeleteAllButtonProps {
  label: string
  confirmMessage: string
  onDelete: () => Promise<PurgeResult>
  onSuccess?: (result: PurgeResult) => void
  variant?: 'default' | 'compact'
}

export function DeleteAllButton({
  label,
  confirmMessage,
  onDelete,
  onSuccess,
  variant = 'default',
}: DeleteAllButtonProps) {
  const [loading, setLoading] = useState(false)
  const [feedback, setFeedback] = useState<PurgeResult | null>(null)
  const [error, setError] = useState<string | null>(null)

  const handleClick = async () => {
    if (!window.confirm(confirmMessage)) return

    setLoading(true)
    setFeedback(null)
    setError(null)

    try {
      const result = await onDelete()
      setFeedback(result)
      onSuccess?.(result)
    } catch {
      setError('Delete failed. Try again.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className={`delete-all-wrap ${variant === 'compact' ? 'delete-all-wrap--compact' : ''}`}>
      <button
        type="button"
        className="btn-delete-all"
        onClick={handleClick}
        disabled={loading}
        title={label}
      >
        {loading ? <RefreshCw size={14} className="spinner" /> : <Trash2 size={14} />}
        <span>{label}</span>
      </button>
      {feedback && (
        <span className="delete-all-feedback delete-all-feedback--success">
          {feedback.message}
        </span>
      )}
      {error && (
        <span className="delete-all-feedback delete-all-feedback--error">{error}</span>
      )}
    </div>
  )
}
