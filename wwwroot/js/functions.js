
globalThis.GetHeight = () => window.innerHeight;
globalThis.GetWidth = () => window.innerWidth;
globalThis.GetMonacoTheme = () => localStorage.getItem('MonacoTheme');
globalThis.HideFooter = () => document.getElementById('site_footer').style.display = 'none';

globalThis.GetSetTheme = () => {
    let theme = localStorage.getItem('RadzenTheme');
    if (!theme) {
        const darkThemeMq = window.matchMedia("(prefers-color-scheme: dark)");
        if (darkThemeMq.matches) {
            theme = 'dark';
        } else {
            theme = 'default';
        }
        localStorage.setItem('RadzenTheme', theme);
    }
    let themeLink = document.getElementById('theme');
    themeLink.href = `_content/Radzen.Blazor/css/${theme}.css`;
    return theme;
}

let lastScrollHeight = 0;
globalThis.SetScrollEvent = () => {
    const body = document.getElementById('site_body');
    body.addEventListener('scroll', (event) => {
        if ((lastScrollHeight + 100) < event.target.scrollTop) {
            document.getElementById('site_header').style.display = "none"
            lastScrollHeight = event.target.scrollTop;
        }
        else if ((lastScrollHeight - 100) > event.target.scrollTop || event.target.scrollTop === 0) {
            document.getElementById('site_header').style.display = "block"
            lastScrollHeight = event.target.scrollTop;
        }
    });
}

globalThis.RunUserScript = (userCode) => {
    const input = document.getElementById('input').value;
    if (!input) {
        alert('Please provide input');
        return;
    }
    const myWorker = new Worker('js/userScriptWorker.js');

    if (!myWorker) {
        alert('Web Worker not found.');
        return;
    }

    const timer = setTimeout(() => {
        myWorker.terminate();
        alert('Script took too long to execute. Terminated.');
    }, 1500);

    myWorker.onmessage = (e) => {
        document.getElementById('output').value = `${e.data}`;
        clearTimeout(timer)
    };

    myWorker.onerror = (e) => {
        myWorker.terminate();
        alert(e.data);
    };

    myWorker.postMessage({ code: userCode, input: input });
};

globalThis.RunWorkerScript = (workerScript) => {
    const myWorker = new Worker('service-worker.js');

    const timer = setTimeout(() => {
        myWorker.terminate();
        alert('Script took too long to execute. Terminated.');
    }, 1000);

    myWorker.onmessage = (e) => {
        clearTimeout(timer)
        return e.data
    };

    myWorker.onerror = (e) => {
        myWorker.terminate();
        alert(e.data);
    };

    myWorker.postMessage({ input: workerScript });
};

globalThis.setSource = async (elementId, stream, contentType, title) => {
    const arrayBuffer = await stream.arrayBuffer();
    let blobOptions = {};
    if (contentType) {
        blobOptions['type'] = contentType;
    }
    const blob = new Blob([arrayBuffer], blobOptions);
    const url = URL.createObjectURL(blob);
    const element = document.getElementById(elementId);
    element.title = title;
    element.onload = () => {
        URL.revokeObjectURL(url);
    }
    element.src = url;
}