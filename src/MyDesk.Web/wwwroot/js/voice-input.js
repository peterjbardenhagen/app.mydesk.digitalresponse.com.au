// ============================================================
// MyDesk Voice Input - Web Speech API Integration
// ============================================================

(function () {
    'use strict';

    let recognition = null;
    let isRecording = false;
    let currentCallback = null;

    const VoiceInput = {
        /**
         * Check if speech recognition is supported
         */
        isSupported: function () {
            return 'webkitSpeechRecognition' in window ||
                   'SpeechRecognition' in window;
        },

        /**
         * Start voice recording
         * @param {Function} onResult - Callback with recognized text
         * @param {Function} onError - Callback with error message
         */
        start: function (onResult, onError) {
            if (this.isRecording()) {
                return;
            }

            if (!this.isSupported()) {
                if (onError) onError('Speech recognition not supported in this browser');
                return;
            }

            try {
                const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
                recognition = new SpeechRecognition();
                recognition.continuous = false;
                recognition.interimResults = true;
                recognition.lang = 'en-AU';
                recognition.maxAlternatives = 1;

                currentCallback = onResult;

                recognition.onresult = function (event) {
                    let interim = '';
                    let final = '';

                    for (let i = event.resultIndex; i < event.results.length; i++) {
                        const transcript = event.results[i][0].transcript;
                        if (event.results[i].isFinal) {
                            final += transcript;
                        } else {
                            interim += transcript;
                        }
                    }

                    // Dispatch events for UI updates
                    const detail = { interim: interim, final: final };
                    document.dispatchEvent(new CustomEvent('voice:result', { detail: detail }));

                    if (final && currentCallback) {
                        currentCallback(final);
                        currentCallback = null;
                    }
                };

                recognition.onerror = function (event) {
                    isRecording = false;
                    document.dispatchEvent(new CustomEvent('voice:end', {}));
                    
                    let errorMsg = 'Voice input error occurred';
                    switch (event.error) {
                        case 'no-speech':
                            errorMsg = 'No speech detected. Please try again.';
                            break;
                        case 'aborted':
                            errorMsg = 'Voice input was cancelled.';
                            break;
                        case 'audio-capture':
                            errorMsg = 'No microphone found. Please check your microphone.';
                            break;
                        case 'not-allowed':
                            errorMsg = 'Microphone permission denied. Please allow microphone access.';
                            break;
                        case 'network':
                            errorMsg = 'Network error occurred. Please check your connection.';
                            break;
                    }
                    
                    if (onError) onError(errorMsg);
                };

                recognition.onend = function () {
                    isRecording = false;
                    document.dispatchEvent(new CustomEvent('voice:end', {}));
                };

                recognition.start();
                isRecording = true;
                document.dispatchEvent(new CustomEvent('voice:start', {}));

            } catch (err) {
                if (onError) onError('Failed to start voice input: ' + err.message);
            }
        },

        /**
         * Stop voice recording
         */
        stop: function () {
            if (recognition && isRecording) {
                try {
                    recognition.stop();
                } catch (e) {
                    // Ignore stop errors
                }
            }
            isRecording = false;
            currentCallback = null;
            document.dispatchEvent(new CustomEvent('voice:end', {}));
        },

        /**
         * Check if currently recording
         */
        isRecording: function () {
            return isRecording;
        },

        /**
         * Check for native Android voice support (WebView bridge)
         */
        useAndroidNative: function () {
            return typeof window.MyDeskAndroid !== 'undefined' ||
                   typeof window.MyDeskAndroidBridge !== 'undefined';
        },

        /**
         * Start Android native voice recognition
         */
        startAndroidVoice: function () {
            if (window.MyDeskAndroid && window.MyDeskAndroid.startVoiceRecognition) {
                window.MyDeskAndroid.startVoiceRecognition();
                return true;
            }
            if (window.MyDeskAndroidBridge && window.MyDeskAndroidBridge.startVoiceRecognition) {
                window.MyDeskAndroidBridge.startVoiceRecognition();
                return true;
            }
            return false;
        }
    };

    // Expose globally
    window.MyDeskVoice = VoiceInput;

    // Auto-detect Android WebView and use native voice
    if (VoiceInput.useAndroidNative()) {
        console.log('MyDesk Voice: Using Android native voice recognition');
    }

    console.log('MyDesk Voice Input initialized');

})();