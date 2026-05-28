import { useState } from 'react';
import { useThingentStore } from '../store/thingentStore';
import { X, Plus, Trash2, Code2, ChevronRight, GripVertical } from 'lucide-react';

interface SnippetsPanelProps {
  onInsert: (code: string) => void;
}

export default function SnippetsPanel({ onInsert }: SnippetsPanelProps) {
  const { snippets, addSnippet, deleteSnippet, isSnippetsPanelOpen, setSnippetsPanelOpen } = useThingentStore();
  const [isAdding, setIsAdding] = useState(false);
  const [newName, setNewName] = useState('');
  const [newCode, setNewCode] = useState('');

  if (!isSnippetsPanelOpen) return null;

  const handleAdd = () => {
    if (!newName.trim() || !newCode.trim()) return;
    addSnippet({
      id: Date.now().toString(),
      name: newName,
      code: newCode
    });
    setNewName('');
    setNewCode('');
    setIsAdding(false);
  };

  return (
    <div className="w-[300px] h-full flex flex-col bg-[var(--bg-surface-alt)] border-l border-[var(--border-subtle)] overflow-hidden shrink-0">
      <div className="h-[40px] px-3 flex items-center justify-between border-b border-[var(--border-subtle)] bg-[var(--bg-surface)] shrink-0">
        <div className="flex items-center space-x-2">
          <Code2 className="w-4 h-4 text-[var(--text-muted)]" />
          <span className="text-[13px] font-medium text-[var(--text-main)] tracking-wide">Quick Snippets</span>
        </div>
        <div className="flex items-center space-x-1">
          <button 
            title="Add Snippet"
            onClick={() => setIsAdding(!isAdding)}
            className={`p-1.5 rounded transition-colors ${isAdding ? 'bg-[var(--brand-primary)] text-white' : 'hover:bg-[var(--bg-surface-alt)] text-[var(--text-muted)] hover:text-[var(--text-main)]'}`}
          >
            <Plus className="w-3.5 h-3.5" />
          </button>
          <button 
            title="Close Panel"
            onClick={() => setSnippetsPanelOpen(false)}
            className="p-1.5 rounded hover:bg-[var(--bg-surface-alt)] text-[var(--text-muted)] hover:text-[var(--text-main)] transition-colors"
          >
            <X className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto custom-scrollbar p-2 space-y-2">
        {isAdding && (
          <div className="p-3 bg-[var(--bg-surface)] border border-[var(--brand-primary)]/50 rounded shadow-sm flex flex-col space-y-2 mb-3">
            <input 
              autoFocus
              type="text" 
              placeholder="Snippet Name..." 
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              className="bg-[var(--bg-surface-alt)] border border-[var(--border-subtle)] rounded px-2 py-1.5 text-xs text-[var(--text-main)] outline-none focus:border-[var(--brand-primary)]"
            />
            <textarea 
              placeholder="Code to insert..." 
              value={newCode}
              onChange={(e) => setNewCode(e.target.value)}
              className="bg-[var(--bg-surface-alt)] border border-[var(--border-subtle)] rounded px-2 py-1.5 text-xs text-[var(--text-main)] outline-none focus:border-[var(--brand-primary)] min-h-[80px] resize-y font-mono"
            />
            <div className="flex justify-end space-x-2 pt-1">
              <button onClick={() => setIsAdding(false)} className="px-2 py-1 text-xs text-[var(--text-sub)] hover:text-[var(--text-main)]">Cancel</button>
              <button onClick={handleAdd} className="px-3 py-1 bg-[var(--brand-primary)] text-white text-xs rounded font-medium hover:bg-emerald-600 transition-colors">Save</button>
            </div>
          </div>
        )}

        {snippets.map((snippet) => (
          <div 
            key={snippet.id} 
            className="group flex flex-col bg-[var(--bg-surface)] border border-[var(--border-subtle)] rounded shadow-sm overflow-hidden hover:border-[var(--brand-primary)]/50 transition-colors"
          >
            <div className="flex items-center justify-between p-2 cursor-pointer" onClick={() => onInsert(snippet.code)}>
              <div className="flex items-center space-x-2 truncate">
                <ChevronRight className="w-3.5 h-3.5 text-[var(--text-muted)] group-hover:text-[var(--brand-primary)] transition-colors" />
                <span className="text-xs font-medium text-[var(--text-main)] truncate">{snippet.name}</span>
              </div>
              <div className="flex items-center space-x-1 opacity-0 group-hover:opacity-100 transition-opacity">
                <button 
                  title="Delete"
                  onClick={(e) => { e.stopPropagation(); deleteSnippet(snippet.id); }}
                  className="p-1 text-[var(--text-muted)] hover:text-red-500 rounded hover:bg-red-500/10"
                >
                  <Trash2 className="w-3 h-3" />
                </button>
                <div className="cursor-grab p-1 text-[var(--border-subtle)] hover:text-[var(--text-sub)]" onClick={e => e.stopPropagation()}>
                  <GripVertical className="w-3 h-3" />
                </div>
              </div>
            </div>
            {/* Optional: Show tiny preview of code 
            <div className="px-3 pb-2 hidden group-hover:block">
               <div className="text-[10px] text-[var(--text-muted)] font-mono truncate opacity-60">{snippet.code.split('\n')[0]}</div>
            </div>
            */}
          </div>
        ))}
        
        {snippets.length === 0 && !isAdding && (
          <div className="text-center p-4 text-[var(--text-muted)] text-xs">
            No snippets yet. Click + to add one.
          </div>
        )}
      </div>
    </div>
  );
}
