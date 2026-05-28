import { useMemo } from 'react';
import ReactFlow, { Background, Controls, Handle, Position, MarkerType } from 'reactflow';
import type { Node, Edge } from 'reactflow';
import 'reactflow/dist/style.css';
import { Table, Zap, Share2, Box } from 'lucide-react';

const PropertySection = ({ title, items, colorClass }: { title: string, items: any[], colorClass: string }) => {
  return (
    <div className="mb-3 last:mb-0">
      <div className="text-[10px] font-bold text-gray-500 uppercase tracking-wider mb-1.5">{title}</div>
      {items.length === 0 ? (
        <div className="text-[11px] text-gray-600 italic">—</div>
      ) : (
        <div className="space-y-1">
          {items.map((item, idx) => (
            <div key={idx} className="flex flex-col bg-[#181818]/50 p-1.5 rounded border border-[#333]/50 hover:bg-[#181818] transition-colors">
              <span className="text-[11px] font-medium text-gray-300 break-words leading-relaxed">{item.label}</span>
              {item.subLabel && <span className={`text-[9px] font-mono mt-0.5 ${colorClass}`}>{item.subLabel}</span>}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

const GroupNode = ({ data }: any) => {
  const Icon = data.icon;
  return (
    <div className={`bg-[#1e1e1e]/90 backdrop-blur-md border border-[#333] rounded-lg shadow-2xl w-[260px] text-[13px] font-sans overflow-hidden transition-all duration-300 hover:border-${data.colorClass}`}>
      <div className={`bg-${data.colorClass}/10 border-b border-${data.colorClass}/20 px-3 py-2 flex items-center space-x-2`}>
        <Icon className={`w-4 h-4 text-${data.colorClass}`} />
        <span className={`font-semibold text-${data.colorClass}`}>{data.label}</span>
      </div>
      <div className="p-3 max-h-[300px] overflow-y-auto custom-scrollbar">
        {data.sections.map((sec: any, i: number) => (
          <PropertySection key={i} title={sec.title} items={sec.items} colorClass={`text-${data.colorClass}`} />
        ))}
      </div>
      {data.handlePosition === 'left' && <Handle type="target" position={Position.Left} style={{ background: '#555' }} />}
      {data.handlePosition === 'right' && <Handle type="source" position={Position.Right} style={{ background: '#555' }} />}
      {data.handlePosition === 'top' && <Handle type="target" position={Position.Top} style={{ background: '#555' }} />}
      {data.handlePosition === 'bottom' && <Handle type="source" position={Position.Bottom} style={{ background: '#555' }} />}
    </div>
  );
};

const CenterConceptNode = ({ data }: any) => {
  return (
    <div className="bg-[#1e1e1e] border-2 border-indigo-500 rounded-xl shadow-[0_0_20px_rgba(99,102,241,0.2)] w-[200px] text-[13px] font-sans overflow-hidden flex flex-col items-center justify-center p-4">
      <Handle type="source" position={Position.Right} id="right" style={{ background: '#6366f1', right: '-6px', width: '12px', height: '12px' }} />
      <Handle type="target" position={Position.Left} id="left" style={{ background: '#6366f1', left: '-6px', width: '12px', height: '12px' }} />
      <Handle type="source" position={Position.Bottom} id="bottom" style={{ background: '#6366f1', bottom: '-6px', width: '12px', height: '12px' }} />
      
      <div className="w-10 h-10 bg-indigo-500/20 rounded-full flex items-center justify-center mb-2">
        <Table className="w-5 h-5 text-indigo-400" />
      </div>
      <div className="text-sm font-bold text-indigo-100 text-center truncate w-full">{data.label}</div>
      <div className="text-[10px] text-indigo-400/80 font-medium tracking-widest uppercase mt-1">Concept Base</div>
    </div>
  );
};

const nodeTypes = {
  center: CenterConceptNode,
  group: GroupNode
};

interface VisualConceptGraphProps {
  jsonData: any;
}

export default function VisualConceptGraph({ jsonData }: VisualConceptGraphProps) {
  const { nodes, edges } = useMemo(() => {
    if (!jsonData) return { nodes: [], edges: [] };
    
    const initialNodes: Node[] = [];
    const initialEdges: Edge[] = [];
    
    // 1. Group / Parent Node
    initialNodes.push({
      id: 'concept_group',
      type: 'group', // using reactflow built-in group is also possible, but here we just use it as a logical parent
      position: { x: 0, y: 0 },
      data: {},
      style: { width: 1000, height: 800, opacity: 0, pointerEvents: 'none' } // Invisible bounding box
    });

    // 2. Concept Center Node (Child of concept_group)
    initialNodes.push({
      id: 'concept_main',
      type: 'center',
      parentNode: 'concept_group',
      position: { x: 400, y: 350 },
      data: { label: jsonData.Name || 'Concept' }
    });

    // Helper to format items
    const formatItems = (list: any[], labelFn: (x: any) => string, subLabelFn?: (x: any) => string) => {
      if (!list) return [];
      return list.map(x => ({ label: labelFn(x), subLabel: subLabelFn ? subLabelFn(x) : undefined }));
    };

    // --- STRUCTURAL GROUP ---
    const structuralSections = [
      { title: 'Base Objects', items: formatItems(jsonData.BaseObjects || [], x => x, () => 'IS_A') },
      { title: 'Variables', items: formatItems(jsonData.Variables || [], x => x.Name, x => x.Domain?.Type || x.Type) },
      { title: 'Same Variables', items: formatItems(jsonData.SameVariables || [], x => `${x.Variable1} = ${x.Variable2}`) },
      { title: 'Aliases', items: formatItems(jsonData.Aliases || [], x => x) },
      { title: 'Properties', items: formatItems(Object.entries(jsonData.Properties || {}), ([k,v]) => `${k}: ${v}`) }
    ];

    initialNodes.push({
      id: 'node_structural',
      type: 'group',
      parentNode: 'concept_group',
      position: { x: 50, y: 50 },
      data: {
        label: 'Structural Properties',
        icon: Box,
        colorClass: 'blue-400',
        handlePosition: 'right',
        sections: structuralSections
      }
    });

    initialEdges.push({
      id: 'e_struct_center',
      source: 'node_structural',
      target: 'concept_main',
      targetHandle: 'left',
      style: { stroke: '#60a5fa', strokeWidth: 1.5, strokeDasharray: '4,4' },
      animated: true,
      markerEnd: { type: MarkerType.ArrowClosed, color: '#60a5fa' }
    });

    // --- LOGIC GROUP ---
    const logicSections = [
      { title: 'Constraints', items: formatItems(jsonData.Constraints || [], x => x.Expression) },
      { title: 'Equations', items: formatItems(jsonData.Equations || [], x => x.Expression) },
      { title: 'Concept Rules', items: formatItems(jsonData.ConceptRules || [], r => `${r.Hypothesis?.join(' AND ')} => ${r.Conclusion?.join(', ')}`, r => r.Kind || 'RULE') }
    ];

    initialNodes.push({
      id: 'node_logic',
      type: 'group',
      parentNode: 'concept_group',
      position: { x: 750, y: 50 },
      data: {
        label: 'Logic & Rules',
        icon: Zap,
        colorClass: 'yellow-500',
        handlePosition: 'left',
        sections: logicSections
      }
    });

    initialEdges.push({
      id: 'e_center_logic',
      source: 'concept_main',
      sourceHandle: 'right',
      target: 'node_logic',
      style: { stroke: '#eab308', strokeWidth: 1.5 },
      animated: true,
      markerEnd: { type: MarkerType.ArrowClosed, color: '#eab308' }
    });

    // --- RELATIONAL GROUP ---
    const relationalSections = [
      { title: 'Construct Relations', items: formatItems(jsonData.ConstructRelations || [], x => `${x.RelationName}(${x.Arguments?.join(', ')})`) },
      { title: 'Computable Relations', items: formatItems(jsonData.CompRels || [], x => `${x.InputVariables?.join(', ')} -> ${x.ResultVariable}`, x => `Cost ${x.Cost}`) }
    ];

    initialNodes.push({
      id: 'node_relational',
      type: 'group',
      parentNode: 'concept_group',
      position: { x: 400, y: 650 }, // Bottom
      data: {
        label: 'Relationships',
        icon: Share2,
        colorClass: 'emerald-400',
        handlePosition: 'top',
        sections: relationalSections
      }
    });

    initialEdges.push({
      id: 'e_center_relational',
      source: 'concept_main',
      sourceHandle: 'bottom',
      target: 'node_relational',
      style: { stroke: '#34d399', strokeWidth: 1.5 },
      animated: true,
      markerEnd: { type: MarkerType.ArrowClosed, color: '#34d399' }
    });

    return { nodes: initialNodes, edges: initialEdges };
  }, [jsonData]);

  return (
    <div className="w-full h-full bg-[#111] rounded-lg border border-[#333] overflow-hidden">
      <ReactFlow 
        nodes={nodes} 
        edges={edges}
        nodeTypes={nodeTypes}
        fitView
        fitViewOptions={{ padding: 0.2 }}
        minZoom={0.1}
        className="touch-none"
      >
        <Background color="#333" gap={20} size={1.5} />
        <Controls style={{ backgroundColor: '#222', borderColor: '#444' }} />
      </ReactFlow>
    </div>
  );
}
