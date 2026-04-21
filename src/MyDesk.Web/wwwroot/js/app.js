window.downloadBase64File = function (base64, mimeType, fileName) {
    var bytes = atob(base64);
    var buf = new ArrayBuffer(bytes.length);
    var arr = new Uint8Array(buf);
    for (var i = 0; i < bytes.length; i++) arr[i] = bytes.charCodeAt(i);
    var blob = new Blob([buf], { type: mimeType });
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

// ── Web Speech API (Proposal #272 voice input) ─────────────────────────────
let _speechRecognition = null;

window.startSpeechRecognition = function (dotNetRef) {
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!SpeechRecognition) {
        throw new Error('Speech recognition not supported in this browser. Use Chrome or Edge.');
    }

    _speechRecognition = new SpeechRecognition();
    _speechRecognition.lang = 'en-AU';
    _speechRecognition.continuous = false;
    _speechRecognition.interimResults = false;

    _speechRecognition.onresult = function (event) {
        const transcript = event.results[0][0].transcript;
        dotNetRef.invokeMethodAsync('OnSpeechResult', transcript);
    };

    _speechRecognition.onend = function () {
        dotNetRef.invokeMethodAsync('OnSpeechEnd');
    };

    _speechRecognition.onerror = function (event) {
        console.error('Speech recognition error:', event.error);
        dotNetRef.invokeMethodAsync('OnSpeechEnd');
    };

    _speechRecognition.start();
};

window.stopSpeechRecognition = function () {
    if (_speechRecognition) {
        _speechRecognition.stop();
        _speechRecognition = null;
    }
};

// Web Speech API helpers for voice input
window.startVoiceRecognition = async function() {
    if (!('webkitSpeechRecognition' in window) && !('SpeechRecognition' in window)) {
        console.error('Speech recognition not supported');
        return null;
    }

    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    const recognition = new SpeechRecognition();
    recognition.lang = 'en-AU';
    recognition.continuous = false;
    recognition.interimResults = false;

    return recognition;
};

window.startDictation = async function(onResult, onError) {
    const recognition = await window.startVoiceRecognition();
    if (!recognition) return null;

    recognition.onresult = (event) => {
        const transcript = event.results[0][0].transcript;
        if (onResult) onResult(transcript);
    };

    recognition.onerror = (event) => {
        console.error('Speech recognition error:', event.error);
        if (onError) onError(event.error);
    };

    recognition.start();
    return recognition;
};

// Download file helper for CSV export
window.downloadFile = function(filename, content) {
    const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', filename);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
