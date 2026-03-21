var WebBrowserLib = {

  $WebBrowserState: {
    initialized: false,
    iframes: {},
    container: null,

    init: function () {
      if (WebBrowserState.initialized) return;
      WebBrowserState.initialized = true;

      var canvas = document.querySelector('#unity-canvas') || document.querySelector('canvas');
      var parent = canvas.parentElement;

      // Ensure parent has relative positioning
      parent.style.position = 'relative';

      // Container for iframes, overlaid on top of canvas
      var container = document.createElement('div');
      container.id = 'iframe-overlay';
      container.style.cssText = 'position:absolute;top:0;left:0;width:100%;height:100%;pointer-events:none;overflow:hidden;z-index:10;';
      parent.appendChild(container);

      WebBrowserState.container = container;

      // postMessage listener for page arrival
      window.addEventListener('message', function (e) {
        if (e.data && e.data.type === 'PAGE_ARRIVED') {
          var panelId = e.data.panelId || 0;
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

    var iframe = document.createElement('iframe');
    iframe.src = urlStr;
    iframe.style.cssText = 'position:absolute;border:none;background:white;pointer-events:auto;display:none;';
    iframe.setAttribute('allow', 'autoplay; fullscreen');
    iframe.id = 'web-panel-' + panelId;

    WebBrowserState.container.appendChild(iframe);
    WebBrowserState.iframes[panelId] = iframe;
  },

  UpdateIframeRect__deps: ['$WebBrowserState'],
  UpdateIframeRect: function (panelId, left, top, width, height, visible) {
    var iframe = WebBrowserState.iframes[panelId];
    if (!iframe) return;
    if (visible) {
      // Values are normalized 0-1, convert to percentage
      iframe.style.display = 'block';
      iframe.style.left = (left * 100) + '%';
      iframe.style.top = (top * 100) + '%';
      iframe.style.width = (width * 100) + '%';
      iframe.style.height = (height * 100) + '%';
    } else {
      iframe.style.display = 'none';
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
