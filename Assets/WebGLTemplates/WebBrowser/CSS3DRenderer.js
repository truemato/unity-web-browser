/**
 * CSS3DRenderer for Three.js (non-module version for Unity WebGL)
 * Based on three.js CSS3DRenderer addon
 */

(function () {

  const _position = new THREE.Vector3();
  const _quaternion = new THREE.Quaternion();
  const _scale = new THREE.Vector3();

  class CSS3DObject extends THREE.Object3D {
    constructor(element) {
      super();
      this.isCSS3DObject = true;
      this.element = element || document.createElement('div');
      this.element.style.position = 'absolute';
      this.element.style.pointerEvents = 'auto';
      this.element.style.userSelect = 'none';
      this.element.setAttribute('draggable', false);

      this.addEventListener('removed', function () {
        this.traverse(function (object) {
          if (object.element instanceof Element && object.element.parentNode !== null) {
            object.element.parentNode.removeChild(object.element);
          }
        });
      });
    }

    copy(source, recursive) {
      super.copy(source, recursive);
      this.element = source.element.cloneNode(true);
      return this;
    }
  }

  const _matrix = new THREE.Matrix4();
  const _matrix2 = new THREE.Matrix4();

  class CSS3DRenderer {
    constructor(parameters) {
      parameters = parameters || {};
      const _this = this;
      let _width, _height;
      let _widthHalf, _heightHalf;

      const cache = {
        camera: { fov: 0, style: '' },
        objects: new WeakMap()
      };

      const domElement = parameters.element || document.createElement('div');
      domElement.style.overflow = 'hidden';
      this.domElement = domElement;

      const viewElement = document.createElement('div');
      viewElement.style.transformOrigin = '0 0';
      viewElement.style.pointerEvents = 'none';
      domElement.appendChild(viewElement);

      const cameraElement = document.createElement('div');
      cameraElement.style.transformStyle = 'preserve-3d';
      viewElement.appendChild(cameraElement);

      this.getSize = function () {
        return { width: _width, height: _height };
      };

      this.render = function (scene, camera) {
        const fov = camera.projectionMatrix.elements[5] * _heightHalf;

        if (cache.camera.fov !== fov) {
          viewElement.style.perspective = camera.isPerspectiveCamera ? fov + 'px' : '';
          cache.camera.fov = fov;
        }

        if (camera.view && camera.view.enabled) {
          viewElement.style.transform =
            'translate(' + (-camera.view.offsetX * (_width / camera.view.width)) + 'px,' +
            (-camera.view.offsetY * (_height / camera.view.height)) + 'px)' +
            'scale(' + (camera.view.fullWidth / camera.view.width) + ',' +
            (camera.view.fullHeight / camera.view.height) + ')';
        } else {
          viewElement.style.transform = '';
        }

        if (scene.matrixWorldAutoUpdate === true) scene.updateMatrixWorld();
        if (camera.parent === null && camera.matrixWorldAutoUpdate === true) camera.updateMatrixWorld();

        const scaleByViewOffset = camera.view && camera.view.enabled
          ? camera.view.height / camera.view.fullHeight : 1;
        const cameraCSSMatrix = camera.isOrthographicCamera
          ? 'scale(' + scaleByViewOffset + ')scale(' + fov + ')' + getCameraCSSMatrix(camera.matrixWorldInverse)
          : 'scale(' + scaleByViewOffset + ')translateZ(' + fov + 'px)' + getCameraCSSMatrix(camera.matrixWorldInverse);

        const style = cameraCSSMatrix + 'translate(' + _widthHalf + 'px,' + _heightHalf + 'px)';

        if (cache.camera.style !== style) {
          cameraElement.style.transform = style;
          cache.camera.style = style;
        }

        renderObject(scene, scene, camera, cameraCSSMatrix);
      };

      this.setSize = function (width, height) {
        _width = width;
        _height = height;
        _widthHalf = _width / 2;
        _heightHalf = _height / 2;

        domElement.style.width = width + 'px';
        domElement.style.height = height + 'px';
        viewElement.style.width = width + 'px';
        viewElement.style.height = height + 'px';
        cameraElement.style.width = width + 'px';
        cameraElement.style.height = height + 'px';
      };

      function epsilon(value) {
        return Math.abs(value) < 1e-10 ? 0 : value;
      }

      function getCameraCSSMatrix(matrix) {
        var e = matrix.elements;
        return 'matrix3d(' +
          epsilon(e[0]) + ',' + epsilon(-e[1]) + ',' + epsilon(e[2]) + ',' + epsilon(e[3]) + ',' +
          epsilon(e[4]) + ',' + epsilon(-e[5]) + ',' + epsilon(e[6]) + ',' + epsilon(e[7]) + ',' +
          epsilon(e[8]) + ',' + epsilon(-e[9]) + ',' + epsilon(e[10]) + ',' + epsilon(e[11]) + ',' +
          epsilon(e[12]) + ',' + epsilon(-e[13]) + ',' + epsilon(e[14]) + ',' + epsilon(e[15]) +
          ')';
      }

      function getObjectCSSMatrix(matrix) {
        var e = matrix.elements;
        return 'translate(-50%,-50%)matrix3d(' +
          epsilon(e[0]) + ',' + epsilon(e[1]) + ',' + epsilon(e[2]) + ',' + epsilon(e[3]) + ',' +
          epsilon(-e[4]) + ',' + epsilon(-e[5]) + ',' + epsilon(-e[6]) + ',' + epsilon(-e[7]) + ',' +
          epsilon(e[8]) + ',' + epsilon(e[9]) + ',' + epsilon(e[10]) + ',' + epsilon(e[11]) + ',' +
          epsilon(e[12]) + ',' + epsilon(e[13]) + ',' + epsilon(e[14]) + ',' + epsilon(e[15]) +
          ')';
      }

      function renderObject(object, scene, camera, cameraCSSMatrix) {
        if (object.isCSS3DObject) {
          var visible = (object.visible === true) && (object.layers.test(camera.layers) === true);
          object.element.style.display = visible ? '' : 'none';

          if (visible) {
            object.onBeforeRender(_this, scene, camera);
            var style = getObjectCSSMatrix(object.matrixWorld);
            var element = object.element;
            var cachedObject = cache.objects.get(object);

            if (cachedObject === undefined || cachedObject.style !== style) {
              element.style.transform = style;
              cache.objects.set(object, { style: style });
            }

            if (element.parentNode !== cameraElement) {
              cameraElement.appendChild(element);
            }

            object.onAfterRender(_this, scene, camera);
          }
        }

        for (var i = 0; i < object.children.length; i++) {
          renderObject(object.children[i], scene, camera, cameraCSSMatrix);
        }
      }
    }
  }

  // Expose globally
  window.CSS3DObject = CSS3DObject;
  window.CSS3DRenderer = CSS3DRenderer;

})();
