var WebBrowserLib = {

  $WebBrowserState: {
    initialized: false,
    iframes: {},
    container: null,
    canvas: null,
    pageArrivedQueue: [],
    stopAmbientQueue: 0,

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
      var keepAliveInterval = setInterval(function () {
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

      // postMessage listener — push to queue, C# polls via CheckPageArrived
      window.addEventListener('message', function (e) {
        if (e.data && e.data.type === 'PAGE_ARRIVED') {
          var panelId = e.data.panelId || 0;
          console.log('[WebBrowser] PAGE_ARRIVED panelId=' + panelId + ' (queued)');
          WebBrowserState.pageArrivedQueue.push(panelId);
        }
        if (e.data && e.data.type === 'STOP_AMBIENT') {
          WebBrowserState.stopAmbientQueue++;
        }
      });
    }
  },

  InitBrowser__deps: ['$WebBrowserState'],
  InitBrowser: function () {
    WebBrowserState.init();
  },

  CheckPageArrived__deps: ['$WebBrowserState'],
  CheckPageArrived: function () {
    if (WebBrowserState.pageArrivedQueue.length > 0) {
      return WebBrowserState.pageArrivedQueue.shift();
    }
    return -1;
  },

  NotifyStageStart__deps: ['$WebBrowserState'],
  NotifyStageStart: function (panelId) {
    var iframe = WebBrowserState.iframes[panelId];
    if (!iframe) return;
    var send = function () {
      try { iframe.contentWindow.postMessage({type:'STAGE_START'}, '*'); } catch(e) {}
    };
    send();
    setTimeout(send, 300);
  },

  CheckStopAmbient__deps: ['$WebBrowserState'],
  CheckStopAmbient: function () {
    if (WebBrowserState.stopAmbientQueue > 0) {
      WebBrowserState.stopAmbientQueue--;
      return 1;
    }
    return 0;
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
