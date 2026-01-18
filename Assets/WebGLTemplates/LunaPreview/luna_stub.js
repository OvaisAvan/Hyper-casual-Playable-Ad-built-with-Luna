/**
 * luna_stub.js
 * ============
 * A lightweight stub of the Luna JS runtime for local browser preview.
 * Include this ONLY in development/preview builds, NOT in the final Luna submission.
 *
 * Usage: Add <script src="luna_stub.js"></script> BEFORE the Unity loader
 *        in your preview index.html when testing outside the Luna environment.
 *
 * This stub mirrors the Luna JS API surface so your Unity code runs correctly
 * without the actual Luna runtime being present.
 *
 * Luna production runtime is injected automatically by the Luna platform
 * when your ad runs inside their ad network.
 */

(function() {
  'use strict';

  // Guard — don't override a real Luna runtime if it somehow exists
  if (typeof Luna !== 'undefined') {
    console.log('[luna_stub] Real Luna runtime detected — stub not applied.');
    return;
  }

  console.log('[luna_stub] Luna runtime not found — applying development stub.');

  // ── Luna namespace ──────────────────────────────────────────────────────────
  window.Luna = {
    Unity: {
      LifeCycle: {
        GameStart: function() {
          console.log('[Luna.stub] LifeCycle.GameStart()');
          window._lunaStubState.gameStarted = true;
          window._lunaStubState.gameStartTime = Date.now();
        },
        GameEnd: function(data) {
          console.log('[Luna.stub] LifeCycle.GameEnd()', data);
          window._lunaStubState.gameEnded = true;
          window._lunaStubState.gameEndData = data;
        }
      },

      Playable: {
        ShowEndCard: function() {
          console.log('[Luna.stub] Playable.ShowEndCard()');
          window._lunaStubState.endCardShown = true;

          // Show a minimal end card overlay in the preview
          var overlay = document.getElementById('luna-stub-endcard');
          if (overlay) {
            overlay.style.display = 'flex';
          } else {
            var div = document.createElement('div');
            div.id = 'luna-stub-endcard';
            div.style.cssText = [
              'position:fixed', 'inset:0', 'background:rgba(15,15,30,0.92)',
              'display:flex', 'flex-direction:column', 'align-items:center',
              'justify-content:center', 'z-index:9999',
              'font-family:sans-serif', 'color:#fff', 'gap:16px'
            ].join(';');
            div.innerHTML = `
              <div style="font-size:1.5rem;font-weight:700">Luna End Card (Stub)</div>
              <div style="font-size:0.9rem;color:rgba(255,255,255,0.6)">
                In production, Luna shows the real end card here.
              </div>
              <button onclick="window.open('${window._lunaStoreUrl || '#'}','_blank')"
                style="padding:14px 32px;border-radius:30px;border:none;
                       background:linear-gradient(135deg,#a855f7,#3b82f6);
                       color:#fff;font-size:1rem;font-weight:700;cursor:pointer">
                INSTALL FREE
              </button>
              <button onclick="document.getElementById('luna-stub-endcard').style.display='none'"
                style="background:transparent;border:1px solid rgba(255,255,255,0.3);
                       color:#fff;padding:8px 20px;border-radius:20px;cursor:pointer;font-size:0.8rem">
                Close Preview
              </button>
            `;
            document.body.appendChild(div);
          }
        }
      },

      Analytics: {
        TrackEvent: function(name, data) {
          console.log(`[Luna.stub] Analytics.TrackEvent("${name}")`, data || {});
          window._lunaStubState.events = window._lunaStubState.events || [];
          window._lunaStubState.events.push({ name, data, time: Date.now() });
        }
      }
    }
  };

  // ── Internal state ──────────────────────────────────────────────────────────
  window._lunaStubState = {
    gameStarted:   false,
    gameEnded:     false,
    endCardShown:  false,
    gameStartTime: null,
    gameEndData:   null,
    events:        []
  };

  // ── Store URL injection simulation ──────────────────────────────────────────
  // In production, Luna calls: unityInstance.SendMessage('LunaCtaHandler','SetStoreUrl','...')
  // In the stub, set window._lunaStoreUrl before loading the Unity build.
  window._lunaStoreUrl = window._lunaStoreUrl ||
    'https://play.google.com/store/apps/details?id=com.yourcompany.tapblitz';

  // ── Debug panel (shown in preview only) ────────────────────────────────────
  var panel = document.createElement('div');
  panel.style.cssText = [
    'position:fixed', 'bottom:8px', 'left:8px',
    'background:rgba(0,0,0,0.7)', 'color:#a78bfa',
    'font-family:monospace', 'font-size:11px',
    'padding:6px 10px', 'border-radius:8px',
    'pointer-events:none', 'z-index:8888'
  ].join(';');
  panel.textContent = '🌙 Luna Stub Active';
  document.body.appendChild(panel);

  console.log('[luna_stub] Stub installed. State available at window._lunaStubState');
})();
