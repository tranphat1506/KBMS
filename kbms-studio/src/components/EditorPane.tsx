import { useEffect, useState } from 'react';
import Editor, { useMonaco } from '@monaco-editor/react';
import { useThingentStore } from '../store/thingentStore';
import type { QueryTab } from '../store/thingentStore';
import { FileCode, X, PlayCircle, Settings2, Plus, Save, SquareTerminal, Table, Zap, Code, Calculator, Info, Pin, Code2 } from 'lucide-react';
import ObjectDetailPane from './ObjectDetailPane';
import { getKbColorStyle } from '../utils/colors';
import TabContextMenu from './TabContextMenu';
import SnippetsPanel from './SnippetsPanel';
import OntologyBuilder from './OntologyBuilder';

export default function EditorPane() {
  const tabs = useThingentStore(state => state.tabs);
  const activeTabId = useThingentStore(state => state.activeTabId);
  const addTab = useThingentStore(state => state.addTab);
  const removeTab = useThingentStore(state => state.removeTab);
  const setActiveTabId = useThingentStore(state => state.setActiveTabId);
  const saveTab = useThingentStore(state => state.saveTab);
  const openTab = useThingentStore(state => state.openTab);
  const studioSettings = useThingentStore(state => state.studioSettings);
  const isSnippetsPanelOpen = useThingentStore(state => state.isSnippetsPanelOpen);
  const setSnippetsPanelOpen = useThingentStore(state => state.setSnippetsPanelOpen);
  
  const [contextMenu, setContextMenu] = useState<{ x: number, y: number, tab: QueryTab } | null>(null);

  const [isSystemDark, setIsSystemDark] = useState(
    window.matchMedia('(prefers-color-scheme: dark)').matches
  );

  useEffect(() => {
    if (studioSettings.theme !== 'device') return;
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    const handler = (e: MediaQueryListEvent) => setIsSystemDark(e.matches);
    mediaQuery.addEventListener('change', handler);
    return () => mediaQuery.removeEventListener('change', handler);
  }, [studioSettings.theme]);

  const effectiveTheme = studioSettings.theme === 'device' ? isSystemDark : studioSettings.theme === 'dark';
  const monacoTheme = effectiveTheme ? 'vs-dark' : 'light';

  const activeTab = tabs.find(t => t.id === activeTabId);
  const query = activeTab ? activeTab.query : '';

  const setQuery = useThingentStore(state => state.setQuery);
  const execute = useThingentStore(state => state.execute);
  const stopExecution = useThingentStore(state => state.stopExecution);
  const kbColorMap = useThingentStore(state => state.serverMetadata.kbColorMap);
  const editorMarkers = useThingentStore(state => state.editorMarkers);
  const monaco = useMonaco();
  
  const [editorInstance, setEditorInstance] = useState<any>(null);

  const handleEditorDidMount = (editor: any) => {
    setEditorInstance(editor);
  };
  
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
        let textToExecute = '';
        if (editorInstance) {
          const selection = editorInstance.getSelection();
          if (selection && !selection.isEmpty()) {
            textToExecute = editorInstance.getModel()?.getValueInRange(selection) || '';
          }
        }
        execute(textToExecute || undefined);
      }
      // Stop: Alt + Space
      if (e.altKey && e.key === ' ') {
        e.preventDefault();
        stopExecution();
      }
      // Save: Cmd + S
      if (isCmdOrCtrl && e.key.toLowerCase() === 's') {
        e.preventDefault();
        const { tabs, activeTabId } = useThingentStore.getState();
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
  }, [execute, stopExecution, saveTab, openTab, addTab, isMac, editorInstance]);

  // Apply server error markers (red squiggles) to the Monaco editor
  useEffect(() => {
    if (!monaco || !editorInstance) return;
    const model = editorInstance.getModel();
    if (!model) return;

    if (editorMarkers && editorMarkers.length > 0) {
      monaco.editor.setModelMarkers(model, 'thingent-server', editorMarkers);
    } else {
      monaco.editor.setModelMarkers(model, 'thingent-server', []);
    }
  }, [monaco, editorInstance, editorMarkers, activeTabId]);
  useEffect(() => {
    if (monaco) {
      const completionProvider = monaco.languages.registerCompletionItemProvider('sql', {
        triggerCharacters: [' ', '.', ':', '('],
        provideCompletionItems: async (model, position) => {
          const code = model.getValue();
          const kbName = useThingentStore.getState().selectedKb;
          
          try {
            const result = await useThingentStore.getState().fetchLspCompletions(code, position.lineNumber, position.column, kbName);
            if (!result || !Array.isArray(result.completions)) {
              return { suggestions: [] };
            }

            const suggestions = result.completions.map((c: any) => ({
              label: c.Label || c,
              kind: c.Kind === 'Keyword' ? monaco.languages.CompletionItemKind.Keyword :
                    c.Kind === 'Concept' ? monaco.languages.CompletionItemKind.Class :
                    c.Kind === 'Variable' ? monaco.languages.CompletionItemKind.Field :
                    c.Kind === 'Type' ? monaco.languages.CompletionItemKind.Struct :
                    monaco.languages.CompletionItemKind.Text,
              insertText: c.Label || c,
              detail: c.Detail || '',
              range: null as any
            }));

            return { suggestions };
          } catch (e) {
            console.error("LSP Autocomplete error", e);
            return { suggestions: [] };
          }
        }
      });
      return () => completionProvider.dispose();
    }
  }, [monaco]);

  const handleFormat = () => {
     if (monaco) {
        const editorInstance = monaco.editor.getEditors()[0];
        if (editorInstance) {
            editorInstance.getAction('editor.action.formatDocument')?.run();
        }
     }
  };

  const handleInsertSnippet = (code: string) => {
    if (monaco) {
      const editorInstance = monaco.editor.getEditors()[0];
      if (editorInstance) {
        const position = editorInstance.getPosition();
        if (position) {
          editorInstance.executeEdits('snippets', [{
            range: new monaco.Range(position.lineNumber, position.column, position.lineNumber, position.column),
            text: code + '\n',
            forceMoveMarkers: true
          }]);
          editorInstance.focus();
        }
      }
    }
  };

  // Handle Empty State
  if (tabs.length === 0) {
      return (
         <div className="h-full w-full flex flex-col items-center justify-center bg-[var(--bg-app)] font-sans transition-colors duration-200">
            <div className="flex flex-col items-center opacity-70 pointer-events-none select-none">
               <SquareTerminal className="w-12 h-12 text-[var(--text-muted)] mb-3" />
               <h2 className="text-lg text-[var(--text-main)] font-normal">Thingent Studio Editor</h2>
               <p className="text-[var(--text-muted)] text-[12px] mt-1">Press {cmd} + N or click New Query to begin</p>
            </div>
            <button 
               onClick={addTab}
               title={`New Query (${cmd} + N)`}
               className="mt-5 px-4 py-2 bg-[var(--bg-surface)] border border-[var(--border-subtle)] hover:border-[var(--brand-primary)] hover:text-[var(--brand-primary)] text-[var(--text-sub)] text-[12px] font-normal rounded shadow-sm transition-all focus:outline-none flex items-center space-x-2 cursor-pointer"
            >
               <Plus className="w-3.5 h-3.5" />
               <span>New Query</span>
            </button>
         </div>
      );
  }

  const activeTabObj = tabs.find(t => t.id === activeTabId);

  const getTabIcon = (tab: QueryTab) => {
    if (tab.type === 'detail') {
      switch(tab.targetType?.toLowerCase()) {
        case 'concept': return <Table className={`w-3.5 h-3.5 shrink-0 ${activeTabId === tab.id ? 'text-[var(--brand-primary)]' : 'text-[var(--text-muted)]'}`} />;
        case 'rule': return <Zap className={`w-3.5 h-3.5 shrink-0 ${activeTabId === tab.id ? 'text-[var(--brand-primary)]' : 'text-[var(--text-muted)]'}`} />;
        case 'function': return <Code className={`w-3.5 h-3.5 shrink-0 ${activeTabId === tab.id ? 'text-[var(--brand-primary)]' : 'text-[var(--text-muted)]'}`} />;
        case 'operator': return <Calculator className={`w-3.5 h-3.5 shrink-0 ${activeTabId === tab.id ? 'text-[var(--brand-primary)]' : 'text-[var(--text-muted)]'}`} />;
        default: return <Info className={`w-3.5 h-3.5 shrink-0 ${activeTabId === tab.id ? 'text-[var(--brand-primary)]' : 'text-[var(--text-muted)]'}`} />;
      }
    }
    return <FileCode className={`w-3.5 h-3.5 shrink-0 ${activeTabId === tab.id ? 'text-[var(--brand-primary)]' : 'text-[var(--text-muted)]'}`} />;
  };
  const sortedTabs = [...tabs].sort((a, b) => (b.pinned ? 1 : 0) - (a.pinned ? 1 : 0));

  return (
    <div className="h-full w-full flex flex-col relative bg-[var(--bg-surface)] font-sans transition-colors duration-200 overflow-hidden">
      {/* Editor Tab Bar */}
      <div className="bg-[var(--bg-surface-alt)] h-[40px] select-none border-b border-[var(--border-subtle)] shadow-[inset_0_-1px_0_rgba(0,0,0,0.02)] overflow-x-auto overflow-y-hidden tab-scrollbar flex-shrink-0 relative">
        <div className="flex h-[36px] w-max items-end pr-3">
          {sortedTabs.map((tab) => (
            <div 
              key={tab.id}
              onClick={() => setActiveTabId(tab.id)}
              onContextMenu={(e) => {
                e.preventDefault();
                setContextMenu({ x: e.clientX, y: e.clientY, tab });
              }}
              className={`h-full px-3 flex items-center justify-between min-w-[140px] max-w-[350px] group cursor-pointer border-r border-[var(--border-subtle)] relative transition-colors ${
                 activeTabId === tab.id 
                 ? 'bg-[var(--bg-surface)] border-t-[2px] border-t-[var(--brand-primary)] shadow-[0_-2px_6px_rgba(0,0,0,0.02)] z-10 text-[var(--text-main)]' 
                 : 'bg-transparent border-t-[2px] border-t-transparent hover:bg-[var(--bg-surface-alt)]/50 text-[var(--text-sub)] hover:text-[var(--text-main)]'
              }`}
            >
              <div className="flex items-center space-x-2 truncate">
                {tab.pinned ? (
                   <Pin className={`w-3 h-3 shrink-0 rotate-45 ${activeTabId === tab.id ? 'text-[var(--brand-primary)]' : 'text-[var(--text-muted)]'}`} />
                ) : getTabIcon(tab)}
                <span title={tab.filePath || tab.name} className={`text-[12px] tracking-wide truncate ${activeTabId === tab.id ? 'font-medium' : 'font-normal'}`}>
                   {tab.name}{tab.type !== 'detail' && !tab.isSaved ? <span className="text-amber-500 ml-0.5">*</span> : ''}
                </span>
                {tab.kb && tabs.some(t => t.id !== tab.id && t.targetName === tab.targetName) && (
                  <span 
                    className="text-[9px] px-1.5 py-0.5 rounded border ml-1 font-medium shrink-0"
                    style={{
                      color: getKbColorStyle(kbColorMap[tab.kb] ?? 0),
                      borderColor: getKbColorStyle(kbColorMap[tab.kb] ?? 0),
                      backgroundColor: `${getKbColorStyle(kbColorMap[tab.kb] ?? 0)}10`
                    }}
                  >
                    {tab.kb}
                  </span>
                )}
              </div>
              {!tab.pinned && (
                <button 
                  title="Close Tab"
                  onClick={(e) => { 
                    e.stopPropagation(); 
                    if (tab.type !== 'detail' && !tab.isSaved) {
                      useThingentStore.getState().showConfirm(
                        'Unsaved Changes',
                        `Tab "${tab.name}" has unsaved changes. Are you sure you want to close it?`,
                        () => removeTab(tab.id)
                      );
                    } else {
                      removeTab(tab.id); 
                    }
                  }}
                  className={`p-0.5 ml-2 rounded transition-all shrink-0 hover:text-red-500 cursor-pointer ${activeTabId === tab.id ? 'opacity-100 hover:bg-[var(--bg-surface-alt)]' : 'opacity-0 group-hover:opacity-100 hover:bg-[var(--bg-surface-alt)]/50'}`}
                >
                   <X className="w-3.5 h-3.5" />
                </button>
              )}
            </div>
          ))}
          <button 
            title={`New Query Tab (${cmd} + N)`}
            onClick={addTab}
            className="h-6 w-6 ml-1.5 self-center flex items-center justify-center rounded hover:bg-[var(--bg-surface-alt)]/80 text-[var(--text-muted)] hover:text-[var(--text-main)] transition-colors shrink-0 cursor-pointer"
          >
             <Plus className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>
      
      {activeTabObj?.type === 'ontology_builder' ? (
        <div className="flex-1 w-full relative overflow-hidden bg-[var(--bg-app)]">
          <OntologyBuilder tab={activeTabObj} />
        </div>
      ) : activeTabObj?.type === 'detail' ? (
        <div className="flex-1 w-full relative overflow-hidden bg-[var(--bg-app)]">
          <ObjectDetailPane tab={activeTabObj} />
        </div>
      ) : (
        <>
          {/* Inline Editor Toolbar */}
          <div className="h-8 bg-[var(--bg-surface)] border-b border-[var(--border-muted)] flex items-center px-3 justify-between space-x-3 text-[11px] text-[var(--text-sub)] font-normal select-none shadow-[0_1px_2px_rgba(0,0,0,0.01)] z-10 transition-colors flex-shrink-0">
             <div className="flex items-center space-x-3">
                <button title={`Execute (${alt} + Enter)`} onClick={() => {
                   let textToExecute = '';
                   if (editorInstance) {
                     const selection = editorInstance.getSelection();
                     if (selection && !selection.isEmpty()) {
                       textToExecute = editorInstance.getModel()?.getValueInRange(selection) || '';
                     }
                   }
                   execute(textToExecute || undefined);
                }} className="flex items-center hover:text-[var(--brand-primary)] hover:bg-[var(--brand-primary-light)]/50 px-1.5 py-1 rounded space-x-1 cursor-pointer transition-colors group px-2">
                   <PlayCircle className="w-3.5 h-3.5 text-[var(--brand-primary)] group-hover:scale-110 transition-transform" />
                   <span className="font-medium text-[var(--brand-primary-text)]">Execute Session</span>
                </button>
                <span className="text-[var(--border-muted)]">|</span>
                <button title={`Save (${cmd} + S)`} onClick={() => saveTab(activeTabId)} className="flex items-center hover:text-[var(--text-main)] hover:bg-[var(--bg-surface-alt)] px-1.5 py-1 rounded space-x-1 cursor-pointer transition-colors">
                   <Save className="w-3.5 h-3.5 text-[var(--text-muted)]" />
                   <span>Save</span>
                </button>
                <button title={`Format Document (Shift + ${alt} + F)`} onClick={handleFormat} className="flex items-center px-1.5 py-1 hover:text-[var(--text-main)] transition-colors rounded hover:bg-[var(--bg-surface-alt)] cursor-pointer">
                   <span>Format Document</span>
                </button>
             </div>
             <div className="flex items-center space-x-1">
                <button 
                  onClick={() => setSnippetsPanelOpen(!isSnippetsPanelOpen)}
                  className={`p-1.5 rounded transition-colors cursor-pointer ${isSnippetsPanelOpen ? 'bg-[var(--brand-primary)] text-white' : 'hover:bg-[var(--bg-surface-alt)] text-[var(--text-muted)] hover:text-[var(--text-main)]'}`} 
                  title="Quick Snippets"
                >
                  <Code2 className="w-3.5 h-3.5" />
                </button>
                <button className="hover:bg-[var(--bg-surface-alt)] p-1.5 rounded transition-colors cursor-pointer text-[var(--text-muted)] hover:text-[var(--text-main)]" title="Editor Settings">
                  <Settings2 className="w-3.5 h-3.5" />
                </button>
             </div>
          </div>

          <div className="flex-1 w-full relative flex flex-row overflow-hidden">
            <div className="flex-1 h-full relative">
              <Editor
              height="100%"
              defaultLanguage="sql"
              theme={monacoTheme}
              options={{
                minimap: { enabled: false },
                fontSize: studioSettings.fontSize === 'small' ? 12 : studioSettings.fontSize === 'medium' ? 14 : 16,
                fontFamily: "'JetBrains Mono', 'Fira Code', 'Courier New', monospace",
                fontWeight: studioSettings.fontWeight === 'thin' ? '300' : studioSettings.fontWeight === 'regular' ? '400' : '600',
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
              onMount={handleEditorDidMount}
              onChange={async (val) => {
                 setQuery(val || '');
                 if (val) {
                   try {
                     const diagnosticsResult = await useThingentStore.getState().fetchLspDiagnostics(val);
                     const markers = diagnosticsResult.errors?.map((err: any) => ({
                       startLineNumber: err.Line,
                       startColumn: err.Column,
                       endLineNumber: err.Line,
                       endColumn: err.Column + 1,
                       message: err.Message,
                       severity: monaco?.MarkerSeverity.Error || 8
                     })) || [];
                     
                     if (monaco && editorInstance) {
                        const model = editorInstance.getModel();
                        if (model) {
                           monaco.editor.setModelMarkers(model, 'thingent-lsp', markers);
                        }
                     }
                   } catch (e) {
                     console.error("LSP Diagnostics error", e);
                   }
                 }
              }}
            />
            </div>
            
            <SnippetsPanel onInsert={handleInsertSnippet} />
          </div>
        </>
      )}
      
      {contextMenu && (
        <TabContextMenu 
          x={contextMenu.x} 
          y={contextMenu.y} 
          tab={contextMenu.tab} 
          onClose={() => setContextMenu(null)} 
          onRename={undefined} // Implement rename later if needed
        />
      )}
    </div>
  );
}
