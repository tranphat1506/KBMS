import { Handle, Position } from 'reactflow';
import { Box, Zap, Network, Info, ChevronDown, ChevronRight, FileText, Edit2, Trash2 } from 'lucide-react';
import { useState } from 'react';

// Reusable Section Component
const PropertySection = ({ title, icon: Icon, items, colorClass }: any) => {
  const [isOpen, setIsOpen] = useState(true);
  const itemCount = items ? items.length : 0;

  return (
    <div className="border-b border-[var(--border-subtle)] last:border-b-0">
      <div 
        className="flex items-center justify-between px-3 py-2 bg-[var(--bg-surface-alt)] cursor-pointer hover:bg-[var(--bg-surface-hover)] transition-colors"
        onClick={() => setIsOpen(!isOpen)}
      >
        <div className="flex items-center space-x-2">
          <Icon className={`w-3.5 h-3.5 ${colorClass}`} />
          <span className="text-[11px] font-bold text-[var(--text-main)] uppercase tracking-wider">{title}</span>
          <span className="text-[10px] text-[var(--text-muted)] font-mono bg-[var(--bg-surface)] px-1.5 rounded border border-[var(--border-subtle)]">{itemCount}</span>
        </div>
        {isOpen ? <ChevronDown className="w-3.5 h-3.5 text-[var(--text-muted)]" /> : <ChevronRight className="w-3.5 h-3.5 text-[var(--text-muted)]" />}
      </div>
      
      {isOpen && (
        <div className="bg-[var(--bg-surface)] flex flex-col">
          {itemCount === 0 ? (
            <div className="px-3 py-3 text-[11px] text-[var(--text-muted)] italic opacity-60">None</div>
          ) : (
            (items || []).map((item: any, idx: number) => (
              <div key={idx} className="flex flex-col group px-3 py-2 hover:bg-[var(--bg-surface-hover)] border-b border-[var(--border-subtle)] last:border-b-0 transition-colors">
                <div className="flex justify-between items-start w-full">
                  <span className="text-[12px] font-medium text-[var(--text-main)] break-words flex-1 pr-2 leading-relaxed">{item.label}</span>
                  {/* Action Buttons */}
                  <div className="hidden group-hover:flex items-center space-x-0.5 shrink-0">
                    <button className="p-1 hover:bg-[var(--brand-primary)]/10 text-[var(--brand-primary)] rounded transition-colors" title="View Details" onClick={(e) => { e.stopPropagation(); console.log('Details:', item.label); }}><FileText className="w-3 h-3" /></button>
                    <button className="p-1 hover:bg-yellow-500/10 text-yellow-500 rounded transition-colors" title="Edit" onClick={(e) => { e.stopPropagation(); console.log('Edit:', item.label); }}><Edit2 className="w-3 h-3" /></button>
                    <button className="p-1 hover:bg-red-500/10 text-red-500 rounded transition-colors" title="Delete" onClick={(e) => { e.stopPropagation(); console.log('Delete:', item.label); }}><Trash2 className="w-3 h-3" /></button>
                  </div>
                </div>
                {item.subLabel && <span className="text-[10px] text-[var(--text-muted)] font-mono mt-0.5 opacity-80">{item.subLabel}</span>}
              </div>
            ))
          )}
        </div>
      )}
    </div>
  );
};

