import { useState } from 'react';
import { useThingentStore } from '../store/thingentStore';
import type { QueryTab } from '../store/thingentStore';
import { Table, Zap, Code, Calculator, Info, Braces, AlignLeft, Share2 } from 'lucide-react';
import VisualConceptGraph from './VisualConceptGraph';

interface ObjectDetailPaneProps {
  tab: QueryTab;
}

export default function ObjectDetailPane({ tab }: ObjectDetailPaneProps) {
  const { metadataDetails } = useThingentStore();
  const [activeView, setActiveView] = useState<'summary' | 'json' | 'visual'>('summary');
  
  if (!tab.targetName) return null;
  
  const serverStr = tab.server || '';
  const kbStr = tab.kb || '';
  const fullKey = `${serverStr}_${kbStr}_${tab.targetName.toLowerCase()}`;
  const details = metadataDetails[fullKey] || metadataDetails[tab.targetName.toLowerCase()];
  
  // Icon mapping based on targetType
  const getIcon = () => {
    switch(tab.targetType?.toLowerCase()) {
      case 'concept': return <Table className="w-5 h-5 text-indigo-500" />;
      case 'rule': return <Zap className="w-5 h-5 text-yellow-500" />;
      case 'function': return <Code className="w-5 h-5 text-emerald-500" />;
      case 'operator': return <Calculator className="w-5 h-5 text-purple-500" />;
      default: return <Info className="w-5 h-5 text-[var(--brand-primary)]" />;
    }
  };

  return (
    <div className="h-full w-full bg-[var(--bg-surface)] text-[var(--text-main)] overflow-y-auto p-6 flex justify-center">
      <div className="max-w-4xl w-full">
        {/* Header */}
        <div className="flex items-center space-x-3 mb-8 border-b border-[var(--border-subtle)] pb-4">
          <div className="p-2 bg-[var(--bg-surface-alt)] rounded-lg">
            {getIcon()}
          </div>
          <div>
            <h1 className="text-xl font-semibold tracking-tight text-[var(--text-main)]">{tab.targetName}</h1>
            <div className="flex items-center space-x-2 mt-1">
              <p className="text-xs font-medium uppercase tracking-wider text-[var(--text-muted)]">{tab.targetType}</p>
              {tab.kb && (
                <>
                  <span className="text-[var(--border-subtle)]">•</span>
                  <p 
                    className="text-xs font-medium text-[var(--brand-primary)]"
                  >
                    KB: {tab.kb}
                  </p>
                </>
              )}
              {tab.server && (
                <>
                  <span className="text-[var(--border-subtle)]">•</span>
                  <p className="text-xs font-medium text-[var(--text-muted)]">Server: {tab.server}</p>
                </>
              )}
            </div>
          </div>
        </div>

        {/* View Toggle */}
        {details?.rows?.[0]?._JsonData && (
          <div className="flex items-center space-x-2 mb-4">
            <button 
              onClick={() => setActiveView('summary')}
              className={`flex items-center space-x-1.5 px-3 py-1.5 rounded text-[13px] font-medium transition-colors ${activeView === 'summary' ? 'bg-[var(--brand-primary)]/10 text-[var(--brand-primary)]' : 'text-[var(--text-muted)] hover:text-[var(--text-main)] hover:bg-[var(--bg-surface-alt)]'}`}
            >
              <AlignLeft className="w-4 h-4" />
              <span>Summary</span>
            </button>
            <button 
              onClick={() => setActiveView('json')}
              className={`flex items-center space-x-1.5 px-3 py-1.5 rounded text-[13px] font-medium transition-colors ${activeView === 'json' ? 'bg-[var(--brand-primary)]/10 text-[var(--brand-primary)]' : 'text-[var(--text-muted)] hover:text-[var(--text-main)] hover:bg-[var(--bg-surface-alt)]'}`}
            >
              <Braces className="w-4 h-4" />
              <span>Structured Data (JSON)</span>
            </button>
            <button 
              onClick={() => setActiveView('visual')}
              className={`flex items-center space-x-1.5 px-3 py-1.5 rounded text-[13px] font-medium transition-colors ${activeView === 'visual' ? 'bg-[var(--brand-primary)]/10 text-[var(--brand-primary)]' : 'text-[var(--text-muted)] hover:text-[var(--text-main)] hover:bg-[var(--bg-surface-alt)]'}`}
            >
              <Share2 className="w-4 h-4" />
              <span>Visual Graph</span>
            </button>
          </div>
        )}

        {/* Content */}
        {!details ? (
          <div className="flex flex-col items-center justify-center py-20 space-y-4">
            <div className="w-6 h-6 border-2 border-[var(--brand-primary)] border-t-transparent rounded-full animate-spin"></div>
            <p className="text-sm text-[var(--text-sub)]">Loading details...</p>
          </div>
        ) : activeView === 'visual' && details.rows?.[0]?._JsonData ? (
          <div className="h-[600px] w-full mt-4">
            {(() => {
              try {
                const parsed = JSON.parse(details.rows[0]._JsonData);
                return <VisualConceptGraph jsonData={parsed} />;
              } catch(e) {
                return <div className="text-red-500">Error parsing graph data</div>;
              }
            })()}
          </div>
        ) : activeView === 'json' && details.rows?.[0]?._JsonData ? (
          <div className="bg-[#1e1e1e] rounded-lg overflow-hidden border border-[#333]">
            <pre className="p-4 text-[13px] font-mono text-[#d4d4d4] overflow-x-auto whitespace-pre-wrap break-words">
              {(() => {
                try {
                  const parsed = JSON.parse(details.rows[0]._JsonData);
                  return JSON.stringify(parsed, null, 2);
                } catch(e) {
                  return details.rows[0]._JsonData;
                }
              })()}
            </pre>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {details.headers?.filter((h: string) => h !== '_JsonData').map((h: string, i: number) => {
              const val = details.rows?.[0]?.[h];
              const isEmpty = val === null || val === undefined || val === '' || val === 'None';

              const isList = !isEmpty && typeof val === 'string' && val.includes(',') && (h === 'Variables' || h === 'Aliases');
              const items = isList ? val.split(',').map((v: string) => v.trim()) : [String(val ?? '')];

              return (
                <div key={i} className="bg-[var(--bg-surface-alt)]/30 border border-[var(--border-subtle)]/50 rounded-lg p-4 transition-colors hover:border-[var(--border-subtle)]">
                  <h3 className="text-[11px] font-bold text-[var(--text-muted)] tracking-wider uppercase mb-2">{h}</h3>
                  {isList ? (
                    <ul className="space-y-1.5 mt-2">
                      {items.map((item, idx) => (
                        <li key={idx} className="flex items-center space-x-2">
                          <div className="w-1.5 h-1.5 rounded-full bg-[var(--brand-primary)]/50"></div>
                          <span className="text-sm text-[var(--text-sub)] font-medium">{item}</span>
                        </li>
                      ))}
                    </ul>
                  ) : isEmpty ? (
                    <p className="text-sm text-[var(--text-muted)] italic">—</p>
                  ) : (
                    <p className="text-sm text-[var(--text-sub)] break-words whitespace-pre-wrap">{String(val)}</p>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
