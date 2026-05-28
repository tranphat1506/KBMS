import { useState, useCallback, useEffect } from 'react';
import { useThingentStore } from '../store/thingentStore';
import type { QueryTab } from '../store/thingentStore';
import ReactFlow, { Background, Controls, MiniMap, MarkerType, useNodesState, useEdgesState, useReactFlow, ReactFlowProvider, Panel } from 'reactflow';
import type { Node, Edge } from 'reactflow';
import dagre from 'dagre';
import { Layers, Network, Box, ChevronDown, ChevronUp } from 'lucide-react';
import 'reactflow/dist/style.css';

import { ConceptNode } from './ontology/nodes/ConceptNode';

const nodeTypes = {
  concept: ConceptNode,
};

const dagreGraph = new dagre.graphlib.Graph();
dagreGraph.setDefaultEdgeLabel(() => ({}));

const getLayoutedElements = (nodes: Node[], edges: Edge[], direction = 'TB') => {
  dagreGraph.setGraph({ rankdir: direction, nodesep: 150, ranksep: 200 });

  nodes.forEach((node) => {
    // ERD Layout Math
    let w = 220;
    let h = 60;
    if (node.data?.expanded) {
      w = 350; // max width of card
      h = 550; // max height of card + padding
    }
    dagreGraph.setNode(node.id, { width: w, height: h });
  });

  edges.forEach((edge) => {
    dagreGraph.setEdge(edge.source, edge.target);
  });

  dagre.layout(dagreGraph);

  const layoutedNodes = nodes.map((node) => {
    const nodeWithPosition = dagreGraph.node(node.id);
    let w = 220;
    let h = 60;
    if (node.data?.expanded) {
      w = 350;
      h = 550;
    }
    return {
      ...node,
      position: {
        x: nodeWithPosition.x - w / 2,
        y: nodeWithPosition.y - h / 2,
      },
    };
  });

  return { nodes: layoutedNodes, edges };
};

