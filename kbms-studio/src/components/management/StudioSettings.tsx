import { useKbmsStore } from '../../store/kbmsStore';
import { Moon, Sun, Type, Palette, Check, X, Bell, Shield, Terminal, Zap } from 'lucide-react';
import { useState } from 'react';

export default function StudioSettings() {
  const { 
    studioSettings, updateStudioSetting, setStudioSettingsOpen,
    notificationSettings, updateNotificationSettings 
  } = useKbmsStore();
  
  const [activeTab, setActiveTab] = useState<'visual' | 'notifications'>('visual');

  const fontSizes = [
    { id: 'small', label: 'Small', desc: 'Compact view for data density' },
    { id: 'medium', label: 'Medium', desc: 'Standard balanced proportions' },
    { id: 'big', label: 'Big', desc: 'Enhanced legibility and focus' }
  ];

  const fontWeights = [
    { id: 'thin', label: 'Thin', desc: 'Ultra-light, modern aesthetic' },
    { id: 'regular', label: 'Regular', desc: 'Standard system readability' },
    { id: 'medium', label: 'Medium', desc: 'Increased emphasis and weight' }
  ];

  return (
    <div className="bg-[var(--bg-surface)] rounded-xl border border-[var(--border-subtle)] shadow-2xl w-full max-w-[800px] overflow-hidden flex flex-col max-h-[90vh] select-none animate-in zoom-in-95 duration-200 transition-colors">
      {/* Header */}
      <div className="p-6 border-b border-[var(--border-muted)] flex items-center justify-between bg-[var(--bg-app)]/30">
        <div>
          <h1 className="text-xl font-bold text-[var(--text-main)] tracking-tight uppercase">Studio Preferences</h1>
          <div className="h-0.5 w-8 bg-[var(--brand-primary)] mt-1" />
          <p className="text-[11px] text-[var(--text-muted)] font-thin mt-2 italic">Customize the visual orchestration and typography of your workspace.</p>
        </div>
        <button 
          onClick={() => setStudioSettingsOpen(false)}
          className="p-2 hover:bg-[var(--bg-surface-alt)] rounded-full transition-all text-[var(--text-muted)] hover:text-[var(--text-main)]"
        >
          <X className="w-5 h-5" />
        </button>
      </div>

      {/* Tabs */}
      <div className="flex px-6 bg-[var(--bg-app)]/30 border-b border-[var(--border-muted)]">
        <button 
          onClick={() => setActiveTab('visual')}
          className={`px-6 py-3 text-[10px] font-bold uppercase tracking-widest transition-all border-b-2 ${activeTab === 'visual' ? 'border-[var(--brand-primary)] text-[var(--brand-primary)]' : 'border-transparent text-[var(--text-muted)] hover:text-[var(--text-main)]'}`}
        >
          Visual Workspace
        </button>
        <button 
          onClick={() => setActiveTab('notifications')}
          className={`px-6 py-3 text-[10px] font-bold uppercase tracking-widest transition-all border-b-2 ${activeTab === 'notifications' ? 'border-[var(--brand-primary)] text-[var(--brand-primary)]' : 'border-transparent text-[var(--text-muted)] hover:text-[var(--text-main)]'}`}
        >
          Notifications
        </button>
      </div>

      {/* Body */}
      <div className="flex-1 overflow-y-auto p-8 custom-scrollbar space-y-8">
        {activeTab === 'visual' ? (
          <>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-8 text-left">
              {/* Theme Setting */}
              <div className="space-y-4">
                <div className="flex items-center space-x-3">
                  <div className="p-2 bg-[var(--bg-app)] rounded border border-[var(--border-subtle)] text-[var(--text-muted)]">
                    <Sun className="w-4 h-4" />
                  </div>
                  <span className="text-[10px] font-thin text-[var(--text-muted)] uppercase tracking-[0.2em]">Appearance Mode</span>
                </div>

                <div className="grid grid-cols-2 gap-3">
                  <button 
                    onClick={() => updateStudioSetting('theme', 'light')}
                    className={`flex items-center justify-between px-3 py-2.5 rounded border transition-all ${studioSettings.theme === 'light' ? 'bg-[var(--brand-primary-light)]/20 border-[var(--brand-primary)]/30 text-[var(--brand-primary)]' : 'bg-[var(--bg-app)] border-[var(--border-subtle)] text-[var(--text-muted)] hover:border-[var(--text-muted)]'}`}
                  >
                    <div className="flex items-center space-x-2">
                      <Sun className="w-3 h-3" />
                      <span className="text-xs font-thin">Light</span>
                    </div>
                    {studioSettings.theme === 'light' && <Check className="w-3 h-3" />}
                  </button>
                  <button 
                    onClick={() => updateStudioSetting('theme', 'dark')}
                    className={`flex items-center justify-between px-3 py-2.5 rounded border transition-all ${studioSettings.theme === 'dark' ? 'bg-[var(--brand-primary-light)]/20 border-[var(--brand-primary)]/30 text-[var(--brand-primary)]' : 'bg-[var(--bg-app)] border-[var(--border-subtle)] text-[var(--text-muted)] hover:border-[var(--text-muted)]'}`}
                  >
                    <div className="flex items-center space-x-2">
                      <Moon className="w-3 h-3" />
                      <span className="text-xs font-thin">Dark</span>
                    </div>
                    {studioSettings.theme === 'dark' && <Check className="w-3 h-3" />}
                  </button>
                </div>
              </div>

              {/* Color Tone Setting */}
              <div className="space-y-4">
                <div className="flex items-center space-x-3">
                  <div className="p-2 bg-[var(--bg-app)] rounded border border-[var(--border-subtle)] text-[var(--text-muted)]">
                    <Palette className="w-4 h-4" />
                  </div>
                  <span className="text-[10px] font-thin text-[var(--text-muted)] uppercase tracking-[0.2em]">Primary Semantic Tone</span>
                </div>

                <div className="flex items-center space-x-2 px-3 py-3 bg-[var(--brand-primary-light)]/20 border border-[var(--brand-primary)]/30 rounded text-[var(--brand-primary)] w-full justify-between transition-colors">
                  <div className="flex items-center space-x-3">
                    <div className="w-3 h-3 rounded-full bg-[var(--brand-primary)] shadow-[0_0_8px_var(--brand-primary)]" />
                    <span className="text-xs font-thin">Emerald Green</span>
                  </div>
                  <Check className="w-3.5 h-3.5" />
                </div>
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-8 text-left">
              {/* Font Size Setting */}
              <div className="space-y-4">
                <div className="flex items-center space-x-3">
                  <div className="p-2 bg-[var(--bg-app)] rounded border border-[var(--border-subtle)] text-[var(--text-muted)]">
                    <Type className="w-4 h-4" />
                  </div>
                  <span className="text-[10px] font-thin text-[var(--text-muted)] uppercase tracking-[0.2em]">Typography Scale</span>
                </div>

                <div className="space-y-2">
                  {fontSizes.map((size) => (
                    <button 
                      key={size.id}
                      onClick={() => updateStudioSetting('fontSize', size.id)}
                      className={`w-full flex items-center justify-between px-4 py-3 rounded border transition-all ${studioSettings.fontSize === size.id ? 'bg-[var(--brand-primary-light)]/20 border-[var(--brand-primary)]/30' : 'bg-[var(--bg-app)] border-[var(--border-subtle)] hover:border-[var(--text-muted)]'}`}
                    >
                      <div className="text-left">
                        <div className={`font-thin text-[var(--text-main)] ${size.id === 'small' ? 'text-[11px]' : size.id === 'medium' ? 'text-xs' : 'text-sm'}`}>
                          {size.label} Environment
                        </div>
                      </div>
                      {studioSettings.fontSize === size.id && <Check className="w-3.5 h-3.5 text-[var(--brand-primary)]" />}
                    </button>
                  ))}
                </div>
              </div>

              {/* Font Weight Setting */}
              <div className="space-y-4">
                <div className="flex items-center space-x-3">
                  <div className="p-2 bg-[var(--bg-app)] rounded border border-[var(--border-subtle)] text-[var(--text-muted)]">
                    <Type className="w-4 h-4" />
                  </div>
                  <span className="text-[10px] font-thin text-[var(--text-muted)] uppercase tracking-[0.2em]">Structural Font Weight</span>
                </div>

                <div className="space-y-2">
                  {fontWeights.map((weight) => (
                    <button 
                      key={weight.id}
                      onClick={() => updateStudioSetting('fontWeight', weight.id)}
                      className={`w-full flex items-center justify-between px-4 py-3 rounded border transition-all ${studioSettings.fontWeight === weight.id ? 'bg-[var(--brand-primary-light)]/20 border-[var(--brand-primary)]/30' : 'bg-[var(--bg-app)] border-[var(--border-subtle)] hover:border-[var(--text-muted)]'}`}
                    >
                      <div className="text-left">
                        <div className={`text-[var(--text-main)] text-xs ${weight.id === 'thin' ? 'font-thin' : weight.id === 'regular' ? 'font-normal' : 'font-medium'}`}>
                          {weight.label} Typographic Weight
                        </div>
                      </div>
                      {studioSettings.fontWeight === weight.id && <Check className="w-3.5 h-3.5 text-[var(--brand-primary)]" />}
                    </button>
                  ))}
                </div>
              </div>
            </div>
          </>
        ) : (
          <div className="space-y-8 animate-in slide-in-from-right-4 duration-300">
            {/* General Toggles */}
            <div className="space-y-4">
              <div className="flex items-center space-x-3">
                <div className="p-2 bg-[var(--bg-app)] rounded border border-[var(--border-subtle)] text-[var(--text-muted)]">
                  <Bell className="w-4 h-4" />
                </div>
                <span className="text-[10px] font-thin text-[var(--text-muted)] uppercase tracking-[0.2em]">Alert Subscriptions</span>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <button 
                  onClick={() => updateNotificationSettings({ enableQuerySuccess: !notificationSettings.enableQuerySuccess })}
                  className={`flex flex-col p-4 rounded border transition-all text-left ${notificationSettings.enableQuerySuccess ? 'bg-[var(--brand-primary-light)]/10 border-[var(--brand-primary)]/30' : 'bg-[var(--bg-app)] border-[var(--border-subtle)]'}`}
                >
                  <div className="flex items-center justify-between mb-2">
                    <Zap className={`w-4 h-4 ${notificationSettings.enableQuerySuccess ? 'text-[var(--brand-primary)]' : 'text-[var(--text-muted)]'}`} />
                    <div className={`w-8 h-4 rounded-full relative transition-colors ${notificationSettings.enableQuerySuccess ? 'bg-[var(--brand-primary)]' : 'bg-[var(--border-subtle)]'}`}>
                      <div className={`absolute top-0.5 w-3 h-3 bg-white rounded-full transition-all ${notificationSettings.enableQuerySuccess ? 'left-4.5' : 'left-0.5'}`} />
                    </div>
                  </div>
                  <span className="text-[11px] font-bold text-[var(--text-main)] uppercase tracking-wider mb-1">Query Success</span>
                  <p className="text-[10px] text-[var(--text-muted)] font-thin">Notify when background statements complete.</p>
                </button>

                <button 
                  onClick={() => updateNotificationSettings({ enableServerLogs: !notificationSettings.enableServerLogs })}
                  className={`flex flex-col p-4 rounded border transition-all text-left ${notificationSettings.enableServerLogs ? 'bg-[var(--brand-primary-light)]/10 border-[var(--brand-primary)]/30' : 'bg-[var(--bg-app)] border-[var(--border-subtle)]'}`}
                >
                  <div className="flex items-center justify-between mb-2">
                    <Terminal className={`w-4 h-4 ${notificationSettings.enableServerLogs ? 'text-[var(--brand-primary)]' : 'text-[var(--text-muted)]'}`} />
                    <div className={`w-8 h-4 rounded-full relative transition-colors ${notificationSettings.enableServerLogs ? 'bg-[var(--brand-primary)]' : 'bg-[var(--border-subtle)]'}`}>
                      <div className={`absolute top-0.5 w-3 h-3 bg-white rounded-full transition-all ${notificationSettings.enableServerLogs ? 'left-4.5' : 'left-0.5'}`} />
                    </div>
                  </div>
                  <span className="text-[11px] font-bold text-[var(--text-main)] uppercase tracking-wider mb-1">Server Telemetry</span>
                  <p className="text-[10px] text-[var(--text-muted)] font-thin">Alert on incoming system and audit logs.</p>
                </button>
              </div>
            </div>

            {/* Severity Filter */}
            <div className="space-y-4">
              <div className="flex items-center space-x-3">
                <div className="p-2 bg-[var(--bg-app)] rounded border border-[var(--border-subtle)] text-[var(--text-muted)]">
                  <Shield className="w-4 h-4" />
                </div>
                <span className="text-[10px] font-thin text-[var(--text-muted)] uppercase tracking-[0.2em]">Telemetry Severity Filters</span>
              </div>

              <div className="flex flex-wrap gap-2">
                {['INFO', 'WARN', 'ERROR'].map(level => (
                  <button 
                    key={level}
                    disabled={!notificationSettings.enableServerLogs}
                    onClick={() => {
                      const current = notificationSettings.logLevels;
                      const next = current.includes(level) 
                        ? current.filter(l => l !== level)
                        : [...current, level];
                      updateNotificationSettings({ logLevels: next });
                    }}
                    className={`px-4 py-2 rounded border text-[10px] font-bold uppercase tracking-widest transition-all disabled:opacity-30 ${
                      notificationSettings.logLevels.includes(level)
                        ? 'bg-[var(--brand-primary)] text-white border-[var(--brand-primary)]'
                        : 'bg-[var(--bg-app)] text-[var(--text-muted)] border-[var(--border-subtle)]'
                    }`}
                  >
                    {level === 'INFO' ? 'Information' : level === 'WARN' ? 'Warning' : 'Critical Errors'}
                  </button>
                ))}
              </div>
            </div>

            {/* Hidden Types */}
            {notificationSettings.mutedTypes.length > 0 && (
              <div className="p-4 bg-[var(--bg-app)] rounded border border-[var(--border-subtle)] flex items-center justify-between">
                <div className="flex items-center space-x-3 text-[var(--text-muted)]">
                  <Bell className="w-4 h-4" />
                  <span className="text-[10px] font-thin uppercase tracking-wider">You have {notificationSettings.mutedTypes.length} muted toast categories</span>
                </div>
                <button 
                  onClick={() => updateNotificationSettings({ mutedTypes: [] })}
                  className="text-[9px] font-bold text-[var(--brand-primary)] uppercase hover:underline"
                >
                  Reset Mutes
                </button>
              </div>
            )}
          </div>
        )}
      </div>
      <div className="p-6 bg-[var(--bg-app)] border-t border-[var(--border-muted)] text-center transition-colors">
        <button 
           onClick={() => setStudioSettingsOpen(false)}
           className="px-8 py-2 bg-[var(--brand-primary)] text-white rounded text-[11px] font-bold uppercase tracking-widest hover:bg-[var(--brand-primary-hover)] transition-all shadow-md shadow-emerald-500/10 active:scale-95"
        >
          Persist Preferences
        </button>
      </div>
    </div>
  );
}
