import { useState, useEffect } from 'react';
import { Terminal, Info, Download, LayoutGrid, CheckCircle, ChevronLeft, ChevronRight, Maximize2, Minimize2, Network } from 'lucide-react';
import { useThingentStore } from '../store/thingentStore';

import ExplainTreeVisualizer from './ExplainTreeVisualizer';

const PaginatedTable = ({ res }: { res: any }) => {
  const [currentPage, setCurrentPage] = useState(1);
  const [explainModalData, setExplainModalData] = useState<{title: string, data: any} | null>(null);
  const [contextMenu, setContextMenu] = useState<{ x: number, y: number, text: string, rowData: any, rowIndex: number } | null>(null);
  const rowsPerPage = 100;

  useEffect(() => {
    const handleClick = () => setContextMenu(null);
    window.addEventListener('click', handleClick);
    return () => window.removeEventListener('click', handleClick);
  }, []);

  const totalRows = res.rows?.length || 0;
  const totalPages = Math.ceil(totalRows / rowsPerPage) || 1;

  // Ensure current page is valid when total rows change
  const validPage = Math.min(currentPage, totalPages);
  
  const startIndex = (validPage - 1) * rowsPerPage;
  const currentRows = res.rows?.slice(startIndex, startIndex + rowsPerPage) || [];

  const renderCellContent = (h: string, val: any) => {
    if (val === null || val === undefined) return <span className="text-[var(--text-muted)] italic font-mono text-[11px] opacity-50">NULL</span>;
    
    if (h.startsWith('EXPLAIN_TREE')) {
       try {
          const parsed = typeof val === 'string' ? JSON.parse(val) : val;
          return (
             <button 
               onClick={() => setExplainModalData({ title: h, data: parsed })}
               className="px-2 py-1 bg-emerald-500/20 text-emerald-400 rounded hover:bg-emerald-500/30 transition-colors font-semibold text-[10px]"
             >
               View Explain Tree
             </button>
          );
       } catch {
          return <span>{String(val)}</span>;
       }
    }
    
    if (h === 'AUDIT_LOG' || h === 'AUDIT_TRAIL' || h === '__audit_trail') {
       try {
          const parsed = typeof val === 'string' ? JSON.parse(val) : val;
          return (
             <div className="flex flex-col gap-2 max-h-32 overflow-auto custom-scrollbar pr-2 min-w-[250px]">
               {parsed.map((log: any, idx: number) => (
                  <div key={idx} className="bg-[var(--bg-app)] p-2 rounded border border-[var(--border-subtle)] text-[11px] font-mono leading-tight shadow-sm hover:border-[var(--brand-primary)] transition-colors">
                     <div className="text-[var(--brand-primary)] font-bold mb-1 flex items-center justify-between">
                        <span>{log.RuleName || log.Action}</span>
                        {log.StepCost !== undefined && <span className="text-[var(--text-sub)] font-normal ml-2">Cost: {log.StepCost}</span>}
                     </div>
                     <div className="text-[var(--text-main)] grid grid-cols-2 gap-2 mt-1 bg-[var(--bg-surface)] p-1.5 rounded border border-[var(--border-muted)]/20">
                       <div>
                         <span className="text-[var(--text-muted)] mr-1 block text-[10px]">Inputs:</span>
                         <span className="text-emerald-500 dark:text-emerald-400 whitespace-pre-wrap block truncate">{JSON.stringify(log.InputFacts || {})}</span>
                       </div>
                       <div>
                         <span className="text-[var(--text-muted)] mr-1 block text-[10px]">Outputs:</span>
                         <span className="text-amber-600 dark:text-amber-400 whitespace-pre-wrap block truncate">{JSON.stringify(log.OutputFacts || {})}</span>
                       </div>
                     </div>
                  </div>
               ))}
             </div>
          );
       } catch {
          return <span className="text-red-400">{String(val)}</span>;
       }
    }

    if (h === 'GENERATED_VARIABLES' || h === '__generated_vars') {
       try {
          const parsed = typeof val === 'string' ? JSON.parse(val) : val;
          return (
             <div className="flex flex-wrap gap-1 max-w-[200px]">
               {parsed.map((v: string, idx: number) => (
                  <span key={idx} className="text-[10px] bg-[var(--brand-primary)]/10 text-[var(--brand-primary)] px-1.5 py-0.5 rounded font-mono border border-[var(--brand-primary)]/20">
                    {v}
                  </span>
               ))}
             </div>
          );
       } catch {
          return <span>{String(val)}</span>;
       }
    }

    if (h === 'MISSING_FACTS') {
       try {
          const parsed = typeof val === 'string' ? JSON.parse(val) : val;
          if (Array.isArray(parsed) && parsed.length > 0) {
             return (
               <div className="flex flex-col space-y-1">
                 {parsed.map((fact: any, idx: number) => (
                    <div key={idx} className="text-[10px] bg-amber-500/10 text-amber-400 p-1 rounded font-mono border border-amber-500/20 flex items-center">
                      <Info className="w-3 h-3 mr-1 shrink-0" /> Missing: {fact.Variable} (Rule: {fact.RuleId})
                    </div>
                 ))}
               </div>
             );
          }
       } catch {
          return <span>{String(val)}</span>;
       }
    }

    if (typeof val === 'number') {
       return <span className="text-blue-500 font-mono font-medium">{String(val)}</span>;
    }
    return <span>{String(val)}</span>;
  };

  return (
    <div className="border border-[var(--border-subtle)] rounded-lg flex flex-col shadow-sm bg-[var(--bg-surface)] min-w-0 transition-colors">
      <div className="overflow-x-auto min-h-[50px] custom-scrollbar bg-[var(--bg-app)]/50 relative">
        <table className="w-full text-left border-collapse min-w-max">
          <thead className="bg-[var(--bg-surface)] sticky top-0 z-10 shadow-[0_1px_2px_rgba(0,0,0,0.05)]">
            <tr>
              <th className="px-2 py-1.5 border-b border-r border-[var(--border-subtle)] bg-[var(--bg-surface-alt)] w-12 text-center text-[var(--text-muted)] font-medium font-mono">#</th>
              {res.headers?.map((h: string, i: number) => (
                <th key={i} className="px-4 py-2 font-bold text-[var(--text-main)] border-b border-r border-[var(--border-subtle)] last:border-r-0 whitespace-nowrap text-[11px] tracking-wider">
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-[var(--border-muted)]">
            {currentRows.length > 0 ? (
              currentRows.map((row: Record<string, any>, rIdx: number) => {
                const absoluteIdx = startIndex + rIdx;
                return (
                  <tr key={absoluteIdx} className="hover:bg-[var(--brand-primary-light)]/40 border-b border-[var(--border-muted)] last:border-b-0 group transition-colors">
                    <td 
                      className="px-2 py-1.5 border-r border-[var(--border-muted)] bg-[var(--bg-app)]/30 w-12 text-center text-[11px] font-mono text-[var(--text-muted)] cursor-context-menu select-none"
                      onContextMenu={(e) => {
                        e.preventDefault();
                        setContextMenu({
                          x: e.clientX,
                          y: e.clientY,
                          text: String(absoluteIdx + 1),
                          rowData: row,
                          rowIndex: absoluteIdx + 1,
                        });
                      }}
                    >
                      {absoluteIdx + 1}
                    </td>
                    {res.headers?.map((h: string, cIdx: number) => (
                      <td 
                        key={cIdx} 
                        className="px-4 py-1.5 text-[var(--text-sub)] border-r border-[var(--border-muted)]/30 last:border-r-0 whitespace-nowrap group-hover:text-[var(--text-main)] focus-within:bg-[var(--brand-primary-light)]/20 focus-within:outline-none" 
                        tabIndex={0}
                        onContextMenu={(e) => {
                          e.preventDefault();
                          setContextMenu({
                            x: e.clientX,
                            y: e.clientY,
                            text: row?.[h] !== null && row?.[h] !== undefined ? String(row[h]) : 'NULL',
                            rowData: row,
                            rowIndex: absoluteIdx + 1,
                          });
                        }}
                      >
                        <div className="max-h-32 overflow-auto custom-scrollbar whitespace-pre-wrap leading-normal font-thin">
                          {renderCellContent(h, row?.[h])}
                        </div>
                      </td>
                    ))}
                  </tr>
                );
              })
            ) : (
              <tr>
                <td colSpan={(res.headers?.length || 0) + 1} className="px-4 py-6 text-center text-[var(--text-muted)] italic bg-[var(--bg-surface)]">
                  No rows returned for this result set.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {explainModalData && (
         <div className="fixed inset-0 z-50 bg-black/60 flex items-center justify-center p-8 backdrop-blur-sm">
            <div className="bg-[var(--bg-surface)] border border-[var(--border-subtle)] rounded-xl shadow-2xl w-full h-full max-w-6xl flex flex-col">
               <div className="flex items-center justify-between p-4 border-b border-[var(--border-subtle)]">
                  <h3 className="text-lg font-bold text-[var(--text-main)] flex items-center space-x-2">
                     <Network className="w-5 h-5 text-[var(--brand-primary)]" />
                     <span>Explanation Tree: {explainModalData.title}</span>
                  </h3>
                  <button onClick={() => setExplainModalData(null)} className="px-3 py-1.5 bg-[var(--bg-app)] text-[var(--text-sub)] hover:text-[var(--text-main)] rounded border border-[var(--border-subtle)]">
                     Close
                  </button>
               </div>
               <div className="flex-1 overflow-hidden relative p-4 bg-[var(--bg-app)]">
                  <ExplainTreeVisualizer data={explainModalData.data} />
               </div>
            </div>
         </div>
      )}

      {contextMenu && (
        <div 
          className="fixed z-50 min-w-[160px] bg-[var(--bg-surface)] border border-[var(--border-subtle)] rounded-lg shadow-xl py-1 text-[13px] font-sans"
          style={{ top: Math.min(contextMenu.y, window.innerHeight - 150), left: Math.min(contextMenu.x, window.innerWidth - 200) }}
          onClick={(e) => e.stopPropagation()}
        >
          <button 
            className="w-full text-left px-4 py-1.5 text-[var(--text-main)] hover:bg-[var(--brand-primary)] hover:text-white transition-colors"
            onClick={() => {
              navigator.clipboard.writeText(contextMenu.text);
              setContextMenu(null);
            }}
          >
            Copy Cell
          </button>
          <button 
            className="w-full text-left px-4 py-1.5 text-[var(--text-main)] hover:bg-[var(--brand-primary)] hover:text-white transition-colors"
            onClick={() => {
              const tsv = res.headers.map((h: string) => {
                let v = contextMenu.rowData[h];
                if (v === null || v === undefined) return 'NULL';
                return String(v).replace(/\t/g, ' ');
              }).join('\t');
              navigator.clipboard.writeText(tsv);
              setContextMenu(null);
            }}
          >
            Copy Row (TSV)
          </button>
          <button 
            className="w-full text-left px-4 py-1.5 text-[var(--text-main)] hover:bg-[var(--brand-primary)] hover:text-white transition-colors"
            onClick={() => {
              const header = res.headers.join('\t');
              const row = res.headers.map((h: string) => {
                let v = contextMenu.rowData[h];
                if (v === null || v === undefined) return 'NULL';
                return String(v).replace(/\t/g, ' ');
              }).join('\t');
              navigator.clipboard.writeText(header + '\n' + row);
              setContextMenu(null);
            }}
          >
            Copy Row with Header
          </button>
          <div className="border-t border-[var(--border-muted)] my-1" />
          <button 
            className="w-full text-left px-4 py-1.5 text-[var(--text-main)] hover:bg-[var(--brand-primary)] hover:text-white transition-colors"
            onClick={() => {
              navigator.clipboard.writeText(JSON.stringify(contextMenu.rowData, null, 2));
              setContextMenu(null);
            }}
          >
            Copy Row (JSON)
          </button>
        </div>
      )}

      {totalRows > rowsPerPage && (
        <div className="flex items-center justify-between px-4 py-2 border-t border-[var(--border-subtle)] bg-[var(--bg-app)]/80 rounded-b-lg">
          <div className="text-[11px] text-[var(--text-sub)] font-medium select-none">
            Showing <span className="text-[var(--text-main)] font-bold">{startIndex + 1}</span> to <span className="text-[var(--text-main)] font-bold">{Math.min(startIndex + rowsPerPage, totalRows)}</span> of <span className="text-[var(--text-main)] font-bold">{totalRows}</span> rows
          </div>
          <div className="flex items-center space-x-1">
            <button 
              onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
              disabled={validPage === 1}
              className="flex items-center justify-center p-1 text-[var(--text-sub)] hover:text-[var(--brand-primary)] hover:bg-[var(--brand-primary-light)]/20 rounded disabled:opacity-30 disabled:hover:bg-transparent disabled:hover:text-[var(--text-sub)] disabled:cursor-not-allowed transition-colors cursor-pointer"
              title="Previous Page"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
            <div className="flex items-center px-2 py-0.5 rounded bg-[var(--bg-surface)] border border-[var(--border-subtle)] text-[11px] font-medium text-[var(--text-sub)] shadow-sm select-none">
              <span className="text-[var(--brand-primary)] font-bold mr-1">{validPage}</span> / <span className="ml-1">{totalPages}</span>
            </div>
            <button 
              onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
              disabled={validPage === totalPages}
              className="flex items-center justify-center p-1 text-[var(--text-sub)] hover:text-[var(--brand-primary)] hover:bg-[var(--brand-primary-light)]/20 rounded disabled:opacity-30 disabled:hover:bg-transparent disabled:hover:text-[var(--text-sub)] disabled:cursor-not-allowed transition-colors cursor-pointer"
              title="Next Page"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
};


export default function ResultsPane() {
  const { result, activeTab, setActiveTab } = useThingentStore();
  const [isFullscreen, setIsFullscreen] = useState(false);

  const handleExportCSV = () => {
    const firstTabular = result?.find((r: any) => r.rows && r.rows.length > 0);
    if (!firstTabular) return;
    
    const headers = firstTabular.headers.join(',');
    const rows = firstTabular.rows.map((row: any) =>
      firstTabular.headers.map((h: string) => {
        let val = row[h];
        if (val === null || val === undefined) return '';
        val = String(val).replace(/"/g, '""');
        return `"${val}"`;
      }).join(',')
    ).join('\n');

    const csv = headers + '\n' + rows;
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', `query_result_${new Date().getTime()}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  // Handle Escape key for fullscreen
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isFullscreen) {
        setIsFullscreen(false);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isFullscreen]);

  const content = (
    <div className="h-full w-full bg-[var(--bg-surface)] flex flex-col font-sans relative transition-colors duration-200">
      <div className="flex items-center px-4 h-10 bg-[var(--bg-app)] border-b border-[var(--border-subtle)] justify-between select-none">
        <div className="flex items-center space-x-1 relative h-full">
          <button
            onClick={() => setActiveTab('results')}
            className={`flex items-center space-x-1.5 px-3 h-full text-sm font-semibold tracking-wide transition-all relative group cursor-pointer ${activeTab === 'results' ? 'text-[var(--brand-primary)] bg-[var(--bg-surface)]' : 'text-[var(--text-sub)] hover:text-[var(--text-main)] hover:bg-[var(--bg-surface-alt)]/50'
              }`}
          >
            <LayoutGrid className="w-3.5 h-3.5" />
            <span>Results</span>
            <div className={`absolute bottom-0 left-0 right-0 h-0.5 rounded-t-sm transition-all ${activeTab === 'results' ? 'bg-[var(--brand-primary)]' : 'bg-transparent group-hover:bg-[var(--border-subtle)]'}`} />
          </button>

          <button
            onClick={() => setActiveTab('messages')}
            className={`flex items-center space-x-1.5 px-3 h-full text-sm font-semibold tracking-wide transition-all relative group cursor-pointer ${activeTab === 'messages' ? 'text-[var(--brand-primary)] bg-[var(--bg-surface)]' : 'text-[var(--text-sub)] hover:text-[var(--text-main)] hover:bg-[var(--bg-surface-alt)]/50'
              }`}
          >
            <Terminal className="w-3.5 h-3.5" />
            <span>Messages</span>
            <div className={`absolute bottom-0 left-0 right-0 h-0.5 rounded-t-sm transition-all ${activeTab === 'messages' ? 'bg-[var(--brand-primary)]' : 'bg-transparent group-hover:bg-[var(--border-subtle)]'}`} />
          </button>
        </div>

        <div className="flex items-center space-x-2">
          <button
            onClick={() => setIsFullscreen(!isFullscreen)}
            className="text-[var(--text-muted)] hover:text-[var(--brand-primary)] p-1.5 rounded hover:bg-[var(--brand-primary-light)]/20 transition-colors tooltip cursor-pointer"
            title={isFullscreen ? "Exit Fullscreen (Esc)" : "Fullscreen Results"}
          >
            {isFullscreen ? <Minimize2 className="w-4 h-4" /> : <Maximize2 className="w-4 h-4" />}
          </button>
          <button onClick={handleExportCSV} className="text-[var(--text-muted)] hover:text-[var(--brand-primary)] p-1.5 rounded hover:bg-[var(--brand-primary-light)]/20 transition-colors tooltip cursor-pointer" title="Export to CSV">
            <Download className="w-4 h-4" />
          </button>
        </div>
      </div>

      <div className="flex-1 overflow-hidden bg-[var(--bg-surface)] flex flex-col transition-colors duration-200">
        {!result || result.length === 0 ? (
          <div className="flex-1 flex flex-col items-center justify-center text-[var(--text-muted)] pointer-events-none select-none">
            <div className="p-4 rounded-3xl bg-[var(--bg-app)] mb-3 border border-[var(--border-muted)] shadow-[inset_0_2px_4px_rgba(0,0,0,0.02)]">
              <Terminal className="w-10 h-10 opacity-60 text-[var(--text-muted)]" />
            </div>
            <p className="font-medium text-[var(--text-muted)] tracking-wide text-sm">Execute a query to view results</p>
          </div>
        ) : (
          <div className="flex-1 w-full h-full relative overflow-auto custom-scrollbar">
            {activeTab === 'results' && (
              <div className="w-full space-y-8 p-4">
                {result
                  .filter((res: any) => res.headers && res.headers.length > 0)
                  .map((res: any, idx: number, arr: any[]) => (
                    <div key={idx} className="space-y-2 last:pb-8 animate-in fade-in slide-in-from-bottom-2 duration-300">
                      {arr.length > 1 && (
                      <div className="flex items-center space-x-2 text-xs font-bold text-[var(--text-muted)] tracking-widest pl-1 uppercase">
                        <div className="w-4 h-[1px] bg-[var(--border-muted)]"></div>
                        <span>Result Set {idx + 1}</span>
                        {res.ConceptName && <span className="text-[var(--brand-primary)] ml-2">({res.ConceptName})</span>}
                        <div className="flex-1 h-[1px] bg-[var(--border-muted)]"></div>
                      </div>
                    )}
                    
                    {res.ConceptName && res.ConceptName.startsWith('Describe_') ? (
                      /* Vertical Key-Value View for DESCRIBE */
                      <div className="max-w-4xl mx-auto space-y-4 font-sans">
                        <div className="grid grid-cols-1 divide-y divide-[var(--border-muted)] border border-[var(--border-subtle)] rounded-lg shadow-sm overflow-hidden bg-[var(--bg-surface)]">
                          {res.headers?.map((h: string, i: number) => {
                            const value = res.rows?.[0]?.[h];
                            return (
                              <div key={i} className="flex flex-col sm:flex-row group hover:bg-[var(--bg-surface-alt)]/50 transition-colors">
                                <div className="sm:w-1/3 bg-[var(--bg-app)] px-4 py-3 text-[11px] font-bold text-[var(--text-muted)] tracking-wider group-hover:bg-[var(--bg-app)]/80 transition-colors self-start sm:border-r border-[var(--border-muted)] uppercase">
                                  {h}
                                </div>
                                <div className="flex-1 px-4 py-3 text-sm text-[var(--text-main)] whitespace-pre-wrap leading-relaxed min-h-[44px] font-thin">
                                  {value !== null && value !== undefined ?
                                    <span className={typeof value === 'number' ? 'text-blue-500 font-mono font-medium' : ''}>{String(value)}</span>
                                    : <span className="text-[var(--text-muted)] italic font-mono text-[11px] opacity-50">NULL</span>
                                  }
                                </div>
                              </div>
                            );
                          })}
                        </div>
                      </div>
                    ) : (
                      /* Normal Table View */
                      <PaginatedTable res={res} />
                    )}
                  </div>
                ))}
              </div>
            )}

            {activeTab === 'messages' && (
              <div className="w-full h-full overflow-auto p-5 bg-[var(--bg-surface)] font-mono text-sm text-[var(--text-main)] leading-relaxed custom-scrollbar transition-colors">
                <div className="mb-4 text-[var(--text-muted)] font-medium">
                  <span className="text-[var(--brand-primary)]">[{new Date().toLocaleTimeString()}]</span> Batch execution details:
                </div>

                {result.flatMap((res: any) => (
                    res.messages || []
                )).map((m: any, i: number) => {
                    const isError = typeof m === 'string' ? m.includes('Error') : (m?.type === 'error' || m?.type === 'Error');
                    const msgText = typeof m === 'string' ? m : (m?.text || JSON.stringify(m));
                    const locationMatch = msgText.match(/at line (\d+), col (\d+)/i);

                    return (
                      <div key={i} className={`flex items-start mb-2 group p-2 rounded-md transition-colors ${isError ? 'bg-red-500/10' : 'hover:bg-[var(--bg-surface-alt)]'}`}>
                        {isError ?
                          <Info className="w-4 h-4 text-red-500 mt-[3px] mr-3 shrink-0" /> :
                          <CheckCircle className="w-4 h-4 text-[var(--brand-primary)] mt-[3px] mr-3 shrink-0" />
                        }
                        <div className="flex flex-col">
                          <span className={`whitespace-pre-wrap ${isError ? 'text-red-500 font-semibold' : 'text-[var(--text-main)]'} font-thin`}>
                            {msgText}
                          </span>
                          {isError && locationMatch && (
                            <span className="text-[10px] text-red-500/70 font-bold uppercase mt-0.5 tracking-tighter">
                              Location Detected: Line {locationMatch[1]}, Column {locationMatch[2]}
                            </span>
                          )}
                        </div>
                      </div>
                    );
                })}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );

  // Fullscreen mode - overlay on top of everything (check BEFORE returning content)
  if (isFullscreen) {
    return (
      <div className="fixed inset-0 z-50 bg-[var(--bg-app)] flex flex-col">
        <div className={`flex items-center justify-between px-4 py-2 bg-[var(--bg-surface)] border-b border-[var(--border-subtle)] ${navigator.userAgent.indexOf('Mac') > -1 ? 'pl-28' : ''}`}>
          <div className="flex items-center space-x-2">
            <LayoutGrid className="w-4 h-4 text-[var(--brand-primary)]" />
            <span className="font-semibold text-[var(--text-main)]">Results - Fullscreen Mode</span>
          </div>
          <button
            onClick={() => setIsFullscreen(false)}
            className="flex items-center space-x-1 px-3 py-1.5 rounded bg-[var(--bg-app)] hover:bg-[var(--bg-surface-alt)] text-[var(--text-sub)] hover:text-[var(--text-main)] transition-colors cursor-pointer"
          >
            <Minimize2 className="w-4 h-4" />
            <span className="text-sm">Exit (Esc)</span>
          </button>
        </div>
        <div className="flex-1 overflow-hidden">
          {content}
        </div>
      </div>
    );
  }

  return content;
}
