package com.digitalresponse.mydesk.shared

import com.digitalresponse.mydesk.shared.models.AIChatMessage
import com.digitalresponse.mydesk.shared.models.Tenant
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.POST
import retrofit2.http.Path

interface ApiService {
    @POST("api/auth/login")
    suspend fun login(@Body credentials: Map<String, String>): LoginResponse

    @GET("api/tenants")
    suspend fun getTenants(@Header("TenantId") tenantId: String? = null): List<Tenant>

    @GET("api/platformsettings/{tenantId}")
    suspend fun getPlatformSettings(@Path("tenantId") tenantId: String): String

    @POST("api/ai/chat")
    suspend fun postChat(@Body message: Map<String, String>): AIChatMessage
}

data class LoginResponse(
    val accessToken: String,
    val refreshToken: String,
    val tenantId: String,
    val expiresIn: Long
)
