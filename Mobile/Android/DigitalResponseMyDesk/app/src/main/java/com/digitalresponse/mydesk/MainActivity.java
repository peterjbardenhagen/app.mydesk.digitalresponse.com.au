package com.digitalresponse.mydesk;

import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.graphics.Bitmap;
import android.graphics.Color;
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

    // Local chat app — the primary mobile experience
    private static final String LOCAL_APP_URL = "file:///android_asset/app.html";
    // Full web app (fallback / "open in browser" target)
    private static final String WEB_APP_URL = "https://app.mydesk.digitalresponse.com.au";

    private static final String PREFS_NAME = "MyDeskPrefs";
    private static final String PREF_THEME = "theme";
    private static final String PREF_BRAND = "brand";

    private WebView webView;
    private ProgressBar progressBar;
    private SwipeRefreshLayout swipeRefreshLayout;
    private FrameLayout loadingOverlay;

    private ValueCallback<Uri[]> filePathCallback;
    private SharedPreferences prefs;
    private String currentTheme;
    private String currentBrand;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        // Read saved preferences before setting content view
        prefs = getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        currentTheme = prefs.getString(PREF_THEME, "dark");
        currentBrand = prefs.getString(PREF_BRAND, "techlight");

        // Apply window background before layout inflate to avoid flicker
        applyWindowStyle();

        setContentView(R.layout.activity_main);

        webView = findViewById(R.id.webView);
        progressBar = findViewById(R.id.progressBar);
        swipeRefreshLayout = findViewById(R.id.swipeRefresh);
        loadingOverlay = findViewById(R.id.loadingOverlay);

        setupWebView();
        setupSwipeRefresh();

        // Load the embedded MyDesk Chat app
        webView.loadUrl(LOCAL_APP_URL);
    }

    private void applyWindowStyle() {
        boolean isDark = "dark".equals(currentTheme);
        int bg = isDark ? Color.parseColor("#0d0d1a") : Color.parseColor("#f4f4f8");
        getWindow().setBackgroundDrawableResource(android.R.color.transparent);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
            // Status bar and navigation bar colour from brand
            int brandColor = getBrandStatusBarColor();
            getWindow().setStatusBarColor(bg);
            getWindow().setNavigationBarColor(bg);
        }
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            View decor = getWindow().getDecorView();
            int flags = decor.getSystemUiVisibility();
            if (!isDark) {
                flags |= View.SYSTEM_UI_FLAG_LIGHT_STATUS_BAR;
            } else {
                flags &= ~View.SYSTEM_UI_FLAG_LIGHT_STATUS_BAR;
            }
            decor.setSystemUiVisibility(flags);
        }
    }

    private int getBrandStatusBarColor() {
        switch (currentBrand) {
            case "ccl": return Color.parseColor("#1C7BC4");
            case "dr":  return Color.parseColor("#3d7a32");
            default:    return Color.parseColor("#00c8c8");
        }
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
        settings.setSupportZoom(false);
        settings.setAllowFileAccess(true);
        settings.setAllowContentAccess(false);
        settings.setGeolocationEnabled(false);
        settings.setTextZoom(100);

        // Allow local file access for the embedded app
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.JELLY_BEAN) {
            settings.setAllowFileAccessFromFileURLs(true);
            settings.setAllowUniversalAccessFromFileURLs(true);
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
            CookieManager.getInstance().setAcceptThirdPartyCookies(webView, true);
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.KITKAT) {
            WebView.setWebContentsDebuggingEnabled(true);
        }

        // Transparent background so native window bg shows through during load
        webView.setBackgroundColor(Color.TRANSPARENT);

        webView.addJavascriptInterface(new WebAppInterface(), "MyDeskAndroid");

        webView.setWebViewClient(new WebViewClient() {
            @Override
            public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                String url = request.getUrl().toString();
                if (url.startsWith("tel:")) {
                    startActivity(new Intent(Intent.ACTION_DIAL, Uri.parse(url)));
                    return true;
                }
                if (url.startsWith("mailto:")) {
                    startActivity(new Intent(Intent.ACTION_SENDTO, Uri.parse(url)));
                    return true;
                }
                // Open external URLs in browser, not WebView
                if (!url.startsWith("file://") && !url.startsWith(WEB_APP_URL)) {
                    startActivity(new Intent(Intent.ACTION_VIEW, Uri.parse(url)));
                    return true;
                }
                return false;
            }

            @Override
            public void onPageStarted(WebView view, String url, Bitmap favicon) {
                super.onPageStarted(view, url, favicon);
                progressBar.setVisibility(View.VISIBLE);
                progressBar.setProgress(0);
            }

            @Override
            public void onPageFinished(WebView view, String url) {
                super.onPageFinished(view, url);
                progressBar.setVisibility(View.GONE);
                loadingOverlay.setVisibility(View.GONE);
                swipeRefreshLayout.setRefreshing(false);
                // Inject persisted prefs into the page's localStorage
                injectPreferences();
                injectNativeBridge();
            }

            @Override
            public void onReceivedError(WebView view, int errorCode, String description, String failingUrl) {
                super.onReceivedError(view, errorCode, description, failingUrl);
                progressBar.setVisibility(View.GONE);
                swipeRefreshLayout.setRefreshing(false);
                if (!isNetworkAvailable() && !failingUrl.startsWith("file://")) {
                    showOfflinePage();
                }
            }
        });

        webView.setWebChromeClient(new WebChromeClient() {
            @Override
            public void onProgressChanged(WebView view, int newProgress) {
                super.onProgressChanged(view, newProgress);
                progressBar.setProgress(newProgress);
                if (newProgress == 100) progressBar.setVisibility(View.GONE);
            }

            @Override
            public void onPermissionRequest(PermissionRequest request) {
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
                    request.grant(request.getResources());
                }
            }

            @Override
            public boolean onShowFileChooser(WebView webView, ValueCallback<Uri[]> filePathCallback,
                                             FileChooserParams fileChooserParams) {
                MainActivity.this.filePathCallback = filePathCallback;
                try {
                    startActivityForResult(fileChooserParams.createIntent(), REQUEST_CODE_FILE_CHOOSER);
                } catch (Exception e) {
                    MainActivity.this.filePathCallback = null;
                    return false;
                }
                return true;
            }
        });
    }

    private void injectPreferences() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.KITKAT) {
            String js = "try{" +
                "localStorage.setItem('md_theme','" + currentTheme + "');" +
                "localStorage.setItem('md_brand','" + currentBrand + "');" +
                "if(typeof applyTheme==='function') applyTheme('" + currentTheme + "',false);" +
                "if(typeof applyBrand==='function') applyBrand('" + currentBrand + "',false);" +
                "var toggle=document.getElementById('darkToggle');" +
                "if(toggle) toggle.checked=" + ("dark".equals(currentTheme) ? "true" : "false") + ";" +
                "}catch(e){}";
            webView.evaluateJavascript(js, null);
        }
    }

    private void injectNativeBridge() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.KITKAT) {
            webView.evaluateJavascript(
                "if(!window._mdNative){window._mdNative=true;" +
                "window.MyDeskAndroidBridge={" +
                "  startVoiceRecognition:function(){window.MyDeskAndroid.startVoiceRecognition();}," +
                "  getPlatform:function(){return 'android';}," +
                "  getAppVersion:function(){return '2.0.0';}" +
                "};" +
                // Config screen "Open in Browser" row
                "var openBtn=document.querySelector('[data-action=\"open-browser\"]');" +
                "if(openBtn)openBtn.addEventListener('click',function(){window.MyDeskAndroid.openWebApp();});" +
                "}", null);
        }
    }

    private void setupSwipeRefresh() {
        swipeRefreshLayout.setOnRefreshListener(() -> webView.reload());
        swipeRefreshLayout.setColorSchemeResources(R.color.brand_primary, R.color.brand_secondary);
    }

    private boolean isNetworkAvailable() {
        ConnectivityManager cm = (ConnectivityManager) getSystemService(Context.CONNECTIVITY_SERVICE);
        if (cm == null) return false;
        NetworkInfo active = cm.getActiveNetworkInfo();
        return active != null && active.isConnectedOrConnecting();
    }

    private void showOfflinePage() {
        boolean isDark = "dark".equals(currentTheme);
        String bg = isDark ? "#0d0d1a" : "#f4f4f8";
        String text = isDark ? "#f0f0f8" : "#111126";
        String offlineHtml = "<!DOCTYPE html><html><head>" +
            "<meta name='viewport' content='width=device-width,initial-scale=1'>" +
            "<style>body{margin:0;display:flex;align-items:center;justify-content:center;" +
            "min-height:100vh;background:" + bg + ";color:" + text + ";" +
            "font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;}" +
            ".c{text-align:center;padding:32px;}" +
            ".ic{font-size:64px;margin-bottom:20px;}" +
            "h2{font-size:22px;font-weight:800;margin-bottom:10px;}" +
            "p{font-size:14px;color:#888;line-height:1.5;}" +
            "</style></head><body>" +
            "<div class='c'><div class='ic'>✦</div>" +
            "<h2>Desky is offline</h2>" +
            "<p>Check your connection.<br>Desky will be back shortly.</p>" +
            "</div></body></html>";
        webView.loadDataWithBaseURL(null, offlineHtml, "text/html", "UTF-8", null);
    }

    private void startVoiceRecognition() {
        Intent intent = new Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH);
        intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL, RecognizerIntent.LANGUAGE_MODEL_FREE_FORM);
        intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE, Locale.getDefault());
        intent.putExtra(RecognizerIntent.EXTRA_PROMPT, "Ask Desky anything…");
        try {
            startActivityForResult(intent, REQUEST_CODE_SPEECH_INPUT);
        } catch (Exception e) {
            Toast.makeText(this, "Voice recognition not available", Toast.LENGTH_SHORT).show();
        }
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, @Nullable Intent data) {
        super.onActivityResult(requestCode, resultCode, data);

        if (requestCode == REQUEST_CODE_SPEECH_INPUT && resultCode == RESULT_OK && data != null) {
            ArrayList<String> results = data.getStringArrayListExtra(RecognizerIntent.EXTRA_RESULTS);
            if (results != null && !results.isEmpty()) {
                String spoken = results.get(0).replace("'", "\\'").replace("\n", "\\n");
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.KITKAT) {
                    webView.evaluateJavascript(
                        "(function(){" +
                        "  var inp=document.getElementById('chatInput');" +
                        "  if(inp){inp.value='" + spoken + "';" +
                        "    inp.dispatchEvent(new Event('input',{bubbles:true}));}" +
                        "})();", null);
                }
            }
        } else if (requestCode == REQUEST_CODE_FILE_CHOOSER) {
            Uri[] results = null;
            if (resultCode == RESULT_OK && data != null) {
                String str = data.getDataString();
                if (str != null) results = new Uri[]{Uri.parse(str)};
            }
            if (filePathCallback != null) {
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
    @SuppressWarnings("deprecation")
    public void onBackPressed() {
        if (webView.canGoBack()) {
            webView.goBack();
        } else {
            new AlertDialog.Builder(this)
                .setTitle("Exit MyDesk")
                .setMessage("Are you sure you want to exit?")
                .setPositiveButton("Exit", (dialog, which) -> finish())
                .setNegativeButton("Cancel", null)
                .show();
        }
    }

    public class WebAppInterface {
        @JavascriptInterface
        public void startVoiceRecognition() {
            runOnUiThread(() -> MainActivity.this.startVoiceRecognition());
        }

        @JavascriptInterface
        public String getPlatform() { return "android"; }

        @JavascriptInterface
        public String getAppVersion() { return "2.0.0"; }

        @JavascriptInterface
        public boolean isOnline() { return isNetworkAvailable(); }

        @JavascriptInterface
        public void openWebApp() {
            runOnUiThread(() -> {
                Intent intent = new Intent(Intent.ACTION_VIEW, Uri.parse(WEB_APP_URL));
                startActivity(intent);
            });
        }

        @JavascriptInterface
        public void saveTheme(String theme) {
            currentTheme = theme;
            prefs.edit().putString(PREF_THEME, theme).apply();
            runOnUiThread(() -> applyWindowStyle());
        }

        @JavascriptInterface
        public void saveBrand(String brand) {
            currentBrand = brand;
            prefs.edit().putString(PREF_BRAND, brand).apply();
            runOnUiThread(() -> applyWindowStyle());
        }

        @JavascriptInterface
        public void shareText(String text) {
            runOnUiThread(() -> {
                Intent intent = new Intent(Intent.ACTION_SEND);
                intent.setType("text/plain");
                intent.putExtra(Intent.EXTRA_TEXT, text);
                startActivity(Intent.createChooser(intent, "Share via"));
            });
        }

        @JavascriptInterface
        public void showToast(String message) {
            runOnUiThread(() -> Toast.makeText(MainActivity.this, message, Toast.LENGTH_SHORT).show());
        }
    }
}
