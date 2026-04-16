<%
' Standardized Response Headers for Techlight MyDesk
' Purpose: Prevent caching of dynamic ASP pages to ensure data security and freshness
' Last Updated: April 16, 2026

' Set Cache-Control header - primary caching directive for HTTP/1.1
' no-store: Do not store any part of the request or response
' no-cache: Must revalidate before using cached copy
' must-revalidate: Must verify with server before using stale copy
' private: For single user, not shared caches
Response.CacheControl = "no-store, no-cache, must-revalidate, private"

' Set Pragma header for HTTP/1.0 compatibility
Response.AddHeader "Pragma", "no-cache"

' Set Expires header - negative value means immediately expired
Response.Expires = -1

' Set ExpiresAbsolute to a past date to ensure immediate expiration
' Using -1 day instead of future date to prevent any caching
Response.ExpiresAbsolute = DateAdd("d", -1, Now())
%>
