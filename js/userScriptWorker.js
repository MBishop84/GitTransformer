onmessage = (e) => {
    const code = 'const input = `' + e.data.input + '`; let output = ""; ' + e.data.code + '; postMessage(output);';
    eval(code);
}