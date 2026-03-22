import { useEffect } from 'react';
import Editor, { useMonaco } from '@monaco-editor/react';
import { useKbmsStore } from '../store/kbmsStore';
import { FileCode, X, PlayCircle, Settings2, Plus, Save, SquareTerminal } from 'lucide-react';

export default function EditorPane() {
  const tabs = useKbmsStore(state => state.tabs);
  const activeTabId = useKbmsStore(state => state.activeTabId);
  const addTab = useKbmsStore(state => state.addTab);
  const removeTab = useKbmsStore(state => state.removeTab);
  const setActiveTabId = useKbmsStore(state => state.setActiveTabId);
  const saveTab = useKbmsStore(state => state.saveTab);
  const openTab = useKbmsStore(state => state.openTab);

  const activeTab = tabs.find(t => t.id === activeTabId);
  const query = activeTab ? activeTab.query : '';

  const setQuery = useKbmsStore(state => state.setQuery);
  const execute = useKbmsStore(state => state.execute);
  const stopExecution = useKbmsStore(state => state.stopExecution);
  const metadata = useKbmsStore(state => state.metadata);
  const editorMarkers = useKbmsStore(state => state.editorMarkers);
  const monaco = useMonaco();
  
  const isMac = navigator.userAgent.indexOf('Mac') > -1;
  const cmd = isMac ? '⌘' : 'Ctrl';
  const alt = isMac ? '⌥' : 'Alt';

  // Keyboard Shortcuts Listener
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      const isCmdOrCtrl = isMac ? e.metaKey : e.ctrlKey;
      
      // Execute: Alt + Enter
      if (e.altKey && e.key === 'Enter') {
        e.preventDefault();
        execute();
      }
      // Stop: Alt + Space
      if (e.altKey && e.key === ' ') {
        e.preventDefault();
        stopExecution();
      }
      // Save: Cmd + S
      if (isCmdOrCtrl && e.key.toLowerCase() === 's') {
        e.preventDefault();
        const { tabs, activeTabId } = useKbmsStore.getState();
        if (tabs.length > 0) saveTab(activeTabId);
      }
      // Open: Cmd + O
      if (isCmdOrCtrl && e.key.toLowerCase() === 'o') {
        e.preventDefault();
        openTab();
      }
      // New: Cmd + N
      if (isCmdOrCtrl && e.key.toLowerCase() === 'n') {
        e.preventDefault();
        addTab();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [execute, stopExecution, saveTab, openTab, addTab, isMac]);

  // Apply server error markers (red squiggles) to the Monaco editor
  useEffect(() => {
    if (!monaco) return;
    const editors = monaco.editor.getEditors();
    if (editors.length === 0) return;
    const model = editors[0].getModel();
    if (!model) return;

    if (editorMarkers && editorMarkers.length > 0) {
      monaco.editor.setModelMarkers(model, 'kbms-server', editorMarkers);
    } else {
      monaco.editor.setModelMarkers(model, 'kbms-server', []);
    }
  }, [monaco, editorMarkers]);
  useEffect(() => {
    if (monaco) {
      const kbmsKeywords = [
        'CREATE', 'CONCEPT', 'RULE', 'KB', 'VARIABLES', 'ALIASES', 'BASE', 'HYPOTHESIS', 'CONCLUSION', 'COST',
        'SELECT', 'INSERT', 'INTO', 'VALUES', 'UPDATE', 'SET', 'DELETE', 'FROM', 'WHERE', 'AND', 'OR', 'SHOW', 'USE', 'SOLVE',
        'HIERARCHY', 'IS_A', 'PART_OF', 'ADD', 'REMOVE', 'DESCRIBE', 'EXPLAIN'
      ];
      const completionProvider = monaco.languages.registerCompletionItemProvider('sql', {
        provideCompletionItems: (_model, _position) => {
          const suggestions: any[] = [];
          kbmsKeywords.forEach(kw => {
             suggestions.push({
                label: kw, kind: monaco.languages.CompletionItemKind.Keyword, insertText: kw + ' ', range: null as any
             });
          });
          metadata.concepts.forEach(c => {
            suggestions.push({
              label: c.Name, kind: monaco.languages.CompletionItemKind.Class, insertText: c.Name, detail: 'Concept (Table)', range: null as any
            });
            if (c.Variables) {
              String(c.Variables).split(',').forEach(v => {
                const vName = v.trim();
                suggestions.push({
                  label: vName, kind: monaco.languages.CompletionItemKind.Field, insertText: vName, detail: `Variable in ${c.Name}`, range: null as any
                });
              });
            }
          });

          metadata.rules.forEach(r => {
            const name = r.Name || r.Id;
            suggestions.push({
              label: name, kind: monaco.languages.CompletionItemKind.Function, insertText: name, detail: 'Rule', range: null as any
            });
          });

          metadata.hierarchies.forEach(h => {
            const label = `${h.Parent}:${h.Child}`;
            suggestions.push({
              label: label, kind: monaco.languages.CompletionItemKind.Struct, insertText: label, detail: 'Hierarchy', range: null as any
            });
          });

          metadata.functions.forEach(f => {
            suggestions.push({
              label: f.Name, kind: monaco.languages.CompletionItemKind.Method, insertText: `${f.Name}(`, detail: 'Function', range: null as any
            });
          });

          metadata.operators.forEach(op => {
            suggestions.push({
              label: op.Symbol, kind: monaco.languages.CompletionItemKind.Operator, insertText: op.Symbol, detail: 'Operator', range: null as any
            });
          });

          return { suggestions };
        }
      });
      return () => completionProvider.dispose();
    }
  }, [monaco, metadata]);

  const handleFormat = () => {
     if (monaco) {
        const editorInstance = monaco.editor.getEditors()[0];
        if (editorInstance) {
            editorInstance.getAction('editor.action.formatDocument')?.run();
        }
     }
  };

  // Handle Empty State
  if (tabs.length === 0) {
      return (
         <div className="h-full w-full flex flex-col items-center justify-center bg-[#f8fafc] font-sans">
            <div className="flex flex-col items-center opacity-70 pointer-events-none select-none">
               <SquareTerminal className="w-12 h-12 text-slate-400 mb-3" />
               <h2 className="text-lg text-slate-600 font-normal">KBMS Studio Editor</h2>
               <p className="text-slate-400 text-[12px] mt-1">Press {cmd} + N or click New Query to begin</p>
            </div>
            <button 
               onClick={addTab}
               title={`New Query (${cmd} + N)`}
               className="mt-5 px-4 py-2 bg-white border border-slate-200 hover:border-emerald-500 hover:text-emerald-700 text-slate-600 text-[12px] font-normal rounded shadow-sm transition-all focus:outline-none flex items-center space-x-2 cursor-pointer"
            >
               <Plus className="w-3.5 h-3.5" />
               <span>New Query</span>
            </button>
         </div>
      );
  }

  return (
    <div className="h-full w-full flex flex-col relative bg-white font-sans">
      {/* Editor Tab Bar */}
      <div className="flex bg-[#f1f5f9] pr-3 h-[36px] select-none border-b border-slate-200/80 shadow-[inset_0_-1px_0_rgba(0,0,0,0.02)] overflow-x-auto custom-scrollbar items-end">
        <div className="flex h-[35px]">
          {tabs.map((tab) => (
            <div 
              key={tab.id}
              onClick={() => setActiveTabId(tab.id)}
              className={`px-3 flex items-center justify-between min-w-[140px] max-w-[180px] group cursor-pointer border-r border-slate-200 relative transition-colors ${
                 activeTabId === tab.id 
                 ? 'bg-white border-t-[2px] border-t-emerald-500 shadow-[0_-2px_6px_rgba(0,0,0,0.02)] z-10 text-slate-800' 
                 : 'bg-transparent border-t-[2px] border-t-transparent hover:bg-slate-200/50 text-slate-500 hover:text-slate-700'
              }`}
            >
              <div className="flex items-center space-x-2 truncate">
                <FileCode className={`w-3.5 h-3.5 shrink-0 ${activeTabId === tab.id ? 'text-emerald-600' : 'text-slate-400'}`} />
                <span title={tab.filePath || tab.name} className={`text-[12px] tracking-wide truncate ${activeTabId === tab.id ? 'font-medium' : 'font-normal'}`}>
                   {tab.name}{!tab.isSaved ? <span className="text-amber-500 ml-0.5">*</span> : ''}
                </span>
              </div>
              <button 
                title="Close Tab"
                onClick={(e) => { e.stopPropagation(); removeTab(tab.id); }}
                className={`p-0.5 ml-2 rounded transition-all shrink-0 hover:text-red-500 cursor-pointer ${activeTabId === tab.id ? 'opacity-100 hover:bg-slate-100' : 'opacity-0 group-hover:opacity-100 hover:bg-slate-300/50'}`}
              >
                 <X className="w-3.5 h-3.5" />
              </button>
            </div>
          ))}
        </div>
        
        <button 
          title={`New Query Tab (${cmd} + N)`}
          onClick={addTab}
          className="h-6 w-6 ml-1.5 mb-1 flex items-center justify-center rounded hover:bg-slate-200/80 text-slate-500 hover:text-slate-700 transition-colors shrink-0 cursor-pointer"
        >
           <Plus className="w-3.5 h-3.5" />
        </button>
      </div>
      
      {/* Inline Editor Toolbar */}
      <div className="h-8 bg-white border-b border-slate-100 flex items-center px-3 justify-between space-x-3 text-[11px] text-slate-500 font-normal select-none shadow-[0_1px_2px_rgba(0,0,0,0.01)] z-10">
         <div className="flex items-center space-x-3">
           <button title={`Execute (${alt} + Enter)`} onClick={() => execute()} className="flex items-center hover:text-emerald-700 hover:bg-emerald-50 px-1.5 py-1 rounded space-x-1 cursor-pointer transition-colors group">
              <PlayCircle className="w-3.5 h-3.5 text-emerald-500 group-hover:text-emerald-600" />
              <span className="font-medium text-emerald-700">Execute Session</span>
           </button>
           <span className="text-slate-200">|</span>
           <button title={`Save (${cmd} + S)`} onClick={() => saveTab(activeTabId)} className="flex items-center hover:text-blue-700 hover:bg-blue-50 px-1.5 py-1 rounded space-x-1 cursor-pointer transition-colors">
              <Save className="w-3.5 h-3.5 text-slate-400" />
              <span>Save</span>
           </button>
           <button title={`Format Document (Shift + ${alt} + F)`} onClick={handleFormat} className="flex items-center px-1.5 py-1 hover:text-slate-800 transition-colors rounded hover:bg-slate-100 cursor-pointer">
              <span>Format Document</span>
           </button>
         </div>
         <div className="flex items-center">
            <button className="hover:bg-slate-100 p-1 rounded transition-colors cursor-pointer" title="Editor Settings">
              <Settings2 className="w-3.5 h-3.5 text-slate-400" />
            </button>
         </div>
      </div>

      <div className="flex-1 w-full relative">
        <Editor
          height="100%"
          defaultLanguage="sql"
          theme="light"
          options={{
            minimap: { enabled: false },
            fontSize: 13,
            fontFamily: "'JetBrains Mono', 'Fira Code', 'Courier New', monospace",
            lineHeight: 22,
            lineNumbers: 'on',
            scrollBeyondLastLine: false,
            wordWrap: 'on',
            padding: { top: 12, bottom: 12 },
            renderLineHighlight: 'line',
            cursorBlinking: 'smooth',
            cursorSmoothCaretAnimation: 'on',
            fontLigatures: true,
            formatOnPaste: true,
            suggestSelection: 'first',
          }}
          value={query}
          onChange={(val) => setQuery(val || '')}
        />
      </div>
    </div>
  );
}
