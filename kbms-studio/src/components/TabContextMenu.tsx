import { useEffect, useRef } from 'react';
import { useThingentStore } from '../store/thingentStore';
import type { QueryTab } from '../store/thingentStore';
import { Pin, PinOff, X, XSquare, Copy, Edit2, FilePlus2 } from 'lucide-react';

interface TabContextMenuProps {
  x: number;
  y: number;
  tab: QueryTab;
  onClose: () => void;
  onRename?: (id: string) => void;
}

export default function TabContextMenu({ x, y, tab, onClose, onRename }: TabContextMenuProps) {
  const menuRef = useRef<HTMLDivElement>(null);
  
  const { 
    removeTab, 
    pinTab, 
    unpinTab, 
    closeOtherTabs, 
    closeTabsToRight, 
    closeAllTabs,
    addTab
  } = useThingentStore();

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        onClose();
      }
    };
    
    // Slight delay to avoid immediate closure if triggered by right click that also fires mousedown
    setTimeout(() => document.addEventListener('mousedown', handleClickOutside), 10);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [onClose]);

  // Ensure menu stays within viewport
  let finalX = x;
  let finalY = y;
  
  if (menuRef.current) {
    const rect = menuRef.current.getBoundingClientRect();
    if (x + rect.width > window.innerWidth) finalX = window.innerWidth - rect.width - 5;
    if (y + rect.height > window.innerHeight) finalY = window.innerHeight - rect.height - 5;
  }

  const handleAction = (action: () => void) => {
    action();
    onClose();
  };

  const copyTabName = () => {
    navigator.clipboard.writeText(tab.name).catch(err => {
      console.error("Failed to copy tab name:", err);
    });
  };

  return (
    <div 
      ref={menuRef}
      className="fixed z-50 bg-[var(--bg-surface-alt)] border border-[var(--border-subtle)] shadow-lg rounded-md py-1.5 min-w-[220px] text-[13px] flex flex-col"
      style={{ left: finalX, top: finalY }}
      onContextMenu={(e) => e.preventDefault()}
    >
      <div className="px-3 py-1.5 text-xs text-[var(--text-muted)] font-medium border-b border-[var(--border-subtle)]/50 mb-1 truncate">
        {tab.name}
      </div>
      
      <button 
        onClick={() => handleAction(() => removeTab(tab.id))}
        className="flex items-center w-full px-3 py-1.5 text-left hover:bg-[var(--brand-primary)] hover:text-white transition-colors"
      >
        <X className="w-4 h-4 mr-2" />
        <span className="flex-1">Close</span>
        <span className="text-[10px] opacity-60 ml-2">Cmd+W</span>
      </button>

      <button 
        onClick={() => handleAction(() => closeOtherTabs(tab.id))}
        className="flex items-center w-full px-3 py-1.5 text-left hover:bg-[var(--brand-primary)] hover:text-white transition-colors"
      >
        <XSquare className="w-4 h-4 mr-2" />
        <span>Close Others</span>
      </button>

      <button 
        onClick={() => handleAction(() => closeTabsToRight(tab.id))}
        className="flex items-center w-full px-3 py-1.5 text-left hover:bg-[var(--brand-primary)] hover:text-white transition-colors"
      >
        <XSquare className="w-4 h-4 mr-2 opacity-70" />
        <span>Close to the Right</span>
      </button>

      <button 
        onClick={() => handleAction(closeAllTabs)}
        className="flex items-center w-full px-3 py-1.5 text-left hover:bg-red-500 hover:text-white transition-colors"
      >
        <XSquare className="w-4 h-4 mr-2" />
        <span>Close All</span>
      </button>

      <div className="h-px bg-[var(--border-subtle)] my-1"></div>

      {tab.pinned ? (
        <button 
          onClick={() => handleAction(() => unpinTab(tab.id))}
          className="flex items-center w-full px-3 py-1.5 text-left hover:bg-[var(--brand-primary)] hover:text-white transition-colors"
        >
          <PinOff className="w-4 h-4 mr-2" />
          <span>Unpin Tab</span>
        </button>
      ) : (
        <button 
          onClick={() => handleAction(() => pinTab(tab.id))}
          className="flex items-center w-full px-3 py-1.5 text-left hover:bg-[var(--brand-primary)] hover:text-white transition-colors"
        >
          <Pin className="w-4 h-4 mr-2" />
          <span>Pin Tab</span>
        </button>
      )}

      {tab.type !== 'detail' && onRename && (
        <button 
          onClick={() => handleAction(() => onRename(tab.id))}
          className="flex items-center w-full px-3 py-1.5 text-left hover:bg-[var(--brand-primary)] hover:text-white transition-colors"
        >
          <Edit2 className="w-4 h-4 mr-2" />
          <span>Rename...</span>
        </button>
      )}

      <button 
        onClick={() => handleAction(copyTabName)}
        className="flex items-center w-full px-3 py-1.5 text-left hover:bg-[var(--brand-primary)] hover:text-white transition-colors"
      >
        <Copy className="w-4 h-4 mr-2" />
        <span>Copy Name</span>
      </button>

      <div className="h-px bg-[var(--border-subtle)] my-1"></div>

      <button 
        onClick={() => handleAction(addTab)}
        className="flex items-center w-full px-3 py-1.5 text-left hover:bg-[var(--brand-primary)] hover:text-white transition-colors"
      >
        <FilePlus2 className="w-4 h-4 mr-2" />
        <span className="flex-1">New Tab</span>
        <span className="text-[10px] opacity-60 ml-2">Cmd+N</span>
      </button>
    </div>
  );
}
