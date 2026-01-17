/**
 * LunaBridge.jslib
 * ================
 * Unity WebGL JS plugin — bridges Unity C# to the Luna playable ad runtime.
 *
 * Luna (Unity Luna, formerly ironSource Luna) injects its runtime into the
 * parent HTML page. This plugin detects and calls the Luna JS API methods.
 *
 * Place at: Assets/Plugins/WebGL/LunaBridge.jslib
 *
 * Luna JS API reference:
 *   https://developer.unity.com/products/luna
 *
 * Supported fallbacks (when Luna runtime not present):
 *   - MRAID 2.0
 *   - Generic postMessage to parent frame
 *   - window.gameReady / window.gameEnd (Mintegral compat)
 */

mergeInto(LibraryManager.library, {

  // ── Luna lifecycle ──────────────────────────────────────────────────────────

  _LunaCall: function(methodPtr) {
    var method = UTF8ToString(methodPtr);
    try {
      if (typeof Luna !== 'undefined' && Luna.Unity && Luna.Unity.LifeCycle) {
        switch(method) {
          case 'gameStart':   Luna.Unity.LifeCycle.GameStart();  break;
          case 'adReady':     /* Luna handles this automatically */   break;
          case 'showEndCard': Luna.Unity.Playable.ShowEndCard(); break;
        }
      } else {
        // Fallbacks
        if (method === 'gameStart' && typeof gameReady === 'function') gameReady();
        if (method === 'showEndCard' && typeof mraid !== 'undefined')  mraid.close();
        window.parent.postMessage({ source: 'TapBlitz', event: method }, '*');
      }
    } catch(e) {
      console.warn('[LunaBridge] _LunaCall error:', method, e);
    }
  },

  _LunaCallData: function(methodPtr, dataPtr) {
    var method = UTF8ToString(methodPtr);
    var data   = UTF8ToString(dataPtr);
    try {
      var payload = JSON.parse(data);
      if (typeof Luna !== 'undefined' && Luna.Unity && Luna.Unity.LifeCycle) {
        if (method === 'gameEnd') Luna.Unity.LifeCycle.GameEnd(payload);
      } else {
        if (method === 'gameEnd' && typeof gameEnd === 'function') gameEnd(payload);
        window.parent.postMessage({ source: 'TapBlitz', event: method, data: payload }, '*');
      }
    } catch(e) {
      console.warn('[LunaBridge] _LunaCallData error:', method, e);
    }
  },

  // ── Luna analytics ──────────────────────────────────────────────────────────

  _LunaTrack: function(eventNamePtr, jsonPtr) {
    var eventName = UTF8ToString(eventNamePtr);
    var json      = UTF8ToString(jsonPtr);
    try {
      var payload = JSON.parse(json);
      if (typeof Luna !== 'undefined' && Luna.Unity && Luna.Unity.Analytics) {
        Luna.Unity.Analytics.TrackEvent(eventName, payload);
      } else {
        window.parent.postMessage({
          source: 'TapBlitz',
          event:  'analytics',
          name:   eventName,
          data:   payload
        }, '*');
      }
    } catch(e) {
      console.warn('[LunaBridge] _LunaTrack error:', eventName, e);
    }
  },

  // ── Store redirect ──────────────────────────────────────────────────────────

  _LunaOpenStore: function(urlPtr) {
    var url = UTF8ToString(urlPtr);
    try {
      if (typeof Luna !== 'undefined' && Luna.Unity && Luna.Unity.Playable) {
        Luna.Unity.Playable.ShowEndCard();   // Luna handles the redirect
        return;
      }
      if (typeof mraid !== 'undefined') { mraid.open(url); return; }
      // Generic fallback
      window.parent.open(url, '_blank');
    } catch(e) {
      window.open(url, '_blank');
    }
  },

  // ── End card ────────────────────────────────────────────────────────────────

  _LunaShowEndCard: function() {
    try {
      if (typeof Luna !== 'undefined' && Luna.Unity && Luna.Unity.Playable)
        Luna.Unity.Playable.ShowEndCard();
      else
        window.parent.postMessage({ source: 'TapBlitz', event: 'showEndCard' }, '*');
    } catch(e) {
      console.warn('[LunaBridge] _LunaShowEndCard error:', e);
    }
  }

});
