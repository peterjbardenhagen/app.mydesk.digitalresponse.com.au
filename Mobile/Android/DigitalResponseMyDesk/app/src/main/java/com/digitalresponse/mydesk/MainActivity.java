package com.digitalresponse.mydesk;

import android.content.Context;
import android.content.Intent;
import android.graphics.Bitmap;
import android.net.ConnectivityManager;
import android.net.NetworkInfo;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.speech.RecognizerIntent;
import android.view.KeyEvent;
import android.view.View;
import android.webkit.CookieManager;
import android.webkit.JavascriptInterface;
import android.webkit.PermissionRequest;
import android.webkit.ValueCallback;
import android.webkit.WebChromeClient;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.FrameLayout;
import android.widget.ProgressBar;
import android.widget.Toast;

import androidx.annotation.Nullable;
import androidx.appcompat.app.AlertDialog;
import androidx.appcompat.app.AppCompatActivity;
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout;

import com.google.android.material.snackbar.Snackbar;

import java.util.ArrayList;
import java.util.Locale;

public class MainActivity extends AppCompatActivity {

    private static final int REQUEST_CODE_SPEECH_INPUT = 1001;
    private static final int REQUEST_CODE_FILE_CHOOSER = 1002;

    private WebView webView;
    private ProgressBar progressBar;
    private SwipeRefreshLayout swipeRefreshLayout;
    private FrameLayout loadingOverlay;

    private ValueCallback<Uri[]> filePathCallback;
    private String currentUrl = "https://app.dr.mydesk.digitalresponse.com.au";

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        webView = findViewById(R.id.webView);
        progressBar = findViewById(R.id.progressBar);
        swipeRefreshLayout = findViewById(R.id.swipeRefresh);
        loadingOverlay = findViewById(R.id.loadingOverlay);

        setupWebView();
        setupSwipeRefresh();

