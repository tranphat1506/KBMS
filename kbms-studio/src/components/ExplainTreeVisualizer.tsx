import { useMemo } from 'react';
import ReactFlow, { Background, Controls, Handle, Position, MarkerType } from 'reactflow';
import type { Node, Edge } from 'reactflow';
import 'reactflow/dist/style.css';
import { Network, FileSearch, Box } from 'lucide-react';
import dagre from 'dagre';

export interface ExplanationNode {
  Goal: string;
  Value: any;
  IsBaseFact: boolean;
  DerivedBy: string | null;
  Logic?: string;
  StepCost?: number;
  Dependencies: ExplanationNode[];
}

interface Props {
  data: ExplanationNode;
}

const ExplainNodeComponent = ({ data }: any) => {
  const isGiven = data.source === 'GIVEN';

  let concept = 'Object';
  let field = data.label;
  if (data.label.includes('.')) {
    const parts = data.label.split('.');
    concept = parts[0];
    field = parts.slice(1).join('.');
  }

  return (
    <div className={`px-4 py-3 rounded-lg border-2 bg-[var(--bg-surface)]/95 backdrop-blur shadow-xl min-w-[160px] flex flex-col items-center justify-center transition-all ${isGiven ? 'border-emerald-500/50 shadow-emerald-500/10' : 'border-amber-500/50 shadow-amber-500/10'}`}>
      <Handle type="target" position={Position.Top} className="!bg-[var(--border-subtle)] !w-2 !h-2" />
      
      <div className="flex items-center space-x-2 mb-1 w-full justify-center">
        {isGiven ? <Box className="w-3 h-3 text-emerald-400" /> : <FileSearch className="w-3 h-3 text-amber-400" />}
        <span className="text-[10px] text-[var(--text-sub)] uppercase tracking-wider font-bold">{concept}</span>
      </div>
      
      <div className="text-[12px] px-2 py-1 rounded bg-[var(--bg-app)]/50 border border-[var(--border-subtle)]/30 font-mono text-[var(--text-main)] w-full text-center mb-1 break-words">
        <span className="text-[var(--brand-primary)] font-bold">{field}</span> = {data.value !== null ? String(data.value) : 'null'}
      </div>

      <Handle type="source" position={Position.Bottom} className="!bg-[var(--border-subtle)] !w-2 !h-2" />
    </div>
  );
};

const ExplainRuleNodeComponent = ({ data }: any) => {
  return (
    <div className="px-4 py-3 rounded-lg border-2 border-blue-500/50 shadow-blue-500/10 shadow-xl bg-[var(--bg-surface)]/95 flex flex-col items-center justify-center transition-all min-w-[200px]">
      <Handle type="target" position={Position.Top} className="!bg-[var(--border-subtle)] !w-2 !h-2" />
      
      <div className="flex items-center space-x-2 mb-2 w-full justify-center">
        <Network className="w-4 h-4 text-blue-400" />
        <span className="font-mono text-sm font-bold text-[var(--text-main)]">Rule: {data.rule}</span>
      </div>
      
      {data.logic && (
        <div className="text-[11px] px-3 py-1.5 rounded bg-[var(--bg-app)]/50 border border-[var(--border-subtle)]/30 font-medium text-[var(--text-sub)] text-center break-words max-w-[300px]">
          {data.logic}
        </div>
      )}
      
      {data.stepCost !== undefined && (
        <div className="text-[9px] text-[var(--brand-primary)] mt-2 font-mono font-bold">Cost: {data.stepCost}</div>
      )}

      <Handle type="source" position={Position.Bottom} className="!bg-[var(--border-subtle)] !w-2 !h-2" />
    </div>
  );
};

const nodeTypes = { explainNode: ExplainNodeComponent, explainRuleNode: ExplainRuleNodeComponent };

