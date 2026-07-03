import UIKit
import WebKit

/// JavaScript ↔ Swift bridge, mirroring Android's WebAppInterface.
///
/// iOS uses window.webkit.messageHandlers.<name>.postMessage(data)
/// instead of Android's direct method call pattern.
/// app.html's nativeBridge() abstraction calls the right side transparently.
///
/// Each handler name listed in `handlerNames` must be registered with
/// the WKUserContentController before the WebView loads (see ViewController.setupWebView).
final class WebBridge: NSObject, WKScriptMessageHandler {

    /// All message handler names registered with WKUserContentController.
    static let handlerNames = [
        "saveTheme",
        "saveBrand",
        "startVoiceRecognition",
        "shareText",
        "showToast",
        "openWebApp",
        "isOnline",
        "getPlatform",
        "getAppVersion",
    ]

    weak var viewController: ViewController?

    init(viewController: ViewController) {
        self.viewController = viewController
    }

    // MARK: - WKScriptMessageHandler

    func userContentController(
        _ userContentController: WKUserContentController,
        didReceive message: WKScriptMessage)
    {
        guard let vc = viewController else { return }

        DispatchQueue.main.async {
            switch message.name {

            case "saveTheme":
                if let theme = message.body as? String {
                    vc.saveTheme(theme)
                }

            case "saveBrand":
                if let brand = message.body as? String {
                    vc.saveBrand(brand)
                }

            case "startVoiceRecognition":
                vc.startVoiceRecognition()

            case "shareText":
                if let text = message.body as? String {
                    let activity = UIActivityViewController(activityItems: [text], applicationActivities: nil)
                    vc.present(activity, animated: true)
                }

            case "showToast":
                if let msg = message.body as? String {
                    self.showToast(msg, in: vc)
                }

            case "openWebApp":
                if let url = URL(string: vc.apiBaseUrl ?? "https://app.mydesk.digitalresponse.com.au") {
                    UIApplication.shared.open(url)
                }

            case "isOnline":
                // iOS: call back into JS with the result
                vc.webView?.evaluateJavaScript(
                    "window._mdIsOnlineCallback && window._mdIsOnlineCallback(\(self.isOnline()))")

            case "getPlatform", "getAppVersion":
                break // Values injected at document start via MyDeskConfig

            default:
                break
            }
        }
    }

    // MARK: - Helpers

    private func isOnline() -> Bool {
        // Simple reachability check — replace with NWPathMonitor for production
        guard let url = URL(string: "https://8.8.8.8") else { return false }
        do {
            let _ = try Data(contentsOf: url, options: .uncached)
            return true
        } catch {
            return false
        }
    }

    private func showToast(_ message: String, in vc: UIViewController) {
        let alert = UIAlertController(title: nil, message: message, preferredStyle: .alert)
        vc.present(alert, animated: true)
        DispatchQueue.main.asyncAfter(deadline: .now() + 2) {
            alert.dismiss(animated: true)
        }
    }
}

// MARK: - ViewController extension to expose apiBaseUrl to WebBridge

extension ViewController {
    var apiBaseUrl: String? {
        Bundle.main.object(forInfoDictionaryKey: "API_BASE_URL") as? String
    }
}
