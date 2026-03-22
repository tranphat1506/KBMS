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
    <div className="border border-slate-200 rounded-lg flex flex-col shadow-sm bg-white min-w-0">
      <div className="overflow-x-auto min-h-[50px] custom-scrollbar bg-slate-50/50 relative">
        <table className="w-full text-left border-collapse min-w-max">
          <thead className="bg-[#fcfdfd] sticky top-0 z-10 shadow-[0_1px_2px_rgba(0,0,0,0.05)] select-none">
            <tr>
              <th className="px-2 py-1.5 border-b border-r border-slate-200 bg-[#f4f7f9] w-12 text-center text-slate-400 font-medium font-mono">#</th>
              {res.headers?.map((h: string, i: number) => (
                <th key={i} className="px-4 py-2 font-bold text-slate-700 border-b border-r border-slate-200 last:border-r-0 whitespace-nowrap text-[11px] tracking-wider">
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {currentRows.length > 0 ? (
              currentRows.map((row: Record<string, any>, rIdx: number) => {
                const absoluteIdx = startIndex + rIdx;
                return (
                  <tr key={absoluteIdx} className="hover:bg-emerald-50/40 border-b border-slate-100 last:border-b-0 group transition-colors">
                    <td className="px-2 py-1.5 border-r border-slate-100 bg-[#fafbfc] w-12 text-center text-[11px] font-mono text-slate-400 select-none">
                      {absoluteIdx + 1}
                    </td>
                    {res.headers?.map((h: string, cIdx: number) => (
                      <td key={cIdx} className="px-4 py-1.5 text-slate-600 border-r border-slate-50 last:border-r-0 whitespace-nowrap group-hover:text-slate-900 focus-within:bg-emerald-50 focus-within:outline-none" tabIndex={0}>
                        <div className="max-h-24 overflow-auto custom-scrollbar whitespace-pre-wrap leading-normal">
                          {row?.[h] !== null && row?.[h] !== undefined ?
                            <span className={typeof row?.[h] === 'number' ? 'text-blue-600 font-mono font-medium' : ''}>{String(row?.[h])}</span>
                            : <span className="text-slate-300 italic font-mono text-[11px]">NULL</span>
                          }
                        </div>
                      </td>
                    ))}
                  </tr>
                );
              })
            ) : (
              <tr>
                <td colSpan={(res.headers?.length || 0) + 1} className="px-4 py-6 text-center text-slate-400 italic bg-white">
                  No rows returned for this result set.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {totalRows > rowsPerPage && (
        <div className="flex items-center justify-between px-4 py-2 border-t border-slate-100 bg-slate-50/80 rounded-b-lg">
          <div className="text-[11px] text-slate-500 font-medium select-none">
            Showing <span className="text-slate-700">{startIndex + 1}</span> to <span className="text-slate-700">{Math.min(startIndex + rowsPerPage, totalRows)}</span> of <span className="text-slate-700 font-bold">{totalRows}</span> rows
          </div>
          <div className="flex items-center space-x-1">
            <button 
              onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
              disabled={validPage === 1}
              className="flex items-center justify-center p-1 text-slate-500 hover:text-emerald-600 hover:bg-emerald-50 rounded disabled:opacity-30 disabled:hover:bg-transparent disabled:hover:text-slate-500 disabled:cursor-not-allowed transition-colors cursor-pointer"
              title="Previous Page"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
            <div className="flex items-center px-2 py-0.5 rounded bg-white border border-slate-200 text-[11px] font-medium text-slate-600 shadow-sm select-none">
              <span className="text-emerald-600 font-bold mr-1">{validPage}</span> / <span className="ml-1">{totalPages}</span>
            </div>
            <button 
              onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
              disabled={validPage === totalPages}
              className="flex items-center justify-center p-1 text-slate-500 hover:text-emerald-600 hover:bg-emerald-50 rounded disabled:opacity-30 disabled:hover:bg-transparent disabled:hover:text-slate-500 disabled:cursor-not-allowed transition-colors cursor-pointer"
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
    <div className="h-full w-full bg-white flex flex-col font-sans relative">
      <div className="flex items-center px-4 h-10 bg-[#f8fafc] border-b border-slate-200 justify-between select-none">
        <div className="flex items-center space-x-1 relative h-full">
          <button
            onClick={() => setActiveTab('results')}
            className={`flex items-center space-x-1.5 px-3 h-full text-[13px] font-semibold tracking-wide transition-all relative group cursor-pointer ${activeTab === 'results' ? 'text-emerald-700 bg-white' : 'text-slate-500 hover:text-slate-700 hover:bg-slate-200/50'
              }`}
          >
            <LayoutGrid className="w-3.5 h-3.5" />
            <span>Results</span>
            <div className={`absolute bottom-0 left-0 right-0 h-0.5 rounded-t-sm transition-all ${activeTab === 'results' ? 'bg-emerald-500' : 'bg-transparent group-hover:bg-slate-300'}`} />
          </button>

          <button
            onClick={() => setActiveTab('messages')}
            className={`flex items-center space-x-1.5 px-3 h-full text-[13px] font-semibold tracking-wide transition-all relative group cursor-pointer ${activeTab === 'messages' ? 'text-emerald-700 bg-white' : 'text-slate-500 hover:text-slate-700 hover:bg-slate-200/50'
              }`}
          >
            <Terminal className="w-3.5 h-3.5" />
            <span>Messages</span>
            <div className={`absolute bottom-0 left-0 right-0 h-0.5 rounded-t-sm transition-all ${activeTab === 'messages' ? 'bg-emerald-500' : 'bg-transparent group-hover:bg-slate-300'}`} />
          </button>
        </div>

        <div className="flex items-center space-x-2">
          <button onClick={handleExportCSV} className="text-slate-400 hover:text-emerald-600 p-1.5 rounded hover:bg-emerald-50 transition-colors tooltip cursor-pointer" title="Export to CSV">
            <Download className="w-4 h-4" />
          </button>
        </div>
      </div>

      <div className="flex-1 overflow-hidden bg-white flex flex-col text-[13px]">
        {!result || result.length === 0 ? (
          <div className="flex-1 flex flex-col items-center justify-center text-slate-300 pointer-events-none select-none">
            <div className="p-4 rounded-3xl bg-slate-50 mb-3 border border-slate-100 shadow-[inset_0_2px_4px_rgba(0,0,0,0.02)]">
              <Terminal className="w-10 h-10 opacity-60 text-slate-400" />
            </div>
            <p className="font-medium text-slate-400 tracking-wide text-sm">Execute a query to view results</p>
          </div>
        ) : (
          <div className="flex-1 w-full h-full relative overflow-auto custom-scrollbar">
            {activeTab === 'results' && (
              <div className="w-full space-y-8 p-4">
                {result.map((res: any, idx: number) => (
                  <div key={idx} className="space-y-2 last:pb-8 animate-in fade-in slide-in-from-bottom-2 duration-300">
                    {result.length > 1 && (
                      <div className="flex items-center space-x-2 text-[11px] font-bold text-slate-400 tracking-widest pl-1">
                        <div className="w-4 h-[1px] bg-slate-200"></div>
                        <span>Result Set {idx + 1}</span>
                        {res.ConceptName && <span className="text-emerald-500 ml-2">({res.ConceptName})</span>}
                        <div className="flex-1 h-[1px] bg-slate-200"></div>
                      </div>
                    )}
                    
                    {res.headers && res.headers.length > 0 ? (
                      res.ConceptName && res.ConceptName.startsWith('Describe_') ? (
                        /* Vertical Key-Value View for DESCRIBE */
                        <div className="max-w-4xl mx-auto space-y-4 font-sans">
                          {/* ... existing Describe view code adjusted to use 'res' ... */}
                          <div className="grid grid-cols-1 divide-y divide-slate-50 border border-slate-100 rounded-lg shadow-sm overflow-hidden bg-white">
                            {res.headers?.map((h: string, i: number) => {
                              const value = res.rows?.[0]?.[h];
                              return (
                                <div key={i} className="flex flex-col sm:flex-row group hover:bg-slate-50/50 transition-colors">
                                  <div className="sm:w-1/3 bg-[#fcfdfd] px-4 py-3 text-[11px] font-bold text-slate-500 tracking-wider group-hover:bg-slate-50 transition-colors self-start sm:border-r border-slate-50">
                                    {h}
                                  </div>
                                  <div className="flex-1 px-4 py-3 text-[13px] text-slate-700 whitespace-pre-wrap leading-relaxed min-h-[44px]">
                                    {value !== null && value !== undefined ?
                                      <span className={typeof value === 'number' ? 'text-blue-600 font-mono font-medium' : ''}>{String(value)}</span>
                                      : <span className="text-slate-300 italic font-mono text-[11px]">NULL</span>
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
                      <div className="p-4 bg-slate-50/50 rounded-lg border border-slate-100 text-slate-500 italic">
                        {res.messages?.length > 0 ? res.messages[0].text : 'Command executed successfully (no data).'}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}

            {activeTab === 'messages' && (
              <div className="w-full h-full overflow-auto p-5 bg-white font-mono text-[12px] text-slate-700 leading-relaxed custom-scrollbar">
                <div className="mb-4 text-slate-400 font-medium">
                  <span className="text-emerald-600">[{new Date().toLocaleTimeString()}]</span> Batch execution details:
                </div>

                {result.flatMap((res: any) => (
                    res.messages || []
                )).map((m: any, i: number) => {
                    const isError = typeof m === 'string' ? m.includes('Error') : (m?.type === 'error' || m?.type === 'Error');
                    const msgText = typeof m === 'string' ? m : (m?.text || JSON.stringify(m));
                    const locationMatch = msgText.match(/at line (\d+), col (\d+)/i);

                    return (
                      <div key={i} className={`flex items-start mb-2 group p-2 rounded-md transition-colors ${isError ? 'bg-red-50/30' : ''}`}>
                        {isError ?
                          <Info className="w-4 h-4 text-red-500 mt-[3px] mr-3 shrink-0" /> :
                          <CheckCircle className="w-4 h-4 text-emerald-500 mt-[3px] mr-3 shrink-0" />
                        }
                        <div className="flex flex-col">
                          <span className={`whitespace-pre-wrap ${isError ? 'text-red-600 font-semibold' : 'text-slate-700'}`}>
                            {msgText}
                          </span>
                          {isError && locationMatch && (
                            <span className="text-[10px] text-red-400 font-bold uppercase mt-0.5 tracking-tighter">
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