export default function ExplainTreeVisualizer({ data }: Props) {
  const { nodes, edges } = useMemo(() => {
    if (!data) return { nodes: [], edges: [] };

    const initialNodes: Node[] = [];
    const initialEdges: Edge[] = [];
    let idCounter = 0;

    const traverse = (nodeData: ExplanationNode, parentId: string | null = null): string => {
      const currentFactId = `n_${idCounter++}`;
      
      initialNodes.push({
        id: currentFactId,
        type: 'explainNode',
        position: { x: 0, y: 0 },
        data: {
          label: nodeData.Goal,
          value: nodeData.Value,
          source: nodeData.IsBaseFact ? 'GIVEN' : 'GENERATED'
        }
      });

      // If parentId exists, this fact is a dependency of parentId.
      // So edge direction is: currentFactId -> parentId
      if (parentId) {
        initialEdges.push({
          id: `e_${currentFactId}_${parentId}`,
          source: currentFactId,
          target: parentId,
          animated: false,
          style: { stroke: 'var(--border-muted)', strokeWidth: 1.5 },
          markerEnd: { type: MarkerType.ArrowClosed, color: 'var(--border-muted)' }
        });
      }

      if (nodeData.DerivedBy) {
        const ruleId = `r_${idCounter++}`;
        initialNodes.push({
          id: ruleId,
          type: 'explainRuleNode',
          position: { x: 0, y: 0 },
          data: {
            rule: nodeData.DerivedBy,
            logic: nodeData.Logic,
            stepCost: nodeData.StepCost
          }
        });

        // Edge from Rule -> Current Fact (Output)
        initialEdges.push({
          id: `e_${ruleId}_${currentFactId}`,
          source: ruleId,
          target: currentFactId,
          animated: true,
          style: { stroke: 'var(--brand-primary)', strokeWidth: 2 },
          markerEnd: { type: MarkerType.ArrowClosed, color: 'var(--brand-primary)' }
        });

        // Dependencies are inputs to the Rule: Dependency -> Rule
        if (nodeData.Dependencies) {
          nodeData.Dependencies.forEach(dep => {
            traverse(dep, ruleId);
          });
        }
      } else {
        // If not derived by rule (maybe generated by other means), connect dependencies directly to fact
        if (nodeData.Dependencies) {
          nodeData.Dependencies.forEach(dep => {
            traverse(dep, currentFactId);
          });
        }
      }

      // We return currentFactId so that if this traverse was called as a dependency, 
      // the caller (traverse) sets parentId = ruleId, linking currentFactId -> ruleId
      return currentFactId;
    };

    traverse(data);

    // Use Dagre to auto-layout the tree
    const dagreGraph = new dagre.graphlib.Graph();
    dagreGraph.setDefaultEdgeLabel(() => ({}));
    dagreGraph.setGraph({ rankdir: 'TB', nodesep: 150, ranksep: 80 });

    initialNodes.forEach(n => dagreGraph.setNode(n.id, { width: 250, height: 120 }));
    initialEdges.forEach(e => dagreGraph.setEdge(e.source, e.target));

    dagre.layout(dagreGraph);

    const layoutedNodes = initialNodes.map(n => {
      const nodeWithPosition = dagreGraph.node(n.id);
      n.position = {
        x: nodeWithPosition.x - 125,
        y: nodeWithPosition.y - 60
      };
      return n;
    });

    return { nodes: layoutedNodes, edges: initialEdges };
  }, [data]);

  return (
    <div className="w-full h-full bg-[var(--bg-app)] rounded border border-[var(--border-subtle)] overflow-hidden relative">
      <ReactFlow
        nodes={nodes}
        edges={edges}
        nodeTypes={nodeTypes}
        fitView
        fitViewOptions={{ padding: 0.2 }}
        minZoom={0.2}
      >
        <Background color="var(--border-subtle)" gap={20} size={1.5} />
        <Controls style={{ backgroundColor: 'var(--bg-surface)', borderColor: 'var(--border-subtle)', fill: 'var(--text-main)' }} />
      </ReactFlow>
    </div>
  );
}
