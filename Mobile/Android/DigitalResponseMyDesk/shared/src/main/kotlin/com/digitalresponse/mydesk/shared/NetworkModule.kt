package com.digitalresponse.mydesk.shared

import android.content.Context
import com.digitalresponse.mydesk.shared.ApiService
import com.digitalresponse.mydesk.shared.AIChatMessage
import com.digitalresponse.mydesk.shared.Tenant
import com.squareup.moshi.Moshi
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory
import okhttp3.Interceptor
import okhttp3.OkHttpClient
import okhttp3.Response
import org.koin.dsl.module
import retrofit2.Retrofit
import retrofit2.converter.moshi.MoshiConverterFactory
import java.util.concurrent.TimeUnit

// Interceptor to add auth headers
class AuthInterceptor(private val context: Context) : Interceptor {
    override fun intercept(chain: Interceptor.Chain): Response {
        val prefs = context.getSharedPreferences("mydesk_prefs", Context.MODE_PRIVATE)
        val token = prefs.getString("access_token", "")
        val tenantId = prefs.getString("tenant_id", "")
        val request = chain.request().newBuilder()
            .addHeader("Authorization", "Bearer $token")
            .addHeader("TenantId", tenantId ?: "")
            .build()
        return chain.proceed(request)
    }
}

val networkModule = module {
    single { 
        val context: Context = get()
        OkHttpClient.Builder()
            .addInterceptor(AuthInterceptor(context))
            .connectTimeout(30, TimeUnit.SECONDS)
            .readTimeout(30, TimeUnit.SECONDS)
            .build()
    }
    single {
        val moshi = Moshi.Builder()
            .add(KotlinJsonAdapterFactory())
            .build()
        Retrofit.Builder()
            .baseUrl("https://demo.digitalresponse.com.au/") // will be overridden via config if needed
            .client(get())
            .addConverterFactory(MoshiConverterFactory.create(moshi))
            .build()
    }
    single { get<Retrofit>().create(ApiService::class.java) }
    single { Repository(get()) }
}

class Repository(private val api: ApiService) {
    suspend fun login(username: String, password: String): com.digitalresponse.mydesk.shared.LoginResponse {
        return api.login(mapOf("username" to username, "password" to password))
    }
    suspend fun getTenants(): List<Tenant> = api.getTenants()
    suspend fun getPlatformSettings(tenantId: String): String = api.getPlatformSettings(tenantId)
    suspend fun postChat(message: String): AIChatMessage = api.postChat(mapOf("content" to message))
}
