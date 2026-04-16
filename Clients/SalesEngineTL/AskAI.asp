<%
Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
%>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Ask AI - Techlight MyDesk</title>
    <link rel="icon" type="image/x-icon" href="/favicon.ico">
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600&display=swap" rel="stylesheet">
    <link rel="stylesheet" type="text/css" href="/System/Style_Modern.css">
    <style>
        body, html {
            margin: 0;
            padding: 0;
            height: 100%;
            background: var(--dark);
            color: #fff;
            font-family: 'Inter', sans-serif;
            overflow: hidden;
        }
        .ai-container {
            display: flex;
            flex-direction: column;
            height: 100vh;
            max-width: 100%;
            margin: 0 auto;
            position: relative;
        }
        .ai-header {
            padding: 16px 20px;
            background: rgba(8, 18, 26, 0.8);
            backdrop-filter: blur(10px);
            border-bottom: 1px solid rgba(255,255,255,0.05);
            display: flex;
            align-items: center;
            gap: 12px;
            z-index: 10;
        }
        .ai-header-icon {
            width: 32px;
            height: 32px;
            border-radius: 8px;
            background: linear-gradient(135deg, var(--primary) 0%, var(--primary-dark) 100%);
            display: flex;
            align-items: center;
            justify-content: center;
        }
        .ai-header-title {
            font-size: 1.1rem;
            font-weight: 600;
            margin: 0;
        }
        .ai-header-subtitle {
            font-size: 0.75rem;
            color: var(--primary);
            opacity: 0.8;
            margin: 0;
        }
        .ai-chat-area {
            flex: 1;
            overflow-y: auto;
            padding: 24px;
            display: flex;
            flex-direction: column;
            gap: 20px;
        }
        .ai-message {
            display: flex;
            gap: 16px;
            max-width: 90%;
            animation: fadeIn 0.3s ease;
        }
        @keyframes fadeIn {
            from { opacity: 0; transform: translateY(10px); }
            to { opacity: 1; transform: translateY(0); }
        }
        .ai-message.user {
            align-self: flex-end;
            flex-direction: row-reverse;
        }
        .ai-avatar {
            width: 36px;
            height: 36px;
            border-radius: 12px;
            display: flex;
            align-items: center;
            justify-content: center;
            flex-shrink: 0;
        }
        .ai-message.assistant .ai-avatar {
            background: rgba(0, 200, 200, 0.1);
            color: var(--primary);
            border: 1px solid rgba(0, 200, 200, 0.2);
        }
        .ai-message.user .ai-avatar {
            background: var(--charcoal);
            color: white;
        }
        .ai-bubble {
            background: rgba(255,255,255,0.03);
            border: 1px solid rgba(255,255,255,0.05);
            padding: 16px;
            border-radius: 12px;
            font-size: 0.95rem;
            line-height: 1.6;
            color: rgba(255,255,255,0.9);
        }
        .ai-message.user .ai-bubble {
            background: rgba(0, 200, 200, 0.1);
            border-color: rgba(0, 200, 200, 0.2);
        }
        .ai-input-area {
            padding: 20px;
            background: linear-gradient(to top, var(--dark) 50%, transparent);
            position: relative;
        }
        .ai-input-wrapper {
            position: relative;
            max-width: 800px;
            margin: 0 auto;
        }
        .ai-input {
            width: 100%;
            background: rgba(255,255,255,0.05);
            border: 1px solid rgba(255,255,255,0.1);
            border-radius: 24px;
            padding: 16px 60px 16px 24px;
            font-size: 1rem;
            color: white;
            font-family: inherit;
            resize: none;
            box-shadow: 0 4px 24px rgba(0,0,0,0.2);
            transition: all 0.2s;
            box-sizing: border-box;
        }
        .ai-input:focus {
            outline: none;
            border-color: var(--primary);
            background: rgba(255,255,255,0.08);
        }
        .ai-send-btn {
            position: absolute;
            right: 8px;
            top: 8px;
            bottom: 8px;
            width: 40px;
            border-radius: 18px;
            border: none;
            background: var(--primary);
            color: var(--dark);
            cursor: pointer;
            display: flex;
            align-items: center;
            justify-content: center;
            transition: all 0.2s;
        }
        .ai-send-btn:hover:not(:disabled) {
            transform: scale(1.05);
            background: var(--primary-light);
        }
        .ai-send-btn:disabled {
            opacity: 0.5;
            cursor: not-allowed;
        }
        .markdown-body pre {
            background: rgba(0,0,0,0.3);
            padding: 12px;
            border-radius: 8px;
            overflow-x: auto;
            border: 1px solid rgba(255,255,255,0.1);
        }
        .markdown-body code {
            font-family: 'JetBrains Mono', monospace;
            font-size: 0.85em;
        }
        .loading-dots {
            display: flex;
            gap: 4px;
            padding: 8px;
        }
        .loading-dots div {
            width: 6px;
            height: 6px;
            border-radius: 50%;
            background: var(--primary);
            animation: bounce 1.4s infinite ease-in-out both;
        }
        .loading-dots div:nth-child(1) { animation-delay: -0.32s; }
        .loading-dots div:nth-child(2) { animation-delay: -0.16s; }
        @keyframes bounce {
            0%, 80%, 100% { transform: scale(0); }
            40% { transform: scale(1); }
        }
    </style>
