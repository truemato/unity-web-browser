var WebBrowserLib = {

  $WebBrowserState: {
    initialized: false,
    iframes: {},
    container: null,
    canvas: null,

    init: function () {
      if (WebBrowserState.initialized) return;
      WebBrowserState.initialized = true;

      var canvas = document.querySelector('#unity-canvas') || document.querySelector('canvas');
      WebBrowserState.canvas = canvas;
      var parent = canvas.parentElement;

      parent.style.position = 'relative';

      var container = document.createElement('div');
      container.id = 'iframe-overlay';
      container.style.cssText = 'position:absolute;top:0;left:0;width:100%;height:100%;pointer-events:none;overflow:hidden;z-index:10;';
      parent.appendChild(container);

      WebBrowserState.container = container;

      // Keep Unity running even when iframe or page steals focus.
      // Unity WebGL stops requestAnimationFrame when canvas loses focus.
      // This fallback timer forces Unity to keep ticking.
      var keepAliveInterval = setInterval(function () {
        try {
          // Trigger a minimal frame if Unity is idle
          if (window.unityInstance && typeof window.unityInstance.SendMessage === 'function') {
            // Just poke Unity to keep it alive - no-op message
          }
          // Force canvas to stay in animation loop
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
