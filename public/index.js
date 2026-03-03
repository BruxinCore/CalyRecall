(function() {
    'use strict';
    const API_URL = "http://localhost:9999";
    const FAB_ID = 'caly-fab';
    let isPromptShowing = false;
    let pendingActionInProgress = false;
    let currentTab = 'backups';
    function ensureCalyStyles() {
        if (document.getElementById('caly-styles')) return;
        const style = document.createElement('style');
        style.id = 'caly-styles';
        style.textContent = `
            @keyframes calyFadeIn { from { opacity: 0; } to { opacity: 1; } }
            @keyframes calySlideUp { 0% { transform: translateY(30px) scale(0.95); opacity: 0; } 100% { transform: translateY(0) scale(1); opacity: 1; } }
            @keyframes calySlideInRight { 0% { transform: translateX(-20px); opacity: 0; } 100% { transform: translateX(0); opacity: 1; } }
            @keyframes calyPulse { 0% { box-shadow: 0 0 0 0 rgba(139, 92, 246, 0.4); } 70% { box-shadow: 0 0 0 10px rgba(139, 92, 246, 0); } 100% { box-shadow: 0 0 0 0 rgba(139, 92, 246, 0); } }
            @keyframes calyPulseRed { 0% { box-shadow: 0 0 0 0 rgba(239, 68, 68, 0.4); } 70% { box-shadow: 0 0 0 10px rgba(239, 68, 68, 0); } 100% { box-shadow: 0 0 0 0 rgba(239, 68, 68, 0); } }
            @keyframes calySpin { 100% { transform: rotate(360deg); } }
            #${FAB_ID} { position: fixed; bottom: 30px; right: 30px; width: 56px; height: 56px; background: linear-gradient(135deg, #7c3aed, #4c1d95); border-radius: 50%; box-shadow: 0 4px 20px rgba(124, 58, 237, 0.5); display: flex; align-items: center; justify-content: center; z-index: 2147483647; cursor: pointer; border: 2px solid rgba(255,255,255,0.2); transition: all 0.3s cubic-bezier(0.34, 1.56, 0.64, 1); color: white; pointer-events: auto !important; }
            #${FAB_ID}:hover { transform: scale(1.15) rotate(10deg); box-shadow: 0 0 30px rgba(139, 92, 246, 0.8); background: linear-gradient(135deg, #8b5cf6, #6d28d9); }
            .caly-overlay { position: fixed; inset: 0; background: rgba(0, 0, 0, 0.75); backdrop-filter: blur(8px); z-index: 2147483647; display: flex; align-items: center; justify-content: center; animation: calyFadeIn 0.3s ease-out; pointer-events: auto !important; }
            .caly-modal { background: linear-gradient(145deg, #1e1b4b 0%, #0f0f16 100%); border: 1px solid rgba(139, 92, 246, 0.3); border-radius: 16px; width: 650px; max-width: 90vw; box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.8), 0 0 0 1px rgba(139, 92, 246, 0.2); animation: calySlideUp 0.4s cubic-bezier(0.16, 1, 0.3, 1); color: #fff; font-family: "Motiva Sans", sans-serif; overflow: hidden; display: flex; flex-direction: column; }
            .caly-header { background: linear-gradient(to right, rgba(46, 16, 101, 0.5), rgba(19, 19, 26, 0.5)); border-bottom: 1px solid rgba(139, 92, 246, 0.15); display: flex; flex-direction: column; }
            .caly-header-top { display: flex; justify-content: space-between; align-items: center; padding: 24px 24px 12px 24px; }
            .caly-title { font-size: 26px; font-weight: 800; background: linear-gradient(135deg, #e9d5ff 0%, #a78bfa 50%, #8b5cf6 100%); -webkit-background-clip: text; -webkit-text-fill-color: transparent; letter-spacing: -0.5px; text-shadow: 0 2px 10px rgba(139, 92, 246, 0.3); }
            .caly-tabs { display: flex; gap: 12px; padding: 0 24px 16px 24px; margin-top: 5px; }
            .caly-tab { padding: 10px 18px; color: #94a3b8; cursor: pointer; border-radius: 10px; border: 1px solid transparent; transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1); font-size: 13px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.8px; display: flex; align-items: center; gap: 10px; background: rgba(255,255,255,0.03); }
            .caly-tab:hover { background: rgba(255,255,255,0.08); color: #e2e8f0; transform: translateY(-1px); }
            .caly-tab.active { background: rgba(139, 92, 246, 0.15); border-color: rgba(139, 92, 246, 0.4); color: #c4b5fd; box-shadow: 0 4px 12px rgba(139, 92, 246, 0.15); }
            .caly-tab svg { width: 18px; height: 18px; opacity: 0.7; transition: 0.2s; }
            .caly-tab.active svg { opacity: 1; stroke: #a78bfa; }
            .caly-body { padding: 0; height: 55vh; max-height: 550px; overflow-y: auto; background: rgba(0,0,0,0.2); }
            .caly-body::-webkit-scrollbar { width: 8px; }
            .caly-body::-webkit-scrollbar-thumb { background: #4c1d95; border-radius: 4px; border: 2px solid transparent; background-clip: content-box; }
            .caly-item { padding: 16px 24px; border-bottom: 1px solid rgba(255,255,255,0.03); display: flex; align-items: center; gap: 18px; transition: all 0.2s; position: relative; }
            .caly-item:hover { background: linear-gradient(90deg, rgba(139, 92, 246, 0.1), transparent); transform: translateX(4px); }
            .caly-game-img { width: 100px; height: 46px; object-fit: cover; border-radius: 6px; box-shadow: 0 4px 6px rgba(0,0,0,0.3); background: #0f0f16; border: 1px solid rgba(255,255,255,0.1); }
            .caly-game-img.large { width: 160px; height: 75px; margin: 0 auto 20px auto; border-radius: 10px; display: block; box-shadow: 0 8px 20px rgba(0,0,0,0.5); }
            .caly-no-icon { width: 100px; height: 46px; background: rgba(255,255,255,0.05); border-radius: 6px; display: flex; align-items: center; justify-content: center; border: 1px dashed rgba(255,255,255,0.2); color: #64748b; }
            .caly-info { display: flex; flex-direction: column; gap: 4px; flex-grow: 1; }
            .caly-main-text { font-size: 16px; font-weight: 700; color: #f1f5f9; text-shadow: 0 1px 2px rgba(0,0,0,0.5); }
            .caly-sub-text { font-size: 12px; color: #94a3b8; font-family: monospace; opacity: 0.8; }
            .caly-actions { display: flex; gap: 10px; align-items: center; }
            .caly-btn { background: linear-gradient(135deg, #7c3aed 0%, #5b21b6 100%); border: 1px solid rgba(139, 92, 246, 0.5); color: white; padding: 10px 20px; border-radius: 8px; font-weight: 700; cursor: pointer; font-size: 13px; display: flex; align-items: center; justify-content: center; gap: 8px; text-transform: uppercase; letter-spacing: 0.5px; transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1); box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06); }
            .caly-btn:hover { filter: brightness(1.2); transform: translateY(-2px); box-shadow: 0 10px 15px -3px rgba(124, 58, 237, 0.4); }
            .caly-btn:active { transform: translateY(0); }
            .caly-btn > svg { width: 18px !important; height: 18px !important; flex: 0 0 auto; }
            .caly-btn.caly-btn--compact { padding: 12px 18px; font-size: 12px; border-radius: 12px; letter-spacing: 0.6px; }
            .caly-btn.confirm { background: linear-gradient(135deg, #16a34a 0%, #15803d 100%); border-color: #22c55e; }
            .caly-btn.confirm:hover { box-shadow: 0 10px 15px -3px rgba(34, 197, 94, 0.25); }
            .caly-btn.secondary { background: transparent; border: 1px solid rgba(255,255,255,0.15); box-shadow: none; }
            .caly-btn.secondary:hover { background: rgba(255,255,255,0.08); border-color: rgba(255,255,255,0.3); }
            .caly-btn.danger { background: rgba(220, 38, 38, 0.1); border: 1px solid rgba(239, 68, 68, 0.4); color: #fca5a5; }
            .caly-btn.danger:hover { background: rgba(220, 38, 38, 0.2); border-color: #f87171; color: #fff; box-shadow: 0 4px 12px rgba(220, 38, 38, 0.2); }
            .caly-icon-btn { background: rgba(255,255,255,0.03); border: 1px solid rgba(255,255,255,0.08); color: #94a3b8; padding: 10px; border-radius: 8px; cursor: pointer; display: flex; align-items: center; justify-content: center; transition: all 0.2s; }
            .caly-icon-btn svg { width: 18px; height: 18px; stroke-width: 2; }
            .caly-icon-btn:hover { background: rgba(255,255,255,0.08); transform: scale(1.05); }
            .caly-icon-btn.del:hover { background: rgba(239, 68, 68, 0.15); border-color: #ef4444; color: #ef4444; }
            .caly-icon-btn.edit:hover { background: rgba(56, 189, 248, 0.15); border-color: #38bdf8; color: #38bdf8; }
            .caly-modal.success { border-color: #22c55e; width: 480px; background: linear-gradient(145deg, #052e16 0%, #022c22 100%); }
            .caly-modal.success .caly-header { background: linear-gradient(90deg, rgba(20, 83, 45, 0.5), transparent); border-bottom-color: rgba(34, 197, 94, 0.3); }
            .caly-modal.success .caly-title { background: linear-gradient(135deg, #86efac 0%, #22c55e 100%); -webkit-background-clip: text; -webkit-text-fill-color: transparent; }
            .caly-success-icon { width: 64px; height: 64px; margin-bottom: 20px; color: #4ade80; animation: calyPulse 2s infinite; stroke-width: 2; }
            .caly-rename-container { padding: 36px; display: flex; flex-direction: column; gap: 20px; }
            .caly-input { background: rgba(0,0,0,0.3); border: 2px solid #4c1d95; color: white; padding: 14px; border-radius: 8px; width: 100%; outline: none; transition: 0.3s; font-size: 15px; }
            .caly-input:focus { border-color: #8b5cf6; background: rgba(0,0,0,0.6); box-shadow: 0 0 0 4px rgba(139, 92, 246, 0.2); }
            .caly-switch { position: relative; display: inline-block; width: 50px; height: 28px; }
            .caly-switch input { opacity: 0; width: 0; height: 0; }
            .caly-slider { position: absolute; cursor: pointer; top: 0; left: 0; right: 0; bottom: 0; background-color: rgba(255,255,255,0.1); transition: .4s; border-radius: 34px; border: 1px solid rgba(255,255,255,0.15); }
            .caly-slider:before { position: absolute; content: ""; height: 20px; width: 20px; left: 3px; bottom: 3px; background-color: #e2e8f0; transition: .4s; border-radius: 50%; box-shadow: 0 2px 4px rgba(0,0,0,0.2); }
            input:checked + .caly-slider { background-color: #7c3aed; border-color: #8b5cf6; }
            input:checked + .caly-slider:before { transform: translateX(22px); background-color: #fff; }
            .caly-spinner { animation: calySpin 1s linear infinite; width: 32px; height: 32px; stroke: #c4b5fd; }
            .caly-prompt-actions { display: flex; gap: 12px; justify-content: center; }
            @media (max-width: 520px) { .caly-prompt-actions { flex-direction: column; } .caly-btn.caly-btn--compact { width: 100%; } }

            .caly-progress-wrap { height: 10px; border-radius: 8px; overflow: hidden; background: rgba(255,255,255,0.08); border: 1px solid rgba(255,255,255,0.12); }
            .caly-progress-bar { height: 100%; width: 0%; background: linear-gradient(135deg, #8b5cf6 0%, #6d28d9 100%); box-shadow: 0 0 12px rgba(139,92,246,0.6) inset; transition: width .25s ease; }
            .caly-export-status { color: #cbd5e1; font-size: 13px; }
            .caly-stats-card { display:flex; flex-direction:column; gap:10px; padding:18px 20px; margin: 14px 20px; border-radius:12px; border:1px solid rgba(139,92,246,0.35); background: linear-gradient(135deg, rgba(46,16,101,0.4), rgba(19,19,26,0.6)); box-shadow: 0 12px 24px rgba(0,0,0,0.35); }
            .caly-stats-top { display:flex; align-items:center; justify-content:space-between; color:#e9d5ff; font-weight:800; font-size:14px; letter-spacing:0.6px; }
            .caly-stats-values { color:#cbd5e1; font-size:12px; }
            .caly-stats-title { display:flex; align-items:center; gap:10px; }
            .caly-stats-title svg { width:18px; height:18px; }
            
            .caly-hotkey-recorder { display: none; } /* Ocultar antigo */
            
            .caly-hotkey-row { display: flex; align-items: center; justify-content: space-between; background: rgba(255,255,255,0.03); border: 1px solid rgba(255,255,255,0.08); border-radius: 12px; padding: 12px 20px; margin-bottom: 20px; transition: all 0.2s; }
            .caly-hotkey-row:hover { background: rgba(255,255,255,0.05); border-color: rgba(255,255,255,0.15); }
            
            .caly-kbd-btn { background: linear-gradient(180deg, #334155 0%, #1e293b 100%); border: 1px solid rgba(255,255,255,0.1); border-bottom-width: 3px; border-radius: 6px; padding: 8px 16px; color: #e2e8f0; font-family: "Consolas", monospace; font-weight: 700; font-size: 13px; cursor: pointer; transition: all 0.1s; min-width: 140px; text-align: center; display: flex; align-items: center; justify-content: center; gap: 8px; position: relative; user-select: none; }
            .caly-kbd-btn:active { transform: translateY(2px); border-bottom-width: 1px; margin-bottom: 2px; }
            .caly-kbd-btn.recording { background: linear-gradient(180deg, #7f1d1d 0%, #450a0a 100%); border-color: #ef4444; color: #fca5a5; animation: calyPulseRed 1.5s infinite; border-bottom-width: 3px; }
            .caly-kbd-icon { opacity: 0.6; width: 14px; height: 14px; }

            .caly-segmented-control { position: relative; display: flex; background: rgba(0,0,0,0.3); border-radius: 12px; padding: 4px; border: 1px solid rgba(255,255,255,0.05); height: 44px; margin-bottom: 12px; }
            .caly-segment-item { flex: 1; display: flex; align-items: center; justify-content: center; gap: 8px; z-index: 2; cursor: pointer; font-size: 13px; font-weight: 600; color: #94a3b8; transition: color 0.3s; user-select: none; border-radius: 8px; }
            .caly-segment-item:hover { color: #cbd5e1; background: rgba(255,255,255,0.02); }
            .caly-segment-item.active { color: #fff; text-shadow: 0 0 10px rgba(139, 92, 246, 0.5); }
            .caly-segment-glider { position: absolute; top: 4px; left: 4px; height: calc(100% - 8px); width: calc(33.33% - 5.33px); background: linear-gradient(135deg, #7c3aed 0%, #5b21b6 100%); border-radius: 8px; z-index: 1; transition: transform 0.4s cubic-bezier(0.34, 1.56, 0.64, 1); box-shadow: 0 2px 10px rgba(124, 58, 237, 0.3); }
            
            .caly-mode-desc-container { margin-top: 12px; padding: 0 4px; display: flex; flex-direction: column; align-items: center; justify-content: center; text-align: center; min-height: 40px; transition: all 0.3s; }
            .caly-desc-title { color: #e2e8f0; font-weight: 700; font-size: 13px; margin-bottom: 4px; letter-spacing: 0.3px; }
            .caly-desc-text { color: #94a3b8; font-size: 12px; line-height: 1.4; max-width: 95%; }
        `;
        document.head.appendChild(style);
    }
    function injectFloatingButton() {
        if (document.getElementById(FAB_ID)) return;
        ensureCalyStyles();
        const fab = document.createElement('div');
        fab.id = FAB_ID;
        fab.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>';
        fab.title = "CalyRecall Dashboard";
        fab.onclick = showDashboard;
        document.body.appendChild(fab);
    }
    function startObserver() {
        injectFloatingButton();
        const observer = new MutationObserver(() => {
            if (document.body && !document.getElementById(FAB_ID)) {
                injectFloatingButton();
            }
            if (isPromptShowing && !document.getElementById('caly-pending-overlay')) {
                isPromptShowing = false;
            }
        });
        if (document.body) {
            observer.observe(document.body, { childList: true, subtree: true });
        }
    }
    function removeOverlay() {
        const existing = document.querySelector('.caly-overlay');
        if (existing) existing.remove();
        isPromptShowing = false;
    }
    function removeOverlayById(id) {
        const el = document.getElementById(id);
        if (el) el.remove();
    }
    function showDashboard() {
        removeOverlay();
        ensureCalyStyles();
        const overlay = document.createElement('div');
        overlay.className = 'caly-overlay';
        overlay.innerHTML = `
            <div class="caly-modal">
                <div class="caly-header">
                    <div class="caly-header-top">
                        <div class="caly-title">CalyRecall</div>
                        <div style="cursor:pointer; padding:8px; opacity:0.6; transition:0.2s; color:#fff; font-size:20px; width:32px; height:32px; display:flex; align-items:center; justify-content:center; border-radius:50%; background:rgba(255,255,255,0.05);" id="caly-close">
                            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                        </div>
                    </div>
                    <div class="caly-tabs">
                        <div class="caly-tab ${currentTab === 'backups' ? 'active' : ''}" data-tab="backups">
                            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>
                            Backups
                        </div>
                        <div class="caly-tab ${currentTab === 'settings' ? 'active' : ''}" data-tab="settings">
                            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="3"></circle><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"></path></svg>
                            Configurações
                        </div>
                    </div>
                </div>
                <div class="caly-body" id="caly-tab-content"></div>
            </div>
        `;
        document.body.appendChild(overlay);
        overlay.onclick = (e) => { if(e.target === overlay) removeOverlay(); };
        overlay.querySelector('#caly-close').onclick = removeOverlay;
        const tabs = overlay.querySelectorAll('.caly-tab');
        tabs.forEach(tab => {
            tab.onclick = () => {
                tabs.forEach(t => t.classList.remove('active'));
                tab.classList.add('active');
                currentTab = tab.getAttribute('data-tab');
                renderTabContent();
            };
        });
        renderTabContent();
    }
    function renderTabContent() {
        const container = document.getElementById('caly-tab-content');
        if (!container) return;
        if (currentTab === 'backups') {
            container.innerHTML = `
                <div id="caly-stats-container" style="min-height: 0px; display: flex; flex-direction: column; align-items: stretch; justify-content: center; transition: min-height 0.3s ease;"></div>
                <div id="caly-backups-list" style="position: relative; min-height: 200px;">
                    <div style="padding:40px; text-align:center; color:#94a3b8; font-style:italic; display:flex; flex-direction:column; align-items:center; gap:15px;">
                        <svg class="caly-spinner" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 12a9 9 0 1 1-6.219-8.56"></path></svg>
                        <div>Carregando backups...</div>
                    </div>
                </div>
            `;
            // Carregar ambos em paralelo sem que um atrapalhe o outro
            fetchStorageStats();
            fetchBackups();
        } else if (currentTab === 'settings') {
            renderSettings(container);
        }
    }
    function renderSettings(container) {
        container.innerHTML = `
            <div style="padding: 36px;">
                <div style="background:rgba(255,255,255,0.03); padding:24px; border-radius:12px; border:1px solid rgba(255,255,255,0.05); margin-bottom: 20px;">
                    <div style="font-weight:bold; color:#fff; font-size:16px; margin-bottom:12px;">Modo de Backup</div>
                    
                    <div class="caly-segmented-control">
                        <div class="caly-segment-glider" id="caly-mode-glider"></div>
                        <div class="caly-segment-item" data-mode="auto">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>
                            Automático
                        </div>
                        <div class="caly-segment-item" data-mode="semi">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path><path d="M9 12l2 2 4-4"></path></svg>
                            Semi-Auto
                        </div>
                        <div class="caly-segment-item" data-mode="manual">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="4.93" y1="4.93" x2="19.07" y2="19.07"></line></svg>
                            Manual
                        </div>
                    </div>

                    <div class="caly-mode-desc-container" id="caly-mode-desc">
                        Selecione um modo.
                    </div>
                </div>

                <div class="caly-hotkey-row">
                    <div>
                        <div style="font-weight:bold; color:#fff; font-size:14px; margin-bottom:4px; display:flex; align-items:center; gap:8px;">
                            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" style="color:#f59e0b;"><polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"></polygon></svg>
                            Quick-Save Global
                        </div>
                        <div style="font-size:12px; color:#94a3b8;">Atalho para backup instantâneo.</div>
                    </div>
                    
                    <div style="display: flex; align-items: center; gap: 12px;">
                        <label class="caly-switch" style="transform: scale(0.85);">
                            <input type="checkbox" id="caly-hotkey-toggle">
                            <span class="caly-slider"></span>
                        </label>
                        <div id="caly-hotkey-recorder" class="caly-kbd-btn">
                            <svg class="caly-kbd-icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="4" width="20" height="16" rx="2" ry="2"></rect><path d="M6 12h.01"></path><path d="M10 12h.01"></path><path d="M14 12h.01"></path><path d="M18 12h.01"></path></svg>
                            <span id="caly-hotkey-label">Configurar</span>
                        </div>
                    </div>
                </div>

                <div style="background:rgba(255,255,255,0.03); padding:24px; border-radius:12px; border:1px solid rgba(255,255,255,0.05);">
                    <div style="font-weight:bold; color:#fff; font-size:16px; margin-bottom:10px;">Portabilidade</div>
                    <div style="font-size:13px; color:#94a3b8; margin-bottom:16px;">Exporte todos os backups para um arquivo .zip e importe novamente quando quiser.</div>
                    <div style="display:flex; gap:12px; align-items:center; flex-wrap:wrap;">
                        <button class="caly-btn" id="caly-export-all" style="background: linear-gradient(135deg, #0ea5e9 0%, #0369a1 100%); border-color: #0ea5e9;">
                            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>
                            Exportar Backups
                        </button>
                        <button class="caly-btn" id="caly-import-zip" style="background: linear-gradient(135deg, #22c55e 0%, #16a34a 100%); border-color: #22c55e;">
                            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 15v4a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>
                            Importar .zip
                        </button>
                        <input type="file" id="caly-import-input" accept=".zip" style="display:none">
                    </div>
                    <div id="caly-portability-status" style="margin-top:12px; font-size:12px; color:#94a3b8;"></div>
                </div>
            </div>
        `;
        // const toggle = container.querySelector('#caly-semi-auto-toggle'); REMOVIDO
        const hkToggle = container.querySelector('#caly-hotkey-toggle');
        const recorder = container.querySelector('#caly-hotkey-recorder');
        const hkLabel = container.querySelector('#caly-hotkey-label');
        
        const exportBtn = container.querySelector('#caly-export-all');
        const importBtn = container.querySelector('#caly-import-zip');
        const importInput = container.querySelector('#caly-import-input');
        const statusBox = container.querySelector('#caly-portability-status');
        
        const modeCards = container.querySelectorAll('.caly-segment-item');
        const modeDesc = container.querySelector('#caly-mode-desc');
        const modeGlider = container.querySelector('#caly-mode-glider');
        
        const DESCRIPTIONS = {
            'auto': '<div class="caly-desc-title">Automático (Padrão)</div><div class="caly-desc-text">O backup é criado automaticamente assim que você fecha o jogo.</div>',
            'semi': '<div class="caly-desc-title">Semi-Automático</div><div class="caly-desc-text">O CalyRecall pergunta se você deseja criar um backup ao fechar o jogo.</div>',
            'manual': '<div class="caly-desc-title">Manual / Desativado</div><div class="caly-desc-text">Use o atalho (Quick-Save) ou o botão manual para salvar.</div>'
        };
        
        let currentHotkey = { enabled: false, mod: 0, vk: 0, str: "" };
        
        // Função para atualizar UI dos modos
        const setModeUI = (mode) => {
            let index = 0;
            if (mode === 'semi') index = 1;
            if (mode === 'manual') index = 2;
            
            // Move o glider
            if (modeGlider) {
                modeGlider.style.transform = `translateX(${index * 100}%)`;
            }
            
            modeCards.forEach(c => {
                if (c.dataset.mode === mode) c.classList.add('active');
                else c.classList.remove('active');
            });
            
            modeDesc.style.opacity = '0';
            setTimeout(() => { 
                modeDesc.innerHTML = DESCRIPTIONS[mode] || DESCRIPTIONS['auto'];
                modeDesc.style.opacity = '1'; 
            }, 150);
        };
        
        fetch(`${API_URL}/settings`).then(res => res.json()).then(data => { 
            // Modos
            const mode = data.backup_mode || (data.semi_auto ? 'semi' : 'auto');
            setModeUI(mode);
            
            // Hotkey
            hkToggle.checked = !!data.hotkey_enabled;
            currentHotkey.enabled = !!data.hotkey_enabled;
            currentHotkey.mod = data.hotkey_mod || 0;
            currentHotkey.vk = data.hotkey_vk || 0;
            currentHotkey.str = data.hotkey_str || ""; // Carregar string salva
            
            if (currentHotkey.str) {
                hkLabel.textContent = currentHotkey.str;
                hkLabel.style.color = '#c4b5fd';
            }
        });

        // Eventos dos modos
        modeCards.forEach(card => {
            card.onclick = () => {
                const mode = card.dataset.mode;
                setModeUI(mode);
                fetch(`${API_URL}/settings`, { 
                    method: 'POST', 
                    headers: {'Content-Type': 'application/json'}, 
                    body: JSON.stringify({ backup_mode: mode }) 
                });
            };
        });
        
        const saveHotkey = () => {
             fetch(`${API_URL}/settings/hotkey`, { 
                 method: 'POST', 
                 headers: {'Content-Type': 'application/json'}, 
                 body: JSON.stringify({ 
                     enabled: hkToggle.checked, 
                     mod: currentHotkey.mod, 
                     vk: currentHotkey.vk,
                     str: currentHotkey.str 
                 }) 
             });
        };

        hkToggle.addEventListener('change', (e) => {
            currentHotkey.enabled = e.target.checked;
            saveHotkey();
        });
        
        // Lógica de gravação de Hotkey (Mantida a mesma, apenas garantindo que currentHotkey.str seja atualizado)
        let isRecording = false;
        
        recorder.onclick = () => {
            if (isRecording) return;
            isRecording = true;
            recorder.classList.add('recording');
            hkLabel.textContent = "Pressione a combinação...";
            hkLabel.style.color = "#ef4444";
            
            // Input invisível para forçar captura de foco do teclado
            const focusTrap = document.createElement('input');
            focusTrap.style.position = 'fixed';
            focusTrap.style.opacity = '0';
            focusTrap.style.top = '-1000px';
            document.body.appendChild(focusTrap);
            focusTrap.focus();

            const keyHandler = (e) => {
                e.preventDefault();
                e.stopPropagation();
                
                // Ignorar apenas teclas modificadoras (esperar a tecla final)
                if (['Control', 'Shift', 'Alt', 'Meta'].includes(e.key)) return;
                
                // Calcular Modificadores (Win32)
                let mod = 0;
                if (e.altKey) mod |= 1;
                if (e.ctrlKey) mod |= 2;
                if (e.shiftKey) mod |= 4;
                if (e.metaKey) mod |= 8;
                
                // Obter código VK
                // Usar keyCode é mais seguro para compatibilidade com user32
                let vk = e.keyCode; 
                
                // Montar string bonita
                let parts = [];
                if (e.ctrlKey) parts.push("Ctrl");
                if (e.shiftKey) parts.push("Shift");
                if (e.altKey) parts.push("Alt");
                if (e.metaKey) parts.push("Win");
                
                let keyName = e.key.toUpperCase();
                if (e.code === 'Space') keyName = 'SPACE';
                if (keyName.length === 1) keyName = keyName.toUpperCase();
                
                // Mapeamentos manuais para teclas comuns que o e.key pode retornar estranho
                const keyMap = {
                    'ESCAPE': 'ESC', 'DELETE': 'DEL', 'INSERT': 'INS', 
                    'ARROWUP': 'UP', 'ARROWDOWN': 'DOWN', 'ARROWLEFT': 'LEFT', 'ARROWRIGHT': 'RIGHT' 
                };
                if (keyMap[keyName]) keyName = keyMap[keyName];

                parts.push(keyName);
                
                const finalStr = parts.join(" + ");
                
                // Atualizar estado
                currentHotkey.mod = mod;
                currentHotkey.vk = vk;
                currentHotkey.str = finalStr;
                
                // UI Update
                hkLabel.textContent = finalStr;
                hkLabel.style.color = "#c4b5fd";
                recorder.classList.remove('recording');
                
                // Cleanup
                isRecording = false;
                window.removeEventListener('keydown', keyHandler);
                document.removeEventListener('keydown', keyHandler); // Garantia extra
                if (document.body.contains(focusTrap)) document.body.removeChild(focusTrap);
                
                // Salvar e ativar automaticamente se não estiver
                if (!hkToggle.checked) {
                    hkToggle.checked = true;
                    currentHotkey.enabled = true;
                }
                saveHotkey();
            };
            
            // Adicionar em ambos para garantir
            window.addEventListener('keydown', keyHandler, { capture: true });
            document.addEventListener('keydown', keyHandler, { capture: true });
            
            // Cancelar se clicar fora
            const outsideClick = (e) => {
                if (!recorder.contains(e.target) && isRecording) {
                    isRecording = false;
                    recorder.classList.remove('recording');
                    hkLabel.textContent = currentHotkey.str || "Clique para configurar atalho";
                    hkLabel.style.color = "#c4b5fd";
                    
                    window.removeEventListener('keydown', keyHandler);
                    document.removeEventListener('keydown', keyHandler);
                    if (document.body.contains(focusTrap)) document.body.removeChild(focusTrap);
                    
                    document.removeEventListener('click', outsideClick);
                }
            };
            setTimeout(() => document.addEventListener('click', outsideClick), 100);
            
            // Fallback de timeout para não ficar travado
            focusTrap.onblur = () => {
                // Se perdeu foco (ex: alt tab), cancela
                setTimeout(() => {
                    if (isRecording && document.activeElement !== focusTrap) {
                        // Tenta recuperar foco uma vez
                        focusTrap.focus();
                    }
                }, 50);
            };
        };

        exportBtn.onclick = async () => {
            try {
                showExportProgressModal();
                const res = await fetch(`${API_URL}/export/start`, {
                    method: 'POST',
                    headers: {'Content-Type':'application/json'},
                    body: JSON.stringify({ scope: 'all' })
                });
                if (!res.ok) { updateExportModalStatus('Falha ao iniciar exportação.'); return; }
                const { id } = await res.json();
                startExportPolling(id);
            } catch (e) {
                updateExportModalStatus('Erro ao exportar.');
            }
        };
        importBtn.onclick = () => importInput.click();
        importInput.onchange = async () => {
            const file = importInput.files && importInput.files[0];
            if (!file) return;
            showImportProgressModal();
            startImportUpload(file);
        };
    }

    let exportPollTimer = null;
    function showExportProgressModal() {
        removeOverlay();
        ensureCalyStyles();
        const overlay = document.createElement('div');
        overlay.className = 'caly-overlay';
        overlay.id = 'caly-export-overlay';
        overlay.innerHTML = `
            <div class="caly-modal" style="width: 520px;">
                <div class="caly-header">
                    <div class="caly-header-top">
                        <div class="caly-title">Exportando Backups</div>
                        <div style="cursor:pointer; padding:8px; opacity:0.6;" id="caly-close">
                            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                        </div>
                    </div>
                </div>
                <div class="caly-body" style="height:auto; padding:32px;">
                    <div class="caly-export-status" id="caly-export-status">Preparando...</div>
                    <div style="margin-top:12px;" class="caly-progress-wrap">
                        <div class="caly-progress-bar" id="caly-progress-bar"></div>
                    </div>
                    <div id="caly-export-numbers" style="margin-top:8px; font-size:12px; color:#94a3b8;">0%</div>
                </div>
            </div>
        `;
        document.body.appendChild(overlay);
        overlay.querySelector('#caly-close').onclick = removeOverlay;
    }
    function updateExportModalStatus(text) {
        const el = document.getElementById('caly-export-status');
        if (el) el.textContent = text;
    }
    function startExportPolling(id) {
        updateExportModalStatus('Compactando backups...');
        const bar = document.getElementById('caly-progress-bar');
        const nums = document.getElementById('caly-export-numbers');
        let failCount = 0;
        exportPollTimer = setInterval(async () => {
            try {
                const res = await fetch(`${API_URL}/export/progress/${id}`);
                if (!res.ok) {
                    failCount++;
                    if (failCount >= 10) {
                        clearInterval(exportPollTimer);
                        exportPollTimer = null;
                        updateExportModalStatus('Falha ao consultar progresso.');
                    }
                    return;
                }
                const data = await res.json();
                const total = Math.max(1, data.total_bytes || 1);
                const done = data.done_bytes || 0;
                const pct = Math.min(100, Math.round((done / total) * 100));
                if (bar) bar.style.width = pct + '%';
                if (nums) nums.textContent = `${pct}% • ${data.done_files || 0}/${data.total_files || 0}`;
                if (data.status === 'ready') {
                    clearInterval(exportPollTimer);
                    exportPollTimer = null;
                    updateExportModalStatus('Concluído. Baixando arquivo...');
                    const down = await fetch(`${API_URL}/export/download/${id}`);
                    if (down.ok) {
                        const blob = await down.blob();
                        const url = URL.createObjectURL(blob);
                        const a = document.createElement('a');
                        a.href = url;
                        a.download = data.filename || 'calyrecall-backups-all.zip';
                        document.body.appendChild(a);
                        a.click();
                        a.remove();
                        URL.revokeObjectURL(url);
                        updateExportModalStatus('Exportação concluída.');
                        if (bar) bar.style.width = '100%';
                        if (nums) nums.textContent = `100% • ${(data.total_files || 0)}/${(data.total_files || 0)}`;
                        setTimeout(() => { removeOverlayById('caly-export-overlay'); }, 1500);
                    } else {
                        updateExportModalStatus('Falha ao baixar arquivo.');
                    }
                } else if (data.status === 'error') {
                    clearInterval(exportPollTimer);
                    exportPollTimer = null;
                    updateExportModalStatus('Erro durante exportação.');
                }
            } catch (e) {
                failCount++;
                if (failCount >= 10) {
                    clearInterval(exportPollTimer);
                    exportPollTimer = null;
                    updateExportModalStatus('Erro durante exportação.');
                }
            }
        }, 500);
    }
    function showImportProgressModal() {
        removeOverlay();
        ensureCalyStyles();
        const overlay = document.createElement('div');
        overlay.className = 'caly-overlay';
        overlay.id = 'caly-import-overlay';
        overlay.innerHTML = `
            <div class="caly-modal" style="width: 520px;">
                <div class="caly-header">
                    <div class="caly-header-top">
                        <div class="caly-title">Importando Backups</div>
                        <div style="cursor:pointer; padding:8px; opacity:0.6;" id="caly-close">
                            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                        </div>
                    </div>
                </div>
                <div class="caly-body" style="height:auto; padding:32px;">
                    <div class="caly-export-status" id="caly-import-status">Enviando arquivo...</div>
                    <div style="margin-top:12px;" class="caly-progress-wrap">
                        <div class="caly-progress-bar" id="caly-import-progress"></div>
                    </div>
                    <div id="caly-import-numbers" style="margin-top:8px; font-size:12px; color:#94a3b8;">0%</div>
                </div>
            </div>
        `;
        document.body.appendChild(overlay);
        overlay.querySelector('#caly-close').onclick = removeOverlay;
    }
    function startImportUpload(file) {
        const bar = document.getElementById('caly-import-progress');
        const nums = document.getElementById('caly-import-numbers');
        const statusEl = document.getElementById('caly-import-status');
        const xhr = new XMLHttpRequest();
        xhr.open('POST', `${API_URL}/import`, true);
        xhr.setRequestHeader('Content-Type', 'application/zip');
        xhr.upload.onprogress = (e) => {
            if (!e.lengthComputable) return;
            const pct = Math.min(100, Math.round((e.loaded / e.total) * 100));
            if (bar) bar.style.width = pct + '%';
            if (nums) nums.textContent = `${pct}% • Enviando`;
        };
        xhr.onreadystatechange = () => {
            if (xhr.readyState === 4) {
                if (xhr.status === 200) {
                    try {
                        const data = JSON.parse(xhr.responseText || '{}');
                        if (statusEl) statusEl.textContent = 'Importação concluída.';
                        if (bar) bar.style.width = '100%';
                        if (nums) nums.textContent = `100% • Backups: ${data.files || 0}`;
                        setTimeout(() => {
                            removeOverlayById('caly-import-overlay');
                            currentTab = 'backups';
                            showDashboard();
                        }, 1200);
                    } catch {
                        if (statusEl) statusEl.textContent = 'Importação concluída.';
                        setTimeout(() => {
                            removeOverlayById('caly-import-overlay');
                            currentTab = 'backups';
                            showDashboard();
                        }, 1200);
                    }
                } else {
                    if (statusEl) statusEl.textContent = 'Erro ao importar.';
                }
            }
        };
        file.arrayBuffer().then(buf => {
            xhr.send(new Uint8Array(buf));
        }).catch(() => {
            if (statusEl) statusEl.textContent = 'Erro ao preparar arquivo.';
        });
    }
    async function fetchBackups() {
        const listContainer = document.getElementById('caly-backups-list');
        if (!listContainer) return;
        
        try {
            const res = await fetch(`${API_URL}/list`);
            const backups = await res.json();
            
            if (!backups || backups.length === 0) {
                listContainer.innerHTML = `<div style="padding:60px; text-align:center; color:#64748b; font-size:15px;">Nenhum snapshot localizado na linha do tempo.</div>`;
                return;
            }
            
            listContainer.innerHTML = '';
            backups.forEach((data, index) => {
                const item = document.createElement('div');
                item.className = 'caly-item';
                item.style.animation = `calySlideInRight 0.3s ease-out forwards`;
                item.style.animationDelay = `${index * 0.05}s`;
                item.style.opacity = '0'; // Começa invisível para a animação funcionar corretamente
                
                let folderName = data.folder;
                let nickname = data.nickname;
                let gameName = data.game_name || "Desconhecido";
                let appid = data.appid;
                let raw = folderName.replace('CalyBackup-', '');
                let dateStr = raw;
                if (raw.includes('_')) {
                    let [d, t] = raw.split('_');
                    dateStr = `${d.replace(/-/g, '/')} ${t.substring(0, 5).replace('-', ':')}`;
                }
                let mainText = nickname ? nickname : gameName;
                if (!nickname && !data.game_name) mainText = "Backup Automático";
                let subText = dateStr;
                if (nickname) subText += ` • ${gameName}`;
                let imgHtml = `<div class="caly-no-icon"><svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="6" y1="11" x2="10" y2="11"></line><line x1="8" y1="9" x2="8" y2="13"></line><line x1="15" y1="12" x2="15.01" y2="12"></line><line x1="18" y1="10" x2="18.01" y2="10"></line><path d="M17.32 5H6.68a4 4 0 0 0-3.978 3.59c-.006.052-.01.101-.017.152C2.604 9.416 2 14.456 2 16a3 3 0 0 0 3 3c1 0 1.5-.5 2-1l1.414-1.414A2 2 0 0 1 9.828 16h4.344a2 2 0 0 1 1.414.586L17 18c.5.5 1 1 2 1a3 3 0 0 0 3-3c0-1.545-.604-6.584-.685-7.258-.007-.05-.011-.1-.017-.151A4 4 0 0 0 17.32 5z"></path></svg></div>`;
                if (appid && appid !== 0) {
                    imgHtml = `<img src="https://cdn.cloudflare.steamstatic.com/steam/apps/${appid}/capsule_sm_120.jpg" class="caly-game-img" onerror="this.style.display='none'">`;
                }
                item.innerHTML = `
                    ${imgHtml}
                    <div class="caly-info">
                        <span class="caly-main-text">${mainText}</span>
                        <span class="caly-sub-text">${subText}</span>
                    </div>
                    <div class="caly-actions">
                        <div class="caly-icon-btn edit" title="Renomear">
                            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
                        </div>
                        <div class="caly-icon-btn del" title="Apagar">
                            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path><line x1="10" y1="11" x2="10" y2="17"></line><line x1="14" y1="11" x2="14" y2="17"></line></svg>
                        </div>
                        <button class="caly-btn" title="Restaurar">
                            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="23 4 23 10 17 10"></polyline><polyline points="1 20 1 14 7 14"></polyline><path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path></svg>
                            Restaurar
                        </button>
                    </div>
                `;
                const restoreBtn = item.querySelector('.caly-btn');
                const deleteBtn = item.querySelector('.caly-icon-btn.del');
                const editBtn = item.querySelector('.caly-icon-btn.edit');
                restoreBtn.onclick = () => triggerRestore(folderName, restoreBtn);
                deleteBtn.onclick = () => triggerDelete(folderName, item);
                editBtn.onclick = () => showRenameModal(folderName, nickname);
                listContainer.appendChild(item);
            });
        } catch (e) {
            listContainer.innerHTML = `<div style="padding:40px; text-align:center; color:#ef4444">Erro ao conectar no servidor local.</div>`;
        }
    }
    async function fetchStorageStats() {
        const statsContainer = document.getElementById('caly-stats-container');
        if (!statsContainer) return;
        
        try {
            const res = await fetch(`${API_URL}/stats`);
            if (!res.ok) return;
            const s = await res.json();
            const backupGB = (s.backup_bytes || 0) / (1024*1024*1024);
            const diskTotalGB = (s.disk_total_bytes || 0) / (1024*1024*1024);
            const diskFreeGB = (s.disk_free_bytes || 0) / (1024*1024*1024);
            const pct = diskTotalGB > 0 ? Math.min(100, Math.round((backupGB / diskTotalGB) * 100)) : 0;
            
            // Design "Pílula" Flutuante
            statsContainer.style.opacity = '0';
            statsContainer.style.minHeight = 'auto'; 
            statsContainer.style.marginTop = '15px'; // Espaço superior sutil
            
            // Container para centralizar a pílula
            statsContainer.innerHTML = `
                <div style="
                    display: flex; 
                    justify-content: center; 
                    width: 100%;
                    animation: calyFadeIn 0.5s ease-out forwards;
                ">
                    <div class="caly-stats-pill" style="
                        background: linear-gradient(135deg, rgba(139, 92, 246, 0.1), rgba(76, 29, 149, 0.2)); 
                        border: 1px solid rgba(139, 92, 246, 0.25); 
                        border-radius: 9999px; 
                        padding: 8px 20px; 
                        display: inline-flex; 
                        align-items: center; 
                        gap: 16px;
                        box-shadow: 0 4px 20px rgba(0,0,0,0.25);
                        position: relative;
                        overflow: hidden;
                        backdrop-filter: blur(4px);
                        transition: all 0.3s ease;
                        white-space: nowrap;
                    ">
                        <!-- Barra de progresso de fundo -->
                        <div id="caly-pill-bg" style="
                            position: absolute; 
                            left: 0; top: 0; bottom: 0; 
                            width: 0%; 
                            background: linear-gradient(90deg, rgba(139, 92, 246, 0.15), rgba(167, 139, 250, 0.1)); 
                            transition: width 1s cubic-bezier(0.34, 1.56, 0.64, 1);
                            pointer-events: none;
                            z-index: 0;
                        "></div>
                        
                        <!-- Conteúdo (z-index garante que fique sobre o fundo) -->
                        <div style="display: flex; align-items: center; gap: 8px; z-index: 1;">
                            <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" style="color: #c4b5fd;"><rect x="2" y="2" width="20" height="8" rx="2" ry="2"></rect><rect x="2" y="14" width="20" height="8" rx="2" ry="2"></rect><line x1="6" y1="6" x2="6.01" y2="6"></line><line x1="6" y1="18" x2="6.01" y2="18"></line></svg>
                            <span style="font-size: 13px; color: #f1f5f9; font-weight: 700; letter-spacing: 0.3px;">${backupGB.toFixed(2)} GB</span>
                        </div>
                        
                        <div style="width: 1px; height: 14px; background: rgba(255,255,255,0.1); z-index: 1;"></div>
                        
                        <div style="font-size: 12px; color: #cbd5e1; font-weight: 500; z-index: 1;">
                            ${s.backup_count || 0} backups
                        </div>

                        <div style="width: 1px; height: 14px; background: rgba(255,255,255,0.1); z-index: 1;"></div>

                        <div style="font-size: 12px; color: #94a3b8; z-index: 1; display: flex; align-items: center; gap: 6px;">
                            <span>Livre:</span>
                            <span style="color: #cbd5e1; font-weight: 600;">${diskFreeGB.toFixed(1)} GB</span>
                        </div>
                    </div>
                </div>
            `;
            
            // Trigger da animação de fade
            requestAnimationFrame(() => {
                statsContainer.style.transition = 'opacity 0.4s ease';
                statsContainer.style.opacity = '1';
            });
            
            // Animação da barra de fundo
            const bgBar = document.getElementById('caly-pill-bg');
            setTimeout(() => { if (bgBar) bgBar.style.width = Math.max(5, pct) + '%'; }, 150);
            
            // Efeito hover simples na pílula via JS
            const pill = statsContainer.querySelector('.caly-stats-pill');
            if (pill) {
                pill.onmouseenter = () => { pill.style.transform = 'translateY(-2px) scale(1.02)'; pill.style.boxShadow = '0 8px 25px rgba(139, 92, 246, 0.2)'; pill.style.borderColor = 'rgba(139, 92, 246, 0.5)'; };
                pill.onmouseleave = () => { pill.style.transform = 'translateY(0) scale(1)'; pill.style.boxShadow = '0 4px 20px rgba(0,0,0,0.25)'; pill.style.borderColor = 'rgba(139, 92, 246, 0.25)'; };
            }

        } catch(e) {
            statsContainer.innerHTML = ''; 
            statsContainer.style.minHeight = '0'; 
            statsContainer.style.marginTop = '0';
        }
    }
    function showPendingPromptModal(gameName, appid) {
        removeOverlay();
        isPromptShowing = true;
        ensureCalyStyles();
        const overlay = document.createElement('div');
        overlay.className = 'caly-overlay';
        overlay.id = 'caly-pending-overlay';
        let imgHtml = '';
        if (appid && appid !== 0) {
            imgHtml = `<img src="https://cdn.cloudflare.steamstatic.com/steam/apps/${appid}/capsule_sm_120.jpg" class="caly-game-img large" onerror="this.style.display='none'">`;
        }
        overlay.innerHTML = `
            <div class="caly-modal" style="width: 480px; text-align: center; border-color: #8b5cf6;">
                <div class="caly-body" style="padding: 48px 36px; height: auto; max-height: none; background: transparent;">
                    ${imgHtml}
                    <h2 style="margin:0 0 12px 0; font-size: 22px; color: #fff; font-weight: 800;">Sessão Encerrada</h2>
                    <p style="color: #cbd5e1; font-size: 15px; margin-bottom: 32px; line-height: 1.6;">Você fechou <strong>${gameName}</strong>.<br>Deseja criar um backup de segurança deste save?</p>
                    <div class="caly-prompt-actions">
                        <button class="caly-btn danger caly-btn--compact" id="caly-btn-ignore"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="15" y1="9" x2="9" y2="15"></line><line x1="9" y1="9" x2="15" y2="15"></line></svg>Ignorar</button>
                        <button class="caly-btn confirm caly-btn--compact" id="caly-btn-confirm"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"></path><polyline points="17 21 17 13 7 13 7 21"></polyline><polyline points="7 3 7 8 15 8"></polyline></svg>Salvar</button>
                    </div>
                </div>
            </div>
        `;
        document.body.appendChild(overlay);
        overlay.querySelector('#caly-btn-confirm').onclick = async () => {
            pendingActionInProgress = true;
            overlay.querySelector('#caly-btn-confirm').innerHTML = `<svg class="caly-spinner" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="stroke:white;"><path d="M21 12a9 9 0 1 1-6.219-8.56"></path></svg> Salvando...`;
            await fetch(`${API_URL}/pending/action`, { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({ action: "confirm", appid: appid, game_name: gameName }) });
            removeOverlay();
            setTimeout(() => { pendingActionInProgress = false; }, 2000);
        };
        overlay.querySelector('#caly-btn-ignore').onclick = async () => {
            pendingActionInProgress = true;
            await fetch(`${API_URL}/pending/action`, { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({ action: "cancel" }) });
            removeOverlay();
            setTimeout(() => { pendingActionInProgress = false; }, 2000);
        };
    }
    function showRenameModal(folder, currentName) {
        ensureCalyStyles();
        const overlay = document.createElement('div');
        overlay.className = 'caly-overlay';
        overlay.style.zIndex = "2147483648";
        overlay.innerHTML = `
            <div class="caly-modal" style="width: 500px;">
                <div class="caly-header">
                    <div class="caly-header-top" style="padding-bottom:20px;">
                        <div class="caly-title">Renomear Backup</div>
                        <div style="cursor:pointer; padding:8px; display:flex;" id="caly-close">
                            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                        </div>
                    </div>
                </div>
                <div class="caly-body" style="height: auto;">
                    <div class="caly-rename-container">
                        <div style="font-size:14px; color:#cbd5e1; font-weight:600;">Defina um apelido para este save:</div>
                        <input type="text" class="caly-input" id="caly-rename-input" placeholder="Ex: Antes do Boss Final" value="${currentName || ''}" autocomplete="off">
                        <div style="display: flex; justify-content: flex-end; gap: 12px; margin-top: 10px;">
                            <button class="caly-btn secondary" id="caly-cancel-btn">Cancelar</button>
                            <button class="caly-btn" id="caly-save-btn">Salvar Alterações</button>
                        </div>
                    </div>
                </div>
            </div>
        `;
        document.body.appendChild(overlay);
        const input = overlay.querySelector('#caly-rename-input');
        const saveBtn = overlay.querySelector('#caly-save-btn');
        setTimeout(() => input.focus(), 100);
        const closeMod = () => overlay.remove();
        const submit = () => { executeRename(folder, input.value); closeMod(); };
        saveBtn.onclick = submit;
        overlay.querySelector('#caly-cancel-btn').onclick = closeMod;
        overlay.querySelector('#caly-close').onclick = closeMod;
        input.onkeydown = (e) => { if(e.key === 'Enter') submit(); };
    }
    function showSuccessModal() {
        removeOverlay();
        ensureCalyStyles();
        const overlay = document.createElement('div');
        overlay.className = 'caly-overlay';
        overlay.innerHTML = `
            <div class="caly-modal success">
                <div class="caly-header" style="border-bottom:none; padding-bottom:0;">
                    <div class="caly-header-top">
                        <div class="caly-title" style="color:#22c55e;">Sucesso</div>
                        <div style="cursor:pointer; padding:5px; display:flex;" id="caly-close">
                            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                        </div>
                    </div>
                </div>
                <div class="caly-body" style="height: auto;">
                    <div style="padding:20px 48px 48px 48px; text-align:center;">
                        <svg class="caly-success-icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>
                        <div style="font-size:20px; font-weight:800; color:#fff; margin-bottom:8px;">Backup Restaurado!</div>
                        <div style="font-size:14px; color:#86efac; margin-bottom:24px;">Seu jogo está pronto para ser carregado.</div>
                        <button class="caly-btn" id="caly-ok-btn" style="margin-top:10px; width:100%; background: #16a34a; border-color: #15803d; box-shadow: 0 4px 12px rgba(22, 163, 74, 0.4);">ENTENDIDO</button>
                    </div>
                </div>
            </div>
        `;
        document.body.appendChild(overlay);
        overlay.querySelector('#caly-close').onclick = removeOverlay;
        overlay.querySelector('#caly-ok-btn').onclick = removeOverlay;
    }
    async function checkStartupStatus() {
        setTimeout(async () => {
            try {
                const res = await fetch(`${API_URL}/check_restore`);
                const data = await res.json();
                if (data.restored === true) showSuccessModal();
            } catch (e) { }
        }, 1500);
    }
    async function executeRename(folder, newName) {
        try {
            const res = await fetch(`${API_URL}/rename`, { method: 'POST', headers: {'Content-Type': 'application/json'}, body: JSON.stringify({ folder: folder, new_name: newName }) });
            if (res.ok) { renderTabContent(); }
        } catch(e) { alert("Erro: " + e); }
    }
    async function triggerRestore(folder, btnElement) {
        if(!confirm(`Deseja restaurar este backup?\nA Steam irá reiniciar.`)) return;
        btnElement.innerHTML = `<svg class="caly-spinner" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="stroke:white;"><path d="M21 12a9 9 0 1 1-6.219-8.56"></path></svg>`;
        btnElement.disabled = true;
        try {
            const response = await fetch(`${API_URL}/restore/${folder}`, { method: 'POST' });
            if (response.ok) {
                const body = document.getElementById('caly-tab-content');
                if(body) body.innerHTML = `<div style="padding:60px; text-align:center; color:#c4b5fd; display:flex; flex-direction:column; align-items:center; gap:15px;"><svg class="caly-spinner" xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 12a9 9 0 1 1-6.219-8.56"></path></svg><div style="font-size:18px; font-weight:bold;">Iniciando Protocolo...</div></div>`;
            } else { alert("Erro no servidor."); btnElement.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="23 4 23 10 17 10"></polyline><polyline points="1 20 1 14 7 14"></polyline><path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path></svg>'; btnElement.disabled = false; }
        } catch(e) { alert(e); btnElement.disabled = false; }
    }
    async function triggerDelete(folder, itemElement) {
        if(!confirm(`Apagar permanentemente este backup?`)) return;
        itemElement.style.opacity = '0.5';
        try {
            const response = await fetch(`${API_URL}/delete/${folder}`, { method: 'POST' });
            if (response.ok) {
                itemElement.remove();
                const container = document.getElementById('caly-tab-content');
                if(container && container.children.length === 0) fetchBackups(container);
            } else { alert("Erro ao deletar."); itemElement.style.opacity = '1'; }
        } catch(e) { alert(e); itemElement.style.opacity = '1'; }
    }
    function initSafe() {
        if (!document.body) {
            setTimeout(initSafe, 100);
            return;
        }
        startObserver();
        checkStartupStatus();
        setInterval(async () => {
            if (isPromptShowing || pendingActionInProgress) return;
            try {
                const res = await fetch(`${API_URL}/pending`);
                const data = await res.json();
                if (data.pending) {
                    showPendingPromptModal(data.game_name, data.appid);
                }
            } catch (e) { }
        }, 1000);
    }
    initSafe();
})();