</head>
<body>

<div class="ai-container">
    <div class="ai-header">
        <div class="ai-header-icon">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="color:var(--dark);">
                <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"></path>
                <circle cx="9" cy="10" r="1" fill="currentColor"></circle>
                <circle cx="15" cy="10" r="1" fill="currentColor"></circle>
            </svg>
        </div>
        <div>
            <h1 class="ai-header-title">Ask AI</h1>
            <p class="ai-header-subtitle">Powered by Azure OpenAI</p>
        </div>
    </div>

    <div class="ai-chat-area" id="chatArea">
        <div class="ai-message assistant">
            <div class="ai-avatar">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"></path>
                </svg>
            </div>
            <div class="ai-bubble">
                Hello <%= Session("Name") %>! I'm your Techlight MyDesk AI assistant. I can help you query MYOB data, draft quotes, or explain system features. How can I help you today?
            </div>
        </div>
    </div>

    <div class="ai-input-area">
        <div class="ai-input-wrapper">
            <input type="text" id="userInput" class="ai-input" placeholder="Message Techlight AI..." autocomplete="off" onkeypress="handleKeyPress(event)">
            <button id="sendBtn" class="ai-send-btn" onclick="sendMessage()">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <line x1="22" y1="2" x2="11" y2="13"></line>
                    <polygon points="22 2 15 22 11 13 2 9 22 2"></polygon>
                </svg>
            </button>
        </div>
    </div>
</div>

<script>
    // Configuration from user specs
    const AZURE_OPENAI_KEY = "B2R52mT8Ifegc2rcLsUBo0hLq1IEiK4fr6ne7ZVdQPd9LsoWrBSzJQQJ99CCACHYHv6XJ3w3AAAAACOG1Sbr";
    const AZURE_OPENAI_ENDPOINT = "https://techlight-ai.openai.azure.com/openai/deployments/gpt-4o/chat/completions?api-version=2024-02-15-preview";
    
    let chatHistory = [
        { role: "system", content: "You are Techlight MyDesk AI, a helpful, deeply integrated AI assistant for the Techlight portal. You help users manage Quotes, Invoices, Purchase Orders, and MYOB synchronization. Be concise, professional, and use modern formatting." }
    ];

    const chatArea = document.getElementById('chatArea');
    const userInput = document.getElementById('userInput');
    const sendBtn = document.getElementById('sendBtn');

    function handleKeyPress(e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    }

    function appendMessage(role, content, isLoading = false) {
        const msgDiv = document.createElement('div');
        msgDiv.className = `ai-message ${role}`;
        
        let avatarIcon = role === 'user' 
            ? '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>'
            : '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"></path></svg>';
            
        let bubbleContent = content;
        if (isLoading) {
            bubbleContent = '<div class="loading-dots"><div></div><div></div><div></div></div>';
            msgDiv.id = 'loadingMessage';
        }

        msgDiv.innerHTML = `
            <div class="ai-avatar">${avatarIcon}</div>
            <div class="ai-bubble markdown-body">${bubbleContent}</div>
        `;
        
        chatArea.appendChild(msgDiv);
        chatArea.scrollTop = chatArea.scrollHeight;
        return msgDiv;
    }

    async function sendMessage() {
        const text = userInput.value.trim();
        if (!text) return;

        // Add user message
        appendMessage('user', text);
        chatHistory.push({ role: "user", content: text });
        
        userInput.value = '';
        userInput.disabled = true;
        sendBtn.disabled = true;

        // Show loading
        appendMessage('assistant', '', true);

        try {
            const response = await fetch(AZURE_OPENAI_ENDPOINT, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'api-key': AZURE_OPENAI_KEY
                },
                body: JSON.stringify({
                    messages: chatHistory,
                    temperature: 0.7,
                    max_tokens: 800,
                    stream: false
                })
            });

            const data = await response.json();
            document.getElementById('loadingMessage').remove();

            if (data.choices && data.choices.length > 0) {
                const aiResponse = data.choices[0].message.content;
                chatHistory.push({ role: "assistant", content: aiResponse });
                // Simple markdown conversion for bolding and line breaks
                const formattedResponse = aiResponse
                    .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
                    .replace(/\n/g, '<br>');
                appendMessage('assistant', formattedResponse);
            } else {
                appendMessage('assistant', "I'm sorry, I couldn't process that request at the moment.");
            }
        } catch (error) {
            console.error("AI Error:", error);
            document.getElementById('loadingMessage').remove();
            appendMessage('assistant', "I encountered a connection error navigating to MyDesk MCP server. Please try again.");
        } finally {
            userInput.disabled = false;
            sendBtn.disabled = false;
            userInput.focus();
        }
    }
</script>

</body>
</html>
