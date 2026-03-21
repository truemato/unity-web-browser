var WebBrowserLib = {

  $WebBrowserState: {
    initialized: false,
    started: false,
    iframes: {},
    container: null,
    canvas: null,
    startOverlay: null,

    init: function () {
      if (WebBrowserState.initialized) return;
      WebBrowserState.initialized = true;

      var canvas = document.querySelector('#unity-canvas') || document.querySelector('canvas');
      WebBrowserState.canvas = canvas;
      var parent = canvas.parentElement;

      parent.style.position = 'relative';

      // --- Click to Start overlay ---
      var overlay = document.createElement('div');
      overlay.id = 'start-overlay';
      overlay.style.cssText = 'position:absolute;top:0;left:0;width:100%;height:100%;'
        + 'background:rgba(0,0,0,0.7);display:flex;align-items:center;justify-content:center;'
        + 'z-index:999;cursor:pointer;';
      overlay.innerHTML = '<div style="text-align:center;color:#fff;font-family:sans-serif;">'
        + '<div style="font-size:2.5em;font-weight:bold;margin-bottom:0.3em;">Click to Start</div>'
        + '<div style="font-size:1.2em;opacity:0.7;">クリックして開始</div></div>';
      parent.appendChild(overlay);
      WebBrowserState.startOverlay = overlay;

      overlay.addEventListener('click', function () {
        // Resume AudioContext (browser requires user gesture)
        try {
          var audioCtx = window.AudioContext || window.webkitAudioContext;
          if (audioCtx) {
            var ctx = new audioCtx();
            ctx.resume();
          }
          // Also resume Unity's AudioContext if it exists
          if (typeof unityInstance !== 'undefined' && unityInstance.Module) {
            var uctx = unityInstance.Module.SDL2 && unityInstance.Module.SDL2.audioContext;
            if (uctx) uctx.resume();
          }
        } catch (e) {}

        // Hide overlay
        overlay.style.display = 'none';
        WebBrowserState.started = true;

        // Notify Unity that game has started
        if (window.unityInstance) {
          window.unityInstance.SendMessage('LevelManager', 'OnGameStarted', '');
        }

        // Focus canvas
        if (canvas) canvas.focus();
      });

      // --- iframe container ---
      var container = document.createElement('div');
      container.id = 'iframe-overlay';
      container.style.cssText = 'position:absolute;top:0;left:0;width:100%;height:100%;pointer-events:none;overflow:hidden;z-index:10;';
      parent.appendChild(container);

      WebBrowserState.container = container;

      // Keep Unity running when focus is lost
      setInterval(function () {
        try {
          if (canvas && canvas.dispatchEvent) {
            canvas.dispatchEvent(new Event('focus', { bubbles: false }));
          }
        } catch (e) {}
      }, 500);

      // Re-focus canvas when clicking outside iframe
      document.addEventListener('click', function (e) {
        if (e.target.tagName !== 'IFRAME' && canvas) {
          canvas.focus();
        }
      });

      // postMessage listener for page arrival
      window.addEventListener('message', function (e) {
        if (e.data && e.data.type === 'PAGE_ARRIVED') {
          var panelId = e.data.panelId || 0;
          console.log('[WebBrowser] PAGE_ARRIVED panelId=' + panelId);
          if (window.unityInstance) {
            window.unityInstance.SendMessage('LevelManager', 'OnPageArrived', panelId.toString());
          }
        }
      });
    }
  },

  InitBrowser__deps: ['$WebBrowserState'],
  InitBrowser: function () {
    WebBrowserState.init();
  },

  CreateIframe__deps: ['$WebBrowserState'],
  CreateIframe: function (url, panelId, pixelWidth, pixelHeight) {
    WebBrowserState.init();
    var urlStr = UTF8ToString(url);
    console.log('[WebBrowser] CreateIframe panelId=' + panelId + ' url=' + urlStr);

    var iframe = document.createElement('iframe');
    iframe.src = urlStr;
    iframe.style.cssText = 'position:absolute;border:none;background:white;pointer-events:auto;display:none;';
    iframe.setAttribute('allow', 'autoplay; fullscreen');
    iframe.setAttribute('tabindex', '-1');
    iframe.id = 'web-panel-' + panelId;

    WebBrowserState.container.appendChild(iframe);
    WebBrowserState.iframes[panelId] = iframe;
  },

  UpdateIframeRect__deps: ['$WebBrowserState'],
  UpdateIframeRect: function (panelId, left, top, width, height, visible) {
    try {
      var iframe = WebBrowserState.iframes[panelId];
      if (!iframe) return;
      if (visible) {
        iframe.style.display = 'block';
        iframe.style.left = (left * 100) + '%';
        iframe.style.top = (top * 100) + '%';
        iframe.style.width = (width * 100) + '%';
        iframe.style.height = (height * 100) + '%';
      } else {
        iframe.style.display = 'none';
      }
    } catch (e) {
      console.error('[WebBrowser] UpdateIframeRect error:', e);
    }
  },

  UpdateIframeURL__deps: ['$WebBrowserState'],
  UpdateIframeURL: function (panelId, url) {
    var urlStr = UTF8ToString(url);
    var iframe = WebBrowserState.iframes[panelId];
    if (iframe) {
      iframe.src = urlStr;
    }
  },

  DestroyIframe__deps: ['$WebBrowserState'],
  DestroyIframe: function (panelId) {
    var iframe = WebBrowserState.iframes[panelId];
    if (iframe) {
      iframe.parentNode.removeChild(iframe);
      delete WebBrowserState.iframes[panelId];
    }
  }
};

autoAddDeps(WebBrowserLib, '$WebBrowserState');
mergeInto(LibraryManager.library, WebBrowserLib);
