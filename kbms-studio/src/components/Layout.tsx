import Navbar from './Navbar';
import Sidebar from './Sidebar';
import EditorPane from './EditorPane';
import ResultsPane from './ResultsPane';
import StatusBar from './StatusBar';
import ActivityBar from './ActivityBar';

export default function Layout() {
  return (
    <div className="h-screen w-full flex flex-col bg-[#f8fafc] text-slate-800 font-sans overflow-hidden selection:bg-emerald-200">
      <Navbar />
      <div className="flex-1 flex overflow-hidden">
        <ActivityBar />
        
        {/* Sidebar */}
        <div className="w-[280px] flex-shrink-0 border-r border-[#e2e8f0] bg-[#f8fafc] shadow-[4px_0_24px_rgba(0,0,0,0.02)] z-10 relative flex flex-col">
           <Sidebar />
        </div>
        
        {/* Main Content */}
        <div className="flex-1 flex flex-col min-w-0 bg-white relative">
           {/* Editor */}
           <div className="h-[55%] border-b border-[#e2e8f0] relative shadow-sm z-10 flex flex-col">
             <EditorPane />
           </div>
           
           {/* Results */}
           <div className="h-[45%] relative flex flex-col bg-[#f8fafc]">
             <ResultsPane />
           </div>
        </div>
      </div>
      <StatusBar />
    </div>
  );
}
