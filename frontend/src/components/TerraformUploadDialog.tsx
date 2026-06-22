import { useState, useRef, useCallback } from 'react'
import {
  X,
  Upload,
  FileCode2,
  AlertCircle,
  CheckCircle2,
  Loader2,
  Trash2,
  FolderArchive,
} from 'lucide-react'
import { uploadTerraform } from '../api/client'
import './TerraformUploadDialog.css'

interface TerraformUploadDialogProps {
  open: boolean
  onClose: () => void
  onSuccess: () => void
}

interface FileEntry {
  file: File
  id: string
  status: 'pending' | 'valid' | 'invalid'
  error?: string
}

const ALLOWED_EXTENSIONS = ['.tf', '.zip']
const MAX_FILE_SIZE = 10 * 1024 * 1024 // 10MB

function validateFile(file: File): { valid: boolean; error?: string } {
  const ext = '.' + file.name.split('.').pop()?.toLowerCase()
  if (!ALLOWED_EXTENSIONS.includes(ext)) {
    return { valid: false, error: `Invalid format. Only .tf and .zip are accepted.` }
  }
  if (file.size > MAX_FILE_SIZE) {
    return { valid: false, error: `File too large (max 10MB).` }
  }
  if (file.size === 0) {
    return { valid: false, error: `File is empty.` }
  }
  return { valid: true }
}

export function TerraformUploadDialog({ open, onClose, onSuccess }: TerraformUploadDialogProps) {
  const [files, setFiles] = useState<FileEntry[]>([])
  const [uploading, setUploading] = useState(false)
  const [progress, setProgress] = useState(0)
  const [uploadStatus, setUploadStatus] = useState<'idle' | 'success' | 'error'>('idle')
  const [resultMessage, setResultMessage] = useState('')
  const [dragOver, setDragOver] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)

  const addFiles = useCallback((newFiles: FileList | File[]) => {
    const entries: FileEntry[] = Array.from(newFiles).map((file) => {
      const validation = validateFile(file)
      return {
        file,
        id: crypto.randomUUID(),
        status: validation.valid ? 'valid' : 'invalid',
        error: validation.error,
      }
    })
    setFiles((prev) => [...prev, ...entries])
    setUploadStatus('idle')
  }, [])

  const removeFile = (id: string) => {
    setFiles((prev) => prev.filter((f) => f.id !== id))
    setUploadStatus('idle')
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    setDragOver(false)
    if (e.dataTransfer.files.length) addFiles(e.dataTransfer.files)
  }

  const handleUpload = async () => {
    const validFiles = files.filter((f) => f.status === 'valid')
    if (validFiles.length === 0) return

    setUploading(true)
    setProgress(0)
    setUploadStatus('idle')

    let completed = 0
    let lastError = ''

    for (const entry of validFiles) {
      try {
        await uploadTerraform(entry.file)
        completed++
        setProgress(Math.round((completed / validFiles.length) * 100))
      } catch (err) {
        lastError = err instanceof Error ? err.message : 'Upload failed'
      }
    }

    setUploading(false)

    if (completed === validFiles.length) {
      setUploadStatus('success')
      setResultMessage(`${completed} file${completed > 1 ? 's' : ''} uploaded successfully`)
      setTimeout(() => {
        onSuccess()
        handleReset()
      }, 1500)
    } else {
      setUploadStatus('error')
      setResultMessage(lastError || 'Some files failed to upload')
    }
  }

  const handleReset = () => {
    setFiles([])
    setProgress(0)
    setUploadStatus('idle')
    setResultMessage('')
  }

  if (!open) return null

  const validCount = files.filter((f) => f.status === 'valid').length
  const invalidCount = files.filter((f) => f.status === 'invalid').length

  return (
    <div className="dialog-overlay" onClick={onClose}>
      <div className="dialog" onClick={(e) => e.stopPropagation()}>
        <div className="dialog-header">
          <div className="dialog-title-group">
            <FolderArchive size={20} className="dialog-title-icon" />
            <div>
              <h2>Import Terraform</h2>
              <p>Upload .tf files or .zip archives to discover infrastructure</p>
            </div>
          </div>
          <button className="dialog-close" onClick={onClose}>
            <X size={18} />
          </button>
        </div>

        <div className="dialog-body">
          <div
            className={`dropzone ${dragOver ? 'dropzone--active' : ''} ${files.length > 0 ? 'dropzone--compact' : ''}`}
            onDragOver={(e) => { e.preventDefault(); setDragOver(true) }}
            onDragLeave={() => setDragOver(false)}
            onDrop={handleDrop}
            onClick={() => inputRef.current?.click()}
          >
            <input
              ref={inputRef}
              type="file"
              accept=".tf,.zip"
              multiple
              hidden
              onChange={(e) => { if (e.target.files) addFiles(e.target.files); e.target.value = '' }}
            />
            <Upload size={files.length > 0 ? 20 : 36} className="dropzone-icon" />
            <div className="dropzone-text">
              <strong>
                {files.length > 0 ? 'Add more files' : 'Drop files here or click to browse'}
              </strong>
              {files.length === 0 && (
                <span>Supports .tf and .zip (max 10MB per file)</span>
              )}
            </div>
          </div>

          {files.length > 0 && (
            <div className="file-list">
              <div className="file-list-header">
                <span>{files.length} file{files.length > 1 ? 's' : ''} selected</span>
                {invalidCount > 0 && (
                  <span className="file-list-errors">{invalidCount} invalid</span>
                )}
              </div>
              {files.map((entry) => (
                <div
                  key={entry.id}
                  className={`file-item ${entry.status === 'invalid' ? 'file-item--invalid' : ''}`}
                >
                  <div className="file-item-icon">
                    {entry.status === 'valid' ? (
                      <FileCode2 size={18} />
                    ) : (
                      <AlertCircle size={18} />
                    )}
                  </div>
                  <div className="file-item-info">
                    <span className="file-item-name">{entry.file.name}</span>
                    <span className="file-item-meta">
                      {entry.error || formatSize(entry.file.size)}
                    </span>
                  </div>
                  <button
                    className="file-item-remove"
                    onClick={() => removeFile(entry.id)}
                    disabled={uploading}
                  >
                    <Trash2 size={14} />
                  </button>
                </div>
              ))}
            </div>
          )}

          {uploading && (
            <div className="upload-progress">
              <div className="progress-header">
                <Loader2 size={14} className="spinner" />
                <span>Uploading... {progress}%</span>
              </div>
              <div className="progress-track">
                <div className="progress-fill" style={{ width: `${progress}%` }} />
              </div>
            </div>
          )}

          {uploadStatus !== 'idle' && (
            <div className={`upload-result upload-result--${uploadStatus}`}>
              {uploadStatus === 'success' ? <CheckCircle2 size={16} /> : <AlertCircle size={16} />}
              <span>{resultMessage}</span>
            </div>
          )}
        </div>

        <div className="dialog-footer">
          <button className="btn btn--ghost" onClick={onClose} disabled={uploading}>
            Cancel
          </button>
          <button
            className="btn btn--primary"
            onClick={handleUpload}
            disabled={uploading || validCount === 0}
          >
            {uploading ? (
              <>
                <Loader2 size={14} className="spinner" />
                Uploading...
              </>
            ) : (
              <>
                <Upload size={14} />
                Upload {validCount > 0 ? `${validCount} file${validCount > 1 ? 's' : ''}` : ''}
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  )
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}
