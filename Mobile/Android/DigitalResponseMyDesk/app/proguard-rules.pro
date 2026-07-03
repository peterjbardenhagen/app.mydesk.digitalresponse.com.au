# Keep WebView JavaScript interface methods
-keepclassmembers class * {
    @android.webkit.JavascriptInterface <methods>;
}

# Keep AndroidX annotations
-keep class androidx.** { *; }

# Don't obfuscate the main activity
-keep class com.digitalresponse.mydesk.** { *; }