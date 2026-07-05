# MyDesk iOS App

WKWebView wrapper that runs the same `app.html` as the Android app.
The Swift layer provides native preferences, bridge methods, and dev/prod endpoint switching.

## Xcode Project Setup

1. **Create project**
   - Open Xcode → File → New → Project → iOS → App
   - Product Name: `MyDesk`
   - Bundle ID: `com.digitalresponse.mydesk`
   - Interface: `Storyboard` (delete `Main.storyboard` — we use code-only layout)
   - Language: Swift

2. **Add source files**
   Copy `Sources/MyDesk/` into the Xcode project:
   - `AppDelegate.swift`
   - `ViewController.swift`
   - `WebBridge.swift`

3. **Add app.html as a bundle resource**
   - Drag `Mobile/Android/.../assets/app.html` into the Xcode project
   - Target Membership: ✅ MyDesk
   - It will be accessible at `Bundle.main.url(forResource: "app", withExtension: "html")`

4. **Configure Info.plist** — add these keys:

   ```xml
   <!-- API endpoint — overridden per build configuration (see step 5) -->
   <key>API_BASE_URL</key>
   <string>https://app.mydesk.digitalresponse.com.au</string>

   <!-- Allow loading the local HTML file -->
   <key>NSAppTransportSecurity</key>
   <dict>
       <key>NSAllowsLocalNetworking</key>
       <true/>
   </dict>

   <!-- Microphone access for voice input -->
   <key>NSMicrophoneUsageDescription</key>
   <string>MyDesk uses the microphone for voice commands to Desky.</string>

   <!-- Speech recognition -->
   <key>NSSpeechRecognitionUsageDescription</key>
   <string>MyDesk transcribes your voice to send messages to Desky.</string>
   ```

5. **Dev vs Prod endpoint** (build configurations)
   - Xcode → Project → Info → Configurations: add `Debug` and `Release` (already exist)
   - Select the **Debug** configuration target → Build Settings → search `API_BASE_URL`
   - Add a User-Defined Setting: `API_BASE_URL = https://dev.digitalresponse.com.au`
   - For **Release**: `API_BASE_URL = https://app.mydesk.digitalresponse.com.au`
   - Update Info.plist key `API_BASE_URL` to use the variable: `$(API_BASE_URL)`

6. **Delete Main.storyboard references**
   - In Info.plist remove the `UIMainStoryboardFile` key
   - In `AppDelegate.swift` the window is set up programmatically

7. **Build & Run** on Simulator or device (iOS 14+)

## JavaScript Bridge

iOS uses `window.webkit.messageHandlers.<name>.postMessage(data)` instead of
Android's direct method pattern. `app.html` abstracts this with:

```javascript
function nativeCall(name, data) {
    if (window.MyDeskAndroid?.[name]) return window.MyDeskAndroid[name](data);
    window.webkit?.messageHandlers?.[name]?.postMessage(data ?? null);
}
```

## Capabilities

| Feature | Android | iOS |
|---------|---------|-----|
| Dark/light theme | ✅ | ✅ |
| Brand palette | ✅ | ✅ |
| Dev/prod endpoint | BuildConfig | Info.plist build var |
| Voice input | Android SpeechRecognizer | SFSpeechRecognizer (implement in ViewController) |
| File sharing | Intent.ACTION_SEND | UIActivityViewController |
| Pull-to-refresh | SwipeRefreshLayout | UIRefreshControl |
| Offline page | WebViewClient.onReceivedError | WKNavigationDelegate.didFail |
| Open in browser | Intent.ACTION_VIEW | UIApplication.shared.open |

## Shared `app.html`

Both platforms load the identical `app.html`. Keep a single copy — symlink or CI step
to copy from `Mobile/Android/.../assets/app.html` into the iOS bundle before build.
