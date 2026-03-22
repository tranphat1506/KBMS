import { Terminal, Table2, Info, Download, LayoutGrid, CheckCircle } from 'lucide-react';
import { useKbmsStore } from '../store/kbmsStore';

export default function ResultsPane() {
  const { result, activeTab, setActiveTab } = useKbmsStore();

  const handleExportCSV = () => {
    if (!result || !result.rows || result.rows.length === 0) return;
    const headers = result.headers.join(',');
    const rows = result.rows.map((row: any) =>
      result.headers.map((h: string) => {
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
        {!result ? (
          <div className="flex-1 flex flex-col items-center justify-center text-slate-300 pointer-events-none select-none">
            <div className="p-4 rounded-3xl bg-slate-50 mb-3 border border-slate-100 shadow-[inset_0_2px_4px_rgba(0,0,0,0.02)]">
              <Terminal className="w-10 h-10 opacity-60 text-slate-400" />
            </div>
            <p className="font-medium text-slate-400 tracking-wide text-sm">Execute a query to view results</p>
          </div>
        ) : (
          <div className="flex-1 w-full h-full relative">
            {activeTab === 'results' && (
              <div className="w-full h-full overflow-auto custom-scrollbar bg-white">
                {result.rows && result.rows.length > 0 ? (
                  result.ConceptName && result.ConceptName.startsWith('Describe_') ? (
                    /* Vertical Key-Value View for DESCRIBE */
                    <div className="p-6 max-w-4xl mx-auto space-y-4 font-sans animate-in fade-in slide-in-from-bottom-4 duration-300">
                      <div className="flex items-center space-x-2 pb-3 border-b border-slate-100">
                        <div className="p-1.5 bg-emerald-50 text-emerald-600 rounded">
                          <Info className="w-5 h-5 text-emerald-500" />
                        </div>
                        <h3 className="text-lg font-bold text-slate-800 tracking-tight">
                          {result.ConceptName?.replace('Describe_', '') || 'Object'} Details
                        </h3>
                      </div>
                      <div className="grid grid-cols-1 divide-y divide-slate-50 border border-slate-100 rounded-lg shadow-sm overflow-hidden bg-white mt-4">
                        {result.headers?.map((h: string, i: number) => {
                          const value = result.rows?.[0]?.[h];
                          return (
                            <div key={i} className="flex flex-col sm:flex-row group hover:bg-slate-50/50 transition-colors">
                              <div className="sm:w-1/3 bg-[#fcfdfd] px-4 py-3 text-[11px] font-bold text-slate-500 uppercase tracking-wider group-hover:bg-slate-50 transition-colors self-start sm:border-r border-slate-50">
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
                    <table className="w-full text-left border-collapse min-w-max">
                      <thead className="bg-[#fcfdfd] sticky top-0 z-10 shadow-[0_1px_2px_rgba(0,0,0,0.05)] select-none">
                        <tr>
                          {/* Row Counter Header */}
                          <th className="px-2 py-1.5 border-b border-r border-slate-200 bg-[#f4f7f9] w-12 text-center text-slate-400 font-medium">#</th>

                          {result.headers?.map((h: string, i: number) => (
                            <th key={i} className="px-4 py-2 font-bold text-slate-700 border-b border-r border-slate-200 last:border-r-0 whitespace-nowrap uppercase text-[11px] tracking-wider">
                              {h}
                            </th>
                          ))}
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-100">
                        {result.rows?.map((row: Record<string, any>, rIdx: number) => (
                          <tr key={rIdx} className="hover:bg-emerald-50/40 border-b border-slate-100 last:border-b-0 group transition-colors">
                            {/* Row Counter Node */}
                            <td className="px-2 py-1.5 border-r border-slate-100 bg-[#fafbfc] w-12 text-center text-[11px] font-mono text-slate-400 select-none">
                              {rIdx + 1}
                            </td>

                            {result.headers?.map((h: string, cIdx: number) => (
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
                        ))}
                      </tbody>
                    </table>
                  )
                ) : (
                  <div className="flex flex-col w-full h-full items-center justify-center text-slate-400 space-y-2 select-none pointer-events-none">
                    <Table2 className="w-6 h-6 opacity-40" />
                    <p className="font-medium text-[13px]">No tabular data to display</p>
                  </div>
                )}
              </div>
            )}

            {activeTab === 'messages' && (
              <div className="w-full h-full overflow-auto p-5 bg-white font-mono text-[12px] text-slate-700 leading-relaxed custom-scrollbar">
                <div className="mb-4 text-slate-400 font-medium">
                  <span className="text-emerald-600">[{new Date().toLocaleTimeString()}]</span> Query execution started
                </div>

                {result.messages && result.messages.map((m: any, i: number) => {
                  const msgText = typeof m === 'string' ? m : (m?.text || JSON.stringify(m));
                  const isError = typeof m === 'string' ? m.includes('Error') : (m?.type === 'error' || m?.type === 'Error');

                  return (
                    <div key={i} className="flex items-start mb-2 group">
                      {isError ?
                        <Info className="w-4 h-4 text-red-500 mt-[3px] mr-3 shrink-0" /> :
                        <CheckCircle className="w-4 h-4 text-emerald-500 mt-[3px] mr-3 shrink-0" />
                      }
                      <span className={`whitespace-pre-wrap ${isError ? 'text-red-600 font-semibold' : 'text-slate-700'}`}>
                        {msgText}
                      </span>
                    </div>
                  );
                })}

                {result.executionTimeMs !== undefined && (
                  <div className="mt-5 pt-3 border-t border-dashed border-slate-200 text-slate-500">
                    Query executed successfully in <span className="font-semibold text-slate-700">{result.executionTimeMs.toFixed(2)} ms</span>
                  </div>
                )}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