        loadUrl(currentUrl);
    }

    private void setupWebView() {
        WebSettings settings = webView.getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);
        settings.setDatabaseEnabled(true);
        settings.setCacheMode(WebSettings.LOAD_DEFAULT);
        settings.setLoadWithOverviewMode(true);
        settings.setUseWideViewPort(true);
        settings.setBuiltInZoomControls(false);
        settings.setDisplayZoomControls(false);
        settings.setSupportZoom(true);
        settings.setAllowFileAccess(false);
        settings.setAllowContentAccess(false);
        settings.setGeolocationEnabled(true);

        // Enable modern web features
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
            CookieManager.getInstance().setAcceptThirdPartyCookies(webView, true);
            webView.getSettings().setMixedContentMode(WebSettings.MIXED_CONTENT_COMPATIBILITY_MODE);
        }

        // Enable remote debugging
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.KITKAT) {
            WebView.setWebContentsDebuggingEnabled(true);
        }

        // JavaScript interface for voice recognition
        webView.addJavascriptInterface(new WebAppInterface(), "MyDeskAndroid");

        webView.setWebViewClient(new WebViewClient() {
            @Override
            public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                String url = request.getUrl().toString();

                // Handle tel: and mailto: links
                if (url.startsWith("tel:")) {
                    Intent intent = new Intent(Intent.ACTION_DIAL, Uri.parse(url));
                    startActivity(intent);
                    return true;
                } else if (url.startsWith("mailto:")) {
                    Intent intent = new Intent(Intent.ACTION_SENDTO, Uri.parse(url));
                    startActivity(intent);
                    return true;
                }

                // Load all other URLs in the WebView
                return false;
            }

            @Override
            public void onPageStarted(WebView view, String url, Bitmap favicon) {
                super.onPageStarted(view, url, favicon);
                currentUrl = url;
                progressBar.setVisibility(View.VISIBLE);
                progressBar.setProgress(0);
                loadingOverlay.setVisibility(View.VISIBLE);
                swipeRefreshLayout.setRefreshing(true);
            }

            @Override
            public void onPageFinished(WebView view, String url) {
                super.onPageFinished(view, url);
                currentUrl = url;
                progressBar.setVisibility(View.GONE);
                loadingOverlay.setVisibility(View.GONE);
                swipeRefreshLayout.setRefreshing(false);
                injectNativeBridge();
            }

            @Override
            public void onReceivedError(WebView view, int errorCode, String description, String failingUrl) {
                super.onReceivedError(view, errorCode, description, failingUrl);
                progressBar.setVisibility(View.GONE);
                swipeRefreshLayout.setRefreshing(false);
                
                // Show offline page if no internet
                if (!isNetworkAvailable()) {
                    showOfflinePage();
                }
            }
        });

        webView.setWebChromeClient(new WebChromeClient() {
            @Override
            public void onProgressChanged(WebView view, int newProgress) {
                super.onProgressChanged(view, newProgress);
                progressBar.setProgress(newProgress);
            }

            @Override
            public void onPermissionRequest(PermissionRequest request) {
                // Auto-grant microphone for voice input
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
                    request.grant(request.getResources());
                }
            }

            @Override
            public boolean onShowFileChooser(WebView webView, ValueCallback<Uri[]> filePathCallback,
                                             FileChooserParams fileChooserParams) {
                MainActivity.this.filePathCallback = filePathCallback;
                Intent intent = fileChooserParams.createIntent();
                try {
                    startActivityForResult(intent, REQUEST_CODE_FILE_CHOOSER);
                } catch (Exception e) {
                    MainActivity.this.filePathCallback = null;
                    return false;
                }
                return true;
            }
        });
    }

    private void setupSwipeRefresh() {
        swipeRefreshLayout.setOnRefreshListener(() -> {
            webView.reload();
        });
        swipeRefreshLayout.setColorSchemeResources(
            R.color.teal_500,
            R.color.purple_500,
            R.color.amber_500
        );
    }

    private void loadUrl(String url) {
        if (isNetworkAvailable()) {
            webView.loadUrl(url);
        } else {
            showOfflinePage();
        }
    }

    private boolean isNetworkAvailable() {
        ConnectivityManager cm = (ConnectivityManager) getSystemService(Context.CONNECTIVITY_SERVICE);
        NetworkInfo activeNetwork = cm.getActiveNetworkInfo();
        return activeNetwork != null && activeNetwork.isConnectedOrConnecting();
    }

    private void showOfflinePage() {
        String offlineHtml = "<!DOCTYPE html><html><head><meta name='viewport' content='width=device-width, initial-scale=1'><style>" +
            "body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; display: flex; " +
            "justify-content: center; align-items: center; min-height: 100vh; margin: 0; background: #f8f9fa; color: #333; }" +
            ".container { text-align: center; padding: 24px; }" +
            ".icon { font-size: 64px; margin-bottom: 16px; color: #a855f7; }" +
            "h1 { font-size: 24px; font-weight: 700; margin-bottom: 8px; color: #1a1a1a; }" +
            "p { font-size: 16px; color: #666; margin-bottom: 24px; line-height: 1.5; }" +
            ".btn { display: inline-block; padding: 12px 24px; background: #a855f7; color: white; " +
            "border: none; border-radius: 12px; font-size: 16px; font-weight: 600; cursor: pointer; }" +
            "</style></head><body>" +
            "<div class='container'>" +
            "<div class='icon'>📡</div>" +
            "<h1>No Internet Connection</h1>" +
            "<p>Please check your connection and try again.</p>" +
            "</div></body></html>";
        webView.loadDataWithBaseURL(null, offlineHtml, "text/html", "UTF-8", null);
    }

    private void injectNativeBridge() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.KITKAT) {
            webView.evaluateJavascript(
                "if (!window.MyDeskAndroidBridge) {" +
                "  window.MyDeskAndroidBridge = {" +
                "    startVoiceRecognition: function() {" +
                "      window.MyDeskAndroid.startVoiceRecognition();" +
                "    }," +
                "    getPlatform: function() { return 'android'; }," +
                "    getAppVersion: function() { return '1.0.0'; }" +
                "  };" +
                "}", null);
        }
    }

    /**
     * Handle voice input from JavaScript
     */
    private void startVoiceRecognition() {
        Intent intent = new Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH);
        intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL, RecognizerIntent.LANGUAGE_MODEL_FREE_FORM);
        intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE, Locale.getDefault());
        intent.putExtra(RecognizerIntent.EXTRA_PROMPT, "Speak your question for MyDesk AI");
        
        try {
            startActivityForResult(intent, REQUEST_CODE_SPEECH_INPUT);
        } catch (Exception e) {
            Toast.makeText(this, "Voice recognition not supported on this device", Toast.LENGTH_SHORT).show();
        }
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, @Nullable Intent data) {
        super.onActivityResult(requestCode, resultCode, data);

        if (requestCode == REQUEST_CODE_SPEECH_INPUT && resultCode == RESULT_OK && data != null) {
            ArrayList<String> result = data.getStringArrayListExtra(RecognizerIntent.EXTRA_RESULTS);
            if (result != null && !result.isEmpty()) {
                String spokenText = result.get(0);
                // Send the recognized text to the WebView's Ask AI input
                String escapedText = spokenText.replace("'", "\\'").replace("\n", "\\n");
                webView.evaluateJavascript(
                    "javascript:(function() {" +
                    "  var input = document.querySelector('[data-voice-input]') || " +
                    "    document.querySelector('textarea, input[type=\"text\"], [contenteditable]');" +
                    "  if (input) {" +
                    "    input.value = '" + escapedText + "';" +
                    "    input.dispatchEvent(new Event('input', { bubbles: true }));" +
                    "    input.dispatchEvent(new Event('change', { bubbles: true }));" +
                    "  }" +
                    "})();", null);
            }
        } else if (requestCode == REQUEST_CODE_FILE_CHOOSER) {
            if (filePathCallback != null) {
                Uri[] results = null;
                if (resultCode == RESULT_OK && data != null) {
                    String dataString = data.getDataString();
                    if (dataString != null) {
                        results = new Uri[]{Uri.parse(dataString)};
                    }
                }
                filePathCallback.onReceiveValue(results);
                filePathCallback = null;
            }
        }
    }

    @Override
    public boolean onKeyDown(int keyCode, KeyEvent event) {
        if (keyCode == KeyEvent.KEYCODE_BACK && webView.canGoBack()) {
            webView.goBack();
            return true;
        }
        return super.onKeyDown(keyCode, event);
    }

    @Override
    public void onBackPressed() {
        if (webView.canGoBack()) {
            webView.goBack();
        } else {
            // Exit confirmation
            new AlertDialog.Builder(this)
                .setTitle("Exit MyDesk")
                .setMessage("Are you sure you want to exit?")
                .setPositiveButton("Yes", (dialog, which) -> finish())
                .setNegativeButton("No", null)
                .show();
        }
    }

    /**
     * JavaScript interface for communication between WebView and native code
     */
    public class WebAppInterface {
        @JavascriptInterface
        public void startVoiceRecognition() {
            runOnUiThread(() -> MainActivity.this.startVoiceRecognition());
        }

        @JavascriptInterface
        public String getPlatform() {
            return "android";
        }

        @JavascriptInterface
        public String getAppVersion() {
            return "1.0.0";
        }

        @JavascriptInterface
        public boolean isOnline() {
            return isNetworkAvailable();
        }

        @JavascriptInterface
        public void shareText(String text) {
            Intent shareIntent = new Intent(Intent.ACTION_SEND);
            shareIntent.setType("text/plain");
            shareIntent.putExtra(Intent.EXTRA_TEXT, text);
            startActivity(Intent.createChooser(shareIntent, "Share via"));
        }

        @JavascriptInterface
        public void showToast(String message) {
            runOnUiThread(() -> Toast.makeText(MainActivity.this, message, Toast.LENGTH_SHORT).show());
        }
    }
}