function OntologyCanvas({ tab }: { tab: QueryTab }) {
  const { kbMetadata, execute } = useThingentStore();
  const kbData = kbMetadata[tab.kb || ''];
  const { fitView, setCenter, getNodes, getEdges } = useReactFlow();

  const [nodes, setNodes, onNodesChange] = useNodesState([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState([]);

  // Filters State
  const [filters, setFilters] = useState({
    concepts: true,
    hierarchies: true,
    relations: true,
    rules: true,
    functions: true,
    operators: true
  });
  const [hudMinimized, setHudMinimized] = useState(false);
  const [hudOpacity, setHudOpacity] = useState(80); 
  const [hasFetched, setHasFetched] = useState(false);

  useEffect(() => {
    if (tab.kb && !hasFetched && (!kbData || !kbData.concepts)) {
      setHasFetched(true);
      const store = useThingentStore.getState();
      if (store.selectedKb !== tab.kb) {
        store.changeKnowledgeBase(tab.kb);
      } else {
        store.fetchMetadata();
      }
    }
  }, [tab.kb, kbData, hasFetched]);

  // Initial Graph Setup
  useEffect(() => {
    if (!kbData?.concepts) return;

    const initialNodes: Node[] = [];
    const initialEdges: Edge[] = [];

    // Concepts
    kbData.concepts.forEach((c) => {
      initialNodes.push({
        id: c.Name,
        type: 'concept',
        position: { x: 0, y: 0 },
        data: { 
          label: c.Name, 
          isHierarchy: c.IsHierarchy,
          expanded: false
        }
      });
    });

    if (kbData.hierarchies) {
      kbData.hierarchies.filter(h => h.HierarchyType === 'IS_A').forEach((rel, idx) => {
        initialEdges.push({
          id: `isa_${idx}`,
          source: rel.ParentConcept,
          target: rel.ChildConcept,
          type: 'smoothstep',
          animated: true,
          style: { stroke: 'var(--text-muted)', strokeWidth: 1.5, strokeDasharray: '4,4' },
          markerEnd: { type: MarkerType.ArrowClosed, color: 'var(--text-muted)' }
        });
      });
    }

    const { nodes: layoutedNodes, edges: layoutedEdges } = getLayoutedElements(initialNodes, initialEdges, 'TB');
    setNodes(layoutedNodes);
    setEdges(layoutedEdges);
    
    setTimeout(() => {
      fitView({ duration: 800, padding: 0.2 });
    }, 100);

  }, [kbData, fitView, setNodes, setEdges]);

  // Apply Visibility
  useEffect(() => {
    setNodes(nds => nds.map(n => {
      if (n.type === 'concept') return { ...n, hidden: !filters.concepts };
      return n;
    }));
    setEdges(eds => eds.map(e => {
      if (e.id.startsWith('isa_')) return { ...e, hidden: !filters.hierarchies };
      return e;
    }));
  }, [filters, setNodes, setEdges]);

  // Handle Node Click
  const onNodeClick = useCallback((_event: React.MouseEvent, node: Node) => {
    if (node.type !== 'concept') return;
    
    const isExpanding = !node.data.expanded;
    const currentNodes = getNodes();
    const currentEdges = getEdges();

    let newNodes = currentNodes.map(n => {
      if (n.id === node.id) {
        return { ...n, data: { ...n.data, expanded: isExpanding } };
      }
      return n;
    });

    // Re-layout immediately so the box expands
    const { nodes: layoutedNodes, edges: layoutedEdges } = getLayoutedElements(newNodes, currentEdges, 'TB');
    setNodes(layoutedNodes);
    setEdges(layoutedEdges);

    if (!isExpanding) {
      // Zoom out to see the network
      const layoutedNode = layoutedNodes.find(n => n.id === node.id);
      if (layoutedNode) {
          const cx = layoutedNode.position.x + 110;
          const cy = layoutedNode.position.y + 30;
          setCenter(cx, cy, { zoom: 1, duration: 800 });
      }
      return;
    }

    // Zoom into the expanded card
    const layoutedNode = layoutedNodes.find(n => n.id === node.id);
    if (layoutedNode) {
        const cx = layoutedNode.position.x + 175; // 350 / 2
        const cy = layoutedNode.position.y + 275; // 550 / 2
        setCenter(cx, cy, { zoom: 0.8, duration: 800 });
    }

    // Fetch details
    execute(`DESCRIBE (CONCEPT: ${node.id});`, { isBackground: true }).then((res: any) => {
      if (res && res.rows && res.rows.length > 0) {
        const jsonString = res.rows[0]._JsonData || res.rows[0]._jsonData;
        if (jsonString) {
          try {
            const fullData = JSON.parse(jsonString);
            const d: any = {};
            for (const [k, v] of Object.entries(fullData)) {
              d[k.charAt(0).toUpperCase() + k.slice(1)] = v;
            }

            setNodes(nds => nds.map(n => {
               if (n.id === node.id) {
                 return { ...n, data: { ...n.data, fullDetailsLoaded: true, fullData: d } };
               }
               return n;
            }));

          } catch (e) { console.error("Parse error:", e); }
        }
      }
    });

  }, [setCenter, setNodes, setEdges, execute, getNodes, getEdges]);

  return (
    <div className="w-full h-full relative">
      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onNodeClick={onNodeClick}
        nodeTypes={nodeTypes}
        fitView
        minZoom={0.1}
        className="bg-[var(--bg-app)]"
      >
        <Background color="var(--border-subtle)" gap={30} size={1.5} />
        <Controls style={{ backgroundColor: 'var(--bg-surface)', borderColor: 'var(--border-subtle)', fill: 'var(--text-main)' }} />
        
        <Panel position="top-left" className="bg-[var(--bg-surface)]/80 backdrop-blur-md border border-[var(--border-subtle)] p-3 rounded-xl shadow-lg flex items-center space-x-3 ml-2 mt-2">
          <div className="p-2 bg-[var(--brand-primary)]/10 rounded-lg text-[var(--brand-primary)]">
            <Network className="w-5 h-5" />
          </div>
          <div>
            <h1 className="text-sm font-bold tracking-tight text-[var(--text-main)]">Blueprint Architecture</h1>
            <div className="flex items-center space-x-2 mt-0.5">
              {tab.kb && (
                <p className="text-[10px] font-medium text-[var(--brand-primary)] uppercase tracking-wider">
                  KB: {tab.kb}
                </p>
              )}
              {tab.server && (
                <>
                  <span className="text-[var(--border-subtle)] text-[10px]">•</span>
                  <p className="text-[10px] font-medium text-[var(--text-muted)] uppercase tracking-wider">
                    SERVER: {tab.server}
                  </p>
                </>
              )}
            </div>
          </div>
        </Panel>

        <MiniMap 
          nodeColor={() => {
            return '#6366f1'; 
          }}
          maskColor="rgba(0, 0, 0, 0.6)"
          style={{ backgroundColor: '#111' }}
        />
      </ReactFlow>

      {/* Floating HUD Filters */}
      <div 
        className={`absolute bottom-6 left-6 z-10 bg-[var(--bg-surface)] backdrop-blur-md border border-[var(--border-subtle)] rounded-xl shadow-2xl transition-all duration-300 ${hudMinimized ? 'w-[140px] p-2' : 'w-[200px] p-4'}`}
        style={{ opacity: hudOpacity / 100 }}
      >
        <div className="flex items-center justify-between text-[var(--text-sub)]">
          <div className="flex items-center space-x-2">
            <Layers className="w-4 h-4" />
            <h3 className="text-xs font-bold uppercase tracking-wider">Layers</h3>
          </div>
          <button onClick={() => setHudMinimized(!hudMinimized)} className="p-1 hover:bg-[var(--bg-surface-alt)] rounded text-[var(--text-muted)] hover:text-[var(--text-main)] transition-colors">
            {hudMinimized ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
          </button>
        </div>
        
        {!hudMinimized && (
          <div className="space-y-2 text-[13px] mt-3 pt-2 border-t border-[var(--border-subtle)]">
            <div className="mb-3 pb-3 border-b border-[var(--border-subtle)] group">
               <div className="flex justify-between items-center mb-1 text-[10px] text-[var(--text-muted)]">
                 <span>Panel Opacity</span>
                 <span>{hudOpacity}%</span>
               </div>
               <input type="range" min="20" max="100" value={hudOpacity} onChange={(e) => setHudOpacity(parseInt(e.target.value))} className="w-full h-1 bg-[var(--border-subtle)] rounded-lg appearance-none cursor-pointer" />
            </div>

            <label className="flex items-center space-x-3 cursor-pointer group">
              <input type="checkbox" checked={filters.concepts} onChange={(e) => setFilters({...filters, concepts: e.target.checked})} className="accent-[var(--brand-primary)]" />
              <Box className="w-3.5 h-3.5 text-[var(--brand-primary)]" />
              <span className="text-[var(--text-main)] group-hover:text-[var(--brand-primary)] transition-colors">Concepts</span>
            </label>
            <label className="flex items-center space-x-3 cursor-pointer group">
              <input type="checkbox" checked={filters.hierarchies} onChange={(e) => setFilters({...filters, hierarchies: e.target.checked})} className="accent-emerald-500" />
              <Network className="w-3.5 h-3.5 text-[var(--brand-primary)]" />
              <span className="text-[var(--text-main)] group-hover:text-[var(--brand-primary)] transition-colors">Hierarchies</span>
            </label>
          </div>
        )}
      </div>
    </div>
  );
}

export default function OntologyBuilder({ tab }: { tab: QueryTab }) {
  return (
    <ReactFlowProvider>
      <OntologyCanvas tab={tab} />
    </ReactFlowProvider>
  );
}
