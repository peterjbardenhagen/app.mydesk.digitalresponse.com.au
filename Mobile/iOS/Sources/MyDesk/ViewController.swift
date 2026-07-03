import UIKit
import WebKit

/// iOS equivalent of Android's MainActivity.java.
///
/// Loads file:///app.html from the app bundle via WKWebView, injects the
/// same MyDesk configuration that the Android layer provides, and wires
/// the native JavaScript bridge (see WebBridge.swift).
///
/// Dev vs Prod endpoint is driven by the API_BASE_URL key in Info.plist
/// (set per Xcode build configuration — Debug/Release).
final class ViewController: UIViewController {

    // MARK: - Properties

    private var webView: WKWebView!
    private var progressView: UIProgressView!
    private var refreshControl: UIRefreshControl!

    private var progressObserver: NSKeyValueObservation?

    // Mirrors Android SharedPreferences
    private let prefs = UserDefaults.standard

    // Matches Info.plist key injected by Xcode build configuration
    private var apiBaseUrl: String {
        Bundle.main.object(forInfoDictionaryKey: "API_BASE_URL") as? String
            ?? "https://app.mydesk.digitalresponse.com.au"
    }

    private var currentTheme: String {
        get { prefs.string(forKey: "md_theme") ?? "dark" }
        set { prefs.set(newValue, forKey: "md_theme") }
    }

    private var currentBrand: String {
        get { prefs.string(forKey: "md_brand") ?? "techlight" }
        set { prefs.set(newValue, forKey: "md_brand") }
    }

    // MARK: - Lifecycle

    override func viewDidLoad() {
        super.viewDidLoad()
        setupWebView()
        setupProgressView()
        applyWindowStyle()
        loadAppHtml()
    }

    override func viewWillAppear(_ animated: Bool) {
        super.viewWillAppear(animated)
        navigationController?.setNavigationBarHidden(true, animated: animated)
    }

    // MARK: - WebView setup

    private func setupWebView() {
        let config  = WKWebViewConfiguration()
        let content = config.userContentController

        // Register bridge message handlers (see WebBridge.swift)
        let bridge = WebBridge(viewController: self)
        for name in WebBridge.handlerNames {
            content.add(bridge, name: name)
        }

        // Inject MyDeskConfig at document start so app.html picks up the right API URL
        // before its own <script> tags execute.
        let configScript = WKUserScript(
            source: """
                window.MyDeskConfig = {
                    apiUrl:   '\(apiBaseUrl)',
                    platform: 'ios',
                    version:  '\(appVersion)'
                };
                """,
            injectionTime: .atDocumentStart,
            forMainFrameOnly: true)
        content.addUserScript(configScript)

        // Allow file:// to load cross-origin resources (mirrors Android setAllowUniversalAccessFromFileURLs)
        config.preferences.setValue(true, forKey: "allowFileAccessFromFileURLs")

        webView = WKWebView(frame: view.bounds, configuration: config)
        webView.autoresizingMask = [.flexibleWidth, .flexibleHeight]
        webView.backgroundColor  = .clear
        webView.isOpaque         = false
        webView.navigationDelegate = self
        webView.scrollView.bounces = false
        view.addSubview(webView)

        // Pull-to-refresh
        refreshControl = UIRefreshControl()
        refreshControl.addTarget(self, action: #selector(refresh), for: .valueChanged)
        webView.scrollView.addSubview(refreshControl)

        // Progress bar KVO
        progressObserver = webView.observe(\.estimatedProgress, options: [.new]) { [weak self] wv, _ in
            DispatchQueue.main.async {
                self?.progressView.setProgress(Float(wv.estimatedProgress), animated: true)
                self?.progressView.isHidden = wv.estimatedProgress >= 1.0
            }
        }
    }

    private func setupProgressView() {
        progressView = UIProgressView(progressViewStyle: .bar)
        progressView.translatesAutoresizingMaskIntoConstraints = false
        progressView.trackTintColor = .clear
        progressView.progressTintColor = brandColor()
        view.addSubview(progressView)
        NSLayoutConstraint.activate([
            progressView.topAnchor.constraint(equalTo: view.safeAreaLayoutGuide.topAnchor),
            progressView.leadingAnchor.constraint(equalTo: view.leadingAnchor),
            progressView.trailingAnchor.constraint(equalTo: view.trailingAnchor),
            progressView.heightAnchor.constraint(equalToConstant: 2),
        ])
    }

    private func loadAppHtml() {
        guard let url = Bundle.main.url(forResource: "app", withExtension: "html") else {
            return
        }
        // baseURL must be the file's directory for relative asset references to resolve
        webView.loadFileURL(url, allowingReadAccessTo: url.deletingLastPathComponent())
    }

    // MARK: - Actions

    @objc private func refresh() {
        webView.reload()
    }

    // MARK: - Preferences injection (mirrors Android injectPreferences)

    func injectPreferences() {
        let theme = currentTheme
        let brand = currentBrand
        let js = """
            try {
                localStorage.setItem('md_theme', '\(theme)');
                localStorage.setItem('md_brand', '\(brand)');
                if (typeof applyTheme === 'function') applyTheme('\(theme)', false);
                if (typeof applyBrand === 'function') applyBrand('\(brand)', false);
                var toggle = document.getElementById('darkToggle');
                if (toggle) toggle.checked = \(theme == "dark" ? "true" : "false");
            } catch(e) {}
            """
        webView.evaluateJavaScript(js)
    }

    // MARK: - Theme / brand

    func saveTheme(_ theme: String) {
        currentTheme = theme
        applyWindowStyle()
    }

    func saveBrand(_ brand: String) {
        currentBrand = brand
        applyWindowStyle()
    }

    private func applyWindowStyle() {
        let isDark = currentTheme == "dark"
        let bg     = isDark
            ? UIColor(red: 0.05, green: 0.05, blue: 0.10, alpha: 1)
            : UIColor(red: 0.96, green: 0.96, blue: 0.97, alpha: 1)

        view.backgroundColor = bg
        webView.backgroundColor = bg
        webView.scrollView.backgroundColor = bg

        if #available(iOS 13.0, *) {
            overrideUserInterfaceStyle = isDark ? .dark : .light
        }
    }