export const ConceptNode = ({ data, selected }: any) => {
  // Collapsed View
  if (!data.expanded) {
    return (
      <div className={`transition-all duration-300 bg-[var(--bg-surface)] border ${selected ? 'border-[var(--brand-primary)] shadow-md' : 'border-[var(--border-subtle)]'} rounded-md overflow-hidden w-[220px] font-sans group hover:border-[var(--text-muted)] cursor-pointer`}>
        <Handle type="target" position={Position.Top} className="!bg-[var(--border-muted)] !w-2 !h-2 !rounded-none" />
        <div className="p-2.5 flex items-center space-x-2 bg-[var(--bg-surface-alt)]">
          <div className="w-5 h-5 flex items-center justify-center shrink-0 border border-[var(--border-subtle)] rounded bg-[var(--bg-surface)]">
            <Box className="w-3.5 h-3.5 text-[var(--brand-primary)]" />
          </div>
          <div className="flex-1 min-w-0">
            <div className="text-[12px] font-bold text-[var(--text-main)] truncate">{data.label}</div>
            <div className="text-[9px] text-[var(--text-muted)] uppercase tracking-wider">{data.isHierarchy ? 'Hierarchy' : 'Concept'}</div>
          </div>
        </div>
        <Handle type="source" position={Position.Bottom} className="!bg-[var(--border-muted)] !w-2 !h-2 !rounded-none" />
      </div>
    );
  }

  // Expanded View (ERD Card)
  const d = data.fullData || {};
  
  const vars: any[] = [];
  (d.Variables || []).forEach((v: any) => vars.push({ label: v.Name, subLabel: v.Domain?.Type || v.Type }));
  
  const sameVars: any[] = [];
  (d.SameVariables || []).forEach((sv: any) => sameVars.push({ label: `${sv.Variable1} = ${sv.Variable2}`, subLabel: 'Same Variable' }));
  
  const constraints: any[] = [];
  (d.Constraints || []).forEach((c: any) => constraints.push({ label: c.Expression, subLabel: 'Constraint' }));
  
  const equations: any[] = [];
  (d.Equations || []).forEach((eq: any) => equations.push({ label: eq.Expression, subLabel: 'Equation' }));
  
  const rules: any[] = [];
  (d.ConceptRules || []).forEach((r: any) => rules.push({ label: `${r.Hypothesis?.join(' AND ')} => ${r.Conclusion?.join(', ')}`, subLabel: r.Kind || 'RULE' }));
  
  const baseObjs: any[] = [];
  (d.BaseObjects || []).forEach((bo: string) => baseObjs.push({ label: bo, subLabel: 'IS A' }));
  
  const constRels: any[] = [];
  (d.ConstructRelations || []).forEach((cr: any) => constRels.push({ label: `${cr.RelationName}(${cr.Arguments?.join(', ')})`, subLabel: 'Construct' }));
  
  const compRels: any[] = [];
  (d.CompRels || []).forEach((cr: any) => compRels.push({ label: `${cr.InputVariables?.join(', ')} -> ${cr.ResultVariable}`, subLabel: `Rank ${cr.Rank} Cost ${cr.Cost}` }));
  
  const aliases: any[] = [];
  (d.Aliases || []).forEach((al: string) => aliases.push({ label: al, subLabel: 'Alias' }));
  
  const props: any[] = [];
  Object.entries(d.Properties || {}).forEach(([k, v]) => props.push({ label: `${k}: ${v}`, subLabel: 'Property' }));

  return (
    <div className={`relative z-10 bg-[var(--bg-surface)] border ${selected ? 'border-[var(--brand-primary)] shadow-2xl' : 'border-[var(--border-subtle)] shadow-xl'} rounded-xl overflow-hidden font-sans w-[350px] transition-all duration-300 flex flex-col`}>
      <Handle type="target" position={Position.Top} className="!opacity-0" />
      
      {/* Header */}
      <div className="p-3 flex items-center space-x-3 border-b border-[var(--brand-primary)]/20 bg-[var(--bg-surface-alt)] cursor-pointer hover:bg-[var(--bg-surface-hover)] transition-colors">
        <div className="w-8 h-8 flex items-center justify-center shrink-0 border border-[var(--brand-primary)]/30 rounded bg-[var(--brand-primary)]/10">
          <Box className="w-5 h-5 text-[var(--brand-primary)]" />
        </div>
        <div className="flex-1 min-w-0">
           <div className="text-[14px] font-bold text-[var(--text-main)] truncate">{data.label}</div>
           <div className="text-[9px] text-[var(--brand-primary)] font-bold uppercase tracking-widest mt-0.5">{data.isHierarchy ? 'Hierarchy' : 'Concept'}</div>
        </div>
      </div>

      {/* Body (Scrollable) */}
      <div className="flex flex-col w-full max-h-[500px] overflow-y-auto custom-scrollbar bg-[var(--bg-surface)] nodrag cursor-default">
        {!data.fullDetailsLoaded ? (
          <div className="p-8 flex items-center justify-center">
            <span className="text-[12px] font-mono text-[var(--brand-primary)] animate-pulse">Loading Blueprint Data...</span>
          </div>
        ) : (
          <div className="pb-2">
            <PropertySection title="Variables" icon={Box} items={vars} colorClass="text-blue-500" />
            <PropertySection title="Same Variables" icon={Box} items={sameVars} colorClass="text-blue-500" />
            <PropertySection title="Constraints" icon={Zap} items={constraints} colorClass="text-yellow-500" />
            <PropertySection title="Equations" icon={Zap} items={equations} colorClass="text-yellow-500" />
            <PropertySection title="Concept Rules" icon={Zap} items={rules} colorClass="text-yellow-500" />
            <PropertySection title="Base Objects" icon={Network} items={baseObjs} colorClass="text-purple-500" />
            <PropertySection title="Construct Relations" icon={Network} items={constRels} colorClass="text-purple-500" />
            <PropertySection title="Computable Relations" icon={Network} items={compRels} colorClass="text-purple-500" />
            <PropertySection title="Aliases" icon={Info} items={aliases} colorClass="text-gray-500" />
            <PropertySection title="Properties" icon={Info} items={props} colorClass="text-gray-500" />
          </div>
        )}
      </div>

      <Handle type="source" position={Position.Bottom} className="!opacity-0" />
    </div>
  );
};
