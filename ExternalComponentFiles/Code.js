var self = this;
self.fp = null;

var entityLoadedSubscription = options.mediator.subscribe("entityLoaded", function (entity) {
    self.fp = new FocalPointsExtension();
    self.fp.initialize(entity);
});

var entityUnloadedSubscription = options.mediator.subscribe("entityUnloaded", function (entity) {
    self.fp.dispose();
});

FocalPointsExtension = function () {
    this._entity = null;
    this._options = null;

    this._previewImage = null;

    this._focalCanvas = null;
    this._ctx = null;
    this._selection = {};
    this._drag = false;

    this._item = {};
    this._ratioX = null;
    this._ratioY = null;

    this._dataBoundDelegate = null;

    this._focalCanvasMouseLeaveDelegate = null;
    this._focalCanvasMouseDownDelegate = null;
    this._focalCanvasMouseUpDelegate = null;
    this._focalCanvasMouseMoveDelegate = null;
    this._previewImageLoadedDelegate = null;
}

FocalPointsExtension.prototype = {
    initialize: function (entity) {
        this._item = entity;

        this._previewImage = $('.previewImage')[0];
        this._previewImageLoadedDelegate = this._previewImageLoaded.bind(this);
        this._previewImage.addEventListener("load", this._previewImageLoadedDelegate);

        $(this._previewImage).attr('src', this._item.renditions().downloadPreview[0].href);

        $(this._previewImage).wrap("<div id='focalPointContainer' style='display:inline-block;position:relative;'></div>");
        $('<canvas id="focalCanvas" style="width:100%;height:100%;position:absolute;top:0px;left:0px;z-index:20;-webkit-touch-callout: none;-webkit-user-select: none;-khtml-user-select: none;-moz-user-select: none;-ms-user-select: none;user-select: none;"></canvas>').appendTo('#focalPointContainer');

        this._focalCanvas = $('#focalCanvas')[0];

        this._ctx = this._focalCanvas.getContext('2d');

        this._focalCanvasMouseDownDelegate = this._focalCanvasMouseDown.bind(this);
        this._focalCanvas.addEventListener("mousedown", this._focalCanvasMouseDownDelegate);

        this._focalCanvasMouseUpDelegate = this._focalCanvasMouseUp.bind(this);
        this._focalCanvas.addEventListener("mouseup", this._focalCanvasMouseUpDelegate);

        this._focalCanvasMouseMoveDelegate = this._focalCanvasMouseMove.bind(this);
        this._focalCanvas.addEventListener("mousemove", this._focalCanvasMouseMoveDelegate);

        this._focalCanvasMouseLeaveDelegate = this._focalCanvasMouseLeave.bind(this);
        this._focalCanvas.addEventListener("mouseleave", this._focalCanvasMouseLeaveDelegate);

        $("<span class='sfExample'></span>").appendTo('.sfPreviewVideoFrame');
    },

    dispose: function () {
        if (this._dataBoundDelegate) {
            delete this._dataBoundDelegate;
        }
        if (this._focalCanvasMouseLeaveDelegate) {
            delete this._focalCanvasMouseLeaveDelegate;
        }
        if (this._focalCanvasMouseDownDelegate) {
            delete this._focalCanvasMouseDownDelegate;
        }
        if (this._focalCanvasMouseUpDelegate) {
            delete this._focalCanvasMouseUpDelegate;
        }
        if (this._focalCanvasMouseMoveDelegate) {
            delete this._focalCanvasMouseMoveDelegate;
        }
        if (this._previewImageLoadedDelegate) {
            delete this._previewImageLoadedDelegate;
        }

        if (this._previewImage) {
            this._previewImage.removeEventListener("load", this._previewImageLoadedDelegate);
        }
        if (this._focalCanvas) {
            this._focalCanvas.removeEventListener("mousedown", this._focalCanvasMouseDownDelegate);
            this._focalCanvas.removeEventListener("mouseup", this._focalCanvasMouseUpDelegate);
            this._focalCanvas.removeEventListener("mousemove", this._focalCanvasMouseMoveDelegate);
            this._focalCanvas.removeEventListener("mouseleave", this._focalCanvasMouseLeaveDelegate);
        }
    },

    _previewImageLoaded: function (sender, args) {
        var itemWidth = this._item.properties["MainFile"]().properties.width;
        var previewWidth = this._previewImage.width;

        this._ratio = itemWidth / previewWidth;
        this._clear();

        this._focalCanvas.width = this._previewImage.width;
        this._focalCanvas.height = this._previewImage.height;

        if (this._item.properties['FocalPointX']() !== null && this._item.properties['FocalPointX']() != 0 && this._item.properties['FocalPointY']() !== null && this._item.properties['FocalPointY']() != 0) {
            this._selection.startX = this._item.properties['FocalPointX']() / this._ratio;
            this._selection.startY = this._item.properties['FocalPointY']() / this._ratio;

            this._selection.w = this._item.properties['FocalPointWidth']() / this._ratio;
            this._selection.h = this._item.properties['FocalPointHeight']() / this._ratio;

            this._draw();
        }
    },

    _focalCanvasMouseDown: function (sender, args) {
        var x = sender.offsetX,
            y = sender.offsetY;
        if (this._item.properties['FocalPointX']() > 0 && this._item.properties['FocalPointY']() > 0) {
            if (this._isPointInFocalPointMarker(x, y)) {
                this._cycleFocalPointAnchor();
                this._draw();
                return;
            } else {
                this._clearFocalPoint();
                this._clear();
            }
        }

        this._setFocalPointAnchor(0);
        this._beginSelection(x, y);

        event.preventDefault();
    },

    _focalCanvasMouseLeave: function (sender, args) {
        this._endSelection();
    },

    _focalCanvasMouseUp: function (sender, args) {
        this._endSelection();
        this._persistFocalPoint();
    },

    _focalCanvasMouseMove: function (sender, args) {
        if (this._drag) {

            var w = sender.offsetX - this._selection.startX,
                h = sender.offsetY - this._selection.startY;

            this._selection.w = w;
            this._selection.h = h;

            this._draw();
        }
    },

    _cycleFocalPointAnchor: function () {
        p = this._item.properties['FocalPointAnchor']() + 1;
        if (p > 4) {
            p = 0;
        }
        this._setFocalPointAnchor(p);
    },

    _setFocalPoint: function (x, y, w, h) {
        this._item.properties.FocalPointX(x);
        this._item.properties.FocalPointY(y);
        this._item.properties.FocalPointWidth(w);
        this._item.properties.FocalPointHeight(h);
    },

    _setFocalPointAnchor: function (p) {
        this._item.properties.FocalPointAnchor(p);
    },

    _clearFocalPoint: function () {
        this._setFocalPoint(0, 0, 0, 0);
        this._setFocalPointAnchor(0);
        this._selection = {};
    },

    _beginSelection: function (x, y) {
        this._selection.startX = x;
        this._selection.startY = y;
        this._drag = true;
    },

    _persistFocalPoint: function () {
        this._item.save();
    },

    _endSelection: function () {
        this._drag = false;

        if (Math.abs(this._selection.w) < 25 || Math.abs(this._selection.h) < 25) {
            this._clearFocalPoint();
            this._clear();
            return;
        }

        var x = Math.ceil(this._selection.startX * this._ratio),
            y = Math.ceil(this._selection.startY * this._ratio),
            w = Math.ceil(this._selection.w * this._ratio),
            h = Math.ceil(this._selection.h * this._ratio);

        this._setFocalPoint(x, y, w, h);
        this._draw();
    },

    _isPointInFocalPointMarker: function (x, y) {
        var isCollision = false;

        var left = this._selection.startX,
            right = this._selection.startX + this._selection.w;
        var top = this._selection.startY,
            bottom = this._selection.startY + this._selection.h;
        if (right >= x
            && left <= x
            && bottom >= y
            && top <= y) {
            isCollision = true;
        }

        return isCollision;
    },

    _draw: function () {
        this._clear();
        this._drawFocalPoint();
        this._drawFocalPointAnchor();
    },

    _clear: function () {
        this._ctx.clearRect(0, 0, this._focalCanvas.width, this._focalCanvas.height);
    },

    _drawFocalPoint: function () {
        this._ctx.beginPath();
        this._ctx.globalAlpha = 0.5;
        this._ctx.rect(this._selection.startX, this._selection.startY, this._selection.w, this._selection.h);
        this._ctx.strokeStyle = 'rgba(0,0,0,0.9)';
        this._ctx.fillStyle = 'rgba(255,255,255,0.6)';
        this._ctx.lineWidth = 2;
        this._ctx.fill();
        this._ctx.stroke();
    },

    _drawFocalPointAnchor: function () {
        for (i = 0; i <= 4; i++) {
            var x, y;

            switch (i) {
                case 0:
                    x = this._selection.startX + (this._selection.w / 2);
                    y = this._selection.startY + (this._selection.h / 2);
                    break;
                case 1:
                    x = this._selection.startX + (this._selection.w / 2);
                    y = this._selection.startY + 10;
                    break;
                case 2:
                    x = this._selection.startX + (this._selection.w - 10);
                    y = this._selection.startY + (this._selection.h / 2);
                    break;
                case 3:
                    x = this._selection.startX + (this._selection.w / 2);
                    y = this._selection.startY + (this._selection.h - 10);
                    break;
                case 4:
                    x = this._selection.startX + 10;
                    y = this._selection.startY + (this._selection.h / 2);
                    break;
            }

            this._ctx.beginPath();
            this._ctx.arc(x, y, 3, 0, 2 * Math.PI, false);

            if (i == this._item.properties['FocalPointAnchor']()) {
                this._ctx.fillStyle = 'white';
            } else {
                this._ctx.fillStyle = 'black';
            }

            this._ctx.fill();
            this._ctx.lineWidth = 1;
            this._ctx.strokeStyle = '#000000';
            this._ctx.stroke();
        }
    }
}