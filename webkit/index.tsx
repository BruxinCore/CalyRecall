import { Millennium } from '@millennium/ui';

const injectStyle = () => {
    if (document.getElementById('calyrecall-styles')) return;

    const style = document.createElement('style');
    style.id = 'calyrecall-styles';
    style.textContent = `
        .calyrecall-dot {
            width: 8px;
            height: 8px;
            background-color: #8b5cf6;
            border-radius: 50%;
            position: absolute;
            top: 4px;
            right: 4px;
            z-index: 99999;
            box-shadow: 0 0 5px rgba(139, 92, 246, 0.8);
            pointer-events: none;
            animation: pulse 2s infinite;
        }
        
        @keyframes pulse {
            0% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(139, 92, 246, 0.7); }
            70% { transform: scale(1); box-shadow: 0 0 0 4px rgba(139, 92, 246, 0); }
            100% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(139, 92, 246, 0); }
        }
    `;
    document.head.appendChild(style);
};

const findStoreTab = (): HTMLElement | null => {
    const modernNavStore = document.querySelector('a[href*="steam://nav/store"]');
    if (modernNavStore) {
        const rect = modernNavStore.getBoundingClientRect();
        if (rect.width > 0 && rect.height > 0) return modernNavStore as HTMLElement;
    }

    const webNavStore = document.querySelector('a[href*="store.steampowered.com"]');
    if (webNavStore) {
        const rect = webNavStore.getBoundingClientRect();
        if (rect.width > 0 && rect.height > 0) return webNavStore as HTMLElement;
    }

    const allElements = document.querySelectorAll('a, div[class*="menuitem"], span[class*="menuitem"]');
    for (const el of Array.from(allElements)) {
        if (!el.textContent) continue;
        
        const text = el.textContent.trim().toUpperCase();
        if (text === 'LOJA' || text === 'STORE') {
            const rect = el.getBoundingClientRect();
            if (rect.width > 0 && rect.height > 0) {
                const parentLink = el.closest('a');
                return (parentLink || el) as HTMLElement;
            }
        }
    }

    return null;
};

const addDot = () => {
    const tab = findStoreTab();
    
    if (tab && !tab.querySelector('.calyrecall-dot')) {
        const dot = document.createElement('div');
        dot.className = 'calyrecall-dot';
        
        const computedStyle = window.getComputedStyle(tab);
        if (computedStyle.position === 'static') {
            tab.style.position = 'relative';
        }
        
        if (computedStyle.overflow === 'hidden') {
            tab.style.overflow = 'visible';
        }
        
        tab.appendChild(dot);
    }
};

let lastInjectionTime = 0;
const INJECTION_THROTTLE = 500;

const robustInject = () => {
    const now = Date.now();
    if (now - lastInjectionTime < INJECTION_THROTTLE) return;
    lastInjectionTime = now;
    
    addDot();
};

export default async function main() {
    console.log('[CalyRecall] Frontend initializing...');
    
    Millennium.callServerMethod("calyrecall.frontend_log", "Frontend initialized and running!");

    injectStyle();
    robustInject();
    setInterval(robustInject, 2000);
    
    const originalPushState = history.pushState;
    history.pushState = function (...args) {
        originalPushState.apply(this, args);
        setTimeout(robustInject, 150);
    };
    
    const originalReplaceState = history.replaceState;
    history.replaceState = function (...args) {
        originalReplaceState.apply(this, args);
        setTimeout(robustInject, 150);
    };
    
    window.addEventListener('popstate', () => setTimeout(robustInject, 150));
    
    let mutationTimeout: number | undefined;
    const observer = new MutationObserver(() => {
        window.clearTimeout(mutationTimeout);
        mutationTimeout = window.setTimeout(() => {
            robustInject();
        }, 300);
    });
    
    observer.observe(document.body, { childList: true, subtree: true });
}