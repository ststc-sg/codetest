"use strict";
let lastpagedata = {};
//list of pages and page names additional parameter redirectid declares that page not accessed from main menu directly
const pageDescription =
[
    { id: "indicatorport.html", label: "INDICATOR" },
    { id: "indicatorstbd.html", label: "INDICATOR", redirectid: "indicatorport.html" },
    { id: "engine.html", label: "ENGINE<br>TELEGRAPH" },
];
const pagesList = new Map();
const updatepageurl = "EngineState.json";

function Startup() {
    addEventListener("message", PassCommandFromChildPageToServer, false); //subscribe to child page commands
    const referer = document.referrer;
    const mastaddhomebutton = referer.search("autopilotandbridge")!==-1;
    const target = document.getElementById("screen");
    const menutarget = document.getElementById("sidemenucontainer");
    if (!pagesList.has()) {
        for (const pageinfo of pageDescription) {
            const container = document.createElement("iframe");
            container.src = pageinfo.id;
            container.style.display = "none";
            container.style.width = "100%";
            container.style.height = "100%";
            pagesList.set(pageinfo.id, { info: pageinfo, ref: container });
            target.appendChild(container);
        }
        for (const pageinfo of pageDescription) {
            if (pageinfo.redirectid != undefined)
                continue; //skip seconds class pages
            const callbutton = document.createElement("button");
            callbutton.id = pageinfo.id;
            callbutton.classList.add("menubutton");
            callbutton.style.height = 100 / (pageDescription.length + 2) + "%";
            callbutton.innerHTML = pageinfo.label;
            callbutton.onclick = () => NavigateTo(pageinfo.id);
            menutarget.appendChild(callbutton);
        }
        {
            const callbutton = document.createElement("button");
            callbutton.classList.add("menubutton");
            callbutton.classList.add("menubuttonchecked");
            callbutton.style.height = 100 / (pageDescription.length + 2) + "%";
            callbutton.innerHTML = "DAY/NIGHT";
            callbutton.onclick = () => SwitchDayNight();
            menutarget.appendChild(callbutton);
        }
        if (mastaddhomebutton) 
            makehomebutton(menutarget);
    }
    setInterval(async () => request_periodical_update(), 100);
    UseJsonForUpdate({ active: "indicatorport.html"});

}


let updateRequest;
let previousresult = null;
function request_periodical_update() {
    if (!updateRequest) {
        updateRequest = new XMLHttpRequest();
        updateRequest.onreadystatechange = function() {
            if (updateRequest.readyState == 4) {
                if (updateRequest.status == 200) {
                    if (previousresult !== updateRequest.responseText) {
                        previousresult = updateRequest.responseText;
                        const data = JSON.parse(updateRequest.responseText);
                        UseJsonForUpdate(data);
                    }
                }
            }
        };
    }
    if (updateRequest.readyState == 4 || updateRequest.readyState == 0) {
        updateRequest.open("POST", updatepageurl, true);
        updateRequest.send(null);
    }
}



function SwitchDayNight() {
    PushJsonCommand("daynightswitch");
}
function NavigateTo(target) {
    PushJsonCommand("navigate", target);
}
function PushJsonCommand(command, parameter) {
    const postdata = {
        command: command,
        parameter: parameter
    };
    PushJsonCommandWithObject(postdata);
}
async function PushJsonCommandWithObject(datamessage) {
    let response = await fetch(updatepageurl, {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(datamessage)
    });
    let result = await response.json();
    UseJsonForUpdate(result);
}


function UseJsonForUpdate(data) {
    
     if (data.active !== lastpagedata.active) //if active page chnaged need switch page and menu button
    {
        //switching active page
        pagesList.forEach((objectref, key) => {
            objectref.ref.style.display = key === data.active ? "block" : "none";
        });
        //get active page informations
        const pajeref = pagesList.get(data.active);
        if (pajeref != null) {
            const info = pajeref.info;
            //check if this page is second class and find original button id
            const buttonid = info.redirectid == null ? info.id : info.redirectid;
            //search all menu buttons
            const selector = document.querySelectorAll("#sidemenucontainer>button");
            for (let button of selector) {
                if (button.id == null || button.id=='')
                    continue;
                if (button.id === buttonid)
                    button.classList.add("menubuttonchecked"); //active page buttons
                else
                    button.classList.remove("menubuttonchecked"); //over button
            }
        }
    }
    UpdateLocalData(data);
    lastpagedata = data;
    //post data to active page using message api
    const ref = pagesList.get(data.active);
    if (ref != null) {
        ref.ref.contentWindow.postMessage(data, "*");
    } else {
        alert(`page with id ${data.active} not found `);
    }


    //pass updated data to all pages

}

function PassCommandFromChildPageToServer(evt) {
    const data = event.data;
    if ("command" in data) {
        PushJsonCommandWithObject(data); //pass command to server
    }
}

function UpdateTextField(name, value) {
    const querystring = `a[id='${name}']`;
    const selector = document.querySelector(querystring);
    if (selector) {
        selector.innerHTML = value;
    }
}

function UpdateLocalData(data) {
    for (const [key, value] of Object.entries(data)) {
        switch (key) {
            case "ThemeStyle":
            {
                UpdateTheme(value);
                break;
            }
        }
    }
}
//create home button for main page return
function makehomebutton(menutarget) {
    const callbutton = document.createElement("button");
    callbutton.classList.add("menubutton");
    callbutton.classList.add("menubuttonchecked");
    callbutton.style.height = 100 / (pageDescription.length + 2) + "%";
    callbutton.innerHTML = '<img src="../autopilotandbridge/home.png" style="max-height: 40px"></img>';
    callbutton.onclick = () => window.location.href = "../autopilotandbridge/main.html";
    menutarget.appendChild(callbutton);
}
function UpdateTheme(value) {
    if (value == 0) {
        document.documentElement.style.setProperty('--backgroundColourMain', '#e6e9f2');
        document.documentElement.style.setProperty('--backgroundColour', '#e6e9f2');
        document.documentElement.style.setProperty('--buttonColour', '#105689');
        document.documentElement.style.setProperty('--buttonCheckedColour', '#606060');
        document.documentElement.style.setProperty('--indicatorHeader', '#062db9');
    } else {
        document.documentElement.style.setProperty("--backgroundColourMain", "#1c1c1cff");
        document.documentElement.style.setProperty("--backgroundColour", "#606060");
        document.documentElement.style.setProperty("--buttonColour", "#105689");
        document.documentElement.style.setProperty("--buttonCheckedColour", "#606060");
        document.documentElement.style.setProperty("--indicatorHeader", "#1c1c1cff");
    }
}