    private func brandColor() -> UIColor {
        switch currentBrand {
        case "ccl":         return UIColor(red: 0.11, green: 0.48, blue: 0.77, alpha: 1) // #1C7BC4
        case "dr":          return UIColor(red: 0.24, green: 0.48, blue: 0.20, alpha: 1) // #3d7a32
        default:            return UIColor(red: 0.00, green: 0.78, blue: 0.78, alpha: 1) // #00c8c8 Techlight
        }
    }

    // MARK: - Voice recognition

    func startVoiceRecognition() {
        // SFSpeechRecognizer integration — see WebBridge for the JS callback approach
        guard let url = URL(string: "x-apple.speech:") else { return }
        if UIApplication.shared.canOpenURL(url) {
            // Native speech — inject result into chatInput via evaluateJavaScript
            // Full implementation: use SFSpeechRecognizer + AVAudioEngine
        }
    }

    func injectVoiceResult(_ text: String) {
        let escaped = text.replacingOccurrences(of: "'", with: "\\'")
        let js = """
            (function(){
                var inp = document.getElementById('chatInput');
                if (inp) {
                    inp.value = '\(escaped)';
                    inp.dispatchEvent(new Event('input', { bubbles: true }));
                }
            })();
            """
        webView.evaluateJavaScript(js)
    }

    // MARK: - Helpers

    private var appVersion: String {
        Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "2.0"
    }
}

// MARK: - WKNavigationDelegate

extension ViewController: WKNavigationDelegate {

    func webView(_ webView: WKWebView, didFinish navigation: WKNavigation!) {
        refreshControl.endRefreshing()
        injectPreferences()
    }

    func webView(_ webView: WKWebView, didFail navigation: WKNavigation!, withError error: Error) {
        refreshControl.endRefreshing()
    }

    func webView(_ webView: WKWebView,
                 decidePolicyFor navigationAction: WKNavigationAction,
                 decisionHandler: @escaping (WKNavigationActionPolicy) -> Void) {

        guard let url = navigationAction.request.url else {
            decisionHandler(.allow); return
        }

        let urlString = url.absoluteString

        if urlString.hasPrefix("tel:") || urlString.hasPrefix("mailto:") {
            UIApplication.shared.open(url)
            decisionHandler(.cancel); return
        }

        // External URLs open in Safari, not WebView
        if !urlString.hasPrefix("file://") && !urlString.hasPrefix(apiBaseUrl) {
            UIApplication.shared.open(url)
            decisionHandler(.cancel); return
        }

        decisionHandler(.allow)
    }
}
