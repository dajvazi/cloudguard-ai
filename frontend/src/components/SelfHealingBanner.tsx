import { Zap, CheckCircle2, X } from 'lucide-react'
import type { SelfHealingResult } from '../api/client'
import './SelfHealingBanner.css'

interface SelfHealingBannerProps {
  result: SelfHealingResult
  onClose?: () => void
}

export function SelfHealingBanner({ result, onClose }: SelfHealingBannerProps) {
  return (
    <div className={`heal-banner heal-banner--${result.success ? 'success' : 'error'}`}>
      {result.success ? <CheckCircle2 size={16} /> : <Zap size={16} />}
      <div className="heal-banner-text">
        <strong>{result.success ? 'Self-Healing Complete' : 'Self-Healing Failed'}</strong>
        <span>{result.message}</span>
        {result.aiAnalysis && (
          <span className="heal-banner-detail">
            {result.aiAnalysis.actionType}: {result.aiAnalysis.recommendedAction}
          </span>
        )}
        {result.aiAnalysis?.rootCause && (
          <span className="heal-banner-detail">{result.aiAnalysis.rootCause}</span>
        )}
        {result.executedViaSsm && result.runbookId && (
          <span className="heal-banner-detail">
            SSM runbook: {result.runbookId}
            {result.ssmCommandId && ` · ${result.ssmCommandId}`}
          </span>
        )}
        {result.executionOutput && (
          <pre className="heal-output">{result.executionOutput.trim()}</pre>
        )}
      </div>
      {onClose && (
        <button type="button" className="heal-banner-close" onClick={onClose} aria-label="Close">
          <X size={14} />
        </button>
      )}
    </div>
  )
}
