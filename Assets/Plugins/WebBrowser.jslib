mergeInto(LibraryManager.library, {

  InitBrowser: function () {
    if (window._webBrowserInitialized) return;
    window._webBrowserInitialized = true;

    // CSS3D container (behind Unity canvas)
    var container = document.getElementById('css3d-container');
    if (!container) {
      container = document.createElement('div');
      container.id = 'css3d-container';
      container.style.cssText = 'position:absolute;top:0;left:0;width:100%;height:100%;overflow:hidden;';
      var canvas = document.querySelector('#unity-canvas') || document.querySelector('canvas');
      canvas.parentElement.insertBefore(container, canvas);
    }
    var canvas = document.querySelector('#unity-canvas') || document.querySelector('canvas');
    canvas.style.position = 'absolute';
    canvas.style.top = '0';
    canvas.style.left = '0';
    canvas.style.pointerEvents = 'none';
    canvas.style.background = 'transparent';

    // Three.js scene + CSS3DRenderer
    window._css3dScene = new THREE.Scene();
    window._css3dCamera = new THREE.PerspectiveCamera(60,
      canvas.clientWidth / canvas.clientHeight, 0.1, 10000);
    window._css3dRenderer = new CSS3DRenderer();
    window._css3dRenderer.setSize(canvas.clientWidth, canvas.clientHeight);
    container.appendChild(window._css3dRenderer.domElement);

    window._iframes = {};

    // Resize handler
    window.addEventListener('resize', function () {
      var w = canvas.clientWidth;
      var h = canvas.clientHeight;
      window._css3dCamera.aspect = w / h;
      window._css3dCamera.updateProjectionMatrix();
      window._css3dRenderer.setSize(w, h);
    });

    // Animation loop
    function animate() {
      requestAnimationFrame(animate);
      window._css3dRenderer.render(window._css3dScene, window._css3dCamera);
    }
    animate();

    // postMessage listener for page arrival
    window.addEventListener('message', function (e) {
      if (e.data && e.data.type === 'PAGE_ARRIVED') {
        var panelId = e.data.panelId || 0;
        if (window.unityInstance) {
          window.unityInstance.SendMessage('LevelManager', 'OnPageArrived', panelId.toString());
        }
      }
    });
  },

  CreateIframe: function (url, posX, posY, posZ, rotX, rotY, rotZ, rotW, width, height, panelId) {
    var urlStr = UTF8ToString(url);
    var id = panelId;

    var iframe = document.createElement('iframe');
    iframe.src = urlStr;
    iframe.style.width = width + 'px';
    iframe.style.height = height + 'px';
    iframe.style.border = 'none';
    iframe.style.background = 'white';
    iframe.setAttribute('allow', 'autoplay; fullscreen');

    var obj = new CSS3DObject(iframe);
    // Unity→Three.js: negate Z for position, convert quaternion
    obj.position.set(posX, posY, -posZ);
    var q = new THREE.Quaternion(-rotX, -rotY, rotZ, rotW);
    obj.quaternion.copy(q);

    // Scale: CSS3D pixels to Unity units (1 unit = ~100px)
    var scaleFactor = 0.01;
    obj.scale.set(scaleFactor, scaleFactor, scaleFactor);

    window._css3dScene.add(obj);
    window._iframes[id] = { iframe: iframe, object: obj };
  },

  SyncIframeTransform: function (panelId, posX, posY, posZ, rotX, rotY, rotZ, rotW) {
    var entry = window._iframes[panelId];
    if (!entry) return;
    // Unity→Three.js coordinate conversion
    entry.object.position.set(posX, posY, -posZ);
    var q = new THREE.Quaternion(-rotX, -rotY, rotZ, rotW);
    entry.object.quaternion.copy(q);
  },

  UpdateIframeURL: function (panelId, url) {
    var urlStr = UTF8ToString(url);
    var entry = window._iframes[panelId];
    if (entry) {
      entry.iframe.src = urlStr;
    }
  },

  SyncCameraTransform: function (px, py, pz, rx, ry, rz, rw) {
    if (!window._css3dCamera) return;
    // Unity→Three.js: negate Z for position, convert quaternion
    window._css3dCamera.position.set(px, py, -pz);
    var q = new THREE.Quaternion(-rx, -ry, rz, rw);
    window._css3dCamera.quaternion.copy(q);
  },

  DestroyIframe: function (panelId) {
    var entry = window._iframes[panelId];
    if (entry) {
      window._css3dScene.remove(entry.object);
      delete window._iframes[panelId];
    }
  }
});
