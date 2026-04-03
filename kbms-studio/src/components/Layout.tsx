import { useState, useEffect, useRef, useCallback } from 'react';
import Navbar from './Navbar';
import Sidebar from './Sidebar';
import EditorPane from './EditorPane';
import ResultsPane from './ResultsPane';
import StatusBar from './StatusBar';
import ActivityBar from './ActivityBar';
import SystemManagement from './SystemManagement';
import NotificationToasts from './NotificationToasts';
import { useKbmsStore } from '../store/kbmsStore';

export default function Layout() {
  const { activeSidebarView } = useKbmsStore();
  const [editorHeight, setEditorHeight] = useState(55);
  const [isDragging, setIsDragging] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const isDraggingRef = useRef(false);

  const handleMouseDown = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    isDraggingRef.current = true;
    setIsDragging(true);
    document.body.style.cursor = 'row-resize';
    document.body.style.userSelect = 'none';
  }, []);

  const handleMouseMove = useCallback((e: MouseEvent) => {
    if (!isDraggingRef.current || !containerRef.current) return;

    const rect = containerRef.current.getBoundingClientRect();
    const y = e.clientY - rect.top;
    const percentage = (y / rect.height) * 100;
    const clampedPercentage = Math.max(20, Math.min(80, percentage));

    requestAnimationFrame(() => {
      setEditorHeight(clampedPercentage);
    });
  }, []);

  const handleMouseUp = useCallback(() => {
    if (isDraggingRef.current) {
      isDraggingRef.current = false;
      setIsDragging(false);
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    }
  }, []);

  useEffect(() => {
    if (isDragging) {
      window.addEventListener('mousemove', handleMouseMove, { passive: true });
      window.addEventListener('mouseup', handleMouseUp);
    }
    return () => {
      window.removeEventListener('mousemove', handleMouseMove);
      window.removeEventListener('mouseup', handleMouseUp);
    };
  }, [isDragging, handleMouseMove, handleMouseUp]);

  return (
    <div className="flex flex-col h-full bg-[var(--bg-app)] text-[var(--text-main)] transition-colors duration-200">
      <Navbar />
      <div className="flex-1 flex overflow-hidden">
        <ActivityBar />

        {/* Sidebar */}
        <div className="w-[280px] flex-shrink-0 border-r border-[var(--border-subtle)] bg-[var(--bg-app)] shadow-[4px_0_24px_rgba(0,0,0,0.02)] z-10 relative flex flex-col transition-colors duration-200">
           <Sidebar />
        </div>

        {/* Main Content */}
        <div className="flex-1 flex flex-col min-w-0 bg-[var(--bg-surface)] relative transition-colors duration-200">
          {activeSidebarView === 'system' ? (
            <SystemManagement />
          ) : (
            <>
              {/* Editor */}
              <div
                className="border-b border-[var(--border-subtle)] relative shadow-sm z-10 flex flex-col overflow-hidden"
                style={{ height: `${editorHeight}%` }}
              >
                <EditorPane />
              </div>

              {/* Resizer */}
              <div
                className={`h-1.5 flex-shrink-0 relative z-20 group ${
                  isDragging
                    ? 'bg-[var(--brand-primary)] cursor-row-resize'
                    : 'bg-[var(--border-subtle)] hover:bg-[var(--brand-primary)] cursor-row-resize'
                } transition-colors`}
                onMouseDown={handleMouseDown}
              >
                <div className="absolute inset-x-0 top-1/2 -translate-y-1/2 flex justify-center pointer-events-none">
                  <div className={`w-16 h-1 rounded-full transition-all ${
                    isDragging
                      ? 'bg-white shadow-lg'
                      : 'bg-[var(--text-muted)]/30 group-hover:bg-white/50'
                  }`} />
                </div>
              </div>

              {/* Results */}
              <div
                ref={containerRef}
                className="relative flex flex-col bg-[var(--bg-app)] flex-1 overflow-hidden"
                style={{ height: `${100 - editorHeight}%` }}
              >
                <ResultsPane />
              </div>
            </>
          )}
        </div>
      </div>
      <StatusBar />
      <NotificationToasts />
    </div>
  );
}
