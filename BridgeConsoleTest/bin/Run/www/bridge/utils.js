"use strict";
const autosubscribtions = new Map();
let outputTag;
function StartIncrement() {
    PushJsonCommand("startincrement");
}

function StartDecrement() {
    PushJsonCommand("startdecrement");
}

function EndValueChange() {
    PushJsonCommand("endvaluechange");
}

function SoftIncrement() {
    PushJsonCommand("increment");
}

function SoftDecrement() {
    PushJsonCommand("decrement");
}

function SwitchDayNight() {
    PushJsonCommand("daynightswitch");
}

function NavigateBack() {
    PushJsonCommand("navigateback");
}

function CommitChanges() {
    PushJsonCommand("commit");
    NavigateBack();
}

function NavigateTo(target) {
    PushJsonCommand("navigate", target);
}

function ButtonClick(id) {
    PushJsonCommand("buttonclick", id);
}
function PressToggleButton(id) {
    PushJsonCommand("pressToggle", id);
}
function ReleaseToggleButton(id) {
    PushJsonCommand("releaseToggle", id);
}
function NavigateStacked(target) {
    PushJsonCommand("navigatestacked", target);
}

function EditSelectedParameterSet() {
    const selectid = document.querySelector('input[name="preset"]:checked').value;
    PushJsonCommand("editpreset", selectid);
    NavigateTo("parameterModify.html");
}

function PushJsonCommand(command, parameter) {
    const postdata = {
        command: command,
        parameter: parameter,
        outputTag: outputTag
    };
    window.parent.postMessage(postdata, "*");
}

function SubscribeToUpdate() { //start periodic fetch json and update
    window.addEventListener("message", handleMessageFromParentFrame, false);
    SearchActiveElements();
}
function SearchActiveElements() {
    //find all buttons
    {
        const allampbuttons = document.querySelectorAll(".lampbutton");
        for (let button of allampbuttons) {
            button.onclick = () => PushJsonCommand("buttonclick", button.id);
            autosubscribtions.set(button.id, (state) => ChangeLampButtonState(button, state));
        }
    }
    //find all lamps
    {
        const allamps = document.querySelectorAll(".signallamp");
        for (let lamp of allamps) {
            RegisterActiveElement(lamp.id, (state) => ChangeLampState(lamp, state));
        }
    }
    //find all indicators
    {
        const alltextind = document.querySelectorAll(".indicator");
        for (let indicator of alltextind) {
            RegisterActiveElement(indicator.id, (state) => ChangeTextState(indicator, state));
        }
    }
    //imagebuttons
    {
        const allImageButtons = document.querySelectorAll(".imageButton");
        for (let button of allImageButtons) {
            RegisterActiveElement(button.id, (state) => SwitchImageButtonState(button, state));
        }
    }
    //imagebuttons
    {
        const allImageButtons = document.querySelectorAll(".buttonwithlamp");
        for (let button of allImageButtons) {
            button.onclick = () => PushJsonCommand("buttonclick", button.id);
            RegisterActiveElement(button.id, (state) => SwitchButtonWithLampState(button, state));
        }
    }
    
}
function RegisterActiveElement(id, callback) {
    autosubscribtions.set(id, callback);
}
function RegisternOutputTag(tag) {
    outputTag = tag;
}

function ChangeLampButtonState(button,state) {
    if (state === true) 
        button.classList.add("lampbuttonon");
     else 
        button.classList.remove("lampbuttonon");
}
function SwitchButtonWithLampState(button, state) {
    if (state === true)
        button.classList.add("buttonwithlampon");
    else
        button.classList.remove("buttonwithlampon");
}

function ChangeLampState(button, state) {
    if (state === true) 
        button.classList.add("signallampon");
     else 
        button.classList.remove("signallampon");
}
function SwitchImageButtonState(button, state) {
    const turncssClass = "imageButtonOn";
    if (state === true)
        button.classList.add(turncssClass);
    else
        button.classList.remove(turncssClass);
}

function ChangeTextState(ind, state) {
    ind.innerHTML = state;
}
function ProcessAutoElements(key, value) {
    const ref = autosubscribtions.get(key);
    if (ref == null)
        return false;
    ref(value);
    return true;
}
function handleMessageFromParentFrame(evt) {
    requestAnimationFrame(() => UseJsonForUpdate(evt.data));
}

function UseJsonForUpdate(data) {
    for (const [key, value] of Object.entries(data)) {
        ProcessAutoElements(key, value);
        switch (key) {
        case "ThemeStyle":
        {
            UpdateTheme(value);
            break;
        }
        case "headingdiff":
        case "crosstrackdistance":
        case "actualrudder":
        case "actualrudder2":
            UpdateGauge(key, value);
            break;
        case "log":
        case "gyro1":
        case "heading":
        case "setcog":
        case "setrot":
        case "setheading":
        case "setrudder":
        case "setrudder2":
        case "actualrudderindicator":
        case "actualrudderindicator2":
        case "distancetocourseline":
            UpdateFloatTextField(key, value);
            break;
        case "preset":
            UpdateParametresList(value);
        default:
            UpdateTextField(key, value);
        }
    }
}

function UpdateTheme(value) {
    if (value == 0) {
        document.documentElement.style.setProperty("--backgroundColourMain", "#e6e9f2");
        document.documentElement.style.setProperty("--backgroundColour", "#e6e9f2");
        document.documentElement.style.setProperty("--buttonColour", "#105689");
        document.documentElement.style.setProperty("--buttonCheckedColour", "#606060");
        document.documentElement.style.setProperty("--indicatorHeader", "#606060");
    } else {
        document.documentElement.style.setProperty("--backgroundColourMain", "#636466ff");
        document.documentElement.style.setProperty("--backgroundColour", "#606060");
        document.documentElement.style.setProperty("--buttonColour", "#606060");
        document.documentElement.style.setProperty("--buttonCheckedColour", "#105689");
        document.documentElement.style.setProperty("--indicatorHeader", "#1c1c1cff");
    }
}

function UpdateTextField(name, value) {
    const querystring = `a[id='${name}']`;
    const selector = document.querySelector(querystring);
    if (selector) {
        selector.innerHTML = value;
    }
}

function UpdateFloatTextField(name, value) {
    const querystring = `a[id='${name}']`;
    const selector = document.querySelector(querystring);
    if (selector) {
        selector.innerHTML = value.toFixed(1);
    }
}

function UpdateGauge(name, value) {
    require(["dijit/registry"],
        function(registry) {
            const tableparent = registry.byId(name);
            if (tableparent) {
                tableparent.set("value", value);
            }
        });
}

function UpdateParametresList(parametres) {
    const count = parametres.length;
    for (let i = 0; i < count; i++) {
        const state = parametres[i];


        {
            const queryyaw = `a[id='y${i + 1}']`;
            const selector = document.querySelector(queryyaw);
            if (!selector) return;
            selector.innerHTML = state.yaw;
        }
        {
            const queryruddder = `a[id='r${i + 1}']`;
            const selector = document.querySelector(queryruddder);
            if (!selector) return;
            selector.innerHTML = state.rudder;
        }
        {
            const querycrudder = `a[id='c${i + 1}']`;
            const selector = document.querySelector(querycrudder);
            if (!selector) return;
            selector.innerHTML = state.contrudder;
        }
    }
}

