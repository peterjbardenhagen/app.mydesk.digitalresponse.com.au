package com.digitalresponse.mydesk.shared

import com.squareup.moshi.JsonClass

@JsonClass(generateAdapter = true)
data class AIChatMessage(
    val role: String,
    val content: String,
    val timestamp: String
)

@JsonClass(generateAdapter = true)
data class Tenant(
    val tenantId: String,
    val name: String,
    val slug: String,
    val branding: Branding?
)

@JsonClass(generateAdapter = true)
data class Branding(
    val primaryColor: String,
    val accentColor: String,
    val logoUrl: String?
)
