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
