import { useState, useEffect } from 'react'
import {
  X,
  BrainCircuit,
  Loader2,
  Sparkles,
  ShieldCheck,
  AlertTriangle,
  CheckCircle2,
  RefreshCw,
} from 'lucide-react'
import {
  analyzeForHealing,
  executeRunbook,
  type HealingAnalysis,
  type HealingOption,
  type SelfHealingResult,
} from '../api/client'
import './SelfHealingDialog.css'

interface SelfHealingDialogProps {
  open: boolean
  serviceId: number | null
  onClose: () => void
  onComplete: (result: SelfHealingResult) => void
}

export function SelfHealingDialog({ open, serviceId, onClose, onComplete }: SelfHealingDialogProps) {
  const [analysis, setAnalysis] = useState<HealingAnalysis | null>(null)
  const [loading, setLoading] = useState(false)
  const [executing, setExecuting] = useState(false)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!open || !serviceId) return
    setLoading(true)
    setAnalysis(null)
    setSelectedId(null)
    setError(null)

    analyzeForHealing(serviceId)
      .then((result) => {
        setAnalysis(result)
        const rec = result.options.find((o) => o.recommended)
        if (rec) setSelectedId(rec.runbookId)
      })
      .catch(() => setError('Failed to analyze service'))
      .finally(() => setLoading(false))
  }, [open, serviceId])

  async function handleExecute() {
    if (!serviceId || !selectedId) return
    setExecuting(true)
    setError(null)
    try {
      const result = await executeRunbook(serviceId, selectedId)
      onComplete(result)
    } catch {
      setError('Execution failed')
    }
    setExecuting(false)
  }

  if (!open) return null

  const selected = analysis?.options.find((o) => o.runbookId === selectedId)

  return (
    <div className="dialog-overlay" onClick={onClose}>
      <div className="dialog heal-dialog" onClick={(e) => e.stopPropagation()}>
        <div className="dialog-header">
          <div className="dialog-title-group">
            <BrainCircuit size={20} className="dialog-title-icon" />
            <div>
              <h2>AI Self-Healing</h2>
              <p>Analyze metrics and choose a recovery action</p>
            </div>
          </div>
          <button className="dialog-close" onClick={onClose}>
            <X size={18} />
          </button>
        </div>

        <div className="dialog-body">
          {loading && (
            <div className="heal-loading">
              <Loader2 size={24} className="spinner" />
              <span>AI is analyzing metrics...</span>
            </div>
          )}

          {error && (
            <div className="heal-error">
              <AlertTriangle size={16} />
              <span>{error}</span>
            </div>
          )}

          {analysis && !analysis.success && (
            <div className="heal-error">
              <AlertTriangle size={16} />
              <span>No anomalies found for this service. Nothing to heal.</span>
            </div>
          )}

          {analysis && analysis.success && (
            <>
              <div className="heal-analysis-section">
                <div className="heal-analysis-header">
                  <BrainCircuit size={14} />
                  <span>AI Analysis</span>
                  <span className={`heal-severity heal-severity--${analysis.aiAnalysis?.severity?.toLowerCase()}`}>
                    {analysis.aiAnalysis?.severity}
                  </span>
                </div>
                <div className="heal-analysis-body">
                  <div className="heal-analysis-row">
                    <span className="heal-analysis-label">Service</span>
                    <span>{analysis.serviceName}</span>
                  </div>
                  <div className="heal-analysis-row">
                    <span className="heal-analysis-label">Anomaly</span>
                    <span>{analysis.anomalyType}</span>
                  </div>
                  <div className="heal-analysis-row">
                    <span className="heal-analysis-label">Root Cause</span>
                    <span>{analysis.aiAnalysis?.rootCause}</span>
                  </div>
                  <div className="heal-analysis-row">
                    <span className="heal-analysis-label">Recommendation</span>
                    <span>{analysis.aiAnalysis?.recommendedAction}</span>
                  </div>
                </div>
              </div>

              <div className="heal-options-section">
                <div className="heal-options-header">
                  <Sparkles size={14} />
                  <span>Choose Recovery Action</span>
                </div>
                <div className="heal-options-list">
                  {analysis.options.map((opt) => (
                    <OptionCard
                      key={opt.runbookId}
                      option={opt}
                      selected={selectedId === opt.runbookId}
                      onSelect={() => setSelectedId(opt.runbookId)}
                    />
                  ))}
                </div>
              </div>

              {selected && (
                <div className="heal-effect-box">
                  <ShieldCheck size={14} />
                  <div>
                    <strong>Effect of "{selected.name}"</strong>
                    <p>{selected.effect}</p>
                  </div>
                </div>
              )}
            </>
          )}
        </div>

        <div className="dialog-footer">
          <button className="btn btn--ghost" onClick={onClose} disabled={executing}>
            Cancel
          </button>
          <button
            className="btn btn--primary btn--heal-execute"
            onClick={handleExecute}
            disabled={!selectedId || executing || !analysis?.success}
          >
            {executing ? (
              <><RefreshCw size={14} className="spinner" /> Executing...</>
            ) : (
              <><CheckCircle2 size={14} /> Execute Action</>
            )}
          </button>
        </div>
      </div>
    </div>
  )
}

function OptionCard({
  option,
  selected,
  onSelect,
}: {
  option: HealingOption
  selected: boolean
  onSelect: () => void
}) {
  return (
    <button
      type="button"
      className={`heal-option-card ${selected ? 'heal-option-card--selected' : ''}`}
      onClick={onSelect}
    >
      <div className="heal-option-top">
        <span className="heal-option-name">{option.name}</span>
        {option.recommended && (
          <span className="heal-option-badge">Recommended</span>
        )}
      </div>
      <p className="heal-option-desc">{option.description}</p>
    </button>
  )
}
