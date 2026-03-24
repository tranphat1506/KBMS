import { useState } from 'react';
import { Terminal, Info, Download, LayoutGrid, CheckCircle, ChevronLeft, ChevronRight } from 'lucide-react';
import { useKbmsStore } from '../store/kbmsStore';

const PaginatedTable = ({ res }: { res: any }) => {
  const [currentPage, setCurrentPage] = useState(1);
  const rowsPerPage = 100;

  const totalRows = res.rows?.length || 0;
  const totalPages = Math.ceil(totalRows / rowsPerPage) || 1;

  // Ensure current page is valid when total rows change
  const validPage = Math.min(currentPage, totalPages);
  
  const startIndex = (validPage - 1) * rowsPerPage;
  const currentRows = res.rows?.slice(startIndex, startIndex + rowsPerPage) || [];

  return (
    <div className="border border-[var(--border-subtle)] rounded-lg flex flex-col shadow-sm bg-[var(--bg-surface)] min-w-0 transition-colors">
      <div className="overflow-x-auto min-h-[50px] custom-scrollbar bg-[var(--bg-app)]/50 relative">
        <table className="w-full text-left border-collapse min-w-max">
          <thead className="bg-[var(--bg-surface)] sticky top-0 z-10 shadow-[0_1px_2px_rgba(0,0,0,0.05)] select-none">
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
                    <td className="px-2 py-1.5 border-r border-[var(--border-muted)] bg-[var(--bg-app)]/30 w-12 text-center text-[11px] font-mono text-[var(--text-muted)] select-none">
                      {absoluteIdx + 1}
                    </td>
                    {res.headers?.map((h: string, cIdx: number) => (
                      <td key={cIdx} className="px-4 py-1.5 text-[var(--text-sub)] border-r border-[var(--border-muted)]/30 last:border-r-0 whitespace-nowrap group-hover:text-[var(--text-main)] focus-within:bg-[var(--brand-primary-light)]/20 focus-within:outline-none" tabIndex={0}>
                        <div className="max-h-24 overflow-auto custom-scrollbar whitespace-pre-wrap leading-normal font-thin">
                          {row?.[h] !== null && row?.[h] !== undefined ?
                            <span className={typeof row?.[h] === 'number' ? 'text-blue-500 font-mono font-medium' : ''}>{String(row?.[h])}</span>
                            : <span className="text-[var(--text-muted)] italic font-mono text-[11px] opacity-50">NULL</span>
                          }
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
  const { result, activeTab, setActiveTab } = useKbmsStore();

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

  return (
    <div className="h-full w-full bg-[var(--bg-surface)] flex flex-col font-sans relative transition-colors duration-200">
      <div className="flex items-center px-4 h-10 bg-[var(--bg-app)] border-b border-[var(--border-subtle)] justify-between select-none">
        <div className="flex items-center space-x-1 relative h-full">
          <button
            onClick={() => setActiveTab('results')}
            className={`flex items-center space-x-1.5 px-3 h-full text-[13px] font-semibold tracking-wide transition-all relative group cursor-pointer ${activeTab === 'results' ? 'text-[var(--brand-primary)] bg-[var(--bg-surface)]' : 'text-[var(--text-sub)] hover:text-[var(--text-main)] hover:bg-[var(--bg-surface-alt)]/50'
              }`}
          >
            <LayoutGrid className="w-3.5 h-3.5" />
            <span>Results</span>
            <div className={`absolute bottom-0 left-0 right-0 h-0.5 rounded-t-sm transition-all ${activeTab === 'results' ? 'bg-[var(--brand-primary)]' : 'bg-transparent group-hover:bg-[var(--border-subtle)]'}`} />
          </button>

          <button
            onClick={() => setActiveTab('messages')}
            className={`flex items-center space-x-1.5 px-3 h-full text-[13px] font-semibold tracking-wide transition-all relative group cursor-pointer ${activeTab === 'messages' ? 'text-[var(--brand-primary)] bg-[var(--bg-surface)]' : 'text-[var(--text-sub)] hover:text-[var(--text-main)] hover:bg-[var(--bg-surface-alt)]/50'
              }`}
          >
            <Terminal className="w-3.5 h-3.5" />
            <span>Messages</span>
            <div className={`absolute bottom-0 left-0 right-0 h-0.5 rounded-t-sm transition-all ${activeTab === 'messages' ? 'bg-[var(--brand-primary)]' : 'bg-transparent group-hover:bg-[var(--border-subtle)]'}`} />
          </button>
        </div>

        <div className="flex items-center space-x-2">
          <button onClick={handleExportCSV} className="text-[var(--text-muted)] hover:text-[var(--brand-primary)] p-1.5 rounded hover:bg-[var(--brand-primary-light)]/20 transition-colors tooltip cursor-pointer" title="Export to CSV">
            <Download className="w-4 h-4" />
          </button>
        </div>
      </div>

      <div className="flex-1 overflow-hidden bg-[var(--bg-surface)] flex flex-col text-[13px] transition-colors duration-200">
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
                {result.map((res: any, idx: number) => (
                  <div key={idx} className="space-y-2 last:pb-8 animate-in fade-in slide-in-from-bottom-2 duration-300">
                    {result.length > 1 && (
                      <div className="flex items-center space-x-2 text-[11px] font-bold text-[var(--text-muted)] tracking-widest pl-1 uppercase">
                        <div className="w-4 h-[1px] bg-[var(--border-muted)]"></div>
                        <span>Result Set {idx + 1}</span>
                        {res.ConceptName && <span className="text-[var(--brand-primary)] ml-2">({res.ConceptName})</span>}
                        <div className="flex-1 h-[1px] bg-[var(--border-muted)]"></div>
                      </div>
                    )}
                    
                    {res.headers && res.headers.length > 0 ? (
                      res.ConceptName && res.ConceptName.startsWith('Describe_') ? (
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
                                  <div className="flex-1 px-4 py-3 text-[13px] text-[var(--text-main)] whitespace-pre-wrap leading-relaxed min-h-[44px] font-thin">
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
                      )
                    ) : (
                      /* Message-only result set in Results tab */
                      <div className="p-4 bg-[var(--bg-app)]/50 rounded-lg border border-[var(--border-muted)] text-[var(--text-sub)] italic transition-colors">
                        {res.messages?.length > 0 ? res.messages[0].text : 'Command executed successfully (no data).'}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}

            {activeTab === 'messages' && (
              <div className="w-full h-full overflow-auto p-5 bg-[var(--bg-surface)] font-mono text-[12px] text-[var(--text-main)] leading-relaxed custom-scrollbar transition-colors">
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
}
