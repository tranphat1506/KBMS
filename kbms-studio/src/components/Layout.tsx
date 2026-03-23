import Navbar from './Navbar';
import Sidebar from './Sidebar';
import EditorPane from './EditorPane';
import ResultsPane from './ResultsPane';
import StatusBar from './StatusBar';
import ActivityBar from './ActivityBar';
import SystemManagement from './SystemManagement';
import { useKbmsStore } from '../store/kbmsStore';

export default function Layout() {
  const { activeSidebarView } = useKbmsStore();

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
              <div className="h-[55%] border-b border-[var(--border-subtle)] relative shadow-sm z-10 flex flex-col">
                <EditorPane />
              </div>
              
              {/* Results */}
              <div className="h-[45%] relative flex flex-col bg-[var(--bg-app)]">
                <ResultsPane />
              </div>
            </>
          )}
        </div>
      </div>
      <StatusBar />
    </div>
  );
}
