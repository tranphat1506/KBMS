import { Component, type ErrorInfo, type ReactNode } from 'react';
import { AlertCircle, RotateCcw } from 'lucide-react';
import { useKbmsStore } from '../store/kbmsStore';

interface Props {
  children?: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false,
    error: null
  };

  public static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error("(ErrorBoundary) Uncaught error:", error, errorInfo);
  }

  private handleReset = () => {
    this.setState({ hasError: false, error: null });
    window.location.reload();
  };

  public render() {
    if (this.state.hasError) {
      const studioSettings = useKbmsStore.getState().studioSettings;
      const themeClass = studioSettings.theme === 'dark' ? 'dark' : '';
      const sizeClass = `size-${studioSettings.fontSize}`;
      const weightClass = `font-${studioSettings.fontWeight}`;

      return (
        <div className={`min-h-screen bg-[var(--bg-app)] flex items-center justify-center p-6 ${themeClass} ${sizeClass} ${weightClass} transition-colors duration-200 antialiased font-sans`}>
          <div className="max-w-md w-full bg-[var(--bg-surface)] rounded-2xl shadow-xl border border-[var(--border-subtle)] p-8 text-center animate-in fade-in zoom-in duration-300">
            <div className="w-16 h-16 bg-red-500/10 text-red-500 rounded-full flex items-center justify-center mx-auto mb-6">
              <AlertCircle className="w-10 h-10" />
            </div>
            
            <h2 className="text-2xl font-bold text-[var(--text-main)] mb-2 uppercase tracking-tight">Something went wrong</h2>
            <p className="text-[var(--text-sub)] mb-6 leading-relaxed font-thin">
              The application encountered an unexpected error. Don't worry, your data is safe on the server.
            </p>
            
            {this.state.error && (
              <div className="bg-[var(--bg-surface-alt)] border border-[var(--border-muted)] rounded-lg p-4 mb-8 text-left overflow-auto max-h-40 custom-scrollbar shadow-inner">
                <code className="text-xs text-red-500 font-mono whitespace-pre-wrap">
                  {this.state.error.toString()}
                </code>
              </div>
            )}
            
            <button
              onClick={this.handleReset}
              className="w-full flex items-center justify-center space-x-2 bg-[var(--brand-primary)] hover:bg-[var(--brand-primary-hover)] text-white font-bold py-3 px-6 rounded-xl shadow-lg transition-all active:scale-95 uppercase tracking-widest text-[11px]"
            >
              <RotateCcw className="w-4 h-4" />
              <span>Reload Application</span>
            </button>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}
