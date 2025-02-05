// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
//self.addEventListener('fetch', (e) => eval(e.data));

onmessage = (e) => {
    eval(e.data.input);
}