"use strict";
const rangeValue = 10;
const largelabels = [
    { pos: -9 / 10, text: "FULL" },
    { pos: -7 / 10, text: "HALF" },
    { pos: -5 / 10, text: "SLOW" },
    { pos: -2.5 / 10, text: "SLOW" },
    { pos: -1.5 / 10, text: "DEAD" },
    { pos: 0, text: "STOP" },
    { pos: 1.5 / 10, text: "SLOW" },
    { pos: 2.5 / 10, text: "DEAD" },
    { pos: 5 / 10, text: "SLOW" },
    { pos: 7 / 10, text: "HALF" },
    { pos: 9 / 10, text: "FULL" }
];

function EngineDrawer(options) {
    const canvas = document.getElementById(options.renderTo);
    const render = {
        options: options,
        canvas: canvas,
        leftpos: 0,
        rightpos: 0,
        draw: function() {
            const context = this.canvas.getContext("2d");
            const width = this.canvas.width;
            const height = this.canvas.height;
            context.fillStyle = "#636466";
            context.fillRect(0, 0, width, height);
            const graywidth = width / 5;

            const colospace = graywidth / 4;
            const colorwidth = colospace * 0.5;

            const ycenter = height / 2;
            const xcenter = width / 2;
            const scaleheight = 0.9 * height / 2;

            context.fillStyle = "gray";
            context.fillRect(0, 0, graywidth, height);
            context.fillRect(width - graywidth, 0, graywidth, height);

            const leftcoloroffset = graywidth + (colospace - colorwidth);
            const rightcoloroffset = width - graywidth - colospace;

            const topscalestart = ycenter - scaleheight;
            context.fillStyle = "green";
            context.fillRect(leftcoloroffset, topscalestart, colorwidth, scaleheight);
            context.fillRect(rightcoloroffset, topscalestart, colorwidth, scaleheight);
            context.fillStyle = "red";
            context.fillRect(leftcoloroffset, ycenter, colorwidth, scaleheight);
            context.fillRect(rightcoloroffset, ycenter, colorwidth, scaleheight);
            context.strokeStyle = "white";

            const scalewidth = 2 * colorwidth;
            const leftscaleoffset = leftcoloroffset + colorwidth;
            const rightscaleoffset = rightcoloroffset - scalewidth;
            DrawScale(context, leftscaleoffset, topscalestart, scalewidth, 2 * scaleheight);
            DrawScale(context, rightscaleoffset, topscalestart, scalewidth, 2 * scaleheight, true);

            context.font = "20px Arial";
            context.fillStyle = "white";

            const leftetxtscaleoffset = leftscaleoffset + scalewidth;
            const righttxtscaleoffset = rightscaleoffset;

            DrawScaleText(context, leftetxtscaleoffset, topscalestart, 2 * scaleheight);
            DrawScaleText(context, righttxtscaleoffset, topscalestart, 2 * scaleheight, true);
            context.font = "28px Arial";
            DrawLargeLabels(context, xcenter, ycenter, scaleheight);
            context.fillStyle = "red";
            context.strokeStyle = "white";
            DrawMarker(context, this.leftpos, leftscaleoffset, ycenter, scalewidth, scaleheight);
            DrawMarker(context, this.rightpos, rightcoloroffset, ycenter, -scalewidth, scaleheight);
        },
        handleMouse: function(x, y) {
            const width = this.canvas.width;
            const height = this.canvas.height;
            const ycenter = height / 2;
            const scaleheight = 0.9 * height / 2;
            let pos = rangeValue * (ycenter - y) / scaleheight;
            if (pos < -rangeValue)
                pos = -rangeValue;
            else if (pos > rangeValue)
                pos = rangeValue;
            const sideControlPart = 0.3;
            if (x < width * sideControlPart) {
                //left side control
                this.leftpos = pos;
                if (this.onLeftChanged)
                    this.onLeftChanged(pos);
            } else if (x > width * (1 - sideControlPart)) {
                //right side control
                this.rightpos = pos;
                if (this.onRightChanged)
                    this.onRightChanged(pos);
            } else {
                //both sides control
                this.leftpos = this.rightpos = pos;
                if (this.onLeftChanged)
                    this.onLeftChanged(pos);
                if (this.onRightChanged)
                    this.onRightChanged(pos);
            }
            this.draw();
        }
    };
    canvas.onmousemove = (e) => {
        if (e.buttons === 1)
            render.handleMouse(e.offsetX, e.offsetY);
    };
    canvas.onmousedown = (e) => {
        if (e.buttons === 1)
            render.handleMouse(e.offsetX, e.offsetY);
    };
    return render;
}

function DrawScale(context, left, top, width, height, isright) {
    const center = top + height / 2;
    const largewidth = isright === true ? -width : width;
    const smallwidth = largewidth / 2;
    const yrange = height / 2;
    const linebase = isright === true ? left + width : left;
    context.beginPath();
    for (let i = -rangeValue; i <= rangeValue; i++) {
        const width = (i % 2 === 0) ? largewidth : smallwidth;
        const x = linebase;
        const y = center - (yrange * i) / rangeValue;
        context.moveTo(x, y);
        context.lineTo(x + width, y);
    }
    context.moveTo(linebase, top);
    context.lineTo(linebase, top + height);
    context.stroke();
}

function DrawScaleText(context, linebase, top, height, isright) {
    const center = top + height / 2;
    const yrange = height / 2;
    context.textAlign = isright === true ? "right" : "left";
    context.textBaseline = "middle";
    for (let i = -rangeValue; i <= rangeValue; i += 2) {
        const y = center - (yrange * i) / rangeValue;
        context.fillText(Math.abs(i), linebase, y);
    }

}

function DrawLargeLabels(context, xcenter, ycenter, yscale) {
    context.textAlign = "center";
    context.textBaseline = "middle";
    for (const ref of largelabels) {
        const y = ycenter - yscale * ref.pos;
        context.fillText(ref.text, xcenter, y);
    }
}

function DrawMarker(context, position, leftscaleoffset, ycenter, scalewidth, yscale) {
    context.beginPath();
    const tpos = position / rangeValue;
    const ypos = ycenter - yscale * tpos;
    const ypost = ycenter - yscale * (tpos - 0.05);
    const yposb = ycenter - yscale * (tpos + 0.05);
    context.moveTo(leftscaleoffset + scalewidth, ypos);
    context.lineTo(leftscaleoffset, ypost);
    context.lineTo(leftscaleoffset, yposb);
    context.closePath();
    context.fill();
    context.stroke();
}