import UIKit
import WebKit

class ViewController: UIViewController, WKNavigationDelegate, WKUIDelegate {

    var webView: WKWebView!
    var activityIndicator: UIActivityIndicatorView!

    // Primary and fallback server endpoints
    let primaryURL = URL(string: "https://biglineconnect.bigus.com.tr")!
    let fallbackURL = URL(string: "https://biglineconnect-production.up.railway.app")!

    override var prefersStatusBarHidden: Bool {
        return false
    }

    override var preferredStatusBarStyle: UIStatusBarStyle {
        return .lightContent
    }

    override func viewDidLoad() {
        super.viewDidLoad()
        view.backgroundColor = UIColor(red: 0.04, green: 0.06, blue: 0.10, alpha: 1.0) // #0A0F1A

        setupWebView()
        setupActivityIndicator()
        loadRemoteService()
    }

    private func setupWebView() {
        let config = WKWebViewConfiguration()
        config.allowsInlineMediaPlayback = true
        config.mediaTypesRequiringUserActionForPlayback = []
        config.preferences.javaScriptCanOpenWindowsAutomatically = true
        
        let prefs = WKWebpagePreferences()
        prefs.allowsContentJavaScript = true
        config.defaultWebpagePreferences = prefs

        webView = WKWebView(frame: view.bounds, configuration: config)
        webView.autoresizingMask = [.flexibleWidth, .flexibleHeight]
        webView.navigationDelegate = self
        webView.uiDelegate = self
        webView.backgroundColor = UIColor(red: 0.04, green: 0.06, blue: 0.10, alpha: 1.0)
        webView.isOpaque = false
        webView.scrollView.bounces = false
        webView.scrollView.contentInsetAdjustmentBehavior = .never
        webView.scrollView.showsVerticalScrollIndicator = false
        webView.scrollView.showsHorizontalScrollIndicator = false

        // Custom User Agent to identify B-Connect Native iOS App
        webView.customUserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 BigLineConnect-iOS/1.0"

        view.addSubview(webView)
    }

    private func setupActivityIndicator() {
        activityIndicator = UIActivityIndicatorView(style: .large)
        activityIndicator.color = UIColor(red: 0.0, green: 0.9, blue: 1.0, alpha: 1.0) // Neon cyan #00E5FF
        activityIndicator.hidesWhenStopped = true
        activityIndicator.center = view.center
        view.addSubview(activityIndicator)
        activityIndicator.startAnimating()
    }

    private func loadRemoteService() {
        var request = URLRequest(url: primaryURL, cachePolicy: .useProtocolCachePolicy, timeoutInterval: 15.0)
        request.setValue("BigLineConnect-iOS", forHTTPHeaderField: "X-Requested-With")
        webView.load(request)
    }

    // MARK: - WKNavigationDelegate
    func webView(_ webView: WKWebView, didFinish navigation: WKNavigation!) {
        activityIndicator.stopAnimating()
    }

    func webView(_ webView: WKWebView, didFail navigation: WKNavigation!, withError error: Error) {
        handleLoadFailure()
    }

    func webView(_ webView: WKWebView, didFailProvisionalNavigation navigation: WKNavigation!, withError error: Error) {
        handleLoadFailure()
    }

    private func handleLoadFailure() {
        activityIndicator.stopAnimating()
        // If primary domain fails, attempt fallback to Railway mirror
        if webView.url?.host != fallbackURL.host {
            let fallbackRequest = URLRequest(url: fallbackURL, cachePolicy: .useProtocolCachePolicy, timeoutInterval: 15.0)
            webView.load(fallbackRequest)
        }
    }
}

